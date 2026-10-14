using System.Collections.Generic;

namespace CapyIsland.Game
{
    /// <summary>
    /// Every word the player reads about the world: names, item blurbs, achievements, discovery
    /// cards, quests and small talk. Written for Capy Island — "The Big Chill":
    ///
    ///   The Capy Isles were warmed by a great hot spring under the Steam Tower. One long winter it
    ///   went cold. Gardens withered, shops shut, capybaras drifted away, and the ones who wandered
    ///   the fens too long turned into grumbly zombie capybaras. Before the cold took her, the
    ///   spring-keeper Old Yuzu sealed what she could into bubbles of warm air. You wash ashore
    ///   wearing a little crown. Every warm thing you do makes a warm heart that flies to the Steam
    ///   Tower, and when they are all home, the spring will flow again.
    ///
    /// Cast: Old Yuzu (the ghost who guides you), Miso (her granddaughter, who dreams of reopening
    /// the family bathhouse — and falls for you), Captain Wasabi (a pirate captain whose ship froze
    /// in the harbour), Salty Kombu (an old sailor and trader), Elder Matcha (keeper of the Blossom
    /// Chapel), and two ghosts with errands, Mister Kinako and Sister Hojicha.
    ///
    /// Internal ids — item names, achievement names — never change, because saves store them. This
    /// file maps each id to what the player sees.
    /// </summary>
    public static class StoryText
    {
        public const string Guide = "Old Yuzu";
        public const string Sweetheart = "Miso";
        public const string Captain = "Captain Wasabi";
        public const string Sailor = "Salty Kombu";
        public const string Elder = "Elder Matcha";
        /// <summary>The shrine keeper on the hill: Old Yuzu's husband, Miso's grandpa. Only his spirit is left.</summary>
        public const string Keeper = "Old Mochi";
        /// <summary>The cook by the hot spring: sells the stove, the pantry goods and recipes.</summary>
        public const string Chef = "Chef Tofu";

        /// <summary>The opening movie's narration, in order (UI/Cutscenes/IntroCutscene).</summary>
        public static readonly string[] Intro =
        {
            "Long ago, the Capy Isles were the warmest place in the whole sea.",
            "A great hot spring bubbled under the Steam Tower, and everyone came to soak.",
            "Then, one night, the Big Chill came.",
            "The spring froze. The gardens withered. One by one, the capybaras drifted away.",
            // Old Yuzu tells it herself: she is the ghost who has just woken and asked your name.
            "I was the keeper of the spring, so I gathered the last of my magic...",
            "...and sealed everything I could save into bubbles of warm air.",
            "But there was no bubble left for me.",
            "So I stayed by the frozen spring, day after day, waiting for the warmth to come back.",
            "I gathered wood and built a little fire, and kept it burning night after night.",
            "But the wind grew stronger, and I grew older... and one night, the fire went out.",
            "The warmth never came back. One quiet winter night, I lay down in the snow and closed my eyes...",
            "...and the last little warmth on the islands went out.",
            "But the spring wasn't finished with me. With its last warm breath, it caught my spirit in a bubble...",
            "...and there I slept, for years and years.",
            "Little by little, the Big Chill passed. The snow melted, and the islands turned green again... but there was no one left to see it.",
            "Far out at sea, little {0} drifted on and on...",
            "...until one morning, the tide brought {0} home.",
            "There on the beach, {0} found a bubble with a sleepy old ghost inside...",
            "Pop! Oh my... green grass and warm sun! The Big Chill is over!",
            "And you woke me, little {0}. Will you help me bring the islands back to life?",
        };

        public const string IntroTitle = "Capy Island", IntroSubtitle = "The Big Chill";
        public const string Spirit = "Spirit";

        /// <summary>
        /// The islanders' own ema on Old Mochi's rack: wishes, thanks and the odd complaint to the
        /// gods, the way people write them at a shrine. Short, because an ema is small.
        /// </summary>
        public static readonly (string title, string by, string text)[] IslandEma =
        {
            ("For the steam", Guide,
                "Please let the hot spring steam again.\n\nNot for me. I am a ghost, I don't feel the cold. For the little ones who have never seen it. And for one stubborn old keeper up on the hill."),
            ("A captain's wish", Captain,
                "Dear gods of the sea,\n\nPlease thaw my ship before spring. She is getting lonely.\n\nAlso please keep the seagulls away from my biscuits. All of them. Forever.\n\nYours, Captain W."),
            ("Thank you", Sailor,
                "Thank you for the pickles that washed up on Tuesday. Very good pickles.\n\nIf those were meant for someone else, I am sorry. I have eaten them."),
            ("A wish, folded very small", Sweetheart,
                "I wish the bathhouse could open again, full of yuzu like Grandma used to make it.\n\nAnd I wish to sit in the steam with someone kind, and not say anything at all for a very long time.\n\nI am not writing his name. The gods know who."),
            ("For my Yuzu", Keeper,
                "I swept the shrine again today. Every day, all these years.\n\nIf the spring wakes, let her see it. That is all I ask."),
        };

        /// <summary>Chef Tofu, the islands' cook before the Big Chill: a spirit still minding his stall by the spring.</summary>
        public static class Tofu
        {
            /// <summary>Before you can cook: who he is, and the stove.</summary>
            public static readonly string[] Meet =
            {
                "Hello, hello! Chef Tofu, at your service. I cooked for the whole of the isles before the Big Chill, and being a ghost was never going to keep me out of my kitchen.",
                "Every good kitchen starts with a stove, and mine are the best on the isles. Then come back for eggs, milk, flour, and all the recipes you can learn!",
            };

            public static readonly string[] Back =
            {
                "Back for more? Anything you can't grow or catch, I've got right here. And my recipes are all in my book!",
            };
        }

