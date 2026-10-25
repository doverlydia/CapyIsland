using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CapyIsland.UI
{
    /// <summary>
    /// Tapping the dimmed island around a popup does what its own way out does: closes it, or
    /// turns the page of one with more to say. Sits on the popup's backdrop (or on an invisible
    /// one behind it).
    ///
    /// Only a real tap counts: pressed and let go on the backdrop, not a drag, and not just beside
    /// the card, so a finger that slips off a button near the edge doesn't throw the popup away. A
    /// tap that only puts the phone's keyboard away doesn't close it either.
    /// </summary>
    public class TapOutside : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>Canvas units around the card that still count as a near miss, not outside.</summary>
        const float NearMiss = 36f;

        RectTransform _card;
        Action _act;
        Func<bool> _allowed;
        bool _armed;
        Vector2 _pressedAt;
        float _typedAt = -1f;

        /// <summary>
        /// Makes <paramref name="backdrop"/> do <paramref name="act"/> when tapped clear of
        /// <paramref name="card"/>; with <paramref name="allowed"/>, only while it says so.
        /// </summary>
        public static TapOutside Add(Component backdrop, RectTransform card, Action act, Func<bool> allowed = null)
        {
            var tap = backdrop.gameObject.AddComponent<TapOutside>();
            tap._card = card;
            tap._act = act;
            tap._allowed = allowed;
            return tap;
        }

        // Watched every frame: by the time the backdrop hears a press, the press has already
        // taken the focus off the text box, so "is someone typing" must be asked a moment earlier.
        void Update()
        {
            if (Typing()) _typedAt = Time.unscaledTime;
        }

        public void OnPointerDown(PointerEventData e)
        {
            bool typing = Typing() || Time.unscaledTime - _typedAt < 0.25f;
            _armed = !typing && !NearCard(e.position, e.pressEventCamera);
            _pressedAt = e.position;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!_armed) return;
            _armed = false;
            var es = EventSystem.current;
            float slop = es != null ? es.pixelDragThreshold * 2f : 20f;
            if (Vector2.Distance(e.position, _pressedAt) > slop) return;   // a drag, not a tap
            if (e.pointerCurrentRaycast.gameObject != gameObject) return;   // let go somewhere else
            if (NearCard(e.position, e.pressEventCamera)) return;
            if (_allowed != null && !_allowed()) return;
            _act?.Invoke();
        }

        bool NearCard(Vector2 screen, Camera cam)
        {
            if (_card == null || !_card.gameObject.activeInHierarchy) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_card, screen, cam, out var local)) return false;
            var r = _card.rect;
            var scale = _card.lossyScale.x > 0f && transform.lossyScale.x > 0f ? transform.lossyScale.x / _card.lossyScale.x : 1f;
            float margin = NearMiss * scale;
            return local.x > r.xMin - margin && local.x < r.xMax + margin && local.y > r.yMin - margin && local.y < r.yMax + margin;
        }

        /// <summary>A text box has the keyboard: the tap is to put it away.</summary>
        static bool Typing()
        {
            if (TouchScreenKeyboard.visible) return true;
            var es = EventSystem.current;
            var selected = es != null ? es.currentSelectedGameObject : null;
            return selected != null && selected.TryGetComponent<TMP_InputField>(out var field) && field.isFocused;
        }
    }
}
