using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The hub behind the ☰ button: big friendly tiles for every screen in the game. Things that are
    /// on their way show a "Soon" ribbon instead of hiding, so the player knows they are coming.
    /// </summary>
    public class MenuScreen : UiScreen
    {
        public static MenuScreen Instance { get; private set; }

        protected override string Title => "Menu";
        protected override Sprite TitleIcon => IconPainter.Menu;
        protected override float MaxWidth => 1100f;

        struct Entry
        {
            public string Label;
            public Sprite Icon;
            public Color Colour;
            public Func<bool> Available;
            public Action Open;
        }

        readonly List<Entry> _entries = new();
        readonly List<(Entry entry, GameObject soon, Button button)> _tiles = new();
        GridLayoutGroup _grid;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Adds a tile. Screens register themselves so the menu never needs to know them all.</summary>
        public void AddEntry(string label, Sprite icon, Color colour, Action open, Func<bool> available = null)
        {
            _entries.Add(new Entry { Label = label, Icon = icon, Colour = colour, Open = open, Available = available });
        }

        protected override void Build(RectTransform body)
        {
            var scroll = UiKit.ScrollList(body, "Tiles", out var content, 0f, stacked: false);
            UiKit.Stretch((RectTransform)scroll.transform);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _grid = content.gameObject.AddComponent<GridLayoutGroup>();
            _grid.spacing = new Vector2(24f, 24f);
            _grid.padding = new RectOffset(4, 4, 8, 24);
            _grid.childAlignment = TextAnchor.UpperCenter;

            foreach (var e in _entries)
            {
                var entry = e;
                var tile = UiKit.Rect(e.Label, content);
                var lip = UiKit.Image(tile, "Lip", UiKit.Rounded(36), Color.Lerp(e.Colour, Color.black, 0.25f));
                UiKit.Stretch(lip.rectTransform);
                var face = UiKit.Image(tile, "Face", UiKit.Rounded(36), e.Colour, true);
                UiKit.Stretch(face.rectTransform, 0f, 8f, 0f, 0f);

                var ic = UiKit.Image(face.transform, "Icon", e.Icon, Color.white);
                ic.preserveAspect = true;
                ic.rectTransform.anchorMin = new Vector2(0.5f, 0.42f);
                ic.rectTransform.anchorMax = new Vector2(0.5f, 0.42f);
                ic.rectTransform.sizeDelta = new Vector2(110f, 110f);
                ic.rectTransform.anchoredPosition = new Vector2(0f, 30f);

                var t = UiKit.Text(face.transform, "Label", e.Label, 38f, Color.white, true, TextAlignmentOptions.Center);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.enableAutoSizing = true;
                t.fontSizeMin = 24f;
                t.fontSizeMax = 38f;
                t.rectTransform.anchorMin = new Vector2(0f, 0f);
                t.rectTransform.anchorMax = new Vector2(1f, 0f);
                t.rectTransform.pivot = new Vector2(0.5f, 0f);
                t.rectTransform.sizeDelta = new Vector2(-24f, 60f);
                t.rectTransform.anchoredPosition = new Vector2(0f, 26f);

                var soon = UiKit.Image(face.transform, "Soon", UiKit.Rounded(20), UiKit.Ink);
                UiKit.Place(soon.rectTransform, new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(110f, 44f));
                var st = UiKit.Text(soon.transform, "Label", "Soon", 26f, Color.white, true, TextAlignmentOptions.Center);
                UiKit.Stretch(st.rectTransform);

                var b = face.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() =>
                {
                    Game.Sfx.Play(Game.SfxKind.Tap, 0.5f);
                    if (entry.Available != null && !entry.Available()) return;
                    entry.Open?.Invoke();
                });
                face.gameObject.AddComponent<PressSquash>();
                _tiles.Add((entry, soon.gameObject, b));
            }
        }

        protected override void OnOpen()
        {
            foreach (var (entry, soon, button) in _tiles)
            {
                bool ok = entry.Available == null || entry.Available();
                soon.SetActive(!ok);
                var group = button.GetComponent<CanvasGroup>();
                if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
                group.alpha = ok ? 1f : 0.55f;
            }
            // The menu's own tip (the red dot on its button) is told here, over the menu itself,
            // rather than waiting for it to close.
            if (HudTour.Wants("menu"))
            {
                HudTour.Withdraw("menu");
                CardPopup.Show("Menu", "Everything else lives here: your goals, the wardrobe, hobbies, friends, and Settings (bigger text, language, sound).", IconPainter.Menu);
            }
        }

        protected override void Fit()
        {
            base.Fit();
            if (_grid == null || Sheet == null) return;
            float width = Sheet.sizeDelta.x - 64f - 8f;
            int columns = width > 900f ? 3 : 2;
            float cell = (width - _grid.spacing.x * (columns - 1)) / columns;
            _grid.cellSize = new Vector2(cell, Mathf.Min(cell * 0.86f, 300f));
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = columns;

            // Only as tall as its tiles: a short card, not a page of empty cream.
            int rows = Mathf.CeilToInt(_entries.Count / (float)columns);
            float content = rows * _grid.cellSize.y + (rows - 1) * _grid.spacing.y + _grid.padding.vertical;
            float wanted = 128f + 24f + content + 32f;
            Sheet.sizeDelta = new Vector2(Sheet.sizeDelta.x, Mathf.Min(Sheet.sizeDelta.y, wanted));
        }
    }
}
