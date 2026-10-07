using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>Sound on or off, starting a fresh island, and credits.</summary>
    public class SettingsScreen : UiScreen
    {
        public static SettingsScreen Instance { get; private set; }

        const string SoundKey = "CapyIsland.Sound";

        protected override string Title => "Settings";
        protected override Sprite TitleIcon => IconPainter.Gear;
        protected override Color AccentColour => UiKit.Hex("6B8FA8");
        protected override float MaxWidth => 900f;

        TextMeshProUGUI _soundState;
        Image _soundKnob, _soundTrack;
        GameObject _confirm;

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
                PlayerPrefs.Save();
                AudioListener.volume = value ? 1f : 0f;
            }
        }

        /// <summary>Applies the saved sound setting at startup.</summary>
        public static void ApplySaved() => AudioListener.volume = SoundOn ? 1f : 0f;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override void Build(RectTransform body)
        {
            var scroll = UiKit.ScrollList(body, "List", out var list, 18f);
            UiKit.Stretch((RectTransform)scroll.transform);

            // Sound toggle row.
            var row = Row(list, IconPainter.Sound, "Sound");
            var toggle = UiKit.Image(row, "Toggle", UiKit.Rounded(30), UiKit.Leaf, true);
            _soundTrack = toggle;
            var le = toggle.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 120f; le.preferredHeight = 64f;
            _soundKnob = UiKit.Image(toggle.transform, "Knob", UiKit.Circle, Color.white);
            _soundKnob.rectTransform.sizeDelta = new Vector2(52f, 52f);
            var b = toggle.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { SoundOn = !SoundOn; Game.Sfx.Play(Game.SfxKind.Tap, 0.5f); RefreshSound(); });
            _soundState = UiKit.Text(row, "State", "", 30f, UiKit.InkSoft, true, TextAlignmentOptions.Right);
            _soundState.textWrappingMode = TextWrappingModes.NoWrap;
            _soundState.overflowMode = TextOverflowModes.Overflow;
            _soundState.transform.SetSiblingIndex(toggle.transform.GetSiblingIndex());

            // New island.
            var reset = Row(list, IconPainter.Star, "Start a new island");
            var resetButton = UiKit.Button(reset, "Reset", "Start over", UiKit.ButtonStyle.Secondary, () => _confirm.SetActive(true), 28f);
            var rle = resetButton.transform.parent.gameObject.AddComponent<LayoutElement>();
            rle.preferredWidth = 220f; rle.preferredHeight = 72f;

            _confirm = UiKit.Rect("Confirm", list).gameObject;
            var cv = _confirm.AddComponent<VerticalLayoutGroup>();
            cv.spacing = 14f;
            cv.padding = new RectOffset(24, 24, 20, 24);
            cv.childControlWidth = cv.childControlHeight = true;
            cv.childForceExpandWidth = true;
            cv.childForceExpandHeight = false;
            var bg = _confirm.AddComponent<Image>();
            bg.sprite = UiKit.Rounded(30);
            bg.type = Image.Type.Sliced;
            bg.color = UiKit.Hex("FDE3DD");
            var warn = UiKit.Text(_confirm.transform, "Warning",
                "This erases your island, your capybaras and every goal you've earned. It can't be undone. Your wardrobe and daily streak stay with you.", 30f, UiKit.Ink);
            warn.overflowMode = TextOverflowModes.Overflow;
            var buttons = UiKit.Rect("Buttons", _confirm.transform);
            var bh = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            bh.spacing = 16f;
            bh.childControlWidth = bh.childControlHeight = true;
            bh.childForceExpandWidth = true;
            buttons.gameObject.AddComponent<LayoutElement>().preferredHeight = 80f;
            UiKit.Button(buttons, "Keep", "Keep my island", UiKit.ButtonStyle.Secondary, () => _confirm.SetActive(false), 28f);
            var erase = UiKit.Button(buttons, "Erase", "Erase it", UiKit.ButtonStyle.Primary, StartOver, 28f);
            erase.GetComponent<Image>().color = UiKit.Danger;
            _confirm.SetActive(false);

            // Credits.
            var credits = UiKit.Text(list, "Credits",
                "<b>Capy Island</b>\nFonts: Fredoka and Nunito, under the SIL Open Font License.", 26f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            credits.overflowMode = TextOverflowModes.Overflow;
            credits.margin = new Vector4(0f, 24f, 0f, 0f);
        }

        static RectTransform Row(RectTransform list, Sprite icon, string label)
        {
            var row = UiKit.Image(list, label, UiKit.Rounded(30), Color.white).rectTransform;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(24, 24, 18, 18);
            h.spacing = 20f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().minHeight = 110f;
            var ic = UiKit.Image(row, "Icon", icon, UiKit.InkSoft);
            ic.preserveAspect = true;
            var le = ic.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = 54f;
            var t = UiKit.Text(row, "Label", label, 34f, UiKit.Ink, true);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return row;
        }

        protected override void OnOpen()
        {
            _confirm.SetActive(false);
            RefreshSound();
        }

        void RefreshSound()
        {
            bool on = SoundOn;
            _soundState.text = on ? "On" : "Off";
            _soundTrack.color = on ? UiKit.Leaf : UiKit.SurfaceSunk;
            var r = _soundKnob.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(on ? 1f : 0f, 0.5f);
            r.pivot = new Vector2(on ? 1f : 0f, 0.5f);
            r.anchoredPosition = new Vector2(on ? -6f : 6f, 0f);
        }

        /// <summary>Erases the save and reloads the island from scratch.</summary>
        static void StartOver()
        {
            SaveGame.Suspended = true;
            SaveGame.Delete();
            SaveGame.ResetStatics();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
