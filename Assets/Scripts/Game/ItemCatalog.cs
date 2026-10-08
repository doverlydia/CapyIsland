using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Every store item the game can create at runtime, by its store_items.xml name — the original's
    /// StoreItemsList. Shops, quest rewards, fruit-tree saplings, random bubbles and bridge arrivals
    /// all make things through here.
    ///
    /// Each entry is an inactive template built by the island builder; creating one clones it.
    /// Templates never enter the world themselves.
    /// </summary>
    public class ItemCatalog : MonoBehaviour
    {
        public static ItemCatalog Instance { get; private set; }

        [Tooltip("Inactive templates, each tagged with its store name.")]
        [SerializeField] List<CatalogItem> templates = new();

        [Tooltip("Where created items live in the hierarchy.")]
        [SerializeField] Transform itemParent;

        readonly Dictionary<string, CatalogItem> _byName = new();

        void Awake()
        {
            Instance = this;
            foreach (var t in templates)
                if (t != null) _byName[t.StoreName] = t;

            // What comes out of a felled fruit tree.
            Hazards.Register(HazardKind.Monster, at => MakeItem("Monster", at));
            Hazards.Register(HazardKind.Deer, at => MakeItem("Deer", at));
            Hazards.Register(HazardKind.Bomb, at => MakeItem("Bomb", at));

            // Our own items (the Garden Shop and its hobby kit), built in code.
            HobbyCatalog.RegisterInto(this);
        }

        /// <summary>Adds a template built at runtime rather than by the island builder.</summary>
        public void AddTemplate(CatalogItem template)
        {
            if (template == null) return;
            template.gameObject.SetActive(false);
            _byName[template.StoreName] = template;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public Transform ItemParent => itemParent != null ? itemParent : transform.parent;

        public bool Has(string storeName) => _byName.ContainsKey(storeName);

        public GameObject Template(string storeName) =>
            _byName.TryGetValue(storeName, out var t) ? t.gameObject : null;

        /// <summary>The picture of an item — its body sprite — for shop displays and bubbles.</summary>
        public Sprite Icon(string storeName)
        {
            var t = Template(storeName);
            if (t == null) return null;
            var body = t.transform.Find("Sprite");
            var sr = body != null ? body.GetComponent<SpriteRenderer>() : null;
            return sr != null ? sr.sprite : null;
        }

        /// <summary>A fresh, inactive copy of the named item.</summary>
        public GameObject Create(string storeName)
        {
            var template = Template(storeName);
            if (template == null)
            {
                Debug.LogWarning($"[ItemCatalog] No template for '{storeName}'.");
                return null;
            }

            var copy = Instantiate(template, ItemParent);
            copy.name = template.name;
            copy.SetActive(false);
            return copy;
        }

        /// <summary>
        /// The original's MakeStore(name, x, y, free): the named item in a free bubble on the ground.
        /// </summary>
        public Bubble MakeStore(string storeName, Vector2 at)
        {
            var item = Create(storeName);
            return item != null ? BubbleSpawner.SpawnAt(item, at, ItemParent) : null;
        }

        /// <summary>
        /// The original's MakeRandomStore: a free bubble holding a random item with no ownership cap
        /// (every max-0 item — gardens, forests, hills, fruit trees, animals, bombs, decorations).
        /// </summary>
        public Bubble MakeRandomStore(Vector2 at)
        {
            var pool = new List<string>();
            foreach (var def in StoreData.All)
                if (def.Max == 0 && Has(def.Name)) pool.Add(def.Name);
            if (pool.Count == 0) return null;
            return MakeStore(pool[Random.Range(0, pool.Count)], at);
        }

        /// <summary>
        /// The original's MakeItem: the real thing, directly in the world — planted and growing, or
        /// an animal on its feet. Used for what bridges and quests put on the islands.
        /// </summary>
        public GameObject MakeItem(string storeName, Vector2 at)
        {
            var item = Create(storeName);
            if (item == null) return null;
            item.transform.position = new Vector3(at.x, at.y, 0f);
            item.SetActive(true);

            var placeable = item.GetComponent<Placeable>();
            if (placeable != null) placeable.MarkPlaced(silent: true);
            else Purchasing.Release(item);
            return item;
        }
    }
}
