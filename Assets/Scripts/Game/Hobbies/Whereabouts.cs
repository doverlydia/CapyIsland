using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Where and when to find what you haven't found yet, for the greyed tiles in the collection and
    /// the hobby journals. Islands are named the way the island map names them, so the map answers
    /// "where's that?".
    ///
    /// One fact a line, each in English: the line is translated as it is drawn, on its own.
    /// </summary>
    public static class Whereabouts
    {
        public static string Fish(FishKind f) =>
            Lines("From any shore", When(f.Times, f.RainOnly), Level(Hobby.Fishing, "Needs fishing level {0}", f.MinLevel));

        public static string Bird(BirdKind b) =>
            Lines(b.Wader ? "On rocks by the shore" : "Up in the trees", When(b.Times, b.RainOnly),
                Level(Hobby.Birding, "Needs birdwatching level {0}", b.MinLevel));

        public static string Bug(BugKind b) =>
            Lines(b.Home switch { BugHome.Flower => "On flower gardens", BugHome.Tree => "In the trees", _ => "In the grass" },
                When(b.Times, b.RainOnly), Level(Hobby.Bugs, "Needs bug catching level {0}", b.MinLevel));

        public static string Crop(CropKind c) =>
            Lines("Grow it in a vegetable plot", Level(Hobby.Gardening, "Needs gardening level {0}", c.MinLevel));

        public static string Dish(DishKind d) =>
            Lines("Cook it at your stove", Level(Hobby.Cooking, "Needs cooking level {0}", d.MinLevel));

        public const string Cat = "Wanders onto your island\nsome days";

        /// <summary>A discovery card: the shop that sells its item, or how else it turns up.</summary>
        public static string Card(CardDef card)
        {
            foreach (var kv in StoreData.Lists)
            {
                if (!Objectives.ShopBuilding.TryGetValue(kv.Key, out var shop)) continue;
                foreach (var item in card.Items)
                    if (System.Array.IndexOf(kv.Value, item) >= 0) return "Sold at the " + StoryText.Name(shop);
            }
            string first = card.Items.Length > 0 ? card.Items[0] : "";
            return first switch
            {
                "Capybara" or "Fox" or "Panda" => "Moves in when it has a home",
                "Monster" => "Warm up a zombie capybara",
                // Waiting on the islands as their bridges open: named as the map names them.
                "Pear Tree" => string.Format("Found on {0}", StoryText.Name("Verdant Bay")),
                "Monster House" => string.Format("Found on {0}", StoryText.Name("Pirate Cove")),   // with the zombies (MissionSetUps)
                "Coriander" or "Pirate Peach" or "Pirate Pete" or "Tower Of Love" or "General Store" or "Church" => "Comes with the story",
                _ => "Keep playing",
            };
        }

        static string When(DayParts times, bool rain) => rain ? "Only in the rain" : GameClock.Describe(times);

        /// <summary>The level it needs, only while you are short of it.</summary>
        static string Level(Hobby h, string template, int level) =>
            level > HobbyLog.Level(h) ? string.Format(template, level) : null;

        static string Lines(params string[] parts)
        {
            var kept = new List<string>();
            foreach (var p in parts) if (!string.IsNullOrEmpty(p)) kept.Add(p);
            return string.Join("\n", kept);
        }
    }
}
