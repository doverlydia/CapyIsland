using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The island's wildlife, from the very first morning: songbirds hopping in the trees and
    /// pecking on the grass, gulls wheeling over the sea, herons on the shore, bumblebees working
    /// the flower gardens, butterflies drifting by, dragonflies darting about the bridges, fireflies
    /// at night. They are there for the life they bring; the hobbies only decide whether tapping one
    /// spots it for the journal (<see cref="WildBird"/>) or nets it (<see cref="WildBug"/>).
    ///
    /// How many are about follows the hour: birds are busiest at dawn and through the morning,
    /// quiet in the heat of the day, singing again at dusk, and only the owl is up at night; bees
    /// and butterflies keep to the warm middle of the day; fireflies come out after dark. Rain
    /// sends the bees home and the birds into cover. Everything is spawned around the view and goes
    /// once it is far out of it, so a phone only ever draws the few you can see.
    ///
    /// Common creatures are about for everyone; rare ones only turn up for players whose hobby
    /// level is high enough (HobbyData.RollBird / RollBug), so collecting still means something.
    /// </summary>
    public class Wildlife : MonoBehaviour
    {
        public static Wildlife Instance { get; private set; }

        /// <summary>Seen about the island whatever your level: the everyday birds and bugs.</summary>
        static readonly HashSet<string> Everyday = new() { "sparrow", "robin", "bluetit", "dove", "owl", "heron", "egret", "gull", "Bee", "Butterfly", "Dragonfly", "Firefly" };

        float _nextTick, _nextFlock, _nextChop;
        readonly HashSet<Object> _chopped = new();
        PlayerAvatar _king;

        void Awake()
        {
            Instance = this;
            _nextFlock = Time.time + Random.Range(25f, 60f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------------------------------------------------------------- the view

        /// <summary>What the camera shows, in world units, grown by <paramref name="margin"/> on every side.</summary>
        public static Rect View(float margin = 0f)
        {
            var cam = Camera.main;
            if (cam == null) return new Rect(-10f, -10f, 20f, 20f);
            float h = cam.orthographicSize, w = h * cam.aspect;
            Vector2 c = cam.transform.position;
            return new Rect(c.x - w - margin, c.y - h - margin, (w + margin) * 2f, (h + margin) * 2f);
        }

        /// <summary>A point just outside the view, on the side nearest <paramref name="toward"/> (or a random side), a little high.</summary>
        public static Vector2 OffScreen(Vector2 toward, bool nearest = true)
        {
            var v = View(2.5f);
            bool left = nearest ? toward.x < v.center.x : Random.value < 0.5f;
            float y = Mathf.Lerp(v.yMin, v.yMax, Random.Range(0.45f, 1f));
            return new Vector2(left ? v.xMin : v.xMax, y);
        }

        /// <summary>Far enough out of the view that it can go without anyone seeing.</summary>
        public static bool FarOut(Vector2 p) => !View(6f).Contains(p);

        /// <summary>
        /// On screen, or near enough that a bit of it shows (the margin is a little more than half
        /// the biggest bird). Nothing is ever taken away while this is true: a flyer flies on out
        /// past the edge first, and a crawler waits until you've looked away.
        /// </summary>
        public static bool Seen(Vector2 p) => View(1f).Contains(p);

        /// <summary>
        /// Where a creature may settle: the view, less the strip under the resource bar and goal
        /// card at the top (a bird landing there would never be seen) and a sliver at the bottom.
        /// </summary>
        public static Rect PerchView()
        {
            var v = View(0.5f);
            return new Rect(v.xMin, v.yMin + v.height * 0.04f, v.width, v.height * 0.78f);
        }

        /// <summary>Someone is already sitting here (two birds on one spot look like one odd bird).</summary>
        public static bool Taken(Vector2 p, float room = 0.9f)
        {
            foreach (var b in WildBird.All)
                if (b != null && !b.Passing && Vector2.Distance(b.Destination, p) < room) return true;
            foreach (var b in WildBug.All)
                if (b != null && Vector2.Distance(b.transform.position, p) < room * 0.6f) return true;
            return false;
        }

        // ---------------------------------------------------------------- how lively

        /// <summary>
        /// How busy the birds are at this hour, 0 to 1: a dawn chorus, a lively morning, a lull in
        /// the heat of the day, a last burst at dusk. (The owl has the night to itself.)
        /// </summary>
        public static float BirdActivity
        {
            get
            {
                float h = GameClock.Hour;
                float a = h < 5f ? 0.12f
                    : h < 7f ? 1f
                    : h < 10.5f ? 0.9f
                    : h < 14f ? 0.45f
                    : h < 17f ? 0.6f
                    : h < 19f ? 0.75f
                    : 0.12f;
                return Weather.IsRaining ? a * 0.3f : a;
            }
        }

        /// <summary>Bees and butterflies: the warm middle of the day, and never in the rain.</summary>
        public static float BugActivity
        {
            get
            {
                if (Weather.IsRaining) return 0f;
                float h = GameClock.Hour;
                return Mathf.Clamp01(Mathf.Min((h - 7.5f) / 2.5f, (18f - h) / 2.5f));
            }
        }

        /// <summary>Fireflies: from dusk, brightest in the evening, a few until the small hours.</summary>
        public static float FireflyActivity
        {
            get
            {
                if (Weather.IsRaining) return 0f;
                float h = GameClock.Hour;
                return h >= 19f && h < 23.5f ? 1f : h >= 23.5f || h < 3f ? 0.5f : h >= 18f ? (h - 18f) : 0f;
            }
        }

        // How many of each there are at most, at the busiest hour, around the view. Kept few: a bird
        // or two and the odd bee is calm; more crowded the island and never stopped chirping.
        const int MaxTreeBirds = 2, MaxWaders = 1, MaxGulls = 1, MaxBees = 2, MaxButterflies = 2, MaxDragonflies = 1, MaxFireflies = 5, MaxCrawlers = 1;

        // ---------------------------------------------------------------- the beat

        void Update()
        {
            if (Time.time >= _nextChop) { _nextChop = Time.time + 0.5f; NoteChopping(); }
            if (Time.time < _nextTick) return;
            _nextTick = Time.time + Random.Range(4f, 8f);   // they trickle in, never all at once
            if (Camera.main == null) return;
            if (_king == null) _king = FindFirstObjectByType<PlayerAvatar>();

            var view = PerchView();
            int treeBirds = 0, waders = 0, gulls = 0, bees = 0, butterflies = 0, darters = 0, fireflies = 0, crawlers = 0;
            foreach (var b in WildBird.All)
            {
                if (b == null || b.Passing || !View(6f).Contains(b.transform.position)) continue;
                if (b.Kind.Id == "gull") gulls++;
                else if (b.Kind.Wader) waders++;
                else treeBirds++;
            }
            foreach (var b in WildBug.All)
            {
                if (b == null || !View(6f).Contains(b.transform.position)) continue;
                switch (b.Mode)
                {
                    case WildBug.Moves.Bee: bees++; break;
                    case WildBug.Moves.Flutter: butterflies++; break;
                    case WildBug.Moves.Dart: darters++; break;
                    case WildBug.Moves.Glow: fireflies++; break;
                    default: crawlers++; break;
                }
            }

            // One new arrival per beat: whichever kind is furthest below where the hour wants it.
            float birds = BirdActivity, bugs = BugActivity, glow = FireflyActivity;
            var wants = new List<(float gap, System.Action spawn)>
            {
                (MaxTreeBirds * birds - treeBirds, () => SpawnTreeBird(view)),
                (MaxWaders * birds - waders, () => SpawnWader(view)),
                (MaxGulls * (Weather.IsRaining ? 0.5f : GameClock.IsNight ? 0f : 0.8f) - gulls, () => SpawnGull(view)),
                (MaxBees * bugs - bees, () => SpawnBug(view, WildBug.Moves.Bee)),
                (MaxButterflies * bugs - butterflies, () => SpawnBug(view, WildBug.Moves.Flutter)),
                (MaxDragonflies * bugs - darters, () => SpawnBug(view, WildBug.Moves.Dart)),
                (MaxFireflies * glow - fireflies, () => SpawnBug(view, WildBug.Moves.Glow)),
                (MaxCrawlers * (Weather.IsRaining ? 1f : 0.6f) - crawlers, () => SpawnBug(view, WildBug.Moves.Crawl)),
            };
            wants.Sort((a, b) => b.gap.CompareTo(a.gap));
            foreach (var (gap, spawn) in wants)
            {
                if (gap < 0.5f) break;
                spawn();
                break;
            }

            Contest();
            Flock(view);
        }

        // ---------------------------------------------------------------- birds

        void SpawnTreeBird(Rect view)
        {
            var kind = PickBird(wader: false);
            if (kind == null) return;
            WildBird.Perch? perch = null;
            // Doves and sparrows often feed on the ground; everyone else (and the owl) wants a tree.
            bool ground = (kind.Id is "sparrow" or "dove" or "pheasant") && Random.value < 0.5f;
            if (kind.Id == "hummingbird") perch = FlowerPerch(view);
            if (perch == null && !ground) perch = TreePerch(view);
            if (perch == null && kind.Id != "owl" && kind.Id != "hummingbird") perch = GroundPerch(view);
            if (perch == null) return;
            WildBird.Arrive(kind, perch.Value);
        }

        void SpawnWader(Rect view)
        {
            var kind = PickBird(wader: true);
            if (kind == null) return;
            var perch = ShorePerch(view);
            if (perch == null) return;
            WildBird.Arrive(kind, perch.Value);
        }

        void SpawnGull(Rect view)
        {
            var gull = HobbyData.BirdById("gull");
            if (gull == null || !GameClock.Allows(gull.Times)) return;
            var sea = SeaPoint(view);
            if (sea == null) return;
            WildBird.Soar(gull, sea.Value, ShorePerch(view));
        }

        /// <summary>
        /// Mostly the everyday birds of this hour; now and then whatever the player's birdwatching
        /// level can turn up (the rarer ones only come to those who look for them).
        /// </summary>
        static BirdKind PickBird(bool wader)
        {
            if (Random.value < 0.3f)
            {
                var rolled = HobbyData.RollBird(HobbyLog.Level(Hobby.Birding), wader);
                if (rolled != null) return rolled;
            }
            var pool = new List<BirdKind>();
            foreach (var b in HobbyData.Birds)
                if (Everyday.Contains(b.Id) && b.Wader == wader && !b.Sea && GameClock.Allows(b.Times) && (!b.RainOnly || Weather.IsRaining))
                    pool.Add(b);
            return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
        }

        /// <summary>
        /// Now and then a little flock crosses the sky: three to five of one everyday bird, in a
        /// loose line, from one side of the view to the other. Mostly in the morning.
        /// </summary>
        void Flock(Rect view)
        {
            if (Time.time < _nextFlock) return;
            float morning = GameClock.Hour is >= 5f and < 11f ? 1f : 0.35f;
            _nextFlock = Time.time + Random.Range(150f, 300f) / morning;
            if (GameClock.IsNight || Weather.IsRaining) return;
            var kind = HobbyData.BirdById(Random.value < 0.6f ? "sparrow" : Random.value < 0.5f ? "dove" : "gull");
            if (kind == null) return;
            bool rightward = Random.value < 0.5f;
            var v = View(3f);
            float y = Mathf.Lerp(view.yMin, view.yMax, Random.Range(0.55f, 0.9f));
            int n = Random.Range(3, 6);
            for (int i = 0; i < n; i++)
            {
                var offset = new Vector2(-i * 0.9f, (i % 2 == 0 ? 1f : -1f) * 0.35f * ((i + 1) / 2));
                if (!rightward) offset.x = -offset.x;
                var from = new Vector2(rightward ? v.xMin : v.xMax, y) + offset;
                var to = new Vector2(rightward ? v.xMax + 4f : v.xMin - 4f, y + Random.Range(-1.5f, 1.5f)) + offset;
                WildBird.Pass(kind, from, to, i * 0.12f);
            }
        }

        // ---------------------------------------------------------------- bugs

        void SpawnBug(Rect view, WildBug.Moves mode)
        {
            var kind = PickBug(mode);
            if (kind == null) return;
            switch (mode)
            {
                case WildBug.Moves.Bee:
                case WildBug.Moves.Flutter:
                {
                    var flower = FlowerPerch(view);
                    if (flower == null) return;   // nothing in bloom in view: nobody to visit
                    WildBug.Arrive(kind, mode, flower.Value.At, flower.Value.Order);
                    break;
                }
                case WildBug.Moves.Dart:
                {
                    var spot = WaterEdge(view);
                    if (spot == null) return;
                    WildBug.Arrive(kind, mode, spot.Value, 0);
                    break;
                }
                case WildBug.Moves.Glow:
                {
                    var spot = GroundPerch(view);
                    if (spot == null) return;
                    WildBug.Arrive(kind, mode, spot.Value.At + Vector2.up * Random.Range(0.4f, 1.2f), 0);
                    break;
                }
                default:
                {
                    var spot = CrawlerSpot(kind);
                    if (spot == null) return;
                    WildBug.Appear(kind, mode, spot.Value.At, spot.Value.Order);
                    break;
                }
            }
        }

        /// <summary>
        /// Where a snail or ladybug is: on its leaf, bark or grass just beyond the edge of the view, never
        /// in it. It can't fly in, so it is simply there to be found when you look that way.
        /// </summary>
        static WildBird.Perch? CrawlerSpot(BugKind kind)
        {
            var around = View(4f);
            var seen = View(0.3f);
            for (int i = 0; i < 6; i++)
            {
                WildBird.Perch? spot = kind.Home == BugHome.Flower ? FlowerPerch(around) : kind.Home == BugHome.Tree ? TreePerch(around, trunk: true) : null;
                spot ??= GroundPerch(around);
                if (spot != null && !seen.Contains(spot.Value.At)) return spot;
            }
            return null;
        }

        /// <summary>The bug for a way of moving: the everyday one for the hour, or what your bug-catching level can turn up.</summary>
        static BugKind PickBug(WildBug.Moves mode)
        {
            var pool = new List<BugKind>();
            int level = HobbyLog.Level(Hobby.Bugs);
            foreach (var b in HobbyData.Bugs)
            {
                if (WildBug.ModeOf(b) != mode || !GameClock.Allows(b.Times) || (b.RainOnly && !Weather.IsRaining)) continue;
                if (!Everyday.Contains(b.Id) && b.MinLevel > level) continue;
                pool.Add(b);
            }
            return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
        }

        // ---------------------------------------------------------------- contests

        /// <summary>
        /// A birdwatching or bug-catching contest fills its island: extra birds and bugs around
        /// you, of the kinds your level can find, on that island only.
        /// </summary>
        void Contest()
        {
            if (_king == null) return;
            Vector2 at = _king.transform.position;
            if (Gathering.Boosting(Hobby.Birding) && WildBird.All.Count < 10 && Random.value < 0.6f)
            {
                var kind = HobbyData.RollBird(HobbyLog.Level(Hobby.Birding), Random.value < 0.25f);
                var perch = kind == null ? null : kind.Wader ? ShorePerch(PerchView()) : TreePerch(PerchView());
                if (kind != null && perch != null && Gathering.OnEventIsland(perch.Value.At)) WildBird.Arrive(kind, perch.Value);
            }
            if (Gathering.Boosting(Hobby.Bugs) && WildBug.All.Count < 14 && Random.value < 0.7f)
            {
                var kind = HobbyData.RollBug(HobbyLog.Level(Hobby.Bugs), (BugHome)Random.Range(0, 3));
                if (kind == null) return;
                var mode = WildBug.ModeOf(kind);
                var spot = mode == WildBug.Moves.Crawl ? CrawlerSpot(kind)
                    : kind.Home == BugHome.Flower ? FlowerPerch(PerchView()) : kind.Home == BugHome.Tree ? TreePerch(PerchView(), trunk: true) : GroundPerch(PerchView());
                if (spot == null || !Gathering.OnEventIsland(spot.Value.At)) return;
                if (mode == WildBug.Moves.Crawl) WildBug.Appear(kind, mode, spot.Value.At, spot.Value.Order);
                else WildBug.Arrive(kind, mode, spot.Value.At, spot.Value.Order);
            }
        }

        // ---------------------------------------------------------------- places

        /// <summary>
        /// A tree in view — a forest's, a fruit tree, or one of the island's own (anything that
        /// sways in the wind and isn't a flower): high in its crown for a bird, or on its trunk for
        /// a beetle.
        /// </summary>
        public static WildBird.Perch? TreePerch(Rect view, bool trunk = false)
        {
            var trees = new List<Component>();
            foreach (var s in FindObjectsByType<Sway>(FindObjectsSortMode.None))
            {
                if (!view.Contains(s.transform.position)) continue;
                if (s.TryGetComponent<Harvestable>(out var node) && (node.Family != NodeFamily.Tree || !node.InWorld)) continue;
                if (s.TryGetComponent<FruitTree>(out var fruit) && !fruit.InWorld) continue;
                if (s.TryGetComponent<Placeable>(out var p) && !p.Placed) continue;   // still in your trail
                // The part a capybara chops (Wildlife.BeingChopped), where there is one.
                trees.Add(node != null ? node : fruit != null ? fruit : s);
            }
            if (trees.Count == 0) return null;
            var tree = trees[Random.Range(0, trees.Count)];
            var sr = tree.GetComponentInChildren<SpriteRenderer>();
            var b = sr != null ? sr.bounds : new Bounds(tree.transform.position, Vector3.one);
            var at = trunk
                ? new Vector2(b.center.x + Random.Range(-0.15f, 0.15f), b.min.y + b.size.y * 0.3f)
                : new Vector2(b.center.x + Random.Range(-0.3f, 0.3f) * b.size.x, b.max.y - b.size.y * Random.Range(0.22f, 0.4f));
            if (Taken(at, 0.6f)) return null;   // someone is already up there; try again on the next beat
            return new WildBird.Perch { At = at, Order = (sr != null ? sr.sortingOrder : 3000) + 5, Node = tree, Place = WildBird.Places.Tree };
        }

        /// <summary>A flower garden in view, just above its blooms.</summary>
        public static WildBird.Perch? FlowerPerch(Rect view)
        {
            var flowers = new List<Harvestable>();
            foreach (var n in FindObjectsByType<Harvestable>(FindObjectsSortMode.None))
                if (n.Family == NodeFamily.Flower && n.InWorld && view.Contains(n.transform.position)) flowers.Add(n);
            if (flowers.Count == 0) return null;
            var f = flowers[Random.Range(0, flowers.Count)];
            var sr = f.GetComponentInChildren<SpriteRenderer>();
            var b = sr != null ? sr.bounds : new Bounds(f.transform.position, Vector3.one * 0.5f);
            return new WildBird.Perch { At = (Vector2)b.center + Vector2.up * (b.extents.y * 0.6f), Order = (sr != null ? sr.sortingOrder : 3000) + 5, Node = f, Place = WildBird.Places.Flower };
        }

        /// <summary>Open grass in view, clear of anything built.</summary>
        public static WildBird.Perch? GroundPerch(Rect view)
        {
            for (int i = 0; i < 24; i++)
            {
                var p = new Vector2(Random.Range(view.xMin, view.xMax), Random.Range(view.yMin, view.yMax));
                if (!BunniSpace.PointOnLand(p) || !BunniSpace.CircleOnMap(p, 25f) || Taken(p)) continue;
                return new WildBird.Perch { At = p, Order = YSorter.PropSortBase + Mathf.RoundToInt(-p.y * 100f) + 5, Place = WildBird.Places.Ground };
            }
            return null;
        }

        /// <summary>Land at the water's edge in view, for the waders and a gull's rest.</summary>
        public static WildBird.Perch? ShorePerch(Rect view)
        {
            for (int i = 0; i < 40; i++)
            {
                var p = new Vector2(Random.Range(view.xMin, view.xMax), Random.Range(view.yMin, view.yMax));
                if (!BunniSpace.PointOnLand(p) || !NearWater(p, 1.1f) || Taken(p, 1.4f)) continue;
                return new WildBird.Perch { At = p, Order = YSorter.PropSortBase + Mathf.RoundToInt(-p.y * 100f) + 5, Place = WildBird.Places.Shore };
            }
            return null;
        }

        /// <summary>Open water in view, a little way from the land, for a gull to wheel over.</summary>
        public static Vector2? SeaPoint(Rect view)
        {
            for (int i = 0; i < 30; i++)
            {
                var p = new Vector2(Random.Range(view.xMin, view.xMax), Random.Range(view.yMin, view.yMax));
                if (!BunniSpace.PointOnLand(p)) return p;
            }
            return null;
        }

        /// <summary>
        /// Where a dragonfly patrols: over the water beside a bridge in view, or failing that
        /// along the shore. (The islands have no ponds; the bridges and the shallows are the wet bits.)
        /// </summary>
        public static Vector2? WaterEdge(Rect view)
        {
            var land = LandMap.Instance;
            if (land != null)
            {
                var spots = new List<Vector2>();
                foreach (var piece in land.Pieces)
                {
                    if (!piece.isBridge || !piece.open) continue;
                    foreach (var c in piece.circles)
                    {
                        var at = LandMap.WorldToUnity(new Vector2(c.x, c.y));
                        if (!view.Contains(at)) continue;
                        // Just off the side of the bridge, over the water.
                        var side = at + Random.insideUnitCircle.normalized * Random.Range(1.2f, 2f);
                        if (!BunniSpace.PointOnLand(side)) spots.Add(side);
                    }
                }
                if (spots.Count > 0) return spots[Random.Range(0, spots.Count)];
            }
            var shore = ShorePerch(view);
            return shore?.At;
        }

        static bool NearWater(Vector2 p, float r)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                if (!BunniSpace.PointOnLand(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r)) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- startles

        /// <summary>The trees and hills being worked right now: birds in them take off at the first chop.</summary>
        void NoteChopping()
        {
            _chopped.Clear();
            foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
            {
                if (w.Activity != WorkerActivity.Harvesting) continue;
                if (w.CurrentTarget != null) _chopped.Add(w.CurrentTarget);
                if (w.CurrentTree != null) _chopped.Add(w.CurrentTree);
            }
        }

        /// <summary>Is someone chopping at this tree right now?</summary>
        public static bool BeingChopped(Object node) => node != null && Instance != null && Instance._chopped.Contains(node);

        // ---------------------------------------------------------------- the hobbies' word

        /// <summary>
        /// Tapped a bird or a bug the player can't log yet: it flits off, and once (per reason) a
        /// little note says what would let them — the gear, then more practice for the rare ones.
        ///
        /// Until the gear is actually on a shelf (the hobby opened with a card and its shop built),
        /// it just flits off with no word at all: a new player who doesn't know what hobbies are yet
        /// doesn't need telling about one. The note is kept for when it means something.
        /// </summary>
        public static void NotYet(Hobby hobby, int minLevel)
        {
            string key, line;
            bool birds = hobby == Hobby.Birding;
            if (!HobbyLog.IsUnlocked(hobby))
            {
                if (!GearOnSale(hobby)) return;   // nothing to buy yet: not a word (and the note isn't spent)
                key = "gear";
                line = birds ? "Get binoculars at the Silver Store, and you can spot birds like this one for your journal."
                             : "Get a bug net at the Bronze Store, and you can catch bugs like this one for your journal.";
            }
            else if (HobbyLog.Level(hobby) < minLevel)
            {
                key = "level";
                line = birds ? "This one is too shy for you yet. Spot more birds to get better at it!"
                             : "This one is too quick for you yet. Catch more bugs to get better at it!";
            }
            else return;
            string pref = "CapyIsland.WildNote." + hobby + "." + key;
            if (PlayerPrefs.GetInt(pref, 0) == 1) return;
            PlayerPrefs.SetInt(pref, 1);
            PlayerPrefs.Save();
            Hints.Answer(line);
        }

        /// <summary>
        /// The net or the binoculars can be bought now: the hobby is open (ShopPacing puts the gear
        /// out) and the shop that sells it stands. Not while the first card's gift of it still waits
        /// in its bubble: there's nothing to buy, only a bubble to pop.
        /// </summary>
        static bool GearOnSale(Hobby hobby)
        {
            bool birds = hobby == Hobby.Birding;
            string gear = birds ? "Binoculars" : "Bug Net";
            if (HobbyLog.FreeGear == gear) return false;
            return ShopPacing.Ready(gear) == true && HobbyCards.Built(birds ? "Silver Store" : "Bronze Store");
        }

        /// <summary>Can the player log this one now (the hobby taken up, and good enough at it)?</summary>
        public static bool CanLog(Hobby hobby, int minLevel) => HobbyLog.IsUnlocked(hobby) && HobbyLog.Level(hobby) >= minLevel;
    }
}
