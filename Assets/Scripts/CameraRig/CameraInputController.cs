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
    /// * press on the ground and hold — the king walks toward your finger, tracking it as it moves;
    ///   or, with the joystick on (Settings), a stick appears under the thumb and steers him
    /// * quick tap — a bubble or shop takes it; on bare ground the king walks to that spot
    ///
    /// The joystick is a floating one: its centre is wherever the thumb landed, shown on screen,
    /// and it is dragged along once the thumb pulls past its rim, so it never drifts out of reach.
    ///
    /// The camera follows the king, so there is no separate pan gesture to conflict with any of it.
    /// To carry something past the edge of the screen, drag it to the edge: the king walks that way
    /// (faster the nearer the edge), the camera follows, and the item stays under your finger.
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

        [Header("Edge walk")]
        [Tooltip("How near a screen edge, as a fraction of the shorter side, a dragged item makes the king walk.")]
        [SerializeField] float edgeBand = 0.13f;

        /// <summary>Raised on a quick tap that did not land on an item.</summary>
        public event Action<Vector2> Tapped;

        /// <summary>World point the player is pointing at, or null when not steering.</summary>
        public Vector2? SteerTarget { get; private set; }

        /// <summary>True while the player is dragging an item or pinching.</summary>
        public bool IsManipulating => _heldActor != null || _pinching;

        /// <summary>While a held thing is being let go: true if it was pressed and released without moving.</summary>
        public static bool ReleaseWasTap { get; private set; }

        public float SteerDeadZone => steerDeadZone;

        ResponsiveCameraRig _rig;
        Camera _camera;

        bool _pressing;
        Vector2 _pressStartScreen;
        float _pressStartTime;
        bool _movedBeyondSlop;
        Game.DraggableActor _heldActor;
        Game.ITappable _pendingTap;

        bool _ignorePress;
        bool _pinching;

        // The joystick: where its centre is, and whether it has shown yet (not for a mere tap).
        bool _sticking, _stickShown;
        Vector2 _stickCentre;
        Game.PlayerAvatar _king;

        /// <summary>The joystick's reach, in pixels: a comfortable thumb's travel.</summary>
        static float StickRadius => Mathf.Min(Screen.width, Screen.height) * 0.11f;

        /// <summary>
        /// Seconds spent steering with the finger out near the screen edge (the reach the joystick
        /// and tapping save you). A one-time tip mentions them once this adds up.
        /// </summary>
        public static float FarSteerSeconds { get; private set; }
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
                _pendingTap = null;
                SteerTarget = null;
                EndStick();
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
                EndStick();
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

                // A dialog is up, or the finger is on the HUD: the world does not hear this press
                // (the original pauses map input while a menu is open).
                _ignorePress = ModalOpen || PointerOverUI();
                if (_ignorePress) { SteerTarget = null; return; }

                // Any touch on the island takes over from walking by himself.
                Game.PlayerAvatar.StopAutoWalk();
                _movedBeyondSlop = false;
                _pressStartScreen = screen;
                _pressStartTime = Time.unscaledTime;

                var world = ScreenToWorld(screen);

                // Carried items first: the trail rides right behind the king and must stay grabbable.
                // A bubble under the finger wins over anything behind it, unless a carried item is
                // the nearer of the two.
                var carried = Game.DraggableActor.Pick(world);
                var bubble = Game.Bubble.Pick(world);
                if (bubble != null && (carried == null ||
                    Vector2.Distance(bubble.transform.position, world) < Vector2.Distance(carried.GrabCentre, world)))
                {
                    _pendingTap = bubble;
                    SteerTarget = null;
                    return;
                }
                if (carried != null)
                {
                    _heldActor = carried;
                    _heldActor.BeginDrag();
                    SteerTarget = null;
                    return;
                }

                // Shop displays, fruit trees, quest givers: a tap target claims the press, so
                // pressing a display never picks up the shop behind it.
                _pendingTap = Game.TapTargets.Pick(world);
                if (_pendingTap != null) { SteerTarget = null; return; }

                // Joystick: its centre is where the thumb landed; it shows once the thumb moves.
                if (Game.Controls.Joystick)
                {
                    _sticking = true;
                    _stickShown = false;
                    _stickCentre = screen;
                    SteerTarget = null;
                    return;
                }

                // Ground press: steer at once, so holding moves you without waiting for a drag.
                SteerTarget = world;
                return;
            }

            // --- press continues ---
            if (pressed)
            {
                if (_ignorePress) return;
                if (!_movedBeyondSlop && Vector2.Distance(screen, _pressStartScreen) > tapSlopPixels)
                    _movedBeyondSlop = true;

                if (_pendingTap != null) return;   // holding a tap target does nothing

                var world = ScreenToWorld(screen);
                if (_heldActor != null)
                {
                    _heldActor.DragTo(world);
                    SteerTarget = EdgeWalk(screen);
                }
                else if (_sticking) SteerTarget = Stick(screen);
                else
                {
                    SteerTarget = world;   // track the finger
                    if (NearEdge(screen)) FarSteerSeconds += Time.unscaledDeltaTime;
                }
                return;
            }

            // --- press ends ---
            if (released && _pressing)
            {
                _pressing = false;
                SteerTarget = null;
                EndStick();
                if (_ignorePress) { _ignorePress = false; return; }

                if (_pendingTap != null)
                {
                    var tapped = _pendingTap;
                    _pendingTap = null;
                    if (!_movedBeyondSlop) tapped.Tap();
                    return;
                }

                if (_heldActor != null)
                {
                    // Something held and let go without moving: a tap, as far as it cares (a bottle opens its note).
                    ReleaseWasTap = !_movedBeyondSlop && Time.unscaledTime - _pressStartTime <= tapMaxDuration;
                    _heldActor.EndDrag();
                    _heldActor = null;
                    ReleaseWasTap = false;
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

        /// <summary>
        /// While an item is held near a screen edge, a steer target off that way, so the king walks
        /// toward the edge and the world scrolls under the item (the map screen does the same).
        /// Null when the finger is clear of every edge, or when it is the king himself being held.
        /// </summary>
        Vector2? EdgeWalk(Vector2 screen)
        {
            if (_heldActor == null || _heldActor.GetComponent<Game.PlayerAvatar>() != null) return null;
            var king = FindFirstObjectByType<Game.PlayerAvatar>();
            if (king == null) return null;

            float band = Mathf.Min(Screen.width, Screen.height) * edgeBand;
            float Push(float at, float size) =>
                at < band ? -Mathf.Clamp01(1f - at / band) : at > size - band ? Mathf.Clamp01(1f - (size - at) / band) : 0f;
            var push = new Vector2(Push(screen.x, Screen.width), Push(screen.y, Screen.height));
            if (push.sqrMagnitude < 1e-4f) return null;

            // The king throttles over three dead zones from his target: put the target at a
            // distance that turns the depth into the edge into his pace.
            float depth = Mathf.Clamp01(push.magnitude);
            float distance = steerDeadZone * (1.05f + 1.95f * Mathf.Max(0.35f, depth));
            return (Vector2)king.transform.position + push.normalized * distance;
        }

        /// <summary>
        /// The joystick's steer target: off from the king in the stick's direction, at a distance
        /// that turns how far the knob is pulled into his pace (he throttles over three dead zones).
        /// </summary>
        Vector2? Stick(Vector2 screen)
        {
            float radius = StickRadius;
            var pull = screen - _stickCentre;
            if (pull.magnitude > radius)
            {
                _stickCentre = screen - pull.normalized * radius;   // drag the ring along
                pull = screen - _stickCentre;
            }
            if (!_movedBeyondSlop) return null;   // still might be a tap

            if (!_stickShown) { UI.FloatingStick.Show(_stickCentre, radius); _stickShown = true; }
            UI.FloatingStick.Move(_stickCentre, screen);

            if (_king == null) _king = FindFirstObjectByType<Game.PlayerAvatar>();
            if (_king == null) return null;
            float depth = Mathf.Clamp01(pull.magnitude / radius);
            if (depth < 0.15f) return null;
            float distance = steerDeadZone * (1.05f + 1.95f * Mathf.InverseLerp(0.15f, 1f, depth));
            return (Vector2)_king.transform.position + pull.normalized * distance;
        }

        void EndStick()
        {
            if (!_sticking) return;
            _sticking = _stickShown = false;
            UI.FloatingStick.Hide();
        }

        bool NearEdge(Vector2 screen)
        {
            float band = Mathf.Min(Screen.width, Screen.height) * edgeBand * 1.6f;
            return screen.x < band || screen.x > Screen.width - band || screen.y < band || screen.y > Screen.height - band;
        }

        static bool ModalOpen => UI.TextReader.IsOpen || UI.HudTour.IsOpen || UI.SpeechDialog.IsOpen || UI.CardPopup.IsShowing || UI.PurchaseDialog.IsOpen || UI.UiScreen.AnyOpen;

        static bool PointerOverUI()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return false;
            if (Input.touchCount > 0) return es.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            return es.IsPointerOverGameObject();
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
