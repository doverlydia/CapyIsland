using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The island's breeze. A constant gentle sway, plus gusts that roll across the island from one
    /// side to the other every so often, bending trees as the front passes and blowing leaves off
    /// them.
    ///
    /// Pure presentation — the original had no wind. Trees and flowers read it through
    /// <see cref="Sway"/>; nothing in the rules does.
    /// </summary>
    public class Wind : MonoBehaviour
    {
        public static Wind Instance { get; private set; }

        [Header("Breeze")]
        [Tooltip("Lean of the everyday breeze, in degrees.")]
        [SerializeField] float breezeLean = 1.6f;
        [SerializeField] float breezeRate = 0.45f;

        [Header("Gusts")]
        [SerializeField] Vector2 gustInterval = new Vector2(7f, 14f);
        [Tooltip("Extra lean at the gust front, in degrees.")]
        [SerializeField] float gustLean = 7f;
        [Tooltip("How fast the gust front crosses the island, in Unity units per second.")]
        [SerializeField] float gustSpeed = 9f;
        [Tooltip("Width of the gust front, in Unity units.")]
        [SerializeField] float gustWidth = 5f;
        [SerializeField] float islandHalfWidth = 16f;

        float _nextGust;
        float _gustFront = float.NegativeInfinity;
        float _gustStrength;
        int _gustDirection = 1;

        /// <summary>Which way the wind currently blows along X: +1 right, -1 left.</summary>
        public int Direction => _gustDirection;

        void Awake()
        {
            Instance = this;
            _nextGust = Time.time + Random.Range(3f, 6f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Time.time >= _nextGust && !GustActive)
            {
                _gustDirection = Random.value < 0.75f ? 1 : -1;   // mostly a prevailing wind
                _gustFront = -_gustDirection * (islandHalfWidth + gustWidth);
                _gustStrength = Random.Range(0.6f, 1f);
            }

            if (GustActive)
            {
                _gustFront += _gustDirection * gustSpeed * Time.deltaTime;
                if (!GustActive) _nextGust = Time.time + Random.Range(gustInterval.x, gustInterval.y);
            }
        }

        bool GustActive => !float.IsNegativeInfinity(_gustFront)
                           && Mathf.Abs(_gustFront) <= islandHalfWidth + gustWidth * 1.01f;

        /// <summary>0..1: how hard the gust is pushing at this spot right now.</summary>
        public float GustAt(Vector2 position)
        {
            if (!GustActive) return 0f;
            float d = Mathf.Abs(position.x - _gustFront) / gustWidth;
            if (d >= 1f) return 0f;
            float falloff = 1f - d;
            return falloff * falloff * (3f - 2f * falloff) * _gustStrength;
        }

        /// <summary>
        /// Lean in degrees for something at <paramref name="position"/>. Negative leans right in
        /// Unity's Z rotation, so the sign is flipped for the wind direction.
        /// </summary>
        public float LeanAt(Vector2 position, float phase)
        {
            float breeze = Mathf.Sin(Time.time * breezeRate * Mathf.PI * 2f + phase) * breezeLean
                         + Mathf.Sin(Time.time * breezeRate * 2.7f + phase * 1.7f) * breezeLean * 0.35f;
            float gust = GustAt(position) * gustLean;
            return -(_gustDirection * gust) + breeze;
        }
    }
}
