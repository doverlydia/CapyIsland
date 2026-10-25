using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// A character talking to you: portrait, name, and their lines one page at a time — tap to turn
    /// the page (the original splits dialogue on [br]). The last page can offer choices ("Pay 20
    /// Gems" / "Cancel", the bank's trades); with none, tapping it closes.
    ///
    /// A tap anywhere around the panel does the same as tapping it (TapOutside). On a page of
    /// choices it takes the one that just closes ("Not now", "Cancel": no action of its own); when
    /// every choice does something, the question waits for an answer.
    ///
    /// One instance in the HUD; anything opens it through <see cref="Show"/>.
    /// </summary>
    public class SpeechDialog : MonoBehaviour
    {
        public struct Choice
        {
            public string Label;
            public Action Action;
            public bool Enabled;
            public Choice(string label, Action action, bool enabled = true) { Label = label; Action = action; Enabled = enabled; }
        }

        static SpeechDialog _instance;

        [SerializeField] GameObject panel;
        [SerializeField] Image portrait;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text bodyLabel;
        [SerializeField] Button pageButton;       // the whole panel: tap to turn the page
        [SerializeField] TMP_Text hintLabel;       // "tap to continue"
        [SerializeField] List<Button> choiceButtons = new();

        string[] _pages = Array.Empty<string>();
        int _page;
        Action _onClosed;
        readonly List<Choice> _choices = new();
        GameObject _outside;

        public static bool IsOpen => _instance != null && _instance.panel != null && _instance.panel.activeSelf;

        void Awake()
        {
            _instance = this;
            if (panel != null) panel.SetActive(false);
            if (pageButton != null) pageButton.onClick.AddListener(NextPage);
            BuildOutside();
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                int index = i;
                if (choiceButtons[i] != null) choiceButtons[i].onClick.AddListener(() => Choose(index));
            }
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>An invisible backdrop behind the panel, there while it is open, to hear taps around it.</summary>
        void BuildOutside()
        {
            if (panel == null) return;
            var img = UiKit.Image(panel.transform.parent, "Outside", null, Color.clear, true);
            UiKit.Stretch(img.rectTransform, -400f, -400f, -400f, -400f);
            img.transform.SetAsFirstSibling();
            TapOutside.Add(img, (RectTransform)panel.transform, Outside);
            _outside = img.gameObject;
            _outside.SetActive(false);
        }

        void Outside()
        {
            bool choosing = _page >= _pages.Length - 1 && _choices.Count > 0;
            if (!choosing) { NextPage(); return; }
            for (int i = 0; i < _choices.Count; i++)
                if (_choices[i].Action == null && _choices[i].Enabled) { Choose(i); return; }
        }

        /// <summary>Opens the dialog. Choices appear on the last page.</summary>
        public static void Show(Sprite face, string speaker, string[] pages, Action onClosed = null,
            params (string label, Action action)[] choices)
        {
            var list = new List<Choice>();
            foreach (var c in choices) list.Add(new Choice(c.label, c.action));
            ShowChoices(face, speaker, pages, onClosed, list);
        }

        public static void ShowChoices(Sprite face, string speaker, string[] pages, Action onClosed, List<Choice> choices)
        {
            if (_instance == null) { onClosed?.Invoke(); return; }
            _instance.Open(face, speaker, pages, onClosed, choices);
        }

        void Open(Sprite face, string speaker, string[] pages, Action onClosed, List<Choice> choices)
        {
            // A dialog already open closes first, so its own callback still runs.
            if (IsOpen) Close(runCallback: true);

            _pages = pages != null && pages.Length > 0 ? pages : new[] { "" };
            _page = 0;
            _onClosed = onClosed;
            _choices.Clear();
            if (choices != null) _choices.AddRange(choices);

            if (portrait != null)
            {
                portrait.sprite = face;
                portrait.enabled = face != null;
            }
            if (nameLabel != null) nameLabel.text = speaker ?? "";
            if (panel != null) panel.SetActive(true);
            if (_outside != null) _outside.SetActive(true);
            FitWidth();
            ShowPage();
        }

        /// <summary>Narrow portrait phones: never wider than the safe area allows.</summary>
        void FitWidth()
        {
            if (panel == null) return;
            var rect = panel.GetComponent<RectTransform>();
            var parent = rect.parent as RectTransform;
            if (parent == null) return;
            float max = parent.rect.width - 40f;
            rect.sizeDelta = new Vector2(Mathf.Min(1000f, max), rect.sizeDelta.y);
            FitHeight(rect);
        }

        float _baseHeight = -1f, _baseNameHeight, _baseBodyTop, _baseShadowHeight;
        RectTransform _shadow;

        /// <summary>
        /// Easy Reading: a taller panel for the bigger words. The body fits its words to the room it
        /// has, so without more room a long line would stay small; the name grows taller too, and
        /// the body starts below it. (Laid out in the scene at normal size; that is the 1x.)
        /// </summary>
        void FitHeight(RectTransform rect)
        {
            if (_baseHeight < 0f)
            {
                _baseHeight = rect.sizeDelta.y;
                if (nameLabel != null) _baseNameHeight = nameLabel.rectTransform.sizeDelta.y;
                if (bodyLabel != null) _baseBodyTop = -bodyLabel.rectTransform.offsetMax.y;
                _shadow = rect.parent.Find("PanelShadow") as RectTransform;
                if (_shadow != null) _baseShadowHeight = _shadow.sizeDelta.y;
            }
            float s = Accessibility.TextScale;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, _baseHeight * s);
            if (_shadow != null) _shadow.sizeDelta = new Vector2(_shadow.sizeDelta.x, _baseShadowHeight + _baseHeight * (s - 1f));
            if (nameLabel != null)
            {
                var n = nameLabel.rectTransform;
                n.sizeDelta = new Vector2(n.sizeDelta.x, _baseNameHeight * s);
            }
            if (bodyLabel != null)
            {
                var b = bodyLabel.rectTransform;
                b.offsetMax = new Vector2(b.offsetMax.x, -(_baseBodyTop + _baseNameHeight * (s - 1f)));
            }
        }

        void ShowPage()
        {
            if (bodyLabel != null) bodyLabel.text = _pages[_page].Trim();
            bool last = _page >= _pages.Length - 1;
            bool choosing = last && _choices.Count > 0;

            for (int i = 0; i < choiceButtons.Count; i++)
            {
                var b = choiceButtons[i];
                if (b == null) continue;
                bool used = choosing && i < _choices.Count;
                b.gameObject.SetActive(used);
                if (!used) continue;
                b.interactable = _choices[i].Enabled;
                var label = b.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = _choices[i].Label;
            }

            if (pageButton != null) pageButton.interactable = !choosing;
            if (hintLabel != null) hintLabel.text = choosing ? "" : last ? "tap to close" : "tap to continue";
        }

        void NextPage()
        {
            Game.Sfx.Play(Game.SfxKind.Tap, 0.5f);
            if (_page < _pages.Length - 1) { _page++; ShowPage(); return; }
            if (_choices.Count == 0) Close(runCallback: true);
        }

        void Choose(int index)
        {
            if (index < 0 || index >= _choices.Count) return;
            var action = _choices[index].Action;
            Close(runCallback: true);
            action?.Invoke();
        }

        void Close(bool runCallback)
        {
            if (panel != null) panel.SetActive(false);
            if (_outside != null) _outside.SetActive(false);
            var callback = _onClosed;
            _onClosed = null;
            if (runCallback) callback?.Invoke();
        }
    }
}
