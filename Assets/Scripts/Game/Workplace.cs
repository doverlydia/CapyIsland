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
        public int MaxWorkers => maxWorkers;

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
        public Harvestable Find(NodeFamily family, Vector2 from, bool reserve, bool childrenOnly = false)
        {
            Harvestable bestChild = null, bestParent = null;
            float childDistance = float.MaxValue, parentDistance = float.MaxValue;

            foreach (var node in FindObjectsByType<Harvestable>(FindObjectsSortMode.None))
            {
                if (node.Family != family || !node.InWorld || !node.CanHarvest || node.Reserved) continue;
                if (!InBoundary(node.transform.position)) continue;

                float d = BunniSpace.WorldDistance(from, node.transform.position);
                if (node.IsParent)
                {
                    if (d < parentDistance) { parentDistance = d; bestParent = node; }
                }
                else if (d < childDistance) { childDistance = d; bestChild = node; }
            }

            var chosen = bestChild != null ? bestChild : (childrenOnly ? null : bestParent);
            if (chosen != null && reserve) chosen.Reserved = true;
            return chosen;
        }

        /// <summary>Inside the 800-unit boundary, and on land connected to this building.</summary>
        bool InBoundary(Vector2 point)
        {
            if (BunniSpace.WorldDistance(transform.position, point) > BunniRules.WorkplaceBoundary) return false;
            var land = LandMap.Instance;
            return land == null || land.SegmentOnLand(transform.position, point);
        }

        /// <summary>
        /// The nearest grown, unreserved fruit tree inside the boundary. With
        /// <paramref name="needFruit"/>, only trees with fruit on them (a hungry worker's search).
        /// </summary>
        public FruitTree FindFruitTree(Vector2 from, bool needFruit, bool reserve)
        {
            FruitTree best = null;
            float bestDistance = float.MaxValue;
            foreach (var tree in FindObjectsByType<FruitTree>(FindObjectsSortMode.None))
            {
                if (!tree.InWorld || !tree.CanHarvest || tree.Reserved) continue;
                if (needFruit && tree.FruitCount == 0) continue;
                if (!InBoundary(tree.transform.position)) continue;
                float d = BunniSpace.WorldDistance(from, tree.transform.position);
                if (d < bestDistance) { bestDistance = d; best = tree; }
            }
            if (best != null && reserve) best.Reserved = true;
            return best;
        }

        /// <summary>
        /// A hungry worker's search — one pass over flowers, fallen fruit and fruit trees with fruit:
        /// the nearest of the child flowers and fruit trees, else the nearest parent flower. Exactly
        /// one of the outputs is set, or neither.
        /// </summary>
        public void FindFood(Vector2 from, bool reserve, out Harvestable flower, out FruitTree tree)
        {
            var flowerChild = Find(NodeFamily.Flower, from, reserve: false, childrenOnly: true);
            var fruitTree = FindFruitTree(from, needFruit: true, reserve: false);

            flower = null;
            tree = null;
            if (flowerChild != null && fruitTree != null)
            {
                if (BunniSpace.WorldDistance(from, fruitTree.transform.position) < BunniSpace.WorldDistance(from, flowerChild.transform.position))
                    tree = fruitTree;
                else
                    flower = flowerChild;
            }
            else if (flowerChild != null) flower = flowerChild;
            else if (fruitTree != null) tree = fruitTree;
            else flower = Find(NodeFamily.Flower, from, reserve: false);

            if (!reserve) return;
            if (flower != null) flower.Reserved = true;
            if (tree != null) tree.Reserved = true;
        }

        /// <summary>
        /// Credits a delivered load, doubled at a Legendary building — except a felled fruit tree,
        /// which the original pays at a flat 500.
        /// </summary>
        public void Deposit(int amount, bool legendaryBonus = true)
        {
            if (amount <= 0 || ResourceStore.Instance == null) return;
            // A friend's workers fill their own stores, not your wallet.
            if (Visit.Active) return;
            int paid = legendary && legendaryBonus ? amount * 2 : amount;
            ResourceStore.Instance.RecordDelivery(accepts, paid);
            ResourcePing.Fly(accepts, paid, (Vector2)transform.position + Vector2.up * 1.4f);
        }

        void OnDrawGizmosSelected()
        {
            var r = BunniSpace.ToUnity(new Vector2(BunniRules.WorkplaceBoundary, BunniRules.WorkplaceBoundary));
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.5f);
            Gizmos.DrawWireCube(transform.position, new Vector3(r.x * 2f, r.y * 2f, 0f));
        }
    }
}
