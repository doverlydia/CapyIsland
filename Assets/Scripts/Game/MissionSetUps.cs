using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// What reading a quest's intro sets in motion (Quests/MissionSetUps.CallSetup). Several of the
    /// original's set-ups are empty functions (second_island, dress_store, pearl_store,
    /// crown_store, create_wedding): the item they talk about is simply in a store already.
    /// </summary>
    public static class MissionSetUps
    {
        public static void Run(string setUp, QuestGiver giver)
        {
            var catalog = ItemCatalog.Instance;
            var land = LandMap.Instance;
            var king = Object.FindFirstObjectByType<PlayerAvatar>();
            Vector2 kingAt = king != null ? (Vector2)king.transform.position : Vector2.zero;
            Vector2 beside = kingAt + BunniSpace.ToUnity(new Vector2(0f, -70f));
            if (catalog == null) return;

            switch (setUp)
            {
                case "create_store":
                    Focus(catalog.MakeStore("General Store", beside));
                    break;

                case "create_pete":
                    if (land != null) Focus(catalog.MakeStore("Pirate Pete", land.RandomSpotOnIsland(2, 30f)));
                    break;

                case "create_deer":
                    if (land == null) break;
                    // Pests turn up on islands you can reach (LandMap.PestSpot), never across the sea.
                    for (int i = 0; i < 3; i++) catalog.MakeItem("Deer", land.PestSpot(0));
                    for (int i = 0; i < 3; i++) catalog.MakeItem("Deer", land.PestSpot(3));
                    break;

                case "create_priest":
                    Motes.Trigger("girl_quest_10");
                    break;

                case "create_panda":
                    Focus(catalog.MakeStore("Panda", beside));
                    break;

                case "create_monsters":
                    if (land == null) break;
                    land.OpenBridge("monster_isle");
                    catalog.MakeItem("Monster House", land.RandomSpotOnIsland(5, 41f));
                    for (int i = 0; i < 5; i++) catalog.MakeItem("Monster", land.PestSpot(5));
                    catalog.MakeItem("Large Gold Hill", land.RandomSpotOnIsland(6, 36f));
                    break;

                case "create_peach":
                    Focus(catalog.MakeStore("Pirate Peach", beside));
                    break;
            }
        }

        /// <summary>The original's focus arrow: point the player at what just appeared.</summary>
        static void Focus(Bubble bubble)
        {
            if (bubble == null) return;
            var pointer = Object.FindFirstObjectByType<TutorialPointer>();
            if (pointer != null) pointer.PointAt(bubble.transform, 8f);
        }
    }
}
