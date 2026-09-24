using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Sprites that things created at runtime need — fallen fruit, treasure chests, the resource
    /// icons that fly to the HUD. One instance in Systems, filled by the island builder, so runtime
    /// spawns need no Resources folder and no per-object wiring.
    /// </summary>
    public class GameArt : MonoBehaviour
    {
        public static GameArt Instance { get; private set; }

        [Header("Common")]
        public GameObject shadowTemplate;

        [Header("Fruit")]
        public Sprite apple;
        public Sprite pear;
        public Sprite plum;

        [Header("Treasure")]
        public Sprite chestClosed;
        public Sprite chestOpen;

        [Header("Resource icons")]
        public Sprite wood;
        public Sprite gold;
        public Sprite gem;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public Sprite Fruit(FruitKind kind) => kind switch
        {
            FruitKind.Apple => apple,
            FruitKind.Pear => pear,
            _ => plum,
        };

        /// <summary>The juice colour of each fruit, for splashes.</summary>
        public static Color Juice(FruitKind kind) => kind switch
        {
            FruitKind.Apple => new Color(0.93f, 0.33f, 0.28f),
            FruitKind.Pear => new Color(0.86f, 0.84f, 0.38f),
            _ => new Color(0.62f, 0.32f, 0.72f),
        };

        public Sprite Icon(ResourceKind kind) => kind switch
        {
            ResourceKind.Wood => wood,
            ResourceKind.Gold => gold,
            _ => gem,
        };
    }
}
