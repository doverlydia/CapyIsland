using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Wind you can see: a few soft, thin wisps that draw themselves through the air, curl into a
    /// little swirl or loop-the-loop, and wind away into nothing — the way a picture book draws a
    /// breeze. Now and then one carries a leaf or a petal along and flings it off the end.
    ///
    /// They ride <see cref="Wind"/>'s gusts: while a gust front crosses the view, wisps start where
    /// it is pushing hardest (so they travel across the screen with the trees bending under it);
    /// in calm air only the odd lazy one. Never more than <see cref="Pool"/> at once.
    ///
    /// Each wisp is one <see cref="LineRenderer"/> with a tapered width and a texture that is soft
    /// across the stroke, so there are no hard edges or square ends. Owned and ticked by
    /// <see cref="Ambience"/>; pure presentation, nothing reads it.
    /// </summary>
    public sealed class WindWisps
    {
        const int Pool = 5;
        const int Steps = 72;     // segments in a wisp's whole path
        const int Points = 40;    // points drawn along the part that is showing

        readonly Wisp[] _wisps = new Wisp[Pool];
        float _nextTry, _nextCalm;

        public WindWisps(Transform parent, int sortingOrder)
        {
            var material = new Material(HarvestEffects.SpriteMaterial()) { name = "WindWisps" };
            material.mainTexture = SoftEdge();
            for (int i = 0; i < Pool; i++) _wisps[i] = new Wisp(parent, material, sortingOrder);
            _nextCalm = Time.time + Random.Range(3f, 6f);
        }

        /// <summary>Start new wisps on the gusts and move the ones in the air. Call while the game runs.</summary>
        public void Tick(Camera cam)
        {
            float h = cam.orthographicSize, w = h * cam.aspect;
            Vector2 c = cam.transform.position;
            float dt = Time.deltaTime, now = Time.time;
            var wind = Wind.Instance;
            int dir = wind != null ? wind.Direction : 1;

            if (now >= _nextTry)
            {
                _nextTry = now + Random.Range(0.3f, 0.55f);
                // Where in view is the gust pushing hardest? (A gust is a front sweeping along X.)
                float best = 0f, bestX = c.x;
                if (wind != null)
                    for (int i = 0; i < 4; i++)
                    {
                        float x = c.x + Random.Range(-w, w);
                        float g = wind.GustAt(new Vector2(x, c.y));
                        if (g > best) { best = g; bestX = x; }
                    }
                // Wisps keep about the same size on screen in portrait, landscape and when zoomed.
                float scale = Mathf.Clamp(Mathf.Sqrt(h * w) / 8f, 0.8f, 1.5f);
                if (best > 0.2f)
                {
                    Spawn(new Vector2(bestX, c.y + Random.Range(-h, h) * 0.8f), dir, best, scale);
                    _nextCalm = Mathf.Max(_nextCalm, now + Random.Range(3f, 6f));
                }
                else if (now >= _nextCalm)
                {
                    _nextCalm = now + Random.Range(4f, 8f);
                    Spawn(c + new Vector2(Random.Range(-w, w) * 0.7f, Random.Range(-h, h) * 0.7f), dir, 0f, scale);
                }
            }

            for (int i = 0; i < _wisps.Length; i++) _wisps[i].Tick(dt);
        }

        void Spawn(Vector2 at, int dir, float gust, float scale)
        {
            for (int i = 0; i < _wisps.Length; i++)
                if (!_wisps[i].Busy) { _wisps[i].Launch(at, dir, gust, scale); return; }
        }

        /// <summary>White, opaque down the middle of the stroke and fading softly to both edges.</summary>
        static Texture2D SoftEdge()
        {
            const int w = 4, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "WindWispEdge",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float a = 1f - Mathf.Abs((y + 0.5f) / h * 2f - 1f);
                a = a * a * (3f - 2f * a);
                for (int x = 0; x < w; x++) px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        // ---------------------------------------------------------------- one wisp

        sealed class Wisp
        {
            enum Kind { Curl, Loop, Wave }

            // Thin at the trailing end, fullest just behind the head, a little rounded at the head.
            static readonly AnimationCurve Taper = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.4f, 0.8f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0.3f));

            static readonly Color[] LeafColours =
            {
                new(0.42f, 0.68f, 0.30f), new(0.50f, 0.72f, 0.32f), new(0.90f, 0.74f, 0.32f),
            };
            static readonly Color[] PetalColours = { new(0.98f, 0.78f, 0.85f), new(1f, 0.95f, 0.96f) };

            readonly LineRenderer _line;
            readonly SpriteRenderer _leaf;
            readonly Vector2[] _path = new Vector2[Steps + 1];
            readonly Vector3[] _points = new Vector3[Points];
            readonly Gradient _gradient = new Gradient();
            readonly GradientColorKey[] _colourKeys = new GradientColorKey[2];
            readonly GradientAlphaKey[] _alphaKeys = new GradientAlphaKey[4];

            Vector2 _origin, _drift, _endDir;
            float _length, _step, _age, _life, _peak, _spin, _leafAngle, _leafSize;
            int _dir;
            bool _carrying;

            public bool Busy => _age < _life;

            public Wisp(Transform parent, Material material, int order)
            {
                var go = new GameObject("WindWisp");
                go.transform.SetParent(parent, false);
                _line = go.AddComponent<LineRenderer>();
                _line.useWorldSpace = true;
                _line.positionCount = Points;
                _line.numCornerVertices = 2;
                _line.numCapVertices = 0;
                _line.textureMode = LineTextureMode.Stretch;
                _line.alignment = LineAlignment.View;
                _line.widthCurve = Taper;
                _line.sharedMaterial = material;
                _line.sortingOrder = order;
                _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _line.receiveShadows = false;
                _line.enabled = false;

                var leafGo = new GameObject("WindWispLeaf");
                leafGo.transform.SetParent(parent, false);
                _leaf = leafGo.AddComponent<SpriteRenderer>();
                _leaf.sharedMaterial = HarvestEffects.SpriteMaterial();
                _leaf.sortingOrder = order + 1;
                _leaf.enabled = false;
            }

            public void Launch(Vector2 at, int dir, float gust, float scale)
            {
                _dir = dir;
                _length = Random.Range(2.4f, 3.4f) * scale;
                _step = _length / Steps;
                float roll = Random.value;
                Shape(roll < 0.45f ? Kind.Curl : roll < 0.8f ? Kind.Loop : Kind.Wave, Random.value < 0.8f ? 1f : -1f);

                // Begin a little upwind so the wisp sits across the spot the gust is pushing.
                _origin = at - new Vector2(dir * _length * 0.45f, 0f);
                _drift = new Vector2(dir * Random.Range(0.5f, 0.9f) * (1f + gust), Random.Range(-0.04f, 0.12f));
                _life = Random.Range(1.7f, 2.3f) * (gust > 0f ? 1f : 1.25f);
                _age = 0f;
                _line.widthMultiplier = Random.Range(0.08f, 0.11f) * scale;

                bool night = GameClock.IsNight;
                _peak = (gust > 0f ? Mathf.Lerp(0.26f, 0.42f, gust) : 0.2f) * (night ? 0.55f : 1f);
                var tint = night ? new Color(0.85f, 0.9f, 1f) : Color.white;
                _colourKeys[0] = new GradientColorKey(tint, 0f);
                _colourKeys[1] = new GradientColorKey(tint, 1f);

                // Now and then a gust carries a leaf or a petal through its swirl.
                var fx = HarvestEffects.Instance;
                _carrying = false;
                if (gust > 0f && fx != null && Random.value < 0.3f)
                {
                    bool petal = Random.value < 0.35f;
                    var sprite = petal ? fx.PetalSprite : fx.LeafSprite;
                    if (sprite != null)
                    {
                        _carrying = true;
                        _leaf.sprite = sprite;
                        var colour = petal ? PetalColours[Random.Range(0, PetalColours.Length)] : LeafColours[Random.Range(0, LeafColours.Length)];
                        if (night) colour = new Color(colour.r * 0.6f, colour.g * 0.6f, colour.b * 0.7f);
                        _leaf.color = new Color(colour.r, colour.g, colour.b, 0f);
                        _leafSize = Random.Range(0.2f, 0.28f) * scale / Mathf.Max(0.01f, sprite.bounds.size.x);
                        _leafAngle = Random.Range(0f, 360f);
                        _spin = Random.Range(180f, 360f) * (Random.value < 0.5f ? -1f : 1f);
                    }
                }
                _leaf.enabled = false;
            }

            /// <summary>
            /// Lays out the whole path the wisp will trace, in its own space (+X downwind, before
            /// flipping for the wind's direction), by steering a pen along it: a gentle wave, plus
            /// either a tightening swirl at the end, a full loop in the middle, or neither.
            /// </summary>
            void Shape(Kind kind, float sign)
            {
                float swing = Random.Range(0.18f, 0.3f) * (kind == Kind.Wave ? 1.7f : 1f);   // radians
                float freq = Random.Range(0.7f, 1.3f);
                float phase = Random.Range(0f, Mathf.PI * 2f);
                float start = Random.Range(-0.12f, 0.12f);
                float from = kind == Kind.Curl ? Random.Range(0.52f, 0.62f) : Random.Range(0.28f, 0.38f);
                float span = kind == Kind.Curl ? 1f - from : Random.Range(0.36f, 0.44f);
                float turn = kind == Kind.Curl ? Random.Range(1.8f, 2.1f) * Mathf.PI
                           : kind == Kind.Loop ? 2f * Mathf.PI : 0f;
                float wave0 = swing * Mathf.Sin(phase);

                var p = Vector2.zero;
                _path[0] = p;
                for (int i = 1; i <= Steps; i++)
                {
                    float t = (i - 0.5f) / Steps;
                    float x = Mathf.Clamp01((t - from) / span);
                    // Curl: the bend tightens steadily, so the stroke winds into a spiral.
                    // Loop: the bend swells and eases off again, turning once right round.
                    float twist = kind == Kind.Curl ? x * x : (1f - Mathf.Cos(Mathf.PI * x)) * 0.5f;
                    float heading = start + swing * Mathf.Sin(Mathf.PI * 2f * freq * t + phase) - wave0 + sign * turn * twist;
                    p += new Vector2(Mathf.Cos(heading), Mathf.Sin(heading)) * _step;
                    _path[i] = p;
                }
                _endDir = (_path[Steps] - _path[Steps - 1]).normalized;
            }

            Vector2 Along(float s)
            {
                if (s >= _length) return _path[Steps] + _endDir * (s - _length);   // past the end, straight on
                float f = Mathf.Max(0f, s) / _step;
                int i = Mathf.Min((int)f, Steps - 1);
                return Vector2.LerpUnclamped(_path[i], _path[i + 1], f - i);
            }

            Vector3 World(Vector2 local) => new Vector3(_origin.x + _dir * local.x, _origin.y + local.y, -0.5f);

            public void Tick(float dt)
            {
                if (!Busy)
                {
                    if (_line.enabled) _line.enabled = false;
                    if (_leaf.enabled) _leaf.enabled = false;
                    return;
                }
                _age += dt;
                _origin += _drift * dt;
                float u = Mathf.Clamp01(_age / _life);
                if (u >= 1f) { _line.enabled = false; _leaf.enabled = false; return; }

                // The head draws the path at an even pace; the tail sets off later and catches up,
                // so the stroke grows, travels, and winds away into the end of its curl.
                float head = Mathf.Clamp01(u / 0.72f) * _length;
                float tail = Smooth((u - 0.28f) / 0.72f) * _length;
                float fade = Smooth(u / 0.18f) * Smooth((1f - u) / 0.3f);

                if (head - tail > 0.03f && fade > 0.003f)
                {
                    for (int k = 0; k < Points; k++)
                        _points[k] = World(Along(Mathf.Lerp(tail, head, k / (Points - 1f))));
                    _line.SetPositions(_points);
                    float a = _peak * fade;
                    _alphaKeys[0] = new GradientAlphaKey(0f, 0f);
                    _alphaKeys[1] = new GradientAlphaKey(a * 0.75f, 0.35f);
                    _alphaKeys[2] = new GradientAlphaKey(a, 0.8f);
                    _alphaKeys[3] = new GradientAlphaKey(a * 0.85f, 1f);
                    _gradient.SetKeys(_colourKeys, _alphaKeys);
                    _line.colorGradient = _gradient;
                    if (!_line.enabled) _line.enabled = true;
                }
                else if (_line.enabled) _line.enabled = false;

                if (_carrying)
                {
                    // The leaf rides the head, and when the path runs out it is flung on, fading.
                    var t = _leaf.transform;
                    t.position = World(Along(u / 0.72f * _length));
                    _leafAngle += _spin * dt;
                    t.rotation = Quaternion.Euler(0f, 0f, _leafAngle);
                    t.localScale = new Vector3(_leafSize, _leafSize, 1f);
                    var col = _leaf.color;
                    col.a = Smooth(u / 0.12f) * Smooth((1f - u) / 0.2f) * 0.95f;
                    _leaf.color = col;
                    if (!_leaf.enabled) _leaf.enabled = true;
                }
            }
        }
    }
}
