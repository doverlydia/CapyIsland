using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// "Continue with Google": asks Google who the player is and returns an ID token for
    /// CloudSync to hand to Unity Authentication. On the phone it's Google's own account sheet
    /// (Plugins/Android/GoogleLogin.androidlib), in the browser Google's button on a small card
    /// (Plugins/WebGL/GoogleLogin.jslib). Elsewhere, the editor included, it isn't offered.
    /// </summary>
    public static class GoogleLogin
    {
        /// <summary>
        /// The game's OAuth "Web application" client in Google Cloud (project Capy Island). Both
        /// the phone and the browser ask for tokens addressed to it, and Unity's Google sign-in
        /// is set to accept them. Not a secret.
        /// </summary>
        const string WebClientId = "";

        /// <summary>What came back: a token, or why not (Cancelled when the player just closed it).</summary>
        public struct Result
        {
            public string Token, Problem;
            public bool Cancelled;
        }

        /// <summary>Google sign-in can be offered on this device.</summary>
        public static bool Available =>
            !string.IsNullOrEmpty(WebClientId) &&
            (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WebGLPlayer);

        static TaskCompletionSource<Result> _waiting;

        public static Task<Result> Ask()
        {
            _waiting?.TrySetResult(new Result { Cancelled = true });
            var waiting = _waiting = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!Available) Finish(new Result { Problem = "Google sign-in isn't available here." });
#if UNITY_ANDROID && !UNITY_EDITOR
            else AskAndroid();
#elif UNITY_WEBGL && !UNITY_EDITOR
            else AskBrowser();
#endif
            return waiting.Task;
        }

        static void Finish(Result result)
        {
            var waiting = _waiting;
            _waiting = null;
            waiting?.TrySetResult(result);
        }

        static Result FromError(string kind, string message)
        {
            if (kind == "cancel") return new Result { Cancelled = true };
            Debug.Log("[GoogleLogin] " + kind + ": " + message);
            return new Result
            {
                Problem = kind == "none" ? "Add a Google account to this phone first, in its settings."
                    : kind == "offline" ? "Can't reach the internet right now. Try again in a moment."
                    : "Google sign-in didn't work. Try again in a moment.",
            };
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static Listener _listener;   // kept alive until Java answers

        static void AskAndroid()
        {
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var login = new AndroidJavaClass("com.capyisland.google.GoogleLogin");
                _listener = new Listener();
                login.CallStatic("signIn", activity, WebClientId, _listener);
            }
            catch (System.Exception e)
            {
                Finish(FromError("error", e.Message));
            }
        }

        /// <summary>Java's answer. It arrives on Android's UI thread; the awaiting code resumes on Unity's.</summary>
        class Listener : AndroidJavaProxy
        {
            public Listener() : base("com.capyisland.google.GoogleLogin$Listener") { }

            [UnityEngine.Scripting.Preserve]
            public void onToken(string idToken) => Finish(new Result { Token = idToken });

            [UnityEngine.Scripting.Preserve]
            public void onError(string kind, string message) => Finish(FromError(kind, message));
        }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void CapyGoogleLogin(string clientId, string receiver, string cancel, string locale);

        static void AskBrowser()
        {
            var receiver = Receiver.Make();
            CapyGoogleLogin(WebClientId, receiver.name, UI.Loc.T("Cancel"), UI.Loc.Current == UI.Language.Hebrew ? "he" : "en");
        }

        /// <summary>The page answers through SendMessage, which needs an object with a name.</summary>
        class Receiver : MonoBehaviour
        {
            const string ObjectName = "[GoogleLogin]";

            public static Receiver Make()
            {
                var go = GameObject.Find(ObjectName);
                if (go == null) { go = new GameObject(ObjectName); Object.DontDestroyOnLoad(go); }
                var receiver = go.GetComponent<Receiver>();
                return receiver != null ? receiver : go.AddComponent<Receiver>();
            }

            [UnityEngine.Scripting.Preserve]
            public void OnGoogleToken(string idToken) => Finish(new Result { Token = idToken });

            [UnityEngine.Scripting.Preserve]
            public void OnGoogleError(string kind) => Finish(FromError(kind, kind));
        }
#endif
    }
}
