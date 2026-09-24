using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A monster (Animals/MonsterItem). **Wild**, it lumbers about the whole map at a crawl
    /// (int(6/5) = 1 unit a frame) wrecking trees and rocks — 0.2 off a tree and 0.4 off a rock per
    /// blow — for nobody's benefit, and never gets hungry. **Tap it** and, for 20 gems, it signs on
    /// for life: from then on it is an ordinary worker, fast on rock (speed 12), and joins your
    /// trail.
    /// </summary>
    [RequireComponent(typeof(Worker))]
    public class MonsterAnimal : WildForager, IWild, ITappable
    {
        public const int TameCost = 20;

        [SerializeField] bool tame;

        Worker _worker;
        DraggableActor _drag;
        Follower _follower;
        WorkerVisuals _visuals;

        public bool Tame => tame;

        protected override int WalkSpeed => 6 / 5;

        protected override bool Running => !tame;

        protected override string ArrivalHint =>
            "Angry monsters are stealing your rocks! Pay it some gems to get it to switch sides.";

        protected override void Awake()
        {
            base.Awake();
            _worker = GetComponent<Worker>();
            _drag = GetComponent<DraggableActor>();
            _follower = GetComponent<Follower>();
            _visuals = GetComponent<WorkerVisuals>();
        }

        void OnEnable()
        {
            TapTargets.Register(this);
            ApplyTameness();
        }

        void OnDisable() => TapTargets.Unregister(this);

        /// <summary>Bought from a Monster House, dropped from a felled fruit tree, or out of a bubble.</summary>
        public void GoWild()
        {
            tame = false;
            ApplyTameness();
            BeginLife();
        }

        /// <summary>A worker's parts only run once it is tame.</summary>
        void ApplyTameness()
        {
            if (_worker != null) _worker.enabled = tame;
            if (_visuals != null) _visuals.enabled = tame;
            if (_drag != null) _drag.enabled = tame;
            if (_follower != null) _follower.enabled = tame;
        }

        protected override void Search()
        {
            TargetNode = FindNode(n => n.Family == NodeFamily.Tree || n.Family == NodeFamily.Rock);
        }

        protected override float HitAmount(Harvestable node) => node.Family == NodeFamily.Rock ? 0.4f : 0.2f;

        // ---------------------------------------------------------------- taming

        public float TapDistance(Vector2 worldPoint)
        {
            if (tame) return -1f;
            float d = Vector2.Distance((Vector2)transform.position + Vector2.up * 0.6f, worldPoint);
            return d < 1.1f ? d : -1f;
        }

        public void Tap()
        {
            var store = ResourceStore.Instance;
            var portrait = transform.Find("Sprite")?.GetComponent<SpriteRenderer>()?.sprite;
            if (store == null || store.Get(ResourceKind.Gem) < TameCost)
            {
                UI.SpeechDialog.Show(portrait, "Monster", new[] { "I only work for people with gems." });
                return;
            }
            UI.SpeechDialog.Show(portrait, "Monster",
                new[] { "I'll work for life for 20 Gems and wait for you to login next time." },
                null,
                ("Pay 20 Gems", () => TameNow()),
                ("Cancel", null));
        }

        public void TameNow()
        {
            var store = ResourceStore.Instance;
            if (store == null || !store.TrySpend(0, 0, TameCost)) return;
            Release();
            tame = true;
            ApplyTameness();
            transform.localScale = transform.localScale;   // keep grown size
            if (_follower != null) _follower.PickUpIntoInventory();
            if (HarvestEffects.Instance != null) HarvestEffects.Instance.Twinkle((Vector2)transform.position + Vector2.up * 0.6f, 12);
        }
    }
}
