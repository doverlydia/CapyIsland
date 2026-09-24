using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Particle bursts for the moments that matter: chips and leaves when a tree is struck, petals
    /// when a flower is nibbled, grit when rock is hit, a bigger puff when anything falls, and the
    /// juicy splash-and-bounce of fruit shaken off a tree.
    ///
    /// Pure presentation — the original had none of this, and none of it touches the rules. All
    /// systems are built in code from a handful of small sprites, so there are no prefabs to keep
    /// in sync, and everything is pooled in a few shared ParticleSystems for WebGL's sake.
    /// </summary>
    public class HarvestEffects : MonoBehaviour
    {
        public static HarvestEffects Instance { get; private set; }

        [Header("Particle sprites")]
        [SerializeField] Sprite leaf;
        [SerializeField] Sprite chip;
        [SerializeField] Sprite petal;
        [SerializeField] Sprite grit;
        [SerializeField] Sprite sparkle;
        [SerializeField] Sprite juice;

        [Header("Look")]
        [SerializeField] Color leafColour = new Color(0.40f, 0.66f, 0.28f);
        [SerializeField] Color chipColour = new Color(0.78f, 0.58f, 0.34f);
        [SerializeField] Color petalColour = new Color(1.00f, 0.72f, 0.78f);
        [SerializeField] Color gritColour = new Color(0.62f, 0.62f, 0.60f);

        ParticleSystem _leaves, _blown, _chips, _petals, _grit, _sparkles, _juice;

        const int SortingOrder = 30000;   // above every prop

        void Awake()
        {
            Instance = this;
            _leaves = MakeSystem("Leaves", leaf, gravity: 0.25f, drag: 1.4f, life: 1.6f, spin: 260f);
            AddFlutter(_leaves, 0.6f);
            // Leaves carried off by the wind: barely any gravity, long-lived, fluttering as they go.
            _blown = MakeSystem("BlownLeaves", leaf, gravity: 0.05f, drag: 0.15f, life: 3.2f, spin: 220f);
            AddFlutter(_blown, 1.1f);
            _chips = MakeSystem("Chips", chip, gravity: 2.2f, drag: 0.4f, life: 0.8f, spin: 540f);
            _petals = MakeSystem("Petals", petal, gravity: 0.2f, drag: 1.6f, life: 1.4f, spin: 200f);
            _grit = MakeSystem("Grit", grit, gravity: 2.6f, drag: 0.5f, life: 0.6f, spin: 360f);
            _sparkles = MakeSystem("Sparkles", sparkle, gravity: -0.3f, drag: 1.2f, life: 0.9f, spin: 90f);
            _juice = MakeSystem("Juice", juice, gravity: 3.2f, drag: 0.3f, life: 0.55f, spin: 0f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>One hit landing on a node.</summary>
        public void Hit(Harvestable node)
        {
            if (node == null) return;
            var at = TopOf(node);
            switch (node.Family)
            {
                case NodeFamily.Tree:
                    Burst(_chips, at + Vector2.down * 0.4f, 4, chipColour, 0.12f, 1.8f, 3.6f);
                    Burst(_leaves, at, 3, leafColour, 0.16f, 0.8f, 1.8f);
                    break;
                case NodeFamily.Flower:
                    Burst(_petals, at, 3, petalColour, 0.12f, 0.6f, 1.4f);
                    break;
                case NodeFamily.Rock:
                    Burst(_grit, at + Vector2.down * 0.3f, 5, gritColour, 0.1f, 1.8f, 3.2f);
                    break;
            }
        }

        /// <summary>An axe blow on a fruit tree: chips from the trunk, leaves from the canopy.</summary>
        public void HitTree(FruitTree tree)
        {
            if (tree == null) return;
            var sprite = tree.transform.Find("Sprite");
            var sr = sprite != null ? sprite.GetComponent<SpriteRenderer>() : null;
            Vector2 at = tree.transform.position;
            if (sr != null)
            {
                var b = sr.bounds;
                Burst(_chips, new Vector2(b.center.x, b.min.y + b.size.y * 0.2f), 4, chipColour, 0.12f, 1.8f, 3.6f);
                Burst(_leaves, new Vector2(b.center.x, b.min.y + b.size.y * 0.7f), 4, leafColour, 0.16f, 0.8f, 1.8f);
                return;
            }
            Burst(_chips, at, 4, chipColour, 0.12f, 1.8f, 3.6f);
        }

        /// <summary>A node falling: a bigger shower, and a little sparkle for the payout.</summary>
        public void Felled(Vector2 at, NodeFamily family)
        {
            switch (family)
            {
                case NodeFamily.Tree:
                    Burst(_leaves, at + Vector2.up * 1.2f, 14, leafColour, 0.18f, 1.2f, 3.0f);
                    Burst(_chips, at, 8, chipColour, 0.14f, 2.0f, 4.0f);
                    break;
                case NodeFamily.Flower:
                    Burst(_petals, at + Vector2.up * 0.4f, 10, petalColour, 0.14f, 0.9f, 2.0f);
                    break;
                case NodeFamily.Rock:
                    Burst(_grit, at + Vector2.up * 0.5f, 14, gritColour, 0.12f, 2.2f, 4.2f);
                    break;
            }
            Burst(_sparkles, at + Vector2.up * 0.8f, 6, Color.white, 0.14f, 0.6f, 1.4f);
        }

        /// <summary>Fruit shaken loose: a splash of juice in the fruit's colour and a few falling leaves.</summary>
        public void FruitPop(Vector2 at, Color fruitColour)
        {
            Burst(_juice, at, 7, fruitColour, 0.1f, 1.6f, 3.0f);
            Burst(_leaves, at + Vector2.up * 0.6f, 5, leafColour, 0.15f, 0.6f, 1.6f);
            Burst(_sparkles, at, 3, Color.white, 0.12f, 0.5f, 1.2f);
        }

        /// <summary>A whole tree rustling when shaken: leaves blown off the canopy, drifting sideways.</summary>
        public void Rustle(Vector2 canopy, float width)
        {
            for (int i = 0; i < 8; i++)
            {
                var p = canopy + new Vector2(Random.Range(-width, width) * 0.5f, Random.Range(-0.4f, 0.6f));
                Emit(_leaves, p, new Vector2(Random.Range(0.6f, 2.2f), Random.Range(0.2f, 1.2f)), leafColour, Random.Range(0.12f, 0.2f));
            }
        }

        /// <summary>One leaf torn off a canopy by a gust, drifting away downwind.</summary>
        public void BlowLeaf(Vector2 at, int windDirection, Color colour)
        {
            var v = new Vector2(windDirection * Random.Range(1.4f, 2.8f), Random.Range(-0.2f, 0.7f));
            Emit(_blown, at, v, colour * Random.Range(0.85f, 1.1f), Random.Range(0.13f, 0.2f));
        }

        /// <summary>A small puff for fruit or a bubble that has gone off: leaves, no sparkle.</summary>
        public void Poof(Vector2 at, Color colour)
        {
            Burst(_petals, at, 5, colour, 0.1f, 0.4f, 1.0f);
        }

        /// <summary>Sparkles for a reward: a chest opening, a sapling found in the branches.</summary>
        public void Twinkle(Vector2 at, int count = 8)
        {
            Burst(_sparkles, at, count, Color.white, 0.16f, 0.8f, 2.0f);
        }

        /// <summary>The on-screen top of a node, where debris should come from.</summary>
        static Vector2 TopOf(Harvestable node)
        {
            var sprite = node.transform.Find("Sprite");
            var sr = sprite != null ? sprite.GetComponent<SpriteRenderer>() : null;
            if (sr == null) return node.transform.position;
            var b = sr.bounds;
            return new Vector2(b.center.x, b.center.y + b.extents.y * 0.35f);
        }

        // ---------------------------------------------------------------- plumbing

        void Burst(ParticleSystem system, Vector2 at, int count, Color colour, float size, float minSpeed, float maxSpeed)
        {
            if (system == null) return;
            for (int i = 0; i < count; i++)
            {
                // Mostly upward, so debris arcs and falls rather than shooting flat.
                float a = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                var v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(minSpeed, maxSpeed);
                Emit(system, at, v, colour, size * Random.Range(0.75f, 1.3f));
            }
        }

        static void Emit(ParticleSystem system, Vector2 at, Vector2 velocity, Color colour, float size)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = new Vector3(at.x, at.y, -0.5f),
                velocity = velocity,
                startColor = colour,
                startSize = size,
                rotation = Random.Range(0f, 360f),
                applyShapeToPosition = false,
            };
            system.Emit(p, 1);
        }

        ParticleSystem MakeSystem(string name, Sprite sprite, float gravity, float drag, float life, float spin)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life);
            main.gravityModifier = gravity;
            main.maxParticles = 400;
            main.startRotation3D = false;

            var emission = ps.emission;
            emission.enabled = false;   // everything is emitted by hand

            var shape = ps.shape;
            shape.enabled = false;

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = drag;
            limit.dampen = 0f;

            var rot = ps.rotationOverLifetime;
            rot.enabled = spin > 0f;
            rot.z = new ParticleSystem.MinMaxCurve(-spin * Mathf.Deg2Rad, spin * Mathf.Deg2Rad);

            // Fade out over the last third, and shrink a touch, so nothing pops off screen.
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.65f), new GradientAlphaKey(0f, 1f) });
            colour.color = g;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.6f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = SortingOrder;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = SpriteMaterial();

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

        /// <summary>Wobble on top of the motion, so leaves flutter instead of flying in straight arcs.</summary>
        static void AddFlutter(ParticleSystem ps, float strength)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = strength;
            noise.frequency = 0.6f;
            noise.scrollSpeed = 0.4f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Low;   // WebGL
        }

        static Material _material;

        /// <summary>One shared unlit sprite material for every particle system.</summary>
        static Material SpriteMaterial()
        {
            if (_material != null) return _material;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                         ?? Shader.Find("Sprites/Default");
            _material = new Material(shader) { name = "HarvestParticles" };
            return _material;
        }
    }
}
