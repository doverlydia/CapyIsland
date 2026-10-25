using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CapyIsland.Game
{
    /// <summary>
    /// One bug about the island (Wildlife decides who and where), moving the way its kind does:
    ///
    ///  * a bumblebee works the flowers, hanging over a bloom with its wings a blur, then buzzing
    ///    off to the next one in the garden;
    ///  * a butterfly (or the moth at night) drifts about in lazy wavering loops, settling on a
    ///    flower now and then with its wings open;
    ///  * a dragonfly hangs in the air over the water by a bridge or the shore, then darts;
    ///  * a firefly wanders slowly through the dark, glowing on and off;
    ///  * everything else (ladybugs, beetles, snails, the silkworm, the rain frog) creeps about on
    ///    its flower, tree or patch of grass.
    ///
    /// Tap one with bug catching taken up and close enough, and the net swoops; without the hobby
    /// (or the skill, for a rare one) it escapes (a crawler just tucks in), and once the net is on
    /// sale, the first time a little note says what would let you catch it (Wildlife.NotYet).
    ///
    /// Nothing is ever taken away while you can see it: a flyer goes off past the edge of the view
    /// first, and a crawler waits until you've looked away (Wildlife.Seen).
    /// </summary>
    public class WildBug : MonoBehaviour, ITappable
    {
        public enum Moves { Bee, Flutter, Dart, Glow, Crawl }

        static readonly List<WildBug> _all = new();
        public static IReadOnlyList<WildBug> All => _all;

        const float NetRange = 4.5f, ScareRange = 0.9f;

        public BugKind Kind { get; private set; }
        public Moves Mode { get; private set; }

        SpriteRenderer _sr, _glow;
        Sprite _still;
        Sprite[] _flap;
        float _flapFps;
        Vector2 _home, _target, _from;
        float _phase, _leaveAt, _moveT, _moveDuration, _restUntil, _flapClock;
        bool _moving, _gone, _resting;
        int _visits;
        PlayerAvatar _king;

        /// <summary>How a kind moves about.</summary>
        public static Moves ModeOf(BugKind k) => k.Id switch
        {
            "Bee" => Moves.Bee,
            "Butterfly" or "BlueButterfly" or "RainbowMoth" => Moves.Flutter,
            "Dragonfly" => Moves.Dart,
            "Firefly" => Moves.Glow,
            _ => Moves.Crawl,
        };

        static float WidthOf(BugKind k) => k.Id switch
        {
            // Small beside a capybara, as a bug should be; the art's dark outline keeps them easy to spot.
            "Snail" or "Ladybug" => 0.44f,
            "Dragonfly" => 0.74f,
            "Firefly" => 0.5f,
            _ => 0.62f,
        };

        // Frame swaps a second. Brisk for bees and dragonflies, lazy for butterflies and fireflies:
        // two drawings swapped much faster than this read as shaking, not flying.
        static float FpsOf(Moves m) => m switch { Moves.Bee => 14f, Moves.Dart => 12f, Moves.Glow => 5f, _ => 6f };

        // ---------------------------------------------------------------- arriving

        static WildBug Make(BugKind kind, Moves mode, Vector2 at, int order)
        {
            var art = HobbyData.Art("Bug_" + kind.Id);
            if (art == null) return null;   // not drawn yet
            var go = new GameObject("WildBug_" + kind.Id);
            go.transform.position = at;
            var bug = go.AddComponent<WildBug>();
            bug.Kind = kind;
            bug.Mode = mode;
            bug._home = at;
            bug._phase = Random.value * 10f;
            bug._sr = go.AddComponent<SpriteRenderer>();
            bug._still = art;
            bug._sr.sprite = art;
            bug._sr.sortingOrder = order > 0 ? order : 29000;
            var up = HobbyData.Art("Bug_" + kind.Id + "_Fly1");
            var down = HobbyData.Art("Bug_" + kind.Id + "_Fly2");
            bug._flap = up != null && down != null ? new[] { up, down } : null;
            bug._flapFps = FpsOf(mode);
            go.transform.localScale = Vector3.one * (WidthOf(kind) / Mathf.Max(0.01f, art.bounds.size.x));
            bug._leaveAt = Time.time + Random.Range(35f, 70f);
            if (kind.Glows) bug.AddGlow();
            return bug;
        }

        /// <summary>Flies in from beyond the view to <paramref name="at"/> (a flower, the water by a bridge).</summary>
        public static WildBug Arrive(BugKind kind, Moves mode, Vector2 at, int order)
        {
            var bug = Make(kind, mode, Wildlife.OffScreen(at), order);
            if (bug == null) return null;
            // A firefly drifts once it's there, but comes in at a steadier pace.
            bug.MoveTo(at, Vector2.Distance(bug.transform.position, at) / Mathf.Max(bug.Speed, 1.4f));
            return bug;
        }

        /// <summary>Fades in where it is: a crawler on its leaf, just out of view.</summary>
        public static WildBug Appear(BugKind kind, Moves mode, Vector2 at, int order)
        {
            var bug = Make(kind, mode, at, order);
            if (bug == null) return null;
            bug._sr.color = new Color(1f, 1f, 1f, 0f);
            bug._sr.DOFade(1f, 0.8f);
            return bug;
        }

        void OnEnable()
        {
            _all.Add(this);
            TapTargets.Register(this);
        }

        void OnDisable()
        {
            _all.Remove(this);
            TapTargets.Unregister(this);
        }

        void OnDestroy()
        {
            transform.DOKill();
            if (_sr != null) _sr.DOKill();
            if (_light != null) _lights--;
        }

        float Speed => Mode switch { Moves.Bee => 1.8f, Moves.Dart => 3.2f, Moves.Glow => 0.5f, Moves.Flutter => 1.3f, _ => 0.2f };

        /// <summary>Where a firefly's lantern is in its pictures: back and low, as a fraction of its width (facing left).</summary>
        static readonly Vector2 Tail = new(0.26f, -0.27f);

        /// <summary>Real lights are dear on a phone: only this many fireflies carry one (as the island's own specks do).</summary>
        const int MaxLights = 3;
        static int _lights;
        Light2D _light;

        /// <summary>
        /// A firefly's lantern: a small point of light on its tail, blinking like the island's own
        /// firefly specks (Atmosphere), drawn unlit so it shines in the dark; the body takes the
        /// island's light like everything else. Only after dusk.
        /// </summary>
        void AddGlow()
        {
            _glow = new GameObject("Lantern").AddComponent<SpriteRenderer>();
            _glow.transform.SetParent(transform, false);
            _glow.sprite = UI.IconPainter.Glow;
            _glow.sharedMaterial = HarvestEffects.SpriteMaterial();   // unlit: it shines in the dark
            _glow.color = Color.clear;
            _glow.sortingOrder = _sr.sortingOrder + 1;
            _lanternSize = 0.42f / Mathf.Max(0.01f, _glow.sprite.bounds.size.x) / transform.localScale.x;
            _period = Random.Range(2.2f, 4.2f);
            // A few carry a soft real light too, so the lantern lights its own tail and the grass under it.
            if (_lights < MaxLights)
            {
                _lights++;
                _light = _glow.gameObject.AddComponent<Light2D>();
                _light.lightType = Light2D.LightType.Point;
                _light.color = new Color(0.8f, 1f, 0.45f);
                _light.pointLightOuterRadius = 1.1f;
                _light.pointLightInnerRadius = 0.05f;
                _light.falloffIntensity = 0.7f;
                _light.intensity = 0f;
            }
        }

        float _lanternSize, _period;

        /// <summary>The lantern on the tail, whichever way the firefly faces, blinking: slowly on, a moment bright, slowly off, a rest in the dark.</summary>
        void Lantern(float t)
        {
            float width = _sr.sprite != null ? _sr.sprite.bounds.size.x : 1f;
            _glow.sortingOrder = _sr.sortingOrder + 1;   // it moves up a layer when it flies off
            _glow.transform.localPosition = new Vector3((_sr.flipX ? -Tail.x : Tail.x) * width, Tail.y * width, 0f);
            float t01 = Mathf.Repeat(t / _period, 1f);
            float blink = t01 < 0.55f ? Mathf.Sin(t01 / 0.55f * Mathf.PI) : 0f;
            _glow.color = new Color(0.86f, 1f, 0.5f, Atmosphere.Darkness * (0.2f + 0.8f * blink));
            _glow.transform.localScale = Vector3.one * _lanternSize * (0.8f + 0.4f * blink);
            if (_light != null) _light.intensity = 0.6f * _glow.color.a;
        }

        // ---------------------------------------------------------------- moving

        void MoveTo(Vector2 to, float seconds)
        {
            _from = transform.position;
            _target = to;
            _moveT = 0f;
            _moveDuration = Mathf.Max(0.25f, seconds);
            _moving = true;
            _resting = false;
        }

        void Update()
        {
            if (_gone) return;
            if (_king == null) _king = FindFirstObjectByType<PlayerAvatar>();
            float t = Time.time + _phase;

            if (_moving)
            {
                _moveT += Time.deltaTime / _moveDuration;
                float k = Mathf.Clamp01(_moveT);
                float eased = Mode == Moves.Dart ? 1f - (1f - k) * (1f - k) * (1f - k) : k * k * (3f - 2f * k);
                var p = Vector2.Lerp(_from, _target, eased);
                // Butterflies waver up and down as they go; bees bob.
                if (Mode == Moves.Flutter) p += new Vector2(0f, Mathf.Sin(t * 5f) * 0.18f);
                else if (Mode == Moves.Bee) p += new Vector2(0f, Mathf.Sin(t * 9f) * 0.05f);
                transform.position = p;
                if (_moveT >= 1f) Arrived();
            }
            else Idle(t);

            if (Mode != Moves.Crawl)
            {
                if (!_resting) Flap(); else _sr.sprite = _still;
                // It turns to face where it is going, never on the little sway of hovering in place
                // (that flipped it back and forth every second or so).
                if (_moving) Face((_target - _from).x);
            }
            if (_glow != null) Lantern(t);

            if (_moving && _target == _leaveTarget) return;   // on its way out
            if (_king != null && Time.time >= _calmAt && Vector2.Distance(_king.transform.position, transform.position) < ScareRange) { Escape(); return; }
            // Fireflies drift off with the dawn.
            if (Time.time > _leaveAt || (Mode == Moves.Glow && Wildlife.FireflyActivity <= 0f)) Leave();
            else if (Wildlife.FarOut(transform.position)) Destroy(gameObject);
        }

        Vector2 _leaveTarget = new(float.NaN, float.NaN);
        float _exitSpeed = 1.4f, _calmAt;

        void Arrived()
        {
            _moving = false;
            _home = transform.position;
            if (_target == _leaveTarget)
            {
                // Out at the edge it was making for. If the camera came along after it (you walked
                // that way), it's still in sight: on it goes, out past the new edge. Never gone in view.
                if (Wildlife.Seen(transform.position)) FlyOff(_exitSpeed);
                else Destroy(gameObject);
                return;
            }
            _restUntil = Time.time + Mode switch
            {
                Moves.Bee => Random.Range(1.5f, 4f),
                Moves.Dart => Random.Range(0.8f, 2.2f),
                Moves.Flutter => Random.Range(2f, 5f),
                _ => Random.Range(2f, 5f),
            };
            // A butterfly settles with its wings open; the others hang in the air.
            _resting = Mode == Moves.Flutter && Random.value < 0.6f;
            // A bee at a bloom hums now and then (one bee at a time, so a garden never drones).
            if (Mode == Moves.Bee && Time.time >= _nextBuzz && Random.value < 0.5f)
            {
                _nextBuzz = Time.time + Random.Range(5f, 9f);
                Sfx.PlayAt(SfxKind.Buzz, transform.position, 0.35f, 0.1f);
            }
        }

        static float _nextBuzz;

        void Idle(float t)
        {
            switch (Mode)
            {
                case Moves.Bee:
                    transform.position = _home + new Vector2(Mathf.Sin(t * 2.3f) * 0.08f, Mathf.Sin(t * 7f) * 0.05f);
                    if (Time.time >= _restUntil) NextFlower(3f);
                    break;
                case Moves.Flutter:
                    if (!_resting) transform.position = _home + new Vector2(Mathf.Sin(t * 1.3f) * 0.25f, Mathf.Sin(t * 2.6f) * 0.15f);
                    if (Time.time >= _restUntil)
                    {
                        // On to another bloom, or just a wander over the grass.
                        if (Random.value < 0.6f) NextFlower(5f);
                        else MoveTo(_home + Random.insideUnitCircle * 2.5f, Random.Range(1.5f, 3f));
                    }
                    break;
                case Moves.Dart:
                    transform.position = _home + new Vector2(Mathf.Sin(t * 3f) * 0.04f, Mathf.Sin(t * 5f) * 0.04f);
                    if (Time.time >= _restUntil)
                    {
                        // A quick dart, keeping to its stretch of water.
                        var to = _home + Random.insideUnitCircle.normalized * Random.Range(0.8f, 1.8f);
                        MoveTo(to, Vector2.Distance(_home, to) / Speed);
                    }
                    break;
                case Moves.Glow:
                    if (Time.time >= _restUntil) MoveTo(_home + Random.insideUnitCircle * 1.2f, Random.Range(2.5f, 4.5f));
                    break;
                default:
                    // A slow creep back and forth.
                    var flat = new Vector2(Mathf.Sin(t * 0.25f) * 0.35f, 0f);
                    transform.position = _home + flat;
                    _sr.flipX = Mathf.Cos(t * 0.25f) > 0f;
                    break;
            }
        }

        /// <summary>Off to another flower within <paramref name="reach"/>; after a few, away.</summary>
        void NextFlower(float reach)
        {
            if (++_visits > Random.Range(4, 8)) { Leave(); return; }
            var view = Wildlife.PerchView();
            var next = Wildlife.FlowerPerch(new Rect(_home.x - reach, _home.y - reach, reach * 2f, reach * 2f));
            if (next == null || !view.Contains(next.Value.At)) { Leave(); return; }
            _sr.sortingOrder = next.Value.Order;
            MoveTo(next.Value.At, Vector2.Distance(transform.position, next.Value.At) / Speed + 0.2f);
        }

        /// <summary>
        /// Off out of view, unhurried. A crawler can't fly: it stays put while you can see it, and
        /// slips away the moment you can't (asked again every frame until then).
        /// </summary>
        void Leave()
        {
            if (Mode == Moves.Crawl)
            {
                if (!Wildlife.Seen(transform.position)) Destroy(gameObject);
                return;
            }
            FlyOff(Speed, nearest: Random.value < 0.6f);
        }

        /// <summary>Away past the edge of the view at <paramref name="speed"/>; it goes once it's out there and out of sight (Arrived).</summary>
        void FlyOff(float speed, bool nearest = true)
        {
            _exitSpeed = Mathf.Max(0.3f, speed);
            _leaveTarget = Wildlife.OffScreen(transform.position, nearest);
            _sr.sortingOrder = 29000;
            MoveTo(_leaveTarget, Vector2.Distance(transform.position, _leaveTarget) / _exitSpeed);
        }

        /// <summary>
        /// Startled: a fast getaway off the edge of the view (a firefly at its own gentler pace).
        /// A crawler can't get away like that: it tucks in where it is, and slips off later, once
        /// you've looked away.
        /// </summary>
        void Escape()
        {
            if (Mode == Moves.Crawl)
            {
                _calmAt = Time.time + 2.5f;                   // one flinch, not one a frame while you stand there
                _leaveAt = Mathf.Min(_leaveAt, Time.time);    // had enough: off as soon as nobody's looking (Leave)
                transform.DOComplete();
                transform.DOPunchScale(transform.localScale * -0.25f, 0.5f, 3, 0.5f);
                return;
            }
            FlyOff(Mode == Moves.Glow ? 2.2f : 5f);
        }

        void Face(float dx)
        {
            if (Mode == Moves.Dart)
            {
                // Drawn from above, head up: it turns to point where it darts.
                var d = _target - _from;
                if (_moving && d.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
                return;
            }
            if (Mathf.Abs(dx) < 0.0005f) return;
            _sr.flipX = dx > 0f;   // the bugs' pictures face left
        }

        void Flap()
        {
            if (_flap == null) { _sr.sprite = _still; return; }
            _flapClock += Time.deltaTime * _flapFps;
            _sr.sprite = _flap[(int)_flapClock % 2];
        }

        // ---------------------------------------------------------------- catching

        public float TapDistance(Vector2 worldPoint)
        {
            if (_gone || _sr == null || (_moving && _target == _leaveTarget)) return -1f;
            float d = Vector2.Distance(_sr.bounds.center, worldPoint);
            return d < 0.7f ? d : -1f;
        }

        public void Tap()
        {
            if (_gone) return;
            if (!Wildlife.CanLog(Hobby.Bugs, Kind.MinLevel))
            {
                Wildlife.NotYet(Hobby.Bugs, Kind.MinLevel);
                Escape();
                return;
            }
            if (_king != null && Vector2.Distance(_king.transform.position, transform.position) > NetRange)
            {
                Hints.Answer("Too far to reach with your net. Tiptoe a little closer.");
                return;
            }
            _gone = true;   // it stops still: here comes the net
            Vector2 paws = _king != null
                ? (Vector2)_king.transform.position + new Vector2(transform.position.x >= _king.transform.position.x ? 0.35f : -0.35f, 0.55f)
                : (Vector2)transform.position;
            NetSwoop(paws, _sr.bounds.center, () =>
            {
                // Into the net it goes.
                transform.DOKill();
                transform.DOScale(0f, 0.2f).SetEase(Ease.InBack);
            }, () =>
            {
                bool first = HobbyLog.LogBug(Kind.Id);
                if (Kind.Id == "Silkworm") HobbyLog.AddToBag("mat:Silk", 1);   // for sewing fedoras and sun hats
                int xp = 5 * Kind.Stars + (first ? 12 : 0);
                HobbyLog.AddXp(Hobby.Bugs, xp);
                Motes.Trigger("hobby_bug");
                Sfx.Play(SfxKind.Pop, 0.6f);
                UI.CardPopup.Show("You caught " + HobbyData.WithArticle(Kind.Name) + "!",
                    HobbyData.Rarity(Kind.Stars) + (first ? "\n<color=#2DB3A6><b>New to your journal!</b></color>" : "") + $"\n+{xp} bug catching XP",
                    _still);
                Destroy(gameObject);
            });
        }

        /// <summary>
        /// The butterfly net: out of your paws, a swoop in an arc down over the bug, a little
        /// wiggle as it goes in, then lifted away. <paramref name="caught"/> runs as the net lands,
        /// <paramref name="done"/> once it has gone.
        /// </summary>
        static void NetSwoop(Vector2 from, Vector2 to, System.Action caught, System.Action done)
        {
            var art = HobbyData.Art("Net");
            if (art == null) { caught(); done(); return; }
            var pivot = new GameObject("BugNet").transform;
            pivot.position = from;
            var body = new GameObject("Net").AddComponent<SpriteRenderer>();
            body.transform.SetParent(pivot, false);
            body.sprite = art;
            body.sortingOrder = 30200;
            // The handle (bottom-left of the picture) at the pivot; the hoop (top-right) out along the reach.
            var e = art.bounds.extents;
            body.transform.localPosition = new Vector3(e.x * 0.72f, e.y * 0.68f, 0f);
            var hoop = new Vector2(e.x * 0.98f, e.y * 1.05f);   // the middle of the hoop, measured on the picture
            float reach = Mathf.Clamp(Vector2.Distance(from, to), 0.5f, 2.2f);
            pivot.localScale = Vector3.one * (reach / Mathf.Max(0.01f, hoop.magnitude));
            float aim = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg - Mathf.Atan2(hoop.y, hoop.x) * Mathf.Rad2Deg;
            bool right = to.x >= from.x;
            float raised = aim + (right ? 80f : -80f);
            pivot.rotation = Quaternion.Euler(0f, 0f, raised);
            body.color = new Color(1f, 1f, 1f, 0f);

            var seq = DOTween.Sequence();
            seq.Append(body.DOFade(1f, 0.1f));
            seq.Append(pivot.DORotate(new Vector3(0f, 0f, aim), 0.22f).SetEase(Ease.InQuad));
            seq.AppendCallback(() =>
            {
                caught();
                Sfx.Play(SfxKind.Plop, 0.7f);
                var fx = HarvestEffects.Instance;
                if (fx != null) fx.Twinkle(to, 6);
            });
            seq.Append(pivot.DOPunchRotation(new Vector3(0f, 0f, 8f), 0.35f, 8, 0.6f));
            seq.Append(pivot.DORotate(new Vector3(0f, 0f, raised), 0.25f).SetEase(Ease.OutQuad));
            seq.Join(body.DOFade(0f, 0.25f));
            seq.OnComplete(() =>
            {
                if (pivot != null) Destroy(pivot.gameObject);
                done();
            });
        }
    }
}
