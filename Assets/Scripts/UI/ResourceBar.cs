using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The counters across the top of the screen: wood, gold, gems, and how many capybaras you own.
    /// The original puts everything in one thin bar at the very top and nothing anywhere else, so
    /// this does the same — it keeps the island unobstructed in both orientations.
    ///
    /// Driven by <see cref="ResourceStore.Changed"/> rather than polled, so a counter only redraws
    /// when its number actually moves.
    ///
    /// Labels are TextMeshPro, which stays crisp at any zoom and DPI - these counters have to
    /// read on a phone as well as a desktop.
    /// </summary>
    public class ResourceBar : MonoBehaviour
    {
        [SerializeField] TMP_Text woodLabel;
        [SerializeField] TMP_Text goldLabel;
        [SerializeField] TMP_Text gemLabel;
        [SerializeField] TMP_Text capyLabel;

        [Tooltip("How long a counter takes to count up to a new value, in seconds.")]
        [SerializeField] float countUpSeconds = 0.35f;

        static ResourceBar _instance;
        Transform _bumping;
        float _bumpTimer;

        ResourceStore _store;
        int _shownWood, _shownGold, _shownGem;
        int _targetWood, _targetGold, _targetGem;
        float _countTimer;

        void Awake() => _instance = this;

        /// <summary>
        /// Where a counter sits, as a world point at depth <paramref name="z"/> — the target of a
        /// resource flying up from the island. Falls back to the top of the screen.
        /// </summary>
        public static Vector3 CounterWorldPosition(ResourceKind kind, float z)
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.zero;

            Vector2 screen = new Vector2(Screen.width * 0.5f, Screen.height);
            var label = _instance != null ? _instance.LabelFor(kind) : null;
            if (label != null)
            {
                var canvas = label.canvas;
                var uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                screen = RectTransformUtility.WorldToScreenPoint(uiCam, label.rectTransform.position);
            }

            var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z - cam.transform.position.z));
            return new Vector3(w.x, w.y, z);
        }

        /// <summary>A little hop on a counter as something lands in it.</summary>
        public static void Bump(ResourceKind kind)
        {
            if (_instance == null) return;
            var label = _instance.LabelFor(kind);
            if (label == null) return;
            if (_instance._bumping != null) _instance._bumping.localScale = Vector3.one;
            _instance._bumping = label.transform;
            _instance._bumpTimer = 0.25f;
        }

        TMP_Text LabelFor(ResourceKind kind) => kind switch
        {
            ResourceKind.Wood => woodLabel,
            ResourceKind.Gold => goldLabel,
            _ => gemLabel,
        };

        void OnEnable()
        {
            _store = ResourceStore.Instance != null ? ResourceStore.Instance : FindFirstObjectByType<ResourceStore>();
            if (_store == null) return;

            _store.Changed += OnChanged;
            _shownWood = _targetWood = _store.Get(ResourceKind.Wood);
            _shownGold = _targetGold = _store.Get(ResourceKind.Gold);
            _shownGem = _targetGem = _store.Get(ResourceKind.Gem);
            Redraw();
        }

        void OnDisable()
        {
            if (_store != null) _store.Changed -= OnChanged;
        }

        void OnChanged(ResourceKind kind, int value)
        {
            switch (kind)
            {
                case ResourceKind.Wood: _targetWood = value; break;
                case ResourceKind.Gold: _targetGold = value; break;
                case ResourceKind.Gem: _targetGem = value; break;
            }
            // The island's own numbers arriving as the save loads (over the scene's starting 1000
            // wood and 1500 gold) are shown as they are, not counted down to.
            if (Time.timeSinceLevelLoad < LoadingSeconds)
            {
                _shownWood = _targetWood; _shownGold = _targetGold; _shownGem = _targetGem;
                Redraw();
            }
        }

        const float LoadingSeconds = 1.5f;

        void Update()
        {
            if (_bumping != null)
            {
                _bumpTimer -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(_bumpTimer / 0.25f);
                _bumping.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.25f);
                if (_bumpTimer <= 0f) { _bumping.localScale = Vector3.one; _bumping = null; }
            }

            // Ticking the number up is a small thing that makes earning feel like earning. On real
            // time, so a paused game (a card, a movie) never leaves a counter stuck halfway.
            bool changed = false;
            _countTimer += Time.unscaledDeltaTime;
            if (_countTimer >= countUpSeconds / 12f)
            {
                _countTimer = 0f;
                changed |= Step(ref _shownWood, _targetWood);
                changed |= Step(ref _shownGold, _targetGold);
                changed |= Step(ref _shownGem, _targetGem);
            }

            if (capyLabel != null)
            {
                // On a friend's island the workers here are theirs, not yours.
                string text = Visit.Active ? "–" : FindObjectsByType<Worker>(FindObjectsSortMode.None).Length.ToString();
                if (capyLabel.text != text) { capyLabel.text = text; }
            }

            if (changed) Redraw();
        }

        static bool Step(ref int shown, int target)
        {
            if (shown == target) return false;
            int diff = target - shown;
            // Move a twelfth of the way each tick, at least 1, so big jumps settle quickly.
            int step = Mathf.Max(1, Mathf.Abs(diff) / 12);
            shown += diff > 0 ? step : -step;
            if ((diff > 0 && shown > target) || (diff < 0 && shown < target)) shown = target;
            return true;
        }

        void Redraw()
        {
            if (woodLabel != null) woodLabel.text = _shownWood.ToString();
            if (goldLabel != null) goldLabel.text = _shownGold.ToString();
            if (gemLabel != null) gemLabel.text = _shownGem.ToString();
        }
    }
}
