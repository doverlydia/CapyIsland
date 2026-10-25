using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The festival page: an illustrated banner for the running contest with its countdown, the
    /// prizes, a podium for the top three (with everyone's capybara) and the rest of the table,
    /// then the day's special goal with pips that fill as you go and a reward to claim.
    /// Opening it marks what is running as seen, which clears the red dot on the Events button.
    /// </summary>
    public class EventsScreen : UiScreen
    {
        public static EventsScreen Instance { get; private set; }

        protected override string Title => "Events";
        protected override Sprite TitleIcon => IconPainter.Trophy;
        protected override Color AccentColour => UiKit.Hex("E3A92B");
        protected override float MaxWidth => 940f;

        static readonly Color Gold = UiKit.Hex("E8B23A"), Silver = UiKit.Hex("AEB9C4"), Bronze = UiKit.Hex("CD8B55");

        RectTransform _list;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override void Build(RectTransform body)
        {
            var scroll = UiKit.ScrollList(body, "List", out _list, 18f, new RectOffset(0, 0, 4, 28));
            UiKit.Stretch((RectTransform)scroll.transform);
            // The podium, the table and the day's goal run on below the banner: a round button says so.
            MoreBelowCue.Add(scroll, AccentColour);
        }

        protected override void OnOpen()
        {
            GameEvents.MarkSeen();
            Refresh();
        }

        void Refresh()
        {
            ClearChildren(_list);
            Prizes();
            Contest();
            Goal();
        }

        // ---------------------------------------------------------------- prizes to claim

        /// <summary>Contests that have ended with your prize still waiting, each with its own Claim.</summary>
        void Prizes()
        {
            foreach (var r in GameEvents.Unclaimed())
            {
                var card = UiKit.Image(_list, "Prize", UiKit.Rounded(30), UiKit.Hex("FFF4D6")).rectTransform;
                var h = card.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.padding = new RectOffset(22, 22, 18, 18);
                h.spacing = 18f;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = h.childForceExpandHeight = false;
                card.gameObject.AddComponent<LayoutElement>().minHeight = 130f;
                RtlLayout.Add(h);

                var disc = UiKit.Image(card, "Disc", UiKit.Circle, Theme(r.Kind));
                var dle = disc.gameObject.AddComponent<LayoutElement>();
                dle.preferredWidth = dle.preferredHeight = dle.minWidth = 84f;
                var medalArt = r.Medal != null ? Wardrobe.Art(r.Medal) : null;
                var icon = UiKit.Image(disc.transform, "Icon", medalArt != null ? medalArt : IconPainter.Trophy, Color.white);
                icon.preserveAspect = true;
                UiKit.Stretch(icon.rectTransform, 16f, 16f, 16f, 16f);

                var words = UiKit.Rect("Words", card);
                var wv = words.gameObject.AddComponent<VerticalLayoutGroup>();
                wv.spacing = 4f;
                wv.childControlWidth = wv.childControlHeight = true;
                wv.childForceExpandWidth = true;
                wv.childForceExpandHeight = false;
                words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                Line(words, Loc.T(ContestName(r.Kind)) + " · " + Dates(r.Start, r.End), 30f, UiKit.Ink, true);
                string place = r.Rank switch { 1 => "first", 2 => "second", 3 => "third", _ => null };
                Line(words, place != null ? Loc.F("You came " + place + " with {0}.", r.Score) : Loc.F("You finished number {0} with {1}.", r.Rank, r.Score), 26f, UiKit.InkSoft);

                var contest = r.Contest;
                var claim = UiKit.Button(card, "Claim", "Claim", UiKit.ButtonStyle.Primary, () =>
                {
                    var paid = GameEvents.Claim(contest);
                    if (paid == null) return;
                    Sfx.Play(SfxKind.Chest, 0.7f);
                    ShowResult(paid);
                    Refresh();
                }, 30f, IconPainter.Sparkle);
                var cle = claim.transform.parent.gameObject.AddComponent<LayoutElement>();
                cle.preferredWidth = 190f; cle.preferredHeight = 88f;
                claim.transform.DOScale(1.05f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
            }
        }

        static readonly string[] Months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        /// <summary>"Oct 9 – 11" (or "9 – 11 Oct" in Hebrew, day first), the month names translated.</summary>
        static string Dates(System.DateTime start, System.DateTime end)
        {
            bool dayFirst = Loc.Current == Language.Hebrew;
            string Day(System.DateTime d, bool withMonth) => !withMonth ? d.Day.ToString()
                : dayFirst ? d.Day + " " + Loc.T(Months[d.Month - 1]) : Loc.T(Months[d.Month - 1]) + " " + d.Day;
            bool sameMonth = start.Month == end.Month;
            return dayFirst
                ? Day(start, !sameMonth) + " – " + Day(end, true)
                : Day(start, true) + " – " + Day(end, !sameMonth);
        }

        // ---------------------------------------------------------------- words

        public static string ContestName(ContestKind k) => k switch
        {
            ContestKind.Bugs => "Bug Contest",
            ContestKind.Birds => "Birdwatching Contest",
            _ => "Fishing Contest",
        };

        static string WhatCounts(ContestKind k) => k switch
        {
            ContestKind.Bugs => "Whoever catches the most bugs wins.",
            ContestKind.Birds => "Whoever spots the most birds wins.",
            _ => "Whoever catches the most fish wins.",
        };

        static string HowToJoin(ContestKind k) => k switch
        {
            ContestKind.Bugs => "Get a bug net to take part.",
            ContestKind.Birds => "Get binoculars to take part.",
            _ => "Get a fishing rod to take part.",
        };

        static Color Theme(ContestKind k) => k switch
        {
            ContestKind.Bugs => UiKit.Hex("6BAA4E"),
            ContestKind.Birds => UiKit.Hex("5B9BD5"),
            _ => UiKit.Hex("2DA39A"),
        };

        // ---------------------------------------------------------------- the contest

        void Contest()
        {
            var kind = GameEvents.CurrentKind;
            Banner(kind);

            var counts = UiKit.Text(_list, "Counts", WhatCounts(kind), 30f, UiKit.Ink, false, TextAlignmentOptions.Center);
            counts.overflowMode = TextOverflowModes.Overflow;
            if (!GameEvents.CanJoin)
            {
                var join = UiKit.Text(_list, "Join", HowToJoin(kind), 28f, UiKit.Danger, true, TextAlignmentOptions.Center);
                join.overflowMode = TextOverflowModes.Overflow;
            }

            Prizes(kind);
            var board = GameEvents.Leaderboard();
            Podium(board);
            for (int i = 3; i < board.Count; i++) Row(board[i], i + 1);
        }

        /// <summary>The contest's painting, its name on a ribbon and the time left in a pill.</summary>
        void Banner(ContestKind kind)
        {
            // A framed picture: a rounded white mount, and the painting clipped inside it.
            var mount = UiKit.Image(_list, "Banner", UiKit.Rounded(34), Color.white).rectTransform;
            mount.gameObject.AddComponent<LayoutElement>().preferredHeight = 400f;
            var frame = UiKit.Image(mount, "Frame", UiKit.Rounded(24), Theme(kind)).rectTransform;
            UiKit.Stretch(frame, 10f, 10f, 10f, 10f);
            frame.gameObject.AddComponent<RectMask2D>();

            var art = Resources.Load<Sprite>("Events/Banner_" + kind);
            if (art != null)
            {
                var pic = UiKit.Image(frame, "Picture", art, Color.white);
                UiKit.Stretch(pic.rectTransform);
                var fit = pic.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;   // fill, cropping the edges
                fit.aspectRatio = art.rect.width / art.rect.height;
            }

            // The name on a ribbon along the bottom.
            var ribbon = UiKit.Image(frame, "Ribbon", UiKit.Rounded(26), Theme(kind));
            var rr = ribbon.rectTransform;
            rr.anchorMin = new Vector2(0f, 0f);
            rr.anchorMax = new Vector2(1f, 0f);
            rr.pivot = new Vector2(0.5f, 0f);
            rr.offsetMin = new Vector2(24f, 20f);
            rr.offsetMax = new Vector2(-24f, 20f + 84f);
            var name = UiKit.Text(ribbon.transform, "Name", ContestName(kind), 44f, Color.white, true, TextAlignmentOptions.Center);
            UiKit.Stretch(name.rectTransform, 16f, 0f, 16f, 0f);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.enableAutoSizing = true;
            name.fontSizeMin = 26f;
            name.fontSizeMax = 44f;

            // How long is left, in the top corner.
            var pill = UiKit.Image(frame, "Ends", UiKit.Rounded(26), UiKit.HudGlass);
            UiKit.Place(pill.rectTransform, new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(300f, 60f));
            var ic = UiKit.Image(pill.transform, "Icon", IconPainter.Calendar, Color.white);
            UiKit.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(34f, 34f));
            var ends = UiKit.Text(pill.transform, "Text", "Ends in " + GameEvents.Left(GameEvents.ContestLeft), 26f, Color.white, true, TextAlignmentOptions.Center);
            UiKit.Stretch(ends.rectTransform, 52f, 0f, 12f, 0f);
            ends.textWrappingMode = TextWrappingModes.NoWrap;
            ends.enableAutoSizing = true;
            ends.fontSizeMin = 18f;
            ends.fontSizeMax = 26f;
            // The pill grows (leftwards, from its corner) to hold the words as drawn: Easy Reading's
            // size, or a longer translation. Padding: the calendar on the left, a margin on the right.
            FitsText.Add(pill, ends, new Vector2(84f, 24f), wide: true, tall: true);
        }

        /// <summary>Three prize chips: the place, the gems, and the medal for first.</summary>
        void Prizes(ContestKind kind)
        {
            var row = UiKit.Rect("Prizes", _list);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;
            var medal = System.Array.Find(Wardrobe.Items, w => w.Unlock == "contest_" + kind);

            for (int place = 1; place <= 3; place++)
            {
                var chip = UiKit.Image(row, "Prize" + place, UiKit.Rounded(28), Color.white);
                var disc = UiKit.Image(chip.transform, "Place", UiKit.Circle, PlaceColour(place - 1));
                UiKit.Place(disc.rectTransform, new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(48f, 48f));
                var n = UiKit.Text(disc.transform, "N", place.ToString(), 26f, Color.white, true, TextAlignmentOptions.Center);
                UiKit.Stretch(n.rectTransform);
                n.GetComponent<LocText>().NoScale = true;

                var gem = UiKit.Image(chip.transform, "Gem", UiKit.ResourceIcon(ResourceKind.Gem), Color.white);
                gem.preserveAspect = true;
                UiKit.Place(gem.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(place == 1 && medal != null ? -46f : -24f, 8f), new Vector2(48f, 48f));
                var amount = UiKit.Text(chip.transform, "Amount", GameEvents.PrizeGems[place - 1].ToString(), 34f, UiKit.Ink, true, TextAlignmentOptions.Left);
                UiKit.Place(amount.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(place == 1 && medal != null ? -14f : 8f, 8f), new Vector2(90f, 48f));
                amount.rectTransform.pivot = new Vector2(0f, 0.5f);
                amount.textWrappingMode = TextWrappingModes.NoWrap;
                amount.GetComponent<LocText>().NoScale = true;

                if (place == 1 && medal != null && Wardrobe.Art(medal) != null)
                {
                    var m = UiKit.Image(chip.transform, "Medal", Wardrobe.Art(medal), Color.white);
                    m.preserveAspect = true;
                    UiKit.Place(m.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(70f, 8f), new Vector2(60f, 76f));
                }
                var label = UiKit.Text(chip.transform, "Label", place == 1 && medal != null ? "+ " + medal.Name : "gems", 20f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
                label.rectTransform.anchorMin = new Vector2(0f, 0f);
                label.rectTransform.anchorMax = new Vector2(1f, 0f);
                label.rectTransform.pivot = new Vector2(0.5f, 0f);
                label.rectTransform.sizeDelta = new Vector2(-12f, 34f);
                label.rectTransform.anchoredPosition = new Vector2(0f, 10f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.enableAutoSizing = true;
                label.fontSizeMin = 14f;
                label.fontSizeMax = 20f;
            }
        }

        /// <summary>First, second and third on a podium, each with their capybara.</summary>
        void Podium(List<ContestEntry> board)
        {
            var stage = UiKit.Rect("Podium", _list);
            var h = stage.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childAlignment = TextAnchor.LowerCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = false;
            var stageLayout = stage.gameObject.AddComponent<LayoutElement>();
            stageLayout.preferredHeight = 400f;
            GrowsWithText.Add(stageLayout, tall: 0.1f);   // room for the names' taller lines

            // Second on the left, first in the middle, third on the right.
            foreach (int i in new[] { 1, 0, 2 })
            {
                if (i >= board.Count) continue;
                var e = board[i];
                var col = UiKit.Rect("Place" + (i + 1), stage);
                var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
                v.spacing = 6f;
                v.childAlignment = TextAnchor.LowerCenter;
                v.childControlWidth = v.childControlHeight = true;
                v.childForceExpandWidth = false;
                v.childForceExpandHeight = false;

                if (i == 0)
                {
                    var crown = UiKit.Image(col, "Crown", IconPainter.Trophy, Gold);
                    crown.preserveAspect = true;
                    var cle = crown.gameObject.AddComponent<LayoutElement>();
                    cle.preferredWidth = cle.preferredHeight = 44f;
                }
                float size = i == 0 ? 120f : 96f;
                var avatar = UiKit.Avatar(col, Profile(e), size);
                if (e.You)
                {
                    var ring = UiKit.Image(avatar, "You", IconPainter.Ring, UiKit.Primary);
                    UiKit.Stretch(ring.rectTransform, -6f, -6f, -6f, -6f);
                }

                var name = UiKit.Text(col, "Name", e.You ? "You" : e.Name, 24f, UiKit.Ink, true, TextAlignmentOptions.Center);
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.enableAutoSizing = true;
                name.fontSizeMin = 14f;
                name.fontSizeMax = 24f;
                var nle = name.gameObject.AddComponent<LayoutElement>();
                nle.preferredWidth = 230f;
                nle.preferredHeight = 32f;
                // A name shrinks to fit its line: with Easy Reading the line grows, taller and a
                // little wider than the step (the columns have the room).
                GrowsWithText.Add(nle, tall: 1f, wide: 0.4f);

                var step = UiKit.Image(col, "Step", UiKit.Rounded(22), PlaceColour(i));
                var sle = step.gameObject.AddComponent<LayoutElement>();
                sle.preferredWidth = 230f;
                sle.preferredHeight = i == 0 ? 170f : i == 1 ? 136f : 112f;
                var place = UiKit.Text(step.transform, "Place", (i + 1).ToString(), 42f, Color.white, true, TextAlignmentOptions.Top);
                UiKit.Stretch(place.rectTransform, 0f, 0f, 0f, 6f);
                place.GetComponent<LocText>().NoScale = true;
                var score = UiKit.Text(step.transform, "Score", e.Score.ToString(), 28f, new Color(1f, 1f, 1f, 0.9f), true, TextAlignmentOptions.Bottom);
                UiKit.Stretch(score.rectTransform, 0f, 8f, 0f, 0f);
                score.GetComponent<LocText>().NoScale = true;
            }
        }

        void Row(ContestEntry e, int place)
        {
            var row = UiKit.Image(_list, "Row" + place, UiKit.Rounded(26), e.You ? UiKit.Hex("FFE7A3") : Color.white).rectTransform;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(18, 26, 10, 10);
            h.spacing = 14f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().minHeight = 80f;
            RtlLayout.Add(h);

            var n = UiKit.Text(row, "Place", place.ToString(), 28f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
            n.gameObject.AddComponent<LayoutElement>().preferredWidth = 40f;
            n.GetComponent<LocText>().NoScale = true;
            UiKit.Avatar(row, Profile(e), 60f);
            var name = UiKit.Text(row, "Name", e.You ? e.Name + " (you)" : e.Name, 30f, UiKit.Ink, e.You);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var score = UiKit.Text(row, "Score", e.Score.ToString(), 32f, UiKit.PrimaryDark, true, TextAlignmentOptions.Right);
            score.textWrappingMode = TextWrappingModes.NoWrap;
        }

        static Color PlaceColour(int i) => i switch { 0 => Gold, 1 => Silver, 2 => Bronze, _ => UiKit.InkSoft };

        /// <summary>
        /// A capybara for a leaderboard name: a real friend if it is one, yourself in what you are
        /// wearing, or a bot whose fur and hat are picked from its name (so it is always the same).
        /// </summary>
        static FriendProfile Profile(ContestEntry e)
        {
            if (e.You)
            {
                var me = new FriendProfile { name = e.Name, seed = 7, you = true };
                foreach (WearSlot slot in System.Enum.GetValues(typeof(WearSlot)))
                {
                    var worn = Wardrobe.Worn(slot);
                    if (worn != null) me.outfit.Add(worn.Id);
                }
                return me;
            }
            foreach (var f in Friends.All) if (f.name == e.Name) return f;

            uint hash = 2166136261;
            foreach (char c in e.Name) hash = (hash ^ c) * 16777619;
            var bot = new FriendProfile { name = e.Name, seed = (int)(hash & 0x7fffffff), tint = (hash % 200) / 100f - 1f };
            var hats = new List<string>();
            foreach (var w in Wardrobe.Items) if (w.Slot == WearSlot.Hat && w.Unlock != "horaira" && Wardrobe.Art(w) != null) hats.Add(w.Id);
            if (hats.Count > 0 && (hash >> 8) % 3 != 0) bot.outfit.Add(hats[(int)((hash >> 12) % (uint)hats.Count)]);
            return bot;
        }

        // ---------------------------------------------------------------- today's goal

        void Goal()
        {
            var goal = GameEvents.TodaysGoal;
            var card = UiKit.Image(_list, "Goal", UiKit.Rounded(34), UiKit.Hex("E8F5F2")).rectTransform;
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(28, 28, 58, 26);
            v.spacing = 12f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            Bunting(card);

            var head = UiKit.Text(card, "Head", "Today's goal", 26f, UiKit.Hex("2D7F75"), true, TextAlignmentOptions.Center);
            head.characterSpacing = 4f;
            if (goal == null)
            {
                Line(card, "Get a fishing rod, binoculars or a bug net, and a new goal will be waiting here every day.", 28f, UiKit.Ink, false, TextAlignmentOptions.Center);
                return;
            }

            // The hobby's picture in a disc, beside the title and what to do.
            var top = UiKit.Rect("Top", card);
            var th = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            th.spacing = 18f;
            th.childAlignment = TextAnchor.MiddleLeft;
            th.childControlWidth = th.childControlHeight = true;
            th.childForceExpandWidth = th.childForceExpandHeight = false;
            RtlLayout.Add(th);
            var disc = UiKit.Image(top, "Disc", UiKit.Circle, UiKit.Spring);
            var dle = disc.gameObject.AddComponent<LayoutElement>();
            dle.preferredWidth = dle.preferredHeight = dle.minWidth = 96f;
            var icon = UiKit.Image(disc.transform, "Icon", HobbyIcon(goal.Hobby), Color.white);
            icon.preserveAspect = true;
            UiKit.Stretch(icon.rectTransform, 20f, 20f, 20f, 20f);
            var words = UiKit.Rect("Words", top);
            var wv = words.gameObject.AddComponent<VerticalLayoutGroup>();
            wv.spacing = 4f;
            wv.childControlWidth = wv.childControlHeight = true;
            wv.childForceExpandWidth = true;
            wv.childForceExpandHeight = false;
            words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            Line(words, goal.Title, 38f, UiKit.Hex("2D7F75"), true);
            Line(words, goal.Text, 30f, UiKit.Ink);

            // Progress as pips that fill in.
            int have = Mathf.Min(GameEvents.GoalProgress, goal.Need);
            var pips = UiKit.Rect("Pips", card);
            var ph = pips.gameObject.AddComponent<HorizontalLayoutGroup>();
            ph.spacing = 10f;
            ph.childAlignment = TextAnchor.MiddleCenter;
            ph.childControlWidth = ph.childControlHeight = true;
            ph.childForceExpandWidth = ph.childForceExpandHeight = false;
            pips.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
            for (int i = 0; i < goal.Need; i++)
            {
                bool done = i < have;
                var pip = UiKit.Image(pips, "Pip", UiKit.Circle, done ? UiKit.Leaf : new Color(0f, 0f, 0f, 0.08f));
                var ple = pip.gameObject.AddComponent<LayoutElement>();
                ple.preferredWidth = ple.preferredHeight = 44f;
                if (done)
                {
                    var tick = UiKit.Image(pip.transform, "Tick", IconPainter.Check, Color.white);
                    UiKit.Stretch(tick.rectTransform, 9f, 9f, 9f, 9f);
                }
            }

            // The reward, as a chip.
            var reward = UiKit.Image(card, "Reward", UiKit.Rounded(26), Color.white).rectTransform;
            var rh = reward.gameObject.AddComponent<HorizontalLayoutGroup>();
            rh.padding = new RectOffset(18, 18, 10, 10);
            rh.spacing = 12f;
            rh.childAlignment = TextAnchor.MiddleCenter;
            rh.childControlWidth = rh.childControlHeight = true;
            rh.childForceExpandWidth = rh.childForceExpandHeight = false;
            var gift = UiKit.Image(reward, "Gift", IconPainter.Gift, UiKit.Hex("E3A92B"));
            var gle = gift.gameObject.AddComponent<LayoutElement>();
            gle.preferredWidth = gle.preferredHeight = 40f;
            var rt = UiKit.Text(reward, "Text", "Reward: " + GameEvents.RewardText(goal), 28f, UiKit.Ink, true);
            rt.textWrappingMode = TextWrappingModes.NoWrap;

            if (GameEvents.GoalClaimed)
                Line(card, "Claimed! A new goal comes tomorrow.", 28f, UiKit.Leaf, true, TextAlignmentOptions.Center);
            else if (GameEvents.GoalDone)
            {
                var claim = UiKit.Button(card, "Claim", "Claim reward", UiKit.ButtonStyle.Primary, () =>
                {
                    if (GameEvents.ClaimGoal()) Refresh();
                }, 34f, IconPainter.Sparkle);
                claim.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
                // A gentle heartbeat: this button wants pressing.
                claim.transform.DOScale(1.05f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
            }
            else Line(card, "New goal in " + GameEvents.Left(GameEvents.GoalLeft), 24f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
        }

        static Sprite HobbyIcon(Hobby h) => h switch
        {
            Hobby.Birding => IconPainter.Bird,
            Hobby.Bugs => HobbyData.Art("Bug_Butterfly") ?? IconPainter.Sparkle,
            _ => IconPainter.Rod,
        };

        /// <summary>A string of little festival flags across the top of a card.</summary>
        static void Bunting(RectTransform card)
        {
            var line = UiKit.Rect("Bunting", card);
            line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            line.anchorMin = new Vector2(0f, 1f);
            line.anchorMax = new Vector2(1f, 1f);
            line.pivot = new Vector2(0.5f, 1f);
            line.offsetMin = new Vector2(24f, -46f);
            line.offsetMax = new Vector2(-24f, -8f);
            var h = line.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6f;
            h.childAlignment = TextAnchor.UpperCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = false;
            var colours = new[] { UiKit.Primary, UiKit.Sun, UiKit.Spring, UiKit.Hex("E58FB0"), UiKit.Leaf };
            for (int i = 0; i < 14; i++)
            {
                var flag = UiKit.Image(line, "Flag", Pennant, colours[i % colours.Length]);
                flag.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            }
        }

        static Sprite _pennant;
        static Sprite Pennant => _pennant != null ? _pennant : _pennant = IconPainter.Paint(64, (x, y) =>
            IconPainter.Triangle(x, y, new Vector2(-0.85f, 0.9f), new Vector2(0.85f, 0.9f), new Vector2(0f, -0.9f), 0.06f));

        static TextMeshProUGUI Line(RectTransform parent, string text, float size, Color colour, bool bold = false,
            TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var t = UiKit.Text(parent, "Line", text, size, colour, bold, align);
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        // ---------------------------------------------------------------- the result card

        /// <summary>The card shown when you claim an ended contest's prize.</summary>
        public static void ShowResult(ContestResult r)
        {
            string place = r.Rank switch { 1 => "first", 2 => "second", 3 => "third", _ => null };
            string title = ContestName(r.Kind) + ": " + (place != null ? $"you came {place}!" : "results");
            string body = place != null
                ? $"You finished {place} with {r.Score}. Here are {r.Gems} gems."
                : $"You finished number {r.Rank} with {r.Score}. Thanks for taking part: {r.Gems} gems for you.";
            if (r.Medal != null) body += "\n" + $"And the {r.Medal.Name} is waiting in your wardrobe!";
            var art = r.Medal != null ? Wardrobe.Art(r.Medal) : null;
            CardPopup.Show(title, body, art != null ? art : IconPainter.Trophy);
        }
    }
}
