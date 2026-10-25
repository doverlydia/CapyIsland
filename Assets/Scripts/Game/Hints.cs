using System;
using System.Collections.Generic;

namespace CapyIsland.Game
{
    /// <summary>
    /// Short messages to the player, of two kinds:
    ///
    ///  * <see cref="Show"/> — news: something happened that you didn't just do (a friend landed, a
    ///    cat brought you something, a new button joined the column). Old Yuzu says it from her
    ///    corner, fading in for the line and out again.
    ///  * <see cref="Answer"/> — the answer to something you just did ("too far", "Copied!", "no
    ///    room there"): a small note by your finger that fades by itself (UI.TapAnswer).
    ///
    /// Remarks about the island itself (a hungry worker, a forest chopped bare, nightfall) aren't
    /// made at all: the island shows those — a sleeper's thought cloud, the arrows at the edge.
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
        /// News that only makes sense while <paramref name="stillTrue"/> holds ("Map is new", until
        /// you've tapped it). The ghost may have to wait for a popup before she says it; if by then
        /// it no longer holds, she drops it instead of saying it late.
        /// </summary>
        public static void Show(string line, Func<bool> stillTrue)
        {
            if (string.IsNullOrEmpty(line)) return;
            if (stillTrue != null) _while[line] = stillTrue;
            Show(line);
        }

        /// <summary>
        /// A condition for a line said some other way (the tutorial's story lines): once it no longer
        /// holds, the ghost drops the line, waiting or showing, as she does a hint's.
        /// </summary>
        public static void While(string line, Func<bool> stillTrue)
        {
            if (!string.IsNullOrEmpty(line) && stillTrue != null) _while[line] = stillTrue;
        }

        /// <summary>False once a hint posted with a condition no longer applies.</summary>
        public static bool StillTrue(string line) =>
            string.IsNullOrEmpty(line) || !_while.TryGetValue(line, out var f) || f == null || f();

        /// <summary>Raised with every answer, for the dev log.</summary>
        public static event Action<string> Answered;

        /// <summary>The answer to something the player just did: a small note by their finger, fading by itself.</summary>
        public static void Answer(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            Answered?.Invoke(line);
            UI.TapAnswer.Show(line);
        }
    }
}
