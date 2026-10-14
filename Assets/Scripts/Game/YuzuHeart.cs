using System;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Old Yuzu's secret. On the last night of the Big Chill she lay down in the snow beside her
    /// spring and froze; when the snow finally melted, the ice around her didn't, because the cold
    /// had got into her heart. She believes she is gone for good (her spirit is the ghost who
    /// guides you). But every warm heart that comes home melts a little of that ice.
    ///
    /// Nobody says so. The player notices first: a drip from the ice at a quarter of the hearts,
    /// a twitch at half (she spins round: "did you see that?"), a glow at three quarters (she
    /// starts to wonder), and when every heart is home, she comes back to life. Then the wedding:
    /// she marries you and her granddaughter herself at the chapel (it is not a heart of its own),
    /// and after it the finale.
    ///
    /// The step reached is remembered with the island (Start over and Delete Save forget it).
    /// </summary>
    public static class YuzuHeart
    {
        const string StageKey = "CapyIsland.YuzuStage";

        /// <summary>Her granddaughter's wedding, remembered under this name (it is not a heart).</summary>
        public const string WeddingHeart = "The wedding bells toll for thee";

        /// <summary>You and Miso are married.</summary>
        public static bool Married => Motes.Has(WeddingHeart);

        /// <summary>She is back and Miso has said yes, and the wedding is still to be held.</summary>
        public static bool WeddingDue => Alive && !Married && Motes.Has("Madly in Love");

        public enum Stage { Frozen, Drip, Twitch, Glow, Alive }

        /// <summary>The share of hearts home at which each step happens.</summary>
        static readonly float[] Threshold = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        /// <summary>The step already shown (its little movie has played).</summary>
        public static Stage Shown => (Stage)Mathf.Clamp(PlayerPrefs.GetInt(StageKey, 0), 0, (int)Stage.Alive);

        public static bool Alive => Shown >= Stage.Alive;

        const string FinaleKey = "CapyIsland.Finale";

        /// <summary>Every heart home and the wedding held: the finale has played.</summary>
        public static bool FinaleSeen => PlayerPrefs.GetInt(FinaleKey, 0) == 1;

        /// <summary>Everything is home and you are married: time for the finale, if it hasn't played.</summary>
        public static bool FinaleDue => Alive && Married && !FinaleSeen && Motes.Total > 0 && Motes.CollectedCount >= Motes.Total;

        public static void MarkFinale()
        {
            PlayerPrefs.SetInt(FinaleKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>A step was reached (its movie just finished).</summary>
        public static event Action<Stage> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => Changed = null;

        /// <summary>
        /// How much of the ice has melted: every heart earned — the same count as the achievements
        /// list, whether or not its heart has been tapped home yet.
        /// </summary>
        public static float Thaw => Motes.Total > 0 ? (float)Motes.CollectedCount / Motes.Total : 0f;

        /// <summary>The step the hearts brought home have earned.</summary>
        public static Stage Earned
        {
            get
            {
                float t = Thaw;
                var s = Stage.Frozen;
                for (int i = 1; i < Threshold.Length; i++)
                    if (t >= Threshold[i] - 0.0001f) s = (Stage)i;
                return s;
            }
        }

        public static void Reach(Stage s)
        {
            if (s <= Shown) return;
            PlayerPrefs.SetInt(StageKey, (int)s);
            PlayerPrefs.Save();
            Changed?.Invoke(s);
        }

        /// <summary>A fresh island: frozen again (Start over, Delete Save).</summary>
        public static void Forget()
        {
            PlayerPrefs.DeleteKey(StageKey);
            PlayerPrefs.DeleteKey(FinaleKey);
        }
    }

    /// <summary>
    /// Watches for a step earned but not yet shown, and plays its movie at a quiet moment: no
    /// popup, card or movie on screen, not mid-soak, not on a friend's island.
    /// </summary>
    public class YuzuHeartDirector : MonoBehaviour
    {
        const float PatienceSeconds = 15f;

        float _next;
        float _calmFor;   // seconds a movie has been waiting on nothing but Old Yuzu's chatter
        bool _playing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<YuzuHeartDirector>() == null)
                new GameObject("[YuzuHeart]").AddComponent<YuzuHeartDirector>();
        }

        void Update()
        {
            if (_playing || Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 1f;
            // Not waiting on the tower: a bomb can bubble it, and she must still wake while it rides in your trail.
            if (Visit.Active || Visit.Travelling || HotSpring.Bathing || LandMap.Instance == null) return;
            if (!YuzuHeart.FinaleDue && YuzuHeart.Earned <= YuzuHeart.Shown) { _calmFor = 0f; return; }
            if (!UI.HudTour.Calm) { _calmFor = 0f; return; }
            // Let her finish what she is saying, but not forever: her hints can follow one another
            // for minutes on a busy island, and these movies matter more than a tip.
            _calmFor += 1f;
            if (UI.HudTour.YuzuTalking && _calmFor < PatienceSeconds) return;
            _calmFor = 0f;
            // Every heart home: the finale, then her lantern.
            if (YuzuHeart.FinaleDue)
            {
                _playing = true;
                UI.HomeAgainCutscene.Show(() =>
                {
                    _playing = false;
                    YuzuHeart.MarkFinale();
                    UI.CardPopup.Show(StoryText.Yuzu.FinaleCardTitle, StoryText.Yuzu.FinaleCardBody, Wardrobe.Art("Hat_Lantern"),
                        () => Wardrobe.Give(Wardrobe.Find("Hat_Lantern")));
                });
                return;
            }
            // Several steps earned at once (an older save, a burst of hearts): just the latest movie,
            // so waking her is never four movies away.
            var next = YuzuHeart.Earned;
            if (next <= YuzuHeart.Shown) return;
            _playing = true;
            UI.ThawCutscene.Show(next, () =>
            {
                _playing = false;
                YuzuHeart.Reach(next);
            });
        }
    }
}
