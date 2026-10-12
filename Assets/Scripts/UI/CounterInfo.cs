using CapyIsland.Game;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CapyIsland.UI
{
    /// <summary>
    /// Press and hold a counter on the resource bar to see what it is and how it is doing — wood
    /// and gold coming in a minute, how many capybaras are working. Let go and it is gone, so it
    /// never sits over the island.
    /// </summary>
    public class CounterInfo : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float HoldSeconds = 0.35f;

        static RectTransform _card;
        static TextMeshProUGUI _title, _body;

        string _kind;
        bool _down;
        float _downAt;

        /// <summary>Makes a resource bar counter holdable; the counter's name (Wood, Gold, Gem, Capy) says which.</summary>
        public static void Add(RectTransform counter, Image hitArea)
        {
            if (counter == null || counter.GetComponent<CounterInfo>() != null) return;
            if (hitArea != null) hitArea.raycastTarget = true;
            counter.gameObject.AddComponent<CounterInfo>()._kind = counter.name;
        }

        public void OnPointerDown(PointerEventData e)
        {
            _down = true;
            _downAt = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();
        void OnDisable() => Release();

        void Release()
        {
            _down = false;
            if (_card != null && _owner == this) _card.gameObject.SetActive(false);
        }

        static CounterInfo _owner;

        void Update()
        {
            if (!_down || (_card != null && _card.gameObject.activeSelf && _owner == this)) return;
            if (Time.unscaledTime - _downAt < HoldSeconds) return;
            Show();
        }

        void Show()
        {
            Build((RectTransform)transform.parent.parent);
            _owner = this;
            Describe(_kind, out var title, out var body);
            _title.text = title;
            _body.text = body;

            // Just under the counter, kept on screen.
            var me = (RectTransform)transform;
            var corners = new Vector3[4];
            me.GetWorldCorners(corners);
            var parent = (RectTransform)_card.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[3]) * 0.5f), null, out var below);
            float half = _card.sizeDelta.x * 0.5f, edge = parent.rect.width * 0.5f - 16f;
            _card.anchoredPosition = new Vector2(Mathf.Clamp(below.x, -edge + half, edge - half), below.y - 12f);
            _card.SetAsLastSibling();
            _card.gameObject.SetActive(true);
            UiKit.PopIn(_card, 0.9f);
            Sfx.Play(SfxKind.Tap, 0.3f);
        }

        static void Describe(string kind, out string title, out string body)
        {
            var store = ResourceStore.Instance;
            switch (kind)
            {
                case "Wood":
                    title = "Wood";
                    body = Rate(store, ResourceKind.Wood, "Capybaras chop it at a sawmill.");
                    break;
                case "Gold":
                    title = "Gold";
                    body = Rate(store, ResourceKind.Gold, "Capybaras dig it at a mine.");
                    break;
                case "Gem":
                    title = "Gems";
                    body = "Rare treasure from quests, daily gifts and hobbies.";
                    break;
                case "Clock":
                    title = GameClock.Now switch
                    {
                        DayParts.Dawn => "Dawn", DayParts.Dusk => "Dusk", DayParts.Night => "Night", _ => "Daytime",
                    };
                    body = "A day on the island lasts 24 minutes.\nSome fish, birds and bugs only come out at certain times. Your journal says when.";
                    break;
                default:
                    title = "Capybaras";
                    body = Workers();
                    break;
            }
        }

        static string Rate(ResourceStore store, ResourceKind kind, string how)
        {
            int perMinute = store != null ? store.PerMinute(kind) : 0;
            return how + "\n" + (perMinute > 0 ? $"+{perMinute} a minute" : "Nothing coming in right now.");
        }

        static string Workers()
        {
            int working = 0, asleep = 0, waiting = 0;
            foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
            {
                if (w.IsAsleep) asleep++;
                else if (w.IsAssigned) working++;
                else waiting++;
            }
            if (working + asleep + waiting == 0) return "None yet. Welcome one in at a Capy House.";
            var text = $"{working} at work";
            if (asleep > 0) text += "\n" + $"{asleep} asleep";
            if (waiting > 0) text += "\n" + $"{waiting} waiting for a job";
            return text;
        }

        static void Build(RectTransform parent)
        {
            if (_card != null) return;
            var card = UiKit.Card(parent, "CounterInfo", UiKit.Surface, 30, shadow: false, raycast: false);
            _card = card.rectTransform;
            _card.anchorMin = _card.anchorMax = new Vector2(0.5f, 0.5f);
            _card.pivot = new Vector2(0.5f, 1f);
            _card.sizeDelta = new Vector2(520f, 0f);
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(28, 28, 20, 22);
            v.spacing = 6f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _title = UiKit.Text(_card, "Title", "", 38f, UiKit.PrimaryDark, true);
            _title.overflowMode = TextOverflowModes.Overflow;
            _body = UiKit.Text(_card, "Body", "", 30f, UiKit.Ink);
            _body.overflowMode = TextOverflowModes.Overflow;
            foreach (var t in new[] { _title, _body })
            {
                t.raycastTarget = false;
                t.GetComponent<LocText>().NoRead = true;
            }
            _card.gameObject.SetActive(false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { _card = null; _owner = null; }
    }
}
