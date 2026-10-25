using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A holiday skin's decorations (pumpkins, little trees…) dotted about the open grass of
    /// whichever island is loaded, yours or a friend's. Never saved and never in the way: they go on
    /// clear ground only, are not map items (nothing counts them as taking a spot), and one that
    /// finds something built over it later quietly hides. The same island gets the same spots all
    /// skin long, so they do not jump about between visits.
    /// </summary>
    public class HolidayDecor : MonoBehaviour
    {
        const float Footprint = 12f;   // original world units: the clear ground one needs

        readonly List<SpriteRenderer> _placed = new();
        int _check;
        float _nextCheck;

        void Start()
        {
            Skins.Changed += Rebuild;
            Rebuild();
        }

        void OnDestroy() => Skins.Changed -= Rebuild;

        void Rebuild()
        {
            foreach (var sr in _placed) if (sr != null) Destroy(sr.transform.parent.gameObject);
            _placed.Clear();
            if (Skins.Decorations.Count == 0 || this == null) return;
            var land = LandMap.Instance;
            if (land == null) return;

            var open = new List<int>();
            int islands = 0;
            foreach (var p in land.Pieces) if (!p.isBridge) islands++;
            for (int i = 0; i < islands; i++) if (land.IslandOpen(i)) open.Add(i);
            if (open.Count == 0) return;

            // The same spots every time this island is built with this skin.
            var saved = Random.state;
            int seed = 17;
            foreach (char c in Skins.ActiveId + "/" + gameObject.scene.name) seed = seed * 31 + c;
            Random.InitState(seed);
            foreach (var d in Skins.Decorations)
                for (int n = 0; n < d.Count; n++)
                {
                    int island = open[Random.Range(0, open.Count)];
                    var at = land.RandomSpotOnIsland(island, Footprint, 60);
                    if (land.IslandAt(at) != island || !BunniSpace.CircleOnMap(at, Footprint) || Crowded(at)) continue;
                    Place(d, at, Random.value < 0.5f);
                }
            Random.state = saved;
        }

        bool Crowded(Vector2 at)
        {
            foreach (var sr in _placed)
                if (BunniSpace.WorldDistance(at, sr.transform.parent.position) < Footprint * 3f) return true;
            return false;
        }

        void Place(Skins.Decor d, Vector2 feet, bool flip)
        {
            var go = new GameObject("HolidayDecor");
            go.transform.SetParent(transform, false);
            go.transform.position = feet;
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = d.Sprite;   // pivot at its feet, sized by the skin (SkinService)
            sr.flipX = flip;
            sr.sortingOrder = YSorter.PropSortBase + Mathf.RoundToInt(-feet.y * 100f);
            _placed.Add(sr);
        }

        void Update()
        {
            // One at a time, a few times a second: hide any that something has been built over.
            if (_placed.Count == 0 || Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.5f;
            _check = (_check + 1) % _placed.Count;
            var sr = _placed[_check];
            if (sr == null) return;
            sr.enabled = BunniSpace.CircleOnMap(sr.transform.parent.position, Footprint * 0.5f);
        }
    }
}
