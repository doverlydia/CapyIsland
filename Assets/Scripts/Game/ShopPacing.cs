using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Some wares wait on the shelf for their moment, so nothing is for sale before it means
    /// anything (StoreData.IsUnlocked asks here):
    ///   • the Shoo Bomb and the Fizz Bomb, until the first pest has turned up;
    ///   • hobby gear (net, binoculars, sewing machine, Chef Tofu's stove, the farm's bridge spirit),
    ///     until a hobby card has opened that hobby — the rod is always there;
    ///   • the Burrow Door, until Miso's first errand (the Bronze Store) is done.
    ///
    /// An island from before this keeps everything its shops already had out: the first time it
    /// loads, whatever is on show is noted and stays for sale. Only what comes later waits.
    /// </summary>
    public static class ShopPacing
    {
        /// <summary>Everything that waits for its moment.</summary>
        static readonly string[] Paced =
        {
            "Deer Bomb", "Bomb", "Bug Net", "Binoculars", "Sewing Machine", "Stove", "Monster Den", Farm.GhostName,
        };

        /// <summary>The errand after which the Burrow Door goes on sale (Miso's first, the Bronze Store).</summary>
        const int FirstErrand = 16;

        static string _firstErrandHeart;

        /// <summary>Whether a paced item is on sale yet; null if the item isn't one of them.</summary>
        public static bool? Ready(string name)
        {
            switch (name)
            {
                case "Deer Bomb":
                case "Bomb": return PestsHaveCome || WasOnSale(name);
                case "Bug Net": return Opened(Hobby.Bugs, name);
                case "Binoculars": return Opened(Hobby.Birding, name);
                case "Sewing Machine": return Opened(Hobby.Sewing, name);
                case "Stove": return Opened(Hobby.Cooking, name);
                case "Monster Den": return FirstErrandDone || WasOnSale(name);
                default: return null;
            }
        }

        static bool Opened(Hobby h, string name) => HobbyLog.IsOpened(h) || WasOnSale(name);

        /// <summary>
        /// The farm's bridge spirit waits for gardening to be opened, unless the island already had
        /// it waiting before pacing.
        /// </summary>
        public static bool FarmSpiritReady => Opened(Hobby.Gardening, Farm.GhostName);

        /// <summary>
        /// An island from before pacing had it out already (or hasn't been looked at yet: until it
        /// has, nothing is hidden from it).
        /// </summary>
        public static bool WasOnSale(string name) => HobbyLog.StockPending || HobbyLog.WasInStock(name);

        /// <summary>
        /// A guinea pig or a zombie capybara has been about: on any island, at any time. Checked
        /// against the live pests whenever a shop asks, and remembered from the first one on.
        /// </summary>
        public static bool PestsHaveCome
        {
            get
            {
                if (HobbyLog.PestSeen) return true;
                var all = MapMarkers.All;
                for (int i = 0; i < all.Count; i++)
                {
                    var m = all[i];
                    if (m == null || m.MarkerKind != MarkerKind.Pest || !m.MarkerActive) continue;
                    HobbyLog.PestSeen = true;
                    return true;
                }
                return false;
            }
        }

        /// <summary>Miso's first errand is done (its heart has come).</summary>
        public static bool FirstErrandDone
        {
            get
            {
                if (_firstErrandHeart == null)
                {
                    _firstErrandHeart = "";
                    foreach (var q in QuestData.Coriander)
                        if (q.Id == FirstErrand) { _firstErrandHeart = q.CompleteMote ?? ""; break; }
                }
                return _firstErrandHeart.Length == 0 || Motes.Has(_firstErrandHeart);
            }
        }

        // ---------------------------------------------------------------- islands from before

        /// <summary>
        /// Notes what an island from before pacing has on show, once it has loaded at home (its shops
        /// set out their wares as they are planted; the beach, the stall and the farm a frame or two
        /// later). Called now and then by <see cref="HobbyCards"/>.
        /// </summary>
        public static void NoteOldStock(float sinceLoad)
        {
            if (!HobbyLog.StockPending || Visit.Active || SaveGame.Instance == null || sinceLoad < 2f) return;
            var paced = new HashSet<string>(Paced);
            var keep = new List<string>();
            foreach (var display in SaleDisplay.All)
                if (display != null && paced.Contains(display.StoreName) && !keep.Contains(display.StoreName)) keep.Add(display.StoreName);
            foreach (var ghost in Object.FindObjectsByType<BridgeGhost>(FindObjectsSortMode.None))
                if (ghost.BridgeName == Farm.GhostName && !keep.Contains(Farm.GhostName)) keep.Add(Farm.GhostName);
            HobbyLog.KeepOldStock(keep);
            if (keep.Count > 0) Debug.Log("[ShopPacing] An island from before: kept on sale " + string.Join(", ", keep));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => _firstErrandHeart = null;
    }
}
