using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A store or an animal house. There is no shop window: once planted, a store lays its stock out
    /// on the ground in a half circle of radius 100 in front of it, from east round through south to
    /// west (StoreBuildingItem.StoreLayout); a house puts its one animal 100 in front
    /// (HouseItem). Tap a display to buy.
    ///
    /// When the building is bubbled or destroyed, its displays go with it.
    /// </summary>
    public class StoreBuilding : MonoBehaviour
    {
        [Tooltip("game.xml <stores> list, e.g. general_store. Empty for a house.")]
        [SerializeField] string listKey;

        [Tooltip("For a house: the animal it sells.")]
        [SerializeField] string houseAnimal;

        const float LayoutRadius = 100f;

        readonly List<SaleDisplay> _displays = new();

        public IReadOnlyList<SaleDisplay> Displays => _displays;
        public string ListKey => listKey;
        public string HouseAnimal => houseAnimal;

        public void Configure(string list, string animal)
        {
            listKey = list;
            houseAnimal = animal;
        }

        void OnPlacedInWorld() => Layout();
        void OnReturnedToBubble() => Clear();
        void OnDisable() => Clear();
        void OnDestroy() => Clear();

        void Layout()
        {
            Clear();
            var parent = transform.parent;
            Vector2 at = transform.position;

            if (!string.IsNullOrEmpty(houseAnimal))
            {
                _displays.Add(SaleDisplay.Create(houseAnimal, at + BunniSpace.ToUnity(new Vector2(0f, -LayoutRadius)), parent));
                return;
            }

            if (string.IsNullOrEmpty(listKey) || !StoreData.Lists.TryGetValue(listKey, out var all)) return;
            // No real-money gems: the Bank has only its wood and gold counters.
            var names = System.Array.FindAll(all, x => x != "Buy Gems");
            int n = names.Length;
            for (int i = 0; i < n; i++)
            {
                // From due east, swinging clockwise on screen (south is down) to due west.
                float degrees = n > 1 ? i * 180f / (n - 1) : 90f;
                float a = degrees * Mathf.Deg2Rad;
                var world = new Vector2(Mathf.Cos(a), -Mathf.Sin(a)) * LayoutRadius;
                _displays.Add(SaleDisplay.Create(names[i], at + BunniSpace.ToUnity(world), parent));
            }
        }

        void Clear()
        {
            foreach (var d in _displays)
                if (d != null) Destroy(d.gameObject);
            _displays.Clear();
        }
    }
}
