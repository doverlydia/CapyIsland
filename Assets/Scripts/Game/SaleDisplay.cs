using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>Something the purchase dialog can show and buy.</summary>
    public interface IPurchasable
    {
        string Title { get; }
        string Description { get; }
        Vector3Int Price { get; }
        bool SoldOut { get; }
        bool Buy();

        /// <summary>Why it can't be bought yet (shown instead of a working Buy), or null.</summary>
        string Locked => null;
    }

    /// <summary>
    /// One item for sale, standing on the ground in front of its store (StoreItem with mFree false).
    /// Tap it to open the purchase dialog.
    ///
    /// It shows its availability the way the original did: hidden while gated or sold out, faded to
    /// half while you cannot afford it, solid when you can.
    /// </summary>
    public class SaleDisplay : MonoBehaviour, IPurchasable, ITappable
    {
        /// <summary>Raised when a display is tapped, so the dialog can open.</summary>
        public static event Action<IPurchasable> Browsed;

        [SerializeField] string storeName;
        [SerializeField] float iconSize = 1.25f;
        [SerializeField] float tapRadius = 0.9f;

        SpriteRenderer _icon;
        SpriteRenderer _shadow;
        SpriteRenderer _plinth;
        float _phase;
        float _alpha = -1f;

        public string StoreName => storeName;

        /// <summary>Every ware on the ground, for the placement test (BunniSpace.CircleOnMap).</summary>
        public static readonly List<SaleDisplay> All = new();

        /// <summary>
        /// Its footprint, in world units: like any item on the original's map, a ware keeps its own
        /// inner circle clear (TestCollision.CircleOnMap counts every map item), so trees can't seed
        /// over it and nothing can be put down on it.
        /// </summary>
        public float InnerRadius => FootprintOf(storeName);

        /// <summary>The footprint a ware of this item keeps clear (its store item's inner radius, kept to a sensible size).</summary>
        public static float FootprintOf(string storeName) =>
            StoreData.TryGet(storeName, out var def) && def.Inner > 0f ? Mathf.Clamp(def.Inner, 18f, 45f) : 24f;

        /// <summary>On show (not gated, not sold out): only then does it keep its spot clear.</summary>
        public bool Blocks => isActiveAndEnabled && StoreData.IsUnlocked(storeName) && !SoldOut;

        public string Title => StoryText.Name(storeName);
        public string Description
        {
            get
            {
                string about = StoreData.Get(storeName).Description, strengths = StoryText.WorkerStrengths(storeName);
                return strengths.Length == 0 ? about : about + "\n\n" + strengths;
            }
        }
        public Vector3Int Price => StoreData.Price(storeName);
        public bool SoldOut => StoreData.IsSoldOut(storeName);
        public bool Visible => _alpha > 0.01f;

        /// <summary>On show but not for you yet (seeds above your gardening level): greyed, and the dialog says why.</summary>
        public string Locked => StoreData.LockedReason(storeName);

        public static SaleDisplay Create(string storeName, Vector2 at, Transform parent)
        {
            var go = new GameObject("Sale_" + storeName.Replace(" ", ""));
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(at.x, at.y, 0f);

            var art = GameArt.Instance;
            if (art != null && art.shadowTemplate != null)
            {
                var shadow = Instantiate(art.shadowTemplate, go.transform);
                shadow.name = "Shadow";
                shadow.transform.localPosition = Vector3.zero;
                shadow.transform.localScale = art.shadowTemplate.transform.localScale * 0.9f;
                shadow.SetActive(true);

                // A soft pale base under the ware, so shop stock reads as "for sale" rather than planted.
                var plinth = Instantiate(art.shadowTemplate, go.transform);
                plinth.name = "Plinth";
                plinth.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                plinth.transform.localScale = art.shadowTemplate.transform.localScale * 1.35f;
                var pr = plinth.GetComponent<SpriteRenderer>();
                if (pr != null) pr.color = new Color(1f, 0.97f, 0.84f, 0.55f);
                plinth.SetActive(true);
            }

            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            body.AddComponent<SpriteRenderer>();

            var display = go.AddComponent<SaleDisplay>();
            display.storeName = storeName;
            display.Init();
            go.AddComponent<YSorter>();
            return display;
        }

        void Init()
        {
            _phase = UnityEngine.Random.value * 6f;
            var body = transform.Find("Sprite");
            _icon = body != null ? body.GetComponent<SpriteRenderer>() : null;
            var shadow = transform.Find("Shadow");
            _shadow = shadow != null ? shadow.GetComponent<SpriteRenderer>() : null;
            var plinth = transform.Find("Plinth");
            _plinth = plinth != null ? plinth.GetComponent<SpriteRenderer>() : null;

            var catalog = ItemCatalog.Instance;
            var sprite = catalog != null ? catalog.Icon(storeName) : null;
            if (_icon == null || sprite == null) return;
            _icon.sprite = sprite;
            var size = sprite.bounds.size;
            float s = iconSize / Mathf.Max(0.01f, Mathf.Max(size.x, size.y));
            _icon.transform.localScale = Vector3.one * s;
        }

        bool _cleared;

        /// <summary>
        /// Islands from before wares kept their spot clear can have a tree or a rock grown over one,
        /// or something put down on top of it, so it can't be seen or bought. Once it's on show, it
        /// clears its spot: a tree or rock that seeded there is gone, and a grove or anything else
        /// put down there goes back into a bubble, to be put somewhere else.
        /// </summary>
        void ClearSpot()
        {
            if (SaveGame.Restoring || Visit.Active) return;
            _cleared = true;
            Vector2 at = transform.position;
            bool moved = false;
            foreach (var node in FindObjectsByType<Harvestable>(FindObjectsSortMode.None))
            {
                if (!node.InWorld || !node.BlocksPlacement) continue;
                if (!Covers(node.transform.position, node.InnerRadius)) continue;
                var placeable = node.GetComponent<Placeable>();
                if (placeable != null && placeable.Placed) { BubbleSpawner.ReturnToBubble(placeable); moved = true; }
                else if (!node.IsParent) Destroy(node.gameObject);
            }
            foreach (var item in FindObjectsByType<Placeable>(FindObjectsSortMode.None))
            {
                if (!item.Placed || item.GetComponent<Harvestable>() != null || item.GetComponent<CatalogItem>() == null) continue;
                if (item.GetComponent<StoreBuilding>() is { } shop && shop.Shows(this)) continue;   // its own shop
                if (!Covers(item.transform.position, item.InnerRadius)) continue;
                BubbleSpawner.ReturnToBubble(item);
                moved = true;
            }
            if (moved) Hints.Island("Something was standing on a shop's wares, so it's back in a bubble. Put it down somewhere else!");
        }

        /// <summary>
        /// Really on top of this ware (its middle within the bigger of the two footprints), not just
        /// a little close: a garden planted beside a ware stays where it is.
        /// </summary>
        bool Covers(Vector2 other, float otherRadius) =>
            BunniSpace.WorldDistance(transform.position, other) < Mathf.Max(InnerRadius, otherRadius) * 0.9f;

        void Update()
        {
            if (!_cleared && Blocks && Time.frameCount > 2) ClearSpot();

            // The original sets alpha every frame from availability.
            float target;
            bool locked = Locked != null;
            if (!StoreData.IsUnlocked(storeName) || SoldOut) target = 0f;
            else if (locked) target = 0.6f;
            else
            {
                var p = Price;
                var store = ResourceStore.Instance;
                target = store != null && store.CanAfford(p.x, p.y, p.z) ? 1f : 0.5f;
            }
            _alpha = _alpha < 0f ? target : Mathf.MoveTowards(_alpha, target, Time.deltaTime * 3f);

            if (_icon != null)
            {
                float grey = locked ? 0.55f : 1f;
                _icon.color = new Color(grey, grey, grey, _alpha);
                // A slow, friendly bob so wares read as "for sale", not as planted things.
                _icon.transform.localPosition = new Vector3(0f, 0.08f + Mathf.Sin(Time.time * 2f + _phase) * 0.06f, 0f);
            }
            if (_shadow != null) { var c = _shadow.color; c.a = _alpha * 0.8f; _shadow.color = c; }
            if (_plinth != null) { var c = _plinth.color; c.a = _alpha * (0.45f + 0.1f * Mathf.Sin(Time.time * 2f + _phase)); _plinth.color = c; }
        }

        public bool Buy() => Purchasing.TryBuy(storeName, transform.position);

        void OnEnable() { TapTargets.Register(this); All.Add(this); }
        void OnDisable() { TapTargets.Unregister(this); All.Remove(this); }

        public float TapDistance(Vector2 worldPoint)
        {
            if (!Visible) return -1f;
            Vector2 at = _icon != null ? (Vector2)_icon.bounds.center : (Vector2)transform.position;
            float d = Vector2.Distance(at, worldPoint);
            return d <= tapRadius ? d : -1f;
        }

        public void Tap()
        {
            if (StoreData.Get(storeName).Kind == StoreItemKind.BankTrade) { BankTrade.Open(storeName, transform.position); return; }
            Browse();
        }

        public void Browse() => Browsed?.Invoke(this);

        /// <summary>The visible display under a world point, or null.</summary>
        public static SaleDisplay Pick(Vector2 worldPoint)
        {
            SaleDisplay best = null;
            float bestDistance = float.MaxValue;
            foreach (var d in FindObjectsByType<SaleDisplay>(FindObjectsSortMode.None))
            {
                if (!d.Visible) continue;
                Vector2 at = d._icon != null ? (Vector2)d._icon.bounds.center : (Vector2)d.transform.position;
                float dist = Vector2.Distance(at, worldPoint);
                if (dist > d.tapRadius || dist >= bestDistance) continue;
                best = d;
                bestDistance = dist;
            }
            return best;
        }

        /// <summary>Lets other buyable things (bridge ghosts, the bank) open the same dialog.</summary>
        public static void BrowseOther(IPurchasable item) => Browsed?.Invoke(item);
    }
}
