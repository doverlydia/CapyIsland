using CapyIsland.Game;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// Watches for the moments each part of the screen first matters, and asks <see cref="HudTour"/>
    /// for that one tip — so the help arrives a page at a time, when it is useful, instead of as one
    /// long tour at the start:
    ///
    ///  * something joins your trail → placing (with the green/red circle)
    ///  * an arrow appears at the screen edge → the arrows and what their colours mean
    ///  * wood or gold first comes in → the resource bar
    ///  * Old Yuzu's bubble first tucks away → tap her picture to hear her again
    ///  * the opening tutorial is done → the goal card, then the Menu
    ///  * the first bridge opens → the Map
    ///  * the first hobby is unlocked → Events, and the clock (creatures keep hours)
    ///  * the first visitor lands → Friends and the emoji button
    ///  * the daily gift screen is closed for the first time → the Gifts button
    ///
    /// Each tip shows once; they queue and wait until nothing else is on screen and you are not in
    /// the middle of dragging something.
    /// </summary>
    public class TipTriggers : MonoBehaviour
    {
        float _next, _since;
        int _wood = -1, _gold = -1;

        void OnEnable()
        {
            GhostDialogue.Tucked += OnTucked;
            UiScreen.Closed += OnScreenClosed;
        }

        void OnDisable()
        {
            GhostDialogue.Tucked -= OnTucked;
            UiScreen.Closed -= OnScreenClosed;
        }

        void OnTucked() => HudTour.Tip("yuzu");

        void OnScreenClosed(UiScreen screen)
        {
            if (screen is DailyGiftScreen) HudTour.Tip("gifts");
        }

        void Update()
        {
            _since += Time.unscaledDeltaTime;
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            if (Visit.Active || !HudTour.Seen("welcome")) return;   // the first two pages come first

            if (InTrail()) HudTour.Tip("placing");
            if (EdgeIndicators.FirstShown != null) HudTour.Tip("arrows");
            if (Earned()) HudTour.Tip("treasure");

            var tutorial = TutorialDirector.Instance;
            if (tutorial == null || tutorial.Finished) HudTour.Tip("goals", "menu");
            var land = LandMap.Instance;
            if (land != null && land.IsBridgeOpen("first_bridge")) HudTour.Tip("map");
            if (HobbyLog.IsUnlocked(Hobby.Fishing) || HobbyLog.IsUnlocked(Hobby.Birding) || HobbyLog.IsUnlocked(Hobby.Bugs))
                HudTour.Tip("events", "clock");
            if (VisitorBot.Current != null) HudTour.Tip("friends", "hello");

            // Never in the middle of a drag or a pinch.
            var input = FindFirstObjectByType<CameraRig.CameraInputController>();
            if (input != null && input.IsManipulating) return;
            HudTour.Pump();
        }

        /// <summary>Something you can place is following you.</summary>
        static bool InTrail()
        {
            foreach (var p in FindObjectsByType<Placeable>(FindObjectsSortMode.None))
                if (!p.Placed && p.gameObject.activeInHierarchy && p.GetComponent<Follower>() is { Carried: true }) return true;
            return false;
        }

        /// <summary>Wood or gold went up since the island loaded (a delivery, a sale).</summary>
        bool Earned()
        {
            var store = ResourceStore.Instance;
            if (store == null) return false;
            int wood = store.Get(ResourceKind.Wood), gold = store.Get(ResourceKind.Gold);
            // Give the save a few seconds to load before taking the starting amounts.
            if (_wood < 0 || _since < 4f) { _wood = wood; _gold = gold; return false; }
            bool up = wood > _wood || gold > _gold;
            _wood = wood; _gold = gold;
            return up;
        }
    }
}
