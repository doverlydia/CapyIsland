using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A sawmill or a mine. Workers are posted to the building, not to a tree, and only work nodes
    /// inside its boundary — a fixed 800-unit circle in the original (MillItem.mBoundry), not the
    /// store item's outer radius. Hungry workers also only look for food inside it, so a garden must
    /// be planted within reach of the mill whose workers it is meant to feed.
    /// </summary>
    public class Workplace : MonoBehaviour
    {
        [Header("What this building harvests")]
        [SerializeField] ResourceKind accepts = ResourceKind.Wood;

        [Tooltip("A Legendary Mill or Mine doubles the value of everything delivered to it.")]
        [SerializeField] bool legendary;

        [Tooltip("How many workers may be stationed here.")]
        [SerializeField] int maxWorkers = 4;

        readonly List<Worker> _workers = new();

        public ResourceKind Accepts => accepts;
        public bool HasRoom => _workers.Count < maxWorkers;
        public int WorkerCount => _workers.Count;

        /// <summary>The family of node this building's workers harvest.</summary>
        public NodeFamily WorkFamily => accepts == ResourceKind.Wood ? NodeFamily.Tree : NodeFamily.Rock;

        public bool TryAssign(Worker worker)
        {
            if (worker == null || !HasRoom || _workers.Contains(worker)) return false;
            _workers.Add(worker);
            worker.AssignTo(this);
            return true;
        }

        public void Release(Worker worker)
        {
            if (worker == null) return;
            _workers.Remove(worker);
        }

        /// <summary>True when this building has anything of its kind in reach. Reserves nothing.</summary>
        public bool HasWorkInReach() => Find(WorkFamily, transform.position, reserve: false) != null;

        /// <summary>
        /// The original's FindItem: among grown, unreserved nodes of the family whose centre is
        /// inside this building's boundary, the nearest **child**; only if there is no child at all,
        /// the nearest **parent**. The winner is reserved when <paramref name="reserve"/> is set.
        /// </summary>
        public Harvestable Find(NodeFamily family, Vector2 from, bool reserve)
        {
            Harvestable bestChild = null, bestParent = null;
            float childDistance = float.MaxValue, parentDistance = float.MaxValue;

            foreach (var node in FindObjectsByType<Harvestable>(FindObjectsSortMode.None))
            {
                if (node.Family != family || !node.InWorld || !node.CanHarvest || node.Reserved) continue;
                if (BunniSpace.WorldDistance(transform.position, node.transform.position) > BunniRules.WorkplaceBoundary) continue;

                float d = BunniSpace.WorldDistance(from, node.transform.position);
                if (node.IsParent)
                {
                    if (d < parentDistance) { parentDistance = d; bestParent = node; }
                }
                else if (d < childDistance) { childDistance = d; bestChild = node; }
            }

            var chosen = bestChild != null ? bestChild : bestParent;
            if (chosen != null && reserve) chosen.Reserved = true;
            return chosen;
        }

        /// <summary>Credits a delivered load, doubled at a Legendary building.</summary>
        public void Deposit(int amount)
        {
            if (amount <= 0 || ResourceStore.Instance == null) return;
            ResourceStore.Instance.Add(accepts, legendary ? amount * 2 : amount);
        }

        void OnDrawGizmosSelected()
        {
            var r = BunniSpace.ToUnity(new Vector2(BunniRules.WorkplaceBoundary, BunniRules.WorkplaceBoundary));
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.5f);
            Gizmos.DrawWireCube(transform.position, new Vector3(r.x * 2f, r.y * 2f, 0f));
        }
    }
}
