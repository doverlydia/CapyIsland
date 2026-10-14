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
        const int ShadowOrder = -125;   // over the shallows, under the sand

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
        bool _lifted;       // the hooked fish is out of the water, up at the rod
        float _stateUntil, _giveUpAt;
        int _nibblesLeft;
        bool _warnedEarly;

        // The fight.
        const float LandAt = 0.7f;
        BaitKind _bait;   // on the hook for this cast, or null
        float BiteSpeed => _bait != null ? _bait.BiteSpeed : 1f;

        float _needed, _remaining, _startDistance, _strength, _phaseUntil, _nextSplash, _slackFor;   // line still to reel in, in reel-seconds

        PlayerAvatar _king;
        static Material _lineMaterial;

        // The rod in the capybara's paws while fishing (part of its body, so it turns with it).
        SpriteRenderer _rod, _kingBody;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var s in _shadows) if (s.Go != null) Destroy(s.Go);
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
        static bool OpenWater(Vector2 p, float margin)
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
                _nextSpawn = Time.time + 1.5f;
                for (int i = _shadows.Count - 1; i >= 0; i--)
                    if (_shadows[i].Go == null || (!_shadows[i].Fleeing && Vector2.Distance(_shadows[i].Go.transform.position, you) > DespawnRange))
                        Remove(_shadows[i]);
                if (_shadows.Count < ShadowCount) TrySpawn(you);
            }

            foreach (var s in _shadows) Swim(s);
            Tick(you);
        }

        void TrySpawn(Vector2 around)
        {
            for (int i = 0; i < 20; i++)
            {
                var p = around + UnityEngine.Random.insideUnitCircle * ShadowRange;
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
                for (int i = 0; i < 8; i++)
                {
                    var t = at + UnityEngine.Random.insideUnitCircle * 2.5f;
                    if (OpenWater(t, 0.4f)) { s.Target = t; break; }
                }
            }
            float speed = s.Fleeing ? 4f : s == _hooked && Now == State.Waiting ? 0.7f : s.Speed;
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
            var body = _king.transform.Find("Sprite");
            if (body == null) return;
            _kingBody = body.GetComponent<SpriteRenderer>();
            // The capybara's head is on the left of its picture: face the water by flipping to suit.
            var scale = body.localScale;
            scale.x = Mathf.Abs(scale.x) * (towards.x > 0f ? -1f : 1f);
            body.localScale = scale;

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
            if (_rod != null) _rod.gameObject.SetActive(false);
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
            _rod.transform.localRotation = Quaternion.Euler(0f, 0f, bend);
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
            var target = NearestShadow(you, 7f);
            _aimDir = ((target != null ? (Vector2)target.Go.transform.position : water.Value) - you).normalized;
            Cast();
        }

        /// <summary>
        /// How far the bobber stays from the land's edge. The drawn beach reaches a little past the
        /// land outline, so "not land" alone can still be sand on screen.
        /// </summary>
        const float BobberMargin = 0.7f;

        /// <summary>The landing point a distance out along the aim, always in open water, clear of the beach.</summary>
        Vector2 CastPoint(float distance)
        {
            Vector2 you = _king.transform.position;
            var p = you + _aimDir * distance;
            if (OpenWater(p, BobberMargin)) return p;
            // Short of the water (or on the beach): further out along the aim until it is open water…
            for (float d = distance; d < 9f; d += 0.25f)
                if (OpenWater(you + _aimDir * d, BobberMargin)) return you + _aimDir * d;
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

            // Just in front of the nearest fish, or a comfortable way out to sea.
            Vector2 spot;
            var fish = NearestShadow(you, 6.5f);
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
            Sfx.Play(SfxKind.Plop, 0.7f);
            Now = State.Waiting;
            _bait = Baits.UseOne();
            _warnedEarly = false;
            _giveUpAt = Time.time + 14f;
            _stateUntil = Time.time + 2f / BiteSpeed;

            // The fish nearest the bobber comes over for a look.
            _hooked = NearestShadow(spot, 3.5f);
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
            _startDistance = Mathf.Max(LandAt + 1.2f, Vector2.Distance(_castFrom, _bobberAt));
            _waterAt = _bobberAt;
            _lifted = false;
            // How much reeling it takes depends on the fish, not on how far out it bit: a common fish
            // is a few holds, a big rare one a proper tussle.
            _needed = _remaining = 4.5f + kind.Shadow * 0.7f + kind.Stars * 0.6f;
            Tension = 0.15f;
            Surging = false;
            _slackFor = 0f;
            _overload = 0f;
            _phaseUntil = Time.time + UnityEngine.Random.Range(1.2f, 1.8f);   // an early pull, to teach the rhythm

            if (_bobberBody != null)
            {
                _bobberBody.DOKill();
                _bobberBody.localPosition = new Vector3(0f, -0.25f, 0f);
                _bobberBody.localScale = new Vector3(0.26f, 0.16f, 1f);
            }
            Sfx.Play(SfxKind.Pop, 0.9f);
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

            // Most of the reeling brings it in through the water to the shore; the last of it lifts
            // it out, up the shortening line, to dangle at the end of the rod.
            float ratio = Mathf.Clamp(_remaining / _needed, 0f, 1.3f);
            float lift = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Clamp01(ratio / LiftShare));
            float distance = LandAt + (_startDistance - LandAt) * Mathf.Max(0f, (ratio - LiftShare) / (1f - LiftShare));

            // Move the bobber and fish along the line, thrashing side to side while it fights.
            var dir = (_waterAt - you).normalized;
            var side = new Vector2(-dir.y, dir.x);
            float thrash = (Surging ? Mathf.Sin(Time.time * 14f) * 0.35f : Mathf.Sin(Time.time * 3f) * 0.08f) * (1f - lift);
            var w = you + dir * distance + side * thrash;
            if (BunniSpace.PointOnLand(w)) w = BunniSpace.PointOnLand(you + dir * distance) ? _waterAt : you + dir * distance;   // never up on the sand
            _waterAt = w;
            var p = Vector2.Lerp(w, RodTip() + Vector2.down * 0.5f, lift);
            _bobberAt = p;
            if (_bobber != null) _bobber.transform.position = p;
            if (_hooked != null && _hooked.Go != null)
            {
                ShowLifted(_hooked, lift > 0.05f);
                float wiggle = Mathf.Sin(Time.time * (Surging ? 22f : 8f)) * (Surging ? 18f : 6f);
                if (_lifted)
                {
                    // Out of the water: hanging nose-up from the hook, wriggling.
                    _hooked.Go.transform.position = p + Vector2.down * 0.32f;
                    _hooked.Sr.flipX = false;
                    _hooked.Go.transform.localRotation = Quaternion.Euler(0f, 0f, -90f + wiggle);
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

        /// <summary>The share of the reeling, at the end, that lifts the fish out of the water to the rod.</summary>
        const float LiftShare = 0.25f;

        /// <summary>
        /// Out of the water a hooked fish is itself, not a shadow: its picture, in front of
        /// everything, sized to it. Back in (it pulled away), a shadow again.
        /// </summary>
        void ShowLifted(Shadow s, bool up)
        {
            if (up == _lifted || s == null || s.Sr == null) return;
            var picture = HobbyData.FishPicture(s.Kind);
            if (picture == null) return;
            _lifted = up;
            s.Sr.DOKill();
            if (up)
            {
                s.Sr.sprite = picture;
                s.Sr.color = Color.white;
                s.Sr.sortingOrder = 29999;   // just under the line
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
                float scale = 0.45f + s.Kind.Shadow * 0.22f;
                s.Go.transform.localScale = new Vector3(scale, scale * 0.8f, 1f);
                Ripple(_waterAt, 1.1f);
            }
        }

        void Escape(string why)
        {
            Say(why);
            Sfx.Play(SfxKind.Pop, 0.4f);
            Flee(_hooked);
            EndLine();
        }

        void Catch(Shadow s)
        {
            var kind = s.Kind;
            float size = Mathf.Round(UnityEngine.Random.Range(kind.Size.x, kind.Size.y));
            var (first, record) = HobbyLog.LogFish(kind.Id, size);
            int xp = 6 * kind.Stars + (first ? 15 : 0) + (record ? 5 : 0);
            HobbyLog.AddXp(Hobby.Fishing, xp);
            Motes.Trigger("hobby_fish");

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
            s.Sr.DOFade(0f, 1f).OnComplete(() => Remove(s));
        }

        void EndLine()
        {
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

        static void Ripple(Vector2 at, float size)
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
