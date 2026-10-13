using System;
using DG.Tweening;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Friends and flights: hop to a random islander's island, visit a friend (their island is
    /// kept and keeps growing), or meet today's suggested islanders.
    /// </summary>
    public class FriendsScreen : UiScreen
    {
        public static FriendsScreen Instance { get; private set; }

        protected override string Title => "Neighbours";
        protected override Sprite TitleIcon => IconPainter.Plane;
        protected override Color AccentColour => UiKit.Hex("5B9BD5");

        RectTransform _list;
        ScrollRect _scroll;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override void Build(RectTransform body)
        {
            _scroll = UiKit.ScrollList(body, "List", out _list, 14f, new RectOffset(4, 4, 8, 32));
            UiKit.Stretch((RectTransform)_scroll.transform);
        }

        string _blocked;          // why you can't fly yet, or null
        RectTransform _fixCard;   // the card that fixes it, bounced when a locked Visit is tapped

        protected override void OnOpen()
        {
            NewStuff.Clear("friends/");   // new friends are on show
            ClearChildren(_list);
            _blocked = Visit.WhyCantFly();
            _fixCard = null;

            if (Visit.Active)
            {
                var home = UiKit.Button(_list, "Home", "Fly home", UiKit.ButtonStyle.Primary, () => { Close(); Visit.FlyHome(); }, 40f, IconPainter.House);
                home.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 110f;
            }

            if (!Airport.Built && !Visit.Active) AirstripCard();
            else if (_blocked != null) Notice(_blocked);
            ServerMap();

            SectionLabel(_list, "Your friends");
            if (Friends.All.Count == 0)
            {
                var empty = UiKit.Text(_list, "Empty", "No friends yet. Visit someone on the server above, and add them from their island!", 28f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
                empty.overflowMode = TextOverflowModes.Overflow;
            }
            // Online friends first.
            var friends = new System.Collections.Generic.List<FriendProfile>(Friends.All);
            friends.Sort((x, y) => Server.IsOnline(y).CompareTo(Server.IsOnline(x)));
            foreach (var f in friends) Row(f, friend: true);

            _scroll.verticalNormalizedPosition = 1f;
        }

        void Row(FriendProfile who, bool friend)
        {
            var row = UiKit.Image(_list, who.name, UiKit.Rounded(30), Color.white).rectTransform;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(18, 18, 16, 16);
            h.spacing = 18f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().minHeight = 128f;

            UiKit.Avatar(row, who, 96f);

            var col = UiKit.Rect("Column", row);
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var name = UiKit.Text(col, "Name", who.name, 34f, UiKit.Ink, true);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            bool online = Server.IsOnline(who);
            string detail = (online ? "<color=#5DBB63>Online</color>" : "<color=#A89A8C>Offline</color>") + " · Island level " + who.level;
            if (friend) detail += " · " + Ago(who.LastVisit);
            if (Visit.Active && Visit.Host.id == who.id) detail = "You're here now";
            UiKit.Text(col, "Detail", detail, 26f, UiKit.InkSoft);

            if (!friend)
            {
                var add = UiKit.Button(row, "Add", null, UiKit.ButtonStyle.Secondary, () => { Friends.Add(who); OnOpen(); }, 28f, IconPainter.AddFriend);
                var ale = add.transform.parent.gameObject.AddComponent<LayoutElement>();
                ale.preferredWidth = 84f; ale.preferredHeight = 72f;
            }
            if (friend)
            {
                var drop = UiKit.Button(row, "Unfriend", null, UiKit.ButtonStyle.Secondary, () => Unfriend(who), 26f, IconPainter.Close);
                var dle = drop.transform.parent.gameObject.AddComponent<LayoutElement>();
                dle.preferredWidth = 76f; dle.preferredHeight = 72f;
            }
            bool here = Visit.Active && Visit.Host.id == who.id;
            if (!here)
            {
                var go = _blocked == null
                    ? UiKit.Button(row, "Visit", "Visit", UiKit.ButtonStyle.Primary, () => { Close(); Visit.Fly(who); }, 28f, IconPainter.Plane)
                    : UiKit.Button(row, "Visit", "Visit", UiKit.ButtonStyle.Secondary, Blocked, 28f, IconPainter.Lock);
                if (_blocked != null) Greyed(go);
                var le = go.transform.parent.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 170f; le.preferredHeight = 72f;
            }
        }

        /// <summary>Asks first, then removes a friend (and sends them home if they are visiting).</summary>
        void Unfriend(FriendProfile who)
        {
            PromptScreen.Ask("Unfriend?", who, $"Remove {who.name} from your friends? Their island won't be kept any more.", "Unfriend", "Cancel", () =>
            {
                Friends.Remove(who);
                var bot = VisitorBot.Current;
                if (bot != null && bot.Who.id == who.id && bot.As == VisitorBot.Role.Guest) bot.SendHome();
                Hints.Show(who.name + " is no longer your friend.");
                OnOpen();
            });
        }

        /// <summary>A locked Visit was tapped: say why, right here, and point at the fix.</summary>
        void Blocked()
        {
            Hints.Show(_blocked);
            Game.Sfx.Play(Game.SfxKind.Tap, 0.4f);
            if (_fixCard == null) return;
            // The card explaining it is at the top of the list: bring it into view.
            if (_scroll != null) DOTween.To(() => _scroll.verticalNormalizedPosition, v => _scroll.verticalNormalizedPosition = v, 1f, 0.35f).SetUpdate(true);
            _fixCard.DOKill();
            _fixCard.localScale = Vector3.one;
            _fixCard.DOPunchScale(new Vector3(0.06f, 0.06f, 0f), 0.45f, 6, 0.6f).SetUpdate(true);
        }

        /// <summary>Visit buttons you can't use yet look it: grey, with a lock.</summary>
        static void Greyed(Button button)
        {
            var face = button.GetComponent<Image>();
            if (face != null) face.color = UiKit.Hex("D9D4CC");
            foreach (var g in button.GetComponentsInChildren<Graphic>())
                if (g != face) g.color = UiKit.Hex("9A9187");
        }

        /// <summary>A note at the top saying what to do before you can fly.</summary>
        void Notice(string line)
        {
            var card = UiKit.Image(_list, "Notice", UiKit.Rounded(32), UiKit.Hex("FFF1D6")).rectTransform;
            var h = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(20, 20, 18, 18);
            h.spacing = 16f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            var icon = UiKit.Image(card, "Icon", IconPainter.Lock, UiKit.Hex("C98A2B"));
            icon.preserveAspect = true;
            var ile = icon.gameObject.AddComponent<LayoutElement>();
            ile.preferredWidth = ile.preferredHeight = 56f;
            var t = UiKit.Text(card, "Line", line, 28f, UiKit.Ink, true);
            t.overflowMode = TextOverflowModes.Overflow;
            t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            _fixCard = card;
        }

        /// <summary>Until you have an airstrip, the way to build one.</summary>
        void AirstripCard()
        {
            var card = UiKit.Image(_list, "Airstrip", UiKit.Rounded(32), UiKit.Hex("FFF1D6")).rectTransform;
            _fixCard = card;
            var h = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(20, 20, 18, 18);
            h.spacing = 18f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            var pic = UiKit.Image(card, "Picture", HobbyData.Art("Airport") ?? IconPainter.Plane, HobbyData.Art("Airport") != null ? Color.white : UiKit.Hex("5B9BD5"));
            pic.preserveAspect = true;
            var ple = pic.gameObject.AddComponent<LayoutElement>();
            ple.preferredWidth = ple.preferredHeight = 110f;
            var col = UiKit.Rect("Column", card);
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            UiKit.Text(col, "Title", "Build an airstrip", 32f, UiKit.Ink, true);
            var t = UiKit.Text(col, "Line", $"You need one to fly to your neighbours. {Airport.CostWood} wood and {Airport.CostGold} gold.", 26f, UiKit.InkSoft);
            t.overflowMode = TextOverflowModes.Overflow;
            var build = UiKit.Button(card, "Build", "Build", UiKit.ButtonStyle.Primary, () =>
            {
                var why = Airport.Build();
                if (why != null) { Hints.Show(why); return; }
                Close();
                Hints.Show("Here's your airstrip! Pop the bubble, then drag it wherever you'd like it to stand.");
            }, 28f);
            var ble = build.transform.parent.gameObject.AddComponent<LayoutElement>();
            ble.preferredWidth = 150f; ble.preferredHeight = 72f;
        }

        // ---------------------------------------------------------------- the server

        /// <summary>The ten island slots of this server: you, your online friends, strangers, and empty water.</summary>
        void ServerMap()
        {
            var head = UiKit.Image(_list, "Server", UiKit.Rounded(32), UiKit.Hex("DDF0F5")).rectTransform;
            var v = head.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(20, 20, 18, 20);
            v.spacing = 12f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var top = UiKit.Rect("Top", head);
            var th = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            th.childAlignment = TextAnchor.MiddleLeft;
            th.spacing = 12f;
            th.childControlWidth = th.childControlHeight = true;
            th.childForceExpandWidth = th.childForceExpandHeight = false;
            var col = UiKit.Rect("Column", top);
            var cv = col.gameObject.AddComponent<VerticalLayoutGroup>();
            cv.childControlWidth = cv.childControlHeight = true;
            cv.childForceExpandWidth = true;
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var name = UiKit.Text(col, "Name", Server.Name, 32f, UiKit.Ink, true);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Text(col, "Count", $"{Server.Online} of {Server.Slots} islands online", 26f, UiKit.InkSoft);
            var swap = UiKit.Button(top, "Switch", "Switch server", UiKit.ButtonStyle.Secondary, () => { Server.Reconnect(); OnOpen(); }, 24f);
            var sle = swap.transform.parent.gameObject.AddComponent<LayoutElement>();
            sle.preferredWidth = 220f; sle.preferredHeight = 64f;

            var grid = UiKit.Rect("Slots", head);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            float width = Sheet.sizeDelta.x - 64f - 8f - 40f;
            float cell = (width - 12f * 4f) / 5f;
            g.cellSize = new Vector2(cell, cell * 1.32f);
            g.spacing = new Vector2(12f, 12f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 5;
            grid.gameObject.AddComponent<LayoutElement>().preferredHeight = 2f * cell * 1.32f + 12f;

            var bySlot = new Server.Seat[Server.Slots];
            foreach (var seat in Server.Seats) bySlot[seat.Slot] = seat;
            for (int i = 0; i < Server.Slots; i++) Slot(grid, bySlot[i], cell);

            var hint = UiKit.Text(head, "Hint", "Tap an island to fly there. Lend a hand, and add the ones you like as friends.", 24f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            hint.overflowMode = TextOverflowModes.Overflow;
        }

        void Slot(RectTransform grid, Server.Seat seat, float cell)
        {
            bool empty = seat == null;
            var tile = UiKit.Image(grid, "Slot", UiKit.Rounded(24), empty ? new Color(1f, 1f, 1f, 0.35f) : Color.white, !empty);

            // A little island: a ring of sand, grass, and whoever lives there.
            var sand = UiKit.Image(tile.transform, "Sand", UiKit.Circle, empty ? new Color(0.6f, 0.75f, 0.8f, 0.4f) : UiKit.Hex("E0C99A"));
            sand.rectTransform.anchorMin = sand.rectTransform.anchorMax = new Vector2(0.5f, 0.62f);
            sand.rectTransform.sizeDelta = new Vector2(cell * 0.78f, cell * 0.78f);
            if (!empty)
            {
                var grass = UiKit.Image(sand.transform, "Grass", UiKit.Circle, UiKit.Hex("7FAF4F"));
                UiKit.Stretch(grass.rectTransform, cell * 0.06f, cell * 0.06f, cell * 0.06f, cell * 0.06f);
                if (seat.IsYou)
                {
                    var pic = PlayerFace();
                    if (pic != null)
                    {
                        var me = UiKit.Image(grass.transform, "You", pic, Color.white);
                        me.preserveAspect = true;
                        UiKit.Stretch(me.rectTransform, cell * 0.06f, cell * 0.04f, cell * 0.06f, cell * 0.08f);
                    }
                }
                else
                {
                    var av = UiKit.Avatar(grass.transform, seat.Who, cell * 0.6f);
                    av.anchorMin = av.anchorMax = new Vector2(0.5f, 0.5f);
                    av.anchoredPosition = Vector2.zero;
                    av.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
                    if (Friends.IsFriend(seat.Who.id))
                    {
                        var heart = UiKit.Image(tile.transform, "Friend", IconPainter.Heart, UiKit.Danger);
                        UiKit.Place(heart.rectTransform, new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(30f, 30f));
                    }
                }
            }

            string label = empty ? "Empty" : seat.IsYou ? PlayerProfile.Name : seat.Who.name;
            var t = UiKit.Text(tile.transform, "Name", label, 22f, empty ? UiKit.InkSoft : UiKit.Ink, !empty, TextAlignmentOptions.Center);
            t.rectTransform.anchorMin = new Vector2(0f, 0f);
            t.rectTransform.anchorMax = new Vector2(1f, 0.3f);
            t.rectTransform.offsetMin = new Vector2(4f, 4f);
            t.rectTransform.offsetMax = new Vector2(-4f, 0f);
            t.enableAutoSizing = true;
            t.fontSizeMin = 13f;
            t.fontSizeMax = 22f;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            if (empty || seat.IsYou) return;

            bool here = Visit.Active && Visit.Host.id == seat.Who.id;
            var who = seat.Who;
            var b = tile.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            if (_blocked != null && !here)
            {
                // You can see who is about, but not fly there yet.
                tile.gameObject.AddComponent<CanvasGroup>().alpha = 0.6f;
                b.onClick.AddListener(Blocked);
            }
            else b.onClick.AddListener(() =>
            {
                Close();
                if (!here) Visit.Fly(who);
            });
            tile.gameObject.AddComponent<PressSquash>();
        }

        static Sprite PlayerFace()
        {
            if (Wardrobe.Ready) return Wardrobe.BaseSprite;
            var king = FindFirstObjectByType<PlayerAvatar>();
            var body = king != null ? king.transform.Find("Sprite") : null;
            var sr = body != null ? body.GetComponent<SpriteRenderer>() : null;
            return sr != null ? sr.sprite : null;
        }

        static string Ago(DateTime when)
        {
            if (when == DateTime.MinValue) return "never visited";
            var span = DateTime.Now - when;
            if (span.TotalMinutes < 2) return "visited just now";
            if (span.TotalHours < 1) return $"visited {(int)span.TotalMinutes} min ago";
            if (span.TotalDays < 1) return $"visited {(int)span.TotalHours} h ago";
            return $"visited {(int)span.TotalDays} d ago";
        }
    }

    /// <summary>A small card with someone's face, a line, and two choices.</summary>
    public class PromptScreen : UiScreen
    {
        public static PromptScreen Instance { get; private set; }

        protected override string Title => _title;
        protected override Sprite TitleIcon => IconPainter.Plane;
        protected override Color AccentColour => UiKit.Hex("5B9BD5");
        protected override float MaxWidth => 820f;

        string _title = "Visitor";
        RectTransform _avatarSlot;
        TextMeshProUGUI _line;
        Button _yes, _no;
        TextMeshProUGUI _yesLabel, _noLabel;
        Action _onYes, _onNo;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Ask(string title, FriendProfile who, string line, string yes, string no, Action onYes, Action onNo = null)
        {
            var p = Instance;
            if (p == null) return;
            p._title = title;
            p.Open(() => { var cb = p._onNo; p._onNo = null; p._onYes = null; cb?.Invoke(); });
            if (p.TitleLabel != null) p.TitleLabel.text = title;
            ClearChildren(p._avatarSlot);
            if (who != null) UiKit.Avatar(p._avatarSlot, who, 150f).anchoredPosition = Vector2.zero;
            p._line.text = line;
            p._yesLabel.text = yes;
            p._noLabel.text = no;
            p._onYes = onYes;
            p._onNo = onNo;
        }

        protected override void Build(RectTransform body)
        {
            _avatarSlot = UiKit.Rect("Avatar", body);
            UiKit.Place(_avatarSlot, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(150f, 150f));

            _line = UiKit.Text(body, "Line", "", 34f, UiKit.Ink, false, TextAlignmentOptions.Center);
            _line.rectTransform.anchorMin = new Vector2(0f, 1f);
            _line.rectTransform.anchorMax = new Vector2(1f, 1f);
            _line.rectTransform.pivot = new Vector2(0.5f, 1f);
            _line.rectTransform.sizeDelta = new Vector2(-20f, 130f);
            _line.rectTransform.anchoredPosition = new Vector2(0f, -176f);

            var buttons = UiKit.Rect("Buttons", body);
            buttons.anchorMin = new Vector2(0f, 0f);
            buttons.anchorMax = new Vector2(1f, 0f);
            buttons.pivot = new Vector2(0.5f, 0f);
            buttons.sizeDelta = new Vector2(0f, 96f);
            var h = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;
            _no = UiKit.Button(buttons, "No", "No", UiKit.ButtonStyle.Secondary, () => Close(), 32f);
            _yes = UiKit.Button(buttons, "Yes", "Yes", UiKit.ButtonStyle.Primary, () =>
            {
                var cb = _onYes;
                _onYes = null;
                _onNo = null;
                Close();
                cb?.Invoke();
            }, 32f);
            _noLabel = _no.GetComponentInChildren<TextMeshProUGUI>();
            _yesLabel = _yes.GetComponentInChildren<TextMeshProUGUI>();
        }

        protected override void Fit()
        {
            base.Fit();
            if (Sheet != null) Sheet.sizeDelta = new Vector2(Sheet.sizeDelta.x, Mathf.Min(Sheet.sizeDelta.y, 128f + 24f + 420f + 32f));
        }
    }
}
