namespace CapyIsland.Game
{
    public static partial class StoryText
    {
        /// <summary>
        /// The end of the opening: the old hot spring, frozen since the Big Chill, a fire fed with
        /// wood to thaw it, and the first soak. Also Miso's start-up gift (a new game starts with
        /// 100 wood and 100 gold) and the zoom beat while the first capybara works.
        /// </summary>
        public static class Spring
        {
            // ---- the goal card
            public const string GoalFind = "Find the hot spring and tap it";
            /// <summary>{0} wood given so far, {1} the fire needs.</summary>
            public const string GoalFeed = "Bring wood for the fire: {0}/{1}";
            public const string GoalFeedNote = "Your capybara keeps chopping at the sawmill. Tap the frozen spring to give the wood.";
            public const string GoalSoak = "Tap the hot spring and hop in";

            // ---- Old Yuzu
            public const string FindIt = "There's the Steam Tower, and my old hot spring at its foot. Tap the spring!";

            /// <summary>The first tap on the frozen spring.</summary>
            public static readonly string[] Frozen =
            {
                "Oh, no... it's frozen solid. The snow melted long ago, but the Big Chill left my spring full of ice.",
                "It needs a fire to thaw it, and a fire needs wood. Your capybara has been chopping: let's feed the fire!",
            };

            /// <summary>Tapped from across the water, before you have walked over.</summary>
            public const string FrozenFromAfar = "That's my old hot spring, frozen solid. Cross the bridge and we'll have a closer look.";

            // ---- the fire (a page with a Give button: all the wood at once)
            /// <summary>{0} the wood the fire still needs.</summary>
            public const string FireNeeds = "The fire for the spring needs {0} wood.";
            public const string FireHave = "You have {0} wood.";
            public const string FireShops = "Wood is usually spent at the shops, but this fire is worth every log.";
            /// <summary>Under the page while you're short.</summary>
            public const string FireShort = "Not enough yet? Another capybara at the sawmill chops wood faster.";
            public const string Give = "Give {0} wood";
            public const string NotEnough = "Not enough wood";
            public const string NotNow = "Not now";

            /// <summary>{0} the wood the fire still needs.</summary>
            public const string Waiting = "When you have {0} wood, bring it to the spring. More capybaras at the sawmill chop it faster.";
            public const string Bring = "You have enough wood for the fire. Tap the frozen spring to give it.";
            public const string Catching = "That's enough! The fire's catching... watch the ice!";

            public const string Thawed = "Listen... it's bubbling! My spring is warm again, for the first time since the Big Chill.";
            public const string Soak = "The spring is warm again! Tap it and hop in for a soak.";

            // ---- the zoom beat
            public const string ZoomDone = "Nicely done! Zoom whenever you like, to see more of your island.";

            // ---- Miso, as she sets out the General Store
            public const string MisoGift = "Oh, and take these! It's everything I gathered myself while the snow melted, to help you start. Wood and gold are for the shops, so spend them well!";
        }
    }
}
