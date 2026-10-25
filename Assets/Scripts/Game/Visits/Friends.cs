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
        /// <summary>A real player's id (RealFriends), or empty for an islander the game made up.</summary>
        public string member;
        /// <summary>A real player's friend code, like "Miso#4821".</summary>
        public string code;

        public bool IsReal => !string.IsNullOrEmpty(member);

        /// <summary>You, drawn among the others (the events board): never marked as an islander.</summary>
        [NonSerialized] public bool you;

        /// <summary>One of the archipelago's islanders, whom the game plays: their portrait wears a leaf.</summary>
        public bool IsIslander => !IsReal && !you;

        public DateTime LastVisit => lastVisitTicks > 0 ? new DateTime(lastVisitTicks) : DateTime.MinValue;
    }

    /// <summary>
    /// Your friends list, kept on this device: the archipelago's islanders the game plays (a name
    /// of their own place, a look, an island at its own stage that carries on growing between your
    /// visits), beside the real players from RealFriends. Islanders are never passed off as
    /// players: their portraits wear a leaf, and their names say where they're from.
    /// </summary>
    public static class Friends
    {
        const string Key = "CapyIsland.Friends";

        [Serializable] class Stored { public List<FriendProfile> friends = new(); }

        static List<FriendProfile> _all;

        // An islander is called by name and by home, like "Tilly of Driftwood Bay": a character of the
        // archipelago, not a player's handle. None of the cast's names (Nori, Mochi, Tofu…), and none of
        // your own islands' or seas'.
        static readonly string[] FirstNames =
        {
            "Tilly", "Suki", "Otis", "Taro", "Hannah", "Roo", "Bea", "Pip", "Juniper", "Biscuit", "Sesame",
            "Pudding", "Chestnut", "Ginger", "Poppy", "Hazel", "Basil", "Clem", "Momo", "Kiki", "Rosie",
            "Mabel", "Ollie", "Fig", "Bun", "Dot", "Toffee", "Bean", "Lulu", "Wren",
        };

        static readonly string[] Places =
        {
            "Driftwood Bay", "Teacup Isle", "Fern Hollow", "Willow Shoals", "Clover Cay", "Saltbreeze Point",
            "Honeydew Isle", "Maple Sands", "Starfish Reef", "Cocoa Key", "Bamboo Bluff", "Firefly Glen",
            "Pinecone Point", "Seashell Shore", "Moonpetal Isle", "Cinnamon Cove", "Bluebell Bay", "Otter Rock",
            "Snailshell Isle", "Puffin Rock",
        };

        /// <summary>An islander's name for <paramref name="seed"/>: always the same for the same seed.</summary>
        public static string IslanderName(int seed)
        {
            uint h = (uint)seed * 2654435761u;
            return FirstNames[h % (uint)FirstNames.Length] + " of " + Places[(h / (uint)FirstNames.Length) % (uint)Places.Length];
        }

        static List<string> _pool;

        /// <summary>Names for islanders who never get a profile (event leaderboards, help pictures).</summary>
        public static IReadOnlyList<string> NamePool
        {
            get
            {
                if (_pool != null) return _pool;
                _pool = new List<string>();
                for (int i = 1; _pool.Count < 30 && i < 400; i++)
                {
                    var n = IslanderName(i * 7919);
                    if (!_pool.Contains(n)) _pool.Add(n);
                }
                return _pool;
            }
        }

        /// <summary>Every friend: the islanders the game made up and the real players (RealFriends), side by side.</summary>
        public static IReadOnlyList<FriendProfile> All
        {
            get
            {
                Load();
                var all = new List<FriendProfile>(_all);
                all.AddRange(RealFriends.All);
                return all;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => _all = null;

        public static bool IsFriend(string id) => Find(id) != null;

        public static FriendProfile Find(string id)
        {
            Load();
            var bot = _all.Find(f => f.id == id);
            if (bot != null) return bot;
            foreach (var f in RealFriends.All) if (f.id == id) return f;
            return null;
        }

        public static void Add(FriendProfile who)
        {
            Load();
            if (who == null || IsFriend(who.id)) return;
            _all.Add(who);
            Save();
            NewStuff.Add("friends/" + who.id);
        }

        public static void Remove(FriendProfile who)
        {
            if (who.IsReal) { _ = RealFriends.Remove(who); return; }
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
                level = 1 + rng.Next(IslandGenerator.MaxLevel),
                seed = rng.Next(),
                tint = (float)(rng.NextDouble() * 2.0 - 1.0),
            };
            who.name = IslanderName(who.seed);
            // Most people wear something; a hat more often than not.
            foreach (var slot in new[] { WearSlot.Hat, WearSlot.Neck, WearSlot.Face, WearSlot.Back })
            {
                if (rng.NextDouble() > (slot == WearSlot.Hat ? 0.75 : 0.35)) continue;
                var options = Array.FindAll(Wardrobe.Items, i => i.Slot == slot && i.Unlock != "horaira");   // his halo is only ever his gift
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
            RealFriends.SaveCache();   // a real friend's last-visit time
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
            // Islanders met before they had names of their own places (handles like "tulip.tilly"):
            // named once, from their seed, so the same islander always gets the same name.
            bool renamed = false;
            foreach (var f in _all)
                if (!f.IsReal && (string.IsNullOrEmpty(f.name) || !f.name.Contains(" of "))) { f.name = IslanderName(f.seed); renamed = true; }
            if (renamed) PlayerPrefs.SetString(Key, JsonUtility.ToJson(new Stored { friends = _all }));
        }
    }
}
