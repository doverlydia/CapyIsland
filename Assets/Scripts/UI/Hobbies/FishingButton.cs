using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Fishing mode. At the water's edge a "Start fishing" button appears at the bottom of the
    /// screen; tap it and the whole screen becomes your rod: hold anywhere to aim, let go to cast;
    /// once a fish is on, hold to reel and let go to give it slack. What to do is written large
    /// at the top of the screen — where your thumb isn't — in the colour of the line, with the
    /// catch bar under it. "Stop fishing" puts the rod away and lets you walk again.
    /// </summary>
    public class FishingButton : MonoBehaviour
    {
        GameObject _start, _mode;
        TextMeshProUGUI _say, _hint;
        Image _progress, _warn;
        RectTransform _progressTrack, _sayRect;
        float _flashUntil;

        public static FishingButton Create(Transform parent)
        {
            var root = UiKit.Rect("Fishing", parent);
            UiKit.Stretch(root);
            var fb = root.gameObject.AddComponent<FishingButton>();
            fb.Build(root);
            return fb;
        }

        void Build(RectTransform root)
        {
            // Outside the mode: a friendly offer, bottom centre.
            var start = UiKit.Button(root, "StartFishing", "Start fishing", UiKit.ButtonStyle.Spring, Enter, 32f, IconPainter.Rod);
            var sr = (RectTransform)start.transform.parent;
            sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0.5f, 0f);
            sr.anchoredPosition = new Vector2(0f, 330f);   // above the speech box
            sr.sizeDelta = new Vector2(340f, 96f);
            _start = sr.gameObject;

            // The mode: everything else is the rod.
            var mode = UiKit.Rect("FishingMode", root);
            UiKit.Stretch(mode);
            _mode = mode.gameObject;
            var catcher = UiKit.Image(mode, "HoldAnywhere", null, new Color(0f, 0f, 0f, 0f), true);
            UiKit.Stretch(catcher.rectTransform);
            catcher.gameObject.AddComponent<HoldRelay>();

            // The words, large, top centre.
            var panel = UiKit.Image(mode, "Say", UiKit.Rounded(36), new Color(0.12f, 0.1f, 0.08f, 0.55f));
            panel.raycastTarget = false;
            _sayRect = panel.rectTransform;
            _sayRect.anchorMin = _sayRect.anchorMax = _sayRect.pivot = new Vector2(0.5f, 1f);
            _sayRect.anchoredPosition = new Vector2(0f, -250f);
            _sayRect.sizeDelta = new Vector2(620f, 170f);
            _say = UiKit.Text(panel.transform, "Text", "Hold to aim", 56f, Color.white, true, TextAlignmentOptions.Center);
            _say.raycastTarget = false;
            _say.rectTransform.anchorMin = new Vector2(0f, 0.42f);
            _say.rectTransform.anchorMax = Vector2.one;
            _say.rectTransform.offsetMin = new Vector2(20f, 0f);
            _say.rectTransform.offsetMax = new Vector2(-20f, -8f);
            _say.textWrappingMode = TextWrappingModes.NoWrap;
            _say.enableAutoSizing = true;
            _say.fontSizeMin = 30f;
            _say.fontSizeMax = 56f;
            _hint = UiKit.Text(panel.transform, "Hint", "", 26f, new Color(1f, 1f, 1f, 0.8f), false, TextAlignmentOptions.Center);
            _hint.raycastTarget = false;
            _hint.rectTransform.anchorMin = new Vector2(0f, 0.2f);
            _hint.rectTransform.anchorMax = new Vector2(1f, 0.44f);
            _hint.rectTransform.offsetMin = new Vector2(20f, 0f);
            _hint.rectTransform.offsetMax = new Vector2(-20f, 0f);

            var bar = UiKit.ProgressBar(panel.transform, "Catch", Color.white, 16f);
            _progressTrack = (RectTransform)bar.transform.parent;
            _progressTrack.anchorMin = new Vector2(0.08f, 0f);
            _progressTrack.anchorMax = new Vector2(0.92f, 0f);
            _progressTrack.pivot = new Vector2(0.5f, 0f);
            _progressTrack.anchoredPosition = new Vector2(0f, 18f);
            _progressTrack.sizeDelta = new Vector2(0f, 16f);
            _progressTrack.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
            _progress = bar;

            // A red ring around the panel while the line is at breaking point.
            _warn = UiKit.Image(mode, "Warn", UiKit.Rounded(40), Fishing.Red);
            _warn.raycastTarget = false;
            _warn.rectTransform.anchorMin = _warn.rectTransform.anchorMax = _warn.rectTransform.pivot = new Vector2(0.5f, 1f);
            _warn.rectTransform.anchoredPosition = new Vector2(0f, -240f);
            _warn.rectTransform.sizeDelta = new Vector2(640f, 190f);
            _warn.transform.SetSiblingIndex(panel.transform.GetSiblingIndex());

            // The way out.
            // Bottom-right, clear of the speech box.
            var stop = UiKit.RoundButton(mode, "StopFishing", IconPainter.Close, "Stop", UiKit.Hex("8A7563"), Exit, 104f);
            var stopR = (RectTransform)stop.transform.parent;
            stopR.anchorMin = stopR.anchorMax = stopR.pivot = new Vector2(1f, 0f);
            stopR.anchoredPosition = new Vector2(-24f, 40f);

            _mode.SetActive(false);
        }

        /// <summary>The whole screen: finger down is Press, finger up is Release.</summary>
        class HoldRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            bool _down;

            public void OnPointerDown(PointerEventData e)
            {
                _down = true;
                if (Fishing.Instance != null) Fishing.Instance.Press();
            }

            public void OnPointerUp(PointerEventData e) => Up();
            void OnDisable() => Up();

            void Up()
            {
                if (!_down) return;
                _down = false;
                if (Fishing.Instance != null) Fishing.Instance.Release();
            }
        }

        void Enter()
        {
            var f = Fishing.Instance;
            if (f == null) return;
            f.Mode = true;
            Sfx.Play(SfxKind.Tap, 0.5f);
            _sayRect.localScale = Vector3.one * 0.8f;
            _sayRect.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        void Exit()
        {
            var f = Fishing.Instance;
            if (f == null) return;
            f.Mode = false;
            f.StopFishing();
        }

        void OnEnable()
        {
            Fishing.Bitten += Flash;
            Fishing.Said += Hints.Show;
            HobbyLog.LevelledUp += OnLevel;
        }

        void OnDisable()
        {
            Fishing.Bitten -= Flash;
            Fishing.Said -= Hints.Show;
            HobbyLog.LevelledUp -= OnLevel;
        }

        static void OnLevel(Hobby h, int level)
        {
            string name = h switch
            {
                Hobby.Fishing => "Fishing", Hobby.Birding => "Birdwatching", Hobby.Writing => "Writing", Hobby.Sewing => "Sewing",
                Hobby.Gardening => "Gardening", Hobby.Cooking => "Cooking", Hobby.Bugs => "Bug catching", _ => "Cat care",
            };
            string more = h switch
            {
                Hobby.Fishing => "Rarer fish will start to swim near the shore, and your line holds a little better.",
                Hobby.Birding => "Rarer birds will start to visit.",
                Hobby.Bugs => "Rarer bugs will start to appear.",
                Hobby.Gardening => "New seeds to plant, and room for another plot!",
                Hobby.Cooking => "New recipes in the kitchen.",
                _ => "Keep it up!",
            };
            CardPopup.Show(name + " level " + level + "!", more, HobbyCutscene.Picture(h));
        }

        void Flash()
        {
            _flashUntil = Time.time + 0.9f;
            _sayRect.DOKill();
            _sayRect.localScale = Vector3.one;
            _sayRect.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.4f, 8, 0.6f).SetUpdate(true);
        }

        void Update()
        {
            var f = Fishing.Instance;
            bool blocked = f == null || UiScreen.AnyOpen || Cutscene.Playing;
            // Leaving the shore (or a menu opening over everything) ends fishing mode.
            if (f != null && f.Mode && (!f.AtShore && f.Now == Fishing.State.Idle)) f.Mode = false;

            bool mode = !blocked && f.Mode;
            bool offer = !blocked && !f.Mode && f.AtShore && f.Now == Fishing.State.Idle && HobbyLog.IsUnlocked(Hobby.Fishing);
            if (_mode.activeSelf != mode) _mode.SetActive(mode);
            if (_start.activeSelf != offer) _start.SetActive(offer);
            if (!mode) return;

            bool fight = f.Now == Fishing.State.Fight;
            _progressTrack.gameObject.SetActive(fight);
            _warn.enabled = fight && f.Overload > 0.01f;
            if (_warn.enabled)
            {
                var c = Fishing.Red;
                c.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * (8f + f.Overload * 14f)));
                _warn.color = c;
            }

            string say, hint = "";
            Color colour = Color.white;
            switch (f.Now)
            {
                case Fishing.State.Aiming: say = "Let go to cast!"; hint = "The ring is where your bobber will land"; break;
                case Fishing.State.Waiting: say = "Wait…"; hint = "A fish is coming to look"; break;
                case Fishing.State.Nibbling: say = "Wait for it…"; hint = "Nibble, nibble… wait for the big splash"; break;
                case Fishing.State.Fight:
                    float t = f.Tension;
                    colour = Fishing.TensionColour(t);
                    if (t >= Fishing.RedAt) { say = f.Overload > 0.01f ? "LET GO!" : "Let go!"; hint = "The line is about to snap"; }
                    else if (t >= Fishing.YellowAt) { say = f.Holding ? "Careful…" : "Hold to reel!"; hint = "Ease off when it turns red"; }
                    else { say = f.Holding ? "Reeling…" : "Hold to reel!"; hint = f.Surging ? "It's pulling hard!" : "Bring it in"; }
                    UiKit.SetProgress(_progress, f.Progress);
                    _progress.color = colour;
                    break;
                default: say = "Hold to aim"; hint = "Hold anywhere, let go to cast"; break;
            }
            if (Time.time < _flashUntil) { say = "Hooked!"; colour = UiKit.Sun; }
            _say.text = say;
            _say.color = colour;
            _hint.text = hint;
        }
    }
}
