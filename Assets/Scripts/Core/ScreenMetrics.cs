using System;
using UnityEngine;

namespace CapyIsland.Core
{
    /// <summary>
    /// Single source of truth for screen size and orientation changes.
    /// Unity fires no reliable event for a mid-session rotation on every platform, so we poll
    /// cheaply and broadcast once when something actually changes. Camera and UI both listen
    /// here instead of each running their own detection.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ScreenMetrics : MonoBehaviour
    {
        public static ScreenMetrics Instance { get; private set; }

        /// <summary>Raised after the screen size, orientation or safe area changes.</summary>
        public static event Action Changed;

        public static int Width => Screen.width;
        public static int Height => Screen.height;
        public static float Aspect => (float)Screen.width / Mathf.Max(1, Screen.height);
        public static bool IsPortrait => Screen.height >= Screen.width;
        public static Rect SafeArea => Screen.safeArea;

        int _lastWidth;
        int _lastHeight;
        Rect _lastSafeArea;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Capture();
        }

        void Update()
        {
            if (Screen.width == _lastWidth && Screen.height == _lastHeight && Screen.safeArea == _lastSafeArea)
                return;

            Capture();
            Changed?.Invoke();
        }

        void Capture()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            _lastSafeArea = Screen.safeArea;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Guarantees a ScreenMetrics exists before anything queries it, so scenes don't have to
        /// remember to add it. Cheap: one GameObject for the lifetime of the app.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[ScreenMetrics]");
            go.AddComponent<ScreenMetrics>();
        }
    }
}
