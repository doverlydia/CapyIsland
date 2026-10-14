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
    public class QuestGiver : MonoBehaviour, ITappable, IMapMarker
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

        // Miso and the Captain stroll about their spot rather than standing like statues.
        Vector2 _home;
        bool _hasHome;
        Vector2 _strollTo;
        bool _strolling;
        float _nextStroll, _hop;
        Vector3 _bodyRest;
        int _dressedUp;   // 0 as she is; 1 in the dress, 2 with the pearls too, 3 and the tiara

        public Kind GiverKind => kind;
        public int GhostId => ghostId;

        public void Configure(Kind k) => kind = k;

        public void AssignGhost(int id)
        {
            kind = Kind.Ghost;
            ghostId = id;
        }

        public string Speaker => kind switch
        {
            Kind.Coriander => StoryText.Sweetheart,
            Kind.Peach => StoryText.Captain,
            _ => StoryText.Spirit,
        };

        void Awake()
        {
            _body = transform.Find("Sprite");
            _bodyRenderer = _body != null ? _body.GetComponent<SpriteRenderer>() : null;
            _placeable = GetComponent<Placeable>();
            if (_body != null) _bodyRest = _body.localPosition;

            var markerGo = new GameObject("QuestMarker");
            markerGo.transform.SetParent(transform, false);
            _marker = markerGo.AddComponent<TextMeshPro>();
            _marker.text = "!";
            if (UI.UiKit.HeadingFont != null) _marker.font = UI.UiKit.HeadingFont;
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
            MapMarkers.Register(this);
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
            MapMarkers.Unregister(this);
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

        /// <summary>A new quest to hear, or a finished one to be thanked for.</summary>
        public bool HasNews
        {
            get
            {
                var q = Progress;
                return InWorld && q != null && q.Stage != QuestSystem.Stage.GiftReceived
                       && (q.Stage == QuestSystem.Stage.NotStarted || q.Complete);
            }
        }

        /// <summary>The finished quest is waiting to be handed in.</summary>
        public bool RewardWaiting
        {
            get
            {
                var q = Progress;
                return InWorld && q != null && q.Complete && q.Stage != QuestSystem.Stage.GiftReceived;
            }
        }

        public bool IsInWorld => InWorld;
        public QuestSystem.Progress CurrentQuest => Progress;
        public Sprite Portrait => _bodyRenderer != null ? _bodyRenderer.sprite : null;

        public MarkerKind MarkerKind => MarkerKind.Quest;
        public bool MarkerActive => HasNews;
        public Vector2 MarkerPosition => transform.position;
        public Sprite MarkerIcon => Portrait;
        public string MarkerLabel => Speaker;

        void Update()
        {
            bool news = HasNews;
            if (_marker != null)
            {
                _marker.enabled = news;
                float top = _bodyRenderer != null ? _bodyRenderer.bounds.max.y - transform.position.y : 1.4f;
                _marker.transform.localPosition = new Vector3(0f, (top + 0.45f + Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.2f) / Mathf.Max(0.01f, transform.lossyScale.y), 0f);
            }

            if (kind == Kind.Ghost && _body != null && !_departing)
                _body.localPosition = new Vector3(0f, 0.25f + Mathf.Sin(Time.time * 1.6f) * 0.12f, 0f);
            else Stroll();

            if (_hintTimer > 0f && InWorld)
            {
                _hintTimer -= Time.deltaTime;
                if (_hintTimer <= 0f && !QuestSystem.AnyoneTalkedTo) Hints.Show(NudgeLine, () => !QuestSystem.AnyoneTalkedTo);
            }
        }

        /// <summary>
        /// A gentle wander around where they live: a few steps, a pause, a few more. They stop to
        /// talk (and while any conversation is on screen), and never leave dry land.
        /// </summary>
        void Stroll()
        {
            if (_body == null || !InWorld || _departing) return;
            if (!_hasHome) { _home = transform.position; _hasHome = true; _nextStroll = Time.time + Random.Range(2f, 5f); }
            if (UI.SpeechDialog.IsOpen || Time.timeScale <= 0f) { _strolling = false; Rest(); return; }

            if (!_strolling)
            {
                Rest();
                if (Time.time < _nextStroll) return;
                for (int i = 0; i < 8; i++)
                {
                    var t = _home + Random.insideUnitCircle * 2.4f;
                    if (!BunniSpace.PointOnLand(t)) continue;
                    _strollTo = t;
                    _strolling = true;
                    break;
                }
                _nextStroll = Time.time + Random.Range(4f, 9f);
                return;
            }

            Vector2 here = transform.position;
            var next = Vector2.MoveTowards(here, _strollTo, 0.75f * Time.deltaTime);
            if (!BunniSpace.PointOnLand(next)) { _strolling = false; return; }
            transform.position = new Vector3(next.x, next.y, transform.position.z);
            // Pictures face left: turn to face the way they walk.
            float dx = _strollTo.x - here.x;
            if (Mathf.Abs(dx) > 0.01f)
            {
                var sc = _body.localScale;
                sc.x = Mathf.Abs(sc.x) * (dx > 0f ? -1f : 1f);
                _body.localScale = sc;
            }
            _hop += Time.deltaTime * 9f;
            _body.localPosition = _bodyRest + new Vector3(0f, Mathf.Abs(Mathf.Sin(_hop)) * 0.06f, 0f);
            if ((next - _strollTo).sqrMagnitude < 0.01f) _strolling = false;
        }

        void Rest()
        {
            if (_body != null) _body.localPosition = Vector3.Lerp(_body.localPosition, _bodyRest, Time.deltaTime * 10f);
        }

        string NudgeLine => StoryText.NudgeFor(kind);
        bool _introduced;

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
            if (DishOrders.TryServe(transform)) return;   // the dish they wanted, if it's in your bag, comes first
            var qs = QuestSystem.Instance;
            if (qs == null) return;
            var q = Progress;
            var pages = qs.PagesFor(q, kind);
            var face = _bodyRenderer != null ? _bodyRenderer.sprite : null;

            // Meeting Miso for the first time before Old Yuzu got to say who she is: Yuzu says it
            // first, so it is never missed, and then Miso speaks.
            if (kind == Kind.Coriander && !QuestSystem.AnyoneTalkedTo && !_introduced
                && (UI.GhostDialogue.Instance == null || !UI.GhostDialogue.Instance.HasSaid(NudgeLine)))
            {
                _introduced = true;
                var guide = UI.GhostDialogue.Instance != null ? UI.GhostDialogue.Instance.Face : null;
                UI.SpeechDialog.Show(guide, StoryText.Guide, new[] { StoryText.MisoIntro }, Tap);
                return;
            }
            QuestSystem.AnyoneTalkedTo = true;

            // Peach's last wish: the dance, for 500 gems.
            if (kind == Kind.Peach && q != null && q.Def.Id == 4 && !q.Complete)
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


        void Dance()
        {
            var store = ResourceStore.Instance;
            if (store == null || !store.TrySpend(0, 0, 500)) return;
            // Paid on her very first ask: that's the task given and done at once (its heart waits for the task).
            var q = QuestSystem.Instance != null ? QuestSystem.Instance.Current(Kind.Peach) : null;
            if (q != null && q.Stage == QuestSystem.Stage.NotStarted) q.Stage = QuestSystem.Stage.IntroRead;
            // The little movie (the original played one), then a happy jig back on the island.
            UI.DanceCutscene.Show(() =>
            {
                Motes.Trigger("peach_dance");
                if (_body != null)
                {
                    _body.DOKill();
                    var seq = DOTween.Sequence();
                    for (int i = 0; i < 4; i++)
                    {
                        seq.Append(_body.DOLocalJump(Vector3.zero, 0.5f, 1, 0.35f));
                        seq.Join(_body.DOLocalRotate(new Vector3(0f, 0f, i % 2 == 0 ? 12f : -12f), 0.35f));
                    }
                    seq.Append(_body.DOLocalRotate(Vector3.zero, 0.2f));
                }
                var fx = HarvestEffects.Instance;
                if (fx != null) fx.Twinkle((Vector2)transform.position + Vector2.up, 30);
            });
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
            if (Wear(gift) && _body != null) _body.DOPunchScale(Vector3.one * 0.15f, 0.5f, 6, 0.6f);
        }

        /// <summary>After a load, she is still wearing everything you have given her.</summary>
        System.Collections.IEnumerator Start()
        {
            if (kind != Kind.Coriander) yield break;
            yield return null;
            if (Motes.Has("The Girl's New Dress")) Wear("Wedding Dress");
            if (Motes.Has("The Girl's New Necklace")) Wear("String of Pearls");
            if (Motes.Has("The Girl's Tiara")) Wear("Tiara");
        }

        /// <summary>
        /// Each gift is a whole new picture of her wearing it: pieces pinned on top never looked
        /// worn. They always come in this order (the dress opens the shop with the pearls, the
        /// pearls the one with the tiara), so three pictures cover every stage.
        /// </summary>
        bool Wear(string gift)
        {
            if (_bodyRenderer == null) return false;
            int stage = gift switch { "Wedding Dress" => 1, "String of Pearls" => 2, "Tiara" => 3, _ => 0 };
            if (stage <= _dressedUp) return false;
            _dressedUp = stage;
            // The newest picture there is, in case one is missing.
            string[] art = { "Miso_Dress", "Miso_DressPearls", "Miso_DressTiara" };
            for (int i = stage - 1; i >= 0; i--)
            {
                var sprite = HobbyData.Art(art[i]);
                if (sprite == null) continue;
                _bodyRenderer.sprite = sprite;
                return true;
            }
            return false;
        }
    }
}
