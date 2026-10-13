using CapyIsland.Game;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// Runs the social side of the island, whichever island you are on.
    ///
    /// On a friend's island: puts your host on the beach to greet you, tags what you pick up and
    /// buy (so the right things come home), and hands out the helping achievements.
    ///
    /// At home: every so often someone asks to come over. Say yes and they land beside you, lend a
    /// hand for a few minutes, and log off — and if they are not a friend yet, you can add them.
    /// </summary>
    public class VisitDirector : MonoBehaviour
    {
        float _nextRequest;
        bool _hostSpawned;

        void OnEnable()
        {
            Bubble.Collected += OnBubble;
            Purchasing.Bought += OnBought;
            Worker.Fed += OnFed;
            Hazards.PestCleared += OnPest;
            VisitorBot.GuestLeft += OnGuestLeft;
        }

        void OnDisable()
        {
            Bubble.Collected -= OnBubble;
            Purchasing.Bought -= OnBought;
            Worker.Fed -= OnFed;
            Hazards.PestCleared -= OnPest;
            VisitorBot.GuestLeft -= OnGuestLeft;
        }

        void Start()
        {
            // The first knock comes after a few minutes of play, then every so often.
            _nextRequest = Time.unscaledTime + Random.Range(150f, 260f);
        }

        void Update()
        {
            if (Visit.Active)
            {
                if (!_hostSpawned) SpawnHost();
                return;
            }
            if (Time.unscaledTime >= _nextRequest) MaybeKnock();
        }

        // ---------------------------------------------------------------- on their island

        void SpawnHost()
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king == null || Visit.Travelling) return;
            _hostSpawned = true;
            // An offline friend's island is still there to visit, but nobody is home.
            if (!Server.IsOnline(Visit.Host))
            {
                Hints.Show(Visit.Host.name + " is offline right now, but you can still look around and lend a hand.");
                return;
            }
            Vector2 at = (Vector2)king.transform.position + new Vector2(2.4f, 0.6f);
            if (!BunniSpace.PointOnLand(at)) at = king.transform.position;
            VisitorBot.Spawn(Visit.Host, VisitorBot.Role.Host, at);
            Hints.Show(Visit.Host.name + " is waving at you! Lend a hand: feed hungry workers and chase off pests.");
        }

        static void OnBubble(GameObject item, Bubble from)
        {
            if (Visit.Active && item != null) VisitTag.Mark(item, VisitTag.Origin.Found, from.transform.position);
        }

        static void OnBought(string name, GameObject item)
        {
            if (Visit.Active && item != null) VisitTag.Mark(item, VisitTag.Origin.Bought);
        }

        static void OnFed(Worker worker)
        {
            if (!Visit.Active || worker == null || worker.GetComponent<VisitTag>() != null) return;
            // Only your flowers count: a garden you planted here, within the worker's reach.
            bool yours = false;
            foreach (var p in FindObjectsByType<Placeable>(FindObjectsSortMode.None))
            {
                if (!p.Placed || p.GetComponent<VisitTag>() == null) continue;
                if (!p.TryGetComponent<Harvestable>(out var node) || node.Family != NodeFamily.Flower) continue;
                if (BunniSpace.WorldDistance(p.transform.position, worker.transform.position) < BunniRules.WorkplaceBoundary * 1.5f) { yours = true; break; }
            }
            if (!yours) return;
            Motes.Trigger("visit_feed");
            if (VisitorBot.Current != null) VisitorBot.Current.Emote(Emoji.Heart);
        }

        static void OnPest(Vector2 at)
        {
            if (!Visit.Active) return;
            Motes.Trigger("visit_pests");
            if (VisitorBot.Current != null) VisitorBot.Current.Emote(Random.value < 0.5f ? Emoji.Laugh : Emoji.Star);
        }

        // ---------------------------------------------------------------- at home

        void MaybeKnock()
        {
            _nextRequest = Time.unscaledTime + Random.Range(360f, 720f);
            if (VisitorBot.Current != null || !Attention.Free(8f)) return;
            if (Random.value > 0.6f) return;

            // Someone online on this server knocks; an online friend more often than a stranger.
            var neighbours = Server.Neighbours();
            if (neighbours.Count == 0) return;
            var online = neighbours.FindAll(n => Friends.IsFriend(n.id));
            var who = online.Count > 0 && Random.value < 0.65f
                ? online[Random.Range(0, online.Count)]
                : neighbours[Random.Range(0, neighbours.Count)];

            PromptScreen.Ask("Visitor!", who, $"<b>{who.name}</b> wants to visit your island!",
                "Let them in", "Not now", () => Welcome(who));
        }

        static void Welcome(FriendProfile who)
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king == null) return;
            Vector2 at = (Vector2)king.transform.position + new Vector2(-2.4f, 0.8f);
            if (!BunniSpace.PointOnLand(at)) at = king.transform.position;
            VisitorBot.Spawn(who, VisitorBot.Role.Guest, at);
            Sfx.Play(SfxKind.Bridge, 0.6f);
            Hints.Show(who.name + " has landed on your island!");
        }

        static void OnGuestLeft(FriendProfile who)
        {
            if (Visit.Active) return;
            if (Friends.IsFriend(who.id))
            {
                Hints.Show(who.name + " has logged off. See you next time!");
                return;
            }
            // Asked when the screen is free, not on top of whatever you're doing when they go.
            Attention.Ask("logged-off:" + who.id, () =>
                PromptScreen.Ask("Logged off", who, $"<b>{who.name}</b> had to go. Add them as a friend so you can visit their island?",
                    "Add friend", "No thanks", () =>
                    {
                        Friends.Add(who);
                        Hints.Show(who.name + " is now your friend. Visit them from the Friends menu!");
                    }),
                () => !Visit.Active && !Friends.IsFriend(who.id));
        }
    }
}
