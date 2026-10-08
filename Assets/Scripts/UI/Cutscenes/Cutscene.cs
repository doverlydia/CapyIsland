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
            var dark = UiKit.Image(_root, "Dark", null, UiKit.Hex("1E1712"), true);   // catches the skip tap
            UiKit.Stretch(dark.rectTransform);

            // The stage keeps a 3:2 picture and fits inside the screen, whichever way the phone is held.
            Stage = UiKit.Rect("Stage", _root);
            Stage.anchorMin = Stage.anchorMax = Stage.pivot = new Vector2(0.5f, 0.5f);
            Stage.gameObject.AddComponent<RectMask2D>();
            Fit();
            Core.ScreenMetrics.Changed += Fit;

            _barTop = Bar("BarTop", 1f);
            _barBottom = Bar("BarBottom", 0f);
            _flash = UiKit.Image(_root, "Flash", null, Color.white);
            UiKit.Stretch(_flash.rectTransform);
            _flash.raycastTarget = false;

            var hint = UiKit.Text(_root, "Skip", "tap to skip", 26f, new Color(1f, 1f, 1f, 0.55f), false, TextAlignmentOptions.BottomRight);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = new Vector2(1f, 0f);
            hint.rectTransform.anchoredPosition = new Vector2(-28f, 24f);
            hint.rectTransform.sizeDelta = new Vector2(300f, 40f);
            hint.raycastTarget = false;
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
            float w = size.x, h = w / 1.5f;
            if (h > size.y * 0.86f) { h = size.y * 0.86f; w = h * 1.5f; }
            Stage.sizeDelta = new Vector2(w, h);
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
            _flash.color = Color.white;
            Track(_flash.DOFade(0f, 0.6f).SetUpdate(true));
            Track(_barTop.DOSizeDelta(new Vector2(0f, 90f), 0.6f).SetUpdate(true));
            Track(_barBottom.DOSizeDelta(new Vector2(0f, 90f), 0.6f).SetUpdate(true));
            yield return Script();
            yield return End();
        }

        /// <summary>The movie itself.</summary>
        protected abstract IEnumerator Script();

        public void OnPointerClick(PointerEventData e)
        {
            if (_skipped || _ending) return;
            _skipped = true;
            StopAllCoroutines();
            StartCoroutine(End());
        }

        IEnumerator End()
        {
            if (_ending) yield break;
            _ending = true;
            _flash.DOKill();
            Track(_flash.DOFade(1f, 0.45f).SetUpdate(true));
            yield return new WaitForSecondsRealtime(0.5f);
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
            var t = UiKit.Text(Stage, "Title", text + (string.IsNullOrEmpty(sub) ? "" : "\n<size=55%>" + sub + "</size>"),
                Stage.rect.height * 0.1f, Color.white, true, TextAlignmentOptions.Center);
            t.raycastTarget = false;
            t.outlineWidth = 0.22f;
            t.outlineColor = new Color32(120, 60, 70, 255);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = at;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(Stage.rect.width * 0.9f, Stage.rect.height * 0.3f);
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
                var seq = DOTween.Sequence().SetUpdate(true);
                seq.Join(img.DOFillAmount(1f - fraction, seconds));
                seq.Join(Body.DOAnchorPosY(-fraction * Root.sizeDelta.y, seconds));
                return _scene.Track(seq);
            }

            public Tween Tint(Color colour, float seconds) =>
                _scene.Track(Body.GetComponent<Image>().DOColor(colour, seconds).SetUpdate(true));

            public Tween Fade(float alpha, float seconds) =>
                _scene.Track(Body.GetComponent<Image>().DOFade(alpha, seconds).SetUpdate(true));

            /// <summary>A gentle up-and-down float, for ghosts.</summary>
            public Tween Bob(float height = 0.06f, float period = 1.4f) =>
                _scene.Track(Body.DOAnchorPosY(Root.sizeDelta.y * height, period * 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true));

            public Tween Nudge(float fracX, float seconds = 0.4f) =>
                _scene.Track(Root.DOAnchorPosX(Root.anchoredPosition.x + fracX * _scene.Stage.rect.width, seconds).SetEase(Ease.OutQuad).SetUpdate(true));
        }
    }
}