        /// <summary>Old Mochi, the shrine keeper's spirit.</summary>
        public static class Mochi
        {
            /// <summary>The first time you tap him: who he is.</summary>
            public static readonly string[] Meet =
            {
                "Oh! A visitor. Nobody has climbed up to my shrine in a very long time.",
                "I am Mochi. I kept this shrine before the Big Chill, and I suppose I am still keeping it. Old habits.",
                "Down by the spring there is a grumpy old ghost called Yuzu. She was my wife. Still is, I like to think. She keeps the spring, I keep the shrine.",
                "And Miso is our granddaughter. Don't tell her I said so, but she has her grandmother's stubbornness. Good. She'll need it.",
                "Now. Folk used to come here to hang an ema: a little wooden plaque with a wish on it, or a thank-you. Would you like to hang one? An offering of one gold coin, for the gods.",
            };

            /// <summary>While Old Yuzu is still showing you round.</summary>
            public const string Busy = "Ah, little one. Yuzu is showing you round, I see. Go on, go with her. My shrine will still be here.";

            public static readonly string[] Hello =
            {
                "Welcome back. The gods have been reading your ema, I'm sure of it.",
                "Every wish on that rack makes the shrine a little warmer. Even the silly ones. Especially the silly ones.",
                "Have you seen Yuzu today? Tell her I swept the steps. She always said I never swept the steps.",
                "Miso came by yesterday and left a wish. I didn't read it. ...I read it. It was very sweet.",
            };

            /// <summary>The lantern story (UI.LanternCutscene), the first time you tap a lantern on the sea.</summary>
            public const string Lantern1 = "Ah, you found the lanterns. And this one in my paws... this is Yuzu's.";
            public const string Lantern2 = "On our wedding night we floated it out together, to tell the spirits we'd look after these islands.";
            public const string Lantern3 = "Every year after, I lit it again for her. Then the Big Chill came, and the candle went out. Let's light it once more.";
            public const string Lantern4 = "Every warm heart you bring home sets another one adrift. Look at them all. Maybe one night she'll see them too.";

            /// <summary>Tapping Yuzu's lantern again, later.</summary>
            public const string LanternAgain = "Yuzu's wedding lantern, still burning. Old Mochi lights it every night.";

            public static string Visit(string host) =>
                $"Ah, a visitor from another island! You may hang an ema here for {host}. They'll find it on their rack.";
        }

        // ---------------------------------------------------------------- names

        static readonly Dictionary<string, string> Names = new()
        {
            { "Coriander", Sweetheart },
            { "Pirate Peach", Captain },
            { "Pirate Pete", Sailor },
            { "Priest", Guide },   // the chapel's officiant: Old Yuzu, once she is back
            { "Ghost", Spirit },

            { "Panda", "Woolly Capybara" },
            { "Fox", "Burrow Capybara" },
            { "Monster", "Zombie Capybara" },
            { "Deer", "Guinea Pig" },
            { "Pirate", "Pirate Capybara" },

            { "Panda House", "Woolly Lodge" },
            { "Fox House", "Burrow Hole" },
            { "Monster House", "Zombie Shack" },
            { "Garden Shop", "Garden Shop" },
            { "Vegetable Plot", "Vegetable Plot" },
            { "Stove", "Cooking Stove" },
            { "Fishing Rod", "Fishing Rod" },
            { "Bug Net", "Bug Net" },
            { "Binoculars", "Binoculars" },
            { "Sewing Machine", "Sewing Machine" },
            { "Deer House", "Guinea Pig Hutch" },
            { "Pirate House", "Pirate Shack" },

            { "Bomb", "Fizz Bomb" },
            { "Deer Bomb", "Shoo Bomb" },
            { "Church", "Blossom Chapel" },
            { "Holy Statue", "Spirit Statue" },
            { "Tower Of Love", "Steam Tower" },
            { "String of Pearls", "Pearl Necklace" },
            { "Monster Den", "Burrow Door" },
            { "Furniture Shop", "Furniture Shop" },
            { "Little Greeny", "Sprout" },
            { "Fall Color", "Autumn Tree" },
            { "Splash Fountain", "Bubbling Fountain" },
            { "White Flower Tree", "White Blossom Tree" },
            { "Pink Flower Tree", "Pink Blossom Tree" },

            { "Small Posie Garden", "Small Daisy Garden" },
            { "Medium Posie Garden", "Medium Daisy Garden" },
            { "Large Posie Garden", "Large Daisy Garden" },

            { "Capy Island", "Home Isle" },     // where you start (the game's own name stays the title)
            { "Verdant Bay", "Mossy Meadow" },
            { "Ginger Cove", "Pebble Cove" },
            { "Wedding Isle", "Blossom Isle" },
            { "Pirate Cove", "Barnacle Bay" },
            { "Monster Isle", "Gloomfen" },
            { "Farm Isle", "Sunflower Farm" },
        };

        /// <summary>The seas between the islands, as the map writes them (west channel, lagoon, strait, open sea).</summary>
        public static readonly string[] Seas = { "Kombu Channel", "Misty Lagoon", "Sakura Strait", "Lantern Sea" };

        /// <summary>What the player calls a thing, from its internal id.</summary>
        public static string Name(string id) =>
            string.IsNullOrEmpty(id) ? id : Names.TryGetValue(id, out var n) ? n : id;

        // ---------------------------------------------------------------- item blurbs

