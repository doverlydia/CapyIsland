using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Every discovery card in a grid: the ones you have found show their picture and name, the rest
    /// a question mark — a quiet list of what is still out there.
    /// </summary>
    public class CollectionScreen : UiScreen
    {
        public static CollectionScreen Instance { get; private set; }

        protected override string Title => "Collection";
        protected override Sprite TitleIcon => IconPainter.Cards;
        protected override Color AccentColour => UiKit.Hex("A77BD6");

        RectTransform _content;
        GridLayoutGroup _grid;
        TextMeshProUGUI _count;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override void Build(RectTransform body)
        {
            _count = UiKit.Text(body, "Count", "", 32f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
            _count.rectTransform.anchorMin = new Vector2(0f, 1f);
            _count.rectTransform.anchorMax = new Vector2(1f, 1f);
            _count.rectTransform.pivot = new Vector2(0.5f, 1f);
            _count.rectTransform.sizeDelta = new Vector2(0f, 50f);

            var scroll = UiKit.ScrollList(body, "Grid", out _content, 0f, stacked: false);
            UiKit.Stretch((RectTransform)scroll.transform, 0f, 0f, 0f, 64f);
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _grid = _content.gameObject.AddComponent<GridLayoutGroup>();
            _grid.spacing = new Vector2(18f, 18f);
            _grid.padding = new RectOffset(4, 4, 8, 24);
            _grid.childAlignment = TextAnchor.UpperCenter;
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        }

        protected override void OnOpen()
        {
            ClearChildren(_content);
            var catalog = ItemCatalog.Instance;
            int found = 0;
            foreach (var card in QuestData.Cards)
            {
                bool have = Cards.IsCollected(card.Id);
                if (have) found++;

                var tile = UiKit.Image(_content, card.Name, UiKit.Rounded(26), have ? Color.white : UiKit.SurfaceSunk);
                var pic = UiKit.Image(tile.transform, "Picture", null, Color.white);
                pic.preserveAspect = true;
                pic.rectTransform.anchorMin = new Vector2(0f, 0.3f);
                pic.rectTransform.anchorMax = new Vector2(1f, 1f);
                pic.rectTransform.offsetMin = new Vector2(18f, 0f);
                pic.rectTransform.offsetMax = new Vector2(-18f, -14f);
                if (have && catalog != null && card.Items.Length > 0) pic.sprite = catalog.Icon(card.Items[0]);
                if (pic.sprite == null)
                {
                    pic.enabled = false;
                    var q = UiKit.Text(tile.transform, "Unknown", "?", 90f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
                    q.rectTransform.anchorMin = new Vector2(0f, 0.3f);
                    q.rectTransform.anchorMax = Vector2.one;
                    q.rectTransform.offsetMin = q.rectTransform.offsetMax = Vector2.zero;
                }

                var label = UiKit.Text(tile.transform, "Name", have ? card.Name : "???", 26f, have ? UiKit.Ink : UiKit.InkSoft, true, TextAlignmentOptions.Center);
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = new Vector2(1f, 0.3f);
                label.rectTransform.offsetMin = new Vector2(10f, 8f);
                label.rectTransform.offsetMax = new Vector2(-10f, 0f);
                label.enableAutoSizing = true;
                label.fontSizeMin = 18f;
                label.fontSizeMax = 26f;
            }
            _count.text = $"{found} of {QuestData.Cards.Length} discovered";
        }

        protected override void Fit()
        {
            base.Fit();
            if (_grid == null || Sheet == null) return;
            float width = Sheet.sizeDelta.x - 64f - 8f;
            int columns = Mathf.Clamp(Mathf.FloorToInt(width / 220f), 3, 6);
            float cell = (width - _grid.spacing.x * (columns - 1)) / columns;
            _grid.cellSize = new Vector2(cell, cell * 1.2f);
            _grid.constraintCount = columns;
        }
    }
}
