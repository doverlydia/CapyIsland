using System;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>The three spendable currencies. Named as the player sees them, not as the original XML did.</summary>
    public enum ResourceKind
    {
        Wood,
        Gold,
        Gem,
    }

    /// <summary>
    /// The player's purse. Everything that earns or spends goes through here so there is exactly one
    /// place that can change a balance, and exactly one event the HUD listens to.
    ///
    /// A new game starts with 100 wood, 100 gold and no gems: a deliberate departure from the
    /// original's 1000 / 1500 (docs/reference/game.xml), so money has a job from the start. Miso
    /// hands over the rest (900 wood, 1400 gold) with the General Store, her first quest
    /// (<see cref="MissionSetUps"/>). The scene still holds the original's numbers; a save, or a
    /// friend's island, sets its own.
    /// </summary>
    public class ResourceStore : MonoBehaviour
    {
        static ResourceStore _instance;

        /// <summary>
        /// Resolves lazily rather than relying on Awake having run. A static set only in Awake goes
        /// null whenever script execution order puts a consumer first, and survives stale across
        /// play sessions when Unity's domain reload is disabled — which silently made every
        /// purchase fail because CanAfford saw no store.
        /// </summary>
        public static ResourceStore Instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<ResourceStore>();
                return _instance;
            }
            private set => _instance = value;
        }

        [SerializeField] int wood = 1000;
        [SerializeField] int gold = 1500;
        [SerializeField] int gems;

        /// <summary>What a brand-new game starts with.</summary>
        public const int NewGameWood = 100, NewGameGold = 100;

        /// <summary>Raised whenever any balance changes, with the kind that changed and its new total.</summary>
        public event Action<ResourceKind, int> Changed;

        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            // A brand-new game (no save, not a visit): the lean start. Old saves bring their own purse.
            if (!SaveGame.HasSave && !Visit.Active)
            {
                wood = NewGameWood;
                gold = NewGameGold;
                gems = 0;
            }
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        void Start()
        {
            // Let the HUD paint its initial state without needing to poll.
            Changed?.Invoke(ResourceKind.Wood, wood);
            Changed?.Invoke(ResourceKind.Gold, gold);
            Changed?.Invoke(ResourceKind.Gem, gems);
        }

        // Best wood / gold delivered by workers in any one minute this session (HUD.updatePerMin):
        // the input to the dynamically priced items. Not saved, as in the original.
        int _woodThisMinute, _goldThisMinute;
        float _minuteStarted;
        public int BestWoodPerMinute { get; private set; }
        public int BestGoldPerMinute { get; private set; }

        int _woodLastMinute, _goldLastMinute;

        /// <summary>
        /// Roughly how much is coming in a minute right now: the last full minute, or this one
        /// if it has already beaten it. For the HUD, not for prices.
        /// </summary>
        public int PerMinute(ResourceKind kind) => kind switch
        {
            ResourceKind.Wood => Mathf.Max(_woodLastMinute, _woodThisMinute),
            ResourceKind.Gold => Mathf.Max(_goldLastMinute, _goldThisMinute),
            _ => 0,
        };

        /// <summary>A worker delivered a load to a mill or mine.</summary>
        public void RecordDelivery(ResourceKind kind, int amount)
        {
            RollMinute();
            if (kind == ResourceKind.Wood) _woodThisMinute += amount;
            else if (kind == ResourceKind.Gold) _goldThisMinute += amount;
        }

        void RollMinute()
        {
            if (Time.time - _minuteStarted < 60f) return;
            BestWoodPerMinute = Mathf.Max(BestWoodPerMinute, _woodThisMinute);
            BestGoldPerMinute = Mathf.Max(BestGoldPerMinute, _goldThisMinute);
            _woodLastMinute = _woodThisMinute;
            _goldLastMinute = _goldThisMinute;
            _woodThisMinute = _goldThisMinute = 0;
            _minuteStarted = Time.time;
        }

        void LateUpdate() => RollMinute();

        public int Get(ResourceKind kind) => kind switch
        {
            ResourceKind.Wood => wood,
            ResourceKind.Gold => gold,
            ResourceKind.Gem => gems,
            _ => 0,
        };

        public void Add(ResourceKind kind, int amount)
        {
            if (amount == 0) return;
            Set(kind, Mathf.Max(0, Get(kind) + amount));
        }

        public bool CanAfford(int woodCost, int goldCost, int gemCost) =>
            wood >= woodCost && gold >= goldCost && gems >= gemCost;

        /// <summary>Spends if affordable. Returns false and changes nothing otherwise.</summary>
        public bool TrySpend(int woodCost, int goldCost, int gemCost)
        {
            if (!CanAfford(woodCost, goldCost, gemCost)) return false;
            if (woodCost != 0) Set(ResourceKind.Wood, wood - woodCost);
            if (goldCost != 0) Set(ResourceKind.Gold, gold - goldCost);
            if (gemCost != 0) Set(ResourceKind.Gem, gems - gemCost);
            return true;
        }

        void Set(ResourceKind kind, int value)
        {
            switch (kind)
            {
                case ResourceKind.Wood: if (wood == value) return; wood = value; break;
                case ResourceKind.Gold: if (gold == value) return; gold = value; break;
                case ResourceKind.Gem: if (gems == value) return; gems = value; break;
            }
            Changed?.Invoke(kind, value);
        }
    }
}
