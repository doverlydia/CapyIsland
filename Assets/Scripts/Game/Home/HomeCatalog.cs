using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Your home under the ground, on the island side: the den becomes its front door, and the
    /// Furniture Shop is a building like any other (built in code, its art in Resources/Home).
    /// Both are handed to the catalog when it wakes, so they are bought, carried, bubbled, placed
    /// and saved exactly like everything else.
    /// </summary>
    public static class HomeCatalog
    {
        public static void RegisterInto(ItemCatalog catalog)
        {
            // The den (the island builder's "Monster Den") is the door down.
            var den = catalog.Template("Monster Den");
            if (den != null && den.GetComponent<BurrowDoor>() == null) den.AddComponent<BurrowDoor>();

            var art = Resources.Load<Sprite>("Home/Store_Furniture");
            if (art == null) return;
            var shop = HobbyItems.Make("Furniture Shop", art, 3.1f, 0.95f, StoreData.Get("Furniture Shop").Inner);
            shop.AddComponent<FurnitureShop>();
            shop.AddComponent<CatalogItem>().SetName("Furniture Shop");
            shop.transform.SetParent(catalog.transform, false);
            catalog.AddTemplate(shop.GetComponent<CatalogItem>());
        }
    }

    /// <summary>Something on the island you tap when it stands placed (its picture is what you tap).</summary>
    public abstract class PlacedTappable : MonoBehaviour, ITappable
    {
        Placeable _placeable;
        SpriteRenderer _body;

        protected virtual void Awake()
        {
            _placeable = GetComponent<Placeable>();
            var sprite = transform.Find("Sprite");
            _body = sprite != null ? sprite.GetComponent<SpriteRenderer>() : null;
        }

        void OnEnable() => TapTargets.Register(this);
        void OnDisable() => TapTargets.Unregister(this);

        public float TapDistance(Vector2 worldPoint)
        {
            if (_body == null || _placeable == null || !_placeable.Placed) return -1f;
            var b = _body.bounds;
            return b.Contains(new Vector3(worldPoint.x, worldPoint.y, b.center.z)) ? Vector2.Distance(b.center, worldPoint) : -1f;
        }

        public abstract void Tap();
    }

    /// <summary>The den: your front door. Tap it and you slide down the tunnel into your burrow.</summary>
    public class BurrowDoor : PlacedTappable
    {
        public override void Tap()
        {
            Sfx.Play(SfxKind.Tap, 0.5f);
            // A friend's: down you go to look round (theirs to arrange, not yours).
            if (Visit.Active)
            {
                var host = Visit.Host;
                if (host == null) return;
                UI.TunnelCutscene.Show(() => UI.BurrowScreen.ShowFriend(host));
                return;
            }
            UI.TunnelCutscene.Show(UI.BurrowScreen.Show);
        }

        /// <summary>How many dens this island has, standing or carried in the trail: one is all an island gets.</summary>
        public static int OnThisIsland()
        {
            int n = 0;
            foreach (var d in FindObjectsByType<BurrowDoor>(FindObjectsSortMode.None))
            {
                var p = d.GetComponent<Placeable>();
                var f = d.GetComponent<Follower>();
                if ((p != null && p.Placed) || (f != null && f.Carried)) n++;
            }
            // …or still in its bubble.
            foreach (var b in FindObjectsByType<Bubble>(FindObjectsSortMode.None))
                if (!b.Taken && b.Contents != null && b.Contents.GetComponent<BurrowDoor>() != null) n++;
            return n;
        }

        /// <summary>Your den on the island, if you have one placed.</summary>
        public static BurrowDoor Placed()
        {
            foreach (var d in FindObjectsByType<BurrowDoor>(FindObjectsSortMode.None))
                if (d.GetComponent<Placeable>() is Placeable p && p.Placed) return d;
            return null;
        }
    }

    /// <summary>The Furniture Shop: tap it to see what's in today.</summary>
    public class FurnitureShop : PlacedTappable
    {
        public override void Tap()
        {
            Sfx.Play(SfxKind.Tap, 0.5f);
            UI.FurnitureShopScreen.Show(transform.position);
        }
    }
}
