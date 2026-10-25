using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A sleeping capybara's thought cloud, tapped: the camera glides to where you can get what it
    /// dreams of and the hand points at it. First a bubble with one inside that you can walk to,
    /// then one you are already carrying, and last the ware beside a shop that sells it.
    /// </summary>
    public static class DreamGuide
    {
        /// <summary>The item a sleeper of this need dreams of, for its cloud and its arrow.</summary>
        public static string DreamItem(StoreItemKind need) => need switch
        {
            StoreItemKind.Forest => "Medium Pine Forest",
            StoreItemKind.Hill => "Medium Gray Hill",
            _ => "Medium Posie Garden",
        };

        public static void ShowWay(StoreItemKind need, Vector2 from)
        {
            var target = BubbleWith(need, from) ?? Carried(need) ?? WareOf(need, from);
            if (target == null) return;
            CameraRig.CameraFocus.Show(target.position, 2.5f);
            var pointer = Object.FindFirstObjectByType<TutorialPointer>();
            if (pointer != null) pointer.PointAt(target, 6f);
        }

        static bool Is(string storeName, StoreItemKind need) =>
            !string.IsNullOrEmpty(storeName) && StoreData.TryGet(storeName, out var def) && def.Kind == need;

        static bool CanWalk(Vector2 from, Vector2 to)
        {
            var land = LandMap.Instance;
            return land == null || land.Reachable(from, to);
        }

        static Transform BubbleWith(StoreItemKind need, Vector2 from)
        {
            Transform best = null;
            float bestDistance = float.MaxValue;
            foreach (var b in Object.FindObjectsByType<Bubble>(FindObjectsSortMode.None))
            {
                if (b.Taken || b.Contents == null || !b.Contents.TryGetComponent<CatalogItem>(out var tag) || !Is(tag.StoreName, need)) continue;
                float d = ((Vector2)b.transform.position - from).sqrMagnitude;
                if (d >= bestDistance || !CanWalk(from, b.transform.position)) continue;
                best = b.transform;
                bestDistance = d;
            }
            return best;
        }

        /// <summary>One in your trail, collected but not yet planted.</summary>
        static Transform Carried(StoreItemKind need)
        {
            foreach (var p in Object.FindObjectsByType<Placeable>(FindObjectsSortMode.None))
                if (!p.Placed && p.TryGetComponent<CatalogItem>(out var tag) && Is(tag.StoreName, need)) return p.transform;
            return null;
        }

        static Transform WareOf(StoreItemKind need, Vector2 from)
        {
            Transform best = null;
            float bestDistance = float.MaxValue;
            foreach (var w in SaleDisplay.All)
            {
                if (w == null || !w.isActiveAndEnabled || !Is(w.StoreName, need)) continue;
                if (!StoreData.IsUnlocked(w.StoreName) || StoreData.IsSoldOut(w.StoreName)) continue;
                float d = ((Vector2)w.transform.position - from).sqrMagnitude;
                if (d >= bestDistance || !CanWalk(from, w.transform.position)) continue;
                best = w.transform;
                bestDistance = d;
            }
            return best;
        }
    }
}
