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

        ShopBuilding _shop;

        void Awake() => Close();

        void OnEnable()
        {
            ShopBuilding.Browsed += Open;
            if (cancelButton != null) cancelButton.onClick.AddListener(Close);
            if (purchaseButton != null) purchaseButton.onClick.AddListener(Buy);
        }

        void OnDisable()
        {
            ShopBuilding.Browsed -= Open;
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Close);
            if (purchaseButton != null) purchaseButton.onClick.RemoveListener(Buy);
        }

        void Open(ShopBuilding shop)
        {
            if (shop == null) return;
            _shop = shop;

            if (titleLabel != null) titleLabel.text = shop.ItemName;
            if (blurbLabel != null) blurbLabel.text = shop.Blurb;

            var store = ResourceStore.Instance;
            SetPrice(woodLabel, shop.WoodCost, store != null ? store.Get(ResourceKind.Wood) : 0);
            SetPrice(goldLabel, shop.GoldCost, store != null ? store.Get(ResourceKind.Gold) : 0);
            SetPrice(gemLabel, shop.GemCost, store != null ? store.Get(ResourceKind.Gem) : 0);

            if (purchaseButton != null) purchaseButton.interactable = shop.CanAfford;
            if (panel != null) panel.SetActive(true);
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
            if (_shop == null) { Close(); return; }
            _shop.TryPurchase();   // refuses on its own if the player cannot afford it
            Close();
        }

        public void Close()
        {
            _shop = null;
            if (panel != null) panel.SetActive(false);
        }
    }
}
