using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Black, behind the whole opening. On a first launch it covers the island from the very first
    /// frame, under Old Yuzu waking and under the movie, so neither the hand-over between them nor
    /// the movie's own fades ever show the game. When the movie ends it lifts slowly, and the
    /// island fades up out of the dark.
    /// </summary>
    public class IntroCurtain : MonoBehaviour
    {
        static IntroCurtain _instance;
        Image _black;

        /// <summary>Covers the screen at once (TutorialDirector, as it starts the opening).</summary>
        public static void Drop()
        {
            if (_instance != null) return;
            var go = new GameObject("[IntroCurtain]", typeof(Canvas));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 440;   // under the awakening (460) and the movie (450), over everything else
            _instance = go.AddComponent<IntroCurtain>();
            var img = new GameObject("Black", typeof(RectTransform), typeof(Image));
            img.transform.SetParent(go.transform, false);
            var r = (RectTransform)img.transform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            _instance._black = img.GetComponent<Image>();
            _instance._black.color = Color.black;
            _instance._black.raycastTarget = true;   // nothing on the island is tappable through it
        }

        /// <summary>Fades away to the island, then is gone.</summary>
        public static void Lift(float seconds = 1.6f)
        {
            if (_instance == null) return;
            var me = _instance;
            _instance = null;
            me._black.raycastTarget = false;
            me._black.DOFade(0f, seconds).SetEase(Ease.InOutSine).SetUpdate(true)
                .OnComplete(() => { if (me != null) Destroy(me.gameObject); });
        }

        void OnDestroy()
        {
            if (_black != null) DOTween.Kill(_black);
            if (_instance == this) _instance = null;
        }
    }
}
