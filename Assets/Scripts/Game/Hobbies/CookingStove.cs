using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The stove from the Garden Shop. Once it stands somewhere, tap it to cook: the pot pops up,
    /// you drag ingredients from your bag into it, and then it's you against the heat.
    /// </summary>
    public class CookingStove : MonoBehaviour, ITappable
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
            // The farm's stove takes up cooking the first time you use it: its movie, then the kitchen.
            if (HobbyLog.Unlock(Hobby.Cooking)) { UI.HobbyCutscene.Show(Hobby.Cooking); return; }
            UI.CookingScreen.Show();
        }

        /// <summary>The stove standing on this island, if any — for "where do I cook?" pointers.</summary>
        public static CookingStove Placed()
        {
            foreach (var s in FindObjectsByType<CookingStove>(FindObjectsSortMode.None))
                if (s._placeable != null && s._placeable.Placed) return s;
            return null;
        }
    }
}
