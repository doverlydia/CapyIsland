using System;
using System.Collections.Generic;

namespace CapyIsland.Game
{
    /// <summary>
    /// The original's hint line (Tasks.HintTextEvent / HintTextMouse): short messages about what just
    /// happened on the island — a worker out of trees, a shaken fruit tree, a monster on the loose.
    /// Anything can post one; the ghost dialogue shows them.
    /// </summary>
    public static class Hints
    {
        public static event Action<string> Shown;

        public static void Show(string line)
        {
            if (!string.IsNullOrEmpty(line)) Shown?.Invoke(line);
        }

        static readonly Dictionary<string, Func<bool>> _while = new();

        /// <summary>
        /// A hint that only makes sense while <paramref name="stillTrue"/> holds ("go and talk to
        /// Miso", until you have). The ghost may have to wait for a popup or the tutorial before she
        /// says it; if by then it no longer holds, she drops it instead of saying it late.
        /// </summary>
        public static void Show(string line, Func<bool> stillTrue)
        {
            if (string.IsNullOrEmpty(line)) return;
            if (stillTrue != null) _while[line] = stillTrue;
            Show(line);
        }

        /// <summary>False once a hint posted with a condition no longer applies.</summary>
        public static bool StillTrue(string line) =>
            string.IsNullOrEmpty(line) || !_while.TryGetValue(line, out var f) || f == null || f();

        static readonly HashSet<string> _island = new();

        /// <summary>
        /// Old Yuzu talking about the island itself (a hungry worker, a forest chopped bare,
        /// nightfall). On someone else's island it isn't said at all: it's their island, and she
        /// only keeps an eye on yours. Everything else (what you did, who came and went) is still
        /// shown there, at the top of the screen instead of in her corner.
        /// </summary>
        public static void Island(string line, Func<bool> stillTrue = null)
        {
            if (string.IsNullOrEmpty(line)) return;
            _island.Add(line);
            Show(line, stillTrue);
        }

        public static bool IsAboutIsland(string line) => line != null && _island.Contains(line);
    }
}
