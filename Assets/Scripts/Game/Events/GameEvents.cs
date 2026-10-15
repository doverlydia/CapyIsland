using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    public enum ContestKind { Fish, Bugs, Birds }

    /// <summary>One line on a contest leaderboard.</summary>
    public struct ContestEntry
    {
        public string Name;
        public int Score;
        public bool You;
    }

    /// <summary>A contest that has ended, with its prize waiting to be claimed (once).</summary>
    public class ContestResult
    {
        public long Contest;
        public ContestKind Kind;
        public int Rank, Score, Gems;
        public WearItem Medal;
        public DateTime Start, End;   // its first day and the day it ended on (UTC dates)
    }

    /// <summary>The day's special goal: something particular to do before the day is out.</summary>
    public class SpecialGoal
    {
        public string Id, Title, Text;
        public Hobby Hobby;
        public int Need;
        public DayParts When = DayParts.Any;   // only catches at these times count
        public int MinStars;                   // only catches at least this rare count
        public int Gems, Gold;
        public string BaitReward;              // a pack of this bait instead of (or as well as) gems
    }

    /// <summary>
    /// Timed events, run from the real calendar so they mean the same thing every time you open
    /// the game:
    ///
    ///  * <b>Contests</b> last three days and take turns: most fish, then most bugs, then most birds
    ///    spotted. Bot capybaras fill the leaderboard, their totals growing over the three days the
    ///    way real players' would. When a contest ends the top three win gems, and first place
    ///    also wins that contest's gold medal for the wardrobe. A finished contest's prize waits
    ///    on the Events screen until you claim it, however many contests go by meanwhile.
    ///  * A <b>special goal</b> changes every day: catch five fish after dark, spot birds at dawn or
    ///    dusk, find something rare… with a reward to claim when it is done.
    ///
    /// You only take part in hobbies you have unlocked. Progress is kept in PlayerPrefs.
    /// </summary>
    public static class GameEvents
    {
        const string Key = "CapyIsland.Events";
        static readonly DateTime Epoch = new(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        public const int ContestDays = 3;

        public static readonly int[] PrizeGems = { 50, 30, 15 };
        public const int ThanksGems = 3;   // for taking part, outside the top three

        public static event Action Changed;

        [Serializable]
        class State
        {
            public long contest = -1;
            public int score;
            public long resolvedThrough = -1;   // last contest whose result was handed out
            public long goalDay = -1;
            public int goalProgress;
            public bool goalClaimed;
            public long lastContest = -1;        // the contest before this one, and how you did
            public int lastScore;
            public long seenContest = -1;        // what the Events screen last showed you
            public long seenGoalDay = -1;
            public List<Finished> unclaimed = new();   // ended contests you took part in, prize not yet claimed
        }

        [Serializable]
        class Finished
        {
            public long contest;
            public int score;
        }

        static State _s;
        static State S
        {
            get
            {
                if (_s != null) return _s;
                try { _s = JsonUtility.FromJson<State>(PlayerPrefs.GetString(Key, "")) ?? new State(); }
                catch (Exception) { _s = new State(); }
                return _s;
            }
        }

        static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(S));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        static DateTime Now => DateTime.UtcNow;

        // ---------------------------------------------------------------- contests

        public static long Contest => (long)Math.Floor((Now - Epoch).TotalDays / ContestDays);
        public static ContestKind KindOf(long contest) => (ContestKind)(int)(((contest % 3) + 3) % 3);
        public static ContestKind CurrentKind => KindOf(Contest);
        public static DateTime EndOf(long contest) => Epoch.AddDays((contest + 1) * ContestDays);
        public static TimeSpan ContestLeft => EndOf(Contest) - Now;
        static double Progress(long contest) => Math.Clamp(1.0 - (EndOf(contest) - Now).TotalDays / ContestDays, 0.0, 1.0);

        public static Hobby HobbyOf(ContestKind k) => k switch
        {
            ContestKind.Bugs => Hobby.Bugs,
            ContestKind.Birds => Hobby.Birding,
            _ => Hobby.Fishing,
        };

        public static bool CanJoin => HobbyLog.IsUnlocked(HobbyOf(CurrentKind));

        /// <summary>Your score in the running contest.</summary>
        public static int Score
        {
            get { Roll(); return S.score; }
        }

        /// <summary>When the calendar has moved on to a new contest, keep the old score for its results.</summary>
        static void Roll()
        {
            S.unclaimed ??= new List<Finished>();
            // Older saves kept only the one contest before this: its prize goes on the list too.
            if (S.lastContest >= 0 && S.lastContest > S.resolvedThrough && S.lastScore > 0 && !S.unclaimed.Exists(f => f.contest == S.lastContest))
            {
                S.unclaimed.Add(new Finished { contest = S.lastContest, score = S.lastScore });
                S.resolvedThrough = S.lastContest;
                Save();
            }
            long now = Contest;
            if (S.contest == now) return;
            if (S.contest >= 0)
            {
                S.lastContest = S.contest;
                S.lastScore = S.score;
                if (S.score > 0) S.unclaimed.Add(new Finished { contest = S.contest, score = S.score });
                S.resolvedThrough = S.contest;
            }
            S.contest = now;
            S.score = 0;
            Save();
        }

        /// <summary>The running contest's table, best first, you included.</summary>
        public static List<ContestEntry> Leaderboard() => Board(Contest, Score, Progress(Contest));

        public static int Rank()
        {
            var board = Leaderboard();
            for (int i = 0; i < board.Count; i++) if (board[i].You) return i + 1;
            return board.Count;
        }

        /// <summary>
        /// Nine bot rivals per contest, the same nine for everyone who looks: a keen one, a few
        /// steady ones and some casual ones. Each has a final total, reached gradually over the
        /// three days with the odd quiet spell, so the table moves while you play.
        /// </summary>
        static List<ContestEntry> Board(long contest, int yours, double progress)
        {
            var rnd = new System.Random((int)(contest * 7919 % int.MaxValue) + 17);
            var names = new List<string>(Friends.NamePool);
            var list = new List<ContestEntry>();
            for (int i = 0; i < 9; i++)
            {
                int pick = rnd.Next(names.Count);
                string name = names[pick];
                names.RemoveAt(pick);
                int final = i == 0 ? rnd.Next(38, 56)            // the keen one
                          : i < 4 ? rnd.Next(16, 34)              // steady
                          : rnd.Next(3, 16);                      // casual
                // Their own pace: some start strong, some catch up at the end.
                double pace = 0.7 + rnd.NextDouble() * 0.8;
                double reached = Math.Pow(progress, pace);
                list.Add(new ContestEntry { Name = name, Score = (int)Math.Round(final * reached) });
            }
            list.Add(new ContestEntry { Name = PlayerProfile.Name, Score = yours, You = true });
            // Ties go to you: you were there.
            list.Sort((a, b) => a.Score != b.Score ? b.Score.CompareTo(a.Score) : b.You.CompareTo(a.You));
            return list;
        }

        /// <summary>Ended contests you took part in whose prize is still to claim, oldest first.</summary>
        public static List<ContestResult> Unclaimed()
        {
            Roll();
            var list = new List<ContestResult>();
            foreach (var f in S.unclaimed) list.Add(ResultOf(f));
            list.Sort((a, b) => a.Contest.CompareTo(b.Contest));
            return list;
        }

        public static bool PrizeWaiting { get { Roll(); return S.unclaimed.Count > 0; } }

        static ContestResult ResultOf(Finished f)
        {
            var board = Board(f.contest, f.score, 1.0);
            int rank = board.FindIndex(e => e.You) + 1;
            var result = new ContestResult
            {
                Contest = f.contest, Kind = KindOf(f.contest), Rank = rank, Score = f.score,
                Start = Epoch.AddDays(f.contest * ContestDays), End = EndOf(f.contest).AddDays(-1),
            };
            result.Gems = rank <= PrizeGems.Length ? PrizeGems[rank - 1] : ThanksGems;
            if (rank == 1) result.Medal = Array.Find(Wardrobe.Items, w => w.Unlock == "contest_" + result.Kind);
            return result;
        }

        /// <summary>
        /// Hands over an ended contest's prize, once: gems for the top three (a little thank-you
        /// below that) and the medal for first. Null if it was already claimed.
        /// </summary>
        public static ContestResult Claim(long contest)
        {
            Roll();
            var f = S.unclaimed.Find(x => x.contest == contest);
            if (f == null) return null;
            var result = ResultOf(f);
            var store = ResourceStore.Instance;
            if (store == null) return null;   // nowhere to put the gems yet: it keeps
            store.Add(ResourceKind.Gem, result.Gems);
            if (result.Medal != null) Wardrobe.Give(result.Medal, announce: false);
            S.unclaimed.Remove(f);
            Save();
            return result;
        }

        // ---------------------------------------------------------------- the day's special goal

        static readonly SpecialGoal[] Goals =
        {
            new() { Id = "night_fish",  Title = "Night Fishing",   Text = "Catch 5 fish after dark.",                Hobby = Hobby.Fishing, Need = 5, When = DayParts.Night, Gems = 10 },
            new() { Id = "twilight",    Title = "Golden Hour",     Text = "Spot 3 birds at dawn or dusk.",           Hobby = Hobby.Birding, Need = 3, When = DayParts.Twilight, Gems = 10 },
            new() { Id = "bug_hunt",    Title = "Bug Hunt",        Text = "Catch 6 bugs.",                           Hobby = Hobby.Bugs,    Need = 6, BaitReward = "Shrimp" },
            new() { Id = "fancy_fish",  Title = "Something Fancy", Text = "Catch 3 fish that are uncommon or rarer.", Hobby = Hobby.Fishing, Need = 3, MinStars = 2, Gems = 12 },
            new() { Id = "birdwatch",   Title = "Birdwatcher",     Text = "Spot 5 birds.",                           Hobby = Hobby.Birding, Need = 5, Gold = 150 },
            new() { Id = "busy_angler", Title = "Busy Angler",     Text = "Catch 8 fish.",                           Hobby = Hobby.Fishing, Need = 8, BaitReward = "Worm", Gold = 80 },
            new() { Id = "night_bugs",  Title = "Moonlight Bugs",  Text = "Catch 3 bugs after dark.",                Hobby = Hobby.Bugs,    Need = 3, When = DayParts.Night, Gems = 10 },
        };

        static long Day => (long)Math.Floor((Now - Epoch).TotalDays);
        public static TimeSpan GoalLeft => Epoch.AddDays(Day + 1) - Now;

        /// <summary>Today's goal among the hobbies you have unlocked, or null if none are yet.</summary>
        public static SpecialGoal TodaysGoal
        {
            get
            {
                var open = new List<SpecialGoal>();
                foreach (var g in Goals) if (HobbyLog.IsUnlocked(g.Hobby)) open.Add(g);
                if (open.Count == 0) return null;
                return open[(int)(((Day * 5) % open.Count + open.Count) % open.Count)];
            }
        }

        public static int GoalProgress
        {
            get
            {
                if (S.goalDay != Day) { S.goalDay = Day; S.goalProgress = 0; S.goalClaimed = false; Save(); }
                return S.goalProgress;
            }
        }

        public static bool GoalClaimed { get { _ = GoalProgress; return S.goalClaimed; } }
        public static bool GoalDone { get { var g = TodaysGoal; return g != null && GoalProgress >= g.Need; } }

        /// <summary>True when something is waiting to be collected.</summary>
        public static bool RewardWaiting => GoalDone && !GoalClaimed;

        /// <summary>A contest or a day's goal you have not opened the Events screen to see yet.</summary>
        public static bool Unseen => S.seenContest != Contest || (TodaysGoal != null && S.seenGoalDay != Day);

        /// <summary>For the red dot on the Events button: something new, or something to collect.</summary>
        public static bool NeedsAttention => Unseen || RewardWaiting || PrizeWaiting;

        /// <summary>The Events screen has been looked at: what is running now is no longer news.</summary>
        public static void MarkSeen()
        {
            if (!Unseen) return;
            S.seenContest = Contest;
            S.seenGoalDay = Day;
            Save();
        }

        public static bool ClaimGoal()
        {
            var g = TodaysGoal;
            if (g == null || !GoalDone || S.goalClaimed) return false;
            S.goalClaimed = true;
            var store = ResourceStore.Instance;
            if (store != null)
            {
                if (g.Gems > 0) store.Add(ResourceKind.Gem, g.Gems);
                if (g.Gold > 0) store.Add(ResourceKind.Gold, g.Gold);
            }
            var bait = Baits.ById(g.BaitReward ?? "");
            if (bait != null) HobbyLog.AddToBag(bait.Item, bait.Pack);
            Sfx.Play(SfxKind.Chest, 0.7f);
            Save();
            return true;
        }

        /// <summary>The reward in words, for the goal card.</summary>
        public static string RewardText(SpecialGoal g)
        {
            var parts = new List<string>();
            if (g.Gems > 0) parts.Add($"{g.Gems} gems");
            if (g.Gold > 0) parts.Add($"{g.Gold} gold");
            var bait = Baits.ById(g.BaitReward ?? "");
            if (bait != null) parts.Add($"{bait.Pack} {bait.Name}");
            return string.Join(" + ", parts);
        }

        // ---------------------------------------------------------------- counting catches

        /// <summary>Called by the hobby log for every fish caught, bird spotted and bug caught.</summary>
        public static void Record(Hobby hobby, int stars)
        {
            Gathering.Caught(hobby);   // a live contest on someone's island counts it too
            Roll();
            bool changed = false;
            if (HobbyOf(CurrentKind) == hobby) { S.score++; changed = true; }

            var g = TodaysGoal;
            if (g != null && g.Hobby == hobby && GoalProgress < g.Need
                && (g.When & GameClock.Now) != 0 && stars >= g.MinStars)
            {
                S.goalProgress++;
                changed = true;
            }
            if (changed) Save();
        }

        /// <summary>"2 days", "5 hours", "12 minutes": how long is left, roughly.</summary>
        public static string Left(TimeSpan t)
        {
            if (t.TotalDays >= 1.5) return $"{(int)Math.Round(t.TotalDays)} days";
            if (t.TotalHours >= 1.5) return $"{(int)Math.Round(t.TotalHours)} hours";
            return $"{Math.Max(1, (int)Math.Ceiling(t.TotalMinutes))} minutes";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            _s = null;
            Changed = null;
        }
    }
}
