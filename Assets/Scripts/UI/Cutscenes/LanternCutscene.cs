using System;
using System.Collections;
using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The first time you tap a lantern on the night sea: the shore at dusk, Old Mochi with a paper
    /// lantern in his paws. It is Yuzu's wedding lantern; they floated it out together once, to tell
    /// the spirits they would look after the islands, and he lit it every year until the Big Chill
    /// put the candle out. He lights it again and sets it on the water, and it drifts out to join
    /// the others — one for every warm heart you have brought home.
    /// </summary>
    public class LanternCutscene : Cutscene
    {
        const string SeenKey = "CapyIsland.LanternStorySeen";

        public static bool Seen => PlayerPrefs.GetInt(SeenKey, 0) == 1;

        public static void Show(Action done = null)
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            Play<LanternCutscene>(() => { IntroAudio.Stop(2f); done?.Invoke(); });
        }

        protected override bool ReaderPaced => true;
        protected override Vector3 BubbleView => new(0.5f, 0.42f, 1f);

        protected override IEnumerator Script()
        {
            IntroAudio.Play("intro_dawn", 2f);
            Backdrop(Resources.Load<Sprite>("Cutscenes/Cutscene_Shore"), UiKit.Hex("3B2D5C"), UiKit.Hex("C98E7A"));

            // Dusk over the sea.
            var dusk = UiKit.Image(Stage, "Dusk", null, new Color(0.16f, 0.08f, 0.3f, 0.5f));
            dusk.raycastTarget = false;
            UiKit.Stretch(dusk.rectTransform);

            // The others already out on the water, glowing.
            var art = Resources.Load<Sprite>("World/Lantern_Float");
            var far = new[] { new Vector2(0.6f, 0.33f), new Vector2(0.73f, 0.37f), new Vector2(0.85f, 0.32f), new Vector2(0.5f, 0.39f) };   // on the water, below the far island
            foreach (var at in far)
            {
                var glow = Prop(SoftLight, at, 0.07f, new Color(1f, 0.75f, 0.4f, 0.7f));
                Track(glow.DOScale(1.2f, 1.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true));
                Prop(art, at, 0.035f);
            }

            var mochi = Cast(HobbyData.Art("Mochi"), new Vector2(0.32f, 0.12f), 0.3f, faceRight: true, tint: new Color(1f, 1f, 1f, 0.92f));
            mochi.Bob(0.02f, 1.6f);
            var lanternAt = new Vector2(0.42f, 0.14f);
            var lantern = Prop(art, lanternAt, 0.09f);
            yield return Wait(1f);

            Caption(StoryText.Mochi.Lantern1);
            yield return Wait(1f);
            yield return Next();
            Caption(StoryText.Mochi.Lantern2);
            yield return Wait(1f);
            yield return Next();

            // He lights it again.
            Caption(StoryText.Mochi.Lantern3);
            var light = Prop(SoftLight, lanternAt, 0.16f, new Color(1f, 0.7f, 0.55f, 0f));
            light.SetSiblingIndex(lantern.GetSiblingIndex());
            Track(light.GetComponent<Image>().DOFade(0.95f, 1.2f).SetUpdate(true));
            Track(light.DOScale(1.15f, 1.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true));
            IntroAudio.Sfx("Sparkle", 0.4f);
            yield return Wait(1.2f);
            yield return Next();

            // And sets it on the water: out it drifts, smaller and smaller, to join the rest.
            Caption(StoryText.Mochi.Lantern4);
            mochi.Hop(1, 0.06f, 0.3f);
            var to = new Vector2(0.66f, 0.35f);
            float w = Stage.rect.width, h = Stage.rect.height;
            var target = new Vector2((to.x - lanternAt.x) * w, (to.y - lanternAt.y) * h);   // props sit on their anchor
            foreach (var piece in new[] { lantern, light })
            {
                Track(piece.DOAnchorPos(target, 6f).SetEase(Ease.InOutSine).SetUpdate(true));
                Track(piece.DOScale(piece.localScale * 0.4f, 6f).SetEase(Ease.InOutSine).SetUpdate(true));
            }
            yield return Wait(2.5f);
            yield return Next();
        }
    }
}
