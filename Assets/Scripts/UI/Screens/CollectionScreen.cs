using System;
using System.Collections.Generic;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Everything you have found, a tab for each kind: the island's discovery cards, clothing,
    /// furniture, recipes, fish, birds, bugs, vegetables and cats. Found things show their picture
    /// and name; the rest are greyed, with a word on how to get them — a quiet list of what is
    /// still out there.
    /// </summary>
    public class CollectionScreen : UiScreen
    {
        public static CollectionScreen Instance { get; private set; }

        protected override string Title => "Collection";
        protected override Sprite TitleIcon => IconPainter.Cards;
        protected override Color AccentColour => UiKit.Hex("A77BD6");

        enum Tab { Discoveries, Clothing, Furniture, Recipes, Fish, Birds, Bugs, Vegetables, Cats }
        static readonly string[] TabNames = { "Discoveries", "Clothing", "Furniture", "Recipes", "Fish", "Birds", "Bugs", "Vegetables", "Cats" };

        /// <summary>Where each tab's new things are kept (<see cref="NewStuff"/>), or null for none.</summary>
        static readonly string[] TabNews = { "collection/", "wardrobe/", null, null, "hobbies/Fishing/", "hobbies/Birding/", "hobbies/Bugs/", "hobbies/Gardening/", "hobbies/Cats/" };

        /// <summary>Anything new on any tab (the menu tile's dot).</summary>
        public static bool AnyNews()
        {
            foreach (var prefix in TabNews) if (prefix != null && NewStuff.Any(prefix)) return true;
            return false;
        }

        Tab _tab;
        RectTransform _content, _chips;
        ScrollRect _scroll;
        GridLayoutGroup _grid;
        TextMeshProUGUI _count;
        readonly List<Image> _chipFaces = new();

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override void Build(RectTransform body)
        {
            // The tabs: chips in a row you can slide sideways.
            var bar = UiKit.Rect("Tabs", body);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = Vector2.one;
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, 84f);
            bar.anchoredPosition = Vector2.zero;
            bar.gameObject.AddComponent<RectMask2D>();
            var hit = bar.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            _chips = UiKit.Rect("Chips", bar);
            _chips.anchorMin = new Vector2(0f, 0f);
            _chips.anchorMax = new Vector2(0f, 1f);
            _chips.pivot = new Vector2(0f, 0.5f);
            _chips.offsetMin = _chips.offsetMax = Vector2.zero;
            var h = _chips.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.padding = new RectOffset(4, 4, 6, 6);
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;   // each chip as tall as the bar
            h.childAlignment = TextAnchor.MiddleLeft;
            _chips.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var chipScroll = bar.gameObject.AddComponent<ScrollRect>();
            chipScroll.content = _chips;
            chipScroll.viewport = bar;
            chipScroll.horizontal = true;
            chipScroll.vertical = false;
            chipScroll.movementType = ScrollRect.MovementType.Clamped;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;
                var chip = UiKit.Image(_chips, TabNames[i], UiKit.Rounded(34), UiKit.SurfaceSunk, true);
                var t = UiKit.Text(chip.transform, "Label", TabNames[i], 30f, UiKit.Ink, true, TextAlignmentOptions.Center);
                UiKit.Stretch(t.rectTransform, 22f, 0f, 22f, 0f);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                var le = chip.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = t.GetPreferredValues(TabNames[i]).x + 56f;
                var b = chip.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { _tab = (Tab)index; Refresh(); });
                chip.gameObject.AddComponent<PressSquash>();
                var news = TabNews[i];
                if (news != null) NewDot.Live(chip.transform, () => NewStuff.Any(news), 26f);
                _chipFaces.Add(chip);
            }

            _count = UiKit.Text(body, "Count", "", 30f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
            _count.rectTransform.anchorMin = new Vector2(0f, 1f);
            _count.rectTransform.anchorMax = new Vector2(1f, 1f);
            _count.rectTransform.pivot = new Vector2(0.5f, 1f);
            _count.rectTransform.sizeDelta = new Vector2(0f, 50f);
            _count.rectTransform.anchoredPosition = new Vector2(0f, -96f);

            _scroll = UiKit.ScrollList(body, "Grid", out _content, 0f, stacked: false);
            UiKit.Stretch((RectTransform)_scroll.transform, 0f, 0f, 0f, 156f);
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _grid = _content.gameObject.AddComponent<GridLayoutGroup>();
            _grid.spacing = new Vector2(18f, 18f);
            _grid.padding = new RectOffset(4, 4, 8, 24);
            _grid.childAlignment = TextAnchor.UpperCenter;
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        }

        protected override void OnOpen() => Refresh();

        void Refresh()
        {
            for (int i = 0; i < _chipFaces.Count; i++)
            {
                bool on = i == (int)_tab;
                _chipFaces[i].color = on ? AccentColour : UiKit.SurfaceSunk;
                _chipFaces[i].GetComponentInChildren<TMP_Text>().color = on ? Color.white : UiKit.Ink;
            }
            ClearChildren(_content);
            int found = 0, total = 0;
            void Add(Sprite pic, bool have, string name, string detail, string key = null)
            {
                total++;
                if (have) found++;
                var tile = Tile(pic, have, have ? name : "???", detail);
                if (key != null && NewStuff.Has(key)) NewDot.Fixed(tile, 30f);   // new since you last looked
            }

            int cooking = HobbyLog.Level(Hobby.Cooking);
            switch (_tab)
            {
                case Tab.Discoveries:
                {
                    var catalog = ItemCatalog.Instance;
                    foreach (var card in QuestData.Cards)
                    {
                        bool have = Cards.IsCollected(card.Id);
                        var pic = have && catalog != null && card.Items.Length > 0 ? catalog.Icon(card.Items[0]) : null;
                        string title = StoryText.CardTitle(card.Id);
                        if (string.IsNullOrEmpty(title)) title = card.Items.Length > 0 ? StoryText.Name(card.Items[0]) : "";
                        Add(pic, have, title, have ? null : Whereabouts.Card(card), "collection/" + card.Id);
                    }
                    break;
                }
                case Tab.Clothing:
                    foreach (var item in Wardrobe.Items)
                    {
                        bool have = Wardrobe.Owns(item);
                        Add(Wardrobe.Art(item), have, item.Name, have ? null : Wardrobe.HowToEarn(item), "wardrobe/" + item.Slot + "/" + item.Id);
                    }
                    break;
                case Tab.Furniture:
                    foreach (var f in Furniture.All)
                    {
                        int placed = 0;
                        foreach (var p in HomeLog.Room) if (p.id == f.Id) placed++;
                        bool have = placed + HomeLog.InStorage(f.Id) > 0;
                        Add(f.Art, have, f.Name, have ? null : "From the Furniture Shop");
                    }
                    break;
                case Tab.Recipes:
                    foreach (var d in HobbyData.Dishes)
                    {
                        bool have = HobbyLog.KnowsRecipe(d.Id);
                        Add(HobbyData.ItemArt("dish:" + d.Id), have, d.Name, have ? null : Whereabouts.Dish(d));
                    }
                    break;
                case Tab.Fish:
                    foreach (var f in HobbyData.Fish)
                    {
                        var e = HobbyLog.FishEntry(f.Id);
                        Add(HobbyData.FishPicture(f), e != null, f.Name, e != null ? $"×{e.count} · best {e.best:0} cm" : Whereabouts.Fish(f), "hobbies/Fishing/" + f.Id);
                    }
                    break;
                case Tab.Birds:
                    foreach (var b in HobbyData.Birds)
                    {
                        var e = HobbyLog.BirdEntry(b.Id);
                        Add(HobbyData.BirdPicture(b), e != null, b.Name, e != null ? $"×{e.count}" : Whereabouts.Bird(b), "hobbies/Birding/" + b.Id);
                    }
                    break;
                case Tab.Bugs:
                    foreach (var b in HobbyData.Bugs)
                    {
                        var e = HobbyLog.BugEntry(b.Id);
                        Add(HobbyData.Art("Bug_" + b.Id), e != null, b.Name, e != null ? $"×{e.count}" : Whereabouts.Bug(b), "hobbies/Bugs/" + b.Id);
                    }
                    break;
                case Tab.Vegetables:
                    foreach (var c in HobbyData.Crops)
                    {
                        var e = HobbyLog.CropEntry(c.Id);
                        Add(HobbyData.ItemArt("crop:" + c.Id), e != null, c.Name, e != null ? Loc.F("×{0} harvested", e.count) : Whereabouts.Crop(c), "hobbies/Gardening/" + c.Id);
                    }
                    break;
                case Tab.Cats:
                    foreach (var cat in HobbyData.Cats)
                    {
                        HobbyLog.Cat state = null;
                        foreach (var c in HobbyLog.Cats) if (c.id == cat.Id) state = c;
                        bool adopted = state != null && state.adopted;
                        Add(HobbyData.Art("Cat_" + cat.Id), adopted, cat.Name, adopted ? null : state != null && state.bond > 0 ? $"Trust {state.bond}/{CatCare.MaxBond}" : Whereabouts.Cat, "hobbies/Cats/" + cat.Id);
                    }
                    break;
            }
            _count.text = Loc.F("{0} of {1} found", found, total);
            // Seen now: this tab's dots (here, on the menu, in the hobby journals) go.
            if (TabNews[(int)_tab] != null) NewStuff.Clear(TabNews[(int)_tab]);
            _scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>One thing in the collection: its picture (a shadow if not found yet), its name, a small line under it.</summary>
        RectTransform Tile(Sprite pic, bool have, string name, string detail)
        {
            // Not found yet: the shadow gets smaller and where-and-when gets the room.
            bool hint = !have && !string.IsNullOrEmpty(detail);
            float detailTop = hint ? 0.34f : 0.19f, labelTop = hint ? 0.48f : 0.36f;
            var tile = UiKit.Image(_content, name, UiKit.Rounded(26), have ? Color.white : UiKit.SurfaceSunk);
            var p = UiKit.Image(tile.transform, "Picture", pic, have ? Color.white : new Color(0.2f, 0.16f, 0.13f, 0.4f));
            p.preserveAspect = true;
            p.rectTransform.anchorMin = new Vector2(0f, labelTop);
            p.rectTransform.anchorMax = Vector2.one;
            p.rectTransform.offsetMin = new Vector2(16f, 0f);
            p.rectTransform.offsetMax = new Vector2(-16f, -12f);
            if (pic == null)
            {
                p.enabled = false;
                var q = UiKit.Text(tile.transform, "Unknown", "?", 90f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
                q.rectTransform.anchorMin = new Vector2(0f, labelTop);
                q.rectTransform.anchorMax = Vector2.one;
                q.rectTransform.offsetMin = q.rectTransform.offsetMax = Vector2.zero;
            }
            var label = UiKit.Text(tile.transform, "Name", name, 26f, have ? UiKit.Ink : UiKit.InkSoft, true, TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = new Vector2(0f, detailTop - 0.01f);
            label.rectTransform.anchorMax = new Vector2(1f, labelTop);
            label.rectTransform.offsetMin = new Vector2(8f, 0f);
            label.rectTransform.offsetMax = new Vector2(-8f, 0f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 26f;
            if (string.IsNullOrEmpty(detail)) return tile.rectTransform;
            var d = UiKit.Text(tile.transform, "Detail", detail, 20f, UiKit.Hex("B98A1E"), false, TextAlignmentOptions.Center);
            d.rectTransform.anchorMin = Vector2.zero;
            d.rectTransform.anchorMax = new Vector2(1f, detailTop);
            d.rectTransform.offsetMin = new Vector2(6f, 6f);
            d.rectTransform.offsetMax = new Vector2(-6f, 0f);
            d.enableAutoSizing = true;
            d.fontSizeMin = 13f;
            d.fontSizeMax = 20f;
            return tile.rectTransform;
        }

        protected override void Fit()
        {
            base.Fit();
            if (_grid == null || Sheet == null) return;
            float width = Sheet.sizeDelta.x - 64f - 8f;
            int columns = Mathf.Clamp(Mathf.FloorToInt(width / 220f), 3, 6);
            float cell = (width - _grid.spacing.x * (columns - 1)) / columns;
            _grid.cellSize = new Vector2(cell, cell * 1.25f);
            _grid.constraintCount = columns;
        }
    }
}
