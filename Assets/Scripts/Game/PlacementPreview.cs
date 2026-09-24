using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Draws the dark translucent ellipse under an item while it is being dragged — the item's
    /// <see cref="Placeable.OuterRadius"/>, the area it will serve once planted. The original shows
    /// exactly this, and it is what makes free-position placement legible: you can see whether a
    /// sawmill will actually reach a forest before you commit.
    ///
    /// Turns red when the spot is blocked by another item's footprint.
    ///
    /// One instance lives in the scene and follows whichever item is currently held, rather than
    /// every placeable carrying its own hidden ring.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class PlacementPreview : MonoBehaviour
    {
        [SerializeField] SpriteRenderer ring;

        [Tooltip("Tint while the spot is valid.")]
        [SerializeField] Color validTint = new Color(0.10f, 0.16f, 0.06f, 0.30f);

        [Tooltip("Tint while another item's footprint is in the way.")]
        [SerializeField] Color blockedTint = new Color(0.55f, 0.10f, 0.08f, 0.38f);


        Placeable _tracking;

        void Awake()
        {
            if (ring == null) ring = GetComponentInChildren<SpriteRenderer>();
            Hide();
        }

        void LateUpdate()
        {
            var held = FindHeld();

            if (held == null)
            {
                if (_tracking != null) Hide();
                _tracking = null;
                return;
            }

            if (_tracking != held)
            {
                _tracking = held;
                if (ring != null) ring.enabled = true;
            }

            Follow(held);
        }

        /// <summary>The placeable currently picked up by the player, if any.</summary>
        static Placeable FindHeld()
        {
            foreach (var actor in FindObjectsByType<DraggableActor>(FindObjectsSortMode.None))
            {
                if (!actor.IsHeld) continue;
                var placeable = actor.GetComponent<Placeable>();
                if (placeable != null) return placeable;
            }
            return null;
        }

        void Follow(Placeable placeable)
        {
            if (ring == null) return;

            var at = placeable.transform.position;
            transform.position = new Vector3(at.x, at.y, at.z + 0.01f);

            // The outer radius is a world circle; on screen it is the original's 2:1 ellipse.
            // The ring sprite is one Unity unit across, so its scale is the ellipse size itself.
            // Mills and houses have no outer radius; show their footprint instead, so a blocked
            // spot still turns red.
            float radius = placeable.OuterRadius > 0f ? placeable.OuterRadius : placeable.InnerRadius;
            var size = BunniSpace.ToUnity(new Vector2(radius, radius) * 2f);
            ring.enabled = true;
            transform.localScale = new Vector3(size.x, size.y, 1f);

            bool clear = placeable.CanPlaceAt(at);
            ring.color = clear ? validTint : blockedTint;
        }

        void Hide()
        {
            if (ring != null) ring.enabled = false;
        }
    }
}
