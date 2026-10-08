using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The hobby kit as real store items: the Garden Shop building and the vegetable plots and
    /// stove it sells. They are built in code (their art lives in Resources/Hobbies) and handed to
    /// the catalog as templates when it wakes, so from then on they are bought, carried, bubbled,
    /// placed, bombed and saved exactly like everything else in a shop.
    /// </summary>
    public static class HobbyCatalog
    {
        public static void RegisterInto(ItemCatalog catalog)
        {
            Add(catalog, GardenShop());
            Add(catalog, Plot());
            Add(catalog, Stove());
            Add(catalog, Gear("Fishing Rod", "Rod"));
            Add(catalog, Gear("Bug Net", "Net"));
            Add(catalog, Gear("Binoculars", "Binoculars"));
            Add(catalog, SewingMachine());
        }

        /// <summary>Gear is never made in the world (buying it unlocks a hobby): it only needs a picture for the shop.</summary>
        static GameObject Gear(string name, string art)
        {
            var sprite = HobbyData.Art(art);
            if (sprite == null) return null;
            var go = new GameObject(name);
            go.SetActive(false);
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            body.AddComponent<SpriteRenderer>().sprite = sprite;
            return Tagged(go, name);
        }

        static GameObject SewingMachine()
        {
            var art = HobbyData.Art("SewingMachine");
            if (art == null) return null;
            var go = HobbyItems.Make("Sewing Machine", art, 1.4f, 0.95f, StoreData.Get("Sewing Machine").Inner);
            go.AddComponent<SewingStation>();
            return Tagged(go, "Sewing Machine");
        }

        static void Add(ItemCatalog catalog, GameObject template)
        {
            if (template == null) return;
            template.transform.SetParent(catalog.transform, false);
            catalog.AddTemplate(template.GetComponent<CatalogItem>());
        }

        static GameObject Tagged(GameObject go, string name)
        {
            go.AddComponent<CatalogItem>().SetName(name);
            return go;
        }

        static GameObject GardenShop()
        {
            var art = HobbyData.Art("Store_Garden");
            if (art == null) return null;
            var go = HobbyItems.Make("Garden Shop", art, 3.1f, 0.95f, StoreData.Get("Garden Shop").Inner);
            go.AddComponent<StoreBuilding>().Configure("garden_store", "");
            return Tagged(go, "Garden Shop");
        }

        static GameObject Plot()
        {
            var art = HobbyData.Art("Plot");
            if (art == null) return null;
            var go = HobbyItems.Make("Vegetable Plot", art, 1.3f, 0f, StoreData.Get("Vegetable Plot").Inner, ySort: false);
            var crop = new GameObject("Crop");
            crop.transform.SetParent(go.transform, false);
            crop.AddComponent<SpriteRenderer>().enabled = false;
            go.AddComponent<Gardening.PlotView>();
            return Tagged(go, "Vegetable Plot");
        }

        static GameObject Stove()
        {
            var art = HobbyData.Art("Stove");
            if (art == null) return null;
            var go = HobbyItems.Make("Stove", art, 1.5f, 0.95f, StoreData.Get("Stove").Inner);
            go.AddComponent<CookingStove>();
            return Tagged(go, "Stove");
        }
    }
}
