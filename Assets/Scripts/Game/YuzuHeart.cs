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
    /// starts to wonder), and when every heart but the wedding's is home, she comes back to life.
    /// Only then can the wedding be held: she marries you and her granddaughter herself, and that
    /// last heart completes the island.
    ///
    /// The step reached is remembered with the island (Start over and Delete Save forget it).
    /// </summary>
    public static class YuzuHeart
    {
        const string StageKey = "CapyIsland.YuzuStage";

        /// <summary>The one heart that can only come after she is back: her granddaughter's wedding.</summary>
        public const string WeddingHeart = "The wedding bells toll for thee";

        public enum Stage { Frozen, Drip, Twitch, Glow, Alive }

        /// <summary>The share of hearts home at which each step happens.</summary>
        static readonly float[] Threshold = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        /// <summary>The step already shown (its little movie has played).</summary>
        public static Stage Shown => (Stage)Mathf.Clamp(PlayerPrefs.GetInt(StageKey, 0), 0, (int)Stage.Alive);

        public static bool Alive => Shown >= Stage.Alive;

        const string FinaleKey = "CapyIsland.Finale";

        /// <summary>Every heart home (the wedding's last of all): the finale has played.</summary>
        public static bool FinaleSeen => PlayerPrefs.GetInt(FinaleKey, 0) == 1;

        /// <summary>Everything is home: time for the finale, if it hasn't played.</summary>
        public static bool FinaleDue => Alive && !FinaleSeen && Motes.Total > 0 && Motes.ClickedCount >= Motes.Total;

        public static void MarkFinale()
        {
            PlayerPrefs.SetInt(FinaleKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>A step was reached (its movie just finished).</summary>
        public static event Action<Stage> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => Changed = null;

        /// <summary>How much of the ice has melted: every heart brought home except the wedding's.</summary>
        public static float Thaw
        {
            get
            {
                int total = 0, home = 0;
                foreach (var m in Motes.All)
                {
                    if (m.Name == WeddingHeart) continue;
                    total++;
                    if (Motes.IsClicked(m.Name)) home++;
                }
                return total > 0 ? (float)home / total : 0f;
            }
        }

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
        float _next;
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
            if (Visit.Active || Visit.Travelling || HotSpring.Bathing || TowerOfLove.Instance == null) return;
            if (!UI.HudTour.Quiet) return;
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
            var shown = YuzuHeart.Shown;
            if (YuzuHeart.Earned <= shown) return;
            var next = shown + 1;
            _playing = true;
            UI.ThawCutscene.Show(next, () =>
            {
                _playing = false;
                YuzuHeart.Reach(next);
            });
        }
    }
}
