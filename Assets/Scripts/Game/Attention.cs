using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The gatekeeper for the player's attention. Anything that would pop up by itself — a card, a
    /// prompt, a tip, a new arrival on the island — asks here instead of showing at once, and gets
    /// its turn when the screen is free: no movie, no open window or card, no tour, the tutorial
    /// over, and a few calm seconds since the last thing went away. One at a time, oldest first.
    ///
    /// Things the player just did (a drop that didn't fit, a tap on a shop) answer straight away and
    /// never come through here; this is only for what the game starts on its own.
    /// </summary>
    public static class Attention
    {
        /// <summary>True from a new game until Old Yuzu's opening is done: only she may speak.</summary>
        public static bool TutorialRunning
        {
            get
            {
                var tutorial = TutorialDirector.Instance;
                return tutorial != null && !tutorial.Finished;
            }
        }

        /// <summary>Something has the screen right now: a movie, a window, a card, a tour, fishing.</summary>
        public static bool Busy =>
            UI.Cutscene.Playing || UI.AwakeningScreen.IsOpen || UI.IntroAudio.Active
            || UI.HudTour.IsOpen || UI.UiScreen.AnyOpen || UI.SpeechDialog.IsOpen || UI.CardPopup.IsShowing
            || UI.PurchaseDialog.IsOpen || UI.TextReader.IsOpen
            || HotSpring.Bathing || (Fishing.Instance != null && Fishing.Instance.Mode)
            || Visit.Travelling;

        static float _calmSince;

        /// <summary>Real seconds since the screen was last busy (or something last took a turn).</summary>
        public static float CalmFor => Time.unscaledTime - _calmSince;

        /// <summary>The screen is the player's and has been for <paramref name="calm"/> seconds.</summary>
        public static bool Free(float calm = 4f) => !TutorialRunning && !Busy && CalmFor >= calm;

        class Request
        {
            public string Key;
            public Action Show;
            public Func<bool> StillWanted;
            public float Calm;
        }

        static readonly List<Request> _waiting = new();

        /// <summary>
        /// Shows something when its turn comes. <paramref name="key"/> keeps one copy of the same
        /// thing waiting; <paramref name="stillWanted"/> drops it if, by then, it no longer applies.
        /// </summary>
        public static void Ask(string key, Action show, Func<bool> stillWanted = null, float calm = 4f)
        {
            if (show == null) return;
            if (!string.IsNullOrEmpty(key))
                foreach (var r in _waiting)
                    if (r.Key == key) return;
            _waiting.Add(new Request { Key = key, Show = show, StillWanted = stillWanted, Calm = calm });
        }

        /// <summary>True while something with this key is waiting its turn.</summary>
        public static bool Waiting(string key)
        {
            foreach (var r in _waiting) if (r.Key == key) return true;
            return false;
        }

        /// <summary>Called every frame by the HUD.</summary>
        public static void Tick()
        {
            if (Busy) { _calmSince = Time.unscaledTime; return; }
            if (_waiting.Count == 0 || TutorialRunning) return;

            for (int i = 0; i < _waiting.Count; i++)
            {
                var r = _waiting[i];
                if (r.StillWanted != null && !r.StillWanted()) { _waiting.RemoveAt(i--); continue; }
                if (CalmFor < r.Calm) return;   // oldest first: the rest wait behind it
                _waiting.RemoveAt(i);
                _calmSince = Time.unscaledTime;   // the next one waits its own calm after this
                r.Show();
                return;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            _waiting.Clear();
            _calmSince = 0f;
        }
    }
}
