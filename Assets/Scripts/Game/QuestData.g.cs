// Originally generated from the shipped balance data by Tools/data/gen_quest_data.mjs.
// Ids, triggers and rewards only: every word the player reads lives in StoryText.cs.

namespace CapyIsland.Game
{
    public static partial class QuestData
    {
        public static readonly MoteDef[] Motes =
        {
            new MoteDef("First Love", 1, "first_mote"),
            new MoteDef("Welcome Back", 2, "welcome_back"),
            new MoteDef("Fox Importer", 4, "fox1"),
            new MoteDef("Swedish Fox Party", 5, "fox5"),
            new MoteDef("Everyone loves Pandas", 6, "panda1"),
            new MoteDef("Silver Store", 7, "silver_store"),
            new MoteDef("Steel Store", 8, "steel_store"),
            new MoteDef("The Most Glorious Gold Store", 9, "gold_store"),
            new MoteDef("Build a church", 10, "built_church"),
            new MoteDef("Tulips for Coriander", 12, "girl_quest_1"),
            new MoteDef("The Girl's New Dress", 13, "girl_quest_2"),
            new MoteDef("Priate Tales", 14, "girl_quest_3"),
            new MoteDef("Red roses, Red lips", 15, "girl_quest_5"),
            new MoteDef("The Girl's New Necklace", 16, "girl_quest_6"),
            new MoteDef("Destroyer of Deer", 17, "girl_quest_8"),
            new MoteDef("Madly in Love", 18, "girl_quest_10"),
            new MoteDef("The wedding bells toll for thee", 19, "wedding_mote"),
            new MoteDef("Legendary Store", 22, "gem_store"),
            new MoteDef("Holy Statue", 23, "holy_statue"),
            new MoteDef("Bronze Store", 24, "bronze_store"),
            new MoteDef("Put that panda to work!", 26, "panda_sawmill"),
            new MoteDef("Plum orchard", 27, "5_plum"),
            new MoteDef("Hired monsters", 28, "monster_tame"),
            new MoteDef("Dancing Queen", 29, "peach_dance"),
            new MoteDef("The Girl's Tiara", 30, "girl_crown"),
            new MoteDef("You Placed a Mine", 31, "place_mine"),
            new MoteDef("Visit Forums", 35, "visit_forums"),
        };

        public static readonly CardDef[] Cards =
        {
            new CardDef(1, new[] { "General Store" }),
            new CardDef(2, new[] { "Bronze Store" }),
            new CardDef(3, new[] { "Steel Store" }),
            new CardDef(4, new[] { "Silver Store" }),
            new CardDef(5, new[] { "Gold Store" }),
            new CardDef(7, new[] { "Small Gray Hill", "Medium Gray Hill", "Large Gray Hill" }),
            new CardDef(8, new[] { "Small Black Hill", "Medium Black Hill", "Large Black Hill" }),
            new CardDef(9, new[] { "Small Gold Hill", "Medium Gold Hill", "Large Gold Hill" }),
            new CardDef(10, new[] { "Small Pine Forest", "Medium Pine Forest", "Large Pine Forest" }),
            new CardDef(11, new[] { "Small Maple Forest", "Medium Maple Forest", "Large Maple Forest" }),
            new CardDef(12, new[] { "Small Ironwood Forest", "Medium Ironwood Forest", "Large Ironwood Forest" }),
            new CardDef(13, new[] { "Sawmill" }),
            new CardDef(14, new[] { "Legendary Sawmill" }),
            new CardDef(15, new[] { "Mine" }),
            new CardDef(16, new[] { "Legendary Mine" }),
            new CardDef(18, new[] { "Capybara" }),
            new CardDef(19, new[] { "Panda" }),
            new CardDef(20, new[] { "Fox" }),
            new CardDef(22, new[] { "Monster" }),
            new CardDef(23, new[] { "Small Posie Garden", "Medium Posie Garden", "Large Posie Garden" }),
            new CardDef(24, new[] { "Small Tulip Garden", "Medium Tulip Garden", "Large Tulip Garden" }),
            new CardDef(25, new[] { "Small Rose Garden", "Medium Rose Garden", "Large Rose Garden" }),
            new CardDef(26, new[] { "Coriander" }),
            new CardDef(27, new[] { "Pirate Peach" }),
            new CardDef(28, new[] { "Pirate Pete" }),
            new CardDef(29, new[] { "Church" }),
            new CardDef(30, new[] { "Tower Of Love" }),
            new CardDef(31, new[] { "Pink Flower Tree" }),
            new CardDef(32, new[] { "White Flower Tree" }),
            new CardDef(33, new[] { "Holy Statue" }),
            new CardDef(34, new[] { "Gem Statue" }),
            new CardDef(35, new[] { "Fox House" }),
            new CardDef(36, new[] { "Monster House" }),
            new CardDef(37, new[] { "Deer House" }),
            new CardDef(38, new[] { "Pirate House" }),
            new CardDef(39, new[] { "Capy House" }),
            new CardDef(40, new[] { "Panda House" }),
            new CardDef(41, new[] { "Apple Tree" }),
            new CardDef(42, new[] { "Pear Tree" }),
            new CardDef(43, new[] { "Legendary Plum Tree" }),
        };

