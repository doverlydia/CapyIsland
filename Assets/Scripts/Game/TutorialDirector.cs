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
            FindSpring,     // the hot spring is frozen: find it and tap it
            FeedSpring,     // bring wood for a fire to thaw it
            OpenTower,      // warm again: the first soak, then the diary
            Done,
        }

        // Saved by name (SavedStep), so steps can be added anywhere; the order is what counts here.

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

        /// <summary>
        /// The opening was played to its end just now (not skipped, not an old save): Old Yuzu
        /// leaves her corner and moves up into the goal card (ObjectiveTracker).
        /// </summary>
        public static event System.Action Graduated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => Graduated = null;

        public static TutorialDirector Instance { get; private set; }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (Placeable.ZoneFor != null) Placeable.ZoneFor = null;
            TowerOfLove.DiaryClosed -= OnDiaryClosed;
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
            Step.FindSpring => StoryText.Spring.GoalFind,
            // The fire takes all its wood at once: the count is what you've gathered toward it.
            Step.FeedSpring => string.Format(StoryText.Spring.GoalFeed, Mathf.Min(_fed + WoodHeld, FireWood), FireWood),
            // Already soaking: done, though the step itself ends when the goals page closes.
            Step.OpenTower => HotSpring.Bathing ? null : StoryText.Spring.GoalSoak,
            _ => null,
        };

        static int WoodHeld => ResourceStore.Instance != null ? ResourceStore.Instance.Get(ResourceKind.Wood) : 0;

        /// <summary>A second line under the goal, when the step needs one.</summary>
        public string CurrentGoalNote => _step == Step.FeedSpring && _fed < FireWood ? StoryText.Spring.GoalFeedNote : null;

        /// <summary>Where the current step wants you to look, if anywhere (the spring, at the end, even while the hand rests).</summary>
        public Vector2? GoalPoint
        {
            get
            {
                if (!Guidance.Off && (_step is Step.FindSpring or Step.FeedSpring or Step.OpenTower) && HotSpring.Instance != null)
                    return HotSpring.Instance.PoolBounds.center;
                return pointer != null ? pointer.Target : null;
            }
        }

        /// <summary>A returning player: no tutorial (Tasks.GameStarted).</summary>
        public void SkipForReturningPlayer()
        {
            _step = Step.Done;
            if (pointer != null) pointer.Hide();
            ZoomHint.Hide();
        }

        // ---------------------------------------------------------------- the save

        /// <summary>Marks a game that started with the lean purse and is still owed Miso's start-up gift.</summary>
        const string GiftFlag = "+gift";

        /// <summary>
        /// The step to keep in the save, so closing the app mid-tutorial loses nothing: its name,
        /// the wood fed to the spring's fire so far ("FeedSpring:37"), and "+gift" while Miso's
        /// start-up gift is still to come. "" once the tutorial is over with nothing owed, as in
        /// every save from before. (Old builds read anything they don't know as "over".)
        /// On a friend's island your own save's value is kept just as it was.
        /// </summary>
        public string SavedStep
        {
            get
            {
                if (Visit.Active) return _homeStep;
                string s = _step == Step.Done ? "" : _step == Step.FeedSpring ? "FeedSpring:" + _fed : _step.ToString();
                return _giftOwed ? s + GiftFlag : s;
            }
        }

        string _homeStep = "";

        /// <summary>
        /// Back from a save made mid-tutorial: carry on from that step. An empty or unknown step
        /// (every save from before steps were kept) means the tutorial is over.
        /// </summary>
        public void Resume(string step)
        {
            step ??= "";
            _giftOwed = step.EndsWith(GiftFlag, System.StringComparison.Ordinal);
            if (_giftOwed) step = step.Substring(0, step.Length - GiftFlag.Length);
            int colon = step.IndexOf(':');
            _fed = 0;
            if (colon >= 0)
            {
                int.TryParse(step.Substring(colon + 1), out _fed);
                step = step.Substring(0, colon);
            }
            if (string.IsNullOrEmpty(step) || !System.Enum.TryParse(step, out Step saved) || saved == Step.Done)
            {
                SkipForReturningPlayer();
                return;
            }
            // Fed in full but closed before the thaw played: it is warm, on to the soak.
            if (saved == Step.FeedSpring && _fed >= FireWood) saved = Step.OpenTower;
            Advance(saved);
            if (saved == Step.CollectGarden || saved == Step.PlantGarden) FindSavedGarden();
            if (saved == Step.OpenTower) { WaitForDiary(); _stepTimer = 3f; }
        }

        // ---------------------------------------------------------------- the frozen spring

        /// <summary>
        /// Wood the fire needs to thaw the spring. A new game has 90 wood here (100, less 10 for
        /// the first capybara). One capybara at one sawmill on the opening's pines brings in 3 wood a
        /// trip, a trip about 7.5 s (10–11 swings of 0.5 s on a child pine, plus a short walk each
        /// way), and needs a posie every two trips or so: roughly 18–20 wood a minute while fed. From
        /// putting it to work (step 8) to reaching the spring is about 100 s, with ~25 s of that
        /// spent asleep and hungry until the garden goes in, so the player arrives holding about
        /// 110–115 and waits a few loads more: about two minutes in all, from step 8 to a lit fire.
        /// Asking for more than the starting wood is what makes the chopping count, and giving
        /// what you hold reads more simply than counting only wood chopped since.
        /// </summary>
        public const int FireWood = 120;

        int _fed;
        bool _giftOwed;
        float _thawStarted = -1f, _soakAt = -1f;
        bool _springWarm;

        /// <summary>
        /// The spring is iced over: in a new game from the moment the Steam Tower rises until the
        /// fire is fed. Never once the tutorial is ended (Settings), on a friend's island, or in a
        /// game from before the spring froze.
        /// </summary>
        public static bool SpringFrozen
        {
            get
            {
                var t = Instance;
                if (t == null || Visit.Active || Guidance.Off) return false;
                if (t._step >= Step.OpenTower) return false;
                return !(t._step == Step.FeedSpring && t._fed >= FireWood);
            }
        }

        /// <summary>Wood given to the spring's fire so far.</summary>
        public static int SpringWoodFed => Instance != null ? Instance._fed : 0;

        /// <summary>
        /// Miso's start-up gift, once: true (and from then on false) for a game that began with
        /// the lean 100 / 100 purse. Games from before never get it.
        /// </summary>
        public static bool TakeStartGift()
        {
            var t = Instance;
            if (t == null || Visit.Active || !t._giftOwed) return false;
            t._giftOwed = false;
            return true;
        }

        static Sprite YuzuFace => UI.GhostDialogue.Instance != null ? UI.GhostDialogue.Instance.GhostFace : null;

        /// <summary>The frozen spring was tapped (HotSpring.TapFrozen).</summary>
        public void SpringTapped()
        {
            switch (_step)
            {
                case Step.FeedSpring:
                    OpenFirePanel();
                    return;
                case Step.FindSpring:
                    UI.SpeechDialog.Show(YuzuFace, StoryText.Guide, StoryText.Spring.Frozen, () =>
                    {
                        if (_step != Step.FindSpring) return;
                        Advance(Step.FeedSpring);
                        OpenFirePanel();
                    });
                    return;
                default:
                    UI.SpeechDialog.Show(YuzuFace, StoryText.Guide, new[] { StoryText.Spring.FrozenFromAfar });
                    return;
            }
        }

        /// <summary>
        /// The fire: how much it needs, what you hold, and a button to give it all at once. Giving in
        /// dribs left the player with no wood for the shops and a long wait; paid in one go, the
        /// wood in hand can buy another capybara for the sawmill until there's enough.
        /// </summary>
        void OpenFirePanel()
        {
            var store = ResourceStore.Instance;
            int have = store != null ? store.Get(ResourceKind.Wood) : 0;
            int need = FireWood - _fed;   // an island from before this rule may have given some already
            bool enough = have >= need;
            string page = string.Format(StoryText.Spring.FireNeeds, need) + "\n"
                        + string.Format(StoryText.Spring.FireHave, have) + "\n"
                        + (enough ? StoryText.Spring.FireShops : StoryText.Spring.FireShort);
            var choices = new System.Collections.Generic.List<UI.SpeechDialog.Choice>
            {
                new(enough ? string.Format(StoryText.Spring.Give, need) : StoryText.Spring.NotEnough, () => FeedFire(need), enough),
                new(StoryText.Spring.NotNow, null),
            };
            UI.SpeechDialog.ShowChoices(YuzuFace, StoryText.Guide, new[] { page }, null, choices);
        }

        void FeedFire(int wood)
        {
            var store = ResourceStore.Instance;
            if (_step != Step.FeedSpring || wood < FireWood - _fed || store == null || !store.TrySpend(wood, 0, 0)) return;
            _fed += wood;
            var king = FindFirstObjectByType<PlayerAvatar>();
            var spring = HotSpring.Instance;
            if (spring != null) spring.Feed(wood, king != null ? (Vector2)king.transform.position : spring.PitPoint);
            // That's all of it: SpringFrozen turns false and the spring plays its thaw.
            _thawStarted = Time.time;
            _springWarm = false;
            if (pointer != null) pointer.Hide();
        }

        void OnSpringThawed() => _springWarm = true;

        void TickFindSpring()
        {
            if (Guidance.Off) { SkipSpring(); return; }
            _stepTimer -= Time.deltaTime;
            if (_stepTimer > 0f) return;
            Say(StoryText.Spring.FindIt);
            PointAtSpring();
        }

        void TickFeedSpring()
        {
            if (_fed >= FireWood)
            {
                if (pointer != null) pointer.Hide();
                // The thaw plays (about five seconds); then Old Yuzu, and in we hop.
                if (_thawStarted < 0f)
                {
                    _thawStarted = Time.time;
                    Say(StoryText.Spring.Catching);   // over any "bring more wood" still showing
                }
                if (!_springWarm && Time.time - _thawStarted < 7f) return;
                Tell(StoryText.Spring.Thawed);
                Advance(Step.OpenTower);
                WaitForDiary();
                _stepTimer = 20f;
                _soakAt = Time.time + 2.5f;
                return;
            }
            if (Guidance.Off) { SkipSpring(); return; }

            // Point at the spring once there's all the wood it needs.
            var store = ResourceStore.Instance;
            int have = store != null ? store.Get(ResourceKind.Wood) : 0;
            bool enough = have >= FireWood - _fed;
            Say(enough ? StoryText.Spring.Bring : string.Format(StoryText.Spring.Waiting, FireWood - _fed));
            if (enough) PointAtSpring();
            else if (pointer != null) pointer.Hide();
        }

        /// <summary>The tutorial was ended: the spring thaws by itself, and the soak is there whenever you want it.</summary>
        void SkipSpring()
        {
            Advance(Step.OpenTower);
            WaitForDiary();
            _stepTimer = 20f;
        }

        void PointAtSpring()
        {
            var spring = HotSpring.Instance;
            if (pointer == null) return;
            if (spring != null) pointer.PointAt((Vector2)spring.PoolBounds.center);
            else if (TowerOfLove.Instance != null) pointer.PointAt(TowerOfLove.Instance.transform);
        }

        // ---------------------------------------------------------------- the zoom beat

        enum ZoomBeat { None, Waiting, Showing, Over }

        ZoomBeat _zoomBeat;
        string _zoomLine;
        float _zoomUntil;
        bool _zoomed;

        /// <summary>
        /// While the first capybara works: Old Yuzu says how to zoom, a picture of the gesture shows,
        /// and the first pinch (or turn of the wheel) gets a "nicely done". It never holds anything
        /// up: after 15 seconds, or once the capybara falls asleep, the picture just goes.
        /// </summary>
        void StartZoomBeat()
        {
            if (Guidance.Off) return;
            _zoomLine = ZoomLine();
            Tell(_zoomLine, Step.WaitForHunger);
            _zoomBeat = ZoomBeat.Waiting;
            _zoomUntil = Time.time + 25f;   // for her to get to the line
            _zoomed = false;
        }

        void OnZoomed() => _zoomed = true;

        void TickZoomBeat()
        {
            if (_zoomBeat is ZoomBeat.None or ZoomBeat.Over) return;
            bool moveOn = _step != Step.WaitForHunger || Guidance.Off;
            if (_zoomed && !Guidance.Off)
            {
                ZoomHint.Hide();
                _zoomBeat = ZoomBeat.Over;
                Tell(StoryText.Spring.ZoomDone, Step.WaitForHunger);
                return;
            }
            if (moveOn || Time.time > _zoomUntil)
            {
                ZoomHint.Hide();
                _zoomBeat = ZoomBeat.Over;
                return;
            }
            if (_zoomBeat == ZoomBeat.Waiting && dialogue != null && dialogue.HasSaid(_zoomLine))
            {
                _zoomBeat = ZoomBeat.Showing;
                _zoomed = false;   // only a zoom after she has asked counts
                _zoomUntil = Time.time + 15f;
                ZoomHint.Show();
            }
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

        void OnEnable()
        {
            Hints.Shown += SayHint;
            CameraRig.CameraInputController.Zoomed += OnZoomed;
            HotSpring.Thawed += OnSpringThawed;
        }

        void OnDisable()
        {
            Hints.Shown -= SayHint;
            CameraRig.CameraInputController.Zoomed -= OnZoomed;
            HotSpring.Thawed -= OnSpringThawed;
        }

        // News is an event: if it comes again, it should show again.
        void SayHint(string line)
        {
            if (dialogue != null) dialogue.Say(line, again: true);
        }

        void Start()
        {
            // A returning player (or a visitor on someone else's island) never sees the opening.
            if (Visit.Active || SaveGame.HasSave) SkipForReturningPlayer();
            // On a friend's island, your own save's tutorial value rides through untouched.
            if (Visit.Active) _homeStep = SaveGame.ReadHome()?.tutorial ?? "";
            if (_step == Step.Done) return;
            // A brand-new game starts with the lean purse: Miso makes it up with the General Store.
            _giftOwed = true;
            // A brand-new game opens with the story of the Big Chill; Old Yuzu says hello once it ends.
            if (!UI.IntroCutscene.Seen) { UI.IntroCurtain.Drop(); UI.AwakeningScreen.Show(() => UI.IntroCutscene.Show()); }
            _greetTimer = greetSeconds;
            // Her hello is a story line, not an instruction: the first "tap the bubble" waits for
            // it to be read instead of replacing it.
            if (!Guidance.Off && dialogue != null)
                dialogue.Say("Hello, little one! I'm Old Yuzu, or what's left of me. The snow is gone, but my island is so empty. Let's bring it back to life, one bubble at a time.");
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
                case Step.FindSpring: TickFindSpring(); break;
                case Step.FeedSpring: TickFeedSpring(); break;
                case Step.OpenTower: TickOpenTower(); break;
            }
            TickZoomBeat();
            TickGardenGone();
            // The tutorial was ended: its steps still move the story on, but nothing points. Once it
            // is over the hand belongs to the story (a quest's focus, the chapel), so leave it be.
            if (Guidance.Off && _step != Step.Done && pointer != null) pointer.Hide();
            GlideToGoal();
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

        float _millPlacedAt = -1f;

        void TickPlantSawmill()
        {
            var mill = FindPlaceable("Sawmill");
            if (mill == null) return;
            if (!mill.Placed) _millPlacedAt = -1f;

            if (mill.Placed)
            {
                var workplace = mill.GetComponent<Workplace>();
                // Planted, but out of reach of any tree: say so rather than let it look broken.
                // (A moment's grace first: the forest's trees settle in a frame or two after a
                // drop, and a "too far" that flashes up and vanishes only confuses.)
                if (_millPlacedAt < 0f) _millPlacedAt = Time.time;
                if (workplace != null && !workplace.HasWorkInReach())
                {
                    if (Time.time - _millPlacedAt < 1f) return;
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
                Tell("That's the rhythm: forest, sawmill, capybara. Watch the wood pile up with every chop.", Step.WaitForHunger);
                Advance(Step.WaitForHunger);
                StartZoomBeat();   // a quiet moment while it works: time to look around
                return;
            }

            Say("Now drag the capybara onto the sawmill to put it to work.");
            var mill = FindFirstObjectByType<Workplace>();
            if (pointer != null && mill != null) pointer.DragFrom(worker.transform, mill.transform);
        }

        /// <summary>
        /// How to zoom, the way this device does it: a phone (in the app or in a browser) pinches, a
        /// computer scrolls, and a computer with a touch screen can do either.
        /// </summary>
        static string ZoomLine() =>
            Application.isMobilePlatform ? "While it works, have a look around: pinch with two fingers to zoom in and out."
            : Input.touchSupported ? "While it works, have a look around: pinch with two fingers, or scroll the mouse wheel, to zoom in and out."
            : "While it works, have a look around: scroll the mouse wheel to zoom in and out.";

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
            Tell(NapLine, Step.PlantGarden);
            Advance(Step.CollectGarden);
        }

        /// <summary>
        /// The first nap: why it sleeps, and what the cloud over it is — a wish, not a bubble, and
        /// tapping it shows where to find one (DreamGuide).
        /// </summary>
        const string NapLine = "Your capybara has dozed off. It's hungry! See the little cloud over its head? That's what it's dreaming of. Tap the cloud and I'll show you where to find some flowers.";

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

            // The garden only goes down near the sleeper, so this first
            // garden always feeds it (and the lesson isn't lost to one planted out of reach).
            if (Placeable.ZoneFor != gardenItem)
            {
                Worker sleeper = null;
                foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
                    if (w.IsAsleep && w.IsHungry) { sleeper = w; break; }
                var mill = FindFirstObjectByType<Workplace>();
                Vector2 centre = sleeper != null ? sleeper.transform.position
                               : mill != null ? mill.transform.position
                               : (Vector2)transform.position;
                KeepNear(gardenItem, centre);
            }

            Say(CircleLineOr(gardenItem, "Plant the garden close to the sleepy capybara so it can reach the flowers.", PlantCircleLine));
            ShowDrag(gardenItem, Placeable.ZoneCentre + new Vector2(2.2f, -1.2f), 3f);
        }

        // ---------------------------------------------------------------- the garden's reach

        /// <summary>How far from the sleeper the first garden may go, in original world units.</summary>
        const float GardenReach = 120f;

        /// <summary>
        /// The first garden has to go near the sleeper, so it always feeds it. Nothing is drawn: a
        /// drop too far away turns the placement ring red and Old Yuzu says it's too far.
        /// </summary>
        static void KeepNear(Placeable item, Vector2 centre)
        {
            Placeable.ZoneFor = item;
            Placeable.ZoneCentre = centre;
            Placeable.ZoneRadius = GardenReach;
        }

        static void HideZone()
        {
            if (Placeable.ZoneFor != null) Placeable.ZoneFor = null;
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
            Tell("Oh! A warm heart! All your building is waking the island up. The spirits can feel it.", Step.ClickMote);
            _stepTimer = 6f;
            Advance(Step.ClickMote);
        }

        float _gardenGoneFor;

        /// <summary>
        /// Every flower gone before the spring is lit (guinea pigs eat a garden whole, parent and
        /// all) and no shops yet to buy another: the capybara would sleep hungry for good and the
        /// fire never get its wood. Old Yuzu shoos off any pests and hands over a fresh garden.
        /// </summary>
        void TickGardenGone()
        {
            bool needWood = _step is >= Step.ClickMote and < Step.OpenTower && WoodHeld + _fed < FireWood;
            if (!needWood || Visit.Active || AnyFlowers()) { _gardenGoneFor = 0f; return; }
            _gardenGoneFor += Time.deltaTime;
            if (_gardenGoneFor < 4f) return;
            _gardenGoneFor = 0f;

            var catalog = ItemCatalog.Instance;
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (catalog == null || king == null) return;
            bool shooed = ShooPests();
            catalog.MakeStore("Small Posie Garden", (Vector2)king.transform.position + new Vector2(2.4f, -2.2f));
            Tell(shooed
                ? "Oh dear, those guinea pigs ate every flower! I've shooed them away. Here's a fresh garden: plant it by your capybara so it can eat and get back to work."
                : "Your capybara has no flowers left to eat! Here's a fresh garden: plant it by your capybara so it can get back to work.");
        }

        /// <summary>Flowers growing, or a garden waiting in a bubble or your trail.</summary>
        static bool AnyFlowers()
        {
            foreach (var n in FindObjectsByType<Harvestable>(FindObjectsSortMode.None))
                if (n.InWorld && n.Family == NodeFamily.Flower) return true;
            foreach (var b in FindObjectsByType<Bubble>(FindObjectsSortMode.None))
                if (!b.Taken && b.Contents != null && b.Contents.TryGetComponent<CatalogItem>(out var inside) && IsGarden(inside)) return true;
            foreach (var c in FindObjectsByType<CatalogItem>(FindObjectsSortMode.None))
                if (IsGarden(c) && c.TryGetComponent<Placeable>(out var p) && !p.Placed) return true;
            return false;

            static bool IsGarden(CatalogItem c) => StoreData.TryGet(c.StoreName, out var def) && def.Kind == StoreItemKind.Garden;
        }

        /// <summary>Puffs away every guinea pig and wild zombie capybara. True if there were any.</summary>
        static bool ShooPests()
        {
            var pests = new System.Collections.Generic.List<GameObject>();
            foreach (var d in FindObjectsByType<DeerAnimal>(FindObjectsSortMode.None)) pests.Add(d.gameObject);
            foreach (var m in FindObjectsByType<MonsterAnimal>(FindObjectsSortMode.None)) if (!m.Tame) pests.Add(m.gameObject);
            var fx = HarvestEffects.Instance;
            foreach (var p in pests)
            {
                if (fx != null) fx.Poof(p.transform.position, Color.white);
                Destroy(p);
            }
            return pests.Count > 0;
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
            if (land != null) land.OpenBridge("first_bridge");
            // The hand may be off the screen, so the line says where to go, not where to look.
            Tell("Wonderful! A bridge has risen to the next island. Walk across it to the other side.", Step.CrossToHeart);
            _stepTimer = 10f;
            Advance(Step.CrossToHeart);
        }

        void TickCrossToHeart()
        {
            var king = FindFirstObjectByType<PlayerAvatar>();
            var land = LandMap.Instance;
            if (king != null && land != null && land.IslandAt(king.transform.position) == 1)
            {
                Tell("Ah... this is the heart of the Capy Isles. The old hot spring is here somewhere.", Step.FindSpring);
                _stepTimer = 6f;
                Advance(Step.FindSpring);
                return;
            }
            if (pointer != null && land != null)
            {
                // Just past the far end, on the new island: a tap there (or on the arrow to it)
                // walks the whole way across, not to the middle of the bridge.
                var bridge = land.Bridge("first_bridge");
                int n = bridge != null ? bridge.circles.Count : 0;
                if (n >= 2)
                {
                    Vector2 end = LandMap.WorldToUnity(new Vector2(bridge.circles[n - 1].x, bridge.circles[n - 1].y));
                    Vector2 before = LandMap.WorldToUnity(new Vector2(bridge.circles[n - 2].x, bridge.circles[n - 2].y));
                    Vector2 across = end + (end - before).normalized * 3f;
                    pointer.PointAt(land.IslandAt(across) == 1 ? across : end);
                }
            }
        }

        bool _towerOpened;
        void OnDiaryClosed() => _towerOpened = true;

        void TickOpenTower()
        {
            if (_towerOpened)
            {
                TowerOfLove.DiaryClosed -= OnDiaryClosed;
                Tell("Whenever you're stuck, visit the Steam Tower. Bring the warmth back to the islands!");
                Advance(Step.Done);
                WelcomeNeighbours();
                Graduated?.Invoke();
                return;
            }
            // Just thawed: once she has said so, the capybara walks over and hops in by itself.
            if (_soakAt > 0f && Time.time >= _soakAt && !Attention.Busy)
            {
                var spring = HotSpring.Instance;
                var tower = TowerOfLove.Instance;
                if (tower != null && (spring == null || !spring.Frozen)) { _soakAt = -1f; tower.Tap(); }
                else if (Time.time > _soakAt + 10f) _soakAt = -1f;
            }
            _stepTimer -= Time.deltaTime;
            if (_stepTimer > 0f) return;
            _stepTimer = 20f;
            if (HotSpring.Bathing) return;
            Say(StoryText.Spring.Soak);
            PointAtSpring();
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
            HideZone();
            // What she was about to tell you to do is done: never say it late.
            if (dialogue != null) dialogue.DropInstructions();
        }

        void Say(string line)
        {
            if (Guidance.Off) return;   // the tutorial was ended: the story goes on, quietly
            // the latest instruction replaces any older one still waiting, so she never lags behind
            if (dialogue != null) dialogue.Instruct(line);
        }

        /// <summary>
        /// A line to understand rather than a step to do (why the capybara fell asleep, what the
        /// heart is). Queued, never replaced by the next instruction: she finishes it first —
        /// unless it is only true up to some step (<paramref name="until"/>) and you have already
        /// gone past it, when she drops it rather than say it late.
        /// </summary>
        void Tell(string line, Step? until = null)
        {
            if (Guidance.Off) return;
            if (until.HasValue)
            {
                var last = until.Value;
                Hints.While(line, () => _step <= last);
            }
            if (dialogue != null) dialogue.Say(line);
        }

        /// <summary>
        /// The opening is over: now the next island's first neighbours turn up — Miso in a bubble
        /// and the ghost who sells the next bridge. (Not earlier: the tower deserves the stage.)
        /// A game saved after they arrived already has them.
        /// </summary>
        static void WelcomeNeighbours()
        {
            var land = LandMap.Instance;
            var catalog = ItemCatalog.Instance;
            if (land == null || catalog == null) return;
            bool ghost = false, miso = false;
            foreach (var g in FindObjectsByType<BridgeGhost>(FindObjectsSortMode.None)) if (g.BridgeName == "Ginger Cove") ghost = true;
            foreach (var c in FindObjectsByType<CatalogItem>(FindObjectsSortMode.None))
                if (c.StoreName == "Coriander") miso = true;
            foreach (var b in FindObjectsByType<Bubble>(FindObjectsSortMode.None))
                if (!b.Taken && b.Contents != null && b.Contents.TryGetComponent<CatalogItem>(out var inside) && inside.StoreName == "Coriander") miso = true;
            if (!ghost) BridgeGhost.Spawn("Ginger Cove", land.RandomSpotOnIsland(1, 30f), catalog.ItemParent);
            if (!miso) catalog.MakeStore("Coriander", land.RandomSpotOnIsland(1, 30f));
        }

        // ---------------------------------------------------------------- the camera

        Step _glidedFor = Step.Done;

        /// <summary>
        /// When a step wants you somewhere you can't see — a bubble off screen, the tower — the
        /// camera glides over to show it, holds a moment and glides back. Once per step, and never
        /// over a movie or a window.
        /// </summary>
        void GlideToGoal()
        {
            if (_glidedFor == _step || pointer == null || Guidance.Off) return;
            var target = pointer.Target;
            if (!target.HasValue || Attention.Busy) return;
            _glidedFor = _step;
            if (!OnScreen(target.Value, 0f)) CameraRig.CameraFocus.Show(target.Value, 2.2f);
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
