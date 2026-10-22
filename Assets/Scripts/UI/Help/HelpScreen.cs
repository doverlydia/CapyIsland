using System;
using System.Collections.Generic;
using System.Text;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// "How do I…?": every common thing a player forgets how to do, on one screen. People open it
    /// with a question in mind ("how do I get gold?"), so it starts with a search box that filters
    /// as you type, in English or Hebrew, then lists every page under plain headings with a
    /// picture each. A page plays a little movie of the move (the tutorial's hand dragging a worker
    /// onto a sawmill, two fingers pinching), a few numbered steps, and a button that takes you
    /// there when there is somewhere to go.
    ///
    /// Pages for things still ahead (a hobby not taken up yet) stay in the list, marked, so you
    /// can see what's coming and how to get it. The pages themselves live in
    /// <see cref="HelpTopics"/>, the movies in <see cref="HelpDemos"/>.
    /// </summary>
    public class HelpScreen : UiScreen
    {
        public static HelpScreen Instance { get; private set; }

        protected override string Title => "How do I…?";
        protected override Sprite TitleIcon => HelpArt.Question;
        protected override Color AccentColour => Accent;
        // Wide on a phone on its side, so the movie and the steps can sit side by side.
        protected override float MaxWidth => 1400f;

        public static readonly Color Accent = UiKit.Hex("4FA3C7");

        /// <summary>Opens the help, on one page if <paramref name="topicId"/> names one.</summary>
        public static void Show(string topicId = null)
        {
            if (Instance == null) return;
            Instance._pending = topicId;
            Instance.Open();
        }

        class Entry
        {
            public HelpTopic Topic;
            public GameObject Card;
            public GameObject NotStarted;
            public RectTransform Chevron;
            public int Order;
            public string Hay, TitleHay, KeyHay;
        }

        readonly List<Entry> _entries = new();
        readonly Dictionary<HelpCategory, GameObject> _sections = new();
        readonly List<UiScreen> _above = new();
        RectTransform _listPage, _list, _topicPage, _backWrap, _demoFrame, _stepsRoot, _steps, _backIcon;
        ScrollRect _listScroll, _stepsScroll;
        TMP_InputField _search;
        GameObject _clear, _empty;
        HelpMovie _movie;
        HelpTopic _topic;
        string _pending;
        int _hayVersion = -1;

        void Awake()
        {
            Instance = this;
            Opened += NoteOpened;
            Closed += NoteClosed;
        }

        void OnDestroy()
        {
            Opened -= NoteOpened;
            Closed -= NoteClosed;
            if (Instance == this) Instance = null;
        }

        // Which screens sit over this one: the back key goes back a page only when it's on top.
        void NoteOpened(UiScreen s) { if (s != this && IsOpen && !_above.Contains(s)) _above.Add(s); }
        void NoteClosed(UiScreen s) => _above.Remove(s);

        protected override void Update()
        {
            // On a page, back (or Escape) returns to the list rather than closing the help.
            if (IsOpen && _topic != null && _above.Count == 0 && Input.GetKeyDown(KeyCode.Escape))
            {
                ShowList();
                return;
            }
            UpdateMoreCue();
            base.Update();
        }

        // ---------------------------------------------------------------- building

        protected override void Build(RectTransform body)
        {
            _listPage = UiKit.Rect("ListPage", body);
            UiKit.Stretch(_listPage);
            BuildSearch(_listPage);
            _listScroll = UiKit.ScrollList(_listPage, "Topics", out _list, 14f);
            UiKit.Stretch((RectTransform)_listScroll.transform, 0f, 0f, 0f, 96f + 18f);

            var empty = UiKit.Text(_list, "Nothing", "Nothing found. Try another word, like wood, map or food.", 30f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            empty.overflowMode = TextOverflowModes.Overflow;
            empty.margin = new Vector4(16f, 30f, 16f, 30f);
            _empty = empty.gameObject;
            _empty.SetActive(false);

            HelpCategory? current = null;
            int order = 0;
            foreach (var topic in HelpTopics.All)
            {
                if (current != topic.Category)
                {
                    current = topic.Category;
                    _sections[topic.Category] = SectionLabel(_list, HelpTopics.CategoryName(topic.Category)).gameObject;
                }
                _entries.Add(Card(topic, order++));
            }

            _topicPage = UiKit.Rect("TopicPage", body);
            UiKit.Stretch(_topicPage);
            BuildTopicPage(_topicPage);
            _topicPage.gameObject.SetActive(false);
        }

        void BuildSearch(RectTransform parent)
        {
            _search = UiKit.Input(parent, "Search", "Search, like “gold” or “zoom”", false, 96f);
            var r = (RectTransform)_search.transform;
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.sizeDelta = new Vector2(0f, 96f);
            r.anchoredPosition = Vector2.zero;
            _search.GetComponent<Image>().color = UiKit.SurfaceSunk;
            // Room for the magnifier on the left and the clear button on the right.
            UiKit.Stretch(_search.textViewport, 86f, 14f, 86f, 14f);
            var glass = UiKit.Image(r, "Magnifier", IconPainter.Magnifier, UiKit.InkSoft);
            glass.preserveAspect = true;
            UiKit.Place(glass.rectTransform, new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(46f, 46f));
            var clear = UiKit.Image(r, "Clear", UiKit.Circle, new Color(0f, 0f, 0f, 0.12f), true);
            UiKit.Place(clear.rectTransform, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(60f, 60f));
            var x = UiKit.Image(clear.transform, "X", IconPainter.Close, UiKit.InkSoft);
            UiKit.Stretch(x.rectTransform, 17f, 17f, 17f, 17f);
            var clearButton = clear.gameObject.AddComponent<Button>();
            clearButton.transition = Selectable.Transition.None;
            clearButton.onClick.AddListener(() => { _search.text = ""; Sfx.Play(SfxKind.Tap, 0.4f); });
            _clear = clear.gameObject;
            _clear.SetActive(false);
            _search.onValueChanged.AddListener(Filter);
        }

        Entry Card(HelpTopic topic, int order)
        {
            var row = UiKit.Image(_list, topic.Id, UiKit.Rounded(30), Color.white, true).rectTransform;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(18, 22, 16, 16);
            h.spacing = 22f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().minHeight = 140f;
            RtlLayout.Add(h);

            Thumb(row, topic, 108f);

            var col = UiKit.Rect("Words", row);
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4f;
            v.childAlignment = TextAnchor.MiddleLeft;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var title = UiKit.Text(col, "Title", topic.Title, 36f, UiKit.Ink, true);
            title.overflowMode = TextOverflowModes.Overflow;
            var summary = UiKit.Text(col, "Summary", topic.Summary, 28f, UiKit.InkSoft);
            summary.overflowMode = TextOverflowModes.Overflow;
            GameObject notStarted = null;
            if (topic.Started != null)
            {
                var later = UiKit.Text(col, "NotStarted", "Not started yet", 24f, UiKit.PrimaryDark, true);
                later.overflowMode = TextOverflowModes.Overflow;
                notStarted = later.gameObject;
            }

            var chevron = UiKit.Image(row, "Go", IconPainter.Chevron, UiKit.InkSoft);
            chevron.preserveAspect = true;
            var cle = chevron.gameObject.AddComponent<LayoutElement>();
            cle.preferredWidth = cle.preferredHeight = cle.minWidth = 30f;

            var button = row.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { Sfx.Play(SfxKind.Tap, 0.45f); OpenTopic(topic); });
            row.gameObject.AddComponent<PressSquash>();

            return new Entry { Topic = topic, Card = row.gameObject, NotStarted = notStarted, Chevron = chevron.rectTransform, Order = order };
        }

        /// <summary>
        /// The topic's picture on a round plate: a painted icon white on the shelf's colour, a
        /// picture from the island on a pale one.
        /// </summary>
        static void Thumb(Transform parent, HelpTopic topic, float size)
        {
            Sprite sprite = null;
            try { sprite = topic.Thumb != null ? topic.Thumb() : null; } catch (Exception) { /* a missing picture gets the star */ }
            var colour = HelpTopics.CategoryColour(topic.Category);
            bool icon = sprite == null || IconPainter.IsIcon(sprite) || sprite == HelpArt.Question;
            if (sprite == null) sprite = IconPainter.Star;
            var disc = UiKit.Image(parent, "Thumb", UiKit.Circle, icon ? colour : Color.Lerp(colour, Color.white, 0.78f));
            var le = disc.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = le.minWidth = le.minHeight = size;
            var pic = UiKit.Image(disc.transform, "Picture", sprite, Color.white);
            pic.preserveAspect = true;
            float inset = size * (icon ? 0.24f : 0.1f);
            UiKit.Stretch(pic.rectTransform, inset, inset, inset, inset);
        }

        void BuildTopicPage(RectTransform page)
        {
            var back = UiKit.Button(page, "Back", "All topics", UiKit.ButtonStyle.Secondary, ShowList, 30f, IconPainter.Chevron);
            _backWrap = (RectTransform)back.transform.parent;
            _backIcon = back.transform.Find("Content/Icon") as RectTransform;

            var frame = UiKit.Image(page, "Movie", UiKit.Rounded(30), HelpArt.Sea);
            frame.gameObject.AddComponent<RectMask2D>();
            _demoFrame = frame.rectTransform;

            _stepsScroll = UiKit.ScrollList(page, "Steps", out _steps, 18f, new RectOffset(4, 4, 4, 28));
            _stepsRoot = (RectTransform)_stepsScroll.transform;
            BuildMoreCue();
        }

        CanvasGroup _more;
        RectTransform _moreLayer;
        RectTransform _moreButton;
        Tween _moreScroll;

        /// <summary>
        /// When the steps run past the bottom (a phone on its side), a small round button floats at
        /// the bottom of them with an arrow pointing down, bobbing gently, so it's clear there's
        /// more to read. Tapping it glides the steps down a page; it fades away at the bottom.
        /// </summary>
        void BuildMoreCue()
        {
            // On a layer of its own over the steps (same place, same size), drawn after them: inside
            // the scroll list the words were drawn over it.
            _moreLayer = UiKit.Rect("MoreLayer", _stepsRoot.parent);
            _moreLayer.SetAsLastSibling();
            var holder = UiKit.Rect("More", _moreLayer);
            UiKit.Place(holder, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(72f, 72f));
            _more = holder.gameObject.AddComponent<CanvasGroup>();
            _more.alpha = 0f;
            _more.blocksRaycasts = _more.interactable = false;

            var disc = UiKit.Image(holder, "Button", UiKit.Circle, Accent, true);
            _moreButton = disc.rectTransform;
            UiKit.Place(_moreButton, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72f, 72f));
            UiKit.AddShadow(_moreButton, 10f, 5f, 0.22f);
            var arrow = UiKit.Image(disc.transform, "Arrow", IconPainter.Chevron, Color.white);
            arrow.preserveAspect = true;
            UiKit.Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(34f, 34f));
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);   // the chevron points right: turned to point down
            var button = disc.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            // It was born disabled (the group isn't interactable yet) while still on the default
            // colour tint, which faded it to half: undo that, or it stays see-through for good.
            disc.CrossFadeColor(Color.white, 0f, true, true);
            button.onClick.AddListener(ScrollStepsDown);
            disc.gameObject.AddComponent<PressSquash>();
        }

        /// <summary>The steps glide down most of a page (a line or two of the old one stays in view).</summary>
        void ScrollStepsDown()
        {
            float spare = _steps.rect.height - _stepsRoot.rect.height;
            if (spare <= 1f) return;
            Sfx.Play(SfxKind.Tap, 0.4f);
            _stepsScroll.StopMovement();
            float from = _stepsScroll.verticalNormalizedPosition;
            float to = Mathf.Clamp01(from - _stepsRoot.rect.height * 0.8f / spare);
            _moreScroll?.Kill();
            _moreScroll = DOTween.To(() => _stepsScroll.verticalNormalizedPosition, v => _stepsScroll.verticalNormalizedPosition = v, to, 0.45f)
                .SetEase(Ease.OutCubic).SetUpdate(true);
        }

        void UpdateMoreCue()
        {
            if (_more == null) return;
            // Keep the layer exactly over the steps, whichever way the screen is turned.
            _moreLayer.anchorMin = _stepsRoot.anchorMin;
            _moreLayer.anchorMax = _stepsRoot.anchorMax;
            _moreLayer.pivot = _stepsRoot.pivot;
            _moreLayer.offsetMin = _stepsRoot.offsetMin;
            _moreLayer.offsetMax = _stepsRoot.offsetMax;
            bool more = _topic != null && _steps.rect.height > _stepsRoot.rect.height + 4f && _stepsScroll.verticalNormalizedPosition > 0.02f;
            _more.alpha = Mathf.MoveTowards(_more.alpha, more ? 1f : 0f, Time.unscaledDeltaTime * 5f);
            _more.blocksRaycasts = _more.interactable = more;
            if (_more.alpha > 0f) _moreButton.anchoredPosition = new Vector2(0f, 5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.6f)));
        }

        // ---------------------------------------------------------------- opening and closing

        protected override void OnOpen()
        {
            _above.Clear();
            bool rtl = Loc.Rtl;
            foreach (var e in _entries)
            {
                if (e.NotStarted != null) e.NotStarted.SetActive(!Started(e.Topic));
                // The chevron points the way you read: on into the page.
                e.Chevron.localScale = new Vector3(rtl ? -1f : 1f, 1f, 1f);
            }
            if (_search.text.Length > 0) _search.text = "";
            else Filter("");

            var topic = _pending != null ? HelpTopics.Find(_pending) : null;
            _pending = null;
            if (topic != null) OpenTopic(topic);
            else ShowList();
        }

        protected override void OnClose()
        {
            StopMovie();
            _topic = null;
        }

        static bool Started(HelpTopic t)
        {
            try { return t.Started == null || t.Started(); }
            catch (Exception) { return true; }
        }

        void ShowList()
        {
            StopMovie();
            _topic = null;
            _topicPage.gameObject.SetActive(false);
            _listPage.gameObject.SetActive(true);
        }

        void OpenTopic(HelpTopic topic)
        {
            _topic = topic;
            _listPage.gameObject.SetActive(false);
            _topicPage.gameObject.SetActive(true);
            if (_backIcon != null) _backIcon.localScale = new Vector3(Loc.Rtl ? 1f : -1f, 1f, 1f);   // pointing back
            Relayout();

            ClearChildren(_steps);
            var title = UiKit.Text(_steps, "Title", topic.Title, 46f, UiKit.PrimaryDark, true);
            title.overflowMode = TextOverflowModes.Overflow;
            var summary = UiKit.Text(_steps, "Summary", topic.Summary, 30f, UiKit.InkSoft);
            summary.overflowMode = TextOverflowModes.Overflow;
            if (!Started(topic))
            {
                var later = UiKit.Text(_steps, "NotStarted", "You haven't started this yet. Here's how, for when you're ready.", 28f, UiKit.PrimaryDark, true);
                later.overflowMode = TextOverflowModes.Overflow;
            }
            for (int i = 0; i < topic.Steps.Length; i++) Step(_steps, i + 1, topic.Steps[i]);

            if (topic.ShowMe != null)
            {
                var action = topic.ShowMe;
                var go = UiKit.Button(_steps, "ShowMe", string.IsNullOrEmpty(topic.ShowMeLabel) ? "Show me" : topic.ShowMeLabel,
                    UiKit.ButtonStyle.Primary, () => { Close(); action(); }, 34f);
                var le = go.transform.parent.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = le.minHeight = 96f;
            }
            _moreScroll?.Kill();
            _stepsScroll.verticalNormalizedPosition = 1f;

            StopMovie();
            _movie = HelpDemos.Play(topic.Demo, _demoFrame, topic.Picture, topic.Found);
        }

        /// <summary>A numbered step: a round number, then the words.</summary>
        void Step(RectTransform list, int number, string text)
        {
            var row = UiKit.Rect("Step" + number, list);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18f;
            h.childAlignment = TextAnchor.UpperLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            RtlLayout.Add(h);
            var disc = UiKit.Image(row, "Number", UiKit.Circle, Accent);
            var dle = disc.gameObject.AddComponent<LayoutElement>();
            dle.preferredWidth = dle.preferredHeight = dle.minWidth = dle.minHeight = 58f;
            var n = UiKit.Text(disc.transform, "N", number.ToString(), 32f, Color.white, true, TextAlignmentOptions.Center);
            UiKit.Stretch(n.rectTransform);
            var loc = n.GetComponent<LocText>();
            if (loc != null) { loc.NoScale = true; loc.NoRead = true; }
            var words = UiKit.Text(row, "Words", text, 32f, UiKit.Ink);
            words.overflowMode = TextOverflowModes.Overflow;
            words.margin = new Vector4(0f, 8f, 0f, 0f);
            words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        void StopMovie()
        {
            if (_movie == null) return;
            var go = _movie.gameObject;
            _movie = null;
            go.SetActive(false);
            Destroy(go);
        }

        // ---------------------------------------------------------------- portrait and landscape

        protected override void Fit()
        {
            base.Fit();
            Relayout();
        }

        /// <summary>
        /// Tall (a phone upright): the movie across the top and the steps scrolling under it.
        /// Wide (a phone on its side, a computer): the movie on the left, the steps beside it.
        /// </summary>
        void Relayout()
        {
            if (_topicPage == null || Body == null) return;
            var size = Body.rect.size;
            if (size.x <= 1f || size.y <= 1f) return;
            const float backHeight = 80f, gap = 16f;
            bool rtl = Loc.Rtl;

            var anchor = new Vector2(rtl ? 1f : 0f, 1f);
            UiKit.Place(_backWrap, anchor, Vector2.zero, new Vector2(Mathf.Min(340f, size.x), backHeight));

            float top = backHeight + gap;
            bool wide = size.x > size.y * 1.15f;
            if (wide)
            {
                float split = 0.5f;
                _demoFrame.anchorMin = new Vector2(0f, 0f);
                _demoFrame.anchorMax = new Vector2(split, 1f);
                _demoFrame.pivot = new Vector2(0.5f, 0.5f);
                _demoFrame.offsetMin = Vector2.zero;
                _demoFrame.offsetMax = new Vector2(-gap * 0.5f, -top);
                _stepsRoot.anchorMin = new Vector2(split, 0f);
                _stepsRoot.anchorMax = Vector2.one;
                _stepsRoot.offsetMin = new Vector2(gap * 0.5f, 0f);
                _stepsRoot.offsetMax = new Vector2(0f, -top);
            }
            else
            {
                float movie = Mathf.Clamp(size.x * 0.62f, 240f, size.y * 0.42f);
                _demoFrame.anchorMin = new Vector2(0f, 1f);
                _demoFrame.anchorMax = new Vector2(1f, 1f);
                _demoFrame.pivot = new Vector2(0.5f, 1f);
                _demoFrame.sizeDelta = new Vector2(0f, movie);
                _demoFrame.anchoredPosition = new Vector2(0f, -top);
                UiKit.Stretch(_stepsRoot, 0f, 0f, 0f, top + movie + gap);
            }
        }

        // ---------------------------------------------------------------- search

        /// <summary>
        /// Keeps the cards whose words hold every word typed — title, line, steps and keywords, in
        /// English and, with Hebrew on, in Hebrew too — best matches first (a title match beats a
        /// keyword, which beats a step). An empty box shows every page under its heading.
        /// </summary>
        void Filter(string query)
        {
            if (_clear != null) _clear.SetActive(!string.IsNullOrEmpty(query));
            var words = Words(query);
            if (words.Count == 0)
            {
                _empty.SetActive(false);
                foreach (var s in _sections.Values) s.SetActive(true);
                // Back to reading order: each heading, then its cards.
                int i = 1;
                HelpCategory? current = null;
                foreach (var e in _entries)
                {
                    if (current != e.Topic.Category)
                    {
                        current = e.Topic.Category;
                        _sections[current.Value].transform.SetSiblingIndex(i++);
                    }
                    e.Card.SetActive(true);
                    e.Card.transform.SetSiblingIndex(i++);
                }
                _listScroll.verticalNormalizedPosition = 1f;
                return;
            }

            BuildHay();
            foreach (var s in _sections.Values) s.SetActive(false);
            var hits = new List<(Entry e, int score)>();
            foreach (var e in _entries)
            {
                int score = Score(e, words);
                e.Card.SetActive(score > 0);
                if (score > 0) hits.Add((e, score));
            }
            hits.Sort((a, b) => a.score != b.score ? b.score.CompareTo(a.score) : a.e.Order.CompareTo(b.e.Order));
            for (int i = 0; i < hits.Count; i++) hits[i].e.Card.transform.SetSiblingIndex(i + 1);
            _empty.SetActive(hits.Count == 0);
            _listScroll.verticalNormalizedPosition = 1f;
        }

        static int Score(Entry e, List<string> words)
        {
            int score = 0;
            foreach (var w in words)
            {
                var found = Variant(w, e.Hay);
                if (found == null) return 0;
                score += e.TitleHay.Contains(found) ? 3 : e.KeyHay.Contains(found) ? 2 : 1;
            }
            return score;
        }

        /// <summary>
        /// The word as typed if it's there, else a looser form: English without a plural "s",
        /// Hebrew without a one-letter prefix ("הזהב" finds "זהב").
        /// </summary>
        static string Variant(string w, string hay)
        {
            if (hay.Contains(w)) return w;
            if (w.Length > 3 && w.EndsWith("s") && hay.Contains(w.Substring(0, w.Length - 1))) return w.Substring(0, w.Length - 1);
            if (w.Length > 3 && Loc.HasHebrew(w) && "הובלמשכ".IndexOf(w[0]) >= 0 && hay.Contains(w.Substring(1))) return w.Substring(1);
            return null;
        }

        static List<string> Words(string query)
        {
            var words = new List<string>();
            if (string.IsNullOrWhiteSpace(query)) return words;
            var sb = new StringBuilder();
            foreach (char c in query.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) { sb.Append(c); continue; }
                if (sb.Length > 0) { words.Add(sb.ToString()); sb.Clear(); }
            }
            if (sb.Length > 0) words.Add(sb.ToString());
            return words;
        }

        /// <summary>Each page's searchable words, in English and the current language (rebuilt when it changes).</summary>
        void BuildHay()
        {
            if (_hayVersion == Loc.Version) return;
            _hayVersion = Loc.Version;
            bool translated = Loc.Current != Language.English;
            foreach (var e in _entries)
            {
                var t = e.Topic;
                string Both(string s) => translated ? s + " " + Loc.T(s) : s;
                var title = Both(t.Title);
                var keys = Both(t.Keywords ?? "");
                var all = new StringBuilder(title).Append(' ').Append(keys).Append(' ')
                    .Append(Both(t.Summary)).Append(' ').Append(Both(HelpTopics.CategoryName(t.Category)));
                foreach (var step in t.Steps) all.Append(' ').Append(Both(step));
                e.TitleHay = title.ToLowerInvariant();
                e.KeyHay = keys.ToLowerInvariant();
                e.Hay = all.ToString().ToLowerInvariant();
            }
        }
    }
}
