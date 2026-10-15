using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The account window from Settings (CloudSync): make an account — a username and password for
    /// the island you're on, so another device can open it — or sign in to one made elsewhere.
    /// </summary>
    public class AccountScreen : MonoBehaviour
    {
        const string ObjectName = "[Account]";

        bool _signIn;
        TMP_InputField _user, _pass;
        TextMeshProUGUI _status;
        Button _go;
        bool _working;

        /// <summary>Gives this island a username and password.</summary>
        public static void ShowMake() => Open(false);

        /// <summary>Signs this device in to an account made on another device.</summary>
        public static void ShowSignIn() => Open(true);

        static void Open(bool signIn)
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
            screen.Build();
        }

        void Build()
        {
            var root = UiKit.Rect("Root", transform);
            UiKit.Stretch(root);
            var scrim = UiKit.Image(root, "Scrim", UiKit.Rounded(4), UiKit.Scrim, true);
            UiKit.Stretch(scrim.rectTransform);

            var card = UiKit.Card(root, "Card", UiKit.Surface, 40);
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

            var title = UiKit.Text(card.transform, "Title", _signIn ? "Sign in" : "Make an account", 48f, UiKit.Ink, true, TextAlignmentOptions.Center);
            title.overflowMode = TextOverflowModes.Overflow;
            var about = UiKit.Text(card.transform, "About", _signIn
                    ? "Sign in with the username and password you made on your other device. If the islands are different, you'll choose which one to keep."
                    : "Pick a username and password for this island. Then sign in with them on another device, and your island is there too. Write the password down: it can't be reset.",
                28f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            about.overflowMode = TextOverflowModes.Overflow;

            _user = UiKit.Input(card.transform, "Username", "Username", false, 96f);
            _user.characterLimit = 20;
            _pass = UiKit.Input(card.transform, "Password", "Password", false, 96f);
            _pass.characterLimit = 30;
            _pass.contentType = TMP_InputField.ContentType.Password;
            _pass.ForceLabelUpdate();
            if (!_signIn)
            {
                var rules = UiKit.Text(card.transform, "Rules", "8 to 30 characters, with a capital letter, a small letter, a number and a symbol.", 24f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
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
            UiKit.Button(row, "Cancel", "Cancel", UiKit.ButtonStyle.Secondary, Close, 32f);
            _go = UiKit.Button(row, "Go", _signIn ? "Sign in" : "Make account", UiKit.ButtonStyle.Primary, Go, 32f);
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
            _working = true;
            _go.interactable = false;
            Say(_signIn ? "Signing in…" : "Making your account…", UiKit.InkSoft);
            string problem = _signIn ? await CloudSync.SignIn(user, pass) : await CloudSync.MakeAccount(user, pass);
            if (this == null) return;   // the game reloaded into the other island meanwhile
            _working = false;
            _go.interactable = true;
            if (problem != null) { Say(problem, UiKit.Danger); return; }
            Sfx.Play(SfxKind.Chest, 0.6f);
            Hints.Show(_signIn
                ? Loc.F("Signed in as {0}.", user)
                : Loc.F("Your island now has an account: {0}. Sign in with it on your other device.", user));
            Destroy(gameObject);
        }

        void Say(string line, Color colour)
        {
            if (_status == null) return;
            _status.text = line;
            _status.color = colour;
        }
    }
}
