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

        [Header("Island bounds")]
        [Tooltip("Must match IslandBuilder: the ellipse the landmass occupies.")]
        [SerializeField] float islandRadiusX = 14.4f;
        [SerializeField] float islandRadiusY = 10.4f;

        [Header("Camera")]
        [SerializeField] bool cameraFollows = true;
        [Tooltip("Higher is snappier. The camera trails rather than locks, so motion reads.")]
        [SerializeField] float followLerp = 4.5f;

        Transform _body;          // carries the sprite, so hopping never lifts the shadow
        Vector3 _bodyRestLocal;
        Vector2 _velocity;
        float _hopPhase;

        ResponsiveCameraRig _rig;
        CameraInputController _input;

        /// <summary>Current speed as a fraction of the maximum. Useful for animation later.</summary>
        public float SpeedFraction => moveSpeed <= 0f ? 0f : _velocity.magnitude / moveSpeed;

        void Awake()
        {
            var sprite = transform.Find("Sprite");
            _body = sprite != null ? sprite : transform;
            _bodyRestLocal = _body.localPosition;
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
            Vector2 next = ClampToIsland(here + _velocity * Time.deltaTime);
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

        void FollowCamera()
        {
            if (_rig == null) return;

            var camPos = _rig.transform.position;
            var target = new Vector3(transform.position.x, transform.position.y, camPos.z);
            _rig.MoveTo(Vector3.Lerp(camPos, target, Time.deltaTime * followLerp));
        }

        /// <summary>Keeps the king on land. Mirrors IslandBuilder's ellipse.</summary>
        Vector2 ClampToIsland(Vector2 p)
        {
            if (islandRadiusX <= 0f || islandRadiusY <= 0f) return p;
            float nx = p.x / islandRadiusX, ny = p.y / islandRadiusY;
            float d = nx * nx + ny * ny;
            if (d <= 1f) return p;
            float scale = 1f / Mathf.Sqrt(d);
            return new Vector2(p.x * scale, p.y * scale);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.4f, 0.6f);
            const int steps = 48;
            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= steps; i++)
            {
                float a = i / (float)steps * Mathf.PI * 2f;
                var pt = new Vector3(Mathf.Cos(a) * islandRadiusX, Mathf.Sin(a) * islandRadiusY, 0f);
                if (i > 0) Gizmos.DrawLine(prev, pt);
                prev = pt;
            }
        }
    }
}
