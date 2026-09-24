using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Marks a thing as a particular store item — its store_items.xml name — whether it is planted,
    /// carried in the trail, sitting in a bubble, or walking about. The shop's sold-out counts and
    /// the quests' "how many do you have" checks count these.
    /// </summary>
    public class CatalogItem : MonoBehaviour
    {
        [SerializeField] string storeName;

        public string StoreName => storeName;
        public StoreItemDef Def => StoreData.Get(storeName);

        public void SetName(string name) => storeName = name;
    }
}
