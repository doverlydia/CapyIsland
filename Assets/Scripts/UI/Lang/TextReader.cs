using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Tap to read: with Easy Reading on, tapping any text opens it big, in a card with a
    /// scroll bar, over a paused game. Text on a button belongs to the button, so there it is a
    /// press-and-hold instead — a tap still presses the button.
    ///
    /// Lives on its own canvas above everything, cutscenes included, and outlives scene loads.
    /// </summary>
    public class TextReader : MonoBehaviour
    {
        const float HoldSeconds = 0.55f;
        const float SlopPixels = 22f;

        static TextReader _instance;

        public static bool IsOpen => _instance != null && _instance._root != null && _instance._root.activeSelf;

        RectTransform _canvasRect;
        GameObject _root;
        TextMeshProUGUI _body;
        ScrollRect _scroll;
        float _timeScaleBefore = 1f;

        // The press being watched.
        bool _down;
        Vector2 _downAt;
        float _downTime;
        TMP_Text _downText;
        bool _downOnButton;
        bool _fired;
        int _openedFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("[TextReader]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<TextReader>();
        }

        /// <summary>Opens the reader on any text, from code.</summary>
        public static void Open(string text)
        {
            if (_instance == null || string.IsNullOrWhiteSpace(text)) return;
            _instance.Show(text);
        }

        void Update()
        {
            if (IsOpen)
            {
                Fit();   // a new canvas has no size until it has been laid out once; phones rotate
                if (Input.GetKeyDown(KeyCode.Escape)) Close();
                return;
            }
            if (!Accessibility.TapToRead) { _down = false; return; }

            GetPointer(out var pos, out bool held, out bool began);
            if (began)
            {
                _down = true;
                _fired = false;
                _downAt = pos;
                _downTime = Time.unscaledTime;
                _downText = TextUnder(pos, out _downOnButton);
                return;
            }
            if (!_down) return;

            if (held)
            {
                if ((pos - _downAt).sqrMagnitude > SlopPixels * SlopPixels) { _downText = null; return; }
                // Press and hold opens anything, buttons included.
                if (!_fired && _downText != null && Time.unscaledTime - _downTime >= HoldSeconds)
                {
                    _fired = true;
                    Show(_downText.text);
                }
                return;
            }

            // Released: a plain tap opens text that is not part of a button.
            _down = false;
            if (_fired || _downText == null || _downOnButton) return;
            if ((pos - _downAt).sqrMagnitude > SlopPixels * SlopPixels) return;
            if (Time.unscaledTime - _downTime > HoldSeconds) return;
            var t = _downText;
            _downText = null;
            Show(t.text);
        }

        static void GetPointer(out Vector2 pos, out bool held, out bool began)
        {
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                pos = t.position;
                began = t.phase == TouchPhase.Began;
                held = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary || began;
                return;
            }
            pos = Input.mousePosition;
            began = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
        }

        static readonly List<RaycastResult> _hits = new();

        /// <summary>The topmost readable label under a screen point, and whether a button owns it.</summary>
        static TMP_Text TextUnder(Vector2 screen, out bool onButton)
        {
            onButton = false;
            var es = EventSystem.current;
            if (es == null) return null;
            var data = new PointerEventData(es) { position = screen };
            _hits.Clear();
            es.RaycastAll(data, _hits);
            if (_hits.Count == 0) return null;

            // Only the very top thing counts: a label hidden under a sheet is not tappable.
            var top = _hits[0].gameObject;
            var text = top.GetComponent<TMP_Text>();
            if (text == null) return null;
            var loc = text.GetComponent<LocText>();
            if (loc == null || loc.NoRead || string.IsNullOrWhiteSpace(text.text)) return null;
            bool letters = false;
            foreach (char c in text.text) if (char.IsLetter(c)) { letters = true; break; }
            if (!letters) return null;
            onButton = ExecuteEvents.GetEventHandler<IPointerClickHandler>(top) != null;
            return text;
        }

        // ---------------------------------------------------------------- the card

        void Show(string text)
        {
            Build();
            // Translate first: stripping tags would stop the English matching its template.
            _body.text = CleanForReading(Loc.T(text));
            _openedFrame = Time.frameCount;
            if (!_root.activeSelf)
            {
                _timeScaleBefore = Time.timeScale;
                Time.timeScale = 0f;
            }
            _root.SetActive(true);
            Fit();
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 1f;
            UiKit.PopIn(_card, 0.9f);
            Game.Sfx.Play(Game.SfxKind.Tap, 0.4f);
        }

        /// <summary>The reader shows the whole text, in one size: sizes and fades come off.</summary>
        static string CleanForReading(string text)
        {
            var s = System.Text.RegularExpressions.Regex.Replace(text, @"</?(size|alpha|voffset|font)[^>]*>", "");
            return s.Trim();
        }

        void Close()
        {
            if (!IsOpen || Time.frameCount == _openedFrame) return;
            _root.SetActive(false);
            Time.timeScale = _timeScaleBefore;
            Game.Sfx.Play(Game.SfxKind.Tap, 0.4f);
        }

        RectTransform _card;

        void Build()
        {
            if (_root != null) return;

            var canvasGo = new GameObject("ReaderCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            _canvasRect = (RectTransform)canvasGo.transform;

            _root = UiKit.Rect("Root", canvasGo.transform).gameObject;
            UiKit.Stretch((RectTransform)_root.transform);

            var scrim = UiKit.Image(_root.transform, "Scrim", null, new Color(0.08f, 0.05f, 0.03f, 0.78f), true);
            UiKit.Stretch(scrim.rectTransform);
            var scrimButton = scrim.gameObject.AddComponent<Button>();
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(Close);

            var card = UiKit.Card(_root.transform, "Card", UiKit.Surface, 44);
            _card = card.rectTransform;
            _card.anchorMin = _card.anchorMax = _card.pivot = new Vector2(0.5f, 0.5f);

            // Big close button along the bottom, easy to find and hit.
            var close = UiKit.Button(_card, "Close", "Close", UiKit.ButtonStyle.Primary, Close, 48f);
            var cr = (RectTransform)close.transform.parent;
            cr.anchorMin = new Vector2(0f, 0f);
            cr.anchorMax = new Vector2(1f, 0f);
            cr.pivot = new Vector2(0.5f, 0f);
            cr.offsetMin = new Vector2(36f, 32f);
            cr.offsetMax = new Vector2(-36f, 32f + 120f);
            MarkNoRead(close.transform);

            _scroll = UiKit.ScrollList(_card, "Scroll", out var content, 0f, new RectOffset(0, 24, 8, 24));
            UiKit.Stretch((RectTransform)_scroll.transform, 40f, 32f + 120f + 24f, 28f, 40f);

            // A fat, always-visible scroll bar on the right.
            var bar = UiKit.Image(_scroll.transform, "Bar", UiKit.Rounded(14), new Color(0f, 0f, 0f, 0.08f), true);
            var br = bar.rectTransform;
            br.anchorMin = new Vector2(1f, 0f);
            br.anchorMax = new Vector2(1f, 1f);
            br.pivot = new Vector2(1f, 0.5f);
            br.sizeDelta = new Vector2(28f, 0f);
            var handle = UiKit.Image(bar.transform, "Handle", UiKit.Rounded(14), UiKit.Spring, true);
            UiKit.Stretch(handle.rectTransform);
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            _scroll.verticalScrollbar = scrollbar;
            _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            _scroll.verticalScrollbarSpacing = 12f;

            _body = UiKit.Text(content, "Body", "", 60f, UiKit.Ink);
            _body.overflowMode = TextOverflowModes.Overflow;
            _body.lineSpacing = 12f;
            var loc = _body.GetComponent<LocText>();
            loc.NoScale = loc.NoRead = true;   // already as big as it gets

            _root.SetActive(false);
        }

        static void MarkNoRead(Transform t)
        {
            foreach (var text in t.GetComponentsInChildren<TMP_Text>(true))
            {
                var l = LocText.Attach(text);
                l.NoRead = true;
            }
        }

        void Fit()
        {
            if (_card == null || _canvasRect == null) return;
            var area = _canvasRect.rect;
            var safe = Screen.safeArea;
            float inset = Mathf.Max(0f, (Screen.height - safe.yMax) / Mathf.Max(1f, Screen.height) * area.height);
            if (area.width < 10f || area.height < 10f) return;
            _card.sizeDelta = new Vector2(Mathf.Min(1000f, area.width - 48f), area.height - 80f - inset * 2f);
            _card.anchoredPosition = Vector2.zero;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => _instance = null;
    }
}
