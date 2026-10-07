using System.Globalization;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The airstrip: build one on your island to fly to your neighbours. Building it hands you the
    /// airstrip in a bubble, and you put it wherever you like. Where it stands is kept in its own
    /// preferences, like the hobbies ("x,y" once placed, empty while it is still waiting in a
    /// bubble or your trail). Tapping it opens the server map.
    /// </summary>
    public class Airport : MonoBehaviour, ITappable
    {
        public const int CostWood = 300, CostGold = 300;
        const string Key = "CapyIsland.Airport";

        public static bool Built => PlayerPrefs.HasKey(Key);

        /// <summary>Built and standing somewhere: you can take off.</summary>
        public static bool Placed => !string.IsNullOrEmpty(PlayerPrefs.GetString(Key, ""));

        SpriteRenderer _sr;
        Placeable _placeable;

        /// <summary>
        /// Puts the airstrip back where it stands, or — if it was never put down — gives it back in
        /// a bubble. Home island only.
        /// </summary>
        public static void Restore(MonoBehaviour host)
        {
            if (!Built || Visit.Active) return;
            var parts = PlayerPrefs.GetString(Key).Split(',');
            if (parts.Length == 2
                && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            {
                var go = Make();
                go.transform.position = new Vector3(x, y, 0f);
                go.SetActive(true);
                go.GetComponent<Placeable>().MarkPlaced(silent: true);
                return;
            }
            HobbyItems.AfterLoad(host, () => HobbyItems.Offer(Make()));
        }

        /// <summary>Pays for the airstrip and hands it over in a bubble. Returns a reason it could not, or null.</summary>
        public static string Build()
        {
            if (Built) return null;
            if (Visit.Active) return "Build your airstrip on your own island.";
            var store = ResourceStore.Instance;
            if (store == null || !store.CanAfford(CostWood, CostGold, 0)) return $"An airstrip costs {CostWood} wood and {CostGold} gold.";
            store.TrySpend(CostWood, CostGold, 0);
            PlayerPrefs.SetString(Key, "");
            PlayerPrefs.Save();
            var bubble = HobbyItems.Offer(Make());
            if (bubble != null) CameraRig.CameraFocus.Show(bubble.transform.position, 1.5f);
            Sfx.Play(SfxKind.Bridge, 0.8f);
            return null;
        }

        static GameObject Make()
        {
            var go = HobbyItems.Make("Airstrip", HobbyData.Art("Airport"), 3.6f, 0.6f, 110f);
            var a = go.AddComponent<Airport>();
            a._sr = go.transform.Find("Sprite").GetComponent<SpriteRenderer>();
            a._placeable = go.GetComponent<Placeable>();
            return go;
        }

        /// <summary>Put down by the player: this is where it stands from now on.</summary>
        void OnPlacedInWorld()
        {
            Vector2 at = transform.position;
            bool first = !Placed;
            PlayerPrefs.SetString(Key, at.x.ToString(CultureInfo.InvariantCulture) + "," + at.y.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
            if (!first) return;
            Motes.Trigger("built_airport");
            var full = transform.localScale;
            transform.localScale = full * 0.2f;
            transform.DOScale(full, 0.5f).SetEase(Ease.OutBack);
            Hints.Show("Your airstrip is ready! Tap it any time to see who's online.");
        }

        /// <summary>Knocked back into a bubble (a bomb): it waits to be put down again.</summary>
        void OnReturnedToBubble()
        {
            PlayerPrefs.SetString(Key, "");
            PlayerPrefs.Save();
        }

        void OnEnable() => TapTargets.Register(this);
        void OnDisable() => TapTargets.Unregister(this);

        public float TapDistance(Vector2 worldPoint)
        {
            if (_sr == null || _sr.sprite == null || _placeable == null || !_placeable.Placed) return -1f;
            var b = _sr.bounds;
            return b.Contains(new Vector3(worldPoint.x, worldPoint.y, b.center.z)) ? Vector2.Distance(b.center, worldPoint) : -1f;
        }

        public void Tap()
        {
            Sfx.Play(SfxKind.Tap, 0.5f);
            if (UI.FriendsScreen.Instance != null) UI.FriendsScreen.Instance.Open();
        }
    }
}
