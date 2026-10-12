using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// One of Skipper Nori's wares on the sand (a pack of bait, a message bottle) — a stand just like a shop's:
    /// the ware bobbing on a pale base, half faded while you can't afford it, tap to open the
    /// purchase dialog.
    /// </summary>
    public class BaitStand : MonoBehaviour, IPurchasable, ITappable
    {
        string _title, _description;
        Vector3Int _price;
        System.Func<bool> _buy;
        System.Func<string> _locked;
        SpriteRenderer _icon, _shadow, _plinth;
        float _phase, _alpha = -1f;
        const float TapRadius = 0.9f, IconSize = 1.1f;

        public string Title => _title;
        public string Description => _description;
        public Vector3Int Price => _price;
        public bool SoldOut => false;
        public bool Buy() => Locked == null && _buy != null && _buy();
        public string Locked => _locked?.Invoke();

        public static BaitStand Create(BaitKind bait, Vector2 at, Transform parent) =>
            Create("Bait_" + bait.Id, bait.Name, bait.Blurb, new Vector3Int(0, bait.Gold, bait.Gems), HobbyData.ItemArt(bait.Item), () => Baits.Buy(bait), at, parent,
                // bait is for fishing: no use (and not for sale) until you have a rod
                () => HobbyLog.IsUnlocked(Hobby.Fishing) ? null : "You need a fishing rod first. Nori sells one right here.");

        public static BaitStand Create(string id, string title, string description, Vector3Int price, Sprite sprite, System.Func<bool> buy, Vector2 at, Transform parent,
            System.Func<string> locked = null)
        {
            var go = new GameObject(id);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(at.x, at.y, 0f);

            var art = GameArt.Instance;
            if (art != null && art.shadowTemplate != null)
            {
                var shadow = Instantiate(art.shadowTemplate, go.transform);
                shadow.name = "Shadow";
                shadow.transform.localPosition = Vector3.zero;
                shadow.transform.localScale = art.shadowTemplate.transform.localScale * 0.8f;
                shadow.SetActive(true);
                var plinth = Instantiate(art.shadowTemplate, go.transform);
                plinth.name = "Plinth";
                plinth.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                plinth.transform.localScale = art.shadowTemplate.transform.localScale * 1.2f;
                var pr = plinth.GetComponent<SpriteRenderer>();
                if (pr != null) pr.color = new Color(1f, 0.97f, 0.84f, 0.55f);
                plinth.SetActive(true);
            }

            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sr.sprite = sprite;
                var size = sprite.bounds.size;
                body.transform.localScale = Vector3.one * (IconSize / Mathf.Max(0.01f, Mathf.Max(size.x, size.y)));
            }

            var stand = go.AddComponent<BaitStand>();
            stand._title = title; stand._description = description; stand._price = price; stand._buy = buy; stand._locked = locked;
            stand._phase = Random.value * 6f;
            stand._icon = sr;
            stand._shadow = go.transform.Find("Shadow")?.GetComponent<SpriteRenderer>();
            stand._plinth = go.transform.Find("Plinth")?.GetComponent<SpriteRenderer>();
            go.AddComponent<YSorter>();
            return stand;
        }

        void Update()
        {
            var store = ResourceStore.Instance;
            bool locked = _locked != null && _locked() != null;
            float target = locked ? 0.4f : store != null && store.CanAfford(_price.x, _price.y, _price.z) ? 1f : 0.5f;
            _alpha = _alpha < 0f ? target : Mathf.MoveTowards(_alpha, target, Time.deltaTime * 3f);
            if (_icon != null)
            {
                // Not for you yet: greyed out as well as faded.
                float grey = locked ? 0.6f : 1f;
                _icon.color = new Color(grey, grey, grey, _alpha);
                _icon.transform.localPosition = new Vector3(0f, 0.08f + Mathf.Sin(Time.time * 2f + _phase) * 0.06f, 0f);
            }
            if (_shadow != null) { var c = _shadow.color; c.a = _alpha * 0.8f; _shadow.color = c; }
            if (_plinth != null) { var c = _plinth.color; c.a = _alpha * (0.45f + 0.1f * Mathf.Sin(Time.time * 2f + _phase)); _plinth.color = c; }
        }

        void OnEnable() => TapTargets.Register(this);
        void OnDisable() => TapTargets.Unregister(this);

        public float TapDistance(Vector2 worldPoint)
        {
            Vector2 at = _icon != null ? (Vector2)_icon.bounds.center : (Vector2)transform.position;
            float d = Vector2.Distance(at, worldPoint);
            return d <= TapRadius ? d : -1f;
        }

        public void Tap() => SaleDisplay.BrowseOther(this);
    }
}
