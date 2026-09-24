using System;
using UnityEngine;

namespace CapyIsland.CameraRig
{
    /// <summary>
    /// All world input, in one place. Legacy Input Manager, which is what this project's Active
    /// Input Handling is set to; touch and mouse share one path so Android and WebGL behave alike.
    ///
    /// Three gestures, and they never compete for the same press:
    ///
    /// * press on an item — pick it up and drag it
    /// * press on the ground and hold — the king walks toward your finger, tracking it as it moves
    /// * quick tap on the ground — a tap, for bubbles and shops
    ///
    /// Steering points at a destination rather than emulating a stick. A stick needs an origin the
    /// player cannot see, so a press landing slightly off feels like it missed, and the further the
    /// thumb travels the more the mapping drifts. Pointing has no origin to miss: the king goes
    /// where you point, which is as accurate as your finger.
    ///
    /// The camera follows the king, so there is no separate pan gesture to conflict with any of it.
    /// </summary>
    [RequireComponent(typeof(ResponsiveCameraRig))]
    public class CameraInputController : MonoBehaviour
    {
        [Header("Zoom")]
        [SerializeField] float pinchSensitivity = 1f;
        [SerializeField] float scrollSensitivity = 0.1f;

        [Header("Tap")]
        [Tooltip("A press shorter than this, that moved less than the slop below, counts as a tap.")]
        [SerializeField] float tapMaxDuration = 0.6f;
        [Tooltip("Movement allowance in screen pixels before a tap becomes a hold.")]
        [SerializeField] float tapSlopPixels = 18f;

        [Header("Steering")]
        [Tooltip("The king ignores a steer target closer than this, so he does not jitter on the spot.")]
        [SerializeField] float steerDeadZone = 0.35f;

        /// <summary>Raised on a quick tap that did not land on an item.</summary>
        public event Action<Vector2> Tapped;

        /// <summary>World point the player is pointing at, or null when not steering.</summary>
        public Vector2? SteerTarget { get; private set; }

        /// <summary>True while the player is dragging an item or pinching.</summary>
        public bool IsManipulating => _heldActor != null || _pinching;

        public float SteerDeadZone => steerDeadZone;

        ResponsiveCameraRig _rig;
        Camera _camera;

        bool _pressing;
        Vector2 _pressStartScreen;
        float _pressStartTime;
        bool _movedBeyondSlop;
        Game.DraggableActor _heldActor;
        Game.ShopBuilding _pendingShop;

        bool _pinching;
        float _pinchStartDistance;
        float _pinchStartZoom;

        void Awake()
        {
            _rig = GetComponent<ResponsiveCameraRig>();
            _camera = GetComponent<Camera>();
        }

        void Update()
        {
            if (_camera == null) return;

            if (Input.touchCount >= 2) { HandlePinch(); return; }

            if (_pinching)
            {
                // Coming out of a pinch, drop everything rather than lurching into a steer.
                _pinching = false;
                _pressing = false;
                _heldActor = null;
                _pendingShop = null;
                SteerTarget = null;
                return;
            }

            HandleScrollZoom();
            HandlePointer();
        }

        void HandlePinch()
        {
            var a = Input.GetTouch(0).position;
            var b = Input.GetTouch(1).position;
            float distance = Vector2.Distance(a, b);
            if (distance < 1f) return;

            if (!_pinching)
            {
                _pinching = true;
                _pressing = false;
                SteerTarget = null;
                _pinchStartDistance = distance;
                _pinchStartZoom = _rig.Zoom;
                return;
            }

            // Fingers apart = see more world = larger orthographic size = higher zoom value here.
            float ratio = _pinchStartDistance / Mathf.Max(1f, distance);
            _rig.Zoom = _pinchStartZoom * Mathf.Lerp(1f, ratio, pinchSensitivity);
        }

        void HandleScrollZoom()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f) return;
            _rig.Zoom -= Mathf.Sign(scroll) * scrollSensitivity;
        }

        void HandlePointer()
        {
            GetPointer(out var screen, out bool pressed, out bool released);

            // --- press begins ---
            if (pressed && !_pressing)
            {
                _pressing = true;
                _movedBeyondSlop = false;
                _pressStartScreen = screen;
                _pressStartTime = Time.unscaledTime;

                var world = ScreenToWorld(screen);

                // A shop's sample sits right beside its building, inside the building's own grab
                // circle. The sample wins, or pressing it would just pick the shop up and the
                // purchase dialog could never open.
                _pendingShop = Game.ShopBuilding.Pick(world);
                if (_pendingShop != null) { SteerTarget = null; return; }

                _heldActor = Game.DraggableActor.Pick(world);
                if (_heldActor != null)
                {
                    _heldActor.BeginDrag();
                    SteerTarget = null;
                }
                else
                {
                    // Ground press: steer at once, so holding moves you without waiting for a drag.
                    SteerTarget = world;
                }
                return;
            }

            // --- press continues ---
            if (pressed)
            {
                if (!_movedBeyondSlop && Vector2.Distance(screen, _pressStartScreen) > tapSlopPixels)
                    _movedBeyondSlop = true;

                if (_pendingShop != null) return;   // holding a shop sample does nothing

                var world = ScreenToWorld(screen);
                if (_heldActor != null) _heldActor.DragTo(world);
                else SteerTarget = world;   // track the finger
                return;
            }

            // --- press ends ---
            if (released && _pressing)
            {
                _pressing = false;
                SteerTarget = null;

                if (_pendingShop != null)
                {
                    var shop = _pendingShop;
                    _pendingShop = null;
                    if (!_movedBeyondSlop) shop.Browse();
                    return;
                }

                if (_heldActor != null)
                {
                    _heldActor.EndDrag();
                    _heldActor = null;
                    return;
                }

                bool quick = Time.unscaledTime - _pressStartTime <= tapMaxDuration;
                if (!quick || _movedBeyondSlop) return;

                var world = ScreenToWorld(screen);

                // Bubbles claim a tap before anything else hears about it.
                var bubble = Game.Bubble.Pick(world);
                if (bubble != null) { bubble.Collect(); return; }

                Tapped?.Invoke(world);
            }
        }

        void GetPointer(out Vector2 screen, out bool pressed, out bool released)
        {
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                screen = t.position;
                pressed = t.phase == TouchPhase.Began
                       || t.phase == TouchPhase.Moved
                       || t.phase == TouchPhase.Stationary;
                released = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                return;
            }

            screen = Input.mousePosition;
            pressed = Input.GetMouseButton(0);
            released = Input.GetMouseButtonUp(0);
        }

        Vector2 ScreenToWorld(Vector2 screen)
        {
            var w = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -transform.position.z));
            return new Vector2(w.x, w.y);
        }
    }
}
