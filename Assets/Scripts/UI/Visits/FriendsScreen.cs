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

        void Awake()
        {
            Instance = this;
            RealFriends.Changed += OnRealFriends;
            PrivateServer.Changed += OnRealFriends;
        }

        void OnDestroy()
        {
            RealFriends.Changed -= OnRealFriends;
            PrivateServer.Changed -= OnRealFriends;
            if (Instance == this) Instance = null;
        }

        /// <summary>A request came in, a friend came online: the open list shows it straight away.</summary>
        void OnRealFriends()
        {
            if (this == null || !IsOpen) return;
            if (_codeBox != null && _codeBox.isFocused) return;   // not under someone's fingers mid-typing
            float scroll = _scroll.verticalNormalizedPosition;
            OnOpen();
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = scroll;
        }

        TMP_InputField _codeBox;

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
            Requests();
            CodeCard();
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
                // Friends are visited while they're online; an offline one's button is grey and says so.
                Button go;
                if (_blocked != null) go = UiKit.Button(row, "Visit", "Visit", UiKit.ButtonStyle.Secondary, Blocked, 28f, IconPainter.Lock);
                else if (!online) go = UiKit.Button(row, "Visit", "Visit", UiKit.ButtonStyle.Secondary, () =>
                {
                    Game.Sfx.Play(Game.SfxKind.Tap, 0.4f);
                    Hints.Show(Loc.F("{0} is offline right now. You can visit when they're online!", who.name));
                }, 28f, IconPainter.Plane);
                else go = UiKit.Button(row, "Visit", "Visit", UiKit.ButtonStyle.Primary, () => { Close(); Visit.Fly(who); }, 28f, IconPainter.Plane);
                if (_blocked != null || !online) Greyed(go);
                var le = go.transform.parent.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 170f; le.preferredHeight = 72f;
            }
        }

        // ---------------------------------------------------------------- friend codes

        string _typed = "";      // the code being typed, kept when the list is rebuilt
        string _addStatus;       // the last answer to Add, shown under the box
        bool _adding;

        /// <summary>Your friend code to share, and a box for a friend's.</summary>
        void CodeCard()
        {
            var card = UiKit.Image(_list, "Codes", UiKit.Rounded(32), UiKit.Hex("EAF3FB")).rectTransform;
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(22, 22, 18, 20);
            v.spacing = 12f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var mine = UiKit.Rect("Mine", card);
            var mh = mine.gameObject.AddComponent<HorizontalLayoutGroup>();
            mh.spacing = 14f;
            mh.childAlignment = TextAnchor.MiddleLeft;
            mh.childControlWidth = mh.childControlHeight = true;
            mh.childForceExpandWidth = mh.childForceExpandHeight = false;
            var col = UiKit.Rect("Column", mine);
            var cv = col.gameObject.AddComponent<VerticalLayoutGroup>();
            cv.childControlWidth = cv.childControlHeight = true;
            cv.childForceExpandWidth = true;
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            UiKit.Text(col, "Label", "Your friend code", 24f, UiKit.InkSoft);
            string code = RealFriends.Code;
            var codeText = UiKit.Text(col, "Code", code ?? "Connecting…", 36f, UiKit.Ink, true);
            codeText.textWrappingMode = TextWrappingModes.NoWrap;
            if (code != null)
            {
                var copy = UiKit.Button(mine, "Copy", "Copy", UiKit.ButtonStyle.Spring, () =>
                {
                    SaveCodeScreen.CopyText(code);
                    Game.Sfx.Play(Game.SfxKind.Coin, 0.6f);
                    Hints.Show("Copied! Send it to a friend so they can add you.");
                }, 26f);
                var cle = copy.transform.parent.gameObject.AddComponent<LayoutElement>();
                cle.preferredWidth = 150f; cle.preferredHeight = 68f;
            }

            var add = UiKit.Rect("Add", card);
            var ah = add.gameObject.AddComponent<HorizontalLayoutGroup>();
            ah.spacing = 14f;
            ah.childAlignment = TextAnchor.MiddleLeft;
            ah.childControlWidth = ah.childControlHeight = true;
            ah.childForceExpandWidth = ah.childForceExpandHeight = false;
            var box = _codeBox = UiKit.Input(add, "FriendCode", "A friend's code, like Miso#1234", false, 80f);
            box.characterLimit = 40;
            box.text = _typed;
            box.onValueChanged.AddListener(t => _typed = t);
            box.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var send = UiKit.Button(add, "AddFriend", "Add", UiKit.ButtonStyle.Primary, () => AddByCode(box.text), 28f, IconPainter.AddFriend);
            var sle = send.transform.parent.gameObject.AddComponent<LayoutElement>();
            sle.preferredWidth = 150f; sle.preferredHeight = 76f;

            if (!string.IsNullOrEmpty(_addStatus))
            {
                var status = UiKit.Text(card, "Status", _addStatus, 24f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
                status.overflowMode = TextOverflowModes.Overflow;
            }
        }

        async void AddByCode(string code)
        {
            if (_adding) return;
            _adding = true;
            string problem = await RealFriends.Add(code);
            _adding = false;
            if (this == null) return;
            if (problem == null)
            {
                Game.Sfx.Play(Game.SfxKind.Chest, 0.5f);
                _typed = "";
                _addStatus = Loc.F("Request sent to {0}. You'll be friends once they accept.", code.Trim());
            }
            else _addStatus = problem;
            if (IsOpen) OnOpen();
        }

        /// <summary>People asking to be your friend: yes or no, right here.</summary>
        void Requests()
        {
            foreach (var (id, code) in RealFriends.Requests)
            {
                var row = UiKit.Image(_list, "Request", UiKit.Rounded(30), UiKit.Hex("FFF1D6")).rectTransform;
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.padding = new RectOffset(22, 18, 16, 16);
                h.spacing = 14f;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = h.childForceExpandHeight = false;
                row.gameObject.AddComponent<LayoutElement>().minHeight = 110f;
                var line = UiKit.Text(row, "Line", Loc.F("<b>{0}</b> wants to be your friend", code), 28f, UiKit.Ink);
                line.overflowMode = TextOverflowModes.Overflow;
                line.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                string memberId = id;
                var no = UiKit.Button(row, "No", "No", UiKit.ButtonStyle.Secondary, async () =>
                {
                    NewStuff.Clear("friends/ask_" + memberId);
                    await RealFriends.Decline(memberId);
                }, 26f);
                var nle = no.transform.parent.gameObject.AddComponent<LayoutElement>();
                nle.preferredWidth = 110f; nle.preferredHeight = 68f;
                var yes = UiKit.Button(row, "Accept", "Accept", UiKit.ButtonStyle.Primary, async () =>
                {
                    NewStuff.Clear("friends/ask_" + memberId);
                    if (await RealFriends.Accept(memberId)) { Game.Sfx.Play(Game.SfxKind.Chest, 0.5f); Hints.Show("You're friends now! Visit them from this list."); }
                    else Hints.Show("Couldn't answer just now. Try again in a moment.");
                }, 26f);
                var yle = yes.transform.parent.gameObject.AddComponent<LayoutElement>();
                yle.preferredWidth = 150f; yle.preferredHeight = 68f;
            }
        }

        /// <summary>Asks first, then removes a friend (and sends them home if they are visiting).</summary>
        void Unfriend(FriendProfile who)
        {
            string line = who.IsReal
                ? Loc.F("Remove {0} from your friends? You'll need their friend code to add them again.", who.name)
                : $"Remove {who.name} from your friends? Their island won't be kept any more.";
            PromptScreen.Ask("Unfriend?", who, line, "Unfriend", "Cancel", () =>
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
            bool mine = PrivateServer.Active;
            UiKit.Text(col, "Count", mine && !PrivateServer.Connected ? "Connecting…" : Loc.F("{0} of {1} islands online", Server.Online, Server.Slots), 26f, UiKit.InkSoft);

            // Buttons: on a private server, its code to share and the way out; otherwise another
            // public server, or a private one.
            var buttons = UiKit.Rect("Buttons", top);
            var bv = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            bv.spacing = 8f;
            bv.childControlWidth = bv.childControlHeight = true;
            bv.childForceExpandWidth = true;
            buttons.gameObject.AddComponent<LayoutElement>().preferredWidth = 230f;
            if (mine)
            {
                string pretty = PrivateServer.Pretty(PrivateServer.Code);
                SmallButton(buttons, "CopyCode", "Copy code", UiKit.ButtonStyle.Spring, () =>
                {
                    SaveCodeScreen.CopyText(pretty);
                    Game.Sfx.Play(Game.SfxKind.Coin, 0.6f);
                    Hints.Show(Loc.F("Copied {0}! Send it to friends so they can join.", pretty));
                });
                SmallButton(buttons, "Leave", "Leave server", UiKit.ButtonStyle.Secondary, () =>
                    PromptScreen.Ask("Leave server?", null, Loc.F("Leave server {0}? You can come back any time with the code.", pretty),
                        "Leave", "Stay", async () =>
                        {
                            await PrivateServer.Leave();
                            if (this != null && IsOpen) OnOpen();
                        }));
            }
            else
            {
                SmallButton(buttons, "Switch", "Switch server", UiKit.ButtonStyle.Secondary, () => { Server.Reconnect(); OnOpen(); });
                SmallButton(buttons, "Private", "Private server", UiKit.ButtonStyle.Spring, PrivateServerScreen.Show);
            }

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

            var hint = UiKit.Text(head, "Hint", mine
                    ? "Everyone with this code lands here. Tap a friend's island to visit, or someone new to add them as a friend."
                    : "Tap an island to fly there. Lend a hand, and add the ones you like as friends.",
                24f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            hint.overflowMode = TextOverflowModes.Overflow;
        }

        static void SmallButton(RectTransform parent, string name, string label, UiKit.ButtonStyle style, System.Action onClick)
        {
            var b = UiKit.Button(parent, name, label, style, onClick, 24f);
            var le = b.transform.parent.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 60f;
        }

        /// <summary>Someone on your private server who isn't your friend yet: ask to be friends.</summary>
        static void OfferFriendship(FriendProfile who)
        {
            if (string.IsNullOrEmpty(who.code)) { Hints.Show("Couldn't find their friend code just now. Try again in a moment."); return; }
            PromptScreen.Ask("Make a friend?", who, Loc.F("Ask {0} to be friends? Once they accept, you can visit each other.", who.name),
                "Add friend", "Not now", async () =>
                {
                    string problem = await RealFriends.Add(who.code);
                    Hints.Show(problem ?? Loc.F("Request sent to {0}. You'll be friends once they accept.", who.name));
                });
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
            if (who.IsReal && !Friends.IsFriend(who.id))
            {
                // A real person on your private server, not a friend yet: visiting is for friends.
                b.onClick.AddListener(() => OfferFriendship(who));
            }
            else if (_blocked != null && !here)
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
