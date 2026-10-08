using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A ghost who will let you across to the next island — for a fee (StoreItems/BridgeAngelItem).
    /// Tap them to see their price and their line; pay, and a land bridge rises out of the sea, the
    /// far island's first residents arrive, and the ghost floats away.
    ///
    ///   Ginger Cove   100 wood + 100 gold
    ///   Verdant Bay   3 × your best minute + a quarter of what you hold
    ///   Pirate Cove   100 gems
    ///   Wedding Isle  5 × your best minute + a quarter of what you hold
    /// </summary>
    public class BridgeGhost : MonoBehaviour, ITappable, IPurchasable, IMapMarker
    {
        static readonly System.Collections.Generic.List<BridgeGhost> _live = new();

        /// <summary>The ghost the current goal needs you to pay first, so the arrows point at it.</summary>
        public static BridgeGhost Pointed { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            _live.Clear();
            Pointed = null;
        }

        [SerializeField] string bridgeName;   // the store item name: "Ginger Cove", …

        Transform _body;
        SpriteRenderer _bodyRenderer;
        bool _gone;

        public string BridgeName => bridgeName;
        public string Title => "Bridge to " + StoryText.Name(bridgeName);
        public string Description => StoreData.Get(bridgeName).Description;
        public Vector3Int Price => StoreData.Price(bridgeName);
        public bool SoldOut => _gone;

        /// <summary>Puts a bridge ghost on the map (MakeStoreRandom(name, island, false)).</summary>
        public static BridgeGhost Spawn(string name, Vector2 at, Transform parent)
        {
            var catalog = ItemCatalog.Instance;
            var template = catalog != null ? catalog.Template("Ghost") : null;
            GameObject go;
            if (template != null)
            {
                // The copy is still inactive (so is its template): strip the quest-giver parts
                // before anything in them wakes up.
                go = Instantiate(template, parent);
                go.SetActive(false);
                DestroyImmediate(go.GetComponent<QuestGiver>());
                DestroyImmediate(go.GetComponent<CatalogItem>());
            }
            else go = new GameObject();
            go.name = "BridgeGhost_" + name.Replace(" ", "");
            go.transform.position = new Vector3(at.x, at.y, 0f);

            var ghost = go.AddComponent<BridgeGhost>();
            ghost.bridgeName = name;
            go.SetActive(true);
            // Priced the moment they appear, from how you are doing right now.
            StoreData.Price(name);
            return ghost;
        }

        void Awake()
        {
            _body = transform.Find("Sprite");
            _bodyRenderer = _body != null ? _body.GetComponent<SpriteRenderer>() : null;
            if (_bodyRenderer != null)
            {
                _bodyRenderer.color = new Color(0.85f, 0.95f, 1f, 0f);
                _bodyRenderer.DOFade(0.9f, 1.5f);
            }
        }

        void OnEnable()
        {
            TapTargets.Register(this);
            MapMarkers.Register(this);
            _live.Add(this);
        }

        void OnDisable()
        {
            TapTargets.Unregister(this);
            MapMarkers.Unregister(this);
            _live.Remove(this);
            if (Pointed == this) Pointed = null;
        }

        public MarkerKind MarkerKind => MarkerKind.Quest;
        public bool MarkerActive => !_gone && Pointed == this;
        public Vector2 MarkerPosition => transform.position;
        public Sprite MarkerIcon => _bodyRenderer != null ? _bodyRenderer.sprite : null;

        /// <summary>The land bridge each ghost raises.</summary>
        static string Piece(string storeName) => storeName switch
        {
            "Ginger Cove" => "second_bridge",
            "Verdant Bay" => "pete_isle",
            "Pirate Cove" => "pirate_cove",
            "Wedding Isle" => "wedding_isle",
            _ => null,
        };

        /// <summary>
        /// The ghost to pay next on the way to <paramref name="target"/>, from where you stand: one
        /// you can walk to, whose bridge lands on the target's island if there is one, otherwise
        /// the one nearest the target (a step closer).
        /// </summary>
        public static BridgeGhost Toward(Vector2 target, Vector2 from)
        {
            var land = LandMap.Instance;
            if (land == null) return null;
            BridgeGhost best = null;
            float bestDistance = float.MaxValue;
            foreach (var g in _live)
            {
                if (g == null || g._gone || !land.Reachable(from, g.transform.position)) continue;
                var piece = Piece(g.bridgeName);
                if (piece != null && land.BridgeReaches(piece, target)) return g;
                float d = Vector2.Distance(g.transform.position, target);
                if (d < bestDistance) { best = g; bestDistance = d; }
            }
            return best;
        }

        void Update()
        {
            if (_body != null && !_gone)
                _body.localPosition = new Vector3(0f, 0.35f + Mathf.Sin(Time.time * 1.3f + transform.position.x) * 0.15f, 0f);
        }

        public float TapDistance(Vector2 worldPoint)
        {
            if (_gone) return -1f;
            Vector2 at = _bodyRenderer != null ? (Vector2)_bodyRenderer.bounds.center : (Vector2)transform.position;
            float d = Vector2.Distance(at, worldPoint);
            return d < 1.2f ? d : -1f;
        }

        public void Tap() => SaleDisplay.BrowseOther(this);

        public bool Buy()
        {
            if (_gone) return false;
            var store = ResourceStore.Instance;
            var p = Price;
            if (store == null || !store.TrySpend(p.x, p.y, p.z)) return false;
            _gone = true;
            Cross(bridgeName);
            Depart();
            // The movie plays over the paused game; the bridge finishes rising in the world after it.
            UI.BridgeCutscene.Show(StoryText.Name(bridgeName));
            return true;
        }

        void Depart()
        {
            if (_body == null) { Destroy(gameObject); return; }
            _body.DOLocalMoveY(_body.localPosition.y + 3f, 2.2f).SetEase(Ease.InSine);
            if (_bodyRenderer != null) _bodyRenderer.DOFade(0f, 2.2f).OnComplete(() => Destroy(gameObject));
        }

        /// <summary>Opens the bridge and brings the far island to life (BridgeAngelItem.purchase).</summary>
        public static void Cross(string name)
        {
            var land = LandMap.Instance;
            var catalog = ItemCatalog.Instance;
            if (land == null || catalog == null) return;
            var parent = catalog.ItemParent;

            Vector2 Spot(int island, float r) => land.RandomSpotOnIsland(island, r);

            switch (name)
            {
                case "Ginger Cove":
                    land.OpenBridge("second_bridge");
                    catalog.MakeStore("Mine", Spot(2, 41f));
                    catalog.MakeItem("Medium Pine Forest", Spot(2, 36f));
                    catalog.MakeItem("Large Gray Hill", Spot(2, 36f));
                    catalog.MakeItem("Medium Gray Hill", Spot(2, 36f));
                    catalog.MakeItem("Apple Tree", Spot(2, 50f));
                    Spawn("Pirate Cove", Spot(2, 30f), parent);
                    Spawn("Verdant Bay", Spot(2, 30f), parent);
                    break;

                case "Verdant Bay":
                    land.OpenBridge("pete_isle");
                    Spawn("Wedding Isle", Spot(3, 30f), parent);
                    catalog.MakeStore("Pirate Peach", Spot(3, 30f));
                    catalog.MakeItem("Medium Maple Forest", Spot(3, 36f));
                    catalog.MakeItem("Medium Black Hill", Spot(3, 36f));
                    catalog.MakeItem("Medium Gray Hill", Spot(3, 36f));
                    catalog.MakeItem("Pear Tree", Spot(3, 50f));
                    break;

                case "Pirate Cove":
                    land.OpenBridge("pirate_cove");
                    catalog.MakeItem("Medium Ironwood Forest", Spot(5, 36f));
                    catalog.MakeItem("Medium Maple Forest", Spot(5, 36f));
                    catalog.MakeItem("Large Black Hill", Spot(5, 36f));
                    catalog.MakeItem("Large Gray Hill", Spot(5, 36f));
                    catalog.MakeItem("Pear Tree", Spot(5, 50f));
                    break;

                case "Wedding Isle":
                    land.OpenBridge("wedding_isle");
                    break;
            }
        }
    }
}
