using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Coriander, Pirate Peach or a quest ghost (MapItems/Ghosts/QuestItem). Tap to talk: they say
    /// whatever fits where their quest stands — the ask, a reminder, or thanks and a reward. A "!"
    /// bobs over their head while they have something new to say.
    ///
    /// Coriander and Peach are placed like any item; a ghost simply appears, and floats away once
    /// its quest is done.
    /// </summary>
    public class QuestGiver : MonoBehaviour, ITappable
    {
        public enum Kind { Coriander, Peach, Ghost }

        [SerializeField] Kind kind;
        [SerializeField] int ghostId;

        TextMeshPro _marker;
        Transform _body;
        SpriteRenderer _bodyRenderer;
        Placeable _placeable;
        float _hintTimer = -1f;
        bool _departing;
        SpriteRenderer _accessory;

        public Kind GiverKind => kind;

        public void Configure(Kind k) => kind = k;

        public void AssignGhost(int id)
        {
            kind = Kind.Ghost;
            ghostId = id;
        }

        string Speaker => kind switch
        {
            Kind.Coriander => "Coriander",
            Kind.Peach => "Pirate Peach",
            _ => "Ghost",
        };

        void Awake()
        {
            _body = transform.Find("Sprite");
            _bodyRenderer = _body != null ? _body.GetComponent<SpriteRenderer>() : null;
            _placeable = GetComponent<Placeable>();

            var markerGo = new GameObject("QuestMarker");
            markerGo.transform.SetParent(transform, false);
            _marker = markerGo.AddComponent<TextMeshPro>();
            _marker.text = "!";
            _marker.fontSize = 9f;
            _marker.fontStyle = FontStyles.Bold;
            _marker.alignment = TextAlignmentOptions.Center;
            _marker.color = new Color(1f, 0.86f, 0.3f);
            _marker.outlineWidth = 0.3f;
            _marker.outlineColor = new Color32(90, 50, 20, 255);
            _marker.rectTransform.sizeDelta = new Vector2(2f, 2f);
            _marker.sortingOrder = 31500;
        }

        void OnEnable()
        {
            TapTargets.Register(this);
            if (kind == Kind.Coriander) Purchasing.GiftGiven += OnGift;

            // The original's nudges: Coriander at once, Peach and ghosts after a minute — only
            // until you have talked to any of them.
            _hintTimer = kind == Kind.Coriander ? 0.5f : 60f;

            if (kind == Kind.Ghost && _bodyRenderer != null)
            {
                var c = _bodyRenderer.color; c.a = 0f; _bodyRenderer.color = c;
                _bodyRenderer.DOFade(0.92f, 1.2f);
            }
        }

        void OnDisable()
        {
            TapTargets.Unregister(this);
            if (kind == Kind.Coriander) Purchasing.GiftGiven -= OnGift;
        }

        bool InWorld => kind == Kind.Ghost ? !_departing : (_placeable == null || _placeable.Placed);

        QuestSystem.Progress Progress
        {
            get
            {
                var qs = QuestSystem.Instance;
                if (qs == null) return null;
                return kind == Kind.Ghost ? qs.Ghost(ghostId) : qs.Current(kind);
            }
        }

        void Update()
        {
            var q = Progress;
            bool news = InWorld && q != null && q.Stage != QuestSystem.Stage.GiftReceived
                        && (q.Stage == QuestSystem.Stage.NotStarted || q.Complete);
            if (_marker != null)
            {
                _marker.enabled = news;
                float top = _bodyRenderer != null ? _bodyRenderer.bounds.max.y - transform.position.y : 1.4f;
                _marker.transform.localPosition = new Vector3(0f, (top + 0.45f + Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.2f) / Mathf.Max(0.01f, transform.lossyScale.y), 0f);
            }

            if (kind == Kind.Ghost && _body != null && !_departing)
                _body.localPosition = new Vector3(0f, 0.25f + Mathf.Sin(Time.time * 1.6f) * 0.12f, 0f);

            if (_hintTimer > 0f && InWorld)
            {
                _hintTimer -= Time.deltaTime;
                if (_hintTimer <= 0f && !QuestSystem.AnyoneTalkedTo) Hints.Show(NudgeLine);
            }
        }

        string NudgeLine => kind switch
        {
            Kind.Coriander => "Goodness, my dear daughter is still alive! Tap on her to see if she needs any help.",
            Kind.Peach => "Have you chatted with Pirate Peach? She's a saucy lass who knows the secret of how to unlock Pirate Bay.",
            _ => "A new ghost has appeared and has a quest for you... tap him to find out more.",
        };

        // ---------------------------------------------------------------- talking

        public float TapDistance(Vector2 worldPoint)
        {
            if (!InWorld) return -1f;
            Vector2 at = _bodyRenderer != null ? (Vector2)_bodyRenderer.bounds.center : (Vector2)transform.position;
            float d = Vector2.Distance(at, worldPoint);
            return d < 1.1f ? d : -1f;
        }

        public void Tap()
        {
            var qs = QuestSystem.Instance;
            if (qs == null) return;
            var q = Progress;
            var pages = qs.PagesFor(q, kind);
            var face = _bodyRenderer != null ? _bodyRenderer.sprite : null;
            QuestSystem.AnyoneTalkedTo = true;

            // Peach's last wish: the dance, for 500 gems.
            if (kind == Kind.Peach && q != null && q.Def.Id == 4 && !q.Complete && WantsToDance(q, pages))
            {
                var store = ResourceStore.Instance;
                bool rich = store != null && store.Get(ResourceKind.Gem) >= 500;
                if (rich)
                {
                    UI.SpeechDialog.Show(face, Speaker, pages, () => qs.Advance(q, this),
                        ("Give 500 gems", Dance), ("Cancel", null));
                    return;
                }
            }

            UI.SpeechDialog.Show(face, Speaker, pages, () => qs.Advance(q, this));
        }

        static bool WantsToDance(QuestSystem.Progress q, string[] pages)
        {
            string text = string.Join(" ", pages);
            return text.Contains("little show for you") || text.Contains("The offer still stands");
        }

        void Dance()
        {
            var store = ResourceStore.Instance;
            if (store == null || !store.TrySpend(0, 0, 500)) return;
            Motes.Trigger("peach_dance");
            if (_body != null)
            {
                _body.DOKill();
                var seq = DOTween.Sequence();
                for (int i = 0; i < 6; i++)
                {
                    seq.Append(_body.DOLocalJump(Vector3.zero, 0.5f, 1, 0.35f));
                    seq.Join(_body.DOLocalRotate(new Vector3(0f, 0f, i % 2 == 0 ? 12f : -12f), 0.35f));
                }
                seq.Append(_body.DOLocalRotate(Vector3.zero, 0.2f));
            }
            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Twinkle((Vector2)transform.position + Vector2.up, 30);
            CameraRig.CameraFocus.Show(transform.position, 3f);
        }

        /// <summary>A ghost whose quest is done drifts up and fades away.</summary>
        public void Depart()
        {
            _departing = true;
            if (_marker != null) _marker.enabled = false;
            if (_body == null) { Destroy(gameObject); return; }
            _body.DOLocalMoveY(_body.localPosition.y + 2.5f, 2f).SetEase(Ease.InSine);
            if (_bodyRenderer != null) _bodyRenderer.DOFade(0f, 2f).OnComplete(() => Destroy(gameObject));
        }

        // ---------------------------------------------------------------- Coriander's gifts

        /// <summary>A dress, pearls or tiara: she lights up and wears it (CorianderItem.Decorate).</summary>
        void OnGift(string gift)
        {
            CameraRig.CameraFocus.Show(transform.position, 3.5f);
            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Twinkle((Vector2)transform.position + Vector2.up * 0.8f, 24);

            var catalog = ItemCatalog.Instance;
            var sprite = catalog != null ? catalog.Icon(gift) : null;
            if (sprite == null || _body == null) return;
            if (_accessory == null)
            {
                var go = new GameObject("Accessory");
                go.transform.SetParent(_body, false);
                _accessory = go.AddComponent<SpriteRenderer>();
            }
            _accessory.sprite = sprite;
            float size = gift == "Tiara" ? 0.45f : gift == "String of Pearls" ? 0.4f : 0.6f;
            _accessory.transform.localScale = Vector3.one * size / Mathf.Max(0.01f, sprite.bounds.size.x);
            _accessory.transform.localPosition = gift == "Tiara" ? new Vector3(0.05f, 1.05f, 0f)
                                              : gift == "String of Pearls" ? new Vector3(0.05f, 0.45f, 0f)
                                              : new Vector3(0.4f, 0.1f, 0f);
            if (_bodyRenderer != null) _accessory.sortingOrder = _bodyRenderer.sortingOrder + 1;
            _body.DOPunchScale(Vector3.one * 0.15f, 0.5f, 6, 0.6f);
        }

        void LateUpdate()
        {
            if (_accessory != null && _bodyRenderer != null) _accessory.sortingOrder = _bodyRenderer.sortingOrder + 1;
        }
    }
}
