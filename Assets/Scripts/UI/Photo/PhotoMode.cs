using System.Collections;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The game's camera. Tap the camera button (on the island, or in your burrow) and the buttons
    /// get out of the way: a square viewfinder, a shutter, and your camera roll in the corner. You
    /// can still walk about and look round to frame the shot. Snap, and the photo is kept
    /// (PhotoLog) with a tag you can write on it ("me and Juniper"), ready for your photo album.
    ///
    /// What's in the photo is what you see in the viewfinder: the island (or the burrow) with
    /// everyone on it, name tags and all, never the buttons.
    /// </summary>
    public class PhotoMode : MonoBehaviour
    {
        static PhotoMode _instance;

        public static bool IsOpen => _instance != null && _instance._root != null && _instance._root.activeSelf;

        Canvas _canvas;
        GameObject _root;
        RectTransform _frame, _overlay;
        Image _lastThumb;
        bool _snapping;

        static Sprite _icon;

        /// <summary>A little painted camera.</summary>
        public static Sprite Icon => _icon != null ? _icon : _icon = IconPainter.Paint(128, (x, y) =>
        {
            float body = IconPainter.Box(x, y, 0f, -0.1f, 0.8f, 0.52f, 0.16f);
            float top = IconPainter.Box(x, y, -0.22f, 0.44f, 0.26f, 0.12f, 0.06f);
            float lens = Mathf.Abs(IconPainter.Disc(x, y, 0.02f, -0.1f, 0.3f)) - 0.08f;
            return Mathf.Max(Mathf.Min(body, top), -lens);
        });

        // ---------------------------------------------------------------- the button on the HUD

        /// <summary>The camera button, bottom right, over the emoji and chat buttons when they're there.</summary>
        public static void CreateButton(Transform hud)
        {
            var root = UiKit.Rect("CameraButton", hud);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(1f, 0f);
            root.sizeDelta = new Vector2(120f, 120f);
            UiKit.RoundButton(root, "Camera", Icon, null, UiKit.Hex("4F5B6E"), Open, 112f);
            root.gameObject.AddComponent<ButtonPlace>();
        }

        /// <summary>Keeps the button stacked clear of the emoji and chat buttons, and away while it doesn't apply.</summary>
        class ButtonPlace : MonoBehaviour
        {
            RectTransform _self;
            Transform _emoji, _chat;
            GameObject _button;

            void Start()
            {
                _self = (RectTransform)transform;
                _button = transform.GetChild(0).gameObject;
            }

            void Update()
            {
                bool fishing = Fishing.Instance != null && Fishing.Instance.Mode;
                bool show = !UiScreen.AnyOpen && !IsOpen && !Attention.TutorialRunning && !fishing && !Cutscene.Playing;
                if (_button.activeSelf != show) _button.SetActive(show);
                if (!show) return;
                if (_emoji == null) _emoji = transform.parent.Find("EmojiBar/Emoji");
                if (_chat == null) _chat = FindChat();
                bool emoji = _emoji != null && _emoji.gameObject.activeInHierarchy;
                bool chat = _chat != null && _chat.gameObject.activeInHierarchy;
                const float step = 132f;
                if (Core.ScreenMetrics.IsPortrait)
                    _self.anchoredPosition = new Vector2(-20f, 250f + (emoji ? step : 0f) + (chat ? step : 0f));
                else
                    _self.anchoredPosition = new Vector2(-20f - (emoji ? step : 0f) - (chat ? step : 0f), 250f);
            }

            Transform FindChat()
            {
                foreach (Transform t in transform.parent)
                {
                    var b = t.Find("ChatButton");
                    if (b != null) return b;
                }
                return null;
            }
        }

        // ---------------------------------------------------------------- opening and closing

        public static void Open()
        {
            if (_instance == null)
            {
                var go = new GameObject("PhotoMode", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                _instance = go.AddComponent<PhotoMode>();
                _instance.Build();
            }
            _instance.Show();
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        void Build()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 640;   // over the HUD and screens, under the loading curtain
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _root = UiKit.Rect("Root", transform).gameObject;
            UiKit.Stretch((RectTransform)_root.transform);

            // The viewfinder: four corner brackets round a square, the rest a touch dimmer.
            _frame = UiKit.Rect("Viewfinder", _root.transform);
            _frame.anchorMin = _frame.anchorMax = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < 4; i++)
            {
                var dim = UiKit.Image(_root.transform, "Dim" + i, null, new Color(0f, 0f, 0f, 0.28f));
                dim.raycastTarget = false;
            }
            for (int i = 0; i < 4; i++)
            {
                var corner = UiKit.Rect("Corner" + i, _frame);
                var c = new Vector2(i % 2, i / 2);
                corner.anchorMin = corner.anchorMax = corner.pivot = c;
                corner.sizeDelta = new Vector2(90f, 90f);
                var a = UiKit.Image(corner, "A", null, Color.white);
                a.raycastTarget = false;
                a.rectTransform.anchorMin = new Vector2(0f, c.y);
                a.rectTransform.anchorMax = new Vector2(1f, c.y);
                a.rectTransform.pivot = new Vector2(0.5f, c.y);
                a.rectTransform.sizeDelta = new Vector2(0f, 10f);
                var b = UiKit.Image(corner, "B", null, Color.white);
                b.raycastTarget = false;
                b.rectTransform.anchorMin = new Vector2(c.x, 0f);
                b.rectTransform.anchorMax = new Vector2(c.x, 1f);
                b.rectTransform.pivot = new Vector2(c.x, 0.5f);
                b.rectTransform.sizeDelta = new Vector2(10f, 0f);
            }

            // The shutter, the way out, the roll.
            var shutter = UiKit.Image(_root.transform, "Shutter", UiKit.Circle, Color.white, true);
            UiKit.Place(shutter.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(170f, 170f));
            UiKit.AddShadow(shutter.rectTransform, 16f, 6f, 0.35f);
            var ring = UiKit.Image(shutter.transform, "Ring", IconPainter.Ring, new Color(0.3f, 0.3f, 0.35f, 0.5f));
            ring.raycastTarget = false;
            UiKit.Stretch(ring.rectTransform, 14f, 14f, 14f, 14f);
            var sb = shutter.gameObject.AddComponent<Button>();
            sb.transition = Selectable.Transition.None;
            sb.onClick.AddListener(Snap);
            shutter.gameObject.AddComponent<PressSquash>();

            var close = UiKit.Button(_root.transform, "Close", "Done", UiKit.ButtonStyle.Secondary, Close, 30f, IconPainter.Close);
            var cr = (RectTransform)close.transform.parent;
            cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(1f, 1f);
            cr.anchoredPosition = new Vector2(-30f, -40f);
            cr.sizeDelta = new Vector2(220f, 88f);

            var thumb = UiKit.Image(_root.transform, "Roll", UiKit.Rounded(24), Color.white, true);
            UiKit.Place(thumb.rectTransform, new Vector2(0f, 0f), new Vector2(150f, 150f), new Vector2(130f, 130f));
            thumb.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            UiKit.AddShadow(thumb.rectTransform, 12f, 5f, 0.3f);
            _lastThumb = UiKit.Image(thumb.transform, "Last", null, new Color(0.85f, 0.85f, 0.85f));
            _lastThumb.preserveAspect = true;
            UiKit.Stretch(_lastThumb.rectTransform, 8f, 8f, 8f, 8f);
            var tb = thumb.gameObject.AddComponent<Button>();
            tb.transition = Selectable.Transition.None;
            tb.onClick.AddListener(ShowRoll);
            thumb.gameObject.AddComponent<PressSquash>();

            UiKit.ApplyFonts(_root.transform);
            _root.SetActive(false);
        }

        void Show()
        {
            _root.SetActive(true);
            HideTheRest(true);
            Fit();
            RefreshThumb();
            Sfx.Play(SfxKind.Tap, 0.5f);
            if (PhotoLog.Full) Hints.Answer(string.Format("Your camera roll is full ({0} photos). Throw a few away to take more.", PhotoLog.Max));
        }

        public static void Close()
        {
            if (_instance == null || !IsOpen) return;
            _instance.CloseOverlay();
            _instance._root.SetActive(false);
            _instance.HideTheRest(false);
        }

        /// <summary>The HUD (and the burrow's buttons) away while you take photos.</summary>
        void HideTheRest(bool hide)
        {
            MobileHud.HideChrome(hide);
            if (BurrowScreen.Instance != null) BurrowScreen.Instance.HideChrome(hide);
        }

        void Update()
        {
            if (!IsOpen) return;
            Fit();
            if (Input.GetKeyDown(KeyCode.Escape)) { if (_overlay != null) CloseOverlay(); else Close(); }
        }

        /// <summary>The square, as big as fits with room for the shutter, and the dim around it.</summary>
        void Fit()
        {
            var area = ((RectTransform)_root.transform).rect;
            float side = Mathf.Min(area.width - 60f, area.height - 520f, 1100f);
            side = Mathf.Max(300f, side);
            _frame.sizeDelta = new Vector2(side, side);
            _frame.anchoredPosition = new Vector2(0f, 40f);
            float half = side * 0.5f;
            // top, bottom, left, right
            SetDim(0, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 40f + half), Vector2.zero);
            SetDim(1, new Vector2(0f, 0f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 40f - half));
            SetDim(2, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f - half), new Vector2(-half, 40f + half));
            SetDim(3, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(half, 40f - half), new Vector2(0f, 40f + half));
        }

        void SetDim(int i, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var r = (RectTransform)_root.transform.Find("Dim" + i);
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = offMin;
            r.offsetMax = offMax;
        }

        void RefreshThumb()
        {
            var all = PhotoLog.All;
            var sprite = all.Count > 0 ? PhotoLog.SpriteOf(all[0]) : null;
            _lastThumb.sprite = sprite != null ? sprite : Icon;
            _lastThumb.color = sprite != null ? Color.white : new Color(0.55f, 0.58f, 0.65f);
        }

        // ---------------------------------------------------------------- taking a photo

        void Snap()
        {
            if (_snapping || _overlay != null) return;
            if (PhotoLog.Full) { Hints.Answer(string.Format("Your camera roll is full ({0} photos). Throw a few away to take more.", PhotoLog.Max)); return; }
            StartCoroutine(Snapping());
        }

        IEnumerator Snapping()
        {
            _snapping = true;
            // The square in screen pixels, then everything of ours out of the way for one frame.
            var corners = new Vector3[4];
            _frame.GetWorldCorners(corners);
            var rect = new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
            _canvas.enabled = false;
            yield return new WaitForEndOfFrame();
            var screen = ScreenCapture.CaptureScreenshotAsTexture();
            _canvas.enabled = true;
            var photo = Crop(screen, rect);
            Destroy(screen);
            Sfx.Play(SfxKind.Pop, 0.7f);
            Flash();
            var kept = PhotoLog.Add(photo);
            _snapping = false;
            if (kept == null) { Hints.Answer("Couldn't keep that photo. Is the phone's storage full?"); yield break; }
            RefreshThumb();
            Review(kept, fresh: true);
        }

        /// <summary>The viewfinder's square of the screen, made small (PhotoLog.Size).</summary>
        static Texture2D Crop(Texture2D screen, Rect px)
        {
            float w = screen.width, h = screen.height;
            px.xMin = Mathf.Clamp(px.xMin, 0f, w);
            px.xMax = Mathf.Clamp(px.xMax, 0f, w);
            px.yMin = Mathf.Clamp(px.yMin, 0f, h);
            px.yMax = Mathf.Clamp(px.yMax, 0f, h);
            int size = PhotoLog.Size;
            // Linear in, linear out: the screen's colours copied as they are (an sRGB target would
            // brighten them a second time and the photo comes out washed out).
            var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(screen, rt, new Vector2(px.width / w, px.height / h), new Vector2(px.xMin / w, px.yMin / h));
            var was = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex.Apply();
            RenderTexture.active = was;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }

        void Flash()
        {
            var flash = UiKit.Image(_root.transform, "Flash", null, Color.white);
            flash.raycastTarget = false;
            UiKit.Stretch(flash.rectTransform, -200f, -200f, -200f, -200f);
            flash.DOFade(0f, 0.35f).SetUpdate(true).SetLink(flash.gameObject).OnComplete(() => Destroy(flash.gameObject));
        }

        // ---------------------------------------------------------------- a photo up close: tag it, keep it

        /// <summary>A photo up close, with its tag to write. Just taken: "Keep" or "Throw away".</summary>
        public void Review(PhotoLog.Photo photo, bool fresh)
        {
            CloseOverlay();
            _overlay = Scrim();
            var card = UiKit.Card(_overlay, "Photo", UiKit.Surface, 36).rectTransform;
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            var area = ((RectTransform)_root.transform).rect;
            float side = Mathf.Min(area.width - 140f, area.height - 640f, 760f);
            card.sizeDelta = new Vector2(side + 60f, side + 330f);
            card.anchoredPosition = new Vector2(0f, 60f);
            var pic = UiKit.Image(card, "Pic", PhotoLog.SpriteOf(photo), Color.white);
            pic.preserveAspect = true;
            UiKit.Place(pic.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(side, side));
            pic.rectTransform.pivot = new Vector2(0.5f, 1f);

            var field = UiKit.Input(card, "Tag", "Add a tag, like \"me and a friend\"", false, 84f);
            var fr = (RectTransform)field.transform;
            fr.anchorMin = fr.anchorMax = new Vector2(0.5f, 0f);
            fr.pivot = new Vector2(0.5f, 0f);
            fr.sizeDelta = new Vector2(side, 84f);
            fr.anchoredPosition = new Vector2(0f, 140f);
            field.characterLimit = 40;
            field.text = photo.tag ?? "";
            field.onEndEdit.AddListener(t => PhotoLog.SetTag(photo, t));

            var row = UiKit.Rect("Buttons", card);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0f);
            row.pivot = new Vector2(0.5f, 0f);
            row.sizeDelta = new Vector2(side, 96f);
            row.anchoredPosition = new Vector2(0f, 28f);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;
            UiKit.Button(row, "Delete", "Throw away", UiKit.ButtonStyle.Secondary, () =>
            {
                PhotoLog.Delete(photo);
                RefreshThumb();
                CloseOverlay();
                if (!fresh) ShowRoll();
            }, 30f);
            UiKit.Button(row, "Keep", fresh ? "Keep" : "Done", UiKit.ButtonStyle.Primary, () =>
            {
                PhotoLog.SetTag(photo, field.text);
                CloseOverlay();
                if (fresh) Hints.Show("Photo kept! Stick it in the photo album in your burrow.");
                else ShowRoll();
            }, 30f);
            UiKit.ApplyFonts(_overlay);
            UiKit.PopIn(card, 0.9f);
        }

        // ---------------------------------------------------------------- the camera roll

        void ShowRoll()
        {
            CloseOverlay();
            _overlay = Scrim();
            // As tall as its rows of photos (it scrolls once they fill the screen).
            float across = Mathf.Min(((RectTransform)_root.transform).rect.width - 60f, 1000f);
            int columns = Mathf.Max(1, Mathf.FloorToInt((across - 48f + 16f) / 246f));
            int rows = Mathf.Max(1, Mathf.CeilToInt(PhotoLog.All.Count / (float)columns));
            var card = Sheet(_overlay, "Your photos", string.Format("{0} / {1}", PhotoLog.All.Count, PhotoLog.Max), CloseOverlay, 130f + rows * 298f + 100f);
            var all = PhotoLog.All;
            if (all.Count == 0)
            {
                var none = UiKit.Text(card, "None", "No photos yet. Frame something lovely and tap the big white button!", 30f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
                UiKit.Stretch(none.rectTransform, 40f, 40f, 40f, 130f);
                UiKit.ApplyFonts(_overlay);
                return;
            }
            PhotoGrid(card, all, p => Review(p, fresh: false));
            var tip = UiKit.Text(card, "Tip", "Tap a photo to write its tag, or to throw it away.", 24f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            tip.rectTransform.anchorMin = Vector2.zero;
            tip.rectTransform.anchorMax = new Vector2(1f, 0f);
            tip.rectTransform.pivot = new Vector2(0.5f, 0f);
            tip.rectTransform.sizeDelta = new Vector2(-40f, 56f);
            tip.rectTransform.anchoredPosition = new Vector2(0f, 14f);
            UiKit.ApplyFonts(_overlay);
        }

        /// <summary>
        /// A sheet in the middle of the screen, like the game's other windows: a coloured header
        /// with its title (and a word beside it), a way back, and room below for what's in it.
        /// </summary>
        public static RectTransform Sheet(RectTransform parent, string title, string aside, System.Action back, float wants = 1500f)
        {
            var area = parent.rect;
            var card = UiKit.Card(parent, "Sheet", UiKit.Surface, 36).rectTransform;
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(Mathf.Min(area.width - 60f, 1000f), Mathf.Clamp(wants, 520f, area.height - 160f));
            var header = UiKit.Image(card, "Header", UiKit.Rounded(36), UiKit.Hex("4F5B6E")).rectTransform;
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = Vector2.one;
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(0f, 110f);
            var icon = UiKit.Image(header, "Icon", Icon, Color.white);
            icon.preserveAspect = true;
            UiKit.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(54f, 0f), new Vector2(58f, 58f));
            var t = UiKit.Text(header, "Title", title, 40f, Color.white, true, TextAlignmentOptions.MidlineLeft);
            UiKit.Stretch(t.rectTransform, 112f, 0f, 220f, 0f);
            if (!string.IsNullOrEmpty(aside))
            {
                var a = UiKit.Text(header, "Aside", aside, 26f, new Color(1f, 1f, 1f, 0.7f), false, TextAlignmentOptions.MidlineRight);
                UiKit.Stretch(a.rectTransform, 0f, 0f, 150f, 0f);
            }
            var close = UiKit.Image(header, "Back", UiKit.Circle, new Color(1f, 1f, 1f, 0.22f), true);
            UiKit.Place(close.rectTransform, new Vector2(1f, 0.5f), new Vector2(-62f, 0f), new Vector2(76f, 76f));
            var x = UiKit.Image(close.transform, "X", IconPainter.Close, Color.white);
            x.raycastTarget = false;
            UiKit.Stretch(x.rectTransform, 20f, 20f, 20f, 20f);
            var b = close.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => back());
            close.gameObject.AddComponent<PressSquash>();
            UiKit.PopIn(card, 0.92f);
            return card;
        }

        /// <summary>A scrolling grid of photos with their tags (the roll here, and the album's picker).</summary>
        public static void PhotoGrid(RectTransform card, System.Collections.Generic.IReadOnlyList<PhotoLog.Photo> photos, System.Action<PhotoLog.Photo> pick)
        {
            var view = UiKit.Rect("View", card);
            UiKit.Stretch(view, 24f, 80f, 24f, 130f);   // under the header, over the tip
            view.gameObject.AddComponent<RectMask2D>();
            var content = UiKit.Rect("Grid", view);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            float cell = 230f;
            grid.cellSize = new Vector2(cell, cell + 50f);
            grid.spacing = new Vector2(16f, 18f);
            grid.childAlignment = TextAnchor.UpperCenter;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            foreach (var photo in photos)
            {
                var p = photo;
                var tile = UiKit.Image(content, p.id, UiKit.Rounded(18), Color.white, true).rectTransform;
                UiKit.AddShadow(tile, 10f, 4f, 0.2f);
                var pic = UiKit.Image(tile, "Pic", PhotoLog.SpriteOf(p), Color.white);
                pic.preserveAspect = true;
                pic.rectTransform.anchorMin = new Vector2(0f, 0f);
                pic.rectTransform.anchorMax = Vector2.one;
                pic.rectTransform.offsetMin = new Vector2(10f, 50f);
                pic.rectTransform.offsetMax = new Vector2(-10f, -10f);
                bool tagged = !string.IsNullOrEmpty(p.tag);
                var tag = UiKit.Text(tile, "Tag", tagged ? p.tag : "No tag yet", 22f, tagged ? UiKit.Ink : UiKit.InkSoft, tagged, TextAlignmentOptions.Center);
                if (!tagged) tag.fontStyle = FontStyles.Italic;
                tag.rectTransform.anchorMin = Vector2.zero;
                tag.rectTransform.anchorMax = new Vector2(1f, 0f);
                tag.rectTransform.pivot = new Vector2(0.5f, 0f);
                tag.rectTransform.sizeDelta = new Vector2(-12f, 46f);
                tag.textWrappingMode = TextWrappingModes.NoWrap;
                tag.overflowMode = TextOverflowModes.Ellipsis;
                var b = tile.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => pick(p));
                tile.gameObject.AddComponent<PressSquash>();
            }
            // As many across as fit.
            float width = Mathf.Max(cell, card.rect.width - 48f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, Mathf.FloorToInt((width + 16f) / (cell + 16f)));
        }

        RectTransform Scrim()
        {
            var scrim = UiKit.Image(_root.transform, "Overlay", null, UiKit.Scrim, true).rectTransform;
            UiKit.Stretch(scrim);   // exactly the screen: what goes in it is placed against it
            return scrim;
        }

        void CloseOverlay()
        {
            if (_overlay != null) Destroy(_overlay.gameObject);
            _overlay = null;
        }
    }
}
