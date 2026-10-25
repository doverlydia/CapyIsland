using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Old Mochi's shrine, on the start island from the very beginning: a little hokora with an
    /// ema rack, and the shrine keeper's spirit sweeping beside it. The Writing hobby lives here —
    /// you hang an ema (a wooden plaque with a wish on it, an offering of one gold coin), and
    /// visiting friends leave theirs. Tap the shrine to look at the rack (UI.LibraryScreen), tap
    /// Mochi to talk and hang one.
    ///
    /// On a friend's island it is their shrine: their ema, and any you have left them.
    ///
    /// The rack keeps what the old beach of message bottles kept, under the same keys, so nothing
    /// written before is lost: your own ema are HobbyLog.Books, friends' are the shore notes.
    /// </summary>
    public class Shrine : MonoBehaviour
    {
        const string ShoreKey = "CapyIsland.ShoreNotes";   // ema friends hung on your rack
        const string LeftKey = "CapyIsland.LeftNotes";     // ema you hung on theirs
        const string SpotKey = "CapyIsland.ShrineSpot";    // where it stands on your island, once found
        const string MetKey = "CapyIsland.MochiMet";
        const string OldBottlesKey = "CapyIsland.MyBottles";
        const float WorldRadius = 46f;                     // its footprint, in original world units

        [Serializable] class Note { public string who, from, title, text; }
        [Serializable] class Notes { public List<Note> list = new(); }

        public static Shrine Instance { get; private set; }

        /// <summary>Where the shrine stands (for "Show me").</summary>
        public static Transform Spot => Instance != null && Instance._root != null ? Instance._root.transform : null;

        GameObject _root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            Attach();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) => Attach();
        }

        static void Attach()
        {
            if (Instance == null) new GameObject("Shrine").AddComponent<Shrine>();
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        IEnumerator Start()
        {
            // After the island (yours, or a friend's) has been built and restored.
            yield return null;
            yield return null;
            if (!Visit.Active) RetireBottles();
            Build();
        }

        // ---------------------------------------------------------------- the shrine in the world

        void Build()
        {
            var land = LandMap.Instance;
            if (land == null) return;
            var at = FindSpot(land);
            if (at == null) return;
            _root = new GameObject("ShrineGrounds");
            _root.transform.position = at.Value;

            var art = HobbyData.Art("Shrine");
            if (art == null) return;
            var shrine = new GameObject("Shrine");
            shrine.transform.SetParent(_root.transform, false);
            var body = new GameObject("Sprite");
            body.transform.SetParent(shrine.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = art;
            shrine.transform.localScale = Vector3.one * (3.4f / Mathf.Max(0.01f, art.bounds.size.x));
            shrine.AddComponent<YSorter>();
            // A footprint, so nothing can be planted on it (never saved: it has no catalogue entry).
            var placeable = shrine.AddComponent<Placeable>();
            placeable.Configure("Shrine", WorldRadius, WorldRadius);
            placeable.MarkPlaced(silent: true);
            shrine.AddComponent<Tappable>().Init(sr, OpenRack);

            MakeMochi(at.Value + new Vector2(-2.1f, -0.5f));
        }

        /// <summary>
        /// A clear spot on the start island, found once and remembered (so it never moves, and
        /// nothing you build later can take its place). On a friend's island, found fresh.
        /// </summary>
        Vector2? FindSpot(LandMap land)
        {
            if (!Visit.Active)
            {
                var saved = PlayerPrefs.GetString(SpotKey, "");
                var parts = saved.Split(',');
                if (parts.Length == 2
                    && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sx)
                    && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sy))
                    return new Vector2(sx, sy);
            }

            var island = land.Island(0);
            if (island == null) return null;
            Vector2 middle = island.rects.Count > 0 ? island.rects[0].center : island.circles.Count > 0 ? (Vector2)island.circles[0] : Vector2.zero;
            var centre = LandMap.WorldToUnity(middle);
            // Up the hill from the middle (the north-west), out of the way of the opening's first steps.
            var wanted = centre + new Vector2(-4f, 4.5f);
            Vector2? found = null;
            for (float r = 0f; r < 22f && found == null; r += 0.8f)
            {
                int around = Mathf.Max(1, Mathf.RoundToInt(r * 4f));
                for (int i = 0; i < around; i++)
                {
                    float a = i * Mathf.PI * 2f / around;
                    var p = wanted + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f) * r;
                    // Room for the shrine and for Mochi beside it.
                    if (land.IslandAt(p) != 0 || !BunniSpace.CircleOnMap(p, WorldRadius) || !BunniSpace.CircleOnMap(p + new Vector2(-2.1f, -0.5f), 18f)) continue;
                    found = p;
                    break;
                }
            }
            if (found != null && !Visit.Active)
            {
                PlayerPrefs.SetString(SpotKey, found.Value.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," + found.Value.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
            }
            return found;
        }

        void MakeMochi(Vector2 at)
        {
            var art = HobbyData.Art("Mochi");
            if (art == null) return;
            var go = new GameObject("OldMochi");
            go.transform.SetParent(_root.transform, false);
            go.transform.position = at;
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = art;
            sr.color = new Color(1f, 1f, 1f, 0.9f);   // a spirit: not quite all there
            go.transform.localScale = Vector3.one * (1.6f / Mathf.Max(0.01f, art.bounds.size.y));
            go.AddComponent<YSorter>();
            go.AddComponent<Floaty>();
            go.AddComponent<Tappable>().Init(sr, () => Talk(sr.sprite));
        }

        // ---------------------------------------------------------------- talking to Mochi

        static void Talk(Sprite face)
        {
            if (Attention.TutorialRunning) { UI.SpeechDialog.Show(face, StoryText.Keeper, new[] { StoryText.Mochi.Busy }, null); return; }

            // Writing opens with a hobby card: until then Mochi says hello, and the rack is just to read.
            bool open = HobbyLog.IsOpened(Hobby.Writing);
            string[] lines;
            Action closed = null;
            if (Visit.Active) lines = new[] { open ? StoryText.Mochi.Visit(Visit.Host.name) : StoryText.HobbyCardLines.MochiWaits };
            else if (PlayerPrefs.GetInt(MetKey, 0) == 0)
            {
                PlayerPrefs.SetInt(MetKey, 1);
                PlayerPrefs.Save();
                lines = StoryText.Mochi.Meet;
                if (!open)
                {
                    // Everything but his offer to hang one: that waits for the card.
                    lines = new string[StoryText.Mochi.Meet.Length];
                    Array.Copy(StoryText.Mochi.Meet, lines, lines.Length - 1);
                    lines[lines.Length - 1] = StoryText.HobbyCardLines.MochiWaits;
                }
                // Old Yuzu has something to say about him, once you've met (when the screen is free).
                closed = () => Attention.Ask("yuzu-met-mochi", () => Hints.Show(StoryText.Yuzu.MetMochi));
            }
            else if (!open) lines = new[] { StoryText.HobbyCardLines.MochiWaits };
            else lines = new[] { StoryText.Mochi.Hello[UnityEngine.Random.Range(0, StoryText.Mochi.Hello.Length)] };

            var store = ResourceStore.Instance;
            bool afford = store != null && store.Get(ResourceKind.Gold) >= 1;
            var choices = new List<UI.SpeechDialog.Choice>();
            if (open) choices.Add(new("Hang an ema (1 gold)", WriteNew, afford));
            choices.Add(new("Look at the rack", OpenRack));
            choices.Add(new("Not now", null));
            UI.SpeechDialog.ShowChoices(face, StoryText.Keeper, lines, closed, choices);
        }

        static void OpenRack() => UI.LibraryScreen.ShowRack();

        /// <summary>Pays the offering and opens a fresh ema to write.</summary>
        public static void WriteNew()
        {
            if (!HobbyLog.IsOpened(Hobby.Writing)) { Hints.Answer(StoryText.HobbyCardLines.WritingClosed); return; }
            var store = ResourceStore.Instance;
            if (store == null || store.Get(ResourceKind.Gold) < 1) { Hints.Answer("An ema is an offering of one gold coin."); return; }
            UI.LibraryScreen.WriteEma((title, text) =>
            {
                if (!store.TrySpend(0, 1, 0)) return;
                Sfx.Play(SfxKind.Buy, 0.5f);
                Hang(title, text);
            });
        }

        /// <summary>A written ema goes up: yours on your rack; on a friend's island, one left for them.</summary>
        static void Hang(string title, string text)
        {
            if (Visit.Active)
            {
                var left = Load(LeftKey);
                left.list.Add(new Note { who = Visit.Host.id, from = "you", title = title, text = text });
                Save(LeftKey, left);
                Hints.Answer($"Your ema hangs on {Visit.Host.name}'s rack. They'll find it!");
            }
            else
            {
                HobbyLog.AddBook(title, text);
                Hints.Answer("Your ema hangs on the rack. Friends who visit can read it!");
            }
            HobbyLog.AddXp(Hobby.Writing, Mathf.Clamp(text.Length / 20, 10, 40));
            Motes.Trigger("hobby_book");
            var fx = HarvestEffects.Instance;
            if (fx != null && Spot != null) fx.Twinkle((Vector2)Spot.position + Vector2.up * 1.6f, 10);
            UI.LibraryScreen.ShowRack();
        }

        // ---------------------------------------------------------------- the rack's ema

        public struct Ema
        {
            public string Title, By, Text;
            /// <summary>Your own, by its index in HobbyLog.Books (so it can be taken down), else -1.</summary>
            public int Mine;
        }

        /// <summary>Everything on this shrine's rack, newest first: yours, friends', the islanders'.</summary>
        public static List<Ema> Rack()
        {
            var list = new List<Ema>();
            if (Visit.Active)
            {
                var left = Load(LeftKey).list;
                for (int i = left.Count - 1; i >= 0; i--)
                    if (left[i].who == Visit.Host.id) list.Add(new Ema { Title = left[i].title, By = "by you", Text = left[i].text, Mine = -1 });
                foreach (var s in BotBooks.ShelfOf(Visit.Host)) list.Add(new Ema { Title = s.Title, By = "by " + Visit.Host.name, Text = s.Text, Mine = -1 });
                return list;
            }
            for (int i = HobbyLog.Books.Count - 1; i >= 0; i--)
                list.Add(new Ema { Title = HobbyLog.Books[i].title, By = "by you", Text = HobbyLog.Books[i].text, Mine = i });
            var notes = Load(ShoreKey).list;
            for (int i = notes.Count - 1; i >= 0; i--) list.Add(new Ema { Title = notes[i].title, By = "from " + notes[i].from, Text = notes[i].text, Mine = -1 });
            foreach (var s in StoryText.IslandEma) list.Add(new Ema { Title = s.title, By = "by " + s.by, Text = s.text, Mine = -1 });
            return list;
        }

        static Notes Load(string key)
        {
            try { return JsonUtility.FromJson<Notes>(PlayerPrefs.GetString(key, "")) ?? new Notes(); }
            catch { return new Notes(); }
        }

        static void Save(string key, Notes n)
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(n));
            PlayerPrefs.Save();
        }

        /// <summary>A visiting friend hangs one of their ema on your rack as they go.</summary>
        public static void LeaveEmaFrom(FriendProfile who)
        {
            if (who == null) return;
            var shelf = BotBooks.ShelfOf(who);
            if (shelf.Count == 0) return;
            var story = shelf[UnityEngine.Random.Range(0, shelf.Count)];
            var notes = Load(ShoreKey);
            if (notes.list.Exists(n => n.from == who.name && n.title == story.Title)) return;
            notes.list.Add(new Note { from = who.name, title = story.Title, text = story.Text });
            while (notes.list.Count > 24) notes.list.RemoveAt(0);
            Save(ShoreKey, notes);
            // A dot on the Hobbies button only once writing is yours: before that its page is a
            // locked card, with nothing to tap that would show (and clear) it.
            if (HobbyLog.IsOpened(Hobby.Writing)) NewStuff.Add("hobbies/Writing/" + who.id + "/" + story.Title);
            Hints.Show(who.name + " hung an ema at Old Mochi's shrine for you!");
        }

        /// <summary>
        /// The message bottles are gone. Everything written in one is already on your rack (a
        /// bottle's note went to your books); an empty bottle still in your trail is refunded.
        /// </summary>
        static void RetireBottles()
        {
            var old = PlayerPrefs.GetString(OldBottlesKey, "");
            if (string.IsNullOrEmpty(old)) return;
            int empty = 0;
            foreach (var part in old.Split(new[] { "\"placed\":false" }, StringSplitOptions.None)) empty++;
            empty--;   // one more piece than matches
            PlayerPrefs.DeleteKey(OldBottlesKey);
            PlayerPrefs.Save();
            if (empty > 0 && ResourceStore.Instance != null) ResourceStore.Instance.Add(ResourceKind.Gold, empty);
        }

        // ---------------------------------------------------------------- the pieces

        /// <summary>Tap the picture to do something.</summary>
        class Tappable : MonoBehaviour, ITappable
        {
            SpriteRenderer _sr;
            Action _tap;

            public void Init(SpriteRenderer sr, Action tap) { _sr = sr; _tap = tap; }

            void OnEnable() => TapTargets.Register(this);
            void OnDisable() => TapTargets.Unregister(this);

            public float TapDistance(Vector2 worldPoint)
            {
                if (_sr == null) return -1f;
                var b = _sr.bounds;
                return b.Contains(new Vector3(worldPoint.x, worldPoint.y, b.center.z)) ? Vector2.Distance(b.center, worldPoint) : -1f;
            }

            public void Tap()
            {
                Sfx.Play(SfxKind.Tap, 0.5f);
                _tap?.Invoke();
            }
        }

        /// <summary>A spirit's float: a slow rise and fall.</summary>
        class Floaty : MonoBehaviour
        {
            Transform _body;
            void Start() => _body = transform.Find("Sprite");
            void Update()
            {
                if (_body != null) _body.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 1.4f) * 0.06f / Mathf.Max(0.01f, transform.localScale.y), 0f);
            }
        }
    }
}
