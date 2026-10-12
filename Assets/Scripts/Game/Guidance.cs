using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Whether the game teaches: the screen tips, Old Yuzu's step-by-step tutorial lines and the
    /// pointing hand. "End tutorial" switches all of it off at once; Settings switches it back.
    ///
    /// Only the teaching stops. The opening's story still happens behind the scenes (the garden
    /// bubble, the first bridge, the tower), and hints that explain what just happened — a blocked
    /// drop, a full mill — still come through.
    /// </summary>
    public static class Guidance
    {
        const string Key = "CapyIsland.TutorialOff";

        public static bool Off
        {
            get => PlayerPrefs.GetInt(Key, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(Key, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
