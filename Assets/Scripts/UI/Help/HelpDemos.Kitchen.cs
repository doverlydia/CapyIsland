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
        // ---------------------------------------------------------------- gardening

        /// <summary>
        /// Gardening from the start: pay the spirit at the south end of Pebble Cove to raise the
        /// bridge to Sunflower Farm, and walk across (<see cref="KitchenFarmBridge"/>). Then, as the
        /// farm plays it: the barn on Sunflower Farm has its seed packets set out
        /// in front of it (the ones above your gardening level greyed). Tap the carrot seeds, tap
        /// Purchase: the gold comes off and the packet pops into your trail. Drag it onto an empty
        /// plot (the field's first three are open at level 1; the rest are padlocked): it plants
        /// itself, and a sprout comes up and grows while the clock runs down its two minutes. When
        /// it's ready the carrot bobs in the soil; tap the plot and the harvest (three carrots)
        /// goes into your bag, which you open from Hobbies.
        /// </summary>
        static void GardenMovie(HelpMovie m)
        {
            // First the bridge to the farm (KitchenFarmBridge, over everything until T), then the farm.
            const float T = 6.2f;
            const float L = T + 14.6f;
            m.Length = L;
            var crop = HobbyData.Crop("Carrot");
            if (crop == null) crop = HobbyData.Crops[0];
            string seeds = StoreData.SeedName(crop);
            var packet = KitchenOr(HelpArt.Item(seeds), HobbyData.Art("Seeds_" + crop.Id));
            var cropArt = HobbyData.Art("Crop_" + crop.Id);
            var price = StoreData.Price(seeds);

            // On the farm: buying, planting, growing, picking, and a look in the bag.
            const float tap = T + 0.95f, cardFrom = T + 1.15f, buy = T + 1.85f, bought = T + 2.0f, grab = T + 2.9f, drop = T + 3.9f;
            const float grow = T + 4.25f, ready = T + 7.6f, harvest = T + 8.45f;
            const float hubTap = T + 10.6f, hubOpen = T + 10.8f, bagTap = T + 11.8f, bagOpen = T + 12.0f;

            m.Island("Farm", new Vector2(0f, -35f), new Vector2(570f, 270f));
            var scarecrow = HobbyData.Art("Farm_Scarecrow");
            if (scarecrow != null) m.Pic("Scarecrow", scarecrow, new Vector2(-20f, 20f), 84f);

            // The field: soil beds in rows. At gardening level 1 the first three are open; the
            // rest wait behind a padlock until you grow into them (Gardening.MaxPlots).
            var plotArt = HobbyData.Art("Plot");
            float[] cols = { -240f, -150f, -60f };
            float[] rows = { -20f, -92f };
            var target = new Vector2(cols[1], rows[0]);   // the middle bed of the open row
            Image targetBed = null;
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < cols.Length; c++)
                {
                    var centre = new Vector2(cols[c], rows[r]);
                    bool locked = r > 0;
                    // A bed is a flat picture: its middle sits 0.4 of the box above the spot it stands on.
                    var bed = m.Pic("Plot", plotArt, centre - new Vector2(0f, 28.8f), 72f, null,
                        locked ? new Color(0.72f, 0.68f, 0.64f) : Color.white);
                    if (locked)
                        m.Shape("Lock", IconPainter.Lock, centre + new Vector2(0f, 4f), new Vector2(26f, 26f), new Color(1f, 0.97f, 0.9f, 0.95f)).preserveAspect = true;
                    if (r == 0 && c == 1) targetBed = bed;
                }
            }

            // The barn, the farm's shop, its seed packets in a half circle in front of it, east
            // round to west, as a store lays out its wares.
            var barn = new Vector2(168f, -15f);
            m.Pic("Barn", HobbyData.Art("Farm_Barn"), barn, 132f);
            var names = StoreData.Lists.TryGetValue("farm_store", out var list) ? list : new[] { seeds };
            var buyAt = barn + new Vector2(100f, 0f);
            for (int i = 0; i < names.Length; i++)
            {
                float a = (names.Length > 1 ? i * 180f / (names.Length - 1) : 90f) * Mathf.Deg2Rad;
                var at = barn + new Vector2(Mathf.Cos(a) * 100f, -Mathf.Sin(a) * 62f);
                var kind = StoreData.SeedCrop(names[i]);
                var art = KitchenOr(HelpArt.Item(names[i]), kind != null ? HobbyData.Art("Seeds_" + kind.Id) : null);
                var ware = Ware(m, art, at, 38f, i * 1.3f);
                // Above your gardening level (1 here): on show, but greyed.
                if (kind != null && kind.MinLevel > 1) ware.color = new Color(0.55f, 0.55f, 0.55f, 1f);
                if (names[i] == seeds) buyAt = at;
            }

            // What grows: a sprout that swells as the minutes pass, then the carrot itself,
            // bobbing in the soil when it's ready (Gardening.PlotView.Refresh).
            var feet = target + new Vector2(0f, -8f);
            var sprout = m.Pic("Sprout", HobbyData.Art("Sprout"), feet, 80f);
            m.Size(sprout.transform).At(drop - 0.01f, 0f).At(drop, 0.1f, DemoEase.Step).At(drop + 0.3f, 0.35f, DemoEase.Back)
                .At(grow, 0.35f).At(ready, 0.75f, DemoEase.Linear);
            m.Between(sprout.gameObject, drop, ready);
            var carrot = m.Pic("Carrot", cropArt, feet, 80f);
            m.Size(carrot.transform).At(ready, 0.3f).At(ready + 0.3f, 0.7f, DemoEase.Back);
            m.Do(s => carrot.rectTransform.anchoredPosition = feet + new Vector2(0f, 4f + (0.5f - 0.5f * Mathf.Cos((s - ready) * Mathf.PI / 0.6f)) * 9f));
            m.Between(carrot.gameObject, ready, harvest);
            m.Sparkle(feet + new Vector2(0f, 40f), ready, UiKit.Sun, 5);
            if (targetBed != null)
                m.Size(targetBed.transform).At(drop, 1f).At(drop + 0.1f, 1.08f).At(drop + 0.3f, 1f)
                    .At(harvest, 1f).At(harvest + 0.1f, 1.05f).At(harvest + 0.3f, 1f);
            m.Sparkle(target + new Vector2(0f, 10f), drop, UiKit.Leaf, 5);

            // Time passing: the minutes left until it's ready, counting down while the sprout grows.
            KitchenCountdown(m, target + new Vector2(0f, 76f), grow, ready, crop.Minutes);

            // You, facing the barn, and the packet you bought riding in your trail behind you.
            var (_, youBody) = Walker(m, "You", HelpArt.You, new Vector2(35f, -150f), 74f);
            m.Face(youBody).At(0f, -1f);
            var trail = new Vector2(-25f, -162f);
            var item = m.Group("Bought", trail);
            var itemPic = m.Pic("Pic", packet, Vector2.zero, 46f, item);
            var room = PlaceRing(m, item, true, 96f);
            m.Fade(room).At(grab, 0f).At(grab + 0.15f, 0.45f).At(drop, 0.45f).At(drop + 0.1f, 0f);
            m.Move(item).At(grab, trail).At(drop, feet, DemoEase.Linear);
            m.Size(itemPic.transform).At(bought, 0f).At(bought + 0.3f, 1f, DemoEase.Back);
            m.Fade(item).At(bought - 0.01f, 0f).At(bought, 1f, DemoEase.Step).At(drop - 0.01f, 1f).At(drop, 0f, DemoEase.Step);
            m.Sparkle(trail + new Vector2(0f, 24f), bought, UiKit.Sun, 5);

            // The counters, and Hobbies, where your bag is.
            m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "120", WoodPill);
            var goldCount = m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "", GoldPill);
            const int gold0 = 60;
            m.Tally(goldCount, gold0, (bought, gold0 - price.y));
            var hobbiesAt = new Vector2(245f, 150f);
            var hobbies = m.RoundButton("Hobbies", IconPainter.Rod, KitchenHobbiesTeal, hobbiesAt, 70f);

            var purchase = BuyCard(m, new Vector2(-20f, 40f), seeds, packet, cardFrom, bought);

            // Picked: the carrots go to your bag.
            m.Sparkle(feet + new Vector2(0f, 30f), harvest + 0.02f, UiKit.Leaf, 7);
            float land = m.Ping(cropArt, "+" + crop.Yield, feet + new Vector2(0f, 46f), hobbiesAt, harvest + 0.1f);
            m.Sparkle(hobbiesAt, land, UiKit.Sun, 5);
            m.Size(hobbies).At(land, 1f).At(land + 0.12f, 1.2f).At(land + 0.28f, 1f)
                .At(hubTap - 0.1f, 1f).At(hubTap, 0.9f).At(hubTap + 0.14f, 1f);

            // Hobbies, then its Bag: the carrots are there.
            var bagButton = KitchenBagPages(m, hubOpen, bagOpen, L - 0.45f, cropArt, crop.Yield);

            // ---- before all that: paying the spirit for the bridge, and walking across
            KitchenFarmBridge(m, T, L);

            m.Hand().Show(T + 0.35f, T + 2.2f)
                .At(T + 0.35f, buyAt + new Vector2(-120f, 110f)).At(T + 0.8f, buyAt + new Vector2(0f, 18f)).Tap(tap)
                .At(T + 1.65f, purchase).Tap(buy).At(T + 2.2f, purchase + new Vector2(70f, -60f));
            m.Hand().Show(T + 2.7f, T + 4.35f)
                .At(T + 2.7f, trail + new Vector2(90f, 90f)).At(T + 2.85f, trail + new Vector2(0f, 20f)).Press(grab).At(grab, trail + new Vector2(0f, 20f))
                .At(drop, feet + new Vector2(0f, 20f), DemoEase.Linear).Lift(drop + 0.05f).At(T + 4.35f, feet + new Vector2(110f, 110f));
            m.Hand().Show(harvest - 0.55f, harvest + 0.5f)
                .At(harvest - 0.55f, target + new Vector2(120f, -90f)).At(harvest - 0.15f, target + new Vector2(0f, 6f)).Tap(harvest)
                .At(harvest + 0.5f, target + new Vector2(110f, -90f));
            m.Hand().Show(land + 0.15f, bagTap + 0.5f)
                .At(land + 0.15f, hobbiesAt + new Vector2(-110f, -100f)).At(hubTap - 0.25f, hobbiesAt).Tap(hubTap)
                .At(bagTap - 0.3f, bagButton).Tap(bagTap).At(bagTap + 0.5f, bagButton + new Vector2(130f, -80f));
        }

        static readonly Color KitchenHobbiesTeal = UiKit.Hex("3E9C8F");

        /// <summary>
        /// Taking up gardening (Farm, BridgeGhost): at the south end of Pebble Cove a spirit waits
        /// by the water, with Sunflower Farm across the channel. Tap it: the buy panel says "Bridge
        /// to Sunflower Farm" and its price (6000 wood, 6000 gold); Purchase, and the counters
        /// drop. The bridge's land swells up out of the sea with a twinkle, the red gate rises over
        /// its middle, and the spirit floats away. Tap the farm and you walk across, under the
        /// gate. All of it over the farm scene, until <paramref name="until"/>. (The game also plays
        /// its bridge movie and the gardening movie here; they're left out to keep it short.)
        /// </summary>
        static void KitchenFarmBridge(HelpMovie m, float until, float length)
        {
            const float tapGhost = 0.95f, cardFrom = 1.15f, buy = 1.85f, paid = 2.0f;
            const float opened = 2.2f, gateUp = 2.7f, tapFar = 3.95f, walk = 4.15f, there = 5.75f;
            const string ghostId = "Farm Isle";   // the store item: the spirit for the bridge to Sunflower Farm
            var price = StoreData.Price(ghostId);
            string island = StoryText.Name(ghostId);
            var ghostArt = HelpArt.Item("Ghost");
            var ghostTint = new Color(0.85f, 0.95f, 1f, 0.9f);   // see-through and a little blue, as BridgeGhost tints it

            // ---- Pebble Cove's south shore along the top, the channel, the farm along the bottom
            // The scene sits a little low, so you and the spirit on the near shore stay clear of the counters.
            var down = new Vector2(0f, -45f);
            var world = m.Group("FarmBridge", down);
            During(m, world, (0f, until));
            m.Shape("Sea", null, Vector2.zero, new Vector2(640f, 440f), HelpArt.Sea, world);
            const float scale = 20f;
            var middle = LandMap.WorldToUnity(new Vector2(1125f, 4480f));   // the farm bridge's middle (IslandPainter)
            Vector2 Spot(Vector2 w) => (LandMap.WorldToUnity(w) - middle) * scale;
            var names = new[] { "Ginger Cove", "Farm Isle", Farm.BridgePiece };
            var pieces = TripLand(m, world, middle, Vector2.zero, scale, names) ?? KitchenFarmLand(m, world, Spot);

            // The bridge's land, hidden until it's paid for: it fades in and swells up into place.
            if (pieces.TryGetValue(Farm.BridgePiece, out var bridge))
                foreach (var layer in bridge.Layers)
                {
                    m.Fade(layer).At(opened, 0f).At(opened + 1.2f, 1f, DemoEase.Out);
                    m.Size(layer).At(opened, 0.92f).At(opened + 1.4f, 1f, DemoEase.Back);
                }
            for (int i = -2; i <= 2; i++) m.Sparkle(down + new Vector2(0f, i * 55f), opened + 0.15f + (i + 2) * 0.05f, Color.white, 4);

            // The spirit, bobbing at the near end of the bridge to come; paid, it floats up and away.
            var ghostAt = new Vector2(0f, 112f);
            var ghost = m.Group("Spirit", ghostAt, world);
            var ghostPic = m.Pic("Body", ghostArt, Vector2.zero, 66f, ghost, ghostTint);
            m.Do(s => ghostPic.rectTransform.anchoredPosition = new Vector2(0f, 7f + Mathf.Sin(s * 1.3f) * 3f));
            m.Move(ghost).At(opened, ghostAt).At(opened + 1.8f, ghostAt + new Vector2(0f, 60f), DemoEase.In);
            m.Fade(ghost).At(opened, 1f).At(opened + 1.8f, 0f);

            // The red gate over the bridge's middle, rising out of it with a bounce.
            var toriiArt = Resources.Load<Sprite>("World/Torii_Front");
            Image gate = null;
            if (toriiArt != null)
            {
                gate = m.Pic("Torii", toriiArt, Vector2.zero, 124f, world);
                var rise = new DemoKeys().At(gateUp, 0f).At(gateUp + 0.9f, 1f, DemoEase.Back);
                var gateRect = gate.rectTransform;
                m.Do(s => gateRect.localScale = new Vector3(1f, Mathf.Max(0f, rise.Eval(s).x), 1f));
            }

            // You, beside the spirit, and the walk across once the bridge is up: a tap on the farm.
            var start = new Vector2(-95f, 118f);
            var mouth = new Vector2(0f, 82f);
            var far = new Vector2(0f, -150f);
            var (you, youBody) = Walker(m, "You", HelpArt.You, start, 64f, world);
            m.Face(youBody).At(0f, -1f);
            m.Move(you).At(walk, start).At(walk + 0.5f, mouth, DemoEase.Linear).At(there, far, DemoEase.Linear);
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
            const int wood0 = 8200, gold0 = 7400;
            var woodCount = m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "", WoodPill - down, world);
            var goldCount = m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "", GoldPill - down, world);
            m.Tally(woodCount, wood0, (paid, wood0 - price.x));
            m.Tally(goldCount, gold0, (paid, gold0 - price.y));

            // The buy panel, as tapping the spirit opens it (BridgeGhost.Title).
            var purchase = TripBuyCard(m, new Vector2(60f, -12f), "Bridge to " + island, ghostArt, ghostTint, price, cardFrom, paid);

            // A blink of dark where the loop starts, on the way to the farm, and at the end.
            var dip = m.Shape("Dip", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("2E1A0E"));
            m.Fade(dip).At(0f, 1f).At(0.3f, 0f).At(until - 0.22f, 0f).At(until, 1f).At(until + 0.22f, 0f)
                .At(length - 0.35f, 0f).At(length, 1f);

            var ghostTap = down + ghostAt + new Vector2(0f, 38f);
            var farTap = down + far;
            m.Hand().Show(0.4f, 2.3f)
                .At(0.4f, ghostTap + new Vector2(120f, -90f)).At(tapGhost - 0.2f, ghostTap).Tap(tapGhost)
                .At(buy - 0.35f, purchase).Tap(buy).At(2.3f, purchase + new Vector2(70f, -50f));
            m.Hand().Show(tapFar - 0.45f, walk + 0.4f)
                .At(tapFar - 0.45f, farTap + new Vector2(120f, 70f)).At(tapFar - 0.15f, farTap).Tap(tapFar)
                .At(walk + 0.4f, farTap + new Vector2(110f, 60f));
        }

        /// <summary>
        /// When the real land art isn't to hand (a friend's island): Pebble Cove painted as the
        /// travel movies paint it, and the farm and its bridge the same way, from where
        /// IslandPainter lays them out (world units, y down).
        /// </summary>
        static Dictionary<string, TripPiece> KitchenFarmLand(HelpMovie m, Transform parent, Func<Vector2, Vector2> spot)
        {
            var made = TripFallbackLand(m, parent, spot, new[] { "Ginger Cove" });
            (Vector2 at, Vector2 size) Box(Rect r)
            {
                var a = spot(new Vector2(r.xMin, r.yMin));
                var b = spot(new Vector2(r.xMax, r.yMax));
                return ((a + b) * 0.5f, new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y)));
            }
            var (farmAt, farmSize) = Box(Rect.MinMaxRect(180f, 4500f, 2070f, 5850f));
            var farm = new TripPiece { Name = "Farm Isle" };
            float rim = Mathf.Min(farmSize.x, farmSize.y) * 0.12f;
            farm.Layers.Add(m.Shape("Farm Isle", UiKit.Circle, farmAt, farmSize, HelpArt.Sand, parent));
            farm.Layers.Add(m.Shape("Farm Isle", UiKit.Circle, farmAt, farmSize - new Vector2(rim, rim), HelpArt.Grass, parent));
            made["Farm Isle"] = farm;
            var (bridgeAt, bridgeSize) = Box(Rect.MinMaxRect(1005f, 4110f, 1245f, 4850f));
            var bridge = new TripPiece { Name = Farm.BridgePiece, IsBridge = true };
            var sand = m.Shape(Farm.BridgePiece, UiKit.Rounded(28), bridgeAt, bridgeSize, HelpArt.Sand, parent);
            sand.type = Image.Type.Sliced;
            var grass = m.Shape(Farm.BridgePiece, UiKit.Rounded(28), bridgeAt, new Vector2(bridgeSize.x * 0.82f, bridgeSize.y), HelpArt.Grass, parent);
            grass.type = Image.Type.Sliced;
            bridge.Layers.Add(sand);
            bridge.Layers.Add(grass);
            made[Farm.BridgePiece] = bridge;
            return made;
        }

        /// <summary>
        /// The minutes left to grow, as a dark pill over the plot: a clock face whose hand goes
        /// round once a minute, and the time counting down to nothing.
        /// </summary>
        static void KitchenCountdown(HelpMovie m, Vector2 at, float from, float to, float minutes)
        {
            var clock = m.Group("Clock", at);
            m.Shape("Back", UiKit.Rounded(20), Vector2.zero, new Vector2(116f, 44f), UiKit.HudGlass, clock).type = Image.Type.Sliced;
            m.Shape("Face", UiKit.Circle, new Vector2(-34f, 0f), new Vector2(30f, 30f), Color.white, clock);
            var hand = m.Shape("Hand", null, new Vector2(-34f, 0f), new Vector2(3f, 12f), UiKit.Ink, clock);
            hand.rectTransform.pivot = new Vector2(0.5f, 0f);
            hand.rectTransform.anchoredPosition = new Vector2(-34f, 0f);
            m.Shape("Pin", UiKit.Circle, new Vector2(-34f, 0f), new Vector2(5f, 5f), UiKit.Ink, clock);
            float turns = Mathf.Max(1f, Mathf.Round(minutes));
            m.Turn(hand).At(from, 0f).At(to, -360f * turns, DemoEase.Linear);
            var left = m.Label("Left", "", new Vector2(14f, 0f), 24f, Color.white, clock);
            float total = minutes * 60f;
            m.Do(s =>
            {
                float u = Mathf.Clamp01((s - from) / Mathf.Max(0.01f, to - from));
                int secs = Mathf.CeilToInt(total * (1f - u));
                string text = (secs / 60) + ":" + (secs % 60).ToString("00");
                if (left.text != text) left.text = text;
            });
            m.Size(clock).At(from, 0.6f).At(from + 0.25f, 1f, DemoEase.Back);
            m.Fade(clock).At(from - 0.01f, 0f).At(from, 1f, DemoEase.Step).At(to, 1f).At(to + 0.2f, 0f);
        }

        /// <summary>
        /// The Hobbies screen as it opens (its teal title bar, Bag along the top, the hobbies under
        /// it), then its Bag page with what you picked. Returns where the Bag button is.
        /// </summary>
        static Vector2 KitchenBagPages(HelpMovie m, float open, float bag, float shut, Sprite crop, int count)
        {
            var at = new Vector2(0f, -8f);
            var size = new Vector2(560f, 370f);
            var screen = Panel(m, at, size, KitchenHobbiesTeal, open, shut);
            float headerY = size.y * 0.5f - 22f;
            m.Shape("Icon", IconPainter.Rod, new Vector2(-size.x * 0.5f + 34f, headerY), new Vector2(30f, 30f), Color.white, screen).preserveAspect = true;
            var hubTitle = m.Label("Title", "Hobbies", new Vector2(0f, headerY), 26f, Color.white, screen);
            var bagTitle = m.Label("Title", "Bag", new Vector2(0f, headerY), 26f, Color.white, screen);
            m.Between(hubTitle.gameObject, 0f, bag);
            m.Between(bagTitle.gameObject, bag, 999f);

            // The hub: Bag along the top, then a card for each hobby.
            var hub = m.Group("Hub", Vector2.zero, screen);
            m.Between(hub.gameObject, 0f, bag);
            var bagAt = new Vector2(-130f, 104f);
            var bagButton = KitchenButton(m, hub, bagAt, new Vector2(240f, 54f), UiKit.Spring, IconPainter.Gift, "Bag", Color.white);
            m.Size(bagButton).At(bag - 0.3f, 1f).At(bag - 0.2f, 0.92f).At(bag - 0.06f, 1f);
            (Sprite icon, Color colour)[] cards =
            {
                (IconPainter.Rod, UiKit.Spring), (IconPainter.Bird, UiKit.Hex("E3A92B")), (IconPainter.Sparkle, UiKit.Hex("8A5A32")),
            };
            for (int i = 0; i < cards.Length; i++)
            {
                var row = m.Shape("Hobby", UiKit.Rounded(18), new Vector2(0f, 30f - i * 76f), new Vector2(520f, 66f), UiKit.SurfaceSunk, hub);
                row.type = Image.Type.Sliced;
                m.Shape("Disc", UiKit.Circle, new Vector2(-222f, 0f), new Vector2(48f, 48f), cards[i].colour, row.transform);
                m.Shape("Icon", cards[i].icon, new Vector2(-222f, 0f), new Vector2(28f, 28f), Color.white, row.transform).preserveAspect = true;
                Lines(m, row.transform, new Vector2(-30f, 12f), 300f, 2, 24f);
            }

            // The bag: the carrots, and how many.
            var page = m.Group("BagPage", Vector2.zero, screen);
            m.Between(page.gameObject, bag, 999f);
            var item = m.Shape("Item", UiKit.Rounded(22), new Vector2(0f, 100f), new Vector2(520f, 84f), Color.white, page);
            item.type = Image.Type.Sliced;
            m.Shape("Picture", crop, new Vector2(-214f, 0f), new Vector2(64f, 64f), Color.white, item.transform).preserveAspect = true;
            LeftLabel(m, item.transform, "×" + count, new Vector2(-166f, 0f), 30f, UiKit.Ink, 200f);
            m.Size(item.transform).At(bag, 0.85f).At(bag + 0.25f, 1f, DemoEase.Back);

            return at + bagAt;
        }

        // ---------------------------------------------------------------- cooking

        static readonly Color KitchenWood = UiKit.Hex("8A5A32"), KitchenWoodDark = UiKit.Hex("5E3B20"),
            KitchenGlow = UiKit.Hex("FFB35C"), KitchenWarm = UiKit.Hex("C98F5A");

        /// <summary>
        /// Cooking as the game plays it. Miso has a dish in a bubble over her head (she fancies
        /// carrot soup). Tap your stove and the kitchen opens: the stove and pot, the pot's empty
        /// slots, what it will make, Cook!, and the shelf of what's in your bag. Drag three carrots
        /// up into the pot (one or two make "???"; three are carrot soup). Press Cook!: the shelf
        /// goes, the stove comes to the middle, and the heat creeps up the thermometer; tap each
        /// spatula as it pops up to stir and cool it. Out comes the soup, five stars. Back on the
        /// island, tap Miso's bubble: she gets her soup, and pays in gold from a chest.
        /// </summary>
        static void CookingMovie(HelpMovie m)
        {
            const float L = 15.2f;
            m.Length = L;
            var soup = HobbyData.Dish("CarrotSoup");
            string soupName = soup != null ? soup.Name : "Carrot Soup";
            var soupArt = HobbyData.Art("Dish_CarrotSoup");
            var carrotArt = HobbyData.Art("Crop_Carrot");
            // Miso pays gold for a dish: 25, and 20 more for each level the recipe needs (DishOrders.Serve).
            int pays = 25 + (soup != null ? soup.MinLevel : 1) * 20;

            // On the island, into the kitchen, out again.
            const float tapStove = 0.95f, inside = 1.35f, outside = 10.4f;
            // Three carrots into the pot, each a press, a drag and a let-go.
            float[] presses = { 2.0f, 2.95f, 3.9f };
            float[] drops = { 2.6f, 3.55f, 4.5f };
            // Cook!, the stirs, the soup, and the card put away.
            const float cookTap = 5.1f, cookGo = 5.6f, done = 8.95f, dismiss = 9.95f;
            float[] stirFrom = { 5.95f, 6.85f, 7.8f };
            float[] stirTap = { 6.45f, 7.35f, 8.3f };
            // Miso's bubble tapped, her chest, her gold.
            const float serve = 11.3f;
            const float chestDrop = serve + 0.15f, chestOpens = chestDrop + 0.7f, chestPays = chestOpens + 0.7f;

            // ---- the island: your stove, you, and Miso wanting soup
            var island = m.Group("Island", Vector2.zero);
            During(m, island, (0f, inside), (outside, L));
            m.Island("Ground", new Vector2(0f, -35f), new Vector2(570f, 270f), island);
            var stoveAt = new Vector2(-175f, -60f);
            var stove = m.Pic("Stove", KitchenOr(HelpArt.Item("Stove"), HobbyData.Art("Stove")), stoveAt, 112f, island);
            m.Size(stove.transform).At(tapStove, 1f).At(tapStove + 0.08f, 1.08f).At(tapStove + 0.22f, 1f);
            var (_, youBody) = Walker(m, "You", HelpArt.You, new Vector2(10f, -140f), 80f, island);
            m.Face(youBody).At(0f, 1f).At(outside, -1f, DemoEase.Step);
            var misoAt = new Vector2(172f, -128f);
            m.Pic("Miso", HelpArt.Item("Coriander"), misoAt, 150f, island);

            // Her order: a white bubble with the dish in it, bobbing over her head, popped when served.
            var orderAt = misoAt + new Vector2(22f, 178f);
            var order = KitchenOrder(m, island, orderAt, 74f, soupArt);
            m.Do(s => order.anchoredPosition = orderAt + new Vector2(0f, Mathf.Sin(s * 2.2f) * 4f));
            m.Size(order).At(serve, 1f).At(serve + 0.1f, 1.25f).At(serve + 0.25f, 0f, DemoEase.In);

            // Thank you: a heart, and the chest she pays with, which opens by itself.
            var heart = UiKit.Emoji(island, Game.Emoji.Heart, 52f);
            heart.anchoredPosition = misoAt + new Vector2(-30f, 150f);
            m.Size(heart).At(serve + 0.1f, 0f).At(serve + 0.4f, 1f, DemoEase.Back).At(serve + 1.6f, 1f).At(serve + 1.8f, 0f);
            var chestAt = misoAt + new Vector2(62f, -32f);
            var shut = m.Pic("Chest", HelpArt.ChestShut, chestAt, 58f, island);
            var open = m.Pic("ChestOpen", HelpArt.ChestOpen, chestAt, 58f, island);
            m.Fade(shut).At(chestDrop, 0f).At(chestDrop + 0.01f, 1f, DemoEase.Step).At(chestOpens - 0.01f, 1f).At(chestOpens, 0f, DemoEase.Step);
            m.Size(shut.transform).At(chestDrop, 0.3f).At(chestDrop + 0.3f, 1f, DemoEase.Back);
            m.Fade(open).At(chestOpens - 0.01f, 0f).At(chestOpens, 1f, DemoEase.Step).At(chestPays + 0.7f, 1f).At(chestPays + 1.3f, 0f);

            m.Pill("Wood", HelpArt.Resource(ResourceKind.Wood), "120", WoodPill, island);
            var goldCount = m.Pill("Gold", HelpArt.Resource(ResourceKind.Gold), "", GoldPill, island);

            // ---- the kitchen (CookingScreen, laid out for a screen turned sideways)
            var kitchen = m.Group("Kitchen", Vector2.zero);
            During(m, kitchen, (inside, outside));
            m.FrameFill(s => s >= inside && s < outside ? KitchenWarm : HelpArt.Sea);
            m.Shape("Warm", null, Vector2.zero, new Vector2(640f, 440f), KitchenWarm, kitchen);

            // The stove stands on the floorboards a third of the way across; the painting of the
            // kitchen is lined up behind it, and both slide to the middle when the cooking starts.
            var feet = new Vector2(-110f, -95f);
            var middle = new Vector2(0f, -95f);
            var wall = m.Group("Wall", feet, kitchen);
            var painting = HobbyData.Art("Cook_Kitchen");
            if (painting != null)
            {
                float tall = 1260f, wide = tall * painting.rect.width / painting.rect.height;
                var pic = m.Shape("Painting", painting, Vector2.zero, new Vector2(wide, tall), Color.white, wall);
                pic.rectTransform.pivot = new Vector2(0.5f, 0.3f);   // its floor line, just behind the stove's feet
                pic.rectTransform.anchoredPosition = new Vector2(0f, 30f);
            }
            m.Shape("Wash", null, Vector2.zero, new Vector2(640f, 440f), new Color(0.18f, 0.1f, 0.05f, 0.22f), kitchen);
            m.Move(wall).At(cookTap + 0.15f, feet).At(cookTap + 0.65f, middle, DemoEase.Out);

            // The heat: nothing until you cook, then creeping up, and down again with each stir.
            var heat = new DemoKeys().At(0f, 0f).At(cookGo - 0.01f, 0f).At(cookGo, 0.25f, DemoEase.Step);
            for (int i = 0; i < stirTap.Length; i++)
                heat.At(stirTap[i], 0.55f - i * 0.03f, DemoEase.Linear).At(stirTap[i] + 0.2f, 0.24f - i * 0.03f, DemoEase.Out);
            heat.At(done, 0.38f, DemoEase.Linear);

            // The hearth: a warm glow, the stove, its fire, the pot, and the slots under it.
            const float S = 200f;
            var hearth = m.Group("Hearth", feet, kitchen);
            var glow = m.Shape("Glow", IconPainter.Glow, new Vector2(0f, 100f), new Vector2(400f, 330f), new Color(KitchenGlow.r, KitchenGlow.g, KitchenGlow.b, 0.35f), hearth);
            m.Do(s =>
            {
                float h = heat.Eval(s).x;
                var c = Color.Lerp(KitchenGlow, UiKit.Danger, h);
                c.a = h <= 0f ? 0.35f : 0.18f + h * 0.3f + Mathf.Sin(s * 6f) * 0.04f;
                glow.color = c;
            });
            m.Shape("Stove", KitchenOr(HobbyData.Art("Cook_Stove"), HobbyData.Art("Stove")), new Vector2(0f, S * 0.33f), new Vector2(S * 0.66f, S * 0.66f), Color.white, hearth).preserveAspect = true;
            var fireAt = new Vector2((0.33f - 0.5f) * S, 0.22f * S);
            var floorLight = m.Shape("FloorLight", IconPainter.Glow, new Vector2(fireAt.x, 0.02f * S), new Vector2(S * 0.7f, S * 0.2f), new Color(1f, 0.62f, 0.25f, 0.3f), hearth);
            var fire = m.Shape("Firelight", IconPainter.Glow, fireAt, new Vector2(S * 0.42f, S * 0.42f), new Color(1f, 0.6f, 0.22f, 0.6f), hearth);
            m.Do(s =>
            {
                float h = heat.Eval(s).x;
                float flicker = 0.8f + 0.12f * Mathf.Sin(s * 9.1f) + 0.08f * Mathf.Sin(s * 23.7f + 1.3f);
                var c = Color.Lerp(new Color(1f, 0.6f, 0.22f), new Color(1f, 0.38f, 0.16f), h);
                fire.color = new Color(c.r, c.g, c.b, (0.5f + h * 0.4f) * flicker);
                fire.rectTransform.localScale = Vector3.one * (1f + h * 0.35f) * (0.96f + 0.06f * flicker);
                floorLight.color = new Color(c.r, c.g, c.b, (0.22f + h * 0.25f) * flicker);
            });
            var potAt = new Vector2(0f, 0.62f * S);
            var pot = m.Shape("Pot", KitchenOr(HobbyData.Art("Cook_Pot"), HobbyData.Art("Pot")), potAt, new Vector2(S * 0.5f, S * 0.5f), Color.white, hearth);
            pot.preserveAspect = true;
            var plop = m.Size(pot.transform).At(0f, 1f);
            foreach (var d in drops) plop.At(d, 1f).At(d + 0.08f, 1.07f).At(d + 0.3f, 1f);
            var stir = m.Turn(pot.transform).At(0f, 0f);
            foreach (var t in stirTap) stir.At(t, 0f).At(t + 0.08f, 6f).At(t + 0.2f, -4f).At(t + 0.35f, 0f);
            KitchenSteam(m, hearth, potAt + new Vector2(0f, 34f), s =>
                s >= cookGo && s < done ? 0.55f + heat.Eval(s).x * 0.45f : s >= drops[0] ? 0.6f : 0.3f);

            // The slots under the stove: what's in the pot.
            for (int i = 0; i < drops.Length; i++)
            {
                var slotAt = new Vector2((i - 1) * 62f, -32f);
                m.Shape("Slot", UiKit.Circle, slotAt, new Vector2(48f, 48f), new Color(1f, 1f, 1f, 0.35f), hearth);
                var full = m.Shape("Full", UiKit.Circle, slotAt, new Vector2(48f, 48f), Color.white, hearth);
                m.Shape("Pic", carrotArt, Vector2.zero, new Vector2(36f, 36f), Color.white, full.transform).preserveAspect = true;
                m.Between(full.gameObject, drops[i], 999f);
                m.Size(full.transform).At(drops[i], 0.4f).At(drops[i] + 0.25f, 1f, DemoEase.Back);
            }

            // The thermometer beside the pot, up only while cooking: a red zone at the top.
            var thermo = m.Group("Heat", new Vector2(0.36f * S + 22f, 0.46f * S), hearth);
            m.Shape("Tube", UiKit.Rounded(12), Vector2.zero, new Vector2(28f, 152f), new Color(0.12f, 0.08f, 0.05f, 0.6f), thermo).type = Image.Type.Sliced;
            m.Shape("Red", UiKit.Rounded(10), new Vector2(0f, 59f), new Vector2(18f, 24f), new Color(UiKit.Danger.r, UiKit.Danger.g, UiKit.Danger.b, 0.35f), thermo).type = Image.Type.Sliced;
            var fill = m.Shape("Fill", UiKit.Rounded(10), new Vector2(0f, -73f), new Vector2(18f, 10f), UiKit.Leaf, thermo);
            fill.type = Image.Type.Sliced;
            fill.rectTransform.pivot = new Vector2(0.5f, 0f);
            fill.rectTransform.anchoredPosition = new Vector2(0f, -73f);
            m.Do(s =>
            {
                float h = Mathf.Clamp01(heat.Eval(s).x);
                fill.rectTransform.sizeDelta = new Vector2(18f, Mathf.Max(10f, h * 146f));
                fill.color = h > 0.82f ? UiKit.Danger : h > 0.6f ? UiKit.Sun : UiKit.Leaf;
            });
            m.Shape("Bulb", IconPainter.Flame, new Vector2(0f, -96f), new Vector2(30f, 30f), KitchenGlow, thermo).preserveAspect = true;
            m.Size(thermo).At(cookTap + 0.29f, 0f).At(cookTap + 0.3f, 0.2f, DemoEase.Step).At(cookTap + 0.7f, 1f, DemoEase.Back)
                .At(done - 0.01f, 1f).At(done, 0f, DemoEase.Step);

            // The spatulas: one pops up over the pot, its ring running out; a tap in time stirs.
            float window = 1.58f;   // CookingScreen at cooking level 1: 1.5 s, a little kinder each level
            Vector2[] stirAt = { new Vector2(-30f, 118f), new Vector2(34f, 132f), new Vector2(-4f, 122f) };
            for (int i = 0; i < stirAt.Length; i++) KitchenSpatula(m, hearth, stirAt[i], stirFrom[i], stirTap[i], window);

            m.Move(hearth).At(cookTap + 0.15f, feet).At(cookTap + 0.65f, middle, DemoEase.Out);
            m.Size(hearth).At(cookTap + 0.15f, 1f).At(cookTap + 0.65f, 1.12f, DemoEase.Back);

            // The title, Recipes, and the way out, along the top.
            var chip = m.Shape("TitleChip", UiKit.Rounded(22), new Vector2(-212f, 172f), new Vector2(156f, 44f), UiKit.HudGlass, kitchen);
            chip.type = Image.Type.Sliced;
            m.Shape("Icon", IconPainter.Flame, new Vector2(-56f, 0f), new Vector2(28f, 28f), KitchenGlow, chip.transform).preserveAspect = true;
            LeftLabel(m, chip.transform, "Cooking", new Vector2(-36f, 0f), 24f, Color.white, 104f);
            KitchenButton(m, kitchen, new Vector2(176f, 172f), new Vector2(128f, 42f), UiKit.SurfaceSunk, IconPainter.Book, "Recipes", UiKit.Ink);
            var close = m.Shape("Close", UiKit.Circle, new Vector2(272f, 172f), new Vector2(46f, 46f), UiKit.HudGlass, kitchen);
            m.Shape("X", IconPainter.Close, Vector2.zero, new Vector2(20f, 20f), Color.white, close.transform);

            // What to do now, in the pill under the title.
            var status = m.Group("Status", new Vector2(-110f, 120f), kitchen);
            m.Shape("Pill", UiKit.Rounded(22), Vector2.zero, new Vector2(300f, 44f), UiKit.HudGlass, status).type = Image.Type.Sliced;
            KitchenSay(m, status, "Drag ingredients into the pot", 0f, cookTap);
            KitchenSay(m, status, "Tap the spatula when it appears, to stir and cool it down!", cookTap, stirTap[0]);
            KitchenSay(m, status, "<color=#8EE59A><b>Perfect stir!</b></color>", stirTap[0], 999f);
            m.Move(status).At(cookTap + 0.15f, new Vector2(-110f, 120f)).At(cookTap + 0.45f, new Vector2(0f, 120f));

            // The counter on the right: what it makes and Cook! along its top, the shelf under them.
            var counterAt = new Vector2(167f, -30f);
            var counter = m.Group("Counter", counterAt, kitchen);
            m.Move(counter).At(cookTap + 0.1f, counterAt).At(cookTap + 0.45f, counterAt + new Vector2(140f, 0f), DemoEase.In);
            m.Fade(counter).At(cookTap + 0.1f, 1f).At(cookTap + 0.4f, 0f);

            var makes = m.Shape("Makes", UiKit.Rounded(20), new Vector2(-55f, 123f), new Vector2(128f, 66f), UiKit.Surface, counter);
            makes.type = Image.Type.Sliced;
            var nothing = LeftLabel(m, makes.transform, "<color=#8A7563>Empty pot</color>", new Vector2(-56f, 0f), 18f, UiKit.Ink, 112f);
            m.Between(nothing.gameObject, 0f, drops[0]);
            var odd = m.Shape("Bizarre", HobbyData.Art("Dish_Bizarre"), new Vector2(-34f, 0f), new Vector2(50f, 50f), Color.white, makes.transform);
            odd.preserveAspect = true;
            var unknown = m.Label("Unknown", "???", new Vector2(24f, 0f), 22f, UiKit.Ink, makes.transform);
            m.Between(odd.gameObject, drops[0], drops[2]);
            m.Between(unknown.gameObject, drops[0], drops[2]);
            var dish = m.Shape("Dish", soupArt, new Vector2(-34f, 0f), new Vector2(50f, 50f), Color.white, makes.transform);
            dish.preserveAspect = true;
            var dishName = LeftLabel(m, makes.transform, soupName, new Vector2(-4f, 0f), 17f, UiKit.Ink, 62f);
            m.Between(dish.gameObject, drops[2], 999f);
            m.Between(dishName.gameObject, drops[2], 999f);
            m.Size(dish.transform).At(drops[2], 0.4f).At(drops[2] + 0.3f, 1f, DemoEase.Back);

            var cookAt = new Vector2(71f, 123f);
            var cook = KitchenButton(m, counter, cookAt, new Vector2(94f, 60f), UiKit.Primary, IconPainter.Flame, "Cook!", Color.white);
            m.Fade(cook).At(drops[0] - 0.01f, 0.5f).At(drops[0], 1f, DemoEase.Step);
            m.Size(cook).At(cookTap - 0.1f, 1f).At(cookTap, 0.9f).At(cookTap + 0.14f, 1f);

            var shelf = m.Shape("Shelf", UiKit.Rounded(20), new Vector2(0f, -37f), new Vector2(238f, 237f), KitchenWood, counter);
            shelf.type = Image.Type.Sliced;
            m.Shape("Lip", UiKit.Rounded(10), new Vector2(0f, -151f), new Vector2(238f, 10f), KitchenWoodDark, counter).type = Image.Type.Sliced;
            // What's in the bag to cook with: some fish, the carrots, Chef Tofu's eggs and milk, a cabbage.
            var tiles = new (string id, Sprite art, int count)[]
            {
                ("fish", HelpArt.Picture("Fish_salmon"), 2), ("carrot", carrotArt, 3), ("egg", HobbyData.Art("Pantry_Egg"), 3),
                ("cabbage", HobbyData.Art("Crop_Cabbage"), 2), ("milk", HobbyData.Art("Pantry_Milk"), 2), ("flour", HobbyData.Art("Pantry_Flour"), 3),
            };
            var carrotTile = Vector2.zero;
            for (int i = 0; i < tiles.Length; i++)
            {
                var tileAt = new Vector2(-72f + (i % 3) * 72f, 30f - (i / 3) * 82f);
                var tile = m.Shape("Tile", UiKit.Rounded(18), tileAt, new Vector2(66f, 75f), UiKit.Surface, counter);
                tile.type = Image.Type.Sliced;
                var pic = m.Shape("Pic", tiles[i].art, new Vector2(0f, 8f), new Vector2(46f, 46f), Color.white, tile.transform);
                pic.preserveAspect = true;
                var n = m.Label("Count", "×" + tiles[i].count, new Vector2(0f, -26f), 17f, UiKit.InkSoft, tile.transform);
                if (tiles[i].id != "carrot") continue;
                carrotTile = counterAt + tileAt;
                // Each carrot dragged out leaves one fewer; with none left the tile greys.
                m.Do(s =>
                {
                    int gone = 0;
                    foreach (var d in drops) if (s >= d) gone++;
                    int have = 3 - gone;
                    string text = "×" + have;
                    if (n.text != text) n.text = text;
                    float a = have > 0 ? 1f : 0.4f;
                    var back = UiKit.Surface;
                    back.a = a;
                    tile.color = have > 0 ? back : new Color(1f, 1f, 1f, a);
                    pic.color = new Color(1f, 1f, 1f, a);
                });
            }

            // A carrot under the finger as it's carried up to the pot.
            var potOnStage = feet + potAt;
            for (int i = 0; i < presses.Length; i++)
            {
                var ghost = m.Shape("Ghost", carrotArt, carrotTile, new Vector2(64f, 64f), Color.white, kitchen);
                ghost.preserveAspect = true;
                m.Move(ghost.transform).At(presses[i], carrotTile).At(drops[i], potOnStage, DemoEase.Linear);
                m.Between(ghost.gameObject, presses[i], drops[i]);
            }

            // Out it comes: the card with the dish, and its stars.
            var scrim = m.Shape("Scrim", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Scrim, kitchen);
            m.Fade(scrim).At(done - 0.01f, 0f).At(done + 0.2f, UiKit.Scrim.a).At(dismiss + 0.05f, UiKit.Scrim.a).At(dismiss + 0.25f, 0f);
            var card = Panel(m, Vector2.zero, new Vector2(320f, 260f), null, done, dismiss + 0.05f);
            card.SetParent(kitchen, false);
            m.Pic("Dish", soupArt, new Vector2(0f, -10f), 120f, card);
            var title = m.Label("Title", soupName, new Vector2(0f, -66f), 30f, UiKit.Ink, card);
            Fit(title, soupName, 290f);
            var stars = m.Label("Stars", "<color=#E3A92B><b>Five stars — perfect!</b></color>", new Vector2(0f, -100f), 20f, UiKit.Ink, card);
            Fit(stars, "<color=#E3A92B><b>Five stars — perfect!</b></color>", 290f);
            m.Sparkle(new Vector2(0f, 40f), done + 0.1f, UiKit.Sun, 7);
            // The stirs' puffs, over the pot where it stands once it's in the middle.
            for (int i = 0; i < stirAt.Length; i++) m.Sparkle(middle + stirAt[i] * 1.12f, stirTap[i] + 0.02f, Color.white, 4);

            // Miso's gold flies up to its counter.
            float land = m.Ping(HelpArt.Resource(ResourceKind.Gold), "+" + pays, chestAt + new Vector2(0f, 60f), PillIcon(GoldPill), chestPays);
            const int gold0 = 60;
            m.Tally(goldCount, gold0, (land, gold0 + pays));
            m.Sparkle(PillIcon(GoldPill), land, UiKit.Sun, 5);
            m.Sparkle(orderAt, serve + 0.1f, Color.white, 7);

            // A blink of darkness going into the kitchen and out again.
            var dip = m.Shape("Dip", null, Vector2.zero, new Vector2(640f, 440f), UiKit.Hex("2E1A0E"));
            var dark = m.Fade(dip).At(0f, 1f).At(0.3f, 0f);
            foreach (var t in new[] { inside, outside }) dark.At(t - 0.22f, 0f).At(t, 1f).At(t + 0.22f, 0f);
            dark.At(L - 0.35f, 0f).At(L, 1f);

            // The fingers: the stove; three carrots and Cook!; the stirs; the card; Miso's bubble.
            m.Hand().Show(0.35f, 1.15f)
                .At(0.35f, stoveAt + new Vector2(120f, 100f)).At(0.8f, stoveAt + new Vector2(0f, 46f)).Tap(tapStove);
            var hand = m.Hand().Show(inside + 0.35f, cookTap + 0.4f).At(inside + 0.35f, carrotTile + new Vector2(60f, -70f));
            for (int i = 0; i < presses.Length; i++)
            {
                hand.At(presses[i] - 0.18f, carrotTile).Press(presses[i]).At(presses[i], carrotTile)
                    .At(drops[i], potOnStage, DemoEase.Linear).Lift(drops[i] + 0.05f);
            }
            var cookOnStage = counterAt + cookAt;
            hand.At(cookTap - 0.3f, cookOnStage).Tap(cookTap).At(cookTap + 0.4f, cookOnStage + new Vector2(40f, -70f));
            var stirHand = m.Hand().Show(stirFrom[0] + 0.1f, stirTap[stirTap.Length - 1] + 0.35f)
                .At(stirFrom[0] + 0.1f, middle + stirAt[0] * 1.12f + new Vector2(110f, -110f));
            for (int i = 0; i < stirAt.Length; i++)
                stirHand.At(stirTap[i] - 0.22f, middle + stirAt[i] * 1.12f).Tap(stirTap[i]);
            stirHand.At(stirTap[stirTap.Length - 1] + 0.35f, middle + stirAt[stirAt.Length - 1] * 1.12f + new Vector2(110f, -110f));
            m.Hand().Show(done + 0.45f, dismiss + 0.3f)
                .At(done + 0.45f, new Vector2(150f, -150f)).At(dismiss - 0.3f, new Vector2(30f, -20f)).Tap(dismiss)
                .At(dismiss + 0.3f, new Vector2(150f, -150f));
            m.Hand().Show(outside + 0.3f, serve + 0.5f)
                .At(outside + 0.3f, orderAt + new Vector2(-130f, -120f)).At(serve - 0.3f, orderAt).Tap(serve)
                .At(serve + 0.5f, orderAt + new Vector2(-120f, -110f));
        }

        /// <summary>The first picture that's there.</summary>
        static Sprite KitchenOr(Sprite a, Sprite b) => a != null ? a : b;

        /// <summary>A line in the status pill, shown between two moments, kept within it.</summary>
        static void KitchenSay(HelpMovie m, Transform pill, string english, float from, float to)
        {
            var t = m.Label("Say", english, Vector2.zero, 20f, Color.white, pill);
            Fit(t, english, 280f);
            m.Between(t.gameObject, from, to);
        }

        /// <summary>
        /// A chunky button as UiKit draws them: a darker lip under the face, and its icon and word
        /// side by side in the middle.
        /// </summary>
        static RectTransform KitchenButton(HelpMovie m, Transform parent, Vector2 at, Vector2 size, Color fill, Sprite icon, string words, Color ink)
        {
            var g = m.Group("Button", at, parent);
            m.Shape("Lip", UiKit.Rounded(18), new Vector2(0f, -3f), size, Color.Lerp(fill, Color.black, 0.22f), g).type = Image.Type.Sliced;
            m.Shape("Face", UiKit.Rounded(18), new Vector2(0f, 2f), size - new Vector2(0f, 4f), fill, g).type = Image.Type.Sliced;
            float iconSize = size.y * 0.48f, gap = 6f;
            var label = LeftLabel(m, g, words, new Vector2(0f, 2f), size.y * 0.42f, ink, size.x - iconSize - gap - 20f);
            float w = label.rectTransform.sizeDelta.x - 4f;
            float start = -(iconSize + gap + w) * 0.5f;
            if (icon != null)
            {
                var ic = m.Shape("Icon", icon, new Vector2(start + iconSize * 0.5f, 2f), new Vector2(iconSize, iconSize), ink, g);
                ic.preserveAspect = true;
            }
            else start = -w * 0.5f - (iconSize + gap);
            label.rectTransform.anchoredPosition = new Vector2(start + iconSize + gap + w * 0.5f, 2f);
            return g;
        }

        /// <summary>A dish in the white bubble over an islander's head (WorldBubble): a disc, a little tail, the dish.</summary>
        static RectTransform KitchenOrder(HelpMovie m, Transform parent, Vector2 at, float size, Sprite dish)
        {
            var g = m.Group("Order", at, parent);
            m.Shape("Tail", UiKit.Circle, new Vector2(-0.25f, -0.5f) * size, Vector2.one * size * 0.3f, Color.white, g);
            m.Shape("Disc", UiKit.Circle, Vector2.zero, Vector2.one * size, Color.white, g);
            m.Shape("Dish", dish, Vector2.zero, Vector2.one * size * 0.7f, Color.white, g).preserveAspect = true;
            return g;
        }

        /// <summary>
        /// A spatula over the pot (CookingScreen's stir prompt): a white disc that pops up at
        /// <paramref name="from"/>, its green ring running out (yellow, then red, as it goes),
        /// gone when it's tapped at <paramref name="tap"/>.
        /// </summary>
        static void KitchenSpatula(HelpMovie m, Transform hearth, Vector2 at, float from, float tap, float window)
        {
            var g = m.Group("Stir", at, hearth);
            var ring = m.Shape("Ring", UiKit.Circle, Vector2.zero, new Vector2(84f, 84f), UiKit.Leaf, g);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            m.Shape("Disc", UiKit.Circle, Vector2.zero, new Vector2(70f, 70f), Color.white, g);
            m.Shape("Spatula", IconPainter.Spatula, Vector2.zero, new Vector2(42f, 42f), KitchenWood, g).preserveAspect = true;
            m.Do(s =>
            {
                float rest = Mathf.Clamp01(1f - (s - from) / window);
                ring.fillAmount = rest;
                ring.color = rest > 0.5f ? UiKit.Leaf : rest > 0.2f ? UiKit.Sun : UiKit.Danger;
            });
            m.Size(g).At(from - 0.01f, 0f).At(from, 0f).At(from + 0.15f, 1f, DemoEase.Back).At(tap, 1f).At(tap + 0.12f, 0f);
        }

        /// <summary>Wisps of steam rising off the pot, as thick as <paramref name="strength"/> says.</summary>
        static void KitchenSteam(HelpMovie m, Transform parent, Vector2 at, Func<float, float> strength)
        {
            const int count = 5;
            for (int i = 0; i < count; i++)
            {
                var puff = m.Shape("Steam", IconPainter.Glow, at, new Vector2(34f, 34f), new Color(1f, 1f, 1f, 0f), parent);
                float phase = i / (float)count, sway = i * 1.7f;
                m.Do(s =>
                {
                    float u = Mathf.Repeat(s / 1.6f + phase, 1f);
                    puff.rectTransform.anchoredPosition = at + new Vector2(Mathf.Sin(sway + u * 3f) * 12f + (phase - 0.5f) * 40f, u * 70f);
                    puff.rectTransform.localScale = Vector3.one * (0.6f + u * 1.4f);
                    puff.color = new Color(1f, 1f, 1f, strength(s) * Mathf.Sin(u * Mathf.PI) * 0.6f);
                });
            }
        }
    }
}
