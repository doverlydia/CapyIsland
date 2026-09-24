using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A sawmill or a mine. Workers are assigned to the building, not to the resource — without a
    /// workplace in range a forest yields nothing, which is the rule the original's tutorial teaches.
    ///
    /// The workplace owns the search for nodes so every worker stationed here shares one radius and
    /// one definition of "nearby".
    /// </summary>
    public class Workplace : MonoBehaviour
    {
        [Header("What this building harvests")]
        [SerializeField] ResourceKind accepts = ResourceKind.Wood;

        [Tooltip("How far this building will send a worker to find a node.")]
        [SerializeField] float workRadius = 8f;

        [Tooltip("How many workers may be stationed here.")]
        [SerializeField] int maxWorkers = 4;

        readonly List<Worker> _workers = new();

        public ResourceKind Accepts => accepts;
        public float WorkRadius => workRadius;
        public bool HasRoom => _workers.Count < maxWorkers;

        /// <summary>
        /// True when this building is staffed but has no resource node inside its radius — the
        /// "you built the sawmill too far from the trees" case, which otherwise looks like a bug
        /// because the worker just stands there.
        /// </summary>
        public bool StaffedButIdle => _workers.Count > 0 && FindNode() == null;
        public int WorkerCount => _workers.Count;

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

        /// <summary>
        /// Nearest node in range that still has stock and matches what this building accepts.
        /// Returns null when there is nothing left to work, which parks the worker.
        /// </summary>
        public Harvestable FindNode()
        {
            Harvestable best = null;
            float bestDistance = float.MaxValue;

            foreach (var node in HarvestableRegistry.All)
            {
                if (node == null || !node.isActiveAndEnabled) continue;
                if (node.Kind != accepts || !node.HasStock) continue;

                float d = Vector2.Distance(node.transform.position, transform.position);
                if (d > workRadius || d >= bestDistance) continue;
                best = node;
                bestDistance = d;
            }
            return best;
        }

        /// <summary>Credits a completed trip. All earning funnels through here.</summary>
        public void Deposit(int amount)
        {
            if (amount <= 0 || ResourceStore.Instance == null) return;
            ResourceStore.Instance.Add(accepts, amount);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, workRadius);
        }
    }

    /// <summary>
    /// Flat list of every live node, so a workplace does not run a physics query or a FindObjects
    /// scan every time a worker needs somewhere to go.
    /// </summary>
    public static class HarvestableRegistry
    {
        static readonly List<Harvestable> _all = new();
        public static IReadOnlyList<Harvestable> All => _all;

        public static void Register(Harvestable h)
        {
            if (h != null && !_all.Contains(h)) _all.Add(h);
        }

        public static void Unregister(Harvestable h) => _all.Remove(h);

        public static void Clear() => _all.Clear();
    }
}
