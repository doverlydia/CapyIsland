using CapyIsland.Core;
using UnityEngine;

namespace CapyIsland.CameraRig
{
    /// <summary>
    /// Orthographic camera that shows the same amount of world no matter the aspect ratio, so a
    /// mid-session rotation never hands one orientation an advantage.
    ///
    /// Visible width  = 2 * orthographicSize * aspect
    /// Visible height = 2 * orthographicSize
    /// Visible area   = 4 * orthographicSize^2 * aspect
    ///
    /// Holding that area constant gives orthographicSize = reference * sqrt(referenceAspect / aspect).
    /// Portrait therefore zooms out vertically exactly as much as it loses horizontally.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-50)]
    public class ResponsiveCameraRig : MonoBehaviour
    {
        [Header("Reference framing")]
        [Tooltip("Orthographic size that looks right at the reference aspect below.")]
        [SerializeField] float referenceOrthographicSize = 6f;

        [Tooltip("Aspect the reference size was authored against (16:9 landscape = 1.777).")]
        [SerializeField] float referenceAspect = 16f / 9f;

        [Header("Zoom")]
        [Tooltip("Multiplier on the computed size. 1 = the reference framing.")]
        [SerializeField] float zoom = 1f;
        [SerializeField] float minZoom = 0.5f;
        [SerializeField] float maxZoom = 2f;

        [Header("World bounds")]
        [Tooltip("The island's extent. The camera never shows anything outside this.")]
        [SerializeField] Bounds worldBounds = new Bounds(Vector3.zero, new Vector3(40f, 40f, 0f));
        [SerializeField] bool clampToBounds = true;

        Camera _camera;

        /// <summary>Current zoom, clamped. Setting it re-frames immediately.</summary>
        public float Zoom
        {
            get => zoom;
            set
            {
                zoom = Mathf.Clamp(value, minZoom, maxZoom);
                Apply();
            }
        }

        public Bounds WorldBounds
        {
            get => worldBounds;
            set { worldBounds = value; Apply(); }
        }

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
        }

        void OnEnable()
        {
            ScreenMetrics.Changed += Apply;
            Apply();
        }

        void OnDisable()
        {
            ScreenMetrics.Changed -= Apply;
        }

        /// <summary>Re-frames the camera for the current screen. Safe to call every frame.</summary>
        public void Apply()
        {
            if (_camera == null) _camera = GetComponent<Camera>();

            _camera.orthographicSize = SizeForAspect(ScreenMetrics.Aspect) * zoom;

            if (clampToBounds)
                transform.position = ClampPosition(transform.position);
        }

        /// <summary>Orthographic size that keeps the visible area constant at the given aspect.</summary>
        public float SizeForAspect(float aspect)
        {
            aspect = Mathf.Max(0.0001f, aspect);
            return referenceOrthographicSize * Mathf.Sqrt(referenceAspect / aspect);
        }

        /// <summary>Moves the camera, keeping it inside the world bounds.</summary>
        public void MoveTo(Vector3 worldPosition)
        {
            worldPosition.z = transform.position.z;
            transform.position = clampToBounds ? ClampPosition(worldPosition) : worldPosition;
        }

        /// <summary>Half the visible world size, in world units.</summary>
        public Vector2 ViewExtents()
        {
            float halfHeight = _camera != null ? _camera.orthographicSize : SizeForAspect(ScreenMetrics.Aspect) * zoom;
            return new Vector2(halfHeight * ScreenMetrics.Aspect, halfHeight);
        }

        Vector3 ClampPosition(Vector3 position)
        {
            Vector2 extents = ViewExtents();

            // When the view is wider than the world on an axis, centre on that axis instead of
            // clamping — otherwise the camera jitters against two opposing limits.
            float halfWorldX = worldBounds.extents.x;
            float halfWorldY = worldBounds.extents.y;

            position.x = extents.x >= halfWorldX
                ? worldBounds.center.x
                : Mathf.Clamp(position.x, worldBounds.center.x - halfWorldX + extents.x, worldBounds.center.x + halfWorldX - extents.x);

            position.y = extents.y >= halfWorldY
                ? worldBounds.center.y
                : Mathf.Clamp(position.y, worldBounds.center.y - halfWorldY + extents.y, worldBounds.center.y + halfWorldY - extents.y);

            return position;
        }

        void OnValidate()
        {
            minZoom = Mathf.Max(0.01f, minZoom);
            maxZoom = Mathf.Max(minZoom, maxZoom);
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            referenceOrthographicSize = Mathf.Max(0.01f, referenceOrthographicSize);
            referenceAspect = Mathf.Max(0.01f, referenceAspect);

            if (Application.isPlaying) Apply();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(worldBounds.center, new Vector3(worldBounds.size.x, worldBounds.size.y, 0.01f));
        }
    }
}
