using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The T-shirt you sewed, worn: drawn over the capybara's body, under any wardrobe pieces, and
    /// redrawn whenever you change or take off your design.
    /// </summary>
    [DefaultExecutionOrder(111)]
    public class DesignWear : MonoBehaviour
    {
        static readonly WearItem Fit = new() { Id = "Shirt", Slot = WearSlot.Neck, Anchor = new(0.5f, 0.3f), Width = 0.46f };

        SpriteRenderer _body, _shirt;
        int _shown = -2;
        string _pixels;

        void Awake()
        {
            var sprite = transform.Find("Sprite");
            _body = sprite != null ? sprite.GetComponent<SpriteRenderer>() : null;
        }

        void OnEnable() => HobbyLog.Changed += Refresh;
        void OnDisable() => HobbyLog.Changed -= Refresh;
        void Start() => Refresh();

        void Refresh()
        {
            if (_body == null) return;
            int worn = HobbyLog.WornDesign;
            var designs = HobbyLog.Designs;
            string pixels = worn >= 0 && worn < designs.Count ? designs[worn].pixels : null;
            if (worn == _shown && pixels == _pixels) return;
            _shown = worn;
            _pixels = pixels;

            if (pixels == null)
            {
                if (_shirt != null) _shirt.enabled = false;
                return;
            }
            if (_shirt == null)
            {
                var go = new GameObject("Shirt");
                go.transform.SetParent(_body.transform, false);
                _shirt = go.AddComponent<SpriteRenderer>();
            }
            _shirt.enabled = true;
            _shirt.sprite = UI.SewingScreen.DesignSprite(pixels);
            CapyOutfit.Place(_shirt.transform, Fit, _body.sprite, _shirt.sprite);
        }

        void LateUpdate()
        {
            if (_shirt != null && _shirt.enabled && _body != null) _shirt.sortingOrder = _body.sortingOrder + 1;
        }
    }
}