        static readonly Dictionary<string, string> Blurbs = new()
        {
            { "Small Posie Garden", "A little patch of daisies. Feeds one hungry capybara." },
            { "Medium Posie Garden", "A cheerful bed of daisies. Feeds two capybaras." },
            { "Large Posie Garden", "A whole meadow of daisies, enough for a busy crew." },
            { "Small Tulip Garden", "Bright tulips, twice as filling as daisies. Feeds two capybaras." },
            { "Medium Tulip Garden", "A bigger bed of tulips for a hungrier workforce." },
            { "Large Tulip Garden", "Rows and rows of tulips. Nobody goes to bed hungry." },
            { "Small Rose Garden", "Roses are the most filling flower of all, and they grow back fast. Feeds three capybaras." },
            { "Medium Rose Garden", "A fragrant rose garden for a large crew." },
            { "Large Rose Garden", "A grand rose garden. Your capybaras will dine like royalty." },

            { "Small Pine Forest", "A few pines. Each tree gives 3 wood." },
            { "Medium Pine Forest", "A stand of pines that spreads on its own. Each tree gives 3 wood." },
            { "Large Pine Forest", "A big pine wood. Each tree gives 3 wood." },
            { "Small Maple Forest", "Maples give 7 wood a tree and regrow faster than pines." },
            { "Medium Maple Forest", "A maple grove. 7 wood a tree." },
            { "Large Maple Forest", "A great maple wood, rustling with leaves. 7 wood a tree." },
            { "Small Ironwood Forest", "Hard as iron and quick to regrow. 15 wood a tree." },
            { "Medium Ironwood Forest", "A dark ironwood grove. 15 wood a tree." },
            { "Large Ironwood Forest", "A towering ironwood forest. 15 wood a tree, and it never stays bare for long." },

            { "Small Gray Hill", "A small rocky hill with bronze inside. 2 gold a rock." },
            { "Medium Gray Hill", "A rocky hill full of bronze. 2 gold a rock." },
            { "Large Gray Hill", "A big bronze hill that keeps a mine busy. 2 gold a rock." },
            { "Small Black Hill", "A dark little hill hiding silver. 7 gold a rock." },
            { "Medium Black Hill", "A silver hill, dark and glinting. 7 gold a rock." },
            { "Large Black Hill", "A great silver hill. 7 gold a rock." },
            { "Small Gold Hill", "A glittering hill with real gold inside. 20 gold a rock." },
            { "Medium Gold Hill", "A gold hill, and it grows back quickly. 20 gold a rock." },
            { "Large Gold Hill", "A mountain of gold. 20 gold a rock." },

            { "Capy House", "A snug home. Capybaras come to live here, ready to work." },
            { "Pirate House", "A driftwood shack with a flag on the roof, for pirate capybaras." },
            { "Panda House", "A warm lodge for woolly capybaras, who feel the cold the least." },
            { "Fox House", "A cosy hillside burrow for small, quick burrow capybaras." },
            { "Monster House", "A place for cured zombie capybaras to call home." },
            { "Garden Shop", "Seeds, soil and a stove: everything for growing and cooking your own food." },
            { "Vegetable Plot", "A little bed of rich soil. Plant seeds in it, and come back when they've grown." },
            { "Stove", "A cosy outdoor stove. Put ingredients in the pot and mind the heat!" },
            { "Fishing Rod", "Take up fishing: walk to the water's edge and cast." },
            { "Bug Net", "Take up bug catching: creep up on bugs and catch them." },
            { "Binoculars", "Take up birdwatching: spot the birds that visit your island." },
            { "Sewing Machine", "Take up sewing: stitch and paint your own hats." },
            { "Deer House", "A hutch for guinea pigs. Are you sure about this?" },

            { "Capybara", "Calm, kind and always ready to help. Put one to work at a sawmill or a mine." },
            { "Pirate", "A capybara with sea legs. Strong, if a little loud." },
            { "Panda", "A capybara in a fluffy winter coat. Waddles beautifully, works hard." },
            { "Fox", "A small, speedy capybara that loves to dig." },
            { "Monster", "A capybara who stayed out in the cold too long. A few gems for a warm bath cures it, and then it works for you." },
            { "Deer", "A guinea pig. Eats every flower it can find. You probably don't want one." },

            { "Sawmill", "Workers chop the trees in its circle and carry the wood here." },
            { "Mine", "Workers dig the rocks in its circle and bring back gold." },
            { "Legendary Sawmill", "A sawmill so grand the trees almost chop themselves." },
            { "Legendary Mine", "The deepest, shiniest mine on the isles." },

            { "General Store", "The first shop on the islands. Every store keeps the plans for the next." },
            { "Bronze Store", "Maple trees, tulips, burrow holes and shoo bombs." },
            { "Silver Store", "Wind chimes in the door and peppermint in the air." },
            { "Steel Store", "Sturdy supplies for serious builders." },
            { "Gold Store", "The fanciest shop on the isles, with gold letters over the door." },
            { "Legendary Gem Store", "Glittering things, bought with gems." },
            { "Fancy Store", "Decorations to make your island beautiful." },
            { "Bank", "Trade your gems for wood or gold." },
            { "Buy Gems", "Trade for gems." },
            { "Buy Wood", "Trade gems for wood." },
            { "Buy Gold", "Trade gems for gold." },

            { "Church", "A chapel under blossom that never falls. Weddings held here." },
            { "Tower Of Love", "It stands over the old hot spring. Every warm heart you find flies here." },
            { "Apple Tree", "Shake it for apples, and the odd surprise." },
            { "Pear Tree", "Shake it for pears and hidden treats." },
            { "Legendary Plum Tree", "Rare, sweet plums, and treasures tucked among the branches." },

            { "White Flower Tree", "Soft white blossom, like warm snow." },
            { "Pink Flower Tree", "Pink petals, all year round." },
            { "Holy Statue", "A stone capybara to watch over the springs. Every statue raises your score." },
            { "Gem Statue", "A statue that sparkles in the sun." },
            { "Bushy Tree", "A round, friendly tree." },
            { "Fall Color", "A tree that's always dressed for autumn." },
            { "Tiny Bush", "Small, green and cheerful." },
            { "Little Greeny", "A fresh green sprout." },
            { "Splash Fountain", "A fountain that bubbles like a little hot spring." },
            { "Monster Den", "The front door of your home under the ground. Tap it to slide down and decorate!" },
            { "Furniture Shop", "Cosy things for your burrow: five new pieces every day, and the burrow door itself." },

            { "Ghost", "A friendly spirit with an errand for you." },
            { "Pirate Pete", "An old sailor full of tall tales. Drag spare items onto him to sell them." },
            { "Pirate Peach", "A pirate captain whose ship froze in the harbour. Loud, sparkly and kind-hearted." },
            { "Coriander", "Old Yuzu's granddaughter. She dreams of reopening the family bathhouse." },
            { "Wedding Dress", "A dress the colour of cherry blossom." },
            { "String of Pearls", "Pearls that glow like little moons." },
            { "Tiara", "On these islands, a tiara means 'you'll always be my favourite'." },

            { "Verdant Bay", "Pay the spirit to raise the bridge to Mossy Meadow." },
            { "Ginger Cove", "Pay the spirit to raise the bridge to Pebble Cove." },
            { "Wedding Isle", "Pay the spirit to raise the bridge to Blossom Isle." },
            { "Pirate Cove", "Pay the spirit to raise the bridge to Barnacle Bay." },
            { "Monster Isle", "Pay the spirit to raise the bridge to the Gloomfen." },
            { "Farm Isle", "Pay the spirit to raise the bridge to Sunflower Farm: fields ready to plant, and a barn full of seeds." },

            { "Bomb", "Goes fizz, then BOOM. Clears pests, and turns everything nearby back into bubbles." },
            { "Deer Bomb", "A gentler bomb. Sends guinea pigs and zombie capybaras packing and leaves everything else alone." },
        };

