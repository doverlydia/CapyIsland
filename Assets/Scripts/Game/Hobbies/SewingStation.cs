using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The sewing machine from the Steel Store. Once it stands somewhere, tap it to sew: pick a
    /// a hat (it needs cotton from your garden), decorate
    /// it, and it goes into your wardrobe to wear.
    /// </summary>
    public class SewingStation : MonoBehaviour, ITappable
    {
        Placeable _placeable;
        SpriteRenderer _body;

        void Awake()
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

        public void Tap()
        {
            Sfx.Play(SfxKind.Tap, 0.5f);
            if (UI.SewingScreen.Instance != null) UI.SewingScreen.Instance.Open();
        }

        /// <summary>The sewing machine standing on this island, if any.</summary>
        public static SewingStation Placed()
        {
            foreach (var s in FindObjectsByType<SewingStation>(FindObjectsSortMode.None))
                if (s._placeable != null && s._placeable.Placed) return s;
            return null;
        }
    }
}
