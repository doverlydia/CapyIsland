using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    public enum Hobby { Fishing, Birding, Writing, Sewing, Gardening, Cooking, Bugs, Cats }

    /// <summary>
    /// Everything the hobbies remember: experience and level per hobby, the journals (fish, birds,
    /// bugs), the bag (fish, crops and dishes to cook and share), the cats you have befriended, your
    /// garden plots, books written and designs sewn. Kept apart from the island save, like the
    /// wardrobe — hobbies belong to the player, not to any one island.
    /// </summary>
    public static class HobbyLog
    {
        const string Key = "CapyIsland.Hobbies";
        public static readonly int HobbyCount = Enum.GetValues(typeof(Hobby)).Length;

        [Serializable] public class Entry { public string id; public int count; public float best; }
        [Serializable] public class Book { public string title; public string text; public long writtenTicks; }
        /// <summary>A sewn piece: <c>kind</c> is "hat" ("cape" for older ones, empty for the oldest shirts).</summary>
        [Serializable] public class Design { public string name; public string pixels; public int size; public string kind; }
        /// <summary>
        /// An adopted cat lives around <c>home</c>; <c>unplaced</c> while it waits in a bubble or your trail for one.
        /// <c>name</c> is what you called it (empty: its own name). <c>freeGift</c>: the present on its way is one
        /// it found by itself, not one a fish paid for. <c>outfit</c>: the wardrobe pieces it has on (CatWear).
        /// </summary>
        [Serializable] public class Cat { public string id; public int bond; public bool adopted; public long lastFussTicks; public Vector2 home; public bool unplaced; public long giftDueTicks; public int gifts; public int fed;
            public string name; public bool freeGift; public List<string> outfit = new();
            /// <summary>Horaira only: you took his halo off (Horaira.WornHalo).</summary>
            public bool bareHead;
            /// <summary>Has ever had a fish from you (older saves only know it from presents).</summary>
            public bool EverFed => fed > 0 || gifts > 0 || giftDueTicks != 0; }
        /// <summary><c>unplaced</c> while the plot waits in a bubble or your trail (false, so older saves stay put).</summary>
        [Serializable] public class Plot { public Vector2 pos; public string crop; public long plantedTicks; public bool unplaced; }

        [Serializable]
        class Data
        {
            public int[] xp = new int[8];
            public List<Entry> fish = new(), birds = new(), bugs = new(), bag = new();
            public List<Entry> crops;   // the vegetable journal: what you have harvested, and how much
            public List<Book> books = new();
            public List<Design> designs = new();
            public List<Cat> cats = new();
            public List<Plot> plots = new();
            public int wornDesign = -1;
            public List<string> unlocked = new();   // hobbies whose gear you have bought
            public List<string> recipes;   // dishes you know how to cook (null in saves from before recipes)
            // Hobbies open to take up (a hobby card, or from before cards): null in saves from before them.
            public List<string> opened;
            public int cardsSpent;   // hobby cards used (an old save counts what it already had open)
            public int cardsShown;   // cards announced so far, so each new one is offered once
            public string freeGear;  // the first card's gift ("Bug Net"), still in its bubble; null once popped

            // The island's shop pacing (ShopPacing). It rides along with the hobbies so it travels
            // with the save code and the cloud, and starts afresh with a new island.
            public bool pestSeen;            // a pest has turned up: the bombs are on the shelf
            public List<string> oldStock;    // what an island from before pacing already had for sale
            public bool stockPending;        // that list is still to be taken, once the island has loaded
        }

        static Data _data;

        public static event Action<Hobby, int> LevelledUp;
        public static event Action Changed;
        /// <summary>A hobby has just been opened with a hobby card.</summary>
        public static event Action<Hobby> Opened;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            _data = null;
            LevelledUp = null;
            Changed = null;
            Opened = null;
        }

        static Data D
        {
            get
            {
                if (_data != null) return _data;
                var json = PlayerPrefs.GetString(Key, "");
                bool hadData = !string.IsNullOrEmpty(json);
                try { _data = !hadData ? new Data() : JsonUtility.FromJson<Data>(json) ?? new Data(); }
                catch { _data = new Data(); }
                // Older saves knew fewer hobbies: keep their experience, make room for the rest.
                if (_data.xp == null) _data.xp = new int[HobbyCount];
                if (_data.xp.Length < HobbyCount) Array.Resize(ref _data.xp, HobbyCount);
                _data.fish ??= new(); _data.birds ??= new(); _data.bugs ??= new(); _data.bag ??= new();
                _data.books ??= new(); _data.designs ??= new(); _data.cats ??= new(); _data.plots ??= new();
                if (_data.crops == null)
                {
                    // Saves from before the vegetable journal: whatever is in the bag was grown.
                    _data.crops = new();
                    foreach (var e in _data.bag)
                        if (e.id.StartsWith("crop:") && e.count > 0) _data.crops.Add(new Entry { id = e.id.Substring(5), count = e.count });
                }
                _data.unlocked ??= new();
                _data.oldStock ??= new();
                if (_data.opened == null) OpenOldHobbies(hadData);
                if (_data.recipes == null) KnowOldRecipes();
                return _data;
            }
        }

        public static void Save()
        {
            Store();
            Changed?.Invoke();
        }

        /// <summary>Writes the hobbies to disk without telling anyone (bookkeeping, not news).</summary>
        static void Store()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(D));
            PlayerPrefs.Save();
        }

        // ---------------------------------------------------------------- opening hobbies

        /// <summary>
        /// Hobbies arrived all at once before hobby cards. Someone who was already playing then keeps
        /// everything they had: cats and writing (always there in those days) and any hobby they had
        /// taken up or made a start in, with the cards their hearts have earned beyond those still to
        /// spend. A new player starts with fishing alone. Decided once and written straight away, so
        /// the island's first save can't make a new player look like an old one.
        /// </summary>
        static void OpenOldHobbies(bool hadData)
        {
            _data.opened = new();
            bool veteran = hadData || PastTutorial();
            foreach (Hobby h in Enum.GetValues(typeof(Hobby)))
                if (h != Hobby.Fishing && (Played(_data, h) || (veteran && h is Hobby.Cats or Hobby.Writing)))
                    _data.opened.Add(h.ToString());
            _data.cardsSpent = _data.opened.Count;
            _data.cardsShown = 0;
            // The island's shops already had their wares out: they stay, once we've seen which.
            _data.stockPending = veteran;
            Store();
        }

        /// <summary>An island saved after the tutorial: someone who has been playing.</summary>
        static bool PastTutorial()
        {
            if (!SaveGame.HasSave) return false;
            try
            {
                var save = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveGame.Key, ""));
                return save != null && string.IsNullOrEmpty(save.tutorial);
            }
            catch { return false; }
        }

        /// <summary>Gear bought, experience earned, or something made: a start in the hobby.</summary>
        static bool Played(Data d, Hobby h)
        {
            if ((int)h < d.xp.Length && d.xp[(int)h] > 0) return true;
            if (d.unlocked.Contains(h.ToString())) return true;
            return h switch
            {
                Hobby.Gardening => d.plots.Count > 0,
                Hobby.Writing => d.books.Count > 0,
                Hobby.Sewing => d.designs.Count > 0,
                Hobby.Cats => d.cats.Exists(c => c.adopted || c.bond > 0),
                _ => false,
            };
        }

        /// <summary>
        /// Whether a hobby is open to take up: fishing always; the rest once a hobby card has opened
        /// them (or you had made a start before cards). Open is not yet playing: the gear goes on
        /// sale, the character offers it, but buying it is still <see cref="IsUnlocked"/>.
        /// </summary>
        public static bool IsOpened(Hobby h) => h == Hobby.Fishing || D.opened.Contains(h.ToString()) || Played(D, h);

        public static int CardsSpent => D.cardsSpent;

        /// <summary>How many cards have been announced: set as each new one is offered.</summary>
        public static int CardsShown
        {
            get => D.cardsShown;
            set { if (D.cardsShown == value) return; D.cardsShown = value; Store(); }
        }

        /// <summary>
        /// The first hobby card's gift, its gear ("Fishing Rod"), while it waits in a bubble by the
        /// king — kept, so a bubble lost to closing the app comes back. Null once it's popped.
        /// </summary>
        public static string FreeGear
        {
            get => string.IsNullOrEmpty(D.freeGear) ? null : D.freeGear;
            set { if (FreeGear == value) return; D.freeGear = value; Store(); }
        }

        /// <summary>
        /// Spends a hobby card on a closed hobby. True if it opened. Fishing is always open, but
        /// the first card can still take it up when you have no rod yet: the rod is its gift.
        /// </summary>
        public static bool OpenWithCard(Hobby h)
        {
            bool rodFirst = h == Hobby.Fishing && D.cardsSpent == 0 && !IsUnlocked(h);
            if (IsOpened(h) && !rodFirst) return false;
            if (!rodFirst) D.opened.Add(h.ToString());
            D.cardsSpent++;
            NewStuff.Add("hobbies/" + h);
            Save();
            Opened?.Invoke(h);
            return true;
        }

        // ---------------------------------------------------------------- the island's shop pacing

        /// <summary>A pest has turned up (ShopPacing): set once, kept.</summary>
        public static bool PestSeen
        {
            get => D.pestSeen;
            set { if (D.pestSeen == value) return; D.pestSeen = value; Store(); }
        }

        /// <summary>True until an island from before pacing has had its wares on show noted.</summary>
        public static bool StockPending => D.stockPending;

        public static bool WasInStock(string name) => D.oldStock.Contains(name);

        /// <summary>What an island from before pacing already had for sale: it stays for sale.</summary>
        public static void KeepOldStock(IEnumerable<string> names)
        {
            foreach (var n in names) if (!D.oldStock.Contains(n)) D.oldStock.Add(n);
            D.stockPending = false;
            Store();
        }

        /// <summary>
        /// A brand-new island: no pests yet, nothing on the shelves from before, and the cards are
        /// announced afresh. The hobbies themselves (and the cards already spent) are the player's
        /// and stay.
        /// </summary>
        public static void NewIsland()
        {
            var d = D;
            if (!d.pestSeen && d.oldStock.Count == 0 && !d.stockPending && d.cardsShown == 0) return;
            d.pestSeen = false;
            d.oldStock.Clear();
            d.stockPending = false;
            d.cardsShown = 0;
            Store();
        }

        // ---------------------------------------------------------------- levels

        /// <summary>Experience needed to reach each level: gentle at first, steeper later.</summary>
        public static int XpForLevel(int level) => level <= 1 ? 0 : 40 * (level - 1) * level;

        public static int Xp(Hobby h) => D.xp[(int)h];

        /// <summary>
        /// Whether you can play a hobby now. Cats and writing need nothing more than to be open
        /// (strays start to wander in; Old Mochi lets you write); the rest need their gear too.
        /// Anything you have already made progress in stays playable.
        /// </summary>
        public static bool IsUnlocked(Hobby h)
        {
            if (h == Hobby.Cats || h == Hobby.Writing) return IsOpened(h);
            var d = D;
            if ((int)h < d.xp.Length && d.xp[(int)h] > 0) return true;
            if (h == Hobby.Gardening && d.plots.Count > 0) return true;
            return d.unlocked.Contains(h.ToString());
        }

        /// <summary>Unlocks a hobby (its gear was bought). True if it was locked before.</summary>
        public static bool Unlock(Hobby h)
        {
            if (IsUnlocked(h)) return false;
            D.unlocked.Add(h.ToString());
            if (h != Hobby.Fishing && !D.opened.Contains(h.ToString())) D.opened.Add(h.ToString());   // playing it, so open
            NewStuff.Add("hobbies/" + h);   // the hobby itself (finds go under "hobbies/<hobby>/")
            GiveFreeRecipes(h);
            Save();
            Changed?.Invoke();
            return true;
        }

        /// <summary>What to buy, and where, to take a hobby up.</summary>
        public static string HowToUnlock(Hobby h) => h switch
        {
            Hobby.Fishing => "Buy a Fishing Rod from Skipper Nori on the beach",
            Hobby.Bugs => "Buy a Bug Net at the Bronze Store",
            Hobby.Birding => "Buy Binoculars at the Silver Store",
            Hobby.Sewing => "Buy a Sewing Machine at the Steel Store",
            Hobby.Gardening => "Pay the spirit at the south end of Pebble Cove to raise the bridge to Sunflower Farm",
            Hobby.Cooking => "Buy a stove from Chef Tofu on Sunflower Farm",
            _ => "",
        };

        public static int Level(Hobby h)
        {
            int xp = Xp(h), level = 1;
            while (xp >= XpForLevel(level + 1)) level++;
            return level;
        }

        /// <summary>Progress from this level to the next, 0–1.</summary>
        public static float LevelProgress(Hobby h)
        {
            int level = Level(h), xp = Xp(h);
            int from = XpForLevel(level), to = XpForLevel(level + 1);
            return Mathf.Clamp01((xp - from) / (float)Mathf.Max(1, to - from));
        }

        public static void AddXp(Hobby h, int amount)
        {
            int before = Level(h);
            D.xp[(int)h] += Mathf.Max(0, amount);
            Save();
            int after = Level(h);
            if (after > before) LevelledUp?.Invoke(h, after);
        }

        // ---------------------------------------------------------------- journals

        public static IReadOnlyList<Entry> Fish => D.fish;
        public static IReadOnlyList<Entry> Birds => D.birds;
        public static IReadOnlyList<Entry> Bugs => D.bugs;

        public static Entry FishEntry(string id) => D.fish.Find(e => e.id == id);
        public static Entry BirdEntry(string id) => D.birds.Find(e => e.id == id);
        public static Entry BugEntry(string id) => D.bugs.Find(e => e.id == id);
        public static IReadOnlyList<Entry> Crops => D.crops;
        public static Entry CropEntry(string id) => D.crops.Find(e => e.id == id);

        /// <summary>A harvest, for the vegetable journal: <paramref name="amount"/> of a crop. True the first time.</summary>
        public static bool LogCrop(string id, int amount)
        {
            var e = CropEntry(id);
            bool first = e == null;
            if (first) { D.crops.Add(e = new Entry { id = id }); NewStuff.Add("hobbies/Gardening/" + id); }
            e.count += amount;
            Save();
            if (first) JournalRewards.Check(Hobby.Gardening);
            return first;
        }

        /// <summary>Logs a catch. Returns (first of its kind, a new record size).</summary>
        public static (bool first, bool record) LogFish(string id, float size)
        {
            var e = FishEntry(id);
            bool first = e == null;
            if (first) { D.fish.Add(e = new Entry { id = id }); NewStuff.Add("hobbies/Fishing/" + id); }
            bool record = !first && size > e.best;
            e.count++;
            e.best = Mathf.Max(e.best, size);
            AddToBag("fish:" + id, 1, save: false);
            Save();
            GameEvents.Record(Hobby.Fishing, HobbyData.FishById(id)?.Stars ?? 1);
            if (first) JournalRewards.Check(Hobby.Fishing);
            return (first, record);
        }

        public static bool LogBird(string id)
        {
            bool first = Log(D.birds, id);
            if (first) NewStuff.Add("hobbies/Birding/" + id);
            GameEvents.Record(Hobby.Birding, HobbyData.BirdById(id)?.Stars ?? 1);
            if (first) JournalRewards.Check(Hobby.Birding);
            return first;
        }

        public static bool LogBug(string id)
        {
            bool first = Log(D.bugs, id);
            if (first) NewStuff.Add("hobbies/Bugs/" + id);
            GameEvents.Record(Hobby.Bugs, HobbyData.Bug(id)?.Stars ?? 1);
            if (first) JournalRewards.Check(Hobby.Bugs);
            return first;
        }

        static bool Log(List<Entry> list, string id)
        {
            var e = list.Find(x => x.id == id);
            bool first = e == null;
            if (first) list.Add(e = new Entry { id = id });
            e.count++;
            Save();
            return first;
        }

        // ---------------------------------------------------------------- recipes

        /// <summary>
        /// The dishes you know how to cook. Taking up gardening or fishing gives you a first recipe;
        /// Chef Tofu sells the rest; and a pot that happens to hold a recipe teaches it to you.
        /// </summary>
        public static IReadOnlyList<string> Recipes => D.recipes;

        public static bool KnowsRecipe(string dishId) => D.recipes.Contains(dishId);

        /// <summary>Learns a recipe. True if it was new.</summary>
        public static bool LearnRecipe(string dishId, bool save = true)
        {
            if (string.IsNullOrEmpty(dishId) || D.recipes.Contains(dishId)) return false;
            D.recipes.Add(dishId);
            if (save) Save();
            return true;
        }

        static void GiveFreeRecipes(Hobby h)
        {
            foreach (var d in HobbyData.Dishes)
                if (d.FreeWith == h) LearnRecipe(d.Id, save: false);
        }

        /// <summary>
        /// A save from before recipes: whoever cooked already knows every dish they could make
        /// then (the ones without pantry goods, up to their level), and every hobby taken up gives
        /// its free recipe, as it would now.
        /// </summary>
        static void KnowOldRecipes()
        {
            _data.recipes = new();
            if (_data.xp[(int)Hobby.Cooking] > 0 || _data.unlocked.Contains(Hobby.Cooking.ToString()))
            {
                int level = Level(Hobby.Cooking);
                foreach (var d in HobbyData.Dishes)
                {
                    bool pantry = false;
                    foreach (var (item, _) in d.Needs) pantry |= item.StartsWith("pantry:");
                    if (!pantry && d.MinLevel <= level) _data.recipes.Add(d.Id);
                }
            }
            foreach (Hobby h in Enum.GetValues(typeof(Hobby)))
                if (IsUnlocked(h)) GiveFreeRecipes(h);
        }

        // ---------------------------------------------------------------- the bag

        /// <summary>Everything carried: "fish:&lt;id&gt;", "crop:&lt;id&gt;", "dish:&lt;id&gt;".</summary>
        public static IReadOnlyList<Entry> Bag => D.bag;

        public static int Count(string id) => D.bag.Find(e => e.id == id)?.count ?? 0;

        /// <summary>How many fish of any kind are in the bag.</summary>
        public static int FishInBag()
        {
            int n = 0;
            foreach (var e in D.bag) if (e.id.StartsWith("fish:")) n += e.count;
            return n;
        }

        public static void AddToBag(string id, int n = 1, bool save = true)
        {
            var e = D.bag.Find(x => x.id == id);
            if (e == null) D.bag.Add(e = new Entry { id = id });
            e.count += n;
            if (save) Save();
        }

        /// <summary>Takes items out; "fish:any" takes whichever fish there are most of.</summary>
        public static bool Take(string id, int n = 1)
        {
            if (id == "fish:any")
            {
                if (FishInBag() < n) return false;
                for (int i = 0; i < n; i++)
                {
                    Entry most = null;
                    foreach (var e in D.bag) if (e.id.StartsWith("fish:") && e.count > 0 && (most == null || e.count > most.count)) most = e;
                    most.count--;
                }
            }
            else
            {
                var e = D.bag.Find(x => x.id == id);
                if (e == null || e.count < n) return false;
                e.count -= n;
            }
            D.bag.RemoveAll(x => x.count <= 0);
            Save();
            return true;
        }

        public static bool Has(string id, int n) => id == "fish:any" ? FishInBag() >= n : Count(id) >= n;

        // ---------------------------------------------------------------- cats

        public static IReadOnlyList<Cat> Cats => D.cats;

        /// <summary>Presents brought by every cat so far.</summary>
        public static int TotalCatGifts()
        {
            int n = 0;
            foreach (var c in D.cats) n += c.gifts;
            return n;
        }

        public static Cat CatState(string id)
        {
            var c = D.cats.Find(x => x.id == id);
            if (c == null) D.cats.Add(c = new Cat { id = id });
            return c;
        }

        // ---------------------------------------------------------------- garden plots

        public static List<Plot> Plots => D.plots;

        // ---------------------------------------------------------------- books and designs

        public static IReadOnlyList<Book> Books => D.books;

        public static void AddBook(string title, string text)
        {
            D.books.Add(new Book { title = title, text = text, writtenTicks = DateTime.Now.Ticks });
            Save();
        }

        public static void RemoveBook(int index)
        {
            if (index < 0 || index >= D.books.Count) return;
            D.books.RemoveAt(index);
            Save();
        }

        public static IReadOnlyList<Design> Designs => D.designs;
        public static int WornDesign => D.wornDesign;

        public static int SaveDesign(string name, string pixels, int size, int replace = -1, string kind = "")
        {
            var design = new Design { name = name, pixels = pixels, size = size, kind = kind };
            if (replace >= 0 && replace < D.designs.Count) D.designs[replace] = design;
            else { D.designs.Add(design); replace = D.designs.Count - 1; }
            Save();
            return replace;
        }

        public static void Wear(int design)
        {
            D.wornDesign = design;
            Save();
        }
    }
}
