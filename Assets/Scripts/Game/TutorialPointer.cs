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
    ///
    /// It never slides across the island: sent somewhere new, it fades out where it is and fades in
    /// at the new spot. It steps aside (fades) while a window, speech box or movie is up, and lets go
    /// as soon as the thing it points at is gone (a bubble popped).
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

        [Tooltip("Seconds to settle over a target that moves (a capybara walking).")]
        [SerializeField] float travelSeconds = 0.35f;

        /// <summary>A new target further than this from the old one: fade out, jump, fade in.</summary>
        const float JumpDistance = 1.2f;
        /// <summary>How fast the hand fades in and out (a full fade in about a sixth of a second).</summary>
        const float FadeRate = 6f;

        float _shown;      // 0..1: how visible it is, on top of its own drag-cycle fades
        bool _jumping;     // fading out, to reappear at a new target
        bool _snapNext;    // just shown: appear at the target, rather than travel there

        Transform _followTarget;
        Vector3 _staticTarget;
        bool _hasStatic;
        Vector3 _velocity;

        /// <summary>The pointing hand's picture, for UI that acts out a gesture (fishing's hold).</summary>
        public static Sprite HandSprite
        {
            get
            {
                var p = FindFirstObjectByType<TutorialPointer>(FindObjectsInactive.Include);
                if (p == null) return null;
                var sr = p.hand != null ? p.hand : p.GetComponentInChildren<SpriteRenderer>(true);
                return sr != null ? sr.sprite : null;
            }
        }

        void Awake()
        {
            if (hand == null) hand = GetComponentInChildren<SpriteRenderer>();
            // Over the bubbles it points at (they float above every prop).
            if (hand != null) hand.sortingOrder = Bubble.SortBase + 4500;
            // Always bright: a guide, not part of the island, so the night doesn't dim it.
            if (hand != null) hand.sharedMaterial = HarvestEffects.SpriteMaterial();
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
            if (target != null) JumpIfFar(target.position);
            _dragging = false;
            _hideAt = -1f;
            _followTarget = target;
            _hasStatic = false;
            Show(target != null);
        }

        /// <summary>Point at a fixed spot on the ground.</summary>
        public void PointAt(Vector2 worldPoint)
        {
            JumpIfFar(worldPoint);
            _dragging = false;
            _hideAt = -1f;
            _followTarget = null;
            _staticTarget = new Vector3(worldPoint.x, worldPoint.y, 0f);
            _hasStatic = true;
            Show(true);
        }

        /// <summary>Point at a spot for a while, then let go ("Show me").</summary>
        public void PointAt(Vector2 worldPoint, float seconds)
        {
            PointAt(worldPoint);
            _hideAt = Time.time + seconds;
        }

        /// <summary>Already showing somewhere else: it fades out there and in at the new target.</summary>
        void JumpIfFar(Vector2 to)
        {
            if (hand == null || !hand.enabled || _dragging) return;
            var from = Target;
            if (from.HasValue && Vector2.Distance(from.Value, to) > JumpDistance) _jumping = true;
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
            if (hand != null)
            {
                // Showing from nothing: it fades in right at the target, never flies in from the last one.
                if (visible && !hand.enabled) { _snapNext = true; _jumping = false; _shown = 0f; }
                hand.enabled = visible;
            }
            enabled = true;
        }

        /// <summary>
        /// One loop of the drag: press on the thing, slide to where it goes, lift off, fade, again.
        /// The fingertip touches what it presses, rather than hovering over it.
        /// </summary>
        /// <summary>How far above the point it shows the fingertip sits, in world units.</summary>
        public float FingertipLift => heightAboveTarget * 0.35f;

        void TickDrag()
        {
            if (_dragFrom == null) { Hide(); return; }
            Vector3 from = _dragFrom.position;
            Vector3 to = _dragToTarget != null ? _dragToTarget.position : _dragToPoint;
            float touch = FingertipLift;
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
            // What it pointed at is gone (a bubble popped, a heart sent home): let go at once.
            if (_followTarget != null && !_followTarget.gameObject.activeInHierarchy) { Hide(); return; }
            // A window, speech box or movie is up: step aside until it's gone, then come back.
            bool covered = Attention.ScreenBusy;
            if (_dragging) { TickDrag(); Fade(covered ? 0f : 1f); return; }

            Vector3 anchor;
            if (_followTarget != null) anchor = _followTarget.position;
            else if (_hasStatic) anchor = _staticTarget;
            else { hand.enabled = false; return; }

            hand.transform.localScale = Vector3.one;
            { var c = hand.color; c.a = 1f; hand.color = c; }
            if (_ghost != null) _ghost.enabled = false;
            var destination = anchor + Vector3.up * heightAboveTarget;
            if (_snapNext || (_jumping && _shown <= 0.01f))
            {
                // Just shown, or faded out at the old target: appear at the new one.
                transform.position = destination;
                _velocity = Vector3.zero;
                _snapNext = _jumping = false;
            }
            else if (!_jumping)
                transform.position = Vector3.SmoothDamp(transform.position, destination, ref _velocity, travelSeconds);

            // Bob on the spot so it reads as "here", not as part of the scenery.
            float bob = Mathf.Abs(Mathf.Sin(Time.time * bobRate * Mathf.PI)) * bobHeight;
            hand.transform.localPosition = new Vector3(0f, bob, 0f);
            Fade(covered || _jumping ? 0f : 1f);
        }

        /// <summary>Eases the hand's visibility toward <paramref name="target"/>, on top of the alpha set this frame.</summary>
        void Fade(float target)
        {
            _shown = Mathf.MoveTowards(_shown, target, Time.unscaledDeltaTime * FadeRate);
            var c = hand.color;
            c.a *= _shown;
            hand.color = c;
            if (_ghost != null && _ghost.enabled) { var g = _ghost.color; g.a *= _shown; _ghost.color = g; }
        }
    }
}
