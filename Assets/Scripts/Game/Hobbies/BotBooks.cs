using System.Collections.Generic;

namespace CapyIsland.Game
{
    /// <summary>
    /// The ema other islanders have hung: little wishes and thank-yous you can read on their
    /// shrine's rack, or that a visiting friend leaves on yours. Each islander has their own one or
    /// two, always the same ones, so their rack feels like theirs.
    /// </summary>
    public static class BotBooks
    {
        public struct Story { public string Title, Text; }

        static readonly Story[] Library =
        {
            new() { Title = "Please, one more nap", Text =
                "Dear gods,\n\nI wish for a nap so long that a burrow capybara digs a hole next to my ear and someone balances a yuzu on my head.\n\nThis happened last week. I would like it to happen again." },
            new() { Title = "To the guinea pigs", Text =
                "I wish the guinea pigs would leave my roses alone.\n\nI have planted them their own garden, far from the mill. They are welcome to it.\n\nPlease tell them. They do not listen to me." },
            new() { Title = "The big fish", Text =
                "I have caught four hundred sardines. I named my favourite one Gerald.\n\nI wish for the big shadow in the morning mist to bite, just once.\n\nGerald says hello." },
            new() { Title = "Wake up, spring", Text =
                "Dear Hot Spring,\n\nI planted a garden today and my workers sang while they chopped. I think you heard them.\n\nPlease wake up soon. We have saved you the best spot." },
            new() { Title = "Thank you for the dance", Text =
                "Thank you for Captain Wasabi, who taught me the Hot Pepper Hornpipe.\n\nShe says it has seven steps. I counted eleven. She says the falling over counts as style.\n\nI wish my feet better soon." },
            new() { Title = "For my woolly one", Text =
                "Please keep my woolly capybara exactly as slow as he is.\n\nHe stops to look at every cloud. But when it rains, everyone huddles against him, warm right through to the middle." },
            new() { Title = "Gently, gently", Text =
                "I wish to pop every bubble on the islands, gently.\n\nOld Yuzu filled them with warm air so nothing would be lost. It seems only polite." },
            new() { Title = "No more rocks for dinner", Text =
                "Thank you for my zombie capybara, who is brown again now and hums while he works.\n\nI wish he would stop eating the rock pile. We all have our habits, I suppose." },
            new() { Title = "A wish for the sea", Text =
                "I wish the sea to stay a little different every day.\n\nSome fish are for catching. Some are just for knowing they are there." },
            new() { Title = "Ten thank-yous", Text =
                "Thank you for: the smell of maple after rain, the mill's song, my first capy house (it leans), the pear tree that drops pears on my head, the kingfisher on Tuesdays, bubbles, the bridge I waited three days for, my friends' islands, my friends.\n\nAnd coming home." },
        };

        /// <summary>One or two ema from this islander's rack.</summary>
        public static List<Story> ShelfOf(FriendProfile who)
        {
            var list = new List<Story>();
            if (who == null) return list;
            var rng = new System.Random(who.seed);
            int first = rng.Next(Library.Length);
            list.Add(Library[first]);
            if (rng.NextDouble() < 0.6)
            {
                int second = (first + 1 + rng.Next(Library.Length - 1)) % Library.Length;
                list.Add(Library[second]);
            }
            return list;
        }
    }
}
