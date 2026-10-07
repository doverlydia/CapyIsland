using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Bug catching. Bugs turn up around you — butterflies and bees on flowers, beetles and
    /// fireflies on trees, snails and dragonflies on the ground. Tap one from close by to net it for
    /// your journal. Rush right up to it and it escapes; rarer bugs appear as your level grows.
    /// </summary>
    public class BugCatching : MonoBehaviour
    {
        const float NetRange = 4.5f, ScareRange = 0.9f;
        float _next;
        PlayerAvatar _king;

        void Start() => _next = Time.time + Random.Range(8f, 16f);

        void Update()
        {
            if (_king == null) _king = FindFirstObjectByType<PlayerAvatar>();
            if (_king == null || Time.time < _next) return;
            _next = Time.time + Random.Range(18f, 35f);
            if (FindObjectsByType<Bug>(FindObjectsSortMode.None).Length >= 3) return;
            Spawn(_king.transform.position);
        }

        void Spawn(Vector2 around)
        {
            int level = HobbyLog.Level(Hobby.Bugs);
            var homes = new[] { BugHome.Flower, BugHome.Tree, BugHome.Ground };
            var home = homes[Random.Range(0, homes.Length)];

            Vector2? at = null;
            int order = 0;
            if (home != BugHome.Ground)
            {
                var family = home == BugHome.Flower ? NodeFamily.Flower : NodeFamily.Tree;
                Harvestable best = null;
                float bestScore = float.MaxValue;
                foreach (var node in FindObjectsByType<Harvestable>(FindObjectsSortMode.None))
                {
                    if (node.Family != family || !node.InWorld) continue;
                    float d = Vector2.Distance(node.transform.position, around);
                    if (d < 2f || d > 8f) continue;
                    float score = Mathf.Abs(d - 4f) + Random.value * 2f;
                    if (score < bestScore) { bestScore = score; best = node; }
                }
                if (best != null)
                {
                    var sr = best.GetComponentInChildren<SpriteRenderer>();
                    var b = sr != null ? sr.bounds : new Bounds(best.transform.position, Vector3.one);
                    at = home == BugHome.Tree ? new Vector2(b.center.x + Random.Range(-0.2f, 0.2f), b.center.y) : (Vector2)b.center + Vector2.up * 0.2f;
                    order = (sr != null ? sr.sortingOrder : 3000) + 5;
                }
                else home = BugHome.Ground;   // no flowers or trees nearby: something on the grass instead
            }
            if (at == null)
            {
                for (int i = 0; i < 20; i++)
                {
                    var p = around + Random.insideUnitCircle.normalized * Random.Range(2.5f, 6f);
                    if (!BunniSpace.PointOnLand(p)) continue;
                    at = p;
                    order = YSorter.PropSortBase + Mathf.RoundToInt(-p.y * 100f) + 5;
                    break;
                }
            }
            if (at == null) return;
            var kind = HobbyData.RollBug(level, home);
            if (kind == null) return;
            Bug.Make(kind, at.Value, order);
        }

        /// <summary>One bug. Tap to catch it.</summary>
        public class Bug : MonoBehaviour, ITappable
        {
            BugKind _kind;
            SpriteRenderer _sr;
            Vector2 _home;
            float _leaveAt, _phase;
            bool _gone;

            public static void Make(BugKind kind, Vector2 at, int order)
            {
                var art = HobbyData.Art("Bug_" + kind.Id);
                if (art == null) return;   // not drawn yet
                var go = new GameObject("Bug_" + kind.Id);
                go.transform.position = at;
                var bug = go.AddComponent<Bug>();
                bug._kind = kind;
                bug._home = at;
                bug._phase = Random.value * 10f;
                bug._sr = go.AddComponent<SpriteRenderer>();
                bug._sr.sprite = art;
                bug._sr.sortingOrder = order;
                float size = kind.Id == "Snail" || kind.Id == "Ladybug" ? 0.32f : 0.45f;
                go.transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, art.bounds.size.x));
                bug._leaveAt = Time.time + Random.Range(30f, 50f);
                bug._sr.color = new Color(1f, 1f, 1f, 0f);
                bug._sr.DOFade(1f, 0.6f);
            }

            void OnEnable() => TapTargets.Register(this);
            void OnDisable() => TapTargets.Unregister(this);

            public float TapDistance(Vector2 worldPoint)
            {
                if (_gone || _sr == null) return -1f;
                float d = Vector2.Distance(_sr.bounds.center, worldPoint);
                return d < 0.7f ? d : -1f;
            }

            public void Tap()
            {
                if (_gone) return;
                var king = FindFirstObjectByType<PlayerAvatar>();
                if (king != null && Vector2.Distance(king.transform.position, transform.position) > NetRange)
                {
                    Hints.Show("Too far to reach with your net. Tiptoe a little closer.");
                    return;
                }
                bool first = HobbyLog.LogBug(_kind.Id);
                int xp = 5 * _kind.Stars + (first ? 12 : 0);
                HobbyLog.AddXp(Hobby.Bugs, xp);
                Motes.Trigger("hobby_bug");
                Sfx.Play(SfxKind.Pop, 0.6f);
                UI.CardPopup.Show("You caught " + HobbyData.WithArticle(_kind.Name) + "!",
                    HobbyData.Rarity(_kind.Stars) + (first ? "\n<color=#2DB3A6><b>New to your journal!</b></color>" : "") + $"\n+{xp} bug catching XP",
                    _sr.sprite);
                _gone = true;
                transform.DOKill();
                transform.DOScale(0f, 0.25f).SetEase(Ease.InBack).OnComplete(() => Destroy(gameObject));
            }

            void Update()
            {
                if (_gone) return;
                float t = Time.time + _phase;
                if (_kind.Flies)
                {
                    // Flit about its spot in lazy loops.
                    var p = _home + new Vector2(Mathf.Sin(t * 0.9f) * 0.6f, Mathf.Sin(t * 1.7f) * 0.3f + 0.25f);
                    transform.position = p;
                    _sr.flipX = Mathf.Cos(t * 0.9f) < 0f;
                }
                else if (_kind.Home == BugHome.Ground)
                {
                    // A slow crawl back and forth.
                    transform.position = _home + new Vector2(Mathf.Sin(t * 0.25f) * 0.4f, 0f);
                    _sr.flipX = Mathf.Cos(t * 0.25f) > 0f;
                }
                if (_kind.Glows) _sr.color = Color.Lerp(new Color(1f, 1f, 0.8f), Color.white * 1.2f, 0.5f + 0.5f * Mathf.Sin(t * 4f));

                var king = FindFirstObjectByType<PlayerAvatar>();
                if (Time.time > _leaveAt || (king != null && Vector2.Distance(king.transform.position, transform.position) < ScareRange))
                    Escape();
            }

            void Escape()
            {
                _gone = true;
                transform.DOKill();
                var to = (Vector2)transform.position + Random.insideUnitCircle.normalized * 4f + Vector2.up * 2f;
                transform.DOMove(to, 1.1f).SetEase(Ease.InQuad);
                _sr.DOFade(0f, 1.1f).OnComplete(() => Destroy(gameObject));
            }
        }
    }
}
