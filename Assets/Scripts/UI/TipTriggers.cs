using CapyIsland.Game;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// Watches for the moments a part of the screen first matters, and asks <see cref="HudTour"/>
    /// for that one tip. The game teaches by doing, so there are few, and none during the opening:
    ///
    ///  * the opening is done → the goal card (your next goal is always there)
    ///  * the map is first opened → how to travel on it (from MapScreen); until then, a finger now
    ///    and then points at the Map button (<see cref="MapNudge"/>)
    ///  * the first hobby is unlocked → Events, and the clock (creatures keep hours)
    ///  * the first visitor lands → Friends and the emoji button
    ///  * a lot of reaching for the screen edge to walk → Old Yuzu mentions tapping, and the joystick
    ///
    /// Each tip shows once, when <see cref="Attention"/> says the screen is free and you are not in
    /// the middle of dragging something.
    /// </summary>
    public class TipTriggers : MonoBehaviour
    {
        const string WalkTipKey = "CapyIsland.Tip.walk";

        float _next;

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            if (Visit.Active || Attention.TutorialRunning) return;

            HudTour.Tip("goals");
            if (HobbyLog.IsUnlocked(Hobby.Fishing) || HobbyLog.IsUnlocked(Hobby.Birding) || HobbyLog.IsUnlocked(Hobby.Bugs))
                HudTour.Tip("events", "clock");
            if (VisitorBot.Current != null) HudTour.Tip("friends", "hello");
            WalkTip();
            MapNudge.Tick();

            // Never in the middle of a drag or a pinch.
            var input = FindFirstObjectByType<CameraRig.CameraInputController>();
            if (input != null && input.IsManipulating) return;
            HudTour.Pump();
        }

        /// <summary>
        /// Someone steering with their finger way out by the screen edge, again and again: once, a
        /// word from Old Yuzu that tapping a spot walks there too, and that there's a joystick.
        /// </summary>
        static void WalkTip()
        {
            if (Controls.Joystick || Guidance.Off || PlayerPrefs.GetInt(WalkTipKey, 0) == 1) return;
            if (CameraRig.CameraInputController.FarSteerSeconds < 8f) return;
            PlayerPrefs.SetInt(WalkTipKey, 1);
            PlayerPrefs.Save();
            Attention.Ask("walk-tip", () => Hints.Show(
                "Tip: you don't have to reach. Tap any spot to walk there, or switch on the joystick in Settings."));
        }
    }
}
