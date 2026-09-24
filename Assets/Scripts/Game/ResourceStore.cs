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
    /// Starting values are the original's: 1000 wood, 1500 gold, 0 gems (see docs/BUNNI_MECHANICS.md).
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

        /// <summary>Raised whenever any balance changes, with the kind that changed and its new total.</summary>
        public event Action<ResourceKind, int> Changed;

        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
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
