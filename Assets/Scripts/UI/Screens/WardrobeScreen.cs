using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Dress up your capybara: a big preview on top, a tab per slot, and a grid of pieces. Tap a
    /// piece you own to put it on (tap again to take it off); locked pieces say how to earn them.
    ///
    /// Opened for an adopted cat (<see cref="ShowFor"/>), the same closet dresses the cat instead.
    /// </summary>
    public class WardrobeScreen : UiScreen
    {
        public static WardrobeScreen Instance { get; private set; }

        protected override string Title => "Wardrobe";
        protected override Sprite TitleIcon => IconPainter.Hanger;
        protected override Color AccentColour => UiKit.Hex("E58FB0");

        static readonly WearSlot[] Slots = { WearSlot.Hat, WearSlot.Neck, WearSlot.Face, WearSlot.Back };
        static readonly string[] SlotNames = { "Hats", "Neck", "Face", "Back" };

        RectTransform _stage, _preview;
        Image _base;
        readonly Dictionary<WearSlot, Image> _layers = new();
        List<Button> _tabs;
        WearSlot _slot = WearSlot.Hat;
        RectTransform _grid;
        GridLayoutGroup _gridLayout;
        TextMeshProUGUI _caption;
        CatKind _cat;   // dressing this cat, or null for your capybara

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Wardrobe.Changed -= OnChanged;
            CatWear.Changed -= OnChanged;
        }

        /// <summary>Opens the closet to dress one of your cats.</summary>
        public static void ShowFor(CatKind cat)
        {
            var w = Instance;
            if (w == null || cat == null) return;
            w._cat = cat;
            w.Open();
        }

        WearItem Worn(WearSlot slot) => _cat != null ? CatWear.Worn(HobbyLog.CatState(_cat.Id), slot) : Wardrobe.Worn(slot);

        protected override void Build(RectTransform body)
        {
            // Preview stage: the capybara on a soft spring-coloured disc.
            _stage = UiKit.Rect("Stage", body);
            _stage.anchorMin = new Vector2(0f, 1f);
            _stage.anchorMax = new Vector2(1f, 1f);
            _stage.pivot = new Vector2(0.5f, 1f);
            _stage.sizeDelta = new Vector2(0f, 400f);
            var disc = UiKit.Image(_stage, "Disc", UiKit.Circle, UiKit.Hex("D9F0EC"));
            disc.rectTransform.anchorMin = disc.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            disc.rectTransform.sizeDelta = new Vector2(380f, 380f);

            _preview = UiKit.Rect("Capybara", _stage);
            _preview.anchorMin = _preview.anchorMax = new Vector2(0.5f, 0f);
            _preview.pivot = new Vector2(0.5f, 0f);
            _preview.anchoredPosition = new Vector2(0f, 24f);
            _base = UiKit.Image(_preview, "Body", null, Color.white);
            UiKit.Stretch(_base.rectTransform);

            _caption = UiKit.Text(body, "Caption", "", 28f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            _caption.rectTransform.anchorMin = new Vector2(0f, 1f);
            _caption.rectTransform.anchorMax = new Vector2(1f, 1f);
            _caption.rectTransform.pivot = new Vector2(0.5f, 1f);
            _caption.rectTransform.sizeDelta = new Vector2(0f, 44f);
            _caption.rectTransform.anchoredPosition = new Vector2(0f, -404f);

            _tabs = Tabs(body, SlotNames, i => { _slot = Slots[i]; Refresh(); }, out var bar);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, 92f);
            bar.anchoredPosition = new Vector2(0f, -456f);
            // A dot on a tab with a new piece in it.
            for (int i = 0; i < Slots.Length && i < _tabs.Count; i++)
            {
                string news = "wardrobe/" + Slots[i] + "/";
                NewDot.Live(_tabs[i].transform, () => NewStuff.Any(news), 26f);
            }

            var scroll = UiKit.ScrollList(body, "Pieces", out _grid, 0f, stacked: false);
            UiKit.Stretch((RectTransform)scroll.transform, 0f, 0f, 0f, 564f);
            _grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _gridLayout = _grid.gameObject.AddComponent<GridLayoutGroup>();
            _gridLayout.spacing = new Vector2(16f, 16f);
            _gridLayout.padding = new RectOffset(4, 4, 4, 24);
            _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayout.childAlignment = TextAnchor.UpperCenter;

            Wardrobe.Changed += OnChanged;
            CatWear.Changed += OnChanged;
        }

        void OnChanged()
        {
            if (IsOpen) Refresh();
        }

        protected override void OnClose() => _cat = null;   // next time, it's yours again

        protected override void OnOpen()
        {
            if (TitleLabel != null) TitleLabel.text = _cat != null ? CatCare.NameOf(_cat) + "'s wardrobe" : Title;
            // Open where the new piece is, if there is one.
            if (!NewStuff.Any("wardrobe/" + _slot + "/"))
                foreach (var slot in Slots)
                    if (NewStuff.Any("wardrobe/" + slot + "/")) { _slot = slot; break; }
            Refresh();
        }

        void Refresh()
        {
            HighlightTab(_tabs, System.Array.IndexOf(Slots, _slot), AccentColour);
            DressPreview();
            BuildGrid();
            int owned = 0, total = 0;
            foreach (var i in Wardrobe.AllItems)
            {
                if (Wardrobe.Art(i) == null) continue;
                total++;
                if (Wardrobe.Owns(i)) owned++;
            }
            _caption.text = $"{owned} of {total} pieces collected · tap one to try it on";
        }

        // ---------------------------------------------------------------- preview

        void DressPreview()
        {
            var body = _cat != null ? HobbyData.Art("Cat_" + _cat.Id) : Wardrobe.BaseSprite;
            _base.sprite = body;
            _base.enabled = body != null;
            if (body == null) return;

            float h = 330f, w = h * body.rect.width / body.rect.height;
            _preview.sizeDelta = new Vector2(w, h);

            foreach (var slot in Slots)
            {
                var item = Worn(slot);
                var art = Wardrobe.Art(item);
                var layer = Layer(slot);
                layer.enabled = art != null;
                if (art == null) continue;
                layer.sprite = art;
                var r = layer.rectTransform;
                // On a cat, each piece moves to the same spot on the cat (CatWear).
                var anchor = item.Anchor;
                float share = item.Width;
                if (_cat != null) CatWear.Fit(item, body, false, out anchor, out share);
                r.anchorMin = r.anchorMax = anchor;
                r.pivot = new Vector2(0.5f, 0.5f);
                float pw = share * w;
                r.sizeDelta = new Vector2(pw, pw * art.rect.height / art.rect.width);
                r.anchoredPosition = Vector2.zero;
                r.localRotation = Quaternion.Euler(0f, 0f, item.Rotation);
                // Behind pieces go under the body; the rest stack neck, back, face, hat.
                if (item.Behind) layer.transform.SetSiblingIndex(0);
                else layer.transform.SetAsLastSibling();
            }
            // Hats always end on top.
            if (_layers.TryGetValue(WearSlot.Face, out var face)) face.transform.SetAsLastSibling();
            if (_layers.TryGetValue(WearSlot.Hat, out var hat)) hat.transform.SetAsLastSibling();
        }

        Image Layer(WearSlot slot)
        {
            if (_layers.TryGetValue(slot, out var img) && img != null) return img;
            img = UiKit.Image(_preview, slot.ToString(), null, Color.white);
            img.preserveAspect = true;
            _layers[slot] = img;
            return img;
        }

        // ---------------------------------------------------------------- pieces

        void BuildGrid()
        {
            ClearChildren(_grid);
            // What you have first, then what's still to earn (each in its usual order).
            var items = new List<WearItem>(Wardrobe.AllItems);
            var ordered = items.FindAll(Wardrobe.Owns);
            ordered.AddRange(items.FindAll(i => !Wardrobe.Owns(i)));
            foreach (var item in ordered)
            {
                if (item.Slot != _slot) continue;
                var art = Wardrobe.Art(item);
                if (art == null) continue;   // still being drawn
                bool owned = Wardrobe.Owns(item);
                bool worn = Worn(item.Slot)?.Id == item.Id;

                var tile = UiKit.Image(_grid, item.Id, UiKit.Rounded(28), worn ? UiKit.Hex("FDE8F0") : owned ? Color.white : UiKit.SurfaceSunk, true);
                var pic = UiKit.Image(tile.transform, "Art", art, owned ? Color.white : new Color(0.25f, 0.2f, 0.18f, 0.35f));
                pic.preserveAspect = true;
                pic.rectTransform.anchorMin = new Vector2(0.12f, 0.34f);
                pic.rectTransform.anchorMax = new Vector2(0.88f, 0.92f);
                pic.rectTransform.offsetMin = pic.rectTransform.offsetMax = Vector2.zero;

                var name = UiKit.Text(tile.transform, "Name", owned ? item.Name : Wardrobe.HowToEarn(item), owned ? 26f : 20f,
                    owned ? UiKit.Ink : UiKit.InkSoft, owned, TextAlignmentOptions.Center);
                name.rectTransform.anchorMin = Vector2.zero;
                name.rectTransform.anchorMax = new Vector2(1f, 0.34f);
                name.rectTransform.offsetMin = new Vector2(8f, 6f);
                name.rectTransform.offsetMax = new Vector2(-8f, 0f);
                name.enableAutoSizing = true;
                name.fontSizeMin = 16f;
                name.fontSizeMax = owned ? 26f : 20f;

                if (!owned)
                {
                    var lockDisc = UiKit.Image(tile.transform, "Lock", UiKit.Circle, UiKit.Ink);
                    UiKit.Place(lockDisc.rectTransform, new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(44f, 44f));
                    var ic = UiKit.Image(lockDisc.transform, "Icon", IconPainter.Lock, Color.white);
                    UiKit.Stretch(ic.rectTransform, 10f, 10f, 10f, 10f);
                }
                else if (worn)
                {
                    var tickDisc = UiKit.Image(tile.transform, "Worn", UiKit.Circle, UiKit.Leaf);
                    UiKit.Place(tickDisc.rectTransform, new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(44f, 44f));
                    var ic = UiKit.Image(tickDisc.transform, "Icon", IconPainter.Check, Color.white);
                    UiKit.Stretch(ic.rectTransform, 9f, 9f, 9f, 9f);
                }

                var button = tile.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                var chosen = item;
                button.onClick.AddListener(() =>
                {
                    if (!Wardrobe.Owns(chosen)) { Sfx.Play(SfxKind.Tap, 0.3f); return; }
                    Sfx.Play(SfxKind.Plop, 0.7f);
                    if (_cat != null) CatWear.Toggle(HobbyLog.CatState(_cat.Id), chosen);
                    else Wardrobe.Toggle(chosen);
                });
                tile.gameObject.AddComponent<PressSquash>();
                if (NewStuff.Has("wardrobe/" + item.Slot + "/" + item.Id)) NewDot.Fixed(tile.transform, 30f);
            }
            NewStuff.Clear("wardrobe/" + _slot + "/");   // seen
        }

        protected override void Fit()
        {
            base.Fit();
            if (Sheet == null || _gridLayout == null) return;
            float width = Sheet.sizeDelta.x - 64f - 8f;
            int columns = Mathf.Clamp(Mathf.FloorToInt(width / 230f), 3, 5);
            float cell = (width - _gridLayout.spacing.x * (columns - 1)) / columns;
            _gridLayout.cellSize = new Vector2(cell, cell * 1.12f);
            _gridLayout.constraintCount = columns;
        }
    }
}