        public static readonly QuestDef[] Ghosts =
        {
            new QuestDef(QuestSource.Ghost, 1,
                trigger: "Bronze Store", setUp: "second_island", completeMote: "Fox Importer",
                rewardItem: "Large Posie Garden", wood: 0, gold: 0, gems: 0),
            new QuestDef(QuestSource.Ghost, 3,
                trigger: "Fox Importer", setUp: "", completeMote: "Swedish Fox Party",
                rewardItem: "Medium Ironwood Forest", wood: 0, gold: 0, gems: 0),
            new QuestDef(QuestSource.Ghost, 8,
                trigger: "Legendary Store", setUp: "", completeMote: "Holy Statue",
                rewardItem: "", wood: 0, gold: 0, gems: 0),
        };

        /// <summary>Miso's missions, in the order she gives them.</summary>
        public static readonly QuestDef[] Coriander =
        {
            new QuestDef(QuestSource.Coriander, 16,
                trigger: "", setUp: "create_store", completeMote: "Bronze Store",
                rewardItem: "", wood: 0, gold: 0, gems: 0),
            new QuestDef(QuestSource.Coriander, 1,
                trigger: "", setUp: "create_pete", completeMote: "Priate Tales",
                rewardItem: "Small Ironwood Forest", wood: 0, gold: 0, gems: 0),
            new QuestDef(QuestSource.Coriander, 2,
                trigger: "", setUp: "", completeMote: "Silver Store",
                rewardItem: "", wood: 0, gold: 1000, gems: 0),
            new QuestDef(QuestSource.Coriander, 8,
                trigger: "", setUp: "dress_store", completeMote: "The Girl's New Dress",
                rewardItem: "", wood: 0, gold: 0, gems: 0),
            new QuestDef(QuestSource.Coriander, 5,
                trigger: "", setUp: "create_deer", completeMote: "Destroyer of Deer",
                rewardItem: "", wood: 0, gold: 0, gems: 10),
            new QuestDef(QuestSource.Coriander, 4,
                trigger: "", setUp: "", completeMote: "Steel Store",
                rewardItem: "", wood: 1000, gold: 0, gems: 0),
            new QuestDef(QuestSource.Coriander, 9,
                trigger: "", setUp: "pearl_store", completeMote: "The Girl's New Necklace",
                rewardItem: "", wood: 0, gold: 0, gems: 0),
            new QuestDef(QuestSource.Coriander, 6,
                trigger: "", setUp: "create_wedding", completeMote: "The Most Glorious Gold Store",
                rewardItem: "", wood: 1500, gold: 0, gems: 0),
            new QuestDef(QuestSource.Coriander, 10,
                trigger: "", setUp: "crown_store", completeMote: "The Girl's Tiara",
                rewardItem: "", wood: 0, gold: 0, gems: 0),
            new QuestDef(QuestSource.Coriander, 7,
                trigger: "", setUp: "create_priest", completeMote: "Madly in Love",
                rewardItem: "", wood: 0, gold: 0, gems: 15),
        };

        /// <summary>Captain Wasabi's missions, in the order she gives them.</summary>
        public static readonly QuestDef[] Peach =
        {
            new QuestDef(QuestSource.Peach, 5,
                trigger: "", setUp: "", completeMote: "Red roses, Red lips",
                rewardItem: "Large Maple Forest", wood: 0, gold: 0, gems: 0),
            new QuestDef(QuestSource.Peach, 6,
                trigger: "", setUp: "", completeMote: "Legendary Store",
                rewardItem: "", wood: 0, gold: 2000, gems: 0),
            new QuestDef(QuestSource.Peach, 2,
                trigger: "", setUp: "", completeMote: "Plum orchard",
                rewardItem: "Large Ironwood Forest", wood: 0, gold: 0, gems: 0),
            new QuestDef(QuestSource.Peach, 1,
                trigger: "", setUp: "create_panda", completeMote: "Put that panda to work!",
                rewardItem: "", wood: 0, gold: 100, gems: 0),
            new QuestDef(QuestSource.Peach, 3,
                trigger: "", setUp: "create_monsters", completeMote: "Hired monsters",
                rewardItem: "", wood: 2000, gold: 0, gems: 0),
            new QuestDef(QuestSource.Peach, 4,
                trigger: "", setUp: "", completeMote: "Dancing Queen",
                rewardItem: "", wood: 0, gold: 0, gems: 0),
        };
    }
}
