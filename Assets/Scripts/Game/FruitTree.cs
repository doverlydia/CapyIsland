using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// An apple, pear or plum tree (MapItems/FruitTrees). Planted, it grows from 0.2 to full size,
    /// then keeps growing fruit — up to five, each frame a small chance of one more.
    ///
    /// **Tap it** and it shakes: every fruit comes down. Each one lands as food for hungry workers
    /// (apples count as posies, pears as tulips, plums as roses) that rots if nobody eats it — or,
    /// sometimes, as a treasure chest of wood, gold or gems; once a minute the first fruit down is a
    /// free sapling of the same tree, and one time in a hundred a bubble with a random item.
    ///
    /// **Hungry workers** with no flowers nearby shake it too, one fruit at a time, then eat what fell.
    ///
    /// **Out of trees**, a sawmill's workers will chop it down: it drops all its fruit, stops
    /// fruiting, and — if they finish the job — is gone for good, worth 500 wood, and lets loose a
    /// monster, a deer or a bomb. Left alone it slowly heals and fruits again.
    ///
    /// Rules: MapItems/FruitTrees (FruitTree, Grow, GrowFruit, Shake, Chopped). Numbers in
    /// <see cref="BunniRules"/>. Everything juicy here — the sway, the ripening, the bouncing fruit,
    /// the splash — is presentation on top of those rules.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class FruitTree : MonoBehaviour, ITappable
    {
        enum Phase { Unplanted, Grow, GrowFruit, Shake, Chopped }

        [SerializeField] FruitKind kind = FruitKind.Apple;

        [Tooltip("Unity scale of the art at full size. Presentation only.")]
        [SerializeField] float artScale = 1.55f;

        [Header("Fruit look")]
        [Tooltip("Where fruit hang, in the tree sprite's local space: centre and radii of an ellipse over the canopy.")]
        [SerializeField] Vector2 canopyCentre = new Vector2(0f, 1.35f);
        [SerializeField] Vector2 canopyRadii = new Vector2(0.55f, 0.36f);
        [Tooltip("Size of a fruit on the branch, in the tree sprite's local space.")]
        [SerializeField] float fruitOnTreeSize = 0.36f;
        [Tooltip("Unity scale of a fallen fruit's art at node scale 1.")]
        [SerializeField] float fruitOnGroundScale = 0.55f;
        [SerializeField] float ripenSeconds = 4f;

        static float _saplingReadyAt;
        static bool _anyShaken, _shakeHinted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSaplingTimer()
        {
            _saplingReadyAt = BunniRules.SaplingIntervalSeconds;
            _anyShaken = _shakeHinted = false;
        }

        class Fruit
        {
            public Transform pivot;       // at the stem, so the fruit dangles from it
            public SpriteRenderer sprite;
            public float bornAt;
            public float phase;
        }

        readonly List<Fruit> _fruit = new();

        Phase _phase = Phase.Unplanted;
        FrameClock _clock;
        float _scale;
        int _shakeCounter;
        bool _canShake;
        bool _canHarvest;
        Transform _body;
        SpriteRenderer _bodyRenderer;
        Sway _sway;
        Placeable _placeable;

        public FruitKind Kind => kind;
        public string StoreName => BunniRules.FruitTreeStoreName(kind);
        public float Scale => _scale;
        public int FruitCount => _fruit.Count;

        /// <summary>Grown: workers may chop it (mCanHarvest). Stays true once set.</summary>
        public bool CanHarvest => _canHarvest;

        /// <summary>Can be shaken: grown and not being chopped (mCanShake).</summary>
        public bool CanShake => _canShake;

        public bool Reserved { get; set; }
        public bool InWorld => isActiveAndEnabled && _phase != Phase.Unplanted;

        void Awake()
        {
            _body = transform.Find("Sprite");
            _bodyRenderer = _body != null ? _body.GetComponent<SpriteRenderer>() : null;
            _sway = GetComponent<Sway>();
            _placeable = GetComponent<Placeable>();
        }

        // ---------------------------------------------------------------- planting

        void OnPlacedInWorld()
        {
            // Grow.Enter: start small and grow to exactly full size.
            _phase = Phase.Grow;
            _scale = BunniRules.SpawnScale;
            _canShake = false;
            _canHarvest = false;
            Reserved = false;
            ApplyScale();
        }

        /// <summary>A saved tree: grown (or regrowing after a chop) with its fruit back on the branches.</summary>
        public void RestoreGrown(float scale, int fruit)
        {
            _scale = Mathf.Clamp(scale, BunniRules.SpawnScale, 1f);
            _canHarvest = _scale >= 1f;
            _canShake = _scale >= 1f;
            _phase = _scale >= 1f ? Phase.GrowFruit : Phase.Grow;
            ApplyScale();
            ClearFruitVisuals();
            for (int i = 0; i < fruit && i < BunniRules.MaxFruitOnTree; i++) AddFruit();
        }

        void OnReturnedToBubble()
        {
            _phase = Phase.Unplanted;
            _canShake = _canHarvest = false;
            Reserved = false;
            ClearFruitVisuals();
            transform.localScale = Vector3.one;
        }

        // ---------------------------------------------------------------- per frame

        void Update()
        {
            if (_phase == Phase.Unplanted) return;
            int frames = _clock.Consume(Time.deltaTime);
            for (int i = 0; i < frames; i++) TickFrame();
        }

        void TickFrame()
        {
            switch (_phase)
            {
                case Phase.Grow:
                    _scale += BunniRules.FruitTreeGrowthPerFrame;
                    if (_scale > 1f)
                    {
                        _scale = 1f;
                        _canHarvest = true;
                        _canShake = true;
                        _phase = Phase.GrowFruit;
                    }
                    ApplyScale();
                    break;

                case Phase.GrowFruit:
                    if (_fruit.Count < BunniRules.MaxFruitOnTree && Random.value < BunniRules.FruitChancePerFrame(kind))
                    {
                        AddFruit();
                        // The fruit-tree card's line, for players who never picked one up.
                        if (_fruit.Count == BunniRules.MaxFruitOnTree && !_anyShaken && !_shakeHinted)
                        {
                            _shakeHinted = true;
                            Hints.Island("Tap fruit trees to shake them. All sorts of goodies fall out, sometimes even gems.");
                        }
                    }
                    break;

                case Phase.Shake:
                    if (++_shakeCounter >= BunniRules.ShakeFrames) _phase = Phase.GrowFruit;
                    break;

                case Phase.Chopped:
                    _scale += BunniRules.ChoppedRegrowPerFrame;
                    if (_scale > 1f)
                    {
                        _scale = 1f;
                        _canHarvest = true;
                        _canShake = true;
                        _phase = Phase.GrowFruit;
                    }
                    ApplyScale();
                    break;
            }
        }

        void ApplyScale() => transform.localScale = Vector3.one * (artScale * Mathf.Max(0.001f, _scale));

        void EnterShake()
        {
            _phase = Phase.Shake;
            _shakeCounter = 0;
            PlayShake();
        }

        // ---------------------------------------------------------------- shaking

        /// <summary>The player tapped the tree (FruitTree.Click): shake down every fruit.</summary>
        public void PlayerShake()
        {
            _anyShaken = true;
            Hints.Island("You shook a <b>" + StoryText.Name(StoreName) + "</b>");
            if (!_canShake) { if (_sway != null) _sway.Kick(3f, 0.3f, 40f); return; }
            EnterShake();
            RemoveFruit();
        }

        /// <summary>A hungry worker shakes loose a single fruit, which lands as food (RemoveOne).</summary>
        public void RemoveOne()
        {
            if (!_canShake || _fruit.Count == 0) return;
            EnterShake();
            var fruit = _fruit[_fruit.Count - 1];
            _fruit.RemoveAt(_fruit.Count - 1);
            DropFood(fruit, GroundSpot());
        }

        /// <summary>
        /// Everything on the branches comes down (RemoveFruit). Per fruit: a sapling if one is due,
        /// else 1% a random item, else a chest at this tree's odds, else food.
        /// </summary>
        void RemoveFruit()
        {
            var catalog = ItemCatalog.Instance;
            foreach (var fruit in _fruit)
            {
                var spot = GroundSpot();
                if (Time.time >= _saplingReadyAt)
                {
                    _saplingReadyAt = Time.time + BunniRules.SaplingIntervalSeconds;
                    DropBubble(fruit, catalog != null ? catalog.MakeStore(StoreName, spot) : null);
                }
                else if (Random.value < BunniRules.RandomStoreChance)
                {
                    DropBubble(fruit, catalog != null ? catalog.MakeRandomStore(spot) : null);
                }
                else if (Random.value < BunniRules.ChestChance(kind, out int tier))
                {
                    DropChest(fruit, spot, tier);
                }
                else
                {
                    DropFood(fruit, spot);
                }
            }
            _fruit.Clear();
        }

        /// <summary>Where a fruit lands: up to 50 either side and up to 60 in front of the trunk.</summary>
        Vector2 GroundSpot()
        {
            var offset = BunniSpace.ToUnity(new Vector2(Random.value * 100f - 50f, -Random.value * 60f));
            return (Vector2)transform.position + offset;
        }

        // ---------------------------------------------------------------- chopping

        /// <summary>A worker starts chopping (Chopped.Enter): all fruit drops and it stops fruiting.</summary>
        public void BeginChop()
        {
            RemoveFruit();
            _canShake = false;
            if (Mathf.Approximately(_scale, 1f)) _scale = BunniRules.ChoppedStartScale;
            _phase = Phase.Chopped;
            ApplyScale();
        }

        /// <summary>One axe blow. True when it felled the tree, which is then gone for good.</summary>
        public bool Hit(float amount)
        {
            _scale -= amount;
            if (_sway != null) _sway.Kick(6f, 0.3f);
            if (_scale >= 0f) { ApplyScale(); return false; }

            _scale = 0f;
            Reserved = false;
            Fell();
            return true;
        }

        void Fell()
        {
            Vector2 at = transform.position;
            Sfx.PlayAt(SfxKind.Fell, at);
            var fx = HarvestEffects.Instance;
            if (fx != null)
            {
                fx.Felled(at, NodeFamily.Tree);
                fx.FruitPop(at + Vector2.up * 1.2f, GameArt.Juice(kind));
            }

            // A third each: a monster, a deer, or a bomb comes out of the stump.
            float r = Random.value;
            if (r < 0.33f)
            {
                Hints.Island("A worker ran out of trees and chopped down a fruit tree! Uh oh, a zombie capybara climbed out!");
                Hazards.Spawn(HazardKind.Monster, at);
            }
            else if (r < 0.66f)
            {
                Hints.Island("A worker ran out of trees and chopped down a fruit tree! Uh oh, a guinea pig jumped out!");
                Hazards.Spawn(HazardKind.Deer, at);
            }
            else
            {
                Hints.Island("A worker ran out of trees and chopped down a fruit tree, and a fizz bomb fell out! Stand back!");
                Hazards.Spawn(HazardKind.Bomb, at);
            }

            Destroy(gameObject);
        }

        // ---------------------------------------------------------------- fruit visuals

        void AddFruit()
        {
            if (_body == null) return;
            var art = GameArt.Instance;

            var pivot = new GameObject("Fruit").transform;
            pivot.SetParent(_body, false);
            pivot.localPosition = FreeBranchSpot();

            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(pivot, false);
            var sr = spriteGo.AddComponent<SpriteRenderer>();
            sr.sprite = art != null ? art.Fruit(kind) : null;
            // Hang below the stem: the sprite pivots at its base, so drop it by its height.
            float h = sr.sprite != null ? sr.sprite.bounds.size.y : 1f;
            spriteGo.transform.localPosition = new Vector3(0f, -h * 0.85f, 0f);

            var fruit = new Fruit { pivot = pivot, sprite = sr, bornAt = Time.time, phase = Random.value * 6.28f };
            _fruit.Add(fruit);

            // Pops in with a springy overshoot, green and hard, then ripens.
            float size = fruitOnTreeSize / Mathf.Max(0.01f, h);
            pivot.localScale = Vector3.zero;
            pivot.DOScale(size, 0.55f).SetEase(Ease.OutBack, 2.2f);
            if (_sway != null) _sway.Kick(1.5f, 0.25f, 30f);
        }

        /// <summary>A spot on the canopy not too close to another fruit.</summary>
        Vector3 FreeBranchSpot()
        {
            Vector2 best = canopyCentre;
            float bestGap = -1f;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                var r = Random.insideUnitCircle;
                var p = canopyCentre + new Vector2(r.x * canopyRadii.x, r.y * canopyRadii.y);
                float gap = float.MaxValue;
                foreach (var f in _fruit) gap = Mathf.Min(gap, Vector2.Distance(p, f.pivot.localPosition));
                if (gap > bestGap) { bestGap = gap; best = p; }
                if (gap > fruitOnTreeSize * 1.3f) break;
            }
            return best;
        }

        void LateUpdate()
        {
            if (_fruit.Count == 0 || _bodyRenderer == null) return;

            int order = _bodyRenderer.sortingOrder + 1;
            float t = Time.time;
            foreach (var f in _fruit)
            {
                if (f.sprite == null) continue;
                f.sprite.sortingOrder = order;

                // Dangle from the stem.
                float swing = Mathf.Sin(t * 2.1f + f.phase) * 5f + Mathf.Sin(t * 3.7f + f.phase * 2f) * 2f;
                f.pivot.localRotation = Quaternion.Euler(0f, 0f, swing);

                // Ripen from hard green to full colour.
                float ripe = Mathf.Clamp01((t - f.bornAt) / Mathf.Max(0.01f, ripenSeconds));
                f.sprite.color = Color.Lerp(new Color(0.72f, 0.92f, 0.55f), Color.white, ripe * ripe * (3f - 2f * ripe));
            }
        }

        void PlayShake()
        {
            Sfx.PlayAt(SfxKind.Rustle, transform.position);
            if (_sway != null) _sway.Kick(9f, 0.5f, 34f);
            if (_body != null)
            {
                _body.DOKill(complete: true);
                _body.DOPunchScale(new Vector3(0.08f, -0.06f, 0f), 0.4f, 8, 0.7f);
            }

            var fx = HarvestEffects.Instance;
            if (fx == null || _bodyRenderer == null) return;
            var b = _bodyRenderer.bounds;
            fx.Rustle(new Vector2(b.center.x, b.min.y + b.size.y * 0.7f), b.size.x * 0.8f);
            int dir = Wind.Instance != null ? Wind.Instance.Direction : 1;
            for (int i = 0; i < 4; i++)
                fx.BlowLeaf(new Vector2(b.center.x + Random.Range(-0.4f, 0.4f) * b.size.x, b.min.y + b.size.y * Random.Range(0.55f, 0.9f)),
                    dir, new Color(0.45f, 0.68f, 0.32f));
        }

        /// <summary>Takes a fruit off the branch and returns where it was, in the world.</summary>
        Vector3 Detach(Fruit fruit)
        {
            if (fruit.pivot == null) return transform.position;
            var from = fruit.sprite != null ? fruit.sprite.transform.position : fruit.pivot.position;
            fruit.pivot.DOKill();
            Destroy(fruit.pivot.gameObject);
            return from;
        }

        /// <summary>A fruit lands as food, bouncing down from its branch and splashing on impact.</summary>
        void DropFood(Fruit fruit, Vector2 spot)
        {
            var art = GameArt.Instance;
            var from = Detach(fruit);
            var sprite = art != null ? art.Fruit(kind) : null;
            var node = Harvestable.SpawnFruit(BunniRules.FruitFood(kind), sprite, fruitOnGroundScale, spot,
                transform.parent, art != null ? art.shadowTemplate : null);

            // The node is live at once — the rules place it instantly — but its art arrives by air.
            var nodeBody = node.transform.Find("Sprite");
            var nodeRenderer = nodeBody != null ? nodeBody.GetComponent<SpriteRenderer>() : null;
            if (nodeRenderer != null) nodeRenderer.enabled = false;

            var juice = GameArt.Juice(kind);
            FlyDown(sprite, from, spot, fruitOnGroundScale, () =>
            {
                if (nodeRenderer != null) nodeRenderer.enabled = true;
                if (nodeBody != null) nodeBody.DOPunchScale(new Vector3(0.35f, -0.3f, 0f), 0.3f, 5, 0.8f);
                var fx = HarvestEffects.Instance;
                if (fx != null) fx.FruitPop(spot + Vector2.up * 0.1f, juice);
                Sfx.PlayAt(SfxKind.Plop, spot, 0.9f, 0.18f);
            });
        }

        void DropChest(Fruit fruit, Vector2 spot, int tier)
        {
            var from = Detach(fruit);
            var chest = TreasureChest.Spawn(spot, tier, transform.parent);
            var body = chest.transform.Find("Sprite");
            if (body == null) return;
            var offset = from - (Vector3)spot;
            body.localPosition = offset;
            body.DOLocalJump(Vector3.zero, 0.8f, 1, 0.5f).SetEase(Ease.InQuad)
                .OnComplete(() => body.DOPunchScale(new Vector3(0.12f, -0.12f, 0f), 0.25f, 5, 0.8f));
        }

        void DropBubble(Fruit fruit, Bubble bubble)
        {
            var from = Detach(fruit);
            if (bubble == null) return;
            bubble.ExpireAfterFrames(BunniRules.RandomLifetimeFrames());
            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Twinkle(from, 10);

            // Floats gently down from the branch instead of dropping like fruit.
            var to = bubble.transform.position;
            bubble.transform.position = from;
            bubble.transform.DOMove(to, 0.9f).SetEase(Ease.OutSine);
        }

        /// <summary>A throwaway sprite that arcs from the branch to the ground.</summary>
        void FlyDown(Sprite sprite, Vector3 from, Vector2 to, float scale, TweenCallback landed)
        {
            var go = new GameObject("FallingFruit");
            go.transform.position = from;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 31000;
            go.transform.DOLocalRotate(new Vector3(0f, 0f, Random.Range(-160f, 160f)), 0.45f, RotateMode.FastBeyond360);
            go.transform.DOJump(new Vector3(to.x, to.y, from.z), 0.5f, 1, 0.45f).SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    landed?.Invoke();
                    Destroy(go);
                });
        }

        void ClearFruitVisuals()
        {
            foreach (var f in _fruit)
                if (f.pivot != null) { f.pivot.DOKill(); Destroy(f.pivot.gameObject); }
            _fruit.Clear();
        }

        void OnDestroy()
        {
            if (_body != null) _body.DOKill();
            ClearFruitVisuals();
        }

        // ---------------------------------------------------------------- input

        void OnEnable() => TapTargets.Register(this);
        void OnDisable() => TapTargets.Unregister(this);

        public float TapDistance(Vector2 worldPoint)
        {
            if (!InWorld || _bodyRenderer == null) return -1f;
            var b = _bodyRenderer.bounds;
            if (!b.Contains(new Vector3(worldPoint.x, worldPoint.y, b.center.z))) return -1f;
            // Trees are big; let smaller things in front of them win a tap.
            return Vector2.Distance(b.center, worldPoint) + 1.5f;
        }

        public void Tap() => PlayerShake();

        /// <summary>The planted fruit tree under a world point, if any — for the player's tap.</summary>
        public static FruitTree Pick(Vector2 worldPoint)
        {
            FruitTree best = null;
            float bestDistance = float.MaxValue;
            foreach (var tree in FindObjectsByType<FruitTree>(FindObjectsSortMode.None))
            {
                if (!tree.InWorld || tree._bodyRenderer == null) continue;
                var b = tree._bodyRenderer.bounds;
                if (!b.Contains(new Vector3(worldPoint.x, worldPoint.y, b.center.z))) continue;
                float d = Vector2.Distance(b.center, worldPoint);
                if (d < bestDistance) { bestDistance = d; best = tree; }
            }
            return best;
        }
    }
}
