using CapyIsland.Game;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The fishing button: appears when you stand at the water's edge. Tap to cast; while the line
    /// is out it becomes "Reel!", and it jumps and turns orange the instant a fish truly bites.
    /// </summary>
    public class FishingButton : MonoBehaviour
    {
        GameObject _root;
        Image _disc;
        TextMeshProUGUI _caption;
        float _flashUntil;

        public static FishingButton Create(Transform parent)
        {
            var root = UiKit.Rect("FishingButton", parent);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(1f, 0f);
            root.anchoredPosition = new Vector2(-20f, 400f);
            root.sizeDelta = new Vector2(130f, 170f);
            var fb = root.gameObject.AddComponent<FishingButton>();
            fb.Build(root);
            return fb;
        }

        void Build(RectTransform root)
        {
            var button = UiKit.RoundButton(root, "Rod", IconPainter.Rod, "Fish", UiKit.Spring, Press, 124f);
            _root = button.transform.parent.gameObject;
            _disc = button.GetComponent<Image>();
            _caption = _root.GetComponentInChildren<TextMeshProUGUI>(true);
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
                Hobby.Fishing => "Rarer fish will start to swim near the shore.",
                Hobby.Birding => "Rarer birds will start to visit.",
                Hobby.Bugs => "Rarer bugs will start to appear.",
                Hobby.Gardening => "New seeds to plant, and room for another plot!",
                Hobby.Cooking => "New recipes in the kitchen.",
                _ => "Keep it up!",
            };
            var icon = h switch
            {
                Hobby.Fishing => IconPainter.Rod, Hobby.Birding => IconPainter.Bird, Hobby.Writing => IconPainter.Book,
                Hobby.Sewing => IconPainter.Shirt, Hobby.Cats => IconPainter.Heart, _ => IconPainter.Star,
            };
            CardPopup.Show(name + " level " + level + "!", more, icon);
        }

        void Press()
        {
            var f = Fishing.Instance;
            if (f == null) return;
            if (f.Now == Fishing.State.Idle) f.Cast();
            else f.Reel();
        }

        void Flash()
        {
            _flashUntil = Time.time + 0.9f;
            _root.transform.DOKill();
            _root.transform.localScale = Vector3.one;
            _root.transform.DOPunchScale(new Vector3(0.25f, 0.25f, 0f), 0.4f, 8, 0.6f);
        }

        void Update()
        {
            var f = Fishing.Instance;
            bool show = f != null && (f.Now != Fishing.State.Idle || f.AtShore) && !UiScreen.AnyOpen;
            if (_root.activeSelf != show) _root.SetActive(show);
            if (!show) return;
            bool fishing = f.Now != Fishing.State.Idle;
            _caption.text = fishing ? "Reel!" : "Fish";
            _disc.color = Time.time < _flashUntil ? UiKit.Primary : fishing ? UiKit.Hex("3E9C8F") : UiKit.Spring;
        }
    }
}
