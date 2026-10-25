using System;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// One of the island's people (Miso, Captain Wasabi, Salty Kombu, Chef Tofu, Skipper Nori,
    /// Old Mochi…), named on the map so you can always find them. Only on the map: they don't get
    /// arrows at the screen's edge (EdgeIndicators skips MarkerKind.Npc). While one has news for
    /// you, their quest pin shows instead, named too.
    /// </summary>
    public class NpcTag : MonoBehaviour, IMapMarker
    {
        string _name;
        Func<bool> _here;
        Func<Sprite> _face;

        /// <summary>
        /// Names <paramref name="go"/> on the map as <paramref name="name"/>, while
        /// <paramref name="here"/> says they're about (standing in the world, no news to show).
        /// </summary>
        public static NpcTag Add(GameObject go, string name, Func<bool> here = null, Func<Sprite> face = null)
        {
            if (go == null) return null;
            var tag = go.GetComponent<NpcTag>();
            if (tag == null) tag = go.AddComponent<NpcTag>();
            tag._name = name;
            tag._here = here;
            tag._face = face;
            return tag;
        }

        void OnEnable() => MapMarkers.Register(this);
        void OnDisable() => MapMarkers.Unregister(this);

        public MarkerKind MarkerKind => MarkerKind.Npc;
        public bool MarkerActive => isActiveAndEnabled && (_here == null || _here());
        public Vector2 MarkerPosition => transform.position;
        public Sprite MarkerIcon => _face != null ? _face() : MapMarkers.SpriteOf(gameObject);
        public string MarkerLabel => _name;   // already a name the player reads; translated as it's drawn
    }
}
