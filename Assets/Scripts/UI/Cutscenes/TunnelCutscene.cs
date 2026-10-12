using System;
using System.Collections;
using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Going home: you hop into the den and slide down the tunnel. We look down the slide to where
    /// it disappears, and rush along it (the painting zooms in on that far end), faster and
    /// faster; you sway round the bends ahead of us with bits of earth flying; the warm light at
    /// the end grows, you shoot off into it, and you're in your burrow. Told in a bubble, like the
    /// other little movies; tap to skip.
    /// </summary>
    public class TunnelCutscene : Cutscene
    {
        public static void Show(Action done) => Play<TunnelCutscene>(done);


        const float Length = 2.8f;

        /// <summary>The bubble looks down the middle of the slide, at you and the far end.</summary>
        protected override Vector3 BubbleView => new(0.5f, 0.5f, 1.05f);

        /// <summary>Where the slide disappears into the distance: in the painting, and on the screen.</summary>
        static readonly Vector2 FarInPainting = new(0.5f, 0.86f), Far = new(0.5f, 0.8f);

        protected override IEnumerator Script()
        {
            float w = Stage.rect.width, h = Stage.rect.height;
            Backdrop(null, UiKit.Hex("6B3F22"), UiKit.Hex("3E2413"));

            // The tunnel, looking down the slide to where it disappears. The camera rushes along it
            // by zooming in on that far end.
            var art = Resources.Load<Sprite>("Home/Tunnel");
            float aspect = art != null ? art.rect.width / art.rect.height : 0.66f;
            float tall = Mathf.Max(h * 1.35f, w / aspect * 1.25f);   // covers the screen with the far end high up
            var wall = UiKit.Image(Stage, "Tunnel", art, art != null ? Color.white : UiKit.Hex("7A4A2A"));
            wall.raycastTarget = false;
            var tunnel = wall.rectTransform;
            tunnel.anchorMin = tunnel.anchorMax = Far;
            tunnel.pivot = FarInPainting;
            tunnel.sizeDelta = new Vector2(tall * aspect, tall);

            // The warm light of home, waiting at the far end.
            var home = Prop(SoftLight, Far, 0.3f, new Color(1f, 0.8f, 0.5f, 0.25f));

            // You, sliding down ahead of the camera.
            var you = CastPlayer(new Vector2(0.5f, 0.2f), 0.24f, faceRight: true);
            var start = you.Root.anchoredPosition;
            var end = new Vector2((Far.x - 0.5f) * w, (Far.y - 0.2f) * h) + start;
            you.Root.localScale = Vector3.zero;
            Track(you.Root.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true));
            Sfx.Play(SfxKind.Slide, 0.9f, 0f);

            float t = 0f, nextClod = 0f;
            while (t < Length)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                float k = t / Length;
                tunnel.localScale = Vector3.one * (1f + 2.2f * k * k);   // rushing on, faster and faster

                // Round the bends: swaying side to side, leaning into each turn, drawing ahead.
                float sway = Mathf.Sin(t * 3.4f) * (1f - k * 0.6f);
                float ahead = Mathf.SmoothStep(0f, 0.35f, k);
                you.Root.anchoredPosition = Vector2.Lerp(start, end, ahead) + new Vector2(sway * w * 0.12f, Mathf.Sin(t * 7f) * h * 0.005f);
                you.Root.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Cos(t * 3.4f) * 20f);

                // Bits of earth flicking out from under you.
                nextClod -= dt;
                if (nextClod <= 0f)
                {
                    nextClod = Mathf.Lerp(0.12f, 0.03f, k);
                    Clod(you.Root.anchoredPosition, h * (0.6f + k));
                }

                // The light at the end grows, and you shoot off into it.
                float e = Mathf.Clamp01((k - 0.62f) / 0.38f);
                home.GetComponent<Image>().color = new Color(1f, 0.8f, 0.5f, 0.25f + e * 0.75f);
                home.localScale = Vector3.one * (1f + e * e * 6f);
                if (e > 0f)
                {
                    you.Root.anchoredPosition = Vector2.Lerp(you.Root.anchoredPosition, end, e * e);
                    you.Root.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, e * e);
                }
                yield return null;
            }
            Flash(new Color(1f, 0.85f, 0.6f), 0.5f);
            yield return Wait(0.2f);
        }

        /// <summary>A little clod of earth flying up from under you, fading as it goes.</summary>
        void Clod(Vector2 from, float speed)
        {
            var c = UiKit.Image(Stage, "Clod", UiKit.Circle, Color.Lerp(UiKit.Hex("5E3B20"), UiKit.Hex("A8774A"), UnityEngine.Random.value));
            c.raycastTarget = false;
            var r = c.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            float s = UnityEngine.Random.Range(10f, 26f);
            r.sizeDelta = new Vector2(s, s);
            r.anchoredPosition = from + new Vector2(UnityEngine.Random.Range(-60f, 60f), -30f);
            float fly = UnityEngine.Random.Range(0.35f, 0.6f);
            Track(r.DOAnchorPos(r.anchoredPosition + new Vector2(UnityEngine.Random.Range(-140f, 140f), speed * fly * 0.6f), fly).SetEase(Ease.OutQuad).SetUpdate(true));
            Track(c.DOFade(0f, fly).SetUpdate(true).OnComplete(() => { if (c != null) Destroy(c.gameObject); }));
        }
    }
}
