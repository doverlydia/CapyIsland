using System;
using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Easy Reading and language first — the things a player who is struggling needs to find —
    /// then sound, the tour, starting a fresh island, and credits.
    /// </summary>
    public class SettingsScreen : UiScreen
    {
        public static SettingsScreen Instance { get; private set; }

        const string SoundKey = "CapyIsland.Sound";
        const string ArrowLabelsKey = "CapyIsland.ArrowLabels";

        protected override string Title => "Settings";
        protected override Sprite TitleIcon => IconPainter.Gear;
        protected override Color AccentColour => UiKit.Hex("6B8FA8");
        protected override float MaxWidth => 900f;

        GameObject _confirm;
        RectTransform _account;
        GameObject _accountButton;
        GameObject _easyOptions;
        TextMeshProUGUI _sizeValue, _sizeSample;
        readonly List<(Language lang, Image pill, TextMeshProUGUI label)> _languages = new();
        readonly List<Action> _refresh = new();

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

        /// <summary>A word under each edge arrow saying what it points at. Off unless asked for.</summary>
        public static bool ArrowLabels
        {
            get => PlayerPrefs.GetInt(ArrowLabelsKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(ArrowLabelsKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Applies the saved sound setting at startup.</summary>
        public static void ApplySaved() => AudioListener.volume = SoundOn ? 1f : 0f;

        void Awake() => Instance = this;

        void OnEnable()
        {
            Accessibility.Changed += Refresh;
            Loc.Changed += Refresh;
            CloudSync.AccountChanged += Refresh;
        }

        void OnDisable()
        {
            Accessibility.Changed -= Refresh;
            Loc.Changed -= Refresh;
            CloudSync.AccountChanged -= Refresh;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override void Build(RectTransform body)
        {
            var scroll = UiKit.ScrollList(body, "List", out var list, 18f);
            UiKit.Stretch((RectTransform)scroll.transform);

            // ---- Easy Reading: one big switch, with its options folded underneath.
            var easy = Row(list, IconPainter.Magnifier, "Easy Reading");
            Toggle(easy, () => Accessibility.Enabled, v => Accessibility.Enabled = v);
            var easyNote = UiKit.Text(list, "EasyNote",
                "Bigger text, and tap any text to read it in a large window.", 28f, UiKit.InkSoft);
            easyNote.overflowMode = TextOverflowModes.Overflow;
            easyNote.margin = new Vector4(16f, -6f, 16f, 0f);

            _easyOptions = UiKit.Rect("EasyOptions", list).gameObject;
            var ev = _easyOptions.AddComponent<VerticalLayoutGroup>();
            ev.spacing = 14f;
            ev.padding = new RectOffset(24, 0, 0, 0);
            ev.childControlWidth = ev.childControlHeight = true;
            ev.childForceExpandWidth = true;
            ev.childForceExpandHeight = false;
            var options = (RectTransform)_easyOptions.transform;

            var size = Row(options, IconPainter.Magnifier, "Text size");
            Stepper(size);
            _sizeSample = UiKit.Text(options, "Sample", "This is how big the words will be.", 34f, UiKit.Ink, false, TextAlignmentOptions.Center);
            _sizeSample.overflowMode = TextOverflowModes.Overflow;
            _sizeSample.margin = new Vector4(12f, 4f, 12f, 4f);

            var read = Row(options, IconPainter.Book, "Tap text to read it");
            Toggle(read, () => Accessibility.TapToReadSetting, v => Accessibility.TapToReadSetting = v);
            var readNote = UiKit.Text(options, "ReadNote",
                "On buttons, press and hold the words instead.", 26f, UiKit.InkSoft);
            readNote.overflowMode = TextOverflowModes.Overflow;
            readNote.margin = new Vector4(16f, -6f, 16f, 0f);

            var instant = Row(options, IconPainter.Sparkle, "Show whole sentences at once");
            Toggle(instant, () => Accessibility.InstantTextSetting, v => Accessibility.InstantTextSetting = v);

            var grab = Row(options, IconPainter.Paw, "Bigger things to drag");
            Toggle(grab, () => Accessibility.BigGrabSetting, v => Accessibility.BigGrabSetting = v);

            // ---- Language.
            var lang = Row(list, IconPainter.Globe, "Language");
            var pills = UiKit.Rect("Languages", lang);
            var ph = pills.gameObject.AddComponent<HorizontalLayoutGroup>();
            ph.spacing = 10f;
            ph.childControlWidth = ph.childControlHeight = true;
            ph.childForceExpandWidth = ph.childForceExpandHeight = false;
            foreach (Language l in Enum.GetValues(typeof(Language)))
            {
                var language = l;
                var pill = UiKit.Image(pills, l.ToString(), UiKit.Rounded(30), UiKit.SurfaceSunk, true);
                var ple = pill.gameObject.AddComponent<LayoutElement>();
                ple.preferredWidth = 150f; ple.preferredHeight = 72f;
                var t = UiKit.Text(pill.transform, "Label", Loc.NativeName(l), 32f, UiKit.Ink, true, TextAlignmentOptions.Center);
                UiKit.Stretch(t.rectTransform);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.overflowMode = TextOverflowModes.Overflow;
                t.GetComponent<LocText>().NoScale = true;
                var b = pill.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { Sfx.Play(SfxKind.Tap, 0.5f); Loc.Set(language); });
                pill.gameObject.AddComponent<PressSquash>();
                _languages.Add((l, pill, t));
            }

            // ---- Sound.
            var sound = Row(list, IconPainter.Sound, "Sound");
            Toggle(sound, () => SoundOn, v => SoundOn = v);
            VolumeRow(list, "Music", () => AudioMix.Music, v => AudioMix.Music = v);
            VolumeRow(list, "Effects", () => AudioMix.Effects, v => AudioMix.Effects = v);
            VolumeRow(list, "Ambience", () => AudioMix.Ambience, v => AudioMix.Ambience = v);

            // ---- Words on the edge arrows.
            var arrows = Row(list, IconPainter.Arrow, "Names on edge arrows");
            Toggle(arrows, () => ArrowLabels, v => ArrowLabels = v);

            // ---- Walking: a stick under the thumb for those who'd rather not reach across the screen.
            var stick = Row(list, IconPainter.Paw, "Walk with a joystick");
            Toggle(stick, () => Controls.Joystick, v => Controls.Joystick = v);
            var stickNote = UiKit.Text(list, "StickNote", "Either way, tap any spot to walk there.", 28f, UiKit.InkSoft);
            stickNote.overflowMode = TextOverflowModes.Overflow;
            stickNote.margin = new Vector4(16f, -6f, 16f, 0f);

            // ---- Teaching on or off ("End tutorial" switches it off; this brings it back).
            var guide = Row(list, IconPainter.Book, "Tutorial and tips");
            Toggle(guide, () => !Guidance.Off, v => Guidance.Off = !v);

            // ---- The tour of the screen.
            var tour = Row(list, IconPainter.Goals, "Show me around");
            var tourButton = UiKit.Button(tour, "Tour", "Start tour", UiKit.ButtonStyle.Spring, () =>
            {
                Close();
                HudTour.Begin();
            }, 28f);
            var tle = tourButton.transform.parent.gameObject.AddComponent<LayoutElement>();
            tle.preferredWidth = 220f; tle.preferredHeight = 72f;

            // ---- The opening movie, again.
            var intro = Row(list, IconPainter.Star, "The story so far");
            var introButton = UiKit.Button(intro, "Intro", "Watch", UiKit.ButtonStyle.Spring, () =>
            {
                Close();
                IntroCutscene.Show();
            }, 28f);
            var ile = introButton.transform.parent.gameObject.AddComponent<LayoutElement>();
            ile.preferredWidth = 220f; ile.preferredHeight = 72f;

            // ---- Your island as a code, to keep in a note, and a code loaded back.
            var keep = Row(list, IconPainter.Star, "Keep a copy of your island");
            var exportButton = UiKit.Button(keep, "Export", "Export", UiKit.ButtonStyle.Spring, SaveCodeScreen.ShowExport, 28f);
            var ele = exportButton.transform.parent.gameObject.AddComponent<LayoutElement>();
            ele.preferredWidth = 220f; ele.preferredHeight = 72f;

            var bring = Row(list, IconPainter.Star, "Bring back a saved island");
            var loadButton = UiKit.Button(bring, "Load", "Load", UiKit.ButtonStyle.Spring, () => SaveCodeScreen.ShowImport(true), 28f);
            var lle = loadButton.transform.parent.gameObject.AddComponent<LayoutElement>();
            lle.preferredWidth = 220f; lle.preferredHeight = 72f;

            // ---- An account, to open this island on another device (CloudSync).
            _account = Row(list, IconPainter.Star, "Play on another device");
            var accountButton = UiKit.Button(_account, "Account", "Make account", UiKit.ButtonStyle.Spring, AccountScreen.ShowMake, 28f);
            _accountButton = accountButton.transform.parent.gameObject;
            var ale = _accountButton.AddComponent<LayoutElement>();
            ale.preferredWidth = 220f; ale.preferredHeight = 72f;
            var signIn = Row(list, IconPainter.Star, "Have an account already?");
            var signInButton = UiKit.Button(signIn, "SignIn", "Sign in", UiKit.ButtonStyle.Secondary, AccountScreen.ShowSignIn, 28f);
            var sle = signInButton.transform.parent.gameObject.AddComponent<LayoutElement>();
            sle.preferredWidth = 220f; sle.preferredHeight = 72f;

            // ---- New island.
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
            RtlLayout.Add(h);
            var ic = UiKit.Image(row, "Icon", icon, UiKit.InkSoft);
            ic.preserveAspect = true;
            var le = ic.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = 54f;
            var t = UiKit.Text(row, "Label", label, 34f, UiKit.Ink, true);
            t.overflowMode = TextOverflowModes.Overflow;
            t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return row;
        }

        /// <summary>An on/off switch at the end of a row, with "On"/"Off" beside it.</summary>
        void Toggle(RectTransform row, Func<bool> get, Action<bool> set)
        {
            var state = UiKit.Text(row, "State", "", 30f, UiKit.InkSoft, true, TextAlignmentOptions.Right);
            state.textWrappingMode = TextWrappingModes.NoWrap;
            state.overflowMode = TextOverflowModes.Overflow;

            var track = UiKit.Image(row, "Toggle", UiKit.Rounded(30), UiKit.Leaf, true);
            var le = track.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 120f; le.preferredHeight = 64f;
            le.minWidth = 120f;
            var knob = UiKit.Image(track.transform, "Knob", UiKit.Circle, Color.white);
            knob.rectTransform.sizeDelta = new Vector2(52f, 52f);
            var b = track.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { set(!get()); Sfx.Play(SfxKind.Tap, 0.5f); Refresh(); });
            // The whole row flips the switch too: a bigger target.
            var rowButton = row.gameObject.AddComponent<Button>();
            rowButton.transition = Selectable.Transition.None;
            row.GetComponent<Image>().raycastTarget = true;
            rowButton.onClick.AddListener(() => { set(!get()); Sfx.Play(SfxKind.Tap, 0.5f); Refresh(); });

            _refresh.Add(() =>
            {
                bool on = get();
                state.text = on ? "On" : "Off";
                track.color = on ? UiKit.Leaf : UiKit.SurfaceSunk;
                var r = knob.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(on ? 1f : 0f, 0.5f);
                r.pivot = new Vector2(on ? 1f : 0f, 0.5f);
                r.anchoredPosition = new Vector2(on ? -6f : 6f, 0f);
            });
        }

        /// <summary>A label and a chunky slider, for one layer of sound.</summary>
        void VolumeRow(RectTransform list, string label, Func<float> get, Action<float> set)
        {
            var row = UiKit.Image(list, label, UiKit.Rounded(30), Color.white).rectTransform;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(98, 30, 14, 14);   // lines up under the Sound row's label
            h.spacing = 20f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().minHeight = 92f;
            RtlLayout.Add(h);
            var t = UiKit.Text(row, "Label", label, 30f, UiKit.Ink, true);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.gameObject.AddComponent<LayoutElement>().preferredWidth = 200f;

            var root = UiKit.Rect("Slider", row);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.preferredHeight = 56f;
            var track = UiKit.Image(root, "Track", UiKit.Rounded(14), UiKit.SurfaceSunk, true);
            track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(0f, 24f);
            var fillArea = UiKit.Rect("FillArea", root);
            UiKit.Stretch(fillArea, 0f, 16f, 0f, 16f);
            var fill = UiKit.Image(fillArea, "Fill", UiKit.Rounded(14), UiKit.Spring);
            fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = UiKit.Rect("HandleArea", root);
            UiKit.Stretch(handleArea, 24f, 0f, 24f, 0f);
            var handle = UiKit.Image(handleArea, "Handle", UiKit.Circle, Color.white, true);
            handle.rectTransform.sizeDelta = new Vector2(52f, 0f);
            UiKit.AddShadow(handle.rectTransform, 8f, 3f, 0.25f);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(get());
            slider.onValueChanged.AddListener(v => set(v));
            // Let go: a tap at the new level, so you hear what you chose.
            var trigger = root.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var up = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
            up.callback.AddListener(_ => Sfx.Play(SfxKind.Tap, 0.8f));
            trigger.triggers.Add(up);
            _refresh.Add(() => slider.SetValueWithoutNotify(get()));
        }

        /// <summary>Smaller / bigger buttons around the current text size.</summary>
        void Stepper(RectTransform row)
        {
            var minus = UiKit.Button(row, "Smaller", "A−", UiKit.ButtonStyle.Secondary, () => Accessibility.TextSizeStep--, 30f);
            var mle = minus.transform.parent.gameObject.AddComponent<LayoutElement>();
            mle.preferredWidth = mle.minWidth = 96f; mle.preferredHeight = 80f;
            _sizeValue = UiKit.Text(row, "Value", "", 32f, UiKit.Ink, true, TextAlignmentOptions.Center);
            _sizeValue.textWrappingMode = TextWrappingModes.NoWrap;
            _sizeValue.overflowMode = TextOverflowModes.Overflow;
            _sizeValue.GetComponent<LocText>().NoScale = true;
            var vle = _sizeValue.gameObject.AddComponent<LayoutElement>();
            vle.preferredWidth = vle.minWidth = 110f;
            var plus = UiKit.Button(row, "Bigger", "A+", UiKit.ButtonStyle.Primary, () => Accessibility.TextSizeStep++, 40f);
            var ple = plus.transform.parent.gameObject.AddComponent<LayoutElement>();
            ple.preferredWidth = ple.minWidth = 96f; ple.preferredHeight = 80f;
            _refresh.Add(() =>
            {
                _sizeValue.text = Mathf.RoundToInt(Accessibility.TextSizes[Accessibility.TextSizeStep] * 100f) + "%";
                minus.interactable = Accessibility.TextSizeStep > 0;
                plus.interactable = Accessibility.TextSizeStep < Accessibility.TextSizes.Length - 1;
            });
        }

        protected override void OnOpen()
        {
            _confirm.SetActive(false);
            Refresh();
        }

        void Refresh()
        {
            if (_easyOptions == null) return;
            foreach (var r in _refresh) r();
            // The account row: an offer to make one, or whose island this is.
            if (_account != null)
            {
                var label = _account.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (label != null)
                    label.text = CloudSync.Username != null ? Loc.F("Signed in as {0}", CloudSync.Username)
                        : CloudSync.HasGoogle ? "Signed in with Google"
                        : "Play on another device";
                _accountButton.SetActive(!CloudSync.Linked);
            }
            _easyOptions.SetActive(Accessibility.Enabled);
            foreach (var (l, pill, label) in _languages)
            {
                bool on = Loc.Current == l;
                pill.color = on ? AccentColour : UiKit.SurfaceSunk;
                label.color = on ? Color.white : UiKit.Ink;
            }
        }

        /// <summary>Erases the save and reloads the island from scratch.</summary>
        static void StartOver()
        {
            SaveGame.Suspended = true;
            SaveGame.Delete();
            HudTour.ResetAll();   // a fresh island starts with the welcome page and its tips
            SaveGame.ResetStatics();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
