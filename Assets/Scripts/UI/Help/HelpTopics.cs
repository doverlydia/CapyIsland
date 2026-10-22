using System;
using CapyIsland.Game;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>The shelves the help pages sit on, in the order the list shows them.</summary>
    public enum HelpCategory { GettingAround, Workers, Resources, Building, Travel, Goals, Hobbies, Friends, Account }

    /// <summary>One "How do I…?" page: what it's called, a line about it, the steps, and its movie.</summary>
    public sealed class HelpTopic
    {
        public string Id;
        public HelpCategory Category;
        public string Title;
        /// <summary>One line under the title in the list.</summary>
        public string Summary;
        /// <summary>Other words someone might search with (each language has its own list in the tables).</summary>
        public string Keywords;
        public string[] Steps;

        public HelpDemo Demo;
        /// <summary>For the picture movies: the picture, and what pops out of it.</summary>
        public string Picture, Found;

        /// <summary>The little picture on the card.</summary>
        public Func<Sprite> Thumb;

        /// <summary>A button that takes you there ("Open the map"), or none.</summary>
        public string ShowMeLabel;
        public Action ShowMe;

        /// <summary>False while it's still ahead of you (a hobby not taken up yet). The page still opens.</summary>
        public Func<bool> Started;
    }

    /// <summary>
    /// Every help page, kept apart from the screen that shows them so they are easy to add to and
    /// edit. Each step was checked against the code that does it (the worker FSM, the map's pin
    /// drag, Salty Kombu's offer, the shop chain…), not written from memory of other games:
    /// a help page that teaches a move that doesn't work is worse than none.
    ///
    /// The words are English here and translated as they are drawn, like every label in the game;
    /// Lang/he/help.txt has the Hebrew for every title, line, step and keyword list.
    /// </summary>
    public static class HelpTopics
    {
        public static string CategoryName(HelpCategory c) => c switch
        {
            HelpCategory.GettingAround => "Getting around",
            HelpCategory.Workers => "Workers",
            HelpCategory.Resources => "Wood, gold and gems",
            HelpCategory.Building => "Building and shops",
            HelpCategory.Travel => "Islands and travel",
            HelpCategory.Goals => "Goals and gifts",
            HelpCategory.Hobbies => "Hobbies",
            HelpCategory.Friends => "Friends",
            _ => "Settings and your account",
        };

        public static Color CategoryColour(HelpCategory c) => c switch
        {
            HelpCategory.GettingAround => UiKit.Spring,
            HelpCategory.Workers => UiKit.Primary,
            HelpCategory.Resources => UiKit.Hex("E3A92B"),
            HelpCategory.Building => UiKit.Hex("C98B4E"),
            HelpCategory.Travel => UiKit.Hex("5B9BD5"),
            HelpCategory.Goals => UiKit.Hex("F0506E"),
            HelpCategory.Hobbies => UiKit.Hex("3E9C8F"),
            HelpCategory.Friends => UiKit.Hex("A77BD6"),
            _ => UiKit.Hex("6B8FA8"),
        };

        public static HelpTopic Find(string id)
        {
            foreach (var t in All) if (t.Id == id) return t;
            return null;
        }

        // ---------------------------------------------------------------- ways in

        static void Settings() { if (SettingsScreen.Instance != null) SettingsScreen.Instance.Open(); }
        static void Gifts() { if (DailyGiftScreen.Instance != null) DailyGiftScreen.Instance.Open(); }
        static void Neighbours() { if (FriendsScreen.Instance != null) FriendsScreen.Instance.Open(); }
        static void Events() { if (EventsScreen.Instance != null) EventsScreen.Instance.Open(); }

        static Func<bool> HobbyStarted(Hobby h) => () => HobbyLog.IsUnlocked(h);

        static Func<Sprite> Item(params string[] ids) => () => HelpArt.Item(ids);
        static Func<Sprite> Art(string name) => () => HelpArt.Picture(name);

        // ---------------------------------------------------------------- the pages

        public static readonly HelpTopic[] All =
        {
            // ---- Getting around
            new HelpTopic
            {
                Id = "walk", Category = HelpCategory.GettingAround, Demo = HelpDemo.Walk,
                Thumb = () => HelpArt.You,
                Title = "Walk around",
                Summary = "Tap a spot, or hold your finger down to steer.",
                Keywords = "move walk go run steer joystick keyboard tap finger",
                Steps = new[]
                {
                    "Tap any spot on the land and your capybara walks there.",
                    "Or press and hold on the ground: your capybara walks toward your finger and follows it.",
                    "Like a joystick better? Turn on Walk with a joystick in Settings.",
                    "On a computer, the arrow keys or W, A, S and D walk too.",
                    "Your capybara can't swim: to reach another island, its bridge has to be open.",
                },
                ShowMeLabel = "Open Settings", ShowMe = Settings,
            },
            new HelpTopic
            {
                Id = "zoom", Category = HelpCategory.GettingAround, Demo = HelpDemo.Zoom,
                Thumb = () => IconPainter.Magnifier,
                Title = "Zoom in and out",
                Summary = "Pinch with two fingers, or scroll the mouse wheel.",
                Keywords = "zoom pinch closer far bigger smaller camera view see mouse wheel scroll look",
                Steps = new[]
                {
                    "Put two fingers on the screen and spread them apart to zoom in.",
                    "Pinch them together to zoom out and see more of the island.",
                    "On a computer, scroll the mouse wheel.",
                    "The view always follows your capybara. To see somewhere else, walk there, or peek at it on the Map.",
                },
            },
            new HelpTopic
            {
                Id = "arrows", Category = HelpCategory.GettingAround, Demo = HelpDemo.Arrows,
                Thumb = () => IconPainter.Arrow,
                Title = "Arrows at the edge",
                Summary = "They point at things you can't see yet.",
                Keywords = "arrow edge point find where off screen stop walking",
                Steps = new[]
                {
                    "An arrow at the edge of the screen points at something you can't see yet.",
                    "The picture inside shows what it is: a bubble, someone with a quest, pests or a visitor.",
                    "Tap the arrow and your capybara walks there. Tap Stop walking to stop on the way.",
                    "Want words under the arrows? Turn on Names on edge arrows in Settings.",
                },
                ShowMeLabel = "Open Settings", ShowMe = Settings,
            },

            // ---- Workers
            new HelpTopic
            {
                Id = "worker", Category = HelpCategory.Workers, Demo = HelpDemo.Worker,
                Thumb = () => HelpArt.Worker,
                Title = "Put a capybara to work",
                Summary = "Drag a capybara from your trail onto a sawmill or a mine.",
                Keywords = "worker work job capybara drag station sawmill mine assign buy house",
                Steps = new[]
                {
                    "Get a capybara: tap the one standing in front of your Capy House. Each one costs 10 wood and 10 gold.",
                    "It follows you in your trail. Press it and drag it onto a sawmill or a mine.",
                    "While you drag, every sawmill and mine shows a ring and how many workers it has, like 1/4. Green has room.",
                    "Let go on it. Your capybara chops the trees, or digs the rocks, in that place's circle and carries them home.",
                    "To move a worker, just pick it up again. Drop it anywhere else and it follows you.",
                },
            },
            new HelpTopic
            {
                Id = "feed", Category = HelpCategory.Workers, Demo = HelpDemo.Feed,
                Thumb = Item("Medium Posie Garden", "Small Posie Garden"),
                Title = "Feed your workers",
                Summary = "Plant flowers near the place they work.",
                Keywords = "food feed eat hungry flowers garden daisy tulip rose snack fruit",
                Steps = new[]
                {
                    "Working makes capybaras hungry. A hungry worker stops and looks for food near its own sawmill or mine.",
                    "Capybaras eat flowers. Plant a garden close to the place they work, so they can reach it.",
                    "Fruit trees nearby feed them too: they shake the fruit down.",
                    "Tulips fill them up more than daisies, and roses most of all.",
                    "Gardens grow back by themselves, but a big crew needs a big garden.",
                },
            },
            new HelpTopic
            {
                Id = "hungry", Category = HelpCategory.Workers, Demo = HelpDemo.Feed,
                Thumb = Item("Medium Posie Garden", "Small Posie Garden"),
                Title = "Is my worker hungry?",
                Summary = "Look for a sleeping capybara dreaming of flowers.",
                Keywords = "hungry sleep asleep tired dream bubble stopped lazy not working",
                Steps = new[]
                {
                    "A hungry capybara with nothing to eat falls asleep beside its sawmill or mine.",
                    "Over its head, a dream bubble shows a garden. Tap it and Old Yuzu tells you what it needs.",
                    "Press and hold the capybara counter at the top of the screen to see how many are asleep.",
                    "Plant a garden near it and it wakes up for a snack.",
                    "A worker also naps when there's nothing left to chop or dig in its circle.",
                },
            },
            new HelpTopic
            {
                Id = "kinds", Category = HelpCategory.Workers, Demo = HelpDemo.Kinds,
                Thumb = Item("Fox", "Capybara"),
                Title = "Other kinds of capybara",
                Summary = "Burrow, woolly and zombie capybaras.",
                Keywords = "burrow woolly zombie pirate kinds types faster worker house lodge hole cure bath",
                Steps = new[]
                {
                    "Each capybara home sells its own capybara: tap the one standing in front of it.",
                    "Burrow Capybaras walk and work twice as fast. Their Burrow Hole is sold at the Bronze Store.",
                    "Woolly Capybaras live in a Woolly Lodge, sold at the Gold Store.",
                    "A zombie capybara stayed out in the cold too long. Tap it and give it a warm bath for 20 gems, and it works for you.",
                    "Then drag it onto a mine: it digs rock 4 times quicker than a capybara.",
                },
            },

            // ---- Wood, gold and gems
            new HelpTopic
            {
                Id = "wood", Category = HelpCategory.Resources, Demo = HelpDemo.Wood,
                Thumb = () => HelpArt.Resource(ResourceKind.Wood),
                Title = "Get more wood",
                Summary = "A forest, a sawmill next to it, and capybaras to work it.",
                Keywords = "wood logs chop trees forest sawmill more money",
                Steps = new[]
                {
                    "Buy a forest: tap one where a shop sets it out, then tap Purchase. It joins your trail.",
                    "Drag it out and plant it near a sawmill. Its workers only chop trees inside its circle.",
                    "Put capybaras to work at the sawmill. Every tree they carry home adds to your wood at the top: pine 3, maple 7, ironwood 15.",
                    "Up to 4 capybaras can work at one sawmill. More sawmills, more wood! The Bronze Store sells them.",
                    "Feed them: plant a garden near the sawmill too.",
                    "A Legendary Sawmill pays double for everything brought to it.",
                },
            },
            new HelpTopic
            {
                Id = "trees", Category = HelpCategory.Resources, Demo = HelpDemo.Forest,
                Thumb = Item("Medium Pine Forest", "Small Pine Forest"),
                Title = "Get more trees",
                Summary = "Forests spread by themselves, and shops sell more.",
                Keywords = "trees forest grow regrow spread bare chopped pine maple ironwood sapling more",
                Steps = new[]
                {
                    "Buy forests at the shops: pine at the General Store, maple at the Bronze Store, ironwood for gems.",
                    "Drag one out of your trail and plant it near a sawmill. Little trees sprout around the big one straight away.",
                    "The big tree keeps planting more little trees around it, as long as there's room.",
                    "Workers chop the little trees first. Too many workers on one small forest can chop it bare: spread them out, or plant more forests.",
                    "If the big tree itself is chopped down, the forest turns back into a bubble. Pop it and plant it again.",
                    "Ironwood grows back fastest. Maple is quicker than pine.",
                },
            },
            new HelpTopic
            {
                Id = "gold", Category = HelpCategory.Resources, Demo = HelpDemo.Gold,
                Thumb = () => HelpArt.Resource(ResourceKind.Gold),
                Title = "Get more gold",
                Summary = "Dig hills with a mine, sell spare things, and collect gifts.",
                Keywords = "gold money coins rich mine hill rock dig earn more",
                Steps = new[]
                {
                    "Buy a hill at the General Store: tap it, then tap Purchase. It joins your trail.",
                    "Plant it near a mine (the General Store sells those too). Its workers only dig rocks inside its circle.",
                    "Put capybaras to work at the mine. Every rock they bring back adds to your gold at the top: gray hill 2, black hill 7, gold hill 20.",
                    "Sell things you don't need to Salty Kombu.",
                    "Collect your daily gift, shake fruit trees for treasure chests, and finish quests.",
                    "Once you have the Bank, you can trade gems for gold.",
                },
            },
            new HelpTopic
            {
                Id = "gems", Category = HelpCategory.Resources, Demo = HelpDemo.Gems,
                Thumb = () => HelpArt.Resource(ResourceKind.Gem),
                Title = "Get gems",
                Summary = "From quests, daily gifts, hobbies and shaking fruit trees.",
                Keywords = "gems diamonds jewels rare treasure earn spend fruit tree shake chest",
                Steps = new[]
                {
                    "Shake fruit trees: tap one and all its fruit falls. Sometimes a treasure chest falls instead, and some chests hold gems.",
                    "Pear and plum trees drop gems more often than apple trees.",
                    "Daily gifts: tap Gifts. Day 5 of your streak brings 10 gems.",
                    "Fill a whole journal for 40 gems: every fish, bird, bug or crop. The fish, bird and bug journals give a hat too.",
                    "Contests: when one ends, tap Events, then Claim. The top three win 50, 30 or 15 gems, and everyone else who took part gets 3.",
                    "Some of Miso's quests and some cat presents give gems.",
                    "Spend gems on roses, ironwood, gold hills, legendary buildings, and trades at the Bank.",
                },
            },
            new HelpTopic
            {
                Id = "sell", Category = HelpCategory.Resources, Demo = HelpDemo.Sell,
                Thumb = () => { var s = HelpArt.Item("Pirate Pete"); return s != null ? s : HelpArt.Picture("Sailor"); },
                Title = "Sell things",
                Summary = "Drag what you don't need onto Salty Kombu.",
                Keywords = "sell trade money spare get rid sailor kombu deal",
                Steps = new[]
                {
                    "Salty Kombu, the old sailor, buys things you don't need. He turns up in a bubble on Pebble Cove during one of Miso's quests.",
                    "Drag something from your trail, or a worker, and drop it on him.",
                    "He makes you an offer. Tap Deal to sell it, or No Deal to keep it.",
                    "He pays about half of what it cost, in treasure chests that open by themselves.",
                    "He won't buy shops, homes, or things there's only one of.",
                },
            },
            new HelpTopic
            {
                Id = "fruit", Category = HelpCategory.Resources, Demo = HelpDemo.FruitTree,
                Thumb = Item("Apple Tree", "Pear Tree"),
                Title = "Shake fruit trees",
                Summary = "Tap a fruit tree: fruit, chests and surprises fall out.",
                Keywords = "fruit tree apple pear plum shake chest treasure tap",
                Steps = new[]
                {
                    "Tap a fruit tree with fruit on it to shake it.",
                    "Fruit falls out for hungry workers. Sometimes a treasure chest drops instead, or a bubble.",
                    "Bubbles from fruit trees don't last: pop them quickly!",
                    "Treasure chests open by themselves and add to your wood, gold or gems.",
                    "Keep forests near your sawmills: a sawmill with no trees left starts chopping fruit trees down.",
                },
            },
            new HelpTopic
            {
                Id = "bank", Category = HelpCategory.Resources, Demo = HelpDemo.Bank,
                Thumb = Item("Bank"),
                Title = "Trade gems at the Bank",
                Summary = "Swap gems for wood or gold.",
                Keywords = "bank trade swap exchange gems wood gold",
                Steps = new[]
                {
                    "The Bank is sold at the Steel Store for 5 gems.",
                    "Plant it. It sets out two trades on the ground in front of it: wood on one side, gold on the other.",
                    "Tap one and pick a trade: 5 gems for 250, up to 30 gems for 2000. The ones you can't pay for yet are greyed out.",
                    "What you bought comes in a treasure chest that opens by itself.",
                },
            },

            // ---- Building and shops
            new HelpTopic
            {
                Id = "bubbles", Category = HelpCategory.Building, Demo = HelpDemo.Bubble,
                Thumb = () => IconPainter.Sparkle,
                Title = "Pop bubbles",
                Summary = "Tap a bubble to get what's inside.",
                Keywords = "bubble pop tap free present collect pick up",
                Steps = new[]
                {
                    "A bubble holds something for you: a forest, a building, a capybara…",
                    "Tap it to pop it. What was inside joins your trail.",
                    "Edge arrows and the Map show where bubbles are.",
                    "A forest or hill that was harvested all the way turns back into a bubble, so you can plant it again.",
                },
            },
            new HelpTopic
            {
                Id = "place", Category = HelpCategory.Building, Demo = HelpDemo.Place,
                Thumb = Item("Capy House"),
                Title = "Put things on your island",
                Summary = "Drag them out of your trail onto the grass.",
                Keywords = "place plant put build drop trail inventory circle green red room",
                Steps = new[]
                {
                    "Things you buy or find follow you in a line behind your capybara: that's your trail.",
                    "Press one and drag it out onto the grass.",
                    "A circle shows under it. Green means there's room. Red means something is in the way, so move it a little.",
                    "Let go to plant it. Once it's planted it stays, so pick the spot well!",
                    "Things only go on land, never in the water.",
                },
            },
            new HelpTopic
            {
                Id = "buy", Category = HelpCategory.Building, Demo = HelpDemo.Buy,
                Thumb = Item("General Store"),
                Title = "Buy from a shop",
                Summary = "Tap what a shop has set out in front of it.",
                Keywords = "buy shop store purchase price cost spend",
                Steps = new[]
                {
                    "A shop sets out what it sells on the ground in front of it.",
                    "Tap a thing to see what it is and what it costs. Faded things cost more than you have right now.",
                    "Tap Purchase. It joins your trail, ready to plant.",
                },
            },
            new HelpTopic
            {
                Id = "shops", Category = HelpCategory.Building, Demo = HelpDemo.Shops,
                Thumb = Item("Bronze Store", "General Store"),
                Title = "Unlock more shops",
                Summary = "Every shop sells the next one.",
                Keywords = "unlock shop store new more next bronze silver steel gold general",
                Steps = new[]
                {
                    "Your first shop, the General Store, comes from Miso's first quest.",
                    "Every shop sells the next one: General Store, then Bronze, Silver, Steel and Gold Store.",
                    "Tap the next shop where your shop sets it out, then Purchase. It joins your trail.",
                    "Drag it out and plant it: the new shop opens and sets out its own wares, the next shop among them.",
                    "Anything you can't afford yet looks faded. The Silver, Steel and Gold Stores are priced from how much you have and earn, so save up a little first.",
                    "The Steel Store also sells the Legendary Gem Store and the Bank.",
                    "Some things wait on a shop's shelf until a quest asks for them.",
                },
            },
            new HelpTopic
            {
                Id = "pests", Category = HelpCategory.Building, Demo = HelpDemo.Pests,
                Thumb = Item("Deer Bomb", "Bomb"),
                Title = "Pests and bombs",
                Summary = "Guinea pigs and zombie capybaras, and how to send them off.",
                Keywords = "pests guinea pig zombie bomb shoo fizz eat flowers wreck",
                Steps = new[]
                {
                    "Guinea pigs nibble your flowers. Zombie capybaras wreck trees and rocks.",
                    "Buy a Shoo Bomb at the Bronze Store and plant it right next to them. It fizzes for a few seconds, then they vanish in a puff, and nothing else is touched.",
                    "A Fizz Bomb (Silver Store) clears pests too, but it also turns everything nearby back into bubbles, workers too.",
                    "Or tap a zombie capybara and Pay 20 Gems for a warm bath: then it joins your trail and works for you.",
                },
            },
            new HelpTopic
            {
                Id = "burrow", Category = HelpCategory.Building, Demo = HelpDemo.Burrow,
                Thumb = Item("Monster Den"),
                Title = "Your burrow",
                Summary = "A cosy home under the ground to decorate.",
                Keywords = "burrow home house den furniture decorate room inside",
                Steps = new[]
                {
                    "Buy the Burrow Door at the General Store and plant it.",
                    "Tap it to slide down into your burrow.",
                    "The first time, a Furniture Shop waits in a bubble. Put it up outside, then tap it to shop.",
                    "Tap a piece in your storage to place it, and drag pieces to move them.",
                },
            },

            // ---- Islands and travel
            new HelpTopic
            {
                Id = "map", Category = HelpCategory.Travel, Demo = HelpDemo.Map,
                Thumb = () => IconPainter.Map,
                Title = "Fast travel with the map",
                Summary = "Drag yourself on the map to jump there.",
                Keywords = "map travel fast teleport jump island go pin far",
                Steps = new[]
                {
                    "Tap the Map button on the right.",
                    "Every island is on it. The ones a bridge joins to yours are open; a padlock means its bridge isn't open yet.",
                    "Press your own pin, the one that says You, drag it onto an open island and let go. You're there in a puff!",
                    "Drop it on a padlocked island and it springs back: open that island's bridge first.",
                    "Tap anywhere else on the map to take a quick look there.",
                },
                ShowMeLabel = "Open the map", ShowMe = MapScreen.Show,
            },
            new HelpTopic
            {
                Id = "bridges", Category = HelpCategory.Travel, Demo = HelpDemo.Bridge,
                Thumb = () => IconPainter.Globe,
                Title = "Open new islands",
                Summary = "Pay a spirit to raise a bridge to a new island.",
                Keywords = "island bridge new open spirit ghost cross unlock",
                Steps = new[]
                {
                    "New islands are reached by bridges. A spirit waiting on an island you can walk to will raise the next one.",
                    "Tap the spirit to see its price, then tap Purchase.",
                    "Up comes the bridge, with a red gate over its middle. The new island may have the next spirit waiting.",
                    "Walk across, or drag yourself there on the map.",
                    "Some spirits set their price by how much you make and how much you hold.",
                    "A few bridges open by themselves as the story goes on.",
                    "New islands bring new things to find.",
                },
            },

            // ---- Goals and gifts
            new HelpTopic
            {
                Id = "goals", Category = HelpCategory.Goals, Demo = HelpDemo.Goals,
                Thumb = () => IconPainter.Goals,
                Title = "What should I do next?",
                Summary = "Your next goal is always at the top of the screen.",
                Keywords = "goal quest next do task stuck what mission help",
                Steps = new[]
                {
                    "The goal card at the top always shows your next step. Tap it to see every goal and quest.",
                    "A yellow ! over someone means they have a quest for you, or a reward. Tap them to talk.",
                    "On the Goals screen, Show me walks you to the right spot.",
                },
                ShowMeLabel = "Open Goals", ShowMe = () => ObjectivesScreen.Show(ObjectivesScreen.Tab.Quests),
            },
            new HelpTopic
            {
                Id = "hearts", Category = HelpCategory.Goals, Demo = HelpDemo.Hearts,
                Thumb = () => HelpArt.Heart,
                Title = "Warm hearts",
                Summary = "Every goal you reach makes a heart for the Steam Tower.",
                Keywords = "heart warm achievement tower steam score",
                Steps = new[]
                {
                    "Every achievement you earn makes a glowing warm heart that follows you.",
                    "Tap it and it flies home to the Steam Tower.",
                    "Tap the Steam Tower to see your achievements, and how many hearts are still to bring home.",
                },
                ShowMeLabel = "See achievements", ShowMe = () => ObjectivesScreen.Show(ObjectivesScreen.Tab.Achievements),
            },
            new HelpTopic
            {
                Id = "gifts", Category = HelpCategory.Goals, Demo = HelpDemo.Gifts,
                Thumb = () => IconPainter.Gift,
                Title = "Daily gifts",
                Summary = "A present every day you play.",
                Keywords = "gift daily present streak every day reward free",
                Steps = new[]
                {
                    "There's a present every day you play. A red dot on the Gifts button means one is waiting.",
                    "Tap Gifts, then collect today's present.",
                    "Come back every day: the gifts grow bigger, and day 7 brings something to wear.",
                    "Miss a day and the streak starts again from day 1.",
                },
                ShowMeLabel = "Open Gifts", ShowMe = Gifts,
            },
            new HelpTopic
            {
                Id = "events", Category = HelpCategory.Goals, Demo = HelpDemo.Contest,
                Thumb = () => IconPainter.Trophy,
                Title = "Contests and events",
                Summary = "A new contest every three days, and a goal every day.",
                Keywords = "contest event competition prize win medal daily leaderboard",
                Steps = new[]
                {
                    "Tap Events on the right. Swipe up to see the podium and where you are.",
                    "A contest runs for three days at a time: most fish, bugs or birds wins.",
                    "You take part with the hobbies you have. The top three win gems, and first place a medal to wear.",
                    "There's a special goal every day too.",
                    "Prizes wait in Events until you claim them.",
                },
                ShowMeLabel = "Open Events", ShowMe = Events,
            },
            new HelpTopic
            {
                Id = "wardrobe", Category = HelpCategory.Goals, Demo = HelpDemo.Wardrobe,
                Thumb = () => IconPainter.Hanger,
                Title = "Dress up your capybara",
                Summary = "Tap your picture at the top left, then Change outfit.",
                Keywords = "clothes outfit hat wardrobe dress wear costume look",
                Steps = new[]
                {
                    "Tap your picture at the top left, then Change outfit.",
                    "Tap a piece you have to put it on. Tap it again to take it off.",
                    "New pieces come from goals, daily gifts, full journals, contests, and hats you sew yourself.",
                    "Your cats can dress up too: tap a cat you've adopted, then Dress up.",
                },
                ShowMeLabel = "Open my profile", ShowMe = ProfileScreen.Show,
            },

            // ---- Hobbies
            new HelpTopic
            {
                Id = "hobbies", Category = HelpCategory.Hobbies, Demo = HelpDemo.HobbyHub,
                Thumb = () => IconPainter.Rod,
                Title = "Hobbies",
                Summary = "Fishing, bugs, birds, gardening, cooking, sewing, cats and more.",
                Keywords = "hobby hobbies journal level bag collection",
                Steps = new[]
                {
                    "Tap the Hobbies button on the right. Swipe the big cards to see every hobby; the ones you haven't started say how to start them.",
                    "On a hobby's card, tap Journal to see everything you've found so far, and what's still to find.",
                    "Tap Bag at the top to see what you've caught, grown and cooked.",
                },
                ShowMeLabel = "Open Hobbies", ShowMe = () => HobbiesScreen.Show(),
            },
            new HelpTopic
            {
                Id = "fishing", Category = HelpCategory.Hobbies, Demo = HelpDemo.Fishing, Found = "Fish_salmon",
                Thumb = Art("Rod"), Started = HobbyStarted(Hobby.Fishing),
                Title = "Go fishing",
                Summary = "Buy a rod from Skipper Nori on the beach.",
                Keywords = "fish fishing rod bait cast reel catch sea water nori mermaid shell",
                Steps = new[]
                {
                    "Skipper Nori sits on the beach with his wares on the sand. Tap the Fishing Rod, then Purchase.",
                    "Walk to the water's edge where fish shadows swim and tap Start fishing.",
                    "When a fish bites, hold to reel it in, and let go when the line turns red!",
                    "Nori's bait brings bites sooner, or makes the line strain less.",
                    "From fishing level 4, toss a Mermaid's Shell into the sea. Everyone fishing inside its ring, friends too, gets more fish, and after five catches a shimmering fish appears.",
                },
                ShowMeLabel = "Open Hobbies", ShowMe = () => HobbiesScreen.Show(),
            },
            new HelpTopic
            {
                Id = "bugs", Category = HelpCategory.Hobbies, Demo = HelpDemo.Bugs, Found = "Bug_Butterfly",
                Thumb = Art("Net"), Started = HobbyStarted(Hobby.Bugs),
                Title = "Catch bugs",
                Summary = "Buy a Bug Net at the Bronze Store.",
                Keywords = "bug bugs insect butterfly net catch beetle",
                Steps = new[]
                {
                    "Buy a Bug Net at the Bronze Store.",
                    "Bugs settle on flowers, trees and grass near you. Tap one from a few steps away to swing your net.",
                    "Don't walk right up to one, or it flies away!",
                },
                ShowMeLabel = "Open Hobbies", ShowMe = () => HobbiesScreen.Show(),
            },
            new HelpTopic
            {
                Id = "birds", Category = HelpCategory.Hobbies, Demo = HelpDemo.Birds, Found = "Bird_robin",
                Thumb = Art("Binoculars"), Started = HobbyStarted(Hobby.Birding),
                Title = "Watch birds",
                Summary = "Buy Binoculars at the Silver Store.",
                Keywords = "bird birds watch binoculars spot birdwatching",
                Steps = new[]
                {
                    "Buy Binoculars at the Silver Store.",
                    "Birds land in trees near you. Tap one from a few steps away to spot it.",
                    "Get too close and it flies off!",
                },
                ShowMeLabel = "Open Hobbies", ShowMe = () => HobbiesScreen.Show(),
            },
            new HelpTopic
            {
                Id = "gardening", Category = HelpCategory.Hobbies, Demo = HelpDemo.Garden, Found = "Crop_Carrot",
                Thumb = Art("Crop_Carrot"), Started = HobbyStarted(Hobby.Gardening),
                Title = "Grow vegetables",
                Summary = "Plant seeds on Sunflower Farm.",
                Keywords = "garden gardening farm vegetables seeds plant grow harvest crops plot",
                Steps = new[]
                {
                    "Pay the spirit at the south end of Pebble Cove to raise the bridge to Sunflower Farm.",
                    "Buy seeds at the barn on the farm, then drag them from your trail onto an empty plot.",
                    "They grow while you're away. Tap the plot to harvest!",
                    "Your harvest goes in your bag: open Hobbies, then Bag.",
                    "More plots and seeds open as your gardening level grows.",
                },
                ShowMeLabel = "Open Hobbies", ShowMe = () => HobbiesScreen.Show(),
            },
            new HelpTopic
            {
                Id = "cooking", Category = HelpCategory.Hobbies, Demo = HelpDemo.Cooking, Found = "Dish_Pancakes",
                Thumb = Art("Stove"), Started = HobbyStarted(Hobby.Cooking),
                Title = "Cooking",
                Summary = "Buy a stove from Chef Tofu by the hot spring.",
                Keywords = "cook cooking food dish recipe stove kitchen tofu chef order",
                Steps = new[]
                {
                    "Buy a stove from Chef Tofu by the hot spring, put it down, and tap it.",
                    "Drag ingredients into the pot, press Cook, and tap the spatula in time to keep the heat down.",
                    "Chef Tofu sells recipes, eggs and milk.",
                    "Islanders with a dish in a bubble over their head will pay for it: tap the bubble to hand it over.",
                },
                ShowMeLabel = "Open Hobbies", ShowMe = () => HobbiesScreen.Show(),
            },
            new HelpTopic
            {
                Id = "sewing", Category = HelpCategory.Hobbies, Demo = HelpDemo.Sewing,
                Thumb = Art("SewingMachine"), Started = HobbyStarted(Hobby.Sewing),
                Title = "Sew hats",
                Summary = "Buy a Sewing Machine at the Steel Store.",
                Keywords = "sew sewing hat make design cotton silk machine clothes",
                Steps = new[]
                {
                    "Buy a Sewing Machine at the Steel Store, put it down, and tap it.",
                    "Pick a hat: a Beanie needs 3 cotton from your garden; a Fedora needs silk from silkworms.",
                    "Paint it: pick a colour, then brush, fill or stick on stickers.",
                    "Tap Sew & wear: it goes in your wardrobe and on your head!",
                },
                ShowMeLabel = "Open Hobbies", ShowMe = () => HobbiesScreen.Show(),
            },
            new HelpTopic
            {
                Id = "cats", Category = HelpCategory.Hobbies, Demo = HelpDemo.Cats, Found = "Cat_Ginger",
                Thumb = Art("Cat_Ginger"),
                Title = "Make friends with cats",
                Summary = "Pet stray cats until they trust you, then adopt them.",
                Keywords = "cat cats kitten stray pet adopt name presents fish",
                Steps = new[]
                {
                    "Stray cats wander onto your island every so often. Tap one to say hello.",
                    "Pet it (+1 trust), or give it a fish you caught (+3 trust). Each cat can be fussed once every few minutes.",
                    "Once it trusts you, tap it to adopt it. Then you can name it and dress it up.",
                    "Cats bring presents. Junk they bring can be sold to Salty Kombu.",
                },
                ShowMeLabel = "Open Hobbies", ShowMe = () => HobbiesScreen.Show(),
            },
            new HelpTopic
            {
                Id = "ema", Category = HelpCategory.Hobbies, Demo = HelpDemo.Ema,
                Thumb = Art("Ema"),
                Title = "Hang a wish at the shrine",
                Summary = "Write an ema at Old Mochi's shrine.",
                Keywords = "ema wish shrine write mochi plaque note friends",
                Steps = new[]
                {
                    "Old Mochi keeps the shrine on the hill. Tap the shrine to see the ema rack.",
                    "Hang a new ema with your wish on it. It's an offering of one gold coin.",
                    "Friends who visit can read it, and you can hang one on theirs.",
                    "Tap an ema on the rack to read it.",
                },
            },

            // ---- Friends
            new HelpTopic
            {
                Id = "visit", Category = HelpCategory.Friends, Demo = HelpDemo.Visit,
                Thumb = () => IconPainter.Plane,
                Title = "Visit other islands",
                Summary = "Build an airstrip, then fly to friends.",
                Keywords = "visit fly friends neighbours plane airstrip island travel help",
                Steps = new[]
                {
                    "First build an airstrip: tap Friends, then Build on Build an airstrip (300 wood and 300 gold). Pop its bubble and drag the airstrip to a clear spot.",
                    "Tap Friends, scroll down to your friends and tap Visit next to one who's online. You can also tap an island at the top.",
                    "Lend a hand there: plant flower gardens for hungry workers, and chase pests off with Shoo Bombs.",
                    "Tap Home on the right to fly back.",
                },
                ShowMeLabel = "Open Friends", ShowMe = Neighbours,
            },
            new HelpTopic
            {
                Id = "friendcode", Category = HelpCategory.Friends, Demo = HelpDemo.FriendCode,
                Thumb = () => IconPainter.AddFriend,
                Title = "Add a friend",
                Summary = "Share your friend code.",
                Keywords = "friend code add request share real online knock",
                Steps = new[]
                {
                    "Tap Friends and scroll down to find your friend code, like Miso#1234. Tap Copy and send it to a friend.",
                    "Type a friend's code in the box and tap Add to send a request. You're friends once they accept.",
                    "To visit a friend who's online, tap Visit: you knock, and they let you in.",
                    "On someone else's island, tap Add on the right to make them your friend.",
                },
                ShowMeLabel = "Open Friends", ShowMe = Neighbours,
            },
            new HelpTopic
            {
                Id = "together", Category = HelpCategory.Friends, Demo = HelpDemo.Together,
                Thumb = () => IconPainter.FaceSmile,
                Title = "Play together",
                Summary = "Parties, contests, building together, and emojis.",
                Keywords = "together party contest build co-op emoji wave heart chat",
                Steps = new[]
                {
                    "On a friend's island, tap Build together under their picture to ask if you can build there too.",
                    "In Friends, Play together starts an island party, or a fishing, bug or birdwatching contest for a hobby you have.",
                    "When someone else is on the island, tap the smiley button to send a wave, a heart or a smile.",
                },
                ShowMeLabel = "Open Friends", ShowMe = Neighbours,
            },

            // ---- Settings and your account
            new HelpTopic
            {
                Id = "account", Category = HelpCategory.Account, Demo = HelpDemo.Account,
                Thumb = () => IconPainter.Cloud,
                Title = "Keep your island safe",
                Summary = "Make an account to play on another device.",
                Keywords = "account sign up login save backup cloud google password device phone",
                Steps = new[]
                {
                    "Your island saves by itself as you play.",
                    "To play on another phone or computer, make an account: in Settings, find Play on another device and tap Make account.",
                    "Continue with Google, or pick a username and password.",
                    "On the other device, open Settings and tap Sign in.",
                    "If the two islands are different, you choose which one to keep.",
                    "Accounts need the internet: offline, these rows are hidden.",
                },
                ShowMeLabel = "Make an account", ShowMe = AccountScreen.ShowMake,
            },
            new HelpTopic
            {
                Id = "savecode", Category = HelpCategory.Account, Demo = HelpDemo.SaveCode,
                Thumb = () => IconPainter.Star,
                Title = "Keep a copy of your island",
                Summary = "Export a save code, and load it back later.",
                Keywords = "save code copy export import load backup restore",
                Steps = new[]
                {
                    "In Settings, tap Export next to Keep a copy of your island, then Copy, and paste the code into a note.",
                    "To bring it back, tap Load next to Bring back a saved island, tap Paste, then Load.",
                    "Loading replaces the island you have now.",
                },
                ShowMeLabel = "Open Settings", ShowMe = Settings,
            },
            new HelpTopic
            {
                Id = "reading", Category = HelpCategory.Account, Demo = HelpDemo.Reading,
                Thumb = () => IconPainter.Book,
                Title = "Bigger text and language",
                Summary = "Easy Reading, and English or Hebrew.",
                Keywords = "text size big read reading language hebrew english easy sound music",
                Steps = new[]
                {
                    "Tap Settings (the gear at the top right) and turn on Easy Reading: every word in the game gets bigger.",
                    "Tap A+ or A− to choose how big. The sentence under them shows the size.",
                    "With Easy Reading on, tap any words to read them in a large window. On a button or a speech bubble, press and hold the words instead.",
                    "Under Language, tap English or עברית to change the language.",
                    "Settings also has the sound, and Show me around to see the tour again.",
                },
                ShowMeLabel = "Open Settings", ShowMe = Settings,
            },
        };
    }
}
