using System;
using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The recipe book: every dish, what it takes and how much of it you have. At the stove a tap
    /// on a dish you can make fills the pot; from Chef Tofu's list it opens at the recipe you
    /// tapped, so you can see what to buy. Dishes you don't know yet, or above your level, wait
    /// behind a lock.
    /// </summary>
    public static class RecipeBook
    {
        /// <summary>
        /// Opens the book over <paramref name="host"/>. With <paramref name="onPick"/>, the dishes
        /// you have everything for can be tapped (to fill the pot); with <paramref name="focus"/>,
        /// the book opens at that dish, marked. Returns the book, to close by destroying it.
        /// </summary>
        public static RectTransform Open(RectTransform host, Action<DishKind> onPick = null, DishKind focus = null)
        {
            var book = UiKit.Image(host, "RecipeBook", null, UiKit.Scrim, true).rectTransform;
            UiKit.Stretch(book, -400f, -400f, -400f, -400f);
            void Close() { if (book != null) UnityEngine.Object.Destroy(book.gameObject); }
            var shut = book.gameObject.AddComponent<Button>();
            shut.transition = Selectable.Transition.None;
            shut.onClick.AddListener(Close);

            // A cream page with a leather spine down its left side.
            var page = UiKit.Card(book, "Page", UiKit.Hex("FFF6E4"), 40, true, true).rectTransform;
            page.anchorMin = page.anchorMax = new Vector2(0.5f, 0.5f);
            float pw = Mathf.Min(host.rect.width - 60f, 900f), ph = Mathf.Min(host.rect.height - 120f, 1500f);
            page.sizeDelta = new Vector2(pw, ph);
            var spine = UiKit.Image(page, "Spine", UiKit.Rounded(40), UiKit.Hex("A8553A"));
            spine.rectTransform.anchorMin = Vector2.zero;
            spine.rectTransform.anchorMax = new Vector2(0f, 1f);
            spine.rectTransform.offsetMin = Vector2.zero;
            spine.rectTransform.offsetMax = new Vector2(34f, 0f);

            var head = UiKit.Text(page, "Title", "Recipe Book", 52f, UiKit.Hex("8A3F28"), true, TextAlignmentOptions.Center);
            head.rectTransform.anchorMin = new Vector2(0f, 1f);
            head.rectTransform.anchorMax = Vector2.one;
            head.rectTransform.pivot = new Vector2(0.5f, 1f);
            head.rectTransform.sizeDelta = new Vector2(-120f, 90f);
            head.rectTransform.anchoredPosition = new Vector2(17f, -24f);

            var x = UiKit.Image(page, "Close", UiKit.Circle, UiKit.SurfaceSunk, true).rectTransform;
            UiKit.Place(x, new Vector2(1f, 1f), new Vector2(-56f, -60f), new Vector2(80f, 80f));
            var xi = UiKit.Image(x, "X", IconPainter.Close, UiKit.InkSoft);
            UiKit.Stretch(xi.rectTransform, 22f, 22f, 22f, 22f);
            var xb = x.gameObject.AddComponent<Button>();
            xb.transition = Selectable.Transition.None;
            xb.onClick.AddListener(Close);

            var scroll = UiKit.ScrollList(page, "Dishes", out var list, 14f, new RectOffset(4, 4, 4, 24));
            UiKit.Stretch((RectTransform)scroll.transform, 60f, 24f, 28f, 130f);

            // What you know first, then what you could learn now, then what waits for your level.
            int level = HobbyLog.Level(Hobby.Cooking);
            var known = new List<DishKind>();
            var unknown = new List<DishKind>();
            var locked = new List<DishKind>();
            foreach (var d in HobbyData.Dishes)
                (d.MinLevel > level ? locked : HobbyLog.KnowsRecipe(d.Id) ? known : unknown).Add(d);
            RectTransform focusRow = null;
            foreach (var d in known)
            {
                var row = Row(list, d, false, onPick == null ? null : () => { Close(); onPick(d); });
                if (d == focus) focusRow = row;
            }
            foreach (var d in unknown) Row(list, d, true, null);
            foreach (var d in locked) Row(list, d, true, null);

            UiKit.PopIn(page, 0.9f);
            if (focusRow != null) Mark(scroll, focusRow);
            return book;
        }

        /// <summary>Opens at a recipe: scrolled to it, its page lit up.</summary>
        static void Mark(ScrollRect scroll, RectTransform row)
        {
            Canvas.ForceUpdateCanvases();
            var content = scroll.content;
            float view = ((RectTransform)scroll.transform).rect.height;
            float room = content.rect.height - view;
            if (room > 0f)
            {
                float middle = -row.anchoredPosition.y;   // rows hang from the top, centred on their pivot
                scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01((middle - view * 0.5f) / room);
            }
            var bg = row.GetComponent<Image>();
            if (bg != null) bg.color = UiKit.Hex("FFF1C7");
            row.DOPunchScale(Vector3.one * 0.05f, 0.5f, 6, 0.6f).SetUpdate(true);
        }

        static bool CanMake(DishKind d)
        {
            foreach (var (item, count) in d.Needs) if (!HobbyLog.Has(item, count)) return false;
            return true;
        }

        static RectTransform Row(RectTransform list, DishKind d, bool locked, Action pick)
        {
            bool ready = !locked && pick != null && CanMake(d);
            var row = UiKit.Image(list, d.Id, UiKit.Rounded(28), locked ? UiKit.Hex("EFE6D6") : Color.white, ready).rectTransform;
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 150f;

            var pic = UiKit.Image(row, "Pic", HobbyData.ItemArt("dish:" + d.Id), locked ? new Color(0f, 0f, 0f, 0.35f) : Color.white);
            pic.preserveAspect = true;
            UiKit.Place(pic.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(116f, 116f));

            var name = UiKit.Text(row, "Name", locked ? "???" : d.Name, 34f, locked ? UiKit.InkSoft : UiKit.Ink, true, TextAlignmentOptions.TopLeft);
            name.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            name.rectTransform.anchorMax = Vector2.one;
            name.rectTransform.offsetMin = new Vector2(150f, 0f);
            name.rectTransform.offsetMax = new Vector2(-24f, -16f);
            name.textWrappingMode = TextWrappingModes.NoWrap;

            // The ingredients, as little pictures with how many it takes (and how many you have).
            var needs = UiKit.Rect("Needs", row);
            needs.anchorMin = Vector2.zero;
            needs.anchorMax = new Vector2(1f, 0.5f);
            needs.offsetMin = new Vector2(150f, 12f);
            needs.offsetMax = new Vector2(-24f, 0f);
            if (locked)
            {
                // Not yours yet: Chef Tofu sells it (or a lucky pot teaches it); above your level, it waits.
                string why = d.MinLevel > HobbyLog.Level(Hobby.Cooking)
                    ? Loc.F("Cooking level {0}", d.MinLevel)
                    : Loc.T("Buy it from Chef Tofu, or work it out in the pot");
                var l = UiKit.Text(needs, "Level", why, 26f, UiKit.InkSoft, true, TextAlignmentOptions.MidlineLeft);
                UiKit.Stretch(l.rectTransform);
                var lk = UiKit.Image(row, "Lock", IconPainter.Lock, UiKit.InkSoft);
                lk.preserveAspect = true;
                UiKit.Place(lk.rectTransform, new Vector2(1f, 0.5f), new Vector2(-60f, 0f), new Vector2(52f, 52f));
                return row;
            }
            var h = needs.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = false;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            foreach (var (item, count) in d.Needs)
            {
                int have = item == "fish:any" ? HobbyLog.FishInBag() : HobbyLog.Count(item);
                var cell = UiKit.Rect(item, needs);
                cell.sizeDelta = new Vector2(130f, 60f);
                var ic = UiKit.Image(cell, "Icon", CookingScreen.Art(item), Color.white);
                ic.preserveAspect = true;
                UiKit.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(56f, 56f));
                var n = UiKit.Text(cell, "Count", $"{Mathf.Min(have, count)}/{count}", 26f, have >= count ? UiKit.Hex("3E9C4F") : UiKit.Danger, true, TextAlignmentOptions.MidlineLeft);
                UiKit.Stretch(n.rectTransform, 62f, 0f, 0f, 0f);
            }

            if (ready)
            {
                var tag = UiKit.Image(row, "Ready", UiKit.Rounded(22), UiKit.Leaf);
                var tt = UiKit.Text(tag.transform, "Label", "Put in pot", 24f, Color.white, true, TextAlignmentOptions.Center);
                UiKit.Stretch(tt.rectTransform, 12f, 0f, 12f, 0f);
                tt.textWrappingMode = TextWrappingModes.NoWrap;
                tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = tag.rectTransform.pivot = new Vector2(1f, 1f);
                tag.rectTransform.anchoredPosition = new Vector2(-18f, -16f);
                tag.rectTransform.sizeDelta = new Vector2(tt.GetPreferredValues().x + 30f, 46f);
                var b = row.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => pick());
                row.gameObject.AddComponent<PressSquash>();
            }
            return row;
        }
    }
}
