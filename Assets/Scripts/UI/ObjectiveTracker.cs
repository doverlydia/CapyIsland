using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The current goal, pinned under the resource bar the way most mobile games do it: an icon,
    /// who asked, and what to do. It refreshes twice a second and bounces when the goal changes.
    /// Tap it for the full list.
    ///
    /// It can be tucked away: swipe it left (or tap its little arrow) and it shrinks to just its
    /// round icon, which still bounces when there is something new. Tap the icon, or swipe right,
    /// to open it out again. Remembered between sessions.
    ///
    /// It is Old Yuzu speaking. Through the opening it isn't there at all — she teaches from her
    /// corner, and one place to look is enough — and when the opening ends she floats up out of
    /// her corner into the card's round icon. From then on her face is on every goal nobody else
    /// asked for (quests show whoever asked), and when the goal changes the card grows for a
    /// moment with a line in her voice (<see cref="Say"/>) before settling back to the goal.
    /// </summary>
    public class ObjectiveTracker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static ObjectiveTracker Instance { get; private set; }

        const string CollapsedKey = "CapyIsland.TrackerCollapsed";
        const float CollapsedSize = 108f;

        bool _collapsed;
        Image _hide;
        GameObject _chevronGo;
        float _dragX;

        RectTransform _card;
        Image _disc, _icon;
        TextMeshProUGUI _caption, _title;
        string _shown;
        float _next;
        bool _tall;   // showing a step to do first, under the goal

        public static ObjectiveTracker Create(Transform parent)
        {
            var root = UiKit.Rect("ObjectiveTracker", parent);
            var tracker = root.gameObject.AddComponent<ObjectiveTracker>();
            tracker.Build(root);
            return tracker;
        }

        void Build(RectTransform root)
        {
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 1f);
            // Beside your profile badge.
            root.anchoredPosition = new Vector2(16f + ProfileBadge.Size + 12f, -104f);

            var card = UiKit.Card(root, "Card", UiKit.Surface, 34);
            _card = card.rectTransform;
            UiKit.Stretch(_card);
            var button = card.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OpenList);
            card.gameObject.AddComponent<PressSquash>();

            _disc = UiKit.Image(_card, "Disc", UiKit.Circle, UiKit.Spring);
            UiKit.Place(_disc.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(80f, 80f));
            _icon = UiKit.Image(_disc.transform, "Icon", IconPainter.Goals, Color.white);
            _icon.preserveAspect = true;
            UiKit.Stretch(_icon.rectTransform, 16f, 16f, 16f, 16f);

            _caption = UiKit.Text(_card, "Caption", "GOAL", 22f, UiKit.Spring, true);
            _caption.characterSpacing = 5f;
            _caption.textWrappingMode = TextWrappingModes.NoWrap;
            _caption.rectTransform.anchorMin = new Vector2(0f, 1f);
            _caption.rectTransform.anchorMax = new Vector2(1f, 1f);
            _caption.rectTransform.pivot = new Vector2(0f, 1f);
            _caption.rectTransform.offsetMin = new Vector2(110f, -44f);
            _caption.rectTransform.offsetMax = new Vector2(-48f, -14f);

            _title = UiKit.Text(_card, "Title", "", 30f, UiKit.Ink, true);
            _title.rectTransform.anchorMin = Vector2.zero;
            _title.rectTransform.anchorMax = Vector2.one;
            _title.rectTransform.offsetMin = new Vector2(110f, 14f);
            _title.rectTransform.offsetMax = new Vector2(-48f, -44f);
            _title.alignment = TextAlignmentOptions.TopLeft;
            _title.enableAutoSizing = true;
            _title.fontSizeMin = 22f;
            _title.fontSizeMax = 30f;

            // A small tab to tuck the card away, pointing the way it goes.
            _hide = UiKit.Image(_card, "Hide", UiKit.Circle, UiKit.SurfaceSunk, true);   // catches its own taps, not the card's
            UiKit.Place(_hide.rectTransform, new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(40f, 40f));
            var arrow = UiKit.Image(_hide.transform, "Arrow", IconPainter.Arrow, UiKit.InkSoft);
            UiKit.Stretch(arrow.rectTransform, 10f, 10f, 10f, 10f);
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            var hideButton = _hide.gameObject.AddComponent<Button>();
            hideButton.transition = Selectable.Transition.None;
            hideButton.onClick.AddListener(() => SetCollapsed(true));
            _chevronGo = _hide.gameObject;

            _collapsed = PlayerPrefs.GetInt(CollapsedKey, 0) == 1;
            ApplyCollapsed(animate: false);

            Core.ScreenMetrics.Changed += Fit;
            Accessibility.Changed += Fit;
            Fit();

            Instance = this;
            _group = root.gameObject.AddComponent<CanvasGroup>();
            _away = Opening;
            ShowCard(!_away, animate: false);
            TutorialDirector.Graduated += OnGraduated;
        }

        void OnDestroy()
        {
            Core.ScreenMetrics.Changed -= Fit;
            Accessibility.Changed -= Fit;
            TutorialDirector.Graduated -= OnGraduated;
            if (Instance == this) Instance = null;
        }

        // ---------------------------------------------------------------- the opening, and Old Yuzu's arrival

        CanvasGroup _group;
        bool _away, _graduated, _handingOff;

        /// <summary>The opening is teaching: Old Yuzu has the floor, from her corner.</summary>
        static bool Opening => Attention.TutorialRunning && !Guidance.Off;

        void ShowCard(bool show, bool animate)
        {
            _group.DOKill();
            _group.blocksRaycasts = _group.interactable = show;
            if (animate) _group.DOFade(show ? 1f : 0f, 0.4f).SetUpdate(true);
            else _group.alpha = show ? 1f : 0f;
        }

        void OnGraduated()
        {
            _graduated = true;
            HudTour.MarkSeen("goals");   // she says it herself as she arrives; no card about the card
        }

        /// <summary>Hidden through the opening; shown once it's over, with Old Yuzu flying up into it if it just ended.</summary>
        void TickAway()
        {
            bool away = Opening;
            if (away != _away)
            {
                _away = away;
                if (away) ShowCard(false, animate: true);
                else if (_graduated && !Guidance.Off) StartCoroutine(HandOff());
                else ShowCard(true, animate: true);
            }
            // An island whose opening is over: her corner is empty but for news.
            var ghost = GhostDialogue.Instance;
            if (!_away && !_handingOff && ghost != null && !ghost.Retired) ghost.Retire();
        }

        const string ArrivalLine = "From now on, I'll be up here. Tap me whenever you want to see what's next.";

        /// <summary>
        /// The opening's last line read, Old Yuzu lifts out of her corner and floats up into the
        /// card's round icon; the card fades in under her and she says where she'll be.
        /// </summary>
        System.Collections.IEnumerator HandOff()
        {
            _handingOff = true;
            var ghost = GhostDialogue.Instance;
            float giveUp = Time.unscaledTime + 25f;
            while (Time.unscaledTime < giveUp && (Attention.ScreenBusy || (ghost != null && ghost.MidLine))) yield return null;

            _arriving = true;   // from here until her first line is read, nothing else talks
            if (_collapsed) SetCollapsed(false);
            Refresh();
            var from = ghost != null ? ghost.PortraitRect : null;
            var face = ghost != null ? ghost.Face : null;
            var canvas = GetComponentInParent<Canvas>();
            if (from != null && from.gameObject.activeInHierarchy && face != null && canvas != null)
            {
                var top = canvas.rootCanvas.transform;
                var flyer = UiKit.Image(top, "YuzuRising", face, Color.white);
                flyer.preserveAspect = true;
                flyer.raycastTarget = false;
                var fr = flyer.rectTransform;
                fr.anchorMin = fr.anchorMax = fr.pivot = new Vector2(0.5f, 0.5f);
                float toCanvas = 1f / Mathf.Max(0.0001f, top.lossyScale.x);
                fr.sizeDelta = from.rect.size * from.lossyScale.x * toCanvas;
                fr.position = from.TransformPoint(from.rect.center);
                ghost.Retire();   // her corner empties as she lifts off

                var disc = _disc.rectTransform;
                var to = disc.TransformPoint(disc.rect.center);
                var size = disc.rect.size * disc.lossyScale.x * toCanvas;
                const float rise = 1.1f;
                fr.DOJump(to, 160f * top.lossyScale.x, 1, rise).SetEase(Ease.InOutSine).SetUpdate(true);
                fr.DOSizeDelta(size, rise).SetEase(Ease.InOutCubic).SetUpdate(true);
                _group.DOKill();
                _group.alpha = 0f;
                _group.DOFade(1f, 0.5f).SetDelay(rise * 0.5f).SetUpdate(true);
                yield return new WaitForSecondsRealtime(rise);
                fr.DOKill();
                Destroy(flyer.gameObject);
                _disc.rectTransform.DOPunchScale(new Vector3(0.3f, 0.3f, 0f), 0.5f, 6, 0.6f).SetUpdate(true);
                Sfx.Play(SfxKind.Heart, 0.5f);
            }
            else if (ghost != null) ghost.Retire();
            ShowCard(true, animate: _group.alpha < 1f);
            _handingOff = false;
            _says.Clear();   // her first word up here is where she'll be
            Say(ArrivalLine);
        }

        bool _arriving;

        /// <summary>
        /// Old Yuzu is flying up into the card, or saying her first line there: her corner and
        /// anything waiting its turn (Attention) hold off, so there's one place to look.
        /// </summary>
        public static bool Arriving => Instance != null && Instance._arriving;

        /// <summary>As wide as fits beside the round buttons on the right, up to a comfortable read.</summary>
        void Fit()
        {
            var root = (RectTransform)transform;
            var parent = root.parent as RectTransform;
            float available = parent != null ? parent.rect.width - 16f - ProfileBadge.Size - 12f - 150f : 620f;
            root.sizeDelta = Size(available);
        }

        /// <summary>How far down the card reaches when it is just showing a goal (not speaking), for what sits under it.</summary>
        public float SettledHeight => _collapsed ? CollapsedSize : (_tall ? 168f : 124f) * Accessibility.TextScale;

        Vector2 Size(float available) => _collapsed
            ? new Vector2(CollapsedSize, CollapsedSize)
            // Easy Reading's bigger words get a taller card to grow into (a goal is a line or two).
            : new Vector2(Mathf.Clamp(available, 380f, 640f), _speaking != null ? _speakingHeight : (_tall ? 168f : 124f) * Accessibility.TextScale);

        void Update()
        {
            TickAway();
            TickSpeech();
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            Refresh();
        }

        // ---------------------------------------------------------------- Old Yuzu's word

        readonly System.Collections.Generic.Queue<(string line, string goal)> _says = new();
        string _speaking;
        float _speakLeft, _speakingHeight = 124f;

        /// <summary>
        /// Old Yuzu says something from the card: it grows to hold the line, her face in the
        /// icon, then settles back to the goal once there's been time to read it. Lines wait for
        /// a free screen (no window or movie in front), and while the card is tucked away.
        /// </summary>
        public void Say(string line) => Say(line, null);

        /// <summary>A line about one goal, dropped if the goal has moved on before she gets to it.</summary>
        void Say(string line, string goal)
        {
            if (string.IsNullOrEmpty(line) || line == _speaking) return;
            foreach (var s in _says) if (s.line == line) return;
            while (_says.Count >= 3) _says.Dequeue();
            _says.Enqueue((line, goal));
        }

        void TickSpeech()
        {
            if (_speaking != null)
            {
                // Read time only counts while she can be read: not behind a window or a movie.
                if (!Attention.ScreenBusy) _speakLeft -= Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                if (_speakLeft <= 0f) EndSpeech();
                return;
            }
            if (_says.Count == 0 || _away || _handingOff || _collapsed || Attention.ScreenBusy) return;
            var (line, goal) = _says.Dequeue();
            if (goal != null && goal != _shown) return;   // overtaken: the goal it was about is gone
            _speaking = line;
            _speakLeft = (3f + 0.06f * line.Length) * (Accessibility.InstantText ? 1.5f : 1f);
            // Tall enough for the line as it will be drawn (a translation can run longer).
            float width = Mathf.Max(100f, ((RectTransform)transform).rect.width - 158f);
            float words = _title.GetPreferredValues(Loc.T(line), width, 0f).y;
            _speakingHeight = Mathf.Clamp(words + 72f, 124f, 300f);
            _caption.text = StoryText.Guide.ToUpperInvariant();
            _caption.color = Color.Lerp(UiKit.Spring, UiKit.Ink, 0.15f);
            _disc.color = UiKit.Spring;
            SetIcon(YuzuFace, IconPainter.Star);
            _title.text = line;
            Resize();
            _card.DOKill();
            _card.localScale = Vector3.one;
            _card.DOPunchScale(new Vector3(0.06f, 0.06f, 0f), 0.45f, 6, 0.6f).SetUpdate(true);
        }

        void EndSpeech()
        {
            if (_speaking == ArrivalLine) _arriving = false;
            _speaking = null;
            _title.text = _shown ?? "";
            Resize();
            Refresh();
        }

        /// <summary>Grows or shrinks to the size it should be now, smoothly.</summary>
        void Resize()
        {
            var root = (RectTransform)transform;
            var before = root.sizeDelta;
            Fit();
            var after = root.sizeDelta;
            root.sizeDelta = before;
            root.DOKill();
            root.DOSizeDelta(after, 0.3f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        static Sprite YuzuFace => GhostDialogue.Instance != null ? GhostDialogue.Instance.Face : null;

        void SetIcon(Sprite portrait, Sprite fallback)
        {
            bool face = portrait != null;
            _icon.sprite = face ? portrait : fallback;
            float pad = face ? 6f : 16f;
            UiKit.Stretch(_icon.rectTransform, pad, pad, pad, pad);
        }

        /// <summary>
        /// What Old Yuzu says as a goal turns up, if it's worth a word: someone with news or a
        /// reward, a present, everything done. Most goals just bounce.
        /// </summary>
        static string VoiceFor(Objectives.Goal goal) => goal.Source switch
        {
            Objectives.Source.Reward when !string.IsNullOrEmpty(goal.From) => string.Format("{0} has a reward waiting for you!", goal.From),
            Objectives.Source.Talk when !string.IsNullOrEmpty(goal.From) => string.Format("{0} would like a word with you.", goal.From),
            Objectives.Source.Gift => "A present is waiting for you today!",
            Objectives.Source.Done => "That's every goal done! Now you can just enjoy your island.",
            _ => null,
        };

        void Refresh()
        {
            var goal = Objectives.Current();
            var (caption, colour, icon) = goal.Source switch
            {
                Objectives.Source.Tutorial => ("GETTING STARTED", UiKit.Spring, IconPainter.Star),
                Objectives.Source.Reward => ("REWARD READY", UiKit.Sun, IconPainter.Gift),
                Objectives.Source.Talk => ("NEW QUEST", UiKit.Primary, IconPainter.Goals),
                Objectives.Source.Quest => (("QUEST · " + goal.From).ToUpperInvariant(), UiKit.Primary, IconPainter.Goals),
                Objectives.Source.Done => ("ALL DONE", UiKit.Leaf, IconPainter.Check),
                Objectives.Source.Visit => (("VISITING · " + goal.From).ToUpperInvariant(), UiKit.Hex("5B9BD5"), IconPainter.Plane),
                Objectives.Source.Gift => ("DAILY GIFT", UiKit.Primary, IconPainter.Gift),
                Objectives.Source.Order => ("FOOD ORDER", UiKit.Leaf, IconPainter.Goals),
                Objectives.Source.Daily => ("TODAY · " + goal.From, UiKit.Spring, IconPainter.Star),
                Objectives.Source.Contest => ("CONTEST", UiKit.Sun, IconPainter.Trophy),
                Objectives.Source.Gathering => (Gathering.NameOf(Gathering.Kind).ToUpperInvariant(), UiKit.Hex("E58FB0"), Gathering.IsContest ? IconPainter.Trophy : IconPainter.Star),
                _ => ("NEXT GOAL", UiKit.Danger, IconPainter.Heart),
            };

            string text = goal.Step == null
                ? goal.Title
                : goal.Title + "\n<size=78%><color=#C9653A>" + goal.Step + "</color></size>";
            bool changed = _shown != text, first = _shown == null;
            _shown = text;
            _tall = goal.Step != null;
            if (changed && !first && !_away && !_handingOff) Say(VoiceFor(goal), text);

            // While she's speaking the card is hers; it shows the goal again when she's done.
            if (_speaking != null) return;
            _caption.text = caption;
            _caption.color = Color.Lerp(colour, UiKit.Ink, 0.15f);
            _disc.color = colour;
            // Whoever asked; a goal nobody in particular asked for is Old Yuzu's. A visit, a food
            // order and a party keep their own pictures.
            bool hers = goal.Source is not (Objectives.Source.Visit or Objectives.Source.Order or Objectives.Source.Gathering);
            SetIcon(goal.Icon != null ? goal.Icon : hers ? YuzuFace : null, icon);

            if (!changed) return;
            _title.text = text;
            Fit();
            if (first) return;

            // Tucked away: just the icon bounces, so you know something changed.
            if (_collapsed)
            {
                _disc.rectTransform.DOKill();
                _disc.rectTransform.localScale = Vector3.one;
                _disc.rectTransform.DOPunchScale(new Vector3(0.25f, 0.25f, 0f), 0.5f, 6, 0.6f).SetUpdate(true);
                Sfx.Play(SfxKind.Coin, 0.4f);
                return;
            }

            // A new goal: bounce, and a chime if the old one was just finished.
            _card.DOKill();
            _card.localScale = Vector3.one;
            _card.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.45f, 6, 0.6f).SetUpdate(true);
            Sfx.Play(SfxKind.Coin, 0.5f);
        }

        // ---------------------------------------------------------------- tucking away

        void SetCollapsed(bool collapsed)
        {
            if (_collapsed == collapsed) return;
            _collapsed = collapsed;
            PlayerPrefs.SetInt(CollapsedKey, collapsed ? 1 : 0);
            PlayerPrefs.Save();
            Sfx.Play(SfxKind.Tap, 0.4f);
            ApplyCollapsed(animate: true);
        }

        void ApplyCollapsed(bool animate)
        {
            bool show = !_collapsed;
            _caption.gameObject.SetActive(show);
            _title.gameObject.SetActive(show);
            _chevronGo.SetActive(show);
            var root = (RectTransform)transform;
            var before = root.sizeDelta;
            Fit();
            if (!animate) return;
            var after = root.sizeDelta;
            root.sizeDelta = before;
            root.DOKill();
            root.DOSizeDelta(after, 0.25f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        public void OnBeginDrag(PointerEventData e) => _dragX = 0f;

        public void OnDrag(PointerEventData e) => _dragX += e.delta.x;

        public void OnEndDrag(PointerEventData e)
        {
            if (_dragX < -40f) SetCollapsed(true);
            else if (_dragX > 40f) SetCollapsed(false);
        }

        void OpenList()
        {
            if (_collapsed) { SetCollapsed(false); return; }
            if (_speaking != null) { EndSpeech(); return; }   // a tap while she speaks just lets her finish
            // Always the quests: what to do next. Achievements are one tap away (and the tower opens on them).
            ObjectivesScreen.Show(ObjectivesScreen.Tab.Quests);
        }
    }
}
