using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// One tree, rock or flower — the original's resource node. Its <see cref="Scale"/> is both its
    /// size on screen and its health: it grows in from 0.2, can only be harvested once fully grown,
    /// and every hit takes scale off until it falls.
    ///
    /// Placing a forest or garden creates one **parent**. The parent immediately tries
    /// outerRadius/10 **children** around itself, and once grown keeps seeding one more every
    /// seed-rate frames — each only if it fits on land without overlapping anything, so a grove fills
    /// its circle and then stops by itself. Children never seed. Felling a child removes it; felling
    /// the parent turns it back into a bubble for the player to replant.
    ///
    /// Rules and numbers: docs/BUNNI_GROUND_TRUTH.md via <see cref="BunniRules"/>.
    /// </summary>
    public class Harvestable : MonoBehaviour
    {
        enum Phase { Unplanted, Growing, Seeding, Grown }

        [Header("What this node is")]
        [SerializeField] NodeResource resource = NodeResource.Pine;

        [Tooltip("True for the node a placed store item becomes. Spawned children set this false.")]
        [SerializeField] bool isParent = true;

        [Header("Footprint (original world units, from store_items.xml)")]
        [Tooltip("The store item's `inner`. A node's own radius is this × its final scale.")]
        [SerializeField] float innerSize = 36f;

        [Tooltip("The store item's `outer`: where children spawn and seed. Parents only.")]
        [SerializeField] float outerRadius = 200f;

        [Header("Look")]
        [Tooltip("Unity scale of the art at a node scale of 1. Presentation only.")]
        [SerializeField] float artScale = 1.4f;

        [Tooltip("Sprite once planted, if different from the carried icon (a garden's icon is a bed, " +
                 "but the planted parent is a single flower). Children always use it.")]
        [SerializeField] Sprite plantedSprite;

        // Fruit shaken off a fruit tree is a flower-type food node that rots if nobody eats it
        // (GardenItem.mIsFruit / mLifeOver).
        bool _isFruit;
        int _lifeTimer;
        int _lifeOver;

        Phase _phase = Phase.Unplanted;
        FrameClock _clock;
        float _scale;
        float _finalScale;
        float _innerRadius;
        int _seedCounter;
        Sprite _iconSprite;
        SpriteRenderer _body;
        Placeable _placeable;

        public NodeResource Resource => resource;
        public NodeFamily Family => BunniRules.FamilyOf(resource);
        public bool IsParent => isParent;
        public float Scale => _scale;

        /// <summary>Only a fully grown node can be harvested (the original's mCanHarvest).</summary>
        public bool CanHarvest => _phase == Phase.Seeding || _phase == Phase.Grown;

        /// <summary>Claimed by a worker already heading for it, so no one else targets it.</summary>
        public bool Reserved { get; set; }

        /// <summary>This node's own inner radius, in world units.</summary>
        public float InnerRadius => _innerRadius;

        /// <summary>Value when harvested: the resource's price, doubled for a parent.</summary>
        public int Value => BunniRules.Price(resource) * (isParent ? 2 : 1);

        /// <summary>Out in the world rather than riding in the trail or sitting in a bubble.</summary>
        public bool InWorld => isActiveAndEnabled && _phase != Phase.Unplanted;

        /// <summary>Whether this node occupies ground that new items and seeds must avoid.</summary>
        public bool BlocksPlacement => InWorld;

        /// <summary>A fruit lying on the ground rather than a planted flower.</summary>
        public bool IsFruit => _isFruit;

        void Awake()
        {
            var sprite = transform.Find("Sprite");
            _body = sprite != null ? sprite.GetComponent<SpriteRenderer>() : null;
            if (_body != null) _iconSprite = _body.sprite;
            _placeable = GetComponent<Placeable>();
        }

        /// <summary>
        /// Configures a spawned child. Children are plain nodes: no Placeable, never picked up,
        /// never seeding.
        /// </summary>
        public void InitChild(NodeResource res, float inner, float art, Sprite sprite)
        {
            resource = res;
            isParent = false;
            innerSize = inner;
            outerRadius = 0f;
            artScale = art;
            plantedSprite = sprite;
            Awake();
            BeginGrowing();
        }

        // ---------------------------------------------------------------- planting

        /// <summary>A parent was planted: grow, and scatter the first children around it.</summary>
        void OnPlacedInWorld()
        {
            if (!isParent) return;
            if (_body != null && plantedSprite != null) _body.sprite = plantedSprite;
            BeginGrowing();

            // The first burst: outerRadius/10 attempts (/5 for hills), each tested with the parent's own radius.
            int attempts = BunniRules.InitialChildAttempts(outerRadius, Family);
            for (int i = 0; i < attempts; i++) TrySpawnChild(childInnerSize: innerSize);
        }

        /// <summary>Bubbled again: forget everything, ready to be replanted fresh.</summary>
        void OnReturnedToBubble()
        {
            _phase = Phase.Unplanted;
            Reserved = false;
            _scale = 0f;
            if (_body != null && _iconSprite != null) _body.sprite = _iconSprite;
        }

        void BeginGrowing()
        {
            var range = BunniRules.FinalScaleRange(Family, isParent);
            _finalScale = Random.Range(range.x, range.y);
            _innerRadius = innerSize * _finalScale;
            _scale = BunniRules.SpawnScale;
            _seedCounter = 0;
            _phase = Phase.Growing;
            Reserved = false;
            ApplyScale();
        }

        // ---------------------------------------------------------------- per frame

        void Update()
        {
            if (_phase == Phase.Unplanted) return;
            if (_phase == Phase.Grown && !_isFruit) return;

            int frames = _clock.Consume(Time.deltaTime);
            for (int i = 0; i < frames; i++)
            {
                if (_phase != Phase.Grown) TickFrame();
                if (_isFruit && TickRot()) return;
            }
        }

        /// <summary>One frame of a fallen fruit's life. True once it has rotted away.</summary>
        bool TickRot()
        {
            _lifeTimer++;
            if (_body != null && _lifeOver - _lifeTimer < 60)
                _body.enabled = (_lifeTimer / 4) % 2 == 0;   // blinks for its last two seconds
            if (_lifeTimer <= _lifeOver) return false;

            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Poof(transform.position, new Color(0.62f, 0.5f, 0.3f));
            Reserved = false;
            Destroy(gameObject);
            return true;
        }

        /// <summary>
        /// A fruit dropped by a fruit tree: food of the given flower kind, child-sized, at
        /// <paramref name="at"/>. It lives (rand x 10 + 1) x 200 frames unless eaten first.
        /// </summary>
        public static Harvestable SpawnFruit(NodeResource food, Sprite sprite, float artScale, Vector2 at,
            Transform parent, GameObject shadowTemplate)
        {
            var go = new GameObject(food + "Fruit");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(at.x, at.y, 0f);

            if (shadowTemplate != null)
            {
                var shadow = Instantiate(shadowTemplate, go.transform);
                shadow.name = "Shadow";
                shadow.transform.localPosition = Vector3.zero;
                shadow.transform.localScale = shadowTemplate.transform.localScale * 0.35f;
                shadow.SetActive(true);
            }

            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            var node = go.AddComponent<Harvestable>();
            node._isFruit = true;
            node._lifeTimer = 0;
            node._lifeOver = BunniRules.RandomLifetimeFrames();
            node.InitChild(food, BunniRules.FruitInnerSize, artScale, sprite);
            go.AddComponent<YSorter>();
            return node;
        }

        void TickFrame()
        {
            if (_phase == Phase.Growing)
            {
                _scale += BunniRules.GrowthPerFrame(Family);
                if (_scale > _finalScale)
                {
                    _scale = _finalScale;
                    // Grown: harvestable. Parents go on to seed; children just stand.
                    _phase = isParent ? Phase.Seeding : Phase.Grown;
                }
                ApplyScale();
                return;
            }

            if (_phase == Phase.Seeding)
            {
                _seedCounter++;
                if (_seedCounter <= BunniRules.SeedRateFrames(resource)) return;
                _seedCounter = 0;
                // A seeded child's inner size is the parent's own radius — the original passes
                // the parent's inner circle through, so seeded children run slightly larger.
                TrySpawnChild(childInnerSize: _innerRadius);
            }
        }

        /// <summary>
        /// One seed: a random point in the outer circle, kept only if a circle the size of this
        /// parent fits there on land without touching anything. A miss simply does nothing.
        /// </summary>
        void TrySpawnChild(float childInnerSize)
        {
            var at = BunniSpace.RandomPointInCircle(transform.position, outerRadius);
            float testRadius = innerSize * _finalScale;
            if (!BunniSpace.CircleOnMap(at, testRadius)) return;

            var child = new GameObject(resource + "Child");
            child.transform.SetParent(transform.parent, false);   // a sibling: it outlives the parent
            child.transform.position = new Vector3(at.x, at.y, transform.position.z);

            var shadowSource = transform.Find("Shadow");
            if (shadowSource != null)
            {
                var shadow = Instantiate(shadowSource.gameObject, child.transform);
                shadow.name = "Shadow";
                shadow.transform.localPosition = shadowSource.localPosition;
                shadow.SetActive(true);
            }

            var body = new GameObject("Sprite");
            body.transform.SetParent(child.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = plantedSprite != null ? plantedSprite : _iconSprite;

            var node = child.AddComponent<Harvestable>();
            node.InitChild(resource, childInnerSize, artScale, sr.sprite);
            child.AddComponent<YSorter>();

            // Children bend in the wind like their parent.
            var parentSway = GetComponent<Sway>();
            if (parentSway != null)
            {
                var sway = child.AddComponent<Sway>();
                sway.CopyFrom(parentSway);
            }
        }

        // ---------------------------------------------------------------- harvesting

        /// <summary>
        /// One hit: takes <paramref name="amount"/> off the node's scale. Returns true when that hit
        /// felled it — a child is removed, a parent turns back into a bubble.
        /// </summary>
        public bool Hit(float amount) => Hit(amount, spoil: false);

        /// <summary>
        /// A hit. With <paramref name="spoil"/> — a deer eating or a wild monster wrecking — a
        /// felled parent is simply gone instead of becoming a bubble.
        /// </summary>
        public bool Hit(float amount, bool spoil)
        {
            if (!CanHarvest) return false;
            _scale -= amount;
            if (_scale >= 0f) { ApplyScale(); return false; }

            _scale = 0f;
            Reserved = false;
            Fell(spoil);
            return true;
        }

        void Fell(bool spoil)
        {
            if (HarvestEffects.Instance != null)
                HarvestEffects.Instance.Felled(transform.position, Family);

            if (isParent && _placeable != null && !spoil)
            {
                BubbleSpawner.ReturnToBubble(_placeable);
                return;
            }
            Destroy(gameObject);
        }

        void ApplyScale()
        {
            transform.localScale = Vector3.one * (artScale * Mathf.Max(0.001f, _scale));
        }

        void OnDrawGizmosSelected()
        {
            var r = BunniSpace.ToUnity(new Vector2(_innerRadius, _innerRadius));
            Gizmos.color = new Color(1f, 0.5f, 0.3f, 0.9f);
            Gizmos.DrawWireCube(transform.position, new Vector3(r.x * 2f, r.y * 2f, 0f));
            if (!isParent) return;
            var o = BunniSpace.ToUnity(new Vector2(outerRadius, outerRadius));
            Gizmos.color = new Color(0.4f, 0.9f, 0.4f, 0.4f);
            Gizmos.DrawWireCube(transform.position, new Vector3(o.x * 2f, o.y * 2f, 0f));
        }
    }
}
