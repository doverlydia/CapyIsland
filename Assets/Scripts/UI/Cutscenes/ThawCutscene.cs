using System;
using System.Collections;
using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The little movies of Old Yuzu thawing (<see cref="YuzuHeart"/>), each in a bubble at her
    /// spring, where she lies frozen under her dome of ice with her ghost floating nearby:
    /// <list type="bullet">
    /// <item>Drip: a crack runs through the ice and a drop falls. She doesn't notice. Only you do.</item>
    /// <item>Twitch: her frozen ear twitches; the ghost spins round. "Did... did you see that?"</item>
    /// <item>Glow: a warm light where her heart is. She begins to wonder.</item>
    /// <item>Alive: the last hearts pour in, the ice shatters, the ghost sinks back into her body,
    /// and Old Yuzu opens her eyes.</item>
    /// </list>
    /// </summary>
    public class ThawCutscene : Cutscene
    {
        static YuzuHeart.Stage _pending;
        YuzuHeart.Stage _stage;

        public static void Show(YuzuHeart.Stage stage, Action done = null)
        {
            _pending = stage;
            var scene = Play<ThawCutscene>(() => { IntroAudio.Stop(1.5f); done?.Invoke(); });
            if (scene == null) IntroAudio.Stop(0f);
        }

        /// <summary>The quiet first clue plays without a word; the rest wait for each line to be read.</summary>
        protected override bool ReaderPaced => _pending >= YuzuHeart.Stage.Twitch;

        protected override Vector3 BubbleView => new(0.55f, 0.42f, 1.25f);

        static Sprite Art(string path) => Resources.Load<Sprite>("Cutscenes/" + path);

        static readonly Color Icy = new(0.6f, 0.76f, 0.95f);
        static readonly Vector2 Spot = new(0.62f, 0.16f);   // where she lies, on the grass beside the spring

        Actor _body, _ghost;

        // Her ghost picture faces right (the others face left): facing the ice is "not turned".
        void GhostLooksAtIce(bool atIce) => _ghost?.Face(!atIce);
        RectTransform _ice, _cracked, _glow;

        protected override IEnumerator Script()
        {
            _stage = _pending;
            var you = string.IsNullOrEmpty(PlayerProfile.Name) ? PlayerProfile.Name : "<color=#8EE07A>" + PlayerProfile.Name + "</color>";
            Backdrop(Art("Cutscene_IntroWarm"), UiKit.Hex("9FD3EA"), UiKit.Hex("F2D49A"));
            StartCoroutine(Steam());

            // Her, frozen; the ice over her; her ghost nearby.
            float t = YuzuHeart.Thaw;
            _body = Cast(Art("Cutscene_YuzuRest"), Spot, 0.2f, tint: Color.Lerp(Icy, new Color(0.92f, 0.95f, 1f), Mathf.Clamp01(t * 1.1f)));
            var shell = Art("Cutscene_IceShell");
            var crackedArt = Art("Cutscene_IceShellCracked");
            float bodyWidth = _body.Root.sizeDelta.x / Stage.rect.height;   // in stage heights
            float iceHigh = shell != null ? bodyWidth * 1.3f * shell.rect.height / shell.rect.width : 0.2f;
            var iceAt = new Vector2(Spot.x, Spot.y + iceHigh * 0.5f - 0.012f);
            bool cracking = _stage > YuzuHeart.Stage.Drip;   // the drip movie is where the cracks first appear
            _ice = Prop(shell, iceAt, iceHigh, new Color(1f, 1f, 1f, cracking ? 0f : 0.62f));
            float crackedHigh = crackedArt != null ? bodyWidth * 1.3f * crackedArt.rect.height / crackedArt.rect.width : iceHigh;
            _cracked = Prop(crackedArt, new Vector2(Spot.x, Spot.y + crackedHigh * 0.5f - 0.012f), crackedHigh, new Color(1f, 1f, 1f, cracking ? 0.55f : 0f));
            _glow = Prop(SoftLight, Spot + new Vector2(0.01f, 0.07f), 0.5f, new Color(1f, 0.62f, 0.25f, _stage > YuzuHeart.Stage.Glow ? 0.7f : 0f));
            var ghostArt = GhostDialogue.Instance != null ? GhostDialogue.Instance.GhostFace : null;
            _ghost = ghostArt != null ? Cast(ghostArt, new Vector2(0.4f, 0.27f), 0.22f, tint: new Color(1f, 1f, 1f, 0.92f)) : null;
            _ghost?.Bob(0.04f, 1.8f);

            switch (_stage)
            {
                case YuzuHeart.Stage.Drip: yield return Drip(); break;
                case YuzuHeart.Stage.Twitch: yield return Twitch(); break;
                case YuzuHeart.Stage.Glow: yield return Glow(); break;
                case YuzuHeart.Stage.Alive: yield return Wake(you); break;
            }
        }

        // ---------------------------------------------------------------- 1. a drip

        IEnumerator Drip()
        {
            IntroAudio.Play("intro_awaken", 1.5f);
            // She hums to herself, looking at the spring, back to the ice.
            if (_ghost != null) { GhostLooksAtIce(false); Bubble(_ghost, IconPainter.FaceSmile, UiKit.Hex("8FD3C8"), 1.6f); }
            yield return Wait(1.8f);
            // Behind her: a crack runs through the ice...
            IntroAudio.Sfx("Freeze", 0.5f);
            Track(_cracked.GetComponent<Image>().DOFade(0.6f, 0.5f).SetUpdate(true));
            Track(_ice.GetComponent<Image>().DOFade(0f, 0.5f).SetUpdate(true));
            Track(_cracked.DOShakeAnchorPos(0.4f, 4f, 20).SetUpdate(true));
            yield return Wait(0.9f);
            // ...and a single drop falls.
            for (int i = 0; i < 2; i++)
            {
                FallingDrop();
                yield return Wait(0.9f);
            }
            if (_ghost != null) Bubble(_ghost, IconPainter.FaceSmile, UiKit.Hex("8FD3C8"), 1.2f);
            yield return Wait(1.6f);
        }

        // ---------------------------------------------------------------- 2. a twitch

        IEnumerator Twitch()
        {
            IntroAudio.Play("intro_awaken", 1.5f);
            GhostLooksAtIce(false);   // chatting away, facing the spring
            yield return Wait(1.2f);
            // Her frozen ear twitches.
            _body.Squash(0.08f);
            Track(_body.Body.DOPunchRotation(new Vector3(0f, 0f, 4f), 0.4f, 6).SetUpdate(true));
            Sfx.Play(SfxKind.Tap, 0.5f);
            yield return Wait(0.5f);
            // The ghost spins round.
            if (_ghost != null) { GhostLooksAtIce(true); _ghost.Hop(1, 0.1f, 0.25f); Bubble(_ghost, IconPainter.FaceWow, UiKit.Hex("E3A92B"), 1.4f); }
            Caption(StoryText.Yuzu.Twitch);
            yield return Wait(1f);
            yield return Next();
            _ghost?.Nudge(0.06f, 0.8f);   // she peers closer; nothing moves
            yield return Wait(0.9f);
            _ghost?.Nudge(-0.06f, 0.6f);
            Caption(StoryText.Yuzu.Seeing);
            yield return Wait(0.8f);
            yield return Next();
        }

        // ---------------------------------------------------------------- 3. a glow

        IEnumerator Glow()
        {
            IntroAudio.Play("intro_awaken", 1.5f);
            yield return Wait(0.8f);
            // A warm light, where her heart is.
            var glow = _glow.GetComponent<Image>();
            Track(glow.DOFade(0.9f, 2f).SetUpdate(true));
            Track(_glow.DOScale(1.25f, 1.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true));
            IntroAudio.Sfx("Sparkle", 0.4f);
            yield return Wait(1.2f);
            // She floats over, very quiet.
            if (_ghost != null) { GhostLooksAtIce(true); _ghost.Nudge(0.1f, 1.6f); }
            yield return Wait(1.4f);
            Caption(StoryText.Yuzu.Glow);
            yield return Wait(0.8f);
            yield return Next();
            Caption(StoryText.Yuzu.Hope);
            yield return Wait(0.8f);
            yield return Next();
        }

        // ---------------------------------------------------------------- 4. alive

        IEnumerator Wake(string you)
        {
            IntroAudio.Play("intro_dawn", 2f);
            var glow = _glow.GetComponent<Image>();
            Track(_glow.DOScale(1.2f, 1.1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true));
            float w = Stage.rect.width, h = Stage.rect.height;
            var heart = (Vector2)Spot + new Vector2(0.01f, 0.08f);

            // The last warm hearts pour in from everywhere.
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f;
                var from = heart + new Vector2(Mathf.Cos(a) * 0.45f, Mathf.Abs(Mathf.Sin(a)) * 0.5f + 0.15f);
                var h1 = Prop(IconPainter.Heart, from, 0.045f, UiKit.Hex("F2668B"));
                var to = new Vector2((heart.x - from.x) * w, (heart.y - from.y) * h);
                Track(h1.DOAnchorPos(h1.anchoredPosition + to, 0.9f).SetEase(Ease.InSine).SetUpdate(true).OnComplete(() => { if (h1 != null) Destroy(h1.gameObject); }));
                Track(glow.DOFade(Mathf.Min(0.9f, 0.4f + i * 0.06f), 0.3f).SetDelay(0.9f).SetUpdate(true));
                yield return Wait(0.18f);
            }
            yield return Wait(1f);

            // The ice cracks, shakes... and shatters.
            IntroAudio.Sfx("Freeze", 0.6f);
            Track(_cracked.GetComponent<Image>().DOFade(0.7f, 0.3f).SetUpdate(true));
            Track(_cracked.DOShakeAnchorPos(1.2f, 8f, 25).SetUpdate(true));
            yield return Wait(1.2f);
            Sfx.Play(SfxKind.Pop, 1f);
            IntroAudio.Sfx("Sparkle", 0.8f);
            Burst(Spot + new Vector2(0f, 0.08f), IconPainter.Sparkle, UiKit.Hex("DDF4FF"), 26, 0.3f, 0.03f, 1.1f);
            Burst(Spot + new Vector2(0f, 0.06f), UiKit.Circle, UiKit.Hex("BFE6FA"), 18, 0.22f, 0.015f, 0.8f);
            Track(_cracked.GetComponent<Image>().DOFade(0f, 0.25f).SetUpdate(true));
            _body.Tint(Color.white, 1.6f);
            yield return Wait(1.2f);

            // Her ghost drifts down and sinks back into her.
            if (_ghost != null)
            {
                GhostLooksAtIce(true);
                float bodyX = (Spot.x - _ghost.Root.anchorMin.x) * w, bodyY = (Spot.y - _ghost.Root.anchorMin.y) * h;
                Track(_ghost.Root.DOAnchorPos(new Vector2(bodyX, bodyY + 0.02f * h), 1.6f).SetEase(Ease.InOutSine).SetUpdate(true));
                _ghost.Fade(0f, 1.8f);
                Track(_ghost.Root.DOScale(0.6f, 1.6f).SetUpdate(true));
            }
            yield return Wait(1.7f);
            Flash(new Color(1f, 0.92f, 0.7f, 0.85f), 1.2f);
            IntroAudio.Sfx("Magic", 0.7f);
            Track(glow.DOFade(0f, 1.2f).SetUpdate(true));

            // And she's up: Old Yuzu, warm and blinking.
            var standing = Cast(Art("Cutscene_IntroYuzu"), Spot, 0.3f, tint: new Color(1f, 1f, 1f, 0f));
            standing.Fade(1f, 0.6f);
            _body.Fade(0f, 0.5f);
            yield return Wait(0.6f);
            standing.Squash(0.15f);
            yield return Wait(0.7f);
            Caption(StoryText.Yuzu.Warm);
            yield return Wait(1f);
            yield return Next();
            standing.Hop(3, 0.18f, 0.3f);
            Bubble(standing, IconPainter.Heart, UiKit.Hex("F2668B"), 1.6f);
            Burst(Spot + new Vector2(0f, 0.2f), IconPainter.Star, UiKit.Hex("FFE08A"), 16, 0.2f, 0.022f, 1f, Vector2.up);
            Caption(string.Format(StoryText.Yuzu.Alive, you));
            yield return Wait(1f);
            yield return Next();
            Caption(StoryText.Yuzu.Wedding);
            yield return Wait(0.8f);
            yield return Next();
        }

        // ---------------------------------------------------------------- bits

        void FallingDrop()
        {
            var b = _cracked;
            var at = (Vector2)b.anchorMin + new Vector2(UnityEngine.Random.Range(-0.05f, 0.05f), -b.sizeDelta.y * 0.42f / Stage.rect.height);
            var drop = Prop(UiKit.Circle, at, 0.014f, new Color(0.75f, 0.9f, 1f, 0.95f));
            drop.localScale = new Vector3(0.8f, 1.1f, 1f);
            Sfx.Play(SfxKind.Plop, 0.35f);
            Track(drop.DOAnchorPosY(drop.anchoredPosition.y - 0.05f * Stage.rect.height, 0.45f).SetEase(Ease.InQuad).SetUpdate(true));
            Track(drop.GetComponent<Image>().DOFade(0f, 0.45f).SetEase(Ease.InQuad).SetUpdate(true).OnComplete(() => { if (drop != null) Destroy(drop.gameObject); }));
        }

        IEnumerator Steam()
        {
            while (true)
            {
                Puff(new Vector2(0.3f + UnityEngine.Random.Range(-0.16f, 0.16f), 0.26f), new Color(1f, 1f, 1f, 0.35f), 0.08f, 0.13f, 2.2f);
                yield return Wait(0.4f);
            }
        }
    }
}
