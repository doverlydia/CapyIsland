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
    /// <b>A contest</b> happens where you stand when you host it (a shore for fishing, among trees
    /// for birds), inside a ring marked with glitter (ContestArena), where fish, bugs and birds turn
    /// up far more often while it runs. Guests land there. When at least two are present the host
    /// presses Start: five minutes, and whoever catches the most wins. Late arrivals join in with
    /// what's left of the clock. Step out of the ring and you have ten seconds to come back, or
    /// you're out of that round. Everyone keeps what they catch (the catches are real ones: they go
    /// in the journal and count for the day's goal and the week's contest). Then the leaderboard,
    /// and the host can run another round or end it.
    ///
    /// The host's game keeps the clock and the scores and tells everyone (Live, "gs" notes);
    /// visitors tell it their catches. Only hobbies you have unlocked can be hosted, and a contest
    /// invitation only reaches people who have that hobby too.
    /// </summary>
    public static class Gathering
    {
        public const float ContestSeconds = 300f, OutGrace = 10f;

        /// <summary>The contest ring's radius, in world units (about six steps across on screen each way).</summary>
        public const float ArenaRadius = 150f;

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
        public static Vector2 Arena { get; private set; }
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

        /// <summary>When the out-of-the-ring countdown runs out (realtime), or 0 while you're inside.</summary>
        public static float OutAt { get; private set; }

        /// <summary>Anything about the gathering changed (the panel redraws).</summary>
        public static event Action Changed;

        /// <summary>A round just finished: the leaderboard, for the results card.</summary>
        public static event Action<List<Player>> Finished;

        static readonly List<Player> _players = new();
        static readonly List<(FriendProfile who, float at)> _botsComing = new();
        static readonly Dictionary<string, float> _botNextCatch = new();
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
            _botNextCatch.Clear();
            _botSkill.Clear();
            _invitesSeen.Clear();
            Changed = null;
            Finished = null;
            OutAt = 0f;
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
            switch (kind)
            {
                case GatheringKind.Fishing:
                    if (!Fishing.WaterNear(at, 2.6f).HasValue) return "Stand at the water's edge to host a fishing contest there.";
                    break;
                case GatheringKind.Birds:
                    if (TreesNear(at) < 2) return "Stand among some trees to host a birdwatching contest there.";
                    break;
                case GatheringKind.Bugs:
                    if (TreesNear(at) + FlowersNear(at) < 2) return "Stand near trees or flowers to host a bug contest there.";
                    break;
            }
            return null;
        }

        static int TreesNear(Vector2 at) => CountNodes(at, NodeFamily.Tree);
        static int FlowersNear(Vector2 at) => CountNodes(at, NodeFamily.Flower);

        static int CountNodes(Vector2 at, NodeFamily family)
        {
            int n = 0;
            foreach (var node in UnityEngine.Object.FindObjectsByType<Harvestable>(FindObjectsSortMode.None))
                if (node.Family == family && node.InWorld && BunniSpace.WorldDistance(node.transform.position, at) < ArenaRadius) n++;
            return n;
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
            if (why != null) { Hints.Show(why); return; }
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            Hosting = true;
            Kind = kind;
            Now = Phase.Lobby;
            Round = 0;
            Arena = king != null ? (Vector2)king.transform.position : Vector2.zero;
            HostName = PlayerProfile.Name;
            _players.Clear();
            _players.Add(new Player { id = Live.MyId, name = PlayerProfile.Name });
            _botsComing.Clear();
            Notify();
            Hints.Show("Opening your island…");

            string code = await Live.OpenIsland();
            if (!Hosting) return;   // ended meanwhile
            if (code == null)
            {
                Hosting = false;
                _players.Clear();
                Notify();
                Hints.Show("Couldn't open your island just now. Try again in a moment.");
                return;
            }
            _code = code;
            int asked = SendInvitations();
            Sfx.Play(SfxKind.Chest, 0.6f);
            Hints.Show(kind == GatheringKind.Party
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
            if (Present() < 2) { Hints.Show("Wait for someone to arrive: a contest needs at least 2 players."); return; }
            Now = Phase.Running;
            Round++;
            EndsAt = Time.realtimeSinceStartup + ContestSeconds;
            foreach (var p in _players)
            {
                p.score = 0;
                p.isOut = false;
                p.playing = !p.gone;
            }
            foreach (var p in _players) if (p.bot && p.playing) NextBotCatch(p.id, first: true);
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
            _botNextCatch.Clear();
            _botSkill.Clear();
            OutAt = 0f;
            Now = Phase.Lobby;
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

        /// <summary>A visitor's contest news: a catch, or that they left the ring for too long.</summary>
        public static void FromGuest(string id, string what)
        {
            if (!Hosting) return;
            var p = _players.Find(x => x.id == id);
            if (p == null) return;
            if (what == "catch" && Now == Phase.Running && p.playing && !p.isOut) p.score++;
            else if (what == "out" && Now == Phase.Running && p.playing) p.isOut = true;
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
            if (why != null) { Hints.Show(why); return; }
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
            EndsAt = Time.realtimeSinceStartup + s.left;
            _players.Clear();
            _players.AddRange(s.players);

            if (first && IsContest) StepIntoRing();
            if (Now == Phase.Running && before != Phase.Running)
            {
                OutAt = 0f;
                Sfx.Play(SfxKind.Chest, 0.8f);
                Hints.Show(UI.Loc.F("Go! Whoever catches the most in 5 minutes wins!"));
            }
            if (Now == Phase.Results && before == Phase.Running) Finished?.Invoke(Board);
            Notify();
        }

        /// <summary>Just landed at a contest: straight over to where it's happening.</summary>
        static void StepIntoRing()
        {
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            if (king == null || BunniSpace.WorldDistance(king.transform.position, Arena) < ArenaRadius * 0.7f) return;
            for (int i = 0; i < 30; i++)
            {
                var p = BunniSpace.RandomPointInCircle(Arena, ArenaRadius * 0.5f);
                if (!BunniSpace.PointOnLand(p)) continue;
                king.transform.position = new Vector3(p.x, p.y, king.transform.position.z);
                return;
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

        /// <summary>Should hobbies be generous here, now? (A running contest of that hobby, and you inside the ring.)</summary>
        public static bool Boosting(Hobby hobby)
        {
            if (!Active || !IsContest || Now != Phase.Running || HobbyOf(Kind) != hobby) return false;
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            return king != null && InRing(king.transform.position);
        }

        public static bool InRing(Vector2 at) => BunniSpace.WorldDistance(at, Arena) <= ArenaRadius;

        // ---------------------------------------------------------------- every frame (LiveRunner)

        internal static void Tick()
        {
            if (!Active || !IsContest) ContestArena.Clear();
            if (!Active) return;
            if (IsContest && !Visit.Travelling) ContestArena.Ensure(Arena, ArenaRadius, Kind, Now == Phase.Running);
            WatchMyRing();
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
            BotCatches();
            if (Time.realtimeSinceStartup >= EndsAt) { Finish(); return; }
            if (Time.realtimeSinceStartup >= _nextBroadcast) Broadcast();   // keeps everyone's clock right
        }

        static void BotArrives(FriendProfile who)
        {
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            Vector2 centre = IsContest ? Arena : king != null ? (Vector2)king.transform.position : Arena;
            Vector2 at = centre;
            for (int i = 0; i < 30; i++)
            {
                var p = BunniSpace.RandomPointInCircle(centre, IsContest ? ArenaRadius * 0.6f : 90f);
                if (BunniSpace.PointOnLand(p)) { at = p; break; }
            }
            var bot = VisitorBot.Spawn(who, VisitorBot.Role.Guest, at);
            bot.NetId = "bot:" + who.id;
            bot.QuietExit = true;
            bot.Linger();
            if (IsContest) bot.KeepTo(Arena, ArenaRadius);
            else bot.LeaveIn(UnityEngine.Random.Range(240f, 540f));   // a party guest stays a good while, then heads off
            Live.AnnounceBot(bot);
            var p2 = _players.Find(x => x.id == bot.NetId);
            if (p2 == null) _players.Add(p2 = new Player { id = bot.NetId, name = who.name, bot = true });
            p2.gone = false;
            if (Now == Phase.Running) { p2.playing = true; p2.score = 0; p2.isOut = false; NextBotCatch(p2.id, first: true); }
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

        static void NextBotCatch(string id, bool first)
        {
            if (!_botSkill.TryGetValue(id, out var skill)) _botSkill[id] = skill = UnityEngine.Random.Range(0.7f, 1.35f);
            float wait = BotPace / skill * UnityEngine.Random.Range(0.6f, 1.5f);
            if (first) wait += UnityEngine.Random.Range(3f, 10f);   // getting going
            _botNextCatch[id] = Time.realtimeSinceStartup + wait;
        }

        static void BotCatches()
        {
            foreach (var p in _players)
            {
                if (!p.bot || !p.playing || p.gone || p.isOut) continue;
                if (!_botNextCatch.TryGetValue(p.id, out var at) || Time.realtimeSinceStartup < at) continue;
                p.score++;
                NextBotCatch(p.id, first: false);
                var bot = VisitorBot.ByNetId(p.id);
                if (bot != null) bot.Emote(UnityEngine.Random.value < 0.5f ? Emoji.Star : Emoji.Smile);
                Broadcast();
                Notify();
            }
        }

        static void Finish()
        {
            Now = Phase.Results;
            OutAt = 0f;
            Broadcast();
            Sfx.Play(SfxKind.Chest, 0.9f);
            Finished?.Invoke(Board);
            Notify();
        }

        /// <summary>Your own ring check: step out and the clock's on; ten seconds and you're out.</summary>
        static void WatchMyRing()
        {
            if (!IsContest || Now != Phase.Running || Visit.Travelling) { OutAt = 0f; return; }
            var me = Me;
            if (me == null || !me.playing || me.isOut || me.gone) { OutAt = 0f; return; }
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            if (king == null) return;
            if (InRing(king.transform.position))
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
            Hints.Show("You left the contest area, so you're out of this round. You keep everything you caught!");
            if (Hosting) Broadcast();
            else Live.TellHost("out");
            Notify();
        }

        // ---------------------------------------------------------------- telling everyone

        static State Snapshot() => new()
        {
            kind = Kind.ToString(), host = HostName, phase = (int)Now, round = Round, x = Arena.x, y = Arena.y,
            left = SecondsLeft, players = new List<Player>(_players),
        };

        static void Broadcast()
        {
            if (!Hosting) return;
            _nextBroadcast = Time.realtimeSinceStartup + 5f;
            Live.SendGathering(JsonUtility.ToJson(Snapshot()));
        }
    }
}
