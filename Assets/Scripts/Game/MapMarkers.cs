using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    public enum MarkerKind { Bubble, Pest, Quest, Tower, Guide, Friend }

    /// <summary>Something worth finding: shown on the map, and at the screen edge while off screen.</summary>
    public interface IMapMarker
    {
        MarkerKind MarkerKind { get; }

        /// <summary>False while there is nothing to point at (taken, tamed, no news).</summary>
        bool MarkerActive { get; }

        Vector2 MarkerPosition { get; }

        /// <summary>A picture of what is there, or null for the kind's default.</summary>
        Sprite MarkerIcon { get; }

        /// <summary>What is there, in words ("Daisy Garden", "Miso"), or null for the kind's word.</summary>
        string MarkerLabel => null;
    }

    /// <summary>
    /// Every live marker, so the edge arrows and the map never have to search the scene. Things
    /// register while enabled — bubble contents and catalog templates sit inactive and never do.
    /// </summary>
    public static class MapMarkers
    {
        static readonly List<IMapMarker> _all = new();

        public static IReadOnlyList<IMapMarker> All => _all;

        public static void Register(IMapMarker marker)
        {
            if (marker != null && !_all.Contains(marker)) _all.Add(marker);
        }

        public static void Unregister(IMapMarker marker) => _all.Remove(marker);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => _all.Clear();

        /// <summary>The first sprite on an item's "Sprite" child, for marker pictures.</summary>
        public static Sprite SpriteOf(GameObject item)
        {
            if (item == null) return null;
            var body = item.transform.Find("Sprite");
            var sr = body != null ? body.GetComponent<SpriteRenderer>() : item.GetComponentInChildren<SpriteRenderer>(true);
            return sr != null ? sr.sprite : null;
        }
    }
}
