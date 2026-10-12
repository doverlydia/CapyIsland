using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Birdwatching. Every so often a bird lands near you — in a tree, or for the wading birds on
    /// the shore. Tap it from a little way off to spot it for your journal. Walk right up to it and
    /// it takes fright; leave it too long and it flies on by itself.
    /// </summary>
    public class Birding : MonoBehaviour
    {
        const float SpotRange = 7f, ScareRange = 1.4f;

        float _nextBird;
        PlayerAvatar _king;

        void Start() => _nextBird = Time.time + Random.Range(12f, 25f);

        void Update()
        {
            if (_king == null) _king = FindFirstObjectByType<PlayerAvatar>();
            if (_king == null || Time.time < _nextBird) return;
            if (!HobbyLog.IsUnlocked(Hobby.Birding)) return;   // birds come once you have binoculars
            _nextBird = Time.time + Random.Range(25f, 50f);
            if (FindObjectsByType<Bird>(FindObjectsSortMode.None).Length >= 2) return;
            TryLand(_king.transform.position);
        }

        void TryLand(Vector2 around)
        {
            int level = HobbyLog.Level(Hobby.Birding);
            bool wader = Random.value < 0.25f;
            var kind = HobbyData.RollBird(level, wader);
            if (kind == null) return;   // nobody about at this hour

            Vector2? perch = null;
            int order = 0;
            if (!kind.Wader)
            {
                // A tree on screen, not too close.
                Harvestable best = null;
                float bestScore = float.MaxValue;
                foreach (var node in FindObjectsByType<Harvestable>(FindObjectsSortMode.None))
                {
                    if (node.Family != NodeFamily.Tree || !node.InWorld) continue;
                    float d = Vector2.Distance(node.transform.position, around);
                    if (d < 2.5f || d > 9f) continue;
                    float score = Mathf.Abs(d - 5f) + Random.value * 2f;
                    if (score < bestScore) { bestScore = score; best = node; }
                }
                if (best != null)
                {
                    var sr = best.GetComponentInChildren<SpriteRenderer>();
                    var b = sr != null ? sr.bounds : new Bounds(best.transform.position, Vector3.one);
                    perch = new Vector2(b.center.x + Random.Range(-0.3f, 0.3f), b.max.y - b.size.y * 0.3f);
                    order = (sr != null ? sr.sortingOrder : 3000) + 5;
                }
            }
            if (perch == null)
            {
                // On open ground (or the shore for waders), a few steps away.
                for (int i = 0; i < 20; i++)
                {
                    var p = around + Random.insideUnitCircle.normalized * Random.Range(3.5f, 7f);
                    if (!BunniSpace.PointOnLand(p)) continue;
                    perch = p;
                    order = YSorter.PropSortBase + Mathf.RoundToInt(-p.y * 100f) + 5;
                    break;
                }
            }
            if (perch == null) return;
            Bird.Land(kind, perch.Value, order);
        }

        /// <summary>One bird, perched. Tap to spot it.</summary>
        public class Bird : MonoBehaviour, ITappable
        {
            BirdKind _kind;
            SpriteRenderer _sr;
            float _leaveAt, _nextHop;
            bool _gone;

            public static void Land(BirdKind kind, Vector2 at, int order)
            {
                var go = new GameObject("Bird_" + kind.Id);
                var bird = go.AddComponent<Bird>();
                bird._kind = kind;
                bird._sr = go.AddComponent<SpriteRenderer>();
                bird._sr.sprite = HobbyData.BirdPicture(kind);
                bird._sr.sortingOrder = order;
                bird._sr.flipX = Random.value < 0.5f;
                float scale = kind.Wader ? 0.75f : 0.5f;
                bird._leaveAt = Time.time + Random.Range(25f, 45f);
                bird._nextHop = Time.time + Random.Range(1.5f, 4f);

                // Flutter in from above.
                go.transform.position = at + new Vector2(Random.Range(-3f, 3f), 5f);
                go.transform.localScale = Vector3.one * scale;
                go.transform.DOMove(at, 1.1f).SetEase(Ease.OutQuad);
            }

            void OnEnable() => TapTargets.Register(this);
            void OnDisable() => TapTargets.Unregister(this);

            public float TapDistance(Vector2 worldPoint)
            {
                if (_gone || _sr == null) return -1f;
                float d = Vector2.Distance(_sr.bounds.center, worldPoint);
                return d < 0.9f ? d : -1f;
            }

            public void Tap()
            {
                if (_gone) return;
                var king = FindFirstObjectByType<PlayerAvatar>();
                float d = king != null ? Vector2.Distance(king.transform.position, transform.position) : 0f;
                if (d > SpotRange)
                {
                    Hints.Show("That bird is too far away to see properly. Creep a little closer.");
                    return;
                }
                bool first = HobbyLog.LogBird(_kind.Id);
                int xp = 5 * _kind.Stars + (first ? 12 : 0);
                HobbyLog.AddXp(Hobby.Birding, xp);
                Motes.Trigger("hobby_bird");
                Sfx.Play(SfxKind.Heart, 0.6f);
                UI.CardPopup.Show("You spotted " + HobbyData.WithArticle(_kind.Name) + "!",
                    HobbyData.Rarity(_kind.Stars) + (first ? "\n<color=#2DB3A6><b>New to your journal!</b></color>" : "") + $"\n+{xp} birding XP",
                    HobbyData.BirdPicture(_kind));
                FlyAway();
            }

            void Update()
            {
                if (_gone) return;
                var king = FindFirstObjectByType<PlayerAvatar>();
                if (Time.time > _leaveAt || (king != null && Vector2.Distance(king.transform.position, transform.position) < ScareRange))
                {
                    FlyAway();
                    return;
                }
                if (Time.time >= _nextHop)
                {
                    // Little hops and turns: a bird never sits perfectly still.
                    _nextHop = Time.time + Random.Range(1.5f, 4f);
                    if (Random.value < 0.4f) _sr.flipX = !_sr.flipX;
                    transform.DOKill();
                    transform.DOJump(transform.position, 0.12f, 1, 0.25f);
                }
            }

            void FlyAway()
            {
                if (_gone) return;
                _gone = true;
                transform.DOKill();
                var to = (Vector2)transform.position + new Vector2(_sr.flipX ? 6f : -6f, 7f);
                _sr.flipX = to.x > transform.position.x;
                transform.DOMove(to, 1.4f).SetEase(Ease.InQuad);
                _sr.DOFade(0f, 1.4f).OnComplete(() => Destroy(gameObject));
            }
        }
    }
}
