using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Emojis and chat lines over heads: a little white speech bubble pops up above whoever sent
    /// one (you included), rides along with them for a few seconds and fades. While the chat panel
    /// is open, bubbles it would hide sit on its top edge instead.
    /// </summary>
    public class EmojiBubbles : MonoBehaviour
    {
        class Live { public RectTransform Root; public Transform Target; public float Until; }

        readonly List<Live> _live = new();
        RectTransform _rect;

        void Awake() => _rect = (RectTransform)transform;

        void OnEnable()
        {
            VisitorBot.Emoted += Show;
            VisitChat.Heard += Said;
        }

        void OnDisable()
        {
            VisitorBot.Emoted -= Show;
            VisitChat.Heard -= Said;
        }

        /// <summary>One bubble per head: a new one replaces the old.</summary>
        void Clear(Transform target)
        {
            for (int i = _live.Count - 1; i >= 0; i--)
                if (_live[i].Target == target) { if (_live[i].Root != null) Destroy(_live[i].Root.gameObject); _live.RemoveAt(i); }
        }

        /// <summary>
        /// A chat line (VisitChat): the words in a speech bubble over whoever said them, for a few
        /// seconds — your own over your capybara too. Someone who isn't your friend gets "…", with
        /// an "Add friend" chip: tapping that bubble (and only it: the rest of the screen still
        /// reaches the island) asks them to be friends (ChatAsk).
        /// </summary>
        void Said(VisitChat.Line line)
        {
            if (this == null || line == null || string.IsNullOrEmpty(line.Text)) return;
            string words = VisitChat.Shown(line);
            Transform target = null;
            if (line.Mine)
            {
                var king = FindFirstObjectByType<PlayerAvatar>();
                if (king != null) target = king.transform.Find("Sprite") ?? king.transform;
            }
            else
            {
                var bot = VisitorBot.ByNetId(line.From);
                if (bot != null) target = bot.transform.Find("Sprite") ?? bot.transform;
            }
            if (target == null) return;
            Clear(target);

            // A stranger's "…": the bubble (only the bubble) can be tapped to ask them to be
            // friends, and says so with a small chip beside the dots.
            bool ask = ChatAsk.CanAsk(line);
            const float widest = 440f, pad = 18f;
            var bubble = UiKit.Card(transform, "ChatBubble", Color.white, 26, shadow: false, raycast: ask);
            var r = bubble.rectTransform;
            r.pivot = new Vector2(0.5f, 0f);
            var t = UiKit.Text(r, "Line", words, 26f, UiKit.Ink, false, TMPro.TextAlignmentOptions.Center);
            t.richText = false;
            t.overflowMode = TMPro.TextOverflowModes.Overflow;
            var loc = t.GetComponent<LocText>();
            if (loc != null) { loc.NoTranslate = true; loc.NoScale = true; }   // as typed, and sized for its bubble
            if (ask)
            {
                t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                var row = bubble.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset((int)pad, 10, 10, 12);
                row.spacing = 10f;
                row.childAlignment = TextAnchor.MiddleCenter;
                row.childControlWidth = row.childControlHeight = true;
                row.childForceExpandWidth = row.childForceExpandHeight = false;
                RtlLayout.Add(row);
                var fit = bubble.gameObject.AddComponent<ContentSizeFitter>();
                fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                AskChip.Create(r, line.From, 20f, clickable: false);
                var tap = bubble.gameObject.AddComponent<Button>();
                tap.transition = Selectable.Transition.None;
                string member = line.From;
                tap.onClick.AddListener(() =>
                {
                    if (ChatAsk.Asked(member)) return;
                    ChatAsk.Ask(member);
                    if (r != null) { r.DOKill(); r.localScale = Vector3.one; r.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.6f); }
                });
            }
            else
            {
                var size = t.GetPreferredValues(words, widest - pad * 2f, 0f);
                float w = Mathf.Min(widest, size.x + pad * 2f + 2f), h = size.y + pad * 1.4f;
                r.sizeDelta = new Vector2(Mathf.Max(90f, w), Mathf.Max(64f, h));
                UiKit.Stretch(t.rectTransform, pad, pad * 0.7f, pad, pad * 0.7f);
            }
            var tail = UiKit.Image(r, "Tail", IconPainter.Arrow, Color.white);
            if (ask) tail.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            tail.rectTransform.anchorMin = tail.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            tail.rectTransform.sizeDelta = new Vector2(30f, 30f);
            tail.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);

            r.localScale = Vector3.one * 0.2f;
            r.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            if (!line.Mine) Sfx.Play(SfxKind.Plop, 0.35f);
            // A bubble you can tap stays a little longer, so there's time to.
            float stay = ask ? 6f : Mathf.Clamp(2.5f + words.Length * 0.06f, 3f, 7f);
            _live.Add(new Live { Root = r, Target = target, Until = Time.time + stay });
            Place(_live[_live.Count - 1]);
        }

        void Show(Transform target, Game.Emoji e)
        {
            if (target == null) return;
            Clear(target);

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

        // ---------------------------------------------------------------- "Add friend" over strangers

        /// <summary>Each real player here who isn't your friend yet: an "Add friend" chip over their head.</summary>
        readonly Dictionary<VisitorBot, RectTransform> _asks = new();
        float _nextAskScan;

        /// <summary>
        /// Between words too, not only on their "…" bubble: anyone real on the island who isn't
        /// your friend (and isn't blocked) carries a small "Add friend" chip over their head, which
        /// you tap to ask. It steps aside while they have a bubble up, and goes once you're friends.
        /// </summary>
        void KeepAskChips()
        {
            if (Time.unscaledTime >= _nextAskScan)
            {
                _nextAskScan = Time.unscaledTime + 0.5f;
                foreach (var bot in VisitorBot.All)
                {
                    var who = bot != null ? bot.Who : null;
                    bool wants = who != null && who.IsReal && !string.IsNullOrEmpty(who.member) && !string.IsNullOrEmpty(who.code)
                                 && RealFriends.Find(who.member) == null && !RealFriends.IsBlocked(who.member);
                    if (wants && !_asks.ContainsKey(bot))
                    {
                        var chip = AskChip.Create(transform, who.member, 22f, clickable: true);
                        var r = (RectTransform)chip.transform;
                        r.pivot = new Vector2(0.5f, 0f);
                        var fit = r.gameObject.AddComponent<ContentSizeFitter>();
                        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        r.SetAsFirstSibling();   // under any speech bubble
                        _asks[bot] = r;
                    }
                    else if (!wants && _asks.TryGetValue(bot, out var gone)) { if (gone != null) Destroy(gone.gameObject); _asks.Remove(bot); }
                }
                var left = new List<VisitorBot>();
                foreach (var pair in _asks) if (pair.Key == null) { if (pair.Value != null) Destroy(pair.Value.gameObject); left.Add(pair.Key); }
                foreach (var b in left) _asks.Remove(b);
            }

            var cam = Camera.main;
            foreach (var pair in _asks)
            {
                var bot = pair.Key;
                var r = pair.Value;
                if (bot == null || r == null || cam == null) continue;
                var body = bot.transform.Find("Sprite") ?? bot.transform;
                bool talking = false;
                foreach (var l in _live) if (l.Target == body) talking = true;
                bool show = !talking && bot.isActiveAndEnabled;
                if (r.gameObject.activeSelf != show) r.gameObject.SetActive(show);
                if (!show) continue;
                var top = body.position + Vector3.up * 2.4f;
                if (body.TryGetComponent<SpriteRenderer>(out var sr)) top = new Vector3(sr.bounds.center.x, sr.bounds.max.y + 0.15f, 0f);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, cam.WorldToScreenPoint(top), null, out var local)) continue;
                r.anchoredPosition = KeepClearOfChat(local + new Vector2(0f, 46f), r.rect.size);   // above their name tag
            }
        }

        void LateUpdate()
        {
            KeepAskChips();
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
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, null, out var local)) return;
            var at = local + new Vector2(0f, 10f);
            l.Root.anchoredPosition = KeepClearOfChat(at, l.Root.rect.size);
        }

        /// <summary>
        /// With the chat panel open, a bubble that would sit under it (yours most of all: your
        /// capybara is in the middle of the screen, where the panel is, and a phone's keyboard
        /// lifts the panel higher still) rides on the panel's top edge instead, so it's seen.
        /// Never pushed off the top of the screen.
        /// </summary>
        Vector2 KeepClearOfChat(Vector2 at, Vector2 size)
        {
            var panel = ChatBar.OpenPanel;
            if (panel == null || !panel.gameObject.activeInHierarchy) return at;
            panel.GetWorldCorners(_corners);
            Vector2 low = _rect.InverseTransformPoint(_corners[0]), high = _rect.InverseTransformPoint(_corners[2]);
            float half = size.x * 0.5f;
            bool over = at.x + half > low.x && at.x - half < high.x && at.y < high.y + 14f && at.y + size.y > low.y;
            if (!over) return at;
            float lifted = Mathf.Min(high.y + 14f, _rect.rect.yMax - size.y - 8f);
            return new Vector2(at.x, Mathf.Max(at.y, lifted));   // only ever up
        }

        readonly Vector3[] _corners = new Vector3[4];
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

        // The chat button (ChatBar) goes right after this one in the drawing order: once the HUD
        // has put the emoji button under the screens, so chat is under them too.
        void Start() => ChatBar.Create(transform.parent).transform.SetSiblingIndex(transform.GetSiblingIndex() + 1);

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
