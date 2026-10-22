using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    public enum WearSlot { Hat, Neck, Face, Back }

    /// <summary>One outfit piece, and where it sits on the player's capybara.</summary>
    public class WearItem
    {
        public string Id;
        public string Name;
        public WearSlot Slot;

        /// <summary>How it is earned: an achievement func, "daily" for the day-7 gift, or "start".</summary>
        public string Unlock;

        /// <summary>Centre of the piece on the body sprite, 0–1 from its bottom-left corner.</summary>
        public Vector2 Anchor;

        /// <summary>Width of the piece as a share of the body sprite's width.</summary>
        public float Width;

        public float Rotation;

        /// <summary>Drawn behind the body (capes), rather than over it.</summary>
        public bool Behind;
    }

    /// <summary>
    /// The player capybara's closet: which pieces exist, which you own, and what you are wearing.
    /// Pieces are earned from achievements and from the day-7 daily gift, never bought, so they
    /// never touch the island's balance.
    ///
    /// Kept apart from the island save, in its own preferences, so starting a new island never
    /// takes your clothes away. Art lives in Resources/Wardrobe; a piece without art yet is kept
    /// out of sight rather than shown broken.
    /// </summary>
    public static class Wardrobe
    {
        const string OwnedKey = "CapyIsland.Wardrobe.Owned";
        const string WornKey = "CapyIsland.Wardrobe.Worn";
        public const string BaseArt = "Capy_Player";

        public static event Action Changed;

        /// <summary>A piece was just earned (for the "new outfit" card).</summary>
        public static event Action<WearItem> Unlocked;

        public static readonly WearItem[] Items =
        {
            new() { Id = "Hat_Crown",       Name = "Royal Crown",     Slot = WearSlot.Hat,  Unlock = "start",         Anchor = new(0.45f, 0.875f), Width = 0.29f },
            new() { Id = "Hat_Yuzu",        Name = "Yuzu",            Slot = WearSlot.Hat,  Unlock = "first_mote",    Anchor = new(0.46f, 0.85f), Width = 0.3f },
            new() { Id = "Hat_Flowers",     Name = "Flower Crown",    Slot = WearSlot.Hat,  Unlock = "girl_quest_1",  Anchor = new(0.46f, 0.825f), Width = 0.38f },
            new() { Id = "Hat_Beanie",      Name = "Cosy Beanie",     Slot = WearSlot.Hat,  Unlock = "panda1",        Anchor = new(0.46f, 0.86f), Width = 0.35f },
            new() { Id = "Hat_Straw",       Name = "Straw Hat",       Slot = WearSlot.Hat,  Unlock = "built_church",  Anchor = new(0.46f, 0.87f), Width = 0.46f },
            new() { Id = "Hat_Party",       Name = "Party Hat",       Slot = WearSlot.Hat,  Unlock = "daily",         Anchor = new(0.44f, 0.87f), Width = 0.28f },
            new() { Id = "Hat_Towel",       Name = "Hot Spring Towel", Slot = WearSlot.Hat, Unlock = "daily",         Anchor = new(0.46f, 0.82f), Width = 0.38f },
            new() { Id = "Hat_Lantern",     Name = "Yuzu's Lantern",  Slot = WearSlot.Hat,  Unlock = "finale",        Anchor = new(0.45f, 0.9f), Width = 0.24f },
            new() { Id = "Neck_Bandana",    Name = "Bandana",         Slot = WearSlot.Neck, Unlock = "fox1",          Anchor = new(0.47f, 0.325f), Width = 0.26f },
            new() { Id = "Neck_BowTie",     Name = "Bow Tie",         Slot = WearSlot.Neck, Unlock = "wedding_mote",  Anchor = new(0.47f, 0.37f), Width = 0.22f },
            new() { Id = "Neck_Scarf",      Name = "Red Scarf",       Slot = WearSlot.Neck, Unlock = "daily",         Anchor = new(0.48f, 0.36f), Width = 0.28f },
            new() { Id = "Face_Glasses",    Name = "Reading Glasses", Slot = WearSlot.Face, Unlock = "silver_store",  Anchor = new(0.37f, 0.665f), Width = 0.34f },
            new() { Id = "Face_Sunglasses", Name = "Sunglasses",      Slot = WearSlot.Face, Unlock = "daily",         Anchor = new(0.37f, 0.665f), Width = 0.36f },
            new() { Id = "Back_Backpack",   Name = "Little Backpack", Slot = WearSlot.Back, Unlock = "place_mine",    Anchor = new(0.74f, 0.635f), Width = 0.26f },
            new() { Id = "Hat_Sock",        Name = "Lucky Sock",      Slot = WearSlot.Hat,  Unlock = "cat_gift",      Anchor = new(0.46f, 0.87f), Width = 0.36f },
            new() { Id = "Neck_Bell",       Name = "Bell Collar",     Slot = WearSlot.Neck, Unlock = "cat_gift",      Anchor = new(0.48f, 0.355f), Width = 0.22f },
            new() { Id = "Hat_Angler",      Name = "Angler's Bucket Hat", Slot = WearSlot.Hat, Unlock = "journal_Fishing", Anchor = new(0.46f, 0.84f), Width = 0.42f },
            new() { Id = "Hat_Feather",     Name = "Feathered Cap",   Slot = WearSlot.Hat,  Unlock = "journal_Birding", Anchor = new(0.45f, 0.875f), Width = 0.38f },
            new() { Id = "Hat_Antennae",    Name = "Bug Antennae",    Slot = WearSlot.Hat,  Unlock = "journal_Bugs",    Anchor = new(0.46f, 0.845f), Width = 0.33f },
            new() { Id = "Neck_MedalFish",  Name = "Gold Fishing Medal", Slot = WearSlot.Neck, Unlock = "contest_Fish",  Anchor = new(0.48f, 0.365f), Width = 0.21f },
            new() { Id = "Neck_MedalBug",   Name = "Gold Bug Medal",     Slot = WearSlot.Neck, Unlock = "contest_Bugs",  Anchor = new(0.48f, 0.365f), Width = 0.21f },
            new() { Id = "Neck_MedalBird",  Name = "Gold Birding Medal", Slot = WearSlot.Neck, Unlock = "contest_Birds", Anchor = new(0.48f, 0.365f), Width = 0.21f },
            new() { Id = "Back_Cape",       Name = "Royal Cape",      Slot = WearSlot.Back, Unlock = "monster_tame",  Anchor = new(0.6f, 0.45f), Width = 0.76f, Behind = true },
            // Only Horaira brings this one (CatGifts): a halo like his own (Horaira).
            new() { Id = Horaira.HaloId,    Name = "Horaira's Halo",  Slot = WearSlot.Hat,  Unlock = "horaira",       Anchor = new(0.45f, 0.985f), Width = 0.48f },   // wide: its glow and sparkles are part of the picture
        };

        static readonly HashSet<string> _owned = new();
        static readonly Dictionary<WearSlot, string> _worn = new();
        static readonly Dictionary<string, Sprite> _art = new();
        static bool _loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            Changed = null;
            Unlocked = null;
            _loaded = false;
            _art.Clear();
        }

        // ---------------------------------------------------------------- queries

        public static WearItem Find(string id)
        {
            if (id != null && id.StartsWith(DesignPrefix)) return Sewn(id);
            return Array.Find(Items, i => i.Id == id);
        }

        // ---------------------------------------------------------------- pieces you sewed

        const string DesignPrefix = "Design_";

        public static string DesignId(int index) => DesignPrefix + index;

        /// <summary>Everything to choose from: the shop's and achievements' pieces, then what you sewed.</summary>
        public static IEnumerable<WearItem> AllItems
        {
            get
            {
                foreach (var i in Items) yield return i;
                for (int d = 0; d < HobbyLog.Designs.Count; d++)
                {
                    var item = Sewn(DesignId(d));
                    if (item != null) yield return item;
                }
            }
        }

        /// <summary>A sewn hat or cape as a wardrobe piece (old shirts are not pieces).</summary>
        static WearItem Sewn(string id)
        {
            if (!int.TryParse(id.Substring(DesignPrefix.Length), out var index) || index < 0 || index >= HobbyLog.Designs.Count) return null;
            var d = HobbyLog.Designs[index];
            return d.kind switch
            {
                "hat" => new WearItem { Id = id, Name = d.name, Slot = WearSlot.Hat, Unlock = "sewn", Anchor = new(0.46f, 0.87f), Width = 0.37f },
                "fedora" => new WearItem { Id = id, Name = d.name, Slot = WearSlot.Hat, Unlock = "sewn", Anchor = new(0.46f, 0.89f), Width = 0.42f },
                "sunhat" => new WearItem { Id = id, Name = d.name, Slot = WearSlot.Hat, Unlock = "sewn", Anchor = new(0.46f, 0.88f), Width = 0.46f },
                "cape" => new WearItem { Id = id, Name = d.name, Slot = WearSlot.Back, Unlock = "sewn", Anchor = new(0.6f, 0.45f), Width = 0.76f, Behind = true },
                _ => null,
            };
        }

        /// <summary>A sewn piece was made or changed: draw it afresh, and let whoever is wearing it update.</summary>
        public static void Sewn(int index)
        {
            _art.Remove(DesignId(index));
            Changed?.Invoke();
        }

        public static bool Owns(WearItem item) { Load(); return item != null && (item.Unlock == "sewn" || _owned.Contains(item.Id)); }

        public static WearItem Worn(WearSlot slot)
        {
            Load();
            return _worn.TryGetValue(slot, out var id) ? Find(id) : null;
        }

        /// <summary>The piece's picture, or null while its art has not been made yet.</summary>
        public static Sprite Art(WearItem item) => item == null ? null : Art(item.Id);

        public static Sprite Art(string id)
        {
            if (_art.TryGetValue(id, out var s)) return s;
            if (id.StartsWith(DesignPrefix))
            {
                var sewn = Sewn(id);
                var design = HobbyLog.Designs[int.Parse(id.Substring(DesignPrefix.Length))];
                s = sewn != null ? UI.SewingScreen.DesignSprite(design.pixels, design.kind) : null;
                _art[id] = s;
                return s;
            }
            s = Resources.Load<Sprite>("Wardrobe/" + id);
            _art[id] = s;
            return s;
        }

        /// <summary>The bare capybara the pieces are layered on, once its art exists.</summary>
        public static Sprite BaseSprite => Art(BaseArt);

        /// <summary>True once there is anything to dress up in.</summary>
        public static bool Ready => BaseSprite != null;

        /// <summary>How a piece is earned, for the locked tile.</summary>
        public static string HowToEarn(WearItem item)
        {
            if (item.Unlock == "daily") return "Day 7 daily gift";
            if (item.Unlock == "start") return "Yours from the start";
            if (item.Unlock == "cat_gift") return "A present from a cat";
            if (item.Unlock == "horaira") return "A present from Horaira";
            if (item.Unlock == "sewn") return "You sewed this!";
            if (item.Unlock == "finale") return "Bring every warm heart home";
            if (item.Unlock == Motes.Wedding) return "Hold the wedding at the Blossom Chapel";
            if (item.Unlock.StartsWith("contest_")) return "First place in a contest";
            if (item.Unlock == "journal_Fishing") return "Complete the fish journal";
            if (item.Unlock == "journal_Birding") return "Complete the bird journal";
            if (item.Unlock == "journal_Bugs") return "Complete the bug journal";
            return Motes.TryGetByFunc(item.Unlock, out var m) ? "Achievement: " + m.Title : "Keep playing";
        }

        /// <summary>The piece an achievement awards, if any.</summary>
        public static WearItem RewardFor(string moteFunc) => Array.Find(Items, i => i.Unlock == moteFunc);

        /// <summary>The next daily-gift piece still to earn (with art), or null once all are yours.</summary>
        public static WearItem NextDailyPiece()
        {
            Load();
            foreach (var i in Items)
                if (i.Unlock == "daily" && !_owned.Contains(i.Id) && Art(i) != null) return i;
            return null;
        }

        // ---------------------------------------------------------------- changes

        /// <summary>Adds a piece to the closet. Returns false if it was already there.</summary>
        public static bool Give(WearItem item, bool announce = true)
        {
            Load();
            if (item == null || !_owned.Add(item.Id)) return false;
            Save();
            if (!_catchingUp) NewStuff.Add("wardrobe/" + item.Slot + "/" + item.Id);
            if (announce) Unlocked?.Invoke(item);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Puts a piece on (replacing whatever was in its slot), or takes it off if already worn.</summary>
        public static void Toggle(WearItem item)
        {
            Load();
            if (item == null || !Owns(item)) return;
            if (_worn.TryGetValue(item.Slot, out var current) && current == item.Id) _worn.Remove(item.Slot);
            else _worn[item.Slot] = item.Id;
            Save();
            if (item.Id != "Hat_Crown") Motes.Trigger("dress_up");
            Changed?.Invoke();
        }

        /// <summary>
        /// Hands over every piece already earned by achievements — so a player who finished goals
        /// before the wardrobe existed still gets them, quietly.
        /// </summary>
        public static void CatchUp()
        {
            Load();
            _catchingUp = true;   // pieces earned long ago aren't news
            foreach (var i in Items)
                if (i.Unlock != "daily" && i.Unlock != "start" && !_owned.Contains(i.Id)
                    && (Motes.TryGetByFunc(i.Unlock, out var m) && Motes.Has(m.Name) || i.Unlock == Motes.Wedding && YuzuHeart.Married))
                    Give(i, announce: false);
            _catchingUp = false;
        }

        static bool _catchingUp;

        /// <summary>An achievement fired: its piece, if it has one.</summary>
        public static void OnAchievement(MoteDef mote)
        {
            var item = RewardFor(mote.Func);
            if (item != null) Give(item);
        }

        // ---------------------------------------------------------------- storage

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _owned.Clear();
            _worn.Clear();
            foreach (var id in PlayerPrefs.GetString(OwnedKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                _owned.Add(id);
            foreach (var pair in PlayerPrefs.GetString(WornKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split(':');
                if (parts.Length == 2 && Enum.TryParse(parts[0], out WearSlot slot)) _worn[slot] = parts[1];
            }
            // Everyone starts with the crown on.
            if (_owned.Add("Hat_Crown") && !_worn.ContainsKey(WearSlot.Hat)) _worn[WearSlot.Hat] = "Hat_Crown";
        }

        static void Save()
        {
            PlayerPrefs.SetString(OwnedKey, string.Join(",", _owned));
            var worn = new List<string>();
            foreach (var kv in _worn) worn.Add(kv.Key + ":" + kv.Value);
            PlayerPrefs.SetString(WornKey, string.Join(",", worn));
            PlayerPrefs.Save();
        }
    }
}
