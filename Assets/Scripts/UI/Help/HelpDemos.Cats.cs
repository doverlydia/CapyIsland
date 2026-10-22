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
        /// <summary>
        /// Making friends with a stray, as CatCare plays it. Down at the water you catch a fish (it
        /// goes straight into your bag). Back on the island a stray wanders up; tap it and it speaks
        /// ("Stray cat · Trust 0/10", Pet / Give a fish (1) / Leave): give it the fish, and a star
        /// pops over its head (+3 trust). A while later (each cat can be fussed once every few
        /// minutes) tap it again: out of fish, so Pet, and a heart (+1). Catch another fish, come
        /// back, give it that (+3): at 7 it trusts you enough, and next time Adopt! is there. Tap it,
        /// and the card says it's yours and has made its home right where it was. Tap your cat now
        /// and it purrs, with Dress up and Rename among its choices.
        /// </summary>
        static void CatsMovie(HelpMovie m)
        {
            const float L = 22.5f;
            m.Length = L;

            var kind = HobbyData.CatById("Ginger");
            string name = kind != null ? kind.Name : "Marmalade";
            string personality = kind != null ? kind.Personality : "";
            var sit = HelpArt.Picture("Cat_Ginger");
            var walk = HelpArt.Picture("Cat_Ginger_Walk");
            if (walk == null) walk = sit;
            var fishKind = HobbyData.FishById("salmon");
            var fishArt = HelpArt.Picture("Fish_salmon");
            string caught = "You caught " + HobbyData.WithArticle(fishKind != null ? fishKind.Name : "Salmon") + "!";

            // The cuts: beach, island (two visits), beach again, island (two visits and the adoption).
            const float d1 = 2.95f, d2 = 7.1f, d3 = 10.25f, d4 = 12.75f, d5 = 15.9f;
            // The two catches.
            const float nib1 = 0.6f, bite1 = 0.9f, leap1 = 1.5f, cardFrom1 = 1.75f, cardShut1 = 2.6f;
            const float nib2 = d3 + 0.3f, bite2 = d3 + 0.55f, leap2 = d3 + 1.15f, cardFrom2 = d3 + 1.4f, cardShut2 = d3 + 2.2f;
            // The stray wandering up the first time.
            const float walkFrom = d1 + 0.3f, walkTo = d1 + 1.2f;

            // ---- the beach: you fishing from the sand, the rod out over the sea (as the contest movie draws it)
            var beach = m.Group("Beach", Vector2.zero);
            During(m, beach, (0f, d1), (d3, d4));
            m.Island("Shore", new Vector2(0f, 80f), new Vector2(600f, 230f), beach);
            m.Pic("Tree", HelpArt.Item("Medium Pine Forest"), new Vector2(200f, 95f), 100f, beach);
            var feet = new Vector2(-90f, -52f);
            const float rodLength = 110f;
            var rodBase = feet + new Vector2(30f, 26f);
            var rod = m.Shape("Rod", null, rodBase, new Vector2(6f, rodLength), UiKit.Hex("8A5A32"), beach);
            rod.rectTransform.pivot = new Vector2(0.5f, 0f);
            rod.rectTransform.anchoredPosition = rodBase;
            rod.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -58f);
            var angler = m.Pic("You", HelpArt.You, feet, 96f, beach);
            m.Face(angler).At(0f, -1f);
            var rodTip = rodBase + (Vector2)(Quaternion.Euler(0f, 0f, -58f) * new Vector3(0f, rodLength, 0f));
            var bobAt = new Vector2(90f, -140f);
            // The line and bobber, gone from the bite until you cast again.
            var tackle = m.Group("Tackle", Vector2.zero, beach);
            During(m, tackle, (0f, bite1), (d3, bite2));
            var fishingLine = m.Shape("Line", null, (rodTip + bobAt) * 0.5f, new Vector2(2f, Vector2.Distance(rodTip, bobAt)), new Color(1f, 1f, 1f, 0.9f), tackle);
            fishingLine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, bobAt - rodTip));
            var bobber = m.Shape("Bobber", UiKit.Circle, bobAt, new Vector2(16f, 16f), UiKit.Danger, tackle);
            m.Do(now => bobber.rectTransform.anchoredPosition = bobAt + new Vector2(0f, Mathf.Sin(now * 3f) * 3f));

            // A nibble, the bite, and the fish up out of the water.
            WaterRing(m, bobAt, nib1, 0.6f);
            WaterRing(m, bobAt, nib2, 0.6f);
            Splash(m, bobAt, bite1);
            Splash(m, bobAt, bite2);
            foreach (var (hop, shown) in new[] { (leap1, cardFrom1), (leap2, cardFrom2) })
            {
                var jump = m.Pic("Catch", fishArt, bobAt, 90f);
                m.Move(jump.transform).At(hop, bobAt + new Vector2(0f, -24f)).At(hop + 0.35f, new Vector2(30f, -40f), DemoEase.Out);
                m.Size(jump.transform).At(hop, 0.5f).At(hop + 0.3f, 1f, DemoEase.Back);
                m.Turn(jump.transform).At(hop + 0.3f, 0f).At(hop + 0.4f, -8f).At(hop + 0.5f, 6f).At(hop + 0.6f, 0f);
                m.Fade(jump).At(hop - 0.01f, 0f).At(hop, 1f, DemoEase.Step).At(shown, 1f).At(shown + 0.1f, 0f);
                m.Sparkle(new Vector2(30f, 10f), hop + 0.25f, Color.white, 5);
            }

            // ---- the island: you on the left, the stray wandering about on the right
            var lawn = m.Group("Lawn", Vector2.zero);
            During(m, lawn, (d1, d3), (d4, L));
            m.Island("Island", new Vector2(0f, 40f), new Vector2(600f, 260f), lawn);
            var pine = HelpArt.Item("Medium Pine Forest");
            m.Pic("Tree", pine, new Vector2(-60f, 100f), 90f, lawn);
            m.Pic("Tree", pine, new Vector2(235f, 80f), 84f, lawn);
            var you = m.Pic("You", HelpArt.You, new Vector2(-170f, -4f), 104f, lawn);
            m.Face(you).At(0f, -1f);

            // Where the cat is sitting at each visit: a stray potters about between them.
            Vector2 c1 = new(115f, 2f), c2 = new(50f, -10f), c3 = new(150f, 12f), c4 = new(85f, -4f);
            var enter = new Vector2(285f, 22f);
            var cat = m.Group("Cat", enter, lawn);
            var catSit = m.Pic("Sit", sit, Vector2.zero, 74f, cat);
            var catWalk = m.Pic("Walk", walk, Vector2.zero, 92f, cat);
            During(m, catWalk, (walkFrom, walkTo));
            During(m, catSit, (walkTo, L));
            m.Bob(catWalk, 4f, 12f, now => now > walkFrom && now < walkTo);
            m.Move(cat).At(0f, enter).At(walkFrom, enter).At(walkTo, c1, DemoEase.Linear)
                .At(d2, c2, DemoEase.Step).At(d4, c3, DemoEase.Step).At(d5, c4, DemoEase.Step);
            m.Fade(cat).At(walkFrom, 0f).At(walkFrom + 0.7f, 1f);
            var punch = m.Size(catSit.transform).At(0f, 1f);

            string stray = "Stray cat", watching = "A stray cat. It watches you carefully.";
            string CatTitle(string who, int trust) => who + "  ·  Trust " + trust + "/" + CatCare.MaxBond;
            string FishChoice(int inBag) => "Give a fish (" + inBag + ")";

            // One visit: the finger taps the cat, its window opens, the finger taps a choice (none
            // for pick < 0: the window just stays a moment), and it shuts. Returns when it shut.
            float Meet(float tap, Vector2 spot, string title, string body, string[] words, bool[] on, int pick, float stay)
            {
                float open = tap + 0.15f, choose = open + 1.0f;
                float shut = pick >= 0 ? choose + 0.08f : open + stay;
                var buttons = CatSpeech(m, sit, title, body, personality, words, on, open, shut);
                var touch = spot + new Vector2(0f, 32f);
                var finger = m.Hand().Show(tap - 0.4f, pick >= 0 ? choose + 0.35f : tap + 0.55f)
                    .At(tap - 0.4f, touch + new Vector2(130f, 70f)).At(tap - 0.08f, touch).Tap(tap);
                if (pick >= 0)
                    finger.At(open + 0.3f, touch).At(choose - 0.22f, buttons[pick]).Tap(choose)
                        .At(choose + 0.35f, buttons[pick] + new Vector2(90f, 60f));
                else finger.At(tap + 0.55f, touch + new Vector2(120f, 80f));
                return shut;
            }

            // What the cat does when it's fussed: a little hop and an emoji over its head.
            void Fussed(float shut, Vector2 spot, Game.Emoji e)
            {
                float at = shut + 0.12f;
                CatEmojiPop(m, lawn, spot + new Vector2(0f, 72f), e, at, shut + 1.1f);
                punch.At(at, 1f).At(at + 0.1f, 1.15f).At(at + 0.35f, 1f);
            }

            // Visit 1: the fish you just caught. A fish is worth 3 trust.
            float shutA = Meet(d1 + 1.55f, c1, CatTitle(stray, 0), watching,
                new[] { "Pet", FishChoice(1), "Leave" }, new[] { true, true, true }, 1, 0f);
            Fussed(shutA, c1, Game.Emoji.Star);

            // Visit 2, a while later: no fish left (greyed), so a pet. A pet is worth 1.
            float shutB = Meet(d2 + 0.55f, c2, CatTitle(stray, 3), watching,
                new[] { "Pet", FishChoice(0), "Leave" }, new[] { true, false, true }, 0, 0f);
            Fussed(shutB, c2, Game.Emoji.Heart);

            // Visit 3, after another catch: the fish again, and that makes 7 (it trusts you at 6).
            float shutC = Meet(d4 + 0.55f, c3, CatTitle(stray, 4), watching,
                new[] { "Pet", FishChoice(1), "Leave" }, new[] { true, true, true }, 1, 0f);
            Fussed(shutC, c3, Game.Emoji.Star);

            // Visit 4: Adopt! is among the choices now.
            float shutD = Meet(d5 + 0.55f, c4, CatTitle(stray, 7), watching,
                new[] { "Pet", FishChoice(0), "Adopt!", "Leave" }, new[] { true, false, true, true }, 2, 0f);
            float adoptCard = shutD + 0.35f, adoptShut = adoptCard + 1.4f;
            m.Sparkle(c4 + new Vector2(0f, 40f), shutD + 0.07f, UiKit.Sun, 8);
            CatEmojiPop(m, lawn, c4 + new Vector2(0f, 72f), Game.Emoji.Heart, shutD + 0.1f, adoptShut + 0.2f);

            // The cards: each catch, then the cat that's yours now.
            var tapFish1 = CatCardPop(m, fishArt, caught, null, null, cardFrom1, cardShut1);
            var tapFish2 = CatCardPop(m, fishArt, caught, null, null, cardFrom2, cardShut2);
            var tapAdopt = CatCardPop(m, sit, name + " is yours!", personality, name + " has made a home right here.", adoptCard, adoptShut);

            // Your cat now: it purrs, and can be dressed up and renamed.
            float tapE = adoptShut + 0.95f;
            Meet(tapE, c4, CatTitle(name, 7), "<b>" + name + "</b> purrs and bumps your hand.",
                new[] { "Pet", FishChoice(0), "Dress up", "Rename", "Leave" }, new[] { true, false, true, true, true }, -1, 1.4f);

            // A blink of darkness at each cut: off to the water, and back a little later.
            var dip = m.Shape("Dip", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("1E2A33"));
            var dark = m.Fade(dip).At(0f, 1f).At(0.3f, 0f);
            foreach (var cut in new[] { d1, d2, d3, d4, d5 }) dark.At(cut - 0.25f, 0f).At(cut, 1f).At(cut + 0.25f, 0f);
            dark.At(L - 0.35f, 0f).At(L, 1f);

            // Fishing: the finger held down while the fish is reeled in, then a tap to close the card.
            var hold = new Vector2(200f, -60f);
            void FingerCatch(float bite, float leap, float shut, Vector2 cardTap)
            {
                m.Hand().Show(bite - 0.15f, shut + 0.1f)
                    .At(bite - 0.15f, hold + new Vector2(70f, 80f)).At(bite + 0.05f, hold).Press(bite + 0.12f, false)
                    .At(leap, hold).Lift(leap)
                    .At(shut - 0.3f, cardTap).Tap(shut - 0.06f).At(shut + 0.3f, cardTap + new Vector2(100f, -70f));
            }
            FingerCatch(bite1, leap1, cardShut1, tapFish1);
            FingerCatch(bite2, leap2, cardShut2, tapFish2);
            m.Hand().Show(adoptCard + 0.4f, adoptShut + 0.2f)
                .At(adoptCard + 0.4f, tapAdopt + new Vector2(120f, -90f)).At(adoptShut - 0.3f, tapAdopt).Tap(adoptShut - 0.06f)
                .At(adoptShut + 0.3f, tapAdopt + new Vector2(110f, -70f));
        }

        /// <summary>
        /// The speech dialog as a cat opens it (SpeechDialog, as MobileHud styles it): a white card
        /// along the bottom, the cat's face in a disc at the top left, its name and trust in orange,
        /// its line under that, and the choices in a row along the bottom, the first one orange.
        /// A choice you can't take is greyed. Returns where each choice is on the stage.
        /// </summary>
        static Vector2[] CatSpeech(HelpMovie m, Sprite face, string title, string body, string more, string[] words, bool[] on, float from, float to)
        {
            var at = new Vector2(0f, -122f);
            var card = Panel(m, at, new Vector2(584f, 148f), null, from, to);
            m.Shape("PortraitDisc", UiKit.Circle, new Vector2(-244f, 24f), new Vector2(80f, 80f), UiKit.SurfaceSunk, card);
            var portrait = m.Shape("Portrait", face, new Vector2(-244f, 24f), new Vector2(70f, 70f), Color.white, card);
            portrait.preserveAspect = true;
            LeftLabel(m, card, title, new Vector2(-194f, 54f), 22f, UiKit.PrimaryDark, 470f).fontStyle = FontStyles.Bold;
            LeftLabel(m, card, body, new Vector2(-194f, 28f), 18f, UiKit.Ink, 470f);
            if (!string.IsNullOrEmpty(more)) LeftLabel(m, card, more, new Vector2(-194f, 6f), 14f, UiKit.Ink, 470f);

            const float row = 564f, gap = 8f;
            int n = words.Length;
            float w = (row - gap * (n - 1)) / n;
            var spots = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                var local = new Vector2(-row * 0.5f + w * 0.5f + i * (w + gap), -48f);
                bool live = on == null || i >= on.Length || on[i];
                var fill = i == 0 ? UiKit.Primary : UiKit.SurfaceSunk;
                // A Button's own disabled tint.
                if (!live) fill = new Color(fill.r * 0.784f, fill.g * 0.784f, fill.b * 0.784f, 0.5f);
                var ink = i == 0 ? Color.white : live ? UiKit.Ink : UiKit.InkSoft;
                WordButton(m, card, local, new Vector2(w, 38f), fill, words[i], ink);
                spots[i] = at + local;
            }
            return spots;
        }

        /// <summary>
        /// An emoji popping up over a head (EmojiBubbles): a white speech bubble with its little
        /// tail, the emoji in it, from <paramref name="from"/> to <paramref name="to"/>.
        /// <paramref name="at"/> is the tail's tip.
        /// </summary>
        static void CatEmojiPop(HelpMovie m, Transform parent, Vector2 at, Game.Emoji e, float from, float to)
        {
            var g = m.Group("Emoji", at, parent);
            var tail = m.Shape("Tail", IconPainter.Arrow, new Vector2(0f, 2f), new Vector2(22f, 22f), Color.white, g);
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            var bubble = m.Shape("Bubble", UiKit.Rounded(18), new Vector2(0f, 38f), new Vector2(68f, 62f), Color.white, g);
            bubble.type = Image.Type.Sliced;
            var icon = UiKit.Emoji(bubble.transform, e, 44f);
            icon.anchoredPosition = new Vector2(0f, 1f);
            m.Size(g).At(from, 0.2f).At(from + 0.3f, 1f, DemoEase.Back).At(to, 1f).At(to + 0.2f, 0f, DemoEase.In);
            m.Fade(g).At(from - 0.01f, 0f).At(from, 1f, DemoEase.Step);
        }

        /// <summary>
        /// The card that pops up in the middle of a dimmed screen (CardPopup): a golden border, the
        /// picture, the title, a line or two, and "tap to continue". Returns where to tap it.
        /// </summary>
        static Vector2 CatCardPop(HelpMovie m, Sprite art, string title, string line1, string line2, float from, float to)
        {
            var dim = m.Shape("Scrim", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Scrim);
            m.Fade(dim).At(from - 0.15f, 0f).At(from, UiKit.Scrim.a).At(to, UiKit.Scrim.a).At(to + 0.2f, 0f);
            var at = new Vector2(0f, 6f);
            var card = Panel(m, at, new Vector2(256f, 330f), null, from, to);
            var border = m.Shape("Border", UiKit.Rounded(20), Vector2.zero, new Vector2(240f, 314f), UiKit.Sun, card);
            border.type = Image.Type.Sliced;
            var inner = m.Shape("Inner", UiKit.Rounded(16), Vector2.zero, new Vector2(230f, 304f), UiKit.Surface, card);
            inner.type = Image.Type.Sliced;
            m.Pic("Picture", art, new Vector2(0f, 4f), 138f, card);
            var head = m.Label("Title", title, new Vector2(0f, -36f), 22f, UiKit.Ink, card);
            head.fontStyle = FontStyles.Bold;
            Fit(head, title, 212f);
            float y = -68f;
            foreach (var words in new[] { line1, line2 })
            {
                if (string.IsNullOrEmpty(words)) continue;
                var said = m.Label("Body", words, new Vector2(0f, y), 14f, UiKit.Ink, card);
                Fit(said, words, 212f);
                y -= 20f;
            }
            var hint = m.Label("Tap", "tap to continue", new Vector2(0f, -136f), 13f, UiKit.InkSoft, card);
            hint.fontStyle = FontStyles.Italic;
            return at + new Vector2(0f, -20f);
        }
    }
}
