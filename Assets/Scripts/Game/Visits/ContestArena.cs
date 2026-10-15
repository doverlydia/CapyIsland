using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Where a contest is happening, marked on the island: a ring of softly glowing lights around
    /// its edge, and glitter rising from everything inside — the place is flourishing, and so is
    /// the hunting (fish, bugs and birds turn up there far more often while a round runs). Brighter
    /// and busier during a round, gentle while everyone gathers.
    ///
    /// Made by Gathering while a contest is on (one per island scene); it goes when the contest does.
    /// </summary>
    public class ContestArena : MonoBehaviour
    {
        const int Lights = 44;
        // On the ground: under everything standing on the island (YSorter's props start at its
        // base), so a tree or a capybara in front of the ring covers it.
        const int GroundOrder = YSorter.PropSortBase - 2;

        static ContestArena _instance;

        Vector2 _centre;
        float _radius;          // world units
        bool _running;
        Transform[] _lights;
        SpriteRenderer[] _glows;
        ParticleSystem _glitter;
        float _emit;

        /// <summary>Puts the ring where it belongs (making it if needed) and sets how lively it is.</summary>
        public static void Ensure(Vector2 centre, float worldRadius, GatheringKind kind, bool running)
        {
            if (_instance == null)
            {
                var go = new GameObject("[ContestArena]");
                _instance = go.AddComponent<ContestArena>();
                _instance.Build(kind);
            }
            if (_instance._centre != centre || !Mathf.Approximately(_instance._radius, worldRadius))
            {
                _instance._centre = centre;
                _instance._radius = worldRadius;
                _instance.Lay();
            }
            _instance._running = running;
        }

        /// <summary>The contest is over: the ring fades away.</summary>
        public static void Clear()
        {
            if (_instance == null) return;
            Destroy(_instance.gameObject);
            _instance = null;
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        Vector2 Radii => BunniSpace.ToUnity(new Vector2(_radius, _radius));

        void Build(GatheringKind kind)
        {
            // The lights: warm for fish and bugs, a little cooler for birds.
            var tint = kind == GatheringKind.Birds ? new Color(1f, 0.93f, 0.7f) : new Color(1f, 0.86f, 0.45f);
            _lights = new Transform[Lights];
            _glows = new SpriteRenderer[Lights];
            for (int i = 0; i < Lights; i++)
            {
                var light = new GameObject("Light");
                light.transform.SetParent(transform, false);
                var glow = light.AddComponent<SpriteRenderer>();
                glow.sprite = UI.UiKit.Circle;
                glow.color = new Color(tint.r, tint.g, tint.b, 0.35f);
                glow.sortingOrder = GroundOrder;
                light.transform.localScale = Vector3.one * 0.32f / SpriteWidth(glow.sprite);
                var core = new GameObject("Core");
                core.transform.SetParent(light.transform, false);
                var dot = core.AddComponent<SpriteRenderer>();
                dot.sprite = UI.UiKit.Circle;
                dot.color = new Color(1f, 0.98f, 0.9f, 0.95f);
                dot.sortingOrder = GroundOrder + 1;
                core.transform.localScale = Vector3.one * 0.38f;
                _lights[i] = light.transform;
                _glows[i] = glow;
            }

            _glitter = MakeGlitter(tint);
        }

        static float SpriteWidth(Sprite s) => s != null ? Mathf.Max(0.01f, s.bounds.size.x) : 1f;

        /// <summary>The lights round the edge (an ellipse on screen: the island is seen at a slant).</summary>
        void Lay()
        {
            var r = Radii;
            transform.position = new Vector3(_centre.x, _centre.y, 0f);
            for (int i = 0; i < Lights; i++)
            {
                float a = i * Mathf.PI * 2f / Lights;
                _lights[i].localPosition = new Vector3(Mathf.Cos(a) * r.x, Mathf.Sin(a) * r.y, 0f);
            }
            if (_glitter != null)
            {
                var shape = _glitter.shape;
                shape.scale = new Vector3(r.x * 2f, r.y * 2f, 1f);
            }
        }

        ParticleSystem MakeGlitter(Color tint)
        {
            var go = new GameObject("Glitter");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 2.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.24f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -0.04f;   // drifting up
            main.maxParticles = 300;
            var colours = new Gradient();
            colours.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(tint, 0.5f), new GradientColorKey(new Color(1f, 0.8f, 0.9f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(colours) { mode = ParticleSystemGradientMode.RandomColor };

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            // Anywhere inside the ring: a flat disc squashed to the island's slant.
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
            shape.rotation = Vector3.zero;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            // Twinkle in and out.
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
            fade.color = g;

            var spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = GroundOrder;
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

        void Update()
        {
            // The lights breathe, each a beat after the last, so the ring shimmers round.
            float t = Time.time;
            float strength = _running ? 1f : 0.65f;
            for (int i = 0; i < Lights; i++)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(t * 2.2f - i * 0.45f);
                var c = _glows[i].color;
                c.a = (0.18f + 0.3f * k) * strength;
                _glows[i].color = c;
                _lights[i].localPosition = new Vector3(_lights[i].localPosition.x, _lights[i].localPosition.y, 0f);
            }
            // Busier while a round runs.
            float want = _running ? 24f : 8f;
            _emit = Mathf.MoveTowards(_emit, want, Time.deltaTime * 10f);
            if (_glitter != null)
            {
                var emission = _glitter.emission;
                emission.rateOverTime = _emit;
            }
        }
    }
}
