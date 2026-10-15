using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Old Yuzu notices the world changing and says so: the sun coming up or going down, night
    /// falling, rain starting and stopping. When the hour brings out creatures that were not
    /// about before, she names a couple — so the hours fish and bugs keep are something you hear
    /// about, not something you have to find in a menu.
    ///
    /// Quiet during the opening tutorial, which has enough to say, and while visiting.
    /// </summary>
    public class WorldMoments : MonoBehaviour
    {
        DayParts _part;
        bool _raining;
        bool _started;
        float _nextCheck;

        void Update()
        {
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 1f;

            var part = GameClock.Now;
            bool raining = Weather.IsRaining;
            if (!_started) { _started = true; _part = part; _raining = raining; return; }

            var tutorial = TutorialDirector.Instance;
            bool quiet = (tutorial != null && !tutorial.Finished) || Visit.Active;

            if (part != _part)
            {
                var previous = _part;
                _part = part;
                if (!quiet) Hints.Island(PartLine(part, previous));
            }
            if (raining != _raining)
            {
                _raining = raining;
                if (!quiet) Hints.Island(raining
                    ? "It's raining! Some shy creatures only come out in the wet."
                    : part == DayParts.Night ? "The rain has stopped." : "The rain has stopped. Look, a rainbow!");
            }
        }

        static string PartLine(DayParts part, DayParts previous)
        {
            string opening = part switch
            {
                DayParts.Dawn => "The sun is coming up.",
                DayParts.Day => "Good morning! It's a bright new day.",
                DayParts.Dusk => "The sun is setting.",
                _ => "Night has fallen.",
            };
            var names = NewlyAbout(part, previous);
            if (names.Count == 0) return opening;
            // Two lines: each one is translated on its own.
            return opening + "\n" + (names.Count == 1 ? $"Look out for the {names[0]}!" : $"Look out for the {names[0]} and the {names[1]}!");
        }

        /// <summary>Up to two creatures you could find now that you could not a moment ago.</summary>
        static List<string> NewlyAbout(DayParts now, DayParts before)
        {
            var found = new List<string>();
            bool Fresh(DayParts t, bool rain) => !rain && (t & now) != 0 && (t & before) == 0;

            if (HobbyLog.IsUnlocked(Hobby.Fishing))
            {
                int level = HobbyLog.Level(Hobby.Fishing);
                foreach (var f in HobbyData.Fish) if (f.MinLevel <= level && Fresh(f.Times, f.RainOnly)) found.Add(f.Name);
            }
            if (HobbyLog.IsUnlocked(Hobby.Bugs))
            {
                int level = HobbyLog.Level(Hobby.Bugs);
                foreach (var b in HobbyData.Bugs) if (b.MinLevel <= level && Fresh(b.Times, b.RainOnly)) found.Add(b.Name);
            }
            if (HobbyLog.IsUnlocked(Hobby.Birding))
            {
                int level = HobbyLog.Level(Hobby.Birding);
                foreach (var b in HobbyData.Birds) if (b.MinLevel <= level && Fresh(b.Times, b.RainOnly)) found.Add(b.Name);
            }
            // A couple, not a list: pick at random so it varies from day to day.
            while (found.Count > 2) found.RemoveAt(Random.Range(0, found.Count));
            return found;
        }
    }
}
