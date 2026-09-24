namespace CapyIsland.Game
{
    public enum QuestSource { Ghost, Coriander, Peach }

    /// <summary>A mote: a named, one-shot achievement that is also a heart on the map.</summary>
    public readonly struct MoteDef
    {
        public readonly string Name;
        public readonly int Id;
        /// <summary>What triggers call it by.</summary>
        public readonly string Func;
        public readonly string Description;

        public MoteDef(string name, int id, string func, string description)
        {
            Name = name; Id = id; Func = func; Description = description;
        }
    }

    /// <summary>A first-discovery card, shown the first time you get any of its items.</summary>
    public readonly struct CardDef
    {
        public readonly string Name;
        public readonly int Id;
        public readonly string Description;
        public readonly string[] Items;

        public CardDef(string name, int id, string description, string[] items)
        {
            Name = name; Id = id; Description = description; Items = items;
        }
    }

    /// <summary>One quest from a ghost, Coriander or Pirate Peach.</summary>
    public readonly struct QuestDef
    {
        public readonly QuestSource Giver;
        public readonly int Id;
        public readonly string Intro, During, Complete;
        /// <summary>Ghosts only: the mote whose firing makes the ghost appear.</summary>
        public readonly string Trigger;
        /// <summary>Action run when the intro has been read.</summary>
        public readonly string SetUp;
        /// <summary>The mote name that completes the quest.</summary>
        public readonly string CompleteMote;
        public readonly string RewardItem;
        public readonly int Wood, Gold, Gems;

        public QuestDef(QuestSource giver, int id, string intro, string during, string complete,
            string trigger, string setUp, string completeMote, string rewardItem, int wood, int gold, int gems)
        {
            Giver = giver; Id = id; Intro = intro; During = during; Complete = complete;
            Trigger = trigger; SetUp = setUp; CompleteMote = completeMote; RewardItem = rewardItem;
            Wood = wood; Gold = gold; Gems = gems;
        }

        /// <summary>A text split into the pages the original shows one at a time.</summary>
        public static string[] Pages(string text) =>
            text.Split(new[] { "[br]" }, System.StringSplitOptions.RemoveEmptyEntries);
    }

    public static partial class QuestData { }
}
