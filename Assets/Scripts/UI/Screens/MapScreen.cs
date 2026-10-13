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
    /// with markers over it for you, bubbles, pests, anyone with news and the tower. Islands you
    /// cannot reach yet wear a padlock. Drag to look around; tap a spot to send the camera there.
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
            UiKit.Stretch(frame.rectTransform, 0f, 84f, 0f, 0f);

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

            // Legend.
            var legend = UiKit.Rect("Legend", body);
            legend.anchorMin = Vector2.zero;
            legend.anchorMax = new Vector2(1f, 0f);
            legend.pivot = new Vector2(0.5f, 0f);
            legend.sizeDelta = new Vector2(0f, 64f);
            var h = legend.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 26f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            LegendItem(legend, UiKit.Primary, "You");
            LegendItem(legend, UiKit.Spring, "Bubble");
            LegendItem(legend, UiKit.Danger, "Pest");
            LegendItem(legend, UiKit.Sun, "News");
            var hint = UiKit.Text(legend, "Hint", "Tap to look · drag yourself to travel", 24f, UiKit.InkSoft);
            hint.textWrappingMode = TextWrappingModes.NoWrap;
            hint.overflowMode = TextOverflowModes.Overflow;
        }

        static void LegendItem(RectTransform parent, Color colour, string label)
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
        }

        protected override void OnOpen()
        {
            EnsureCamera();
            if (_camera != null) _camera.enabled = true;
            Layout();
            RebuildLocks();
            CentreOnKing();
            Game.NewStuff.Clear("map/");   // the new island is on show
            // The first time you look at the map, and only then: how to travel on it.
            if (!Game.Guidance.Off && !HudTour.Seen("map"))
            {
                HudTour.MarkSeen("map");
                CardPopup.Show("Map", "See all the islands. Drag yourself on the map to travel.", IconPainter.Map);
            }
        }

        protected override void OnClose()
        {
            if (_camera != null) _camera.enabled = false;
        }

        protected override void Fit()
        {
            base.Fit();
            if (IsOpen) Layout();
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
                var widget = Find(m);
                if (widget == null)
                {
                    widget = MakeMarker(m);
                    _widgets.Add((m, widget));
                }
                bool show = m.MarkerActive || m.MarkerKind == MarkerKind.Tower;
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
                if (!_draggingKing) _king.anchoredPosition = ToMap(king.transform.position);
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
                _ => (UiKit.Spring, 52f),
            };
            return Pin(m.MarkerIcon, colour, size, null);
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
            _draggingKing = true;
            _dragScreen = e.position;
            _dragCamera = e.pressEventCamera;
            _king.DOKill();
            _king.DOScale(1.2f, 0.15f).SetUpdate(true);
        }

        void TravelDrag(PointerEventData e)
        {
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
            _king.DOKill();
            _king.DOScale(1f, 0.15f).SetUpdate(true);
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king == null || !LocalPoint(e, out var local)) { _draggingKing = false; return; }
            var world = ToWorld(local);
            string why = null;
            var land = LandMap.Instance;
            if (!BunniSpace.PointOnLand(world)) why = "You can't stand in the sea! Drop yourself on land.";
            else if (land != null && !land.Reachable(king.transform.position, world)) why = "That island is still closed. Open its bridge first!";
            if (why != null)
            {
                _draggingKing = false;   // springs back to where you really are
                Toast(why);
                Sfx.Play(SfxKind.Tap, 0.4f);
                return;
            }
            _draggingKing = false;
            Close();
            king.TravelTo(world);
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

        internal void Clicked(PointerEventData e)
        {
            if (e.dragging) return;
            var content = _scroll.content;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(content, e.position, e.pressEventCamera, out var local)) return;
            var size = content.rect.size;
            var n = new Vector2(local.x / size.x + 0.5f, local.y / size.y + 0.5f);
            var world = new Vector2(Mathf.Lerp(_world.min.x, _world.max.x, n.x), Mathf.Lerp(_world.min.y, _world.max.y, n.y));
            Close();
            CameraRig.CameraFocus.Show(world, 3f);
        }

        /// <summary>Forwards clicks on the picture without the scroll view swallowing drags.</summary>
        class MapClick : MonoBehaviour, IPointerClickHandler
        {
            public MapScreen Owner;
            public void OnPointerClick(PointerEventData e) => Owner?.Clicked(e);
        }
    }
}
