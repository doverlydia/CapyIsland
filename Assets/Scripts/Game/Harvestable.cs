using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A resource node a worker chops or mines: a forest, an ore hill.
    ///
    /// Every strike pays out immediately and shrinks the node a little, so work is visible the
    /// whole way through rather than only when a trip completes. The node dies when its last
    /// strike lands, and a forest that has lost every clump hands its item back as a bubble for
    /// the player to collect and replant.
    ///
    /// Yields come from the original's `price` block: pine 3, maple 7, ironwood 15, bronze 2,
    /// silver 7, gold 20.
    /// </summary>
    public class Harvestable : MonoBehaviour
    {
        [Header("What this node gives")]
        [SerializeField] ResourceKind kind = ResourceKind.Wood;

        [Tooltip("Granted per strike. Pine 3, maple 7, ironwood 15, bronze 2, silver 7, gold 20.")]
        [SerializeField] int yieldPerTrip = 3;

        [Header("Depletion")]
        [Tooltip("How many strikes this node supports before it is used up.")]
        [SerializeField] int capacity = 24;

        [Tooltip("Smallest fraction of full size a node shrinks to before its final strike.")]
        [Range(0.05f, 0.9f)]
        [SerializeField] float minScale = 0.25f;

        [Tooltip("Seconds each shrink step eases over.")]
        [SerializeField] float shrinkSeconds = 0.25f;

        [Header("Regrowth")]
        [Tooltip("Trees regrow; ore does not (the wiki is explicit that rock never replenishes).")]
        [SerializeField] bool renewable;

        [SerializeField] float regrowSeconds = 30f;

        int _remaining;
        float _regrowTimer;

        // Per-clump shrink state, so each tree in a forest wears down on its own.
        Transform[] _clumps;
        Vector3[] _clumpFullScale;
        int _nextClump;

        float _shrinkT;
        Transform _shrinking;
        Vector3 _shrinkFrom, _shrinkTo;

        public ResourceKind Kind => kind;
        public int YieldPerTrip => yieldPerTrip;
        public bool HasStock => _remaining > 0;
        public bool Depleted => _remaining <= 0;

        /// <summary>0..1 of the node's original stock still standing.</summary>
        public float Fullness => capacity <= 0 ? 0f : (float)_remaining / capacity;

        void Awake()
        {
            _remaining = capacity;
            CacheClumps();
        }

        void OnEnable() => HarvestableRegistry.Register(this);
        void OnDisable() => HarvestableRegistry.Unregister(this);

        /// <summary>
        /// Finds the individual trees a forest scattered, so strikes can fell them one at a time.
        /// Falls back to the node's own sprite for a single-piece node like an ore hill.
        /// </summary>
        void CacheClumps()
        {
            var found = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in transform)
                if (child.name.StartsWith("Clump")) found.Add(child);

            if (found.Count == 0)
            {
                var sprite = transform.Find("Sprite");
                if (sprite != null) found.Add(sprite);
            }

            _clumps = found.ToArray();
            _clumpFullScale = new Vector3[_clumps.Length];
            for (int i = 0; i < _clumps.Length; i++) _clumpFullScale[i] = _clumps[i].localScale;
        }

        void Update()
        {
            TickShrink();

            if (!renewable || _remaining >= capacity) return;
            _regrowTimer += Time.deltaTime;
            if (_regrowTimer < regrowSeconds) return;
            _regrowTimer = 0f;
            _remaining++;
        }

        /// <summary>
        /// One strike. Pays out at once, shrinks the node, and retires it when the last strike
        /// lands. Returns 0 when there is nothing left, so the worker looks elsewhere.
        /// </summary>
        public int Strike()
        {
            if (_remaining <= 0) return 0;
            _remaining--;

            ApplyWear();

            if (_remaining <= 0) Exhaust();
            return yieldPerTrip;
        }

        /// <summary>
        /// Shrinks whichever clump is currently being worked; when it is small enough it vanishes
        /// and the next one takes over, so a forest visibly thins tree by tree.
        /// </summary>
        void ApplyWear()
        {
            if (_clumps == null || _clumps.Length == 0) return;

            // How many strikes each clump is worth.
            int perClump = Mathf.Max(1, capacity / _clumps.Length);
            int used = capacity - _remaining;
            int clumpIndex = Mathf.Min(_clumps.Length - 1, used / perClump);

            // Moving on to a new clump: retire the finished one outright.
            if (clumpIndex > _nextClump)
            {
                for (int i = _nextClump; i < clumpIndex && i < _clumps.Length; i++)
                    if (_clumps[i] != null) _clumps[i].gameObject.SetActive(false);
                _nextClump = clumpIndex;
            }

            var target = _clumps[Mathf.Clamp(clumpIndex, 0, _clumps.Length - 1)];
            if (target == null) return;

            int intoClump = used - clumpIndex * perClump;
            float wear = Mathf.Clamp01((float)intoClump / perClump);
            float factor = Mathf.Lerp(1f, minScale, wear);

            _shrinking = target;
            _shrinkFrom = target.localScale;
            _shrinkTo = _clumpFullScale[Mathf.Clamp(clumpIndex, 0, _clumpFullScale.Length - 1)] * factor;
            _shrinkT = 0f;
        }

        void TickShrink()
        {
            if (_shrinking == null) return;
            _shrinkT += Time.deltaTime / Mathf.Max(0.01f, shrinkSeconds);
            float t = Mathf.Clamp01(_shrinkT);
            // Ease out, so each strike lands with a visible settle.
            _shrinking.localScale = Vector3.Lerp(_shrinkFrom, _shrinkTo, 1f - Mathf.Pow(1f - t, 3f));
            if (t >= 1f) _shrinking = null;
        }

        /// <summary>
        /// Used up. A renewable node just stops until it regrows; a felled forest hands its item
        /// back to the world as a bubble, ready to be collected and replanted.
        /// </summary>
        void Exhaust()
        {
            if (renewable) return;

            var placeable = GetComponent<Placeable>();
            if (placeable != null) BubbleSpawner.ReturnToBubble(placeable);
            else gameObject.SetActive(false);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = kind == ResourceKind.Wood ? Color.green : new Color(1f, 0.85f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 0.6f);
        }
    }
}
