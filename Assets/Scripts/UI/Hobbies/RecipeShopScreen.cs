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

        void Awake() => Instance = this;

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

            var intro = UiKit.Text(_list, "Intro", "A recipe shows what goes into a dish, and islanders only ask for dishes you know. Put the right things in the pot and you might work one out for yourself!", 28f, UiKit.InkSoft);
            intro.overflowMode = TextOverflowModes.Overflow;

            // For sale first, then what's coming, then what you know.
            var rows = new List<DishKind>(HobbyData.Dishes);
            int Order(DishKind d) => HobbyLog.KnowsRecipe(d.Id) ? 2 : ChefTofu.WhyNot(d) != null || d.FreeWith != null ? 1 : 0;
            rows.Sort((a, b) => Order(a) != Order(b) ? Order(a).CompareTo(Order(b)) : a.MinLevel.CompareTo(b.MinLevel));
            foreach (var d in rows) Row(d);
        }

        void Row(DishKind d)
        {
            bool known = HobbyLog.KnowsRecipe(d.Id);
            string why = ChefTofu.WhyNot(d);
            bool levelLocked = !known && why != null;

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
            if (known)
            {
                Tag(row, "Known", UiKit.Leaf);
                return;
            }
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
            var store = ResourceStore.Instance;
            var buy = UiKit.Button(row, "Buy", Loc.F("{0} gold", d.RecipeGold), UiKit.ButtonStyle.Primary, () => { if (ChefTofu.BuyRecipe(d)) Refresh(); }, 26f,
                UiKit.ResourceIcon(ResourceKind.Gold));
            var ble = buy.transform.parent.gameObject.AddComponent<LayoutElement>();
            ble.preferredWidth = ble.minWidth = 200f;
            ble.preferredHeight = 84f;
            buy.interactable = store != null && store.CanAfford(0, d.RecipeGold, 0);
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
