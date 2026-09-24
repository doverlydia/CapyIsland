using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A capybara that works. Assigned to a <see cref="Workplace"/>, it walks to a node, harvests,
    /// carries the load back, and the yield is credited on arrival — the same loop the original runs.
    ///
    /// Timing is calibrated to the play-test: one bunni on pine delivered +3 wood on a ~45s median
    /// cycle. `speed` is the original's relative rate (bunni 3, fox 6, panda 2, and x8/x12 species
    /// bonuses on their specialisms), so harvest time is <see cref="HarvestSecondsAtSpeedOne"/>
    /// divided by speed, and walking makes up the rest of the trip.
    /// </summary>
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

    public class Worker : MonoBehaviour
    {
        /// <summary>Chop/mine seconds for a worker of speed 1. Speed 3 gives ~33s, plus walking ~= 45s.</summary>
        public const float HarvestSecondsAtSpeedOne = 100f;

        /// <summary>Seconds per strike for a worker of speed 1. Speed 3 gives a strike every ~2.7s.</summary>
        public const float SecondsPerStrikeAtSpeedOne = 8f;

        /// <summary>How near a station a dropped worker must land to take the job.</summary>
        public const float DropOnStationRadius = 2.2f;

        enum State { Following, Idle, ToNode, Harvesting, ToWorkplace, ToFood, Eating }

        [Header("Species")]
        [Tooltip("The original's relative rate: bunni 3, pirate 4, panda 2, fox 6, monster 6.")]
        [SerializeField] float speed = 3f;
        [Tooltip("Walking pace in world units per second.")]
        [SerializeField] float moveSpeed = 2.2f;

        [Header("Hunger")]
        [Tooltip("Strikes this worker makes before it needs to eat. 0 disables hunger.")]
                [SerializeField] int tripsBeforeHungry = 12;

        [Tooltip("While asleep, how often to check whether a garden has appeared nearby.")]
        [SerializeField] float wakeCheckSeconds = 0.5f;

        State _state = State.Following;
        FoodSource _food;
        float _eatTimer;
        float _wakeTimer;

        /// <summary>Seconds a meal takes. The original's bunni was back at work about 9s after reaching a garden.</summary>
        const float EatSeconds = 3f;
        Workplace _workplace;
        Harvestable _target;
        float _harvestTimer;
        float _harvestDuration;
        int _carrying;
        int _tripsSinceMeal;
        bool _asleep;

        SpriteRenderer _renderer;

        public bool IsAssigned => _workplace != null;

        /// <summary>What this worker is visibly doing right now.</summary>
        public WorkerActivity Activity
        {
            get
            {
                if (_asleep) return WorkerActivity.Asleep;
                if (_workplace == null) return WorkerActivity.Following;
                return _state switch
                {
                    State.ToNode => WorkerActivity.WalkingToNode,
                    State.Harvesting => WorkerActivity.Harvesting,
                    State.ToWorkplace => WorkerActivity.CarryingHome,
                    State.ToFood => WorkerActivity.WalkingToFood,
                    State.Eating => WorkerActivity.Eating,
                    _ => WorkerActivity.Waiting,
                };
            }
        }

        /// <summary>0..1 through the current chop or mine, for a progress read.</summary>
        public float HarvestProgress =>
            _state == State.Harvesting && _harvestDuration > 0.01f
                ? Mathf.Clamp01(_harvestTimer / _harvestDuration)
                : 0f;

        /// <summary>The node being worked, so a tree can shake when it is struck.</summary>
        public Harvestable CurrentTarget => _target;
        public bool IsAsleep => _asleep;
        public Workplace Workplace => _workplace;

        Follower _follower;

        void Awake()
        {
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _follower = GetComponent<Follower>();
        }

        public void AssignTo(Workplace workplace)
        {
            _workplace = workplace;
            if (_follower != null) _follower.PlaceInWorld();   // leaves the inventory trail
            _state = State.Idle;
            _target = null;
            _carrying = 0;
        }

        public void Unassign()
        {
            if (_workplace != null) _workplace.Release(this);
            _workplace = null;
            _state = State.Following;
            _target = null;
            _carrying = 0;
            if (_follower != null) _follower.PickUpIntoInventory();   // back into the trail
        }

        /// <summary>
        /// Dropped by the player. If it landed on a station that accepts it, it takes the job;
        /// otherwise it rejoins the queue behind the king.
        /// </summary>
        void OnDropped()
        {
            Workplace best = null;
            float bestDistance = float.MaxValue;
            foreach (var w in FindObjectsByType<Workplace>(FindObjectsSortMode.None))
            {
                if (!w.HasRoom) continue;
                float d = Vector2.Distance(w.transform.position, transform.position);
                if (d > DropOnStationRadius || d >= bestDistance) continue;
                best = w;
                bestDistance = d;
            }

            if (best != null && best.TryAssign(this)) return;

            // Not on a station: the drop is refused. A capy is either following you or working, so
            // it snaps back to your feet rather than standing wherever it was let go.
            Unassign();
            ReturnToKing();
        }

        /// <summary>Picked up by the player: drop whatever job it had.</summary>
        void OnPickedUp()
        {
            if (_workplace != null) _workplace.Release(this);
            _workplace = null;
            _target = null;
            _carrying = 0;
            _state = State.Following;
        }

        /// <summary>Feeding wakes a sleeping worker and resets its hunger.</summary>
        public void Feed()
        {
            _tripsSinceMeal = 0;
            _food = null;
            if (!_asleep) return;
            _asleep = false;
            _state = State.Idle;
        }

        void Update()
        {
            if (_asleep) { TickAsleep(); return; }
            // While unassigned the Follower component walks it; the worker just waits.
            if (_workplace == null) return;

            switch (_state)
            {
                case State.Following: return;
                case State.Idle: TickIdle(); break;
                case State.ToNode: TickToNode(); break;
                case State.Harvesting: TickHarvesting(); break;
                case State.ToWorkplace: TickToWorkplace(); break;
                case State.ToFood: TickToFood(); break;
                case State.Eating: TickEating(); break;
            }
        }

        void TickIdle()
        {
            if (tripsBeforeHungry > 0 && _tripsSinceMeal >= tripsBeforeHungry)
            {
                if (TryGoEat()) return;
                // Nothing to eat in reach: fall asleep beside the mill. This is the beat the
                // original's Tutorial 2 teaches you to fix by planting a garden.
                _asleep = true;
                _wakeTimer = 0f;
                return;
            }

            _target = _workplace.FindNode();
            if (_target == null) return;   // nothing in range; try again next frame
            _state = State.ToNode;
        }

        void TickToNode()
        {
            if (_target == null || !_target.IsWorkable) { _state = State.Idle; return; }

            // Walk to the tree actually being felled, not the middle of the forest.
            if (!MoveToward(_target.WorkPoint)) return;

            // Seconds per STRIKE now, not per trip: a whole node still takes about as long,
            // but the payout and the shrink arrive steadily instead of all at the end.
            _harvestDuration = SecondsPerStrikeAtSpeedOne / Mathf.Max(0.01f, speed);
            _harvestTimer = 0f;
            _state = State.Harvesting;
        }

        void TickHarvesting()
        {
            if (_target == null || !_target.IsWorkable) { _state = State.Idle; return; }

            // Drifted out of the mill's reach (the node shrank, or the mill was replaced).
            if (_workplace == null) { _state = State.Following; return; }

            _harvestTimer += Time.deltaTime;
            if (_harvestTimer < _harvestDuration) return;
            _harvestTimer = 0f;

            // Every strike pays out on the spot. Waiting for a round trip made work invisible for
            // most of a minute, which read as the capybara doing nothing.
            int gained = _target.Strike();
            if (gained > 0)
            {
                _workplace.Deposit(gained);
                _tripsSinceMeal++;
            }

            bool pauseToEat = tripsBeforeHungry > 0 && _tripsSinceMeal >= tripsBeforeHungry;
            if (_target == null || !_target.IsWorkable || pauseToEat) { _state = State.Idle; return; }

            // That strike felled the tree: walk over to the next one before carrying on.
            if (Vector2.Distance(transform.position, _target.WorkPoint) > 0.5f) _state = State.ToNode;
        }

        /// <summary>Strikes between hunger checks, so a worker still stops to eat.</summary>
        const int HungerCheckEvery = 6;

        void TickToWorkplace()
        {
            if (_workplace == null) { _state = State.Idle; return; }
            if (!MoveToward(_workplace.transform.position)) return;

            // Credited on arrival, not on chop - matches the observed behaviour.
            _workplace.Deposit(_carrying);
            _carrying = 0;
            _tripsSinceMeal++;
            _state = State.Idle;
        }

        /// <summary>Snaps back to just behind the king, where a refused drop belongs.</summary>
        void ReturnToKing()
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king == null) return;
            var behind = (Vector2)king.transform.position + new Vector2(-0.9f, -0.5f);
            transform.position = new Vector3(behind.x, behind.y, transform.position.z);
        }

        /// <summary>Heads for the nearest open garden if there is one. False when there is none.</summary>
        bool TryGoEat()
        {
            _food = FoodSource.FindNear(transform.position);
            if (_food == null) return false;
            _state = State.ToFood;
            return true;
        }

        void TickToFood()
        {
            if (_food == null || !_food.IsOpen) { _state = State.Idle; return; }
            if (!MoveToward(_food.EatPoint)) return;
            _eatTimer = 0f;
            _state = State.Eating;
        }

        void TickEating()
        {
            if (_food == null) { _state = State.Idle; return; }
            _eatTimer += Time.deltaTime;
            if (_eatTimer < EatSeconds) return;

            _food.Eat();
            _food = null;
            _tripsSinceMeal = 0;
            _state = State.Idle;
        }

        /// <summary>A sleeping worker wakes the moment a garden comes within reach.</summary>
        void TickAsleep()
        {
            _wakeTimer -= Time.deltaTime;
            if (_wakeTimer > 0f) return;
            _wakeTimer = wakeCheckSeconds;

            if (!TryGoEat()) return;
            _asleep = false;
        }

        /// <summary>Steps toward a target. Returns true on arrival.</summary>
        bool MoveToward(Vector3 destination)
        {
            destination.z = transform.position.z;
            var delta = destination - transform.position;
            float distance = delta.magnitude;
            if (distance <= 0.15f) return true;

            transform.position += delta / distance * (moveSpeed * Time.deltaTime);
            FaceMovement(delta.x);
            return false;
        }

        void FaceMovement(float dx)
        {
            if (_renderer == null || Mathf.Abs(dx) < 0.001f) return;
            var scale = _renderer.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (dx < 0f ? -1f : 1f);
            _renderer.transform.localScale = scale;
        }
    }
}
