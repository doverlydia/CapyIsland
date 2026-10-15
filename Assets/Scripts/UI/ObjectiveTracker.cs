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
    /// </summary>
    public class ObjectiveTracker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
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
            Fit();
        }

        void OnDestroy() => Core.ScreenMetrics.Changed -= Fit;

        /// <summary>As wide as fits beside the round buttons on the right, up to a comfortable read.</summary>
        void Fit()
        {
            var root = (RectTransform)transform;
            var parent = root.parent as RectTransform;
            float available = parent != null ? parent.rect.width - 16f - ProfileBadge.Size - 12f - 150f : 620f;
            root.sizeDelta = _collapsed
                ? new Vector2(CollapsedSize, CollapsedSize)
                : new Vector2(Mathf.Clamp(available, 380f, 640f), _tall ? 168f : 124f);
        }

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            Refresh();
        }

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

            _caption.text = caption;
            _caption.color = Color.Lerp(colour, UiKit.Ink, 0.15f);
            _disc.color = colour;
            bool portrait = goal.Icon != null;
            _icon.sprite = portrait ? goal.Icon : icon;
            UiKit.Stretch(_icon.rectTransform, portrait ? 6f : 16f, portrait ? 6f : 16f, portrait ? 6f : 16f, portrait ? 6f : 16f);

            string text = goal.Step == null
                ? goal.Title
                : goal.Title + "\n<size=78%><color=#C9653A>" + goal.Step + "</color></size>";
            if (_shown == text) return;
            bool first = _shown == null;
            _shown = text;
            _title.text = text;
            _tall = goal.Step != null;
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
            // Always the quests: what to do next. Achievements are one tap away (and the tower opens on them).
            ObjectivesScreen.Show(ObjectivesScreen.Tab.Quests);
        }
    }
}
