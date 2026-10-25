using System;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// A small window for giving something a name: a picture of it, a box with its name now, and
    /// Save or Cancel. (Naming your cat.)
    /// </summary>
    public class NamePrompt : MonoBehaviour
    {
        const string ObjectName = "[NamePrompt]";

        TMP_InputField _field;
        Action<string> _onSave;

        public static void Ask(string title, Sprite picture, string current, int limit, Action<string> onSave)
        {
            var old = GameObject.Find(ObjectName);
            if (old != null) Destroy(old);
            var go = new GameObject(ObjectName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 480;   // over every menu and the speech bubble
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            var prompt = go.AddComponent<NamePrompt>();
            prompt._onSave = onSave;
            prompt.Build(title, picture, current, limit);
        }

        void Build(string title, Sprite picture, string current, int limit)
        {
            var root = UiKit.Rect("Root", transform);
            UiKit.Stretch(root);
            var scrim = UiKit.Image(root, "Scrim", UiKit.Rounded(4), UiKit.Scrim, true);
            UiKit.Stretch(scrim.rectTransform);

            var card = UiKit.Card(root, "Card", UiKit.Surface, 40);
            TapOutside.Add(scrim, card.rectTransform, Close);   // a tap around it is Cancel
            var cr = card.rectTransform;
            cr.anchorMin = new Vector2(0.08f, 0.5f);
            cr.anchorMax = new Vector2(0.92f, 0.5f);
            cr.pivot = new Vector2(0.5f, 0.5f);
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(44, 44, 40, 40);
            v.spacing = 22f;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var heading = UiKit.Text(card.transform, "Title", title, 46f, UiKit.Ink, true, TextAlignmentOptions.Center);
            heading.overflowMode = TextOverflowModes.Overflow;

            if (picture != null)
            {
                var pic = UiKit.Image(card.transform, "Picture", picture, Color.white);
                pic.preserveAspect = true;
                pic.gameObject.AddComponent<LayoutElement>().preferredHeight = 200f;
            }

            _field = UiKit.Input(card.transform, "Name", "A name", false, 96f);
            _field.characterLimit = limit;
            _field.text = current ?? "";
            _field.onSubmit.AddListener(_ => Save());

            var row = UiKit.Rect("Buttons", card.transform);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            UiKit.Button(row, "Cancel", "Cancel", UiKit.ButtonStyle.Secondary, Close, 32f);
            UiKit.Button(row, "Save", "Save", UiKit.ButtonStyle.Primary, Save, 32f);
        }

        void Save()
        {
            var cb = _onSave;
            _onSave = null;
            string name = _field != null ? _field.text : null;
            Destroy(gameObject);
            cb?.Invoke(name);
        }

        void Close()
        {
            Sfx.Play(SfxKind.Tap, 0.5f);
            _onSave = null;
            Destroy(gameObject);
        }
    }
}
