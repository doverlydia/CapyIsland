using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Pirate Pete, the buy-back merchant (MapItems/PiratePete). Tap him and he tells a tale — which
    /// is all Coriander wanted (closing any of his dialogs fires "Priate Tales"). After that, drag
    /// spare loot onto him to sell it: anything without an ownership cap, for half its price with
    /// wood and gold swapped, as the original pays. Workers can be sold to him too.
    /// </summary>
    public class PiratePete : MonoBehaviour, ITappable
    {
        /// <summary>How near him an item must be dropped to be offered, in world units.</summary>
        public const float SellRadius = 100f;

        SpriteRenderer _body;
        Placeable _placeable;

        void Awake()
        {
            var body = transform.Find("Sprite");
            _body = body != null ? body.GetComponent<SpriteRenderer>() : null;
            _placeable = GetComponent<Placeable>();
        }

        void OnEnable() => TapTargets.Register(this);
        void OnDisable() => TapTargets.Unregister(this);

        public bool InWorld => _placeable == null || _placeable.Placed;
        Sprite Face => _body != null ? _body.sprite : null;

        public float TapDistance(Vector2 worldPoint)
        {
            if (!InWorld || _body == null) return -1f;
            float d = Vector2.Distance(_body.bounds.center, worldPoint);
            return d < 1f ? d : -1f;
        }

        public void Tap()
        {
            string line = Motes.Has("Priate Tales")
                ? "Arr, back again? If you've spare goods, drag 'em onto me and I'll make you an offer."
                : "*Salty Kombu leans in and tells you about the time he arm-wrestled a sea serpent for a barrel of pickles. It goes on for quite a while.*";
            UI.SpeechDialog.Show(Face, StoryText.Sailor, new[] { line }, OnClosed);
        }

        void OnClosed() => Motes.Trigger("girl_quest_3");

        /// <summary>The Pete within reach of where something was dropped, if any.</summary>
        public static PiratePete Near(Vector2 at)
        {
            foreach (var pete in FindObjectsByType<PiratePete>(FindObjectsSortMode.None))
                if (pete.InWorld && BunniSpace.WorldDistance(pete.transform.position, at) <= SellRadius) return pete;
            return null;
        }

        /// <summary>
        /// Offers to buy a dropped item or worker. Returns true if Pete took over the decision —
        /// the item waits by him until Deal or No Deal.
        /// </summary>
        public bool OfferFor(GameObject item, System.Action noDeal)
        {
            // A cat's present. He has seen worse.
            var junk = item.GetComponent<CatJunk>();
            if (junk != null)
            {
                UI.SpeechDialog.Show(Face, StoryText.Sailor, new[] { $"A {junk.Name}? Arr… I've traded for worse. One gold, and not a coin more." }, null,
                    ("Deal", () => Deal(item, new Vector3Int(0, 1, 0))),
                    ("No Deal", noDeal));
                return true;
            }

            var tag = item.GetComponent<CatalogItem>();
            if (tag == null) return false;

            Vector3Int pay;
            var worker = item.GetComponent<Worker>();
            if (worker != null) pay = AnimalPrice(tag.StoreName);
            else
            {
                var def = tag.Def;
                if (!def.IsValid || def.Max != 0)
                {
                    UI.SpeechDialog.Show(Face, StoryText.Sailor, new[] { "Can't take that one off your paws, I'm afraid." }, noDeal);
                    return true;
                }
                var price = StoreData.Price(tag.StoreName);
                pay = new Vector3Int(price.y / 2, price.x / 2, price.z / 2);   // wood and gold swapped, as coded
            }

            var parts = new List<string>();
            if (pay.x > 0) parts.Add(pay.x + " wood");
            if (pay.y > 0) parts.Add(pay.y + " gold");
            if (pay.z > 0) parts.Add(pay.z + " gems");
            string offer = parts.Count > 0 ? string.Join(" and ", parts) : "a pat on the back";

            UI.SpeechDialog.Show(Face, StoryText.Sailor, new[] { $"I'll give you {offer} for your {StoryText.Name(tag.StoreName)}." }, null,
                ("Deal", () => Deal(item, pay)),
                ("No Deal", noDeal));
            return true;
        }

        static Vector3Int AnimalPrice(string animal) => animal switch
        {
            "Capybara" => new Vector3Int(10, 10, 0),
            "Pirate" => new Vector3Int(0, 0, 5),
            "Panda" => new Vector3Int(20, 20, 0),
            "Fox" => new Vector3Int(15, 30, 0),
            "Monster" => new Vector3Int(0, 0, 10),
            _ => Vector3Int.zero,
        };

        void Deal(GameObject item, Vector3Int pay)
        {
            var catalog = ItemCatalog.Instance;
            var parent = catalog != null ? catalog.ItemParent : transform.parent;
            var spot = (Vector2)transform.position + BunniSpace.ToUnity(new Vector2(0f, -70f));
            float x = -1f;
            if (pay.x > 0) { TreasureChest.SpawnValue(spot + new Vector2(x, 0f), ResourceKind.Wood, pay.x, parent); x += 1f; }
            if (pay.y > 0) { TreasureChest.SpawnValue(spot + new Vector2(x, 0f), ResourceKind.Gold, pay.y, parent); x += 1f; }
            if (pay.z > 0) TreasureChest.SpawnValue(spot + new Vector2(x, 0f), ResourceKind.Gem, pay.z, parent);

            var worker = item.GetComponent<Worker>();
            if (worker != null) worker.Unassign();
            Destroy(item);
        }
    }
}
