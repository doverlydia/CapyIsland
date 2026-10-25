using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The answer to something you just did ("too far", "Copied!", "no room there"): a small dark
    /// note just above your finger that drifts up a little and fades by itself. It is where you
    /// were already looking, so it is never Old Yuzu and never her corner. An answer that comes
    /// late (a reply over the internet) shows in the middle of the screen instead.
    ///
    /// One at a time: a new answer replaces the last, or joins it when the two come together.
    /// </summary>
    public class TapAnswer : MonoBehaviour
    {
        static TapAnswer _instance;

        RectTransform _root, _note;
        TextMeshProUGUI _text;
        CanvasGroup _group;
        string _line;
        float _shownAt = -10f;
        Vector2 _at;

        static Vector2 _lastTap;
        static float _lastTapAt = -10f;

        /// <summary>Widest a note gets before its words wrap, in canvas units.</summary>
        const float MaxWidth = 760f;

        /// <summary>A finger this recent means the answer is to it; older, and it goes mid-screen.</summary>
        const float FingerFresh = 1.5f;

        public static void Show(string line)
        {
            if (string.IsNullOrEmpty(line) || Cutscene.Playing) return;   // nothing to answer during a movie
            if (_instance == null) Create();
            _instance.Present(line);
        }

        static void Create()
        {
            var go = new GameObject("[TapAnswer]", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;   // over windows too: "Copied!" answers a button in one
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            _instance = go.AddComponent<TapAnswer>();
            _instance.Build();
        }

        void Build()
        {
            _root = (RectTransform)transform;
            var note = UiKit.Image(_root, "Note", UiKit.Rounded(26), new Color(0.13f, 0.11f, 0.15f, 0.88f));
            note.raycastTarget = false;
            _note = note.rectTransform;
            _note.anchorMin = _note.anchorMax = new Vector2(0.5f, 0.5f);
            _note.pivot = new Vector2(0.5f, 0f);   // sits above the point it answers
            _group = note.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.alpha = 0f;
            _text = UiKit.Text(_note, "Text", "", 32f, Color.white, true, TextAlignmentOptions.Center);
            _text.raycastTarget = false;
            _text.textWrappingMode = TextWrappingModes.Normal;
            var tr = _text.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.pivot = new Vector2(0.5f, 0.5f);
        }

        void Present(string line)
        {
            // Two answers at once (a harvest and a new journal entry): one note, two lines.
            bool together = Time.unscaledTime - _shownAt < 0.6f && !string.IsNullOrEmpty(_line) && _line != line;
            _line = together ? _line + "\n" + line : line;
            _text.text = _line;
            if (!together) _at = Spot();
            _shownAt = Time.unscaledTime;

            _group.DOKill();
            _note.DOKill();
            if (!together)
            {
                _group.alpha = 0f;
                _note.localScale = Vector3.one * 0.85f;
                _note.DOScale(1f, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);
            }
            _group.DOFade(1f, 0.15f).SetUpdate(true);
            float hold = (1.6f + 0.045f * _line.Length) * (Accessibility.InstantText ? 1.5f : 1f);
            _note.anchoredPosition = _at;
            _note.DOAnchorPosY(_at.y + 36f, hold + 0.4f).SetEase(Ease.OutSine).SetUpdate(true);
            _group.DOFade(0f, 0.4f).SetDelay(hold).SetUpdate(true);
        }

        /// <summary>Just above the finger that asked, kept on the screen; mid-screen for a late answer.</summary>
        Vector2 Spot()
        {
            var rect = _root.rect;
            Vector2 at = new(0f, rect.height * 0.12f);
            if (Time.unscaledTime - _lastTapAt < FingerFresh
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, _lastTap, null, out var local))
                at = local + new Vector2(0f, 110f);
            float halfWidth = Mathf.Min(MaxWidth, rect.width - 48f) * 0.5f;
            at.x = Mathf.Clamp(at.x, rect.xMin + 24f + halfWidth, rect.xMax - 24f - halfWidth);
            at.y = Mathf.Clamp(at.y, rect.yMin + 140f, rect.yMax - 360f);   // clear of the bars at top and bottom
            return at;
        }

        void Update()
        {
            // A movie starting takes the note with it; it would only come back afterwards, out of place.
            if (Cutscene.Playing && _group.alpha > 0f)
            {
                _group.DOKill();
                _note.DOKill();
                _group.alpha = 0f;
                _line = null;
            }
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began) { _lastTap = t.position; _lastTapAt = Time.unscaledTime; }
            }
            else if (Input.GetMouseButtonDown(0)) { _lastTap = Input.mousePosition; _lastTapAt = Time.unscaledTime; }
        }

        /// <summary>
        /// Sized to its words as they are drawn (the translation can be longer than the English):
        /// as wide as the line, up to a comfortable width, then wrapping.
        /// </summary>
        void LateUpdate()
        {
            if (_group.alpha <= 0f) return;
            float max = Mathf.Min(MaxWidth, _root.rect.width - 48f) - 56f;
            float width = Mathf.Min(_text.preferredWidth, max);
            float height = _text.GetPreferredValues(_text.text, width, 0f).y;
            _text.rectTransform.sizeDelta = new Vector2(width + 2f, height);
            _note.sizeDelta = new Vector2(width + 56f, height + 34f);
        }

        /// <summary>Taps are watched from the start, so the first answer already knows where the finger was.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (_instance == null) Create();
            DontDestroyOnLoad(_instance.gameObject);
        }
    }
}
