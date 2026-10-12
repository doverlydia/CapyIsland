using System;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// Easy Reading: one switch that turns on everything below, so it is one tap to opt in and one
    /// to opt out. Inside it, the text size and each helper can be tuned; turning the switch off
    /// puts the game back exactly as it was without forgetting those choices.
    ///
    ///  * Text size — every label in the game grows by this factor.
    ///  * Tap to read — tap any text (hold it, if it is on a button) to open it large in a
    ///    scrolling reader.
    ///  * Instant text — Old Yuzu's lines appear whole instead of typing out, and stay longer.
    ///  * Bigger grab — things in your trail are drawn larger and easier to pick up.
    /// </summary>
    public static class Accessibility
    {
        const string Prefix = "CapyIsland.A11y.";

        public static event Action Changed;

        /// <summary>Bumped on every change, so texts can tell their sizing is stale.</summary>
        public static int Version { get; private set; } = 1;

        public static readonly float[] TextSizes = { 1f, 1.25f, 1.5f, 1.75f, 2f };

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(Prefix + "On", 0) == 1;
            set => SetInt("On", value ? 1 : 0);
        }

        /// <summary>Index into <see cref="TextSizes"/> chosen for Easy Reading (default 150%).</summary>
        public static int TextSizeStep
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Size", 2), 0, TextSizes.Length - 1);
            set => SetInt("Size", Mathf.Clamp(value, 0, TextSizes.Length - 1));
        }

        public static bool TapToReadSetting
        {
            get => PlayerPrefs.GetInt(Prefix + "Read", 1) == 1;
            set => SetInt("Read", value ? 1 : 0);
        }

        public static bool InstantTextSetting
        {
            get => PlayerPrefs.GetInt(Prefix + "Instant", 1) == 1;
            set => SetInt("Instant", value ? 1 : 0);
        }

        public static bool BigGrabSetting
        {
            get => PlayerPrefs.GetInt(Prefix + "Grab", 1) == 1;
            set => SetInt("Grab", value ? 1 : 0);
        }

        // What the game actually uses: the setting, only while Easy Reading is on.
        public static float TextScale => Enabled ? TextSizes[TextSizeStep] : 1f;
        public static bool TapToRead => Enabled && TapToReadSetting;
        public static bool InstantText => Enabled && InstantTextSetting;
        public static bool BigGrab => Enabled && BigGrabSetting;

        static void SetInt(string key, int value)
        {
            if (PlayerPrefs.GetInt(Prefix + key, int.MinValue) == value) return;
            PlayerPrefs.SetInt(Prefix + key, value);
            PlayerPrefs.Save();
            Version++;
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => Changed = null;
    }
}
