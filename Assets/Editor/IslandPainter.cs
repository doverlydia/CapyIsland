using System.Collections.Generic;
using System.IO;
using CapyIsland.Game;
using UnityEditor;
using UnityEngine;

namespace CapyIsland.EditorTools
{
    /// <summary>
    /// Lays out and paints the archipelago. Each island is a union of circles filling its
    /// game.xml &lt;islands&gt; rect, inset from the edges so channels of water separate neighbours;
    /// each bridge is a short chain of circles across a channel at its save_data.xml &lt;bridge&gt;
    /// location. The shapes are this project's own — only the rects and bridge points come from the
    /// data.
    ///
    /// Every piece is painted into three textures drawn in three sorting bands — shallow water and
    /// foam, then sand, then grass — so where a bridge meets an island the grass of both joins
    /// seamlessly over both beaches, the way the original's overlapping land blocks did.
    /// </summary>
    public static class IslandPainter
    {
        const float Density = 10f;          // texture pixels per Unity unit
        const float Inset = 150f;            // world units of water kept inside each rect edge
        const float CornerRadius = 420f;
        const float Pad = 140f;              // world units of shore painted beyond the land

        const string Folder = "Assets/Art/Terrain/Land";

        public const int ShoreOrder = -130, SandOrder = -120, GrassOrder = -110;

        /// <summary>The islands: game.xml &lt;islands&gt; rects as fractions of the 4500 world.</summary>
        static readonly (string name, Rect[] rects)[] Islands =
        {
            ("Capy Island", new[] { R(0f, 0f, 0.25f, 0.33f) }),
            ("Heart of the Isles", new[] { R(0f, 0.33f, 0.25f, 0.33f) }),
            ("Ginger Cove", new[] { R(0f, 0.66f, 0.5f, 0.33f) }),
            ("Verdant Bay", new[] { R(0.5f, 0.66f, 0.5f, 0.33f) }),
            ("Wedding Isle", new[] { R(0.75f, 0f, 0.25f, 0.66f) }),
            ("Pirate Cove", new[] { R(0.25f, 0f, 0.5f, 0.33f), R(0.25f, 0f, 0.25f, 0.66f) }),
            ("Monster Isle", new[] { R(0.5f, 0.33f, 0.25f, 0.33f) }),
        };

        /// <summary>The bridges: save_data.xml &lt;bridge&gt; locs, (x, y, 1 = runs along x).</summary>
        static readonly (string name, Vector3[] locs)[] Bridges =
        {
            ("first_bridge", new[] { new Vector3(562, 1485, 0) }),
            ("second_bridge", new[] { new Vector3(562, 2970, 0) }),
            ("pete_isle", new[] { new Vector3(2250, 3712, 1) }),
            ("pirate_cove", new[] { new Vector3(1125, 2250, 1), new Vector3(1687, 2970, 0) }),
            ("wedding_isle", new[] { new Vector3(3937, 2970, 0) }),
            ("monster_isle", new[] { new Vector3(2812, 1485, 0), new Vector3(2812, 2970, 0), new Vector3(3375, 2250, 1), new Vector3(2250, 2250, 1) }),
        };

        static Rect R(float x, float y, float w, float h) =>
            new Rect(x * LandMap.WorldSize, y * LandMap.WorldSize, w * LandMap.WorldSize, h * LandMap.WorldSize);

        // ---------------------------------------------------------------- shapes

        public static List<LandMap.Piece> Layout()
        {
            var pieces = new List<LandMap.Piece>();
            for (int i = 0; i < Islands.Length; i++)
            {
                var piece = new LandMap.Piece { name = Islands[i].name, isBridge = false, open = true };
                piece.rects.AddRange(Islands[i].rects);
                piece.circles = IslandCircles(Islands[i].rects, 1000 + i * 17);
                pieces.Add(piece);
            }
            for (int i = 0; i < Bridges.Length; i++)
            {
                var piece = new LandMap.Piece { name = Bridges[i].name, isBridge = true, open = false };
                piece.circles = BridgeCircles(Bridges[i].locs, 5000 + i * 31);
                pieces.Add(piece);
            }
            return pieces;
        }

