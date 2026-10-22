using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Chat during a live visit: a speech-bubble button beside the emoji button, there only while
    /// you may chat (VisitChat: the host said yes, or it's a party). Tap it for a small panel with the last lines,
    /// a box to type in and Send. New lines also pop up over the speaker's head (EmojiBubbles),
    /// and a red count on the button says how many came while the panel was shut.
    ///
    /// On a phone the keyboard comes up over the bottom of the screen, so while you type the
    /// panel rides up to sit just above it, and gets shorter if it has to.
    ///
    /// A line from someone who isn't your friend shows as "…" with their name (VisitChat.Shown),
    /// and doesn't count toward the red number. After it, a small "Add friend" pill asks them to
    /// be friends (ChatAsk); once they say yes their lines read in full, here and from then on.
    /// </summary>
    public class ChatBar : MonoBehaviour
    {
        const float ButtonSize = 112f, Edge = 20f, EmojiBottom = 250f, Gap = 16f;
        const float PanelWidth = 860f, PanelTallest = 560f, PanelShortest = 250f, TopClear = 220f;

        /// <summary>
        /// The chat panel while it's open, else null. Speech bubbles (EmojiBubbles) keep above it:
        /// your own capybara stands in the middle of the screen, right where the panel sits.
        /// </summary>
        public static RectTransform OpenPanel { get; private set; }

        RectTransform _root, _buttonRoot, _panel, _lines;
        ScrollRect _scroll;
        TMP_InputField _box;
        TextMeshProUGUI _badge;
        GameObject _badgeDot;
        int _unread;
        bool _open;
        string _readable;   // which lines were drawn readable, so a new friend redraws the panel
        int _pinFrames;   // frames left to hold the list on its newest line while new rows settle
        float _panelHeight;
        readonly Vector3[] _corners = new Vector3[4];

        /// <summary>Made by the emoji button, right above it in the drawing order (and under every screen).</summary>
        public static ChatBar Create(Transform parent)
        {
            var root = UiKit.Rect("ChatBar", parent);
            UiKit.Stretch(root);
            var bar = root.gameObject.AddComponent<ChatBar>();
            bar._root = root;
            bar.Build();
            return bar;
        }

        void OnEnable()
        {
            VisitChat.Heard += OnHeard;
            VisitChat.Changed += OnChanged;
            RealFriends.Changed += OnFriendsChanged;
        }

        void OnDisable()
        {
            VisitChat.Heard -= OnHeard;
            VisitChat.Changed -= OnChanged;
            RealFriends.Changed -= OnFriendsChanged;
            if (OpenPanel == _panel) OpenPanel = null;
        }

        // ---------------------------------------------------------------- building

        void Build()
        {
            _buttonRoot = UiKit.Rect("ChatButton", _root);
            _buttonRoot.anchorMin = _buttonRoot.anchorMax = _buttonRoot.pivot = new Vector2(1f, 0f);
            _buttonRoot.sizeDelta = new Vector2(ButtonSize + 8f, ButtonSize + 8f);
            var button = UiKit.RoundButton(_buttonRoot, "Chat", IconPainter.Chat, null, UiKit.Spring, Toggle, ButtonSize);
            _badge = UiKit.Badge(button.transform, UiKit.Danger, 42f);
            _badgeDot = _badge.transform.parent.gameObject;
            _badgeDot.SetActive(false);
            _buttonRoot.gameObject.SetActive(false);
            Place();

            // The panel: a card along the bottom with a header, the lines, and a box to type in.
            var card = UiKit.Card(_root, "ChatPanel", UiKit.Surface, 36);
            _panel = card.rectTransform;
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = new Vector2(0.5f, 0f);

            var header = UiKit.Rect("Header", _panel);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(0f, 76f);
            header.anchoredPosition = Vector2.zero;
            var icon = UiKit.Image(header, "Icon", IconPainter.Chat, UiKit.Spring);
            icon.preserveAspect = true;
            UiKit.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(26f, -2f), new Vector2(48f, 48f));
            var title = UiKit.Text(header, "Title", "Chat", 36f, UiKit.Ink, true);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Stretch(title.rectTransform, 88f, 0f, 100f, 0f);
            var close = UiKit.Image(header, "Close", UiKit.Circle, UiKit.SurfaceSunk, true);
            UiKit.Place(close.rectTransform, new Vector2(1f, 0.5f), new Vector2(-18f, -2f), new Vector2(60f, 60f));
            var x = UiKit.Image(close.transform, "X", IconPainter.Close, UiKit.InkSoft);
            UiKit.Stretch(x.rectTransform, 16f, 16f, 16f, 16f);
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.onClick.AddListener(() => { Sfx.Play(SfxKind.Tap, 0.4f); Show(false); });
            close.gameObject.AddComponent<PressSquash>();

            // The lines, newest at the bottom.
            var well = UiKit.Image(_panel, "Lines", UiKit.Rounded(24), UiKit.SurfaceSunk);
            UiKit.Stretch(well.rectTransform, 18f, 18f + 84f + 12f, 18f, 76f);
            _scroll = UiKit.ScrollList(well.transform, "Scroll", out _lines, 6f, new RectOffset(16, 16, 12, 12));
            UiKit.Stretch((RectTransform)_scroll.transform);
            // A few lines sit at the top with room below; no bouncing a short list about.
            _scroll.movementType = ScrollRect.MovementType.Clamped;

            var row = UiKit.Rect("Say", _panel);
            row.anchorMin = new Vector2(0f, 0f);
            row.anchorMax = new Vector2(1f, 0f);
            row.pivot = new Vector2(0.5f, 0f);
            row.offsetMin = new Vector2(18f, 18f);
            row.offsetMax = new Vector2(-18f, 18f + 84f);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            _box = UiKit.Input(row, "Box", "Say something…", false, 84f);
            _box.characterLimit = VisitChat.MaxLength;
            _box.richText = false;
            _box.GetComponent<LayoutElement>().flexibleWidth = 1f;
            // What you type is drawn as typed: never swapped for a translation.
            var typed = _box.textComponent.GetComponent<LocText>();
            if (typed != null) { typed.NoTranslate = true; typed.NoRead = true; }
            _box.onSubmit.AddListener(_ => Send());
            var send = UiKit.Button(row, "Send", "Send", UiKit.ButtonStyle.Primary, Send, 28f);
            var sle = send.transform.parent.gameObject.AddComponent<LayoutElement>();
            sle.preferredWidth = 150f;
            sle.minWidth = 120f;

            _panel.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- every frame

        void Update()
        {
            bool can = VisitChat.CanChat;
            if (!can && _open) Show(false);
            bool showButton = can && !_open && !UiScreen.AnyOpen && VisitorBot.Current != null;
            if (_buttonRoot.gameObject.activeSelf != showButton) _buttonRoot.gameObject.SetActive(showButton);
            if (showButton) Place();
            if (_open)
            {
                FitPanel();
                if (Input.GetKeyDown(KeyCode.Escape) && !UiScreen.AnyOpen) Show(false);
            }
        }

        void LateUpdate()
        {
            if (!_open || _pinFrames <= 0) return;
            _pinFrames--;
            PinToEnd();
        }

        /// <summary>Above the emoji button standing up; beside it (to its left) lying down, where there's less height.</summary>
        void Place()
        {
            _buttonRoot.anchoredPosition = Core.ScreenMetrics.IsPortrait
                ? new Vector2(-Edge, EmojiBottom + ButtonSize + Gap)
                : new Vector2(-Edge - ButtonSize - Gap - 8f, EmojiBottom);
        }

        /// <summary>The panel's size and height off the bottom: clear of the keyboard while you type.</summary>
        void FitPanel()
        {
            var area = _root.rect;
            float lift = 0f;
            bool typing = _box.isFocused && (Application.isMobilePlatform || TouchScreenKeyboard.visible);
            if (typing)
            {
                // The keyboard is measured in screen pixels from the screen's bottom; this
                // panel's parent may start above that (the safe area), and in its own units.
                float keyboardPx = TouchScreenKeyboard.area.height > 1f ? TouchScreenKeyboard.area.height : Screen.height * 0.45f;
                _root.GetWorldCorners(_corners);
                var canvas = GetComponentInParent<Canvas>();
                var cam = canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
                float bottomPx = RectTransformUtility.WorldToScreenPoint(cam, _corners[0]).y;
                float topPx = RectTransformUtility.WorldToScreenPoint(cam, _corners[1]).y;
                float unitsPerPx = area.height / Mathf.Max(1f, topPx - bottomPx);
                lift = Mathf.Max(0f, (keyboardPx - bottomPx) * unitsPerPx);
            }
            float bottom = Gap + lift;
            float height = Mathf.Clamp(area.height - bottom - TopClear, PanelShortest, PanelTallest);
            float width = Mathf.Min(PanelWidth, area.width - 32f);
            _panel.sizeDelta = new Vector2(width, height);
            _panel.anchoredPosition = new Vector2(0f, bottom);
            // Shorter or taller (the keyboard came or went): the newest line stays in sight.
            if (!Mathf.Approximately(height, _panelHeight)) { _panelHeight = height; ScrollToEnd(); }
        }

        // ---------------------------------------------------------------- opening and talking

        void Toggle() => Show(!_open);

        void Show(bool open)
        {
            if (open && !VisitChat.CanChat) return;
            _open = open;
            _panel.gameObject.SetActive(open);
            OpenPanel = open ? _panel : null;
            if (!open)
            {
                if (_box != null) _box.DeactivateInputField();
                return;
            }
            _unread = 0;
            ShowUnread();
            // Sized first, then filled: the list is measured against the panel it will really have
            // (filling a panel still at its old size scrolled the first lines out of sight).
            FitPanel();
            Refill();
            UiKit.PopIn(_panel, 0.9f);
        }

        void Send()
        {
            if (_box == null) return;
            string text = _box.text;
            _box.text = "";
            if (!string.IsNullOrWhiteSpace(text))
            {
                VisitChat.Say(text);
                Sfx.Play(SfxKind.Plop, 0.4f);
            }
            // At a computer, keep typing; on a phone the keyboard goes when you send.
            if (!Application.isMobilePlatform) _box.ActivateInputField();
        }

        void OnHeard(VisitChat.Line line)
        {
            if (this == null) return;
            if (_open) { AddLine(line); ScrollToEnd(); }
            else if (!line.Mine && VisitChat.Readable(line))   // a stranger's "…" is nothing to read
            {
                _unread++;
                ShowUnread();
            }
        }

        void OnChanged()
        {
            if (this == null) return;
            if (!VisitChat.CanChat)
            {
                _unread = 0;
                ShowUnread();
            }
        }

        /// <summary>
        /// Someone said yes (or was unfriended): their lines are drawn afresh, readable or not.
        /// Only when that changed: the list also changes with every friend coming online.
        /// </summary>
        void OnFriendsChanged()
        {
            if (this == null || !_open || Readability() == _readable) return;
            Refill();
        }

        static string Readability()
        {
            var lines = VisitChat.Lines;
            var sb = new System.Text.StringBuilder(lines.Count);
            foreach (var l in lines) sb.Append(VisitChat.Readable(l) ? '1' : '0');
            return sb.ToString();
        }

        void ShowUnread()
        {
            _badgeDot.SetActive(_unread > 0);
            _badge.text = _unread > 9 ? "9+" : _unread.ToString();
        }

        /// <summary>The panel shows what's been said so far this visit.</summary>
        void Refill()
        {
            for (int i = _lines.childCount - 1; i >= 0; i--) Drop(_lines.GetChild(i));
            var lines = VisitChat.Lines;
            if (lines.Count == 0)
            {
                var empty = UiKit.Text(_lines, "Empty", "Nothing said yet. Say hello!", 26f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
                empty.overflowMode = TextOverflowModes.Overflow;
            }
            foreach (var l in lines) AddLine(l);
            _readable = Readability();
            ScrollToEnd();
        }

        void AddLine(VisitChat.Line line)
        {
            var empty = _lines.Find("Empty");
            if (empty != null) Drop(empty);
            // Only so many: the oldest go as new ones come.
            while (_lines.childCount >= 20) Drop(_lines.GetChild(0));

            string colour = line.Mine ? "D9661C" : "1F8A80";
            // Someone who isn't your friend: their name, but "…" for their words, and a pill to
            // ask them to be friends on the same line.
            string words = VisitChat.Readable(line) ? line.Text : "<color=#8A8A8A>" + VisitChat.Hidden + "</color>";
            Transform parent = _lines;
            bool ask = ChatAsk.CanAsk(line);
            if (ask)
            {
                var row = UiKit.Rect("Line", _lines);
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 14f;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = h.childForceExpandHeight = false;
                RtlLayout.Add(h);
                parent = row;
            }
            var t = UiKit.Text(parent, ask ? "Words" : "Line", "<color=#" + colour + "><b>" + line.Name + "</b></color>: " + words, 28f, UiKit.Ink);
            t.overflowMode = TextOverflowModes.Overflow;
            var loc = t.GetComponent<LocText>();
            if (loc != null) loc.NoTranslate = true;   // names and lines as the players typed them
            if (ask)
            {
                t.textWrappingMode = TextWrappingModes.NoWrap;
                AskChip.Create(parent, line.From, 22f, clickable: true);
            }
            if (_open) _readable = Readability();
        }

        /// <summary>Out of the list at once (so the count is right this frame), destroyed at the end of it.</summary>
        static void Drop(Transform row)
        {
            row.gameObject.SetActive(false);
            row.SetParent(null, false);
            Destroy(row.gameObject);
        }

        /// <summary>
        /// Shows the newest line: now, and again for a few frames, because a new row's height
        /// settles only once it has been drawn (the language, Easy Reading's letter size).
        /// </summary>
        void ScrollToEnd()
        {
            _pinFrames = 3;
            PinToEnd();
        }

        /// <summary>
        /// A short list starts at the top; a long one is scrolled so its last line sits at the
        /// bottom. Placed by hand rather than by ScrollRect's normalized position, which pushes a
        /// list shorter than its window up past the top and cut off the first lines.
        /// </summary>
        void PinToEnd()
        {
            if (_scroll == null || _lines == null || !_lines.gameObject.activeInHierarchy) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_lines);
            float view = _scroll.viewport != null ? _scroll.viewport.rect.height : ((RectTransform)_scroll.transform).rect.height;
            float hidden = Mathf.Max(0f, _lines.rect.height - view);
            _scroll.StopMovement();
            _lines.anchoredPosition = new Vector2(_lines.anchoredPosition.x, hidden);
        }
    }
}

