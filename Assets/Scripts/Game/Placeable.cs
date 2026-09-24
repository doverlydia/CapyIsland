using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// An item that lives in the inventory trail until the player drags it onto the ground.
    /// Carries the identity the UI needs (name, blurb, recipe line) and the two radii the original
    /// uses: <see cref="innerRadius"/> is the footprint nothing may overlap, <see cref="outerRadius"/>
    /// is the area it serves, drawn as a translucent ellipse while placing.
    /// </summary>
    public class Placeable : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] string displayName = "Item";

        [TextArea(2, 4)]
        [SerializeField] string description = "";

        [Tooltip("The little production line shown on the found-item card, e.g. 'Trees + Sawmill + Capy = 3 wood'.")]
        [SerializeField] string recipeLine = "";

        [Header("Radii (original's inner/outer, in world units)")]
        [Tooltip("Footprint. Nothing else may overlap this.")]
        [SerializeField] float innerRadius = 0.8f;

        [Tooltip("Area of effect. Drawn as a translucent ellipse while placing.")]
        [SerializeField] float outerRadius = 4f;

        [Header("Carried look")]
        [Tooltip("While in the inventory trail an item shows as a small icon, not at full size. " +
                 "This is the fraction of its placed scale it shrinks to.")]
        [Range(0.15f, 1f)]
        [SerializeField] float carriedScale = 0.7f;

        [Header("State")]
        [SerializeField] bool placed;

        Vector3 _placedScale;
        bool _capturedScale;

        public string DisplayName => displayName;
        public string Description => description;
        public string RecipeLine => recipeLine;
        public float InnerRadius => innerRadius;
        public float OuterRadius => outerRadius;
        public bool Placed => placed;

        void Awake()
        {
            // Capture the authored scale FIRST. Capturing it lazily inside MarkCarried let a saved,
            // already-shrunken scale be mistaken for full size, so items shrank again on every load.
            CaptureScale();
            transform.localScale = placed ? _placedScale : _placedScale * carriedScale;
        }

        /// <summary>True when this spot is clear of every other placed item's footprint.</summary>
        public bool CanPlaceAt(Vector2 worldPoint)
        {
            foreach (var other in FindObjectsByType<Placeable>(FindObjectsSortMode.None))
            {
                if (other == this || !other.placed) continue;
                float gap = Vector2.Distance(other.transform.position, worldPoint);
                if (gap < other.innerRadius + innerRadius) return false;
            }
            return true;
        }

        /// <summary>
        /// Dropped by the player. Plants it if the spot is clear; otherwise it goes back into the
        /// trail rather than overlapping something, which is what the original does when it says
        /// "Placement is blocked".
        /// </summary>
        void OnDropped()
        {
            if (CanPlaceAt(transform.position)) { MarkPlaced(); return; }

            // Blocked: back into the trail at the king's feet, not left hovering over the obstacle.
            MarkCarried();
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king != null)
            {
                var behind = (Vector2)king.transform.position + new Vector2(-0.9f, -0.5f);
                transform.position = new Vector3(behind.x, behind.y, transform.position.z);
            }
        }

        /// <summary>Picked up again: back into the inventory trail.</summary>
        void OnPickedUp() => MarkCarried();

        public void MarkPlaced()
        {
            CaptureScale();
            placed = true;
            transform.localScale = _placedScale;

            var follower = GetComponent<Follower>();
            if (follower != null) follower.PlaceInWorld();

            // A forest item is a single tree icon while carried; planting it grows the real thing.
            SendMessage("OnPlacedInWorld", SendMessageOptions.DontRequireReceiver);
        }

        public void MarkCarried()
        {
            CaptureScale();
            placed = false;
            // Carried items ride in the trail as small icons, not full-size buildings and forests.
            transform.localScale = _placedScale * carriedScale;

            var follower = GetComponent<Follower>();
            if (follower != null) follower.PickUpIntoInventory();
        }

        /// <summary>
        /// Remembers the authored scale once, so repeated pick-up/place cycles cannot compound the
        /// carried shrink into nothing.
        /// </summary>
        void CaptureScale()
        {
            if (_capturedScale) return;
            _placedScale = transform.localScale;
            _capturedScale = true;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, innerRadius);
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, outerRadius);
        }
    }
}
