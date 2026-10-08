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
    }

    public enum BugHome { Flower, Tree, Ground }

    public class BugKind
    {
        public string Id, Name;
        public int Stars, MinLevel;
        public BugHome Home;
        public bool Flies, Glows;
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
            new() { Id = "CarrotSoup",     Name = "Carrot Soup",     MinLevel = 1, Needs = new[] { ("crop:Carrot", 3) },
                    Blurb = "Smooth, warm and orange. Fills a hungry worker right up." },
            new() { Id = "GrilledFish",    Name = "Grilled Fish",    MinLevel = 1, Needs = new[] { ("fish:any", 2) },
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
        };

        public static readonly BugKind[] Bugs =
        {
            new() { Id = "Ladybug",      Name = "Ladybug",        Stars = 1, MinLevel = 1, Home = BugHome.Flower },
            new() { Id = "Butterfly",    Name = "Sulphur Butterfly", Stars = 1, MinLevel = 1, Home = BugHome.Flower, Flies = true },
            new() { Id = "Snail",        Name = "Garden Snail",   Stars = 1, MinLevel = 1, Home = BugHome.Ground },
            new() { Id = "Bee",          Name = "Bumblebee",      Stars = 2, MinLevel = 2, Home = BugHome.Flower, Flies = true },
            new() { Id = "Beetle",       Name = "Jewel Beetle",   Stars = 2, MinLevel = 2, Home = BugHome.Tree },
            new() { Id = "Silkworm",     Name = "Silkworm",       Stars = 2, MinLevel = 2, Home = BugHome.Tree },   // spins silk for capes
            new() { Id = "Dragonfly",    Name = "Dragonfly",      Stars = 2, MinLevel = 3, Home = BugHome.Ground, Flies = true },
            new() { Id = "BlueButterfly",Name = "Sky Blue Butterfly", Stars = 3, MinLevel = 4, Home = BugHome.Flower, Flies = true },
            new() { Id = "Firefly",      Name = "Firefly",        Stars = 3, MinLevel = 5, Home = BugHome.Tree, Flies = true, Glows = true },
            new() { Id = "StagBeetle",   Name = "Stag Beetle",    Stars = 4, MinLevel = 6, Home = BugHome.Tree },
            new() { Id = "RainbowMoth",  Name = "Rainbow Moth",   Stars = 5, MinLevel = 8, Home = BugHome.Tree, Flies = true },
        };

        public static readonly CatKind[] Cats =
        {
            new() { Id = "Ginger",  Name = "Marmalade", Personality = "Loves naps in the sun and anyone holding a fish." },
            new() { Id = "Black",   Name = "Soot",      Personality = "Shy at first, then follows you everywhere." },
            new() { Id = "Calico",  Name = "Patches",   Personality = "Curious about every bubble on the island." },
            new() { Id = "Grey",    Name = "Pebble",    Personality = "A very serious cat. Secretly adores being petted." },
            new() { Id = "Siamese", Name = "Miso Jr.",  Personality = "Chatty. Has opinions about your fishing technique." },
            new() { Id = "White",   Name = "Snowball",  Personality = "Fluffy, gentle and fond of the hot spring." },
        };

        public static CropKind Crop(string id) => System.Array.Find(Crops, c => c.Id == id);
        public static DishKind Dish(string id) => System.Array.Find(Dishes, d => d.Id == id);
        public static BugKind Bug(string id) => System.Array.Find(Bugs, b => b.Id == id);
        public static CatKind CatById(string id) => System.Array.Find(Cats, c => c.Id == id);

        public static BugKind RollBug(int level, BugHome home) =>
            Roll(Bugs, b => b.MinLevel <= level && b.Home == home, b => b.Stars);

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
            if (item.StartsWith("dish:")) return Art("Dish_" + item.Substring(5));
            if (item == "mat:Silk") return Art("Bug_Silkworm");
            return null;
        }

        public static string ItemName(string item)
        {
            if (item == "fish:any") return "any fish";
            if (item.StartsWith("fish:")) return FishById(item.Substring(5))?.Name ?? "Fish";
            if (item.StartsWith("crop:")) return Crop(item.Substring(5))?.Name ?? "Crop";
            if (item == "dish:Bizarre") return "Bizarre Food";
            if (item == "mat:Silk") return "Silk";
            if (item.StartsWith("dish:")) return Dish(item.Substring(5))?.Name ?? "Dish";
            return item;
        }
    }
}
