using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// "There's more below": when a scrolling list runs past the bottom of its window, a small round
    /// button floats at the bottom of it with an arrow pointing down, bobbing gently, so it's clear
    /// there's more to read. Tapping it glides the list down most of a page; it fades away once you
    /// reach the bottom (and never shows for a list that fits).
    ///
    /// Every list made with <see cref="UiKit.ScrollList"/> on a screen gets one, in the colour of
    /// that screen's header band; a screen can name its own colour with <see cref="Add"/>. Lists
    /// that scroll sideways (the map) and lists outside a screen (the chat, the big-text reader)
    /// don't get one.
    ///
    /// It sits on a layer of its own over the list (same place, same size), drawn after it: inside
    /// the list the words were drawn over it. The layer follows the list whichever way the screen is
    /// turned, and only catches taps while the button is showing.
    /// </summary>
    public class MoreBelowCue : MonoBehaviour
    {
        const float Size = 72f;
        /// <summary>How much of the list (canvas units) must be out of sight below before the cue shows.</summary>
        const float MinHidden = 80f;

        ScrollRect _scroll;
        RectTransform _list;
        RectTransform _layer, _button;
        CanvasGroup _group;
        Image _disc;
        Tween _glide;
        bool _ownColour, _onlyOnScreens, _checked;

        /// <summary>
        /// A cue on <paramref name="scroll"/>, in <paramref name="accent"/>. A list that already has
        /// one (every <see cref="UiKit.ScrollList"/>) keeps it and just takes the colour.
        /// </summary>
        public static MoreBelowCue Add(ScrollRect scroll, Color accent)
        {
            var cue = Find(scroll) ?? Make(scroll);
            if (cue == null) return null;
            cue._ownColour = true;
            cue._onlyOnScreens = false;
            cue.Paint(accent);
            return cue;
        }

        /// <summary>
        /// The one every <see cref="UiKit.ScrollList"/> gets: it takes its screen's colour when it
        /// first shows, and quietly goes away if it isn't on a screen or the list scrolls sideways.
        /// </summary>
        internal static MoreBelowCue AddDefault(ScrollRect scroll)
        {
            var cue = Find(scroll) ?? Make(scroll);
            if (cue != null && !cue._ownColour) cue._onlyOnScreens = true;
            return cue;
        }

        static MoreBelowCue Find(ScrollRect scroll)
        {
            if (scroll == null || scroll.transform.parent == null) return null;
            foreach (Transform sibling in scroll.transform.parent)
                if (sibling.TryGetComponent<MoreBelowCue>(out var cue) && cue._scroll == scroll) return cue;
            return null;
        }

        static MoreBelowCue Make(ScrollRect scroll)
        {
            if (scroll == null || scroll.transform.parent == null) return null;
            var list = (RectTransform)scroll.transform;
            var layer = UiKit.Rect(scroll.name + "MoreBelow", list.parent);
            // Never one more thing for a layout group to lay out beside the list.
            layer.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            layer.SetSiblingIndex(list.GetSiblingIndex() + 1);

            var holder = UiKit.Rect("More", layer);
            UiKit.Place(holder, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(Size, Size));
            var group = holder.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = group.interactable = false;

            var disc = UiKit.Image(holder, "Button", UiKit.Circle, UiKit.Primary, true);
            UiKit.Place(disc.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Size, Size));
            UiKit.AddShadow(disc.rectTransform, 10f, 5f, 0.22f);
            var arrow = UiKit.Image(disc.transform, "Arrow", IconPainter.Chevron, Color.white);
            arrow.preserveAspect = true;
            UiKit.Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(34f, 34f));
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);   // the chevron points right: turned to point down
            var button = disc.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            // It was born disabled (the group isn't interactable yet) while still on the default
            // colour tint, which faded it to half: undo that, or it stays see-through for good.
            disc.CrossFadeColor(Color.white, 0f, true, true);
            disc.gameObject.AddComponent<PressSquash>();

            var cue = layer.gameObject.AddComponent<MoreBelowCue>();
            cue._scroll = scroll;
            cue._list = list;
            cue._layer = layer;
            cue._button = disc.rectTransform;
            cue._group = group;
            cue._disc = disc;
            button.onClick.AddListener(cue.GlideDown);
            cue.Follow();
            return cue;
        }

        void Paint(Color accent)
        {
            if (_disc != null) _disc.color = accent;
        }

        /// <summary>Stops a glide under way (the list is about to be filled afresh).</summary>
        public void Stop() => _glide?.Kill();

        void OnDisable() => Stop();

        void OnDestroy() => Stop();

        /// <summary>The list glides down most of a page (a line or two of the old one stays in view).</summary>
        void GlideDown()
        {
            if (_scroll == null || _scroll.content == null) return;
            float view = View.rect.height;
            float spare = _scroll.content.rect.height - view;
            if (spare <= 1f) return;
            Sfx.Play(SfxKind.Tap, 0.4f);
            _scroll.StopMovement();
            float from = _scroll.verticalNormalizedPosition;
            float to = Mathf.Clamp01(from - view * 0.8f / spare);
            var scroll = _scroll;
            Stop();
            _glide = DOTween.To(() => scroll.verticalNormalizedPosition, v => scroll.verticalNormalizedPosition = v, to, 0.45f)
                .SetEase(Ease.OutCubic).SetUpdate(true);
        }

        RectTransform View => _scroll.viewport != null ? _scroll.viewport : _list;

        /// <summary>
        /// Wanted here? Only on a list that scrolls up and down, and (for the one every list gets) on
        /// a screen. Asked once, the first time it is on show, after the screen has finished setting
        /// the list up.
        /// </summary>
        bool Wanted()
        {
            if (!_scroll.vertical || _scroll.horizontal) return false;
            if (!_onlyOnScreens) return true;
            var screen = GetComponentInParent<UiScreen>(true);
            if (screen == null) return false;
            // The screen's colour: its header band.
            if (!_ownColour && screen.transform.Find("Root/Sheet/Header") is Transform header && header.TryGetComponent<Image>(out var band))
                Paint(band.color);
            return true;
        }

        /// <summary>Keeps the layer exactly over the list, and just after it, whichever way the screen is turned.</summary>
        void Follow()
        {
            _layer.anchorMin = _list.anchorMin;
            _layer.anchorMax = _list.anchorMax;
            _layer.pivot = _list.pivot;
            _layer.offsetMin = _list.offsetMin;
            _layer.offsetMax = _list.offsetMax;
            _layer.localScale = _list.localScale;
            int after = _list.GetSiblingIndex() + 1;
            if (_layer.parent == _list.parent && _layer.GetSiblingIndex() != after)
                _layer.SetSiblingIndex(_layer.GetSiblingIndex() < after ? after - 1 : after);
        }

        void Update()
        {
            if (_scroll == null || _list == null || _list.parent != _layer.parent) { Destroy(gameObject); return; }
            if (!_checked)
            {
                _checked = true;
                if (!Wanted()) { Destroy(gameObject); return; }
            }
            Follow();
            var content = _scroll.content;
            // Only when a real piece is still out of sight below (a list a few pixels too tall, like
            // the Hobbies carousel, has nothing to scroll to).
            float hidden = content != null ? (content.rect.height - View.rect.height) * _scroll.verticalNormalizedPosition : 0f;
            bool more = _list.gameObject.activeInHierarchy && content != null && hidden > MinHidden;
            _group.alpha = Mathf.MoveTowards(_group.alpha, more ? 1f : 0f, Time.unscaledDeltaTime * 5f);
            _group.blocksRaycasts = _group.interactable = more;
            if (_group.alpha > 0f) _button.anchoredPosition = new Vector2(0f, 5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.6f)));
        }
    }
}
