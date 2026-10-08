using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// What the player should do next, in one line — the tracker in the top-left corner — and the
    /// full list behind it: every quest in progress, and every achievement with what it asks for.
    ///
    /// Nothing here drives the game. It only reads the tutorial, the quest system and the motes, so
    /// it can never disagree with them.
    /// </summary>
    public static class Objectives
    {
        public enum Source { Tutorial, Talk, Reward, Quest, Achievement, Done, Visit }

        public struct Goal
        {
            public Source Source;
            public string Title;      // short, imperative
            public string From;       // who asked, if anyone
            public Vector2? Where;    // somewhere to look, if known
            public Sprite Icon;
            public string Step;       // something to do first (open a bridge), shown under the title
        }

        public struct Achievement
        {
            public MoteDef Def;
            public string Goal;
            public bool Done;
        }

        /// <summary>The single most useful thing to do right now.</summary>
        public static Goal Current()
        {
            BridgeGhost.Pointed = null;
            return Reachable(Pick());
        }

        /// <summary>
        /// If the goal is somewhere you cannot walk to yet (on an island you have not opened), it
        /// keeps its title and gains a step to do first — pay the bridge ghost — and the arrows
        /// point at that ghost, since that is where to go next.
        /// </summary>
        static Goal Reachable(Goal goal)
        {
            if (!goal.Where.HasValue) return goal;
            var step = StepTo(goal.Where.Value, out var ghost);
            if (step == null) return goal;
            BridgeGhost.Pointed = ghost;
            goal.Step = step;
            goal.Where = ghost.transform.position;
            return goal;
        }

        /// <summary>The step needed before you can reach <paramref name="where"/>, or null if you can walk there now.</summary>
        public static string StepTo(Vector2 where, out BridgeGhost ghost)
        {
            ghost = null;
            var land = LandMap.Instance;
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            if (land == null || king == null) return null;
            Vector2 you = king.transform.position;
            if (land.Reachable(you, where)) return null;
            ghost = BridgeGhost.Toward(where, you);
            if (ghost == null) return null;
            return "First: pay the ghost to open the bridge to " + StoryText.Name(ghost.BridgeName) + " (" + PriceText(ghost.Price) + ")";
        }

        static string PriceText(Vector3Int p)
        {
            var parts = new List<string>();
            if (p.x > 0) parts.Add(p.x + " wood");
            if (p.y > 0) parts.Add(p.y + " gold");
            if (p.z > 0) parts.Add(p.z + " gems");
            return parts.Count > 0 ? string.Join(", ", parts) : "free";
        }

        static Goal Pick()
        {
            if (Visit.Active)
                return new Goal { Source = Source.Visit, Title = "Help " + Visit.Host.name + ": feed hungry workers and chase off pests", From = Visit.Host.name };

            var tutorial = TutorialDirector.Instance;
            if (tutorial != null && !tutorial.Finished && !string.IsNullOrEmpty(tutorial.CurrentGoal))
                return new Goal { Source = Source.Tutorial, Title = tutorial.CurrentGoal, Where = tutorial.GoalPoint };

            // Someone waiting to hand over a reward beats everything: it is free and immediate.
            foreach (var giver in Givers())
                if (giver.RewardWaiting)
                    return new Goal { Source = Source.Reward, Title = "Collect your reward from " + giver.Speaker, From = giver.Speaker,
                        Where = giver.transform.position, Icon = giver.Portrait };

            // A quest in progress next.
            foreach (var giver in Givers())
            {
                var q = giver.CurrentQuest;
                if (q == null || q.Stage != QuestSystem.Stage.IntroRead || q.Complete) continue;
                return new Goal { Source = Source.Quest, Title = QuestGoal(q.Def), From = giver.Speaker, Icon = giver.Portrait, Where = QuestWhere(q.Def) };
            }

            // Then someone with a new quest to tell you about.
            foreach (var giver in Givers())
                if (giver.HasNews)
                    return new Goal { Source = Source.Talk, Title = "Talk to " + giver.Speaker, From = giver.Speaker,
                        Where = giver.transform.position, Icon = giver.Portrait };

            // Otherwise the next achievement still to earn.
            foreach (var a in AllAchievements())
                if (!a.Done) return new Goal { Source = Source.Achievement, Title = a.Goal };

            return new Goal { Source = Source.Done, Title = "Every goal complete. Enjoy your island!" };
        }

        /// <summary>Quests that have been started and not yet handed in.</summary>
        public static List<(QuestGiver giver, QuestSystem.Progress quest)> ActiveQuests()
        {
            var list = new List<(QuestGiver, QuestSystem.Progress)>();
            foreach (var giver in Givers())
            {
                var q = giver.CurrentQuest;
                if (q == null || q.Stage == QuestSystem.Stage.GiftReceived) continue;
                list.Add((giver, q));
            }
            return list;
        }

        public static List<Achievement> AllAchievements()
        {
            var list = new List<Achievement>();
            foreach (var m in Motes.All)
                list.Add(new Achievement { Def = m, Goal = GoalFor(m), Done = Motes.Has(m.Name) });
            return list;
        }

        /// <summary>The to-do line for a quest: what its completing achievement asks for.</summary>
        /// <summary>
        /// Where a quest's task happens, when it is about someone or somewhere in particular — so
        /// the arrows can point there, and a bridge still to open shows up as the step before it.
        /// </summary>
        public static Vector2? QuestWhere(QuestDef q)
        {
            if (string.IsNullOrEmpty(q.CompleteMote) || !Motes.TryGetByName(q.CompleteMote, out var m)) return null;
            switch (m.Func)
            {
                case "girl_quest_3":
                    foreach (var pete in UnityEngine.Object.FindObjectsByType<PiratePete>(FindObjectsSortMode.None))
                        if (pete.InWorld) return pete.transform.position;
                    return null;
                case "wedding_mote":
                    var church = UnityEngine.Object.FindFirstObjectByType<Church>();
                    return church != null ? church.transform.position : null;
                case "girl_quest_5":
                case "peach_dance":
                    return GiverOf(QuestGiver.Kind.Peach);
                case "girl_quest_1":
                case "girl_quest_10":
                    return GiverOf(QuestGiver.Kind.Coriander);
            }
            return null;
        }

        static Vector2? GiverOf(QuestGiver.Kind kind)
        {
            foreach (var g in UnityEngine.Object.FindObjectsByType<QuestGiver>(FindObjectsSortMode.None))
                if (g.GiverKind == kind) return g.transform.position;
            return null;
        }

        public static string QuestGoal(QuestDef q)
        {
            if (!string.IsNullOrEmpty(q.CompleteMote) && Motes.TryGetByName(q.CompleteMote, out var m)) return GoalFor(m);
            return "Finish the task you were given";
        }

        /// <summary>What an achievement asks you to do, as a short instruction.</summary>
        public static string GoalFor(MoteDef m) => m.Func switch
        {
            "first_mote" => "Plant flowers for a hungry capybara",
            "fox1" => "Bring a burrow capybara to the island",
            "fox5" => "Gather five burrow capybaras",
            "panda1" => "Find a woolly capybara",
            "silver_store" => "Build the Silver Store",
            "steel_store" => "Build the Steel Store",
            "gold_store" => "Build the Gold Store",
            "bronze_store" => "Build the Bronze Store",
            "gem_store" => "Build the Legendary Gem Store",
            "built_church" => "Build the Blossom Chapel",
            "holy_statue" => "Build a Spirit Statue",
            "place_mine" => "Place a mine",
            "girl_quest_1" => "Plant a tulip garden for Miso",
            "girl_quest_2" => "Buy the Wedding Dress",
            "girl_quest_3" => "Hear Salty Kombu's story",
            "girl_quest_5" => "Plant a rose garden for the Captain",
            "girl_quest_6" => "Buy the Pearl Necklace",
            "girl_quest_8" => "Chase every guinea pig off the island",
            "girl_quest_10" => "Keep helping Miso",
            "girl_crown" => "Buy the Tiara",
            "wedding_mote" => "Hold the wedding at the Blossom Chapel",
            "panda_sawmill" => "Put a woolly capybara to work at a sawmill",
            "5_plum" => "Grow five Legendary Plum Trees",
            "monster_tame" => "Warm up five zombie capybaras",
            "dress_up" => "Try on an outfit in the Wardrobe",
            "visit_three" => "Visit three friends' islands",
            "visit_feed" => "Feed a hungry worker on a friend's island",
            "visit_pests" => "Chase the pests off a friend's island",
            "hobby_fish" => "Catch a fish (walk to the shore)",
            "hobby_bird" => "Spot a bird in the trees",
            "hobby_book" => "Write a book in the Library",
            "hobby_sew" => "Sew your own T-shirt design",
            "hobby_garden" => "Grow and harvest a vegetable",
            "hobby_cook" => "Cook a dish from your bag",
            "hobby_bug" => "Catch a bug with your net",
            "hobby_cat" => "Befriend and adopt a stray cat",
            "hobby_catgift" => "Give a cat a fish, then wait for its present",
            "built_airport" => "Build an airstrip (Neighbours screen)",
            "peach_dance" => "Pay Captain Wasabi 500 gems for a dance",
            _ => m.Title,
        };

        static IEnumerable<QuestGiver> Givers()
        {
            // Coriander first, then Peach, then any ghosts: the main story leads.
            var all = Object.FindObjectsByType<QuestGiver>(FindObjectsSortMode.None);
            System.Array.Sort(all, (a, b) => a.GiverKind.CompareTo(b.GiverKind));
            foreach (var g in all)
                if (g.IsInWorld) yield return g;
        }
    }
}
