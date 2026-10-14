using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Sunflower Farm: the island south of Pebble Cove where gardening lives. A spirit at the end of
    /// Pebble Cove raises the bridge (6000 wood, 6000 gold); crossing it takes up gardening, and the
    /// farm is already laid out: twelve plots in a field — as many open as your gardening level
    /// allows, the rest roped off until you grow into them — and a stove for cooking.
    ///
    /// The farm is built fresh every time the island loads (like the stone lanterns): its plots
    /// and stove are fixtures, never saved as items, never carried off, spared by bombs. What grows
    /// in each plot is kept in <see cref="HobbyLog.Plots"/> by where the plot stands.
    ///
    /// Islands from before the farm (plots dotted about, a Garden Shop, a stove) are moved over the
    /// first time they load: the bridge rises for free and every crop goes on growing in a farm plot.
    /// </summary>
    public class Farm : MonoBehaviour
    {
        public const string BridgePiece = "farm_bridge";
        public const string GhostName = "Farm Isle";
        public const int PlotCount = 12;
        const int IslandIndex = 7;   // appended after the original seven (IslandPainter.LaterIslands)

        public static Farm Instance { get; private set; }

        /// <summary>The bridge is up: the farm is yours.</summary>
        public static bool IsOpen
        {
            get
            {
                var land = LandMap.Instance;
                var b = land != null ? land.Bridge(BridgePiece) : null;
                return b != null && b.open;
            }
        }

        /// <summary>The middle of the field, for "show me the farm".</summary>
        public static Vector2? Centre => Instance != null && Instance._centre.HasValue ? Instance._centre : null;

        /// <summary>Where to send you for gardening: the field once it's yours, else the spirit to pay.</summary>
        public static Vector2? Destination
        {
            get
            {
                if (IsOpen) return Centre;
                foreach (var g in FindObjectsByType<BridgeGhost>(FindObjectsSortMode.None))
                    if (g.BridgeName == GhostName) return g.transform.position;
                return null;
            }
        }

        /// <summary>The farm's stove, once the farm is laid out.</summary>
        public static Vector2? StoveSpot => IsOpen && Centre.HasValue ? Centre.Value + new Vector2(-9.5f, 0.6f) : null;

        readonly List<Gardening.PlotView> _plots = new();
        GameObject _root;
        Vector2? _centre;
        bool _introPending;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            LandMap.BridgeOpened -= OnBridgeOpened;
        }

        void Start()
        {
            if (Visit.Active) return;   // a friend's island has no farm of yours
            HobbyItems.AfterLoad(this, AfterLoad);
        }

        void AfterLoad()
        {
            var land = LandMap.Instance;
            if (land == null || land.Island(IslandIndex) == null) return;
            var rect = land.Island(IslandIndex).rects[0];
            _centre = LandMap.WorldToUnity(rect.center);

            MoveOldGarden();
            if (IsOpen) LayOut();
            else EnsureGhost();
            LandMap.BridgeOpened += OnBridgeOpened;
        }

        void OnBridgeOpened(string name)
        {
            if (name == "second_bridge") EnsureGhost();   // Pebble Cove is open: the farm's spirit waits at its end
        }

        /// <summary>The spirit has been paid (BridgeGhost.Cross): take up gardening, and the farm is there.</summary>
        public void Opened()
        {
            if (HobbyLog.Unlock(Hobby.Gardening)) _introPending = true;
            LayOut();
        }

        void Update()
        {
            // The gardening movie waits for the bridge's to finish (one movie at a time).
            if (!_introPending || UI.Cutscene.Playing || UI.UiScreen.AnyOpen) return;
            _introPending = false;
            UI.HobbyCutscene.Show(Hobby.Gardening);
        }

        // ---------------------------------------------------------------- the spirit

        void EnsureGhost()
        {
            var land = LandMap.Instance;
            if (land == null || IsOpen) return;
            var toCove = land.Bridge("second_bridge");
            if (toCove == null || !toCove.open) return;
            foreach (var g in FindObjectsByType<BridgeGhost>(FindObjectsSortMode.None))
                if (g.BridgeName == GhostName) return;
            var bridge = land.Bridge(BridgePiece);
            if (bridge == null || bridge.circles.Count == 0) return;

            // On Pebble Cove's sand, at the near end of the bridge to come.
            Vector2 at = LandMap.WorldToUnity(bridge.circles[0]);
            for (int i = 0; i < 40 && !BunniSpace.PointOnLand(at); i++) at += Vector2.up * 0.5f;
            var catalog = ItemCatalog.Instance;
            BridgeGhost.Spawn(GhostName, at + Vector2.up * 0.8f, catalog != null ? catalog.ItemParent : null);
        }

        // ---------------------------------------------------------------- the farm

        /// <summary>Where each plot stands: a field of four by three, read left to right, top to bottom.</summary>
        IEnumerable<Vector2> PlotSpots()
        {
            if (!_centre.HasValue) yield break;
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 4; col++)
                    yield return _centre.Value + new Vector2(-3.9f + col * 2.6f, 2.4f - row * 2.1f);
        }

        Vector2 Offset(float x, float y) => _centre.Value + new Vector2(x, y);

        /// <summary>True while the farm puts its plots down, so they don't each announce themselves.</summary>
        public static bool LayingOut { get; private set; }

        void LayOut()
        {
            if (_root != null || !_centre.HasValue) return;
            var catalog = ItemCatalog.Instance;
            if (catalog == null) return;
            LayingOut = true;
            try { Build(catalog); }
            finally { LayingOut = false; }
        }

        void Build(ItemCatalog catalog)
        {
            _root = new GameObject("Sunflower Farm");
            _root.transform.SetParent(catalog.ItemParent, false);

            int i = 0;
            foreach (var at in PlotSpots())
            {
                var plot = Fixture(catalog, "Vegetable Plot", at);
                var view = plot != null ? plot.GetComponent<Gardening.PlotView>() : null;
                if (view != null) { view.FarmIndex = i; _plots.Add(view); }
                i++;
            }
            Fixture(catalog, "Stove", StoveSpot.Value);

            // Dressing, where its art has been made: a barn, a scarecrow over the field, hay.
            // The barn is the farm's shop: its seeds stand out front, like any store's wares.
            Prop("Farm_Barn", Offset(10.5f, 2.2f), 4.2f, 70f, shopList: "farm_store");
            Prop("Farm_Scarecrow", Offset(6.0f, 4.0f), 2.2f, 14f);
            Prop("Farm_Hay", Offset(7.8f, -3.6f), 1.4f, 18f);
            Prop("Farm_Hay", Offset(-11.2f, -2.6f), 1.4f, 18f);
            Fence(Offset(0f, -3.7f), 6, 1.1f);
        }

        /// <summary>A catalogue item made a fixture: planted, but not saved, not carried, not bombed.</summary>
        GameObject Fixture(ItemCatalog catalog, string storeName, Vector2 at)
        {
            var go = catalog.Create(storeName);
            if (go == null) return null;
            // Still inactive: strip what makes it an item you own and carry before anything wakes.
            DestroyImmediate(go.GetComponent<CatalogItem>());
            DestroyImmediate(go.GetComponent<DraggableActor>());
            DestroyImmediate(go.GetComponent<Follower>());
            go.transform.SetParent(_root.transform, true);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            go.SetActive(true);
            var placeable = go.GetComponent<Placeable>();
            if (placeable != null) placeable.MarkPlaced(silent: true);
            return go;
        }

        /// <summary>
        /// A run of fence along the front of the field, centred on <paramref name="middle"/>. Each
        /// section's art has a post at both ends, so neighbours overlap by one post and share it.
        /// </summary>
        void Fence(Vector2 middle, int sections, float height)
        {
            var art = HobbyData.Art("Farm_Fence");
            if (art == null) return;
            float width = height * art.bounds.size.x / Mathf.Max(0.01f, art.bounds.size.y);
            float step = width * (1f - SharedPost);
            float first = -step * (sections - 1) * 0.5f;
            for (int i = 0; i < sections; i++) Prop("Farm_Fence", middle + new Vector2(first + i * step, 0f), height, 0f);
        }

        /// <summary>How much of a fence section's width one end post takes (measured on its art).</summary>
        const float SharedPost = 0.215f;

        /// <summary>A piece of farm dressing standing on its feet, holding its spot.</summary>
        void Prop(string art, Vector2 feet, float height, float footprint, string shopList = null)
        {
            var sprite = HobbyData.Art(art);
            if (sprite == null) return;
            var go = new GameObject(art);
            go.transform.SetParent(_root.transform, false);
            go.transform.position = feet;
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            float scale = height / Mathf.Max(0.01f, sprite.bounds.size.y);
            body.transform.localScale = Vector3.one * scale;
            body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            body.AddComponent<SpriteRenderer>().sprite = sprite;
            go.AddComponent<YSorter>();
            if (footprint <= 0f) return;
            // A shop lays its wares out when it's planted (StoreBuilding.OnPlacedInWorld).
            if (shopList != null) go.AddComponent<StoreBuilding>().Configure(shopList, "");
            var placeable = go.AddComponent<Placeable>();
            placeable.Configure(art, footprint, footprint);
            placeable.MarkPlaced(silent: true);
        }

        // ---------------------------------------------------------------- older islands

        /// <summary>
        /// An island from before the farm: its plots, Garden Shop and stove go, the bridge rises for
        /// free, and every crop carries on growing in a farm plot (in the order they were planted).
        /// </summary>
        void MoveOldGarden()
        {
            var old = new List<GameObject>();
            foreach (var tag in FindObjectsByType<CatalogItem>(FindObjectsSortMode.None))
                if (tag.StoreName is "Vegetable Plot" or "Garden Shop" or "Stove") old.Add(tag.gameObject);
            foreach (var bubble in FindObjectsByType<Bubble>(FindObjectsSortMode.None))
            {
                if (bubble.Taken || bubble.Contents == null) continue;
                var tag = bubble.Contents.GetComponent<CatalogItem>();
                if (tag != null && tag.StoreName is "Vegetable Plot" or "Garden Shop" or "Stove") old.Add(bubble.gameObject);
            }

            var spots = new List<Vector2>(PlotSpots());
            bool OnFarm(Vector2 p) { foreach (var s in spots) if ((s - p).sqrMagnitude < 0.09f) return true; return false; }
            var loose = new List<HobbyLog.Plot>();
            foreach (var p in HobbyLog.Plots) if (!OnFarm(p.pos)) loose.Add(p);
            if (old.Count == 0 && loose.Count == 0) return;

            // Each crop to the next free farm plot; anything beyond twelve is let go.
            int next = 0;
            foreach (var p in loose)
            {
                while (next < spots.Count && Taken(spots[next])) next++;
                if (next >= spots.Count || string.IsNullOrEmpty(p.crop)) { HobbyLog.Plots.Remove(p); continue; }
                p.pos = spots[next++];
                p.unplaced = false;
            }
            HobbyLog.Save();

            foreach (var go in old) if (go != null) Destroy(go);
            StoreData.InvalidateCounts();

            var land = LandMap.Instance;
            if (land != null && !IsOpen)
            {
                land.OpenBridge(BridgePiece, animate: false);
                foreach (var g in FindObjectsByType<BridgeGhost>(FindObjectsSortMode.None))
                    if (g.BridgeName == GhostName) Destroy(g.gameObject);
                HobbyLog.Unlock(Hobby.Gardening);
            }
            Hints.Show("Your vegetable plots have moved to Sunflower Farm, south of Pebble Cove. Your crops are still growing there!");

            bool Taken(Vector2 spot)
            {
                foreach (var q in HobbyLog.Plots) if ((q.pos - spot).sqrMagnitude < 0.09f) return true;
                return false;
            }
        }
    }
}
