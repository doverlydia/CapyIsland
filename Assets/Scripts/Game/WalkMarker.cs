using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A soft ring on the ground where you tapped to walk, so the tap visibly landed. It opens out
    /// and fades in half a second; a new tap replaces it.
    /// </summary>
    public static class WalkMarker
    {
        static SpriteRenderer _ring;

        public static void Show(Vector2 at)
        {
            if (_ring == null)
            {
                var go = new GameObject("WalkMarker");
                _ring = go.AddComponent<SpriteRenderer>();
                _ring.sprite = UI.IconPainter.Ring;
                // Drawn on the ground, the same as the placement circle.
                var preview = Object.FindFirstObjectByType<PlacementPreview>(FindObjectsInactive.Include);
                var under = preview != null ? preview.GetComponentInChildren<SpriteRenderer>(true) : null;
                if (under != null) { _ring.sortingLayerID = under.sortingLayerID; _ring.sortingOrder = under.sortingOrder; }
            }
            var t = _ring.transform;
            t.DOKill();
            _ring.DOKill();
            t.position = new Vector3(at.x, at.y, 0f);
            t.localScale = new Vector3(0.5f, 0.25f, 1f);   // flat on the ground, like every circle here
            _ring.color = new Color(1f, 1f, 1f, 0.85f);
            _ring.gameObject.SetActive(true);
            t.DOScale(new Vector3(1.1f, 0.55f, 1f), 0.5f).SetEase(Ease.OutQuad);
            _ring.DOFade(0f, 0.5f).SetEase(Ease.InQuad).OnComplete(() => { if (_ring != null) _ring.gameObject.SetActive(false); });
        }
    }
}
