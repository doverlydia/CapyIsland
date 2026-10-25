using DG.Tweening;
using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The whole archipelago at a glance: a live picture of every island taken by a second camera,
    /// with markers over it for you, bubbles, pests, anyone with news and the tower, and a badge on
    /// the island where a party or contest is going on (its shore glows in the picture). Islands you
    /// cannot reach yet wear a padlock. Drag to look around. Tap a spot and you go there: your
    /// marker hurries across the picture to it, then the map closes and you pop up on the spot.
    /// Or drag your marker there yourself, which does the same.
    /// </summary>
    public class MapScreen : UiScreen
    {
        public static MapScreen Instance { get; private set; }

        protected override string Title => "Island Map";
        protected override Sprite TitleIcon => IconPainter.Map;
        protected override Color AccentColour => UiKit.Spring;
        protected override float MaxWidth => 1700f;

        const float Pad = 3f;   // world units of sea around the land

        Camera _camera;
        RenderTexture _texture;
        Bounds _world;
        ScrollRect _scroll;
        RectTransform _viewport;
        RawImage _picture;
        RectTransform _markerLayer;

        readonly List<(IMapMarker marker, RectTransform widget)> _widgets = new();
        RectTransform _king;
        readonly List<RectTransform> _locks = new();

        // A party or contest going on: a badge on its island, and its legend entry.
        RectTransform _eventBadge, _legendEvent;
        string _eventShown;   // what the badge was made for ("Fishing@3"), so it's remade when that changes

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_texture != null) _texture.Release();
            if (_camera != null) Destroy(_camera.gameObject);
        }

        public static void Show()
        {
            if (Instance != null) Instance.Open();
        }

        protected override void Build(RectTransform body)
        {
            // The map frame, with a legend strip under it.
            var frame = UiKit.Image(body, "Frame", UiKit.Rounded(30), UiKit.Hex("8FD3E0"));
            UiKit.Stretch(frame.rectTransform, 0f, 118f, 0f, 0f);

            _scroll = UiKit.ScrollList(frame.transform, "Scroll", out var listContent, 0f, stacked: false);
            UiKit.Stretch((RectTransform)_scroll.transform, 6f, 6f, 6f, 6f);
            _scroll.horizontal = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _viewport = _scroll.viewport;
            listContent.anchorMin = listContent.anchorMax = listContent.pivot = new Vector2(0.5f, 0.5f);

            var pic = UiKit.Rect("Picture", listContent);
            UiKit.Stretch(pic);
            _picture = pic.gameObject.AddComponent<RawImage>();
            _picture.raycastTarget = true;
            pic.gameObject.AddComponent<MapClick>().Owner = this;

            _markerLayer = UiKit.Rect("Markers", listContent);
            UiKit.Stretch(_markerLayer);

            // Legend: the dots on one line, how to use the map on the line under it (on one line
            // together, an upright phone cut the end off).
            var strip = UiKit.Rect("LegendStrip", body);
            strip.anchorMin = Vector2.zero;
            strip.anchorMax = new Vector2(1f, 0f);
            strip.pivot = new Vector2(0.5f, 0f);
            strip.sizeDelta = new Vector2(0f, 104f);
            var v = strip.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2f;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            var legend = UiKit.Rect("Legend", strip);
            legend.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
            var h = legend.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 26f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            LegendItem(legend, UiKit.Primary, "You");
            LegendItem(legend, UiKit.Spring, "Bubble");
            LegendItem(legend, UiKit.Danger, "Pest");
            LegendItem(legend, UiKit.Sun, "News");
            // Only while a party or contest is on: the island with the glowing shore.
            _legendEvent = LegendItem(legend, UiKit.Hex("E58FB0"), "Event");
            _legendEvent.gameObject.SetActive(false);
            var hint = UiKit.Text(strip, "Hint", "Tap a spot or drag yourself to travel", 24f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            hint.overflowMode = TextOverflowModes.Overflow;
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            GrowsWithText.Add(hint);
        }

        static RectTransform LegendItem(RectTransform parent, Color colour, string label)
        {
            var row = UiKit.Rect(label, parent);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            var dot = UiKit.Image(row, "Dot", UiKit.Circle, colour);
            var le = dot.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = 26f;
            var t = UiKit.Text(row, "Label", label, 26f, UiKit.Ink, true);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            return row;
        }

        protected override void OnOpen()
        {
            EnsureCamera();
            if (_camera != null) _camera.enabled = true;
            Layout();
            RebuildLocks();
            RebuildNames();
            _eventShown = "?";   // placed afresh
            CentreOnKing();
            if (!Visit.Active) Game.NewStuff.Clear("map/");   // the new island is on show (yours: a friend's map leaves it waiting)
            // The first time you look at the map, and only then: how to travel on it.
            if (!Game.Guidance.Off && !HudTour.Seen("map"))
            {
                HudTour.MarkSeen("map");
                CardPopup.Show("Map", "See all the islands. Tap a spot to go there, or drag yourself across the map.", IconPainter.Map);
            }
        }

        protected override void OnClose()
        {
            if (_camera != null) _camera.enabled = false;
            // Shut mid-hurry (the back button): the trip is off, and the marker is yours again.
            if (_hurrying)
            {
                _hurrying = false;
                if (_king != null) { _king.DOKill(); _king.localScale = Vector3.one; }
            }
        }

        protected override void Fit()
        {
            base.Fit();
            if (!IsOpen) return;
            Layout();
            RebuildLocks();
            RebuildNames();
            _eventShown = "?";
        }

        // ---------------------------------------------------------------- the picture

        void EnsureCamera()
        {
            if (_camera != null) return;
            var land = LandMap.Instance;
            if (land == null) return;

            bool any = false;
            var b = new Bounds();
            foreach (var piece in land.Pieces)
                foreach (var layer in piece.layers)
                {
                    if (layer == null) continue;
                    if (!any) { b = layer.bounds; any = true; }
                    else b.Encapsulate(layer.bounds);
                }
            if (!any) return;
            b.Expand(new Vector3(Pad * 2f, Pad * 2f, 0f));
            _world = b;

            float aspect = b.size.x / Mathf.Max(0.01f, b.size.y);
            MakeTexture(aspect);

            var main = Camera.main;
            var go = new GameObject("[MapCamera]");
            _camera = go.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = b.size.y * 0.5f;
            _camera.aspect = aspect;
            _camera.transform.position = new Vector3(b.center.x, b.center.y, main != null ? main.transform.position.z : -10f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = main != null ? main.backgroundColor : UiKit.Hex("8FD3E0");
            _camera.cullingMask = ~(1 << 5);   // everything but UI
            _camera.targetTexture = _texture;
            _camera.depth = -50;
            _camera.enabled = false;
        }

        /// <summary>
        /// The picture the map camera draws into. Plain, with no multisampling: phone GPUs can leave
        /// a multisampled target unresolved, which showed as a screen of random coloured squares.
        /// </summary>
        void MakeTexture(float aspect)
        {
            int longest = 1536;
            int w = aspect >= 1f ? longest : Mathf.Clamp(Mathf.RoundToInt(longest * aspect), 256, longest);
            int h = aspect >= 1f ? Mathf.Clamp(Mathf.RoundToInt(longest / aspect), 256, longest) : longest;
            _texture = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32) { name = "IslandMap", antiAliasing = 1 };
            _texture.Create();
            _picture.texture = _texture;
            if (_camera != null) _camera.targetTexture = _texture;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsOpen) return;

            // Drawn every frame while the map is open. Drawing once and keeping the picture failed on
            // phones: the GPU is free to throw a render target's contents away (switching apps, or
            // simply tile memory), and then the map shows garbage until the next draw.
            if (_camera != null)
            {
                if (_texture == null || !_texture.IsCreated())
                {
                    if (_texture != null) _texture.Release();
                    MakeTexture(_world.size.x / Mathf.Max(0.01f, _world.size.y));
                }
                if (!_camera.enabled) _camera.enabled = true;
            }
            UpdateMarkers();
            UpdateEventBadge();
            EdgeScroll();
        }

        /// <summary>Sizes the map to cover the frame, so there is never empty space; drag reveals the rest.</summary>
        void Layout()
        {
            if (_viewport == null || _world.size.x <= 0f) return;
            Canvas.ForceUpdateCanvases();
            var view = _viewport.rect.size;
            float aspect = _world.size.x / _world.size.y;
            float height = Mathf.Max(view.y, view.x / aspect);
            var content = _scroll.content;
            content.sizeDelta = new Vector2(height * aspect, height);
        }

        void CentreOnKing()
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king == null) return;
            var n = Normalised(king.transform.position);
            Canvas.ForceUpdateCanvases();
            _scroll.horizontalNormalizedPosition = n.x;
            _scroll.verticalNormalizedPosition = n.y;
        }

        Vector2 Normalised(Vector2 world) => new(
            Mathf.InverseLerp(_world.min.x, _world.max.x, world.x),
            Mathf.InverseLerp(_world.min.y, _world.max.y, world.y));

        Vector2 ToMap(Vector2 world)
        {
            var size = _scroll.content.sizeDelta;
            var n = Normalised(world);
            return new Vector2((n.x - 0.5f) * size.x, (n.y - 0.5f) * size.y);
        }

        // ---------------------------------------------------------------- markers

        void UpdateMarkers()
        {
            var markers = MapMarkers.All;
            for (int i = _widgets.Count - 1; i >= 0; i--)
            {
                var (m, w) = _widgets[i];
                bool alive = m is Object o && o != null && Contains(markers, m);
                if (!alive) { Destroy(w.gameObject); _widgets.RemoveAt(i); }
            }
            foreach (var m in markers)
            {
                bool show = m.MarkerActive || m.MarkerKind == MarkerKind.Tower;
                var widget = Find(m);
                if (widget == null)
                {
                    // Made the first time it shows, so its picture is the one it has then (a
                    // station's is only there while its capybaras sleep).
                    if (!show) continue;
                    widget = MakeMarker(m);
                    _widgets.Add((m, widget));
                }
                widget.gameObject.SetActive(show);
                if (show) widget.anchoredPosition = ToMap(m.MarkerPosition);
            }

            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king != null)
            {
                if (_king == null)
                {
                    _king = Pin(MapMarkers.SpriteOf(king.gameObject), UiKit.Primary, 88f, "You");
                    // Drag yourself somewhere on the map to go there.
                    _king.GetComponent<Image>().raycastTarget = true;
                    _king.gameObject.AddComponent<KingDrag>().Owner = this;
                }
                if (!_draggingKing && !_hurrying) _king.anchoredPosition = ToMap(king.transform.position);
                _king.SetAsLastSibling();
            }
        }

        static bool Contains(IReadOnlyList<IMapMarker> list, IMapMarker m)
        {
            for (int i = 0; i < list.Count; i++) if (ReferenceEquals(list[i], m)) return true;
            return false;
        }

        RectTransform Find(IMapMarker m)
        {
            foreach (var (marker, widget) in _widgets) if (ReferenceEquals(marker, m)) return widget;
            return null;
        }

        RectTransform MakeMarker(IMapMarker m)
        {
            var (colour, size) = m.MarkerKind switch
            {
                MarkerKind.Pest => (UiKit.Danger, 60f),
                MarkerKind.Quest => (UiKit.Sun, 70f),
                MarkerKind.Tower => (UiKit.Hex("E58FB0"), 76f),
                MarkerKind.Guide => (UiKit.Primary, 60f),
                MarkerKind.Friend => (UiKit.Hex("5B9BD5"), 64f),
                MarkerKind.Asleep => (EdgeIndicators.AsleepColour, 60f),
                MarkerKind.Npc => (UiKit.Hex("C98B5B"), 64f),
                _ => (UiKit.Spring, 52f),
            };
            // People are named: the island's own folk, anyone with news, a friend who's here.
            bool named = m.MarkerKind is MarkerKind.Npc or MarkerKind.Quest or MarkerKind.Friend;
            return Pin(m.MarkerIcon, colour, size, named ? m.MarkerLabel : null);
        }

        RectTransform Pin(Sprite icon, Color colour, float size, string label)
        {
            var ring = UiKit.Image(_markerLayer, "Pin", UiKit.Circle, colour);
            ring.rectTransform.sizeDelta = new Vector2(size, size);
            var face = UiKit.Image(ring.transform, "Face", UiKit.Circle, Color.white);
            UiKit.Stretch(face.rectTransform, 6f, 6f, 6f, 6f);
            if (icon != null)
            {
                var ic = UiKit.Image(face.transform, "Icon", icon, Color.white);
                ic.preserveAspect = true;
                UiKit.Stretch(ic.rectTransform, 5f, 5f, 5f, 5f);
            }
            if (!string.IsNullOrEmpty(label))
            {
                var tag = UiKit.Image(ring.transform, "Tag", UiKit.Rounded(18), colour);
                var tr = tag.rectTransform;
                tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0f);
                tr.pivot = new Vector2(0.5f, 1f);
                tr.anchoredPosition = new Vector2(0f, 6f);
                tr.sizeDelta = new Vector2(84f, 36f);
                var t = UiKit.Text(tag.transform, "Label", label, 24f, Color.white, true, TextAlignmentOptions.Center);
                UiKit.Stretch(t.rectTransform);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                FitsText.Add(tag, t, new Vector2(24f, 8f), wide: true, tall: true);
            }
            return ring.rectTransform;
        }

        /// <summary>A padlock on every island the king cannot walk to yet.</summary>
        void RebuildLocks()
        {
            foreach (var l in _locks) if (l != null) Destroy(l.gameObject);
            _locks.Clear();
            var land = LandMap.Instance;
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (land == null || king == null) return;

            for (int i = 0; ; i++)
            {
                var island = land.Island(i);
                if (island == null) break;
                Vector2 middle;
                if (island.rects.Count > 0) middle = island.rects[0].center;
                else if (island.circles.Count > 0) middle = island.circles[0];
                else continue;
                var centre = LandMap.WorldToUnity(middle);
                if (land.Reachable(king.transform.position, centre)) continue;

                var badge = UiKit.Image(_markerLayer, "Lock", UiKit.Circle, new Color(0.15f, 0.12f, 0.1f, 0.75f));
                badge.rectTransform.sizeDelta = new Vector2(84f, 84f);
                badge.rectTransform.anchoredPosition = ToMap(centre);
                var ic = UiKit.Image(badge.transform, "Icon", IconPainter.Lock, Color.white);
                UiKit.Stretch(ic.rectTransform, 20f, 20f, 20f, 20f);
                badge.transform.SetAsFirstSibling();
                _locks.Add(badge.rectTransform);
            }
        }

        // ---------------------------------------------------------------- a party or contest

        /// <summary>
        /// While a party or contest is on, a badge on its island says what's happening there
        /// ("Fishing Contest"), for the host and every guest alike, and its legend entry shows.
        /// The event glows in the picture too (EventGlow: a fishing contest lights only the water
        /// along that island's shore), but from this far out a word helps. The badge breathes
        /// gently so the eye finds it.
        /// </summary>
        void UpdateEventBadge()
        {
            bool on = Gathering.Active && Gathering.EventIsland >= 0;
            string key = on ? Gathering.Kind + "@" + Gathering.EventIsland : null;
            if (_legendEvent != null && _legendEvent.gameObject.activeSelf != on) _legendEvent.gameObject.SetActive(on);
            if (key != _eventShown)
            {
                _eventShown = key;
                if (_eventBadge != null) Destroy(_eventBadge.gameObject);
                _eventBadge = on ? MakeEventBadge(Gathering.Kind, Gathering.EventIsland) : null;
                if (on && _legendEvent != null && _legendEvent.childCount > 0 && _legendEvent.GetChild(0).TryGetComponent<Image>(out var dot))
                    dot.color = GatheringPanel.ColourOf(Gathering.Kind);
            }
            if (_eventBadge != null)
                _eventBadge.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(Time.unscaledTime * 3f));
        }

        RectTransform MakeEventBadge(GatheringKind kind, int island)
        {
            var land = LandMap.Instance;
            var piece = land != null ? land.Island(island) : null;
            if (piece == null || piece.circles.Count == 0) return null;
            // The island's middle (its land circles, weighted by size), since the whole island is
            // the event. A fishing contest is only its shore, so that badge sits on the shore
            // where the host started it, by the glowing water, not over the island's grass.
            Vector2 sum = Vector2.zero;
            float weight = 0f;
            foreach (var c in piece.circles) { float a = c.z * c.z; sum += new Vector2(c.x, c.y) * a; weight += a; }
            var middle = LandMap.WorldToUnity(sum / Mathf.Max(0.0001f, weight));
            if (kind == GatheringKind.Fishing && land.IslandAt(Gathering.Arena) == island) middle = Gathering.Arena;

            var colour = GatheringPanel.ColourOf(kind);
            var pill = UiKit.Image(_markerLayer, "Event", UiKit.Rounded(30), UiKit.Surface);
            pill.raycastTarget = false;
            var r = pill.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = ToMap(middle);
            var h = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(8, 22, 8, 8);
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            RtlLayout.Add(h);
            var fit = pill.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var disc = UiKit.Image(r, "Disc", UiKit.Circle, colour);
            disc.raycastTarget = false;
            var dle = disc.gameObject.AddComponent<LayoutElement>();
            dle.preferredWidth = dle.preferredHeight = dle.minWidth = 58f;
            var icon = UiKit.Image(disc.transform, "Icon", GatheringPanel.IconOf(kind), Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            UiKit.Stretch(icon.rectTransform, 10f, 10f, 10f, 10f);
            var label = UiKit.Text(r, "Label", Gathering.NameOf(kind), 30f, Color.Lerp(colour, UiKit.Ink, 0.25f), true);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;

            // Over the island names and padlocks, under the pins.
            r.SetSiblingIndex(Mathf.Min(_markerLayer.childCount - 1, _names.Count + _locks.Count));
            return r;
        }

        // ---------------------------------------------------------------- names

        readonly List<RectTransform> _names = new();

        /// <summary>
        /// The seas, by the openest water in each (Unity units: the islands never move). Names in
        /// <see cref="StoryText.Seas"/>.
        /// </summary>
        static readonly Vector2[] SeaSpots = { new(19f, -16f), new(67f, -46f), new(109f, -18f), new(65f, -72f) };

        /// <summary>
        /// Every island's name and every sea's, so "Found on Mossy Meadow" or "the Kombu Channel"
        /// has somewhere to look. At home only: a friend's island is just the one.
        /// </summary>
        void RebuildNames()
        {
            foreach (var n in _names) if (n != null) Destroy(n.gameObject);
            _names.Clear();
            _taken.Clear();
            var land = LandMap.Instance;
            if (land == null || Visit.Active) return;

            // Island names along the top of each island: the middle is where things stand.
            for (int i = 0; ; i++)
            {
                var island = land.Island(i);
                if (island == null) break;
                if (island.rects.Count == 0 || island.circles.Count == 0) continue;
                float top = float.MinValue;
                foreach (var c in island.circles)
                {
                    top = Mathf.Max(top, LandMap.WorldToUnity(new Vector2(c.x, c.y - c.z)).y);
                    top = Mathf.Max(top, LandMap.WorldToUnity(new Vector2(c.x, c.y + c.z)).y);
                }
                var middle = LandMap.WorldToUnity(island.rects[0].center);
                Tag(StoryText.Name(island.name), ToMap(new Vector2(middle.x, top)) - new Vector2(0f, 46f), 32f, new Color(0.12f, 0.2f, 0.24f, 0.62f));
            }

            // The seas: lighter, as if written on the water.
            for (int i = 0; i < SeaSpots.Length && i < StoryText.Seas.Length; i++)
                Tag(StoryText.Seas[i], ToMap(SeaSpots[i]), 26f, new Color(0.16f, 0.3f, 0.55f, 0.45f));
            // Under the pins, over the picture.
            foreach (var n in _names) n.SetSiblingIndex(0);
        }

        readonly List<Rect> _taken = new();   // where the names already placed sit

        /// <summary>A name in a pill; stepped up or down out of the way if another is already there.</summary>
        void Tag(string text, Vector2 at, float size, Color back)
        {
            var pill = UiKit.Image(_markerLayer, "Name", UiKit.Rounded(18), back);
            var t = UiKit.Text(pill.transform, "Label", text, size, Color.white, true, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            // Measured as it will be drawn: translated, and at Easy Reading's size.
            t.GetComponent<LocText>().Refresh();
            var fits = t.GetPreferredValues(Loc.T(text));
            var box = new Vector2(fits.x + size * 1.1f, size * 1.6f * Accessibility.TextScale);
            pill.rectTransform.sizeDelta = box;

            Vector2 best = at;
            foreach (float step in new[] { 0f, -1f, 1f, -2f, 2f, -3f, 3f })
            {
                var p = at + new Vector2(0f, step * (box.y + 4f));
                var r = new Rect(p - box * 0.5f, box);
                bool clear = true;
                foreach (var other in _taken) if (other.Overlaps(r)) { clear = false; break; }
                if (clear) { best = p; break; }
            }
            _taken.Add(new Rect(best - box * 0.5f, box));
            pill.rectTransform.anchoredPosition = best;
            UiKit.Stretch(t.rectTransform);
            _names.Add(pill.rectTransform);
        }

        // ---------------------------------------------------------------- travelling

        bool _draggingKing;
        TextMeshProUGUI _toast;
        Vector2 _dragScreen;   // where the finger is while dragging yourself
        Camera _dragCamera;

        const float EdgeBand = 140f;     // how near an edge (canvas units) the map starts to scroll
        const float EdgeSpeed = 1400f;   // canvas units a second, right at the edge

        Vector2 ToWorld(Vector2 local)
        {
            var size = _scroll.content.rect.size;
            var n = new Vector2(local.x / size.x + 0.5f, local.y / size.y + 0.5f);
            return new Vector2(Mathf.Lerp(_world.min.x, _world.max.x, n.x), Mathf.Lerp(_world.min.y, _world.max.y, n.y));
        }

        bool LocalPoint(PointerEventData e, out Vector2 local) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_scroll.content, e.position, e.pressEventCamera, out local);

        void BeginTravelDrag(PointerEventData e)
        {
            if (_hurrying) return;   // already on the way somewhere
            _draggingKing = true;
            _dragScreen = e.position;
            _dragCamera = e.pressEventCamera;
            _king.DOKill();
            _king.DOScale(1.2f, 0.15f).SetUpdate(true);
        }

        void TravelDrag(PointerEventData e)
        {
            if (!_draggingKing) return;
            _dragScreen = e.position;
            _dragCamera = e.pressEventCamera;
            if (LocalPoint(e, out var local)) _king.anchoredPosition = local;
        }

        /// <summary>
        /// Holding yourself near an edge of the map scrolls it that way (faster the nearer the
        /// edge), so you can carry yourself further than the part of the map on screen.
        /// </summary>
        void EdgeScroll()
        {
            if (!_draggingKing || _viewport == null || _king == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, _dragScreen, _dragCamera, out var p)) return;
            var view = _viewport.rect;
            float Push(float at, float min, float max) =>
                at < min + EdgeBand ? -Mathf.Clamp01(1f - (at - min) / EdgeBand)
                : at > max - EdgeBand ? Mathf.Clamp01(1f - (max - at) / EdgeBand) : 0f;
            var push = new Vector2(Push(p.x, view.xMin, view.xMax), Push(p.y, view.yMin, view.yMax));
            if (push == Vector2.zero) return;

            var spare = _scroll.content.rect.size - view.size;   // how far the map can move each way
            float step = EdgeSpeed * Time.unscaledDeltaTime;
            _scroll.velocity = Vector2.zero;
            if (spare.x > 1f) _scroll.horizontalNormalizedPosition = Mathf.Clamp01(_scroll.horizontalNormalizedPosition + push.x * step / spare.x);
            if (spare.y > 1f) _scroll.verticalNormalizedPosition = Mathf.Clamp01(_scroll.verticalNormalizedPosition + push.y * step / spare.y);

            // The map slid under the finger: keep yourself under it.
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_scroll.content, _dragScreen, _dragCamera, out var local))
                _king.anchoredPosition = local;
        }

        void EndTravelDrag(PointerEventData e)
        {
            if (!_draggingKing) return;
            _king.DOKill();
            _king.DOScale(1f, 0.15f).SetUpdate(true);
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king == null || !LocalPoint(e, out var local)) { _draggingKing = false; return; }
            var world = ToWorld(local);
            _draggingKing = false;   // on a refusal, springs back to where you really are
            if (Refused(king, world)) return;
            Close();
            king.TravelTo(world);
        }

        /// <summary>
        /// The sea, or an island whose bridge isn't open yet: says so, and the trip is off. The
        /// same for a tap and a drag.
        /// </summary>
        bool Refused(PlayerAvatar king, Vector2 world)
        {
            string why = null;
            var land = LandMap.Instance;
            if (!BunniSpace.PointOnLand(world)) why = "You can't stand in the sea! Pick a spot on land.";
            else if (land != null && !land.Reachable(king.transform.position, world)) why = "That island is still closed. Open its bridge first!";
            if (why == null) return false;
            Toast(why);
            Sfx.Play(SfxKind.Tap, 0.4f);
            return true;
        }

        void Toast(string line)
        {
            if (_toast == null)
            {
                var pill = UiKit.Image(_viewport, "Toast", UiKit.Rounded(24), new Color(0.15f, 0.12f, 0.1f, 0.85f));
                pill.raycastTarget = false;
                var r = pill.rectTransform;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 1f);
                r.anchoredPosition = new Vector2(0f, -20f);
                r.sizeDelta = new Vector2(640f, 70f);
                _toast = UiKit.Text(pill.transform, "Text", "", 26f, Color.white, true, TextAlignmentOptions.Center);
                UiKit.Stretch(_toast.rectTransform, 16f, 0f, 16f, 0f);
                _toast.enableAutoSizing = true;
                _toast.fontSizeMin = 16f;
                _toast.fontSizeMax = 26f;
            }
            var g = _toast.transform.parent.GetComponent<Image>();
            _toast.text = line;
            g.gameObject.SetActive(true);
            g.DOKill();
            _toast.DOKill();
            g.color = new Color(0.15f, 0.12f, 0.1f, 0.85f);
            _toast.alpha = 1f;
            g.DOFade(0f, 0.5f).SetDelay(2.2f).SetUpdate(true);
            _toast.DOFade(0f, 0.5f).SetDelay(2.2f).SetUpdate(true);
        }

        /// <summary>Your marker on the map: drag it somewhere to travel there (held near an edge, the map scrolls that way).</summary>
        class KingDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public MapScreen Owner;
            public void OnBeginDrag(PointerEventData e) => Owner?.BeginTravelDrag(e);
            public void OnDrag(PointerEventData e) => Owner?.TravelDrag(e);
            public void OnEndDrag(PointerEventData e) => Owner?.EndTravelDrag(e);
        }

        // ---------------------------------------------------------------- tapping

        bool _hurrying;   // your marker is on its way across the map to a tapped spot

        const float HurrySpeed = 2600f;   // canvas units a second across the map picture

        /// <summary>
        /// A tap on the map: go there. Your marker hurries across the picture to the spot (well
        /// under a second, however far), then the map closes and you pop up there, just as if you
        /// had dragged yourself. The sea or a closed island says why not, and nothing moves. Taps
        /// while the marker is on its way are ignored.
        /// </summary>
        internal void Clicked(PointerEventData e)
        {
            if (e.dragging || _hurrying || _draggingKing) return;
            if (!LocalPoint(e, out var local)) return;
            var world = ToWorld(local);

            // No capybara to send (nobody to move): just look there, as the map used to.
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king == null || _king == null)
            {
                Close();
                CameraRig.CameraFocus.Show(world, 3f);
                return;
            }
            if (Refused(king, world)) return;

            _hurrying = true;
            Sfx.Play(SfxKind.Tap, 0.5f);
            float seconds = Mathf.Clamp(Vector2.Distance(_king.anchoredPosition, local) / HurrySpeed, 0.25f, 0.55f);
            _king.DOKill();
            _king.SetAsLastSibling();
            // Lifted a little for the trip, like a marker picked up and carried.
            _king.DOScale(1.2f, 0.12f).SetUpdate(true);
            _king.DOAnchorPos(local, seconds).SetEase(Ease.InOutQuad).SetUpdate(true).OnComplete(() =>
            {
                _hurrying = false;
                if (_king != null) _king.localScale = Vector3.one;
                if (king == null || !IsOpen) return;
                Close();
                king.TravelTo(world);
            });
        }

        /// <summary>Forwards clicks on the picture without the scroll view swallowing drags.</summary>
        class MapClick : MonoBehaviour, IPointerClickHandler
        {
            public MapScreen Owner;
            public void OnPointerClick(PointerEventData e) => Owner?.Clicked(e);
        }
    }
}
