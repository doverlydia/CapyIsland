using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The shared life of a wild creature — a deer, or a monster nobody has tamed. It works the same
    /// job machine as a worker (search every 61 frames, walk, swing, repeat) but with no workplace,
    /// no boundary and no reward: whatever it finishes is simply gone, and a parent it finishes never
    /// comes back as a bubble. Finding nothing, it just stands there.
    ///
    /// A newborn one grows from 0.2 to full size at 0.01 a frame, 70 units from where it appeared
    /// (Animals/States/Born).
    /// </summary>
    public abstract class WildForager : MonoBehaviour, IMapMarker
    {
        enum State { Born, Find, Walk, Swing }

        State _state = State.Born;
        FrameClock _clock;
        int _searchCounter;
        float _bornScale = 0.2f;
        Vector3 _fullScale;
        Vector2 _bornTarget;
        float _hintTimer;
        bool _hinted;

        protected Harvestable TargetNode;
        protected FruitTree TargetTree;
        int _swingFrame, _swingFrames = 9, _hitFrame = 4;

        /// <summary>World units per frame.</summary>
        protected abstract int WalkSpeed { get; }

        /// <summary>Picks the next target, setting <see cref="TargetNode"/> or <see cref="TargetTree"/>.</summary>
        protected abstract void Search();

        /// <summary>Scale removed from a node per hit.</summary>
        protected abstract float HitAmount(Harvestable node);

        /// <summary>The hint shown 30 seconds after it appears.</summary>
        protected abstract string ArrivalHint { get; }

        protected virtual bool Running => true;

        public bool IsBusy => _state == State.Walk || _state == State.Swing;

        protected virtual void Awake()
        {
            _fullScale = transform.localScale;
        }

        protected virtual void OnEnable() => MapMarkers.Register(this);
        protected virtual void OnDisable() => MapMarkers.Unregister(this);

        public MarkerKind MarkerKind => MarkerKind.Pest;
        public virtual bool MarkerActive => isActiveAndEnabled && Running;
        public Vector2 MarkerPosition => transform.position;
        public Sprite MarkerIcon => MapMarkers.SpriteOf(gameObject);
        public string MarkerLabel => TryGetComponent<CatalogItem>(out var tag) ? StoryText.Name(tag.StoreName) : null;

        /// <summary>Starts life: grows in, then wanders off to feed.</summary>
        public void BeginLife()
        {
            _state = State.Born;
            _bornScale = 0.2f;
            transform.localScale = _fullScale * _bornScale;
            float a = Random.value * Mathf.PI * 2f;
            _bornTarget = (Vector2)transform.position + BunniSpace.ToUnity(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 70f);
            _hintTimer = 30f;
            _hinted = false;
        }

        protected virtual void Update()
        {
            if (!Running) return;

            if (!_hinted)
            {
                _hintTimer -= Time.deltaTime;
                if (_hintTimer <= 0f) { _hinted = true; Hints.Island(ArrivalHint); }
            }

            int frames = _clock.Consume(Time.deltaTime);
            for (int i = 0; i < frames && Running; i++) Tick();
        }

        void Tick()
        {
            switch (_state)
            {
                case State.Born:
                    _bornScale = Mathf.Min(1f, _bornScale + 0.01f);
                    transform.localScale = _fullScale * _bornScale;
                    StepToward(_bornTarget, 2);
                    if (_bornScale >= 1f) { _state = State.Find; _searchCounter = 0; }
                    break;

                case State.Find:
                    if (_searchCounter == 0)
                    {
                        Release();
                        Search();
                        if (TargetNode != null || TargetTree != null) { _state = State.Walk; break; }
                    }
                    if (++_searchCounter > BunniRules.SearchIntervalFrames - 1) _searchCounter = 0;
                    break;

                case State.Walk:
                    if (TargetGone) { Release(); _state = State.Find; _searchCounter = 0; break; }
                    var spot = (Vector2)TargetTransform.position + BunniSpace.ToUnity(new Vector2(0f, 5f));
                    if (StepToward(spot, WalkSpeed))
                    {
                        _state = State.Swing;
                        _swingFrame = 0;
                        if (TargetTree != null) BunniRules.Swing(NodeFamily.Tree, out _swingFrames, out _hitFrame);
                        else BunniRules.Swing(TargetNode.Family, out _swingFrames, out _hitFrame);
                    }
                    break;

                case State.Swing:
                    if (TargetGone) { Release(); _state = State.Find; _searchCounter = 0; break; }
                    if (++_swingFrame >= _swingFrames) _swingFrame = 0;
                    if (_swingFrame != _hitFrame) break;
                    Strike();
                    break;
            }
        }

        void Strike()
        {
            if (TargetTree != null)
            {
                // Fruit trees are shaken, one fruit at a time, never chopped.
                var sway = TargetTree.GetComponent<Sway>();
                if (sway != null) sway.Kick(5f, 0.3f);
                Sfx.PlayAt(SfxKind.Rustle, TargetTree.transform.position, 0.6f);
                TargetTree.RemoveOne();
                Release();
                _state = State.Find;
                _searchCounter = 0;
                return;
            }

            var node = TargetNode;
            Sfx.PlayAt(node.Family == NodeFamily.Tree ? SfxKind.Chop : node.Family == NodeFamily.Rock ? SfxKind.Mine : SfxKind.Nibble,
                node.transform.position, 0.6f);
            var nodeSway = node.GetComponent<Sway>();
            if (nodeSway != null) nodeSway.Kick(6f, 0.3f);
            if (HarvestEffects.Instance != null) HarvestEffects.Instance.Hit(node);
            if (!node.Hit(HitAmount(node), spoil: true)) return;
            TargetNode = null;
            _state = State.Find;
            _searchCounter = 0;
        }

        bool TargetGone => TargetTree != null ? !TargetTree.InWorld
                         : TargetNode == null || !TargetNode.InWorld;

        Transform TargetTransform => TargetTree != null ? TargetTree.transform : TargetNode.transform;

        protected void Release()
        {
            if (TargetNode != null) TargetNode.Reserved = false;
            if (TargetTree != null) TargetTree.Reserved = false;
            TargetNode = null;
            TargetTree = null;
        }

        /// <summary>One frame's walk toward a point. True on arrival.</summary>
        protected bool StepToward(Vector2 target, int worldSpeed)
        {
            var pos = (Vector2)transform.position;
            float dx = target.x - pos.x;
            bool arrived = BunniSpace.Step(ref pos, target, worldSpeed);
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
            var body = transform.Find("Sprite");
            if (body != null && Mathf.Abs(dx) > 0.001f)
            {
                var s = body.localScale;
                s.x = Mathf.Abs(s.x) * (dx < 0f ? 1f : -1f);   // the art faces left
                body.localScale = s;
            }
            return arrived;
        }

        /// <summary>
        /// The nearest unreserved, grown node of the wanted families anywhere it can walk to: a child
        /// if there is one, otherwise a parent. Reserves the winner.
        /// </summary>
        protected Harvestable FindNode(System.Func<Harvestable, bool> wanted)
        {
            Harvestable bestChild = null, bestParent = null;
            float dc = float.MaxValue, dp = float.MaxValue;
            var land = LandMap.Instance;
            Vector2 from = transform.position;
            foreach (var node in FindObjectsByType<Harvestable>(FindObjectsSortMode.None))
            {
                if (!node.InWorld || !node.CanHarvest || node.Reserved || !wanted(node)) continue;
                if (land != null && !land.SegmentOnLand(from, node.transform.position)) continue;
                float d = BunniSpace.WorldDistance(from, node.transform.position);
                if (node.IsParent) { if (d < dp) { dp = d; bestParent = node; } }
                else if (d < dc) { dc = d; bestChild = node; }
            }
            var chosen = bestChild != null ? bestChild : bestParent;
            if (chosen != null) chosen.Reserved = true;
            return chosen;
        }

        protected FruitTree FindFruitTreeWithFruit(out float distance)
        {
            FruitTree best = null;
            distance = float.MaxValue;
            var land = LandMap.Instance;
            Vector2 from = transform.position;
            foreach (var tree in FindObjectsByType<FruitTree>(FindObjectsSortMode.None))
            {
                if (!tree.InWorld || tree.Reserved || tree.FruitCount == 0 || !tree.CanShake) continue;
                if (land != null && !land.SegmentOnLand(from, tree.transform.position)) continue;
                float d = BunniSpace.WorldDistance(from, tree.transform.position);
                if (d < distance) { distance = d; best = tree; }
            }
            return best;
        }
    }
}
