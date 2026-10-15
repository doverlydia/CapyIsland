using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The end of a contest round: who won, and everyone's catch, best first (anyone who left the
    /// ring or flew home at the bottom). Bragging rights only: what you caught is yours anyway.
    /// </summary>
    public class ContestResults : MonoBehaviour
    {
        const string ObjectName = "[ContestResults]";
        static readonly Color Gold = UiKit.Hex("E8B23A"), Silver = UiKit.Hex("AEB9C4"), Bronze = UiKit.Hex("CD8B55");

        public static void Show(GatheringKind kind, List<Gathering.Player> board, string me)
        {
            var old = GameObject.Find(ObjectName);
            if (old != null) Destroy(old);
            var go = new GameObject(ObjectName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 470;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<ContestResults>().Build(kind, board, me);
        }

        void Build(GatheringKind kind, List<Gathering.Player> board, string me)
        {
            var root = UiKit.Rect("Root", transform);
            UiKit.Stretch(root);
            var scrim = UiKit.Image(root, "Scrim", UiKit.Rounded(4), UiKit.Scrim, true);
            UiKit.Stretch(scrim.rectTransform);

            var card = UiKit.Card(root, "Card", UiKit.Surface, 40);
            var cr = card.rectTransform;
            cr.anchorMin = new Vector2(0.06f, 0.5f);
            cr.anchorMax = new Vector2(0.94f, 0.5f);
            cr.pivot = new Vector2(0.5f, 0.5f);
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(36, 36, 34, 34);
            v.spacing = 12f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var trophy = UiKit.Image(card.transform, "Trophy", IconPainter.Trophy, Gold);
            trophy.preserveAspect = true;
            trophy.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            trophy.transform.DOPunchScale(new Vector3(0.25f, 0.25f, 0f), 0.6f, 6, 0.5f).SetUpdate(true);

            var title = UiKit.Text(card.transform, "Title", Loc.T(Gathering.NameOf(kind)), 46f, UiKit.Ink, true, TextAlignmentOptions.Center);
            title.overflowMode = TextOverflowModes.Overflow;

            var playing = board.FindAll(p => p.playing);
            bool InIt(Gathering.Player p) => !p.gone && !p.isOut;
            var top = playing.Count > 0 && InIt(playing[0]) && playing[0].score > 0 ? playing[0] : null;
            // Level at the top: nobody wins outright.
            bool tie = top != null && playing.Count > 1 && InIt(playing[1]) && playing[1].score == top.score;
            var winner = tie ? null : top;
            bool iWon = winner != null && winner.id == me;
            string headline = top == null ? Loc.T("Nobody caught anything this time!")
                : tie ? Loc.T("It's a tie at the top!")
                : iWon ? Loc.T("You won!")
                : Loc.F("{0} wins!", winner.name);
            var head = UiKit.Text(card.transform, "Winner", headline, 36f, iWon ? UiKit.PrimaryDark : UiKit.Ink, true, TextAlignmentOptions.Center);
            head.overflowMode = TextOverflowModes.Overflow;
            if (iWon) Sfx.Play(SfxKind.Chest, 1f);

            for (int i = 0; i < playing.Count && i < 10; i++) Row(card.transform, playing[i], i, kind, playing[i].id == me);

            var note = UiKit.Text(card.transform, "Note", "Everyone keeps what they caught.", 24f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            note.overflowMode = TextOverflowModes.Overflow;

            var row = UiKit.Rect("Buttons", card.transform);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 92f;
            UiKit.Button(row, "Close", "Close", UiKit.ButtonStyle.Primary, () => Destroy(gameObject), 32f);
            UiKit.PopIn(card.transform, 0.85f);
        }

        static void Row(Transform parent, Gathering.Player p, int index, GatheringKind kind, bool me)
        {
            var row = UiKit.Image(parent, "Row", UiKit.Rounded(24), me ? UiKit.Hex("FFE7A3") : Color.white).rectTransform;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(14, 20, 8, 8);
            h.spacing = 14f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().minHeight = 70f;
            RtlLayout.Add(h);

            bool placed = !p.gone && !p.isOut;
            var disc = UiKit.Image(row, "Place", UiKit.Circle, placed && index < 3 ? (index == 0 ? Gold : index == 1 ? Silver : Bronze) : UiKit.SurfaceSunk);
            var dle = disc.gameObject.AddComponent<LayoutElement>();
            dle.preferredWidth = dle.preferredHeight = dle.minWidth = 48f;
            var n = UiKit.Text(disc.transform, "N", (index + 1).ToString(), 24f, placed && index < 3 ? Color.white : UiKit.InkSoft, true, TextAlignmentOptions.Center);
            UiKit.Stretch(n.rectTransform);

            string name = me ? Loc.T("You") : p.name;
            string tag = p.gone ? Loc.T("flew home") : p.isOut ? Loc.T("left the area") : null;
            var label = UiKit.Text(row, "Name", tag != null ? name + "  <size=75%><color=#A89A8C>" + tag + "</color></size>" : name, 30f, UiKit.Ink, me);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var score = UiKit.Text(row, "Score", Loc.T(Gathering.Counted(kind, p.score)), 30f, UiKit.PrimaryDark, true, TextAlignmentOptions.Right);
            score.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }
}
