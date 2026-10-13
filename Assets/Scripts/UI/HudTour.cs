using System;
using System.Collections;
using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// A guided look around the screen: everything goes dark except the one thing being talked
    /// about, which sits in a bright frame, and a card underneath says what it is for. Back and
    /// Next move through the pages; "End tutorial" switches every tip (and Old Yuzu's step-by-step
    /// lines) off for good — Settings can switch them back, and replay the whole tour.
    ///
    /// The game teaches by doing, so most of these never come by themselves: a page turns up only
    /// when the thing it explains first matters to you (see <see cref="TipTriggers"/>) — the menu
    /// after your first outfit, Friends when the first visitor lands — and only once the opening is
    /// over and <see cref="Attention"/> says the screen is free. Each tip shows once. Settings →
    /// Show me around plays every page in a row.
    ///
    /// Never while visiting someone else's island.
    /// </summary>
    public class HudTour : MonoBehaviour
    {
        const string SeenKey = "CapyIsland.TourSeen";        // the old all-in-one tour: seen it, seen every tip
        const string TipKey = "CapyIsland.Tip.";

        /// <summary>Has this page been shown (as a tip or in a tour)?</summary>
        public static bool Seen(string id) => PlayerPrefs.GetInt(SeenKey, 0) == 1 || PlayerPrefs.GetInt(TipKey + id, 0) == 1;

        /// <summary>Tips the screen they're about tells, over itself, when you open it (see <see cref="MenuScreen"/>).</summary>
        static bool ToldByItsScreen(string id) => id == "menu";

        /// <summary>Is this page waiting its turn as a tip (and not yet seen)?</summary>
        public static bool Wants(string id)
        {
            if (Seen(id)) return false;
            foreach (var tip in _tips) if (System.Array.IndexOf(tip, id) >= 0) return true;
            return false;
        }

        /// <summary>Takes a waiting page out of the line, as seen: it was explained another way.</summary>
        public static void Withdraw(string id)
        {
            for (int i = _tips.Count - 1; i >= 0; i--)
            {
                if (System.Array.IndexOf(_tips[i], id) < 0) continue;
                var rest = new List<string>(_tips[i]);
                rest.Remove(id);
                if (rest.Count == 0) _tips.RemoveAt(i);
                else _tips[i] = rest.ToArray();
            }
            MarkSeen(id);
        }

        /// <summary>Counts a page as seen when its subject was explained some other way (a card on first use).</summary>
        public static void MarkSeen(string id)
        {
            PlayerPrefs.SetInt(TipKey + id, 1);
            PlayerPrefs.Save();
        }

        static readonly string[] AllIds = { "welcome", "goals", "arrows", "menu", "map", "gifts", "friends", "events", "profile", "clock", "hello", "help", "walk" };

        /// <summary>
        /// Forgets every tip and the tour, and switches the tutorial back on (Start over): a fresh
        /// island gets the welcome page and its tips again.
        /// </summary>
        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            foreach (var id in AllIds) PlayerPrefs.DeleteKey(TipKey + id);
            _tips.Clear();
            Guidance.Off = false;
            IntroCutscene.Forget();   // a fresh island opens with the story
            Game.YuzuHeart.Forget();  // and Old Yuzu frozen again
            PlayerPrefs.Save();
        }

        static readonly List<string[]> _tips = new();

        /// <summary>
        /// Asks for a tip (one or more pages, by id). Pages already seen are dropped; the rest wait
        /// in line until nothing else is on screen (<see cref="Pump"/>).
        /// </summary>
        public static void Tip(params string[] ids)
        {
            if (Guidance.Off) return;
            var fresh = new List<string>();
            foreach (var id in ids) if (!Seen(id) && !Showing(id)) fresh.Add(id);
            if (fresh.Count == 0) return;
            foreach (var queued in _tips) if (queued[0] == fresh[0]) return;
            _tips.Add(fresh.ToArray());
        }

        /// <summary>Nothing else is on screen, so a tip may show.</summary>
        public static bool Quiet =>
            !IsOpen && !UiScreen.AnyOpen && !SpeechDialog.IsOpen && !CardPopup.IsShowing && !PurchaseDialog.IsOpen
            && !Cutscene.Playing && !TextReader.IsOpen && !Visit.Active && !Visit.Travelling
            && !AwakeningScreen.IsOpen && !IntroAudio.Active   // never during the opening, start to finish
            && !YuzuTalking;

        /// <summary>Old Yuzu is mid-sentence: a tip waits for her to finish rather than cut her off.</summary>
        static bool YuzuTalking => GhostDialogue.Instance != null && GhostDialogue.Instance.MidLine;

        /// <summary>Shows the next waiting tip, if the moment is right. Called by TipTriggers.</summary>
        public static void Pump()
        {
            if (_tips.Count == 0 || !Quiet || Guidance.Off) return;
            // The gatekeeper's rules: never during the opening, and a calm moment first.
            if (!Attention.Free(4f) || Time.unscaledTime < _restUntil) return;
            // Some tips are told by their own screen when it opens (a red dot on its button calls
            // you over); the line skips those.
            int at = _tips.FindIndex(t => System.Array.Exists(t, id => !ToldByItsScreen(id)));
            if (at < 0) return;
            var next = System.Array.FindAll(_tips[at], id => !ToldByItsScreen(id));
            var held = System.Array.FindAll(_tips[at], ToldByItsScreen);
            if (held.Length > 0) _tips[at] = held; else _tips.RemoveAt(at);
            // Drop pages that were seen while this tip waited (the full tour, say).
            var fresh = new List<string>();
            foreach (var id in next)
            {
                if (Seen(id)) continue;
                // The arrows page is about an arrow you can see: if the one that queued it has gone,
                // it waits for the next (TipTriggers queues it again whenever one is on screen).
                if (id == "arrows" && EdgeIndicators.FirstShown == null) continue;
                fresh.Add(id);
            }
            if (fresh.Count > 0) Begin(fresh.ToArray());
        }

        /// <summary>A breather after each card, so tips never arrive one on top of another.</summary>
        const float RestBetweenTips = 15f;
        static float _restUntil;

        /// <summary>Is this page on screen right now?</summary>
        static bool Showing(string id) =>
            IsOpen && (_instance._only == null || System.Array.IndexOf(_instance._only, id) >= 0);

        static HudTour _instance;

        public static bool IsOpen => _instance != null && _instance._open;

        class Step
        {
            public string Id;
            public string Title, Body;
            public Func<Rect?> Target;               // screen-space rect to spotlight; null = none
            public Action<RectTransform> Extra;      // extra content under the body
            public bool Optional;                    // skipped when its target is not on screen
        }

        readonly List<Step> _steps = new();
        string[] _only;   // the pages this run shows, or null for all of them
        int _index;
        bool _open;

        RectTransform _canvasRect, _root;
        Image _top, _bottom, _left, _right, _frame;
        RectTransform _panel, _extra;
        Rect _holeScreen;   // the spotlight, in screen pixels
        Image _face;
        TextMeshProUGUI _title, _body, _counter;
        Button _back, _next, _skip;
        RectTransform _demoArrow;

        /// <summary>Plays pages by id, or every page with none given (Settings → Show me around).</summary>
        public static void Begin(params string[] ids)
        {
            if (_instance == null)
            {
                var go = new GameObject("[HudTour]");
                _instance = go.AddComponent<HudTour>();
            }
            _instance._only = ids != null && ids.Length > 0 ? ids : null;
            _instance.Open();
        }

        /// <summary>Called once the HUD is built: the two first-launch pages, if never seen.</summary>
        public static void BeginIfFirstTime(MonoBehaviour host)
        {
            if (Guidance.Off || Seen("welcome") || Visit.Active || Visit.Travelling) return;
            host.StartCoroutine(WaitThenBegin());
        }

        static IEnumerator WaitThenBegin()
        {
            // Let the island settle and Old Yuzu say hello, then wait for anything else on screen.
            yield return new WaitForSecondsRealtime(0.8f);
            while (UiScreen.AnyOpen || SpeechDialog.IsOpen || CardPopup.IsShowing || PurchaseDialog.IsOpen || Cutscene.Playing || AwakeningScreen.IsOpen || IntroAudio.Active || TextReader.IsOpen
                   || YuzuTalking)   // and let her finish her hello
                yield return new WaitForSecondsRealtime(0.5f);
            var first = new List<string>();
            foreach (var id in new[] { "welcome" }) if (!Seen(id)) first.Add(id);
            if (first.Count == 0) yield break;
            Begin(first.ToArray());
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ---------------------------------------------------------------- flow

        void Open()
        {
            Build();
            BuildSteps();
            if (_only != null) _steps.RemoveAll(st => System.Array.IndexOf(_only, st.Id) < 0);
            _open = true;
            _root.gameObject.SetActive(true);
            var ghost = GhostDialogue.Instance;
            if (_face != null)
            {
                _face.sprite = ghost != null ? ghost.Face : null;
                _face.transform.parent.gameObject.SetActive(_face.sprite != null);
            }
            _index = -1;
            Go(+1);
        }

        void Finish()
        {
            _open = false;
            if (GhostDialogue.Instance != null) GhostDialogue.Instance.Hidden = false;   // back to her corner
            // Every page actually shown counts as seen, so its tip never comes back. A page skipped
            // because its subject wasn't on screen stays unseen, to turn up as a tip when it is.
            foreach (var id in _shown) PlayerPrefs.SetInt(TipKey + id, 1);
            _shown.Clear();
            PlayerPrefs.Save();
            _restUntil = Time.unscaledTime + RestBetweenTips;
            if (_root != null) _root.gameObject.SetActive(false);
            Sfx.Play(SfxKind.Tap, 0.4f);
        }

        /// <summary>
        /// "End tutorial": no more tips, no more step-by-step lines from Old Yuzu, no pointing hand.
        /// The opening's story still plays out behind the scenes.
        /// </summary>
        void EndTutorial()
        {
            Guidance.Off = true;
            _tips.Clear();
            var ghost = FindFirstObjectByType<GhostDialogue>();
            if (ghost != null) ghost.Clear();
            Finish();
        }

        /// <summary>Moves to the next (or previous) page whose target can be shown.</summary>
        void Go(int dir)
        {
            int i = _index;
            while (true)
            {
                i += dir;
                if (i < 0) return;
                if (i >= _steps.Count) { Finish(); return; }
                var s = _steps[i];
                if (!s.Optional || s.Target == null || s.Target() != null) break;
            }
            _index = i;
            Show(_steps[i]);
            Sfx.Play(SfxKind.Tap, 0.4f);
        }

        Canvas _canvas;

        void Update()
        {
            if (!_open) return;
            // A movie that starts while a card is up plays on its own: the card steps aside until it ends.
            if (_canvas != null) _canvas.enabled = !Cutscene.Playing;
            if (Cutscene.Playing) return;
            if (Input.GetKeyDown(KeyCode.Escape) && !TextReader.IsOpen) { Finish(); return; }
            // Pressed the highlighted thing itself: the tip has done its job; the press goes through.
            if (_holeScreen.width > 0f && Input.GetMouseButtonDown(0) && _holeScreen.Contains(Input.mousePosition)) { Finish(); return; }
            if (Time.frameCount % 15 == 0) Footer();
            Layout();
        }

        // ---------------------------------------------------------------- the pages

        void BuildSteps()
        {
            _steps.Clear();
            var hud = MobileHud.Instance != null ? MobileHud.Instance.transform : null;
            Transform Find(string path) => hud != null ? hud.Find("SafeArea/" + path) ?? hud.Find(path) : null;
            Func<Rect?> Hud(string path) => () => ScreenRect(Find(path));

            _steps.Add(new Step
            {
                Id = "welcome",
                Title = "Welcome to Capy Island!",
                Body = "Before we start, pick your language. If the words are hard to read, turn on Easy Reading for bigger text.",
                Extra = BuildWelcomeChoices,
            });
            _steps.Add(new Step
            {
                Id = "goals",
                Title = "What to do next",
                Body = "Your next goal is always here. Tap it to see every goal and quest.",
                Target = Hud("ObjectiveTracker"), Optional = true,
            });
            _steps.Add(new Step
            {
                Id = "arrows",
                Title = "Arrows at the edge",
                Body = "An arrow at the edge of the screen points at something you can't see yet. The picture inside shows what it is. Tap the arrow and your capybara walks there.",
                // Only ever about a real arrow on screen: in the full tour too, it is skipped until one shows.
                Target = () => ScreenRect(EdgeIndicators.FirstShown),
                Optional = true,
                Extra = BuildArrowLegend,
            });
            _steps.Add(new Step
            {
                Id = "menu",
                Title = "Menu",
                Body = "Everything else lives here: your goals, the wardrobe, hobbies, friends, and Settings (bigger text, language, sound).",
                Target = Hud("SideButtons/Menu"), Optional = true,
            });
            _steps.Add(new Step
            {
                Id = "map",
                Title = "Map",
                Body = "See all the islands. Drag yourself on the map to travel.",
                Target = Hud("SideButtons/Map"), Optional = true,
            });
            _steps.Add(new Step
            {
                Id = "gifts",
                Title = "Daily gifts",
                Body = "A present every day you play. A red dot means one is waiting.",
                Target = Hud("SideButtons/Gifts"), Optional = true,
            });
            _steps.Add(new Step
            {
                Id = "friends",
                Title = "Friends",
                Body = "Fly to your friends' islands and invite them to visit yours.",
                Target = Hud("SideButtons/Friends"), Optional = true,
            });
            _steps.Add(new Step
            {
                Id = "events",
                Title = "Events",
                Body = "A contest every three days (most fish, bugs or birds wins prizes) and a new goal each day.",
                Target = Hud("SideButtons/Events"), Optional = true,
            });
            _steps.Add(new Step
            {
                Id = "profile",
                Title = "Your profile",
                Body = "Your name, your island's score and your hobbies.",
                Target = Hud("ProfileBadge"), Optional = true,
            });
            _steps.Add(new Step
            {
                Id = "clock",
                Title = "Day and night",
                Body = "A day on the island lasts 24 minutes. Some fish, birds and bugs only come out at certain times, so look again later!",
                Target = Hud("Clock"), Optional = true,
            });
            _steps.Add(new Step
            {
                Id = "hello",
                Title = "Say hello",
                Body = "Send a wave, a heart or a smile to anyone visiting.",
                Target = Hud("EmojiBar"), Optional = true,
            });
            _steps.Add(new Step
            {
                Id = "help",
                Title = "Need help later?",
                Body = "Bigger text, tap-to-read and the language are in Menu → Settings. You can watch this tour again there too.",
                Target = Hud("SideButtons/Menu"), Optional = true,
            });
        }

        static Rect? ScreenRect(Transform t)
        {
            if (t == null || !t.gameObject.activeInHierarchy) return null;
            var rt = t as RectTransform;
            if (rt == null) return null;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var canvas = rt.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            var r = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            if (r.width < 2f || r.height < 2f) return null;
            return r;
        }

        // ---------------------------------------------------------------- extras

        void BuildWelcomeChoices(RectTransform parent)
        {
            var row = UiKit.Rect("Languages", parent);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            var pills = new List<(Language, Image, TextMeshProUGUI)>();
            void Paint()
            {
                foreach (var (l, img, label) in pills)
                {
                    bool on = Loc.Current == l;
                    img.color = on ? UiKit.Spring : UiKit.SurfaceSunk;
                    label.color = on ? Color.white : UiKit.Ink;
                }
            }
            foreach (Language l in Enum.GetValues(typeof(Language)))
            {
                var language = l;
                var pill = UiKit.Image(row, l.ToString(), UiKit.Rounded(30), UiKit.SurfaceSunk, true);
                var t = UiKit.Text(pill.transform, "Label", Loc.NativeName(l), 40f, UiKit.Ink, true, TextAlignmentOptions.Center);
                UiKit.Stretch(t.rectTransform);
                t.GetComponent<LocText>().NoScale = true;
                var b = pill.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { Sfx.Play(SfxKind.Tap, 0.5f); Loc.Set(language); Paint(); });
                pill.gameObject.AddComponent<PressSquash>();
                pills.Add((l, pill, t));
            }
            Paint();

            var easy = UiKit.Button(parent, "Easy", "", UiKit.ButtonStyle.Secondary, null, 38f, IconPainter.Magnifier);
            var holder = easy.transform.parent.gameObject;
            holder.AddComponent<LayoutElement>().preferredHeight = 104f;
            var label = easy.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null)
            {
                label = UiKit.Text(easy.transform.Find("Content"), "Label", "", 38f, UiKit.Ink, true, TextAlignmentOptions.Center);
                label.textWrappingMode = TextWrappingModes.NoWrap;
            }
            var face = easy.GetComponent<Image>();
            void PaintEasy()
            {
                bool on = Accessibility.Enabled;
                label.text = on ? "Easy Reading: On" : "Easy Reading: Off";
                face.color = on ? UiKit.Leaf : UiKit.SurfaceSunk;
                label.color = on ? Color.white : UiKit.Ink;
            }
            easy.onClick.AddListener(() =>
            {
                Sfx.Play(SfxKind.Tap, 0.5f);
                Accessibility.Enabled = !Accessibility.Enabled;
                PaintEasy();
            });
            PaintEasy();
        }

        void BuildArrowLegend(RectTransform parent)
        {
            var kinds = new (Color colour, string what)[]
            {
                (UiKit.Primary, "Where Old Yuzu wants you to go"),
                (UiKit.Sun, "Someone with a quest for you"),
                (UiKit.Spring, "A bubble with a present inside"),
                (UiKit.Danger, "Trouble: pests on your island"),
                (UiKit.Hex("5B9BD5"), "A friend visiting"),
            };
            foreach (var (colour, what) in kinds)
            {
                var row = UiKit.Rect("Kind", parent);
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 18f;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = h.childForceExpandHeight = false;
                RtlLayout.Add(h);
                var dot = UiKit.Image(row, "Dot", UiKit.Circle, colour);
                var dle = dot.gameObject.AddComponent<LayoutElement>();
                dle.preferredWidth = dle.preferredHeight = dle.minWidth = 40f;
                var t = UiKit.Text(row, "What", what, 30f, UiKit.Ink);
                t.overflowMode = TextOverflowModes.Overflow;
                t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            }
        }

        // ---------------------------------------------------------------- building

        void Build()
        {
            if (_root != null) return;

            var canvasGo = new GameObject("TourCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            _canvas = canvas;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 29000;   // over everything but the reader, so the tour can be read big too
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            _canvasRect = (RectTransform)canvasGo.transform;

            _root = UiKit.Rect("Root", canvasGo.transform);
            UiKit.Stretch(_root);

            // The dark shades catch taps; the spotlight itself does not, so the thing being shown
            // (the Map button, say) can be pressed right there — which also closes the tip.
            var blocker = UiKit.Image(_root, "Blocker", null, new Color(0f, 0f, 0f, 0f), false);
            UiKit.Stretch(blocker.rectTransform);

            var dim = new Color(ShadeColour.r, ShadeColour.g, ShadeColour.b, ShadeAlpha);
            _top = Shade("Top", dim);
            _bottom = Shade("Bottom", dim);
            _left = Shade("Left", dim);
            _right = Shade("Right", dim);

            _frame = UiKit.Image(_root, "Frame", RoundedOutline(), UiKit.Sun);
            _frame.type = Image.Type.Sliced;
            _frame.rectTransform.anchorMin = _frame.rectTransform.anchorMax = _frame.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            _demoArrow = BuildDemoArrow();

            var card = UiKit.Card(_root, "Panel", UiKit.Surface, 44, shadow: false);
            _panel = card.rectTransform;
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(40, 40, 36, 32);
            v.spacing = 18f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Old Yuzu says the tip: her picture beside the title, so it is her talking
            var header = UiKit.Rect("Header", _panel);
            var hh = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            hh.spacing = 22f;
            hh.childAlignment = TextAnchor.MiddleLeft;
            hh.childControlWidth = hh.childControlHeight = true;
            hh.childForceExpandWidth = false;
            hh.childForceExpandHeight = false;
            RtlLayout.Add(hh);
            var disc = UiKit.Image(header, "YuzuDisc", UiKit.Circle, UiKit.Spring);
            var discLe = disc.gameObject.AddComponent<LayoutElement>();
            discLe.preferredWidth = discLe.minWidth = discLe.preferredHeight = discLe.minHeight = 132f;
            _face = UiKit.Image(disc.rectTransform, "Face", null, Color.white);
            _face.preserveAspect = true;
            UiKit.Stretch(_face.rectTransform);
            _face.rectTransform.offsetMin = new Vector2(8f, 8f);
            _face.rectTransform.offsetMax = new Vector2(-8f, -8f);
            _title = UiKit.Text(header, "Title", "", 50f, UiKit.PrimaryDark, true);
            _title.overflowMode = TextOverflowModes.Overflow;
            _title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            _body = UiKit.Text(_panel, "Body", "", 36f, UiKit.Ink);
            _body.overflowMode = TextOverflowModes.Overflow;
            _body.lineSpacing = 6f;

            _extra = UiKit.Rect("Extra", _panel);
            var ev = _extra.gameObject.AddComponent<VerticalLayoutGroup>();
            ev.spacing = 12f;
            ev.childControlWidth = ev.childControlHeight = true;
            ev.childForceExpandWidth = true;
            ev.childForceExpandHeight = false;

            var footer = UiKit.Rect("Footer", _panel);
            var fh = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            fh.spacing = 14f;
            fh.childAlignment = TextAnchor.MiddleCenter;
            fh.childControlWidth = fh.childControlHeight = true;
            fh.childForceExpandWidth = false;
            fh.childForceExpandHeight = false;
            footer.gameObject.AddComponent<LayoutElement>().preferredHeight = 100f;
            RtlLayout.Add(fh);   // Hebrew reads right to left: "Next" goes on the left

            _skip = UiKit.Button(footer, "Skip", "End tutorial", UiKit.ButtonStyle.Ghost, () => { if (_only == null) Finish(); else EndTutorial(); }, 28f);
            Size(_skip, 190f);
            _counter = UiKit.Text(footer, "Counter", "", 28f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
            _counter.textWrappingMode = TextWrappingModes.NoWrap;
            _counter.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            _back = UiKit.Button(footer, "Back", "Previous", UiKit.ButtonStyle.Secondary, () => Go(-1), 32f);
            Size(_back, 170f);
            _next = UiKit.Button(footer, "Next", "Next", UiKit.ButtonStyle.Primary, () => Go(+1), 36f);
            Size(_next, 230f);

            _root.gameObject.SetActive(false);
        }

        static void Size(Button b, float width)
        {
            var le = b.transform.parent.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = width;
            le.preferredHeight = 96f;
        }

        Image Shade(string name, Color c)
        {
            var img = UiKit.Image(_root, name, null, c, true);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = img.rectTransform.pivot = Vector2.zero;
            return img;
        }

        /// <summary>A copy of an edge arrow, parked on the right edge to talk about.</summary>
        RectTransform BuildDemoArrow()
        {
            var root = UiKit.Rect("ExampleArrow", _root);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(1f, 0.5f);
            root.sizeDelta = new Vector2(96f, 96f);
            var arrow = UiKit.Image(root, "Arrow", IconPainter.Arrow, UiKit.Spring);
            arrow.rectTransform.sizeDelta = new Vector2(44f, 44f);
            arrow.rectTransform.anchoredPosition = new Vector2(54f, 0f);
            var ring = UiKit.Image(root, "Ring", UiKit.Circle, UiKit.Spring);
            UiKit.Stretch(ring.rectTransform);
            var face = UiKit.Image(ring.transform, "Face", UiKit.Circle, Color.white);
            UiKit.Stretch(face.rectTransform, 7f, 7f, 7f, 7f);
            var icon = ItemCatalog.Instance != null ? ItemCatalog.Instance.Icon("Small Daisy Garden") ?? ItemCatalog.Instance.Icon("Small Posie Garden") : null;
            var ic = UiKit.Image(face.transform, "Icon", icon != null ? icon : IconPainter.Gift, icon != null ? Color.white : UiKit.Spring);
            ic.preserveAspect = true;
            UiKit.Stretch(ic.rectTransform, 9f, 9f, 9f, 9f);
            root.gameObject.SetActive(false);
            return root;
        }

        static Sprite _outline;
        static readonly Color ShadeColour = new(0.06f, 0.04f, 0.03f);
        const float ShadeAlpha = 0.74f;

        static Sprite RoundedOutline()
        {
            if (_outline != null) return _outline;
            const int radius = 36, size = radius * 2 + 4;
            const float thickness = 7f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - half) - (half - 1f - radius);
                float qy = Mathf.Abs(y + 0.5f - half) - (half - 1f - radius);
                float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                float ring = Mathf.Abs(d + thickness * 0.5f) - thickness * 0.5f;
                // Outside the rounded line, the same dim as the shades: the spotlight is cut as a
                // square, and this fills its corners so the bright part is exactly the rounded frame.
                float line = Mathf.Clamp01(0.5f - ring), shade = Mathf.Clamp01(d + 0.5f) * ShadeAlpha * (1f - line);
                float a = line + shade;
                var c = a > 0f ? (Color.white * line + (Color)ShadeColour * shade) / a : Color.clear;
                px[y * size + x] = new Color(c.r, c.g, c.b, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            int border = radius + 2;
            _outline = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            return _outline;
        }

        // ---------------------------------------------------------------- showing a page

        readonly HashSet<string> _shown = new();

        void Show(Step s)
        {
            _shown.Add(s.Id);
            _title.text = s.Title;
            // She is in the popup, so her corner is empty.
            if (GhostDialogue.Instance != null) GhostDialogue.Instance.Hidden = true;
            _body.text = s.Body;
            for (int i = _extra.childCount - 1; i >= 0; i--) Destroy(_extra.GetChild(i).gameObject);
            _extra.gameObject.SetActive(s.Extra != null);
            s.Extra?.Invoke(_extra);
            Footer();

            _frame.rectTransform.DOKill();
            _frame.rectTransform.localScale = Vector3.one * 1.25f;
            _frame.rectTransform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);
            UiKit.PopIn(_panel, 0.94f);
            Layout();
        }

        /// <summary>
        /// The page counter and button labels. Kept fresh while the card is open: a page can come
        /// and go with its target (the king spawns a moment after the first page shows).
        /// </summary>
        void Footer()
        {
            if (_index < 0 || _index >= _steps.Count) return;
            var s = _steps[_index];
            int shown = 0, at = 0;
            for (int i = 0; i < _steps.Count; i++)
            {
                var st = _steps[i];
                if (st.Optional && st.Target != null && st.Target() == null) continue;
                shown++;
                if (i <= _index) at = shown;
            }
            _counter.text = shown > 1 ? at + " / " + shown : "";
            _back.transform.parent.gameObject.SetActive(_index > 0);
            // Replaying the whole tour from Settings: a plain Skip. Otherwise the way to stop for good.
            var skipLabel = _skip.GetComponentInChildren<TextMeshProUGUI>();
            if (skipLabel != null) skipLabel.text = _only == null ? "Skip" : "End tutorial";
            var nextLabel = _next.GetComponentInChildren<TextMeshProUGUI>();
            bool tip = _only != null && System.Array.IndexOf(_only, "welcome") < 0;
            if (nextLabel != null) nextLabel.text = at < shown ? (s.Id == "welcome" && _only == null ? "Show me around" : "Next")
                                                   : tip ? "Got it!" : "Let's play!";
            // The example arrow only when there is no real one to point at.
            _demoArrow.gameObject.SetActive(s.Extra == BuildArrowLegend && EdgeIndicators.FirstShown == null);
        }

        /// <summary>Re-cuts the spotlight every frame: targets move (the king walks) and phones rotate.</summary>
        void Layout()
        {
            if (_index < 0 || _index >= _steps.Count || _canvasRect == null) return;
            var area = _canvasRect.rect;
            float w = area.width, h = area.height;

            if (_demoArrow.gameObject.activeSelf)
                _demoArrow.anchoredPosition = new Vector2(-70f, -h * 0.06f);   // below the side buttons

            var target = _steps[_index].Target?.Invoke();
            Rect hole;
            if (target.HasValue)
            {
                var r = target.Value;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, r.min, null, out var a);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, r.max, null, out var b);
                // Local space is centred; shift to bottom-left origin and pad the hole a little.
                const float pad = 16f;
                hole = Rect.MinMaxRect(a.x + w * 0.5f - pad, a.y + h * 0.5f - pad, b.x + w * 0.5f + pad, b.y + h * 0.5f + pad);
                hole.xMin = Mathf.Clamp(hole.xMin, 0f, w); hole.xMax = Mathf.Clamp(hole.xMax, 0f, w);
                hole.yMin = Mathf.Clamp(hole.yMin, 0f, h); hole.yMax = Mathf.Clamp(hole.yMax, 0f, h);
            }
            else hole = new Rect(w * 0.5f, h * 0.5f, 0f, 0f);
            _holeScreen = target.HasValue ? target.Value : Rect.zero;

            SetRect(_bottom, 0f, 0f, w, hole.yMin);
            SetRect(_top, 0f, hole.yMax, w, h - hole.yMax);
            SetRect(_left, 0f, hole.yMin, hole.xMin, hole.height);
            SetRect(_right, hole.xMax, hole.yMin, w - hole.xMax, hole.height);

            _frame.enabled = target.HasValue;
            if (target.HasValue)
            {
                _frame.rectTransform.anchoredPosition = new Vector2(hole.center.x - w * 0.5f, hole.center.y - h * 0.5f);
                _frame.rectTransform.sizeDelta = hole.size;   // exactly over the cut-out, its corners shaded
            }

            // The card sits on the far side of the screen from the spotlight.
            float pw = Mathf.Min(960f, w - 48f);
            bool below = !target.HasValue || hole.center.y > h * 0.5f;
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, below ? 0f : 1f);
            if (!target.HasValue) _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = _panel.anchorMin;
            float inset = SafeInset(below);
            _panel.anchoredPosition = !target.HasValue ? Vector2.zero : new Vector2(0f, below ? 40f + inset : -40f - inset);
            _panel.sizeDelta = new Vector2(pw, _panel.sizeDelta.y);
        }

        float SafeInset(bool bottom)
        {
            var safe = Screen.safeArea;
            float px = bottom ? safe.yMin : Screen.height - safe.yMax;
            return px / Mathf.Max(1f, Screen.height) * _canvasRect.rect.height;
        }

        static void SetRect(Image img, float x, float y, float w, float h)
        {
            var r = img.rectTransform;
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(Mathf.Max(0f, w), Mathf.Max(0f, h));
        }
    }
}
