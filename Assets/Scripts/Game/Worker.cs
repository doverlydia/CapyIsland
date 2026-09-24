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

        /// <summary>Raised with the original's hint lines when a worker runs out of work or food.</summary>
        public static event Action<string> Hint;

        State _state = State.Following;
        FrameClock _clock;

        Workplace _workplace;
        Harvestable _target;
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
        public string SpeciesName => speciesName;

        /// <summary>0..1 through the current swing, for the tool animation to follow.</summary>
        public float SwingPhase => _state == State.HarvestWood ? (float)_swingFrame / _swingFrames : 0f;

        /// <summary>Fired on the hit frame of each swing, with the node struck.</summary>
        public event Action<Harvestable> Struck;

        public WorkerActivity Activity
        {
            get
            {
                switch (_state)
                {
                    case State.Following: return WorkerActivity.Following;
                    case State.FindWood: return _asleep ? WorkerActivity.Asleep : WorkerActivity.Waiting;
                    case State.GetWood: return TargetIsFood ? WorkerActivity.WalkingToFood : WorkerActivity.WalkingToNode;
                    case State.HarvestWood: return TargetIsFood ? WorkerActivity.Eating : WorkerActivity.Harvesting;
                    case State.TakeWoodToMill: return WorkerActivity.CarryingHome;
                    default: return WorkerActivity.Waiting;
                }
            }
        }

        bool TargetIsFood => _target != null && _target.Family == NodeFamily.Flower;

        void Awake() => _follower = GetComponent<Follower>();

        /// <summary>The species' speed against a resource: its modifier if it has one, else its normal speed.</summary>
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
            Workplace best = null;
            float bestDistance = float.MaxValue;
            foreach (var w in FindObjectsByType<Workplace>(FindObjectsSortMode.None))
            {
                if (!w.HasRoom) continue;
                var placeable = w.GetComponent<Placeable>();
                if (placeable != null && !placeable.Placed) continue;   // a station still in the trail
                float d = Vector2.Distance(w.transform.position, transform.position);
                if (d > DropOnStationRadius || d >= bestDistance) continue;
                best = w;
                bestDistance = d;
            }

            if (best != null && best.TryAssign(this)) return;
            Unassign();
            ReturnToKing();
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
        }

        // ---------------------------------------------------------------- the frame loop

        void Update()
        {
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
            _hunger += amount;   // no cap: a rose banks a worker far past full
            if (_hunger >= BunniRules.HungryBelow) _hungry = false;
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
                var family = _hungry ? NodeFamily.Flower : _workplace.WorkFamily;
                _target = _workplace.Find(family, transform.position, reserve: true);
                if (_target != null) { EnterGetWood(); return; }
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

        void RaiseDreamHint()
        {
            string who = "A " + speciesName;
            if (_hungry)
                Hint?.Invoke(who + " ate the last flower in the garden. I guess you need to plant bigger flower gardens for your hungry workers.");
            else if (_workplace.Accepts == ResourceKind.Wood)
                Hint?.Invoke("Uh oh... I hear a " + speciesName + " chopped down the last tree in a forest. Too many workers leads to overharvesting.");
            else
                Hint?.Invoke("I hear a " + speciesName + " mined the last rock in a hill. Soon the mine will collapse.");
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
            if (_target == null || !_target.InWorld) { ReleaseTarget(); EnterFindWood(); return; }

            // The path is walked at the species' speed for this resource, not its normal speed.
            var pos = (Vector2)transform.position;
            bool arrived = BunniSpace.Step(ref pos, WorkSpot(_target.transform), SpeedFor(_target.Resource));
            MoveTo(pos);
            if (arrived) EnterHarvestWood();
        }

        // ---------------------------------------------------------------- HarvestWood

        void EnterHarvestWood()
        {
            _state = State.HarvestWood;
            BunniRules.Swing(_target.Family, out _swingFrames, out _hitFrame);
            _swingFrame = 0;
        }

        void TickHarvestWood()
        {
            GetHungry();
            if (_state != State.HarvestWood) return;
            if (_target == null || !_target.InWorld) { ReleaseTarget(); EnterFindWood(); return; }

            _swingFrame++;
            if (_swingFrame >= _swingFrames) _swingFrame = 0;
            if (_swingFrame != _hitFrame) return;

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

            _workplace.Deposit(_carrying);
            _carrying = 0;
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
            s.x = Mathf.Abs(s.x) * (dx < 0f ? -1f : 1f);
            body.localScale = s;
        }
    }
}
