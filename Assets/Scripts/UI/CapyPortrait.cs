using System;
using System.Collections.Generic;
using CapyIsland.Game;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Your capybara as a UI picture, dressed in whatever it is wearing right now, and kept up to
    /// date as you change. The pieces sit where the wardrobe says, measured on the same sprite.
    /// </summary>
    public class CapyPortrait : MonoBehaviour
    {
        static readonly WearSlot[] Order = { WearSlot.Back, WearSlot.Neck, WearSlot.Face, WearSlot.Hat };

        Image _base;
        readonly Dictionary<WearSlot, Image> _layers = new();
        float _height;

        /// <summary>A portrait <paramref name="height"/> units tall, standing on its feet (pivot bottom-centre).</summary>
        public static CapyPortrait Create(Transform parent, string name, float height)
        {
            var r = UiKit.Rect(name, parent);
            r.pivot = new Vector2(0.5f, 0f);
            var p = r.gameObject.AddComponent<CapyPortrait>();
            p._height = height;
            p._base = UiKit.Image(r, "Body", null, Color.white);
            p._base.preserveAspect = true;
            p._base.raycastTarget = false;
            UiKit.Stretch(p._base.rectTransform);
            p.Refresh();
            return p;
        }

        public RectTransform Rect => (RectTransform)transform;

        void OnEnable()
        {
            Wardrobe.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => Wardrobe.Changed -= Refresh;

        public void Refresh()
        {
            if (_base == null) return;
            var body = Wardrobe.Ready ? Wardrobe.BaseSprite : ItemCatalog.Instance != null ? ItemCatalog.Instance.Icon("Capybara") : null;
            _base.sprite = body;
            _base.enabled = body != null;
            if (body == null) return;

            float w = _height * body.rect.width / body.rect.height;
            Rect.sizeDelta = new Vector2(w, _height);

            foreach (var slot in Order)
            {
                var item = Wardrobe.Ready ? Wardrobe.Worn(slot) : null;
                var art = Wardrobe.Art(item);
                var layer = Layer(slot);
                layer.enabled = art != null;
                if (art == null) continue;
                layer.sprite = art;
                var r = layer.rectTransform;
                r.anchorMin = r.anchorMax = item.Anchor;
                r.pivot = new Vector2(0.5f, 0.5f);
                float pw = item.Width * w;
                r.sizeDelta = new Vector2(pw, pw * art.rect.height / art.rect.width);
                r.anchoredPosition = Vector2.zero;
                r.localRotation = Quaternion.Euler(0f, 0f, item.Rotation);
                if (item.Behind) r.SetSiblingIndex(0);
            }
        }

        Image Layer(WearSlot slot)
        {
            if (_layers.TryGetValue(slot, out var img) && img != null) return img;
            img = UiKit.Image(transform, slot.ToString(), null, Color.white);
            img.preserveAspect = true;
            img.raycastTarget = false;
            _layers[slot] = img;
            return img;
        }
    }
}
