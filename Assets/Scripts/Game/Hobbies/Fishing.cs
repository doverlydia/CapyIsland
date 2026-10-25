using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Fishing, played the way Heartopia plays it. Fish shadows swim in the shallows around whichever
    /// shore you are near — the bigger the shadow, the bigger the fish.
    ///
    /// 1. <b>Cast.</b> Starting to fish casts straight away, just in front of the nearest shadow
    ///    (or out to sea if none is near). After a catch, a tap anywhere casts again.
    /// 2. <b>Wait.</b> The fish comes over, nibbles, then bites with a big splash. It is hooked.
    /// 3. <b>Reel.</b> Hold to reel the fish in; let go to give it slack. The line shows the strain:
    ///    green is perfect, yellow is getting tight, red means let go now or it snaps. Every so
    ///    often the fish fights back hard, so reeling is a rhythm — hold, ease off, hold again.
    ///    Give it slack too long and it swims off.
    ///
    /// Bait (bought from Skipper Nori) goes on the hook as you cast: worms bring bites sooner,
    /// shrimp keep the line from straining so fast, a golden lure does both.
    ///
    /// Catches go in the fish journal and raise your fishing level, which brings rarer fish and a
    /// slightly kinder line.
    /// </summary>
    public class Fishing : MonoBehaviour
    {
        public enum State { Idle, Waiting, Nibbling, Fight }

        public static Fishing Instance { get; private set; }

        /// <summary>Something worth showing the player: a catch, an escape, a hint.</summary>
        public static event Action<string> Said;

        /// <summary>Raised the moment a fish bites and the fight begins.</summary>
        public static event Action Bitten;

        const int ShadowCount = 5;
        const float ShadowRange = 10f, DespawnRange = 18f;
        internal const int ShadowOrder = -125;   // over the shallows, under the sand

        // The tension zones, shared with the button so the two always agree.
        public const float YellowAt = 0.5f, RedAt = 0.8f;
        public static readonly Color Green = new(0.36f, 0.82f, 0.36f), Yellow = new(1f, 0.82f, 0.2f), Red = new(0.93f, 0.27f, 0.22f);

        /// <summary>The colour of the line at a given strain.</summary>
        public static Color TensionColour(float t) => t >= RedAt ? Red : t >= YellowAt ? Yellow : Green;

        class Shadow
        {
            public GameObject Go;
            public SpriteRenderer Sr;
            public FishKind Kind;
            public Vector2 Target;
            public float Speed, NextTurn;
            public bool Fleeing;
            public MermaidCharms.Charm Charm;   // the singing shell it came to, which keeps it in the ring
            public bool Shimmer;                // the shell's shimmering shadow: a charm-only fish
            public SpriteRenderer Halo;         // the shimmer's glow in the water
            public float NextSparkle, Born;
        }

        readonly List<Shadow> _shadows = new();
        float _nextSpawn;

        public State Now { get; private set; }

        /// <summary>The line's strain during a fight, 0 slack … 1 snapping.</summary>
        public float Tension { get; private set; }

        /// <summary>How far the fish has been reeled in, 0 just hooked … 1 landed.</summary>
        public float Progress => _needed <= 0f ? 0f : Mathf.Clamp01(1f - _remaining / _needed);

        /// <summary>True while the fish is fighting back hard.</summary>
        public bool Surging { get; private set; }

        /// <summary>The button is held down.</summary>
        public bool Holding { get; private set; }

        /// <summary>Fishing mode: the whole screen is the rod (see FishingButton). Off, you walk about as usual.</summary>
        public bool Mode { get; set; }

        /// <summary>How close a maxed-out line is to snapping, 0 … 1 (it holds for a moment first).</summary>
        public float Overload => Mathf.Clamp01(_overload / SnapAfter);

        const float SnapAfter = 1.1f;   // seconds a line can stay at full strain before it snaps
        const float SlackEscape = 4.5f; // seconds of slack before it swims off
        float _overload;

        Shadow _hooked;
        GameObject _bobber;
        Transform _bobberBody;
        LineRenderer _line;
        Vector2 _castFrom, _bobberAt, _aimDir;
        Vector2 _waterAt;   // during a fight: where the fish is in the water (before it's lifted out)
        Vector2 _hookAt;    // where it bit
        bool _lifted;       // the hooked fish is out of the water, up at the rod
        float _stateUntil, _giveUpAt;
        int _nibblesLeft;
        bool _warnedEarly;

        // The fight.
        BaitKind _bait;   // on the hook for this cast, or null

        /// <summary>The bait on the hook while a line is out (null when fishing plain or between casts).</summary>
        public BaitKind BaitOnHook => Now == State.Idle ? null : _bait;
        // A running fishing contest makes the fish along its shore keener too (Gathering).
        // So does a singing Mermaid's Shell, for anyone whose bobber is in its ring (MermaidCharms).
        float BiteSpeed => (_bait != null ? _bait.BiteSpeed : 1f) * (Gathering.Boosting(Hobby.Fishing) ? 1.6f : 1f)
                           * (InCharm ? MermaidCharms.BiteBoost : 1f);

        float _needed, _remaining, _strength, _phaseUntil, _nextSplash, _slackFor;   // line still to reel in, in reel-seconds

        PlayerAvatar _king;
        static Material _lineMaterial;

        // The rod in the capybara's paws while fishing (part of its body, so it turns with it).
        SpriteRenderer _rod, _kingBody;

        void Awake()
        {
            Instance = this;
            // The shells you and your friends toss into the sea live alongside the fish.
            if (GetComponent<MermaidCharms>() == null) gameObject.AddComponent<MermaidCharms>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var s in _shadows) if (s.Go != null) Destroy(s.Go);
            foreach (var go in _thrown) if (go != null) Destroy(go);   // their tweens go with them (SetLink)
            _thrown.Clear();
            if (_pose != null && _pose.IsActive()) _pose.Kill();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            Said = null;
            Bitten = null;
        }

        // ---------------------------------------------------------------- can I fish here?

        /// <summary>True when the king stands close enough to open water to cast.</summary>
        public bool AtShore => _king != null && NearestWater(_king.transform.position, 2.6f).HasValue;

        /// <summary>The nearest water within reach of a spot, if any (where a fishing contest can be held).</summary>
        public static Vector2? WaterNear(Vector2 from, float within) => NearestWater(from, within);

        static Vector2? NearestWater(Vector2 from, float within)
        {
            Vector2? best = null;
            float bestD = float.MaxValue;
            for (int ring = 1; ring <= 4; ring++)
            {
                float r = within * ring / 4f;
                for (int a = 0; a < 16; a++)
                {
                    float ang = a * Mathf.PI / 8f;
                    var p = from + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.6f) * r;
                    if (BunniSpace.PointOnLand(p)) continue;
                    float d = (p - from).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = p; }
                }
                if (best.HasValue) return best;
            }
            return null;
        }

        /// <summary>Open water: this point and a little ring around it are all sea.</summary>
        internal static bool OpenWater(Vector2 p, float margin)
        {
            if (BunniSpace.PointOnLand(p)) return false;
            for (int a = 0; a < 6; a++)
            {
                float ang = a * Mathf.PI / 3f;
                if (BunniSpace.PointOnLand(p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.6f) * margin)) return false;
            }
            return true;
        }

        /// <summary>Shallows: open water with land not far off, where fish like to be seen.</summary>
        static bool NearShore(Vector2 p, float within)
        {
            for (int a = 0; a < 8; a++)
            {
                float ang = a * Mathf.PI / 4f;
                if (BunniSpace.PointOnLand(p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.6f) * within)) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- the sea

        void Update()
        {
            if (_king == null) _king = FindFirstObjectByType<PlayerAvatar>();
            if (_king == null) return;
            Vector2 you = _king.transform.position;

            if (Time.time >= _nextSpawn)
            {
                // A fishing contest's shore teems: twice the fish, turning up quicker, around you
                // wherever you fish along it.
                bool contest = Gathering.Boosting(Hobby.Fishing);
                _nextSpawn = Time.time + (contest ? 0.6f : 1.5f);
                for (int i = _shadows.Count - 1; i >= 0; i--)
                    if (_shadows[i].Go == null || (!_shadows[i].Fleeing && Vector2.Distance(_shadows[i].Go.transform.position, you) > DespawnRange))
                        Remove(_shadows[i]);
                if (_shadows.Count < (contest ? ShadowCount * 2 : ShadowCount))
                    TrySpawn(you, ShadowRange);
            }

            foreach (var s in _shadows) Swim(s);
            // The rod can be out between casts (a bait was just put on it); out of fishing mode it goes away.
            if (!Mode && Now == State.Idle) PutRodAway();
            Charms(you);
            Tick(you);
        }

        void TrySpawn(Vector2 around, float range)
        {
            for (int i = 0; i < 20; i++)
            {
                var p = around + UnityEngine.Random.insideUnitCircle * range;
                if (!OpenWater(p, 0.6f) || !NearShore(p, 5f)) continue;
                var kind = HobbyData.RollFish(HobbyLog.Level(Hobby.Fishing));
                var go = new GameObject("FishShadow_" + kind.Id);
                go.transform.position = p;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = UI.IconPainter.FishShadow;
                sr.color = new Color(0.08f, 0.14f, 0.22f, 0f);
                sr.sortingOrder = ShadowOrder;
                float scale = 0.45f + kind.Shadow * 0.22f;
                go.transform.localScale = new Vector3(scale, scale * 0.8f, 1f);
                sr.DOFade(0.42f, 1.2f);
                var s = new Shadow { Go = go, Sr = sr, Kind = kind, Target = p, Speed = UnityEngine.Random.Range(0.4f, 0.9f) };
                _shadows.Add(s);
                return;
            }
        }

        void Swim(Shadow s)
        {
            if (s.Go == null) return;
            if (s == _hooked && Now == State.Fight) return;   // the fight moves it
            Vector2 at = s.Go.transform.position;
            if (s != _hooked && !s.Fleeing && Time.time >= s.NextTurn)
            {
                s.NextTurn = Time.time + UnityEngine.Random.Range(2f, 5f);
                // A fish listening to a shell's song stays in its ring (and one that strayed out
                // heads back towards the shell).
                var ring = s.Charm != null && (s.Charm.Singing || s.Shimmer) ? s.Charm : null;
                for (int i = 0; i < 8; i++)
                {
                    var t = at + UnityEngine.Random.insideUnitCircle * 2.5f;
                    if (ring != null && !ring.Contains(t)) t = Vector2.Lerp(t, ring.At, 0.6f);
                    if (OpenWater(t, 0.4f)) { s.Target = t; break; }
                }
            }
            float speed = s.Fleeing ? 4f : s == _hooked && Now == State.Waiting ? 0.7f : s.Shimmer ? s.Speed * 0.6f : s.Speed;
            var next = Vector2.MoveTowards(at, s.Target, speed * Time.deltaTime);
            if (!s.Fleeing && BunniSpace.PointOnLand(next)) { s.NextTurn = 0f; return; }
            s.Go.transform.position = next;
            float dx = s.Target.x - at.x;
            if (Mathf.Abs(dx) > 0.01f) s.Sr.flipX = dx > 0f;   // the shadow's nose points left
            // A slow wiggle as it swims.
            s.Go.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 5f + s.Speed * 10f) * (s.Fleeing ? 12f : 5f));
        }

        void Remove(Shadow s)
        {
            if (s.Go != null) Destroy(s.Go);
            _shadows.Remove(s);
            if (_hooked == s) _hooked = null;
        }

        // ---------------------------------------------------------------- mermaid's shells

        MermaidCharms.Charm _castCharm;   // the singing shell whose ring the bobber went into, or null
        float _nextCharmLook;

        /// <summary>The bobber is in a singing Mermaid's Shell's ring: bites come quicker.</summary>
        public bool InCharm => _castCharm != null && _castCharm.Singing;

        /// <summary>Where the player's capybara stands.</summary>
        public Vector2 KingAt => _king != null ? (Vector2)_king.transform.position : Vector2.zero;

        /// <summary>
        /// Where a Mermaid's Shell lands if you toss it now: right by your bobber while you wait for
        /// a bite (so you're fishing in its ring at once), otherwise a little way out to sea in front
        /// of you. Null away from the water.
        /// </summary>
        public Vector2? CharmSpot()
        {
            if (_king == null) return null;
            if (Now == State.Waiting || Now == State.Nibbling) return _bobberAt;
            Vector2 you = _king.transform.position;
            var water = NearestWater(you, 2.6f);
            if (!water.HasValue) return null;
            var p = CastPoint(3.5f, (water.Value - you).normalized);
            return OpenWater(p, BobberMargin) ? p : null;
        }

        /// <summary>The fish to cast at: a shell's shimmering one if it's in reach, else the nearest.</summary>
        Shadow AimShadow(Vector2 from, float within)
        {
            foreach (var s in _shadows)
                if (s.Shimmer && !s.Fleeing && s.Go != null && Vector2.Distance(from, s.Go.transform.position) < within) return s;
            return NearestShadow(from, within);
        }

        /// <summary>
        /// The shells' part of the sea, every frame: each singing shell near you keeps a few fish in
        /// its ring (another swims in after every catch), and once its tally reaches five, its
        /// shimmering shadow comes up, glinting blue. When the song ends its fish are free to wander
        /// off; a shimmering one stays until caught, or until it gives up and swims away.
        /// </summary>
        void Charms(Vector2 you)
        {
            foreach (var s in _shadows) if (s.Shimmer) Glint(s);
            if (Time.time < _nextCharmLook) return;
            _nextCharmLook = Time.time + 0.5f;

            // A shimmering fish nobody caught in time swims off (a hooked one finishes its fight first).
            foreach (var s in _shadows)
                if (s.Shimmer && !s.Fleeing && s != _hooked && (s.Charm == null || !s.Charm.Shimmering)) FadeAway(s);

            foreach (var c in MermaidCharms.All)
            {
                int inside = 0;
                Shadow shimmer = null;
                foreach (var s in _shadows)
                {
                    if (s.Charm != c || s.Go == null) continue;
                    if (s.Shimmer) { if (!s.Fleeing) shimmer = s; continue; }
                    if (!c.Singing) s.Charm = null;   // the song is over: off it swims, an ordinary fish again
                    else if (!s.Fleeing) inside++;
                }

                if (Vector2.Distance(you, c.At) > DespawnRange - 3f) continue;   // too far off to see
                if (c.Singing && inside < MermaidCharms.FishInside) SpawnInCharm(c, false);
                if (c.Shimmering && shimmer == null) SpawnInCharm(c, true);
            }
        }

        void SpawnInCharm(MermaidCharms.Charm c, bool shimmer)
        {
            for (int i = 0; i < 20; i++)
            {
                var r = UnityEngine.Random.insideUnitCircle * 0.85f;
                var p = c.At + new Vector2(r.x * MermaidCharms.Radii.x, r.y * MermaidCharms.Radii.y);
                if (!OpenWater(p, 0.6f)) continue;
                var kind = shimmer ? HobbyData.RollCharmFish() : HobbyData.RollFish(HobbyLog.Level(Hobby.Fishing));
                var go = new GameObject((shimmer ? "ShimmerShadow_" : "FishShadow_") + kind.Id);
                go.transform.position = p;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = UI.IconPainter.FishShadow;
                sr.sortingOrder = ShadowOrder;
                float scale = 0.45f + kind.Shadow * 0.22f;
                go.transform.localScale = new Vector3(scale, scale * 0.8f, 1f);
                var s = new Shadow { Go = go, Sr = sr, Kind = kind, Target = p, Speed = UnityEngine.Random.Range(0.4f, 0.9f), Charm = c, Shimmer = shimmer, Born = Time.time };
                if (shimmer)
                {
                    // A soft blue light in the water around it, so it's spotted from across the ring.
                    var halo = new GameObject("Halo").AddComponent<SpriteRenderer>();
                    halo.transform.SetParent(go.transform, false);
                    halo.transform.localScale = new Vector3(2.4f, 2.4f, 1f) / Mathf.Max(0.01f, UI.IconPainter.Glow.bounds.size.x);
                    halo.sprite = UI.IconPainter.Glow;
                    halo.sortingOrder = ShadowOrder - 1;
                    s.Halo = halo;
                    sr.color = new Color(0.45f, 0.8f, 1f, 0f);
                    halo.color = new Color(0.5f, 0.85f, 1f, 0f);
                    Ripple(p, 1.6f);
                    Sfx.PlayAt(SfxKind.Plop, p, 0.6f, 0.1f);
                }
                else
                {
                    sr.color = new Color(0.08f, 0.14f, 0.22f, 0f);
                    sr.DOFade(0.42f, 1.2f);
                }
                _shadows.Add(s);
                return;
            }
        }

        /// <summary>The shimmering shadow flashes blue and white, and throws off little sparkles.</summary>
        void Glint(Shadow s)
        {
            if (s.Go == null || s.Fleeing || (s == _hooked && _lifted)) return;
            float age = Mathf.Clamp01((Time.time - s.Born) / 1.2f);   // rising into view
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f + s.Speed * 7f);
            var c = Color.Lerp(new Color(0.3f, 0.7f, 1f), new Color(0.85f, 0.97f, 1f), pulse);
            c.a = Mathf.Lerp(0.55f, 0.9f, pulse) * age;
            s.Sr.color = c;
            if (s.Halo != null) s.Halo.color = new Color(0.5f, 0.85f, 1f, (0.25f + 0.2f * pulse) * age);
            if (Time.time < s.NextSparkle) return;
            s.NextSparkle = Time.time + UnityEngine.Random.Range(0.18f, 0.4f);
            Sparkle((Vector2)s.Go.transform.position + UnityEngine.Random.insideUnitCircle * new Vector2(0.6f, 0.35f));
        }

        static void Sparkle(Vector2 at)
        {
            var art = UI.IconPainter.Sparkle;
            if (art == null) return;
            var go = new GameObject("Glint");
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art;
            sr.color = new Color(0.8f, 0.95f, 1f, 0.95f);
            sr.sortingOrder = ShadowOrder + 20;
            float size = UnityEngine.Random.Range(0.14f, 0.26f) / Mathf.Max(0.01f, art.bounds.size.x);
            go.transform.localScale = Vector3.zero;
            go.transform.DOScale(size, 0.25f).SetEase(Ease.OutBack);
            go.transform.DOMoveY(at.y + 0.35f, 0.8f).SetEase(Ease.OutSine);
            sr.DOFade(0f, 0.5f).SetDelay(0.3f).OnComplete(() => Destroy(go));
        }

        void FadeAway(Shadow s)
        {
            if (s == null || s.Go == null) return;
            s.Fleeing = true;
            if (s.Halo != null) s.Halo.DOFade(0f, 1.5f);
            s.Sr.DOFade(0f, 1.5f).OnComplete(() => Remove(s));
        }

        // ---------------------------------------------------------------- the button

        /// <summary>Finger down on the fishing button.</summary>
        public void Press()
        {
            if (_king == null) return;
            Holding = true;
            switch (Now)
            {
                case State.Idle: CastNow(); break;
                case State.Waiting:
                case State.Nibbling:
                    if (!_warnedEarly) { _warnedEarly = true; Say("Wait for the big splash — then hold to reel."); }
                    break;
            }
        }

        /// <summary>Puts the rod away: whatever was on the line gets away, and no harm done.</summary>
        public void StopFishing()
        {
            Holding = false;
            if (Now == State.Fight) Flee(_hooked);
            if (Now != State.Idle) EndLine();
        }

        /// <summary>Finger up.</summary>
        public void Release() => Holding = false;

        // ---------------------------------------------------------------- casting

        // ---------------------------------------------------------------- the rod

        /// <summary>Turns to face the water and takes out the rod.</summary>
        void HoldRod(Vector2 towards)
        {
            var body = FaceTowards(towards);
            if (body == null) return;
            _kingBody = body.GetComponent<SpriteRenderer>();

            if (_rod == null)
            {
                var art = HobbyData.Art("Rod");
                if (art == null) return;
                var go = new GameObject("FishingRod");
                go.transform.SetParent(body, false);
                _rod = go.AddComponent<SpriteRenderer>();
                _rod.sprite = art;
                _rod.flipX = true;   // tip towards the head, out over the water
                go.transform.localScale = Vector3.one * (1.5f / Mathf.Max(0.01f, art.bounds.size.x));
                go.transform.localPosition = new Vector3(-0.55f, 0.85f, 0f);
            }
            _rod.gameObject.SetActive(true);
            _rod.transform.localRotation = Quaternion.identity;
            _rod.transform.DOKill();
        }

        void PutRodAway()
        {
            if (_rod != null && _rod.gameObject.activeSelf) _rod.gameObject.SetActive(false);
        }

        /// <summary>
        /// Turns the capybara to look along <paramref name="towards"/>. Its head is on the left of
        /// its picture, so facing right is a flip. Returns its body (null without one).
        /// </summary>
        Transform FaceTowards(Vector2 towards)
        {
            var body = _king != null ? _king.transform.Find("Sprite") : null;
            if (body == null) return null;
            var scale = body.localScale;
            scale.x = Mathf.Abs(scale.x) * (towards.x > 0f ? -1f : 1f);
            body.localScale = scale;
            return body;
        }

        /// <summary>Which way the capybara is looking: +1 to the right, -1 to the left.</summary>
        float Facing()
        {
            var body = _king != null ? _king.transform.Find("Sprite") : null;
            return body != null && body.localScale.x < 0f ? 1f : -1f;
        }

        /// <summary>Where the line leaves the rod.</summary>
        Vector2 RodTip()
        {
            if (_rod == null || !_rod.gameObject.activeInHierarchy) return (Vector2)_king.transform.position + new Vector2(0.55f, 1.25f);
            var e = _rod.sprite.bounds.extents;
            return _rod.transform.TransformPoint(new Vector3(-e.x * 0.95f, e.y * 0.95f, 0f));
        }

        void LateUpdate()
        {
            if (_rod == null || !_rod.gameObject.activeInHierarchy) return;
            if (_kingBody != null) _rod.sortingOrder = _kingBody.sortingOrder + 6;
            // It bends towards the fish as the line pulls tight.
            float bend = Now == State.Fight ? Tension * 22f + (Surging ? Mathf.Sin(Time.time * 30f) * 4f : 0f) : 0f;
            // A bait just put on: the tip springs down and quivers back.
            float since = Time.time - _twitchAt;
            if (since < TwitchFor) bend += Mathf.Sin(since * 38f) * 10f * (1f - since / TwitchFor);
            _rod.transform.localRotation = Quaternion.Euler(0f, 0f, bend);
        }

        // ---------------------------------------------------------------- tackle, put on by hand

        const float TwitchFor = 0.6f;
        float _twitchAt = -10f;
        readonly List<GameObject> _thrown = new();   // bait and the like in the air, tidied away with the fishing
        Sequence _pose;

        /// <summary>Where the capybara's front paws are, for what it throws.</summary>
        public Vector2 PawAt => _king == null ? Vector2.zero : (Vector2)_king.transform.position + new Vector2(0.42f * Facing(), 0.72f);

        /// <summary>
        /// A bait chosen in the tackle panel goes on by hand, where you can see it. With the bobber
        /// out and nothing on the hook yet, the capybara lobs it out to the float and it is on this
        /// line from now: fish bite sooner straight away. Otherwise it is flicked up to the end of
        /// the rod (taking the rod out if it was put away) and goes on as you next cast, the way
        /// bait always has, one used up per cast. Returns true when it went on the line that's out.
        /// </summary>
        public bool PutOnBait(BaitKind b)
        {
            Baits.Select(b);
            if (_king == null || b == null) return false;
            var art = HobbyData.ItemArt(b.Item);
            if (Now == State.Waiting && _bait == null && HobbyLog.Take(b.Item))
            {
                _bait = b;
                _stateUntil = Mathf.Min(_stateUntil, Time.time + 2f / BiteSpeed);   // a curious fish sooner
                Fly(art, PawAt, () => _bobberBody != null ? (Vector2)_bobberBody.position : _bobberAt, 1.3f, 0.6f, 0.5f, () =>
                {
                    if (Now == State.Idle) return;
                    Sfx.PlayAt(SfxKind.Plop, _bobberAt, 0.6f, 0.1f);
                    Ripple(_bobberAt, 1f);
                    if (Now == State.Waiting && _bobberBody != null)
                    {
                        // The float bobs under with the weight of it.
                        _bobberBody.DOKill();
                        _bobberBody.localPosition = Vector3.zero;
                        _bobberBody.DOLocalMoveY(-0.08f, 0.12f).SetLoops(2, LoopType.Yoyo);
                    }
                    _twitchAt = Time.time;
                });
                return true;
            }
            if (Now == State.Idle)
            {
                var water = NearestWater(_king.transform.position, 2.6f);
                HoldRod(water.HasValue ? water.Value - KingAt : Vector2.right * Facing());
            }
            Fly(art, PawAt, () => _king != null ? RodTip() : PawAt, 0.9f, 0.45f, 0.45f, () =>
            {
                if (_king == null) return;
                Vector2 tip = RodTip();
                for (int i = 0; i < 3; i++) Twinkle(tip + UnityEngine.Random.insideUnitCircle * 0.2f);
                Sfx.Play(SfxKind.Pop, 0.35f);
                _twitchAt = Time.time;
            });
            return false;
        }

        /// <summary>
        /// The capybara throws something out to a spot on the water (a Mermaid's Shell): it turns
        /// to face it, unless it's busy with a line, leans back with its weight on its heels, then
        /// flings its paws forward and settles. Returns where the thing leaves its paws, and in
        /// <paramref name="release"/> how long until it does, for the flight to start there and then.
        /// </summary>
        public Vector2 ThrowTowards(Vector2 spot, out float release)
        {
            release = 0f;
            if (_king == null) return spot;
            var body = Now == State.Idle ? FaceTowards(spot - KingAt) : _king.transform.Find("Sprite");
            if (body == null) return PawAt;
            if (_pose != null && _pose.IsActive()) _pose.Kill(true);   // finishing it puts the body back
            float dir = Facing(), rest = body.localScale.y;
            const float windUp = 0.22f;
            _pose = DOTween.Sequence()
                .Append(body.DOLocalRotate(new Vector3(0f, 0f, 13f * dir), windUp).SetEase(Ease.OutQuad))    // lean back…
                .Join(body.DOScaleY(rest * 0.93f, windUp))
                .Append(body.DOLocalRotate(new Vector3(0f, 0f, -11f * dir), 0.09f).SetEase(Ease.InQuad))   // …and fling
                .Join(body.DOScaleY(rest * 1.07f, 0.09f))
                .Append(body.DOLocalRotate(Vector3.zero, 0.4f).SetEase(Ease.OutBack))
                .Join(body.DOScaleY(rest, 0.4f).SetEase(Ease.OutBack))
                .SetLink(body.gameObject);
            release = windUp + 0.05f;
            return PawAt + new Vector2(0.15f * dir, 0.25f);
        }

        /// <summary>
        /// A small thing thrown by hand: its picture flies in an arc to a point that may be moving
        /// (the end of a rod, a bobbing float), turning over as it goes, then
        /// <paramref name="landed"/> runs and it is gone. Over everything but the line while it flies.
        /// </summary>
        void Fly(Sprite art, Vector2 from, Func<Vector2> to, float height, float time, float size, Action landed)
        {
            if (art == null) { landed?.Invoke(); return; }
            var go = new GameObject("Thrown");
            go.transform.position = from;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art;
            sr.sortingOrder = 29995;
            float unit = size / Mathf.Max(0.01f, Mathf.Max(art.bounds.size.x, art.bounds.size.y));
            go.transform.localScale = Vector3.one * unit * 0.6f;
            _thrown.Add(go);
            float spin = (UnityEngine.Random.value < 0.5f ? 1f : -1f) * 300f;
            DOVirtual.Float(0f, 1f, time, t =>
            {
                if (go == null) return;
                go.transform.position = Vector2.Lerp(from, to(), t) + Vector2.up * (height * 4f * t * (1f - t));
                go.transform.localRotation = Quaternion.Euler(0f, 0f, spin * t);
                // Up towards you and away again: bigger at the top of the arc.
                go.transform.localScale = Vector3.one * unit * (t < 0.4f ? Mathf.Lerp(0.6f, 1.1f, t / 0.4f) : Mathf.Lerp(1.1f, 0.7f, (t - 0.4f) / 0.6f));
            }).SetEase(Ease.Linear).SetLink(go).OnComplete(() =>
            {
                _thrown.Remove(go);
                if (go != null) Destroy(go);
                landed?.Invoke();
            });
        }

        /// <summary>A little star that pops out and fades, up in the air (by the rod's end).</summary>
        static void Twinkle(Vector2 at)
        {
            var art = UI.IconPainter.Sparkle;
            if (art == null) return;
            var go = new GameObject("Twinkle");
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art;
            sr.color = new Color(1f, 0.95f, 0.7f, 1f);
            sr.sortingOrder = 29996;
            float size = UnityEngine.Random.Range(0.18f, 0.3f) / Mathf.Max(0.01f, art.bounds.size.x);
            go.transform.localScale = Vector3.zero;
            go.transform.DOScale(size, 0.2f).SetEase(Ease.OutBack).SetLink(go);
            go.transform.DOLocalRotate(new Vector3(0f, 0f, 90f), 0.6f).SetLink(go);
            sr.DOFade(0f, 0.35f).SetDelay(0.25f).SetLink(go).OnComplete(() => Destroy(go));
        }

        /// <summary>
        /// Casts at once: towards the nearest fish if there is one in reach, otherwise straight out
        /// to sea. Starting to fish calls this, and so does a tap while the line is in.
        /// </summary>
        public void CastNow()
        {
            if (_king == null || Now != State.Idle) return;
            Vector2 you = _king.transform.position;
            var water = NearestWater(you, 2.6f);
            if (!water.HasValue) { Holding = false; Say("Walk right up to the water's edge to fish."); return; }
            var target = AimShadow(you, 7f);
            _aimDir = ((target != null ? (Vector2)target.Go.transform.position : water.Value) - you).normalized;
            Cast();
        }

        /// <summary>
        /// How far the bobber stays from the land's edge. The drawn beach reaches a little past the
        /// land outline, so "not land" alone can still be sand on screen.
        /// </summary>
        const float BobberMargin = 0.7f;

        /// <summary>The landing point a distance out along the aim, always in open water, clear of the beach.</summary>
        Vector2 CastPoint(float distance) => CastPoint(distance, _aimDir);

        Vector2 CastPoint(float distance, Vector2 aim)
        {
            Vector2 you = _king.transform.position;
            var p = you + aim * distance;
            if (OpenWater(p, BobberMargin)) return p;
            // Short of the water (or on the beach): further out along the aim until it is open water…
            for (float d = distance; d < 9f; d += 0.25f)
                if (OpenWater(you + aim * d, BobberMargin)) return you + aim * d;
            // …else the nearest open water all round.
            for (float r = 1f; r <= 7f; r += 0.5f)
                for (int a = 0; a < 16; a++)
                {
                    float ang = a * Mathf.PI / 8f;
                    var q = you + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.6f) * r;
                    if (OpenWater(q, BobberMargin)) return q;
                }
            return NearestWater(you, 2.6f) ?? p;
        }

        Shadow NearestShadow(Vector2 from, float within)
        {
            Shadow best = null;
            foreach (var s in _shadows)
            {
                if (s.Go == null || s.Fleeing) continue;
                float d = Vector2.Distance(from, s.Go.transform.position);
                if (d < within) { within = d; best = s; }
            }
            return best;
        }

        void Cast()
        {
            Vector2 you = _king.transform.position;

            // Just in front of the nearest fish (a shell's shimmering one first), or a comfortable way out to sea.
            Vector2 spot;
            var fish = AimShadow(you, 6.5f);
            if (fish != null)
            {
                Vector2 at = fish.Go.transform.position;
                spot = at + (you - at).normalized * 0.9f;
                if (!OpenWater(spot, BobberMargin)) spot = at;
            }
            else spot = CastPoint(3.2f);

            _castFrom = you;
            _bobberAt = spot;
            HoldRod(spot - you);
            MakeBobber(spot);
            Live.SendHobby(null, "cast", spot, null, 0);   // friends here see your line go out (BotHobby)
            Sfx.Play(SfxKind.Plop, 0.7f);
            Now = State.Waiting;
            _castCharm = MermaidCharms.SingingAt(spot);   // before BiteSpeed: a shell's ring makes them keener
            _bait = Baits.UseOne();
            _warnedEarly = false;
            _giveUpAt = Time.time + 14f;
            _stateUntil = Time.time + 2f / BiteSpeed;

            // The fish you cast at comes over for a look (or whichever is nearest the bobber).
            _hooked = fish != null && Vector2.Distance(fish.Go.transform.position, spot) < 3.5f ? fish : NearestShadow(spot, 3.5f);
            if (_hooked != null) _hooked.Target = spot + (Vector2)(_hooked.Go.transform.position - (Vector3)spot).normalized * 0.35f;
        }

        // ---------------------------------------------------------------- waiting for a bite

        void Tick(Vector2 you)
        {
            if (Now == State.Idle) return;

            DrawLine();

            // Walking off reels the line in.
            if (Vector2.Distance(you, _castFrom) > 0.8f)
            {
                if (Now == State.Fight) { Escape("You walked off and the fish got away."); return; }
                EndLine();
                return;
            }

            switch (Now)
            {
                case State.Waiting:
                    if (_hooked == null || _hooked.Go == null)
                    {
                        // Now and then a fish a little further off notices the bobber and swims over.
                        if (Time.time >= _stateUntil)
                        {
                            _stateUntil = Time.time + 2f / BiteSpeed;
                            var curious = UnityEngine.Random.value < 0.5f * BiteSpeed ? NearestShadow(_bobberAt, 6f * Mathf.Sqrt(BiteSpeed)) : null;
                            if (curious != null)
                            {
                                _hooked = curious;
                                _hooked.Target = _bobberAt + (Vector2)(_hooked.Go.transform.position - (Vector3)_bobberAt).normalized * 0.35f;
                                break;
                            }
                        }
                        if (Time.time > _giveUpAt) { Say("Nothing's biting here. Try casting just in front of a fish shadow."); EndLine(); }
                        break;
                    }
                    if (Vector2.Distance(_hooked.Go.transform.position, _bobberAt) < 0.5f)
                    {
                        Now = State.Nibbling;
                        // Bait it can't resist: fewer, quicker nibbles before the bite.
                        _nibblesLeft = BiteSpeed > 1f ? UnityEngine.Random.Range(0, 2) : UnityEngine.Random.Range(1, 4);
                        _stateUntil = Time.time + UnityEngine.Random.Range(0.7f, 1.4f) / BiteSpeed;
                    }
                    break;

                case State.Nibbling:
                    if (Time.time < _stateUntil) break;
                    if (_nibblesLeft-- > 0)
                    {
                        Nibble();
                        _stateUntil = Time.time + UnityEngine.Random.Range(0.8f, 1.6f) / BiteSpeed;
                    }
                    else Hook();
                    break;

                case State.Fight:
                    Fight(you);
                    break;
            }
        }

        void Nibble()
        {
            if (_bobberBody == null) return;
            _bobberBody.DOKill();
            _bobberBody.localPosition = Vector3.zero;
            _bobberBody.DOLocalMoveY(-0.1f, 0.1f).SetLoops(2, LoopType.Yoyo);
            Sfx.Play(SfxKind.Plop, 0.35f);
            Ripple(_bobberAt, 0.9f);
        }

        // ---------------------------------------------------------------- the fight

        /// <summary>The big splash: the fish is on, and the reeling starts.</summary>
        void Hook()
        {
            Now = State.Fight;
            var kind = _hooked.Kind;
            // Bigger and rarer fish pull harder; a higher fishing level makes the line a little kinder.
            float rod = 1f - Mathf.Min(0.25f, HobbyLog.Level(Hobby.Fishing) * 0.03f);
            _strength = (0.8f + kind.Stars * 0.12f + kind.Shadow * 0.06f) * rod;
            _waterAt = _hookAt = _bobberAt;
            _lifted = false;
            // How much reeling it takes depends on the fish, not on how far out it bit: a common fish
            // is a few holds, a big rare one a proper tussle.
            _needed = _remaining = 4.5f + kind.Shadow * 0.7f + kind.Stars * 0.6f;
            Tension = 0.15f;
            Surging = false;
            _slackFor = 0f;
            _overload = 0f;
            _phaseUntil = Time.time + UnityEngine.Random.Range(1.2f, 1.8f);   // an early pull, to teach the rhythm

            // Now and then it isn't a fish at all. Junk doesn't fight: a short, steady reel.
            _junk = HobbyLog.Fish.Count > 0 && !_hooked.Shimmer && UnityEngine.Random.value < JunkChance ? Junk[UnityEngine.Random.Range(0, Junk.Length)] : default;
            if (_junk.id != null)
            {
                _strength = 0.35f * rod;
                _needed = _remaining = 3.5f;
                _phaseUntil = float.MaxValue;   // never pulls
            }

            if (_bobberBody != null)
            {
                _bobberBody.DOKill();
                _bobberBody.localPosition = new Vector3(0f, -0.25f, 0f);
                _bobberBody.localScale = new Vector3(0.26f, 0.16f, 1f);
            }
            Live.SendHobby(null, "bite", _bobberAt, kind.Id, 1);
            Sfx.Play(SfxKind.Pop, 0.9f);
            Sfx.PlayAt(SfxKind.Plop, _bobberAt, 0.9f, 0.1f);
            Splash(_bobberAt);
            Ripple(_bobberAt, 1.8f);
            Ripple(_bobberAt, 1.1f);
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
            Bitten?.Invoke();
            if (HobbyLog.Fish.Count == 0)
                Say("It's hooked! Hold to reel. If the line turns red, let go until it calms down.");
        }

        float CalmFor() => UnityEngine.Random.Range(1.3f, 2.8f) / (1f + _hooked.Kind.Stars * 0.08f);
        float SurgeFor() => UnityEngine.Random.Range(0.7f, 1.4f) * (1f + _hooked.Kind.Stars * 0.08f);

        void Fight(Vector2 you)
        {
            float dt = Time.deltaTime;

            // The fish alternates between swimming steadily and fighting back hard.
            if (Time.time >= _phaseUntil)
            {
                Surging = !Surging;
                _phaseUntil = Time.time + (Surging ? SurgeFor() : CalmFor());
                if (Surging) Sfx.PlayAt(SfxKind.Plop, _bobberAt, 0.6f, 0.15f);
            }

            if (Holding)
            {
                Tension += dt * (Surging ? 1f : 0.28f) * _strength * (_bait != null ? _bait.Gentle : 1f);
                _remaining -= dt * (Surging ? 0.3f : 0.85f);
                _slackFor = 0f;
            }
            else
            {
                Tension -= dt * 1.1f;
                _remaining += dt * (Surging ? 0.5f * _strength : 0.12f);
                _slackFor += dt;
            }
            Tension = Mathf.Clamp01(Tension);

            // At full strain the line holds for a moment (time to let go) before it snaps.
            if (Tension >= 1f && Holding) _overload += dt;
            else _overload = Mathf.Max(0f, _overload - dt * 2f);
            if (_overload >= SnapAfter) { Escape("Snap! The line broke. Let go when it turns red."); return; }
            if (_remaining > _needed + 1.5f || _slackFor > SlackEscape) { Escape("It swam off. Keep reeling between its pulls."); return; }
            if (_remaining <= 0f) { Catch(_hooked); return; }

            // The fish comes in straight along the line, from where it bit to the end of the rod, as
            // far as you've reeled: the line shortens with every bit of reeling, and the fish pulling
            // away takes it back out. Over water it's a shadow swimming; once the way in leaves the
            // water (and for the last stretch, whatever's below), it's lifted out, dangling.
            float ratio = Mathf.Clamp(_remaining / _needed, 0f, 1.3f);
            var end = RodTip() + Vector2.down * 0.5f;
            var along = Vector2.LerpUnclamped(end, _hookAt, ratio);
            if (ratio > 1f && BunniSpace.PointOnLand(along)) along = _hookAt;   // pulled out past where it bit, but not onto land
            bool up = ratio < LiftShare || BunniSpace.PointOnLand(along);

            // Thrashing side to side while it fights, in the water.
            var dir = (_hookAt - end).sqrMagnitude > 0.0001f ? (_hookAt - end).normalized : (_hookAt - you).normalized;
            var side = new Vector2(-dir.y, dir.x);
            float thrash = up ? 0f : Surging ? Mathf.Sin(Time.time * 14f) * 0.35f : Mathf.Sin(Time.time * 3f) * 0.08f;
            var p = along + side * thrash;
            if (!up && BunniSpace.PointOnLand(p)) p = along;   // a thrash never flips it up onto the sand
            if (!up) _waterAt = p;
            _bobberAt = p;
            if (_bobber != null) _bobber.transform.position = p;
            if (_hooked != null && _hooked.Go != null)
            {
                ShowLifted(_hooked, up);
                float wiggle = Mathf.Sin(Time.time * (Surging ? 22f : 8f)) * (Surging ? 18f : 6f);
                if (_lifted)
                {
                    // Out of the water: hanging nose-up from the hook, wriggling.
                    _hooked.Go.transform.position = p + Vector2.down * 0.32f;
                    _hooked.Sr.flipX = false;
                    // (Junk just dangles, the right way up, swinging a little.)
                    _hooked.Go.transform.localRotation = Quaternion.Euler(0f, 0f, _junk.id != null ? wiggle * 0.5f : -90f + wiggle);
                }
                else
                {
                    _hooked.Go.transform.position = p + dir * 0.35f;
                    _hooked.Sr.flipX = dir.x < 0f;   // its nose points away from you, pulling
                    _hooked.Go.transform.localRotation = Quaternion.Euler(0f, 0f, wiggle);
                }
            }
            if (Surging && !_lifted && Time.time >= _nextSplash)
            {
                _nextSplash = Time.time + 0.28f;
                Ripple(p, 0.8f);
            }
        }

        /// <summary>The last stretch of the line, by the rod, where the fish always hangs in the air.</summary>
        const float LiftShare = 0.12f;

        /// <summary>
        /// Out of the water a hooked fish is itself, not a shadow: its picture, in front of
        /// everything, sized to it. Back in (it pulled away), a shadow again.
        /// </summary>
        void ShowLifted(Shadow s, bool up)
        {
            if (up == _lifted || s == null || s.Sr == null) return;
            var picture = _junk.id != null ? HobbyData.Art(_junk.id) : HobbyData.FishPicture(s.Kind);
            if (picture == null) return;
            _lifted = up;
            s.Sr.DOKill();
            if (up)
            {
                s.Sr.sprite = picture;
                s.Sr.color = Color.white;
                s.Sr.sortingOrder = 29999;   // just under the line
                if (s.Halo != null) s.Halo.enabled = false;
                float width = 0.55f + s.Kind.Shadow * 0.12f;
                s.Go.transform.localScale = Vector3.one * (width / Mathf.Max(0.01f, picture.bounds.size.x));
                Sfx.PlayAt(SfxKind.Plop, _waterAt, 0.7f, 0.1f);
                Ripple(_waterAt, 1.4f);
            }
            else
            {
                s.Sr.sprite = UI.IconPainter.FishShadow;
                s.Sr.color = new Color(0.08f, 0.14f, 0.22f, 0.42f);
                s.Sr.sortingOrder = ShadowOrder;
                if (s.Halo != null) s.Halo.enabled = true;
                float scale = 0.45f + s.Kind.Shadow * 0.22f;
                s.Go.transform.localScale = new Vector3(scale, scale * 0.8f, 1f);
                Ripple(_waterAt, 1.1f);
            }
        }

        /// <summary>How often a bite turns out to be junk (never your very first catch).</summary>
        const float JunkChance = 0.1f;

        /// <summary>
        /// What else is down there: (art id, name, line). It goes to Salty Kombu for a coin, like
        /// the junk cats drag in (CatGifts).
        /// </summary>
        static readonly (string id, string name, string line)[] Junk =
        {
            ("Gift_Boot", "soggy old boot", "Not quite the catch of the day."),
            ("Gift_Can", "rusty tin can", "Somebody's lunch, a very long time ago."),
            ("Gift_Seaweed", "clump of seaweed", "Slimy, tangled and very, very green."),
        };

        /// <summary>The junk on the hook this fight, or default (a fish).</summary>
        (string id, string name, string line) _junk;

        /// <summary>Reeled in junk: a laugh, a little experience, and the thing itself in a bubble for Salty Kombu.</summary>
        void CatchJunk(Shadow s)
        {
            var junk = _junk;
            Live.SendHobby(null, "land", _bobberAt, junk.id, 0);
            const int xp = 2;
            HobbyLog.AddXp(Hobby.Fishing, xp);
            MermaidCharms.CaughtAt(_hookAt);   // a boot from a shell's ring still counts towards its five
            Remove(s);
            Sfx.Play(SfxKind.Pop, 0.8f);
            if (!_lifted) Ripple(_bobberAt, 2.2f);
            var art = HobbyData.Art(junk.id);
            var king = _king != null ? (Vector2)_king.transform.position : _bobberAt;
            CatGifts.GiveJunk(junk.id, junk.name, art, king);
            string line = junk.line + "\n" + string.Format("+{0} fishing XP", xp);
            UI.CardPopup.Show("You caught " + HobbyData.WithArticle(junk.name) + "!", line, art);
            EndLine();
        }

        void Escape(string why)
        {
            // A boot doesn't swim off; it just slips off the hook and sinks.
            if (_junk.id != null) why = "It slipped off the hook and sank. Keep reeling!";
            Live.SendHobby(null, "lose", _bobberAt, null, 0);
            Say(why);
            Sfx.Play(SfxKind.Pop, 0.4f);
            Flee(_hooked);
            EndLine();
        }

        void Catch(Shadow s)
        {
            if (_junk.id != null) { CatchJunk(s); return; }
            var kind = s.Kind;
            Live.SendHobby(null, "land", _bobberAt, kind.Id, 0);
            float size = Mathf.Round(UnityEngine.Random.Range(kind.Size.x, kind.Size.y));
            var (first, record) = HobbyLog.LogFish(kind.Id, size);
            int xp = 6 * kind.Stars + (first ? 15 : 0) + (record ? 5 : 0);
            HobbyLog.AddXp(Hobby.Fishing, xp);
            Motes.Trigger("hobby_fish");
            // In a shell's ring, every catch counts towards its shimmering shadow, for everyone there.
            if (s.Shimmer) MermaidCharms.ShimmerLanded(s.Charm);
            else MermaidCharms.CaughtAt(_hookAt);

            Remove(s);
            Sfx.Play(SfxKind.Chest, 0.8f);
            if (!_lifted) Ripple(_bobberAt, 2.2f);
            string line = $"{size:0} cm · {HobbyData.Rarity(kind.Stars)}" + (first ? "\n<color=#2DB3A6><b>New to your journal!</b></color>" : record ? "\n<color=#E3A92B><b>A new record!</b></color>" : "") + $"\n+{xp} fishing XP";
            UI.CardPopup.Show("You caught " + HobbyData.WithArticle(kind.Name) + "!", line, HobbyData.FishPicture(kind));
            EndLine();
        }

        void Flee(Shadow s)
        {
            if (s == null || s.Go == null) return;
            s.Fleeing = true;
            Vector2 at = s.Go.transform.position;
            s.Target = at + (at - _castFrom).normalized * 6f;
            if (s.Halo != null) s.Halo.DOFade(0f, 1f);
            s.Sr.DOFade(0f, 1f).OnComplete(() => Remove(s));
        }

        void EndLine()
        {
            if (Now != State.Idle) Live.SendHobby(null, "stop", _bobberAt, null, 0);   // (a catch being shown finishes first)
            PutRodAway();
            Now = State.Idle;
            _lifted = false;
            Holding = false;
            Tension = 0f;
            Surging = false;
            if (_bobber != null) Destroy(_bobber);
            if (_line != null) Destroy(_line.gameObject);
            _bobber = null;
            _line = null;
            if (_hooked != null && !_hooked.Fleeing) _hooked.NextTurn = 0f;
            _hooked = null;
            _castCharm = null;
        }

        static void Say(string line) => Said?.Invoke(line);

        // ---------------------------------------------------------------- props

        /// <summary>The line from the rod tip to the bobber: it pulls taut and changes colour with the strain.</summary>
        void DrawLine()
        {
            if (_line == null) return;
            Vector2 tip = RodTip();
            Vector2 bob = _bobber != null ? (Vector2)_bobberBody.position : _bobberAt;
            bool fighting = Now == State.Fight;
            float sag = fighting ? Mathf.Lerp(0.3f, 0.02f, Tension) : 0.35f;
            for (int i = 0; i < _line.positionCount; i++)
            {
                float t = i / (float)(_line.positionCount - 1);
                var p = Vector2.Lerp(tip, bob, t) + Vector2.down * Mathf.Sin(t * Mathf.PI) * sag;
                _line.SetPosition(i, new Vector3(p.x, p.y, 0f));
            }

            Color c = new(1f, 1f, 1f, 0.85f);
            float width = 0.025f;
            if (fighting)
            {
                c = TensionColour(Tension);
                width = 0.045f;
                // Red flashes, so it reads as a warning even out of the corner of your eye.
                if (Tension >= RedAt && Mathf.Repeat(Time.time * 8f, 1f) < 0.5f) c = Color.Lerp(c, Color.white, 0.45f);
            }
            _line.startColor = _line.endColor = c;
            _line.startWidth = _line.endWidth = width;
        }

        void MakeBobber(Vector2 at)
        {
            _bobber = new GameObject("Bobber");
            _bobber.transform.position = at;
            var body = new GameObject("Body").transform;
            body.SetParent(_bobber.transform, false);
            _bobberBody = body;
            var white = body.gameObject.AddComponent<SpriteRenderer>();
            white.sprite = UI.UiKit.Circle;
            white.color = Color.white;
            white.sortingOrder = ShadowOrder + 400;
            body.localScale = Vector3.one * 0.26f;
            var red = new GameObject("Cap").AddComponent<SpriteRenderer>();
            red.transform.SetParent(body, false);
            red.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            red.transform.localScale = new Vector3(0.9f, 0.6f, 1f);
            red.sprite = UI.UiKit.Circle;
            red.color = UI.UiKit.Danger;
            red.sortingOrder = white.sortingOrder + 1;
            _bobber.transform.localScale = Vector3.zero;
            _bobber.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            Ripple(at, 1.2f);

            var lineGo = new GameObject("FishingLine");
            _line = lineGo.AddComponent<LineRenderer>();
            _line.positionCount = 12;
            _line.startWidth = _line.endWidth = 0.025f;
            _line.useWorldSpace = true;
            if (_lineMaterial == null)
            {
                // A shader looked up by name can be stripped from a phone build; the bobber's own
                // sprite material is always there.
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                _lineMaterial = shader != null ? new Material(shader) : white.sharedMaterial;
            }
            _line.material = _lineMaterial;
            _line.startColor = _line.endColor = new Color(1f, 1f, 1f, 0.85f);
            _line.sortingOrder = 30000;
        }

        /// <summary>
        /// The big splash of a bite: a white burst where it broke the surface, and droplets thrown
        /// up in every direction, falling back with little rings where they land in the water.
        /// Smaller (<paramref name="size"/> under 1) for something tossed in, like a Mermaid's Shell.
        /// </summary>
        internal static void Splash(Vector2 at, float size = 1f)
        {
            var dot = UI.UiKit.Circle;
            float unit = Mathf.Max(0.01f, dot.bounds.size.x);

            var burst = new GameObject("SplashBurst");
            burst.transform.position = at;
            var bs = burst.AddComponent<SpriteRenderer>();
            bs.sprite = dot;
            bs.color = new Color(1f, 1f, 1f, 0.75f);
            bs.sortingOrder = 29990;   // up out of the water, over the shore
            burst.transform.localScale = new Vector3(0.25f, 0.13f, 1f) / unit;
            burst.transform.DOScale(new Vector3(1.5f, 0.75f, 1f) * size / unit, 0.4f).SetEase(Ease.OutCubic);
            bs.DOFade(0f, 0.4f).SetEase(Ease.InQuad).OnComplete(() => Destroy(burst));

            int drops = Mathf.RoundToInt(22 * size);
            for (int i = 0; i < drops; i++)
            {
                var drop = new GameObject("SplashDrop");
                drop.transform.position = at + UnityEngine.Random.insideUnitCircle * 0.12f;
                var sr = drop.AddComponent<SpriteRenderer>();
                sr.sprite = dot;
                sr.color = new Color(0.95f, 0.99f, 1f, 1f);
                sr.sortingOrder = 29991;
                float across = UnityEngine.Random.Range(0.1f, 0.24f);
                drop.transform.localScale = Vector3.one * (across / unit);
                // out to the sides (squashed: the water is seen from above at an angle), up and back down
                var land = at + new Vector2(UnityEngine.Random.Range(-1.2f, 1.2f), UnityEngine.Random.Range(-0.45f, 0.35f)) * size;
                float time = UnityEngine.Random.Range(0.5f, 0.85f);
                drop.transform.DOJump(land, UnityEngine.Random.Range(0.5f, 1.6f) * size, 1, time).SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        if (!BunniSpace.PointOnLand(land)) Ripple(land, 0.35f);
                        Destroy(drop);
                    });
                sr.DOFade(0f, time * 0.35f).SetDelay(time * 0.65f);
            }
        }

        internal static void Ripple(Vector2 at, float size)
        {
            var go = new GameObject("Ripple");
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UI.IconPainter.Ring;
            sr.color = new Color(1f, 1f, 1f, 0.7f);
            sr.sortingOrder = ShadowOrder + 10;
            go.transform.localScale = new Vector3(0.2f, 0.12f, 1f);
            go.transform.DOScale(new Vector3(size * 0.5f, size * 0.3f, 1f), 0.9f).SetEase(Ease.OutCubic);
            sr.DOFade(0f, 0.9f).OnComplete(() => Destroy(go));
        }
    }
}
