using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>One kind of fish: how big its shadow is, how rare, and from what fishing level it turns up.</summary>
    public class FishKind
    {
        public string Id, Name;
        public int Shadow;            // 1 tiny … 5 huge
        public int Stars;             // rarity, 1–5
        public int MinLevel;
        public Vector2 Size;          // cm
        public Color Body, Fin;
        public DayParts Times = DayParts.Any;   // when it swims near the shore
        public bool RainOnly;                   // only bites while it rains
        /// <summary>Never in the ordinary sea: only the shimmering shadow a Mermaid's Shell calls up after five catches (MermaidCharms).</summary>
        public bool CharmOnly;
    }

    public class BirdKind
    {
        public string Id, Name;
        public int Stars, MinLevel;
        public Color Body, Wing, Breast;
        public bool Wader;            // stands on rocks and shore rather than perching in trees
        public bool Sea;              // wheels over the sea and rests on the beach (the gull): never in a tree, never wading
        public DayParts Times = DayParts.Any;
        public bool RainOnly;
    }

    /// <summary>The fish in the sea and the birds in the trees, and their pictures.</summary>
    public static partial class HobbyData
    {
        static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }

        public static readonly FishKind[] Fish =
        {
            new() { Id = "sardine",   Name = "Sardine",         Shadow = 1, Stars = 1, MinLevel = 1,  Size = new(10, 20),   Body = C("9DB4C8"), Fin = C("5E7A93") },
            new() { Id = "anchovy",   Name = "Anchovy",         Shadow = 1, Stars = 1, MinLevel = 1,  Size = new(8, 15),    Body = C("B9C6CF"), Fin = C("70838F") },
            new() { Id = "mackerel",  Name = "Mackerel",        Shadow = 2, Stars = 1, MinLevel = 1,  Size = new(25, 40),   Body = C("5F93B8"), Fin = C("2F5D80"), Times = DayParts.Dawn | DayParts.Day },
            new() { Id = "bream",     Name = "Sea Bream",       Shadow = 3, Stars = 2, MinLevel = 1,  Size = new(30, 50),   Body = C("E9A3A0"), Fin = C("C46B6B"), Times = DayParts.Day },
            new() { Id = "clownfish", Name = "Clownfish",       Shadow = 1, Stars = 2, MinLevel = 2,  Size = new(8, 12),    Body = C("F28C3A"), Fin = C("FFFFFF"), Times = DayParts.Day },
            new() { Id = "puffer",    Name = "Pufferfish",      Shadow = 2, Stars = 2, MinLevel = 2,  Size = new(15, 30),   Body = C("D9C27A"), Fin = C("8C7A3A"), Times = DayParts.Day | DayParts.Dusk },
            new() { Id = "flounder",  Name = "Flounder",        Shadow = 3, Stars = 2, MinLevel = 2,  Size = new(30, 60),   Body = C("B08A62"), Fin = C("7A5A3A"), Times = DayParts.Night | DayParts.Dawn },
            new() { Id = "squid",     Name = "Squid",           Shadow = 2, Stars = 2, MinLevel = 3,  Size = new(20, 45),   Body = C("F1E3E8"), Fin = C("D49BB0"), Times = DayParts.Night | DayParts.Dusk },
            new() { Id = "salmon",    Name = "Salmon",          Shadow = 4, Stars = 2, MinLevel = 3,  Size = new(60, 90),   Body = C("E58C6E"), Fin = C("A85A45"), Times = DayParts.Twilight },
            new() { Id = "octopus",   Name = "Octopus",         Shadow = 3, Stars = 3, MinLevel = 4,  Size = new(40, 90),   Body = C("C25A6E"), Fin = C("8A3448"), Times = DayParts.Night },
            new() { Id = "tuna",      Name = "Tuna",            Shadow = 5, Stars = 3, MinLevel = 5,  Size = new(120, 250), Body = C("3E5F86"), Fin = C("E6C34A"), Times = DayParts.Day },
            new() { Id = "yuzuray",   Name = "Yuzu Ray",        Shadow = 4, Stars = 3, MinLevel = 5,  Size = new(70, 140),  Body = C("F0C94A"), Fin = C("B98E1E"), Times = DayParts.Dawn | DayParts.Day },
            new() { Id = "moonfish",  Name = "Moonfish",        Shadow = 4, Stars = 3, MinLevel = 6,  Size = new(80, 150),  Body = C("C9D3DE"), Fin = C("D9534F"), Times = DayParts.Night },
            new() { Id = "swordfish", Name = "Swordfish",       Shadow = 5, Stars = 4, MinLevel = 7,  Size = new(150, 300), Body = C("6F8597"), Fin = C("33475A"), Times = DayParts.Day | DayParts.Dusk },
            new() { Id = "snapper",   Name = "Golden Snapper",  Shadow = 3, Stars = 4, MinLevel = 8,  Size = new(40, 70),   Body = C("F2B632"), Fin = C("D9822B"), Times = DayParts.Dusk },
            new() { Id = "steameel",  Name = "Hot Spring Eel",  Shadow = 2, Stars = 4, MinLevel = 9,  Size = new(50, 110),  Body = C("8FC7B8"), Fin = C("4F8F7F"), Times = DayParts.Night },
            new() { Id = "rainbowtrout", Name = "Rainbow Trout", Shadow = 3, Stars = 3, MinLevel = 3, Size = new(40, 70), Body = C("9DB07A"), Fin = C("D98FA8"), RainOnly = true },
            new() { Id = "coelacanth",Name = "Coelacanth",      Shadow = 5, Stars = 5, MinLevel = 10, Size = new(150, 200), Body = C("4C5B7A"), Fin = C("A9B8D6"), Times = DayParts.Night | DayParts.Dawn },
            // Only ever the shimmering shadow at a Mermaid's Shell, any time of day (MermaidCharms).
            new() { Id = "lullaby",   Name = "Lullaby Fish",    Shadow = 3, Stars = 4, MinLevel = 4,  Size = new(30, 55),   Body = C("D9C2E8"), Fin = C("F2C6D8"), CharmOnly = true },
            new() { Id = "pearlhorse",Name = "Pearl Seahorse",  Shadow = 2, Stars = 4, MinLevel = 4,  Size = new(12, 25),   Body = C("F4E4EA"), Fin = C("E9B9C8"), CharmOnly = true },
            new() { Id = "tidecrown", Name = "Crowned Tidefin", Shadow = 4, Stars = 5, MinLevel = 4,  Size = new(90, 160),  Body = C("2E7FA6"), Fin = C("E8B23E"), CharmOnly = true },
        };

        public static readonly BirdKind[] Birds =
        {
            new() { Id = "sparrow",    Name = "Sparrow",          Stars = 1, MinLevel = 1, Body = C("A47A52"), Wing = C("6E4B2E"), Breast = C("E5D3B8"), Times = DayParts.Daylight },
            new() { Id = "robin",      Name = "Robin",            Stars = 1, MinLevel = 1, Body = C("8A6A4E"), Wing = C("5E4532"), Breast = C("E5734A"), Times = DayParts.Daylight },
            new() { Id = "bluetit",    Name = "Blue Tit",         Stars = 1, MinLevel = 1, Body = C("6FA3D6"), Wing = C("3E6EA3"), Breast = C("F2D45C"), Times = DayParts.Day },
            new() { Id = "finch",      Name = "Goldfinch",        Stars = 2, MinLevel = 2, Body = C("E9C43A"), Wing = C("3A3A3A"), Breast = C("F5E07A"), Times = DayParts.Day },
            new() { Id = "dove",       Name = "Turtle Dove",      Stars = 2, MinLevel = 2, Body = C("C9B8B0"), Wing = C("8E7A70"), Breast = C("E8C7C0"), Times = DayParts.Daylight },
            new() { Id = "heron",      Name = "Grey Heron",       Stars = 2, MinLevel = 3, Body = C("A7B0B8"), Wing = C("6B7680"), Breast = C("EEF1F3"), Wader = true, Times = DayParts.Twilight },
            new() { Id = "kingfisher", Name = "Kingfisher",       Stars = 3, MinLevel = 3, Body = C("2F8FC9"), Wing = C("1E5E8E"), Breast = C("F08A3E"), Times = DayParts.Dawn | DayParts.Day },
            new() { Id = "parrot",     Name = "Parrot",           Stars = 3, MinLevel = 4, Body = C("4CB050"), Wing = C("2E6FBF"), Breast = C("E9443A"), Times = DayParts.Day },
            new() { Id = "toucan",     Name = "Toucan",           Stars = 3, MinLevel = 5, Body = C("2B2B2B"), Wing = C("1A1A1A"), Breast = C("F7E7B0"), Times = DayParts.Day },
            new() { Id = "egret",      Name = "Snowy Egret",      Stars = 3, MinLevel = 5, Body = C("F6F6F2"), Wing = C("DADAD2"), Breast = C("FFFFFF"), Wader = true, Times = DayParts.Dawn | DayParts.Day },
            new() { Id = "hummingbird",Name = "Hummingbird",      Stars = 4, MinLevel = 6, Body = C("3FBF9F"), Wing = C("2A8A73"), Breast = C("E04F8F"), Times = DayParts.Day },
            new() { Id = "crane",      Name = "Red-crowned Crane",Stars = 4, MinLevel = 7, Body = C("F4F2EC"), Wing = C("2B2B2B"), Breast = C("FFFFFF"), Wader = true, Times = DayParts.Twilight },
            new() { Id = "owl",        Name = "Tawny Owl",        Stars = 3, MinLevel = 4, Body = C("9A7550"), Wing = C("6E5236"), Breast = C("D9C29A"), Times = DayParts.Night | DayParts.Dusk },
            new() { Id = "pheasant",   Name = "Golden Pheasant",  Stars = 5, MinLevel = 9, Body = C("E8A42F"), Wing = C("B83A2A"), Breast = C("F2D04A"), Times = DayParts.Dawn },
            new() { Id = "gull",       Name = "Seagull",          Stars = 1, MinLevel = 1, Body = C("F4F4F0"), Wing = C("9AA3AD"), Breast = C("FFFFFF"), Sea = true, Times = DayParts.Daylight },
        };

        static readonly string[] RarityNames = { "Common", "Uncommon", "Rare", "Very rare", "Legendary" };

        /// <summary>Rarity in words (the fonts have no star glyph).</summary>
        public static string Rarity(int stars) => RarityNames[Mathf.Clamp(stars, 1, 5) - 1];

        /// <summary>"a Sardine", "an Anchovy".</summary>
        public static string WithArticle(string name) => ("AEIOUaeiou".IndexOf(name[0]) >= 0 ? "an " : "a ") + name;

        public static FishKind FishById(string id) => System.Array.Find(Fish, f => f.Id == id);
        public static BirdKind BirdById(string id) => System.Array.Find(Birds, b => b.Id == id);

        /// <summary>A fish worth putting in the sea for someone at this level: rarer ones less often.</summary>
        public static FishKind RollFish(int level)
        {
            return Roll(Fish, f => !f.CharmOnly && f.MinLevel <= level && GameClock.Allows(f.Times) && (!f.RainOnly || Weather.IsRaining), f => f.Stars) ?? Fish[0];
        }

        /// <summary>What the shimmering shadow at a Mermaid's Shell turns out to be: one of the fish that only come to its song.</summary>
        public static FishKind RollCharmFish() => Roll(Fish, f => f.CharmOnly, f => f.Stars) ?? Fish[0];

        /// <summary>A bird for this level and time of day, or null if none is about (most sleep at night).</summary>
        public static BirdKind RollBird(int level, bool wader)
        {
            return Roll(Birds, b => !b.Sea && b.MinLevel <= level && b.Wader == wader && GameClock.Allows(b.Times) && (!b.RainOnly || Weather.IsRaining), b => b.Stars);
        }

        static T Roll<T>(T[] all, System.Predicate<T> allowed, System.Func<T, int> stars) where T : class
        {
            float total = 0f;
            var pool = new List<(T item, float weight)>();
            foreach (var x in all)
            {
                if (!allowed(x)) continue;
                float w = 1f / Mathf.Pow(stars(x), 1.6f);   // ★ common, ★★★★★ very rare
                pool.Add((x, w));
                total += w;
            }
            float pick = Random.value * total;
            foreach (var (item, weight) in pool)
            {
                pick -= weight;
                if (pick <= 0f) return item;
            }
            return pool.Count > 0 ? pool[pool.Count - 1].item : null;
        }

        // ---------------------------------------------------------------- pictures

        static readonly Dictionary<string, Sprite> _pictures = new();

        /// <summary>
        /// The fish's picture: its illustration (Resources/Hobbies/Fish_&lt;id&gt;), or, until one is
        /// drawn, a side-on fish painted in its colours.
        /// </summary>
        public static Sprite FishPicture(FishKind f)
        {
            if (_pictures.TryGetValue("fish_" + f.Id, out var s) && s != null) return s;
            s = Illustration("Fish_" + f.Id);
            if (s != null) { _pictures["fish_" + f.Id] = s; return s; }
            s = Paint(128, (x, y) =>
            {
                float body = Ellipse(x, y, -0.08f, 0f, 0.62f, 0.36f);
                float tail = TriangleSdf(x, y, new Vector2(0.48f, 0f), new Vector2(0.9f, 0.34f), new Vector2(0.9f, -0.34f));
                float fin = TriangleSdf(x, y, new Vector2(-0.2f, 0.28f), new Vector2(0.12f, 0.28f), new Vector2(0.02f, 0.54f));
                float eye = Disc(x, y, -0.48f, 0.08f, 0.06f);
                if (eye < 0f) return new Color(0.15f, 0.12f, 0.1f, 1f);
                if (tail < 0f || fin < 0f) return f.Fin;
                if (body < 0f) return Color.Lerp(f.Body * 0.85f + new Color(0, 0, 0, 0.15f), Color.Lerp(f.Body, Color.white, 0.35f), Mathf.InverseLerp(0.3f, -0.3f, y));
                return Color.clear;
            });
            _pictures["fish_" + f.Id] = s;
            return s;
        }

        /// <summary>The bird's illustration, or until one is drawn, a plump little painted bird.</summary>
        public static Sprite BirdPicture(BirdKind b)
        {
            if (_pictures.TryGetValue("bird_" + b.Id, out var s) && s != null) return s;
            s = Illustration("Bird_" + b.Id);
            if (s != null) { _pictures["bird_" + b.Id] = s; return s; }
            float legs = b.Wader ? 0.35f : 0f;
            s = Paint(128, (x, y) =>
            {
                y += legs * 0.5f;
                float eye = Disc(x, y, -0.36f, 0.34f, 0.05f);
                if (eye < 0f) return new Color(0.1f, 0.08f, 0.06f, 1f);
                float beak = TriangleSdf(x, y, new Vector2(-0.5f, 0.38f), new Vector2(-0.5f, 0.24f), new Vector2(b.Wader ? -0.92f : -0.72f, 0.3f));
                if (beak < 0f) return new Color(0.95f, 0.72f, 0.25f, 1f);
                float wing = Ellipse(x, y, 0.12f, -0.02f, 0.36f, 0.2f);
                if (wing < 0f) return b.Wing;
                float head = Disc(x, y, -0.28f, 0.3f, 0.24f);
                float breast = Ellipse(x, y, -0.16f, -0.08f, 0.3f, 0.26f);
                if (breast < 0f && head > 0f) return b.Breast;
                float body = Ellipse(x, y, 0.02f, -0.02f, 0.46f, 0.32f);
                float tail = TriangleSdf(x, y, new Vector2(0.36f, 0.02f), new Vector2(0.82f, 0.12f), new Vector2(0.78f, -0.12f));
                if (head < 0f || body < 0f || tail < 0f) return b.Body;
                if (legs > 0f && (Mathf.Abs(x + 0.04f) < 0.025f || Mathf.Abs(x - 0.1f) < 0.025f) && y < -0.3f && y > -0.3f - legs * 2f)
                    return new Color(0.85f, 0.6f, 0.3f, 1f);
                return Color.clear;
            });
            _pictures["bird_" + b.Id] = s;
            return s;
        }

        /// <summary>
        /// A hobby illustration re-cut to the size the painted pictures had (1.28 units on its
        /// longer side, centred), so everything sized around the old pictures stays the same.
        /// </summary>
        static Sprite Illustration(string name)
        {
            var art = Art(name);
            if (art == null) return null;
            var r = art.rect;
            var s = Sprite.Create(art.texture, r, new Vector2(0.5f, 0.5f), Mathf.Max(r.width, r.height) / 1.28f);
            s.name = name;
            return s;
        }

        // Signed distances in a -1..1 square, as in IconPainter.
        static float Disc(float x, float y, float cx, float cy, float r) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

        static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            float dx = (x - cx) / rx, dy = (y - cy) / ry;
            return (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * Mathf.Min(rx, ry);
        }

        static float TriangleSdf(float x, float y, Vector2 a, Vector2 b, Vector2 c)
        {
            float s1 = Cross(b - a, new Vector2(x, y) - a), s2 = Cross(c - b, new Vector2(x, y) - b), s3 = Cross(a - c, new Vector2(x, y) - c);
            bool inside = (s1 >= 0 && s2 >= 0 && s3 >= 0) || (s1 <= 0 && s2 <= 0 && s3 <= 0);
            return inside ? -0.01f : 0.01f;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        /// <summary>Paints a colour field, supersampled 2×2 for smooth edges.</summary>
        static Sprite Paint(int size, System.Func<float, float, Color> colour)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave,
            };
            var px = new Color[size * size];
            for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                Color sum = Color.clear;
                for (int sy = 0; sy < 2; sy++)
                for (int sx = 0; sx < 2; sx++)
                {
                    float x = (i + 0.25f + sx * 0.5f) / size * 2f - 1f;
                    float y = (j + 0.25f + sy * 0.5f) / size * 2f - 1f;
                    var c = colour(x, y);
                    sum += new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                }
                sum /= 4f;
                px[j * size + i] = sum.a > 0f ? new Color(sum.r / sum.a, sum.g / sum.a, sum.b / sum.a, sum.a) : Color.clear;
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
