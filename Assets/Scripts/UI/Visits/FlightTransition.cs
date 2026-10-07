using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The flight between islands: clouds roll in from both sides, your capybara putters across
    /// the sky in a little propeller plane while the next island loads behind them, then the
    /// clouds part to reveal where you landed.
    ///
    /// Lives on its own overlay canvas that survives the scene reload in the middle of it.
    /// </summary>
    public class FlightTransition : MonoBehaviour
    {
        const float CoverSeconds = 0.75f, FlySeconds = 2.2f, RevealSeconds = 0.9f;

        static FlightTransition _running;

        RectTransform _root;
        Image _sky;
        readonly List<RectTransform> _bigClouds = new();
        readonly List<(RectTransform r, float speed)> _drift = new();
        RectTransform _plane, _propeller;
        TextMeshProUGUI _label;

        /// <summary>
        /// Plays the flight. <paramref name="swap"/> runs once the clouds cover the screen (load the
        /// next island there); <paramref name="done"/> once they have parted again.
        /// </summary>
        public static void Play(string label, Action swap, Action done)
        {
            if (_running != null) return;
            var go = new GameObject("[Flight]", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(go);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            _running = go.AddComponent<FlightTransition>();
            _running.Build(label);
            _running.StartCoroutine(_running.Run(swap, done));
        }

        void Build(string label)
        {
            _root = UiKit.Rect("Root", transform);
            UiKit.Stretch(_root);

            _sky = UiKit.Image(_root, "Sky", null, UiKit.Hex("9FD8EE"), true);   // swallows taps while flying
            UiKit.Stretch(_sky.rectTransform, -200f, -200f, -200f, -200f);
            _sky.color = new Color(_sky.color.r, _sky.color.g, _sky.color.b, 0f);

            // Small clouds drifting past behind the plane: the sense of speed.
            for (int i = 0; i < 9; i++)
            {
                var c = UiKit.Image(_root, "Drift", IconPainter.Cloud, new Color(1f, 1f, 1f, 0.85f));
                float s = UnityEngine.Random.Range(220f, 420f);
                c.rectTransform.sizeDelta = new Vector2(s, s * 0.7f);
                c.rectTransform.anchoredPosition = new Vector2(UnityEngine.Random.Range(-900f, 900f), UnityEngine.Random.Range(-800f, 800f));
                _drift.Add((c.rectTransform, UnityEngine.Random.Range(500f, 1100f)));
                c.gameObject.SetActive(false);
            }

            // The plane: body, a capybara pilot, a spinning propeller.
            _plane = UiKit.Rect("Plane", _root);
            _plane.sizeDelta = new Vector2(360f, 360f);
            var pilotSprite = PilotSprite();
            if (pilotSprite != null)
            {
                var pilot = UiKit.Image(_plane, "Pilot", pilotSprite, Color.white);
                pilot.preserveAspect = true;
                pilot.rectTransform.sizeDelta = new Vector2(170f, 170f);
                pilot.rectTransform.anchoredPosition = new Vector2(-10f, 92f);
                pilot.rectTransform.localScale = new Vector3(-1f, 1f, 1f);   // facing the way we fly
            }
            var body = UiKit.Image(_plane, "Body", IconPainter.Plane, UiKit.Hex("E8553E"));
            UiKit.Stretch(body.rectTransform);
            var stripe = UiKit.Image(body.transform, "Stripe", UiKit.Rounded(10), new Color(1f, 1f, 1f, 0.85f));
            stripe.rectTransform.sizeDelta = new Vector2(190f, 18f);
            stripe.rectTransform.anchoredPosition = new Vector2(-30f, -4f);
            _propeller = UiKit.Image(_plane, "Propeller", UiKit.Rounded(12), UiKit.Ink).rectTransform;
            _propeller.sizeDelta = new Vector2(16f, 150f);
            _propeller.anchoredPosition = new Vector2(146f, 4f);
            _plane.gameObject.SetActive(false);

            _label = UiKit.Text(_root, "Label", label + "...", 46f, Color.white, true, TextAlignmentOptions.Center);
            _label.rectTransform.anchorMin = new Vector2(0f, 0f);
            _label.rectTransform.anchorMax = new Vector2(1f, 0f);
            _label.rectTransform.pivot = new Vector2(0.5f, 0f);
            _label.rectTransform.sizeDelta = new Vector2(-60f, 120f);
            _label.rectTransform.anchoredPosition = new Vector2(0f, 260f);
            _label.outlineWidth = 0.22f;
            _label.outlineColor = new Color32(70, 120, 150, 255);
            _label.alpha = 0f;

            // The big clouds that close over the screen and open again.
            for (int i = 0; i < 15; i++)
            {
                var c = UiKit.Image(_root, "Cover", IconPainter.Cloud, Color.Lerp(Color.white, UiKit.Hex("EAF6FB"), UnityEngine.Random.value * 0.6f));
                float w = UnityEngine.Random.Range(1100f, 1600f);
                c.rectTransform.sizeDelta = new Vector2(w, w * UnityEngine.Random.Range(0.6f, 0.8f));
                c.rectTransform.localScale = new Vector3(UnityEngine.Random.value < 0.5f ? -1f : 1f, 1f, 1f);
                _bigClouds.Add(c.rectTransform);
            }
        }

        static Sprite PilotSprite()
        {
            if (Game.Wardrobe.Ready) return Game.Wardrobe.BaseSprite;
            var king = FindFirstObjectByType<Game.PlayerAvatar>();
            var body = king != null ? king.transform.Find("Sprite") : null;
            var sr = body != null ? body.GetComponent<SpriteRenderer>() : null;
            return sr != null ? sr.sprite : null;
        }

        readonly List<(Vector2 cover, Vector2 away)> _spots = new();

        /// <summary>Where each big cloud rests while covering (a jittered 3×5 heap), and where it waits off screen.</summary>
        (Vector2 cover, Vector2 away) CloudSpots(int i, Vector2 half)
        {
            while (_spots.Count <= i)
            {
                int n = _spots.Count, col = n % 3, row = n / 3;
                var cover = new Vector2(
                    Mathf.Lerp(-half.x * 0.7f, half.x * 0.7f, col / 2f) + UnityEngine.Random.Range(-120f, 120f),
                    Mathf.Lerp(half.y * 0.9f, -half.y * 0.9f, row / 4f) + UnityEngine.Random.Range(-90f, 90f));
                bool left = col == 0 || (col == 1 && UnityEngine.Random.value < 0.5f);
                var away = new Vector2(left ? -half.x - 1100f : half.x + 1100f, cover.y + UnityEngine.Random.Range(-200f, 200f));
                _spots.Add((cover, away));
            }
            return _spots[i];
        }

        IEnumerator Run(Action swap, Action done)
        {
            var half = _root.rect.size * 0.5f;
            for (int i = 0; i < _bigClouds.Count; i++)
            {
                var (cover, away) = CloudSpots(i, half);
                _bigClouds[i].anchoredPosition = away;
                _bigClouds[i].DOAnchorPos(cover, CoverSeconds).SetEase(Ease.OutCubic).SetUpdate(true);
            }
            _sky.DOFade(1f, CoverSeconds * 0.8f).SetUpdate(true);
            Game.Sfx.Play(Game.SfxKind.Bridge, 0.6f);
            yield return new WaitForSecondsRealtime(CoverSeconds + 0.05f);

            // Behind the clouds: the old island goes, the new one loads.
            try { swap?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }

            // The flight itself.
            foreach (var (r, _) in _drift) r.gameObject.SetActive(true);
            _plane.gameObject.SetActive(true);
            _plane.anchoredPosition = new Vector2(-half.x - 300f, 40f);
            _plane.DOAnchorPosX(half.x * 0.15f, FlySeconds).SetEase(Ease.OutSine).SetUpdate(true);
            _label.DOFade(1f, 0.4f).SetUpdate(true);
            for (int i = 0; i < _bigClouds.Count; i++)
            {
                // The cover clouds thin out to show the sky the plane is crossing.
                var (_, away) = CloudSpots(i, half);
                _bigClouds[i].DOAnchorPos(Vector2.Lerp(_bigClouds[i].anchoredPosition, away, 0.55f), 0.6f).SetUpdate(true);
            }

            float t = 0f;
            while (t < FlySeconds)
            {
                t += Time.unscaledDeltaTime;
                Fly(t, half);
                yield return null;
            }

            // Land: the plane zooms off, the clouds part, the island appears.
            _plane.DOAnchorPosX(half.x + 500f, 0.6f).SetEase(Ease.InCubic).SetUpdate(true);
            _label.DOFade(0f, 0.3f).SetUpdate(true);
            for (int i = 0; i < _bigClouds.Count; i++)
            {
                var (_, away) = CloudSpots(i, half);
                _bigClouds[i].DOAnchorPos(away, RevealSeconds).SetEase(Ease.InCubic).SetUpdate(true);
            }
            foreach (var (r, _) in _drift)
                r.GetComponent<Image>().DOFade(0f, RevealSeconds * 0.6f).SetUpdate(true);
            _sky.DOFade(0f, RevealSeconds).SetUpdate(true);
            float until = Time.unscaledTime + RevealSeconds;
            while (Time.unscaledTime < until)
            {
                Fly(t += Time.unscaledDeltaTime, half);
                yield return null;
            }

            _running = null;
            done?.Invoke();
            Destroy(gameObject);
        }

        void Fly(float t, Vector2 half)
        {
            if (_propeller != null) _propeller.localScale = new Vector3(1f, Mathf.Abs(Mathf.Sin(t * 40f)), 1f);
            if (_plane != null)
            {
                var p = _plane.anchoredPosition;
                _plane.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.4f) * 4f);
                _plane.anchoredPosition = new Vector2(p.x, 40f + Mathf.Sin(t * 3.1f) * 26f);
            }
            float dt = Time.unscaledDeltaTime;
            foreach (var (r, speed) in _drift)
            {
                var p = r.anchoredPosition;
                p.x -= speed * dt;
                if (p.x < -half.x - 400f) p = new Vector2(half.x + 400f, UnityEngine.Random.Range(-half.y, half.y));
                r.anchoredPosition = p;
            }
        }
    }
}
