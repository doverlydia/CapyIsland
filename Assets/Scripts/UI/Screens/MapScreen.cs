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
        float _nextRender;
        int _renderFrames;

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
            var hint = UiKit.Text(legend, "Hint", "Tap to look there", 24f, UiKit.InkSoft);
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
            _renderFrames = 2;
            if (_camera != null) _camera.enabled = true;
            Layout();
            RebuildLocks();
            CentreOnKing();
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
            int w = 2048, h = Mathf.Clamp(Mathf.RoundToInt(w / aspect), 256, 2048);
            _texture = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32) { name = "IslandMap", antiAliasing = 2 };
            _texture.Create();
            _picture.texture = _texture;

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

        protected override void Update()
        {
            base.Update();
            if (!IsOpen) return;

            // The land changes slowly: render a frame or two, then once a second.
            if (_camera != null)
            {
                if (_renderFrames > 0) { _renderFrames--; if (_renderFrames == 0) _camera.enabled = false; }
                else if (Time.unscaledTime >= _nextRender)
                {
                    _nextRender = Time.unscaledTime + 1f;
                    _camera.enabled = true;
                    _renderFrames = 1;
                }
            }
            UpdateMarkers();
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
                if (_king == null) _king = Pin(MapMarkers.SpriteOf(king.gameObject), UiKit.Primary, 88f, "You");
                _king.anchoredPosition = ToMap(king.transform.position);
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
