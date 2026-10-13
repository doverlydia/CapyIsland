using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Floating paper lanterns (tōrō nagashi) on the sea after dark — the old way of lighting the
    /// spirits home. One drifts for every warm heart you have brought back, so as the island warms
    /// up, its night sea fills with light.
    ///
    /// They float off the coasts (never in the surf, never under a bridge), the first ones nearest
    /// the start island, and each idles in a slow loop around its own spot, bobbing. They fade in
    /// with dusk and out with dawn, and glow by themselves (unlit), so night doesn't dim them.
    /// </summary>
    public class SeaLanterns : MonoBehaviour
    {
        const int Max = 48;
        const int WaterOrder = -145;   // on the water, under the beaches (Atmosphere's water is -150)

        class Lantern
        {
            public Transform root;
            public SpriteRenderer body, glow, shine;
            public Vector2 anchor;
            public float phase, alpha;
        }

        readonly List<Vector2> _spots = new();
        readonly List<Lantern> _lanterns = new();
        Sprite _art;
        Material _unlit;
        float _nextCount;
        int _wanted;

        void Start()
        {
            _art = Resources.Load<Sprite>("World/Lantern_Float");
            if (_art == null) { enabled = false; return; }
            _unlit = HarvestEffects.SpriteMaterial();
            FindSpots();
        }

        /// <summary>
        /// Spots on open water a little way off the coasts, spaced apart, nearest the start island
        /// first. Seeded, so every visit lays them out the same.
        /// </summary>
        void FindSpots()
        {
            var land = LandMap.Instance;
            var rig = FindFirstObjectByType<CameraRig.ResponsiveCameraRig>();
            if (land == null || rig == null) return;
            var area = rig.WorldBounds;

            Vector2 home = Vector2.zero;
            foreach (var p in land.Pieces)
                if (!p.isBridge && p.circles.Count > 0) { home = LandMap.WorldToUnity(new Vector2(p.circles[0].x, p.circles[0].y)); break; }

            bool Wet(Vector2 q) => land.IslandAt(q) < 0 && !BunniSpace.PointOnLand(q);
            bool LandWithin(Vector2 q, float r)
            {
                for (int a = 0; a < 12; a++)
                {
                    float ang = a * Mathf.PI / 6f;
                    if (!Wet(q + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.6f) * r)) return true;
                }
                return false;
            }

            var rng = new System.Random(20261013);
            var found = new List<Vector2>();
            for (int tries = 0; tries < 6000 && found.Count < Max * 3; tries++)
            {
                var q = new Vector2(
                    Mathf.Lerp(area.min.x, area.max.x, (float)rng.NextDouble()),
                    Mathf.Lerp(area.min.y, area.max.y, (float)rng.NextDouble()));
                if (!Wet(q) || LandWithin(q, 3f) || !(LandWithin(q, 6f) || LandWithin(q, 10f))) continue;
                bool crowded = false;
                foreach (var f in found) if ((f - q).sqrMagnitude < 2.6f * 2.6f) { crowded = true; break; }
                if (!crowded) found.Add(q);
            }
            found.Sort((a, b) => (a - home).sqrMagnitude.CompareTo((b - home).sqrMagnitude));
            for (int i = 0; i < found.Count && _spots.Count < Max; i++) _spots.Add(found[i]);
        }

        /// <summary>How many lanterns: one per warm heart home (a few for a friend's island).</summary>
        static int Wanted() => Visit.Active ? 8 : Motes.CollectedCount;

        void Update()
        {
            if (Time.unscaledTime >= _nextCount)
            {
                _nextCount = Time.unscaledTime + 2f;
                _wanted = Mathf.Min(Wanted(), _spots.Count);
            }

            float dark = NightLights.Darkness();
            bool show = dark > 0.01f;
            float t = Time.time;
            for (int i = 0; i < _spots.Count; i++)
            {
                bool want = show && i < _wanted;
                if (i >= _lanterns.Count)
                {
                    if (!want) break;
                    _lanterns.Add(Make(_spots[i], i));
                }
                var l = _lanterns[i];
                // Each fades on its own, a moment after the last, so they light up one by one.
                float target = want ? dark : 0f;
                l.alpha = Mathf.MoveTowards(l.alpha, target, Time.deltaTime * (want ? 0.25f : 0.5f) * (1f + (i % 5) * 0.15f));
                bool on = l.alpha > 0.002f;
                if (l.root.gameObject.activeSelf != on) l.root.gameObject.SetActive(on);
                if (!on) continue;

                // A slow loop round its spot, a bob, and a candle's flicker.
                float p = t * 0.06f + l.phase;
                l.root.position = l.anchor + new Vector2(Mathf.Sin(p) * 1.1f, Mathf.Sin(p * 1.7f) * 0.45f);
                float bob = Mathf.Sin(t * 1.3f + l.phase * 3f);
                l.body.transform.localPosition = new Vector3(0f, bob * 0.03f, 0f);
                float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(t * 3f, l.phase * 10f);
                l.body.color = new Color(1f, 1f, 1f, l.alpha);
                l.glow.color = new Color(1f, 0.72f, 0.35f, 0.6f * l.alpha * flicker);
                l.shine.color = new Color(1f, 0.78f, 0.45f, 0.3f * l.alpha * flicker);
            }
        }

        Lantern Make(Vector2 at, int index)
        {
            var root = new GameObject("SeaLantern" + index).transform;
            root.SetParent(transform, true);
            root.position = at;

            SpriteRenderer Layer(string name, Sprite sprite, int order, Vector3 scale, Vector3 offset)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root, false);
                go.transform.localPosition = offset;
                go.transform.localScale = scale;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sharedMaterial = _unlit;
                sr.sortingOrder = order;
                return sr;
            }

            var glowSprite = UI.IconPainter.Glow;
            float g = 1f / Mathf.Max(0.01f, glowSprite.bounds.size.x);
            var l = new Lantern
            {
                root = root,
                anchor = at,
                phase = index * 2.399f,   // the golden angle: no two drift in step
                // Its light on the water: a warm pool around it, and a long soft streak below.
                glow = Layer("Glow", glowSprite, WaterOrder - 2, new Vector3(2.8f * g, 1.7f * g, 1f), new Vector3(0f, -0.1f, 0f)),
                shine = Layer("Reflection", glowSprite, WaterOrder - 1, new Vector3(0.7f * g, 2.4f * g, 1f), new Vector3(0f, -0.9f, 0f)),
                body = Layer("Lantern", _art, WaterOrder, Vector3.one * (0.9f / Mathf.Max(0.01f, _art.bounds.size.x)), Vector3.zero),
            };
            root.gameObject.SetActive(false);
            return l;
        }
    }
}
