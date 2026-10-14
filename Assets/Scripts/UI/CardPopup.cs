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
        /// <summary>The rest of a card too long for one page: shown next, before anything queued.</summary>
        (string title, string body, Sprite sprite, System.Action closed)? _nextPage;
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
            if (!_showing && !MoviePlaying) ShowNext();
        }

        /// <summary>A movie has the screen: cards wait for it to end rather than pop up underneath.</summary>
        static bool MoviePlaying => Cutscene.Playing || AwakeningScreen.IsOpen;

        void Update()
        {
            if (!_showing && _queue.Count > 0 && !MoviePlaying) ShowNext();
        }

        void ShowNext()
        {
            if (_nextPage == null && _queue.Count == 0)
            {
                _showing = false;
                if (panel != null) panel.SetActive(false);
                Time.timeScale = _timeScaleBefore;
                return;
            }

            if (!_showing) _timeScaleBefore = Time.timeScale > 0f ? Time.timeScale : 1f;
            _showing = true;
            Time.timeScale = 0f;

            var (title, body, sprite, closed) = _nextPage ?? _queue.Dequeue();
            _nextPage = null;
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

            // Too much for one card: the rest goes on the next page (same title and picture), and
            // whatever waits for the card waits for its last page.
            // Cut in the language it is read in, so each page shows as it should either way.
            if (bodyLabel != null && Overflows() && Split(Loc.T(body), out var first, out var rest))
            {
                bodyLabel.text = first;
                _nextPage = (title, rest, sprite, closed);
                _current = null;
            }
            if (card != null)
            {
                card.DOKill();
                card.localScale = Vector3.one * 0.3f;
                card.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        bool Overflows()
        {
            Canvas.ForceUpdateCanvases();
            bodyLabel.ForceMeshUpdate();
            return bodyLabel.isTextOverflowing;
        }

        /// <summary>
        /// Splits a card's text in two: at the paragraph break nearest the middle, else at the
        /// sentence end nearest it. False if there is nowhere sensible to cut.
        /// </summary>
        static bool Split(string body, out string first, out string rest)
        {
            first = body;
            rest = null;
            int cut = Nearest(body, "\n");
            if (cut < 0) cut = Nearest(body, ". ");
            if (cut <= 0 || cut >= body.Length) return false;
            first = body.Substring(0, cut).Trim();
            rest = body.Substring(cut).Trim();
            return first.Length > 0 && rest.Length > 0;
        }

        /// <summary>Where to cut after the <paramref name="mark"/> nearest the middle, or -1.</summary>
        static int Nearest(string s, string mark)
        {
            int mid = s.Length / 2, best = -1;
            for (int i = s.IndexOf(mark, System.StringComparison.Ordinal); i >= 0; i = s.IndexOf(mark, i + 1, System.StringComparison.Ordinal))
            {
                int at = i + (mark == "\n" ? 0 : 1);   // a sentence keeps its full stop
                if (best < 0 || Mathf.Abs(at - mid) < Mathf.Abs(best - mid)) best = at;
            }
            return best;
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
