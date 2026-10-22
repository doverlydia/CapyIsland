using System;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The account window from Settings (CloudSync): make an account — Google, or a username and
    /// password, for the island you're on, so another device can open it — or sign in to one made
    /// elsewhere. The opening asks too (<see cref="ShowWelcome"/>): there it says plainly that an
    /// island without an account lives only on this device, and you can still go on as a guest.
    /// </summary>
    public class AccountScreen : MonoBehaviour
    {
        const string ObjectName = "[Account]";

        bool _signIn;
        TMP_InputField _user, _pass;
        TextMeshProUGUI _status;
        Button _go, _google;
        bool _working;
        RectTransform _root, _card;

        /// <summary>From the opening: what happens next, once you have an account or chose to go on without one.</summary>
        Action _welcomeDone;

        /// <summary>Gives this island a username and password.</summary>
        public static void ShowMake() => Open(false);

        /// <summary>Signs this device in to an account made on another device.</summary>
        public static void ShowSignIn() => Open(true);

        /// <summary>
        /// The opening's sign-up: make an account (or sign in to one), or continue as a guest under
        /// a warning that the island could be lost. <paramref name="done"/> runs either way, unless
        /// signing in brings another island down (the game then reloads into it).
        /// </summary>
        public static void ShowWelcome(Action done, bool signIn = false) => Open(signIn, done);

        static void Open(bool signIn, Action welcomeDone = null)
        {
            var old = GameObject.Find(ObjectName);
            if (old != null) Destroy(old);
            var go = new GameObject(ObjectName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 480;   // over Settings and every menu
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            var screen = go.AddComponent<AccountScreen>();
            screen._signIn = signIn;
            screen._welcomeDone = welcomeDone;
            screen.Build();
        }

        void Build()
        {
            var root = _root = UiKit.Rect("Root", transform);
            UiKit.Stretch(root);
            var scrim = UiKit.Image(root, "Scrim", UiKit.Rounded(4), UiKit.Scrim, true);
            UiKit.Stretch(scrim.rectTransform);

            var card = UiKit.Card(root, "Card", UiKit.Surface, 40);
            _card = card.rectTransform;
            var cr = card.rectTransform;
            cr.anchorMin = new Vector2(0.06f, 0.5f);
            cr.anchorMax = new Vector2(0.94f, 0.5f);
            cr.pivot = new Vector2(0.5f, 0.5f);
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(44, 44, 40, 40);
            v.spacing = 22f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            bool welcome = _welcomeDone != null;
            var title = UiKit.Text(card.transform, "Title", _signIn ? "Sign in" : welcome ? "Keep your island safe" : "Make an account", 48f, UiKit.Ink, true, TextAlignmentOptions.Center);
            title.overflowMode = TextOverflowModes.Overflow;
            var about = UiKit.Text(card.transform, "About", _signIn
                    ? welcome
                        ? "Sign in, and the island you already have comes back."
                        : "Open the island from your other device. If the two islands are different, you'll choose which one to keep."
                    : welcome
                        ? "Sign up, and your island is kept safe in the cloud. Lose your phone or delete the game, and it all comes back when you sign in."
                        : "Keep this island in an account, and open it on another device too.",
                28f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            about.overflowMode = TextOverflowModes.Overflow;

            // Google first, where it works (the phone and the browser; shown in the editor to see the layout).
            if (GoogleLogin.Available || Application.isEditor)
            {
                var googleRow = UiKit.Rect("GoogleRow", card.transform);
                var gh = googleRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                gh.childControlWidth = gh.childControlHeight = true;
                gh.childForceExpandWidth = true;
                googleRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
                _google = UiKit.Button(googleRow, "Google", "Continue with Google", UiKit.ButtonStyle.Secondary, GoGoogle, 32f);
                var or = UiKit.Text(card.transform, "Or", _signIn ? "Or with your username and password:" : "Or pick a username and password:",
                    26f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
                or.overflowMode = TextOverflowModes.Overflow;
            }

            _user = UiKit.Input(card.transform, "Username", "Username", false, 96f);
            _user.characterLimit = 20;
            _pass = UiKit.Input(card.transform, "Password", "Password", false, 96f);
            _pass.characterLimit = 30;
            _pass.contentType = TMP_InputField.ContentType.Password;
            _pass.ForceLabelUpdate();
            if (!_signIn)
            {
                var rules = UiKit.Text(card.transform, "Rules", "8 to 30 characters, with a capital letter, a small letter, a number and a symbol. Write it down: it can't be reset.", 24f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
                rules.overflowMode = TextOverflowModes.Overflow;
            }

            _status = UiKit.Text(card.transform, "Status", "", 28f, UiKit.Danger, true, TextAlignmentOptions.Center);
            _status.overflowMode = TextOverflowModes.Overflow;

            var row = UiKit.Rect("Buttons", card.transform);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            if (!welcome) UiKit.Button(row, "Cancel", "Cancel", UiKit.ButtonStyle.Secondary, Close, 32f);
            _go = UiKit.Button(row, "Go", _signIn ? "Sign in" : welcome ? "Sign up" : "Make account", UiKit.ButtonStyle.Primary, Go, 32f);
            if (welcome) WelcomeFooter(card.transform);
        }

        /// <summary>
        /// The opening's way on: the other choice (sign in / sign up), then going on as a guest, under
        /// a plain warning of what that risks.
        /// </summary>
        void WelcomeFooter(Transform card)
        {
            var other = UiKit.Button(card, "Other", _signIn ? "New here? Sign up" : "Have an account? Sign in", UiKit.ButtonStyle.Ghost, () =>
            {
                if (_working) return;
                var done = _welcomeDone;
                _welcomeDone = null;
                Destroy(gameObject);
                Open(!_signIn, done);
            }, 28f);
            other.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;

            var line = UiKit.Image(card, "Line", UiKit.Rounded(4), UiKit.SurfaceSunk);
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 4f;

            var warning = UiKit.Text(card, "GuestWarning",
                "As a guest, your island lives only on this device. If the game is deleted or the phone is lost, your island could be lost forever.",
                26f, UiKit.Danger, false, TextAlignmentOptions.Center);
            warning.overflowMode = TextOverflowModes.Overflow;
            var guest = UiKit.Button(card, "Guest", "Continue as guest", UiKit.ButtonStyle.Secondary, () =>
            {
                if (!_working) Finish();
            }, 30f);
            guest.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 90f;
        }

        /// <summary>Closes, and from the opening, goes on with it.</summary>
        void Finish()
        {
            var done = _welcomeDone;
            _welcomeDone = null;
            Destroy(gameObject);
            done?.Invoke();
        }

        /// <summary>A phone on its side is too short for the whole card: it shrinks to fit rather than run off the screen.</summary>
        void LateUpdate()
        {
            if (_card == null || _root == null) return;
            float need = LayoutUtility.GetPreferredHeight(_card), room = _root.rect.height * 0.94f;
            float scale = need > room && need > 0f ? room / need : 1f;
            if (Mathf.Abs(_card.localScale.x - scale) > 0.001f) _card.localScale = new Vector3(scale, scale, 1f);
        }

        void Close()
        {
            if (_working) return;
            Sfx.Play(SfxKind.Tap, 0.5f);
            Destroy(gameObject);
        }

        async void Go()
        {
            if (_working) return;
            string user = _user.text.Trim(), pass = _pass.text;
            // The rules first, at once, before asking the cloud anything.
            if (!_signIn)
            {
                var why = CloudSync.CheckUsername(user) ?? CloudSync.CheckPassword(pass);
                if (why != null) { Say(why, UiKit.Danger); return; }
            }
            Busy(true);
            Say(_signIn ? "Signing in…" : "Making your account…", UiKit.InkSoft);
            string problem = _signIn ? await CloudSync.SignIn(user, pass) : await CloudSync.MakeAccount(user, pass);
            if (this == null) return;   // the game reloaded into the other island meanwhile
            Busy(false);
            if (problem != null) { Say(problem, UiKit.Danger); return; }
            Sfx.Play(SfxKind.Chest, 0.6f);
            Hints.Show(_signIn
                ? Loc.F("Signed in as {0}.", user)
                : _welcomeDone != null
                    ? Loc.F("Your island is kept safe: {0}. Sign in with it on any device.", user)
                    : Loc.F("Your island now has an account: {0}. Sign in with it on your other device.", user));
            Finish();
        }

        /// <summary>
        /// Continue with Google, from either window: a Google account new to the game takes this
        /// island; one that already has an island opens it here (CloudSync.UseGoogle).
        /// </summary>
        async void GoGoogle()
        {
            if (_working) return;
            Busy(true);
            Say("Waiting for Google…", UiKit.InkSoft);
            var answer = await GoogleLogin.Ask();
            if (this == null) return;
            if (answer.Token == null)
            {
                Busy(false);
                Say(answer.Cancelled ? "" : answer.Problem, UiKit.Danger);
                return;
            }
            Say("Signing in…", UiKit.InkSoft);
            var (problem, switched) = await CloudSync.UseGoogle(answer.Token);
            if (this == null) return;   // the game reloaded into the other island meanwhile
            Busy(false);
            if (problem != null) { Say(problem, UiKit.Danger); return; }
            Sfx.Play(SfxKind.Chest, 0.6f);
            Hints.Show(switched
                ? "Signed in with Google."
                : "Your island is now kept with your Google account. On your other device, choose Continue with Google.");
            Finish();
        }

        void Busy(bool working)
        {
            _working = working;
            _go.interactable = !working;
            if (_google != null) _google.interactable = !working;
        }

        void Say(string line, Color colour)
        {
            if (_status == null) return;
            _status.text = line;
            _status.color = colour;
        }
    }
}
