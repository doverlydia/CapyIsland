using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// The private server window (Neighbours): type a code you were sent, or make a new server
    /// and get its code to share (PrivateServer).
    /// </summary>
    public class PrivateServerScreen : MonoBehaviour
    {
        const string ObjectName = "[PrivateServer]";

        TMP_InputField _code;
        TextMeshProUGUI _status;
        Button _join, _make;
        bool _working;

        public static void Show()
        {
            var old = GameObject.Find(ObjectName);
            if (old != null) Destroy(old);
            var go = new GameObject(ObjectName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 480;   // over Neighbours and every menu
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<PrivateServerScreen>().Build();
        }

        void Build()
        {
            var root = UiKit.Rect("Root", transform);
            UiKit.Stretch(root);
            var scrim = UiKit.Image(root, "Scrim", UiKit.Rounded(4), UiKit.Scrim, true);
            UiKit.Stretch(scrim.rectTransform);

            var card = UiKit.Card(root, "Card", UiKit.Surface, 40);
            TapOutside.Add(scrim, card.rectTransform, Close);   // a tap around it is Cancel
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

            var title = UiKit.Text(card.transform, "Title", "Private server", 48f, UiKit.Ink, true, TextAlignmentOptions.Center);
            title.overflowMode = TextOverflowModes.Overflow;
            var about = UiKit.Text(card.transform, "About",
                "A server just for you and your friends: everyone with the same code lands on it. Make one and send its code, or type a code you were sent.",
                28f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            about.overflowMode = TextOverflowModes.Overflow;

            var row = UiKit.Rect("JoinRow", card.transform);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            _code = UiKit.Input(row, "Code", "Server code, like K7M-2QX", false, 96f);
            _code.characterLimit = 9;
            _code.GetComponent<LayoutElement>().flexibleWidth = 1f;
            _join = UiKit.Button(row, "Join", "Join", UiKit.ButtonStyle.Primary, Join, 32f);
            var jle = _join.transform.parent.gameObject.AddComponent<LayoutElement>();
            jle.preferredWidth = 170f; jle.preferredHeight = 96f;

            var or = UiKit.Text(card.transform, "Or", "Or", 26f, UiKit.InkSoft, true, TextAlignmentOptions.Center);
            or.overflowMode = TextOverflowModes.Overflow;
            var makeRow = UiKit.Rect("MakeRow", card.transform);
            var mh = makeRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            mh.childControlWidth = mh.childControlHeight = true;
            mh.childForceExpandWidth = true;
            makeRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            _make = UiKit.Button(makeRow, "Make", "Make my own server", UiKit.ButtonStyle.Spring, Make, 32f);

            _status = UiKit.Text(card.transform, "Status", "", 28f, UiKit.Danger, true, TextAlignmentOptions.Center);
            _status.overflowMode = TextOverflowModes.Overflow;

            var cancelRow = UiKit.Rect("CancelRow", card.transform);
            var ch = cancelRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            ch.childControlWidth = ch.childControlHeight = true;
            ch.childForceExpandWidth = true;
            cancelRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 88f;
            UiKit.Button(cancelRow, "Cancel", "Cancel", UiKit.ButtonStyle.Secondary, Close, 30f);
        }

        void Close()
        {
            if (_working) return;
            Sfx.Play(SfxKind.Tap, 0.5f);
            Destroy(gameObject);
        }

        void Join() => Go(_code.text, made: false);

        void Make() => Go(PrivateServer.NewCode(), made: true);

        async void Go(string code, bool made)
        {
            if (_working) return;
            if (!made && PrivateServer.Clean(code) == null) { Say("A server code has 6 letters and numbers, like K7M-2QX.", UiKit.Danger); return; }
            _working = true;
            _join.interactable = _make.interactable = false;
            Say(made ? "Making your server…" : "Joining…", UiKit.InkSoft);
            string problem = await PrivateServer.Join(code);
            if (this == null) return;
            _working = false;
            _join.interactable = _make.interactable = true;
            if (problem != null) { Say(problem, UiKit.Danger); return; }
            Sfx.Play(SfxKind.Chest, 0.6f);
            string pretty = PrivateServer.Pretty(PrivateServer.Code);
            if (made) SaveCodeScreen.CopyText(pretty);
            Hints.Answer(made
                ? Loc.F("Your server is ready: {0}. The code is copied, send it to your friends!", pretty)
                : Loc.F("You're on server {0}.", pretty));
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
