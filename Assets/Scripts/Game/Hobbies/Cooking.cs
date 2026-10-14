using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Cooking: turn crops, fish and Chef Tofu's pantry goods into dishes, then serve a dish to fill every
    /// hungry worker on the island at once — your own, or a friend's on a visit (which counts as
    /// lending a hand).
    /// </summary>
    public static class Cooking
    {
        public const string Bizarre = "Bizarre";

        /// <summary>
        /// The dish a pot of ingredients makes, or null if it makes nothing anyone has a recipe for
        /// (which, cooked anyway, comes out as Bizarre Food). Fish are all just "fish:any". A dish
        /// you don't know yet still matches, up to your level: cook it well and you've worked out
        /// its recipe.
        /// </summary>
        public static DishKind Match(System.Collections.Generic.IReadOnlyList<string> pot)
        {
            int level = HobbyLog.Level(Hobby.Cooking);
            foreach (var dish in HobbyData.Dishes)
            {
                if (dish.MinLevel > level) continue;
                int total = 0;
                bool ok = true;
                foreach (var (item, count) in dish.Needs)
                {
                    total += count;
                    int have = 0;
                    foreach (var p in pot) if (p == item) have++;
                    if (have != count) { ok = false; break; }
                }
                if (ok && total == pot.Count) return dish;
            }
            return null;
        }

        /// <summary>
        /// Finishes a cook at the stove: the ingredients are used up either way; a matched recipe
        /// cooked without burning becomes its dish, anything else Bizarre Food. Returns the bag id.
        /// </summary>
        public static string Finish(System.Collections.Generic.IReadOnlyList<string> pot, DishKind dish, bool burnt, int stars)
        {
            foreach (var item in pot) HobbyLog.Take(item);
            bool good = dish != null && !burnt;
            string id = "dish:" + (good ? dish.Id : Bizarre);
            if (good) HobbyLog.LearnRecipe(dish.Id, save: false);
            HobbyLog.AddToBag(id);
            HobbyLog.AddXp(Hobby.Cooking, good ? 6 + dish.MinLevel * 3 + stars * 2 : 2);
            if (good) Motes.Trigger("hobby_cook");
            Sfx.Play(good ? SfxKind.Chest : SfxKind.Pop, 0.7f);
            return id;
        }
        public static bool CanCook(DishKind dish)
        {
            if (dish == null || dish.MinLevel > HobbyLog.Level(Hobby.Cooking) || !HobbyLog.KnowsRecipe(dish.Id)) return false;
            foreach (var (item, count) in dish.Needs) if (!HobbyLog.Has(item, count)) return false;
            return true;
        }

        public static bool Cook(DishKind dish)
        {
            if (!CanCook(dish)) return false;
            foreach (var (item, count) in dish.Needs) HobbyLog.Take(item, count);
            HobbyLog.AddToBag("dish:" + dish.Id);
            HobbyLog.AddXp(Hobby.Cooking, 10 + dish.MinLevel * 4);
            Motes.Trigger("hobby_cook");
            Sfx.Play(SfxKind.Chest, 0.6f);
            return true;
        }

        /// <summary>Serves a dish to every hungry worker here. Returns how many were fed (0 keeps the dish).</summary>
        public static int Serve(string dishId)
        {
            var hungry = new System.Collections.Generic.List<Worker>();
            foreach (var w in Object.FindObjectsByType<Worker>(FindObjectsSortMode.None))
                if (w.IsHungry) hungry.Add(w);
            if (dishId == Bizarre) return 0;   // nobody will touch it
            if (hungry.Count == 0 || !HobbyLog.Take("dish:" + dishId)) return 0;
            foreach (var w in hungry)
            {
                w.FeedFromFriend();
                VisitorBot.ShowEmoji(w.transform, Emoji.Heart);
            }
            HobbyLog.AddXp(Hobby.Cooking, 4 * hungry.Count);
            if (Visit.Active)
            {
                Motes.Trigger("visit_feed");
                if (VisitorBot.Current != null) VisitorBot.Current.Emote(Emoji.Heart);
            }
            return hungry.Count;
        }
    }
}
