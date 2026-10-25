using System;
using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Before the opening movie: a dark screen, and Old Yuzu's ghost glowing softly in the middle of
    /// it, as if your arrival had woken her. She asks which language you speak (the screen switches
    /// as you choose), offers Easy Reading, and asks your name — so the movie that follows is in
    /// your language and tells your story.
    /// </summary>
    public class AwakeningScreen : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        Action _done;
        float _timeScaleBefore = 1f;
        TMP_InputField _name;
        CanvasGroup _group;

        public static void Show(Action done)
        {
            var go = new GameObject("[Awakening]", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 460;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            var screen = go.AddComponent<AwakeningScreen>();
            screen._done = done;
            screen.Build();
        }

        /// <summary>How long the bubble takes to drift up to you out of the dark.</summary>
        const float Approach = 5f;

        RectTransform _root, _bubble;
        Image _glass, _sleeper, _ghost, _glow, _hand, _dark, _candle;
        TextMeshProUGUI _prompt;
        bool _popped;
        float _born;
        Sprite _soft;

        /// <summary>
        /// Landscape: the screen is too short to stack the bubble over the questions, so once she
        /// wakes the bubble drifts to the left and the questions take the right half.
        /// </summary>
        bool _wide;

        /// <summary>Where the bubble floats, and everything that gathers around it.</summary>
        Vector2 Spot => _spot;
        Vector2 _spot;

        void Build()
        {
            IsOpen = true;
            _wide = Screen.width > Screen.height;
            _spot = _wide ? new Vector2(0.5f, 0.55f) : new Vector2(0.5f, 0.66f);
            _timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
            IntroAudio.Play("intro_awaken", 2.5f);

            _root = UiKit.Rect("Root", transform);
            UiKit.Stretch(_root);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _born = Time.unscaledTime;
            _soft = SoftLight();
            _dark = UiKit.Image(_root, "Dark", null, UiKit.Hex("07060A"), true);
            UiKit.Stretch(_dark.rectTransform);

            // A warm light, as if from a candle somewhere off to the side: it wavers (see Update).
            _candle = UiKit.Image(_root, "Candle", _soft, new Color(1f, 0.74f, 0.42f, 0f));
            _candle.raycastTarget = false;
            var cr = _candle.rectTransform;
            cr.anchorMin = cr.anchorMax = Spot - new Vector2(0f, 0.06f);
            cr.sizeDelta = new Vector2(1900f, 1900f);

            // A faint glow in the dark, where she sleeps; it grows as she drifts closer.
            _glow = UiKit.Image(_root, "Glow", _soft, new Color(0.55f, 0.95f, 0.9f, 0f));
            _glow.raycastTarget = false;
            var gr = _glow.rectTransform;
            gr.anchorMin = gr.anchorMax = Spot;
            gr.sizeDelta = new Vector2(900f, 900f);
            _glow.DOFade(0.22f, 2.4f).SetUpdate(true);

            // Old Yuzu, curled up asleep in a bubble, floating up to you out of the dark.
            _bubble = UiKit.Rect("Bubble", _root);
            _bubble.anchorMin = _bubble.anchorMax = Spot;
            _bubble.sizeDelta = new Vector2(500f, 500f);
            _bubble.localScale = Vector3.one * 0.28f;
            _bubble.DOScale(1f, Approach).SetEase(Ease.OutSine).SetUpdate(true);
            _sleeper = UiKit.Image(_bubble, "Asleep", Resources.Load<Sprite>("Cutscenes/Cutscene_GhostSleep"), new Color(1f, 1f, 1f, 0f));
            _sleeper.preserveAspect = true;
            _sleeper.raycastTarget = false;
            UiKit.Stretch(_sleeper.rectTransform, 95f, 95f, 95f, 95f);
            _glass = UiKit.Image(_bubble, "Glass", BubbleGlass(), new Color(1f, 1f, 1f, 0f), true);
            _glass.preserveAspect = true;
            UiKit.Stretch(_glass.rectTransform);
            var pop = _glass.gameObject.AddComponent<Button>();
            pop.transition = Selectable.Transition.None;
            pop.onClick.AddListener(Pop);
            _sleeper.DOFade(0.95f, 1.8f).SetUpdate(true);
            _glass.DOFade(0.9f, 1.8f).SetUpdate(true);
            _bubble.DOAnchorPosY(22f, 2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
            _sleeper.rectTransform.DOScale(new Vector3(1.03f, 0.97f, 1f), 1.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);   // breathing
            StartCoroutine(Snoring());
            StartCoroutine(Motes());
            StartCoroutine(Invite());
        }

        /// <summary>A round light, bright in the middle and fading softly to nothing.</summary>
        static Sprite SoftLight()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x + 0.5f - n / 2f, y + 0.5f - n / 2f).magnitude / (n / 2f);
                    float a = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Clamp01(d));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// The candle: never still. A slow breath of light and a quicker flutter on top, so the dark
        /// gets a little deeper, then a little warmer, over and over.
        /// </summary>
        void Update()
        {
            KeepNameAboveKeyboard();
            FitPanel();
            if (_candle == null) return;
            float t = Time.unscaledTime;
            float flame = 0.65f * Mathf.PerlinNoise(t * 0.9f, 0.3f) + 0.35f * Mathf.PerlinNoise(t * 4.5f, 7.1f);
            float wake = Mathf.Clamp01((t - _born) / 2.5f);   // lights up gently at the start
            var c = _candle.color;
            c.a = Mathf.Lerp(0.05f, 0.2f, flame) * wake;
            _candle.color = c;
            _candle.rectTransform.localScale = Vector3.one * (0.95f + 0.1f * flame);
            _dark.color = Color.Lerp(UiKit.Hex("040306"), UiKit.Hex("120D0C"), flame);
            // her own glow comes closer with the bubble, and breathes with her
            float near = _bubble != null ? _bubble.localScale.x : 1f;
            _glow.rectTransform.localScale = Vector3.one * near * (1f + 0.06f * Mathf.Sin(t * 2.4f));
        }

        /// <summary>
        /// Glimmers drifting in the dark around the bubble, from the very first moment — mint
        /// sparks of her magic and a few warm motes catching the candlelight.
        /// </summary>
        System.Collections.IEnumerator Motes()
        {
            for (int i = 0; i < 14; i++) Mote(UnityEngine.Random.value);   // already there when the lights come up
            while (true)
            {
                Mote(0f);
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.12f, 0.3f));
            }
        }

        void Mote(float age)
        {
            bool warm = UnityEngine.Random.value < 0.3f;
            var m = UiKit.Image(_root, "Mote", _soft, warm ? new Color(1f, 0.82f, 0.55f, 0f) : new Color(0.7f, 1f, 0.95f, 0f));
            m.raycastTarget = false;
            var r = m.rectTransform;
            r.SetSiblingIndex(_bubble.GetSiblingIndex());   // behind the bubble, the hand and the words
            r.anchorMin = r.anchorMax = Spot;
            float size = UnityEngine.Random.Range(10f, 30f);
            r.sizeDelta = new Vector2(size, size);
            // a ring around the bubble, wider than it, with a few strays further out
            float ang = UnityEngine.Random.value * Mathf.PI * 2f;
            float rad = UnityEngine.Random.Range(170f, UnityEngine.Random.value < 0.25f ? 620f : 380f);
            var from = new Vector2(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad * 0.9f);
            var drift = new Vector2(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(70f, 170f));
            float life = UnityEngine.Random.Range(3f, 5.5f), left = life * (1f - age);
            r.anchoredPosition = from + drift * age;
            float peak = UnityEngine.Random.Range(0.35f, 0.85f);
            var seq = DOTween.Sequence().SetUpdate(true).SetTarget(r);
            seq.Append(m.DOFade(peak, Mathf.Min(0.8f, left * 0.3f)).SetEase(Ease.OutSine));
            seq.AppendInterval(Mathf.Max(0f, left * 0.4f));
            seq.Append(m.DOFade(0f, Mathf.Max(0.3f, left * 0.3f)).SetEase(Ease.InSine));
            seq.Insert(0f, r.DOAnchorPos(from + drift, left).SetEase(Ease.Linear));
            seq.Insert(0f, r.DOScale(UnityEngine.Random.Range(0.6f, 1.3f), left).SetEase(Ease.InOutSine));
            seq.OnComplete(() => { if (r != null) Destroy(r.gameObject); });
        }

        /// <summary>The glass of a real island bubble, so this one matches the ones you pop out there.</summary>
        static Sprite BubbleGlass()
        {
            foreach (var b in FindObjectsByType<Bubble>(FindObjectsSortMode.None))
            {
                var sr = b.transform.Find("Sprite")?.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null) return sr.sprite;
            }
            return IconPainter.Ring;
        }

        System.Collections.IEnumerator Snoring()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            while (!_popped)
            {
                var z = UiKit.Text(_root, "Z", "z", 54f, new Color(0.75f, 0.95f, 0.92f, 0f), true, TextAlignmentOptions.Center);
                z.raycastTarget = false;
                var r = z.rectTransform;
                r.anchorMin = r.anchorMax = Spot;
                r.sizeDelta = new Vector2(80f, 80f);
                r.anchoredPosition = _bubble.anchoredPosition + new Vector2(150f, 150f) * _bubble.localScale.x;
                z.DOFade(0.8f, 0.4f).SetUpdate(true);
                r.DOAnchorPos(r.anchoredPosition + new Vector2(70f, 160f), 1.8f).SetEase(Ease.OutSine).SetUpdate(true);
                r.DOLocalRotate(new Vector3(0f, 0f, 18f), 1.8f).SetUpdate(true);
                z.DOFade(0f, 1f).SetDelay(0.8f).SetUpdate(true).OnComplete(() => { if (r != null) Destroy(r.gameObject); });
                yield return new WaitForSecondsRealtime(1.1f);
            }
        }

        /// <summary>A moment later: a pointing hand on the bubble and a gentle "tap it".</summary>
        System.Collections.IEnumerator Invite()
        {
            yield return new WaitForSecondsRealtime(Approach - 0.8f);   // once she has floated up close
            if (_popped) yield break;
            _prompt = UiKit.Text(_root, "Prompt", "Tap the bubble!", 46f, new Color(0.9f, 0.97f, 0.96f, 0f), true, TextAlignmentOptions.Center);
            _prompt.raycastTarget = false;
            var pr = _prompt.rectTransform;
            pr.anchorMin = pr.anchorMax = _wide ? new Vector2(Spot.x, 0.14f) : new Vector2(0.5f, 0.39f);
            pr.sizeDelta = new Vector2(900f, 100f);
            _prompt.DOFade(1f, 0.6f).SetUpdate(true);
            pr.DOScale(1.06f, 0.9f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);

            var handArt = Game.TutorialPointer.HandSprite;
            if (handArt == null) yield break;
            _hand = UiKit.Image(_root, "Hand", handArt, new Color(1f, 1f, 1f, 0f));
            _hand.preserveAspect = true;
            _hand.raycastTarget = false;
            var hr = _hand.rectTransform;
            hr.anchorMin = hr.anchorMax = Spot;
            // Upside down from the tutorial's "go here" pointer: the hand reaches up from below and
            // its fingertip presses on the bubble, a tap rather than a direction.
            hr.pivot = new Vector2(0.5f, 0f);   // the fingertip, once flipped
            hr.sizeDelta = new Vector2(150f, 150f);
            hr.localScale = new Vector3(1f, -1f, 1f);
            Vector2 rest = new Vector2(40f, -95f), press = new Vector2(40f, -65f);
            hr.anchoredPosition = rest;
            _hand.DOFade(1f, 0.5f).SetUpdate(true);
            // press, press: a little tap gesture, over and over
            var tap = DOTween.Sequence().SetUpdate(true).SetLoops(-1);
            tap.Append(hr.DOAnchorPos(press, 0.3f).SetEase(Ease.OutQuad));
            tap.Join(hr.DOScale(new Vector3(0.9f, -0.9f, 1f), 0.3f));
            tap.Append(hr.DOAnchorPos(rest, 0.35f).SetEase(Ease.InQuad));
            tap.Join(hr.DOScale(new Vector3(1f, -1f, 1f), 0.35f));
            tap.AppendInterval(0.5f);
            tap.SetTarget(hr);
        }

        /// <summary>Pop! The bubble bursts, and she wakes with a stretch.</summary>
        void Pop()
        {
            if (_popped) return;
            _popped = true;
            Sfx.Play(SfxKind.Pop, 0.9f);
            IntroAudio.Sfx("Bubble", 0.9f);
            if (_prompt != null) { _prompt.DOKill(); _prompt.rectTransform.DOKill(); _prompt.DOFade(0f, 0.25f).SetUpdate(true); }
            if (_hand != null) { DOTween.Kill(_hand.rectTransform); _hand.DOFade(0f, 0.2f).SetUpdate(true); }

            // The glass swells and vanishes in a spray of droplets.
            _glass.raycastTarget = false;
            _glass.rectTransform.DOScale(1.18f, 0.18f).SetUpdate(true);
            _glass.DOFade(0f, 0.2f).SetUpdate(true);
            for (int i = 0; i < 16; i++)
            {
                var drop = UiKit.Image(_root, "Drop", UiKit.Circle, new Color(0.8f, 0.97f, 1f, 0.9f));
                drop.raycastTarget = false;
                var d = drop.rectTransform;
                d.anchorMin = d.anchorMax = Spot;
                float size = UnityEngine.Random.Range(14f, 30f);
                d.sizeDelta = new Vector2(size, size);
                float a = i / 16f * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
                var start = _bubble.anchoredPosition + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 230f * _bubble.localScale.x;
                d.anchoredPosition = start;
                d.DOAnchorPos(start + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * UnityEngine.Random.Range(90f, 180f), 0.6f).SetEase(Ease.OutQuad).SetUpdate(true);
                drop.DOFade(0f, 0.6f).SetEase(Ease.InQuad).SetUpdate(true).OnComplete(() => { if (d != null) Destroy(d.gameObject); });
            }

            // Awake: the sleeping picture gives way to her, with a stretch and a brighter glow.
            _sleeper.DOKill();
            _sleeper.rectTransform.DOKill();
            _sleeper.DOFade(0f, 0.3f).SetUpdate(true);
            var face = GhostDialogue.Instance != null ? GhostDialogue.Instance.GhostFace : null;
            _ghost = UiKit.Image(_bubble, "Yuzu", face, new Color(1f, 1f, 1f, 0f));
            _ghost.preserveAspect = true;
            _ghost.raycastTarget = false;
            UiKit.Stretch(_ghost.rectTransform, 70f, 60f, 70f, 80f);
            _ghost.DOFade(0.95f, 0.35f).SetUpdate(true);
            var gr = _ghost.rectTransform;
            gr.localScale = new Vector3(1.22f, 0.78f, 1f);
            gr.DOScale(1f, 0.9f).SetEase(Ease.OutElastic).SetUpdate(true);
            _glow.DOKill();
            _glow.DOFade(0.34f, 0.8f).SetUpdate(true);
            IntroAudio.Sfx("Sparkle", 0.6f);

            ShowQuestion();
        }

        /// <summary>What she asks, and where you answer.</summary>
        void ShowQuestion()
        {
            IntroCutscene.Preload();   // the movie loads while you answer
            if (_wide) MakeRoom();
            var panel = UiKit.Rect("Panel", _root);
            _panel = panel;
            // Under the bubble; in landscape beside it, below the "Load a save" corner.
            panel.anchorMin = _wide ? new Vector2(0.52f, 0.06f) : new Vector2(0.06f, 0.06f);
            panel.anchorMax = _wide ? new Vector2(0.95f, 0.86f) : new Vector2(0.94f, 0.5f);
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            panel.pivot = new Vector2(0.5f, 1f);   // FitPanel shrinks it from its top edge
            var v = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 26f;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            var panelGroup = panel.gameObject.AddComponent<CanvasGroup>();
            panelGroup.alpha = 0f;
            panelGroup.DOFade(1f, 1f).SetDelay(0.9f).SetUpdate(true);

            var line = UiKit.Text(panel, "Line", "Mmm... oh! A little capybara. I must have dozed off, it's been so cold for so long. Before I tell you how all this began: which language do you speak? And what shall I call you?",
                40f, new Color(0.9f, 0.97f, 0.96f), false, TextAlignmentOptions.Center);
            line.textWrappingMode = TextWrappingModes.Normal;

            Languages(panel);
            // In Hebrew: a boy or a girl, so everything after (the movie first) speaks to you rightly.
            _address = AddressChoice.Build(panel, 84f, 36f, new Color(0.9f, 0.97f, 0.96f), UiKit.Spring, new Color(1f, 1f, 1f, 0.12f),
                Color.white, new Color(0.88f, 0.95f, 0.94f)).gameObject;
            var easy = EasyReading(panel);
            // Stepped aside while you type your name, so the name box can sit above the keyboard.
            _asideWhileTyping = new[] { line.gameObject, panel.Find("Languages").gameObject, _address, easy };

            _name = UiKit.Input(panel, "Name", "Your name", false, 110f);
            _name.characterLimit = PlayerProfile.MaxNameLength;
            _name.pointSize = 44f;
            var current = PlayerPrefs.GetString("CapyIsland.PlayerName", "");
            if (!string.IsNullOrWhiteSpace(current)) _name.text = current;

            // Every way in, right here, so an island isn't lost with its phone: Google (where it
            // works), a new account, or one you already have. Each opens the account window on that
            // step (AccountScreen.ShowWelcome…); going on as a guest is under them, with its warning.
            if (GoogleLogin.Available || Application.isEditor)
            {
                var google = UiKit.Button(panel, "Google", "Continue with Google", UiKit.ButtonStyle.Secondary, () => SignUp(Way.Google), 38f);
                google.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 104f;
            }
            var ways = UiKit.Rect("Accounts", panel);
            var wh = ways.gameObject.AddComponent<HorizontalLayoutGroup>();
            wh.spacing = 18f;
            wh.childControlWidth = wh.childControlHeight = true;
            wh.childForceExpandWidth = true;
            ways.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            UiKit.Button(ways, "SignUp", "Sign up", UiKit.ButtonStyle.Primary, () => SignUp(Way.SignUp), 36f);
            UiKit.Button(ways, "SignIn", "Sign in", UiKit.ButtonStyle.Secondary, () => SignUp(Way.SignIn), 36f);
            var risk = UiKit.Text(panel, "Risk", "Without an account, your island could be lost forever.", 28f, new Color(1f, 0.78f, 0.7f), false, TextAlignmentOptions.Center);
            risk.overflowMode = TextOverflowModes.Overflow;
            // The same way on as the sign-up window offers, right here, for those who'd rather not.
            var guest = UiKit.Button(panel, "Guest", "Continue as guest", UiKit.ButtonStyle.Ghost, Continue, 30f);
            guest.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;
            foreach (var t in guest.GetComponentsInChildren<TextMeshProUGUI>()) t.color = new Color(0.88f, 0.95f, 0.94f);
            // Played before (another device, a cleared browser)? Sign in brings that island back, and a
            // saved island code loads from the same window (AccountScreen), so there's one way back, not two.
        }

        /// <summary>Landscape: she drifts over to the left, making room for the questions beside her.</summary>
        void MakeRoom()
        {
            Vector2 from = _spot, to = new Vector2(0.27f, 0.55f);
            DOVirtual.Float(0f, 1f, 0.9f, t =>
            {
                _spot = Vector2.Lerp(from, to, t);
                _bubble.anchorMin = _bubble.anchorMax = _spot;
                _glow.rectTransform.anchorMin = _glow.rectTransform.anchorMax = _spot;
                _candle.rectTransform.anchorMin = _candle.rectTransform.anchorMax = _spot - new Vector2(0f, 0.06f);
            }).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this);
        }

        void Languages(RectTransform parent)
        {
            var row = UiKit.Rect("Languages", parent);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 100f;
            var pills = new List<(Language, Image, TextMeshProUGUI)>();
            void Paint()
            {
                foreach (var (l, img, label) in pills)
                {
                    bool on = Loc.Current == l;
                    img.color = on ? UiKit.Spring : new Color(1f, 1f, 1f, 0.12f);
                    label.color = on ? Color.white : new Color(0.88f, 0.95f, 0.94f);
                }
            }
            foreach (Language l in Enum.GetValues(typeof(Language)))
            {
                var language = l;
                var pill = UiKit.Image(row, l.ToString(), UiKit.Rounded(30), Color.white, true);
                var t = UiKit.Text(pill.transform, "Label", Loc.NativeName(l), 42f, Color.white, true, TextAlignmentOptions.Center);
                UiKit.Stretch(t.rectTransform);
                t.GetComponent<LocText>().NoScale = true;
                var b = pill.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { Sfx.Play(SfxKind.Tap, 0.5f); Loc.Set(language); Paint(); });
                pill.gameObject.AddComponent<PressSquash>();
                pills.Add((l, pill, t));
            }
            Paint();
        }

        /// <summary>The Easy Reading switch; returns its row in the panel.</summary>
        GameObject EasyReading(RectTransform parent)
        {
            var easy = UiKit.Button(parent, "Easy", "Easy Reading: Off", UiKit.ButtonStyle.Secondary, null, 36f, IconPainter.Magnifier);
            easy.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 100f;
            var label = easy.GetComponentInChildren<TextMeshProUGUI>();
            var face = easy.GetComponent<Image>();
            void Paint()
            {
                bool on = Accessibility.Enabled;
                if (label != null) { label.text = on ? "Easy Reading: On" : "Easy Reading: Off"; label.color = on ? Color.white : UiKit.Ink; }
                face.color = on ? UiKit.Leaf : UiKit.SurfaceSunk;
            }
            easy.onClick.AddListener(() =>
            {
                Sfx.Play(SfxKind.Tap, 0.5f);
                Accessibility.Enabled = !Accessibility.Enabled;
                Paint();
            });
            Paint();
            return easy.transform.parent.gameObject;
        }

        RectTransform _panel;
        GameObject _address;
        GameObject[] _asideWhileTyping;
        bool _typing;

        /// <summary>
        /// On a phone the keyboard rises over the bottom half of the screen, right where the name
        /// box is. While you type, the questions above it step aside and the box moves up to sit
        /// just above the keyboard; done typing, everything goes back.
        /// </summary>
        void KeepNameAboveKeyboard()
        {
            if (_name == null || _panel == null) return;
            bool typing = _name.isFocused && (Application.isMobilePlatform || TouchScreenKeyboard.visible);
            if (typing == _typing) return;
            _typing = typing;
            // (The boy-or-girl row only ever comes back in Hebrew.)
            foreach (var g in _asideWhileTyping) if (g != null) g.SetActive(!typing && (g != _address || Loc.Current == Language.Hebrew));
            float shift = 0f;
            if (typing)
            {
                float height = _root.rect.height;
                float keyboard = TouchScreenKeyboard.area.height > 1f ? TouchScreenKeyboard.area.height : Screen.height * 0.45f;
                keyboard = keyboard / Mathf.Max(1f, Screen.height) * height;
                // With the name box now first in the panel, lift the panel's top far enough that
                // the box and the Continue button under it both clear the keyboard.
                float top = height * _panel.anchorMax.y;
                shift = Mathf.Max(0f, keyboard + 290f - top);
            }
            _panel.offsetMin = new Vector2(0f, shift);
            _panel.offsetMax = new Vector2(0f, shift);
        }

        enum Way { Google, SignUp, SignIn }

        /// <summary>Your name kept, then the account window on the step you chose; it goes on with the opening either way.</summary>
        void SignUp(Way way)
        {
            PlayerProfile.Name = _name != null ? _name.text : "";
            if (CloudSync.Linked) { Continue(); return; }
            Action done = () => { if (this != null) Continue(); };
            if (way == Way.Google) AccountScreen.ShowWelcomeGoogle(done);
            else AccountScreen.ShowWelcome(done, way == Way.SignIn);
        }

        /// <summary>
        /// The panel's rows (the question, languages, Easy Reading, your name, every way in, the
        /// guest line) can need more height than a short or sideways screen gives it, more so with
        /// Easy Reading's bigger words: then it shrinks from its top edge instead of running off the bottom.
        /// </summary>
        void FitPanel()
        {
            if (_panel == null || _typing) return;
            // The room its anchors give it, and the height its rows want.
            float room = ((RectTransform)_panel.parent).rect.height * (_panel.anchorMax.y - _panel.anchorMin.y);
            float need = LayoutUtility.GetPreferredHeight(_panel);
            float scale = need > room && need > 0f ? room / need : 1f;
            // Laid out at its full height, so no row is squeezed (the question was being cut short),
            // then shrunk as a whole from its top edge to fit the room.
            float extra = scale < 1f ? need - room : 0f;
            if (Mathf.Abs(_panel.offsetMax.y - _panel.offsetMin.y - extra) > 0.5f)
                _panel.offsetMin = new Vector2(_panel.offsetMin.x, _panel.offsetMax.y - extra);
            if (Mathf.Abs(_panel.localScale.x - scale) > 0.001f) _panel.localScale = new Vector3(scale, scale, 1f);
        }

        void Continue()
        {
            Sfx.Play(SfxKind.Tap, 0.6f);
            PlayerProfile.Name = _name != null ? _name.text : "";
            // Language and Easy Reading are chosen: the welcome page that asks again isn't needed.
            PlayerPrefs.SetInt("CapyIsland.Tip.welcome", 1);
            PlayerPrefs.Save();
            _group.blocksRaycasts = false;
            _group.DOFade(0f, 0.8f).SetUpdate(true).OnComplete(() =>
            {
                IsOpen = false;
                Time.timeScale = _timeScaleBefore <= 0f ? 1f : _timeScaleBefore;
                var done = _done;
                Destroy(gameObject);
                done?.Invoke();
            });
        }

        void OnDestroy()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true)) DOTween.Kill(t);
            foreach (var g in GetComponentsInChildren<Graphic>(true)) DOTween.Kill(g);
            if (_soft != null) Destroy(_soft.texture);
            if (!IsOpen) return;
            IsOpen = false;
            Time.timeScale = _timeScaleBefore <= 0f ? 1f : _timeScaleBefore;
        }
    }
}
