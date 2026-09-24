using UnityEngine;

namespace CapyIsland.CameraRig
{
    /// <summary>
    /// Borrows the camera for a moment to show something happening away from the king — a bridge
    /// rising, a quest reward, a new island's arrivals — then hands it back. The original jumps the
    /// camera and flashes white; a glide keeps the player oriented instead.
    /// </summary>
    public static class CameraFocus
    {
        static Vector2 _target;
        static float _until = -1f;

        /// <summary>Glides the camera to <paramref name="unityPoint"/> and holds it there for a while.</summary>
        public static void Show(Vector2 unityPoint, float seconds)
        {
            _target = unityPoint;
            _until = Time.time + seconds;
        }

        /// <summary>The point being shown, if the camera is currently borrowed.</summary>
        public static bool TryGet(out Vector2 target)
        {
            target = _target;
            return Time.time < _until;
        }

        public static void Release() => _until = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => _until = -1f;
    }
}