namespace CapyIsland.UI
{
    /// <summary>
    /// Asking someone you met at a party to be friends, straight from the chat: the "Add friend"
    /// pill after their "…" line, or the chip on the "…" bubble over their head. The same friend
    /// request as tapping their face (RealFriends.Add with their friend code); they say yes on
    /// their side, and from then their lines read in full (VisitChat.Readable).
    /// </summary>
    public static class ChatAsk
    {
        static readonly HashSet<string> _asked = new();   // player ids asked this session

        /// <summary>Someone was asked (or the asking failed): pills and chips redraw.</summary>
        public static event System.Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void ResetSession()
        {
            _asked.Clear();
            Changed = null;
        }

        /// <summary>The real player behind this id on the live link, if they're still here.</summary>
        public static FriendProfile Who(string member)
        {
            if (string.IsNullOrEmpty(member)) return null;
            var bot = VisitorBot.ByNetId(member);
            if (bot != null && bot.Who != null && bot.Who.member == member) return bot.Who;
            foreach (var g in Game.Live.Guests) if (g != null && g.member == member) return g;
            return null;
        }

        /// <summary>A stranger's "…" line from someone real who's still here: they can be asked.</summary>
        public static bool CanAsk(VisitChat.Line line)
        {
            if (line == null || line.Mine || VisitChat.Readable(line) || RealFriends.IsBlocked(line.From)) return false;
            var who = Who(line.From);
            return who != null && who.IsReal;
        }

