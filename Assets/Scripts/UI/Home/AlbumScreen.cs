using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Your photo album, opened from the album in your burrow (the one piece every burrow starts
    /// with). A book of pages, two photos to a page, each tucked in with its tag written under it
    /// ("me and Juniper"). Flip through with the arrows; add a blank page when you run out; tap an
    /// empty corner to stick in a photo from your camera roll (PhotoMode), or a photo to change its
    /// tag or take it out again.
    /// </summary>
    public class AlbumScreen : UiScreen
    {
        public static AlbumScreen Instance { get; private set; }

        protected override string Title => _friendName != null ? string.Format("{0}'s Photo Album", _friendName) : "Photo Album";

        // A friend's album (in their burrow, on a visit): theirs to look through, not to change.
        string _friendName;
        PhotoLog.Shared _friendAlbum;
        readonly System.Collections.Generic.Dictionary<string, Sprite> _friendPhotos = new();
        bool Mine => _friendName == null;
        System.Collections.Generic.List<HomeLog.AlbumPage> Pages => Mine ? HomeLog.Album : _friendAlbum.pages;

        /// <summary>A friend's album, from what they last saved (RealFriends.FetchAlbum).</summary>
        public static async void ShowFriend(FriendProfile who)
        {
            if (Instance == null || who == null) return;
            var shared = who.IsReal ? PhotoLog.ReadShared(await RealFriends.FetchAlbum(who)) : null;
            if (Instance == null) return;
            if (shared == null || shared.photos.Count == 0 || shared.pages.Count == 0)
            {
                Hints.Answer(Loc.F("{0}'s album is still empty. Maybe next time!", who.name));
                return;
            }
            Instance._friendName = who.name;
            Instance._friendAlbum = shared;
            Instance._at = 0;
            Instance.Open();
        }

        Sprite FriendPhoto(string id, out string tag)
        {
            tag = "";
            if (string.IsNullOrEmpty(id) || _friendAlbum == null) return null;
            var photo = _friendAlbum.photos.Find(p => p.id == id);
            if (photo == null) return null;
            tag = photo.tag ?? "";
            if (!_friendPhotos.TryGetValue(id, out var s)) _friendPhotos[id] = s = PhotoLog.SpriteOf(photo);
            return s;
        }
        protected override Sprite TitleIcon => PhotoMode.Icon;
        protected override Color AccentColour => UiKit.Hex("C98B5B");
        protected override float MaxWidth => 1100f;

        RectTransform _body, _page;
        int _at;
        bool _wide;

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
            _at = Mathf.Clamp(_at, 0, Pages.Count - 1);
            Refresh();
        }

        protected override void Update()
        {
            base.Update();
            if (IsOpen && _body != null && Wide() != _wide) Refresh();
        }

        bool Wide() => _body.rect.width > _body.rect.height * 1.05f;

        void Refresh()
        {
            ClearChildren(_body);
            _wide = Wide();
            var pages = Pages;
            _at = Mathf.Clamp(_at, 0, pages.Count - 1);

            // The page: warm paper, two corners for photos.
            _page = UiKit.Card(_body, "Page", UiKit.Hex("F7EEDC"), 28).rectTransform;
            UiKit.Stretch(_page, 10f, 120f, 10f, 10f);
            var layout = _wide ? (HorizontalOrVerticalLayoutGroup)_page.gameObject.AddComponent<HorizontalLayoutGroup>() : _page.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 36, 36);
            layout.spacing = 36f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            var page = pages[_at];
            Corner(page, true);
            Corner(page, false);

            // Along the bottom: back a page, where you are, on a page, and a new one.
            var nav = UiKit.Rect("Nav", _body);
            nav.anchorMin = new Vector2(0f, 0f);
            nav.anchorMax = new Vector2(1f, 0f);
            nav.pivot = new Vector2(0.5f, 0f);
            nav.sizeDelta = new Vector2(-20f, 100f);
            nav.anchoredPosition = new Vector2(0f, 4f);
            var h = nav.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            var prev = UiKit.Button(nav, "Prev", null, UiKit.ButtonStyle.Secondary, () => Flip(-1), 30f, IconPainter.Chevron);
            Width(prev, 110f);
            var flipIcon = prev.transform.Find("Content/Icon");
            if (flipIcon != null) flipIcon.localScale = new Vector3(-1f, 1f, 1f);
            prev.interactable = _at > 0;
            var where = UiKit.Text(nav, "Where", string.Format("Page {0} of {1}", _at + 1, pages.Count), 30f, UiKit.Ink, true, TextAlignmentOptions.Center);
            where.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var next = UiKit.Button(nav, "Next", null, UiKit.ButtonStyle.Secondary, () => Flip(1), 30f, IconPainter.Chevron);
            Width(next, 110f);
            next.interactable = _at < pages.Count - 1;
            if (!Mine) { UiKit.ApplyFonts(_body); return; }   // a friend's: theirs to add to
            var add = UiKit.Button(nav, "AddPage", "New page", UiKit.ButtonStyle.Spring, () =>
            {
                HomeLog.AddAlbumPage();
                _at = HomeLog.Album.Count - 1;
                Sfx.Play(SfxKind.Rustle, 0.5f);
                Refresh();
                UiKit.PopIn(_page, 0.9f);
            }, 28f);
            Width(add, 240f);
            UiKit.ApplyFonts(_body);
        }

        static void Width(Button b, float w)
        {
            var le = b.transform.parent.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = w;
        }

        /// <summary>To the next or the previous page, with a turn of the page.</summary>
        void Flip(int by)
        {
            int to = Mathf.Clamp(_at + by, 0, Pages.Count - 1);
            if (to == _at) return;
            Sfx.Play(SfxKind.Rustle, 0.5f);
            _page.DOKill();
            _page.DOScaleX(0f, 0.12f).SetEase(Ease.InQuad).SetUpdate(true).OnComplete(() =>
            {
                _at = to;
                Refresh();
                _page.localScale = new Vector3(0f, 1f, 1f);
                _page.DOScaleX(1f, 0.16f).SetEase(Ease.OutQuad).SetUpdate(true);
            });
        }

        /// <summary>One corner of the page: a photo with its tag, tucked in at a slight slant, or room for one.</summary>
        void Corner(HomeLog.AlbumPage page, bool first)
        {
            var slot = UiKit.Rect(first ? "A" : "B", _page);
            if (!Mine)
            {
                // A friend's: their photo and tag to look at, or just the page.
                var picture = FriendPhoto(first ? page.a : page.b, out var words);
                if (picture != null) Print(slot, first, picture, words);
                return;
            }
            var photo = PhotoLog.Find(first ? page.a : page.b);
            if (photo == null)
            {
                var empty = UiKit.Image(slot, "Empty", UiKit.Rounded(24), new Color(0.55f, 0.4f, 0.25f, 0.12f), true);
                UiKit.Stretch(empty.rectTransform);
                var plus = UiKit.Text(empty.transform, "Add", "+ Add a photo", 32f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
                UiKit.Stretch(plus.rectTransform);
                var b = empty.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Pick(page, first));
                empty.gameObject.AddComponent<PressSquash>();
                return;
            }
            var print = Print(slot, first, PhotoLog.SpriteOf(photo), photo.tag);
            var button = print.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Options(page, first, photo));
            print.gameObject.AddComponent<PressSquash>();
        }

        /// <summary>A white-bordered print tucked in at a slant, its tag on the border below.</summary>
        static RectTransform Print(RectTransform slot, bool first, Sprite picture, string words)
        {
            var print = UiKit.Image(slot, "Print", UiKit.Rounded(10), Color.white, true).rectTransform;
            print.anchorMin = print.anchorMax = new Vector2(0.5f, 0.5f);
            print.gameObject.AddComponent<FitSquare>();
            print.localRotation = Quaternion.Euler(0f, 0f, first ? -3f : 2.5f);
            UiKit.AddShadow(print, 14f, 6f, 0.25f);
            var pic = UiKit.Image(print, "Pic", picture, Color.white);
            pic.preserveAspect = true;
            pic.rectTransform.anchorMin = Vector2.zero;
            pic.rectTransform.anchorMax = Vector2.one;
            pic.rectTransform.offsetMin = new Vector2(18f, 70f);
            pic.rectTransform.offsetMax = new Vector2(-18f, -18f);
            var tag = UiKit.Text(print, "Tag", words ?? "", 30f, UiKit.Ink, true, TextAlignmentOptions.Center);
            tag.rectTransform.anchorMin = Vector2.zero;
            tag.rectTransform.anchorMax = new Vector2(1f, 0f);
            tag.rectTransform.pivot = new Vector2(0.5f, 0f);
            tag.rectTransform.sizeDelta = new Vector2(-24f, 66f);
            tag.textWrappingMode = TextWrappingModes.NoWrap;
            tag.enableAutoSizing = true;
            tag.fontSizeMin = 18f;
            tag.fontSizeMax = 30f;
            return print;
        }

        /// <summary>Keeps a print square and as big as its corner allows, with room for the tag.</summary>
        class FitSquare : MonoBehaviour
        {
            void LateUpdate()
            {
                var self = (RectTransform)transform;
                var box = ((RectTransform)transform.parent).rect;
                float side = Mathf.Max(80f, Mathf.Min(box.width - 20f, box.height - 20f));
                var size = new Vector2(side * 0.9f, side);
                if (self.sizeDelta != size) self.sizeDelta = size;
            }
        }

        // ---------------------------------------------------------------- sticking photos in

        RectTransform _overlay;

        void Pick(HomeLog.AlbumPage page, bool first)
        {
            CloseOverlay();
            var all = PhotoLog.All;
            if (all.Count == 0)
            {
                Hints.Answer("No photos yet! Tap Photo in your burrow, or the camera on your island, and snap something.");
                return;
            }
            _overlay = UiKit.Card(_body, "Picker", UiKit.Surface, 30).rectTransform;
            UiKit.Stretch(_overlay, -10f, -10f, -10f, -10f);
            var title = UiKit.Text(_overlay, "Title", "Pick a photo", 36f, UiKit.Ink, true, TextAlignmentOptions.TopLeft);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = Vector2.one;
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(-240f, 60f);
            title.rectTransform.anchoredPosition = new Vector2(-100f, -24f);
            var back = UiKit.Button(_overlay, "Back", "Back", UiKit.ButtonStyle.Secondary, CloseOverlay, 28f);
            var br = (RectTransform)back.transform.parent;
            br.anchorMin = br.anchorMax = br.pivot = new Vector2(1f, 1f);
            br.anchoredPosition = new Vector2(-20f, -18f);
            br.sizeDelta = new Vector2(170f, 76f);
            PhotoMode.PhotoGrid(_overlay, all, p =>
            {
                if (first) page.a = p.id; else page.b = p.id;
                HomeLog.Save();
                Sfx.Play(SfxKind.Plop, 0.6f);
                CloseOverlay();
                Refresh();
            });
            UiKit.ApplyFonts(_overlay);
        }

        /// <summary>A photo on the page tapped: write a new tag, or take it out.</summary>
        void Options(HomeLog.AlbumPage page, bool first, PhotoLog.Photo photo)
        {
            CloseOverlay();
            _overlay = UiKit.Image(_body, "Options", null, new Color(0f, 0f, 0f, 0.35f), true).rectTransform;
            UiKit.Stretch(_overlay, -10f, -10f, -10f, -10f);
            var card = UiKit.Card(_overlay, "Card", UiKit.Surface, 30).rectTransform;
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(Mathf.Min(_body.rect.width - 60f, 760f), 330f);
            var field = UiKit.Input(card, "Tag", "Add a tag, like \"me and a friend\"", false, 84f);
            var fr = (RectTransform)field.transform;
            fr.anchorMin = new Vector2(0f, 1f);
            fr.anchorMax = Vector2.one;
            fr.pivot = new Vector2(0.5f, 1f);
            fr.sizeDelta = new Vector2(-60f, 84f);
            fr.anchoredPosition = new Vector2(0f, -40f);
            field.characterLimit = 40;
            field.text = photo.tag ?? "";
            var row = UiKit.Rect("Buttons", card);
            row.anchorMin = Vector2.zero;
            row.anchorMax = new Vector2(1f, 0f);
            row.pivot = new Vector2(0.5f, 0f);
            row.sizeDelta = new Vector2(-60f, 96f);
            row.anchoredPosition = new Vector2(0f, 30f);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;
            UiKit.Button(row, "TakeOut", "Take it out", UiKit.ButtonStyle.Secondary, () =>
            {
                if (first) page.a = ""; else page.b = "";
                HomeLog.Save();
                CloseOverlay();
                Refresh();
            }, 28f);
            UiKit.Button(row, "Save", "Save", UiKit.ButtonStyle.Primary, () =>
            {
                PhotoLog.SetTag(photo, field.text);
                CloseOverlay();
                Refresh();
            }, 28f);
            UiKit.ApplyFonts(_overlay);
            UiKit.PopIn(card, 0.9f);
        }

        void CloseOverlay()
        {
            if (_overlay != null) Destroy(_overlay.gameObject);
            _overlay = null;
        }

        protected override void OnClose()
        {
            CloseOverlay();
            if (Mine) return;
            foreach (var s in _friendPhotos.Values) if (s != null) { Destroy(s.texture); Destroy(s); }
            _friendPhotos.Clear();
            _friendName = null;
            _friendAlbum = null;
            _at = 0;
        }
    }
}
