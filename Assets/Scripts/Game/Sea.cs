using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Brings the sea to life: puts the moving-water shader (Resources/Shaders/Sea) on the big
    /// water sprite under the islands, and bakes the little map it reads to know where the shore
    /// is — for each spot of sea, how far it is to the nearest island, out to a few units.
    ///
    /// Bridges aren't counted as land, so opening one changes nothing here. Built once per island
    /// (home or a friend's), in a single frame at start.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Sea : MonoBehaviour
    {
        /// <summary>How far out from the beach the foam reaches, in Unity units.</summary>
        const float FoamReach = 3.2f;

        /// <summary>How far out the water keeps getting deeper (bluer), in Unity units.</summary>
        const float DepthReach = 12f;

        /// <summary>Size of one cell of the shore map, in Unity units.</summary>
        const float Cell = 0.35f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            Attach();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) => Attach();   // a friend's island, or home again
        }

        static void Attach()
        {
            var land = LandMap.Instance != null ? LandMap.Instance : FindFirstObjectByType<LandMap>();
            var water = land != null ? land.transform.Find("Water") : null;
            if (water != null && water.GetComponent<Sea>() == null) water.gameObject.AddComponent<Sea>();
        }

        Texture2D _shore;
        Material _material;

        void Start()
        {
            var shader = Resources.Load<Shader>("Shaders/Sea");
            if (shader == null || !shader.isSupported) return;   // the flat blue sea stays
            var land = LandMap.Instance != null ? LandMap.Instance : FindFirstObjectByType<LandMap>();
            if (land == null) return;

            var sr = GetComponent<SpriteRenderer>();
            _material = new Material(shader) { name = "Sea (runtime)" };
            sr.sharedMaterial = _material;

            // The shore map covers the camera's world (everything that can be seen).
            var rig = FindFirstObjectByType<CameraRig.ResponsiveCameraRig>();
            var area = rig != null ? rig.WorldBounds : sr.bounds;
            var min = (Vector2)area.min - Vector2.one * DepthReach;
            var size = (Vector2)area.size + Vector2.one * DepthReach * 2f;
            _shore = BakeShore(land, min, size);
            _material.SetTexture("_ShoreTex", _shore);
            _material.SetVector("_ShoreRect", new Vector4(min.x, min.y, size.x, size.y));
        }

        /// <summary>
        /// Distance to the nearest island, twice over: red is 0 on the beach to 1 at
        /// <see cref="FoamReach"/> (for the foam), green the same out to <see cref="DepthReach"/>
        /// (for the deeper blue of open water). Marks the land cells, then a two-pass chamfer
        /// sweep spreads the distance out over the sea.
        /// </summary>
        static Texture2D BakeShore(LandMap land, Vector2 min, Vector2 size)
        {
            int w = Mathf.Clamp(Mathf.CeilToInt(size.x / Cell), 8, 1024);
            int h = Mathf.Clamp(Mathf.CeilToInt(size.y / Cell), 8, 1024);
            var dist = new float[w * h];
            const float Far = 1e6f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = min + new Vector2((x + 0.5f) * size.x / w, (y + 0.5f) * size.y / h);
                    dist[y * w + x] = land.IslandAt(p) >= 0 ? 0f : Far;
                }

            const float D = 1f, Diag = 1.4142f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    float v = dist[i];
                    if (x > 0) v = Mathf.Min(v, dist[i - 1] + D);
                    if (y > 0)
                    {
                        v = Mathf.Min(v, dist[i - w] + D);
                        if (x > 0) v = Mathf.Min(v, dist[i - w - 1] + Diag);
                        if (x < w - 1) v = Mathf.Min(v, dist[i - w + 1] + Diag);
                    }
                    dist[i] = v;
                }
            for (int y = h - 1; y >= 0; y--)
                for (int x = w - 1; x >= 0; x--)
                {
                    int i = y * w + x;
                    float v = dist[i];
                    if (x < w - 1) v = Mathf.Min(v, dist[i + 1] + D);
                    if (y < h - 1)
                    {
                        v = Mathf.Min(v, dist[i + w] + D);
                        if (x < w - 1) v = Mathf.Min(v, dist[i + w + 1] + Diag);
                        if (x > 0) v = Mathf.Min(v, dist[i + w - 1] + Diag);
                    }
                    dist[i] = v;
                }

            var tex = new Texture2D(w, h, TextureFormat.RG16, false, true)
            {
                name = "Sea shore",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var bytes = new byte[w * h * 2];
            float foamCells = FoamReach / Cell, depthCells = DepthReach / Cell;
            for (int i = 0; i < w * h; i++)
            {
                bytes[i * 2] = (byte)Mathf.RoundToInt(Mathf.Clamp01(dist[i] / foamCells) * 255f);
                bytes[i * 2 + 1] = (byte)Mathf.RoundToInt(Mathf.Clamp01(dist[i] / depthCells) * 255f);
            }
            tex.SetPixelData(bytes, 0);
            tex.Apply(false, true);
            return tex;
        }

        void OnDestroy()
        {
            if (_shore != null) Destroy(_shore);
            if (_material != null) Destroy(_material);
        }
    }
}
