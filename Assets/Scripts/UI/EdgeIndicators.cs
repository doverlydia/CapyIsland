using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Arrows around the edge of the screen pointing at things you cannot see: bubbles (showing what
    /// is inside), pests to bomb or tame, anyone with news, and wherever the guide's hand is pointing.
    /// Things in the same direction share one arrow with a count. Tap an arrow and your capybara
    /// walks over there (touch the ground to take over again).
    /// </summary>
    public class EdgeIndicators : MonoBehaviour
    {
        const int MaxShown = 10;
        const float MergeDistance = 120f;
        const float Size = 96f;

        /// <summary>Canvas-unit margins inside the safe area: clear of the resource bar and tracker.</summary>
        public Vector4 Insets = new(70f, 80f, 70f, 300f);   // left, bottom, right, top

        class Widget
        {
            public RectTransform Root, Arrow;
            public Image Ring, Icon, ArrowImage;
            public TextMeshProUGUI Count, Label;
            public RectTransform LabelPill;
            public Vector2 Target;
            public MarkerKind Kind;
        }

        readonly List<Widget> _pool = new();
        static EdgeIndicators _instance;

        /// <summary>The first arrow on screen right now, or null (for the arrows tip to point at).</summary>
        public static RectTransform FirstShown
        {
            get
            {
                if (_instance == null) return null;
                foreach (var w in _instance._pool) if (w.Root != null && w.Root.gameObject.activeInHierarchy) return w.Root;
                return null;
            }
        }
        RectTransform _rect;

        struct Hit { public IMapMarker Marker; public Vector2 Local; public float Distance; }
        readonly List<Hit> _hits = new();
        readonly List<(Widget w, Vector2 pos, int count)> _placed = new();

        void Awake()
        {
            _rect = (RectTransform)transform;
            _instance = this;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || UiScreen.AnyOpen || SpeechDialog.IsOpen) { HideAll(0); return; }

            var bounds = _rect.rect;
            float left = bounds.xMin + Insets.x, right = bounds.xMax - Insets.z;
            float bottom = bounds.yMin + Insets.y, top = bounds.yMax - Insets.w;
            var centre = new Vector2((left + right) * 0.5f, (bottom + top) * 0.5f);
            float hx = (right - left) * 0.5f, hy = (top - bottom) * 0.5f;
            Vector2 camPos = cam.transform.position;

            _hits.Clear();
            foreach (var m in MapMarkers.All)
            {
                if (m is Object o && o == null) continue;
                if (!m.MarkerActive || m.MarkerKind == MarkerKind.Tower) continue;
                var world = m.MarkerPosition;
                var screen = cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, null, out var local)) continue;
                // Visible: no arrow needed.
                // Visible, or near enough the edge that most of it shows: no arrow needed.
                const float slack = 70f;
                if (local.x > bounds.xMin - slack && local.x < bounds.xMax + slack && local.y > bounds.yMin - slack && local.y < bounds.yMax - 40f) continue;
                _hits.Add(new Hit { Marker = m, Local = local, Distance = (world - camPos).sqrMagnitude });
            }

            // Most urgent first, nearest first within a kind: they claim the arrow a cluster shows.
            _hits.Sort((a, b) =>
            {
                int pa = Priority(a.Marker.MarkerKind), pb = Priority(b.Marker.MarkerKind);
                return pa != pb ? pb.CompareTo(pa) : a.Distance.CompareTo(b.Distance);
            });

            // Old Yuzu's "go here" pointing at something that has its own arrow (a quest giver, a
            // bubble): show that one instead — knowing who is there beats a second arrow under it.
            _hits.RemoveAll(h => h.Marker.MarkerKind == MarkerKind.Guide && _hits.Exists(o =>
                o.Marker.MarkerKind != MarkerKind.Guide && (o.Marker.MarkerPosition - h.Marker.MarkerPosition).sqrMagnitude < 9f));

            _placed.Clear();
            int used = 0;
            foreach (var hit in _hits)
            {
                var d = hit.Local - centre;
                if (d.sqrMagnitude < 1e-4f) continue;
                float t = Mathf.Min(hx / Mathf.Max(1e-4f, Mathf.Abs(d.x)), hy / Mathf.Max(1e-4f, Mathf.Abs(d.y)));
                var edge = centre + d * t;

                // Same kind, same direction: count it on the existing arrow.
                bool merged = false;
                for (int i = 0; i < _placed.Count; i++)
                {
                    var (w, pos, count) = _placed[i];
                    if (w.Kind != hit.Marker.MarkerKind || (pos - edge).sqrMagnitude > MergeDistance * MergeDistance) continue;
                    _placed[i] = (w, pos, count + 1);
                    merged = true;
                    break;
                }
                if (merged || used >= MaxShown) continue;

                // A different arrow already sits here: slide along the edge so both can be seen.
                bool sideEdge = Mathf.Abs(Mathf.Abs(edge.x - centre.x) - hx) < 1f;
                var along = sideEdge ? Vector2.up : Vector2.right;
                for (int tries = 1; tries <= 4 && _placed.Exists(pl => (pl.pos - edge).sqrMagnitude < Size * Size * 0.8f); tries++)
                {
                    var shifted = edge + along * Size * 1.05f * ((tries + 1) / 2) * (tries % 2 == 1 ? 1f : -1f);
                    shifted.x = Mathf.Clamp(shifted.x, left, right);
                    shifted.y = Mathf.Clamp(shifted.y, bottom, top);
                    if (!_placed.Exists(pl => (pl.pos - shifted).sqrMagnitude < Size * Size * 0.8f)) { edge = shifted; break; }
                }

                // Never under Old Yuzu: an arrow that would land on her picture or bubble goes up above her.
                var ghost = GhostDialogue.Instance;
                if (ghost != null)
                {
                    var screenPt = RectTransformUtility.WorldToScreenPoint(null, _rect.TransformPoint(edge));
                    float scale = _rect.lossyScale.x;
                    if (ghost.Covers(screenPt, Size * 0.5f * scale)
                        && RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, new Vector2(screenPt.x, ghost.TopOnScreen + Size * 0.7f * scale), null, out var above))
                        edge = new Vector2(edge.x, Mathf.Min(above.y, top));
                }

                var widget = Get(used++);
                widget.Kind = hit.Marker.MarkerKind;
                widget.Target = hit.Marker.MarkerPosition;
                Dress(widget, hit.Marker);
                widget.Root.anchoredPosition = edge;
                float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                widget.Arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
                var outward = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                widget.Arrow.anchoredPosition = outward * (Size * 0.5f + 6f);
                // The name sits on the screen side of the ring, opposite the arrow, so it is never
                // pushed off the edge.
                bool labelled = SettingsScreen.ArrowLabels;
                widget.LabelPill.gameObject.SetActive(labelled);
                if (labelled)
                {
                    widget.Label.text = hit.Marker.MarkerLabel ?? LabelOf(widget.Kind);   // what is there, by name
                    widget.LabelPill.anchoredPosition = -outward * (Size * 0.5f + 30f);
                }
                _placed.Add((widget, edge, 1));
            }

            foreach (var (w, _, count) in _placed)
            {
                w.Count.transform.parent.gameObject.SetActive(count > 1);
                w.Count.text = count > 9 ? "9+" : count.ToString();
            }
            HideAll(used);
        }

        static int Priority(MarkerKind k) => k switch
        {
            MarkerKind.Guide => 4,
            MarkerKind.Quest => 3,
            MarkerKind.Pest => 2,
            MarkerKind.Friend => 2,
            _ => 1,
        };

        static string LabelOf(MarkerKind k) => k switch
        {
            MarkerKind.Guide => "Go here",
            MarkerKind.Quest => "Quest",
            MarkerKind.Pest => "Pests",
            MarkerKind.Friend => "Visitor",
            _ => "Bubble",
        };

        static Color ColourOf(MarkerKind k) => k switch
        {
            MarkerKind.Guide => UiKit.Primary,
            MarkerKind.Quest => UiKit.Sun,
            MarkerKind.Pest => UiKit.Danger,
            MarkerKind.Friend => UiKit.Hex("5B9BD5"),
            _ => UiKit.Spring,
        };

        void Dress(Widget w, IMapMarker m)
        {
            var colour = ColourOf(m.MarkerKind);
            w.Ring.color = colour;
            w.ArrowImage.color = colour;
            var icon = m.MarkerIcon;
            w.Icon.sprite = icon != null ? icon : IconPainter.Star;
            w.Icon.color = icon != null ? Color.white : colour;

            // Pests and the guide's hand pulse: they want attention now.
            bool pulse = m.MarkerKind == MarkerKind.Pest || m.MarkerKind == MarkerKind.Guide;
            float s = pulse ? 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.06f : 1f;
            w.Root.localScale = new Vector3(s, s, 1f);
        }

        Widget Get(int index)
        {
            while (_pool.Count <= index) _pool.Add(Make());
            var w = _pool[index];
            if (!w.Root.gameObject.activeSelf)
            {
                w.Root.gameObject.SetActive(true);
                w.Root.DOKill();
                w.Root.localScale = Vector3.one * 0.4f;
                w.Root.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }
            return w;
        }

        void HideAll(int from)
        {
            for (int i = from; i < _pool.Count; i++)
                if (_pool[i].Root.gameObject.activeSelf) _pool[i].Root.gameObject.SetActive(false);
        }

        Widget Make()
        {
            var w = new Widget();
            w.Root = UiKit.Rect("Indicator", transform);
            w.Root.anchorMin = w.Root.anchorMax = new Vector2(0.5f, 0.5f);
            w.Root.sizeDelta = new Vector2(Size, Size);

            var arrow = UiKit.Image(w.Root, "Arrow", IconPainter.Arrow, Color.white);
            w.Arrow = arrow.rectTransform;
            w.Arrow.sizeDelta = new Vector2(44f, 44f);
            w.ArrowImage = arrow;

            var shadow = UiKit.Image(w.Root, "Shadow", UiKit.Circle, new Color(0f, 0f, 0f, 0.25f));
            shadow.rectTransform.sizeDelta = new Vector2(Size, Size);
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -5f);

            w.Ring = UiKit.Image(w.Root, "Ring", UiKit.Circle, Color.white, true);
            w.Ring.rectTransform.sizeDelta = new Vector2(Size, Size);
            var face = UiKit.Image(w.Ring.transform, "Face", UiKit.Circle, Color.white);
            UiKit.Stretch(face.rectTransform, 7f, 7f, 7f, 7f);
            w.Icon = UiKit.Image(face.transform, "Icon", null, Color.white);
            w.Icon.preserveAspect = true;
            UiKit.Stretch(w.Icon.rectTransform, 9f, 9f, 9f, 9f);

            w.Count = UiKit.Badge(w.Ring.transform, UiKit.Ink, 40f);

            var pill = UiKit.Image(w.Root, "LabelPill", UiKit.Rounded(18), UiKit.HudGlass);
            w.LabelPill = pill.rectTransform;
            w.LabelPill.anchorMin = w.LabelPill.anchorMax = w.LabelPill.pivot = new Vector2(0.5f, 0.5f);
            var ph = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            ph.padding = new RectOffset(14, 14, 4, 6);
            ph.childControlWidth = ph.childControlHeight = true;
            pill.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            pill.gameObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            w.Label = UiKit.Text(pill.transform, "Label", "", 26f, Color.white, true, TextAlignmentOptions.Center);
            w.Label.textWrappingMode = TextWrappingModes.NoWrap;
            w.Label.overflowMode = TextOverflowModes.Overflow;
            w.LabelPill.gameObject.SetActive(false);

            var button = w.Ring.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                Sfx.Play(SfxKind.Tap, 0.5f);
                PlayerAvatar.GoTo(w.Target);
            });
            w.Ring.gameObject.AddComponent<PressSquash>();

            w.Root.gameObject.SetActive(false);
            return w;
        }
    }
}
