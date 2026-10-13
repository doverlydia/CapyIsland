using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Stone lanterns (tōrō): a pair either side of Old Mochi's shrine, and a pair at each end of
    /// every open bridge, guarding the way under its torii. By day just stone; after dark a candle
    /// glows in each window and a warm pool of light spreads on the ground around it.
    ///
    /// Each has a small footprint so nothing is built on top of it, and a spot that is already
    /// taken (something you built there first) simply goes without. Never saved: they are laid out
    /// again, the same, every time the island is built.
    /// </summary>
    public class StoneLanterns : MonoBehaviour
    {
        const float Height = 1.5f;          // how tall one stands, in Unity units
        const float Footprint = 14f;        // in original world units
        const int GroundOrder = -39;        // the light on the grass, under every prop

        // Measured on the art: its visible height, its feet and its window, as fractions of the frame.
        const float ArtVisible = 879f / 1024f, ArtFeet = 961f / 1024f;
        static readonly Vector2 ArtWindow = new(437f / 1024f, 490f / 1024f);

        class Lantern { public SpriteRenderer window, pool; public float phase; }

        readonly List<Lantern> _lanterns = new();
        readonly HashSet<string> _bridgesDone = new();
        Sprite _art;
        Material _unlit;

        IEnumerator Start()
        {
            _art = Resources.Load<Sprite>("World/Toro");
            if (_art == null) yield break;
            _unlit = HarvestEffects.SpriteMaterial();
            // After the island is restored and the shrine has found its place.
            yield return null; yield return null; yield return null;
            var shrine = Shrine.Spot;
            if (shrine != null)
            {
                Vector2 s = shrine.position;
                Place(s + new Vector2(2.3f, -0.5f));
                Place(s + new Vector2(-3.5f, -0.6f));
            }
            var land = LandMap.Instance;
            if (land != null)
                foreach (var p in land.Pieces)
                    if (p.isBridge && p.open) Bridge(p);
            LandMap.BridgeOpened += OnBridgeOpened;
        }

        void OnDestroy() => LandMap.BridgeOpened -= OnBridgeOpened;

        void OnBridgeOpened(string name)
        {
            var land = LandMap.Instance;
            var b = land != null ? land.Bridge(name) : null;
            if (b != null) Bridge(b);
        }

        /// <summary>A pair at each end of the bridge, either side of the way, on the island's grass.</summary>
        void Bridge(LandMap.Piece bridge)
        {
            if (!_bridgesDone.Add(bridge.name) || bridge.circles.Count < 2) return;
            int n = bridge.circles.Count;
            Vector2 W(Vector3 c) => LandMap.WorldToUnity(new Vector2(c.x, c.y));
            Vector2 a = W(bridge.circles[0]), b = W(bridge.circles[n - 1]);
            var along = (b - a).normalized;
            var side = new Vector2(-along.y, along.x);
            foreach (var (end, outward) in new[] { (a, -along), (b, along) })
            {
                // A step on toward the island, then out to either side of the way.
                var at = end + outward * 1.2f;
                Place(at + side * 2.8f);
                Place(at - side * 2.8f);
            }
        }

        void Place(Vector2 feet)
        {
            if (!BunniSpace.PointOnLand(feet) || !BunniSpace.CircleOnMap(feet, Footprint)) return;

            float scale = Height / (ArtVisible * _art.bounds.size.y);
            float size = _art.bounds.size.y * scale;
            var go = new GameObject("StoneLantern");
            go.transform.SetParent(transform, false);
            go.transform.position = feet;
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, (ArtFeet - 0.5f) * size, 0f);   // the art is centred: lift it onto its feet
            body.transform.localScale = Vector3.one * scale;
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = _art;
            int order = YSorter.PropSortBase + Mathf.RoundToInt(-feet.y * 100f);
            sr.sortingOrder = order;

            // Keeps the spot: nothing can be planted on it.
            var placeable = go.AddComponent<Placeable>();
            placeable.Configure("Stone Lantern", Footprint, Footprint);
            placeable.MarkPlaced(silent: true);

            var glow = UI.IconPainter.Glow;
            float g = 1f / Mathf.Max(0.01f, glow.bounds.size.x);
            var windowAt = (Vector2)body.transform.position + new Vector2((ArtWindow.x - 0.5f) * size, (0.5f - ArtWindow.y) * size);
            var l = new Lantern
            {
                window = Light(go.transform, "Candle", windowAt, new Vector3(0.75f * g, 0.75f * g, 1f), order + 1),
                pool = Light(go.transform, "Pool", feet, new Vector3(2.6f * g, 1.4f * g, 1f), GroundOrder),
                phase = feet.x * 1.7f + feet.y,
            };
            _lanterns.Add(l);
        }

        SpriteRenderer Light(Transform parent, string name, Vector2 at, Vector3 scale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UI.IconPainter.Glow;
            sr.sharedMaterial = _unlit;
            sr.sortingOrder = order;
            sr.color = Color.clear;
            return sr;
        }

        void Update()
        {
            float dark = NightLights.Darkness();
            float t = Time.time;
            foreach (var l in _lanterns)
            {
                bool on = dark > 0.01f;
                if (l.window.enabled != on) { l.window.enabled = on; l.pool.enabled = on; }
                if (!on) continue;
                float flicker = 0.82f + 0.18f * Mathf.PerlinNoise(t * 2.6f, l.phase);
                l.window.color = new Color(1f, 0.78f, 0.4f, 0.95f * dark * flicker);
                l.pool.color = new Color(1f, 0.75f, 0.42f, 0.32f * dark * flicker);
            }
        }
    }
}
