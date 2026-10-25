using System;
using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// A hobby card to spend (Game.HobbyCards): the closed hobbies as picture cards, tap one, then
    /// confirm. The hobby opens and its character says where to start. "Later" keeps the card; it
    /// waits in the Hobbies hub.
    ///
    /// Not made by the HUD: built beside the Hobbies screen the first time it is needed.
    /// </summary>
    public class HobbyCardScreen : UiScreen
    {
        public static HobbyCardScreen Instance { get; private set; }

        protected override string Title => StoryText.HobbyCardLines.Title;
        protected override Sprite TitleIcon => IconPainter.Star;
        protected override Color AccentColour => UiKit.Hex("E58FB0");
        protected override float MaxWidth => 900f;

        // A few pictures and two buttons: a small card in the middle, not a tall page.
        protected override bool FitToContent => true;
        protected override RectTransform ContentRoot => _list;

        RectTransform _list;
        /// <summary>The sheet's width the hobby pictures were sized for (they are redrawn if it turns).</summary>
        float _builtWidth;
        ScrollRect _scroll;
        Hobby? _picked;
        Button _confirm;
        TextMeshProUGUI _confirmLabel;
        readonly List<(Hobby hobby, GameObject ring, Image face)> _tiles = new();

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Opens the choice (the card waiting its turn has come). False if there's no HUD to show it in.</summary>
        public static bool Offer(Hobby? preselect = null, Action closed = null)
        {
            if (Instance == null)
            {
                var hub = HobbiesScreen.Instance;
                if (hub == null || hub.transform.parent == null) return false;
                var r = UiKit.Rect("HobbyCardScreen", hub.transform.parent);
                UiKit.Stretch(r);
                r.gameObject.AddComponent<HobbyCardScreen>();
            }
            Instance._picked = preselect;
            Instance.Open(closed);
            return true;
        }

        protected override void Build(RectTransform body)
        {
            _scroll = UiKit.ScrollList(body, "List", out _list, 18f, new RectOffset(4, 4, 8, 32));
            UiKit.Stretch((RectTransform)_scroll.transform);
        }

        protected override void OnOpen() => Refresh();

        void Refresh()
        {
            ClearChildren(_list);
            _tiles.Clear();
            _confirm = null;
            int waiting = HobbyCards.Waiting;
            var closed = HobbyCards.Choices();
            if (_picked is Hobby p && !closed.Contains(p)) _picked = null;

            string head = waiting > 1 ? Loc.F(StoryText.HobbyCardLines.EarnedMany, waiting) : StoryText.HobbyCardLines.EarnedOne;
            if (waiting <= 0)
                head = closed.Count == 0 ? StoryText.HobbyCardLines.AllOpen
                    : HobbyCards.HeartsToNext is int more ? Loc.F(StoryText.HobbyCardLines.NextAt, HobbyCards.Hearts + more)
                    : StoryText.HobbyCardLines.LockedLater;
            var title = UiKit.Text(_list, "Head", head, 40f, UiKit.Ink, true, TextAlignmentOptions.Center);
            title.overflowMode = TextOverflowModes.Overflow;

            if (waiting > 0)
            {
                var pick = UiKit.Text(_list, "Pick", HobbyCards.FirstCard ? StoryText.HobbyCardLines.PickFirst : StoryText.HobbyCardLines.Pick, 28f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
                pick.overflowMode = TextOverflowModes.Overflow;
                Grid(closed);

                _confirm = UiKit.Button(_list, "Confirm", StoryText.HobbyCardLines.Choose, UiKit.ButtonStyle.Primary, Confirm, 36f);
                _confirm.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 100f;
                _confirmLabel = _confirm.GetComponentInChildren<TextMeshProUGUI>();
            }

            var later = UiKit.Button(_list, "Later", waiting > 0 ? StoryText.HobbyCardLines.Later : "Close", UiKit.ButtonStyle.Secondary, Close, 30f);
            later.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 80f;
            ShowPicked();
            _scroll.verticalNormalizedPosition = 1f;
            _builtWidth = Sheet.sizeDelta.x;
        }

        protected override void Fit()
        {
            base.Fit();
            // Turned between portrait and landscape while open: the pictures are re-cut to the new width.
            if (IsOpen && _list != null && _builtWidth > 0f && Mathf.Abs(Sheet.sizeDelta.x - _builtWidth) > 1f) Refresh();
        }

        void Grid(List<Hobby> hobbies)
        {
            var grid = UiKit.Rect("Grid", _list);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            float width = Sheet.sizeDelta.x - 64f - 8f;
            int columns = Mathf.Clamp(Mathf.FloorToInt(width / 260f), 2, 4);
            float cell = (width - 16f * (columns - 1)) / columns;
            g.cellSize = new Vector2(cell, cell * 1.18f);
            g.spacing = new Vector2(16f, 16f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columns;
            g.childAlignment = TextAnchor.UpperCenter;
            int rows = Mathf.CeilToInt(hobbies.Count / (float)columns);
            grid.gameObject.AddComponent<LayoutElement>().preferredHeight = rows * (cell * 1.18f + 16f);
            foreach (var h in hobbies) Tile(grid, h);
        }

        /// <summary>One hobby: the capybara at it, and its name. Tap to pick it.</summary>
        void Tile(RectTransform grid, Hobby hobby)
        {
            var root = UiKit.Rect(hobby.ToString(), grid);
            var ring = UiKit.Image(root, "Ring", UiKit.Rounded(36), AccentColour);
            UiKit.Stretch(ring.rectTransform);
            var face = UiKit.Image(root, "Face", UiKit.Rounded(32), Color.white, true);
            UiKit.Stretch(face.rectTransform, 7f, 7f, 7f, 7f);

            var disc = UiKit.Image(face.transform, "Disc", IconPainter.Glow, new Color(AccentColour.r, AccentColour.g, AccentColour.b, 0.25f));
            disc.preserveAspect = true;
            var dr = disc.rectTransform;
            dr.anchorMin = new Vector2(0f, 0.24f);
            dr.anchorMax = Vector2.one;
            dr.offsetMin = new Vector2(8f, 0f);
            dr.offsetMax = new Vector2(-8f, -8f);
            var pic = UiKit.Image(face.transform, "Picture", HobbyData.Art("Vignette_" + hobby), Color.white);
            pic.preserveAspect = true;
            var pr = pic.rectTransform;
            pr.anchorMin = new Vector2(0f, 0.24f);
            pr.anchorMax = Vector2.one;
            pr.offsetMin = new Vector2(14f, 0f);
            pr.offsetMax = new Vector2(-14f, -14f);

            var name = UiKit.Text(face.transform, "Name", StoryText.HobbyCardLines.Name(hobby), 32f, UiKit.Ink, true, TextAlignmentOptions.Center);
            var nr = name.rectTransform;
            nr.anchorMin = Vector2.zero;
            nr.anchorMax = new Vector2(1f, 0.24f);
            nr.offsetMin = new Vector2(8f, 6f);
            nr.offsetMax = new Vector2(-8f, 0f);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.enableAutoSizing = true;
            name.fontSizeMin = 18f;
            name.fontSizeMax = 32f;

            var button = face.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                Sfx.Play(SfxKind.Tap, 0.5f);
                _picked = hobby;
                ShowPicked();
                UiKit.PopIn(root, 0.92f);
            });
            face.gameObject.AddComponent<PressSquash>();
            _tiles.Add((hobby, ring.gameObject, face));
        }

        /// <summary>The picked card stands out; the button says what it will open.</summary>
        void ShowPicked()
        {
            foreach (var (hobby, ring, face) in _tiles)
            {
                bool on = _picked == hobby;
                ring.SetActive(on);
                face.color = on ? UiKit.Hex("FFF4EC") : Color.white;
            }
            if (_confirm == null) return;
            _confirm.interactable = _picked.HasValue;
            if (_confirmLabel != null)
                _confirmLabel.text = _picked is Hobby h ? string.Format(StoryText.HobbyCardLines.Confirm, StoryText.HobbyCardLines.Name(h)) : StoryText.HobbyCardLines.Choose;
        }

        void Confirm()
        {
            if (_picked is not Hobby h) return;
            _picked = null;
            HobbyCards.Choose(h);   // opened first, so whatever refreshes as this closes sees it
            Close();
        }
    }
}
