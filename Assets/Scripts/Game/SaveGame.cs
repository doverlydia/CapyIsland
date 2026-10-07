using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    [Serializable] public class ItemSave { public string name; public Vector2 pos; public float scale = -1f, finalScale = -1f; public int fruit; }
    [Serializable] public class NodeSave { public string origin; public Vector2 pos; public float inner, scale, finalScale; }
    [Serializable] public class BubbleSave { public string name; public Vector2 pos; }
    [Serializable] public class AnimalSave { public string name; public Vector2 pos; public int workplace = -1; public bool carried, tame; public int hunger; }
    [Serializable] public class QuestSave { public int giver, id, stage; public bool complete; }
    [Serializable] public class PriceSave { public string name; public int wood, gold, gems; }
    [Serializable] public class GhostSave { public string name; public int id; public Vector2 pos; }

    /// <summary>
    /// A whole island on disk. Two halves share it: the player's <b>progress</b> (resources,
    /// achievements, cards, quests) and the island's <b>world</b> (bridges, everything planted, the
    /// trail, bubbles, animals, spirits). A friend's island uses only the world half.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int wood, gold, gems, score;
        public List<string> motes = new(), motesClicked = new(), bridges = new(), carried = new();
        public List<int> cards = new(), ghostsSpawned = new();
        public int corianderIndex, peachIndex;
        public List<QuestSave> quests = new();
        public List<PriceSave> prices = new();
        public Vector2 king;
        public List<ItemSave> placed = new();
        public List<NodeSave> children = new();
        public List<BubbleSave> bubbles = new();
        public List<AnimalSave> animals = new();
        public List<GhostSave> questGhosts = new(), bridgeGhosts = new();
    }

    /// <summary>
    /// Saves the island and brings it back next time. Saving starts once the tutorial is over; the
    /// game saves every 30 seconds and whenever the app is paused or closed — there is no save
    /// button to forget.
    ///
    /// Stored as JSON in PlayerPrefs, which is IndexedDB on WebGL and app storage on Android.
    ///
    /// What is kept: resources, achievements, cards, quests, bridges, the king's position,
    /// everything planted (with each grove's children and each fruit tree's fruit), everything in
    /// the trail, uncollected bubbles, every animal, and the spirits still waiting. What is not: a
    /// heart not yet tapped, fruit lying on the ground, a lit bomb, an unopened chest.
    ///
    /// While visiting a friend, your own island stays safely on disk: this saves the friend's world
    /// to their profile instead (or nothing, for a stranger), and never touches your save.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class SaveGame : MonoBehaviour
    {
        public const string Key = "CapyIsland.Save.v1";
        const float AutosaveSeconds = 30f;

        public static SaveGame Instance { get; private set; }

        /// <summary>True while a save is being rebuilt, so placing things does not re-seed groves.</summary>
        public static bool Restoring { get; private set; }

        public static bool HasSave => PlayerPrefs.HasKey(Key);

        /// <summary>Set while a fresh start or a flight is swapping islands, so nothing saves over them.</summary>
        public static bool Suspended { get; set; }

        /// <summary>
        /// Puts every static the game keeps back to a new session, as a fresh launch would: each
        /// class with session state already resets it in a SubsystemRegistration method, so run them all.
        /// </summary>
        public static void ResetStatics()
        {
            foreach (var type in typeof(SaveGame).Assembly.GetTypes())
            foreach (var m in type.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            {
                if (m.GetParameters().Length != 0) continue;
                foreach (RuntimeInitializeOnLoadMethodAttribute a in m.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false))
                    if (a.loadType == RuntimeInitializeLoadType.SubsystemRegistration) { m.Invoke(null, null); break; }
            }
            QuestSystem.AnyoneTalkedTo = false;
        }

        float _nextSave;
        bool _loaded;

        // ---------------------------------------------------------------- lifecycle

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            Suspended = false;
            if (Visit.Active) LoadVisit();
            else if (HasSave) Load();
            _loaded = true;
            _nextSave = Time.unscaledTime + AutosaveSeconds;
        }

        bool CanSave
        {
            get
            {
                var tutorial = FindFirstObjectByType<TutorialDirector>();
                return _loaded && !Suspended && (tutorial == null || tutorial.Finished);
            }
        }

        void Update()
        {
            if (Time.unscaledTime < _nextSave) return;
            _nextSave = Time.unscaledTime + AutosaveSeconds;
            if (CanSave) Save();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && CanSave) Save();
        }

        void OnApplicationQuit()
        {
            if (CanSave) Save();
        }

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        // ---------------------------------------------------------------- save

        /// <summary>Saves whichever island you are on: yours to your save, a friend's to their profile.</summary>
        public void Save()
        {
            if (Visit.Active)
            {
                Visit.StoreHostWorld(CaptureWorld());
                return;
            }
            var d = CaptureWorld();
            CaptureProgress(d);
            WriteHome(d);
        }

        /// <summary>Saves your island now, whatever the autosave timer says (before a flight).</summary>
        public bool SaveHomeNow()
        {
            if (Visit.Active || !_loaded) return false;
            Save();
            return true;
        }

        public static SaveData ReadHome()
        {
            if (!HasSave) return null;
            try { return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key)); }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveGame] Save unreadable: " + e.Message);
                return null;
            }
        }

        public static void WriteHome(SaveData d)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(d));
            PlayerPrefs.Save();
        }

        /// <summary>The player's own progress: wallet, achievements, cards, prices, quests.</summary>
        public static void CaptureProgress(SaveData d)
        {
            var store = ResourceStore.Instance;
            if (store != null)
            {
                d.wood = store.Get(ResourceKind.Wood);
                d.gold = store.Get(ResourceKind.Gold);
                d.gems = store.Get(ResourceKind.Gem);
            }
            d.score = TowerOfLove.Score;
            d.motes.Clear(); d.motes.AddRange(Motes.Collected);
            d.motesClicked.Clear(); d.motesClicked.AddRange(Motes.Clicked);
            d.cards.Clear(); d.cards.AddRange(Cards.Collected);
            d.prices.Clear();
            foreach (var kv in StoreData.PinnedPrices)
                d.prices.Add(new PriceSave { name = kv.Key, wood = kv.Value.x, gold = kv.Value.y, gems = kv.Value.z });

            var qs = QuestSystem.Instance;
            if (qs != null)
            {
                var snap = qs.Snapshot();
                d.corianderIndex = snap.coriander;
                d.peachIndex = snap.peach;
                d.quests.Clear();
                foreach (var (giver, id, stage, complete) in snap.quests)
                    d.quests.Add(new QuestSave { giver = giver, id = id, stage = stage, complete = complete });
                d.ghostsSpawned.Clear();
                d.ghostsSpawned.AddRange(snap.ghosts);
            }
        }

        /// <summary>Everything on the island as it stands now.</summary>
        public static SaveData CaptureWorld()
        {
            var d = new SaveData();
            var land = LandMap.Instance;
            if (land != null)
                foreach (var p in land.Pieces)
                    if (p.isBridge && p.open) d.bridges.Add(p.name);

            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king != null) d.king = king.transform.position;

            // Everything planted, in an order the animals can refer to.
            var placedIndex = new Dictionary<GameObject, int>();
            foreach (var tag in FindObjectsByType<CatalogItem>(FindObjectsSortMode.None))
            {
                var placeable = tag.GetComponent<Placeable>();
                if (placeable == null) continue;
                if (!placeable.Placed)
                {
                    if (tag.GetComponent<Follower>() is { Carried: true }) d.carried.Add(tag.StoreName);
                    continue;
                }
                if (tag.GetComponent<Bomb>() != null) continue;
                var item = new ItemSave { name = tag.StoreName, pos = tag.transform.position };
                if (tag.TryGetComponent<Harvestable>(out var node) && node.InWorld) { item.scale = node.Scale; item.finalScale = node.FinalScale; }
                if (tag.TryGetComponent<FruitTree>(out var tree)) { item.scale = tree.Scale; item.fruit = tree.FruitCount; }
                placedIndex[tag.gameObject] = d.placed.Count;
                d.placed.Add(item);
            }

            foreach (var node in FindObjectsByType<Harvestable>(FindObjectsSortMode.None))
            {
                if (node.IsParent || node.IsFruit || !node.InWorld || string.IsNullOrEmpty(node.OriginName)) continue;
                d.children.Add(new NodeSave
                {
                    origin = node.OriginName, pos = node.transform.position,
                    inner = node.InnerSize, scale = node.Scale, finalScale = node.FinalScale,
                });
            }

            foreach (var b in FindObjectsByType<Bubble>(FindObjectsSortMode.None))
            {
                if (b.Taken || b.Contents == null) continue;
                var tag = b.Contents.GetComponent<CatalogItem>();
                if (tag != null) d.bubbles.Add(new BubbleSave { name = tag.StoreName, pos = b.transform.position });
            }

            foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
            {
                var tag = w.GetComponent<CatalogItem>();
                if (tag == null || w.GetComponent<VisitorBot>() != null) continue;
                var monster = w.GetComponent<MonsterAnimal>();
                bool wild = monster != null && !monster.Tame;
                var a = new AnimalSave { name = tag.StoreName, pos = w.transform.position, tame = !wild, hunger = w.Hunger };
                if (!wild)
                {
                    if (w.Workplace != null && placedIndex.TryGetValue(w.Workplace.gameObject, out int idx)) a.workplace = idx;
                    else a.carried = true;
                }
                d.animals.Add(a);
            }
            foreach (var deer in FindObjectsByType<DeerAnimal>(FindObjectsSortMode.None))
                d.animals.Add(new AnimalSave { name = "Deer", pos = deer.transform.position });

            foreach (var g in FindObjectsByType<QuestGiver>(FindObjectsSortMode.None))
                if (g.GiverKind == QuestGiver.Kind.Ghost && g.GhostId > 0)
                    d.questGhosts.Add(new GhostSave { id = g.GhostId, pos = g.transform.position });
            foreach (var g in FindObjectsByType<BridgeGhost>(FindObjectsSortMode.None))
                if (!g.SoldOut) d.bridgeGhosts.Add(new GhostSave { name = g.BridgeName, pos = g.transform.position });
            return d;
        }

        // ---------------------------------------------------------------- load

        void Load()
        {
            var d = ReadHome();
            if (d == null) return;

            Restoring = true;
            try
            {
                SkipTutorial();
                ApplyProgress(d);
                ApplyWorld(d, d);
            }
            finally { Restoring = false; }

            bool backFromTrip = Visit.ReturnedHome;
            Visit.ReturnedHome = false;
            Hints.Show(backFromTrip ? "Welcome home! Your island missed you." : "Welcome back! Your island kept warm while you were away.");
        }

        /// <summary>On a friend's island: your progress and your trail, their world.</summary>
        void LoadVisit()
        {
            var home = ReadHome() ?? new SaveData();
            var world = Visit.HostWorld ?? new SaveData();
            Restoring = true;
            try
            {
                SkipTutorial();
                ApplyProgress(home);
                ApplyWorld(world, home);
            }
            finally { Restoring = false; }
            Visit.OnArrived();
        }

        static void SkipTutorial()
        {
            var tutorial = FindFirstObjectByType<TutorialDirector>();
            if (tutorial != null) tutorial.SkipForReturningPlayer();
        }

        static void ApplyProgress(SaveData d)
        {
            var store = ResourceStore.Instance;
            if (store != null)
            {
                store.Add(ResourceKind.Wood, d.wood - store.Get(ResourceKind.Wood));
                store.Add(ResourceKind.Gold, d.gold - store.Get(ResourceKind.Gold));
                store.Add(ResourceKind.Gem, d.gems - store.Get(ResourceKind.Gem));
            }
            TowerOfLove.Score = d.score;
            Motes.Restore(d.motes, d.motesClicked);
            Cards.Restore(d.cards);
            foreach (var p in d.prices) StoreData.RestorePinnedPrice(p.name, new Vector3Int(p.wood, p.gold, p.gems));

            var qs = QuestSystem.Instance;
            if (qs != null)
            {
                var quests = new List<(int, int, int, bool)>();
                foreach (var q in d.quests) quests.Add((q.giver, q.id, q.stage, q.complete));
                qs.Restore(d.corianderIndex, d.peachIndex, quests, d.ghostsSpawned);
            }
        }

        /// <summary>
        /// Builds the island from <paramref name="d"/>. The trail — what is carried, and the animals
        /// following the king — comes from <paramref name="trail"/>, which is the same save at
        /// home and the player's own save when visiting, so your trail always travels with you.
        /// </summary>
        static void ApplyWorld(SaveData d, SaveData trail)
        {
            bool visiting = Visit.Active;

            // The opening bubbles belong to a new game.
            foreach (var b in FindObjectsByType<Bubble>(FindObjectsSortMode.None))
            {
                if (b.Contents != null) Destroy(b.Contents);
                Destroy(b.gameObject);
            }

            var land = LandMap.Instance;
            if (land != null) foreach (var b in d.bridges) land.OpenBridge(b, animate: false);

            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king != null)
            {
                king.transform.position = new Vector3(d.king.x, d.king.y, king.transform.position.z);
                var cam = Camera.main;
                if (cam != null) cam.transform.position = new Vector3(d.king.x, d.king.y, cam.transform.position.z);
            }

            var catalog = ItemCatalog.Instance;
            if (catalog == null) return;

            var placed = new List<GameObject>();
            foreach (var item in d.placed)
            {
                var go = catalog.MakeItem(item.name, item.pos);
                placed.Add(go);
                if (go == null) continue;
                if (item.scale >= 0f && go.TryGetComponent<Harvestable>(out var node)) node.RestoreState(item.scale, item.finalScale);
                if (go.TryGetComponent<FruitTree>(out var tree)) tree.RestoreGrown(item.scale, item.fruit);
            }

            foreach (var c in d.children)
            {
                var template = catalog.Template(c.origin);
                var source = template != null ? template.GetComponent<Harvestable>() : null;
                Harvestable.RestoreChild(source, c.pos, c.inner, c.scale, c.finalScale, catalog.ItemParent);
            }

            Vector2 behindKing = king != null ? (Vector2)king.transform.position + new Vector2(-0.8f, -0.4f) : Vector2.zero;
            foreach (var name in trail.carried)
            {
                var go = catalog.Create(name);
                if (go == null) continue;
                go.transform.position = behindKing;
                go.SetActive(true);
                var placeable = go.GetComponent<Placeable>();
                if (placeable != null) placeable.MarkCarried();
                if (visiting) VisitTag.Mark(go, VisitTag.Origin.Home);
            }

            foreach (var b in d.bubbles) catalog.MakeStore(b.name, b.pos);

            foreach (var a in d.animals)
            {
                if (a.carried && trail != d) continue;   // the host's trail is not yours
                SpawnAnimal(a, placed, catalog, null);
            }
            if (trail != d)
                foreach (var a in trail.animals)
                    if (a.carried && a.tame) SpawnAnimal(a, placed, catalog, behindKing);

            foreach (var g in d.questGhosts)
            {
                var go = catalog.Create("Ghost");
                if (go == null) continue;
                go.transform.position = new Vector3(g.pos.x, g.pos.y, 0f);
                var giver = go.GetComponent<QuestGiver>();
                if (giver != null) giver.AssignGhost(g.id);
                go.SetActive(true);
            }
            foreach (var g in d.bridgeGhosts) BridgeGhost.Spawn(g.name, g.pos, catalog.ItemParent);
        }

        static void SpawnAnimal(AnimalSave a, List<GameObject> placed, ItemCatalog catalog, Vector2? at)
        {
            if (!a.tame || a.name == "Deer") { catalog.MakeItem(a.name, a.pos); return; }
            var go = catalog.Create(a.name);
            if (go == null) return;
            var pos = at ?? a.pos;
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.SetActive(true);
            if (go.TryGetComponent<MonsterAnimal>(out var monster)) monster.RestoreTame();
            var worker = go.GetComponent<Worker>();
            if (worker != null) worker.RestoreHunger(a.hunger);
            if (at.HasValue && Visit.Active) VisitTag.Mark(go, VisitTag.Origin.Home);
            Workplace wp = !at.HasValue && a.workplace >= 0 && a.workplace < placed.Count && placed[a.workplace] != null
                ? placed[a.workplace].GetComponent<Workplace>() : null;
            if (wp != null && worker != null && wp.TryAssign(worker)) return;
            var follower = go.GetComponent<Follower>();
            if (follower != null) follower.PickUpIntoInventory();
        }
    }
}
