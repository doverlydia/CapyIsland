using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Live visits between real friends: you knock, they let you in, and you're on their island
    /// together — each of you sees the other's capybara walk about and the emojis you send.
    ///
    /// How it connects: the knock and the answer are Friends messages (RealFriends). Saying yes
    /// opens a private two-player session (Unity Multiplayer: Lobby + Relay, over WebSockets so a
    /// browser and a phone can meet), and its code goes back with the answer. The visitor joins,
    /// flies over to the island as the host has just saved it (Visit, from Cloud Save), and the
    /// two games then swap small notes over the link (LiveLink): where each capybara is, emojis,
    /// hello and goodbye. The host's island stays the host's: the visitor's game shows a copy.
    /// </summary>
    public static class Live
    {
        public enum Mode { None, Knocking, Hosting, Visiting }

        /// <summary>What this game is doing live right now.</summary>
        public static Mode Now { get; private set; }

        /// <summary>The friend on the other end (the one visiting, or being visited).</summary>
        public static FriendProfile Friend { get; private set; }

        /// <summary>The two games are connected.</summary>
        public static bool Connected => _link != null && _link.Connected;

        /// <summary>The other person's capybara, here on this island.</summary>
        public static VisitorBot Puppet => _puppet;

        const float KnockWait = 45f, HostWait = 75f, SendEvery = 0.1f;

        static ISession _session;
        static LiveLink _link;
        static VisitorBot _puppet;
        static RealFriends.Message _answer;
        static Vector2? _theirSpot;
        static bool _installed, _theyArrived;
        static float _nextSend, _hostDeadline;
        static Vector2 _lastSent;

        /// <summary>One note between the two games.</summary>
        [Serializable]
        class Note
        {
            // hi, at, emo, bye; ask / build / nobuild (building together); put (something placed),
            // pop (a bubble collected), bubble (a found thing given back as a bubble); leave (the
            // host asks the visitor to go)
            public string t;
            public float x, y;
            public int e;
            public string name;
            public List<string> outfit;
        }

        // Once per launch, not SubsystemRegistration: SaveGame.ResetStatics runs those on every
        // island swap, and a live visit has to survive the flight.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void ResetSession()
        {
            Now = Mode.None;
            Friend = null;
            _session = null;
            _link = null;
            _puppet = null;
            _answer = null;
            _theirSpot = null;
            _installed = _theyArrived = false;
        }

        /// <summary>Listens for knocks and answers (RealFriends calls this once connected).</summary>
        public static void Install()
        {
            if (_installed) return;
            _installed = true;
            RealFriends.MessageReceived += OnFriendMessage;
            Rehook();
            LiveRunner.Make();
        }

        /// <summary>
        /// The island's own events (emojis, placing, popping) are cleared on every island swap
        /// (SaveGame.ResetStatics): hooked again each time a scene loads (LiveRunner).
        /// </summary>
        internal static void Rehook()
        {
            VisitorBot.PlayerEmoted -= OnPlayerEmoted;
            VisitorBot.PlayerEmoted += OnPlayerEmoted;
            Placeable.PlayerPlaced -= OnPlaced;
            Placeable.PlayerPlaced += OnPlaced;
            Bubble.Collected -= OnCollected;
            Bubble.Collected += OnCollected;
            PlayerProfile.Changed -= RealFriends.NameChanged;   // your friend code follows your name
            PlayerProfile.Changed += RealFriends.NameChanged;
            Wardrobe.Changed -= PrivateServer.UpdateMe;          // and your private server sees your outfit
            Wardrobe.Changed += PrivateServer.UpdateMe;
        }

        static void OnPlayerEmoted(Emoji e) => Send(new Note { t = "emo", e = (int)e });

        // ---------------------------------------------------------------- the island, shared

        /// <summary>You put something down: it appears on your friend's screen too.</summary>
        static void OnPlaced(Placeable p)
        {
            if (p == null || !OnSharedIsland) return;
            var item = p.GetComponent<CatalogItem>();
            if (item == null) return;
            Vector2 at = p.transform.position;
            Send(new Note { t = "put", name = item.StoreName, x = at.x, y = at.y });
        }

        /// <summary>You popped a bubble: it's gone on your friend's screen too.</summary>
        static void OnCollected(GameObject item, Bubble from)
        {
            if (item == null || from == null || !OnSharedIsland) return;
            var tag = item.GetComponent<CatalogItem>();
            if (tag == null) return;
            Vector2 at = from.transform.position;
            Send(new Note { t = "pop", name = tag.StoreName, x = at.x, y = at.y });
        }

        /// <summary>Puts down what your friend put down (silently: it's their placement, not yours).</summary>
        static void PutRemote(string name, Vector2 at)
        {
            // On your island: chores always, other building only with your yes, bombs and story things never.
            if (Now == Mode.Hosting && !CoBuild.IsChore(name) && (!CoBuild.GuestAllowed || StoreData.StaysHome(name))) return;
            var catalog = ItemCatalog.Instance;
            if (catalog == null || string.IsNullOrEmpty(name)) return;
            var go = catalog.MakeItem(name, at);
            var fx = HarvestEffects.Instance;
            if (go != null && fx != null) fx.Poof(at, Color.white);
        }

        /// <summary>Your friend popped a bubble: the same one goes here (the nearest holding the same thing).</summary>
        static void PopRemote(string name, Vector2 at)
        {
            Bubble best = null;
            float bestDistance = 0.8f;
            foreach (var b in UnityEngine.Object.FindObjectsByType<Bubble>(FindObjectsSortMode.None))
            {
                var contents = b.Contents;
                var item = contents != null ? contents.GetComponent<CatalogItem>() : null;
                if (item == null || item.StoreName != name) continue;
                float d = Vector2.Distance(b.transform.position, at);
                if (d < bestDistance) { best = b; bestDistance = d; }
            }
            if (best == null) return;
            if (best.Contents != null) UnityEngine.Object.Destroy(best.Contents);
            UnityEngine.Object.Destroy(best.gameObject);
        }

        /// <summary>
        /// Leaving a friend's island live: what you found there goes back into bubbles on their
        /// real island too (Visit keeps the same rule on your copy).
        /// </summary>
        public static void GiveBackFound(List<BubbleSave> found)
        {
            if (Now != Mode.Visiting || found == null) return;
            foreach (var b in found) Send(new Note { t = "bubble", name = b.name, x = b.pos.x, y = b.pos.y });
        }

        /// <summary>Building together: the visitor asks, the host answers (CoBuild).</summary>
        public static void AskToBuild() => Send(new Note { t = "ask" });

        public static void AnswerBuild(bool yes) => Send(new Note { t = yes ? "build" : "nobuild" });

        /// <summary>The host asks the visitor to go: they fly home.</summary>
        public static void AskToLeave()
        {
            if (Now != Mode.Hosting) return;
            Send(new Note { t = "leave" });
            _ = End(false);
        }

        // ---------------------------------------------------------------- visiting

        /// <summary>Should a visit to this friend knock first (they're online and at home)?</summary>
        public static bool ShouldKnock(FriendProfile to) =>
            to != null && to.IsReal && Now == Mode.None && RealFriends.Ready && RealFriends.IsOnline(to) && !RealFriends.IsAway(to);

        /// <summary>Knocks on a friend's door; if they let you in, flies you over to visit live.</summary>
        public static async void Knock(FriendProfile to)
        {
            if (Now != Mode.None) return;
            Now = Mode.Knocking;
            Friend = to;
            _answer = null;
            Hints.Show(UI.Loc.F("Knocking on {0}'s door…", to.name));
            if (!await RealFriends.Send(to, "knock")) { Fail("Couldn't reach them just now. Try again in a moment."); return; }

            float until = Time.realtimeSinceStartup + KnockWait;
            while (_answer == null && Now == Mode.Knocking && Time.realtimeSinceStartup < until) await Task.Yield();
            if (Now != Mode.Knocking) return;
            var answer = _answer;
            if (answer == null) { Fail(UI.Loc.F("{0} didn't answer. Maybe they're busy. Try again later!", to.name)); return; }
            switch (answer.kind)
            {
                case "open": break;
                case "away": Fail(UI.Loc.F("{0} is visiting someone else right now.", to.name)); return;
                case "busy": Fail(UI.Loc.F("{0} already has a visitor. Try again a bit later!", to.name)); return;
                default: Fail(UI.Loc.F("{0} can't have visitors right now.", to.name)); return;
            }

            // Fly first, connect on landing: a flight reloads the whole scene, which a live link
            // doesn't survive (ArrivedAt joins).
            _pendingCode = answer.text;
            Now = Mode.Visiting;
            _theirSpot = null;
            _ = RealFriends.Heartbeat(visiting: true);
            Visit.Fly(to);   // fetches their island as they've just saved it
        }

        static string _pendingCode;

        /// <summary>Joins the host's open island over the live link; false (and the reason logged) if it can't.</summary>
        static async Task<bool> Join(string code)
        {
            try
            {
                _link = new LiveLink(false);
                _session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code,
                    new JoinSessionOptions { Type = "visit" }.WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS }).WithNetworkHandler(_link));
                float connectBy = Time.realtimeSinceStartup + 15f;
                while (_link != null && !_link.Connected && Time.realtimeSinceStartup < connectBy) await Task.Yield();
                if (_link == null || !_link.Connected) throw new Exception("no connection");
                return Now == Mode.Visiting;
            }
            catch (Exception e)
            {
                Debug.Log("[Live] Couldn't join: " + e.Message + (e.InnerException != null ? " / " + e.InnerException : ""));
                await Close();
                return false;
            }
        }

        static void Fail(string why)
        {
            Now = Mode.None;
            Friend = null;
            Hints.Show(why);
        }

        /// <summary>Visit has built the host's island: their capybara steps out to meet you.</summary>
        public static bool ArrivedAt(FriendProfile host)
        {
            if (Now != Mode.Visiting || Friend == null || host == null || host.id != Friend.id) return false;
            Connect(host);
            return true;
        }

        /// <summary>Landed on the host's island: now the link, then their capybara steps out.</summary>
        static async void Connect(FriendProfile host)
        {
            string code = _pendingCode;
            _pendingCode = null;
            bool joined = !string.IsNullOrEmpty(code) && await Join(code);
            if (Now != Mode.Visiting || Friend == null || Friend.id != host.id) return;   // flew off meanwhile
            if (!joined)
            {
                Hints.Show(UI.Loc.F("{0} went offline. You can look around, or fly home.", host.name));
                await End(false);
                return;
            }
            SayHello();
            SpawnPuppet(VisitorBot.Role.Host);
            Hints.Show(UI.Loc.F("You're on {0}'s island together! Say hi with an emoji.", host.name));
        }

        // ---------------------------------------------------------------- hosting

        static void OnFriendMessage(string from, RealFriends.Message m)
        {
            var who = RealFriends.Find(from);
            if (who == null || m == null) return;
            if (m.kind == "knock") { Knocked(who); return; }
            if (Now == Mode.Knocking && Friend != null && Friend.member == from) _answer = m;
        }

        static void Knocked(FriendProfile who)
        {
            Debug.Log("[Live] " + who.name + " knocked (visiting=" + Visit.Active + ", now=" + Now + ")");
            if (Visit.Active || Visit.Travelling) { _ = RealFriends.Send(who, "away"); return; }
            if (Now != Mode.None) { _ = RealFriends.Send(who, "busy"); return; }
            UI.PromptScreen.Ask("Visitor!", who, UI.Loc.F("<b>{0}</b> wants to visit your island!", who.name),
                "Let them in", "Not now", () => LetIn(who), () => _ = RealFriends.Send(who, "no"));
        }

        static async void LetIn(FriendProfile who)
        {
            if (Now != Mode.None || Visit.Active) { _ = RealFriends.Send(who, "busy"); return; }
            Now = Mode.Hosting;
            Friend = who;
            _theyArrived = false;
            _hostDeadline = 0f;   // not counting until the island is open
            try
            {
                // They'll see the island as it is right now.
                if (SaveGame.Instance != null) SaveGame.Instance.SaveHomeNow();
                await CloudSync.PublishNow();
                _link = new LiveLink(true);
                _session = await MultiplayerService.Instance.CreateSessionAsync(new SessionOptions { MaxPlayers = 2, IsPrivate = true, Type = "visit" }
                    .WithRelayNetwork()
                    .WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS })
                    .WithNetworkHandler(_link));
                if (Now != Mode.Hosting || Friend != who) { await Close(); return; }   // given up meanwhile
                await RealFriends.Send(who, "open", _session.Code);
                _hostDeadline = Time.realtimeSinceStartup + HostWait;
                Hints.Show(UI.Loc.F("{0} is on the way!", who.name));
            }
            catch (Exception e)
            {
                Debug.Log("[Live] Couldn't open the island: " + e.Message + (e.InnerException != null ? " / " + e.InnerException : ""));
                await Close();
                Now = Mode.None;
                Friend = null;
                _ = RealFriends.Send(who, "no");
                Hints.Show("Couldn't open your island just now. Try again in a moment.");
            }
        }

        // ---------------------------------------------------------------- ending

        /// <summary>
        /// Ends the live visit: the other game hears goodbye, the link closes. Visit calls this
        /// when you fly anywhere; it's also what happens when the other side goes.
        /// </summary>
        public static async Task End(bool sayBye = true)
        {
            if (Now == Mode.None) return;
            Debug.Log("[Live] Ending the visit (" + Now + ", bye=" + sayBye + ")");
            if (sayBye) Send(new Note { t = "bye" });
            _pendingCode = null;
            Now = Mode.None;
            Friend = null;
            _hostDeadline = 0f;
            _theyArrived = false;
            if (_puppet != null) { _puppet.RemoteLeave(); _puppet = null; }
            await Task.Yield();   // let the goodbye go out
            await Close();
            _ = RealFriends.Heartbeat(visiting: Visit.Active);
        }

        /// <summary>The game is closing: say goodbye if we can and close the link straight away.</summary>
        internal static void Shutdown()
        {
            if (_link != null && _link.Connected) Send(new Note { t = "bye" });
            _link?.Dispose();
            _link = null;
            _session = null;
            Now = Mode.None;
            Friend = null;
        }

        static async Task Close()
        {
            var session = _session;
            var link = _link;
            _session = null;
            _link = null;
            try
            {
                if (session != null)
                {
                    if (session.IsHost) await session.AsHost().DeleteAsync();
                    else await session.LeaveAsync();
                }
            }
            catch (Exception e) { Debug.Log("[Live] Closing: " + e.Message); }
            link?.Dispose();
        }

        // ---------------------------------------------------------------- the link

        /// <summary>Every frame (LiveRunner): reads notes and sends where you are.</summary>
        internal static void Tick()
        {
            if (_link == null) return;
            _link.Tick(OnNote, OnJoined, OnLeft);

            if (Now == Mode.Hosting && !_theyArrived && _hostDeadline > 0f && Time.realtimeSinceStartup > _hostDeadline)
            {
                var who = Friend;
                _ = End();
                if (who != null) Hints.Show(UI.Loc.F("{0} couldn't come after all.", who.name));
                return;
            }

            // Where you are, a few times a second while you're both on the same island.
            if (!Connected || Time.unscaledTime < _nextSend || !OnSharedIsland) return;
            _nextSend = Time.unscaledTime + SendEvery;
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            if (king == null) return;
            Vector2 at = king.transform.position;
            if ((at - _lastSent).sqrMagnitude < 0.0025f && Time.frameCount % 30 != 0) return;
            _lastSent = at;
            Send(new Note { t = "at", x = at.x, y = at.y });
        }

        /// <summary>Both of you are standing on the host's island (not mid-flight, not elsewhere).</summary>
        static bool OnSharedIsland =>
            Now == Mode.Hosting ? !Visit.Active && !Visit.Travelling
            : Now == Mode.Visiting && Visit.Active && !Visit.Travelling && Visit.Host != null && Friend != null && Visit.Host.id == Friend.id;

        static void OnJoined() { if (Now == Mode.Hosting) SayHello(); }

        static void OnLeft()
        {
            if (Now == Mode.None) return;
            var who = Friend;
            bool hosting = Now == Mode.Hosting;
            _ = End(false);
            if (who == null) return;
            Hints.Show(hosting ? UI.Loc.F("{0} flew home. See you next time!", who.name)
                : UI.Loc.F("{0} went offline. You can look around, or fly home.", who.name));
        }

        static void OnNote(string text)
        {
            Note n;
            try { n = JsonUtility.FromJson<Note>(text); } catch (Exception) { return; }
            if (n == null) return;
            switch (n.t)
            {
                case "hi":
                    if (Friend != null)
                    {
                        if (n.outfit != null) Friend.outfit = n.outfit.FindAll(id => Wardrobe.Find(id) != null);
                        if (!string.IsNullOrEmpty(n.name)) Friend.name = n.name;
                    }
                    if (Now == Mode.Hosting && !_theyArrived && OnSharedIsland)
                    {
                        _theyArrived = true;
                        SpawnPuppet(VisitorBot.Role.Guest);
                        Sfx.Play(SfxKind.Bridge, 0.6f);
                        Hints.Show(UI.Loc.F("{0} has landed on your island!", Friend.name));
                        CoBuild.AskHost(Friend);
                    }
                    break;
                case "put":
                    if (OnSharedIsland) PutRemote(n.name, new Vector2(n.x, n.y));
                    break;
                case "pop":
                    if (OnSharedIsland) PopRemote(n.name, new Vector2(n.x, n.y));
                    break;
                case "bubble":
                    if (Now == Mode.Hosting && ItemCatalog.Instance != null && !string.IsNullOrEmpty(n.name))
                        ItemCatalog.Instance.MakeStore(n.name, new Vector2(n.x, n.y));
                    break;
                case "ask":
                    if (Now == Mode.Hosting && Friend != null) CoBuild.AskHost(Friend);
                    break;
                case "build":
                case "nobuild":
                    if (Now == Mode.Visiting) CoBuild.Granted(n.t == "build");
                    break;
                case "leave":
                    if (Now == Mode.Visiting && Friend != null)
                    {
                        var host = Friend;
                        _ = End(false);
                        Hints.Show(UI.Loc.F("{0} has to go now. Flying you home!", host.name));
                        Visit.FlyHome();
                    }
                    break;
                case "at":
                    _theirSpot = new Vector2(n.x, n.y);
                    if (_puppet != null && OnSharedIsland) _puppet.RemoteAt(_theirSpot.Value);
                    break;
                case "emo":
                    if (_puppet != null) _puppet.Emote((Emoji)n.e);
                    break;
                case "bye":
                    OnLeft();
                    break;
            }
        }

        static void SayHello()
        {
            var outfit = new List<string>();
            foreach (WearSlot slot in Enum.GetValues(typeof(WearSlot)))
            {
                var worn = Wardrobe.Worn(slot);
                if (worn != null) outfit.Add(worn.Id);
            }
            Send(new Note { t = "hi", name = PlayerProfile.Name, outfit = outfit });
            _lastSent = new Vector2(float.NaN, float.NaN);   // and where you are, straight after
            _nextSend = 0f;
        }

        static void SpawnPuppet(VisitorBot.Role role)
        {
            if (_puppet != null || Friend == null) return;
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            Vector2 near = king != null ? (Vector2)king.transform.position + new Vector2(role == VisitorBot.Role.Host ? 2.4f : -2.4f, 0.6f) : Vector2.zero;
            Vector2 at = _theirSpot ?? near;
            if (!BunniSpace.PointOnLand(at)) at = king != null ? (Vector2)king.transform.position : at;
            _puppet = VisitorBot.SpawnRemote(Friend, role, at);
        }

        static void Send(Note n)
        {
            if (!Connected) return;
            _link.Send(JsonUtility.ToJson(n));
        }
    }

    /// <summary>Ticks Live every frame, across island swaps.</summary>
    public class LiveRunner : MonoBehaviour
    {
        static LiveRunner _instance;

        public static void Make()
        {
            if (_instance != null || !Application.isPlaying) return;
            var go = new GameObject("[Live]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<LiveRunner>();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) => Live.Rehook();
        }

        void Update() => Live.Tick();

        // Closing the game (or stopping Play): the link closes at once, so nothing is left
        // running when the game goes (an open driver kept the editor from reloading).
        void OnApplicationQuit() { Live.Shutdown(); PrivateServer.Shutdown(); }
        void OnDestroy() { Live.Shutdown(); PrivateServer.Shutdown(); }
    }

    /// <summary>
    /// The wire between two games: Unity Transport over Relay WebSockets, with reliable,
    /// ordered notes (fragmented if long). Sessions start and stop it (INetworkHandler).
    /// </summary>
    public class LiveLink : INetworkHandler, IDisposable
    {
        readonly bool _host;
        NetworkDriver _driver;
        NetworkPipeline _pipe;
        NetworkConnection _server;   // a visitor's connection to the host
        readonly List<NetworkConnection> _peers = new();
        bool _clientUp;

        public LiveLink(bool host) { _host = host; }

        public bool Connected => _host ? _peers.Count > 0 : _clientUp;

        public Task StartAsync(NetworkConfiguration configuration)
        {
            // Both sides find their Relay details here (RelayClientData is only filled in for
            // Netcode for Entities' host-side client).
            var relay = configuration.RelayServerData;
            var settings = new NetworkSettings();
            settings.WithRelayParameters(ref relay);
            _driver = NetworkDriver.Create(new WebSocketNetworkInterface(), settings);
            _pipe = _driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
            if (_host)
            {
                if (_driver.Bind(NetworkEndpoint.AnyIpv4) != 0) throw new Exception("couldn't bind");
                if (_driver.Listen() != 0) throw new Exception("couldn't listen");
            }
            else _server = _driver.Connect(relay.Endpoint);
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (!_driver.IsCreated) return;
            _driver.Dispose();
            _peers.Clear();
            _clientUp = false;
        }

        public void Tick(Action<string> onNote, Action onJoined, Action onLeft)
        {
            if (!_driver.IsCreated) return;
            _driver.ScheduleUpdate().Complete();
            if (_host)
            {
                NetworkConnection c;
                while ((c = _driver.Accept()) != default)
                {
                    _peers.Add(c);
                    onJoined();
                }
                for (int i = _peers.Count - 1; i >= 0; i--) Pump(_peers[i], onNote, onLeft);
            }
            else if (_server != default) Pump(_server, onNote, onLeft, onJoined);
        }

        void Pump(NetworkConnection c, Action<string> onNote, Action onLeft, Action onJoined = null)
        {
            NetworkEvent.Type type;
            while (_driver.IsCreated && (type = _driver.PopEventForConnection(c, out var reader)) != NetworkEvent.Type.Empty)
            {
                switch (type)
                {
                    case NetworkEvent.Type.Connect:
                        _clientUp = true;
                        onJoined?.Invoke();
                        break;
                    case NetworkEvent.Type.Data:
                        int length = reader.ReadInt();
                        if (length <= 0 || length > 64 * 1024) break;
                        var bytes = new NativeArray<byte>(length, Allocator.Temp);
                        reader.ReadBytes(bytes);
                        onNote(Encoding.UTF8.GetString(bytes.ToArray()));
                        bytes.Dispose();
                        break;
                    case NetworkEvent.Type.Disconnect:
                        Debug.Log("[Live] Link closed (" + (_host ? "host" : "visitor") + "): " +
                            (reader.Length > 0 ? ((Unity.Networking.Transport.Error.DisconnectReason)reader.ReadByte()).ToString() : "no reason"));
                        _peers.Remove(c);
                        if (!_host) { _clientUp = false; _server = default; }
                        onLeft();
                        return;
                }
            }
        }

        public void Send(string text)
        {
            if (!_driver.IsCreated) return;
            var data = Encoding.UTF8.GetBytes(text);
            using var bytes = new NativeArray<byte>(data, Allocator.Temp);
            if (_host) foreach (var c in _peers) SendTo(c, bytes);
            else if (_clientUp) SendTo(_server, bytes);
        }

        void SendTo(NetworkConnection c, NativeArray<byte> bytes)
        {
            if (_driver.BeginSend(_pipe, c, out var writer) != 0) return;
            writer.WriteInt(bytes.Length);
            writer.WriteBytes(bytes);
            _driver.EndSend(writer);
        }
    }
}
