using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Emojis over heads: a little white speech bubble pops up above whoever sent one, rides along
    /// with them for a couple of seconds and fades.
    /// </summary>
    public class EmojiBubbles : MonoBehaviour
    {
        class Live { public RectTransform Root; public Transform Target; public float Until; }

        readonly List<Live> _live = new();
        RectTransform _rect;

        void Awake() => _rect = (RectTransform)transform;
        void OnEnable() => VisitorBot.Emoted += Show;
        void OnDisable() => VisitorBot.Emoted -= Show;

        void Show(Transform target, Game.Emoji e)
        {
            if (target == null) return;
            // One bubble per head: a new emoji replaces the old.
            for (int i = _live.Count - 1; i >= 0; i--)
                if (_live[i].Target == target) { Destroy(_live[i].Root.gameObject); _live.RemoveAt(i); }

            var bubble = UiKit.Card(transform, "EmojiBubble", Color.white, 30, shadow: false, raycast: false);
            var r = bubble.rectTransform;
            r.sizeDelta = new Vector2(112f, 104f);
            r.pivot = new Vector2(0.5f, 0f);
            var tail = UiKit.Image(r, "Tail", IconPainter.Arrow, Color.white);
            tail.rectTransform.anchorMin = tail.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            tail.rectTransform.sizeDelta = new Vector2(30f, 30f);
            tail.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            var icon = UiKit.Emoji(r, e, 76f);
            icon.anchoredPosition = new Vector2(0f, 2f);

            r.localScale = Vector3.one * 0.2f;
            r.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            Sfx.Play(SfxKind.Plop, 0.4f);
            _live.Add(new Live { Root = r, Target = target, Until = Time.time + 2.4f });
            Place(_live[_live.Count - 1]);
        }

        void LateUpdate()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var l = _live[i];
                if (l.Root == null) { _live.RemoveAt(i); continue; }
                if (l.Target == null || Time.time > l.Until)
                {
                    var root = l.Root;
                    _live.RemoveAt(i);
                    root.DOKill();
                    root.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() => { if (root != null) Destroy(root.gameObject); });
                    continue;
                }
                Place(l);
            }
        }

        void Place(Live l)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var top = l.Target.position + Vector3.up * 2.4f;
            var renderer = l.Target.GetComponent<SpriteRenderer>();
            if (renderer != null) top = new Vector3(renderer.bounds.center.x, renderer.bounds.max.y + 0.3f, 0f);
            var screen = cam.WorldToScreenPoint(top);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, null, out var local))
                l.Root.anchoredPosition = local + new Vector2(0f, 10f);
        }
    }

    /// <summary>
    /// The emoji button: shown whenever someone else is on the island. Tap it for a tray of
    /// emojis; tap one to send it.
    /// </summary>
    public class EmojiBar : MonoBehaviour
    {
        static readonly Game.Emoji[] Choices = { Game.Emoji.Wave, Game.Emoji.Heart, Game.Emoji.Smile, Game.Emoji.Laugh, Game.Emoji.Wow, Game.Emoji.Star };

        GameObject _button, _tray;

        public static EmojiBar Create(Transform parent)
        {
            var root = UiKit.Rect("EmojiBar", parent);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(1f, 0f);
            root.anchoredPosition = new Vector2(-20f, 250f);
            root.sizeDelta = new Vector2(120f, 120f);
            var bar = root.gameObject.AddComponent<EmojiBar>();
            bar.Build(root);
            return bar;
        }

        void Build(RectTransform root)
        {
            var button = UiKit.RoundButton(root, "Emoji", null, null, UiKit.Hex("FFD24A"), ToggleTray, 112f);
            _button = button.transform.parent.gameObject;
            var icon = button.transform.Find("Icon");
            if (icon != null) icon.gameObject.SetActive(false);
            var face = UiKit.Emoji(button.transform, Game.Emoji.Smile, 78f);
            face.anchoredPosition = Vector2.zero;

            _tray = UiKit.Card(root, "Tray", UiKit.Surface, 40).gameObject;
            var tr = (RectTransform)_tray.transform;
            tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(1f, 0.5f);
            tr.anchoredPosition = new Vector2(-124f, 0f);
            tr.sizeDelta = new Vector2(Choices.Length * 92f + 24f, 108f);
            var h = _tray.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(12, 12, 12, 12);
            h.spacing = 8f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = false;
            foreach (var e in Choices)
            {
                var choice = e;
                var slot = UiKit.Image(_tray.transform, e.ToString(), UiKit.Rounded(24), new Color(0f, 0f, 0f, 0.04f), true);
                slot.rectTransform.sizeDelta = new Vector2(84f, 84f);
                UiKit.Emoji(slot.transform, e, 64f);
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { VisitorBot.PlayerEmote(choice); _tray.SetActive(false); });
                slot.gameObject.AddComponent<PressSquash>();
            }
            _tray.SetActive(false);
        }

        void ToggleTray()
        {
            _tray.SetActive(!_tray.activeSelf);
            if (_tray.activeSelf) UiKit.PopIn(_tray.transform, 0.8f);
        }

        void Update()
        {
            bool someone = VisitorBot.Current != null && !UiScreen.AnyOpen;
            if (_button.activeSelf != someone) _button.SetActive(someone);
            if (!someone && _tray.activeSelf) _tray.SetActive(false);
        }
    }
}
