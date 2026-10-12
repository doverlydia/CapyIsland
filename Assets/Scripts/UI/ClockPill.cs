using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The island's time, under your profile picture: a sun (or a moon after dark) and the hour.
    /// Hold it to read what the time of day means — some fish, birds and bugs keep hours.
    /// </summary>
    public class ClockPill : MonoBehaviour
    {
        Image _icon;
        TextMeshProUGUI _time;
        string _shown;

        public static ClockPill Create(Transform parent)
        {
            var root = UiKit.Rect("Clock", parent);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 1f);
            // Under the profile badge and its name.
            root.anchoredPosition = new Vector2(16f, -104f - ProfileBadge.Size - 52f);
            root.sizeDelta = new Vector2(ProfileBadge.Size + 24f, 52f);

            var pill = UiKit.Image(root, "Pill", UiKit.Rounded(26), UiKit.HudGlass, true);
            UiKit.Stretch(pill.rectTransform);
            var clock = root.gameObject.AddComponent<ClockPill>();
            clock._icon = UiKit.Image(root, "Icon", IconPainter.Sun, UiKit.Sun);
            UiKit.Place(clock._icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(34f, 34f));
            clock._time = UiKit.Text(root, "Time", "", 28f, Color.white, true, TextAlignmentOptions.Center);
            UiKit.Stretch(clock._time.rectTransform, 44f, 0f, 6f, 0f);
            clock._time.textWrappingMode = TextWrappingModes.NoWrap;
            clock._time.overflowMode = TextOverflowModes.Overflow;
            var loc = clock._time.GetComponent<LocText>();
            loc.NoScale = loc.NoRead = true;
            CounterInfo.Add(root, pill);
            return clock;
        }

        void Update()
        {
            var text = GameClock.Clock;
            if (text == _shown) return;
            _shown = text;
            _time.text = text;
            bool night = GameClock.IsNight;
            _icon.sprite = night ? IconPainter.Moon : IconPainter.Sun;
            _icon.color = night ? UiKit.Hex("DCE3FF") : UiKit.Sun;
        }
    }
}
