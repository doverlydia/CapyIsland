using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A little speech-bubble over someone's head in the world, with a picture in it — a dish
    /// someone fancies, a heart from a cat with a present. It bobs gently so it catches the eye,
    /// and can be tapped (the owner decides what that does).
    /// </summary>
    public class WorldBubble : MonoBehaviour, ITappable
    {
        SpriteRenderer _disc, _icon;
        Transform _over;
        float _height, _phase;
        System.Action _onTap;

        /// <summary>Puts a bubble showing <paramref name="icon"/> over <paramref name="over"/>, <paramref name="height"/> units up.</summary>
        public static WorldBubble Show(Transform over, Sprite icon, float height, System.Action onTap = null, Color? tint = null)
        {
            var go = new GameObject("WorldBubble");
            var b = go.AddComponent<WorldBubble>();
            b._over = over;
            b._height = height;
            b._onTap = onTap;
            b._phase = Random.value * 6f;

            b._disc = go.AddComponent<SpriteRenderer>();
            b._disc.sprite = UI.UiKit.Circle;
            b._disc.color = Color.white;
            b._disc.sortingOrder = 30500;
            go.transform.localScale = Vector3.one * 0.9f / Mathf.Max(0.01f, UI.UiKit.Circle.bounds.size.x);

            var tail = new GameObject("Tail").AddComponent<SpriteRenderer>();
            tail.transform.SetParent(go.transform, false);
            tail.sprite = UI.UiKit.Circle;
            tail.color = Color.white;
            tail.sortingOrder = 30499;
            tail.transform.localScale = Vector3.one * 0.3f;
            tail.transform.localPosition = new Vector3(-0.25f, -0.5f, 0f) * UI.UiKit.Circle.bounds.size.x;

            var ic = new GameObject("Icon");
            ic.transform.SetParent(go.transform, false);
            b._icon = ic.AddComponent<SpriteRenderer>();
            b._icon.sortingOrder = 30501;
            b.SetIcon(icon, tint);

            go.transform.position = b.Anchor();
            var full = go.transform.localScale;
            go.transform.localScale = Vector3.zero;
            DG.Tweening.ShortcutExtensions.DOScale(go.transform, full, 0.3f);
            return b;
        }

        public void SetIcon(Sprite icon, Color? tint = null)
        {
            _icon.sprite = icon;
            _icon.color = tint ?? Color.white;
            if (icon == null) return;
            // Fit the picture inside the disc, whatever its size.
            float disc = UI.UiKit.Circle.bounds.size.x;
            float s = disc * 0.66f / Mathf.Max(0.01f, Mathf.Max(icon.bounds.size.x, icon.bounds.size.y));
            _icon.transform.localScale = Vector3.one * s;
            _icon.transform.localPosition = Vector3.zero - (Vector3)icon.bounds.center * s;
        }

        Vector3 Anchor() => _over != null ? _over.position + Vector3.up * _height : transform.position;

        void OnEnable() => TapTargets.Register(this);
        void OnDisable() => TapTargets.Unregister(this);

        void LateUpdate()
        {
            if (_over == null) { Destroy(gameObject); return; }
            transform.position = Anchor() + Vector3.up * Mathf.Sin(Time.time * 2.4f + _phase) * 0.08f;
        }

        public float TapDistance(Vector2 worldPoint)
        {
            if (_onTap == null) return -1f;
            float d = Vector2.Distance(transform.position, worldPoint);
            return d < 0.7f ? d * 0.5f : -1f;   // nearer than whoever it floats over, so it wins the tap
        }

        public void Tap()
        {
            Sfx.Play(SfxKind.Tap, 0.5f);
            _onTap?.Invoke();
        }

        public void Pop()
        {
            DG.Tweening.ShortcutExtensions.DOKill(transform);
            DG.Tweening.TweenSettingsExtensions.OnComplete(DG.Tweening.ShortcutExtensions.DOScale(transform, 0f, 0.2f), () => { if (this != null) Destroy(gameObject); });
        }
    }
}
