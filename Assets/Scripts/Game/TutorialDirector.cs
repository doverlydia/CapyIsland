using CapyIsland.UI;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Walks the player through the opening: collect a bubble, plant it, collect the next, buy a
    /// capybara, put it to work. Mirrors the original's order — forest, sawmill, capy house, worker
    /// — because that sequence teaches the production chain one link at a time.
    ///
    /// Each step is a condition plus a line and a thing to point at. The director polls rather than
    /// subscribing to a dozen events, which keeps every step readable in one place and means it
    /// recovers correctly if the player does things out of order.
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        enum Step
        {
            Greet,
            CollectForest,
            PlantForest,
            CollectSawmill,
            PlantSawmillNearTrees,
            CollectHouse,
            PlantHouse,
            BuyCapy,
            AssignCapy,
            WaitForHunger,
            CollectGarden,
            PlantGarden,
            ClickMote,
            CrossToHeart,
            OpenTower,
            Done,
        }

        [SerializeField] GhostDialogue dialogue;
        [SerializeField] TutorialPointer pointer;

        [Header("Hunger beat")]
        [Tooltip("The garden item stashed inactive in the scene; a bubble for it appears when a worker first falls asleep.")]
        [SerializeField] Placeable gardenItem;
        [SerializeField] Transform bubbleParent;

        Bubble _gardenBubble;

        [Tooltip("Seconds the opening line shows before the first pointer appears.")]
        [SerializeField] float greetSeconds = 3.5f;

        Step _step = Step.Greet;
        float _stepTimer;

        public bool Finished => _step == Step.Done;

        public static TutorialDirector Instance { get; private set; }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>The current step as a short to-do, for the objective tracker.</summary>
        public string CurrentGoal => _step switch
        {
            Step.Greet => "Look around your new island",
            Step.CollectForest => "Pop the bubble to get a forest",
            Step.PlantForest => "Drag the forest onto the grass",
            Step.CollectSawmill => "Pop the bubble to get a sawmill",
            Step.PlantSawmillNearTrees => "Plant the sawmill beside the trees",
            Step.CollectHouse => "Pop the bubble to get a house",
            Step.PlantHouse => "Plant the capy house",
            Step.BuyCapy => "Buy your first capybara",
            Step.AssignCapy => "Drag the capybara onto the sawmill",
            Step.WaitForHunger => "Watch your capybara work",
            Step.CollectGarden => "Pop the garden bubble",
            Step.PlantGarden => "Plant flowers by the sleepy capybara",
            Step.ClickMote => "Tap the warm heart following you",
            Step.CrossToHeart => "Walk across the new bridge",
            Step.OpenTower => "Find the Steam Tower and tap it",
            _ => null,
        };

        /// <summary>Where the current step wants you to look, if anywhere.</summary>
        public Vector2? GoalPoint => pointer != null ? pointer.Target : null;

        /// <summary>A returning player: no tutorial (Tasks.GameStarted).</summary>
        public void SkipForReturningPlayer()
        {
            _step = Step.Done;
            if (pointer != null) pointer.Hide();
        }

        /// <summary>The step to keep in the save ("" once it is over), so closing the app mid-tutorial loses nothing.</summary>
        public string SavedStep => _step == Step.Done ? "" : _step.ToString();

        /// <summary>
        /// Back from a save made mid-tutorial: carry on from that step. An empty or unknown step
        /// (every save from before steps were kept) means the tutorial is over.
        /// </summary>
        public void Resume(string step)
        {
            if (string.IsNullOrEmpty(step) || !System.Enum.TryParse(step, out Step saved) || saved == Step.Done)
            {
                SkipForReturningPlayer();
                return;
            }
            Advance(saved);
            if (saved == Step.CollectGarden || saved == Step.PlantGarden) FindSavedGarden();
            if (saved == Step.OpenTower) WaitForDiary();
        }

        /// <summary>
        /// The save brought the garden back as a copy (in its bubble, or in the trail), not as the
        /// scene's stashed one the steps were watching: watch the copy instead.
        /// </summary>
        void FindSavedGarden()
        {
            foreach (var b in FindObjectsByType<Bubble>(FindObjectsSortMode.None))
                if (!b.Taken && b.Contents != null && b.Contents.TryGetComponent<Placeable>(out var inBubble) && inBubble.DisplayName.Contains("Garden"))
                {
                    _gardenBubble = b;
                    gardenItem = inBubble;
                    return;
                }
            Placeable planted = null;
            foreach (var p in FindObjectsByType<Placeable>(FindObjectsSortMode.None))
            {
                if (!p.DisplayName.Contains("Garden")) continue;
                if (!p.Placed) { gardenItem = p; return; }
                planted = p;
            }
            // Already in the ground: the step is as good as done.
            if (planted != null) gardenItem = planted;
            else AwakenSpirits();
        }

        void WaitForDiary()
        {
            _towerOpened = false;
            TowerOfLove.DiaryClosed -= OnDiaryClosed;
            TowerOfLove.DiaryClosed += OnDiaryClosed;
        }
        float _greetTimer;

        void OnEnable() => Hints.Shown += SayHint;
        void OnDisable() => Hints.Shown -= SayHint;

        // A hint is an event: if it fires again, it should show again.
        void SayHint(string line)
        {
            if (dialogue != null) dialogue.Say(line, again: true);
        }

        void Start()
        {
            // A returning player (or a visitor on someone else's island) never sees the opening.
            if (Visit.Active || SaveGame.HasSave) SkipForReturningPlayer();
            if (_step == Step.Done) return;
            // A brand-new game opens with the story of the Big Chill; Old Yuzu says hello once it ends.
            if (!UI.IntroCutscene.Seen) { UI.IntroCurtain.Drop(); UI.AwakeningScreen.Show(() => UI.IntroCutscene.Show()); }
            _greetTimer = greetSeconds;
            // Her hello is a story line, not an instruction: the first "tap the bubble" waits for
            // it to be read instead of replacing it.
            if (!Guidance.Off && dialogue != null)
                dialogue.Say("Hello, little one! I'm Old Yuzu, or what's left of me. The snow is gone, but my island is so empty. Let's start with a home.");
        }

        void Update()
        {
            if (dialogue != null) dialogue.HoldLastLine = _step != Step.Done && !Guidance.Off;

            switch (_step)
            {
                case Step.Greet: TickGreet(); break;
                case Step.CollectForest: TickCollect("Forest", Step.PlantForest,
                    "Tap the bubble to collect the pine forest inside it."); break;
                case Step.PlantForest: TickPlant("Forest", Step.CollectSawmill,
                    "Planting time! Drag the tree out and drop it to plant a forest."); break;
                case Step.CollectSawmill: TickCollect("Sawmill", Step.PlantSawmillNearTrees,
                    "There's another bubble. Tap it to find a Sawmill."); break;
                case Step.PlantSawmillNearTrees: TickPlantSawmill(); break;
                case Step.CollectHouse: TickCollect("House", Step.PlantHouse,
                    "One more bubble. That one holds a Capy House."); break;
                case Step.PlantHouse: TickPlant("House", Step.BuyCapy,
                    "Drag the Capy House out and drop it anywhere on the grass."); break;
                case Step.BuyCapy: TickBuy(); break;
                case Step.AssignCapy: TickAssign(); break;
                case Step.WaitForHunger: TickWaitForHunger(); break;
                case Step.CollectGarden: TickCollectGarden(); break;
                case Step.PlantGarden: TickPlantGarden(); break;
                case Step.ClickMote: TickClickMote(); break;
                case Step.CrossToHeart: TickCrossToHeart(); break;
                case Step.OpenTower: TickOpenTower(); break;
            }
            // The tutorial was ended: its steps still move the story on, but nothing points.
            if (Guidance.Off && pointer != null) pointer.Hide();
        }

        // ---------------------------------------------------------------- steps

        void TickGreet()
        {
            _greetTimer -= Time.deltaTime;
            if (_greetTimer > 0f) return;
            _step = Step.CollectForest;
        }

        void TickCollect(string nameFragment, Step next, string line)
        {
            var bubble = FindBubble(nameFragment);
            if (bubble == null) { Advance(next); return; }

            Say(line);
            if (pointer != null) pointer.PointAt(bubble.transform);
        }

        void TickPlant(string nameFragment, Step next, string line)
        {
            var item = FindPlaceable(nameFragment);
            if (item == null) return;                       // still inside its bubble
            if (item.Placed) { Advance(next); return; }

            Say(CircleLineOr(item, line, PlantCircleLine));
            // Show the drag: from the thing in your trail to the nearest open ground beside the king.
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king != null) ShowDrag(item, (Vector2)king.transform.position + new Vector2(1.4f, 0.4f), 7f);
        }

        void TickPlantSawmill()
        {
            var mill = FindPlaceable("Sawmill");
            if (mill == null) return;

            if (mill.Placed)
            {
                var workplace = mill.GetComponent<Workplace>();
                // Planted, but out of reach of any tree: say so rather than let it look broken.
                if (workplace != null && !workplace.HasWorkInReach())
                {
                    Say("That sawmill is too far from the trees. Its capybaras can't walk that far to chop.");
                    var forest = FindPlaceable("Forest");
                    if (pointer != null && forest != null) pointer.PointAt(forest.transform);
                    return;
                }
                Advance(Step.CollectHouse);
                return;
            }

            Say(CircleLineOr(mill, "Drop the sawmill near your trees. Its capybaras chop the forest around it.", PlantCircleLine));
            var trees = FindPlaceable("Forest");
            if (trees != null) ShowDrag(mill, (Vector2)trees.transform.position + new Vector2(2.6f, -1.4f), 4f);
        }

        void TickBuy()
        {
            SaleDisplay shop = null;
            foreach (var d in FindObjectsByType<SaleDisplay>(FindObjectsSortMode.None))
                if (d.StoreName == "Capybara") { shop = d; break; }
            if (shop == null) return;

            if (FindFirstObjectByType<Worker>() != null) { Advance(Step.AssignCapy); return; }

            Say("Tap the capybara by the house to welcome one in. It costs 10 wood and 10 gold.");
            if (pointer != null) pointer.PointAt(shop.transform);
        }

        void TickAssign()
        {
            var worker = FindFirstObjectByType<Worker>();
            if (worker == null) return;

            if (worker.IsAssigned)
            {
                Say("That's the rhythm: forest, sawmill, capybara. Watch the wood pile up with every chop.");
                Advance(Step.WaitForHunger);
                return;
            }

            Say("Now drag the capybara onto the sawmill to put it to work.");
            var mill = FindFirstObjectByType<Workplace>();
            if (pointer != null && mill != null) pointer.DragFrom(worker.transform, mill.transform);
        }

        void TickWaitForHunger()
        {
            Worker sleeper = null;
            foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
                if (w.IsAsleep && w.IsHungry) { sleeper = w; break; }
            if (sleeper == null) return;

            // The problem is on screen; now hand out the fix.
            if (gardenItem != null && bubbleParent != null && _gardenBubble == null)
            {
                var king = FindFirstObjectByType<PlayerAvatar>();
                var at = king != null ? (Vector2)king.transform.position + new Vector2(2.4f, -2.2f) : Vector2.zero;
                _gardenBubble = BubbleSpawner.SpawnAt(gardenItem, at, bubbleParent);
            }

            UI.NapCutscene.Show();
            Say("Your capybara has dozed off. It's hungry! Capybaras eat flowers, and there's a garden in that bubble.");
            Advance(Step.CollectGarden);
        }

        void TickCollectGarden()
        {
            if (_gardenBubble == null || _gardenBubble.Taken) { Advance(Step.PlantGarden); return; }
            Say("Tap the bubble to collect the daisy garden.");
            if (pointer != null) pointer.PointAt(_gardenBubble.transform);
        }

        void TickPlantGarden()
        {
            if (gardenItem == null) { Advance(Step.Done); return; }
            if (gardenItem.Placed) { AwakenSpirits(); return; }

            Say(CircleLineOr(gardenItem, "Plant the garden close to the sleepy capybara so it can reach the flowers.", PlantCircleLine));
            Worker sleeper = null;
            foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
                if (w.IsAsleep && w.IsHungry) { sleeper = w; break; }
            if (sleeper != null) ShowDrag(gardenItem, (Vector2)sleeper.transform.position + new Vector2(2.2f, -1.2f), 3f);
        }

        /// <summary>
        /// The garden is planted (Tasks.TaskClickMote): the first bit of love appears, and the Tower
        /// of Love rises on the next island over.
        /// </summary>
        void AwakenSpirits()
        {
            Motes.Trigger("first_mote");
            var land = LandMap.Instance;
            var catalog = ItemCatalog.Instance;
            if (land != null && catalog != null)
                catalog.MakeItem("Tower Of Love", land.SpotNearIslandMiddle(1, 83f));   // the middle of its island
            Say("Oh! A warm heart! All your building is waking the island up. The spirits can feel it.");
            _stepTimer = 6f;
            Advance(Step.ClickMote);
        }

        void TickClickMote()
        {
            if (Motes.IsClicked("First Love") || FindObjectsByType<MoteHeart>(FindObjectsSortMode.None).Length == 0)
            {
                OpenFirstBridge();
                return;
            }
            _stepTimer -= Time.deltaTime;
            if (_stepTimer > 0f) return;
            Say("That glowing heart following you is a warm heart. Tap it to send it home.");
            var heart = FindFirstObjectByType<MoteHeart>();
            if (pointer != null && heart != null) pointer.PointAt(heart.transform);
        }

        /// <summary>The first mote is home (Tasks.TaskFindTower): the way to the next island opens.</summary>
        void OpenFirstBridge()
        {
            var land = LandMap.Instance;
            var catalog = ItemCatalog.Instance;
            if (land != null)
            {
                land.OpenBridge("first_bridge");
                if (catalog != null)
                {
                    BridgeGhost.Spawn("Ginger Cove", land.RandomSpotOnIsland(1, 30f), catalog.ItemParent);
                    catalog.MakeStore("Coriander", land.RandomSpotOnIsland(1, 30f));
                }
            }
            Say("Wonderful! A bridge has risen to the next island. Hold your finger down to walk your capybara across.");
            _stepTimer = 10f;
            Advance(Step.CrossToHeart);
        }

        void TickCrossToHeart()
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            var land = LandMap.Instance;
            if (king != null && land != null && land.IslandAt(king.transform.position) == 1)
            {
                Say("Ah... this is the heart of the Capy Isles. The old hot spring is here somewhere.");
                _stepTimer = 10f;
                WaitForDiary();
                Advance(Step.OpenTower);
                return;
            }
            if (pointer != null && land != null)
            {
                var bridge = land.Bridge("first_bridge");
                if (bridge != null && bridge.circles.Count > 0)
                    pointer.PointAt(LandMap.WorldToUnity(new Vector2(bridge.circles[2].x, bridge.circles[2].y)));
            }
        }

        bool _towerOpened;
        void OnDiaryClosed() => _towerOpened = true;

        void TickOpenTower()
        {
            if (_towerOpened)
            {
                TowerOfLove.DiaryClosed -= OnDiaryClosed;
                Say("Whenever you're stuck, visit the Steam Tower. Bring the warmth back to the islands!");
                Advance(Step.Done);
                return;
            }
            _stepTimer -= Time.deltaTime;
            if (_stepTimer > 0f) return;
            _stepTimer = 20f;
            Say("Find the tall Steam Tower and tap it. Let's see what's inside.");
            var tower = TowerOfLove.Instance;
            if (pointer != null && tower != null) pointer.PointAt(tower.transform);
        }

        // ---------------------------------------------------------------- helpers

        Placeable _spotFor;
        Vector2 _spotWanted, _spot;
        float _spotCheckAt;

        /// <summary>
        /// The pointing hand drags the item to where it can really be planted: the nearest spot to
        /// <paramref name="wanted"/> (within <paramref name="reach"/>) where its circle would be
        /// green. Copying the hand must never end in a red circle. The spot is kept while it stays
        /// clear, so the hand doesn't wander as the king walks.
        /// </summary>
        void ShowDrag(Placeable item, Vector2 wanted, float reach)
        {
            if (pointer == null || item == null) return;
            bool stale = item != _spotFor || (wanted - _spotWanted).sqrMagnitude > 9f;
            if (stale || Time.time >= _spotCheckAt)
            {
                _spotCheckAt = Time.time + 0.5f;
                if (stale || !Roomy(item, _spot, pointer.FingertipLift) || !OnScreen(_spot, pointer.FingertipLift))
                {
                    _spotFor = item;
                    _spotWanted = wanted;
                    _spot = OpenSpotNear(item, wanted, reach, pointer.FingertipLift) ?? wanted;
                }
            }
            pointer.DragFrom(item.transform, _spot);
        }

        /// <summary>
        /// Clear with room to spare. Someone copying the hand lets go where its fingertip is (a
        /// little above the point it aims at) and never exactly on it, so the circle has to be green
        /// at the point, under the fingertip, and a finger's width around both.
        /// </summary>
        static bool Roomy(Placeable item, Vector2 p, float lift)
        {
            var tip = p + Vector2.up * lift;
            if (!item.CanPlaceAt(p) || !item.CanPlaceAt(tip)) return false;
            var mid = (p + tip) * 0.5f;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                if (!item.CanPlaceAt(mid + new Vector2(Mathf.Cos(a) * 0.7f, Mathf.Sin(a) * 0.45f))) return false;
            }
            return true;
        }

        /// <summary>
        /// The closest spot to <paramref name="wanted"/> the item fits, preferring one you can see:
        /// a hand dragging off the edge of the screen shows nothing. Only if nothing on screen will
        /// do does it look further.
        /// </summary>
        static Vector2? OpenSpotNear(Placeable item, Vector2 wanted, float reach, float lift) =>
            OpenSpotNear(item, wanted, reach, lift, true) ?? OpenSpotNear(item, wanted, reach, lift, false);

        /// <summary>Well inside the screen, clear of the edges and the bars along them.</summary>
        static bool OnScreen(Vector2 p, float lift)
        {
            var cam = Camera.main;
            if (cam == null) return true;
            foreach (var q in new[] { p, p + Vector2.up * lift })
            {
                var v = cam.WorldToViewportPoint(q);
                if (v.x < 0.12f || v.x > 0.88f || v.y < 0.18f || v.y > 0.8f) return false;
            }
            return true;
        }

        static Vector2? OpenSpotNear(Placeable item, Vector2 wanted, float reach, float lift, bool seen)
        {
            bool Good(Vector2 p) => (!seen || OnScreen(p, lift)) && Roomy(item, p, lift);
            if (Good(wanted)) return wanted;
            // Rings outward from the wanted spot; the first clear one is the closest.
            for (float r = 0.6f; r <= reach; r += 0.6f)
            {
                int around = Mathf.Max(8, Mathf.RoundToInt(r * 6f));
                float turn = Random.value;   // not always the same side first
                for (int i = 0; i < around; i++)
                {
                    float a = (i / (float)around + turn) * Mathf.PI * 2f;
                    var p = wanted + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    if (Good(p)) return p;
                }
            }
            // Nowhere roomy: at least somewhere it fits.
            for (float r = 0.6f; r <= reach; r += 0.6f)
                for (int i = 0; i < 16; i++)
                {
                    float a = i * Mathf.PI / 8f;
                    var p = wanted + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    if ((!seen || OnScreen(p, lift)) && item.CanPlaceAt(p)) return p;
                }
            return null;
        }

        const string PlantCircleLine =
            "See the circle around it? Green means there's room to plant it there. Red means something is in the way, so move it a little. Let go to plant it.";

        bool _circleExplained;

        /// <summary>
        /// The first time the player actually picks something up, the circle under it is on
        /// screen: that is the moment to explain it. From then on the explanation stays the line
        /// for this step, so the two lines do not take turns.
        /// </summary>
        string CircleLineOr(Placeable item, string line, string circleLine)
        {
            var drag = item != null ? item.GetComponent<DraggableActor>() : null;
            if (drag != null && drag.IsHeld) _circleExplained = true;
            return _circleExplained ? circleLine : line;
        }

        void Advance(Step next)
        {
            _step = next;
            _circleExplained = false;
            if (pointer != null) pointer.Hide();
        }

        void Say(string line)
        {
            if (Guidance.Off) return;   // the tutorial was ended: the story goes on, quietly
            // the latest instruction replaces any older one still waiting, so she never lags behind
            if (dialogue != null) dialogue.Instruct(line);
        }

        static Bubble FindBubble(string nameFragment)
        {
            foreach (var b in FindObjectsByType<Bubble>(FindObjectsSortMode.None))
            {
                if (b.Taken || b.Contents == null) continue;
                if (b.Contents.name.Contains(nameFragment)) return b;
            }
            return null;
        }

        static Placeable FindPlaceable(string nameFragment)
        {
            foreach (var p in FindObjectsByType<Placeable>(FindObjectsSortMode.None))
                if (p.DisplayName.Contains(nameFragment)) return p;
            return null;
        }
    }
}
