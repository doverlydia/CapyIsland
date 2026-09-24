using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Starts the game-wide listeners that are not tied to any one object: bits of love get a heart
    /// the moment they fire.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            var art = GetComponent<GameArt>();
            MoteHeart.Listen(art != null ? art.heart : null);
        }
    }
}
