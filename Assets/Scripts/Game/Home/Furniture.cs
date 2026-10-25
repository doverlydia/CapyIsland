using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>What you do with a piece when you tap it (outside Decorate mode).</summary>
    public enum FurnitureUse
    {
        Look,    // admire it: a heart
        Sit,     // hop up and sit (chairs, the sofa, cushions)
        Lie,     // a nap (the bed): little z's
        Swing,   // sit and sway (the hammock, the hanging chair)
        Soak,    // in up to the neck, towel on head (the hot tub, the bath)
        Tuck,    // legs under the blanket (the kotatsu)
        Music,   // play it (the piano, the drum, the radio): notes
        Light,   // switch it on or off (lamps, lanterns)
        Warm,    // sit by the fire
        Read,    // a book off the shelf, or a letter at the desk
        Dress,   // the wardrobe opens (the dresser, the mirror, the wardrobe)
        Watch,   // the telly, the fish tank, the stars through the telescope
        Cook,    // stir a pot on the stove
        Snack,   // something nice from the icebox
        Wash,    // bubbles at the basin
        Toy,     // a toy out of the chest
        Game,    // a go on the arcade
        Album,   // your photo album opens
    }

    /// <summary>One piece of furniture for your burrow: its picture, its price, how big it stands, what it's for.</summary>
    public class FurnitureDef
    {
        public string Id, Name;
        public int Wood, Gold, Gems;
        /// <summary>How wide its picture (margin and all) is drawn in the room, as a share of a phone's width.</summary>
        public float Width;
        /// <summary>Flat on the floor (a rug): always drawn under everything else.</summary>
        public bool Flat;
        public FurnitureUse Use;
        /// <summary>
        /// For sitting, lying, soaking: how high up its picture the capybara sits, as a share of its
        /// height from its foot. For a soak, where the water is.
        /// </summary>
        public float Seat = 0.3f;
        /// <summary>Never sold: comes with the burrow (the photo album).</summary>
        public bool Free;

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
            new() { Id = "Rug",       Name = "Woven Rug",         Wood = 120, Gold = 80,  Width = 0.6f, Flat = true, Use = FurnitureUse.Sit, Seat = 0f },
            new() { Id = "Bed",       Name = "Hay Bed",           Wood = 300, Gold = 150, Width = 0.46f, Use = FurnitureUse.Lie, Seat = 0.32f },
            new() { Id = "Armchair",  Name = "Comfy Armchair",    Wood = 200, Gold = 200, Width = 0.36f, Use = FurnitureUse.Sit, Seat = 0.3f },
            new() { Id = "Table",     Name = "Round Table",       Wood = 250, Gold = 100, Width = 0.32f },
            new() { Id = "Stool",     Name = "Mushroom Stool",    Wood = 60,  Gold = 60,  Width = 0.2f, Use = FurnitureUse.Sit, Seat = 0.5f },
            new() { Id = "Bookshelf", Name = "Bookshelf",         Wood = 350, Gold = 150, Width = 0.44f, Use = FurnitureUse.Read },
            new() { Id = "Lamp",      Name = "Floor Lamp",        Wood = 100, Gold = 150, Width = 0.44f, Use = FurnitureUse.Light },
            new() { Id = "Plant",     Name = "Potted Fern",       Wood = 50,  Gold = 100, Width = 0.32f },
            new() { Id = "Dresser",   Name = "Chest of Drawers",  Wood = 300, Gold = 200, Width = 0.38f, Use = FurnitureUse.Dress },
            new() { Id = "Painting",  Name = "Sea Painting",      Wood = 100, Gold = 250, Width = 0.36f },
            new() { Id = "ToyChest",  Name = "Toy Chest",         Wood = 150, Gold = 150, Width = 0.32f, Use = FurnitureUse.Toy },
            new() { Id = "Radio",     Name = "Old Radio",         Wood = 100, Gold = 300, Width = 0.3f, Use = FurnitureUse.Music },
            new() { Id = "Aquarium",  Name = "Fish Tank",         Gems = 15,  Width = 0.34f, Use = FurnitureUse.Watch },
            new() { Id = "HotTub",    Name = "Little Hot Tub",    Gems = 25,  Width = 0.46f, Use = FurnitureUse.Soak, Seat = 0.5f },
            new() { Id = "Piano",     Name = "Tiny Piano",        Gems = 20,  Width = 0.42f, Use = FurnitureUse.Music },
            new() { Id = "Fireplace", Name = "Stone Fireplace",   Gems = 20,  Width = 0.48f, Use = FurnitureUse.Warm },

            // More rooms, more to put in them (2026-10-25): a kitchen, a bathroom, a den, a study…
            new() { Id = "Sofa",       Name = "Cosy Sofa",         Wood = 400, Gold = 300, Width = 0.58f, Use = FurnitureUse.Sit, Seat = 0.24f },
            new() { Id = "Cushion",    Name = "Floor Cushion",     Wood = 60,  Gold = 60,  Width = 0.24f, Use = FurnitureUse.Sit, Seat = 0.22f },
            new() { Id = "BeanBag",    Name = "Bean Bag",          Wood = 120, Gold = 150, Width = 0.32f, Use = FurnitureUse.Sit, Seat = 0.28f },
            new() { Id = "Hammock",    Name = "Rope Hammock",      Wood = 250, Gold = 150, Width = 0.58f, Use = FurnitureUse.Swing, Seat = 0.36f },
            new() { Id = "Stove",      Name = "Little Stove",      Wood = 300, Gold = 250, Width = 0.36f, Use = FurnitureUse.Cook },
            new() { Id = "Icebox",     Name = "Mint Icebox",       Wood = 250, Gold = 300, Width = 0.32f, Use = FurnitureUse.Snack },
            new() { Id = "Desk",       Name = "Writing Desk",      Wood = 300, Gold = 200, Width = 0.42f, Use = FurnitureUse.Read },
            new() { Id = "Telly",      Name = "Old Telly",         Wood = 200, Gold = 400, Width = 0.36f, Use = FurnitureUse.Watch },
            new() { Id = "Taiko",      Name = "Taiko Drum",        Wood = 250, Gold = 250, Width = 0.32f, Use = FurnitureUse.Music },
            new() { Id = "Mirror",     Name = "Standing Mirror",   Wood = 200, Gold = 250, Width = 0.3f, Use = FurnitureUse.Dress },
            new() { Id = "Lantern",    Name = "Paper Lantern",     Wood = 80,  Gold = 120, Width = 0.24f, Use = FurnitureUse.Light },
            new() { Id = "Basin",      Name = "Washstand",         Wood = 180, Gold = 150, Width = 0.32f, Use = FurnitureUse.Wash },
            new() { Id = "Bonsai",     Name = "Bonsai",            Wood = 80,  Gold = 200, Width = 0.24f },
            new() { Id = "Wardrobe",   Name = "Wardrobe",          Wood = 400, Gold = 250, Width = 0.42f, Use = FurnitureUse.Dress },
            new() { Id = "Bathtub",    Name = "Bubble Bath",       Gems = 20,  Width = 0.5f, Use = FurnitureUse.Soak, Seat = 0.4f },
            new() { Id = "Kotatsu",    Name = "Kotatsu",           Gems = 18,  Width = 0.46f, Use = FurnitureUse.Tuck, Seat = 0.3f },
            new() { Id = "Telescope",  Name = "Brass Telescope",   Gems = 15,  Width = 0.32f, Use = FurnitureUse.Watch },
            new() { Id = "SwingChair", Name = "Hanging Chair",     Gems = 15,  Width = 0.36f, Use = FurnitureUse.Swing, Seat = 0.34f },
            new() { Id = "Arcade",     Name = "Arcade Machine",    Gems = 22,  Width = 0.32f, Use = FurnitureUse.Game },

            // Comes with the burrow, never sold.
            new() { Id = Album,        Name = "Photo Album",       Free = true, Width = 0.3f, Use = FurnitureUse.Album },
        };

        /// <summary>The photo album: the one piece every burrow starts with.</summary>
        public const string Album = "Album";

        public static FurnitureDef Get(string id) => Array.Find(All, f => f.Id == id);

        /// <summary>Only pieces whose picture has been drawn go in the shop.</summary>
        static bool Drawn(FurnitureDef f) => f.Art != null;

        /// <summary>Today, counted in whole days, so everyone's shop turns over at midnight.</summary>
        public static int Day => (int)(DateTime.Now.Date - new DateTime(2024, 1, 1)).TotalDays;

        /// <summary>How long until the shop has new pieces in.</summary>
        public static TimeSpan UntilTomorrow => DateTime.Now.Date.AddDays(1) - DateTime.Now;

        /// <summary>Today's seven: six everyday pieces and one fancy one, the same all day.</summary>
        public static List<FurnitureDef> Today()
        {
            var rng = new System.Random(Day * 7919 + 17);
            var plain = new List<FurnitureDef>();
            var fancy = new List<FurnitureDef>();
            foreach (var f in All) if (!f.Free && Drawn(f)) (f.Fancy ? fancy : plain).Add(f);
            var picks = new List<FurnitureDef>();
            for (int i = 0; i < 6 && plain.Count > 0; i++)
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

        /// <summary>A piece standing in one of your rooms. <c>off</c>: a lamp you switched off.</summary>
        [Serializable] public class Placed { public string id; public float x, y; public bool flip; public int room; public bool off; }
        [Serializable] class Owned { public string id; public int count; }
        [Serializable] public class AlbumPage { public string a, b; }   // the photos in its two corners, by id (PhotoLog), or empty
        [Serializable] class Data
        {
            public List<Owned> owned = new();
            public List<Placed> placed = new();
            public int boughtDay = -1;
            public List<string> boughtToday = new();
            public bool shopGiven;   // the Furniture Shop's bubble, waiting in a new burrow, was popped
            public int rooms = 1;    // rooms dug out so far (the first comes with the burrow)
            public int layout;       // 1 once the old one-screen room was spread into the wide room
            public bool albumGiven;  // the photo album was put in your first room
            public List<AlbumPage> album = new();
        }

        /// <summary>What digging out each further room costs, in gold: the second room first.</summary>
        public static readonly int[] RoomPrices = { 10000, 25000, 50000, 100000, 200000, 400000, 700000, 1000000 };

        public static int MaxRooms => RoomPrices.Length + 1;

        /// <summary>Rooms dug out so far (at least one).</summary>
        public static int Rooms => Mathf.Clamp(D.rooms, 1, MaxRooms);

        /// <summary>The gold the next room costs, or -1 when every room is dug out.</summary>
        public static int NextRoomPrice => Rooms >= MaxRooms ? -1 : RoomPrices[Rooms - 1];

        /// <summary>Pays for and digs out the next room. False if you can't afford it (or there are no more).</summary>
        public static bool DigNextRoom()
        {
            int price = NextRoomPrice;
            var store = ResourceStore.Instance;
            if (price < 0 || store == null || !store.TrySpend(0, price, 0)) return false;
            D.rooms = Rooms + 1;
            Save();
            return true;
        }

        public static List<Placed> InRoom(int room)
        {
            var list = new List<Placed>();
            foreach (var p in D.placed) if (p.room == room) list.Add(p);
            return list;
        }

        /// <summary>The album's pages (at least two, blank to start).</summary>
        public static List<AlbumPage> Album
        {
            get
            {
                while (D.album.Count < 2) D.album.Add(new AlbumPage());
                return D.album;
            }
        }

        public static void AddAlbumPage()
        {
            D.album.Add(new AlbumPage());
            Save();
        }

        /// <summary>
        /// The first time down with this version: a burrow arranged in the old one-screen room is
        /// spread into the wide one, by the door.
        /// </summary>
        public static void Settle()
        {
            if (D.layout >= 1) return;
            foreach (var p in D.placed) p.x = 0.06f + Mathf.Clamp01(p.x) * 0.38f;
            D.layout = 1;
            Save();
        }

        /// <summary>The photo album is still waiting in its bubble in your first room (it comes with the burrow).</summary>
        public static bool AlbumWaiting => !D.albumGiven && !D.placed.Exists(p => p.id == Furniture.Album) && InStorage(Furniture.Album) == 0;

        /// <summary>Its bubble popped: the album goes in storage, to stand wherever you like.</summary>
        public static void GiveAlbum()
        {
            if (D.albumGiven) return;
            D.albumGiven = true;
            AddToStorage(Furniture.Album);
        }

        /// <summary>The whole burrow as JSON, for showing to a friend who visits (they can look, not touch).</summary>
        public static string Snapshot() => JsonUtility.ToJson(new Data { placed = D.placed, rooms = D.rooms, layout = D.layout, albumGiven = true });

        /// <summary>A friend's burrow from their <see cref="Snapshot"/>: the pieces by room, and how many rooms.</summary>
        public static (List<Placed> placed, int rooms) Read(string json)
        {
            try
            {
                var d = JsonUtility.FromJson<Data>(json);
                if (d != null) return (d.placed ?? new List<Placed>(), Mathf.Clamp(d.rooms, 1, MaxRooms));
            }
            catch { }
            return (new List<Placed>(), 1);
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
        public static Placed PlaceFromStorage(string id, float x, float y, int room = 0)
        {
            var o = D.owned.Find(v => v.id == id);
            if (o == null || o.count <= 0) return null;
            o.count--;
            var p = new Placed { id = id, x = x, y = y, room = room };
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
