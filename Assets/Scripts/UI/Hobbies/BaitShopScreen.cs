using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Skipper Nori's bait box: each bait with what it does, how many you have, and a pack to buy.
    /// </summary>
    public class BaitShopScreen : UiScreen
    {
        public static BaitShopScreen Instance { get; private set; }

        protected override string Title => "Bait Shop";
        protected override Sprite TitleIcon => IconPainter.Rod;
        protected override Color AccentColour => UiKit.Hex("3E9C8F");
        protected override float MaxWidth => 900f;

        readonly List<(BaitKind bait, TextMeshProUGUI have, Button buy)> _rows = new();

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override void Build(RectTransform body)
        {
            var scroll = UiKit.ScrollList(body, "List", out var list, 18f);
            UiKit.Stretch((RectTransform)scroll.transform);

            var intro = UiKit.Text(list, "Intro", "Bait goes on your hook as you cast, one per cast. Tap Tackle while fishing to choose.", 28f, UiKit.InkSoft);
            intro.overflowMode = TextOverflowModes.Overflow;

            foreach (var bait in Baits.All)
            {
                var b = bait;
                var row = UiKit.Image(list, b.Id, UiKit.Rounded(30), Color.white).rectTransform;
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.padding = new RectOffset(20, 20, 18, 18);
                h.spacing = 18f;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = h.childForceExpandHeight = false;
                RtlLayout.Add(h);

                var pic = UiKit.Image(row, "Picture", HobbyData.ItemArt(b.Item), Color.white);
                pic.preserveAspect = true;
                var ple = pic.gameObject.AddComponent<LayoutElement>();
                ple.preferredWidth = ple.preferredHeight = ple.minWidth = 96f;

                var words = UiKit.Rect("Words", row);
                var v = words.gameObject.AddComponent<VerticalLayoutGroup>();
                v.spacing = 4f;
                v.childControlWidth = v.childControlHeight = true;
                v.childForceExpandWidth = true;
                v.childForceExpandHeight = false;
                words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                var name = UiKit.Text(words, "Name", b.Name, 34f, UiKit.Ink, true);
                name.overflowMode = TextOverflowModes.Overflow;
                var blurb = UiKit.Text(words, "Blurb", b.Blurb, 26f, UiKit.InkSoft);
                blurb.overflowMode = TextOverflowModes.Overflow;
                var have = UiKit.Text(words, "Have", "", 24f, UiKit.Hex("B98A1E"), true);

                string price = b.Gems > 0 ? $"{b.Pack} for {b.Gems} gems" : $"{b.Pack} for {b.Gold} gold";
                var buy = UiKit.Button(row, "Buy", price, UiKit.ButtonStyle.Primary, () => { if (Baits.Buy(b)) Refresh(); }, 26f,
                    UiKit.ResourceIcon(b.Gems > 0 ? ResourceKind.Gem : ResourceKind.Gold));
                var ble = buy.transform.parent.gameObject.AddComponent<LayoutElement>();
                ble.preferredWidth = ble.minWidth = 230f;
                ble.preferredHeight = 84f;
                // As wide as its price at Easy Reading's size (the coin, the gap and the button's
                // padding beside it), and never squeezed below that by the blurb next to it.
                FitsText.Add(ble, buy.GetComponentInChildren<TMP_Text>(), new Vector2(80f, 0f), firm: true);
                _rows.Add((b, have, buy));
            }
        }

        protected override void OnOpen() => Refresh();

        void Refresh()
        {
            var store = ResourceStore.Instance;
            foreach (var (bait, have, buy) in _rows)
            {
                int n = Baits.Count(bait);
                have.text = n > 0 ? $"You have {n}" : "";
                buy.interactable = store != null && store.CanAfford(0, bait.Gold, bait.Gems);
            }
        }
    }
}
