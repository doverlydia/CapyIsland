using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Something the player can pick up and drag across the map — the king, a worker being posted
    /// to a mill, an item being placed. This is how the original moves everything, so it is the
    /// primary verb rather than a convenience.
    ///
    /// Registers itself so the camera's input can ask "did the press land on anything?" without a
    /// physics raycast, which keeps colliders off every prop.
    /// </summary>
    public class DraggableActor : MonoBehaviour
    {

        [Tooltip("How close a press must land to grab this, before this object's scale.")]
        [SerializeField] float grabRadius = 0.9f;
    
        [Tooltip("Sprites pivot at the feet, so the grab circle has to sit up over the body.")]
        [SerializeField] float grabOffsetY = 0.85f;

        [Tooltip("Lift the sprite while held, so it reads as picked up.")]
        [SerializeField] float carryLift = 0.35f;

        Transform _body;
        Vector3 _bodyRest;
        bool _held;

        /// <summary>Grab circle centre, lifted over the body and scaled with the object.</summary>
        public Vector2 GrabCentre => (Vector2)transform.position + Vector2.up * (grabOffsetY * Mathf.Abs(transform.lossyScale.y));

        /// <summary>Grab radius in world units, scaled with the object.</summary>
        public float GrabRadius => grabRadius * Mathf.Abs(transform.lossyScale.y);
        public bool IsHeld => _held;

        void Awake()
        {
            var sprite = transform.Find("Sprite");
            _body = sprite != null ? sprite : transform;
            _bodyRest = _body.localPosition;
        }

        void OnDisable() => _held = false;

        /// <summary>
        /// Nearest draggable under a world point, or null.
        ///
        /// Scans the scene rather than keeping a static registry: a registry desyncs whenever
        /// components are added from an editor script (OnEnable does not fire there) or a domain
        /// reload clears the static. A press happens once per interaction, not per frame, so the
        /// scan costs nothing that matters and cannot go stale.
        /// </summary>
        public static DraggableActor Pick(Vector2 worldPoint)
        {
            DraggableActor best = null;
            float bestDistance = float.MaxValue;
            foreach (var a in FindObjectsByType<DraggableActor>(FindObjectsSortMode.None))
            {
                if (a == null || !a.isActiveAndEnabled) continue;

                // A planted building or forest is permanent. Placement is the commitment, which is
                // what the radius preview exists to make deliberate. Workers carry no Placeable, so
                // they stay pickable and can be moved between stations freely.
                var placeable = a.GetComponent<Placeable>();
                if (placeable != null && placeable.Placed) continue;
                float d = Vector2.Distance(a.GrabCentre, worldPoint);
                if (d > a.GrabRadius || d >= bestDistance) continue;
                best = a;
                bestDistance = d;
            }
            return best;
        }

        public void BeginDrag()
        {
            _held = true;
            if (_body != null) _body.localPosition = _bodyRest + new Vector3(0f, carryLift, 0f);
            SendMessage("OnPickedUp", SendMessageOptions.DontRequireReceiver);
        }

        public void DragTo(Vector2 worldPoint)
        {
            if (!_held) return;
            transform.position = new Vector3(worldPoint.x, worldPoint.y, transform.position.z);
        }

        public void EndDrag()
        {
            if (!_held) return;
            _held = false;
            if (_body != null) _body.localPosition = _bodyRest;

            // No clamping here: whether a drop is allowed is the dropped thing's call (an item
            // checks it is on land and clear; a worker looks for a station), and the islands are
            // not one shape to clamp to.
            SendMessage("OnDropped", SendMessageOptions.DontRequireReceiver);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(GrabCentre, GrabRadius);
        }
    }
}