        /// <summary>Asked already, from the chat or anywhere else (and not answered yet)?</summary>
        public static bool Asked(string member)
        {
            if (string.IsNullOrEmpty(member)) return false;
            if (_asked.Contains(member)) return true;
            var who = Who(member);
            return who != null && !string.IsNullOrEmpty(who.code) && RealFriends.Asked(who.code);
        }

        /// <summary>Sends the friend request. The pill says "Asked" at once, and goes back if it couldn't be sent.</summary>
        public static async void Ask(string member)
        {
            if (Asked(member)) return;
            var who = Who(member);
            if (who == null || string.IsNullOrEmpty(who.code)) { Hints.Show("Couldn't find their friend code just now. Try again in a moment."); return; }
            _asked.Add(member);
            Sfx.Play(SfxKind.Plop, 0.45f);
            Changed?.Invoke();
            string problem = await RealFriends.Add(who.code);
            // "Asked already" and "friends already" leave the pill as it is; anything else lets you try again.
            bool sent = problem == null
                || problem == "You've asked already. They'll see it next time they play."
                || problem == "You're friends already!";
            if (!sent)
            {
                _asked.Remove(member);
                Changed?.Invoke();
            }
            Hints.Show(problem ?? Loc.F("Request sent to {0}. You'll be friends once they accept.", who.name));
        }
    }

