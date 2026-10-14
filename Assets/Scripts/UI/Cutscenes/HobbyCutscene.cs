using System.Collections;
using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.UI
{
    /// <summary>
    /// A new hobby, as a little movie: the gear drops into your capybara's paws with a sparkle,
    /// it has a first go (which never quite goes to plan), and then a card says how to do it
    /// for real.
    /// </summary>
    public class HobbyCutscene : Cutscene
    {
        Hobby _hobby;

        public static void Show(Hobby hobby)
        {
            var scene = Play<HobbyCutscene>(() => HowTo(hobby));
            if (scene != null) scene._hobby = hobby;
        }

        /// <summary>The little tutorial after the movie.</summary>
        static void HowTo(Hobby hobby)
        {
            var (title, body) = hobby switch
            {
                Hobby.Fishing => ("How to fish", "Walk to the water's edge where fish shadows swim and tap Start fishing.\nYour bobber goes straight in by the nearest fish. When it bites, hold to reel and let go when the line turns red!"),
                Hobby.Bugs => ("How to catch bugs", "Bugs settle on flowers, trees and grass. Walk close and tap one to swing your net.\nSome are shy, so don't run straight at them!"),
                Hobby.Birding => ("How to watch birds", "Birds land in trees and on the shore. Tap one from a few steps away to spot it through your binoculars.\nGet too close and it flies off!"),
                Hobby.Sewing => ("How to sew", "Put your sewing machine down, then tap it.\nA hat needs cotton from your garden. Decorate it, and it goes in your wardrobe!"),
                Hobby.Gardening => ("How to garden", "Buy seeds at the barn on Sunflower Farm, then drag them from your trail onto an empty plot.\nThey grow while you're away. Tap the plot to harvest! More plots and seeds open as your gardening level grows."),
                Hobby.Cooking => ("How to cook", "Drag ingredients into the pot, press Cook, and tap the spatula in time to keep the heat down.\nIslanders with a dish in a bubble will pay for it!"),
                _ => ("New hobby!", "Find it in Hobbies, in the menu."),
            };
            CardPopup.Show(title, body, Picture(hobby));
        }

        /// <summary>The hobby's own gear in full colour (an icon only where it has none).</summary>
        public static Sprite Picture(Hobby hobby)
        {
            var art = hobby switch
            {
                Hobby.Fishing => HobbyData.Art("Rod"), Hobby.Bugs => HobbyData.Art("Net"), Hobby.Birding => HobbyData.Art("Binoculars"),
                Hobby.Sewing => HobbyData.Art("SewingMachine"), Hobby.Gardening => HobbyData.Art("Plot"), Hobby.Cooking => HobbyData.Art("Stove"),
                Hobby.Writing => HobbyData.Art("Ema"), _ => null,
            };
            if (art != null) return art;
            return hobby switch
            {
                Hobby.Fishing => IconPainter.Rod, Hobby.Birding => IconPainter.Bird, Hobby.Writing => IconPainter.Book,
                Hobby.Sewing => IconPainter.Hat, Hobby.Cats => IconPainter.Heart, Hobby.Cooking => IconPainter.Flame, _ => IconPainter.Star,
            };
        }

        static string Name(Hobby h) => h switch
        {
            Hobby.Fishing => "Fishing", Hobby.Bugs => "Bug catching", Hobby.Birding => "Birdwatching", Hobby.Sewing => "Sewing",
            Hobby.Gardening => "Gardening", Hobby.Cooking => "Cooking", Hobby.Writing => "Writing", _ => "Cat care",
        };

        /// <summary>In its bubble: you, and the new hobby landing beside you.</summary>
        protected override Vector3 BubbleView => new(0.52f, 0.45f, 1.1f);

        protected override IEnumerator Script()
        {
            string backdrop = _hobby switch
            {
                Hobby.Fishing or Hobby.Birding => "Cutscene_Shore",
                Hobby.Sewing => "Cutscene_Wedding",
                Hobby.Gardening or Hobby.Cooking => "Cutscene_Farm",   // Sunflower Farm, where both live
                _ => "Cutscene_Mill",
            };
            Backdrop(Resources.Load<Sprite>("Cutscenes/" + backdrop), UiKit.Hex("BFE3F2"), UiKit.Hex("8DBA5A"));
            var you = CastPlayer(new Vector2(0.44f, 0.12f), 0.3f, faceRight: true);

            // The gear drops in with a sparkle.
            var gearArt = _hobby switch
            {
                Hobby.Fishing => HobbyData.Art("Rod"), Hobby.Bugs => HobbyData.Art("Net"), Hobby.Birding => HobbyData.Art("Binoculars"),
                Hobby.Sewing => HobbyData.Art("SewingMachine"), Hobby.Gardening => HobbyData.Art("Plot"), Hobby.Cooking => HobbyData.Art("Stove"),
                _ => null,
            };
            RectTransform gear = null;
            if (gearArt != null)
            {
                gear = Prop(gearArt, new Vector2(0.62f, 1.1f), _hobby == Hobby.Sewing || _hobby == Hobby.Cooking || _hobby == Hobby.Gardening ? 0.22f : 0.16f);
                var land = new Vector2(0.62f, _hobby == Hobby.Fishing || _hobby == Hobby.Bugs || _hobby == Hobby.Birding ? 0.3f : 0.17f);
                Track(gear.DOAnchorPos(gear.anchoredPosition + new Vector2(0f, (land.y - 1.1f) * Stage.rect.height), 0.6f).SetEase(Ease.OutBounce).SetUpdate(true));
            }
            yield return Wait(0.7f);
            Burst(new Vector2(0.62f, 0.3f), IconPainter.Sparkle, UiKit.Hex("FFE9A8"), 14, 0.14f, 0.035f);
            you.Hop(2, 0.16f, 0.3f);
            Bubble(you, IconPainter.FaceWow, UiKit.Hex("5B9BD5"), 0.9f);
            Sfx.Play(SfxKind.Chest, 0.7f);
            yield return Wait(1f);

            // A first go.
            switch (_hobby)
            {
                case Hobby.Fishing:
                {
                    var boot = Prop(HobbyData.Art("Gift_Boot"), new Vector2(0.75f, -0.1f), 0.12f);
                    Track(boot.DOJumpAnchorPos(boot.anchoredPosition + new Vector2(-0.22f * Stage.rect.width, 0.28f * Stage.rect.height), 0.12f * Stage.rect.height, 1, 0.8f).SetUpdate(true));
                    yield return Wait(0.85f);
                    you.Shake(0.4f);
                    Bubble(you, IconPainter.Star, UiKit.Hex("8A7563"), 1f);
                    break;
                }
                case Hobby.Bugs:
                {
                    var bug = Prop(HobbyData.Art("Bug_" + HobbyData.Bugs[0].Id), new Vector2(0.7f, 0.45f), 0.06f);
                    if (gear != null) Track(gear.DOLocalRotate(new Vector3(0f, 0f, 80f), 0.25f).SetLoops(2, LoopType.Yoyo).SetUpdate(true));
                    yield return Wait(0.3f);
                    // Missed — and it lands on your head.
                    Track(bug.DOAnchorPos(bug.anchoredPosition + new Vector2(-0.27f * Stage.rect.width, -0.03f * Stage.rect.height), 0.7f).SetEase(Ease.InOutSine).SetUpdate(true));
                    yield return Wait(0.8f);
                    Bubble(you, IconPainter.Heart, UiKit.Hex("F2668B"), 1f);
                    break;
                }
                case Hobby.Birding:
                {
                    var bird = Prop(HobbyData.BirdPicture(HobbyData.Birds[0]), new Vector2(0.95f, 0.7f), 0.07f);
                    if (gear != null) Track(gear.DOAnchorPos(gear.anchoredPosition + new Vector2(0.06f * Stage.rect.width, 0f), 0.4f).SetLoops(2, LoopType.Yoyo).SetUpdate(true));
                    Track(bird.DOJumpAnchorPos(bird.anchoredPosition + new Vector2(-0.52f * Stage.rect.width, -0.28f * Stage.rect.height), 0.1f * Stage.rect.height, 2, 1.1f).SetUpdate(true));
                    yield return Wait(1.2f);
                    Bubble(you, IconPainter.Bird, UiKit.Hex("E3A92B"), 1f);
                    break;
                }
                case Hobby.Sewing:
                    Confetti(40, 2f);
                    you.Shake(0.5f);
                    yield return Wait(0.8f);
                    you.Hop(2, 0.2f, 0.3f);
                    Bubble(you, IconPainter.Hat, UiKit.Hex("C2577F"), 1f);
                    break;
                case Hobby.Gardening:
                {
                    var sprout = Prop(HobbyData.Art("Sprout"), new Vector2(0.62f, 0.24f), 0.08f);
                    sprout.localScale = Vector3.zero;
                    Track(sprout.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetUpdate(true));
                    yield return Wait(0.7f);
                    var carrot = Prop(HobbyData.Art("Crop_Carrot"), new Vector2(0.62f, 0.3f), 0.1f);
                    carrot.localScale = Vector3.zero;
                    Track(sprout.DOScale(0f, 0.2f).SetUpdate(true));
                    Track(carrot.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true));
                    Burst(new Vector2(0.62f, 0.3f), IconPainter.Sparkle, UiKit.Hex("FFE9A8"), 8, 0.08f, 0.03f);
                    yield return Wait(0.5f);
                    Bubble(you, IconPainter.Heart, UiKit.Hex("F2668B"), 1f);
                    break;
                }
                case Hobby.Cooking:
                {
                    Puff(new Vector2(0.62f, 0.4f), new Color(0.3f, 0.3f, 0.3f, 0.8f), 0.08f, 0.15f, 1.2f);
                    var bad = Prop(HobbyData.Art("Dish_Bizarre"), new Vector2(0.62f, 0.5f), 0.1f);
                    you.Shake(0.4f);
                    yield return Wait(0.9f);
                    Track(bad.DOScale(0f, 0.2f).SetUpdate(true));
                    var soup = Prop(HobbyData.ItemArt("dish:CarrotSoup"), new Vector2(0.62f, 0.5f), 0.1f);
                    soup.localScale = Vector3.zero;
                    Track(soup.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true));
                    yield return Wait(0.4f);
                    Bubble(you, IconPainter.Star, UiKit.Hex("E3A92B"), 1f);
                    break;
                }
            }
            yield return Wait(0.5f);
            Title("New hobby: " + Name(_hobby) + "!", "Let's give it a go", new Vector2(0.5f, 0.82f));
            yield return Wait(2.2f);
        }
    }
}
