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
            WaitForWake,
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
        float _greetTimer;

        void OnEnable() => Worker.Hint += Say;
        void OnDisable() => Worker.Hint -= Say;

        void Start()
        {
            _greetTimer = greetSeconds;
            Say("Hi! Welcome to my desolate island. Unless you start building a home, you will soon be dead, just like me.");
        }

        void Update()
        {
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
                case Step.WaitForWake: TickWaitForWake(); break;
            }
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

            Say(line);
            // Point at open ground ahead of the king, so there is somewhere obvious to drop it.
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (pointer != null && king != null)
                pointer.PointAt((Vector2)king.transform.position + new Vector2(3.2f, 2.2f));
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
                    Say("That Sawmill is too far from the trees. It needs a forest within its circle to work.");
                    var forest = FindPlaceable("Forest");
                    if (pointer != null && forest != null) pointer.PointAt(forest.transform);
                    return;
                }
                Advance(Step.CollectHouse);
                return;
            }

            Say("Drop the Sawmill right next to your trees — it only reaches the forest inside its circle.");
            var trees = FindPlaceable("Forest");
            if (pointer != null && trees != null)
                pointer.PointAt((Vector2)trees.transform.position + new Vector2(2.6f, -1.4f));
        }

        void TickBuy()
        {
            var shop = FindFirstObjectByType<ShopBuilding>();
            if (shop == null) return;

            if (FindFirstObjectByType<Worker>() != null) { Advance(Step.AssignCapy); return; }

            Say("Tap the capybara standing beside the house to buy one. It costs 10 wood and 10 gold.");
            if (pointer != null) pointer.PointAt(shop.transform);
        }

        void TickAssign()
        {
            var worker = FindFirstObjectByType<Worker>();
            if (worker == null) return;

            if (worker.IsAssigned)
            {
                Say("That's the loop: forest, sawmill, capybara. Watch the wood climb with every chop.");
                Advance(Step.WaitForHunger);
                return;
            }

            Say("Now drag the capybara onto the Sawmill to put it to work.");
            var mill = FindFirstObjectByType<Workplace>();
            if (pointer != null && mill != null) pointer.PointAt(mill.transform);
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

            Say("Your capybara fell asleep - it's hungry. Capybaras eat flowers. There's a garden in that bubble.");
            Advance(Step.CollectGarden);
        }

        void TickCollectGarden()
        {
            if (_gardenBubble == null || _gardenBubble.Taken) { Advance(Step.PlantGarden); return; }
            Say("Tap the bubble to collect the posie garden.");
            if (pointer != null) pointer.PointAt(_gardenBubble.transform);
        }

        void TickPlantGarden()
        {
            if (gardenItem == null) { Advance(Step.Done); return; }
            if (gardenItem.Placed) { Advance(Step.WaitForWake); return; }

            Say("Plant the garden near the sleeping capybara so it can reach the flowers.");
            Worker sleeper = null;
            foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
                if (w.IsAsleep && w.IsHungry) { sleeper = w; break; }
            if (pointer != null && sleeper != null)
                pointer.PointAt((Vector2)sleeper.transform.position + new Vector2(2.2f, -1.2f));
        }

        void TickWaitForWake()
        {
            bool anyStarving = false;
            foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
                if (w.IsAsleep && w.IsHungry) { anyStarving = true; break; }
            if (anyStarving) return;

            Say("Fed and back to work. Gardens get eaten out, so plant more as your capybaras multiply.");
            Advance(Step.Done);
        }

        // ---------------------------------------------------------------- helpers

        void Advance(Step next)
        {
            _step = next;
            if (pointer != null) pointer.Hide();
        }

        void Say(string line)
        {
            if (dialogue != null) dialogue.Say(line);
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
