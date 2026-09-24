using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Keeps a moving object correctly layered in the 3/4 view: things lower on screen draw in
    /// front. Static props get their order once at build time, but anything that walks has to
    /// re-sort as it moves, or it will slide through trees instead of behind them.
    ///
    /// The base constant matches IslandBuilder.PropSortBase so moving and static objects share one
    /// ordering space.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class YSorter : MonoBehaviour
    {
        public const int PropSortBase = 3000;

        [Tooltip("Renderers to sort. Left empty, every SpriteRenderer under this object is used.")]
        [SerializeField] SpriteRenderer[] renderers;

        [Tooltip("Drawn just behind the body, so a shadow never covers its own owner.")]
        [SerializeField] SpriteRenderer shadowRenderer;

        float _lastY = float.NaN;

        void Awake()
        {
            if (renderers == null || renderers.Length == 0)
            {
                var found = GetComponentsInChildren<SpriteRenderer>(true);
                var list = new System.Collections.Generic.List<SpriteRenderer>();
                foreach (var r in found)
                {
                    if (r.transform.name == "Shadow") { shadowRenderer = r; continue; }
                    list.Add(r);
                }
                renderers = list.ToArray();
            }
        }

        void LateUpdate()
        {
            float y = transform.position.y;
            // Sorting only changes in whole steps, so skip the work when nothing moved much.
            if (!float.IsNaN(_lastY) && Mathf.Abs(y - _lastY) < 0.005f) return;
            _lastY = y;

            int order = PropSortBase + Mathf.RoundToInt(-y * 100f);
            foreach (var r in renderers)
                if (r != null) r.sortingOrder = order;
            if (shadowRenderer != null) shadowRenderer.sortingOrder = order - 1;
        }
    }
}
