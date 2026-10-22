using System;
using System.Collections.Generic;
using System.Reflection;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The little looping movies on the help pages, built from the game's own pictures. A movie is a
    /// handful of pictures on a fixed 600 × 400 stage, each with keyframes for where it is, how big,
    /// how see-through and how tilted, played round and round like a gif.
    ///
    /// Keyframes rather than chains of tweens: a loop that restarts from a formula can never drift
    /// or pile up, and everything stops the moment the page is closed, because the movie is just a
    /// component on the stage and goes when the stage goes.
    /// </summary>
    public sealed class HelpMovie : MonoBehaviour
    {
        public const float StageWidth = 600f, StageHeight = 400f;

        /// <summary>Seconds before the movie starts again.</summary>
        public float Length = 5f;

        readonly List<Action<float>> _tracks = new();
        float _born;
        RectTransform _stage, _frame;

        public RectTransform Stage => _stage;

        /// <summary>A fresh stage inside <paramref name="frame"/>, scaled to fit it whatever its shape.</summary>
        public static HelpMovie Create(RectTransform frame)
        {
            var stage = UiKit.Rect("Stage", frame);
            stage.anchorMin = stage.anchorMax = stage.pivot = new Vector2(0.5f, 0.5f);
            stage.sizeDelta = new Vector2(StageWidth, StageHeight);
            // The stage is the picture: the sea behind, and nothing drawn past its edges, whatever
            // shape the frame around it is.
            var sea = stage.gameObject.AddComponent<Image>();
            sea.color = HelpArt.Sea;
            sea.raycastTarget = false;
            stage.gameObject.AddComponent<RectMask2D>();
            var movie = stage.gameObject.AddComponent<HelpMovie>();
            movie._stage = stage;
            movie._frame = frame;
            movie._born = Time.unscaledTime;
            return movie;
        }

        void OnEnable() => _born = Time.unscaledTime;

        void OnDisable()
        {
            if (_frameFill != null) _frameFill.color = _frameWas;
        }

        Image _frameFill;
        Color _frameWas;

        /// <summary>
        /// Colours the frame round the stage, which shows at its sides when the frame is a
        /// different shape: the sea's blue is right on the island, but not down in a burrow.
        /// Its own colour comes back when the movie stops.
        /// </summary>
        public void FrameFill(Func<float, Color> colour)
        {
            if (_frame == null || colour == null || (_frameFill = _frame.GetComponent<Image>()) == null) return;
            _frameWas = _frameFill.color;
            Do(s => _frameFill.color = colour(s));
        }

        void Update() => Step();

        /// <summary>Draws the current moment (called once straight after building, so no frame shows the raw layout).</summary>
        public void Step()
        {
            if (_frame != null)
            {
                var r = _frame.rect;
                float scale = Mathf.Max(0.05f, Mathf.Min(r.width / StageWidth, r.height / StageHeight));
                _stage.localScale = new Vector3(scale, scale, 1f);
            }
            float s = Mathf.Repeat(Time.unscaledTime - _born, Mathf.Max(0.5f, Length));
            foreach (var t in _tracks) t(s);
        }

        // ---------------------------------------------------------------- tracks

        public void Do(Action<float> track) { if (track != null) _tracks.Add(track); }

        public DemoKeys Move(Component t)
        {
            var k = new DemoKeys();
            var r = (RectTransform)t.transform;
            Do(s => r.anchoredPosition = k.Eval(s));
            return k;
        }

        /// <summary>Uniform size, 1 = as built.</summary>
        public DemoKeys Size(Component c)
        {
            var k = new DemoKeys();
            var t = c.transform;
            Do(s => { float v = k.Eval(s).x; t.localScale = new Vector3(v, v, 1f); });
            return k;
        }

        /// <summary>Which way a picture faces: 1 as drawn, -1 mirrored (the island's pictures face left).</summary>
        public DemoKeys Face(Component c)
        {
            var k = new DemoKeys();
            var t = c.transform;
            Do(s =>
            {
                float v = k.Eval(s).x >= 0f ? 1f : -1f;
                var l = t.localScale;
                t.localScale = new Vector3(Mathf.Abs(l.x) * v, l.y, 1f);
            });
            return k;
        }

        public DemoKeys Turn(Component c)
        {
            var k = new DemoKeys();
            var t = c.transform;
            Do(s => t.localRotation = Quaternion.Euler(0f, 0f, k.Eval(s).x));
            return k;
        }

        /// <summary>See-through-ness of a picture, or of a whole group (it gets a CanvasGroup).</summary>
        public DemoKeys Fade(Component c)
        {
            var k = new DemoKeys();
            if (c is Graphic g)
            {
                Do(s => { var col = g.color; col.a = k.Eval(s).x; g.color = col; });
                return k;
            }
            var group = c.GetComponent<CanvasGroup>();
            if (group == null) group = c.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = group.interactable = false;
            Do(s => group.alpha = k.Eval(s).x);
            return k;
        }

        /// <summary>Shown only between two moments (an instant switch, no fade).</summary>
        public void Between(GameObject go, float from, float to) => Do(s =>
        {
            bool on = s >= from && s < to;
            if (go.activeSelf != on) go.SetActive(on);
        });

        /// <summary>A gentle up-and-down, as long as <paramref name="when"/> holds.</summary>
        public void Bob(Component t, float height = 6f, float speed = 9f, Func<float, bool> when = null)
        {
            var r = (RectTransform)t.transform;
            Do(s => r.anchoredPosition = new Vector2(0f, when == null || when(s) ? Mathf.Abs(Mathf.Sin(s * speed)) * height : 0f));
        }

        // ---------------------------------------------------------------- pictures

        public RectTransform Group(string name, Vector2 at, Transform parent = null)
        {
            var r = UiKit.Rect(name, parent != null ? parent : _stage);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = Vector2.zero;
            r.anchoredPosition = at;
            return r;
        }

        /// <summary>A picture fitted inside a square of <paramref name="size"/>, standing on its feet at <paramref name="at"/>.</summary>
        public Image Pic(string name, Sprite sprite, Vector2 at, float size, Transform parent = null, Color? tint = null)
        {
            var img = UiKit.Image(parent != null ? parent : _stage, name, sprite != null ? sprite : IconPainter.Star,
                sprite != null ? tint ?? Color.white : UiKit.Sun);
            img.preserveAspect = true;
            img.type = Image.Type.Simple;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.1f);
            r.sizeDelta = new Vector2(size, size);
            r.anchoredPosition = at;
            return img;
        }

        /// <summary>A plain shape (an island, a card, a ring), centred on <paramref name="at"/>.</summary>
        public Image Shape(string name, Sprite sprite, Vector2 at, Vector2 size, Color colour, Transform parent = null)
        {
            var img = UiKit.Image(parent != null ? parent : _stage, name, sprite, colour);
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = size;
            r.anchoredPosition = at;
            return img;
        }

        /// <summary>A short label with no words in it ("+3", "1/4"), so it needs no translating.</summary>
        public TextMeshProUGUI Label(string name, string text, Vector2 at, float size, Color colour, Transform parent = null)
        {
            var t = UiKit.Text(parent != null ? parent : _stage, name, text, size, colour, true, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            var loc = t.GetComponent<LocText>();
            if (loc != null) { loc.NoScale = true; loc.NoRead = true; }
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(220f, size * 1.4f);
            r.anchoredPosition = at;
            return t;
        }

        /// <summary>A piece of island: sand rim and grass, as an oval.</summary>
        public RectTransform Island(string name, Vector2 at, Vector2 size, Transform parent = null)
        {
            var sand = Shape(name, UiKit.Circle, at, size + new Vector2(26f, 18f), HelpArt.Sand, parent);
            Shape("Grass", UiKit.Circle, Vector2.zero, size, HelpArt.Grass, sand.transform);
            return sand.rectTransform;
        }

        /// <summary>One of the HUD's round buttons, without its caption.</summary>
        public RectTransform RoundButton(string name, Sprite icon, Color fill, Vector2 at, float size = 76f, Transform parent = null)
        {
            var disc = Shape(name, UiKit.Circle, at, new Vector2(size, size), fill, parent);
            var ring = Shape("Ring", IconPainter.Ring, Vector2.zero, new Vector2(size, size), new Color(1f, 1f, 1f, 0.9f), disc.transform);
            ring.raycastTarget = false;
            var ic = Shape("Icon", icon, Vector2.zero, new Vector2(size * 0.56f, size * 0.56f), Color.white, disc.transform);
            ic.preserveAspect = true;
            return disc.rectTransform;
        }

        /// <summary>A rounded white card, like a dialog or a screen.</summary>
        public RectTransform Card(string name, Vector2 at, Vector2 size, Color? header = null, Transform parent = null)
        {
            var card = Shape(name, UiKit.Rounded(22), at, size, UiKit.Surface, parent);
            card.type = Image.Type.Sliced;
            if (header.HasValue)
            {
                var band = Shape("Header", UiKit.Rounded(22), new Vector2(0f, size.y * 0.5f - 22f), new Vector2(size.x, 44f), header.Value, card.transform);
                band.type = Image.Type.Sliced;
            }
            return card.rectTransform;
        }

        /// <summary>A small resource pill (an icon and a number), like the bar at the top.</summary>
        public TextMeshProUGUI Pill(string name, Sprite icon, string amount, Vector2 at, Transform parent = null)
        {
            var pill = Shape(name, UiKit.Rounded(20), at, new Vector2(150f, 50f), UiKit.HudGlass, parent);
            pill.type = Image.Type.Sliced;
            var ic = Pic("Icon", icon, new Vector2(-46f, -20f), 40f, pill.transform);
            ic.rectTransform.pivot = new Vector2(0.5f, 0f);
            return Label("Amount", amount, new Vector2(18f, 0f), 30f, Color.white, pill.transform);
        }

        /// <summary>
        /// Something rising out of a spot and fading, like the "+3" over a sawmill: shown from
        /// <paramref name="at"/> seconds for one second.
        /// </summary>
        public void FloatUp(Sprite icon, string amount, Vector2 from, float at)
        {
            var g = Group("Float", from);
            if (icon != null) Pic("Icon", icon, new Vector2(-26f, -18f), 38f, g);
            Label("Amount", amount, new Vector2(icon != null ? 20f : 0f, 0f), 34f, Color.white, g).outlineWidth = 0.25f;
            Move(g).At(at, from).At(at + 1.1f, from + new Vector2(0f, 70f), DemoEase.Out);
            Fade(g).At(at - 0.01f, 0f).At(at, 1f, DemoEase.Step).At(at + 0.7f, 1f).At(at + 1.1f, 0f);
        }

        /// <summary>
        /// What a worker (or a chest) brings in, as the island shows it: the icon and "+3" pop up at
        /// <paramref name="from"/> at <paramref name="at"/> seconds, hang there a moment, then fly in
        /// an arc to their counter at <paramref name="to"/>, shrinking as they go (ResourcePing).
        /// Returns the moment they land, which is when the counter should count up.
        /// </summary>
        public float Ping(Sprite icon, string amount, Vector2 from, Vector2 to, float at)
        {
            const float pop = 0.25f, hang = 0.35f, fly = 0.9f;
            float leave = at + pop + hang, land = leave + fly;
            var g = Group("Ping", from);
            if (icon != null) Pic("Icon", icon, new Vector2(-24f, -18f), 38f, g);
            var t = Label("Amount", amount, new Vector2(icon != null ? 26f : 0f, 0f), 32f, Color.white, g);
            t.outlineWidth = 0.25f;
            t.outlineColor = new Color32(70, 42, 20, 255);
            var lift = from + new Vector2(0f, 10f);
            Do(s =>
            {
                if (s < leave) { g.anchoredPosition = from + new Vector2(0f, Mathf.Sin((s - at) * 6f) * 3f); return; }
                float e = DemoEase.InOut(Mathf.Clamp01((s - leave) / fly));
                g.anchoredPosition = Vector2.Lerp(lift, to, e) + new Vector2(0f, Mathf.Sin(e * Mathf.PI) * 60f);
            });
            Size(g).At(at, 0f).At(at + pop, 1f, DemoEase.Back).At(leave, 1f).At(land, 0.55f);
            Fade(g).At(at - 0.01f, 0f).At(at, 1f, DemoEase.Step).At(land - 0.05f, 1f).At(land, 0f);
            return land;
        }

        /// <summary>
        /// A counter's number, starting at <paramref name="start"/> and counting to each new value
        /// from its moment, quickly, with a little hop, as the bar at the top does.
        /// </summary>
        public void Tally(TMP_Text label, int start, params (float time, int value)[] changes)
        {
            Do(s =>
            {
                int shown = start, before = start;
                foreach (var (time, value) in changes)
                {
                    if (s < time) break;
                    shown = Mathf.RoundToInt(Mathf.Lerp(before, value, Mathf.Clamp01((s - time) / 0.35f)));
                    before = value;
                }
                var text = shown.ToString();
                if (label.text != text) label.text = text;
            });
            var hop = Size(label).At(0f, 1f);
            foreach (var (time, _) in changes) hop.At(time, 1f).At(time + 0.12f, 1.3f).At(time + 0.28f, 1f);
        }

        /// <summary>A burst of little sparkles at a spot, at a moment.</summary>
        public void Sparkle(Vector2 at, float when, Color? colour = null, int count = 6)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f + 0.4f;
                var sp = Shape("Sparkle", IconPainter.Sparkle, at, new Vector2(28f, 28f), colour ?? UiKit.Sun);
                var to = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 70f;
                Move(sp.transform).At(when, at).At(when + 0.6f, to, DemoEase.Out);
                Fade(sp).At(when - 0.01f, 0f).At(when, 1f, DemoEase.Step).At(when + 0.6f, 0f);
                Size(sp.transform).At(when, 0.4f).At(when + 0.3f, 1.1f, DemoEase.Out).At(when + 0.6f, 0.6f);
            }
        }

        // ---------------------------------------------------------------- the finger

        /// <summary>The tutorial's pointing hand, fingertip at <c>anchoredPosition</c>, with ripples where it presses.</summary>
        public Finger Hand() => new Finger(this, false);

        /// <summary>Just a fingertip dot (for pinching: two of them).</summary>
        public Finger Dot() => new Finger(this, true);

        public sealed class Finger
        {
            readonly HelpMovie _m;
            readonly DemoKeys _move, _size, _fade;
            readonly RectTransform _r;

            internal Finger(HelpMovie m, bool dot)
            {
                _m = m;
                var art = dot ? null : TutorialPointer.HandSprite;
                Image img;
                if (art != null)
                {
                    img = m.Shape("Hand", art, Vector2.zero, new Vector2(130f, 130f), Color.white);
                    img.preserveAspect = true;
                    img.rectTransform.pivot = new Vector2(0.48f, 0.06f);   // the fingertip
                }
                else
                {
                    // No hand art to hand (or two fingers): a soft fingertip.
                    img = m.Shape("Finger", UiKit.Circle, Vector2.zero, new Vector2(54f, 54f), new Color(1f, 1f, 1f, 0.85f));
                    m.Shape("Rim", IconPainter.Ring, Vector2.zero, new Vector2(54f, 54f), new Color(0.2f, 0.15f, 0.1f, 0.5f), img.transform);
                }
                img.raycastTarget = false;
                _r = img.rectTransform;
                _move = m.Move(_r);
                _size = m.Size(_r);
                _fade = m.Fade(img);
                _size.At(0f, 1f);
            }

            /// <summary>The finger is at <paramref name="at"/> by <paramref name="time"/>.</summary>
            public Finger At(float time, Vector2 at, Func<float, float> ease = null) { _move.At(time, at, ease); return this; }

            /// <summary>Visible from <paramref name="from"/> to <paramref name="to"/>, fading in and out.</summary>
            public Finger Show(float from, float to)
            {
                _fade.At(from - 0.25f, 0f).At(from, 1f).At(to, 1f).At(to + 0.25f, 0f);
                return this;
            }

            /// <summary>Presses down at <paramref name="time"/> (a ripple where it touches).</summary>
            public Finger Press(float time, bool ripple = true)
            {
                _size.At(time - 0.12f, 1f).At(time, 0.86f);
                if (ripple) _m.Ripple(_move.Eval(time), time);
                return this;
            }

            public Finger Lift(float time)
            {
                _size.At(time, 0.86f).At(time + 0.14f, 1f);
                return this;
            }

            /// <summary>A quick tap: down and straight back up.</summary>
            public Finger Tap(float time) => Press(time).Lift(time + 0.16f);
        }

        /// <summary>The ring that spreads where a finger touches.</summary>
        public void Ripple(Vector2 at, float when)
        {
            var ring = Shape("Ripple", IconPainter.Ring, at, new Vector2(80f, 80f), Color.white);
            Size(ring.transform).At(when, 0.3f).At(when + 0.45f, 1.3f, DemoEase.Out);
            Fade(ring).At(when - 0.01f, 0f).At(when, 0.95f, DemoEase.Step).At(when + 0.45f, 0f);
        }
    }

    /// <summary>A track's keyframes: values at moments, eased between, held before the first and after the last.</summary>
    public sealed class DemoKeys
    {
        readonly List<float> _t = new();
        readonly List<Vector2> _v = new();
        readonly List<Func<float, float>> _e = new();

        public DemoKeys At(float time, Vector2 value, Func<float, float> ease = null)
        {
            int i = _t.Count;
            while (i > 0 && _t[i - 1] > time) i--;
            _t.Insert(i, time);
            _v.Insert(i, value);
            _e.Insert(i, ease ?? DemoEase.InOut);
            return this;
        }

        public DemoKeys At(float time, float value, Func<float, float> ease = null) => At(time, new Vector2(value, 0f), ease);

        public Vector2 Eval(float s)
        {
            int n = _t.Count;
            if (n == 0) return Vector2.zero;
            if (s <= _t[0]) return _v[0];
            for (int i = 1; i < n; i++)
            {
                if (s >= _t[i]) continue;
                float u = (s - _t[i - 1]) / Mathf.Max(1e-4f, _t[i] - _t[i - 1]);
                return Vector2.LerpUnclamped(_v[i - 1], _v[i], _e[i](u));
            }
            return _v[n - 1];
        }
    }

    /// <summary>How a value travels between two keyframes.</summary>
    public static class DemoEase
    {
        public static readonly Func<float, float> Linear = u => u;
        public static readonly Func<float, float> InOut = u => u * u * (3f - 2f * u);
        public static readonly Func<float, float> Out = u => 1f - (1f - u) * (1f - u);
        public static readonly Func<float, float> In = u => u * u;
        /// <summary>Holds the old value, then jumps at the keyframe.</summary>
        public static readonly Func<float, float> Step = u => u >= 1f ? 1f : 0f;
        /// <summary>A little overshoot, for things popping in.</summary>
        public static readonly Func<float, float> Back = u =>
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float v = u - 1f;
            return 1f + c3 * v * v * v + c1 * v * v;
        };
    }

    /// <summary>The game's own pictures for the movies and thumbnails, each with a painted stand-in.</summary>
    public static class HelpArt
    {
        public static readonly Color Sea = UiKit.Hex("8ED1E3");
        public static readonly Color Grass = UiKit.Hex("8FCB6B");
        public static readonly Color Sand = UiKit.Hex("F1DFA8");

        static Sprite Or(Sprite a, Sprite b) => a != null ? a : b;

        /// <summary>The first of these island items that has a picture.</summary>
        public static Sprite Item(params string[] ids)
        {
            var c = ItemCatalog.Instance;
            if (c == null) return null;
            foreach (var id in ids)
            {
                var s = c.Icon(id);
                if (s != null) return s;
            }
            return null;
        }

        /// <summary>A picture from Resources/Hobbies (or a path under Resources with a slash in it).</summary>
        public static Sprite Picture(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return name.IndexOf('/') >= 0 ? Resources.Load<Sprite>(name) : HobbyData.Art(name);
        }

        /// <summary>
        /// What a grove looks like once it's in the ground: a garden is carried as a bed of soil but
        /// planted as single flowers (Harvestable's planted picture); a forest or a hill looks the
        /// same either way, so it falls back to the item's own picture.
        /// </summary>
        public static Sprite Planted(params string[] ids)
        {
            var c = ItemCatalog.Instance;
            if (c == null) return null;
            foreach (var id in ids)
            {
                var template = c.Template(id);
                var node = template != null ? template.GetComponent<Harvestable>() : null;
                var s = Field<Sprite>(node, "plantedSprite");
                if (s == null) s = c.Icon(id);
                if (s != null) return s;
            }
            return null;
        }

        /// <summary>A worker's look (WorkerVisuals on the capybara the shops sell): its tools, its log, its nap.</summary>
        static WorkerVisuals WorkerLook
        {
            get
            {
                var c = ItemCatalog.Instance;
                var template = c != null ? c.Template("Capybara") : null;
                return template != null ? template.GetComponent<WorkerVisuals>() : null;
            }
        }

        /// <summary>The axe for a sawmill's workers, the pickaxe for a mine's.</summary>
        public static Sprite Tool(ResourceKind kind) => Field<Sprite>(WorkerLook, kind == ResourceKind.Gold ? "pickaxe" : "axe");
        /// <summary>The log a sawmill's worker carries home (a mine's carries nothing you can see).</summary>
        public static Sprite Log => Field<Sprite>(WorkerLook, "log");
        /// <summary>A capybara asleep, curled up.</summary>
        public static Sprite Asleep => Or(Field<Sprite>(WorkerLook, "sleeping"), HobbyData.Art("Sleep_Capybara"));

        /// <summary>A picture the game keeps in a private field, read rather than copied, so the movies always match.</summary>
        static T Field<T>(Component c, string name) where T : class
        {
            if (c == null) return null;
            var f = c.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return f != null ? f.GetValue(c) as T : null;
        }

        /// <summary>You: the bare capybara from the wardrobe, or a worker if that isn't drawn.</summary>
        public static Sprite You => Or(Wardrobe.Ready ? Wardrobe.BaseSprite : null, Item("Capybara"));
        public static Sprite Worker => Or(Item("Capybara"), You);

        public static Sprite Resource(ResourceKind kind)
        {
            var s = UiKit.ResourceIcon(kind);
            if (s == null && GameArt.Instance != null) s = GameArt.Instance.Icon(kind);
            return s;
        }

        public static Sprite Heart => Or(GameArt.Instance != null ? GameArt.Instance.heart : null, IconPainter.Heart);
        public static Sprite ChestShut => GameArt.Instance != null ? GameArt.Instance.chestClosed : null;
        public static Sprite ChestOpen => GameArt.Instance != null ? GameArt.Instance.chestOpen : null;
        public static Sprite Apple => GameArt.Instance != null ? GameArt.Instance.apple : null;

        static Sprite _question;

        /// <summary>A round "?" for the Help button and the title of its screen.</summary>
        public static Sprite Question => _question != null ? _question : _question = IconPainter.Paint(128, (x, y) =>
            Mathf.Min(
                IconPainter.Line(x, y, 0.14f, -0.42f, 0.36f, -0.3f, 0.62f, 0f, 0.74f, 0.32f, 0.62f, 0.42f, 0.36f, 0.3f, 0.12f, 0.04f, -0.06f, 0.04f, -0.24f),
                IconPainter.Disc(x, y, 0.04f, -0.6f, 0.15f)));
    }
}
