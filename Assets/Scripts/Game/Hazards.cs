using System;
using UnityEngine;

namespace CapyIsland.Game
{
    public enum HazardKind { Monster, Deer, Bomb }

    /// <summary>
    /// Trouble let loose on the island — what comes out of a felled fruit tree
    /// (StoreItemsList.MakeItem("Monster" / "Deer" / "Bomb")). The creatures themselves register a
    /// spawner here; until one is registered for a kind, the event still fires so the rest of the
    /// game can react.
    /// </summary>
    public static class Hazards
    {
        /// <summary>Raised for every hazard released, with where it appeared.</summary>
        public static event Action<HazardKind, Vector2> Released;

        /// <summary>Raised when a pest (a guinea pig or a wild zombie capybara) is bombed away or cured.</summary>
        public static event Action<Vector2> PestCleared;

        public static void NotePestCleared(Vector2 at) => PestCleared?.Invoke(at);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            Released = null;
            PestCleared = null;
        }

        static readonly Func<Vector2, GameObject>[] Spawners = new Func<Vector2, GameObject>[3];

        public static void Register(HazardKind kind, Func<Vector2, GameObject> spawner) => Spawners[(int)kind] = spawner;

        public static GameObject Spawn(HazardKind kind, Vector2 at)
        {
            Released?.Invoke(kind, at);
            var spawner = Spawners[(int)kind];
            if (spawner != null) return spawner(at);
            Debug.Log($"[Hazards] {kind} released at {at} (no spawner registered yet).");
            return null;
        }
    }
}
