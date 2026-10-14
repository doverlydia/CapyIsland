using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The Blossom Chapel (Building/CastleItem) and the spot beside it where the words are said
    /// (MapItems/Priest), 50 east and 100 south of it. Nobody stands there until Old Yuzu is
    /// alive again (YuzuHeart); then she does, and once Miso has said yes she marries you two
    /// for 50 wood and 50 gold, and renews your vows, for the same, as often as you like.
    /// </summary>
    public class Church : MonoBehaviour
    {
        Priest _priest;

        void OnPlacedInWorld()
        {
            if (_priest != null) return;
            var at = (Vector2)transform.position + BunniSpace.ToUnity(new Vector2(50f, -100f));
            _priest = Priest.Spawn(at, transform.parent);
        }
    }

    public class Priest : MonoBehaviour, ITappable, IMapMarker
    {
        public const int WeddingWood = 50, WeddingGold = 50;

        /// <summary>Close enough to the chapel that the task turns from "go there" to "pay".</summary>
        public const float NearDistance = 4.5f;

        public static Priest Instance { get; private set; }

        SpriteRenderer _body;
        bool _pointed;   // the hand has shown you her, this visit
        float _nextLook;

        public static Priest Spawn(Vector2 at, Transform parent)
        {
            var go = new GameObject("Priest");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(at.x, at.y, 0f);

            var art = GameArt.Instance;
            if (art != null && art.shadowTemplate != null)
            {
                var shadow = Instantiate(art.shadowTemplate, go.transform);
                shadow.name = "Shadow";
                shadow.transform.localPosition = Vector3.zero;
                shadow.transform.localScale = art.shadowTemplate.transform.localScale * 0.55f;
                shadow.SetActive(true);
            }

            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Cutscenes/Cutscene_IntroYuzu");   // Old Yuzu, alive
            if (sr.sprite != null) body.transform.localScale = Vector3.one * (1.55f / Mathf.Max(0.01f, sr.sprite.bounds.size.y));

            go.AddComponent<YSorter>();
            var priest = go.AddComponent<Priest>();
            priest._body = sr;
            return priest;
        }

        void OnEnable()
        {
            Instance = this;
            TapTargets.Register(this);
            MapMarkers.Register(this);
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            TapTargets.Unregister(this);
            MapMarkers.Unregister(this);
        }

        /// <summary>Where she stands, for the arrows.</summary>
        public Vector2 Spot => _body != null ? (Vector2)_body.bounds.center : (Vector2)transform.position;

        /// <summary>Only once she is back: until then there is nobody here to say the words.</summary>
        void Update()
        {
            bool here = YuzuHeart.Alive;
            if (_body != null && _body.enabled != here) _body.enabled = here;
            var shadow = transform.Find("Shadow");
            if (shadow != null && shadow.gameObject.activeSelf != here) shadow.gameObject.SetActive(here);
            PointWhenNear();
        }

        /// <summary>
        /// The wedding waiting: when you walk up to the chapel, the hand shows you who to tap.
        /// Once per visit, so it never nags while you stand about.
        /// </summary>
        void PointWhenNear()
        {
            if (Time.time < _nextLook) return;
            _nextLook = Time.time + 0.5f;
            // Shown even with the tutorial switched off: this is the story's last step, not a lesson.
            if (!YuzuHeart.WeddingDue || _body == null || !_body.enabled) return;
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king == null) return;
            bool near = Vector2.Distance(king.transform.position, transform.position) < NearDistance;
            if (!near) { _pointed = false; return; }
            if (_pointed) return;
            var tutorial = TutorialDirector.Instance;
            if (tutorial != null && !tutorial.Finished) return;   // the hand is busy teaching
            var pointer = FindFirstObjectByType<TutorialPointer>();
            if (pointer == null) return;
            _pointed = true;
            pointer.PointAt(_body.transform, 6f);
        }

        // ---------------------------------------------------------------- the arrows

        public MarkerKind MarkerKind => MarkerKind.Quest;
        public bool MarkerActive => YuzuHeart.WeddingDue && _body != null && _body.enabled;
        public Vector2 MarkerPosition => Spot;
        public Sprite MarkerIcon => _body != null ? _body.sprite : null;
        public string MarkerLabel => StoryText.Name("Church");

        public float TapDistance(Vector2 worldPoint)
        {
            if (_body == null || !_body.enabled) return -1f;
            float d = Vector2.Distance(_body.bounds.center, worldPoint);
            return d < 1f ? d : -1f;
        }

        public void Tap()
        {
            var face = _body != null ? _body.sprite : null;
            if (!Motes.Has("Madly in Love"))
            {
                UI.SpeechDialog.Show(face, StoryText.Guide, new[]
                {
                    "The Blossom Chapel! I said my own vows under this very blossom. If two hearts ever find each other here, I'd be honoured to say the words.",
                });
                return;
            }

            bool married = Motes.Has("The wedding bells toll for thee");
            var store = ResourceStore.Instance;
            bool afford = store != null && store.CanAfford(WeddingWood, WeddingGold, 0);
            string line = married
                ? "Back again, you two? Shall we renew your vows under the blossom?"
                : "My granddaughter, and the little one who woke me... Shall we hold the wedding? The ceremony costs 50 wood and 50 gold, for the lanterns and the cake.";
            var choices = new System.Collections.Generic.List<UI.SpeechDialog.Choice>
            {
                new(married ? "Renew Vows" : "Wedding Ceremony", Wed, afford),
                new("Cancel", null),
            };
            UI.SpeechDialog.ShowChoices(face, StoryText.Guide, new[] { line }, null, choices);
        }

        void Wed()
        {
            var store = ResourceStore.Instance;
            if (store == null || !store.TrySpend(WeddingWood, WeddingGold, 0)) return;
            bool renewal = Motes.Has("The wedding bells toll for thee");
            // The little movie first (as the original did), then the party carries on outside.
            UI.WeddingCutscene.Show(renewal, () =>
            {
                // Not a heart: the hearts are what woke her. Remembered, and the bow tie is yours.
                if (!renewal)
                {
                    Motes.Record(Motes.Wedding);
                    Wardrobe.Give(Wardrobe.RewardFor(Motes.Wedding));
                }
                Celebrate();
            });
        }

        /// <summary>The wedding: the camera comes over, hearts and sparkles everywhere.</summary>
        void Celebrate()
        {
            CameraRig.CameraFocus.Show(transform.position, 5f);
            var fx = HarvestEffects.Instance;
            if (fx == null) return;
            var seq = DOTween.Sequence();
            for (int i = 0; i < 8; i++)
            {
                seq.AppendCallback(() =>
                {
                    var p = (Vector2)transform.position + new Vector2(Random.Range(-3f, 3f), Random.Range(0.5f, 3f));
                    fx.Twinkle(p, 14);
                    fx.FruitPop(p, new Color(1f, 0.55f, 0.7f));
                });
                seq.AppendInterval(0.35f);
            }
        }
    }
}
