using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A little speech-bubble over someone's head in the world, with a picture in it — a dish
    /// someone fancies, a heart from a cat with a present — or a thought cloud over a sleeper,
    /// showing what it dreams of. It bobs gently so it catches the eye, and can be tapped (the
    /// owner decides what that does).
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

        bool _dream;
        Vector3 _rest;

        /// <summary>
        /// A dream: a pale, puffy thought cloud well above a sleeper's head, with two small dots
        /// trailing down to it instead of a speech tail, drifting and swaying gently. Its colours are
        /// duller than the round bubbles', so it never reads as one to pop. The height is measured
        /// from the top of whoever it floats over.
        /// </summary>
        public static WorldBubble Thought(Transform over, Sprite icon, float above, System.Action onTap = null)
        {
            var body = over.GetComponentInChildren<SpriteRenderer>();
            float top = body != null ? body.bounds.max.y - over.position.y : 1f;
            var b = Show(over, icon, top + above, onTap);
            b._dream = true;
            float scale = 0.9f / Mathf.Max(0.01f, UI.UiKit.Circle.bounds.size.x);
            b._rest = Vector3.one * scale;
            b._disc.enabled = false;   // the cloud stands in for the round disc

            // The cloud and its outline, a little wider than the disc was so the picture sits inside.
            var cloud = UI.IconPainter.ThoughtCloud;
            float fit = CloudWidth / Mathf.Max(0.01f, cloud.bounds.size.x * scale);
            var rim = new GameObject("CloudRim").AddComponent<SpriteRenderer>();
            rim.transform.SetParent(b.transform, false);
            rim.transform.localScale = Vector3.one * fit;
            rim.sprite = UI.IconPainter.ThoughtCloudRim;
            rim.color = DreamRim;
            rim.sortingOrder = 30498;
            var fill = new GameObject("Cloud").AddComponent<SpriteRenderer>();
            fill.transform.SetParent(b.transform, false);
            fill.transform.localScale = Vector3.one * fit;
            fill.sprite = cloud;
            fill.color = DreamFill;
            fill.sortingOrder = 30500;
            b._dreamTint = DreamIcon;
            b.SetIcon(icon);   // again, now it's a dream: a bigger picture, faded

            // The speech tail becomes two little thought dots, getting smaller toward the head.
            var tail = b.transform.Find("Tail").GetComponent<SpriteRenderer>();
            tail.color = DreamDot;
            tail.transform.localScale = Vector3.one * 0.26f;
            tail.transform.localPosition = new Vector3(-0.4f, -0.84f, 0f) * UI.UiKit.Circle.bounds.size.x;
            var dot = Object.Instantiate(tail.gameObject, b.transform).GetComponent<SpriteRenderer>();
            dot.name = "Dot";
            dot.transform.localScale = Vector3.one * 0.15f;
            dot.transform.localPosition = new Vector3(-0.56f, -1.14f, 0f) * UI.UiKit.Circle.bounds.size.x;
            return b;
        }

        /// <summary>
        /// The thought cloud's width, in world units: wide enough to read at a glance beside the
        /// round bubbles (0.9 here, 1.6 for the ones with things in) and a sawmill's chimney puffs.
        /// </summary>
        const float CloudWidth = 1.8f;

        /// <summary>A dream's muted colours: a misty off-white cloud, a slate outline, a picture a little faded.</summary>
        public static readonly Color DreamFill = new(0.9f, 0.92f, 0.95f, 0.94f),
            DreamRim = new(0.56f, 0.62f, 0.7f, 0.85f),
            DreamDot = new(0.82f, 0.86f, 0.9f, 0.9f),
            DreamIcon = new(0.9f, 0.9f, 0.92f, 0.85f);

        Color? _dreamTint;

        public void SetIcon(Sprite icon, Color? tint = null)
        {
            _icon.sprite = icon;
            _icon.color = tint ?? _dreamTint ?? Color.white;
            if (icon == null) return;
            // Fit the picture inside the disc (or the wider cloud), whatever its size.
            float disc = UI.UiKit.Circle.bounds.size.x;
            float s = disc * (_dream ? 1f : 0.66f) / Mathf.Max(0.01f, Mathf.Max(icon.bounds.size.x, icon.bounds.size.y));
            _icon.transform.localScale = Vector3.one * s;
            _icon.transform.localPosition = Vector3.zero - (Vector3)icon.bounds.center * s;
        }

        Vector3 Anchor() => _over != null ? _over.position + Vector3.up * _height : transform.position;

        void OnEnable() => TapTargets.Register(this);
        void OnDisable() => TapTargets.Unregister(this);

        void LateUpdate()
        {
            if (_over == null) { Destroy(gameObject); return; }
            if (_dream)
            {
                // Slow and dreamy: a lazy float, a little sway, and a breath, once it has popped in.
                float t = Time.time + _phase;
                transform.position = Anchor() + new Vector3(Mathf.Sin(t * 0.9f) * 0.06f, Mathf.Sin(t * 1.5f) * 0.12f, 0f);
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.1f) * 5f);
                if (!DG.Tweening.DOTween.IsTweening(transform))
                    transform.localScale = _rest * (1f + 0.05f * Mathf.Sin(t * 2.2f));
                return;
            }
            transform.position = Anchor() + Vector3.up * Mathf.Sin(Time.time * 2.4f + _phase) * 0.08f;
        }

        public float TapDistance(Vector2 worldPoint)
        {
            if (_onTap == null) return -1f;
            float d = Vector2.Distance(transform.position, worldPoint);
            return d < (_dream ? 0.9f : 0.7f) ? d * 0.5f : -1f;   // nearer than whoever it floats over, so it wins the tap
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
