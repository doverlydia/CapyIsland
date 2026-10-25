using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Holiday skins: a look the island wears for a few days (Halloween, Christmas…), fetched from
    /// this repo on GitHub, so players who already have the game see it without an update.
    ///
    /// Skins/skins.json lists each skin and its dates; each skin is a folder beside it
    /// (Skins/&lt;id&gt;/: a skin.json and its pictures). <see cref="SkinService"/> reads the list at
    /// start, fetches the skin today falls in, and keeps it on the device. Skins/README.md has the
    /// format and the steps.
    ///
    /// What a skin can change: any sprite, by name (<see cref="SkinPainter"/> swaps it wherever it is
    /// drawn, island or HUD), the colour of the light (<see cref="DayNight"/>), decorations scattered
    /// on the island (<see cref="HolidayDecor"/>), snow or leaves drifting down (<see cref="HolidayFall"/>),
    /// the daytime music (<see cref="MusicPlayer"/>) and a greeting card shown once. Anything it
    /// leaves out stays as it always is. Skins dress the island's fixed scenery (the land, bridges,
    /// lanterns) — never things a player buys.
    /// </summary>
    public static class Skins
    {
        public class Decor
        {
            public Sprite Sprite;
            public int Count;
            public float Height;   // Unity units, the art's full height
        }

        public class Falling
        {
            public Sprite Sprite;
            public int Count;
            public float Size;    // share of the screen's height
            public float Speed;   // screen heights a second
            public float Sway;    // sideways drift, screen widths a second at most
            public bool Spin;
        }

        /// <summary>What drifts down over the island (<see cref="HolidayFall"/>), or null.</summary>
        public static Falling Fall { get; private set; }

        /// <summary>The skin being worn ("halloween"), or null for the island's own look.</summary>
        public static string ActiveId { get; private set; }
        public static bool Active => ActiveId != null;

        /// <summary>Raised when a skin goes on or comes off.</summary>
        public static event Action Changed;

        /// <summary>Multiplies the island's light. White when no skin is on.</summary>
        public static Color Tint { get; private set; } = Color.white;

        /// <summary>A tune that replaces the morning and afternoon music, or null.</summary>
        public static AudioClip Music { get; private set; }

        public static IReadOnlyList<Decor> Decorations => _decor;

        /// <summary>
        /// True while some sprite has a replacement, or a skin that came off left its sprites
        /// somewhere (the painter only works then).
        /// </summary>
        public static bool Painting => _textures.Count > 0 || _back.Count > 0;

        /// <summary>
        /// What a renderer showing <paramref name="s"/> should show now: the island's own sprite
        /// behind any skin's, in the skin being worn.
        /// </summary>
        public static Sprite Want(Sprite s) => Swap(Original(s));

        static readonly List<Decor> _decor = new();
        static readonly Dictionary<string, Texture2D> _textures = new();   // by sprite name
        static readonly Dictionary<Sprite, Sprite> _swap = new();          // original → worn (itself if none)
        static readonly HashSet<Sprite> _made = new();

        /// <summary>
        /// The sprite to draw instead of <paramref name="original"/>: the skin's picture of the same
        /// name, sized and pinned like the original (same pivot, same size in the world, same slicing),
        /// so art drawn at any resolution with the same proportions drops straight in.
        /// </summary>
        public static Sprite Swap(Sprite original)
        {
            if (original == null || _textures.Count == 0 || _made.Contains(original)) return original;
            if (_swap.TryGetValue(original, out var known)) return known;

            var worn = original;
            if (_textures.TryGetValue(BaseName(original.name), out var tex))
            {
                var r = original.rect;
                float k = tex.width / Mathf.Max(1f, r.width);
                worn = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(original.pivot.x / r.width, original.pivot.y / r.height),
                    original.pixelsPerUnit * k, 0, SpriteMeshType.FullRect, original.border * k);
                worn.name = original.name + " [" + ActiveId + "]";
                _made.Add(worn);
                _back[worn] = original;
            }
            _swap[original] = worn;
            return worn;
        }

        // Every skin sprite ever made this session, to its original. Kept after a skin comes off:
        // code that stored the sprite it was showing (a capybara's awake frame) may still hand it
        // out, and the painter turns it back into the island's own.
        static readonly Dictionary<Sprite, Sprite> _back = new();

        /// <summary>
        /// The skin's picture for a sprite name, for art that is cut up in code rather than drawn
        /// whole (the bridge gates), or null.
        /// </summary>
        public static Texture2D Texture(string name) => _textures.TryGetValue(name, out var t) ? t : null;

        /// <summary>The island's own sprite behind a skin's one (itself if it is not a skin's).</summary>
        public static Sprite Original(Sprite s) => s != null && _back.TryGetValue(s, out var o) ? o : s;

        // Sprites fetched from an atlas at runtime come out as "Name(Clone)".
        static string BaseName(string name) =>
            name.EndsWith("(Clone)", StringComparison.Ordinal) ? name.Substring(0, name.Length - 7) : name;

        // ---------------------------------------------------------------- put on, take off

        internal static void Wear(string id, Color tint, Dictionary<string, Texture2D> sprites, List<Decor> decor, AudioClip music, Falling fall)
        {
            Strip(raise: false);
            ActiveId = id;
            Tint = tint;
            foreach (var kv in sprites) _textures[kv.Key] = kv.Value;
            _decor.AddRange(decor);
            Music = music;
            Fall = fall;
            Debug.Log($"[Skins] Wearing \"{id}\": {sprites.Count} sprites, {decor.Count} decorations, falling {(fall != null ? fall.Sprite.name : "nothing")}, music {(music != null ? "yes" : "no")}.");
            Changed?.Invoke();
        }

        internal static void Strip(bool raise = true)
        {
            if (ActiveId == null && _textures.Count == 0) return;
            // The sprites and their pictures stay alive (see _back); decorations and music are only
            // ever held by HolidayDecor and MusicPlayer, which let go of them on Changed.
            var oldDecor = new List<Decor>(_decor);
            var oldMusic = Music;
            var oldFall = Fall;
            _textures.Clear();
            _swap.Clear();
            _decor.Clear();
            Music = null;
            Fall = null;
            Tint = Color.white;
            if (ActiveId != null) Debug.Log($"[Skins] \"{ActiveId}\" is off.");
            ActiveId = null;
            if (raise) Changed?.Invoke();
            foreach (var d in oldDecor)
                if (d.Sprite != null) { UnityEngine.Object.Destroy(d.Sprite.texture); UnityEngine.Object.Destroy(d.Sprite); }
            if (oldMusic != null) UnityEngine.Object.Destroy(oldMusic);
            // HolidayFall lets go of it at the end of this frame; the picture goes after.
            if (oldFall?.Sprite != null) { UnityEngine.Object.Destroy(oldFall.Sprite.texture, 1f); UnityEngine.Object.Destroy(oldFall.Sprite, 1f); }
        }
    }
}
