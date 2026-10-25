using System.Collections.Generic;
using System.Threading.Tasks;

namespace CapyIsland.Game
{
    /// <summary>
    /// Someone else's burrow, to look round on a visit (UI/Home/BurrowScreen.ShowFriend). A real
    /// friend's is the one they last saved (RealFriends.FetchHome); an islander's is furnished
    /// to suit them, the same every time you call (from who they are), roomier the further along
    /// their island is.
    /// </summary>
    public static class FriendHomes
    {
        public static async Task<(List<HomeLog.Placed> placed, int rooms)> Of(FriendProfile who)
        {
            if (who != null && who.IsReal)
            {
                var json = await RealFriends.FetchHome(who);
                if (!string.IsNullOrEmpty(json)) return HomeLog.Read(json);
                return (new List<HomeLog.Placed>(), 1);   // not furnished yet (or an older game): a bare room
            }
            return Islander(who);
        }

        static (List<HomeLog.Placed>, int) Islander(FriendProfile who)
        {
            var rng = new System.Random(who != null ? who.seed * 31 + 7 : 1);
            int level = who != null ? who.level : 1;
            int rooms = UnityEngine.Mathf.Clamp(1 + level / 8, 1, 3);
            var choices = new List<FurnitureDef>();
            foreach (var f in Furniture.All) if (!f.Free && f.Art != null) choices.Add(f);
            var placed = new List<HomeLog.Placed>();
            if (choices.Count == 0) return (placed, rooms);
            for (int room = 0; room < rooms; room++)
            {
                // A rug in the middle, then a handful of pieces spread along the room.
                var rug = Furniture.Get("Rug");
                if (rug != null && rug.Art != null && rng.NextDouble() < 0.7)
                    placed.Add(new HomeLog.Placed { id = rug.Id, x = 0.35f + (float)rng.NextDouble() * 0.3f, y = 0.35f, room = room });
                int n = 5 + rng.Next(4);
                for (int i = 0; i < n; i++)
                {
                    var f = choices[rng.Next(choices.Count)];
                    if (f.Flat) continue;
                    float x = (i + 0.5f) / n * 0.84f + 0.08f + ((float)rng.NextDouble() - 0.5f) * 0.05f;
                    float y = 0.55f + (float)rng.NextDouble() * 0.4f;
                    if (room == 0 && x < 0.22f) x += 0.14f;   // leave the door and its steps clear
                    placed.Add(new HomeLog.Placed { id = f.Id, x = x, y = y, room = room, flip = rng.NextDouble() < 0.4 });
                }
            }
            return (placed, rooms);
        }
    }
}
