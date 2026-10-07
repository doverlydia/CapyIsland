using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// Flat white UI glyphs — menu, map, goals, close, check, lock and the rest — painted at startup
    /// from signed-distance shapes, so they are crisp at any size and tint with Image.color.
    ///
    /// Shapes work in a square from -1 to 1 with y up; a distance is negative inside a shape.
    /// </summary>
    public static class IconPainter
    {
        public delegate float Sdf(float x, float y);

        static readonly Dictionary<string, Sprite> _cache = new();

        public static Sprite Paint(int size, Sdf shape)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            var px = new Color32[size * size];
            float pixel = 2f / size;
            for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                float x = (i + 0.5f) / size * 2f - 1f;
                float y = (j + 0.5f) / size * 2f - 1f;
                float d = shape(x, y);
                float a = Mathf.Clamp01(0.5f - d / pixel);
                px[j * size + i] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Like <see cref="Cached"/>, with an edge feathered over <paramref name="softness"/> (in -1..1 units).</summary>
        static Sprite CachedSoft(string key, Sdf shape, float softness, int size)
        {
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave,
            };
            var px = new Color32[size * size];
            for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                float x = (i + 0.5f) / size * 2f - 1f, y = (j + 0.5f) / size * 2f - 1f;
                float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-softness, softness, shape(x, y)));
                px[j * size + i] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            s.name = "Icon_" + key;
            _cache[key] = s;
            return s;
        }

        static Sprite Cached(string key, Sdf shape, int size = 128)
        {
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
            s = Paint(size, shape);
            s.name = "Icon_" + key;
            _cache[key] = s;
            return s;
        }

        // ---------------------------------------------------------------- primitives

        public static float Disc(float x, float y, float cx, float cy, float r) =>
            Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

        public static float Capsule(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float px = x - ax, py = y - ay, dx = bx - ax, dy = by - ay;
            float h = Mathf.Clamp01((px * dx + py * dy) / Mathf.Max(1e-6f, dx * dx + dy * dy));
            float qx = px - dx * h, qy = py - dy * h;
            return Mathf.Sqrt(qx * qx + qy * qy) - r;
        }

        public static float Box(float x, float y, float cx, float cy, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(x - cx) - (hx - r), qy = Mathf.Abs(y - cy) - (hy - r);
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        /// <summary>A polyline of capsules.</summary>
        public static float Line(float x, float y, float r, params float[] pts)
        {
            float d = float.MaxValue;
            for (int k = 0; k + 3 < pts.Length; k += 2)
                d = Mathf.Min(d, Capsule(x, y, pts[k], pts[k + 1], pts[k + 2], pts[k + 3], r));
            return d;
        }

        /// <summary>Triangle (a, b, c), filled, with corners rounded by <paramref name="r"/>.</summary>
        public static float Triangle(float x, float y, Vector2 a, Vector2 b, Vector2 c, float r)
        {
            var p = new Vector2(x, y);
            Vector2 e0 = b - a, e1 = c - b, e2 = a - c;
            Vector2 v0 = p - a, v1 = p - b, v2 = p - c;
            Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            float dx = Mathf.Min(Mathf.Min(pq0.sqrMagnitude, pq1.sqrMagnitude), pq2.sqrMagnitude);
            float dy = Mathf.Min(Mathf.Min(s * (v0.x * e0.y - v0.y * e0.x), s * (v1.x * e1.y - v1.y * e1.x)), s * (v2.x * e2.y - v2.y * e2.x));
            return -Mathf.Sqrt(dx) * Mathf.Sign(dy) - r;
        }

        static float Union(float a, float b) => Mathf.Min(a, b);
        static float Cut(float a, float b) => Mathf.Max(a, -b);

        // ---------------------------------------------------------------- the set

        public static Sprite Ring => Cached("Ring", (x, y) => Mathf.Abs(Disc(x, y, 0f, 0f, 0.93f)) - 0.035f, 192);

        public static Sprite Menu => Cached("Menu", (x, y) =>
            Union(Union(Capsule(x, y, -0.62f, 0.5f, 0.62f, 0.5f, 0.13f), Capsule(x, y, -0.62f, 0f, 0.62f, 0f, 0.13f)),
                  Capsule(x, y, -0.62f, -0.5f, 0.62f, -0.5f, 0.13f)));

        /// <summary>A folded paper map, drawn as an outline with its creases.</summary>
        public static Sprite Map => Cached("Map", (x, y) =>
        {
            const float w = 0.1f;
            float top = Line(x, y, w, -0.8f, 0.55f, -0.27f, 0.78f, 0.27f, 0.55f, 0.8f, 0.78f);
            float bottom = Line(x, y, w, -0.8f, -0.78f, -0.27f, -0.55f, 0.27f, -0.78f, 0.8f, -0.55f);
            float folds = Union(Union(Capsule(x, y, -0.8f, 0.55f, -0.8f, -0.78f, w), Capsule(x, y, -0.27f, 0.78f, -0.27f, -0.55f, w)),
                                Union(Capsule(x, y, 0.27f, 0.55f, 0.27f, -0.78f, w), Capsule(x, y, 0.8f, 0.78f, 0.8f, -0.55f, w)));
            return Union(Union(top, bottom), folds);
        });

        /// <summary>A checklist: three rows, the first ticked.</summary>
        public static Sprite Goals => Cached("Goals", (x, y) =>
        {
            float tick = Line(x, y, 0.09f, -0.82f, 0.56f, -0.64f, 0.38f, -0.32f, 0.76f);
            float dot2 = Disc(x, y, -0.6f, -0.02f, 0.13f);
            float dot3 = Disc(x, y, -0.6f, -0.56f, 0.13f);
            float lines = Union(Union(Capsule(x, y, -0.1f, 0.54f, 0.78f, 0.54f, 0.11f), Capsule(x, y, -0.1f, -0.02f, 0.78f, -0.02f, 0.11f)),
                                Capsule(x, y, -0.1f, -0.56f, 0.5f, -0.56f, 0.11f));
            return Union(Union(tick, Union(dot2, dot3)), lines);
        });

        public static Sprite Close => Cached("Close", (x, y) =>
            Union(Capsule(x, y, -0.55f, -0.55f, 0.55f, 0.55f, 0.14f), Capsule(x, y, -0.55f, 0.55f, 0.55f, -0.55f, 0.14f)));

        public static Sprite Check => Cached("Check", (x, y) => Line(x, y, 0.15f, -0.62f, 0.02f, -0.18f, -0.44f, 0.66f, 0.5f));

        public static Sprite Lock => Cached("Lock", (x, y) =>
        {
            float body = Box(x, y, 0f, -0.3f, 0.62f, 0.5f, 0.14f);
            float shackle = y > -0.1f ? Mathf.Abs(Disc(x, y, 0f, 0.28f, 0.38f)) - 0.11f : float.MaxValue;
            float legs = Union(Capsule(x, y, -0.38f, 0.28f, -0.38f, -0.1f, 0.11f), Capsule(x, y, 0.38f, 0.28f, 0.38f, -0.1f, 0.11f));
            float keyhole = Disc(x, y, 0f, -0.25f, 0.12f);
            return Cut(Union(Union(body, shackle), legs), keyhole);
        });

        public static Sprite Gear => Cached("Gear", (x, y) =>
        {
            float angle = Mathf.Atan2(y, x);
            float rad = Mathf.Sqrt(x * x + y * y);
            // Eight teeth: push the rim out where cos(8θ) is high.
            float teeth = Mathf.Clamp01((Mathf.Cos(angle * 8f) - 0.2f) * 2.5f) * 0.18f;
            float outer = rad - (0.66f + teeth);
            return Cut(outer, Disc(x, y, 0f, 0f, 0.28f));
        });

        public static Sprite Gift => Cached("Gift", (x, y) =>
        {
            float box = Box(x, y, 0f, -0.3f, 0.66f, 0.5f, 0.1f);
            float lid = Box(x, y, 0f, 0.28f, 0.78f, 0.17f, 0.08f);
            float gaps = Union(Box(x, y, 0f, -0.05f, 0.11f, 1f, 0f), Box(x, y, 0f, 0.07f, 1f, 0.04f, 0f));
            float bow = Union(Disc(x, y, -0.26f, 0.62f, 0.18f), Disc(x, y, 0.26f, 0.62f, 0.18f));
            return Union(Cut(Union(box, lid), gaps), Cut(bow, Disc(x, y, 0f, 0.52f, 0.06f)));
        });

        public static Sprite Hanger => Cached("Hanger", (x, y) =>
        {
            float hook = y > 0.45f ? Mathf.Abs(Disc(x, y, 0f, 0.62f, 0.2f)) - 0.08f : float.MaxValue;
            if (x > 0.05f && y > 0.45f && y < 0.7f) hook = float.MaxValue;   // open the hook
            float neck = Capsule(x, y, 0f, 0.42f, 0f, 0.2f, 0.08f);
            float frame = Line(x, y, 0.09f, 0f, 0.2f, -0.82f, -0.42f, 0.82f, -0.42f, 0f, 0.2f);
            return Union(Union(hook, neck), frame);
        });

        public static Sprite Star => Cached("Star", (x, y) =>
        {
            float angle = Mathf.Atan2(x, y);
            float rad = Mathf.Sqrt(x * x + y * y);
            float k = Mathf.Cos(angle * 5f) * 0.5f + 0.5f;   // 1 at the points
            return rad - Mathf.Lerp(0.42f, 0.9f, Mathf.Pow(k, 2.4f));
        });

        public static Sprite Heart => Cached("Heart", (x, y) =>
        {
            float lobes = Union(Disc(x, y, -0.36f, 0.25f, 0.42f), Disc(x, y, 0.36f, 0.25f, 0.42f));
            float point = Triangle(x, y, new Vector2(-0.74f, 0.12f), new Vector2(0.74f, 0.12f), new Vector2(0f, -0.82f), 0.04f);
            return Union(lobes, point);
        });

        /// <summary>A rounded triangle pointing right: the edge indicator's arrowhead.</summary>
        public static Sprite Arrow => Cached("Arrow", (x, y) =>
            Triangle(x, y, new Vector2(-0.55f, 0.7f), new Vector2(0.8f, 0f), new Vector2(-0.55f, -0.7f), 0.12f));

        public static Sprite Calendar => Cached("Calendar", (x, y) =>
        {
            float body = Cut(Box(x, y, 0f, -0.12f, 0.78f, 0.66f, 0.16f), Box(x, y, 0f, -0.24f, 0.62f, 0.4f, 0.06f));
            float rings = Union(Capsule(x, y, -0.36f, 0.62f, -0.36f, 0.82f, 0.09f), Capsule(x, y, 0.36f, 0.62f, 0.36f, 0.82f, 0.09f));
            float day = Box(x, y, 0.24f, -0.38f, 0.17f, 0.15f, 0.04f);
            return Union(Union(body, rings), day);
        });

        public static Sprite Sound => Cached("Sound", (x, y) =>
        {
            float box = Box(x, y, -0.55f, 0f, 0.18f, 0.24f, 0.05f);
            float cone = Triangle(x, y, new Vector2(-0.5f, 0.24f), new Vector2(0.02f, 0.62f), new Vector2(0.02f, -0.62f), 0.04f);
            cone = Cut(cone, Box(x, y, -0.62f, 0f, 0.12f, 1f, 0f));
            float wave1 = x > 0.12f ? Mathf.Abs(Disc(x, y, 0.02f, 0f, 0.32f)) - 0.07f : float.MaxValue;
            float wave2 = x > 0.12f ? Mathf.Abs(Disc(x, y, 0.02f, 0f, 0.62f)) - 0.07f : float.MaxValue;
            return Union(Union(box, Union(cone, Triangle(x, y, new Vector2(-0.55f, 0.24f), new Vector2(0.02f, 0.62f), new Vector2(-0.55f, -0.24f), 0.02f))), Union(wave1, wave2));
        });

        /// <summary>A little propeller plane seen from the side, nose to the right.</summary>
        public static Sprite Plane => Cached("Plane", (x, y) =>
        {
            float body = Capsule(x, y, -0.62f, 0.02f, 0.5f, 0.02f, 0.2f);
            float nose = Disc(x, y, 0.56f, 0.02f, 0.18f);
            // Swept wings: one reaching down towards us, one up behind the body.
            float wing = Union(Triangle(x, y, new Vector2(0.18f, -0.02f), new Vector2(-0.36f, -0.7f), new Vector2(-0.08f, -0.02f), 0.06f),
                               Triangle(x, y, new Vector2(0.1f, 0.06f), new Vector2(-0.3f, 0.52f), new Vector2(-0.1f, 0.06f), 0.05f));
            float tail = Triangle(x, y, new Vector2(-0.82f, 0.48f), new Vector2(-0.48f, 0.08f), new Vector2(-0.8f, 0.04f), 0.05f);
            return Union(Union(body, nose), Union(wing, tail));
        });

        public static Sprite House => Cached("House", (x, y) =>
        {
            float roof = Triangle(x, y, new Vector2(-0.82f, 0.08f), new Vector2(0f, 0.8f), new Vector2(0.82f, 0.08f), 0.08f);
            float walls = Box(x, y, 0f, -0.36f, 0.56f, 0.44f, 0.08f);
            float door = Box(x, y, 0f, -0.52f, 0.15f, 0.3f, 0.06f);
            return Cut(Union(roof, walls), door);
        });

        public static Sprite AddFriend => Cached("AddFriend", (x, y) =>
        {
            float head = Disc(x, y, -0.2f, 0.32f, 0.3f);
            float body = y < -0.1f ? Disc(x, y, -0.2f, -0.62f, 0.58f) : float.MaxValue;
            body = Mathf.Max(body, y - (-0.08f));
            float plus = Union(Capsule(x, y, 0.62f, 0.02f, 0.62f, 0.5f, 0.1f), Capsule(x, y, 0.38f, 0.26f, 0.86f, 0.26f, 0.1f));
            return Union(Union(head, body), plus);
        });

        /// <summary>A soft white cloud: a heap of discs with feathered edges.</summary>
        public static Sprite Cloud => CachedSoft("Cloud", (x, y) =>
        {
            float d = Disc(x, y, -0.42f, -0.12f, 0.36f);
            d = Union(d, Disc(x, y, 0.02f, 0.12f, 0.48f));
            d = Union(d, Disc(x, y, 0.46f, -0.08f, 0.38f));
            d = Union(d, Box(x, y, 0f, -0.3f, 0.82f, 0.2f, 0.18f));
            return d;
        }, 0.07f, 256);

        // Emoji features, drawn dark over a coloured disc.
        public static Sprite FaceSmile => Cached("FaceSmile", (x, y) =>
        {
            float eyes = Union(Disc(x, y, -0.3f, 0.2f, 0.11f), Disc(x, y, 0.3f, 0.2f, 0.11f));
            float smile = y < -0.05f ? Mathf.Abs(Disc(x, y, 0f, 0.05f, 0.48f)) - 0.07f : float.MaxValue;
            return Union(eyes, smile);
        });

        public static Sprite FaceLaugh => Cached("FaceLaugh", (x, y) =>
        {
            float eyeL = y > 0.18f ? Mathf.Abs(Disc(x, y, -0.3f, 0.12f, 0.15f)) - 0.05f : float.MaxValue;
            float eyeR = y > 0.18f ? Mathf.Abs(Disc(x, y, 0.3f, 0.12f, 0.15f)) - 0.05f : float.MaxValue;
            float mouth = y < -0.02f ? Disc(x, y, 0f, -0.02f, 0.46f) : float.MaxValue;
            return Union(Union(eyeL, eyeR), mouth);
        });

        public static Sprite FaceWow => Cached("FaceWow", (x, y) =>
        {
            float eyes = Union(Disc(x, y, -0.3f, 0.24f, 0.12f), Disc(x, y, 0.3f, 0.24f, 0.12f));
            float mouth = Mathf.Abs(Disc(x, y, 0f, -0.34f, 0.2f)) - 0.07f;
            return Union(eyes, mouth);
        });

        /// <summary>A waving paw: a round pad and four toes.</summary>
        public static Sprite Paw => Cached("Paw", (x, y) =>
        {
            float pad = Disc(x, y, 0f, -0.3f, 0.42f);
            float toes = Union(Union(Disc(x, y, -0.52f, 0.2f, 0.17f), Disc(x, y, -0.2f, 0.48f, 0.18f)),
                               Union(Disc(x, y, 0.2f, 0.48f, 0.18f), Disc(x, y, 0.52f, 0.2f, 0.17f)));
            return Union(pad, toes);
        });

        /// <summary>A fish seen from above, nose to the left: what a fish shadow in the water looks like.</summary>
        public static Sprite FishShadow => CachedSoft("FishShadow", (x, y) =>
        {
            float bx = (x + 0.12f) / 0.62f, by = y / 0.3f;
            float body = (Mathf.Sqrt(bx * bx + by * by) - 1f) * 0.3f;
            float tail = Triangle(x, y, new Vector2(0.42f, 0f), new Vector2(0.86f, 0.24f), new Vector2(0.86f, -0.24f), 0.04f);
            return Union(body, tail);
        }, 0.05f, 128);

        public static Sprite Rod => Cached("Rod", (x, y) =>
        {
            float pole = Capsule(x, y, -0.7f, -0.7f, 0.55f, 0.7f, 0.07f);
            float reel = Mathf.Abs(Disc(x, y, -0.36f, -0.3f, 0.16f)) - 0.06f;
            float line = x > 0.5f ? Capsule(x, y, 0.58f, 0.68f, 0.62f, -0.3f, 0.025f) : float.MaxValue;
            float hook = y < -0.2f ? Mathf.Abs(Disc(x, y, 0.5f, -0.36f, 0.13f)) - 0.035f : float.MaxValue;
            return Union(Union(pole, reel), Union(line, hook));
        });

        public static Sprite Bird => Cached("Bird", (x, y) =>
        {
            float body = Disc(x, y, 0.05f, -0.1f, 0.42f);
            float head = Disc(x, y, -0.32f, 0.3f, 0.26f);
            float beak = Triangle(x, y, new Vector2(-0.56f, 0.38f), new Vector2(-0.56f, 0.22f), new Vector2(-0.82f, 0.3f), 0.02f);
            float tail = Triangle(x, y, new Vector2(0.36f, 0.05f), new Vector2(0.86f, 0.2f), new Vector2(0.8f, -0.12f), 0.03f);
            float eye = Disc(x, y, -0.36f, 0.36f, 0.06f);
            return Cut(Union(Union(body, head), Union(beak, tail)), eye);
        });

        public static Sprite Book => Cached("Book", (x, y) =>
        {
            float left = Box(x, y, -0.38f, 0f, 0.36f, 0.6f, 0.08f);
            float right = Box(x, y, 0.38f, 0f, 0.36f, 0.6f, 0.08f);
            float spine = Box(x, y, 0f, 0f, 0.04f, 0.62f, 0f);
            float lines = Union(Union(Box(x, y, -0.38f, 0.25f, 0.2f, 0.035f, 0f), Box(x, y, -0.38f, 0.05f, 0.2f, 0.035f, 0f)),
                                Union(Box(x, y, 0.38f, 0.25f, 0.2f, 0.035f, 0f), Box(x, y, 0.38f, 0.05f, 0.2f, 0.035f, 0f)));
            return Cut(Cut(Union(left, right), spine), lines);
        });

        public static Sprite Shirt => Cached("Shirt", (x, y) => ShirtShape(x, y));

        /// <summary>The T-shirt silhouette, shared by the icon and the sewing canvas.</summary>
        public static float ShirtShape(float x, float y)
        {
            float body = Box(x, y, 0f, -0.18f, 0.5f, 0.66f, 0.1f);
            float sleeves = Union(
                Triangle(x, y, new Vector2(-0.48f, 0.46f), new Vector2(-0.94f, 0.12f), new Vector2(-0.48f, -0.08f), 0.06f),
                Triangle(x, y, new Vector2(0.48f, 0.46f), new Vector2(0.94f, 0.12f), new Vector2(0.48f, -0.08f), 0.06f));
            float neck = Disc(x, y, 0f, 0.58f, 0.2f);
            return Cut(Union(body, sleeves), neck);
        }

        public static Sprite Sparkle => Cached("Sparkle", (x, y) =>
        {
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            return Mathf.Pow(ax, 0.5f) + Mathf.Pow(ay, 0.5f) - 0.9f;
        });

        public static Sprite Cards => Cached("Cards", (x, y) =>
        {
            float back = Cut(Box(x, y, -0.18f, 0.12f, 0.46f, 0.62f, 0.1f), Box(x, y, -0.18f, 0.12f, 0.33f, 0.49f, 0.04f));
            float front = Box(x, y, 0.22f, -0.12f, 0.46f, 0.62f, 0.1f);
            return Union(Cut(back, Box(x, y, 0.22f, -0.12f, 0.58f, 0.74f, 0.14f)), front);
        });
    }
}
