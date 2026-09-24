using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A garden a hungry worker eats from. Planted, it grows into separate flowers; each meal
    /// shrinks one flower and then eats it away, so you can watch the garden empty. The original's
    /// gardens "feed up to N bunnies" and get eaten out ("A Bunni ate the last flower in the
    /// garden"), so scaling the workforce means scaling the food.
    ///
    /// When the last flower goes, the garden returns to a bubble like a felled forest does.
    /// </summary>
    public class FoodSource : MonoBehaviour
    {
        [Tooltip("Meals per flower. With 6 flowers and 2 bites each, a garden feeds 12 meals.")]
        [SerializeField] int bitesPerFlower = 2;

        [Tooltip("How far a worker will come to eat here.")]
        [SerializeField] float feedRadius = 10f;

        [Tooltip("Size a flower shrinks to before its last bite eats it away.")]
        [Range(0.2f, 1f)]
        [SerializeField] float minScale = 0.45f;

        int _servings;
        int _remaining;

        // Resolved lazily for the same reason as Harvestable: the flowers only exist once planted.
        readonly List<Transform> _flowers = new();
        readonly List<Vector3> _flowerFull = new();
        bool _dirty = true;

        public float FeedRadius => feedRadius;
        public bool HasFood => !_dirty ? _remaining > 0 : true;
        public int Remaining { get { Refresh(); return _remaining; } }

        /// <summary>Only a planted garden can be eaten from; one still in the trail does not count.</summary>
        public bool IsOpen
        {
            get
            {
                if (!isActiveAndEnabled) return false;
                var placeable = GetComponent<Placeable>();
                if (placeable != null && !placeable.Placed) return false;
                Refresh();
                return _remaining > 0;
            }
        }

        void OnPlacedInWorld() => _dirty = true;

        /// <summary>Bubbled again: a replanted garden is full.</summary>
        void OnReturnedToBubble()
        {
            _flowers.Clear();
            _flowerFull.Clear();
            _dirty = true;
        }

        void Refresh()
        {
            if (!_dirty) return;
            _flowers.Clear();
            _flowerFull.Clear();

            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Clump")) continue;
                _flowers.Add(child);
                var grow = child.GetComponent<GrowIn>();
                _flowerFull.Add(grow != null ? Vector3.one * grow.TargetScale : child.localScale);
            }

            // Unplanted or unscattered: nothing to count yet.
            if (_flowers.Count == 0) return;

            _servings = _flowers.Count * Mathf.Max(1, bitesPerFlower);
            _remaining = _servings;
            _dirty = false;
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

        /// <summary>The flower being eaten next — what a hungry worker walks to.</summary>
        public Vector2 EatPoint
        {
            get
            {
                Refresh();
                int i = CurrentFlower();
                var at = i >= 0 ? _flowers[i].position : transform.position;
                return (Vector2)at + new Vector2(0.6f, -0.2f);
            }
        }

        int CurrentFlower()
        {
            if (_flowers.Count == 0) return -1;
            int eaten = _servings - _remaining;
            return Mathf.Clamp(eaten / Mathf.Max(1, bitesPerFlower), 0, _flowers.Count - 1);
        }

        /// <summary>One meal: shrinks the current flower, eats it away on its last bite.</summary>
        public bool Eat()
        {
            Refresh();
            if (_remaining <= 0) return false;

            int i = CurrentFlower();
            _remaining--;

            if (i >= 0)
            {
                var flower = _flowers[i];
                var grow = flower.GetComponent<GrowIn>();
                if (grow != null) grow.enabled = false;

                int eaten = _servings - _remaining;
                int intoFlower = eaten - i * Mathf.Max(1, bitesPerFlower);
                if (intoFlower >= bitesPerFlower || _remaining <= 0)
                    flower.gameObject.SetActive(false);
                else
                    flower.localScale = _flowerFull[i] *
                        Mathf.Lerp(1f, minScale, (float)intoFlower / Mathf.Max(1, bitesPerFlower));
            }

            if (_remaining <= 0)
            {
                var placeable = GetComponent<Placeable>();
                if (placeable != null) BubbleSpawner.ReturnToBubble(placeable);
                else gameObject.SetActive(false);
            }
            return true;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.8f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, feedRadius);
        }
    }
}
