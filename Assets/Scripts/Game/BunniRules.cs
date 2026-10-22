using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>What a resource node is, in the original's terms. Each has its own price and seed rate.</summary>
    public enum NodeResource
    {
        Pine, Maple, Ironwood,
        Bronze, Silver, Gold,
        Posie, Tulip, Rose,
    }

    /// <summary>The three families of node, which decide the swing animation and who may harvest.</summary>
    public enum FruitKind { Apple, Pear, Plum }

    public enum NodeFamily
    {
        Tree,
        Rock,
        Flower,
    }

    /// <summary>
    /// The original game's numbers, in one place. Every value cites where it comes from — the
    /// shipped config (docs/reference/game.xml) or the decompiled AS3 summarised in
    /// docs/BUNNI_GROUND_TRUTH.md. Nothing here is tuned by feel; change it only with a source.
    ///
    /// The original runs at 30 frames per second and every rate is per frame, so gameplay code
    /// steps in whole frames through <see cref="FrameClock"/> rather than scaling by deltaTime.
    /// </summary>
    public static class BunniRules
    {
        /// <summary>The SWF's frame rate. Every per-frame number below assumes it.</summary>
        public const int FramesPerSecond = 30;

        // ---------------------------------------------------------------- nodes

        /// <summary>Scale a node starts at when it appears (ForestGrow/GardenGrow/RockGrow.Enter).</summary>
        public const float SpawnScale = 0.2f;

        /// <summary>game.xml &lt;price&gt;: value of one harvested node; for flowers, hunger restored.</summary>
        public static int Price(NodeResource r) => r switch
        {
            NodeResource.Pine => 3,
            NodeResource.Maple => 7,
            NodeResource.Ironwood => 15,
            NodeResource.Bronze => 2,
            NodeResource.Silver => 7,
            NodeResource.Gold => 20,
            NodeResource.Posie => 500,
            NodeResource.Tulip => 750,
            NodeResource.Rose => 1600,
            _ => 0,
        };

        /// <summary>game.xml &lt;seedrates&gt;: frames between a parent's seed attempts. Lower is faster.</summary>
        public static int SeedRateFrames(NodeResource r) => r switch
        {
            NodeResource.Pine => 100,
            NodeResource.Maple => 50,
            NodeResource.Ironwood => 25,
            NodeResource.Bronze => 100,
            NodeResource.Silver => 50,
            NodeResource.Gold => 25,
            NodeResource.Posie => 300,
            NodeResource.Tulip => 200,
            NodeResource.Rose => 100,
            _ => 100,
        };

        public static NodeFamily FamilyOf(NodeResource r) => r switch
        {
            NodeResource.Pine or NodeResource.Maple or NodeResource.Ironwood => NodeFamily.Tree,
            NodeResource.Bronze or NodeResource.Silver or NodeResource.Gold => NodeFamily.Rock,
            _ => NodeFamily.Flower,
        };

        /// <summary>Scale gained per frame while growing: 0.005 trees and rocks, 0.03 flowers.</summary>
        public static float GrowthPerFrame(NodeFamily f) => f == NodeFamily.Flower ? 0.03f : 0.005f;

        /// <summary>
        /// Final scale range, randomised per node. Trees and flowers: parent 1.25–1.75, child
        /// 0.75–1.25. Rocks: parent 1.0–1.5, child 0.5–1.0. Returns (min, max).
        /// </summary>
        public static Vector2 FinalScaleRange(NodeFamily f, bool parent)
        {
            if (f == NodeFamily.Rock) return parent ? new Vector2(1.0f, 1.5f) : new Vector2(0.5f, 1.0f);
            return parent ? new Vector2(1.25f, 1.75f) : new Vector2(0.75f, 1.25f);
        }

        /// <summary>
        /// Not the original's: Capy Island's groves grow closer together (playtesters found the
        /// original's trees and rocks too scattered, 2026-10-22). A seed is tested with, and its child
        /// takes, this share of the original's footprint, so neighbours stand nearer...
        /// </summary>
        public const float GroveSpacing = 0.6f;

        /// <summary>
        /// ...and the grove fills only this share of its outer circle, so it holds about as many
        /// trees as the original's (0.65² of the area at 1/0.6² the packing), just in a tighter clump.
        /// </summary>
        public const float GroveSpread = 0.65f;

        /// <summary>
        /// A placed parent tries this many children at once: outerRadius / 10 for forests and gardens,
        /// outerRadius / 5 for hills (HillItem.initStateMachine).
        /// </summary>
        public static int InitialChildAttempts(float outerRadius, NodeFamily family) =>
            Mathf.FloorToInt(outerRadius / (family == NodeFamily.Rock ? 5f : 10f));

        // ---------------------------------------------------------------- harvesting

        /// <summary>
        /// Scale removed by one hit: 0.1 × speed / 3 (AnimalType.GetSpeedFromType divides by 3).
        /// A capybara (speed 3) removes exactly 0.1.
        /// </summary>
        public static float ScalePerHit(int speed) => 0.1f * speed / 3f;

        /// <summary>Swing length in frames and the frame the hit lands on (GenericAnimal constants).</summary>
        public static void Swing(NodeFamily f, out int frames, out int hitFrame)
        {
            switch (f)
            {
                case NodeFamily.Rock: frames = 18; hitFrame = 5; break;    // 314–331, hit 319
                case NodeFamily.Flower: frames = 9; hitFrame = 4; break;   // 333–341, hit 337
                default: frames = 15; hitFrame = 3; break;                 // 299–313, hit 302
            }
        }

        // ---------------------------------------------------------------- workers

        /// <summary>A mill's or mine's work boundary: a fixed 800-unit circle (MillItem.mBoundry).</summary>
        public const float WorkplaceBoundary = 800f;

        /// <summary>Frames between target searches while looking for work (FindWood's counter).</summary>
        public const int SearchIntervalFrames = 61;

        /// <summary>Where an idle worker naps: 70 units from its workplace, at a random 0–180° angle.</summary>
        public const float SleepSpotDistance = 70f;

        /// <summary>Hunger: starts at 1500, drains 1 per active frame, hungry below 1000.</summary>
        public const int StartingHunger = 1500;
        public const int HungryBelow = 1000;

        /// <summary>game.xml &lt;animals&gt;&lt;bunni&gt;: the capybara stands in for the bunni.</summary>
        // ---- fruit trees (MapItems/FruitTrees) ----

        /// <summary>A fruit tree's footprint: a fixed circle, not scaled by growth.</summary>
        public const float FruitTreeInnerRadius = 50f;
        /// <summary>A fruit tree grows from 0.2 to exactly 1 at this rate per frame.</summary>
        public const float FruitTreeGrowthPerFrame = 0.005f;
        public const int MaxFruitOnTree = 5;
        /// <summary>Frames the shake animation plays (art frames 2..12).</summary>
        public const int ShakeFrames = 11;
        /// <summary>Scale a full-grown tree drops to the moment a worker starts chopping it.</summary>
        public const float ChoppedStartScale = 0.9f;
        /// <summary>Chopped trees regrow at a fifth of the growth rate.</summary>
        public const float ChoppedRegrowPerFrame = FruitTreeGrowthPerFrame / 5f;
        /// <summary>Workers chop fruit trees at a flat 0.5 harvest speed (not the species' speed / 3).</summary>
        public const float FruitTreeHarvestSpeed = 0.5f;
        /// <summary>Wood carried home for felling a fruit tree. Not doubled by a Legendary mill.</summary>
        public const int FruitTreeWoodValue = 500;
        /// <summary>A dropped fruit is a food node with this inner size.</summary>
        public const float FruitInnerSize = 22f;
        /// <summary>Seconds between saplings: the first fruit shaken off after this has passed is a free tree.</summary>
        public const float SaplingIntervalSeconds = 60f;
        /// <summary>Chance a shaken fruit is instead a bubble holding a random store item.</summary>
        public const float RandomStoreChance = 0.01f;

        /// <summary>Per-frame chance of a new fruit while below the maximum: 0.005 divided per tree.</summary>
        public static float FruitChancePerFrame(FruitKind kind) => kind switch
        {
            FruitKind.Apple => 0.005f / 2f,
            FruitKind.Pear => 0.005f / 2.5f,
            FruitKind.Plum => 0.005f / 3f,
            _ => 0.005f,
        };

        /// <summary>The food a fallen fruit counts as: apples are posies, pears tulips, plums roses.</summary>
        public static NodeResource FruitFood(FruitKind kind) => kind switch
        {
            FruitKind.Apple => NodeResource.Posie,
            FruitKind.Pear => NodeResource.Tulip,
            _ => NodeResource.Rose,
        };

        /// <summary>Chance a shaken fruit is a treasure chest instead, and the chest's tier.</summary>
        public static float ChestChance(FruitKind kind, out int tier)
        {
            switch (kind)
            {
                case FruitKind.Apple: tier = 1; return 0.2f;
                case FruitKind.Pear: tier = 2; return 0.3f;
                default: tier = 3; return 0.2f;
            }
        }

        /// <summary>Store name of each tree, for the free sapling bubble.</summary>
        public static string FruitTreeStoreName(FruitKind kind) => kind switch
        {
            FruitKind.Apple => "Apple Tree",
            FruitKind.Pear => "Pear Tree",
            _ => "Legendary Plum Tree",
        };

        /// <summary>Frames a timed bubble or fallen fruit lasts: (rand x 10 + 1) x 200.</summary>
        public static int RandomLifetimeFrames() => Mathf.FloorToInt((Random.value * 10f + 1f) * 200f);

        public const int CapybaraSpeed = 3;
        public const float CapybaraFoodRate = 1f;
    }

    /// <summary>
    /// Converts between the original's world and ours. The original world is measured in its own
    /// units and drawn 2:1 — a world circle shows on screen as an ellipse twice as wide as tall — so
    /// every radius and distance in the rules is a world measure.
    ///
    /// <see cref="UnitsPerWorld"/> is our choice, not the original's: it is set so a capybara and a
    /// tree keep the proportions the original's art has (a bunni is roughly 60 world units wide).
    /// Change it to rescale the whole world consistently.
    /// </summary>
    public static class BunniSpace
    {
        /// <summary>Unity units per original world unit, horizontally. Vertically it is half.</summary>
        public const float UnitsPerWorld = 0.04f;

        /// <summary>Unity-space offset → original world offset (y doubled back out of the 2:1 view).</summary>
        public static Vector2 ToWorld(Vector2 unityDelta) =>
            new Vector2(unityDelta.x / UnitsPerWorld, unityDelta.y * 2f / UnitsPerWorld);

        /// <summary>Original world offset → Unity-space offset.</summary>
        public static Vector2 ToUnity(Vector2 worldDelta) =>
            new Vector2(worldDelta.x * UnitsPerWorld, worldDelta.y * UnitsPerWorld * 0.5f);

        /// <summary>Distance between two Unity positions, measured in original world units.</summary>
        public static float WorldDistance(Vector2 a, Vector2 b) => ToWorld(b - a).magnitude;

        /// <summary>
        /// One frame's step toward a target at <paramref name="worldSpeed"/> world units per frame.
        /// Returns true (and lands exactly) when the target is within one step.
        /// </summary>
        public static bool Step(ref Vector2 position, Vector2 target, float worldSpeed)
        {
            var world = ToWorld(target - position);
            float distance = world.magnitude;
            if (distance <= worldSpeed) { position = target; return true; }
            position += ToUnity(world / distance * worldSpeed);
            return false;
        }

        /// <summary>
        /// A uniformly random point inside a world circle, as a Unity position — the original's
        /// Circle.RandomPoint (sqrt of a uniform radius, uniform angle).
        /// </summary>
        public static Vector2 RandomPointInCircle(Vector2 centreUnity, float worldRadius)
        {
            float r = worldRadius * Mathf.Sqrt(Random.value);
            float a = Random.value * Mathf.PI * 2f;
            return centreUnity + ToUnity(new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }

        // ---------------------------------------------------------------- the land

        /// <summary>Our landmass ellipse, in Unity units. Mirrors IslandBuilder.</summary>
        public const float IslandRadiusX = 14.4f;
        public const float IslandRadiusY = 10.4f;

        /// <summary>
        /// True when a world circle lies entirely on land — the first half of the original's
        /// TestCollision.CircleOnMap. Land is the <see cref="LandMap"/>'s union of circles; the
        /// ellipse below is only a fallback for scenes without one.
        /// </summary>
        public static bool CircleOnLand(Vector2 centreUnity, float worldRadius)
        {
            var land = LandMap.Instance;
            if (land != null) return land.CircleOnLand(centreUnity, worldRadius);

            var r = ToUnity(new Vector2(worldRadius, worldRadius));
            float rx = IslandRadiusX - r.x, ry = IslandRadiusY - r.y;
            if (rx <= 0f || ry <= 0f) return false;
            float nx = centreUnity.x / rx, ny = centreUnity.y / ry;
            return nx * nx + ny * ny <= 1f;
        }

        /// <summary>
        /// The full CircleOnMap test: on land, and not overlapping any other item's inner circle.
        /// Animals, the avatar and items still riding in the trail do not block, exactly as in the
        /// original.
        /// </summary>
        /// <summary>True if a Unity position is on land.</summary>
        public static bool PointOnLand(Vector2 unity)
        {
            var land = LandMap.Instance;
            if (land != null) return land.IsOnLand(unity);
            float nx = unity.x / IslandRadiusX, ny = unity.y / IslandRadiusY;
            return nx * nx + ny * ny <= 1f;
        }

        public static bool CircleOnMap(Vector2 centreUnity, float worldRadius, Object ignore = null)
        {
            if (!CircleOnLand(centreUnity, worldRadius)) return false;

            foreach (var node in Object.FindObjectsByType<Harvestable>(FindObjectsSortMode.None))
            {
                if (node == ignore || !node.BlocksPlacement) continue;
                if (WorldDistance(centreUnity, node.transform.position) < worldRadius + node.InnerRadius) return false;
            }
            foreach (var item in Object.FindObjectsByType<Placeable>(FindObjectsSortMode.None))
            {
                if (item == ignore || !item.Placed) continue;
                if (item.GetComponent<Harvestable>() != null) continue;   // counted above
                if (WorldDistance(centreUnity, item.transform.position) < worldRadius + item.InnerRadius) return false;
            }
            // A shop's wares are map items too: nothing seeds or is put down on one.
            foreach (var ware in SaleDisplay.All)
            {
                if (ware == ignore || !ware.Blocks) continue;
                if (WorldDistance(centreUnity, ware.transform.position) < worldRadius + ware.InnerRadius) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Turns real time into whole original frames. Each component keeps one and asks it how many
    /// 30 Hz frames to run this Update, so every per-frame rule runs at the original's rate
    /// regardless of the device's frame rate.
    /// </summary>
    public struct FrameClock
    {
        float _carry;

        /// <summary>Whole frames elapsed since the last call. Capped, so a hitch cannot spiral.</summary>
        public int Consume(float deltaTime)
        {
            _carry += deltaTime * BunniRules.FramesPerSecond;
            int frames = Mathf.Min(Mathf.FloorToInt(_carry), 30);
            _carry -= Mathf.FloorToInt(_carry);
            return frames;
        }
    }
}