        /// <summary>The line about an item in the shop and on its cards.</summary>
        public static string Blurb(string id)
        {
            // A seed packet says what it grows, and where to plant it.
            if (StoreData.SeedCrop(id) is CropKind crop)
                return string.Format("Drop it on an empty plot on Sunflower Farm: {0} × {1} in {2} min.", crop.Yield, crop.Name, crop.Minutes);
            return !string.IsNullOrEmpty(id) && Blurbs.TryGetValue(id, out var b) && b.Length > 0 ? b : "";
        }

        /// <summary>
        /// What a worker home's capybara is good at, for its buy panel — so a pricier breed says why.
        /// Read off the species stats (game.xml &lt;animals&gt;, built in IslandBuilder.BuildAnimal):
        /// a capybara has speed 3, and speed sets both walking and how much a swing takes off.
        /// </summary>
        static readonly Dictionary<string, string[]> Strengths = new()
        {
            { "Capy House", new[] { "Works at a steady pace, at every job.", "Eats an ordinary amount." } },
            // speed 6; ironwood and tulips 12
            { "Fox House", new[] { "Walks and works twice as fast as a capybara.", "Chops ironwood 4 times quicker.", "Eats tulips in a flash." } },
            // speed 2; maple and tulips 8; food rate 2
            { "Panda House", new[] { "Walks and works a little slower than a capybara.", "Chops maple nearly 3 times quicker.", "Eats tulips in a flash.", "Each flower keeps it full twice as long." } },
            // speed 4; daisies 8
            { "Pirate House", new[] { "Walks and works a third faster than a capybara, at every job.", "Eats daisies in a flash." } },
            // speed 6; bronze, silver and gold 12
            { "Monster House", new[] { "Walks and works twice as fast as a capybara.", "Digs bronze, silver and gold 4 times quicker." } },
        };

        /// <summary>The worker each house sells beside it, whose buy panel shows the same lines.</summary>
        static readonly Dictionary<string, string> HomeOf = new()
        {
            { "Capybara", "Capy House" }, { "Fox", "Fox House" }, { "Panda", "Panda House" },
            { "Pirate", "Pirate House" }, { "Monster", "Monster House" },
        };

        /// <summary>The worker lines for an item's buy panel (a home or the worker itself), or "" when it brings no worker.</summary>
        public static string WorkerStrengths(string id)
        {
            if (!string.IsNullOrEmpty(id) && HomeOf.TryGetValue(id, out var home)) id = home;
            if (string.IsNullOrEmpty(id) || !Strengths.TryGetValue(id, out var lines)) return "";
            return "<b>As a worker</b>\n" + string.Join("\n", lines);
        }

        // ---------------------------------------------------------------- achievements

        static readonly Dictionary<string, (string title, string blurb)> Achievements = new()
        {
            { "first_mote", ("First Warmth", "Your first warm heart! Each one you find brings a little more warmth back to the islands.") },
            { "fox1", ("A Little Burrower", "A burrow capybara has moved in. Mind the new hole in the lawn.") },
            { "fox5", ("Burrow Village", "Five burrow capybaras, five front doors, one very happy hillside.") },
            { "panda1", ("Fluff Has Arrived", "You found a woolly capybara. It is mostly fluff.") },
            { "silver_store", ("Silver Store", "Wind chimes and peppermint. The Silver Store is open again.") },
            { "steel_store", ("Steel Store", "Sturdy tools for a sturdy builder.") },
            { "gold_store", ("The Gold Store", "Gold letters over the door and cinnamon in the air.") },
            { "built_church", ("The Blossom Chapel", "A chapel for celebrations, under blossom that never falls.") },
            { "girl_quest_1", ("Tulips for Miso", "You planted tulips. Miso pretended not to notice, then smiled for an hour.") },
            { "girl_quest_2", ("The Blossom Dress", "Miso's new dress is the colour of cherry blossom.") },
            { "girl_quest_3", ("A Tall Tale", "Salty Kombu told you a story. Some of it may even have been true.") },
            { "girl_quest_5", ("Roses for the Captain", "You planted roses as red as Captain Wasabi's favourite chilli.") },
            { "girl_quest_6", ("Pearls Like Moons", "A pearl necklace, and one very flustered Miso.") },
            { "girl_quest_8", ("Guinea Pig Patrol", "The guinea pig gang has been sent packing. The gardens are safe.") },
            { "girl_quest_10", ("Head Over Paws", "Miso said it first. Then you said it too.") },
            { "wedding_mote", ("Happily Ever After", "A wedding at the Blossom Chapel. The whole island came, even the ghosts.") },
            { "gem_store", ("Legendary Store", "Gems, glitter and fancy fountains, all for sale.") },
            { "holy_statue", ("Spirit Statue", "A little stone capybara watches over the springs again. Every statue raises your score.") },
            { "bronze_store", ("Bronze Store", "The second shop on the islands. More goodies await!") },
            { "panda_sawmill", ("Fluffy Lumberjack", "A woolly capybara is chopping wood for Captain Wasabi. Look at that waddle.") },
            { "5_plum", ("Plum Orchard", "Five plum trees and one very sticky captain.") },
            { "monster_tame", ("Warm Again", "Five zombie capybaras had a warm bath and remembered how to smile.") },
            { "peach_dance", ("The Hot Pepper Hornpipe", "Captain Wasabi danced for you. There was a spin. There was nearly a splash.") },
            { "girl_crown", ("The Tiara", "You gave Miso a tiara. On these islands, that means something special.") },
            { "place_mine", ("Into the Hills", "You placed a mine. Now the hills will share their gold.") },
            { "dress_up", ("Dressed Up", "You put together your very first outfit. Looking sharp!") },
            { "visit_three", ("Good Neighbour", "You visited three friends' islands. Island life is better together.") },
            { "visit_feed", ("Lunch Delivery", "You fed a hungry worker on a friend's island.") },
            { "visit_pests", ("Pest Control", "You chased the pests off a friend's island.") },
            { "hobby_fish", ("First Catch", "Your very first fish! The sea is full of them.") },
            { "hobby_bird", ("Bird Spotter", "You spotted your first bird. Keep your eyes on the treetops.") },
            { "hobby_book", ("A Wish on the Rack", "Your first ema hangs at Old Mochi's shrine. Friends who visit can read it.") },
            { "hobby_sew", ("Fashion Designer", "You sewed your first hat. Very stylish.") },
            { "hobby_garden", ("Green Thumb", "Your first harvest! Vegetables taste better when you grew them yourself.") },
            { "hobby_cook", ("Home Cooking", "You cooked your first dish. The whole island smells wonderful.") },
            { "hobby_bug", ("Bug Catcher", "Your first bug, safely netted and noted in your journal.") },
            { "hobby_cat", ("Cat Person", "A cat has chosen you. Or you chose it. It's hard to tell with cats.") },
            { "hobby_catgift", ("It's the Thought That Counts", "A cat brought you a present. You said thank you. You meant it, mostly.") },
            { "built_airport", ("Ready for Take-off", "You built an airstrip. The whole archipelago is a short flight away.") },
        };

