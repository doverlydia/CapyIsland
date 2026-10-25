using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The welcome-back popup: seven gift tiles for the week's streak, today's glowing, the ones
    /// already collected ticked off, and one big button to collect (or tap today's tile). It opens
    /// by itself the first time you play each day.
    /// </summary>
    public class DailyGiftScreen : UiScreen
    {
        public static DailyGiftScreen Instance { get; private set; }

        protected override string Title => "Daily Gifts";
        protected override Sprite TitleIcon => IconPainter.Calendar;
        protected override Color AccentColour => UiKit.Hex("E3A92B");
        protected override float MaxWidth => 1000f;

        GridLayoutGroup _grid;
        LayoutElement _gridSize;
        RectTransform _gridRect;
        TextMeshProUGUI _subtitle;
        Button _claim;
        TextMeshProUGUI _claimLabel;
        readonly List<RectTransform> _tiles = new();

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override void Build(RectTransform body)
        {
            var v = body.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 22f;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            _subtitle = UiKit.Text(body, "Subtitle", "", 30f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            _subtitle.overflowMode = TextOverflowModes.Overflow;

            _gridRect = UiKit.Rect("Days", body);
            _grid = _gridRect.gameObject.AddComponent<GridLayoutGroup>();
            _grid.spacing = new Vector2(16f, 16f);
            _grid.childAlignment = TextAnchor.UpperCenter;
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = 4;
            _gridSize = _gridRect.gameObject.AddComponent<LayoutElement>();

            for (int i = 0; i < DailyGifts.Week.Length; i++) _tiles.Add(Tile(i));

            _claim = UiKit.Button(body, "Claim", "Collect", UiKit.ButtonStyle.Primary, Claim, 40f, IconPainter.Gift);
            var le = _claim.transform.parent.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = 110f;
            _claimLabel = _claim.GetComponentInChildren<TextMeshProUGUI>();
        }

        RectTransform Tile(int index)
        {
            var gift = DailyGifts.Week[index];
            var tile = UiKit.Image(_gridRect, "Day" + (index + 1), UiKit.Rounded(28), Color.white, true).rectTransform;
            // Today's glowing tile collects too, just like the button under them.
            var tap = tile.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => { if (DailyGifts.CanClaim && DailyGifts.CurrentDay == index + 1) Claim(); });

            var day = UiKit.Text(tile, "Day", "Day " + (index + 1), 26f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
            day.rectTransform.anchorMin = new Vector2(0f, 1f);
            day.rectTransform.anchorMax = new Vector2(1f, 1f);
            day.rectTransform.pivot = new Vector2(0.5f, 1f);
            day.rectTransform.sizeDelta = new Vector2(0f, 44f);
            day.rectTransform.anchoredPosition = new Vector2(0f, -8f);

            Sprite icon; string amount;
            if (gift.Outfit) { icon = IconPainter.Hanger; amount = "Outfit!"; }
            else if (gift.Gems > 0) { icon = UiKit.ResourceIcon(ResourceKind.Gem); amount = "+" + gift.Gems; }
            else if (gift.Wood > 0 && gift.Gold > 0) { icon = UiKit.ResourceIcon(ResourceKind.Gold); amount = $"+{gift.Wood}/{gift.Gold}"; }
            else if (gift.Gold > 0) { icon = UiKit.ResourceIcon(ResourceKind.Gold); amount = "+" + gift.Gold; }
            else { icon = UiKit.ResourceIcon(ResourceKind.Wood); amount = "+" + gift.Wood; }

            var pic = UiKit.Image(tile, "Icon", icon, gift.Outfit ? UiKit.Hex("E58FB0") : Color.white);
            pic.preserveAspect = true;
            pic.rectTransform.anchorMin = new Vector2(0.2f, 0.32f);
            pic.rectTransform.anchorMax = new Vector2(0.8f, 0.76f);
            pic.rectTransform.offsetMin = pic.rectTransform.offsetMax = Vector2.zero;
            if (gift.Wood > 0 && gift.Gold > 0)
            {
                // Both: a little wood icon tucked behind the gold.
                var second = UiKit.Image(tile, "Icon2", UiKit.ResourceIcon(ResourceKind.Wood), Color.white);
                second.preserveAspect = true;
                second.rectTransform.anchorMin = new Vector2(0.08f, 0.36f);
                second.rectTransform.anchorMax = new Vector2(0.5f, 0.68f);
                second.rectTransform.offsetMin = second.rectTransform.offsetMax = Vector2.zero;
                second.transform.SetSiblingIndex(pic.transform.GetSiblingIndex());
            }

            var label = UiKit.Text(tile, "Amount", amount, 30f, UiKit.Ink, true, TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = new Vector2(1f, 0f);
            label.rectTransform.pivot = new Vector2(0.5f, 0f);
            label.rectTransform.sizeDelta = new Vector2(-12f, 46f);
            label.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 30f;
            label.textWrappingMode = TextWrappingModes.NoWrap;

            var done = UiKit.Image(tile, "Done", UiKit.Rounded(28), new Color(UiKit.Leaf.r, UiKit.Leaf.g, UiKit.Leaf.b, 0.82f));
            UiKit.Stretch(done.rectTransform);
            var tick = UiKit.Image(done.transform, "Tick", IconPainter.Check, Color.white);
            tick.preserveAspect = true;
            tick.rectTransform.anchorMin = new Vector2(0.25f, 0.25f);
            tick.rectTransform.anchorMax = new Vector2(0.75f, 0.75f);
            tick.rectTransform.offsetMin = tick.rectTransform.offsetMax = Vector2.zero;
            return tile;
        }

        protected override void OnOpen()
        {
            Refresh();
            Fit();   // again, now the line at the top has its words to measure
        }

        void Refresh()
        {
            int today = DailyGifts.CurrentDay;
            bool can = DailyGifts.CanClaim;
            for (int i = 0; i < _tiles.Count; i++)
            {
                int day = i + 1;
                bool collected = day < today || (day == today && !can);
                bool isToday = day == today && can;
                var tile = _tiles[i];
                tile.Find("Done").gameObject.SetActive(collected);
                tile.GetComponent<Image>().color = isToday ? UiKit.Hex("FFF1C9") : day == 7 ? UiKit.Hex("FDE8F0") : Color.white;
                tile.DOKill();
                tile.localScale = Vector3.one;
                if (isToday) tile.DOScale(1.06f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
            }

            _subtitle.text = can
                ? (today == 1 ? "Welcome! Come back every day and the gifts grow bigger." : $"Day {today} of your streak — keep it going!")
                    + "\n<size=80%>Miss a day and the streak starts again from day 1.</size>"
                : "You've collected today's gift. See you tomorrow!";
            _claim.interactable = can;
            _claimLabel.text = can ? $"Collect day {today}" : "Come back tomorrow";
            // Spent for today: a quiet grey button rather than a dimmed orange one.
            _claim.GetComponent<Image>().color = can ? UiKit.Primary : UiKit.SurfaceSunk;
            _claim.transform.parent.Find("Lip").GetComponent<Image>().color = can ? UiKit.PrimaryDark : UiKit.Hex("DCCFBD");
            foreach (var g in _claim.GetComponentsInChildren<Graphic>())
                if (g != _claim.targetGraphic) g.color = can ? Color.white : UiKit.InkSoft;
            var colours = _claim.colors;
            colours.disabledColor = Color.white;
            _claim.colors = colours;
        }

        void Claim()
        {
            if (!DailyGifts.CanClaim) return;
            int day = DailyGifts.CurrentDay;
            var outfit = DailyGifts.Claim();
            Sfx.Play(SfxKind.Chest, 0.8f);
            var gift = DailyGifts.Week[day - 1];
            if (gift.Wood > 0) ResourceBar.Bump(ResourceKind.Wood);
            if (gift.Gold > 0) ResourceBar.Bump(ResourceKind.Gold);
            if (gift.Gems > 0 || (gift.Outfit && outfit == null)) ResourceBar.Bump(ResourceKind.Gem);
            var tile = _tiles[day - 1];
            Refresh();
            tile.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.5f, 6, 0.6f).SetUpdate(true);
        }

        protected override void Fit()
        {
            base.Fit();
            if (Sheet == null || _grid == null) return;
            float width = Sheet.sizeDelta.x - 64f;
            float cell = (width - _grid.spacing.x * 3f) / 4f;
            _grid.cellSize = new Vector2(cell, Mathf.Min(cell * 1.15f, 260f));
            _gridSize.preferredHeight = 2f * _grid.cellSize.y + _grid.spacing.y;
            // A short card, sized to its content: the line at the top as it will be drawn (translated,
            // and at Easy Reading's size), at least the two lines it was made for.
            _subtitle.GetComponent<LocText>()?.Refresh();
            float subtitle = Mathf.Max(100f, _subtitle.GetPreferredValues(_subtitle.text, width, 0f).y + 8f);
            float h = 128f + 24f + subtitle + 2f * (_grid.cellSize.y) + _grid.spacing.y + 110f + 22f * 2f + 32f;
            Sheet.sizeDelta = new Vector2(Sheet.sizeDelta.x, Mathf.Min(Sheet.sizeDelta.y, h));
        }
    }
}
