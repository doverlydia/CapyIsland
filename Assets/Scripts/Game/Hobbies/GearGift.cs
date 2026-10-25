using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The first hobby card's gift — a fishing rod, a bug net or binoculars — waiting in a bubble
    /// by the king. Popping the bubble switches its contents on, and that is the moment it is
    /// yours: just as if you had bought it (Purchasing.GearGiven), and then it is gone, since gear
    /// is carried, not planted.
    /// </summary>
    public class GearGift : MonoBehaviour
    {
        string _gear;

        /// <summary>The gift, inactive, ready for a bubble: a picture of the gear, about a step wide.</summary>
        public static GameObject Make(string gear, Sprite art)
        {
            var go = new GameObject(gear);
            go.SetActive(false);
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = art;
            if (art != null) go.transform.localScale = Vector3.one * (1f / Mathf.Max(0.01f, art.bounds.size.x));
            go.AddComponent<GearGift>()._gear = gear;
            return go;
        }

        /// <summary>The gift for a bubble already on the island, if this is one.</summary>
        public string Gear => _gear;

        void OnEnable()
        {
            if (string.IsNullOrEmpty(_gear)) return;   // just made, still asleep in its bubble
            HobbyLog.FreeGear = null;
            Purchasing.GearGiven(_gear);
            Destroy(gameObject);
        }
    }
}