        public static string AchievementTitle(string func, string fallback) =>
            Achievements.TryGetValue(func ?? "", out var a) ? a.title : fallback;

        public static string AchievementBlurb(string func) =>
            Achievements.TryGetValue(func ?? "", out var a) ? a.blurb : "";

        // ---------------------------------------------------------------- discovery cards

        static readonly Dictionary<int, (string title, string blurb)> CardText = new()
        {
            { 1, ("General Store", "Every shop on the islands starts here.") },
            { 2, ("Bronze Store", "Maple trees, tulips and handy shoo bombs.") },
            { 3, ("Steel Store", "Sturdy supplies for serious builders.") },
            { 4, ("Silver Store", "Wind chimes in the door and peppermint in the air.") },
            { 5, ("Gold Store", "The fanciest shop on the isles.") },
            { 7, ("Gray Hill", "A rocky hill full of bronze. Put a mine beside it.") },
            { 8, ("Black Hill", "A dark hill hiding silver.") },
            { 9, ("Gold Hill", "A glittering hill with gold inside.") },
            { 10, ("Pine Tree", "Quick to spread. A sawmill's best friend.") },
            { 11, ("Maple Tree", "More wood with every swing.") },
            { 12, ("Ironwood Tree", "Hard as iron, and it grows back fast.") },
            { 13, ("Sawmill", "Capybaras chop nearby trees and bring the wood here.") },
            { 14, ("Legendary Sawmill", "So grand the trees line up to be chopped.") },
            { 15, ("Mine", "Workers dig gold out of nearby hills.") },
            { 16, ("Legendary Mine", "The deepest, shiniest mine on the isles.") },
            { 18, ("Capybara", "Calm, kind and always ready to help. The heart of every island.") },
            { 19, ("Woolly Capybara", "A capybara in a fluffy winter coat. Waddles beautifully.") },
            { 20, ("Burrow Capybara", "Small, quick and fond of digging cosy holes.") },
            { 22, ("Zombie Capybara", "Grumbly and muddy, until someone gives it a warm bath.") },
            { 23, ("Daisies", "Sweet little flowers. A snack for hungry capybaras.") },
            { 24, ("Tulips", "Bright tulips that keep workers well fed.") },
            { 25, ("Roses", "The most filling flower, and quick to grow back.") },
            { 26, (Sweetheart, "Old Yuzu's granddaughter. She dreams of reopening the family bathhouse.") },
            { 27, (Captain, "Her ship froze in the harbour. Loud, sparkly and kind-hearted.") },
            { 28, (Sailor, "An old sailor full of tall tales. He'll buy your spare loot.") },
            { 29, ("Blossom Chapel", "Where the islands come together to celebrate.") },
            { 30, ("Steam Tower", "It stands over the old hot spring. Every warm heart you find flies here.") },
            { 31, ("Pink Blossom Tree", "Pink petals, all year round.") },
            { 32, ("White Blossom Tree", "Soft white blossom, like warm snow.") },
            { 33, ("Spirit Statue", "A stone capybara that watches over the springs.") },
            { 34, ("Gem Statue", "A statue that sparkles in the sun.") },
            { 35, ("Burrow Hole", "A cosy hillside home for burrow capybaras.") },
            { 36, ("Zombie Shack", "Where cured zombie capybaras come to live.") },
            { 37, ("Guinea Pig Hutch", "A home for guinea pigs. Brave choice.") },
            { 38, ("Pirate Shack", "A home for pirate capybaras, with a flag on the roof.") },
            { 39, ("Capy House", "A snug home where capybaras come to live and work.") },
            { 40, ("Woolly Lodge", "A warm lodge for woolly capybaras.") },
            { 41, ("Apple Tree", "Shake it for apples, and the odd surprise.") },
            { 42, ("Pear Tree", "Shake it for pears and hidden treats.") },
            { 43, ("Legendary Plum Tree", "Rare, sweet plums with treasures in the branches.") },
        };

        public static string CardTitle(int id) => CardText.TryGetValue(id, out var c) ? c.title : "";
        public static string CardBlurb(int id) => CardText.TryGetValue(id, out var c) ? c.blurb : "";

        // ---------------------------------------------------------------- quests

