using System;
using System.Globalization;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A gift for every day you come back, growing through a seven-day streak: wood and gold, then
    /// gems, then on day seven a piece for the wardrobe. Miss a day and the streak starts again
    /// from day one. Kept in its own preferences, so it follows the player rather than the island.
    ///
    /// Amounts are small next to the 1000 wood and 1500 gold a new island starts with, so they are
    /// a welcome, not a shortcut.
    /// </summary>
    public static class DailyGifts
    {
        public struct Gift
        {
            public int Wood, Gold, Gems;
            public bool Outfit;
        }

        public static readonly Gift[] Week =
        {
            new() { Wood = 100 },
            new() { Gold = 100 },
            new() { Wood = 200 },
            new() { Gold = 200 },
            new() { Gems = 10 },
            new() { Wood = 300, Gold = 300 },
            new() { Outfit = true },
        };

        /// <summary>Day 7 when every daily outfit is already yours.</summary>
        public const int OutfitFallbackGems = 25;

        const string LastKey = "CapyIsland.Daily.Last";
        const string StreakKey = "CapyIsland.Daily.Streak";
        const string Format = "yyyy-MM-dd";

        static string Today => DateTime.Now.ToString(Format, CultureInfo.InvariantCulture);
        static string Yesterday => DateTime.Now.AddDays(-1).ToString(Format, CultureInfo.InvariantCulture);
        static string Last => PlayerPrefs.GetString(LastKey, "");
        static int Streak => PlayerPrefs.GetInt(StreakKey, 0);

        /// <summary>Today's gift has not been collected yet.</summary>
        public static bool CanClaim => Last != Today;

        /// <summary>Which day of the week-long streak today is, 1 to 7.</summary>
        public static int CurrentDay
        {
            get
            {
                if (Last == Today) return Mathf.Clamp(Streak, 1, 7);
                if (Last == Yesterday) return Streak % 7 + 1;
                return 1;
            }
        }

        /// <summary>
        /// Collects today's gift: resources go straight into the store, an outfit into the wardrobe.
        /// Returns the outfit given, if any.
        /// </summary>
        public static WearItem Claim()
        {
            if (!CanClaim) return null;
            int day = CurrentDay;
            var gift = Week[day - 1];
            WearItem outfit = null;

            var store = ResourceStore.Instance;
            if (gift.Outfit)
            {
                outfit = Wardrobe.NextDailyPiece();
                if (outfit != null) Wardrobe.Give(outfit);
                else if (store != null) store.Add(ResourceKind.Gem, OutfitFallbackGems);
            }
            if (store != null)
            {
                if (gift.Wood > 0) store.Add(ResourceKind.Wood, gift.Wood);
                if (gift.Gold > 0) store.Add(ResourceKind.Gold, gift.Gold);
                if (gift.Gems > 0) store.Add(ResourceKind.Gem, gift.Gems);
            }

            PlayerPrefs.SetString(LastKey, Today);
            PlayerPrefs.SetInt(StreakKey, day);
            PlayerPrefs.Save();
            return outfit;
        }
    }
}
