using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CapyIsland.EditorTools
{
    /// <summary>
    /// The talking and discovery UI: the speech dialog every character uses, and the card that pops
    /// up for a first discovery or a bit of love. Both are built stretched to the safe area so they
    /// reflow in portrait and landscape.
    /// </summary>
    public static partial class IslandBuilder
    {
        static readonly Color Parchment = new Color(0.98f, 0.96f, 0.91f, 0.98f);
        static readonly Color Ink = new Color(0.22f, 0.16f, 0.11f);

        static RectTransform Stretch(GameObject go)
        {
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            return r;
        }

        static Image Panel(Transform parent, string name, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Common/White.png");
            img.color = colour;
            return img;
        }

        /// <summary>Bottom-centre speech panel: portrait, name, page text, choice buttons.</summary>
        static void BuildSpeechDialog(Transform safeArea)
        {
            var root = new GameObject("SpeechDialogRoot", typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            Stretch(root);

            var panelImg = Panel(root.transform, "Panel", new Color(Parchment.r, Parchment.g, Parchment.b, 1f));
            var panel = panelImg.gameObject;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 250f);
            rect.sizeDelta = new Vector2(1000f, 380f);

            // The whole panel is the "next page" button.
            var pageButton = panel.AddComponent<Button>();
            pageButton.transition = Selectable.Transition.None;

            var portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGo.transform.SetParent(panel.transform, false);
            var pr = portraitGo.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(0f, 1f);
            pr.pivot = new Vector2(0f, 1f);
            pr.anchoredPosition = new Vector2(20f, -20f);
            pr.sizeDelta = new Vector2(200f, 200f);
            var portrait = portraitGo.GetComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;

            var name = Label(panel.transform, "Name", 40f, TextAlignmentOptions.Left,
                Vector2.zero, new Vector2(700f, 56f), Ink);
            var nr = name.rectTransform;
            nr.anchorMin = nr.anchorMax = new Vector2(0f, 1f);
            nr.pivot = new Vector2(0f, 1f);
            nr.anchoredPosition = new Vector2(240f, -22f);
            name.fontStyle = FontStyles.Bold;

            var body = Label(panel.transform, "Body", 32f, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.zero, new Color(0.30f, 0.24f, 0.18f));
            var br = body.rectTransform;
            br.anchorMin = new Vector2(0f, 0f);
            br.anchorMax = new Vector2(1f, 1f);
            br.offsetMin = new Vector2(240f, 110f);
            br.offsetMax = new Vector2(-32f, -84f);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Ellipsis;

            var hint = Label(panel.transform, "Hint", 24f, TextAlignmentOptions.BottomRight,
                Vector2.zero, new Vector2(300f, 40f), new Color(0.5f, 0.42f, 0.34f));
            var hr = hint.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(1f, 0f);
            hr.pivot = new Vector2(1f, 0f);
            hr.anchoredPosition = new Vector2(-24f, 16f);
            hint.fontStyle = FontStyles.Italic;

            // Up to five choices in a row along the bottom.
            var row = new GameObject("Choices", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(panel.transform, false);
            var rr = row.GetComponent<RectTransform>();
            rr.anchorMin = new Vector2(0f, 0f);
            rr.anchorMax = new Vector2(1f, 0f);
            rr.pivot = new Vector2(0.5f, 0f);
            rr.offsetMin = new Vector2(24f, 18f);
            rr.offsetMax = new Vector2(-24f, 100f);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var buttons = new List<Button>();
            for (int i = 0; i < 5; i++)
            {
                var b = DialogButton(row.transform, "Choice" + i, "Choice", 0f,
                    i == 0 ? new Color(0.42f, 0.66f, 0.30f) : new Color(0.86f, 0.83f, 0.77f),
                    i == 0 ? Color.white : Ink);
                var caption = b.GetComponentInChildren<TMP_Text>();
                caption.fontSize = 26f;
                caption.enableAutoSizing = true;
                caption.fontSizeMin = 16f;
                caption.fontSizeMax = 30f;
                caption.rectTransform.anchorMin = Vector2.zero;
                caption.rectTransform.anchorMax = Vector2.one;
                caption.rectTransform.offsetMin = new Vector2(8f, 4f);
                caption.rectTransform.offsetMax = new Vector2(-8f, -4f);
                b.gameObject.SetActive(false);
                buttons.Add(b);
            }

            var dialog = root.AddComponent<UI.SpeechDialog>();
            var so = new SerializedObject(dialog);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("portrait").objectReferenceValue = portrait;
            so.FindProperty("nameLabel").objectReferenceValue = name;
            so.FindProperty("bodyLabel").objectReferenceValue = body;
            so.FindProperty("pageButton").objectReferenceValue = pageButton;
            so.FindProperty("hintLabel").objectReferenceValue = hint;
            var list = so.FindProperty("choiceButtons");
            list.arraySize = buttons.Count;
            for (int i = 0; i < buttons.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
        }

        /// <summary>A card in the middle of a dimmed screen: picture, title, a line of text.</summary>
        static void BuildCardPopup(Transform safeArea)
        {
            var root = new GameObject("CardPopupRoot", typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            Stretch(root);

            var dim = Panel(root.transform, "Panel", new Color(0.08f, 0.06f, 0.05f, 0.55f));
            Stretch(dim.gameObject);
            var dismiss = dim.gameObject.AddComponent<Button>();
            dismiss.transition = Selectable.Transition.None;

            var card = Panel(dim.transform, "Card", Parchment);
            var cr = card.rectTransform;
            cr.sizeDelta = new Vector2(620f, 820f);
            card.raycastTarget = false;

            var border = Panel(card.transform, "Border", new Color(0.86f, 0.56f, 0.62f));
            var brd = border.rectTransform;
            brd.anchorMin = Vector2.zero; brd.anchorMax = Vector2.one;
            brd.offsetMin = new Vector2(14f, 14f); brd.offsetMax = new Vector2(-14f, -14f);
            border.raycastTarget = false;
            var inner = Panel(border.transform, "Inner", Parchment);
            var inr = inner.rectTransform;
            inr.anchorMin = Vector2.zero; inr.anchorMax = Vector2.one;
            inr.offsetMin = new Vector2(8f, 8f); inr.offsetMax = new Vector2(-8f, -8f);
            inner.raycastTarget = false;

            var picGo = new GameObject("Picture", typeof(RectTransform), typeof(Image));
            picGo.transform.SetParent(card.transform, false);
            var pr = picGo.GetComponent<RectTransform>();
            pr.anchoredPosition = new Vector2(0f, 150f);
            pr.sizeDelta = new Vector2(360f, 360f);
            var pic = picGo.GetComponent<Image>();
            pic.preserveAspect = true;
            pic.raycastTarget = false;

            var title = Label(card.transform, "Title", 46f, TextAlignmentOptions.Center,
                new Vector2(0f, -85f), new Vector2(560f, 70f), Ink);
            title.fontStyle = FontStyles.Bold;
            var body = Label(card.transform, "Body", 30f, TextAlignmentOptions.Top,
                new Vector2(0f, -225f), new Vector2(540f, 210f), new Color(0.30f, 0.24f, 0.18f));
            body.textWrappingMode = TextWrappingModes.Normal;
            var tap = Label(card.transform, "Tap", 24f, TextAlignmentOptions.Center,
                new Vector2(0f, -370f), new Vector2(540f, 40f), new Color(0.5f, 0.42f, 0.34f));
            tap.text = "tap to continue";
            tap.fontStyle = FontStyles.Italic;

            var popup = root.AddComponent<UI.CardPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("panel").objectReferenceValue = dim.gameObject;
            so.FindProperty("card").objectReferenceValue = cr;
            so.FindProperty("picture").objectReferenceValue = pic;
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("bodyLabel").objectReferenceValue = body;
            so.FindProperty("dismiss").objectReferenceValue = dismiss;
            so.ApplyModifiedPropertiesWithoutUndo();

            dim.gameObject.SetActive(false);
        }

        /// <summary>
        /// Little grass tufts, wildflowers and pebbles strewn over every island, so the land does not
        /// read as flat paint. Pure dressing: they block nothing.
        /// </summary>
        static void ScatterDressing(Transform parent, Game.LandMap land)
        {
            var sprites = new[]
            {
                Load("Assets/Art/Terrain/Deco_GrassTuft.png"),
                Load("Assets/Art/Terrain/Deco_Wildflowers.png"),
                Load("Assets/Art/Terrain/Deco_Pebbles.png"),
            };
            if (System.Array.TrueForAll(sprites, s => s == null)) return;

            var root = new GameObject("Dressing");
            root.transform.SetParent(parent, false);
            var rng = new System.Random(42);
            int island = 0;
            foreach (var piece in land.Pieces)
            {
                if (piece.isBridge) continue;
                int count = piece.circles.Count * 2;
                for (int i = 0; i < count; i++)
                {
                    var c = piece.circles[rng.Next(piece.circles.Count)];
                    float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float r = c.z * 0.85f * Mathf.Sqrt((float)rng.NextDouble());
                    var world = new Vector2(c.x + Mathf.Cos(a) * r, c.y + Mathf.Sin(a) * r);
                    var at = Game.LandMap.WorldToUnity(world);
                    if (!land.CircleOnLand(at, 20f)) continue;

                    int pick = rng.NextDouble() < 0.55 ? 0 : rng.NextDouble() < 0.6 ? 1 : 2;
                    var sprite = sprites[pick];
                    if (sprite == null) continue;
                    var go = new GameObject("Tuft");
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = at;
                    float size = pick == 2 ? 0.35f : 0.55f;
                    go.transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, sprite.bounds.size.x)) * (0.8f + (float)rng.NextDouble() * 0.5f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.flipX = rng.NextDouble() < 0.5;
                    // Flat on the ground: under everything that stands up.
                    sr.sortingOrder = IslandPainter.GrassOrder + 5;
                }
                island++;
            }
        }
    }
}
