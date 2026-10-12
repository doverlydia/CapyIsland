using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// How loud each kind of sound is, set from Settings: music, effects (taps, pops, chopping)
    /// and ambience (waves, wind, birds). Each is 0..1 and multiplies that layer; the Sound switch
    /// still mutes everything at once.
    /// </summary>
    public static class AudioMix
    {
        const string Prefix = "CapyIsland.Volume.";

        public static float Music
        {
            get => PlayerPrefs.GetFloat(Prefix + "Music", 0.6f);
            set => Set("Music", value);
        }

        public static float Effects
        {
            get => PlayerPrefs.GetFloat(Prefix + "Effects", 0.8f);
            set => Set("Effects", value);
        }

        public static float Ambience
        {
            get => PlayerPrefs.GetFloat(Prefix + "Ambience", 0.7f);
            set => Set("Ambience", value);
        }

        static void Set(string key, float value)
        {
            PlayerPrefs.SetFloat(Prefix + key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }
}
