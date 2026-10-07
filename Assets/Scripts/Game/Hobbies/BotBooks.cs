using System.Collections.Generic;

namespace CapyIsland.Game
{
    /// <summary>
    /// The books other islanders have written: little stories you can read on their islands, or
    /// that a visiting friend leaves on yours. Each islander has their own one or two, always the
    /// same ones, so their shelf feels like theirs.
    /// </summary>
    public static class BotBooks
    {
        public struct Story { public string Title, Text; }

        static readonly Story[] Library =
        {
            new() { Title = "The Longest Nap", Text =
                "It started after lunch, under the maple by the mill.\n\nI only meant to close my eyes for a moment. When I opened them, the sun had moved to the other side of the tree, a burrow capybara had dug a hole beside my ear, and somebody had balanced a yuzu on my head.\n\nI never found out who. I kept the yuzu.\n\nThe moral of this story is that naps are excellent, and that you should always check your head afterwards." },
            new() { Title = "Guinea Pig Diaries", Text =
                "Day 1. A guinea pig ate my tulips.\n\nDay 2. It came back with a friend. They ate the roses.\n\nDay 3. I planted daisies, which everyone says guinea pigs do not like. Everyone was wrong.\n\nDay 4. I bought a fizz bomb. I could not do it. They looked so happy.\n\nDay 5. I planted a second garden, just for them, far away from the mill. Now they leave my workers' flowers alone, and sometimes they wave.\n\nI think we are friends now." },
            new() { Title = "How to Catch a Coelacanth", Text =
                "Step one: go fishing.\n\nStep two: go fishing again.\n\nStep three: catch four hundred sardines. Learn to love sardines. Name your favourite one Gerald.\n\nStep four: one morning, when the mist is thick and you have nearly given up, see the biggest shadow you have ever seen.\n\nStep five: wait. Do not reel at the nibbles. Wait for the tug.\n\nStep six: I will let you know when I get to step six." },
            new() { Title = "Letters to the Hot Spring", Text =
                "Dear Hot Spring,\n\nI know you are still cold. I know you have been cold for a long time. But I planted a garden today and it felt warm, and my workers sang while they chopped, and I think you heard them.\n\nI will keep building. I will keep finding warm hearts and sending them your way.\n\nPlease wake up soon. We have saved you the best spot.\n\nLove, an islander" },
            new() { Title = "Captain Wasabi Taught Me to Dance", Text =
                "She said the Hot Pepper Hornpipe has seven steps. I counted eleven. She said that was because I kept falling over and getting up again, and that this counted as style.\n\nWe danced until the moon came up over the harbour. My feet hurt for two days.\n\nWorth every gem." },
            new() { Title = "The Woolly One", Text =
                "My woolly capybara is the slowest worker on the island. He stops to look at every cloud. He stops to look at every flower. Once he stopped to look at a pebble for twenty minutes.\n\nBut when it rains, everyone huddles against him, because he is warm and dry right through to the middle.\n\nSome workers are fast. Some are the best place to be on a rainy day." },
            new() { Title = "A Very Small Book About Bubbles", Text =
                "Bubbles are where things wait.\n\nForests wait in bubbles. Houses wait in bubbles. Once I found a whole sawmill waiting in one, looking very patient.\n\nOld Yuzu says she filled them with warm air before the cold came, so that nothing would be lost.\n\nI pop them gently. It seems polite." },
            new() { Title = "My Zombie Capybara", Text =
                "When I found him he was grey and grumbly and covered in Gloomfen mud, and he tried to eat my rock pile.\n\nI gave him twenty gems for a warm bath.\n\nNow he is brown again, and he hums when he works, and he will not let anyone else carry his pickaxe.\n\nHe still eats the occasional rock. We all have our habits." },
            new() { Title = "Notes From the Shore", Text =
                "The sea is never the same twice.\n\nIn the morning the shadows come close to the sand and you can count them. At noon they drift out to the deeper blue. In the evening something big always swims past, just too far away to cast to.\n\nI have stopped minding. Some fish are for catching. Some are just for knowing they are there." },
            new() { Title = "Ten Things I Love About My Island", Text =
                "1. The smell of maple after rain.\n2. The sound the mill makes.\n3. My first capy house, which leans a little.\n4. The pear tree that drops pears on my head.\n5. The kingfisher who visits on Tuesdays.\n6. Bubbles.\n7. The bridge I waited three days to open.\n8. My friends' islands, which are nothing like mine.\n9. My friends.\n10. Coming home." },
        };

        /// <summary>One or two books from this islander's shelf.</summary>
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
