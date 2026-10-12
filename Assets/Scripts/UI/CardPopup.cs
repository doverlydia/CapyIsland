using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The first-discovery card (Rewards/Cards) and the mote card: the game pauses, a card zooms
    /// up with the item's picture, its name and a line about it, and waits for a tap. Cards found
    /// while one is showing queue up behind it.
    /// </summary>
    public class CardPopup : MonoBehaviour
    {
        static CardPopup _instance;

        [SerializeField] GameObject panel;
        [SerializeField] RectTransform card;
        [SerializeField] Image picture;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text bodyLabel;
        [SerializeField] Button dismiss;

        readonly Queue<(string title, string body, Sprite sprite, System.Action closed)> _queue = new();
        System.Action _current;
        float _timeScaleBefore = 1f;
        bool _showing;

        public static bool IsShowing => _instance != null && _instance._showing;

        void Awake()
        {
            _instance = this;
            if (panel != null) panel.SetActive(false);
            if (dismiss != null) dismiss.onClick.AddListener(Dismiss);
            // A long title shrinks to fit its line rather than spilling into the text below.
            if (titleLabel != null)
            {
                titleLabel.fontSizeMax = titleLabel.fontSize;
                titleLabel.fontSizeMin = titleLabel.fontSize * 0.6f;
                titleLabel.enableAutoSizing = true;
            }
            // The card is a fixed size: with Easy Reading the text grows as far as the card
            // allows and no further (tap or hold it to read it in the big reader).
            if (bodyLabel != null) bodyLabel.overflowMode = TextOverflowModes.Ellipsis;
        }

        void OnEnable() => Cards.Found += OnCardFound;
        void OnDisable() => Cards.Found -= OnCardFound;

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
            if (_showing) Time.timeScale = _timeScaleBefore;
        }

        void OnCardFound(CardDef def, string item)
        {
            var catalog = ItemCatalog.Instance;
            var sprite = catalog != null ? catalog.Icon(item) : null;
            Enqueue(def.Name, def.Description, sprite, null);
        }

        /// <summary>Shows any card — a mote, a discovery — pausing the game until it is tapped.</summary>
        public static void Show(string title, string body, Sprite sprite, System.Action closed = null)
        {
            if (_instance == null) { closed?.Invoke(); return; }
            _instance.Enqueue(title, body, sprite, closed);
        }

        void Enqueue(string title, string body, Sprite sprite, System.Action closed)
        {
            _queue.Enqueue((title, body, sprite, closed));
            if (!_showing) ShowNext();
        }

        void ShowNext()
        {
            if (_queue.Count == 0)
            {
                _showing = false;
                if (panel != null) panel.SetActive(false);
                Time.timeScale = _timeScaleBefore;
                return;
            }

            if (!_showing) _timeScaleBefore = Time.timeScale > 0f ? Time.timeScale : 1f;
            _showing = true;
            Time.timeScale = 0f;

            var (title, body, sprite, closed) = _queue.Dequeue();
            _current = closed;
            if (titleLabel != null) titleLabel.text = title;
            if (bodyLabel != null) bodyLabel.text = body;
            if (picture != null)
            {
                picture.sprite = sprite;
                picture.enabled = sprite != null;
                // Painted icons are plain white and would vanish on the cream card: colour them in.
                picture.color = IconPainter.IsIcon(sprite) ? UiKit.Primary : Color.white;
            }
            if (panel != null) panel.SetActive(true);
            if (card != null)
            {
                card.DOKill();
                card.localScale = Vector3.one * 0.3f;
                card.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        void Dismiss()
        {
            Sfx.Play(SfxKind.Tap, 0.5f);
            var closed = _current;
            _current = null;
            ShowNext();
            closed?.Invoke();
        }
    }
}
