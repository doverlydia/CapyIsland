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

    public class Priest : MonoBehaviour, ITappable
    {
        public const int WeddingWood = 50, WeddingGold = 50;

        SpriteRenderer _body;

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

        void OnEnable() => TapTargets.Register(this);
        void OnDisable() => TapTargets.Unregister(this);

        /// <summary>Only once she is back: until then there is nobody here to say the words.</summary>
        void Update()
        {
            bool here = YuzuHeart.Alive;
            if (_body != null && _body.enabled != here) _body.enabled = here;
            var shadow = transform.Find("Shadow");
            if (shadow != null && shadow.gameObject.activeSelf != here) shadow.gameObject.SetActive(here);
        }

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
                Motes.Trigger("wedding_mote");
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
