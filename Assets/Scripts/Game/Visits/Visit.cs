using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CapyIsland.Game
{
    /// <summary>Where something in your trail came from, while you are on someone else's island.</summary>
    public class VisitTag : MonoBehaviour
    {
        public enum Origin { Home, Bought, Found }

        public Origin From;
        public Vector2 FoundAt;

        /// <summary>Tags a thing once: its first origin is the one that counts.</summary>
        public static void Mark(GameObject go, Origin from, Vector2 foundAt = default)
        {
            if (go == null || go.GetComponent<VisitTag>() != null) return;
            var tag = go.AddComponent<VisitTag>();
            tag.From = from;
            tag.FoundAt = foundAt;
        }
    }

    /// <summary>
    /// Flying to someone else's island and back. There is one island scene: a trip saves the island
    /// you are leaving, reloads the scene and builds the next one from a save — your own, or the
    /// friend's world with your progress and your trail laid over it.
    ///
    /// What travels with you follows a simple rule. Anything you brought comes home if you are
    /// still carrying it. Anything you found there (a bubble you popped) stays: if it is still in
    /// your trail when you leave, it goes back into a bubble where you found it. Anything you
    /// bought there is a gift for their island: unused, it waits in a bubble where you stood.
    /// Anything you planted on their island is theirs to keep. Bombs and story things never go
    /// down there at all (StoreData.StaysHome).
    /// </summary>
    public static class Visit
    {
        const string VisitedKey = "CapyIsland.Visited";

        /// <summary>Whose island you are on, or null at home.</summary>
        public static FriendProfile Host { get; private set; }

        public static bool Active => Host != null;

        /// <summary>A friend's island is kept between visits; a stranger's is forgotten.</summary>
        public static bool HostIsFriend => Active && Friends.IsFriend(Host.id);

        /// <summary>The world to build when the island scene loads on a visit.</summary>
        public static SaveData HostWorld { get; private set; }

        static SaveData _realWorld;   // a real friend's island, fetched before take-off

        /// <summary>Set for the first load after flying home.</summary>
        public static bool ReturnedHome { get; set; }

        /// <summary>True from take-off until the new island is revealed.</summary>
        public static bool Travelling { get; private set; }

        public static int IslandsVisited => PlayerPrefs.GetString(VisitedKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries).Length;

        // ---------------------------------------------------------------- flying

        /// <summary>Flies to someone's island (or home, for null), with the cloud transition.</summary>
        public static async void Fly(FriendProfile to)
        {
            if (Travelling) return;
            if (to == null && !Active) return;
            var why = WhyCantFly();
            if (why != null) { Hints.Answer(why); Live.FlightFailed(to); return; }
            // A real person's island: only with their yes. Knock, and fly once they let you in
            // (Live flies you then); never just because the button was pressed again.
            if (to != null && to.IsReal && !Live.MayLand(to))
            {
                if (Live.ShouldKnock(to)) { Live.Knock(to); return; }
                var notNow = Live.WhyNotKnock(to);
                if (notNow != null) Hints.Answer(notNow);
                return;
            }
            Travelling = true;
            if (to != null && to.IsReal)
            {
                // A real friend's island is fetched as they last saved it.
                _realWorld = await RealFriends.FetchIsland(to);
                if (_realWorld == null) { Travelling = false; Live.FlightFailed(to); return; }
            }
            string label = to != null ? "Flying to " + to.name + "'s island" : "Flying home";
            UI.FlightTransition.Play(label, () => Swap(to), () => Travelling = false);
        }

        public static void FlyHome() => Fly(null);

        /// <summary>Why you can't take off from home right now, or null if you can. (Flying home is always allowed.)</summary>
        public static string WhyCantFly()
        {
            if (Active) return null;
            if (Gathering.Hosting) return "You're hosting right now. End it first, then you can fly off.";
            var tutorial = TutorialDirector.Instance;
            if (tutorial != null && !tutorial.Finished) return "Let's get your own island settled first. Then you can fly anywhere you like!";
            if (!Airport.Built) return "You'll need an airstrip before you can fly. Build one above!";
            if (!Airport.Placed) return "Put your airstrip down somewhere first. Pop its bubble and drag it to a clear spot!";
            return null;
        }

        /// <summary>Behind the clouds: put the old island away and load the new one.</summary>
        static void Swap(FriendProfile to)
        {
            // Leaving where a live visit happens ends it: the host's island, or home while hosting.
            // What you found on their island goes back into bubbles there, on their real island too.
            if (Live.Now == Live.Mode.Visiting && Active)
            {
                SortTrail(out _, out _, out var found);
                Live.GiveBackFound(found);
            }
            if ((Live.Now == Live.Mode.Visiting && Active) || Live.Now == Live.Mode.Hosting) _ = Live.End();
            CoBuild.Reset();
            if (Active) LeaveHost();
            else if (SaveGame.Instance != null) SaveGame.Instance.SaveHomeNow();

            Host = null;
            HostWorld = null;
            if (to != null) Prepare(to);
            else ReturnedHome = true;

            SaveGame.Suspended = true;
            SaveGame.ResetStatics();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Builds or brings back the island to land on.</summary>
        static void Prepare(FriendProfile to)
        {
            var friend = Friends.Find(to.id);
            if (friend == null && to.IsReal)
            {
                // A real person who isn't a friend (someone from your private server who invited
                // you): their island as they saved it, and nothing of it kept here.
                Host = to;
                HostWorld = _realWorld;
                _realWorld = null;
                return;
            }
            if (friend == null)
            {
                Host = to;
                HostWorld = IslandGenerator.Generate(to);
                return;
            }

            Host = friend;
            if (friend.IsReal)
            {
                // Theirs, as they left it: nothing to grow, and nothing of it kept here.
                friend.visits++;
                friend.lastVisitTicks = DateTime.Now.Ticks;
                Friends.Save();
                HostWorld = _realWorld;
                _realWorld = null;
                return;
            }
            SaveData world = null;
            if (!string.IsNullOrEmpty(friend.world))
            {
                try { world = JsonUtility.FromJson<SaveData>(friend.world); }
                catch (Exception e) { Debug.LogWarning("[Visit] friend's island unreadable, making a new one: " + e.Message); }
            }
            if (world == null) world = IslandGenerator.Generate(friend);
            else IslandGenerator.Progress(friend, world, (DateTime.Now - friend.LastVisit).TotalHours);

            friend.visits++;
            friend.lastVisitTicks = DateTime.Now.Ticks;
            friend.world = JsonUtility.ToJson(world);
            Friends.Save();
            HostWorld = world;
        }

        /// <summary>The island has been built: count it towards Good Neighbour.</summary>
        public static void OnArrived()
        {
            if (!Active) return;
            var seen = new HashSet<string>(PlayerPrefs.GetString(VisitedKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries));
            if (seen.Add(Host.name))
            {
                PlayerPrefs.SetString(VisitedKey, string.Join(",", seen));
                PlayerPrefs.Save();
            }
            if (seen.Count >= 3) Motes.Trigger("visit_three");
        }

        /// <summary>Makes a stranger you are visiting a friend — their island will be kept from now on.</summary>
        public static void BefriendHost()
        {
            if (!Active || HostIsFriend) return;
            Host.lastVisitTicks = DateTime.Now.Ticks;
            Host.visits = Math.Max(1, Host.visits);
            Friends.Add(Host);
            StoreHostWorld(SaveGame.CaptureWorld());
        }

        // ---------------------------------------------------------------- saving their island

        /// <summary>The autosave while visiting: keeps a friend's island as it stands, minus your trail.</summary>
        public static void StoreHostWorld(SaveData world)
        {
            if (!HostIsFriend || Host.IsReal) return;
            SortTrail(out _, out _, out var found);
            world.carried.Clear();
            world.animals.RemoveAll(a => a.carried);
            world.bubbles.AddRange(found);
            Host.world = JsonUtility.ToJson(world);
            Friends.Save();
        }

        /// <summary>Leaving: their island keeps what is theirs, and your save gets your trail and wallet back.</summary>
        static void LeaveHost()
        {
            var world = SaveGame.CaptureWorld();
            SortTrail(out var carryHome, out var animalsHome, out var found);
            world.carried.Clear();
            world.animals.RemoveAll(a => a.carried);
            world.bubbles.AddRange(found);

            if (HostIsFriend && !Host.IsReal)
            {
                Host.world = JsonUtility.ToJson(world);
                Host.lastVisitTicks = DateTime.Now.Ticks;
                Friends.Save();
            }

            var home = SaveGame.ReadHome() ?? new SaveData();
            SaveGame.CaptureProgress(home);
            home.carried = carryHome;
            home.animals.RemoveAll(a => a.carried);
            foreach (var a in animalsHome) a.pos = home.king;   // they land beside you
            home.animals.AddRange(animalsHome);
            SaveGame.WriteHome(home);
        }

        /// <summary>
        /// Splits your trail: what goes home with you (brought or bought), and what stays here as a
        /// bubble where you found it.
        /// </summary>
        static void SortTrail(out List<string> carryHome, out List<AnimalSave> animalsHome, out List<BubbleSave> found)
        {
            carryHome = new List<string>();
            animalsHome = new List<AnimalSave>();
            found = new List<BubbleSave>();
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            Vector2 here = king != null ? (Vector2)king.transform.position : Vector2.zero;

            foreach (var tag in UnityEngine.Object.FindObjectsByType<CatalogItem>(FindObjectsSortMode.None))
            {
                var placeable = tag.GetComponent<Placeable>();
                var worker = tag.GetComponent<Worker>();
                bool inTrail = placeable != null
                    ? !placeable.Placed && tag.GetComponent<Follower>() is { Carried: true }
                    : worker != null && worker.Workplace == null && !(tag.TryGetComponent<MonsterAnimal>(out var m) && !m.Tame);
                if (!inTrail) continue;

                var origin = tag.GetComponent<VisitTag>();
                // Bought here: it's for their island, and waits there in a bubble as a gift (no
                // second copies of one-of-a-kind buildings carried home, either).
                bool yours = origin != null && origin.From == VisitTag.Origin.Home;
                if (yours)
                {
                    if (worker != null) animalsHome.Add(new AnimalSave { name = tag.StoreName, tame = true, carried = true, hunger = worker.Hunger });
                    else carryHome.Add(tag.StoreName);
                }
                else
                {
                    bool foundHere = origin != null && origin.From == VisitTag.Origin.Found;
                    found.Add(new BubbleSave { name = tag.StoreName, pos = foundHere ? origin.FoundAt : here });
                }
            }
        }
    }
}