        public readonly struct QuestLines
        {
            public readonly string Intro, During, Complete;
            public QuestLines(string intro, string during, string complete) { Intro = intro; During = during; Complete = complete; }
        }

        // Pages are separated by [br], one tap each.
        static readonly Dictionary<(QuestSource, int), QuestLines> Quests = new()
        {
            // Mister Kinako, who misses the Burrowfolk.
            { (QuestSource.Ghost, 1), new(
                "Ooooh... oh! Did I startle you? I'm Mister Kinako. I used to sweep the paths for the Burrowfolk.[br]Burrow capybaras, little ones! They dug cosy holes in the hillsides and kept their tunnels spotless.[br]When the Big Chill came, they curled up deep underground and never came out.[br]Could you coax one back? Look for a Burrow Hole at the Bronze Store.",
                "No burrow capybaras yet? Their noses twitch for warm soil. Try the Burrow Hole from the Bronze Store.",
                "A burrow capybara, popping its head out of the ground! I could weep, if ghosts had tears.[br]Here, take this. Daisies have been growing on my old porch, and I've no use for them now.") },
            { (QuestSource.Ghost, 3), new(
                "Mister Kinako again! Our burrow capybara is lovely, but lonely.[br]In the old days the Burrowfolk had a whole village, their round doors all in a row along the hill.[br]On the coldest night of the year they'd share one big pot of chestnut soup and sing tunnel songs.[br]Could you gather five of them? Then they'd have a proper village again.",
                "Still fewer than five burrow capybaras. You can't sing a tunnel song as a duet!",
                "Five burrow capybaras! They've already dug a doorway for each other.[br]One of them gave me a chestnut. I can't eat it, being see-through, but I'll treasure it.[br]Please, have this ironwood. It always loved them, and it will love you too.") },
            // Sister Hojicha, from the chapel.
            { (QuestSource.Ghost, 8), new(
                "Hoo-hoo... I'm Sister Hojicha, from the Blossom Chapel. Well, from what's left of it.[br]Long ago every island had spirit statues, little stone capybaras keeping watch over the hot springs.[br]When the cold came they cracked and tumbled, and the islands have felt unwatched ever since.[br]The Legendary Gem Store sells Spirit Statues now. Would you raise one? Every statue adds to your island's score.",
                "Not a single spirit statue yet. Who is keeping an eye on the springs?",
                "A spirit statue! Look at its little stone smile.[br]Every one you build raises your island's score. The more watchful stone friends, the merrier.") },

            // Miso.
            { (QuestSource.Coriander, 16), new(
                "Oh! Hello. Sorry, I'm Miso. I've been inside a bubble so long I forgot how to say hello.[br]Before the Big Chill there were shops all over the islands. Now there's barely a stall.[br]I saved this one, the General Store. Every shop keeps the plans for the next.[br]Could you build the Bronze Store? It would be a start.",
                "The Bronze Store sells maple trees, and shoo bombs too. Very handy.",
                "The Bronze Store is open! We should keep a few bombs about, in case the guinea pigs come back.[br]Thank you. It feels like the whole island just took a deep breath.") },
            { (QuestSource.Coriander, 1), new(
                "Grandma used to read me sea stories by the bathhouse fire. Storms, sea serpents, buried treasure...[br]I've heard there's a real sailor washed up on the beach. Salty Kombu, he calls himself.[br]Would you go and hear one of his stories? Then come back and tell me everything!",
                "Have you talked to Salty Kombu yet? I want a story with at least one sea serpent in it.",
                "You heard one of Kombu's tales? Tell me, tell me![br]A giant squid that juggled lanterns? A lighthouse made of cheese? I don't believe a word of it. I love it.[br]Here, a little ironwood grove, for being my storyteller.") },
            { (QuestSource.Coriander, 2), new(
                "When I was little, Grandma took me to the Silver Store every spring. It had wind chimes in the doorway and smelled of peppermint.[br]I'd press my nose to the glass and choose the things I'd buy one day.[br]Do you think its plans are still around somewhere? I'd love to see it again.",
                "No Silver Store yet... I can almost hear the wind chimes, though.",
                "The Silver Store! The chimes, the peppermint, everything![br]Grandma left a purse of gold for a special day. I think today counts.") },
            { (QuestSource.Coriander, 8), new(
                "Have you seen the dress in the Silver Store window? It's the colour of cherry blossom.[br]I know it's silly, wanting something pretty while the island's still so chilly. But I keep thinking about it.",
                "I walked past the Silver Store three times today. Just to look. Not because of the dress. Ahem.",
                "You bought it? For me?![br]I'm wearing it right now. It feels like spring came early.[br]Do... do I look all right? Don't answer that. No, answer it.") },
            { (QuestSource.Coriander, 5), new(
                "Shh! Do you hear that? Nibble, nibble, nibble...[br]Guinea pigs. A whole gang of them. They came with the cold and ate every flower on the islands.[br]They've turned up again, on our island and the next one over.[br]Please be careful, and take some bombs. They only scatter when something goes fizz!",
                "Nibble, nibble... they're still out there. A bomb under their noses will send them packing.",
                "No more nibbling! The gardens are safe![br]I'll sleep tonight without dreaming about tiny teeth. Take these gems. You've earned them, hero.") },
            { (QuestSource.Coriander, 4), new(
                "Hello again, crown-wearer. I'm starting to like that little crown of yours, you know.[br]Grandma always said the Steel Store sold the sturdiest tools on the isles. Builders came from everywhere.[br]With all the building you're doing, maybe you need one?",
                "Still no Steel Store. Your hammer must be getting tired.",
                "The Steel Store is open! Here, Grandpa's old woodpile. He'd want it to go to a real builder.") },
            { (QuestSource.Coriander, 9), new(
                "The new shop has a counter full of pearls. Have you seen them? They glow like little moons.[br]I was hoping... maybe we could go and look at them together sometime?[br]Oh. You're busy. That's fine. Everyone's busy.",
                "I'm not sulking. I'm just... thinking about pearls. On my own.",
                "You went and bought the necklace? I only wanted to look at it with you![br]Oh, you sweet, ridiculous capybara. I'm never taking it off.") },
            { (QuestSource.Coriander, 6), new(
                "Can I tell you a secret? There was a shop at the edge of the island with gold letters over the door.[br]An old gentleman ran it, in a velvet waistcoat, with a cane topped with a sapphire.[br]I never had enough to buy anything. I'd just stand inside and breathe in the cinnamon and polish.[br]I wonder if anyone could ever build it again...",
                "I was daydreaming about the Gold Store again. I think the Big Chill froze it solid.",
                "The Gold Store is back! Same gold letters, same cinnamon smell.[br]Grandma bought her wedding lantern there. Here, some timber she was saving. Build something wonderful.") },
            { (QuestSource.Coriander, 10), new(
                "Grandma had a tiara, you know. Grandpa saved for a whole year to buy it for her.[br]On these islands, when you give someone a tiara, it means 'you'll always be my favourite'.[br]Hers was lost in the cold. I suppose old traditions get lost too.",
                "There's a tiara in the Gold Store window. Some lucky capybara is going to be very happy one day.",
                "A tiara... for me?[br]I don't know what to say. My heart is doing somersaults.[br]Can you give me a little while to think? I want to say this right.") },
            { (QuestSource.Coriander, 7), new(
                "There you are! I didn't sleep a wink last night. I kept practising this.[br]Okay. Deep breath.[br]When you washed up here, everything was so quiet and empty. Then you started building, and planting, and smiling at me, and...[br]I love you. There. I said it.[br]Would you... would you marry me?",
                "Well? Don't leave me standing here like a frozen puddle!",
                "You said yes! You said YES![br]Oh, if only Grandma could marry us herself... We'll wait for the perfect moment. I've saved a few gems for the celebration.") },

            // Captain Wasabi.
            { (QuestSource.Peach, 5), new(
                "Ahoy! Captain Wasabi of the good ship Hot Pepper, at your service! Phew, that bubble was a tight squeeze.[br]My ship froze stiff in the harbour when the cold came, and my crew went off to find somewhere warmer.[br]Now, a captain needs a bit of colour. Plant me some roses and I'll make you an honorary first mate!",
                "Roses, matey! Red ones! Don't make me walk you off a plank.",
                "Roses! Red as my favourite chilli! You've got taste.[br]Have this maple grove, first mate. A good captain rewards loyalty.") },
            { (QuestSource.Peach, 6), new(
                "This island has all the sparkle of a wet sock. Practical, aye, but where's the treasure?[br]Build the Legendary Gem Store and we'll give this place some real dazzle.[br]Last I saw, the Steel Store had the plans.",
                "No Gem Store yet? A captain can't live on practical alone!",
                "Now that's more like it! Gems, glitter, fancy fountains, the works![br]Here's a chest of gold from my cabin. Spend it on something shiny.") },
            { (QuestSource.Peach, 2), new(
                "Nothing like a ripe plum on a long voyage. Sweet, juicy, gloriously messy.[br]Pity there isn't a plum tree for leagues.[br]Plant me an orchard, would you? Five Legendary Plum Trees should do it.",
                "Five plum trees, matey. I'm counting!",
                "An orchard! I've already eaten nine plums. Don't tell anyone.[br]Here, some ironwood for you. Sturdy, like our friendship.") },
            { (QuestSource.Peach, 1), new(
                "Truth be told, it's been lonely since my crew left. A captain without a crew is just a capybara in a fancy hat.[br]But look who I found snoozing in a bubble! A woolly capybara! The fluffiest crew member a captain could want.[br]Would you give this fluffball a job at a sawmill? Watching them waddle cheers me right up.",
                "Is our woolly friend chopping wood yet? Pop them on a sawmill!",
                "Look at that fluff waddle! Best crew member I've ever had, and I once had a parrot who could do sums.[br]Here, a little gold for the wages.") },
            { (QuestSource.Peach, 3), new(
                "When I first sailed in, I was nearly nibbled by zombie capybaras! Grumbly, muddy, stumbling about.[br]Thing is, they're not bad. They're just cold. They stayed out in the Gloomfen too long and forgot how to be warm.[br]A few gems buys one a nice hot bath, and it turns right back into a sweetheart.[br]Warm up five of them for me, would you? Every crew needs a few big softies.",
                "Five zombie capybaras, warmed up and happy. Gems for their bathwater, remember!",
                "Five warm, happy, ex-zombies! They follow you about like ducklings.[br]Here's a stack of timber from my ship. She won't be sailing anywhere cold again.") },
            { (QuestSource.Peach, 4), new(
                "Psst. I've been practising a dance. The Hot Pepper Hornpipe! Very traditional. Very spicy.[br]Trouble is, a proper performance needs a proper costume, and a proper costume needs sparkles.[br]Five hundred gems' worth of sparkles. For you, I'll dance it!",
                "The offer stands: five hundred gems and you get the Hot Pepper Hornpipe.",
                "Ta-daaa! Did you see the spin at the end? I nearly fell in the sea![br]Truth is, I didn't need all those gems for the costume. I just like being spoiled. You're a good egg, first mate.") },
        };

