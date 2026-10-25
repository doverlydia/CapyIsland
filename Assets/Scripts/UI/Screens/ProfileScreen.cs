using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// You: your capybara in full, dressed as it is now, your name (tap to change it), and a few
    /// numbers about your island life — achievements, friends, islands visited, hobbies.
    /// </summary>
    public class ProfileScreen : UiScreen
    {
        public static ProfileScreen Instance { get; private set; }

        protected override string Title => "Profile";
        protected override Sprite TitleIcon => IconPainter.FaceSmile;
        protected override Color AccentColour => UiKit.Hex("E58FB0");

        RectTransform _list;
        TMP_InputField _name;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Show()
        {
            if (Instance != null) Instance.Open();
        }

        protected override void Build(RectTransform body)
        {
            var scroll = UiKit.ScrollList(body, "List", out _list, 18f, new RectOffset(4, 4, 8, 32));
            UiKit.Stretch((RectTransform)scroll.transform);
        }

        protected override void OnOpen()
        {
            ClearChildren(_list);
            Hero();
            NameField();

            if (Wardrobe.Ready)
            {
                var dress = UiKit.Button(_list, "Wardrobe", "Change outfit", UiKit.ButtonStyle.Secondary, () =>
                {
                    Close();
                    if (WardrobeScreen.Instance != null) WardrobeScreen.Instance.Open();
                }, 32f, IconPainter.Hanger);
                dress.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
                if (NewStuff.Any("wardrobe/")) NewDot.Fixed(dress.transform, 30f);   // a new piece to try on
            }

            SectionLabel(_list, "Your island life");
            Stats();

            SectionLabel(_list, "Hobbies");
            Hobbies();
        }

        protected override void OnClose()
        {
            if (_name != null) PlayerProfile.Name = _name.text;
        }

        // ---------------------------------------------------------------- the picture

        void Hero()
        {
            var stage = UiKit.Image(_list, "Hero", UiKit.Rounded(40), UiKit.Hex("FBE3EC"));
            stage.gameObject.AddComponent<LayoutElement>().preferredHeight = 360f;
            var glow = UiKit.Image(stage.transform, "Glow", UiKit.Circle, new Color(1f, 1f, 1f, 0.65f));
            UiKit.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 300f));
            var capy = CapyPortrait.Create(stage.transform, "You", 290f);
            capy.Rect.anchorMin = capy.Rect.anchorMax = new Vector2(0.5f, 0f);
            capy.Rect.anchoredPosition = new Vector2(0f, 26f);
        }

        void NameField()
        {
            var row = UiKit.Rect("NameRow", _list);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            var label = UiKit.Text(row, "Label", "Name", 30f, UiKit.InkSoft, true);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = 110f;

            _name = UiKit.Input(row, "Name", "Your name", false, 88f);
            _name.characterLimit = PlayerProfile.MaxNameLength;
            _name.text = PlayerProfile.Name;
            _name.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1f;
            _name.onEndEdit.AddListener(v =>
            {
                PlayerProfile.Name = v;
                _name.SetTextWithoutNotify(PlayerProfile.Name);
            });
            var pencil = UiKit.Image(_name.transform, "Edit", IconPainter.Gear, UiKit.InkSoft);
            UiKit.Place(pencil.rectTransform, new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(34f, 34f));
            pencil.raycastTarget = false;
        }

        // ---------------------------------------------------------------- numbers

        void Stats()
        {
            int cats = 0;
            foreach (var c in HobbyLog.Cats) if (c.adopted) cats++;
            int pieces = 0, total = 0;
            foreach (var item in Wardrobe.Items)
            {
                if (Wardrobe.Art(item) == null) continue;
                total++;
                if (Wardrobe.Owns(item)) pieces++;
            }
            var stats = new List<(Sprite icon, Color colour, string value, string label)>
            {
                (IconPainter.Star, UiKit.Sun, $"{Motes.CollectedCount} / {Motes.Total}", "Achievements"),
                (IconPainter.Hanger, UiKit.Hex("E58FB0"), $"{pieces} / {total}", "Outfit pieces"),
                (IconPainter.AddFriend, UiKit.Hex("5B9BD5"), Friends.All.Count.ToString(), "Friends"),
                (IconPainter.Plane, UiKit.Hex("5B9BD5"), Visit.IslandsVisited.ToString(), "Islands visited"),
                (IconPainter.Rod, UiKit.Hex("3E9C8F"), HobbyLog.Fish.Count.ToString(), "Kinds of fish"),
                (IconPainter.Heart, UiKit.Hex("F2668B"), cats.ToString(), "Cats adopted"),
                (IconPainter.Calendar, UiKit.Primary, $"Day {DailyGifts.CurrentDay}", "Daily streak"),
                (IconPainter.Cards, UiKit.Hex("A77BD6"), CardsFound(), "Cards found"),
            };

            var grid = UiKit.Rect("Stats", _list);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            grid.gameObject.AddComponent<FlexGrid>();
            float cell = 104f * Accessibility.TextScale;   // taller with Easy Reading, so the words grow
            g.cellSize = new Vector2(440f, cell);
            g.spacing = new Vector2(14f, 14f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 2;
            g.childAlignment = TextAnchor.UpperCenter;
            int rows = (stats.Count + 1) / 2;
            grid.gameObject.AddComponent<LayoutElement>().preferredHeight = rows * cell + (rows - 1) * 14f;
            foreach (var s in stats) Stat(grid, s.icon, s.colour, s.value, s.label);
        }

        static string CardsFound()
        {
            int found = 0;
            foreach (var card in Cards.All) if (Cards.IsCollected(card.Id)) found++;
            return $"{found} / {Cards.All.Length}";
        }

        static void Stat(Transform parent, Sprite icon, Color colour, string value, string label)
        {
            var tile = UiKit.Image(parent, label, UiKit.Rounded(28), Color.white);
            var disc = UiKit.Image(tile.transform, "Disc", UiKit.Circle, colour);
            UiKit.Place(disc.rectTransform, new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(68f, 68f));
            var ic = UiKit.Image(disc.transform, "Icon", icon, Color.white);
            ic.preserveAspect = true;
            UiKit.Stretch(ic.rectTransform, 14f, 14f, 14f, 14f);
            var v = UiKit.Text(tile.transform, "Value", value, 34f, UiKit.Ink, true);
            v.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            v.rectTransform.anchorMax = new Vector2(1f, 1f);
            v.rectTransform.offsetMin = new Vector2(100f, 0f);
            v.rectTransform.offsetMax = new Vector2(-12f, -8f);
            v.textWrappingMode = TextWrappingModes.NoWrap;
            var l = UiKit.Text(tile.transform, "Label", label, 24f, UiKit.InkSoft, false);
            l.rectTransform.anchorMin = Vector2.zero;
            l.rectTransform.anchorMax = new Vector2(1f, 0.45f);
            l.rectTransform.offsetMin = new Vector2(100f, 8f);
            l.rectTransform.offsetMax = new Vector2(-12f, 0f);
            l.textWrappingMode = TextWrappingModes.NoWrap;
        }

        void Hobbies()
        {
            var names = new (Hobby h, string name, Sprite icon)[]
            {
                (Hobby.Fishing, "Fishing", IconPainter.Rod), (Hobby.Birding, "Birdwatching", IconPainter.Bird),
                (Hobby.Bugs, "Bug catching", IconPainter.Sparkle), (Hobby.Gardening, "Gardening", IconPainter.Sparkle),
                (Hobby.Cooking, "Cooking", IconPainter.Star), (Hobby.Cats, "Cat care", IconPainter.Heart),
                (Hobby.Writing, "Writing", IconPainter.Book), (Hobby.Sewing, "Sewing", IconPainter.Hat),
            };
            var grid = UiKit.Rect("Hobbies", _list);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            grid.gameObject.AddComponent<FlexGrid>();
            float cell = 84f * Accessibility.TextScale;
            g.cellSize = new Vector2(440f, cell);
            g.spacing = new Vector2(14f, 12f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 2;
            g.childAlignment = TextAnchor.UpperCenter;
            int rows = (names.Length + 1) / 2;
            grid.gameObject.AddComponent<LayoutElement>().preferredHeight = rows * cell + (rows - 1) * 12f;
            foreach (var n in names)
            {
                var tile = UiKit.Image(grid, n.name, UiKit.Rounded(24), Color.white);
                var ic = UiKit.Image(tile.transform, "Icon", n.icon, UiKit.Hex("3E9C8F"));
                ic.preserveAspect = true;
                UiKit.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(46f, 46f));
                var t = UiKit.Text(tile.transform, "Name", $"{n.name}  <color=#8A7563>Lv {HobbyLog.Level(n.h)}</color>", 28f, UiKit.Ink, true);
                t.rectTransform.anchorMin = Vector2.zero;
                t.rectTransform.anchorMax = Vector2.one;
                t.rectTransform.offsetMin = new Vector2(80f, 0f);
                t.rectTransform.offsetMax = new Vector2(-12f, 0f);
                t.alignment = TextAlignmentOptions.MidlineLeft;
                t.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }
    }
}

namespace CapyIsland.UI
{
    /// <summary>Keeps a grid's columns sharing whatever width it is given, so tiles never spill off a narrow phone.</summary>
    public class FlexGrid : MonoBehaviour
    {
        void OnRectTransformDimensionsChange()
        {
            var grid = GetComponent<UnityEngine.UI.GridLayoutGroup>();
            var rect = (RectTransform)transform;
            if (grid == null || rect.rect.width <= 0f) return;
            int cols = Mathf.Max(1, grid.constraintCount);
            float w = (rect.rect.width - grid.padding.horizontal - grid.spacing.x * (cols - 1)) / cols;
            if (Mathf.Abs(grid.cellSize.x - w) > 0.5f) grid.cellSize = new Vector2(w, grid.cellSize.y);
        }
    }
}
