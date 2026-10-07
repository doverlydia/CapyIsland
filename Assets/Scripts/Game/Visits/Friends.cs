using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>A pretend player: who they are, how far along their island is, and what they wear.</summary>
    [Serializable]
    public class FriendProfile
    {
        public string id;
        public string name;
        public int level = 1;
        public int seed;
        public int visits;
        public List<string> outfit = new();
        public float tint;              // a little variety in fur colour, -1 to 1
        public long lastVisitTicks;
        /// <summary>Their island as you last left it (friends only), as JSON.</summary>
        public string world;

        public DateTime LastVisit => lastVisitTicks > 0 ? new DateTime(lastVisitTicks) : DateTime.MinValue;
    }

    /// <summary>
    /// Your friends list, kept on this device. There is no server: every "player" here is a bot,
    /// made to feel like a person — a name, a look, an island at its own stage that carries on
    /// growing between your visits.
    /// </summary>
    public static class Friends
    {
        const string Key = "CapyIsland.Friends";

        [Serializable] class Stored { public List<FriendProfile> friends = new(); }

        static List<FriendProfile> _all;

        static readonly string[] Names =
        {
            "mochi_dreams", "SleepyTofu", "capy.queen", "NoriNoodle", "biscuit_bay", "Pudding42", "yuzu_fan",
            "sesame.seed", "HotSpringHannah", "chestnut_kid", "MatchaMaker", "lazy.river", "Kombu_Kid", "waddle_bee",
            "sunny_soak", "plumtastic", "TeaCupTaro", "LittleLantern", "bubbles4ever", "GingerSnapper", "mossy.moss",
            "PebblePaws", "snoozy_suki", "BathBomber", "fluffcaptain", "OnsenOtis", "tulip.tilly", "RiverRoo",
        };

        public static IReadOnlyList<FriendProfile> All
        {
            get
            {
                Load();
                return _all;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => _all = null;

        public static bool IsFriend(string id)
        {
            Load();
            return _all.Exists(f => f.id == id);
        }

        public static FriendProfile Find(string id)
        {
            Load();
            return _all.Find(f => f.id == id);
        }

        public static void Add(FriendProfile who)
        {
            Load();
            if (who == null || IsFriend(who.id)) return;
            _all.Add(who);
            Save();
        }

        public static void Remove(FriendProfile who)
        {
            Load();
            _all.RemoveAll(f => f.id == who.id);
            Save();
        }

        /// <summary>Someone new: a name, a level a little above or below yours, an outfit.</summary>
        public static FriendProfile Stranger(int? seed = null)
        {
            var rng = new System.Random(seed ?? Environment.TickCount ^ UnityEngine.Random.Range(0, int.MaxValue));
            var who = new FriendProfile
            {
                id = Guid.NewGuid().ToString("N").Substring(0, 12),
                name = Names[rng.Next(Names.Length)],
                level = 1 + rng.Next(IslandGenerator.MaxLevel),
                seed = rng.Next(),
                tint = (float)(rng.NextDouble() * 2.0 - 1.0),
            };
            // Most people wear something; a hat more often than not.
            foreach (var slot in new[] { WearSlot.Hat, WearSlot.Neck, WearSlot.Face, WearSlot.Back })
            {
                if (rng.NextDouble() > (slot == WearSlot.Hat ? 0.75 : 0.35)) continue;
                var options = Array.FindAll(Wardrobe.Items, i => i.Slot == slot);
                if (options.Length > 0) who.outfit.Add(options[rng.Next(options.Length)].Id);
            }
            return who;
        }

        /// <summary>Three islanders to meet today — the same three all day, new ones tomorrow.</summary>
        public static List<FriendProfile> Suggested()
        {
            int day = (int)(DateTime.Now.Date - new DateTime(2026, 1, 1)).TotalDays;
            var list = new List<FriendProfile>();
            for (int i = 0; list.Count < 3 && i < 12; i++)
            {
                var who = Stranger(day * 31 + i);
                who.id = "s" + day + "_" + i;
                if (IsFriend(who.id) || list.Exists(x => x.name == who.name)) continue;
                list.Add(who);
            }
            return list;
        }

        public static void Save()
        {
            Load();
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(new Stored { friends = _all }));
            PlayerPrefs.Save();
        }

        static void Load()
        {
            if (_all != null) return;
            _all = new List<FriendProfile>();
            var json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var stored = JsonUtility.FromJson<Stored>(json);
                if (stored?.friends != null) _all = stored.friends;
            }
            catch (Exception e) { Debug.LogWarning("[Friends] list unreadable: " + e.Message); }
        }
    }
}
