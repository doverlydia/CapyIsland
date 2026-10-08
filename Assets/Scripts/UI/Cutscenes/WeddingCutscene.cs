using System;
using System.Collections;
using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// The wedding, as a little movie (the original played one too). You and Miso meet under the
    /// blossom arch, Elder Matcha says the words, the two of you kiss, and the guests — your workers
    /// and Captain Wasabi — cheer. The Captain has rather a soft spot for you, so she sulks under a
    /// little rain cloud first… then gives in and cheers loudest of all.
    /// </summary>
    public class WeddingCutscene : Cutscene
    {
        bool _renewal;

        /// <summary>Plays the wedding (or a vow renewal); <paramref name="done"/> runs when it is over.</summary>
        public static void Show(bool renewal, Action done)
        {
            var scene = Play<WeddingCutscene>(done);
            if (scene != null) scene._renewal = renewal;
        }

        static Sprite Icon(string name)
        {
            var catalog = ItemCatalog.Instance;
            return catalog != null && catalog.Has(name) ? catalog.Icon(name) : null;
        }

        protected override IEnumerator Script()
        {
            Backdrop(Resources.Load<Sprite>("Cutscenes/Cutscene_Wedding"), UiKit.Hex("F6C9A8"), UiKit.Hex("A7C46A"));

            // Elder Matcha under the arch; the couple will meet in front of it.
            var priestArt = GameArt.Instance != null ? GameArt.Instance.priest : null;
            var elder = priestArt != null ? Cast(priestArt, new Vector2(0.5f, 0.35f), 0.2f) : null;

            // The couple first, so the guests in the front row stand in front of them.
            var you = CastPlayer(new Vector2(-0.12f, 0.2f), 0.28f, faceRight: true);
            // In everything you gave her (the church, and so the wedding, comes after the tiara).
            var misoArt = Game.HobbyData.Art("Miso_DressTiara") ?? Game.HobbyData.Art("Miso_Dress") ?? Icon("Coriander");
            var miso = misoArt != null ? Cast(misoArt, new Vector2(1.12f, 0.2f), 0.28f) : null;

            var worker = Icon("Capybara");
            var guests = new List<Actor>();
            if (worker != null)
            {
                guests.Add(Cast(worker, new Vector2(0.17f, 0.02f), 0.21f, faceRight: true));
                guests.Add(Cast(worker, new Vector2(0.06f, -0.02f), 0.24f, faceRight: true));
                guests.Add(Cast(worker, new Vector2(0.95f, -0.02f), 0.23f));
            }
            var captainArt = Icon("Pirate Peach");
            var captain = captainArt != null ? Cast(captainArt, new Vector2(0.81f, 0.0f), 0.26f) : null;

            // Guests turn to watch.
            foreach (var g in guests) g.Squash(0.06f);
            yield return Wait(0.5f);

            // The couple come in from either side and meet under the arch.
            you.WalkTo(new Vector2(0.39f, 0.2f), 1.8f, 6);
            miso?.WalkTo(new Vector2(0.61f, 0.2f), 1.8f, 6);
            yield return Wait(1.9f);
            you.Face(true);
            miso?.Face(false);

            // The Elder says the words: a pair of rings.
            if (elder != null)
            {
                elder.Squash(0.08f);
                Bubble(elder, IconPainter.Ring, UiKit.Hex("E3A92B"), 1.1f);
            }
            yield return Wait(0.8f);

            // Meanwhile, in the front row… a small, very personal rain cloud.
            RectTransform cloud = null;
            if (captain != null)
            {
                cloud = Bubble(captain, IconPainter.Cloud, UiKit.Hex("7B8794"), 0f, 0.12f);
                captain.Shake(0.7f);
            }
            yield return Wait(0.8f);

            // The kiss.
            you.Nudge(0.05f, 0.35f);
            miso?.Nudge(-0.05f, 0.35f);
            you.Lean(9f, 0.35f);
            miso?.Lean(9f, 0.35f);
            yield return Wait(0.35f);
            var heart = Prop(IconPainter.Heart, new Vector2(0.5f, 0.55f), 0.15f, UiKit.Hex("F2668B"));
            heart.localScale = Vector3.zero;
            Track(heart.DOScale(1f, 0.45f).SetEase(Ease.OutBack).SetUpdate(true));
            Sfx.Play(SfxKind.Heart, 0.9f);
            yield return Wait(0.9f);
            you.Lean(0f, 0.3f);
            miso?.Lean(0f, 0.3f);

            // Everyone cheers.
            Confetti(70, 3.2f);
            Sfx.Play(SfxKind.Chest, 0.7f);
            foreach (var g in guests) g.Hop(4, 0.16f, 0.36f);
            yield return Wait(0.3f);
            for (int i = 0; i < guests.Count; i++)
                Bubble(guests[i], i % 2 == 0 ? IconPainter.Star : IconPainter.Heart, i % 2 == 0 ? UiKit.Hex("E3A92B") : UiKit.Hex("F2668B"), 1f, 0.09f);
            you.Hop(2, 0.14f, 0.34f);
            miso?.Hop(2, 0.14f, 0.34f);
            yield return Wait(0.9f);

            // The Captain sighs… and gives in, loudest of all.
            if (captain != null)
            {
                if (cloud != null)
                {
                    Track(cloud.DOScale(0f, 0.25f).SetEase(Ease.InBack).SetUpdate(true));
                    cloud = null;
                }
                captain.Squash(0.15f);
                yield return Wait(0.3f);
                Bubble(captain, IconPainter.Heart, UiKit.Hex("F2668B"), 1.4f, 0.12f);
                captain.Hop(4, 0.3f, 0.34f);
            }

            yield return Wait(0.4f);
            Title(_renewal ? "Vows renewed!" : "Just married!", "You & " + StoryText.Sweetheart, new Vector2(0.5f, 0.8f));
            yield return Wait(2.6f);
        }
    }
}
