using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The tutorial's zoom beat: a little picture of the gesture in the middle of the screen while
    /// Old Yuzu says how to zoom. Two fingertips spreading and closing on a phone, a mouse with its
    /// wheel rolling on a computer, both on a computer with a touch screen.
    ///
    /// Only a picture: it catches no touches, so the pinch goes straight through to the island.
    /// It steps aside whenever something else has the screen (the nap movie). The tutorial shows
    /// it and hides it.
    /// </summary>
    public class ZoomHint : MonoBehaviour
    {
        static ZoomHint _instance;

        CanvasGroup _group;
        RectTransform _fingerA, _fingerB, _wheel;
        Image _up, _down;
        float _alpha, _wanted;

        public static void Show()
        {
            var h = Get();
            if (h != null) h._wanted = 1f;
        }

        public static void Hide()
        {
            if (_instance != null) _instance._wanted = 0f;
        }

        static ZoomHint Get()
        {
            if (_instance != null) return _instance;
            var hud = MobileHud.Instance;
            if (hud == null) return null;
            var go = new GameObject("ZoomHint", typeof(RectTransform));
            go.layer = hud.gameObject.layer;
            go.transform.SetParent(hud.transform, false);
            go.transform.SetAsFirstSibling();   // over the island, beneath every button and popup
            _instance = go.AddComponent<ZoomHint>();
            _instance.Build();
            return _instance;
        }

        void Build()
        {
            var root = (RectTransform)transform;
            UiKit.Stretch(root);
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;

            bool pinch = Application.isMobilePlatform || Input.touchSupported;
            bool wheel = !Application.isMobilePlatform;
            float width = pinch && wheel ? 420f : 230f;

            var glass = UiKit.HudGlass;
            glass.a *= 0.75f;
            var card = UiKit.Image(root, "Card", UiKit.Rounded(48), glass);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(width, 230f));
            card.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            float x = pinch && wheel ? -95f : 0f;
            if (pinch)
            {
                var spot = UiKit.Rect("Pinch", card.transform);
                UiKit.Place(spot, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(200f, 200f));
                _fingerA = Fingertip(spot);
                _fingerB = Fingertip(spot);
                var glassIcon = UiKit.Image(spot, "Magnifier", IconPainter.Magnifier, new Color(1f, 1f, 1f, 0.55f));
                UiKit.Place(glassIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(62f, -62f), new Vector2(40f, 40f));
                x += 190f;
            }
            if (wheel)
            {
                var spot = UiKit.Rect("Mouse", card.transform);
                UiKit.Place(spot, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(160f, 210f));
                var body = UiKit.Image(spot, "Body", UiKit.Rounded(40), new Color(1f, 1f, 1f, 0.92f));
                UiKit.Place(body.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 124f));
                var seam = UiKit.Image(body.transform, "Seam", UiKit.Rounded(2), new Color(UiKit.InkSoft.r, UiKit.InkSoft.g, UiKit.InkSoft.b, 0.6f));
                UiKit.Place(seam.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), new Vector2(4f, 52f));
                var w = UiKit.Image(body.transform, "Wheel", UiKit.Rounded(8), UiKit.Ink);
                _wheel = w.rectTransform;
                UiKit.Place(_wheel, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(16f, 32f));
                _up = UiKit.Image(spot, "Up", IconPainter.Chevron, Color.white);
                UiKit.Place(_up.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 88f), new Vector2(28f, 28f));
                _up.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                _down = UiKit.Image(spot, "Down", IconPainter.Chevron, Color.white);
                UiKit.Place(_down.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -88f), new Vector2(28f, 28f));
                _down.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            }
        }

        /// <summary>A fingertip: a soft ring with a bright dot in it.</summary>
        static RectTransform Fingertip(RectTransform parent)
        {
            var ring = UiKit.Image(parent, "Finger", IconPainter.Ring, new Color(1f, 1f, 1f, 0.5f));
            UiKit.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f));
            ring.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var dot = UiKit.Image(ring.transform, "Tip", UiKit.Circle, new Color(1f, 0.97f, 0.9f, 0.95f));
            UiKit.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return ring.rectTransform;
        }

        void Update()
        {
            float target = Game.Attention.Busy ? 0f : _wanted;
            _alpha = Mathf.MoveTowards(_alpha, target, Time.unscaledDeltaTime * (target > _alpha ? 3f : 4f));
            _group.alpha = _alpha;
            if (_alpha <= 0f) return;

            // Apart, hold, together, hold: one breath every 2.4 seconds.
            float t = Mathf.Repeat(Time.unscaledTime, 2.4f) / 2.4f;
            float open = t < 0.4f ? Mathf.SmoothStep(0f, 1f, t / 0.4f)
                       : t < 0.5f ? 1f
                       : t < 0.9f ? 1f - Mathf.SmoothStep(0f, 1f, (t - 0.5f) / 0.4f)
                       : 0f;
            if (_fingerA != null)
            {
                float spread = Mathf.Lerp(18f, 62f, open);
                var diagonal = new Vector2(0.75f, 0.66f);
                _fingerA.anchoredPosition = diagonal * spread;
                _fingerB.anchoredPosition = -diagonal * spread;
            }
            if (_wheel != null)
            {
                // The wheel rolls up a notch and back: the chevron on the side it turns lights up.
                float roll = Mathf.Sin(Time.unscaledTime * Mathf.PI / 1.2f);
                _wheel.anchoredPosition = new Vector2(0f, 30f + roll * 7f);
                _up.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(roll)));
                _down.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(-roll)));
            }
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
