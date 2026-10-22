using System;
using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The friends and account movies: flying to a friend's island, friend codes, playing
    /// together, an account for another device, and the save code. Each acts out the real screens
    /// (Neighbours, the knock, the flight, Settings, the account and save-code windows) with the
    /// tutorial's finger doing the taps.
    /// </summary>
    public static partial class HelpDemos
    {
        // ---------------------------------------------------------------- shared pieces

        static readonly Color PalsBlue = UiKit.Hex("5B9BD5");    // Friends, Home, Neighbours
        static readonly Color PalsGear = UiKit.Hex("6B8FA8");    // Settings
        static readonly Color PalsAmber = UiKit.Hex("E3A92B");   // Gifts, Events
        const string PalsName = "NoriNoodle", PalsCode = "NoriNoodle#5072";
        /// <summary>The flight's three stretches (FlightTransition: clouds in, the plane across, clouds part), shortened.</summary>
        const float PalsCover = 0.5f, PalsFly = 1.5f, PalsReveal = 0.6f;
        static readonly Vector2 PalsSheetAt = new(-20f, -6f), PalsSheetSize = new(450f, 340f);
        /// <summary>The HUD's column of round buttons down the right.</summary>
        static readonly Vector2 PalsSide1 = new(255f, 150f), PalsSide2 = new(255f, 70f), PalsSide3 = new(255f, -10f);

        /// <summary>An islander the game made up, as the friends list keeps them.</summary>
        static FriendProfile PalsFriend(string name, int seed, float tint, string hat) => new FriendProfile
        {
            id = "help_" + name, name = name, seed = seed, tint = tint, level = 7, outfit = new List<string> { hat },
        };

        /// <summary>A player's name or a code: drawn as it is, never looked up in the tables.</summary>
        static void PalsAsTyped(TMP_Text t)
        {
            var loc = t != null ? t.GetComponent<LocText>() : null;
            if (loc != null) loc.NoTranslate = true;
        }

        /// <summary>Your friend code, as Neighbours shows it.</summary>
        static string PalsMyCode()
        {
            if (!string.IsNullOrEmpty(RealFriends.Code)) return RealFriends.Code;
            var s = new System.Text.StringBuilder();
            foreach (char c in PlayerProfile.Name ?? "") if (char.IsLetterOrDigit(c) && c < 128) s.Append(c);
            return (s.Length > 0 ? s.ToString() : "Capy") + "#4821";
        }

        /// <summary>
        /// A capybara standing on its feet: you (bare), or a friend as VisitorBot draws them, their
        /// fur a little tinted and their hat on.
        /// </summary>
        static RectTransform PalsCapy(HelpMovie m, Transform parent, Vector2 feet, float height, FriendProfile who = null)
        {
            string hat = who != null && who.outfit.Count > 0 ? who.outfit[0] : null;
            var body = hat != null ? Dressed(m, parent, feet, height, (hat, -1f, 999f)) : Dressed(m, parent, feet, height);
            if (who != null)
            {
                var img = body.GetComponent<Image>();
                float t = Mathf.Abs(who.tint) * 0.45f;
                if (img != null) img.color = Color.Lerp(Color.white, who.tint > 0f ? new Color(0.84f, 0.72f, 0.6f) : new Color(0.95f, 0.88f, 0.8f), t);
            }
            return body;
        }

        /// <summary>An island: 0 yours, 1 a friend's, 2 someone you've just met, 3 bare (a new device), 4 the one in a save code.</summary>
        static void PalsIsland(HelpMovie m, Transform parent, int look)
        {
            m.Island("Island", new Vector2(0f, -45f), new Vector2(570f, 250f), parent);
            switch (look)
            {
                case 0:
                    m.Pic("Garden", HelpArt.Item("Medium Posie Garden"), new Vector2(-20f, 5f), 66f, parent);
                    m.Pic("Mill", HelpArt.Item("Sawmill"), new Vector2(-120f, 0f), 100f, parent);
                    m.Pic("Forest", HelpArt.Item("Medium Pine Forest"), new Vector2(-215f, -30f), 86f, parent);
                    break;
                case 1:
                    m.Pic("Mill", HelpArt.Item("Sawmill"), new Vector2(-75f, 25f), 95f, parent);
                    m.Pic("Forest", HelpArt.Item("Medium Maple Forest", "Medium Pine Forest"), new Vector2(-205f, 0f), 100f, parent);
                    m.Pic("Garden", HelpArt.Item("Medium Posie Garden"), new Vector2(175f, -10f), 70f, parent);
                    break;
                case 2:
                    m.Pic("Mine", HelpArt.Item("Mine"), new Vector2(-150f, 15f), 100f, parent);
                    m.Pic("Hill", HelpArt.Item("Medium Gray Hill", "Small Gray Hill"), new Vector2(150f, 5f), 80f, parent);
                    m.Pic("Garden", HelpArt.Item("Medium Posie Garden"), new Vector2(-215f, -40f), 66f, parent);
                    break;
                case 4:
                    m.Pic("Forest", HelpArt.Item("Medium Maple Forest", "Medium Pine Forest"), new Vector2(40f, 20f), 90f, parent);
                    m.Pic("House", HelpArt.Item("Capy House"), new Vector2(-150f, 10f), 110f, parent);
                    m.Pic("Tree", HelpArt.Item("Apple Tree", "Pear Tree"), new Vector2(150f, 0f), 120f, parent);
                    break;
            }
        }

        /// <summary>One of the HUD's round buttons down the right, with its caption under it.</summary>
        static RectTransform PalsSide(HelpMovie m, Transform parent, Vector2 at, Sprite icon, Color fill, string caption)
        {
            var disc = m.RoundButton(caption, icon, fill, at, 54f, parent);
            var pill = m.Shape("CaptionPill", UiKit.Rounded(12), at + new Vector2(0f, -37f), new Vector2(68f, 20f), UiKit.HudGlass, parent);
            pill.type = Image.Type.Sliced;
            var t = m.Label("Caption", caption, Vector2.zero, 13f, Color.white, pill.transform);
            Fit(t, caption, 62f);
            return disc;
        }

        /// <summary>The column at home: Gifts, Friends, Events. Returns Friends.</summary>
        static RectTransform PalsHomeSide(HelpMovie m, Transform parent)
        {
            PalsSide(m, parent, PalsSide1, IconPainter.Gift, PalsAmber, "Gifts");
            var friends = PalsSide(m, parent, PalsSide2, IconPainter.Plane, PalsBlue, "Friends");
            PalsSide(m, parent, PalsSide3, IconPainter.Trophy, PalsAmber, "Events");
            return friends;
        }

        /// <summary>The top of the column: Settings, Help, Map. Returns Settings.</summary>
        static RectTransform PalsAccountSide(HelpMovie m, Transform parent)
        {
            var settings = PalsSide(m, parent, PalsSide1, IconPainter.Gear, PalsGear, "Settings");
            PalsSide(m, parent, PalsSide2, HelpArt.Question, HelpScreen.Accent, "Help");
            PalsSide(m, parent, PalsSide3, IconPainter.Map, UiKit.Spring, "Map");
            return settings;
        }

        /// <summary>A button squashing under each tap.</summary>
        static void PalsPress(HelpMovie m, Component c, params float[] taps)
        {
            var k = m.Size(c).At(0f, 1f);
            foreach (var t in taps) k.At(t - 0.02f, 1f).At(t + 0.08f, 0.88f).At(t + 0.24f, 1f);
        }

        static Image PalsCard(HelpMovie m, Transform parent, Vector2 at, Vector2 size, Color colour)
        {
            var c = m.Shape("Card", UiKit.Rounded(20), at, size, colour, parent);
            c.type = Image.Type.Sliced;
            return c;
        }

        /// <summary>A button as UiKit draws them: its word, and its icon (in the word's colour) before it.</summary>
        static RectTransform PalsButton(HelpMovie m, Transform parent, Vector2 at, Vector2 size, Color fill, string words, Color ink, Sprite icon = null)
        {
            var face = m.Shape("Button", UiKit.Rounded(16), at, size, fill, parent);
            face.type = Image.Type.Sliced;
            float iconSize = size.y * 0.52f;
            if (string.IsNullOrEmpty(words))
            {
                if (icon != null) m.Shape("Icon", icon, Vector2.zero, new Vector2(iconSize, iconSize), ink, face.transform).preserveAspect = true;
                return face.rectTransform;
            }
            var t = m.Label("Label", words, Vector2.zero, size.y * 0.42f, ink, face.transform);
            float room = size.x - 16f - (icon != null ? iconSize + 6f : 0f);
            Fit(t, words, room);
            if (icon == null) return face.rectTransform;
            float w = Mathf.Min(room, t.GetPreferredValues(Loc.T(words)).x);
            float total = iconSize + 6f + w;
            m.Shape("Icon", icon, new Vector2(-total * 0.5f + iconSize * 0.5f, 0f), new Vector2(iconSize, iconSize), ink, face.transform).preserveAspect = true;
            t.rectTransform.anchoredPosition = new Vector2(-total * 0.5f + iconSize + 6f + w * 0.5f, 0f);
            return face.rectTransform;
        }

        /// <summary>A text box: its grey placeholder, and the line typed into it (empty to begin with).</summary>
        static (TextMeshProUGUI hint, TextMeshProUGUI text) PalsInput(HelpMovie m, Transform parent, Vector2 at, Vector2 size, string placeholder)
        {
            m.Shape("Rim", UiKit.Rounded(16), at, size + new Vector2(4f, 4f), new Color(0f, 0f, 0f, 0.08f), parent).type = Image.Type.Sliced;
            var box = m.Shape("Box", UiKit.Rounded(16), at, size, Color.white, parent);
            box.type = Image.Type.Sliced;
            float fontSize = Mathf.Min(17f, size.y * 0.4f);
            var hint = m.Label("Placeholder", placeholder, Vector2.zero, fontSize, new Color(UiKit.InkSoft.r, UiKit.InkSoft.g, UiKit.InkSoft.b, 0.7f), box.transform);
            hint.fontStyle = FontStyles.Italic;
            Fit(hint, placeholder, size.x - 28f);
            var text = m.Label("Text", "", Vector2.zero, fontSize, UiKit.Ink, box.transform);
            PalsAsTyped(text);
            foreach (var line in new[] { hint, text })
            {
                line.alignment = TextAlignmentOptions.MidlineLeft;
                line.rectTransform.sizeDelta = new Vector2(size.x - 28f, size.y);
            }
            return (hint, text);
        }

        /// <summary>
        /// <paramref name="words"/> typed in, a letter at a time, between two moments; the
        /// placeholder shows while the box is empty. From <paramref name="clear"/> it's empty again.
        /// </summary>
        static void PalsTyping(HelpMovie m, TMP_Text text, TMP_Text hint, string words, float from, float to, float clear = float.MaxValue)
        {
            m.Do(s =>
            {
                int n = s < from || s >= clear ? 0 : Mathf.Clamp(Mathf.CeilToInt((s - from) / Mathf.Max(0.01f, to - from) * words.Length), 0, words.Length);
                string shown = words.Substring(0, n);
                if (text.text != shown) text.text = shown;
                if (hint != null && hint.enabled != (n == 0)) hint.enabled = n == 0;
            });
        }

        /// <summary>A few lines of the game's own words, wrapped, shrinking to fit their box.</summary>
        static TextMeshProUGUI PalsPara(HelpMovie m, Transform parent, string words, Vector2 at, float width, float height, float size, Color colour)
        {
            var t = m.Label("Words", words, at, size, colour, parent);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.rectTransform.sizeDelta = new Vector2(width, height);
            t.enableAutoSizing = true;
            t.fontSizeMin = size * 0.55f;
            t.fontSizeMax = size;
            return t;
        }

        /// <summary>An emoji in its little white speech bubble over someone's head (EmojiBubbles), its foot at <paramref name="at"/>.</summary>
        static void PalsEmote(HelpMovie m, Transform parent, CapyIsland.Game.Emoji e, Vector2 at, float from, float to)
        {
            var g = m.Group("Emote", at, parent);
            m.Shape("Bubble", UiKit.Rounded(18), new Vector2(0f, 31f), new Vector2(62f, 56f), Color.white, g).type = Image.Type.Sliced;
            var tail = m.Shape("Tail", IconPainter.Arrow, new Vector2(0f, 2f), new Vector2(18f, 18f), Color.white, g);
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            UiKit.Emoji(g, e, 42f).anchoredPosition = new Vector2(0f, 31f);
            m.Between(g.gameObject, from, to);
            m.Size(g).At(from, 0.2f).At(from + 0.3f, 1f, DemoEase.Back);
        }

        /// <summary>
        /// The emoji button (EmojiBar): yellow, with a smile, bottom right; its tray of six opens to
        /// the left between two moments. Returns where the wave sits in the tray.
        /// </summary>
        static Vector2 PalsEmojiBar(HelpMovie m, Transform parent, Vector2 at, float trayFrom, float trayTo, out RectTransform button)
        {
            button = m.RoundButton("Emoji", null, UiKit.Hex("FFD24A"), at, 64f, parent);
            var blank = button.Find("Icon");
            if (blank != null) blank.gameObject.SetActive(false);
            UiKit.Emoji(button, CapyIsland.Game.Emoji.Smile, 44f).anchoredPosition = Vector2.zero;
            CapyIsland.Game.Emoji[] choices =
            {
                CapyIsland.Game.Emoji.Wave, CapyIsland.Game.Emoji.Heart, CapyIsland.Game.Emoji.Smile,
                CapyIsland.Game.Emoji.Laugh, CapyIsland.Game.Emoji.Wow, CapyIsland.Game.Emoji.Star,
            };
            float trayW = choices.Length * 44f + 12f;
            var trayAt = at + new Vector2(-40f - trayW * 0.5f, 0f);
            var tray = PalsCard(m, parent, trayAt, new Vector2(trayW, 56f), UiKit.Surface);
            for (int i = 0; i < choices.Length; i++)
            {
                var slot = PalsCard(m, tray.transform, new Vector2(-trayW * 0.5f + 28f + i * 44f, 0f), new Vector2(40f, 40f), new Color(0f, 0f, 0f, 0.04f));
                UiKit.Emoji(slot.transform, choices[i], 32f).anchoredPosition = Vector2.zero;
            }
            m.Between(tray.gameObject, trayFrom, trayTo);
            m.Size(tray.transform).At(trayFrom, 0.8f).At(trayFrom + 0.2f, 1f, DemoEase.Back);
            return trayAt + new Vector2(-trayW * 0.5f + 28f, 0f);
        }

        /// <summary>A dark dimmer behind a window.</summary>
        static void PalsScrim(HelpMovie m, float from, float to)
        {
            var scrim = m.Shape("Scrim", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Scrim);
            float a = UiKit.Scrim.a;
            m.Fade(scrim).At(from - 0.15f, 0f).At(from, a).At(to, a).At(to + 0.2f, 0f);
        }

        /// <summary>A blink of darkness at each cut, and at the loop's ends.</summary>
        static void PalsDip(HelpMovie m, float length, params float[] cuts)
        {
            var dip = m.Shape("Dip", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("1E2A33"));
            var fade = m.Fade(dip).At(0f, 1f).At(0.3f, 0f);
            foreach (var t in cuts) fade.At(t - 0.25f, 0f).At(t, 1f).At(t + 0.25f, 0f);
            fade.At(length - 0.35f, 0f).At(length, 1f);
        }

        sealed class PalsSheet
        {
            public RectTransform Card, Content;
            /// <summary>Where the top of the list is on the stage, before it's scrolled.</summary>
            public Vector2 Top;
            public float Width;
        }

        /// <summary>
        /// A screen (UiScreen): the dimmer, the sheet with its coloured header, icon, title and
        /// close button, and a scrolling list under it, scrolled up by <paramref name="scroll"/>
        /// between two moments (a swipe).
        /// </summary>
        static PalsSheet PalsScreen(HelpMovie m, Vector2 at, Vector2 size, Color accent, Sprite icon, string title,
            float from, float to, float scroll = 0f, float scrollFrom = 0f, float scrollTo = 0f)
        {
            PalsScrim(m, from, to);
            var card = Panel(m, at, size, accent, from, to);
            float top = size.y * 0.5f - 22f;
            m.Shape("Icon", icon, new Vector2(-size.x * 0.5f + 30f, top), new Vector2(28f, 28f), Color.white, card).preserveAspect = true;
            LeftLabel(m, card, title, new Vector2(-size.x * 0.5f + 52f, top), 22f, Color.white, size.x - 130f);
            var close = m.Shape("Close", UiKit.Circle, new Vector2(size.x * 0.5f - 28f, top), new Vector2(30f, 30f), new Color(1f, 1f, 1f, 0.25f), card);
            m.Shape("X", IconPainter.Close, Vector2.zero, new Vector2(13f, 13f), Color.white, close.transform);

            float viewH = size.y - 54f;
            var view = m.Group("View", new Vector2(0f, -21f), card);
            view.sizeDelta = new Vector2(size.x - 16f, viewH);
            view.gameObject.AddComponent<RectMask2D>();
            var rest = new Vector2(0f, viewH * 0.5f);
            var content = m.Group("Content", rest, view);
            if (scroll != 0f) m.Move(content).At(scrollFrom, rest).At(scrollTo, rest + new Vector2(0f, scroll), DemoEase.Out);
            return new PalsSheet { Card = card, Content = content, Top = at + new Vector2(0f, -21f) + rest, Width = size.x - 36f };
        }

        /// <summary>The server's islands at the top of Neighbours: you, islanders (a heart on a friend's), an empty spot.</summary>
        static void PalsServer(HelpMovie m, Transform parent, Vector2 at, float width)
        {
            var card = PalsCard(m, parent, at, new Vector2(width, 112f), UiKit.Hex("DDF0F5"));
            Lines(m, card.transform, new Vector2(-width * 0.5f + 90f, 40f), 140f, 2, 18f);
            Color[] fur = { Color.white, UiKit.Hex("E9D3BD"), UiKit.Hex("F5E6D6"), UiKit.Hex("DCC0A4") };
            for (int i = 0; i < 5; i++)
            {
                bool empty = i == 4;
                var tile = PalsCard(m, card.transform, new Vector2(-140f + i * 70f, -16f), new Vector2(58f, 66f), empty ? new Color(1f, 1f, 1f, 0.35f) : Color.white);
                var sand = m.Shape("Sand", UiKit.Circle, new Vector2(0f, 8f), new Vector2(44f, 44f), empty ? new Color(0.6f, 0.75f, 0.8f, 0.4f) : UiKit.Hex("E0C99A"), tile.transform);
                if (!empty)
                {
                    m.Shape("Grass", UiKit.Circle, Vector2.zero, new Vector2(38f, 38f), UiKit.Hex("7FAF4F"), sand.transform);
                    m.Pic("Face", HelpArt.You, new Vector2(0f, -15f), 32f, sand.transform, fur[i]);
                }
                if (i == 2) m.Shape("Friend", IconPainter.Heart, new Vector2(21f, 25f), new Vector2(14f, 14f), UiKit.Danger, tile.transform);
                m.Shape("Name", UiKit.Rounded(6), new Vector2(0f, -24f), new Vector2(40f, 7f), new Color(0f, 0f, 0f, empty ? 0.06f : 0.14f), tile.transform).type = Image.Type.Sliced;
            }
        }

        /// <summary>Neighbours' "Play together" card: Island party and the three contests. Returns where Island party is.</summary>
        static Vector2 PalsPlayTogether(HelpMovie m, Transform parent, Vector2 at, float width)
        {
            var card = PalsCard(m, parent, at, new Vector2(width, 146f), UiKit.Hex("FDEBF2"));
            LeftLabel(m, card.transform, "Play together", new Vector2(-width * 0.5f + 16f, 50f), 20f, UiKit.Ink, 220f);
            float bw = (width - 44f) * 0.5f;
            Vector2[] spots =
            {
                new(-bw * 0.5f - 6f, 4f), new(bw * 0.5f + 6f, 4f), new(-bw * 0.5f - 6f, -44f), new(bw * 0.5f + 6f, -44f),
            };
            var size = new Vector2(bw, 40f);
            PalsButton(m, card.transform, spots[0], size, UiKit.Primary, "Island party", Color.white, IconPainter.Star);
            PalsButton(m, card.transform, spots[1], size, UiKit.Spring, "Fishing Contest", Color.white, IconPainter.Rod);
            var butterfly = HobbyData.Art("Bug_Butterfly");
            PalsButton(m, card.transform, spots[2], size, UiKit.Spring, "Bug Contest", Color.white, butterfly != null ? butterfly : IconPainter.Sparkle);
            PalsButton(m, card.transform, spots[3], size, UiKit.Spring, "Birdwatching Contest", Color.white, IconPainter.Bird);
            return at + spots[0];
        }

        /// <summary>A friend in Neighbours' list: face, name, online, Unfriend, Visit. Returns where Visit is.</summary>
        static Vector2 PalsFriendRow(HelpMovie m, Transform parent, Vector2 at, float width, FriendProfile who)
        {
            var row = PalsCard(m, parent, at, new Vector2(width, 80f), Color.white);
            var face = UiKit.Avatar(row.transform, who, 58f);
            face.anchoredPosition = new Vector2(-width * 0.5f + 40f, 0f);
            PalsAsTyped(LeftLabel(m, row.transform, who.name, new Vector2(-width * 0.5f + 78f, 13f), 18f, UiKit.Ink, 150f));
            LeftLabel(m, row.transform, "<color=#5DBB63>Online</color> · Island level " + who.level, new Vector2(-width * 0.5f + 78f, -14f), 12f, UiKit.InkSoft, 150f);
            PalsButton(m, row.transform, new Vector2(width * 0.5f - 140f, 0f), new Vector2(40f, 40f), UiKit.SurfaceSunk, null, UiKit.Ink, IconPainter.Close);
            var visit = new Vector2(width * 0.5f - 60f, 0f);
            PalsButton(m, row.transform, visit, new Vector2(100f, 42f), UiKit.Primary, "Visit", Color.white, IconPainter.Plane);
            return at + visit;
        }

        sealed class PalsCodeParts
        {
            public Vector2 Copy, Box, Add;   // in the list, like the card
            public TextMeshProUGUI Hint, Typed, Status;
        }

        /// <summary>Neighbours' friend-code card: your code and Copy, a box for a friend's code and Add, and the answer under it (hidden).</summary>
        static PalsCodeParts PalsCodeCard(HelpMovie m, Transform parent, Vector2 at, float width, string mine)
        {
            var card = PalsCard(m, parent, at, new Vector2(width, 150f), UiKit.Hex("EAF3FB"));
            var t = card.transform;
            LeftLabel(m, t, "Your friend code", new Vector2(-width * 0.5f + 16f, 52f), 14f, UiKit.InkSoft, 200f);
            PalsAsTyped(LeftLabel(m, t, mine, new Vector2(-width * 0.5f + 16f, 26f), 24f, UiKit.Ink, 240f));
            var parts = new PalsCodeParts
            {
                Copy = at + new Vector2(width * 0.5f - 60f, 40f),
                Box = at + new Vector2(-58f, -18f),
                Add = at + new Vector2(width * 0.5f - 60f, -18f),
            };
            PalsButton(m, t, parts.Copy - at, new Vector2(100f, 38f), UiKit.Spring, "Copy", Color.white);
            (parts.Hint, parts.Typed) = PalsInput(m, t, parts.Box - at, new Vector2(width - 150f, 42f), "A friend's code, like Miso#1234");
            PalsButton(m, t, parts.Add - at, new Vector2(104f, 42f), UiKit.Primary, "Add", Color.white, IconPainter.AddFriend);
            parts.Status = PalsPara(m, t, "Request sent to " + PalsCode + ". You'll be friends once they accept.", new Vector2(0f, -56f), width - 30f, 28f, 13f, UiKit.InkSoft);
            parts.Status.gameObject.SetActive(false);
            return parts;
        }

        /// <summary>
        /// The flight (FlightTransition): clouds roll in over the sky, your capybara putters across
        /// in the little red plane with the label under it, and the clouds part again.
        /// </summary>
        static void PalsFlight(HelpMovie m, float start, string label)
        {
            float covered = start + PalsCover, landing = covered + PalsFly, gone = landing + PalsReveal;
            var sky = m.Shape("Sky", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("9FD8EE"));
            m.Fade(sky).At(start, 0f).At(start + PalsCover * 0.8f, 1f).At(landing, 1f).At(gone, 0f);

            // Small clouds drifting past: the sense of speed.
            for (int i = 0; i < 5; i++)
            {
                float y = -150f + i * 75f, x0 = 360f + (i * 137) % 300;
                var drift = m.Shape("Drift", IconPainter.Cloud, new Vector2(x0, y), new Vector2(130f, 90f), Color.white);
                m.Move(drift.transform).At(covered, new Vector2(x0, y)).At(landing + 0.4f, new Vector2(x0 - 900f, y), DemoEase.Linear);
                m.Fade(drift).At(covered - 0.01f, 0f).At(covered, 0.85f, DemoEase.Step).At(landing, 0.85f).At(landing + 0.35f, 0f);
            }

            // The plane: a capybara pilot, the red body with its stripe, a spinning propeller.
            var plane = m.Group("Plane", new Vector2(-420f, 20f));
            if (HelpArt.You != null)
            {
                var pilot = m.Shape("Pilot", HelpArt.You, new Vector2(-5f, 43f), new Vector2(80f, 80f), Color.white, plane);
                pilot.preserveAspect = true;
                pilot.rectTransform.localScale = new Vector3(-1f, 1f, 1f);   // facing the way it flies
            }
            m.Shape("Body", IconPainter.Plane, Vector2.zero, new Vector2(170f, 170f), UiKit.Hex("E8553E"), plane).preserveAspect = true;
            m.Shape("Stripe", UiKit.Rounded(10), new Vector2(-14f, -2f), new Vector2(90f, 9f), new Color(1f, 1f, 1f, 0.85f), plane).type = Image.Type.Sliced;
            var prop = m.Shape("Propeller", UiKit.Rounded(12), new Vector2(69f, 2f), new Vector2(8f, 70f), UiKit.Ink, plane);
            prop.type = Image.Type.Sliced;
            m.Do(s => prop.rectTransform.localScale = new Vector3(1f, Mathf.Abs(Mathf.Sin(s * 40f)), 1f));
            m.Move(plane).At(covered, new Vector2(-420f, 20f)).At(landing, new Vector2(40f, 20f), DemoEase.Out).At(landing + 0.5f, new Vector2(460f, 20f), DemoEase.In);
            m.Do(s =>
            {
                plane.anchoredPosition += new Vector2(0f, Mathf.Sin(s * 3.1f) * 12f);
                plane.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(s * 2.4f) * 4f);
            });
            m.Between(plane.gameObject, covered, landing + 0.55f);

            var words = m.Label("Flying", label, new Vector2(0f, -140f), 24f, Color.white);
            words.outlineWidth = 0.22f;
            words.outlineColor = new Color32(70, 120, 150, 255);
            Fit(words, label, 540f);
            m.Fade(words).At(covered, 0f).At(covered + 0.4f, 1f).At(landing, 1f).At(landing + 0.3f, 0f);

            // The big clouds that close over the screen, thin out for the flight, and part.
            for (int i = 0; i < 9; i++)
            {
                int col = i % 3, row = i / 3;
                var cover = new Vector2(-200f + col * 200f + ((i * 53) % 60 - 30f), 135f - row * 135f + ((i * 37) % 40 - 20f));
                bool left = col == 0 || (col == 1 && row != 1);
                var off = new Vector2(left ? -720f : 720f, cover.y);
                var thin = Vector2.Lerp(cover, off, 0.55f);
                var cloud = m.Shape("Cover", IconPainter.Cloud, off, new Vector2(440f, 310f), Color.Lerp(Color.white, UiKit.Hex("EAF6FB"), (i % 3) * 0.3f));
                if (i % 2 == 1) cloud.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                m.Move(cloud.transform).At(start, off).At(covered, cover, DemoEase.Out)
                    .At(covered + 0.6f, thin).At(landing, thin).At(gone, off, DemoEase.In);
            }
        }

        // ---------------------------------------------------------------- visiting

        /// <summary>
        /// Visiting, from the very start. Tap Friends: Neighbours opens with "Build an airstrip" at
        /// the top (300 wood and 300 gold); tap Build and the counters drop, the window closes, and the
        /// airstrip waits in a bubble beside you. Pop it, drag it out of your trail and put it down.
        /// Tap Friends again: scroll down past the server and Play together to your friends, and tap
        /// Visit. The clouds roll in, you fly over, and you land on their island with them waving
        /// hello. Home, at the top of the column on the right, flies you back to your airstrip.
        /// </summary>
        static void VisitMovie(HelpMovie m)
        {
            const float L = 17.6f;
            m.Length = L;
            const float tapFriends = 0.9f, open1 = 1.1f, tapBuild = 2.1f, built = 2.25f;
            const float bubbleIn = 2.6f, pop = 3.4f, inTrail = 4.0f, grab = 4.6f, drop = 5.6f;
            const float tapFriends2 = 6.7f, open2 = 6.9f, swipe = 7.4f, swiped = 8.0f, tapVisit = 8.6f;
            const float fly1 = tapVisit + 0.25f, there = fly1 + PalsCover, landed = there + PalsFly + PalsReveal;
            const float tapHome = 13.0f, fly2 = tapHome + 0.25f, back = fly2 + PalsCover;
            const float scroll = 330f;
            const int wood0 = 420, gold0 = 380;
            var friend = PalsFriend(PalsName, 5, 0.6f, "Hat_Straw");
            var airstrip = HelpArt.Picture("Airport");

            // ---- home: the airstrip goes in
            var home = m.Group("Home", Vector2.zero);
            During(m, home, (0f, there), (back, L));
            PalsIsland(m, home, 0);
            var strip = new Vector2(150f, -85f);
            var placed = m.Pic("Airstrip", airstrip, strip, 150f, home);
            m.Size(placed.transform).At(drop - 0.01f, 0f).At(drop, 0.2f, DemoEase.Step).At(drop + 0.5f, 1f, DemoEase.Back);
            var you = PalsCapy(m, home, new Vector2(-50f, -150f), 70f);
            m.Face(you).At(0f, -1f);

            // Built: it waits in a bubble beside you; popped, it joins your trail; dragged out, it's down.
            var trail = new Vector2(-110f, -158f);
            var bubbleAt = new Vector2(40f, -60f);
            var bubble = BubbleWith(m, airstrip, bubbleAt, 100f, home);
            m.Do(s => bubble.anchoredPosition = bubbleAt + new Vector2(0f, Mathf.Sin(s * 2.4f) * 5f));
            m.Size(bubble).At(bubbleIn, 0.3f).At(bubbleIn + 0.35f, 1f, DemoEase.Back);
            m.Between(bubble.gameObject, bubbleIn, pop);
            var carried = m.Group("Carried", trail, home);
            m.Pic("Pic", airstrip, Vector2.zero, 60f, carried);
            var room = PlaceRing(m, carried, true, 120f);
            m.Fade(room).At(grab, 0f).At(grab + 0.15f, 0.45f).At(drop, 0.45f).At(drop + 0.1f, 0f);
            m.Move(carried).At(pop, bubbleAt + new Vector2(0f, -30f)).At(inTrail, trail).At(grab, trail).At(drop, strip, DemoEase.Linear);
            m.Between(carried.gameObject, pop, drop);

            var wood = m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "", WoodPill, home);
            var gold = m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "", GoldPill, home);
            m.Tally(wood, wood0, (built, wood0 - Airport.CostWood));
            m.Tally(gold, gold0, (built, gold0 - Airport.CostGold));
            PalsPress(m, PalsHomeSide(m, home), tapFriends, tapFriends2);

            // ---- their island
            var away = m.Group("Away", Vector2.zero);
            During(m, away, (there, back));
            PalsIsland(m, away, 1);
            var you2 = PalsCapy(m, away, new Vector2(-50f, -150f), 70f);
            m.Face(you2).At(0f, -1f);
            PalsCapy(m, away, new Vector2(40f, -140f), 70f, friend);
            PalsEmote(m, away, CapyIsland.Game.Emoji.Wave, new Vector2(40f, -62f), landed - 0.3f, landed + 1.1f);
            PalsPress(m, PalsSide(m, away, PalsSide1, IconPainter.House, PalsBlue, "Home"), tapHome);

            m.Sparkle(bubbleAt, pop, Color.white, 6);
            m.Sparkle(strip + new Vector2(0f, 40f), drop, UiKit.Sun, 5);

            // ---- Neighbours, the first time: the airstrip card at the top
            var s1 = PalsScreen(m, PalsSheetAt, PalsSheetSize, PalsBlue, IconPainter.Plane, "Neighbours", open1, built);
            float w = s1.Width;
            var stripCardAt = new Vector2(0f, -64f);
            var stripCard = PalsCard(m, s1.Content, stripCardAt, new Vector2(w, 112f), UiKit.Hex("FFF1D6"));
            m.Pic("Picture", airstrip, new Vector2(-w * 0.5f + 52f, -40f), 84f, stripCard.transform);
            LeftLabel(m, stripCard.transform, "Build an airstrip", new Vector2(-w * 0.5f + 100f, 26f), 20f, UiKit.Ink, 190f);
            Cost(m, stripCard.transform, new Vector2(-w * 0.5f + 136f, -16f), ResourceKind.Wood, Airport.CostWood.ToString());
            Cost(m, stripCard.transform, new Vector2(-w * 0.5f + 226f, -16f), ResourceKind.Gold, Airport.CostGold.ToString());
            var buildLocal = new Vector2(w * 0.5f - 60f, 0f);
            PalsButton(m, stripCard.transform, buildLocal, new Vector2(96f, 46f), UiKit.Primary, "Build", Color.white);
            PalsServer(m, s1.Content, new Vector2(0f, -190f), w);
            var build = s1.Top + stripCardAt + buildLocal;

            // ---- Neighbours again: down past the server and Play together to your friends
            var s2 = PalsScreen(m, PalsSheetAt, PalsSheetSize, PalsBlue, IconPainter.Plane, "Neighbours", open2, tapVisit + 0.15f, scroll, swipe, swiped);
            PalsServer(m, s2.Content, new Vector2(0f, -62f), w);
            PalsPlayTogether(m, s2.Content, new Vector2(0f, -195f), w);
            LeftLabel(m, s2.Content, "Your friends", new Vector2(-w * 0.5f + 6f, -290f), 17f, UiKit.InkSoft, 200f);
            PalsCodeCard(m, s2.Content, new Vector2(0f, -385f), w, PalsMyCode());
            var visit = s2.Top + PalsFriendRow(m, s2.Content, new Vector2(0f, -510f), w, friend) + new Vector2(0f, scroll);

            PalsFlight(m, fly1, "Flying to " + PalsName + "'s island...");
            PalsFlight(m, fly2, "Flying home...");
            PalsDip(m, L);

            var swipeFrom = new Vector2(-20f, -120f);
            m.Hand().Show(0.35f, built + 0.3f)
                .At(0.35f, PalsSide2 + new Vector2(-120f, -80f)).At(tapFriends - 0.2f, PalsSide2).Tap(tapFriends)
                .At(tapBuild - 0.35f, build).Tap(tapBuild).At(built + 0.3f, build + new Vector2(70f, -60f));
            m.Hand().Show(bubbleIn + 0.45f, drop + 0.2f)
                .At(bubbleIn + 0.45f, bubbleAt + new Vector2(120f, -50f)).At(pop - 0.25f, bubbleAt).Tap(pop)
                .At(grab - 0.25f, trail + new Vector2(0f, 20f)).Press(grab).At(grab, trail + new Vector2(0f, 20f))
                .At(drop, strip + new Vector2(0f, 30f), DemoEase.Linear).Lift(drop + 0.05f).At(drop + 0.2f, strip + new Vector2(-40f, 60f));
            m.Hand().Show(tapFriends2 - 0.4f, tapVisit + 0.4f)
                .At(tapFriends2 - 0.4f, PalsSide2 + new Vector2(-120f, -80f)).At(tapFriends2 - 0.15f, PalsSide2).Tap(tapFriends2)
                .At(swipe - 0.1f, swipeFrom).Press(swipe).At(swipe, swipeFrom)
                .At(swiped - 0.2f, swipeFrom + new Vector2(0f, 210f), DemoEase.Linear).Lift(swiped - 0.15f)
                .At(tapVisit - 0.3f, visit).Tap(tapVisit).At(tapVisit + 0.4f, visit + new Vector2(60f, -60f));
            m.Hand().Show(tapHome - 0.45f, tapHome + 0.3f)
                .At(tapHome - 0.45f, PalsSide1 + new Vector2(-120f, -80f)).At(tapHome - 0.15f, PalsSide1).Tap(tapHome);
        }

        /// <summary>
        /// Friend codes. Tap Friends and scroll down to your friend code: Copy it to send. Tap the box,
        /// type your friend's code, tap Add: "Request sent…". Once they accept, they're in your list:
        /// tap Visit, and you knock ("Waiting for … to let you in", with Cancel) until they let you in,
        /// then fly over and they step out to wave. Last, on the island of someone who isn't your
        /// friend yet, Add on the right makes them one (a heart from them, and Add goes away).
        /// </summary>
        static void FriendCodeMovie(HelpMovie m)
        {
            const float L = 18.2f;
            m.Length = L;
            const float tapFriends = 0.9f, open = 1.1f, swipe = 1.5f, swiped = 2.1f, tapCopy = 2.7f;
            const float tapBox = 3.4f, typeFrom = 3.6f, typeTo = 4.6f, tapAdd = 5.1f, sent = 5.25f, accepted = 6.2f, tapVisit = 7.0f;
            const float knock = 7.25f, letIn = 9.2f, fly = 9.3f, there = fly + PalsCover, landed = there + PalsFly + PalsReveal;
            const float cut = 13.7f, tapAdd2 = 14.7f, added = 14.85f;
            const float scroll = 290f;
            var friend = PalsFriend(PalsName, 5, 0.6f, "Hat_Straw");
            var stranger = PalsFriend("PebblePaws", 29, -0.5f, "Hat_Party");

            // ---- home
            var home = m.Group("Home", Vector2.zero);
            During(m, home, (0f, there));
            PalsIsland(m, home, 0);
            var you = PalsCapy(m, home, new Vector2(-50f, -150f), 70f);
            m.Face(you).At(0f, -1f);
            PalsPress(m, PalsHomeSide(m, home), tapFriends);

            // ---- their island: they step out to meet you
            var theirs = m.Group("Theirs", Vector2.zero);
            During(m, theirs, (there, cut));
            PalsIsland(m, theirs, 1);
            var you2 = PalsCapy(m, theirs, new Vector2(-50f, -150f), 70f);
            m.Face(you2).At(0f, -1f);
            var host = PalsCapy(m, theirs, new Vector2(40f, -140f), 70f, friend);
            m.Size(host).At(landed - 0.01f, 0f).At(landed, 0.3f, DemoEase.Step).At(landed + 0.4f, 1f, DemoEase.Back);
            PalsEmote(m, theirs, CapyIsland.Game.Emoji.Wave, new Vector2(40f, -62f), landed + 0.5f, cut - 0.1f);
            PalsSide(m, theirs, PalsSide1, IconPainter.House, PalsBlue, "Home");

            // ---- someone you've just met: Add, on the right
            var met = m.Group("Stranger", Vector2.zero);
            During(m, met, (cut, L));
            PalsIsland(m, met, 2);
            var you3 = PalsCapy(m, met, new Vector2(-50f, -150f), 70f);
            m.Face(you3).At(0f, -1f);
            PalsCapy(m, met, new Vector2(40f, -140f), 70f, stranger);
            PalsEmote(m, met, CapyIsland.Game.Emoji.Heart, new Vector2(40f, -62f), added + 0.1f, added + 1.6f);
            PalsSide(m, met, PalsSide1, IconPainter.House, PalsBlue, "Home");
            var addGroup = m.Group("Add", PalsSide2, met);
            PalsPress(m, PalsSide(m, addGroup, Vector2.zero, IconPainter.AddFriend, UiKit.Leaf, "Add"), tapAdd2);
            m.Size(addGroup).At(added, 1f).At(added + 0.25f, 0f);
            // Away from home, what happened is said at the top of the screen.
            var said = m.Card("Hint", new Vector2(-10f, 150f), new Vector2(430f, 64f), null, met);
            PalsPara(m, said, stranger.name + " is now your friend! Their island will be here next time.", Vector2.zero, 400f, 52f, 16f, UiKit.Ink);
            m.Between(said.gameObject, added + 0.2f, L - 0.3f);
            m.Size(said).At(added + 0.2f, 0.85f).At(added + 0.45f, 1f, DemoEase.Back);

            // ---- Neighbours, scrolled down to your code and your friends
            var sheet = PalsScreen(m, PalsSheetAt, PalsSheetSize, PalsBlue, IconPainter.Plane, "Neighbours", open, tapVisit + 0.15f, scroll, swipe, swiped);
            float w = sheet.Width;
            var lift = new Vector2(0f, scroll);
            PalsServer(m, sheet.Content, new Vector2(0f, -62f), w);
            PalsPlayTogether(m, sheet.Content, new Vector2(0f, -195f), w);
            LeftLabel(m, sheet.Content, "Your friends", new Vector2(-w * 0.5f + 6f, -296f), 17f, UiKit.InkSoft, 200f);
            var code = PalsCodeCard(m, sheet.Content, new Vector2(0f, -390f), w, PalsMyCode());
            // Typed in, Add: the box empties and the answer shows under it.
            PalsTyping(m, code.Typed, code.Hint, PalsCode, typeFrom, typeTo, sent);
            m.Between(code.Status.gameObject, sent, L);
            var rowAt = new Vector2(0f, -515f);
            var accept = m.Group("Accepted", rowAt, sheet.Content);
            var visit = sheet.Top + rowAt + PalsFriendRow(m, accept, Vector2.zero, w, friend) + lift;
            m.Between(accept.gameObject, accepted, L);
            m.Size(accept).At(accepted, 0.6f).At(accepted + 0.3f, 1f, DemoEase.Back);
            m.Sparkle(sheet.Top + rowAt + lift, accepted, UiKit.Sun, 5);
            var copy = sheet.Top + code.Copy + lift;
            var box = sheet.Top + code.Box + lift;
            var add = sheet.Top + code.Add + lift;

            // ---- the knock (KnockPanel): waiting for them to let you in
            var knocking = Panel(m, new Vector2(0f, 118f), new Vector2(540f, 100f), null, knock, letIn);
            UiKit.Avatar(knocking, friend, 66f).anchoredPosition = new Vector2(-226f, 0f);
            LeftLabel(m, knocking, "Waiting for " + PalsName + " to let you in…", new Vector2(-184f, 16f), 17f, UiKit.Ink, 290f);
            var countdown = LeftLabel(m, knocking, "You can cancel any time (45s)", new Vector2(-184f, -16f), 13f, UiKit.InkSoft, 290f);
            m.Do(s =>
            {
                int sec = Mathf.Clamp(45 - Mathf.FloorToInt(s - knock), 0, 45);
                string line = "You can cancel any time (" + sec + "s)";
                if (countdown.text != line) countdown.text = line;
            });
            PalsButton(m, knocking, new Vector2(210f, 0f), new Vector2(100f, 46f), UiKit.SurfaceSunk, "Cancel", UiKit.Ink);

            PalsFlight(m, fly, "Flying to " + PalsName + "'s island...");
            PalsDip(m, L, cut);

            var swipeFrom = new Vector2(-20f, -120f);
            m.Hand().Show(0.35f, tapVisit + 0.35f)
                .At(0.35f, PalsSide2 + new Vector2(-120f, -80f)).At(tapFriends - 0.2f, PalsSide2).Tap(tapFriends)
                .At(swipe - 0.1f, swipeFrom).Press(swipe).At(swipe, swipeFrom)
                .At(swiped - 0.15f, swipeFrom + new Vector2(0f, 190f), DemoEase.Linear).Lift(swiped - 0.1f)
                .At(tapCopy - 0.3f, copy).Tap(tapCopy)
                .At(tapBox - 0.3f, box).Tap(tapBox).At(typeFrom + 0.2f, box + new Vector2(30f, -60f)).At(typeTo, box + new Vector2(60f, -60f))
                .At(tapAdd - 0.25f, add).Tap(tapAdd).At(sent + 0.4f, add + new Vector2(50f, -50f)).At(accepted + 0.2f, add + new Vector2(50f, -50f))
                .At(tapVisit - 0.3f, visit).Tap(tapVisit).At(tapVisit + 0.35f, visit + new Vector2(60f, -60f));
            m.Hand().Show(tapAdd2 - 0.45f, tapAdd2 + 0.4f)
                .At(tapAdd2 - 0.45f, PalsSide2 + new Vector2(-120f, -80f)).At(tapAdd2 - 0.15f, PalsSide2).Tap(tapAdd2)
                .At(tapAdd2 + 0.4f, PalsSide2 + new Vector2(-90f, -70f));
        }

        /// <summary>
        /// Playing together. On a friend's island: tap Build together under their face (top left);
        /// "Asking…", then a heart, and it says Building: drag a forest out of your trail and plant it
        /// on their island. Tap the smiley button for the emoji tray, tap the wave: it pops up over your
        /// head, and they wave back. Then at home: Friends, Play together, Island party. The party card
        /// shows "Waiting for your guests…" until two islanders turn up beside you.
        /// </summary>
        static void TogetherMovie(HelpMovie m)
        {
            const float L = 15.4f;
            m.Length = L;
            const float tapAsk = 0.9f, asked = 1.0f, yes = 2.3f, grab = 2.9f, drop = 3.9f;
            const float tapSmile = 5.0f, tray = 5.1f, tapWave = 5.8f, waved = 5.9f, answer = 6.7f;
            const float cut = 8.3f, tapFriends = 8.9f, open = 9.1f, tapParty = 10.2f, hosting = 10.45f, arrive = 11.6f, greet = 12.3f;
            var friend = PalsFriend(PalsName, 5, 0.6f, "Hat_Straw");
            var smileAt = new Vector2(248f, -150f);

            // ---- their island
            var away = m.Group("Away", Vector2.zero);
            During(m, away, (0f, cut));
            PalsIsland(m, away, 1);
            var spot = new Vector2(95f, -85f);
            var planted = m.Pic("Planted", HelpArt.Planted("Medium Maple Forest", "Medium Pine Forest"), spot, 110f, away);
            m.Size(planted.transform).At(drop - 0.01f, 0f).At(drop, 0.2f, DemoEase.Step).At(drop + 0.4f, 1f, DemoEase.Back);
            var host = new Vector2(-25f, -105f);
            PalsCapy(m, away, host, 70f, friend);
            var me = new Vector2(-110f, -115f);
            var you = PalsCapy(m, away, me, 70f);
            m.Face(you).At(0f, -1f);
            var trail = new Vector2(-172f, -125f);
            var carried = m.Group("Carried", trail, away);
            m.Pic("Pic", HelpArt.Item("Medium Maple Forest", "Medium Pine Forest"), Vector2.zero, 56f, carried);
            var room = PlaceRing(m, carried, true, 110f);
            m.Fade(room).At(grab, 0f).At(grab + 0.15f, 0.45f).At(drop, 0.45f).At(drop + 0.1f, 0f);
            m.Move(carried).At(grab, trail).At(drop, spot, DemoEase.Linear);
            m.Between(carried.gameObject, 0f, drop);
            PalsEmote(m, away, CapyIsland.Game.Emoji.Heart, host + new Vector2(0f, 75f), yes, yes + 1.3f);
            PalsEmote(m, away, CapyIsland.Game.Emoji.Wave, me + new Vector2(0f, 75f), waved, waved + 1.5f);
            PalsEmote(m, away, CapyIsland.Game.Emoji.Wave, host + new Vector2(0f, 75f), answer, answer + 1.5f);

            // Who's here (VisitorsColumn): their face and name, and under it Build together.
            var faceAt = new Vector2(-225f, 128f);
            UiKit.Avatar(away, friend, 60f).anchoredPosition = faceAt;
            m.Shape("Online", UiKit.Circle, faceAt + new Vector2(22f, -20f), new Vector2(16f, 16f), UiKit.Leaf, away);
            var namePill = m.Shape("NamePill", UiKit.Rounded(10), faceAt + new Vector2(0f, -42f), new Vector2(96f, 22f), UiKit.HudGlass, away);
            namePill.type = Image.Type.Sliced;
            var nameText = m.Label("Name", friend.name, Vector2.zero, 13f, Color.white, namePill.transform);
            Fit(nameText, friend.name, 88f);
            PalsAsTyped(nameText);
            var askAt = faceAt + new Vector2(0f, -76f);
            var ask = PalsButton(m, away, askAt, new Vector2(126f, 32f), UiKit.Spring, "Build together", Color.white, IconPainter.House);
            m.Between(ask.gameObject, 0f, asked);
            PalsPress(m, ask, tapAsk);
            var waiting = new Color(UiKit.Spring.r * 0.75f, UiKit.Spring.g * 0.75f, UiKit.Spring.b * 0.75f, 0.6f);
            m.Between(PalsButton(m, away, askAt, new Vector2(126f, 32f), waiting, "Asking…", Color.white, IconPainter.House).gameObject, asked, yes);
            m.Between(PalsButton(m, away, askAt, new Vector2(110f, 28f), UiKit.Leaf, "Building", Color.white).gameObject, yes, L);

            PalsSide(m, away, PalsSide1, IconPainter.House, PalsBlue, "Home");
            var waveAt = PalsEmojiBar(m, away, smileAt, tray, waved, out var smile);
            PalsPress(m, smile, tapSmile);
            m.Sparkle(spot + new Vector2(0f, 40f), drop, UiKit.Leaf, 5);

            // ---- home: a party
            var home = m.Group("Home", Vector2.zero);
            During(m, home, (cut, L));
            PalsIsland(m, home, 0);
            var guestA = PalsCapy(m, home, new Vector2(50f, -125f), 66f, PalsFriend("mochi_dreams", 17, -0.5f, "Hat_Flowers"));
            var you2 = PalsCapy(m, home, new Vector2(-40f, -140f), 70f);
            m.Face(you2).At(0f, -1f);
            var guestB = PalsCapy(m, home, new Vector2(-140f, -150f), 66f, PalsFriend("PebblePaws", 29, 0.3f, "Hat_Beanie"));
            foreach (var guest in new[] { guestA, guestB })
                m.Size(guest).At(arrive - 0.01f, 0f).At(arrive, 0.3f, DemoEase.Step).At(arrive + 0.4f, 1f, DemoEase.Back);
            m.Face(guestB).At(0f, -1f);
            PalsEmote(m, home, CapyIsland.Game.Emoji.Wave, new Vector2(50f, -55f), greet, greet + 1.5f);
            PalsPress(m, PalsHomeSide(m, home), tapFriends);
            PalsEmojiBar(m, home, smileAt, -1f, -1f, out var homeSmile);
            m.Between(homeSmile.gameObject, arrive, L);

            // The party card (GatheringPanel), top left.
            var pink = UiKit.Hex("E58FB0");
            var partyCard = m.Card("Party", new Vector2(-150f, 118f), new Vector2(260f, 104f), null, home);
            m.Between(partyCard.gameObject, hosting, L);
            m.Size(partyCard).At(hosting, 0.85f).At(hosting + 0.25f, 1f, DemoEase.Back);
            var disc = m.Shape("Disc", UiKit.Circle, new Vector2(-98f, 18f), new Vector2(44f, 44f), pink, partyCard);
            m.Shape("Icon", IconPainter.Star, Vector2.zero, new Vector2(28f, 28f), Color.white, disc.transform).preserveAspect = true;
            LeftLabel(m, partyCard, "ISLAND PARTY", new Vector2(-68f, 30f), 13f, Color.Lerp(pink, UiKit.Ink, 0.15f), 170f);
            m.Between(LeftLabel(m, partyCard, "Waiting for your guests…", new Vector2(-68f, 8f), 15f, UiKit.Ink, 180f).gameObject, 0f, arrive);
            m.Between(LeftLabel(m, partyCard, "2 guests here", new Vector2(-68f, 8f), 15f, UiKit.Ink, 180f).gameObject, arrive, L);
            PalsButton(m, partyCard, new Vector2(0f, -30f), new Vector2(236f, 30f), UiKit.SurfaceSunk, "End party", UiKit.Ink);

            // Neighbours: Play together is right under the server.
            var sheet = PalsScreen(m, PalsSheetAt, PalsSheetSize, PalsBlue, IconPainter.Plane, "Neighbours", open, hosting - 0.1f);
            PalsServer(m, sheet.Content, new Vector2(0f, -62f), sheet.Width);
            var party = sheet.Top + PalsPlayTogether(m, sheet.Content, new Vector2(0f, -195f), sheet.Width);

            PalsDip(m, L, cut);

            m.Hand().Show(0.35f, drop + 0.2f)
                .At(0.35f, askAt + new Vector2(120f, -60f)).At(tapAsk - 0.25f, askAt).Tap(tapAsk)
                .At(grab - 0.3f, trail + new Vector2(0f, 20f)).Press(grab).At(grab, trail + new Vector2(0f, 20f))
                .At(drop, spot + new Vector2(0f, 30f), DemoEase.Linear).Lift(drop + 0.05f).At(drop + 0.2f, spot + new Vector2(40f, 60f));
            m.Hand().Show(tapSmile - 0.4f, tapWave + 0.4f)
                .At(tapSmile - 0.4f, smileAt + new Vector2(-100f, 60f)).At(tapSmile - 0.15f, smileAt).Tap(tapSmile)
                .At(tapWave - 0.3f, waveAt).Tap(tapWave).At(tapWave + 0.4f, waveAt + new Vector2(-40f, 70f));
            m.Hand().Show(tapFriends - 0.45f, tapParty + 0.4f)
                .At(tapFriends - 0.45f, PalsSide2 + new Vector2(-120f, -80f)).At(tapFriends - 0.15f, PalsSide2).Tap(tapFriends)
                .At(tapParty - 0.35f, party).Tap(tapParty).At(tapParty + 0.4f, party + new Vector2(60f, -60f));
        }

        // ---------------------------------------------------------------- settings, account and save code

        sealed class PalsSettingsSheet
        {
            public PalsSheet Sheet;
            /// <summary>Where each button is on the stage once the list has scrolled.</summary>
            public Vector2 Export, Load, Make, SignIn;
            public Transform AccountRow;
            public TextMeshProUGUI AccountLabel;
            public RectTransform MakeButton;
        }

        static (Image row, TextMeshProUGUI label) PalsRow(HelpMovie m, Transform parent, float y, float width, Sprite icon, string label)
        {
            var row = PalsCard(m, parent, new Vector2(0f, y), new Vector2(width, 50f), Color.white);
            m.Shape("Icon", icon, new Vector2(-width * 0.5f + 26f, 0f), new Vector2(26f, 26f), UiKit.InkSoft, row.transform).preserveAspect = true;
            var text = LeftLabel(m, row.transform, label, new Vector2(-width * 0.5f + 48f, 0f), 16f, UiKit.Ink, width - 200f);
            return (row, text);
        }

        static (Image row, TextMeshProUGUI label, RectTransform button) PalsRowButton(HelpMovie m, Transform parent, float y, float width,
            Sprite icon, string label, string words, Color fill, Color ink)
        {
            var (row, text) = PalsRow(m, parent, y, width, icon, label);
            var button = PalsButton(m, row.transform, new Vector2(width * 0.5f - 64f, 0f), new Vector2(112f, 36f), fill, words, ink);
            return (row, text, button);
        }

        /// <summary>An on/off switch at the end of a row, with On or Off beside it.</summary>
        static void PalsToggle(HelpMovie m, Transform row, float width, bool on)
        {
            m.Label("State", on ? "On" : "Off", new Vector2(width * 0.5f - 92f, 0f), 14f, UiKit.InkSoft, row);
            var track = m.Shape("Toggle", UiKit.Rounded(14), new Vector2(width * 0.5f - 42f, 0f), new Vector2(52f, 28f), on ? UiKit.Leaf : UiKit.SurfaceSunk, row);
            track.type = Image.Type.Sliced;
            m.Shape("Knob", UiKit.Circle, new Vector2(on ? 12f : -12f, 0f), new Vector2(22f, 22f), Color.white, track.transform);
        }

        /// <summary>
        /// Settings (SettingsScreen), its rows in their real order, swiped down to the island and
        /// account rows at the bottom.
        /// </summary>
        static PalsSettingsSheet PalsSettings(HelpMovie m, float from, float to, float swipe, float swiped)
        {
            const float scroll = 320f;
            var sheet = PalsScreen(m, PalsSheetAt, PalsSheetSize, PalsGear, IconPainter.Gear, "Settings", from, to, scroll, swipe, swiped);
            float w = sheet.Width;
            var list = sheet.Content;
            float Y(int i) => -30f - i * 58f;
            Vector2 Spot(int i) => sheet.Top + new Vector2(w * 0.5f - 64f, Y(i) + scroll);
            PalsToggle(m, PalsRow(m, list, Y(0), w, IconPainter.Magnifier, "Easy Reading").row.transform, w, false);
            PalsToggle(m, PalsRow(m, list, Y(1), w, IconPainter.Sound, "Sound").row.transform, w, true);
            PalsToggle(m, PalsRow(m, list, Y(2), w, IconPainter.Paw, "Walk with a joystick").row.transform, w, false);
            PalsToggle(m, PalsRow(m, list, Y(3), w, IconPainter.Book, "Tutorial and tips").row.transform, w, true);
            PalsRowButton(m, list, Y(4), w, IconPainter.Goals, "Show me around", "Start tour", UiKit.Spring, Color.white);
            PalsRowButton(m, list, Y(5), w, IconPainter.Star, "The story so far", "Watch", UiKit.Spring, Color.white);
            PalsRowButton(m, list, Y(6), w, IconPainter.Star, "Keep a copy of your island", "Export", UiKit.Spring, Color.white);
            PalsRowButton(m, list, Y(7), w, IconPainter.Star, "Bring back a saved island", "Load", UiKit.Spring, Color.white);
            var (accountRow, accountLabel, makeButton) = PalsRowButton(m, list, Y(8), w, IconPainter.Star, "Play on another device", "Make account", UiKit.Spring, Color.white);
            PalsRowButton(m, list, Y(9), w, IconPainter.Star, "Have an account already?", "Sign in", UiKit.SurfaceSunk, UiKit.Ink);
            return new PalsSettingsSheet
            {
                Sheet = sheet, Export = Spot(6), Load = Spot(7), Make = Spot(8), SignIn = Spot(9),
                AccountRow = accountRow.transform, AccountLabel = accountLabel, MakeButton = makeButton,
            };
        }

        sealed class PalsAccountParts { public Vector2 User, Pass, Go; }

        /// <summary>The account window (AccountScreen): Make an account, or Sign in, with the username and password typed in.</summary>
        static PalsAccountParts PalsAccountCard(HelpMovie m, bool signIn, float from, float to, float userFrom, float userTo,
            float passFrom, float passTo, float go, string username)
        {
            PalsScrim(m, from, to);
            var card = Panel(m, Vector2.zero, new Vector2(440f, 370f), null, from, to);
            string title = signIn ? "Sign in" : "Make an account";
            Fit(m.Label("Title", title, new Vector2(0f, 154f), 26f, UiKit.Ink, card), title, 400f);
            PalsPara(m, card, signIn
                    ? "Open the island from your other device. If the two islands are different, you'll choose which one to keep."
                    : "Keep this island in an account, and open it on another device too.",
                new Vector2(0f, 118f), 400f, 36f, 13f, UiKit.InkSoft);
            PalsButton(m, card, new Vector2(0f, 78f), new Vector2(400f, 40f), UiKit.SurfaceSunk, "Continue with Google", UiKit.Ink);
            string or = signIn ? "Or with your username and password:" : "Or pick a username and password:";
            Fit(m.Label("Or", or, new Vector2(0f, 46f), 13f, UiKit.InkSoft, card), or, 400f);
            var user = new Vector2(0f, 12f);
            var pass = new Vector2(0f, -38f);
            var (userHint, userText) = PalsInput(m, card, user, new Vector2(400f, 42f), "Username");
            var (passHint, passText) = PalsInput(m, card, pass, new Vector2(400f, 42f), "Password");
            PalsTyping(m, userText, userHint, username, userFrom, userTo);
            PalsTyping(m, passText, passHint, "**********", passFrom, passTo);
            var status = m.Label("Status", signIn ? "Signing in…" : "Making your account…", new Vector2(0f, -76f), 14f, UiKit.InkSoft, card);
            m.Between(status.gameObject, go + 0.1f, to + 0.5f);
            var goAt = new Vector2(102f, -134f);
            PalsButton(m, card, new Vector2(-102f, -134f), new Vector2(196f, 46f), UiKit.SurfaceSunk, "Cancel", UiKit.Ink);
            PalsButton(m, card, goAt, new Vector2(196f, 46f), UiKit.Primary, signIn ? "Sign in" : "Make account", Color.white);
            return new PalsAccountParts { User = user, Pass = pass, Go = goAt };
        }

        /// <summary>A whole screen as one device: its edge (shown only when it's small) and the sea behind the island.</summary>
        static RectTransform PalsDevice(HelpMovie m, string name, out Image bezel)
        {
            var g = m.Group(name, Vector2.zero);
            bezel = m.Shape("Bezel", UiKit.Rounded(40), Vector2.zero, new Vector2(660f, 460f), UiKit.Hex("2B211A"), g);
            bezel.type = Image.Type.Sliced;
            m.Shape("Screen", null, Vector2.zero, new Vector2(600f, 400f), HelpArt.Sea, g);
            return g;
        }

        /// <summary>
        /// An account. On this device: Settings, swipe down to "Play on another device", Make account:
        /// type a username and password, Make account, and the row now says "Signed in as …". Then
        /// this phone moves aside for another one with a bare new island: Settings, swipe down, Sign
        /// in on "Have an account already?", type the same username and password, Sign in, and your
        /// island comes down onto it.
        /// </summary>
        static void AccountMovie(HelpMovie m)
        {
            const float L = 17.4f;
            m.Length = L;
            const float tapSettings = 0.9f, open = 1.1f, swipe = 1.5f, swiped = 2.1f, tapMake = 2.7f, card = 2.9f;
            const float tapUser = 3.5f, userFrom = 3.65f, userTo = 4.3f, tapPass = 4.6f, passFrom = 4.75f, passTo = 5.3f, tapGo = 5.8f, made = 6.5f, shut = 7.5f;
            const float shrink = 7.9f, other = 8.3f, small = 8.5f, grow = 8.9f, big = 9.5f;
            const float tapSettings2 = 10.0f, open2 = 10.2f, swipe2 = 10.6f, swiped2 = 11.2f, tapSignIn = 11.8f, card2 = 12.0f;
            const float tapUser2 = 12.5f, user2From = 12.65f, user2To = 13.3f, tapPass2 = 13.5f, pass2From = 13.65f, pass2To = 14.2f, tapGo2 = 14.6f, signedIn = 15.3f;
            const string username = "IslandFan";

            // Between the two devices, they sit side by side.
            var desk = m.Shape("Desk", null, Vector2.zero, new Vector2(640f, 440f), UiKit.SurfaceSunk);
            m.Fade(desk).At(shrink, 0f).At(shrink + 0.3f, 1f).At(grow, 1f).At(big, 0f);

            // ---- this device
            var here = PalsDevice(m, "ThisDevice", out var hereBezel);
            During(m, here, (0f, grow));
            m.Size(here).At(shrink, 1f).At(small, 0.36f);
            m.Move(here).At(shrink, Vector2.zero).At(small, new Vector2(-150f, 0f));
            m.Fade(hereBezel).At(shrink, 0f).At(shrink + 0.2f, 1f);
            PalsIsland(m, here, 0);
            var you = PalsCapy(m, here, new Vector2(-50f, -150f), 70f);
            m.Face(you).At(0f, -1f);
            PalsPress(m, PalsAccountSide(m, here), tapSettings);

            // ---- the other device: a bare island until you sign in
            var there = PalsDevice(m, "OtherDevice", out var thereBezel);
            During(m, there, (other, L));
            m.Size(there).At(other, 0.36f).At(grow, 0.36f).At(big, 1f);
            m.Move(there).At(other, new Vector2(150f, 0f)).At(grow, new Vector2(150f, 0f)).At(big, Vector2.zero);
            m.Fade(thereBezel).At(grow, 1f).At(big, 0f);
            var bare = m.Group("Bare", Vector2.zero, there);
            m.Between(bare.gameObject, 0f, signedIn + 0.1f);
            PalsIsland(m, bare, 3);
            var yours = m.Group("Yours", Vector2.zero, there);
            m.Between(yours.gameObject, signedIn + 0.1f, L);
            PalsIsland(m, yours, 0);
            var you2 = PalsCapy(m, there, new Vector2(-50f, -150f), 70f);
            m.Face(you2).At(0f, -1f);
            PalsPress(m, PalsAccountSide(m, there), tapSettings2);

            // ---- making the account
            var s1 = PalsSettings(m, open, shut, swipe, swiped);
            var signedAs = LeftLabel(m, s1.AccountRow, "Signed in as " + username, new Vector2(-s1.Sheet.Width * 0.5f + 48f, 0f), 16f, UiKit.Ink, s1.Sheet.Width - 120f);
            m.Between(signedAs.gameObject, made, L);
            m.Between(s1.AccountLabel.gameObject, 0f, made);
            m.Between(s1.MakeButton.gameObject, 0f, made);
            var a = PalsAccountCard(m, false, card, made, userFrom, userTo, passFrom, passTo, tapGo, username);

            // ---- signing in on the other device
            var s2 = PalsSettings(m, open2, signedIn, swipe2, swiped2);
            var b = PalsAccountCard(m, true, card2, signedIn, user2From, user2To, pass2From, pass2To, tapGo2, username);
            m.Sparkle(new Vector2(-60f, -20f), signedIn + 0.3f, UiKit.Sun, 8);

            PalsDip(m, L, signedIn + 0.1f);

            var swipeFrom = new Vector2(-20f, -120f);
            m.Hand().Show(0.35f, made + 0.2f)
                .At(0.35f, PalsSide1 + new Vector2(-120f, -80f)).At(tapSettings - 0.2f, PalsSide1).Tap(tapSettings)
                .At(swipe - 0.1f, swipeFrom).Press(swipe).At(swipe, swipeFrom)
                .At(swiped - 0.15f, swipeFrom + new Vector2(0f, 200f), DemoEase.Linear).Lift(swiped - 0.1f)
                .At(tapMake - 0.3f, s1.Make).Tap(tapMake)
                .At(tapUser - 0.3f, a.User).Tap(tapUser).At(userTo, a.User + new Vector2(80f, -50f))
                .At(tapPass - 0.2f, a.Pass).Tap(tapPass).At(passTo, a.Pass + new Vector2(80f, -50f))
                .At(tapGo - 0.3f, a.Go).Tap(tapGo).At(made + 0.2f, a.Go + new Vector2(60f, -40f));
            m.Hand().Show(tapSettings2 - 0.4f, signedIn - 0.4f)
                .At(tapSettings2 - 0.4f, PalsSide1 + new Vector2(-120f, -80f)).At(tapSettings2 - 0.2f, PalsSide1).Tap(tapSettings2)
                .At(swipe2 - 0.1f, swipeFrom).Press(swipe2).At(swipe2, swipeFrom)
                .At(swiped2 - 0.15f, swipeFrom + new Vector2(0f, 200f), DemoEase.Linear).Lift(swiped2 - 0.1f)
                .At(tapSignIn - 0.3f, s2.SignIn).Tap(tapSignIn)
                .At(tapUser2 - 0.3f, b.User).Tap(tapUser2).At(user2To, b.User + new Vector2(80f, -50f))
                .At(tapPass2 - 0.2f, b.Pass).Tap(tapPass2).At(pass2To, b.Pass + new Vector2(80f, -50f))
                .At(tapGo2 - 0.3f, b.Go).Tap(tapGo2).At(signedIn - 0.4f, b.Go + new Vector2(60f, -40f));
        }

        /// <summary>
        /// The save code. Settings, swipe down, Export on "Keep a copy of your island": the save-code
        /// window shows the start of the code; tap Copy ("Copied! Now paste it into a note."), then
        /// Done. Load on "Bring back a saved island": the window warns that the island you're on
        /// gives way; tap Paste, the code is found and Load lights up; tap Load, and the island in the
        /// code replaces this one.
        /// </summary>
        static void SaveCodeMovie(HelpMovie m)
        {
            const float L = 12.6f;
            m.Length = L;
            const float tapSettings = 0.9f, open = 1.1f, swipe = 1.5f, swiped = 2.1f, tapExport = 2.7f, card1 = 2.9f;
            const float tapCopy = 3.9f, copied = 4.0f, tapDone = 5.0f, tapLoad = 5.8f, card2 = 6.0f;
            const float tapPaste = 7.1f, found = 7.2f, tapGo = 8.3f, loaded = 8.45f, swap = loaded + 0.25f;
            // What the window shows of a code (SaveCodeScreen.Preview): its first 28 letters.
            const string preview = "CAPY1:eyJrZXlzIjpbeyJrIjoiQ2…";

            var before = m.Group("Before", Vector2.zero);
            During(m, before, (0f, swap));
            PalsIsland(m, before, 0);
            var after = m.Group("After", Vector2.zero);
            During(m, after, (swap, L));
            PalsIsland(m, after, 4);
            var you = PalsCapy(m, null, new Vector2(-50f, -150f), 70f);
            m.Face(you).At(0f, -1f);
            PalsPress(m, PalsAccountSide(m, null), tapSettings);
            m.Sparkle(new Vector2(0f, -10f), swap + 0.3f, UiKit.Sun, 8);

            var settings = PalsSettings(m, open, loaded, swipe, swiped);

            // ---- Export: the code, Copy, Done
            PalsScrim(m, card1, tapDone);
            var exportCard = Panel(m, Vector2.zero, new Vector2(440f, 300f), null, card1, tapDone);
            Fit(m.Label("Title", "Your save code", new Vector2(0f, 118f), 26f, UiKit.Ink, exportCard), "Your save code", 400f);
            PalsPara(m, exportCard, "Copy it and paste it into a note somewhere safe. To bring this island back, on this phone or another device, choose \"Load a save\" on the first screen and paste it in.",
                new Vector2(0f, 70f), 400f, 56f, 13f, UiKit.InkSoft);
            var well = PalsCard(m, exportCard, new Vector2(0f, 6f), new Vector2(400f, 62f), UiKit.SurfaceSunk);
            PalsAsTyped(m.Label("Code", preview, new Vector2(0f, 11f), 16f, UiKit.InkSoft, well.transform));
            m.Label("Count", "48213 characters", new Vector2(0f, -13f), 12f, UiKit.InkSoft, well.transform);
            var copiedLine = m.Label("Status", "Copied! Now paste it into a note.", new Vector2(0f, -44f), 15f, UiKit.Leaf, exportCard);
            Fit(copiedLine, "Copied! Now paste it into a note.", 400f);
            m.Between(copiedLine.gameObject, copied, L);
            var done = new Vector2(-100f, -104f);
            var copy = new Vector2(100f, -104f);
            PalsButton(m, exportCard, done, new Vector2(190f, 46f), UiKit.SurfaceSunk, "Done", UiKit.Ink);
            PalsButton(m, exportCard, copy, new Vector2(190f, 46f), UiKit.Primary, "Copy", Color.white);

            // ---- Load: Paste, then Load
            PalsScrim(m, card2, loaded);
            var importCard = Panel(m, Vector2.zero, new Vector2(440f, 300f), null, card2, loaded);
            Fit(m.Label("Title", "Load a save", new Vector2(0f, 118f), 26f, UiKit.Ink, importCard), "Load a save", 400f);
            PalsPara(m, importCard, "Paste the save code you kept in a note. The island you're on now gives way to that one, so export this one first if you want to keep it.",
                new Vector2(0f, 70f), 400f, 56f, 13f, UiKit.InkSoft);
            var (pasteHint, pasted) = PalsInput(m, importCard, new Vector2(0f, 12f), new Vector2(400f, 46f), "Paste your save code here");
            PalsTyping(m, pasted, pasteHint, preview, found, found + 0.01f);
            var foundLine = PalsPara(m, importCard, "Save code found. Tap Load to bring your island back.", new Vector2(0f, -36f), 400f, 30f, 14f, UiKit.Leaf);
            m.Between(foundLine.gameObject, found, L);
            var paste = new Vector2(0f, -104f);
            var load = new Vector2(138f, -104f);
            PalsButton(m, importCard, new Vector2(-138f, -104f), new Vector2(126f, 46f), UiKit.SurfaceSunk, "Cancel", UiKit.Ink);
            PalsButton(m, importCard, paste, new Vector2(126f, 46f), UiKit.Spring, "Paste", Color.white);
            var loadButton = PalsButton(m, importCard, load, new Vector2(126f, 46f), UiKit.Primary, "Load", Color.white);
            // Load can't be pressed until a code is found.
            var greyed = loadButton.gameObject.AddComponent<CanvasGroup>();
            m.Do(s => greyed.alpha = s >= found ? 1f : 0.45f);

            PalsDip(m, L, swap);

            var swipeFrom = new Vector2(-20f, -120f);
            m.Hand().Show(0.35f, tapGo + 0.3f)
                .At(0.35f, PalsSide1 + new Vector2(-120f, -80f)).At(tapSettings - 0.2f, PalsSide1).Tap(tapSettings)
                .At(swipe - 0.1f, swipeFrom).Press(swipe).At(swipe, swipeFrom)
                .At(swiped - 0.15f, swipeFrom + new Vector2(0f, 200f), DemoEase.Linear).Lift(swiped - 0.1f)
                .At(tapExport - 0.3f, settings.Export).Tap(tapExport)
                .At(tapCopy - 0.35f, copy).Tap(tapCopy)
                .At(tapDone - 0.35f, done).Tap(tapDone)
                .At(tapLoad - 0.3f, settings.Load).Tap(tapLoad)
                .At(tapPaste - 0.35f, paste).Tap(tapPaste)
                .At(tapGo - 0.35f, load).Tap(tapGo).At(tapGo + 0.3f, load + new Vector2(60f, -50f));
        }
    }
}
