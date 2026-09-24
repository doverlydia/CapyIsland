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

        public static int CollectedCount => _collected.Count;
        public static int ClickedCount => _clicked.Count;
        public static int Total => QuestData.Motes.Length;
        public static IEnumerable<string> Collected => _collected;
        public static IEnumerable<string> Clicked => _clicked;

        /// <summary>True once the mote with this name has fired.</summary>
        public static bool Has(string name) => _collected.Contains(name);

        public static bool IsClicked(string name) => _clicked.Contains(name);

        public static bool TryGetByFunc(string func, out MoteDef def)
        {
            foreach (var m in QuestData.Motes)
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
            if (!TryGetByFunc(func, out var def) || _collected.Contains(def.Name)) return;
            _collected.Add(def.Name);
            Triggered?.Invoke(def);
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
