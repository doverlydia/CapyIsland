using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Chef Tofu's recipe book: every dish, and whether it's yours. Recipes at your cooking level are
    /// for sale; the ones above it wait, greyed; the first ones come free with gardening and
    /// fishing. What goes into a dish stays a secret until you know it (or work it out in the pot).
    /// </summary>
    public class RecipeShopScreen : UiScreen
    {
        public static RecipeShopScreen Instance { get; private set; }

        protected override string Title => "Chef Tofu's Recipes";
        protected override Sprite TitleIcon => IconPainter.Flame;
        protected override Color AccentColour => UiKit.Hex("C0703E");
        protected override float MaxWidth => 900f;

        RectTransform _list;

        /// <summary>A recipe's price button, kept up to date while the list is open (the island earns as you look).</summary>
        class Price
        {
            public DishKind Dish;
            public Button Button;
            public Image Face, Lip;
            public TextMeshProUGUI[] Labels;
            public Image[] Icons;
            public TextMeshProUGUI Short;
            public int Shown = -1;   // the gold still missing, as last drawn (0 = affordable)
        }

        readonly List<Price> _prices = new();
        float _nextCheck;

        void Awake() => Instance = this;

        protected override void Update()
        {
            base.Update();
            if (_prices.Count == 0 || Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.25f;
            foreach (var p in _prices) Paint(p);
        }

        /// <summary>Bright and pressable when you can pay; grey, with how much is missing, when you can't.</summary>
        static void Paint(Price p)
        {
            if (p.Button == null) return;
            var store = ResourceStore.Instance;
            int missing = Mathf.Max(0, p.Dish.RecipeGold - (store != null ? store.Get(ResourceKind.Gold) : 0));
            if (missing == p.Shown) return;
            p.Shown = missing;
            bool afford = missing == 0;
            var fill = afford ? UiKit.Primary : UiKit.Hex("E4DDD2");
            p.Face.color = fill;
            p.Lip.color = Color.Lerp(fill, Color.black, 0.22f);
            var ink = afford ? Color.white : UiKit.InkSoft;
            foreach (var t in p.Labels) t.color = ink;
            foreach (var i in p.Icons) i.color = afford ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            p.Button.interactable = afford;
            p.Short.gameObject.SetActive(!afford);
            if (!afford) p.Short.text = Loc.F("You need {0} more gold.", missing);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override void Build(RectTransform body)
        {
            var scroll = UiKit.ScrollList(body, "List", out _list, 18f);
            UiKit.Stretch((RectTransform)scroll.transform);
        }

        protected override void OnOpen() => Refresh();

        void Refresh()
        {
            for (int i = _list.childCount - 1; i >= 0; i--) Destroy(_list.GetChild(i).gameObject);
            _prices.Clear();

            // The recipes you already know are in your book, right at the top; the list below is
            // only what's still to learn.
            var book = UiKit.Button(_list, "MyBook", "My recipe book", UiKit.ButtonStyle.Secondary, () =>
            {
                Sfx.Play(SfxKind.Tap, 0.5f);
                RecipeBook.Open((RectTransform)transform);
            }, 30f, IconPainter.Book);
            book.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;

            var intro = UiKit.Text(_list, "Intro", "A recipe shows what goes into a dish, and islanders only ask for dishes you know. Put the right things in the pot and you might work one out for yourself!", 28f, UiKit.InkSoft);
            intro.overflowMode = TextOverflowModes.Overflow;

            // For sale first, then what's coming.
            var rows = new List<DishKind>();
            foreach (var d in HobbyData.Dishes) if (!HobbyLog.KnowsRecipe(d.Id)) rows.Add(d);
            int Order(DishKind d) => ChefTofu.WhyNot(d) != null || d.FreeWith != null ? 1 : 0;
            rows.Sort((a, b) => Order(a) != Order(b) ? Order(a).CompareTo(Order(b)) : a.MinLevel.CompareTo(b.MinLevel));
            foreach (var d in rows) Row(d);
            if (rows.Count == 0)
            {
                var all = UiKit.Text(_list, "All", "You know every recipe Chef Tofu has. Bravo!", 30f, UiKit.Leaf, true, TextAlignmentOptions.Center);
                all.overflowMode = TextOverflowModes.Overflow;
            }
        }

        void Row(DishKind d)
        {
            string why = ChefTofu.WhyNot(d);
            bool levelLocked = why != null;

            var row = UiKit.Image(_list, d.Id, UiKit.Rounded(30), levelLocked ? UiKit.Hex("EFE6D6") : Color.white).rectTransform;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(20, 20, 18, 18);
            h.spacing = 18f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            RtlLayout.Add(h);

            var pic = UiKit.Image(row, "Picture", HobbyData.ItemArt("dish:" + d.Id), levelLocked ? new Color(0.6f, 0.6f, 0.6f, 0.6f) : Color.white);
            pic.preserveAspect = true;
            var ple = pic.gameObject.AddComponent<LayoutElement>();
            ple.preferredWidth = ple.preferredHeight = ple.minWidth = 104f;

            var words = UiKit.Rect("Words", row);
            var v = words.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var name = UiKit.Text(words, "Name", d.Name, 34f, levelLocked ? UiKit.InkSoft : UiKit.Ink, true);
            name.overflowMode = TextOverflowModes.Overflow;
            var blurb = UiKit.Text(words, "Blurb", d.Blurb, 26f, UiKit.InkSoft);
            blurb.overflowMode = TextOverflowModes.Overflow;

            // The right-hand side: a price, or why not.
            // A free one says how to get it, once you have a stove (before that: "Buy a stove first", like the rest).
            if (d.FreeWith is Hobby free && !HobbyLog.IsUnlocked(free) && HobbyLog.IsUnlocked(Hobby.Cooking))
            {
                Tag(row, free == Hobby.Fishing ? "Free when you take up fishing" : "Free when you take up gardening", UiKit.InkSoft);
                return;
            }
            if (levelLocked)
            {
                Tag(row, why, UiKit.InkSoft);
                return;
            }
            // Too dear for now: a grey price that clearly can't be pressed, and how much is missing
            // (kept up to date while the list is open: see Paint).
            var buy = UiKit.Button(row, "Buy", Loc.F("{0} gold", d.RecipeGold), UiKit.ButtonStyle.Primary,
                () => { if (ChefTofu.BuyRecipe(d)) Refresh(); }, 26f, UiKit.ResourceIcon(ResourceKind.Gold));
            var ble = buy.transform.parent.gameObject.AddComponent<LayoutElement>();
            ble.preferredWidth = ble.minWidth = 200f;
            ble.preferredHeight = 84f;
            var shortBy = UiKit.Text(words, "Short", "", 24f, UiKit.Danger, true);
            shortBy.overflowMode = TextOverflowModes.Overflow;
            var price = new Price
            {
                Dish = d, Button = buy, Face = buy.GetComponent<Image>(),
                Lip = buy.transform.parent.Find("Lip")?.GetComponent<Image>(),
                Labels = buy.GetComponentsInChildren<TextMeshProUGUI>(true),
                Icons = System.Array.FindAll(buy.GetComponentsInChildren<Image>(true), i => i.gameObject != buy.gameObject),
                Short = shortBy,
            };
            _prices.Add(price);
            Paint(price);
        }

        static void Tag(RectTransform row, string text, Color colour)
        {
            var t = UiKit.Text(row, "Tag", text, 24f, colour, true, TextAlignmentOptions.Center);
            t.overflowMode = TextOverflowModes.Overflow;
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = 200f;
        }
    }
}
