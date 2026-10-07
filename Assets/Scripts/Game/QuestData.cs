namespace CapyIsland.Game
{
    public enum QuestSource { Ghost, Coriander, Peach }

    /// <summary>
    /// An achievement: one-shot, triggered by its func, known to quests and saves by its name. The
    /// name is an id; what the player reads is <see cref="Title"/> and <see cref="Description"/>.
    /// </summary>
    public readonly struct MoteDef
    {
        public readonly string Name;
        public readonly int Id;
        /// <summary>What triggers call it by.</summary>
        public readonly string Func;

        public MoteDef(string name, int id, string func)
        {
            Name = name; Id = id; Func = func;
        }

        public string Title => StoryText.AchievementTitle(Func, Name);
        public string Description => StoryText.AchievementBlurb(Func);
    }

    /// <summary>A first-discovery card, shown the first time you get any of its items.</summary>
    public readonly struct CardDef
    {
        public readonly int Id;
        public readonly string[] Items;

        public CardDef(int id, string[] items)
        {
            Id = id; Items = items;
        }

        public string Name => StoryText.CardTitle(Id);
        public string Description => StoryText.CardBlurb(Id);
    }

    /// <summary>One quest from a spirit, Miso or Captain Wasabi. Its words live in <see cref="StoryText"/>.</summary>
    public readonly struct QuestDef
    {
        public readonly QuestSource Giver;
        public readonly int Id;
        /// <summary>Spirits only: the achievement whose firing makes the spirit appear.</summary>
        public readonly string Trigger;
        /// <summary>Action run when the intro has been read.</summary>
        public readonly string SetUp;
        /// <summary>The achievement name that completes the quest.</summary>
        public readonly string CompleteMote;
        public readonly string RewardItem;
        public readonly int Wood, Gold, Gems;

        public QuestDef(QuestSource giver, int id,
            string trigger, string setUp, string completeMote, string rewardItem, int wood, int gold, int gems)
        {
            Giver = giver; Id = id;
            Trigger = trigger; SetUp = setUp; CompleteMote = completeMote; RewardItem = rewardItem;
            Wood = wood; Gold = gold; Gems = gems;
        }

        public string Intro => StoryText.Quest(Giver, Id).Intro;
        public string During => StoryText.Quest(Giver, Id).During;
        public string Complete => StoryText.Quest(Giver, Id).Complete;

        /// <summary>A text split into the pages shown one at a time.</summary>
        public static string[] Pages(string text) =>
            text.Split(new[] { "[br]" }, System.StringSplitOptions.RemoveEmptyEntries);
    }

    public static partial class QuestData { }
}
