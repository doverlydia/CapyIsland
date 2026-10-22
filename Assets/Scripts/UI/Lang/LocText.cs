using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.SceneManagement;

namespace CapyIsland.UI
{
    /// <summary>
    /// Rides on every TextMeshPro label and adjusts what it draws, never what it holds:
    ///
    ///  * the language — the English in <c>text</c> is translated as the label is drawn, so code
    ///    that reads a label back still sees the English it wrote;
    ///  * the direction — a Hebrew line is laid out right to left, and left-aligned text moves to
    ///    the right edge;
    ///  * the size — Easy Reading grows every label by its text-size factor;
    ///  * the hit target — with tap-to-read on, labels catch taps so the reader can open them.
    ///
    /// Runs late in the frame, after the HUD has written its labels, so a label that changed this
    /// frame is drawn translated this frame.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public class LocText : MonoBehaviour, ITextPreprocessor
    {
        /// <summary>Badges, counters and pickers sized for their box keep their size.</summary>
        public bool NoScale
        {
            get => _noScale;
            set
            {
                if (_noScale == value) return;
                _noScale = value;
                if (value) RestoreSize();
                _a11yVersion = -1;
            }
        }
        bool _noScale;

        /// <summary>Not offered to the reader (the reader's own text, tiny counters).</summary>
        public bool NoRead;

        /// <summary>
        /// What a player typed (chat): drawn as it is, never looked up in the tables — "Yes" from a
        /// friend stays "Yes". Hebrew in it is still laid out right to left.
        /// </summary>
        public bool NoTranslate
        {
            get => _noTranslate;
            set
            {
                if (_noTranslate == value) return;
                _noTranslate = value;
                _langVersion = -1;   // drawn afresh on the next refresh
            }
        }
        bool _noTranslate;

        string Translate(string s) => _noTranslate ? s : Loc.T(s);

        TMP_Text _t;
        string _source;
        string _display;
        int _langVersion = -1;

        bool _rtl;
        bool _flippedAlign;
        List<OTL_FeatureTag> _authoredFeatures;

        int _a11yVersion = -1;
        float _baseSize, _baseMin, _baseMax;
        float _appliedSize, _appliedMin, _appliedMax;
        bool _sized;

        bool _authoredRaycast;
        bool _raycastForced;

        public TMP_Text Text => _t;

        /// <summary>Adds the component if missing. Safe to call on any label, any number of times.</summary>
        public static LocText Attach(TMP_Text t)
        {
            if (t == null) return null;
            var l = t.GetComponent<LocText>();
            if (l == null) l = t.gameObject.AddComponent<LocText>();
            return l;
        }

        void Awake()
        {
            _t = GetComponent<TMP_Text>();
            if (_t != null) _t.textPreprocessor = this;
        }

        // Sizing waits for the first LateUpdate: whoever made the label is still setting it up.
        void OnEnable() => Refresh(size: false);

        void OnDestroy()
        {
            if (_t != null && ReferenceEquals(_t.textPreprocessor, this)) _t.textPreprocessor = null;
        }

        void LateUpdate() => Refresh();

        /// <summary>Brings the label up to date with its text, the language and Easy Reading.</summary>
        public void Refresh() => Refresh(size: true);

        void Refresh(bool size)
        {
            if (_t == null) return;

            var src = _t.text;
            bool textChanged = !string.Equals(src, _source);
            bool langChanged = _langVersion != Loc.Version;
            if (textChanged || langChanged)
            {
                _source = src;
                _langVersion = Loc.Version;
                var translated = Translate(src);
                bool rtl = Loc.HasHebrew(translated);   // "עברית" on the language picker, even in English
                _display = rtl ? Loc.Visual(translated) : translated;
                SetDirection(rtl);
                // Same text, new language: TextMeshPro would not re-read it by itself.
                if (!textChanged) _t.havePropertiesChanged = true;
            }

            SyncAlignment();
            if (size) ApplySize();
            ApplyRaycast();
        }

        public string PreprocessText(string text)
        {
            if (string.Equals(text, _source) && _langVersion == Loc.Version) return _display;
            // Set after this frame's refresh: translate now; the direction catches up next frame.
            var translated = Translate(text);
            return _t != null && _t.isRightToLeftText ? Loc.Visual(translated) : translated;
        }

        void SetDirection(bool rtl)
        {
            if (rtl == _rtl) return;
            _rtl = rtl;
            _t.isRightToLeftText = rtl;

            // TextMeshPro applies kerning pairs with the wrong sign when laying out right to left,
            // which throws letters like י and ל onto their neighbours. Hebrew does without.
            if (rtl)
            {
                _authoredFeatures = new List<OTL_FeatureTag>(_t.fontFeatures);
                _t.fontFeatures = new List<OTL_FeatureTag>();
            }
            else if (_authoredFeatures != null) _t.fontFeatures = _authoredFeatures;
            _t.havePropertiesChanged = true;
        }

        /// <summary>
        /// Left-aligned Hebrew moves to the right edge. Checked every refresh, not just when the
        /// direction flips, because whoever made the label often sets its alignment afterwards.
        /// </summary>
        void SyncAlignment()
        {
            if (_rtl)
            {
                if (_t.horizontalAlignment != HorizontalAlignmentOptions.Left) return;
                _t.horizontalAlignment = HorizontalAlignmentOptions.Right;
                _flippedAlign = true;
            }
            else if (_flippedAlign)
            {
                if (_t.horizontalAlignment == HorizontalAlignmentOptions.Right) _t.horizontalAlignment = HorizontalAlignmentOptions.Left;
                _flippedAlign = false;
            }
        }

        void ApplySize()
        {
            if (_noScale) return;

            if (!_sized || _t.enableAutoSizing != (_sizedAuto || _converted)) Capture();
            else if (_sizedAuto)
            {
                // Code gave an auto-sized label a new range: that range is the new 1x.
                if (!Mathf.Approximately(_t.fontSizeMax, _appliedMax) || !Mathf.Approximately(_t.fontSizeMin, _appliedMin)) Capture();
            }
            else if (!_converted && !Mathf.Approximately(_t.fontSize, _appliedSize)) Capture();   // code set a new size

            if (_a11yVersion == Accessibility.Version) return;
            _a11yVersion = Accessibility.Version;

            float s = Accessibility.TextScale;
            if (_sizedAuto)
            {
                // An auto-sized label already fits its box: let it grow into the room it has.
                _t.fontSizeMax = _appliedMax = _baseMax * s;
                _t.fontSizeMin = _appliedMin = _baseMin * Mathf.Min(s, 1.2f);
            }
            else if (s > 1.001f && (_t.overflowMode == TextOverflowModes.Ellipsis || _t.overflowMode == TextOverflowModes.Truncate))
            {
                // A label that cuts itself off would vanish entirely once the bigger size no
                // longer fits its box. Grow as far as the box allows instead, never below 1x;
                // labels in lists still grow fully, because layout asks for the largest size.
                _converted = true;
                _t.enableAutoSizing = true;
                _t.fontSizeMin = _appliedMin = _baseSize;
                _t.fontSizeMax = _appliedMax = _baseSize * s;
                _t.fontSize = _baseSize * s;
            }
            else
            {
                Unconvert();
                _t.fontSize = _appliedSize = _baseSize * s;
            }
        }

        void Capture()
        {
            Unconvert();
            _baseSize = _t.fontSize;
            _baseMin = _t.fontSizeMin;
            _baseMax = _t.fontSizeMax;
            _sizedAuto = _t.enableAutoSizing;
            _sized = true;
            _a11yVersion = -1;
        }

        void Unconvert()
        {
            if (!_converted) return;
            _converted = false;
            _t.enableAutoSizing = false;
            _t.fontSizeMin = _baseMin;
            _t.fontSizeMax = _baseMax;
        }

        /// <summary>Back to the size the label was made with.</summary>
        void RestoreSize()
        {
            if (!_sized || _t == null) return;
            Unconvert();
            if (_sizedAuto)
            {
                _t.fontSizeMax = _appliedMax = _baseMax;
                _t.fontSizeMin = _appliedMin = _baseMin;
            }
            else _t.fontSize = _appliedSize = _baseSize;
        }

        bool _sizedAuto, _converted;

        void ApplyRaycast()
        {
            bool want = Accessibility.TapToRead && !NoRead && _t is TextMeshProUGUI;
            if (want == _raycastForced) return;
            if (want)
            {
                _authoredRaycast = _t.raycastTarget;
                _t.raycastTarget = true;
            }
            else _t.raycastTarget = _authoredRaycast;
            _raycastForced = want;
        }
    }

