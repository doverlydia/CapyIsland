using System;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>Parts of the day a creature can be found in.</summary>
    [Flags]
    public enum DayParts
    {
        None = 0,
        Dawn = 1,
        Day = 2,
        Dusk = 4,
        Night = 8,
        Daylight = Dawn | Day | Dusk,
        Twilight = Dawn | Dusk,
        Any = Dawn | Day | Dusk | Night,
    }

    /// <summary>
    /// The island's clock: a whole day passes every 24 real minutes, so one sitting sees morning,
    /// noon and night. It runs on the real clock, not on play time — the day carries on while
    /// you are away, the way the sea does — and a brand-new island starts at seven in the morning.
    ///
    /// Dawn 5–7, day 7–17, dusk 17–19, night 19–5. Some fish, birds and bugs only come out in
    /// some of them (<see cref="Allows"/>).
    /// </summary>
    public static class GameClock
    {
        public const float DaySeconds = 24f * 60f;
        const string OffsetKey = "CapyIsland.ClockOffset";

        static double? _offset;

        /// <summary>Set to pin the time of day (0..1) for testing; null runs the clock.</summary>
        public static float? Override;

        static double Offset
        {
            get
            {
                if (_offset.HasValue) return _offset.Value;
                var saved = PlayerPrefs.GetString(OffsetKey, "");
                if (double.TryParse(saved, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var o))
                    _offset = o;
                else
                {
                    // First run: whatever the real time, the island wakes up at 07:00.
                    _offset = 7.0 / 24.0 - Frac(Seconds / DaySeconds);
                    PlayerPrefs.SetString(OffsetKey, _offset.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                    PlayerPrefs.Save();
                }
                return _offset.Value;
            }
        }

        static double Seconds => (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
        static double Frac(double x) => x - Math.Floor(x);

        /// <summary>
        /// On a real player's island: their island's clock (Live hears it from their game), so if
        /// it's night there it's night for you too. Null at home. Every flight clears it.
        /// </summary>
        public static double? Visiting;

        /// <summary>This island's clock setting, for a visitor's game to follow.</summary>
        public static double IslandOffset => Offset;

        static double Current => Visiting ?? Offset;

        /// <summary>0 at midnight, 0.5 at noon.</summary>
        public static float TimeOfDay => Override ?? (float)Frac(Seconds / DaySeconds + Current);

        /// <summary>A fresh island (Start over): its clock begins again at 07:00, like a first run.</summary>
        public static void StartAfresh()
        {
            PlayerPrefs.DeleteKey(OffsetKey);
            _offset = null;
        }

        public static float Hour => TimeOfDay * 24f;

        /// <summary>Which island day it is (counts up by one at every midnight).</summary>
        public static long Day => (long)Math.Floor(Seconds / DaySeconds + Current);

        public static DayParts Now => PartAt(Hour);

        public static DayParts PartAt(float hour) =>
            hour >= 5f && hour < 7f ? DayParts.Dawn
            : hour >= 7f && hour < 17f ? DayParts.Day
            : hour >= 17f && hour < 19f ? DayParts.Dusk
            : DayParts.Night;

        public static bool IsNight => Now == DayParts.Night;

        /// <summary>True if something found in <paramref name="parts"/> is about right now.</summary>
        public static bool Allows(DayParts parts) => (parts & Now) != 0;

        /// <summary>"07:12" — the island's time, on a 24-hour clock.</summary>
        public static string Clock
        {
            get
            {
                int minutes = Mathf.FloorToInt(TimeOfDay * 24f * 60f);
                return (minutes / 60).ToString("00") + ":" + (minutes % 60).ToString("00");
            }
        }

        /// <summary>When something can be found, in words: "Night", "Dawn · Dusk", "Any time".</summary>
        public static string Describe(DayParts parts)
        {
            if (parts == DayParts.Any || parts == DayParts.None) return "Any time";
            if (parts == DayParts.Daylight) return "Daytime";
            var words = new System.Collections.Generic.List<string>();
            if ((parts & DayParts.Dawn) != 0) words.Add("Dawn");
            if ((parts & DayParts.Day) != 0) words.Add("Day");
            if ((parts & DayParts.Dusk) != 0) words.Add("Dusk");
            if ((parts & DayParts.Night) != 0) words.Add("Night");
            return string.Join(" · ", words);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            _offset = null;
            Override = null;
            Visiting = null;
        }
    }
}