        public static QuestLines Quest(QuestSource giver, int id) =>
            Quests.TryGetValue((giver, id), out var q) ? q : new QuestLines("...", "...", "...");

        // ---------------------------------------------------------------- Old Yuzu's heart (YuzuHeart)

        /// <summary>
        /// Old Yuzu, frozen beside her spring, thawing as warm hearts come home; then alive again;
        /// then the finale, once every heart is home.
        /// </summary>
        public static class Yuzu
        {
            /// <summary>Once, after you first talk to Old Mochi at his shrine: she knows exactly who he is.</summary>
            public const string MetMochi = "You went up to the shrine? Then you met my Mochi. Fifty years he swept those steps, and he still thinks I never noticed. Tell him... no. I'll tell him myself, when the spring is warm.";

            /// <summary>Her ghost, by her frozen body, before she has noticed anything.</summary>
            public static readonly string[] AtHerBody =
            {
                "That's... me, from before. Don't be sad, little one. I'm right here.",
                "I lay down here on the coldest night. The snow melted long ago... but the ice never did.",
                "Sometimes I come and sit beside myself. Is that strange? It feels a little less lonely.",
            };
            public const string Warmer = "Funny... I haven't felt warm in so many years.";
            public const string Wondering = "Every time a warm heart comes home, I feel it. Here. Could it be...? No. Let's not hope too hard.";

