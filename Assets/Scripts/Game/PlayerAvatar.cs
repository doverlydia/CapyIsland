using DG.Tweening;
using CapyIsland.CameraRig;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The king you control. Driven by the on-screen stick (or WASD / arrows on a keyboard), with
    /// the camera trailing him as he moves.
    ///
    /// You hold anywhere on the ground and he walks toward your finger, tracking it as it moves.
    /// That beats both tap-to-move (a beat of lag between intent and motion) and a virtual stick
    /// (an invisible origin you can miss, and a mapping that drifts the further your thumb travels).
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
        }

        void OnEnable()
        {
            _rig = FindFirstObjectByType<ResponsiveCameraRig>();
            _input = _rig != null ? _rig.GetComponent<CameraInputController>() : null;
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
            var keyboard = new Vector2(
                (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f) +
                (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f),
                (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? -1f : 0f) +
                (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f));
            if (keyboard.sqrMagnitude > 0.01f)
                return keyboard.sqrMagnitude > 1f ? keyboard.normalized : keyboard;

            if (_input == null || !_input.SteerTarget.HasValue) return Vector2.zero;

            Vector2 to = _input.SteerTarget.Value - (Vector2)transform.position;
            float distance = to.magnitude;
            if (distance <= _input.SteerDeadZone) return Vector2.zero;

            // Ease off over the last stride so he arrives rather than skidding past the point.
            float throttle = Mathf.Clamp01(distance / (_input.SteerDeadZone * 3f));
            return to / distance * throttle;
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
            _hopPhase += Time.deltaTime * hopRate * SpeedFraction * Mathf.PI * 2f;
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
            scale.x = Mathf.Abs(scale.x) * (dx < 0f ? -1f : 1f);
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
        /// </summary>
        static Vector2 StepOnLand(Vector2 here, Vector2 step)
        {
            if (step.sqrMagnitude < 1e-8f) return here;
            if (BunniSpace.PointOnLand(here + step)) return here + step;
            // Already off land (e.g. spawned on water): let him walk back.
            if (!BunniSpace.PointOnLand(here)) return here + step;

            for (int turn = 5; turn <= 85; turn += 5)
            {
                var left = (Vector2)(Quaternion.Euler(0f, 0f, turn) * step);
                if (BunniSpace.PointOnLand(here + left)) return here + left * Mathf.Cos(turn * Mathf.Deg2Rad);
                var right = (Vector2)(Quaternion.Euler(0f, 0f, -turn) * step);
                if (BunniSpace.PointOnLand(here + right)) return here + right * Mathf.Cos(turn * Mathf.Deg2Rad);
            }
            return here;
        }
    }
}
