using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>What a worker is visibly doing, for animation and UI to read.</summary>
    public enum WorkerActivity
    {
        Following,
        WalkingToNode,
        Harvesting,
        CarryingHome,
        WalkingToFood,
        Eating,
        Asleep,
        Waiting,
    }

    /// <summary>A species' speed against one resource — a row of game.xml's &lt;modifiers&gt;.</summary>
    [Serializable]
    public struct SpeedModifier
    {
        public NodeResource resource;
        public int speed;
    }

    /// <summary>
    /// A capybara at work. Runs the original's job machine, one step per 30 Hz frame:
    ///
    ///   FindWood ─► GetWood ─► HarvestWood ─► TakeWoodToMill ─► FindWood
    ///
    /// FindWood searches every 61 frames — food if hungry, otherwise its workplace's resource, always
    /// inside the workplace boundary. Finding nothing, it walks to a nap spot beside the workplace and
    /// sleeps, dreaming of whatever it lacks. GetWood walks to the target at the species' speed for
    /// that resource. HarvestWood swings on the original's frame timing, each hit taking
    /// 0.1 × speed / 3 off the node. TakeWoodToMill carries the value home.
    ///
    /// Hunger starts at 1500 and drains one per frame only while walking to a target, harvesting, or
    /// walking to the nap spot. Dropping below 1000 interrupts whatever it was doing.
    ///
    /// Rules: docs/BUNNI_GROUND_TRUTH.md.
    /// </summary>
    public class Worker : MonoBehaviour
    {
        enum State { Following, FindWood, GetWood, HarvestWood, TakeWoodToMill }

        /// <summary>How near a station a dropped worker must land to take the job.</summary>
        public const float DropOnStationRadius = 2.2f;

        [Header("Species (game.xml <animals>; the capybara stands in for the bunni)")]
        [SerializeField] string speciesName = "Capybara";
        [SerializeField] int normalSpeed = BunniRules.CapybaraSpeed;
        [SerializeField] float foodRate = BunniRules.CapybaraFoodRate;
        [SerializeField] List<SpeedModifier> modifiers = new();


        State _state = State.Following;
        FrameClock _clock;

        Workplace _workplace;
        Harvestable _target;
        // A fruit tree target: chopped for wood when a mill runs out of forest, or shaken for one
        // fruit by a hungry worker (mShake).
        FruitTree _tree;
        bool _shake;
        bool _carryingNoBonus;
        int _carrying;
        int _hunger = BunniRules.StartingHunger;
        bool _hungry;

        // FindWood
        int _searchCounter;
        Vector2 _napOffset;
        bool _asleep;
        bool _dreamStarted;

        // HarvestWood
        int _swingFrame;
        int _swingFrames = 15;
        int _hitFrame = 3;

        Follower _follower;

        public bool IsAssigned => _workplace != null;
        public bool IsAsleep => _asleep;
        public bool IsHungry => _hungry;
        public int Hunger => _hunger;
        public Workplace Workplace => _workplace;
        public Harvestable CurrentTarget => _target;
        public FruitTree CurrentTree => _tree;
        public bool IsShaking => _tree != null && _shake;
        public string SpeciesName => speciesName;

        /// <summary>0..1 through the current swing, for the tool animation to follow.</summary>
        public float SwingPhase => _state == State.HarvestWood ? (float)_swingFrame / _swingFrames : 0f;

        /// <summary>Fired on the hit frame of each swing, with the node or fruit tree struck.</summary>
        public event Action<Component> Struck;

        public WorkerActivity Activity
        {
            get
            {
                switch (_state)
                {
                    case State.Following: return WorkerActivity.Following;
                    case State.FindWood: return _asleep ? WorkerActivity.Asleep : WorkerActivity.Waiting;
                    case State.GetWood: return TargetIsFood || IsShaking ? WorkerActivity.WalkingToFood : WorkerActivity.WalkingToNode;
                    case State.HarvestWood: return TargetIsFood ? WorkerActivity.Eating : WorkerActivity.Harvesting;
                    case State.TakeWoodToMill: return WorkerActivity.CarryingHome;
                    default: return WorkerActivity.Waiting;
                }
            }
        }

        bool TargetIsFood => _target != null && _target.Family == NodeFamily.Flower;

        void Awake() => _follower = GetComponent<Follower>();

        /// <summary>The species' speed against a resource: its modifier if it has one, else its normal speed.</summary>
        /// <summary>Restores a saved worker's hunger.</summary>
        public void RestoreHunger(int hunger)
        {
            _hunger = hunger;
            _hungry = _hunger < BunniRules.HungryBelow;
        }

        public int SpeedFor(NodeResource resource)
        {
            foreach (var m in modifiers) if (m.resource == resource) return m.speed;
            return normalSpeed;
        }

        // ---------------------------------------------------------------- assignment

        public void AssignTo(Workplace workplace)
        {
            ReleaseTarget();
            _workplace = workplace;
            if (_follower != null) _follower.PlaceInWorld();   // leaves the inventory trail
            EnterFindWood();
        }

        public void Unassign()
        {
            ReleaseTarget();
            if (_workplace != null) _workplace.Release(this);
            _workplace = null;
            _asleep = false;
            _state = State.Following;
            if (_follower != null) _follower.PickUpIntoInventory();
        }

        /// <summary>
        /// Dropped by the player. If it landed on a station with room it takes the job; otherwise it
        /// snaps back to the king's feet — a worker is either following you or working.
        /// </summary>
        void OnDropped()
        {
            var pete = PiratePete.Near(transform.position);
            if (pete != null)
            {
                Unassign();
                ReturnToKing();
                pete.OfferFor(gameObject, null);
                return;
            }

            var best = StationAt(transform.position, out var full);
            if (best != null && best.TryAssign(this)) return;
            Unassign();
            ReturnToKing();

            // Dropped on a station with no room: say why it came back, rather than nothing.
            if (full != null)
            {
                var name = full.TryGetComponent<Placeable>(out var p) ? StoryText.Name(p.DisplayName) : "workplace";
                Hints.Show($"That {name} is full. It has room for {full.MaxWorkers} capybaras.");
            }
        }

        /// <summary>
        /// The planted station with room that a capybara dropped at <paramref name="at"/> would
        /// join, or null; <paramref name="full"/> is the nearest full one in reach, for the reason why.
        /// </summary>
        public static Workplace StationAt(Vector2 at, out Workplace full)
        {
            Workplace best = null;
            full = null;
            float bestDistance = float.MaxValue, fullDistance = float.MaxValue;
            foreach (var w in FindObjectsByType<Workplace>(FindObjectsSortMode.None))
            {
                var placeable = w.GetComponent<Placeable>();
                if (placeable != null && !placeable.Placed) continue;   // a station still in the trail
                float d = Vector2.Distance(w.transform.position, at);
                if (d > DropOnStationRadius) continue;
                if (!w.HasRoom)
                {
                    if (d < fullDistance) { full = w; fullDistance = d; }
                    continue;
                }
                if (d >= bestDistance) continue;
                best = w;
                bestDistance = d;
            }
            return best;
        }

        /// <summary>Picked up by the player: drop whatever job it had.</summary>
        void OnPickedUp()
        {
            ReleaseTarget();
            if (_workplace != null) _workplace.Release(this);
            _workplace = null;
            _asleep = false;
            _state = State.Following;
        }

        void ReturnToKing()
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king == null) return;
            var behind = (Vector2)king.transform.position + new Vector2(-0.9f, -0.5f);
            transform.position = new Vector3(behind.x, behind.y, transform.position.z);
        }

        void ReleaseTarget()
        {
            if (_target != null) _target.Reserved = false;
            _target = null;
            if (_tree != null) _tree.Reserved = false;
            _tree = null;
            _shake = false;
        }

        bool HasTarget => _target != null || _tree != null;

        /// <summary>The target is gone or no longer in the world.</summary>
        bool TargetLost => _tree != null ? !_tree.InWorld : (_target == null || !_target.InWorld);

        Transform TargetTransform => _tree != null ? _tree.transform : _target.transform;

        // ---------------------------------------------------------------- the frame loop

        void Update()
        {
            ShowHungerThought();
            if (_state == State.Following || _workplace == null) return;

            int frames = _clock.Consume(Time.deltaTime);
            for (int i = 0; i < frames && _workplace != null; i++)
            {
                switch (_state)
                {
                    case State.FindWood: TickFindWood(); break;
                    case State.GetWood: TickGetWood(); break;
                    case State.HarvestWood: TickHarvestWood(); break;
                    case State.TakeWoodToMill: TickTakeWoodToMill(); break;
                }
            }
        }

        // ---------------------------------------------------------------- the hungry dream

        WorldBubble _thought;

        /// <summary>
        /// Asleep because it's hungry: a little garden in a thought bubble over its head, so you
        /// can see at a glance what it needs. Tap it and Old Yuzu says so in words.
        /// </summary>
        void ShowHungerThought()
        {
            bool dreaming = _asleep && _hungry && _workplace != null;
            if (dreaming == (_thought != null)) return;
            if (!dreaming) { _thought.Pop(); _thought = null; return; }
            var catalog = ItemCatalog.Instance;
            var flowers = catalog != null ? catalog.Icon("Medium Posie Garden") : null;
            _thought = WorldBubble.Thought(transform, flowers, 0.75f, () =>
                Hints.Show("This capybara is too hungry to work. Plant a garden near its workplace and it will wake up for a snack."));
        }

        void OnDisable()
        {
            if (_thought != null) { _thought.Pop(); _thought = null; }
        }

        // ---------------------------------------------------------------- hunger

        /// <summary>One active frame of hunger. Crossing below 1000 drops everything to go and eat.</summary>
        void GetHungry()
        {
            _hunger--;
            if (_hunger >= BunniRules.HungryBelow || _hungry) return;
            _hungry = true;
            ReleaseTarget();
            EnterFindWood();
        }

        void ResolveHunger(int amount)
        {
            bool wasHungry = _hungry;
            _hunger += amount;   // no cap: a rose banks a worker far past full
            if (_hunger >= BunniRules.HungryBelow) _hungry = false;
            if (wasHungry && !_hungry) Fed?.Invoke(this);
        }

        /// <summary>Raised when a hungry worker has eaten its fill.</summary>
        public static event Action<Worker> Fed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetFedSession() => Fed = null;

        /// <summary>A visiting friend hands this worker a snack: as good as a garden's worth of flowers.</summary>
        public void FeedFromFriend()
        {
            if (!_hungry) return;
            ResolveHunger(BunniRules.StartingHunger);
        }

        // ---------------------------------------------------------------- FindWood

        void EnterFindWood()
        {
            _state = State.FindWood;
            _searchCounter = 0;
            _asleep = false;
            _dreamStarted = false;

            // The nap spot: 70 units from the workplace at a random angle through the front half.
            float angle = UnityEngine.Random.value * Mathf.PI;
            _napOffset = BunniSpace.ToUnity(new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)) * BunniRules.SleepSpotDistance);
        }

        void TickFindWood()
        {
            if (_searchCounter == 0)
            {
                Search();
                if (HasTarget) { EnterGetWood(); return; }
            }

            var napSpot = (Vector2)_workplace.transform.position + _napOffset;
            if (BunniSpace.WorldDistance(transform.position, napSpot) < 3f)
            {
                // Asleep beside the mill: no hunger drain, dreaming of what it lacks.
                _asleep = true;
                if (!_dreamStarted)
                {
                    _dreamStarted = true;
                    RaiseDreamHint();
                }
            }
            else
            {
                _asleep = false;
                _dreamStarted = false;
                GetHungry();
                if (_state != State.FindWood) return;   // hunger restarted the search
                var pos = (Vector2)transform.position;
                BunniSpace.Step(ref pos, napSpot, normalSpeed);
                MoveTo(pos);
            }

            if (++_searchCounter > BunniRules.SearchIntervalFrames - 1) _searchCounter = 0;
        }

        /// <summary>
        /// The original's FindWood search. Hungry: the nearest food — a child flower or fallen fruit,
        /// or a fruit tree with fruit to shake — else a parent flower. Otherwise the workplace's own
        /// resource, children before parents, and for a sawmill with no forest left, a fruit tree.
        /// </summary>
        void Search()
        {
            ReleaseTarget();
            Vector2 from = transform.position;
            if (_hungry)
            {
                _workplace.FindFood(from, reserve: true, out _target, out _tree);
                _shake = _tree != null;
                return;
            }

            _target = _workplace.Find(_workplace.WorkFamily, from, reserve: true);
            if (_target == null && _workplace.Accepts == ResourceKind.Wood)
                _tree = _workplace.FindFruitTree(from, needFruit: false, reserve: true);
        }

        void RaiseDreamHint()
        {
            // The opening explains its own first hungry nap, with a movie and Old Yuzu.
            if (Attention.TutorialRunning) return;
            string who = "A " + StoryText.Name(speciesName);
            if (_hungry && !AnyFlowers())
                Hints.Show(who + " is hungry and fell asleep. There's no garden to eat from: plant one near its workplace.");
            else if (_hungry)
                Hints.Show(who + " ate the last flower in the garden. Plant bigger gardens for your hungry workers.");
            else if (_workplace.Accepts == ResourceKind.Wood)
                Hints.Show(who + " chopped down the last tree in a forest. Too many workers on one forest will strip it bare.");
            else
                Hints.Show(who + " dug out the last rock in a hill. With no rocks in reach, the mine will close.");
        }

        /// <summary>Is any flower garden planted on the island at all (eaten bare or not)?</summary>
        static bool AnyFlowers()
        {
            foreach (var tag in FindObjectsByType<CatalogItem>(FindObjectsSortMode.None))
            {
                var n = tag.StoreName ?? "";
                if (!n.Contains("Garden") || n.Contains("Vegetable")) continue;
                if (tag.TryGetComponent<Placeable>(out var p) && p.Placed) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- GetWood

        void EnterGetWood()
        {
            _state = State.GetWood;
            _asleep = false;
        }

        /// <summary>Where a worker stands to work a node: 5 world units behind its centre.</summary>
        Vector2 WorkSpot(Transform node) => (Vector2)node.position + BunniSpace.ToUnity(new Vector2(0f, 5f));

        void TickGetWood()
        {
            GetHungry();
            if (_state != State.GetWood) return;
            if (TargetLost) { ReleaseTarget(); EnterFindWood(); return; }

            // The path is walked at the species' speed for this resource, not its normal speed.
            // Fruit trees have no modifier: normal speed.
            int speed = _tree != null ? normalSpeed : SpeedFor(_target.Resource);
            var pos = (Vector2)transform.position;
            bool arrived = BunniSpace.Step(ref pos, WorkSpot(TargetTransform), speed);
            MoveTo(pos);
            if (arrived) EnterHarvestWood();
        }

        // ---------------------------------------------------------------- HarvestWood

        void EnterHarvestWood()
        {
            _state = State.HarvestWood;
            BunniRules.Swing(_tree != null ? NodeFamily.Tree : _target.Family, out _swingFrames, out _hitFrame);
            _swingFrame = 0;

            // Starting to chop a fruit tree (not shake it) drops its fruit and stops it fruiting.
            if (_tree != null && !_shake) _tree.BeginChop();
        }

        void TickHarvestWood()
        {
            GetHungry();
            if (_state != State.HarvestWood) return;
            if (TargetLost) { ReleaseTarget(); EnterFindWood(); return; }

            _swingFrame++;
            if (_swingFrame >= _swingFrames) _swingFrame = 0;
            if (_swingFrame != _hitFrame) return;

            if (_tree != null) { StrikeTree(); return; }

            var node = _target;
            bool food = node.Family == NodeFamily.Flower;
            int value = node.Value;
            Struck?.Invoke(node);

            if (!node.Hit(BunniRules.ScalePerHit(SpeedFor(node.Resource)))) return;

            _target = null;
            if (food)
            {
                // Eating restores the flower's price × this species' food rate.
                ResolveHunger(Mathf.RoundToInt(BunniRules.Price(node.Resource) * foodRate));
                EnterFindWood();
                return;
            }

            _carrying = value;
            _carryingNoBonus = false;
            _state = State.TakeWoodToMill;
        }

        /// <summary>
        /// A hit on a fruit tree. Shaking knocks down one fruit and goes looking for food again;
        /// chopping takes 0.1 × 0.5 off per blow, and a felled tree is 500 wood.
        /// </summary>
        void StrikeTree()
        {
            var tree = _tree;
            Struck?.Invoke(tree);

            if (_shake)
            {
                tree.RemoveOne();
                ReleaseTarget();
                EnterFindWood();
                return;
            }

            if (!tree.Hit(0.1f * BunniRules.FruitTreeHarvestSpeed)) return;

            _tree = null;
            _carrying = BunniRules.FruitTreeWoodValue;
            _carryingNoBonus = true;   // the original sets 500 flat, no Legendary doubling
            _state = State.TakeWoodToMill;
        }

        // ---------------------------------------------------------------- TakeWoodToMill

        /// <summary>Carrying home. Walks at normal speed and — as in the original — drains no hunger.</summary>
        void TickTakeWoodToMill()
        {
            var pos = (Vector2)transform.position;
            bool arrived = BunniSpace.Step(ref pos, WorkSpot(_workplace.transform), normalSpeed);
            MoveTo(pos);
            if (!arrived) return;

            _workplace.Deposit(_carrying, legendaryBonus: !_carryingNoBonus);
            _carrying = 0;
            _carryingNoBonus = false;
            EnterFindWood();
        }

        // ---------------------------------------------------------------- movement

        void MoveTo(Vector2 pos)
        {
            var dx = pos.x - transform.position.x;
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);

            var body = transform.Find("Sprite");
            if (body == null || Mathf.Abs(dx) < 0.0001f) return;
            var s = body.localScale;
            // the pictures face left: mirror them to walk right
            s.x = Mathf.Abs(s.x) * (dx > 0f ? -1f : 1f);
            body.localScale = s;
        }
    }
}
