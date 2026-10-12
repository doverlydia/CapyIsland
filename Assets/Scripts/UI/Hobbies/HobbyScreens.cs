using System;
using System.Collections.Generic;
using System.Text;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The hobbies hub, in the spirit of Heartopia: fishing, birdwatching, bug catching, gardening,
    /// cooking, cat care, writing and sewing — each with its level and a way in. Also the bag
    /// (what you have caught, grown and cooked) and the kitchen.
    /// </summary>
    public class HobbiesScreen : UiScreen
    {
        public static HobbiesScreen Instance { get; private set; }

        public enum Page { Hub, Fish, Birds, Bugs, Cats, Bag, Kitchen }

        protected override string Title => "Hobbies";
        protected override Sprite TitleIcon => IconPainter.Rod;
        protected override Color AccentColour => UiKit.Hex("3E9C8F");

        RectTransform _list;
        ScrollRect _scroll;
        Page _page;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Show(Page page = Page.Hub)
        {
            if (Instance == null) return;
            Instance._page = page;
            Instance.Open();
        }

        protected override void Build(RectTransform body)
        {
            _scroll = UiKit.ScrollList(body, "List", out _list, 16f, new RectOffset(4, 4, 8, 32));
            UiKit.Stretch((RectTransform)_scroll.transform);
        }

        protected override void OnOpen() => Refresh();

        void Go(Page page)
        {
            _page = page;
            Refresh();
        }

        void Refresh()
        {
            ClearChildren(_list);
            TitleLabel.text = _page switch
            {
                Page.Fish => "Fish Journal", Page.Birds => "Bird Journal", Page.Bugs => "Bug Journal",
                Page.Cats => "Cats", Page.Bag => "Bag", Page.Kitchen => "Kitchen", _ => "Hobbies",
            };
            switch (_page)
            {
                case Page.Fish: Journal(Hobby.Fishing, HobbyData.Fish.Length, i => (HobbyData.Fish[i].Name, HobbyData.FishPicture(HobbyData.Fish[i]), HobbyData.Fish[i].Stars, HobbyLog.FishEntry(HobbyData.Fish[i].Id), HobbyData.Fish[i].Times, HobbyData.Fish[i].RainOnly), true); break;
                case Page.Birds: Journal(Hobby.Birding, HobbyData.Birds.Length, i => (HobbyData.Birds[i].Name, HobbyData.BirdPicture(HobbyData.Birds[i]), HobbyData.Birds[i].Stars, HobbyLog.BirdEntry(HobbyData.Birds[i].Id), HobbyData.Birds[i].Times, HobbyData.Birds[i].RainOnly), false); break;
                case Page.Bugs: Journal(Hobby.Bugs, HobbyData.Bugs.Length, i => (HobbyData.Bugs[i].Name, HobbyData.Art("Bug_" + HobbyData.Bugs[i].Id), HobbyData.Bugs[i].Stars, HobbyLog.BugEntry(HobbyData.Bugs[i].Id), HobbyData.Bugs[i].Times, HobbyData.Bugs[i].RainOnly), false); break;
                case Page.Cats: CatsPage(); break;
                case Page.Bag: BagPage(); break;
                case Page.Kitchen: KitchenPage(); break;
                default: Hub(); break;
            }
            _scroll.verticalNormalizedPosition = 1f;
        }

        // ---------------------------------------------------------------- hub

        void Hub()
        {
            var row = UiKit.Rect("Quick", _list);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            UiKit.Button(row, "Bag", "Bag", UiKit.ButtonStyle.Spring, () => Go(Page.Bag), 32f, IconPainter.Gift);
            if (HobbyLog.IsUnlocked(Hobby.Cooking))
                UiKit.Button(row, "Kitchen", "Kitchen", UiKit.ButtonStyle.Primary, () => Go(Page.Kitchen), 32f, IconPainter.Star);

            // Hobbies you have first; the ones still to take up after them, with how to get them.
            var all = new (Hobby hobby, string name, Sprite icon, Color colour, System.Action card)[]
            {
                (Hobby.Fishing, "Fishing", IconPainter.Rod, UiKit.Spring, () => HobbyCard(Hobby.Fishing, "Fishing", IconPainter.Rod, UiKit.Spring,
                    $"{HobbyLog.Fish.Count} of {HobbyData.Fish.Length} fish found",
                    "Walk to the water's edge where fish shadows swim, and tap Start fishing.",
                    "Journal", () => Go(Page.Fish))),
                (Hobby.Birding, "Birdwatching", IconPainter.Bird, UiKit.Hex("E3A92B"), () => HobbyCard(Hobby.Birding, "Birdwatching", IconPainter.Bird, UiKit.Hex("E3A92B"),
                    $"{HobbyLog.Birds.Count} of {HobbyData.Birds.Length} birds spotted",
                    "Birds land in trees and on the shore. Tap one from a few steps away. Don't rush up to it!",
                    "Journal", () => Go(Page.Birds))),
                (Hobby.Bugs, "Bug Catching", IconPainter.Sparkle, UiKit.Hex("7BAE3E"), () => HobbyCard(Hobby.Bugs, "Bug Catching", IconPainter.Sparkle, UiKit.Hex("7BAE3E"),
                    $"{HobbyLog.Bugs.Count} of {HobbyData.Bugs.Length} bugs caught",
                    "Bugs settle on flowers, trees and grass near you. Tap one to net it, gently.",
                    "Journal", () => Go(Page.Bugs))),
                (Hobby.Gardening, "Gardening", IconPainter.Sparkle, UiKit.Hex("8A5A32"), () => HobbyCard(Hobby.Gardening, "Gardening", IconPainter.Sparkle, UiKit.Hex("8A5A32"),
                    $"{HobbyLog.Plots.Count} of {Game.Gardening.MaxPlots} plots",
                    "Buy vegetable plots at the Garden Shop and put them anywhere. Tap one to plant seeds, come back when it's grown.",
                    "Show me", () => ShowInWorld(FindPlaced("Vegetable Plot") ?? FindPlaced("Garden Shop"), "Buy a vegetable plot at the Garden Shop, then put it down anywhere."))),
                (Hobby.Cooking, "Cooking", IconPainter.Flame, UiKit.Primary, () => HobbyCard(Hobby.Cooking, "Cooking", IconPainter.Flame, UiKit.Primary,
                    $"{CountDishes()} dish{(CountDishes() == 1 ? "" : "es")} in your bag",
                    "Tap your stove and drag ingredients into the pot. Islanders with a dish in a bubble over their head will pay for it.",
                    "Recipes", () => Go(Page.Kitchen))),
                (Hobby.Cats, "Cat Care", IconPainter.Heart, UiKit.Hex("E58FB0"), () => HobbyCard(Hobby.Cats, "Cat Care", IconPainter.Heart, UiKit.Hex("E58FB0"),
                    $"{AdoptedCats()} of {HobbyData.Cats.Length} cats adopted",
                    "Stray cats visit your island. Pet them and share fish until they trust you.",
                    "Cats", () => Go(Page.Cats))),
                (Hobby.Writing, "Writing", IconPainter.Book, UiKit.Hex("A77BD6"), () => HobbyCard(Hobby.Writing, "Writing", IconPainter.Book, UiKit.Hex("A77BD6"),
                    $"{HobbyLog.Books.Count} stor{(HobbyLog.Books.Count == 1 ? "y" : "ies")} written",
                    "Message bottles wash up on your beach. Read them, and buy a bottle from the sailor to leave a story of your own.",
                    "Show me", () => ShowInWorld(SeaLibrary.Spot, "The bottles are on your beach."))),
                (Hobby.Sewing, "Sewing", IconPainter.Shirt, UiKit.Hex("C2577F"), () => HobbyCard(Hobby.Sewing, "Sewing", IconPainter.Shirt, UiKit.Hex("C2577F"),
                    $"{HobbyLog.Designs.Count} piece{(HobbyLog.Designs.Count == 1 ? "" : "s")} sewn",
                    "At your sewing machine: a hat needs cotton from your garden, a cape needs silk from silkworms. Then decorate it and wear it!",
                    "Show me", () => ShowInWorld(SewingStation.Placed() != null ? SewingStation.Placed().transform : null, "Put your sewing machine down somewhere first."))),
            };
            foreach (var c in all) if (HobbyLog.IsUnlocked(c.hobby)) c.card();
            bool header = false;
            foreach (var c in all)
            {
                if (HobbyLog.IsUnlocked(c.hobby)) continue;
                if (!header) { SectionLabel(_list, "Still to discover"); header = true; }
                LockedCard(c.hobby, c.name, c.icon);
            }
        }

        /// <summary>A hobby you have not taken up yet: greyed out, with what to buy and where.</summary>
        void LockedCard(Hobby hobby, string name, Sprite icon)
        {
            var card = UiKit.Image(_list, name, UiKit.Rounded(32), new Color(1f, 1f, 1f, 0.6f)).rectTransform;
            var h = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(24, 24, 18, 18);
            h.spacing = 18f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            var disc = UiKit.Image(card, "Disc", UiKit.Circle, UiKit.Hex("CFC7BE"));
            var dle = disc.gameObject.AddComponent<LayoutElement>();
            dle.preferredWidth = dle.preferredHeight = 80f;
            var ic = UiKit.Image(disc.transform, "Icon", icon, Color.white);
            ic.preserveAspect = true;
            UiKit.Stretch(ic.rectTransform, 18f, 18f, 18f, 18f);
            var lk = UiKit.Image(disc.transform, "Lock", IconPainter.Lock, UiKit.Hex("8A7563"));
            UiKit.Place(lk.rectTransform, new Vector2(1f, 0f), new Vector2(4f, -4f), new Vector2(32f, 32f));
            var col = UiKit.Rect("Column", card);
            var cv = col.gameObject.AddComponent<VerticalLayoutGroup>();
            cv.spacing = 4f;
            cv.childControlWidth = cv.childControlHeight = true;
            cv.childForceExpandWidth = true;
            cv.childForceExpandHeight = false;
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var title = UiKit.Text(col, "Name", name, 34f, UiKit.InkSoft, true);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Text(col, "How", HobbyLog.HowToUnlock(hobby), 26f, UiKit.InkSoft);
        }

        static int CountDishes()
        {
            int n = 0;
            foreach (var e in HobbyLog.Bag) if (e.id.StartsWith("dish:")) n += e.count;
            return n;
        }

        static int AdoptedCats()
        {
            int n = 0;
            foreach (var c in HobbyLog.Cats) if (c.adopted) n++;
            return n;
        }

        void HobbyCard(Hobby hobby, string name, Sprite icon, Color colour, string stat, string tip, string action, Action open)
        {
            var card = UiKit.Image(_list, name, UiKit.Rounded(32), Color.white).rectTransform;
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(24, 24, 22, 24);
            v.spacing = 12f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var top = UiKit.Rect("Top", card);
            var h = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;

            var disc = UiKit.Image(top, "Disc", UiKit.Circle, colour);
            var dle = disc.gameObject.AddComponent<LayoutElement>();
            dle.preferredWidth = dle.preferredHeight = 96f;
            var ic = UiKit.Image(disc.transform, "Icon", icon, Color.white);
            ic.preserveAspect = true;
            UiKit.Stretch(ic.rectTransform, 20f, 20f, 20f, 20f);

            var col = UiKit.Rect("Column", top);
            var cv = col.gameObject.AddComponent<VerticalLayoutGroup>();
            cv.spacing = 6f;
            cv.childControlWidth = cv.childControlHeight = true;
            cv.childForceExpandWidth = true;
            cv.childForceExpandHeight = false;
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var title = UiKit.Text(col, "Name", $"{name}  <size=75%><color=#{ColorUtility.ToHtmlStringRGB(colour)}>Level {HobbyLog.Level(hobby)}</color></size>", 38f, UiKit.Ink, true);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            var fill = UiKit.ProgressBar(col, "Progress", colour, 18f);
            UiKit.SetProgress(fill, HobbyLog.LevelProgress(hobby));
            UiKit.Text(col, "Stat", stat, 26f, UiKit.InkSoft);

            var go = UiKit.Button(top, "Open", action, UiKit.ButtonStyle.Primary, open, 28f);
            var ble = go.transform.parent.gameObject.AddComponent<LayoutElement>();
            ble.preferredWidth = 170f; ble.preferredHeight = 72f;

            var t = UiKit.Text(card, "Tip", tip, 26f, UiKit.InkSoft);
            t.overflowMode = TextOverflowModes.Overflow;
        }

        void Back()
        {
            var back = UiKit.Button(_list, "Back", "Back to hobbies", UiKit.ButtonStyle.Secondary, () => Go(Page.Hub), 28f);
            back.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;
        }

        // ---------------------------------------------------------------- journals

        GridLayoutGroup Grid(int count, float aspect = 1.25f)
        {
            var grid = UiKit.Rect("Grid", _list);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            float width = Sheet.sizeDelta.x - 64f - 8f;
            int columns = Mathf.Clamp(Mathf.FloorToInt(width / 230f), 3, 5);
            float cell = (width - 14f * (columns - 1)) / columns;
            g.cellSize = new Vector2(cell, cell * aspect);
            g.spacing = new Vector2(14f, 14f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columns;
            int rows = Mathf.CeilToInt(count / (float)columns);
            grid.gameObject.AddComponent<LayoutElement>().preferredHeight = rows * (cell * aspect + 14f);
            return g;
        }

        void Journal(Hobby hobby, int count, Func<int, (string name, Sprite pic, int stars, HobbyLog.Entry entry, DayParts times, bool rain)> get, bool sizes)
        {
            Back();
            JournalHeader(hobby);
            var grid = Grid(count);
            for (int i = 0; i < count; i++)
            {
                var (name, pic, stars, entry, times, rain) = get(i);
                bool known = entry != null;
                var tile = UiKit.Image(grid.transform, name, UiKit.Rounded(26), known ? Color.white : UiKit.SurfaceSunk);
                Picture(tile.transform, pic, known);
                Label(tile.transform, known ? name : "???", known);
                // When it is about, even before you have found one: something to go looking for.
                string detail = HobbyData.Rarity(stars) + "\n" + (rain ? "Only in the rain" : GameClock.Describe(times));
                if (known) detail += sizes ? $"\n×{entry.count} · best {entry.best:0} cm" : $"\n×{entry.count}";
                Detail(tile.transform, detail);
            }
        }

        /// <summary>How much of the page is found, and what a full page wins.</summary>
        void JournalHeader(Hobby hobby)
        {
            int found = JournalRewards.Found(hobby), total = JournalRewards.Total(hobby);
            var hat = JournalRewards.Hat(hobby);
            string line = JournalRewards.Rewarded(hobby)
                ? $"Complete! You found all {total}."
                : $"Found {found} of {total}. Find them all for {JournalRewards.Gems} gems" + (hat != null ? " and the " + hat.Name + "." : ".");
            var t = UiKit.Text(_list, "Progress", line, 28f, UiKit.Ink, true, TextAlignmentOptions.Center);
            t.overflowMode = TextOverflowModes.Overflow;
            var bar = UiKit.ProgressBar(_list, "Bar", UiKit.Leaf, 20f);
            UiKit.SetProgress(bar, total > 0 ? found / (float)total : 0f);
        }

        static void Picture(Transform tile, Sprite pic, bool known)
        {
            var p = UiKit.Image(tile, "Picture", pic, known ? Color.white : new Color(0.2f, 0.16f, 0.13f, 0.55f));
            p.preserveAspect = true;
            p.enabled = pic != null;
            p.rectTransform.anchorMin = new Vector2(0.08f, 0.38f);
            p.rectTransform.anchorMax = new Vector2(0.92f, 0.95f);
            p.rectTransform.offsetMin = p.rectTransform.offsetMax = Vector2.zero;
        }

        static void Label(Transform tile, string text, bool strong)
        {
            var label = UiKit.Text(tile, "Name", text, 24f, strong ? UiKit.Ink : UiKit.InkSoft, true, TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = new Vector2(0f, 0.2f);
            label.rectTransform.anchorMax = new Vector2(1f, 0.38f);
            label.rectTransform.offsetMin = new Vector2(6f, 0f);
            label.rectTransform.offsetMax = new Vector2(-6f, 0f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 24f;
        }

        static void Detail(Transform tile, string text)
        {
            var d = UiKit.Text(tile, "Detail", text, 20f, UiKit.Hex("B98A1E"), false, TextAlignmentOptions.Center);
            d.rectTransform.anchorMin = Vector2.zero;
            d.rectTransform.anchorMax = new Vector2(1f, 0.22f);
            d.rectTransform.offsetMin = new Vector2(4f, 4f);
            d.rectTransform.offsetMax = new Vector2(-4f, 0f);
            d.enableAutoSizing = true;
            d.fontSizeMin = 14f;
            d.fontSizeMax = 20f;
        }

        void CatsPage()
        {
            Back();
            var grid = Grid(HobbyData.Cats.Length);
            foreach (var cat in HobbyData.Cats)
            {
                HobbyLog.Cat state = null;
                foreach (var c in HobbyLog.Cats) if (c.id == cat.Id) state = c;
                bool met = state != null && state.bond > 0;
                bool adopted = state != null && state.adopted;
                var tile = UiKit.Image(grid.transform, cat.Name, UiKit.Rounded(26), adopted ? UiKit.Hex("FDE8F0") : met ? Color.white : UiKit.SurfaceSunk);
                Picture(tile.transform, HobbyData.Art("Cat_" + cat.Id), met);
                Label(tile.transform, adopted ? cat.Name : met ? "Stray cat" : "???", met);
                Detail(tile.transform, adopted ? "Adopted!" : met ? $"Trust {state.bond}/{CatCare.MaxBond}" : "Not met yet");
            }
            var tip = UiKit.Text(_list, "Tip", "Strays wander onto your island every so often. Pet them, share a fish, and once they trust you, adopt them.", 26f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            tip.overflowMode = TextOverflowModes.Overflow;
        }

        // ---------------------------------------------------------------- bag and kitchen

        void BagPage()
        {
            Back();
            if (HobbyLog.Bag.Count == 0)
            {
                var empty = UiKit.Text(_list, "Empty", "Your bag is empty. Catch fish, grow vegetables and cook dishes to fill it.", 28f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
                empty.overflowMode = TextOverflowModes.Overflow;
                return;
            }
            var items = new List<HobbyLog.Entry>(HobbyLog.Bag);
            items.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            foreach (var e in items)
            {
                // Dishes go to whoever fancies them: look for a bubble over someone's head.
                bool dish = e.id.StartsWith("dish:") && e.id != "dish:" + Cooking.Bizarre;
                ItemRow(e.id, dish ? $"×{e.count}  ·  <size=85%>Someone might fancy this — look for a bubble!</size>" : $"×{e.count}");
            }
        }

        void KitchenPage()
        {
            Back();
            // A recipe book: the cooking itself happens at your stove.
            var stove = CookingStove.Placed();
            var go = UiKit.Button(_list, "Stove", stove != null ? "Go to my stove" : "Find a stove at the Garden Shop",
                UiKit.ButtonStyle.Primary, () => ShowInWorld(stove != null ? stove.transform : FindPlaced("Garden Shop"),
                    stove != null ? null : "The Garden Shop opens up after the Legendary Gem Store. It sells the Cooking Stove."), 30f, IconPainter.Flame);
            go.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            int level = HobbyLog.Level(Hobby.Cooking);
            foreach (var dish in HobbyData.Dishes)
            {
                bool known = dish.MinLevel <= level;
                var needs = new StringBuilder();
                foreach (var (item, count) in dish.Needs)
                {
                    if (needs.Length > 0) needs.Append(", ");
                    bool have = HobbyLog.Has(item, count);
                    // A list no table can hold: each name is translated as it goes in.
                    needs.Append(have ? "" : "<color=#C0503E>").Append(count).Append(' ').Append(Loc.T(HobbyData.ItemName(item))).Append(have ? "" : "</color>");
                }
                string line = known ? needs.ToString() + "\n<size=85%>" + dish.Blurb + "</size>" : $"Unlocks at cooking level {dish.MinLevel}";
                ItemRow("dish:" + dish.Id, line, known ? dish.Name : "???");
            }
        }

        /// <summary>A placed building of this kind on the island, if any.</summary>
        static Transform FindPlaced(string storeName)
        {
            foreach (var tag in FindObjectsByType<CatalogItem>(FindObjectsSortMode.None))
            {
                if (tag.StoreName != storeName) continue;
                var p = tag.GetComponent<Placeable>();
                if (p != null && p.Placed) return tag.transform;
            }
            return null;
        }

        /// <summary>Closes the menu and walks over to something on the island — or says where to find it.</summary>
        void ShowInWorld(Transform target, string otherwise)
        {
            if (target == null) { if (otherwise != null) Hints.Show(otherwise); Close(); return; }
            Close();
            PlayerAvatar.GoTo(target.position);
        }

        RectTransform ItemRow(string item, string detail, string name = null)
        {
            var row = UiKit.Image(_list, item, UiKit.Rounded(28), Color.white).rectTransform;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(18, 18, 14, 14);
            h.spacing = 18f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().minHeight = 110f;
            var pic = UiKit.Image(row, "Picture", HobbyData.ItemArt(item), Color.white);
            pic.preserveAspect = true;
            pic.enabled = pic.sprite != null;
            var ple = pic.gameObject.AddComponent<LayoutElement>();
            ple.preferredWidth = ple.preferredHeight = 84f;
            var col = UiKit.Rect("Column", row);
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            UiKit.Text(col, "Name", name ?? HobbyData.ItemName(item), 32f, UiKit.Ink, true);
            var t = UiKit.Text(col, "Detail", detail, 26f, UiKit.InkSoft);
            t.overflowMode = TextOverflowModes.Overflow;
            return row;
        }
    }

    /// <summary>
    /// Books: the ones you have written, and the ones your friends have written — on a visit, the
    /// shelf of whoever's island you are on. Write a new one with a title and a story.
    /// </summary>
    public class LibraryScreen : UiScreen
    {
        public static LibraryScreen Instance { get; private set; }

        protected override string Title => "Library";
        protected override Sprite TitleIcon => IconPainter.Book;
        protected override Color AccentColour => UiKit.Hex("A77BD6");

        RectTransform _list;
        ScrollRect _scroll;
        List<Button> _tabs;
        int _tab;
        HashSet<string> _read = new();

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override void Build(RectTransform body)
        {
            _tabs = Tabs(body, new[] { "Friends' books", "My books" }, i => { _tab = i; Refresh(); }, out var bar);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, 92f);
            _scroll = UiKit.ScrollList(body, "List", out _list, 14f, new RectOffset(4, 4, 8, 32));
            UiKit.Stretch((RectTransform)_scroll.transform, 0f, 0f, 0f, 108f);
        }

        protected override void OnOpen() => Refresh();

        void Refresh()
        {
            HighlightTab(_tabs, _tab, AccentColour);
            ClearChildren(_list);
            if (_tab == 0) FriendsBooks();
            else MyBooks();
            _scroll.verticalNormalizedPosition = 1f;
        }

        void FriendsBooks()
        {
            var shelves = new List<FriendProfile>();
            if (Visit.Active) shelves.Add(Visit.Host);
            foreach (var f in Friends.All) if (!Visit.Active || f.id != Visit.Host.id) shelves.Add(f);
            if (shelves.Count == 0)
            {
                Note("Make friends to read their books. Visit an island from the Friends menu!");
                return;
            }
            foreach (var who in shelves)
            {
                SectionLabel(_list, Visit.Active && who.id == Visit.Host.id ? who.name + "'s shelf (you're here)" : who.name + "'s shelf");
                foreach (var book in BotBooks.ShelfOf(who))
                    BookRow(book.Title, "by " + who.name, () => Read(book.Title, "by " + who.name, book.Text, who.name + "/" + book.Title));
            }
        }

        void MyBooks()
        {
            var write = UiKit.Button(_list, "Write", "Write a new book", UiKit.ButtonStyle.Primary, Write, 34f, IconPainter.Book);
            write.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 100f;
            if (HobbyLog.Books.Count == 0) Note("Your shelf is empty. Every great author starts with one page.");
            for (int i = HobbyLog.Books.Count - 1; i >= 0; i--)
            {
                var b = HobbyLog.Books[i];
                int index = i;
                BookRow(b.title, new DateTime(b.writtenTicks).ToString("d MMM yyyy"), () => Read(b.title, "by you", b.text, null, index));
            }
        }

        void BookRow(string title, string by, Action open)
        {
            var row = UiKit.Image(_list, title, UiKit.Rounded(28), Color.white).rectTransform;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(20, 20, 16, 16);
            h.spacing = 18f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().minHeight = 116f;
            var cover = UiKit.Image(row, "Cover", UiKit.Rounded(14), Color.HSVToRGB(Mathf.Repeat(title.GetHashCode() * 0.0001f, 1f), 0.45f, 0.85f));
            var cle = cover.gameObject.AddComponent<LayoutElement>();
            cle.preferredWidth = 64f; cle.preferredHeight = 84f;
            var ic = UiKit.Image(cover.transform, "Icon", IconPainter.Book, new Color(1f, 1f, 1f, 0.85f));
            UiKit.Stretch(ic.rectTransform, 12f, 22f, 12f, 22f);
            var col = UiKit.Rect("Column", row);
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            UiKit.Text(col, "Title", title, 32f, UiKit.Ink, true);
            UiKit.Text(col, "By", by, 24f, UiKit.InkSoft);
            var read = UiKit.Button(row, "Read", "Read", UiKit.ButtonStyle.Secondary, open, 28f);
            var rle = read.transform.parent.gameObject.AddComponent<LayoutElement>();
            rle.preferredWidth = 140f; rle.preferredHeight = 70f;
        }

        void Note(string text)
        {
            var t = UiKit.Text(_list, "Note", text, 28f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            t.overflowMode = TextOverflowModes.Overflow;
            t.margin = new Vector4(20f, 10f, 20f, 10f);
        }

        /// <summary>Opens a book on the page.</summary>
        void Read(string title, string by, string text, string readKey, int mine = -1)
        {
            ClearChildren(_list);
            var back = UiKit.Button(_list, "Back", "Back to the shelf", UiKit.ButtonStyle.Secondary, Refresh, 28f);
            back.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;
            var page = UiKit.Image(_list, "Page", UiKit.Rounded(30), UiKit.Hex("FFF8E8")).rectTransform;
            var v = page.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(40, 40, 36, 44);
            v.spacing = 10f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            var t = UiKit.Text(page, "Title", title, 44f, UiKit.Ink, true, TextAlignmentOptions.Center);
            t.overflowMode = TextOverflowModes.Overflow;
            UiKit.Text(page, "By", by, 26f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            var body = UiKit.Text(page, "Text", text, 32f, UiKit.Ink);
            body.overflowMode = TextOverflowModes.Overflow;
            body.lineSpacing = 12f;
            body.margin = new Vector4(0f, 20f, 0f, 0f);
            if (mine >= 0)
            {
                var del = UiKit.Button(_list, "Delete", "Take off the shelf", UiKit.ButtonStyle.Secondary, () => { HobbyLog.RemoveBook(mine); Refresh(); }, 26f);
                del.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;
            }
            _scroll.verticalNormalizedPosition = 1f;

            // A little reading XP, once per book.
            if (readKey != null && _read.Add(readKey) && PlayerPrefs.GetInt("CapyIsland.Read." + readKey.GetHashCode(), 0) == 0)
            {
                PlayerPrefs.SetInt("CapyIsland.Read." + readKey.GetHashCode(), 1);
                HobbyLog.AddXp(Hobby.Writing, 5);
            }
        }

        /// <summary>Opens straight onto a story from a bottle on the beach.</summary>
        public static void ReadBottle(string title, string by, string text)
        {
            if (Instance == null) return;
            Instance.Open();
            Instance.Read(title, by, text, by + "/" + title);
        }

        /// <summary>Opens the page to write the story going into a bottle; <paramref name="written"/> gets it.</summary>
        public static void WriteBottle(Action<string, string> written, string title = null, string text = null, Action cancelled = null)
        {
            if (Instance == null) return;
            Instance.Open();
            Instance.Write(written, title, text, cancelled);
        }

        void Write() => Write(null);

        void Write(Action<string, string> written, string startTitle = null, string startText = null, Action cancelled = null)
        {
            ClearChildren(_list);
            var title = UiKit.Input(_list, "Title", "Title", false, 96f);
            title.characterLimit = 40;
            var text = UiKit.Input(_list, "Story", "Once upon a time, on a little island...", true, 640f);
            text.characterLimit = 2000;
            if (!string.IsNullOrEmpty(startTitle)) title.text = startTitle;
            if (!string.IsNullOrEmpty(startText)) text.text = startText;
            var row = UiKit.Rect("Buttons", _list);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            UiKit.Button(row, "Cancel", "Cancel", UiKit.ButtonStyle.Secondary, () =>
            {
                if (cancelled == null) { Refresh(); return; }
                Close();
                cancelled();
            }, 30f);
            // What is missing, said right here (Old Yuzu keeps quiet while a page is open).
            var problem = UiKit.Text(_list, "Problem", "", 28f, new Color(0.78f, 0.25f, 0.22f), true, TMPro.TextAlignmentOptions.Center);
            problem.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
            problem.gameObject.SetActive(false);
            UiKit.Button(row, "Save", written != null ? "Put it in the bottle" : "Put it on my shelf", UiKit.ButtonStyle.Primary, () =>
            {
                string t = title.text.Trim(), s = text.text.Trim();
                // A bottle note can be short and needs no title; a story for the shelf wants both.
                string missing = written != null
                    ? (s.Length == 0 ? "Write a few words first." : null)
                    : (t.Length == 0 || s.Length < 20 ? "A story needs a title and at least a few sentences." : null);
                if (missing != null)
                {
                    problem.text = missing;
                    problem.gameObject.SetActive(true);
                    problem.transform.DOKill(true);
                    problem.transform.DOPunchScale(Vector3.one * 0.08f, 0.3f, 6).SetUpdate(true);
                    return;
                }
                if (t.Length == 0) t = "Message in a Bottle";
                if (written != null)
                {
                    Close();
                    written(t, s);
                    return;
                }
                HobbyLog.AddBook(t, s);
                HobbyLog.AddXp(Hobby.Writing, Mathf.Clamp(s.Length / 20, 10, 40));
                Motes.Trigger("hobby_book");
                Hints.Show("\"" + t + "\" is on your shelf. Friends who visit can read it!");
                Refresh();
            }, 30f);
            _scroll.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>
    /// The sewing machine: pick a pattern — a hat (3 cotton from your garden) or a cape (3 silk
    /// from silkworms) — paint it pixel by pixel from a little palette, and save it. Every piece
    /// you sew goes into your wardrobe (hats with the hats, capes on the back) to wear.
    /// </summary>
    public class SewingScreen : UiScreen
    {
        public static SewingScreen Instance { get; private set; }

        public const int Size = 16;
        public static readonly Color[] Palette =
        {
            UiKit.Hex("FFFFFF"), UiKit.Hex("2B2B2B"), UiKit.Hex("E8553E"), UiKit.Hex("FF8A3D"), UiKit.Hex("FFC93C"), UiKit.Hex("5DBB63"),
            UiKit.Hex("2DB3A6"), UiKit.Hex("5B9BD5"), UiKit.Hex("A77BD6"), UiKit.Hex("E58FB0"), UiKit.Hex("8A5A32"), UiKit.Hex("C9C9C9"),
        };

        /// <summary>What can be sewn, and what it takes.</summary>
        public static readonly (string kind, string name, string material, int count, string where)[] Patterns =
        {
            ("hat", "Hat", "crop:Cotton", 3, "Grow cotton in your garden (gardening level 2)."),
            ("cape", "Cape", "mat:Silk", 3, "Catch silkworms for their silk (bug catching level 2)."),
        };

        protected override string Title => "Sewing";
        protected override Sprite TitleIcon => IconPainter.Shirt;
        protected override Color AccentColour => UiKit.Hex("E58FB0");

        Texture2D _texture;
        RawImage _canvas;
        Image _silhouette;
        int _colour = 2;
        bool _erase;
        readonly char[] _pixels = new char[Size * Size];
        readonly List<Image> _swatches = new();
        RectTransform _designs, _patternRow;
        int _editing = -1;
        int _pattern;
        TextMeshProUGUI _status;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        string Kind => Patterns[_pattern].kind;

        /// <summary>The outline of a pattern, as a signed distance (negative inside). Shirts are the old designs.</summary>
        public static float Shape(string kind, float u, float v)
        {
            switch (kind)
            {
                case "hat":
                {
                    // A beanie: a round crown on a turned-up brim, and a bobble on top.
                    float crown = IconPainter.Disc(u, v, 0f, -0.05f, 0.62f);
                    crown = Mathf.Max(crown, -0.2f - v);   // flat at the bottom, where the brim goes
                    float brim = IconPainter.Box(u, v, 0f, -0.33f, 0.8f, 0.16f, 0.1f);
                    float bobble = IconPainter.Disc(u, v, 0f, 0.66f, 0.17f);
                    return Mathf.Min(Mathf.Min(crown, brim), bobble);
                }
                case "cape":
                {
                    // Narrow at the shoulders, flaring out to a wavy hem, with a collar.
                    float t = Mathf.InverseLerp(0.7f, -0.85f, v);
                    float half = Mathf.Lerp(0.36f, 0.86f, t);
                    float side = Mathf.Abs(u) - half;
                    float top = v - 0.7f, bottom = -0.85f + Mathf.Sin(u * 9f) * 0.04f - v;
                    float body = Mathf.Max(side, Mathf.Max(top, bottom));
                    float collar = IconPainter.Box(u, v, 0f, 0.74f, 0.42f, 0.09f, 0.08f);
                    return Mathf.Min(body, collar);
                }
                default:
                    return IconPainter.ShirtShape(u, v);
            }
        }

        static bool Inside(string kind, int x, int y)
        {
            float u = (x + 0.5f) / Size * 2f - 1f, v = (y + 0.5f) / Size * 2f - 1f;
            return Shape(kind, u, v) < 0f;
        }

        protected override void Build(RectTransform body)
        {
            var scroll = UiKit.ScrollList(body, "List", out var list, 16f, new RectOffset(4, 4, 8, 32));
            UiKit.Stretch((RectTransform)scroll.transform);

            // Which pattern.
            _patternRow = UiKit.Rect("Patterns", list);
            var ph = _patternRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            ph.spacing = 14f;
            ph.childControlWidth = ph.childControlHeight = true;
            ph.childForceExpandWidth = true;
            _patternRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;

            // The canvas: the pattern's silhouette under a painted 16×16 texture.
            var frame = UiKit.Rect("Canvas", list);
            frame.gameObject.AddComponent<LayoutElement>().preferredHeight = 520f;
            var mat = UiKit.Image(frame, "Mat", UiKit.Rounded(36), UiKit.Hex("CFE3E0"));
            UiKit.Place(mat.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 520f));
            mat.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _silhouette = UiKit.Image(frame, "Silhouette", null, Color.white);
            _silhouette.preserveAspect = true;
            UiKit.Place(_silhouette.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 500f));
            _silhouette.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var raw = UiKit.Rect("Paint", _silhouette.transform);
            UiKit.Stretch(raw);
            _canvas = raw.gameObject.AddComponent<RawImage>();
            _canvas.texture = _texture;
            raw.gameObject.AddComponent<PaintSurface>().Owner = this;

            _status = UiKit.Text(list, "Status", "Tap or drag to paint.", 26f, UiKit.InkSoft, false, TextAlignmentOptions.Center);

            // Palette.
            var pal = UiKit.Rect("Palette", list);
            var grid = pal.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(64f, 64f);
            grid.spacing = new Vector2(10f, 10f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 7;
            pal.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;
            for (int i = 0; i < Palette.Length; i++)
            {
                int index = i;
                var sw = UiKit.Image(pal, "Swatch" + i, UiKit.Circle, Palette[i], true);
                var b = sw.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { _colour = index; _erase = false; Swatches(); });
                _swatches.Add(sw);
            }
            var eraser = UiKit.Image(pal, "Eraser", UiKit.Circle, UiKit.SurfaceSunk, true);
            var x = UiKit.Image(eraser.transform, "X", IconPainter.Close, UiKit.InkSoft);
            UiKit.Stretch(x.rectTransform, 16f, 16f, 16f, 16f);
            var eb = eraser.gameObject.AddComponent<Button>();
            eb.transition = Selectable.Transition.None;
            eb.onClick.AddListener(() => { _erase = true; Swatches(); });
            _swatches.Add(eraser);

            var row = UiKit.Rect("Buttons", list);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 90f;
            UiKit.Button(row, "Clear", "Clear", UiKit.ButtonStyle.Secondary, () => { Clear(); _editing = -1; Status("A fresh piece of fabric."); }, 28f);
            UiKit.Button(row, "Save", "Sew it", UiKit.ButtonStyle.Spring, () => Save(false), 28f);
            UiKit.Button(row, "Wear", "Sew & wear", UiKit.ButtonStyle.Primary, () => Save(true), 28f);

            SectionLabel(list, "Your sewing");
            _designs = UiKit.Rect("Designs", list);
            var dg = _designs.gameObject.AddComponent<GridLayoutGroup>();
            dg.cellSize = new Vector2(150f, 150f);
            dg.spacing = new Vector2(14f, 14f);
            dg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            dg.constraintCount = 5;
            _designs.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Clear();
        }

        protected override void OnOpen()
        {
            Swatches();
            Patterns_();
            Designs();
            Silhouette();
        }

        void Patterns_()
        {
            ClearChildren(_patternRow);
            for (int i = 0; i < Patterns.Length; i++)
            {
                int index = i;
                var p = Patterns[i];
                int have = HobbyLog.Count(p.material);
                string label = $"{p.name}  <size=75%>{have}/{p.count} {HobbyData.ItemName(p.material).ToLower()}</size>";
                var b = UiKit.Button(_patternRow, p.name, label, index == _pattern ? UiKit.ButtonStyle.Primary : UiKit.ButtonStyle.Secondary, () =>
                {
                    if (_pattern == index) return;
                    _pattern = index;
                    _editing = -1;
                    Clear();
                    Patterns_();
                    Silhouette();
                    Status(HobbyLog.Count(Patterns[index].material) >= Patterns[index].count ? "Paint your " + Patterns[index].name.ToLower() + "!" : Patterns[index].where);
                }, 28f);
            }
        }

        void Silhouette() => _silhouette.sprite = DesignSprite(null, Kind);

        void Swatches()
        {
            for (int i = 0; i < _swatches.Count; i++)
            {
                bool on = _erase ? i == _swatches.Count - 1 : i == _colour;
                _swatches[i].transform.localScale = Vector3.one * (on ? 1.18f : 0.92f);
            }
        }

        void Clear()
        {
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = '.';
            Redraw();
        }

        internal void PaintAt(Vector2 normalised)
        {
            int x = Mathf.FloorToInt(normalised.x * Size), y = Mathf.FloorToInt(normalised.y * Size);
            if (x < 0 || y < 0 || x >= Size || y >= Size || !Inside(Kind, x, y)) return;
            char c = _erase ? '.' : (char)('a' + _colour);
            if (_pixels[y * Size + x] == c) return;
            _pixels[y * Size + x] = c;
            Redraw();
        }

        void Redraw()
        {
            var px = new Color32[Size * Size];
            for (int i = 0; i < px.Length; i++)
                px[i] = _pixels[i] == '.' ? new Color32(0, 0, 0, 0) : (Color32)Palette[_pixels[i] - 'a'];
            _texture.SetPixels32(px);
            _texture.Apply();
        }

        /// <summary>Sews it: a new piece uses its material; changing one you made before is free.</summary>
        void Save(bool wear)
        {
            var p = Patterns[_pattern];
            bool fresh = _editing < 0;
            if (fresh && !HobbyLog.Take(p.material, p.count))
            {
                Status($"A {p.name.ToLower()} needs {p.count} {HobbyData.ItemName(p.material).ToLower()}. {p.where}");
                return;
            }
            string data = new string(_pixels);
            string name = fresh ? $"My {p.name} {CountOf(p.kind) + 1}" : HobbyLog.Designs[_editing].name;
            _editing = HobbyLog.SaveDesign(name, data, Size, _editing, p.kind);
            Wardrobe.Sewn(_editing);
            if (fresh)
            {
                HobbyLog.AddXp(Hobby.Sewing, 20);
                Motes.Trigger("hobby_sew");
            }
            var item = Wardrobe.Find(Wardrobe.DesignId(_editing));
            if (wear && item != null && Wardrobe.Worn(item.Slot)?.Id != item.Id) Wardrobe.Toggle(item);
            Status(wear ? "You're wearing it! Have a look around the island." : "Sewn! It's in your wardrobe.");
            Patterns_();
            Designs();
        }

        static int CountOf(string kind)
        {
            int n = 0;
            foreach (var d in HobbyLog.Designs) if (d.kind == kind) n++;
            return n;
        }

        void Status(string text) => _status.text = text;

        void Designs()
        {
            ClearChildren(_designs);
            for (int i = 0; i < HobbyLog.Designs.Count; i++)
            {
                int index = i;
                var d = HobbyLog.Designs[i];
                var tile = UiKit.Image(_designs, d.name, UiKit.Rounded(22), Color.white, true);
                var pic = UiKit.Image(tile.transform, "Piece", DesignSprite(d.pixels, d.kind), Color.white);
                pic.preserveAspect = true;
                UiKit.Stretch(pic.rectTransform, 12f, 12f, 12f, 12f);
                var b = tile.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() =>
                {
                    var design = HobbyLog.Designs[index];
                    int pattern = System.Array.FindIndex(Patterns, x => x.kind == design.kind);
                    if (pattern < 0) { Status("That's one of your old shirts. Shirts can't be changed any more."); return; }
                    _pattern = pattern;
                    _editing = index;
                    for (int k = 0; k < _pixels.Length && k < design.pixels.Length; k++) _pixels[k] = design.pixels[k];
                    Redraw();
                    Patterns_();
                    Silhouette();
                    Status("Changing " + design.name + ".");
                });
            }
        }

        /// <summary>A sewn piece as a finished picture: fabric where unpainted, seamed, pixels crisp.</summary>
        public static Sprite DesignSprite(string pixels, string kind = "", int res = 64)
        {
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color32[res * res];
            var fabric = UiKit.Hex("F4EBDD");
            var seam = UiKit.Hex("8A6A52");
            for (int j = 0; j < res; j++)
            for (int i = 0; i < res; i++)
            {
                float u = (i + 0.5f) / res * 2f - 1f, v = (j + 0.5f) / res * 2f - 1f;
                float d = Shape(kind, u, v);
                if (d > 0.02f) { px[j * res + i] = new Color32(0, 0, 0, 0); continue; }
                if (d > -0.03f) { px[j * res + i] = seam; continue; }
                int x = Mathf.Clamp(i * Size / res, 0, Size - 1), y = Mathf.Clamp(j * Size / res, 0, Size - 1);
                char c = pixels != null && y * Size + x < pixels.Length ? pixels[y * Size + x] : '.';
                px[j * res + i] = c == '.' || c - 'a' >= Palette.Length ? fabric : Palette[c - 'a'];
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Turns taps and drags on the canvas into paint.</summary>
        class PaintSurface : MonoBehaviour, IPointerDownHandler, IDragHandler
        {
            public SewingScreen Owner;
            public void OnPointerDown(PointerEventData e) => Paint(e);
            public void OnDrag(PointerEventData e) => Paint(e);

            void Paint(PointerEventData e)
            {
                var r = (RectTransform)transform;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(r, e.position, e.pressEventCamera, out var local)) return;
                var size = r.rect.size;
                Owner.PaintAt(new Vector2(local.x / size.x + 0.5f, local.y / size.y + 0.5f));
            }
        }
    }
}
