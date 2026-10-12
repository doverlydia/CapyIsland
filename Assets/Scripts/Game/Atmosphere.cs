using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CapyIsland.Game
{
    /// <summary>
    /// The island's mood through the day, on top of <see cref="Ambience"/>'s leaves, wind and rain:
    ///
    ///  * Dawn: low mist drifting over grass and sea, burning off by mid-morning; dew glinting on
    ///    the grass. Mist comes back for a while after rain.
    ///  * Day: soft cloud shadows sliding over the island, sun glints on the water, foam along the
    ///    beach, petals and the odd butterfly about the flower gardens.
    ///  * Golden hour: warm light rays and drifting pollen.
    ///  * Night: fireflies that blink, gather by trees, flowers and the shore, scatter as you pass,
    ///    and a few of which light the grass; starlight twinkling on the water; a faint blue glow
    ///    where the waves break; now and then a shooting star — tap it to make a wish. On some cold
    ///    nights, a gentle snowfall.
    ///  * Always: smoke curling from the houses and shops, slower and warmer after dark; "z"s over
    ///    sleeping capybaras and the odd heart over happy ones.
    ///
    /// Pure presentation, like Ambience: it all follows the camera, and nothing reads it (the wish
    /// gift aside). Kept light for phones: particles are emitted only around the view, and only
    /// three fireflies carry a real light.
    /// </summary>
    public class Atmosphere : MonoBehaviour
    {
        const int AirOrder = Bubble.SortBase - 480;   // over the island, under bubbles
        const int GroundOrder = -40;                  // on the ground, under every prop
        const int WaterOrder = -150;                  // on the water, tucked under the beach

        Transform _root;
        Material _lit, _unlit;
        ParticleSystem _dew, _glints, _foam, _pollen, _petals, _smoke, _zzz, _hearts, _snow, _bits;
        readonly List<SpriteRenderer> _mist = new(), _clouds = new(), _rays = new();
        readonly List<Firefly> _flies = new();
        readonly List<Butterfly> _butterflies = new();
        ShootingStar _star;

        float _nextScan, _nextSmoke, _nextZ, _nextHeart, _nextStar, _nextPetal;
        float _dewDebt, _glintDebt, _foamDebt, _pollenDebt, _snowDebt;
        readonly List<Transform> _chimneys = new(), _gardens = new(), _sleepers = new(), _capys = new(), _nature = new();

        void Start()
        {
            _root = new GameObject("Atmosphere").transform;
            _lit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default") ?? Shader.Find("Sprites/Default")) { name = "AtmosphereLit" };
            _unlit = HarvestEffects.SpriteMaterial();

            Sprite glow = Glow(), sparkle = Sparkle(), soft = SoftBlob(), dash = Dash();
            _dew = MakeSystem("Dew", sparkle, 1.2f, GroundOrder, _unlit, 0f);
            _glints = MakeSystem("WaterGlints", sparkle, 0.9f, WaterOrder, _unlit, 0f);
            _foam = MakeSystem("Foam", dash, 2.2f, WaterOrder, _unlit, 0.15f, grow: 1.4f);
            _pollen = MakeSystem("Pollen", glow, 5f, AirOrder, _unlit, 0.4f);
            var fx = HarvestEffects.Instance;
            _petals = MakeSystem("GardenPetals", fx != null ? fx.PetalSprite : glow, 4f, AirOrder, _lit, 0.5f, spin: 200f);
            _smoke = MakeSystem("ChimneySmoke", UI.IconPainter.Cloud, 4.5f, AirOrder, _lit, 0.25f, grow: 2.6f);
            _zzz = MakeSystem("Zzz", ZSprite(), 1.8f, AirOrder + 5, _unlit, 0f, grow: 1.6f);
            _hearts = MakeSystem("Hearts", UI.IconPainter.Heart, 1.6f, AirOrder + 5, _unlit, 0f, grow: 1.3f);
            _snow = MakeSystem("Snow", glow, 7f, AirOrder, _unlit, 0.35f);
            _bits = MakeSystem("WishSparkles", sparkle, 0.9f, AirOrder + 10, _unlit, 0f);

            for (int i = 0; i < 6; i++) _mist.Add(Sheet("Mist", soft, AirOrder - 30, _lit));
            for (int i = 0; i < 3; i++) _clouds.Add(Sheet("CloudShadow", UI.IconPainter.Cloud, AirOrder - 40, _unlit));
            for (int i = 0; i < 3; i++) _rays.Add(Sheet("SunRay", RaySprite(), AirOrder - 20, _unlit));
            for (int i = 0; i < 18; i++) _flies.Add(new Firefly(_root, glow, _unlit, withLight: i < 3));
            for (int i = 0; i < 4; i++) _butterflies.Add(new Butterfly(_root, _lit, i));
            _star = new GameObject("ShootingStar").AddComponent<ShootingStar>();
            _star.transform.SetParent(_root, false);
            _star.Init(glow, dash, _unlit, OnWish);

            _nextStar = Time.time + Random.Range(20f, 40f);
        }

        void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
        }

        // ---------------------------------------------------------------- the clock's moods

        static float Ramp(float x, float a, float b) => Mathf.Clamp01((x - a) / (b - a));
        /// <summary>0 before <paramref name="a"/>, up to 1 by <paramref name="b"/>, held to <paramref name="c"/>, down to 0 by <paramref name="d"/>.</summary>
        static float Bell(float h, float a, float b, float c, float d) => Mathf.Min(Ramp(h, a, b), 1f - Ramp(h, c, d));

        static float Night(float h) => Mathf.Max(Ramp(h, 18.6f, 19.6f), 1f - Ramp(h, 4.4f, 5.4f));
        static float Daylight(float h) => Bell(h, 5.8f, 7.2f, 17.6f, 19f);

        /// <summary>About one night in four is cold enough for a little snow.</summary>
        static bool SnowyNight()
        {
            long day = GameClock.Hour < 12f ? GameClock.Day - 1 : GameClock.Day;   // the night that began this evening
            uint x = (uint)(day * 2654435761L) ^ 0x9E3779B9u;
            x ^= x >> 15; x *= 2246822519u; x ^= x >> 13;
            return x % 4 == 0;
        }

        void Update()
        {
            var cam = Camera.main;
            if (cam == null || Time.timeScale <= 0f) return;
            float h = cam.orthographicSize, w = h * cam.aspect;
            Vector2 c = cam.transform.position;
            float hour = GameClock.Hour, dt = Time.deltaTime, rain = Weather.Rain;
            float night = Night(hour), day = Daylight(hour) * (1f - rain);
            float mist = Mathf.Max(Bell(hour, 4.6f, 6f, 7.6f, 9.6f), Weather.Rainbow * 0.7f, rain * 0.45f);
            float dew = Bell(hour, 5.4f, 6.4f, 7.6f, 9.2f) * (1f - rain);
            float golden = Bell(hour, 15.8f, 16.8f, 18.1f, 19f) * (1f - rain);
            float snow = SnowyNight() && rain < 0.05f ? Bell(hour < 12f ? hour + 24f : hour, 20.5f, 21.5f, 28.5f, 29.5f) : 0f;
            var wind = Wind.Instance;
            int dir = wind != null ? wind.Direction : 1;
            var king = FindFirstObjectByType<PlayerAvatar>();
            Vector2 you = king != null ? (Vector2)king.transform.position : c;

            if (Time.time >= _nextScan) { _nextScan = Time.time + 2.5f; Scan(c, w + 4f, h + 4f); }

            Sheets(_mist, c, w, h, dt, dir * 0.25f, mist * 0.3f, new Color(1f, 1f, 1f), new Vector2(w * 1.3f, h * 0.38f));
            Sheets(_clouds, c, w, h, dt, dir * 0.7f, day * 0.11f, new Color(0.12f, 0.16f, 0.28f), new Vector2(w * 1.7f, w * 1.1f));
            Rays(c, w, h, golden);
            Ground(c, w, h, dt, dew, day, night);
            Air(c, w, h, dt, golden, snow, dir);
            Life(dt, night, day);
            foreach (var f in _flies) f.Tick(c, w, h, you, night, _nature, dt);
            foreach (var b in _butterflies) b.Tick(day, _gardens, dt);
            Stars(c, w, h, night);
        }

        // ---------------------------------------------------------------- what's near

        void Scan(Vector2 c, float w, float h)
        {
            _chimneys.Clear(); _gardens.Clear(); _sleepers.Clear(); _capys.Clear(); _nature.Clear();
            bool Near(Vector2 p) => Mathf.Abs(p.x - c.x) < w && Mathf.Abs(p.y - c.y) < h;
            foreach (var tag in FindObjectsByType<CatalogItem>(FindObjectsSortMode.None))
            {
                var placeable = tag.GetComponent<Placeable>();
                if (placeable == null || !placeable.Placed || !Near(tag.transform.position)) continue;
                string n = tag.StoreName ?? "";
                if (n.Contains("House") || n.Contains("Store") || tag.GetComponent<StoreBuilding>() != null || n == "Sawmill" || n == "Bank")
                    _chimneys.Add(tag.transform);
                if (n.Contains("Garden") && !n.Contains("Vegetable")) { _gardens.Add(tag.transform); _nature.Add(tag.transform); }
                else if (n.Contains("Forest") || n.Contains("Tree")) _nature.Add(tag.transform);
            }
            foreach (var worker in FindObjectsByType<Worker>(FindObjectsSortMode.None))
            {
                if (!Near(worker.transform.position)) continue;
                if (worker.IsAsleep) _sleepers.Add(worker.transform);
                else _capys.Add(worker.transform);
            }
        }

        static float Top(Transform t, out float width)
        {
            var sr = t.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) { width = 1f; return t.position.y + 1.5f; }
            width = sr.bounds.size.x;
            return sr.bounds.max.y;
        }

        // ---------------------------------------------------------------- sheets of air: mist, cloud shadows, sun rays

        void Sheets(List<SpriteRenderer> sheets, Vector2 c, float w, float h, float dt, float speed, float alpha, Color tint, Vector2 size)
        {
            for (int i = 0; i < sheets.Count; i++)
            {
                var s = sheets[i];
                bool on = alpha > 0.004f;
                if (!on) { if (s.enabled) s.enabled = false; continue; }
                var t = s.transform;
                float spanX = w + size.x;
                // Off the view (or first time): start again just upwind, at a new height.
                if (!s.enabled || Mathf.Abs(t.position.x - c.x) > spanX || Mathf.Abs(t.position.y - c.y) > h + size.y)
                {
                    float y = c.y + Random.Range(-h, h);
                    float x = s.enabled ? c.x - Mathf.Sign(speed == 0f ? 1f : speed) * spanX * 0.95f : c.x + Random.Range(-w, w);
                    t.position = new Vector3(x, y, 0f);
                    var sb = s.sprite.bounds.size;
                    float k = Random.Range(0.8f, 1.2f);
                    t.localScale = new Vector3(size.x / sb.x * k, size.y / sb.y * k, 1f);
                    s.enabled = true;
                }
                t.position += new Vector3(speed * (0.8f + 0.1f * i) * dt, 0f, 0f);
                float wobble = 0.85f + 0.15f * Mathf.Sin(Time.time * 0.3f + i * 1.7f);
                s.color = new Color(tint.r, tint.g, tint.b, alpha * wobble);
            }
        }

        void Rays(Vector2 c, float w, float h, float golden)
        {
            for (int i = 0; i < _rays.Count; i++)
            {
                var r = _rays[i];
                r.enabled = golden > 0.01f;
                if (!r.enabled) continue;
                var t = r.transform;
                float x = c.x + (i - 1) * w * 0.8f + Mathf.Sin(Time.time * 0.07f + i) * w * 0.15f;
                t.position = new Vector3(x, c.y + h * 0.2f, 0f);
                t.rotation = Quaternion.Euler(0f, 0f, -28f);
                var sb = r.sprite.bounds.size;
                t.localScale = new Vector3(w * 0.5f / sb.x, h * 2.6f / sb.y, 1f);
                float breathe = 0.7f + 0.3f * Mathf.Sin(Time.time * 0.4f + i * 2.1f);
                r.color = new Color(1f, 0.86f, 0.55f, golden * 0.09f * breathe);
            }
        }

        // ---------------------------------------------------------------- on the ground and the water

        void Ground(Vector2 c, float w, float h, float dt, float dew, float day, float night)
        {
            // Dew glinting on the grass in the early morning.
            _dewDebt += dew * 9f * dt;
            for (; _dewDebt >= 1f; _dewDebt--)
            {
                var p = c + new Vector2(Random.Range(-w, w), Random.Range(-h, h));
                if (BunniSpace.PointOnLand(p)) Emit(_dew, p, Vector2.zero, new Color(1f, 1f, 1f, 0.85f), Random.Range(0.12f, 0.2f));
            }
            // Sun glints on the water by day; starlight twinkling on it at night.
            _glintDebt += (day * 14f + night * 9f) * dt;
            for (; _glintDebt >= 1f; _glintDebt--)
            {
                var p = c + new Vector2(Random.Range(-w, w), Random.Range(-h, h));
                if (BunniSpace.PointOnLand(p)) continue;
                var col = night > 0.5f ? new Color(0.8f, 0.88f, 1f, 0.8f) : new Color(1f, 1f, 0.92f, 0.9f);
                Emit(_glints, p, Vector2.zero, col, Random.Range(0.14f, 0.24f) * (night > 0.5f ? 0.8f : 1f));
            }
            // Foam along the beach; after dark the breaking waves glow a faint blue.
            _foamDebt += 7f * dt;
            for (; _foamDebt >= 1f; _foamDebt--)
            {
                var p = c + new Vector2(Random.Range(-w, w), Random.Range(-h, h));
                if (BunniSpace.PointOnLand(p) || !NearLand(p, 0.7f)) continue;
                var col = night > 0.5f ? new Color(0.45f, 0.85f, 1f, 0.75f) : new Color(1f, 1f, 1f, 0.55f);
                var v = new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(-0.05f, 0.05f));
                Emit(_foam, p, v, col, Random.Range(0.35f, 0.6f), Random.Range(-15f, 15f));
            }
        }

        static bool NearLand(Vector2 p, float r)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                if (BunniSpace.PointOnLand(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f) * r)) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- in the air

        void Air(Vector2 c, float w, float h, float dt, float golden, float snow, int dir)
        {
            // Pollen floating in the low gold light.
            _pollenDebt += golden * 7f * dt;
            for (; _pollenDebt >= 1f; _pollenDebt--)
            {
                var p = c + new Vector2(Random.Range(-w, w), Random.Range(-h, h));
                Emit(_pollen, p, new Vector2(dir * 0.15f, Random.Range(0.05f, 0.2f)), new Color(1f, 0.9f, 0.55f, 0.75f), Random.Range(0.06f, 0.1f));
            }
            // A soft snowfall, drifting with the wind.
            _snowDebt += snow * 22f * dt;
            for (; _snowDebt >= 1f; _snowDebt--)
            {
                var p = c + new Vector2(Random.Range(-w - 2f, w + 2f), Random.Range(-h, h + 2f));
                Emit(_snow, p, new Vector2(dir * Random.Range(0.2f, 0.5f), -Random.Range(0.6f, 1.1f)),
                     new Color(1f, 1f, 1f, 0.9f), Random.Range(0.08f, 0.16f));
            }
            // Petals lifting off the flower gardens by day.
            if (Time.time >= _nextPetal && _gardens.Count > 0 && Daylight(GameClock.Hour) > 0.3f)
            {
                _nextPetal = Time.time + Random.Range(0.6f, 1.4f);
                var g = _gardens[Random.Range(0, _gardens.Count)];
                if (g != null)
                {
                    string n = g.GetComponent<CatalogItem>()?.StoreName ?? "";
                    var col = n.Contains("Rose") ? new Color(0.92f, 0.3f, 0.35f) : n.Contains("Tulip") ? new Color(1f, 0.7f, 0.3f) : new Color(1f, 0.78f, 0.86f);
                    var at = (Vector2)g.position + new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(0.1f, 0.6f));
                    Emit(_petals, at, new Vector2(dir * Random.Range(0.3f, 0.7f), Random.Range(0.1f, 0.4f)), col, Random.Range(0.14f, 0.2f), Random.Range(0f, 360f));
                }
            }
        }

        // ---------------------------------------------------------------- homes and the capybaras in them

        void Life(float dt, float night, float day)
        {
            if (Time.time >= _nextSmoke && _chimneys.Count > 0)
            {
                _nextSmoke = Time.time + Mathf.Lerp(0.45f, 0.8f, night) / Mathf.Max(1, _chimneys.Count) * 3f;
                var b = _chimneys[Random.Range(0, _chimneys.Count)];
                if (b != null)
                {
                    float top = Top(b, out float width);
                    var at = new Vector2(b.position.x + width * 0.18f, top - 0.15f);
                    float rise = Mathf.Lerp(0.38f, 0.24f, night);
                    var wind = Wind.Instance;
                    var v = new Vector2((wind != null ? wind.Direction : 1) * 0.12f, rise);
                    var col = Color.Lerp(new Color(0.94f, 0.93f, 0.92f, 0.42f), new Color(1f, 0.86f, 0.7f, 0.38f), night);
                    Emit(_smoke, at, v, col, Random.Range(0.28f, 0.4f));
                }
            }
            if (Time.time >= _nextZ && _sleepers.Count > 0)
            {
                _nextZ = Time.time + 1.3f / _sleepers.Count;
                var s = _sleepers[Random.Range(0, _sleepers.Count)];
                if (s != null)
                {
                    float top = Top(s, out float width);
                    Emit(_zzz, new Vector2(s.position.x + width * 0.2f, top), new Vector2(0.25f, 0.45f), new Color(0.55f, 0.62f, 0.95f, 0.95f), 0.22f, -12f);
                }
            }
            if (Time.time >= _nextHeart && _capys.Count > 0)
            {
                _nextHeart = Time.time + Random.Range(5f, 10f);
                var k = _capys[Random.Range(0, _capys.Count)];
                if (k != null)
                {
                    float top = Top(k, out _);
                    Emit(_hearts, new Vector2(k.position.x, top + 0.1f), new Vector2(0f, 0.5f), new Color(1f, 0.45f, 0.58f, 0.95f), 0.26f);
                }
            }
        }

        // ---------------------------------------------------------------- shooting stars

        void Stars(Vector2 c, float w, float h, float night)
        {
            if (night < 0.9f || Time.time < _nextStar || _star.Flying) return;
            _nextStar = Time.time + Random.Range(35f, 80f);
            int side = Random.value < 0.5f ? -1 : 1;
            var from = c + new Vector2(side * w * Random.Range(0.3f, 0.8f), h * Random.Range(0.45f, 0.8f));
            var to = from + new Vector2(-side * w * Random.Range(0.9f, 1.3f), -h * Random.Range(0.25f, 0.45f));
            _star.Fly(from, to);
        }

        void OnWish(Vector2 at)
        {
            int gold = Random.Range(5, 16);
            var store = ResourceStore.Instance;
            if (store != null) store.Add(ResourceKind.Gold, gold);
            for (int i = 0; i < 18; i++)
                Emit(_bits, at, Random.insideUnitCircle * Random.Range(0.8f, 2.4f), new Color(1f, 0.95f, 0.6f, 1f), Random.Range(0.18f, 0.3f));
            Sfx.Play(SfxKind.Heart, 0.7f);
            Hints.Show($"A wish on a shooting star! +{gold} gold");
        }

        // ---------------------------------------------------------------- plumbing

        static void Emit(ParticleSystem ps, Vector2 at, Vector2 velocity, Color colour, float size, float rotation = 0f)
        {
            if (ps == null) return;
            ps.Emit(new ParticleSystem.EmitParams
            {
                position = new Vector3(at.x, at.y, -0.5f),
                velocity = velocity,
                startColor = colour,
                startSize = size,
                rotation = rotation,
                applyShapeToPosition = false,
            }, 1);
        }

        ParticleSystem MakeSystem(string name, Sprite sprite, float life, int order, Material material, float flutter, float spin = 0f, float grow = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.75f, life);
            main.maxParticles = 300;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            if (spin > 0f)
            {
                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-spin * Mathf.Deg2Rad, spin * Mathf.Deg2Rad);
            }
            if (flutter > 0f)
            {
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = flutter;
                noise.frequency = 0.4f;
                noise.scrollSpeed = 0.25f;
                noise.damping = true;
                noise.quality = ParticleSystemNoiseQuality.Low;
            }
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colour.color = g;
            if (!Mathf.Approximately(grow, 1f))
            {
                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, grow));
            }
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = order;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = material;
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

        SpriteRenderer Sheet(string name, Sprite sprite, int order, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.sharedMaterial = material;
            sr.enabled = false;
            return sr;
        }

        // ---------------------------------------------------------------- pictures, drawn in code

        static Sprite Paint(int w, int h, System.Func<float, float, float> alpha, Vector2? pivot = null)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha((x + 0.5f) / w, (y + 0.5f) / h)) * 255f));
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot ?? new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>A soft round glow with a bright heart.</summary>
        static Sprite Glow() => Paint(32, 32, (u, v) =>
        {
            float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
            float a = Mathf.Clamp01(1f - d);
            return a * a * 1.3f + (d < 0.22f ? 0.5f : 0f);
        });

        /// <summary>A four-pointed twinkle.</summary>
        static Sprite Sparkle() => Paint(32, 32, (u, v) =>
        {
            float x = Mathf.Abs(u - 0.5f) * 2f, y = Mathf.Abs(v - 0.5f) * 2f;
            float star = Mathf.Clamp01(1f - (x * y * 14f + Mathf.Max(x, y) * 0.9f));
            float core = Mathf.Clamp01(1f - new Vector2(x, y).magnitude * 3f);
            return Mathf.Max(star, core);
        });

        /// <summary>A big soft cloud of nothing much: mist.</summary>
        static Sprite SoftBlob() => Paint(128, 64, (u, v) =>
        {
            float dx = (u - 0.5f) * 2f, dy = (v - 0.5f) * 2f;
            float d = dx * dx + dy * dy * 1.1f;
            float lumps = 0.12f * Mathf.Sin(u * 17f) * Mathf.Sin(v * 9f + u * 5f);
            return Mathf.Clamp01(1f - d + lumps) * 0.9f;
        });

        /// <summary>A soft white dash: foam, or a star's trail.</summary>
        static Sprite Dash() => Paint(96, 16, (u, v) =>
        {
            float thick = Mathf.Sin(Mathf.PI * u) * 0.9f + 0.05f;
            float a = Mathf.Clamp01(1f - Mathf.Abs(v * 2f - 1f) / thick) * Mathf.Sin(Mathf.PI * u);
            return a * a;
        });

        /// <summary>A long beam of light, soft at the sides and fading toward the ground.</summary>
        static Sprite RaySprite() => Paint(32, 256, (u, v) =>
        {
            float side = Mathf.Sin(Mathf.PI * u);
            return side * side * Mathf.Lerp(0.15f, 1f, v);
        });

        /// <summary>A rounded, hand-drawn-looking "z".</summary>
        static Sprite ZSprite() => Paint(32, 32, (u, v) =>
        {
            float t = 0.13f;
            bool top = v > 0.72f && v < 0.72f + t * 1.4f && u > 0.18f && u < 0.82f;
            bool bottom = v < 0.28f && v > 0.28f - t * 1.4f && u > 0.18f && u < 0.82f;
            float lineV = 0.72f - (u - 0.18f) * (0.44f / 0.64f);
            bool slash = u > 0.18f && u < 0.82f && Mathf.Abs(v - lineV) < t * 0.8f;
            return top || bottom || slash ? 1f : 0f;
        });

        // ---------------------------------------------------------------- fireflies

        class Firefly
        {
            readonly SpriteRenderer _sr;
            readonly Light2D _light;
            Vector2 _at, _vel, _home;
            float _phase, _period, _alive;

            public Firefly(Transform parent, Sprite glow, Material material, bool withLight)
            {
                var go = new GameObject("Firefly");
                go.transform.SetParent(parent, false);
                _sr = go.AddComponent<SpriteRenderer>();
                _sr.sprite = glow;
                _sr.sharedMaterial = material;
                _sr.sortingOrder = AirOrder + 2;
                _sr.enabled = false;
                if (withLight)
                {
                    _light = go.AddComponent<Light2D>();
                    _light.lightType = Light2D.LightType.Point;
                    _light.color = new Color(0.8f, 1f, 0.45f);
                    _light.pointLightOuterRadius = 1.6f;
                    _light.pointLightInnerRadius = 0.1f;
                    _light.intensity = 0f;
                    _light.falloffIntensity = 0.7f;
                }
                _phase = Random.value * 10f;
                _period = Random.Range(2.2f, 4.2f);
            }

            public void Tick(Vector2 c, float w, float h, Vector2 you, float night, List<Transform> nature, float dt)
            {
                if (night < 0.02f)
                {
                    if (_sr.enabled) { _sr.enabled = false; if (_light != null) _light.intensity = 0f; }
                    _alive = 0f;
                    return;
                }
                // Out of sight (or just woken): settle somewhere in view, preferably by trees and flowers.
                if (!_sr.enabled || Mathf.Abs(_at.x - c.x) > w + 1.5f || Mathf.Abs(_at.y - c.y) > h + 1.5f)
                {
                    Transform near = null;
                    for (int tries = 0; tries < 4 && nature.Count > 0 && near == null; tries++)
                    {
                        var t = nature[Random.Range(0, nature.Count)];
                        if (t != null && Mathf.Abs(t.position.x - c.x) < w && Mathf.Abs(t.position.y - c.y) < h) near = t;
                    }
                    _home = near != null && Random.value < 0.7f
                        ? (Vector2)near.position + new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(0.2f, 1.6f))
                        : c + new Vector2(Random.Range(-w, w) * 0.9f, Random.Range(-h, h) * 0.85f);
                    _at = _home + Random.insideUnitCircle * 0.5f;
                    _vel = Vector2.zero;
                    _alive = 0f;
                    _sr.enabled = true;
                }
                _alive += dt;

                // A lazy wander around its spot…
                _vel += (Random.insideUnitCircle * 1.2f + (_home - _at) * 0.35f) * dt;
                // …and away from a capybara walking through.
                var away = _at - you;
                if (away.sqrMagnitude < 1.8f * 1.8f) _vel += away.normalized * 4f * dt;
                _vel = Vector2.ClampMagnitude(_vel * (1f - 0.6f * dt), 1.2f);
                _at += _vel * dt;
                _sr.transform.position = new Vector3(_at.x, _at.y, -0.5f);

                // Blink: slowly on, glow a moment, slowly off, rest in the dark.
                float t01 = Mathf.Repeat((Time.time + _phase) / _period, 1f);
                float blink = t01 < 0.55f ? Mathf.Sin(t01 / 0.55f * Mathf.PI) : 0f;
                float a = night * Mathf.Clamp01(_alive / 1.5f) * (0.25f + 0.75f * blink);
                _sr.color = new Color(0.86f, 1f, 0.5f, a);
                _sr.transform.localScale = Vector3.one * (0.22f + 0.08f * blink);
                if (_light != null) _light.intensity = 0.55f * a;
            }
        }

        // ---------------------------------------------------------------- butterflies

        class Butterfly
        {
            readonly SpriteRenderer _sr;
            Vector2 _at, _target;
            Transform _garden;
            float _flap, _retarget, _alpha;

            static readonly Color[] Colours = { new(1f, 0.95f, 0.75f), new(0.85f, 0.92f, 1f), new(1f, 0.82f, 0.9f), new(0.9f, 1f, 0.82f) };

            public Butterfly(Transform parent, Material material, int i)
            {
                var go = new GameObject("Butterfly");
                go.transform.SetParent(parent, false);
                _sr = go.AddComponent<SpriteRenderer>();
                _sr.sprite = Wings();
                _sr.sharedMaterial = material;
                _sr.sortingOrder = AirOrder + 1;
                _sr.color = Colours[i % Colours.Length];
                _sr.enabled = false;
                _flap = Random.value * 6f;
            }

            public void Tick(float day, List<Transform> gardens, float dt)
            {
                bool want = day > 0.3f && gardens.Count > 0;
                _alpha = Mathf.MoveTowards(_alpha, want ? 1f : 0f, dt * 0.8f);
                if (_alpha <= 0f) { _sr.enabled = false; _garden = null; return; }
                if (_garden == null && gardens.Count > 0)
                {
                    _garden = gardens[Random.Range(0, gardens.Count)];
                    if (_garden == null) return;
                    _at = (Vector2)_garden.position + new Vector2(Random.Range(-1f, 1f), Random.Range(0.3f, 1.2f));
                    _target = _at;
                }
                if (_garden == null) return;
                _sr.enabled = true;
                _retarget -= dt;
                if (_retarget <= 0f || Vector2.Distance(_at, _target) < 0.1f)
                {
                    _retarget = Random.Range(0.8f, 2f);
                    _target = (Vector2)_garden.position + new Vector2(Random.Range(-1.3f, 1.3f), Random.Range(0.2f, 1.5f));
                }
                _at = Vector2.MoveTowards(_at, _target, 0.9f * dt);
                _flap += dt * 14f;
                var t = _sr.transform;
                t.position = new Vector3(_at.x, _at.y + Mathf.Sin(_flap * 0.25f) * 0.08f, -0.5f);
                float open = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(_flap));
                t.localScale = new Vector3(0.22f * open * (_target.x < _at.x ? -1f : 1f), 0.22f, 1f);
                var col = _sr.color; col.a = _alpha; _sr.color = col;
            }

            static Sprite _wings;
            static Sprite Wings() => _wings != null ? _wings : _wings = Paint(48, 40, (u, v) =>
            {
                float x = u - 0.5f, y = v - 0.5f;
                float upper = new Vector2((Mathf.Abs(x) - 0.22f) / 0.24f, (y - 0.1f) / 0.3f).sqrMagnitude;
                float lower = new Vector2((Mathf.Abs(x) - 0.16f) / 0.16f, (y + 0.22f) / 0.2f).sqrMagnitude;
                float body = new Vector2(x / 0.045f, y / 0.4f).sqrMagnitude;
                return (upper < 1f || lower < 1f) ? 0.95f : body < 1f ? 1f : 0f;
            });
        }

        // ---------------------------------------------------------------- the shooting star itself

        class ShootingStar : MonoBehaviour, ITappable
        {
            SpriteRenderer _head, _trail;
            Vector2 _from, _to;
            float _t = 1f;
            bool _wished;
            System.Action<Vector2> _onWish;
            const float Seconds = 1.6f;

            public bool Flying => _t < 1f;

            public void Init(Sprite glow, Sprite dash, Material material, System.Action<Vector2> onWish)
            {
                _onWish = onWish;
                _head = Part("Head", glow, material, AirOrder + 8);
                _trail = Part("Trail", dash, material, AirOrder + 7);
            }

            SpriteRenderer Part(string name, Sprite sprite, Material material, int order)
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sharedMaterial = material;
                sr.sortingOrder = order;
                sr.enabled = false;
                return sr;
            }

            public void Fly(Vector2 from, Vector2 to)
            {
                _from = from; _to = to; _t = 0f; _wished = false;
                _head.enabled = _trail.enabled = true;
                TapTargets.Register(this);
            }

            void Update()
            {
                if (_t >= 1f) return;
                _t += Time.deltaTime / Seconds;
                float k = Mathf.SmoothStep(0f, 1f, _t);
                var at = Vector2.Lerp(_from, _to, k);
                var dir = (_to - _from).normalized;
                float a = Mathf.Sin(Mathf.Clamp01(_t) * Mathf.PI);
                _head.transform.position = new Vector3(at.x, at.y, -0.6f);
                _head.transform.localScale = Vector3.one * 0.5f;
                _head.color = new Color(1f, 0.97f, 0.85f, a);
                var mid = at - dir * 1.2f;
                _trail.transform.position = new Vector3(mid.x, mid.y, -0.6f);
                _trail.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                _trail.transform.localScale = new Vector3(2.6f / Mathf.Max(0.01f, _trail.sprite.bounds.size.x), 0.16f / Mathf.Max(0.01f, _trail.sprite.bounds.size.y), 1f);
                _trail.color = new Color(1f, 0.95f, 0.8f, a * 0.8f);
                if (_t >= 1f) { _head.enabled = _trail.enabled = false; TapTargets.Unregister(this); }
            }

            void OnDisable() => TapTargets.Unregister(this);

            public float TapDistance(Vector2 worldPoint)
            {
                if (_t >= 1f || _wished) return -1f;
                float d = Vector2.Distance(_head.transform.position, worldPoint);
                return d < 1.6f ? d : -1f;
            }

            public void Tap()
            {
                if (_wished) return;
                _wished = true;
                _onWish?.Invoke(_head.transform.position);
            }
        }
    }
}
