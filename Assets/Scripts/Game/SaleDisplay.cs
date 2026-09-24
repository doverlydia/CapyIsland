using System;
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
        float _phase;
        float _alpha = -1f;

        public string StoreName => storeName;
        public string Title => storeName;
        public string Description => StoreData.Get(storeName).Description;
        public Vector3Int Price => StoreData.Price(storeName);
        public bool SoldOut => StoreData.IsSoldOut(storeName);
        public bool Visible => _alpha > 0.01f;

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

            var catalog = ItemCatalog.Instance;
            var sprite = catalog != null ? catalog.Icon(storeName) : null;
            if (_icon == null || sprite == null) return;
            _icon.sprite = sprite;
            var size = sprite.bounds.size;
            float s = iconSize / Mathf.Max(0.01f, Mathf.Max(size.x, size.y));
            _icon.transform.localScale = Vector3.one * s;
        }

        void Update()
        {
            // The original sets alpha every frame from availability.
            float target;
            if (!StoreData.IsUnlocked(storeName) || SoldOut) target = 0f;
            else
            {
                var p = Price;
                var store = ResourceStore.Instance;
                target = store != null && store.CanAfford(p.x, p.y, p.z) ? 1f : 0.5f;
            }
            _alpha = _alpha < 0f ? target : Mathf.MoveTowards(_alpha, target, Time.deltaTime * 3f);

            if (_icon != null)
            {
                var c = _icon.color; c.a = _alpha; _icon.color = c;
                // A slow, friendly bob so wares read as "for sale", not as planted things.
                _icon.transform.localPosition = new Vector3(0f, 0.08f + Mathf.Sin(Time.time * 2f + _phase) * 0.06f, 0f);
            }
            if (_shadow != null) { var c = _shadow.color; c.a = _alpha * 0.8f; _shadow.color = c; }
        }

        public bool Buy() => Purchasing.TryBuy(storeName, transform.position);

        void OnEnable() => TapTargets.Register(this);
        void OnDisable() => TapTargets.Unregister(this);

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
