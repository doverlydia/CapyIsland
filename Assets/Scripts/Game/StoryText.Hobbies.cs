namespace CapyIsland.Game
{
    public static partial class StoryText
    {
        /// <summary>
        /// Hobby cards (Game/Hobbies/HobbyCards, UI/Hobbies/HobbyCardScreen): warm hearts earn a card
        /// that opens one closed hobby, and that hobby's own character says where to start. Hebrew in
        /// Resources/Lang/he/onboarding_hobbies.txt.
        /// </summary>
        public static class HobbyCardLines
        {
            /// <summary>The hobby's name, as the Hobbies hub writes it.</summary>
            public static string Name(Hobby h) => h switch
            {
                Hobby.Fishing => "Fishing", Hobby.Birding => "Birdwatching", Hobby.Bugs => "Bug Catching",
                Hobby.Gardening => "Gardening", Hobby.Cooking => "Cooking", Hobby.Cats => "Cat Care",
                Hobby.Writing => "Writing", Hobby.Sewing => "Sewing", _ => "",
            };

            // ------------------------------------------------ the choice

            public const string Title = "Hobby card";
            public const string EarnedOne = "Your warm hearts have earned you a hobby card!";
            public const string EarnedMany = "You have {0} hobby cards to use!";
            public const string Pick = "Choose a hobby to take up. The others will wait for your next card.";
            public const string PickFirst = "Choose your first hobby. Its gear is a gift: no need to buy it!";
            public const string Choose = "Choose a hobby";
            public const string Confirm = "Choose {0}";
            public const string Later = "Later";
            public const string NextAt = "Your next hobby card comes at {0} warm hearts.";
            public const string AllOpen = "Every hobby is open to you now. Enjoy them all!";

            // ------------------------------------------------ the Hobbies hub

            public const string HubOne = "You have a hobby card!";
            public const string HubMany = "You have {0} hobby cards!";
            public const string HubButton = "Choose a hobby";
            public const string UseCard = "Use my hobby card";
            public const string LockedChoose = "Choose it with your hobby card";
            public const string LockedFree = "Choose it with your hobby card: its gear is free!";

            // ------------------------------------------------ the first card's gift, in a bubble by you

            public const string GiftRod = "A fishing rod for you, on the house! It's in that bubble right beside you. Pop it, and let's see what's biting.";
            public const string GiftNet = "Bug catching! I've tucked a bug net into a bubble for you, right beside you. Pop it to take it.";
            public const string GiftBinoculars = "Birdwatching! Here, my old binoculars, in a bubble right beside you. Pop it to take them.";
            public const string LockedHearts = "Opens with a hobby card: {0} more warm hearts";
            public const string LockedHeart = "Opens with a hobby card: 1 more warm heart";
            public const string LockedLater = "Opens with a hobby card";

            // ------------------------------------------------ before a hobby is open

            /// <summary>Chef Tofu, before cooking is open.</summary>
            public const string TofuWaits = "Hello, hello! Chef Tofu, at your service. My kitchen isn't ready for a new cook just yet. Choose cooking with a hobby card, and I'll have a stove waiting for you!";

            /// <summary>Cooking's locked card while Sunflower Farm is still closed: a card can't open it yet.</summary>
            public const string CookingAfterFarm = "Opens with a hobby card once Sunflower Farm is open. Chef Tofu has his stall there.";

            /// <summary>Why the pantry and the recipes can't be bought yet.</summary>
            public const string CookingClosed = "Choose cooking with a hobby card first";

            /// <summary>Old Mochi, before writing is open: the last page of his hello.</summary>
            public const string MochiWaits = "Folk used to come up here to hang an ema: a little wooden plaque with a wish on it. When you'd like to take up writing, choose it with a hobby card, and I'll show you how.";

            /// <summary>Tapping the rack's "hang an ema" before writing is open.</summary>
            public const string WritingClosed = "Choose writing with a hobby card first, then Old Mochi will show you how to hang an ema.";

            // ------------------------------------------------ a hobby opens: its character, on where to start

            public const string Cats = "Cats! Horaira would be so pleased. Strays will start wandering onto your island soon. Say hello, pet them, and share a fish if you have one. Trust takes a little time.";
            public const string Writing = "Ah, you'd like to write? Come up to my shrine on your first island and hang an ema on the rack. A wish, a thank-you, anything at all. One gold coin for the gods.";
            public const string Cooking = "A new cook! Come and see me at my stall on Sunflower Farm. I have a stove with your name on it, and recipes whenever you're ready.";
            public const string Fishing = "Ahoy! My rod's laid out on the sand on the beach. Buy it, stand where the fish shadows swim, and cast.";

            public const string BugsReady = "Bug catching! There's a Bug Net waiting for you at the Bronze Store. Then creep up on the bugs in the flowers and grass. Gently now!";
            public const string BugsLater = "Bug catching! Once you've built the Bronze Store, you'll find a Bug Net there. Then creep up on the bugs in the flowers and grass. Gently now!";
            public const string BirdingReady = "Birdwatching! The Silver Store has a pair of Binoculars for you. Birds land in the trees and on the shore. Watch them from a few steps away.";
            public const string BirdingLater = "Birdwatching! Once you've built the Silver Store, it will have a pair of Binoculars for you. Birds land in the trees and on the shore. Watch them from a few steps away.";
            public const string SewingReady = "Sewing? Oh, I love sewing! There's a Sewing Machine for you at the Steel Store. A hat needs cotton, so a little garden helps too.";
            public const string SewingLater = "Sewing? Oh, I love sewing! When you've built the Steel Store, you'll find a Sewing Machine there. A hat needs cotton, so a little garden helps too.";
            public const string GardeningReady = "Gardening! Go to the south end of Pebble Cove. The spirit there can raise the bridge to Sunflower Farm, where the fields are waiting for you.";
            public const string GardeningLater = "Gardening! First raise the bridge to Pebble Cove. At its south end, a spirit can raise another bridge, to Sunflower Farm, where the fields are waiting for you.";
        }
    }
}
