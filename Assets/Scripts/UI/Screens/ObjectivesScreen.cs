using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Every goal in one scrollable place. "Quests" is what each character wants from you right
    /// now; "Achievements" is the whole list — the ones still to earn first, each saying what it
    /// asks for, then the ones you have. Opened from the HUD tracker, the menu, or the tower.
    /// </summary>
    public class ObjectivesScreen : UiScreen
    {
        public static ObjectivesScreen Instance { get; private set; }

        public enum Tab { Quests, Achievements }

        protected override string Title => "Goals";
        protected override Sprite TitleIcon => IconPainter.Goals;

        List<Button> _tabs;
        Tab _tab;
        RectTransform _list;
        ScrollRect _scroll;
        string _banner;
        bool _allGoals;   // the whole to-do list, rather than the next few

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Opens on a tab, optionally with a line at the top (the tower's diary mood).</summary>
        public static void Show(Tab tab, string banner = null, System.Action onClosed = null)
        {
            if (Instance == null) { onClosed?.Invoke(); return; }
            Instance._tab = tab;
            Instance._banner = banner;
            Instance._allGoals = false;
            Instance.Open(onClosed);
        }

        protected override void Build(RectTransform body)
        {
            _tabs = Tabs(body, new[] { "Quests", "Achievements" }, i => { _tab = (Tab)i; Refresh(); }, out var bar);
            NewDot.Live(_tabs[1].transform, () => NewStuff.Any("goals/"), 26f);   // a goal just reached
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, 96f);
            bar.anchoredPosition = Vector2.zero;

            _scroll = UiKit.ScrollList(body, "List", out _list, 14f, new RectOffset(4, 4, 8, 32));
            UiKit.Stretch((RectTransform)_scroll.transform, 0f, 0f, 0f, 112f);
        }

        protected override void OnOpen() => Refresh();

        void Refresh()
        {
            HighlightTab(_tabs, (int)_tab, AccentColour);
            ClearChildren(_list);
            if (!string.IsNullOrEmpty(_banner)) Banner(_banner);
            if (_tab == Tab.Quests) BuildQuests();
            else BuildAchievements();
            _scroll.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
        }

        // ---------------------------------------------------------------- quests

        /// <summary>The colour strip down each task's edge: what kind of task it is.</summary>
        static readonly Color StoryColour = UiKit.Primary, OrderColour = UiKit.Leaf, HobbyColour = UiKit.Spring;

        /// <summary>
        /// Everything you could be doing, story first (the tutorial, each character's request,
        /// the next achievement), then the side things: food orders, today's hobby goal, the
        /// contest and the daily gift. Each kind has its own colour down the card's edge.
        /// </summary>
        void BuildQuests()
        {
            _stripe = StoryColour;
            var tutorial = TutorialDirector.Instance;
            if (tutorial != null && !tutorial.Finished && !string.IsNullOrEmpty(tutorial.CurrentGoal))
            {
                SectionLabel(_list, "Getting started");
                Row(IconPainter.Star, UiKit.Spring, "Your guide", tutorial.CurrentGoal, "Now", UiKit.Spring, tutorial.GoalPoint);
            }

            var quests = Objectives.ActiveQuests();
            SectionLabel(_list, "Story");
            bool wedding = YuzuHeart.WeddingDue && Priest.Instance != null;
            if (wedding)
            {
                var g = Objectives.WeddingGoal(Priest.Instance);
                string line = g.Step == null ? g.Title : g.Title + "\n<color=#C9653A>" + g.Step + "</color>";
                var step = Objectives.StepTo(g.Where.Value, out var ghost);
                if (step != null) line += "\n<color=#C9653A>" + step + "</color>";
                Row(g.Icon, Color.white, StoryText.Guide, line, "The wedding", UiKit.Danger,
                    ghost != null ? (Vector2)ghost.transform.position : g.Where.Value, portrait: true);
            }
            if (quests.Count == 0 && !wedding)
            {
                Empty("Nobody needs a hand right now. Keep building, and new friends will turn up with requests.");
            }
            foreach (var (giver, q) in quests)
            {
                string status; Color chip; string line;
                if (q.Complete) { status = "Reward ready"; chip = UiKit.Sun; line = "Talk to " + giver.Speaker + " to collect your reward"; }
                else if (q.Stage == QuestSystem.Stage.NotStarted) { status = "New"; chip = UiKit.Primary; line = "Talk to " + giver.Speaker + " to hear what they need"; }
                else { status = "In progress"; chip = UiKit.Spring; line = Objectives.QuestGoal(q.Def); }
                // On an island you have not opened yet: say what has to happen first.
                // Where it happens: the task itself while in progress (a character elsewhere), otherwise the giver.
                string first = null;
                Vector2 where = q.Complete || q.Stage == QuestSystem.Stage.NotStarted
                    ? (Vector2)giver.transform.position
                    : Objectives.QuestWhere(q.Def, out first) ?? (Vector2)giver.transform.position;
                if (first != null) line += "\n<color=#C9653A>" + first + "</color>";
                var step = Objectives.StepTo(where, out var ghost);
                if (step != null) line += "\n<color=#C9653A>" + step + "</color>";
                // "Show me" goes where the task happens (roses for the Captain: to the Captain), or to
                // the bridge ghost when that has to come first — not back to whoever asked.
                Vector2 showMe = ghost != null ? (Vector2)ghost.transform.position : where;
                Row(giver.Portrait, Color.white, giver.Speaker, line, status, chip, showMe, portrait: true);
            }

            // What the next achievement is, so the list never leaves you without a plan.
            foreach (var a in Objectives.AllAchievements())
            {
                if (a.Done) continue;
                SectionLabel(_list, "Next up");
                Row(IconPainter.Heart, UiKit.Danger, a.Def.Title, a.Goal, null, default, null);
                break;
            }

            BuildOrders();
            BuildHobbies();
            _stripe = null;
        }

        /// <summary>Who would like which dish, and whether it is already in your bag.</summary>
        void BuildOrders()
        {
            var orders = DishOrders.Open();
            if (orders.Count == 0) return;
            _stripe = OrderColour;
            SectionLabel(_list, "Food orders");
            foreach (var (who, dish, where) in orders)
            {
                bool have = HobbyLog.Count("dish:" + dish.Id) > 0;
                string line = have
                    ? "It's in your bag. Tap them to serve it."
                    : "Cook it at your stove: " + DishOrders.RecipeOf(dish);
                Row(HobbyData.ItemArt("dish:" + dish.Id), UiKit.SurfaceSunk, Loc.F("{0} would like {1}", who, Loc.T(dish.Name)), line,
                    have ? "Ready to serve" : "To cook", have ? UiKit.Leaf : UiKit.Primary, where, portrait: true);
            }
        }

        /// <summary>Today's hobby goal, the running contest, and the daily gift.</summary>
        void BuildHobbies()
        {
            var goal = GameEvents.TodaysGoal;
            bool contest = GameEvents.CanJoin;
            bool gift = DailyGifts.CanClaim;
            if (goal == null && !contest && !gift) return;
            _stripe = HobbyColour;
            SectionLabel(_list, "Hobbies and daily");

            if (goal != null)
            {
                int got = Mathf.Min(GameEvents.GoalProgress, goal.Need);
                string status = GameEvents.GoalClaimed ? "Done" : GameEvents.RewardWaiting ? "Reward ready" : $"{got} / {goal.Need}";
                Color chip = GameEvents.GoalClaimed ? UiKit.Leaf : GameEvents.RewardWaiting ? UiKit.Sun : UiKit.Spring;
                string line = Loc.T(goal.Text) + "\n<color=#C2577F>" + Loc.F("Reward: {0}", GameEvents.RewardText(goal)) + "</color>";
                Row(IconPainter.Star, UiKit.Sun, Loc.F("Today: {0}", Loc.T(goal.Title)), line, status, chip, null,
                    open: () => EventsScreen.Instance?.Open(), openLabel: GameEvents.RewardWaiting ? "Claim" : "Open", faded: GameEvents.GoalClaimed);
            }
            if (contest)
            {
                string name = EventsScreen.ContestName(GameEvents.CurrentKind);
                string line = Loc.F("You're number {0}. {1} left.", GameEvents.Rank(), GameEvents.Left(GameEvents.ContestLeft));
                Row(IconPainter.Trophy, UiKit.Sun, name, line, null, default, null,
                    open: () => EventsScreen.Instance?.Open(), openLabel: "Open");
            }
            if (gift)
                Row(IconPainter.Gift, UiKit.Primary, "Daily gift", "Today's present is waiting for you.", "New", UiKit.Primary, null,
                    open: () => DailyGiftScreen.Instance?.Open(), openLabel: "Open");
        }

        // ---------------------------------------------------------------- achievements

        const int NextFew = 3;

        void BuildAchievements()
        {
            var all = Objectives.AllAchievements();
            int done = 0;
            foreach (var a in all) if (a.Done) done++;

            // Summary: how far along the island is.
            var card = Card(150f);
            var col = Column(card, 8f);
            var head = UiKit.Text(col, "Count", $"{done} of {all.Count} complete", 40f, UiKit.Ink, true);
            head.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
            var fill = UiKit.ProgressBar(col, "Progress", UiKit.Leaf, 26f);
            UiKit.SetProgress(fill, all.Count > 0 ? (float)done / all.Count : 0f);

            // The next few, not all at once: a new island facing forty goals doesn't know where to start.
            SectionLabel(_list, "To do");
            int todo = 0;
            foreach (var a in all)
            {
                if (a.Done) continue;
                todo++;
                if (!_allGoals && todo > NextFew) continue;
                Row(IconPainter.Heart, UiKit.SurfaceSunk, a.Def.Title, a.Goal, null, default, null, iconTint: UiKit.InkSoft, reward: RewardOf(a.Def));
            }
            if (todo == 0) Empty("You've done everything. The island is whole again!");
            else if (!_allGoals && todo > NextFew)
            {
                var more = UiKit.Button(_list, "AllGoals", "Show all " + todo + " goals", UiKit.ButtonStyle.Secondary,
                    () => { _allGoals = true; Refresh(); }, 32f);
                more.gameObject.AddComponent<LayoutElement>().preferredHeight = 88f;
            }

            if (done > 0) SectionLabel(_list, "Complete");
            foreach (var a in all)
            {
                if (!a.Done) continue;
                Row(IconPainter.Check, UiKit.Leaf, a.Def.Title, a.Def.Description, null, default, null, faded: true, reward: RewardOf(a.Def));
            }
            NewStuff.Clear("goals/");   // seen
        }

        // ---------------------------------------------------------------- rows

        /// <summary>The kind colour for the rows being built now (story, orders, hobbies), or none.</summary>
        Color? _stripe;

        RectTransform Card(float minHeight, Color? colour = null, Color? stripe = null)
        {
            var root = UiKit.Rect("Card", _list);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.minHeight = minHeight;
            var lip = UiKit.Image(root, "Lip", UiKit.Rounded(30), new Color(0.82f, 0.74f, 0.64f, 0.55f));
            UiKit.Stretch(lip.rectTransform);
            var face = UiKit.Image(root, "Face", UiKit.Rounded(30), colour ?? Color.white);
            UiKit.Stretch(face.rectTransform, 0f, 5f, 0f, 0f);
            if (stripe.HasValue)
            {
                var band = UiKit.Image(root, "Kind", UiKit.Rounded(8), stripe.Value);
                band.rectTransform.anchorMin = Vector2.zero;
                band.rectTransform.anchorMax = new Vector2(0f, 1f);
                band.rectTransform.offsetMin = new Vector2(10f, 22f);
                band.rectTransform.offsetMax = new Vector2(22f, -18f);
                band.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }
            var h = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(stripe.HasValue ? 38 : 22, 22, 20, 26);
            h.spacing = 22f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            lip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            face.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return root;
        }

        static RectTransform Column(RectTransform card, float spacing)
        {
            var col = UiKit.Rect("Column", card);
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childAlignment = TextAnchor.MiddleLeft;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return col;
        }

        void Row(Sprite icon, Color iconBack, string title, string line, string status, Color statusColour,
            Vector2? where, bool portrait = false, Color? iconTint = null, bool faded = false, WearItem reward = null,
            System.Action open = null, string openLabel = null)
        {
            var rewardArt = Wardrobe.Art(reward);
            if (rewardArt != null)
                line += "\n<color=#C2577F>Reward: " + reward.Name + "</color>";
            var card = Card(132f, null, _stripe);

            // Icon disc.
            var disc = UiKit.Image(card, "Disc", UiKit.Circle, portrait ? UiKit.SurfaceSunk : iconBack);
            var dle = disc.gameObject.AddComponent<LayoutElement>();
            dle.preferredWidth = dle.preferredHeight = dle.minWidth = dle.minHeight = 92f;
            if (icon != null)
            {
                var ic = UiKit.Image(disc.transform, "Icon", icon, portrait ? Color.white : iconTint ?? Color.white);
                ic.preserveAspect = true;
                UiKit.Stretch(ic.rectTransform, portrait ? 8f : 22f, portrait ? 8f : 22f, portrait ? 8f : 22f, portrait ? 8f : 22f);
            }

            var col = Column(card, 4f);
            var t = UiKit.Text(col, "Title", title, 34f, faded ? UiKit.InkSoft : UiKit.Ink, true);
            t.textWrappingMode = TextWrappingModes.Normal;
            if (!string.IsNullOrEmpty(line))
            {
                var l = UiKit.Text(col, "Line", line, 28f, faded ? new Color(UiKit.InkSoft.r, UiKit.InkSoft.g, UiKit.InkSoft.b, 0.8f) : UiKit.InkSoft);
                l.overflowMode = TextOverflowModes.Overflow;
            }

            var right = UiKit.Rect("Right", card);
            var rv = right.gameObject.AddComponent<VerticalLayoutGroup>();
            rv.spacing = 10f;
            rv.childAlignment = TextAnchor.MiddleRight;
            rv.childControlWidth = rv.childControlHeight = true;
            rv.childForceExpandWidth = rv.childForceExpandHeight = false;

            if (rewardArt != null)
            {
                var pic = UiKit.Image(right, "Reward", rewardArt, faded ? new Color(1f, 1f, 1f, 0.6f) : Color.white);
                pic.preserveAspect = true;
                var le = pic.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = 72f;
            }
            if (!string.IsNullOrEmpty(status))
            {
                var chip = UiKit.Image(right, "Status", UiKit.Rounded(22), statusColour);
                var cl = UiKit.Text(chip.transform, "Label", status, 24f, Color.white, true, TextAlignmentOptions.Center);
                cl.textWrappingMode = TextWrappingModes.NoWrap;
                cl.overflowMode = TextOverflowModes.Overflow;
                UiKit.Stretch(cl.rectTransform, 16f, 0f, 16f, 0f);
                var le = chip.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 44f;
                le.preferredWidth = cl.GetPreferredValues(status).x + 36f;
            }
            if (where.HasValue)
            {
                var spot = where.Value;
                var go = UiKit.Button(right, "Show", "Show me", UiKit.ButtonStyle.Secondary, () =>
                {
                    Close();
                    PlayerAvatar.GoTo(spot);   // walk there: a camera peek that snaps back reads as nothing happening
                }, 26f);
                var le = go.transform.parent.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 170f;
                le.preferredHeight = 64f;
            }
            else if (open != null)
            {
                var go = UiKit.Button(right, "Open", openLabel ?? "Open", UiKit.ButtonStyle.Secondary, () => { Close(); open(); }, 26f);
                var le = go.transform.parent.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 170f;
                le.preferredHeight = 64f;
            }
        }

        /// <summary>The outfit an achievement awards, once the wardrobe has art.</summary>
        static WearItem RewardOf(MoteDef m) => Wardrobe.Ready ? Wardrobe.RewardFor(m.Func) : null;

        void Banner(string text)
        {
            var card = Card(110f, UiKit.Hex("FFF1D6"));
            var ic = UiKit.Image(card, "Icon", IconPainter.Heart, UiKit.Danger);
            ic.preserveAspect = true;
            var le = ic.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = 56f;
            var t = UiKit.Text(card, "Text", text, 30f, UiKit.Ink);
            t.overflowMode = TextOverflowModes.Overflow;
            t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        void Empty(string text)
        {
            var t = UiKit.Text(_list, "Empty", text, 28f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            t.overflowMode = TextOverflowModes.Overflow;
            t.margin = new Vector4(24f, 12f, 24f, 12f);
        }
    }
}
