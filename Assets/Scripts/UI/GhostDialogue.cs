using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The ghost who talks you through the opening, sitting bottom-left with a speech bubble beside
    /// them — the same place and shape the original uses.
    ///
    /// Lines queue: calling <see cref="Say"/> while one is showing adds to the queue rather than
    /// cutting it off, so a burst of events during one beat still reads in order.
    /// </summary>
    public class GhostDialogue : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text label;
        [SerializeField] Image portrait;

        [Header("Timing")]
        [Tooltip("Characters revealed per second. 0 shows the whole line at once.")]
        [SerializeField] float typeSpeed = 42f;

        [Tooltip("Seconds a finished line stays up before the next one, when queued.")]
        [SerializeField] float holdSeconds = 2.2f;

        readonly Queue<string> _queue = new();
        string _current = "";
        float _revealed;
        float _holdTimer;

        public bool IsSpeaking => panel != null && panel.activeSelf;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        /// <summary>Queues a line. Safe to call repeatedly with the same text — duplicates are ignored.</summary>
        public void Say(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            if (_current == line) return;
            foreach (var queued in _queue) if (queued == line) return;

            _queue.Enqueue(line);
            if (!IsSpeaking) Advance();
        }

        /// <summary>Clears everything and hides the panel.</summary>
        public void Clear()
        {
            _queue.Clear();
            _current = "";
            if (panel != null) panel.SetActive(false);
        }

        void Advance()
        {
            if (_queue.Count == 0)
            {
                _current = "";
                if (panel != null) panel.SetActive(false);
                return;
            }

            _current = _queue.Dequeue();
            _revealed = 0f;
            _holdTimer = holdSeconds;
            if (panel != null) panel.SetActive(true);
            if (label != null) label.text = "";
        }

        void Update()
        {
            if (!IsSpeaking || label == null) return;

            if (typeSpeed <= 0f)
            {
                label.text = _current;
            }
            else if (_revealed < _current.Length)
            {
                _revealed += Time.deltaTime * typeSpeed;
                int count = Mathf.Clamp(Mathf.FloorToInt(_revealed), 0, _current.Length);
                label.text = _current.Substring(0, count);
            }

            bool finished = typeSpeed <= 0f || _revealed >= _current.Length;
            if (!finished) return;

            // Only auto-advance when something else is waiting; a lone line stays up as the
            // current instruction until the tutorial replaces it.
            if (_queue.Count == 0) return;

            _holdTimer -= Time.deltaTime;
            if (_holdTimer <= 0f) Advance();
        }
    }
}
