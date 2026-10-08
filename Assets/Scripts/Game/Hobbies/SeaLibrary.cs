using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The sea library: a stretch of beach where message bottles wash up, each with a story
    /// inside. On your island there are tales by Old Yuzu, the Captain, Salty Kombu and Miso, your
    /// own stories, and notes that visiting friends leave you. On a friend's island, their stories
    /// (and any notes you have left them). Skipper Nori sits on the sand selling empty bottles for
    /// a gold coin: write something, and he sets it on the shore for whoever passes by.
    /// </summary>
    public class SeaLibrary : MonoBehaviour
    {
        public const string Skipper = "Skipper Nori";
        const string ShoreKey = "CapyIsland.ShoreNotes";   // notes friends left on your beach
        const string LeftKey = "CapyIsland.LeftNotes";     // notes you left on theirs
        const int MaxBottles = 8;

        [Serializable] class Note { public string who, from, title, text; }
        [Serializable] class Notes { public List<Note> list = new(); }

        public static SeaLibrary Instance { get; private set; }

        /// <summary>The middle of the beach (for "Show me").</summary>
        public static Transform Spot => Instance != null && Instance._root != null ? Instance._root.transform : null;

        GameObject _root;
        Vector2 _shoreDir;   // from the island out to sea, at the beach
        readonly List<GameObject> _bottles = new();

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        IEnumerator Start()
        {
            // After the island (yours, or a friend's) has been built.
            yield return null;
            yield return null;
            Build();
        }

        // ---------------------------------------------------------------- the beach

        void Build()
        {
            var land = LandMap.Instance;
            if (land == null) return;
            var island = land.Island(0);
            if (island == null) return;
            Vector2 middle = island.rects.Count > 0 ? island.rects[0].center : island.circles.Count > 0 ? (Vector2)island.circles[0] : Vector2.zero;
            var centre = LandMap.WorldToUnity(middle);

            // Walk out from the middle until the sand ends; the beach is a step inland from there.
            float start = Visit.Active ? Mathf.Repeat(Visit.Host.seed * 37f, 360f) : 235f;
            for (int turn = 0; turn < 18; turn++)
            {
                float ang = (start + turn * 20f) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Vector2? edge = null;
                for (float d = 1f; d < 40f; d += 0.25f)
                    if (!BunniSpace.PointOnLand(centre + dir * d)) { edge = centre + dir * (d - 0.25f); break; }
                if (edge == null) continue;
                var spot = edge.Value - dir * 1.4f;
                if (!BunniSpace.PointOnLand(spot)) continue;
                _shoreDir = dir;
                _root = new GameObject("SeaLibrary");
                _root.transform.position = spot;
                MakeSailor(spot - new Vector2(-dir.y, dir.x) * 0.2f + dir * 0.5f);
                Refresh();
                return;
            }
        }

        void MakeSailor(Vector2 at)
        {
            var art = HobbyData.Art("Sailor");
            if (art == null) return;
            var go = new GameObject("SkipperNori");
            go.transform.SetParent(_root.transform, false);
            go.transform.position = at;
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = art;
            go.transform.localScale = Vector3.one * (1.7f / Mathf.Max(0.01f, art.bounds.size.y));
            go.AddComponent<YSorter>();
            go.AddComponent<Sailor>().Init(this, sr);
        }

        /// <summary>Lays the bottles out along the sand, newest first.</summary>
        public void Refresh()
        {
            foreach (var b in _bottles) if (b != null) Destroy(b);
            _bottles.Clear();
            if (_root == null) return;
            var tangent = new Vector2(-_shoreDir.y, _shoreDir.x);
            Vector2 centre = _root.transform.position;
            var entries = Entries();
            for (int i = 0; i < entries.Count && i < MaxBottles; i++)
            {
                float along = (i % 2 == 0 ? 1f : -1f) * (1.1f + (i / 2) * 0.95f);
                // Each bottle walks out to its own bit of waterline (the coast curves) and rests on the sand.
                var at = centre + tangent * along;
                if (!BunniSpace.PointOnLand(at)) at = centre;
                for (float d = 0f; d < 6f; d += 0.1f)
                    if (!BunniSpace.PointOnLand(at + _shoreDir * (d + 0.1f))) { at += _shoreDir * Mathf.Max(0f, d - 0.25f - ((i * 37) % 3) * 0.08f); break; }
                _bottles.Add(MakeBottle(at, entries[i], i));
            }
        }

        struct Entry { public string Title, By, Text; }

        List<Entry> Entries()
        {
            var list = new List<Entry>();
            if (Visit.Active)
            {
                foreach (var n in Load(LeftKey).list)
                    if (n.who == Visit.Host.id) list.Add(new Entry { Title = n.title, By = "by you", Text = n.text });
                foreach (var s in BotBooks.ShelfOf(Visit.Host)) list.Add(new Entry { Title = s.Title, By = "by " + Visit.Host.name, Text = s.Text });
                return list;
            }
            for (int i = HobbyLog.Books.Count - 1; i >= 0; i--)
                list.Add(new Entry { Title = HobbyLog.Books[i].title, By = "by you", Text = HobbyLog.Books[i].text });
            var notes = Load(ShoreKey).list;
            for (int i = notes.Count - 1; i >= 0; i--) list.Add(new Entry { Title = notes[i].title, By = "a note from " + notes[i].from, Text = notes[i].text });
            foreach (var s in StoryText.IslandStories) list.Add(new Entry { Title = s.title, By = "by " + s.by, Text = s.text });
            return list;
        }

        GameObject MakeBottle(Vector2 at, Entry entry, int index)
        {
            var art = HobbyData.Art("Bottle");
            var go = new GameObject("Bottle_" + entry.Title);
            go.transform.SetParent(_root.transform, false);
            go.transform.position = at;
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = art;
            sr.flipX = index % 2 == 1;
            if (art != null) go.transform.localScale = Vector3.one * (0.55f / Mathf.Max(0.01f, art.bounds.size.x));
            body.transform.localRotation = Quaternion.Euler(0f, 0f, (index * 23) % 30 - 15);
            go.AddComponent<YSorter>();
            go.AddComponent<Bottle>().Init(entry.Title, entry.By, entry.Text, sr);
            return go;
        }

        // ---------------------------------------------------------------- notes

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

        /// <summary>A visiting friend leaves one of their stories on your beach as they go.</summary>
        public static void LeaveNoteFrom(FriendProfile who)
        {
            if (who == null) return;
            var shelf = BotBooks.ShelfOf(who);
            if (shelf.Count == 0) return;
            var story = shelf[UnityEngine.Random.Range(0, shelf.Count)];
            var notes = Load(ShoreKey);
            if (notes.list.Exists(n => n.from == who.name && n.title == story.Title)) return;
            notes.list.Add(new Note { from = who.name, title = story.Title, text = story.Text });
            while (notes.list.Count > 12) notes.list.RemoveAt(0);
            Save(ShoreKey, notes);
            if (Instance != null) Instance.Refresh();
            Hints.Show(who.name + " left a message bottle on your beach!");
        }

        /// <summary>You wrote something: on your beach it's your story; on a friend's, a note for them.</summary>
        void Written(string title, string text)
        {
            if (Visit.Active)
            {
                var left = Load(LeftKey);
                left.list.Add(new Note { who = Visit.Host.id, from = "you", title = title, text = text });
                Save(LeftKey, left);
                Hints.Show($"Your bottle is on {Visit.Host.name}'s beach. They'll find it!");
            }
            else
            {
                HobbyLog.AddBook(title, text);
                Hints.Show("Your bottle is on the beach. Friends who visit can read it!");
            }
            HobbyLog.AddXp(Hobby.Writing, Mathf.Clamp(text.Length / 20, 10, 40));
            Motes.Trigger("hobby_book");
            Refresh();
            // The new bottle (the first) pops in with a splash of sand.
            if (_bottles.Count > 0 && _bottles[0] != null)
            {
                var t = _bottles[0].transform;
                var full = t.localScale;
                t.localScale = Vector3.zero;
                t.DOScale(full, 0.4f).SetEase(Ease.OutBack);
                var fx = HarvestEffects.Instance;
                if (fx != null) fx.Twinkle((Vector2)t.position + Vector2.up * 0.3f, 8);
            }
        }

        // ---------------------------------------------------------------- the pieces

        /// <summary>A bottle on the sand: tap it to read what's inside.</summary>
        class Bottle : MonoBehaviour, ITappable
        {
            string _title, _by, _text;
            SpriteRenderer _sr;
            float _phase;

            public void Init(string title, string by, string text, SpriteRenderer sr)
            {
                _title = title; _by = by; _text = text; _sr = sr;
                _phase = UnityEngine.Random.value * 6f;
            }

            void OnEnable() => TapTargets.Register(this);
            void OnDisable() => TapTargets.Unregister(this);

            void Update()
            {
                // Rocking gently, as if the tide just set it down.
                if (_sr != null) _sr.transform.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.time * 1.3f + _phase)) * 0.06f / Mathf.Max(0.01f, transform.localScale.y), 0f);
            }

            public float TapDistance(Vector2 worldPoint)
            {
                if (_sr == null) return -1f;
                float d = Vector2.Distance(_sr.bounds.center, worldPoint);
                return d < 0.6f ? d : -1f;
            }

            public void Tap()
            {
                Sfx.Play(SfxKind.Pop, 0.5f);
                UI.LibraryScreen.ReadBottle(_title, _by, _text);
            }
        }

        /// <summary>Skipper Nori: a bottle for a gold coin.</summary>
        class Sailor : MonoBehaviour, ITappable
        {
            SeaLibrary _library;
            SpriteRenderer _sr;

            public void Init(SeaLibrary library, SpriteRenderer sr) { _library = library; _sr = sr; }

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
                var store = ResourceStore.Instance;
                bool afford = store != null && store.Get(ResourceKind.Gold) >= 1;
                string line = Visit.Active
                    ? $"Ahoy! Leave {Visit.Host.name} a message in a bottle? One gold coin, and I'll set it on the sand for 'em."
                    : "Ahoy! Fancy a message bottle? One gold coin. Write what you like and I'll set it on the shore for anyone passing by.";
                UI.SpeechDialog.ShowChoices(_sr.sprite, Skipper, new[] { line }, null, new List<UI.SpeechDialog.Choice>
                {
                    new("Buy a bottle (1 gold)", () =>
                    {
                        var s = ResourceStore.Instance;
                        if (s == null || !s.TrySpend(0, 1, 0)) return;
                        Sfx.Play(SfxKind.Buy, 0.6f);
                        UI.LibraryScreen.WriteBottle(_library.Written);
                    }, afford),
                    new("Not now", null),
                });
            }
        }
    }
}
