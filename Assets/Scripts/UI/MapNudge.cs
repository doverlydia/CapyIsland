using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Playtesters never found the map, and its tip only came once they opened it. So, a while after
    /// the opening (or as soon as a bridge joins a second island, when travel starts to matter), a
    /// pointing finger taps on the Map button, with a word on what it's for. It goes once the map
    /// is opened, and comes back a couple more times, well spaced, until it is.
    /// </summary>
    public class MapNudge : MonoBehaviour
    {
        const string CountKey = "CapyIsland.MapNudges";
        const int MostTimes = 3;
        const float FirstAfter = 240f, Between = 360f, ShowFor = 16f;

        static float _readyAt = -1f;
        static MapNudge _shown;

        RectTransform _hand;
        float _until;

        /// <summary>Called by TipTriggers twice a second, once the opening is over.</summary>
        public static void Tick()
        {
            if (_shown != null || Guidance.Off || Visit.Active || HudTour.Seen("map")) return;
            if (PlayerPrefs.GetInt(CountKey, 0) >= MostTimes) return;
            if (_readyAt < 0f) _readyAt = Time.unscaledTime + (AnyBridgeOpen ? 20f : FirstAfter);
            else if (AnyBridgeOpen) _readyAt = Mathf.Min(_readyAt, Time.unscaledTime + 20f);
            if (Time.unscaledTime < _readyAt || !HudTour.Quiet || !Attention.Free(4f)) return;
            var hud = MobileHud.Instance;
            var map = hud != null ? (hud.transform.Find("SafeArea/SideButtons/Map") ?? hud.transform.Find("SideButtons/Map")) : null;
            if (map == null || !map.gameObject.activeInHierarchy) return;
            PlayerPrefs.SetInt(CountKey, PlayerPrefs.GetInt(CountKey, 0) + 1);
            PlayerPrefs.Save();
            _readyAt = Time.unscaledTime + Between;
            _shown = map.gameObject.AddComponent<MapNudge>();
        }

        static bool AnyBridgeOpen
        {
            get
            {
                var land = LandMap.Instance;
                if (land == null) return false;
                foreach (var p in land.Pieces) if (p.isBridge && p.open) return true;
                return false;
            }
        }

        void Start()
        {
            _until = Time.unscaledTime + ShowFor;
            UiScreen.Opened += OnScreen;
            var root = (RectTransform)transform;
            float size = root.sizeDelta.x;

            // The hand, to the left of the button, its finger pressing on it (the tutorial hand
            // points down; a quarter turn points it right, at the button by the screen edge).
            var hand = UiKit.Image(root, "NudgeHand", Game.TutorialPointer.HandSprite, Color.white);
            hand.preserveAspect = true;
            hand.raycastTarget = false;
            _hand = hand.rectTransform;
            _hand.anchorMin = _hand.anchorMax = new Vector2(0.5f, 1f);
            _hand.pivot = new Vector2(0.5f, 0f);   // the fingertip, once turned
            _hand.sizeDelta = new Vector2(130f, 130f);
            _hand.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Vector2 rest = new Vector2(-size * 0.5f - 40f, -size * 0.5f), press = rest + new Vector2(22f, 0f);
            _hand.anchoredPosition = rest;
            hand.color = new Color(1f, 1f, 1f, 0f);
            hand.DOFade(1f, 0.4f).SetUpdate(true);
            var tap = DOTween.Sequence().SetUpdate(true).SetLoops(-1).SetTarget(_hand);
            tap.Append(_hand.DOAnchorPos(press, 0.3f).SetEase(Ease.OutQuad));
            tap.Join(_hand.DOScale(0.9f, 0.3f));
            tap.Append(_hand.DOAnchorPos(rest, 0.35f).SetEase(Ease.InQuad));
            tap.Join(_hand.DOScale(1f, 0.35f));
            tap.AppendInterval(0.45f);

            // What it's for, to the left of the hand.
            var chip = UiKit.Image(root, "NudgeChip", UiKit.Rounded(26), UiKit.Surface);
            chip.raycastTarget = false;
            var cr = chip.rectTransform;
            cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 1f);
            cr.pivot = new Vector2(1f, 0.5f);
            cr.anchoredPosition = new Vector2(-size * 0.5f - 190f, -size * 0.5f);
            cr.sizeDelta = new Vector2(400f, 120f);
            var text = UiKit.Text(cr, "Text", "Tap the map to see every island, and travel there fast!", 28f, UiKit.Ink, true, TextAlignmentOptions.Center);
            UiKit.Stretch(text.rectTransform, 18f, 10f, 18f, 10f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 28f;
            var group = chip.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.DOFade(1f, 0.4f).SetUpdate(true);
            UiKit.ApplyFonts(cr);
        }

        void OnScreen(UiScreen screen)
        {
            if (screen is MapScreen) Destroy(this);
        }

        void Update()
        {
            if (Time.unscaledTime >= _until || HudTour.Seen("map") || Visit.Travelling) { Destroy(this); return; }
            // Steps aside while a tip, a window or a dialog has the screen, rather than sit on top of it.
            bool show = !HudTour.IsOpen && !UiScreen.AnyOpen && !SpeechDialog.IsOpen && !CardPopup.IsShowing;
            if (_hand != null && _hand.gameObject.activeSelf != show)
            {
                _hand.gameObject.SetActive(show);
                var chip = transform.Find("NudgeChip");
                if (chip != null) chip.gameObject.SetActive(show);
            }
        }

        void OnDestroy()
        {
            UiScreen.Opened -= OnScreen;
            if (_shown == this) _shown = null;
            if (_hand != null) { DOTween.Kill(_hand); Destroy(_hand.gameObject); }
            var chip = transform.Find("NudgeChip");
            if (chip != null) { DOTween.Kill(chip.GetComponent<CanvasGroup>()); Destroy(chip.gameObject); }
        }
    }
}
