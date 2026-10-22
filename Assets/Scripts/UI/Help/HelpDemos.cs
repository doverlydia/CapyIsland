using System;
using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>Which little movie a help page plays.</summary>
    public enum HelpDemo
    {
        Walk, Zoom, Arrows, Worker, Feed, Wood, Forest, Gold, Gems, Sell, FruitTree, Bubble, Place,
        Buy, Shops, Pests, Map, Bridge, Goals, Hearts, Gifts, Wardrobe, Hobby, Picture,
        Account, Fishing, Bank, Burrow, Contest,
        HobbyHub, Reading, Kinds, Bugs, Birds, Garden, Cooking, Cats, Sewing, Ema, Visit, FriendCode, Together, SaveCode,
    }

    /// <summary>
    /// The movies themselves: each one acts out a help page with the game's own pictures and the
    /// tutorial's pointing hand, the way a friend would show you on your phone. They act out what
    /// the game really does (a worker is dragged onto a station, a garden goes near the mill, the
    /// map is travelled by dragging your own pin), so a movie never teaches a move that doesn't work.
    ///
    /// Positions are on the 600 × 400 stage, (0, 0) in the middle. Pictures stand on their feet.
    /// </summary>
    public static partial class HelpDemos
    {
        /// <summary>Builds <paramref name="kind"/> in <paramref name="frame"/> and starts it playing.</summary>
        public static HelpMovie Play(HelpDemo kind, RectTransform frame, string picture = null, string extra = null)
        {
            var m = HelpMovie.Create(frame);
            try
            {
                switch (kind)
                {
                    case HelpDemo.Walk: Walk(m); break;
                    case HelpDemo.Zoom: Zoom(m); break;
                    case HelpDemo.Arrows: Arrows(m); break;
                    case HelpDemo.Worker: Worker(m); break;
                    case HelpDemo.Feed: Feed(m); break;
                    case HelpDemo.Wood: Grove(m, gold: false, spreading: false); break;
                    case HelpDemo.Gold: Grove(m, gold: true, spreading: false); break;
                    case HelpDemo.Forest: Grove(m, gold: false, spreading: true); break;
                    case HelpDemo.Bank: Bank(m); break;
                    case HelpDemo.Gems: GemsMovie(m); break;
                    case HelpDemo.Sell: Sell(m); break;
                    case HelpDemo.FruitTree: FruitTree(m); break;
                    case HelpDemo.Bubble: Bubble(m); break;
                    case HelpDemo.Place: Place(m); break;
                    case HelpDemo.Buy: Buy(m); break;
                    case HelpDemo.Shops: ShopsMovie(m); break;
                    case HelpDemo.Pests: PestsMovie(m); break;
                    case HelpDemo.Map: MapMovie(m); break;
                    case HelpDemo.Bridge: BridgeMovie(m); break;
                    case HelpDemo.Goals: Goals(m); break;
                    case HelpDemo.Hearts: Hearts(m); break;
                    case HelpDemo.Gifts: Gifts(m); break;
                    case HelpDemo.Wardrobe: DressUp(m); break;
                    case HelpDemo.Burrow: Burrow(m); break;
                    case HelpDemo.Contest: Contest(m); break;
                    case HelpDemo.HobbyHub: HobbyHubMovie(m); break;
                    case HelpDemo.Reading: ReadingMovie(m); break;
                    case HelpDemo.Kinds: KindsMovie(m); break;
                    case HelpDemo.Bugs: BugsMovie(m); break;
                    case HelpDemo.Birds: BirdsMovie(m); break;
                    case HelpDemo.Garden: GardenMovie(m); break;
                    case HelpDemo.Cooking: CookingMovie(m); break;
                    case HelpDemo.Cats: CatsMovie(m); break;
                    case HelpDemo.Sewing: SewingMovie(m); break;
                    case HelpDemo.Ema: EmaMovie(m); break;
                    case HelpDemo.Visit: VisitMovie(m); break;
                    case HelpDemo.FriendCode: FriendCodeMovie(m); break;
                    case HelpDemo.Together: TogetherMovie(m); break;
                    case HelpDemo.SaveCode: SaveCodeMovie(m); break;
                    case HelpDemo.Hobby: Hobby(m, picture, extra); break;
                    case HelpDemo.Picture: Hobby(m, picture, null); break;
                    case HelpDemo.Fishing: GoFishing(m, extra); break;
                    case HelpDemo.Account: AccountMovie(m); break;
                }
            }
            catch (Exception e)
            {
                // A movie is a nicety: a missing picture must never stop the page from opening.
                Debug.LogWarning("[Help] The " + kind + " movie couldn't be built: " + e.Message);
            }
            m.Step();
            return m;
        }

        // ---------------------------------------------------------------- shared bits

        /// <summary>A walking figure: a holder to move, and the picture in it to turn and bob.</summary>
        static (RectTransform holder, Image body) Walker(HelpMovie m, string name, Sprite sprite, Vector2 at, float size, Transform parent = null)
        {
            var holder = m.Group(name, at, parent);
            var body = m.Pic("Body", sprite, Vector2.zero, size, holder);
            return (holder, body);
        }

        /// <summary>A bubble with a picture inside, as the island draws them.</summary>
        static RectTransform BubbleWith(HelpMovie m, Sprite inside, Vector2 at, float size, Transform parent = null)
        {
            var g = m.Group("Bubble", at, parent);
            m.Shape("Glass", UiKit.Circle, Vector2.zero, new Vector2(size, size), new Color(0.85f, 0.97f, 1f, 0.45f), g);
            var pic = m.Pic("Inside", inside, new Vector2(0f, -size * 0.32f), size * 0.62f, g);
            pic.rectTransform.pivot = new Vector2(0.5f, 0.1f);
            m.Shape("Rim", IconPainter.Ring, Vector2.zero, new Vector2(size, size), new Color(1f, 1f, 1f, 0.85f), g);
            m.Shape("Shine", UiKit.Circle, new Vector2(-size * 0.22f, size * 0.22f), new Vector2(size * 0.16f, size * 0.16f), new Color(1f, 1f, 1f, 0.8f), g);
            return g;
        }

        /// <summary>The ellipse that shows under a thing being placed, green for room or red for blocked.</summary>
        static Image PlaceRing(HelpMovie m, Transform parent, bool ok, float width = 120f)
        {
            var ring = m.Shape(ok ? "Room" : "Blocked", UiKit.Circle, new Vector2(0f, 6f), new Vector2(width, width * 0.5f),
                ok ? new Color(0.36f, 0.82f, 0.36f, 0.45f) : new Color(0.9f, 0.3f, 0.25f, 0.45f), parent);
            ring.transform.SetAsFirstSibling();
            return ring;
        }

        /// <summary>A small panel with a coloured top, standing in for a screen or a dialog.</summary>
        static RectTransform Panel(HelpMovie m, Vector2 at, Vector2 size, Color? header, float showFrom, float showTo)
        {
            var p = m.Card("Panel", at, size, header);
            m.Size(p).At(showFrom, 0.85f).At(showFrom + 0.25f, 1f, DemoEase.Back);
            m.Fade(p).At(showFrom - 0.01f, 0f).At(showFrom, 1f, DemoEase.Step).At(showTo, 1f).At(showTo + 0.2f, 0f);
            return p;
        }

        /// <summary>Grey lines standing in for words, so the movies need no translating.</summary>
        static void Lines(HelpMovie m, Transform parent, Vector2 at, float width, int count, float gap = 26f)
        {
            for (int i = 0; i < count; i++)
            {
                var line = m.Shape("Line", UiKit.Rounded(8), at + new Vector2(0f, -i * gap), new Vector2(i == count - 1 ? width * 0.6f : width, 14f), new Color(0f, 0f, 0f, 0.12f), parent);
                line.type = Image.Type.Sliced;
            }
        }

        /// <summary>A pretend button with an icon on it (a tick for "yes", a cross for "no").</summary>
        static RectTransform Button(HelpMovie m, Transform parent, Vector2 at, Vector2 size, Color fill, Sprite icon)
        {
            var face = m.Shape("Button", UiKit.Rounded(18), at, size, fill, parent);
            face.type = Image.Type.Sliced;
            var ic = m.Shape("Icon", icon, Vector2.zero, new Vector2(size.y * 0.55f, size.y * 0.55f), Color.white, face.transform);
            ic.preserveAspect = true;
            return face.rectTransform;
        }

        static RectTransform Cost(HelpMovie m, Transform parent, Vector2 at, ResourceKind kind, string amount)
        {
            var g = m.Group("Cost", at, parent);
            m.Pic("Icon", HelpArt.Resource(kind), new Vector2(-30f, -16f), 34f, g);
            m.Label("Amount", amount, new Vector2(16f, 0f), 28f, UiKit.Ink, g);
            return g;
        }

        /// <summary>Where the counters sit across the top, as on the screen: wood, gold, then gems.</summary>
        static readonly Vector2 WoodPill = new(-215f, 168f), GoldPill = new(-55f, 168f), GemPill = new(105f, 168f);

        /// <summary>The icon end of a counter, where a ping lands.</summary>
        static Vector2 PillIcon(Vector2 pill) => pill + new Vector2(-46f, 0f);

        /// <summary>Shrinks a label until <paramref name="english"/> (in the language on screen) fits <paramref name="width"/>.</summary>
        static void Fit(TMP_Text t, string english, float width)
        {
            float w = t.GetPreferredValues(Loc.T(english)).x;
            if (w > width) t.fontSize *= width / w;
        }

        /// <summary>A label that starts at <paramref name="left"/> and runs right, kept within <paramref name="width"/>.</summary>
        static TextMeshProUGUI LeftLabel(HelpMovie m, Transform parent, string text, Vector2 left, float size, Color colour, float width)
        {
            var t = m.Label("Label", text, left, size, colour, parent);
            Fit(t, text, width);
            float w = Mathf.Min(width, t.GetPreferredValues(Loc.T(text)).x);
            t.rectTransform.sizeDelta = new Vector2(w + 4f, t.rectTransform.sizeDelta.y);
            t.rectTransform.anchoredPosition = left + new Vector2(w * 0.5f, 0f);
            return t;
        }

        /// <summary>A dialog button with its word on it ("Purchase", "Cancel").</summary>
        static RectTransform WordButton(HelpMovie m, Transform parent, Vector2 at, Vector2 size, Color fill, string words, Color ink)
        {
            var face = m.Shape("Button", UiKit.Rounded(18), at, size, fill, parent);
            face.type = Image.Type.Sliced;
            var t = m.Label("Label", words, Vector2.zero, size.y * 0.52f, ink, face.transform);
            Fit(t, words, size.x - 18f);
            return face.rectTransform;
        }

        /// <summary>
        /// Something a shop has set out on the ground in front of it: just its picture, bobbing
        /// gently so it reads as "for sale" rather than planted.
        /// </summary>
        static Image Ware(HelpMovie m, Sprite art, Vector2 at, float size, float phase, Transform parent = null)
        {
            var g = m.Group("Ware", at, parent);
            var pic = m.Pic("Pic", art, Vector2.zero, size, g);
            m.Do(s => g.anchoredPosition = at + new Vector2(0f, 3f + Mathf.Sin(s * 2.4f + phase) * 3f));
            return pic;
        }

        /// <summary>
        /// The buy panel as it opens when you tap a ware: the name at the top, the item, its three
        /// prices down the right (wood, gold, gems), and Cancel and Purchase. Returns where Purchase is.
        /// </summary>
        static Vector2 BuyCard(HelpMovie m, Vector2 at, string id, Sprite art, float from, float to)
        {
            var card = Panel(m, at, new Vector2(330f, 210f), null, from, to);
            string name = StoryText.Name(id);
            Fit(m.Label("Title", name, new Vector2(0f, 78f), 26f, UiKit.Ink, card), name, 300f);
            m.Pic("Item", art, new Vector2(-88f, -46f), 104f, card);
            var price = StoreData.Price(id);
            int[] amounts = { price.x, price.y, price.z };
            ResourceKind[] kinds = { ResourceKind.Wood, ResourceKind.Gold, ResourceKind.Gem };
            for (int i = 0; i < 3; i++)
            {
                float y = 40f - i * 36f;
                m.Label("Price", amounts[i].ToString(), new Vector2(86f, y), 24f, UiKit.Ink, card);
                m.Shape("Icon", HelpArt.Resource(kinds[i]), new Vector2(128f, y), new Vector2(30f, 30f), Color.white, card).preserveAspect = true;
            }
            var buy = new Vector2(78f, -76f);
            WordButton(m, card, new Vector2(-78f, -76f), new Vector2(140f, 42f), UiKit.SurfaceSunk, "Cancel", UiKit.Ink);
            WordButton(m, card, buy, new Vector2(140f, 42f), UiKit.Primary, "Purchase", Color.white);
            return at + buy;
        }

        /// <summary>
        /// A capybara as the island draws a worker: <see cref="Nap"/> curled up asleep, <see cref="Body"/>
        /// up and about (the holder moves, the body faces and bobs), the tool gripped low at the front
        /// paw (the pictures face left) and swung from there, and what it carries home.
        /// </summary>
        sealed class Crew
        {
            public RectTransform Holder, Paw;
            public Image Nap, Body, Tool, Load;
        }

        static Crew Capy(HelpMovie m, Vector2 at, float size, Sprite tool, Sprite load)
        {
            var c = new Crew { Holder = m.Group("Worker", at) };
            var asleep = HelpArt.Asleep;
            c.Nap = m.Pic("Nap", asleep != null ? asleep : HelpArt.Worker, Vector2.zero, size, c.Holder);
            if (asleep == null) c.Nap.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 80f);   // no nap picture: lying on its side
            c.Body = m.Pic("Body", HelpArt.Worker, Vector2.zero, size, c.Holder);
            // Children of the body are placed from its middle, which is 0.4 of its height above its feet.
            c.Paw = m.Group("Paw", new Vector2(-size * 0.27f, -size * 0.12f), c.Body.transform);
            if (tool != null)
            {
                // The tool pictures lean head up-left with the end of the handle at the bottom right: held there.
                c.Tool = m.Shape("Tool", tool, Vector2.zero, new Vector2(size * 0.62f, size * 0.62f), Color.white, c.Paw);
                c.Tool.preserveAspect = true;
                c.Tool.rectTransform.pivot = new Vector2(0.72f, 0.2f);
            }
            if (load != null)
            {
                c.Load = m.Shape("Load", load, new Vector2(-size * 0.2f, size * 0.05f), new Vector2(size * 0.5f, size * 0.5f), Color.white, c.Body.transform);
                c.Load.preserveAspect = true;
            }
            return c;
        }

        /// <summary>Shown only during these stretches of the loop.</summary>
        static void During(HelpMovie m, Component c, params (float from, float to)[] spans)
        {
            if (c == null) return;
            var go = c.gameObject;
            m.Do(s =>
            {
                bool on = false;
                foreach (var (from, to) in spans) on |= s >= from && s < to;
                if (go.activeSelf != on) go.SetActive(on);
            });
        }

        /// <summary>
        /// Swings landing on each of <paramref name="hits"/> (at least 0.45 s apart): a slow wind-up
        /// and a hard strike, leaning into it, as WorkerVisuals plays the harvest frames. With no tool
        /// it's just the lean: a capybara nibbling.
        /// </summary>
        static void Swings(HelpMovie m, Crew c, float[] facingLeft, float[] facingRight = null)
        {
            var paw = m.Turn(c.Paw).At(0f, 0f);
            var lean = m.Turn(c.Body).At(0f, 0f);
            void Swing(float h, float into)
            {
                paw.At(h - 0.32f, 0f).At(h - 0.1f, -40f).At(h, 55f, DemoEase.In).At(h + 0.12f, 0f);
                lean.At(h - 0.1f, 0f).At(h, into, DemoEase.In).At(h + 0.16f, 0f);
            }
            foreach (var h in facingLeft) Swing(h, 9f);
            if (facingRight != null) foreach (var h in facingRight) Swing(h, -9f);
        }

        /// <summary>A planted thing rocking as it's struck.</summary>
        static void Rock(HelpMovie m, Component node, params float[] hits)
        {
            var turn = m.Turn(node).At(0f, 0f);
            foreach (var h in hits) turn.At(h, 0f).At(h + 0.06f, -6f).At(h + 0.2f, 0f);
        }

        /// <summary>
        /// The hungry dream: a pale thought bubble with a garden in it and two little dots trailing
        /// down to the sleeper's head (WorldBubble.Thought).
        /// </summary>
        static RectTransform Dream(HelpMovie m, Vector2 at, float size, Sprite inside)
        {
            var g = m.Group("Dream", at);
            var dot = new Color(1f, 1f, 1f, 0.7f);
            m.Shape("Dot", UiKit.Circle, new Vector2(-0.42f, -0.98f) * size, Vector2.one * size * 0.15f, dot, g);
            m.Shape("Dot", UiKit.Circle, new Vector2(-0.28f, -0.68f) * size, Vector2.one * size * 0.26f, dot, g);
            m.Shape("Disc", UiKit.Circle, Vector2.zero, Vector2.one * size, new Color(1f, 1f, 1f, 0.78f), g);
            var pic = m.Shape("Icon", inside, Vector2.zero, Vector2.one * size * 0.66f, new Color(1f, 1f, 1f, 0.9f), g);
            pic.preserveAspect = true;
            return g;
        }

        /// <summary>
        /// A grove going into the ground where it was dropped: the big one (the parent) grows in
        /// from small, and its first little ones sprout round it at once (Harvestable.OnPlacedInWorld).
        /// Drawn back to front. Returns the little ones (and their size keys, to take bites out of),
        /// in the order of <paramref name="spots"/>.
        /// </summary>
        static (Image pic, DemoKeys size)[] Sprout(HelpMovie m, Sprite planted, Vector2 at, float size, Vector2[] spots,
            float[] scales, float when, float grow, float gone)
        {
            var kids = new (Image, DemoKeys)[spots.Length];
            var order = new List<int>();
            for (int i = -1; i < spots.Length; i++) order.Add(i);
            order.Sort((a, b) => (b < 0 ? at.y : spots[b].y).CompareTo(a < 0 ? at.y : spots[a].y));
            foreach (int i in order)
            {
                float born = i < 0 ? when : when + 0.06f + i * 0.07f;
                float full = i < 0 ? 1f : scales[i];
                var pic = m.Pic(i < 0 ? "Parent" : "Child", planted, i < 0 ? at : spots[i], size);
                var keys = m.Size(pic.transform).At(born - 0.01f, 0f).At(born, 0.2f * full, DemoEase.Step)
                    .At(born + 0.3f, 0.5f * full, DemoEase.Back).At(born + grow, full);
                m.Fade(pic).At(gone, 1f).At(gone + 0.3f, 0f);
                if (i >= 0) kids[i] = (pic, keys);
            }
            return kids;
        }

        /// <summary>A planted thing taken down a share at a time, one bite per hit, until it's gone.</summary>
        static void Bitten(DemoKeys size, float full, params float[] hits)
        {
            float left = full;
            for (int i = 0; i < hits.Length; i++)
            {
                size.At(hits[i] - 0.01f, left);
                left = full * (1f - (i + 1f) / hits.Length);
                size.At(hits[i] + 0.08f, left, DemoEase.Out);
            }
        }

        // ---------------------------------------------------------------- getting around

        static void Walk(HelpMovie m)
        {
            m.Length = 5.2f;
            m.Island("Island", new Vector2(0f, -20f), new Vector2(540f, 300f));
            m.Pic("Tree", HelpArt.Item("Medium Pine Forest"), new Vector2(-40f, 40f), 90f);
            var (you, body) = Walker(m, "You", HelpArt.You, new Vector2(-160f, -60f), 110f);
            m.Move(you).At(1.2f, new Vector2(-160f, -60f)).At(2.6f, new Vector2(130f, -40f), DemoEase.Linear)
                       .At(3.6f, new Vector2(130f, -40f)).At(4.8f, new Vector2(-160f, -60f), DemoEase.Linear);
            m.Face(body).At(0f, 1f).At(1.2f, -1f, DemoEase.Step).At(3.6f, 1f, DemoEase.Step);
            m.Bob(body, 7f, 12f, s => (s > 1.2f && s < 2.6f) || (s > 3.6f && s < 4.8f));
            m.Hand().Show(0.4f, 3.9f)
                .At(0.4f, new Vector2(230f, 60f)).At(0.9f, new Vector2(130f, -40f)).Tap(1.0f)
                .At(2.9f, new Vector2(130f, -40f)).At(3.3f, new Vector2(-160f, -60f)).Tap(3.45f)
                .At(3.9f, new Vector2(-230f, 60f));
        }

        static void Zoom(HelpMovie m)
        {
            m.Length = 5.6f;
            var world = m.Group("World", new Vector2(0f, -10f));
            m.Island("Island", Vector2.zero, new Vector2(430f, 250f), world);
            m.Pic("Forest", HelpArt.Item("Medium Pine Forest"), new Vector2(-120f, -20f), 110f, world);
            m.Pic("Mill", HelpArt.Item("Sawmill"), new Vector2(30f, -40f), 100f, world);
            m.Pic("House", HelpArt.Item("Capy House"), new Vector2(130f, 0f), 90f, world);
            m.Pic("You", HelpArt.You, new Vector2(-30f, -80f), 56f, world);
            m.Size(world).At(0.6f, 1f).At(2.0f, 1.65f).At(3.0f, 1.65f).At(4.4f, 1f);

            // Two fingertips: apart to come closer, together to see more.
            var a = m.Dot().Show(0.35f, 4.6f);
            var b = m.Dot().Show(0.35f, 4.6f);
            a.At(0.6f, new Vector2(-40f, -30f)).At(2.0f, new Vector2(-180f, -120f)).At(3.0f, new Vector2(-180f, -120f)).At(4.4f, new Vector2(-40f, -30f));
            b.At(0.6f, new Vector2(40f, 30f)).At(2.0f, new Vector2(180f, 120f)).At(3.0f, new Vector2(180f, 120f)).At(4.4f, new Vector2(40f, 30f));

            var badge = m.Shape("Badge", UiKit.Circle, new Vector2(240f, 150f), new Vector2(84f, 84f), UiKit.HudGlass);
            m.Shape("Glass", IconPainter.Magnifier, Vector2.zero, new Vector2(54f, 54f), Color.white, badge.transform);
            var plus = m.Label("In", "+", new Vector2(26f, -26f), 44f, UiKit.Sun, badge.transform);
            var minus = m.Label("Out", "−", new Vector2(26f, -26f), 44f, UiKit.Sun, badge.transform);
            m.Between(plus.gameObject, 0.5f, 2.4f);
            m.Between(minus.gameObject, 2.9f, 4.8f);
            m.Fade(badge.rectTransform).At(0.3f, 0f).At(0.5f, 1f).At(4.6f, 1f).At(4.9f, 0f);
        }

        static void Arrows(HelpMovie m)
        {
            m.Length = 6f;
            var world = m.Group("World", Vector2.zero);
            m.Island("Island", new Vector2(220f, -30f), new Vector2(1000f, 300f), world);
            var you = m.Group("YouHolder", new Vector2(-60f, -60f), world);
            var body = m.Pic("You", HelpArt.You, Vector2.zero, 90f, you);
            var bubble = BubbleWith(m, HelpArt.Item("Medium Posie Garden"), new Vector2(480f, 0f), 110f);
            bubble.SetParent(world, false);
            m.Move(world).At(1.4f, Vector2.zero).At(3.6f, new Vector2(-430f, 0f), DemoEase.InOut);
            m.Move(you).At(1.4f, new Vector2(-60f, -60f)).At(3.6f, new Vector2(390f, -60f), DemoEase.InOut);
            m.Face(body).At(0f, 1f).At(1.4f, -1f, DemoEase.Step).At(5.9f, 1f, DemoEase.Step);
            m.Bob(body, 7f, 12f, s => s > 1.4f && s < 3.6f);

            // The arrow at the edge, with a picture of what it points at.
            var arrow = m.Group("EdgeArrow", new Vector2(250f, 10f));
            m.Shape("Disc", UiKit.Circle, Vector2.zero, new Vector2(84f, 84f), UiKit.Spring, arrow);
            m.Shape("Face", UiKit.Circle, Vector2.zero, new Vector2(64f, 64f), Color.white, arrow);
            m.Pic("Icon", HelpArt.Item("Medium Posie Garden"), new Vector2(0f, -24f), 50f, arrow);
            m.Shape("Point", IconPainter.Chevron, new Vector2(56f, 0f), new Vector2(30f, 30f), UiKit.Spring, arrow);
            m.Fade(arrow).At(0.1f, 0f).At(0.4f, 1f).At(1.6f, 1f).At(1.9f, 0f);
            m.Size(arrow).At(0.4f, 1f).At(0.7f, 1.08f).At(1.0f, 1f);

            m.Hand().Show(0.5f, 1.6f).At(0.5f, new Vector2(150f, 120f)).At(1.0f, new Vector2(250f, 10f)).Tap(1.15f).At(1.6f, new Vector2(150f, 120f));
            m.Hand().Show(4.0f, 4.9f).At(4.0f, new Vector2(150f, 120f)).At(4.4f, new Vector2(50f, 10f)).Tap(4.55f);
            m.Fade(bubble).At(4.55f, 1f).At(4.75f, 0f).At(5.8f, 0f).At(5.95f, 1f);
            m.Sparkle(new Vector2(50f, 10f), 4.6f, UiKit.Spring);
        }

        // ---------------------------------------------------------------- workers

        static void Worker(HelpMovie m)
        {
            m.Length = 7f;
            m.Island("Island", new Vector2(0f, -20f), new Vector2(560f, 300f));
            var treeArt = HelpArt.Item("Medium Pine Forest");
            var tree = m.Pic("Tree", treeArt, new Vector2(200f, -10f), 100f);
            m.Pic("Tree2", treeArt, new Vector2(250f, -90f), 80f);
            var ring = m.Shape("StationRing", IconPainter.Ring, new Vector2(60f, 0f), new Vector2(230f, 130f), new Color(0.36f, 0.82f, 0.36f, 0.9f));
            m.Pic("Mill", HelpArt.Item("Sawmill"), new Vector2(60f, -40f), 120f);
            var count = m.Label("Count", "0/4", new Vector2(60f, 110f), 34f, Color.white);
            count.outlineWidth = 0.25f;
            m.Fade(ring).At(0.9f, 0f).At(1.1f, 0.9f).At(2.3f, 0.9f).At(2.5f, 0f);
            m.Size(ring).At(1.1f, 1f).At(1.5f, 1.06f).At(1.9f, 1f).At(2.3f, 1.06f);
            m.Fade(count).At(0.9f, 0f).At(1.1f, 1f).At(2.3f, 1f).At(2.5f, 0f);

            var (you, youBody) = Walker(m, "You", HelpArt.You, new Vector2(-190f, -70f), 90f);
            var (worker, body) = Walker(m, "Worker", HelpArt.Worker, new Vector2(-260f, -80f), 66f);
            m.Bob(youBody, 3f, 3f);
            m.Move(worker).At(1.0f, new Vector2(-260f, -80f)).At(2.2f, new Vector2(60f, -50f), DemoEase.Linear)
                .At(2.4f, new Vector2(60f, -60f)).At(2.8f, new Vector2(60f, -60f))
                .At(3.6f, new Vector2(165f, -30f), DemoEase.Linear).At(4.6f, new Vector2(165f, -30f))
                .At(5.4f, new Vector2(70f, -60f), DemoEase.Linear);
            m.Size(worker).At(2.3f, 1f).At(2.45f, 1.25f).At(2.65f, 1f);
            m.Face(body).At(0f, 1f).At(1.0f, -1f, DemoEase.Step).At(4.6f, 1f, DemoEase.Step).At(6.9f, 1f);
            m.Turn(body).At(3.7f, 0f).At(3.85f, 14f).At(4.0f, -6f).At(4.15f, 14f).At(4.3f, -6f).At(4.45f, 14f).At(4.6f, 0f);
            m.Bob(body, 6f, 12f, s => (s > 2.8f && s < 3.6f) || (s > 4.6f && s < 5.4f));
            m.Turn(tree.transform).At(3.85f, 0f).At(3.9f, -5f).At(4.05f, 0f).At(4.15f, -5f).At(4.3f, 0f).At(4.45f, -5f).At(4.6f, 0f);
            m.Fade(worker).At(0f, 0f).At(0.3f, 1f).At(6.6f, 1f).At(6.9f, 0f);
            m.FloatUp(HelpArt.Resource(ResourceKind.Wood), "+3", new Vector2(60f, 80f), 5.5f);

            m.Hand().Show(0.3f, 2.6f)
                .At(0.3f, new Vector2(-120f, 80f)).At(0.85f, new Vector2(-260f, -50f)).Press(1.0f)
                .At(2.2f, new Vector2(60f, -20f), DemoEase.Linear).Lift(2.3f).At(2.6f, new Vector2(0f, 90f));
        }

        /// <summary>
        /// Feeding, as the game plays it: a worker that got hungry with nothing to eat has gone to
        /// sleep beside its sawmill, dreaming of a garden. You drag a garden out of your trail and drop
        /// it near the mill: where it lands it turns into flowers, a big one with a little patch
        /// sprouting round it. The capybara wakes (the dream goes), walks to the nearest little
        /// flower, nibbles it away, and goes back to chopping.
        /// </summary>
        static void Feed(HelpMovie m)
        {
            const float L = 9f;
            m.Length = L;
            m.Island("Island", new Vector2(0f, -25f), new Vector2(570f, 300f));
            m.Pic("Mill", HelpArt.Item("Sawmill"), new Vector2(150f, -5f), 125f);
            var pine = m.Pic("Tree", HelpArt.Item("Medium Pine Forest"), new Vector2(252f, -92f), 86f);

            // The garden goes in: dragged out of the trail, dropped, and it becomes flowers.
            const float grab = 1.0f, drop = 2.3f;
            var trail = new Vector2(-258f, -92f);
            var spot = new Vector2(-15f, -112f);
            var flower = HelpArt.Planted("Medium Posie Garden", "Small Posie Garden");
            var spots = new[]
            {
                spot + new Vector2(-62f, 12f), spot + new Vector2(58f, 16f), spot + new Vector2(-38f, -30f),
                spot + new Vector2(40f, -32f), spot + new Vector2(2f, -48f),
            };
            float[] scales = { 0.62f, 0.68f, 0.58f, 0.64f, 0.6f };
            var kids = Sprout(m, flower, spot, 66f, spots, scales, drop, 0.5f, L - 0.5f);
            m.Sparkle(spot + new Vector2(0f, 24f), drop, UiKit.Leaf, 5);

            // The hungry one: asleep, then up, to the nearest little flower (it stands just right of
            // it, facing it), three bites, and off to the pine to chop.
            var nap = new Vector2(92f, -78f);
            var eat = new Vector2(84f, -104f);
            var work = new Vector2(212f, -100f);
            const float wake = 3.3f, atFood = 3.9f, fed = 5.4f, atTree = 6.3f;
            float[] bites = { 4.3f, 4.75f, 5.2f };
            float[] chops = { 6.75f, 7.2f, 7.65f };
            var c = Capy(m, nap, 64f, HelpArt.Tool(ResourceKind.Wood), null);
            m.Move(c.Holder).At(wake + 0.1f, nap).At(atFood, eat, DemoEase.Linear).At(fed, eat).At(atTree, work, DemoEase.Linear);
            m.Face(c.Body).At(0f, 1f).At(fed, -1f, DemoEase.Step);
            m.Bob(c.Body, 6f, 12f, s => (s > wake + 0.1f && s < atFood) || (s > fed && s < atTree));
            During(m, c.Nap, (0f, wake));
            During(m, c.Body, (wake, L));
            During(m, c.Tool, (fed, L));
            Swings(m, c, bites, chops);
            m.Fade(c.Holder).At(0f, 0f).At(0.3f, 1f).At(L - 0.5f, 1f).At(L - 0.2f, 0f);
            Bitten(kids[1].size, scales[1], bites);
            Rock(m, kids[1].pic, bites);
            Rock(m, pine, chops);

            // Its dream, over its head until it wakes.
            var dreamAt = nap + new Vector2(18f, 128f);
            var dream = Dream(m, dreamAt, 78f, HelpArt.Item("Medium Posie Garden", "Small Posie Garden"));
            m.Move(dream).At(0f, dreamAt).At(1.1f, dreamAt + new Vector2(0f, 6f)).At(2.2f, dreamAt).At(wake, dreamAt + new Vector2(0f, 5f));
            m.Size(dream).At(wake, 1f).At(wake + 0.12f, 1.2f).At(wake + 0.3f, 0.3f);
            m.Fade(dream).At(0f, 0f).At(0.3f, 1f).At(wake, 1f).At(wake + 0.3f, 0f);

            // You, facing right, with the garden in your trail behind you.
            var (_, youBody) = Walker(m, "You", HelpArt.You, new Vector2(-195f, -70f), 86f);
            m.Face(youBody).At(0f, -1f);
            var bed = m.Group("Garden", trail);
            m.Pic("Bed", HelpArt.Item("Medium Posie Garden", "Small Posie Garden"), Vector2.zero, 58f, bed);
            var room = PlaceRing(m, bed, true, 100f);
            m.Fade(room).At(grab, 0f).At(grab + 0.15f, 0.45f).At(drop, 0.45f).At(drop + 0.1f, 0f);
            m.Move(bed).At(grab, trail).At(drop, spot, DemoEase.Linear);
            m.Fade(bed).At(0f, 0f).At(0.3f, 1f).At(drop - 0.01f, 1f).At(drop, 0f, DemoEase.Step);

            m.Hand().Show(0.45f, 2.75f)
                .At(0.45f, trail + new Vector2(110f, 120f)).At(0.9f, trail + new Vector2(0f, 22f)).Press(grab).At(grab, trail + new Vector2(0f, 22f))
                .At(drop, spot + new Vector2(0f, 22f), DemoEase.Linear).Lift(drop + 0.05f).At(2.75f, spot + new Vector2(90f, 110f));
        }

        // ---------------------------------------------------------------- resources

        /// <summary>
        /// More wood (or gold), the whole loop as the game plays it: tap a forest (a hill) where the
        /// General Store sets it out, tap Purchase, and it turns up in your trail, paid for. Drag it
        /// out and drop it near the sawmill (the mine): the big tree goes in and little ones sprout
        /// round it at once. The worker napping by the mill with nothing to chop wakes, takes the
        /// nearest little one first, swing by swing, carries it home, and the "+3" flies up to the
        /// counter at the top. <paramref name="spreading"/>: then the forest keeps planting itself,
        /// one little tree at a time.
        /// </summary>
        static void Grove(HelpMovie m, bool gold, bool spreading)
        {
            const float L = 10.5f;
            m.Length = L;
            var kind = gold ? ResourceKind.Gold : ResourceKind.Wood;
            string node = gold ? "Medium Gray Hill" : "Medium Pine Forest";
            var art = HelpArt.Item(node, gold ? "Small Gray Hill" : "Small Pine Forest");
            var planted = HelpArt.Planted(node, gold ? "Small Gray Hill" : "Small Pine Forest");
            var price = StoreData.Price(node);
            // A little tree or rock pays its resource's price (pine 3, gray rock 2); only the big one pays double.
            int pays = BunniRules.Price(gold ? NodeResource.Bronze : NodeResource.Pine);

            const float tap = 0.95f, cardFrom = 1.15f, buy = 1.85f, bought = 2.0f, grab = 2.95f, drop = 4.0f;
            const float wake = 4.9f, reach = 5.45f, leave = 6.95f, home = 7.65f;
            float[] hits = { 5.85f, 6.3f, 6.75f };

            m.Island("Island", new Vector2(0f, -35f), new Vector2(570f, 270f));

            // The General Store, its wares in a half circle in front of it, east round to west.
            var shop = new Vector2(-165f, -10f);
            m.Pic("Store", HelpArt.Item("General Store"), shop, 125f);
            var names = StoreData.Lists.TryGetValue("general_store", out var list) ? list : new[] { node };
            var buyAt = shop + new Vector2(0f, -68f);
            for (int i = 0; i < names.Length; i++)
            {
                float a = (names.Length > 1 ? i * 180f / (names.Length - 1) : 90f) * Mathf.Deg2Rad;
                var at = shop + new Vector2(Mathf.Cos(a) * 108f, -Mathf.Sin(a) * 68f);
                Ware(m, HelpArt.Item(names[i]), at, 44f, i * 1.3f);
                if (names[i] == node) buyAt = at;
            }

            var mill = new Vector2(195f, -15f);
            m.Pic("Workplace", HelpArt.Item(gold ? "Mine" : "Sawmill"), mill, 125f);

            // Planted near the mill: the big one and its first little ones.
            var spot = new Vector2(70f, -105f);
            var spots = new[]
            {
                spot + new Vector2(-55f, 14f), spot + new Vector2(52f, 20f), spot + new Vector2(-34f, -30f),
                spot + new Vector2(40f, -36f), spot + new Vector2(2f, -52f),
            };
            float[] scales = gold ? new[] { 0.55f, 0.62f, 0.5f, 0.58f, 0.52f } : new[] { 0.62f, 0.68f, 0.58f, 0.64f, 0.6f };
            float size = gold ? 92f : 100f;
            var kids = Sprout(m, planted, spot, size, spots, scales, drop, 1.2f, L - 0.5f);
            m.Sparkle(spot + new Vector2(0f, 30f), drop, gold ? UiKit.Sun : UiKit.Leaf, 5);

            // The worker: asleep with nothing to do, then to the nearest little one, three swings, home.
            var nap = new Vector2(192f, -84f);
            var work = new Vector2(150f, -94f);
            var door = new Vector2(178f, -42f);
            var c = Capy(m, nap, 62f, HelpArt.Tool(kind), gold ? null : HelpArt.Log);
            m.Move(c.Holder).At(wake + 0.1f, nap).At(reach, work, DemoEase.Linear).At(leave, work).At(home, door, DemoEase.Linear);
            m.Face(c.Body).At(0f, 1f).At(leave, -1f, DemoEase.Step);
            m.Bob(c.Body, 6f, 12f, s => (s > wake + 0.1f && s < reach) || (s > leave && s < home));
            During(m, c.Nap, (0f, wake));
            During(m, c.Body, (wake, L));
            During(m, c.Tool, (wake, leave));
            During(m, c.Load, (leave, home + 0.05f));
            Swings(m, c, hits);
            m.Fade(c.Holder).At(0f, 0f).At(0.3f, 1f).At(L - 0.5f, 1f).At(L - 0.2f, 0f);
            Bitten(kids[1].size, scales[1], hits);
            Rock(m, kids[1].pic, hits);
            m.Sparkle(spots[1] + new Vector2(0f, 20f), hits[hits.Length - 1] + 0.05f, gold ? Color.white : UiKit.Leaf, 4);

            // A forest left alone keeps planting little trees round itself, as long as there's room.
            if (spreading)
            {
                var more = new[] { new Vector2(150f, -150f), new Vector2(-12f, -140f), new Vector2(-28f, -78f) };
                for (int i = 0; i < more.Length; i++)
                {
                    float t = 7.9f + i * 0.6f;
                    var seedling = m.Pic("Seedling", planted, more[i], size);
                    m.Size(seedling.transform).At(t - 0.01f, 0f).At(t, 0.12f, DemoEase.Step)
                        .At(t + 0.3f, 0.3f, DemoEase.Back).At(t + 1.4f, 0.56f);
                    m.Fade(seedling).At(L - 0.5f, 1f).At(L - 0.2f, 0f);
                    m.Sparkle(more[i] + new Vector2(0f, 18f), t, UiKit.Leaf, 4);
                }
            }

            // You, facing right, and what you bought popping into your trail behind you.
            var (_, youBody) = Walker(m, "You", HelpArt.You, new Vector2(-75f, -150f), 74f);
            m.Face(youBody).At(0f, -1f);
            var trail = new Vector2(-135f, -162f);
            var item = m.Group("Bought", trail);
            var itemPic = m.Pic("Pic", art, Vector2.zero, 54f, item);
            var room = PlaceRing(m, item, true, 110f);
            m.Fade(room).At(grab, 0f).At(grab + 0.15f, 0.45f).At(drop, 0.45f).At(drop + 0.1f, 0f);
            m.Move(item).At(grab, trail).At(drop, spot, DemoEase.Linear);
            m.Size(itemPic.transform).At(bought, 0f).At(bought + 0.3f, 1f, DemoEase.Back);
            m.Fade(item).At(bought - 0.01f, 0f).At(bought, 1f, DemoEase.Step).At(drop - 0.01f, 1f).At(drop, 0f, DemoEase.Step);
            m.Sparkle(trail + new Vector2(0f, 24f), bought, UiKit.Sun, 5);

            // The counters: the price comes off as you buy, and what the worker brings in goes on.
            var woodCount = m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "", WoodPill);
            var goldCount = m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "", GoldPill);
            var purchase = BuyCard(m, new Vector2(40f, 28f), node, art, cardFrom, bought);
            var counter = gold ? GoldPill : WoodPill;
            float land = m.Ping(HelpArt.Resource(kind), "+" + pays, mill + new Vector2(0f, 95f), PillIcon(counter), home + 0.05f);
            const int wood0 = 120, gold0 = 60;
            int wood1 = wood0 - price.x, gold1 = gold0 - price.y;
            var woodSteps = new List<(float, int)>();
            var goldSteps = new List<(float, int)>();
            if (price.x != 0) woodSteps.Add((bought, wood1));
            if (price.y != 0) goldSteps.Add((bought, gold1));
            if (gold) goldSteps.Add((land, gold1 + pays));
            else woodSteps.Add((land, wood1 + pays));
            m.Tally(woodCount, wood0, woodSteps.ToArray());
            m.Tally(goldCount, gold0, goldSteps.ToArray());
            m.Sparkle(PillIcon(counter), land, UiKit.Sun, 5);

            m.Hand().Show(0.35f, 2.2f)
                .At(0.35f, buyAt + new Vector2(130f, 110f)).At(0.8f, buyAt + new Vector2(0f, 20f)).Tap(tap)
                .At(1.65f, purchase).Tap(buy).At(2.2f, purchase + new Vector2(70f, -60f));
            m.Hand().Show(2.75f, 4.4f)
                .At(2.75f, trail + new Vector2(90f, 100f)).At(2.9f, trail + new Vector2(0f, 20f)).Press(grab).At(grab, trail + new Vector2(0f, 20f))
                .At(drop, spot + new Vector2(0f, 20f), DemoEase.Linear).Lift(drop + 0.05f).At(4.4f, spot + new Vector2(110f, 120f));
        }

        /// <summary>
        /// The Bank as you meet it: the building with its two trades set out on the ground, wood to
        /// the west and gold to the east. Tap the wood and the Bank speaks, with its trades in a row
        /// (BankTrade's four tiers and Cancel; the ones your gems can't cover yet are greyed). Tap
        /// "5 â†’ 250": the gems come off at once, and a chest drops in front of the trade, opens by
        /// itself, and the wood flies up to its counter.
        /// </summary>
        static void Bank(HelpMovie m)
        {
            const float L = 8f;
            m.Length = L;
            const int gems0 = 12, wood0 = 120;
            // BankTrade's tiers: gems for wood (or gold).
            (int gems, int amount)[] tiers = { (5, 250), (10, 500), (20, 1250), (30, 2000) };
            const float tap = 0.95f, cardFrom = 1.15f, choose = 2.25f, spent = 2.4f, opens = 3.2f, pays = 3.9f;

            m.Island("Island", new Vector2(0f, -45f), new Vector2(570f, 250f));
            var bank = new Vector2(0f, -30f);
            m.Pic("Bank", HelpArt.Item("Bank"), bank, 150f);
            var woodArt = HelpArt.Item("Buy Wood");
            if (woodArt == null) woodArt = HelpArt.Resource(ResourceKind.Wood);
            var goldArt = HelpArt.Item("Buy Gold");
            if (goldArt == null) goldArt = HelpArt.Resource(ResourceKind.Gold);
            var woodWare = bank + new Vector2(-128f, 0f);
            Ware(m, woodArt, woodWare, 54f, 0f);
            Ware(m, goldArt, bank + new Vector2(128f, 0f), 54f, 1.7f);
            var (_, youBody) = Walker(m, "You", HelpArt.You, new Vector2(-222f, -128f), 80f);
            m.Face(youBody).At(0f, -1f);

            // The chest lands in front of the trade, settles, opens by itself, pays, and fades.
            var chestAt = woodWare + new Vector2(0f, -72f);
            var shut = m.Pic("Chest", HelpArt.ChestShut, chestAt, 66f);
            var open = m.Pic("ChestOpen", HelpArt.ChestOpen, chestAt, 66f);
            m.Fade(shut).At(spent, 0f).At(spent + 0.01f, 1f, DemoEase.Step).At(opens - 0.01f, 1f).At(opens, 0f, DemoEase.Step);
            m.Size(shut.transform).At(spent, 0.3f).At(spent + 0.3f, 1f, DemoEase.Back);
            m.Fade(open).At(opens - 0.01f, 0f).At(opens, 1f, DemoEase.Step).At(pays + 0.67f, 1f).At(pays + 1.35f, 0f);

            // The counters.
            var woodCount = m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "", WoodPill);
            m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "60", GoldPill);
            var gemCount = m.Pill("Gems", HelpArt.Resource(ResourceKind.Gem), "", GemPill);

            // The Bank speaking, at the bottom of the screen as every speech is: its picture in a
            // round frame, its name, the line, and the trades in a row.
            var card = Panel(m, new Vector2(0f, -95f), new Vector2(584f, 202f), null, cardFrom, choose + 0.1f);
            m.Shape("Frame", UiKit.Circle, new Vector2(-232f, 38f), new Vector2(96f, 96f), UiKit.SurfaceSunk, card);
            m.Shape("Face", woodArt, new Vector2(-232f, 38f), new Vector2(68f, 68f), Color.white, card).preserveAspect = true;
            LeftLabel(m, card, "Bank", new Vector2(-170f, 70f), 28f, UiKit.PrimaryDark, 380f);
            LeftLabel(m, card, "Trade your gems for wood. You have " + gems0 + " gems.", new Vector2(-170f, 32f), 20f, UiKit.Ink, 400f);
            var first = Vector2.zero;
            for (int i = 0; i <= tiers.Length; i++)
            {
                var at = new Vector2(-224f + i * 112f, -52f);
                if (i == tiers.Length)
                {
                    WordButton(m, card, at, new Vector2(104f, 68f), UiKit.SurfaceSunk, "Cancel", UiKit.Ink);
                    break;
                }
                var face = TradeButton(m, card, at, tiers[i].gems, tiers[i].amount, ResourceKind.Wood, i == 0);
                if (tiers[i].gems > gems0) face.gameObject.AddComponent<CanvasGroup>().alpha = 0.45f;
                if (i == 0) first = new Vector2(0f, -95f) + at;
            }

            m.Tally(gemCount, gems0, (spent, gems0 - tiers[0].gems));
            float land = m.Ping(HelpArt.Resource(ResourceKind.Wood), "+" + tiers[0].amount, chestAt + new Vector2(0f, 62f), PillIcon(WoodPill), pays);
            m.Tally(woodCount, wood0, (land, wood0 + tiers[0].amount));
            m.Sparkle(PillIcon(WoodPill), land, UiKit.Sun, 5);

            m.Hand().Show(0.35f, 2.6f)
                .At(0.35f, woodWare + new Vector2(120f, 100f)).At(0.8f, woodWare + new Vector2(0f, 26f)).Tap(tap)
                .At(2.0f, first + new Vector2(0f, 4f)).Tap(choose).At(2.6f, first + new Vector2(90f, 40f));
        }

        /// <summary>One of the Bank's trades as a button: the gems it costs over what it gives.</summary>
        static RectTransform TradeButton(HelpMovie m, Transform parent, Vector2 at, int gems, int amount, ResourceKind kind, bool first)
        {
            var ink = first ? Color.white : UiKit.Ink;
            var face = m.Shape("Choice", UiKit.Rounded(18), at, new Vector2(104f, 68f), first ? UiKit.Primary : UiKit.SurfaceSunk, parent);
            face.type = Image.Type.Sliced;
            m.Shape("Gem", HelpArt.Resource(ResourceKind.Gem), new Vector2(-14f, 15f), new Vector2(24f, 24f), Color.white, face.transform).preserveAspect = true;
            m.Label("Pay", gems.ToString(), new Vector2(12f, 15f), 21f, ink, face.transform);
            m.Shape("For", IconPainter.Chevron, new Vector2(-40f, -14f), new Vector2(14f, 14f), ink, face.transform).preserveAspect = true;
            m.Shape("Icon", HelpArt.Resource(kind), new Vector2(-19f, -14f), new Vector2(24f, 24f), Color.white, face.transform).preserveAspect = true;
            m.Label("Get", amount.ToString(), new Vector2(20f, -14f), 21f, ink, face.transform);
            return face.rectTransform;
        }

        static void Sell(HelpMovie m)
        {
            m.Length = 7.5f;
            m.Island("Island", new Vector2(0f, -20f), new Vector2(560f, 300f));
            var kombu = HelpArt.Item("Pirate Pete");
            if (kombu == null) kombu = HelpArt.Picture("Sailor");
            m.Pic("Kombu", kombu, new Vector2(150f, -40f), 150f);
            m.Pic("You", HelpArt.You, new Vector2(-170f, -70f), 90f);

            var item = m.Group("Item", new Vector2(-240f, -85f));
            m.Pic("Forest", HelpArt.Item("Medium Maple Forest", "Medium Pine Forest"), Vector2.zero, 64f, item);
            m.Move(item).At(1.0f, new Vector2(-240f, -85f)).At(2.3f, new Vector2(110f, -80f), DemoEase.Linear);
            m.Fade(item).At(0f, 0f).At(0.3f, 1f).At(3.8f, 1f).At(4.0f, 0f);

            var offer = Panel(m, new Vector2(70f, 120f), new Vector2(280f, 150f), UiKit.Primary, 2.5f, 3.9f);
            Cost(m, offer, new Vector2(-60f, 18f), ResourceKind.Wood, "4");
            Cost(m, offer, new Vector2(60f, 18f), ResourceKind.Gold, "5");
            Button(m, offer, new Vector2(-62f, -40f), new Vector2(110f, 52f), UiKit.Leaf, IconPainter.Check);
            Button(m, offer, new Vector2(62f, -40f), new Vector2(110f, 52f), UiKit.InkSoft, IconPainter.Close);

            var shut = m.Pic("Chest", HelpArt.ChestShut, new Vector2(60f, -150f), 70f);
            var open = m.Pic("ChestOpen", HelpArt.ChestOpen, new Vector2(60f, -150f), 70f);
            m.Fade(shut).At(4.0f, 0f).At(4.2f, 1f).At(4.7f, 1f).At(4.71f, 0f, DemoEase.Step);
            m.Size(shut.transform).At(4.0f, 0.3f).At(4.3f, 1f, DemoEase.Back);
            m.Fade(open).At(4.7f, 0f).At(4.71f, 1f, DemoEase.Step).At(5.8f, 1f).At(6.1f, 0f);
            m.FloatUp(HelpArt.Resource(ResourceKind.Gold), "+5", new Vector2(60f, -80f), 4.9f);

            m.Hand().Show(0.5f, 3.9f)
                .At(0.5f, new Vector2(-150f, 100f)).At(0.9f, new Vector2(-240f, -55f)).Press(1.0f)
                .At(2.3f, new Vector2(110f, -50f), DemoEase.Linear).Lift(2.35f)
                .At(3.3f, new Vector2(8f, 80f)).Tap(3.5f).At(3.9f, new Vector2(-60f, 120f));
        }

        static void FruitTree(HelpMovie m)
        {
            m.Length = 6.5f;
            m.Island("Island", new Vector2(0f, -30f), new Vector2(560f, 280f));
            var tree = m.Pic("Tree", HelpArt.Item("Apple Tree", "Pear Tree"), new Vector2(-20f, -90f), 230f);
            m.Turn(tree.transform).At(1.1f, 0f).At(1.2f, 5f).At(1.35f, -5f).At(1.5f, 4f).At(1.65f, -3f).At(1.8f, 0f);
            var fruitSpots = new[] { new Vector2(-70f, 40f), new Vector2(10f, 70f), new Vector2(50f, 20f) };
            var landSpots = new[] { new Vector2(-120f, -120f), new Vector2(110f, -110f), new Vector2(40f, -140f) };
            for (int i = 0; i < fruitSpots.Length; i++)
            {
                var apple = m.Pic("Fruit", HelpArt.Apple, fruitSpots[i], 36f);
                float drop = 1.3f + i * 0.15f;
                m.Move(apple.transform).At(drop, fruitSpots[i]).At(drop + 0.5f, landSpots[i], DemoEase.In);
                bool chest = i == 2;
                m.Fade(apple).At(0f, 0f).At(0.3f, 1f).At(chest ? drop + 0.5f : 5.6f, 1f).At(chest ? drop + 0.6f : 5.9f, 0f);
            }
            // One of them was a treasure chest: it opens by itself.
            var shut = m.Pic("Chest", HelpArt.ChestShut, landSpots[2] + new Vector2(0f, -10f), 66f);
            var open = m.Pic("ChestOpen", HelpArt.ChestOpen, landSpots[2] + new Vector2(0f, -10f), 66f);
            m.Fade(shut).At(2.1f, 0f).At(2.2f, 1f).At(2.8f, 1f).At(2.81f, 0f, DemoEase.Step);
            m.Size(shut.transform).At(2.1f, 0.3f).At(2.4f, 1f, DemoEase.Back);
            m.Fade(open).At(2.8f, 0f).At(2.81f, 1f, DemoEase.Step).At(4.2f, 1f).At(4.5f, 0f);
            m.FloatUp(HelpArt.Resource(ResourceKind.Gem), "+5", landSpots[2] + new Vector2(0f, 60f), 3.0f);
            m.Hand().Show(0.4f, 1.6f).At(0.4f, new Vector2(200f, 120f)).At(0.9f, new Vector2(-10f, 20f)).Tap(1.05f).At(1.6f, new Vector2(200f, 120f));
        }

        // ---------------------------------------------------------------- building

        static void Bubble(HelpMovie m)
        {
            m.Length = 5f;
            m.Island("Island", new Vector2(0f, -20f), new Vector2(560f, 300f));
            m.Pic("You", HelpArt.You, new Vector2(-170f, -70f), 90f);
            var art = HelpArt.Item("Medium Pine Forest");
            var bubble = BubbleWith(m, art, new Vector2(110f, 10f), 130f);
            m.Move(bubble).At(0f, new Vector2(110f, 10f)).At(0.6f, new Vector2(110f, 22f)).At(1.2f, new Vector2(110f, 10f));
            m.Size(bubble).At(1.3f, 1f).At(1.5f, 1.3f).At(4.5f, 1.3f).At(4.51f, 0.2f, DemoEase.Step).At(4.9f, 1f, DemoEase.Back);
            m.Fade(bubble).At(1.3f, 1f).At(1.5f, 0f).At(4.5f, 0f).At(4.7f, 1f);
            m.Sparkle(new Vector2(110f, 10f), 1.35f, UiKit.Spring);

            var item = m.Pic("Item", art, new Vector2(110f, -30f), 80f);
            m.Move(item.transform).At(1.5f, new Vector2(110f, -30f)).At(2.4f, new Vector2(-245f, -85f), DemoEase.InOut);
            m.Size(item.transform).At(1.5f, 1f).At(2.4f, 0.75f);
            m.Fade(item).At(1.45f, 0f).At(1.5f, 1f, DemoEase.Step).At(4.2f, 1f).At(4.45f, 0f);

            m.Hand().Show(0.5f, 1.8f).At(0.5f, new Vector2(240f, 140f)).At(1.1f, new Vector2(110f, 20f)).Tap(1.25f).At(1.8f, new Vector2(240f, 140f));
        }

        static void Place(HelpMovie m)
        {
            m.Length = 6.5f;
            m.Island("Island", new Vector2(0f, -20f), new Vector2(560f, 300f));
            m.Pic("House", HelpArt.Item("Capy House"), new Vector2(40f, -40f), 120f);
            m.Pic("You", HelpArt.You, new Vector2(-190f, -60f), 90f);

            var item = m.Group("Forest", new Vector2(-255f, -75f));
            var pic = m.Pic("Pic", HelpArt.Item("Medium Pine Forest"), Vector2.zero, 64f, item);
            var red = PlaceRing(m, item, false, 130f);
            var green = PlaceRing(m, item, true, 130f);
            m.Fade(red).At(1.6f, 0f).At(1.7f, 0.5f).At(2.3f, 0.5f).At(2.45f, 0f);
            m.Fade(green).At(2.4f, 0f).At(2.55f, 0.5f).At(3.0f, 0.5f).At(3.15f, 0f);
            var no = m.Shape("No", IconPainter.Close, new Vector2(0f, 70f), new Vector2(34f, 34f), UiKit.Danger, item);
            var yes = m.Shape("Yes", IconPainter.Check, new Vector2(0f, 70f), new Vector2(34f, 34f), UiKit.Leaf, item);
            m.Fade(no).At(1.6f, 0f).At(1.7f, 1f).At(2.3f, 1f).At(2.45f, 0f);
            m.Fade(yes).At(2.4f, 0f).At(2.55f, 1f).At(3.0f, 1f).At(3.15f, 0f);
            m.Move(item).At(0.9f, new Vector2(-255f, -75f)).At(1.8f, new Vector2(40f, -60f), DemoEase.Linear)
                .At(2.2f, new Vector2(40f, -60f)).At(2.9f, new Vector2(190f, -90f), DemoEase.Linear);
            m.Size(pic.transform).At(3.0f, 1f).At(3.3f, 1.7f, DemoEase.Back);
            m.Fade(item).At(0f, 0f).At(0.3f, 1f).At(6.0f, 1f).At(6.3f, 0f);

            m.Hand().Show(0.4f, 3.4f)
                .At(0.4f, new Vector2(-150f, 100f)).At(0.8f, new Vector2(-255f, -45f)).Press(0.9f)
                .At(1.8f, new Vector2(40f, -30f), DemoEase.Linear).At(2.2f, new Vector2(40f, -30f))
                .At(2.9f, new Vector2(190f, -60f), DemoEase.Linear).Lift(2.95f).At(3.4f, new Vector2(120f, 120f));
        }

        static void Buy(HelpMovie m)
        {
            m.Length = 6.5f;
            m.Island("Island", new Vector2(0f, -20f), new Vector2(560f, 320f));
            m.Pic("Store", HelpArt.Item("General Store"), new Vector2(0f, 20f), 160f);
            var garden = HelpArt.Item("Medium Posie Garden");
            m.Pic("Left", HelpArt.Item("Medium Pine Forest"), new Vector2(-130f, -50f), 60f);
            m.Pic("Middle", garden, new Vector2(0f, -110f), 60f);
            m.Pic("Right", HelpArt.Item("Mine"), new Vector2(130f, -50f), 60f, null, new Color(1f, 1f, 1f, 0.5f));
            m.Pic("You", HelpArt.You, new Vector2(-220f, -120f), 76f);

            var card = Panel(m, new Vector2(0f, 90f), new Vector2(320f, 190f), UiKit.Primary, 1.3f, 2.5f);
            m.Pic("Item", garden, new Vector2(-90f, -30f), 80f, card);
            Lines(m, card, new Vector2(50f, 30f), 140f, 2, 24f);
            Cost(m, card, new Vector2(10f, -18f), ResourceKind.Wood, "5");
            Cost(m, card, new Vector2(100f, -18f), ResourceKind.Gold, "1");
            Button(m, card, new Vector2(50f, -66f), new Vector2(170f, 50f), UiKit.Primary, IconPainter.Check);

            var bought = m.Pic("Bought", garden, new Vector2(0f, -110f), 60f);
            m.Move(bought.transform).At(2.7f, new Vector2(0f, -110f)).At(3.5f, new Vector2(-280f, -130f));
            m.Fade(bought).At(2.69f, 0f).At(2.7f, 1f, DemoEase.Step).At(6.0f, 1f).At(6.3f, 0f);

            m.Hand().Show(0.4f, 2.9f)
                .At(0.4f, new Vector2(200f, -150f)).At(0.9f, new Vector2(0f, -90f)).Tap(1.05f)
                .At(2.0f, new Vector2(50f, 30f)).Tap(2.2f).At(2.9f, new Vector2(200f, 0f));
        }

        // ---------------------------------------------------------------- goals and gifts

        static void Goals(HelpMovie m)
        {
            m.Length = 6.5f;
            m.Island("Island", new Vector2(0f, -60f), new Vector2(560f, 240f));
            var card = m.Card("GoalCard", new Vector2(-80f, 150f), new Vector2(400f, 80f));
            var star = m.Shape("StarDisc", UiKit.Circle, new Vector2(-160f, 0f), new Vector2(56f, 56f), UiKit.Sun, card);
            m.Shape("Star", IconPainter.Star, Vector2.zero, new Vector2(34f, 34f), Color.white, star.transform);
            Lines(m, card, new Vector2(30f, 12f), 260f, 2, 24f);

            var who = m.Pic("Miso", HelpArt.Item("Coriander"), new Vector2(150f, -110f), 150f);
            var mark = m.Label("Quest", "!", new Vector2(150f, 70f), 70f, UiKit.Sun);
            mark.outlineWidth = 0.3f;
            mark.outlineColor = new Color32(90, 50, 20, 255);
            m.Move(mark.transform).At(0f, new Vector2(150f, 70f)).At(0.4f, new Vector2(150f, 82f)).At(0.8f, new Vector2(150f, 70f))
                .At(1.2f, new Vector2(150f, 82f)).At(1.6f, new Vector2(150f, 70f));
            m.Fade(mark).At(4.0f, 1f).At(4.2f, 0f).At(6.2f, 0f).At(6.4f, 1f);

            var goals = Panel(m, new Vector2(0f, -10f), new Vector2(460f, 300f), UiKit.Sun, 1.3f, 2.7f);
            for (int i = 0; i < 3; i++)
            {
                var y = 70f - i * 80f;
                m.Shape("Tick", UiKit.Circle, new Vector2(-180f, y), new Vector2(44f, 44f), i == 0 ? UiKit.Leaf : UiKit.SurfaceSunk, goals);
                if (i == 0) m.Shape("Check", IconPainter.Check, new Vector2(-180f, y), new Vector2(28f, 28f), Color.white, goals);
                Lines(m, goals, new Vector2(30f, y + 12f), 300f, 2, 24f);
            }

            var talk = Panel(m, new Vector2(-40f, 40f), new Vector2(300f, 130f), Color.white, 4.0f, 5.6f);
            m.Pic("Face", HelpArt.Item("Coriander"), new Vector2(-100f, -50f), 90f, talk);
            Lines(m, talk, new Vector2(40f, 20f), 160f, 3, 24f);
            m.Sparkle(new Vector2(150f, 0f), 4.3f);

            m.Hand().Show(0.4f, 4.3f)
                .At(0.4f, new Vector2(100f, 0f)).At(0.9f, new Vector2(-80f, 140f)).Tap(1.05f)
                .At(3.0f, new Vector2(50f, 80f)).At(3.5f, new Vector2(150f, -40f)).Tap(3.7f).At(4.3f, new Vector2(250f, 60f));
        }

        /// <summary>
        /// A warm heart: earn something and a glowing heart joins your trail, beating. Tap it and it
        /// flies straight home to the tower (MoteHeart: no card), which glows as it lands.
        /// </summary>
        static void Hearts(HelpMovie m)
        {
            m.Length = 5.5f;
            m.Island("Island", new Vector2(0f, -30f), new Vector2(560f, 300f));
            var glow = m.Shape("Glow", IconPainter.Glow, new Vector2(170f, 40f), new Vector2(300f, 300f), new Color(1f, 0.75f, 0.6f, 0f));
            m.Pic("Tower", HelpArt.Item("Tower Of Love"), new Vector2(170f, -110f), 250f);
            m.Fade(glow).At(2.3f, 0f).At(2.6f, 0.9f).At(3.4f, 0f);
            m.Pic("You", HelpArt.You, new Vector2(-180f, -80f), 90f);

            var heart = m.Shape("Heart", HelpArt.Heart, new Vector2(-250f, -20f), new Vector2(54f, 54f), Color.white);
            heart.preserveAspect = true;
            if (IconPainter.IsIcon(heart.sprite)) heart.color = UiKit.Hex("F0506E");
            m.Move(heart.transform).At(1.25f, new Vector2(-250f, -20f)).At(1.75f, new Vector2(-40f, 150f), DemoEase.Out).At(2.3f, new Vector2(170f, 60f), DemoEase.In);
            m.Size(heart.transform).At(0f, 1f).At(0.3f, 1.15f).At(0.6f, 1f).At(0.9f, 1.15f).At(1.2f, 1f)
                .At(2.2f, 1f).At(2.3f, 0.2f).At(4.9f, 0.2f).At(5.0f, 0f).At(5.4f, 1f, DemoEase.Back);
            m.Fade(heart).At(2.2f, 1f).At(2.3f, 0f).At(5.0f, 0f).At(5.1f, 1f);
            m.Sparkle(new Vector2(170f, 60f), 2.35f);

            m.Hand().Show(0.4f, 1.7f)
                .At(0.4f, new Vector2(-100f, 120f)).At(0.9f, new Vector2(-250f, -20f)).Tap(1.05f).At(1.7f, new Vector2(-150f, 120f));
        }

        /// <summary>
        /// Daily gifts as the game has them (DailyGifts, DailyGiftScreen). A red dot on the Gifts
        /// button in the column on the right says today's present is waiting. Tap it and the Daily
        /// Gifts screen opens over the dimmed island: "Day 3 of your streak", the week's seven tiles
        /// four to a row (wood, gold, wood, gold, gems, wood and gold, and an outfit on day 7), the
        /// days already collected ticked off in green, today's tile glowing and gently pulsing, and
        /// one big "Collect day 3" under them. Tap it: the tile bounces and is ticked off, the wood
        /// goes straight onto its counter at the top, and the button greys to "Come back tomorrow".
        /// Close it with the ✕: the dot has gone, and the wood is yours.
        /// </summary>
        static void Gifts(HelpMovie m)
        {
            const float L = 6.4f;
            m.Length = L;
            const float tap = 0.95f, open = 1.1f, claim = 2.6f, shut = 3.95f;
            const int today = 3;   // the third day of a streak: days 1 and 2 already collected
            var gift = DailyGifts.Week[today - 1];
            var accent = UiKit.Hex("E3A92B");

            // The island, you, and the counters at the top.
            m.Island("Island", new Vector2(-35f, -45f), new Vector2(500f, 260f));
            m.Pic("House", HelpArt.Item("Capy House"), new Vector2(-170f, -20f), 110f);
            m.Pic("Forest", HelpArt.Item("Medium Pine Forest"), new Vector2(80f, -5f), 100f);
            var (_, youBody) = Walker(m, "You", HelpArt.You, new Vector2(-40f, -125f), 90f);
            m.Face(youBody).At(0f, -1f);
            const int wood0 = 640, gold0 = 520, gems0 = 12;
            var woodCount = m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "", WoodPill);
            var goldCount = m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "", GoldPill);
            var gemCount = m.Pill("Gems", HelpArt.Resource(ResourceKind.Gem), "", GemPill);

            // The column of round buttons down the right, the Gifts one wearing its red dot.
            FixHudButton(m, "Collection", IconPainter.Cards, UiKit.Hex("A77BD6"), new Vector2(262f, 100f), 56f);
            var giftAt = new Vector2(262f, 18f);
            var giftButton = FixHudButton(m, "Gifts", IconPainter.Gift, accent, giftAt, 56f);
            FixHudButton(m, "Friends", IconPainter.Plane, UiKit.Hex("5B9BD5"), new Vector2(262f, -64f), 56f);
            var dot = m.Shape("Dot", UiKit.Circle, new Vector2(21f, 21f), new Vector2(18f, 18f), UiKit.Danger, giftButton);
            m.Between(dot.gameObject, 0f, claim);
            m.Size(giftButton).At(tap - 0.1f, 1f).At(tap, 0.88f).At(tap + 0.18f, 1f);

            // The Daily Gifts screen.
            var sheet = FixSheet(m, accent, IconPainter.Calendar, "Daily Gifts", open, shut, out var closeAt);
            string streak = "Day " + today + " of your streak — keep it going!";
            const string miss = "Miss a day and the streak starts again from day 1.";
            const string done = "You've collected today's gift. See you tomorrow!";
            var line1 = m.Label("Subtitle", streak, new Vector2(0f, 107f), 17f, UiKit.InkSoft, sheet);
            Fit(line1, streak, 510f);
            var line2 = m.Label("Subtitle", miss, new Vector2(0f, 86f), 13f, UiKit.InkSoft, sheet);
            Fit(line2, miss, 510f);
            var line3 = m.Label("Subtitle", done, new Vector2(0f, 97f), 17f, UiKit.InkSoft, sheet);
            Fit(line3, done, 510f);
            m.Between(line1.gameObject, 0f, claim);
            m.Between(line2.gameObject, 0f, claim);
            m.Between(line3.gameObject, claim, L);

            // The seven tiles, four to a row (the last row starts at the left, as the grid lays it).
            for (int i = 0; i < DailyGifts.Week.Length; i++)
            {
                int day = i + 1;
                var at = new Vector2(-186f + (i % 4) * 124f, 26f - (i / 4) * 100f);
                var tile = FixGiftTile(m, sheet, at, day, DailyGifts.Week[i]);
                if (day < today) FixGiftDone(m, tile, 0f);
                if (day != today) continue;
                // Today's: glowing and gently pulsing until it's collected, then a bounce and ticked off.
                var glow = UiKit.Hex("FFF1C9");
                m.Do(s => tile.color = s >= open && s < claim ? glow : Color.white);
                var pulse = m.Size(tile).At(0f, 1f);
                for (float t = open; t + 0.6f < claim; t += 1.2f) pulse.At(t, 1f).At(t + 0.6f, 1.06f);
                pulse.At(claim, 1f).At(claim + 0.1f, 1.2f).At(claim + 0.22f, 0.92f).At(claim + 0.34f, 1.05f).At(claim + 0.46f, 1f);
                FixGiftDone(m, tile, claim);
            }

            // One big button under them: Collect day 3, then grey once it's done.
            var collect = new Vector2(0f, -146f);
            var ready = FixButton(m, sheet, collect, new Vector2(520f, 44f), UiKit.Primary, UiKit.PrimaryDark, IconPainter.Gift, "Collect day " + today, Color.white);
            var spent = FixButton(m, sheet, collect, new Vector2(520f, 44f), UiKit.SurfaceSunk, UiKit.Hex("DCCFBD"), IconPainter.Gift, "Come back tomorrow", UiKit.InkSoft);
            m.Between(ready.gameObject, 0f, claim);
            m.Between(spent.gameObject, claim, L);
            m.Size(ready).At(claim - 0.12f, 1f).At(claim, 0.95f);

            // The gift goes straight into the store: its counter at the top counts up.
            m.Tally(woodCount, wood0, gift.Wood > 0 ? new[] { (claim, wood0 + gift.Wood) } : Array.Empty<(float, int)>());
            m.Tally(goldCount, gold0, gift.Gold > 0 ? new[] { (claim, gold0 + gift.Gold) } : Array.Empty<(float, int)>());
            m.Tally(gemCount, gems0, gift.Gems > 0 ? new[] { (claim, gems0 + gift.Gems) } : Array.Empty<(float, int)>());
            var counter = gift.Gems > 0 ? GemPill : gift.Gold > 0 && gift.Wood == 0 ? GoldPill : WoodPill;
            m.Sparkle(PillIcon(counter), claim + 0.05f, UiKit.Sun, 6);

            m.Hand().Show(0.35f, shut + 0.4f)
                .At(0.35f, giftAt + new Vector2(-150f, -70f)).At(0.8f, giftAt).Tap(tap)
                .At(2.25f, FixSheetAt + collect).Tap(claim)
                .At(shut - 0.45f, closeAt).Tap(shut - 0.12f).At(shut + 0.4f, closeAt + new Vector2(-90f, -110f));
            FixDip(m, L);
        }

        // ---------------------------------------------------------------- dressing up

        /// <summary>
        /// Your capybara as the wardrobe draws it: the bare picture standing on its feet at
        /// <paramref name="feet"/>, and each piece pinned where it really sits (its Anchor, Width
        /// and Rotation, as the wardrobe's preview and BurrowScreen.DressedCapy place them), worn
        /// between its two moments.
        /// </summary>
        static RectTransform Dressed(HelpMovie m, Transform parent, Vector2 feet, float height, params (string id, float on, float off)[] worn)
        {
            var art = HelpArt.You;
            float aspect = art != null ? art.rect.width / art.rect.height : 1f;
            var body = m.Shape("You", art, feet, new Vector2(height * aspect, height), Color.white, parent);
            body.preserveAspect = true;
            var r = body.rectTransform;
            r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = feet;
            foreach (var (id, on, off) in worn)
            {
                var item = Array.Find(Wardrobe.Items, x => x.Id == id);
                var look = Wardrobe.Art(item);
                if (look == null) continue;
                float w = item.Width * height * aspect;
                var layer = m.Shape(id, look, Vector2.zero, new Vector2(w, w * look.rect.height / look.rect.width), Color.white, body.transform);
                layer.preserveAspect = true;
                var lr = layer.rectTransform;
                lr.anchorMin = lr.anchorMax = item.Anchor;
                lr.anchoredPosition = Vector2.zero;
                lr.localRotation = Quaternion.Euler(0f, 0f, item.Rotation);
                if (item.Behind) lr.SetAsFirstSibling();
                m.Between(layer.gameObject, on, off);
                m.Size(layer.transform).At(on, 0.4f).At(on + 0.25f, 1f, DemoEase.Back);
            }
            return r;
        }

        /// <summary>A piece in the wardrobe's grid, ringed in pink while it's being worn.</summary>
        static void WearTile(HelpMovie m, Transform parent, Vector2 at, string id, float shownFrom, float shownTo, float wornFrom = -1f, float wornTo = -1f)
        {
            var g = m.Group(id, at, parent);
            var mark = m.Shape("Worn", UiKit.Rounded(20), Vector2.zero, new Vector2(94f, 94f), UiKit.Hex("E58FB0"), g);
            mark.type = Image.Type.Sliced;
            var tile = m.Shape("Tile", UiKit.Rounded(16), Vector2.zero, new Vector2(82f, 82f), UiKit.SurfaceSunk, g);
            tile.type = Image.Type.Sliced;
            m.Shape("Pic", Wardrobe.Art(id), Vector2.zero, new Vector2(66f, 66f), Color.white, g).preserveAspect = true;
            m.Between(g.gameObject, shownFrom, shownTo);
            m.Between(mark.gameObject, wornFrom, wornTo);
        }

        /// <summary>
        /// Dressing up as the game does it: tap your picture at the top left, then Change outfit.
        /// The wardrobe opens with your capybara on its little stage and the hats laid out; tap the
        /// straw hat and it's on your head, tap the flower crown and that takes its place (one
        /// piece a slot). The Face tab has the glasses: tap the sunglasses and they go on too.
        /// Close it, and you're out on the island in your new look.
        /// </summary>
        static void DressUp(HelpMovie m)
        {
            const float L = 9f;
            m.Length = L;
            const float badgeTap = 0.9f, profileFrom = 1.1f, outfitTap = 1.95f, open = 2.2f;
            const float straw = 3.0f, flowers = 4.0f, faceTab = 5.0f, shades = 5.9f, close = 6.85f;

            m.Island("Island", new Vector2(0f, -60f), new Vector2(560f, 240f));
            Dressed(m, null, new Vector2(30f, -150f), 130f, ("Hat_Flowers", close, L), ("Face_Sunglasses", close, L));
            m.Sparkle(new Vector2(30f, 10f), close + 0.2f, UiKit.Sun, 6);

            // Your picture at the top left of the screen.
            var badgeAt = new Vector2(-240f, 145f);
            var badge = m.Shape("Badge", UiKit.Circle, badgeAt, new Vector2(90f, 90f), UiKit.Surface);
            m.Pic("Face", HelpArt.You, new Vector2(0f, -36f), 74f, badge.transform);
            m.Shape("Rim", IconPainter.Ring, Vector2.zero, new Vector2(90f, 90f), UiKit.Primary, badge.transform);

            // Your profile, with Change outfit under your picture.
            var profileAt = new Vector2(0f, 10f);
            var profile = Panel(m, profileAt, new Vector2(380f, 260f), UiKit.Primary, profileFrom, open);
            m.Shape("Disc", UiKit.Circle, new Vector2(-105f, 10f), new Vector2(130f, 130f), UiKit.SurfaceSunk, profile);
            m.Pic("You", HelpArt.You, new Vector2(-105f, -45f), 110f, profile);
            Lines(m, profile, new Vector2(70f, 40f), 150f, 3, 28f);
            var outfit = new Vector2(0f, -88f);
            WordButton(m, profile, outfit, new Vector2(300f, 54f), UiKit.SurfaceSunk, "Change outfit", UiKit.Ink);

            // The wardrobe: you on your stage, a tab for each slot, and the pieces of that slot.
            var panelAt = new Vector2(0f, -10f);
            Vector2 S(Vector2 local) => panelAt + local;
            var pink = UiKit.Hex("E58FB0");
            var panel = Panel(m, panelAt, new Vector2(580f, 370f), pink, open, close);
            var closeAt = new Vector2(262f, 163f);
            m.Shape("Close", IconPainter.Close, closeAt, new Vector2(26f, 26f), Color.white, panel).preserveAspect = true;
            m.Shape("Stage", UiKit.Circle, new Vector2(-145f, -30f), new Vector2(240f, 240f), UiKit.Hex("E9F6DD"), panel);
            Dressed(m, panel, new Vector2(-145f, -130f), 210f, ("Hat_Straw", straw, flowers), ("Hat_Flowers", flowers, L), ("Face_Sunglasses", shades, L));

            string[] tabs = { "Hats", "Neck", "Face" };
            var tabAt = new Vector2[3];
            for (int i = 0; i < tabs.Length; i++)
            {
                tabAt[i] = new Vector2(60f + i * 90f, 100f);
                WordButton(m, panel, tabAt[i], new Vector2(84f, 40f), UiKit.SurfaceSunk, tabs[i], UiKit.Ink);
            }
            m.Between(WordButton(m, panel, tabAt[0], new Vector2(84f, 40f), pink, tabs[0], Color.white).gameObject, 0f, faceTab);
            m.Between(WordButton(m, panel, tabAt[2], new Vector2(84f, 40f), pink, tabs[2], Color.white).gameObject, faceTab, L);

            Vector2 Cell(int i) => new(60f + (i % 3) * 90f, 20f - (i / 3) * 96f);
            string[] hats = { "Hat_Straw", "Hat_Flowers", "Hat_Beanie", "Hat_Party", "Hat_Crown", "Hat_Angler" };
            for (int i = 0; i < hats.Length; i++)
            {
                float on = i == 0 ? straw : i == 1 ? flowers : -1f, off = i == 0 ? flowers : i == 1 ? L : -1f;
                WearTile(m, panel, Cell(i), hats[i], 0f, faceTab, on, off);
            }
            WearTile(m, panel, Cell(0), "Face_Glasses", faceTab, L);
            WearTile(m, panel, Cell(1), "Face_Sunglasses", faceTab, L, shades, L);

            m.Hand().Show(0.4f, close + 0.5f)
                .At(0.4f, badgeAt + new Vector2(130f, -50f)).At(0.8f, badgeAt).Tap(badgeTap)
                .At(1.7f, profileAt + outfit).Tap(outfitTap)
                .At(2.7f, S(Cell(0))).Tap(straw)
                .At(3.7f, S(Cell(1))).Tap(flowers)
                .At(4.7f, S(tabAt[2])).Tap(faceTab)
                .At(5.6f, S(Cell(1))).Tap(shades)
                .At(6.5f, S(closeAt)).Tap(close - 0.15f).At(close + 0.5f, new Vector2(160f, -40f));
        }

        // ---------------------------------------------------------------- your burrow

        /// <summary>
        /// Your burrow from the very start, as the game has it. On the island: tap the Burrow Door
        /// the General Store sets out, Purchase, and it pops into your trail; drag it out and put
        /// it down. Tap it, and you slide down the tunnel into your burrow, where the Furniture
        /// Shop waits in a bubble the first time: pop it and it joins your trail outside. Back on
        /// the island, put the shop up and tap it: buy the armchair, and it goes down to your
        /// burrow's storage. Tap the door again, and down below tap the armchair in the storage
        /// along the bottom to stand it in the room, then drag it where it looks cosy.
        /// </summary>
        static void Burrow(HelpMovie m)
        {
            const float L = 17f;
            m.Length = L;
            const string door = "Monster Den";
            // Outside: buying the door, putting it down, tapping it.
            const float tap = 0.95f, cardFrom = 1.15f, buy = 1.85f, bought = 2.0f, grab = 2.95f, drop = 4.0f, knock = 4.7f;
            // Down the tunnel, the Furniture Shop's bubble, and back up.
            const float down = 5.0f, room = 5.9f, pop = 6.9f, up = 8.6f;
            // Outside again: the shop put up, the armchair bought, and down you go.
            const float grab2 = 9.05f, drop2 = 9.95f, shopTap = 10.45f, shopFrom = 10.65f, pay = 11.55f, shopShut = 12.0f, knock2 = 12.55f, down2 = 12.9f;
            // Below: the armchair out of storage and into its place.
            const float pick = 13.8f, hold = 14.55f, set = 15.45f;

            var shopArt = HelpArt.Item("Furniture Shop");
            if (shopArt == null) shopArt = Resources.Load<Sprite>("Home/Store_Furniture");
            var doorArt = HelpArt.Item(door);
            var chair = Furniture.Get("Armchair");
            var bed = Furniture.Get("Bed");

            // ---- outside
            var outside = m.Group("Outside", Vector2.zero);
            During(m, outside, (0f, down), (up, down2));
            m.Island("Island", new Vector2(0f, -35f), new Vector2(570f, 270f), outside);

            var store = new Vector2(-165f, -10f);
            m.Pic("Store", HelpArt.Item("General Store"), store, 125f, outside);
            var names = StoreData.Lists.TryGetValue("general_store", out var list) ? list : new[] { door };
            var buyAt = store + new Vector2(0f, -68f);
            for (int i = 0; i < names.Length; i++)
            {
                float a = (names.Length > 1 ? i * 180f / (names.Length - 1) : 90f) * Mathf.Deg2Rad;
                var at = store + new Vector2(Mathf.Cos(a) * 108f, -Mathf.Sin(a) * 68f);
                Ware(m, HelpArt.Item(names[i]), at, 44f, i * 1.3f, outside);
                if (names[i] == door) buyAt = at;
            }

            // The door, once it's down, and the Furniture Shop beside it later.
            var spot = new Vector2(150f, -55f);
            var placed = m.Pic("Door", doorArt, spot, 125f, outside);
            m.Size(placed.transform).At(drop - 0.01f, 0f).At(drop, 0.3f, DemoEase.Step).At(drop + 0.35f, 1f, DemoEase.Back)
                .At(knock, 1f).At(knock + 0.08f, 1.1f).At(knock + 0.22f, 1f)
                .At(knock2, 1f).At(knock2 + 0.08f, 1.1f).At(knock2 + 0.22f, 1f);
            m.Sparkle(spot + new Vector2(0f, 40f), drop, UiKit.Sun, 5);
            var shopSpot = new Vector2(40f, -105f);
            var shop = m.Pic("FurnitureShop", shopArt, shopSpot, 120f, outside);
            m.Size(shop.transform).At(drop2 - 0.01f, 0f).At(drop2, 0.3f, DemoEase.Step).At(drop2 + 0.35f, 1f, DemoEase.Back)
                .At(shopTap, 1f).At(shopTap + 0.08f, 1.08f).At(shopTap + 0.22f, 1f);
            m.Sparkle(shopSpot + new Vector2(0f, 50f), drop2, UiKit.Sun, 5);

            var (_, youBody) = Walker(m, "You", HelpArt.You, new Vector2(-50f, -150f), 74f, outside);
            m.Face(youBody).At(0f, -1f);
            var trail = new Vector2(-110f, -162f);

            // What you're carrying, popping into the trail behind you and dragged out to its spot.
            void Carried(Sprite art, Vector2 to, float from, float take, float put)
            {
                var item = m.Group("Carried", trail, outside);
                var pic = m.Pic("Pic", art, Vector2.zero, 56f, item);
                var ring = PlaceRing(m, item, true, 110f);
                m.Fade(ring).At(take, 0f).At(take + 0.15f, 0.45f).At(put, 0.45f).At(put + 0.1f, 0f);
                m.Move(item).At(take, trail).At(put, to, DemoEase.Linear);
                m.Size(pic.transform).At(from, 0f).At(from + 0.3f, 1f, DemoEase.Back);
                m.Fade(item).At(from - 0.01f, 0f).At(from, 1f, DemoEase.Step).At(put - 0.01f, 1f).At(put, 0f, DemoEase.Step);
                m.Sparkle(trail + new Vector2(0f, 24f), from, UiKit.Sun, 5);
            }
            Carried(doorArt, spot, bought, grab, drop);
            Carried(shopArt, shopSpot, up + 0.15f, grab2, drop2);

            // The counters: the door's price, then the armchair's.
            var price = StoreData.Price(door);
            const int wood0 = 1500, gold0 = 1600;
            var woodCount = m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "", WoodPill, outside);
            var goldCount = m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "", GoldPill, outside);
            m.Tally(woodCount, wood0, (bought, wood0 - price.x), (pay, wood0 - price.x - chair.Wood));
            m.Tally(goldCount, gold0, (bought, gold0 - price.y), (pay, gold0 - price.y - chair.Gold));
            var purchase = BuyCard(m, new Vector2(40f, 28f), door, doorArt, cardFrom, bought);

            // The Furniture Shop's window: today's pieces, each with its price and Buy.
            var shopAt = new Vector2(30f, 0f);
            var window = Panel(m, shopAt, new Vector2(420f, 270f), UiKit.Hex("9C5B8E"), shopFrom, shopShut);
            var buyButton = Vector2.zero;
            FurnitureDef[] pieces = { chair, bed };
            for (int i = 0; i < pieces.Length; i++)
            {
                var rowAt = new Vector2(0f, 45f - i * 105f);
                var row = m.Shape("Piece", UiKit.Rounded(18), rowAt, new Vector2(396f, 94f), UiKit.SurfaceSunk, window);
                row.type = Image.Type.Sliced;
                m.Pic("Pic", pieces[i].Art, new Vector2(-150f, -36f), 80f, row.transform);
                LeftLabel(m, row.transform, pieces[i].Name, new Vector2(-100f, 20f), 22f, UiKit.Ink, 170f);
                Cost(m, row.transform, new Vector2(-62f, -20f), ResourceKind.Wood, pieces[i].Wood.ToString());
                Cost(m, row.transform, new Vector2(28f, -20f), ResourceKind.Gold, pieces[i].Gold.ToString());
                var b = new Vector2(140f, 0f);
                WordButton(m, row.transform, b, new Vector2(96f, 50f), UiKit.Primary, "Buy", Color.white);
                if (i == 0) buyButton = shopAt + rowAt + b;
            }
            // Bought: it goes down to the burrow's storage, through the door.
            m.Ping(chair.Art, "", buyButton + new Vector2(0f, 30f), spot + new Vector2(0f, 60f), pay + 0.05f);

            // ---- the tunnel, looking down the slide as you whizz along it
            var tunnel = m.Group("Tunnel", Vector2.zero);
            During(m, tunnel, (down, room));
            m.Shape("Earth", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("6B3F22"), tunnel);
            var tunnelArt = Resources.Load<Sprite>("Home/Tunnel");
            var far = m.Group("Far", new Vector2(0f, 70f), tunnel);
            if (tunnelArt != null)
            {
                float ta = tunnelArt.rect.width / tunnelArt.rect.height, th = Mathf.Max(640f / ta, 520f);
                m.Shape("Slide", tunnelArt, new Vector2(0f, -(0.86f - 0.5f) * th), new Vector2(th * ta, th), Color.white, far);
            }
            m.Size(far).At(down, 1f).At(room, 1.9f, DemoEase.In);
            var slider = m.Pic("You", HelpArt.You, new Vector2(0f, -190f), 130f, tunnel);
            m.Move(slider.transform).At(down, new Vector2(0f, -190f)).At(room, new Vector2(0f, 60f), DemoEase.In);
            m.Size(slider.transform).At(down, 1f).At(room, 0.15f, DemoEase.In);

            // ---- inside: the room, you by the steps, and (the first time) the shop's bubble
            var inside = m.Group("Inside", Vector2.zero);
            During(m, inside, (room, up), (down2, L));
            var earth = UiKit.Hex("7A4A2A");
            m.FrameFill(s => (s >= down && s < up) || s >= down2 ? earth : HelpArt.Sea);
            m.Shape("Earth", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("7A4A2A"), inside);
            var cave = Resources.Load<Sprite>("Home/Burrow_Cave");
            if (cave != null)
            {
                // Its painted floor line a little below the middle, as BurrowScreen hangs it.
                float ca = cave.rect.width / cave.rect.height, ch = Mathf.Max(540f, 620f / ca);
                var wall = m.Shape("Cave", cave, new Vector2(0f, -40f), new Vector2(ch * ca, ch), Color.white, inside);
                wall.rectTransform.pivot = new Vector2(0.5f, 0.55f);
                wall.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            }
            var homeYou = m.Pic("You", HelpArt.You, new Vector2(-190f, -150f), 100f, inside);
            m.Face(homeYou).At(0f, -1f);

            var bubbleAt = new Vector2(120f, -40f);
            var bubble = BubbleWith(m, shopArt, bubbleAt, 130f, inside);
            m.Do(s => bubble.anchoredPosition = bubbleAt + new Vector2(0f, Mathf.Sin(s * 2.2f) * 7f));
            m.Between(bubble.gameObject, 0f, pop);
            m.Sparkle(bubbleAt, pop, Color.white, 7);
            // Popped: the shop goes off with you, to be put up outside.
            var given = m.Pic("Given", shopArt, bubbleAt + new Vector2(0f, -40f), 90f, inside);
            m.Move(given.transform).At(pop + 0.35f, bubbleAt + new Vector2(0f, -40f)).At(pop + 1.1f, new Vector2(-190f, -110f), DemoEase.InOut);
            m.Size(given.transform).At(pop, 0.2f).At(pop + 0.3f, 1f, DemoEase.Back).At(pop + 0.75f, 1f).At(pop + 1.1f, 0.3f);
            m.Between(given.gameObject, pop, pop + 1.1f);

            // The storage drawer along the bottom, with the armchair in it.
            var drawer = m.Group("Storage", new Vector2(0f, -158f), inside);
            m.Between(drawer.gameObject, down2, L);
            m.Shape("Drawer", UiKit.Rounded(20), Vector2.zero, new Vector2(600f, 76f), UiKit.HudGlass, drawer).type = Image.Type.Sliced;
            var slotAt = new Vector2(-220f, 0f);
            m.Shape("Slot", UiKit.Rounded(14), slotAt, new Vector2(64f, 64f), new Color(1f, 1f, 1f, 0.35f), drawer).type = Image.Type.Sliced;
            var stored = m.Shape("Armchair", chair.Art, slotAt, new Vector2(56f, 56f), Color.white, drawer);
            stored.preserveAspect = true;
            m.Between(stored.gameObject, 0f, pick);

            var standAt = new Vector2(10f, -120f);
            var cosyAt = new Vector2(150f, -95f);
            var piece = m.Pic("Armchair", chair.Art, standAt, 110f, inside);
            m.Between(piece.gameObject, pick, L);
            m.Move(piece.transform).At(hold, standAt).At(set, cosyAt, DemoEase.Linear);
            m.Size(piece.transform).At(pick, 0.3f).At(pick + 0.3f, 1f, DemoEase.Back).At(set, 1f, DemoEase.Step).At(set + 0.1f, 0.88f).At(set + 0.3f, 1f, DemoEase.Back);
            m.Sparkle(cosyAt + new Vector2(0f, 50f), set + 0.05f, UiKit.Sun, 6);

            // A blink of darkness at each change of scene, so the cuts read as going down and up.
            var dip = m.Shape("Dip", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("2E1A0E"));
            var fade = m.Fade(dip).At(0f, 1f).At(0.3f, 0f);
            foreach (var t in new[] { down, room, up, down2 }) fade.At(t - 0.25f, 0f).At(t, 1f).At(t + 0.25f, 0f);
            fade.At(L - 0.35f, 0f).At(L, 1f);

            // The fingers, one stretch at a time.
            m.Hand().Show(0.35f, knock + 0.35f)
                .At(0.35f, buyAt + new Vector2(130f, 110f)).At(0.8f, buyAt + new Vector2(0f, 20f)).Tap(tap)
                .At(1.65f, purchase).Tap(buy)
                .At(2.75f, trail + new Vector2(0f, 20f)).Press(grab).At(grab, trail + new Vector2(0f, 20f))
                .At(drop, spot + new Vector2(0f, 20f), DemoEase.Linear).Lift(drop + 0.05f)
                .At(knock - 0.3f, spot + new Vector2(0f, 45f)).Tap(knock);
            m.Hand().Show(room + 0.3f, pop + 0.45f)
                .At(room + 0.3f, bubbleAt + new Vector2(140f, -90f)).At(pop - 0.35f, bubbleAt).Tap(pop).At(pop + 0.45f, bubbleAt + new Vector2(120f, -80f));
            m.Hand().Show(up + 0.25f, knock2 + 0.3f)
                .At(up + 0.25f, trail + new Vector2(90f, 100f)).At(grab2 - 0.15f, trail + new Vector2(0f, 20f)).Press(grab2).At(grab2, trail + new Vector2(0f, 20f))
                .At(drop2, shopSpot + new Vector2(0f, 20f), DemoEase.Linear).Lift(drop2 + 0.05f)
                .At(shopTap - 0.3f, shopSpot + new Vector2(0f, 50f)).Tap(shopTap)
                .At(pay - 0.4f, buyButton).Tap(pay)
                .At(knock2 - 0.3f, spot + new Vector2(0f, 45f)).Tap(knock2);
            m.Hand().Show(down2 + 0.35f, set + 0.5f)
                .At(down2 + 0.35f, slotAt + new Vector2(120f, -60f)).At(pick - 0.35f, new Vector2(0f, -158f) + slotAt).Tap(pick)
                .At(hold - 0.15f, standAt + new Vector2(0f, 40f)).Press(hold).At(hold, standAt + new Vector2(0f, 40f))
                .At(set, cosyAt + new Vector2(0f, 40f), DemoEase.Linear).Lift(set + 0.05f).At(set + 0.5f, cosyAt + new Vector2(110f, 90f));
        }

        // ---------------------------------------------------------------- contests

        /// <summary>
        /// A contest as the Events screen shows it (EventsScreen, GameEvents). The Events button in
        /// the column on the right wears a red dot: a contest is on that you haven't looked at yet.
        /// Tap it, and Events opens over the island: the contest's painting with its name on a
        /// ribbon and "Ends in 2 days", who wins, and the three prizes (gems, and the gold medal for
        /// first). Swipe up: the podium, first, second and third each with their own capybara
        /// standing on their step (fur and hats as the game picks them), and the rest of the table
        /// in rows, you fourth in yours. Close it and fish: every catch counts. Open it again and
        /// swipe: you're on the podium now, second. Days later the contest is over and the dot is
        /// back: at the top of Events your prize waits with its own Claim. Tap it: the card says how
        /// you did, the gems go onto their counter, and the prize leaves the list.
        /// </summary>
        static void Contest(HelpMovie m)
        {
            const float L = 17f;
            m.Length = L;
            const float tap1 = 0.85f, open1 = 1.05f, swipe1 = 2.1f, shut1 = 3.75f;
            float[] catches = { 4.4f, 5.3f, 6.2f };
            const float tap2 = 8.6f, open2 = 8.8f, swipe2 = 9.4f, shut2 = 11.0f;
            const float later = 11.5f, tap3 = 12.25f, open3 = 12.45f, claim = 13.45f, popTap = 15.0f, shut3 = 15.9f;
            const float scroll = 290f;
            const int gems0 = 12;
            int[] prize = GameEvents.PrizeGems;

            // You, fishing from the sand at the water's edge: the rod tipped out over the sea and
            // the line down to the bobber (drawn as the fishing movie draws them).
            m.Island("Island", new Vector2(0f, 80f), new Vector2(600f, 230f));
            var feet = new Vector2(-90f, -52f);
            const float rodLength = 110f;
            var rodBase = feet + new Vector2(30f, 26f);
            var rod = m.Shape("Rod", null, rodBase, new Vector2(6f, rodLength), UiKit.Hex("8A5A32"));
            rod.rectTransform.pivot = new Vector2(0.5f, 0f);
            rod.rectTransform.anchoredPosition = rodBase;
            rod.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -58f);
            var you = m.Pic("You", HelpArt.You, feet, 96f);
            m.Face(you).At(0f, -1f);
            var tip = rodBase + (Vector2)(Quaternion.Euler(0f, 0f, -58f) * new Vector3(0f, rodLength, 0f));
            var bobAt = new Vector2(90f, -140f);
            var line = m.Shape("Line", null, (tip + bobAt) * 0.5f, new Vector2(2f, Vector2.Distance(tip, bobAt)), new Color(1f, 1f, 1f, 0.9f));
            line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, bobAt - tip));
            var bobber = m.Shape("Bobber", UiKit.Circle, bobAt, new Vector2(16f, 16f), UiKit.Danger);
            m.Do(s => bobber.rectTransform.anchoredPosition = bobAt + new Vector2(0f, Mathf.Sin(s * 3f) * 3f));

            // The Events button in the column on the right, with its red dot while something there
            // is new (a contest you haven't looked at, a prize to claim), and the gems at the top.
            var trophyAt = new Vector2(262f, 128f);
            var events = FixHudButton(m, "Events", IconPainter.Trophy, UiKit.Hex("E3A92B"), trophyAt, 56f);
            var dot = m.Shape("Dot", UiKit.Circle, new Vector2(21f, 21f), new Vector2(18f, 18f), UiKit.Danger, events);
            During(m, dot, (0f, open1), (later, open3));
            var press = m.Size(events).At(0f, 1f);
            foreach (var t in new[] { tap1, tap2, tap3 }) press.At(t - 0.1f, 1f).At(t, 0.88f).At(t + 0.18f, 1f);
            var gemCount = m.Pill("Gems", HelpArt.Resource(ResourceKind.Gem), "", GemPill);

            // Every fish you catch while it's on counts.
            var fish = HelpArt.Picture("Fish_salmon");
            foreach (var c in catches)
            {
                var leap = m.Pic("Catch", fish, bobAt, 80f);
                m.Move(leap.transform).At(c, bobAt + new Vector2(0f, -30f)).At(c + 0.35f, new Vector2(10f, -40f), DemoEase.Out);
                m.Fade(leap).At(c - 0.01f, 0f).At(c, 1f, DemoEase.Step).At(c + 0.45f, 1f).At(c + 0.55f, 0f);
                m.Sparkle(bobAt, c, Color.white, 5);
                float land = m.Ping(fish, "+1", new Vector2(10f, 0f), trophyAt, c + 0.45f);
                m.Sparkle(trophyAt, land, UiKit.Sun, 4);
            }

            // The table: the islanders' names are the bot names the game borrows for its contests.
            var pool = CapyIsland.Game.Friends.NamePool;
            var rivals = new string[4];
            for (int n = 0; n < rivals.Length; n++)
                rivals[n] = pool != null && pool.Count > 0 ? pool[(n * 5 + 10) % pool.Count] : "Capy " + (n + 1);
            string me = PlayerProfile.Name;
            var before = new (string name, int score, bool you)[]
            {
                (rivals[0], 12, false), (rivals[1], 9, false), (rivals[2], 8, false), (me, 7, true), (rivals[3], 5, false),
            };
            var after = new (string name, int score, bool you)[]
            {
                (rivals[0], 12, false), (me, 7 + catches.Length, true), (rivals[1], 9, false), (rivals[2], 8, false), (rivals[3], 5, false),
            };

            // First look: the contest, then a swipe up to the podium and the rows, you fourth.
            var list1 = FixEventsList(m, open1, shut1, out var close1);
            FixContestPage(m, list1, ContestKind.Fish, "2 days", before, true);
            m.Move(list1).At(swipe1, new Vector2(0f, FixListTop)).At(swipe1 + 0.6f, new Vector2(0f, FixListTop + scroll), DemoEase.Linear);

            // After the catches: you're on the podium, second.
            var list2 = FixEventsList(m, open2, shut2, out var close2);
            FixContestPage(m, list2, ContestKind.Fish, "2 days", after, true);
            m.Move(list2).At(swipe2, new Vector2(0f, FixListTop)).At(swipe2 + 0.6f, new Vector2(0f, FixListTop + scroll), DemoEase.Linear);
            m.Sparkle(FixListAt + new Vector2(-172f, scroll - 400f), swipe2 + 0.75f, UiKit.Sun, 6);

            // Days later: the contest is over. Its prize waits at the top of Events, the next contest under it.
            var list3 = FixEventsList(m, open3, shut3, out var close3);
            var card = m.Shape("Prize", UiKit.Rounded(20), new Vector2(0f, -44f), new Vector2(520f, 84f), UiKit.Hex("FFF4D6"), list3);
            card.type = Image.Type.Sliced;
            var disc = m.Shape("Disc", UiKit.Circle, new Vector2(-218f, 0f), new Vector2(56f, 56f), FixTheme(ContestKind.Fish), card.transform);
            m.Shape("Icon", IconPainter.Trophy, Vector2.zero, new Vector2(34f, 34f), Color.white, disc.transform).preserveAspect = true;
            string month = Loc.T("Oct");
            string dates = Loc.Current == Language.Hebrew ? "19 – 21 " + month : month + " 19 – 21";
            LeftLabel(m, card.transform, Loc.T(EventsScreen.ContestName(ContestKind.Fish)) + " · " + dates, new Vector2(-180f, 14f), 17f, UiKit.Ink, 270f);
            LeftLabel(m, card.transform, "You came second with " + after[1].score + ".", new Vector2(-180f, -13f), 14f, UiKit.InkSoft, 270f);
            var claimAt = new Vector2(190f, 0f);
            var claimButton = FixButton(m, card.transform, claimAt, new Vector2(124f, 46f), UiKit.Primary, UiKit.PrimaryDark, IconPainter.Sparkle, "Claim", Color.white);
            var beat = m.Size(claimButton).At(0f, 1f);
            for (float t = open3; t + 0.6f < claim; t += 1.2f) beat.At(t, 1f).At(t + 0.6f, 1.05f);
            beat.At(claim - 0.1f, 1f).At(claim, 0.92f).At(claim + 0.15f, 1f);
            m.Between(card.gameObject, 0f, claim + 0.1f);
            // Claimed, the prize leaves the list and what's under it moves up.
            var rest = m.Group("Rest", new Vector2(0f, -98f), list3);
            m.Move(rest).At(claim + 0.1f, new Vector2(0f, -98f)).At(claim + 0.11f, Vector2.zero, DemoEase.Step);
            FixContestPage(m, rest, ContestKind.Bugs, "3 days", null, false);

            // The card says how you did; the gems go straight onto their counter.
            FixResultCard(m, IconPainter.Trophy, EventsScreen.ContestName(ContestKind.Fish) + ": you came second!",
                "You finished second with " + after[1].score + ". Here are " + prize[1] + " gems.", claim + 0.05f, popTap);
            m.Tally(gemCount, gems0, (claim + 0.05f, gems0 + prize[1]));
            m.Sparkle(PillIcon(GemPill), claim + 0.15f, UiKit.Sun, 6);

            FixDip(m, L, later);

            var swipeFrom = new Vector2(160f, -170f);
            var swipeTo = swipeFrom + new Vector2(0f, scroll);
            var aside = new Vector2(-70f, -90f);
            var claimOnStage = FixListAt + new Vector2(0f, -44f) + claimAt;
            var popAt = new Vector2(0f, -40f);
            m.Hand().Show(0.4f, shut1 + 0.3f)
                .At(0.4f, trophyAt + new Vector2(-130f, -90f)).At(tap1 - 0.2f, trophyAt).Tap(tap1)
                .At(swipe1 - 0.3f, swipeFrom).Press(swipe1).At(swipe1, swipeFrom).At(swipe1 + 0.6f, swipeTo, DemoEase.Linear).Lift(swipe1 + 0.62f)
                .At(shut1 - 0.45f, close1).Tap(shut1 - 0.12f).At(shut1 + 0.3f, close1 + aside);
            m.Hand().Show(tap2 - 0.5f, shut2 + 0.3f)
                .At(tap2 - 0.5f, trophyAt + new Vector2(-130f, -90f)).At(tap2 - 0.2f, trophyAt).Tap(tap2)
                .At(swipe2 - 0.3f, swipeFrom).Press(swipe2).At(swipe2, swipeFrom).At(swipe2 + 0.6f, swipeTo, DemoEase.Linear).Lift(swipe2 + 0.62f)
                .At(shut2 - 0.45f, close2).Tap(shut2 - 0.12f).At(shut2 + 0.3f, close2 + aside);
            m.Hand().Show(later + 0.35f, shut3 + 0.3f)
                .At(later + 0.35f, trophyAt + new Vector2(-130f, -90f)).At(tap3 - 0.2f, trophyAt).Tap(tap3)
                .At(claim - 0.35f, claimOnStage).Tap(claim).At(claim + 0.45f, claimOnStage + new Vector2(50f, -120f))
                .At(popTap - 0.35f, popAt).Tap(popTap)
                .At(shut3 - 0.45f, close3).Tap(shut3 - 0.12f).At(shut3 + 0.3f, close3 + aside);
        }

        // ---------------------------------------------------------------- the game's screens, for the gifts and contest movies

        /// <summary>Where a screen sits on the stage: short enough that the counters at the top still show above it.</summary>
        static readonly Vector2 FixSheetAt = new(0f, -27f), FixSheetSize = new(560f, 340f);
        /// <summary>The scrolling list inside the Events screen: its window, and where the top of the list sits in it.</summary>
        const float FixViewY = -22f, FixListTop = 141f;
        /// <summary>The top of the Events list on the stage, before any scrolling.</summary>
        static Vector2 FixListAt => FixSheetAt + new Vector2(0f, FixViewY + FixListTop);

        static readonly Color FixGold = UiKit.Hex("E8B23A"), FixSilver = UiKit.Hex("AEB9C4"), FixBronze = UiKit.Hex("CD8B55");

        static Color FixPlace(int i) => i switch { 0 => FixGold, 1 => FixSilver, 2 => FixBronze, _ => UiKit.InkSoft };

        /// <summary>A contest's colour, as EventsScreen paints its frame, ribbon and prize disc.</summary>
        static Color FixTheme(ContestKind k) => k switch
        {
            ContestKind.Bugs => UiKit.Hex("6BAA4E"),
            ContestKind.Birds => UiKit.Hex("5B9BD5"),
            _ => UiKit.Hex("2DA39A"),
        };

        /// <summary>A name someone chose (a bot's, yours): drawn as it is, never looked up in the tables.</summary>
        static void FixNoTranslate(TMP_Text t)
        {
            var loc = t != null ? t.GetComponent<LocText>() : null;
            if (loc != null) loc.NoTranslate = true;
        }

        /// <summary>One of the round buttons down the right of the screen (UiKit.RoundButton), its caption in a dark pill under it.</summary>
        static RectTransform FixHudButton(HelpMovie m, string caption, Sprite icon, Color fill, Vector2 at, float size)
        {
            var disc = m.RoundButton(caption, icon, fill, at, size);
            var pill = m.Shape("Caption", UiKit.Rounded(18), at + new Vector2(0f, -size * 0.5f - 13f), new Vector2(size + 8f, 21f), UiKit.HudGlass);
            pill.type = Image.Type.Sliced;
            var t = m.Label("Caption", caption, Vector2.zero, 14f, Color.white, pill.transform);
            Fit(t, caption, size + 2f);
            return disc;
        }

        /// <summary>The island dimmed behind a screen while it's open (UiScreen's scrim).</summary>
        static void FixScrim(HelpMovie m, float from, float to)
        {
            var dim = m.Shape("Scrim", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Scrim);
            dim.raycastTarget = false;
            float a = UiKit.Scrim.a;
            m.Fade(dim).At(from, 0f).At(from + 0.15f, a).At(to, a).At(to + 0.2f, 0f);
        }

        /// <summary>
        /// A game screen opening over the dimmed island (UiScreen): the card, its coloured band with
        /// the icon and title at the left and the round ✕ at the right. <paramref name="close"/> is
        /// where the ✕ is, on the stage.
        /// </summary>
        static RectTransform FixSheet(HelpMovie m, Color accent, Sprite icon, string title, float from, float to, out Vector2 close)
        {
            FixScrim(m, from, to);
            var size = FixSheetSize;
            var sheet = Panel(m, FixSheetAt, size, accent, from, to);
            float band = size.y * 0.5f - 22f;
            // The band's lower corners squared off where it meets the body.
            m.Shape("Square", null, new Vector2(0f, band - 11f), new Vector2(size.x, 22f), accent, sheet);
            m.Shape("Icon", icon, new Vector2(-size.x * 0.5f + 28f, band), new Vector2(28f, 28f), Color.white, sheet).preserveAspect = true;
            LeftLabel(m, sheet, title, new Vector2(-size.x * 0.5f + 52f, band), 24f, Color.white, 360f);
            var x = new Vector2(size.x * 0.5f - 28f, band);
            m.Shape("Close", UiKit.Circle, x, new Vector2(34f, 34f), new Color(1f, 1f, 1f, 0.25f), sheet);
            m.Shape("X", IconPainter.Close, x, new Vector2(15f, 15f), Color.white, sheet).preserveAspect = true;
            close = FixSheetAt + x;
            return sheet;
        }

        /// <summary>A button as UiKit.Button draws it: a darker lip under the face, an icon and its word side by side.</summary>
        static RectTransform FixButton(HelpMovie m, Transform parent, Vector2 at, Vector2 size, Color fill, Color lip, Sprite icon, string words, Color ink)
        {
            var g = m.Group("Button", at, parent);
            var under = m.Shape("Lip", UiKit.Rounded(18), new Vector2(0f, -4f), size, lip, g);
            under.type = Image.Type.Sliced;
            var face = m.Shape("Face", UiKit.Rounded(18), Vector2.zero, size, fill, g);
            face.type = Image.Type.Sliced;
            float h = size.y * 0.5f;
            Image ic = null;
            if (icon != null)
            {
                ic = m.Shape("Icon", icon, Vector2.zero, new Vector2(h, h), ink, face.transform);
                ic.preserveAspect = true;
            }
            var t = m.Label("Label", words, Vector2.zero, size.y * 0.46f, ink, face.transform);
            float room = size.x - (ic != null ? h + 8f : 0f) - 30f;
            Fit(t, words, room);
            float w = Mathf.Min(room, t.GetPreferredValues(Loc.T(words)).x);
            float row = (ic != null ? h + 8f : 0f) + w;
            if (ic != null) ic.rectTransform.anchoredPosition = new Vector2(-row * 0.5f + h * 0.5f, 0f);
            t.rectTransform.anchoredPosition = new Vector2(row * 0.5f - w * 0.5f, 0f);
            return g;
        }

        /// <summary>A soft fade through the sea's blue at each of <paramref name="cuts"/> (time passing), and as the movie goes round again.</summary>
        static void FixDip(HelpMovie m, float length, params float[] cuts)
        {
            var dip = m.Shape("Dip", null, Vector2.zero, new Vector2(640f, 440f), HelpArt.Sea);
            dip.raycastTarget = false;
            var fade = m.Fade(dip).At(0f, 1f).At(0.3f, 0f);
            foreach (var t in cuts) fade.At(t - 0.3f, 0f).At(t, 1f).At(t + 0.3f, 0f);
            fade.At(length - 0.35f, 0f).At(length, 1f);
        }

        // ---- daily gifts

        /// <summary>One day's tile on the Daily Gifts screen: "Day N" at the top, the gift's icon, and how much.</summary>
        static Image FixGiftTile(HelpMovie m, Transform parent, Vector2 at, int day, DailyGifts.Gift gift)
        {
            var tile = m.Shape("Day" + day, UiKit.Rounded(16), at, new Vector2(112f, 88f), day == 7 ? UiKit.Hex("FDE8F0") : Color.white, parent);
            tile.type = Image.Type.Sliced;
            var head = m.Label("Day", "Day " + day, new Vector2(0f, 30f), 13f, UiKit.InkSoft, tile.transform);
            Fit(head, "Day " + day, 100f);
            Sprite icon;
            string amount;
            if (gift.Outfit) { icon = IconPainter.Hanger; amount = "Outfit!"; }
            else if (gift.Gems > 0) { icon = HelpArt.Resource(ResourceKind.Gem); amount = "+" + gift.Gems; }
            else if (gift.Wood > 0 && gift.Gold > 0) { icon = HelpArt.Resource(ResourceKind.Gold); amount = "+" + gift.Wood + "/" + gift.Gold; }
            else if (gift.Gold > 0) { icon = HelpArt.Resource(ResourceKind.Gold); amount = "+" + gift.Gold; }
            else { icon = HelpArt.Resource(ResourceKind.Wood); amount = "+" + gift.Wood; }
            // Both: a little wood tucked behind the gold.
            if (gift.Wood > 0 && gift.Gold > 0)
                m.Shape("Icon2", HelpArt.Resource(ResourceKind.Wood), new Vector2(-24f, 2f), new Vector2(46f, 28f), Color.white, tile.transform).preserveAspect = true;
            m.Shape("Icon", icon, new Vector2(0f, 4f), new Vector2(66f, 38f), gift.Outfit ? UiKit.Hex("E58FB0") : Color.white, tile.transform).preserveAspect = true;
            var t = m.Label("Amount", amount, new Vector2(0f, -28f), 16f, UiKit.Ink, tile.transform);
            Fit(t, amount, 100f);
            return tile;
        }

        /// <summary>The green cover and tick over a day that's been collected, from <paramref name="from"/> on.</summary>
        static void FixGiftDone(HelpMovie m, Image tile, float from)
        {
            var done = m.Shape("Done", UiKit.Rounded(16), Vector2.zero, new Vector2(112f, 88f), new Color(UiKit.Leaf.r, UiKit.Leaf.g, UiKit.Leaf.b, 0.82f), tile.transform);
            done.type = Image.Type.Sliced;
            m.Shape("Tick", IconPainter.Check, Vector2.zero, new Vector2(44f, 44f), Color.white, done.transform).preserveAspect = true;
            m.Between(done.gameObject, from, float.MaxValue);
        }

        // ---- events

        /// <summary>The Events screen opening, and the list inside its scrolling window (move it up to scroll).</summary>
        static RectTransform FixEventsList(HelpMovie m, float from, float to, out Vector2 close)
        {
            var sheet = FixSheet(m, UiKit.Hex("E3A92B"), IconPainter.Trophy, "Events", from, to, out close);
            var view = m.Group("View", new Vector2(0f, FixViewY), sheet);
            view.sizeDelta = new Vector2(540f, 290f);
            view.gameObject.AddComponent<RectMask2D>();
            return m.Group("List", new Vector2(0f, FixListTop), view);
        }

        /// <summary>
        /// The running contest in the Events list, from the list's top down: the banner, who wins,
        /// how to join if you can't, the three prizes, and (given a <paramref name="board"/>) the
        /// podium for the top three and a row each for the rest.
        /// </summary>
        static void FixContestPage(HelpMovie m, Transform list, ContestKind kind, string left, (string name, int score, bool you)[] board, bool canJoin)
        {
            FixBanner(m, list, -85f, kind, left);
            string counts = kind switch
            {
                ContestKind.Bugs => "Whoever catches the most bugs wins.",
                ContestKind.Birds => "Whoever spots the most birds wins.",
                _ => "Whoever catches the most fish wins.",
            };
            Fit(m.Label("Counts", counts, new Vector2(0f, -186f), 15f, UiKit.Ink, list), counts, 500f);
            float prizesY = -235f;
            if (!canJoin)
            {
                string join = kind switch
                {
                    ContestKind.Bugs => "Get a bug net to take part.",
                    ContestKind.Birds => "Get binoculars to take part.",
                    _ => "Get a fishing rod to take part.",
                };
                Fit(m.Label("Join", join, new Vector2(0f, -206f), 14f, UiKit.Danger, list), join, 500f);
                prizesY = -257f;
            }
            FixPrizes(m, list, prizesY, kind);
            if (board == null) return;
            FixPodium(m, list, -500f, board);
            for (int i = 3; i < board.Length; i++) FixRow(m, list, -527f - (i - 3) * 48f, i + 1, board[i]);
        }

        /// <summary>The contest's painting in a white mount, its name on a ribbon along the bottom, and the time left in a pill.</summary>
        static void FixBanner(HelpMovie m, Transform parent, float y, ContestKind kind, string left)
        {
            var theme = FixTheme(kind);
            var mount = m.Shape("Banner", UiKit.Rounded(22), new Vector2(0f, y), new Vector2(520f, 170f), Color.white, parent);
            mount.type = Image.Type.Sliced;
            var frame = m.Shape("Frame", UiKit.Rounded(16), Vector2.zero, new Vector2(508f, 158f), theme, mount.transform);
            frame.type = Image.Type.Sliced;
            frame.gameObject.AddComponent<RectMask2D>();
            var art = HelpArt.Picture("Events/Banner_" + kind);
            if (art != null)
            {
                // Filling the frame, its edges cropped.
                float aspect = art.rect.width / Mathf.Max(1f, art.rect.height);
                var size = aspect > 508f / 158f ? new Vector2(158f * aspect, 158f) : new Vector2(508f, 508f / aspect);
                m.Shape("Picture", art, Vector2.zero, size, Color.white, frame.transform);
            }
            var ribbon = m.Shape("Ribbon", UiKit.Rounded(16), new Vector2(0f, -52f), new Vector2(484f, 34f), theme, frame.transform);
            ribbon.type = Image.Type.Sliced;
            string name = EventsScreen.ContestName(kind);
            Fit(m.Label("Name", name, Vector2.zero, 22f, Color.white, ribbon.transform), name, 460f);
            var pill = m.Shape("Ends", UiKit.Rounded(16), new Vector2(168f, 56f), new Vector2(156f, 30f), UiKit.HudGlass, frame.transform);
            pill.type = Image.Type.Sliced;
            m.Shape("Icon", IconPainter.Calendar, new Vector2(-62f, 0f), new Vector2(18f, 18f), Color.white, pill.transform).preserveAspect = true;
            string ends = "Ends in " + left;
            Fit(m.Label("Text", ends, new Vector2(10f, 0f), 14f, Color.white, pill.transform), ends, 118f);
        }

        /// <summary>The three prize chips: the place in its coloured disc, the gems, and the medal for first.</summary>
        static void FixPrizes(HelpMovie m, Transform parent, float y, ContestKind kind)
        {
            var medal = Array.Find(Wardrobe.Items, w => w.Unlock == "contest_" + kind);
            var medalArt = medal != null ? Wardrobe.Art(medal) : null;
            for (int place = 1; place <= 3; place++)
            {
                var chip = m.Shape("Prize", UiKit.Rounded(16), new Vector2(-177f + (place - 1) * 177f, y), new Vector2(166f, 64f), Color.white, parent);
                chip.type = Image.Type.Sliced;
                var disc = m.Shape("Place", UiKit.Circle, new Vector2(-65f, 14f), new Vector2(26f, 26f), FixPlace(place - 1), chip.transform);
                m.Label("N", place.ToString(), Vector2.zero, 15f, Color.white, disc.transform);
                bool first = place == 1 && medal != null;
                m.Shape("Gem", HelpArt.Resource(ResourceKind.Gem), new Vector2(first ? -26f : -14f, 5f), new Vector2(26f, 26f), Color.white, chip.transform).preserveAspect = true;
                LeftLabel(m, chip.transform, GameEvents.PrizeGems[place - 1].ToString(), new Vector2(first ? -10f : 2f, 5f), 20f, UiKit.Ink, 50f);
                if (first && medalArt != null)
                    m.Shape("Medal", medalArt, new Vector2(44f, 5f), new Vector2(30f, 38f), Color.white, chip.transform).preserveAspect = true;
                string label = first ? "+ " + medal.Name : "gems";
                Fit(m.Label("Label", label, new Vector2(0f, -22f), 11f, UiKit.InkSoft, chip.transform), label, 150f);
            }
        }

        /// <summary>
        /// First, second and third on their steps, second on the left and third on the right: each
        /// one's capybara standing on top, the cup over the winner, and on the step its name, the
        /// place and the score.
        /// </summary>
        static void FixPodium(HelpMovie m, Transform list, float baseY, (string name, int score, bool you)[] board)
        {
            float[] xs = { 0f, -172f, 172f };
            float[] tall = { 96f, 78f, 64f };
            for (int i = 0; i < 3 && i < board.Length; i++)
            {
                var e = board[i];
                float top = baseY + tall[i];
                var step = m.Shape("Step", UiKit.Rounded(14), new Vector2(xs[i], baseY + tall[i] * 0.5f), new Vector2(160f, tall[i]), FixPlace(i), list);
                step.type = Image.Type.Sliced;
                float height = i == 0 ? 76f : 64f;
                FixCapy(m, list, new Vector2(xs[i], top - 2f), height, e.name, e.you);
                if (i == 0)
                    m.Shape("Cup", IconPainter.Trophy, new Vector2(xs[i], top + height + 16f), new Vector2(30f, 30f), FixGold, list).preserveAspect = true;
                string shown = e.you ? "You" : e.name;
                var name = m.Label("Name", shown, new Vector2(0f, tall[i] * 0.5f - 11f), 13f, Color.white, step.transform);
                Fit(name, shown, 148f);
                name.outlineWidth = 0.2f;
                name.outlineColor = new Color32(60, 40, 25, 255);
                if (!e.you) FixNoTranslate(name);
                m.Label("Place", (i + 1).ToString(), new Vector2(0f, i == 0 ? 4f : 1f), i == 0 ? 28f : 22f, Color.white, step.transform);
                m.Label("Score", e.score.ToString(), new Vector2(0f, -tall[i] * 0.5f + 11f), 15f, new Color(1f, 1f, 1f, 0.9f), step.transform);
            }
        }

        /// <summary>A row of the table under the podium: the place, the capybara, the name ("(you)" on yours, in yellow), the score.</summary>
        static void FixRow(HelpMovie m, Transform list, float y, int place, (string name, int score, bool you) e)
        {
            var row = m.Shape("Row", UiKit.Rounded(16), new Vector2(0f, y), new Vector2(520f, 42f), e.you ? UiKit.Hex("FFE7A3") : Color.white, list);
            row.type = Image.Type.Sliced;
            m.Label("Place", place.ToString(), new Vector2(-236f, 0f), 17f, UiKit.InkSoft, row.transform);
            FixCapy(m, row.transform, new Vector2(-198f, -19f), 36f, e.name, e.you);
            string shown = e.you ? e.name + " (you)" : e.name;
            var name = LeftLabel(m, row.transform, shown, new Vector2(-172f, 0f), 17f, UiKit.Ink, 320f);
            if (!e.you) FixNoTranslate(name);
            m.Label("Score", e.score.ToString(), new Vector2(228f, 0f), 19f, UiKit.PrimaryDark, row.transform);
        }

        /// <summary>
        /// Someone's capybara, standing on its feet, as the Events screen picks it (EventsScreen.Profile):
        /// you in what you're wearing; a bot with its fur shade and hat worked out from its name, so
        /// it always looks the same.
        /// </summary>
        static RectTransform FixCapy(HelpMovie m, Transform parent, Vector2 feet, float height, string name, bool you)
        {
            var worn = new List<(string id, float on, float off)>();
            float tint = 0f;
            if (you)
            {
                foreach (WearSlot slot in Enum.GetValues(typeof(WearSlot)))
                {
                    var piece = Wardrobe.Worn(slot);
                    if (piece != null) worn.Add((piece.Id, -1f, float.MaxValue));
                }
            }
            else
            {
                uint hash = 2166136261;
                foreach (char ch in name ?? "") hash = (hash ^ ch) * 16777619;
                tint = (hash % 200) / 100f - 1f;
                var hats = new List<string>();
                foreach (var piece in Wardrobe.Items)
                    if (piece.Slot == WearSlot.Hat && piece.Unlock != "horaira" && Wardrobe.Art(piece) != null) hats.Add(piece.Id);
                if (hats.Count > 0 && (hash >> 8) % 3 != 0) worn.Add((hats[(int)((hash >> 12) % (uint)hats.Count)], -1f, float.MaxValue));
            }
            var body = Dressed(m, parent, feet, height, worn.ToArray());
            var img = body.GetComponent<Image>();
            if (img != null) img.color = Color.Lerp(Color.white, new Color(0.84f, 0.72f, 0.6f), Mathf.Max(0f, tint) * 0.45f);
            return body;
        }

        /// <summary>The game's card pop-up, as the island draws it (the same one the gems movie uses).</summary>
        static void FixResultCard(HelpMovie m, Sprite picture, string title, string body, float from, float tap) =>
            IsleCardPop(m, picture, title, body, from, tap);

        // ---------------------------------------------------------------- hobbies and pictures

        /// <summary>A hobby's picture, a tap, and what you found popping out of it.</summary>
        static void Hobby(HelpMovie m, string picture, string found)
        {
            m.Length = 5f;
            var art = HelpArt.Picture(picture);
            // The picture right on the movie's own sea-blue, no card of its own (a white box inside
            // the blue frame looked like a frame within a frame). It's see-through around the edges.
            var frame = m.Shape("Picture", UiKit.Rounded(26), Vector2.zero, new Vector2(560f, 370f), new Color(1f, 1f, 1f, 0f));
            frame.type = Image.Type.Sliced;
            if (art != null)
            {
                var pic = m.Shape("Art", art, Vector2.zero, new Vector2(540f, 350f), Color.white, frame.transform);
                pic.preserveAspect = true;
            }
            m.Size(frame.transform).At(0f, 1f).At(2.5f, 1.04f).At(5f, 1f);
            m.Hand().Show(0.6f, 1.9f).At(0.6f, new Vector2(180f, -150f)).At(1.1f, new Vector2(0f, -20f)).Tap(1.25f).At(1.9f, new Vector2(180f, -150f));
            var item = HelpArt.Picture(found);
            if (item == null) { m.Sparkle(new Vector2(0f, 0f), 1.4f); return; }
            var pop = m.Pic("Found", item, new Vector2(0f, -60f), 150f);
            m.Size(pop.transform).At(1.4f, 0f).At(1.75f, 1f, DemoEase.Back).At(3.8f, 1f).At(4.1f, 0f);
            m.Sparkle(new Vector2(0f, 10f), 1.5f);
        }

        /// <summary>
        /// Fishing from the very start, as the game plays it. Skipper Nori sits on the sand by the
        /// water with his wares laid out on the land side of him like a shop's (SeaLibrary): the
        /// Fishing Rod, his three baits, and the Mermaid's Shell (greyed until your fishing is good
        /// enough). Tap the rod and the buy panel opens; Purchase, the price comes off the counters,
        /// the rod is gone from the sand and drops into your paws with a sparkle (Purchasing: gear
        /// opens the hobby at once). Then tap the sand to walk down to the water's edge, tap Start
        /// fishing, and the bobber goes straight in (no aiming). A fish shadow comes to look,
        /// nibbles, and bites with a splash; then "Hold to reel!", a finger held down anywhere, the
        /// catch bar filling as the fish is pulled in, and up it comes. <paramref name="found"/> is
        /// the fish's picture.
        /// </summary>
        static void GoFishing(HelpMovie m, string found)
        {
            // Buying the rod comes first: the fishing itself starts D seconds in.
            const float D = 3.1f;
            const float L = 9f + D;
            m.Length = L;
            const float tapWare = 0.95f, cardFrom = 1.15f, pay = 1.85f, bought = 2.0f, landed = 2.5f, stowed = 3.25f;

            // The beach: grass along the top, a strip of sand, foam, the shallows and the sea (the
            // stage's own blue) below.
            m.Shape("Shallows", null, new Vector2(0f, -45f), new Vector2(640f, 50f), new Color(1f, 1f, 1f, 0.22f));
            var foam = m.Shape("Foam", UiKit.Rounded(8), new Vector2(0f, -30f), new Vector2(660f, 16f), new Color(1f, 1f, 1f, 0.7f));
            m.Do(s => foam.rectTransform.anchoredPosition = new Vector2(0f, -30f + Mathf.Sin(s * Mathf.PI * 2f / 3f) * 4f));
            m.Shape("Sand", null, new Vector2(0f, 15f), new Vector2(640f, 70f), HelpArt.Sand);
            for (int i = 0; i < 7; i++) m.Shape("SandEdge", UiKit.Circle, new Vector2(-330f + i * 110f, -20f), new Vector2(140f, 34f), HelpArt.Sand);
            m.Shape("Grass", null, new Vector2(0f, 125f), new Vector2(640f, 160f), HelpArt.Grass);
            for (int i = 0; i < 6; i++) m.Shape("GrassEdge", UiKit.Circle, new Vector2(-300f + i * 120f, 46f), new Vector2(150f, 40f), HelpArt.Grass);

            // Skipper Nori on the sand by the water, his wares in an arc on the land side of him, the
            // rod at one end, then each bait, then the shell (locked until fishing level 4: greyed).
            var noriAt = new Vector2(180f, -18f);
            var rodArt = HelpArt.Item("Fishing Rod");
            if (rodArt == null) rodArt = HelpArt.Picture("Rod");
            var rodWareAt = noriAt + new Vector2(-97f, 29f);
            int wareCount = 1 + Baits.All.Length + 1;
            for (int i = 0; i < wareCount; i++)
            {
                float share = wareCount > 1 ? i / (float)(wareCount - 1) : 0.5f;
                float ang = Mathf.Lerp(-70f, 70f, share) * Mathf.Deg2Rad;
                var spot = noriAt + new Vector2(Mathf.Sin(ang) * 103f, Mathf.Cos(ang) * 85f);
                var art = i == 0 ? rodArt : i <= Baits.All.Length ? HobbyData.ItemArt(Baits.All[i - 1].Item) : HobbyData.ItemArt(MermaidCharms.Item);
                var ware = Ware(m, art, spot, 40f, i * 1.3f);
                if (i == 0)
                {
                    // Once it's yours Nori has no more rods to sell you: it's gone from the sand.
                    rodWareAt = spot;
                    m.Fade(ware).At(bought - 0.01f, 1f).At(bought, 0f, DemoEase.Step);
                }
                else if (i > Baits.All.Length) ware.color = new Color(0.6f, 0.6f, 0.6f, 0.4f);
            }
            m.Pic("Nori", HelpArt.Picture("Sailor"), noriAt, 90f);

            // Fish shadows gliding in the shallows, as the game draws them (nose to the left).
            var shade = new Color(0.08f, 0.14f, 0.22f, 0.42f);
            var paths = new[] { (new Vector2(-250f, -105f), new Vector2(-130f, -140f)), (new Vector2(-40f, -180f), new Vector2(-200f, -170f)) };
            foreach (var (a, b) in paths)
            {
                var g = m.Group("Shadow", a);
                var fish = m.Shape("Fish", IconPainter.FishShadow, Vector2.zero, new Vector2(80f, 46f), shade, g);
                m.Move(g).At(0f, a).At(L * 0.5f, b).At(L, a);
                bool right = b.x > a.x;
                m.Face(fish).At(0f, right ? -1f : 1f).At(L * 0.5f, right ? 1f : -1f, DemoEase.Step);
            }

            // The one that bites: it comes over to look at the bobber, nibbles, and is hooked.
            var bobAt = new Vector2(100f, -95f);
            var near = bobAt + new Vector2(34f, -6f);
            var fishG = m.Group("TheFish", new Vector2(255f, -150f));
            var fishShadow = m.Shape("Fish", IconPainter.FishShadow, Vector2.zero, new Vector2(96f, 56f), shade, fishG);
            var fishKeys = new DemoKeys().At(0f, new Vector2(255f, -150f)).At(D + 4.4f, new Vector2(205f, -132f))
                .At(D + 5.5f, near).At(D + 5.75f, near).At(D + 5.82f, near + new Vector2(-6f, 0f)).At(D + 5.95f, near)
                .At(D + 6.07f, near + new Vector2(-6f, 0f)).At(D + 6.2f, near).At(D + 6.4f, near + new Vector2(0f, -12f));
            m.Fade(fishShadow).At(0f, 0f).At(0.6f, shade.a).At(D + 7.9f, shade.a).At(D + 8.1f, 0f);
            var wriggle = m.Turn(fishShadow).At(D + 6.4f, 0f);
            for (int i = 0; i < 7; i++) wriggle.At(D + 6.6f + i * 0.2f, i % 2 == 0 ? 14f : -14f);
            wriggle.At(D + 8.0f, 0f);

            WaterRing(m, bobAt, D + 4.35f);
            WaterRing(m, bobAt, D + 5.82f, 0.6f);
            WaterRing(m, bobAt, D + 6.07f, 0.6f);
            WaterRing(m, bobAt, D + 6.4f, 1.3f);

            // You: up on the grass while you buy the rod, then down to the water's edge where you were tapped.
            var start = new Vector2(-230f, 52f);
            var edge = new Vector2(-45f, -8f);
            var (you, body) = Walker(m, "You", HelpArt.You, start, 100f);
            m.Move(you).At(D + 1.15f, start).At(D + 2.5f, edge, DemoEase.Linear);
            m.Face(body).At(0f, -1f);
            // Walking, and two happy hops when the rod lands.
            m.Bob(body, 7f, 12f, s => (s > D + 1.15f && s < D + 2.5f) || (s > landed && s < landed + 0.5f));
            m.Fade(body).At(0f, 0f).At(0.25f, 1f).At(D + 8.6f, 1f).At(D + 8.9f, 0f);

            // The rod you bought drops in beside you with a bounce and a sparkle (HobbyCutscene), then it's in your paws.
            var beside = start + new Vector2(48f, 0f);
            var high = beside + new Vector2(0f, 380f);
            var gear = m.Group("Gear", high);
            m.Pic("Pic", rodArt, Vector2.zero, 60f, gear);
            m.Move(gear).At(bought, high).At(landed - 0.1f, beside, DemoEase.In)
                .At(landed, beside + new Vector2(0f, 16f), DemoEase.Out).At(landed + 0.1f, beside, DemoEase.In)
                .At(stowed - 0.3f, beside).At(stowed, start + new Vector2(22f, 30f));
            m.Size(gear).At(stowed - 0.3f, 1f).At(stowed, 0f, DemoEase.In);
            m.Fade(gear).At(bought - 0.01f, 0f).At(bought, 1f, DemoEase.Step);
            m.Sparkle(beside + new Vector2(0f, 30f), landed - 0.1f, UiKit.Sun, 6);

            // The rod comes out when fishing starts: wound back, then swung out to cast; tipped up
            // while reeling.
            const float rodLength = 100f;
            var paw = new Vector2(22f, 32f);
            var rod = m.Shape("Rod", null, paw, new Vector2(6f, rodLength), UiKit.Hex("8A5A32"), you);
            var rodR = rod.rectTransform;
            rodR.pivot = new Vector2(0.5f, 0f);
            m.Turn(rod).At(0f, 35f).At(D + 3.6f, 35f).At(D + 3.72f, 48f).At(D + 3.9f, -55f, DemoEase.Out)
                .At(D + 6.4f, -55f).At(D + 6.55f, -38f).At(D + 7.95f, -38f).At(D + 8.1f, -62f).At(D + 8.3f, -50f);
            m.Fade(rod).At(0f, 0f).At(D + 3.5f, 0f).At(D + 3.62f, 1f).At(D + 8.6f, 1f).At(D + 8.85f, 0f);
            Vector2 Tip() => you.anchoredPosition + rodR.anchoredPosition + (Vector2)(rodR.localRotation * new Vector3(0f, rodLength, 0f));
            var castTip = edge + paw + (Vector2)(Quaternion.Euler(0f, 0f, -55f) * new Vector3(0f, rodLength, 0f));

            // The line: a few short pieces along a curve from the rod's tip to the bobber, slack
            // while waiting and pulled tight once a fish is on.
            const int pieces = 12;
            var lineG = m.Group("Line", Vector2.zero);
            var bits = new RectTransform[pieces];
            for (int i = 0; i < pieces; i++)
            {
                bits[i] = m.Shape("Bit", null, Vector2.zero, new Vector2(10f, 3f), new Color(1f, 1f, 1f, 0.9f), lineG).rectTransform;
                bits[i].pivot = new Vector2(0f, 0.5f);
            }
            m.Between(lineG.gameObject, D + 3.88f, D + 8.0f);

            // The bobber, red over white: out in an arc, a ripple, bobbing, two nibbles, then under.
            var bob = m.Group("Bobber", castTip);
            m.Shape("Body", UiKit.Circle, Vector2.zero, new Vector2(24f, 24f), Color.white, bob);
            m.Shape("Cap", UiKit.Circle, new Vector2(0f, 6f), new Vector2(22f, 14f), UiKit.Danger, bob);
            var reelEnd = new Vector2(45f, -62f);
            var hookAt = bobAt + new Vector2(0f, -12f);
            var bobMove = m.Move(bob).At(D + 3.88f, castTip)
                .At(D + 4.12f, Vector2.Lerp(castTip, bobAt, 0.5f) + new Vector2(0f, 40f), DemoEase.Out)
                .At(D + 4.35f, bobAt, DemoEase.In)
                .At(D + 4.7f, bobAt + new Vector2(0f, 3f)).At(D + 5.05f, bobAt).At(D + 5.4f, bobAt + new Vector2(0f, 3f)).At(D + 5.75f, bobAt)
                .At(D + 5.82f, bobAt + new Vector2(0f, -6f)).At(D + 5.95f, bobAt)
                .At(D + 6.07f, bobAt + new Vector2(0f, -6f)).At(D + 6.2f, bobAt)
                .At(D + 6.4f, hookAt, DemoEase.Out).At(D + 6.85f, hookAt);
            // Reeled in towards the shore, the fish pulling this way and that.
            for (int k = 1; k <= 5; k++)
            {
                var p = Vector2.Lerp(hookAt, reelEnd, k / 5f);
                if (k < 5) p += new Vector2(0f, k % 2 == 0 ? 6f : -6f);
                bobMove.At(D + 6.85f + k * 0.21f, p, DemoEase.Linear);
            }
            m.Fade(bob).At(D + 3.86f, 0f).At(D + 3.88f, 1f, DemoEase.Step).At(D + 6.38f, 1f).At(D + 6.45f, 0.75f).At(D + 7.95f, 0.75f).At(D + 8.02f, 0f);

            var slack = new DemoKeys().At(D + 3.88f, 10f).At(D + 4.35f, 36f).At(D + 6.35f, 36f).At(D + 6.5f, 3f);
            m.Do(s =>
            {
                if (s < D + 3.88f || s >= D + 8.0f) return;
                Vector2 p0 = Tip(), p2 = bob.anchoredPosition + new Vector2(0f, 8f);
                Vector2 p1 = (p0 + p2) * 0.5f + new Vector2(0f, -slack.Eval(s).x);
                Vector2 prev = p0;
                for (int i = 0; i < pieces; i++)
                {
                    float u = (i + 1f) / pieces, v = 1f - u;
                    Vector2 q = v * v * p0 + 2f * v * u * p1 + u * u * p2;
                    Vector2 d = q - prev;
                    bits[i].anchoredPosition = prev;
                    bits[i].sizeDelta = new Vector2(d.magnitude + 1f, 3f);
                    bits[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                    prev = q;
                }
            });
            // Once hooked, the shadow goes wherever the bobber is pulled.
            m.Do(s => fishG.anchoredPosition = s < D + 6.4f ? fishKeys.Eval(s) : bob.anchoredPosition + (near - bobAt));

            Splash(m, bobAt, D + 6.4f);
            Splash(m, reelEnd, D + 8.0f, 0.8f);

            // Caught: the fish itself, up out of the water.
            var caught = HelpArt.Picture(string.IsNullOrEmpty(found) ? "Fish_salmon" : found);
            var pop = m.Pic("Caught", caught, reelEnd, 120f);
            m.Move(pop.transform).At(D + 8.0f, reelEnd + new Vector2(0f, -20f)).At(D + 8.35f, new Vector2(70f, -10f), DemoEase.Out);
            m.Size(pop.transform).At(D + 8.0f, 0f).At(D + 8.3f, 1f, DemoEase.Back);
            m.Turn(pop.transform).At(D + 8.3f, 0f).At(D + 8.42f, -8f).At(D + 8.54f, 6f).At(D + 8.66f, 0f);
            m.Fade(pop).At(D + 8.55f, 1f).At(D + 8.85f, 0f);
            m.Sparkle(new Vector2(70f, 50f), D + 8.25f);

            // The counters at the top while you shop: the rod's price comes off as you buy. (They
            // make way for the big words once you're fishing.)
            const int wood0 = 400, gold0 = 300;
            var price = StoreData.Price("Fishing Rod");
            var woodCount = m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "", WoodPill);
            var goldCount = m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "", GoldPill);
            m.Tally(woodCount, wood0, (bought, wood0 - price.x));
            m.Tally(goldCount, gold0, (bought, gold0 - price.y));
            m.Fade(woodCount.transform.parent).At(D + 3.3f, 1f).At(D + 3.55f, 0f);
            m.Fade(goldCount.transform.parent).At(D + 3.3f, 1f).At(D + 3.55f, 0f);
            var purchase = BuyCard(m, new Vector2(40f, 28f), "Fishing Rod", rodArt, cardFrom, bought);

            // What to do, large at the top, as in the game: Wait…, Hooked!, then Hold to reel! in the
            // line's colour with the catch bar filling under it.
            var say = m.Group("Say", new Vector2(0f, 155f));
            m.Shape("Back", UiKit.Rounded(30), Vector2.zero, new Vector2(340f, 100f), new Color(0.12f, 0.1f, 0.08f, 0.7f), say);   // a little darker than the game's: it sits on bright grass
            var wait = m.Label("Wait", "Wait…", new Vector2(0f, 12f), 40f, Color.white, say);
            var hooked = m.Label("Hooked", "Hooked!", new Vector2(0f, 12f), 40f, UiKit.Sun, say);
            var reel = m.Label("Reel", "Hold to reel!", new Vector2(0f, 12f), 38f, Game.Fishing.Green, say);
            m.Between(wait.gameObject, D + 3.7f, D + 6.4f);
            m.Between(hooked.gameObject, D + 6.4f, D + 6.75f);
            m.Between(reel.gameObject, D + 6.75f, D + 8.4f);
            const float barWidth = 260f;
            var bar = m.Group("Bar", new Vector2(0f, -28f), say);
            m.Shape("Track", UiKit.Rounded(8), Vector2.zero, new Vector2(barWidth, 14f), new Color(1f, 1f, 1f, 0.2f), bar);
            var fill = m.Shape("Fill", UiKit.Rounded(8), new Vector2(-barWidth * 0.5f, 0f), new Vector2(14f, 14f), Game.Fishing.Green, bar);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            var filled = new DemoKeys().At(D + 6.85f, 14f).At(D + 7.9f, barWidth, DemoEase.Linear);
            m.Do(s => fill.rectTransform.sizeDelta = new Vector2(filled.Eval(s).x, 14f));
            m.Between(bar.gameObject, D + 6.4f, D + 8.4f);
            m.Fade(say).At(D + 3.65f, 0f).At(D + 3.8f, 1f).At(D + 8.1f, 1f).At(D + 8.3f, 0f);
            m.Size(say).At(D + 3.65f, 0.8f).At(D + 3.9f, 1f, DemoEase.Back);

            // Start fishing, bottom centre: the teal button with the rod on it.
            var button = m.Group("StartFishing", new Vector2(0f, -158f));
            var lip = m.Shape("Lip", UiKit.Rounded(26), new Vector2(0f, -2f), new Vector2(10f, 64f), Color.Lerp(UiKit.Spring, Color.black, 0.22f), button);
            var face = m.Shape("Face", UiKit.Rounded(26), new Vector2(0f, 3f), new Vector2(10f, 60f), UiKit.Spring, button);
            var icon = m.Shape("Icon", IconPainter.Rod, Vector2.zero, new Vector2(38f, 38f), Color.white, face.transform);
            icon.preserveAspect = true;
            var words = m.Label("Label", "Start fishing", Vector2.zero, 28f, Color.white, face.transform);
            float wordsWidth = words.GetPreferredValues(Loc.T("Start fishing")).x;
            float row = 38f + 12f + wordsWidth;
            icon.rectTransform.anchoredPosition = new Vector2(-row * 0.5f + 19f, 0f);
            words.rectTransform.anchoredPosition = new Vector2(row * 0.5f - wordsWidth * 0.5f, 0f);
            lip.rectTransform.sizeDelta = new Vector2(row + 56f, 64f);
            face.rectTransform.sizeDelta = new Vector2(row + 56f, 60f);
            m.Size(button).At(0f, 0f).At(D + 2.7f, 0f).At(D + 3.0f, 1f, DemoEase.Back).At(D + 3.42f, 1f).At(D + 3.5f, 0.92f).At(D + 3.62f, 1f);
            m.Fade(button).At(D + 3.62f, 1f).At(D + 3.8f, 0f);

            // Holding to reel: the finger stays down (anywhere: the whole screen is the rod) while a
            // ring fills under it.
            var holdAt = new Vector2(-180f, -120f);
            var holdRing = m.Shape("HoldRing", IconPainter.Ring, holdAt, new Vector2(130f, 130f), Color.white);
            m.Size(holdRing.transform).At(D + 6.8f, 0.35f).At(D + 7.9f, 1f, DemoEase.Linear);
            m.Fade(holdRing).At(0f, 0f).At(D + 6.75f, 0f).At(D + 6.8f, 0.3f).At(D + 7.9f, 0.9f).At(D + 8.1f, 0f);

            FixDip(m, L);

            // Buying the rod: tap it on the sand, then Purchase.
            var ware0 = rodWareAt + new Vector2(0f, 20f);
            m.Hand().Show(0.35f, 2.2f)
                .At(0.35f, rodWareAt + new Vector2(130f, 110f)).At(0.8f, ware0).Tap(tapWare).At(1.1f, ware0)
                .At(1.65f, purchase).Tap(pay).At(1.95f, purchase).At(2.2f, purchase + new Vector2(70f, -60f));
            m.Hand().Show(D + 0.35f, D + 1.5f).At(D + 0.35f, new Vector2(-150f, 150f)).At(D + 0.8f, edge).Tap(D + 0.95f).At(D + 1.5f, new Vector2(-150f, 150f));
            m.Hand().Show(D + 2.95f, D + 3.95f).At(D + 2.95f, new Vector2(150f, -60f)).At(D + 3.3f, new Vector2(10f, -165f)).Tap(D + 3.45f).At(D + 3.95f, new Vector2(150f, -60f));
            m.Hand().Show(D + 6.45f, D + 8.1f).At(D + 6.45f, holdAt + new Vector2(60f, 90f)).At(D + 6.72f, holdAt).Press(D + 6.8f, false)
                .At(D + 7.9f, holdAt).Lift(D + 7.9f).At(D + 8.1f, holdAt + new Vector2(60f, 90f));
        }

        /// <summary>A flat ring spreading on the water.</summary>
        static void WaterRing(HelpMovie m, Vector2 at, float when, float size = 1f)
        {
            var flat = m.Group("WaterRing", at);
            flat.localScale = new Vector3(1f, 0.45f, 1f);
            var ring = m.Shape("Ring", IconPainter.Ring, Vector2.zero, new Vector2(110f, 110f) * size, Color.white, flat);
            m.Size(ring.transform).At(when, 0.25f).At(when + 0.8f, 1f, DemoEase.Out);
            m.Fade(ring).At(when - 0.01f, 0f).At(when, 0.8f, DemoEase.Step).At(when + 0.8f, 0f);
        }

        /// <summary>The splash of a bite: a white burst and droplets thrown up and falling back.</summary>
        static void Splash(HelpMovie m, Vector2 at, float when, float size = 1f)
        {
            var burst = m.Shape("Splash", IconPainter.Glow, at, new Vector2(110f, 80f) * size, Color.white);
            m.Size(burst.transform).At(when, 0.3f).At(when + 0.35f, 1.2f, DemoEase.Out);
            m.Fade(burst).At(when - 0.01f, 0f).At(when, 0.95f, DemoEase.Step).At(when + 0.45f, 0f);
            for (int i = 0; i < 6; i++)
            {
                float side = (i - 2.5f) / 2.5f;   // -1 … 1
                var drop = m.Shape("Drop", UiKit.Circle, at, new Vector2(12f, 12f) * size, Color.white);
                var up = at + new Vector2(side * 36f, 36f + (1f - Mathf.Abs(side)) * 26f) * size;
                var down = at + new Vector2(side * 58f, -6f) * size;
                m.Move(drop.transform).At(when, at).At(when + 0.22f, up, DemoEase.Out).At(when + 0.5f, down, DemoEase.In);
                m.Fade(drop).At(when - 0.01f, 0f).At(when, 1f, DemoEase.Step).At(when + 0.4f, 1f).At(when + 0.5f, 0f);
            }
        }
    }
}
