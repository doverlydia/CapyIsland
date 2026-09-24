using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The buy panel: item name, blurb, the three prices, and Cancel / Purchase — the same shape
    /// the original uses when you tap the sample beside a shop.
    ///
    /// Listens for <see cref="ShopBuilding.Browsed"/> rather than being opened by the shop, so a
    /// shop needs no reference to any UI and new shops work without wiring.
    /// </summary>
    public class PurchaseDialog : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text blurbLabel;
        [SerializeField] TMP_Text woodLabel;
        [SerializeField] TMP_Text goldLabel;
        [SerializeField] TMP_Text gemLabel;
        [SerializeField] Button cancelButton;
        [SerializeField] Button purchaseButton;

        [Header("Colours")]
        [SerializeField] Color affordable = Color.white;
        [SerializeField] Color tooExpensive = new Color(1f, 0.45f, 0.4f);

        IPurchasable _item;

        void Awake() => Close();

        void OnEnable()
        {
            SaleDisplay.Browsed += Open;
            if (cancelButton != null) cancelButton.onClick.AddListener(Close);
            if (purchaseButton != null) purchaseButton.onClick.AddListener(Buy);
        }

        void OnDisable()
        {
            SaleDisplay.Browsed -= Open;
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Close);
            if (purchaseButton != null) purchaseButton.onClick.RemoveListener(Buy);
        }

        void Open(IPurchasable item)
        {
            if (item == null) return;
            _item = item;

            if (titleLabel != null) titleLabel.text = item.Title;

            var store = ResourceStore.Instance;
            var price = item.Price;
            SetPrice(woodLabel, price.x, store != null ? store.Get(ResourceKind.Wood) : 0);
            SetPrice(goldLabel, price.y, store != null ? store.Get(ResourceKind.Gold) : 0);
            SetPrice(gemLabel, price.z, store != null ? store.Get(ResourceKind.Gem) : 0);

            bool soldOut = item.SoldOut;
            bool affordable = store != null && store.CanAfford(price.x, price.y, price.z);
            string feedback = soldOut ? "<b>Sold out.</b>" : affordable ? "" : "<color=#b0413e>" + Shortfall(price, store) + "</color>";
            if (blurbLabel != null)
                blurbLabel.text = item.Description + (feedback.Length > 0 ? "\n\n" + feedback : "");

            if (purchaseButton != null)
            {
                purchaseButton.gameObject.SetActive(!soldOut);
                purchaseButton.interactable = affordable;
            }
            if (panel != null) panel.SetActive(true);
        }

        /// <summary>What you are short of — the original's purchase feedback.</summary>
        static string Shortfall(Vector3Int price, ResourceStore store)
        {
            if (store == null) return "";
            bool wood = store.Get(ResourceKind.Wood) < price.x;
            bool gold = store.Get(ResourceKind.Gold) < price.y;
            bool gems = store.Get(ResourceKind.Gem) < price.z;
            if (wood && gold && gems) return "You need more wood, gold and gems.";
            if (gems) return "You need more gems.";
            if (gold && wood) return "You need more wood and gold.";
            if (gold) return "You need more gold.";
            return "You need more wood.";
        }

        /// <summary>Shows the cost, reddened when you cannot cover it.</summary>
        void SetPrice(TMP_Text label, int cost, int held)
        {
            if (label == null) return;
            label.text = cost.ToString();
            label.color = held >= cost ? affordable : tooExpensive;
        }

        void Buy()
        {
            if (_item == null) { Close(); return; }
            _item.Buy();   // refuses on its own if the player cannot afford it
            Close();
        }

        public void Close()
        {
            _item = null;
            if (panel != null) panel.SetActive(false);
        }
    }
}
