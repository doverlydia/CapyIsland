using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>Anything in the world the player can tap: shop displays, fruit trees, quest givers, ghosts.</summary>
    public interface ITappable
    {
        /// <summary>Distance from the tap to this thing if the tap hits it, or a negative number if it misses.</summary>
        float TapDistance(Vector2 worldPoint);

        void Tap();
    }

    /// <summary>
    /// The registry the input controller asks what was tapped. A tap target claims the press the
    /// moment it lands — before dragging or steering can — and fires on release if the finger did
    /// not wander, so pressing a shop display never picks up the shop behind it.
    /// </summary>
    public static class TapTargets
    {
        static readonly List<ITappable> _all = new();

        public static void Register(ITappable t)
        {
            if (t != null && !_all.Contains(t)) _all.Add(t);
        }

        public static void Unregister(ITappable t) => _all.Remove(t);

        /// <summary>The nearest target under a world point, or null.</summary>
        public static ITappable Pick(Vector2 worldPoint)
        {
            ITappable best = null;
            float bestDistance = float.MaxValue;
            for (int i = _all.Count - 1; i >= 0; i--)
            {
                var t = _all[i];
                if (t == null || (t is Object o && o == null)) { _all.RemoveAt(i); continue; }
                float d = t.TapDistance(worldPoint);
                if (d < 0f || d >= bestDistance) continue;
                best = t;
                bestDistance = d;
            }
            return best;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => _all.Clear();
    }
}
