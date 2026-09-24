using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A resource node a worker can harvest from: a tree, an ore hill.
    ///
    /// Yields come from the original's `price` block, which the play-test confirmed is the amount
    /// credited per trip, not a sale value: pine 3, maple 7, ironwood 15, bronze 2, silver 7, gold 20.
    ///
    /// Ore is finite. The wiki is explicit that rocks do not replenish like trees, and the mining
    /// tutorial ends when no rock is left, so <see cref="renewable"/> is off for ore.
    /// </summary>
    public class Harvestable : MonoBehaviour
    {
        [Header("What this node gives")]
        [SerializeField] ResourceKind kind = ResourceKind.Wood;
        [Tooltip("Credited per completed trip. Pine 3, maple 7, ironwood 15, bronze 2, silver 7, gold 20.")]
        [SerializeField] int yieldPerTrip = 3;

        [Header("Depletion")]
        [Tooltip("Trips this node supports before it is used up. Trees regrow, ore does not.")]
        [SerializeField] int capacity = 12;
        [SerializeField] bool renewable = true;
        [Tooltip("Seconds to grow one trip's worth back. Derived from the original's seed rates.")]
        [SerializeField] float regrowSeconds = 30f;

        float _regrowTimer;
        int _remaining;

        public ResourceKind Kind => kind;
        public int YieldPerTrip => yieldPerTrip;
        public bool Depleted => _remaining <= 0;

        /// <summary>True while this node still has something to give.</summary>
        public bool HasStock => _remaining > 0;

        void Awake() => _remaining = capacity;

        void OnEnable() => HarvestableRegistry.Register(this);

        void OnDisable() => HarvestableRegistry.Unregister(this);

        void Update()
        {
            if (!renewable || _remaining >= capacity) return;

            _regrowTimer += Time.deltaTime;
            if (_regrowTimer < regrowSeconds) return;
            _regrowTimer = 0f;
            _remaining++;
        }

        /// <summary>
        /// Takes one trip's worth. Returns the amount actually granted, or 0 when exhausted, so a
        /// worker can decide to look elsewhere.
        /// </summary>
        public int Take()
        {
            if (_remaining <= 0) return 0;
            _remaining--;
            if (_remaining <= 0 && !renewable) OnExhausted();
            return yieldPerTrip;
        }

        /// <summary>Non-renewable nodes disappear once emptied, which is what ends the mining tutorial.</summary>
        void OnExhausted()
        {
            gameObject.SetActive(false);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = kind == ResourceKind.Wood ? Color.green : new Color(1f, 0.85f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 0.6f);
        }
    }
}
