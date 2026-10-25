using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The old hot spring at the foot of the Steam Tower: an onsen ringed with river stones, a straw
    /// rope with paper streamers along its back and a bamboo spout pouring in, in front of the
    /// tower (which stands a little grander for it), steaming away all day.
    ///
    /// Tap the tower or the pool and your capybara walks over, hops in and soaks: sunk to its middle
    /// in the water, a warm towel on its head in place of its hat, steam curling off it. After a
    /// moment of bliss the tower's page opens; close it and the capybara hops back out.
    ///
    /// In a new game the spring is first found frozen, as the Big Chill left it: iced over, no
    /// steam, a cold fire pit beside it. Tapping it then goes to the tutorial, which has you feed
    /// the fire wood (<see cref="TutorialDirector.SpringFrozen"/>); once it is fed, the fire
    /// catches, the ice cracks and lets go, and the steam comes back. Every save from before
    /// (and every friend's island) finds it warm, with the fire burning.
    /// </summary>
    [DefaultExecutionOrder(105)]   // after YSorter, so the pool and steam layer over the sorted tower
    public class HotSpring : MonoBehaviour
    {
        const float PoolWidth = 4.4f;     // world units across
        const float TowerGrow = 1.25f;
        /// <summary>
        /// Where the water's middle sits, from the middle of the picture, as a share of its height:
        /// the onsen's rope and bamboo spout stand up behind the pool, so the water is a little low.
        /// </summary>
        const float Water = -0.07f;
        const string Towel = "Hat_Towel";

        public static HotSpring Instance { get; private set; }

        /// <summary>The player's capybara is in the water (input is off until it climbs out).</summary>
        public static bool Bathing { get; private set; }

        SpriteRenderer _tower, _pool;
        float _nextPuff, _nextVent;
        /// <summary>The tower as built (warm), and the same tower under snow while the spring is frozen.</summary>
        Sprite _towerWarm, _towerCold;
        int _vent;
        static Sprite _square;

        public Bounds PoolBounds => _pool != null ? _pool.bounds : default;

        /// <summary>While the capybara soaks: the grass at the pool's edge (where a cat on its head hops off to).</summary>
        public static Vector3? Poolside { get; private set; }

        void Awake()
        {
            var body = transform.Find("Sprite");
            _tower = body != null ? body.GetComponent<SpriteRenderer>() : null;
            if (_tower == null || _tower.sprite == null) return;
            _towerWarm = _tower.sprite;
            _towerCold = Resources.Load<Sprite>("World/Build_Tower_Cold");

            // A grander tower, still standing on the same spot.
            float bottom = _tower.bounds.min.y;
            body.localScale *= TowerGrow;
            float lift = bottom - _tower.bounds.min.y;
            body.localPosition += Vector3.up * lift / Mathf.Max(0.0001f, transform.lossyScale.y);

            // The pool in front of it, its far rim tucked against the tower's base.
            var art = Resources.Load<Sprite>("World/HotSpring");
            if (art == null) return;
            var go = new GameObject("HotSpring");
            go.transform.SetParent(transform, false);
            _pool = go.AddComponent<SpriteRenderer>();
            _pool.sprite = art;
            float scale = PoolWidth / Mathf.Max(0.0001f, art.bounds.size.x * transform.lossyScale.x);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            float poolHeight = art.bounds.size.y * scale * transform.lossyScale.y;
            var b = _tower.bounds;
            go.transform.position = new Vector3(b.center.x, b.min.y - poolHeight * 0.3f, transform.position.z);

            // And beside it, where she lay down on the last night of the Big Chill: Old Yuzu.
            FrozenYuzu.Create(transform, _pool.bounds);

            BuildIce();
            BuildFirePit();
        }

        // ---------------------------------------------------------------- frozen

        /// <summary>The pool's colour while iced over: the stones and water gone cold.</summary>
        static readonly Color Chilled = new(0.8f, 0.88f, 1f);

        /// <summary>Raised when the thaw has played out: the ice is gone and the steam is back.</summary>
        public static event Action Thawed;

        bool _frozen, _stateSet, _thawing;
        SpriteRenderer _ice, _pit, _pitGlow;
        Sprite _iceWhole, _iceCracked, _pitOut, _pitLaid, _pitLit;
        float _nextGlint;
        Tween _flicker;

        /// <summary>Iced over (or thawing right now): no soak until it is warm.</summary>
        public bool Frozen => _frozen || _thawing;

        /// <summary>A sheet of ice over the water, painted to the water's shape.</summary>
        void BuildIce()
        {
            _iceWhole = PaintIce(false);
            _iceCracked = PaintIce(true);
            var go = new GameObject("Ice");
            go.transform.SetParent(_pool.transform, false);
            _ice = go.AddComponent<SpriteRenderer>();
            _ice.sprite = _iceWhole;
            // The water fills about 80% of the picture's width and half its height, a little low.
            var local = _pool.sprite.bounds;
            float px = _iceWhole.bounds.size.x, py = _iceWhole.bounds.size.y;
            go.transform.localScale = new Vector3(local.size.x * 0.81f / px, local.size.y * 0.54f / py, 1f);
            go.transform.localPosition = new Vector3(local.center.x + 0.005f * local.size.x, local.center.y + Water * local.size.y, 0f);
            _ice.enabled = false;
        }

        /// <summary>
        /// The fire pit on the far side of the pool from Old Yuzu: cold ashes under snow at first,
        /// a stack of logs once wood has been brought, and a cheerful fire once the spring is warm.
        /// </summary>
        void BuildFirePit()
        {
            _pitOut = Resources.Load<Sprite>("Cutscenes/Cutscene_CampfireOut");
            _pitLaid = Resources.Load<Sprite>("Cutscenes/Cutscene_Firewood");
            _pitLit = Resources.Load<Sprite>("Cutscenes/Cutscene_Campfire");
            if (_pitLit == null) return;
            var pool = _pool.bounds;
            var go = new GameObject("FirePit");
            go.transform.SetParent(transform, true);
            go.transform.position = new Vector3(pool.max.x + 0.3f, pool.center.y - 0.05f, transform.position.z);
            float s = 0.95f / Mathf.Max(0.0001f, _pitLit.bounds.size.x);
            go.transform.localScale = Vector3.one * s / Mathf.Max(0.0001f, transform.lossyScale.x);   // world-sized

            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            _pitGlow = glow.AddComponent<SpriteRenderer>();
            _pitGlow.sprite = UI.IconPainter.Glow;
            float g = 2.4f / Mathf.Max(0.0001f, (_pitGlow.sprite != null ? _pitGlow.sprite.bounds.size.x : 1f) * s);
            glow.transform.localScale = Vector3.one * g;
            _pitGlow.color = new Color(1f, 0.6f, 0.25f, 0f);

            var body = new GameObject("Fire");
            body.transform.SetParent(go.transform, false);
            _pit = body.AddComponent<SpriteRenderer>();
            _pit.sprite = _pitOut;
        }

        /// <summary>The fire pit was tapped (a tap there is a tap on the spring).</summary>
        public bool PitContains(Vector2 p)
        {
            if (_pit == null) return false;
            var b = _pit.bounds;
            b.Expand(new Vector3(0.2f, 0.2f, 0f));
            return b.Contains(new Vector3(p.x, p.y, b.center.z));
        }

        public Vector2 PitPoint => _pit != null ? (Vector2)_pit.bounds.center : (Vector2)PoolBounds.center;

        /// <summary>Iced over or warm, at once (a game loading, a tower appearing).</summary>
        void SetFrozenNow(bool frozen)
        {
            _frozen = frozen;
            ShowTower(frozen);
            if (_ice != null) { _ice.sprite = _iceWhole; _ice.color = Color.white; _ice.enabled = frozen; }
            if (_pool != null) _pool.color = frozen ? Chilled : Color.white;
            if (!frozen) LightFire(false);
            else ShowPit();
        }

        /// <summary>Cold ashes, or logs laid ready once some wood has come.</summary>
        void ShowPit()
        {
            if (_pit == null) return;
            _pit.sprite = TutorialDirector.SpringWoodFed > 0 && _pitLaid != null ? _pitLaid : _pitOut;
            _pitGlow.color = new Color(1f, 0.6f, 0.25f, 0f);
        }

        /// <summary>The tower under snow while the spring is frozen; as built once it's warm.</summary>
        void ShowTower(bool cold)
        {
            if (_tower == null) return;
            var want = cold && _towerCold != null ? _towerCold : _towerWarm;
            if (want != null && _tower.sprite != want) _tower.sprite = want;
        }

        void LightFire(bool punch)
        {
            if (_pit == null) return;
            _pit.sprite = _pitLit;
            _pitGlow.color = new Color(1f, 0.6f, 0.25f, 0.3f);
            _flicker?.Kill();
            var t = _pit.transform;
            t.localScale = Vector3.one;
            if (punch) t.DOPunchScale(new Vector3(0.25f, 0.35f, 0f), 0.5f, 6);
            _flicker = DOTween.Sequence().SetTarget(gameObject)
                .Append(t.DOScaleY(1.06f, 0.32f).SetEase(Ease.InOutSine))
                .Join(_pitGlow.DOFade(0.42f, 0.32f))
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(punch ? 0.5f : 0f);
        }

        /// <summary>
        /// Wood brought for the fire: a few logs hop from the capybara onto the pit, each landing
        /// with a knock. More wood, more logs (up to six).
        /// </summary>
        public void Feed(int wood, Vector2 from)
        {
            if (_pit == null || wood <= 0) return;
            var art = Resources.Load<Sprite>("Cutscenes/Cutscene_Log");
            int logs = Mathf.Clamp(wood / 12 + 1, 1, 6);
            Vector2 to = PitPoint;
            if (Vector2.Distance(from, to) > 9f) from = to + new Vector2(-1.6f, 1.2f);   // too far to see: from just off to one side
            for (int i = 0; i < logs; i++)
            {
                var go = new GameObject("Log");
                go.transform.SetParent(transform.parent, false);
                go.transform.position = new Vector3(from.x, from.y + 0.5f, transform.position.z);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = art != null ? art : UI.UiKit.Circle;
                if (art == null) sr.color = new Color(0.62f, 0.4f, 0.22f);
                sr.sortingOrder = _pit.sortingOrder + 10;
                float size = 0.42f / Mathf.Max(0.0001f, sr.sprite.bounds.size.x);
                go.transform.localScale = Vector3.one * size;
                go.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-30f, 30f));
                var land = to + UnityEngine.Random.insideUnitCircle * 0.12f;
                var seq = DOTween.Sequence().SetTarget(go).SetDelay(i * 0.12f);
                seq.Append(go.transform.DOJump(new Vector3(land.x, land.y, go.transform.position.z), 1.1f, 1, 0.55f).SetEase(Ease.Linear));
                seq.Join(go.transform.DORotate(new Vector3(0f, 0f, UnityEngine.Random.Range(200f, 400f)), 0.55f, RotateMode.FastBeyond360));
                seq.OnComplete(() =>
                {
                    if (go != null) Destroy(go);
                    if (_pit == null) return;
                    Sfx.PlayAt(SfxKind.Chop, to, 0.5f);
                    ShowPit();
                    _pit.transform.DOKill(true);
                    _pit.transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.25f, 5);
                });
            }
        }

        /// <summary>
        /// The thaw: the fire catches, the ice creaks and cracks, then lets go all at once in a
        /// great breath of steam.
        /// </summary>
        IEnumerator Thaw()
        {
            _thawing = true;
            var fx = HarvestEffects.Instance;
            yield return new WaitForSeconds(0.9f);   // the last logs land

            LightFire(true);
            Sfx.PlayAt(SfxKind.Fuse, PitPoint, 0.8f);
            if (fx != null) fx.Twinkle(PitPoint + Vector2.up * 0.3f, 8);
            yield return new WaitForSeconds(1f);

            if (_ice != null)
            {
                _ice.sprite = _iceCracked;
                _ice.transform.DOShakePosition(0.45f, new Vector3(0.05f, 0.03f, 0f), 18).SetTarget(gameObject);
            }
            Sfx.PlayAt(SfxKind.Fell, PoolBounds.center, 0.7f);
            yield return new WaitForSeconds(0.9f);

            if (_ice != null) _ice.transform.DOShakePosition(0.3f, new Vector3(0.07f, 0.04f, 0f), 22).SetTarget(gameObject);
            Sfx.PlayAt(SfxKind.Fell, PoolBounds.center, 0.9f);
            yield return new WaitForSeconds(0.35f);

            // Warm again: the snow goes from the tower inside the burst of steam.
            _frozen = false;
            ShowTower(false);
            if (_ice != null) _ice.DOFade(0f, 1.3f).SetTarget(gameObject);
            if (_pool != null) _pool.DOColor(Color.white, 1.3f).SetTarget(gameObject);
            Sfx.Play(SfxKind.Heart, 0.7f);
            var p = PoolBounds;
            for (int i = 0; i < 14; i++)
            {
                var inWater = UnityEngine.Random.insideUnitCircle;
                Puff(new Vector2(p.center.x + inWater.x * p.extents.x * 0.62f, p.center.y + Water * p.size.y + inWater.y * p.extents.y * 0.45f),
                     UnityEngine.Random.Range(0.6f, 1f), UnityEngine.Random.Range(1.8f, 2.8f));
            }
            if (fx != null) fx.Twinkle(new Vector2(p.center.x, p.center.y + Water * p.size.y), 14);
            yield return new WaitForSeconds(1.4f);

            if (_ice != null) _ice.enabled = false;
            _thawing = false;
            Thawed?.Invoke();
        }

        /// <summary>
        /// A colour painting of the ice: pale blue, frosted white towards the rim, with soft bands
        /// of shine; the cracked one has dark lines running out from near the middle.
        /// </summary>
        /// <summary>
        /// 0 below <paramref name="from"/>, 1 above <paramref name="to"/>, eased between: a shader's
        /// smoothstep. (Unity's Mathf.SmoothStep is something else: it eases from its first number to
        /// its second, which left the ice almost see-through.)
        /// </summary>
        static float Fade(float from, float to, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, x));

        static Sprite PaintIce(bool cracked)
        {
            const int w = 256, h = 160;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave,
            };
            var px = new Color32[w * h];
            // Cracks as polylines in -1..1 space.
            var cracks = new[]
            {
                new[] { new Vector2(-0.05f, 0.05f), new Vector2(-0.3f, 0.25f), new Vector2(-0.45f, 0.2f), new Vector2(-0.7f, 0.42f) },
                new[] { new Vector2(-0.05f, 0.05f), new Vector2(0.18f, -0.12f), new Vector2(0.42f, -0.08f), new Vector2(0.75f, -0.35f) },
                new[] { new Vector2(-0.05f, 0.05f), new Vector2(0.05f, 0.4f), new Vector2(0.22f, 0.62f) },
                new[] { new Vector2(0.18f, -0.12f), new Vector2(0.1f, -0.5f), new Vector2(-0.12f, -0.72f) },
                new[] { new Vector2(-0.3f, 0.25f), new Vector2(-0.38f, -0.15f), new Vector2(-0.62f, -0.38f) },
            };
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = (i + 0.5f) / w * 2f - 1f, y = (j + 0.5f) / h * 2f - 1f;
                float r = Mathf.Sqrt(x * x + y * y);
                if (r >= 1f) { px[j * w + i] = new Color32(0, 0, 0, 0); continue; }
                float edge = 1f - Fade(0.9f, 1f, r);
                float frost = Fade(0.55f, 0.95f, r);
                float band = Mathf.Abs(Mathf.Repeat((x * 0.8f + y * 0.6f) * 2.2f + 0.3f, 1f) - 0.5f) * 2f;
                float shine = Fade(0.75f, 1f, band) * 0.5f;
                var c = Color.Lerp(new Color(0.74f, 0.88f, 0.98f), Color.white, Mathf.Clamp01(frost * 0.7f + shine));
                float a = (0.78f + 0.18f * frost) * edge;
                if (cracked)
                {
                    float d = float.MaxValue;
                    foreach (var line in cracks)
                        for (int k = 0; k + 1 < line.Length; k++)
                            d = Mathf.Min(d, SegmentDistance(new Vector2(x, y * 0.62f), new Vector2(line[k].x, line[k].y * 0.62f), new Vector2(line[k + 1].x, line[k + 1].y * 0.62f)));
                    float crack = 1f - Fade(0.006f, 0.016f, d);
                    float halo = (1f - Fade(0.016f, 0.04f, d)) * 0.6f;
                    c = Color.Lerp(c, Color.white, halo * (1f - crack));
                    c = Color.Lerp(c, new Color(0.4f, 0.58f, 0.78f), crack);
                    a = Mathf.Max(a, crack * edge);
                }
                px[j * w + i] = new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), (byte)(Mathf.Clamp01(a) * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>Tapped while iced over: the tutorial knows what to say (and what to do about it).</summary>
        public void TapFrozen()
        {
            if (_thawing) return;
            var tutorial = TutorialDirector.Instance;
            if (tutorial != null) { tutorial.SpringTapped(); return; }
            var ghost = UI.GhostDialogue.Instance;
            UI.SpeechDialog.Show(ghost != null ? ghost.GhostFace : null, StoryText.Guide, new[] { StoryText.Spring.FrozenFromAfar });
        }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { Bathing = false; Poolside = null; Thawed = null; }

        void LateUpdate()
        {
            if (_pool == null || _tower == null) return;
            _pool.sortingOrder = _tower.sortingOrder + 1;   // in front of the tower's base
            if (_ice != null) _ice.sortingOrder = _pool.sortingOrder + 1;
            if (_pit != null)
            {
                int order = YSorter.PropSortBase + Mathf.RoundToInt(-_pit.transform.position.y * 100f);
                _pit.sortingOrder = order;
                _pitGlow.sortingOrder = order - 1;
            }

            // Frozen or warm, as the opening has it. The first look just sets it; a change after
            // that (the fire fed, the tutorial ended) plays the thaw.
            bool frozen = TutorialDirector.SpringFrozen;
            if (!_stateSet) { _stateSet = true; SetFrozenNow(frozen); }
            else if (!_thawing && frozen != _frozen)
            {
                if (frozen) SetFrozenNow(true);
                else StartCoroutine(Thaw());
            }
            if (_frozen)
            {
                if (!_thawing) ShowPit();
                // Now and then a cold glint on the ice, instead of steam.
                if (Time.time >= _nextGlint)
                {
                    _nextGlint = Time.time + UnityEngine.Random.Range(1.2f, 2.4f);
                    var fx = HarvestEffects.Instance;
                    var b = _ice != null ? _ice.bounds : _pool.bounds;
                    var at = UnityEngine.Random.insideUnitCircle * 0.7f;
                    if (fx != null) fx.Twinkle(new Vector2(b.center.x + at.x * b.extents.x, b.center.y + at.y * b.extents.y), 1);
                }
                return;
            }

            // And from the tower: its roof tip and the two heart vents take turns puffing.
            if (_tower != null && Time.time >= _nextVent)
            {
                _nextVent = Time.time + UnityEngine.Random.Range(0.55f, 0.95f);
                var t = _tower.bounds;
                _vent = (_vent + 1 + UnityEngine.Random.Range(0, 2)) % 3;
                // Measured off the picture: the tip at the top middle, the hearts either side lower down the roof.
                var vent = _vent == 0 ? new Vector2(t.center.x, t.max.y - t.size.y * 0.08f)
                         : new Vector2(t.center.x + (_vent == 1 ? -0.1f : 0.1f) * t.size.x, t.max.y - t.size.y * 0.25f);
                Puff(vent, UnityEngine.Random.Range(0.35f, 0.55f), UnityEngine.Random.Range(1.1f, 1.7f));
            }

            // Steam off the water, all day long.
            if (Time.time >= _nextPuff)
            {
                _nextPuff = Time.time + UnityEngine.Random.Range(0.22f, 0.4f);
                var p = _pool.bounds;
                var inWater = UnityEngine.Random.insideUnitCircle;
                Puff(new Vector2(p.center.x + inWater.x * p.extents.x * 0.62f, p.center.y + (Water + 0.01f) * p.size.y + inWater.y * p.extents.y * 0.45f),
                     UnityEngine.Random.Range(0.45f, 0.75f), UnityEngine.Random.Range(1.4f, 2.2f));
            }
        }

        /// <summary>A soft cloud of steam that rises, swells and fades.</summary>
        void Puff(Vector2 at, float size, float rise)
        {
            var go = new GameObject("Steam");
            go.transform.SetParent(transform.parent, false);
            go.transform.position = new Vector3(at.x, at.y, transform.position.z);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UI.IconPainter.Cloud;
            sr.color = new Color(1f, 1f, 1f, 0f);
            sr.sortingOrder = (_pool != null ? _pool.sortingOrder : 0) + 400;   // over a soaking capybara too
            float px = sr.sprite != null ? sr.sprite.bounds.size.x : 1f;
            go.transform.localScale = Vector3.one * (size * 0.55f / Mathf.Max(0.0001f, px));
            float life = UnityEngine.Random.Range(2.4f, 3.2f);
            var seq = DOTween.Sequence().SetTarget(go);
            seq.Append(sr.DOFade(0.42f, life * 0.25f));
            seq.Append(sr.DOFade(0f, life * 0.75f).SetEase(Ease.InSine));
            seq.Insert(0f, go.transform.DOMove(go.transform.position + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), rise, 0f), life).SetEase(Ease.OutSine));
            seq.Insert(0f, go.transform.DOScale(go.transform.localScale * 1.9f, life));
            seq.OnComplete(() => { if (go != null) Destroy(go); });
        }

        /// <summary>The tower or the pool was tapped (within the pool's own shape).</summary>
        public bool PoolContains(Vector2 p)
        {
            if (_pool == null) return false;
            var b = _pool.bounds;
            float dx = (p.x - b.center.x) / Mathf.Max(0.01f, b.extents.x), dy = (p.y - b.center.y) / Mathf.Max(0.01f, b.extents.y);
            return dx * dx + dy * dy <= 1f;
        }

        // ---------------------------------------------------------------- a soak

        /// <summary>
        /// Walks the capybara over for a soak, then calls <paramref name="openPage"/> with the climb-out
        /// to run when that page closes. Without a pool or a capybara (or already in), just opens it.
        /// </summary>
        public void Visit(Action<Action> openPage)
        {
            if (Bathing) return;
            var king = FindFirstObjectByType<PlayerAvatar>();
            if (king == null || _pool == null) { openPage(null); return; }
            StartCoroutine(Soak(king, openPage));
        }

        IEnumerator Soak(PlayerAvatar king, Action<Action> openPage)
        {
            Bathing = true;
            var pool = _pool.bounds;
            var edge = new Vector2(pool.center.x, pool.min.y - 0.35f);

            // Over to the water's edge, if not there already. Steering away calls the soak off.
            if (Vector2.Distance(king.transform.position, edge) > 1.3f)
            {
                king.WalkTo(edge, 0.5f);
                float giveUp = Time.time + 14f;
                while (king.AutoWalking && Time.time < giveUp) yield return null;
                if (Vector2.Distance(king.transform.position, edge) > 2.6f) { Bathing = false; openPage(null); yield break; }
            }

            // Hop in. Everything following along (the trail, a warm heart) steps out of sight
            // until the capybara climbs out, so nothing wades in after it or perches on its head.
            Poolside = new Vector3(pool.max.x + 0.4f, pool.min.y - 0.2f, king.transform.position.z);
            var hidden = HideTrail();
            king.enabled = false;
            var body = king.transform.Find("Sprite");
            var bodyRenderer = body != null ? body.GetComponent<SpriteRenderer>() : null;
            if (bodyRenderer == null) { ShowTrail(hidden, king.transform.position); king.enabled = true; Bathing = false; Poolside = null; openPage(null); yield break; }
            body.localPosition = Vector3.zero;
            float height = bodyRenderer.bounds.size.y;
            float feet = king.transform.position.y - bodyRenderer.bounds.min.y;   // root above its feet
            float waterY = pool.center.y + Water * pool.size.y;
            var seat = new Vector3(pool.center.x + 0.08f * pool.size.x, waterY - 0.52f * height + feet, king.transform.position.z);
            var fx = HarvestEffects.Instance;
            Sfx.Play(SfxKind.Rustle, 0.4f);
            yield return king.transform.DOJump(seat, 0.9f, 1, 0.55f).SetEase(Ease.Linear).WaitForCompletion();

            // In the water: only what is above the surface shows, with foam where it meets it.
            CapyOutfit.OverrideHat(Towel);
            yield return null;   // let the towel go on before the mask is set
            // The outfit's sorting group would keep its layers out of reach of the water mask.
            var group = body.GetComponent<UnityEngine.Rendering.SortingGroup>();
            if (group != null) group.enabled = false;
            var mask = WaterLine(king, bodyRenderer, waterY, pool);
            var foam = Foam(bodyRenderer, waterY);
            Sfx.Play(SfxKind.Plop, 0.9f);
            if (fx != null) fx.Twinkle(new Vector2(seat.x, waterY + 0.2f), 8);
            var restScale = body.localScale;
            var breath = body.DOScaleY(restScale.y * 1.04f, 1.3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

            // A moment of pure bliss, steam rising off the towel.
            float until = Time.time + 2.8f;
            while (Time.time < until)
            {
                var top = bodyRenderer.bounds;
                Puff(new Vector2(top.center.x + UnityEngine.Random.Range(-0.2f, 0.2f), top.max.y - 0.15f), 0.35f, 1.1f);
                yield return new WaitForSeconds(0.35f);
            }
            if (fx != null) fx.Twinkle((Vector2)bodyRenderer.bounds.center + Vector2.up * 0.6f, 5);

            // The tower's page; closing it means time to climb out.
            bool closed = false;
            openPage(() => closed = true);
            while (!closed)
            {
                if (UnityEngine.Random.value < 0.02f)
                {
                    var top = bodyRenderer.bounds;
                    Puff(new Vector2(top.center.x, top.max.y - 0.15f), 0.3f, 1f);
                }
                yield return null;
            }

            // Out, with a shake.
            breath.Kill();
            body.localScale = new Vector3(body.localScale.x, restScale.y, restScale.z);
            foreach (var f in foam) if (f != null) { DOTween.Kill(f.transform); Destroy(f); }
            foreach (var r in body.GetComponentsInChildren<SpriteRenderer>(true)) SetMasked(r, false);
            if (mask != null) Destroy(mask);
            if (group != null) group.enabled = true;
            CapyOutfit.OverrideHat(null);
            var landing = new Vector3(edge.x + 0.6f, edge.y - 0.2f, seat.z);
            Sfx.Play(SfxKind.Plop, 0.6f);
            yield return king.transform.DOJump(landing, 0.9f, 1, 0.5f).WaitForCompletion();
            body.DOPunchRotation(new Vector3(0f, 0f, 12f), 0.5f, 8);
            if (fx != null) fx.Twinkle((Vector2)bodyRenderer.bounds.center, 10);
            ShowTrail(hidden, landing);
            king.enabled = true;
            Bathing = false;
            Poolside = null;
        }

        /// <summary>Hides what follows the capybara: pictures off and standing still, but still on the island (and in the save).</summary>
        static List<(Follower follower, List<Renderer> drawn)> HideTrail()
        {
            var hidden = new List<(Follower, List<Renderer>)>();
            foreach (var f in FindObjectsByType<Follower>(FindObjectsSortMode.None))
            {
                if (!f.Carried || !f.enabled) continue;
                var drawn = new List<Renderer>();
                foreach (var r in f.GetComponentsInChildren<Renderer>())
                    if (r.enabled) { r.enabled = false; drawn.Add(r); }
                f.enabled = false;
                hidden.Add((f, drawn));
            }
            return hidden;
        }

        /// <summary>Back again, in a little line behind the capybara where it landed.</summary>
        static void ShowTrail(List<(Follower follower, List<Renderer> drawn)> hidden, Vector3 landing)
        {
            for (int i = 0; i < hidden.Count; i++)
            {
                var (f, drawn) = hidden[i];
                if (f == null) continue;
                f.transform.position = new Vector3(landing.x - 0.9f * (i + 1), landing.y + 0.15f * i, f.transform.position.z);
                foreach (var r in drawn) if (r != null) r.enabled = true;
                f.enabled = true;
            }
        }

        /// <summary>A mask that shows only what is above the water.</summary>
        static GameObject WaterLine(PlayerAvatar king, SpriteRenderer body, float waterY, Bounds pool)
        {
            if (_square == null)
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var white = new Color32[16];
                for (int i = 0; i < white.Length; i++) white[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(white);
                tex.Apply(false, true);
                _square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0f), 4f);   // one unit, standing on its base
            }
            var go = new GameObject("WaterLine");
            go.transform.position = new Vector3(body.bounds.center.x, waterY, king.transform.position.z);
            go.transform.localScale = new Vector3(pool.size.x, 10f, 1f);
            var mask = go.AddComponent<SpriteMask>();
            mask.sprite = _square;
            // Only for the capybara's own layers, nothing else on the island.
            mask.isCustomRangeActive = true;
            mask.frontSortingOrder = body.sortingOrder + 10;
            mask.backSortingOrder = body.sortingOrder - 10;
            foreach (var r in body.GetComponentsInChildren<SpriteRenderer>(true)) SetMasked(r, true);
            return go;
        }

        /// <summary>
        /// Turns a layer's water mask on or off. The sprite is set again afterwards: otherwise, the
        /// first time a layer is masked, Unity can draw it with the body's picture (a tiny
        /// capybara where the towel should be).
        /// </summary>
        static void SetMasked(SpriteRenderer r, bool masked)
        {
            r.maskInteraction = masked ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;
            var sprite = r.sprite;
            r.sprite = null;
            r.sprite = sprite;
        }

        /// <summary>Soft white foam along the water line, hiding where the capybara meets it.</summary>
        List<GameObject> Foam(SpriteRenderer body, float waterY)
        {
            var list = new List<GameObject>();
            var b = body.bounds;
            const int blobs = 9;
            for (int i = 0; i < blobs; i++)
            {
                float t = i / (blobs - 1f), edge = Mathf.Abs(t - 0.5f) * 2f;
                var go = new GameObject("Foam");
                go.transform.position = new Vector3(Mathf.Lerp(b.min.x - 0.05f, b.max.x + 0.05f, t), waterY + UnityEngine.Random.Range(-0.04f, 0.04f) - edge * 0.05f, body.transform.position.z);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = UI.UiKit.Circle;
                sr.color = i % 3 == 1 ? new Color(0.84f, 0.95f, 0.97f, 0.95f) : new Color(1f, 1f, 1f, 0.95f);
                sr.sortingOrder = body.sortingOrder + 6;
                float px = sr.sprite != null ? sr.sprite.bounds.size.x : 1f;
                float size = Mathf.Lerp(0.3f, 0.2f, edge) * UnityEngine.Random.Range(0.85f, 1.15f);
                go.transform.localScale = new Vector3(size * 1.3f / px, size / px, 1f);
                go.transform.DOScale(go.transform.localScale * 1.1f, UnityEngine.Random.Range(0.7f, 1.2f)).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                list.Add(go);
            }
            return list;
        }

        void OnDestroy()
        {
            DOTween.Kill(gameObject);
        }
    }
}