        static List<Vector3> IslandCircles(Rect[] rects, int seed)
        {
            var rng = new System.Random(seed);
            float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            float noiseOffset = Rand(0f, 1000f);

            var circles = new List<Vector3>();
            const float step = 165f;
            foreach (var rect in rects)
            {
                for (float x = rect.xMin + Inset; x <= rect.xMax - Inset + 1f; x += step)
                for (float y = rect.yMin + Inset; y <= rect.yMax - Inset + 1f; y += step)
                {
                    var p = new Vector2(x + Rand(-35f, 35f), y + Rand(-35f, 35f));
                    float inside = InsideDistance(p, rects);
                    // A wavy coast: push the edge in and out along its length.
                    inside += (Mathf.PerlinNoise(noiseOffset + p.x / 560f, noiseOffset + p.y / 560f) - 0.5f) * 300f
                            + (Mathf.PerlinNoise(noiseOffset + 40f + p.x / 230f, noiseOffset + 40f + p.y / 230f) - 0.5f) * 110f;
                    if (inside < 45f) continue;
                    float r = Mathf.Min(Rand(165f, 235f), inside + 30f);
                    circles.Add(new Vector3(p.x, p.y, r));
                }
            }
            return circles;
        }

        /// <summary>Distance from a point to the edge of the island's inset, rounded rects (positive inside).</summary>
        static float InsideDistance(Vector2 p, Rect[] rects)
        {
            float best = float.NegativeInfinity;
            foreach (var r in rects)
            {
                var inner = new Rect(r.xMin + Inset, r.yMin + Inset, r.width - Inset * 2f, r.height - Inset * 2f);
                var half = inner.size * 0.5f;
                var q = new Vector2(Mathf.Abs(p.x - inner.center.x), Mathf.Abs(p.y - inner.center.y)) - (half - Vector2.one * CornerRadius);
                float outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - CornerRadius;
                best = Mathf.Max(best, -outside);
            }
            return best;
        }

        static List<Vector3> BridgeCircles(Vector3[] locs, int seed)
        {
            var rng = new System.Random(seed);
            var circles = new List<Vector3>();
            foreach (var loc in locs)
            {
                bool alongX = loc.z > 0.5f;
                float drift = 0f;
                for (int k = -2; k <= 2; k++)
                {
                    drift += ((float)rng.NextDouble() - 0.5f) * 50f;
                    var along = k * 150f;
                    var c = alongX ? new Vector2(loc.x + along, loc.y + drift) : new Vector2(loc.x + drift, loc.y + along);
                    circles.Add(new Vector3(c.x, c.y, 135f));
                }
            }
            return circles;
        }

        // ---------------------------------------------------------------- painting

        /// <summary>
        /// Paints (or reuses) the three layers of a piece and places them in the scene under
        /// <paramref name="parent"/>. Returns the renderers: shore, sand, grass.
        /// </summary>
        public static List<SpriteRenderer> Paint(LandMap.Piece piece, int index, Transform parent, bool repaint)
        {
            Directory.CreateDirectory(Folder);
            string stem = $"{Folder}/Land_{index:00}_{Sanitise(piece.name)}";

            var bounds = WorldBounds(piece.circles, Pad);
            int w = Mathf.Max(8, Mathf.CeilToInt(bounds.width * BunniSpace.UnitsPerWorld * Density));
            int h = Mathf.Max(8, Mathf.CeilToInt(bounds.height * BunniSpace.UnitsPerWorld * 0.5f * Density));

            string[] layerNames = { "Shore", "Sand", "Grass" };
            if (repaint || !File.Exists(stem + "_Grass.png"))
            {
                var textures = Rasterise(piece.circles, bounds, w, h, index * 7.13f);
                for (int l = 0; l < 3; l++)
                {
                    File.WriteAllBytes($"{stem}_{layerNames[l]}.png", textures[l].EncodeToPNG());
                    Object.DestroyImmediate(textures[l]);
                }
                AssetDatabase.Refresh();
            }

            var go = new GameObject(piece.name);
            go.transform.SetParent(parent, false);
            var centreWorld = bounds.center;
            go.transform.localPosition = LandMap.WorldToUnity(centreWorld);

            var renderers = new List<SpriteRenderer>();
            int[] orders = { ShoreOrder, SandOrder, GrassOrder };
            for (int l = 0; l < 3; l++)
            {
                string path = $"{stem}_{layerNames[l]}.png";
                ConfigureImporter(path);
                var layer = new GameObject(layerNames[l]);
                layer.transform.SetParent(go.transform, false);
                var sr = layer.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                sr.sortingOrder = orders[l];
                renderers.Add(sr);
            }
            return renderers;
        }

        static string Sanitise(string s) => s.Replace(" ", "").Replace("'", "");

