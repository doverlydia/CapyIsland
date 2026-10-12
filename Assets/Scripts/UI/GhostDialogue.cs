using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The ghost who talks you through the opening, sitting bottom-left with a speech bubble beside
    /// them — the same place and shape the original uses.
    ///
    /// Lines queue: calling <see cref="Say"/> while one is showing adds to the queue rather than
    /// cutting it off, so a burst of events during one beat still reads in order. Tutorial
    /// instructions are different (<see cref="Instruct"/>): only the latest one matters, so a new
    /// one replaces any that are waiting or showing — she never tells you to do what you just did.
    ///
    /// While a tip is on screen she steps into the tip's popup (<see cref="Hidden"/>), so the
    /// corner is empty and the two never talk over each other.
    ///
    /// Once a line has been up long enough to read, the bubble tucks away and only the portrait
    /// stays, so it never sits on the island for long. Tap the portrait to bring the line back, and
    /// again to step back through earlier ones; tap the bubble to tuck it away sooner.
    /// </summary>
    public class GhostDialogue : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text label;
        [SerializeField] Image portrait;

        [Header("Timing")]
        [Tooltip("Characters revealed per second. 0 shows the whole line at once.")]
        [SerializeField] float typeSpeed = 42f;

        [Tooltip("Seconds a finished line stays up before the next one, when queued.")]
        [SerializeField] float holdSeconds = 2.2f;

        readonly Queue<string> _queue = new();
        readonly HashSet<string> _instructions = new();
        bool _currentIsInstruction;
        CanvasGroup _group;

        public static GhostDialogue Instance { get; private set; }

        /// <summary>
        /// A popup is up (an item card, a tip, a menu, a purchase, a cutscene), or you are fishing: she waits — her line
        /// neither types nor runs out its reading time unseen — and carries on when it closes.
        /// </summary>
        static bool PopupOpen =>
            HudTour.IsOpen || UiScreen.AnyOpen || SpeechDialog.IsOpen || CardPopup.IsShowing || PurchaseDialog.IsOpen
            || Cutscene.Playing || AwakeningScreen.IsOpen || IntroAudio.Active || TextReader.IsOpen
            || (Fishing.Instance != null && Fishing.Instance.Mode);   // fishing has its own words, at the top

        /// <summary>Her picture, for the tip popup to show her in.</summary>
        public Sprite Face => portrait != null ? portrait.sprite : null;

        /// <summary>Her ghost, always (the opening, the story, her frozen body's lines), even once she is alive again.</summary>
        public Sprite GhostFace { get; private set; }

        /// <summary>Alive again (YuzuHeart): her corner shows her as she is now.</summary>
        void ShowAsShe()
        {
            if (portrait == null || !Game.YuzuHeart.Alive) return;
            var alive = Resources.Load<Sprite>("Cutscenes/Cutscene_IntroYuzu");
            if (alive != null) portrait.sprite = alive;
            WarmDisc();
        }

        bool _discWarmed;

        /// <summary>The disc behind her picture: ghostly mint while she is a ghost, warm apricot once she is alive.</summary>
        void WarmDisc()
        {
            if (_discWarmed || portrait == null || portrait.transform.parent == null) return;
            var disc = portrait.transform.parent.Find("PortraitDisc");   // added when the HUD is styled
            if (disc == null || !disc.TryGetComponent<Image>(out var image)) return;
            image.color = UiKit.Hex("F6C89A");
            _discWarmed = true;
        }

        void OnYuzuStage(Game.YuzuHeart.Stage s) => ShowAsShe();

        void OnDestroy() => Game.YuzuHeart.Changed -= OnYuzuStage;

        /// <summary>
        /// A tip is speaking for her: the corner (picture and bubble) is hidden and nothing advances
        /// until it is shown again, when the current line comes straight back.
        /// </summary>
        public bool Hidden
        {
            get => _hidden;
            set
            {
                if (_hidden == value) return;
                _hidden = value;
                if (_group == null && !TryGetComponent(out _group)) _group = gameObject.AddComponent<CanvasGroup>();
                _group.alpha = value ? 0f : 1f;
                _group.blocksRaycasts = !value;
                _group.interactable = !value;
                if (!value && !string.IsNullOrEmpty(_current) && _queue.Count == 0 && _currentIsInstruction) Say(_current, again: true);
            }
        }
        bool _hidden;
        string _current = "";
        float _revealed;
        float _holdTimer;

        public bool IsSpeaking => panel != null && panel.activeSelf;

        /// <summary>
        /// In the middle of saying something: a line still typing, or still up to be read, or more
        /// waiting. Her bubble tucks away once it has been read. Tips wait for this, so a card never
        /// cuts her off mid-sentence.
        /// </summary>
        public bool MidLine => IsSpeaking && !_hidden && (!_collapsed || _queue.Count > 0);

        /// <summary>
        /// During the tutorial each line is an instruction, so it stays up twice as long before
        /// the bubble tucks away.
        /// </summary>
        public bool HoldLastLine { get; set; } = true;

        float _readTimer;
        bool _collapsed;
        readonly List<string> _history = new();

        /// <summary>True once she has begun saying <paramref name="line"/> (it is in her recent lines).</summary>
        public bool HasSaid(string line) => _history.Contains(line);
        int _historyIndex = -1;
        const int HistoryLength = 8;

        /// <summary>Long enough to read the line twice at an easy pace.</summary>
        float ReadSeconds(string line) =>
            (3.5f + 0.06f * (line?.Length ?? 0)) * (Accessibility.InstantText ? 2f : 1f) * (HoldLastLine ? 2f : 1f);

        /// <summary>Lines type out, unless Easy Reading asks for whole sentences at once.</summary>
        bool Typing => typeSpeed > 0f && !Accessibility.InstantText;

        RectTransform _bubble;
        float _bubbleHeight;

        /// <summary>
        /// Her picture and (when open) her bubble, in screen pixels, so edge arrows can step around
        /// her instead of hiding underneath.
        /// </summary>
        public bool Covers(Vector2 screenPoint, float margin)
        {
            if (_hidden || !isActiveAndEnabled) return false;
            if (portrait != null && portrait.isActiveAndEnabled && Hit(portrait.rectTransform, screenPoint, margin)) return true;
            return _bubble != null && _bubble.gameObject.activeInHierarchy && Hit(_bubble, screenPoint, margin);
        }

        static bool Hit(RectTransform r, Vector2 p, float margin)
        {
            var c = new Vector3[4];
            r.GetWorldCorners(c);   // overlay canvas: world corners are screen pixels
            return p.x > c[0].x - margin && p.x < c[2].x + margin && p.y > c[0].y - margin && p.y < c[2].y + margin;
        }

        /// <summary>Top of her picture or bubble, whichever is higher, in screen pixels.</summary>
        public float TopOnScreen
        {
            get
            {
                float top = 0f; var c = new Vector3[4];
                if (portrait != null) { portrait.rectTransform.GetWorldCorners(c); top = c[1].y; }
                if (_bubble != null && _bubble.gameObject.activeInHierarchy) { _bubble.GetWorldCorners(c); top = Mathf.Max(top, c[1].y); }
                return top;
            }
        }

        /// <summary>How far she sits in from the left edge, clear of the corner where edge arrows land.</summary>
        const float CornerGap = 90f;

        void Awake()
        {
            Instance = this;
            GhostFace = portrait != null ? portrait.sprite : null;
            ShowAsShe();
            Game.YuzuHeart.Changed += OnYuzuStage;
            if (panel != null && panel.transform is RectTransform pr) pr.anchoredPosition += new Vector2(CornerGap, 0f);
            if (panel != null) panel.SetActive(false);
            if (portrait != null)
            {
                portrait.raycastTarget = true;
                var b = portrait.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(Replay);
                portrait.gameObject.AddComponent<PressSquash>();
            }
            if (label != null && label.transform.parent is RectTransform bubble)
            {
                // Grow upward from a fixed bottom edge, so a long line (or Easy Reading's bigger
                // text, or a longer translation) stretches the bubble instead of spilling out.
                _bubble = bubble;
                _bubbleHeight = bubble.sizeDelta.y;
                float bottom = bubble.anchoredPosition.y - bubble.sizeDelta.y * bubble.pivot.y;
                var parent = (RectTransform)bubble.parent;
                float parentBottom = -parent.sizeDelta.y * bubble.anchorMin.y;
                bubble.anchorMin = new Vector2(bubble.anchorMin.x, 0f);
                bubble.anchorMax = new Vector2(bubble.anchorMax.x, 0f);
                bubble.pivot = new Vector2(bubble.pivot.x, 0f);
                bubble.anchoredPosition = new Vector2(bubble.anchoredPosition.x, bottom - parentBottom);

                var bubbleImage = bubble.GetComponent<Image>();
                if (bubbleImage != null)
                {
                    bubbleImage.raycastTarget = true;
                    var tuck = bubble.gameObject.AddComponent<Button>();
                    tuck.transition = Selectable.Transition.None;
                    tuck.onClick.AddListener(Collapse);
                }
            }
        }

        /// <summary>Raised when the bubble tucks away (the first time is when the tip about tapping her shows).</summary>
        public static event System.Action Tucked;

        /// <summary>Hides the bubble, keeping the portrait to tap.</summary>
        void Collapse()
        {
            if (_collapsed) return;
            _collapsed = true;
            Tucked?.Invoke();
            if (_bubble != null) _bubble.gameObject.SetActive(false);
        }

        void Expand()
        {
            _collapsed = false;
            if (_bubble != null) _bubble.gameObject.SetActive(true);
            if (panel != null) panel.SetActive(true);
        }

        /// <summary>
        /// The portrait: brings the latest line back if tucked away, otherwise steps back to the
        /// one before (wrapping round to the latest).
        /// </summary>
        void Replay()
        {
            if (_history.Count == 0) return;
            Game.Sfx.Play(Game.SfxKind.Tap, 0.4f);
            if (_collapsed) _historyIndex = _history.Count - 1;
            else _historyIndex = (_historyIndex <= 0 ? _history.Count : _historyIndex) - 1;
            var line = _history[_historyIndex];
            Expand();
            label.text = line;
            label.maxVisibleCharacters = int.MaxValue;
            _revealed = float.MaxValue;
            _readTimer = ReadSeconds(line);
        }

        /// <summary>Most of the screen's height the bubble may take before the reader is the answer.</summary>
        const float MaxBubbleHeight = 760f;

        void LateUpdate()
        {
            if (_bubble == null || !IsSpeaking || _collapsed) return;
            // No empty bubble, and none behind a popup: closed until there is something to read
            // and nothing in front of it.
            bool anything = !PopupOpen && label != null && !string.IsNullOrEmpty(label.text) && (!Typing || label.maxVisibleCharacters > 0);
            if (_bubble.gameObject.activeSelf != anything) _bubble.gameObject.SetActive(anything);
            if (!anything) return;
            var margins = -label.rectTransform.sizeDelta.y;   // the label is inset inside the bubble
            float want = Mathf.Clamp(label.preferredHeight + margins, _bubbleHeight, MaxBubbleHeight);
            if (Mathf.Abs(_bubble.sizeDelta.y - want) > 1f) _bubble.sizeDelta = new Vector2(_bubble.sizeDelta.x, want);
        }

        /// <summary>
        /// Queues a line. Safe to call repeatedly with the same text — duplicates are ignored, unless
        /// <paramref name="again"/> (a hint that fired a second time) asks to bring a tucked-away
        /// line back.
        /// </summary>
        public void Say(string line, bool again = false)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            if (_current == line)
            {
                if (again && _collapsed)
                {
                    Expand();
                    label.text = line;
                    label.maxVisibleCharacters = int.MaxValue;
                    _revealed = float.MaxValue;
                    _readTimer = ReadSeconds(line);
                }
                return;
            }
            foreach (var queued in _queue) if (queued == line) return;

            _queue.Enqueue(line);
            if (!IsSpeaking || _collapsed) Advance();
        }

        /// <summary>
        /// The tutorial's current instruction. Anything older that is still waiting is dropped, and
        /// an older instruction on screen is replaced at once; a story line being read finishes first.
        /// </summary>
        public void Instruct(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || _current == line) return;
            if (_queue.Count > 0)
            {
                var keep = new List<string>();
                foreach (var q in _queue) if (!_instructions.Contains(q) && q != line) keep.Add(q);
                _queue.Clear();
                foreach (var q in keep) _queue.Enqueue(q);
            }
            _instructions.Add(line);
            _queue.Enqueue(line);
            if (!IsSpeaking || _collapsed || _currentIsInstruction) { if (_queue.Count == 1) Advance(); }
        }

        /// <summary>Clears everything and hides the panel.</summary>
        public void Clear()
        {
            _queue.Clear();
            _current = "";
            _collapsed = false;
            if (panel != null) panel.SetActive(false);
        }

        void Advance()
        {
            // Lines that went stale while they waited (you already did what they ask) are dropped.
            while (_queue.Count > 0 && !Game.Hints.StillTrue(_queue.Peek())) _queue.Dequeue();
            if (_queue.Count == 0)
            {
                // The last line stays current (so repeating it is still ignored); only the bubble goes.
                Collapse();
                return;
            }

            _current = _queue.Dequeue();
            _currentIsInstruction = _instructions.Contains(_current);
            _revealed = 0f;
            // After the typing finishes, time to finish reading before the next line: she types far
            // faster than anyone reads, so long lines get longer (half a second, plus a bit a letter).
            // With Easy Reading (whole sentences at once) there was no typing, so it waits longer still.
            _holdTimer = Typing ? 0.5f + 0.03f * _current.Length : Mathf.Max(holdSeconds, 1f + 0.045f * _current.Length) * 2.5f;
            _readTimer = ReadSeconds(_current);
            if (_history.Count == 0 || _history[_history.Count - 1] != _current) _history.Add(_current);
            if (_history.Count > HistoryLength) _history.RemoveAt(0);
            _historyIndex = _history.Count - 1;
            Expand();
            if (label != null)
            {
                // The whole line is set once and revealed by count, so rich-text tags never show.
                label.text = _current;
                label.maxVisibleCharacters = Typing ? 0 : int.MaxValue;
            }
        }

        void Update()
        {
            if (!_discWarmed && Game.YuzuHeart.Alive) WarmDisc();
            if (_hidden || PopupOpen || !IsSpeaking || label == null) return;
            if (!_collapsed && !Game.Hints.StillTrue(_current)) { Advance(); return; }   // overtaken by events
            if (_collapsed)
            {
                // Tucked away with more to say (the bubble was tapped mid-queue): on to the next line.
                if (_queue.Count > 0) Advance();
                return;
            }

            // The label may draw a translation longer than the English it holds: reveal to whichever
            // is longer.
            int total = Mathf.Max(_current.Length, label.textInfo != null ? label.textInfo.characterCount : 0);
            if (!Typing) label.maxVisibleCharacters = int.MaxValue;
            else if (_revealed < total)
            {
                _revealed += Time.deltaTime * typeSpeed;
                label.maxVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(_revealed), 0, int.MaxValue);
            }

            bool finished = !Typing || _revealed >= total;
            if (!finished) return;

            // Nothing else waiting: give the line time to be read, then tuck the bubble away.
            if (_queue.Count == 0)
            {
                _readTimer -= Time.deltaTime;
                if (_readTimer <= 0f) Collapse();
                return;
            }

            _holdTimer -= Time.deltaTime;
            if (_holdTimer <= 0f) Advance();
        }
    }
}
