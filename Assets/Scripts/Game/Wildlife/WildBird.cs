using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// One bird about the island (Wildlife decides who and where). It flies in from beyond the
    /// view with its wings flapping, lands in a tree, on the grass or at the shore, and lives there
    /// a while — little hops, pecks and turns — then flies on to another perch or away. Walk right
    /// up and it flutters off to settle a little further on; chop at its tree and it takes off.
    /// A gull wheels over the sea instead, and may come down on the beach; a hummingbird hovers at
    /// the flowers; a passing flock just crosses the sky.
    ///
    /// Tap it with birdwatching taken up to spot it for your journal, and it flies off out of
    /// sight. Without the hobby (or the skill, for a rare one) it just flits off, and once the
    /// binoculars are on sale, the first time a little note says what would let you log it
    /// (Wildlife.NotYet). It only ever goes out of sight: never in front of you (Gone).
    /// </summary>
    [DefaultExecutionOrder(100)]   // after Sway (90), so a perched bird rides the branch the same frame
    public class WildBird : MonoBehaviour, ITappable
    {
        public enum Places { Tree, Ground, Shore, Flower }

        public struct Perch
        {
            public Vector2 At;
            public int Order;
            public Component Node;   // the tree or flower it sits in, if any
            public Places Place;
        }

        enum States { Flying, Perched, Hovering, Soaring }

        static readonly List<WildBird> _all = new();
        public static IReadOnlyList<WildBird> All => _all;

        const float SpotRange = 7f, ScareRange = 1.4f, FlyingOrder = 29000;

        public BirdKind Kind { get; private set; }
        /// <summary>Crossing the sky in a flock: not counted among the island's birds, not to be tapped.</summary>
        public bool Passing { get; private set; }

        SpriteRenderer _sr;
        Sprite _still;
        Sprite[] _flap;
        Style _style;
        bool _artFacesRight;
        States _state;
        Perch _perch, _next;

        // Sitting in a tree that sways in the wind: the swaying picture, where on it the bird sits,
        // and how far the sway has carried it so far (added on top of its hops and fluffs).
        Transform _swayBody;
        Vector3 _swayLocal;
        Vector2 _swayShift;
        bool _landing, _leaving;
        Vector2 _from, _bend, _to;
        float _t, _duration, _flapClock, _glideUntil, _nextFlapBurst;
        float _restUntil, _nextIdle;
        Vector2 _soarCentre;
        float _soarAngle, _soarRadius, _soarUntil;
        Perch? _beach;
        PlayerAvatar _king;

        // ---------------------------------------------------------------- how each bird flies

        struct Style
        {
            public float Width;    // its standing picture's width, in world units
            public float Speed;    // units a second in flight
            public float Fps;      // wingbeats a second (two frames each)
            public bool Glides;    // big birds hold their wings out between bursts of flapping
        }

        // About 70% of their first size: beside a capybara, a sparrow should look like a sparrow.
        static Style StyleOf(BirdKind k) => k.Id switch
        {
            "gull" => new Style { Width = 0.75f, Speed = 3.2f, Fps = 3.5f, Glides = true },
            "heron" or "egret" or "crane" => new Style { Width = 1.05f, Speed = 2.6f, Fps = 2.8f, Glides = true },
            "owl" => new Style { Width = 0.74f, Speed = 3.1f, Fps = 5f },
            "pheasant" => new Style { Width = 0.88f, Speed = 3.4f, Fps = 8f },
            "hummingbird" => new Style { Width = 0.48f, Speed = 3.4f, Fps = 20f },
            "dove" => new Style { Width = 0.7f, Speed = 4f, Fps = 7f },
            "parrot" or "toucan" => new Style { Width = 0.78f, Speed = 3.6f, Fps = 7f },
            _ => new Style { Width = 0.66f, Speed = 4.4f, Fps = 10f },   // the little songbirds
        };

        /// <summary>Its pictures face right (all of them face left but the robin's).</summary>
        static bool FacesRight(BirdKind k) => k.Id == "robin";

        // ---------------------------------------------------------------- arriving

        static WildBird Make(BirdKind kind)
        {
            var go = new GameObject("WildBird_" + kind.Id);
            var bird = go.AddComponent<WildBird>();
            bird.Kind = kind;
            bird._style = StyleOf(kind);
            bird._artFacesRight = FacesRight(kind);
            bird._sr = go.AddComponent<SpriteRenderer>();
            bird._still = HobbyData.BirdPicture(kind);
            var up = HobbyData.Art("Bird_" + kind.Id + "_Fly1");
            var down = HobbyData.Art("Bird_" + kind.Id + "_Fly2");
            bird._flap = up != null && down != null ? new[] { up, down } : null;
            bird._sr.sprite = bird._still;
            // Sized by its standing picture, so standing and flying match.
            float w = bird._still != null ? bird._still.bounds.size.x : 2f;
            go.transform.localScale = Vector3.one * (bird._style.Width / Mathf.Max(0.01f, w));
            bird._flapClock = Random.value;
            bird._nextFlapBurst = Time.time;
            return bird;
        }

        /// <summary>Flies in from beyond the view and lands at <paramref name="perch"/>.</summary>
        public static WildBird Arrive(BirdKind kind, Perch perch)
        {
            var bird = Make(kind);
            bird.transform.position = Wildlife.OffScreen(perch.At);
            bird.FlyTo(perch, leave: false);
            return bird;
        }

        /// <summary>A gull: wheels over the sea around <paramref name="over"/> and may come down at <paramref name="beach"/>.</summary>
        public static WildBird Soar(BirdKind kind, Vector2 over, Perch? beach)
        {
            var bird = Make(kind);
            bird._soarCentre = over;
            bird._soarRadius = Random.Range(1.8f, 3.2f);
            bird._soarAngle = Random.value * Mathf.PI * 2f;
            bird._soarUntil = Time.time + Random.Range(18f, 40f);
            bird._beach = beach;
            bird.transform.position = Wildlife.OffScreen(over);
            // In to the loop first.
            var start = bird.SoarPoint(bird._soarAngle);
            bird.Fly(bird.transform.position, start, () => bird._state = States.Soaring);
            return bird;
        }

        /// <summary>One bird of a passing flock: straight across, then gone. <paramref name="delay"/> staggers the line.</summary>
        public static void Pass(BirdKind kind, Vector2 from, Vector2 to, float delay)
        {
            var bird = Make(kind);
            bird.Passing = true;
            bird.transform.position = from;
            bird._sr.enabled = false;
            DOVirtual.DelayedCall(delay, () =>
            {
                if (bird == null) return;
                bird._sr.enabled = true;
                bird._leaving = true;
                bird.Fly(from, to, bird.Gone, arc: 0.4f);
            }).SetLink(bird.gameObject);
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

        void OnDestroy() => transform.DOKill();   // no hop or bounce left running on a bird that has gone

        /// <summary>Where it sits, or is on its way to settle (so two never pick one spot).</summary>
        public Vector2 Destination => _state == States.Flying && _landing ? _next.At : _state is States.Perched or States.Hovering ? _perch.At : (Vector2)transform.position;

        // ---------------------------------------------------------------- flying

        void FlyTo(Perch perch, bool leave)
        {
            _next = perch;
            _landing = !leave;
            _leaving = leave;
            Fly(transform.position, perch.At, Land);
        }

        System.Action _arrived;

        /// <summary>An arc from <paramref name="from"/> to <paramref name="to"/>, rising in the middle, at the bird's own speed.</summary>
        void Fly(Vector2 from, Vector2 to, System.Action arrived, float arc = 1f)
        {
            transform.DOKill();
            _state = States.Flying;
            _from = from;
            _to = to;
            float d = Vector2.Distance(from, to);
            _bend = (from + to) * 0.5f + Vector2.up * Mathf.Min(3f, d * 0.3f) * arc;
            _duration = Mathf.Max(0.4f, d / _style.Speed);
            _t = 0f;
            _arrived = arrived;
            _sr.sortingOrder = (int)FlyingOrder + (Passing ? 0 : 10);
        }

        static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t) => Vector2.Lerp(Vector2.Lerp(a, b, t), Vector2.Lerp(b, c, t), t);

        void Land()
        {
            if (_leaving) { Gone(); return; }
            _perch = _next;
            _state = _perch.Place == Places.Flower ? States.Hovering : States.Perched;
            _sr.sortingOrder = _perch.Order;
            _sr.sprite = _state == States.Hovering && _flap != null ? _flap[0] : _still;
            // In a swaying tree, it rides the branch (Sway rocks the tree's "Sprite" about its base).
            _swayBody = _perch.Place == Places.Tree && _perch.Node != null && _perch.Node.GetComponent<Sway>() != null
                ? _perch.Node.transform.Find("Sprite") : null;
            if (_swayBody != null) _swayLocal = _swayBody.InverseTransformPoint(_perch.At);
            _swayShift = Vector2.zero;
            _restUntil = Time.time + Random.Range(14f, 35f);
            _nextIdle = Time.time + Random.Range(0.8f, 2.5f);
            transform.DOPunchScale(new Vector3(0.08f, -0.08f, 0f), 0.25f, 4, 0.6f);   // a little bounce as it lands
        }

        /// <summary>Somewhere else to be: another perch of the same kind nearby, or away out of view.</summary>
        void MoveOn(bool startled)
        {
            Sfx.PlayAt(SfxKind.Flutter, transform.position, startled ? 0.55f : 0.35f, 0.15f);
            var view = Wildlife.PerchView();
            Perch? next = null;
            if (Random.value < (startled ? 0.7f : 0.5f))
            {
                next = _perch.Place switch
                {
                    Places.Tree => Wildlife.TreePerch(view),
                    Places.Ground => Random.value < 0.5f ? Wildlife.GroundPerch(view) : Wildlife.TreePerch(view),
                    Places.Shore => Wildlife.ShorePerch(view),
                    _ => Wildlife.FlowerPerch(view),
                };
                // Not right back where the trouble was.
                if (next != null && Vector2.Distance(next.Value.At, transform.position) < 2.5f) next = null;
            }
            if (next != null) FlyTo(next.Value, leave: false);
            else FlyAway();
        }

        /// <summary>Off out of sight: up and away to the nearer side, then gone.</summary>
        public void FlyAway()
        {
            if (_leaving) return;
            if (_state != States.Flying && _state != States.Soaring) Sfx.PlayAt(SfxKind.Flutter, transform.position, 0.4f, 0.15f);
            _leaving = true;
            var p = (Vector2)transform.position;
            var off = Wildlife.OffScreen(p);
            off.y = Mathf.Max(off.y, p.y + 3f);
            Fly(p, off, Gone, arc: 0.6f);
        }

        /// <summary>
        /// Out at the edge it was making for. Gone, if nobody can see it; but if the camera came
        /// along after it (you walked that way), it is still in sight, so it flies on out past the
        /// new edge and goes there. A bird never just vanishes in front of you.
        /// </summary>
        void Gone()
        {
            var p = (Vector2)transform.position;
            if (!Wildlife.Seen(p)) { Destroy(gameObject); return; }
            Fly(p, Wildlife.OffScreen(p), Gone, arc: 0.3f);
        }

        // ---------------------------------------------------------------- every frame

        void Update()
        {
            if (_king == null) _king = FindFirstObjectByType<PlayerAvatar>();
            switch (_state)
            {
                case States.Flying: TickFlying(); break;
                case States.Soaring: TickSoaring(); break;
                case States.Perched: TickPerched(); break;
                case States.Hovering: TickHovering(); break;
            }
            // Gone far out of view while sitting about: nobody will see it go. (One on the wing goes
            // only at the end of its flight, and only out of sight: Gone.)
            if (_state != States.Flying && Wildlife.FarOut(transform.position)) Destroy(gameObject);
        }

        /// <summary>Carried along by the branch it sits on, as the tree sways (after Sway has rocked it this frame).</summary>
        void LateUpdate()
        {
            if (_state != States.Perched || _swayBody == null) return;
            Vector2 shift = (Vector2)_swayBody.TransformPoint(_swayLocal) - _perch.At;
            transform.position += (Vector3)(shift - _swayShift);
            _swayShift = shift;
        }

        void TickFlying()
        {
            _t += Time.deltaTime / _duration;
            float t = Mathf.Clamp01(_t);
            float eased = _landing && !_leaving ? 1f - (1f - t) * (1f - t) : t;   // slows to land
            var before = (Vector2)transform.position;
            var p = Bezier(_from, _bend, _to, eased);
            transform.position = p;
            Face(p.x - before.x);
            Flap(1f);
            if (_t >= 1f) _arrived?.Invoke();
        }

        void TickSoaring()
        {
            // Lazy loops over the water, a little wider and narrower as it goes.
            _soarAngle += Time.deltaTime * (_style.Speed / Mathf.Max(0.5f, _soarRadius)) * 0.6f;
            var before = (Vector2)transform.position;
            var p = SoarPoint(_soarAngle);
            transform.position = p;
            Face(p.x - before.x);
            Flap(1f);
            if (Time.time < _soarUntil) return;
            if (_beach != null && Random.value < 0.5f)
            {
                _next = _beach.Value;
                _landing = true;
                Fly(transform.position, _next.At, Land);
            }
            else FlyAway();
        }

        Vector2 SoarPoint(float a) => _soarCentre + new Vector2(Mathf.Cos(a) * _soarRadius, Mathf.Sin(a * 2f) * _soarRadius * 0.35f);

        void TickPerched()
        {
            if (Startled()) { MoveOn(startled: true); return; }
            if (Time.time >= _restUntil) { MoveOn(startled: false); return; }
            if (Time.time < _nextIdle) return;
            _nextIdle = Time.time + Random.Range(1.2f, 3.5f);
            transform.DOKill(true);
            float r = Random.value;
            if (r < 0.35f) _sr.flipX = !_sr.flipX;                               // looks the other way
            else if (r < 0.65f) transform.DOJump(transform.position, 0.12f, 1, 0.25f);   // a hop
            else if (_perch.Place != Places.Tree)                               // a peck at the ground
                transform.DOPunchRotation(new Vector3(0f, 0f, _sr.flipX == _artFacesRight ? -14f : 14f), 0.35f, 2, 0.5f);
            else transform.DOPunchScale(new Vector3(0.05f, -0.05f, 0f), 0.3f, 3, 0.5f); // a fluff of feathers
        }

        void TickHovering()
        {
            // A hummingbird at the flowers: hangs in the air with a little bob, then darts to the next.
            Flap(1f);
            var p = _perch.At + new Vector2(0f, Mathf.Sin(Time.time * 6f) * 0.05f);
            transform.position = p;
            if (Startled() || Time.time >= _restUntil) MoveOn(startled: Startled());
        }

        bool Startled()
        {
            if (_king != null && Vector2.Distance(_king.transform.position, transform.position) < ScareRange) return true;
            return _perch.Node != null && Wildlife.BeingChopped(_perch.Node);
        }

        /// <summary>Faces the way it is going (its pictures face left, but for the robin's).</summary>
        void Face(float dx)
        {
            if (Mathf.Abs(dx) < 0.0001f) return;
            _sr.flipX = (dx > 0f) != _artFacesRight;
        }

        /// <summary>
        /// The wings: the two flight pictures in turn. A big bird flaps in bursts and holds its
        /// wings out to glide between them. Without flight pictures it keeps its standing one,
        /// with a squash to suggest the beat.
        /// </summary>
        void Flap(float speed)
        {
            bool gliding = _style.Glides && Time.time < _glideUntil;
            if (_style.Glides && Time.time >= _nextFlapBurst)
            {
                _glideUntil = Time.time + Random.Range(0.6f, 1.4f) + 0.8f;
                _nextFlapBurst = _glideUntil + Random.Range(0.4f, 1.2f);
            }
            if (_flap == null)
            {
                _sr.sprite = _still;
                return;
            }
            if (gliding) { _sr.sprite = _flap[0]; return; }
            _flapClock += Time.deltaTime * _style.Fps * 2f * speed;
            _sr.sprite = _flap[(int)_flapClock % 2];
        }

        // ---------------------------------------------------------------- tapping

        public float TapDistance(Vector2 worldPoint)
        {
            if (Passing || _leaving || _sr == null) return -1f;
            float d = Vector2.Distance(_sr.bounds.center, worldPoint);
            return d < 0.9f ? d : -1f;
        }

        public void Tap()
        {
            if (Passing || _leaving) return;
            if (!Wildlife.CanLog(Hobby.Birding, Kind.MinLevel))
            {
                Wildlife.NotYet(Hobby.Birding, Kind.MinLevel);
                MoveOn(startled: true);
                return;
            }
            float d = _king != null ? Vector2.Distance(_king.transform.position, transform.position) : 0f;
            if (d > SpotRange)
            {
                Hints.Answer("That bird is too far away to see properly. Creep a little closer.");
                return;
            }
            bool first = HobbyLog.LogBird(Kind.Id);
            int xp = 5 * Kind.Stars + (first ? 12 : 0);
            HobbyLog.AddXp(Hobby.Birding, xp);
            Motes.Trigger("hobby_bird");
            Sfx.Play(SfxKind.Heart, 0.6f);
            UI.CardPopup.Show("You spotted " + HobbyData.WithArticle(Kind.Name) + "!",
                HobbyData.Rarity(Kind.Stars) + (first ? "\n<color=#2DB3A6><b>New to your journal!</b></color>" : "") + $"\n+{xp} birding XP",
                HobbyData.BirdPicture(Kind));
            FlyAway();   // seen: off it goes, right out of view
        }
    }
}
