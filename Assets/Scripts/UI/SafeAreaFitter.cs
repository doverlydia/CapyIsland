using CapyIsland.Core;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// Insets a RectTransform to the device safe area, re-applying whenever the screen changes.
    /// Put this on a full-screen child of the Canvas and anchor HUD elements inside it, so
    /// notches, punch-holes and gesture bars never cover a button after a rotation.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DefaultExecutionOrder(-40)]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("Ignore the safe area horizontally — useful for a bar that should span the full width.")]
        [SerializeField] bool ignoreHorizontal;

        [Tooltip("Ignore the safe area vertically.")]
        [SerializeField] bool ignoreVertical;

        RectTransform _rect;

        void Awake() => _rect = GetComponent<RectTransform>();

        void OnEnable()
        {
            ScreenMetrics.Changed += Apply;
            Apply();
        }

        void OnDisable()
        {
            ScreenMetrics.Changed -= Apply;
        }

        public void Apply()
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();

            Rect safe = Screen.safeArea;
            int w = Mathf.Max(1, Screen.width);
            int h = Mathf.Max(1, Screen.height);

            Vector2 min = new Vector2(safe.xMin / w, safe.yMin / h);
            Vector2 max = new Vector2(safe.xMax / w, safe.yMax / h);

            if (ignoreHorizontal) { min.x = 0f; max.x = 1f; }
            if (ignoreVertical)   { min.y = 0f; max.y = 1f; }

            // Guard against a degenerate safe area (some editors/simulators report zero).
            if (max.x <= min.x || max.y <= min.y) return;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
