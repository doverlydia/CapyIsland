using System;
using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// While you knock on a real friend's door: who you're waiting for, how long you'll wait, and
    /// a Cancel that takes the knock back (their "let them in?" question goes away too). Live
    /// shows it and takes it down when they answer.
    /// </summary>
    public class KnockPanel : MonoBehaviour
    {
        const string ObjectName = "[KnockPanel]";

        static KnockPanel _shown;

        TextMeshProUGUI _left;
        float _until;
        Action _onCancel;

        public static void Show(FriendProfile who, float seconds, Action onCancel)
        {
            Hide();
            var go = new GameObject(ObjectName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 470;   // over the HUD, under questions that need an answer
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            _shown = go.AddComponent<KnockPanel>();
            _shown._until = Time.realtimeSinceStartup + seconds;
            _shown._onCancel = onCancel;
            _shown.Build(who);
        }

        public static void Hide()
        {
            if (_shown != null) Destroy(_shown.gameObject);
            _shown = null;
        }

        void Build(FriendProfile who)
        {
            var root = UiKit.Rect("Root", transform);
            UiKit.Stretch(root);
            root.gameObject.AddComponent<SafeAreaFitter>();

            var card = UiKit.Card(root, "Card", UiKit.Surface, 36);
            var cr = card.rectTransform;
            cr.anchorMin = new Vector2(0.06f, 0.74f);   // up top, clear of your capybara
            cr.anchorMax = new Vector2(0.94f, 0.74f);
            cr.pivot = new Vector2(0.5f, 0.5f);
            var h = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(30, 30, 28, 28);
            h.spacing = 24f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UiKit.Avatar(card.transform, who, 120f);

            var col = UiKit.Rect("Words", card.transform);
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 6f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var line = UiKit.Text(col, "Line", Loc.F("Waiting for {0} to let you in…", who.name), 32f, UiKit.Ink, true);
            line.overflowMode = TextOverflowModes.Overflow;
            _left = UiKit.Text(col, "Left", "", 26f, UiKit.InkSoft, false);
            _left.overflowMode = TextOverflowModes.Overflow;

            var cancel = UiKit.Button(card.transform, "Cancel", "Cancel", UiKit.ButtonStyle.Secondary, Cancel, 30f);
            var le = cancel.transform.parent.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 190f; le.preferredHeight = 88f;

            UiKit.PopIn(card.transform);
            Tick();
        }

        void Update() => Tick();

        void Tick()
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(_until - Time.realtimeSinceStartup));
            if (_left != null) _left.text = Loc.F("You can cancel any time ({0}s)", seconds);
        }

        void Cancel()
        {
            Sfx.Play(SfxKind.Tap, 0.5f);
            var cb = _onCancel;
            _onCancel = null;
            Hide();
            cb?.Invoke();
        }

        void OnDestroy()
        {
            if (_shown == this) _shown = null;
        }
    }
}
