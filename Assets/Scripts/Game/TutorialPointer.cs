using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The pointing hand that bobs over whatever you are meant to touch next — a bubble to tap, a
    /// patch of ground to drop something on, a mill to staff.
    ///
    /// Lives in world space rather than on the canvas so it can sit over a moving target without
    /// any screen-space conversion, and so it is hidden by the same camera bounds as everything else.
    /// </summary>
    public class TutorialPointer : MonoBehaviour, IMapMarker
    {
        [SerializeField] SpriteRenderer hand;

        [Header("Bob")]
        [SerializeField] float bobHeight = 0.34f;
        [SerializeField] float bobRate = 1.8f;

        [Header("Placement")]
        [Tooltip("How far above the target the fingertip sits.")]
        [SerializeField] float heightAboveTarget = 1.5f;

        [Tooltip("Seconds to glide to a new target, so the hand does not teleport.")]
        [SerializeField] float travelSeconds = 0.35f;

        Transform _followTarget;
        Vector3 _staticTarget;
        bool _hasStatic;
        Vector3 _velocity;

        void Awake()
        {
            if (hand == null) hand = GetComponentInChildren<SpriteRenderer>();
            Hide();
        }

        /// <summary>Point at a moving object.</summary>
        public void PointAt(Transform target)
        {
            _hideAt = -1f;
            _followTarget = target;
            _hasStatic = false;
            Show(target != null);
        }

        /// <summary>Point at a fixed spot on the ground.</summary>
        public void PointAt(Vector2 worldPoint)
        {
            _followTarget = null;
            _staticTarget = new Vector3(worldPoint.x, worldPoint.y, 0f);
            _hasStatic = true;
            Show(true);
        }

        float _hideAt = -1f;

        /// <summary>Point at something for a while, then let go (the original's quest focus arrow).</summary>
        public void PointAt(Transform target, float seconds)
        {
            PointAt(target);
            _hideAt = Time.time + seconds;
        }

        public void Hide() => Show(false);

        /// <summary>Where the hand is pointing, while it is showing.</summary>
        public Vector2? Target
        {
            get
            {
                if (hand == null || !hand.enabled) return null;
                if (_followTarget != null) return _followTarget.position;
                if (_hasStatic) return _staticTarget;
                return null;
            }
        }

        // Off screen, the hand gets an arrow at the screen edge like anything else worth finding.
        void OnEnable() => MapMarkers.Register(this);
        void OnDestroy() => MapMarkers.Unregister(this);

        public MarkerKind MarkerKind => MarkerKind.Guide;
        public bool MarkerActive => Target.HasValue;
        public Vector2 MarkerPosition => Target ?? (Vector2)transform.position;
        public Sprite MarkerIcon => hand != null ? hand.sprite : null;

        void Show(bool visible)
        {
            if (hand != null) hand.enabled = visible;
            enabled = true;   // keep ticking so a re-show glides rather than snapping
        }

        void LateUpdate()
        {
            if (hand == null || !hand.enabled) return;
            if (_hideAt > 0f && Time.time > _hideAt) { _hideAt = -1f; Hide(); return; }

            Vector3 anchor;
            if (_followTarget != null) anchor = _followTarget.position;
            else if (_hasStatic) anchor = _staticTarget;
            else { hand.enabled = false; return; }

            var destination = anchor + Vector3.up * heightAboveTarget;
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref _velocity, travelSeconds);

            // Bob on the spot so it reads as "here", not as part of the scenery.
            float bob = Mathf.Abs(Mathf.Sin(Time.time * bobRate * Mathf.PI)) * bobHeight;
            hand.transform.localPosition = new Vector3(0f, bob, 0f);
        }
    }
}
