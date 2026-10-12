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
        [SerializeField] Color affordable = new Color(0.22f, 0.16f, 0.11f);
        [SerializeField] Color tooExpensive = new Color(1f, 0.45f, 0.4f);

        IPurchasable _item;

        static PurchaseDialog _instance;

        public static bool IsOpen => _instance != null && _instance.panel != null && _instance.panel.activeSelf;

        void Awake()
        {
            _instance = this;
            Close();
        }

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
            string feedback = soldOut ? "<b>Sold out.</b>" : affordable ? "" : Shortfall(price, store);
            if (blurbLabel != null)
            {
                blurbLabel.text = item.Description + (feedback.Length > 0 ? "\n\n" + feedback : "");
                FitToText();
            }

            if (purchaseButton != null)
            {
                purchaseButton.gameObject.SetActive(!soldOut);
                purchaseButton.interactable = affordable;
            }
            if (panel != null) panel.SetActive(true);
        }

        /// <summary>
        /// What you are short of, and by how much — so you know whether to wait a minute or come
        /// back much later. One line per resource.
        /// </summary>
        static string Shortfall(Vector3Int price, ResourceStore store)
        {
            if (store == null) return "";
            var lines = new System.Collections.Generic.List<string>();
            int wood = price.x - store.Get(ResourceKind.Wood);
            int gold = price.y - store.Get(ResourceKind.Gold);
            int gems = price.z - store.Get(ResourceKind.Gem);
            if (wood > 0) lines.Add($"You need {wood} more wood.");
            if (gold > 0) lines.Add($"You need {gold} more gold.");
            if (gems > 0) lines.Add(gems == 1 ? "You need 1 more gem." : $"You need {gems} more gems.");
            // Coloured line by line, so each still matches its translation.
            for (int i = 0; i < lines.Count; i++) lines[i] = "<color=#b0413e>" + lines[i] + "</color>";
            return string.Join("\n", lines);
        }

        /// <summary>Readable size for the blurb; past what fits at this, the panel grows instead.</summary>
        const float ComfortableFont = 26f;

        RectTransform[] _parts;
        Vector2[] _partPos, _partSize;
        Vector2 _panelSize;

        /// <summary>
        /// A long blurb (a worker's strengths, a shortfall) makes the panel taller rather than the
        /// words tiny: the blurb grows down the middle, everything above it moves up and the buttons
        /// move down. A short one leaves the panel as built.
        /// </summary>
        void FitToText()
        {
            var panelRect = panel != null ? panel.transform as RectTransform : null;
            if (panelRect == null) return;
            if (_parts == null)
            {
                _panelSize = panelRect.sizeDelta;
                _parts = new RectTransform[panelRect.childCount];
                _partPos = new Vector2[_parts.Length];
                _partSize = new Vector2[_parts.Length];
                for (int i = 0; i < _parts.Length; i++)
                {
                    _parts[i] = (RectTransform)panelRect.GetChild(i);
                    _partPos[i] = _parts[i].anchoredPosition;
                    _partSize[i] = _parts[i].sizeDelta;
                }
            }

            var blurbRect = blurbLabel.rectTransform;
            int index = System.Array.IndexOf(_parts, blurbRect);
            if (index < 0) return;
            bool auto = blurbLabel.enableAutoSizing;
            float size = blurbLabel.fontSize;
            blurbLabel.enableAutoSizing = false;
            blurbLabel.fontSize = ComfortableFont;
            float needed = blurbLabel.GetPreferredValues(Loc.T(blurbLabel.text), _partSize[index].x, 0f).y;
            blurbLabel.enableAutoSizing = auto;
            blurbLabel.fontSize = size;
            float extra = Mathf.Clamp(needed + 12f - _partSize[index].y, 0f, 420f);

            panelRect.sizeDelta = _panelSize + new Vector2(0f, extra);
            for (int i = 0; i < _parts.Length; i++)
            {
                if (_parts[i] == null) continue;
                if (i == index)
                {
                    _parts[i].anchoredPosition = _partPos[i];
                    _parts[i].sizeDelta = _partSize[i] + new Vector2(0f, extra);
                }
                else
                    _parts[i].anchoredPosition = _partPos[i] + new Vector2(0f, _partPos[i].y > -100f ? extra / 2f : -extra / 2f);
            }
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
