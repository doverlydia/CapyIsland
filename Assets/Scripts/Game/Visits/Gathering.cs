using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    public enum GatheringKind { Party, Fishing, Bugs, Birds }

    /// <summary>
    /// Playing together on one island: an island party, or a fishing, bug or birdwatching contest.
    ///
    /// <b>Hosting.</b> From your own island you invite everyone on your server: real friends who
    /// are online (a Friends message), everyone on your private server, friends or not (its lobby,
    /// PrivateServer), and on a public server the islanders the game makes up, each of whom says
    /// yes or no and turns up a little later (your game plays them, and Live shows them to
    /// everyone else). A party is just company: guests wander about and lend a hand.
    ///
    /// <b>A contest</b> happens on the island you host it from: the whole island for bugs and
    /// birds, its shore for fishing. Playtesters found a small ring too fiddly to stay in; an island
    /// has edges everyone already understands, and a bridge is the obvious way out. The area
    /// glows while it's on (EventGlow: the sea all along the island's coast, and for anything but
    /// fishing its land too), so it stands out on the map as well, and fish, bugs and birds
    /// turn up around you far more often while you're in it. Guests land there. When at least two
    /// are present the host presses Start: five minutes, and whoever catches the most wins. Late
    /// arrivals join in with what's left of the clock. Cross a bridge off the island (or, fishing,
    /// wander inland away from the water) and you have ten seconds to come back, or you're out of
    /// that round. Out isn't for good: come back into the area while the round still runs and
    /// you're asked whether to join in again (once each time you come back), with what you'd
    /// caught before still on the board. Everyone keeps what they catch (the catches are real
    /// ones: they go in the journal and count for the day's goal and the week's contest). Then the
    /// leaderboard, and the host can run another round or end it.
    ///
    /// The host's game keeps the clock and the scores and tells everyone (Live, "gs" notes);
    /// visitors tell it their catches. Only hobbies you have unlocked can be hosted, and a contest
    /// invitation only reaches people who have that hobby too.
    /// </summary>
    public static class Gathering
    {
        public const float ContestSeconds = 300f, OutGrace = 10f;

        /// <summary>
        /// How close to the water (Unity units) still counts as "on the shore" for a fishing
        /// contest: a little more than a cast's reach (Fishing.AtShore is 2.6), so stepping back to
        /// untangle the line doesn't start the countdown.
        /// </summary>
        public const float ShoreBand = 4f;

        public enum Phase { Lobby, Running, Results }

        [Serializable]
        public class Player
        {
            public string id, name;
            public int score;
            public bool isOut, gone, bot, playing;   // playing: in the round that's on (or the last one)
        }

        [Serializable]
        class Invitation
        {
            public string kind, code, host;
            public long at;   // UTC ticks
        }

        [Serializable]
        class State
        {
            public string kind, host;
            public int phase, round;
            public float x, y, left;
            // The event's island plus one, so a host on an older build (no field, read as 0)
            // can't be mistaken for island 0: then it's worked out from x, y instead.
            public int island1;
            public bool over;
            public List<Player> players = new();
        }

        /// <summary>Hosting a party or contest on your island.</summary>
        public static bool Hosting { get; private set; }

        /// <summary>At someone else's party or contest (connected, and told how it stands).</summary>
        public static bool Joined { get; private set; }

        /// <summary>Said yes to an invitation and on the way.</summary>
        public static bool JoiningSomething { get; private set; }

        public static bool Active => Hosting || Joined;

        public static GatheringKind Kind { get; private set; }
        public static Phase Now { get; private set; }

        /// <summary>Where the host stood to start it: a meeting point on the event island (guests land near it).</summary>
        public static Vector2 Arena { get; private set; }

        /// <summary>The island it's on (LandMap's index, the same on every player's game), or -1 when nothing's on.</summary>
        public static int EventIsland { get; private set; } = -1;
        public static string HostName { get; private set; }
        public static int Round { get; private set; }

        /// <summary>When the running round ends (realtime clock).</summary>
        public static float EndsAt { get; private set; }

        public static float SecondsLeft => Now == Phase.Running ? Mathf.Max(0f, EndsAt - Time.realtimeSinceStartup) : 0f;

        public static bool IsContest => Kind != GatheringKind.Party;

        /// <summary>Everyone at it, best score first (gone and out at the end).</summary>
        public static List<Player> Board
        {
            get
            {
                var list = new List<Player>(_players);
                list.Sort((a, b) =>
                {
                    int ra = a.gone || !a.playing ? 2 : a.isOut ? 1 : 0, rb = b.gone || !b.playing ? 2 : b.isOut ? 1 : 0;
                    if (ra != rb) return ra.CompareTo(rb);
                    return b.score.CompareTo(a.score);
                });
                return list;
            }
        }

        /// <summary>When the left-the-contest-area countdown runs out (realtime), or 0 while you're in it.</summary>
        public static float OutAt { get; private set; }

        /// <summary>Anything about the gathering changed (the panel redraws).</summary>
        public static event Action Changed;

        /// <summary>A round just finished: the leaderboard, for the results card.</summary>
        public static event Action<List<Player>> Finished;

        static readonly List<Player> _players = new();
        static readonly List<(FriendProfile who, float at)> _botsComing = new();
        static readonly Dictionary<string, float> _botSkill = new();
        static readonly HashSet<string> _invitesSeen = new();
        static string _code;
        static float _nextBroadcast;

        // Once per launch: a gathering survives the flight there (SaveGame.ResetStatics resets the rest).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void ResetSession()
        {
            Hosting = Joined = JoiningSomething = false;
            _players.Clear();
            _botsComing.Clear();
            _botSkill.Clear();
            _invitesSeen.Clear();
            Changed = null;
            Finished = null;
            OutAt = 0f;
            EventIsland = -1;
            _rejoinAsked = _backInArea = false;
            _rejoinRound = -1;
        }

        static void Notify() => Changed?.Invoke();

        public static Hobby HobbyOf(GatheringKind k) => k switch
        {
            GatheringKind.Bugs => Hobby.Bugs,
            GatheringKind.Birds => Hobby.Birding,
            _ => Hobby.Fishing,
        };

        /// <summary>Its name, as the player reads it (the week's contests share these names).</summary>
        public static string NameOf(GatheringKind k) => k switch
        {
            GatheringKind.Fishing => "Fishing Contest",
            GatheringKind.Bugs => "Bug Contest",
            GatheringKind.Birds => "Birdwatching Contest",
            _ => "Island Party",
        };

        /// <summary>What a catch is called on the scoreboard ("5 fish").</summary>
        public static string Counted(GatheringKind k, int n) => k switch
        {
            GatheringKind.Bugs => n == 1 ? "1 bug" : n + " bugs",
            GatheringKind.Birds => n == 1 ? "1 bird" : n + " birds",
            _ => n + " fish",
        };

        static Player Me => _players.Find(p => p.id == Live.MyId);

        // ---------------------------------------------------------------- hosting

        /// <summary>Why you can't host this right now, or null if you can.</summary>
        public static string WhyCantHost(GatheringKind kind)
        {
            if (Hosting) return "You're hosting already!";
            if (Joined || JoiningSomething) return "You're at someone else's party right now.";
            if (Visit.Active || Visit.Travelling) return "Host from your own island: fly home first.";
            if (Live.Now == Live.Mode.Knocking || Live.Now == Live.Mode.Visiting) return "You're on your way to visit someone right now.";
            if (Attention.TutorialRunning) return "Let's get your own island settled first.";
            if (kind != GatheringKind.Party && !HobbyLog.IsUnlocked(HobbyOf(kind))) return kind switch
            {
                GatheringKind.Bugs => "You need a bug net to host a bug contest.",
                GatheringKind.Birds => "You need binoculars to host a birdwatching contest.",
                _ => "You need a fishing rod to host a fishing contest.",
            };
            var spot = WhyNotHere(kind);
            if (spot != null) return spot;
            if (kind != GatheringKind.Party && Invitable(kind) == 0)
                return PrivateServer.Active
                    ? "A contest needs at least 2 players, and nobody else is on this server right now. Switch to a public server to play with others."
                    : "A contest needs at least 2 players, and nobody's online to invite right now. Try again in a little while.";
            return null;
        }

        /// <summary>Is where you're standing a place for this contest? Null if so.</summary>
        static string WhyNotHere(GatheringKind kind)
        {
            if (kind == GatheringKind.Party) return null;
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            if (king == null) return "Can't find you on the island!";
            Vector2 at = king.transform.position;
            int island = IslandFor(at);
            switch (kind)
            {
                case GatheringKind.Fishing:
                    // A fishing contest is the shore of an island, so it starts from one (not a bridge).
                    if (!Fishing.WaterNear(at, 2.6f).HasValue) return "Stand at the water's edge to host a fishing contest there.";
                    if (LandMap.Instance != null && LandMap.Instance.IslandAt(at) < 0)
                        return "Step off the bridge onto an island's shore to host a fishing contest there.";
                    break;
                case GatheringKind.Birds:
                    if (CountNodes(island, NodeFamily.Tree) < 2) return "This island doesn't have enough trees for a birdwatching contest. Try one with more.";
                    break;
                case GatheringKind.Bugs:
                    if (CountNodes(island, NodeFamily.Tree) + CountNodes(island, NodeFamily.Flower) < 2)
                        return "This island doesn't have enough trees or flowers for a bug contest. Try one with more.";
                    break;
            }
            return null;
        }

        /// <summary>Trees or flowers growing on an island.</summary>
        static int CountNodes(int island, NodeFamily family)
        {
            var land = LandMap.Instance;
            int n = 0;
            foreach (var node in UnityEngine.Object.FindObjectsByType<Harvestable>(FindObjectsSortMode.None))
                if (node.Family == family && node.InWorld && (land == null || land.IslandAt(node.transform.position) == island)) n++;
            return n;
        }

        /// <summary>
        /// The island a spot belongs to: the one under it, or, on a bridge, the nearest island's
        /// land (so hosting from a bridge still picks somewhere sensible). 0 if all else fails.
        /// </summary>
        static int IslandFor(Vector2 at)
        {
            var land = LandMap.Instance;
            if (land == null) return 0;
            int here = land.IslandAt(at);
            if (here >= 0) return here;
            for (float r = 0.5f; r <= 30f; r += 0.5f)
                for (int a = 0; a < 16; a++)
                {
                    float ang = a * Mathf.PI / 8f;
                    int island = land.IslandAt(at + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.5f) * r);
                    if (island >= 0) return island;
                }
            return 0;
        }

        /// <summary>The event island's name as the player reads it ("Pebble Cove").</summary>
        public static string AreaName
        {
            get
            {
                var piece = LandMap.Instance != null && EventIsland >= 0 ? LandMap.Instance.Island(EventIsland) : null;
                return piece != null ? StoryText.Name(piece.name) : "the island";
            }
        }

        /// <summary>How many people could be asked: real players online, people on your private server, and the islanders about.</summary>
        static int Invitable(GatheringKind kind)
        {
            var asked = new HashSet<string>();
            foreach (var f in RealFriends.All) if (RealFriends.IsOnline(f) && !RealFriends.IsAway(f)) asked.Add(f.member);
            if (PrivateServer.Active) foreach (var p in PrivateServer.Others()) asked.Add(p.member);
            int n = asked.Count;
            if (!PrivateServer.Active) foreach (var who in Server.Neighbours()) if (!who.IsReal) n++;
            return n;
        }

        /// <summary>Opens your island and sends everyone on the server an invitation.</summary>
        public static async void Host(GatheringKind kind)
        {
            var why = WhyCantHost(kind);
            if (why != null) { Hints.Answer(why); return; }
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            Hosting = true;
            Kind = kind;
            Now = Phase.Lobby;
            Round = 0;
            Arena = king != null ? (Vector2)king.transform.position : Vector2.zero;
            EventIsland = IslandFor(Arena);
            HostName = PlayerProfile.Name;
            _players.Clear();
            _players.Add(new Player { id = Live.MyId, name = PlayerProfile.Name });
            _botsComing.Clear();
            Notify();
            Hints.Answer("Opening your island…");

            string code = await Live.OpenIsland();
            if (!Hosting) return;   // ended meanwhile
            if (code == null)
            {
                Hosting = false;
                _players.Clear();
                EventIsland = -1;
                Notify();
                Hints.Answer("Couldn't open your island just now. Try again in a moment.");
                return;
            }
            _code = code;
            int asked = SendInvitations();
            Sfx.Play(SfxKind.Chest, 0.6f);
            Hints.Answer(kind == GatheringKind.Party
                ? UI.Loc.F("Party invitations sent to {0} islanders! Guests will turn up soon.", asked)
                : UI.Loc.F("Invitations sent to {0} islanders! Press Start once someone has arrived.", asked));
            Broadcast();
        }

        /// <summary>Everyone on the server hears about it. Returns how many were asked.</summary>
        static int SendInvitations()
        {
            var inv = JsonUtility.ToJson(new Invitation { kind = Kind.ToString(), code = _code, host = PlayerProfile.Name, at = DateTime.UtcNow.Ticks });
            var asked = new HashSet<string>();
            foreach (var f in RealFriends.All)
            {
                if (!RealFriends.IsOnline(f) || RealFriends.IsAway(f)) continue;
                asked.Add(f.member);
                _ = RealFriends.Send(f, "party", inv);
            }
            if (PrivateServer.Active)
            {
                foreach (var p in PrivateServer.Others()) asked.Add(p.member);
                PrivateServer.SetParty(inv);
            }
            int n = asked.Count;

            // The islanders on a public server: some can come, some can't, and the ones who can
            // turn up over the next half minute or so.
            if (!PrivateServer.Active)
                foreach (var who in Server.Neighbours())
                {
                    if (who.IsReal) continue;
                    n++;
                    bool friend = Friends.IsFriend(who.id);
                    float yes = Kind == GatheringKind.Party ? (friend ? 0.8f : 0.6f) : (friend ? 0.7f : 0.5f);
                    if (UnityEngine.Random.value < yes) _botsComing.Add((who, Time.realtimeSinceStartup + UnityEngine.Random.Range(5f, 35f)));
                }
            return n;
        }

        /// <summary>The host starts a round (once there are two to play).</summary>
        public static void Start()
        {
            if (!Hosting || !IsContest || Now == Phase.Running) return;
            if (Present() < 2) { Hints.Answer("Wait for someone to arrive: a contest needs at least 2 players."); return; }
            Now = Phase.Running;
            Round++;
            EndsAt = Time.realtimeSinceStartup + ContestSeconds;
            foreach (var p in _players)
            {
                p.score = 0;
                p.isOut = false;
                p.playing = !p.gone;
            }
            foreach (var p in _players) if (p.bot && p.playing) BotPlays(p.id);
            OutAt = 0f;
            Sfx.Play(SfxKind.Chest, 0.8f);
            Hints.Show(UI.Loc.F("Go! Whoever catches the most in 5 minutes wins!"));
            Broadcast();
            Notify();
        }

        /// <summary>How many are here to play (you included).</summary>
        public static int Present()
        {
            int n = 0;
            foreach (var p in _players) if (!p.gone) n++;
            return n;
        }

        /// <summary>The host ends it all: guests head home, the island closes.</summary>
        public static async void EndHosting()
        {
            if (!Hosting) return;
            var state = Snapshot();
            state.over = true;
            Live.SendGathering(JsonUtility.ToJson(state));
            Hosting = false;
            PrivateServer.SetParty("");
            foreach (var bot in new List<VisitorBot>(VisitorBot.All))
                if (bot != null && !bot.Remote && bot.NetId != null && bot.NetId.StartsWith("bot:")) bot.LeaveIn(UnityEngine.Random.Range(0.5f, 3f));
            Clear();
            Notify();
            await System.Threading.Tasks.Task.Delay(400);   // let the news go out first
            await Live.End();
        }

        /// <summary>It's over (the host ended it, the link closed, you flew off): forget it.</summary>
        public static void Ended()
        {
            if (!Hosting && !Joined && !JoiningSomething) return;
            if (Hosting) PrivateServer.SetParty("");
            Hosting = Joined = JoiningSomething = false;
            Clear();
            Notify();
        }

        static void Clear()
        {
            _players.Clear();
            _botsComing.Clear();
            _botSkill.Clear();
            OutAt = 0f;
            Now = Phase.Lobby;
            EventIsland = -1;
            _rejoinAsked = false;
            _rejoinRound = -1;
        }

        // ---------------------------------------------------------------- who's here (host)

        /// <summary>A real player has landed on your island (Live).</summary>
        public static void Arrived(string id, FriendProfile who)
        {
            if (!Hosting) return;
            var p = _players.Find(x => x.id == id);
            if (p == null) _players.Add(p = new Player { id = id, name = who.name });
            p.gone = false;
            p.name = who.name;
            // Mid-round: in, with what's left of the clock.
            if (Now == Phase.Running && !p.playing) { p.playing = true; p.score = 0; p.isOut = false; }
            Broadcast();
            Notify();
        }

        /// <summary>A real player left your island.</summary>
        public static void Gone(string id)
        {
            if (!Hosting) return;
            var p = _players.Find(x => x.id == id);
            if (p == null) return;
            p.gone = true;
            Broadcast();
            Notify();
        }

        /// <summary>
        /// A visitor's contest news: a catch, that they left the contest's area for too long
        /// ("out"), or that they came back and chose to join the round again ("in").
        /// </summary>
        public static void FromGuest(string id, string what)
        {
            if (!Hosting) return;
            var p = _players.Find(x => x.id == id);
            if (p == null) return;
            if (what == "catch" && Now == Phase.Running && p.playing && !p.isOut) p.score++;
            else if (what == "out" && Now == Phase.Running && p.playing) p.isOut = true;
            else if (what == "in" && Now == Phase.Running && p.playing && p.isOut) p.isOut = false;
            else return;
            Broadcast();
            Notify();
        }

        // ---------------------------------------------------------------- invitations (guest)

        /// <summary>An invitation arrived (a Friends message, or a private server's lobby).</summary>
        public static void Invited(FriendProfile host, string json)
        {
            Invitation inv;
            try { inv = JsonUtility.FromJson<Invitation>(json); } catch (Exception) { return; }
            if (inv == null || string.IsNullOrEmpty(inv.code) || host == null) return;
            if (!_invitesSeen.Add(inv.code)) return;   // a friend on your private server sends it twice
            if (DateTime.UtcNow - new DateTime(inv.at, DateTimeKind.Utc) > TimeSpan.FromMinutes(30)) return;
            if (!Enum.TryParse(inv.kind, out GatheringKind kind)) return;
            // A contest for a hobby you haven't got isn't for you; nor anything before you can fly.
            if (kind != GatheringKind.Party && !HobbyLog.IsUnlocked(HobbyOf(kind))) return;
            if (Visit.WhyCantFly() != null) return;
            if (Active || JoiningSomething) return;

            string line = kind == GatheringKind.Party
                ? UI.Loc.F("<b>{0}</b> is having an island party! Come over?", host.name)
                : UI.Loc.F("<b>{0}</b> is hosting a {1}! Join in?", host.name, UI.Loc.T(NameOf(kind)));
            Attention.Ask("invite:" + inv.code, () =>
                UI.PromptScreen.Ask(kind == GatheringKind.Party ? "Party!" : NameOf(kind), host, line, "Join", "No thanks",
                    () => Join(host, kind, inv.code)),
                () => !Active && !JoiningSomething && !Visit.Travelling);
        }

        static void Join(FriendProfile host, GatheringKind kind, string code)
        {
            var why = Visit.WhyCantFly();
            if (why != null) { Hints.Answer(why); return; }
            JoiningSomething = true;
            Kind = kind;
            HostName = host.name;
            _players.Clear();
            Notify();
            Live.Accept(host, code);
        }

        /// <summary>How things stand, from the host.</summary>
        public static void FromHost(string json)
        {
            State s;
            try { s = JsonUtility.FromJson<State>(json); } catch (Exception) { return; }
            if (s == null) return;
            if (s.over)
            {
                bool was = Joined || JoiningSomething;
                string host = HostName;
                Ended();
                if (!was) return;
                _ = Live.End(false);
                Hints.Show(UI.Loc.F("{0} ended the party. Flying you home!", host));
                Visit.FlyHome();
                return;
            }
            bool first = !Joined;
            var before = Now;
            Joined = true;
            JoiningSomething = false;
            if (Enum.TryParse(s.kind, out GatheringKind kind)) Kind = kind;
            HostName = s.host;
            Now = (Phase)s.phase;
            Round = s.round;
            Arena = new Vector2(s.x, s.y);
            EventIsland = s.island1 > 0 ? s.island1 - 1 : IslandFor(Arena);
            EndsAt = Time.realtimeSinceStartup + s.left;
            _players.Clear();
            _players.AddRange(s.players);
            // Just chose to join back in: news the host sent before it heard that mustn't put you
            // straight back out (or drop the catches in between).
            var me = Me;
            if (me != null && me.isOut && Now == Phase.Running && Round == _rejoinRound
                && Time.realtimeSinceStartup - _rejoinedAt < 5f) me.isOut = false;

            if (first && IsContest) StepIntoArea();
            if (Now == Phase.Running && before != Phase.Running)
            {
                OutAt = 0f;
                Sfx.Play(SfxKind.Chest, 0.8f);
                Hints.Show(UI.Loc.F("Go! Whoever catches the most in 5 minutes wins!"));
            }
            if (Now == Phase.Results && before == Phase.Running) Finished?.Invoke(Board);
            Notify();
        }

        /// <summary>
        /// Just landed at a contest: straight over to where it's happening, unless you're already
        /// there. Near where the host started it if there's room, so everyone meets up.
        /// </summary>
        static void StepIntoArea()
        {
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            if (king == null || InArea(king.transform.position)) return;
            var p = SpotInArea(Arena, 5f);
            king.transform.position = new Vector3(p.x, p.y, king.transform.position.z);
        }

        // ---------------------------------------------------------------- the event's area

        /// <summary>
        /// Is this spot part of the gathering's area? The event island for bugs, birds and a party;
        /// for fishing, the island's shore: within <see cref="ShoreBand"/> of the water. A bridge
        /// belongs to no island, so stepping onto one is leaving.
        /// </summary>
        public static bool InArea(Vector2 at) => AreaHas(at, EventIsland, Kind == GatheringKind.Fishing);

        /// <summary>The same test for any island (a bot keeping to it uses this too).</summary>
        public static bool AreaHas(Vector2 at, int island, bool shore)
        {
            var land = LandMap.Instance;
            if (land == null || island < 0) return true;   // nothing to measure against: don't throw anyone out
            if (land.IslandAt(at) != island) return false;
            return !shore || Fishing.WaterNear(at, ShoreBand).HasValue;
        }

        /// <summary>On the event island at all (shore or not): where contest bugs and birds may turn up.</summary>
        public static bool OnEventIsland(Vector2 at)
        {
            var land = LandMap.Instance;
            return land == null || EventIsland < 0 || land.IslandAt(at) == EventIsland;
        }

        /// <summary>A spot in the area near <paramref name="near"/> (Unity units), or anywhere in it if there's none close.</summary>
        public static Vector2 SpotInArea(Vector2 near, float within) =>
            SpotIn(EventIsland, Kind == GatheringKind.Fishing, near, within);

        /// <summary>
        /// A spot on <paramref name="island"/> (on its shore, if <paramref name="shore"/>) near a
        /// point, or anywhere on it when nothing close fits. Shore spots come from just inside the
        /// island's own land circles, where the beach is.
        /// </summary>
        public static Vector2 SpotIn(int island, bool shore, Vector2 near, float within)
        {
            for (int i = 0; i < 30; i++)
            {
                var p = near + UnityEngine.Random.insideUnitCircle * within;
                if (AreaHas(p, island, shore)) return p;
            }
            var land = LandMap.Instance;
            if (land == null || island < 0) return near;
            var piece = land.Island(island);
            if (shore && piece != null && piece.circles.Count > 0)
            {
                for (int i = 0; i < 80; i++)
                {
                    var c = piece.circles[UnityEngine.Random.Range(0, piece.circles.Count)];
                    float ang = UnityEngine.Random.value * Mathf.PI * 2f;
                    var w = new Vector2(c.x, c.y) + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (c.z - UnityEngine.Random.Range(15f, 50f));
                    var u = LandMap.WorldToUnity(w);
                    if (AreaHas(u, island, true)) return u;
                }
            }
            return land.RandomSpotOnIsland(island, 20f);
        }

        /// <summary>Where to head to get back into the area: the nearest water when you've wandered inland, else the meeting point.</summary>
        public static Vector2 WayBack(Vector2 from)
        {
            if (Kind == GatheringKind.Fishing && OnEventIsland(from))
            {
                var water = Fishing.WaterNear(from, 12f);
                if (water.HasValue) return water.Value;
            }
            return MeetingPoint;
        }

        static Vector3 _meetKey = new(float.NaN, 0f, 0f);
        static Vector2 _meet;

        /// <summary>Where the host started it, or (if that's off the area: a bridge) a spot in the area beside it. Worked out once.</summary>
        static Vector2 MeetingPoint
        {
            get
            {
                var key = new Vector3(Arena.x, Arena.y, EventIsland * 10 + (int)Kind);
                if (key == _meetKey) return _meet;
                _meetKey = key;
                return _meet = InArea(Arena) ? Arena : SpotInArea(Arena, 5f);
            }
        }

        // ---------------------------------------------------------------- catching

        /// <summary>The hobby log saw a catch (GameEvents.Record): it counts if you're in the round.</summary>
        public static void Caught(Hobby hobby)
        {
            if (!Active || !IsContest || Now != Phase.Running || HobbyOf(Kind) != hobby) return;
            var me = Me;
            if (me == null || !me.playing || me.isOut) return;
            if (Hosting)
            {
                me.score++;
                Broadcast();
            }
            else
            {
                me.score++;   // shown at once; the host's count follows
                Live.TellHost("catch");
            }
            Notify();
        }

        /// <summary>
        /// Should hobbies be generous here, now? (A running contest of that hobby, and you in its
        /// area.) The fish, bugs and birds then turn up around you, wherever you are on it.
        /// </summary>
        public static bool Boosting(Hobby hobby)
        {
            if (!Active || !IsContest || Now != Phase.Running || HobbyOf(Kind) != hobby) return false;
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            return king != null && InArea(king.transform.position);
        }

        // ---------------------------------------------------------------- every frame (LiveRunner)

        internal static void Tick()
        {
            if (!Active || EventIsland < 0) EventGlow.Clear();
            if (!Active) return;
            if (EventIsland >= 0 && !Visit.Travelling) EventGlow.Ensure(EventIsland, Kind, Now == Phase.Running);
            WatchMyArea();
            if (!Hosting) return;

            // Islanders turning up.
            for (int i = _botsComing.Count - 1; i >= 0; i--)
            {
                if (Time.realtimeSinceStartup < _botsComing[i].at) continue;
                var who = _botsComing[i].who;
                _botsComing.RemoveAt(i);
                BotArrives(who);
            }
            // Islanders who went home.
            foreach (var p in _players)
            {
                if (!p.bot || p.gone) continue;
                var bot = VisitorBot.ByNetId(p.id);
                if (bot != null && !bot.Leaving) continue;
                p.gone = true;
                if (bot != null) Live.BotGone(bot);
                Broadcast();
                Notify();
            }

            if (Now != Phase.Running) return;
            if (Time.realtimeSinceStartup >= EndsAt) { Finish(); return; }
            if (Time.realtimeSinceStartup >= _nextBroadcast) Broadcast();   // keeps everyone's clock right
        }

        static void BotArrives(FriendProfile who)
        {
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            Vector2 at = Arena;
            if (IsContest)
            {
                // Somewhere in the contest's area (a fishing contest: on the shore), near the
                // host's starting point if there's room, so they're easy to spot arriving.
                at = SpotInArea(Arena, 6f);
            }
            else
            {
                // A party guest pops up beside you.
                Vector2 centre = king != null ? (Vector2)king.transform.position : Arena;
                at = centre;
                for (int i = 0; i < 30; i++)
                {
                    var p = BunniSpace.RandomPointInCircle(centre, 90f);
                    if (BunniSpace.PointOnLand(p)) { at = p; break; }
                }
            }
            var bot = VisitorBot.Spawn(who, VisitorBot.Role.Guest, at);
            bot.NetId = "bot:" + who.id;
            bot.QuietExit = true;
            if (IsContest)
            {
                bot.KeepTo(EventIsland, Kind == GatheringKind.Fishing);
                // Here for the round: through to its end once it's on, or a good while in the lobby
                // waiting for it to start (nobody waits for ever).
                if (Now == Phase.Running) bot.Linger();
                else bot.LeaveIn(UnityEngine.Random.Range(LobbyWait.x, LobbyWait.y));
            }
            else bot.LeaveIn(UnityEngine.Random.Range(240f, 540f));   // a party guest stays a good while, then heads off
            Live.AnnounceBot(bot);
            var p2 = _players.Find(x => x.id == bot.NetId);
            if (p2 == null) _players.Add(p2 = new Player { id = bot.NetId, name = who.name, bot = true });
            p2.gone = false;
            if (Now == Phase.Running) { p2.playing = true; p2.score = 0; p2.isOut = false; BotPlays(p2.id); }
            Sfx.Play(SfxKind.Bridge, 0.5f);
            Hints.Show(UI.Loc.F("{0} has come to your {1}!", who.name, UI.Loc.T(Kind == GatheringKind.Party ? "party" : "contest")));
            Broadcast();
            Notify();
        }

        /// <summary>Seconds between an average islander's catches, by contest.</summary>
        static float BotPace => Kind switch
        {
            GatheringKind.Bugs => 13f,
            GatheringKind.Birds => 17f,
            _ => 28f,
        };

        /// <summary>
        /// An islander starts on the round: off to the water's edge, a flower or a tree, really
        /// fishing, netting or looking (BotHobby), and it scores as each catch is held up, where
        /// everyone can see it. Some are better at it than others.
        /// </summary>
        /// <summary>How long (seconds) an islander waits in the lobby for a round, and stays on after one.</summary>
        static readonly Vector2 LobbyWait = new(300f, 480f), AfterRound = new(90f, 200f);

        static void BotPlays(string id)
        {
            var bot = VisitorBot.ByNetId(id);
            if (bot == null || bot.Pastime == null) return;
            bot.Linger();   // through to the end of the round
            if (!_botSkill.TryGetValue(id, out var skill)) _botSkill[id] = skill = UnityEngine.Random.Range(0.7f, 1.35f);
            bot.Pastime.Pursue(HobbyOf(Kind), BotPace / skill, () => BotCaught(id), EventIsland);
        }

        static void BotCaught(string id)
        {
            if (!Hosting || Now != Phase.Running) return;
            var p = _players.Find(x => x.id == id);
            if (p == null || !p.playing || p.gone || p.isOut) return;
            p.score++;
            Broadcast();
            Notify();
        }

        static void Finish()
        {
            Now = Phase.Results;
            OutAt = 0f;
            // The islanders put their rods and nets away and potter about a little; start another
            // round soon and they'll play it, or by and by they head home on their own (islanders
            // are company for a contest, not helpers to keep for ever).
            foreach (var p in _players)
            {
                if (!p.bot || !(VisitorBot.ByNetId(p.id) is { } bot)) continue;
                if (bot.Pastime != null) bot.Pastime.Stop();
                bot.LeaveIn(UnityEngine.Random.Range(AfterRound.x, AfterRound.y));
            }
            Broadcast();
            Sfx.Play(SfxKind.Chest, 0.9f);
            Finished?.Invoke(Board);
            Notify();
        }

        /// <summary>
        /// Your own area check: cross a bridge off the island (or, fishing, walk inland from the
        /// shore) and the clock's on; ten seconds and you're out. Once out, coming back into the
        /// area while the round runs offers a way back in. A party has no such rule.
        /// </summary>
        static void WatchMyArea()
        {
            if (!IsContest || Now != Phase.Running || Visit.Travelling) { OutAt = 0f; return; }
            var me = Me;
            if (me == null || !me.playing || me.gone) { OutAt = 0f; return; }
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            if (king == null) return;
            bool inside = InArea(king.transform.position);
            if (me.isOut)
            {
                OutAt = 0f;
                OfferRejoin(inside);
                return;
            }
            if (inside)
            {
                if (OutAt > 0f) { OutAt = 0f; Notify(); }
                return;
            }
            if (OutAt <= 0f)
            {
                OutAt = Time.realtimeSinceStartup + OutGrace;
                Sfx.Play(SfxKind.Tap, 0.6f);
                Notify();
                return;
            }
            if (Time.realtimeSinceStartup < OutAt) return;
            OutAt = 0f;
            me.isOut = true;
            _rejoinAsked = false;
            Hints.Show(Kind == GatheringKind.Fishing
                ? UI.Loc.F("You left the shore of {0}, so you're out of this round for now. Come back to join in again!", UI.Loc.T(AreaName))
                : UI.Loc.F("You left {0}, so you're out of this round for now. Come back to join in again!", UI.Loc.T(AreaName)));
            if (Hosting) Broadcast();
            else Live.TellHost("out");
            Notify();
        }

        // ---------------------------------------------------------------- back in after being out

        const string RejoinKey = "contest:rejoin";
        static bool _rejoinAsked;      // asked (or answered) since you last came back into the area
        static bool _backInArea;       // out of the round, and standing in its area right now
        static int _rejoinRound = -1;  // the round you last joined back into, and when
        static float _rejoinedAt = -100f;

        /// <summary>
        /// Out of the round but back in its area while the clock still runs: asks, once, whether
        /// to join in again. "Not now" holds until you leave the area and come back once more.
        /// The question waits its turn behind anything else on screen (Attention).
        /// </summary>
        static void OfferRejoin(bool inside)
        {
            _backInArea = inside;
            if (!inside) { _rejoinAsked = false; return; }
            if (_rejoinAsked) return;
            _rejoinAsked = true;
            int round = Round;
            string title = NameOf(Kind);
            Attention.Ask(RejoinKey, () =>
                    UI.PromptScreen.Ask(title, null,
                        UI.Loc.F("You're back at the {0}! Join this round again? What you caught before still counts.", UI.Loc.T(title)),
                        "Join in", "Not now", () => Rejoin(round), null, null, UI.GatheringPanel.IconOf(Kind)),
                () => _backInArea && _rejoinAsked && StillOut(round), calm: 0.5f);
        }

        static bool StillOut(int round)
        {
            var me = Me;
            return Active && IsContest && Now == Phase.Running && Round == round && me != null && me.playing && me.isOut;
        }

        /// <summary>
        /// The gathering panel's "Join in" while you're out of the round: back in if you're in the
        /// area, else a nudge to come back to it first.
        /// </summary>
        public static void TryRejoin()
        {
            if (!StillOut(Round)) return;
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            if (king != null && !InArea(king.transform.position))
            {
                Hints.Show(Kind == GatheringKind.Fishing
                    ? UI.Loc.F("Come back to the shore of {0} first, then join in again!", UI.Loc.T(AreaName))
                    : UI.Loc.F("Come back to {0} first, then join in again!", UI.Loc.T(AreaName)));
                return;
            }
            Rejoin(Round);
        }

        /// <summary>
        /// In again, with the score you had (catches while you were out didn't count). The host
        /// tells everyone; a visitor tells the host ("in"), and meanwhile counts itself in.
        /// </summary>
        static void Rejoin(int round)
        {
            if (!StillOut(round)) return;   // the round ended while you were deciding
            var me = Me;
            me.isOut = false;
            OutAt = 0f;
            _rejoinRound = round;
            _rejoinedAt = Time.realtimeSinceStartup;
            Sfx.Play(SfxKind.Chest, 0.5f);
            Hints.Answer("You're back in this round. Good luck!");
            if (Hosting) Broadcast();
            else Live.TellHost("in");
            Notify();
        }

        // ---------------------------------------------------------------- telling everyone

        static State Snapshot() => new()
        {
            kind = Kind.ToString(), host = HostName, phase = (int)Now, round = Round, x = Arena.x, y = Arena.y,
            island1 = EventIsland + 1, left = SecondsLeft, players = new List<Player>(_players),
        };

        static void Broadcast()
        {
            if (!Hosting) return;
            _nextBroadcast = Time.realtimeSinceStartup + 5f;
            Live.SendGathering(JsonUtility.ToJson(Snapshot()));
        }
    }
}
