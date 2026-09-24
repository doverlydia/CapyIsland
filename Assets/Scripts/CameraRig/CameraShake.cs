using UnityEngine;

namespace CapyIsland.CameraRig
{
    /// <summary>A short, decaying camera shake — for a bomb going off.</summary>
    public static class CameraShake
    {
        static float _amplitude;
        static float _until;
        static float _duration = 0.1f;

        public static void Kick(float amplitude, float seconds)
        {
            _amplitude = amplitude;
            _duration = Mathf.Max(0.05f, seconds);
            _until = Time.time + _duration;
        }

        /// <summary>The offset to add to the camera this frame.</summary>
        public static Vector2 Offset
        {
            get
            {
                float left = _until - Time.time;
                if (left <= 0f) return Vector2.zero;
                float k = left / _duration * _amplitude;
                return new Vector2(Mathf.Sin(Time.time * 71f), Mathf.Cos(Time.time * 59f)) * k;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => _until = 0f;
    }
}
