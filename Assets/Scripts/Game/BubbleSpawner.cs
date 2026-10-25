using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Wraps a used-up item back into a bubble so the player can collect and replant it.
    ///
    /// Keeps the loop closed: a forest that has been chopped flat is not litter, it is next
    /// season's forest sitting on the ground waiting to be picked up.
    /// </summary>
    public static class BubbleSpawner
    {
        /// <summary>
        /// Resets a placed item to its carried state, hides it, and drops a fresh bubble holding it
        /// at the same spot.
        /// </summary>
        public static void ReturnToBubble(Placeable item)
        {
            if (item == null) return;

            var at = item.transform.position;
            var parent = item.transform.parent;

            // Reset the item so a replant behaves like the first one.
            item.ResetForRebirth();
            item.gameObject.SetActive(false);

            var go = new GameObject("Bubble_" + item.DisplayName.Replace(" ", ""));
            go.transform.SetParent(parent, false);
            go.transform.position = at;

            int order = 3000 + Mathf.RoundToInt(-at.y * 100f);

            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = Vector3.one * 1.6f;
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = BubbleSprite;
            sr.sortingOrder = order;

            var bubble = go.AddComponent<Bubble>();
            bubble.Fill(item.gameObject);
        }

        /// <summary>
        /// Drops a bubble holding an item that has not been in the world yet — the garden the
        /// tutorial hands out once the first worker falls asleep. The item must already exist
        /// inactive in the scene, the way the builder stashes everything.
        /// </summary>
        public static Bubble SpawnAt(Placeable item, Vector2 at, Transform parent) =>
            item == null ? null : SpawnAt(item.gameObject, at, parent);

        /// <summary>A bubble around anything — an item, or an animal from a quest or a bomb.</summary>
        public static Bubble SpawnAt(GameObject item, Vector2 at, Transform parent)
        {
            if (item == null) return null;
            var placeable = item.GetComponent<Placeable>();
            if (placeable != null) placeable.ResetForRebirth();
            item.SetActive(false);

            var go = new GameObject("Bubble_" + item.name.Replace(" ", ""));
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(at.x, at.y, 0f);

            int order = 3000 + Mathf.RoundToInt(-at.y * 100f);

            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = Vector3.one * 1.6f;
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = BubbleSprite;
            sr.sortingOrder = order;

            var bubble = go.AddComponent<Bubble>();
            bubble.Fill(item);
            return bubble;
        }

        static Sprite _bubbleSprite;

        /// <summary>
        /// The glass: its own copy in Resources (World/Bubble). Borrowing it from a bubble already
        /// in the scene, as this once did, failed on an island reopened after every bubble it
        /// started with had been popped — new bubbles came out as bare items with no glass.
        /// </summary>
        public static Sprite BubbleSprite
        {
            get
            {
                if (_bubbleSprite != null) return _bubbleSprite;
                _bubbleSprite = Resources.Load<Sprite>("World/Bubble");
                if (_bubbleSprite != null) return _bubbleSprite;
                foreach (var b in Resources.FindObjectsOfTypeAll<Bubble>())
                {
                    var sprite = b.transform.Find("Sprite");
                    var sr = sprite != null ? sprite.GetComponent<SpriteRenderer>() : null;
                    if (sr != null && sr.sprite != null) { _bubbleSprite = sr.sprite; break; }
                }
                return _bubbleSprite;
            }
        }
    }
}
