using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// A pill or chip sized to its words as they are actually drawn: translated (Hebrew often runs
    /// longer than the English) and at Easy Reading's size. A box measured once, when it was built,
    /// held the English at normal size and nothing else. Sets its LayoutElement if it has one,
    /// otherwise its own size, on the axes asked for.
    /// </summary>
    [DefaultExecutionOrder(10010)]   // after the label has translated and sized itself (LocText, 10000)
    public class FitsText : MonoBehaviour
    {
        TMP_Text _text;
        Vector2 _padding;
        bool _wide, _tall, _firm;
        float _minWidth, _minHeight;
        RectTransform _rect;
        LayoutElement _layout;

        /// <summary>
        /// Keeps <paramref name="box"/> as big as <paramref name="text"/> plus
        /// <paramref name="padding"/> (both sides together), never smaller than it is now. With
        /// <paramref name="firm"/>, its width is also its LayoutElement's minimum: a crowded row
        /// squeezes every child below the size it asks for, and a squeezed label spills.
        /// </summary>
        public static FitsText Add(Component box, TMP_Text text, Vector2 padding, bool wide = true, bool tall = false, bool firm = false)
        {
            if (box == null || text == null) return null;
            var f = box.GetComponent<FitsText>();
            if (f == null) f = box.gameObject.AddComponent<FitsText>();
            f._text = text;
            f._padding = padding;
            f._wide = wide;
            f._tall = tall;
            f._firm = firm;
            f._rect = (RectTransform)box.transform;
            f._layout = box.GetComponent<LayoutElement>();
            f._minWidth = f._layout != null ? Mathf.Max(0f, f._layout.preferredWidth) : f._rect.sizeDelta.x;
            f._minHeight = f._layout != null ? Mathf.Max(0f, f._layout.preferredHeight) : f._rect.sizeDelta.y;
            return f;
        }

        void LateUpdate()
        {
            if (_text == null) return;
            float width = _minWidth, height = _minHeight;
            if (_wide) width = Mathf.Max(_minWidth, _text.preferredWidth + _padding.x);
            if (_tall)
            {
                float room = (_wide ? width : _rect.rect.width) - _padding.x;
                height = Mathf.Max(_minHeight, _text.GetPreferredValues(_text.text, Mathf.Max(1f, room), 0f).y + _padding.y);
            }
            if (_layout != null)
            {
                if (_wide && !Mathf.Approximately(_layout.preferredWidth, width)) _layout.preferredWidth = width;
                if (_wide && _firm && !Mathf.Approximately(_layout.minWidth, width)) _layout.minWidth = width;
                if (_tall && !Mathf.Approximately(_layout.preferredHeight, height)) _layout.preferredHeight = height;
                return;
            }
            var size = _rect.sizeDelta;
            if (_wide) size.x = width;
            if (_tall) size.y = height;
            if (size != _rect.sizeDelta) _rect.sizeDelta = size;
        }
    }
}
