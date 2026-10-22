using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The Mermaid's Shell: a fishing charm you toss into the sea rather than a buff you carry. It
    /// floats where it lands and sings for a minute, and its song draws a glowing ring on the water.
    /// Inside the ring the fish gather (a new one swims in after every catch), and they bite
    /// quicker for anyone whose bobber is in it — you, a friend visiting, the host whose island it
    /// is. Every catch inside counts towards the shell's tally, whoever made it; at five, a
    /// shimmering blue shadow comes up to listen, and that one is a fish you can only ever meet
    /// this way (HobbyData's charm-only fish). It stays a while after the song ends, until caught.
    ///
    /// Skipper Nori sells shells on the beach once your fishing is good enough, and gives you your
    /// first three. Up to three can sing on one island at once, one of them yours.
    ///
    /// On a live visit every game is told about every shell (Live, "charm" notes): where it was
    /// tossed and how long it has left, then each catch inside it. So every game draws the same
    /// ring and keeps the same tally, and when the tally reaches five each game raises its own
    /// shimmering shadow — fish are each player's own, as they always are (Fishing).
    /// </summary>
    public class MermaidCharms : MonoBehaviour
    {
        /// <summary>The shell in the bag.</summary>
        public const string Item = "lure:Mermaid";

        public const int Gold = 300;          // one shell, at Nori's stand
        public const int MinLevel = 4;        // fishing level that unlocks it
        public const int FirstFree = 3;       // Nori's welcome gift
        public const int MaxSinging = 3;      // on one island, everyone's together
        public const int ShimmerAfter = 5;    // catches inside before the shimmering shadow comes
        public const int FishInside = 4;      // shadows the song keeps in the ring
        public const float Lasts = 60f;       // seconds a shell sings
        public const float ShimmerLasts = 180f;
        public const float BiteBoost = 1.5f;  // how much sooner fish bite in the ring

        /// <summary>
        /// The ring's half-width and half-height: a circle on the sea, squashed to the island's
        /// slant like every other circle on the ground.
        /// </summary>
        public static readonly Vector2 Radii = new(5f, 3f);

        const string GiftKey = "CapyIsland.MermaidGift";

        /// <summary>One shell in the sea, yours or someone else's.</summary>
        public class Charm
        {
            public string Id;
            public bool Mine;
            public Vector2 At;
            public float EndsAt;          // when it goes quiet (Time.time)
            public int Tally;             // catches inside it, everyone's
            public float ShimmerEndsAt;   // set when the tally reaches five
            public bool ShimmerCaught;    // landed in this game (each game has its own)
            internal Singer View;

            public bool Singing => Time.time < EndsAt;
            public float Left => Mathf.Max(0f, EndsAt - Time.time);

            /// <summary>The shimmering shadow is about in this game: five catches, not landed yet, not swum off.</summary>
            public bool Shimmering => Tally >= ShimmerAfter && !ShimmerCaught && Time.time < ShimmerEndsAt;

            public bool Contains(Vector2 p)
            {
                var d = p - At;
                d.x /= Radii.x;
                d.y /= Radii.y;
                return d.sqrMagnitude <= 1f;
            }
        }

        /// <summary>A shell as it goes over the link, inside a Live note's data.</summary>
        [Serializable]
        class Wire
        {
            public string id, by;
            public float left, shimmer;
            public int tally;
        }

        public static MermaidCharms Instance { get; private set; }

        readonly List<Charm> _charms = new();
        float _nextGiftCheck;

        static readonly List<Charm> None = new();

        /// <summary>Every shell on this island that still matters: singing, or with its shimmering shadow about.</summary>
        public static IReadOnlyList<Charm> All => Instance != null ? Instance._charms : None;

        public static int Count => HobbyLog.Count(Item);

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------------------------------------------------------------- buying

        /// <summary>Why you can't buy one yet (for Nori's stand), or null.</summary>
        public static string Locked =>
            !HobbyLog.IsUnlocked(Hobby.Fishing) ? "You need a fishing rod first. Nori sells one right here."
            : HobbyLog.Level(Hobby.Fishing) < MinLevel ? StoryText.Charm.NeedLevel
            : null;

        public static bool Buy()
        {
            var store = ResourceStore.Instance;
            if (Locked != null || store == null || !store.TrySpend(0, Gold, 0)) return false;
            HobbyLog.AddToBag(Item, 1);
            Sfx.Play(SfxKind.Buy, 0.6f);
            return true;
        }

        /// <summary>Nori's stand on the sand, beside his bait.</summary>
        public static BaitStand MakeStand(Vector2 at, Transform parent) =>
            BaitStand.Create("Charm_Mermaid", StoryText.Charm.Name, StoryText.Charm.Blurb, new Vector3Int(0, Gold, 0),
                HobbyData.ItemArt(Item), Buy, at, parent, () => Locked);

        // ---------------------------------------------------------------- tossing one in

        /// <summary>Your shell that is singing now, or null.</summary>
        public static Charm Yours()
        {
            foreach (var c in All) if (c.Mine && c.Singing) return c;
            return null;
        }

        static int SingingCount()
        {
            int n = 0;
            foreach (var c in All) if (c.Singing) n++;
            return n;
        }

        /// <summary>The singing shell whose ring holds this point, or null.</summary>
        public static Charm SingingAt(Vector2 p)
        {
            foreach (var c in All) if (c.Singing && c.Contains(p)) return c;
            return null;
        }

        /// <summary>
        /// Why a shell can't be tossed right now (for the tackle panel's greyed Throw), or null
        /// when one can.
        /// </summary>
        public static string CantToss()
        {
            var fishing = Fishing.Instance;
            if (Instance == null || fishing == null) return StoryText.Charm.NoWater;
            if (Count < 1) return StoryText.Charm.NoneLeft;
            if (Yours() != null) return StoryText.Charm.StillSinging;
            if (SingingCount() >= MaxSinging) return StoryText.Charm.TooMany;
            if (!fishing.CharmSpot().HasValue) return StoryText.Charm.NoWater;
            return null;
        }

        /// <summary>
        /// Tosses a shell into the water in front of you (or by your bobber). Returns what went
        /// wrong, or null when it's in and singing. The capybara throws it: it leaves your paws
        /// and arcs out, and the ring comes up where it splashes down.
        /// </summary>
        public static string Toss()
        {
            var me = Instance;
            var fishing = Fishing.Instance;
            if (me == null || fishing == null) return null;
            string why = CantToss();
            if (why != null) return why;
            var spot = fishing.CharmSpot();
            if (!spot.HasValue) return StoryText.Charm.NoWater;
            if (!HobbyLog.Take(Item)) return StoryText.Charm.NoneLeft;

            var c = me.Add(Live.MyId + "-" + UnityEngine.Random.Range(1, int.MaxValue), spot.Value, Lasts, 0, 0f, mine: true);
            var from = fishing.ThrowTowards(c.At, out float release);
            c.View.TossFrom(from, release);
            Live.SendCharm("toss", c.At, JsonUtility.ToJson(new Wire { id = c.Id, by = PlayerProfile.Name, left = Lasts }));
            return null;
        }

        Charm Add(string id, Vector2 at, float left, int tally, float shimmerLeft, bool mine)
        {
            var c = new Charm
            {
                Id = id, Mine = mine, At = at, EndsAt = Time.time + left, Tally = tally,
                ShimmerEndsAt = shimmerLeft > 0f ? Time.time + shimmerLeft : 0f,
            };
            c.View = Singer.Make(c);
            _charms.Add(c);
            return c;
        }

        // ---------------------------------------------------------------- catches

        /// <summary>
        /// Something was landed from this point (Fishing): if it's in a singing shell's ring, the
        /// shell's tally goes up, here and in every other game on the island.
        /// </summary>
        public static void CaughtAt(Vector2 at)
        {
            var c = SingingAt(at);
            if (c == null) return;
            Count1(c);
            Live.SendCharm("catch", c.At, JsonUtility.ToJson(new Wire { id = c.Id }));
        }

        /// <summary>You landed the shimmering fish: it won't come again for this shell, in this game.</summary>
        public static void ShimmerLanded(Charm c)
        {
            if (c != null) c.ShimmerCaught = true;
        }

        static void Count1(Charm c)
        {
            c.Tally++;
            if (c.View != null) c.View.Counted();
            if (c.Tally != ShimmerAfter) return;
            c.ShimmerEndsAt = Time.time + ShimmerLasts;
            Sfx.PlayAt(SfxKind.Heart, c.At, 0.7f, 0.05f);
            if (Near(c)) Hints.Show(StoryText.Charm.Shimmer);
        }

        /// <summary>Close enough to care: your own shell, or one you are standing by.</summary>
        static bool Near(Charm c)
        {
            if (c.Mine) return true;
            var f = Fishing.Instance;
            return f != null && Vector2.Distance(f.KingAt, c.At) < 16f;
        }

        // ---------------------------------------------------------------- the link

        /// <summary>A shell note from another game on this island (Live): a toss, or a catch in one.</summary>
        public static void FromNet(string what, Vector2 at, string data)
        {
            var me = Instance;
            if (me == null || string.IsNullOrEmpty(data)) return;
            Wire w;
            try { w = JsonUtility.FromJson<Wire>(data); } catch (Exception) { return; }
            if (w == null || string.IsNullOrEmpty(w.id)) return;
            var known = me._charms.Find(x => x.Id == w.id);
            switch (what)
            {
                case "toss":
                    if (known != null)
                    {
                        // Told again (a newcomer's catch-up crossing a fresh toss): take the later news.
                        known.Tally = Mathf.Max(known.Tally, w.tally);
                        return;
                    }
                    if (w.left <= 0f && w.shimmer <= 0f) return;
                    var c = me.Add(w.id, at, w.left, w.tally, w.shimmer, mine: false);
                    Sfx.PlayAt(SfxKind.Plop, at, 0.8f, 0.1f);
                    if (w.left > Lasts - 5f && !string.IsNullOrEmpty(w.by) && HobbyLog.IsUnlocked(Hobby.Fishing))
                        Hints.Show(UI.Loc.F(StoryText.Charm.FriendTossed, w.by));
                    break;
                case "catch":
                    if (known != null) Count1(known);
                    break;
            }
        }

        /// <summary>Hosting: someone has just landed on your island; tell them about every shell still in the sea.</summary>
        public static void Greet(string id)
        {
            foreach (var c in All)
            {
                if (!c.Singing && !(c.Tally >= ShimmerAfter && Time.time < c.ShimmerEndsAt)) continue;
                var w = new Wire { id = c.Id, left = c.Left, tally = c.Tally, shimmer = c.ShimmerEndsAt > 0f ? Mathf.Max(0f, c.ShimmerEndsAt - Time.time) : 0f };
                Live.SendCharm("toss", c.At, JsonUtility.ToJson(w), id);
            }
        }

        // ---------------------------------------------------------------- every frame

        void Update()
        {
            // A shell that has gone quiet, with no shimmering fish left about it, is done.
            for (int i = _charms.Count - 1; i >= 0; i--)
            {
                var c = _charms[i];
                if (c.Singing || c.Shimmering) continue;
                if (c.View != null) c.View.Sink();
                _charms.RemoveAt(i);
            }
            if (Time.time >= _nextGiftCheck)
            {
                _nextGiftCheck = Time.time + 1f;
                MaybeGift();
            }
        }

        /// <summary>
        /// The first time your fishing reaches the shell's level (or the first time you play after
        /// it did), Skipper Nori calls over with three shells and how they work — once any card
        /// or chat on screen is out of the way, and never in the middle of a fight with a fish.
        /// </summary>
        static void MaybeGift()
        {
            if (PlayerPrefs.GetInt(GiftKey, 0) == 1) return;
            if (!HobbyLog.IsUnlocked(Hobby.Fishing) || HobbyLog.Level(Hobby.Fishing) < MinLevel) return;
            if (UI.CardPopup.IsShowing || UI.SpeechDialog.IsOpen || UI.UiScreen.AnyOpen || UI.Cutscene.Playing || Attention.TutorialRunning) return;
            if (Fishing.Instance != null && Fishing.Instance.Now == Fishing.State.Fight) return;
            PlayerPrefs.SetInt(GiftKey, 1);
            PlayerPrefs.Save();
            HobbyLog.AddToBag(Item, FirstFree);
            Sfx.Play(SfxKind.Chest, 0.6f);
            UI.SpeechDialog.Show(HobbyData.Art("Sailor"), SeaLibrary.Skipper, StoryText.Charm.Gift);
        }

        // ---------------------------------------------------------------- the shell and its ring

        /// <summary>
        /// What a shell looks like in the sea: the shell itself bobbing on the swell, a soft glow
        /// on the water and a ring round the edge of its reach, and rings of song spreading out
        /// from it. The glow and the ring fade as the song runs down, so you can see how long it
        /// has left; five little pearls under the shell fill in as the catches count.
        /// </summary>
        internal class Singer : MonoBehaviour
        {
            // On the water, under the beach (the sand covers the part of the ring that reaches
            // land); the shell floats with the bobber, over the water but under everything standing.
            const int FillOrder = -128, EdgeOrder = -122, ShellOrder = 265;
            const int InAirOrder = 29994;   // thrown: over the capybara and the shore, under a fishing line
            static readonly Color Sea = new(0.55f, 0.93f, 1f), Pearl = new(1f, 0.86f, 0.45f);

            Charm _charm;
            SpriteRenderer _fill, _edge, _shell;
            readonly List<SpriteRenderer> _pearls = new();
            Transform _body;
            float _nextSong, _phase, _shown;
            bool _sinking, _flying;

            public static Singer Make(Charm c)
            {
                var go = new GameObject("MermaidsShell");
                go.transform.position = c.At;
                var s = go.AddComponent<Singer>();
                s._charm = c;
                s.Build();
                return s;
            }

            void Build()
            {
                _phase = UnityEngine.Random.value * 6f;

                _fill = Part("Glow", UI.IconPainter.Glow, FillOrder);
                // The glow is brightest in the middle and has faded away just past the ring.
                Fit(_fill.transform, _fill.sprite, Radii * 2.2f);
                _edge = Part("Ring", UI.IconPainter.Ring, EdgeOrder);
                Fit(_edge.transform, _edge.sprite, Radii * 2f / 0.93f);   // the ring is drawn at 93% of its picture

                _body = new GameObject("Shell").transform;
                _body.SetParent(transform, false);
                _shell = _body.gameObject.AddComponent<SpriteRenderer>();
                _shell.sprite = HobbyData.Art("Charm_Mermaid");
                _shell.sortingOrder = ShellOrder;
                if (_shell.sprite != null) _body.localScale = Vector3.one * (0.85f / Mathf.Max(0.01f, Mathf.Max(_shell.sprite.bounds.size.x, _shell.sprite.bounds.size.y)));

                // The tally, as five little pearls in a smile under the shell.
                for (int i = 0; i < ShimmerAfter; i++)
                {
                    var p = Part("Pearl", UI.UiKit.Circle, ShellOrder + 1);
                    float t = i / (float)(ShimmerAfter - 1) - 0.5f;
                    p.transform.localPosition = new Vector3(t * 0.9f, -0.5f + t * t * 0.4f, 0f);
                    Fit(p.transform, p.sprite, Vector2.one * 0.13f);
                    _pearls.Add(p);
                }
                Counted();
                SetAlpha(0f);
            }

            SpriteRenderer Part(string name, Sprite sprite, int order)
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = order;
                return sr;
            }

            static void Fit(Transform t, Sprite s, Vector2 size)
            {
                if (s == null) return;
                var b = s.bounds.size;
                t.localScale = new Vector3(size.x / Mathf.Max(0.01f, b.x), size.y / Mathf.Max(0.01f, b.y), 1f);
            }

            /// <summary>
            /// Your own toss. While the capybara winds up (<paramref name="delay"/>) the shell is in
            /// its paws, out of sight; then it leaves them at <paramref name="from"/>, sails high
            /// over the shore turning over and over with a glittering trail, and comes down with a
            /// splash. The ring and its song wait for the splash.
            /// </summary>
            public void TossFrom(Vector2 from, float delay = 0f)
            {
                if (_body == null) return;
                _flying = true;
                Vector2 at = transform.position;
                var rest = _body.localScale;
                _body.position = from;
                _body.localScale = rest * 0.7f;
                _shell.enabled = false;
                _shell.sortingOrder = InAirOrder;
                float time = Mathf.Clamp(0.55f + Vector2.Distance(from, at) * 0.05f, 0.65f, 0.95f);
                float height = 1.8f + Vector2.Distance(from, at) * 0.12f;
                float spin = (from.x < at.x ? -1f : 1f) * 540f;   // tumbling forwards, whichever way it was thrown
                float trailAt = 0f;
                DOTween.Sequence()
                    .AppendInterval(delay)
                    .AppendCallback(() =>
                    {
                        if (_shell != null) _shell.enabled = true;
                        Sfx.PlayAt(SfxKind.Pop, from, 0.35f, 0.1f);
                    })
                    .Append(_body.DOJump(at, height, 1, time).SetEase(Ease.Linear))
                    .Join(_body.DOLocalRotate(new Vector3(0f, 0f, spin), time, RotateMode.FastBeyond360).SetEase(Ease.Linear))
                    .Join(_body.DOScale(rest * 1.2f, time * 0.45f).SetEase(Ease.OutQuad))   // up towards the camera…
                    .Insert(delay + time * 0.45f, _body.DOScale(rest, time * 0.55f).SetEase(Ease.InQuad))   // …and down
                    .SetLink(gameObject)
                    .OnUpdate(() =>
                    {
                        if (_body == null || _shell == null || !_shell.enabled || Time.time < trailAt) return;
                        trailAt = Time.time + 0.05f;
                        Trail(_body.position);
                    })
                    .OnComplete(() =>
                    {
                        _flying = false;
                        if (_body == null) return;
                        _body.localPosition = Vector3.zero;
                        _body.localRotation = Quaternion.identity;
                        _body.localScale = rest;
                        _shell.sortingOrder = ShellOrder;
                        Fishing.Splash(at, 0.55f);
                        Fishing.Ripple(at, 2f);
                        Splash();
                    });
            }

            /// <summary>A glint left behind the shell in the air, fading as it falls away.</summary>
            void Trail(Vector2 at)
            {
                var sr = Part("Trail", UI.IconPainter.Sparkle, InAirOrder - 1);
                if (sr.sprite == null) { Destroy(sr.gameObject); return; }
                var t = sr.transform;
                t.position = at + UnityEngine.Random.insideUnitCircle * 0.12f;
                Fit(t, sr.sprite, Vector2.one * UnityEngine.Random.Range(0.14f, 0.24f));
                sr.color = new Color(0.8f, 0.97f, 1f, 0.9f);
                t.DOScale(0f, 0.45f).SetEase(Ease.InQuad).SetLink(sr.gameObject);
                sr.DOFade(0f, 0.45f).SetLink(sr.gameObject).OnComplete(() => { if (sr != null) Destroy(sr.gameObject); });
            }

            void Start()
            {
                if (!_flying) Splash();   // someone else's: it's simply there, with a splash
            }

            void Splash()
            {
                _shown = 0f;   // the ring fades in from here
                Sfx.PlayAt(SfxKind.Plop, transform.position, 0.9f, 0.1f);
                Song(0.6f);
                Song(1f);
            }

            /// <summary>A catch counted: one more pearl lights up.</summary>
            public void Counted()
            {
                for (int i = 0; i < _pearls.Count; i++)
                {
                    bool lit = i < _charm.Tally;
                    var c = lit ? Pearl : new Color(0.75f, 0.85f, 0.9f);
                    _pearls[i].color = new Color(c.r, c.g, c.b, _pearls[i].color.a);   // SetAlpha keeps the fade
                    if (lit && i == _charm.Tally - 1)
                    {
                        _pearls[i].transform.DOComplete();
                        _pearls[i].transform.DOPunchScale(_pearls[i].transform.localScale * 0.8f, 0.4f, 6, 0.6f);
                    }
                }
            }

            /// <summary>The song is over: the shell sinks and the ring goes.</summary>
            public void Sink()
            {
                if (_sinking) return;
                _sinking = true;
                Sfx.PlayAt(SfxKind.Plop, transform.position, 0.5f, 0.1f);
                transform.DOScale(0f, 0.8f).SetEase(Ease.InBack).OnComplete(() => Destroy(gameObject));
            }

            /// <summary>A ring of song spreading out from the shell, to the edge of its reach.</summary>
            void Song(float strength)
            {
                var sr = Part("Song", UI.IconPainter.Ring, EdgeOrder);
                var t = sr.transform;
                Fit(t, sr.sprite, Radii * 0.3f);
                var end = t.localScale * (2f / 0.3f / 0.93f);
                sr.color = new Color(1f, 1f, 1f, 0.55f * strength);
                t.DOScale(end, 2.4f).SetEase(Ease.OutSine);
                sr.DOFade(0f, 2.4f).SetEase(Ease.InQuad).OnComplete(() => { if (sr != null) Destroy(sr.gameObject); });
            }

            void SetAlpha(float a)
            {
                if (_fill != null) _fill.color = new Color(Sea.r, Sea.g, Sea.b, 0.22f * a);
                if (_edge != null) _edge.color = new Color(0.85f, 1f, 1f, 0.8f * a);
                foreach (var p in _pearls) { var c = p.color; c.a = (_charm.Tally >= ShimmerAfter ? 0f : 0.95f) * a; p.color = c; }
            }

            void Update()
            {
                if (_sinking || _charm == null || _flying) return;
                // Fade in after the splash, and down with the time it has left: fullest when tossed,
                // a faint ring in its last seconds (and a soft blink in the last ten).
                _shown = Mathf.MoveTowards(_shown, 1f, Time.deltaTime * 2f);
                float life = _charm.Singing ? Mathf.Lerp(0.35f, 1f, _charm.Left / Lasts) : 0f;
                if (_charm.Singing && _charm.Left < 10f) life *= 0.75f + 0.25f * Mathf.Cos(Time.time * 6f);
                SetAlpha(Mathf.Clamp01(_shown) * life);

                // Bobbing on the swell, rocking a little.
                if (_body != null && _shown > 0f && _charm.Singing)
                {
                    _body.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2.2f + _phase) * 0.06f, 0f);
                    _body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.6f + _phase) * 6f);
                }
                if (!_charm.Singing && _shell != null && _shell.enabled)
                {
                    // Quiet now, but its shimmering fish is still about: the shell has sunk, the
                    // fish stays (Fishing keeps it until it is caught or swims off).
                    _shell.enabled = false;
                    Sfx.PlayAt(SfxKind.Plop, transform.position, 0.5f, 0.1f);
                }

                if (_charm.Singing && _shown > 0.5f && Time.time >= _nextSong)
                {
                    _nextSong = Time.time + 1.6f;
                    Song(Mathf.Lerp(0.4f, 1f, _charm.Left / Lasts));
                }
            }
        }
    }
}
