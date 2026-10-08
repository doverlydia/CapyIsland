using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The pointing hand that bobs over whatever you are meant to touch next — a bubble to tap, a
    /// patch of ground to drop something on, a mill to staff. For a drag it acts the drag out:
    /// presses on the thing to pick up, slides to where it goes, lifts off, and does it again.
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

        // A drag demonstration: from something to somewhere.
        Transform _dragFrom, _dragToTarget;
        Vector3 _dragToPoint;
        bool _dragging;
        float _dragStart;
        const float DragCycle = 2.2f;
        SpriteRenderer _ghost;   // a faint copy of the thing being dragged, riding under the fingertip

        /// <summary>Shows a drag from <paramref name="from"/> to a spot on the ground, over and over.</summary>
        public void DragFrom(Transform from, Vector2 to)
        {
            if (from == null) { Hide(); return; }
            if (!_dragging || _dragFrom != from || _dragToTarget != null || ((Vector2)_dragToPoint - to).sqrMagnitude > 0.04f) _dragStart = Time.time;
            _dragFrom = from;
            _dragToTarget = null;
            _dragToPoint = new Vector3(to.x, to.y, 0f);
            Drag();
        }

        /// <summary>Shows a drag from one thing onto another (a capybara onto a mill).</summary>
        public void DragFrom(Transform from, Transform onto)
        {
            if (from == null || onto == null) { Hide(); return; }
            if (!_dragging || _dragFrom != from || _dragToTarget != onto) _dragStart = Time.time;
            _dragFrom = from;
            _dragToTarget = onto;
            Drag();
        }

        void Drag()
        {
            _hideAt = -1f;
            _dragging = true;
            _followTarget = null;
            _hasStatic = false;
            Show(true);
        }

        /// <summary>Point at a moving object.</summary>
        public void PointAt(Transform target)
        {
            _dragging = false;
            _hideAt = -1f;
            _followTarget = target;
            _hasStatic = false;
            Show(target != null);
        }

        /// <summary>Point at a fixed spot on the ground.</summary>
        public void PointAt(Vector2 worldPoint)
        {
            _dragging = false;
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

        public void Hide()
        {
            _dragging = false;
            if (_ghost != null) _ghost.enabled = false;
            Show(false);
        }

        /// <summary>Where the hand is pointing, while it is showing.</summary>
        public Vector2? Target
        {
            get
            {
                if (hand == null || !hand.enabled) return null;
                if (_dragging) return _dragFrom != null ? _dragFrom.position : null;   // first, find the thing to pick up
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

        /// <summary>
        /// One loop of the drag: press on the thing, slide to where it goes, lift off, fade, again.
        /// The fingertip touches what it presses, rather than hovering over it.
        /// </summary>
        void TickDrag()
        {
            if (_dragFrom == null) { Hide(); return; }
            Vector3 from = _dragFrom.position;
            Vector3 to = _dragToTarget != null ? _dragToTarget.position : _dragToPoint;
            float touch = heightAboveTarget * 0.35f;
            float t = Mathf.Repeat(Time.time - _dragStart, DragCycle);
            Vector3 at;
            float press = 0f, alpha = 1f;
            if (t < 0.35f)              // finger comes down on the thing
            {
                at = from;
                press = t / 0.35f;
                alpha = Mathf.Clamp01(t / 0.15f);
            }
            else if (t < 1.45f)         // and drags it across
            {
                float k = (t - 0.35f) / 1.1f;
                k = k * k * (3f - 2f * k);
                at = Vector3.Lerp(from, to, k);
                press = 1f;
            }
            else if (t < 1.8f)          // lets go
            {
                at = to;
                press = 1f - (t - 1.45f) / 0.35f;
            }
            else                         // and fades, ready to show it again
            {
                at = to;
                alpha = 1f - (t - 1.8f) / 0.4f;
            }
            transform.position = at + Vector3.up * (touch + (1f - press) * 0.35f);
            hand.transform.localPosition = Vector3.zero;

            // The thing itself, faintly, travelling with the finger while it is pressed.
            if (_ghost == null)
            {
                _ghost = new GameObject("DragGhost").AddComponent<SpriteRenderer>();
                _ghost.transform.SetParent(transform, false);
            }
            var body = _dragFrom.Find("Sprite");
            var src = body != null ? body.GetComponent<SpriteRenderer>() : _dragFrom.GetComponentInChildren<SpriteRenderer>();
            _ghost.enabled = src != null && press > 0.2f;
            if (_ghost.enabled)
            {
                _ghost.sprite = src.sprite;
                _ghost.flipX = src.flipX;
                _ghost.transform.position = at + (src.transform.position - from);
                _ghost.transform.localScale = new Vector3(src.transform.lossyScale.x / Mathf.Max(0.0001f, transform.lossyScale.x),
                                                          src.transform.lossyScale.y / Mathf.Max(0.0001f, transform.lossyScale.y), 1f);
                _ghost.color = new Color(1f, 1f, 1f, 0.45f * alpha * press);
                _ghost.sortingOrder = hand.sortingOrder - 1;
            }
            hand.transform.localScale = Vector3.one * (1f - press * 0.12f);
            var c = hand.color; c.a = alpha; hand.color = c;
        }

        void LateUpdate()
        {
            if (hand == null || !hand.enabled) return;
            if (_hideAt > 0f && Time.time > _hideAt) { _hideAt = -1f; Hide(); return; }
            if (_dragging) { TickDrag(); return; }

            Vector3 anchor;
            if (_followTarget != null) anchor = _followTarget.position;
            else if (_hasStatic) anchor = _staticTarget;
            else { hand.enabled = false; return; }

            hand.transform.localScale = Vector3.one;
            { var c = hand.color; c.a = 1f; hand.color = c; }
            if (_ghost != null) _ghost.enabled = false;
            var destination = anchor + Vector3.up * heightAboveTarget;
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref _velocity, travelSeconds);

            // Bob on the spot so it reads as "here", not as part of the scenery.
            float bob = Mathf.Abs(Mathf.Sin(Time.time * bobRate * Mathf.PI)) * bobHeight;
            hand.transform.localPosition = new Vector3(0f, bob, 0f);
        }
    }
}
