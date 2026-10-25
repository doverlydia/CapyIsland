using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>What buying (or collecting) a store item does.</summary>
    public enum StoreItemKind
    {
        Garden, Forest, Hill, Workplace, Store, House, FruitTree, Decoration, Bomb, DeerBomb,
        Animal, Gift, Church, Tower, BridgeGhost, Character, BankTrade,
        Plot, Stove,   // the hobby kit from the Garden Shop (built in code, see HobbyCatalog)
        Tool,          // hobby gear you carry (rod, net, binoculars): buying it unlocks the hobby
        Sewing,        // the sewing machine, a placed station
        FurnitureShop, // sells the burrow door and the day's furniture for your home (Game/Home)
        Seed,          // a packet of seeds from the farm's barn: carried, and dropped on a farm plot to plant it
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

        /// <summary>
        /// Items that are ours, not the original's: the hobby kit, the shops for it, and the bridge to
        /// Sunflower Farm. Kept apart from the generated table.
        /// </summary>
        public static readonly StoreItemDef[] Extra =
        {
            new StoreItemDef("Garden Shop", 200, 6000, 6000, 0, 41f, 0f, 1, StoreItemKind.Store),
            new StoreItemDef("Vegetable Plot", 201, Gardening.DigWood, 0, 0, 45f, 0f, 99, StoreItemKind.Plot),
            new StoreItemDef("Stove", 202, 400, 300, 0, 40f, 0f, 1, StoreItemKind.Stove),
            new StoreItemDef("Fishing Rod", 203, 150, 100, 0, 30f, 0f, 1, StoreItemKind.Tool),
            new StoreItemDef("Bug Net", 204, 100, 150, 0, 30f, 0f, 1, StoreItemKind.Tool),
            new StoreItemDef("Binoculars", 205, 400, 400, 0, 30f, 0f, 1, StoreItemKind.Tool),
            new StoreItemDef("Sewing Machine", 206, 1500, 1500, 0, 40f, 0f, 1, StoreItemKind.Sewing),
            new StoreItemDef("Furniture Shop", 207, 2000, 1500, 0, 41f, 0f, 1, StoreItemKind.FurnitureShop),
            // The spirit at the end of Pebble Cove: the bridge to Sunflower Farm (Farm).
            new StoreItemDef("Farm Isle", 208, 6000, 6000, 0, 0f, 0f, 1, StoreItemKind.BridgeGhost),
        };

        /// <summary>The hobby a piece of gear (or a station) opens up, if it is one.</summary>
        public static Hobby? HobbyFor(string name) => name switch
        {
            "Fishing Rod" => Hobby.Fishing,
            "Bug Net" => Hobby.Bugs,
            "Binoculars" => Hobby.Birding,
            "Sewing Machine" => Hobby.Sewing,
            "Vegetable Plot" or "Garden Shop" => Hobby.Gardening,
            "Stove" => Hobby.Cooking,
            _ => null,
        };

        static void AddTo(string list, string item)
        {
            if (!Lists.TryGetValue(list, out var items) || System.Array.IndexOf(items, item) >= 0) return;
            var more = new string[items.Length + 1];
            items.CopyTo(more, 0);
            more[items.Length] = item;
            Lists[list] = more;
        }

        static StoreData()
        {
            // Gardening and its stove live on Sunflower Farm now (Farm): the Garden Shop is no longer
            // sold. Its items stay defined so older islands still load them, to be moved to the farm.
            // The farm's barn sells the seeds.
            var seeds = new List<string>();
            foreach (var c in HobbyData.Crops) seeds.Add(SeedName(c));
            Lists["farm_store"] = seeds.ToArray();
            AddTo("bronze_store", "Bug Net");
            AddTo("silver_store", "Binoculars");
            AddTo("steel_store", "Sewing Machine");
            // The woolly capybara's home: the data has it, but no shop sold it, so the only woolly was
            // Captain Wasabi's. The Gold Store comes around the time she introduces them.
            AddTo("gold_store", "Panda House");
            // Your home under the ground: the General Store sells the den (your front door), and the
            // Furniture Shop waits inside it in a bubble the first time you go down (BurrowScreen);
            // it sells everything to put in there. (The Fancy Store, which also lists the shop, was
            // the original's donors-only store: nothing sells it.)
            AddTo("fancy_store", "Furniture Shop");
            RemoveFrom("fancy_store", "Monster Den");
            AddTo("general_store", "Monster Den");
        }

        static void RemoveFrom(string list, string item)
        {
            if (!Lists.TryGetValue(list, out var items) || System.Array.IndexOf(items, item) < 0) return;
            var fewer = new List<string>(items);
            fewer.Remove(item);
            Lists[list] = fewer.ToArray();
        }

        public static bool TryGet(string name, out StoreItemDef def)
        {
            if (_byName == null)
            {
                _byName = new Dictionary<string, StoreItemDef>();
                foreach (var d in All) _byName[d.Name] = d;
                foreach (var d in Extra) _byName[d.Name] = d;
                foreach (var d in Seeds()) _byName[d.Name] = d;
            }
            return _byName.TryGetValue(name, out def);
        }

        // ---------------------------------------------------------------- seeds

        /// <summary>The store name of a crop's seed packet ("Carrot Seeds").</summary>
        public static string SeedName(CropKind crop) => crop.Id + " Seeds";

        /// <summary>The crop a seed packet grows, or null if the name isn't a packet.</summary>
        public static CropKind SeedCrop(string name)
        {
            if (string.IsNullOrEmpty(name) || !name.EndsWith(" Seeds", System.StringComparison.Ordinal)) return null;
            return HobbyData.Crop(name.Substring(0, name.Length - " Seeds".Length));
        }

        /// <summary>A packet for every crop, at its seed price in gold, as many as you like.</summary>
        static IEnumerable<StoreItemDef> Seeds()
        {
            for (int i = 0; i < HobbyData.Crops.Length; i++)
            {
                var c = HobbyData.Crops[i];
                yield return new StoreItemDef(SeedName(c), 300 + i, 0, c.SeedGold, 0, 12f, 0f, 0, StoreItemKind.Seed);
            }
        }

        /// <summary>
        /// Why something on display can't be bought yet, or null: seeds wait for your gardening
        /// level. The display still shows (greyed), so you can see what's coming.
        /// </summary>
        public static string LockedReason(string name)
        {
            var crop = SeedCrop(name);
            if (crop != null && crop.MinLevel > HobbyLog.Level(Hobby.Gardening))
                return string.Format("Requires gardening level {0}", crop.MinLevel);
            return null;
        }

        public static StoreItemDef Get(string name) => TryGet(name, out var d) ? d : default;

        /// <summary>The house each worker is sold beside (a worker isn't in any shop's stock list).</summary>
        public static readonly Dictionary<string, string> WorkerHome = new()
        {
            { "Capybara", "Capy House" }, { "Fox", "Fox House" }, { "Panda", "Panda House" },
            { "Pirate", "Pirate House" }, { "Monster", "Monster House" },
        };

        /// <summary>The heart (by func) a gated display waits for — the story step before it's for sale — or null.</summary>
        public static string UnlockedBy(string name) => name switch
        {
            "Silver Store" => "bronze_store",
            "Church" => "girl_crown",
            _ => null,
        };

        /// <summary>
        /// Whether a sale display is shown at all (StoreItem.Avaliable). The Church waits for the
        /// tiara. Unlike the original, the Steel and Gold Stores don't wait for Miso's gifts: the
        /// Captain's errands need them too, and one character's task shouldn't hold up another's.
        /// Miso's gifts themselves (dress, pearls, tiara) go on sale once she has asked for them,
        /// since a gift bought before she asks wouldn't count. The bombs, the hobby gear and the
        /// Burrow Door wait for their own moment too (ShopPacing).
        /// </summary>
        public static bool IsUnlocked(string name) => name switch
        {
            "Silver Store" => Motes.Has("Bronze Store"),
            "Church" => Motes.Has("The Girl's Tiara"),
            "Wedding Dress" => Asked("The Girl's New Dress"),
            "String of Pearls" => Asked("The Girl's New Necklace"),
            "Tiara" => Asked("The Girl's Tiara"),
            _ => ShopPacing.Ready(name) ?? true,
        };

        static bool Asked(string taskHeart) => QuestSystem.Instance == null || !QuestSystem.Instance.WaitsForTask(taskHeart);

        /// <summary>
        /// Sold out (StoreItem.SoldOut): the dress, pearls and tiara once given; otherwise when the
        /// item has a cap and more than that many exist anywhere — its own display counts as one.
        /// </summary>
        public static bool IsSoldOut(string name)
        {
            // On a friend's island the shops are theirs: you can only buy the help you came to give.
            if (Visit.Active && !ForSaleWhileVisiting(name)) return true;
            switch (name)
            {
                case "Wedding Dress": return Motes.Has("The Girl's New Dress");
                case "String of Pearls": return Motes.Has("The Girl's New Necklace");
                case "Tiara": return Motes.Has("The Girl's Tiara");
            }
            var def = Get(name);
            if (!def.IsValid || def.Max <= 0) return false;
            // Gear: once you have it, the shop has nothing more to sell you.
            if (def.Kind == StoreItemKind.Tool && HobbyFor(name) is Hobby gear) return HobbyLog.IsUnlocked(gear);
            // Plots: as many as your gardening level allows (the display itself counts as one).
            if (name == "Vegetable Plot") return CountByName(name) > Gardening.MaxPlots;
            return CountByName(name) > def.Max;
        }

        /// <summary>
        /// What a visitor may buy on someone else's island: food for their workers (flower gardens)
        /// and the gentle Shoo Bomb for their pests. Everything else would be yours to keep, and the
        /// one-of-a-kind buildings you already own would turn into second copies back home.
        /// </summary>
        public static bool ForSaleWhileVisiting(string name) =>
            IsChoreItem(name)
            // Building together (CoBuild): anything that doesn't move a story on.
            || (CoBuild.Allowed && !StaysHome(name));

        /// <summary>Help a visitor can always give: flower gardens for hungry workers, the Shoo Bomb for pests.</summary>
        public static bool IsChoreItem(string name) =>
            name == "Deer Bomb" || name.EndsWith(" Garden", System.StringComparison.Ordinal);

        /// <summary>
        /// Never bought or put down on a friend's island, even when building together: things that
        /// move someone's story or progress on (the tower, the church, new islands, the wedding
        /// gifts, characters, shops, hobby kits, animals), and the real bomb, which would knock
        /// their island back into bubbles. The Shoo Bomb only scares pests off, so it may.
        /// </summary>
        public static bool StaysHome(string name)
        {
            var def = Get(name);
            if (!def.IsValid) return true;
            switch (def.Kind)
            {
                case StoreItemKind.Gift: case StoreItemKind.Church: case StoreItemKind.Tower:
                case StoreItemKind.BridgeGhost: case StoreItemKind.Character: case StoreItemKind.Store:
                case StoreItemKind.Animal: case StoreItemKind.Tool: case StoreItemKind.Plot:
                case StoreItemKind.Stove: case StoreItemKind.Sewing: case StoreItemKind.FurnitureShop:
                case StoreItemKind.Seed: case StoreItemKind.Bomb:
                    return true;
                default:
                    return false;
            }
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
