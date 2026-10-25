using System;
using System.Collections.Generic;
using CapyIsland.Game;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// Which "How do I…?" pages you have reached. A page turns up in the help only once the thing it
    /// explains has come into your game (a sawmill planted, the Friends button arrived, a pest seen),
    /// so a new player opens Help to a short list instead of all of it at once. Each page says when
    /// with its <see cref="HelpTopic.Unlocked"/>; a page without one is there from the start.
    ///
    /// Every few seconds this looks at those, and a page reached for the first time is remembered for
    /// good and wears a red dot (NewStuff "help/&lt;id&gt;") on the Help button and on its row until
    /// it is opened. Quietly, with no dot: on the first look at an island (one from before this
    /// remembers everything it has already reached at once, rather than lighting up forty dots),
    /// and while Old Yuzu is still showing you round, since she is teaching those very things.
    ///
    /// Kept in preferences beside the side buttons (<see cref="HudReveal"/>), outside the island
    /// save. Visits look at nothing: a friend's island isn't your progress.
    /// </summary>
    public class HelpUnlocks : MonoBehaviour
    {
        const string Key = "CapyIsland.HelpSeen";
        const string NewPrefix = "help/";
        /// <summary>Seconds between looks.</summary>
        const float Every = 3f;
        /// <summary>Let the save load, the shops set out their wares and the side buttons arrive first.</summary>
        const float Settle = 3f;
        /// <summary>The first look at an island is a quiet catch-up, this long, for things that come in a beat late.</summary>
        const float CatchUp = 5f;

        public static HelpUnlocks Instance { get; private set; }

        static HashSet<string> _known;
        static bool _hooked;

        float _loadedAt, _nextLook, _quietUntil = -1f;

        // ---------------------------------------------------------------- what's known

        static HashSet<string> Known
        {
            get
            {
                if (_known != null) return _known;
                _known = new HashSet<string>();
                foreach (var id in PlayerPrefs.GetString(Key, "").Split(','))
                    if (id.Length > 0) _known.Add(id);
                return _known;
            }
        }

        /// <summary>Is this page in the help? (Always, for a page that's there from the start.)</summary>
        public static bool Available(HelpTopic topic) => topic != null && (topic.Unlocked == null || Known.Contains(topic.Id));

        /// <summary>Reached, and not opened since: it wears a red dot.</summary>
        public static bool IsNew(string id) => NewStuff.Has(NewPrefix + id);

        /// <summary>Any page still to open? (The Help button's dot.)</summary>
        public static bool AnyNew => NewStuff.Any(NewPrefix);

        /// <summary>The page was opened: no more dot, and it stays in the help from now on.</summary>
        public static void Seen(string id)
        {
            NewStuff.Remove(NewPrefix + id);
            var topic = HelpTopics.Find(id);
            // (Not before the first look at your island, which would then think it had looked.)
            if (topic != null && topic.Unlocked != null && PlayerPrefs.HasKey(Key) && Known.Add(id)) Save();
        }

        /// <summary>
        /// Start over: a fresh island, whose help starts short again. (Called with the rest of
        /// HudTour.ResetAll.)
        /// </summary>
        public static void Forget()
        {
            PlayerPrefs.DeleteKey(Key);
            _known = null;
            NewStuff.Clear(NewPrefix);
            PlayerPrefs.Save();
            if (Instance != null) Instance._quietUntil = -1f;
        }

        /// <summary>Looks now (the help is opening), so nothing just reached is missing from it.</summary>
        public static void CheckNow()
        {
            if (Instance != null && Instance.CanLook) Instance.Look();
        }

        static void Save()
        {
            PlayerPrefs.SetString(Key, string.Join(",", Known));
            PlayerPrefs.Save();
        }

        // ---------------------------------------------------------------- the watcher

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            Attach();
            if (_hooked) return;
            _hooked = true;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) => Attach();
        }

        static void Attach()
        {
            if (Instance == null) new GameObject("HelpUnlocks").AddComponent<HelpUnlocks>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            _known = null;
            _hooked = false;
            _snapFrame = -1;
        }

        void Awake()
        {
            Instance = this;
            _loadedAt = Time.unscaledTime;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>At home, on an island that has finished loading (not the start-up screens, not mid-flight).</summary>
        bool CanLook =>
            SaveGame.Instance != null && !SaveGame.Restoring && !SaveGame.Suspended
            && !Visit.Active && !Visit.Travelling
            && Time.unscaledTime - _loadedAt >= Settle;

        void Update()
        {
            if (Time.unscaledTime < _nextLook) return;
            _nextLook = Time.unscaledTime + Every;
            if (CanLook) Look();
        }

        void Look()
        {
            // The very first look at this island: what it has reached counts as known, quietly.
            bool changed = false;
            if (!PlayerPrefs.HasKey(Key))
            {
                _quietUntil = Time.unscaledTime + CatchUp;
                changed = true;   // saved even if nothing is reached yet, so the next look isn't a first
            }
            bool quiet = Time.unscaledTime < _quietUntil || (Attention.TutorialRunning && !Guidance.Off);

            foreach (var topic in HelpTopics.All)
            {
                if (topic.Unlocked == null || Known.Contains(topic.Id) || !Reached(topic)) continue;
                Known.Add(topic.Id);
                if (!quiet) NewStuff.Add(NewPrefix + topic.Id);
                changed = true;
            }
            if (changed) Save();
        }

        static bool Reached(HelpTopic topic)
        {
            try { return topic.Unlocked(); }
            catch (Exception) { return false; }   // something not there yet: ask again next time
        }

        // ---------------------------------------------------------------- what's on the island
        //
        // One look round the island per look at the pages, however many pages ask: what is planted,
        // what you have (planted, or riding in your trail; not still shut in a bubble), and what the
        // shops have out for sale right now.

        static int _snapFrame = -1;
        static readonly HashSet<string> _placed = new(), _have = new(), _forSale = new();

        static void Snapshot()
        {
            if (_snapFrame == Time.frameCount) return;
            _snapFrame = Time.frameCount;
            _placed.Clear();
            _have.Clear();
            _forSale.Clear();
            foreach (var tag in FindObjectsByType<CatalogItem>(FindObjectsSortMode.None))
            {
                var name = tag.StoreName;
                if (string.IsNullOrEmpty(name)) continue;
                _have.Add(name);
                if (tag.TryGetComponent<Placeable>(out var p) && p.Placed) _placed.Add(name);
            }
            foreach (var display in SaleDisplay.All)
                if (display != null && display.Blocks) _forSale.Add(display.StoreName);
        }

        static bool AnyIn(HashSet<string> set, string[] names)
        {
            Snapshot();
            foreach (var n in names) if (set.Contains(n)) return true;
            return false;
        }

        /// <summary>One of these is planted on the island.</summary>
        public static bool Placed(params string[] names) => AnyIn(_placed, names);

        /// <summary>One of these is yours (planted, carried, or walking about), or a shop has one out for sale.</summary>
        public static bool Met(params string[] names) => AnyIn(_have, names) || AnyIn(_forSale, names);

        /// <summary>A shop has one of these out for sale right now.</summary>
        public static bool ForSale(params string[] names) => AnyIn(_forSale, names);

        /// <summary>Something of this kind is planted (any forest, any hill).</summary>
        public static bool PlacedKind(StoreItemKind kind)
        {
            Snapshot();
            foreach (var n in _placed) if (StoreData.Get(n).Kind == kind) return true;
            return false;
        }

        /// <summary>A capybara of your own (not a wild zombie stumbling about).</summary>
        public static bool OwnsWorker
        {
            get
            {
                foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
                    if (!w.TryGetComponent<MonsterAnimal>(out var m) || m.Tame) return true;
                return false;
            }
        }

        /// <summary>A worker asleep with hunger, the moment the feeding pages are for.</summary>
        public static bool HungryWorker
        {
            get
            {
                foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
                    if (w.IsAsleep && w.IsHungry) return true;
                return false;
            }
        }

        /// <summary>A zombie capybara who has had its bath and works for you.</summary>
        public static bool TameZombie
        {
            get
            {
                foreach (var m in FindObjectsByType<MonsterAnimal>(FindObjectsSortMode.None))
                    if (m.Tame) return true;
                return false;
            }
        }

        /// <summary>The first bridge is up: the Heart of the Isles (the hot spring, Skipper Nori's beach) can be walked to.</summary>
        public static bool FirstBridgeOpen
        {
            get
            {
                var land = LandMap.Instance;
                if (land == null) return false;
                var first = land.Bridge("first_bridge");
                if (first != null) return first.open;
                foreach (var p in land.Pieces) if (p.isBridge && p.open) return true;
                return false;
            }
        }

        /// <summary>
        /// A bridge spirit is waiting somewhere, or one has already been paid (a bridge other than
        /// the first is up: an island from before this may have none left waiting).
        /// </summary>
        public static bool SpiritsAbout
        {
            get
            {
                if (FindFirstObjectByType<BridgeGhost>() != null) return true;
                var land = LandMap.Instance;
                if (land == null) return false;
                foreach (var p in land.Pieces)
                    if (p.isBridge && p.open && p.name != "first_bridge") return true;
                return false;
            }
        }

        /// <summary>The opening is over (or was ended in Settings).</summary>
        public static bool OpeningOver
        {
            get
            {
                var tutorial = TutorialDirector.Instance;
                return Guidance.Off || (tutorial != null && tutorial.Finished);
            }
        }

        /// <summary>Something earned to wear, beyond the crown you start with.</summary>
        public static bool EarnedOutfit
        {
            get
            {
                if (!Wardrobe.Ready) return false;
                foreach (var item in Wardrobe.Items)
                    if (item.Unlock != "start" && item.Unlock != "sewn" && Wardrobe.Owns(item)) return true;
                return false;
            }
        }
    }
}
