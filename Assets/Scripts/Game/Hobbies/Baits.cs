using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>A bait Skipper Nori sells: it makes the next cast easier, one way or another.</summary>
    public class BaitKind
    {
        public string Id, Name, Blurb;
        public float BiteSpeed = 1f;   // 2 = fish come over and bite twice as soon
        public float Gentle = 1f;      // multiplies how fast the line strains while reeling
        public int Pack, Gold, Gems;   // what a pack costs
        public string Item => "bait:" + Id;
    }

    /// <summary>
    /// Baits: bought in packs from the fisherman, kept in the bag, and one used up with every
    /// cast. You pick which one is on the hook from the tackle panel in fishing mode; with none
    /// chosen (or none left) you fish plain, as before.
    /// </summary>
    public static class Baits
    {
        const string SelectedKey = "CapyIsland.Bait";

        public static readonly BaitKind[] All =
        {
            new() { Id = "Worm",   Name = "Wiggly Worms", Blurb = "Fish come to your bobber and bite sooner.",
                    BiteSpeed = 2f, Pack = 5, Gold = 10 },
            new() { Id = "Shrimp", Name = "Sweet Shrimp", Blurb = "The line strains more slowly, so reeling in is easier.",
                    Gentle = 0.6f, Pack = 5, Gold = 25 },
            new() { Id = "Lure",   Name = "Golden Lure",  Blurb = "Quick bites and a gentle line, both at once.",
                    BiteSpeed = 2f, Gentle = 0.6f, Pack = 3, Gems = 2 },
        };

        /// <summary>
        /// The secret one, never sold: cook three cockroaches (cats bring them) in one pot and they
        /// come out crispy. Fish can't resist them.
        /// </summary>
        public static readonly BaitKind Roach = new()
        {
            Id = "Roach", Name = "Crispy Roach", Blurb = "Cooked by you, somehow. Fish can't resist it: they bite almost at once.",
            BiteSpeed = 3f, Gentle = 0.8f,
        };

        /// <summary>Every bait you might have in your tackle box: the shop's, and the secret one.</summary>
        public static readonly BaitKind[] Every = { All[0], All[1], All[2], Roach };

        public static BaitKind ById(string id) => System.Array.Find(Every, b => b.Id == id);

        public static int Count(BaitKind b) => b == null ? 0 : HobbyLog.Count(b.Item);

        /// <summary>The bait that will go on the hook next, or null for none.</summary>
        public static BaitKind Current
        {
            get
            {
                var b = ById(PlayerPrefs.GetString(SelectedKey, ""));
                return Count(b) > 0 ? b : null;
            }
        }

        public static void Select(BaitKind b)
        {
            PlayerPrefs.SetString(SelectedKey, b != null ? b.Id : "");
            PlayerPrefs.Save();
        }

        /// <summary>Takes one of the current bait out of the bag for a cast; null if fishing plain.</summary>
        public static BaitKind UseOne()
        {
            var b = Current;
            if (b == null || !HobbyLog.Take(b.Item)) return null;
            return b;
        }

        /// <summary>Buys one pack, if you can afford it. A bait you buy goes straight on the hook.</summary>
        public static bool Buy(BaitKind b)
        {
            var store = ResourceStore.Instance;
            if (b == null || store == null || !store.TrySpend(0, b.Gold, b.Gems)) return false;
            HobbyLog.AddToBag(b.Item, b.Pack);
            if (Current == null) Select(b);
            Sfx.Play(SfxKind.Buy, 0.6f);
            return true;
        }
    }
}
