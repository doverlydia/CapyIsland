using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Cooking: turn crops and fish from your bag into dishes, then serve a dish to fill every
    /// hungry worker on the island at once — your own, or a friend's on a visit (which counts as
    /// lending a hand).
    /// </summary>
    public static class Cooking
    {
        public static bool CanCook(DishKind dish)
        {
            if (dish == null || dish.MinLevel > HobbyLog.Level(Hobby.Cooking)) return false;
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
