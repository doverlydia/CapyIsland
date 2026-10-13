using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The joystick (Settings → Walk with a joystick): a soft ring that appears where the thumb lands
    /// on the ground and a knob that follows it. Only a picture — the world input reads the thumb
    /// and steers; this just shows it. Hidden whenever no thumb is down.
    /// </summary>
    public class FloatingStick : MonoBehaviour
    {
        static FloatingStick _instance;

        RectTransform _root, _ring, _knob;
        Image _ringImage, _knobImage;
        float _alpha, _wanted;

        /// <summary>Shows the ring centred on a screen point.</summary>
        public static void Show(Vector2 screen, float radiusPixels)
        {
            var s = Get();
            if (s == null) return;
            s._wanted = 1f;
            s.Size(radiusPixels);
            s._ring.anchoredPosition = s.Local(screen);
            s._knob.anchoredPosition = s._ring.anchoredPosition;
        }

        /// <summary>Moves the ring (it is dragged along once the thumb pulls past its edge) and the knob.</summary>
        public static void Move(Vector2 centreScreen, Vector2 knobScreen)
        {
            if (_instance == null) return;
            _instance._ring.anchoredPosition = _instance.Local(centreScreen);
            _instance._knob.anchoredPosition = _instance.Local(knobScreen);
        }

        public static void Hide()
        {
            if (_instance != null) _instance._wanted = 0f;
        }

        static FloatingStick Get()
        {
            if (_instance != null) return _instance;
            var hud = MobileHud.Instance;
            if (hud == null) return null;
            var go = new GameObject("FloatingStick", typeof(RectTransform));
            go.layer = hud.gameObject.layer;
            go.transform.SetParent(hud.transform, false);
            go.transform.SetAsFirstSibling();   // beneath every button and popup
            _instance = go.AddComponent<FloatingStick>();
            _instance.Build();
            return _instance;
        }

        void Build()
        {
            _root = (RectTransform)transform;
            UiKit.Stretch(_root);
            _ringImage = UiKit.Image(_root, "Ring", UiKit.Circle, Color.white);
            _ring = _ringImage.rectTransform;
            _ring.anchorMin = _ring.anchorMax = _ring.pivot = new Vector2(0.5f, 0.5f);
            var rim = UiKit.Image(_ring, "Rim", IconPainter.Ring, Color.white);
            UiKit.Stretch(rim.rectTransform);
            _knobImage = UiKit.Image(_root, "Knob", UiKit.Circle, Color.white);
            _knob = _knobImage.rectTransform;
            _knob.anchorMin = _knob.anchorMax = _knob.pivot = new Vector2(0.5f, 0.5f);
            Paint();
        }

        void Size(float radiusPixels)
        {
            float scale = _root.lossyScale.x > 0f ? _root.lossyScale.x : 1f;
            float r = radiusPixels / scale;
            _ring.sizeDelta = new Vector2(r * 2f, r * 2f);
            _knob.sizeDelta = new Vector2(r * 0.9f, r * 0.9f);
        }

        Vector2 Local(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out var local);
            return local;
        }

        void Update()
        {
            if (Mathf.Approximately(_alpha, _wanted)) return;
            // Quick in, a little slower out, so a tap doesn't flash it.
            _alpha = Mathf.MoveTowards(_alpha, _wanted, Time.unscaledDeltaTime * (_wanted > _alpha ? 8f : 4f));
            Paint();
        }

        void Paint()
        {
            var ring = UiKit.HudGlass;
            ring.a *= 0.55f * _alpha;
            _ringImage.color = ring;
            foreach (var g in _ring.GetComponentsInChildren<Image>())
                if (g != _ringImage) g.color = new Color(1f, 1f, 1f, 0.55f * _alpha);
            _knobImage.color = new Color(1f, 0.97f, 0.9f, 0.85f * _alpha);
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
