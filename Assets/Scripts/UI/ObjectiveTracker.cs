using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The current goal, pinned under the resource bar the way most mobile games do it: an icon,
    /// who asked, and what to do. It refreshes twice a second and bounces when the goal changes.
    /// Tap it for the full list.
    /// </summary>
    public class ObjectiveTracker : MonoBehaviour
    {
        RectTransform _card;
        Image _disc, _icon;
        TextMeshProUGUI _caption, _title;
        string _shown;
        float _next;

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
            root.anchoredPosition = new Vector2(16f, -104f);

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

            var chevron = UiKit.Image(_card, "More", IconPainter.Arrow, UiKit.InkSoft);
            UiKit.Place(chevron.rectTransform, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(26f, 26f));

            Core.ScreenMetrics.Changed += Fit;
            Fit();
        }

        void OnDestroy() => Core.ScreenMetrics.Changed -= Fit;

        /// <summary>As wide as fits beside the round buttons on the right, up to a comfortable read.</summary>
        void Fit()
        {
            var root = (RectTransform)transform;
            var parent = root.parent as RectTransform;
            float available = parent != null ? parent.rect.width - 16f - 150f : 620f;
            root.sizeDelta = new Vector2(Mathf.Clamp(available, 380f, 640f), 124f);
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
                _ => ("NEXT GOAL", UiKit.Danger, IconPainter.Heart),
            };

            _caption.text = caption;
            _caption.color = Color.Lerp(colour, UiKit.Ink, 0.15f);
            _disc.color = colour;
            bool portrait = goal.Icon != null;
            _icon.sprite = portrait ? goal.Icon : icon;
            UiKit.Stretch(_icon.rectTransform, portrait ? 6f : 16f, portrait ? 6f : 16f, portrait ? 6f : 16f, portrait ? 6f : 16f);

            if (_shown == goal.Title) return;
            bool first = _shown == null;
            _shown = goal.Title;
            _title.text = goal.Title;
            if (first) return;

            // A new goal: bounce, and a chime if the old one was just finished.
            _card.DOKill();
            _card.localScale = Vector3.one;
            _card.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.45f, 6, 0.6f).SetUpdate(true);
            Sfx.Play(SfxKind.Coin, 0.5f);
        }

        void OpenList()
        {
            var goal = Objectives.Current();
            ObjectivesScreen.Show(goal.Source == Objectives.Source.Achievement || goal.Source == Objectives.Source.Done
                ? ObjectivesScreen.Tab.Achievements
                : ObjectivesScreen.Tab.Quests);
        }
    }
}
