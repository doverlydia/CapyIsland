using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// "How should I talk to you?" — a boy or a girl. Hebrew says "you" differently to each
    /// ("הקש" / "הקישי"), so it asks; English doesn't, so the row is only there while Hebrew is
    /// the language (<see cref="Loc.Feminine"/>). On the opening screen, under the languages, and
    /// in Settings. Neither answer is lit until one is picked.
    /// </summary>
    public class AddressChoice : MonoBehaviour
    {
        readonly List<(bool feminine, Image pill, TextMeshProUGUI label)> _pills = new();
        Color _on, _off, _onText, _offText;

        /// <summary>
        /// The row: a question and two answers, in the colours of the screen it sits on; with
        /// <paramref name="card"/>, on a white card like the Settings rows around it.
        /// </summary>
        public static AddressChoice Build(RectTransform parent, float height, float fontSize, Color question, Color on, Color off, Color onText, Color offText, bool card = false)
        {
            var row = card ? UiKit.Image(parent, "Address", UiKit.Rounded(30), Color.white).rectTransform : UiKit.Rect("Address", parent);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            if (card) h.padding = new RectOffset(24, 24, 18, 18);
            RtlLayout.Add(h);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = card ? height + 36f : height;

            var choice = row.gameObject.AddComponent<AddressChoice>();
            choice._on = on; choice._off = off; choice._onText = onText; choice._offText = offText;

            var ask = UiKit.Text(row, "Question", "How should I talk to you?", fontSize, question, true, TextAlignmentOptions.Center);
            ask.textWrappingMode = TextWrappingModes.NoWrap;
            ask.overflowMode = TextOverflowModes.Overflow;
            ask.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            foreach (bool feminine in new[] { false, true })
            {
                bool f = feminine;
                var pill = UiKit.Image(row, f ? "Girl" : "Boy", UiKit.Rounded(30), off, true);
                var le = pill.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = height * 1.7f;
                le.preferredHeight = height;
                var t = UiKit.Text(pill.transform, "Label", f ? "As a girl" : "As a boy", fontSize, offText, true, TextAlignmentOptions.Center);
                UiKit.Stretch(t.rectTransform);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.overflowMode = TextOverflowModes.Overflow;
                var b = pill.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { Sfx.Play(SfxKind.Tap, 0.5f); Loc.SetFeminine(f); choice.Paint(); });
                pill.gameObject.AddComponent<PressSquash>();
                choice._pills.Add((f, pill, t));
            }
            choice.Paint();
            return choice;
        }

        void Awake() => Loc.Changed += Paint;

        void OnDestroy() => Loc.Changed -= Paint;

        /// <summary>Lit for the answer given; the whole row only while Hebrew is the language.</summary>
        void Paint()
        {
            if (this == null) return;
            gameObject.SetActive(Loc.Current == Language.Hebrew);
            foreach (var (feminine, pill, label) in _pills)
            {
                bool lit = Loc.AddressChosen && Loc.Feminine == feminine;
                pill.color = lit ? _on : _off;
                label.color = lit ? _onText : _offText;
            }
        }
    }
}
