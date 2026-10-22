using System;
using System.Collections.Generic;
using System.Reflection;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    public static partial class HelpDemos
    {
        // ---------------------------------------------------------------- sewing

        /// <summary>
        /// Sewing a hat, as the game does it: tap the sewing machine on the island and the sewing
        /// room opens (SewingScreen, the wide layout): the mannequin on its rug wearing the plain
        /// beanie you're about to make, and the workbench down the right with the patterns (the
        /// Beanie takes 3 cotton, and you have them), the colours, the tools and the stickers. Pick
        /// teal, take the Fill tool and tap the crown; pick pink and tap the brim; pick the star
        /// sticker and tap it onto the crown. Sew &amp; wear: the cotton is used up, the hat joins
        /// your sewing, and when you close the room you're on the island wearing it.
        /// </summary>
        static void SewingMovie(HelpMovie m)
        {
            const float L = 13.6f;
            m.Length = L;
            const float tapMachine = 1.0f, open = 1.25f;
            const float tapBeanie = 2.1f, tapTeal = 2.9f, tapFill = 3.6f, tapCrown = 4.4f, tapPink = 5.2f, tapBrim = 6.0f;
            const float tapStar = 6.9f, tapStamp = 7.7f, tapSew = 8.7f, tapClose = 10.0f, shut = 10.2f;

            // The hat at each step: plain fabric, the crown filled, the brim filled, the star on.
            var stages = CraftHatStages();
            float[] changes = { tapCrown, tapBrim, tapStamp };
            var finished = stages[stages.Length - 1];

            // ---- the island: the machine set down, and you
            m.Island("Island", new Vector2(0f, -40f), new Vector2(570f, 270f));
            var machineAt = new Vector2(-120f, -75f);
            var machineArt = HelpArt.Item("Sewing Machine");
            if (machineArt == null) machineArt = HobbyData.Art("SewingMachine");
            var machine = m.Pic("SewingMachine", machineArt, machineAt, 130f);
            m.Size(machine.transform).At(tapMachine, 1f).At(tapMachine + 0.08f, 1.08f).At(tapMachine + 0.22f, 1f);
            var feet = new Vector2(75f, -165f);
            CraftWearing(m, feet, 125f, finished, shut);

            // ---- the sewing room
            var screen = m.Group("SewingScreen", Vector2.zero);
            During(m, screen, (open, shut + 0.2f));
            m.Fade(screen).At(open - 0.01f, 0f).At(open + 0.2f, 1f).At(shut, 1f).At(shut + 0.2f, 0f);
            m.Shape("Room", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("E9C9B8"), screen);

            // The painted room, its rug under the mannequin's feet (SewingScreen hangs it from 0.3 up).
            var figureAt = new Vector2(-140f, -20f);
            const float figure = 240f;
            var room = HobbyData.Art("Sew_Room");
            if (room != null)
            {
                float aspect = room.rect.width / room.rect.height;
                float tall = Mathf.Max(500f, 340f / aspect);
                var wall = m.Shape("Painting", room, Vector2.zero, new Vector2(tall * aspect, tall), Color.white, screen);
                wall.rectTransform.pivot = new Vector2(0.5f, 0.3f);
                wall.rectTransform.anchoredPosition = new Vector2(figureAt.x, figureAt.y - figure * 0.46f);
            }

            // The mannequin with the hat you're making on its head (HatOnHead 0.84 up, HatWidth 0.55 across).
            var mannequin = m.Shape("Mannequin", HobbyData.Art("Sew_Mannequin"), figureAt, new Vector2(figure, figure), Color.white, screen);
            mannequin.preserveAspect = true;
            var hatAt = figureAt + new Vector2(0f, (0.84f - 0.5f) * figure);
            float hatSize = 0.55f * figure, half = hatSize * 0.5f;
            for (int i = 0; i < stages.Length; i++)
            {
                var hat = m.Shape("Hat", stages[i], hatAt, new Vector2(hatSize, hatSize), Color.white, screen);
                hat.preserveAspect = true;
                m.Between(hat.gameObject, i == 0 ? 0f : changes[i - 1], i == stages.Length - 1 ? L + 1f : changes[i]);
            }
            var crownTap = hatAt + new Vector2(-0.25f, 0.12f) * half;
            var brimTap = hatAt + new Vector2(-0.45f, -0.33f) * half;
            var stampTap = hatAt + new Vector2((CraftStarX + 0.5f) / SewingScreen.Size * 2f - 1f, (CraftStarY + 0.5f) / SewingScreen.Size * 2f - 1f) * half;

            // The title in its dark chip at the top left, the close button at the top right.
            var chip = m.Shape("TitleChip", UiKit.Rounded(40), new Vector2(-218f, 172f), new Vector2(140f, 44f), UiKit.HudGlass, screen);
            chip.type = Image.Type.Sliced;
            m.Shape("Icon", IconPainter.Hat, new Vector2(-46f, 0f), new Vector2(28f, 28f), UiKit.Hex("FFC2D6"), chip.transform).preserveAspect = true;
            LeftLabel(m, chip.transform, "Sewing", new Vector2(-26f, 0f), 22f, Color.white, 90f);
            var closeAt = new Vector2(272f, 174f);
            var close = m.Shape("Close", UiKit.Circle, closeAt, new Vector2(40f, 40f), UiKit.HudGlass, screen);
            m.Shape("X", IconPainter.Close, Vector2.zero, new Vector2(18f, 18f), Color.white, close.transform);

            // + − Fit, at the stage's top corner.
            string[] zoom = { "+", "−", "Fit" };
            for (int i = 0; i < zoom.Length; i++)
            {
                var disc = m.Shape("Zoom", UiKit.Circle, new Vector2(-12f, 132f - i * 32f), new Vector2(28f, 28f), new Color(1f, 1f, 1f, 0.92f), screen);
                m.Label("Label", zoom[i], Vector2.zero, i < 2 ? 22f : 11f, UiKit.Ink, disc.transform);
            }

            // ---- the workbench
            var benchAt = new Vector2(158f, -24f);
            var bench = m.Card("Workbench", benchAt, new Vector2(264f, 340f), null, screen);
            Vector2 B(Vector2 local) => benchAt + local;

            // The patterns, each with what it takes and how much of it you have.
            string[] patterns = { "Beanie", "Fedora", "Sun Hat" };
            var cotton = HobbyData.ItemArt("crop:Cotton");
            var silk = HobbyData.ItemArt("mat:Silk");
            var beanieAt = new Vector2(-86f, 138f);
            for (int i = 0; i < patterns.Length; i++)
            {
                var at = beanieAt + new Vector2(i * 86f, 0f);
                bool chosen = i == 0;
                var face = m.Shape(patterns[i], UiKit.Rounded(18), at, new Vector2(80f, 54f), chosen ? UiKit.Primary : UiKit.SurfaceSunk, bench);
                face.type = Image.Type.Sliced;
                var ink = chosen ? Color.white : UiKit.Ink;
                Fit(m.Label("Name", patterns[i], new Vector2(0f, 11f), 16f, ink, face.transform), patterns[i], 72f);
                if (i == 0) CraftNeed(m, face.transform, new Vector2(0f, -12f), cotton, 3, 0, 3, tapSew + 0.05f, ink);
                else if (i == 1) CraftNeed(m, face.transform, new Vector2(0f, -12f), silk, 0, 0, 3, tapSew + 0.05f, ink);
                else
                {
                    CraftNeed(m, face.transform, new Vector2(-18f, -12f), cotton, 2, 0, 2, tapSew + 0.05f, ink);
                    CraftNeed(m, face.transform, new Vector2(18f, -12f), silk, 0, 0, 2, tapSew + 0.05f, ink);
                }
            }

            // The palette: twelve colours and the eraser, the chosen one a little bigger (red to start).
            var palette = SewingScreen.Palette;
            float Chosen(int i, int colour) => i == colour ? 1.18f : 0.92f;
            Vector2 tealAt = Vector2.zero, pinkAt = Vector2.zero;
            for (int i = 0; i <= palette.Length; i++)
            {
                int row = i / 7, col = i % 7;
                int inRow = row == 0 ? 7 : palette.Length + 1 - 7;
                var at = new Vector2((col - (inRow - 1) * 0.5f) * 34f, 86f - row * 32f);
                bool eraser = i == palette.Length;
                var sw = m.Shape(eraser ? "Eraser" : "Swatch", UiKit.Circle, at, new Vector2(28f, 28f), eraser ? UiKit.SurfaceSunk : palette[i], bench);
                if (eraser) m.Shape("X", IconPainter.Close, Vector2.zero, new Vector2(14f, 14f), UiKit.InkSoft, sw.transform);
                else m.Shape("Ring", IconPainter.Ring, Vector2.zero, new Vector2(28f, 28f), new Color(0.23f, 0.16f, 0.12f, 0.45f), sw.transform);
                m.Size(sw.transform).At(0f, Chosen(i, 2)).At(tapTeal, Chosen(i, 2)).At(tapTeal + 0.12f, Chosen(i, CraftTeal))
                    .At(tapPink, Chosen(i, CraftTeal)).At(tapPink + 0.12f, Chosen(i, CraftPink));
                if (i == CraftTeal) tealAt = at;
                if (i == CraftPink) pinkAt = at;
            }

            // The tools: Brush (with its three sizes), Fill, Move, Undo. Brush to start; Fill once picked;
            // neither while a sticker is chosen.
            var fillAt = new Vector2(18f, 16f);
            CraftTool(m, bench, new Vector2(-98f, 16f), IconPainter.Brush, 0f, tapFill);
            for (int i = 0; i < 3; i++)
            {
                var at = new Vector2(-64f + i * 20f, 16f);
                m.Shape("Size", UiKit.Circle, at, new Vector2(18f, 18f), UiKit.SurfaceSunk, bench);
                if (i == 0) m.Between(m.Shape("On", UiKit.Circle, at, new Vector2(20f, 20f), UiKit.Hex("FFE7B8"), bench).gameObject, 0f, tapFill);
                m.Shape("Dot", UiKit.Circle, at, Vector2.one * (4f + i * 3f), UiKit.Ink, bench);
            }
            CraftTool(m, bench, fillAt, IconPainter.Bucket, tapFill, tapStar);
            CraftTool(m, bench, new Vector2(60f, 16f), TutorialPointer.HandSprite, -1f, -1f);
            CraftTool(m, bench, new Vector2(102f, 16f), IconPainter.Undo, -1f, -1f);

            // The stickers.
            LeftLabel(m, bench, "Stickers", new Vector2(-118f, -20f), 14f, UiKit.InkSoft, 120f);
            int stickers = CraftStickers(out int star);
            float gap = Mathf.Min(29f, 236f / Mathf.Max(1, stickers));
            var starAt = Vector2.zero;
            for (int i = 0; i < stickers; i++)
            {
                var at = new Vector2((i - (stickers - 1) * 0.5f) * gap, -46f);
                var tile = m.Group("Sticker", at, bench);
                m.Shape("Back", UiKit.Rounded(12), Vector2.zero, new Vector2(25f, 25f), Color.white, tile).type = Image.Type.Sliced;
                if (i == star)
                {
                    var lit = m.Shape("On", UiKit.Rounded(12), Vector2.zero, new Vector2(25f, 25f), UiKit.Hex("FFE7B8"), tile);
                    lit.type = Image.Type.Sliced;
                    m.Between(lit.gameObject, tapStar, L + 1f);
                    m.Size(tile).At(tapStar, 1f).At(tapStar + 0.12f, 1.12f);
                    starAt = at;
                }
                var art = CraftStickerArt(i);
                if (art != null) m.Shape("Art", art, Vector2.zero, new Vector2(19f, 19f), Color.white, tile).preserveAspect = true;
            }

            // Clear, Sew it, Sew & wear.
            WordButton(m, bench, new Vector2(-86f, -88f), new Vector2(74f, 36f), UiKit.SurfaceSunk, "Clear", UiKit.Ink);
            WordButton(m, bench, new Vector2(-4f, -88f), new Vector2(82f, 36f), UiKit.Spring, "Sew it", Color.white);
            var wearAt = new Vector2(86f, -88f);
            WordButton(m, bench, wearAt, new Vector2(90f, 36f), UiKit.Primary, "Sew & wear", Color.white);

            // Your sewing: the new hat joins it.
            LeftLabel(m, bench, "Your sewing", new Vector2(-118f, -120f), 14f, UiKit.InkSoft, 120f);
            var sewnAt = new Vector2(-104f, -148f);
            var sewn = m.Group("Sewn", sewnAt, bench);
            m.Shape("Edge", UiKit.Rounded(12), Vector2.zero, new Vector2(38f, 38f), UiKit.SurfaceSunk, sewn).type = Image.Type.Sliced;
            m.Shape("Tile", UiKit.Rounded(12), Vector2.zero, new Vector2(34f, 34f), Color.white, sewn).type = Image.Type.Sliced;
            m.Shape("Piece", finished, Vector2.zero, new Vector2(30f, 30f), Color.white, sewn).preserveAspect = true;
            m.Size(sewn).At(tapSew + 0.1f, 0f).At(tapSew + 0.4f, 1f, DemoEase.Back);

            m.Sparkle(hatAt, tapSew + 0.05f, UiKit.Sun, 6);
            m.Sparkle(B(sewnAt), tapSew + 0.25f, UiKit.Sun, 4);
            m.Sparkle(feet + new Vector2(0f, 125f), shut + 0.25f, UiKit.Sun, 6);

            // A blink between the loop's end and its start.
            var dip = m.Shape("Dip", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("2E1A0E"));
            m.Fade(dip).At(0f, 1f).At(0.3f, 0f).At(L - 0.3f, 0f).At(L, 1f);

            m.Hand().Show(0.3f, 1.45f)
                .At(0.3f, machineAt + new Vector2(150f, 110f)).At(0.8f, machineAt + new Vector2(0f, 55f)).Tap(tapMachine)
                .At(1.45f, machineAt + new Vector2(120f, 90f));
            m.Hand().Show(1.7f, tapClose + 0.35f)
                .At(1.7f, B(beanieAt) + new Vector2(80f, -80f)).At(tapBeanie - 0.35f, B(beanieAt)).Tap(tapBeanie)
                .At(tapTeal - 0.4f, B(tealAt)).Tap(tapTeal)
                .At(tapFill - 0.4f, B(fillAt)).Tap(tapFill)
                .At(tapCrown - 0.45f, crownTap).Tap(tapCrown)
                .At(tapPink - 0.45f, B(pinkAt)).Tap(tapPink)
                .At(tapBrim - 0.45f, brimTap).Tap(tapBrim)
                .At(tapStar - 0.45f, B(starAt)).Tap(tapStar)
                .At(tapStamp - 0.45f, stampTap).Tap(tapStamp)
                .At(tapSew - 0.45f, B(wearAt)).Tap(tapSew)
                .At(tapClose - 0.45f, closeAt).Tap(tapClose).At(tapClose + 0.35f, closeAt + new Vector2(-60f, -60f));
        }

        const BindingFlags CraftHidden = BindingFlags.NonPublic | BindingFlags.Static;

        /// <summary>The palette's teal and pink (SewingScreen.Palette 6 and 9), and the stitch the star is stuck on.</summary>
        const int CraftTeal = 6, CraftPink = 9, CraftStarX = 36, CraftStarY = 35;

        /// <summary>
        /// The beanie as it's decorated in the movie, drawn by the sewing screen itself
        /// (SewingScreen.DesignSprite): plain; the crown filled teal; the brim filled pink; the star
        /// sticker stuck on the crown. The parts and the sticker are read from the screen, so the
        /// movie's hat is always the game's hat.
        /// </summary>
        static Sprite[] CraftHatStages()
        {
            const int n = SewingScreen.Size;
            var regions = CraftHatRegions();
            var px = new char[n * n];
            for (int i = 0; i < px.Length; i++) px[i] = '.';
            var stages = new Sprite[4];
            stages[0] = SewingScreen.DesignSprite(null, "hat");
            for (int i = 0; i < px.Length; i++) if (regions[i] == 0) px[i] = (char)('a' + CraftTeal);
            stages[1] = SewingScreen.DesignSprite(new string(px), "hat");
            for (int i = 0; i < px.Length; i++) if (regions[i] == 1) px[i] = (char)('a' + CraftPink);
            stages[2] = SewingScreen.DesignSprite(new string(px), "hat");
            CraftStickers(out int star);
            CraftStamp(px, star, CraftStarX, CraftStarY);
            stages[3] = SewingScreen.DesignSprite(new string(px), "hat");
            for (int i = 1; i < stages.Length; i++) if (stages[i] == null) stages[i] = stages[i - 1];
            return stages;
        }

        /// <summary>Which part of the beanie each stitch is on (0 crown, 1 brim, 2 bobble, -1 off it), as the Fill tool sees them.</summary>
        static int[] CraftHatRegions()
        {
            const int n = SewingScreen.Size;
            var regions = new int[n * n];
            MethodInfo regionAt = null;
            try { regionAt = typeof(SewingScreen).GetMethod("RegionAt", CraftHidden, null, new[] { typeof(string), typeof(int), typeof(int) }, null); }
            catch (Exception) { regionAt = null; }
            var args = new object[3];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int r = -2;
                if (regionAt != null)
                {
                    try
                    {
                        args[0] = "hat"; args[1] = x; args[2] = y;
                        r = (int)regionAt.Invoke(null, args);
                    }
                    catch (Exception) { regionAt = null; r = -2; }
                }
                if (r == -2)
                {
                    // Not to be had: the crown is above the brim's top, the bobble above the crown.
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    r = SewingScreen.Shape("hat", u, v) < 0f ? (v < -0.2f ? 1 : v > 0.55f ? 2 : 0) : -1;
                }
                regions[y * n + x] = r;
            }
            return regions;
        }

        /// <summary>How many stickers the bench has, and which one is the star.</summary>
        static int CraftStickers(out int star)
        {
            star = 1;
            try
            {
                if (typeof(SewingScreen).GetField("Stickers", CraftHidden)?.GetValue(null) is Array all)
                {
                    for (int i = 0; i < all.Length; i++)
                        if (all.GetValue(i) is ValueTuple<string, Func<float, float, float>, Func<float, float, char>> t && t.Item1 == "Star") star = i;
                    return all.Length;
                }
            }
            catch (Exception) { }
            return 8;
        }

        /// <summary>A sticker's own picture, as its button on the bench shows it.</summary>
        static Sprite CraftStickerArt(int index)
        {
            try { return typeof(SewingScreen).GetMethod("StickerSprite", CraftHidden)?.Invoke(null, new object[] { index }) as Sprite; }
            catch (Exception) { return null; }
        }

        /// <summary>Sticks a sticker on, centred on a stitch, only where the hat is (SewingScreen.Stamp).</summary>
        static void CraftStamp(char[] px, int sticker, int cx, int cy)
        {
            const int n = SewingScreen.Size;
            try
            {
                var at = typeof(SewingScreen).GetMethod("StickerAt", CraftHidden);
                if (at == null) return;
                int size = 20;
                var field = typeof(SewingScreen).GetField("StickerSize", CraftHidden);
                if (field != null && field.IsLiteral && field.GetRawConstantValue() is int k) size = k;
                int from = -size / 2;
                var args = new object[3];
                for (int sy = 0; sy < size; sy++)
                for (int sx = 0; sx < size; sx++)
                {
                    args[0] = sticker;
                    args[1] = (sx + 0.5f) / size * 2f - 1f;
                    args[2] = (sy + 0.5f) / size * 2f - 1f;
                    char ch = (char)at.Invoke(null, args);
                    if (ch == '.') continue;
                    int x = cx + from + sx, y = cy + from + sy;
                    if (x < 0 || y < 0 || x >= n || y >= n) continue;
                    if (SewingScreen.Shape("hat", (x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f) >= 0f) continue;
                    px[y * n + x] = ch;
                }
            }
            catch (Exception) { }
        }

        /// <summary>What a pattern takes: the material's picture and "have/need", counting down when it's sewn.</summary>
        static void CraftNeed(HelpMovie m, Transform parent, Vector2 at, Sprite icon, int before, int after, int need, float when, Color ink)
        {
            if (icon != null) m.Shape("Icon", icon, at + new Vector2(-11f, 0f), new Vector2(15f, 15f), Color.white, parent).preserveAspect = true;
            string had = before + "/" + need, left = after + "/" + need;
            var t = m.Label("Count", had, at + new Vector2(8f, 0f), 12f, ink, parent);
            m.Do(s =>
            {
                var want = s >= when ? left : had;
                if (t.text != want) t.text = want;
            });
        }

        /// <summary>One of the bench's tool buttons: lit orange while it's the tool in hand.</summary>
        static void CraftTool(HelpMovie m, Transform parent, Vector2 at, Sprite icon, float onFrom, float onTo)
        {
            for (int k = 0; k < 2; k++)
            {
                bool lit = k == 1;
                var face = m.Shape(lit ? "On" : "Tool", UiKit.Rounded(14), at, new Vector2(38f, 34f), lit ? UiKit.Primary : UiKit.SurfaceSunk, parent);
                face.type = Image.Type.Sliced;
                if (icon != null) m.Shape("Icon", icon, Vector2.zero, new Vector2(22f, 22f), lit ? Color.white : UiKit.Ink, face.transform).preserveAspect = true;
                if (lit) m.Between(face.gameObject, onFrom, onTo);
            }
        }

        /// <summary>
        /// You, as the wardrobe draws you, standing on <paramref name="feet"/>; from <paramref name="on"/>
        /// the hat you sewed on your head, where Wardrobe pins a sewn beanie (at 0.46, 0.87 of the
        /// picture, 0.37 of its width across).
        /// </summary>
        static RectTransform CraftWearing(HelpMovie m, Vector2 feet, float height, Sprite hat, float on)
        {
            var art = HelpArt.You;
            float aspect = art != null ? art.rect.width / art.rect.height : 1f;
            var body = m.Shape("You", art, feet, new Vector2(height * aspect, height), Color.white);
            body.preserveAspect = true;
            var r = body.rectTransform;
            r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = feet;
            if (hat == null) return r;
            float w = 0.37f * height * aspect;
            var layer = m.Shape("SewnHat", hat, Vector2.zero, new Vector2(w, w * hat.rect.height / hat.rect.width), Color.white, r);
            layer.preserveAspect = true;
            var lr = layer.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0.46f, 0.87f);
            lr.anchoredPosition = Vector2.zero;
            m.Between(layer.gameObject, on, m.Length + 1f);
            m.Size(lr).At(on, 0.4f).At(on + 0.25f, 1f, DemoEase.Back);
            return r;
        }

        // ---------------------------------------------------------------- the shrine

        /// <summary>LibraryScreen's red, for the Ema Rack.</summary>
        static readonly Color CraftVermilion = UiKit.Hex("D2553F");

        /// <summary>Where the ema hang on the rack painting (LibraryScreen.PegsX / BarsY), as shares of it.</summary>
        static readonly float[] CraftPegsX = { 0.286f, 0.501f, 0.714f };
        static readonly float[] CraftBarsY = { 0.418f, 0.577f };

        /// <summary>
        /// Hanging a wish at Old Mochi's shrine, as the game does it: tap the ground to walk up to the
        /// shrine, with Mochi's spirit floating beside it, and tap its rack. The Ema Rack opens, its
        /// ema hanging from their pegs. Tap "Hang a new ema (1 gold)": tap the first box and type a
        /// few words for the wish, tap the big one and write it, and tap "Hang it on the rack". The
        /// gold coin goes, and yours hangs first on the rack, the others moving along a peg (the last
        /// one onto the next rack). Then tap the ema a friend left and read it.
        /// </summary>
        static void EmaMovie(HelpMovie m)
        {
            const float L = 14.4f;
            m.Length = L;
            const float tapGround = 0.9f, walkFrom = 1.0f, walkTo = 2.2f, tapRack = 2.65f, open = 2.85f;
            const float tapHang = 3.85f, write = 4.1f, tapTitle = 4.65f, typeTitle = 4.85f, titleDone = 5.85f;
            const float tapWish = 6.25f, typeWish = 6.45f, wishDone = 7.85f, tapHangIt = 8.45f, hung = 8.7f;
            const float shiftFrom = 8.8f, shiftTo = 9.2f, tapFriend = 10.55f, reading = 10.75f;

            // ---- the shrine on the hill, and Old Mochi beside it
            m.Island("Island", new Vector2(0f, -30f), new Vector2(580f, 300f));
            var shrineAt = new Vector2(120f, -80f);
            const float shrineSize = 230f;
            var shrine = m.Pic("Shrine", HobbyData.Art("Shrine"), shrineAt, shrineSize);
            m.Size(shrine.transform).At(tapRack, 1f).At(tapRack + 0.08f, 1.05f).At(tapRack + 0.22f, 1f);
            // The rack stands at the shrine's right, in its picture.
            var rackTap = shrineAt + new Vector2((0.79f - 0.5f) * shrineSize, (0.41f - 0.1f) * shrineSize);
            // Shrine.MakeMochi: 2.1 to the left of the shrine's middle and half a step down, 1.6 tall,
            // where the shrine is 3.4 wide; a spirit, not quite all there, floating.
            float unit = shrineSize / 3.4f;
            var mochiAt = shrineAt + new Vector2(0f, shrineSize * 0.4f) + new Vector2(-2.1f, -0.5f) * unit;
            var mochiArt = HobbyData.Art("Mochi");
            if (mochiArt != null)
            {
                var mochi = m.Shape("OldMochi", mochiArt, mochiAt, Vector2.one * 1.6f * unit, new Color(1f, 1f, 1f, 0.9f));
                mochi.preserveAspect = true;
                m.Do(s => mochi.rectTransform.anchoredPosition = mochiAt + new Vector2(0f, Mathf.Sin(s * 1.4f) * 0.06f * unit));
            }

            // You, walking up to where you tapped.
            var start = new Vector2(-235f, -125f);
            var stand = new Vector2(60f, -158f);
            var (you, body) = Walker(m, "You", HelpArt.You, start, 80f);
            m.Move(you).At(walkFrom, start).At(walkTo, stand, DemoEase.Linear);
            m.Face(body).At(0f, -1f);
            m.Bob(body, 6f, 12f, s => s > walkFrom && s < walkTo);

            // ---- the Ema Rack
            var screen = m.Group("EmaRack", Vector2.zero);
            During(m, screen, (open, L + 1f));
            m.Fade(screen).At(open - 0.01f, 0f).At(open + 0.2f, 1f);
            m.Shape("Scrim", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Scrim, screen);
            var sheet = m.Card("Sheet", Vector2.zero, new Vector2(588f, 392f), CraftVermilion, screen);
            m.Size(sheet).At(open, 0.9f).At(open + 0.25f, 1f, DemoEase.Back);

            // The painted garden behind it all.
            var clip = m.Shape("Garden", null, new Vector2(0f, -24f), new Vector2(572f, 336f), UiKit.Hex("E7EFD9"), sheet);
            clip.gameObject.AddComponent<RectMask2D>();
            var garden = HobbyData.Art("Shrine_Backdrop");
            if (garden != null)
            {
                float ga = garden.rect.width / garden.rect.height;
                float gw = Mathf.Max(572f, 336f * ga);
                m.Shape("Painting", garden, Vector2.zero, new Vector2(gw, gw / ga), Color.white, clip.transform);
            }

            // The header: the ema, the title, the close button.
            var emaArt = HobbyData.Art("Ema");
            m.Shape("Icon", emaArt != null ? emaArt : IconPainter.Book, new Vector2(-262f, 174f), new Vector2(32f, 32f), Color.white, sheet).preserveAspect = true;
            LeftLabel(m, sheet, "Ema Rack", new Vector2(-236f, 174f), 26f, Color.white, 380f);
            var shut = m.Shape("Close", UiKit.Circle, new Vector2(262f, 174f), new Vector2(34f, 34f), new Color(1f, 1f, 1f, 0.25f), sheet);
            m.Shape("X", IconPainter.Close, Vector2.zero, new Vector2(16f, 16f), Color.white, shut.transform);

            // ---- the rack page: before you hang yours, and after
            var rackPage = m.Group("RackPage", Vector2.zero, sheet);
            During(m, rackPage, (open, write), (hung, reading));
            var hangAt = new Vector2(0f, 126f);
            CraftIconButton(m, rackPage, hangAt, new Vector2(340f, 42f), UiKit.Primary, emaArt, "Hang a new ema (1 gold)", Color.white);

            // With yours up there are seven: a second rack, and the arrows to it.
            var pages = m.Group("Pages", new Vector2(0f, 86f), rackPage);
            During(m, pages, (hung, L + 1f));
            var prev = m.Shape("Prev", UiKit.Circle, new Vector2(-118f, 0f), new Vector2(32f, 32f), UiKit.SurfaceSunk, pages);
            var back = m.Shape("Arrow", IconPainter.Chevron, Vector2.zero, new Vector2(14f, 14f), Color.white, prev.transform);
            back.preserveAspect = true;
            back.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            m.Label("Label", "Rack 1 of 2", Vector2.zero, 18f, UiKit.Ink, pages);
            var next = m.Shape("Next", UiKit.Circle, new Vector2(118f, 0f), new Vector2(32f, 32f), CraftVermilion, pages);
            m.Shape("Arrow", IconPainter.Chevron, Vector2.zero, new Vector2(14f, 14f), Color.white, next.transform).preserveAspect = true;

            const float rackH = 240f, rackW = rackH / 1.5f;
            var rackAt = new Vector2(0f, -70f);
            m.Shape("Rack", HobbyData.Art("Ema_Rack"), rackAt, new Vector2(rackW, rackH), Color.white, rackPage).preserveAspect = true;
            float plaque = rackW * 0.205f;
            Vector2 Peg(int i) => i < 6
                ? rackAt + new Vector2((CraftPegsX[i % 3] - 0.5f) * rackW, (0.5f - CraftBarsY[i / 3]) * rackH)
                : rackAt + new Vector2(rackW * 0.75f, (0.5f - CraftBarsY[1]) * rackH);   // off to the next rack
            float Tilt(int i) => ((i * 37) % 11 - 5) * 0.8f;

            // Newest first (Shrine.Rack): yours, then what friends left, then the islanders' wishes.
            var friend = CraftFriendEma();
            var pool = CapyIsland.Game.Friends.NamePool;
            string friendName = pool != null && pool.Count > 3 ? pool[3] : "NoriNoodle";
            var hanging = new List<(bool mine, int before, int after)> { (true, -1, 0), (false, 0, 1) };
            for (int i = 0; i < StoryText.IslandEma.Length && i < 5; i++) hanging.Add((false, i + 1, i + 2));
            var friendTap = Peg(1) + new Vector2(0f, -0.36f * plaque);
            foreach (var (mine, before, after) in hanging)
            {
                var g = m.Group("Ema", before >= 0 ? Peg(before) : Peg(after), rackPage);
                CraftPlaque(m, g, plaque, mine);
                if (before >= 0)
                {
                    m.Move(g).At(shiftFrom, Peg(before)).At(shiftTo, Peg(after));
                    m.Turn(g).At(shiftFrom, Tilt(before)).At(shiftTo, Tilt(Mathf.Min(after, 5)));
                    if (after >= 6) m.Fade(g).At(shiftFrom, 1f).At(shiftTo, 0f);
                    if (after == 1) m.Size(g).At(tapFriend, 1f).At(tapFriend + 0.08f, 0.9f).At(tapFriend + 0.22f, 1f);
                }
                else
                {
                    m.Turn(g).At(0f, Tilt(after));
                    m.Size(g).At(shiftTo - 0.05f, 0f).At(shiftTo + 0.25f, 1f, DemoEase.Back);
                }
            }

            // ---- writing one
            var writePage = m.Group("WritePage", Vector2.zero, sheet);
            During(m, writePage, (write, hung));
            Lines(m, writePage, new Vector2(0f, 128f), 440f, 2, 20f);
            float edge = Loc.Rtl ? 222f : -222f;
            var titleBox = new Vector2(0f, 64f);
            var wishBox = new Vector2(0f, -38f);
            m.Shape("Title", UiKit.Rounded(24), titleBox, new Vector2(480f, 48f), Color.white, writePage).type = Image.Type.Sliced;
            m.Shape("Wish", UiKit.Rounded(24), wishBox, new Vector2(480f, 124f), Color.white, writePage).type = Image.Type.Sliced;
            var hint = new Color(UiKit.InkSoft.r, UiKit.InkSoft.g, UiKit.InkSoft.b, 0.7f);
            var titleHint = CraftEdgeLabel(m, writePage, "Your wish, in a few words", new Vector2(edge, titleBox.y), 19f, hint, 440f);
            titleHint.fontStyle = FontStyles.Italic;
            m.Between(titleHint.gameObject, 0f, typeTitle);
            var wishLine = new Vector2(edge, wishBox.y + 40f);
            var wishHint = CraftEdgeLabel(m, writePage, "I wish...", wishLine, 19f, hint, 440f);
            wishHint.fontStyle = FontStyles.Italic;
            m.Between(wishHint.gameObject, 0f, typeWish);
            CraftTyped(m, writePage, new Vector2(edge, titleBox.y), "Sunny days", 21f, 440f, typeTitle, titleDone, tapTitle, tapWish);
            CraftTyped(m, writePage, wishLine, "I wish for warm, sunny days.", 19f, 440f, typeWish, wishDone, tapWish, tapHangIt);
            WordButton(m, writePage, new Vector2(-150f, -140f), new Vector2(170f, 46f), UiKit.SurfaceSunk, "Cancel", UiKit.Ink);
            var hangItAt = new Vector2(96f, -140f);
            WordButton(m, writePage, hangItAt, new Vector2(290f, 46f), UiKit.Primary, "Hang it on the rack", Color.white);

            // ---- reading the one a friend left
            var readPage = m.Group("ReadPage", Vector2.zero, sheet);
            During(m, readPage, (reading, L + 1f));
            WordButton(m, readPage, new Vector2(0f, 126f), new Vector2(300f, 42f), UiKit.SurfaceSunk, "Back to the rack", UiKit.Ink);
            var paper = m.Shape("Page", UiKit.Rounded(30), new Vector2(0f, -42f), new Vector2(540f, 290f), UiKit.Hex("FBEBD0"), readPage);
            paper.type = Image.Type.Sliced;
            m.Size(paper.transform).At(reading, 0.92f).At(reading + 0.25f, 1f, DemoEase.Back);
            m.Pic("Ema", emaArt, new Vector2(0f, 50f), 74f, paper.transform);
            Fit(m.Label("Title", friend.title, new Vector2(0f, 22f), 30f, UiKit.Ink, paper.transform), friend.title, 500f);
            string by = "from " + friendName;
            Fit(m.Label("By", by, new Vector2(0f, -8f), 17f, UiKit.InkSoft, paper.transform), by, 500f);
            var said = (friend.text ?? "").Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < said.Length && i < 3; i++)
            {
                string line = said[i].Trim();
                Fit(m.Label("Text", line, new Vector2(0f, -42f - i * 28f), 16f, UiKit.Ink, paper.transform), line, 500f);
            }

            // The offering, paid as it goes up.
            m.FloatUp(HelpArt.Resource(ResourceKind.Gold), "-1", hangItAt + new Vector2(0f, 40f), tapHangIt + 0.1f);
            m.Sparkle(Peg(0) + new Vector2(0f, -0.36f * plaque), shiftTo + 0.1f, UiKit.Sun, 6);

            var dip = m.Shape("Dip", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("2E1A0E"));
            m.Fade(dip).At(0f, 1f).At(0.3f, 0f).At(L - 0.3f, 0f).At(L, 1f);

            m.Hand().Show(0.35f, 2.95f)
                .At(0.35f, stand + new Vector2(140f, 120f)).At(0.8f, stand + new Vector2(0f, 8f)).Tap(tapGround)
                .At(1.6f, stand + new Vector2(110f, 70f)).At(tapRack - 0.4f, rackTap).Tap(tapRack)
                .At(2.95f, rackTap + new Vector2(70f, -60f));
            m.Hand().Show(3.3f, tapFriend + 0.4f)
                .At(3.3f, hangAt + new Vector2(130f, -90f)).At(tapHang - 0.4f, hangAt).Tap(tapHang)
                .At(tapTitle - 0.4f, titleBox + new Vector2(60f, 0f)).Tap(tapTitle)
                .At(typeTitle + 0.2f, titleBox + new Vector2(190f, -60f))
                .At(tapWish - 0.4f, wishBox + new Vector2(60f, 0f)).Tap(tapWish)
                .At(typeWish + 0.2f, wishBox + new Vector2(200f, -50f))
                .At(tapHangIt - 0.4f, hangItAt).Tap(tapHangIt)
                .At(hung + 0.4f, hangItAt + new Vector2(80f, 60f))
                .At(tapFriend - 0.45f, friendTap).Tap(tapFriend).At(tapFriend + 0.4f, friendTap + new Vector2(90f, -70f));
        }

        /// <summary>
        /// An ema hanging from its peg: the plaque with its cord's loop on the peg (pivot 0.86 up),
        /// yours a little paler, and the wish written across its plain wood (too small to read here,
        /// so two strokes of ink).
        /// </summary>
        static void CraftPlaque(HelpMovie m, Transform parent, float size, bool mine)
        {
            var plaque = m.Shape("Plaque", HobbyData.Art("Ema_Blank"), Vector2.zero, new Vector2(size, size), mine ? UiKit.Hex("FFF6E2") : Color.white, parent);
            plaque.preserveAspect = true;
            var r = plaque.rectTransform;
            r.pivot = new Vector2(0.5f, 0.86f);
            r.anchoredPosition = Vector2.zero;
            var ink = new Color(0.29f, 0.21f, 0.14f, 0.75f);
            float stroke = Mathf.Max(1.5f, size * 0.05f);
            m.Shape("Ink", null, new Vector2(0f, -0.14f * size), new Vector2(size * 0.5f, stroke), ink, r);
            m.Shape("Ink", null, new Vector2(0f, -0.23f * size), new Vector2(size * 0.34f, stroke), ink, r);
        }

        /// <summary>One of the ema a friend can leave on your rack (BotBooks), read from the game; an islander's if it can't be.</summary>
        static (string title, string text) CraftFriendEma()
        {
            try
            {
                if (typeof(BotBooks).GetField("Library", CraftHidden)?.GetValue(null) is BotBooks.Story[] all && all.Length > 0)
                {
                    var pick = all[Mathf.Min(2, all.Length - 1)];
                    return (pick.Title, pick.Text);
                }
            }
            catch (Exception) { }
            var e = StoryText.IslandEma[0];
            return (e.title, e.text);
        }

        /// <summary>A button with its picture at the left and its words beside it ("Hang a new ema (1 gold)").</summary>
        static RectTransform CraftIconButton(HelpMovie m, Transform parent, Vector2 at, Vector2 size, Color fill, Sprite icon, string words, Color ink)
        {
            var face = WordButton(m, parent, at, size, fill, words, ink);
            if (icon == null) return face;
            var label = face.Find("Label") as RectTransform;
            if (label != null)
            {
                label.anchoredPosition = new Vector2(16f, 0f);
                var t = label.GetComponent<TMP_Text>();
                if (t != null) Fit(t, words, size.x - 80f);
            }
            m.Shape("Icon", icon, new Vector2(-size.x * 0.5f + 28f, 0f), new Vector2(size.y * 0.8f, size.y * 0.8f), Color.white, face).preserveAspect = true;
            return face;
        }

        /// <summary>A label that starts at a box's reading edge (its left, or its right in Hebrew) and runs on from there.</summary>
        static TextMeshProUGUI CraftEdgeLabel(HelpMovie m, Transform parent, string text, Vector2 edge, float size, Color colour, float width)
        {
            var t = m.Label("Label", text, edge, size, colour, parent);
            Fit(t, text, width);
            float w = Mathf.Min(width, t.GetPreferredValues(Loc.T(text)).x);
            t.rectTransform.sizeDelta = new Vector2(w + 4f, t.rectTransform.sizeDelta.y);
            t.rectTransform.anchoredPosition = edge + new Vector2((Loc.Rtl ? -0.5f : 0.5f) * w, 0f);
            return t;
        }

        /// <summary>
        /// Words typed into a box a letter at a time, from its reading edge at <paramref name="edge"/>,
        /// between <paramref name="from"/> and <paramref name="to"/>; the caret blinks after them while
        /// the box has the focus (<paramref name="focusFrom"/> to <paramref name="focusTo"/>). The whole
        /// line is set at once and uncovered, so it translates like any other.
        /// </summary>
        static void CraftTyped(HelpMovie m, Transform parent, Vector2 edge, string english, float size, float width,
            float from, float to, float focusFrom, float focusTo)
        {
            bool rtl = Loc.Rtl;
            var clip = m.Group("Typed", edge, parent);
            clip.pivot = new Vector2(rtl ? 1f : 0f, 0.5f);
            clip.anchoredPosition = edge;
            clip.gameObject.AddComponent<RectMask2D>();
            var t = m.Label("Words", english, Vector2.zero, size, UiKit.Ink, clip);
            Fit(t, english, width);
            float w = Mathf.Min(width, t.GetPreferredValues(Loc.T(english)).x) + 4f;
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(rtl ? 1f : 0f, 0.5f);
            r.sizeDelta = new Vector2(w, size * 1.4f);
            r.anchoredPosition = Vector2.zero;
            var caret = m.Shape("Caret", null, edge, new Vector2(2f, size * 1.1f), UiKit.Ink, parent);
            int letters = Mathf.Max(1, Loc.T(english).Length);
            m.Do(s =>
            {
                float u = Mathf.Clamp01((s - from) / Mathf.Max(0.01f, to - from));
                float shown = u >= 1f ? w : w * Mathf.Floor(u * letters) / letters;
                clip.sizeDelta = new Vector2(shown, size * 1.6f);
                caret.rectTransform.anchoredPosition = edge + new Vector2(rtl ? -shown : shown, 0f);
                bool typing = s >= from && s < to;
                caret.enabled = s >= focusFrom && s < focusTo && (typing || Mathf.Repeat(s, 0.8f) < 0.45f);
            });
        }
    }
}
