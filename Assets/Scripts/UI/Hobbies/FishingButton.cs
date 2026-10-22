using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Fishing mode. At the water's edge a "Start fishing" button appears at the bottom of the
    /// screen; tap it and the bobber goes straight in, by the nearest fish, and the whole screen
    /// becomes your rod: once a fish is on, hold to reel and let go to give it slack; after a
    /// catch, a tap anywhere casts again. What to do is written large
    /// at the top of the screen — where your thumb isn't — in the colour of the line, with the
    /// catch bar under it. "Stop fishing" puts the rod away and lets you walk again; beside it,
    /// "Tackle" opens what you fish with (bait to put on, Mermaid's Shells to throw), and the
    /// capybara puts on or throws whatever you pick, there on the shore where you can watch.
    ///
    /// "Hold" is not something a player guesses from a tap-everything phone: if they hesitate, a
    /// hand acts it out — presses and stays down with a ring filling under the fingertip, or lifts
    /// off when it is time to let go.
    /// </summary>
    public class FishingButton : MonoBehaviour
    {
        GameObject _start, _mode;
        TextMeshProUGUI _say, _hint;
        Image _progress, _warn;
        RectTransform _progressTrack, _sayRect;
        float _flashUntil;

        enum Gesture { None, Hold, Release }
        RectTransform _demo, _hand, _ring;
        Image _handImage, _ringImage;
        Gesture _gesture;
        float _gestureSince;

        public static FishingButton Create(Transform parent)
        {
            var root = UiKit.Rect("Fishing", parent);
            UiKit.Stretch(root);
            var fb = root.gameObject.AddComponent<FishingButton>();
            fb.Build(root);
            return fb;
        }

        void Build(RectTransform root)
        {
            // Outside the mode: a friendly offer, bottom centre.
            var start = UiKit.Button(root, "StartFishing", "Start fishing", UiKit.ButtonStyle.Spring, Enter, 32f, IconPainter.Rod);
            var sr = (RectTransform)start.transform.parent;
            sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0.5f, 0f);
            sr.anchoredPosition = new Vector2(0f, 330f);   // above the speech box
            sr.sizeDelta = new Vector2(340f, 96f);
            _start = sr.gameObject;

            // The mode: everything else is the rod.
            var mode = UiKit.Rect("FishingMode", root);
            UiKit.Stretch(mode);
            _mode = mode.gameObject;
            var catcher = UiKit.Image(mode, "HoldAnywhere", null, new Color(0f, 0f, 0f, 0f), true);
            UiKit.Stretch(catcher.rectTransform);
            catcher.gameObject.AddComponent<HoldRelay>();

            // The words, large, top centre.
            var panel = UiKit.Image(mode, "Say", UiKit.Rounded(36), new Color(0.12f, 0.1f, 0.08f, 0.55f));
            panel.raycastTarget = false;
            _sayRect = panel.rectTransform;
            _sayRect.anchorMin = _sayRect.anchorMax = _sayRect.pivot = new Vector2(0.5f, 1f);
            _sayRect.anchoredPosition = new Vector2(0f, -250f);
            _sayRect.sizeDelta = new Vector2(620f, 170f);
            _say = UiKit.Text(panel.transform, "Text", "Wait…", 56f, Color.white, true, TextAlignmentOptions.Center);
            _say.raycastTarget = false;
            _say.rectTransform.anchorMin = new Vector2(0f, 0.42f);
            _say.rectTransform.anchorMax = Vector2.one;
            _say.rectTransform.offsetMin = new Vector2(20f, 0f);
            _say.rectTransform.offsetMax = new Vector2(-20f, -8f);
            _say.textWrappingMode = TextWrappingModes.NoWrap;
            _say.enableAutoSizing = true;
            _say.fontSizeMin = 30f;
            _say.fontSizeMax = 56f;
            _hint = UiKit.Text(panel.transform, "Hint", "", 26f, new Color(1f, 1f, 1f, 0.8f), false, TextAlignmentOptions.Center);
            _hint.raycastTarget = false;
            _hint.rectTransform.anchorMin = new Vector2(0f, 0.2f);
            _hint.rectTransform.anchorMax = new Vector2(1f, 0.44f);
            _hint.rectTransform.offsetMin = new Vector2(20f, 0f);
            _hint.rectTransform.offsetMax = new Vector2(-20f, 0f);

            var bar = UiKit.ProgressBar(panel.transform, "Catch", Color.white, 16f);
            _progressTrack = (RectTransform)bar.transform.parent;
            _progressTrack.anchorMin = new Vector2(0.08f, 0f);
            _progressTrack.anchorMax = new Vector2(0.92f, 0f);
            _progressTrack.pivot = new Vector2(0.5f, 0f);
            _progressTrack.anchoredPosition = new Vector2(0f, 18f);
            _progressTrack.sizeDelta = new Vector2(0f, 16f);
            _progressTrack.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
            _progress = bar;

            // A red ring around the panel while the line is at breaking point.
            _warn = UiKit.Image(mode, "Warn", UiKit.Rounded(40), Fishing.Red);
            _warn.raycastTarget = false;
            _warn.rectTransform.anchorMin = _warn.rectTransform.anchorMax = _warn.rectTransform.pivot = new Vector2(0.5f, 1f);
            _warn.rectTransform.anchoredPosition = new Vector2(0f, -240f);
            _warn.rectTransform.sizeDelta = new Vector2(640f, 190f);
            _warn.transform.SetSiblingIndex(panel.transform.GetSiblingIndex());

            // The way out: a clear, labelled button at the bottom in the middle, below where the
            // thumb rests while holding to reel. (Old Yuzu waits while fishing, so it has the space.)
            var stop = UiKit.Button(mode, "StopFishing", "Stop fishing", UiKit.ButtonStyle.Secondary, Exit, 32f, IconPainter.Close);
            var stopR = (RectTransform)stop.transform.parent;
            stopR.anchorMin = stopR.anchorMax = stopR.pivot = new Vector2(0.5f, 0f);
            stopR.anchoredPosition = new Vector2(-150f, 40f);
            stopR.sizeDelta = new Vector2(340f, 96f);

            BuildTackle(mode);
            BuildBoosts(mode);
            BuildDemo(mode);
            BuildTacklePanel(mode);   // last: over everything else in the mode
            _mode.SetActive(false);
        }

        // ---------------------------------------------------------------- the tackle button

        RectTransform _tackleRoot;
        GameObject _onHook, _singing;
        Image _onHookArt, _singingLeft;
        TextMeshProUGUI _onHookCount;
        string _onHookShown;

        static Sprite _tackleIcon;

        /// <summary>A tackle box: a lidded case with a carrying handle on top and a clasp at the front.</summary>
        static Sprite TackleIcon => _tackleIcon != null ? _tackleIcon : _tackleIcon = IconPainter.Paint(128, (x, y) =>
        {
            float box = IconPainter.Box(x, y, 0f, -0.2f, 0.82f, 0.5f, 0.12f);
            float seam = IconPainter.Box(x, y, 0f, 0.04f, 0.9f, 0.035f, 0f);
            float clasp = IconPainter.Box(x, y, 0f, 0.04f, 0.13f, 0.12f, 0.04f);
            float handle = Mathf.Max(Mathf.Abs(IconPainter.Box(x, y, 0f, 0.42f, 0.34f, 0.22f, 0.12f)) - 0.065f, 0.3f - y);
            return Mathf.Min(Mathf.Min(Mathf.Max(box, -seam), clasp), handle);
        });

        /// <summary>
        /// Beside Stop fishing: one button for everything you fish with, which opens the tackle
        /// panel. On its top right corner, the bait on your hook and how many are left in the bag;
        /// on its top left, while your Mermaid's Shell sings, the shell with a ring running down
        /// with its song. So what's helping is there at a glance without opening anything.
        /// </summary>
        void BuildTackle(RectTransform mode)
        {
            var button = UiKit.Button(mode, "Tackle", "Tackle", UiKit.ButtonStyle.Secondary, ToggleTackle, 30f, TackleIcon);
            var r = (RectTransform)button.transform.parent;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = new Vector2(170f, 40f);
            r.sizeDelta = new Vector2(250f, 96f);
            _tackleRoot = r;

            var hook = CornerDisc(r, "OnHook", new Vector2(1f, 1f), new Vector2(16f, 24f), BaitBrown);
            _onHook = hook.gameObject;
            _onHookArt = UiKit.Image(hook, "Art", null, Color.white);
            _onHookArt.preserveAspect = true;
            UiKit.Stretch(_onHookArt.rectTransform, 9f, 9f, 9f, 9f);
            _onHookCount = UiKit.Badge(hook, BaitBrown, 30f);
            _onHook.SetActive(false);

            var shell = CornerDisc(r, "Singing", new Vector2(0f, 1f), new Vector2(-16f, 24f), ShellTeal);
            _singing = shell.gameObject;
            _singingLeft = UiKit.Image(shell, "Left", IconPainter.Ring, ShellTeal);
            UiKit.Stretch(_singingLeft.rectTransform, -3f, -3f, -3f, -3f);
            _singingLeft.type = Image.Type.Filled;
            _singingLeft.fillMethod = Image.FillMethod.Radial360;
            _singingLeft.fillOrigin = (int)Image.Origin360.Top;
            _singingLeft.fillClockwise = false;
            var art = UiKit.Image(shell, "Art", HobbyData.ItemArt(MermaidCharms.Item), Color.white);
            art.preserveAspect = true;
            UiKit.Stretch(art.rectTransform, 10f, 10f, 10f, 10f);
            _singing.SetActive(false);
        }

        static readonly Color BaitBrown = UiKit.Hex("8A5A32"), ShellTeal = UiKit.Hex("3E9C8F");

        /// <summary>A little white disc with a faint coloured edge, hanging off a corner of the button.</summary>
        static RectTransform CornerDisc(RectTransform button, string name, Vector2 corner, Vector2 offset, Color edge)
        {
            var disc = UiKit.Image(button, name, UiKit.Circle, Color.white);
            UiKit.Place(disc.rectTransform, corner, offset, new Vector2(66f, 66f));
            var ring = UiKit.Image(disc.transform, "Edge", IconPainter.Ring, new Color(edge.r, edge.g, edge.b, 0.4f));
            UiKit.Stretch(ring.rectTransform);
            return disc.rectTransform;
        }

        void RefreshTackle(Fishing f)
        {
            // While a line is out, what's actually on its hook; between casts, what goes on next.
            var b = f.Now == Fishing.State.Idle ? Baits.Current : f.BaitOnHook;
            int left = Baits.Count(b);
            string key = b != null ? b.Id + "/" + left : "";
            if (key != _onHookShown)
            {
                bool changed = _onHookShown != null;
                _onHookShown = key;
                _onHook.SetActive(b != null);
                if (b != null)
                {
                    _onHookArt.sprite = HobbyData.ItemArt(b.Item);
                    _onHookCount.text = left.ToString();
                    _onHookCount.transform.parent.gameObject.SetActive(left > 0);
                    if (changed)
                    {
                        var t = _onHook.transform;
                        t.DOKill();
                        t.localScale = Vector3.one;
                        t.DOPunchScale(Vector3.one * 0.25f, 0.35f, 6, 0.6f).SetUpdate(true);
                    }
                }
            }

            var mine = MermaidCharms.Yours();
            if (_singing.activeSelf != (mine != null)) _singing.SetActive(mine != null);
            if (mine != null)
            {
                _singingLeft.fillAmount = mine.Left / MermaidCharms.Lasts;
                _singing.transform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 4f));   // singing away
            }
            if (_tackleOpen) RefreshPanel(f);
        }

        // ---------------------------------------------------------------- the tackle panel

        RectTransform _panel, _rows, _outside;
        bool _tackleOpen;
        string _rowsShown;
        TextMeshProUGUI _shellAction;   // counts down while your shell sings

        /// <summary>
        /// What you have to fish with, on a card by the Tackle button: each bait you own, to put on
        /// the hook, and the Mermaid's Shell, to throw. Choosing closes it so you can watch the
        /// capybara do it. A tap anywhere else closes it too, and it shuts by itself the moment a
        /// fish bites, since while it's open the screen isn't your rod.
        /// </summary>
        void BuildTacklePanel(RectTransform mode)
        {
            var outside = UiKit.Image(mode, "TackleOutside", null, new Color(0f, 0f, 0f, 0f), true);
            _outside = UiKit.Stretch(outside.rectTransform);
            var shut = outside.gameObject.AddComponent<Button>();
            shut.transition = Selectable.Transition.None;
            shut.onClick.AddListener(CloseTackle);

            var card = UiKit.Card(mode, "TacklePanel", UiKit.Surface, 32);
            _panel = card.rectTransform;
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0f);
            _panel.sizeDelta = new Vector2(640f, 300f);
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(20, 20, 12, 20);
            v.spacing = 10f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Its name, and a cross to close it.
            var head = UiKit.Rect("Head", card.transform);
            head.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
            var hh = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            hh.childAlignment = TextAnchor.MiddleLeft;
            hh.spacing = 12f;
            hh.childControlWidth = hh.childControlHeight = true;
            hh.childForceExpandWidth = false;
            hh.childForceExpandHeight = false;
            var icon = UiKit.Image(head, "Icon", TackleIcon, UiKit.Ink);
            icon.preserveAspect = true;
            var ile = icon.gameObject.AddComponent<LayoutElement>();
            ile.preferredWidth = ile.preferredHeight = 44f;
            var title = UiKit.Text(head, "Title", "Tackle", 36f, UiKit.Ink, true);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            var tle = title.gameObject.AddComponent<LayoutElement>();
            tle.flexibleWidth = 1f;
            tle.preferredHeight = 52f;
            var close = UiKit.Button(head, "Close", "", UiKit.ButtonStyle.Ghost, CloseTackle, 30f, IconPainter.Close);
            var cle = close.transform.parent.gameObject.AddComponent<LayoutElement>();
            cle.preferredWidth = 64f;
            cle.preferredHeight = 58f;

            _rows = UiKit.Rect("Rows", card.transform);
            var rv = _rows.gameObject.AddComponent<VerticalLayoutGroup>();
            rv.spacing = 10f;
            rv.childControlWidth = rv.childControlHeight = true;
            rv.childForceExpandWidth = true;
            rv.childForceExpandHeight = false;

            _outside.gameObject.SetActive(false);
            _panel.gameObject.SetActive(false);
        }

        void ToggleTackle()
        {
            if (_tackleOpen) { CloseTackle(); return; }
            var f = Fishing.Instance;
            if (f == null) return;
            if (f.Now == Fishing.State.Fight) { Hints.Show("Land this fish first!"); return; }
            _tackleOpen = true;
            _rowsShown = null;
            _outside.gameObject.SetActive(true);
            _panel.gameObject.SetActive(true);
            RefreshPanel(f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);
            UiKit.PopIn(_panel, 0.8f);
        }

        void OnDestroy()
        {
            if (_panel != null) _panel.DOKill();
            if (_onHook != null) _onHook.transform.DOKill();
        }

        void CloseTackle()
        {
            _tackleOpen = false;
            if (_panel == null) return;
            _panel.DOKill();
            _panel.gameObject.SetActive(false);
            _outside.gameObject.SetActive(false);
        }

        /// <summary>
        /// Upright, the panel opens over the buttons, in the middle. On a wide screen there isn't
        /// the height for that under the words at the top, so it opens beside the Tackle button.
        /// </summary>
        void PlacePanel()
        {
            var area = ((RectTransform)transform).rect;
            bool wide = area.width > area.height * 1.1f;
            _panel.pivot = wide ? new Vector2(0f, 0f) : new Vector2(0.5f, 0f);
            _panel.anchoredPosition = wide
                ? new Vector2(_tackleRoot.anchoredPosition.x + _tackleRoot.sizeDelta.x * 0.5f + 20f, 40f)
                : new Vector2(10f, 154f);
            _panel.sizeDelta = new Vector2(wide ? 600f : 640f, _panel.sizeDelta.y);
        }

        /// <summary>Builds the rows afresh when what you have changes; the shell's countdown ticks on its own.</summary>
        void RefreshPanel(Fishing f)
        {
            PlacePanel();
            var mine = MermaidCharms.Yours();
            if (mine != null && _shellAction != null)
                _shellAction.text = string.Format(StoryText.Charm.ChipSinging, Mathf.CeilToInt(mine.Left));

            var current = Baits.Current;
            string why = MermaidCharms.CantToss();
            bool shells = MermaidCharms.Count > 0 || mine != null;
            bool lineOut = f.Now != Fishing.State.Idle;
            bool straightOn = f.Now == Fishing.State.Waiting && f.BaitOnHook == null;   // see Fishing.PutOnBait
            var key = new System.Text.StringBuilder();
            foreach (var b in Baits.All) key.Append(b.Id).Append(Baits.Count(b)).Append(b == current ? "*," : ",");
            key.Append(shells).Append(MermaidCharms.Count).Append(why).Append(mine != null).Append(lineOut).Append(straightOn);
            string k = key.ToString();
            if (k == _rowsShown) return;
            _rowsShown = k;

            for (int i = _rows.childCount - 1; i >= 0; i--)
            {
                var old = _rows.GetChild(i).gameObject;
                old.SetActive(false);   // out of the layout now; gone at the end of the frame
                Destroy(old);
            }
            _shellAction = null;

            // First, what's on the hook, with a way to take it off: fishing plain isn't an item, so
            // it's not a row among them (the playtesters' word), just the hook's empty state.
            if (Baits.All.Length > 0 && (current != null || HasAnyBait())) HookLine(current, lineOut && f.BaitOnHook != current);   // a different one waits for the next cast

            bool anyBait = false;
            foreach (var b in Baits.All)
            {
                int n = Baits.Count(b);
                if (n <= 0) continue;
                anyBait = true;
                var kind = b;
                bool on = kind == current;
                Row(HobbyData.ItemArt(kind.Item), n, kind.Name, BaitEffect(kind), false,
                    on ? "On hook" : "Put on", on ? UiKit.ButtonStyle.Ghost : UiKit.ButtonStyle.Spring, on ? IconPainter.Check : null,
                    !on, on, () => ChooseBait(kind));
            }
            if (!anyBait) Note("No bait yet. Skipper Nori sells it, down on the beach.");

            if (shells)
            {
                bool singing = mine != null;
                string effect = why ?? string.Format("Sings for a minute. Bites ×{0} in its ring", Times(MermaidCharms.BiteBoost));
                var label = Row(HobbyData.ItemArt(MermaidCharms.Item), MermaidCharms.Count, StoryText.Charm.Name, effect, why != null && !singing,
                    singing ? string.Format(StoryText.Charm.ChipSinging, Mathf.CeilToInt(mine.Left)) : "Throw",
                    singing ? UiKit.ButtonStyle.Ghost : why == null ? UiKit.ButtonStyle.Primary : UiKit.ButtonStyle.Secondary, null,
                    why == null, singing, ThrowShell);
                if (singing) _shellAction = label;
            }
            else if (HobbyLog.Level(Hobby.Fishing) >= MermaidCharms.MinLevel) Note(StoryText.Charm.NoneLeft);
        }

        static bool HasAnyBait()
        {
            foreach (var b in Baits.All) if (Baits.Count(b) > 0) return true;
            return false;
        }

        /// <summary>
        /// The top line of the panel: "On the hook: Wiggly Worms" and a small "Take off", or "On
        /// the hook: nothing" when you're fishing plain. Taking it off is how you go without.
        /// </summary>
        void HookLine(BaitKind current, bool nextCast)
        {
            var line = UiKit.Rect("OnHook", _rows);
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
            var h = line.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(8, 4, 0, 0);
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            RtlLayout.Add(h);
            string what = current != null ? Loc.F("On the hook: {0}", Loc.T(current.Name)) : "On the hook: nothing";
            if (current != null && nextCast) what += "\n" + Loc.T("(from your next cast)");
            var t = UiKit.Text(line, "Text", what, 26f, current != null ? UiKit.Ink : UiKit.InkSoft, current != null);
            t.overflowMode = TextOverflowModes.Overflow;
            t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            if (current == null) return;
            var off = UiKit.Button(line, "TakeOff", "Take off", UiKit.ButtonStyle.Ghost, () => ChooseBait(null), 24f, IconPainter.Close);
            var le = off.transform.parent.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 170f;
            le.preferredHeight = 54f;
        }

        static string BaitEffect(BaitKind b)
        {
            bool quick = b.BiteSpeed > 1f, gentle = b.Gentle < 1f;
            return quick && gentle ? string.Format("Bites ×{0}, easier reeling", Times(b.BiteSpeed))
                : quick ? string.Format("Bites ×{0}", Times(b.BiteSpeed))
                : "Easier reeling";
        }

        /// <summary>
        /// One thing in the tackle panel: its picture (with how many you have on its corner), its
        /// name and what it does, and the button that uses it. Returns that button's words.
        /// </summary>
        TextMeshProUGUI Row(Sprite art, int count, string name, string effect, bool warn,
            string action, UiKit.ButtonStyle style, Sprite actionIcon, bool can, bool chosen, System.Action use)
        {
            var row = UiKit.Image(_rows, "Row", UiKit.Rounded(24), chosen ? Color.Lerp(UiKit.Surface, UiKit.Spring, 0.2f) : UiKit.SurfaceSunk);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 104f;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(14, 14, 10, 10);
            h.spacing = 14f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;

            // No picture (no bait): an empty hoop where one would be.
            var pic = UiKit.Image(row.transform, "Art", art != null ? art : IconPainter.Ring, art != null ? Color.white : UiKit.InkSoft);
            pic.preserveAspect = true;
            var ple = pic.gameObject.AddComponent<LayoutElement>();
            ple.preferredWidth = ple.preferredHeight = 76f;
            if (count > 0) UiKit.Badge(pic.transform, BaitBrown, 34f).text = count.ToString();

            var words = UiKit.Rect("Words", row.transform);
            var wle = words.gameObject.AddComponent<LayoutElement>();
            wle.flexibleWidth = 1f;
            wle.minWidth = 120f;
            var wv = words.gameObject.AddComponent<VerticalLayoutGroup>();
            wv.childAlignment = TextAnchor.MiddleLeft;
            wv.childControlWidth = wv.childControlHeight = true;
            wv.childForceExpandWidth = true;
            wv.childForceExpandHeight = false;
            var title = UiKit.Text(words, "Name", name, 28f, UiKit.Ink, true);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true;
            title.fontSizeMin = 20f;
            title.fontSizeMax = 28f;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
            var about = UiKit.Text(words, "Effect", effect, 21f, warn ? UiKit.Hex("B5523B") : UiKit.InkSoft);
            about.enableAutoSizing = true;
            about.fontSizeMin = 15f;
            about.fontSizeMax = 21f;
            about.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

            var button = UiKit.Button(row.transform, "Use", action, style, can ? use : null, 26f, actionIcon);
            var ble = button.transform.parent.gameObject.AddComponent<LayoutElement>();
            ble.preferredWidth = 176f;
            ble.preferredHeight = 72f;
            button.interactable = can;
            return button.GetComponentInChildren<TextMeshProUGUI>();
        }

        void Note(string text)
        {
            var t = UiKit.Text(_rows, "Note", text, 22f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            t.enableAutoSizing = true;
            t.fontSizeMin = 16f;
            t.fontSizeMax = 22f;
            t.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
        }

        /// <summary>A bait chosen: the panel closes and the capybara puts it on (Fishing.PutOnBait).</summary>
        void ChooseBait(BaitKind b)
        {
            CloseTackle();
            var f = Fishing.Instance;
            if (f == null) { Baits.Select(b); return; }
            bool onLine = f.PutOnBait(b);
            if (b == null) Hints.Show("Fishing without bait.");
            else if (onLine || f.Now == Fishing.State.Idle) Hints.Show(string.Format("{0} on the hook!", b.Name));
            else Hints.Show(string.Format("{0} goes on at your next cast.", b.Name));
        }

        /// <summary>
        /// Throw the shell: the panel closes and you watch it go (MermaidCharms.Toss has the
        /// capybara throw it). The good news waits until it has splashed down and started singing.
        /// </summary>
        void ThrowShell()
        {
            CloseTackle();
            string why = MermaidCharms.Toss();
            if (why != null) { Hints.Show(why); return; }
            DOVirtual.DelayedCall(1.1f, () => Hints.Show(StoryText.Charm.Tossed)).SetLink(gameObject);
        }

        // ---------------------------------------------------------------- what's helping right now

        RectTransform _boosts;
        string _boostsShown;

        /// <summary>
        /// Under the words: a little tag for each thing making the fish keener right now (the bait,
        /// a singing shell's ring, a contest), and what they come to together. They multiply, so
        /// with more than one it's not obvious otherwise.
        /// </summary>
        void BuildBoosts(RectTransform mode)
        {
            // A column under the words: a row of three ran off the sides of an upright phone.
            _boosts = UiKit.Rect("Boosts", mode);
            _boosts.anchorMin = _boosts.anchorMax = _boosts.pivot = new Vector2(0.5f, 1f);
            _boosts.anchoredPosition = new Vector2(0f, -436f);   // just under the words
            _boosts.sizeDelta = new Vector2(620f, 10f);
            var v = _boosts.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 8f;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = false;   // each tag as wide as its words (ContentSizeFitter on it)
            v.childControlHeight = true;
            v.childForceExpandWidth = v.childForceExpandHeight = false;
            _boosts.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>
        /// The words sit at the top in the middle, where a contest's panel also is: while it shows,
        /// they (and the boost tags under them) move down below it.
        /// </summary>
        void KeepClearOfContest()
        {
            float top = -250f;
            var card = GatheringPanel.ShownCard;
            if (card != null && _sayRect.parent is RectTransform parent)
            {
                var corners = new Vector3[4];
                card.GetWorldCorners(corners);   // bottom left first
                float bottom = parent.InverseTransformPoint(corners[0]).y - parent.rect.yMax;   // from the top edge
                top = Mathf.Min(top, bottom - 24f);
            }
            if (Mathf.Abs(_sayRect.anchoredPosition.y - top) < 0.5f) return;
            _sayRect.anchoredPosition = new Vector2(0f, top);
            _warn.rectTransform.anchoredPosition = new Vector2(0f, top + 10f);
            _boosts.anchoredPosition = new Vector2(0f, top - 186f);
        }

        static string Times(float x) => x.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

        void RefreshBoosts(Fishing f)
        {
            var tags = new List<(string text, Color colour)>();
            float total = 1f;
            var bait = f.Now == Fishing.State.Idle ? Baits.Current : f.BaitOnHook;
            if (bait != null)
            {
                bool quick = bait.BiteSpeed > 1f, gentle = bait.Gentle < 1f;
                string text = quick && gentle ? Loc.F("Bait: bites ×{0}, easier reeling", Times(bait.BiteSpeed))
                    : quick ? Loc.F("Bait: bites ×{0}", Times(bait.BiteSpeed))
                    : "Bait: easier reeling";
                tags.Add((text, UiKit.Hex("8A5A32")));
                total *= bait.BiteSpeed;
            }
            if (f.InCharm)
            {
                tags.Add((Loc.F("Shell: bites ×{0}", Times(MermaidCharms.BiteBoost)), UiKit.Hex("3E9C8F")));
                total *= MermaidCharms.BiteBoost;
            }
            if (Gathering.Boosting(Hobby.Fishing))
            {
                tags.Add((Loc.F("Contest: bites ×{0}", Times(1.6f)), UiKit.Hex("E3A92B")));
                total *= 1.6f;
            }
            int quickers = 0;
            if (bait != null && bait.BiteSpeed > 1f) quickers++;
            if (f.InCharm) quickers++;
            if (Gathering.Boosting(Hobby.Fishing)) quickers++;
            if (quickers > 1) tags.Add((Loc.F("All together: bites ×{0}", Times(total)), UiKit.Hex("2B211A")));

            string key = string.Join("|", tags.ConvertAll(t => t.text));
            if (key == _boostsShown) return;
            _boostsShown = key;
            for (int i = _boosts.childCount - 1; i >= 0; i--) Destroy(_boosts.GetChild(i).gameObject);
            foreach (var (text, colour) in tags)
            {
                var c = colour;
                c.a = 0.82f;
                var pill = UiKit.Image(_boosts, "Boost", UiKit.Rounded(26), c);
                pill.raycastTarget = false;
                var ph = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
                ph.padding = new RectOffset(16, 16, 5, 5);
                ph.childControlWidth = ph.childControlHeight = true;
                pill.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                var t = UiKit.Text(pill.transform, "Text", text, 22f, Color.white, true, TextAlignmentOptions.Center);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.overflowMode = TextOverflowModes.Overflow;
            }
            UiKit.ApplyFonts(_boosts);
        }

        void BuildDemo(RectTransform mode)
        {
            _demo = UiKit.Rect("HoldDemo", mode);
            _demo.anchorMin = _demo.anchorMax = new Vector2(0.5f, 0.36f);
            _demo.sizeDelta = Vector2.zero;
            _ringImage = UiKit.Image(_demo, "Ring", IconPainter.Ring, Color.white);
            _ring = _ringImage.rectTransform;
            _ring.sizeDelta = new Vector2(150f, 150f);
            _handImage = UiKit.Image(_demo, "Hand", TutorialPointer.HandSprite, Color.white);
            _handImage.preserveAspect = true;
            _hand = _handImage.rectTransform;
            _hand.pivot = new Vector2(0.48f, 0.06f);   // the fingertip
            _hand.sizeDelta = new Vector2(190f, 190f);
            _demo.gameObject.SetActive(false);
        }

        /// <summary>Which gesture the moment calls for, and how long to wait before showing it.</summary>
        static Gesture Wanted(Fishing f, out float patience)
        {
            patience = 1.2f;
            switch (f.Now)
            {
                case Fishing.State.Fight:
                    if (f.Tension >= Fishing.RedAt && f.Holding) { patience = 0.35f; return Gesture.Release; }
                    if (!f.Holding && f.Tension < Fishing.YellowAt) { patience = 0.8f; return Gesture.Hold; }
                    return Gesture.None;
                default:
                    return Gesture.None;   // waiting for a bite: hands off
            }
        }

        void UpdateDemo(Fishing f)
        {
            var g = Wanted(f, out float patience);
            if (g != _gesture) { _gesture = g; _gestureSince = Time.unscaledTime; }
            float shown = Time.unscaledTime - _gestureSince - patience;
            bool show = g != Gesture.None && shown > 0f && _handImage.sprite != null;
            if (_demo.gameObject.activeSelf != show) _demo.gameObject.SetActive(show);
            if (!show) return;

            const float cycle = 1.8f;
            float p = Mathf.Repeat(shown, cycle) / cycle;
            float lift, press, fill;
            if (g == Gesture.Hold)
            {
                // Come down, press, stay down while the ring fills, then lift and start over.
                lift = p < 0.15f ? 1f - p / 0.15f : p > 0.88f ? (p - 0.88f) / 0.12f : 0f;
                press = p >= 0.15f && p <= 0.88f ? 1f : 0f;
                fill = Mathf.Clamp01((p - 0.15f) / 0.6f);
            }
            else
            {
                // Down and holding… then lift clear away, and the ring lets go with it.
                lift = p < 0.4f ? 0f : Mathf.Clamp01((p - 0.4f) / 0.25f);
                press = p < 0.4f ? 1f : 0f;
                fill = 1f - lift;
            }
            _hand.anchoredPosition = new Vector2(0f, lift * 70f);
            _hand.localScale = Vector3.one * (1f - press * 0.08f);
            _ring.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, fill);
            _ringImage.color = new Color(1f, 1f, 1f, press > 0f ? 0.25f + 0.65f * fill : 0.2f * fill);
            _handImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(shown * 3f));
        }

        /// <summary>The whole screen: finger down is Press, finger up is Release.</summary>
        class HoldRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            bool _down;

            public void OnPointerDown(PointerEventData e)
            {
                _down = true;
                if (Fishing.Instance != null) Fishing.Instance.Press();
            }

            public void OnPointerUp(PointerEventData e) => Up();
            void OnDisable() => Up();

            void Up()
            {
                if (!_down) return;
                _down = false;
                if (Fishing.Instance != null) Fishing.Instance.Release();
            }
        }

        void Enter()
        {
            var f = Fishing.Instance;
            if (f == null) return;
            f.Mode = true;
            Sfx.Play(SfxKind.Tap, 0.5f);
            _sayRect.localScale = Vector3.one * 0.8f;
            _sayRect.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
            f.CastNow();   // straight into the water: no aiming
        }

        void Exit()
        {
            var f = Fishing.Instance;
            if (f == null) return;
            f.Mode = false;
            f.StopFishing();
        }

        void OnEnable()
        {
            Fishing.Bitten += Flash;
            Fishing.Said += Hints.Show;
            HobbyLog.LevelledUp += OnLevel;
        }

        void OnDisable()
        {
            Fishing.Bitten -= Flash;
            Fishing.Said -= Hints.Show;
            HobbyLog.LevelledUp -= OnLevel;
        }

        static void OnLevel(Hobby h, int level)
        {
            string name = h switch
            {
                Hobby.Fishing => "Fishing", Hobby.Birding => "Birdwatching", Hobby.Writing => "Writing", Hobby.Sewing => "Sewing",
                Hobby.Gardening => "Gardening", Hobby.Cooking => "Cooking", Hobby.Bugs => "Bug catching", _ => "Cat care",
            };
            string more = h switch
            {
                Hobby.Fishing => "Rarer fish will start to swim near the shore, and your line holds a little better.",
                Hobby.Birding => "Rarer birds will start to visit.",
                Hobby.Bugs => "Rarer bugs will start to appear.",
                Hobby.Gardening => "New seeds to plant, and room for another plot!",
                Hobby.Cooking => "New recipes in the kitchen.",
                _ => "Keep it up!",
            };
            CardPopup.Show(name + " level " + level + "!", more, HobbyCutscene.Picture(h));
        }

        void Flash()
        {
            if (_tackleOpen) CloseTackle();   // a fish is on: the whole screen is the rod again
            _flashUntil = Time.time + 0.9f;
            _sayRect.DOKill();
            _sayRect.localScale = Vector3.one;
            _sayRect.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.4f, 8, 0.6f).SetUpdate(true);
        }

        void Update()
        {
            var f = Fishing.Instance;
            bool blocked = f == null || UiScreen.AnyOpen || Cutscene.Playing;
            // Leaving the shore (or a menu opening over everything) ends fishing mode.
            if (f != null && f.Mode && (!f.AtShore && f.Now == Fishing.State.Idle)) f.Mode = false;

            bool mode = !blocked && f.Mode;
            // The offer steps aside for anyone talking or a card, rather than sitting over their words.
            bool offer = !blocked && !f.Mode && f.AtShore && f.Now == Fishing.State.Idle && HobbyLog.IsUnlocked(Hobby.Fishing)
                         && !SpeechDialog.IsOpen && !CardPopup.IsShowing && ChatBar.OpenPanel == null;   // nor over the chat
            if (_mode.activeSelf != mode) _mode.SetActive(mode);
            if (_start.activeSelf != offer) _start.SetActive(offer);
            if (!mode) { if (_tackleOpen) CloseTackle(); return; }

            bool fight = f.Now == Fishing.State.Fight;
            UpdateDemo(f);
            RefreshTackle(f);
            RefreshBoosts(f);
            KeepClearOfContest();
            _progressTrack.gameObject.SetActive(fight);
            _warn.enabled = fight && f.Overload > 0.01f;
            if (_warn.enabled)
            {
                var c = Fishing.Red;
                c.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * (8f + f.Overload * 14f)));
                _warn.color = c;
            }

            string say, hint = "";
            Color colour = Color.white;
            switch (f.Now)
            {
                case Fishing.State.Waiting: say = "Wait…"; hint = f.InCharm ? StoryText.Charm.Waiting : "A fish is coming to look"; break;
                case Fishing.State.Nibbling: say = "Wait for it…"; hint = "Nibble, nibble… wait for the big splash"; break;
                case Fishing.State.Fight:
                    float t = f.Tension;
                    colour = Fishing.TensionColour(t);
                    if (t >= Fishing.RedAt) { say = f.Overload > 0.01f ? "LET GO!" : "Let go!"; hint = "The line is about to snap"; }
                    else if (t >= Fishing.YellowAt) { say = f.Holding ? "Careful…" : "Hold to reel!"; hint = "Ease off when it turns red"; }
                    else { say = f.Holding ? "Reeling…" : "Hold to reel!"; hint = f.Surging ? "It's pulling hard!" : "Bring it in"; }
                    UiKit.SetProgress(_progress, f.Progress);
                    _progress.color = colour;
                    break;
                default: say = "Tap to cast"; hint = "Your bobber lands by the nearest fish"; break;
            }
            if (Time.time < _flashUntil) { say = "Hooked!"; colour = UiKit.Sun; }
            _say.text = say;
            _say.color = colour;
            _hint.text = hint;
        }
    }
}
