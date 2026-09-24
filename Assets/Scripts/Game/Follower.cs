using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Anything you have bought but not yet placed. It trails the king in a queue — the trail *is*
    /// the inventory, so a tree, a sawmill, a garden and a worker all behave identically while
    /// carried. You drag one out of the trail to place it (or, for a worker, to post it to a
    /// station); until then it walks behind you.
    ///
    /// Each follower trails the one ahead rather than the king directly, so a long inventory
    /// strings out in a line instead of stacking on one spot.
    /// </summary>
    public class Follower : MonoBehaviour
    {
        [Tooltip("How far behind the king, or the follower ahead, this one trails.")]
        [SerializeField] float spacing = 1.1f;

        [Tooltip("Travel pace in world units per second.")]
        [SerializeField] float moveSpeed = 4.4f;

        [Tooltip("Beyond this distance it hurries to catch up, so the tail does not get stranded.")]
        [SerializeField] float catchUpDistance = 3.5f;

        [Tooltip("Multiplier applied while catching up.")]
        [SerializeField] float catchUpBoost = 2.2f;

        SpriteRenderer _renderer;

        /// <summary>False while the item is placed, working, or held by the player.</summary>
        public bool Carried { get; private set; } = true;

        void Awake() => _renderer = GetComponentInChildren<SpriteRenderer>();

        /// <summary>Put back into the trail behind the king.</summary>
        public void PickUpIntoInventory() => Carried = true;

        /// <summary>Taken out of the trail: placed on the map, or posted to a station.</summary>
        public void PlaceInWorld() => Carried = false;

        void Update()
        {
            if (!Carried) return;

            var leader = LeaderTransform();
            if (leader == null) return;

            Vector3 to = leader.position - transform.position;
            float distance = to.magnitude;
            if (distance <= spacing) return;

            float pace = moveSpeed * (distance > catchUpDistance ? catchUpBoost : 1f);
            Vector3 step = to / distance * (pace * Time.deltaTime);

            // Never overshoot into the leader's personal space.
            float travel = Mathf.Min(step.magnitude, distance - spacing);
            transform.position += (Vector3)((Vector2)to.normalized * travel);

            Face(to.x);
        }

        /// <summary>
        /// The king, or whichever carried follower sits between us and him. Choosing the nearest
        /// follower that is *closer to the king than we are* gives the queue a direction, so two
        /// items can never end up following each other in a loop.
        /// </summary>
        Transform LeaderTransform()
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king == null) return null;

            float myDistanceToKing = Vector3.Distance(transform.position, king.transform.position);
            Transform best = king.transform;
            float bestGap = myDistanceToKing;

            foreach (var other in FindObjectsByType<Follower>(FindObjectsSortMode.None))
            {
                if (other == this || !other.Carried) continue;

                float theirDistanceToKing = Vector3.Distance(other.transform.position, king.transform.position);
                if (theirDistanceToKing >= myDistanceToKing) continue;

                float gap = Vector3.Distance(other.transform.position, transform.position);
                if (gap >= bestGap) continue;
                best = other.transform;
                bestGap = gap;
            }
            return best;
        }

        void Face(float dx)
        {
            if (_renderer == null || Mathf.Abs(dx) < 0.01f) return;
            var t = _renderer.transform;
            var scale = t.localScale;
            scale.x = Mathf.Abs(scale.x) * (dx < 0f ? -1f : 1f);
            t.localScale = scale;
        }
    }
}
