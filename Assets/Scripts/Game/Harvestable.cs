using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A resource node a worker chops or mines: a forest, an ore hill.
    ///
    /// Every strike pays out immediately and wears down one tree, so work is visible the whole way
    /// through. A forest is felled tree by tree; when the last one goes, the item returns to the
    /// world as a bubble for the player to collect and replant.
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
        [SerializeField] int capacity = 30;

        [Tooltip("Size a tree has shrunk to just before its final strike fells it.")]
        [Range(0.05f, 0.9f)]
        [SerializeField] float minScale = 0.3f;

        [Tooltip("Seconds each shrink step eases over.")]
        [SerializeField] float shrinkSeconds = 0.25f;

        [Header("Regrowth")]
        [Tooltip("Trees regrow; ore does not (the wiki is explicit that rock never replenishes).")]
        [SerializeField] bool renewable;

        [SerializeField] float regrowSeconds = 30f;

        int _remaining;
        float _regrowTimer;

        // The individual trees. Resolved lazily: a forest's trees do not exist until it is planted
        // (ScatterOnPlace creates them then), so caching them in Awake found only the hidden icon
        // and "shrank" something nobody could see.
        readonly List<Transform> _clumps = new();
        readonly List<Vector3> _clumpFull = new();
        bool _clumpsDirty = true;

        float _shrinkT = 1f;
        Transform _shrinking;
        Vector3 _shrinkFrom, _shrinkTo;

        public ResourceKind Kind => kind;
        public int YieldPerTrip => yieldPerTrip;
        public bool HasStock => _remaining > 0;
        public bool Depleted => _remaining <= 0;

        /// <summary>0..1 of the node's original stock still standing.</summary>
        public float Fullness => capacity <= 0 ? 0f : (float)_remaining / capacity;

        /// <summary>
        /// Only a planted node can be worked. A forest still riding in the inventory trail is an
        /// icon, not a forest, and a worker must not wander over to chop it.
        /// </summary>
        public bool IsWorkable
        {
            get
            {
                if (!HasStock || !isActiveAndEnabled) return false;
                var placeable = GetComponent<Placeable>();
                return placeable == null || placeable.Placed;
            }
        }

        /// <summary>The tree currently being felled — what a worker walks to and what shakes when struck.</summary>
        public Transform CurrentVisual
        {
            get
            {
                RefreshClumps();
                int i = CurrentClumpIndex();
                return i >= 0 && i < _clumps.Count ? _clumps[i] : transform;
            }
        }

        /// <summary>Where a worker stands to strike the current tree: beside it, not inside it.</summary>
        public Vector2 WorkPoint => (Vector2)CurrentVisual.position + new Vector2(0.75f, -0.25f);

        void Awake() => _remaining = capacity;

        void OnEnable() => HarvestableRegistry.Register(this);
        void OnDisable() => HarvestableRegistry.Unregister(this);

        /// <summary>Planted: the trees are about to exist, so look them up again before the first strike.</summary>
        void OnPlacedInWorld() => _clumpsDirty = true;

        /// <summary>
        /// Bubbled again. Restock completely — without this a replanted forest kept its stock of
        /// zero, so workers saw an empty node and never went back to it.
        /// </summary>
        void OnReturnedToBubble()
        {
            _remaining = capacity;
            _shrinking = null;
            _clumps.Clear();
            _clumpFull.Clear();
            _clumpsDirty = true;
        }

        void RefreshClumps()
        {
            if (!_clumpsDirty) return;
            _clumps.Clear();
            _clumpFull.Clear();

            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Clump")) continue;
                _clumps.Add(child);
                // A sapling may still be growing; its real size is where GrowIn is heading.
                var grow = child.GetComponent<GrowIn>();
                _clumpFull.Add(grow != null ? Vector3.one * grow.TargetScale : child.localScale);
            }

            if (_clumps.Count == 0)
            {
                // A single-piece node, like an ore hill: wear the sprite itself.
                var sprite = transform.Find("Sprite");
                if (sprite != null) { _clumps.Add(sprite); _clumpFull.Add(sprite.localScale); }
            }

            // Only settle once the trees are really there, so a strike before planting finishes
            // does not lock in the wrong set.
            _clumpsDirty = _clumps.Count == 0;
        }

        int StrikesPerClump => Mathf.Max(1, capacity / Mathf.Max(1, _clumps.Count));

        int CurrentClumpIndex()
        {
            if (_clumps.Count == 0) return -1;
            int used = capacity - _remaining;
            return Mathf.Clamp(used / StrikesPerClump, 0, _clumps.Count - 1);
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
        /// One strike. Pays out at once, wears the current tree down a step, fells it when its
        /// share of strikes is spent, and retires the whole node on the last one.
        /// </summary>
        public int Strike()
        {
            if (_remaining <= 0) return 0;
            RefreshClumps();

            int before = CurrentClumpIndex();
            _remaining--;

            if (before >= 0)
            {
                var tree = _clumps[before];
                var grow = tree.GetComponent<GrowIn>();
                if (grow != null) grow.enabled = false;   // growth must not overwrite the wear

                int used = capacity - _remaining;
                int intoTree = used - before * StrikesPerClump;
                bool felled = intoTree >= StrikesPerClump || _remaining <= 0;

                if (felled)
                {
                    tree.gameObject.SetActive(false);
                    if (_shrinking == tree) _shrinking = null;
                }
                else
                {
                    float wear = Mathf.Clamp01((float)intoTree / StrikesPerClump);
                    _shrinking = tree;
                    _shrinkFrom = tree.localScale;
                    _shrinkTo = _clumpFull[before] * Mathf.Lerp(1f, minScale, wear);
                    _shrinkT = 0f;
                }
            }

            if (_remaining <= 0) Exhaust();
            return yieldPerTrip;
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
        /// Used up. A renewable node just waits to regrow; a felled forest hands its item back to
        /// the world as a bubble, ready to be collected and replanted.
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
