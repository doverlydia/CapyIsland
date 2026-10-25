using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The Furniture Shop's window. Until you have a burrow door it sells you that first (your
    /// furniture needs somewhere to go); after that it shows the day's five pieces — four
    /// everyday ones for wood and gold, one fancy one for gems — each to be had once a day.
    /// What you buy waits in your burrow's storage, ready to place.
    /// </summary>
    public class FurnitureShopScreen : UiScreen
    {
        public static FurnitureShopScreen Instance { get; private set; }

        protected override string Title => "Furniture Shop";
        protected override Sprite TitleIcon => IconPainter.Gift;
        protected override Color AccentColour => UiKit.Hex("9C5B8E");
        protected override float MaxWidth => 900f;

        RectTransform _list;
        ScrollRect _scroll;
        Vector2 _shopAt;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Show(Vector2 shopAt)
        {
            if (Instance == null) return;
            Instance._shopAt = shopAt;
            Instance.Open();
        }

        protected override void Build(RectTransform body)
        {
            _scroll = UiKit.ScrollList(body, "Pieces", out _list, 16f, new RectOffset(4, 4, 8, 32));
            UiKit.Stretch((RectTransform)_scroll.transform);
        }

        protected override void OnOpen() => Refresh();

        /// <summary>A burrow door of your own, standing, carried, or waiting in a bubble.</summary>
        static bool HaveDoor => StoreData.CountByName("Monster Den") > 0;

        void Refresh()
        {
            ClearChildren(_list);

            if (!HaveDoor)
            {
                SectionLabel(_list, "First things first");
                DoorRow();
                SectionLabel(_list, "Today's pieces");
                Note("Get your burrow door first, and then your furniture has a home to go to!");
            }
            else
            {
                SectionLabel(_list, "Today's pieces");
                var left = Furniture.UntilTomorrow;
                Note(Loc.F("New pieces in {0}h {1}m. Each piece can be bought once a day.", (int)left.TotalHours, left.Minutes));
            }

            foreach (var f in Furniture.Today()) PieceRow(f, HaveDoor);
            _scroll.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
        }

        void Note(string text)
        {
            var t = UiKit.Text(_list, "Note", text, 28f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            t.overflowMode = TextOverflowModes.Overflow;
            t.margin = new Vector4(16f, 4f, 16f, 8f);
        }

        RectTransform Row(Sprite art, string title, string blurb, bool dim, Color? tint = null)
        {
            var card = UiKit.Image(_list, "Piece", UiKit.Rounded(30), tint ?? Color.white).rectTransform;
            var le = card.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 200f;
            GrowsWithText.Add(card, tall: 1.5f);   // Easy Reading: a taller card, so the name and two lines of blurb have room

            var back = UiKit.Image(card, "Back", UiKit.Rounded(26), UiKit.SurfaceSunk);
            UiKit.Place(back.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(164f, 164f));
            var pic = UiKit.Image(back.transform, "Pic", art, dim ? new Color(1f, 1f, 1f, 0.45f) : Color.white);
            pic.preserveAspect = true;
            UiKit.Stretch(pic.rectTransform, 12f, 12f, 12f, 12f);

            var name = UiKit.Text(card, "Name", title, 36f, dim ? UiKit.InkSoft : UiKit.Ink, true, TextAlignmentOptions.TopLeft);
            name.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            name.rectTransform.anchorMax = Vector2.one;
            name.rectTransform.offsetMin = new Vector2(200f, 0f);
            name.rectTransform.offsetMax = new Vector2(-230f, -18f);
            name.enableAutoSizing = true;
            name.fontSizeMin = 24f;
            name.fontSizeMax = 36f;

            if (!string.IsNullOrEmpty(blurb))
            {
                var b = UiKit.Text(card, "Blurb", blurb, 22f, UiKit.InkSoft, false, TextAlignmentOptions.TopLeft);
                b.overflowMode = TextOverflowModes.Overflow;
                b.rectTransform.anchorMin = new Vector2(0f, 0f);
                b.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                b.rectTransform.offsetMin = new Vector2(200f, 64f);
                b.rectTransform.offsetMax = new Vector2(-230f, 30f);   // room for two lines
            }
            return card;
        }

        /// <summary>Wood, gold and gem prices as little chips along the bottom of a row.</summary>
        static void Prices(RectTransform card, int wood, int gold, int gems)
        {
            var row = UiKit.Rect("Price", card);
            row.anchorMin = new Vector2(0f, 0f);
            row.anchorMax = new Vector2(1f, 0f);
            row.pivot = new Vector2(0f, 0f);
            row.offsetMin = new Vector2(200f, 22f);
            row.offsetMax = new Vector2(-230f, 70f);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = false;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            var store = ResourceStore.Instance;
            void Chip(ResourceKind kind, int n)
            {
                if (n <= 0) return;
                bool enough = store != null && store.Get(kind) >= n;
                var c = UiKit.Rect(kind.ToString(), row);
                c.sizeDelta = new Vector2(150f, 48f);
                var ic = UiKit.Image(c, "Icon", UiKit.ResourceIcon(kind), Color.white);
                ic.preserveAspect = true;
                UiKit.Place(ic.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(44f, 44f));
                var t = UiKit.Text(c, "N", n.ToString(), 28f, enough ? UiKit.Ink : UiKit.Danger, true, TextAlignmentOptions.MidlineLeft);
                UiKit.Stretch(t.rectTransform, 52f, 0f, 0f, 0f);
            }
            Chip(ResourceKind.Wood, wood);
            Chip(ResourceKind.Gold, gold);
            Chip(ResourceKind.Gem, gems);
        }

        static Button BuyButton(RectTransform card, string label, bool enabled, System.Action buy, UiKit.ButtonStyle style = UiKit.ButtonStyle.Primary)
        {
            var b = UiKit.Button(card, "Buy", label, style, buy, 32f);
            var r = (RectTransform)b.transform.parent;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 0.5f);
            r.anchoredPosition = new Vector2(-22f, 0f);
            r.sizeDelta = new Vector2(190f, 92f);
            b.interactable = enabled;
            return b;
        }

        void DoorRow()
        {
            var catalog = ItemCatalog.Instance;
            var art = catalog != null ? catalog.Icon("Monster Den") : null;
            var card = Row(art, StoryText.Name("Monster Den"), StoryText.Blurb("Monster Den"), false, UiKit.Hex("FFF1D6"));
            var price = StoreData.Price("Monster Den");
            Prices(card, price.x, price.y, price.z);
            var store = ResourceStore.Instance;
            bool afford = store != null && store.CanAfford(price.x, price.y, price.z);
            BuyButton(card, "Buy", afford, () =>
            {
                if (!Purchasing.TryBuy("Monster Den", _shopAt)) return;
                Close();
                Hints.Answer("Your burrow door is with you. Put it down somewhere cosy, then tap it to go inside!");
            });
        }

        void PieceRow(FurnitureDef f, bool open)
        {
            bool sold = HomeLog.BoughtToday(f.Id);
            var card = Row(f.Art, f.Name, f.Fancy ? "<color=#9C5B8E>Today's special</color>" : null, !open || sold, f.Fancy ? UiKit.Hex("F7EDF6") : (Color?)null);
            Prices(card, f.Wood, f.Gold, f.Gems);
            if (!open) return;
            if (sold)
            {
                BuyButton(card, "Sold", false, null, UiKit.ButtonStyle.Secondary);
                return;
            }
            var store = ResourceStore.Instance;
            bool afford = store != null && store.CanAfford(f.Wood, f.Gold, f.Gems);
            BuyButton(card, "Buy", afford, () =>
            {
                if (store == null || !store.TrySpend(f.Wood, f.Gold, f.Gems)) return;
                Sfx.Play(SfxKind.Buy, 0.8f);
                HomeLog.AddToStorage(f.Id);
                HomeLog.MarkBought(f.Id);
                Hints.Answer(Loc.F("The {0} is waiting in your burrow's storage.", Loc.T(f.Name)));
                Refresh();
            });
        }
    }
}
