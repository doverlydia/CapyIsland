using System;

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
    }
}
