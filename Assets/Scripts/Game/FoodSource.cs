using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A garden a hungry worker eats from. Holds a limited number of servings — the original's
    /// gardens "feed up to N bunnies" and get eaten out ("A Bunni ate the last flower in the
    /// garden") — so scaling the workforce means scaling the food, not planting one bed once.
    ///
    /// When the last serving goes, the garden returns to a bubble like a felled forest does.
    /// </summary>
    public class FoodSource : MonoBehaviour
    {
        [Tooltip("Meals this garden holds before it is eaten out. Small posie 1, medium 2 in the original; ours are more generous because chops are frequent.")]
        [SerializeField] int servings = 6;

        [Tooltip("How far a worker will come to eat here.")]
        [SerializeField] float feedRadius = 10f;

        [Tooltip("Smallest fraction of full size the bed shrinks to on its last serving.")]
        [Range(0.2f, 1f)]
        [SerializeField] float minScale = 0.55f;

        int _remaining;
        Vector3 _fullScale;
        Transform _sprite;

        public float FeedRadius => feedRadius;
        public bool HasFood => _remaining > 0;
        public int Remaining => _remaining;

        void Awake()
        {
            _remaining = servings;
            _sprite = transform.Find("Sprite");
            if (_sprite != null) _fullScale = _sprite.localScale;
        }

        /// <summary>Only a planted garden can be eaten from; one still in the trail does not count.</summary>
        public bool IsOpen
        {
            get
            {
                var placeable = GetComponent<Placeable>();
                return (placeable == null || placeable.Placed) && HasFood;
            }
        }

        /// <summary>Nearest open garden within reach of a point, or null.</summary>
        public static FoodSource FindNear(Vector2 point)
        {
            FoodSource best = null;
            float bestDistance = float.MaxValue;
            foreach (var f in FindObjectsByType<FoodSource>(FindObjectsSortMode.None))
            {
                if (!f.IsOpen) continue;
                float d = Vector2.Distance(f.transform.position, point);
                if (d > f.feedRadius || d >= bestDistance) continue;
                best = f;
                bestDistance = d;
            }
            return best;
        }

        /// <summary>One meal. Shrinks the bed, and bubbles it when the last flower goes.</summary>
        public bool Eat()
        {
            if (_remaining <= 0) return false;
            _remaining--;

            if (_sprite != null)
            {
                float t = servings <= 0 ? 1f : 1f - (float)_remaining / servings;
                _sprite.localScale = _fullScale * Mathf.Lerp(1f, minScale, t);
            }

            if (_remaining <= 0)
            {
                var placeable = GetComponent<Placeable>();
                if (placeable != null) BubbleSpawner.ReturnToBubble(placeable);
                else gameObject.SetActive(false);
            }
            return true;
        }

        /// <summary>Called when bubbled again: a replanted garden is full.</summary>
        void OnReturnedToBubble()
        {
            _remaining = servings;
            if (_sprite != null) _sprite.localScale = _fullScale;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.8f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, feedRadius);
        }
    }
}