            // The movies.
            public const string Twitch = "Did... did you see that?";
            public const string Seeing = "No, no. Silly old ghost, seeing things.";
            public const string Glow = "Little one... every time a warm heart comes home, I feel it. Here.";
            public const string Hope = "Could it be...? No. Let's not hope too hard. Keep going.";
            public const string Warm = "I'm... warm.";
            public const string Alive = "I'm warm! I'm alive! Little {0}... you did this. Every warm heart you brought home.";
            public const string Wedding = "And now... I believe my granddaughter has a wedding to plan. I wouldn't miss it for anything!";

            /// <summary>Alive, in person.</summary>
            public static readonly string[] Back =
            {
                "I'm warm! I still can't get over it. Thank you, little one.",
                "My old knees are complaining again. I'd forgotten how much I missed them.",
            };
            public const string Engaged = "Miso told me everything! The Blossom Chapel is waiting, whenever you're ready.";
            public const string Married = "Seeing you two together... it was worth every frozen year.";

            /// <summary>Every heart home: the finale, and her everyday words after it.</summary>
            public const string Finale1 = "When the cold came, I saved what I could in my bubbles, and I thought that was all I had left.";
            public const string Finale2 = "Then you washed up on my shore. Thank you for bringing everyone home, little {0}.";
            public const string FinaleSubtitle = "Home Again";
            public const string FinaleCardTitle = "Home Again";
            public const string FinaleCardBody = "Every warm heart is home, and the Capy Isles are alive again. Old Yuzu gave you her lantern, to remember the night everyone came home.";
            public static readonly string[] Home =
            {
                "Look at it, little one. Steam in the spring, lanterns on the path... and everyone home.",
                "I've decided I'm going to live to be very, very old. Again.",
                "Some evenings I still sit by the spring where I lay all those years. Now I just put my feet in.",
            };
        }

        /// <summary>Miso, once her grandmother is back, and once everyone is home.</summary>
        public static readonly string[] MisoGrandmaBack =
        {
            "Grandma's back. GRANDMA'S BACK. I still say it out loud every morning, just to hear it.",
            "Grandma says you can have a free soak in the spring any time. Forever. She made it official.",
        };
        public static readonly string[] MisoHome =
        {
            "The spring is steaming, the lanterns are lit, and everyone's home. I keep pinching myself.",
            "Remember the list of everything we built together? I ran out of paper.",
        };

        // ---------------------------------------------------------------- small talk

        public static readonly string[] MisoChat =
        {
            "Grandma used to say a hot bath fixes almost everything. I'm still working out what it doesn't fix.",
            "Some nights I sit on the bathhouse steps and imagine the steam rising again.",
            "Do you ever talk to the trees? I do. The maples are good listeners.",
            "I found a single mitten in the snow this morning. I feel like that sometimes.",
            "When the spring flows again, I'm going to open the bathhouse and give everyone a free first soak.",
            "You've been stopping by a lot lately. Not that I mind. I don't mind at all.",
            "What a lovely day for sitting and chatting.",
            "Grandma's ghost hums when she's happy. She's been humming a lot since you arrived.",
            "I like a capybara who works hard. And who brings snacks.",
            "When you say hello, I bounce a little. I can't help it.",
            "It almost feels like spring. Even the bees look giddy.",
            "A crown, though? Couldn't you have washed up in a sensible hat?",
            "Have you tried floating on your back in warm water? It's the best feeling in the world.",
            "Why are you looking at me like that? You're making me blush.",
            "You have very kind eyes. Small, but kind.",
            "Salty Kombu told me he once wrestled a whale. I think the whale won.",
            "I hope the guinea pigs never come back. Their tiny teeth haunt me.",
            "I think we make a good team, you and I.",
            "If we ever have little ones, they're going to love swimming.",
            "I keep a list of everything we've built together. It's getting very long.",
        };

        public static readonly string[] WasabiChat =
        {
            "A captain never shows fear. Except of crabs. Crabs are the worst.",
            "My ship's still frozen in the harbour. I talk to her every morning so she doesn't get lonely.",
            "Sparkly things make everything better. That's not an opinion, it's a law of the sea.",
            "My grandfather sailed these waters. He swore the hot spring could be smelled from three islands away.",
            "I left home with nothing but a hat and a hunch. The hat's held up better than the hunch.",
            "Treasure isn't always gold, first mate. Sometimes it's plums.",
            "If you ever need someone shouted at, I'm your capybara.",
            "Did you know zombie capybaras purr when they get warm? Like a big muddy kettle.",
            "The guinea pigs ate my map once. The whole map. I had to sail by vibes for a month.",
            "A dance a day keeps the cold away!",
            "My crew used to call me Captain Spicy. I'm not sure it was a compliment.",
            "Gems sparkle, plums are sweet, and a good friend is worth both.",
            "Someday I'll sail again. But I might just stay a while.",
            "Secret: I get seasick. Don't tell the crew.",
            "Miso keeps looking at you when you're not watching. Just saying.",
        };

        // ---------------------------------------------------------------- the spirit's nudges

        /// <summary>Old Yuzu introducing her granddaughter, the first time you go to talk to her.</summary>
        public const string MisoIntro = "Oh! That's my granddaughter Miso! She made it through the cold! Go on, see if she needs a hand.";

        public static string NudgeFor(QuestGiver.Kind kind) => kind switch
        {
            QuestGiver.Kind.Coriander => "Oh! My granddaughter Miso made it through the cold! Tap her and see if she needs a hand.",
            QuestGiver.Kind.Peach => "There's a pirate captain on the next island. Captain Wasabi knows how to raise the bridge to Barnacle Bay.",
            _ => "A spirit has appeared with an errand for you. Tap them to find out more.",
        };
    }
}
