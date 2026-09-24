using System;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// What happens when you buy something (StoreItem.purchase) or pick up a free bubble
    /// (StoreItem.pickUp):
    ///
    ///   * anything you place — gardens, forests, hills, buildings, stores, trees, bombs, statues —
    ///     joins your trail at the king's feet, ready to drag out;
    ///   * a worker animal appears at the shop and follows you; a Monster or Deer comes out wild;
    ///   * the Wedding Dress, pearls and tiara go straight to Coriander;
    ///   * the Church is built at once on Wedding Isle.
    ///
    /// Then, every time: the item's first-discovery card, and any purchase mote.
    /// </summary>
    public static class Purchasing
    {
        /// <summary>Raised after anything is bought or collected, with its name and what was made.</summary>
        public static event Action<string, GameObject> Acquired;

        /// <summary>Raised when a gift for Coriander is bought.</summary>
        public static event Action<string> GiftGiven;

        /// <summary>Where the Church always stands, in world units (StoreItemsList.MakeItem).</summary>
        public static readonly Vector2 ChurchWorldSpot = new Vector2(3937f, 1027f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            Acquired = null;
            GiftGiven = null;
        }

        /// <summary>Spends the price and delivers the item. False if it cannot be afforded.</summary>
        public static bool TryBuy(string name, Vector2 shopSpot)
        {
            var store = ResourceStore.Instance;
            var price = StoreData.Price(name);
            if (store == null || !store.TrySpend(price.x, price.y, price.z)) return false;
            Sfx.Play(SfxKind.Buy, 0.8f);
            Deliver(name, shopSpot);
            return true;
        }

        /// <summary>Makes a bought item appear, by kind, then fires the card and mote hooks.</summary>
        public static GameObject Deliver(string name, Vector2 shopSpot)
        {
            var def = StoreData.Get(name);
            var catalog = ItemCatalog.Instance;
            GameObject item = null;

            switch (def.Kind)
            {
                case StoreItemKind.Gift:
                    GiftGiven?.Invoke(name);
                    break;

                case StoreItemKind.Church:
                    if (catalog != null)
                    {
                        var at = LandMap.WorldToUnity(ChurchWorldSpot);
                        item = catalog.MakeItem(name, at);
                        CameraRig.CameraFocus.Show(at, 3f);
                    }
                    break;

                case StoreItemKind.Animal:
                    if (catalog == null) break;
                    item = catalog.Create(name);
                    if (item == null) break;
                    item.transform.position = new Vector3(shopSpot.x, shopSpot.y, 0f);
                    item.SetActive(true);
                    Release(item);
                    break;

                default:
                    if (catalog == null) break;
                    item = catalog.Create(name);
                    if (item == null) break;
                    var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
                    Vector2 spot = king != null ? (Vector2)king.transform.position + new Vector2(-0.8f, -0.4f) : shopSpot;
                    item.transform.position = new Vector3(spot.x, spot.y, 0f);
                    item.SetActive(true);
                    var placeable = item.GetComponent<Placeable>();
                    if (placeable != null) placeable.MarkCarried();
                    break;
            }

            Announce(name, item);
            return item;
        }

        /// <summary>An item came out of a free bubble: the same hooks as a purchase.</summary>
        public static void Collected(GameObject item)
        {
            if (item == null) return;
            var tag = item.GetComponent<CatalogItem>();
            if (tag == null) return;

            // Animals out of a bubble behave as if bought: workers follow, monsters and deer run wild.
            if (item.GetComponent<Placeable>() == null) Release(item);
            Announce(tag.StoreName, item);
        }

        static void Announce(string name, GameObject item)
        {
            Cards.FoundItem(name);
            StoreData.InvalidateCounts();
            Motes.PurchaseMote(name);
            Acquired?.Invoke(name, item);
        }

        /// <summary>
        /// A newly arrived animal: a worker joins the king's trail; anything wild (an untamed
        /// Monster, a Deer) is let loose.
        /// </summary>
        public static void Release(GameObject animal)
        {
            if (animal == null) return;
            var wild = animal.GetComponent<IWild>();
            if (wild != null) { wild.GoWild(); return; }

            var follower = animal.GetComponent<Follower>();
            if (follower != null) follower.PickUpIntoInventory();
        }
    }

    /// <summary>Something that roams on its own rather than following the king (Deer, wild Monster).</summary>
    public interface IWild
    {
        void GoWild();
    }
}
