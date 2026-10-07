using System;
using System.Collections;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Things you get from hobbies and building — a garden plot, the airstrip, an adopted cat —
    /// arrive like everything else on the island: in a bubble. Pop it, the thing joins your trail,
    /// and you drag it to wherever you want it. Nothing appears on top of what you have built.
    ///
    /// These objects are built in code with the same parts as a shop item (trail follower, drag,
    /// placement footprint), so they behave exactly the same in the hand.
    /// </summary>
    public static class HobbyItems
    {
        /// <summary>
        /// A placeable item, inactive, ready for a bubble. Its picture is <paramref name="width"/>
        /// world units wide and lifted by <paramref name="lift"/> of its half-height (0 for flat
        /// things like a plot, 1 to stand a centred sprite on its feet).
        /// </summary>
        public static GameObject Make(string name, Sprite art, float width, float lift, float innerRadius, bool ySort = true)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = art;
            float half = 0.5f;
            if (art != null)
            {
                go.transform.localScale = Vector3.one * (width / Mathf.Max(0.01f, art.bounds.size.x));
                half = art.bounds.extents.y;
                body.transform.localPosition = new Vector3(0f, half * lift, 0f);
            }
            go.AddComponent<Follower>();
            if (ySort) go.AddComponent<YSorter>();
            // Grab anywhere over the picture, however small it is drawn.
            float reach = art != null ? Mathf.Max(art.bounds.extents.x, half) : 0.9f;
            go.AddComponent<DraggableActor>().Configure(reach, half * lift);
            go.AddComponent<Placeable>().Configure(name, innerRadius, 0f);
            return go;
        }

        /// <summary>Wraps an item in a bubble a few steps from the king.</summary>
        public static Bubble Offer(GameObject item, string hint = null)
        {
            var king = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
            Vector2 around = king != null ? (Vector2)king.transform.position : Vector2.zero;
            Vector2 at = around + new Vector2(1.8f, -0.8f);
            var others = UnityEngine.Object.FindObjectsByType<Bubble>(FindObjectsSortMode.None);
            for (int i = 0; i < 40; i++)
            {
                // Widen the ring as tries fail, so a crowd of bubbles still finds room.
                var p = around + UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1.6f, 3f + i * 0.05f);
                if (!BunniSpace.CircleOnMap(p, 40f) || Crowded(p, others)) continue;   // clear ground, not on a building
                at = p;
                break;
            }
            var bubble = BubbleSpawner.SpawnAt(item, at, null);
            if (!string.IsNullOrEmpty(hint)) Hints.Show(hint);
            return bubble;
        }

        static bool Crowded(Vector2 p, Bubble[] others)
        {
            foreach (var b in others)
                if (b != null && b.isActiveAndEnabled && Vector2.Distance(b.transform.position, p) < 1.8f) return true;
            return false;
        }

        /// <summary>
        /// Runs <paramref name="restore"/> once the island has finished loading — the save clears
        /// stray bubbles as it rebuilds, so anything offered earlier would be swept away.
        /// </summary>
        public static void AfterLoad(MonoBehaviour host, Action restore) => host.StartCoroutine(Wait(restore));

        static IEnumerator Wait(Action restore)
        {
            yield return null;
            yield return null;
            restore();
        }

        /// <summary>The trail's own question: is this item planted in the world?</summary>
        public static bool IsPlaced(Component c)
        {
            var p = c != null ? c.GetComponent<Placeable>() : null;
            return p == null || p.Placed;
        }
    }
}
