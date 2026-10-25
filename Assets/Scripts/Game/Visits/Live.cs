using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Live visits between real players: you knock, they let you in (or invite you to their party
    /// or contest, Gathering), and you're on their island together — everyone sees everyone else's
    /// capybara walk about and the emojis they send.
    ///
    /// How it connects: the knock and the answer are Friends messages (RealFriends); a party's
    /// invitations go out the same way, and on a private server through its lobby (PrivateServer).
    /// The host opens a private session (Unity Multiplayer: Lobby + Relay, over WebSockets so a
    /// browser and a phone can meet) for up to ten players, and its code goes out with the
    /// answer or the invitation. A visitor joins, flies over to the island as the host has just
    /// saved it (Visit, from Cloud Save), and the games swap small notes over the link
    /// (LiveLink): where each capybara is, emojis, things put down and popped, hello and goodbye.
    ///
    /// The host's game is the hub: visitors only talk to it, and it passes each note on to
    /// everyone else, saying whose it is. It also plays the islanders (bots) who came to its
    /// party and tells the others where they are, and it keeps a contest's clock and scores
    /// (Gathering). The host's island stays the host's: visitors' games show a copy.
    /// </summary>
    public static class Live
    {
        public enum Mode { None, Knocking, Hosting, Visiting }

        /// <summary>What this game is doing live right now.</summary>
        public static Mode Now { get; private set; }

        /// <summary>
        /// The other person: the host you're knocking on or visiting, or (hosting) your first
        /// visitor. With several visitors, see <see cref="Guests"/>.
        /// </summary>
        public static FriendProfile Friend
        {
            get
            {
                if (Now != Mode.Hosting) return _host;
                foreach (var p in _peers.Values) if (!p.Bot) return p.Who;
                return _expected.Count > 0 ? _expected[0].who : null;
            }
        }

        /// <summary>Connected to at least one other game.</summary>
        public static bool Connected => _link != null && _link.Connected;

        /// <summary>The other person's capybara here (your host's, or your first visitor's).</summary>
        public static VisitorBot Puppet
        {
            get
            {
                if (Now == Mode.Visiting && _host != null && _peers.TryGetValue(_host.member, out var h)) return h.Puppet;
                foreach (var p in _peers.Values) if (!p.Bot && p.Puppet != null) return p.Puppet;
                return null;
            }
        }

        /// <summary>The most players on one island at once, the host included.</summary>
        public const int MaxPlayers = 10;

        const float KnockWait = 45f, HostWait = 75f, SendEvery = 0.1f, BotSendEvery = 0.3f;

        /// <summary>Someone else live on this island: a real player, or (hosting) an islander your game plays.</summary>
        class Peer
        {
            public string Id;               // their player id, or "bot:…"
            public FriendProfile Who;
            public VisitorBot Puppet;
            public NetworkConnection Conn;  // host: their connection (real players only)
            public bool Arrived, Bot;
            public Vector2? Spot;
        }

        static ISession _session;
        static LiveLink _link;
        static FriendProfile _host;                                   // knocking on / visiting
        static readonly Dictionary<string, Peer> _peers = new();      // everyone else here, by id
        static readonly Dictionary<NetworkConnection, string> _byConn = new();
        static readonly List<(FriendProfile who, float until)> _expected = new();   // let in, on their way
        static RealFriends.Message _answer;
        static bool _installed;
        static float _nextSend, _nextBotSend;
        static Vector2 _lastSent;
        static string _pendingCode;

        /// <summary>One note between the games.</summary>
        [Serializable]
        class Note
        {
            // hi, at, emo, bye, gone (someone left: from); ask / build / nobuild (building
            // together); put (something placed), pop (a bubble collected), bubble (a found thing
            // given back as a bubble); leave (the host asks a visitor to go); gs (a gathering's
            // state, host to all), gn (a visitor's contest news for the host: data says what);
            // chat (a typed line: data, with the sender's name), chaton / chatoff (the host lets a
            // visitor chat, or stops them: VisitChat)
            public string t;
            public string from, to;   // whose note it is, and (host to one) who it's for
            public float x, y;
            public int e;
            public string name;
            public List<string> outfit;
            public float tint;
            public int seed;
            public string data;
            public string code;    // hi: their friend code, so others can ask to be friends
            public bool clocked;   // hi: clock is the sender's island clock (GameClock), for a visitor to follow
            public double clock;
        }

        // Once per launch, not SubsystemRegistration: SaveGame.ResetStatics runs those on every
        // island swap, and a live visit has to survive the flight.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void ResetSession()
        {
            Now = Mode.None;
            _host = null;
            _session = null;
            _link = null;
            _peers.Clear();
            _byConn.Clear();
            _expected.Clear();
            _answer = null;
            _installed = false;
            _pendingCode = null;
        }

        /// <summary>This player's id on the link (their Unity player id).</summary>
        public static string MyId
        {
            get
            {
                try { if (AuthenticationService.Instance.IsSignedIn) return AuthenticationService.Instance.PlayerId; }
                catch (Exception) { }
                return "me";
            }
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
            VisitorBot.BotEmoted -= OnBotEmoted;
            VisitorBot.BotEmoted += OnBotEmoted;
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

        /// <summary>An islander at your party waved: everyone else sees it too.</summary>
        static void OnBotEmoted(VisitorBot bot, Emoji e)
        {
            if (Now != Mode.Hosting || bot == null || string.IsNullOrEmpty(bot.NetId)) return;
            Send(new Note { t = "emo", from = bot.NetId, e = (int)e });
        }

        // ---------------------------------------------------------------- the island, shared

        /// <summary>You put something down: it appears on the others' screens too.</summary>
        static void OnPlaced(Placeable p)
        {
            if (p == null || !OnSharedIsland) return;
            var item = p.GetComponent<CatalogItem>();
            if (item == null) return;
            Vector2 at = p.transform.position;
            Send(new Note { t = "put", name = item.StoreName, x = at.x, y = at.y });
        }

        /// <summary>You popped a bubble: it's gone on the others' screens too.</summary>
        static void OnCollected(GameObject item, Bubble from)
        {
            if (item == null || from == null || !OnSharedIsland) return;
            var tag = item.GetComponent<CatalogItem>();
            if (tag == null) return;
            Vector2 at = from.transform.position;
            Send(new Note { t = "pop", name = tag.StoreName, x = at.x, y = at.y });
        }

        /// <summary>Puts down what someone else put down (silently: it's their placement, not yours).</summary>
        static void PutRemote(string name, Vector2 at, FriendProfile by)
        {
            // On your island: chores always, other building only for a friend you said yes to,
            // bombs and story things never.
            if (Now == Mode.Hosting && !CoBuild.IsChore(name)
                && (!CoBuild.GuestAllowed || by == null || !Friends.IsFriend(by.id) || StoreData.StaysHome(name))) return;
            var catalog = ItemCatalog.Instance;
            if (catalog == null || string.IsNullOrEmpty(name)) return;
            var go = catalog.MakeItem(name, at);
            var fx = HarvestEffects.Instance;
            if (go != null && fx != null) fx.Poof(at, Color.white);
        }

        /// <summary>Someone popped a bubble: the same one goes here (the nearest holding the same thing).</summary>
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
        /// Leaving someone's island live: what you found there goes back into bubbles on their
        /// real island too (Visit keeps the same rule on your copy).
        /// </summary>
        public static void GiveBackFound(List<BubbleSave> found)
        {
            if (Now != Mode.Visiting || found == null) return;
            foreach (var b in found) Send(new Note { t = "bubble", name = b.name, x = b.pos.x, y = b.pos.y });
        }

        /// <summary>Building together: the visitor asks, the host answers (CoBuild).</summary>
        public static void AskToBuild() => Send(new Note { t = "ask" });

        public static void AnswerBuild(FriendProfile guest, bool yes)
        {
            var peer = PeerOf(guest);
            if (peer != null) SendTo(peer, new Note { t = yes ? "build" : "nobuild" });
        }

        /// <summary>Chatting: the host lets one visitor chat, or stops them (VisitChat).</summary>
        public static void AnswerChat(FriendProfile guest, bool on)
        {
            if (Now != Mode.Hosting) return;
            var peer = PeerOf(guest);
            if (peer != null) SendTo(peer, new Note { t = on ? "chaton" : "chatoff" });
        }

        /// <summary>
        /// A line you typed: a visitor's goes to the host; the host's goes only to the visitors
        /// it lets chat. VisitChat has cleaned it already.
        /// </summary>
        public static void SendChat(string text)
        {
            if (Now == Mode.Visiting) { Send(new Note { t = "chat", name = PlayerProfile.Name, data = text }); return; }
            if (Now != Mode.Hosting) return;
            foreach (var p in _peers.Values)
                if (!p.Bot && p.Arrived && VisitChat.GuestAllowed(p.Who)) SendTo(p, new Note { t = "chat", name = PlayerProfile.Name, data = text });
        }

        /// <summary>
        /// You just blocked someone (RealFriends.Block): wherever they are in your live visit, they
        /// go. Your visitor is sent home; the host you're visiting means flying home yourself; a
        /// fellow guest at someone's party simply disappears from your screen.
        /// </summary>
        public static void Blocked(string member)
        {
            if (string.IsNullOrEmpty(member)) return;
            if (Now == Mode.Knocking && _host != null && _host.member == member) { CancelKnock(); return; }
            _expected.RemoveAll(x => x.who.member == member);
            if (Now == Mode.Hosting)
            {
                if (_peers.TryGetValue(member, out var guest) && !guest.Bot) AskToLeave(guest.Who);
                return;
            }
            if (Now != Mode.Visiting) return;
            if (_host != null && _host.member == member)
            {
                Gathering.Ended();
                _ = End();
                if (Visit.Active) Visit.FlyHome();
                return;
            }
            if (_peers.ContainsKey(member)) Left(member, said: false);
        }

        /// <summary>The host asks one visitor to go: they fly home.</summary>
        public static void AskToLeave(FriendProfile guest)
        {
            if (Now != Mode.Hosting) return;
            var peer = PeerOf(guest);
            if (peer == null) return;
            SendTo(peer, new Note { t = "leave" });
            Left(peer.Id, said: false);
        }

        static Peer PeerOf(FriendProfile who)
        {
            if (who == null) return null;
            foreach (var p in _peers.Values) if (p.Who == who || (!p.Bot && p.Who != null && p.Who.member == who.member && who.member != null)) return p;
            return null;
        }

        /// <summary>Real players here with you right now (hosting: your visitors; visiting: your host and the other visitors).</summary>
        public static List<FriendProfile> Guests
        {
            get
            {
                var list = new List<FriendProfile>();
                foreach (var p in _peers.Values) if (!p.Bot && p.Arrived) list.Add(p.Who);
                return list;
            }
        }

        // ---------------------------------------------------------------- visiting

        /// <summary>Should a visit to this friend knock first (they're online and at home)?</summary>
        public static bool ShouldKnock(FriendProfile to) =>
            to != null && to.IsReal && Now == Mode.None && RealFriends.Ready && RealFriends.IsOnline(to) && !RealFriends.IsAway(to);

        /// <summary>
        /// Has this real person said yes to you coming over (let you in, or invited you)? A real
        /// island is only ever flown to with their yes, never on your own say-so.
        /// </summary>
        public static bool MayLand(FriendProfile to) =>
            to != null && Now == Mode.Visiting && _host != null && _host.id == to.id;

        /// <summary>Why you can't fly to a real person right now (and nothing is sent), for Visit.</summary>
        public static string WhyNotKnock(FriendProfile to)
        {
            if (Now == Mode.Knocking) return _host != null && _host.id == to.id ? null : UI.Loc.F("You're still waiting for {0} to answer.", _host?.name ?? "");
            if (Now == Mode.Hosting) return Gathering.Active
                ? "You're hosting right now. End it first, then you can fly off."
                : UI.Loc.F("{0} is on your island right now. Say goodbye first!", Friend?.name ?? "");
            if (!RealFriends.Ready) return "Can't reach the internet right now. Try again in a moment.";
            if (!RealFriends.IsOnline(to)) return UI.Loc.F("{0} is offline right now. You can visit when they're online!", to.name);
            if (RealFriends.IsAway(to)) return UI.Loc.F("{0} is visiting someone else right now.", to.name);
            return null;
        }

        /// <summary>Knocks on a friend's door; if they let you in, flies you over to visit live.</summary>
        public static async void Knock(FriendProfile to)
        {
            if (Now != Mode.None) return;
            Now = Mode.Knocking;
            _host = to;
            _answer = null;
            UI.KnockPanel.Show(to, KnockWait, CancelKnock);
            if (!await RealFriends.Send(to, "knock")) { Fail("Couldn't reach them just now. Try again in a moment."); return; }

            float until = Time.realtimeSinceStartup + KnockWait;
            while (_answer == null && Now == Mode.Knocking && _host == to && Time.realtimeSinceStartup < until) await Task.Yield();
            if (Now != Mode.Knocking || _host != to) return;   // cancelled
            UI.KnockPanel.Hide();
            var answer = _answer;
            if (answer == null)
            {
                _ = RealFriends.Send(to, "cancel");   // their question goes away too
                Fail(UI.Loc.F("{0} didn't answer. Maybe they're busy. Try again later!", to.name));
                return;
            }
            switch (answer.kind)
            {
                case "open": break;
                case "away": Fail(UI.Loc.F("{0} is visiting someone else right now.", to.name)); return;
                case "busy": Fail(UI.Loc.F("{0} already has a visitor. Try again a bit later!", to.name)); return;
                case "no": Fail(UI.Loc.F("{0} said not right now. Maybe another time!", to.name)); return;
                default: Fail(UI.Loc.F("{0} can't have visitors right now.", to.name)); return;
            }
            FlyOver(to, answer.text);
        }

        /// <summary>
        /// An invitation accepted (a party or a contest): straight over, the invitation is the
        /// host's yes. Ends whatever live visit you were on first.
        /// </summary>
        public static async void Accept(FriendProfile host, string code)
        {
            if (host == null || string.IsNullOrEmpty(code)) return;
            if (Now == Mode.Knocking) CancelKnock();
            if (Now == Mode.Hosting) { Hints.Answer("You're hosting right now. End it first, then you can fly off."); return; }
            if (Now == Mode.Visiting) await End();
            FlyOver(host, code);
        }

        /// <summary>Fly first, connect on landing: a flight reloads the whole scene, which a live link doesn't survive.</summary>
        static void FlyOver(FriendProfile to, string code)
        {
            _pendingCode = code;
            _host = to;
            Now = Mode.Visiting;
            _peers.Clear();
            VisitChat.Reset();   // chat waits for this host's say-so
            _ = RealFriends.Heartbeat(visiting: true);
            Visit.Fly(to);   // fetches their island as they've just saved it
        }

        /// <summary>You stopped knocking (the panel's Cancel): they hear it, and nobody flies anywhere.</summary>
        public static void CancelKnock()
        {
            if (Now != Mode.Knocking) return;
            var to = _host;
            Now = Mode.None;
            _host = null;
            UI.KnockPanel.Hide();
            if (to != null) _ = RealFriends.Send(to, "cancel");
        }

        static void Fail(string why)
        {
            UI.KnockPanel.Hide();
            Now = Mode.None;
            _host = null;
            Hints.Answer(why);
        }

        /// <summary>The flight to them didn't happen after all (no airstrip, their island unreadable): nothing live either.</summary>
        public static void FlightFailed(FriendProfile to)
        {
            if (Now != Mode.Visiting || _host == null || to == null || _host.id != to.id || Visit.Active) return;
            Now = Mode.None;
            _host = null;
            _pendingCode = null;
            _ = RealFriends.Heartbeat(visiting: false);
        }

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

        /// <summary>Visit has built the host's island: their capybara steps out to meet you.</summary>
        public static bool ArrivedAt(FriendProfile host)
        {
            if (Now != Mode.Visiting || _host == null || host == null || host.id != _host.id) return false;
            Connect(host);
            return true;
        }

        /// <summary>Landed on the host's island: now the link, then their capybara steps out.</summary>
        static async void Connect(FriendProfile host)
        {
            string code = _pendingCode;
            _pendingCode = null;
            bool joined = !string.IsNullOrEmpty(code) && await Join(code);
            if (Now != Mode.Visiting || _host == null || _host.id != host.id) return;   // flew off meanwhile
            if (!joined)
            {
                Hints.Show(Gathering.JoiningSomething
                    ? UI.Loc.F("{0}'s party is over already. You can look around, or fly home.", host.name)
                    : UI.Loc.F("{0} went offline. You can look around, or fly home.", host.name));
                Gathering.Ended();
                await End(false);
                return;
            }
            var peer = PeerFor(host.member, host, bot: false);
            peer.Arrived = true;
            SayHello();
            SpawnPuppet(peer, VisitorBot.Role.Host);
            Hints.Show(Gathering.JoiningSomething
                ? UI.Loc.F("You're at {0}'s island! Say hi with an emoji.", host.name)
                : UI.Loc.F("You're on {0}'s island together! Say hi with an emoji.", host.name));
        }

        // ---------------------------------------------------------------- hosting

        static void OnFriendMessage(string from, RealFriends.Message m)
        {
            if (RealFriends.IsBlocked(from)) return;   // no knocks, answers or invitations from someone you blocked
            var who = RealFriends.Find(from);
            if (who == null || m == null) return;
            switch (m.kind)
            {
                case "knock": Knocked(who); return;
                case "cancel": KnockWithdrawn(who); return;
                case "party": Gathering.Invited(who, m.text); return;
            }
            if (Now == Mode.Knocking && _host != null && _host.member == from) _answer = m;
        }

        static string _askedBy;   // whose knock the "let them in?" question is about, while it's up

        static void Knocked(FriendProfile who)
        {
            Debug.Log("[Live] " + who.name + " knocked (visiting=" + Visit.Active + ", now=" + Now + ")");
            if (Visit.Active || Visit.Travelling || Now == Mode.Visiting || Now == Mode.Knocking) { _ = RealFriends.Send(who, "away"); return; }
            if (Now == Mode.Hosting && _peers.Count + _expected.Count >= MaxPlayers - 1) { _ = RealFriends.Send(who, "busy"); return; }
            // Your party's open to all: come on in.
            if (Gathering.Hosting) { LetIn(who); return; }
            _askedBy = who.member;
            UI.PromptScreen.Ask("Visitor!", who, UI.Loc.F("<b>{0}</b> wants to visit your island!", who.name),
                "Let them in", "Not now",
                () => { _askedBy = null; LetIn(who); },
                () => { _askedBy = null; _ = RealFriends.Send(who, "no"); });
        }

        /// <summary>They stopped knocking (cancelled, or gave up waiting): the question goes, and a yes is too late.</summary>
        static void KnockWithdrawn(FriendProfile who)
        {
            if (_askedBy == who.member)
            {
                _askedBy = null;
                UI.PromptScreen.Dismiss();
                Hints.Show(UI.Loc.F("{0} stopped knocking.", who.name));
            }
            // Already said yes and opening up for them: they won't be coming.
            _expected.RemoveAll(x => x.who.member == who.member);
        }

        static async void LetIn(FriendProfile who)
        {
            if (Visit.Active || Visit.Travelling) { _ = RealFriends.Send(who, "busy"); return; }
            // Expected from now, so the island isn't closed again as soon as it opens.
            _expected.RemoveAll(x => x.who.member == who.member);
            _expected.Add((who, Time.realtimeSinceStartup + HostWait + 30f));
            string code = await OpenIsland();
            if (code == null)
            {
                _expected.RemoveAll(x => x.who.member == who.member);
                _ = RealFriends.Send(who, "no");
                Hints.Answer("Couldn't open your island just now. Try again in a moment.");
                return;
            }
            await RealFriends.Send(who, "open", code);
            Hints.Show(UI.Loc.F("{0} is on the way!", who.name));
        }

        static Task<string> _opening;

        static bool Opening => _opening != null && !_opening.IsCompleted;

        /// <summary>
        /// Opens your island for visitors (once; later calls get the same code): saves it, sends
        /// it up so they land on it as it is now, and starts the session. Null if it couldn't.
        /// </summary>
        public static Task<string> OpenIsland()
        {
            if (Now == Mode.Hosting && _session != null) return Task.FromResult(_session.Code);
            if (Opening) return _opening;
            return _opening = Open();

            static async Task<string> Open()
            {
                try
                {
                    if (Now != Mode.None && Now != Mode.Hosting) return null;
                    Now = Mode.Hosting;
                    _host = null;
                    if (SaveGame.Instance != null) SaveGame.Instance.SaveHomeNow();
                    await CloudSync.PublishNow();
                    _link = new LiveLink(true);
                    _session = await MultiplayerService.Instance.CreateSessionAsync(new SessionOptions { MaxPlayers = MaxPlayers, IsPrivate = true, Type = "visit" }
                        .WithRelayNetwork()
                        .WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS })
                        .WithNetworkHandler(_link));
                    if (Now != Mode.Hosting) { await Close(); return null; }   // given up meanwhile
                    return _session.Code;
                }
                catch (Exception e)
                {
                    Debug.Log("[Live] Couldn't open the island: " + e.Message + (e.InnerException != null ? " / " + e.InnerException : ""));
                    await Close();
                    if (Now == Mode.Hosting) Now = Mode.None;
                    return null;
                }
            }
        }

        // ---------------------------------------------------------------- islanders at your party

        /// <summary>An islander your game plays has come to your party: everyone else sees them too.</summary>
        public static void AnnounceBot(VisitorBot bot)
        {
            if (bot == null || string.IsNullOrEmpty(bot.NetId)) return;
            _peers[bot.NetId] = new Peer { Id = bot.NetId, Who = bot.Who, Puppet = bot, Arrived = true, Bot = true };
            Send(BotHello(bot));
        }

        static Note BotHello(VisitorBot bot) => new()
        {
            t = "hi", from = bot.NetId, name = bot.Who.name, outfit = new List<string>(bot.Who.outfit), tint = bot.Who.tint, seed = bot.Who.seed,
            x = bot.transform.position.x, y = bot.transform.position.y, data = "bot",
        };

        /// <summary>An islander went home.</summary>
        public static void BotGone(VisitorBot bot)
        {
            if (bot == null || string.IsNullOrEmpty(bot.NetId)) return;
            _peers.Remove(bot.NetId);
            Send(new Note { t = "gone", from = bot.NetId });
        }

        // ---------------------------------------------------------------- gatherings

        /// <summary>The host tells everyone (or one newcomer) how the party or contest stands.</summary>
        public static void SendGathering(string json, string to = null)
        {
            if (Now != Mode.Hosting) return;
            var n = new Note { t = "gs", from = MyId, data = json };
            if (to != null && _peers.TryGetValue(to, out var p)) SendTo(p, n);
            else Send(n);
        }

        /// <summary>A visitor tells the host something about the contest ("catch", "out").</summary>
        public static void TellHost(string what) => Send(new Note { t = "gn", data = what });

        // ---------------------------------------------------------------- mermaid's shells

        /// <summary>
        /// A Mermaid's Shell tossed into the sea, or a catch in its ring (MermaidCharms): everyone
        /// on the island hears, so they all see the ring and share its tally. <paramref name="to"/>
        /// (hosting): just one newcomer, catching up on the shells already singing.
        /// </summary>
        public static void SendCharm(string what, Vector2 at, string data, string to = null)
        {
            if (!Connected || !OnSharedIsland) return;
            var n = new Note { t = "charm", name = what, x = at.x, y = at.y, data = data };
            if (to != null) { if (_peers.TryGetValue(to, out var p)) SendTo(p, n); }
            else Send(n);
        }

        // ---------------------------------------------------------------- ending

        /// <summary>
        /// Ends the live visit (or the hosting): the others hear goodbye, the link closes. Visit
        /// calls this when you fly anywhere; it's also what happens when the host goes.
        /// </summary>
        public static async Task End(bool sayBye = true)
        {
            if (Now == Mode.None) return;
            Debug.Log("[Live] Ending (" + Now + ", bye=" + sayBye + ")");
            if (sayBye) Send(new Note { t = "bye" });
            _pendingCode = null;
            Now = Mode.None;
            _host = null;
            _expected.Clear();
            foreach (var p in _peers.Values)
                if (p.Puppet != null && !p.Bot) p.Puppet.RemoteLeave();
            _peers.Clear();
            _byConn.Clear();
            VisitChat.Reset();
            Gathering.Ended();   // a party or contest ends with the link (yours, or the one you were at)
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
            _host = null;
            _peers.Clear();
            _byConn.Clear();
            _expected.Clear();
            VisitChat.Reset();
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

            if (Now == Mode.Hosting)
            {
                // Let in, but never came: they're not coming after all.
                for (int i = _expected.Count - 1; i >= 0; i--)
                {
                    if (Time.realtimeSinceStartup < _expected[i].until) continue;
                    var who = _expected[i].who;
                    _expected.RemoveAt(i);
                    Hints.Show(UI.Loc.F("{0} couldn't come after all.", who.name));
                }
                // Nobody here, nobody coming, no party: close up.
                if (_expected.Count == 0 && !Gathering.Hosting && !AnyRealPeer() && _session != null && !Opening)
                {
                    _ = End();
                    return;
                }
                if (Gathering.Hosting) OpenChatToAll();
                SendBots();
            }

            // Where you are, a few times a second while you're on the same island.
            if (!Connected || Time.unscaledTime < _nextSend || !OnSharedIsland) return;
            _nextSend = Time.unscaledTime + SendEvery;
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            if (king == null) return;
            Vector2 at = king.transform.position;
            if ((at - _lastSent).sqrMagnitude < 0.0025f && Time.frameCount % 30 != 0) return;
            _lastSent = at;
            Send(new Note { t = "at", x = at.x, y = at.y });
        }

        /// <summary>
        /// Hosting a party or contest: everyone here may chat, nobody is asked (VisitChat.Welcome).
        /// Checked every frame, so it covers a newcomer the moment they land, a guest already here
        /// when the party started, and someone back after a dropped link; each one's game hears it
        /// once. Whoever the host turned off stays off.
        /// </summary>
        static void OpenChatToAll()
        {
            foreach (var p in _peers.Values)
                if (!p.Bot && p.Arrived && p.Who != null && VisitChat.Welcome(p.Who)) SendTo(p, new Note { t = "chaton" });
        }

        static bool AnyRealPeer()
        {
            foreach (var p in _peers.Values) if (!p.Bot) return true;
            return false;
        }

        /// <summary>Hosting: where the islanders at your party are, for everyone else's screen.</summary>
        static void SendBots()
        {
            if (!Connected || Time.unscaledTime < _nextBotSend || !OnSharedIsland) return;
            _nextBotSend = Time.unscaledTime + BotSendEvery;
            foreach (var p in _peers.Values)
            {
                if (!p.Bot || p.Puppet == null) continue;
                Vector2 at = p.Puppet.transform.position;
                Send(new Note { t = "at", from = p.Id, x = at.x, y = at.y });
            }
        }

        /// <summary>Both of you are standing on the host's island (not mid-flight, not elsewhere).</summary>
        static bool OnSharedIsland =>
            Now == Mode.Hosting ? !Visit.Active && !Visit.Travelling
            : Now == Mode.Visiting && Visit.Active && !Visit.Travelling && Visit.Host != null && _host != null && Visit.Host.id == _host.id;

        static void OnJoined(NetworkConnection c)
        {
            if (Now != Mode.Hosting) return;
            // A newcomer: hello from you, from everyone already here, and how things stand.
            SendTo(c, HelloNote(MyId));
            foreach (var p in _peers.Values)
            {
                if (!p.Arrived) continue;
                if (p.Bot && p.Puppet != null) SendTo(c, BotHello(p.Puppet));
                else if (!p.Bot) SendTo(c, new Note { t = "hi", from = p.Id, name = p.Who.name, outfit = new List<string>(p.Who.outfit), seed = p.Who.seed,
                    code = p.Who.code, x = p.Spot?.x ?? 0f, y = p.Spot?.y ?? 0f });
            }
            // How the party or contest stands follows once they say who they are (Gathering.Arrived).
        }

        static void OnLeft(NetworkConnection c)
        {
            if (Now == Mode.None) return;
            if (Now == Mode.Hosting)
            {
                if (_byConn.TryGetValue(c, out var id)) Left(id, said: true);
                _byConn.Remove(c);
                return;
            }
            // The host's game went: the visit is over.
            var who = _host;
            Gathering.Ended();
            _ = End(false);
            if (who != null) Hints.Show(UI.Loc.F("{0} went offline. You can look around, or fly home.", who.name));
        }

        /// <summary>Someone else left (flew home, closed the game, was asked to go).</summary>
        static void Left(string id, bool said)
        {
            if (string.IsNullOrEmpty(id) || !_peers.TryGetValue(id, out var p)) return;
            _peers.Remove(id);
            if (p.Puppet != null) p.Puppet.RemoteLeave();
            if (Now == Mode.Hosting)
            {
                Send(new Note { t = "gone", from = id });
                Gathering.Gone(id);
                VisitChat.GuestGone(id);
                if (said && p.Who != null && !p.Bot) Hints.Show(UI.Loc.F("{0} flew home. See you next time!", p.Who.name));
            }
            else if (!p.Bot && p.Who != null && said) Hints.Show(UI.Loc.F("{0} flew home. See you next time!", p.Who.name));
        }

        static void OnNote(NetworkConnection c, string text)
        {
            Note n;
            try { n = JsonUtility.FromJson<Note>(text); } catch (Exception) { return; }
            if (n == null) return;
            if (Now == Mode.Hosting) HostNote(c, n);
            else if (Now == Mode.Visiting) VisitorNote(n);
        }

        /// <summary>A visitor's note: act on it here, and pass it on to everyone else.</summary>
        static void HostNote(NetworkConnection c, Note n)
        {
            string id = n.t == "hi" ? n.from : _byConn.TryGetValue(c, out var known) ? known : n.from;
            if (string.IsNullOrEmpty(id)) return;
            n.from = id;
            if (RealFriends.IsBlocked(id))
            {
                // Someone you blocked came in (on a party's open code): sent home as if asked to
                // go, and nothing of theirs is shown here or passed on.
                if (n.t == "hi") SendTo(c, new Note { t = "leave" });
                return;
            }
            _peers.TryGetValue(id, out var peer);
            switch (n.t)
            {
                case "hi":
                {
                    _byConn[c] = id;
                    peer = PeerFor(id, null, bot: false, n.name, n.outfit, n.seed);
                    if (string.IsNullOrEmpty(peer.Who.code)) peer.Who.code = n.code;
                    peer.Conn = c;
                    if (!peer.Arrived && OnSharedIsland)
                    {
                        peer.Arrived = true;
                        _expected.RemoveAll(x => x.who.member == id);
                        peer.Spot = new Vector2(n.x, n.y);
                        SpawnPuppet(peer, VisitorBot.Role.Guest);
                        Sfx.Play(SfxKind.Bridge, 0.6f);
                        Hints.Show(UI.Loc.F("{0} has landed on your island!", peer.Who.name));
                        if (!Gathering.Hosting) CoBuild.AskHost(peer.Who);
                        if (!Gathering.Hosting) VisitChat.AskHost(peer.Who);   // after the building question (Attention: one at a time)
                        Gathering.Arrived(id, peer.Who);
                        MermaidCharms.Greet(id);   // the shells already singing in your sea
                    }
                    // Back after a dropped link with chat already on: their game hears it again.
                    if (VisitChat.GuestAllowed(peer.Who)) SendTo(peer, new Note { t = "chaton" });
                    Relay(n, c);
                    break;
                }
                case "chat":
                {
                    // Only from a visitor you let chat; passed on only to the others you let chat.
                    if (peer == null || peer.Bot || !VisitChat.GuestAllowed(peer.Who)) return;
                    string text = VisitChat.Clean(n.data);
                    if (text.Length == 0) return;
                    VisitChat.Received(id, peer.Who.name, text);
                    foreach (var p in _peers.Values)
                        if (p != peer && !p.Bot && p.Arrived && VisitChat.GuestAllowed(p.Who))
                            SendTo(p, new Note { t = "chat", from = id, name = peer.Who.name, data = text });
                    break;
                }
                case "at":
                    if (peer == null) return;
                    peer.Spot = new Vector2(n.x, n.y);
                    if (peer.Puppet != null && OnSharedIsland) peer.Puppet.RemoteAt(peer.Spot.Value);
                    Relay(n, c);
                    break;
                case "emo":
                    if (peer?.Puppet != null) peer.Puppet.Emote((Emoji)n.e);
                    Relay(n, c);
                    break;
                case "put":
                    if (OnSharedIsland) PutRemote(n.name, new Vector2(n.x, n.y), peer?.Who);
                    Relay(n, c);
                    break;
                case "pop":
                    if (OnSharedIsland) PopRemote(n.name, new Vector2(n.x, n.y));
                    Relay(n, c);
                    break;
                case "bubble":
                    if (ItemCatalog.Instance != null && !string.IsNullOrEmpty(n.name)) ItemCatalog.Instance.MakeStore(n.name, new Vector2(n.x, n.y));
                    break;
                case "ask":
                    if (peer?.Who != null) CoBuild.AskHost(peer.Who);
                    break;
                case "gn":
                    Gathering.FromGuest(id, n.data);
                    break;
                case "charm":
                    if (OnSharedIsland) MermaidCharms.FromNet(n.name, new Vector2(n.x, n.y), n.data);
                    Relay(n, c);
                    break;
                case "bye":
                    Left(id, said: true);
                    _byConn.Remove(c);
                    break;
            }
        }

        /// <summary>A note from the host (its own, or one it passed on from someone else).</summary>
        static void VisitorNote(Note n)
        {
            if (_host == null) return;
            string id = string.IsNullOrEmpty(n.from) ? _host.member : n.from;
            bool fromHost = id == _host.member;
            if (id == MyId) return;   // our own, back again
            // A fellow guest you blocked: not shown here at all (their capybara, waves, lines).
            if (!fromHost && RealFriends.IsBlocked(id) && n.t != "gone" && n.t != "bye") return;
            _peers.TryGetValue(id, out var peer);
            switch (n.t)
            {
                case "hi":
                    if (fromHost)
                    {
                        if (n.outfit != null) _host.outfit = n.outfit.FindAll(w => Wardrobe.Find(w) != null);
                        if (!string.IsNullOrEmpty(n.name)) _host.name = n.name;
                        // Their island's time of day, not yours: night there is night here.
                        if (n.clocked && OnSharedIsland) GameClock.Visiting = n.clock;
                        break;
                    }
                    // Someone else at the party (a player, or an islander the host plays).
                    peer = PeerFor(id, null, bot: n.data == "bot", n.name, n.outfit, n.seed, n.tint);
                    if (string.IsNullOrEmpty(peer.Who.code)) peer.Who.code = n.code;
                    peer.Spot = new Vector2(n.x, n.y);
                    if (!peer.Arrived && OnSharedIsland)
                    {
                        peer.Arrived = true;
                        SpawnPuppet(peer, VisitorBot.Role.Guest);
                    }
                    break;
                case "at":
                    if (peer == null) return;
                    peer.Spot = new Vector2(n.x, n.y);
                    if (peer.Puppet != null && OnSharedIsland) peer.Puppet.RemoteAt(peer.Spot.Value);
                    break;
                case "emo":
                    if (peer?.Puppet != null) peer.Puppet.Emote((Emoji)n.e);
                    break;
                case "put":
                    if (OnSharedIsland) PutRemote(n.name, new Vector2(n.x, n.y), peer?.Who);
                    break;
                case "pop":
                    if (OnSharedIsland) PopRemote(n.name, new Vector2(n.x, n.y));
                    break;
                case "build":
                case "nobuild":
                    CoBuild.Granted(n.t == "build");
                    break;
                case "chaton":
                case "chatoff":
                    if (fromHost) VisitChat.Granted(n.t == "chaton", _host.name);
                    break;
                case "chat":
                    // The host only sends lines while you may chat; checked here too.
                    if (VisitChat.Allowed) VisitChat.Received(id, fromHost ? _host.name : peer?.Who?.name ?? n.name, n.data);
                    break;
                case "leave":
                {
                    var host = _host;
                    Gathering.Ended();
                    _ = End(false);
                    Hints.Show(UI.Loc.F("{0} has to go now. Flying you home!", host.name));
                    Visit.FlyHome();
                    break;
                }
                case "gs":
                    Gathering.FromHost(n.data);
                    break;
                case "charm":
                    if (OnSharedIsland) MermaidCharms.FromNet(n.name, new Vector2(n.x, n.y), n.data);
                    break;
                case "gone":
                    Left(id, said: true);
                    break;
                case "bye":
                    if (fromHost) OnLeft(default);
                    else Left(id, said: true);
                    break;
            }
        }

        /// <summary>Someone's peer record, made the first time they're heard of.</summary>
        static Peer PeerFor(string id, FriendProfile known, bool bot, string name = null, List<string> outfit = null, int seed = 0, float tint = 0f)
        {
            if (_peers.TryGetValue(id, out var p)) return p;
            var who = known;
            if (who == null && !bot) who = RealFriends.Find(id) ?? PrivateServer.Find(id);
            if (who == null)
            {
                who = new FriendProfile
                {
                    id = bot ? id.Substring(4) : "u_" + id,
                    member = bot ? null : id,
                    name = string.IsNullOrEmpty(name) ? "Capy" : name,
                    seed = seed != 0 ? seed : id.GetHashCode(),
                    tint = tint,
                };
                if (outfit != null) who.outfit = outfit.FindAll(w => Wardrobe.Find(w) != null);
            }
            else if (outfit != null && !Friends.IsFriend(who.id)) who.outfit = outfit.FindAll(w => Wardrobe.Find(w) != null);
            p = new Peer { Id = id, Who = who, Bot = bot };
            _peers[id] = p;
            return p;
        }

        static Note HelloNote(string from)
        {
            var outfit = new List<string>();
            foreach (WearSlot slot in Enum.GetValues(typeof(WearSlot)))
            {
                var worn = Wardrobe.Worn(slot);
                if (worn != null) outfit.Add(worn.Id);
            }
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            Vector2 at = king != null ? (Vector2)king.transform.position : Vector2.zero;
            return new Note { t = "hi", from = from, name = PlayerProfile.Name, outfit = outfit, x = at.x, y = at.y, code = RealFriends.Code,
                clocked = Now == Mode.Hosting, clock = GameClock.IslandOffset };
        }

        static void SayHello()
        {
            Send(HelloNote(MyId));
            _lastSent = new Vector2(float.NaN, float.NaN);   // and where you are, straight after
            _nextSend = 0f;
        }

        static void SpawnPuppet(Peer peer, VisitorBot.Role role)
        {
            if (peer.Puppet != null || peer.Who == null) return;
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            Vector2 near = king != null ? (Vector2)king.transform.position + new Vector2(role == VisitorBot.Role.Host ? 2.4f : -2.4f, 0.6f) : Vector2.zero;
            Vector2 at = peer.Spot.HasValue && peer.Spot.Value != Vector2.zero ? peer.Spot.Value : near;
            if (!BunniSpace.PointOnLand(at)) at = king != null ? (Vector2)king.transform.position : at;
            peer.Puppet = VisitorBot.SpawnRemote(peer.Who, role, at);
            peer.Puppet.NetId = peer.Id;
        }

        /// <summary>A note out: a visitor's goes to the host; the host's to every visitor.</summary>
        static void Send(Note n)
        {
            if (!Connected) return;
            if (string.IsNullOrEmpty(n.from)) n.from = MyId;
            _link.Send(JsonUtility.ToJson(n));
        }

        static void SendTo(Peer p, Note n)
        {
            if (p == null || p.Bot) return;
            if (Now == Mode.Hosting) SendTo(p.Conn, n);
            else { n.to = p.Id; Send(n); }
        }

        static void SendTo(NetworkConnection c, Note n)
        {
            if (_link == null || c == default) return;
            if (string.IsNullOrEmpty(n.from)) n.from = MyId;
            _link.SendTo(c, JsonUtility.ToJson(n));
        }

        /// <summary>Hosting: a visitor's note on to everyone else.</summary>
        static void Relay(Note n, NetworkConnection except)
        {
            if (_link == null) return;
            _link.SendExcept(except, JsonUtility.ToJson(n));
        }
    }

    /// <summary>Ticks Live and Gathering every frame, across island swaps.</summary>
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

        void Update()
        {
            Live.Tick();
            Gathering.Tick();
        }

        // Closing the game (or stopping Play): the link closes at once, so nothing is left
        // running when the game goes (an open driver kept the editor from reloading).
        void OnApplicationQuit() { Live.Shutdown(); PrivateServer.Shutdown(); }
        void OnDestroy() { Live.Shutdown(); PrivateServer.Shutdown(); }
    }

    /// <summary>
    /// The wire between the games: Unity Transport over Relay WebSockets, with reliable, ordered
    /// notes (fragmented if long). Sessions start and stop it (INetworkHandler). The host's end
    /// takes up to nine visitors; a visitor's end talks only to the host.
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

        public void Tick(Action<NetworkConnection, string> onNote, Action<NetworkConnection> onJoined, Action<NetworkConnection> onLeft)
        {
            if (!_driver.IsCreated) return;
            _driver.ScheduleUpdate().Complete();
            if (_host)
            {
                NetworkConnection c;
                while ((c = _driver.Accept()) != default)
                {
                    _peers.Add(c);
                    onJoined(c);
                }
                for (int i = _peers.Count - 1; i >= 0; i--)
                    if (i < _peers.Count) Pump(_peers[i], onNote, onLeft);
            }
            else if (_server != default) Pump(_server, onNote, onLeft, onJoined);
        }

        void Pump(NetworkConnection c, Action<NetworkConnection, string> onNote, Action<NetworkConnection> onLeft, Action<NetworkConnection> onJoined = null)
        {
            NetworkEvent.Type type;
            while (_driver.IsCreated && (type = _driver.PopEventForConnection(c, out var reader)) != NetworkEvent.Type.Empty)
            {
                switch (type)
                {
                    case NetworkEvent.Type.Connect:
                        _clientUp = true;
                        onJoined?.Invoke(c);
                        break;
                    case NetworkEvent.Type.Data:
                        int length = reader.ReadInt();
                        if (length <= 0 || length > 64 * 1024) break;
                        var bytes = new NativeArray<byte>(length, Allocator.Temp);
                        reader.ReadBytes(bytes);
                        onNote(c, Encoding.UTF8.GetString(bytes.ToArray()));
                        bytes.Dispose();
                        break;
                    case NetworkEvent.Type.Disconnect:
                        Debug.Log("[Live] Link closed (" + (_host ? "host" : "visitor") + "): " +
                            (reader.Length > 0 ? ((Unity.Networking.Transport.Error.DisconnectReason)reader.ReadByte()).ToString() : "no reason"));
                        _peers.Remove(c);
                        if (!_host) { _clientUp = false; _server = default; }
                        onLeft(c);
                        return;
                }
            }
        }

        /// <summary>To everyone at the other end (the host: every visitor; a visitor: the host).</summary>
        public void Send(string text) => SendExcept(default, text);

        /// <summary>To everyone but one visitor (the host passing a note on).</summary>
        public void SendExcept(NetworkConnection except, string text)
        {
            if (!_driver.IsCreated) return;
            var data = Encoding.UTF8.GetBytes(text);
            using var bytes = new NativeArray<byte>(data, Allocator.Temp);
            if (_host) { foreach (var c in _peers) if (c != except) SendTo(c, bytes); }
            else if (_clientUp) SendTo(_server, bytes);
        }

        /// <summary>To one visitor only.</summary>
        public void SendTo(NetworkConnection c, string text)
        {
            if (!_driver.IsCreated || c == default) return;
            var data = Encoding.UTF8.GetBytes(text);
            using var bytes = new NativeArray<byte>(data, Allocator.Temp);
            SendTo(c, bytes);
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
