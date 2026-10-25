using System;
using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The travel movies: getting about on the Island Map, and paying a spirit to raise a bridge.
    /// Both draw the islands with the game's own land art (LandMap's shore, sand and grass, the same
    /// pictures the map camera photographs), so the shapes, the sand and the land bridges are the
    /// real ones; away from home (a friend's island) they fall back to painted ovals.
    /// </summary>
    public static partial class HelpDemos
    {
        // ---------------------------------------------------------------- fast travel with the map

        /// <summary>
        /// The map as the game plays it (MapScreen). On the island, tap Map in the column on the
        /// right: the Island Map opens, a picture of the whole archipelago with every island's name,
        /// the land bridges you've opened joining Home Isle to the Heart of the Isles and on to
        /// Pebble Cove, and a padlock on each island no bridge reaches yet. Press your own pin (your
        /// face, "You" under it) and drag it onto a padlocked island: let go and it springs back,
        /// with "That island is still closed. Open its bridge first!" across the top. Drag it down
        /// over the bridges to Pebble Cove instead: the map closes, and you're there in a puff.
        /// </summary>
        static void MapMovie(HelpMovie m)
        {
            const float L = 9.4f;
            m.Length = L;
            const float tapMap = 0.95f, open = 1.15f;
            const float grab1 = 1.95f, carry1 = 2.1f, drop1 = 3.2f;     // onto the padlock: refused
            const float grab2 = 4.55f, carry2 = 4.7f, drop2 = 6.3f;     // over the bridges: off you go
            const float shut = drop2 + 0.05f, arrive = shut + 0.12f;

            // ---- on the island: home, and after the jump, Pebble Cove
            var home = m.Group("Home", Vector2.zero);
            During(m, home, (0f, shut));
            m.Island("Island", new Vector2(-40f, -45f), new Vector2(470f, 250f), home);
            m.Pic("House", HelpArt.Item("Capy House"), new Vector2(-150f, -10f), 105f, home);
            m.Pic("Forest", HelpArt.Item("Medium Pine Forest"), new Vector2(40f, 15f), 90f, home);
            var (_, homeBody) = Walker(m, "You", HelpArt.You, new Vector2(-50f, -110f), 80f, home);
            m.Face(homeBody).At(0f, -1f);

            // Pebble Cove, with what turned up there when its bridge opened (a mine, a hill, an apple tree).
            var cove = m.Group("Cove", Vector2.zero);
            During(m, cove, (shut, L));
            m.Island("Island", new Vector2(-30f, -45f), new Vector2(490f, 250f), cove);
            m.Pic("Mine", HelpArt.Item("Mine"), new Vector2(-165f, -10f), 110f, cove);
            m.Pic("Hill", HelpArt.Item("Large Gray Hill", "Medium Gray Hill"), new Vector2(50f, 10f), 84f, cove);
            m.Pic("Tree", HelpArt.Item("Apple Tree"), new Vector2(140f, -70f), 84f, cove);
            var landed = new Vector2(-45f, -110f);
            var (arrived, arrivedBody) = Walker(m, "You", HelpArt.You, landed, 80f, cove);
            m.Face(arrivedBody).At(0f, -1f);
            m.Fade(arrived).At(arrive - 0.01f, 0f).At(arrive, 1f, DemoEase.Step);
            // TravelTo: a puff where you land, a twinkle, and a little squash.
            m.Size(arrived).At(arrive, 0.6f).At(arrive + 0.15f, 1.18f, DemoEase.Out).At(arrive + 0.45f, 1f, DemoEase.Back);
            TripPoof(m, cove, landed + new Vector2(0f, 36f), arrive);
            m.Sparkle(landed + new Vector2(0f, 70f), arrive + 0.1f, Color.white, 8);

            // The column of round buttons on the right (Settings, Help, Map, Hobbies…).
            var column = new Vector2(262f, 121f);
            TripHudButton(m, "Settings", IconPainter.Gear, UiKit.Hex("6B8FA8"), column);
            TripHudButton(m, "Help", HelpArt.Question, HelpScreen.Accent, column + new Vector2(0f, -81f));
            var mapButtonAt = column + new Vector2(0f, -162f);
            var mapButton = TripHudButton(m, "Map", IconPainter.Map, UiKit.Spring, mapButtonAt);
            TripHudButton(m, "Hobbies", IconPainter.Rod, UiKit.Hex("3E9C8F"), column + new Vector2(0f, -243f));
            m.Size(mapButton).At(tapMap - 0.1f, 1f).At(tapMap, 0.88f).At(tapMap + 0.2f, 1f, DemoEase.Back);

            // ---- the Island Map: its teal title bar, the picture, and how to use it underneath
            var sheet = Panel(m, Vector2.zero, new Vector2(592f, 392f), UiKit.Spring, open, shut);
            m.Shape("Icon", IconPainter.Map, new Vector2(-262f, 174f), new Vector2(28f, 28f), Color.white, sheet).preserveAspect = true;
            LeftLabel(m, sheet, "Island Map", new Vector2(-240f, 174f), 24f, Color.white, 320f);
            var closeDisc = m.Shape("Close", UiKit.Circle, new Vector2(266f, 174f), new Vector2(32f, 32f), new Color(1f, 1f, 1f, 0.25f), sheet);
            m.Shape("X", IconPainter.Close, Vector2.zero, new Vector2(16f, 16f), Color.white, closeDisc.transform).preserveAspect = true;
            const string hintWords = "Tap a spot or drag yourself to travel";
            Fit(m.Label("Hint", hintWords, new Vector2(0f, -173f), 15f, UiKit.InkSoft, sheet), hintWords, 560f);

            var frameAt = new Vector2(0f, -2f);
            var frame = m.Shape("Frame", UiKit.Rounded(30), frameAt, new Vector2(576f, 296f), UiKit.Hex("8FD3E0"), sheet);
            frame.type = Image.Type.Sliced;
            frame.gameObject.AddComponent<RectMask2D>();
            var picture = frame.transform;

            // The whole original world square across the frame (Unity units → movie pixels).
            const float scale = 3.15f;
            var middle = LandMap.WorldToUnity(new Vector2(2250f, 2250f));
            Vector2 Spot(Vector2 w) => (LandMap.WorldToUnity(w) - middle) * scale;

            // Every island, and the two bridges opened so far: Home Isle to the Heart of the Isles
            // (the first, opened by the story) and on to Pebble Cove. A shut bridge isn't drawn at all.
            var drawn = new[]
            {
                "Capy Island", "Heart of the Isles", "Ginger Cove", "Verdant Bay", "Wedding Isle", "Pirate Cove", "Monster Isle",
                "first_bridge", "second_bridge",
            };
            if (TripLand(m, picture, middle, Vector2.zero, scale, drawn) == null)
                TripFallbackLand(m, picture, Spot, drawn);
            // A red gate over the middle of each open bridge (BridgeGates), tiny from up here.
            var torii = Resources.Load<Sprite>("World/Torii_Front");
            var firstBridge = Spot(new Vector2(610.56f, 1485f));
            var secondBridge = Spot(new Vector2(508.55f, 2970f));
            if (torii != null)
            {
                m.Pic("Torii", torii, firstBridge, 20f, picture);
                m.Pic("Torii", torii, secondBridge, 20f, picture);
            }

            // The names along the top of each island, then the padlocks, then your pin, as the map stacks them.
            var nameBack = new Color(0.12f, 0.2f, 0.24f, 0.62f);
            (string id, Vector2 top)[] tags =
            {
                ("Capy Island", new Vector2(562.5f, 177f)), ("Heart of the Isles", new Vector2(562.5f, 1649f)),
                ("Ginger Cove", new Vector2(1125f, 3084f)), ("Verdant Bay", new Vector2(3375f, 3003f)),
                ("Wedding Isle", new Vector2(3937.5f, 224f)), ("Pirate Cove", new Vector2(2250f, 22f)),
                ("Monster Isle", new Vector2(2812.5f, 1579f)),
            };
            foreach (var (tagId, tagTop) in tags)
                TripName(m, picture, StoryText.Name(tagId), Spot(tagTop) - new Vector2(0f, 10f), 13f, nameBack);
            // A padlock on every island you can't walk to yet, in the middle of its spawn rect.
            var shutIsland = Spot(new Vector2(2250f, 742.5f));   // Barnacle Bay, the one we try
            TripLock(m, picture, shutIsland, 40f);
            TripLock(m, picture, Spot(new Vector2(2812.5f, 2227.5f)), 40f);
            TripLock(m, picture, Spot(new Vector2(3937.5f, 1485f)), 40f);
            TripLock(m, picture, Spot(new Vector2(3375f, 3712.5f)), 40f);

            // You: your face in the orange ring with "You" under it, where you stand on Home Isle.
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            var face = king != null ? MapMarkers.SpriteOf(king.gameObject) : null;
            if (face == null) face = HelpArt.You;
            var homePin = Spot(new Vector2(640f, 900f));
            var coveDrop = Spot(new Vector2(543f, 3790f));
            var pin = TripPin(m, picture, face, homePin);

            // First try: over the water onto the padlocked island. Let go and it springs straight back.
            Vector2 Hop(float u)
            {
                float e = DemoEase.InOut(u);
                return Vector2.Lerp(homePin, shutIsland, e) + new Vector2(0f, Mathf.Sin(e * Mathf.PI) * 30f);
            }
            // Then: down over both bridges to Pebble Cove.
            var route = new[] { homePin, firstBridge, Spot(new Vector2(600f, 2250f)), secondBridge, coveDrop };
            Vector2 Along(float u) => TripAlong(route, DemoEase.InOut(u));

            const int steps = 10;
            var pinMove = m.Move(pin).At(carry1, homePin);
            for (int i = 1; i <= steps; i++) pinMove.At(carry1 + (drop1 - carry1) * i / steps, Hop(i / (float)steps), DemoEase.Linear);
            pinMove.At(drop1 + 0.02f, homePin, DemoEase.Step).At(carry2, homePin);
            for (int i = 1; i <= steps; i++) pinMove.At(carry2 + (drop2 - carry2) * i / steps, Along(i / (float)steps), DemoEase.Linear);
            // Carried a little bigger while it's held.
            m.Size(pin).At(grab1, 1f).At(grab1 + 0.15f, 1.2f).At(drop1, 1.2f).At(drop1 + 0.15f, 1f)
                .At(grab2, 1f).At(grab2 + 0.15f, 1.2f);

            // Refused: the line across the top of the map, for a couple of seconds.
            TripToast(m, picture, "That island is still closed. Open its bridge first!", new Vector2(0f, 121f), drop1);

            // A blink of dark where the loop starts again.
            var dip = m.Shape("Dip", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("2E1A0E"));
            m.Fade(dip).At(0f, 1f).At(0.3f, 0f).At(L - 0.35f, 0f).At(L, 1f);

            // The finger: Map, then the two drags. (Stage = frame + its place on the sheet.)
            Vector2 S(Vector2 local) => frameAt + local;
            var hand = m.Hand().Show(0.35f, drop2 + 0.45f)
                .At(0.35f, mapButtonAt + new Vector2(-120f, 60f)).At(tapMap - 0.22f, mapButtonAt).Tap(tapMap)
                .At(grab1 - 0.3f, S(homePin)).Press(grab1).At(carry1, S(homePin));
            for (int i = 1; i <= steps; i++) hand.At(carry1 + (drop1 - carry1) * i / steps, S(Hop(i / (float)steps)), DemoEase.Linear);
            hand.Lift(drop1 + 0.05f).At(drop1 + 0.5f, S(shutIsland) + new Vector2(40f, -50f))
                .At(grab2 - 0.3f, S(homePin)).Press(grab2).At(carry2, S(homePin));
            for (int i = 1; i <= steps; i++) hand.At(carry2 + (drop2 - carry2) * i / steps, S(Along(i / (float)steps)), DemoEase.Linear);
            hand.Lift(drop2 + 0.05f).At(drop2 + 0.45f, S(coveDrop) + new Vector2(110f, -40f));
        }

        // ---------------------------------------------------------------- opening a bridge

        /// <summary>
        /// Opening the bridge to Pebble Cove, as the game does it (BridgeGhost). The spirit floats on
        /// the Heart of the Isles across the water from Pebble Cove. Tap it: the buy panel shows
        /// "Bridge to Pebble Cove" and its price (100 wood, 100 gold); tap Purchase and the counters
        /// drop. The game pauses for its little movie (BridgeCutscene): in a bubble, the spirit waves
        /// its paws over the sea… a sad puff of nothing; it tries much harder, and the land bridge
        /// bursts up out of the water and soaks you both. "A bridge to Pebble Cove!", a bow, and the
        /// spirit fades. Back on the island the bridge's land swells up out of the sea with a
        /// twinkle, a red gate rises over its middle, and the spirit floats away. Tap Pebble Cove and
        /// you walk across, under the gate.
        /// </summary>
        static void BridgeMovie(HelpMovie m)
        {
            const float L = 14f;
            m.Length = L;
            const float tapGhost = 1.0f, cardFrom = 1.2f, buy = 2.2f, paid = 2.3f;
            const float cut = 2.45f, back = 8.6f;   // the cutscene, over the paused game
            const float opened = back + 0.05f, gateUp = opened + 0.6f, tapFar = 10.4f, walk = 10.6f, there = 13.0f;
            const string bridgeId = "Ginger Cove";   // the store item: the spirit for the bridge to Pebble Cove
            var price = StoreData.Price(bridgeId);
            string island = StoryText.Name(bridgeId);
            var ghostArt = HelpArt.Item("Ghost");
            var ghostTint = new Color(0.85f, 0.95f, 1f, 0.9f);   // see-through and a little blue, as BridgeGhost tints it

            // ---- the island: the south shore of the Heart of the Isles, the channel, Pebble Cove across it
            var world = m.Group("World", Vector2.zero);
            During(m, world, (0f, cut + 0.15f), (back - 0.1f, L));
            const float scale = 20f;
            var middle = LandMap.WorldToUnity(new Vector2(508.5f, 2970f));   // the bridge's middle, where its gate stands
            Vector2 Spot(Vector2 w) => (LandMap.WorldToUnity(w) - middle) * scale;
            var names = new[] { "Heart of the Isles", "Ginger Cove", "second_bridge" };
            var pieces = TripLand(m, world, middle, Vector2.zero, scale, names) ?? TripFallbackLand(m, world, Spot, names);

            // The bridge's land, hidden until it's paid for: it fades in and swells up into place.
            if (pieces.TryGetValue("second_bridge", out var bridge))
                foreach (var layer in bridge.Layers)
                {
                    m.Fade(layer).At(opened, 0f).At(opened + 1.4f, 1f, DemoEase.Out);
                    m.Size(layer).At(opened, 0.92f).At(opened + 1.6f, 1f, DemoEase.Back);
                }

            // The spirit, bobbing gently over the grass near the shore.
            var ghostAt = new Vector2(60f, 92f);
            var ghost = m.Group("Spirit", ghostAt, world);
            var ghostPic = m.Pic("Body", ghostArt, Vector2.zero, 66f, ghost, ghostTint);
            m.Do(s => ghostPic.rectTransform.anchoredPosition = new Vector2(0f, 7f + Mathf.Sin(s * 1.3f) * 3f));
            // Its job done, it floats up and away.
            m.Move(ghost).At(opened, ghostAt).At(opened + 2.2f, ghostAt + new Vector2(0f, 60f), DemoEase.In);
            m.Fade(ghost).At(opened, 1f).At(opened + 2.2f, 0f);

            // The red gate over the bridge's middle, rising out of it with a bounce.
            var toriiArt = Resources.Load<Sprite>("World/Torii_Front");
            Image gate = null;
            if (toriiArt != null)
            {
                gate = m.Pic("Torii", toriiArt, Vector2.zero, 124f, world);   // 5.4 units across the bridge
                var rise = new DemoKeys().At(gateUp, 0f).At(gateUp + 0.9f, 1f, DemoEase.Back);
                var gateRect = gate.rectTransform;
                m.Do(s => gateRect.localScale = new Vector3(1f, Mathf.Max(0f, rise.Eval(s).x), 1f));
            }

            // You, and the walk across once it's up: a tap on Pebble Cove.
            var start = new Vector2(-110f, 84f);
            var far = new Vector2(30f, -150f);
            var (you, youBody) = Walker(m, "You", HelpArt.You, start, 64f, world);
            m.Face(youBody).At(0f, -1f);
            m.Move(you).At(walk, start).At(there, far, DemoEase.Linear);
            m.Bob(youBody, 5f, 12f, s => s > walk && s < there);
            if (gate != null)
            {
                // Behind the gate until you pass under it, in front of it after.
                var gateT = gate.transform;
                m.Do(s =>
                {
                    int gi = gateT.GetSiblingIndex(), yi = you.GetSiblingIndex();
                    bool behind = you.anchoredPosition.y > 0f;
                    if (behind != (yi < gi)) you.SetSiblingIndex(gi);
                });
            }

            // The counters: the spirit's price comes off as you pay.
            const int wood0 = 260, gold0 = 180, gems0 = 12;
            var woodCount = m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "", WoodPill, world);
            var goldCount = m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "", GoldPill, world);
            m.Tally(woodCount, wood0, (paid, wood0 - price.x));
            m.Tally(goldCount, gold0, (paid, gold0 - price.y));
            if (price.z > 0)
            {
                var gemCount = m.Pill("Gems", HelpArt.Resource(ResourceKind.Gem), "", GemPill, world);
                m.Tally(gemCount, gems0, (paid, gems0 - price.z));
            }

            // The bridge twinkles along its length as it comes up.
            foreach (var c in new[] { new Vector2(539.4f, 2670f), new Vector2(514.7f, 2820f), new Vector2(508.5f, 2970f), new Vector2(503f, 3120f), new Vector2(509.6f, 3270f) })
                m.Sparkle(Spot(c), opened + 0.15f, Color.white, 4);

            // The buy panel, as tapping the spirit opens it.
            var purchase = TripBuyCard(m, new Vector2(60f, -12f), "Bridge to " + island, ghostArt, ghostTint, price, cardFrom, paid);

            // ---- the little movie
            TripCutscene(m, cut, back, island, ghostArt, ghostTint);
            var dark = UiKit.Hex("07060A");
            m.FrameFill(s => s >= cut && s < back ? dark : HelpArt.Sea);

            var dip = m.Shape("Dip", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("2E1A0E"));
            m.Fade(dip).At(0f, 1f).At(0.3f, 0f).At(L - 0.35f, 0f).At(L, 1f);

            var ghostTap = ghostAt + new Vector2(0f, 38f);
            m.Hand().Show(0.4f, 2.3f)
                .At(0.4f, ghostTap + new Vector2(120f, -90f)).At(tapGhost - 0.2f, ghostTap).Tap(tapGhost)
                .At(buy - 0.35f, purchase).Tap(buy).At(2.3f, purchase + new Vector2(70f, -50f));
            m.Hand().Show(tapFar - 0.45f, walk + 0.4f)
                .At(tapFar - 0.45f, far + new Vector2(120f, 70f)).At(tapFar - 0.15f, far).Tap(tapFar)
                .At(walk + 0.4f, far + new Vector2(110f, 60f));
        }

        /// <summary>
        /// BridgeCutscene, shortened: the paused game goes dark and the scene floats in, in its
        /// bubble of glass, with the words beside it. On the shore, you and a friend; over the
        /// sea, the spirit. It waves its paws (sparkles)… a grey puff of nothing. It tries again,
        /// shaking with the effort, and the bridge bursts up out of the water with a splash that
        /// soaks you both. "A bridge to …!", a bow, the spirit fades, and the bubble pops.
        /// </summary>
        static void TripCutscene(HelpMovie m, float from, float to, string island, Sprite ghostArt, Color ghostTint)
        {
            float wave = from + 0.9f, puff = from + 1.7f, shake = from + 2.6f, rise = from + 3.4f;
            float titleAt = from + 3.95f, bow = from + 4.55f, pop = to - 0.5f;

            var scene = m.Group("Cutscene", Vector2.zero);
            During(m, scene, (from, to));
            m.Fade(scene).At(from, 0f).At(from + 0.12f, 1f).At(to - 0.15f, 1f).At(to, 0f);
            m.Shape("Dark", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("07060A"), scene);

            // Glimmers drifting up in the dark around the bubble.
            for (int i = 0; i < 7; i++)
            {
                int k = i;
                bool warm = k % 3 == 0;
                var mote = m.Shape("Mote", IconPainter.Glow, Vector2.zero, Vector2.one * (18f + k % 3 * 8f),
                    warm ? new Color(1f, 0.82f, 0.55f, 0f) : new Color(0.7f, 1f, 0.95f, 0f), scene);
                float x0 = -270f + k * 88f, phase = k * 0.37f;
                m.Do(s =>
                {
                    float u = Mathf.Repeat(s * 0.16f + phase, 1f);
                    mote.rectTransform.anchoredPosition = new Vector2(x0 + Mathf.Sin(u * 6f + k) * 14f, -210f + u * 420f);
                    var col = mote.color;
                    col.a = Mathf.Sin(u * Mathf.PI) * 0.6f;
                    mote.color = col;
                });
            }

            // The bubble: the painting seen through a round window, under a skin of glass.
            const float d = 300f;
            var bubbleAt = new Vector2(-120f, 0f);
            var bubble = m.Group("Bubble", bubbleAt, scene);
            m.Move(bubble).At(from, bubbleAt + new Vector2(0f, d * 0.2f)).At(from + 1.4f, bubbleAt, DemoEase.Out);
            m.Size(bubble).At(from, 0.25f).At(from + 1.4f, 1f, DemoEase.Out).At(pop, 1f).At(pop + 0.12f, 1.08f, DemoEase.Out);
            m.Fade(bubble).At(from, 0f).At(from + 0.6f, 1f).At(pop + 0.1f, 1f).At(pop + 0.25f, 0f);
            var window = m.Shape("Window", UiKit.Circle, Vector2.zero, new Vector2(d, d), Color.white, bubble);
            window.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var inside = window.transform;

            // The painting, aimed as the movie aims it (a little right of and below its middle, a touch closer).
            var paint = new Vector2(1.5f * d * 1.1f, d * 1.1f);
            var aim = new Vector2(-10f, 15f);
            Vector2 P(float fx, float fy) => aim + new Vector2((fx - 0.5f) * paint.x, (fy - 0.5f) * paint.y);
            var shore = HelpArt.Picture("Cutscenes/Cutscene_Shore");
            if (shore != null) m.Shape("Shore", shore, aim, paint, Color.white, inside);
            else
            {
                m.Shape("Sea", null, aim, paint, UiKit.Hex("6FB7C9"), inside);
                m.Shape("Sky", null, P(0.5f, 0.725f), new Vector2(paint.x, paint.y * 0.55f), UiKit.Hex("BFE3F2"), inside);
            }

            // The land bridge, waiting under the water: drawn from the top down, so it rises into view.
            var archArt = HelpArt.Picture("Cutscenes/Cutscene_Bridge");
            if (archArt != null)
            {
                float h = 0.2f * paint.y, w = h * archArt.rect.width / archArt.rect.height;
                var archAt = P(0.6f, 0.36f);
                var arch = m.Shape("Bridge", archArt, archAt, new Vector2(w, h), Color.white, inside);
                arch.type = Image.Type.Filled;
                arch.fillMethod = Image.FillMethod.Vertical;
                arch.fillOrigin = (int)Image.OriginVertical.Top;
                var fill = new DemoKeys().At(rise, 0f).At(rise + 1.1f, 1f, DemoEase.Back);
                m.Do(s => arch.fillAmount = Mathf.Clamp01(fill.Eval(s).x));
                m.Move(arch).At(rise, archAt - new Vector2(0f, h * 0.5f)).At(rise + 1.1f, archAt, DemoEase.Back);
            }

            // On the shore, you and a friend facing the sea; the spirit over the water.
            float body = paint.y * 1.15f;
            var mate = m.Pic("Mate", HelpArt.Worker, P(0.36f, 0.13f), 0.22f * body, inside);
            m.Face(mate).At(0f, -1f);
            var you = m.Pic("You", HelpArt.You, P(0.46f, 0.09f), 0.26f * body, inside);
            m.Face(you).At(0f, -1f);
            var ghostHolder = m.Group("Spirit", P(0.42f, 0.3f), inside);
            var ghost = m.Pic("Body", ghostArt, Vector2.zero, 0.22f * body, ghostHolder, ghostTint);
            m.Do(s => ghost.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Sin(s * 2.4f) * 4f));
            m.Fade(ghost).At(from + 0.15f, 0f).At(from + 0.95f, 0.9f).At(bow + 0.3f, 0.9f).At(bow + 0.8f, 0f);

            // Abracadabra (a lean each way, sparkles)…
            var lean = m.Turn(ghost).At(wave, 0f).At(wave + 0.25f, 14f).At(wave + 0.3f, 14f).At(wave + 0.55f, -14f).At(wave + 0.75f, 0f);
            var magicAt = P(0.42f, 0.45f);
            TripBurst(m, inside, magicAt, IconPainter.Sparkle, UiKit.Hex("FFE9A8"), 8, 30f, 14f, wave + 0.55f);
            // …nothing: a sad grey puff, and the two of you lean in to look.
            TripBurst(m, inside, P(0.52f, 0.39f), UiKit.Circle, new Color(0.6f, 0.62f, 0.68f, 0.9f), 6, 22f, 18f, puff, 1f, new Vector2(0f, 1f));
            m.Turn(you).At(puff, 0f).At(puff + 0.3f, -10f).At(puff + 0.95f, -10f).At(puff + 1.15f, 0f);
            m.Turn(mate).At(puff, 0f).At(puff + 0.3f, -12f).At(puff + 0.95f, -12f).At(puff + 1.15f, 0f);
            // Again, much harder: it shakes, a star over its head, a big burst of sparkles.
            for (int j = 0; j < 8; j++) lean.At(shake + j * 0.1f, 0f).At(shake + j * 0.1f + 0.05f, j % 2 == 0 ? 9f : -9f);
            lean.At(shake + 0.85f, 0f);
            var star = m.Group("Star", P(0.42f, 0.3f) + new Vector2(30f, 0.22f * body + 10f), inside);
            m.Shape("Disc", UiKit.Circle, Vector2.zero, new Vector2(34f, 34f), new Color(1f, 1f, 1f, 0.92f), star);
            m.Shape("Icon", IconPainter.Star, Vector2.zero, new Vector2(22f, 22f), UiKit.Hex("E3A92B"), star).preserveAspect = true;
            m.Size(star).At(shake - 0.01f, 0f).At(shake + 0.25f, 1f, DemoEase.Back).At(shake + 0.8f, 1f).At(shake + 0.95f, 0f);
            TripBurst(m, inside, magicAt, IconPainter.Sparkle, UiKit.Hex("FFE9A8"), 16, 60f, 14f, shake + 0.05f);

            // Up it comes, and everyone gets soaked.
            for (int i = 0; i < 4; i++)
                TripBurst(m, inside, P(0.15f + i * 0.23f, 0.36f), UiKit.Circle, UiKit.Hex("BFEAF0"), 7, 40f, 9f, rise + i * 0.12f, 0.7f, new Vector2(0f, 0.8f));
            TripBurst(m, inside, P(0.3f, 0.35f), UiKit.Circle, UiKit.Hex("A9DCEB"), 14, 60f, 9f, rise + 0.5f, 0.8f, new Vector2(-0.4f, -0.6f));
            var soaked = new Color(0.7f, 0.82f, 1f);
            var wet = new DemoKeys().At(rise + 0.85f, 0f).At(rise + 1.0f, 1f).At(rise + 1.6f, 1f).At(rise + 2.1f, 0f);
            m.Do(s =>
            {
                var c = Color.Lerp(Color.white, soaked, wet.Eval(s).x);
                you.color = c;
                mate.color = c;
            });

            // A bow, and the spirit's job is done.
            lean.At(bow, 0f).At(bow + 0.25f, 20f).At(bow + 0.5f, 20f).At(bow + 0.75f, 0f);
            TripBurst(m, inside, P(0.42f, 0.4f), IconPainter.Sparkle, Color.white, 10, 40f, 12f, bow + 0.3f, 1f);

            m.Shape("Glass", IconPainter.Ring, Vector2.zero, new Vector2(d * 1.03f, d * 1.03f), new Color(0.85f, 0.98f, 0.95f, 0.85f), bubble);
            m.Shape("Shine", UiKit.Circle, new Vector2(-0.52f, 0.5f) * d * 0.5f, new Vector2(12f, 12f), new Color(1f, 1f, 1f, 0.85f), bubble);

            // The words beside the bubble.
            var title = m.Label("Title", "A bridge to " + island + "!", new Vector2(176f, 20f), 26f, Color.white, scene);
            title.textWrappingMode = TextWrappingModes.Normal;
            title.rectTransform.sizeDelta = new Vector2(230f, 130f);
            title.outlineWidth = 0.22f;
            title.outlineColor = new Color32(120, 60, 70, 255);
            m.Size(title).At(titleAt, 0.6f).At(titleAt + 0.5f, 1f, DemoEase.Back);
            m.Fade(title).At(titleAt, 0f).At(titleAt + 0.35f, 1f).At(pop, 1f).At(pop + 0.3f, 0f);

            // Pop: droplets fly off the rim.
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var drop = m.Shape("Drop", UiKit.Circle, bubbleAt, Vector2.one * (10f + i % 4 * 5f), new Color(0.82f, 0.97f, 1f, 0.9f), scene);
                var rim = bubbleAt + dir * d * 0.52f;
                m.Move(drop).At(pop + 0.1f, rim).At(pop + 0.7f, rim + dir * (60f + i % 5 * 25f), DemoEase.Out);
                m.Fade(drop).At(pop + 0.09f, 0f).At(pop + 0.1f, 0.9f, DemoEase.Step).At(pop + 0.7f, 0f, DemoEase.In);
            }
        }

        // ---------------------------------------------------------------- travel helpers

        /// <summary>A piece of land as a movie draws it: its layers (shore, sand, grass), to fade or swell together.</summary>
        sealed class TripPiece
        {
            public string Name;
            public bool IsBridge;
            public readonly List<Image> Layers = new();
        }

        /// <summary>
        /// The named pieces of land, drawn with LandMap's own layers (every shore first, then every
        /// sand, then every grass, as they sort in the world) under <paramref name="parent"/>:
        /// the Unity point <paramref name="centre"/> lands on <paramref name="at"/>, at
        /// <paramref name="scale"/> movie pixels per Unity unit. Null when they aren't all there
        /// (a friend's island, or no island loaded), so the caller can paint its own.
        /// </summary>
        static Dictionary<string, TripPiece> TripLand(HelpMovie m, Transform parent, Vector2 centre, Vector2 at, float scale, ICollection<string> names)
        {
            var land = LandMap.Instance;
            if (land == null) return null;
            var found = new Dictionary<string, TripPiece>();
            var layers = new List<(int order, int index, SpriteRenderer renderer, TripPiece piece)>();
            foreach (var p in land.Pieces)
            {
                if (p == null || !names.Contains(p.name) || found.ContainsKey(p.name)) continue;
                var entry = new TripPiece { Name = p.name, IsBridge = p.isBridge };
                found[p.name] = entry;
                foreach (var sr in p.layers)
                    if (sr != null && sr.sprite != null) layers.Add((sr.sortingOrder, layers.Count, sr, entry));
            }
            foreach (var name in names) if (!found.ContainsKey(name)) return null;
            if (layers.Count == 0) return null;
            layers.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : a.index.CompareTo(b.index));

            foreach (var (_, _, sr, owner) in layers)
            {
                // The sprite's whole rectangle, as an Image draws it, wherever its pivot is.
                var sprite = sr.sprite;
                float ppu = Mathf.Max(0.01f, sprite.pixelsPerUnit);
                var local = (sprite.rect.size * 0.5f - sprite.pivot) / ppu;
                Vector2 mid = sr.transform.TransformPoint(local);
                var lossy = sr.transform.lossyScale;
                var size = new Vector2(sprite.rect.width / ppu * Mathf.Abs(lossy.x), sprite.rect.height / ppu * Mathf.Abs(lossy.y));
                var colour = sr.color;
                colour.a = 1f;
                var img = m.Shape(owner.Name, sprite, at + (mid - centre) * scale, size * scale, colour, parent);
                img.raycastTarget = false;
                owner.Layers.Add(img);
            }
            return found;
        }

        /// <summary>Where each piece of the home archipelago lies, in world units (y down), for the painted stand-in.</summary>
        static readonly Dictionary<string, Rect> TripBounds = new()
        {
            { "Capy Island", Rect.MinMaxRect(115f, 177f, 1116f, 1297f) },
            { "Heart of the Isles", Rect.MinMaxRect(203f, 1649f, 1103f, 2826f) },
            { "Ginger Cove", Rect.MinMaxRect(170f, 3084f, 2184f, 4277f) },
            { "Verdant Bay", Rect.MinMaxRect(2241f, 3003f, 4407f, 4409f) },
            { "Wedding Isle", Rect.MinMaxRect(3434f, 224f, 4397f, 2886f) },
            { "Pirate Cove", Rect.MinMaxRect(1300f, 40f, 3290f, 1300f) },
            { "Monster Isle", Rect.MinMaxRect(2272f, 1579f, 3194f, 2857f) },
            { "first_bridge", Rect.MinMaxRect(480f, 1150f, 740f, 1820f) },
            { "second_bridge", Rect.MinMaxRect(400f, 2600f, 640f, 3340f) },
        };

        /// <summary>
        /// When the real land art isn't to hand: each island a sand-rimmed oval, each bridge a strip
        /// of sand and grass laid over the shores it joins. <paramref name="spot"/> turns a world
        /// point into a movie position.
        /// </summary>
        static Dictionary<string, TripPiece> TripFallbackLand(HelpMovie m, Transform parent, Func<Vector2, Vector2> spot, ICollection<string> names)
        {
            var made = new Dictionary<string, TripPiece>();
            (Vector2 at, Vector2 size) Box(Rect r)
            {
                var a = spot(new Vector2(r.xMin, r.yMin));
                var b = spot(new Vector2(r.xMax, r.yMax));
                return ((a + b) * 0.5f, new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y)));
            }
            foreach (bool bridges in new[] { false, true })
                foreach (var name in names)
                {
                    if (!TripBounds.TryGetValue(name, out var bounds)) continue;
                    bool isBridge = !name.Contains(" ") && name.Contains("_");
                    if (isBridge != bridges) continue;
                    var (at, size) = Box(bounds);
                    var piece = new TripPiece { Name = name, IsBridge = isBridge };
                    made[name] = piece;
                    if (!isBridge)
                    {
                        float rim = Mathf.Min(size.x, size.y) * 0.12f;
                        piece.Layers.Add(m.Shape(name, UiKit.Circle, at, size, HelpArt.Sand, parent));
                        piece.Layers.Add(m.Shape(name, UiKit.Circle, at, size - new Vector2(rim, rim), HelpArt.Grass, parent));
                    }
                    else
                    {
                        var sand = m.Shape(name, UiKit.Rounded(28), at, size, HelpArt.Sand, parent);
                        sand.type = Image.Type.Sliced;
                        var grass = m.Shape(name, UiKit.Rounded(28), at, new Vector2(size.x * 0.82f, size.y), HelpArt.Grass, parent);
                        grass.type = Image.Type.Sliced;
                        piece.Layers.Add(sand);
                        piece.Layers.Add(grass);
                    }
                }
            return made;
        }

        /// <summary>A point a fraction <paramref name="u"/> of the way along a path of points, by distance.</summary>
        static Vector2 TripAlong(Vector2[] path, float u)
        {
            if (path.Length == 1) return path[0];
            float total = 0f;
            for (int i = 1; i < path.Length; i++) total += Vector2.Distance(path[i - 1], path[i]);
            float want = Mathf.Clamp01(u) * total;
            for (int i = 1; i < path.Length; i++)
            {
                float leg = Vector2.Distance(path[i - 1], path[i]);
                if (want <= leg || i == path.Length - 1) return Vector2.Lerp(path[i - 1], path[i], leg > 0f ? Mathf.Clamp01(want / leg) : 1f);
                want -= leg;
            }
            return path[path.Length - 1];
        }

        /// <summary>One of the round buttons in the HUD's right-hand column, with its caption in a dark pill under it.</summary>
        static RectTransform TripHudButton(HelpMovie m, string caption, Sprite icon, Color fill, Vector2 at, float size = 56f)
        {
            var root = m.Group(caption, at);
            var disc = m.RoundButton("Disc", icon, fill, Vector2.zero, size, root);
            var pill = m.Shape("CaptionPill", UiKit.Rounded(18), new Vector2(0f, -size * 0.5f - 12f), new Vector2(size + 4f, 22f), UiKit.HudGlass, root);
            pill.type = Image.Type.Sliced;
            Fit(m.Label("Caption", caption, Vector2.zero, 14f, Color.white, pill.transform), caption, size);
            return disc;
        }

        /// <summary>Your marker on the map: your face in an orange ring, and "You" on a tag under it.</summary>
        static RectTransform TripPin(HelpMovie m, Transform parent, Sprite face, Vector2 at)
        {
            var pin = m.Group("You", at, parent);
            m.Shape("Ring", UiKit.Circle, Vector2.zero, new Vector2(50f, 50f), UiKit.Primary, pin);
            var disc = m.Shape("Face", UiKit.Circle, Vector2.zero, new Vector2(42f, 42f), Color.white, pin);
            m.Shape("Icon", face, Vector2.zero, new Vector2(36f, 36f), Color.white, disc.transform).preserveAspect = true;
            var tag = m.Shape("Tag", UiKit.Rounded(18), new Vector2(0f, -31f), new Vector2(48f, 20f), UiKit.Primary, pin);
            tag.type = Image.Type.Sliced;
            Fit(m.Label("Label", "You", Vector2.zero, 13f, Color.white, tag.transform), "You", 44f);
            return pin;
        }

        /// <summary>The dark disc with a padlock the map puts on an island you can't reach yet.</summary>
        static void TripLock(HelpMovie m, Transform parent, Vector2 at, float size)
        {
            var badge = m.Shape("Lock", UiKit.Circle, at, new Vector2(size, size), new Color(0.15f, 0.12f, 0.1f, 0.75f), parent);
            m.Shape("Icon", IconPainter.Lock, Vector2.zero, new Vector2(size * 0.52f, size * 0.52f), Color.white, badge.transform).preserveAspect = true;
        }

        /// <summary>An island's name in a dark pill, as the map writes them.</summary>
        static void TripName(HelpMovie m, Transform parent, string name, Vector2 at, float size, Color back)
        {
            var pill = m.Shape("Name", UiKit.Rounded(18), at, new Vector2(10f, size * 1.6f), back, parent);
            pill.type = Image.Type.Sliced;
            var t = m.Label("Label", name, Vector2.zero, size, Color.white, pill.transform);
            float width = t.GetPreferredValues(Loc.T(name)).x;
            pill.rectTransform.sizeDelta = new Vector2(width + size * 1.1f, size * 1.6f);
        }

        /// <summary>The map's line across its top when you drop yourself somewhere you can't go: shown, held, faded.</summary>
        static void TripToast(HelpMovie m, Transform parent, string line, Vector2 at, float from)
        {
            var pill = m.Shape("Toast", UiKit.Rounded(24), at, new Vector2(420f, 36f), new Color(0.15f, 0.12f, 0.1f, 0.85f), parent);
            pill.type = Image.Type.Sliced;
            Fit(m.Label("Text", line, Vector2.zero, 15f, Color.white, pill.transform), line, 400f);
            m.Fade(pill.rectTransform).At(from - 0.01f, 0f).At(from, 1f, DemoEase.Step).At(from + 2.2f, 1f).At(from + 2.7f, 0f);
        }

        /// <summary>A white puff where you turn up after travelling, with little clouds flying off it.</summary>
        static void TripPoof(HelpMovie m, Transform parent, Vector2 at, float when)
        {
            var glow = m.Shape("Poof", IconPainter.Glow, at, new Vector2(130f, 130f), Color.white, parent);
            m.Size(glow).At(when, 0.3f).At(when + 0.5f, 1.5f, DemoEase.Out);
            m.Fade(glow).At(when - 0.01f, 0f).At(when, 0.95f, DemoEase.Step).At(when + 0.55f, 0f);
            TripBurst(m, parent, at, UiKit.Circle, new Color(1f, 1f, 1f, 0.9f), 6, 50f, 34f, when, 0.5f);
        }

        /// <summary>
        /// Bits (sparkles, drops, puffs) flying out from a point at a moment and fading,
        /// <paramref name="drift"/> pushing them one way (up for a splash).
        /// </summary>
        static void TripBurst(HelpMovie m, Transform parent, Vector2 at, Sprite bit, Color colour, int count, float reach, float size,
            float when, float seconds = 0.7f, Vector2 drift = default)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (i + 0.5f) / count * Mathf.PI * 2f + (i % 2) * 0.3f;
                float away = reach * (0.6f + 0.1f * (i * 7 % 5));
                var end = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * away + drift * reach;
                var piece = m.Shape("Bit", bit, at, new Vector2(size, size), colour, parent);
                piece.preserveAspect = true;
                m.Move(piece).At(when, at).At(when + seconds, end, DemoEase.Out);
                m.Fade(piece).At(when - 0.01f, 0f).At(when, colour.a, DemoEase.Step).At(when + seconds, 0f, DemoEase.In);
            }
        }

        /// <summary>
        /// The buy panel a spirit opens (PurchaseDialog): its title, the spirit, the three prices
        /// down the right (wood, gold, gems), Cancel and Purchase. Returns where Purchase is.
        /// </summary>
        static Vector2 TripBuyCard(HelpMovie m, Vector2 at, string title, Sprite art, Color tint, Vector3Int price, float from, float to)
        {
            var card = Panel(m, at, new Vector2(340f, 220f), null, from, to);
            Fit(m.Label("Title", title, new Vector2(0f, 82f), 24f, UiKit.Ink, card), title, 310f);
            m.Pic("Item", art, new Vector2(-90f, -52f), 100f, card, tint);
            int[] amounts = { price.x, price.y, price.z };
            ResourceKind[] kinds = { ResourceKind.Wood, ResourceKind.Gold, ResourceKind.Gem };
            for (int i = 0; i < 3; i++)
            {
                float y = 42f - i * 36f;
                m.Label("Price", amounts[i].ToString(), new Vector2(88f, y), 24f, UiKit.Ink, card);
                m.Shape("Icon", HelpArt.Resource(kinds[i]), new Vector2(130f, y), new Vector2(30f, 30f), Color.white, card).preserveAspect = true;
            }
            var buyAt = new Vector2(80f, -82f);
            WordButton(m, card, new Vector2(-80f, -82f), new Vector2(140f, 42f), UiKit.SurfaceSunk, "Cancel", UiKit.Ink);
            WordButton(m, card, buyAt, new Vector2(140f, 42f), UiKit.Primary, "Purchase", Color.white);
            return at + buyAt;
        }
    }
}
