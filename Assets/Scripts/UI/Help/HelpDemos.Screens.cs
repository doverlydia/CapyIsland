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
        // ---------------------------------------------------------------- shared bits for the screens

        /// <summary>One of the round buttons down the right of the screen, with its caption in a dark pill under it.</summary>
        static RectTransform ScreensHudButton(HelpMovie m, string caption, Sprite icon, Color fill, Vector2 at, float size = 56f)
        {
            var disc = m.RoundButton(caption, icon, fill, at, size);
            var pill = m.Shape("Caption", UiKit.Rounded(18), at + new Vector2(0f, -size * 0.5f - 12f), new Vector2(size + 24f, 24f), UiKit.HudGlass);
            pill.type = Image.Type.Sliced;
            var t = m.Label("Caption", caption, Vector2.zero, 15f, Color.white, pill.transform);
            Fit(t, caption, size + 16f);
            return disc;
        }

        /// <summary>The island dimmed behind a screen while it is open.</summary>
        static void ScreensScrim(HelpMovie m, float from, float to)
        {
            var dim = m.Shape("Scrim", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Scrim);
            float a = UiKit.Scrim.a;
            m.Fade(dim).At(from, 0f).At(from + 0.15f, a).At(to, a).At(to + 0.2f, 0f);
        }

        /// <summary>
        /// A game screen as it opens over the island (UiScreen): the card, its coloured band with the
        /// icon at the left and the round close button at the right. Titles go on separately, so a
        /// screen can change page. <paramref name="close"/> is where close is, on the stage.
        /// </summary>
        static RectTransform ScreensSheet(HelpMovie m, Vector2 at, Vector2 size, Color accent, Sprite icon, float from, float to, out Vector2 close)
        {
            var sheet = Panel(m, at, size, accent, from, to);
            float band = size.y * 0.5f - 22f;
            var ic = m.Shape("Icon", icon, new Vector2(-size.x * 0.5f + 30f, band), new Vector2(28f, 28f), Color.white, sheet);
            ic.preserveAspect = true;
            var x = new Vector2(size.x * 0.5f - 30f, band);
            m.Shape("Close", UiKit.Circle, x, new Vector2(34f, 34f), new Color(1f, 1f, 1f, 0.25f), sheet);
            m.Shape("X", IconPainter.Close, x, new Vector2(16f, 16f), Color.white, sheet).preserveAspect = true;
            close = at + x;
            return sheet;
        }

        /// <summary>Words that wrap inside a box whose top left corner is <paramref name="topLeft"/>.</summary>
        static TextMeshProUGUI ScreensPara(HelpMovie m, Transform parent, string text, Vector2 topLeft, Vector2 box, float size, Color colour)
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

        /// <summary>A rounded button with an icon and its word side by side, as UiKit.Button draws them.</summary>
        static RectTransform ScreensIconButton(HelpMovie m, Transform parent, Vector2 at, Vector2 size, Color fill, Sprite icon, string words, Color ink)
        {
            var lip = m.Shape("Lip", UiKit.Rounded(18), at + new Vector2(0f, -3f), size, Color.Lerp(fill, Color.black, 0.22f), parent);
            lip.type = Image.Type.Sliced;
            var face = m.Shape("Button", UiKit.Rounded(18), at, size, fill, parent);
            face.type = Image.Type.Sliced;
            float h = size.y * 0.55f;
            var ic = m.Shape("Icon", icon, Vector2.zero, new Vector2(h, h), Color.white, face.transform);
            ic.preserveAspect = true;
            var t = m.Label("Label", words, Vector2.zero, size.y * 0.48f, ink, face.transform);
            float room = size.x - h - 34f;
            Fit(t, words, room);
            float w = Mathf.Min(room, t.GetPreferredValues(Loc.T(words)).x);
            float row = h + 8f + w;
            ic.rectTransform.anchoredPosition = new Vector2(-row * 0.5f + h * 0.5f, 0f);
            t.rectTransform.anchoredPosition = new Vector2(row * 0.5f - w * 0.5f, 0f);
            return face.rectTransform;
        }

        // ---------------------------------------------------------------- hobbies

        /// <summary>
        /// The Hobbies screen as you use it: tap Hobbies in the column on the right, and the screen
        /// opens with Bag at the top and a carousel of big picture cards, one per hobby: the capybara
        /// at it, its name and level, the bar to the next level, how you're getting on, and Journal.
        /// Swipe to the next hobby, and once more to the first you haven't taken up (greyed, locked,
        /// with how to start it); swipe back, and tap Journal: every bug, the ones you've caught with
        /// their pictures and the rest still "???", and how many you've found. Back to hobbies, tap
        /// Bag: what you've caught and grown. Close.
        /// </summary>
        static void HobbyHubMovie(HelpMovie m)
        {
            const float L = 13f;
            m.Length = L;
            const float tap = 0.95f, open = 1.15f;
            // Each swipe: half a second under the finger, then the snap to the page.
            const float drag1 = 2.05f, drag2 = 3.25f, drag3 = 4.55f, held = 0.5f, snapped = 0.7f;
            const float journalTap = 5.95f, journal = 6.1f, backTap = 8.55f, hub = 8.7f, bagTap = 9.45f, bag = 9.6f;
            const float closeTap = 11.5f, shut = 11.65f;
            var teal = UiKit.Hex("3E9C8F");

            // The island, and the column of round buttons down the right (Settings first).
            m.Island("Island", new Vector2(-30f, -20f), new Vector2(520f, 300f));
            m.Pic("Tree", HelpArt.Item("Medium Pine Forest"), new Vector2(-170f, 10f), 90f);
            m.Pic("House", HelpArt.Item("Capy House"), new Vector2(20f, 20f), 100f);
            m.Pic("You", HelpArt.You, new Vector2(-70f, -90f), 84f);
            var hobbiesAt = new Vector2(250f, -100f);
            ScreensHudButton(m, "Settings", IconPainter.Gear, UiKit.Hex("6B8FA8"), new Vector2(250f, 152f));
            ScreensHudButton(m, "Help", HelpArt.Question, HelpScreen.Accent, new Vector2(250f, 68f));
            ScreensHudButton(m, "Map", IconPainter.Map, UiKit.Spring, new Vector2(250f, -16f));
            ScreensHudButton(m, "Hobbies", IconPainter.Rod, teal, hobbiesAt);

            // The screen, with its title changing as the pages do.
            ScreensScrim(m, open, shut);
            var sheetAt = new Vector2(0f, -4f);
            var sheet = ScreensSheet(m, sheetAt, new Vector2(584f, 392f), teal, IconPainter.Rod, open, shut, out var closeAt);
            During(m, LeftLabel(m, sheet, "Hobbies", new Vector2(-236f, 174f), 24f, Color.white, 380f), (0f, journal), (hub, bag));
            During(m, LeftLabel(m, sheet, "Bug Journal", new Vector2(-236f, 174f), 24f, Color.white, 380f), (journal, hub));
            During(m, LeftLabel(m, sheet, "Bag", new Vector2(-236f, 174f), 24f, Color.white, 380f), (bag, L));

            // ---- the hub: Bag on top (no Kitchen until cooking is taken up), then the carousel
            var hubPage = m.Group("Hub", Vector2.zero, sheet);
            During(m, hubPage, (0f, journal), (hub, bag));
            var bagAt = new Vector2(0f, 122f);
            ScreensIconButton(m, hubPage, bagAt, new Vector2(530f, 40f), UiKit.Spring, IconPainter.Gift, "Bag", Color.white);

            const float W = 440f;
            var viewAt = new Vector2(0f, -28f);
            var view = m.Group("View", viewAt, hubPage);
            view.sizeDelta = new Vector2(W, 262f);
            view.gameObject.AddComponent<RectMask2D>();
            var strip = m.Group("Pages", Vector2.zero, view);
            int fishCount = HobbyData.Fish.Length, bugCount = HobbyData.Bugs.Length;
            const int bugsFound = 6;
            // Yours first (fishing, bug catching), then the first one still to take up.
            ScreensHobbyCard(m, strip, 0f, HobbyData.Art("Vignette_Fishing"), UiKit.Spring,
                "Fishing  <size=75%><color=#2DB3A6>Level 4</color></size>", 0.6f, $"7 of {fishCount} fish found");
            var journalAt = ScreensHobbyCard(m, strip, W, HobbyData.Art("Vignette_Bugs"), UiKit.Hex("7BAE3E"),
                "Bug Catching  <size=75%><color=#7BAE3E>Level 3</color></size>", 0.35f, $"{bugsFound} of {bugCount} bugs caught");
            ScreensHobbyCard(m, strip, 2f * W, HobbyData.Art("Vignette_Birding"), UiKit.InkSoft, "Birdwatching", 0f, null,
                HobbyLog.HowToUnlock(CapyIsland.Game.Hobby.Birding));
            m.Move(strip)
                .At(drag1, Vector2.zero).At(drag1 + held, new Vector2(-170f, 0f), DemoEase.Linear).At(drag1 + snapped, new Vector2(-W, 0f), DemoEase.Out)
                .At(drag2, new Vector2(-W, 0f)).At(drag2 + held, new Vector2(-W - 170f, 0f), DemoEase.Linear).At(drag2 + snapped, new Vector2(-2f * W, 0f), DemoEase.Out)
                .At(drag3, new Vector2(-2f * W, 0f)).At(drag3 + held, new Vector2(-2f * W + 170f, 0f), DemoEase.Linear).At(drag3 + snapped, new Vector2(-W, 0f), DemoEase.Out);

            // Arrows at the sides, and a dot for each of the eight hobbies.
            for (int i = -1; i <= 1; i += 2)
            {
                var arrow = m.Shape(i < 0 ? "Prev" : "Next", UiKit.Circle, new Vector2(i * 252f, 10f), new Vector2(40f, 40f), new Color(1f, 1f, 1f, 0.95f), hubPage);
                m.Shape("Rim", IconPainter.Ring, Vector2.zero, new Vector2(40f, 40f), new Color(0f, 0f, 0f, 0.12f), arrow.transform);
                var ic = m.Shape("Arrow", IconPainter.Arrow, Vector2.zero, new Vector2(22f, 22f), UiKit.Ink, arrow.transform);
                ic.preserveAspect = true;
                if (i < 0) ic.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }
            var dots = new Image[8];
            for (int i = 0; i < dots.Length; i++)
                dots[i] = m.Shape("Dot", UiKit.Circle, new Vector2((i - 3.5f) * 20f, -172f), new Vector2(10f, 10f), UiKit.SurfaceSunk, hubPage);
            m.Do(s =>
            {
                int shown = s < drag1 + snapped ? 0 : s < drag2 + snapped ? 1 : s < drag3 + snapped ? 2 : 1;
                for (int d = 0; d < dots.Length; d++)
                {
                    dots[d].color = d == shown ? teal : UiKit.SurfaceSunk;
                    dots[d].rectTransform.sizeDelta = Vector2.one * (d == shown ? 13f : 10f);
                }
            });

            // ---- the bug journal: how many found, and every bug, found or still "???"
            var journalPage = m.Group("Journal", Vector2.zero, sheet);
            During(m, journalPage, (journal, hub));
            var backAt = new Vector2(0f, 122f);
            WordButton(m, journalPage, backAt, new Vector2(300f, 36f), UiKit.SurfaceSunk, "Back to hobbies", UiKit.Ink);
            var hat = JournalRewards.Hat(CapyIsland.Game.Hobby.Bugs);
            string progress = $"Found {bugsFound} of {bugCount}. Find them all for {JournalRewards.Gems} gems" + (hat != null ? " and the " + hat.Name + "." : ".");
            Fit(m.Label("Progress", progress, new Vector2(0f, 90f), 15f, UiKit.Ink, journalPage), progress, 540f);
            m.Shape("Track", UiKit.Rounded(8), new Vector2(0f, 70f), new Vector2(520f, 10f), UiKit.SurfaceSunk, journalPage).type = Image.Type.Sliced;
            var bar = m.Shape("Fill", UiKit.Rounded(8), new Vector2(-260f, 70f), new Vector2(520f * bugsFound / Mathf.Max(1, bugCount), 10f), UiKit.Leaf, journalPage);
            bar.type = Image.Type.Sliced;
            bar.rectTransform.pivot = new Vector2(0f, 0.5f);
            bar.rectTransform.anchoredPosition = new Vector2(-260f, 70f);
            for (int i = 0; i < Mathf.Min(12, bugCount); i++)
            {
                var bug = HobbyData.Bugs[i];
                var at = new Vector2(-215f + (i % 6) * 86f, 6f - (i / 6) * 108f);
                ScreensJournalTile(m, journalPage, at, HobbyData.Art("Bug_" + bug.Id), bug.Name, i < bugsFound, HobbyData.Rarity(bug.Stars));
            }

            // ---- the bag: what you've caught and grown (sorted as the game sorts it)
            var bagPage = m.Group("BagPage", Vector2.zero, sheet);
            During(m, bagPage, (bag, L));
            WordButton(m, bagPage, backAt, new Vector2(300f, 36f), UiKit.SurfaceSunk, "Back to hobbies", UiKit.Ink);
            (string id, int count)[] carried = { ("crop:Carrot", 4), ("fish:salmon", 2), ("fish:sardine", 3) };
            for (int i = 0; i < carried.Length; i++)
            {
                var row = m.Shape("Row", UiKit.Rounded(22), new Vector2(0f, 62f - i * 76f), new Vector2(540f, 66f), Color.white, bagPage);
                row.type = Image.Type.Sliced;
                var art = HobbyData.ItemArt(carried[i].id);
                var pic = m.Shape("Picture", art, new Vector2(-232f, 0f), new Vector2(52f, 52f), Color.white, row.transform);
                pic.preserveAspect = true;
                pic.enabled = art != null;
                LeftLabel(m, row.transform, HobbyData.ItemName(carried[i].id), new Vector2(-196f, 12f), 20f, UiKit.Ink, 380f);
                LeftLabel(m, row.transform, "×" + carried[i].count, new Vector2(-196f, -15f), 16f, UiKit.InkSoft, 200f);
            }

            // The finger: Hobbies, three swipes, Journal, back, Bag, close.
            Vector2 S(Vector2 local) => sheetAt + local;
            var swipeRight = new Vector2(130f, 0f);
            var swipeLeft = new Vector2(-40f, 0f);
            var journalButton = S(viewAt + journalAt + new Vector2(-W, 0f));   // the strip is on its second page by then
            m.Hand().Show(0.35f, closeTap + 0.4f)
                .At(0.35f, hobbiesAt + new Vector2(-130f, 40f)).At(tap - 0.2f, hobbiesAt).Tap(tap)
                .At(drag1 - 0.3f, swipeRight).Press(drag1).At(drag1, swipeRight).At(drag1 + held, swipeLeft, DemoEase.Linear).Lift(drag1 + held)
                .At(drag2 - 0.3f, swipeRight).Press(drag2).At(drag2, swipeRight).At(drag2 + held, swipeLeft, DemoEase.Linear).Lift(drag2 + held)
                .At(drag3 - 0.3f, new Vector2(-80f, 0f)).Press(drag3).At(drag3, new Vector2(-80f, 0f)).At(drag3 + held, new Vector2(90f, 0f), DemoEase.Linear).Lift(drag3 + held)
                .At(journalTap - 0.35f, journalButton).Tap(journalTap)
                .At(backTap - 0.35f, S(backAt)).Tap(backTap)
                .At(bagTap - 0.35f, S(bagAt)).Tap(bagTap)
                .At(closeTap - 0.35f, closeAt).Tap(closeTap)
                .At(closeTap + 0.4f, closeAt + new Vector2(-70f, -90f));
        }

        /// <summary>
        /// One page of the hobbies carousel (HobbiesScreen.PictureCard): the capybara at it on a soft
        /// disc of its colour, its name and level, the bar to the next level, how you're getting on,
        /// and Journal. With <paramref name="how"/>: not taken up yet, so greyed, locked, and how to
        /// start it. Returns where Journal is, on the strip.
        /// </summary>
        static Vector2 ScreensHobbyCard(HelpMovie m, Transform strip, float x, Sprite art, Color colour, string title, float progress, string stat, string how = null)
        {
            bool locked = how != null;
            var card = m.Shape("Card", UiKit.Rounded(30), new Vector2(x, 0f), new Vector2(420f, 252f), locked ? UiKit.Hex("F1ECE4") : Color.white, strip);
            card.type = Image.Type.Sliced;
            var c = card.transform;
            var picAt = new Vector2(0f, 54f);
            m.Shape("Disc", IconPainter.Glow, picAt, new Vector2(170f, 150f), new Color(colour.r, colour.g, colour.b, locked ? 0.12f : 0.3f), c);
            if (art != null)
                m.Shape("Picture", art, picAt, new Vector2(330f, 128f), locked ? new Color(0.55f, 0.5f, 0.47f, 0.55f) : Color.white, c).preserveAspect = true;
            if (locked)
            {
                var lk = m.Shape("Lock", UiKit.Circle, picAt, new Vector2(56f, 56f), UiKit.Hex("8A7563"), c);
                m.Shape("Icon", IconPainter.Lock, Vector2.zero, new Vector2(30f, 30f), Color.white, lk.transform).preserveAspect = true;
                Fit(m.Label("Name", title, new Vector2(0f, -36f), 24f, UiKit.InkSoft, c), title, 390f);
                var words = ScreensPara(m, c, how, new Vector2(-190f, -58f), new Vector2(380f, 60f), 16f, UiKit.InkSoft);
                words.alignment = TextAlignmentOptions.Top;
                return new Vector2(x, 0f);
            }
            Fit(m.Label("Name", title, new Vector2(0f, -34f), 23f, UiKit.Ink, c), title, 390f);
            m.Shape("Track", UiKit.Rounded(8), new Vector2(0f, -58f), new Vector2(280f, 12f), UiKit.SurfaceSunk, c).type = Image.Type.Sliced;
            var fill = m.Shape("Fill", UiKit.Rounded(8), Vector2.zero, new Vector2(Mathf.Max(12f, 280f * progress), 12f), colour, c);
            fill.type = Image.Type.Sliced;
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.anchoredPosition = new Vector2(-140f, -58f);
            Fit(m.Label("Stat", stat, new Vector2(0f, -79f), 16f, UiKit.Ink, c), stat, 390f);
            var go = new Vector2(0f, -106f);
            WordButton(m, c, go, new Vector2(170f, 34f), UiKit.Primary, "Journal", Color.white);
            return new Vector2(x, 0f) + go;
        }

        /// <summary>A journal tile: found, its picture and name; not yet, a dark shadow and "???". Its rarity under it.</summary>
        static void ScreensJournalTile(HelpMovie m, Transform parent, Vector2 at, Sprite pic, string name, bool known, string rarity)
        {
            var tile = m.Shape("Tile", UiKit.Rounded(16), at, new Vector2(80f, 98f), known ? Color.white : UiKit.SurfaceSunk, parent);
            tile.type = Image.Type.Sliced;
            var p = m.Shape("Picture", pic, new Vector2(0f, known ? 18f : 22f), new Vector2(64f, known ? 52f : 46f),
                known ? Color.white : new Color(0.2f, 0.16f, 0.13f, 0.55f), tile.transform);
            p.preserveAspect = true;
            p.enabled = pic != null;
            string shown = known ? name : "???";
            Fit(m.Label("Name", shown, new Vector2(0f, -17f), 12f, known ? UiKit.Ink : UiKit.InkSoft, tile.transform), shown, 74f);
            Fit(m.Label("Detail", rarity, new Vector2(0f, -34f), 10f, UiKit.Hex("B98A1E"), tile.transform), rarity, 74f);
        }

        // ---------------------------------------------------------------- bigger text and language

        /// <summary>Where the settings page puts what the finger taps (sheet coordinates).</summary>
        sealed class ScreensSettingsSpots
        {
            public RectTransform Page;
            public Vector2 Toggle, Bigger;
            public Vector2[] Pills;
        }

        /// <summary>
        /// Easy Reading and the language, as the Settings screen does them. Old Yuzu is saying
        /// something in her corner, small. Tap Settings (the gear at the top of the column): turn
        /// Easy Reading on, and its options fold out while every word on the screen grows to 150%;
        /// tap A+ twice and the words (the sample sentence most of all) grow to 175%, then 200%. Tap
        /// the other language and the whole screen is in it, laid out the other way round; tap your
        /// own and it's back. Close Settings: Yuzu's line is big now. Press and hold it (it's on a
        /// bubble you can tap, so it's a hold rather than a tap) and it opens in the large reading
        /// window; Close.
        /// </summary>
        static void ReadingMovie(HelpMovie m)
        {
            const float L = 10.8f;
            m.Length = L;
            const float settingsTap = 1.0f, open = 1.2f, on = 2.1f, plus1 = 3.0f, plus2 = 3.6f, other = 4.55f, back = 5.5f;
            const float closeTap = 6.35f, shut = 6.5f, hold = 7.55f, reads = hold + 0.55f, readerClose = 9.6f, readerShut = 9.75f;
            var accent = UiKit.Hex("6B8FA8");
            var here = Loc.Current;
            var away = here == Language.English ? Language.Hebrew : Language.English;
            float[] steps = Accessibility.TextSizes;

            // Every label on the screen grows with Easy Reading: off, then the default 150%, then A+ twice.
            float Grown(float time) => time < on ? 1f : steps[Mathf.Min(steps.Length - 1, time < plus1 ? 2 : time < plus2 ? 3 : 4)];
            var grow = new List<(TMP_Text label, float baseSize, float room)>();
            float grownTo = -1f;
            m.Do(s =>
            {
                float k = Grown(s);
                if (Mathf.Approximately(k, grownTo)) return;
                grownTo = k;
                foreach (var (label, baseSize, room) in grow)
                {
                    label.fontSize = baseSize * k;
                    if (room <= 0f) continue;
                    float w = label.GetPreferredValues(label.text).x;
                    if (w > room) label.fontSize *= room / w;
                }
            });

            // The island, the buttons down the right, and Old Yuzu talking in her corner.
            m.Island("Island", new Vector2(-30f, 0f), new Vector2(520f, 300f));
            m.Pic("Tree", HelpArt.Item("Medium Pine Forest"), new Vector2(-160f, 30f), 90f);
            m.Pic("House", HelpArt.Item("Capy House"), new Vector2(40f, 30f), 100f);
            m.Pic("You", HelpArt.You, new Vector2(-70f, -50f), 80f);
            var settingsAt = new Vector2(250f, 150f);
            ScreensHudButton(m, "Settings", IconPainter.Gear, accent, settingsAt);
            ScreensHudButton(m, "Help", HelpArt.Question, HelpScreen.Accent, new Vector2(250f, 66f));
            ScreensHudButton(m, "Map", IconPainter.Map, UiKit.Spring, new Vector2(250f, -18f));

            var yuzuAt = new Vector2(-240f, -142f);
            m.Shape("Disc", UiKit.Circle, yuzuAt, new Vector2(88f, 88f), UiKit.Hex("F6C89A"));
            var yuzu = Resources.Load<Sprite>("Cutscenes/Cutscene_IntroYuzu");
            if (yuzu != null) m.Shape("Yuzu", yuzu, yuzuAt, new Vector2(80f, 80f), Color.white).preserveAspect = true;
            var bubbleAt = new Vector2(48f, -140f);
            var bubble = m.Shape("Bubble", UiKit.Rounded(22), bubbleAt, new Vector2(400f, 118f), UiKit.Surface);
            bubble.type = Image.Type.Sliced;
            string line = StoryText.Yuzu.Back[0];
            var said = ScreensPara(m, bubble.transform, line, new Vector2(-185f, 54f), new Vector2(370f, 108f), 13f, UiKit.Ink);
            said.alignment = TextAlignmentOptions.MidlineLeft;
            grow.Add((said, 13f, 0f));

            // Settings, in your language and (for the moment you switch) in the other one.
            ScreensScrim(m, open, shut);
            var sheetAt = new Vector2(0f, -4f);
            var sheet = ScreensSheet(m, sheetAt, new Vector2(584f, 392f), accent, IconPainter.Gear, open, shut, out var closeAt);
            var mine = ScreensSettingsPage(m, sheet, here, grow, on, plus1, plus2);
            var theirs = ScreensSettingsPage(m, sheet, away, grow, on, plus1, plus2);
            During(m, mine.Page, (0f, other + 0.1f), (back + 0.1f, L));
            During(m, theirs.Page, (other + 0.1f, back + 0.1f));

            // The large reading window: dark behind, the words big, and Close along the bottom.
            var reader = m.Group("Reader", Vector2.zero);
            m.Shape("Scrim", null, Vector2.zero, new Vector2(640f, 440f), new Color(0.08f, 0.05f, 0.03f, 0.78f), reader);
            var card = m.Shape("Card", UiKit.Rounded(30), Vector2.zero, new Vector2(540f, 370f), UiKit.Surface, reader);
            card.type = Image.Type.Sliced;
            var big = ScreensPara(m, card.transform, line, new Vector2(-240f, 160f), new Vector2(480f, 240f), 30f, UiKit.Ink);
            big.lineSpacing = 12f;
            var readerCloseAt = new Vector2(0f, -140f);
            WordButton(m, card.transform, readerCloseAt, new Vector2(470f, 58f), UiKit.Primary, "Close", Color.white);
            m.Fade(reader).At(reads - 0.01f, 0f).At(reads, 1f, DemoEase.Step).At(readerShut, 1f).At(readerShut + 0.01f, 0f, DemoEase.Step);
            m.Size(card.transform).At(reads, 0.9f).At(reads + 0.25f, 1f, DemoEase.Back);

            // The finger: Settings, the switch, A+ twice, the other language, back, close.
            Vector2 S(Vector2 local) => sheetAt + local;
            m.Hand().Show(0.4f, closeTap + 0.4f)
                .At(0.4f, settingsAt + new Vector2(-120f, -80f)).At(settingsTap - 0.2f, settingsAt).Tap(settingsTap)
                .At(on - 0.4f, S(mine.Toggle)).Tap(on - 0.1f)
                .At(plus1 - 0.4f, S(mine.Bigger)).Tap(plus1 - 0.1f)
                .At(plus2 - 0.3f, S(mine.Bigger)).Tap(plus2 - 0.1f)
                .At(other - 0.4f, S(mine.Pills[(int)away])).Tap(other)
                .At(back - 0.4f, S(theirs.Pills[(int)here])).Tap(back)
                .At(closeTap - 0.35f, closeAt).Tap(closeTap)
                .At(closeTap + 0.4f, closeAt + new Vector2(-60f, -90f));
            // Then press and hold Yuzu's words until the reader opens, and Close it.
            var holdAt = bubbleAt + new Vector2(30f, 0f);
            m.Hand().Show(hold - 0.5f, reads + 0.45f)
                .At(hold - 0.5f, holdAt + new Vector2(130f, 90f)).At(hold - 0.1f, holdAt).Press(hold).At(hold, holdAt)
                .At(reads + 0.15f, holdAt).Lift(reads + 0.15f).At(reads + 0.45f, holdAt + new Vector2(100f, 60f));
            m.Hand().Show(readerClose - 0.6f, readerClose + 0.4f)
                .At(readerClose - 0.6f, readerCloseAt + new Vector2(120f, 90f)).At(readerClose - 0.2f, readerCloseAt).Tap(readerClose)
                .At(readerClose + 0.4f, readerCloseAt + new Vector2(110f, 60f));
        }

        /// <summary>
        /// Words in <paramref name="lang"/>, not translated again, starting at <paramref name="x"/>
        /// (given for left to right) and running away from it: rightwards, or leftwards from the
        /// mirrored spot in Hebrew, as a mirrored settings row has them.
        /// </summary>
        static TextMeshProUGUI ScreensWords(HelpMovie m, Transform parent, Language lang, string english, float x, float y, float size, Color colour, float room)
        {
            var t = m.Label("Words", Loc.In(lang, english), new Vector2(x, y), size, colour, parent);
            var loc = t.GetComponent<LocText>();
            if (loc != null) loc.NoTranslate = true;
            bool rtl = lang == Language.Hebrew;
            var r = t.rectTransform;
            r.pivot = new Vector2(rtl ? 1f : 0f, 0.5f);
            r.sizeDelta = new Vector2(room > 0f ? room : 300f, size * 1.6f);
            r.anchoredPosition = new Vector2(rtl ? -x : x, y);
            t.alignment = rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            return t;
        }

        /// <summary>Words in <paramref name="lang"/>, not translated again, centred (wrapping in <paramref name="wrap"/> if given).</summary>
        static TextMeshProUGUI ScreensSay(HelpMovie m, Transform parent, Language lang, string english, Vector2 at, float size, Color colour, float wrap = 0f)
        {
            var t = m.Label("Say", Loc.In(lang, english), at, size, colour, parent);
            var loc = t.GetComponent<LocText>();
            if (loc != null) loc.NoTranslate = true;
            if (wrap > 0f)
            {
                t.textWrappingMode = TextWrappingModes.Normal;
                t.rectTransform.sizeDelta = new Vector2(wrap, 80f);
            }
            return t;
        }

        /// <summary>
        /// The top of the Settings screen in one language: Easy Reading with its switch; once it's on,
        /// Text size (A−, the size, A+), the sample sentence and Tap text to read it; then Language
        /// with a pill for each, and Sound. In Hebrew every row runs the other way (RtlLayout).
        /// Its words go in <paramref name="grow"/>, to grow with Easy Reading.
        /// </summary>
        static ScreensSettingsSpots ScreensSettingsPage(HelpMovie m, Transform sheet, Language lang,
            List<(TMP_Text label, float baseSize, float room)> grow, float on, float plus1, float plus2)
        {
            bool rtl = lang == Language.Hebrew;
            float X(float x) => rtl ? -x : x;
            var accent = UiKit.Hex("6B8FA8");
            var spots = new ScreensSettingsSpots { Page = m.Group(lang.ToString(), Vector2.zero, sheet), Pills = new Vector2[2] };
            var page = spots.Page;

            TextMeshProUGUI Words(Transform parent, string english, float x, float y, float fontSize, Color colour, float room)
            {
                var words = ScreensWords(m, parent, lang, english, x, y, fontSize, colour, room);
                grow.Add((words, fontSize, room));
                return words;
            }

            RectTransform Row(Transform parent, float y, Sprite icon, string english, float room)
            {
                var row = m.Group("Row", new Vector2(0f, y), parent);
                m.Shape("Back", UiKit.Rounded(22), Vector2.zero, new Vector2(548f, 52f), Color.white, row).type = Image.Type.Sliced;
                m.Shape("Icon", icon, new Vector2(X(-246f), 0f), new Vector2(28f, 28f), UiKit.InkSoft, row).preserveAspect = true;
                Words(row, english, -222f, 0f, 18f, UiKit.Ink, room);
                return row;
            }

            // An on/off switch at the end of a row, "On"/"Off" beside it; on from <paramref name="from"/> (always, if negative).
            void Switch(Transform row, float from)
            {
                var track = m.Shape("Toggle", UiKit.Rounded(18), new Vector2(X(228f), 0f), new Vector2(64f, 34f), UiKit.Leaf, row);
                track.type = Image.Type.Sliced;
                var knob = m.Shape("Knob", UiKit.Circle, Vector2.zero, new Vector2(28f, 28f), Color.white, track.transform);
                var onWord = ScreensSay(m, row, lang, "On", new Vector2(X(168f), 0f), 15f, UiKit.InkSoft);
                var offWord = ScreensSay(m, row, lang, "Off", new Vector2(X(168f), 0f), 15f, UiKit.InkSoft);
                grow.Add((onWord, 15f, 60f));
                grow.Add((offWord, 15f, 60f));
                float lit = Mathf.Max(0f, from);
                During(m, onWord, (lit, 999f));
                During(m, offWord, (0f, lit));
                m.Do(s =>
                {
                    bool isOn = s >= lit;
                    track.color = isOn ? UiKit.Leaf : UiKit.SurfaceSunk;
                    knob.rectTransform.anchoredPosition = new Vector2(isOn ? 15f : -15f, 0f);
                });
            }

            Words(page, "Settings", -228f, 174f, 20f, Color.white, 400f);

            var easy = Row(page, 128f, IconPainter.Magnifier, "Easy Reading", 320f);
            Switch(easy, on);
            spots.Toggle = new Vector2(X(228f), 128f);

            // What Easy Reading folds out.
            var options = m.Group("Options", Vector2.zero, page);
            m.Between(options.gameObject, on, 999f);
            var sizeRow = Row(options, 70f, IconPainter.Magnifier, "Text size", 290f);
            var minus = m.Shape("Smaller", UiKit.Rounded(16), new Vector2(X(118f), 0f), new Vector2(54f, 38f), UiKit.SurfaceSunk, sizeRow);
            minus.type = Image.Type.Sliced;
            ScreensSay(m, minus.transform, lang, "A−", Vector2.zero, 18f, UiKit.Ink);
            var value = ScreensSay(m, sizeRow, lang, "", new Vector2(X(178f), 0f), 16f, UiKit.Ink);
            var plus = m.Shape("Bigger", UiKit.Rounded(16), new Vector2(X(238f), 0f), new Vector2(54f, 38f), UiKit.Primary, sizeRow);
            plus.type = Image.Type.Sliced;
            ScreensSay(m, plus.transform, lang, "A+", Vector2.zero, 20f, Color.white);
            spots.Bigger = new Vector2(X(238f), 70f);
            float[] steps = Accessibility.TextSizes;
            m.Do(s =>
            {
                int step = Mathf.Min(steps.Length - 1, s < plus1 ? 2 : s < plus2 ? 3 : 4);
                string pct = Mathf.RoundToInt(steps[step] * 100f) + "%";
                if (value.text != pct) value.text = pct;
            });
            var sample = ScreensSay(m, options, lang, "This is how big the words will be.", new Vector2(0f, 14f), 16f, UiKit.Ink, 520f);
            grow.Add((sample, 16f, 0f));
            var read = Row(options, -44f, IconPainter.Book, "Tap text to read it", 300f);
            Switch(read, -1f);

            // Language and Sound, pushed down once the options are out.
            var langRow = Row(page, 70f, IconPainter.Globe, "Language", 280f);
            m.Move(langRow).At(on - 0.01f, new Vector2(0f, 70f)).At(on, new Vector2(0f, -102f), DemoEase.Step);
            // The pills keep their own order (English, then עברית); the row around them is mirrored.
            float[] pillX = rtl ? new[] { -226f, -132f } : new[] { 132f, 226f };
            Language[] langs = { Language.English, Language.Hebrew };
            for (int i = 0; i < langs.Length; i++)
            {
                bool chosen = langs[i] == lang;
                var pill = m.Shape("Pill", UiKit.Rounded(18), new Vector2(pillX[i], 0f), new Vector2(88f, 38f), chosen ? accent : UiKit.SurfaceSunk, langRow);
                pill.type = Image.Type.Sliced;
                var name = m.Label("Name", Loc.NativeName(langs[i]), Vector2.zero, 17f, chosen ? Color.white : UiKit.Ink, pill.transform);
                var loc = name.GetComponent<LocText>();
                if (loc != null) loc.NoTranslate = true;
                spots.Pills[(int)langs[i]] = new Vector2(pillX[i], -102f);
            }
            var soundRow = Row(page, 12f, IconPainter.Sound, "Sound", 320f);
            Switch(soundRow, -1f);
            m.Move(soundRow).At(on - 0.01f, new Vector2(0f, 12f)).At(on, new Vector2(0f, -160f), DemoEase.Step);
            return spots;
        }

        // ---------------------------------------------------------------- kinds of capybara

        /// <summary>
        /// The other capybaras, as you get them. The Bronze Store sells the Burrow Hole: tap it,
        /// Purchase, put it down, and its Burrow Capybara stands in front of it; tap that, Purchase,
        /// and it joins your trail. Drag it onto the sawmill and it works beside an ordinary capybara,
        /// walking and chopping twice as fast: two loads home while the other brings one. The Gold
        /// Store sells the Woolly Lodge the same way, with its Woolly Capybara (a little slower on its
        /// feet). And a zombie capybara, wrecking a tree for nobody: tap it, Pay 20 Gems, it has its
        /// warm bath, comes out happy and into your trail, and digs for you at the mine.
        /// </summary>
        static void KindsMovie(HelpMovie m)
        {
            const float bFrom = 7.2f, cFrom = 14.5f, dFrom = 21.6f;
            var pine = HelpArt.Planted("Medium Pine Forest", "Small Pine Forest");
            var axe = HelpArt.Tool(ResourceKind.Wood);
            int pinePays = BunniRules.Price(NodeResource.Pine), rockPays = BunniRules.Price(NodeResource.Bronze);

            // ---- the Burrow Hole and its capybara, from the Bronze Store
            ScreensHome(m, 0f, bFrom, "Bronze Store", "bronze_store", "Fox House", "Fox", 0.7f, 900, 120);

            // ---- twice as fast: a burrow capybara beside an ordinary one at the sawmill
            const float bGrab = 7.75f, bDrop = 8.4f, go = 8.6f;
            var work = m.Group("Work", Vector2.zero);
            During(m, work, (bFrom, cFrom));
            m.Island("Island", new Vector2(0f, -30f), new Vector2(570f, 290f), work);
            var mill = new Vector2(-150f, -15f);
            m.Pic("Mill", HelpArt.Item("Sawmill"), mill, 120f, work);
            var ring = m.Shape("StationRing", IconPainter.Ring, mill + new Vector2(0f, 40f), new Vector2(220f, 130f), new Color(0.36f, 0.82f, 0.36f, 0.9f), work);
            m.Fade(ring).At(bGrab, 0f).At(bGrab + 0.2f, 0.9f).At(bDrop, 0.9f).At(bDrop + 0.2f, 0f);
            m.Pic("Forest", pine, new Vector2(235f, -20f), 105f, work);
            // Little trees round the forest, drawn back to front: the ordinary one's, then the burrow's two.
            var nTree = new Vector2(160f, -55f);
            var b2Tree = new Vector2(220f, -118f);
            var b1Tree = new Vector2(120f, -128f);
            const float nFull = 0.62f, b1Full = 0.6f, b2Full = 0.64f;
            var nPic = m.Pic("Pine", pine, nTree, 100f, work);
            var nSize = m.Size(nPic.transform).At(0f, nFull);
            var b2Pic = m.Pic("Pine", pine, b2Tree, 100f, work);
            var b2Size = m.Size(b2Pic.transform).At(0f, b2Full);
            var b1Pic = m.Pic("Pine", pine, b1Tree, 100f, work);
            var b1Size = m.Size(b1Pic.transform).At(0f, b1Full);
            // The same swing takes twice as much for a burrow capybara: half the swings.
            float[] nHits = { 10.1f, 10.6f, 11.1f, 11.6f };
            float[] b1Hits = { 9.5f, 10.0f }, b2Hits = { 11.9f, 12.4f };
            Bitten(nSize, nFull, nHits);
            Rock(m, nPic, nHits);
            Bitten(b1Size, b1Full, b1Hits);
            Rock(m, b1Pic, b1Hits);
            Bitten(b2Size, b2Full, b2Hits);
            Rock(m, b2Pic, b2Hits);

            var (_, youB) = Walker(m, "You", HelpArt.You, new Vector2(-200f, -115f), 74f, work);
            m.Face(youB).At(0f, -1f);
            var trailB = new Vector2(-258f, -130f);

            // The ordinary capybara: to its tree, four swings, and home with the log.
            var homeN = new Vector2(-70f, -72f);
            var workN = nTree + new Vector2(-46f, -6f);
            var capy = ScreensCrew(m, work, homeN, 62f, HelpArt.Worker, axe, HelpArt.Log);
            m.Move(capy.Holder).At(go, homeN).At(9.8f, workN, DemoEase.Linear).At(11.75f, workN).At(12.95f, homeN, DemoEase.Linear);
            m.Face(capy.Body).At(0f, 1f).At(go, -1f, DemoEase.Step).At(11.75f, 1f, DemoEase.Step);
            m.Bob(capy.Body, 6f, 12f, s => (s > go && s < 9.8f) || (s > 11.75f && s < 12.95f));
            During(m, capy.Tool, (go, 11.75f));
            During(m, capy.Load, (11.75f, 13.0f));
            Swings(m, capy, Array.Empty<float>(), nHits);

            // The burrow capybara: dragged out of your trail onto the mill, then there and back twice.
            var homeB = new Vector2(-160f, -82f);
            var millDrop = mill + new Vector2(0f, 10f);
            var workB1 = b1Tree + new Vector2(-46f, -4f);
            var workB2 = b2Tree + new Vector2(-46f, -4f);
            var fox = ScreensCrew(m, work, trailB, 58f, HelpArt.Item("Fox"), axe, HelpArt.Log);
            m.Move(fox.Holder).At(bGrab, trailB).At(bDrop, millDrop, DemoEase.Linear).At(bDrop + 0.15f, homeB)
                .At(go, homeB).At(9.2f, workB1, DemoEase.Linear).At(10.15f, workB1).At(10.75f, homeB, DemoEase.Linear)
                .At(10.85f, homeB).At(11.6f, workB2, DemoEase.Linear).At(12.55f, workB2).At(13.3f, homeB, DemoEase.Linear);
            m.Size(fox.Holder).At(bDrop, 1f).At(bDrop + 0.15f, 1.25f).At(bDrop + 0.35f, 1f);
            m.Face(fox.Body).At(0f, -1f).At(bDrop + 0.15f, 1f, DemoEase.Step).At(go, -1f, DemoEase.Step).At(10.15f, 1f, DemoEase.Step)
                .At(10.85f, -1f, DemoEase.Step).At(12.55f, 1f, DemoEase.Step);
            m.Bob(fox.Body, 6f, 14f, s => (s > go && s < 9.2f) || (s > 10.15f && s < 10.75f) || (s > 10.85f && s < 11.6f) || (s > 12.55f && s < 13.3f));
            During(m, fox.Tool, (go, 10.15f), (10.85f, 12.55f));
            During(m, fox.Load, (10.15f, 10.8f), (12.55f, 13.35f));
            Swings(m, fox, Array.Empty<float>(), new[] { b1Hits[0], b1Hits[1], b2Hits[0], b2Hits[1] });

            var woodArt = HelpArt.Resource(ResourceKind.Wood);
            m.FloatUp(woodArt, "+" + pinePays, homeB + new Vector2(-10f, 130f), 10.8f);
            m.FloatUp(woodArt, "+" + pinePays, homeN + new Vector2(20f, 130f), 13.0f);
            m.FloatUp(woodArt, "+" + pinePays, homeB + new Vector2(-10f, 130f), 13.35f);

            m.Hand().Show(bGrab - 0.4f, bDrop + 0.4f)
                .At(bGrab - 0.4f, trailB + new Vector2(100f, 100f)).At(bGrab - 0.1f, trailB + new Vector2(0f, 24f)).Press(bGrab).At(bGrab, trailB + new Vector2(0f, 24f))
                .At(bDrop, millDrop + new Vector2(0f, 24f), DemoEase.Linear).Lift(bDrop + 0.05f).At(bDrop + 0.4f, millDrop + new Vector2(90f, 90f));

            // ---- the Woolly Lodge and its capybara, from the Gold Store (it walks a little slower)
            ScreensHome(m, cFrom, dFrom, "Gold Store", "gold_store", "Panda House", "Panda", 1.1f, 2500, 800);

            // ---- a zombie capybara: wrecking, tapped, paid for, bathed, and working
            float wreck1 = dFrom + 0.6f, wreck2 = dFrom + 1.15f, zTap = dFrom + 1.55f, talkFrom = dFrom + 1.75f;
            float payTap = dFrom + 3.1f, paid = dFrom + 3.25f, bathFrom = dFrom + 3.6f;
            float walkIn = bathFrom + 0.15f, atEdge = bathFrom + 0.7f, jump = bathFrom + 0.75f, splash = bathFrom + 1.0f;
            float warmFrom = bathFrom + 1.2f, warmTo = bathFrom + 1.9f, rise = bathFrom + 2.05f, landed = bathFrom + 2.3f, bathTo = bathFrom + 2.6f;
            float zGrab = bathTo + 0.45f, zDrop = bathTo + 1.2f, zGo = bathTo + 1.4f, zThere = zGo + 0.25f;
            float dig1 = zThere + 0.3f, dig2 = zThere + 0.9f, zLeave = zThere + 1.05f, zHome = zLeave + 0.25f;
            float L = zHome + 1.65f;
            m.Length = L;

            var wild = m.Group("Wild", Vector2.zero);
            During(m, wild, (dFrom, bathFrom), (bathTo, L));
            m.Island("Island", new Vector2(0f, -30f), new Vector2(570f, 290f), wild);
            var mineAt = new Vector2(-40f, -20f);
            m.Pic("Mine", HelpArt.Item("Mine"), mineAt, 115f, wild);
            m.Pic("Forest", pine, new Vector2(230f, -15f), 100f, wild);
            var victimAt = new Vector2(170f, -88f);
            var victim = m.Pic("Pine", pine, victimAt, 100f, wild);
            var victimSize = m.Size(victim.transform).At(0f, 0.42f);
            var rockAt = new Vector2(120f, -150f);
            var rock = m.Pic("Rock", HelpArt.Planted("Medium Gray Hill", "Small Gray Hill"), rockAt, 90f, wild);
            var rockSize = m.Size(rock.transform).At(0f, 0.55f);
            var (_, youD) = Walker(m, "You", HelpArt.You, new Vector2(-215f, -118f), 74f, wild);
            m.Face(youD).At(0f, -1f);
            var trailD = new Vector2(-270f, -134f);
            var gemCount = m.Pill("Gems", HelpArt.Resource(ResourceKind.Gem), "", GemPill, wild);
            m.Tally(gemCount, 25, (paid, 25 - MonsterAnimal.TameCost));

            // Wild: at a crawl up to a little tree, and two blows that leave nothing of it.
            var zombieArt = HelpArt.Item("Monster");
            var zAt = victimAt + new Vector2(-50f, -6f);
            var zombie = ScreensCrew(m, wild, zAt + new Vector2(-60f, -8f), 64f, zombieArt, null, null);
            m.Move(zombie.Holder).At(dFrom, zAt + new Vector2(-60f, -8f)).At(dFrom + 0.45f, zAt, DemoEase.Linear);
            m.Face(zombie.Body).At(0f, -1f);
            Swings(m, zombie, Array.Empty<float>(), new[] { wreck1, wreck2 });
            Bitten(victimSize, 0.42f, wreck1, wreck2);
            Rock(m, victim, wreck1, wreck2);
            m.Between(zombie.Holder.gameObject, 0f, paid + 0.3f);
            m.Sparkle(zAt + new Vector2(0f, 40f), paid, Color.white, 8);

            // Tapped, it speaks: its picture, its name, the line, and Pay 20 Gems or Cancel.
            var talkAt = new Vector2(0f, -95f);
            var talk = Panel(m, talkAt, new Vector2(584f, 202f), null, talkFrom, paid);
            m.Shape("Frame", UiKit.Circle, new Vector2(-232f, 38f), new Vector2(96f, 96f), UiKit.SurfaceSunk, talk);
            m.Shape("Face", zombieArt, new Vector2(-232f, 38f), new Vector2(70f, 70f), Color.white, talk).preserveAspect = true;
            LeftLabel(m, talk, StoryText.Name("Monster"), new Vector2(-170f, 70f), 26f, UiKit.PrimaryDark, 380f);
            ScreensPara(m, talk, "Grrrnnh... so cold... (A warm bath costs 20 gems. Afterwards it'll happily work for you.)",
                new Vector2(-170f, 50f), new Vector2(400f, 64f), 16f, UiKit.Ink);
            var payAt = new Vector2(-40f, -58f);
            WordButton(m, talk, payAt, new Vector2(230f, 52f), UiKit.Primary, "Pay 20 Gems", Color.white);
            WordButton(m, talk, new Vector2(170f, -58f), new Vector2(160f, 52f), UiKit.SurfaceSunk, "Cancel", UiKit.Ink);

            // The warm bath (BathCutscene): shivering up to the spring, in with a splash, sinking to
            // its chin while the cold melts away, and out again warm.
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
            Vector2 P(float fx, float fy) => new Vector2((fx - 0.5f) * 600f, (fy - 0.5f) * 400f);
            var edge = P(0.3f, 0.13f);
            var inPool = P(0.45f, 0.22f);
            var outAt = P(0.62f, 0.13f);
            float waterY = P(0f, 0.31f).y;
            var cold = new Color(0.72f, 0.82f, 0.95f);
            var warm = new Color(1f, 0.88f, 0.82f);

            var dry = m.Pic("Zombie", zombieArt, edge, 100f, bath);
            m.Face(dry).At(0f, -1f);
            m.Move(dry.transform).At(walkIn, P(-0.05f, 0.13f)).At(atEdge, edge, DemoEase.Linear)
                .At(jump, edge).At(jump + 0.12f, Vector2.Lerp(edge, inPool, 0.5f) + new Vector2(0f, 50f), DemoEase.Out).At(splash, inPool, DemoEase.In)
                .At(rise, inPool).At(rise + 0.12f, Vector2.Lerp(inPool, outAt, 0.5f) + new Vector2(0f, 50f), DemoEase.Out).At(landed, outAt, DemoEase.In);
            m.Do(s => dry.color = s < rise ? cold : warm);
            During(m, dry, (0f, splash), (rise, 999f));

            // In the water: only what's above the surface shows.
            var surface = new Vector2(0f, waterY + 160f);
            var water = m.Group("Water", surface, bath);
            water.sizeDelta = new Vector2(640f, 320f);
            water.gameObject.AddComponent<RectMask2D>();
            var wet = m.Pic("Soaking", zombieArt, inPool - surface, 100f, water);
            m.Face(wet).At(0f, -1f);
            m.Move(wet.transform).At(splash, inPool - surface).At(warmFrom, inPool - surface + new Vector2(0f, -22f), DemoEase.Out)
                .At(rise - 0.15f, inPool - surface + new Vector2(0f, -22f)).At(rise, inPool - surface);
            var warming = new DemoKeys().At(warmFrom, 0f).At(warmTo, 1f);
            m.Do(s => wet.color = Color.Lerp(cold, warm, warming.Eval(s).x));
            During(m, wet, (splash, rise));
            m.Shape("Bar", null, new Vector2(0f, 184f), new Vector2(640f, 40f), Color.black, bath);
            m.Shape("Bar", null, new Vector2(0f, -184f), new Vector2(640f, 40f), Color.black, bath);
            m.Sparkle(new Vector2(inPool.x, waterY), splash, UiKit.Hex("BFEAF0"), 8);
            m.Sparkle(new Vector2(inPool.x + 10f, waterY + 30f), warmFrom + 0.2f, Color.white, 4);
            m.Sparkle(new Vector2(inPool.x - 10f, waterY + 40f), warmFrom + 0.55f, Color.white, 4);
            m.FrameFill(s => s >= bathFrom && s < bathTo ? Color.black : HelpArt.Sea);

            // Cured, warm and in your trail: dragged onto the mine, it digs, quick on rock.
            var happyArt = HobbyData.Art("Monster_Happy");
            if (happyArt == null) happyArt = zombieArt;
            var mineDoor = mineAt + new Vector2(30f, -55f);
            var atRock = rockAt + new Vector2(-46f, -2f);
            var tame = ScreensCrew(m, wild, trailD, 62f, happyArt, HelpArt.Tool(ResourceKind.Gold), null);
            m.Between(tame.Holder.gameObject, bathTo, 999f);
            m.Move(tame.Holder).At(zGrab, trailD).At(zDrop, mineAt + new Vector2(0f, 10f), DemoEase.Linear).At(zDrop + 0.15f, mineDoor)
                .At(zGo, mineDoor).At(zThere, atRock, DemoEase.Linear).At(zLeave, atRock).At(zHome, mineDoor, DemoEase.Linear);
            m.Size(tame.Holder).At(zDrop, 1f).At(zDrop + 0.15f, 1.25f).At(zDrop + 0.35f, 1f);
            m.Face(tame.Body).At(0f, -1f).At(zLeave, 1f, DemoEase.Step);
            m.Bob(tame.Body, 6f, 14f, s => (s > zGo && s < zThere) || (s > zLeave && s < zHome));
            During(m, tame.Tool, (zGo, zLeave));
            Swings(m, tame, Array.Empty<float>(), new[] { dig1, dig2 });
            Bitten(rockSize, 0.55f, dig1, dig2);
            Rock(m, rock, dig1, dig2);
            m.FloatUp(HelpArt.Resource(ResourceKind.Gold), "+" + rockPays, mineDoor + new Vector2(0f, 120f), zHome + 0.05f);

            m.Hand().Show(zTap - 0.5f, payTap + 0.35f)
                .At(zTap - 0.5f, zAt + new Vector2(120f, 110f)).At(zTap - 0.15f, zAt + new Vector2(0f, 34f)).Tap(zTap)
                .At(payTap - 0.35f, talkAt + payAt).Tap(payTap)
                .At(payTap + 0.35f, talkAt + payAt + new Vector2(90f, -40f));
            m.Hand().Show(zGrab - 0.4f, zDrop + 0.4f)
                .At(zGrab - 0.4f, trailD + new Vector2(100f, 100f)).At(zGrab - 0.1f, trailD + new Vector2(0f, 24f)).Press(zGrab).At(zGrab, trailD + new Vector2(0f, 24f))
                .At(zDrop, mineAt + new Vector2(0f, 34f), DemoEase.Linear).Lift(zDrop + 0.05f).At(zDrop + 0.4f, mineAt + new Vector2(90f, 100f));

            // A blink of darkness at each change of scene.
            var dip = m.Shape("Dip", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("2E1A0E"));
            var fade = m.Fade(dip).At(0f, 1f).At(0.3f, 0f);
            foreach (var cut in new[] { bFrom, cFrom, dFrom, bathFrom, bathTo }) fade.At(cut - 0.25f, 0f).At(cut, 1f).At(cut + 0.25f, 0f);
            fade.At(L - 0.35f, 0f).At(L, 1f);
        }

        /// <summary>
        /// Buying a capybara home and its capybara, as the shops do it: tap the home where the store
        /// sets it out, Purchase, and it pops into your trail; drag it out and put it down. Its
        /// capybara stands in front of it for sale (StoreBuilding's house ware); tap it, Purchase, and
        /// it walks over to join your trail (taking <paramref name="walk"/> seconds).
        /// </summary>
        static void ScreensHome(HelpMovie m, float t0, float end, string store, string listKey, string house, string animal, float walk, int wood0, int gold0)
        {
            float tap = t0 + 0.95f, card = t0 + 1.15f, buy = t0 + 1.85f, bought = t0 + 2.0f, grab = t0 + 2.8f, drop = t0 + 3.6f;
            float ware = t0 + 3.9f, tap2 = t0 + 4.55f, card2 = t0 + 4.75f, buy2 = t0 + 5.45f, bought2 = t0 + 5.6f, home = bought2 + walk;

            var scene = m.Group(house, Vector2.zero);
            During(m, scene, (t0, end));
            m.Island("Island", new Vector2(0f, -35f), new Vector2(570f, 270f), scene);
            var buyAt = ScreensShop(m, scene, store, listKey, new Vector2(-165f, -10f), house);

            // The home, once it's down.
            var spot = new Vector2(150f, -40f);
            var houseArt = HelpArt.Item(house);
            var placed = m.Pic("House", houseArt, spot, 125f, scene);
            m.Size(placed.transform).At(drop - 0.01f, 0f).At(drop, 0.3f, DemoEase.Step).At(drop + 0.35f, 1f, DemoEase.Back);
            m.Sparkle(spot + new Vector2(0f, 40f), drop, UiKit.Sun, 5);

            // Its capybara, for sale in front of it.
            var animalArt = HelpArt.Item(animal);
            var wareAt = spot + new Vector2(0f, -64f);
            var stand = m.Group("Ware", wareAt, scene);
            var standPic = m.Pic("Pic", animalArt, Vector2.zero, 46f, stand);
            m.Do(s => stand.anchoredPosition = wareAt + new Vector2(0f, 3f + Mathf.Sin(s * 2.4f) * 3f));
            m.Size(standPic.transform).At(ware - 0.01f, 0f).At(ware, 0.3f, DemoEase.Step).At(ware + 0.3f, 1f, DemoEase.Back);
            m.Between(stand.gameObject, 0f, bought2);
            m.Sparkle(wareAt + new Vector2(0f, 24f), ware, UiKit.Sun, 4);

            // You, facing right, with what you bought popping into your trail behind you.
            var (_, youBody) = Walker(m, "You", HelpArt.You, new Vector2(-50f, -150f), 74f, scene);
            m.Face(youBody).At(0f, -1f);
            var trail = new Vector2(-110f, -162f);
            ScreensCarried(m, scene, houseArt, trail, spot, bought, grab, drop);
            var (pet, petBody) = Walker(m, animal, animalArt, wareAt, 60f, scene);
            var behind = new Vector2(-112f, -150f);
            m.Move(pet).At(bought2, wareAt).At(home, behind, DemoEase.Linear);
            m.Bob(petBody, 6f, 12f, s => s > bought2 && s < home);
            m.Between(pet.gameObject, bought2, end);

            // The counters: the home's price, then its capybara's.
            var price1 = StoreData.Price(house);
            var price2 = StoreData.Price(animal);
            var woodCount = m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "", WoodPill, scene);
            var goldCount = m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "", GoldPill, scene);
            m.Tally(woodCount, wood0, (bought, wood0 - price1.x), (bought2, wood0 - price1.x - price2.x));
            m.Tally(goldCount, gold0, (bought, gold0 - price1.y), (bought2, gold0 - price1.y - price2.y));
            var purchase = BuyCard(m, new Vector2(40f, 28f), house, houseArt, card, bought);
            var purchase2 = BuyCard(m, new Vector2(-30f, 40f), animal, animalArt, card2, bought2);

            m.Hand().Show(t0 + 0.35f, home)
                .At(t0 + 0.35f, buyAt + new Vector2(130f, 110f)).At(tap - 0.15f, buyAt + new Vector2(0f, 20f)).Tap(tap)
                .At(buy - 0.2f, purchase).Tap(buy)
                .At(grab - 0.2f, trail + new Vector2(0f, 20f)).Press(grab).At(grab, trail + new Vector2(0f, 20f))
                .At(drop, spot + new Vector2(0f, 20f), DemoEase.Linear).Lift(drop + 0.05f)
                .At(tap2 - 0.25f, wareAt + new Vector2(0f, 24f)).Tap(tap2)
                .At(buy2 - 0.2f, purchase2).Tap(buy2)
                .At(home, purchase2 + new Vector2(90f, -70f));
        }

        /// <summary>A shop with its wares in a half circle in front of it, east round to west (StoreBuilding). Returns where <paramref name="target"/> stands.</summary>
        static Vector2 ScreensShop(HelpMovie m, Transform parent, string store, string listKey, Vector2 at, string target)
        {
            m.Pic("Store", HelpArt.Item(store), at, 125f, parent);
            var names = StoreData.Lists.TryGetValue(listKey, out var list) ? list : new[] { target };
            var found = at + new Vector2(0f, -68f);
            for (int i = 0; i < names.Length; i++)
            {
                float a = (names.Length > 1 ? i * 180f / (names.Length - 1) : 90f) * Mathf.Deg2Rad;
                var spot = at + new Vector2(Mathf.Cos(a) * 108f, -Mathf.Sin(a) * 68f);
                var art = HelpArt.Item(names[i]);
                if (art != null) Ware(m, art, spot, 44f, i * 1.3f, parent);
                if (names[i] == target) found = spot;
            }
            return found;
        }

        /// <summary>Something bought, popping into the trail behind you, then dragged out to its spot (the green ring under it while it's carried).</summary>
        static void ScreensCarried(HelpMovie m, Transform parent, Sprite art, Vector2 trail, Vector2 to, float from, float take, float put)
        {
            var item = m.Group("Carried", trail, parent);
            var pic = m.Pic("Pic", art, Vector2.zero, 56f, item);
            var ring = PlaceRing(m, item, true, 110f);
            m.Fade(ring).At(take, 0f).At(take + 0.15f, 0.45f).At(put, 0.45f).At(put + 0.1f, 0f);
            m.Move(item).At(take, trail).At(put, to, DemoEase.Linear);
            m.Size(pic.transform).At(from, 0f).At(from + 0.3f, 1f, DemoEase.Back);
            m.Fade(item).At(from - 0.01f, 0f).At(from, 1f, DemoEase.Step).At(put - 0.01f, 1f).At(put, 0f, DemoEase.Step);
            m.Sparkle(trail + new Vector2(0f, 24f), from, UiKit.Sun, 5);
        }

        /// <summary>A worker of any kind, as <see cref="Capy"/> draws one, inside <paramref name="parent"/>: its body, the tool at its paw, what it carries.</summary>
        static Crew ScreensCrew(HelpMovie m, Transform parent, Vector2 at, float size, Sprite body, Sprite tool, Sprite load)
        {
            var c = new Crew { Holder = m.Group("Worker", at, parent) };
            c.Body = m.Pic("Body", body != null ? body : HelpArt.Worker, Vector2.zero, size, c.Holder);
            c.Paw = m.Group("Paw", new Vector2(-size * 0.27f, -size * 0.12f), c.Body.transform);
            if (tool != null)
            {
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
    }
}
