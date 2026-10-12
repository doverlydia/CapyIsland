using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// A full-screen sheet over a dimmed island: a rounded card with a title, a close button and a
    /// body. Screens stack — the menu opens the map over itself — and the phone's back button (or
    /// Escape) closes the top one. While any is open the island ignores touches.
    ///
    /// Only the ✕ and the back button close a screen: a tap that just misses a button near the
    /// sheet's edge lands on the dimmed island and does nothing.
    ///
    /// Subclasses build their body once in <see cref="Build"/> and refresh it in <see cref="OnOpen"/>.
    /// </summary>
    public abstract class UiScreen : MonoBehaviour
    {
        static readonly List<UiScreen> _stack = new();

        /// <summary>True while any screen is showing.</summary>
        public static bool AnyOpen => _stack.Count > 0;

        public static event Action<UiScreen> Opened, Closed;

        protected RectTransform Sheet { get; private set; }
        protected RectTransform Body { get; private set; }
        protected TextMeshProUGUI TitleLabel { get; private set; }

        GameObject _root;
        bool _built;
        Action _onClosed;

        protected abstract string Title { get; }
        protected virtual Sprite TitleIcon => null;
        protected virtual Color AccentColour => UiKit.Primary;

        /// <summary>Widest the sheet gets, in canvas units.</summary>
        protected virtual float MaxWidth => 980f;

        /// <summary>
        /// A place rather than a page (the kitchen): no card and no header, the body is the whole
        /// screen, and the subclass draws its own way out (it calls <see cref="Close"/>).
        /// </summary>
        protected virtual bool FullBleed => false;

        public bool IsOpen => _root != null && _root.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            _stack.Clear();
            Opened = Closed = null;
        }

        public void Open(Action onClosed = null)
        {
            EnsureBuilt();
            _onClosed = onClosed;
            transform.SetAsLastSibling();
            _root.SetActive(true);
            Fit();
            if (!_stack.Contains(this)) _stack.Add(this);
            OnOpen();
            UiKit.PopIn(Sheet, 0.9f);
            Game.Sfx.Play(Game.SfxKind.Tap, 0.4f);
            Opened?.Invoke(this);
        }

        public void Close()
        {
            if (!IsOpen) return;
            _root.SetActive(false);
            _stack.Remove(this);
            OnClose();
            var callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
            Closed?.Invoke(this);
        }

        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }
        protected abstract void Build(RectTransform body);

        void OnEnable() => Core.ScreenMetrics.Changed += Fit;
        void OnDisable() => Core.ScreenMetrics.Changed -= Fit;

        protected virtual void Update()
        {
            if (!IsOpen) return;
            if (_stack.Count > 0 && _stack[_stack.Count - 1] == this && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            var self = (RectTransform)transform;
            UiKit.Stretch(self);

            _root = UiKit.Rect("Root", transform).gameObject;
            UiKit.Stretch((RectTransform)_root.transform);

            // The dimmed island behind: catches stray taps so they never reach the game.
            var scrim = UiKit.Image(_root.transform, "Scrim", null, UiKit.Scrim, true);
            UiKit.Stretch(scrim.rectTransform, -400f, -400f, -400f, -400f);

            if (FullBleed)
            {
                Sheet = Body = UiKit.Rect("Place", _root.transform);
                UiKit.Stretch(Body);
                Build(Body);
                _root.SetActive(false);
                return;
            }

            var card = UiKit.Card(_root.transform, "Sheet", UiKit.Surface, 44);
            Sheet = card.rectTransform;
            Sheet.anchorMin = Sheet.anchorMax = Sheet.pivot = new Vector2(0.5f, 0.5f);

            // Header: a coloured band with the title and the close button.
            var header = UiKit.Image(Sheet, "Header", UiKit.Rounded(44), AccentColour);
            var hr = header.rectTransform;
            hr.anchorMin = new Vector2(0f, 1f);
            hr.anchorMax = new Vector2(1f, 1f);
            hr.pivot = new Vector2(0.5f, 1f);
            hr.sizeDelta = new Vector2(0f, 128f);
            hr.anchoredPosition = Vector2.zero;
            // Square off the band's lower corners where it meets the body.
            var square = UiKit.Image(header.transform, "Square", null, AccentColour);
            square.rectTransform.anchorMin = Vector2.zero;
            square.rectTransform.anchorMax = new Vector2(1f, 0f);
            square.rectTransform.pivot = new Vector2(0.5f, 0f);
            square.rectTransform.sizeDelta = new Vector2(0f, 48f);

            float textLeft = 40f;
            if (TitleIcon != null)
            {
                var icon = UiKit.Image(header.transform, "Icon", TitleIcon, Color.white);
                icon.preserveAspect = true;
                UiKit.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(64f, 64f));
                textLeft = 116f;
            }
            TitleLabel = UiKit.Text(header.transform, "Title", Title, 54f, Color.white, true);
            TitleLabel.alignment = TextAlignmentOptions.MidlineLeft;
            TitleLabel.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Stretch(TitleLabel.rectTransform, textLeft, 0f, 130f, 0f);

            var close = UiKit.Image(header.transform, "Close", UiKit.Circle, new Color(1f, 1f, 1f, 0.25f), true);
            UiKit.Place(close.rectTransform, new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(88f, 88f));
            var x = UiKit.Image(close.transform, "X", IconPainter.Close, Color.white);
            UiKit.Stretch(x.rectTransform, 24f, 24f, 24f, 24f);
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.onClick.AddListener(Close);
            close.gameObject.AddComponent<PressSquash>();

            Body = UiKit.Rect("Body", Sheet);
            UiKit.Stretch(Body, 32f, 32f, 32f, 128f + 24f);

            Build(Body);
            _root.SetActive(false);
        }

        /// <summary>Sizes the sheet to the safe area: full height on a phone, capped width on a tablet.</summary>
        protected virtual void Fit()
        {
            if (Sheet == null || FullBleed) return;
            var area = ((RectTransform)transform).rect;
            float w = Mathf.Min(MaxWidth, area.width - 32f);
            float h = area.height - 48f;
            Sheet.sizeDelta = new Vector2(w, h);
            Sheet.anchoredPosition = Vector2.zero;
        }

        // ---------------------------------------------------------------- helpers for bodies

        /// <summary>A pair of pill tabs; returns the selected index through <paramref name="onSelect"/>.</summary>
        protected static List<Button> Tabs(RectTransform parent, string[] labels, Action<int> onSelect, out RectTransform bar)
        {
            bar = UiKit.Rect("Tabs", parent);
            var track = bar.gameObject.AddComponent<Image>();
            track.sprite = UiKit.Rounded(40);
            track.type = Image.Type.Sliced;
            track.color = UiKit.SurfaceSunk;
            var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(8, 8, 8, 8);
            h.spacing = 8f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;

            var buttons = new List<Button>();
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                var pill = UiKit.Image(bar, "Tab" + i, UiKit.Rounded(34), Color.white, true);
                var t = UiKit.Text(pill.transform, "Label", labels[i], 34f, UiKit.Ink, true, TextAlignmentOptions.Center);
                UiKit.Stretch(t.rectTransform);
                var b = pill.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { Game.Sfx.Play(Game.SfxKind.Tap, 0.4f); onSelect(index); });
                buttons.Add(b);
            }
            return buttons;
        }

        protected static void HighlightTab(List<Button> tabs, int selected, Color accent)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                var img = tabs[i].GetComponent<Image>();
                img.color = i == selected ? accent : new Color(1f, 1f, 1f, 0f);
                var label = tabs[i].GetComponentInChildren<TMP_Text>();
                label.color = i == selected ? Color.white : UiKit.InkSoft;
            }
        }

        /// <summary>Removes every row from a list.</summary>
        protected static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                Destroy(c);
            }
        }

        /// <summary>A section heading inside a list.</summary>
        protected static TextMeshProUGUI SectionLabel(Transform list, string text)
        {
            var t = UiKit.Text(list, "Section", text.ToUpperInvariant(), 26f, UiKit.InkSoft, true);
            t.characterSpacing = 6f;
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 56f;
            t.alignment = TextAlignmentOptions.BottomLeft;
            t.margin = new Vector4(12f, 0f, 0f, 6f);
            return t;
        }
    }
}
