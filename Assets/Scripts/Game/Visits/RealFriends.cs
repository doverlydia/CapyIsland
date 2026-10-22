using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using Unity.Services.Core;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Options;
using Unity.Services.Friends.Notifications;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Friends who are real players (Unity Friends): added with a friend code, they show up in the
    /// friends list and on the server map beside the islanders the game makes up, and their real
    /// island is the one you fly to.
    ///
    /// A friend code is the player's Unity name, like "Miso#4821": their in-game name and four
    /// digits Unity picks. Adding someone by code sends them a request; once they accept (or if
    /// they had already added you), you are friends both ways.
    ///
    /// What friends can see of you lives in Cloud Save's public data, next to your private save:
    /// "profile" (name, level, outfit) and "island" (the island itself, without the rest of the
    /// save). CloudSync sends them up with the island.
    ///
    /// Everything works offline from the last list seen; the real list catches up once connected.
    /// </summary>
    public static class RealFriends
    {
        const string CacheKey = "CapyIsland.RealFriends";
        public const string ProfileKey = "profile", IslandKey = "island";
        /// <summary>A friend counts as online while their presence was refreshed this recently.</summary>
        const double OnlineMinutes = 4.0;

        /// <summary>What a friend shows the world about themselves.</summary>
        [Serializable]
        public class PublicProfile
        {
            public string name;
            public int level = 1;
            public int hearts;
            public List<string> outfit = new();
        }

        /// <summary>The presence note friends see: still here, and where.</summary>
        [Serializable]
        public class Activity
        {
            public long seen;       // UTC ticks of the last heartbeat
            public bool visiting;   // away on someone's island
            public string session;  // an island open for live visits right now (Live), or null
        }

        [Serializable] class Cache { public List<FriendProfile> friends = new(); }

        static List<FriendProfile> _list;
        static readonly Dictionary<string, Activity> _presence = new();
        static bool _started, _ready;

        /// <summary>Connected to Unity Friends: requests can be sent and answered.</summary>
        public static bool Ready => _ready;

        /// <summary>Your friend code ("Miso#4821"), or null until connected.</summary>
        public static string Code { get; private set; }

        /// <summary>Real friends, as islanders (FriendProfile.member is their player id).</summary>
        public static IReadOnlyList<FriendProfile> All { get { Load(); return _list; } }

        /// <summary>People who asked to be your friend: their player id and name.</summary>
        public static List<(string id, string name)> Requests { get; } = new();

        /// <summary>The list, requests or someone's presence changed.</summary>
        public static event Action Changed;

        /// <summary>A friend's message arrived (Live uses these to knock and answer).</summary>
        public static event Action<string, Message> MessageReceived;

        /// <summary>A small note between two online friends.</summary>
        [Serializable]
        public class Message
        {
            public string kind;   // what it is (Live: "knock", "open", "busy", ...)
            public string text;   // anything it carries
        }

        // Once per launch, not SubsystemRegistration: SaveGame.ResetStatics runs those on every
        // island swap, and your friends (and their connection) stay with you when you fly.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void ResetSession()
        {
            _list = null;
            _presence.Clear();
            _started = _ready = false;
            _retryAt = -1f;
            Code = null;
            Requests.Clear();
            Changed = null;
            MessageReceived = null;
            _blocked = null;
            _sending.Clear();
            _unblocking.Clear();
        }

        static void Notify()
        {
            Server.Reseat();
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- starting

        /// <summary>Called by CloudSync once signed in (and again after switching accounts).</summary>
        public static async void Start()
        {
            if (_started) return;
            _started = true;
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn) { _started = false; return; }
                await FriendsService.Instance.InitializeAsync(new InitializeOptions().WithEvents(true).WithMemberPresence(true).WithMemberProfile(true));
                // Signing out resets the service, so this runs again after every account switch:
                // the handlers are hooked once.
                var friends = FriendsService.Instance;
                friends.RelationshipAdded -= OnRelationship;
                friends.RelationshipAdded += OnRelationship;
                friends.RelationshipDeleted -= OnRelationshipGone;
                friends.RelationshipDeleted += OnRelationshipGone;
                friends.PresenceUpdated -= OnPresence;
                friends.PresenceUpdated += OnPresence;
                friends.MessageReceived -= OnMessage;
                friends.MessageReceived += OnMessage;
                await EnsureCode();
                PlayerProfile.Changed -= NameChanged;
                PlayerProfile.Changed += NameChanged;
                _ready = true;
                Live.Install();
                PrivateServer.Resume();   // back onto your private server, if you're on one
                await Heartbeat();
                Refresh();
            }
            catch (Exception e)
            {
                _started = false;
                _retryAt = Time.unscaledTime + RetryEvery;
                Debug.Log("[RealFriends] Not connected yet: " + e.Message);
            }
        }

        const float RetryEvery = 30f;
        static float _retryAt = -1f;

        /// <summary>
        /// Tries again a little later if connecting failed (offline, or Unity's notifications still
        /// closing after an account switch). CloudSync calls this every frame.
        /// </summary>
        public static void Tick()
        {
            if (_ready || _started || _retryAt < 0f || Time.unscaledTime < _retryAt) return;
            _retryAt = -1f;
            Start();
        }

        /// <summary>After signing in to another account: start over with that account's friends.</summary>
        public static void Restart()
        {
            _started = _ready = false;
            Code = null;
            _list = new List<FriendProfile>();
            _presence.Clear();
            Requests.Clear();
            SaveCache();
            // Blocks belong to the account: the new one's come down from the service.
            _blocked = new List<BlockedPlayer>();
            _unblocking.Clear();
            SaveBlocked();
            Notify();
            Start();
        }

        /// <summary>The friend code is your in-game name: kept in step with it.</summary>
        static async Task EnsureCode()
        {
            string want = CodeName(PlayerProfile.Name);
            string have = AuthenticationService.Instance.PlayerName;
            if (string.IsNullOrEmpty(have)) have = await AuthenticationService.Instance.GetPlayerNameAsync(false);
            if (!string.IsNullOrEmpty(have) && BaseName(have) == want) { Code = have; return; }
            try { Code = await AuthenticationService.Instance.UpdatePlayerNameAsync(want); }
            catch (Exception)
            {
                // A name Unity won't take (some letters): an English one instead.
                try { Code = await AuthenticationService.Instance.UpdatePlayerNameAsync(Ascii(want)); }
                catch (Exception e) { Debug.Log("[RealFriends] No friend code yet: " + e.Message); Code = have; }
            }
        }

        /// <summary>A name as Unity takes it: no spaces, at most 20 letters.</summary>
        static string CodeName(string name)
        {
            var s = new System.Text.StringBuilder();
            foreach (char c in name ?? "") if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.') s.Append(c);
            var clean = s.Length > 20 ? s.ToString(0, 20) : s.ToString();
            return clean.Length > 0 ? clean : PlayerProfile.DefaultName;
        }

        static string Ascii(string name)
        {
            var s = new System.Text.StringBuilder();
            foreach (char c in name) if (c < 128) s.Append(c);
            return s.Length > 0 ? s.ToString() : PlayerProfile.DefaultName;
        }

        static string BaseName(string code)
        {
            int hash = code.LastIndexOf('#');
            return hash > 0 ? code.Substring(0, hash) : code;
        }

        /// <summary>Your name changed in the game: the code follows (the four digits may change).</summary>
        public static async void NameChanged()
        {
            if (!_ready) return;
            try { await EnsureCode(); Notify(); }
            catch (Exception) { }
        }

        // ---------------------------------------------------------------- the list

        static void OnRelationship(IRelationshipAddedEvent e) => Refresh();
        static void OnRelationshipGone(IRelationshipDeletedEvent e) => Refresh();

        static async void Refresh()
        {
            if (!_ready) return;
            var friends = FriendsService.Instance;
            SyncBlocks();
            var list = new List<FriendProfile>();
            foreach (var r in friends.Friends)
            {
                var m = r.Member;
                if (IsBlocked(m.Id)) continue;
                var old = Find(m.Id);
                var who = old ?? new FriendProfile { id = "u_" + m.Id, member = m.Id, seed = m.Id.GetHashCode() };
                if (string.IsNullOrEmpty(who.name)) who.name = BaseName(m.Profile?.Name ?? "Capy");
                who.code = m.Profile?.Name;
                ReadPresence(m.Id, m.Presence);
                list.Add(who);
            }
            var before = new HashSet<string>();
            foreach (var (id, _) in Requests) before.Add(id);
            Requests.Clear();
            foreach (var r in friends.IncomingFriendRequests)
            {
                if (IsBlocked(r.Member.Id)) continue;   // someone you blocked can't ask
                string name = r.Member.Profile?.Name ?? "?";
                Requests.Add((r.Member.Id, name));
                if (before.Contains(r.Member.Id)) continue;
                NewStuff.Add("friends/ask_" + r.Member.Id);
                Hints.Show(UI.Loc.F("{0} wants to be your friend! Answer in Neighbours.", BaseName(name)));
            }

            bool newFriend = false;
            foreach (var f in list) if (Find(f.member) == null) { newFriend = true; NewStuff.Add("friends/" + f.id); }
            _list = list;
            SaveCache();
            Notify();
            if (newFriend)
            {
                // Presence only reaches friends: a new one needs to hear we're here, and we them.
                await Heartbeat();
                await LoadProfiles();
                await Task.Delay(3000);
                try { await FriendsService.Instance.ForceRelationshipsRefreshAsync(); } catch (Exception) { }
                foreach (var r in FriendsService.Instance.Friends) ReadPresence(r.Member.Id, r.Member.Presence);
                Notify();
            }
            else _ = LoadProfiles();
        }

        /// <summary>Each friend's public profile: their name, level and outfit as they are now.</summary>
        static async Task LoadProfiles()
        {
            foreach (var f in new List<FriendProfile>(_list))
            {
                try
                {
                    var items = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { ProfileKey },
                        new LoadOptions(new PublicReadAccessClassOptions(f.member)));
                    if (!items.TryGetValue(ProfileKey, out var item)) continue;
                    var p = JsonUtility.FromJson<PublicProfile>(item.Value.GetAs<string>());
                    if (p == null) continue;
                    if (!string.IsNullOrEmpty(p.name)) f.name = p.name;
                    f.level = Mathf.Clamp(p.level, 1, IslandGenerator.MaxLevel);
                    f.outfit = p.outfit?.FindAll(id => Wardrobe.Find(id) != null) ?? new List<string>();
                }
                catch (Exception e) { Debug.Log("[RealFriends] Couldn't read a friend's profile: " + e.Message); }
            }
            SaveCache();
            Notify();
        }

        public static FriendProfile Find(string member)
        {
            Load();
            return string.IsNullOrEmpty(member) ? null : _list.Find(f => f.member == member);
        }

        // ---------------------------------------------------------------- presence

        static void OnPresence(IPresenceUpdatedEvent e)
        {
            ReadPresence(e.ID, e.Presence);
            Notify();
        }

        static void ReadPresence(string id, Presence presence)
        {
            if (presence == null) return;
            Activity a = null;
            try { a = presence.GetActivity<Activity>(); } catch (Exception) { }
            if (presence.Availability == Availability.Offline || presence.Availability == Availability.Invisible) a = null;
            _presence[id] = a;
        }

        /// <summary>The friend has the game open right now.</summary>
        public static bool IsOnline(FriendProfile who)
        {
            if (who?.member == null || !_presence.TryGetValue(who.member, out var a) || a == null) return false;
            return (DateTime.UtcNow - new DateTime(a.seen, DateTimeKind.Utc)).TotalMinutes < OnlineMinutes;
        }

        /// <summary>The friend is online but away on someone else's island.</summary>
        public static bool IsAway(FriendProfile who) =>
            IsOnline(who) && _presence.TryGetValue(who.member, out var a) && a.visiting;

        /// <summary>The live session the friend's island is open on, if they're home and open.</summary>
        public static string SessionOf(FriendProfile who) =>
            IsOnline(who) && _presence.TryGetValue(who.member, out var a) && !a.visiting ? a.session : null;

        static string _session;
        static bool _visiting;

        /// <summary>Tells friends you're here (and where); repeated every couple of minutes by CloudSync.</summary>
        public static async Task Heartbeat(string session = null, bool? visiting = null)
        {
            if (session != null) _session = session.Length == 0 ? null : session;
            if (visiting.HasValue) _visiting = visiting.Value;
            if (!_ready) return;
            try
            {
                await FriendsService.Instance.SetPresenceAsync(Availability.Online,
                    new Activity { seen = DateTime.UtcNow.Ticks, visiting = _visiting, session = _session });
            }
            catch (Exception e) { Debug.Log("[RealFriends] Presence not sent: " + e.Message); }
        }

        /// <summary>Leaving (the game put away): friends see you go at once.</summary>
        public static async void GoOffline()
        {
            if (!_ready) return;
            try { await FriendsService.Instance.SetPresenceAvailabilityAsync(Availability.Offline); }
            catch (Exception) { }
        }

        // ---------------------------------------------------------------- adding and removing

        /// <summary>Sends a request to the player with this code. Returns null, or what went wrong.</summary>
        public static async Task<string> Add(string code)
        {
            code = (code ?? "").Trim().Replace(" ", "");
            if (!_ready) return "Can't reach the internet right now. Try again in a moment.";
            int hash = code.LastIndexOf('#');
            if (hash <= 0 || hash == code.Length - 1) return "A friend code looks like Miso#1234: a name, a # and some numbers.";
            if (Code != null && string.Equals(code, Code, StringComparison.OrdinalIgnoreCase)) return "That's your own friend code!";
            try
            {
                await FriendsService.Instance.AddFriendByNameAsync(code);
                Refresh();
                return null;
            }
            catch (FriendsServiceException e)
            {
                Debug.Log("[RealFriends] Add failed: " + e.ErrorCode + " " + e.StatusCode + " " + e.Message);
                return e.ErrorCode switch
                {
                    FriendsErrorCode.FriendshipAlreadyExists => "You're friends already!",
                    FriendsErrorCode.RelationshipAlreadyExists => "You've asked already. They'll see it next time they play.",
                    FriendsErrorCode.UserTargetingSelf => "That's your own friend code!",
                    FriendsErrorCode.FriendLimitReached or FriendsErrorCode.TargetsFriendLimitReached => "That's as many friends as anyone can have.",
                    _ => e.StatusCode == System.Net.HttpStatusCode.NotFound
                        ? "Nobody has that friend code. Check the name and the numbers."
                        : "Couldn't send the request. Try again in a moment.",
                };
            }
            catch (Exception e)
            {
                Debug.Log("[RealFriends] Add failed: " + e.Message);
                return "Couldn't send the request. Try again in a moment.";
            }
        }

        /// <summary>Did you already ask this code (and they haven't answered)?</summary>
        public static bool Asked(string code)
        {
            if (!_ready) return false;
            foreach (var r in FriendsService.Instance.OutgoingFriendRequests)
                if (string.Equals(r.Member.Profile?.Name, code, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static async Task<bool> Accept(string id)
        {
            try { await FriendsService.Instance.AddFriendAsync(id); Refresh(); return true; }
            catch (Exception e) { Debug.Log("[RealFriends] Accept failed: " + e.Message); return false; }
        }

        public static async Task Decline(string id)
        {
            try { await FriendsService.Instance.DeleteIncomingFriendRequestAsync(id); }
            catch (Exception e) { Debug.Log("[RealFriends] Decline failed: " + e.Message); }
            Refresh();
        }

        // ---------------------------------------------------------------- blocking

        const string BlockedKey = "CapyIsland.Blocked";

        /// <summary>Someone you blocked, with the name they had then (the service doesn't always tell).</summary>
        [Serializable]
        public class BlockedPlayer
        {
            public string member;
            public string name;
        }

        [Serializable] class BlockedCache { public List<BlockedPlayer> players = new(); }

        static List<BlockedPlayer> _blocked;

        /// <summary>Everyone you blocked, newest last.</summary>
        public static IReadOnlyList<BlockedPlayer> Blocked { get { LoadBlocked(); return _blocked; } }

        /// <summary>
        /// Did you block this player? Then nothing of theirs reaches you: no knocks, invitations,
        /// friend requests, visits, capybara, emojis or chat (Live, PrivateServer, Refresh).
        /// </summary>
        public static bool IsBlocked(string member)
        {
            if (string.IsNullOrEmpty(member)) return false;
            LoadBlocked();
            foreach (var b in _blocked) if (b.member == member) return true;
            return false;
        }

        /// <summary>
        /// Blocks a real player: they stop being your friend (and any request between you goes),
        /// they're sent away if they're with you now, and nothing of theirs reaches you until you
        /// unblock them in Neighbours. Kept on this device straight away, so it holds offline too.
        /// </summary>
        public static async Task Block(string member, string name)
        {
            if (string.IsNullOrEmpty(member) || IsBlocked(member)) return;
            _unblocking.Remove(member);
            _blocked.Add(new BlockedPlayer { member = member, name = string.IsNullOrEmpty(name) ? "Capy" : BaseName(name) });
            SaveBlocked();
            Load();
            _list.RemoveAll(f => f.member == member);
            Requests.RemoveAll(r => r.id == member);
            NewStuff.Clear("friends/ask_" + member);
            SaveCache();
            Live.Blocked(member);
            PrivateServer.Reread();
            Notify();
            if (!_ready) return;   // sent up once connected (SyncBlocks)
            var friends = FriendsService.Instance;
            _sending.Add(member);   // the refreshes the unfriending sets off mustn't send it twice
            // Unfriended first, so unblocking later doesn't bring the friendship back by itself.
            try { if (HasRelationship(member, RelationshipType.Friend)) await friends.DeleteFriendAsync(member); }
            catch (Exception e) { Debug.Log("[RealFriends] Unfriending before a block: " + e.Message); }
            try { if (HasRelationship(member, RelationshipType.FriendRequest)) await friends.DeleteIncomingFriendRequestAsync(member); }
            catch (Exception) { }
            try { if (HasRelationship(member, RelationshipType.FriendRequest)) await friends.DeleteOutgoingFriendRequestAsync(member); }
            catch (Exception) { }
            try { await friends.AddBlockAsync(member); }
            catch (Exception e) { Debug.Log("[RealFriends] Block not sent (kept on this device): " + e.Message); }
            _sending.Remove(member);
            Refresh();
        }

        /// <summary>Unblocks someone: they can find you again (as a stranger: add them again to be friends).</summary>
        public static async Task Unblock(string member)
        {
            LoadBlocked();
            if (_blocked.RemoveAll(b => b.member == member) == 0) return;
            SaveBlocked();
            PrivateServer.Reread();
            Notify();
            _unblocking.Add(member);   // until the service has heard, its old block mustn't bring it back
            if (!_ready) return;
            try { if (HasRelationship(member, RelationshipType.Block)) await FriendsService.Instance.DeleteBlockAsync(member); }
            catch (Exception e) { Debug.Log("[RealFriends] Unblock failed: " + e.Message); }
            Refresh();
        }

        static bool HasRelationship(string member, RelationshipType type)
        {
            foreach (var r in FriendsService.Instance.Relationships)
                if (r.Type == type && r.Member?.Id == member) return true;
            return false;
        }

        /// <summary>
        /// The service's blocks and this device's, made one list: blocks made on another device
        /// arrive here, and ones made here while offline are sent up.
        /// </summary>
        static void SyncBlocks()
        {
            LoadBlocked();
            var friends = FriendsService.Instance;
            var known = new HashSet<string>();
            bool changed = false;
            foreach (var r in friends.Blocks)
            {
                var m = r.Member;
                if (m == null || m.Role == MemberRole.Source) continue;   // only blocks you made
                if (_unblocking.Contains(m.Id)) { UnblockLater(m.Id); continue; }
                known.Add(m.Id);
                if (IsBlocked(m.Id)) continue;
                _blocked.Add(new BlockedPlayer { member = m.Id, name = BaseName(m.Profile?.Name ?? "Capy") });
                changed = true;
            }
            foreach (var b in _blocked)
                if (!known.Contains(b.member) && _sending.Add(b.member)) SendBlockLater(b.member);
            if (changed) SaveBlocked();
        }

        static readonly HashSet<string> _sending = new();      // blocks on their way up, so each is sent once
        static readonly HashSet<string> _unblocking = new();   // unblocked here, the service not told yet

        static async void UnblockLater(string member)
        {
            if (!_sending.Add(member)) return;
            try { await FriendsService.Instance.DeleteBlockAsync(member); _unblocking.Remove(member); }
            catch (Exception e) { Debug.Log("[RealFriends] Unblock still not sent: " + e.Message); }
            _sending.Remove(member);
        }

        static async void SendBlockLater(string member)
        {
            try { await FriendsService.Instance.AddBlockAsync(member); }
            catch (Exception e) { Debug.Log("[RealFriends] Block still not sent: " + e.Message); }
            _sending.Remove(member);
        }

        static void LoadBlocked()
        {
            if (_blocked != null) return;
            _blocked = new List<BlockedPlayer>();
            var json = PlayerPrefs.GetString(BlockedKey, "");
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var c = JsonUtility.FromJson<BlockedCache>(json);
                if (c?.players != null) _blocked = c.players.FindAll(b => !string.IsNullOrEmpty(b.member));
            }
            catch (Exception) { }
        }

        static void SaveBlocked()
        {
            LoadBlocked();
            PlayerPrefs.SetString(BlockedKey, JsonUtility.ToJson(new BlockedCache { players = _blocked }));
            PlayerPrefs.Save();
        }

        public static async Task Remove(FriendProfile who)
        {
            Load();
            _list.RemoveAll(f => f.member == who.member);
            SaveCache();
            Notify();
            try { await FriendsService.Instance.DeleteFriendAsync(who.member); }
            catch (Exception e) { Debug.Log("[RealFriends] Unfriend failed: " + e.Message); }
            Refresh();
        }

        /// <summary>Deleting the account: every friendship and request goes, and friends see you leave.</summary>
        public static async Task ForgetEveryone()
        {
            if (!_ready) return;
            var friends = FriendsService.Instance;
            foreach (var r in new List<Relationship>(friends.Relationships))
            {
                try { await friends.DeleteRelationshipAsync(r.Id); }
                catch (Exception e) { Debug.Log("[RealFriends] Couldn't remove a relationship: " + e.Message); }
            }
            try { await friends.SetPresenceAvailabilityAsync(Availability.Offline); } catch (Exception) { }
        }

        // ---------------------------------------------------------------- messages

        public static async Task<bool> Send(FriendProfile to, string kind, string text = null)
        {
            if (!_ready || to?.member == null) return false;
            try { await FriendsService.Instance.MessageAsync(to.member, new Message { kind = kind, text = text }); return true; }
            catch (Exception e) { Debug.Log("[RealFriends] Message not sent: " + e.Message); return false; }
        }

        static void OnMessage(IMessageReceivedEvent e)
        {
            Message m = null;
            try { m = e.GetAs<Message>(); } catch (Exception ex) { Debug.Log("[RealFriends] Unreadable message: " + ex.Message); }
            Debug.Log("[RealFriends] Message from " + e.UserId + ": " + (m != null ? m.kind : "?"));
            if (m != null && !IsBlocked(e.UserId)) MessageReceived?.Invoke(e.UserId, m);
        }

        // ---------------------------------------------------------------- your public side

        /// <summary>What friends see of you: sent up by CloudSync beside the private save.</summary>
        public static Dictionary<string, object> PublicData()
        {
            var outfit = new List<string>();
            foreach (WearSlot slot in Enum.GetValues(typeof(WearSlot)))
            {
                var worn = Wardrobe.Worn(slot);
                if (worn != null) outfit.Add(worn.Id);
            }
            int hearts = Motes.CollectedCount;
            var profile = new PublicProfile
            {
                name = PlayerProfile.Name,
                hearts = hearts,
                level = Mathf.Clamp(1 + hearts / 6, 1, IslandGenerator.MaxLevel),
                outfit = outfit,
            };
            return new Dictionary<string, object>
            {
                { ProfileKey, JsonUtility.ToJson(profile) },
                { IslandKey, PlayerPrefs.GetString(SaveGame.Key, "") },
            };
        }

        /// <summary>A friend's island as they last saved it, or null (with a hint shown) if it can't be fetched.</summary>
        public static async Task<SaveData> FetchIsland(FriendProfile who)
        {
            try
            {
                var items = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { IslandKey },
                    new LoadOptions(new PublicReadAccessClassOptions(who.member)));
                if (items.TryGetValue(IslandKey, out var item))
                {
                    var world = JsonUtility.FromJson<SaveData>(item.Value.GetAs<string>());
                    if (world != null) return world;
                }
                Hints.Show(UI.Loc.F("{0}'s island isn't ready for visitors yet. Try again later!", who.name));
            }
            catch (Exception e)
            {
                Debug.Log("[RealFriends] Couldn't fetch a friend's island: " + e.Message);
                Hints.Show("Can't reach the internet right now. Try again in a moment.");
            }
            return null;
        }

        // ---------------------------------------------------------------- kept on the device

        static void Load()
        {
            if (_list != null) return;
            _list = new List<FriendProfile>();
            var json = PlayerPrefs.GetString(CacheKey, "");
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var c = JsonUtility.FromJson<Cache>(json);
                if (c?.friends != null) _list = c.friends;
            }
            catch (Exception) { }
        }

        public static void SaveCache()
        {
            Load();
            PlayerPrefs.SetString(CacheKey, JsonUtility.ToJson(new Cache { friends = _list }));
            PlayerPrefs.Save();
        }
    }
}
