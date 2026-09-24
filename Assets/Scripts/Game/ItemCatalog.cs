using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Every store item the game can hand out at runtime, by its store_items.xml name. The original's
    /// StoreItemsList.MakeStore / MakeItem build items from their names — a sapling found in a fruit
    /// tree, a random bubble, a shop purchase — and this is that lookup.
    ///
    /// Each entry is an inactive template in the scene, built by the island builder; creating one
    /// clones it. Templates never enter the world themselves.
    /// </summary>
    public class ItemCatalog : MonoBehaviour
    {
        public static ItemCatalog Instance { get; private set; }

        [Tooltip("Inactive templates. Each one's Placeable.DisplayName is its store name.")]
        [SerializeField] List<Placeable> templates = new();

        [Tooltip("Names MakeRandomStore may pick from.")]
        [SerializeField] List<string> randomPool = new();

        [Tooltip("Where created items live in the hierarchy.")]
        [SerializeField] Transform itemParent;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public IReadOnlyList<Placeable> Templates => templates;

        public Placeable Template(string storeName)
        {
            foreach (var t in templates)
                if (t != null && t.DisplayName == storeName) return t;
            return null;
        }

        /// <summary>A fresh, inactive copy of the named item, ready to go in a bubble or the trail.</summary>
        public Placeable Create(string storeName)
        {
            var template = Template(storeName);
            if (template == null)
            {
                Debug.LogWarning($"[ItemCatalog] No template for '{storeName}'.");
                return null;
            }

            var copy = Instantiate(template.gameObject, itemParent != null ? itemParent : transform.parent);
            copy.name = template.gameObject.name;
            copy.SetActive(false);
            return copy.GetComponent<Placeable>();
        }

        /// <summary>
        /// The original's MakeStore(name, x, y, free): the named item in a free bubble on the ground.
        /// </summary>
        public Bubble MakeStore(string storeName, Vector2 at)
        {
            var item = Create(storeName);
            return item != null ? BubbleSpawner.SpawnAt(item, at, itemParent != null ? itemParent : transform.parent) : null;
        }

        /// <summary>The original's MakeRandomStore: a free bubble holding a random item.</summary>
        public Bubble MakeRandomStore(Vector2 at)
        {
            if (randomPool.Count == 0) return null;
            return MakeStore(randomPool[Random.Range(0, randomPool.Count)], at);
        }
    }
}
