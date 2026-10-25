using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// A box that makes room for Easy Reading's bigger words. A label sized to fit its box can
    /// only shrink into it, so with the box fixed the words never grow; this grows the box itself
    /// with the text size — its LayoutElement if it has one, otherwise its own size — on the axes
    /// asked for, and follows the setting as it changes.
    /// </summary>
    [DefaultExecutionOrder(9990)]   // before the labels inside size themselves (LocText, 10000)
    public class GrowsWithText : MonoBehaviour
    {
        RectTransform _rect;
        LayoutElement _layout;
        Vector2 _baseSize;
        float _baseWidth = -1f, _baseHeight = -1f;
        float _tall, _wide;
        int _version = -1;

        /// <summary>
        /// Grows <paramref name="box"/> with the text: <paramref name="tall"/> and
        /// <paramref name="wide"/> are how much of the growth each axis takes (1 all of it, 0 none).
        /// Its size at the end of this frame (whoever placed it has finished sizing it by then) is
        /// its size at normal text.
        /// </summary>
        public static GrowsWithText Add(Component box, float tall = 1f, float wide = 0f)
        {
            if (box == null) return null;
            var g = box.GetComponent<GrowsWithText>();
            if (g == null) g = box.gameObject.AddComponent<GrowsWithText>();
            g._tall = tall;
            g._wide = wide;
            g._capture = true;
            return g;
        }

        bool _capture;

        /// <summary>The box was given a new size by its owner: that is the new normal.</summary>
        public void Rebase()
        {
            _rect = (RectTransform)transform;
            _layout = GetComponent<LayoutElement>();
            float s = _version >= 0 ? Accessibility.TextScale : 1f;   // undo what was applied before
            if (_layout != null)
            {
                _baseWidth = _layout.preferredWidth >= 0f ? _layout.preferredWidth / Grow(s, _wide) : -1f;
                _baseHeight = _layout.preferredHeight >= 0f ? _layout.preferredHeight / Grow(s, _tall) : -1f;
            }
            else _baseSize = new Vector2(_rect.sizeDelta.x / Grow(s, _wide), _rect.sizeDelta.y / Grow(s, _tall));
            _version = -1;
            Apply();
        }

        static float Grow(float scale, float share) => 1f + (scale - 1f) * share;

        void LateUpdate()
        {
            if (_capture) { _capture = false; _version = -1; Rebase(); return; }
            if (_version != Accessibility.Version) Apply();
        }

        void Apply()
        {
            _version = Accessibility.Version;
            float s = Accessibility.TextScale, w = Grow(s, _wide), h = Grow(s, _tall);
            if (_layout != null)
            {
                if (_baseWidth >= 0f) _layout.preferredWidth = _baseWidth * w;
                if (_baseHeight >= 0f) _layout.preferredHeight = _baseHeight * h;
                return;
            }
            // Only an axis the box sizes itself on (its anchors together): a stretched axis follows its parent.
            var size = _rect.sizeDelta;
            if (Mathf.Approximately(_rect.anchorMin.x, _rect.anchorMax.x)) size.x = _baseSize.x * w;
            if (Mathf.Approximately(_rect.anchorMin.y, _rect.anchorMax.y)) size.y = _baseSize.y * h;
            _rect.sizeDelta = size;
        }
    }
}
