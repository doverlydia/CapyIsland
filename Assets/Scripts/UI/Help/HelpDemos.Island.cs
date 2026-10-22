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
        // ================================================================ getting gems

        /// <summary>
        /// Where gems come from, one real source after another, each ending with the gem counter
        /// counting up. A pear tree tapped: it shakes and every fruit comes down, and one of them is a
        /// treasure chest (FruitTree.RemoveFruit) that jumps down from its branch, settles, opens by
        /// itself and sends its gems up to the counter (TreasureChest). Then Gifts on the right: the
        /// Daily Gifts screen with the week's tiles, the first four ticked off and day 5 (the gems)
        /// glowing; Collect, and it's ticked too. Then the last missing fish of the journal leaps out of
        /// the water, and the card says the journal is complete, with its 40 gems and a hat. Last,
        /// Events (red dot): your prize from a contest that ended waits with its own Claim; claimed,
        /// the card says what you won.
        /// </summary>
        static void GemsMovie(HelpMovie m)
        {
            const float bFrom = 4.8f, cFrom = 8.5f, dFrom = 12.4f, L = 17.3f;
            m.Length = L;
            const int gems0 = 12;
            var gemArt = HelpArt.Resource(ResourceKind.Gem);
            var amber = UiKit.Hex("E3A92B");

            // ---- a pear tree, shaken: the fruit falls, and one of them is a chest
            // A chest settles for 15 frames and opens after 21, and its gems fly off 21 frames later.
            const float shake = 0.9f, landed = shake + 0.5f, opens = shake + 0.7f, pays = shake + 1.4f, fades = shake + 2.07f;
            var a = m.Group("PearTree", Vector2.zero);
            During(m, a, (0f, bFrom));
            m.Island("Island", new Vector2(0f, -35f), new Vector2(570f, 270f), a);
            m.Pic("Forest", HelpArt.Planted("Medium Pine Forest", "Small Pine Forest"), new Vector2(205f, -30f), 96f, a);
            var (_, youA) = Walker(m, "You", HelpArt.You, new Vector2(-200f, -128f), 84f, a);
            m.Face(youA).At(0f, -1f);
            var treeAt = new Vector2(10f, -118f);
            const float treeSize = 236f;
            var tree = m.Pic("Tree", HelpArt.Item("Pear Tree", "Apple Tree"), treeAt, treeSize, a);
            m.Turn(tree.transform).At(shake, 0f).At(shake + 0.08f, 5f).At(shake + 0.2f, -5f).At(shake + 0.32f, 4f)
                .At(shake + 0.44f, -3f).At(shake + 0.56f, 0f);
            // The tree's children are placed from its middle, 0.4 of its height above its feet.
            var treeMiddle = treeAt + new Vector2(0f, treeSize * 0.4f);
            var pear = GameArt.Instance != null && GameArt.Instance.pear != null ? GameArt.Instance.pear : HelpArt.Apple;
            Vector2[] hang = { new Vector2(-48f, 40f), new Vector2(-8f, 72f), new Vector2(36f, 52f), new Vector2(54f, 8f), new Vector2(-30f, -4f) };
            // Each lands up to 50 either side of the trunk and up to 60 in front of it.
            Vector2[] ground = { new Vector2(-62f, -150f), new Vector2(-24f, -170f), new Vector2(26f, -158f), new Vector2(64f, -166f), new Vector2(98f, -146f) };
            const int chestFruit = 2;
            for (int i = 0; i < hang.Length; i++)
            {
                // Ripe on the branch, dangling from its stem, until the tree is shaken.
                var onBranch = m.Pic("Pear", pear, hang[i], 30f, tree.transform);
                var stem = onBranch.rectTransform;
                float phase = i * 1.3f;
                m.Do(s => stem.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(s * 2.1f + phase) * 6f));
                m.Between(onBranch.gameObject, 0f, shake);
                if (i == chestFruit) continue;

                // Down it comes in a little hop, spinning, and lands upright as food.
                var top = treeMiddle + hang[i];
                var fallen = m.Pic("Fallen", pear, top, 30f, a);
                float down = shake + 0.45f;
                m.Move(fallen.transform).At(shake, top).At(shake + 0.15f, Vector2.Lerp(top, ground[i], 0.3f) + new Vector2(0f, 30f), DemoEase.Out)
                    .At(down, ground[i], DemoEase.In);
                m.Turn(fallen.transform).At(shake, 0f).At(down, i % 2 == 0 ? 160f : -150f, DemoEase.Linear).At(down + 0.01f, 0f, DemoEase.Step);
                m.Size(fallen.transform).At(down, 1f).At(down + 0.08f, 1.35f).At(down + 0.3f, 1f);
                m.Fade(fallen).At(shake - 0.01f, 0f).At(shake, 1f, DemoEase.Step);
            }
            // The chest jumps down from where its fruit hung, opens by itself, and fades once it has paid.
            var chestTop = treeMiddle + hang[chestFruit];
            var chestAt = ground[chestFruit];
            var shut = m.Pic("Chest", HelpArt.ChestShut, chestTop, 58f, a);
            m.Move(shut.transform).At(shake, chestTop).At(shake + 0.2f, Vector2.Lerp(chestTop, chestAt, 0.35f) + new Vector2(0f, 46f), DemoEase.Out)
                .At(landed, chestAt, DemoEase.In);
            m.Size(shut.transform).At(shake, 0.6f).At(shake + 0.2f, 1f).At(landed, 1f).At(landed + 0.08f, 1.15f).At(landed + 0.25f, 1f);
            m.Between(shut.gameObject, shake, opens);
            var chestOpen = m.Pic("ChestOpen", HelpArt.ChestOpen, chestAt, 58f, a);
            m.Between(chestOpen.gameObject, opens, fades + 0.7f);
            m.Size(chestOpen.transform).At(opens, 1f).At(opens + 0.1f, 1.2f).At(opens + 0.35f, 1f);
            m.Fade(chestOpen).At(fades, 1f).At(fades + 0.67f, 0f);

            // ---- the Daily Gifts: Gifts, day 5's gems, Collect
            const float giftTap = bFrom + 0.85f, giftsOpen = bFrom + 1.05f, collect = bFrom + 2.4f;
            var b = m.Group("DailyGifts", Vector2.zero);
            During(m, b, (bFrom, cFrom));
            IsleHome(m, b);
            var hudAt = new Vector2(258f, 62f);
            var giftButton = IsleHudButton(m, b, "Gifts", IconPainter.Gift, amber, hudAt);
            IsleHudButton(m, b, "Friends", IconPainter.Plane, UiKit.Hex("5B9BD5"), hudAt + new Vector2(0f, -84f));
            IsleHudButton(m, b, "Events", IconPainter.Trophy, amber, hudAt + new Vector2(0f, -168f));
            var giftDot = m.Shape("Dot", UiKit.Circle, new Vector2(21f, 21f), new Vector2(18f, 18f), UiKit.Danger, giftButton);
            m.Between(giftDot.gameObject, 0f, giftTap + 0.1f);
            m.Size(giftButton).At(giftTap - 0.05f, 1f).At(giftTap + 0.05f, 0.88f).At(giftTap + 0.2f, 1f);
            IsleScrim(m, b, giftsOpen, cFrom);
            var sheetAt = new Vector2(-14f, -22f);
            var sheetSize = new Vector2(500f, 336f);
            var gifts = IsleSheet(m, b, sheetAt, sheetSize, amber, IconPainter.Calendar, "Daily Gifts", giftsOpen, cFrom);

            // Seven tiles, four to a row: what each day brings (DailyGiftScreen). Days 1 to 4 are
            // collected; today is day 5, the gems, glowing.
            var week = DailyGifts.Week;
            int today = Array.FindIndex(week, x => x.Gems > 0) + 1;
            if (today < 1) today = 5;
            int dayGems = week[today - 1].Gems;
            RectTransform todayTile = null;
            for (int i = 0; i < week.Length; i++)
            {
                var w = week[i];
                int day = i + 1;
                var cell = new Vector2(-156f + (i % 4) * 104f, 60f - (i / 4) * 110f);
                var tile = m.Shape("Day" + day, UiKit.Rounded(16), cell, new Vector2(94f, 102f),
                    day == today ? UiKit.Hex("FFF1C9") : day == 7 ? UiKit.Hex("FDE8F0") : Color.white, gifts);
                tile.type = Image.Type.Sliced;
                string dayWords = "Day " + day;
                Fit(m.Label("Day", dayWords, new Vector2(0f, 35f), 15f, UiKit.InkSoft, tile.transform), dayWords, 86f);
                Sprite icon;
                string amount;
                bool both = w.Wood > 0 && w.Gold > 0;
                if (w.Outfit) { icon = IconPainter.Hanger; amount = "Outfit!"; }
                else if (w.Gems > 0) { icon = HelpArt.Resource(ResourceKind.Gem); amount = "+" + w.Gems; }
                else if (both) { icon = HelpArt.Resource(ResourceKind.Gold); amount = "+" + w.Wood + "/" + w.Gold; }
                else if (w.Gold > 0) { icon = HelpArt.Resource(ResourceKind.Gold); amount = "+" + w.Gold; }
                else { icon = HelpArt.Resource(ResourceKind.Wood); amount = "+" + w.Wood; }
                // Both: a little wood tucked behind the gold.
                if (both)
                    m.Shape("Icon2", HelpArt.Resource(ResourceKind.Wood), new Vector2(-14f, 4f), new Vector2(32f, 32f), Color.white, tile.transform).preserveAspect = true;
                m.Shape("Icon", icon, new Vector2(both ? 6f : 0f, 2f), new Vector2(44f, 44f), w.Outfit ? UiKit.Hex("E58FB0") : Color.white, tile.transform).preserveAspect = true;
                var sum = m.Label("Amount", amount, new Vector2(0f, -34f), 17f, UiKit.Ink, tile.transform);
                Fit(sum, amount, 86f);
                if (day <= today)
                {
                    var done = m.Shape("Done", UiKit.Rounded(16), Vector2.zero, new Vector2(94f, 102f),
                        new Color(UiKit.Leaf.r, UiKit.Leaf.g, UiKit.Leaf.b, 0.82f), tile.transform);
                    done.type = Image.Type.Sliced;
                    m.Shape("Tick", IconPainter.Check, Vector2.zero, new Vector2(44f, 44f), Color.white, done.transform).preserveAspect = true;
                    if (day == today) m.Between(done.gameObject, collect, L);
                }
                if (day == today) todayTile = tile.rectTransform;
            }
            // Today's tile breathes until it's collected, then gives a happy wobble.
            if (todayTile != null)
                m.Do(s =>
                {
                    float grow = 1f;
                    if (s >= giftsOpen && s < collect) grow = 1f + 0.03f * (1f - Mathf.Cos((s - giftsOpen) * Mathf.PI * 2f / 1.2f));
                    else if (s >= collect && s < collect + 0.5f)
                    {
                        float since = (s - collect) / 0.5f;
                        grow = 1f + 0.2f * Mathf.Sin(since * Mathf.PI * 3f) * (1f - since);
                    }
                    todayTile.localScale = new Vector3(grow, grow, 1f);
                });
            // The big button: Collect day 5, then a quiet grey Come back tomorrow.
            var collectAt = new Vector2(0f, -132f);
            var collectButton = IsleIconButton(m, gifts, collectAt, new Vector2(300f, 54f), UiKit.Primary, IconPainter.Gift, "Collect day " + today, Color.white);
            var tomorrow = IsleIconButton(m, gifts, collectAt, new Vector2(300f, 54f), UiKit.SurfaceSunk, IconPainter.Gift, "Come back tomorrow", UiKit.InkSoft);
            m.Between(collectButton.gameObject, 0f, collect + 0.15f);
            m.Between(tomorrow.gameObject, collect + 0.15f, L);
            var collectStage = sheetAt + collectAt;

            // ---- a full journal: the last fish you were missing
            const float leap = cFrom + 0.7f, journalFrom = cFrom + 1.25f, journalTap = cFrom + 2.75f;
            var c = m.Group("Journal", Vector2.zero);
            During(m, c, (cFrom, dFrom));
            m.Island("Island", new Vector2(0f, 80f), new Vector2(600f, 230f), c);
            m.Pic("Forest", HelpArt.Planted("Medium Pine Forest", "Small Pine Forest"), new Vector2(200f, 50f), 90f, c);
            // You at the water's edge, the rod out over the sea and the line down to the bobber.
            var feet = new Vector2(-90f, -52f);
            const float rodLength = 110f;
            var rodBase = feet + new Vector2(30f, 26f);
            var rod = m.Shape("Rod", null, rodBase, new Vector2(6f, rodLength), UiKit.Hex("8A5A32"), c);
            rod.rectTransform.pivot = new Vector2(0.5f, 0f);
            rod.rectTransform.anchoredPosition = rodBase;
            rod.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -58f);
            var angler = m.Pic("You", HelpArt.You, feet, 96f, c);
            m.Face(angler).At(0f, -1f);
            var tip = rodBase + (Vector2)(Quaternion.Euler(0f, 0f, -58f) * new Vector3(0f, rodLength, 0f));
            var bobAt = new Vector2(90f, -140f);
            var line = m.Shape("Line", null, (tip + bobAt) * 0.5f, new Vector2(2f, Vector2.Distance(tip, bobAt)), new Color(1f, 1f, 1f, 0.9f), c);
            line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, bobAt - tip));
            var bobber = m.Shape("Bobber", UiKit.Circle, bobAt, new Vector2(16f, 16f), UiKit.Danger, c);
            m.Do(s => bobber.rectTransform.anchoredPosition = bobAt
                + new Vector2(0f, Mathf.Sin(s * 3f) * 3f + (s > leap - 0.45f && s < leap ? Mathf.Sin(s * 40f) * 4f : 0f)));
            m.Between(line.gameObject, 0f, leap);
            m.Between(bobber.gameObject, 0f, leap);
            var fish = m.Pic("Catch", HelpArt.Picture("Fish_salmon"), bobAt, 90f, c);
            m.Move(fish.transform).At(leap, bobAt + new Vector2(0f, -30f)).At(leap + 0.35f, new Vector2(0f, -40f), DemoEase.Out);
            m.Size(fish.transform).At(leap, 0.4f).At(leap + 0.3f, 1f, DemoEase.Back);
            m.Turn(fish.transform).At(leap + 0.3f, 0f).At(leap + 0.42f, -8f).At(leap + 0.54f, 6f).At(leap + 0.66f, 0f);
            m.Fade(fish).At(leap - 0.01f, 0f).At(leap, 1f, DemoEase.Step).At(journalFrom, 1f).At(journalFrom + 0.2f, 0f);

            // ---- a contest prize: Events, Claim
            const float eventsTap = dFrom + 0.85f, eventsOpen = dFrom + 1.05f, claim = dFrom + 2.2f, resultFrom = dFrom + 2.35f, resultTap = dFrom + 3.75f;
            var d = m.Group("Events", Vector2.zero);
            During(m, d, (dFrom, L));
            IsleHome(m, d);
            IsleHudButton(m, d, "Gifts", IconPainter.Gift, amber, hudAt);
            IsleHudButton(m, d, "Friends", IconPainter.Plane, UiKit.Hex("5B9BD5"), hudAt + new Vector2(0f, -84f));
            var eventsAt = hudAt + new Vector2(0f, -168f);
            var eventsButton = IsleHudButton(m, d, "Events", IconPainter.Trophy, amber, eventsAt);
            var eventsDot = m.Shape("Dot", UiKit.Circle, new Vector2(21f, 21f), new Vector2(18f, 18f), UiKit.Danger, eventsButton);
            m.Between(eventsDot.gameObject, 0f, eventsTap + 0.1f);
            m.Size(eventsButton).At(eventsTap - 0.05f, 1f).At(eventsTap + 0.05f, 0.88f).At(eventsTap + 0.2f, 1f);
            IsleScrim(m, d, eventsOpen, L - 0.3f);
            // The Events screen, the same size and place as in the contest page's movie.
            var eventsSheetAt = new Vector2(0f, -27f);
            var board = IsleSheet(m, d, eventsSheetAt, new Vector2(560f, 340f), amber, IconPainter.Trophy, "Events", eventsOpen, L - 0.3f);

            // The prize waiting at the top of the list (EventsScreen.Prizes): a cream card, the
            // contest's colour in a disc with the trophy, which contest and when in bold, how you did
            // under it, and Claim (sparkle and word) breathing at the right.
            const int score = 12;
            int won = GameEvents.PrizeGems[1];
            var prizeAt = new Vector2(0f, 72f);
            var prize = m.Shape("Prize", UiKit.Rounded(20), prizeAt, new Vector2(520f, 84f), UiKit.Hex("FFF4D6"), board);
            prize.type = Image.Type.Sliced;
            var disc = m.Shape("Disc", UiKit.Circle, new Vector2(-218f, 0f), new Vector2(56f, 56f), UiKit.Hex("2DA39A"), prize.transform);
            m.Shape("Icon", IconPainter.Trophy, Vector2.zero, new Vector2(34f, 34f), Color.white, disc.transform).preserveAspect = true;
            string month = Loc.T("Oct");
            string dates = Loc.Current == Language.Hebrew ? "19 – 21 " + month : month + " 19 – 21";
            LeftLabel(m, prize.transform, Loc.T(EventsScreen.ContestName(ContestKind.Fish)) + " · " + dates, new Vector2(-180f, 14f), 17f, UiKit.Ink, 270f);
            var how = LeftLabel(m, prize.transform, "You came second with " + score + ".", new Vector2(-180f, -13f), 14f, UiKit.InkSoft, 270f);
            if (UiKit.BodyFont != null) how.font = UiKit.BodyFont;
            var claimAt = new Vector2(190f, 0f);
            var claimButton = IsleIconButton(m, prize.transform, claimAt, new Vector2(124f, 46f), UiKit.Primary, IconPainter.Sparkle, "Claim", Color.white);
            var beat = m.Size(claimButton).At(0f, 1f);
            for (float t = eventsOpen; t + 0.6f < claim; t += 1.2f) beat.At(t, 1f).At(t + 0.6f, 1.05f);
            beat.At(claim - 0.1f, 1f).At(claim, 0.92f).At(claim + 0.15f, 1f);
            m.Between(prize.gameObject, 0f, claim + 0.1f);
            // Under it, the next contest as the Events list really shows it (the contest movie's page:
            // framed banner, ribbon, how long it runs, who wins, the prizes). Claimed, the prize card
            // leaves the list and the page moves up into its place.
            var view = m.Group("View", new Vector2(0f, FixViewY), board);
            view.sizeDelta = new Vector2(540f, 290f);
            view.gameObject.AddComponent<RectMask2D>();
            var list = m.Group("List", new Vector2(0f, FixListTop), view);
            var rest = m.Group("Rest", new Vector2(0f, -98f), list);
            m.Move(rest).At(claim + 0.1f, new Vector2(0f, -98f)).At(claim + 0.11f, Vector2.zero, DemoEase.Step);
            FixContestPage(m, rest, ContestKind.Bugs, "3 days", null, false);
            var claimStage = eventsSheetAt + prizeAt + claimAt;

            // ---- the counters, and the gems arriving
            m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "1240", WoodPill);
            m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "860", GoldPill);
            var gemCount = m.Pill("Gems", gemArt, "", GemPill);

            const int chestGems = 5;   // a pear tree's gem chest holds 1, 5, 10 or 20
            m.Sparkle(chestAt + new Vector2(0f, 30f), opens, UiKit.Sun, 6);
            float fromChest = m.Ping(gemArt, "+" + chestGems, chestAt + new Vector2(0f, 70f), PillIcon(GemPill), pays);
            Splash(m, bobAt, leap);

            int g1 = gems0 + chestGems, g2 = g1 + dayGems, g3 = g2 + JournalRewards.Gems, g4 = g3 + won;
            float fromGift = collect + 0.05f, fromJournal = journalTap + 0.1f, fromContest = claim + 0.05f;   // Claim pays at once (GameEvents.Claim); the card only tells you
            m.Tally(gemCount, gems0, (fromChest, g1), (fromGift, g2), (fromJournal, g3), (fromContest, g4));
            foreach (var arrives in new[] { fromChest, fromGift, fromJournal, fromContest }) m.Sparkle(PillIcon(GemPill), arrives, UiKit.Sun, 5);

            // The cards the game pops up (JournalRewards → MobileHud.AnnounceJournal; EventsScreen.ShowResult).
            var hat = JournalRewards.Hat(CapyIsland.Game.Hobby.Fishing);
            var hatArt = hat != null ? Wardrobe.Art(hat) : null;
            string journalBody = "You found every fish on the islands! Here are " + JournalRewards.Gems + " gems"
                                 + (hat != null ? ", and the <b>" + hat.Name + "</b> is waiting in your wardrobe." : ".");
            var journalCard = IsleCardPop(m, hatArt != null ? hatArt : IconPainter.Trophy, "The fish journal is complete!", journalBody, journalFrom, journalTap);
            var resultCard = IsleCardPop(m, IconPainter.Trophy, "Fishing Contest: you came second!",
                "You finished second with " + score + ". Here are " + won + " gems.", resultFrom, resultTap);

            IsleDip(m, L, bFrom, cFrom, dFrom);

            // The fingers, one stretch at a time.
            m.Hand().Show(0.3f, 1.6f)
                .At(0.3f, treeMiddle + new Vector2(190f, -40f)).At(shake - 0.15f, treeMiddle + new Vector2(0f, 40f)).Tap(shake)
                .At(1.6f, treeMiddle + new Vector2(190f, -40f));
            m.Hand().Show(bFrom + 0.3f, collect + 0.5f)
                .At(bFrom + 0.3f, hudAt + new Vector2(-130f, -60f)).At(giftTap - 0.2f, hudAt).Tap(giftTap)
                .At(collect - 0.3f, collectStage).Tap(collect).At(collect + 0.5f, collectStage + new Vector2(110f, -40f));
            IsleTapCard(m, journalCard, journalTap);
            m.Hand().Show(dFrom + 0.3f, claim + 0.4f)
                .At(dFrom + 0.3f, eventsAt + new Vector2(-130f, 60f)).At(eventsTap - 0.2f, eventsAt).Tap(eventsTap)
                .At(claim - 0.3f, claimStage).Tap(claim).At(claim + 0.4f, claimStage + new Vector2(60f, -90f));
            IsleTapCard(m, resultCard, resultTap);
        }

        // ================================================================ unlocking shops

        /// <summary>
        /// The shops' chain, as you walk it. At the General Store, tap the Bronze Store it sets out,
        /// Purchase (only one of each shop, so its ware is gone), and it pops into your trail; drag it
        /// out and plant it: it sets its own wares out in a half circle, the Silver Store among them,
        /// faded, because a new shop's price is worked out from what you have and earn the moment it
        /// goes on show (StoreData.Price), so it always waits for a little more gold. Walk over while
        /// the gold comes in; once you can afford it, it's solid: tap it, Purchase, plant it, and the
        /// Silver Store opens with its wares, the Steel Store among them.
        /// </summary>
        static void ShopsMovie(HelpMovie m)
        {
            const float L = 12.9f;
            m.Length = L;
            const float tap1 = 0.95f, card1 = 1.15f, buy1 = 1.85f, bought1 = 2.0f, grab1 = 2.8f, drop1 = 3.8f;
            const float walkTap = 5.15f, panFrom = 5.3f, panTo = 6.7f;
            float[] income = { 5.9f, 6.6f, 7.3f };
            const float tap2 = 7.95f, card2 = 8.15f, buy2 = 8.85f, bought2 = 9.0f, grab2 = 9.8f, drop2 = 10.8f;
            // The camera follows you along the island, one shop over.
            var pan = new Vector2(-315f, 0f);

            // The island's purse in this little story. The Bronze Store has a set price; the Silver
            // and Steel Stores' are worked out the moment they go on show (StoreData.Price), from
            // your best minute's work and what you have then, and stay put.
            const int wood0 = 1500, gold0 = 1300, gems0 = 12, woodPerMinute = 150, goldPerMinute = 45;
            var bronze = IsleListed("Bronze Store");
            int wood1 = wood0 - bronze.x, gold1 = gold0 - bronze.y;
            var silver = new Vector3Int(2 * woodPerMinute + gold1, 2 * goldPerMinute + gold1 + 10, 0);
            int[] golds = { gold1 + 40, silver.y - 5, silver.y + 40 };
            int wood2 = wood1 - silver.x, gold2 = golds[2] - silver.y;
            var steel = new Vector3Int(6 * woodPerMinute + gold2 / 2, 6 * goldPerMinute + wood2 / 2, 0);
            Vector3Int PriceOf(string id) => id == "Silver Store" ? silver : id == "Steel Store" ? steel : IsleListed(id);
            // A ware you can't afford yet stands at half strength (SaleDisplay).
            float Shown(string id, int haveWood, int haveGold) =>
                StoreData.IsDynamic(id) && id != "Silver Store" && id != "Steel Store" ? 0.5f
                : IsleAffords(PriceOf(id), haveWood, haveGold, gems0) ? 1f : 0.5f;

            var world = m.Group("World", Vector2.zero);
            m.Move(world).At(panFrom, Vector2.zero).At(panTo, pan, DemoEase.InOut);
            m.Island("Island", new Vector2(160f, -40f), new Vector2(1240f, 300f), world);

            // The General Store and its wares; the Bronze Store's is the last, due west.
            var general = new Vector2(-165f, -10f);
            m.Pic("General", HelpArt.Item("General Store"), general, 125f, world);
            var bronzeWare = general + new Vector2(-108f, 0f);
            foreach (var w in IsleWares(m, world, "general_store", general))
            {
                if (w.Name != "Bronze Store") continue;
                bronzeWare = w.At;
                // There's only ever one of each shop: bought, it's sold out and its ware goes.
                m.Fade(w.Pic).At(bought1, 1f).At(bought1 + 0.3f, 0f);
            }

            // The Bronze Store, planted, setting out its wares (StoreBuilding.Layout).
            var bronzeAt = new Vector2(150f, -15f);
            var bronzeArt = HelpArt.Item("Bronze Store");
            IsleGrowIn(m, m.Pic("Bronze", bronzeArt, bronzeAt, 125f, world), drop1);
            var silverWare = bronzeAt + new Vector2(-108f, 0f);
            var bronzeWares = IsleWares(m, world, "bronze_store", bronzeAt);
            for (int i = 0; i < bronzeWares.Length; i++)
            {
                var w = bronzeWares[i];
                IslePop(m, w.Pic, drop1 + 0.3f + i * 0.07f);
                if (w.Name == "Silver Store")
                {
                    silverWare = w.At;
                    m.Fade(w.Pic).At(0f, 0.5f).At(income[2], 0.5f).At(income[2] + 0.2f, 1f).At(bought2, 1f).At(bought2 + 0.3f, 0f);
                }
                else m.Fade(w.Pic).At(0f, Shown(w.Name, wood1, gold1));
            }

            // The Silver Store, one shop further along, and its wares.
            var silverAt = bronzeAt - pan;
            var silverArt = HelpArt.Item("Silver Store");
            IsleGrowIn(m, m.Pic("Silver", silverArt, silverAt, 125f, world), drop2);
            var silverWares = IsleWares(m, world, "silver_store", silverAt);
            for (int i = 0; i < silverWares.Length; i++)
            {
                var w = silverWares[i];
                IslePop(m, w.Pic, drop2 + 0.3f + i * 0.07f);
                m.Fade(w.Pic).At(0f, Shown(w.Name, wood2, gold2));
            }

            // You, facing along the island; the camera keeps you where you are as you walk.
            var (_, youBody) = Walker(m, "You", HelpArt.You, new Vector2(-75f, -150f), 74f);
            m.Face(youBody).At(0f, -1f);
            m.Bob(youBody, 6f, 12f, s => s > panFrom && s < panTo);
            var trail = new Vector2(-135f, -162f);
            var silverStage = silverAt + pan;
            IsleCarried(m, bronzeArt, trail, bronzeAt, bought1, grab1, drop1);
            IsleCarried(m, silverArt, trail, silverStage, bought2, grab2, drop2);
            m.Sparkle(bronzeAt + new Vector2(0f, 40f), drop1, UiKit.Sun, 5);
            m.Sparkle(silverStage + new Vector2(0f, 40f), drop2, UiKit.Sun, 5);

            // The counters: each shop's price comes off, and the gold comes in while you wait.
            var woodCount = m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "", WoodPill);
            var goldCount = m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "", GoldPill);
            m.Pill("Gems", HelpArt.Resource(ResourceKind.Gem), gems0.ToString(), GemPill);
            m.Tally(woodCount, wood0, (bought1, wood1), (bought2, wood2));
            m.Tally(goldCount, gold0, (bought1, gold1), (income[0], golds[0]), (income[1], golds[1]), (income[2], golds[2]), (bought2, gold2));

            var purchase1 = IsleBuyCard(m, new Vector2(40f, 28f), "Bronze Store", bronzeArt, bronze, card1, bought1);
            var purchase2 = IsleBuyCard(m, new Vector2(60f, 30f), "Silver Store", silverArt, silver, card2, bought2);

            IsleDip(m, L);

            var walkTo = new Vector2(240f, -150f);
            var silverTap = silverWare + pan;
            m.Hand().Show(0.35f, drop1 + 0.4f)
                .At(0.35f, bronzeWare + new Vector2(130f, 110f)).At(tap1 - 0.15f, bronzeWare + new Vector2(0f, 20f)).Tap(tap1)
                .At(buy1 - 0.2f, purchase1).Tap(buy1)
                .At(grab1 - 0.2f, trail + new Vector2(0f, 20f)).Press(grab1).At(grab1, trail + new Vector2(0f, 20f))
                .At(drop1, bronzeAt + new Vector2(0f, 20f), DemoEase.Linear).Lift(drop1 + 0.05f)
                .At(drop1 + 0.4f, bronzeAt + new Vector2(100f, 110f));
            m.Hand().Show(walkTap - 0.45f, walkTap + 0.35f)
                .At(walkTap - 0.45f, walkTo + new Vector2(40f, 100f)).At(walkTap - 0.1f, walkTo).Tap(walkTap)
                .At(walkTap + 0.35f, walkTo + new Vector2(40f, 100f));
            m.Hand().Show(tap2 - 0.5f, drop2 + 0.4f)
                .At(tap2 - 0.5f, silverTap + new Vector2(130f, 90f)).At(tap2 - 0.15f, silverTap + new Vector2(0f, 20f)).Tap(tap2)
                .At(buy2 - 0.2f, purchase2).Tap(buy2)
                .At(grab2 - 0.2f, trail + new Vector2(0f, 20f)).Press(grab2).At(grab2, trail + new Vector2(0f, 20f))
                .At(drop2, silverStage + new Vector2(0f, 20f), DemoEase.Linear).Lift(drop2 + 0.05f)
                .At(drop2 + 0.4f, silverStage + new Vector2(100f, 110f));
        }

        // ================================================================ pests and bombs

        /// <summary>
        /// Pests, as the game has them. Two guinea pigs nibble a planted posie garden, little flower
        /// after little flower, each one shrinking bite by bite and gone for good. The Bronze Store
        /// sells the Shoo Bomb: tap it, Purchase, drag it from your trail and plant it right beside
        /// them. Its fuse burns three seconds, throbbing faster and redder (Bomb.Fizz), then it goes
        /// off: the guinea pigs vanish in a puff and the flowers that are left stay (a Shoo Bomb
        /// touches nothing else). Then a zombie capybara wrecking a little tree; tap it, Pay 20 Gems,
        /// it has its warm bath, and comes out cured into your trail.
        /// </summary>
        static void PestsMovie(HelpMovie m)
        {
            // The garden: buying the Shoo Bomb and planting it.
            const float tap = 2.0f, cardFrom = 2.2f, buy = 2.9f, bought = 3.05f, grab = 3.8f, drop = 4.7f;
            const float boom = drop + 3f;   // the fuse burns 90 frames
            const float zFrom = boom + 0.9f;
            // The zombie capybara: crawling up, three blows, tapped, paid, bathed, and in your trail.
            const float crawl = zFrom + 0.6f, zTap = zFrom + 2.15f, talkFrom = zFrom + 2.35f, payTap = zFrom + 3.65f, paid = payTap + 0.15f;
            const float bathFrom = paid + 0.35f, bathTo = bathFrom + 1.6f, L = bathTo + 1.3f;
            float[] blows = { zFrom + 0.85f, zFrom + 1.3f, zFrom + 1.75f };
            m.Length = L;

            // ---- the garden
            var garden = m.Group("Garden", Vector2.zero);
            During(m, garden, (0f, zFrom));
            m.Island("Island", new Vector2(0f, -35f), new Vector2(570f, 270f), garden);
            var shop = new Vector2(-175f, -5f);
            m.Pic("Store", HelpArt.Item("Bronze Store"), shop, 120f, garden);
            var bombWare = shop + new Vector2(108f, 0f);
            foreach (var w in IsleWares(m, garden, "bronze_store", shop))
                if (w.Name == "Deer Bomb") bombWare = w.At;

            // A posie garden in the ground: the big flower and its little ones round it.
            var posie = HelpArt.Planted("Medium Posie Garden", "Small Posie Garden");
            var bedAt = new Vector2(95f, -80f);
            Vector2[] spots =
            {
                new Vector2(150f, -62f), new Vector2(138f, -112f), new Vector2(88f, -126f), new Vector2(35f, -104f),
                new Vector2(45f, -60f), new Vector2(192f, -96f), new Vector2(45f, -140f), new Vector2(140f, -150f),
            };
            float[] scales = { 0.62f, 0.6f, 0.64f, 0.58f, 0.6f, 0.62f, 0.6f, 0.58f };
            var kids = IsleBed(m, garden, posie, bedAt, 70f, spots, scales);

            // The guinea pigs: in from the east to the nearest little flower, nibble it away (a sixth of
            // a flower a bite), on to the next. Whatever they finish is simply gone. When the bomb goes
            // off they vanish in a puff, mid-meal.
            var pigArt = HelpArt.Item("Deer");
            void Pig(Vector2 enter, params (int kid, float reach, float[] bites)[] meals)
            {
                var (holder, body) = Walker(m, "GuineaPig", pigArt, enter, 50f, garden);
                var move = m.Move(holder).At(0f, enter);
                var lean = m.Turn(body).At(0f, 0f);
                var walks = new List<Vector2>();
                var last = enter;
                float leave = 0.1f;
                foreach (var (kid, reach, bites) in meals)
                {
                    var stand = spots[kid] + new Vector2(34f, -3f);
                    move.At(leave, last).At(reach, stand, DemoEase.Linear);
                    walks.Add(new Vector2(leave, reach));
                    foreach (var h in bites) lean.At(h - 0.1f, 0f).At(h, 9f, DemoEase.In).At(h + 0.16f, 0f);
                    Bitten(kids[kid].size, scales[kid], bites);
                    Rock(m, kids[kid].pic, bites);
                    last = stand;
                    leave = bites[bites.Length - 1] + 0.25f;
                }
                m.Bob(body, 4f, 14f, s =>
                {
                    foreach (var span in walks) if (s > span.x && s < span.y) return true;
                    return false;
                });
                m.Size(holder).At(boom + 0.05f, 1f).At(boom + 0.41f, 0f, DemoEase.In);
                IslePoof(m, last + new Vector2(0f, 22f), boom + 0.05f);
            }
            // (A last bite past the end of the loop leaves that flower half eaten.)
            Pig(new Vector2(330f, -45f),
                (0, 0.8f, new[] { 1.0f, 1.5f, 2.0f, 2.5f }),
                (4, 3.3f, new[] { 3.5f, 4.0f, 4.5f, 5.0f }),
                (3, 5.6f, new[] { 5.9f, 6.5f, 7.1f, 99f }));
            Pig(new Vector2(330f, -130f),
                (5, 1.0f, new[] { 1.25f, 1.75f, 2.25f, 2.75f }),
                (1, 3.35f, new[] { 3.6f, 4.1f, 4.6f, 5.1f }),
                (2, 5.7f, new[] { 6.05f, 6.65f, 99f, 100f }));

            // You, with your trail behind you.
            var (_, youP) = Walker(m, "You", HelpArt.You, new Vector2(-40f, -150f), 74f, garden);
            m.Face(youP).At(0f, -1f);
            var trail = new Vector2(-100f, -162f);

            // The Shoo Bomb, planted right beside them: the fuse fizzes, quicker and redder, sparks
            // fly off the top, and then it goes off (Bomb).
            var bombArt = HelpArt.Item("Deer Bomb", "Bomb");
            var bombAt = new Vector2(128f, -36f);
            var bomb = m.Pic("ShooBomb", bombArt, bombAt, 46f, garden);
            m.Between(bomb.gameObject, drop, boom + 0.4f);
            var bombShape = bomb.rectTransform;
            m.Do(s =>
            {
                if (s < drop || s >= boom + 0.4f) return;
                if (s < boom)
                {
                    float burnt = (s - drop) / (boom - drop);
                    float throb = Mathf.Abs(Mathf.Sin(s * Mathf.Lerp(6f, 30f, burnt)));
                    float swell = 1f + throb * Mathf.Lerp(0.04f, 0.16f, burnt);
                    bombShape.localScale = new Vector3(swell, 2f - swell, 1f);
                    bomb.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.45f), throb * burnt);
                }
                else
                {
                    float gone = Mathf.Clamp01((s - boom) / 0.35f);
                    float big = Mathf.Lerp(1f, 2.2f, DemoEase.Out(gone));
                    bombShape.localScale = new Vector3(big, big, 1f);
                    bomb.color = new Color(1f, 1f, 1f, 1f - gone);
                }
            });
            var flash = m.Shape("Boom", IconPainter.Glow, bombAt + new Vector2(0f, 20f), new Vector2(160f, 160f), Color.white, garden);
            m.Size(flash.transform).At(boom, 0.2f).At(boom + 0.45f, 2.6f, DemoEase.Out);
            m.Fade(flash).At(boom - 0.01f, 0f).At(boom, 1f, DemoEase.Step).At(boom + 0.5f, 0f);
            // The camera gives a little shake.
            m.Move(garden).At(boom, Vector2.zero).At(boom + 0.05f, new Vector2(7f, -4f)).At(boom + 0.12f, new Vector2(-6f, 4f))
                .At(boom + 0.2f, new Vector2(4f, -2f)).At(boom + 0.3f, new Vector2(-2f, 1f)).At(boom + 0.4f, Vector2.zero);
            var fuse = bombAt + new Vector2(10f, 40f);
            foreach (var spark in new[] { 0.3f, 0.9f, 1.5f, 2.0f, 2.4f, 2.7f }) m.Sparkle(fuse, drop + spark, UiKit.Sun, 3);
            m.Sparkle(bombAt + new Vector2(0f, 24f), boom, Color.white, 10);

            // ---- a zombie capybara
            var z = m.Group("Zombie", Vector2.zero);
            During(m, z, (zFrom, bathFrom), (bathTo, L));
            m.Island("Island", new Vector2(0f, -30f), new Vector2(570f, 290f), z);
            var pine = HelpArt.Planted("Medium Pine Forest", "Small Pine Forest");
            m.Pic("Forest", pine, new Vector2(232f, -22f), 104f, z);
            m.Pic("Pine", pine, new Vector2(262f, -96f), 104f * 0.62f, z);
            m.Pic("Pine", pine, new Vector2(200f, -132f), 104f * 0.58f, z);
            var victimAt = new Vector2(150f, -82f);
            var victim = m.Pic("Pine", pine, victimAt, 104f, z);
            var victimSize = m.Size(victim.transform).At(0f, 0.6f);
            // A blow takes 0.2 off a tree: three, and the little tree is gone.
            Bitten(victimSize, 0.6f, blows);
            Rock(m, victim, blows);
            var (_, youZ) = Walker(m, "You", HelpArt.You, new Vector2(-200f, -120f), 76f, z);
            m.Face(youZ).At(0f, -1f);
            var zTrail = new Vector2(-258f, -134f);

            var zombieArt = HelpArt.Item("Monster");
            var zAt = victimAt + new Vector2(-48f, -6f);
            var (zombie, zBody) = Walker(m, "Zombie", zombieArt, zAt + new Vector2(-34f, -4f), 64f, z);
            m.Move(zombie).At(zFrom, zAt + new Vector2(-34f, -4f)).At(crawl, zAt, DemoEase.Linear);
            m.Face(zBody).At(0f, -1f);
            m.Bob(zBody, 3f, 8f, s => s > zFrom && s < crawl);
            var zLean = m.Turn(zBody).At(0f, 0f);
            foreach (var h in blows) zLean.At(h - 0.1f, 0f).At(h, -9f, DemoEase.In).At(h + 0.16f, 0f);
            m.Between(zombie.gameObject, 0f, paid + 0.1f);
            m.Sparkle(zAt + new Vector2(0f, 40f), paid, Color.white, 8);

            // Tapped, it speaks (MonsterAnimal.Tap): its picture, its name, the line, Pay 20 Gems or Cancel.
            var talkAt = new Vector2(0f, -95f);
            var talk = IslePanel(m, z, talkAt, new Vector2(584f, 202f), null, talkFrom, paid);
            m.Shape("Frame", UiKit.Circle, new Vector2(-232f, 38f), new Vector2(96f, 96f), UiKit.SurfaceSunk, talk);
            m.Shape("Face", zombieArt, new Vector2(-232f, 38f), new Vector2(70f, 70f), Color.white, talk).preserveAspect = true;
            LeftLabel(m, talk, StoryText.Name("Monster"), new Vector2(-170f, 70f), 26f, UiKit.PrimaryDark, 380f);
            IslePara(m, talk, "Grrrnnh... so cold... (A warm bath costs 20 gems. Afterwards it'll happily work for you.)",
                new Vector2(-170f, 50f), new Vector2(400f, 64f), 16f, UiKit.Ink);
            var payAt = new Vector2(-40f, -58f);
            WordButton(m, talk, payAt, new Vector2(230f, 52f), UiKit.Primary, "Pay 20 Gems", Color.white);
            WordButton(m, talk, new Vector2(170f, -58f), new Vector2(160f, 52f), UiKit.SurfaceSunk, "Cancel", UiKit.Ink);

            // Cured, warm, towel on its head, and in your trail.
            var happyArt = HobbyData.Art("Monster_Happy");
            if (happyArt == null) happyArt = zombieArt;
            var cured = m.Pic("Cured", happyArt, zTrail, 60f, z);
            m.Face(cured).At(0f, -1f);
            m.Between(cured.gameObject, bathTo, L);
            m.Size(cured.transform).At(bathTo + 0.1f, 0.3f).At(bathTo + 0.4f, 1f, DemoEase.Back);

            // ---- the warm bath (BathCutscene), just a glimpse: soaking to its chin as the cold melts away
            var bath = m.Group("Bath", Vector2.zero);
            During(m, bath, (bathFrom, bathTo));
            var bathArt = Resources.Load<Sprite>("Cutscenes/Cutscene_Bath");
            if (bathArt != null)
            {
                float aspect = bathArt.rect.width / bathArt.rect.height;
                float tall = Mathf.Max(400f, 600f / aspect);
                m.Shape("Backdrop", bathArt, Vector2.zero, new Vector2(tall * aspect, tall), Color.white, bath);
            }
            else m.Shape("Backdrop", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("8E9BD6"), bath);
            var inPool = new Vector2((0.45f - 0.5f) * 600f, (0.22f - 0.5f) * 400f);
            float waterY = (0.31f - 0.5f) * 400f;
            var surface = new Vector2(0f, waterY + 160f);
            var water = m.Group("Water", surface, bath);
            water.sizeDelta = new Vector2(640f, 320f);
            water.gameObject.AddComponent<RectMask2D>();
            var soaking = m.Pic("Soaking", zombieArt, inPool - surface + new Vector2(0f, -12f), 100f, water);
            m.Face(soaking).At(0f, -1f);
            var cold = new Color(0.72f, 0.82f, 0.95f);
            var warm = new Color(1f, 0.88f, 0.82f);
            var warming = new DemoKeys().At(bathFrom + 0.3f, 0f).At(bathTo - 0.4f, 1f);
            m.Do(s => soaking.color = Color.Lerp(cold, warm, warming.Eval(s).x));
            m.Shape("Bar", null, new Vector2(0f, 184f), new Vector2(640f, 40f), Color.black, bath);
            m.Shape("Bar", null, new Vector2(0f, -184f), new Vector2(640f, 40f), Color.black, bath);
            m.FrameFill(s => s >= bathFrom && s < bathTo ? Color.black : HelpArt.Sea);

            // ---- the counters (put away while the bath plays)
            var hud = m.Group("Hud", Vector2.zero);
            During(m, hud, (0f, bathFrom), (bathTo, L));
            const int wood0 = 1240, gold0 = 860, gems0 = 32;
            var bombPrice = IsleListed("Deer Bomb");
            var woodCount = m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "", WoodPill, hud);
            var goldCount = m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "", GoldPill, hud);
            var gemCount = m.Pill("Gems", HelpArt.Resource(ResourceKind.Gem), "", GemPill, hud);
            m.Tally(woodCount, wood0, (bought, wood0 - bombPrice.x));
            m.Tally(goldCount, gold0, (bought, gold0 - bombPrice.y));
            m.Tally(gemCount, gems0, (paid, gems0 - MonsterAnimal.TameCost));

            // Effects on top: the bath's steam, the cured one's sparkle.
            m.Sparkle(new Vector2(inPool.x + 10f, waterY + 30f), bathFrom + 0.5f, Color.white, 4);
            m.Sparkle(new Vector2(inPool.x - 10f, waterY + 40f), bathFrom + 0.9f, Color.white, 4);
            m.Sparkle(zTrail + new Vector2(0f, 30f), bathTo + 0.15f, UiKit.Sun, 6);

            var purchase = IsleBuyCard(m, new Vector2(40f, 28f), "Deer Bomb", bombArt, bombPrice, cardFrom, bought);
            IsleCarried(m, bombArt, trail, bombAt, bought, grab, drop);

            IsleDip(m, L, zFrom, bathFrom, bathTo);

            m.Hand().Show(1.45f, drop + 0.4f)
                .At(1.45f, bombWare + new Vector2(130f, 110f)).At(tap - 0.15f, bombWare + new Vector2(0f, 20f)).Tap(tap)
                .At(buy - 0.2f, purchase).Tap(buy)
                .At(grab - 0.2f, trail + new Vector2(0f, 20f)).Press(grab).At(grab, trail + new Vector2(0f, 20f))
                .At(drop, bombAt + new Vector2(0f, 20f), DemoEase.Linear).Lift(drop + 0.05f)
                .At(drop + 0.4f, bombAt + new Vector2(110f, 100f));
            m.Hand().Show(zTap - 0.5f, payTap + 0.35f)
                .At(zTap - 0.5f, zAt + new Vector2(120f, 110f)).At(zTap - 0.15f, zAt + new Vector2(0f, 34f)).Tap(zTap)
                .At(payTap - 0.35f, talkAt + payAt).Tap(payTap)
                .At(payTap + 0.35f, talkAt + payAt + new Vector2(90f, -40f));
        }

        // ================================================================ shared bits for these movies

        /// <summary>One of a shop's wares, where it stands and its picture.</summary>
        sealed class IsleWare
        {
            public string Name;
            public Vector2 At;
            public Image Pic;
        }

        /// <summary>
        /// A shop's wares in a half circle in front of it, due east round through south to due west
        /// (StoreBuilding.WareSpots), each bobbing like a SaleDisplay. A wedding gift is not on show
        /// until Miso asks for it, so its spot stays empty.
        /// </summary>
        static IsleWare[] IsleWares(HelpMovie m, Transform parent, string listKey, Vector2 shop)
        {
            var names = StoreData.Lists.TryGetValue(listKey, out var list) ? list : Array.Empty<string>();
            var wares = new List<IsleWare>();
            for (int i = 0; i < names.Length; i++)
            {
                if (StoreData.Get(names[i]).Kind == StoreItemKind.Gift) continue;
                var art = HelpArt.Item(names[i]);
                if (art == null) continue;
                float angle = (names.Length > 1 ? i * 180f / (names.Length - 1) : 90f) * Mathf.Deg2Rad;
                var spot = shop + new Vector2(Mathf.Cos(angle) * 108f, -Mathf.Sin(angle) * 68f);
                wares.Add(new IsleWare { Name = names[i], At = spot, Pic = Ware(m, art, spot, 44f, i * 1.3f, parent) });
            }
            return wares.ToArray();
        }

        /// <summary>The price on the shop's list (never one worked out from your purse, which would pin it).</summary>
        static Vector3Int IsleListed(string id)
        {
            var def = StoreData.Get(id);
            return new Vector3Int(def.Wood, def.Gold, def.Gems);
        }

        static bool IsleAffords(Vector3Int price, int wood, int gold, int gems) => wood >= price.x && gold >= price.y && gems >= price.z;

        /// <summary>Something popping into view at <paramref name="when"/> (hidden before).</summary>
        static void IslePop(HelpMovie m, Component c, float when) =>
            m.Size(c.transform).At(when - 0.01f, 0f).At(when, 0.3f, DemoEase.Step).At(when + 0.3f, 1f, DemoEase.Back);

        /// <summary>A building going into the ground where it was dropped (hidden before).</summary>
        static void IsleGrowIn(HelpMovie m, Component c, float when) =>
            m.Size(c.transform).At(when - 0.01f, 0f).At(when, 0.3f, DemoEase.Step).At(when + 0.35f, 1f, DemoEase.Back);

        /// <summary>
        /// The buy panel (PurchaseDialog) with a price given rather than looked up: the name, the
        /// item, wood, gold and gems down the right, and Cancel and Purchase. Returns where Purchase is.
        /// </summary>
        static Vector2 IsleBuyCard(HelpMovie m, Vector2 at, string id, Sprite art, Vector3Int price, float from, float to)
        {
            var card = Panel(m, at, new Vector2(330f, 210f), null, from, to);
            string name = StoryText.Name(id);
            Fit(m.Label("Title", name, new Vector2(0f, 78f), 26f, UiKit.Ink, card), name, 300f);
            m.Pic("Item", art, new Vector2(-88f, -46f), 104f, card);
            int[] amounts = { price.x, price.y, price.z };
            ResourceKind[] kinds = { ResourceKind.Wood, ResourceKind.Gold, ResourceKind.Gem };
            for (int i = 0; i < 3; i++)
            {
                float y = 40f - i * 36f;
                m.Label("Price", amounts[i].ToString(), new Vector2(86f, y), 24f, UiKit.Ink, card);
                m.Shape("Icon", HelpArt.Resource(kinds[i]), new Vector2(128f, y), new Vector2(30f, 30f), Color.white, card).preserveAspect = true;
            }
            var buyAt = new Vector2(78f, -76f);
            WordButton(m, card, new Vector2(-78f, -76f), new Vector2(140f, 42f), UiKit.SurfaceSunk, "Cancel", UiKit.Ink);
            WordButton(m, card, buyAt, new Vector2(140f, 42f), UiKit.Primary, "Purchase", Color.white);
            return at + buyAt;
        }

        /// <summary>Something bought popping into the trail behind you, then dragged out to its spot (the green ring under it on the way).</summary>
        static void IsleCarried(HelpMovie m, Sprite art, Vector2 trail, Vector2 to, float from, float take, float put)
        {
            var item = m.Group("Carried", trail);
            var pic = m.Pic("Pic", art, Vector2.zero, 56f, item);
            var ring = PlaceRing(m, item, true, 110f);
            m.Fade(ring).At(take, 0f).At(take + 0.15f, 0.45f).At(put, 0.45f).At(put + 0.1f, 0f);
            m.Move(item).At(take, trail).At(put, to, DemoEase.Linear);
            m.Size(pic.transform).At(from, 0f).At(from + 0.3f, 1f, DemoEase.Back);
            m.Fade(item).At(from - 0.01f, 0f).At(from, 1f, DemoEase.Step).At(put - 0.01f, 1f).At(put, 0f, DemoEase.Step);
            m.Sparkle(trail + new Vector2(0f, 24f), from, UiKit.Sun, 5);
        }

        /// <summary>
        /// A garden already in the ground: the big flower and its little ones at their sizes, drawn
        /// back to front. Returns the little ones and their size keys, to take bites out of.
        /// </summary>
        static (Image pic, DemoKeys size)[] IsleBed(HelpMovie m, Transform parent, Sprite planted, Vector2 at, float size, Vector2[] spots, float[] scales)
        {
            var kids = new (Image, DemoKeys)[spots.Length];
            var order = new List<int>();
            for (int i = -1; i < spots.Length; i++) order.Add(i);
            order.Sort((p, q) => (q < 0 ? at.y : spots[q].y).CompareTo(p < 0 ? at.y : spots[p].y));
            foreach (int i in order)
            {
                var pic = m.Pic(i < 0 ? "Parent" : "Child", planted, i < 0 ? at : spots[i], size, parent);
                var keys = m.Size(pic.transform).At(0f, i < 0 ? 1f : scales[i]);
                if (i >= 0) kids[i] = (pic, keys);
            }
            return kids;
        }

        /// <summary>A pest blown away (Bomb.BlowAway): a grey puff where it stood.</summary>
        static void IslePoof(HelpMovie m, Vector2 at, float when)
        {
            var grey = new Color(0.6f, 0.55f, 0.5f, 0.9f);
            for (int i = 0; i < 6; i++)
            {
                float angle = i / 6f * Mathf.PI * 2f + 0.3f;
                var cloud = m.Shape("Poof", UiKit.Circle, at, new Vector2(34f, 34f), grey);
                var away = at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.7f) * 44f;
                m.Move(cloud.transform).At(when, at).At(when + 0.45f, away, DemoEase.Out);
                m.Size(cloud.transform).At(when, 0.5f).At(when + 0.2f, 1.1f, DemoEase.Out).At(when + 0.45f, 0.5f);
                m.Fade(cloud).At(when - 0.01f, 0f).At(when, 0.9f, DemoEase.Step).At(when + 0.45f, 0f);
            }
        }

        /// <summary>Your island as the screens open over it: a forest, the sawmill, and you.</summary>
        static void IsleHome(HelpMovie m, Transform parent)
        {
            m.Island("Island", new Vector2(-20f, -40f), new Vector2(540f, 280f), parent);
            m.Pic("Forest", HelpArt.Planted("Medium Pine Forest", "Small Pine Forest"), new Vector2(-190f, -10f), 100f, parent);
            m.Pic("Mill", HelpArt.Item("Sawmill"), new Vector2(40f, 0f), 110f, parent);
            var (_, you) = Walker(m, "You", HelpArt.You, new Vector2(-90f, -125f), 88f, parent);
            m.Face(you).At(0f, -1f);
        }

        /// <summary>One of the round buttons down the right of the screen, its caption in a dark pill under it.</summary>
        static RectTransform IsleHudButton(HelpMovie m, Transform parent, string caption, Sprite icon, Color fill, Vector2 at, float size = 56f)
        {
            var disc = m.RoundButton(caption, icon, fill, at, size, parent);
            var pill = m.Shape("Caption", UiKit.Rounded(18), at + new Vector2(0f, -size * 0.5f - 12f), new Vector2(size + 24f, 24f), UiKit.HudGlass, parent);
            pill.type = Image.Type.Sliced;
            Fit(m.Label("Caption", caption, Vector2.zero, 15f, Color.white, pill.transform), caption, size + 16f);
            return disc;
        }

        /// <summary>The island dimmed behind an open screen.</summary>
        static void IsleScrim(HelpMovie m, Transform parent, float from, float to)
        {
            var dim = m.Shape("Scrim", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Scrim, parent);
            float alpha = UiKit.Scrim.a;
            m.Fade(dim).At(from - 0.01f, 0f).At(from + 0.15f, alpha).At(to, alpha).At(to + 0.2f, 0f);
        }

        /// <summary>A small panel like <see cref="Panel"/>, inside <paramref name="parent"/>.</summary>
        static RectTransform IslePanel(HelpMovie m, Transform parent, Vector2 at, Vector2 size, Color? header, float from, float to)
        {
            var p = m.Card("Panel", at, size, header, parent);
            m.Size(p).At(from, 0.85f).At(from + 0.25f, 1f, DemoEase.Back);
            m.Fade(p).At(from - 0.01f, 0f).At(from, 1f, DemoEase.Step).At(to, 1f).At(to + 0.2f, 0f);
            return p;
        }

        /// <summary>A game screen (UiScreen): the card, its coloured band with the icon and title at the left and the round close at the right.</summary>
        static RectTransform IsleSheet(HelpMovie m, Transform parent, Vector2 at, Vector2 size, Color accent, Sprite icon, string title, float from, float to)
        {
            var sheet = IslePanel(m, parent, at, size, accent, from, to);
            float band = size.y * 0.5f - 22f;
            // The band's lower corners squared off where it meets the body.
            m.Shape("Square", null, new Vector2(0f, band - 11f), new Vector2(size.x, 22f), accent, sheet);
            m.Shape("Icon", icon, new Vector2(-size.x * 0.5f + 30f, band), new Vector2(28f, 28f), Color.white, sheet).preserveAspect = true;
            LeftLabel(m, sheet, title, new Vector2(-size.x * 0.5f + 54f, band), 24f, Color.white, size.x - 150f).fontStyle = FontStyles.Bold;
            var x = new Vector2(size.x * 0.5f - 30f, band);
            m.Shape("Close", UiKit.Circle, x, new Vector2(34f, 34f), new Color(1f, 1f, 1f, 0.25f), sheet);
            m.Shape("X", IconPainter.Close, x, new Vector2(16f, 16f), Color.white, sheet).preserveAspect = true;
            return sheet;
        }

        /// <summary>A button with its icon and word side by side and a darker lip under it, as UiKit.Button draws them.</summary>
        static RectTransform IsleIconButton(HelpMovie m, Transform parent, Vector2 at, Vector2 size, Color fill, Sprite icon, string words, Color ink)
        {
            var g = m.Group("Button", at, parent);
            var lip = m.Shape("Lip", UiKit.Rounded(18), new Vector2(0f, -3f), size, Color.Lerp(fill, Color.black, 0.22f), g);
            lip.type = Image.Type.Sliced;
            var face = m.Shape("Face", UiKit.Rounded(18), Vector2.zero, size, fill, g);
            face.type = Image.Type.Sliced;
            float h = size.y * 0.55f;
            var ic = m.Shape("Icon", icon, Vector2.zero, new Vector2(h, h), ink, face.transform);
            ic.preserveAspect = true;
            var t = m.Label("Label", words, Vector2.zero, size.y * 0.46f, ink, face.transform);
            float room = size.x - h - 34f;
            Fit(t, words, room);
            float w = Mathf.Min(room, t.GetPreferredValues(Loc.T(words)).x);
            float row = h + 8f + w;
            ic.rectTransform.anchoredPosition = new Vector2(-row * 0.5f + h * 0.5f, 0f);
            t.rectTransform.anchoredPosition = new Vector2(row * 0.5f - w * 0.5f, 0f);
            return g;
        }

        /// <summary>Words that wrap inside a box whose top left corner is <paramref name="topLeft"/>.</summary>
        static TextMeshProUGUI IslePara(HelpMovie m, Transform parent, string text, Vector2 topLeft, Vector2 box, float size, Color colour)
        {
            var t = m.Label("Para", text, Vector2.zero, size, colour, parent);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.alignment = TextAlignmentOptions.TopLeft;
            var r = t.rectTransform;
            r.pivot = new Vector2(0f, 1f);
            r.sizeDelta = box;
            r.anchoredPosition = topLeft;
            return t;
        }

        /// <summary>
        /// The card the game pops up in the middle of the dimmed screen, as it looks in play: the
        /// island builder's CardPopup (a 620 × 820 card, a border 14 in, an inner panel 8 in from
        /// that, a 360 picture at the top, a bold 46 title, the 30 body under it, "tap to continue"
        /// at the foot) as MobileHud.Restyle dresses it: the screen's scrim behind, a rounded cream
        /// card with a soft shadow, a sunny yellow border, and a painted icon coloured orange. Drawn
        /// at 0.4 of its size, it zooms up from 0.3 with a little overshoot. Shown from
        /// <paramref name="from"/> until it's tapped at <paramref name="tap"/>. Returns where to tap.
        /// </summary>
        static Vector2 IsleCardPop(HelpMovie m, Sprite picture, string title, string body, float from, float tap)
        {
            const float k = 0.4f;
            var cardSize = new Vector2(620f, 820f) * k;
            var g = m.Group("CardPopup", Vector2.zero);
            m.Shape("Dim", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Scrim, g);
            var card = m.Group("Card", new Vector2(0f, -4f), g);
            var shade = m.Shape("Shadow", UiKit.Rounded(20), new Vector2(0f, -5f), cardSize + new Vector2(6f, 6f), new Color(0f, 0f, 0f, 0.18f), card);
            shade.type = Image.Type.Sliced;
            var paper = m.Shape("Paper", UiKit.Rounded(20), Vector2.zero, cardSize, UiKit.Surface, card);
            paper.type = Image.Type.Sliced;
            var border = m.Shape("Border", UiKit.Rounded(16), Vector2.zero, cardSize - new Vector2(28f, 28f) * k, UiKit.Sun, card);
            border.type = Image.Type.Sliced;
            var inner = m.Shape("Inner", UiKit.Rounded(13), Vector2.zero, cardSize - new Vector2(44f, 44f) * k, UiKit.Surface, card);
            inner.type = Image.Type.Sliced;
            if (picture != null)
                m.Shape("Picture", picture, new Vector2(0f, 150f) * k, new Vector2(360f, 360f) * k,
                    IconPainter.IsIcon(picture) ? UiKit.Primary : Color.white, card).preserveAspect = true;
            // The title in the heavy heading face (UiKit.ApplyFonts), the rest in the body face.
            var head = m.Label("Title", title, new Vector2(0f, -85f) * k, 46f * k, UiKit.Ink, card);
            Fit(head, title, 560f * k);
            var words = m.Label("Body", body, new Vector2(0f, -225f) * k, 30f * k + 0.5f, new Color(0.30f, 0.24f, 0.18f), card);
            words.textWrappingMode = TextWrappingModes.Normal;
            words.alignment = TextAlignmentOptions.Top;
            words.rectTransform.sizeDelta = new Vector2(540f, 210f) * k;
            if (UiKit.BodyFont != null) words.font = UiKit.BodyFont;
            var more = m.Label("Tap", "tap to continue", new Vector2(0f, -370f) * k, 24f * k + 0.5f, new Color(0.5f, 0.42f, 0.34f), card);
            more.fontStyle = FontStyles.Italic;
            if (UiKit.BodyFont != null) more.font = UiKit.BodyFont;
            Fit(more, "tap to continue", 540f * k);
            m.Size(card).At(from, 0.3f).At(from + 0.35f, 1f, DemoEase.Back);
            m.Fade(g).At(from - 0.01f, 0f).At(from, 1f, DemoEase.Step).At(tap + 0.05f, 1f).At(tap + 0.06f, 0f, DemoEase.Step);
            return new Vector2(0f, -40f);
        }

        /// <summary>The finger coming in to tap a card away.</summary>
        static void IsleTapCard(HelpMovie m, Vector2 at, float tap)
        {
            m.Hand().Show(tap - 0.6f, tap + 0.3f)
                .At(tap - 0.6f, at + new Vector2(130f, -100f)).At(tap - 0.2f, at).Tap(tap)
                .At(tap + 0.3f, at + new Vector2(130f, -100f));
        }

        /// <summary>A blink of darkness at each change of scene, and as the movie goes round again.</summary>
        static void IsleDip(HelpMovie m, float length, params float[] cuts)
        {
            var dip = m.Shape("Dip", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("2E1A0E"));
            dip.raycastTarget = false;
            var fade = m.Fade(dip).At(0f, 1f).At(0.3f, 0f);
            foreach (var cut in cuts) fade.At(cut - 0.25f, 0f).At(cut, 1f).At(cut + 0.25f, 0f);
            fade.At(length - 0.35f, 0f).At(length, 1f);
        }
    }
}
