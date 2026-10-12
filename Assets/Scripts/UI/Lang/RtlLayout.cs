using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Mirrors a row in a right-to-left language: icon, label and switch run the other way, so a
    /// Hebrew label sits beside its icon at the right edge where the eye starts.
    /// </summary>
    [RequireComponent(typeof(HorizontalLayoutGroup))]
    public class RtlLayout : MonoBehaviour
    {
        HorizontalLayoutGroup _row;
        TextAnchor _authored;

        public static void Add(Component row)
        {
            if (row != null && row.GetComponent<RtlLayout>() == null) row.gameObject.AddComponent<RtlLayout>();
        }

        void Awake()
        {
            _row = GetComponent<HorizontalLayoutGroup>();
            _authored = _row.childAlignment;
        }

        void OnEnable()
        {
            Loc.Changed += Apply;
            Apply();
        }

        void OnDisable() => Loc.Changed -= Apply;

        void Apply()
        {
            bool rtl = Loc.Rtl;
            _row.reverseArrangement = rtl;
            _row.childAlignment = rtl ? Mirror(_authored) : _authored;
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }

        static TextAnchor Mirror(TextAnchor a) => a switch
        {
            TextAnchor.UpperLeft => TextAnchor.UpperRight,
            TextAnchor.UpperRight => TextAnchor.UpperLeft,
            TextAnchor.MiddleLeft => TextAnchor.MiddleRight,
            TextAnchor.MiddleRight => TextAnchor.MiddleLeft,
            TextAnchor.LowerLeft => TextAnchor.LowerRight,
            TextAnchor.LowerRight => TextAnchor.LowerLeft,
            _ => a,
        };
    }
}
