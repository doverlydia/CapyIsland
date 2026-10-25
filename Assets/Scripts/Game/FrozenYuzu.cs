using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Old Yuzu herself, beside her spring where she lay down on the last night of the Big Chill:
    /// curled up under a dome of ice, tinted cold. As warm hearts come home she thaws a little at
    /// a time (see <see cref="YuzuHeart"/>): the ice cracks and drips, she twitches now and then,
    /// a warm glow shows at her heart. Once she is back she stands here alive, unless she has
    /// gone over to the Blossom Chapel to marry you two.
    ///
    /// Tap her: her ghost has something to say about it (and later, she does in person).
    /// </summary>
    public class FrozenYuzu : MonoBehaviour, ITappable
    {
        public static FrozenYuzu Instance { get; private set; }

        SpriteRenderer _body, _ice, _cracked, _glow, _alive;
        float _nextDrip, _nextTwitch;
        int _said;
        static Sprite _soft;

        static readonly Color Icy = new(0.6f, 0.76f, 0.95f);

        /// <summary>Lies down by the pool (left of it, on the grass), riding with the tower.</summary>
        public static FrozenYuzu Create(Transform parent, Bounds pool)
        {
            var go = new GameObject("OldYuzu");
            go.transform.SetParent(parent, true);
            go.transform.position = new Vector3(pool.min.x - 1.1f, pool.center.y - 0.2f, parent.position.z);
            go.transform.localScale = Vector3.one / Mathf.Max(0.0001f, parent.lossyScale.x);   // world-sized, whatever the tower's scale
            var me = go.AddComponent<FrozenYuzu>();
            me.Build();
            return me;
        }

        SpriteRenderer Layer(string name, Sprite art, float worldWidth, Vector2 offset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art;
            if (art != null)
            {
                float s = worldWidth / Mathf.Max(0.01f, art.bounds.size.x);
                go.transform.localScale = new Vector3(s, s, 1f);
                // stand it on the ground at the root, whatever the picture's pivot
                float bottom = (art.bounds.min.y) * s;
                go.transform.localPosition = new Vector3(offset.x, offset.y - bottom, 0f);
            }
            return sr;
        }

        void Build()
        {
            var rest = Resources.Load<Sprite>("Cutscenes/Cutscene_YuzuRest");
            var shell = Resources.Load<Sprite>("Cutscenes/Cutscene_IceShell");
            var cracked = Resources.Load<Sprite>("Cutscenes/Cutscene_IceShellCracked");
            var standing = Resources.Load<Sprite>("Cutscenes/Cutscene_IntroYuzu");
            _body = Layer("Body", rest, 1.5f, Vector2.zero);
            _ice = Layer("Ice", shell, 1.95f, new Vector2(0f, -0.08f));
            _cracked = Layer("IceCracked", cracked, 1.95f, new Vector2(0f, -0.08f));
            _glow = Layer("Glow", SoftLight(), 1.7f, new Vector2(0.05f, 0.05f));
            _glow.color = new Color(1f, 0.62f, 0.25f, 0f);
            _alive = Layer("Alive", standing, 1.3f, Vector2.zero);
            _alive.enabled = false;
            Refresh(true);
            // Named on the map: frozen in her shell, or up and about.
            NpcTag.Add(gameObject, StoryText.Guide, null, () => _alive != null && _alive.enabled ? _alive.sprite : _body != null ? _body.sprite : null);
        }

        void OnEnable()
        {
            Instance = this;
            TapTargets.Register(this);
            YuzuHeart.Changed += OnStage;
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            TapTargets.Unregister(this);
            YuzuHeart.Changed -= OnStage;
        }

        void OnStage(YuzuHeart.Stage s) => Refresh(true);

        /// <summary>Where a heart flies to once she has started to feel them: her heart.</summary>
        public Vector2 HeartPoint => _body != null ? (Vector2)_body.bounds.center + new Vector2(0.1f, 0.05f) : (Vector2)transform.position;

        /// <summary>A heart just arrived: the glow flares.</summary>
        public void Warmed()
        {
            if (_glow == null) return;
            _glow.transform.DOKill(true);
            _glow.transform.DOPunchScale(Vector3.one * 0.5f, 0.6f, 4);
            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Twinkle(HeartPoint, 6);
        }

        /// <summary>Somewhere else she is needed: alive Yuzu marries people at the chapel, if there is one.</summary>
        static bool AtChapel => FindFirstObjectByType<Church>() != null;

        void Refresh(bool instant)
        {
            var stage = YuzuHeart.Shown;
            bool alive = stage >= YuzuHeart.Stage.Alive;
            _body.enabled = _ice.enabled = _cracked.enabled = _glow.enabled = !alive;
            _alive.enabled = alive && !AtChapel;
            if (alive) return;
            float t = YuzuHeart.Thaw;
            // The ice shows its cracks once the first drip has been seen; her colour warms with the thaw.
            bool cracking = stage >= YuzuHeart.Stage.Drip;
            _ice.color = new Color(1f, 1f, 1f, cracking ? 0f : 0.62f);
            _cracked.color = new Color(1f, 1f, 1f, cracking ? Mathf.Lerp(0.62f, 0.35f, t) : 0f);
            _body.color = Color.Lerp(Icy, new Color(0.92f, 0.95f, 1f), Mathf.Clamp01(t * 1.1f));
        }

        void Update()
        {
            var stage = YuzuHeart.Shown;
            if (stage >= YuzuHeart.Stage.Alive)
            {
                // Alive: by the spring, unless she has gone to the chapel.
                bool here = !AtChapel;
                if (_alive.enabled != here) _alive.enabled = here;
                return;
            }
            Refresh(false);

            // The ice drips.
            if (stage >= YuzuHeart.Stage.Drip && Time.time >= _nextDrip)
            {
                _nextDrip = Time.time + Random.Range(2.5f, 5f);
                Drip();
            }
            // Now and then, the tiniest twitch.
            if (stage >= YuzuHeart.Stage.Twitch && Time.time >= _nextTwitch)
            {
                _nextTwitch = Time.time + Random.Range(7f, 14f);
                _body.transform.DOComplete();
                _body.transform.DOPunchScale(new Vector3(0.03f, 0.05f, 0f), 0.35f, 5);
            }
            // A warm glow where her heart is, breathing.
            if (stage >= YuzuHeart.Stage.Glow)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 1.6f);
                _glow.color = new Color(1f, 0.62f, 0.25f, 0.4f + 0.35f * pulse);
            }
            else _glow.color = new Color(1f, 0.62f, 0.25f, 0f);
        }

        void LateUpdate()
        {
            // Layered in the island's 3/4 order: body, then ice over it, then the glow.
            int order = YSorter.PropSortBase + Mathf.RoundToInt(-transform.position.y * 100f);
            _body.sortingOrder = order;
            _ice.sortingOrder = _cracked.sortingOrder = order + 1;
            _glow.sortingOrder = order + 2;
            _alive.sortingOrder = order;
        }

        void Drip()
        {
            var b = _cracked.bounds;
            var go = new GameObject("Drip");
            go.transform.position = new Vector3(Random.Range(b.min.x + 0.2f, b.max.x - 0.2f), b.min.y + 0.12f, transform.position.z);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UI.UiKit.Circle;
            sr.color = new Color(0.75f, 0.9f, 1f, 0.95f);
            sr.sortingOrder = _cracked.sortingOrder + 1;
            float px = sr.sprite != null ? sr.sprite.bounds.size.x : 1f;
            go.transform.localScale = new Vector3(0.08f / px, 0.11f / px, 1f);
            var seq = DOTween.Sequence().SetTarget(go);
            seq.Append(go.transform.DOMoveY(go.transform.position.y - 0.25f, 0.35f).SetEase(Ease.InQuad));
            seq.Join(sr.DOFade(0f, 0.35f).SetEase(Ease.InQuad));
            seq.OnComplete(() => { if (go != null) Destroy(go); });
        }

        // ---------------------------------------------------------------- tapping

        public float TapDistance(Vector2 worldPoint)
        {
            var r = YuzuHeart.Alive ? _alive : _body;
            if (r == null || !r.enabled) return -1f;
            var b = r.bounds;
            b.Expand(new Vector3(0.3f, 0.3f, 0f));
            return b.Contains(new Vector3(worldPoint.x, worldPoint.y, b.center.z)) ? Vector2.Distance(b.center, worldPoint) : -1f;
        }

        public void Tap()
        {
            var ghost = UI.GhostDialogue.Instance;
            if (YuzuHeart.Alive)
            {
                string[] alive = YuzuHeart.FinaleSeen ? StoryText.Yuzu.Home
                    : Motes.Has(YuzuHeart.WeddingHeart) ? new[] { StoryText.Yuzu.Married, StoryText.Yuzu.Back[0] }
                    : Motes.Has("Madly in Love") ? new[] { StoryText.Yuzu.Engaged }
                    : StoryText.Yuzu.Back;
                UI.SpeechDialog.Show(_alive.sprite, StoryText.Guide, new[] { alive[_said++ % alive.Length] });
                return;
            }
            var stage = YuzuHeart.Shown;
            string line = stage >= YuzuHeart.Stage.Glow ? StoryText.Yuzu.Wondering
                : stage >= YuzuHeart.Stage.Twitch ? StoryText.Yuzu.Warmer
                : StoryText.Yuzu.AtHerBody[_said++ % StoryText.Yuzu.AtHerBody.Length];
            UI.SpeechDialog.Show(ghost != null ? ghost.GhostFace : null, StoryText.Guide, new[] { line });
        }

        static Sprite SoftLight()
        {
            if (_soft != null) return _soft;
            const int n = 48;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x + 0.5f - n / 2f, y + 0.5f - n / 2f).magnitude / (n / 2f);
                    float a = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Clamp01(d));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return _soft = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
