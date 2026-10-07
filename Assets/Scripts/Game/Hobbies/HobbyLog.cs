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
        [Serializable] public class Design { public string name; public string pixels; public int size; }
        /// <summary>An adopted cat lives around <c>home</c>; <c>unplaced</c> while it waits in a bubble or your trail for one.</summary>
        [Serializable] public class Cat { public string id; public int bond; public bool adopted; public long lastFussTicks; public Vector2 home; public bool unplaced; }
        /// <summary><c>unplaced</c> while the plot waits in a bubble or your trail (false, so older saves stay put).</summary>
        [Serializable] public class Plot { public Vector2 pos; public string crop; public long plantedTicks; public bool unplaced; }

        [Serializable]
        class Data
        {
            public int[] xp = new int[8];
            public List<Entry> fish = new(), birds = new(), bugs = new(), bag = new();
            public List<Book> books = new();
            public List<Design> designs = new();
            public List<Cat> cats = new();
            public List<Plot> plots = new();
            public int wornDesign = -1;
        }

        static Data _data;

        public static event Action<Hobby, int> LevelledUp;
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            _data = null;
            LevelledUp = null;
            Changed = null;
        }

        static Data D
        {
            get
            {
                if (_data != null) return _data;
                var json = PlayerPrefs.GetString(Key, "");
                try { _data = string.IsNullOrEmpty(json) ? new Data() : JsonUtility.FromJson<Data>(json) ?? new Data(); }
                catch { _data = new Data(); }
                // Older saves knew fewer hobbies: keep their experience, make room for the rest.
                if (_data.xp == null) _data.xp = new int[HobbyCount];
                if (_data.xp.Length < HobbyCount) Array.Resize(ref _data.xp, HobbyCount);
                _data.fish ??= new(); _data.birds ??= new(); _data.bugs ??= new(); _data.bag ??= new();
                _data.books ??= new(); _data.designs ??= new(); _data.cats ??= new(); _data.plots ??= new();
                return _data;
            }
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(D));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- levels

        /// <summary>Experience needed to reach each level: gentle at first, steeper later.</summary>
        public static int XpForLevel(int level) => level <= 1 ? 0 : 40 * (level - 1) * level;

        public static int Xp(Hobby h) => D.xp[(int)h];

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

        /// <summary>Logs a catch. Returns (first of its kind, a new record size).</summary>
        public static (bool first, bool record) LogFish(string id, float size)
        {
            var e = FishEntry(id);
            bool first = e == null;
            if (first) D.fish.Add(e = new Entry { id = id });
            bool record = !first && size > e.best;
            e.count++;
            e.best = Mathf.Max(e.best, size);
            AddToBag("fish:" + id, 1, save: false);
            Save();
            return (first, record);
        }

        public static bool LogBird(string id) => Log(D.birds, id);
        public static bool LogBug(string id) => Log(D.bugs, id);

        static bool Log(List<Entry> list, string id)
        {
            var e = list.Find(x => x.id == id);
            bool first = e == null;
            if (first) list.Add(e = new Entry { id = id });
            e.count++;
            Save();
            return first;
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

        public static int SaveDesign(string name, string pixels, int size, int replace = -1)
        {
            var design = new Design { name = name, pixels = pixels, size = size };
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
