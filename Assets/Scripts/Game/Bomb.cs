using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A Bomb or Deer Bomb (Terrain/BombItem, BombStates/BombFuse). Planted, it fizzes for three
    /// seconds, then goes off, hitting everything whose footprint touches its 300-unit blast:
    ///
    ///   * **Deer Bomb** — deer and wild monsters are blown away; nothing else is touched.
    ///   * **Bomb** — deer and wild monsters are blown away too, and everything else in reach is
    ///     wrapped back into a free bubble of itself, ready to pick up and replant: forests, gardens,
    ///     buildings, stores, even working animals. Children of a grove are simply destroyed.
    ///     Never touched: things in your trail, the quest givers, the Church and Priest, shop
    ///     displays and bubbles, other bombs, and animals following you.
    ///
    /// Timing, in 30 Hz frames: bang at 85, effect at 90, gone when the blast finishes (~102).
    /// </summary>
    public class Bomb : MonoBehaviour
    {
        [SerializeField] bool deerOnly;

        FrameClock _clock;
        int _counter = -1;
        Transform _body;
        SpriteRenderer _bodyRenderer;

        public bool DeerOnly => deerOnly;
        public bool Armed => _counter >= 0;

        public void Configure(bool deerOnly) => this.deerOnly = deerOnly;

        void Awake()
        {
            _body = transform.Find("Sprite");
            _bodyRenderer = _body != null ? _body.GetComponent<SpriteRenderer>() : null;
        }

        void OnPlacedInWorld()
        {
            _counter = 0;
            Sfx.PlayAt(SfxKind.Fuse, transform.position);
        }

        void OnReturnedToBubble() => _counter = -1;

        void Update()
        {
            if (_counter < 0) return;
            int frames = _clock.Consume(Time.deltaTime);
            for (int i = 0; i < frames; i++)
                if (Tick()) return;
            Fizz();
        }

        bool Tick()
        {
            _counter++;
            if (_counter == 90) Explode();
            if (_counter >= 102)
            {
                Destroy(gameObject);
                return true;
            }
            return false;
        }

        /// <summary>The fuse: a quickening throb, sparks off the top.</summary>
        void Fizz()
        {
            if (_body == null || _counter >= 90) return;
            float t = _counter / 90f;
            float rate = Mathf.Lerp(6f, 30f, t);
            float swell = 1f + Mathf.Abs(Mathf.Sin(Time.time * rate)) * Mathf.Lerp(0.04f, 0.16f, t);
            _body.localScale = new Vector3(swell, 2f - swell, 1f);
            if (_bodyRenderer != null) _bodyRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.45f), Mathf.Abs(Mathf.Sin(Time.time * rate)) * t);
            if (Random.value < 0.25f && HarvestEffects.Instance != null && _bodyRenderer != null)
                HarvestEffects.Instance.Twinkle(new Vector2(_bodyRenderer.bounds.center.x + 0.15f, _bodyRenderer.bounds.max.y), 1);
        }

        void Explode()
        {
            var fx = HarvestEffects.Instance;
            Vector2 at = transform.position;
            Sfx.PlayAt(SfxKind.Boom, at, 1f, 0.05f);
            if (fx != null)
            {
                fx.Felled(at, NodeFamily.Rock);
                fx.Twinkle(at + Vector2.up * 0.5f, 20);
            }
            if (_body != null)
            {
                _body.localScale = Vector3.one;
                _body.DOScale(2.2f, 0.35f).SetEase(Ease.OutQuad);
                if (_bodyRenderer != null) _bodyRenderer.DOFade(0f, 0.35f);
            }
            CameraRig.CameraShake.Kick(0.35f, 0.4f);

            float blast = StoreData.Get(deerOnly ? "Deer Bomb" : "Bomb").Outer;
            foreach (var victim in InBlast(at, blast)) Hit(victim);
        }

        /// <summary>Everything whose footprint touches the blast circle.</summary>
        List<GameObject> InBlast(Vector2 at, float blast)
        {
            var hits = new List<GameObject>();
            bool Touches(Vector2 p, float r) => BunniSpace.WorldDistance(at, p) < blast + r;

            foreach (var node in FindObjectsByType<Harvestable>(FindObjectsSortMode.None))
                if (node.InWorld && Touches(node.transform.position, node.InnerRadius)) hits.Add(node.gameObject);
            foreach (var p in FindObjectsByType<Placeable>(FindObjectsSortMode.None))
                if (p.Placed && p.GetComponent<Harvestable>() == null && Touches(p.transform.position, p.InnerRadius) && !hits.Contains(p.gameObject))
                    hits.Add(p.gameObject);
            foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
                if (Touches(w.transform.position, 0f)) hits.Add(w.gameObject);
            foreach (var d in FindObjectsByType<DeerAnimal>(FindObjectsSortMode.None))
                if (Touches(d.transform.position, 0f)) hits.Add(d.gameObject);
            hits.Remove(gameObject);
            return hits;
        }

        void Hit(GameObject victim)
        {
            if (victim == null) return;
            if (victim.TryGetComponent<CatCare.CatView>(out var rider) && rider.OnHead) return;   // safe on your head

            bool wild = victim.GetComponent<DeerAnimal>() != null
                        || (victim.TryGetComponent<MonsterAnimal>(out var monster) && !monster.Tame);
            if (wild) { Hazards.NotePestCleared(victim.transform.position); BlowAway(victim); return; }
            if (deerOnly) return;

            // Spared by a normal bomb.
            if (victim.GetComponent<QuestGiver>() != null || victim.GetComponent<PiratePete>() != null) return;
            if (victim.GetComponent<Church>() != null || victim.GetComponent<Bomb>() != null) return;
            if (victim.TryGetComponent<Worker>(out var worker) && !worker.IsAssigned) return;   // following you

            var tag = victim.GetComponent<CatalogItem>();
            var placeable = victim.GetComponent<Placeable>();
            var catalog = ItemCatalog.Instance;

            // Fixed scenery (the stone lanterns, Old Mochi's shrine) only holds its spot with a
            // footprint; it can't ride in your trail, so a bubble of it would never come back.
            if (placeable != null && victim.GetComponent<Follower>() == null) return;
            if (placeable != null)
            {
                // A store item planted in the world goes back into a bubble of itself.
                BubbleSpawner.ReturnToBubble(placeable);
                return;
            }
            if (tag != null && catalog != null)
            {
                // A working animal: a free bubble of its kind, and the animal itself is gone.
                if (worker != null) worker.Unassign();
                catalog.MakeStore(tag.StoreName, victim.transform.position);
                Destroy(victim);
                return;
            }
            // Unnamed things — the children of a grove — are simply blown away.
            BlowAway(victim);
        }

        static void BlowAway(GameObject victim)
        {
            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Poof(victim.transform.position, new Color(0.6f, 0.55f, 0.5f));
            victim.transform.DOKill();
            victim.transform.DOScale(0f, 0.36f).SetEase(Ease.InBack).OnComplete(() => { if (victim != null) Destroy(victim); });
            foreach (var c in victim.GetComponents<MonoBehaviour>())
                if (c is not Bomb) c.enabled = false;
        }
    }
}