        static Rect WorldBounds(List<Vector3> circles, float pad)
        {
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            foreach (var c in circles)
            {
                xMin = Mathf.Min(xMin, c.x - c.z); xMax = Mathf.Max(xMax, c.x + c.z);
                yMin = Mathf.Min(yMin, c.y - c.z); yMax = Mathf.Max(yMax, c.y + c.z);
            }
            return Rect.MinMaxRect(xMin - pad, yMin - pad, xMax + pad, yMax + pad);
        }

        static void ConfigureImporter(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            bool dirty = importer.textureType != TextureImporterType.Sprite
                         || !Mathf.Approximately(importer.spritePixelsPerUnit, Density)
                         || importer.mipmapEnabled;
            if (!dirty) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Density;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        /// <summary>Shore, sand and grass textures for one piece.</summary>
        static Texture2D[] Rasterise(List<Vector3> circles, Rect bounds, int w, int h, float seed)
        {
            var shore = new Color32[w * h];
            var sand = new Color32[w * h];
            var grass = new Color32[w * h];

            float worldPerPxX = 1f / (Density * BunniSpace.UnitsPerWorld);
            float worldPerPxY = 1f / (Density * BunniSpace.UnitsPerWorld * 0.5f);
            float aa = Mathf.Max(worldPerPxX, worldPerPxY) * 0.9f;

            var grassBase = new Color(0.498f, 0.686f, 0.310f);
            var grassLight = new Color(0.560f, 0.735f, 0.335f);
            var grassLip = new Color(0.405f, 0.585f, 0.262f);
            var sandDry = new Color(0.878f, 0.788f, 0.604f);
            var sandWet = new Color(0.760f, 0.668f, 0.500f);
            var shallow = new Color(0.720f, 0.900f, 0.940f);

            for (int j = 0; j < h; j++)
            {
                float wy = bounds.yMax - (j + 0.5f) * worldPerPxY;   // row 0 is the bottom of the texture
                for (int i = 0; i < w; i++)
                {
                    float wx = bounds.xMin + (i + 0.5f) * worldPerPxX;

                    // Signed distance to the union's edge, in world units: positive on land.
                    float sd = float.NegativeInfinity;
                    foreach (var c in circles)
                    {
                        float dx = wx - c.x, dy = wy - c.y;
                        float d = c.z - Mathf.Sqrt(dx * dx + dy * dy);
                        if (d > sd) sd = d;
                    }

                    int k = j * w + i;

                    // --- grass: soft patches of lighter and darker green, with a lip at the edge
                    if (sd > -aa)
                    {
                        float n1 = Mathf.PerlinNoise(seed + wx / 260f, seed + wy / 260f);
                        float n2 = Mathf.PerlinNoise(seed + 50f + wx / 90f, seed + 50f + wy / 90f);
                        var g = Color.Lerp(grassBase, grassLight, Mathf.SmoothStep(0.35f, 0.75f, n1) * 0.8f);
                        g *= 0.97f + n2 * 0.06f;
                        g = Color.Lerp(g, grassLip, Mathf.Clamp01(1f - sd / 22f) * 0.7f);
                        g.a = Mathf.Clamp01((sd + aa) / (aa * 2f));
                        grass[k] = g;
                    }

                    // --- sand: a beach band running out from under the grass
                    const float beach = 36f;
                    if (sd > -beach - aa)
                    {
                        float wet = Mathf.Clamp01((-sd - (beach - 14f)) / 14f);
                        var s = Color.Lerp(sandDry, sandWet, wet);
                        float speck = Mathf.PerlinNoise(seed + 300f + wx / 25f, seed + 300f + wy / 25f);
                        s *= 0.98f + speck * 0.04f;
                        s.a = Mathf.Clamp01((sd + beach + aa) / (aa * 2f));
                        sand[k] = s;
                    }

                    // --- shore: a foam line just off the beach, then shallow water fading out
                    float off = -sd - beach;   // distance out from the waterline
                    if (off > -aa && off < Pad)
                    {
                        float foam = Mathf.Clamp01(1f - Mathf.Abs(off - 7f) / 5f);
                        float shal = Mathf.Pow(Mathf.Clamp01(1f - off / (Pad - 20f)), 1.6f) * 0.6f;
                        var c = Color.Lerp(shallow, Color.white, foam);
                        c.a = Mathf.Max(foam * 0.85f, shal);
                        shore[k] = c;
                    }
                }
            }

            return new[] { Make(w, h, shore), Make(w, h, sand), Make(w, h, grass) };
        }

        static Texture2D Make(int w, int h, Color32[] px)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.SetPixels32(px);
            t.Apply();
            return t;
        }
    }
}
