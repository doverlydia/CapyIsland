using System;
using System.Collections;
using CapyIsland.Game;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Paying a bridge spirit, as a little movie. The ghost floats out over the sea and waves its
    /// paws like a stage magician… nothing. It tries again, much harder — and the land bridge
    /// bursts up out of the water with a splash that soaks you both. The far island's folk wave,
    /// the ghost takes a bow and fades away.
    /// </summary>
    public class BridgeCutscene : Cutscene
    {
        // Where things are in the painting (fractions of the stage).
        const float SeaTop = 0.47f, BridgeY = 0.36f;
        static readonly Vector2 GhostAt = new(0.42f, 0.3f);

        string _island;

        public static void Show(string island, Action done = null)
        {
            var scene = Play<BridgeCutscene>(done);
            if (scene != null) scene._island = island;
        }

        static Sprite Icon(string name)
        {
            var catalog = ItemCatalog.Instance;
            return catalog != null && catalog.Has(name) ? catalog.Icon(name) : null;
        }

        /// <summary>In its bubble: the shore, the bridge and the island across.</summary>
        protected override Vector3 BubbleView => new(0.52f, 0.45f, 1.1f);

        protected override IEnumerator Script()
        {
            Backdrop(Resources.Load<Sprite>("Cutscenes/Cutscene_Shore"), UiKit.Hex("BFE3F2"), UiKit.Hex("6FB7C9"));

            // The bridge, waiting under the water: drawn from the top down, so it rises into view.
            var bridgeArt = Resources.Load<Sprite>("Cutscenes/Cutscene_Bridge");
            RectTransform bridge = null;
            Image bridgeImage = null;
            if (bridgeArt != null)
            {
                // An arch of sand and grass, out across the water towards the far island.
                bridge = Prop(bridgeArt, new Vector2(0.6f, BridgeY), 0.2f);
                bridgeImage = bridge.GetComponent<Image>();
                bridgeImage.preserveAspect = false;
                bridgeImage.type = Image.Type.Filled;
                bridgeImage.fillMethod = Image.FillMethod.Vertical;
                bridgeImage.fillOrigin = (int)Image.OriginVertical.Top;
                bridgeImage.fillAmount = 0f;
                bridge.anchoredPosition -= new Vector2(0f, bridge.sizeDelta.y * 0.5f);
            }

            var worker = Icon("Capybara");
            var mate = worker != null ? Cast(worker, new Vector2(0.36f, 0.13f), 0.22f, faceRight: true) : null;
            var you = CastPlayer(new Vector2(0.46f, 0.09f), 0.26f, faceRight: true);

            var ghostArt = Icon("Ghost");
            var ghost = ghostArt != null ? Cast(ghostArt, GhostAt, 0.22f, tint: new Color(0.85f, 0.95f, 1f, 0f)) : null;
            if (ghost != null)
            {
                ghost.Fade(0.9f, 0.8f);
                ghost.Bob();
            }
            yield return Wait(1f);

            // Abracadabra…
            if (ghost != null)
            {
                ghost.Lean(-14f, 0.25f);
                yield return Wait(0.3f);
                ghost.Lean(14f, 0.25f);
                Burst(GhostAt + new Vector2(0f, 0.15f), IconPainter.Sparkle, UiKit.Hex("FFE9A8"), 8, 0.08f, 0.03f);
                yield return Wait(0.5f);
                ghost.Lean(0f, 0.2f);
            }
            yield return Wait(0.5f);

            // …nothing. A sad little puff.
            Puff(new Vector2(0.52f, SeaTop - 0.08f), new Color(0.6f, 0.62f, 0.68f, 0.9f), 0.06f, 0.06f, 1f);
            if (mate != null) mate.Lean(12f, 0.3f);
            you.Lean(10f, 0.3f);
            yield return Wait(1.1f);
            if (mate != null) mate.Lean(0f, 0.2f);
            you.Lean(0f, 0.2f);

            // Again, MUCH harder.
            if (ghost != null)
            {
                ghost.Shake(0.9f);
                Bubble(ghost, IconPainter.Star, UiKit.Hex("E3A92B"), 0.9f);
                Burst(GhostAt + new Vector2(0f, 0.15f), IconPainter.Sparkle, UiKit.Hex("FFE9A8"), 22, 0.18f, 0.035f);
            }
            yield return Wait(0.9f);

            // Up it comes, and everyone gets soaked.
            if (bridge != null)
            {
                Track(bridgeImage.DOFillAmount(1f, 1.1f).SetEase(Ease.OutBack).SetUpdate(true));
                Track(bridge.DOAnchorPosY(bridge.anchoredPosition.y + bridge.sizeDelta.y * 0.5f, 1.1f).SetEase(Ease.OutBack).SetUpdate(true));
            }
            Sfx.Play(SfxKind.Bridge, 1f);
            for (int i = 0; i < 4; i++)
            {
                Burst(new Vector2(0.15f + i * 0.23f, BridgeY), UiKit.Circle, UiKit.Hex("BFEAF0"), 10, 0.2f, 0.02f, 0.7f, Vector2.up);
                yield return Wait(0.12f);
            }
            Burst(new Vector2(0.3f, 0.35f), UiKit.Circle, UiKit.Hex("A9DCEB"), 26, 0.25f, 0.02f, 0.8f, new Vector2(-0.6f, -0.8f));
            yield return Wait(0.5f);
            var soaked = new Color(0.7f, 0.82f, 1f);
            you.Tint(soaked, 0.15f);
            mate?.Tint(soaked, 0.15f);
            yield return Wait(0.5f);

            // Shake it off.
            you.Shake(0.5f);
            mate?.Shake(0.5f);
            Burst(new Vector2(0.28f, 0.12f), UiKit.Circle, UiKit.Hex("BFEAF0"), 12, 0.12f, 0.014f);
            if (mate != null) Burst(new Vector2(0.12f, 0.14f), UiKit.Circle, UiKit.Hex("BFEAF0"), 10, 0.1f, 0.014f);
            you.Tint(Color.white, 0.5f);
            mate?.Tint(Color.white, 0.5f);
            yield return Wait(0.6f);

            // The far island waves back.
            if (worker != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var tiny = Cast(worker, new Vector2(i == 2 ? 0.76f : 0.6f + i * 0.06f, SeaTop), 0.07f);
                    tiny.Root.localScale = Vector3.zero;
                    Track(tiny.Root.DOScale(1f, 0.3f).SetDelay(i * 0.12f).SetEase(Ease.OutBack).SetUpdate(true));
                    tiny.Hop(4, 0.3f, 0.3f);
                }
            }
            you.Hop(3, 0.16f, 0.34f);
            mate?.Hop(3, 0.16f, 0.34f);

            // A bow, and the spirit's job is done.
            if (ghost != null)
            {
                ghost.Lean(-20f, 0.3f);
                yield return Wait(0.5f);
                ghost.Lean(0f, 0.3f);
                ghost.Fade(0f, 1f);
                Burst(GhostAt + new Vector2(0f, 0.1f), IconPainter.Sparkle, Color.white, 14, 0.12f, 0.03f, 1f);
            }
            Title("A bridge to " + (_island ?? "a new island") + "!", "Off you go, explorer", new Vector2(0.5f, 0.84f));
            yield return Wait(2.6f);
        }
    }
}
