using System;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Now and then it rains. Time is cut into six-minute stretches, and about one in six is
    /// rainy — picked from the real clock, so it is the same however often you reopen the game.
    /// Rain eases in and out over half a minute, and when it stops in daylight a rainbow hangs in
    /// the sky for a while.
    ///
    /// A few shy creatures only come out in the rain (<see cref="FishKind.RainOnly"/> and friends).
    /// </summary>
    public static class Weather
    {
        const double SlotSeconds = 360.0;
        const double FadeSeconds = 25.0;
        const double RainbowSeconds = 90.0;

        /// <summary>Set to force the rain (0..1) for testing; null follows the clock.</summary>
        public static float? Override;

        static double Seconds => (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;

        static bool Rainy(long slot)
        {
            unchecked
            {
                uint h = (uint)slot * 2654435761u;
                h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
                return h % 100 < 17;
            }
        }

        /// <summary>How hard it is raining, 0 dry … 1 steady rain.</summary>
        public static float Rain
        {
            get
            {
                if (Override.HasValue) return Override.Value;
                double s = Seconds;
                long slot = (long)Math.Floor(s / SlotSeconds);
                if (!Rainy(slot)) return 0f;
                double into = s - slot * SlotSeconds, left = SlotSeconds - into;
                double up = Rainy(slot - 1) ? 1.0 : Math.Min(1.0, into / FadeSeconds);
                double down = Rainy(slot + 1) ? 1.0 : Math.Min(1.0, left / FadeSeconds);
                return (float)Math.Min(up, down);
            }
        }

        public static bool IsRaining => Rain > 0.5f;

        /// <summary>0..1: the rainbow after a shower, fading in and then slowly away.</summary>
        public static float Rainbow
        {
            get
            {
                if (Override.HasValue) return 0f;
                double s = Seconds;
                long slot = (long)Math.Floor(s / SlotSeconds);
                if (Rainy(slot) || !Rainy(slot - 1)) return 0f;
                double since = s - slot * SlotSeconds;
                if (since > RainbowSeconds) return 0f;
                double fadeIn = Math.Min(1.0, since / 8.0), fadeOut = Math.Min(1.0, (RainbowSeconds - since) / 30.0);
                return (float)Math.Min(fadeIn, fadeOut);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => Override = null;
    }
}
