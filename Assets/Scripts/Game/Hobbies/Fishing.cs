using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Fishing. Fish shadows swim in the shallows around whichever shore you are near — the bigger
    /// the shadow, the bigger the fish. Stand at the water's edge and cast: the nearest fish comes
    /// to look, nibbles once or twice (wait!), then bites — reel in right then to catch it. Too
    /// early and it darts away; too late and it is gone.
    ///
    /// Catches go in the fish journal and raise your fishing level, which brings rarer fish.
    /// </summary>
    public class Fishing : MonoBehaviour
    {
        public enum State { Idle, Waiting, Nibbling, Bite }

        public static Fishing Instance { get; private set; }

        /// <summary>Something worth showing the player: a catch, an escape, a hint.</summary>
        public static event Action<string> Said;

        const int ShadowCount = 5;
        const float ShadowRange = 10f, DespawnRange = 18f;
        const int ShadowOrder = -125;   // over the shallows, under the sand

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
        Shadow _hooked;
        GameObject _bobber;
        Transform _bobberBody;
        LineRenderer _line;
        Vector2 _castFrom, _bobberAt;
        float _stateUntil, _giveUpAt;
        int _nibblesLeft;

        PlayerAvatar _king;
        static Material _lineMaterial;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var s in _shadows) if (s.Go != null) Destroy(s.Go);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => Said = null;

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

        // ---------------------------------------------------------------- the line

        /// <summary>Casts towards the nearest fish in reach, or just out into the water.</summary>
        public bool Cast()
        {
            if (Now != State.Idle || _king == null) return false;
            Vector2 you = _king.transform.position;
            var water = NearestWater(you, 2.6f);
            if (!water.HasValue) { Say("Walk right up to the water's edge to fish."); return false; }

            // Aim a little in front of the closest fish, or a few steps out to sea.
            Shadow target = null;
            float best = 6.5f;
            foreach (var s in _shadows)
            {
                if (s.Go == null || s.Fleeing) continue;
                float d = Vector2.Distance(you, s.Go.transform.position);
                if (d < best) { best = d; target = s; }
            }
            Vector2 spot;
            if (target != null)
            {
                Vector2 fish = target.Go.transform.position;
                spot = fish + (you - fish).normalized * 0.9f;
                if (BunniSpace.PointOnLand(spot)) spot = fish;
            }
            else
            {
                var dir = (water.Value - you).normalized;
                spot = you + dir * 3.2f;
                if (BunniSpace.PointOnLand(spot)) spot = water.Value;
            }

            _castFrom = you;
            _bobberAt = spot;
            MakeBobber(spot);
            Sfx.Play(SfxKind.Plop, 0.7f);
            Now = State.Waiting;
            _giveUpAt = Time.time + 14f;

            // The fish nearest the bobber comes over for a look.
            _hooked = null;
            float near = 3.5f;
            foreach (var s in _shadows)
            {
                if (s.Go == null || s.Fleeing) continue;
                float d = Vector2.Distance(spot, s.Go.transform.position);
                if (d < near) { near = d; _hooked = s; }
            }
            if (_hooked != null) _hooked.Target = spot + (Vector2)(_hooked.Go.transform.position - (Vector3)spot).normalized * 0.35f;
            return true;
        }

        /// <summary>The reel button: right on a bite, a catch; any earlier, the fish takes fright.</summary>
        public void Reel()
        {
            if (Now == State.Idle) return;
            if (Now == State.Bite && _hooked != null) { Catch(_hooked); return; }
            if (_hooked != null && Now == State.Nibbling) { Flee(_hooked); Say("Too soon! It got away. Wait for the big tug."); }
            else Say("Nothing on the line yet.");
            EndLine();
        }

        void Tick(Vector2 you)
        {
            if (Now == State.Idle) return;
            if (_line != null)
            {
                Vector2 tip = (Vector2)_king.transform.position + new Vector2(0.55f, 1.25f);
                Vector2 bob = _bobber != null ? (Vector2)_bobberBody.position : _bobberAt;
                for (int i = 0; i < _line.positionCount; i++)
                {
                    float t = i / (float)(_line.positionCount - 1);
                    var p = Vector2.Lerp(tip, bob, t) + Vector2.down * Mathf.Sin(t * Mathf.PI) * 0.35f;
                    _line.SetPosition(i, new Vector3(p.x, p.y, 0f));
                }
            }

            // Walking off reels the line in.
            if (Vector2.Distance(you, _castFrom) > 0.8f) { EndLine(); return; }

            switch (Now)
            {
                case State.Waiting:
                    if (_hooked == null || _hooked.Go == null)
                    {
                        if (Time.time > _giveUpAt) { Say("Nothing's biting here. Try near a fish shadow."); EndLine(); }
                        break;
                    }
                    if (Vector2.Distance(_hooked.Go.transform.position, _bobberAt) < 0.5f)
                    {
                        Now = State.Nibbling;
                        _nibblesLeft = UnityEngine.Random.Range(1, 4);
                        _stateUntil = Time.time + UnityEngine.Random.Range(0.7f, 1.4f);
                    }
                    break;

                case State.Nibbling:
                    if (Time.time < _stateUntil) break;
                    if (_nibblesLeft-- > 0)
                    {
                        Nibble(false);
                        _stateUntil = Time.time + UnityEngine.Random.Range(0.8f, 1.6f);
                    }
                    else
                    {
                        Now = State.Bite;
                        Nibble(true);
                        // Rarer fish give you less time.
                        _stateUntil = Time.time + Mathf.Max(0.42f, 0.85f - _hooked.Kind.Stars * 0.08f);
                    }
                    break;

                case State.Bite:
                    if (Time.time >= _stateUntil)
                    {
                        Flee(_hooked);
                        Say("Too slow! It slipped off the hook.");
                        EndLine();
                    }
                    break;
            }
        }

        void Nibble(bool bite)
        {
            if (_bobberBody == null) return;
            _bobberBody.DOKill();
            _bobberBody.localPosition = Vector3.zero;
            if (bite)
            {
                _bobberBody.DOLocalMoveY(-0.32f, 0.12f);
                _bobberBody.DOScaleY(0.55f, 0.12f);
                Sfx.Play(SfxKind.Pop, 0.9f);
                Bitten?.Invoke();
            }
            else
            {
                _bobberBody.DOLocalMoveY(-0.1f, 0.1f).SetLoops(2, LoopType.Yoyo);
                Sfx.Play(SfxKind.Plop, 0.35f);
            }
            Ripple(_bobberAt, bite ? 1.6f : 0.9f);
        }

        /// <summary>Raised the moment a fish truly bites, for the reel button to flash.</summary>
        public static event Action Bitten;

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
            Ripple(_bobberAt, 2.2f);
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
            Now = State.Idle;
            if (_bobber != null) Destroy(_bobber);
            if (_line != null) Destroy(_line.gameObject);
            _bobber = null;
            _line = null;
            if (_hooked != null && !_hooked.Fleeing) _hooked.NextTurn = 0f;
            _hooked = null;
        }

        static void Say(string line) => Said?.Invoke(line);

        // ---------------------------------------------------------------- props

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
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                _lineMaterial = new Material(shader);
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