    /// <summary>
    /// Finds every label in the game and gives it a <see cref="LocText"/>: labels built by
    /// <see cref="UiKit.Text"/> get one at birth; the rest (the island builder's panels, anything
    /// made with AddComponent) are caught the first time they draw, and by a sweep whenever a scene
    /// loads or a setting changes.
    /// </summary>
    public class TextWatcher : MonoBehaviour
    {
        static TextWatcher _instance;
        readonly List<TMP_Text> _pending = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("[TextWatcher]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<TextWatcher>();
        }

        void OnEnable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Loc.Changed += Sweep;
            Accessibility.Changed += Sweep;
        }

        void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Loc.Changed -= Sweep;
            Accessibility.Changed -= Sweep;
        }

        void Start()
        {
            UiKit.EnsureFallbacks();
            Sweep();
        }

        void OnSceneLoaded(Scene s, LoadSceneMode m) => Sweep();

        // Fires from inside the canvas rebuild, where nothing may be changed: note it for later.
        void OnTextChanged(Object o)
        {
            if (o is TMP_Text t && t != null && t.textPreprocessor == null) _pending.Add(t);
        }

        void Update() => Flush();

        void LateUpdate() => Flush();

        void Flush()
        {
            if (_pending.Count == 0) return;
            foreach (var t in _pending)
                if (t != null && t.GetComponent<LocText>() == null) LocText.Attach(t).Refresh();
            _pending.Clear();
        }

        /// <summary>Attaches to every label in loaded scenes, active or not.</summary>
        public static void Sweep()
        {
            foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                LocText.Attach(t);
        }
    }
}
