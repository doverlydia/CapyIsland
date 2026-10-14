using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Bits of love (Rewards/Motes.as). Each mote is a one-shot achievement: triggered by its func,
    /// known to quests by its name. The first time one fires it is marked collected, a heart appears
    /// beside the king, and every quest giver hears its name — which is how every quest objective in
    /// the game completes.
    ///
    /// Quests only need the mote to have fired; tapping the heart (which flies it to the Tower of
    /// Love) only matters for the Tower.
    /// </summary>
    public static class Motes
    {
        /// <summary>Raised once per mote, with its definition, the moment it is collected.</summary>
        public static event Action<MoteDef> Triggered;

        static readonly HashSet<string> _collected = new();    // by name
        static readonly HashSet<string> _clicked = new();      // hearts delivered to the Tower

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            _collected.Clear();
            _clicked.Clear();
            Triggered = null;
        }

        /// <summary>
        /// Achievements that are no longer in the game: visiting the original's web forum (nothing
        /// here can trigger it, so the island could never be saved), and logging in a second day,
        /// which the daily sign-in reward replaces. They never fire and do not count.
        /// </summary>
        static readonly HashSet<string> Retired = new() { "visit_forums", "welcome_back", Wedding };

        /// <summary>
        /// The wedding is not a heart: it is what the hearts are for. Every other heart home wakes
        /// Old Yuzu, and she marries you at the chapel. It is still remembered with the rest
        /// (<see cref="Record"/>), so the island knows you are married.
        /// </summary>
        public const string Wedding = "wedding_mote";

        /// <summary>Achievements this game adds of its own.</summary>
        static readonly MoteDef[] Added =
        {
            new MoteDef("Dressed Up", 40, "dress_up"),
        };

        /// <summary>The visiting achievements, always available.</summary>
        static readonly MoteDef[] Visiting =
        {
            new MoteDef("Good Neighbour", 41, "visit_three"),
            new MoteDef("Lunch Delivery", 42, "visit_feed"),
            new MoteDef("Pest Control", 43, "visit_pests"),
            new MoteDef("First Catch", 44, "hobby_fish"),
            new MoteDef("Bird Spotter", 45, "hobby_bird"),
            new MoteDef("Published Author", 46, "hobby_book"),
            new MoteDef("Fashion Designer", 47, "hobby_sew"),
            new MoteDef("Green Thumb", 48, "hobby_garden"),
            new MoteDef("Home Cooking", 49, "hobby_cook"),
            new MoteDef("Bug Catcher", 50, "hobby_bug"),
            new MoteDef("Cat Person", 51, "hobby_cat"),
            new MoteDef("Ready for Take-off", 52, "built_airport"),
            new MoteDef("It's the Thought That Counts", 53, "hobby_catgift"),
        };

        /// <summary>Every achievement still in the game, in order.</summary>
        public static IEnumerable<MoteDef> All
        {
            get
            {
                foreach (var m in QuestData.Motes)
                    if (!Retired.Contains(m.Func)) yield return m;
                // Only once the wardrobe has art: an achievement nobody can earn is worse than none.
                if (Wardrobe.Ready)
                    foreach (var m in Added) yield return m;
                foreach (var m in Visiting) yield return m;
            }
        }

        public static int CollectedCount { get { int n = 0; foreach (var m in All) if (_collected.Contains(m.Name)) n++; return n; } }
        public static int ClickedCount { get { int n = 0; foreach (var m in All) if (_clicked.Contains(m.Name)) n++; return n; } }
        public static int Total { get { int n = 0; foreach (var _ in All) n++; return n; } }
        public static IEnumerable<string> Collected => _collected;
        public static IEnumerable<string> Clicked => _clicked;

        /// <summary>True once the mote with this name has fired.</summary>
        public static bool Has(string name) => _collected.Contains(name);

        public static bool IsClicked(string name) => _clicked.Contains(name);

        public static bool TryGetByFunc(string func, out MoteDef def)
        {
            foreach (var m in All)
                if (m.Func == func) { def = m; return true; }
            def = default;
            return false;
        }

        public static bool TryGetByName(string name, out MoteDef def)
        {
            foreach (var m in QuestData.Motes)
                if (m.Name == name) { def = m; return true; }
            def = default;
            return false;
        }

        /// <summary>Fires a mote by its func (TriggerMote). Does nothing if it has already fired.</summary>
        public static void Trigger(string func)
        {
            // On a friend's island only the visiting achievements can be earned: their island
            // is not where your own story moves on.
            if (Visit.Active && !func.StartsWith("visit_") && !func.StartsWith("hobby_")) return;
            if (!TryGetByFunc(func, out var def) || _collected.Contains(def.Name)) return;
            // A task's heart only once the task has been given (QuestSystem.Advance rechecks then).
            if (QuestSystem.Instance != null && QuestSystem.Instance.WaitsForTask(def.Name)) return;
            _collected.Add(def.Name);
            NewStuff.Add("goals/" + def.Name);
            Triggered?.Invoke(def);
        }

        /// <summary>
        /// Remembers something that happened without it being a heart (the wedding): no heart
        /// appears and nothing counts it, but <see cref="Has"/> knows and it is saved.
        /// </summary>
        public static void Record(string func)
        {
            foreach (var m in QuestData.Motes)
                if (m.Func == func) { _collected.Add(m.Name); return; }
        }

        /// <summary>A heart has reached the Tower of Love.</summary>
        public static void MarkClicked(string name) => _clicked.Add(name);

        /// <summary>Restores saved state without firing anything.</summary>
        public static void Restore(IEnumerable<string> collected, IEnumerable<string> clicked)
        {
            _collected.Clear(); _clicked.Clear();
            foreach (var n in collected) _collected.Add(n);
            foreach (var n in clicked) _clicked.Add(n);
        }

        /// <summary>After buying or collecting something (Motes.PurchaseMote).</summary>
        public static void PurchaseMote(string name)
        {
            switch (name)
            {
                case "Fox":
                    int foxes = StoreData.CountByName("Fox");
                    if (foxes >= 2) Trigger("fox1");
                    if (foxes >= 6) Trigger("fox5");
                    break;
                case "Panda": Trigger("panda1"); break;
                case "Church": Trigger("built_church"); break;
                case "Wedding Dress": Trigger("girl_quest_2"); break;
                case "String of Pearls": Trigger("girl_quest_6"); break;
                case "Tiara": Trigger("girl_crown"); break;
            }
        }

        /// <summary>
        /// A task was just given: whatever already stands on the island (or rides in your trail)
        /// that does it counts now, as if it had just been bought or planted.
        /// </summary>
        public static void Recheck()
        {
            foreach (var tag in UnityEngine.Object.FindObjectsByType<CatalogItem>(FindObjectsSortMode.None))
            {
                var placeable = tag.GetComponent<Placeable>();
                if (placeable != null && placeable.Placed) PlacementMote(tag.StoreName);
                PurchaseMote(tag.StoreName);
            }
        }

        /// <summary>After planting something (Motes.PlacementMote).</summary>
        public static void PlacementMote(string name)
        {
            switch (name)
            {
                case "Silver Store": Trigger("silver_store"); break;
                case "Steel Store": Trigger("steel_store"); break;
                case "Gold Store": Trigger("gold_store"); break;
                case "Legendary Gem Store": Trigger("gem_store"); break;
                case "Holy Statue": Trigger("holy_statue"); break;
                case "Bronze Store": Trigger("bronze_store"); break;
                case "Mine": Trigger("place_mine"); break;
            }
            if (name.Contains("Tulip")) Trigger("girl_quest_1");
            if (name.Contains("Rose")) Trigger("girl_quest_5");
        }
    }
}
