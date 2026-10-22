using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CapyIsland.Game
{
    /// <summary>
    /// Which island a party or contest is on, shown in the world and on the map screen (which
    /// films the real world).
    ///
    /// A fishing contest's area is the shore, so that's where the magic is: the sea round the
    /// island catches the light. Out over the water, from just past the foam to a good six steps
    /// out, little warm sparkles wink on and off, motes of light rise and drift along the coast like
    /// a slow current carries them, now and then a big star-glint flashes, and the water itself
    /// twinkles with tiny stars and thin wavering caustic lines (the band's shader). None of it
    /// touches the sand: the beach is left exactly as it was. From far away all that would be
    /// specks, so the map camera instead sees a soft halo round the whole coast, which is what
    /// tells you at a glance where the contest is.
    ///
    /// For bugs, birds and a party, the whole island is the area: a fainter halo rings it, a light
    /// wash lies over its land and glitter rises from the ground. Everything is busier during a
    /// round and gentler while everyone gathers.
    ///
    /// The band is baked once when the gathering starts, from the island's land circles (how far
    /// each bit of sea is from this island's waterline, and from any other island's, so it never
    /// spills onto a neighbour's shore), into a small texture drawn as light with its own shader
    /// (Resources/Shaders/EventGlow). It sits on the water under the beaches, so it can never tint
    /// the land. Nothing is read back or rebuilt per frame, and nothing here makes a shadow.
    ///
    /// Made by Gathering while one is on (one per island scene); it goes when the gathering does.
    /// </summary>
    public class EventGlow : MonoBehaviour
    {
        // Draw order. The sea is at -150 and each island's painted layers lie over it: its shallows
        // (-130), then fish shadows (-125), its beach (-120) and its grass (-110). The band sits on
        // the water just over the shallows and under the beach, so it can run in under the sand
        // and never shows on land. Sparkles on the water twinkle over the fish shadows, still under
        // the beach, so one drifting shoreward slips under the sand instead of lying on it. Glitter
        // on the ground goes under everything standing on the island (YSorter's props start at its
        // base), so a tree or a capybara in front of it covers it.
        const int BandOrder = -127, GlintOrder = -122;
        const int GroundOrder = YSorter.PropSortBase - 2;

        // The band, in world units (the island art's own, the same both ways; the screen squashes
        // y by half; 25 of them make one Unity unit). The beach runs Beach past the land circles
        // before the water starts.
        const float Beach = 36f;      // as the island painter draws it
        const float Reach = 280f;     // how far out over the sea the halo fades to nothing
        const float Cell = 10f;       // one texel of the baked band: 0.4 × 0.2 Unity units

        // Where a fishing contest's sparkles play: from just clear of the foam line (half a Unity
        // unit out) to six Unity units out over the open water.
        const float SeaNear = 12.5f, SeaFar = 150f;

        // The map's camera, which sees the halo instead of the sparkles (MapScreen makes it).
        const string MapCameraName = "[MapCamera]";
        static readonly int MapLookId = Shader.PropertyToID("_EventGlowMap");
        static Camera _mapCamera;

        static EventGlow _instance;

        int _island = -1;
        GatheringKind _kind;
        bool _running;
        Color _tint;
        bool _lit;   // the band has its own light-like shader (else a plain see-through sprite stands in)

        SpriteRenderer _band, _wash;
        readonly List<Object> _made = new();   // textures, sprites and materials made at runtime, freed with it
        ParticleSystem _sparkles;              // glitter off the ground (not fishing)
        ParticleSystem _motes, _glints;        // light on the water (fishing)

        struct Spot { public Vector2 at, drift; }
        readonly List<Spot> _spots = new();
        float _debt;

        // The spots on (or near) the screen just now, refreshed when the camera moves.
        readonly List<int> _inView = new();
        Rect _lastView;
        bool _viewKnown;

        /// <summary>Glows the gathering's island (making the glow if needed) and sets how lively it is.</summary>
        public static void Ensure(int island, GatheringKind kind, bool running)
        {
            if (_instance != null && (_instance._island != island || _instance._kind != kind)) Clear();
            if (_instance == null)
            {
                var land = LandMap.Instance;
                if (land == null || land.Island(island) == null) return;
                var go = new GameObject("[EventGlow]");
                _instance = go.AddComponent<EventGlow>();
                _instance.Build(land, island, kind);
            }
            _instance._running = running;
        }

        /// <summary>It's over: the glow goes.</summary>
        public static void Clear()
        {
            if (_instance == null) return;
            Destroy(_instance.gameObject);
            _instance = null;
        }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += OnCameraDraws;

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnCameraDraws;
            Shader.SetGlobalFloat(MapLookId, 0f);
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
            foreach (var o in _made) if (o != null) Destroy(o);
            _made.Clear();
        }

        /// <summary>
        /// Just before each camera draws: the map's camera gets the halo, every other one the
        /// sparkling water. One switch the band's shader reads, so a single sprite serves both.
        /// The camera's name is only asked of cameras drawing into a picture, and only until the
        /// map's is found, so this costs nothing per frame.
        /// </summary>
        static void OnCameraDraws(ScriptableRenderContext context, Camera cam)
        {
            if (_mapCamera == null && cam != null && cam.targetTexture != null && cam.name == MapCameraName) _mapCamera = cam;
            Shader.SetGlobalFloat(MapLookId, cam != null && cam == _mapCamera ? 1f : 0f);
        }

        /// <summary>
        /// The glow's colour for each kind. Fishing's gold leans a little towards peach, because
        /// plain yellow light on blue-green water turns greenish; with the white-gold sparkles it
        /// reads as warm light by day and glows clearly by dusk and night.
        /// </summary>
        static Color TintOf(GatheringKind kind) => kind switch
        {
            GatheringKind.Fishing => new Color(1f, 0.78f, 0.42f),    // warm gold along the shore
            GatheringKind.Bugs => new Color(0.82f, 1f, 0.4f),        // fresh lime
            GatheringKind.Birds => new Color(0.8f, 0.72f, 1f),       // soft lavender
            _ => new Color(1f, 0.6f, 0.82f),                         // party pink
        };

        bool OnShore => _kind == GatheringKind.Fishing;

        void Build(LandMap land, int island, GatheringKind kind)
        {
            _island = island;
            _kind = kind;
            _tint = TintOf(kind);
            var piece = land.Island(island);
            var layers = piece.layers;
            SpriteRenderer Layer(int i) => layers != null && i < layers.Count ? layers[i] : null;

            var shore = Layer(0);
            int sortingLayer = shore != null ? shore.sortingLayerID : 0;
            _band = Band(land, piece, island, sortingLayer);
            if (OnShore)
            {
                MakeSeaLights(sortingLayer);
                return;
            }
            // Bugs, birds and a party have the whole island: a faint wash over its land too.
            _wash = Copy(Layer(2), "Wash", 1, 1f);
            LandSpots(land, piece, island);
            _sparkles = MakeSparkles(GroundOrder, sortingLayer);
        }

        // ---------------------------------------------------------------- the band of light

        /// <summary>
        /// Bakes the band round the island. For every bit of sea near it: how far it is from this
        /// island's land and from any other island's (both by a quick two-pass distance sweep over
        /// a coarse grid, as Sea.cs bakes its foam). The halo falls off with distance from the
        /// waterline, a bright core close in and a long soft tail, and fades out halfway to any
        /// neighbouring island, so it hugs this coast only. The open-water stretch where fishing's
        /// sparkles play starts clear of the foam and never reaches back onto the sand. For fishing
        /// it also picks the spots on that water where the floating sparkles are born.
        /// </summary>
        SpriteRenderer Band(LandMap land, LandMap.Piece piece, int island, int sortingLayer)
        {
            if (piece.circles.Count == 0) return null;
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            foreach (var c in piece.circles)
            {
                x0 = Mathf.Min(x0, c.x - c.z); x1 = Mathf.Max(x1, c.x + c.z);
                y0 = Mathf.Min(y0, c.y - c.z); y1 = Mathf.Max(y1, c.y + c.z);
            }
            float margin = Beach + Reach + Cell * 2f;
            x0 -= margin; y0 -= margin;
            int w = Mathf.Clamp(Mathf.CeilToInt((x1 + margin - x0) / Cell), 8, 1024);
            int h = Mathf.Clamp(Mathf.CeilToInt((y1 + margin - y0) / Cell), 8, 1024);
            y1 = y0 + h * Cell;   // the grid's southern edge (world y runs down the screen)

            // Row 0 is the southern edge: the bottom of the texture, as Unity draws it.
            Vector2 WorldOf(int i, int j) => new(x0 + (i + 0.5f) * Cell, y1 - (j + 0.5f) * Cell);

            const float Far = 1e6f;
            var mine = new float[w * h];
            var other = new float[w * h];
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    int at = land.IslandAt(LandMap.WorldToUnity(WorldOf(i, j)));
                    int k = j * w + i;
                    mine[k] = at == island ? 0f : Far;
                    other[k] = at >= 0 && at != island ? 0f : Far;
                }
            Spread(mine, w, h);
            Spread(other, w, h);

            var glow = new float[w * h];
            var line = new float[w * h];
            var sea = new float[w * h];
            for (int k = 0; k < w * h; k++)
            {
                if (other[k] <= 0f) continue;   // another island's land
                float near = mine[k] * Cell, neighbour = other[k] * Cell;
                float off = near - Beach;       // out from the waterline (below 0: under the sand, hidden)
                float own = mine[k] <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((neighbour - near) / 50f));
                if (own <= 0f) continue;
                float wide = off <= 0f ? 1f
                    : (0.7f * Mathf.Exp(-off / 60f) + 0.3f * Mathf.Exp(-off / 150f)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Reach * 0.6f, Reach, off)));
                float core = off <= 0f ? 1f : Mathf.Exp(-off / 16f);
                glow[k] = wide * own;
                line[k] = core * own;
                // The open water: in from just past the foam, out to SeaFar and softly beyond.
                if (off > SeaNear * 0.5f)
                    sea[k] = own * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(SeaNear * 0.5f, SeaNear * 1.6f, off))
                                 * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(SeaFar * 0.75f, SeaFar * 1.25f, off)));
            }
            Soften(glow, w, h);
            Soften(line, w, h);
            Soften(sea, w, h);

            if (OnShore) SeaSpots(mine, other, w, h, WorldOf);

            // The band's own shader if it's there; else a plain see-through sprite, paler.
            var material = BandMaterial();
            _lit = material != null;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true)
            {
                name = "EventGlowBand",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[w * h];
            for (int k = 0; k < w * h; k++)
            {
                byte r = (byte)Mathf.RoundToInt(Mathf.Clamp01(glow[k]) * 255f);
                byte g = (byte)Mathf.RoundToInt(Mathf.Clamp01(line[k]) * 255f);
                byte b = (byte)Mathf.RoundToInt(Mathf.Clamp01(sea[k]) * 255f);
                px[k] = _lit ? new Color32(r, g, b, 255) : new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(glow[k] * 0.7f + line[k] * 0.3f) * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);   // nothing reads it again: let the CPU copy go
            _made.Add(tex);

            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.zero, 1f, 0, SpriteMeshType.FullRect);
            sprite.name = "EventGlowBand";
            _made.Add(sprite);

            var go = new GameObject("Band");
            go.transform.SetParent(transform, false);
            go.transform.position = LandMap.WorldToUnity(new Vector2(x0, y1));   // its bottom-left corner
            go.transform.localScale = new Vector3(Cell * BunniSpace.UnitsPerWorld, Cell * BunniSpace.UnitsPerWorld * 0.5f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = material != null ? material : HarvestEffects.SpriteMaterial();
            sr.sortingLayerID = sortingLayer;
            sr.sortingOrder = BandOrder;
            sr.color = new Color(_tint.r, _tint.g, _tint.b, 0f);
            return sr;
        }

        /// <summary>
        /// The band's material. The halo (a white-gold line at the waterline and soft light over
        /// the sea) is what the map shows for fishing (bright: the shore is the whole event, and
        /// that's the look that was liked) and what every other kind always shows, more faintly.
        /// Up close, fishing's band is only sparkle on the open water: tiny stars, caustics and the
        /// faintest warm lift, never a fill. Null if the shader can't run here.
        /// </summary>
        Material BandMaterial()
        {
            var shader = Resources.Load<Shader>("Shaders/EventGlow");
            if (shader == null || !shader.isSupported) return null;
            var m = new Material(shader) { name = "EventGlow (runtime)" };
            m.SetColor("_Core", OnShore ? new Color(1f, 0.97f, 0.86f) : Color.Lerp(_tint, Color.white, 0.6f));
            // Tuned by eye in Play: at the first values the halo all but vanished in daylight.
            m.SetFloat("_Gain", OnShore ? 1.25f : 0.8f);
            m.SetFloat("_Cover", OnShore ? 0.5f : 0.36f);
            m.SetFloat("_Shimmer", OnShore ? 0.6f : 0.45f);
            m.SetFloat("_Ring", OnShore ? 0f : 1f);     // fishing: the halo on the map only
            m.SetFloat("_Sea", OnShore ? 1f : 0f);      // fishing: sparkling water up close
            m.SetFloat("_Grain", 0.5f);                 // about one little star per half a Unity unit
            _made.Add(m);
            return m;
        }

        /// <summary>
        /// Distance (in cells) from the nearest zero, spread out over the grid by a forward and a
        /// backward sweep, straight steps 1 and diagonal ones √2.
        /// </summary>
        static void Spread(float[] d, int w, int h)
        {
            const float D = 1f, Diag = 1.4142f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    float v = d[i];
                    if (x > 0) v = Mathf.Min(v, d[i - 1] + D);
                    if (y > 0)
                    {
                        v = Mathf.Min(v, d[i - w] + D);
                        if (x > 0) v = Mathf.Min(v, d[i - w - 1] + Diag);
                        if (x < w - 1) v = Mathf.Min(v, d[i - w + 1] + Diag);
                    }
                    d[i] = v;
                }
            for (int y = h - 1; y >= 0; y--)
                for (int x = w - 1; x >= 0; x--)
                {
                    int i = y * w + x;
                    float v = d[i];
                    if (x < w - 1) v = Mathf.Min(v, d[i + 1] + D);
                    if (y < h - 1)
                    {
                        v = Mathf.Min(v, d[i + w] + D);
                        if (x < w - 1) v = Mathf.Min(v, d[i + w + 1] + Diag);
                        if (x > 0) v = Mathf.Min(v, d[i + w - 1] + Diag);
                    }
                    d[i] = v;
                }
        }

        /// <summary>A light 1-2-1 blur each way, so the grid's steps never show.</summary>
        static void Soften(float[] v, int w, int h)
        {
            var tmp = new float[v.Length];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    float l = v[x > 0 ? i - 1 : i], r = v[x < w - 1 ? i + 1 : i];
                    tmp[i] = (l + 2f * v[i] + r) * 0.25f;
                }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    float d = tmp[y > 0 ? i - w : i], u = tmp[y < h - 1 ? i + w : i];
                    v[i] = (d + 2f * tmp[i] + u) * 0.25f;
                }
        }

        /// <summary>
        /// Where sparkles are born for a fishing contest: any bit of open water from just past the
        /// foam to SeaFar out (never on land or a bridge, never nearer another island), thinning a
        /// little with distance so the light gathers towards the shore. Each has a gentle drift
        /// along the coast, all the same way round, like a slow current carrying the light.
        /// </summary>
        void SeaSpots(float[] mine, float[] other, int w, int h, System.Func<int, int, Vector2> worldOf)
        {
            _spots.Clear();
            for (int j = 1; j < h - 1; j++)
                for (int i = 1; i < w - 1; i++)
                {
                    int k = j * w + i;
                    float near = mine[k] * Cell, off = near - Beach;
                    if (off < SeaNear || off > SeaFar) continue;
                    if (other[k] * Cell - near < 40f) continue;   // halfway to a neighbour: not ours
                    if (Random.value > Mathf.Lerp(1f, 0.4f, (off - SeaNear) / (SeaFar - SeaNear))) continue;
                    var at = LandMap.WorldToUnity(worldOf(i, j));
                    if (BunniSpace.PointOnLand(at)) continue;   // an open bridge over the water
                    // Along the coast: square to the way the distance grows (rows run up the screen).
                    var grad = new Vector2(mine[k + 1] - mine[k - 1], mine[k + w] - mine[k - w]);
                    var along = grad.sqrMagnitude > 1e-6f ? new Vector2(-grad.y, grad.x).normalized : Vector2.zero;
                    _spots.Add(new Spot { at = at, drift = new Vector2(along.x, along.y * 0.5f) * Random.Range(0.08f, 0.22f) });
                }
        }

        // ---------------------------------------------------------------- the land (not fishing)

        /// <summary>
        /// A plain white copy of one of the island's painted layers (its shape only), laid exactly
        /// over it, one step above in the draw order, to be tinted and pulsed. <paramref name="lift"/>
        /// below 1 brightens faint edges (alpha to that power).
        /// </summary>
        SpriteRenderer Copy(SpriteRenderer layer, string name, int above, float lift)
        {
            if (layer == null || layer.sprite == null) return null;
            var mask = WhiteMask(layer.sprite, lift);
            if (mask == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(layer.transform.position, layer.transform.rotation);
            go.transform.localScale = layer.transform.lossyScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = mask;
            sr.sharedMaterial = HarvestEffects.SpriteMaterial();   // unlit: it glows the same by night
            sr.sortingLayerID = layer.sortingLayerID;
            sr.sortingOrder = layer.sortingOrder + above;
            sr.flipX = layer.flipX;
            sr.flipY = layer.flipY;
            sr.color = new Color(_tint.r, _tint.g, _tint.b, 0f);
            return sr;
        }

        /// <summary>
        /// The sprite's shape in white, keeping its alpha. Island art isn't readable from script,
        /// so it's drawn into a render texture and read back once.
        /// </summary>
        Sprite WhiteMask(Sprite source, float lift)
        {
            var tex = source.texture;
            if (tex == null) return null;
            int w = tex.width, h = tex.height;
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var was = RenderTexture.active;
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "EventGlowMask", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            copy.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            RenderTexture.active = was;
            RenderTexture.ReleaseTemporary(rt);

            var curve = new byte[256];
            for (int a = 0; a < 256; a++) curve[a] = (byte)Mathf.RoundToInt(Mathf.Pow(a / 255f, lift) * 255f);
            var px = copy.GetPixels32();
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, curve[px[i].a]);
            copy.SetPixels32(px);
            copy.Apply(false, true);   // nothing reads it again: let the CPU copy go
            _made.Add(copy);

            var r = source.textureRect;
            var sprite = Sprite.Create(copy, r, new Vector2(source.pivot.x / r.width, source.pivot.y / r.height), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = "EventGlow_" + source.name;
            _made.Add(sprite);
            return sprite;
        }

        /// <summary>Where glitter can rise: on the island's land. Picked once, from its own land circles.</summary>
        void LandSpots(LandMap land, LandMap.Piece piece, int island)
        {
            _spots.Clear();
            if (piece.circles.Count == 0) return;
            for (int i = 0; i < 2000 && _spots.Count < 220; i++)
            {
                var c = piece.circles[Random.Range(0, piece.circles.Count)];
                var w = new Vector2(c.x, c.y) + Random.insideUnitCircle * c.z * 0.95f;
                var u = LandMap.WorldToUnity(w);
                if (land.IslandAt(u) != island) continue;
                _spots.Add(new Spot { at = u });
            }
        }

        // ---------------------------------------------------------------- glitter off the ground (not fishing)

        ParticleSystem MakeSparkles(int order, int sortingLayer)
        {
            var go = new GameObject("Sparkles");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 2.1f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.24f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 400;
            var colours = new Gradient();
            colours.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.Lerp(_tint, Color.white, 0.4f), 0.5f), new GradientColorKey(new Color(1f, 0.95f, 0.8f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(colours) { mode = ParticleSystemGradientMode.RandomColor };

            var emission = ps.emission;
            emission.rateOverTime = 0f;   // emitted by hand, at the spots (Update)

            var shape = ps.shape;
            shape.enabled = false;

            // Glitter drifts up off the ground.
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            // In softly, a twinkle, out softly.
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0.75f, 0.6f), new GradientAlphaKey(0f, 1f) });
            fade.color = g;

            var spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = order;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = HarvestEffects.SpriteMaterial();
            var sparkle = UI.IconPainter.Sparkle;
            if (sparkle != null)
            {
                var sheet = ps.textureSheetAnimation;
                sheet.enabled = true;
                sheet.mode = ParticleSystemAnimationMode.Sprites;
                sheet.SetSprite(0, sparkle);
            }
            ps.Play();
            return ps;
        }

        // ---------------------------------------------------------------- light on the water (fishing)

        /// <summary>
        /// Fishing's floating light: many small soft motes that rise a little and drift along the
        /// coast, twinkling as they go, and fewer four-pointed glints that flash and fade, a few of
        /// them big. Both unlit, so they shine the same by night, and drawn under the beach.
        /// </summary>
        void MakeSeaLights(int sortingLayer)
        {
            // Twinkling: brightness and size both flicker through a life, so no two in step.
            var twinkleAlpha = new Gradient();
            twinkleAlpha.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.14f), new GradientAlphaKey(0.45f, 0.3f),
                    new GradientAlphaKey(1f, 0.46f), new GradientAlphaKey(0.5f, 0.62f), new GradientAlphaKey(0.9f, 0.78f),
                    new GradientAlphaKey(0f, 1f),
                });
            var twinkleSize = new AnimationCurve(
                new Keyframe(0f, 0.3f), new Keyframe(0.14f, 1f), new Keyframe(0.3f, 0.65f),
                new Keyframe(0.46f, 1f), new Keyframe(0.62f, 0.7f), new Keyframe(0.78f, 0.95f), new Keyframe(1f, 0.2f));
            _motes = SeaSystem("Motes", SoftDot(), sortingLayer, twinkleAlpha, twinkleSize,
                new ParticleSystem.MinMaxCurve(1.6f, 3f), new ParticleSystem.MinMaxCurve(0.07f, 0.17f), 700, wander: 0.12f, spin: 0f);

            // A glint: up fast to a bright flash, then a slower fade, its arms shrinking away.
            var flashAlpha = new Gradient();
            flashAlpha.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
            var flashSize = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(0.22f, 1f), new Keyframe(1f, 0f));
            _glints = SeaSystem("Glints", StarGlint(), sortingLayer, flashAlpha, flashSize,
                new ParticleSystem.MinMaxCurve(0.7f, 1.2f), new ParticleSystem.MinMaxCurve(0.16f, 0.34f), 160, wander: 0f, spin: 0.5f);
        }

        ParticleSystem SeaSystem(string name, Sprite sprite, int sortingLayer, Gradient alpha, AnimationCurve size,
            ParticleSystem.MinMaxCurve life, ParticleSystem.MinMaxCurve startSize, int max, float wander, float spin)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = life;
            main.startSpeed = 0f;
            main.startSize = startSize;
            main.startRotation = spin > 0f ? new ParticleSystem.MinMaxCurve(-0.25f, 0.25f) : new ParticleSystem.MinMaxCurve(0f);
            main.maxParticles = max;
            // White, pale gold and the contest's warm gold, picked at random.
            var colours = new Gradient();
            colours.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.94f, 0.72f), 0.5f), new GradientColorKey(Color.Lerp(_tint, Color.white, 0.25f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(colours) { mode = ParticleSystemGradientMode.RandomColor };

            var emission = ps.emission;
            emission.rateOverTime = 0f;   // emitted by hand, at the spots on screen (Update)
            var shape = ps.shape;
            shape.enabled = false;

            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            fade.color = alpha;
            var grow = ps.sizeOverLifetime;
            grow.enabled = true;
            grow.size = new ParticleSystem.MinMaxCurve(1f, size);

            if (wander > 0f)
            {
                // A little lazy wandering, so the motes don't slide along rails.
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = wander;
                noise.frequency = 0.5f;
                noise.scrollSpeed = 0.3f;
                noise.damping = true;
                noise.quality = ParticleSystemNoiseQuality.Low;
            }
            if (spin > 0f)
            {
                var turn = ps.rotationOverLifetime;
                turn.enabled = true;
                turn.z = new ParticleSystem.MinMaxCurve(-spin, spin);
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = GlintOrder;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = HarvestEffects.SpriteMaterial();   // unlit: as bright by night
            if (sprite != null)
            {
                var sheet = ps.textureSheetAnimation;
                sheet.enabled = true;
                sheet.mode = ParticleSystemAnimationMode.Sprites;
                sheet.SetSprite(0, sprite);
            }
            ps.Play();
            return ps;
        }

        /// <summary>A soft round mote of light with a brighter heart.</summary>
        Sprite SoftDot() => Paint("EventGlowMote", 32, (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            float soft = Mathf.Clamp01(1f - d);
            return soft * soft * 0.85f + Mathf.Clamp01(1f - d * 3.2f) * 0.6f;
        });

        /// <summary>A four-pointed star glint: two long thin arms, a bright heart and a faint halo.</summary>
        Sprite StarGlint() => Paint("EventGlowGlint", 64, (x, y) =>
        {
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            float d = Mathf.Sqrt(x * x + y * y);
            float armH = Mathf.Pow(Mathf.Clamp01(1f - ax), 2.2f) * Mathf.Exp(-ay * ay / 0.005f);
            float armV = Mathf.Pow(Mathf.Clamp01(1f - ay), 2.2f) * Mathf.Exp(-ax * ax / 0.005f);
            float heart = Mathf.Pow(Mathf.Clamp01(1f - d * 4f), 1.5f);
            float halo = Mathf.Pow(Mathf.Clamp01(1f - d * 1.6f), 3f) * 0.3f;
            return Mathf.Max(armH, armV) + heart + halo;
        });

        /// <summary>A small white picture drawn in code, its alpha from <paramref name="alpha"/> over -1..1 each way.</summary>
        Sprite Paint(string name, int size, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(u, v)) * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _made.Add(tex);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = name;
            _made.Add(sprite);
            return sprite;
        }

        // ---------------------------------------------------------------- every frame

        void Update()
        {
            // A slow breath: brighter and quicker while a round runs. The band glows a touch less
            // at night, when the dark water makes it stand out anyway.
            float strength = _running ? 1f : 0.75f;
            float k = 0.5f + 0.5f * Mathf.Sin(Time.time * (_running ? 2.2f : 1.4f));
            float night = Mathf.Lerp(1f, 0.85f, NightLights.Darkness());
            float band = OnShore ? 0.78f + 0.22f * k : 0.55f + 0.2f * k;
            // The stand-in sprite paints rather than lights, and can't tell the map from the world:
            // for fishing it keeps to a faint hint so it never washes out the beach.
            if (!_lit) band *= OnShore ? 0.3f : 0.5f;
            Fade(_band, band * strength * night, _lit ? _tint : Color.Lerp(_tint, Color.white, 0.35f));
            Fade(_wash, (0.06f + 0.08f * k) * strength, _tint);
            if (OnShore) SeaSparkle();
            else Sparkle();
        }

        static void Fade(SpriteRenderer sr, float alpha, Color tint)
        {
            if (sr == null) return;
            sr.color = new Color(tint.r, tint.g, tint.b, alpha);
        }

        /// <summary>
        /// The view the main camera shows just now, a little enlarged so sparkles are already there
        /// as it pans; false if there's no orthographic main camera.
        /// </summary>
        static bool View(out Rect view)
        {
            var cam = Camera.main;
            view = default;
            if (cam == null || !cam.orthographic) return false;
            float hh = cam.orthographicSize + 2f, hw = cam.orthographicSize * cam.aspect + 2f;
            var c = cam.transform.position;
            view = new Rect(c.x - hw, c.y - hh, hw * 2f, hh * 2f);
            return true;
        }

        /// <summary>
        /// Glitter at the land spots (not fishing), mostly the ones on screen (an island is far
        /// bigger than the view, and that's where they're seen).
        /// </summary>
        void Sparkle()
        {
            if (_sparkles == null || _spots.Count == 0) return;
            float rate = _running ? 24f : 9f;
            _debt += rate * Time.deltaTime;
            if (_debt < 1f) return;
            bool haveView = View(out var view);
            var emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            while (_debt >= 1f)
            {
                _debt -= 1f;
                var spot = _spots[Random.Range(0, _spots.Count)];
                for (int i = 0; haveView && i < 6 && !view.Contains(spot.at); i++) spot = _spots[Random.Range(0, _spots.Count)];
                emit.position = new Vector3(spot.at.x + Random.Range(-0.2f, 0.2f), spot.at.y + Random.Range(-0.1f, 0.1f), 0f);
                _sparkles.Emit(emit, 1);
            }
        }

        /// <summary>
        /// Fishing's light on the water, born only at spots on screen (the coast is far longer than
        /// the view, and light nobody sees is wasted), as many as the water in view calls for: a
        /// wide stretch of sea on screen sparkles all over, a sliver of it a little. Most are
        /// motes; about one in seven is a glint, and about one glint in eight is a big one.
        /// </summary>
        void SeaSparkle()
        {
            if (_motes == null || _spots.Count == 0) return;
            RefreshView();
            if (_inView.Count == 0) { _debt = 0f; return; }

            float rate = Mathf.Min(_inView.Count * (_running ? 0.1f : 0.055f), _running ? 160f : 80f);
            _debt += rate * Time.deltaTime;
            int count = Mathf.Min((int)_debt, 40);   // after a hitch, don't dump a burst
            _debt -= (int)_debt;

            var emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int n = 0; n < count; n++)
            {
                var spot = _spots[_inView[Random.Range(0, _inView.Count)]];
                // Anywhere within the spot's cell (0.4 × 0.2 Unity units), rising a little as it drifts.
                emit.position = new Vector3(spot.at.x + Random.Range(-0.2f, 0.2f), spot.at.y + Random.Range(-0.1f, 0.1f), 0f);
                emit.velocity = spot.drift + new Vector2(0f, Random.Range(0.03f, 0.12f));
                if (_glints != null && Random.value < 0.14f)
                {
                    emit.ResetStartSize();
                    emit.ResetStartLifetime();
                    if (Random.value < 0.12f)
                    {
                        emit.startSize = Random.Range(0.5f, 0.8f);   // a big one, catching the sun
                        emit.startLifetime = Random.Range(1f, 1.5f);
                    }
                    _glints.Emit(emit, 1);
                }
                else
                {
                    emit.ResetStartSize();
                    emit.ResetStartLifetime();
                    _motes.Emit(emit, 1);
                }
            }
        }

        /// <summary>
        /// Which spots are on screen, worked out again only when the camera has moved or zoomed
        /// half a unit, since the coast can hold thousands of them. With no camera to go by, all.
        /// </summary>
        void RefreshView()
        {
            if (!View(out var view))
            {
                if (_viewKnown || _inView.Count != _spots.Count)
                {
                    _inView.Clear();
                    for (int i = 0; i < _spots.Count; i++) _inView.Add(i);
                    _viewKnown = false;
                }
                return;
            }
            if (_viewKnown && Mathf.Abs(view.xMin - _lastView.xMin) < 0.5f && Mathf.Abs(view.yMin - _lastView.yMin) < 0.5f
                && Mathf.Abs(view.width - _lastView.width) < 0.5f && Mathf.Abs(view.height - _lastView.height) < 0.5f) return;
            _lastView = view;
            _viewKnown = true;
            _inView.Clear();
            for (int i = 0; i < _spots.Count; i++)
                if (view.Contains(_spots[i].at)) _inView.Add(i);
        }
    }
}
