using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A private server: everyone who enters the same code shares one server — its ten island
    /// slots are just them (no made-up islanders), and they're shown online while their game is
    /// open. Visiting still needs being friends; a stranger on your server can be added with a tap.
    ///
    /// The code is the server. Behind it is a Unity Multiplayer session (a lobby, no network)
    /// named after the code: the first to arrive creates it, the rest join, and when everyone
    /// has gone it closes by itself and costs nothing — typing the code again opens it afresh.
    /// You stay on your private server every time you play until you leave it.
    /// </summary>
    public static class PrivateServer
    {
        const string PrefKey = "CapyIsland.PrivateServer";
        const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";   // no 0/O, 1/I/L to mix up
        const int CodeLength = 6;
        public const int Slots = 10;

        /// <summary>The code of the private server you're on (saved), or null on the public ones.</summary>
        public static string Code => PlayerPrefs.GetString(PrefKey, "") is { Length: > 0 } c ? c : null;

        /// <summary>On a private server (even while it's still connecting).</summary>
        public static bool Active => Code != null;

        /// <summary>Connected to it: the members are known.</summary>
        public static bool Connected => _session != null;

        /// <summary>The list of people on it changed (someone came, went, or changed outfit).</summary>
        public static event Action Changed;

        static ISession _session;
        static bool _joining;
        static readonly Dictionary<string, FriendProfile> _people = new();

        // Once per launch (island swaps must not drop the server: SaveGame.ResetStatics).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void ResetSession()
        {
            _session = null;
            _joining = false;
            _people.Clear();
            Changed = null;
        }

        // ---------------------------------------------------------------- codes

        public static string NewCode()
        {
            var rng = new System.Random(Guid.NewGuid().GetHashCode());
            var c = new char[CodeLength];
            for (int i = 0; i < c.Length; i++) c[i] = Alphabet[rng.Next(Alphabet.Length)];
            return new string(c);
        }

        /// <summary>A typed code as the server knows it: capitals, no spaces or dashes; null if it can't be one.</summary>
        public static string Clean(string typed)
        {
            var s = new System.Text.StringBuilder();
            foreach (char ch in (typed ?? "").ToUpperInvariant())
            {
                if (ch == '-' || char.IsWhiteSpace(ch)) continue;
                char c = ch == '0' ? 'O' : ch == '1' ? 'I' : ch;   // what people type for what they see
                if (Alphabet.IndexOf(c) < 0) return null;
                s.Append(c);
            }
            return s.Length == CodeLength ? s.ToString() : null;
        }

        /// <summary>How a code is shown: ABC-DEF.</summary>
        public static string Pretty(string code) => code == null || code.Length != CodeLength ? code : code.Substring(0, 3) + "-" + code.Substring(3);

        // ---------------------------------------------------------------- joining and leaving

        /// <summary>Joins (or opens) the server with this code and remembers it. Null, or what went wrong.</summary>
        public static async Task<string> Join(string typed)
        {
            string code = Clean(typed);
            if (code == null) return "A server code has 6 letters and numbers, like K7M-2QX.";
            if (!CloudSync.Online) return "Can't reach the internet right now. Try again in a moment.";
            if (code == Code && Connected) return null;
            await LeaveSession();
            PlayerPrefs.SetString(PrefKey, code);
            PlayerPrefs.Save();
            string problem = await Connect();
            if (problem != null)
            {
                PlayerPrefs.DeleteKey(PrefKey);
                PlayerPrefs.Save();
                Notify();
            }
            return problem;
        }

        /// <summary>Back to the public servers.</summary>
        public static async Task Leave()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.Save();
            await LeaveSession();
            Server.Reconnect();
            Notify();
        }

        /// <summary>On starting the game (CloudSync, once signed in): back onto your private server, if you have one.</summary>
        public static async void Resume()
        {
            if (!Active || Connected || _joining) return;
            string problem = await Connect();
            if (problem != null) Debug.Log("[PrivateServer] Couldn't rejoin " + Code + ": " + problem);
        }

        static async Task<string> Connect()
        {
            if (_joining) return null;
            _joining = true;
            try
            {
                var options = new SessionOptions { MaxPlayers = Slots, IsPrivate = true, Type = "server", PlayerProperties = MyProperties() };
                var session = await MultiplayerService.Instance.CreateOrJoinSessionAsync("capyserver-" + Code, options);
                _session = session;
                session.PlayerJoined += _ => Rebuild();
                session.PlayerHasLeft += _ => Rebuild();
                session.PlayerPropertiesChanged += Rebuild;
                session.Changed += Rebuild;
                session.RemovedFromSession += () => { _session = null; Rebuild(); };
                Rebuild();
                return null;
            }
            catch (Exception e) when (e.ToString().IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "That server is full: 10 islands already.";
            }
            catch (Exception e)
            {
                Debug.Log("[PrivateServer] Couldn't join: " + e.Message);
                return "Couldn't reach that server. Try again in a moment.";
            }
            finally { _joining = false; }
        }

        static async Task LeaveSession()
        {
            var s = _session;
            _session = null;
            _people.Clear();
            if (s == null) return;
            try { await s.LeaveAsync(); }
            catch (Exception e) { Debug.Log("[PrivateServer] Leaving: " + e.Message); }
        }

        /// <summary>The game is closing: off the server at once (the others see you go).</summary>
        internal static void Shutdown()
        {
            var s = _session;
            _session = null;
            if (s != null) _ = s.LeaveAsync();
        }

        /// <summary>Your outfit or name changed: the others see it.</summary>
        public static async void UpdateMe()
        {
            var s = _session;
            if (s == null) return;
            try
            {
                s.CurrentPlayer.SetProperties(MyProperties());
                await s.SaveCurrentPlayerDataAsync();
            }
            catch (Exception) { }
        }

        static Dictionary<string, PlayerProperty> MyProperties()
        {
            var outfit = new List<string>();
            foreach (WearSlot slot in Enum.GetValues(typeof(WearSlot)))
            {
                var worn = Wardrobe.Worn(slot);
                if (worn != null) outfit.Add(worn.Id);
            }
            return new Dictionary<string, PlayerProperty>
            {
                { "name", new PlayerProperty(PlayerProfile.Name) },
                { "code", new PlayerProperty(RealFriends.Code ?? "") },
                { "outfit", new PlayerProperty(string.Join(",", outfit)) },
            };
        }

        // ---------------------------------------------------------------- who's here

        /// <summary>Everyone else on the server, as islanders (real friends are the friend's own profile).</summary>
        public static List<FriendProfile> Others()
        {
            var list = new List<FriendProfile>();
            var s = _session;
            if (s == null) return list;
            string me = AuthenticationService.Instance.PlayerId;
            foreach (var p in s.Players)
            {
                if (p.Id == me) continue;
                if (_people.TryGetValue(p.Id, out var who)) list.Add(who);
            }
            return list;
        }

        /// <summary>Is this person on your private server right now?</summary>
        public static bool IsHere(FriendProfile who) => who?.member != null && _session != null && _people.ContainsKey(who.member);

        static void Rebuild()
        {
            _people.Clear();
            var s = _session;
            if (s != null)
            {
                string me = AuthenticationService.Instance.PlayerId;
                foreach (var p in s.Players)
                {
                    if (p.Id == me) continue;
                    var friend = RealFriends.Find(p.Id);
                    var who = friend ?? new FriendProfile { id = "u_" + p.Id, member = p.Id, seed = p.Id.GetHashCode() };
                    if (p.Properties != null)
                    {
                        if (p.Properties.TryGetValue("name", out var n) && !string.IsNullOrEmpty(n.Value) && friend == null) who.name = n.Value;
                        if (p.Properties.TryGetValue("code", out var c) && !string.IsNullOrEmpty(c.Value)) who.code = c.Value;
                        if (p.Properties.TryGetValue("outfit", out var o) && friend == null)
                            who.outfit = new List<string>(Array.FindAll((o.Value ?? "").Split(','), id => Wardrobe.Find(id) != null));
                    }
                    if (string.IsNullOrEmpty(who.name)) who.name = "Capy";
                    _people[p.Id] = who;
                }
            }
            Notify();
        }

        static void Notify()
        {
            Server.Reseat();
            Changed?.Invoke();
        }
    }
}
