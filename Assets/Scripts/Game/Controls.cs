using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// How the player likes to walk. Tapping a spot always walks there; on top of that, holding on
    /// the ground either steers toward the finger (the default) or, with the joystick on, works a
    /// small stick that appears wherever the thumb lands, so the thumb never travels far.
    /// </summary>
    public static class Controls
    {
        const string JoystickKey = "CapyIsland.Joystick";

        public static bool Joystick
        {
            get => PlayerPrefs.GetInt(JoystickKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(JoystickKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
