using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Something you can carry and drop that is not a Placeable (a message bottle): it still gets
    /// the placing ring, green where it may go and red where it may not.
    /// </summary>
    public interface IDropSpot
    {
        /// <summary>The ring's size on screen, in world units (an ellipse, wider than tall).</summary>
        Vector2 DropSize { get; }
        bool CanDropAt(Vector2 at);
        string DropName { get; }
    }

    /// <summary>
    /// Draws the translucent ellipse under an item while it is being dragged — the item's
    /// <see cref="Placeable.OuterRadius"/>, the area it will serve once planted. The original shows
    /// exactly this, and it is what makes free-position placement legible: you can see whether a
    /// sawmill will actually reach a forest before you commit.
    ///
    /// Green when the spot is clear, red when another item's footprint is in the way, with a tick or
    /// a cross beside it so the answer does not rest on colour alone.
    ///
    /// The held item's name floats above it, so you know what you picked out of the trail —
    /// only while it is in your hand, so the island never fills up with labels.
    ///
    /// One instance lives in the scene and follows whichever item is currently held, rather than
    /// every placeable carrying its own hidden ring.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class PlacementPreview : MonoBehaviour
    {
        [SerializeField] SpriteRenderer ring;

        /// <summary>The two states, shared with the tour's picture of them.</summary>
        public static readonly Color ClearColour = new(0.30f, 0.78f, 0.34f, 0.42f);
        public static readonly Color BlockedColour = new(0.90f, 0.22f, 0.16f, 0.45f);

        SpriteRenderer _mark, _markDisc;
        TMPro.TextMeshPro _name;


        Component _tracking;

        void Awake()
        {
            if (ring == null) ring = GetComponentInChildren<SpriteRenderer>();
            BuildMark();
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

        /// <summary>The placeable (or other droppable thing) currently picked up by the player, if any.</summary>
        static Component FindHeld()
        {
            foreach (var actor in FindObjectsByType<DraggableActor>(FindObjectsSortMode.None))
            {
                if (!actor.IsHeld) continue;
                var placeable = actor.GetComponent<Placeable>();
                if (placeable != null) return placeable;
                if (actor.GetComponent<IDropSpot>() is Component spot) return spot;
            }
            return null;
        }

        void Follow(Component held)
        {
            if (ring == null) return;

            var at = held.transform.position;
            transform.position = new Vector3(at.x, at.y, at.z + 0.01f);

            Vector2 size;
            bool clear;
            string label;
            if (held is Placeable placeable)
            {
                // The outer radius is a world circle; on screen it is the original's 2:1 ellipse.
                // The ring sprite is one Unity unit across, so its scale is the ellipse size itself.
                // Mills and houses have no outer radius; show their footprint instead, so a blocked
                // spot still turns red.
                float radius = placeable.OuterRadius > 0f ? placeable.OuterRadius : placeable.InnerRadius;
                size = BunniSpace.ToUnity(new Vector2(radius, radius) * 2f);
                clear = placeable.CanPlaceAt(at);
                var tag = placeable.GetComponent<CatalogItem>();
                label = StoryText.Name(tag != null ? tag.StoreName : placeable.DisplayName);
            }
            else
            {
                var spot = (IDropSpot)held;
                size = spot.DropSize;
                clear = spot.CanDropAt(at);
                label = spot.DropName;
            }
            ring.enabled = true;
            transform.localScale = new Vector3(size.x, size.y, 1f);
            ring.color = clear ? ClearColour : BlockedColour;

            if (_name != null)
            {
                _name.enabled = true;
                _name.text = label;
                var body = held.GetComponentInChildren<SpriteRenderer>();
                float top = body != null ? body.bounds.max.y : at.y + 1.5f;
                _name.transform.position = new Vector3(at.x, top + 0.2f, at.z - 0.02f);
            }

            // The tick or cross rides at the right-hand edge of the ellipse, clear of the finger.
            if (_markDisc != null)
            {
                _markDisc.enabled = _mark.enabled = true;
                _markDisc.transform.position = new Vector3(at.x + size.x * 0.5f + 0.15f, at.y + 0.2f, at.z - 0.02f);
                var solid = clear ? ClearColour : BlockedColour;
                _markDisc.color = new Color(solid.r, solid.g, solid.b, 1f);
                _mark.sprite = clear ? UI.IconPainter.Check : UI.IconPainter.Close;
            }
        }

        void BuildMark()
        {
            var disc = new GameObject("PlacementMark");
            disc.transform.SetParent(transform.parent, false);
            _markDisc = disc.AddComponent<SpriteRenderer>();
            _markDisc.sprite = UI.UiKit.Circle;
            disc.transform.localScale = Vector3.one * 0.5f;
            var icon = new GameObject("Icon");
            icon.transform.SetParent(disc.transform, false);
            icon.transform.localScale = Vector3.one * 0.62f;
            _mark = icon.AddComponent<SpriteRenderer>();
            _mark.color = Color.white;
            if (ring != null)
            {
                _markDisc.sortingLayerID = _mark.sortingLayerID = ring.sortingLayerID;
            }
            // Over everything on the island, the held item included.
            _markDisc.sortingOrder = 32000;
            _mark.sortingOrder = 32001;

            var label = new GameObject("HeldName");
            label.transform.SetParent(transform.parent, false);
            _name = label.AddComponent<TMPro.TextMeshPro>();
            if (UI.UiKit.HeadingFont != null) _name.font = UI.UiKit.HeadingFont;
            _name.fontSize = 4.2f;
            _name.alignment = TMPro.TextAlignmentOptions.Bottom;
            _name.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            _name.color = Color.white;
            _name.outlineWidth = 0.28f;
            _name.outlineColor = new Color32(60, 40, 25, 255);
            _name.rectTransform.sizeDelta = new Vector2(10f, 1.2f);
            _name.rectTransform.pivot = new Vector2(0.5f, 0f);   // sits on top of the item
            _name.sortingOrder = 32002;
            UI.LocText.Attach(_name);
        }

        void Hide()
        {
            if (ring != null) ring.enabled = false;
            if (_markDisc != null) _markDisc.enabled = _mark.enabled = false;
            if (_name != null) _name.enabled = false;
        }
    }
}
