using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A resource icon and "+N" that pop up at a spot on the island and fly to their counter in the
    /// resource bar, crediting the amount when they arrive — the original's MillText / MineText /
    /// GemText pings, which tween to the HUD over two seconds and add on completion.
    /// </summary>
    public class ResourcePing : MonoBehaviour
    {
        const float FlySeconds = 1.1f;
        const float PopSeconds = 0.35f;

        ResourceKind _kind;
        int _amount;
        Vector3 _start;
        float _t;
        SpriteRenderer _icon;
        TextMeshPro _label;
        bool _credited;

        /// <summary>Launches a ping from <paramref name="worldFrom"/>; the amount is added on arrival.</summary>
        public static void Fly(ResourceKind kind, int amount, Vector2 worldFrom)
        {
            var go = new GameObject("Ping_" + kind);
            go.transform.position = new Vector3(worldFrom.x, worldFrom.y, -1f);
            var ping = go.AddComponent<ResourcePing>();
            ping.Init(kind, amount);
        }

        void Init(ResourceKind kind, int amount)
        {
            _kind = kind;
            _amount = amount;
            _start = transform.position;

            var art = GameArt.Instance;
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(transform, false);
            _icon = iconGo.AddComponent<SpriteRenderer>();
            _icon.sprite = art != null ? art.Icon(kind) : null;
            _icon.sortingOrder = 32000;
            FitIcon(0.7f);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0.62f, 0f, 0f);
            _label = labelGo.AddComponent<TextMeshPro>();
            _label.text = "+" + amount;
            _label.fontSize = 5f;
            _label.fontStyle = FontStyles.Bold;
            _label.alignment = TextAlignmentOptions.Left;
            _label.color = Color.white;
            _label.outlineWidth = 0.25f;
            _label.outlineColor = new Color32(70, 42, 20, 255);
            _label.rectTransform.sizeDelta = new Vector2(3f, 1f);
            _label.rectTransform.pivot = new Vector2(0f, 0.5f);
            _label.sortingOrder = 32001;

            transform.localScale = Vector3.zero;
            transform.DOScale(1f, PopSeconds).SetEase(Ease.OutBack);
        }

        void FitIcon(float size)
        {
            if (_icon == null || _icon.sprite == null) return;
            var b = _icon.sprite.bounds.size;
            float s = size / Mathf.Max(0.01f, Mathf.Max(b.x, b.y));
            _icon.transform.localScale = Vector3.one * s;
            _icon.transform.localPosition = new Vector3(0f, -_icon.sprite.bounds.center.y * s, 0f);
        }

        void Update()
        {
            _t += Time.deltaTime;
            if (_t < PopSeconds + 0.25f)
            {
                // Hang in the air a moment so the player sees what they got.
                transform.position = _start + Vector3.up * Mathf.Sin(_t * 6f) * 0.05f;
                return;
            }

            float k = Mathf.Clamp01((_t - PopSeconds - 0.25f) / FlySeconds);
            float eased = k * k * (3f - 2f * k);

            // The target moves with the camera, so it is recomputed every frame.
            var target = UI.ResourceBar.CounterWorldPosition(_kind, _start.z);
            var from = _start + Vector3.up * 0.3f;
            var pos = Vector3.Lerp(from, target, eased);
            pos.y += Mathf.Sin(eased * Mathf.PI) * 1.2f;   // a little arc
            transform.position = pos;
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, eased);

            if (k < 1f) return;
            Credit();
            Destroy(gameObject);
        }

        void Credit()
        {
            if (_credited) return;
            _credited = true;
            if (ResourceStore.Instance != null) ResourceStore.Instance.Add(_kind, _amount);
            Sfx.Play(SfxKind.Coin, 0.35f, 0.15f);
            UI.ResourceBar.Bump(_kind);
        }

        void OnDestroy()
        {
            transform.DOKill();
            Credit();   // never lose a payout, even if destroyed early
        }
    }
}
