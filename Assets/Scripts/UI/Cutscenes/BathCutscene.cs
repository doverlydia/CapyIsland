using System;
using System.Collections;
using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// Curing a zombie capybara, as a little movie. A zombie is only a capybara who stayed out in
    /// the cold too long — so it gets a warm bath. It shuffles up shivering and grumpy, flops into
    /// the hot spring, sinks to its chin with a long sigh… a floating yuzu bonks it on the head…
    /// and it climbs out rosy and warm, shakes itself off, and hops for joy.
    /// </summary>
    public class BathCutscene : Cutscene
    {
        // Where things are in the painting (fractions of the stage).
        static readonly Vector2 PoolEdge = new(0.3f, 0.13f), InPool = new(0.45f, 0.22f), Out = new(0.62f, 0.13f);
        const float WaterY = 0.31f;

        public static void Show(Action done = null) => Play<BathCutscene>(done);

        static Sprite Icon(string name)
        {
            var catalog = ItemCatalog.Instance;
            return catalog != null && catalog.Has(name) ? catalog.Icon(name) : null;
        }

        /// <summary>In its bubble: the spring, a little close.</summary>
        protected override Vector3 BubbleView => new(0.47f, 0.42f, 1.2f);

        protected override IEnumerator Script()
        {
            Backdrop(Resources.Load<Sprite>("Cutscenes/Cutscene_Bath"), UiKit.Hex("8E9BD6"), UiKit.Hex("DCE7EE"));
            var art = Icon("Monster");
            if (art == null) yield break;

            // Steam, all the way through.
            StartCoroutine(Steam());

            var cold = new Color(0.72f, 0.82f, 0.95f);
            var zombie = Cast(art, new Vector2(-0.12f, 0.08f), 0.24f, faceRight: true, tint: cold);
            zombie.WalkTo(PoolEdge, 2f, 4);
            yield return Wait(2.1f);

            // Shivering, grumpy.
            zombie.Shake(0.8f);
            Bubble(zombie, IconPainter.Cloud, UiKit.Hex("8FB3D9"), 0.9f);
            yield return Wait(1.1f);

            // In it goes.
            zombie.JumpTo(InPool, 0.14f, 0.55f);
            yield return Wait(0.5f);
            Burst(new Vector2(InPool.x, WaterY), UiKit.Circle, UiKit.Hex("BFEAF0"), 16, 0.16f, 0.018f, 0.6f, Vector2.up);
            Sfx.Play(SfxKind.Plop, 0.9f);
            zombie.Sink(0.55f, 0.6f);
            yield return Wait(0.8f);

            // A long, happy sigh, and the cold melts away.
            Bubble(zombie, IconPainter.FaceSmile, UiKit.Hex("E3A92B"), 1.2f);
            zombie.Tint(new Color(1f, 0.88f, 0.82f), 2.2f);
            for (int i = 0; i < 3; i++)
            {
                Burst(new Vector2(InPool.x + 0.04f, WaterY + 0.01f), UiKit.Circle, new Color(1f, 1f, 1f, 0.8f), 2, 0.05f, 0.012f, 0.8f, Vector2.up * 2f);
                yield return Wait(0.35f);
            }

            // Bonk.
            var yuzu = Wardrobe.Art("Hat_Yuzu");
            if (yuzu != null)
            {
                var fruit = Prop(yuzu, new Vector2(0.62f, WaterY - 0.01f), 0.07f);
                float h = Stage.rect.height, w = Stage.rect.width;
                var head = new Vector2((InPool.x - 0.62f) * w + 0.03f * w, 0.05f * h);
                var seq = DOTween.Sequence().SetUpdate(true);
                seq.Append(fruit.DOAnchorPos(fruit.anchoredPosition + new Vector2(head.x * 0.7f, 0f), 1.2f).SetEase(Ease.InOutSine));
                seq.Append(fruit.DOJumpAnchorPos(fruit.anchoredPosition + head, 0.08f * h, 1, 0.35f));
                seq.AppendCallback(() =>
                {
                    zombie.Squash(0.2f);
                    Bubble(zombie, IconPainter.Star, UiKit.Hex("E3A92B"), 0.8f, 0.09f);
                    Sfx.Play(SfxKind.Tap, 0.9f);
                });
                seq.Append(fruit.DOJumpAnchorPos(fruit.anchoredPosition + head + new Vector2(0.1f * w, -0.02f * h), 0.06f * h, 1, 0.5f));
                Track(seq);
                yield return Wait(2.2f);
            }

            // Out, warm and rosy, with a good shake.
            zombie.Sink(0f, 0.3f);
            zombie.JumpTo(Out, 0.16f, 0.6f);
            yield return Wait(0.65f);
            zombie.Tint(Color.white, 0.6f);
            zombie.Shake(0.5f);
            Burst(Out + new Vector2(0f, 0.12f), UiKit.Circle, UiKit.Hex("BFEAF0"), 18, 0.18f, 0.016f, 0.6f);
            yield return Wait(0.6f);
            zombie.Hop(3, 0.2f, 0.32f);
            Bubble(zombie, IconPainter.Heart, UiKit.Hex("F2668B"), 1.4f);
            Sfx.Play(SfxKind.Heart, 0.8f);
            Title("Warmed right up!", "Ready to work for you", new Vector2(0.5f, 0.82f));
            yield return Wait(2.6f);
        }

        IEnumerator Steam()
        {
            while (true)
            {
                Puff(new Vector2(UnityEngine.Random.Range(0.28f, 0.6f), WaterY + 0.03f), new Color(1f, 1f, 1f, 0.32f), 0.07f, 0.2f, 2.2f);
                yield return Wait(0.35f);
            }
        }
    }
}
