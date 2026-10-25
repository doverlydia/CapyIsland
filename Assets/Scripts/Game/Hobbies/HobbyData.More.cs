using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    public class CropKind
    {
        public string Id, Name;
        public float Minutes;         // real minutes from seed to harvest
        public int SeedGold, Yield, MinLevel;
    }

    public class DishKind
    {
        public string Id, Name, Blurb;
        public (string item, int count)[] Needs;
        public int MinLevel;
        /// <summary>The hobby whose taking-up gives this recipe away, or null: Chef Tofu sells it.</summary>
        public Hobby? FreeWith;

        /// <summary>What Chef Tofu asks for the recipe: dearer as the dish gets fancier.</summary>
        public int RecipeGold => 30 + MinLevel * 30;

        /// <summary>
        /// How hard it is to cook well, 1 easy … 4 master: the fancier the dish, the quicker the
        /// spatula runs out, the sooner the next comes, and from 2 up, now and then two at once
        /// (a finger each) or one to tap fast three or four times (CookingScreen).
        /// </summary>
        public int Difficulty => MinLevel <= 2 ? 1 : MinLevel <= 4 ? 2 : MinLevel <= 6 ? 3 : 4;

        /// <summary>The difficulty in words (the fonts have no star glyph).</summary>
        public static string DifficultyName(int d) => d switch { 1 => "Easy", 2 => "Tricky", 3 => "Hard", _ => "Master chef" };
    }

    /// <summary>What you can't grow or catch: Chef Tofu sells it in small packs, straight into the bag.</summary>
    public class PantryKind
    {
        public string Id, Name, Blurb;
        public int Pack, Gold;
        public string Item => "pantry:" + Id;
    }

    public enum BugHome { Flower, Tree, Ground }

    public class BugKind
    {
        public string Id, Name;
        public int Stars, MinLevel;
        public BugHome Home;
        public bool Flies, Glows;
        public DayParts Times = DayParts.Any;
        public bool RainOnly;
    }

    public class CatKind
    {
        public string Id, Name, Personality;
    }

    /// <summary>Crops, dishes, bugs and cats — and the art for them, loaded from Resources/Hobbies.</summary>
    public static partial class HobbyData
    {
        public static readonly CropKind[] Crops =
        {
            new() { Id = "Carrot",     Name = "Carrot",     Minutes = 2f,  SeedGold = 5,  Yield = 3, MinLevel = 1 },
            new() { Id = "Cabbage",    Name = "Cabbage",    Minutes = 3f,  SeedGold = 8,  Yield = 2, MinLevel = 1 },
            new() { Id = "Cotton",     Name = "Cotton",     Minutes = 5f,  SeedGold = 12, Yield = 3, MinLevel = 2 },   // for sewing hats
            new() { Id = "Tomato",     Name = "Tomato",     Minutes = 4f,  SeedGold = 10, Yield = 3, MinLevel = 2 },
            new() { Id = "Corn",       Name = "Corn",       Minutes = 6f,  SeedGold = 14, Yield = 2, MinLevel = 3 },
            new() { Id = "Strawberry", Name = "Strawberry", Minutes = 8f,  SeedGold = 20, Yield = 4, MinLevel = 4 },
            new() { Id = "Pumpkin",    Name = "Pumpkin",    Minutes = 12f, SeedGold = 30, Yield = 1, MinLevel = 5 },
        };

        public static readonly DishKind[] Dishes =
        {
            new() { Id = "CarrotSoup",     Name = "Carrot Soup",     MinLevel = 1, Needs = new[] { ("crop:Carrot", 3) }, FreeWith = Hobby.Gardening,
                    Blurb = "Smooth, warm and orange. Fills a hungry worker right up." },
            new() { Id = "GrilledFish",    Name = "Grilled Fish",    MinLevel = 1, Needs = new[] { ("fish:any", 2) }, FreeWith = Hobby.Fishing,
                    Blurb = "Crispy skin, a squeeze of lemon. A fisher's favourite." },
            new() { Id = "TomatoSalad",    Name = "Tomato Salad",    MinLevel = 2, Needs = new[] { ("crop:Tomato", 2), ("crop:Cabbage", 1) },
                    Blurb = "Fresh and crunchy, straight from the garden." },
            new() { Id = "VeggieStew",     Name = "Veggie Stew",     MinLevel = 3, Needs = new[] { ("crop:Carrot", 2), ("crop:Tomato", 1), ("crop:Cabbage", 1) },
                    Blurb = "A whole garden in one pot." },
            new() { Id = "CornChowder",    Name = "Corn Chowder",    MinLevel = 4, Needs = new[] { ("crop:Corn", 2), ("fish:any", 1) },
                    Blurb = "Creamy, sweet and very filling." },
            new() { Id = "StrawberryMochi",Name = "Strawberry Mochi",MinLevel = 5, Needs = new[] { ("crop:Strawberry", 3) },
                    Blurb = "Soft, sweet and impossible to eat just one." },
            new() { Id = "HotPot",         Name = "Yuzu Hot Pot",    MinLevel = 6, Needs = new[] { ("fish:any", 2), ("crop:Cabbage", 2), ("crop:Carrot", 1) },
                    Blurb = "Bubbling broth with a slice of yuzu. Warms you to the toes." },
            new() { Id = "PumpkinPie",     Name = "Pumpkin Pie",     MinLevel = 7, Needs = new[] { ("crop:Pumpkin", 1), ("crop:Strawberry", 2) },
                    Blurb = "The pride of any island kitchen." },
            // With Chef Tofu's pantry.
            new() { Id = "Omelette",       Name = "Fluffy Omelette", MinLevel = 1, Needs = new[] { ("pantry:Egg", 2), ("crop:Cabbage", 1) },
                    Blurb = "Rolled up soft and golden, with a little crunch of cabbage." },
            new() { Id = "Onigiri",        Name = "Onigiri",         MinLevel = 1, Needs = new[] { ("pantry:Rice", 2), ("fish:any", 1) },
                    Blurb = "A rice ball with a fishy surprise inside. Perfect for a walk." },
            new() { Id = "Pancakes",       Name = "Pancakes",        MinLevel = 2, Needs = new[] { ("pantry:Flour", 1), ("pantry:Egg", 1), ("pantry:Milk", 1) },
                    Blurb = "A fluffy stack with butter melting down the sides." },
            new() { Id = "Tempura",        Name = "Fish Tempura",    MinLevel = 3, Needs = new[] { ("fish:any", 2), ("pantry:Flour", 1), ("pantry:Egg", 1) },
                    Blurb = "Light, crispy batter. Gone the moment it leaves the pan." },
            new() { Id = "Milkshake",      Name = "Strawberry Milkshake", MinLevel = 4, Needs = new[] { ("crop:Strawberry", 2), ("pantry:Milk", 1), ("pantry:Sugar", 1) },
                    Blurb = "Pink, cold and frothy. Comes with a moustache." },
            new() { Id = "CornBread",      Name = "Corn Bread",      MinLevel = 4, Needs = new[] { ("crop:Corn", 2), ("pantry:Flour", 1), ("pantry:Egg", 1) },
                    Blurb = "Golden squares, warm from the tin." },
            new() { Id = "Pudding",        Name = "Custard Pudding", MinLevel = 5, Needs = new[] { ("pantry:Milk", 2), ("pantry:Egg", 1), ("pantry:Sugar", 1) },
                    Blurb = "It wobbles. Then it's gone." },
        };

        public static readonly PantryKind[] Pantry =
        {
            new() { Id = "Egg",   Name = "Eggs",  Pack = 3, Gold = 12, Blurb = "Three fresh eggs, for omelettes, pancakes and baking." },
            new() { Id = "Milk",  Name = "Milk",  Pack = 2, Gold = 12, Blurb = "Two bottles of creamy milk, for pancakes, shakes and pudding." },
            new() { Id = "Flour", Name = "Flour", Pack = 3, Gold = 10, Blurb = "Three scoops of flour, for batter, bread and anything fluffy." },
            new() { Id = "Rice",  Name = "Rice",  Pack = 3, Gold = 10, Blurb = "Three bowls of sticky rice, for rice balls." },
            new() { Id = "Sugar", Name = "Sugar", Pack = 3, Gold = 8,  Blurb = "Three spoons of sugar: a little sweetness for desserts." },
        };

        public static PantryKind PantryById(string id) => System.Array.Find(Pantry, p => p.Id == id);

        public static readonly BugKind[] Bugs =
        {
            new() { Id = "Ladybug",      Name = "Ladybug",        Stars = 1, MinLevel = 1, Home = BugHome.Flower, Times = DayParts.Daylight },
            new() { Id = "Butterfly",    Name = "Sulphur Butterfly", Stars = 1, MinLevel = 1, Home = BugHome.Flower, Flies = true, Times = DayParts.Day },
            new() { Id = "Snail",        Name = "Garden Snail",   Stars = 1, MinLevel = 1, Home = BugHome.Ground, Times = DayParts.Night | DayParts.Dawn },
            new() { Id = "Bee",          Name = "Bumblebee",      Stars = 2, MinLevel = 2, Home = BugHome.Flower, Flies = true, Times = DayParts.Day },
            new() { Id = "Beetle",       Name = "Jewel Beetle",   Stars = 2, MinLevel = 2, Home = BugHome.Tree, Times = DayParts.Day | DayParts.Dusk },
            new() { Id = "Silkworm",     Name = "Silkworm",       Stars = 2, MinLevel = 2, Home = BugHome.Tree },   // spins silk for fedoras and sun hats
            new() { Id = "Dragonfly",    Name = "Dragonfly",      Stars = 2, MinLevel = 3, Home = BugHome.Ground, Flies = true, Times = DayParts.Day | DayParts.Dusk },
            new() { Id = "BlueButterfly",Name = "Sky Blue Butterfly", Stars = 3, MinLevel = 4, Home = BugHome.Flower, Flies = true, Times = DayParts.Day },
            new() { Id = "Firefly",      Name = "Firefly",        Stars = 3, MinLevel = 5, Home = BugHome.Tree, Flies = true, Glows = true, Times = DayParts.Night },
            new() { Id = "StagBeetle",   Name = "Stag Beetle",    Stars = 4, MinLevel = 6, Home = BugHome.Tree, Times = DayParts.Night },
            new() { Id = "RainFrog",     Name = "Rain Frog",      Stars = 2, MinLevel = 2, Home = BugHome.Ground, RainOnly = true },
            new() { Id = "RainbowMoth",  Name = "Rainbow Moth",   Stars = 5, MinLevel = 8, Home = BugHome.Tree, Flies = true, Times = DayParts.Night | DayParts.Dusk },
        };

        public static readonly CatKind[] Cats =
        {
            new() { Id = "Ginger",  Name = "Marmalade", Personality = "Loves naps in the sun and anyone holding a fish." },
            new() { Id = "Black",   Name = "Soot",      Personality = "Shy at first, then follows you everywhere." },
            new() { Id = "Calico",  Name = "Patches",   Personality = "Curious about every bubble on the island." },
            new() { Id = "Grey",    Name = "Pebble",    Personality = "A very serious cat. Secretly adores being petted." },
            new() { Id = "Siamese", Name = "Miso Jr.",  Personality = "Chatty. Has opinions about your fishing technique." },
            new() { Id = "White",   Name = "Snowball",  Personality = "Fluffy, gentle and fond of the hot spring." },
            // Old Yuzu's cat: never a stray, he shows himself once she is warm again (Horaira).
            new() { Id = Horaira.Id, Name = StoryText.Yuzu.HorairaName, Personality = "Old Yuzu's cat. Gentle, a little bit glowy, and never far from her side." },
        };

        public static CropKind Crop(string id) => System.Array.Find(Crops, c => c.Id == id);
        public static DishKind Dish(string id) => System.Array.Find(Dishes, d => d.Id == id);
        public static BugKind Bug(string id) => System.Array.Find(Bugs, b => b.Id == id);
        public static CatKind CatById(string id) => System.Array.Find(Cats, c => c.Id == id);

        public static BugKind RollBug(int level, BugHome home) =>
            Roll(Bugs, b => b.MinLevel <= level && b.Home == home && GameClock.Allows(b.Times) && (!b.RainOnly || Weather.IsRaining), b => b.Stars);

        // ---------------------------------------------------------------- art

        static readonly Dictionary<string, Sprite> _art = new();

        /// <summary>A hobby picture by name ("Crop_Carrot", "Cat_Ginger", "Plot"…), or null if not drawn yet.</summary>
        public static Sprite Art(string name)
        {
            if (_art.TryGetValue(name, out var s)) return s;
            s = Resources.Load<Sprite>("Hobbies/" + name);
            _art[name] = s;
            return s;
        }

        /// <summary>The picture for something in the bag.</summary>
        public static Sprite ItemArt(string item)
        {
            if (item.StartsWith("fish:")) { var f = FishById(item.Substring(5)); return f != null ? FishPicture(f) : null; }
            if (item.StartsWith("crop:")) return Art("Crop_" + item.Substring(5));
            if (item.StartsWith("pantry:")) return Art("Pantry_" + item.Substring(7));
            if (item.StartsWith("dish:")) return Art("Dish_" + item.Substring(5));
            if (item == "mat:Silk") return Art("Bug_Silkworm");
            if (item == Cooking.Roach) return Art("Gift_Roach");
            if (item == Baits.Roach.Item) return Art("Bait_Roach") ?? Art("Gift_Roach");
            if (item.StartsWith("bait:")) return Art("Bait_" + item.Substring(5));
            if (item == MermaidCharms.Item) return Art("Charm_Mermaid");
            return null;
        }

        public static string ItemName(string item)
        {
            if (item == "fish:any") return "any fish";
            if (item.StartsWith("fish:")) return FishById(item.Substring(5))?.Name ?? "Fish";
            if (item.StartsWith("crop:")) return Crop(item.Substring(5))?.Name ?? "Crop";
            if (item.StartsWith("pantry:")) return PantryById(item.Substring(7))?.Name ?? "Food";
            if (item == "dish:Bizarre") return "Bizarre Food";
            if (item == "mat:Silk") return "Silk";
            if (item == Cooking.Roach) return "Cockroach";
            if (item.StartsWith("bait:")) return Baits.ById(item.Substring(5))?.Name ?? "Bait";
            if (item == MermaidCharms.Item) return StoryText.Charm.Name;
            if (item.StartsWith("dish:")) return Dish(item.Substring(5))?.Name ?? "Dish";
            return item;
        }
    }
}
