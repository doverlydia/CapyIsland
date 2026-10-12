using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Grass clippings under the player's feet: every time a hop lands, a few tiny cut bits of grass
    /// flick up and tumble back down behind. Nothing on a bridge or over water.
    ///
    /// Pure presentation. One small hand-emitted particle system, drawn just behind the player and
    /// lit by the island's light so it dims at night like everything else.
    /// </summary>
    [DefaultExecutionOrder(120)]   // after YSorter, so we copy this frame's body order
    public class Footsteps : MonoBehaviour
    {
        static readonly Color[] Grass =
        {
            new(0.47f, 0.71f, 0.30f), new(0.56f, 0.79f, 0.34f), new(0.38f, 0.61f, 0.25f), new(0.66f, 0.83f, 0.40f),
        };

        PlayerAvatar _avatar;
        SpriteRenderer _body;
        ParticleSystem _blades;
        ParticleSystemRenderer _bladeRenderer;

        void Awake()
        {
            _avatar = GetComponent<PlayerAvatar>();
            var sprite = transform.Find("Sprite");
            _body = sprite != null ? sprite.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();

            _blades = Make("FootGrass", BladeSprite(), gravity: 0.7f, drag: 0.6f, spin: 520f, grow: 0.8f);
            _bladeRenderer = _blades.GetComponent<ParticleSystemRenderer>();
        }

        void OnEnable() { if (_avatar != null) _avatar.Landed += OnLanded; }
        void OnDisable() { if (_avatar != null) _avatar.Landed -= OnLanded; }

        void OnDestroy()
        {
            if (_blades != null) Destroy(_blades.gameObject);
        }

        void LateUpdate()
        {
            // Just behind the player, wherever the player sorts this frame.
            if (_body == null) return;
            _bladeRenderer.sortingOrder = _body.sortingOrder - 1;
        }

        void OnLanded()
        {
            Vector2 feet = transform.position;
            Vector2 v = _avatar.Velocity;
            float speed = v.magnitude;
            if (speed < 0.5f) return;
            Vector2 dir = v / speed;
            Vector2 side = new(-dir.y, dir.x);
            float pace = _avatar.SpeedFraction;

            var land = LandMap.Instance;
            bool onLand = land == null || land.IsOnLand(feet);
            if (!onLand) return;
            if (land != null && land.IslandAt(feet) < 0) return;   // a bridge: no grass

            // A few clippings most steps, kicked back and up from behind the heel.
            int bits = Random.value < 0.35f + 0.5f * pace ? Random.Range(2, 5) : 0;
            for (int i = 0; i < bits; i++)
            {
                var at = feet - dir * 0.15f + side * Random.Range(-0.3f, 0.3f);
                var vel = -dir * Random.Range(0.4f, 1.0f) + side * Random.Range(-0.6f, 0.6f) + Vector2.up * Random.Range(1.4f, 2.3f);
                var colour = Grass[Random.Range(0, Grass.Length)];
                if (Weather.IsRaining) colour *= 0.85f;   // wet grass is darker
                colour.a = 1f;
                Emit(_blades, at, vel, colour, Random.Range(0.14f, 0.22f), Random.Range(0.5f, 0.7f));
            }
        }

        static void Emit(ParticleSystem system, Vector2 at, Vector2 velocity, Color colour, float size, float life)
        {
            system.Emit(new ParticleSystem.EmitParams
            {
                position = new Vector3(at.x, at.y, -0.4f),
                velocity = velocity,
                startColor = colour,
                startSize = size,
                startLifetime = life,
                rotation = Random.Range(0f, 360f),
                applyShapeToPosition = false,
            }, 1);
        }

        ParticleSystem Make(string name, Sprite sprite, float gravity, float drag, float spin, float grow)
        {
            // Not parented to the player: puffs stay where they were kicked up, and don't hop along.
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.maxParticles = 80;

            var emission = ps.emission;
            emission.enabled = false;   // emitted by hand, on each landing
            var shape = ps.shape;
            shape.enabled = false;

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = drag;
            limit.dampen = 0f;

            var rot = ps.rotationOverLifetime;
            rot.enabled = spin > 0f;
            rot.z = new ParticleSystem.MinMaxCurve(-spin * Mathf.Deg2Rad, spin * Mathf.Deg2Rad);

            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.8f, 0.55f), new GradientAlphaKey(0f, 1f) });
            colour.color = g;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, grow));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = LitMaterial();

            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.SetSprite(0, sprite);

            ps.Play();
            return ps;
        }

        static Material _lit;

        /// <summary>A sprite material that takes the island's light, so the dust dims at night.</summary>
        static Material LitMaterial()
        {
            if (_lit != null) return _lit;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader == null) return HarvestEffects.SpriteMaterial();
            _lit = new Material(shader) { name = "FootstepParticles" };
            return _lit;
        }

        static Sprite _blade;

        /// <summary>One tiny clipping of grass — a short piece with cut ends — white so it can be tinted.</summary>
        static Sprite BladeSprite()
        {
            if (_blade != null) return _blade;
            const int w = 16, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (h - 1f);                          // 0 at one cut end, 1 at the other
                float centre = w * 0.5f + (t - 0.5f) * (t - 0.5f) * 8f;   // a slight curve
                float half = Mathf.Lerp(3.2f, 2.0f, t);          // barely tapering: snipped, not grown
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Abs(x + 0.5f - centre) - half;
                    float a = Mathf.Clamp01(0.5f - d);            // one pixel of soft edge
                    if (t < 0.04f || t > 0.96f) a *= 0.6f;     // soft cut ends
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _blade = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            _blade.name = "GrassClipping";
            return _blade;
        }
    }
}
