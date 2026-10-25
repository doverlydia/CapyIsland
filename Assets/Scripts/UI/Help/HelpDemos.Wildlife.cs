using System;
using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    public static partial class HelpDemos
    {
        // ---------------------------------------------------------------- bugs and birds
        //
        // Both hobbies start the same way: the gear is a ware outside a shop (the Bug Net at the
        // Bronze Store, the Binoculars at the Silver Store). Tap it, Purchase, and it drops into your
        // paws (Purchasing.Deliver: gear opens the hobby at once). Then you tap the grass and walk
        // off, the camera following you, to where the bugs or birds are.

        /// <summary>How far the camera has moved by the time you've walked off to the wildlife.</summary>
        static readonly Vector2 WildPan = new(-320f, 0f);
        /// <summary>Where you stand by the shop at the start.</summary>
        static readonly Vector2 WildStart = new(-75f, -150f);
        /// <summary>Where the finger sends you; once the camera has followed, you're at this plus <see cref="WildPan"/>.</summary>
        static readonly Vector2 WildWalkTap = new(240f, -120f);

        const float WildTapWare = 0.95f, WildCardFrom = 1.15f, WildPay = 1.85f, WildBought = 2.0f, WildLand = 2.5f,
            WildStowed = 3.25f, WildTapGrass = 3.45f, WildWalkFrom = 3.6f, WildWalkTo = 5.1f;

        /// <summary>A point on the stage once the camera has followed you, as a point in the world group.</summary>
        static Vector2 WildW(Vector2 stage) => stage - WildPan;

        /// <summary>
        /// Bug catching as the game plays it (WildBug). Buy the Bug Net at the Bronze Store and
        /// walk off to the flowers. A butterfly settles on them and flits about its spot; you walk
        /// straight at it and, once you're right up close, it escapes up and away. Another settles on
        /// the next flowers a few steps off: tap it, it holds still, the net swoops out of your paws
        /// in an arc down over it, wiggles as it goes in, and lifts away. Then the card: "You caught
        /// a Sulphur Butterfly!", tap to continue.
        /// </summary>
        static void BugsMovie(HelpMovie m)
        {
            const float L = 12f;
            m.Length = L;
            const float appearA = 5.2f, tapGrass = 6.05f, strollFrom = 6.15f, strollTo = 6.9f, escape = 6.7f;
            const float appearB = 7.3f, tapBug = 8.4f, swoop = 8.45f, cardFrom = 9.4f, cardTap = 11.0f;

            var kind = HobbyData.Bug("Butterfly");
            var bugArt = HobbyData.Art("Bug_Butterfly");
            var netArt = HobbyData.Art("Net");
            var gearArt = netArt != null ? netArt : HelpArt.Item("Bug Net");

            // After the walk (stage coordinates): where you walk to, the flowers you walk at, and the next flowers along.
            var grassA = new Vector2(20f, -80f);
            var flowersA = new Vector2(60f, -50f);
            var flowersB = new Vector2(205f, -85f);
            const float flowerSize = 95f;
            // A bug on flowers sits just above their middle (Wildlife.FlowerPerch).
            var homeA = flowersA + new Vector2(0f, flowerSize * 0.4f + 15f);
            var homeB = flowersB + new Vector2(0f, flowerSize * 0.4f + 15f);

            Func<float, Vector2> flitB = null;
            RectTransform bugB = null;
            var (world, _) = WildIntro(m, "Bronze Store", "bronze_store", "Bug Net", gearArt, 250, 300, w =>
            {
                var tree = HelpArt.Planted("Medium Pine Forest");
                m.Pic("Tree", tree, WildW(new Vector2(-230f, 30f)), 120f, w);
                m.Pic("Tree", tree, WildW(new Vector2(255f, 45f)), 110f, w);
                var posies = HelpArt.Planted("Medium Posie Garden");
                m.Pic("Flowers", posies, WildW(flowersA), flowerSize, w);
                m.Pic("Flowers", posies, WildW(flowersB), flowerSize, w);
                // The one you rush at: it flits about until you're right up close, then it's off.
                WildBug(m, w, bugArt, WildW(homeA), 64f, appearA, escape, escape);
                // The one you catch: it holds still the moment it's tapped.
                (bugB, flitB) = WildBug(m, w, bugArt, WildW(homeB), 64f, appearB, tapBug, -1f);
            }, grassA, strollFrom, strollTo);

            // Tapped: it stops, the net comes down over it, and in it goes.
            var bugAt = flitB(tapBug);   // in the world group
            var paws = WildW(grassA + new Vector2(26f, 41f));
            float land = WildNetSwoop(m, world, netArt, paws, bugAt, swoop);
            m.Size(bugB).At(land, 1f).At(land + 0.2f, 0f, DemoEase.In);
            m.Sparkle(bugAt + WildPan, land, Color.white, 6);

            string name = kind != null ? kind.Name : "Sulphur Butterfly";
            int stars = kind != null ? kind.Stars : 1;
            var cardAt = WildCard(m, bugArt, "You caught " + HobbyData.WithArticle(name) + "!", stars,
                $"+{5 * stars + 12} bug catching XP", cardFrom, cardTap);

            var tapAt = bugAt + WildPan;
            m.Hand().Show(5.6f, 6.45f).At(5.6f, new Vector2(150f, -170f)).At(5.95f, grassA).Tap(tapGrass)
                .At(6.2f, grassA).At(6.45f, new Vector2(160f, -170f));
            m.Hand().Show(7.95f, 8.85f).At(7.95f, tapAt + new Vector2(90f, -110f)).At(8.3f, tapAt).Tap(tapBug)
                .At(8.6f, tapAt).At(8.85f, tapAt + new Vector2(100f, -110f));
            WildTapCard(m, cardAt, cardTap);
            WildDip(m, L);
        }

        /// <summary>
        /// Birdwatching as the game plays it (WildBird). Buy the Binoculars at the Silver Store and
        /// walk off to the trees. A sparrow flutters down into one, hopping and turning about; you
        /// walk right up under it and it takes fright and flies off. A robin lands in the next tree,
        /// a few steps away: tap it from where you are and the card comes up at once, "You spotted a
        /// Robin!". Tap to continue, and off it flies.
        /// </summary>
        static void BirdsMovie(HelpMovie m)
        {
            const float L = 13.5f;
            m.Length = L;
            const float landA = 5.2f, hopA = 6.6f, tapGrass = 6.8f, strollFrom = 6.9f, strollTo = 7.75f, leaveA = 7.45f;
            const float landB = 7.9f, hopB = 9.35f, tapBird = 10.0f, cardFrom = 10.1f, cardTap = 11.6f, leaveB = 11.7f;

            var sparrow = HobbyData.BirdById("sparrow");
            var robin = HobbyData.BirdById("robin");
            var sparrowArt = sparrow != null ? HobbyData.BirdPicture(sparrow) : HobbyData.Art("Bird_sparrow");
            var robinArt = robin != null ? HobbyData.BirdPicture(robin) : HobbyData.Art("Bird_robin");
            var gearArt = HobbyData.Art("Binoculars");
            if (gearArt == null) gearArt = HelpArt.Item("Binoculars");

            // After the walk (stage coordinates): the tree you walk under, and the next tree along.
            var grassA = new Vector2(30f, -80f);
            var treeA = new Vector2(60f, -45f);
            var treeB = new Vector2(225f, -5f);
            // A bird perches near the top of its tree (Wildlife.TreePerch): a quarter to a third of the way down.
            var perchA = treeA + new Vector2(10f, -14f + 140f * 0.7f);
            var perchB = treeB + new Vector2(-10f, -13f + 130f * 0.7f);

            WildIntro(m, "Silver Store", "silver_store", "Binoculars", gearArt, 600, 550, w =>
            {
                var tree = HelpArt.Planted("Medium Pine Forest");
                m.Pic("Tree", tree, WildW(new Vector2(-230f, 30f)), 120f, w);
                m.Pic("Tree", tree, WildW(treeA), 140f, w);
                m.Pic("Tree", tree, WildW(treeB), 130f, w);
                // The one you walk up to: it turns about on a hop, and flies off the way it faces.
                WildBird(m, w, sparrowArt, WildW(perchA), 56f, landA, 120f, false, hopA, true, leaveA);
                // The one you spot: it stays put through the card, and flies off once it's closed.
                WildBird(m, w, robinArt, WildW(perchB), 56f, landB, -100f, false, hopB, false, leaveB);
            }, grassA, strollFrom, strollTo);

            string name = robin != null ? robin.Name : "Robin";
            int stars = robin != null ? robin.Stars : 1;
            var cardAt = WildCard(m, robinArt, "You spotted " + HobbyData.WithArticle(name) + "!", stars,
                $"+{5 * stars + 12} birding XP", cardFrom, cardTap);

            m.Hand().Show(6.3f, 7.25f).At(6.3f, new Vector2(150f, -170f)).At(6.65f, grassA).Tap(tapGrass)
                .At(7.0f, grassA).At(7.25f, new Vector2(160f, -170f));
            m.Hand().Show(9.55f, 10.45f).At(9.55f, perchB + new Vector2(90f, -120f)).At(9.9f, perchB).Tap(tapBird)
                .At(10.2f, perchB).At(10.45f, perchB + new Vector2(100f, -110f));
            WildTapCard(m, cardAt, cardTap);
            WildDip(m, L);
        }

        /// <summary>
        /// The opening both movies share: the island, the shop with its wares round it, you beside
        /// it; the gear tapped, bought, and dropping into your paws; the price coming off the
        /// counters; then a tap on the grass and the walk off to the wildlife, the camera following.
        /// <paramref name="scenery"/> draws what's out there (in the world group, behind you).
        /// Later you walk to <paramref name="strollTo"/> (stage coordinates, after the camera has
        /// followed) between <paramref name="strollFrom"/> and <paramref name="strollEnd"/>.
        /// </summary>
        static (RectTransform world, RectTransform you) WildIntro(HelpMovie m, string store, string list, string gear, Sprite gearArt,
            int wood0, int gold0, Action<RectTransform> scenery, Vector2 strollTo, float strollFrom, float strollEnd)
        {
            var world = m.Group("World", Vector2.zero);
            m.Move(world).At(WildWalkFrom, Vector2.zero).At(WildWalkTo, WildPan, DemoEase.Linear);

            // One island, long enough for the shop and the wild end: sand rims first, then the grass over them.
            var homeEnd = new Vector2(-40f, -35f);
            var wildEnd = new Vector2(420f, -30f);
            m.Shape("Sand", UiKit.Circle, homeEnd, new Vector2(626f, 308f), HelpArt.Sand, world);
            m.Shape("Sand", UiKit.Circle, wildEnd, new Vector2(726f, 338f), HelpArt.Sand, world);
            m.Shape("Grass", UiKit.Circle, homeEnd, new Vector2(600f, 290f), HelpArt.Grass, world);
            m.Shape("Grass", UiKit.Circle, wildEnd, new Vector2(700f, 320f), HelpArt.Grass, world);

            // The shop, its wares in a half circle in front of it, east round to west (StoreBuilding).
            var shop = new Vector2(-165f, -10f);
            m.Pic("Store", HelpArt.Item(store), shop, 125f, world);
            var names = StoreData.Lists.TryGetValue(list, out var wares) ? wares : new[] { gear };
            var buyAt = shop + new Vector2(0f, -68f);
            for (int i = 0; i < names.Length; i++)
            {
                float a = (names.Length > 1 ? i * 180f / (names.Length - 1) : 90f) * Mathf.Deg2Rad;
                var at = shop + new Vector2(Mathf.Cos(a) * 108f, -Mathf.Sin(a) * 68f);
                bool isGear = names[i] == gear;
                var ware = Ware(m, isGear ? gearArt : HelpArt.Item(names[i]), at, 44f, i * 1.3f, world);
                if (!isGear) continue;
                buyAt = at;
                // Once it's yours the shop has no more to sell you: the display goes.
                m.Fade(ware).At(WildBought - 0.01f, 1f).At(WildBought, 0f, DemoEase.Step);
            }

            scenery?.Invoke(world);

            // You, facing right all the way: by the shop, off across the island, and on a few steps more.
            var (you, body) = Walker(m, "You", HelpArt.You, WildStart, 80f, world);
            m.Face(body).At(0f, -1f);
            m.Move(you).At(WildWalkFrom, WildStart).At(WildWalkTo, WildWalkTap, DemoEase.Linear)
                .At(strollFrom, WildWalkTap).At(strollEnd, WildW(strollTo), DemoEase.Linear);
            m.Bob(body, 7f, 12f, s => (s > WildWalkFrom && s < WildWalkTo) || (s > strollFrom && s < strollEnd)
                || (s > WildLand && s < WildLand + 0.5f));   // and two happy hops when the gear lands

            // The gear drops in beside you with a bounce and a sparkle (HobbyCutscene), then it's in your paws.
            var beside = WildStart + new Vector2(48f, 0f);
            var high = beside + new Vector2(0f, 380f);
            var drop = m.Group("Gear", high);
            m.Pic("Pic", gearArt, Vector2.zero, 60f, drop);
            m.Move(drop).At(WildBought, high).At(WildLand - 0.1f, beside, DemoEase.In)
                .At(WildLand, beside + new Vector2(0f, 16f), DemoEase.Out).At(WildLand + 0.1f, beside, DemoEase.In)
                .At(WildStowed - 0.3f, beside).At(WildStowed, WildStart + new Vector2(22f, 30f));
            m.Size(drop).At(WildStowed - 0.3f, 1f).At(WildStowed, 0f, DemoEase.In);
            m.Fade(drop).At(WildBought - 0.01f, 0f).At(WildBought, 1f, DemoEase.Step);
            m.Sparkle(beside + new Vector2(0f, 30f), WildLand - 0.1f, UiKit.Sun, 6);

            // The counters at the top: the price comes off as you buy.
            var price = StoreData.Price(gear);
            var woodCount = m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "", WoodPill);
            var goldCount = m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "", GoldPill);
            m.Tally(woodCount, wood0, (WildBought, wood0 - price.x));
            m.Tally(goldCount, gold0, (WildBought, gold0 - price.y));
            var purchase = BuyCard(m, new Vector2(40f, 28f), gear, gearArt, WildCardFrom, WildBought);

            var ware0 = buyAt + new Vector2(0f, 20f);
            m.Hand().Show(0.35f, 2.2f)
                .At(0.35f, buyAt + new Vector2(130f, 110f)).At(0.8f, ware0).Tap(WildTapWare).At(1.1f, ware0)
                .At(1.65f, purchase).Tap(WildPay).At(1.95f, purchase).At(2.2f, purchase + new Vector2(70f, -60f));
            m.Hand().Show(3.1f, 3.9f)
                .At(3.1f, WildWalkTap + new Vector2(70f, 110f)).At(3.32f, WildWalkTap).Tap(WildTapGrass)
                .At(3.6f, WildWalkTap).At(3.9f, WildWalkTap + new Vector2(80f, 90f));
            return (world, you);
        }

        /// <summary>
        /// A bug as WildBug draws it, centred on its spot in <paramref name="parent"/>: it
        /// fades in, flits about its spot in lazy loops facing the way it's going, holds still from
        /// <paramref name="still"/> (tapped), and from <paramref name="escape"/> (if not negative)
        /// flies up and away, fading. Returns the bug's holder (to shrink into the net) and where it
        /// is at any moment.
        /// </summary>
        static (RectTransform holder, Func<float, Vector2> at) WildBug(HelpMovie m, Transform parent, Sprite art, Vector2 home,
            float size, float appear, float still, float escape)
        {
            var g = m.Group("Bug", home, parent);
            var pic = m.Shape("Pic", art, Vector2.zero, new Vector2(size, size), Color.white, g);
            pic.preserveAspect = true;
            Func<float, Vector2> flit = t => home + new Vector2(Mathf.Sin(t * 1.6f) * 40f, Mathf.Sin(t * 3f) * 18f + 14f);
            var away = new Vector2(150f, 230f);
            var picR = pic.rectTransform;
            m.Do(s =>
            {
                float t = Mathf.Min(s, still);
                if (escape >= 0f) t = Mathf.Min(t, escape);
                var p = flit(t);
                if (escape >= 0f && s > escape) p += away * DemoEase.In(Mathf.Clamp01((s - escape) / 1.1f));
                g.anchoredPosition = p;
                // The pictures face right: mirrored while it drifts left.
                float face = Mathf.Cos(t * 1.6f) < 0f ? -1f : 1f;
                var l = picR.localScale;
                picR.localScale = new Vector3(Mathf.Abs(l.x) * face, l.y, 1f);
            });
            var fade = m.Fade(pic).At(appear - 0.01f, 0f).At(appear, 0f).At(appear + 0.6f, 1f);
            if (escape >= 0f) fade.At(escape, 1f).At(escape + 1.1f, 0f);
            return (g, flit);
        }

        /// <summary>
        /// The butterfly net as WildBug.NetSwoop swings it: out of your paws at
        /// <paramref name="from"/>, raised, then swooped in an arc down over <paramref name="to"/>,
        /// a wiggle as the bug goes in, and lifted away fading. Returns the moment it lands.
        /// </summary>
        static float WildNetSwoop(HelpMovie m, Transform parent, Sprite art, Vector2 from, Vector2 to, float start)
        {
            float land = start + 0.1f + 0.22f;
            if (art == null) return land;
            // The handle (bottom left of the picture) in your paws; the hoop (top right) out along the reach.
            float aspect = art.rect.width / Mathf.Max(1f, art.rect.height);
            const float h = 100f;
            float w = h * aspect;
            var hoop = new Vector2(0.49f * w, 0.525f * h);
            float reach = Mathf.Clamp(Vector2.Distance(from, to), 40f, 165f);
            var pivot = m.Group("Net", from, parent);
            pivot.localScale = Vector3.one * (reach / Mathf.Max(1f, hoop.magnitude));
            var body = m.Shape("Body", art, Vector2.zero, new Vector2(w, h), Color.white, pivot);
            body.rectTransform.pivot = new Vector2(0.14f, 0.16f);
            body.rectTransform.anchoredPosition = Vector2.zero;

            float aim = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg - Mathf.Atan2(hoop.y, hoop.x) * Mathf.Rad2Deg;
            float raised = aim + (to.x >= from.x ? 80f : -80f);
            m.Turn(pivot).At(0f, raised).At(start + 0.1f, raised).At(land, aim, DemoEase.In)
                .At(land + 0.07f, aim + 8f).At(land + 0.14f, aim - 6f).At(land + 0.21f, aim + 4f).At(land + 0.28f, aim - 2f).At(land + 0.35f, aim)
                .At(land + 0.6f, raised, DemoEase.Out);
            m.Fade(body).At(start, 0f).At(start + 0.1f, 1f).At(land + 0.35f, 1f).At(land + 0.6f, 0f);
            return land;
        }

        /// <summary>
        /// A bird as WildBird draws it: fluttering down from above to its perch at
        /// <paramref name="land"/>, a little hop (turning about if <paramref name="turns"/>), and at
        /// <paramref name="leave"/> off it flies, up and away the way it's facing, fading. The
        /// pictures face left.
        /// </summary>
        static void WildBird(HelpMovie m, Transform parent, Sprite art, Vector2 perch, float size, float land, float fromSide,
            bool facingRight, float hop, bool turns, float leave)
        {
            var high = perch + new Vector2(fromSide, 380f);
            var g = m.Group("Bird", high, parent);
            var pic = m.Shape("Pic", art, Vector2.zero, new Vector2(size, size), Color.white, g);
            pic.preserveAspect = true;
            bool right = turns ? !facingRight : facingRight;
            m.Move(g).At(land, high).At(land + 1.1f, perch, DemoEase.Out)
                .At(hop, perch).At(hop + 0.12f, perch + new Vector2(0f, 9f), DemoEase.Out).At(hop + 0.25f, perch, DemoEase.In)
                .At(leave, perch).At(leave + 1.4f, perch + new Vector2(right ? 450f : -450f, 525f), DemoEase.In);
            var face = m.Face(pic).At(0f, facingRight ? -1f : 1f);
            if (turns) face.At(hop + 0.12f, right ? -1f : 1f, DemoEase.Step);
            m.Fade(pic).At(land - 0.01f, 0f).At(land, 1f, DemoEase.Step).At(leave, 1f).At(leave + 1.4f, 0f);
        }

        /// <summary>
        /// The card the game pops up for a catch (CardPopup): the screen dimmed, a parchment card with
        /// a pink border zooming up, the picture, the title, its rarity, "New to your journal!", the
        /// XP, and "tap to continue". Shown from <paramref name="from"/> until it's tapped at
        /// <paramref name="tap"/>. Returns where to tap.
        /// </summary>
        static Vector2 WildCard(HelpMovie m, Sprite picture, string title, int stars, string xp, float from, float tap)
        {
            var parchment = new Color(0.98f, 0.96f, 0.91f, 0.98f);
            var ink = new Color(0.22f, 0.16f, 0.11f);
            var words = new Color(0.30f, 0.24f, 0.18f);
            var g = m.Group("CardPopup", Vector2.zero);
            m.Shape("Dim", null, Vector2.zero, new Vector2(640f, 440f), new Color(0.08f, 0.06f, 0.05f, 0.55f), g);
            var card = m.Group("Card", new Vector2(0f, -4f), g);
            m.Shape("Paper", null, Vector2.zero, new Vector2(250f, 330f), parchment, card);
            m.Shape("Border", null, Vector2.zero, new Vector2(239f, 319f), new Color(0.86f, 0.56f, 0.62f), card);
            m.Shape("Inner", null, Vector2.zero, new Vector2(232f, 312f), parchment, card);
            if (picture != null)
                m.Shape("Picture", picture, new Vector2(0f, 62f), new Vector2(140f, 140f), Color.white, card).preserveAspect = true;

            var t = m.Label("Title", title, new Vector2(0f, -32f), 20f, ink, card);
            t.fontStyle = FontStyles.Bold;
            Fit(t, title, 218f);
            string rarity = HobbyData.Rarity(stars);
            Fit(m.Label("Rarity", rarity, new Vector2(0f, -60f), 15f, words, card), rarity, 210f);
            const string fresh = "<color=#2DB3A6><b>New to your journal!</b></color>";
            Fit(m.Label("New", fresh, new Vector2(0f, -80f), 15f, words, card), fresh, 210f);
            Fit(m.Label("Xp", xp, new Vector2(0f, -100f), 15f, words, card), xp, 210f);
            var more = m.Label("Tap", "tap to continue", new Vector2(0f, -146f), 11f, new Color(0.5f, 0.42f, 0.34f), card);
            more.fontStyle = FontStyles.Italic;
            Fit(more, "tap to continue", 200f);

            m.Size(card).At(from, 0.3f).At(from + 0.35f, 1f, DemoEase.Back);
            m.Fade(g).At(from - 0.01f, 0f).At(from, 1f, DemoEase.Step).At(tap + 0.05f, 1f).At(tap + 0.06f, 0f, DemoEase.Step);
            return new Vector2(0f, -40f);
        }

        /// <summary>The finger coming in to tap the card away.</summary>
        static void WildTapCard(HelpMovie m, Vector2 at, float tap)
        {
            m.Hand().Show(tap - 0.6f, tap + 0.3f)
                .At(tap - 0.6f, at + new Vector2(130f, -100f)).At(tap - 0.2f, at).Tap(tap)
                .At(tap + 0.15f, at).At(tap + 0.3f, at + new Vector2(130f, -100f));
        }

        /// <summary>A soft fade through the sea's blue as the movie goes round again, so the jump back to the shop doesn't jolt.</summary>
        static void WildDip(HelpMovie m, float length)
        {
            var dip = m.Shape("Dip", null, Vector2.zero, new Vector2(640f, 440f), HelpArt.Sea);
            dip.raycastTarget = false;
            m.Fade(dip).At(0f, 1f).At(0.3f, 0f).At(length - 0.35f, 0f).At(length, 1f);
        }
    }
}
