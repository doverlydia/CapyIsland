using System;
using System.Collections;
using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The finale, once every warm heart is home (the wedding's last of all): evening at the
    /// spring. The lanterns along the path light one by one, everyone gathers (Old Yuzu, Miso,
    /// Captain Wasabi, Salty Kombu, a capybara soaking, a cat), little bubbles rise from the
    /// steam and pop into fireflies, and Old Yuzu has the last word. Then the title from the
    /// opening comes back: Capy Island, and under it, instead of the Big Chill, "Home Again".
    /// </summary>
    public class HomeAgainCutscene : Cutscene
    {
        public static void Show(Action done = null) => Play<HomeAgainCutscene>(() => { IntroAudio.Stop(2.5f); done?.Invoke(); });

        protected override bool ReaderPaced => true;
        protected override Vector3 BubbleView => new(0.45f, 0.5f, 1f);

        static Sprite Art(string path) => Resources.Load<Sprite>(path);

        static Sprite Icon(string name)
        {
            var catalog = ItemCatalog.Instance;
            return catalog != null && catalog.Has(name) ? catalog.Icon(name) : null;
        }

        /// <summary>The paper lanterns strung along the path in the picture.</summary>
        static readonly Vector2[] Lanterns = { new(0.18f, 0.555f), new(0.258f, 0.55f), new(0.33f, 0.555f), new(0.42f, 0.55f) };

        protected override IEnumerator Script()
        {
            var you = string.IsNullOrEmpty(PlayerProfile.Name) ? PlayerProfile.Name : "<color=#8EE07A>" + PlayerProfile.Name + "</color>";
            IntroAudio.Play("intro_dawn", 2f);
            Backdrop(Art("Cutscenes/Cutscene_IntroWarm"), UiKit.Hex("9FD3EA"), UiKit.Hex("F2D49A"));
            StartCoroutine(Steam());

            // Evening comes on, a soft violet over everything.
            var dusk = UiKit.Image(Stage, "Dusk", null, new Color(0.14f, 0.08f, 0.26f, 0f));
            dusk.raycastTarget = false;
            UiKit.Stretch(dusk.rectTransform);

            // Everyone, gathered by the spring.
            var bather = Cast(Icon("Capybara"), new Vector2(0.24f, 0.22f), 0.15f, faceRight: true);
            bather.Sink(0.5f, 0.01f); bather.Bob(0.03f, 1.9f);
            var wasabi = Icon("Pirate Peach") is Sprite w ? Cast(w, new Vector2(0.28f, 0.12f), 0.22f, faceRight: true) : null;
            var player = CastPlayer(new Vector2(0.37f, 0.1f), 0.22f, faceRight: true);
            var yuzu = Cast(Art("Cutscenes/Cutscene_IntroYuzu"), new Vector2(0.47f, 0.13f), 0.24f);
            var miso = Icon("Coriander") is Sprite m ? Cast(m, new Vector2(0.56f, 0.11f), 0.21f) : null;
            var kombu = Icon("Pirate Pete") is Sprite k ? Cast(k, new Vector2(0.65f, 0.13f), 0.22f) : null;
            var cat = HobbyData.Art("Cat_Ginger") is Sprite c ? Cast(c, new Vector2(0.43f, 0.04f), 0.08f) : null;
            yield return Wait(1f);

            Track(dusk.DOFade(0.55f, 3f).SetEase(Ease.InOutSine).SetUpdate(true));
            yield return Wait(1.6f);

            // The lanterns light, one by one.
            foreach (var at in Lanterns)
            {
                var glow = Prop(SoftLight, at, 0.12f, new Color(1f, 0.78f, 0.4f, 0f));
                Track(glow.GetComponent<Image>().DOFade(0.95f, 0.5f).SetUpdate(true));
                Track(glow.DOScale(1.15f, 1.3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true));
                IntroAudio.Sfx("Sparkle", 0.3f);
                yield return Wait(0.45f);
            }

            // Little bubbles rise from the steam and pop into fireflies.
            StartCoroutine(Fireflies());
            yield return Wait(1.6f);

            // Everyone, one after another: a heart.
            foreach (var a in new[] { wasabi, player, miso, kombu })
            {
                if (a == null) continue;
                a.Hop(1, 0.12f, 0.28f);
                Bubble(a, IconPainter.Heart, UiKit.Hex("F2668B"), 1.2f, 0.09f);
                yield return Wait(0.35f);
            }
            if (cat != null) cat.Hop(2, 0.1f, 0.22f);
            yield return Wait(0.8f);

            // Old Yuzu has the last word.
            yuzu.Squash(0.08f);
            Caption(StoryText.Yuzu.Finale1);
            yield return Wait(1.2f);
            yield return Next();
            yuzu.Hop(2, 0.14f, 0.3f);
            Bubble(yuzu, IconPainter.Heart, UiKit.Hex("F2668B"), 1.6f);
            Caption(string.Format(StoryText.Yuzu.Finale2, you));
            yield return Wait(1.2f);
            yield return Next();

            // Everyone cheers, and the title comes back, with a new line under it.
            foreach (var a in new[] { wasabi, player, yuzu, miso, kombu })
                a?.Hop(3, 0.16f, 0.3f);
            Burst(new Vector2(0.45f, 0.5f), IconPainter.Star, UiKit.Hex("FFE08A"), 22, 0.35f, 0.025f, 1.4f, Vector2.up);
            IntroAudio.Sfx("Crown", 0.7f);
            Title(StoryText.IntroTitle, StoryText.Yuzu.FinaleSubtitle, new Vector2(0.5f, 0.74f));
            yield return Wait(2f);
            yield return Next();
        }

        IEnumerator Fireflies()
        {
            float h = Stage.rect.height;
            while (true)
            {
                var from = new Vector2(UnityEngine.Random.Range(0.12f, 0.45f), 0.26f);
                var bubble = Prop(UiKit.Circle, from, 0.022f, new Color(0.85f, 0.97f, 1f, 0.55f));
                float rise = UnityEngine.Random.Range(0.18f, 0.32f);
                var seq = DOTween.Sequence().SetUpdate(true);
                seq.Append(bubble.DOAnchorPosY(bubble.anchoredPosition.y + rise * h, 1.6f).SetEase(Ease.OutSine));
                seq.AppendCallback(() =>
                {
                    if (bubble == null) return;
                    // pop! and a firefly drifts off
                    var at = (Vector2)bubble.anchorMin + new Vector2(0f, rise);
                    Destroy(bubble.gameObject);
                    var fly = Prop(SoftLight, at, 0.035f, new Color(0.95f, 1f, 0.55f, 0.95f));
                    var path = new Vector2(UnityEngine.Random.Range(-0.12f, 0.12f) * Stage.rect.width, UnityEngine.Random.Range(0.02f, 0.12f) * h);
                    Track(fly.DOAnchorPos(fly.anchoredPosition + path, 3f).SetEase(Ease.InOutSine).SetUpdate(true));
                    Track(fly.GetComponent<Image>().DOFade(0f, 1f).SetDelay(2f).SetUpdate(true).OnComplete(() => { if (fly != null) Destroy(fly.gameObject); }));
                });
                Track(seq);
                yield return Wait(UnityEngine.Random.Range(0.25f, 0.5f));
            }
        }

        IEnumerator Steam()
        {
            while (true)
            {
                Puff(new Vector2(0.3f + UnityEngine.Random.Range(-0.16f, 0.16f), 0.26f), new Color(1f, 1f, 1f, 0.3f), 0.08f, 0.13f, 2.2f);
                yield return Wait(0.45f);
            }
        }
    }
}
