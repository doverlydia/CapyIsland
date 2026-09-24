using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Turns one carried icon into the real thing when planted. A "Medium Pine Forest" rides in the
    /// inventory as a single small tree; dropping it scatters five saplings around the spot, which
    /// grow to full size over a few seconds — the original grows them in about 4s.
    ///
    /// The scattered copies are plain sprites: the parent keeps the Harvestable, so the whole clump
    /// is one resource node rather than five independent ones.
    /// </summary>
    public class ScatterOnPlace : MonoBehaviour
    {
        [Header("Scatter")]
        [SerializeField] int count = 5;
        [SerializeField] float spreadX = 2.6f;
        [SerializeField] float spreadY = 1.6f;

        [Header("Growth")]
        [Tooltip("Seconds for a sapling to reach full size. The original takes about 4.")]
        [SerializeField] float growSeconds = 4f;

        [Tooltip("Starting fraction of full size.")]
        [SerializeField] float startScale = 0.12f;

        [Header("Look")]
        [Tooltip("Size variation between copies, so a forest does not look stamped.")]
        [Range(0f, 0.5f)]
        [SerializeField] float sizeJitter = 0.18f;

        bool _scattered;

        /// <summary>
        /// Called when the item is bubbled again. Clears the old clumps and restores the icon, so
        /// replanting grows a fresh forest rather than re-showing a stump field.
        /// </summary>
        void OnReturnedToBubble()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Clump")) DestroyImmediate(child.gameObject);
            }

            var source = transform.Find("Sprite");
            if (source != null)
            {
                var sr = source.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = true;
            }
            var shadow = transform.Find("Shadow");
            if (shadow != null) shadow.gameObject.SetActive(true);

            _scattered = false;
        }

        /// <summary>Called by <see cref="Placeable.MarkPlaced"/>.</summary>
        void OnPlacedInWorld()
        {
            if (_scattered) return;
            _scattered = true;

            var source = transform.Find("Sprite");
            if (source == null) return;
            var sourceRenderer = source.GetComponent<SpriteRenderer>();
            if (sourceRenderer == null) return;

            var shadow = transform.Find("Shadow");

            // Deterministic per position, so rebuilding the same island looks the same.
            var rng = new System.Random(Mathf.RoundToInt(transform.position.x * 73 + transform.position.y * 131));

            for (int i = 0; i < count; i++)
            {
                var offset = new Vector2(
                    (float)(rng.NextDouble() * 2 - 1) * spreadX,
                    (float)(rng.NextDouble() * 2 - 1) * spreadY);

                var clump = new GameObject("Clump" + i);
                clump.transform.SetParent(transform, false);
                clump.transform.localPosition = new Vector3(offset.x, offset.y, 0f);

                if (shadow != null)
                {
                    var sh = Instantiate(shadow.gameObject, clump.transform);
                    sh.name = "Shadow";
                    sh.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                }

                var body = new GameObject("Sprite");
                body.transform.SetParent(clump.transform, false);
                var sr = body.AddComponent<SpriteRenderer>();
                sr.sprite = sourceRenderer.sprite;
                // Lower on screen draws in front, within this clump.
                sr.sortingOrder = sourceRenderer.sortingOrder + Mathf.RoundToInt(-offset.y * 10f);

                float jitter = 1f + (float)(rng.NextDouble() * 2 - 1) * sizeJitter;
                var grow = clump.AddComponent<GrowIn>();
                grow.Begin(jitter, startScale, growSeconds);
            }

            // The original icon becomes the first tree; hide it so the clump reads as the forest.
            sourceRenderer.enabled = false;
            if (shadow != null) shadow.gameObject.SetActive(false);
        }
    }

    /// <summary>Scales an object up from a sapling to full size, with a slight overshoot.</summary>
    public class GrowIn : MonoBehaviour
    {
        float _target = 1f;
        float _from = 0.1f;
        float _duration = 4f;
        float _elapsed;

        public void Begin(float targetScale, float startScale, float seconds)
        {
            _target = targetScale;
            _from = startScale * targetScale;
            _duration = Mathf.Max(0.01f, seconds);
            _elapsed = 0f;
            transform.localScale = Vector3.one * _from;
        }

        void Update()
        {
            if (_elapsed >= _duration) { enabled = false; return; }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            // Ease out with a small overshoot near the end, so it springs rather than ramps.
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float overshoot = Mathf.Sin(t * Mathf.PI) * 0.06f;
            transform.localScale = Vector3.one * Mathf.Lerp(_from, _target, eased) * (1f + overshoot);

            if (t >= 1f) transform.localScale = Vector3.one * _target;
        }
    }
}
