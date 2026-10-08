using System;
using System.Collections;
using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// The first worker to run out of puff, as a little movie — and the lesson that goes with it.
    /// It chops away at a log, slower… and slower… yawns, keels over and snores, dreaming of
    /// flowers. Old Yuzu floats over and gives it a poke. It snores louder.
    /// </summary>
    public class NapCutscene : Cutscene
    {
        static readonly Vector2 WorkerAt = new(0.42f, 0.14f), LogAt = new(0.58f, 0.29f);

        public static void Show(Action done = null) => Play<NapCutscene>(done);

        static Sprite Icon(string name)
        {
            var catalog = ItemCatalog.Instance;
            return catalog != null && catalog.Has(name) ? catalog.Icon(name) : null;
        }

        protected override IEnumerator Script()
        {
            Backdrop(Resources.Load<Sprite>("Cutscenes/Cutscene_Mill"), UiKit.Hex("BFE3F2"), UiKit.Hex("8DBA5A"));
            var art = Icon("Capybara");
            if (art == null) yield break;
            var worker = Cast(art, WorkerAt, 0.26f, faceRight: true);
            var chip = UiKit.Hex("C99A63");

            // Chop, chop, chop… chop…… chop.
            float[] gaps = { 0.45f, 0.45f, 0.5f, 0.65f, 0.9f, 1.25f };
            foreach (var gap in gaps)
            {
                worker.Lean(-16f, gap * 0.25f);
                yield return Wait(gap * 0.3f);
                worker.Lean(8f, 0.08f);
                worker.Squash(0.08f);
                Burst(LogAt, UiKit.Rounded(4), chip, 4, 0.07f, 0.014f, 0.5f, Vector2.up);
                Sfx.Play(SfxKind.Tap, 0.5f);
                yield return Wait(gap * 0.7f);
            }
            worker.Lean(0f, 0.3f);
            yield return Wait(0.4f);

            // A big yawn…
            Bubble(worker, IconPainter.FaceWow, UiKit.Hex("8A7AB8"), 1f);
            worker.Squash(-0.1f);
            yield return Wait(1.1f);

            // …and over it goes, backwards, away from the log.
            Track(worker.Body.DOLocalRotate(new Vector3(0f, 0f, 75f), 0.9f).SetEase(Ease.OutBounce).SetUpdate(true));
            yield return Wait(1f);

            // Snoring, and dreaming of flowers.
            var flowers = Icon("Small Posie Garden");
            StartCoroutine(Snore(worker));
            yield return Wait(0.6f);
            if (flowers != null) Bubble(worker, flowers, Color.white, 2.2f, 0.12f);
            yield return Wait(1.6f);

            // Old Yuzu comes to see what's the matter.
            var ghostArt = Icon("Ghost");
            if (ghostArt != null)
            {
                var yuzu = Cast(ghostArt, new Vector2(-0.12f, 0.14f), 0.22f, tint: new Color(0.85f, 0.95f, 1f, 0.9f));
                yuzu.Bob();
                yuzu.WalkTo(new Vector2(0.27f, 0.13f), 1.2f, 1);
                yield return Wait(1.3f);
                yuzu.Face(true);
                // A poke.
                yuzu.Nudge(0.04f, 0.15f);
                yield return Wait(0.18f);
                yuzu.Nudge(-0.04f, 0.2f);
                worker.Shake(0.3f);
                yield return Wait(0.3f);
                // An even bigger snore.
                Zed(worker, 1.8f, 0.45f);
                yield return Wait(0.6f);
                yuzu.Lean(12f, 0.3f);
                Bubble(yuzu, flowers != null ? flowers : IconPainter.Heart, Color.white, 2.4f, 0.12f);
            }

            Title("Hungry workers nap", "Flowers wake them up!", new Vector2(0.5f, 0.84f));
            yield return Wait(2.8f);
        }

        IEnumerator Snore(Actor sleeper)
        {
            while (true)
            {
                Zed(sleeper, 1f, 0.45f);
                Track(sleeper.Body.DOScaleY(1.06f, 0.6f).SetLoops(2, LoopType.Yoyo).SetUpdate(true));
                yield return Wait(1.2f);
            }
        }
    }
}
