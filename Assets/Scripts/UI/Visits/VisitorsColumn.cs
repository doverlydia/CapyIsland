using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Who else is on this island right now, as round faces under your profile badge. At home,
    /// tap a visitor to ask them to leave; on someone else's island, tap your host to add them
    /// as a friend (or unfriend them).
    /// </summary>
    public class VisitorsColumn : MonoBehaviour
    {
        const float Size = 84f;

        RectTransform _face;
        TextMeshProUGUI _name;
        VisitorBot _shown;
        float _next;

        public static VisitorsColumn Create(Transform parent)
        {
            var root = UiKit.Rect("Visitors", parent);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(16f + (ProfileBadge.Size - Size) * 0.5f, -104f - ProfileBadge.Size - 46f);
            root.sizeDelta = new Vector2(Size, Size + 30f);
            var c = root.gameObject.AddComponent<VisitorsColumn>();
            return c;
        }

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            var bot = VisitorBot.Current;
            if (bot != null && bot.Leaving) bot = null;
            if (bot == _shown) return;
            _shown = bot;
            Rebuild();
        }

        void Rebuild()
        {
            if (_face != null) Destroy(_face.gameObject);
            _face = null;
            if (_shown == null) return;

            var who = _shown.Who;
            _face = UiKit.Rect("Face", transform);
            UiKit.Stretch(_face);
            var avatar = UiKit.Avatar(_face, who, Size);
            avatar.anchorMin = avatar.anchorMax = avatar.pivot = new Vector2(0.5f, 1f);
            avatar.anchoredPosition = Vector2.zero;
            var dot = UiKit.Image(_face, "Online", UiKit.Circle, UiKit.Hex("5DBB63"));
            UiKit.Place(dot.rectTransform, new Vector2(1f, 1f), new Vector2(-4f, -Size + 22f), new Vector2(22f, 22f));
            dot.raycastTarget = false;

            var pill = UiKit.Image(_face, "NamePill", UiKit.Rounded(14), UiKit.HudGlass);
            UiKit.Place(pill.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(Size + 30f, 30f));
            pill.raycastTarget = false;
            _name = UiKit.Text(pill.transform, "Name", who.name, 20f, Color.white, true, TextAlignmentOptions.Center);
            UiKit.Stretch(_name.rectTransform, 4f, 0f, 4f, 0f);
            _name.enableAutoSizing = true;
            _name.fontSizeMin = 12f;
            _name.fontSizeMax = 20f;
            _name.textWrappingMode = TextWrappingModes.NoWrap;

            var hit = UiKit.Image(_face, "Hit", UiKit.Circle, new Color(0f, 0f, 0f, 0f));
            UiKit.Place(hit.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(Size, Size));
            var b = hit.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(Tapped);
            hit.gameObject.AddComponent<PressSquash>();

            _face.localScale = Vector3.one * 0.3f;
            _face.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        void Tapped()
        {
            var bot = _shown;
            if (bot == null) return;
            var who = bot.Who;
            Sfx.Play(SfxKind.Tap, 0.5f);
            if (bot.As == VisitorBot.Role.Guest)
            {
                PromptScreen.Ask("Visitor", who, $"{who.name} is visiting your island and helping out.", "Ask to leave", "Let them stay", () =>
                {
                    bot.SendHome();
                    Hints.Show($"{who.name} waves goodbye and heads home.");
                });
                return;
            }
            // On their island.
            if (Friends.IsFriend(who.id))
                PromptScreen.Ask("Your friend", who, $"You're on {who.name}'s island.", "Unfriend", "Close", () =>
                {
                    Friends.Remove(who);
                    Hints.Show(who.name + " is no longer your friend.");
                });
            else
                PromptScreen.Ask("Make a friend?", who, $"You're on {who.name}'s island. Add them as a friend to visit again — their island will be kept.", "Add friend", "Not now", () =>
                {
                    Visit.BefriendHost();
                    Hints.Show(who.name + " is your friend now!");
                });
        }
    }
}