    /// <summary>
    /// The little "Add friend" pill (a person with a plus, and the words), turning into a grey
    /// "Asked" with a tick once the request has gone. Keeps itself up to date (ChatAsk.Changed).
    /// </summary>
    public sealed class AskChip : MonoBehaviour
    {
        string _member;
        Image _back, _icon;
        TextMeshProUGUI _label;
        Button _button;

        /// <summary>
        /// A pill in a layout. <paramref name="clickable"/>: it is the button itself; otherwise
        /// whatever it sits on is (the speech bubble) and the pill only shows the state.
        /// </summary>
        public static AskChip Create(Transform parent, string member, float fontSize, bool clickable)
        {
            var back = UiKit.Image(parent, "AddFriend", UiKit.Rounded(20), UiKit.Spring, clickable);
            var h = back.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(Mathf.RoundToInt(fontSize * 0.55f), Mathf.RoundToInt(fontSize * 0.75f), 6, 6);
            h.spacing = 6f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            RtlLayout.Add(h);
            var icon = UiKit.Image(back.transform, "Icon", IconPainter.AddFriend, Color.white);
            icon.preserveAspect = true;
            var le = icon.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = le.minWidth = fontSize * 1.3f;
            var label = UiKit.Text(back.transform, "Label", "Add friend", fontSize, Color.white, true, TextAlignmentOptions.Center);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;

            var chip = back.gameObject.AddComponent<AskChip>();
            chip._member = member;
            chip._back = back;
            chip._icon = icon;
            chip._label = label;
            if (clickable)
            {
                chip._button = back.gameObject.AddComponent<Button>();
                chip._button.transition = Selectable.Transition.None;
                chip._button.onClick.AddListener(() => ChatAsk.Ask(member));
                back.gameObject.AddComponent<PressSquash>();
            }
            chip.Refresh();
            return chip;
        }

        void OnEnable()
        {
            ChatAsk.Changed += Refresh;
            if (_back != null) Refresh();
        }

        void OnDisable() => ChatAsk.Changed -= Refresh;

        public void Refresh()
        {
            if (this == null || _back == null) return;
            bool asked = ChatAsk.Asked(_member);
            _back.color = asked ? new Color(0f, 0f, 0f, 0.07f) : UiKit.Spring;
            _icon.sprite = asked ? IconPainter.Check : IconPainter.AddFriend;
            _icon.color = asked ? UiKit.Spring : Color.white;
            _label.text = asked ? "Asked" : "Add friend";
            _label.color = asked ? UiKit.InkSoft : Color.white;
            if (_button != null) _button.interactable = !asked;
        }
    }
}
