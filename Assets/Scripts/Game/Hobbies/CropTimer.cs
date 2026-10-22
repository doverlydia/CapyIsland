using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The time left on a growing crop, floating just over it: a small dark pill with a clock face
    /// and the minutes and seconds counting down ("1:42"), the way the help book's gardening movie
    /// shows it. The clock's hand ticks round once a minute and points straight up as each minute
    /// runs out.
    ///
    /// Belongs to a <see cref="Gardening.PlotView"/>, which says where and how long; the pill only
    /// redraws when the shown second changes. Like the island's other floating labels it draws over
    /// the island (and its weather) rather than being sorted in among the props, under bubbles.
    /// </summary>
    public class CropTimer : MonoBehaviour
    {
        // In world units: the pill's height, the clock face, and the room around them.
        const float Height = 0.5f;
        const float Face = 0.34f;
        const float PadLeft = 0.09f, Gap = 0.07f, PadRight = 0.16f;
        const float Lift = 0.08f;   // clear air between the top of the crop and the pill
        const float FontSize = 2.8f;
        const float MaxGrow = 1.5f; // Easy Reading grows the whole pill, but not over its neighbours
        const int Order = Bubble.SortBase - 120;   // over the island and Atmosphere, under station guides and bubbles

        static Sprite _handArt;

        /// <summary>A clock hand from the middle up, with the pin it turns on, painted once.</summary>
        static Sprite HandArt
        {
            get
            {
                if (_handArt != null) return _handArt;
                _handArt = UI.IconPainter.Paint(96, (x, y) => Mathf.Min(
                    UI.IconPainter.Capsule(x, y, 0f, 0f, 0f, 0.74f, 0.11f),
                    UI.IconPainter.Disc(x, y, 0f, 0f, 0.19f)));
                _handArt.name = "CropTimer_Hand";
                return _handArt;
            }
        }

        Transform _body;   // what pops in, and grows with Easy Reading
        SpriteRenderer _back, _face, _hand;
        TextMeshPro _label;
        bool _shown;
        int _seconds = -1;
        int _length = -1;
        int _a11y = -1;
        float _grow = 1f;

        /// <summary>Makes a (hidden) countdown over <paramref name="plot"/>.</summary>
        public static CropTimer Create(Transform plot)
        {
            var go = new GameObject("CropTimer");
            go.transform.SetParent(plot, false);
            var t = go.AddComponent<CropTimer>();
            t.Build();
            go.SetActive(false);
            return t;
        }

        void Build()
        {
            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);

            // A capsule one unit tall (its picture is drawn to fit the digits in Layout), scaled to the pill's height.
            _back = Part("Back", Capsule(3f), UI.UiKit.HudGlass, Order);
            _back.transform.localScale = Vector3.one * Height;

            _face = Part("Face", UI.UiKit.Circle, Color.white, Order + 1);
            _face.transform.localScale = Vector3.one * (Face / Mathf.Max(0.01f, UI.UiKit.Circle.bounds.size.x));
            _hand = Part("Hand", HandArt, UI.UiKit.Ink, Order + 2);
            _hand.transform.localScale = Vector3.one * (Face / Mathf.Max(0.01f, HandArt.bounds.size.x));

            var label = new GameObject("Left");
            label.transform.SetParent(_body, false);
            _label = label.AddComponent<TextMeshPro>();
            if (UI.UiKit.HeadingFont != null) _label.font = UI.UiKit.HeadingFont;
            _label.fontSize = FontSize;
            _label.alignment = TextAlignmentOptions.MidlineLeft;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.color = Color.white;
            _label.rectTransform.pivot = new Vector2(0f, 0.5f);
            _label.rectTransform.sizeDelta = new Vector2(1f, Height);
            _label.sortingOrder = Order + 2;
            // Digits only, so nothing to translate; the pill grows as a whole for Easy Reading instead.
            var loc = UI.LocText.Attach(_label);
            if (loc != null) { loc.NoScale = true; loc.NoRead = true; }
        }

        SpriteRenderer Part(string name, Sprite sprite, Color colour, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_body, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = colour;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>
        /// Shows <paramref name="secondsLeft"/> in a pill whose bottom floats just over
        /// <paramref name="top"/> (the top of the growing crop, in the world).
        /// </summary>
        public void Show(Vector3 top, int secondsLeft)
        {
            if (_a11y != UI.Accessibility.Version)
            {
                // Easy Reading's text size changed (or this is the first showing).
                _a11y = UI.Accessibility.Version;
                _grow = Mathf.Clamp(UI.Accessibility.TextScale, 1f, MaxGrow);
                if (_shown)
                {
                    _body.DOKill();
                    _body.localScale = Vector3.one * _grow;
                }
            }

            if (!_shown)
            {
                _shown = true;
                gameObject.SetActive(true);
                _body.DOKill();
                _body.localScale = Vector3.one * (0.6f * _grow);
                _body.DOScale(_grow, 0.25f).SetEase(Ease.OutBack);
            }

            // Undo the plot's own scale (and any mirroring), so the pill is measured in world units.
            var parent = transform.parent;
            if (parent != null)
            {
                var ls = parent.lossyScale;
                if (Mathf.Abs(ls.x) > 0.0001f && Mathf.Abs(ls.y) > 0.0001f)
                    transform.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f);
            }
            transform.position = top + Vector3.up * (Lift + Height * 0.5f * _grow);

            if (secondsLeft == _seconds) return;
            _seconds = secondsLeft;
            string text = Format(secondsLeft);
            if (text.Length != _length) Layout(text);
            _label.text = text;
            // The hand ticks round once a minute, straight up as each minute runs out.
            _hand.transform.localRotation = Quaternion.Euler(0f, 0f, 6f * (secondsLeft % 60));
        }

        /// <summary>Gone: the crop is ready, picked, or the plot is being moved.</summary>
        public void Hide(bool quick = false)
        {
            if (!_shown) return;
            _shown = false;
            _seconds = -1;
            _body.DOKill();
            if (quick || !gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
                return;
            }
            _body.DOScale(0f, 0.18f).SetEase(Ease.InBack).OnComplete(() =>
            {
                if (this != null && !_shown) gameObject.SetActive(false);
            });
        }

        /// <summary>"1:42", or "1:05:00" for anything that takes hours.</summary>
        static string Format(int secs)
        {
            if (secs < 0) secs = 0;
            if (secs >= 3600) return (secs / 3600) + ":" + (secs / 60 % 60).ToString("00") + ":" + (secs % 60).ToString("00");
            return (secs / 60) + ":" + (secs % 60).ToString("00");
        }

        /// <summary>Fits the pill around text of this length (measured as zeros, so it doesn't twitch as digits change).</summary>
        void Layout(string text)
        {
            _length = text.Length;
            var sample = new System.Text.StringBuilder(text.Length);
            foreach (char c in text) sample.Append(char.IsDigit(c) ? '0' : c);
            float textW = _label.GetPreferredValues(sample.ToString()).x;
            float w = PadLeft + Face + Gap + textW + PadRight;
            float left = -w * 0.5f;

            _back.sprite = Capsule(w / Height);

            float faceX = left + PadLeft + Face * 0.5f;
            _face.transform.localPosition = new Vector3(faceX, 0f, 0f);
            _hand.transform.localPosition = new Vector3(faceX, 0f, 0f);
            _label.rectTransform.sizeDelta = new Vector2(textW + 0.1f, Height);
            _label.transform.localPosition = new Vector3(left + PadLeft + Face + Gap, 0f, 0f);
        }

        static readonly System.Collections.Generic.Dictionary<int, Sprite> Capsules = new();

        /// <summary>
        /// A white capsule one unit tall and <paramref name="aspect"/> units wide, drawn whole
        /// (a sliced sprite on a SpriteRenderer lost its middle). One per width, shared by every plot.
        /// </summary>
        static Sprite Capsule(float aspect)
        {
            int key = Mathf.Max(10, Mathf.RoundToInt(aspect * 10f));
            if (Capsules.TryGetValue(key, out var made) && made != null) return made;
            const int h = 64;
            int w = Mathf.RoundToInt(h * key / 10f);
            float r = h * 0.5f;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float cx = x + 0.5f, cy = y + 0.5f;
                    float dx = cx - Mathf.Clamp(cx, r, w - r), dy = cy - r;
                    float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            made = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), h);
            Capsules[key] = made;
            return made;
        }

        void OnDestroy()
        {
            if (_body != null) _body.DOKill();
        }
    }
}
