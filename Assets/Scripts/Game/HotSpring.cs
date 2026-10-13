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
        float _nextPuff;
        static Sprite _square;

        public Bounds PoolBounds => _pool != null ? _pool.bounds : default;

        /// <summary>While the capybara soaks: the grass at the pool's edge (where a cat on its head hops off to).</summary>
        public static Vector3? Poolside { get; private set; }

        void Awake()
        {
            var body = transform.Find("Sprite");
            _tower = body != null ? body.GetComponent<SpriteRenderer>() : null;
            if (_tower == null || _tower.sprite == null) return;

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
        }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { Bathing = false; Poolside = null; }

        void LateUpdate()
        {
            if (_pool == null || _tower == null) return;
            _pool.sortingOrder = _tower.sortingOrder + 1;   // in front of the tower's base

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
