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

        public enum Page { Hub, Fish, Birds, Bugs, Cats, Bag, Kitchen, Veg }

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
            _dotsShowing = 0;
            _dotsPrefix = null;
            TitleLabel.text = _page switch
            {
                Page.Fish => "Fish Journal", Page.Birds => "Bird Journal", Page.Bugs => "Bug Journal", Page.Veg => "Vegetable Journal",
                Page.Cats => "Cats", Page.Bag => "Bag", Page.Kitchen => "Kitchen", _ => "Hobbies",
            };
            switch (_page)
            {
                case Page.Fish: Journal(Hobby.Fishing, HobbyData.Fish.Length, i => (HobbyData.Fish[i].Name, HobbyData.FishPicture(HobbyData.Fish[i]), HobbyData.Fish[i].Stars, HobbyLog.FishEntry(HobbyData.Fish[i].Id), HobbyData.Fish[i].Times, HobbyData.Fish[i].RainOnly, Whereabouts.Fish(HobbyData.Fish[i])), true); break;
                case Page.Birds: Journal(Hobby.Birding, HobbyData.Birds.Length, i => (HobbyData.Birds[i].Name, HobbyData.BirdPicture(HobbyData.Birds[i]), HobbyData.Birds[i].Stars, HobbyLog.BirdEntry(HobbyData.Birds[i].Id), HobbyData.Birds[i].Times, HobbyData.Birds[i].RainOnly, Whereabouts.Bird(HobbyData.Birds[i])), false); break;
                case Page.Bugs: Journal(Hobby.Bugs, HobbyData.Bugs.Length, i => (HobbyData.Bugs[i].Name, HobbyData.Art("Bug_" + HobbyData.Bugs[i].Id), HobbyData.Bugs[i].Stars, HobbyLog.BugEntry(HobbyData.Bugs[i].Id), HobbyData.Bugs[i].Times, HobbyData.Bugs[i].RainOnly, Whereabouts.Bug(HobbyData.Bugs[i])), false); break;
                case Page.Cats: CatsPage(); break;
                case Page.Bag: BagPage(); break;
                case Page.Kitchen: KitchenPage(); break;
                case Page.Veg: VegPage(); break;
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
            CardBanner();

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
                    $"{HobbyLog.Crops.Count} of {HobbyData.Crops.Length} vegetables grown",
                    "Buy seeds at the barn on Sunflower Farm and drag them onto a plot. Come back when they've grown. More plots and seeds open as you level up.",
                    "Journal", () => Go(Page.Veg))),
                (Hobby.Cooking, "Cooking", IconPainter.Flame, UiKit.Primary, () => HobbyCard(Hobby.Cooking, "Cooking", IconPainter.Flame, UiKit.Primary,
                    $"{CountDishes()} dish{(CountDishes() == 1 ? "" : "es")} in your bag",
                    "Tap your stove and drag ingredients into the pot. Islanders with a dish in a bubble over their head will pay for it.",
                    "Recipes", () => Go(Page.Kitchen))),
                (Hobby.Cats, "Cat Care", IconPainter.Heart, UiKit.Hex("E58FB0"), () => HobbyCard(Hobby.Cats, "Cat Care", IconPainter.Heart, UiKit.Hex("E58FB0"),
                    $"{AdoptedCats()} of {HobbyData.Cats.Length} cats adopted",
                    "Stray cats visit your island. Pet them and share fish until they trust you.",
                    "Cats", () => Go(Page.Cats))),
                (Hobby.Writing, "Writing", IconPainter.Book, UiKit.Hex("A77BD6"), () => HobbyCard(Hobby.Writing, "Writing", IconPainter.Book, UiKit.Hex("A77BD6"),
                    $"{HobbyLog.Books.Count} ema hung",
                    "Old Mochi keeps the shrine on the hill. Hang an ema with your wish on his rack, and read the ones friends leave you.",
                    // A friend has left an ema: straight to the rack to read it (which is what clears its dot).
                    NewStuff.Any("hobbies/Writing/") ? "Look at the rack" : "Show me",
                    NewStuff.Any("hobbies/Writing/") ? (Action)(() => { Close(); LibraryScreen.ShowRack(); }) : () => ShowInWorld(Shrine.Spot, "The shrine is on your first island."))),
                (Hobby.Sewing, "Sewing", IconPainter.Hat, UiKit.Hex("C2577F"), () => HobbyCard(Hobby.Sewing, "Sewing", IconPainter.Hat, UiKit.Hex("C2577F"),
                    $"{HobbyLog.Designs.Count} piece{(HobbyLog.Designs.Count == 1 ? "" : "s")} sewn",
                    "At your sewing machine: a hat needs cotton from your garden. Then decorate it and wear it!",
                    "Show me", () => ShowInWorld(SewingStation.Placed() != null ? SewingStation.Placed().transform : null, "Put your sewing machine down somewhere first."))),
            };
            // A carousel, one big picture card per hobby: yours first, then the ones open to take
            // up, then the ones still waiting for a hobby card.
            var order = new List<(Hobby hobby, string name, Sprite icon, Color colour, System.Action card)>();
            foreach (var c in all) if (HobbyLog.IsUnlocked(c.hobby)) order.Add(c);
            foreach (var c in all) if (!HobbyLog.IsUnlocked(c.hobby) && HobbyLog.IsOpened(c.hobby)) order.Add(c);
            foreach (var c in all) if (!HobbyLog.IsOpened(c.hobby)) order.Add(c);

            // Something new on a page: it opens there (unless the page it was left on has news too),
            // and the pager's dot and arrow toward any other such page turn red.
            var news = new List<bool>();
            foreach (var c in order) news.Add(PageNews(c.hobby));
            if (_carouselIndex < 0 || _carouselIndex >= news.Count || !news[_carouselIndex])
            {
                int first = news.IndexOf(true);
                if (first >= 0) _carouselIndex = first;
            }
            Carousel(order.Count, i =>
            {
                var c = order[i];
                if (HobbyLog.IsUnlocked(c.hobby)) c.card();
                else LockedCard(c.hobby, c.name, c.icon);
            }, news);
            foreach (var c in all)
            {
                NewStuff.Remove("hobbies/" + c.hobby);   // a newly opened or taken-up hobby: seen
                // Finds for a hobby not taken up have no page to be seen on (a friend's ema left
                // before writing was open, say): let them go rather than keep the button's dot lit.
                if (!HobbyLog.IsUnlocked(c.hobby)) NewStuff.Clear("hobbies/" + c.hobby + "/");
            }
        }

        static readonly Hobby[] AllHobbies = (Hobby[])Enum.GetValues(typeof(Hobby));

        /// <summary>
        /// New on a hobby's page of the carousel: the hobby itself, just opened or taken up (its
        /// card shows a dot), or a find in its journal — only for a hobby you have, since a locked
        /// card has no journal to open.
        /// </summary>
        static bool PageNews(Hobby h) => NewStuff.Has("hobbies/" + h) || (HobbyLog.IsUnlocked(h) && NewStuff.Any("hobbies/" + h + "/"));

        /// <summary>
        /// The Hobbies button's red dot: only for what this screen will show you (and clear), so it
        /// never stays lit with nothing to find inside — a hobby card waiting, or new on a page.
        /// </summary>
        public static bool HasNews()
        {
            foreach (var h in AllHobbies) if (PageNews(h)) return true;
            return NewStuff.Has("hobbies/card") && HobbyCards.Waiting > 0;
        }

        // ---------------------------------------------------------------- the carousel

        RectTransform _cardSlot;    // the card being built goes in here
        ScrollRect _carousel;
        int _carouselIndex;
        readonly List<Image> _dots = new();
        List<bool> _pageNews;           // pages with something new on them
        GameObject _prevNew, _nextNew;  // the red dots on the arrows: more news that way

        /// <summary>
        /// A row of full-width pages you swipe through, snapping to each; arrows at the sides and
        /// dots underneath. <paramref name="build"/> fills page i (its cards go into <see cref="_cardSlot"/>).
        /// Pages marked in <paramref name="news"/> have a red dot underneath, and an arrow pointing
        /// toward one carries a little red dot of its own.
        /// </summary>
        void Carousel(int count, Action<int> build, List<bool> news = null)
        {
            _pageNews = news;
            float width = Sheet.sizeDelta.x - 64f - 8f;
            float height = Mathf.Max(760f, Sheet.sizeDelta.y - 128f - 64f - 96f - 16f - 70f);
            var holder = UiKit.Rect("Carousel", _list);
            holder.gameObject.AddComponent<LayoutElement>().preferredHeight = height;

            var view = UiKit.Rect("View", holder);
            UiKit.Stretch(view);
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            var strip = UiKit.Rect("Pages", view);
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(0f, 1f);
            strip.pivot = new Vector2(0f, 0.5f);
            strip.sizeDelta = new Vector2(width * count, 0f);
            strip.anchoredPosition = Vector2.zero;
            _carousel = view.gameObject.AddComponent<ScrollRect>();
            _carousel.content = strip;
            _carousel.viewport = view;
            _carousel.horizontal = true;
            _carousel.vertical = false;
            _carousel.movementType = ScrollRect.MovementType.Elastic;
            _carousel.inertia = false;
            view.gameObject.AddComponent<Snap>().Init(this, count);

            for (int i = 0; i < count; i++)
            {
                var page = UiKit.Rect("Page" + i, strip);
                page.anchorMin = new Vector2(0f, 0f);
                page.anchorMax = new Vector2(0f, 1f);
                page.pivot = new Vector2(0f, 0.5f);
                page.sizeDelta = new Vector2(width, 0f);
                page.anchoredPosition = new Vector2(i * width, 0f);
                var v = page.gameObject.AddComponent<VerticalLayoutGroup>();
                v.padding = new RectOffset(10, 10, 4, 4);
                v.childControlWidth = v.childControlHeight = true;
                v.childForceExpandWidth = v.childForceExpandHeight = true;
                _cardSlot = page;
                build(i);
            }
            _cardSlot = null;

            // Arrows at the sides.
            _prevNew = Arrow(holder, -1, count);
            _nextNew = Arrow(holder, 1, count);

            // Dots.
            _dots.Clear();
            var dots = UiKit.Rect("Dots", _list);
            var dh = dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            dh.spacing = 14f;
            dh.childAlignment = TextAnchor.MiddleCenter;
            dh.childControlWidth = dh.childControlHeight = false;
            dh.childForceExpandWidth = dh.childForceExpandHeight = false;
            dots.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
            for (int i = 0; i < count; i++)
            {
                var d = UiKit.Image(dots, "Dot" + i, UiKit.Circle, UiKit.SurfaceSunk);
                d.rectTransform.sizeDelta = new Vector2(20f, 20f);
                _dots.Add(d);
            }
            ShowPage(Mathf.Clamp(_carouselIndex, 0, count - 1), false);
        }

        /// <summary>One of the side arrows. Returns its red dot (hidden until there's news that way).</summary>
        GameObject Arrow(RectTransform holder, int dir, int count)
        {
            var a = UiKit.Image(holder, dir < 0 ? "Prev" : "Next", UiKit.Circle, new Color(1f, 1f, 1f, 0.92f), true).rectTransform;
            a.anchorMin = a.anchorMax = a.pivot = new Vector2(dir < 0 ? 0f : 1f, 0.68f);
            a.anchoredPosition = new Vector2(dir < 0 ? 18f : -18f, 0f);
            a.sizeDelta = new Vector2(76f, 76f);
            UiKit.AddShadow(a, 10f, 4f, 0.22f);
            var ic = UiKit.Image(a, "Arrow", IconPainter.Arrow, UiKit.Ink);
            ic.preserveAspect = true;
            UiKit.Stretch(ic.rectTransform, 20f, 20f, 20f, 20f);
            if (dir < 0) ic.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            var b = a.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => ShowPage(Mathf.Clamp(_carouselIndex + dir, 0, count - 1), true));
            a.gameObject.AddComponent<PressSquash>();
            var dot = NewDot.Fixed(a, 24f);
            dot.SetActive(false);
            return dot;
        }

        /// <summary>Is there a page with something new beyond <paramref name="index"/> in direction <paramref name="dir"/>?</summary>
        bool NewsBeyond(int index, int dir)
        {
            if (_pageNews == null) return false;
            for (int i = index + dir; i >= 0 && i < _pageNews.Count; i += dir)
                if (_pageNews[i]) return true;
            return false;
        }

        void ShowPage(int index, bool animate)
        {
            if (_carousel == null) return;
            _carouselIndex = index;
            int count = _dots.Count;
            float target = count > 1 ? index / (float)(count - 1) : 0f;
            _carousel.StopMovement();
            DOTween.Kill(_carousel);
            if (animate) DOTween.To(() => _carousel.horizontalNormalizedPosition, x => _carousel.horizontalNormalizedPosition = x, target, 0.3f).SetEase(Ease.OutCubic).SetUpdate(true).SetTarget(_carousel);
            else { Canvas.ForceUpdateCanvases(); _carousel.horizontalNormalizedPosition = target; }
            for (int i = 0; i < _dots.Count; i++)
            {
                bool fresh = _pageNews != null && i < _pageNews.Count && _pageNews[i];
                _dots[i].color = fresh ? UiKit.Danger : i == index ? AccentColour : UiKit.SurfaceSunk;
                _dots[i].rectTransform.sizeDelta = Vector2.one * (i == index ? 26f : 20f);
            }
            if (_prevNew != null) _prevNew.SetActive(NewsBeyond(index, -1));
            if (_nextNew != null) _nextNew.SetActive(NewsBeyond(index, 1));
        }

        /// <summary>Lets go of a swipe on the nearest page (or the next one, for a flick).</summary>
        class Snap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
        {
            HobbiesScreen _screen;
            int _count;
            float _from;

            public void Init(HobbiesScreen s, int count) { _screen = s; _count = count; }

            public void OnBeginDrag(PointerEventData e)
            {
                DOTween.Kill(_screen._carousel);
                _from = _screen._carousel.horizontalNormalizedPosition;
            }

            public void OnEndDrag(PointerEventData e)
            {
                if (_count <= 1) return;
                float pos = _screen._carousel.horizontalNormalizedPosition * (_count - 1);
                float moved = (_screen._carousel.horizontalNormalizedPosition - _from) * (_count - 1);
                int index = Mathf.Abs(moved) > 0.15f && Mathf.Abs(moved) < 0.5f
                    ? Mathf.RoundToInt(_from * (_count - 1)) + (moved > 0 ? 1 : -1)
                    : Mathf.RoundToInt(pos);
                _screen.ShowPage(Mathf.Clamp(index, 0, _count - 1), true);
            }
        }

        /// <summary>The picture of the capybara at a hobby, for its card.</summary>
        static Sprite Vignette(Hobby hobby) => HobbyData.Art("Vignette_" + hobby);

        /// <summary>
        /// A page of the carousel: the capybara at it, big, with its level, how you're getting on,
        /// a tip and a button. A hobby not taken up yet is greyed, locked, and says how to start.
        /// </summary>
        RectTransform PictureCard(Hobby hobby, Color colour, bool locked, out RectTransform text)
        {
            var card = UiKit.Image(_cardSlot != null ? (Transform)_cardSlot : _list, hobby.ToString(), UiKit.Rounded(40), locked ? UiKit.Hex("F1ECE4") : Color.white).rectTransform;
            UiKit.AddShadow(card, 16f, 6f, 0.18f);

            // The picture, on a soft disc in the hobby's colour.
            var art = UiKit.Rect("Art", card);
            art.anchorMin = new Vector2(0f, 0.42f);
            art.anchorMax = Vector2.one;
            art.offsetMin = new Vector2(24f, 0f);
            art.offsetMax = new Vector2(-24f, -24f);
            var disc = UiKit.Image(art, "Disc", IconPainter.Glow, new Color(colour.r, colour.g, colour.b, locked ? 0.12f : 0.3f));
            disc.preserveAspect = true;
            UiKit.Stretch(disc.rectTransform, -40f, -40f, -40f, -40f);
            var pic = UiKit.Image(art, "Picture", Vignette(hobby), locked ? new Color(0.55f, 0.5f, 0.47f, 0.55f) : Color.white);
            pic.preserveAspect = true;
            UiKit.Stretch(pic.rectTransform);
            if (locked)
            {
                var lk = UiKit.Image(art, "Lock", UiKit.Circle, UiKit.Hex("8A7563"));
                UiKit.Place(lk.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f));
                lk.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var li = UiKit.Image(lk.transform, "Icon", IconPainter.Lock, Color.white);
                UiKit.Stretch(li.rectTransform, 26f, 26f, 26f, 26f);
            }

            // The words, under it.
            text = UiKit.Rect("Words", card);
            text.anchorMin = Vector2.zero;
            text.anchorMax = new Vector2(1f, 0.42f);
            text.offsetMin = new Vector2(30f, 24f);
            text.offsetMax = new Vector2(-30f, 0f);
            var v = text.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10f;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return card;
        }

        /// <summary>
        /// A hobby you have not taken up yet: greyed out, with what to buy and where — or, while it
        /// is still closed, how a hobby card opens it (and a way to spend one, if one is waiting).
        /// </summary>
        void LockedCard(Hobby hobby, string name, Sprite icon)
        {
            var locked = PictureCard(hobby, UiKit.InkSoft, true, out var text);
            if (NewStuff.Has("hobbies/" + hobby)) NewDot.Fixed(locked, 40f);   // just opened with a card
            var title = UiKit.Text(text, "Name", name, 46f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            bool closed = !HobbyLog.IsOpened(hobby);
            // A card can take it up now (the first card only a hobby with gear, given free).
            bool card = HobbyCards.Waiting > 0 && HobbyCards.Choices().Contains(hobby);
            string words = card ? (HobbyCards.FirstCard ? StoryText.HobbyCardLines.LockedFree : StoryText.HobbyCardLines.LockedChoose)
                : !closed ? HobbyLog.HowToUnlock(hobby)
                : !HobbyCards.CanOpenYet(hobby) ? StoryText.HobbyCardLines.CookingAfterFarm
                : HobbyCards.Waiting > 0 ? StoryText.HobbyCardLines.LockedLater
                : HobbyCards.HeartsToNext is int more
                    ? (more == 1 ? StoryText.HobbyCardLines.LockedHeart : string.Format(StoryText.HobbyCardLines.LockedHearts, more))
                    : StoryText.HobbyCardLines.LockedLater;
            var how = UiKit.Text(text, "How", words, 28f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            how.overflowMode = TextOverflowModes.Overflow;
            if (!card) return;
            var use = UiKit.Button(text, "UseCard", StoryText.HobbyCardLines.UseCard, UiKit.ButtonStyle.Primary, () => SpendCard(hobby), 30f, IconPainter.Star);
            use.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 84f;
        }

        /// <summary>Cards waiting to be spent: how many, and the way to the choice.</summary>
        void CardBanner()
        {
            int waiting = HobbyCards.Waiting;
            if (waiting <= 0) return;
            var banner = UiKit.Image(_list, "HobbyCards", UiKit.Rounded(32), UiKit.Hex("FDE7EF")).rectTransform;
            var h = banner.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(24, 18, 14, 14);
            h.spacing = 16f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            banner.gameObject.AddComponent<LayoutElement>().minHeight = 112f;
            var words = UiKit.Text(banner, "Words", waiting > 1 ? string.Format(StoryText.HobbyCardLines.HubMany, waiting) : StoryText.HobbyCardLines.HubOne,
                32f, UiKit.Ink, true);
            words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var go = UiKit.Button(banner, "Choose", StoryText.HobbyCardLines.HubButton, UiKit.ButtonStyle.Primary, () => SpendCard(null), 28f, IconPainter.Star);
            var le = go.transform.parent.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 330f; le.preferredHeight = 80f;
            NewDot.Fixed(go.transform, 30f);   // the card is what lit the Hobbies button
        }

        void SpendCard(Hobby? hobby) => HobbyCardScreen.Offer(hobby, () => { if (IsOpen && _page == Page.Hub) Refresh(); });

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
            var card = PictureCard(hobby, colour, false, out var text);
            // New here: the hobby itself (just taken up), or a find in its journal.
            if (NewStuff.Any("hobbies/" + hobby)) NewDot.Fixed(card, 40f);
            var title = UiKit.Text(text, "Name", $"{name}  <size=70%><color=#{ColorUtility.ToHtmlStringRGB(colour)}>Level {HobbyLog.Level(hobby)}</color></size>", 46f, UiKit.Ink, true, TextAlignmentOptions.Center);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            var fill = UiKit.ProgressBar(text, "Progress", colour, 18f);
            UiKit.SetProgress(fill, HobbyLog.LevelProgress(hobby));
            UiKit.Text(text, "Stat", stat, 28f, UiKit.Ink, true, TextAlignmentOptions.Center);
            var t = UiKit.Text(text, "Tip", tip, 25f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            t.overflowMode = TextOverflowModes.Overflow;
            var go = UiKit.Button(text, "Open", action, UiKit.ButtonStyle.Primary, open, 32f);
            var ble = go.transform.parent.gameObject.AddComponent<LayoutElement>();
            ble.preferredHeight = 88f;
            if (NewStuff.Any("hobbies/" + hobby + "/")) NewDot.Fixed(go.transform, 30f);
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

        void Journal(Hobby hobby, int count, Func<int, (string name, Sprite pic, int stars, HobbyLog.Entry entry, DayParts times, bool rain, string where)> get, bool sizes)
        {
            Back();
            JournalHeader(hobby);
            var grid = Grid(count);
            for (int i = 0; i < count; i++)
            {
                var (name, pic, stars, entry, times, rain, where) = get(i);
                bool known = entry != null;
                var tile = UiKit.Image(grid.transform, name, UiKit.Rounded(26), known ? Color.white : UiKit.SurfaceSunk);
                Picture(tile.transform, pic, known);
                Label(tile.transform, known ? name : "???", known);
                // Not found yet: where and when to go looking. Found: when it's about, and your tally.
                string detail = known
                    ? HobbyData.Rarity(stars) + "\n" + (rain ? "Only in the rain" : GameClock.Describe(times))
                      + (sizes ? $"\n×{entry.count} · best {entry.best:0} cm" : $"\n×{entry.count}")
                    : HobbyData.Rarity(stars) + "\n" + where;
                Detail(tile.transform, detail, known);
                TapTile(tile, known, known ? "hobbies/" + hobby + "/" + entry.id : null, name, detail, pic);
            }
            DotsSeen("hobbies/" + hobby + "/");
        }

        // ---------------------------------------------------------------- tapping a journal tile

        int _dotsShowing;
        string _dotsPrefix;

        /// <summary>
        /// A journal tile you can tap (a Button, so dragging the page along doesn't count): found,
        /// a card about it, and its new-dot goes; not found yet, where to look, by your finger.
        /// </summary>
        void TapTile(Image tile, bool known, string key, string title, string about, Sprite pic)
        {
            GameObject dot = null;
            if (known && key != null && NewStuff.Has(key)) { dot = NewDot.Fixed(tile.transform, 30f); _dotsShowing++; }
            tile.raycastTarget = true;
            var button = tile.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            tile.gameObject.AddComponent<PressSquash>();
            button.onClick.AddListener(() =>
            {
                Sfx.Play(SfxKind.Tap, 0.5f);
                if (dot != null)
                {
                    NewStuff.Remove(key);
                    Destroy(dot);
                    dot = null;
                    // The last new one looked at: the page is seen (and anything it doesn't show a tile for).
                    if (--_dotsShowing <= 0 && _dotsPrefix != null) NewStuff.Clear(_dotsPrefix);
                }
                if (known) CardPopup.Show(title, about ?? "", pic);
                else Hints.Answer(string.IsNullOrEmpty(about) ? "Not found yet." : about);
            });
        }

        /// <summary>After a page is built: with nothing new on it to tap, it is seen at once.</summary>
        void DotsSeen(string prefix)
        {
            _dotsPrefix = prefix;
            if (_dotsShowing == 0) NewStuff.Clear(prefix);
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
            p.rectTransform.anchorMin = new Vector2(0.08f, known ? 0.38f : 0.52f);
            p.rectTransform.anchorMax = new Vector2(0.92f, 0.95f);
            p.rectTransform.offsetMin = p.rectTransform.offsetMax = Vector2.zero;
        }

        // A tile not found yet gives the shadow less room and where-and-when more.
        static void Label(Transform tile, string text, bool strong)
        {
            var label = UiKit.Text(tile, "Name", text, 24f, strong ? UiKit.Ink : UiKit.InkSoft, true, TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = new Vector2(0f, strong ? 0.2f : 0.38f);
            label.rectTransform.anchorMax = new Vector2(1f, strong ? 0.38f : 0.52f);
            label.rectTransform.offsetMin = new Vector2(6f, 0f);
            label.rectTransform.offsetMax = new Vector2(-6f, 0f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 24f;
        }

        static void Detail(Transform tile, string text, bool known = true)
        {
            var d = UiKit.Text(tile, "Detail", text, 20f, UiKit.Hex("B98A1E"), false, TextAlignmentOptions.Center);
            d.rectTransform.anchorMin = Vector2.zero;
            d.rectTransform.anchorMax = new Vector2(1f, known ? 0.22f : 0.38f);
            d.rectTransform.offsetMin = new Vector2(4f, 4f);
            d.rectTransform.offsetMax = new Vector2(-4f, 0f);
            d.enableAutoSizing = true;
            d.fontSizeMin = 14f;
            d.fontSizeMax = 20f;
        }

        /// <summary>
        /// Every vegetable you can grow: the ones you have harvested (and how many), the rest still
        /// a mystery, with the gardening level that brings their seeds.
        /// </summary>
        void VegPage()
        {
            Back();
            var plots = UiKit.Button(_list, "Plots", Farm.IsOpen ? "Go to my farm" : "Find Sunflower Farm", UiKit.ButtonStyle.Spring,
                () => ShowInWorld(Farm.Destination, "Open Pebble Cove first: Sunflower Farm's bridge is at its south end."), 30f, IconPainter.Sparkle);
            plots.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 90f;
            JournalHeader(Hobby.Gardening);
            var grid = Grid(HobbyData.Crops.Length);
            foreach (var crop in HobbyData.Crops)
            {
                var entry = HobbyLog.CropEntry(crop.Id);
                bool known = entry != null;
                var tile = UiKit.Image(grid.transform, crop.Name, UiKit.Rounded(26), known ? Color.white : UiKit.SurfaceSunk);
                Picture(tile.transform, HobbyData.ItemArt("crop:" + crop.Id), known);
                Label(tile.transform, known ? crop.Name : "???", known);
                string detail = known
                    ? Loc.F("×{0} harvested", entry.count) + "\n" + Loc.F("Grows in {0} min", crop.Minutes)
                    : Whereabouts.Crop(crop);
                Detail(tile.transform, detail, known);
                TapTile(tile, known, known ? "hobbies/Gardening/" + crop.Id : null, crop.Name, detail, HobbyData.ItemArt("crop:" + crop.Id));
            }
            DotsSeen("hobbies/Gardening/");
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
                Picture(tile.transform, Horaira.Picture(cat), met);
                string title = adopted || met && Horaira.Is(cat) ? CatCare.NameOf(cat) : met ? "Stray cat" : "???";
                Label(tile.transform, title, met);
                string detail = adopted ? "Adopted!" : met ? $"Trust {state.bond}/{CatCare.MaxBond}" : Whereabouts.CatHint(cat);
                Detail(tile.transform, detail, met);
                // A cat you've met: its card (its nature once adopted); one not met yet: where strays turn up.
                TapTile(tile, met, adopted ? "hobbies/Cats/" + cat.Id : null, title, adopted ? cat.Personality + "\n" + detail : detail, Horaira.Picture(cat));
                // A cat you've found: a paw in the corner takes you to it.
                if (met)
                {
                    var kind = cat;
                    string who = title;
                    var go = UiKit.RoundButton(tile.transform, "GoToCat", IconPainter.Paw, null, UiKit.Spring, () => GoToCat(kind, who), 64f);
                    var gr = (RectTransform)go.transform.parent;
                    gr.anchorMin = gr.anchorMax = gr.pivot = new Vector2(1f, 1f);
                    gr.anchoredPosition = new Vector2(-8f, -8f);
                    gr.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                }
            }
            DotsSeen("hobbies/Cats/");
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
            // His stall is on Sunflower Farm: until its bridge is up, he's not there to walk to.
            const string ChefAway = "Chef Tofu's stall is on Sunflower Farm, once its bridge is up.";
            var go = UiKit.Button(_list, "Stove", stove != null ? "Go to my stove" : "Buy a stove from Chef Tofu",
                UiKit.ButtonStyle.Primary, () => ShowInWorld(stove != null ? (Vector2?)stove.transform.position : ChefTofu.Spot, ChefAway), 30f, IconPainter.Flame);
            go.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            // Recipes and pantry goods: Chef Tofu's.
            var chef = UiKit.Button(_list, "Chef", "Visit Chef Tofu", UiKit.ButtonStyle.Spring, () => ShowInWorld(ChefTofu.Spot, ChefAway), 30f);
            chef.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            foreach (var dish in HobbyData.Dishes)
            {
                bool known = HobbyLog.KnowsRecipe(dish.Id);
                var needs = new StringBuilder();
                foreach (var (item, count) in dish.Needs)
                {
                    if (needs.Length > 0) needs.Append(", ");
                    bool have = HobbyLog.Has(item, count);
                    // A list no table can hold: each name is translated as it goes in.
                    needs.Append(have ? "" : "<color=#C0503E>").Append(count).Append(' ').Append(Loc.T(HobbyData.ItemName(item))).Append(have ? "" : "</color>");
                }
                string line = known ? needs.ToString() + "\n<size=70%><color=#8A7563>" + DishKind.DifficultyName(dish.Difficulty) + "</color></size>\n<size=85%>" + dish.Blurb + "</size>" : Whereabouts.Dish(dish);
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
        void ShowInWorld(Transform target, string otherwise) => ShowInWorld(target != null ? (Vector2?)target.position : null, otherwise);

        /// <summary>Off to wherever this cat is on the island now (a stray may have wandered off).</summary>
        void GoToCat(CatKind cat, string name)
        {
            foreach (var view in FindObjectsByType<CatCare.CatView>(FindObjectsSortMode.None))
                if (view.Kind == cat && view.isActiveAndEnabled) { ShowInWorld(view.transform, null); return; }
            // (A hint shows under this page, so the page goes first, as the other "go to" buttons do.)
            ShowInWorld((Vector2?)null, Loc.F("{0} isn't on the island right now. Cats come and go: look again later!", Loc.T(name)));
        }

        void ShowInWorld(Vector2? target, string otherwise)
        {
            if (target == null) { if (otherwise != null) Hints.Answer(otherwise); Close(); return; }
            Close();
            PlayerAvatar.GoTo(target.Value);
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
    /// Old Mochi's ema rack, as a page drawn like the rack itself: a red wooden frame under a dark
    /// roof beam, with little wooden plaques hanging from its bars on white strings. Tap one to read
    /// the wish on it. When one rack is full, the rest hang on the next (the arrows under it).
    /// Hanging a new one asks an offering of one gold (Game.Shrine).
    ///
    /// (Named for the library it used to be, so screens that open it needn't change.)
    /// </summary>
    public class LibraryScreen : UiScreen
    {
        public static LibraryScreen Instance { get; private set; }

        protected override string Title => "Ema Rack";
        protected override Sprite TitleIcon => HobbyData.Art("Ema") ?? IconPainter.Book;
        protected override Color AccentColour => Vermilion;

        static readonly Color Vermilion = UiKit.Hex("D2553F");

        RectTransform _list;
        ScrollRect _scroll;
        int _page;
        Action<string, string> _pendingWrite;
        readonly HashSet<string> _read = new();

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Opens on the rack.</summary>
        public static void ShowRack()
        {
            if (Instance == null) return;
            Instance._pendingWrite = null;
            Instance._page = 0;
            if (Instance.IsOpen) Instance.Refresh(); else Instance.Open();
        }

        /// <summary>Opens on a fresh ema to write; <paramref name="written"/> gets its wish and words.</summary>
        public static void WriteEma(Action<string, string> written)
        {
            if (Instance == null) return;
            Instance._pendingWrite = written;
            if (Instance.IsOpen) Instance.Refresh(); else Instance.Open();
        }

        protected override void Build(RectTransform body)
        {
            // A soft painted garden behind it all, filling the page whatever its shape.
            var clip = UiKit.Rect("Backdrop", body);
            UiKit.Stretch(clip, -32f, -32f, -32f, -8f);
            clip.gameObject.AddComponent<RectMask2D>();
            var back = UiKit.Image(clip, "Garden", HobbyData.Art("Shrine_Backdrop"), Color.white);
            UiKit.Stretch(back.rectTransform);
            var fit = back.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 1024f / 1536f;

            _scroll = UiKit.ScrollList(body, "List", out _list, 16f, new RectOffset(4, 4, 8, 32));
            UiKit.Stretch((RectTransform)_scroll.transform);
        }

        protected override void OnOpen() => Refresh();

        bool _onRack;   // the rack is showing (not a wish being read or written)

        /// <summary>The window changed shape (a phone turned, an editor layout): the rack is redrawn to fit it.</summary>
        protected override void Fit()
        {
            base.Fit();
            if (IsOpen && _onRack) Refresh();
        }

        void Refresh()
        {
            _onRack = false;
            ClearChildren(_list);
            if (_pendingWrite != null) Write(_pendingWrite);
            else RackPage();
            _scroll.verticalNormalizedPosition = 1f;
        }

        // ---------------------------------------------------------------- the rack

        void RackPage()
        {
            _onRack = true;
            NewStuff.Clear("hobbies/Writing/");   // the ema friends left: seen
            var all = Shrine.Rack();
            int perPage = PegsX.Length * BarsY.Length;
            int pages = Mathf.Max(1, Mathf.CeilToInt(all.Count / (float)perPage));
            _page = Mathf.Clamp(_page, 0, pages - 1);

            // Only once writing is open (a hobby card): until then the rack is just to read.
            if (HobbyLog.IsOpened(Hobby.Writing))
            {
                var hang = UiKit.Button(_list, "Hang", "Hang a new ema (1 gold)", UiKit.ButtonStyle.Primary, Shrine.WriteNew, 32f, HobbyData.Art("Ema"));
                hang.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            }

            // Which rack, right at the top: never below the fold where it could be missed.
            if (pages > 1)
            {
                var row = UiKit.Rect("Pages", _list);
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 18f;
                h.childAlignment = TextAnchor.MiddleCenter;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = false;
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 84f;
                RtlLayout.Add(h);
                PageButton(row, -1, _page > 0);
                var label = UiKit.Text(row, "Label", $"Rack {_page + 1} of {pages}", 30f, UiKit.Ink, true, TextAlignmentOptions.Center);
                label.gameObject.AddComponent<LayoutElement>().preferredWidth = 260f;
                PageButton(row, +1, _page < pages - 1);
            }

            // The rack, as big as fits in what is left of the page (no scrolling to find its bottom).
            Canvas.ForceUpdateCanvases();
            float used = 96f + 16f + (pages > 1 ? 84f + 16f : 0f) + 48f;
            float room = Body != null && Body.rect.height > 200f ? Body.rect.height - used : float.MaxValue;
            var shown = all.GetRange(_page * perPage, Mathf.Min(perPage, all.Count - _page * perPage));
            Rack(shown, room);
        }

        void PageButton(RectTransform row, int dir, bool enabled)
        {
            var disc = UiKit.Image(row, dir < 0 ? "Prev" : "Next", UiKit.Circle, enabled ? Vermilion : UiKit.SurfaceSunk, true);
            var le = disc.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = 76f;
            var ic = UiKit.Image(disc.transform, "Arrow", IconPainter.Chevron, Color.white);
            ic.preserveAspect = true;
            UiKit.Stretch(ic.rectTransform, 22f, 22f, 22f, 22f);
            if (dir < 0) ic.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            if (!enabled) return;
            var b = disc.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { Sfx.Play(SfxKind.Tap, 0.5f); _page += dir; Refresh(); });
            disc.gameObject.AddComponent<PressSquash>();
        }

        /// <summary>
        /// One rack: the painting (Hobbies/Ema_Rack), with the ema hung from its pegs — three on
        /// each of its two bars. Peg places are measured on the painting, as fractions of its size.
        /// </summary>
        void Rack(List<Shrine.Ema> ema, float room)
        {
            float width = Sheet != null ? Sheet.sizeDelta.x - 72f : 900f;
            float height = Mathf.Min(width * RackAspect, room);
            width = height / RackAspect;
            var holder = UiKit.Rect("RackHolder", _list);
            holder.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            var rack = UiKit.Rect("Rack", holder);
            rack.anchorMin = rack.anchorMax = rack.pivot = new Vector2(0.5f, 0.5f);
            rack.sizeDelta = new Vector2(width, height);

            var art = UiKit.Image(rack, "Painting", HobbyData.Art("Ema_Rack"), Color.white);
            art.preserveAspect = true;
            UiKit.Stretch(art.rectTransform);

            if (ema.Count == 0)
            {
                var t = UiKit.Text(rack, "Empty", "The rack is empty. Hang the first wish!", 30f, UiKit.Ink, true, TextAlignmentOptions.Center);
                t.rectTransform.anchorMin = new Vector2(0.24f, 0.45f);
                t.rectTransform.anchorMax = new Vector2(0.76f, 0.56f);
                t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
                t.overflowMode = TextOverflowModes.Overflow;
                return;
            }
            // With Easy Reading the plaques hang a little bigger, for bigger writing: up to 15%,
            // which still leaves a gap between neighbours' wood (the painting's edges are clear).
            float size = width * 0.205f * Mathf.Min(1.15f, 1f + 0.3f * (Accessibility.TextScale - 1f));
            for (int i = 0; i < ema.Count && i < PegsX.Length * BarsY.Length; i++)
                Plaque(rack, ema[i], i, PegsX[i % PegsX.Length], BarsY[i / PegsX.Length], size);
        }

        // Measured on the painting: the pegs used (left, middle, right) and the two bars, from the top.
        static readonly float[] PegsX = { 0.286f, 0.501f, 0.714f };
        static readonly float[] BarsY = { 0.418f, 0.577f };
        const float RackAspect = 1.5f;

        /// <summary>An ema on its peg: the plaque, a little crooked, with its wish written on the plain wood.</summary>
        void Plaque(RectTransform rack, Shrine.Ema e, int index, float x, float barFromTop, float size)
        {
            var plaque = UiKit.Image(rack, "Ema" + index, HobbyData.Art("Ema_Blank"), e.Mine >= 0 ? UiKit.Hex("FFF6E2") : Color.white, true);
            plaque.preserveAspect = true;
            var r = plaque.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(x, 1f - barFromTop);
            // The cord's loop sits on the peg, so the plaque hangs from a point near its top.
            r.pivot = new Vector2(0.5f, 0.86f);
            r.sizeDelta = new Vector2(size, size);
            r.anchoredPosition = Vector2.zero;
            r.localRotation = Quaternion.Euler(0f, 0f, ((index * 37) % 11 - 5) * 0.8f);

            // The wish's title, in ink on the plain wood below the little picture.
            // With Easy Reading it takes more of the plain wood: nearer the edges, up to the picture.
            var t = UiKit.Text(plaque.transform, "Wish", e.Title, 22f, UiKit.Hex("4A3524"), true, TextAlignmentOptions.Center);
            float more = Mathf.Clamp01((Accessibility.TextScale - 1f) * 2f);
            t.rectTransform.anchorMin = new Vector2(Mathf.Lerp(0.12f, 0.1f, more), Mathf.Lerp(0.15f, 0.14f, more));
            t.rectTransform.anchorMax = new Vector2(Mathf.Lerp(0.88f, 0.9f, more), Mathf.Lerp(0.45f, 0.48f, more));
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            t.enableAutoSizing = true;
            t.fontSizeMin = 12f;
            t.fontSizeMax = 22f;

            var b = plaque.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            var entry = e;
            b.onClick.AddListener(() => { Sfx.Play(SfxKind.Tap, 0.5f); Read(entry); });
            plaque.gameObject.AddComponent<PressSquash>();
        }

        // ---------------------------------------------------------------- reading one

        void Read(Shrine.Ema e)
        {
            _onRack = false;
            ClearChildren(_list);
            var back = UiKit.Button(_list, "Back", "Back to the rack", UiKit.ButtonStyle.Secondary, Refresh, 28f);
            back.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;
            var page = UiKit.Image(_list, "Page", UiKit.Rounded(30), UiKit.Hex("FBEBD0")).rectTransform;
            var v = page.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(40, 40, 30, 44);
            v.spacing = 10f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            var pic = UiKit.Image(page, "Ema", HobbyData.Art("Ema"), Color.white);
            pic.preserveAspect = true;
            pic.gameObject.AddComponent<LayoutElement>().preferredHeight = 120f;
            var title = UiKit.Text(page, "Title", e.Title, 42f, UiKit.Ink, true, TextAlignmentOptions.Center);
            title.overflowMode = TextOverflowModes.Overflow;
            UiKit.Text(page, "By", e.By, 26f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            var body = UiKit.Text(page, "Text", e.Text, 32f, UiKit.Ink);
            body.overflowMode = TextOverflowModes.Overflow;
            body.lineSpacing = 12f;
            body.margin = new Vector4(0f, 16f, 0f, 0f);
            if (e.Mine >= 0)
            {
                int mine = e.Mine;
                var down = UiKit.Button(_list, "TakeDown", "Take it down", UiKit.ButtonStyle.Secondary, () => { HobbyLog.RemoveBook(mine); Refresh(); }, 26f);
                down.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;
            }
            _scroll.verticalNormalizedPosition = 1f;

            // A little Writing XP for reading, once per ema.
            string key = e.By + "/" + e.Title;
            if (_read.Add(key) && PlayerPrefs.GetInt("CapyIsland.Read." + key.GetHashCode(), 0) == 0)
            {
                PlayerPrefs.SetInt("CapyIsland.Read." + key.GetHashCode(), 1);
                HobbyLog.AddXp(Hobby.Writing, 5);
            }
        }

        // ---------------------------------------------------------------- writing one

        void Write(Action<string, string> written)
        {
            var intro = UiKit.Text(_list, "Intro", "Write your wish, your thanks, or whatever is on your mind. The gods are good listeners.", 28f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            intro.overflowMode = TextOverflowModes.Overflow;
            var title = UiKit.Input(_list, "Title", "Your wish, in a few words", false, 96f);
            title.characterLimit = 40;
            var text = UiKit.Input(_list, "Wish", "I wish...", true, 520f);
            text.characterLimit = 800;
            var problem = UiKit.Text(_list, "Problem", "", 28f, new Color(0.78f, 0.25f, 0.22f), true, TextAlignmentOptions.Center);
            problem.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
            problem.gameObject.SetActive(false);
            var row = UiKit.Rect("Buttons", _list);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            UiKit.Button(row, "Cancel", "Cancel", UiKit.ButtonStyle.Secondary, () => { _pendingWrite = null; Refresh(); }, 30f);
            UiKit.Button(row, "Hang", "Hang it on the rack", UiKit.ButtonStyle.Primary, () =>
            {
                string t = title.text.Trim(), s = text.text.Trim();
                if (s.Length == 0)
                {
                    problem.text = "Write a few words first.";
                    problem.gameObject.SetActive(true);
                    problem.transform.DOKill(true);
                    problem.transform.DOPunchScale(Vector3.one * 0.08f, 0.3f, 6).SetUpdate(true);
                    return;
                }
                if (t.Length == 0) t = "A wish";
                _pendingWrite = null;
                written(t, s);   // pays, hangs it, and shows the rack again
            }, 30f);
        }
    }

    /// <summary>
    /// The sewing machine: a hat (3 cotton from your garden), painted pixel by pixel from a little
    /// palette and saved. Every hat you sew goes into your wardrobe, with the hats, to wear.
    /// </summary>
    public class SewingScreen : UiScreen
    {
        public static SewingScreen Instance { get; private set; }

        /// <summary>The canvas, in stitches each way (hats sewn before were 16 or 32: they are scaled up to open).</summary>
        public const int Size = 64;
        public static readonly Color[] Palette =
        {
            UiKit.Hex("FFFFFF"), UiKit.Hex("2B2B2B"), UiKit.Hex("E8553E"), UiKit.Hex("FF8A3D"), UiKit.Hex("FFC93C"), UiKit.Hex("5DBB63"),
            UiKit.Hex("2DB3A6"), UiKit.Hex("5B9BD5"), UiKit.Hex("A77BD6"), UiKit.Hex("E58FB0"), UiKit.Hex("8A5A32"), UiKit.Hex("C9C9C9"),
        };

        /// <summary>What can be sewn, and what it takes.</summary>
        public static readonly (string kind, string name, (string material, int count)[] needs, string where)[] Patterns =
        {
            ("hat", "Beanie", new[] { ("crop:Cotton", 3) }, "Grow cotton in your garden (gardening level 2)."),
            ("fedora", "Fedora", new[] { ("mat:Silk", 3) }, "Catch silkworms for their silk (bug catching level 2)."),
            ("sunhat", "Sun Hat", new[] { ("crop:Cotton", 2), ("mat:Silk", 2) }, "Grow cotton in your garden and catch silkworms for their silk."),
            // Capes were dropped: they hang behind the capybara, so the painting never showed.
            // Old ones still wear (Wardrobe), and still draw ("cape" in Shape below).
        };

        /// <summary>Everything a pattern needs is in your bag.</summary>
        static bool HaveFor(int pattern)
        {
            foreach (var (m, n) in Patterns[pattern].needs) if (HobbyLog.Count(m) < n) return false;
            return true;
        }

        /// <summary>"2/3 cotton · 0/2 silk": what a pattern takes, and what you have of it.</summary>
        static string NeedsLine(int pattern)
        {
            var parts = new List<string>();
            foreach (var (m, n) in Patterns[pattern].needs)
                parts.Add($"{Mathf.Min(HobbyLog.Count(m), n)}/{n} {Loc.T(HobbyData.ItemName(m)).ToLower()}");
            return string.Join(" · ", parts);
        }

        protected override string Title => "Sewing";
        protected override Sprite TitleIcon => IconPainter.Hat;
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

        // ---------------------------------------------------------------- the shapes

        /// <summary>
        /// A pattern's parts (crown, brim, band, bobble…), each as a signed distance at (u, v):
        /// negative inside. Later parts lie on top of earlier ones. Fills the first
        /// <paramref name="d"/> slots and returns how many parts there are.
        /// </summary>
        static int Parts(string kind, float u, float v, float[] d)
        {
            switch (kind)
            {
                case "hat":
                {
                    // A beanie: a round crown on a turned-up brim, and a bobble on top.
                    d[0] = Mathf.Max(IconPainter.Disc(u, v, 0f, -0.05f, 0.62f), -0.2f - v);
                    d[1] = IconPainter.Box(u, v, 0f, -0.33f, 0.8f, 0.16f, 0.1f);
                    d[2] = IconPainter.Disc(u, v, 0f, 0.66f, 0.17f);
                    return 3;
                }
                case "fedora":
                {
                    // A smart felt fedora: a tall crown pinched in at the top, a band, a wide brim.
                    float crown = Mathf.Max(IconPainter.Box(u, v, 0f, 0.08f, 0.44f, 0.4f, 0.18f), -IconPainter.Disc(u, v, 0f, 0.62f, 0.16f));
                    d[0] = crown;
                    d[1] = Mathf.Max(crown, Mathf.Abs(v + 0.13f) - 0.08f);   // the band round the crown
                    d[2] = IconPainter.Box(u, v, 0f, -0.36f, 0.9f, 0.1f, 0.09f);
                    return 3;
                }
                case "sunhat":
                {
                    // A straw sun hat: a round crown with a ribbon, a very wide brim, a bow at the side.
                    float crown = Mathf.Max(IconPainter.Disc(u, v, 0f, -0.12f, 0.5f), -0.2f - v);
                    d[0] = crown;
                    d[1] = Mathf.Max(crown, Mathf.Abs(v + 0.08f) - 0.07f);   // the ribbon
                    d[2] = IconPainter.Box(u, v, 0f, -0.3f, 0.96f, 0.12f, 0.12f);
                    d[3] = Mathf.Min(IconPainter.Disc(u, v, 0.5f, -0.05f, 0.14f), IconPainter.Disc(u, v, 0.7f, 0.02f, 0.12f));
                    return 4;
                }
                case "cape":
                {
                    // Narrow at the shoulders, flaring out to a wavy hem, with a collar.
                    float t = Mathf.InverseLerp(0.7f, -0.85f, v);
                    float half = Mathf.Lerp(0.36f, 0.86f, t);
                    float side = Mathf.Abs(u) - half;
                    float top = v - 0.7f, bottom = -0.85f + Mathf.Sin(u * 9f) * 0.04f - v;
                    d[0] = Mathf.Max(side, Mathf.Max(top, bottom));
                    d[1] = IconPainter.Box(u, v, 0f, 0.74f, 0.42f, 0.09f, 0.08f);
                    return 2;
                }
                default:
                    d[0] = IconPainter.ShirtShape(u, v);
                    return 1;
            }
        }

        [System.ThreadStatic] static float[] _parts;

        /// <summary>The outline of a pattern, as a signed distance (negative inside). Shirts are the old designs.</summary>
        public static float Shape(string kind, float u, float v)
        {
            var d = _parts ??= new float[4];
            int n = Parts(kind, u, v, d);
            float s = d[0];
            for (int i = 1; i < n; i++) s = Mathf.Min(s, d[i]);
            return s;
        }

        /// <summary>
        /// Which part of the pattern a point is on (the topmost one there), or -1 off it, and how
        /// far inside that part's own edge it is (for the seams between parts).
        /// </summary>
        static int Region(string kind, float u, float v, out float edge)
        {
            var d = _parts ??= new float[4];
            int n = Parts(kind, u, v, d);
            for (int i = n - 1; i >= 0; i--)
                if (d[i] < 0f) { edge = d[i]; return i; }
            edge = 1f;
            return -1;
        }

        static int RegionAt(string kind, int x, int y) =>
            Region(kind, (x + 0.5f) / Size * 2f - 1f, (y + 0.5f) / Size * 2f - 1f, out _);

        static bool Inside(string kind, int x, int y)
        {
            float u = (x + 0.5f) / Size * 2f - 1f, v = (y + 0.5f) / Size * 2f - 1f;
            return Shape(kind, u, v) < 0f;
        }

        protected override bool FullBleed => true;

        /// <summary>Where, up the painting, the mannequin stands: on the rug, in front of the wall (which meets the floor at 0.38).</summary>
        const float PaintedFloor = 0.3f;

        RectTransform _body, _painting, _figure, _hat, _panel, _zoomBar;
        Vector2 _figureHome;
        bool _wide;

        bool IsWide() => _body.rect.width > _body.rect.height * 1.1f;

        /// <summary>
        /// A little tailor's corner: the painted room, a capybara mannequin standing on the rug
        /// with the hat you are making on its head (you paint it right there, zooming in as close
        /// as you like), and a workbench of colours, tools and stickers along the bottom (or down
        /// the right when the phone is turned).
        /// </summary>
        protected override void Build(RectTransform body)
        {
            _body = body;

            // The room.
            var wash = UiKit.Image(body, "Room", null, UiKit.Hex("E9C9B8"), true);
            UiKit.Stretch(wash.rectTransform, -400f, -400f, -400f, -400f);
            var wall = UiKit.Rect("Wall", body);
            UiKit.Stretch(wall, -60f, -60f, -60f, -60f);
            wall.gameObject.AddComponent<RectMask2D>();
            var art = HobbyData.Art("Sew_Room");
            if (art != null)
            {
                var pic = UiKit.Image(wall, "Painting", art, Color.white);
                _painting = pic.rectTransform;
                _painting.pivot = new Vector2(0.5f, PaintedFloor);
            }

            // The stage: the mannequin, and on its head the hat, together in a window you can
            // zoom into (pinch, the mouse wheel, or + and −) and move about with two fingers.
            var stage = UiKit.Image(body, "Stage", null, new Color(0f, 0f, 0f, 0f), true);
            stage.gameObject.AddComponent<PanSurface>().Owner = this;   // the hand tool drags anywhere on the stage
            stage.gameObject.AddComponent<RectMask2D>();
            _window = stage.rectTransform;
            _figure = UiKit.Rect("Figure", _window);
            _figure.anchorMin = _figure.anchorMax = _figure.pivot = new Vector2(0.5f, 0.5f);
            _zoomRoot = _figure;
            var mannequin = UiKit.Image(_figure, "Mannequin", HobbyData.Art("Sew_Mannequin"), Color.white);
            mannequin.preserveAspect = true;
            UiKit.Stretch(mannequin.rectTransform);
            _hat = UiKit.Rect("Hat", _figure);
            _hat.anchorMin = _hat.anchorMax = HatOnHead;
            _hat.pivot = new Vector2(0.5f, 0.5f);
            _silhouette = UiKit.Image(_hat, "Silhouette", null, Color.white);
            _silhouette.preserveAspect = true;
            UiKit.Stretch(_silhouette.rectTransform);
            _texture = new Texture2D(View, View, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var raw = UiKit.Rect("Paint", _hat);
            UiKit.Stretch(raw);
            _canvas = raw.gameObject.AddComponent<RawImage>();
            _canvas.texture = _texture;
            raw.gameObject.AddComponent<PaintSurface>().Owner = this;
            _seamOverlay = UiKit.Image(_hat, "Seams", null, Color.white);
            _seamOverlay.preserveAspect = true;
            UiKit.Stretch(_seamOverlay.rectTransform);

            _zoomBar = UiKit.Rect("ZoomBar", body);
            _zoomBar.pivot = new Vector2(1f, 1f);
            _zoomBar.sizeDelta = new Vector2(84f, 272f);
            var zv = _zoomBar.gameObject.AddComponent<VerticalLayoutGroup>();
            zv.spacing = 10f;
            zv.childControlWidth = zv.childControlHeight = true;
            zv.childForceExpandWidth = zv.childForceExpandHeight = true;
            // Bigger buttons for Easy Reading's "Fit" (longer in Hebrew). A little less growth down
            // than across, because the gaps between them stay the same: they stay round.
            GrowsWithText.Add(_zoomBar, tall: 0.55f, wide: 0.6f);
            ZoomButton(_zoomBar, "+", () => ZoomBy(1.6f, HatCentre()));
            ZoomButton(_zoomBar, "−", () => ZoomBy(1f / 1.6f, HatCentre()));
            ZoomButton(_zoomBar, "Fit", Recentre);

            TopBar(body);

            // The workbench: everything else, scrolling.
            _panel = UiKit.Card(body, "Workbench", UiKit.Surface, 40, true, true).rectTransform;
            var scroll = UiKit.ScrollList(_panel, "List", out var list, 16f, new RectOffset(4, 4, 8, 32));
            UiKit.Stretch((RectTransform)scroll.transform, 24f, 16f, 24f, 20f);

            // Which pattern.
            _patternRow = UiKit.Rect("Patterns", list);
            var ph = _patternRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            ph.spacing = 14f;
            ph.childControlWidth = ph.childControlHeight = true;
            ph.childForceExpandWidth = true;
            var patternLayout = _patternRow.gameObject.AddComponent<LayoutElement>();
            patternLayout.preferredHeight = 112f;
            GrowsWithText.Add(patternLayout, tall: 1f);   // Easy Reading: room for what each needs, a line apiece (Patterns_)

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
                // A thin ring round every swatch, so the white one shows on the white bench.
                var ring = UiKit.Image(sw.transform, "Ring", IconPainter.Ring, new Color(0.23f, 0.16f, 0.12f, 0.45f));
                UiKit.Stretch(ring.rectTransform);
                var b = sw.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { _colour = index; _erase = false; if (_tool == Tool.Sticker) Pick(Tool.Brush); Swatches(); });
                _swatches.Add(sw);
            }
            var eraser = UiKit.Image(pal, "Eraser", UiKit.Circle, UiKit.SurfaceSunk, true);
            var x = UiKit.Image(eraser.transform, "X", IconPainter.Close, UiKit.InkSoft);
            UiKit.Stretch(x.rectTransform, 16f, 16f, 16f, 16f);
            var eb = eraser.gameObject.AddComponent<Button>();
            eb.transition = Selectable.Transition.None;
            eb.onClick.AddListener(() => { _erase = true; if (_tool == Tool.Sticker) Pick(Tool.Brush); Swatches(); });
            _swatches.Add(eraser);

            // Tools: paint pixel by pixel, fill a whole area, or take back the last change.
            var tools = UiKit.Rect("Tools", list);
            var th = tools.gameObject.AddComponent<HorizontalLayoutGroup>();
            th.spacing = 14f;
            th.childControlWidth = th.childControlHeight = true;
            th.childForceExpandWidth = true;
            var toolsLayout = tools.gameObject.AddComponent<LayoutElement>();
            toolsLayout.preferredHeight = 84f;
            GrowsWithText.Add(toolsLayout, tall: 0.6f);
            _tools = tools;
            _brushButton = UiKit.Button(tools, "Brush", "Brush", UiKit.ButtonStyle.Secondary, () => Pick(Tool.Brush), 26f, IconPainter.Brush);
            // The brush's size: three dots, small to large, beside it.
            var sizes = UiKit.Rect("Sizes", tools);
            var sh = sizes.gameObject.AddComponent<HorizontalLayoutGroup>();
            sh.spacing = 6f;
            sh.childAlignment = TextAnchor.MiddleCenter;
            sh.childControlWidth = sh.childControlHeight = false;
            sh.childForceExpandWidth = sh.childForceExpandHeight = false;
            for (int i = 0; i < BrushSizes.Length; i++)
            {
                int index = i;
                var back = UiKit.Image(sizes, "Size" + i, UiKit.Circle, UiKit.SurfaceSunk, true);
                back.rectTransform.sizeDelta = new Vector2(64f, 64f);
                var dot = UiKit.Image(back.transform, "Dot", UiKit.Circle, UiKit.Ink);
                float r = 12f + i * 9f;
                UiKit.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(r, r));
                dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var bb = back.gameObject.AddComponent<Button>();
                bb.transition = Selectable.Transition.None;
                bb.onClick.AddListener(() => { _brush = index; if (_tool != Tool.Brush) Pick(Tool.Brush); BrushDots(); });
                _sizeDots.Add(back);
            }
            _fillButton = UiKit.Button(tools, "Fill", "Fill", UiKit.ButtonStyle.Secondary, () => Pick(Tool.Fill), 26f, IconPainter.Bucket);
            _handButton = UiKit.Button(tools, "Hand", "Move", UiKit.ButtonStyle.Secondary, () => Pick(Tool.Hand), 26f, TutorialPointer.HandSprite);
            var undo = UiKit.Button(tools, "Undo", "Undo", UiKit.ButtonStyle.Secondary, Undo, 26f, IconPainter.Undo);
            foreach (var b in new[] { _fillButton, _handButton, undo }) _moreTools.Add((RectTransform)b.transform.parent);

            // Easy Reading's second row of tools (SplitTools): five side by side only have room for small words.
            _toolsMore = UiKit.Rect("MoreTools", list);
            var mh = _toolsMore.gameObject.AddComponent<HorizontalLayoutGroup>();
            mh.spacing = 14f;
            mh.childControlWidth = mh.childControlHeight = true;
            mh.childForceExpandWidth = true;
            var moreLayout = _toolsMore.gameObject.AddComponent<LayoutElement>();
            moreLayout.preferredHeight = 84f;
            GrowsWithText.Add(moreLayout, tall: 0.6f);
            _toolsMore.gameObject.SetActive(false);

            // Stickers: little ready-made pictures to stamp on, for when you don't want to paint it all.
            SectionLabel(list, "Stickers");
            var stickers = UiKit.Rect("Stickers", list);
            var sg = stickers.gameObject.AddComponent<GridLayoutGroup>();
            sg.cellSize = new Vector2(84f, 84f);
            sg.spacing = new Vector2(12f, 12f);
            sg.childAlignment = TextAnchor.MiddleCenter;
            sg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            sg.constraintCount = Stickers.Length;
            stickers.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            for (int i = 0; i < Stickers.Length; i++)
            {
                int index = i;
                var back = UiKit.Image(stickers, Stickers[i].name, UiKit.Rounded(20), Color.white, true);
                var stickerArt = UiKit.Image(back.transform, "Art", StickerSprite(i), Color.white);
                stickerArt.preserveAspect = true;
                UiKit.Stretch(stickerArt.rectTransform, 12f, 12f, 12f, 12f);
                var sb = back.gameObject.AddComponent<Button>();
                sb.transition = Selectable.Transition.None;
                sb.onClick.AddListener(() => { _sticker = index; Pick(Tool.Sticker); });
                back.gameObject.AddComponent<PressSquash>();
                _stickerTiles.Add(back);
            }

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

        /// <summary>Where the hat sits on the mannequin, as a share of the mannequin's picture, and how wide it is.</summary>
        static readonly Vector2 HatOnHead = new(0.5f, 0.84f);
        const float HatWidth = 0.55f;

        /// <summary>How high the top of the hat comes, over the mannequin's feet, as a share of its picture (the bobble pokes above it).</summary>
        const float HatTop = 0.84f + HatWidth * 0.5f - 0.04f;

        void TopBar(RectTransform body)
        {
            var chip = UiKit.Image(body, "TitleChip", UiKit.Rounded(40), UiKit.HudGlass).rectTransform;
            chip.anchorMin = chip.anchorMax = chip.pivot = new Vector2(0f, 1f);
            chip.anchoredPosition = new Vector2(28f, -28f);
            chip.sizeDelta = new Vector2(300f, 88f);
            var icon = UiKit.Image(chip, "Icon", IconPainter.Hat, UiKit.Hex("FFC2D6"));
            icon.preserveAspect = true;
            UiKit.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(54f, 54f));
            var t = UiKit.Text(chip, "Title", Title, 44f, Color.white, true, TextAlignmentOptions.MidlineLeft);
            UiKit.Stretch(t.rectTransform, 88f, 0f, 16f, 0f);
            FitsText.Add(chip, t, new Vector2(104f, 33f), wide: true, tall: true);   // the title as drawn: translated, at Easy Reading's size
            t.textWrappingMode = TextWrappingModes.NoWrap;

            var close = UiKit.Image(body, "Close", UiKit.Circle, UiKit.HudGlass, true).rectTransform;
            close.anchorMin = close.anchorMax = close.pivot = new Vector2(1f, 1f);
            close.anchoredPosition = new Vector2(-28f, -28f);
            close.sizeDelta = new Vector2(96f, 96f);
            var x = UiKit.Image(close, "X", IconPainter.Close, Color.white);
            UiKit.Stretch(x.rectTransform, 28f, 28f, 28f, 28f);
            var b = close.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(Close);
            close.gameObject.AddComponent<PressSquash>();
        }

        /// <summary>The hat's middle, in the window's own coordinates (from its centre): where the zoom buttons zoom to.</summary>
        Vector2 HatCentre()
        {
            var world = _hat.TransformPoint(_hat.rect.center);
            var local = (Vector2)_window.InverseTransformPoint(world);
            return local - _window.rect.center;
        }

        /// <summary>Lays the room out for the screen as it is: upright, workbench along the bottom; turned, down the right.</summary>
        void Arrange()
        {
            if (_body == null) return;
            _wide = IsWide();
            float w = _body.rect.width, h = _body.rect.height;
            const float top = 140f;   // under the title and close button

            float floorY, standX, size;
            if (_wide)
            {
                _panel.anchorMin = new Vector2(0.55f, 0f);
                _panel.anchorMax = new Vector2(1f, 1f);
                _panel.offsetMin = new Vector2(0f, 24f);
                _panel.offsetMax = new Vector2(-24f, -top);
                _window.anchorMin = Vector2.zero;
                _window.anchorMax = new Vector2(0.55f, 1f);
                _window.offsetMin = Vector2.zero;
                _window.offsetMax = new Vector2(0f, -top);
                floorY = Mathf.Max(60f, h * 0.08f);
                standX = w * 0.275f;
                size = Mathf.Min(w * 0.5f, (h - top - floorY - 20f) / HatTop);
            }
            else
            {
                float panelH = Mathf.Min(h * 0.5f, 1150f);
                _panel.anchorMin = Vector2.zero;
                _panel.anchorMax = new Vector2(1f, 0f);
                _panel.pivot = new Vector2(0.5f, 0f);
                _panel.sizeDelta = new Vector2(-48f, panelH);
                _panel.anchoredPosition = new Vector2(0f, 24f);
                _window.anchorMin = Vector2.zero;
                _window.anchorMax = Vector2.one;
                _window.offsetMin = new Vector2(0f, panelH + 24f);
                _window.offsetMax = new Vector2(0f, -top);
                floorY = panelH + 36f;
                standX = w * 0.5f;
                size = Mathf.Min(w * 0.92f, (h - top - floorY - 20f) / HatTop);
            }

            // The mannequin stands on the rug: its feet (a little up from the bottom of its picture) on the floor.
            _figure.sizeDelta = new Vector2(size, size);
            var win = _window.rect;
            float windowBottom = _wide ? 0f : floorY - 12f;   // the stage starts just above the workbench
            _figureHome = new Vector2(0f, floorY - windowBottom - win.height * 0.5f + size * 0.46f);
            _zoom = 1f;
            _figure.localScale = Vector3.one;
            _figure.anchoredPosition = _figureHome;
            _hat.sizeDelta = Vector2.one * size * HatWidth;

            // The painting, lined up so its rug is under the mannequin's feet.
            if (_painting != null && _painting.GetComponent<Image>().sprite is Sprite art)
            {
                float aspect = art.rect.width / art.rect.height;
                float bw = w + 120f, bh = h + 120f, y = floorY + 60f;
                float tall = Mathf.Max(bh, bw / aspect, y / PaintedFloor, (bh - y) / (1f - PaintedFloor));
                _painting.anchorMin = _painting.anchorMax = new Vector2(standX / w, 0f);
                _painting.sizeDelta = new Vector2(tall * aspect, tall);
                _painting.anchoredPosition = new Vector2(0f, y);
            }

            // Zoom buttons at the top corner of the stage.
            _zoomBar.anchorMin = _zoomBar.anchorMax = new Vector2(_wide ? 0.55f : 1f, 1f);
            _zoomBar.anchoredPosition = new Vector2(-24f, -top - 10f);
        }

        RectTransform _tools, _toolsMore;
        readonly List<RectTransform> _moreTools = new();

        /// <summary>
        /// With Easy Reading, Fill, Move and Undo go down to a row of their own, so every tool's
        /// word has room to be drawn big; at normal size all five share one row.
        /// </summary>
        void SplitTools()
        {
            bool two = Accessibility.TextScale > 1.2f;
            foreach (var t in _moreTools) t.SetParent(two ? _toolsMore : _tools, false);
            _toolsMore.gameObject.SetActive(two);
        }

        protected override void OnOpen()
        {
            Arrange();
            SplitTools();
            Swatches();
            Pick(_tool);
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
                // With Easy Reading each thing it needs gets a line: side by side they could only shrink.
                string needs = NeedsLine(i);
                if (Accessibility.TextScale > 1.2f) needs = needs.Replace(" · ", "</size>\n<size=65%>");
                string label = $"{p.name}\n<size=65%>{needs}</size>";
                var b = UiKit.Button(_patternRow, p.name, label, index == _pattern ? UiKit.ButtonStyle.Primary : UiKit.ButtonStyle.Secondary, () =>
                {
                    if (_pattern == index) return;
                    _pattern = index;
                    _editing = -1;
                    Clear();
                    Patterns_();
                    Silhouette();
                    Status(HaveFor(index) ? "Paint your " + Patterns[index].name.ToLower() + "!" : Patterns[index].where);
                }, 28f);
                // The words get the button's full height, not just the height they asked for at the
                // size they were last drawn: otherwise three lines settle smaller than the room allows.
                b.GetComponentInChildren<HorizontalLayoutGroup>().childForceExpandHeight = true;
            }
        }

        void Silhouette()
        {
            _silhouette.sprite = DesignSprite(null, Kind);
            _seamOverlay.sprite = SeamSprite(Kind);
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
            _undo.Clear();
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = '.';
            Redraw();
        }

        // ---------------------------------------------------------------- tools

        enum Tool { Brush, Fill, Sticker, Hand }
        Tool _tool;
        int _sticker;

        /// <summary>Brush widths in stitches: fine, medium, broad.</summary>
        static readonly int[] BrushSizes = { 1, 3, 6 };
        int _brush;
        readonly List<Image> _sizeDots = new();
        int _lastX = -1, _lastY = -1;

        Image _seamOverlay;
        RectTransform _window, _zoomRoot;
        float _zoom = 1f;
        const float MaxZoom = 4f;

        void BrushDots()
        {
            for (int i = 0; i < _sizeDots.Count; i++)
            {
                bool on = _tool == Tool.Brush && i == _brush;
                _sizeDots[i].color = on ? UiKit.Hex("FFE7B8") : UiKit.SurfaceSunk;
                _sizeDots[i].transform.localScale = Vector3.one * (on ? 1.12f : 1f);
            }
        }

        // ---------------------------------------------------------------- zoom

        static void ZoomButton(Transform parent, string label, System.Action act)
        {
            var b = UiKit.Image(parent, "Zoom" + label, UiKit.Circle, new Color(1f, 1f, 1f, 0.92f), true);
            var t = UiKit.Text(b.transform, "Label", label, label.Length > 1 ? 26f : 48f, UiKit.Ink, true, TextAlignmentOptions.Center);
            UiKit.Stretch(t.rectTransform);
            var button = b.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => act());
            b.gameObject.AddComponent<PressSquash>();
        }

        /// <summary>Zooms by a factor, keeping <paramref name="about"/> (a point in the window, from its centre) still.</summary>
        void ZoomBy(float factor, Vector2 about)
        {
            float before = _zoom;
            _zoom = Mathf.Clamp(_zoom * factor, 1f, MaxZoom);
            float k = _zoom / before;
            Pan(_zoomRoot.anchoredPosition * k + about * (1f - k) - _zoomRoot.anchoredPosition);
        }

        /// <summary>All the way back out, the mannequin in the middle again.</summary>
        void Recentre()
        {
            _zoom = 1f;
            _zoomRoot.localScale = Vector3.one;
            _zoomRoot.anchoredPosition = _figureHome;
        }

        /// <summary>The hand tool: a drag moves the zoomed mannequin about (in screen pixels).</summary>
        internal void Drag(Vector2 screenDelta)
        {
            var canvas = _window.GetComponentInParent<Canvas>();
            Pan(screenDelta / (canvas != null ? canvas.scaleFactor : 1f));
        }

        internal bool HandTool => _tool == Tool.Hand;

        /// <summary>The stage around the mannequin: with the hand tool, dragging here moves it about too.</summary>
        class PanSurface : MonoBehaviour, IDragHandler
        {
            public SewingScreen Owner;
            public void OnDrag(PointerEventData e)
            {
                if (Owner.HandTool && Input.touchCount < 2) Owner.Drag(e.delta);
            }
        }

        /// <summary>Moves the zoomed mannequin about; the further in, the further it may go (back home at no zoom).</summary>
        void Pan(Vector2 delta)
        {
            _zoomRoot.localScale = Vector3.one * _zoom;
            var reach = _figure.sizeDelta * (_zoom - 1f) * 0.6f;
            var off = _zoomRoot.anchoredPosition + delta - _figureHome;
            _zoomRoot.anchoredPosition = _figureHome + new Vector2(Mathf.Clamp(off.x, -reach.x, reach.x), Mathf.Clamp(off.y, -reach.y, reach.y));
        }

        bool _pinching;
        float _pinchStarted;

        protected override void Update()
        {
            base.Update();
            if (!IsOpen || _window == null) return;
            if (IsWide() != _wide) Arrange();   // the phone was turned

            // The mouse wheel, over the window: zoom in towards the pointer.
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f && RectTransformUtility.ScreenPointToLocalPointInRectangle(_window, Input.mousePosition, null, out var at) && _window.rect.Contains(at))
                ZoomBy(Mathf.Pow(1.15f, wheel), at);

            // Two fingers: pinch to zoom, drag to move about. The first finger's dab is taken back.
            if (Input.touchCount >= 2)
            {
                var a = Input.GetTouch(0); var b = Input.GetTouch(1);
                if (!_pinching)
                {
                    _pinching = true;
                    if (Time.unscaledTime - _pinchStarted < 0.35f) Undo();
                }
                var prevA = a.position - a.deltaPosition; var prevB = b.position - b.deltaPosition;
                float before = Vector2.Distance(prevA, prevB), now = Vector2.Distance(a.position, b.position);
                var canvas = _window.GetComponentInParent<Canvas>();
                float scale = canvas != null ? canvas.scaleFactor : 1f;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_window, (a.position + b.position) * 0.5f, null, out var mid);
                if (before > 1f) ZoomBy(now / before, mid);
                Pan(((a.deltaPosition + b.deltaPosition) * 0.5f) / scale);
            }
            else if (Input.touchCount == 0) _pinching = false;
        }
        Button _brushButton, _fillButton, _handButton;
        readonly List<Image> _stickerTiles = new();
        readonly List<string> _undo = new();

        /// <summary>
        /// Stickers: little drawn pictures, each a shape (signed distance on -1…1, negative inside)
        /// and the palette colour at each point of it (a white, b black, c red, d orange,
        /// e yellow, f green, g teal, h blue, i purple, j pink, k brown, l grey). Drawn at whatever
        /// size they are stuck on, with a thin dark outline, like a real sticker.
        /// </summary>
        static readonly (string name, Func<float, float, float> shape, Func<float, float, char> colour)[] Stickers =
        {
            ("Heart", (u, v) => Mathf.Min(Mathf.Min(IconPainter.Disc(u, v, -0.36f, 0.25f, 0.44f), IconPainter.Disc(u, v, 0.36f, 0.25f, 0.44f)),
                                          IconPainter.Triangle(u, v, new Vector2(-0.78f, 0.1f), new Vector2(0.78f, 0.1f), new Vector2(0f, -0.85f), 0.06f)),
                      (u, v) => IconPainter.Disc(u, v, -0.42f, 0.38f, 0.14f) < 0f ? 'a' : 'c'),
            ("Star", (u, v) => StarShape(u, v),
                     (u, v) => 'e'),
            ("Smile", (u, v) => IconPainter.Disc(u, v, 0f, 0f, 0.86f),
                      (u, v) => IconPainter.Disc(u, v, -0.3f, 0.22f, 0.12f) < 0f || IconPainter.Disc(u, v, 0.3f, 0.22f, 0.12f) < 0f
                                || (Mathf.Abs(IconPainter.Disc(u, v, 0f, 0.05f, 0.48f)) < 0.07f && v < -0.12f) ? 'b' : 'e'),
            ("Flower", (u, v) => FlowerShape(u, v),
                       (u, v) => IconPainter.Disc(u, v, 0f, 0f, 0.28f) < 0f ? 'e' : 'j'),
            ("Leaf", (u, v) => LeafShape(u, v),
                     (u, v) => Mathf.Abs(u - v) < 0.06f && u + v < 1.1f ? 'g' : 'f'),
            ("Paw", (u, v) => Mathf.Min(IconPainter.Disc(u, v, 0f, -0.3f, 0.48f), Mathf.Min(
                                Mathf.Min(IconPainter.Disc(u, v, -0.62f, 0.2f, 0.2f), IconPainter.Disc(u, v, -0.24f, 0.55f, 0.2f)),
                                Mathf.Min(IconPainter.Disc(u, v, 0.24f, 0.55f, 0.2f), IconPainter.Disc(u, v, 0.62f, 0.2f, 0.2f)))),
                    (u, v) => 'k'),
            ("Fish", (u, v) => Mathf.Min(IconPainter.Box(u, v, -0.15f, 0f, 0.6f, 0.38f, 0.38f),
                                         IconPainter.Triangle(u, v, new Vector2(0.38f, 0f), new Vector2(0.9f, 0.4f), new Vector2(0.9f, -0.4f), 0.04f)),
                     (u, v) => IconPainter.Disc(u, v, -0.45f, 0.08f, 0.09f) < 0f ? 'b' : 'h'),
            ("Mushroom", (u, v) => Mathf.Min(Mathf.Max(IconPainter.Disc(u, v, 0f, -0.05f, 0.82f), -0.05f - v), IconPainter.Box(u, v, 0f, -0.42f, 0.26f, 0.38f, 0.1f)),
                         (u, v) => v < -0.05f ? 'a'
                                   : IconPainter.Disc(u, v, -0.36f, 0.3f, 0.14f) < 0f || IconPainter.Disc(u, v, 0.2f, 0.52f, 0.12f) < 0f || IconPainter.Disc(u, v, 0.46f, 0.14f, 0.1f) < 0f ? 'a' : 'c'),
        };

        static float StarShape(float u, float v)
        {
            // Five points: the edge swings between an outer and an inner radius every 36°.
            float a = Mathf.Atan2(u, v), r = Mathf.Sqrt(u * u + v * v);
            float seg = 2f * Mathf.PI / 5f;
            float t = Mathf.Abs(Mathf.Repeat(a, seg) - seg * 0.5f) / (seg * 0.5f);   // 1 at a point, 0 between
            return (r - Mathf.Lerp(0.42f, 0.95f, t)) * 0.8f;
        }

        static float FlowerShape(float u, float v)
        {
            float d = IconPainter.Disc(u, v, 0f, 0f, 0.3f);
            for (int i = 0; i < 5; i++)
            {
                float ang = Mathf.PI / 2f + i * 2f * Mathf.PI / 5f;
                d = Mathf.Min(d, IconPainter.Disc(u, v, Mathf.Cos(ang) * 0.48f, Mathf.Sin(ang) * 0.48f, 0.34f));
            }
            return d;
        }

        static float LeafShape(float u, float v)
        {
            // A pointed oval, tipped corner to corner.
            float a = (u + v) * 0.7071f, b = (v - u) * 0.7071f;
            return Mathf.Max(IconPainter.Disc(a, b, 0f, -0.5f, 0.85f), IconPainter.Disc(a, b, 0f, 0.5f, 0.85f));
        }

        /// <summary>What a sticker puts at (u, v): its colour inside, black on its rim, nothing outside.</summary>
        static char StickerAt(int index, float u, float v)
        {
            float d = Stickers[index].shape(u, v);
            if (d > 0.1f) return '.';
            return d > -0.02f ? 'b' : Stickers[index].colour(u, v);
        }

        static readonly Dictionary<int, Sprite> _stickerArt = new();

        /// <summary>A sticker's picture for its button.</summary>
        static Sprite StickerSprite(int index)
        {
            if (_stickerArt.TryGetValue(index, out var s) && s != null) return s;
            const int n = 48;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color32[n * n];
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                char ch = StickerAt(index, (i + 0.5f) / n * 2f - 1f, (j + 0.5f) / n * 2f - 1f);
                if (ch != '.') px[j * n + i] = Palette[ch - 'a'];
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return _stickerArt[index] = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>How many stitches across a sticker is stuck on.</summary>
        const int StickerSize = 20;

        void Pick(Tool tool)
        {
            _tool = tool;
            void Shade(Button b, bool on)
            {
                if (b == null) return;
                b.GetComponent<Image>().color = on ? UiKit.Primary : UiKit.SurfaceSunk;
                foreach (var g in b.GetComponentsInChildren<Graphic>())
                    if (g != b.targetGraphic) g.color = on ? Color.white : UiKit.Ink;
            }
            Shade(_brushButton, tool == Tool.Brush);
            Shade(_fillButton, tool == Tool.Fill);
            Shade(_handButton, tool == Tool.Hand);
            BrushDots();
            for (int i = 0; i < _stickerTiles.Count; i++)
            {
                bool on = tool == Tool.Sticker && i == _sticker;
                _stickerTiles[i].color = on ? UiKit.Hex("FFE7B8") : Color.white;
                _stickerTiles[i].transform.localScale = Vector3.one * (on ? 1.12f : 1f);
            }
            if (_status != null)
                Status(tool switch
                {
                    Tool.Fill => _erase ? "Tap an area to clear it." : "Tap an area to fill it with your colour.",
                    Tool.Sticker => "Tap the hat to stick it on.",
                    Tool.Hand => "Drag to move around. Zoom in to see the details.",
                    _ => "Tap or drag to paint.",
                });
        }

        /// <summary>Takes back the last change (a stroke, a fill, a sticker).</summary>
        void Undo()
        {
            if (_undo.Count == 0) { Status("Nothing to undo."); return; }
            var last = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            for (int i = 0; i < _pixels.Length && i < last.Length; i++) _pixels[i] = last[i];
            Redraw();
        }

        /// <summary>A touch on the canvas: the start of a stroke (<paramref name="first"/>) or a drag along it.</summary>
        internal void Touch(Vector2 normalised, bool first)
        {
            if (_pinching || Input.touchCount >= 2) return;   // two fingers move the hat about
            int x = Mathf.FloorToInt(normalised.x * Size), y = Mathf.FloorToInt(normalised.y * Size);
            if (x < 0 || y < 0 || x >= Size || y >= Size) return;
            if (first)
            {
                _pinchStarted = Time.unscaledTime;
                _undo.Add(new string(_pixels));
                if (_undo.Count > 30) _undo.RemoveAt(0);
            }
            switch (_tool)
            {
                case Tool.Fill:
                    if (first) Fill(x, y);
                    break;
                case Tool.Sticker:
                    if (first) Stamp(x, y);
                    break;
                default:
                    // A quick drag skips stitches: fill in the line between this touch and the last.
                    if (first || _lastX < 0) Dab(x, y);
                    else
                    {
                        int steps = Mathf.Max(Mathf.Abs(x - _lastX), Mathf.Abs(y - _lastY));
                        for (int s = 1; s <= steps; s++)
                            Dab(_lastX + Mathf.RoundToInt((x - _lastX) * s / (float)steps), _lastY + Mathf.RoundToInt((y - _lastY) * s / (float)steps));
                    }
                    Redraw();
                    break;
            }
            _lastX = x; _lastY = y;
        }

        /// <summary>One touch of the brush: a little square as wide as the brush size.</summary>
        void Dab(int cx, int cy)
        {
            int w = BrushSizes[_brush], from = -(w - 1) / 2;
            char c = _erase ? '.' : (char)('a' + _colour);
            for (int dy = 0; dy < w; dy++)
            for (int dx = 0; dx < w; dx++)
            {
                int x = cx + from + dx, y = cy + from + dy;
                if (x < 0 || y < 0 || x >= Size || y >= Size || !Inside(Kind, x, y)) continue;
                _pixels[y * Size + x] = c;
            }
        }

        /// <summary>The paint bucket: everything joined to this pixel in the same colour takes the new one.</summary>
        void Fill(int x, int y)
        {
            if (!Inside(Kind, x, y)) return;
            char from = _pixels[y * Size + x], to = _erase ? '.' : (char)('a' + _colour);
            if (from == to) return;
            int part = RegionAt(Kind, x, y);   // the seams between parts stop the paint
            var open = new Stack<(int, int)>();
            open.Push((x, y));
            while (open.Count > 0)
            {
                var (px, py) = open.Pop();
                if (px < 0 || py < 0 || px >= Size || py >= Size) continue;
                int i = py * Size + px;
                if (_pixels[i] != from || !Inside(Kind, px, py) || RegionAt(Kind, px, py) != part) continue;
                _pixels[i] = to;
                open.Push((px + 1, py)); open.Push((px - 1, py)); open.Push((px, py + 1)); open.Push((px, py - 1));
            }
            Sfx.Play(SfxKind.Plop, 0.5f);
            Redraw();
        }

        /// <summary>Sticks the chosen sticker on, centred where you tapped (only the part on the hat).</summary>
        void Stamp(int cx, int cy)
        {
            int n = StickerSize, from = -n / 2;
            for (int sy = 0; sy < n; sy++)
            for (int sx = 0; sx < n; sx++)
            {
                char ch = StickerAt(_sticker, (sx + 0.5f) / n * 2f - 1f, (sy + 0.5f) / n * 2f - 1f);
                if (ch == '.') continue;
                int x = cx + from + sx, y = cy + from + sy;
                if (x < 0 || y < 0 || x >= Size || y >= Size || !Inside(Kind, x, y)) continue;
                _pixels[y * Size + x] = ch;
            }
            Sfx.Play(SfxKind.Pop, 0.5f);
            Redraw();
        }


        /// <summary>The painted canvas is drawn finer than the stitches, so the paint can stop exactly at the hat's edge.</summary>
        const int View = 128;
        static readonly Dictionary<string, bool[]> _masks = new();
        Color32[] _view;

        /// <summary>Which of the canvas's fine pixels are on the hat (for clipping the paint to its outline).</summary>
        static bool[] Mask(string kind)
        {
            if (_masks.TryGetValue(kind, out var m)) return m;
            m = new bool[View * View];
            for (int j = 0; j < View; j++)
            for (int i = 0; i < View; i++)
                m[j * View + i] = Shape(kind, (i + 0.5f) / View * 2f - 1f, (j + 0.5f) / View * 2f - 1f) < 0f;
            return _masks[kind] = m;
        }

        void Redraw()
        {
            var mask = Mask(Kind);
            _view ??= new Color32[View * View];
            for (int j = 0; j < View; j++)
            for (int i = 0; i < View; i++)
            {
                char c = _pixels[(j * Size / View) * Size + i * Size / View];
                _view[j * View + i] = c == '.' || !mask[j * View + i] ? new Color32(0, 0, 0, 0) : (Color32)Palette[c - 'a'];
            }
            _texture.SetPixels32(_view);
            _texture.Apply();
        }

        /// <summary>Sews it: a new piece uses its material; changing one you made before is free.</summary>
        void Save(bool wear)
        {
            var p = Patterns[_pattern];
            bool fresh = _editing < 0;
            if (fresh)
            {
                if (!HaveFor(_pattern))
                {
                    Status(Loc.F("A {0} needs {1}. {2}", Loc.T(p.name).ToLower(), NeedsLine(_pattern), Loc.T(p.where)));
                    return;
                }
                foreach (var (m, n) in p.needs) HobbyLog.Take(m, n);
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
                    if (pattern < 0) { Status("That's one of your older pieces. You can still wear it, but it can't be changed any more."); return; }
                    _pattern = pattern;
                    _editing = index;
                    _undo.Clear();
                    // Older hats were sewn on a coarser canvas: each of their stitches becomes a little block.
                    int n = SideOf(design.pixels);
                    for (int py = 0; py < Size; py++)
                    for (int px = 0; px < Size; px++)
                    {
                        int k = (py * n / Size) * n + px * n / Size;
                        _pixels[py * Size + px] = k < design.pixels.Length ? design.pixels[k] : '.';
                    }
                    Redraw();
                    Patterns_();
                    Silhouette();
                    Status("Changing " + design.name + ".");
                });
            }
        }

        /// <summary>A sewn piece as a finished picture: fabric where unpainted, seamed, pixels crisp.</summary>
        static readonly Dictionary<string, Sprite> _seams = new();

        /// <summary>Just the seams of a pattern (outline and the lines between its parts), to lie over the paint.</summary>
        static Sprite SeamSprite(string kind, int res = 256)
        {
            if (_seams.TryGetValue(kind, out var s) && s != null) return s;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color32[res * res];
            Color32 seam = UiKit.Hex("8A6A52");
            for (int j = 0; j < res; j++)
            for (int i = 0; i < res; i++)
            {
                float u = (i + 0.5f) / res * 2f - 1f, v = (j + 0.5f) / res * 2f - 1f;
                float d = Shape(kind, u, v);
                if (d > 0.02f) continue;
                Region(kind, u, v, out float edge);
                if (d > -0.025f || edge > -0.025f) px[j * res + i] = seam;
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return _seams[kind] = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>How many stitches across a saved piece is (older ones are 16, newer 32).</summary>
        static int SideOf(string pixels)
        {
            if (string.IsNullOrEmpty(pixels)) return Size;
            int n = Mathf.RoundToInt(Mathf.Sqrt(pixels.Length));
            return n * n == pixels.Length ? n : 16;
        }

        public static Sprite DesignSprite(string pixels, string kind = "", int res = 128)
        {
            int side = SideOf(pixels);
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
                Region(kind, u, v, out float edge);   // the seams between the parts, too
                if (d > -0.03f || edge > -0.03f) { px[j * res + i] = seam; continue; }
                int x = Mathf.Clamp(i * side / res, 0, side - 1), y = Mathf.Clamp(j * side / res, 0, side - 1);
                char c = pixels != null && y * side + x < pixels.Length ? pixels[y * side + x] : '.';
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
            public void OnPointerDown(PointerEventData e) { if (!Owner.HandTool) Paint(e, true); }
            public void OnDrag(PointerEventData e)
            {
                if (Owner.HandTool) { if (Input.touchCount < 2) Owner.Drag(e.delta); }
                else Paint(e, false);
            }

            void Paint(PointerEventData e, bool first)
            {
                var r = (RectTransform)transform;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(r, e.position, e.pressEventCamera, out var local)) return;
                var size = r.rect.size;
                Owner.Touch(new Vector2(local.x / size.x + 0.5f, local.y / size.y + 0.5f), first);
            }
        }
    }
}
