using System;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A building that sells something — the Capy House to begin with. A sample of the goods stands
    /// on a pedestal beside it; tapping that sample opens a purchase dialog, and buying drops the
    /// new item into your inventory trail.
    ///
    /// The original's Capy (Bunni) House sells a worker for 10 wood + 10 gold, which is the first
    /// thing the player ever spends money on.
    /// </summary>
    public class ShopBuilding : MonoBehaviour
    {
        [Header("Goods")]
        [SerializeField] string itemName = "Capybara";

        [TextArea(2, 4)]
        [SerializeField] string blurb = "A calm creature with a broad nose and endless patience. Capybaras earn you wood and gold.";

        [Tooltip("Source object cloned on purchase. Kept inactive in the scene.")]
        [SerializeField] GameObject itemPrefab;

        [Header("Price")]
        [SerializeField] int woodCost = 10;
        [SerializeField] int goldCost = 10;
        [SerializeField] int gemCost;

        [Header("Sample on the pedestal")]
        [Tooltip("The display model beside the shop that the player taps to buy.")]
        [SerializeField] Transform sample;
        [SerializeField] float tapRadius = 1.1f;

        /// <summary>Raised when the player taps the sample, so the UI can open the buy dialog.</summary>
        public static event Action<ShopBuilding> Browsed;

        /// <summary>Raised after a successful purchase, with the newly created item.</summary>
        public static event Action<GameObject, ShopBuilding> Purchased;

        public string ItemName => itemName;
        public string Blurb => blurb;
        public int WoodCost => woodCost;
        public int GoldCost => goldCost;
        public int GemCost => gemCost;

        public bool CanAfford =>
            ResourceStore.Instance != null && ResourceStore.Instance.CanAfford(woodCost, goldCost, gemCost);

        /// <summary>Nearest shop sample under a world point, or null.</summary>
        public static ShopBuilding Pick(Vector2 worldPoint)
        {
            ShopBuilding best = null;
            float bestDistance = float.MaxValue;
            foreach (var s in FindObjectsByType<ShopBuilding>(FindObjectsSortMode.None))
            {
                if (s == null || !s.isActiveAndEnabled) continue;
                Vector2 at = s.sample != null ? (Vector2)s.sample.position : (Vector2)s.transform.position;
                float d = Vector2.Distance(at, worldPoint);
                if (d > s.tapRadius || d >= bestDistance) continue;
                best = s;
                bestDistance = d;
            }
            return best;
        }

        public void Browse() => Browsed?.Invoke(this);

        /// <summary>Spends the price and spawns the item into the inventory trail. False if too poor.</summary>
        public bool TryPurchase()
        {
            var store = ResourceStore.Instance;
            if (store == null || itemPrefab == null) return false;
            if (!store.TrySpend(woodCost, goldCost, gemCost)) return false;

            var spawnAt = sample != null ? sample.position : transform.position;
            var item = Instantiate(itemPrefab, spawnAt, Quaternion.identity, transform.parent);
            item.name = itemName;
            item.SetActive(true);

            var placeable = item.GetComponent<Placeable>();
            if (placeable != null) placeable.MarkCarried();
            var follower = item.GetComponent<Follower>();
            if (follower != null) follower.PickUpIntoInventory();

            Purchased?.Invoke(item, this);
            return true;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.8f);
            var at = sample != null ? sample.position : transform.position;
            Gizmos.DrawWireSphere(at, tapRadius);
        }
    }
}
