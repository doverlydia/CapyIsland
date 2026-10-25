using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The look of every screen: one palette, two fonts, rounded cards with a soft drop shadow, and
    /// chunky buttons that squash when pressed. Everything is built in code, so the HUD survives an
    /// island rebuild and every panel in the game matches.
    ///
    /// The rounded shapes are generated at startup rather than shipped as textures: a rounded rect
    /// is geometry, not art, and generating it means any corner radius is available.
    /// </summary>
    public static class UiKit
    {
        // ---------------------------------------------------------------- palette

        public static readonly Color Surface = Hex("FFFBF3");
        public static readonly Color SurfaceSunk = Hex("F3EADB");
        public static readonly Color Ink = Hex("3A2A1F");
        public static readonly Color InkSoft = Hex("8A7564");
        public static readonly Color Primary = Hex("FF8A3D");       // warm orange: the main action
        public static readonly Color PrimaryDark = Hex("D9661C");
        public static readonly Color Spring = Hex("2DB3A6");        // hot-spring teal: info, bubbles
        public static readonly Color Leaf = Hex("5DBB63");          // done, success
        public static readonly Color Danger = Hex("E8553E");        // pests, warnings
        public static readonly Color Sun = Hex("FFC93C");           // quests, rewards
        public static readonly Color Scrim = new Color(0.10f, 0.07f, 0.05f, 0.55f);
        public static readonly Color HudGlass = new Color(0.16f, 0.11f, 0.08f, 0.72f);

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        // ---------------------------------------------------------------- fonts

        static TMP_FontAsset _heading, _body;

        /// <summary>Rounded display face for titles, numbers and buttons.</summary>
        public static TMP_FontAsset HeadingFont => _heading != null ? _heading : _heading = Resources.Load<TMP_FontAsset>("Fonts/Fredoka-SemiBold SDF");

        /// <summary>Friendly, very legible face for running text.</summary>
        public static TMP_FontAsset BodyFont => _body != null ? _body : _body = Resources.Load<TMP_FontAsset>("Fonts/Nunito-Bold SDF");

        /// <summary>
        /// Nunito has no Hebrew letters; Fredoka does. Making Fredoka the fallback for every face
        /// means a Hebrew line draws in Fredoka whatever font the label was given.
        /// </summary>
        public static void EnsureFallbacks()
        {
            var hebrew = HeadingFont;
            if (hebrew == null) return;
            foreach (var face in new[] { BodyFont, TMP_Settings.defaultFontAsset })
            {
                if (face == null || face == hebrew) continue;
                // First in line, ahead of LiberationSans (which also has Hebrew, in a mismatched style).
                face.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
                face.fallbackFontAssetTable.Remove(hebrew);
                face.fallbackFontAssetTable.Insert(0, hebrew);
            }
            var global = TMP_Settings.fallbackFontAssets;
            if (global != null)
            {
                global.Remove(hebrew);
                global.Insert(0, hebrew);
            }
        }

        /// <summary>Re-faces every label under <paramref name="root"/>: bold or big text gets the display face.</summary>
        public static void ApplyFonts(Transform root)
        {
            if (root == null) return;
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                bool heading = (t.fontStyle & FontStyles.Bold) != 0 || t.fontSize >= 38f;
                var face = heading ? HeadingFont : BodyFont;
                if (face == null) continue;
                t.font = face;
                if (heading) t.fontStyle &= ~FontStyles.Bold;   // the face is already heavy
            }
        }

        // ---------------------------------------------------------------- game icons

        static readonly Dictionary<Game.ResourceKind, Sprite> _resourceIcons = new();

        /// <summary>The wood, gold and gem pictures, borrowed from the resource bar.</summary>
        public static Sprite ResourceIcon(Game.ResourceKind kind) =>
            _resourceIcons.TryGetValue(kind, out var s) ? s : null;

        public static void SetResourceIcon(Game.ResourceKind kind, Sprite sprite)
        {
            if (sprite != null) _resourceIcons[kind] = sprite;
        }

        // ---------------------------------------------------------------- text entry

        /// <summary>A rounded text box. On phones, tapping it brings up the keyboard.</summary>
        public static TMP_InputField Input(Transform parent, string name, string placeholder, bool multiline, float height)
        {
            var root = Image(parent, name, Rounded(24), Color.white, true);
            root.rectTransform.sizeDelta = new Vector2(0f, height);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = height;

            var area = Rect("TextArea", root.transform);
            Stretch(area, 22f, 14f, 22f, 14f);
            area.gameObject.AddComponent<RectMask2D>();

            var hint = Text(area, "Placeholder", placeholder, 30f, new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.7f));
            Stretch(hint.rectTransform);
            hint.fontStyle = FontStyles.Italic;
            var text = Text(area, "Text", "", 30f, Ink);
            Stretch(text.rectTransform);
            text.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
            hint.alignment = text.alignment;
            text.overflowMode = TextOverflowModes.Overflow;

            var field = root.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = hint;
            field.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
            field.fontAsset = BodyFont;
            field.pointSize = 30f;
            field.caretColor = Ink;
            field.selectionColor = new Color(Spring.r, Spring.g, Spring.b, 0.35f);
            return field;
        }

        // ---------------------------------------------------------------- emoji and avatars

        /// <summary>A colour emoji built from painted layers, so no emoji font is needed.</summary>
        public static RectTransform Emoji(Transform parent, Game.Emoji e, float size)
        {
            var root = Rect("Emoji_" + e, parent);
            root.sizeDelta = new Vector2(size, size);
            var faceDark = Hex("5A3A1E");
            switch (e)
            {
                case Game.Emoji.Heart:
                    Stretch(Image(root, "Heart", IconPainter.Heart, Hex("F0506E")).rectTransform);
                    break;
                case Game.Emoji.Star:
                    Stretch(Image(root, "Star", IconPainter.Star, Hex("FFC93C")).rectTransform);
                    break;
                case Game.Emoji.Wave:
                    var paw = Image(root, "Paw", IconPainter.Paw, Hex("C98B4E"));
                    Stretch(paw.rectTransform, size * 0.06f, size * 0.06f, size * 0.06f, size * 0.06f);
                    paw.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 18f);
                    break;
                default:
                    Stretch(Image(root, "Face", Circle, Hex("FFD24A")).rectTransform);
                    var features = e == Game.Emoji.Laugh ? IconPainter.FaceLaugh : e == Game.Emoji.Wow ? IconPainter.FaceWow : IconPainter.FaceSmile;
                    Stretch(Image(root, "Features", features, faceDark).rectTransform, size * 0.12f, size * 0.12f, size * 0.12f, size * 0.12f);
                    break;
            }
            return root;
        }

        /// <summary>
        /// A round portrait of a friend: their capybara, tinted, with their hat if there is art for it.
        /// An islander (one the game plays, not a real player) wears a little leaf at the corner.
        /// </summary>
        public static RectTransform Avatar(Transform parent, Game.FriendProfile who, float size)
        {
            float hue = Mathf.Repeat((who.seed % 997) * 0.618034f, 1f);
            var disc = Image(parent, "Avatar", Circle, Color.HSVToRGB(hue, 0.28f, 0.95f));
            disc.rectTransform.sizeDelta = new Vector2(size, size);
            var le = disc.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = le.minWidth = le.minHeight = size;
            var catalog = Game.ItemCatalog.Instance;
            var body = Game.Wardrobe.Ready ? Game.Wardrobe.BaseSprite : catalog != null ? catalog.Icon("Capybara") : null;
            if (body != null)
            {
                var pic = Image(disc.transform, "Capy", body, Color.Lerp(Color.white, new Color(0.84f, 0.72f, 0.6f), Mathf.Max(0f, who.tint) * 0.45f));
                pic.preserveAspect = true;
                Stretch(pic.rectTransform, size * 0.1f, size * 0.06f, size * 0.1f, size * 0.14f);
                foreach (var id in who.outfit)
                {
                    var item = Game.Wardrobe.Find(id);
                    var art = Game.Wardrobe.Art(item);
                    if (item == null || art == null || item.Slot != Game.WearSlot.Hat) continue;
                    var hat = Image(pic.transform, "Hat", art, Color.white);
                    hat.preserveAspect = true;
                    var r = hat.rectTransform;
                    r.anchorMin = r.anchorMax = item.Anchor;
                    r.sizeDelta = new Vector2(item.Width, item.Width) * size * 0.8f;
                }
            }
            if (who.IsIslander) IslanderLeaf(disc.transform, size);
            return disc.rectTransform;
        }

        /// <summary>
        /// The islander's mark: a green leaf in a white disc at a portrait's lower left (the lower
        /// right is where the "online" dot goes).
        /// </summary>
        static void IslanderLeaf(Transform portrait, float size)
        {
            float d = Mathf.Max(22f, size * 0.4f);
            var badge = Image(portrait, "Islander", Circle, Color.white);
            badge.raycastTarget = false;
            var r = badge.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 0f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(d, d);
            r.anchoredPosition = new Vector2(d * 0.18f, d * 0.18f);
            var leaf = Image(badge.transform, "Leaf", IconPainter.Leaf, Hex("5DA34A"));
            leaf.raycastTarget = false;
            Stretch(leaf.rectTransform, d * 0.16f, d * 0.16f, d * 0.16f, d * 0.16f);
            badge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        /// <summary>The islander's leaf on its own, for a row that shows a name without a face (a contest board).</summary>
        public static Image IslanderLeafIcon(Transform parent, float size)
        {
            var leaf = Image(parent, "Islander", IconPainter.Leaf, Hex("5DA34A"));
            leaf.raycastTarget = false;
            leaf.preserveAspect = true;
            var le = leaf.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = le.minWidth = le.minHeight = size;
            return leaf;
        }

        // ---------------------------------------------------------------- shapes

        static readonly Dictionary<int, Sprite> _rounded = new();
        static Sprite _circle, _shadow;

        /// <summary>A nine-sliced white rounded rectangle; tint it with Image.color.</summary>
        public static Sprite Rounded(int radius = 28)
        {
            radius = Mathf.Clamp(radius, 2, 128);
            if (_rounded.TryGetValue(radius, out var s) && s != null) return s;
            int size = radius * 2 + 4;
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Signed distance to a rounded box filling the texture less a 1px apron.
                float qx = Mathf.Abs(x + 0.5f - half) - (half - 1f - radius);
                float qy = Mathf.Abs(y + 0.5f - half) - (half - 1f - radius);
                float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                byte a = (byte)(Mathf.Clamp01(0.5f - outside) * 255f);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            int border = radius + 2;
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            s.name = "Rounded" + radius;
            _rounded[radius] = s;
            return s;
        }

        /// <summary>A soft blurred rounded rect, nine-sliced, for drop shadows.</summary>
        public static Sprite ShadowSprite
        {
            get
            {
                if (_shadow != null) return _shadow;
                const int size = 96, radius = 28, blur = 16;
                var tex = NewTexture(size, size);
                var px = new Color32[size * size];
                float half = size * 0.5f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - half) - (half - blur - radius);
                    float qy = Mathf.Abs(y + 0.5f - half) - (half - blur - radius);
                    float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d + blur * 0.5f) / blur));
                    px[y * size + x] = new Color32(0, 0, 0, (byte)(a * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                int border = size / 2 - 1;
                _shadow = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(border, border, border, border));
                return _shadow;
            }
        }

        public static Sprite Circle => _circle != null ? _circle : _circle = IconPainter.Paint(128, (x, y) => IconPainter.Disc(x, y, 0f, 0f, 0.98f));

        static Texture2D NewTexture(int w, int h) => new(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave,
        };

        // ---------------------------------------------------------------- building blocks

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;   // UI
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform r, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(left, bottom);
            r.offsetMax = new Vector2(-right, -top);
            return r;
        }

        /// <summary>Anchors and pivots at one point, at a size.</summary>
        public static RectTransform Place(RectTransform r, Vector2 anchor, Vector2 position, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = anchor;
            r.anchoredPosition = position;
            r.sizeDelta = size;
            return r;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color colour, bool raycast = false)
        {
            var r = Rect(name, parent);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = colour;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        /// <summary>A rounded card, optionally with a soft shadow beneath it.</summary>
        public static Image Card(Transform parent, string name, Color colour, int radius = 32, bool shadow = true, bool raycast = true)
        {
            var img = Image(parent, name, Rounded(radius), colour, raycast);
            if (shadow) AddShadow(img.rectTransform);
            return img;
        }

        /// <summary>
        /// Puts a soft shadow behind a rect. A child would draw over its parent, so the shadow is a
        /// sibling placed just before it, following its size, position and visibility.
        /// </summary>
        public static Image AddShadow(RectTransform target, float spread = 22f, float drop = 10f, float alpha = 0.28f)
        {
            var parent = target.parent;
            if (parent == null) return null;
            var sh = Image(parent, target.name + "Shadow", ShadowSprite, new Color(0f, 0f, 0f, alpha));
            sh.type = UnityEngine.UI.Image.Type.Sliced;
            sh.transform.SetSiblingIndex(target.GetSiblingIndex());
            sh.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            sh.gameObject.AddComponent<ShadowFollow>().Init(target, spread, drop);
            return sh;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color colour,
            bool heading = false, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var r = Rect(name, parent);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            var face = heading ? HeadingFont : BodyFont;
            if (face != null) t.font = face;
            t.text = text;
            t.fontSize = size;
            t.color = colour;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Ellipsis;
            LocText.Attach(t);
            return t;
        }

        public enum ButtonStyle { Primary, Secondary, Spring, Ghost }

        /// <summary>A chunky rounded button with a darker lip underneath and a squash on press.</summary>
        public static Button Button(Transform parent, string name, string label, ButtonStyle style, Action onClick,
            float fontSize = 36f, Sprite icon = null)
        {
            Color fill = style switch
            {
                ButtonStyle.Primary => Primary,
                ButtonStyle.Spring => Spring,
                ButtonStyle.Ghost => new Color(1f, 1f, 1f, 0f),
                _ => SurfaceSunk,
            };
            Color text = style == ButtonStyle.Secondary || style == ButtonStyle.Ghost ? Ink : Color.white;

            var root = Rect(name, parent);
            var lip = Image(root, "Lip", Rounded(26), style == ButtonStyle.Ghost ? fill : Color.Lerp(fill, Color.black, 0.22f));
            Stretch(lip.rectTransform, 0f, 0f, 0f, 6f);
            var face = Image(root, "Face", Rounded(26), fill, true);
            Stretch(face.rectTransform, 0f, 6f, 0f, 0f);

            var row = Rect("Content", face.transform);
            Stretch(row, 16f, 0f, 16f, 0f);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 12f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;

            if (icon != null)
            {
                var ic = Image(row, "Icon", icon, text);
                ic.preserveAspect = true;
                var le = ic.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = fontSize * 1.15f;
            }
            if (!string.IsNullOrEmpty(label))
            {
                var t = Text(row, "Label", label, fontSize, text, true, TextAlignmentOptions.Center);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.enableAutoSizing = true;
                t.fontSizeMin = fontSize * 0.55f;
                t.fontSizeMax = fontSize;
                // Its label shrinks to fit the button, so with Easy Reading the button grows a little
                // (taller, and wider if it has a set width) to let the bigger words through.
                GrowsWithText.Add(root, tall: 0.6f, wide: 0.4f);
            }

            var button = face.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var colours = button.colors;
            colours.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
            colours.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            button.colors = colours;
            if (onClick != null) button.onClick.AddListener(() => { Game.Sfx.Play(Game.SfxKind.Tap, 0.5f); onClick(); });
            face.gameObject.AddComponent<PressSquash>();
            return button;
        }

        /// <summary>A round floating button with an icon and an optional caption underneath.</summary>
        public static Button RoundButton(Transform parent, string name, Sprite icon, string caption, Color fill, Action onClick, float size = 112f)
        {
            var root = Rect(name, parent);
            root.sizeDelta = new Vector2(size, size + (string.IsNullOrEmpty(caption) ? 0f : 36f));

            var disc = Image(root, "Disc", Circle, fill, true);
            Place(disc.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(size, size));
            AddShadow(disc.rectTransform, 14f, 6f, 0.3f);
            var ring = Image(disc.transform, "Ring", IconPainter.Ring, new Color(1f, 1f, 1f, 0.9f));
            Stretch(ring.rectTransform);
            var ic = Image(disc.transform, "Icon", icon, Color.white);
            ic.preserveAspect = true;
            Stretch(ic.rectTransform, size * 0.22f, size * 0.22f, size * 0.22f, size * 0.22f);

            if (!string.IsNullOrEmpty(caption))
            {
                // A dark pill under the caption keeps it readable over grass, sand or sea.
                var pill = Image(root, "CaptionPill", Rounded(18), HudGlass);
                Place(pill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -8f), new Vector2(size, 40f));
                var t = Text(pill.transform, "Caption", caption, 28f, Color.white, true, TextAlignmentOptions.Center);
                Stretch(t.rectTransform, 6f, 0f, 6f, 0f);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.overflowMode = TextOverflowModes.Overflow;
                // A pill sized for the button: longer words shrink to fit it. With Easy Reading the
                // pill (and the button's slot, so the column spaces out) grows to hold bigger words.
                t.enableAutoSizing = true;
                t.fontSizeMin = 18f;
                t.fontSizeMax = 28f;
                GrowsWithText.Add(pill.rectTransform, tall: 1f, wide: 0.5f);
                GrowsWithText.Add(root, tall: 36f / (size + 36f));
            }

            var button = disc.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            if (onClick != null) button.onClick.AddListener(() => { Game.Sfx.Play(Game.SfxKind.Tap, 0.5f); onClick(); });
            disc.gameObject.AddComponent<PressSquash>();
            return button;
        }

        /// <summary>A small coloured dot with a number, for unread counts.</summary>
        public static TextMeshProUGUI Badge(Transform parent, Color colour, float size = 40f)
        {
            var dot = Image(parent, "Badge", Circle, colour);
            Place(dot.rectTransform, new Vector2(1f, 1f), new Vector2(6f, 6f), new Vector2(size, size));
            var t = Text(dot.transform, "Count", "", size * 0.6f, Color.white, true, TextAlignmentOptions.Center);
            Stretch(t.rectTransform);
            var loc = t.GetComponent<LocText>();
            loc.NoScale = loc.NoRead = true;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        /// <summary>
        /// A vertical scrolling list. Add rows to <paramref name="content"/>; they stack with
        /// <paramref name="spacing"/> and the list grows to fit them. With <paramref name="stacked"/>
        /// false the content gets no layout at all, for a grid or a free-size picture.
        /// </summary>
        public static ScrollRect ScrollList(Transform parent, string name, out RectTransform content, float spacing = 16f,
            RectOffset padding = null, bool stacked = true)
        {
            var root = Rect(name, parent);
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);   // catch drags between rows
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            scroll.decelerationRate = 0.12f;

            var viewport = Rect("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            scroll.content = content;
            // A round "more below" button at the bottom when the list runs on past it (only on a
            // screen, and only up and down: it removes itself from the map and the chat).
            MoreBelowCue.AddDefault(scroll);
            if (!stacked) return scroll;
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding ?? new RectOffset(0, 0, 8, 24);
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return scroll;
        }

        /// <summary>A thin rounded progress bar; set fill with <see cref="SetProgress"/>.</summary>
        public static Image ProgressBar(Transform parent, string name, Color fill, float height = 22f)
        {
            var track = Image(parent, name, Rounded(12), new Color(0f, 0f, 0f, 0.12f));
            var le = track.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = height;
            var bar = Image(track.transform, "Fill", Rounded(12), fill);
            bar.rectTransform.anchorMin = Vector2.zero;
            bar.rectTransform.anchorMax = new Vector2(0f, 1f);
            bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;
            return bar;
        }

        public static void SetProgress(Image fill, float t)
        {
            if (fill == null) return;
            t = Mathf.Clamp01(t);
            var r = fill.rectTransform;
            r.anchorMax = new Vector2(t, 1f);
            fill.enabled = t > 0.001f;
        }

        // ---------------------------------------------------------------- restyling the built HUD

        /// <summary>Turns a flat rectangle into a rounded card, keeping its colour (made opaque).</summary>
        public static void Restyle(Image image, Color? colour = null, int radius = 32, bool shadow = true)
        {
            if (image == null) return;
            image.sprite = Rounded(radius);
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            if (colour.HasValue) image.color = colour.Value;
            if (shadow && image.transform.parent != null && image.transform.parent.Find(image.name + "Shadow") == null)
                AddShadow(image.rectTransform);
        }

        /// <summary>Restyles a dialog button from the island builder into a rounded chunky one.</summary>
        public static void RestyleButton(Button button, bool primary)
        {
            if (button == null) return;
            var img = button.targetGraphic as Image;
            if (img == null) img = button.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = Rounded(26);
                img.type = UnityEngine.UI.Image.Type.Sliced;
                img.color = primary ? Primary : SurfaceSunk;
            }
            foreach (var t in button.GetComponentsInChildren<TMP_Text>(true))
            {
                t.font = HeadingFont != null ? HeadingFont : t.font;
                t.fontStyle &= ~FontStyles.Bold;
                t.color = primary ? Color.white : Ink;
            }
            if (button.GetComponent<PressSquash>() == null) button.gameObject.AddComponent<PressSquash>();
        }

        /// <summary>Pops a panel in: a quick scale-up with a little overshoot.</summary>
        public static void PopIn(Transform t, float from = 0.85f)
        {
            if (t == null) return;
            t.DOKill();
            t.localScale = Vector3.one * from;
            t.DOScale(1f, 0.28f).SetEase(Ease.OutBack).SetUpdate(true);
            var group = t.GetComponent<CanvasGroup>();
            if (group == null) group = t.gameObject.AddComponent<CanvasGroup>();
            group.DOKill();
            group.alpha = 0f;
            group.DOFade(1f, 0.18f).SetUpdate(true);
        }
    }

    /// <summary>Squashes a button a little while pressed, so taps feel physical.</summary>
    public class PressSquash : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public void OnPointerDown(PointerEventData e)
        {
            // A button that can't do anything right now doesn't pretend to be pressed.
            if (TryGetComponent<Selectable>(out var button) && !button.IsInteractable()) return;
            transform.DOKill();
            transform.DOScale(new Vector3(0.94f, 0.92f, 1f), 0.08f).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        void Release()
        {
            transform.DOKill();
            transform.DOScale(1f, 0.22f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        void OnDisable()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
        }
    }

    /// <summary>Keeps a sibling shadow sized, placed and shown along with the rect it shadows.</summary>
    public class ShadowFollow : MonoBehaviour
    {
        RectTransform _target, _self;
        float _spread, _drop;
        Image _image;
        float _alpha;

        public void Init(RectTransform target, float spread, float drop)
        {
            _target = target;
            _self = (RectTransform)transform;
            _spread = spread;
            _drop = drop;
            _image = GetComponent<Image>();
            _alpha = _image != null ? _image.color.a : 0.3f;
            Sync();
        }

        void LateUpdate() => Sync();

        void Sync()
        {
            if (_target == null) { Destroy(gameObject); return; }
            if (_self == null) return;
            bool show = _target.gameObject.activeInHierarchy;
            if (_image != null)
            {
                _image.enabled = show;
                var group = _target.GetComponent<CanvasGroup>();
                var c = _image.color;
                c.a = _alpha * (group != null ? group.alpha : 1f);
                _image.color = c;
            }
            if (!show) return;
            _self.anchorMin = _target.anchorMin;
            _self.anchorMax = _target.anchorMax;
            _self.pivot = _target.pivot;
            _self.sizeDelta = _target.sizeDelta + new Vector2(_spread * 2f, _spread * 2f);
            _self.anchoredPosition = _target.anchoredPosition + new Vector2(
                (_target.pivot.x - 0.5f) * _spread * 2f,
                (_target.pivot.y - 0.5f) * _spread * 2f - _drop);
            _self.localScale = _target.localScale;
        }
    }
}
