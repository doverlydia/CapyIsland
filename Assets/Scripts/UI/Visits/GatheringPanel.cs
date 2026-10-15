using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// While a party or contest is on (Gathering), a card under the goal card says what's
    /// happening: who's here, the clock, the top of the scoreboard and your place. The host gets
    /// Start (once two are here), Again after a round, and End; a guest gets Leave.
    ///
    /// Step out of a running contest's ring and a big countdown says to come back. When a round
    /// ends, everyone sees the leaderboard (ContestResults).
    /// </summary>
    public class GatheringPanel : MonoBehaviour
    {
        const float Width = 620f;

        RectTransform _card, _board, _buttons, _warning;
        Image _disc, _icon;
        TextMeshProUGUI _caption, _line, _warnText;
        string _shown;
        float _next;

        public static GatheringPanel Create(Transform parent)
        {
            var root = UiKit.Rect("Gathering", parent);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 1f);
            // Under the goal card (ObjectiveTracker: beside your badge, 124 tall at -104).
            root.anchoredPosition = new Vector2(16f + ProfileBadge.Size + 12f, -104f - 124f - 14f);
            root.sizeDelta = new Vector2(Width, 10f);
            var panel = root.gameObject.AddComponent<GatheringPanel>();
            panel.Build(parent);
            return panel;
        }

        void Build(Transform hud)
        {
            var card = UiKit.Card(transform, "Card", UiKit.Surface, 30);
            _card = card.rectTransform;
            _card.anchorMin = _card.anchorMax = _card.pivot = new Vector2(0f, 1f);
            _card.anchoredPosition = Vector2.zero;
            _card.sizeDelta = new Vector2(Width, 10f);
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(18, 18, 14, 16);
            v.spacing = 10f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var top = UiKit.Rect("Top", card.transform);
            var th = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            th.spacing = 14f;
            th.childAlignment = TextAnchor.MiddleLeft;
            th.childControlWidth = th.childControlHeight = true;
            th.childForceExpandWidth = th.childForceExpandHeight = false;
            RtlLayout.Add(th);
            _disc = UiKit.Image(top, "Disc", UiKit.Circle, UiKit.Sun);
            var dle = _disc.gameObject.AddComponent<LayoutElement>();
            dle.preferredWidth = dle.preferredHeight = dle.minWidth = 64f;
            _icon = UiKit.Image(_disc.transform, "Icon", IconPainter.Trophy, Color.white);
            _icon.preserveAspect = true;
            UiKit.Stretch(_icon.rectTransform, 12f, 12f, 12f, 12f);
            var words = UiKit.Rect("Words", top);
            var wv = words.gameObject.AddComponent<VerticalLayoutGroup>();
            wv.spacing = 2f;
            wv.childControlWidth = wv.childControlHeight = true;
            wv.childForceExpandWidth = true;
            wv.childForceExpandHeight = false;
            words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            _caption = UiKit.Text(words, "Caption", "", 22f, UiKit.Spring, true);
            _caption.characterSpacing = 3f;
            _caption.textWrappingMode = TextWrappingModes.NoWrap;
            _line = UiKit.Text(words, "Line", "", 26f, UiKit.Ink, true);
            _line.overflowMode = TextOverflowModes.Overflow;

            _board = UiKit.Rect("Board", card.transform);
            var bv = _board.gameObject.AddComponent<VerticalLayoutGroup>();
            bv.spacing = 2f;
            bv.childControlWidth = bv.childControlHeight = true;
            bv.childForceExpandWidth = true;
            bv.childForceExpandHeight = false;

            _buttons = UiKit.Rect("Buttons", card.transform);
            var bh = _buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            bh.spacing = 12f;
            bh.childControlWidth = bh.childControlHeight = true;
            bh.childForceExpandWidth = true;
            bh.childForceExpandHeight = false;

            // The come-back countdown, big in the middle of the screen.
            var warn = UiKit.Card(hud, "OutWarning", UiKit.Hex("FFE3D9"), 36);
            _warning = warn.rectTransform;
            _warning.anchorMin = _warning.anchorMax = _warning.pivot = new Vector2(0.5f, 0.66f);
            _warning.sizeDelta = new Vector2(760f, 170f);
            warn.raycastTarget = false;
            _warnText = UiKit.Text(warn.transform, "Text", "", 40f, UiKit.Danger, true, TextAlignmentOptions.Center);
            UiKit.Stretch(_warnText.rectTransform, 24f, 12f, 24f, 12f);
            _warnText.overflowMode = TextOverflowModes.Overflow;
            _warning.gameObject.SetActive(false);

            Gathering.Changed += OnChanged;
            Gathering.Finished += OnFinished;
            Refresh(force: true);
        }

        void OnDestroy()
        {
            Gathering.Changed -= OnChanged;
            Gathering.Finished -= OnFinished;
            if (_warning != null) Destroy(_warning.gameObject);
        }

        void OnChanged() => Refresh(force: true);

        static void OnFinished(List<Gathering.Player> board) => ContestResults.Show(Gathering.Kind, board, Live.MyId);

        void Update()
        {
            Warning();
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            Refresh(force: false);
        }

        /// <summary>Out of the ring with the clock running: come back!</summary>
        void Warning()
        {
            bool show = Gathering.OutAt > 0f && Gathering.Now == Gathering.Phase.Running && !UiScreen.AnyOpen;
            if (_warning.gameObject.activeSelf != show)
            {
                _warning.gameObject.SetActive(show);
                if (show) UiKit.PopIn(_warning, 0.8f);
            }
            if (!show) return;
            int left = Mathf.Max(0, Mathf.CeilToInt(Gathering.OutAt - Time.realtimeSinceStartup));
            _warnText.text = Loc.F("Come back to the contest area! {0}", left) + "\n<size=70%>" + Loc.T("Stay out and you're out of this round.") + "</size>";
        }

        void Refresh(bool force)
        {
            bool on = Gathering.Active || Gathering.JoiningSomething;
            if (_card.gameObject.activeSelf != on) _card.gameObject.SetActive(on);
            if (!on) { _shown = null; return; }

            var kind = Gathering.Kind;
            _disc.color = kind switch
            {
                GatheringKind.Fishing => UiKit.Hex("2DA39A"),
                GatheringKind.Bugs => UiKit.Hex("6BAA4E"),
                GatheringKind.Birds => UiKit.Hex("5B9BD5"),
                _ => UiKit.Hex("E58FB0"),
            };
            _icon.sprite = kind switch
            {
                GatheringKind.Fishing => IconPainter.Rod,
                GatheringKind.Bugs => HobbyData.Art("Bug_Butterfly") ?? IconPainter.Sparkle,
                GatheringKind.Birds => IconPainter.Bird,
                _ => IconPainter.Star,
            };

            string caption = Loc.T(Gathering.NameOf(kind)).ToUpperInvariant();
            if (Gathering.Now == Gathering.Phase.Running)
            {
                int s = Mathf.CeilToInt(Gathering.SecondsLeft);
                caption += " · " + (s / 60) + ":" + (s % 60).ToString("00");
            }
            _caption.text = caption;
            _caption.color = Color.Lerp(_disc.color, UiKit.Ink, 0.15f);

            // The rest only changes with the state: rebuilt when it does.
            string key = Gathering.Now + "|" + Gathering.Hosting + "|" + Gathering.Joined + "|" + Gathering.Present() + "|" + BoardKey();
            if (!force && key == _shown) return;
            _shown = key;
            _line.text = Line();
            BuildBoard();
            BuildButtons();
        }

        static string BoardKey()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var p in Gathering.Board) sb.Append(p.id).Append(p.score).Append(p.isOut).Append(p.gone);
            return sb.ToString();
        }

        static string Line()
        {
            if (Gathering.JoiningSomething && !Gathering.Joined) return Loc.F("On the way to {0}'s island…", Gathering.HostName);
            int here = Gathering.Present();
            if (!Gathering.IsContest)
                return Gathering.Hosting
                    ? (here <= 1 ? Loc.T("Waiting for your guests…") : Loc.F("{0} guests here", here - 1))
                    : Loc.F("At {0}'s party", Gathering.HostName);
            switch (Gathering.Now)
            {
                case Gathering.Phase.Running:
                {
                    var board = Gathering.Board;
                    int place = board.FindIndex(p => p.id == Live.MyId) + 1;
                    var me = board.Find(p => p.id == Live.MyId);
                    if (me == null || !me.playing) return Loc.T("Watching this round: you'll be in the next one.");
                    if (me.isOut) return Loc.T("You're out of this round. You keep what you caught!");
                    return Loc.F("You: {0} · place {1}", Loc.T(Gathering.Counted(Gathering.Kind, me.score)), place);
                }
                case Gathering.Phase.Results:
                    return Gathering.Hosting ? Loc.T("Round over! Play again, or end the contest.") : Loc.T("Round over! Waiting to see if there's another.");
                default:
                    if (!Gathering.Hosting) return Loc.F("Waiting for {0} to start…", Gathering.HostName);
                    return here < 2 ? Loc.T("Waiting for players to arrive…") : Loc.F("{0} here: press Start when you're ready!", here);
            }
        }

        /// <summary>The top three, and you if you're further down.</summary>
        void BuildBoard()
        {
            for (int i = _board.childCount - 1; i >= 0; i--) Destroy(_board.GetChild(i).gameObject);
            bool show = Gathering.Joined || Gathering.Hosting;
            if (!show) return;
            var board = Gathering.Board;
            if (!Gathering.IsContest || Gathering.Now == Gathering.Phase.Lobby)
            {
                // Who's here: names in a row.
                var names = new List<string>();
                foreach (var p in board) if (!p.gone) names.Add(p.id == Live.MyId ? Loc.T("You") : p.name);
                if (names.Count == 0) return;
                var t = UiKit.Text(_board, "Here", string.Join(", ", names), 22f, UiKit.InkSoft);
                t.overflowMode = TextOverflowModes.Ellipsis;
                t.textWrappingMode = TextWrappingModes.Normal;
                t.maxVisibleLines = 2;
                return;
            }
            int mine = board.FindIndex(p => p.id == Live.MyId);
            for (int i = 0; i < board.Count; i++)
            {
                if (i >= 3 && i != mine) continue;
                var p = board[i];
                if (!p.playing) continue;
                bool me = p.id == Live.MyId;
                string who = me ? Loc.T("You") : p.name;
                string tag = p.gone ? " · " + Loc.T("left") : p.isOut ? " · " + Loc.T("out") : "";
                var row = UiKit.Text(_board, "Row", (i + 1) + ". " + who + tag + "   <b>" + p.score + "</b>", 24f, me ? UiKit.PrimaryDark : UiKit.Ink, me);
                row.textWrappingMode = TextWrappingModes.NoWrap;
                row.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        void BuildButtons()
        {
            for (int i = _buttons.childCount - 1; i >= 0; i--) Destroy(_buttons.GetChild(i).gameObject);
            if (Gathering.Hosting)
            {
                if (Gathering.IsContest && Gathering.Now != Gathering.Phase.Running)
                {
                    bool ready = Gathering.Present() >= 2;
                    var start = Small(Gathering.Now == Gathering.Phase.Results ? "Again" : "Start", UiKit.ButtonStyle.Primary, Gathering.Start);
                    start.interactable = ready;
                }
                Small(Gathering.IsContest ? "End contest" : "End party", UiKit.ButtonStyle.Secondary, AskEnd);
            }
            else if (Gathering.Joined || Gathering.JoiningSomething)
                Small("Leave", UiKit.ButtonStyle.Secondary, Leave);
        }

        Button Small(string label, UiKit.ButtonStyle style, System.Action onClick)
        {
            var b = UiKit.Button(_buttons, label, label, style, onClick, 26f);
            b.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;
            return b;
        }

        static void AskEnd()
        {
            PromptScreen.Ask(Gathering.IsContest ? "End contest?" : "End party?", null,
                Loc.T("Everyone heads home, and your island closes to visitors."), "End it", "Not yet", Gathering.EndHosting);
        }

        static void Leave()
        {
            PromptScreen.Ask("Leave?", null, Loc.F("Fly home from {0}'s island?", Gathering.HostName), "Fly home", "Stay", Visit.FlyHome);
        }
    }
}
