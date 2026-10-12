using System;
using System.Collections;
using System.Collections.Generic;
using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The opening movie, before a new game: the Capy Isles when they were warm (capybaras soaking
    /// in the hot spring under the Steam Tower), the Big Chill freezing it all, Old Yuzu pouring
    /// the last of her magic into bubbles that carry what she can save away over the sea — and
    /// staying behind herself, growing old by the frozen spring until her spirit rises as the ghost
    /// who narrates all this — then a little capybara asleep in a bath tub, drifting under
    /// the stars until the tide brings it ashore, where it finds a crown in the waves.
    ///
    /// Plays once per new game (Start over and Delete Save bring it back); Settings can replay it.
    /// Tap to skip, like every movie.
    /// </summary>
    public class IntroCutscene : Cutscene
    {
        const string SeenKey = "CapyIsland.IntroSeen";

        public static bool Seen => PlayerPrefs.GetInt(SeenKey, 0) == 1;

        public static void Forget() => PlayerPrefs.DeleteKey(SeenKey);

        /// <summary>Plays the opening; <paramref name="done"/> runs when it ends (or is skipped).</summary>
        public static void Show(Action done = null)
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            IntroCurtain.Drop();   // black behind it all, in case it was started some other way
            Play<IntroCutscene>(() => { IntroAudio.Stop(2f); IntroCurtain.Lift(); done?.Invoke(); });
        }

        /// <summary>Out of the dark and back into it; the island then fades up from black (IntroCurtain).</summary>
        protected override Color Edge => Color.black;

        /// <summary>Waits on every line, so nobody misses the story; tap or Next to go on.</summary>
        protected override bool ReaderPaced => true;


        Image _narrator;

        /// <summary>
        /// Old Yuzu's ghost, floating at the bubble's edge while she tells it; she bobs as each
        /// line begins.
        /// </summary>
        void Narrator(Sprite ghost)
        {
            if (ghost == null || Frame == null) return;
            _narrator = UiKit.Image(Frame, "Narrator", ghost, new Color(1f, 1f, 1f, 0f));
            _narrator.preserveAspect = true;
            _narrator.raycastTarget = false;
            var r = _narrator.rectTransform;
            r.anchorMin = new Vector2(-0.05f, -0.06f);
            r.anchorMax = new Vector2(0.25f, 0.24f);
            r.offsetMin = r.offsetMax = Vector2.zero;
            Track(_narrator.DOFade(0.92f, 1.2f).SetUpdate(true));
            Track(r.DOAnchorPosY(14f, 1.7f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true));
        }

        protected override void Narrating()
        {
            if (_narrator == null || _narrator.color.a < 0.2f) return;
            var r = _narrator.rectTransform;
            r.DOKill(true);
            r.localScale = Vector3.one;
            Track(r.DOPunchScale(new Vector3(0.08f, -0.06f, 0f), 0.45f, 6).SetUpdate(true));
            Track(r.DOAnchorPosY(14f, 1.7f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true));
        }

        static Sprite Art(string path) => Resources.Load<Sprite>(path);

        static Sprite Capy(string name)
        {
            var catalog = ItemCatalog.Instance;
            if (catalog != null && catalog.Has(name)) return catalog.Icon(name);
            return null;
        }

        /// <summary>The glass of a real island bubble, so the magic bubbles match the ones you pop.</summary>
        static Sprite BubbleGlass()
        {
            foreach (var b in FindObjectsByType<Bubble>(FindObjectsSortMode.None))
            {
                var sr = b.transform.Find("Sprite")?.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null) return sr.sprite;
            }
            return IconPainter.Ring;
        }

        readonly List<Coroutine> _loops = new();

        protected override IEnumerator Script()
        {
            var lines = (string[])StoryText.Intro.Clone();
            var you = PlayerProfile.Name;
            var idle = Wardrobe.Ready ? Wardrobe.BaseSprite : Capy("Capybara");
            var miso = Capy("Coriander");
            var yuzuAlive = Art("Cutscenes/Cutscene_IntroYuzu");
            var ghost = GhostDialogue.Instance != null ? GhostDialogue.Instance.Face : null;   // the ghost you will meet
            var glass = BubbleGlass();

            // Each memory is framed inside its bubble (Aim): the bubble is round and the pictures
            // are wide, so everyone stands well inside it and the view pans to follow the story.

            // ---------------------------------------------------------------- 1. the warm isles
            IntroAudio.Play("intro_warm", 1.2f);
            Aim(0.34f, 0.42f, 1.2f);
            Backdrop(Art("Cutscenes/Cutscene_IntroWarm"), UiKit.Hex("9FD3EA"), UiKit.Hex("F2D49A"));
            Narrator(ghost);
            Caption(lines[0]);
            _loops.Add(StartCoroutine(Steam(new Vector2(0.3f, 0.26f), 0.16f, 0.42f)));
            _loops.Add(StartCoroutine(Steam(new Vector2(0.555f, 0.8f), 0.01f, 0.9f)));

            var soakA = Cast(idle, new Vector2(0.2f, 0.21f), 0.17f, faceRight: true);
            soakA.Sink(0.5f, 0.01f); soakA.Bob(0.03f, 1.8f); Foam(soakA);
            var soakB = Cast(idle, new Vector2(0.38f, 0.2f), 0.17f);
            soakB.Sink(0.5f, 0.01f); soakB.Bob(0.03f, 2.1f); Foam(soakB);
            var misoA = miso != null ? Cast(miso, new Vector2(0.8f, 0.17f), 0.22f) : null;
            var kid = Cast(idle, new Vector2(0.85f, 0.12f), 0.13f);
            var yuzu = yuzuAlive != null ? Cast(yuzuAlive, new Vector2(0.74f, 0.19f), 0.26f) : null;
            misoA?.WalkTo(new Vector2(0.53f, 0.17f), 1.8f, 5);
            kid.WalkTo(new Vector2(0.45f, 0.12f), 2.2f, 7);
            yield return Wait(2.4f);
            kid.Hop(3, 0.25f, 0.3f);
            if (misoA != null) Bubble(misoA, IconPainter.Heart, UiKit.Hex("F2668B"), 1.4f);
            Bubble(soakB, IconPainter.FaceSmile, UiKit.Hex("E3A92B"), 1.3f);
            yield return Wait(1.4f);
            yield return Next();

            // Everyone came to soak: in jumps the little one, splash!
            Caption(lines[1]);
            kid.Face(false);
            kid.JumpTo(new Vector2(0.3f, 0.22f), 0.14f, 0.55f);
            yield return Wait(0.55f);
            kid.Sink(0.5f, 0.2f); kid.Bob(0.03f, 1.6f); Foam(kid);
            Sfx.Play(SfxKind.Plop, 0.7f);
            Burst(new Vector2(0.3f, 0.25f), UiKit.Circle, UiKit.Hex("BFEAF0"), 12, 0.1f, 0.016f, 0.6f, Vector2.up);
            Bubble(soakA, IconPainter.FaceLaugh, UiKit.Hex("E3A92B"), 1.2f);
            yield return Wait(0.9f);
            // ...and over by the bathhouse, Old Yuzu, keeper of it all.
            Aim(0.6f, 0.42f, 1.2f, 2.2f);
            yield return Wait(2.2f);
            if (yuzu != null) Bubble(yuzu, IconPainter.Heart, UiKit.Hex("F2668B"), 1.4f);
            yield return Wait(1.2f);
            yield return Next();

            // ---------------------------------------------------------------- 2. the Big Chill
            Caption(lines[2]);
            IntroAudio.Play("intro_chill", 2.2f);
            IntroAudio.Sfx("Wind", 0.9f);
            yield return Wait(1.6f);
            yield return Next();
            StopLoops();
            IntroAudio.Sfx("Freeze", 0.8f);
            Actor shiverA = null, shiverB = null, misoCold = null, yuzuCold = null;
            yield return Change(new Color(0.78f, 0.86f, 1f), 1.4f, () =>
            {
                Aim(0.5f, 0.42f, 1.2f);
                Backdrop(Art("Cutscenes/Cutscene_IntroFrozen"), UiKit.Hex("8C9BB5"), UiKit.Hex("DDE5EE"));
                var cold = new Color(0.72f, 0.8f, 0.95f);
                shiverA = Cast(idle, new Vector2(0.35f, 0.17f), 0.17f, faceRight: true, tint: cold);
                shiverB = Cast(idle, new Vector2(0.44f, 0.15f), 0.17f, tint: cold);
                misoCold = miso != null ? Cast(miso, new Vector2(0.54f, 0.15f), 0.22f, tint: cold) : null;
                yuzuCold = yuzuAlive != null ? Cast(yuzuAlive, new Vector2(0.64f, 0.17f), 0.27f, tint: new Color(0.85f, 0.88f, 0.98f)) : null;
                _loops.Add(StartCoroutine(Snow()));
            });
            Caption(lines[3]);
            shiverA.Shake(1.2f); shiverB.Shake(1.2f); misoCold?.Shake(1.2f);
            Bubble(shiverB, IconPainter.Cloud, UiKit.Hex("8FB3D9"), 1.2f);
            yield return Wait(1.6f);
            // One by one, off into the cold.
            shiverA.WalkTo(new Vector2(-0.2f, 0.17f), 2f, 6);
            yield return Wait(0.6f);
            shiverB.WalkTo(new Vector2(-0.2f, 0.15f), 2.2f, 6);
            yield return Wait(2.2f);
            yield return Next();

            // ---------------------------------------------------------------- 3. Old Yuzu's magic
            Caption(lines[4]);
            IntroAudio.Play("intro_magic", 1.6f);
            if (yuzuCold != null)
            {
                yuzuCold.WalkTo(new Vector2(0.48f, 0.16f), 1.6f, 4);
                yield return Wait(1.7f);
                yuzuCold.Tint(new Color(1f, 0.92f, 0.7f), 0.8f);
                var glow = Prop(SoftLight, new Vector2(0.48f, 0.3f), 0.6f, new Color(1f, 0.85f, 0.45f, 0f));
                glow.SetSiblingIndex(yuzuCold.Root.GetSiblingIndex());
                var glowImg = glow.GetComponent<Image>();
                Track(glowImg.DOFade(0.7f, 1f).SetUpdate(true));
                Track(glow.DOScale(1.25f, 0.9f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true));
                Flash(new Color(1f, 0.9f, 0.6f, 0.8f), 1f);
                IntroAudio.Sfx("Magic", 0.9f);
                Burst(new Vector2(0.48f, 0.3f), IconPainter.Star, UiKit.Hex("FFE08A"), 18, 0.25f, 0.025f, 1f);
                yield return Wait(1.2f);
                yield return Next();

                Caption(lines[5]);
                // Bubbles of warm air carry away what she can save — Miso among them.
                var keep = new[] { Capy("General Store"), Capy("Medium Pine Forest"), Capy("Capy House"), Capy("Medium Posie Garden"), Capy("Sawmill") };
                float[] xs = { 0.3f, 0.4f, 0.55f, 0.65f, 0.75f };
                for (int i = 0; i < keep.Length; i++)
                {
                    FloatAway(glass, keep[i], new Vector2(0.48f, 0.26f), new Vector2(xs[i], 1.15f), 3.4f + i * 0.2f, 0.15f, i * 0.25f);
                    if (i % 2 == 0) IntroAudio.Sfx("Bubble", 0.6f);
                }
                if (misoCold != null)
                {
                    yield return Wait(0.6f);
                    var bubble = Prop(glass, misoCold.Root.anchorMin + new Vector2(0f, 0.12f), 0.32f, new Color(1f, 1f, 1f, 0.8f));
                    bubble.localScale = Vector3.zero;
                    Track(bubble.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetUpdate(true));
                    yield return Wait(0.55f);
                    var up = new Vector2(0.7f, 1.3f);
                    float w = Stage.rect.width, h = Stage.rect.height;
                    var shift = new Vector2((up.x - misoCold.Root.anchorMin.x) * w, (up.y - misoCold.Root.anchorMin.y) * h);
                    Track(misoCold.Root.DOAnchorPos(misoCold.Root.anchoredPosition + shift, 3.6f).SetEase(Ease.InSine).SetUpdate(true));
                    Track(bubble.DOAnchorPos(bubble.anchoredPosition + shift, 3.6f).SetEase(Ease.InSine).SetUpdate(true));
                    misoCold.Tint(Color.white, 1f);
                }
                yield return Wait(2.4f);
                yield return Next();

                // Her magic spent, and the last bubble gone: none is left for her.
                Track(glowImg.DOFade(0f, 1.2f).SetUpdate(true));
                yuzuCold.Tint(new Color(0.85f, 0.88f, 0.98f), 1.2f);
                Caption(lines[6]);
                IntroAudio.Sfx("Fade", 0.5f);
                yield return Wait(1.5f);
                yield return Next();
            }

            // ---------------------------------------------------------------- 4. the keeper stays
            yield return Keeper(lines, ghost);

            // ---------------------------------------------------------------- 5. the years go by
            // The same spot by the spring, empty: the snow stops, it all thaws into the green warm
            // island of the start, and her sleeping bubble drifts across the sky over it.
            StopLoops();
            Image thaw = null;
            Actor drifting = null;
            yield return Change(UiKit.Hex("10162A"), 1.6f, () =>
            {
                Aim(0.5f, 0.45f, 1.15f);
                Backdrop(Art("Cutscenes/Cutscene_IntroFrozen"), UiKit.Hex("8C9BB5"), UiKit.Hex("DDE5EE"));
                thaw = UiKit.Image(Stage, "Thaw", Art("Cutscenes/Cutscene_IntroWarm"), new Color(1f, 1f, 1f, 0f));
                thaw.raycastTarget = false;
                UiKit.Stretch(thaw.rectTransform);
                _loops.Add(StartCoroutine(Snow(0.1f)));
                drifting = SleepingBubble(glass, ghost, new Vector2(0.28f, 0.58f), 0.16f);
            });
            Caption(lines[14]);
            if (drifting != null)
            {
                float dw = Stage.rect.width;
                Track(drifting.Root.DOAnchorPosX(drifting.Root.anchoredPosition.x + 0.45f * dw, 10f).SetEase(Ease.Linear).SetUpdate(true));
                drifting.Bob(0.05f, 2.2f);
                Zed(drifting, 0.7f, 0.9f);
            }
            yield return Wait(1.6f);
            StopLoops();   // the last snow
            Track(thaw.DOFade(1f, 4.5f).SetEase(Ease.InOutSine).SetUpdate(true));
            IntroAudio.Sfx("Sparkle", 0.4f);
            yield return Wait(2.6f);
            _loops.Add(StartCoroutine(Steam(new Vector2(0.3f, 0.26f), 0.16f, 0.42f)));
            if (drifting != null) Zed(drifting, 0.7f, 0.9f);
            yield return Wait(2.4f);
            yield return Next();

            // ---------------------------------------------------------------- 6. adrift
            StopLoops();
            Actor sleeper = null;
            yield return Change(UiKit.Hex("10162A"), 1.6f, () =>
            {
                Aim(0.64f, 0.5f, 1f);
                Backdrop(Art("Cutscenes/Cutscene_IntroSea"), UiKit.Hex("1B2546"), UiKit.Hex("21405A"));
                // Curled up inside the tub, its front wall hiding all but a sleepy head.
                sleeper = Cast(Art("Cutscenes/Cutscene_IntroTubSleeper"), new Vector2(0.72f, 0.16f), 0.24f);
                for (int i = 0; i < 4; i++)
                    FloatAway(glass, null, new Vector2(0.3f + i * 0.14f, 0.45f + (i % 2) * 0.08f), new Vector2(0.35f + i * 0.12f, 1.2f), 7f, 0.07f, i * 0.8f);
            });
            IntroAudio.Play("intro_sea", 2f);
            IntroAudio.Sfx("Waves", 0.7f);
            Caption(string.Format(lines[15], you));
            // The bubble drifts along with the tub, under the moon.
            float sw = Stage.rect.width;
            Track(sleeper.Root.DOAnchorPosX(sleeper.Root.anchoredPosition.x - 0.42f * sw, 7f).SetEase(Ease.Linear).SetUpdate(true));
            Aim(0.38f, 0.5f, 1f, 7f);
            Track(sleeper.Body.DOLocalRotate(new Vector3(0f, 0f, 5f), 1.4f).From(new Vector3(0f, 0f, -5f)).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true));
            for (int i = 0; i < 6; i++)
            {
                Zed(sleeper, 0.8f, 0.7f);
                yield return Wait(1.1f);
            }
            yield return Next();

            // ---------------------------------------------------------------- 7. the shore, and a crown
            Actor king = null;
            RectTransform tubAshore = null;
            yield return Change(UiKit.Hex("FFD9B0"), 1.6f, () =>
            {
                Aim(0.52f, 0.44f, 1.15f);
                Backdrop(Art("Cutscenes/Cutscene_Shore"), UiKit.Hex("F6C79A"), UiKit.Hex("E6B87A"));
                tubAshore = Prop(Art("Cutscenes/Cutscene_IntroTub"), new Vector2(0.62f, 0.2f), 0.18f);
                tubAshore.localRotation = Quaternion.Euler(0f, 0f, -14f);
                king = Cast(idle, new Vector2(0.44f, 0.14f), 0.2f, faceRight: true);
                king.Lean(-80f, 0.01f);   // fast asleep on the sand
            });
            IntroAudio.Play("intro_dawn", 2f);
            IntroAudio.Sfx("Waves", 0.5f);
            Caption(string.Format(lines[16], you));
            Zed(king, 1f, 0.5f);
            yield return Wait(1.3f);
            Zed(king, 1f, 0.5f);
            yield return Wait(1.2f);

            // Awake! A stretch, and something sparkles in the waves.
            king.Lean(0f, 0.4f);
            king.Squash(0.15f);
            yield return Wait(0.6f);
            var crownArt = Wardrobe.Art("Hat_Crown");
            var shine = new Vector2(0.6f, 0.36f);
            Burst(shine, IconPainter.Star, UiKit.Hex("FFE08A"), 10, 0.08f, 0.02f, 0.8f);
            IntroAudio.Sfx("Sparkle", 0.8f);
            yield return Wait(0.6f);
            if (crownArt != null)
            {
                var crown = Prop(crownArt, shine, 0.08f);
                float w = Stage.rect.width, h = Stage.rect.height;
                // It tumbles in on a wave to the capybara's feet…
                var feet = new Vector2((0.5f - shine.x) * w, (0.15f - shine.y) * h);
                Track(crown.DOJumpAnchorPos(crown.anchoredPosition + feet, 0.06f * h, 2, 1.2f).SetUpdate(true));
                Track(crown.DOLocalRotate(new Vector3(0f, 0f, 360f), 1.2f, RotateMode.FastBeyond360).SetUpdate(true));
                yield return Wait(1.4f);
                Bubble(king, IconPainter.Star, UiKit.Hex("E3A92B"), 0.8f);
                yield return Wait(0.6f);
                // …and up it goes, landing exactly where it will sit on its head (the worn crown
                // waits there, hidden, so the leap ends on the head and not in the air beside it).
                var worn = WearCrown(king, crownArt);
                var wornSize = worn.rect.size * (worn.lossyScale.y / Mathf.Max(0.0001f, crown.lossyScale.y));
                Track(crown.DOJump(worn.position, 0.1f * h * crown.lossyScale.y, 1, 0.6f).SetUpdate(true));
                Track(crown.DOSizeDelta(wornSize, 0.6f).SetUpdate(true));
                Track(crown.DOLocalRotate(Vector3.zero, 0.6f).SetUpdate(true));
                yield return Wait(0.62f);
                Destroy(crown.gameObject);
                Reveal(worn);
                IntroAudio.Sfx("Crown", 0.9f);
                Burst(new Vector2(king.Root.anchorMin.x, 0.34f), IconPainter.Star, UiKit.Hex("FFE08A"), 14, 0.12f, 0.022f, 0.8f);
            }
            king.Hop(3, 0.22f, 0.32f);
            Bubble(king, IconPainter.Heart, UiKit.Hex("F2668B"), 1.6f);
            yield return Wait(1.8f);
            yield return Next();

            // ---------------------------------------------------------------- 8. a bubble on the beach
            // Her bubble comes down to the water's edge, and the little king wakes her: the very
            // moment the game began with.
            var orb = SleepingBubble(glass, ghost, new Vector2(0.66f, 0.22f), 0.22f);
            if (orb != null)
            {
                Caption(string.Format(lines[17], you));
                float oh = Stage.rect.height;
                var rest = orb.Root.anchoredPosition;
                orb.Root.anchoredPosition = rest + new Vector2(0f, 0.75f * oh);
                Track(orb.Root.DOAnchorPos(rest, 3f).SetEase(Ease.OutSine).SetUpdate(true));
                yield return Wait(3f);
                orb.Bob(0.03f, 2f);
                Zed(orb, 0.7f, 0.9f);
                king.WalkTo(new Vector2(0.52f, 0.14f), 0.9f, 3);
                yield return Wait(1f);
                king.Face(true);
                Bubble(king, IconPainter.FaceWow, UiKit.Hex("E3A92B"), 1.2f);
                yield return Wait(1.4f);
                yield return Next();

                // Pop! She wakes, and can hardly believe it.
                king.Squash(0.15f);
                yield return Wait(0.2f);
                Sfx.Play(SfxKind.Pop, 0.9f);
                IntroAudio.Sfx("Bubble", 0.9f);
                var middle = orb.Root.anchorMin + new Vector2(0f, 0.11f);
                Burst(middle, UiKit.Circle, UiKit.Hex("CFF4FA"), 16, 0.13f, 0.014f, 0.6f);
                foreach (var g in orb.Root.GetComponentsInChildren<Image>()) Track(g.DOFade(0f, 0.2f).SetUpdate(true));
                var awake = Cast(ghost, orb.Root.anchorMin, 0.2f, tint: new Color(1f, 1f, 1f, 0f));
                awake.Fade(0.95f, 0.3f);
                awake.Body.localScale = new Vector3(1.25f, 0.75f, 1f);
                Track(awake.Body.DOScale(1f, 0.9f).SetEase(Ease.OutElastic).SetUpdate(true));
                IntroAudio.Sfx("Sparkle", 0.6f);
                yield return Wait(0.6f);
                Caption(lines[18]);
                Bubble(awake, IconPainter.FaceWow, UiKit.Hex("E3A92B"), 1.4f);
                awake.Bob(0.04f, 1.8f);
                Burst(middle + new Vector2(0f, 0.08f), IconPainter.Star, UiKit.Hex("FFE08A"), 12, 0.16f, 0.02f, 1f, Vector2.up);
                yield return Wait(1.8f);
                yield return Next();

                Caption(string.Format(lines[19], you));
                Bubble(awake, IconPainter.Heart, UiKit.Hex("F2668B"), 1.6f);
                yield return Wait(0.5f);
                Bubble(king, IconPainter.Heart, UiKit.Hex("F2668B"), 1.4f);
                king.Hop(2, 0.18f, 0.3f);
                yield return Wait(1.6f);
                yield return Next();
            }
            Title(StoryText.IntroTitle, StoryText.IntroSubtitle, new Vector2(0.5f, 0.74f));
            yield return Wait(1.6f);
            yield return Next();
        }

        /// <summary>
        /// Old Yuzu could not save herself. She stays by the frozen spring, watching the bathhouse.
        /// She gathers wood and keeps a little fire going, its light the only warm thing in the
        /// snow, through days and nights while she grows older; then the wind grows too strong, the
        /// fire gutters and goes out, and one winter night she lies down in the snow. The music
        /// stops, the wind dies, the light goes almost out, and even the ghost telling the story
        /// fades away: a long held breath in the dark. Then a glow: the spring's last warm breath
        /// catches her spirit in a bubble, where she sleeps until you wake her (the opening).
        /// </summary>
        IEnumerator Keeper(string[] lines, Sprite ghost)
        {
            var ages = new[] { Art("Cutscenes/Cutscene_IntroYuzu"), Art("Cutscenes/Cutscene_YuzuOlder"), Art("Cutscenes/Cutscene_YuzuOldest") };
            var rest = Art("Cutscenes/Cutscene_YuzuRest");
            if (ages[0] == null) yield break;
            var cold = new Color(0.86f, 0.89f, 0.98f);
            var spot = new Vector2(0.4f, 0.15f);
            var hearth = new Vector2(0.54f, 0.22f);
            Actor keeper = null;
            Image night = null;
            yield return Change(UiKit.Hex("10162A"), 1.4f, () =>
            {
                Aim(0.5f, 0.42f, 1.2f);
                Backdrop(Art("Cutscenes/Cutscene_IntroFrozen"), UiKit.Hex("8C9BB5"), UiKit.Hex("DDE5EE"));
                keeper = Cast(ages[0], spot, 0.3f, faceRight: true, tint: cold);   // looking at the bathhouse
                night = UiKit.Image(Stage, "Night", null, new Color(0.02f, 0.03f, 0.1f, 0f));
                night.raycastTarget = false;
                UiKit.Stretch(night.rectTransform);
                _loops.Add(StartCoroutine(Snow()));
            });
            IntroAudio.Play("intro_keeper", 2.5f);   // the long, lonely wait
            IntroAudio.Air("Amb_Wind", 0.5f, 2.5f);   // and the cold wind that never stops
            Caption(lines[7]);
            yield return Wait(2.2f);
            yield return Next();

            // ---- she gathers wood, and lights a little fire
            Caption(lines[8]);
            Track(night.DOFade(0.3f, 3f).SetEase(Ease.InOutSine).SetUpdate(true));   // evening comes on
            var log = Art("Cutscenes/Cutscene_Log");
            RectTransform pile = null;
            for (int trip = 0; trip < 2; trip++)
            {
                keeper.WalkTo(new Vector2(0.28f, 0.15f), 1.2f, 4);
                yield return Wait(1.3f);
                RectTransform carried = null;
                if (log != null)
                {
                    carried = Prop(log, Vector2.zero, 0.09f);
                    // balanced on her head: on the picture itself, so it hops and turns with her
                    // (her head, in the left-facing picture, is left of centre)
                    carried.SetParent(keeper.Body, false);
                    carried.anchorMin = carried.anchorMax = new Vector2(0.44f, 0.84f);
                    carried.pivot = new Vector2(0.5f, 0.1f);   // its underside, settled on top of her head
                    carried.anchoredPosition = Vector2.zero;
                    carried.localRotation = Quaternion.Euler(0f, 0f, -6f);
                }
                keeper.WalkTo(new Vector2(0.44f, 0.15f), 1.2f, 4);
                yield return Wait(1.3f);
                if (carried != null) Destroy(carried.gameObject);
                Sfx.Play(SfxKind.Plop, 0.4f);
                if (pile == null)
                {
                    pile = Prop(Art("Cutscenes/Cutscene_Firewood"), hearth, 0.17f, cold);
                    pile.SetSiblingIndex(night.rectTransform.GetSiblingIndex());   // in the dusk with everything else
                    pile.localScale = Vector3.one * 0.55f;
                }
                Track(pile.DOScale(trip == 0 ? 0.6f : 1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true));
            }
            keeper.Face(true);
            yield return Wait(0.4f);

            // Light! The fire's glow sits over the dark, so it lights her and the snow around it.
            var flame = Prop(Art("Cutscenes/Cutscene_Campfire"), hearth, 0.17f, new Color(1f, 1f, 1f, 0f));
            flame.pivot = new Vector2(0.5f, 0.12f);   // sways and shrinks from its base, not its middle
            flame.anchoredPosition -= new Vector2(0f, flame.sizeDelta.y * 0.38f);
            var near = Prop(SoftLight, hearth + new Vector2(0f, 0.05f), 0.55f, new Color(1f, 0.62f, 0.28f, 0f));
            var room = Prop(SoftLight, hearth + new Vector2(0f, 0.06f), 1.7f, new Color(1f, 0.55f, 0.25f, 0f));
            near.SetSiblingIndex(flame.GetSiblingIndex());
            room.SetSiblingIndex(near.GetSiblingIndex());
            Burst(hearth + new Vector2(0f, 0.04f), UiKit.Circle, UiKit.Hex("FFC266"), 10, 0.08f, 0.012f, 0.7f, Vector2.up);
            IntroAudio.Sfx("Sparkle", 0.35f);
            Track(flame.GetComponent<Image>().DOFade(1f, 0.6f).SetUpdate(true));
            Track(pile.GetComponent<Image>().DOFade(0f, 0.8f).SetUpdate(true));
            IntroAudio.Air("Amb_Fire", 0.5f, 1.5f);
            _fire = 0f;
            Track(DOTween.To(() => _fire, v => _fire = v, 1f, 1.6f).SetEase(Ease.OutSine).SetUpdate(true));
            _loops.Add(StartCoroutine(Firelight(flame, near.GetComponent<Image>(), room.GetComponent<Image>())));
            Track(night.DOFade(0.6f, 2f).SetEase(Ease.InOutSine).SetUpdate(true));
            keeper.Tint(new Color(1f, 0.9f, 0.8f), 2f);   // warm, by the fire
            yield return Wait(2.6f);

            // Day after night after day by the fire: each dark, she comes back a little older.
            for (int i = 1; i < ages.Length; i++)
            {
                Track(night.DOFade(0.15f, 1.1f).SetEase(Ease.InOutSine).SetUpdate(true));   // day: the fire is a small thing
                Track(DOTween.To(() => _fire, v => _fire = v, 0.45f, 1.1f).SetUpdate(true));
                yield return Wait(1.6f);
                Track(night.DOFade(0.62f, 1.1f).SetEase(Ease.InOutSine).SetUpdate(true));   // night: it is everything
                Track(DOTween.To(() => _fire, v => _fire = v, 1f, 1.1f).SetUpdate(true));
                yield return Wait(1.2f);
                if (ages[i] != null) keeper = Become(keeper, ages[i], spot, 0.3f, new Color(1f, 0.9f, 0.8f));
                yield return Wait(1.4f);
            }
            yield return Next();

            // ---- the wind grows too strong, and the fire goes out
            Caption(lines[9]);
            StopLoops();
            _loops.Add(StartCoroutine(Firelight(flame, near.GetComponent<Image>(), room.GetComponent<Image>(), 2.2f)));   // whipped by the wind
            _loops.Add(StartCoroutine(Snow(0.02f)));
            IntroAudio.Air("Amb_Wind", 1f, 1.5f);
            IntroAudio.Sfx("Wind", 0.8f);
            keeper.Shake(2.5f);
            Track(DOTween.To(() => _fire, v => _fire = v, 0.25f, 3.5f).SetEase(Ease.InSine).SetUpdate(true));
            yield return Wait(3.6f);
            // Out.
            var ashes = Prop(Art("Cutscenes/Cutscene_CampfireOut"), hearth, 0.17f, new Color(cold.r, cold.g, cold.b, 0f));
            ashes.SetSiblingIndex(night.rectTransform.GetSiblingIndex());
            Track(ashes.GetComponent<Image>().DOFade(1f, 0.6f).SetUpdate(true));
            Track(flame.GetComponent<Image>().DOFade(0f, 0.5f).SetUpdate(true));
            Track(DOTween.To(() => _fire, v => _fire = v, 0f, 0.8f).SetUpdate(true));
            IntroAudio.Air("Amb_Fire", 0f, 0.8f);
            IntroAudio.Sfx("Freeze", 0.5f);
            for (int i = 0; i < 3; i++) Puff(hearth + new Vector2(0.01f * i, 0.06f), new Color(0.75f, 0.78f, 0.85f, 0.5f), 0.07f, 0.2f, 2.2f + i * 0.4f);
            Track(night.DOFade(0.68f, 1.2f).SetUpdate(true));
            keeper.Tint(cold, 1.2f);
            yield return Wait(2.4f);
            yield return Next();

            // ---- One last winter night. The music leaves first; the snow thins to a few last flakes.
            Caption(lines[10]);
            StopLoops();
            Track(night.DOFade(0.55f, 2.2f).SetEase(Ease.InOutSine).SetUpdate(true));
            IntroAudio.Hush(3f);
            IntroAudio.Air("Amb_Wind", 0.35f, 3f);
            _loops.Add(StartCoroutine(Snow(0.35f)));
            yield return Wait(2.4f);
            // She lies down, slowly, and closes her eyes.
            IntroAudio.Sfx("Fade", 0.45f);
            if (rest != null) keeper = Become(keeper, rest, spot, 0.3f * rest.rect.height / rest.rect.width, cold, 2.4f);
            yield return Wait(3f);
            yield return Next();

            // The last warmth goes out: almost black, the wind falls silent, the snow stops,
            // and even the one telling the story is gone. Hold it.
            Caption(lines[11]);
            StopLoops();
            Track(night.DOFade(0.9f, 3.5f).SetEase(Ease.InOutSine).SetUpdate(true));
            keeper.Tint(new Color(0.55f, 0.6f, 0.75f), 3.5f);
            IntroAudio.Air("Amb_Wind", 0f, 3.5f);
            if (_narrator != null)
            {
                _narrator.rectTransform.DOKill();
                Track(_narrator.DOFade(0f, 2.5f).SetUpdate(true));
            }
            yield return Wait(4.5f);
            yield return Next();
            yield return Wait(1.2f);   // and a breath more of the dark

            if (ghost == null) yield break;
            // ...a glow, in the dark: her spirit rises out of her...
            Caption(lines[12]);
            var from = spot + new Vector2(0f, 0.05f);
            var light = Prop(SoftLight, from + new Vector2(0f, 0.05f), 0.15f, new Color(0.75f, 1f, 0.95f, 0f));
            Track(light.GetComponent<Image>().DOFade(0.85f, 1.6f).SetUpdate(true));
            Track(light.DOScale(4f, 3.2f).SetEase(Ease.OutSine).SetUpdate(true));
            IntroAudio.Play("intro_awaken", 2.5f);   // her theme, from when she woke for you
            IntroAudio.Sfx("Magic", 0.5f);
            yield return Wait(1.2f);
            Track(night.DOFade(0.45f, 3f).SetEase(Ease.InOutSine).SetUpdate(true));   // moonlight comes back
            keeper.Tint(cold, 3f);
            var spirit = Cast(ghost, from, 0.24f, tint: new Color(1f, 1f, 1f, 0f));
            spirit.Fade(0.95f, 1.6f);
            float h = Stage.rect.height;
            Track(spirit.Root.DOAnchorPosY(spirit.Root.anchoredPosition.y + 0.16f * h, 2.6f).SetEase(Ease.OutSine).SetUpdate(true));
            Burst(from + new Vector2(0f, 0.1f), IconPainter.Star, UiKit.Hex("CFF7EE"), 14, 0.14f, 0.018f, 1.4f, Vector2.up);
            Track(light.GetComponent<Image>().DOFade(0.25f, 2.5f).SetDelay(1.6f).SetUpdate(true));
            yield return Wait(2.4f);

            // ...and the frozen spring breathes out one last warm wisp, which wraps her in a bubble.
            var spring = new Vector2(0.24f, 0.27f);
            var target = from + new Vector2(0f, 0.16f + 0.13f);
            for (int i = 0; i <= 8; i++)
            {
                var at = Vector2.Lerp(spring, target, i / 8f) + new Vector2(0f, Mathf.Sin(i / 8f * Mathf.PI) * 0.08f);
                Puff(at, new Color(1f, 0.93f, 0.8f, 0.55f), 0.06f, 0.05f, 1.4f);
                yield return Wait(0.12f);
            }
            IntroAudio.Sfx("Bubble", 0.8f);
            var shell = UiKit.Image(spirit.Root, "Bubble", BubbleGlass(), new Color(1f, 1f, 1f, 0.85f));
            shell.preserveAspect = true;
            shell.raycastTarget = false;
            var sr = shell.rectTransform;
            sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0.5f);
            float across = Mathf.Max(spirit.Root.sizeDelta.x, spirit.Root.sizeDelta.y) * 1.45f;
            sr.sizeDelta = new Vector2(across, across);
            sr.localScale = Vector3.zero;
            Track(sr.DOScale(1f, 0.6f).SetEase(Ease.OutBack).SetUpdate(true));
            yield return Wait(0.6f);
            // Inside, she curls up and falls asleep.
            var dream = Art("Cutscenes/Cutscene_GhostSleep");
            if (dream != null)
            {
                var curled = UiKit.Image(spirit.Root, "Asleep", dream, new Color(1f, 1f, 1f, 0f));
                curled.preserveAspect = true;
                curled.raycastTarget = false;
                var cr = curled.rectTransform;
                cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.5f);
                cr.sizeDelta = new Vector2(across * 0.8f, across * 0.8f);
                cr.SetSiblingIndex(sr.GetSiblingIndex());
                Track(curled.DOFade(0.95f, 1.2f).SetUpdate(true));
                spirit.Fade(0f, 1.2f);
            }
            yield return Wait(1.4f);
            spirit.Bob(0.04f, 2f);
            Zed(spirit, 0.8f, 0.9f);
            yield return Wait(1.2f);
            yield return Next();

            // She sleeps there a long, long time, drifting, until you come along.
            Caption(lines[13]);
            Track(spirit.Root.DOAnchorPosY(spirit.Root.anchoredPosition.y + 0.7f * h, 7f).SetEase(Ease.InSine).SetUpdate(true));
            Zed(spirit, 0.8f, 0.9f);
            if (_narrator != null)
            {
                Track(_narrator.DOFade(0.92f, 1.2f).SetDelay(0.8f).SetUpdate(true));
                yield return Wait(0.9f);
                Narrating();
            }
            yield return Wait(1.6f);
            Zed(spirit, 0.8f, 0.9f);
            yield return Wait(0.6f);
            yield return Next();
            StopLoops();
        }

        float _fire;   // how brightly the fire burns, 0 (out) … 1

        /// <summary>
        /// The fire's light, never still: the flames sway and the glows breathe and flutter (Perlin
        /// noise, fast and slow). <paramref name="wild"/> above 1 is the fire whipped by wind.
        /// </summary>
        IEnumerator Firelight(RectTransform flame, Image near, Image room, float wild = 1f)
        {
            while (flame != null)
            {
                float t = Time.unscaledTime;
                float n = 0.6f * Mathf.PerlinNoise(t * 1.4f * wild, 0.2f) + 0.4f * Mathf.PerlinNoise(t * 6f * wild, 5.3f);
                float f = _fire * (wild > 1f ? Mathf.Lerp(0.55f, 1.2f, Mathf.PerlinNoise(t * 9f, 1.7f)) : 1f);
                near.color = new Color(near.color.r, near.color.g, near.color.b, f * (0.45f + 0.3f * n));
                room.color = new Color(room.color.r, room.color.g, room.color.b, f * (0.16f + 0.1f * n));
                near.rectTransform.localScale = Vector3.one * (0.92f + 0.12f * n);
                flame.localScale = new Vector3(1f + 0.03f * (n - 0.5f), Mathf.Lerp(0.4f, 1f, Mathf.Clamp01(_fire)) * (1f + 0.07f * (n - 0.5f) * wild), 1f);
                flame.localRotation = Quaternion.Euler(0f, 0f, (n - 0.5f) * 4f * wild);
                yield return null;
            }
        }

        /// <summary>
        /// Old Yuzu's spirit asleep in a bubble of the spring's warm air: the glass, with her curled
        /// up inside. Standing at <paramref name="at"/> like any actor, so it bobs, drifts and snores.
        /// </summary>
        Actor SleepingBubble(Sprite glass, Sprite ghost, Vector2 at, float height)
        {
            var dream = Art("Cutscenes/Cutscene_GhostSleep");
            if (dream == null && ghost == null) return null;
            // the actor's own picture stays empty; she and then the glass go on top of it
            var orb = Cast(glass, at, height, tint: new Color(1f, 1f, 1f, 0f));
            var inside = UiKit.Image(orb.Body, "Asleep", dream != null ? dream : ghost, new Color(1f, 1f, 1f, 0.95f));
            inside.preserveAspect = true;
            inside.raycastTarget = false;
            float pad = orb.Root.sizeDelta.y * 0.16f;
            UiKit.Stretch(inside.rectTransform, pad, pad, pad, pad);
            var skin = UiKit.Image(orb.Body, "Glass", glass, new Color(1f, 1f, 1f, 0.85f));
            skin.preserveAspect = true;
            skin.raycastTarget = false;
            UiKit.Stretch(skin.rectTransform);
            return orb;
        }

        /// <summary>Crossfades someone into a new picture of themselves, standing where they were.</summary>
        Actor Become(Actor was, Sprite art, Vector2 at, float height, Color tint, float seconds = 0.6f)
        {
            var next = Cast(art, at, height, faceRight: true, tint: new Color(tint.r, tint.g, tint.b, 0f));
            next.Root.SetSiblingIndex(was.Root.GetSiblingIndex() + 1);
            next.Fade(1f, seconds);
            was.Fade(0f, seconds);
            return next;
        }

        void StopLoops()
        {
            foreach (var c in _loops) if (c != null) StopCoroutine(c);
            _loops.Clear();
        }

        /// <summary>The capybara's crown, worn where the wardrobe puts it.</summary>
        static RectTransform WearCrown(Actor king, Sprite art)
        {
            var item = Wardrobe.Find("Hat_Crown");
            var layer = UiKit.Image(king.Body, "Hat", art, Color.white);
            layer.preserveAspect = true;
            layer.raycastTarget = false;
            var r = layer.rectTransform;
            var size = king.Root.sizeDelta;
            r.anchorMin = r.anchorMax = item != null ? item.Anchor : new Vector2(0.45f, 0.875f);
            r.pivot = new Vector2(0.5f, 0.5f);
            float pw = (item != null ? item.Width : 0.29f) * size.x;
            r.sizeDelta = new Vector2(pw, pw * art.rect.height / art.rect.width);
            r.anchoredPosition = Vector2.zero;
            layer.color = new Color(1f, 1f, 1f, 0f);   // hidden until the crown lands (Reveal)
            return r;
        }

        /// <summary>The worn crown appears with a little settle as the flying one lands.</summary>
        static void Reveal(RectTransform worn)
        {
            worn.GetComponent<Image>().color = Color.white;
            worn.localScale = Vector3.one * 1.25f;
            worn.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        /// <summary>A bubble of warm air, with something inside, rising and drifting away.</summary>
        void FloatAway(Sprite glass, Sprite inside, Vector2 from, Vector2 to, float seconds, float size, float delay)
        {
            var bubble = Prop(glass, from, size, new Color(1f, 1f, 1f, 0.85f));
            if (inside != null)
            {
                var pic = UiKit.Image(bubble, "Inside", inside, Color.white);
                pic.preserveAspect = true;
                pic.raycastTarget = false;
                UiKit.Stretch(pic.rectTransform, bubble.sizeDelta.x * 0.2f, bubble.sizeDelta.y * 0.2f, bubble.sizeDelta.x * 0.2f, bubble.sizeDelta.y * 0.2f);
            }
            bubble.localScale = Vector3.zero;
            float w = Stage.rect.width, h = Stage.rect.height;
            var path = new Vector2((to.x - from.x) * w, (to.y - from.y) * h);
            var seq = DOTween.Sequence().SetUpdate(true).SetDelay(delay);
            seq.Append(bubble.DOScale(1f, 0.5f).SetEase(Ease.OutBack));
            seq.Append(bubble.DOAnchorPos(bubble.anchoredPosition + path, seconds).SetEase(Ease.InSine));
            seq.Join(bubble.DOLocalRotate(new Vector3(0f, 0f, UnityEngine.Random.Range(-15f, 15f)), seconds));
            seq.AppendCallback(() => { if (bubble != null) Destroy(bubble.gameObject); });
            Track(seq);
        }

        /// <summary>
        /// A soft ring of bath foam where a soaking capybara meets the water, hiding the edge of the
        /// picture where it is cut off below the surface; it breathes gently, and tiny bubbles fizz up.
        /// </summary>
        void Foam(Actor bather)
        {
            var root = bather.Root;
            float w = root.sizeDelta.x, h = root.sizeDelta.y;
            var rnd = new System.Random(root.GetInstanceID());
            float R() => (float)rnd.NextDouble();
            const int blobs = 10;
            for (int i = 0; i < blobs; i++)
            {
                float t = i / (blobs - 1f), edge = Mathf.Abs(t - 0.5f) * 2f;
                float size = h * Mathf.Lerp(0.17f, 0.27f, R()) * (1f - 0.35f * edge);
                var blob = UiKit.Image(root, "Foam", UiKit.Circle, i % 3 == 1 ? new Color(0.84f, 0.95f, 0.97f, 0.96f) : new Color(1f, 1f, 1f, 0.96f));
                blob.raycastTarget = false;
                var r = blob.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(size * 1.3f, size);
                r.anchoredPosition = new Vector2((t - 0.5f) * w * 1.05f, h * (0.015f + 0.04f * R()) - edge * h * 0.03f);
                Track(r.DOScale(1.1f, 0.7f + 0.6f * R()).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetDelay(R()).SetUpdate(true));
            }
            _loops.Add(StartCoroutine(Fizz(root)));
        }

        IEnumerator Fizz(RectTransform root)
        {
            float w = root.sizeDelta.x, h = root.sizeDelta.y;
            while (root != null)
            {
                var bead = UiKit.Image(root, "Fizz", UiKit.Circle, new Color(1f, 1f, 1f, 0.9f));
                bead.raycastTarget = false;
                var r = bead.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
                float size = h * UnityEngine.Random.Range(0.035f, 0.06f);
                r.sizeDelta = new Vector2(size, size);
                r.anchoredPosition = new Vector2(UnityEngine.Random.Range(-0.45f, 0.45f) * w, h * 0.1f);
                Track(r.DOAnchorPosY(h * UnityEngine.Random.Range(0.22f, 0.32f), 0.9f).SetEase(Ease.OutSine).SetUpdate(true));
                Track(bead.DOFade(0f, 0.9f).SetEase(Ease.InQuad).SetUpdate(true).OnComplete(() => { if (r != null) Destroy(r.gameObject); }));
                yield return Wait(UnityEngine.Random.Range(0.25f, 0.6f));
            }
        }

        IEnumerator Steam(Vector2 at, float spread, float rise)
        {
            while (true)
            {
                Puff(at + new Vector2(UnityEngine.Random.Range(-spread, spread), 0f), new Color(1f, 1f, 1f, 0.35f), 0.08f, rise * 0.3f, 2.2f);
                yield return Wait(0.4f);
            }
        }

        IEnumerator Snow(float every = 0.06f)
        {
            float h = Stage.rect.height, w = Stage.rect.width;
            while (true)
            {
                var flake = Prop(UiKit.Circle, new Vector2(UnityEngine.Random.Range(0f, 1.1f), 1.05f), UnityEngine.Random.Range(0.008f, 0.018f), new Color(1f, 1f, 1f, 0.9f));
                float fall = UnityEngine.Random.Range(2.5f, 4f);
                Track(flake.DOAnchorPos(flake.anchoredPosition + new Vector2(-UnityEngine.Random.Range(0.05f, 0.2f) * w, -1.15f * h), fall).SetEase(Ease.Linear).SetUpdate(true)
                    .OnComplete(() => { if (flake != null) Destroy(flake.gameObject); }));
                yield return Wait(every);
            }
        }
    }
}
