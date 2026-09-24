using System;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A glowing bubble on the ground holding an item. Tap it and the item joins your inventory
    /// trail. These are how the island is seeded: you start with nothing, and every early item —
    /// forest, sawmill, capy house — arrives in one.
    ///
    /// In the original these are survivors Grandpa Angel sealed away before the disaster, which is
    /// why they are scattered around rather than bought.
    /// </summary>
    public class Bubble : MonoBehaviour
    {
        [Header("Contents")]
        [Tooltip("Prefab-like source object, inactive in the scene, handed over when collected.")]
        [SerializeField] GameObject contents;

        [Header("Look")]
        [SerializeField] float bobHeight = 0.18f;
        [SerializeField] float bobRate = 1.1f;
        [Tooltip("The white arrow that bobs above an uncollected bubble.")]
        [SerializeField] Transform arrow;
        [SerializeField] float arrowBobHeight = 0.3f;
        [SerializeField] float arrowBobRate = 2.2f;

        [Header("Interaction")]
        [SerializeField] float tapRadius = 1.2f;

        /// <summary>Raised when collected, with the item that came out.</summary>
        public static event Action<GameObject, Bubble> Collected;

        Transform _body;
        Vector3 _bodyRest;
        Vector3 _arrowRest;
        bool _taken;

        public bool Taken => _taken;
        public float TapRadius => tapRadius;
        public GameObject Contents => contents;

        void Awake()
        {
            var sprite = transform.Find("Sprite");
            _body = sprite != null ? sprite : transform;
            _bodyRest = _body.localPosition;
            if (arrow != null) _arrowRest = arrow.localPosition;
        }

        void Update()
        {
            if (_taken) return;

            float t = Time.time;
            if (_body != null)
                _body.localPosition = _bodyRest + new Vector3(0f, Mathf.Sin(t * bobRate * Mathf.PI * 2f) * bobHeight, 0f);

            // The arrow bobs faster and out of phase, so the pair reads as "look here".
            if (arrow != null)
                arrow.localPosition = _arrowRest + new Vector3(0f, Mathf.Abs(Mathf.Sin(t * arrowBobRate * Mathf.PI)) * arrowBobHeight, 0f);
        }

        /// <summary>Nearest uncollected bubble under a world point, or null.</summary>
        public static Bubble Pick(Vector2 worldPoint)
        {
            Bubble best = null;
            float bestDistance = float.MaxValue;
            foreach (var b in FindObjectsByType<Bubble>(FindObjectsSortMode.None))
            {
                if (b == null || b._taken || !b.isActiveAndEnabled) continue;
                float d = Vector2.Distance(b.transform.position, worldPoint);
                if (d > b.tapRadius || d >= bestDistance) continue;
                best = b;
                bestDistance = d;
            }
            return best;
        }

        /// <summary>Pops the bubble and hands over its contents, already in the inventory trail.</summary>
        public GameObject Collect()
        {
            if (_taken) return null;
            _taken = true;

            var item = contents;
            if (item != null)
            {
                item.transform.position = transform.position;
                item.SetActive(true);

                // Straight into the trail: picked-up items follow you until you place them.
                var placeable = item.GetComponent<Placeable>();
                if (placeable != null) placeable.MarkCarried();
                else
                {
                    var follower = item.GetComponent<Follower>();
                    if (follower != null) follower.PickUpIntoInventory();
                }
            }

            Collected?.Invoke(item, this);
            gameObject.SetActive(false);
            return item;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 1f, 0.9f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, tapRadius);
        }
    }
}
