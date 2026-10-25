using System;
using System.Collections;
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
    /// Your home under the ground, reached by sliding down from the den: a row of cosy earthen
    /// rooms, each one wider than your screen. You walk about in it and the view follows you, so
    /// it's the same room in portrait and in landscape (landscape just shows more of it at once).
    ///
    /// <b>Living in it.</b> Tap the floor to walk; tap a piece to use it: sit in the armchair, nap
    /// in the bed, soak in the hot tub with a towel on your head, play the piano, switch the lamp
    /// off, open your photo album (FurnitureUse says what each piece is for). Tap the door up the
    /// steps to go outside. At the far end of a room a tunnel leads on to the next: dig out a new
    /// room for gold (HomeLog.RoomPrices), or walk through to one you have.
    ///
    /// <b>Decorate</b> (the button at the top) is for arranging: what you buy at the Furniture Shop
    /// waits in the storage drawer along the bottom — tap a piece, or drag it up, to stand it in
    /// the room. Drag pieces about; the further back they stand the smaller they look. Tap one to
    /// flip it round or put it away. Drag the floor to look along the room.
    ///
    /// <b>A friend's burrow</b> (their den, on a visit): theirs to look round, sit in, soak in —
    /// but not to rearrange. Real friends' burrows come with their island (RealFriends), the
    /// islanders' are furnished to suit them.
    ///
    /// The first time you come down, the Furniture Shop is waiting in a bubble: pop it and it
    /// joins your trail, to be put up outside on the island (it's far too big for in here).
    /// </summary>
    public class BurrowScreen : UiScreen
    {
        public static BurrowScreen Instance { get; private set; }

        protected override string Title => _friend != null ? string.Format("{0}'s Burrow", _friend.name) : "Your Burrow";
        protected override Sprite TitleIcon => IconPainter.House;
        protected override bool FullBleed => true;

        // ---------------------------------------------------------------- the room, in room units (canvas units)

        /// <summary>How big a room is: the painting, drawn at a fixed size whatever the screen.</summary>
        const float RoomW = 3300f, RoomH = 2200f;

        /// <summary>Where feet may go: the floor in the painting, from just above its bottom edge to the foot of the back wall.</summary>
        static readonly Rect Floor = new(170f, 190f, 2960f, 990f);

        /// <summary>Canvas units per "width 1" of furniture: a phone's width, as pieces were first sized.</summary>
        const float PieceUnit = 966f;

        const float DrawerH = 230f;

        RectTransform _body, _world, _pieces, _fx, _chrome, _drawer, _drawerContent, _toolbar, _ghost, _fade, _decorate;
        ScrollRect _drawerScroll;
        readonly Dictionary<HomeLog.Placed, RectTransform> _views = new();
        HomeLog.Placed _selected;
        bool _editing;
        int _room;
        Vector2 _cam, _lastSize;
        float _freeLookUntil;

        // A friend's burrow: theirs to look at, not to change.
        FriendProfile _friend;
        List<HomeLog.Placed> _friendPieces = new();
        int _friendRooms = 1;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Your own burrow.</summary>
        public static void Show()
        {
            if (Instance == null) return;
            Instance._friend = null;
            Instance.Open();
        }

        /// <summary>A friend's burrow, to look round (their den, on a visit).</summary>
        public static async void ShowFriend(FriendProfile who)
        {
            if (Instance == null || who == null) return;
            var (pieces, rooms) = await FriendHomes.Of(who);
            if (Instance == null) return;
            if (Instance._friend == null) { _homeRoom = Instance._room; _homeAt = _youAt; }
            Instance._friend = who;
            Instance._friendPieces = pieces;
            Instance._friendRooms = rooms;
            Instance.Open();
        }

        protected override void Build(RectTransform body) => _body = body;

        protected override void OnOpen()
        {
            _selected = null;
            _editing = false;
            if (_friend == null) HomeLog.Settle();
            else { _room = 0; _youAt = new Vector2(0.12f, 0.75f); }
            if (_room >= Rooms) _room = 0;
            Layout();
            SnapCamera();
        }

        protected override void OnClose()
        {
            if (_ghost != null) Destroy(_ghost.gameObject);
            _ghost = null;
            StandUp(instant: true);
            if (_friend != null) { _room = _homeRoom; _youAt = _homeAt; }   // back where you were in your own
            _friend = null;
            StopAllCoroutines();
        }

        // Where you were in your own burrow, kept while you look round a friend's.
        static int _homeRoom;
        static Vector2 _homeAt;

        int Rooms => _friend != null ? _friendRooms : HomeLog.Rooms;
        bool Mine => _friend == null;
        IReadOnlyList<HomeLog.Placed> Pieces => Mine ? HomeLog.InRoom(_room) : _friendPieces.FindAll(p => p.room == _room);

        protected override void Update()
        {
            base.Update();
            if (!IsOpen || _body == null) return;
            Walk();
            Follow();
            EdgePan();
            Radios();
            if (_lastSize != _body.rect.size) { _lastSize = _body.rect.size; ClampNow(); }   // turned the phone: the same room, a different window
        }

        // ---------------------------------------------------------------- building it

        void Layout()
        {
            ClearChildren(_body);
            _views.Clear();
            _toolbar = null;
            _sitting = null;
            _lastSize = _body.rect.size;

            var wash = UiKit.Image(_body, "Earth", null, UiKit.Hex("B8692F"), true);
            UiKit.Stretch(wash.rectTransform, -400f, -400f, -400f, -400f);

            _world = UiKit.Rect("Room", _body);
            _world.anchorMin = _world.anchorMax = _world.pivot = Vector2.zero;
            _world.sizeDelta = new Vector2(RoomW, RoomH);
            Backdrop();

            var catcher = UiKit.Image(_world, "Floor", null, new Color(0f, 0f, 0f, 0f), true);
            UiKit.Stretch(catcher.rectTransform, -600f, -600f, -600f, -600f);
            catcher.gameObject.AddComponent<FloorTap>().Init(this);

            Doors();

            _pieces = UiKit.Rect("Pieces", _world);
            UiKit.Stretch(_pieces);
            _fx = UiKit.Rect("Fx", _world);
            UiKit.Stretch(_fx);

            You();
            if (!Mine) Host();
            foreach (var p in Pieces) MakeView(p);
            if (Mine && _room == 0) { ShopBubble(); AlbumBubble(); }
            Sort();

            _fade = UiKit.Image(_body, "Fade", null, new Color(0.08f, 0.05f, 0.03f, 0f)).rectTransform;
            UiKit.Stretch(_fade, -400f, -400f, -400f, -400f);
            _fade.GetComponent<Image>().raycastTarget = false;

            _chrome = UiKit.Rect("Chrome", _body);
            UiKit.Stretch(_chrome);
            TopBar();
            if (Mine)
            {
                Drawer();
                RefreshDrawer();
            }
            SetEditing(_editing);
        }

        /// <summary>
        /// The painting, at room size, with a little more earth beyond each edge (bleed) in case a
        /// tall screen shows more than the room. The room's own picture once it's drawn; the old
        /// one-screen cave until then.
        /// </summary>
        void Backdrop()
        {
            var art = Resources.Load<Sprite>("Home/Burrow_Room") ?? Resources.Load<Sprite>("Home/Burrow_Cave");
            // Above the room, more wall; below it, more floor.
            var wall = UiKit.Image(_world, "WallBleed", null, UiKit.Hex("B35F2C"));
            UiKit.Place(wall.rectTransform, new Vector2(0f, 0f), Vector2.zero, new Vector2(RoomW + 1600f, 1400f));
            wall.rectTransform.pivot = new Vector2(0.5f, 0f);
            wall.rectTransform.anchoredPosition = new Vector2(RoomW * 0.5f, RoomH - 40f);
            var ground = UiKit.Image(_world, "FloorBleed", null, UiKit.Hex("CC7A3D"));
            ground.rectTransform.anchorMin = ground.rectTransform.anchorMax = Vector2.zero;
            ground.rectTransform.pivot = new Vector2(0.5f, 1f);
            ground.rectTransform.sizeDelta = new Vector2(RoomW + 1600f, 1400f);
            ground.rectTransform.anchoredPosition = new Vector2(RoomW * 0.5f, 40f);
            if (art == null) return;
            var pic = UiKit.Image(_world, "Painting", art, Color.white);
            pic.rectTransform.anchorMin = pic.rectTransform.anchorMax = pic.rectTransform.pivot = Vector2.zero;
            pic.rectTransform.anchoredPosition = Vector2.zero;
            pic.rectTransform.sizeDelta = new Vector2(RoomW, RoomH);
        }

        // ---------------------------------------------------------------- the door and the tunnels

        static readonly Vector2 DoorAt = new(520f, Floor.yMax + 50f);   // on the wall line (58% up the painting), its steps on the floor
        static readonly Vector2 ArchRight = new(RoomW - 360f, Floor.yMax + 70f), ArchLeft = new(360f, Floor.yMax + 70f);

        void Doors()
        {
            // The way out: only in the first room, up the steps (tap the door; "Go outside" up top does the same).
            if (_room == 0) WallPiece("Door", "Home/Burrow_Door", DoorAt, 400f, Exit);
            // On to the next room, dug out or not yet.
            bool more = _room + 1 < Rooms;
            bool canDig = Mine && _room + 1 == Rooms && HomeLog.NextRoomPrice > 0;
            if (more || canDig)
            {
                var arch = WallPiece("ArchNext", "Home/Burrow_Arch", ArchRight, 420f, NextRoom);
                if (arch != null)
                {
                    if (more) Pill(arch, string.Format("Room {0}", _room + 2), IconPainter.Chevron, UiKit.HudGlass, NextRoom);
                    else Pill(arch, string.Format("New room · {0}", HomeLog.NextRoomPrice.ToString("N0")), IconPainter.Lock, UiKit.Hex("8A5A32"), NextRoom);
                }
            }
            if (_room > 0)
            {
                var back = WallPiece("ArchBack", "Home/Burrow_Arch", ArchLeft, 420f, PreviousRoom);
                if (back != null) Pill(back, string.Format("Room {0}", _room), IconPainter.Chevron, UiKit.HudGlass, PreviousRoom, flipIcon: true);
            }
        }

        /// <summary>Something on the back wall, standing on the floor line at <paramref name="foot"/>, tapped to <paramref name="tap"/>.</summary>
        RectTransform WallPiece(string name, string artPath, Vector2 foot, float width, Action tap)
        {
            var art = Resources.Load<Sprite>(artPath);
            var r = UiKit.Rect(name, _world);
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.pivot = new Vector2(0.5f, 0.06f);
            float h = art != null ? width * art.rect.height / art.rect.width : width * 1.1f;
            r.sizeDelta = new Vector2(width, h);
            r.anchoredPosition = foot;
            var img = UiKit.Image(r, "Art", art, art != null ? Color.white : new Color(0.25f, 0.15f, 0.08f, 0.9f), true);
            if (art == null) img.sprite = UiKit.Rounded(80);
            img.preserveAspect = art != null;
            UiKit.Stretch(img.rectTransform);
            var b = img.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { if (!_editing) tap(); });
            return r;
        }

        void Pill(RectTransform over, string text, Sprite icon, Color fill, Action tap, bool flipIcon = false)
        {
            var button = UiKit.Button(over, "Pill", text, UiKit.ButtonStyle.Secondary, () => { if (!_editing) tap(); }, 30f, icon);
            var r = (RectTransform)button.transform.parent;
            // At the foot of the tunnel, on the floor: clear of the buttons up top in landscape.
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, -10f);
            r.sizeDelta = new Vector2(330f, 84f);
            if (flipIcon)
            {
                var ic = button.transform.Find("Content/Icon");
                if (ic != null) ic.localScale = new Vector3(-1f, 1f, 1f);
            }
            r.DOAnchorPosY(-22f, 1.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(r.gameObject);
        }

        /// <summary>Up the steps and out of the door.</summary>
        void Exit() => WalkThen(ToFloorPoint(DoorAt + new Vector2(0f, -60f)), Close);

        void NextRoom()
        {
            if (_room + 1 < Rooms) { WalkThen(ToFloorPoint(ArchRight + new Vector2(0f, -60f)), () => GoTo(_room + 1, fromLeft: true)); return; }
            if (!Mine) return;
            int price = HomeLog.NextRoomPrice;
            if (price <= 0) return;
            PromptScreen.Ask("A new room", null,
                Loc.F("Dig out room {0}? It costs {1} gold, and then it's yours to fill.", HomeLog.Rooms + 1, price.ToString("N0")),
                "Dig it out", "Not now", () =>
                {
                    if (!HomeLog.DigNextRoom())
                    {
                        Hints.Answer(Loc.F("You need {0} gold to dig out a new room.", price.ToString("N0")));
                        return;
                    }
                    Sfx.Play(SfxKind.Chest, 0.8f);
                    Hints.Answer(Loc.F("Room {0} is dug out! Decorate it however you like.", HomeLog.Rooms));
                    GoTo(_room + 1, fromLeft: true);
                });
        }

        void PreviousRoom() => WalkThen(ToFloorPoint(ArchLeft + new Vector2(0f, -60f)), () => GoTo(_room - 1, fromLeft: false));

        /// <summary>Through the tunnel: a moment's dark, and the next room, you stepping in at its side.</summary>
        void GoTo(int room, bool fromLeft)
        {
            if (room < 0 || room >= Rooms) return;
            StandUp(instant: true);
            var fade = _fade.GetComponent<Image>();
            fade.raycastTarget = true;
            Sfx.Play(SfxKind.Slide, 0.4f);
            fade.DOFade(1f, 0.25f).SetUpdate(true).OnComplete(() =>
            {
                _room = room;
                _youAt = ToFloorPoint((fromLeft ? ArchLeft : ArchRight) + new Vector2(0f, -80f));
                _facingRight = fromLeft;
                Layout();
                SnapCamera();
                var f = _fade.GetComponent<Image>();
                f.color = new Color(f.color.r, f.color.g, f.color.b, 1f);
                f.DOFade(0f, 0.3f).SetUpdate(true).OnComplete(() => f.raycastTarget = false);
            });
        }

        // ---------------------------------------------------------------- the top bar

        void TopBar()
        {
            var chip = UiKit.Image(_chrome, "TitleChip", UiKit.Rounded(40), UiKit.HudGlass).rectTransform;
            chip.anchorMin = chip.anchorMax = chip.pivot = new Vector2(0f, 1f);
            chip.anchoredPosition = new Vector2(28f, -28f);
            chip.sizeDelta = new Vector2(400f, Rooms > 1 ? 112f : 88f);
            var icon = UiKit.Image(chip, "Icon", IconPainter.House, UiKit.Hex("FFC98A"));
            icon.preserveAspect = true;
            UiKit.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(54f, 54f));
            var t = UiKit.Text(chip, "Title", Title, 40f, Color.white, true, TextAlignmentOptions.MidlineLeft);
            UiKit.Stretch(t.rectTransform, 88f, Rooms > 1 ? 36f : 0f, 16f, 0f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.enableAutoSizing = true;
            t.fontSizeMin = 24f;
            t.fontSizeMax = 40f;
            if (Rooms > 1)
            {
                var sub = UiKit.Text(chip, "Room", string.Format("Room {0} of {1}", _room + 1, Rooms), 26f, new Color(1f, 1f, 1f, 0.75f), false, TextAlignmentOptions.BottomLeft);
                UiKit.Stretch(sub.rectTransform, 88f, 12f, 16f, 60f);
            }

            // Down the right: out, decorate, the camera.
            var column = UiKit.Rect("Buttons", _chrome);
            column.anchorMin = column.anchorMax = column.pivot = new Vector2(1f, 1f);
            column.anchoredPosition = new Vector2(-28f, -28f);
            column.sizeDelta = new Vector2(290f, 400f);
            var v = column.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 14f;
            v.childAlignment = TextAnchor.UpperRight;
            v.childControlWidth = v.childControlHeight = false;
            v.childForceExpandWidth = v.childForceExpandHeight = false;

            var up = UiKit.Button(column, "Outside", "Go outside", UiKit.ButtonStyle.Secondary, Close, 30f, IconPainter.Arrow);
            ((RectTransform)up.transform.parent).sizeDelta = new Vector2(280f, 88f);
            if (Mine)
            {
                var deco = UiKit.Button(column, "Decorate", "Decorate", UiKit.ButtonStyle.Primary, () => SetEditing(!_editing), 30f, IconPainter.Brush);
                _decorate = (RectTransform)deco.transform.parent;
                _decorate.sizeDelta = new Vector2(280f, 88f);
                NewDot.Live(deco.transform, () => !_editing && HomeLog.Storage().Count > 0);
            }
            var snap = UiKit.Button(column, "Camera", "Photo", UiKit.ButtonStyle.Spring, PhotoMode.Open, 30f, PhotoMode.Icon);
            ((RectTransform)snap.transform.parent).sizeDelta = new Vector2(280f, 88f);

            if (Mine && HomeLog.Room.Count == 0 && HomeLog.Storage().Count == 0 && !HomeLog.AlbumWaiting)
            {
                var note = UiKit.Image(_chrome, "Empty", UiKit.Rounded(36), UiKit.HudGlass).rectTransform;
                note.anchorMin = note.anchorMax = note.pivot = new Vector2(0.5f, 1f);
                note.anchoredPosition = new Vector2(0f, -170f);
                note.sizeDelta = new Vector2(Mathf.Min(_body.rect.width - 420f, 760f), 140f);
                string line = HomeLog.ShopGiven || StoreData.CountByName("Furniture Shop") > 0
                    ? "Your burrow is bare! Buy something cosy at the Furniture Shop on your island."
                    : "Your burrow is bare! Pop the bubble: there's a Furniture Shop inside.";
                var nt = UiKit.Text(note, "Text", line, 28f, Color.white, true, TextAlignmentOptions.Center);
                UiKit.Stretch(nt.rectTransform, 20f, 8f, 20f, 8f);
            }
        }

        /// <summary>Decorate mode on or off: the drawer and dragging, or living in it.</summary>
        void SetEditing(bool on)
        {
            if (!Mine) on = false;
            if (on) StandUp(instant: false);
            _editing = on;
            Select(null);
            if (_drawer != null) _drawer.gameObject.SetActive(on);
            if (_decorate != null)
            {
                var label = _decorate.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = on ? "Done" : "Decorate";
                var face = _decorate.Find("Face");
                if (face != null) face.GetComponent<Image>().color = on ? UiKit.Leaf : UiKit.Primary;
            }
            var hint = _chrome != null ? _chrome.Find("EditHint") : null;
            if (on && hint == null && _chrome != null)
            {
                var chip = UiKit.Image(_chrome, "EditHint", UiKit.Rounded(30), UiKit.HudGlass).rectTransform;
                chip.anchorMin = chip.anchorMax = chip.pivot = new Vector2(0.5f, 0f);
                chip.anchoredPosition = new Vector2(0f, DrawerH + 40f);
                chip.sizeDelta = new Vector2(Mathf.Min(_body.rect.width - 60f, 820f), 76f);
                var t = UiKit.Text(chip, "Text", "Drag pieces to move them. Tap one to flip it or put it away.", 26f, Color.white, false, TextAlignmentOptions.Center);
                UiKit.Stretch(t.rectTransform, 16f, 4f, 16f, 4f);
            }
            else if (!on && hint != null) Destroy(hint.gameObject);
        }

        /// <summary>The photo camera hides the buttons while it takes a picture (PhotoMode).</summary>
        public void HideChrome(bool hide)
        {
            if (_chrome == null) return;
            var group = _chrome.GetComponent<CanvasGroup>();
            if (group == null) group = _chrome.gameObject.AddComponent<CanvasGroup>();
            group.alpha = hide ? 0f : 1f;
            group.blocksRaycasts = !hide;
            if (_fx != null)
            {
                // The little "Go outside" and "Room 2" signs over the doors too.
                foreach (Transform t in _world) { var pill = t.Find("Pill"); if (pill != null) pill.gameObject.SetActive(!hide); }
            }
        }

        // ---------------------------------------------------------------- the view, following you

        /// <summary>The window onto the room: its bottom-left corner in room units, kept inside the room.</summary>
        Vector2 Clamp(Vector2 cam)
        {
            var view = _body.rect.size;
            float minY = _editing ? -DrawerH - 30f : 0f;
            cam.x = RoomW <= view.x ? (RoomW - view.x) * 0.5f : Mathf.Clamp(cam.x, 0f, RoomW - view.x);
            float maxY = RoomH - view.y;
            cam.y = maxY < minY ? (minY + maxY) * 0.5f : Mathf.Clamp(cam.y, minY, maxY);
            return cam;
        }

        Vector2 Wanted()
        {
            var view = _body.rect.size;
            var you = _you != null ? YouInRoom() : FloorPoint(0.5f, 0.5f);
            return Clamp(you - new Vector2(view.x * 0.5f, view.y * 0.36f));
        }

        void SnapCamera()
        {
            _cam = Wanted();
            _freeLookUntil = 0f;
            if (_world != null) _world.anchoredPosition = -_cam;
        }

        void ClampNow()
        {
            _cam = Clamp(_cam);
            if (_world != null) _world.anchoredPosition = -_cam;
        }

        void Follow()
        {
            if (_world == null) return;
            if (!_editing && !_panning && Time.unscaledTime >= _freeLookUntil)
                _cam = Vector2.Lerp(_cam, Wanted(), 1f - Mathf.Exp(-Time.unscaledDeltaTime * 5f));
            _cam = Clamp(_cam);
            _world.anchoredPosition = -_cam;
        }

        bool _panning;

        /// <summary>Dragging the floor looks along the room (for a while, then the view comes back to you).</summary>
        void Pan(Vector2 screenDelta)
        {
            var canvas = _body.GetComponentInParent<Canvas>();
            float k = canvas != null ? 1f / canvas.scaleFactor : 1f;
            _cam = Clamp(_cam - screenDelta * k);
            _world.anchoredPosition = -_cam;
            _freeLookUntil = Time.unscaledTime + 4f;
        }

        // While a piece is dragged near a side of the screen, the view slides along to make room.
        Vector2? _dragPointer;
        PieceView _dragPiece;

        void EdgePan()
        {
            if (_dragPiece == null || !_dragPointer.HasValue) return;
            float x = _dragPointer.Value.x / Mathf.Max(1f, Screen.width);
            float dir = x < 0.12f ? -1f : x > 0.88f ? 1f : 0f;
            if (dir == 0f) return;
            _cam = Clamp(_cam + new Vector2(dir * 900f * Time.unscaledDeltaTime, 0f));
            _world.anchoredPosition = -_cam;
            _dragPiece.Follow(_dragPointer.Value);
        }

        // ---------------------------------------------------------------- floor coordinates

        /// <summary>Room units of a spot on the floor: x 0 … 1 across, y 0 (front) … 1 (back wall).</summary>
        static Vector2 FloorPoint(float x, float y) => new(Floor.xMin + x * Floor.width, Floor.yMin + y * Floor.height);

        static Vector2 ToFloorPoint(Vector2 room) =>
            new(Mathf.Clamp01((room.x - Floor.xMin) / Floor.width), Mathf.Clamp01((room.y - Floor.yMin) / Floor.height));

        /// <summary>Further back, a little smaller.</summary>
        static float DepthScale(float y) => Mathf.Lerp(1f, 0.72f, y);

        /// <summary>A pointer position as a floor spot (clamped onto the floor).</summary>
        Vector2 ToFloor(Vector2 screen, Camera cam)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_world, screen, cam, out var local);
            return ToFloorPoint(local);
        }

        bool OverRoom(Vector2 screen, Camera cam)
        {
            if (!_editing || _drawer == null) return true;
            return !RectTransformUtility.RectangleContainsScreenPoint(_drawer, screen, cam);
        }

        // ---------------------------------------------------------------- you, walking about

        // Where you stand (floor coordinates) and in which room; kept while the game runs.
        static Vector2 _youAt = new(0.12f, 0.75f);
        Vector2? _walkTo;
        Action _arrived;
        bool _facingRight = true;   // into the room, from the steps
        float _hop;
        RectTransform _you, _youBody;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSpot()
        {
            _youAt = new Vector2(0.12f, 0.75f);
            _homeRoom = 0;
        }

        void You()
        {
            _you = DressedCapy(_pieces, PieceUnit * 0.3f);
            _you.name = "You";
            _you.anchorMin = _you.anchorMax = Vector2.zero;
            _youBody = (RectTransform)_you.Find("Body");
            _walkTo = null;
            _arrived = null;
            PlaceYou();
            // Breathing, on the body so walking can move the whole capybara.
            if (_youBody != null)
            {
                _youBody.pivot = new Vector2(0.5f, 0f);
                _youBody.DOScaleY(1.025f, 1.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(_youBody.gameObject);
            }
        }

        /// <summary>A friend's burrow: they're home too, by the door, to say hello.</summary>
        void Host()
        {
            if (_room != 0 || _friend == null) return;
            var host = DressedCapy(_pieces, PieceUnit * 0.3f, slot =>
            {
                foreach (var id in _friend.outfit) { var w = Wardrobe.Find(id); if (w != null && w.Slot == slot) return w; }
                return null;
            });
            host.name = "Host";
            host.anchorMin = host.anchorMax = Vector2.zero;
            var at = new Vector2(0.24f, 0.7f);
            host.anchoredPosition = FloorPoint(at.x, at.y);
            float s = DepthScale(at.y);
            host.localScale = new Vector3(s, s, 1f);
            var name = UiKit.Text(host, "Name", _friend.name, 34f, Color.white, true, TextAlignmentOptions.Center);
            name.rectTransform.anchorMin = name.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            name.rectTransform.sizeDelta = new Vector2(400f, 50f);
            name.rectTransform.anchoredPosition = new Vector2(0f, 30f);
            name.outlineWidth = 0.25f;
            name.outlineColor = new Color32(60, 40, 25, 255);
            name.raycastTarget = false;
        }

        void PlaceYou()
        {
            if (_you == null || _sitting != null) return;
            _you.anchoredPosition = FloorPoint(_youAt.x, _youAt.y);
            float s = DepthScale(_youAt.y);
            // The picture faces left: mirrored to face right.
            _you.localScale = new Vector3(_facingRight ? -s : s, s, 1f);
        }

        /// <summary>Tapped the floor: off you trot.</summary>
        void WalkTo(Vector2 floor)
        {
            StandUp(instant: false);
            _walkTo = floor;
            _arrived = null;
            _freeLookUntil = 0f;
            Sfx.Play(SfxKind.Tap, 0.25f);
        }

        /// <summary>Walks to a spot, then does something there.</summary>
        void WalkThen(Vector2 floor, Action then)
        {
            StandUp(instant: false);
            _walkTo = floor;
            _arrived = then;
            _freeLookUntil = 0f;
        }

        const float WalkSpeed = 560f;   // room units a second, at the front of the room

        void Walk()
        {
            if (_you == null || !_walkTo.HasValue || _sitting != null) return;
            Vector2 here = FloorPoint(_youAt.x, _youAt.y), there = FloorPoint(_walkTo.Value.x, _walkTo.Value.y);
            var step = Vector2.MoveTowards(here, there, WalkSpeed * DepthScale(_youAt.y) * Time.unscaledDeltaTime);
            if (Mathf.Abs(there.x - here.x) > 1f) _facingRight = there.x > here.x;
            _youAt = ToFloorPoint(step);
            bool arrived = (step - there).sqrMagnitude < 1f;
            _hop += Time.unscaledDeltaTime * 14f;
            if (_youBody != null) _youBody.anchoredPosition = new Vector2(0f, arrived ? 0f : Mathf.Abs(Mathf.Sin(_hop)) * 10f);
            PlaceYou();
            Sort();
            if (!arrived) return;
            _walkTo = null;
            var then = _arrived;
            _arrived = null;
            then?.Invoke();
        }

        /// <summary>The floor itself: a tap walks you there (or lets go of a picked piece); a drag looks along the room.</summary>
        class FloorTap : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            BurrowScreen _screen;
            bool _dragged;

            public void Init(BurrowScreen s) => _screen = s;

            public void OnPointerClick(PointerEventData e)
            {
                if (_dragged) { _dragged = false; return; }
                bool picked = _screen._selected != null;
                _screen.Select(null);
                if (picked || _screen._editing) return;   // the first tap just lets go; decorating, the floor isn't for walking
                _screen.WalkTo(_screen.ToFloor(e.position, e.pressEventCamera));
            }

            public void OnBeginDrag(PointerEventData e) { _dragged = true; _screen._panning = true; }
            public void OnDrag(PointerEventData e) => _screen.Pan(e.delta);
            public void OnEndDrag(PointerEventData e) => _screen._panning = false;
        }

        // ---------------------------------------------------------------- the Furniture Shop's bubble

        /// <summary>
        /// In a new burrow, the Furniture Shop waits in a bubble (unless you have one already).
        /// It only goes up outside, so popping it puts it in your trail on the island.
        /// </summary>
        void ShopBubble()
        {
            if (HomeLog.ShopGiven) return;
            if (StoreData.CountByName("Furniture Shop") > 0) { HomeLog.ShopGiven = true; return; }
            var catalog = ItemCatalog.Instance;
            var art = catalog != null && catalog.Has("Furniture Shop") ? catalog.Icon("Furniture Shop") : null;
            if (art == null) return;
            var bubble = FloatingBubble("ShopBubble", art, FloorPoint(0.24f, 0.5f));
            bubble.GetComponent<Button>().onClick.AddListener(() => PopShop(bubble, art));
        }

        /// <summary>
        /// The photo album comes with the burrow, in a bubble by the door: pop it and it goes in
        /// storage, to stand wherever you like (Decorate).
        /// </summary>
        void AlbumBubble()
        {
            if (!HomeLog.AlbumWaiting) return;
            var art = Furniture.Get(Furniture.Album)?.Art;
            if (art == null) return;
            var bubble = FloatingBubble("AlbumBubble", art, FloorPoint(0.36f, 0.62f));
            bubble.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (!HomeLog.AlbumWaiting) return;
                HomeLog.GiveAlbum();
                Sfx.Play(SfxKind.Pop, 0.8f);
                bubble.DOKill();
                bubble.DOScale(1.25f, 0.12f).SetUpdate(true).OnComplete(() => { if (bubble != null) Destroy(bubble.gameObject); });
                RefreshDrawer();
                CardPopup.Show("A photo album!",
                    "It's in your storage. Tap Decorate and stand it wherever you like, then fill it with photos from your camera.", art);
            });
        }

        /// <summary>A bubble floating over the floor with something in it, bobbing gently. Tap it (its Button) to pop.</summary>
        RectTransform FloatingBubble(string name, Sprite art, Vector2 over)
        {
            float size = PieceUnit * 0.34f;
            var bubble = UiKit.Image(_fx, name, UiKit.Circle, new Color(0.85f, 0.95f, 1f, 0.55f), true).rectTransform;
            bubble.anchorMin = bubble.anchorMax = Vector2.zero;
            bubble.sizeDelta = new Vector2(size, size);
            bubble.anchoredPosition = over + new Vector2(0f, size * 0.6f);
            var rim = UiKit.Image(bubble, "Rim", IconPainter.Ring, new Color(1f, 1f, 1f, 0.9f));
            UiKit.Stretch(rim.rectTransform);
            var pic = UiKit.Image(bubble, "Shop", art, Color.white);
            pic.preserveAspect = true;
            UiKit.Stretch(pic.rectTransform, size * 0.16f, size * 0.16f, size * 0.16f, size * 0.16f);
            var shine = UiKit.Image(bubble, "Shine", UiKit.Circle, new Color(1f, 1f, 1f, 0.7f));
            UiKit.Place(shine.rectTransform, new Vector2(0.28f, 0.74f), Vector2.zero, new Vector2(size * 0.16f, size * 0.1f));
            shine.raycastTarget = false;
            bubble.DOAnchorPosY(bubble.anchoredPosition.y + 14f, 1.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(bubble.gameObject);
            var tap = bubble.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            return bubble;
        }

        void PopShop(RectTransform bubble, Sprite art)
        {
            if (HomeLog.ShopGiven) return;
            var catalog = ItemCatalog.Instance;
            var item = catalog != null ? catalog.Create("Furniture Shop") : null;
            if (item == null) return;
            var king = FindFirstObjectByType<PlayerAvatar>();
            item.transform.position = king != null ? king.transform.position : Vector3.zero;
            item.SetActive(true);
            if (item.TryGetComponent<Placeable>(out var placeable)) placeable.MarkCarried();
            StoreData.InvalidateCounts();
            HomeLog.ShopGiven = true;

            Sfx.Play(SfxKind.Pop, 0.8f);
            bubble.DOKill();
            bubble.DOScale(1.25f, 0.12f).SetUpdate(true).OnComplete(() => { if (bubble != null) Destroy(bubble.gameObject); });
            CardPopup.Show("A Furniture Shop!",
                "It's far too big for in here, so it's in your trail. Go outside and put it up somewhere on your island, then tap it to shop for your burrow.", art);
        }

        // ---------------------------------------------------------------- the dressed capybara

        /// <summary>
        /// A capybara as it's dressed (you, unless <paramref name="worn"/> says otherwise), standing on
        /// its feet (pivot at the bottom). The body is one layer among its clothes, under a "Body"
        /// holder that breathes and hops: a cape that goes behind sits under the body layer (a
        /// picture's children always draw over it, which put the cape on top of you).
        /// </summary>
        public static RectTransform DressedCapy(Transform parent, float height, Func<WearSlot, WearItem> worn = null)
        {
            worn ??= Wardrobe.Worn;
            var root = UiKit.Rect("Capy", parent);
            root.pivot = new Vector2(0.5f, 0f);
            var body = Wardrobe.Ready ? Wardrobe.BaseSprite : ItemCatalog.Instance != null ? ItemCatalog.Instance.Icon("Capybara") : null;
            float aspect = body != null ? body.rect.width / body.rect.height : 1f;
            root.sizeDelta = new Vector2(height * aspect, height);
            var holder = UiKit.Rect("Body", root);
            UiKit.Stretch(holder);
            var img = UiKit.Image(holder, "Base", body, Color.white);
            img.preserveAspect = true;
            img.raycastTarget = false;
            UiKit.Stretch(img.rectTransform);
            if (!Wardrobe.Ready) return root;
            foreach (WearSlot slot in Enum.GetValues(typeof(WearSlot)))
            {
                var item = worn(slot);
                var art = Wardrobe.Art(item);
                if (art == null) continue;
                var layer = UiKit.Image(holder, slot.ToString(), art, Color.white);
                layer.preserveAspect = true;
                layer.raycastTarget = false;
                var r = layer.rectTransform;
                r.anchorMin = r.anchorMax = item.Anchor;
                float pw = item.Width * root.sizeDelta.x;
                r.sizeDelta = new Vector2(pw, pw * art.rect.height / art.rect.width);
                r.localRotation = Quaternion.Euler(0f, 0f, item.Rotation);
                if (item.Behind) r.SetAsFirstSibling();   // under the body layer
            }
            return root;
        }

        // ---------------------------------------------------------------- furniture in the room

        void MakeView(HomeLog.Placed p)
        {
            var def = Furniture.Get(p.id);
            if (def == null) return;
            var art = def.Art;
            var r = UiKit.Rect(def.Id, _pieces);
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.pivot = new Vector2(0.5f, def.Flat ? 0.5f : 0.06f);
            float wid = def.Width * PieceUnit;
            float aspect = art != null ? art.rect.height / art.rect.width : 1f;
            r.sizeDelta = new Vector2(wid, wid * aspect);
            if (def.Use == FurnitureUse.Light)
            {
                // Its light: a warm glow about it while it's on.
                var glow = UiKit.Image(r, "Glow", IconPainter.Glow, new Color(1f, 0.85f, 0.45f, 0.55f));
                glow.raycastTarget = false;
                UiKit.Place(glow.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(wid * 1.6f, wid * 1.6f));
            }
            var img = UiKit.Image(r, "Art", art, Color.white, true);
            img.preserveAspect = true;
            UiKit.Stretch(img.rectTransform);
            r.gameObject.AddComponent<PieceView>().Init(this, p);
            _views[p] = r;
            Place(p);
            Lit(p);
        }

        void Place(HomeLog.Placed p)
        {
            if (!_views.TryGetValue(p, out var r)) return;
            r.anchoredPosition = FloorPoint(p.x, p.y);
            float s = DepthScale(p.y);
            r.localScale = new Vector3(p.flip ? -s : s, s, 1f);
        }

        /// <summary>A lamp on or off (its glow, and the picture a touch darker when it's off).</summary>
        void Lit(HomeLog.Placed p)
        {
            if (!_views.TryGetValue(p, out var r)) return;
            var glow = r.Find("Glow");
            if (glow != null) glow.gameObject.SetActive(!p.off);
            var img = r.Find("Art")?.GetComponent<Image>();
            if (img != null && Furniture.Get(p.id)?.Use == FurnitureUse.Light) img.color = p.off ? new Color(0.78f, 0.74f, 0.7f) : Color.white;
        }

        /// <summary>Back to front: rugs first, then everything (you too) by how far back it stands.</summary>
        void Sort()
        {
            if (_pieces == null) return;
            var order = new List<(float y, bool flat, Transform t)>();
            foreach (var kv in _views)
                order.Add((kv.Key.y, Furniture.Get(kv.Key.id)?.Flat == true, kv.Value));
            var host = _pieces.Find("Host");
            if (host != null) order.Add((0.7f, false, host));
            if (_you != null && _sitting == null && _you.parent == _pieces) order.Add((_youAt.y, false, _you));
            order.Sort((a, b) =>
            {
                if (a.flat != b.flat) return a.flat ? -1 : 1;
                return b.y.CompareTo(a.y);
            });
            int i = 0;
            foreach (var (_, _, t) in order) t.SetSiblingIndex(i++);
            // Sitting: just in front of what you sit on (a soak is in its own holder there).
            if (_sitting != null && _views.TryGetValue(_sitting, out var seat))
            {
                var mine = _soak != null ? (Transform)_soak : _you;
                if (mine != null && mine.parent == _pieces) mine.SetSiblingIndex(Mathf.Min(seat.GetSiblingIndex() + 1, _pieces.childCount - 1));
                if (_foam != null && mine != null) _foam.SetSiblingIndex(Mathf.Min(mine.GetSiblingIndex() + 1, _pieces.childCount - 1));   // over the waterline
            }
        }

        void Select(HomeLog.Placed p)
        {
            _selected = p;
            if (_toolbar != null) Destroy(_toolbar.gameObject);
            _toolbar = null;
            foreach (var kv in _views)
            {
                var img = kv.Value.Find("Art")?.GetComponent<Image>();
                if (img == null) continue;
                bool dim = kv.Key.off && Furniture.Get(kv.Key.id)?.Use == FurnitureUse.Light;
                img.color = kv.Key == p ? new Color(1f, 0.95f, 0.8f) : dim ? new Color(0.78f, 0.74f, 0.7f) : Color.white;
            }
            if (p == null || !_views.TryGetValue(p, out var view)) return;

            // A little bar over the piece: flip it round, or put it back in storage.
            _toolbar = UiKit.Image(_fx, "Tools", UiKit.Rounded(30), UiKit.HudGlass, true).rectTransform;
            _toolbar.anchorMin = _toolbar.anchorMax = Vector2.zero;
            _toolbar.pivot = new Vector2(0.5f, 0f);
            _toolbar.sizeDelta = new Vector2(420f, 96f);
            _toolbar.anchoredPosition = view.anchoredPosition + new Vector2(0f, view.sizeDelta.y * view.localScale.y * (1f - view.pivot.y) + 16f);
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

        /// <summary>A placed piece: decorating, drag to move it and tap to pick it; otherwise tap to use it.</summary>
        class PieceView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
        {
            BurrowScreen _screen;
            HomeLog.Placed _p;
            bool _dragged, _moving;
            Vector2 _grab;
            Camera _cam;

            public void Init(BurrowScreen s, HomeLog.Placed p) { _screen = s; _p = p; }

            public void OnPointerClick(PointerEventData e)
            {
                if (_dragged) { _dragged = false; return; }
                if (_screen._editing) _screen.Select(_screen._selected == _p ? null : _p);
                else _screen.Use(_p);
            }

            public void OnBeginDrag(PointerEventData e)
            {
                _dragged = true;
                _moving = _screen._editing;
                if (!_moving) { _screen._panning = true; return; }   // not decorating: a drag looks along the room
                _screen.Select(null);
                _cam = e.pressEventCamera;
                var at = _screen.ToFloor(e.position, _cam);
                _grab = new Vector2(_p.x, _p.y) - at;
                _screen._dragPiece = this;
                transform.DOScale(transform.localScale * 1.06f, 0.1f).SetUpdate(true);
            }

            public void OnDrag(PointerEventData e)
            {
                if (!_moving) { _screen.Pan(e.delta); return; }
                _screen._dragPointer = e.position;
                Follow(e.position);
            }

            public void Follow(Vector2 pointer)
            {
                var at = _screen.ToFloor(pointer, _cam) + _grab;
                _p.x = Mathf.Clamp01(at.x);
                _p.y = Mathf.Clamp01(at.y);
                _screen.Place(_p);
                _screen.Sort();
            }

            public void OnEndDrag(PointerEventData e)
            {
                if (!_moving) { _screen._panning = false; return; }
                _screen._dragPiece = null;
                _screen._dragPointer = null;
                _screen.Place(_p);
                _screen.Sort();
                HomeLog.Save();
                Sfx.Play(SfxKind.Plop, 0.4f);
            }
        }

        // ---------------------------------------------------------------- using the furniture

        HomeLog.Placed _sitting;       // what you're sitting / lying / soaking in, if anything
        RectTransform _soak;           // the waterline you're in up to your neck
        RectTransform _foam;           // foam (or the blanket's edge) along it, so you never see where you're cut off
        Coroutine _poseFx;
        Image _towel;

        /// <summary>Where you are in the room (room units), sitting, soaking or standing.</summary>
        Vector2 YouInRoom() => _you == null ? Vector2.zero : (Vector2)_world.InverseTransformPoint(_you.position);

        /// <summary>
        /// Soft blobs along the waterline, as at the hot spring: white foam in the water, the
        /// quilt's own red under a kotatsu. They hide the line where the capybara goes in.
        /// </summary>
        void Foam(Vector2 waterline, float halfWidth, bool quilt)
        {
            _foam = UiKit.Rect("Foam", _pieces);
            _foam.anchorMin = _foam.anchorMax = Vector2.zero;
            _foam.anchoredPosition = waterline;
            float capyHalf = _you.sizeDelta.x * Mathf.Abs(_you.localScale.x) * 0.5f;
            float half = Mathf.Min(halfWidth, capyHalf * 0.85f);
            const int blobs = 9;
            for (int i = 0; i < blobs; i++)
            {
                float t = i / (blobs - 1f), edge = Mathf.Abs(t - 0.5f) * 2f;
                var c = quilt ? (i % 3 == 1 ? UiKit.Hex("D9705E") : UiKit.Hex("C4584A")) : i % 3 == 1 ? new Color(0.84f, 0.95f, 0.97f, 0.95f) : new Color(1f, 1f, 1f, 0.95f);
                var blob = UiKit.Image(_foam, "Blob", UiKit.Circle, c);
                blob.raycastTarget = false;
                var r = blob.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                float size = Mathf.Lerp(38f, 26f, edge) * UnityEngine.Random.Range(0.85f, 1.15f);
                r.sizeDelta = new Vector2(size * 1.3f, size);
                r.anchoredPosition = new Vector2(Mathf.Lerp(-half, half, t), UnityEngine.Random.Range(-4f, 4f) - edge * 6f);
                r.DOScale(1.1f, UnityEngine.Random.Range(0.7f, 1.2f)).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(blob.gameObject);
            }
        }

        /// <summary>Tapped a piece (not decorating): over to it, and use it.</summary>
        void Use(HomeLog.Placed p)
        {
            var def = Furniture.Get(p.id);
            if (def == null || !_views.TryGetValue(p, out var view)) return;
            if (_sitting == p) { StandUp(instant: false); return; }
            StandUp(instant: false);

            switch (def.Use)
            {
                case FurnitureUse.Album:
                    // Yours to fill, or a friend's to look through.
                    var friend = _friend;
                    WalkThen(Beside(p, view), () => { FaceTowards(view); if (friend == null) AlbumScreen.Show(); else AlbumScreen.ShowFriend(friend); });
                    return;
                case FurnitureUse.Dress:
                    WalkThen(Beside(p, view), () =>
                    {
                        FaceTowards(view);
                        Burst(view, IconPainter.Hanger, UiKit.Hex("A77BD6"), 1);
                        if (WardrobeScreen.Instance != null) WardrobeScreen.Instance.Open(RebuildYou);
                    });
                    return;
                case FurnitureUse.Light:
                    WalkThen(Beside(p, view), () =>
                    {
                        FaceTowards(view);
                        p.off = !p.off;
                        Lit(p);
                        Sfx.Play(SfxKind.Tap, 0.6f);
                        if (Mine) HomeLog.Save();
                    });
                    return;
            }

            bool onIt = def.Use is FurnitureUse.Sit or FurnitureUse.Lie or FurnitureUse.Swing or FurnitureUse.Soak or FurnitureUse.Tuck;
            if (onIt) { WalkThen(InFront(p), () => SitOn(p, def, view)); return; }
            WalkThen(Beside(p, view), () => { FaceTowards(view); Act(p, def, view); });
        }

        /// <summary>Just in front of a piece, on the floor.</summary>
        static Vector2 InFront(HomeLog.Placed p) => new(p.x, Mathf.Clamp01(p.y - 0.06f));

        /// <summary>Beside a piece (on your side of it), close enough to reach it.</summary>
        Vector2 Beside(HomeLog.Placed p, RectTransform view)
        {
            float half = view.sizeDelta.x * Mathf.Abs(view.localScale.x) * 0.5f + PieceUnit * 0.12f;
            float side = _youAt.x <= p.x ? -1f : 1f;
            var spot = FloorPoint(p.x, p.y) + new Vector2(side * half, -12f);
            if (spot.x < Floor.xMin || spot.x > Floor.xMax) spot.x = FloorPoint(p.x, p.y).x - side * half;
            return ToFloorPoint(spot);
        }

        void FaceTowards(RectTransform view)
        {
            _facingRight = view.anchoredPosition.x > _you.anchoredPosition.x;
            PlaceYou();
        }

        /// <summary>Up onto it: sitting, lying, swinging, or in the water up to your neck.</summary>
        void SitOn(HomeLog.Placed p, FurnitureDef def, RectTransform view)
        {
            _sitting = p;
            float s = DepthScale(p.y);
            float h = view.sizeDelta.y * s;
            var foot = view.anchoredPosition;
            float pivotLift = view.pivot.y * h;   // the piece's anchor is a little up its picture
            var seat = foot + new Vector2(0f, h * def.Seat - pivotLift);
            _you.DOKill();
            _you.localRotation = Quaternion.identity;
            float ys = s * (def.Use == FurnitureUse.Lie ? 0.92f : 1f);
            _you.localScale = new Vector3(_facingRight ? -ys : ys, ys, 1f);

            if (def.Use is FurnitureUse.Soak or FurnitureUse.Tuck)
            {
                // In it up to the neck: only what's above the water (or the blanket) shows.
                _soak = UiKit.Rect("Soak", _pieces);
                _soak.anchorMin = _soak.anchorMax = Vector2.zero;
                _soak.pivot = new Vector2(0.5f, 0f);
                _soak.sizeDelta = new Vector2(view.sizeDelta.x * s * 1.4f, 1200f);
                _soak.anchoredPosition = seat;
                _soak.gameObject.AddComponent<RectMask2D>();
                _you.SetParent(_soak, false);
                _you.anchorMin = _you.anchorMax = new Vector2(0.5f, 0f);
                _you.anchoredPosition = new Vector2(0f, -_you.sizeDelta.y * ys * (def.Use == FurnitureUse.Tuck ? 0.3f : 0.34f));
                if (def.Use == FurnitureUse.Soak) Towel(true);
                Foam(seat, view.sizeDelta.x * s * 0.5f, def.Use == FurnitureUse.Tuck);
                Sfx.Play(SfxKind.Plop, 0.7f);
            }
            else
            {
                _you.anchoredPosition = seat;
                Sfx.Play(SfxKind.Plop, 0.4f);
            }
            if (_youBody != null) _youBody.DOPunchScale(new Vector3(0.08f, -0.1f, 0f), 0.35f, 5, 0.6f).SetUpdate(true);
            if (def.Use == FurnitureUse.Swing)
            {
                _you.localRotation = Quaternion.Euler(0f, 0f, -5f);
                _you.DOLocalRotate(new Vector3(0f, 0f, 5f), 1.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(_you.gameObject);
                view.DOLocalRotate(new Vector3(0f, 0f, 2.5f), 1.4f).From(new Vector3(0f, 0f, -2.5f)).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(view.gameObject);
            }
            Sort();
            _poseFx = StartCoroutine(PoseFx(def, view));
        }

        /// <summary>While you're on it: z's in bed, steam in the tub, a heart now and then.</summary>
        IEnumerator PoseFx(FurnitureDef def, RectTransform view)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            while (_sitting != null)
            {
                var head = (Vector2)_you.position;
                switch (def.Use)
                {
                    case FurnitureUse.Lie: FloatUp(Zed, Color.white, Above(_you, 0.9f), 56f, new Vector2(_facingRight ? 40f : -40f, 160f)); break;
                    case FurnitureUse.Soak:
                        FloatUp(IconPainter.Cloud, new Color(1f, 1f, 1f, 0.6f), Above(view, 0.55f) + new Vector2(UnityEngine.Random.Range(-80f, 80f), 0f), 90f, new Vector2(0f, 200f));
                        if (def.Id == "Bathtub") FloatUp(IconPainter.Ring, new Color(1f, 1f, 1f, 0.85f), Above(view, 0.5f) + new Vector2(UnityEngine.Random.Range(-120f, 120f), 0f), 40f, new Vector2(UnityEngine.Random.Range(-30f, 30f), 160f));
                        break;
                    default: FloatUp(IconPainter.Heart, UiKit.Hex("FF8FB0"), Above(_you, 1f), 50f, new Vector2(0f, 140f)); break;
                }
                yield return new WaitForSecondsRealtime(def.Use == FurnitureUse.Soak ? 0.7f : def.Use == FurnitureUse.Lie ? 1.1f : 2.6f);
            }
        }

        /// <summary>Off it again, back onto the floor in front of it.</summary>
        void StandUp(bool instant)
        {
            if (_sitting == null || _you == null) return;
            var p = _sitting;
            _sitting = null;
            if (_poseFx != null) StopCoroutine(_poseFx);
            _poseFx = null;
            Towel(false);
            _you.DOKill();
            _you.localRotation = Quaternion.identity;
            if (_views.TryGetValue(p, out var view)) { view.DOKill(); view.localRotation = Quaternion.identity; }
            if (_you.parent != _pieces) _you.SetParent(_pieces, false);
            _you.anchorMin = _you.anchorMax = Vector2.zero;
            if (_soak != null) Destroy(_soak.gameObject);
            if (_foam != null) Destroy(_foam.gameObject);
            _foam = null;
            _soak = null;
            _youAt = InFront(p);
            PlaceYou();
            Sort();
            if (!instant) Sfx.Play(SfxKind.Plop, 0.3f);
        }

        /// <summary>A towel on your head in the hot water, as at the hot spring.</summary>
        void Towel(bool on)
        {
            if (!on)
            {
                if (_towel != null) Destroy(_towel.gameObject);
                _towel = null;
                var worn = _youBody != null ? _youBody.Find(WearSlot.Hat.ToString()) : null;
                if (worn != null) worn.gameObject.SetActive(true);   // your hat back on
                return;
            }
            var item = Wardrobe.Find("Hat_Towel");
            var art = Wardrobe.Art(item);
            if (art == null || _youBody == null) return;
            var hat = _youBody.Find(WearSlot.Hat.ToString());
            if (hat != null) hat.gameObject.SetActive(false);
            _towel = UiKit.Image(_youBody, "Towel", art, Color.white);
            _towel.preserveAspect = true;
            _towel.raycastTarget = false;
            var r = _towel.rectTransform;
            r.anchorMin = r.anchorMax = item.Anchor;
            float pw = item.Width * _you.sizeDelta.x;
            r.sizeDelta = new Vector2(pw, pw * art.rect.height / art.rect.width);
            r.localRotation = Quaternion.Euler(0f, 0f, item.Rotation);
        }

        void RebuildYou()
        {
            if (!IsOpen) return;
            StandUp(instant: true);
            var at = _youAt;
            if (_you != null) Destroy(_you.gameObject);
            You();
            _youAt = at;
            PlaceYou();
            Sort();
        }

        /// <summary>Using something from beside it: a tune, a book, a snack, the telly…</summary>
        void Act(HomeLog.Placed p, FurnitureDef def, RectTransform view)
        {
            switch (def.Use)
            {
                case FurnitureUse.Music:
                    if (def.Id == "Radio")
                    {
                        // The radio stays on (notes drift out of it) until you switch it off.
                        p.off = !p.off;
                        if (Mine) HomeLog.Save();
                        Sfx.Play(SfxKind.Tap, 0.6f);
                        if (!p.off) Burst(view, Note, UiKit.Hex("5B9BD5"), 3);
                        return;
                    }
                    StartCoroutine(Tune(def, view));
                    return;
                case FurnitureUse.Warm:
                    StartCoroutine(Repeat(6, 0.5f, () => FloatUp(IconPainter.Sparkle, UiKit.Hex("FFB347"), Above(view, 0.5f) + new Vector2(UnityEngine.Random.Range(-60f, 60f), 0f), 40f, new Vector2(UnityEngine.Random.Range(-30f, 30f), 180f))));
                    Burst(_you, IconPainter.Heart, UiKit.Hex("FF8FB0"), 2);
                    if (_youBody != null) _youBody.DOPunchScale(new Vector3(0.06f, -0.06f, 0f), 0.5f, 4, 0.5f).SetUpdate(true);
                    return;
                case FurnitureUse.Read:
                    Hold(IconPainter.Book, def.Id == "Desk" ? UiKit.Hex("F2E6C9") : UiKit.Hex(BookColours[UnityEngine.Random.Range(0, BookColours.Length)]), 3f);
                    StartCoroutine(Repeat(2, 1.2f, () => FloatUp(IconPainter.Heart, UiKit.Hex("FF8FB0"), Above(_you, 1f), 46f, new Vector2(0f, 130f))));
                    Sfx.Play(SfxKind.Rustle, 0.5f);
                    return;
                case FurnitureUse.Watch:
                    StartCoroutine(Watching(def, view));
                    return;
                case FurnitureUse.Cook:
                    StartCoroutine(Cooking(view));
                    return;
                case FurnitureUse.Snack:
                    Sfx.Play(SfxKind.Tap, 0.5f);
                    Hold(SnackArt(), Color.white, 1.6f, munch: true);
                    return;
                case FurnitureUse.Wash:
                    StartCoroutine(Repeat(8, 0.18f, () => FloatUp(IconPainter.Ring, new Color(1f, 1f, 1f, 0.9f), Above(_you, 0.55f) + new Vector2(UnityEngine.Random.Range(-70f, 70f), 0f), UnityEngine.Random.Range(26f, 48f), new Vector2(UnityEngine.Random.Range(-40f, 40f), 160f))));
                    Sfx.Play(SfxKind.Plop, 0.5f);
                    return;
                case FurnitureUse.Toy:
                    Toy(view);
                    return;
                case FurnitureUse.Game:
                    StartCoroutine(Repeat(6, 0.3f, () =>
                    {
                        FloatUp(IconPainter.Star, UiKit.Hex(BookColours[UnityEngine.Random.Range(0, BookColours.Length)]), Above(view, 0.8f), 42f, new Vector2(UnityEngine.Random.Range(-60f, 60f), 150f));
                        Sfx.Play(SfxKind.Tap, 0.35f);
                    }));
                    return;
                default:   // a lovely thing to look at
                    Burst(view, IconPainter.Heart, UiKit.Hex("FF8FB0"), 2);
                    Sfx.Play(SfxKind.Heart, 0.35f);
                    return;
            }
        }

        static readonly string[] BookColours = { "E8553E", "5B9BD5", "5DBB63", "FFC93C", "A77BD6", "FF8A3D" };

        IEnumerator Tune(FurnitureDef def, RectTransform view)
        {
            bool drum = def.Id == "Taiko";
            for (int i = 0; i < 6; i++)
            {
                if (drum)
                {
                    Sfx.Play(SfxKind.Boom, 0.25f, 0.2f);
                    view.DOPunchScale(new Vector3(0.05f, -0.05f, 0f), 0.2f, 4, 0.5f).SetUpdate(true);
                }
                else Sfx.Play(SfxKind.Heart, 0.3f, 0.35f);
                FloatUp(drum ? IconPainter.Star : Note, UiKit.Hex(drum ? "FFC93C" : "5B9BD5"), Above(view, 0.85f) + new Vector2(UnityEngine.Random.Range(-50f, 50f), 0f), 52f, new Vector2(UnityEngine.Random.Range(-70f, 70f), 170f));
                if (_youBody != null) _youBody.DOPunchScale(new Vector3(0.04f, -0.05f, 0f), 0.25f, 4, 0.5f).SetUpdate(true);
                yield return new WaitForSecondsRealtime(drum ? 0.45f : UnityEngine.Random.Range(0.3f, 0.55f));
            }
        }

        IEnumerator Watching(FurnitureDef def, RectTransform view)
        {
            var img = view.Find("Art")?.GetComponent<Image>();
            for (int i = 0; i < 8; i++)
            {
                if (def.Id == "Telly" && img != null) img.color = i % 2 == 0 ? new Color(0.85f, 0.92f, 1f) : Color.white;
                if (def.Id == "Aquarium") FloatUp(IconPainter.Ring, new Color(0.85f, 0.95f, 1f, 0.9f), Above(view, 0.6f) + new Vector2(UnityEngine.Random.Range(-40f, 40f), 0f), 26f, new Vector2(0f, 70f));
                if (def.Id == "Telescope" && i % 2 == 0) FloatUp(IconPainter.Sparkle, UiKit.Hex("FFF1B0"), Above(view, 1.1f) + new Vector2(UnityEngine.Random.Range(-90f, 90f), 0f), 40f, new Vector2(0f, 90f));
                yield return new WaitForSecondsRealtime(0.35f);
            }
            if (img != null) img.color = Color.white;
            Burst(_you, IconPainter.Heart, UiKit.Hex("FF8FB0"), 1);
        }

        IEnumerator Cooking(RectTransform view)
        {
            Sfx.Play(SfxKind.Stir, 0.5f);
            for (int i = 0; i < 6; i++)
            {
                FloatUp(IconPainter.Cloud, new Color(1f, 1f, 1f, 0.6f), Above(view, 0.9f) + new Vector2(UnityEngine.Random.Range(-40f, 40f), 0f), 70f, new Vector2(0f, 160f));
                yield return new WaitForSecondsRealtime(0.3f);
            }
            Hold(SnackArt(), Color.white, 1.6f, munch: true);
        }

        static Sprite SnackArt()
        {
            var dishes = HobbyData.Dishes;
            for (int i = 0; i < 6; i++)
            {
                var art = HobbyData.ItemArt("dish:" + dishes[UnityEngine.Random.Range(0, dishes.Length)].Id);
                if (art != null) return art;
            }
            return IconPainter.Heart;
        }

        /// <summary>A ball bounces out of the chest and rolls away.</summary>
        void Toy(RectTransform view)
        {
            var ball = UiKit.Image(_fx, "Ball", UiKit.Circle, UiKit.Hex(BookColours[UnityEngine.Random.Range(0, BookColours.Length)]));
            ball.raycastTarget = false;
            var r = ball.rectTransform;
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.sizeDelta = new Vector2(70f, 70f);
            var from = Above(view, 0.7f);
            r.anchoredPosition = from;
            var to = from + new Vector2(UnityEngine.Random.value < 0.5f ? -260f : 260f, -view.sizeDelta.y * 0.5f);
            r.DOJumpAnchorPos(to, 160f, 3, 1.4f).SetUpdate(true).SetLink(ball.gameObject);
            ball.DOFade(0f, 0.4f).SetDelay(1.8f).SetUpdate(true).SetLink(ball.gameObject).OnComplete(() => Destroy(ball.gameObject));
            Sfx.Play(SfxKind.Pop, 0.5f);
            Burst(_you, IconPainter.Star, UiKit.Sun, 1);
        }

        /// <summary>The radios you've switched on keep playing: a note now and then.</summary>
        float _nextRadioNote;

        void Radios()
        {
            if (Time.unscaledTime < _nextRadioNote) return;
            _nextRadioNote = Time.unscaledTime + 0.9f;
            foreach (var kv in _views)
            {
                if (kv.Key.id != "Radio" || kv.Key.off) continue;
                FloatUp(Note, UiKit.Hex("5B9BD5"), Above(kv.Value, 0.8f) + new Vector2(UnityEngine.Random.Range(-30f, 30f), 0f), 44f, new Vector2(UnityEngine.Random.Range(-50f, 50f), 150f));
            }
        }

        // ---------------------------------------------------------------- little effects

        /// <summary>A point a share of the way up a thing's picture (room units).</summary>
        static Vector2 Above(RectTransform t, float up)
        {
            float h = t.sizeDelta.y * Mathf.Abs(t.localScale.y);
            var at = t.anchoredPosition + new Vector2(0f, h * (up - t.pivot.y));
            if (t.parent != null && t.parent.name == "Soak") at = ((RectTransform)t.parent).anchoredPosition + new Vector2(0f, h * up * 0.5f);
            return at;
        }

        void FloatUp(Sprite s, Color c, Vector2 at, float size, Vector2 drift, float time = 1.5f)
        {
            if (s == null || _fx == null) return;
            var img = UiKit.Image(_fx, "Float", s, c);
            img.raycastTarget = false;
            img.preserveAspect = true;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.sizeDelta = new Vector2(size, size);
            r.anchoredPosition = at;
            r.localScale = Vector3.one * 0.4f;
            r.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(img.gameObject);
            r.DOAnchorPos(at + drift, time).SetEase(Ease.OutSine).SetUpdate(true).SetLink(img.gameObject);
            img.DOFade(0f, time * 0.5f).SetDelay(time * 0.5f).SetUpdate(true).SetLink(img.gameObject).OnComplete(() => Destroy(img.gameObject));
        }

        void Burst(RectTransform over, Sprite s, Color c, int n)
        {
            for (int i = 0; i < n; i++)
                FloatUp(s, c, Above(over, 0.9f) + new Vector2((i - (n - 1) * 0.5f) * 50f, 0f), 54f, new Vector2(UnityEngine.Random.Range(-30f, 30f), 140f));
        }

        IEnumerator Repeat(int times, float every, Action act)
        {
            for (int i = 0; i < times; i++)
            {
                act();
                yield return new WaitForSecondsRealtime(every);
            }
        }

        /// <summary>Something held up in your paws for a moment (a book, a snack you nibble).</summary>
        void Hold(Sprite s, Color c, float seconds, bool munch = false)
        {
            if (s == null || _you == null) return;
            var img = UiKit.Image(_fx, "Held", s, c);
            img.raycastTarget = false;
            img.preserveAspect = true;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.sizeDelta = new Vector2(110f, 110f);
            float h = _you.sizeDelta.y * Mathf.Abs(_you.localScale.y);
            r.anchoredPosition = YouInRoom() + new Vector2((_facingRight ? 1f : -1f) * _you.sizeDelta.x * Mathf.Abs(_you.localScale.x) * 0.42f, h * 0.45f);
            r.localScale = Vector3.zero;
            var seq = DOTween.Sequence().SetUpdate(true).SetLink(img.gameObject);
            seq.Append(r.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
            if (munch)
                for (int i = 0; i < 3; i++)
                {
                    seq.AppendCallback(() => Sfx.Play(SfxKind.Nibble, 0.4f));
                    seq.Append(r.DOPunchScale(new Vector3(-0.15f, -0.15f, 0f), 0.3f, 4, 0.5f));
                    seq.AppendInterval(seconds / 4f);
                }
            else seq.AppendInterval(seconds);
            seq.Append(r.DOScale(0f, 0.2f).SetEase(Ease.InBack));
            seq.OnComplete(() =>
            {
                Destroy(img.gameObject);
                if (_you != null) Burst(_you, IconPainter.Heart, UiKit.Hex("FF8FB0"), 1);
            });
        }

        static Sprite _note, _zed;

        /// <summary>A painted music note (the fonts have no ♪).</summary>
        static Sprite Note => _note != null ? _note : _note = IconPainter.Paint(96, (x, y) =>
            Mathf.Min(Mathf.Min(IconPainter.Disc(x, y, -0.22f, -0.5f, 0.3f), IconPainter.Box(x, y, 0.04f, 0.1f, 0.07f, 0.62f, 0.03f)),
                      IconPainter.Line(x, y, 0.08f, 0.04f, 0.68f, 0.5f, 0.42f, 0.5f, 0.12f)));

        /// <summary>A sleepy "z".</summary>
        static Sprite Zed => _zed != null ? _zed : _zed = IconPainter.Paint(96, (x, y) =>
            IconPainter.Line(x, y, 0.12f, -0.5f, 0.5f, 0.5f, 0.5f, -0.5f, -0.5f, 0.5f, -0.5f));

        // ---------------------------------------------------------------- the storage drawer

        void Drawer()
        {
            var board = UiKit.Card(_chrome, "Storage", UiKit.Hex("8A5A32"), 30, true, true).rectTransform;
            board.anchorMin = Vector2.zero;
            board.anchorMax = new Vector2(1f, 0f);
            board.pivot = new Vector2(0.5f, 0f);
            board.sizeDelta = new Vector2(-48f, DrawerH);
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
            if (_drawerContent == null) return;
            ClearChildren(_drawerContent);
            var none = _drawer.Find("None");
            if (none != null) Destroy(none.gameObject);
            var items = HomeLog.Storage();
            if (items.Count == 0)
            {
                var t = UiKit.Text(_drawer, "None", "Nothing in storage. Pieces you buy wait here.", 26f, new Color(1f, 1f, 1f, 0.8f), false, TextAlignmentOptions.Center);
                UiKit.Stretch(t.rectTransform, 20f, 10f, 20f, 50f);
            }
            float tile = DrawerH - 70f;
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

        /// <summary>Takes a piece out of storage and stands it on the floor of this room.</summary>
        void PlaceNew(FurnitureDef def, Vector2 at)
        {
            var p = HomeLog.PlaceFromStorage(def.Id, at.x, at.y, _room);
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
            var empty = _chrome.Find("Empty");
            if (empty != null) Destroy(empty.gameObject);
        }

        /// <summary>Somewhere in view on the floor, for a piece tapped out of the drawer.</summary>
        Vector2 InView()
        {
            var view = _body.rect.size;
            var middle = _cam + new Vector2(view.x * 0.5f, DrawerH + (view.y - DrawerH) * 0.35f);
            var f = ToFloorPoint(middle);
            return new Vector2(Mathf.Clamp01(f.x + UnityEngine.Random.Range(-0.04f, 0.04f)), Mathf.Clamp(f.y, 0.25f, 0.6f));
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
                if (!_dragged) _screen.PlaceNew(_def, _screen.InView());
                _dragged = false;
            }

            public void OnBeginDrag(PointerEventData e)
            {
                _dragged = true;
                _scrolling = Mathf.Abs(e.delta.x) > Mathf.Abs(e.delta.y);
                if (_scrolling) { _screen._drawerScroll.OnBeginDrag(e); return; }
                var ghost = UiKit.Image(_screen._body, "Ghost", _def.Art, new Color(1f, 1f, 1f, 0.85f));
                ghost.preserveAspect = true;
                ghost.raycastTarget = false;
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
