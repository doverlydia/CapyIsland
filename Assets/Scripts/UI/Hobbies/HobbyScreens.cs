using System;
using System.Collections.Generic;
using System.Text;
using CapyIsland.Game;
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
                case Page.Fish: Journal(HobbyData.Fish.Length, i => (HobbyData.Fish[i].Name, HobbyData.FishPicture(HobbyData.Fish[i]), HobbyData.Fish[i].Stars, HobbyLog.FishEntry(HobbyData.Fish[i].Id)), true); break;
                case Page.Birds: Journal(HobbyData.Birds.Length, i => (HobbyData.Birds[i].Name, HobbyData.BirdPicture(HobbyData.Birds[i]), HobbyData.Birds[i].Stars, HobbyLog.BirdEntry(HobbyData.Birds[i].Id)), false); break;
                case Page.Bugs: Journal(HobbyData.Bugs.Length, i => (HobbyData.Bugs[i].Name, HobbyData.Art("Bug_" + HobbyData.Bugs[i].Id), HobbyData.Bugs[i].Stars, HobbyLog.BugEntry(HobbyData.Bugs[i].Id)), false); break;
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
            UiKit.Button(row, "Kitchen", "Kitchen", UiKit.ButtonStyle.Primary, () => Go(Page.Kitchen), 32f, IconPainter.Star);

            HobbyCard(Hobby.Fishing, "Fishing", IconPainter.Rod, UiKit.Spring,
                $"{HobbyLog.Fish.Count} of {HobbyData.Fish.Length} fish found",
                "Walk to the water's edge where fish shadows swim, then tap the rod button.",
                "Journal", () => Go(Page.Fish));
            HobbyCard(Hobby.Birding, "Birdwatching", IconPainter.Bird, UiKit.Hex("E3A92B"),
                $"{HobbyLog.Birds.Count} of {HobbyData.Birds.Length} birds spotted",
                "Birds land in trees and on the shore. Tap one from a few steps away. Don't rush up to it!",
                "Journal", () => Go(Page.Birds));
            HobbyCard(Hobby.Bugs, "Bug Catching", IconPainter.Sparkle, UiKit.Hex("7BAE3E"),
                $"{HobbyLog.Bugs.Count} of {HobbyData.Bugs.Length} bugs caught",
                "Bugs settle on flowers, trees and grass near you. Tap one to net it, gently.",
                "Journal", () => Go(Page.Bugs));
            HobbyCard(Hobby.Gardening, "Gardening", IconPainter.Sparkle, UiKit.Hex("8A5A32"),
                $"{HobbyLog.Plots.Count} of {Game.Gardening.MaxPlots} plots dug",
                $"Make a plot ({Game.Gardening.DigWood} wood) and put it anywhere. Tap it to plant seeds, come back when it's grown.",
                "Dig plot", () =>
                {
                    var why = Game.Gardening.Dig();
                    if (why != null) Hints.Show(why);
                    else Close();
                });
            HobbyCard(Hobby.Cooking, "Cooking", IconPainter.Star, UiKit.Primary,
                $"{CountDishes()} dish{(CountDishes() == 1 ? "" : "es")} in your bag",
                "Cook crops and fish into dishes. Serve one to fill every hungry worker at once.",
                "Kitchen", () => Go(Page.Kitchen));
            HobbyCard(Hobby.Cats, "Cat Care", IconPainter.Heart, UiKit.Hex("E58FB0"),
                $"{AdoptedCats()} of {HobbyData.Cats.Length} cats adopted",
                "Stray cats visit your island. Pet them and share fish until they trust you.",
                "Cats", () => Go(Page.Cats));
            HobbyCard(Hobby.Writing, "Writing", IconPainter.Book, UiKit.Hex("A77BD6"),
                $"{HobbyLog.Books.Count} book{(HobbyLog.Books.Count == 1 ? "" : "s")} written",
                "Write stories for your shelf, and read the books your friends have written.",
                "Library", () => { Close(); if (LibraryScreen.Instance != null) LibraryScreen.Instance.Open(); });
            HobbyCard(Hobby.Sewing, "Sewing", IconPainter.Shirt, UiKit.Hex("C2577F"),
                $"{HobbyLog.Designs.Count} design{(HobbyLog.Designs.Count == 1 ? "" : "s")} sewn",
                "Draw your own T-shirt design and wear it around the island.",
                "Sew", () => { Close(); if (SewingScreen.Instance != null) SewingScreen.Instance.Open(); });
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

        void Journal(int count, Func<int, (string name, Sprite pic, int stars, HobbyLog.Entry entry)> get, bool sizes)
        {
            Back();
            var grid = Grid(count);
            for (int i = 0; i < count; i++)
            {
                var (name, pic, stars, entry) = get(i);
                bool known = entry != null;
                var tile = UiKit.Image(grid.transform, name, UiKit.Rounded(26), known ? Color.white : UiKit.SurfaceSunk);
                Picture(tile.transform, pic, known);
                Label(tile.transform, known ? name : "???", known);
                string detail = HobbyData.Rarity(stars);
                if (known) detail += sizes ? $"\n×{entry.count} · best {entry.best:0} cm" : $"\n×{entry.count}";
                Detail(tile.transform, detail);
            }
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
                var row = ItemRow(e.id, $"×{e.count}");
                if (!e.id.StartsWith("dish:")) continue;
                string dish = e.id.Substring(5);
                var serve = UiKit.Button(row, "Serve", "Serve", UiKit.ButtonStyle.Primary, () =>
                {
                    int fed = Cooking.Serve(dish);
                    Hints.Show(fed == 0 ? "Nobody here is hungry right now. Save it for later!" : $"You fed {fed} hungry worker{(fed == 1 ? "" : "s")}!");
                    if (fed > 0) Close();
                }, 26f);
                var le = serve.transform.parent.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 150f; le.preferredHeight = 68f;
            }
        }

        void KitchenPage()
        {
            Back();
            int level = HobbyLog.Level(Hobby.Cooking);
            foreach (var dish in HobbyData.Dishes)
            {
                bool known = dish.MinLevel <= level;
                var needs = new StringBuilder();
                foreach (var (item, count) in dish.Needs)
                {
                    if (needs.Length > 0) needs.Append(", ");
                    bool have = HobbyLog.Has(item, count);
                    needs.Append(have ? "" : "<color=#C0503E>").Append(count).Append(' ').Append(HobbyData.ItemName(item)).Append(have ? "" : "</color>");
                }
                string line = known ? needs.ToString() + "\n<size=85%>" + dish.Blurb + "</size>" : $"Unlocks at cooking level {dish.MinLevel}";
                var row = ItemRow("dish:" + dish.Id, line, known ? dish.Name : "???");
                if (!known) continue;
                var d = dish;
                var cook = UiKit.Button(row, "Cook", "Cook", UiKit.ButtonStyle.Primary, () =>
                {
                    if (Cooking.Cook(d)) { Hints.Show(d.Name + " is ready! It's in your bag."); Refresh(); }
                }, 26f);
                cook.interactable = Cooking.CanCook(d);
                var le = cook.transform.parent.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 150f; le.preferredHeight = 68f;
            }
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

        void Write()
        {
            ClearChildren(_list);
            var title = UiKit.Input(_list, "Title", "Title", false, 96f);
            title.characterLimit = 40;
            var text = UiKit.Input(_list, "Story", "Once upon a time, on a little island...", true, 640f);
            text.characterLimit = 2000;
            var row = UiKit.Rect("Buttons", _list);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            UiKit.Button(row, "Cancel", "Cancel", UiKit.ButtonStyle.Secondary, Refresh, 30f);
            UiKit.Button(row, "Save", "Put it on my shelf", UiKit.ButtonStyle.Primary, () =>
            {
                string t = title.text.Trim(), s = text.text.Trim();
                if (t.Length == 0 || s.Length < 20)
                {
                    Hints.Show("A book needs a title and at least a few sentences.");
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
    /// The sewing table: paint a T-shirt pixel by pixel from a little palette, save the design,
    /// and wear it. The canvas only takes paint inside the shirt.
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

        protected override string Title => "Sewing";
        protected override Sprite TitleIcon => IconPainter.Shirt;
        protected override Color AccentColour => UiKit.Hex("E58FB0");

        Texture2D _texture;
        RawImage _canvas;
        int _colour = 2;
        bool _erase;
        readonly char[] _pixels = new char[Size * Size];
        readonly List<Image> _swatches = new();
        RectTransform _designs;
        int _editing = -1;
        TextMeshProUGUI _status;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Is this pixel inside the T-shirt?</summary>
        public static bool InShirt(int x, int y)
        {
            float u = (x + 0.5f) / Size * 2f - 1f, v = (y + 0.5f) / Size * 2f - 1f;
            return IconPainter.ShirtShape(u, v) < 0f;
        }

        protected override void Build(RectTransform body)
        {
            var scroll = UiKit.ScrollList(body, "List", out var list, 16f, new RectOffset(4, 4, 8, 32));
            UiKit.Stretch((RectTransform)scroll.transform);

            // The canvas: a shirt-coloured silhouette under a painted 16×16 texture.
            var frame = UiKit.Rect("Canvas", list);
            frame.gameObject.AddComponent<LayoutElement>().preferredHeight = 520f;
            var mat = UiKit.Image(frame, "Mat", UiKit.Rounded(36), UiKit.Hex("CFE3E0"));
            UiKit.Place(mat.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 520f));
            mat.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var shirt = UiKit.Image(frame, "Shirt", IconPainter.Shirt, UiKit.Hex("F4EBDD"));
            shirt.preserveAspect = true;
            UiKit.Place(shirt.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 500f));
            shirt.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var raw = UiKit.Rect("Paint", shirt.transform);
            UiKit.Stretch(raw);
            _canvas = raw.gameObject.AddComponent<RawImage>();
            _canvas.texture = _texture;
            raw.gameObject.AddComponent<PaintSurface>().Owner = this;

            _status = UiKit.Text(list, "Status", "Tap or drag to paint your shirt.", 26f, UiKit.InkSoft, false, TextAlignmentOptions.Center);

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
            UiKit.Button(row, "Clear", "Clear", UiKit.ButtonStyle.Secondary, () => { Clear(); _editing = -1; }, 28f);
            UiKit.Button(row, "Save", "Save", UiKit.ButtonStyle.Spring, Save, 28f);
            UiKit.Button(row, "Wear", "Save & wear", UiKit.ButtonStyle.Primary, () => { Save(); HobbyLog.Wear(_editing); Status("You're wearing it! Have a look around the island."); }, 28f);

            SectionLabel(list, "Your designs");
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
            Designs();
        }

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
            if (x < 0 || y < 0 || x >= Size || y >= Size || !InShirt(x, y)) return;
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

        void Save()
        {
            string data = new string(_pixels);
            bool firstSave = _editing < 0;
            _editing = HobbyLog.SaveDesign("Design " + (HobbyLog.Designs.Count + (firstSave ? 1 : 0)), data, Size, _editing);
            if (firstSave)
            {
                HobbyLog.AddXp(Hobby.Sewing, 20);
                Motes.Trigger("hobby_sew");
            }
            Status("Saved!");
            Designs();
        }

        void Status(string text) => _status.text = text;

        void Designs()
        {
            ClearChildren(_designs);
            for (int i = 0; i < HobbyLog.Designs.Count; i++)
            {
                int index = i;
                var d = HobbyLog.Designs[i];
                var tile = UiKit.Image(_designs, d.name, UiKit.Rounded(22), index == HobbyLog.WornDesign ? UiKit.Hex("FDE8F0") : Color.white, true);
                var pic = UiKit.Image(tile.transform, "Shirt", DesignSprite(d.pixels), Color.white);
                pic.preserveAspect = true;
                UiKit.Stretch(pic.rectTransform, 12f, 12f, 12f, 12f);
                var b = tile.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() =>
                {
                    _editing = index;
                    var p = HobbyLog.Designs[index].pixels;
                    for (int k = 0; k < _pixels.Length && k < p.Length; k++) _pixels[k] = p[k];
                    Redraw();
                    Status("Editing " + HobbyLog.Designs[index].name + ".");
                });
            }
        }

        /// <summary>A design as a finished shirt: fabric where unpainted, outlined, pixels crisp.</summary>
        public static Sprite DesignSprite(string pixels, int res = 64)
        {
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color32[res * res];
            var fabric = UiKit.Hex("F4EBDD");
            var seam = UiKit.Hex("8A6A52");
            for (int j = 0; j < res; j++)
            for (int i = 0; i < res; i++)
            {
                float u = (i + 0.5f) / res * 2f - 1f, v = (j + 0.5f) / res * 2f - 1f;
                float d = IconPainter.ShirtShape(u, v);
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
