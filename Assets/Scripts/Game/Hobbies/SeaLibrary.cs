using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Skipper Nori's beach: an old fisherman sitting on the sand with his bait box, his rod and
    /// bait laid out in front of him like a shop's wares. (Writing used to live here too, in message
    /// bottles; it has moved up the hill to Old Mochi's shrine.)
    /// </summary>
    public class SeaLibrary : MonoBehaviour
    {
        public const string Skipper = "Skipper Nori";

        public static SeaLibrary Instance { get; private set; }

        /// <summary>The middle of the beach (for "Show me").</summary>
        public static Transform Spot => Instance != null && Instance._root != null ? Instance._root.transform : null;

        GameObject _root;
        Vector2 _shoreDir;   // from the island out to sea, at the beach

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        IEnumerator Start()
        {
            // After the island (yours, or a friend's) has been built.
            yield return null;
            yield return null;
            Build();
        }

        void Build()
        {
            var land = LandMap.Instance;
            if (land == null) return;
            // At home the beach is on the Heart of the Isles, the hot spring's island. A friend's
            // island has just the one.
            var island = (!Visit.Active ? land.Island(1) : null) ?? land.Island(0);
            if (island == null) return;
            Vector2 middle = island.rects.Count > 0 ? island.rects[0].center : island.circles.Count > 0 ? (Vector2)island.circles[0] : Vector2.zero;
            var centre = LandMap.WorldToUnity(middle);

            // Walk out from the middle until the sand ends; the beach is a step inland from there.
            float start = Visit.Active ? Mathf.Repeat(Visit.Host.seed * 37f, 360f) : 235f;
            for (int turn = 0; turn < 18; turn++)
            {
                float ang = (start + turn * 20f) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Vector2? edge = null;
                for (float d = 1f; d < 40f; d += 0.25f)
                    if (!BunniSpace.PointOnLand(centre + dir * d)) { edge = centre + dir * (d - 0.25f); break; }
                if (edge == null) continue;
                var spot = edge.Value - dir * 1.4f;
                if (!BunniSpace.PointOnLand(spot)) continue;
                _shoreDir = dir;
                _root = new GameObject("SkipperNorisBeach");
                _root.transform.position = spot;
                MakeSailor(spot - new Vector2(-dir.y, dir.x) * 0.2f + dir * 0.5f);
                return;
            }
        }

        void MakeSailor(Vector2 at)
        {
            var art = HobbyData.Art("Sailor");
            if (art == null) return;
            var go = new GameObject("SkipperNori");
            go.transform.SetParent(_root.transform, false);
            go.transform.position = at;
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = art;
            go.transform.localScale = Vector3.one * (1.7f / Mathf.Max(0.01f, art.bounds.size.y));
            go.AddComponent<YSorter>();
            go.AddComponent<Sailor>().Init(sr);
            NpcTag.Add(go, Skipper, null, () => sr.sprite);   // named on the map
            LayOutWares(at);
        }

        /// <summary>
        /// His wares on the sand in front of him, like a shop's: the fishing rod, each bait and the
        /// Mermaid's Shell, in an arc on the land side (away from the water).
        /// </summary>
        void LayOutWares(Vector2 nori)
        {
            var landward = -_shoreDir;
            var side = new Vector2(-landward.y, landward.x);
            int count = 1 + Baits.All.Length + 1;
            for (int i = 0; i < count; i++)
            {
                // from one side round to the other, through straight ahead
                float t = count > 1 ? i / (float)(count - 1) : 0.5f;
                float a = Mathf.Lerp(-70f, 70f, t) * Mathf.Deg2Rad;
                var offset = (landward * Mathf.Cos(a) + side * Mathf.Sin(a)) * 1.9f;
                offset.y *= 0.65f;   // the island is seen from above at an angle: circles are squashed
                var at = nori + offset;
                for (int k = 0; k < 6 && !BunniSpace.PointOnLand(at); k++) at += landward * 0.3f;
                if (i == 0) SaleDisplay.Create("Fishing Rod", at, _root.transform);
                else if (i <= Baits.All.Length) BaitStand.Create(Baits.All[i - 1], at, _root.transform);
                else MermaidCharms.MakeStand(at, _root.transform);
            }
        }

        /// <summary>Skipper Nori: tap him for a word about the fish.</summary>
        class Sailor : MonoBehaviour, ITappable
        {
            SpriteRenderer _sr;

            public void Init(SpriteRenderer sr) => _sr = sr;

            void OnEnable() => TapTargets.Register(this);
            void OnDisable() => TapTargets.Unregister(this);

            public float TapDistance(Vector2 worldPoint)
            {
                if (_sr == null) return -1f;
                var b = _sr.bounds;
                return b.Contains(new Vector3(worldPoint.x, worldPoint.y, b.center.z)) ? Vector2.Distance(b.center, worldPoint) : -1f;
            }

            public void Tap()
            {
                string line = HobbyLog.IsUnlocked(Hobby.Fishing)
                    ? "Ahoy! The fish are biting. My bait's laid out on the sand, take your pick. Stand by the water where the shadows swim, and cast."
                    : "Ahoy! The fish are biting. My rod's laid out right there on the sand. Buy it, stand by the water where the shadows swim, and cast.";
                UI.SpeechDialog.Show(_sr.sprite, Skipper, new[] { line }, null);
            }
        }
    }
}
