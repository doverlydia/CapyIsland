using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The "server" you join each time you open the game, like a town in Heartopia: ten island
    /// slots, one of them yours. How many others are online changes with every connection —
    /// sometimes the archipelago is busy, sometimes it is just you and a couple of neighbours.
    /// Friends who happen to be online are seated first; strangers fill the rest.
    ///
    /// Rolled once per launch and kept for the whole session (it survives flying between islands),
    /// so the neighbours you see stay put until you next open the game.
    /// </summary>
    public static class Server
    {
        public const int Slots = 10;

        public class Seat
        {
            public int Slot;
            public FriendProfile Who;     // null for your own slot
            public bool IsYou => Who == null;
        }

        static List<Seat> _seats;
        static HashSet<string> _onlineFriends;

        /// <summary>A name for the server, like "Capy Isles · Willow 27".</summary>
        public static string Name { get; private set; }

        /// <summary>Every occupied slot (including yours), in slot order. Empty slots are not listed.</summary>
        public static IReadOnlyList<Seat> Seats
        {
            get
            {
                if (_seats == null) Roll();
                return _seats;
            }
        }

        public static int Online => Seats.Count;

        public static bool IsOnline(FriendProfile who)
        {
            if (who == null) return false;
            if (who.IsReal) return RealFriends.IsOnline(who);
            foreach (var s in Seats) if (!s.IsYou && s.Who.id == who.id) return true;
            return false;
        }

        /// <summary>
        /// Real friends who are online always have a seat (taking a stranger's if the server is
        /// full); ones who went offline leave theirs. Called whenever RealFriends changes.
        /// </summary>
        public static void SeatRealFriends()
        {
            if (_seats == null) return;
            bool changed = _seats.RemoveAll(s => !s.IsYou && s.Who.IsReal && !RealFriends.IsOnline(s.Who)) > 0;
            foreach (var f in RealFriends.All)
            {
                if (!RealFriends.IsOnline(f) || _seats.Exists(s => !s.IsYou && s.Who.id == f.id)) continue;
                var used = new HashSet<int>();
                foreach (var s in _seats) used.Add(s.Slot);
                int slot = -1;
                for (int i = 0; i < Slots; i++) if (!used.Contains(i)) { slot = i; break; }
                if (slot < 0)
                {
                    var stranger = _seats.Find(s => !s.IsYou && !Friends.IsFriend(s.Who.id));
                    if (stranger == null) continue;
                    slot = stranger.Slot;
                    _seats.Remove(stranger);
                }
                _seats.Add(new Seat { Slot = slot, Who = f });
                changed = true;
            }
            if (changed) _seats.Sort((a, b) => a.Slot.CompareTo(b.Slot));
        }

        /// <summary>Everyone but you, friends first.</summary>
        public static List<FriendProfile> Neighbours()
        {
            var list = new List<FriendProfile>();
            foreach (var s in Seats) if (!s.IsYou) list.Add(s.Who);
            list.Sort((a, b) => Friends.IsFriend(b.id).CompareTo(Friends.IsFriend(a.id)));
            return list;
        }

        /// <summary>Connects again: a new server with a new crowd (the Friends screen's refresh).</summary>
        public static void Reconnect() => Roll();

        static void Roll()
        {
            var rng = new System.Random(Environment.TickCount);
            string[] groves = { "Willow", "Maple", "Juniper", "Mochi", "Hazel", "Bamboo", "Clover", "Pebble", "Saffron", "Lotus" };
            Name = $"Capy Isles · {groves[rng.Next(groves.Length)]} {rng.Next(1, 99)}";

            // How busy is it? Mostly a comfortable crowd, sometimes quiet, sometimes full.
            int[] weights = { 0, 0, 1, 2, 3, 4, 4, 3, 2, 2, 2 };   // index = islands online, 2..10
            int total = 0;
            foreach (var w in weights) total += w;
            int pick = rng.Next(total), online = 2;
            for (int i = 0; i < weights.Length; i++) { pick -= weights[i]; if (pick < 0) { online = Mathf.Max(2, i); break; } }

            var free = new List<int>();
            for (int i = 0; i < Slots; i++) free.Add(i);
            int TakeSlot() { int k = rng.Next(free.Count); int s = free[k]; free.RemoveAt(k); return s; }

            _seats = new List<Seat> { new Seat { Slot = TakeSlot() } };

            // Friends who are online right now come first.
            _onlineFriends = new HashSet<string>();
            var friends = new List<FriendProfile>(Friends.All);
            Shuffle(friends, rng);
            foreach (var f in friends)
            {
                if (f.IsReal) continue;   // seated by SeatRealFriends, only while really online
                if (_seats.Count >= online) break;
                if (rng.NextDouble() > 0.6) continue;   // not everyone is on at once
                _onlineFriends.Add(f.id);
                _seats.Add(new Seat { Slot = TakeSlot(), Who = f });
            }

            // Strangers fill the rest.
            var names = new HashSet<string>();
            foreach (var s in _seats) if (!s.IsYou) names.Add(s.Who.name);
            while (_seats.Count < online)
            {
                var who = Friends.Stranger(rng.Next());
                if (!names.Add(who.name)) continue;
                _seats.Add(new Seat { Slot = TakeSlot(), Who = who });
            }
            _seats.Sort((a, b) => a.Slot.CompareTo(b.Slot));
            SeatRealFriends();
        }

        static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
