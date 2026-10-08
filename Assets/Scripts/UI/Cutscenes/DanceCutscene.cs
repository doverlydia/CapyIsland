using System;
using System.Collections;
using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// Captain Wasabi's Hot Pepper Hornpipe, as a little movie (the original had one for its pirate
    /// girl too). She dances on the harbour stage in her new sparkles, you and the workers clap
    /// along — then the big finishing spin carries her right off the stage to the edge of the
    /// dock. There is a wobble. There is very nearly a splash. A fish leaps to see. She sticks the
    /// landing anyway.
    /// </summary>
    public class DanceCutscene : Cutscene
    {
        public static void Show(Action done) => Play<DanceCutscene>(done);

        static Sprite Icon(string name)
        {
            var catalog = ItemCatalog.Instance;
            return catalog != null && catalog.Has(name) ? catalog.Icon(name) : null;
        }

        protected override IEnumerator Script()
        {
            Backdrop(Resources.Load<Sprite>("Cutscenes/Cutscene_Dance"), UiKit.Hex("F4D79B"), UiKit.Hex("A9744A"));

            var captainArt = Icon("Pirate Peach");
            if (captainArt == null) yield break;
            var captain = Cast(captainArt, new Vector2(0.67f, 0.36f), 0.27f);

            // The audience, on the dock.
            var crowd = new List<Actor>();
            var worker = Icon("Capybara");
            if (worker != null) crowd.Add(Cast(worker, new Vector2(0.1f, 0.16f), 0.22f, faceRight: true));
            var you = CastPlayer(new Vector2(0.26f, 0.1f), 0.27f, faceRight: true);
            crowd.Add(you);
            if (worker != null) crowd.Add(Cast(worker, new Vector2(0.42f, 0.08f), 0.23f, faceRight: true));

            yield return Wait(0.5f);

            // A bow, and a flash of the new sparkly costume.
            captain.Lean(-14f, 0.3f);
            yield return Wait(0.35f);
            captain.Lean(0f, 0.25f);
            Bubble(captain, IconPainter.Sparkle, UiKit.Hex("E3A92B"), 0.9f);
            SparkleBurst(captain, 14);
            yield return Wait(0.6f);

            // The hornpipe: hop, turn, hop, turn — with the crowd clapping along.
            for (int beat = 0; beat < 8; beat++)
            {
                captain.Face(beat % 2 == 1);
                captain.Hop(1, 0.22f, 0.36f);
                captain.Lean(beat % 2 == 0 ? 10f : -10f, 0.18f);
                var fan = crowd[beat % crowd.Count];
                fan.Hop(1, 0.12f, 0.3f);
                if (beat % 3 == 0) Bubble(fan, beat % 2 == 0 ? IconPainter.Star : IconPainter.Heart, beat % 2 == 0 ? UiKit.Hex("E3A92B") : UiKit.Hex("F2668B"), 0.6f, 0.08f);
                if (beat % 2 == 0) Sfx.Play(SfxKind.Tap, 0.6f);
                yield return Wait(0.38f);
            }
            captain.Lean(0f, 0.15f);

            // The big finish: a spin so big it carries her right off the stage…
            for (int i = 0; i < 6; i++)
            {
                captain.Face(i % 2 == 0);
                yield return Wait(0.07f);
            }
            captain.WalkTo(new Vector2(0.6f, 0.08f), 0.55f, 2);
            yield return Wait(0.6f);
            captain.Face(false);

            // …to the very edge of the dock. Wobble. Wobble.
            foreach (var fan in crowd) Bubble(fan, IconPainter.FaceWow, UiKit.Hex("2F5F99"), 1.1f, 0.11f);
            for (int i = 0; i < 4; i++)
            {
                captain.Lean(i % 2 == 0 ? -22f : 16f, 0.2f);
                yield return Wait(0.22f);
            }

            // A fish leaps to have a look.
            LeapingFish();
            yield return Wait(0.35f);

            // She sticks the landing anyway.
            captain.Lean(0f, 0.15f);
            captain.Squash(0.18f);
            Bubble(captain, IconPainter.Star, UiKit.Hex("E3A92B"), 1.4f, 0.12f);
            SparkleBurst(captain, 20);
            yield return Wait(0.5f);

            Confetti(55, 3f);
            Sfx.Play(SfxKind.Chest, 0.7f);
            foreach (var fan in crowd) fan.Hop(3, 0.16f, 0.34f);
            Title("The Hot Pepper Hornpipe!", "Bravo, Captain!", new Vector2(0.5f, 0.82f));
            yield return Wait(2.8f);
        }

        void SparkleBurst(Actor around, int count)
        {
            var colours = new[] { UiKit.Hex("FFD36E"), UiKit.Hex("9ED9F2"), UiKit.Hex("F7A8C4"), Color.white };
            var centre = around.Root.anchorMin + new Vector2(0f, around.Root.sizeDelta.y * 0.5f / Stage.rect.height);
            for (int i = 0; i < count; i++)
            {
                var r = Prop(IconPainter.Sparkle, centre, UnityEngine.Random.Range(0.025f, 0.045f), colours[i % colours.Length]);
                var to = r.anchoredPosition + UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.08f, 0.18f) * Stage.rect.height;
                Track(r.DOAnchorPos(to, 0.7f).SetEase(Ease.OutCubic).SetUpdate(true));
                Track(r.DOScale(0f, 0.7f).SetEase(Ease.InQuad).SetUpdate(true));
            }
        }

        void LeapingFish()
        {
            var kinds = HobbyData.Fish;
            if (kinds == null || kinds.Length == 0) return;
            var fish = Prop(HobbyData.FishPicture(kinds[0]), new Vector2(0.5f, 0.0f), 0.08f);
            float h = Stage.rect.height;
            var start = fish.anchoredPosition;
            var seq = DOTween.Sequence().SetUpdate(true);
            seq.Append(fish.DOJumpAnchorPos(start + new Vector2(0.1f * Stage.rect.width, 0f), 0.22f * h, 1, 0.9f));
            seq.Join(fish.DOLocalRotate(new Vector3(0f, 0f, -200f), 0.9f, RotateMode.FastBeyond360));
            seq.AppendCallback(() => Sfx.Play(SfxKind.Plop, 0.8f));
            seq.Append(fish.DOScale(0f, 0.1f));
            Track(seq);
            Sfx.Play(SfxKind.Plop, 0.6f);
        }
    }
}
