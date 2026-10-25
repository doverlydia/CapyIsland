using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Your face in the top-left corner: a round portrait of your capybara as it is dressed, with
    /// your name underneath. Tap it for your profile.
    /// </summary>
    public class ProfileBadge : MonoBehaviour
    {
        public const float Size = 104f;

        TextMeshProUGUI _name;

        public static ProfileBadge Create(Transform parent)
        {
            var root = UiKit.Rect("ProfileBadge", parent);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(16f, -104f);
            root.sizeDelta = new Vector2(Size, Size + 34f);
            var badge = root.gameObject.AddComponent<ProfileBadge>();
            badge.Build(root);
            return badge;
        }

        void Build(RectTransform root)
        {
            // The disc clips a close-up of the portrait to a circle.
            var disc = UiKit.Image(root, "Disc", UiKit.Circle, UiKit.Hex("FBE3EC"), true);
            UiKit.Place(disc.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(Size, Size));
            UiKit.AddShadow(disc.rectTransform, 14f, 6f, 0.3f);
            disc.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            var face = CapyPortrait.Create(disc.transform, "Face", Size * 1.75f);
            // Frame the head: the capybara's face sits up and to the left of its middle.
            face.Rect.anchorMin = face.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            face.Rect.pivot = new Vector2(0.42f, 0.66f);
            face.Rect.anchoredPosition = new Vector2(0f, -6f);

            var ring = UiKit.Image(root, "Ring", IconPainter.Ring, UiKit.Hex("E58FB0"));
            UiKit.Place(ring.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(Size, Size));
            ring.raycastTarget = false;

            var pill = UiKit.Image(root, "NamePill", UiKit.Rounded(16), UiKit.HudGlass);
            UiKit.Place(pill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(Size + 20f, 36f));
            pill.raycastTarget = false;
            _name = UiKit.Text(pill.transform, "Name", PlayerProfile.Name, 24f, Color.white, true, TextAlignmentOptions.Center);
            UiKit.Stretch(_name.rectTransform, 6f, 0f, 6f, 0f);
            _name.textWrappingMode = TextWrappingModes.NoWrap;
            _name.enableAutoSizing = true;
            _name.fontSizeMin = 14f;
            _name.fontSizeMax = 24f;
            GrowsWithText.Add(pill.rectTransform, tall: 1f, wide: 0.2f);   // room for Easy Reading's bigger name

            var button = disc.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                Sfx.Play(SfxKind.Tap, 0.5f);
                ProfileScreen.Show();
            });
            disc.gameObject.AddComponent<PressSquash>();
        }

        void OnEnable() => PlayerProfile.Changed += Refresh;
        void OnDisable() => PlayerProfile.Changed -= Refresh;

        void Refresh()
        {
            if (_name != null) _name.text = PlayerProfile.Name;
        }
    }
}
