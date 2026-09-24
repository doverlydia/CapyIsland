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

        ResourceStore _store;
        int _shownWood, _shownGold, _shownGem;
        int _targetWood, _targetGold, _targetGem;
        float _countTimer;

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
        }

        void Update()
        {
            // Ticking the number up is a small thing that makes earning feel like earning.
            bool changed = false;
            _countTimer += Time.deltaTime;
            if (_countTimer >= countUpSeconds / 12f)
            {
                _countTimer = 0f;
                changed |= Step(ref _shownWood, _targetWood);
                changed |= Step(ref _shownGold, _targetGold);
                changed |= Step(ref _shownGem, _targetGem);
            }

            if (capyLabel != null)
            {
                int owned = FindObjectsByType<Worker>(FindObjectsSortMode.None).Length;
                string text = owned.ToString();
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
