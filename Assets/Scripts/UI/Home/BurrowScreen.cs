using System;
using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Your home under the ground, reached by sliding down from the den. A cosy earthen room you
    /// furnish yourself: what you buy at the Furniture Shop waits in the storage drawer along the
    /// bottom — tap a piece, or drag it up, to stand it in the room. Drag pieces about to
    /// arrange them; the further back they stand the smaller they look, and they are drawn
    /// behind whatever stands in front. Tap a piece to flip it round or put it away again.
    /// Everything is remembered (HomeLog).
    /// </summary>
    public class BurrowScreen : UiScreen
    {
        public static BurrowScreen Instance { get; private set; }

        protected override string Title => "Your Burrow";
        protected override Sprite TitleIcon => IconPainter.House;
        protected override bool FullBleed => true;

        /// <summary>Where the floor meets the wall in the painting, from its bottom.</summary>
        const float PaintedFloorLine = 0.55f;

        RectTransform _body, _room, _drawer, _drawerContent, _toolbar, _ghost;
        ScrollRect _drawerScroll;
        Rect _floor;          // where feet may go, in room coordinates
        float _pieceScale;    // canvas units per "width 1" of furniture
        bool _wide;
        readonly Dictionary<HomeLog.Placed, RectTransform> _views = new();
        HomeLog.Placed _selected;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Show()
        {
            if (Instance != null) Instance.Open();
        }

        protected override void Build(RectTransform body) => _body = body;

        protected override void OnOpen()
        {
            _selected = null;
            Layout();
        }

        protected override void OnClose()
        {
            if (_ghost != null) Destroy(_ghost.gameObject);
            _ghost = null;
        }

        protected override void Update()
        {
            base.Update();
            if (IsOpen && _body != null && IsWide() != _wide) Layout();
        }

        bool IsWide() => _body.rect.width > _body.rect.height * 1.1f;

        // ---------------------------------------------------------------- the room

        void Layout()
        {
            ClearChildren(_body);
            _views.Clear();
            _toolbar = null;
            _wide = IsWide();
            float w = _body.rect.width, h = _body.rect.height;
            float drawerH = _wide ? 210f : 250f;
            float floorLine = _wide ? h * 0.56f : h * 0.5f;

            Backdrop(floorLine);

            // The room: the whole screen, with the floor between the drawer and the back wall.
            _room = UiKit.Rect("Room", _body);
            UiKit.Stretch(_room);
            var catcher = UiKit.Image(_room, "Floor", null, new Color(0f, 0f, 0f, 0f), true);
            UiKit.Stretch(catcher.rectTransform);
            var deselect = catcher.gameObject.AddComponent<Button>();
            deselect.transition = Selectable.Transition.None;
            deselect.onClick.AddListener(() => Select(null));
            _floor = new Rect(60f, drawerH + 50f, w - 120f, Mathf.Max(100f, floorLine - 20f - (drawerH + 50f)));
            _pieceScale = Mathf.Min(w, h * 0.62f);

            You();
            foreach (var p in HomeLog.Room) MakeView(p);
            Sort();

            TopBar();
            Drawer(drawerH);
            RefreshDrawer();
        }

        void Backdrop(float floorLine)
        {
            var wash = UiKit.Image(_body, "Earth", null, UiKit.Hex("7A4A2A"), true);
            UiKit.Stretch(wash.rectTransform, -400f, -400f, -400f, -400f);
            var art = Resources.Load<Sprite>("Home/Burrow_Cave");
            if (art == null) return;
            float w = _body.rect.width + 120f, h = _body.rect.height + 120f, y = floorLine + 60f;
            float aspect = art.rect.width / art.rect.height;
            float tall = Mathf.Max(h, w / aspect, y / PaintedFloorLine, (h - y) / (1f - PaintedFloorLine));
            var area = UiKit.Rect("Cave", _body);
            UiKit.Stretch(area, -60f, -60f, -60f, -60f);
            area.gameObject.AddComponent<RectMask2D>();
            var pic = UiKit.Image(area, "Painting", art, Color.white);
            pic.rectTransform.anchorMin = pic.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            pic.rectTransform.pivot = new Vector2(0.5f, PaintedFloorLine);
            pic.rectTransform.sizeDelta = new Vector2(tall * aspect, tall);
            pic.rectTransform.anchoredPosition = new Vector2(0f, y);
        }

        void TopBar()
        {
            var chip = UiKit.Image(_body, "TitleChip", UiKit.Rounded(40), UiKit.HudGlass).rectTransform;
            chip.anchorMin = chip.anchorMax = chip.pivot = new Vector2(0f, 1f);
            chip.anchoredPosition = new Vector2(28f, -28f);
            chip.sizeDelta = new Vector2(360f, 88f);
            var icon = UiKit.Image(chip, "Icon", IconPainter.House, UiKit.Hex("FFC98A"));
            icon.preserveAspect = true;
            UiKit.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(54f, 54f));
            var t = UiKit.Text(chip, "Title", Title, 42f, Color.white, true, TextAlignmentOptions.MidlineLeft);
            UiKit.Stretch(t.rectTransform, 88f, 0f, 16f, 0f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.enableAutoSizing = true;
            t.fontSizeMin = 26f;
            t.fontSizeMax = 42f;

            var up = UiKit.Button(_body, "Outside", "Go outside", UiKit.ButtonStyle.Secondary, Close, 30f, IconPainter.Arrow);
            var ur = (RectTransform)up.transform.parent;
            ur.anchorMin = ur.anchorMax = ur.pivot = new Vector2(1f, 1f);
            ur.anchoredPosition = new Vector2(-28f, -30f);
            ur.sizeDelta = new Vector2(280f, 88f);

            if (HomeLog.Room.Count == 0 && HomeLog.Storage().Count == 0)
            {
                var note = UiKit.Image(_body, "Empty", UiKit.Rounded(36), UiKit.HudGlass).rectTransform;
                note.anchorMin = note.anchorMax = note.pivot = new Vector2(0.5f, 1f);
                note.anchoredPosition = new Vector2(0f, -150f);
                note.sizeDelta = new Vector2(Mathf.Min(_body.rect.width - 80f, 820f), 140f);
                var nt = UiKit.Text(note, "Text", "Your burrow is bare! Buy something cosy at the Furniture Shop on your island.", 30f, Color.white, true, TextAlignmentOptions.Center);
                UiKit.Stretch(nt.rectTransform, 24f, 8f, 24f, 8f);
            }
        }

        /// <summary>Your capybara, at home, standing by the steps down from the door.</summary>
        void You()
        {
            float hgt = _pieceScale * 0.3f;
            var you = DressedCapy(_room, hgt);
            you.name = "You";
            you.anchorMin = you.anchorMax = Vector2.zero;
            you.anchoredPosition = FloorPoint(0.16f, 0.62f);
            float s = DepthScale(0.62f);
            you.localScale = new Vector3(-s, s, 1f);   // facing into the room
            you.DOAnchorPosY(you.anchoredPosition.y + 6f, 1.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
        }

        /// <summary>The player's capybara as it is dressed right now, standing on its feet (pivot at the bottom).</summary>
        public static RectTransform DressedCapy(Transform parent, float height)
        {
            var root = UiKit.Rect("Capy", parent);
            root.pivot = new Vector2(0.5f, 0f);
            var body = Wardrobe.Ready ? Wardrobe.BaseSprite : ItemCatalog.Instance != null ? ItemCatalog.Instance.Icon("Capybara") : null;
            float aspect = body != null ? body.rect.width / body.rect.height : 1f;
            root.sizeDelta = new Vector2(height * aspect, height);
            var img = UiKit.Image(root, "Body", body, Color.white);
            img.preserveAspect = true;
            UiKit.Stretch(img.rectTransform);
            if (!Wardrobe.Ready) return root;
            foreach (WearSlot slot in Enum.GetValues(typeof(WearSlot)))
            {
                var item = Wardrobe.Worn(slot);
                var art = Wardrobe.Art(item);
                if (art == null) continue;
                var layer = UiKit.Image(img.transform, slot.ToString(), art, Color.white);
                layer.preserveAspect = true;
                var r = layer.rectTransform;
                r.anchorMin = r.anchorMax = item.Anchor;
                float pw = item.Width * root.sizeDelta.x;
                r.sizeDelta = new Vector2(pw, pw * art.rect.height / art.rect.width);
                r.localRotation = Quaternion.Euler(0f, 0f, item.Rotation);
                if (item.Behind) r.SetAsFirstSibling();
            }
            return root;
        }

        // ---------------------------------------------------------------- furniture in the room

        /// <summary>Room coordinates of a spot on the floor: x 0 … 1 across, y 0 (front) … 1 (back wall).</summary>
        Vector2 FloorPoint(float x, float y) => new(_floor.xMin + x * _floor.width, _floor.yMin + y * _floor.height);

        /// <summary>Further back, a little smaller.</summary>
        static float DepthScale(float y) => Mathf.Lerp(1f, 0.72f, y);

        void MakeView(HomeLog.Placed p)
        {
            var def = Furniture.Get(p.id);
            if (def == null) return;
            var art = def.Art;
            var r = UiKit.Rect(def.Id, _room);
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.pivot = new Vector2(0.5f, def.Flat ? 0.5f : 0.06f);
            float wid = def.Width * _pieceScale;
            float aspect = art != null ? art.rect.height / art.rect.width : 1f;
            r.sizeDelta = new Vector2(wid, wid * aspect);
            var img = UiKit.Image(r, "Art", art, Color.white, true);
            img.preserveAspect = true;
            UiKit.Stretch(img.rectTransform);
            r.gameObject.AddComponent<PieceView>().Init(this, p);
            _views[p] = r;
            Place(p);
        }

        void Place(HomeLog.Placed p)
        {
            if (!_views.TryGetValue(p, out var r)) return;
            r.anchoredPosition = FloorPoint(p.x, p.y);
            float s = DepthScale(p.y);
            r.localScale = new Vector3(p.flip ? -s : s, s, 1f);
        }

        /// <summary>Back to front: rugs first, then everything by how far back it stands.</summary>
        void Sort()
        {
            var order = new List<(HomeLog.Placed p, RectTransform r)>();
            foreach (var kv in _views) order.Add((kv.Key, kv.Value));
            order.Sort((a, b) =>
            {
                bool fa = Furniture.Get(a.p.id)?.Flat == true, fb = Furniture.Get(b.p.id)?.Flat == true;
                if (fa != fb) return fa ? -1 : 1;
                return b.p.y.CompareTo(a.p.y);
            });
            int first = 1;   // after the floor catcher
            var you = _room.Find("You");
            foreach (var (_, r) in order) r.SetSiblingIndex(first++);
            // You stand among them by depth too.
            if (you != null)
            {
                int at = 1;
                foreach (var (p, _) in order) if (Furniture.Get(p.id)?.Flat == true || p.y > 0.62f) at++;
                you.SetSiblingIndex(Mathf.Min(at, _room.childCount - 1));
            }
            if (_toolbar != null) _toolbar.SetAsLastSibling();
        }

        void Select(HomeLog.Placed p)
        {
            _selected = p;
            if (_toolbar != null) Destroy(_toolbar.gameObject);
            _toolbar = null;
            foreach (var kv in _views)
            {
                var img = kv.Value.GetComponentInChildren<Image>();
                if (img != null) img.color = kv.Key == p ? new Color(1f, 0.95f, 0.8f) : Color.white;
            }
            if (p == null || !_views.TryGetValue(p, out var view)) return;

            // A little bar over the piece: flip it round, or put it back in storage.
            _toolbar = UiKit.Image(_room, "Tools", UiKit.Rounded(30), UiKit.HudGlass, true).rectTransform;
            _toolbar.anchorMin = _toolbar.anchorMax = Vector2.zero;
            _toolbar.pivot = new Vector2(0.5f, 0f);
            _toolbar.sizeDelta = new Vector2(420f, 96f);
            var top = view.anchoredPosition + new Vector2(0f, view.sizeDelta.y * view.localScale.y * (1f - view.pivot.y) + 16f);
            top.x = Mathf.Clamp(top.x, 230f, _body.rect.width - 230f);
            top.y = Mathf.Min(top.y, _body.rect.height - 260f);
            _toolbar.anchoredPosition = top;
            var h = _toolbar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(10, 10, 10, 10);
            h.spacing = 10f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;
            UiKit.Button(_toolbar, "Flip", "Flip", UiKit.ButtonStyle.Secondary, () =>
            {
                p.flip = !p.flip;
                Place(p);
                HomeLog.Save();
            }, 28f);
            UiKit.Button(_toolbar, "PutAway", "Put away", UiKit.ButtonStyle.Spring, () =>
            {
                if (_views.TryGetValue(p, out var v)) Destroy(v.gameObject);
                _views.Remove(p);
                HomeLog.PutAway(p);
                Select(null);
                RefreshDrawer();
            }, 28f);
            UiKit.PopIn(_toolbar, 0.85f);
        }

        /// <summary>A pointer position as a floor spot (clamped onto the floor).</summary>
        Vector2 ToFloor(Vector2 screen, Camera cam)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_room, screen, cam, out var local);
            // Local is about the room's centre; floor coordinates are from its bottom-left.
            local += _room.rect.size * 0.5f;
            return new Vector2(Mathf.Clamp01((local.x - _floor.xMin) / _floor.width), Mathf.Clamp01((local.y - _floor.yMin) / _floor.height));
        }

        bool OverRoom(Vector2 screen, Camera cam)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_room, screen, cam, out var local);
            local += _room.rect.size * 0.5f;
            return local.y > _drawer.rect.height + 20f;
        }

        /// <summary>A placed piece: drag to move it, tap to pick it (flip / put away).</summary>
        class PieceView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
        {
            BurrowScreen _screen;
            HomeLog.Placed _p;
            bool _dragged;
            Vector2 _grab;

            public void Init(BurrowScreen s, HomeLog.Placed p) { _screen = s; _p = p; }

            public void OnPointerClick(PointerEventData e)
            {
                if (!_dragged) _screen.Select(_screen._selected == _p ? null : _p);
                _dragged = false;
            }

            public void OnBeginDrag(PointerEventData e)
            {
                _dragged = true;
                _screen.Select(null);
                var at = _screen.ToFloor(e.position, e.pressEventCamera);
                _grab = new Vector2(_p.x, _p.y) - at;
                transform.DOScale(transform.localScale * 1.06f, 0.1f).SetUpdate(true);
            }

            public void OnDrag(PointerEventData e)
            {
                var at = _screen.ToFloor(e.position, e.pressEventCamera) + _grab;
                _p.x = Mathf.Clamp01(at.x);
                _p.y = Mathf.Clamp01(at.y);
                _screen.Place(_p);
                _screen.Sort();
            }

            public void OnEndDrag(PointerEventData e)
            {
                _screen.Place(_p);
                _screen.Sort();
                HomeLog.Save();
                Sfx.Play(SfxKind.Plop, 0.4f);
            }
        }

        // ---------------------------------------------------------------- the storage drawer

        void Drawer(float height)
        {
            var board = UiKit.Card(_body, "Storage", UiKit.Hex("8A5A32"), 30, true, true).rectTransform;
            board.anchorMin = Vector2.zero;
            board.anchorMax = new Vector2(1f, 0f);
            board.pivot = new Vector2(0.5f, 0f);
            board.sizeDelta = new Vector2(-48f, height);
            board.anchoredPosition = new Vector2(0f, 24f);
            _drawer = board;
            var label = UiKit.Text(board, "Label", "Storage", 26f, new Color(1f, 1f, 1f, 0.85f), true, TextAlignmentOptions.TopLeft);
            label.rectTransform.anchorMin = new Vector2(0f, 1f);
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.pivot = new Vector2(0.5f, 1f);
            label.rectTransform.sizeDelta = new Vector2(-40f, 40f);
            label.rectTransform.anchoredPosition = new Vector2(0f, -8f);

            var view = UiKit.Rect("View", board);
            UiKit.Stretch(view, 14f, 14f, 14f, 50f);
            view.gameObject.AddComponent<RectMask2D>();
            _drawerContent = UiKit.Rect("Items", view);
            _drawerContent.anchorMin = new Vector2(0f, 0f);
            _drawerContent.anchorMax = new Vector2(0f, 1f);
            _drawerContent.pivot = new Vector2(0f, 0.5f);
            _drawerContent.offsetMin = _drawerContent.offsetMax = Vector2.zero;
            var hl = _drawerContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12f;
            hl.padding = new RectOffset(4, 4, 0, 0);
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = hl.childControlHeight = false;
            hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            _drawerContent.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            _drawerScroll = view.gameObject.AddComponent<ScrollRect>();
            _drawerScroll.content = _drawerContent;
            _drawerScroll.viewport = view;
            _drawerScroll.horizontal = true;
            _drawerScroll.vertical = false;
            _drawerScroll.movementType = ScrollRect.MovementType.Clamped;
        }

        void RefreshDrawer()
        {
            ClearChildren(_drawerContent);
            var none = _drawer.Find("None");
            if (none != null) Destroy(none.gameObject);
            var items = HomeLog.Storage();
            if (items.Count == 0)
            {
                var t = UiKit.Text(_drawer, "None", "Nothing in storage. Pieces you buy wait here.", 26f, new Color(1f, 1f, 1f, 0.8f), false, TextAlignmentOptions.Center);
                UiKit.Stretch(t.rectTransform, 20f, 10f, 20f, 50f);
            }
            float tile = _drawer.sizeDelta.y - 70f;
            foreach (var (def, count) in items)
            {
                var cell = UiKit.Image(_drawerContent, def.Id, UiKit.Rounded(24), UiKit.Surface, true).rectTransform;
                cell.sizeDelta = new Vector2(tile, tile);
                var pic = UiKit.Image(cell, "Pic", def.Art, Color.white);
                pic.preserveAspect = true;
                UiKit.Stretch(pic.rectTransform, 12f, 30f, 12f, 8f);
                var n = UiKit.Text(cell, "Count", "×" + count, 24f, UiKit.InkSoft, true, TextAlignmentOptions.Bottom);
                UiKit.Stretch(n.rectTransform, 4f, 4f, 4f, 4f);
                cell.gameObject.AddComponent<StoredPiece>().Init(this, def);
            }
        }

        /// <summary>Takes a piece out of storage and stands it on the floor.</summary>
        void PlaceNew(FurnitureDef def, Vector2 at)
        {
            var p = HomeLog.PlaceFromStorage(def.Id, at.x, at.y);
            if (p == null) return;
            MakeView(p);
            Sort();
            RefreshDrawer();
            Sfx.Play(SfxKind.Plop, 0.6f);
            if (_views.TryGetValue(p, out var v))
            {
                var s = v.localScale;
                v.localScale = s * 0.6f;
                v.DOScale(s, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
            }
            var empty = _body.Find("Empty");
            if (empty != null) Destroy(empty.gameObject);
        }

        /// <summary>A piece in the drawer: tap to stand it in the room, or drag it up to where you want it.</summary>
        class StoredPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
        {
            BurrowScreen _screen;
            FurnitureDef _def;
            bool _dragged, _scrolling;

            public void Init(BurrowScreen s, FurnitureDef def) { _screen = s; _def = def; }

            public void OnPointerClick(PointerEventData e)
            {
                if (!_dragged) _screen.PlaceNew(_def, new Vector2(UnityEngine.Random.Range(0.35f, 0.65f), UnityEngine.Random.Range(0.3f, 0.5f)));
                _dragged = false;
            }

            public void OnBeginDrag(PointerEventData e)
            {
                _dragged = true;
                _scrolling = Mathf.Abs(e.delta.x) > Mathf.Abs(e.delta.y);
                if (_scrolling) { _screen._drawerScroll.OnBeginDrag(e); return; }
                var ghost = UiKit.Image(_screen._body, "Ghost", _def.Art, new Color(1f, 1f, 1f, 0.85f));
                ghost.preserveAspect = true;
                ghost.rectTransform.sizeDelta = new Vector2(180f, 180f);
                ghost.rectTransform.pivot = new Vector2(0.5f, 0.1f);
                _screen._ghost = ghost.rectTransform;
                OnDrag(e);
            }

            public void OnDrag(PointerEventData e)
            {
                if (_scrolling) { _screen._drawerScroll.OnDrag(e); return; }
                if (_screen._ghost == null) return;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_screen._body, e.position, e.pressEventCamera, out var local))
                {
                    _screen._ghost.anchorMin = _screen._ghost.anchorMax = new Vector2(0.5f, 0.5f);
                    _screen._ghost.anchoredPosition = local;
                }
            }

            public void OnEndDrag(PointerEventData e)
            {
                if (_scrolling) { _screen._drawerScroll.OnEndDrag(e); _scrolling = false; return; }
                if (_screen._ghost != null) Destroy(_screen._ghost.gameObject);
                _screen._ghost = null;
                if (_screen.OverRoom(e.position, e.pressEventCamera))
                    _screen.PlaceNew(_def, _screen.ToFloor(e.position, e.pressEventCamera));
            }
        }
    }
}
