using System;
using System.Collections;
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
    /// A little movie, like the short animations the original played for its big moments. The game
    /// pauses, cinema bars close in around a painted stage, a few actors hop about on it, and a tap
    /// anywhere skips to the end.
    ///
    /// Positions on the stage are fractions: (0,0) bottom-left, (1,1) top-right. Actors stand on
    /// their feet at the point you give, and are sized as a fraction of the stage's height.
    /// Subclasses write the script in <see cref="Script"/>, waiting with <see cref="Wait"/>.
    /// </summary>
    public abstract class Cutscene : MonoBehaviour, IPointerClickHandler
    {
        static Cutscene _running;
        public static bool Playing => _running != null;

        protected RectTransform Stage { get; private set; }
        RectTransform _root, _barTop, _barBottom;
        Image _flash;
        Action _done;
        bool _skipped, _ending;
        float _timeScaleBefore = 1f;
        readonly List<Tween> _tweens = new();

        /// <summary>Starts a cutscene of type <typeparamref name="T"/>; <paramref name="done"/> runs after it fades out.</summary>
        protected static T Play<T>(Action done) where T : Cutscene
        {
            if (_running != null) { done?.Invoke(); return null; }
            var go = new GameObject("[Cutscene] " + typeof(T).Name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 450;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            var scene = go.AddComponent<T>();
            _running = scene;
            scene._done = done;
            scene.Build();
            scene.StartCoroutine(scene.Run());
            return scene;
        }

        // ---------------------------------------------------------------- the frame

        void Build()
        {
            _timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;

            _root = UiKit.Rect("Root", transform);
            UiKit.Stretch(_root);
            var dark = UiKit.Image(_root, "Dark", null, InBubbles ? UiKit.Hex("07060A") : UiKit.Hex("1E1712"), true);   // catches the skip tap
            UiKit.Stretch(dark.rectTransform);

            if (InBubbles) BuildBubble();
            else
            {
                // The stage keeps a 3:2 picture and fits inside the screen, whichever way the phone is held.
                Stage = UiKit.Rect("Stage", _root);
                Stage.anchorMin = Stage.anchorMax = Stage.pivot = new Vector2(0.5f, 0.5f);
                Stage.gameObject.AddComponent<RectMask2D>();
            }
            // Narration sits on its own layer, so nothing on stage (a capy leaping into the water,
            // a spray of bubbles) ever passes in front of the words.
            _captions = UiKit.Rect("Captions", _root);
            _captions.anchorMin = _captions.anchorMax = _captions.pivot = new Vector2(0.5f, 0.5f);
            Fit();
            Core.ScreenMetrics.Changed += Fit;

            if (!InBubbles)
            {
                _barTop = Bar("BarTop", 1f);
                _barBottom = Bar("BarBottom", 0f);
                _flash = UiKit.Image(_root, "Flash", null, Edge);
                UiKit.Stretch(_flash.rectTransform);
                _flash.raycastTarget = false;
            }

            // A movie you read at your own pace has a tap meaning "next", so skipping is its own button.
            var hint = UiKit.Text(_root, "Skip", ReaderPaced ? "Skip" : "tap to skip", ReaderPaced ? 30f : 26f,
                new Color(1f, 1f, 1f, ReaderPaced ? 0.7f : 0.55f), ReaderPaced, TextAlignmentOptions.BottomRight);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = new Vector2(1f, 0f);
            hint.rectTransform.anchoredPosition = new Vector2(-28f, 24f);
            hint.rectTransform.sizeDelta = new Vector2(300f, 48f);
            hint.raycastTarget = ReaderPaced;
            if (ReaderPaced)
            {
                var skip = hint.gameObject.AddComponent<Button>();
                skip.transition = Selectable.Transition.None;
                skip.onClick.AddListener(Skip);
            }
        }

        /// <summary>
        /// True for a movie that waits for you to read each line (<see cref="Next"/>): a tap anywhere
        /// moves on instead of skipping, and a Skip button in the corner ends it.
        /// </summary>
        protected virtual bool ReaderPaced => false;

        bool _waiting, _advance;

        /// <summary>
        /// Holds the story until the line on screen has been read: a Next button bobs beside the
        /// caption, and a tap on it (or anywhere on the picture) carries on. The scene keeps living
        /// meanwhile — steam, snow and bobbing go on.
        /// </summary>
        protected IEnumerator Next()
        {
            if (_ending) yield break;
            float h = Stage.rect.height;
            var button = UiKit.Button(_captions, "Next", "Next", UiKit.ButtonStyle.Primary, () => _advance = true, 34f);
            var r = (RectTransform)button.transform.parent;   // the Button sits on the face; move the whole thing
            r.sizeDelta = new Vector2(230f, 88f);
            if (InBubbles)
            {
                // under the words, in the middle, where a thumb finds it
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0f);
                r.anchoredPosition = Vector2.zero;
            }
            else
            {
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 0f);
                r.anchoredPosition = new Vector2(-h * 0.04f, h * 0.17f + 14f);
            }
            var group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            Track(group.DOFade(1f, 0.35f).SetUpdate(true));
            Track(r.DOAnchorPosY(r.anchoredPosition.y + 10f, 0.7f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true));
            _advance = false;
            _waiting = true;
            while (!_advance && !_ending) yield return null;
            _waiting = false;
            if (r != null) { DOTween.Kill(r); DOTween.Kill(group); Destroy(r.gameObject); }
            if (!_ending) Sfx.Play(SfxKind.Tap, 0.5f);
        }

        void Skip()
        {
            if (_skipped || _ending) return;
            _skipped = true;
            StopAllCoroutines();
            StartCoroutine(End());
        }

        Vector2 _fittedTo;

        void LateUpdate()
        {
            // Rotating the phone mid-scene: keep the picture fitted.
            var size = ((RectTransform)transform).rect.size;
            if ((size - _fittedTo).sqrMagnitude > 1f) Fit();
        }

        void Fit()
        {
            var size = ((RectTransform)transform).rect.size;
            _fittedTo = size;
            if (size.x <= 0f) size = new Vector2(1080f, 1920f);
            if (InBubbles) { FitBubble(size); return; }
            float w = size.x, h = w / 1.5f;
            if (h > size.y * 0.86f) { h = size.y * 0.86f; w = h * 1.5f; }
            Stage.sizeDelta = new Vector2(w, h);
            _captions.sizeDelta = Stage.sizeDelta;
        }

        // ---------------------------------------------------------------- memories in bubbles

        /// <summary>
        /// True for a movie told as memories in bubbles: each scene plays inside a round bubble of
        /// glass floating in the dark; a change of scene pops it, and the next drifts in from far
        /// away. The words sit beside it, under it in portrait and to its right in landscape.
        /// </summary>
        protected virtual bool InBubbles => true;

        /// <summary>
        /// Where a movie's bubble looks when it opens: the centre (x, y, in stage fractions) and the
        /// zoom (z). A script can move it as it goes with <see cref="Aim"/>.
        /// </summary>
        protected virtual Vector3 BubbleView => new(0.5f, 0.5f, 1f);

        /// <summary>The square the bubble fills; stays put while the bubble pops and floats (for a narrator beside it).</summary>
        protected RectTransform Frame => _holder;

        RectTransform _holder, _bubble;
        CanvasGroup _bubbleGroup;
        float _aimX = 0.5f, _aimY = 0.5f, _zoom = 1f;
        static Sprite _glassArt, _softArt;

        void BuildBubble()
        {
            _holder = UiKit.Rect("Memory", _root);
            _holder.anchorMin = _holder.anchorMax = _holder.pivot = new Vector2(0.5f, 0.5f);
            _bubble = UiKit.Rect("Bubble", _holder);
            UiKit.Stretch(_bubble);
            _bubbleGroup = _bubble.gameObject.AddComponent<CanvasGroup>();
            _bubbleGroup.blocksRaycasts = false;
            // The picture shows through a round window...
            var window = UiKit.Image(_bubble, "Window", UiKit.Circle, Color.white);
            window.raycastTarget = false;
            UiKit.Stretch(window.rectTransform);
            window.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            Stage = UiKit.Rect("Stage", window.rectTransform);
            Stage.anchorMin = Stage.anchorMax = Stage.pivot = new Vector2(0.5f, 0.5f);
            _flash = UiKit.Image(window.rectTransform, "Flash", null, Color.clear);
            UiKit.Stretch(_flash.rectTransform);
            _flash.raycastTarget = false;
            // ...under a skin of glass: a bright rim and a curved highlight.
            var glass = UiKit.Image(_bubble, "Glass", GlassArt(), Color.white);
            glass.raycastTarget = false;
            UiKit.Stretch(glass.rectTransform);
            glass.rectTransform.localScale = Vector3.one * 1.03f;
            // It breathes, the way a bubble wobbles on the air.
            Track(_holder.DOScale(1.012f, 2.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true));
            StartCoroutine(Motes());
        }

        void FitBubble(Vector2 size)
        {
            const float gap = 40f;
            float d;
            Vector2 bubbleAt, wordsAt, words;
            if (size.y >= size.x)
            {
                // Portrait: bubble above, words and Next under it.
                d = Mathf.Min(size.x * 0.92f, (size.y * 0.9f - gap) / 1.34f);
                words = new Vector2(Mathf.Min(size.x * 0.9f, d * 1.05f), d * 0.34f);
                float total = d + gap + words.y;
                bubbleAt = new Vector2(0f, total / 2f - d / 2f);
                wordsAt = new Vector2(0f, total / 2f - d - gap - words.y / 2f);
            }
            else
            {
                // Landscape: bubble on the left, words and Next on the right.
                d = Mathf.Min(size.y * 0.86f, size.x * 0.55f);
                words = new Vector2(Mathf.Min(size.x - d - gap * 3f, d * 1.1f), d * 0.7f);
                float total = d + gap + words.x;
                bubbleAt = new Vector2(-total / 2f + d / 2f, 0f);
                wordsAt = new Vector2(total / 2f - words.x / 2f, 0f);
            }
            _holder.sizeDelta = new Vector2(d, d);
            _holder.anchoredPosition = bubbleAt;
            Stage.sizeDelta = new Vector2(d * 1.5f, d);
            _captions.sizeDelta = words;
            _captions.anchoredPosition = wordsAt;
            Stage.localScale = Vector3.one * _zoom;
            Stage.anchoredPosition = AimOffset(_aimX, _aimY, _zoom);
        }

        /// <summary>
        /// Points the bubble at part of the picture: (<paramref name="fx"/>, <paramref name="fy"/>) in
        /// stage fractions at its centre, magnified by <paramref name="zoom"/> (1 shows the whole
        /// height), moving there over <paramref name="seconds"/>. The picture always fills the
        /// bubble, so the aim stops at its edges. Does nothing for a movie that is not in bubbles.
        /// </summary>
        protected Tween Aim(float fx, float fy, float zoom = 1f, float seconds = 0f)
        {
            if (!InBubbles) return null;
            _aimX = fx; _aimY = fy; _zoom = Mathf.Max(1f, zoom);
            var to = AimOffset(fx, fy, _zoom);
            DOTween.Kill(Stage);
            if (seconds <= 0f)
            {
                Stage.anchoredPosition = to;
                Stage.localScale = Vector3.one * _zoom;
                return null;
            }
            var seq = DOTween.Sequence().SetUpdate(true).SetTarget(Stage);
            seq.Join(Stage.DOAnchorPos(to, seconds).SetEase(Ease.InOutSine));
            seq.Join(Stage.DOScale(_zoom, seconds).SetEase(Ease.InOutSine));
            return Track(seq);
        }

        Vector2 AimOffset(float fx, float fy, float zoom)
        {
            float d = _holder.sizeDelta.x;
            float maxX = 0.75f * d * zoom - 0.5f * d, maxY = 0.5f * d * zoom - 0.5f * d;
            return new Vector2(Mathf.Clamp(-(fx - 0.5f) * 1.5f * d * zoom, -maxX, maxX),
                               Mathf.Clamp(-(fy - 0.5f) * d * zoom, -maxY, maxY));
        }

        /// <summary>The next memory drifts up out of the dark, small and far at first.</summary>
        IEnumerator FloatIn()
        {
            float d = _holder.sizeDelta.x;
            _bubble.DOKill();
            _bubbleGroup.DOKill();
            _bubble.localScale = Vector3.one * 0.25f;
            _bubble.anchoredPosition = new Vector2(0f, d * 0.2f);
            _bubbleGroup.alpha = 0f;
            Track(_bubbleGroup.DOFade(1f, 0.7f).SetUpdate(true));
            Track(_bubble.DOScale(1f, 1.6f).SetEase(Ease.OutSine).SetUpdate(true));
            Track(_bubble.DOAnchorPos(Vector2.zero, 1.6f).SetEase(Ease.OutSine).SetUpdate(true));
            yield return Wait(1.2f);
        }

        /// <summary>Pop: the bubble swells a touch, bursts into droplets, and is gone.</summary>
        IEnumerator PopBubble()
        {
            float d = _holder.sizeDelta.x;
            Sfx.Play(SfxKind.Pop, 0.9f);
            Track(_bubble.DOScale(1.08f, 0.12f).SetEase(Ease.OutQuad).SetUpdate(true));
            yield return Wait(0.1f);
            Track(_bubbleGroup.DOFade(0f, 0.15f).SetUpdate(true));
            for (int i = 0; i < 22; i++)
            {
                var drop = UiKit.Image(_root, "Drop", UiKit.Circle, new Color(0.82f, 0.97f, 1f, 0.9f));
                drop.raycastTarget = false;
                var r = drop.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                float s = UnityEngine.Random.Range(10f, 26f);
                r.sizeDelta = new Vector2(s, s);
                float a = i / 22f * Mathf.PI * 2f + UnityEngine.Random.Range(-0.15f, 0.15f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var from = _holder.anchoredPosition + dir * d * 0.52f;
                r.anchoredPosition = from;
                Track(r.DOAnchorPos(from + dir * UnityEngine.Random.Range(60f, 160f), 0.6f).SetEase(Ease.OutQuad).SetUpdate(true));
                Track(drop.DOFade(0f, 0.6f).SetEase(Ease.InQuad).SetUpdate(true).OnComplete(() => { if (r != null) Destroy(r.gameObject); }));
            }
            yield return Wait(0.45f);
        }

        /// <summary>Glimmers drifting in the dark around the memories.</summary>
        IEnumerator Motes()
        {
            while (true)
            {
                var size = ((RectTransform)transform).rect.size;
                bool warm = UnityEngine.Random.value < 0.35f;
                var m = UiKit.Image(_root, "Mote", SoftArt(), warm ? new Color(1f, 0.82f, 0.55f, 0f) : new Color(0.7f, 1f, 0.95f, 0f));
                m.raycastTarget = false;
                var r = m.rectTransform;
                r.SetSiblingIndex(_holder.GetSiblingIndex());   // behind the bubble and the words
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                float s = UnityEngine.Random.Range(10f, 28f);
                r.sizeDelta = new Vector2(s, s);
                var from = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f) * size.x, UnityEngine.Random.Range(-0.5f, 0.4f) * size.y);
                r.anchoredPosition = from;
                float life = UnityEngine.Random.Range(3.5f, 6f), peak = UnityEngine.Random.Range(0.3f, 0.75f);
                Track(r.DOAnchorPos(from + new Vector2(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(80f, 200f)), life).SetEase(Ease.Linear).SetUpdate(true));
                var seq = DOTween.Sequence().SetUpdate(true);
                seq.Append(m.DOFade(peak, life * 0.3f).SetEase(Ease.OutSine));
                seq.AppendInterval(life * 0.4f);
                seq.Append(m.DOFade(0f, life * 0.3f).SetEase(Ease.InSine));
                seq.OnComplete(() => { if (r != null) Destroy(r.gameObject); });
                Track(seq);
                yield return Wait(UnityEngine.Random.Range(0.18f, 0.4f));
            }
        }

        /// <summary>
        /// The bubble's glass, painted: clear in the middle so the memory shows true, a soft pale
        /// rim that thickens toward the edge with a bright line on it, and a curved shine at the
        /// top-left with a fainter one opposite.
        /// </summary>
        static Sprite GlassArt()
        {
            if (_glassArt != null) return _glassArt;
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f, d = Mathf.Sqrt(u * u + v * v);
                    if (d > 1f) { px[y * n + x] = new Color32(255, 255, 255, 0); continue; }
                    float ang = Mathf.Atan2(v, u);
                    float rim = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.78f, 0.99f, d)) * 0.42f;
                    float line = Mathf.Exp(-Mathf.Pow((d - 0.965f) / 0.014f, 2f)) * 0.75f;
                    float shine = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(ang - 2.36f)), 5f) * Mathf.Exp(-Mathf.Pow((d - 0.83f) / 0.05f, 2f)) * 0.7f;
                    float back = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(ang + 0.79f)), 6f) * Mathf.Exp(-Mathf.Pow((d - 0.88f) / 0.04f, 2f)) * 0.3f;
                    float dot = Mathf.Exp(-(Mathf.Pow(u + 0.52f, 2f) + Mathf.Pow(v - 0.5f, 2f)) / 0.0025f) * 0.85f;
                    float edge = Mathf.Clamp01((1f - d) * n * 0.5f);   // smooth outer edge
                    float a = Mathf.Clamp01(rim + line + shine + back + dot) * edge;
                    // tinted mint at the rim, white where it shines
                    float white = Mathf.Clamp01((line + shine + dot) * 1.5f);
                    var c = Color.Lerp(new Color(0.78f, 0.97f, 0.93f), Color.white, white);
                    px[y * n + x] = new Color(c.r, c.g, c.b, a);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return _glassArt = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>A soft round light, bright in the middle and fading to nothing (glows, motes).</summary>
        protected static Sprite SoftLight => SoftArt();

        static Sprite SoftArt()
        {
            if (_softArt != null) return _softArt;
            const int n = 48;
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
            return _softArt = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        RectTransform Bar(string name, float edge)
        {
            var bar = UiKit.Image(_root, name, null, Color.black).rectTransform;
            bar.anchorMin = new Vector2(0f, edge);
            bar.anchorMax = new Vector2(1f, edge);
            bar.pivot = new Vector2(0.5f, edge);
            bar.sizeDelta = new Vector2(0f, 0f);
            bar.GetComponent<Image>().raycastTarget = false;
            return bar;
        }

        IEnumerator Run()
        {
            // The canvas only knows its real size after a frame; actors are sized from the stage.
            yield return null;
            Canvas.ForceUpdateCanvases();
            Fit();
            if (InBubbles)
            {
                _flash.color = Color.clear;
                Aim(BubbleView.x, BubbleView.y, BubbleView.z);
                yield return FloatIn();
            }
            else
            {
                _flash.color = Edge;
                Track(_flash.DOFade(0f, 0.6f).SetUpdate(true));
                Track(_barTop.DOSizeDelta(new Vector2(0f, 90f), 0.6f).SetUpdate(true));
                Track(_barBottom.DOSizeDelta(new Vector2(0f, 90f), 0.6f).SetUpdate(true));
            }
            yield return Script();
            yield return End();
        }

        /// <summary>The colour the movie opens out of and closes into: a white flash by default.</summary>
        protected virtual Color Edge => Color.white;

        /// <summary>The movie itself.</summary>
        protected abstract IEnumerator Script();

        public void OnPointerClick(PointerEventData e)
        {
            if (ReaderPaced) { if (_waiting) _advance = true; return; }
            Skip();
        }

        IEnumerator End()
        {
            if (_ending) yield break;
            _ending = true;
            if (InBubbles)
            {
                foreach (var g in _captions.GetComponentsInChildren<Graphic>()) Track(g.DOFade(0f, 0.3f).SetUpdate(true));
                yield return PopBubble();
                yield return new WaitForSecondsRealtime(0.5f);
            }
            else
            {
                _flash.DOKill();
                _flash.color = new Color(Edge.r, Edge.g, Edge.b, _flash.color.a);
                Track(_flash.DOFade(1f, 0.45f).SetUpdate(true));
                yield return new WaitForSecondsRealtime(0.5f);
            }
            foreach (var t in _tweens) t?.Kill();
            Core.ScreenMetrics.Changed -= Fit;
            Time.timeScale = _timeScaleBefore <= 0f ? 1f : _timeScaleBefore;
            _running = null;
            var done = _done;
            Destroy(gameObject);
            done?.Invoke();
        }

        void OnDestroy()
        {
            Core.ScreenMetrics.Changed -= Fit;
            if (_running == this)
            {
                _running = null;
                Time.timeScale = _timeScaleBefore <= 0f ? 1f : _timeScaleBefore;
            }
            foreach (var t in _tweens) t?.Kill();
        }

        // ---------------------------------------------------------------- helpers for scripts

        protected static WaitForSecondsRealtime Wait(float seconds) => new(seconds);

        protected T Track<T>(T tween) where T : Tween
        {
            _tweens.Add(tween);
            return tween;
        }

        /// <summary>A picture filling the stage, or a soft two-colour sky if there is no picture.</summary>
        protected Image Backdrop(Sprite art, Color top, Color bottom)
        {
            var sky = UiKit.Image(Stage, "Sky", null, bottom);
            UiKit.Stretch(sky.rectTransform);
            var upper = UiKit.Image(Stage, "SkyTop", null, top);
            upper.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            upper.rectTransform.anchorMax = Vector2.one;
            upper.rectTransform.offsetMin = upper.rectTransform.offsetMax = Vector2.zero;
            if (art == null) return sky;
            var pic = UiKit.Image(Stage, "Backdrop", art, Color.white);
            UiKit.Stretch(pic.rectTransform);
            pic.preserveAspect = false;
            return pic;
        }

        /// <summary>
        /// A change of scene: the picture fades out through <paramref name="through"/>, the stage is
        /// cleared and <paramref name="build"/> sets the next one up, and it fades back in.
        /// </summary>
        protected IEnumerator Change(Color through, float seconds, Action build)
        {
            if (InBubbles)
            {
                // This memory's bubble pops; the next drifts in out of the dark.
                yield return PopBubble();
                ClearStage();
                Aim(0.5f, 0.5f, 1f);
                build?.Invoke();
                yield return null;
                yield return FloatIn();
                yield break;
            }
            _flash.DOKill();
            _flash.color = new Color(through.r, through.g, through.b, 0f);
            Track(_flash.DOFade(1f, seconds * 0.5f).SetUpdate(true));
            yield return Wait(seconds * 0.5f);
            ClearStage();
            build?.Invoke();
            yield return null;
            Track(_flash.DOFade(0f, seconds * 0.5f).SetUpdate(true));
        }

        void ClearStage()
        {
            foreach (var layer in new[] { Stage, _captions })
                for (int i = layer.childCount - 1; i >= 0; i--)
                {
                    var child = layer.GetChild(i);
                    foreach (var t in child.GetComponentsInChildren<Transform>(true)) DOTween.Kill(t);
                    foreach (var g in child.GetComponentsInChildren<Graphic>(true)) DOTween.Kill(g);
                    Destroy(child.gameObject);
                }
            _caption = null;
        }

        /// <summary>A quick flash of colour over the stage (a gust of cold, a burst of magic).</summary>
        protected void Flash(Color colour, float seconds)
        {
            _flash.DOKill();
            _flash.color = colour;
            Track(_flash.DOFade(0f, seconds).SetUpdate(true));
        }

        TextMeshProUGUI _caption;
        RectTransform _captions;

        /// <summary>
        /// A line of narration along the bottom of the picture, on a soft dark band. A new line
        /// replaces the last one.
        /// </summary>
        protected void Caption(string text)
        {
            if (_caption != null)
            {
                var old = _caption;
                Track(old.DOFade(0f, 0.3f).SetUpdate(true).OnComplete(() => { if (old != null) Destroy(old.transform.parent.gameObject); }));
            }
            float h = Stage.rect.height;
            var band = UiKit.Image(_captions, "CaptionBand", null, new Color(0.08f, 0.06f, 0.05f, 0.5f));
            band.raycastTarget = false;
            var br = band.rectTransform;
            float bandAlpha = 0.5f, size = h * 0.05f;
            if (InBubbles)
            {
                // the words above, room for Next below; on the dark, no band is needed
                br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one;
                br.offsetMin = new Vector2(0f, 100f); br.offsetMax = Vector2.zero;
                bandAlpha = 0f;
                size = Mathf.Clamp(h * 0.055f, 30f, 50f);
            }
            else
            {
                br.anchorMin = new Vector2(0f, 0f); br.anchorMax = new Vector2(1f, 0f);
                br.pivot = new Vector2(0.5f, 0f);
                br.sizeDelta = new Vector2(0f, h * 0.17f);
                br.anchoredPosition = Vector2.zero;
            }
            var t = UiKit.Text(br, "Caption", text, size, Color.white, true, TextAlignmentOptions.Center);
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            if (InBubbles)
            {
                t.enableAutoSizing = true;
                t.fontSizeMin = size * 0.6f;
                t.fontSizeMax = size;
                UiKit.Stretch(t.rectTransform, 8f, 4f, 8f, 4f);
            }
            else UiKit.Stretch(t.rectTransform, h * 0.04f, h * 0.01f, h * 0.04f, h * 0.01f);
            t.alpha = 0f;
            band.color = new Color(band.color.r, band.color.g, band.color.b, 0f);
            Track(t.DOFade(1f, 0.5f).SetUpdate(true));
            Track(band.DOFade(bandAlpha, 0.5f).SetUpdate(true));
            _caption = t;
            Narrating();
        }

        /// <summary>Called as each line of narration appears (a narrator can bob along).</summary>
        protected virtual void Narrating() { }

        /// <summary>A prop or picture placed by its centre.</summary>
        protected RectTransform Prop(Sprite art, Vector2 at, float height, Color? tint = null)
        {
            var img = UiKit.Image(Stage, "Prop", art, tint ?? Color.white);
            img.preserveAspect = true;
            img.raycastTarget = false;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = at;
            r.pivot = new Vector2(0.5f, 0.5f);
            float h = height * Stage.rect.height;
            float aspect = art != null ? art.rect.width / art.rect.height : 1f;
            r.sizeDelta = new Vector2(h * aspect, h);
            return r;
        }

        /// <summary>Someone in the scene, standing on their feet at <paramref name="at"/>.</summary>
        protected Actor Cast(Sprite art, Vector2 at, float height, bool faceRight = false, Color? tint = null)
        {
            var root = UiKit.Rect("Actor", Stage);
            root.anchorMin = root.anchorMax = at;
            root.pivot = new Vector2(0.5f, 0f);
            float h = height * Stage.rect.height;
            float aspect = art != null ? art.rect.width / art.rect.height : 1f;
            root.sizeDelta = new Vector2(h * aspect, h);
            var body = UiKit.Image(root, "Body", art, tint ?? Color.white);
            body.preserveAspect = true;
            body.raycastTarget = false;
            UiKit.Stretch(body.rectTransform);
            body.rectTransform.pivot = new Vector2(0.5f, 0f);
            var actor = new Actor(this, root, body.rectTransform);
            actor.Face(faceRight);
            return actor;
        }

        /// <summary>The player's capybara as it is dressed right now.</summary>
        protected Actor CastPlayer(Vector2 at, float height, bool faceRight)
        {
            var body = Wardrobe.Ready ? Wardrobe.BaseSprite : ItemCatalog.Instance != null ? ItemCatalog.Instance.Icon("Capybara") : null;
            var actor = Cast(body, at, height, faceRight);
            if (!Wardrobe.Ready) return actor;
            var size = actor.Root.sizeDelta;
            foreach (WearSlot slot in Enum.GetValues(typeof(WearSlot)))
            {
                var item = Wardrobe.Worn(slot);
                var art = Wardrobe.Art(item);
                if (art == null) continue;
                var layer = UiKit.Image(actor.Body, slot.ToString(), art, Color.white);
                layer.preserveAspect = true;
                layer.raycastTarget = false;
                var r = layer.rectTransform;
                r.anchorMin = r.anchorMax = item.Anchor;
                r.pivot = new Vector2(0.5f, 0.5f);
                float pw = item.Width * size.x;
                r.sizeDelta = new Vector2(pw, pw * art.rect.height / art.rect.width);
                r.anchoredPosition = Vector2.zero;
                r.localRotation = Quaternion.Euler(0f, 0f, item.Rotation);
                if (item.Behind) r.SetAsFirstSibling();
            }
            // The body image itself must stay under the pieces it carries.
            return actor;
        }

        /// <summary>A little bubble with a picture over someone's head that pops up, hangs, and goes.</summary>
        protected RectTransform Bubble(Actor over, Sprite icon, Color colour, float seconds = 1.4f, float size = 0.11f)
        {
            float px = size * Stage.rect.height;
            var disc = UiKit.Image(Stage, "Bubble", UiKit.Circle, Color.white);
            disc.raycastTarget = false;
            var r = disc.rectTransform;
            r.anchorMin = r.anchorMax = over.Root.anchorMin;
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(px, px);
            r.anchoredPosition = over.Root.anchoredPosition + new Vector2(0f, over.Root.sizeDelta.y * 1.02f);
            var ic = UiKit.Image(disc.transform, "Icon", icon, colour);
            ic.preserveAspect = true;
            ic.raycastTarget = false;
            UiKit.Stretch(ic.rectTransform, px * 0.18f, px * 0.18f, px * 0.18f, px * 0.18f);
            r.localScale = Vector3.zero;
            var seq = DOTween.Sequence().SetUpdate(true);
            seq.Append(r.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
            if (seconds > 0f)
            {
                seq.AppendInterval(seconds);
                seq.Append(r.DOScale(0f, 0.2f).SetEase(Ease.InBack));
                seq.AppendCallback(() => { if (r != null) Destroy(r.gameObject); });
            }
            Track(seq);
            return r;
        }

        /// <summary>Confetti and petals drifting down over the whole stage.</summary>
        protected void Confetti(int count, float seconds)
        {
            var colours = new[] { UiKit.Hex("F7A8C4"), UiKit.Hex("FFFFFF"), UiKit.Hex("FFD36E"), UiKit.Hex("9ED9C8"), UiKit.Hex("F58E8E"), UiKit.Hex("C7B3F2") };
            float w = Stage.rect.width, h = Stage.rect.height;
            for (int i = 0; i < count; i++)
            {
                var bit = UiKit.Image(Stage, "Confetto", i % 3 == 0 ? UiKit.Circle : UiKit.Rounded(6), colours[i % colours.Length]);
                bit.raycastTarget = false;
                var r = bit.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
                float s = UnityEngine.Random.Range(0.012f, 0.022f) * h;
                r.sizeDelta = new Vector2(s, s * (i % 3 == 0 ? 1f : 0.55f));
                float x = UnityEngine.Random.Range(0f, w);
                r.anchoredPosition = new Vector2(x, UnityEngine.Random.Range(10f, h * 0.4f));
                float fall = UnityEngine.Random.Range(0.75f, 1f) * seconds;
                float delay = UnityEngine.Random.Range(0f, seconds * 0.5f);
                Track(r.DOAnchorPos(new Vector2(x + UnityEngine.Random.Range(-0.15f, 0.15f) * w, -h * 1.05f), fall).SetDelay(delay).SetEase(Ease.Linear).SetUpdate(true));
                Track(r.DOLocalRotate(new Vector3(0f, 0f, UnityEngine.Random.Range(-720f, 720f)), fall, RotateMode.FastBeyond360).SetDelay(delay).SetUpdate(true));
            }
        }

        /// <summary>Big text sliding up over the stage, such as a title.</summary>
        protected TextMeshProUGUI Title(string text, string sub, Vector2 at)
        {
            // In a bubble the title takes the narration's place, under the picture.
            if (InBubbles && _caption != null)
            {
                var old = _caption;
                _caption = null;
                Track(old.DOFade(0f, 0.3f).SetUpdate(true).OnComplete(() => { if (old != null) Destroy(old.transform.parent.gameObject); }));
            }
            var t = UiKit.Text(InBubbles ? _captions : Stage, "Title", text + (string.IsNullOrEmpty(sub) ? "" : "\n<size=55%>" + sub + "</size>"),
                Stage.rect.height * (InBubbles ? 0.085f : 0.1f), Color.white, true, TextAlignmentOptions.Center);
            if (InBubbles) at = new Vector2(0.5f, 0.62f);
            t.raycastTarget = false;
            t.outlineWidth = 0.22f;
            t.outlineColor = new Color32(120, 60, 70, 255);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = at;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = InBubbles ? new Vector2(_captions.rect.width, Stage.rect.height * 0.3f) : new Vector2(Stage.rect.width * 0.9f, Stage.rect.height * 0.3f);
            r.localScale = Vector3.one * 0.6f;
            t.alpha = 0f;
            Track(t.DOFade(1f, 0.35f).SetUpdate(true));
            Track(r.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetUpdate(true));
            return t;
        }

        /// <summary>A burst of little bits (wood chips, water drops, sparkles) flying out from a point.</summary>
        protected void Burst(Vector2 at, Sprite bit, Color colour, int count, float spread, float size = 0.02f, float seconds = 0.7f, Vector2 bias = default)
        {
            float h = Stage.rect.height;
            for (int i = 0; i < count; i++)
            {
                var r = Prop(bit, at, UnityEngine.Random.Range(size * 0.6f, size * 1.4f), colour);
                var dir = (UnityEngine.Random.insideUnitCircle.normalized + bias).normalized;
                var to = r.anchoredPosition + dir * UnityEngine.Random.Range(0.4f, 1f) * spread * h;
                var seq = DOTween.Sequence().SetUpdate(true);
                seq.Append(r.DOJumpAnchorPos(to, spread * h * 0.3f, 1, seconds).SetEase(Ease.OutQuad));
                seq.Join(r.DOScale(0f, seconds).SetEase(Ease.InQuad));
                seq.AppendCallback(() => { if (r != null) Destroy(r.gameObject); });
                Track(seq);
            }
        }

        /// <summary>A soft puff (steam, smoke) that drifts up and fades.</summary>
        protected void Puff(Vector2 at, Color colour, float size = 0.08f, float rise = 0.15f, float seconds = 1.6f)
        {
            var r = Prop(IconPainter.Cloud, at, size, colour);
            var img = r.GetComponent<Image>();
            r.localScale = Vector3.one * 0.5f;
            Track(r.DOAnchorPosY(r.anchoredPosition.y + rise * Stage.rect.height, seconds).SetEase(Ease.OutSine).SetUpdate(true));
            Track(r.DOScale(1.3f, seconds).SetUpdate(true));
            Track(img.DOFade(0f, seconds).SetEase(Ease.InQuad).SetUpdate(true).OnComplete(() => { if (r != null) Destroy(r.gameObject); }));
        }

        /// <summary>A sleepy "z" that floats up from someone's head and fades.</summary>
        protected void Zed(Actor over, float size = 1f, float up = 0.9f)
        {
            float h = Stage.rect.height;
            var t = UiKit.Text(Stage, "Z", "z", h * 0.06f * size, Color.white, true, TextAlignmentOptions.Center);
            t.raycastTarget = false;
            t.outlineWidth = 0.25f;
            t.outlineColor = new Color32(70, 60, 110, 255);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = over.Root.anchorMin;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(h * 0.1f, h * 0.1f);
            r.anchoredPosition = over.Root.anchoredPosition + new Vector2(over.Root.sizeDelta.x * 0.3f, over.Root.sizeDelta.y * up);
            Track(r.DOAnchorPos(r.anchoredPosition + new Vector2(h * 0.06f, h * 0.14f), 1.4f).SetEase(Ease.OutSine).SetUpdate(true));
            Track(r.DOLocalRotate(new Vector3(0f, 0f, 20f), 1.4f).SetUpdate(true));
            Track(t.DOFade(0f, 1.4f).SetEase(Ease.InQuad).SetUpdate(true).OnComplete(() => { if (r != null) Destroy(r.gameObject); }));
        }

        // ---------------------------------------------------------------- actors

        /// <summary>One character on the stage, with a few simple moves.</summary>
        public class Actor
        {
            readonly Cutscene _scene;
            public RectTransform Root { get; }
            public RectTransform Body { get; }
            bool _right;
            float _restY;   // where the body sits: lower once sunk into water

            public Actor(Cutscene scene, RectTransform root, RectTransform body)
            {
                _scene = scene;
                Root = root;
                Body = body;
            }

            Vector2 Px(Vector2 frac) => new((frac.x - Root.anchorMin.x) * _scene.Stage.rect.width, (frac.y - Root.anchorMin.y) * _scene.Stage.rect.height);

            /// <summary>Sprites face left; this turns them.</summary>
            public void Face(bool right)
            {
                _right = right;
                var s = Body.localScale;
                Body.localScale = new Vector3(right ? -Mathf.Abs(s.x) : Mathf.Abs(s.x), s.y, 1f);
            }

            /// <summary>Hops across the stage to a point, turning to face the way it goes.</summary>
            public Tween WalkTo(Vector2 frac, float seconds, int hops = 5)
            {
                var to = Px(frac);
                if (Mathf.Abs(to.x - Root.anchoredPosition.x) > 1f) Face(to.x > Root.anchoredPosition.x);
                float hop = Root.sizeDelta.y * 0.12f;
                var seq = DOTween.Sequence().SetUpdate(true);
                seq.Join(Root.DOAnchorPos(to, seconds).SetEase(Ease.Linear));
                seq.Join(Body.DOAnchorPosY(hop, seconds / hops * 0.5f).SetLoops(hops * 2, LoopType.Yoyo).SetEase(Ease.OutQuad));
                return _scene.Track(seq);
            }

            /// <summary>One big leap in an arc to a point.</summary>
            public Tween JumpTo(Vector2 frac, float height, float seconds)
            {
                var to = Px(frac);
                if (Mathf.Abs(to.x - Root.anchoredPosition.x) > 1f) Face(to.x > Root.anchoredPosition.x);
                return _scene.Track(Root.DOJumpAnchorPos(to, height * _scene.Stage.rect.height, 1, seconds).SetUpdate(true));
            }

            /// <summary>Happy little jumps on the spot.</summary>
            public Tween Hop(int times = 3, float height = 0.18f, float each = 0.32f)
            {
                var t = Body.DOAnchorPosY(Root.sizeDelta.y * height, each * 0.5f).SetLoops(times * 2, LoopType.Yoyo).SetEase(Ease.OutQuad).SetUpdate(true);
                return _scene.Track(t);
            }

            public Tween Squash(float amount = 0.12f) =>
                _scene.Track(Body.DOPunchScale(new Vector3(amount * (_right ? -1f : 1f), -amount, 0f), 0.35f, 6, 0.6f).SetUpdate(true));

            public Tween Shake(float seconds = 0.6f) =>
                _scene.Track(Body.DOShakeAnchorPos(seconds, new Vector2(Root.sizeDelta.x * 0.04f, 0f), 30, 0f, false, false).SetUpdate(true));

            /// <summary>Leans towards the way it faces (a kiss, a bow).</summary>
            public Tween Lean(float degrees, float seconds = 0.4f) =>
                _scene.Track(Body.DOLocalRotate(new Vector3(0f, 0f, _right ? -degrees : degrees), seconds).SetEase(Ease.OutQuad).SetUpdate(true));

            /// <summary>
            /// Lowers it into water (or anything) by <paramref name="fraction"/> of its height,
            /// hiding what has gone under: the picture is drawn from the top down only.
            /// </summary>
            public Tween Sink(float fraction, float seconds)
            {
                var img = Body.GetComponent<Image>();
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Vertical;
                img.fillOrigin = (int)Image.OriginVertical.Top;
                _restY = -fraction * Root.sizeDelta.y;
                var seq = DOTween.Sequence().SetUpdate(true);
                seq.Join(img.DOFillAmount(1f - fraction, seconds));
                seq.Join(Body.DOAnchorPosY(_restY, seconds));
                return _scene.Track(seq);
            }

            public Tween Tint(Color colour, float seconds) =>
                _scene.Track(Body.GetComponent<Image>().DOColor(colour, seconds).SetUpdate(true));

            public Tween Fade(float alpha, float seconds) =>
                _scene.Track(Body.GetComponent<Image>().DOFade(alpha, seconds).SetUpdate(true));

            /// <summary>A gentle up-and-down float, for ghosts.</summary>
            /// <summary>
            /// A gentle up-and-down around where the body rests — after a Sink, around the sunk
            /// spot, so a bather bobs in the water instead of rising out of it.
            /// </summary>
            public Tween Bob(float height = 0.06f, float period = 1.4f) =>
                _scene.Track(DOTween.To(() => 0f, v => Body.anchoredPosition = new Vector2(Body.anchoredPosition.x, _restY + v),
                        Root.sizeDelta.y * height, period * 0.5f)
                    .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetTarget(Body).SetUpdate(true));

            public Tween Nudge(float fracX, float seconds = 0.4f) =>
                _scene.Track(Root.DOAnchorPosX(Root.anchoredPosition.x + fracX * _scene.Stage.rect.width, seconds).SetEase(Ease.OutQuad).SetUpdate(true));
        }
    }
}
