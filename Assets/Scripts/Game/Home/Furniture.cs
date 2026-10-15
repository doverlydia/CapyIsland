using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>One piece of furniture for your burrow: its picture, its price, how big it stands.</summary>
    public class FurnitureDef
    {
        public string Id, Name;
        public int Wood, Gold, Gems;
        /// <summary>How wide its picture (margin and all) is drawn in the room, as a share of the room's width.</summary>
        public float Width;
        /// <summary>Flat on the floor (a rug): always drawn under everything else.</summary>
        public bool Flat;

        /// <summary>A treat, bought with gems: one of these is in the shop each day.</summary>
        public bool Fancy => Gems > 0;

        public Sprite Art => Resources.Load<Sprite>("Home/Furn_" + Id);
    }

    /// <summary>
    /// Every piece the Furniture Shop can sell, and which five it has in today: four everyday
    /// pieces (wood and gold) and one fancy one (gems), a fresh choice each morning.
    /// </summary>
    public static class Furniture
    {
        public static readonly FurnitureDef[] All =
        {
            new() { Id = "Rug",       Name = "Woven Rug",         Wood = 120, Gold = 80,  Width = 0.6f, Flat = true },
            new() { Id = "Bed",       Name = "Hay Bed",           Wood = 300, Gold = 150, Width = 0.46f },
            new() { Id = "Armchair",  Name = "Comfy Armchair",    Wood = 200, Gold = 200, Width = 0.36f },
            new() { Id = "Table",     Name = "Round Table",       Wood = 250, Gold = 100, Width = 0.32f },
            new() { Id = "Stool",     Name = "Mushroom Stool",    Wood = 60,  Gold = 60,  Width = 0.2f },
            new() { Id = "Bookshelf", Name = "Bookshelf",         Wood = 350, Gold = 150, Width = 0.44f },
            new() { Id = "Lamp",      Name = "Floor Lamp",        Wood = 100, Gold = 150, Width = 0.44f },
            new() { Id = "Plant",     Name = "Potted Fern",       Wood = 50,  Gold = 100, Width = 0.32f },
            new() { Id = "Dresser",   Name = "Chest of Drawers",  Wood = 300, Gold = 200, Width = 0.38f },
            new() { Id = "Painting",  Name = "Sea Painting",      Wood = 100, Gold = 250, Width = 0.36f },
            new() { Id = "ToyChest",  Name = "Toy Chest",         Wood = 150, Gold = 150, Width = 0.32f },
            new() { Id = "Radio",     Name = "Old Radio",         Wood = 100, Gold = 300, Width = 0.3f },
            new() { Id = "Aquarium",  Name = "Fish Tank",         Gems = 15,  Width = 0.34f },
            new() { Id = "HotTub",    Name = "Little Hot Tub",    Gems = 25,  Width = 0.46f },
            new() { Id = "Piano",     Name = "Tiny Piano",        Gems = 20,  Width = 0.42f },
            new() { Id = "Fireplace", Name = "Stone Fireplace",   Gems = 20,  Width = 0.48f },
        };

        public static FurnitureDef Get(string id) => Array.Find(All, f => f.Id == id);

        /// <summary>Today, counted in whole days, so everyone's shop turns over at midnight.</summary>
        public static int Day => (int)(DateTime.Now.Date - new DateTime(2024, 1, 1)).TotalDays;

        /// <summary>How long until the shop has new pieces in.</summary>
        public static TimeSpan UntilTomorrow => DateTime.Now.Date.AddDays(1) - DateTime.Now;

        /// <summary>Today's five: four everyday pieces and one fancy one, the same all day.</summary>
        public static List<FurnitureDef> Today()
        {
            var rng = new System.Random(Day * 7919 + 17);
            var plain = new List<FurnitureDef>();
            var fancy = new List<FurnitureDef>();
            foreach (var f in All) (f.Fancy ? fancy : plain).Add(f);
            var picks = new List<FurnitureDef>();
            for (int i = 0; i < 4 && plain.Count > 0; i++)
            {
                int k = rng.Next(plain.Count);
                picks.Add(plain[k]);
                plain.RemoveAt(k);
            }
            if (fancy.Count > 0) picks.Add(fancy[rng.Next(fancy.Count)]);
            return picks;
        }
    }

    /// <summary>
    /// Your burrow: the furniture you own, and where each placed piece stands in the room (0 … 1
    /// across, 0 … 1 from the front of the floor to the back wall). Kept in PlayerPrefs, like
    /// your hobbies and outfits, so it survives starting the island over.
    /// </summary>
    public static class HomeLog
    {
        const string Key = "CapyIsland.Home";

        [Serializable] public class Placed { public string id; public float x, y; public bool flip; }
        [Serializable] class Owned { public string id; public int count; }
        [Serializable] class Data
        {
            public List<Owned> owned = new();
            public List<Placed> placed = new();
            public int boughtDay = -1;
            public List<string> boughtToday = new();
            public bool shopGiven;   // the Furniture Shop's bubble, waiting in a new burrow, was popped
        }

        /// <summary>The Furniture Shop has been handed over (from its bubble in your first burrow).</summary>
        public static bool ShopGiven
        {
            get => D.shopGiven;
            set { D.shopGiven = value; Save(); }
        }

        static Data _d;
        static Data D => _d ??= Load();

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            _d = null;
            Changed = null;
        }

        static Data Load()
        {
            try { return JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key, "")) ?? new Data(); }
            catch { return new Data(); }
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(D));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Pieces waiting in storage (owned, not standing in the room).</summary>
        public static int InStorage(string id) => D.owned.Find(o => o.id == id)?.count ?? 0;

        public static List<(FurnitureDef def, int count)> Storage()
        {
            var list = new List<(FurnitureDef, int)>();
            foreach (var o in D.owned)
            {
                var def = Furniture.Get(o.id);
                if (def != null && o.count > 0) list.Add((def, o.count));
            }
            return list;
        }

        public static IReadOnlyList<Placed> Room => D.placed;

        public static void AddToStorage(string id, int n = 1)
        {
            var o = D.owned.Find(x => x.id == id);
            if (o == null) D.owned.Add(o = new Owned { id = id });
            o.count += n;
            Save();
        }

        /// <summary>Takes a piece out of storage and stands it in the room.</summary>
        public static Placed PlaceFromStorage(string id, float x, float y)
        {
            var o = D.owned.Find(v => v.id == id);
            if (o == null || o.count <= 0) return null;
            o.count--;
            var p = new Placed { id = id, x = x, y = y };
            D.placed.Add(p);
            Save();
            return p;
        }

        /// <summary>Back into storage.</summary>
        public static void PutAway(Placed p)
        {
            if (!D.placed.Remove(p)) return;
            AddToStorage(p.id);
        }

        // ---------------------------------------------------------------- the shop's day

        /// <summary>Each of today's pieces can be bought once; the shop restocks tomorrow.</summary>
        public static bool BoughtToday(string id)
        {
            if (D.boughtDay != Furniture.Day) return false;
            return D.boughtToday.Contains(id);
        }

        public static void MarkBought(string id)
        {
            if (D.boughtDay != Furniture.Day) { D.boughtDay = Furniture.Day; D.boughtToday.Clear(); }
            D.boughtToday.Add(id);
            Save();
        }
    }
}
