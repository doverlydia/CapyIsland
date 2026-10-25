using System.Collections.Generic;
using DG.Tweening;
using CapyIsland.CameraRig;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The king you control, with the camera trailing him as he moves (WASD / arrows on a keyboard).
    ///
    /// Tap a spot on the ground and he walks there; tap the sea and he walks down to the nearest
    /// bit of shore he can reach, ready to fish. Hold on the ground and he walks toward your
    /// finger, tracking it as it moves — or, with the joystick on (Settings), a floating stick
    /// under your thumb steers him, so you never have to reach for the edge of the screen. See
    /// <see cref="CameraInputController"/> for the gestures.
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] float moveSpeed = 6f;
        [Tooltip("Seconds to reach full speed, and to stop. Keeps starts and stops from snapping.")]
        [SerializeField] float acceleration = 14f;

        [Header("Hop")]
        [Tooltip("Height of the bounce while walking. 0 disables it.")]
        [SerializeField] float hopHeight = 0.14f;
        [Tooltip("Hops per second at full speed.")]
        [SerializeField] float hopRate = 3.4f;

        [Header("Camera")]
        [SerializeField] bool cameraFollows = true;
        [Tooltip("Higher is snappier. The camera trails rather than locks, so motion reads.")]
        [SerializeField] float followLerp = 4.5f;

        Transform _body;          // carries the sprite, so hopping never lifts the shadow
        Vector3 _bodyRestLocal;
        Vector2 _velocity;
        float _hopPhase;

        Vector2 _lastShake;
        ResponsiveCameraRig _rig;
        CameraInputController _input;

        // Walking somewhere by itself (an edge arrow was tapped), until it gets there or you steer.
        Vector2? _autoTarget;
        float _autoArrive;
        float _autoBest, _autoBestAt;

        /// <summary>True while walking to a tapped arrow's target.</summary>
        public bool AutoWalking => _autoTarget.HasValue;

        /// <summary>True while walking to a spot on the ground the player tapped (a short trip, no stop button).</summary>
        public bool TapWalking => _autoTarget.HasValue && _tapWalk;
        bool _tapWalk;

        /// <summary>
        /// Walks to <paramref name="target"/> on its own, stopping <paramref name="arrive"/> short
        /// of it. Any touch on the ground, a key, or picking something up takes control back; if
        /// the way is blocked (water between), it stops and says so.
        /// </summary>
        public void WalkTo(Vector2 target, float arrive = 1.6f)
        {
            _tapWalk = false;
            // A route over land: round a bay, over a bridge — never straight into the sea.
            var land = LandMap.Instance;
            _route = land != null ? land.FindPath(transform.position, target) : new List<Vector2> { target };
            if (_route == null || _route.Count == 0)
            {
                _autoTarget = null;
                Hints.Answer(Unreachable);
                return;
            }
            _routeAt = 0;
            _replans = 0;
            _autoTarget = target;
            _autoArrive = arrive;
            _autoBest = Vector2.Distance(transform.position, _route[0]);
            _autoBestAt = Time.time;
        }

        List<Vector2> _route;
        int _routeAt, _replans;

        public void StopWalking() => _autoTarget = null;

        /// <summary>
        /// "Take me there": the king walks to <paramref name="target"/> (edge arrows, Goals' Show
        /// me). Without a king, the camera just looks.
        /// </summary>
        public static void GoTo(Vector2 target)
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king == null) { CameraFocus.Show(target, 2.5f); return; }

            // Across the water with no open bridge: say so, and don't set off at all.
            var land = LandMap.Instance;
            if (land != null && !land.Reachable(king.transform.position, target))
            {
                Hints.Answer(Unreachable);
                return;
            }
            king.WalkTo(target);
        }

        const string Unreachable = "You can't get there from here.";

        /// <summary>Stops the king walking by himself, if he is.</summary>
        public static void StopAutoWalk()
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king != null) king.StopWalking();
        }

        /// <summary>Fired each time a hop lands while walking (the footstep dust listens).</summary>
        public event System.Action Landed;

        /// <summary>Current walking velocity, in units per second.</summary>
        public Vector2 Velocity => _velocity;

        /// <summary>Current speed as a fraction of the maximum. Useful for animation later.</summary>
        public float SpeedFraction => moveSpeed <= 0f ? 0f : _velocity.magnitude / moveSpeed;

        void Awake()
        {
            var sprite = transform.Find("Sprite");
            _body = sprite != null ? sprite : transform;
            _bodyRestLocal = _body.localPosition;
            // The wardrobe dresses the player; added here so the island builder needn't know.
            if (GetComponent<CapyOutfit>() == null) gameObject.AddComponent<CapyOutfit>();
            if (GetComponent<DesignWear>() == null) gameObject.AddComponent<DesignWear>();
            if (GetComponent<Footsteps>() == null) gameObject.AddComponent<Footsteps>();
        }

        void OnEnable()
        {
            _rig = FindFirstObjectByType<ResponsiveCameraRig>();
            _input = _rig != null ? _rig.GetComponent<CameraInputController>() : null;
            if (_input != null) _input.Tapped += OnGroundTapped;
        }

        void OnDisable()
        {
            if (_input != null) _input.Tapped -= OnGroundTapped;
        }

        /// <summary>
        /// A quick tap on bare ground: walk there, whichever way you like to steer. A tap on the sea
        /// walks you down to the water's edge nearest it (somewhere you can fish from); land you
        /// can't reach yet says so.
        /// </summary>
        void OnGroundTapped(Vector2 world)
        {
            if (!BunniSpace.PointOnLand(world))
            {
                // A line out: a stray tap on the water mustn't walk you off and lose the fish.
                var rod = Fishing.Instance;
                if (rod != null && rod.Now != Fishing.State.Idle) return;
                var shore = ShoreNear(world);
                if (!shore.HasValue) return;   // no shore of yours anywhere: just a tap
                world = shore.Value;
            }
            if (Vector2.Distance(transform.position, world) < 0.5f) return;
            var land = LandMap.Instance;
            if (land != null && !land.Reachable(transform.position, world)) { Hints.Answer(Unreachable); return; }
            WalkTo(world, 0.15f);
            if (!_autoTarget.HasValue) return;
            _tapWalk = true;
            WalkMarker.Show(world);
        }

        /// <summary>How far in from the water's edge a walk to the shore stops, in world units: dry feet, rod's reach.</summary>
        const float ShoreInset = 8f;

        /// <summary>
        /// The spot at the water's edge nearest a point out on the sea, on land you can walk to from
        /// here (never the beach of an island whose bridge isn't open), a step in from the waterline
        /// so you stand on the sand with the sea close enough to fish (<see cref="Fishing.AtShore"/>).
        /// Null if there is none.
        ///
        /// The land is made of circles (<see cref="LandMap.Piece"/>), so the shore is their rims
        /// wherever no other circle covers them. Each rim is walked round, nearest circles first,
        /// and the closest uncovered point (as it looks on screen, not in the squashed world) wins.
        /// </summary>
        Vector2? ShoreNear(Vector2 sea)
        {
            var land = LandMap.Instance;
            if (land == null) return null;
            Vector2 seaWorld = LandMap.UnityToWorld(sea);
            Vector2 here = transform.position;
            int myIsland = land.IslandAt(here);

            // Every land circle, by how near its rim could possibly be on screen (a world unit is
            // never less than half of UnitsPerWorld on screen: y is squashed 2:1).
            var circles = new List<(float bound, Vector3 c)>();
            foreach (var piece in land.Pieces)
            {
                if (piece.isBridge && !piece.open) continue;
                foreach (var c in piece.circles)
                {
                    float gap = Vector2.Distance(seaWorld, new Vector2(c.x, c.y)) - c.z;
                    circles.Add((Mathf.Max(0f, gap) * BunniSpace.UnitsPerWorld * 0.5f, c));
                }
            }
            circles.Sort((a, b) => a.bound.CompareTo(b.bound));

            Vector2? best = null;
            float bestD = float.MaxValue;
            foreach (var (bound, c) in circles)
            {
                if (bound >= bestD) break;   // no rim further down the list can be nearer
                var centre = new Vector2(c.x, c.y);
                // A point every quarter of a unit or so along the rim.
                int around = Mathf.Clamp(Mathf.CeilToInt(2f * Mathf.PI * c.z * BunniSpace.UnitsPerWorld / 0.25f), 16, 720);
                for (int i = 0; i < around; i++)
                {
                    float a = i * Mathf.PI * 2f / around;
                    var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    var rim = LandMap.WorldToUnity(centre + dir * c.z);
                    float d = Vector2.Distance(rim, sea);
                    if (d >= bestD) continue;
                    // Covered by another circle: inland, not shore.
                    if (land.IsOnLand(LandMap.WorldToUnity(centre + dir * (c.z + 4f)))) continue;
                    var spot = LandMap.WorldToUnity(centre + dir * Mathf.Max(0f, c.z - ShoreInset));
                    if (!land.IsOnLand(spot) || !SameLand(land, myIsland, here, spot)) continue;
                    if (!Fishing.WaterNear(spot, 2.6f).HasValue) continue;
                    best = spot;
                    bestD = d;
                }
            }
            return best;
        }

        /// <summary>
        /// True if a spot is on land joined to where you stand. Standing on a bridge, an island
        /// counts if it is open (joined to the first island); a spot on an open bridge always does.
        /// </summary>
        static bool SameLand(LandMap land, int myIsland, Vector2 here, Vector2 spot)
        {
            int island = land.IslandAt(spot);
            if (island < 0) return true;   // an open bridge (a closed one isn't land at all)
            if (myIsland < 0) return land.IslandOpen(island);
            return land.Reachable(here, spot);
        }

        void Update()
        {
            Steer();
            Move();
            if (cameraFollows) FollowCamera();
        }

        void Steer()
        {
            Vector2 wanted = WantedDirection();

            // Accelerate toward the stick, decelerate to a stop when it is released. Same curve
            // either way, so the capybara has a little weight without feeling sluggish.
            Vector2 target = wanted * moveSpeed;
            _velocity = Vector2.MoveTowards(_velocity, target, acceleration * Time.deltaTime);
        }

        /// <summary>
        /// Unit direction the player is asking for: toward the pointed-at world point, or from the
        /// keyboard on desktop. Zero inside the dead zone so he settles instead of jittering.
        /// </summary>
        Vector2 WantedDirection()
        {
            // Typing in a box (chat, a name): those W, A, S and D keys are letters, not steps.
            var selected = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
            bool typing = selected != null && selected.TryGetComponent<TMPro.TMP_InputField>(out var box) && box.isFocused;
            var keyboard = typing ? Vector2.zero : new Vector2(
                (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f) +
                (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f),
                (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? -1f : 0f) +
                (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f));
            if (keyboard.sqrMagnitude > 0.01f)
            {
                _autoTarget = null;
                return keyboard.sqrMagnitude > 1f ? keyboard.normalized : keyboard;
            }

            // Your finger always wins over walking by itself.
            if (_input != null && (_input.SteerTarget.HasValue || _input.IsManipulating)) _autoTarget = null;
            if (_autoTarget.HasValue) return AutoDirection();

            if (_input == null || !_input.SteerTarget.HasValue) return Vector2.zero;

            Vector2 to = _input.SteerTarget.Value - (Vector2)transform.position;
            float distance = to.magnitude;
            if (distance <= _input.SteerDeadZone) return Vector2.zero;

            // Ease off over the last stride so he arrives rather than skidding past the point.
            float throttle = Mathf.Clamp01(distance / (_input.SteerDeadZone * 3f));
            return to / distance * throttle;
        }

        Vector2 AutoDirection()
        {
            // Close enough to the end: arrived.
            if (Vector2.Distance(_autoTarget.Value, transform.position) <= _autoArrive) { _autoTarget = null; return Vector2.zero; }

            // Head for the next corner of the route; on reaching it, the one after.
            bool last = _route == null || _routeAt >= _route.Count - 1;
            Vector2 waypoint = _route != null && _route.Count > 0 ? _route[Mathf.Min(_routeAt, _route.Count - 1)] : _autoTarget.Value;
            Vector2 to = waypoint - (Vector2)transform.position;
            float distance = to.magnitude;
            if (!last && distance < 0.6f)
            {
                _routeAt++;
                waypoint = _route[_routeAt];
                to = waypoint - (Vector2)transform.position;
                distance = to.magnitude;
                _autoBest = distance; _autoBestAt = Time.time;
                last = _routeAt >= _route.Count - 1;
            }
            if (last && distance <= _autoArrive) { _autoTarget = null; return Vector2.zero; }

            // Not getting any closer for a couple of seconds — stuck, or sliding along a coast
            // with the sea in between: stop and say so.
            if (distance < _autoBest - 0.4f) { _autoBest = distance; _autoBestAt = Time.time; }
            else if (Time.time - _autoBestAt > 1.5f)
            {
                // Snagged on a corner of the coast: plan again from here before giving up.
                var land = LandMap.Instance;
                var again = _replans < 2 && land != null ? land.FindPath(transform.position, _autoTarget.Value) : null;
                if (again != null && again.Count > 0)
                {
                    _replans++;
                    _route = again; _routeAt = 0;
                    _autoBest = Vector2.Distance(transform.position, _route[0]); _autoBestAt = Time.time;
                    return Vector2.zero;
                }
                _autoTarget = null;
                Hints.Answer(Unreachable);
                return Vector2.zero;
            }

            // Ease off over the last stride, like steering by finger; full pace round the corners.
            float throttle = last ? Mathf.Clamp01((distance - _autoArrive) / 1.2f + 0.35f) : 1f;
            return to / Mathf.Max(0.0001f, distance) * throttle;
        }

        void Move()
        {
            if (_velocity.sqrMagnitude < 0.0001f) { ResetHop(); return; }

            Vector2 here = transform.position;
            Vector2 next = StepOnLand(here, _velocity * Time.deltaTime);
            transform.position = new Vector3(next.x, next.y, transform.position.z);

            Face(_velocity.x);
            Hop();
        }

        void Hop()
        {
            if (hopHeight <= 0f || _body == null) return;

            // Hop faster the faster he moves, so the bounce matches the pace.
            float before = _hopPhase;
            _hopPhase += Time.deltaTime * hopRate * SpeedFraction * Mathf.PI * 2f;
            // Each time the bounce comes back down, the feet touch the ground.
            if (Mathf.FloorToInt(_hopPhase / Mathf.PI) != Mathf.FloorToInt(before / Mathf.PI)) Landed?.Invoke();
            float lift = Mathf.Abs(Mathf.Sin(_hopPhase)) * hopHeight * SpeedFraction;
            _body.localPosition = _bodyRestLocal + new Vector3(0f, lift, 0f);
        }

        void ResetHop()
        {
            _hopPhase = 0f;
            if (_body != null)
                _body.localPosition = Vector3.Lerp(_body.localPosition, _bodyRestLocal, Time.deltaTime * 12f);
        }

        void Face(float dx)
        {
            if (_body == null || Mathf.Abs(dx) < 0.01f) return;
            var scale = _body.localScale;
            // the pictures face left: mirror them to walk right
            scale.x = Mathf.Abs(scale.x) * (dx > 0f ? -1f : 1f);
            _body.localScale = scale;
        }

        /// <summary>
        /// Map travel: off in a puff, and here in another — with everything in your trail. The
        /// camera jumps with you rather than gliding across the whole island.
        /// </summary>
        public void TravelTo(Vector2 to)
        {
            Vector2 from = transform.position;
            var delta = to - from;
            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Poof(from, Color.white);
            transform.position = new Vector3(to.x, to.y, transform.position.z);
            _velocity = Vector2.zero;
            foreach (var f in FindObjectsByType<Follower>(FindObjectsSortMode.None))
                if (f.Carried) f.transform.position += (Vector3)delta;
            if (_rig != null) _rig.MoveTo(new Vector3(to.x, to.y, _rig.transform.position.z));
            if (fx != null) { fx.Poof(to, Color.white); fx.Twinkle(to + Vector2.up * 0.8f, 8); }
            Sfx.Play(SfxKind.Pop, 0.6f);
            if (_body != null) _body.DOPunchScale(new Vector3(0.15f, 0.2f, 0f), 0.35f, 6, 0.6f);
        }

        void FollowCamera()
        {
            if (_rig == null) return;

            var camPos = _rig.transform.position;
            Vector2 look = transform.position;
            if (CameraFocus.TryGet(out var focus)) look = focus;
            var target = new Vector3(look.x, look.y, camPos.z);
            var next = Vector3.Lerp(camPos - (Vector3)_lastShake, target, Time.deltaTime * followLerp);
            _lastShake = CameraShake.Offset;
            _rig.MoveTo(next + (Vector3)_lastShake);
        }

        /// <summary>
        /// One step that never leaves land. Blocked straight ahead, it tries turning a little either
        /// way, further each time, so the king slides along the coast instead of sticking to it —
        /// the original's NextStep, which rotates the step in 5 degree increments.
        ///
        /// The whole stride is checked, not just where it lands: a long step on a stuttery frame
        /// must not hop a narrow channel onto the next island.
        /// </summary>
        static Vector2 StepOnLand(Vector2 here, Vector2 step)
        {
            if (step.sqrMagnitude < 1e-8f) return here;
            // Stranded on water somehow (an old save, a spawn spot): back to the nearest shore,
            // never free to wander the sea.
            if (!BunniSpace.PointOnLand(here)) return TowardLand(here, step.magnitude);
            if (Walkable(here, here + step)) return here + step;

            for (int turn = 5; turn <= 85; turn += 5)
            {
                var left = (Vector2)(Quaternion.Euler(0f, 0f, turn) * step);
                if (Walkable(here, here + left)) return here + left * Mathf.Cos(turn * Mathf.Deg2Rad);
                var right = (Vector2)(Quaternion.Euler(0f, 0f, -turn) * step);
                if (Walkable(here, here + right)) return here + right * Mathf.Cos(turn * Mathf.Deg2Rad);
            }
            return here;
        }

        static bool Walkable(Vector2 from, Vector2 to)
        {
            if (!BunniSpace.PointOnLand(to)) return false;
            var land = LandMap.Instance;
            return land == null || land.SegmentOnLand(from, to);
        }

        /// <summary>A step toward the nearest land, whichever way the player is pushing.</summary>
        static Vector2 TowardLand(Vector2 here, float stride)
        {
            for (float r = 0.25f; r <= 10f; r += 0.25f)
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI * 2f / 24f;
                var p = here + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (!BunniSpace.PointOnLand(p)) continue;
                var to = p - here;
                return here + to.normalized * Mathf.Min(to.magnitude, Mathf.Max(stride, 0.12f));
            }
            return here;   // no land anywhere near: stay put rather than drift out to sea
        }
    }
}
