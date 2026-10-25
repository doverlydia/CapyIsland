using System.Runtime.InteropServices;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Your island as a save code. From Settings it shows the code and copies it, so you can keep
    /// it in a note; from the first screen it takes a code back (pasted, or typed into the box) and
    /// loads that island instead of starting a new one.
    ///
    /// Codes run to many thousands of characters, too many to draw in a text box on a phone, so the
    /// screen shows a short preview and the clipboard does the carrying.
    /// </summary>
    public class SaveCodeScreen : MonoBehaviour
    {
        const string ObjectName = "[SaveCode]";

        bool _import, _replacing;
        string _code;          // the code shown (export) or pasted (import)
        object _island;        // read from the pasted code, ready to load
        TextMeshProUGUI _preview, _status;
        TMP_InputField _box;
        Button _load;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CapyCopyText(string text);
        [DllImport("__Internal")] static extern void CapyPasteText(string objectName, string method);
#endif

        /// <summary>Shows your save code, ready to copy (Settings).</summary>
        public static void ShowExport() => Open(false);

        /// <summary>Asks for a save code and loads it (the first screen).</summary>
        public static void ShowImport() => Open(true);

        /// <summary>
        /// Asks for a save code and loads it; <paramref name="replacing"/> (from Settings) warns that
        /// the island you're playing gives way to the one in the code.
        /// </summary>
        public static void ShowImport(bool replacing) => Open(true, replacing);

        static void Open(bool import, bool replacing = false)
        {
            var old = GameObject.Find(ObjectName);
            if (old != null) Destroy(old);
            var go = new GameObject(ObjectName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 480;   // over the first screen and every menu
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            var screen = go.AddComponent<SaveCodeScreen>();
            screen._import = import;
            screen._replacing = replacing;
            if (!import) SaveGame.Instance?.Save();   // the code holds the island as it is right now
            screen._code = import ? "" : SaveTransfer.Export();
            screen.Build();
        }

        void Build()
        {
            var root = UiKit.Rect("Root", transform);
            UiKit.Stretch(root);
            var scrim = UiKit.Image(root, "Scrim", UiKit.Rounded(4), UiKit.Scrim, true);
            UiKit.Stretch(scrim.rectTransform);

            var card = UiKit.Card(root, "Card", UiKit.Surface, 40);
            TapOutside.Add(scrim, card.rectTransform, Close);   // a tap around it is Cancel / Done
            var cr = card.rectTransform;
            cr.anchorMin = new Vector2(0.06f, 0.5f);
            cr.anchorMax = new Vector2(0.94f, 0.5f);
            cr.pivot = new Vector2(0.5f, 0.5f);
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(44, 44, 40, 40);
            v.spacing = 24f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            var fit = card.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var title = UiKit.Text(card.transform, "Title", _import ? "Load a save" : "Your save code", 48f, UiKit.Ink, true, TextAlignmentOptions.Center);
            title.overflowMode = TextOverflowModes.Overflow;
            var about = UiKit.Text(card.transform, "About", _import
                    ? _replacing
                        ? "Paste the save code you kept in a note. The island you're on now gives way to that one, so export this one first if you want to keep it."
                        : "Paste the save code you kept in a note, and your island comes back just as you left it."
                    : "Copy it and paste it into a note somewhere safe. To bring this island back, on this phone or another device, choose \"Load a save\" on the first screen and paste it in.",
                30f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
            about.overflowMode = TextOverflowModes.Overflow;

            if (_import)
            {
                // Typed or long-pressed in; a long paste is kept and summed up rather than drawn.
                _box = UiKit.Input(card.transform, "Code", "Paste your save code here", false, 100f);
                _box.onValueChanged.AddListener(OnTyped);
            }
            else
            {
                var well = UiKit.Image(card.transform, "Preview", UiKit.Rounded(24), UiKit.SurfaceSunk);
                well.gameObject.AddComponent<LayoutElement>().preferredHeight = 100f;
                _preview = UiKit.Text(well.transform, "Text", Preview(_code), 26f, UiKit.InkSoft, false, TextAlignmentOptions.Center);
                UiKit.Stretch(_preview.rectTransform, 20f, 10f, 20f, 10f);
            }

            _status = UiKit.Text(card.transform, "Status", "", 28f, UiKit.Leaf, true, TextAlignmentOptions.Center);
            _status.overflowMode = TextOverflowModes.Overflow;

            var row = UiKit.Rect("Buttons", card.transform);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            if (_import)
            {
                UiKit.Button(row, "Cancel", "Cancel", UiKit.ButtonStyle.Secondary, Close, 32f);
                UiKit.Button(row, "Paste", "Paste", UiKit.ButtonStyle.Spring, Paste, 32f);
                _load = UiKit.Button(row, "Load", "Load", UiKit.ButtonStyle.Primary, Load, 32f);
                _load.interactable = false;
            }
            else
            {
                UiKit.Button(row, "Done", "Done", UiKit.ButtonStyle.Secondary, Close, 32f);
                UiKit.Button(row, "Copy", "Copy", UiKit.ButtonStyle.Primary, Copy, 32f);
            }
        }

        static string Preview(string code) =>
            string.IsNullOrEmpty(code) ? "" : code.Substring(0, Mathf.Min(28, code.Length)) + "…\n<size=80%>" + string.Format("{0} characters", code.Length) + "</size>";

        void Close()
        {
            Sfx.Play(SfxKind.Tap, 0.5f);
            Destroy(gameObject);
        }

        // ---------------------------------------------------------------- export

        /// <summary>Puts text on the clipboard, the browser's included.</summary>
        public static void CopyText(string text)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CapyCopyText(text);
#else
            GUIUtility.systemCopyBuffer = text;
#endif
        }

        void Copy()
        {
            CopyText(_code);
            Sfx.Play(SfxKind.Coin, 0.6f);
            Say("Copied! Now paste it into a note.", UiKit.Leaf);
        }

        // ---------------------------------------------------------------- import

        void Paste()
        {
            Sfx.Play(SfxKind.Tap, 0.5f);
#if UNITY_WEBGL && !UNITY_EDITOR
            CapyPasteText(ObjectName, nameof(OnPasted));
#else
            OnPasted(GUIUtility.systemCopyBuffer);
#endif
        }

        /// <summary>The clipboard's text arrived (at once, or later from the browser).</summary>
        public void OnPasted(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                Say("Nothing to paste. Copy your save code first, or paste it into the box.", UiKit.Danger);
                return;
            }
            Take(text);
        }

        void OnTyped(string text)
        {
            // A whole code pasted into the box: take it, and show a short summary instead.
            if (text.Length > 200) Take(text);
            else if (text.Length > 0) Take(text, keepBox: true);
        }

        void Take(string text, bool keepBox = false)
        {
            _code = text;
            if (_box != null && !keepBox)
            {
                _box.onValueChanged.RemoveListener(OnTyped);
                _box.text = Preview(text.Trim()).Replace("\n<size=80%>", " (").Replace("</size>", ")");
                _box.onValueChanged.AddListener(OnTyped);
            }
            bool ok = SaveTransfer.TryRead(_code, out _island, out var why);
            if (_load != null) _load.interactable = ok;
            if (ok) Say("Save code found. Tap Load to bring your island back.", UiKit.Leaf);
            else Say(why, UiKit.Danger);
        }

        void Load()
        {
            if (_island == null) return;
            Sfx.Play(SfxKind.Buy, 0.8f);
            IntroAudio.Stop(0f);   // leaving the first screen: its music and its hold on the game end here
            SaveTransfer.Apply(_island);
        }

        void Say(string line, Color colour)
        {
            if (_status == null) return;
            _status.text = line;
            _status.color = colour;
        }
    }
}
