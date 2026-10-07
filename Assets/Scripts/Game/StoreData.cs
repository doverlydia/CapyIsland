using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>What buying (or collecting) a store item does.</summary>
    public enum StoreItemKind
    {
        Garden, Forest, Hill, Workplace, Store, House, FruitTree, Decoration, Bomb, DeerBomb,
        Animal, Gift, Church, Tower, BridgeGhost, Character, BankTrade,
    }

    /// <summary>One store item. <see cref="Name"/> is its id; the player sees <see cref="Title"/>.</summary>
    public readonly struct StoreItemDef
    {
        public readonly string Name;
        public readonly int Id;
        public readonly int Wood, Gold, Gems;
        public readonly float Inner, Outer;
        /// <summary>Ownership cap: 0 = unlimited; otherwise sold out once more than this many exist.</summary>
        public readonly int Max;
        public readonly StoreItemKind Kind;

        public StoreItemDef(string name, int id, int wood, int gold, int gems, float inner, float outer, int max,
            StoreItemKind kind)
        {
            Name = name; Id = id; Wood = wood; Gold = gold; Gems = gems; Inner = inner; Outer = outer;
            Max = max; Kind = kind;
        }

        public string Title => StoryText.Name(Name);
        public string Description => StoryText.Blurb(Name);

        public bool IsValid => !string.IsNullOrEmpty(Name);
    }

    /// <summary>
    /// The store rules around the data: lookups, which items are gated, which are sold out, and the
    /// handful whose price is worked out from how well you are doing (docs/PROGRESSION.md A7–A9).
    /// </summary>
    public static partial class StoreData
    {
        static Dictionary<string, StoreItemDef> _byName;

        public static bool TryGet(string name, out StoreItemDef def)
        {
            if (_byName == null)
            {
                _byName = new Dictionary<string, StoreItemDef>();
                foreach (var d in All) _byName[d.Name] = d;
            }
            return _byName.TryGetValue(name, out def);
        }

        public static StoreItemDef Get(string name) => TryGet(name, out var d) ? d : default;

        /// <summary>
        /// Whether a sale display is shown at all (StoreItem.Avaliable). The Steel Store waits for
        /// the Wedding Dress, the Gold Store for the pearls, the Church for the tiara.
        /// </summary>
        public static bool IsUnlocked(string name) => name switch
        {
            "Silver Store" => Motes.Has("Bronze Store"),
            "Steel Store" => Motes.Has("The Girl's New Dress"),
            "Gold Store" => Motes.Has("The Girl's New Necklace"),
            "Church" => Motes.Has("The Girl's Tiara"),
            _ => true,
        };

        /// <summary>
        /// Sold out (StoreItem.SoldOut): the dress, pearls and tiara once given; otherwise when the
        /// item has a cap and more than that many exist anywhere — its own display counts as one.
        /// </summary>
        public static bool IsSoldOut(string name)
        {
            switch (name)
            {
                case "Wedding Dress": return Motes.Has("The Girl's New Dress");
                case "String of Pearls": return Motes.Has("The Girl's New Necklace");
                case "Tiara": return Motes.Has("The Girl's Tiara");
            }
            var def = Get(name);
            if (!def.IsValid || def.Max <= 0) return false;
            return CountByName(name) > def.Max;
        }

        /// <summary>
        /// How many things named this exist: planted or carried items, bubbles holding one, sale
        /// displays, animals — the original's Items.GetCountByName.
        /// </summary>
        public static int CountByName(string name)
        {
            // Counted once per frame for every name at once: shop displays ask every frame.
            if (_countedFrame != Time.frameCount)
            {
                _countedFrame = Time.frameCount;
                _counts.Clear();
                void Add(string n) { if (!string.IsNullOrEmpty(n)) _counts[n] = _counts.TryGetValue(n, out var c) ? c + 1 : 1; }
                foreach (var tag in Object.FindObjectsByType<CatalogItem>(FindObjectsSortMode.None)) Add(tag.StoreName);
                foreach (var display in Object.FindObjectsByType<SaleDisplay>(FindObjectsSortMode.None)) Add(display.StoreName);
                foreach (var bubble in Object.FindObjectsByType<Bubble>(FindObjectsSortMode.None))
                {
                    if (bubble.Taken || bubble.Contents == null) continue;
                    var tag = bubble.Contents.GetComponent<CatalogItem>();
                    if (tag != null) Add(tag.StoreName);
                }
            }
            return _counts.TryGetValue(name, out var n2) ? n2 : 0;
        }

        static int _countedFrame = -1;
        static readonly Dictionary<string, int> _counts = new();

        /// <summary>Forget this frame's counts (after creating or destroying things mid-frame).</summary>
        public static void InvalidateCounts() => _countedFrame = -1;

        // ---------------------------------------------------------------- dynamic prices

        static readonly Dictionary<string, Vector3Int> _pinnedPrices = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            _pinnedPrices.Clear();
            _byName = null;
        }

        /// <summary>
        /// The price asked for an item: the XML cost, except for the few priced from your best
        /// minute of production and your current stock. Those are worked out once, the first time
        /// they are asked for, and then stay put (StoreItem constructor / Avaliable).
        /// </summary>
        public static Vector3Int Price(string name)
        {
            var def = Get(name);
            if (!IsDynamic(name)) return new Vector3Int(def.Wood, def.Gold, def.Gems);
            if (_pinnedPrices.TryGetValue(name, out var pinned)) return pinned;

            var store = ResourceStore.Instance;
            int wood = store != null ? store.Get(ResourceKind.Wood) : 0;
            int gold = store != null ? store.Get(ResourceKind.Gold) : 0;
            int woodMin = store != null ? store.BestWoodPerMinute : 0;
            int goldMin = store != null ? store.BestGoldPerMinute : 0;

            int w, g;
            switch (name)
            {
                case "Verdant Bay": w = 3 * woodMin + wood / 4; g = 3 * goldMin + gold / 4; break;
                case "Wedding Isle": w = 5 * woodMin + wood / 4; g = 5 * goldMin + gold / 4; break;
                case "Wedding Dress": w = 1 * woodMin + gold / 2; g = 1 * goldMin + wood / 2; break;
                case "String of Pearls": w = 4 * woodMin + gold / 2; g = 4 * goldMin + wood / 2; break;
                case "Tiara": w = 8 * woodMin + gold / 2; g = 8 * goldMin + wood / 2; break;
                case "Silver Store": w = 2 * woodMin + gold; g = 2 * goldMin + gold + 10; break;
                case "Steel Store": w = 6 * woodMin + gold / 2; g = 6 * goldMin + wood / 2; break;
                case "Gold Store": w = 7 * woodMin + gold / 2; g = 7 * goldMin + wood / 2; break;
                case "Church": w = 10 * woodMin + gold / 2; g = 10 * goldMin + wood / 2; break;
                default: w = def.Wood; g = def.Gold; break;
            }
            var price = new Vector3Int(Mathf.Min(w, 99999), Mathf.Min(g, 99999), def.Gems);
            _pinnedPrices[name] = price;
            return price;
        }

        public static bool IsDynamic(string name) => name is
            "Verdant Bay" or "Wedding Isle" or "Wedding Dress" or "String of Pearls" or "Tiara" or
            "Silver Store" or "Steel Store" or "Gold Store" or "Church";

        /// <summary>Prices already pinned, for saving.</summary>
        public static IReadOnlyDictionary<string, Vector3Int> PinnedPrices => _pinnedPrices;

        public static void RestorePinnedPrice(string name, Vector3Int price) => _pinnedPrices[name] = price;
    }
}
