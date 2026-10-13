using System;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A glowing bubble on the ground holding an item, drawn inside the glass so you can see what
    /// you are about to get. Tap it and the item joins your inventory trail. These are how the
    /// island is seeded — you start with nothing — and how a felled forest or eaten garden comes
    /// back to be replanted.
    ///
    /// In the original these are survivors Grandpa Angel sealed away before the disaster.
    /// </summary>
    public class Bubble : MonoBehaviour, ITappable, IMapMarker
    {
        [Header("Contents")]
        [Tooltip("Item held inside, inactive in the scene until collected.")]
        [SerializeField] GameObject contents;

        [Header("Look")]
        [SerializeField] float bobHeight = 0.18f;
        [SerializeField] float bobRate = 1.1f;

        [Tooltip("Largest size of the item picture inside the glass, in world units.")]
        [SerializeField] float iconSize = 1.6f;

        [Header("Interaction")]
        [SerializeField] float tapRadius = 1.7f;

        /// <summary>Raised when collected, with the item that came out.</summary>
        public static event Action<GameObject, Bubble> Collected;

        Transform _body;
        Vector3 _bodyRest;
        float _bodyScale = 1f;
        bool _taken;
        SpriteRenderer _icon;

        // A timed bubble (a sapling or random item shaken from a fruit tree) pops if left alone.
        FrameClock _clock;
        int _lifeFrames = -1;
        int _age;

        public bool Taken => _taken;
        public float TapRadius => tapRadius;
        public GameObject Contents => contents;

        void OnEnable() => MapMarkers.Register(this);
        void OnDisable() => MapMarkers.Unregister(this);

        public MarkerKind MarkerKind => MarkerKind.Bubble;
        public bool MarkerActive => !_taken && isActiveAndEnabled;
        public Vector2 MarkerPosition => transform.position;
        public Sprite MarkerIcon => _icon != null ? _icon.sprite : MapMarkers.SpriteOf(contents);

        void Awake()
        {
            var sprite = transform.Find("Sprite");
            _body = sprite != null ? sprite : transform;
            _bodyRest = _body.localPosition;
            _bodyScale = _body.localScale.x;
            // Bubbles built before the arrow was dropped still carry one: the bobbing bubble and
            // the edge arrows say "look here" well enough on their own.
            var arrow = transform.Find("Arrow");
            if (arrow != null) arrow.gameObject.SetActive(false);
            FloatAbove();
            RefreshIcon();
        }

        /// <summary>Above every prop, below speech bubbles and markers; nearer bubbles in front.</summary>
        public const int SortBase = 25000;

        /// <summary>
        /// A bubble floats: it draws over buildings and trees rather than being sorted in among
        /// them, so one dropped just behind a house is never hidden by it.
        /// </summary>
        void FloatAbove()
        {
            var glass = _body != null ? _body.GetComponent<SpriteRenderer>() : null;
            if (glass == null) return;
            glass.sortingOrder = SortBase + Mathf.Clamp(Mathf.RoundToInt(-transform.position.y * 10f), -1900, 1900) * 2;
        }

        // A press on a bubble belongs to the bubble, whatever is standing behind it.
        public float TapDistance(Vector2 worldPoint)
        {
            if (_taken || !isActiveAndEnabled) return -1f;
            float d = Vector2.Distance(transform.position, worldPoint);
            return d <= tapRadius ? d : -1f;
        }

        public void Tap() => Collect();

        /// <summary>
        /// Shows the item inside the glass. Sits just behind the bubble sprite, so the glass rim and
        /// highlight draw over it, and rides the body so it bobs with the bubble.
        /// </summary>
        void RefreshIcon()
        {
            if (_body == null || contents == null) return;

            var source = contents.transform.Find("Sprite");
            var sourceRenderer = source != null ? source.GetComponent<SpriteRenderer>() : null;
            if (sourceRenderer == null || sourceRenderer.sprite == null) return;

            if (_icon == null)
            {
                var existing = _body.Find("Icon");
                var go = existing != null ? existing.gameObject : new GameObject("Icon");
                go.transform.SetParent(_body, false);
                _icon = go.GetComponent<SpriteRenderer>();
                if (_icon == null) _icon = go.AddComponent<SpriteRenderer>();
            }

            _icon.sprite = sourceRenderer.sprite;

            // Over the glass: under it, the bubble's tinted fill washed the item out to a ghost.
            var glass = _body.GetComponent<SpriteRenderer>();
            if (glass != null) _icon.sortingOrder = glass.sortingOrder + 1;

            // Fit the picture inside the glass whatever its native size, centred on the bubble
            // (sprites pivot at their feet, so pull it down by half its height).
            var size = sourceRenderer.sprite.bounds.size;
            float largest = Mathf.Max(0.01f, Mathf.Max(size.x, size.y));
            float bodyScale = Mathf.Max(0.01f, Mathf.Abs(_body.lossyScale.x));
            float s = iconSize / largest / bodyScale;
            _icon.transform.localScale = Vector3.one * s;
            _icon.transform.localPosition = new Vector3(0f, -sourceRenderer.sprite.bounds.center.y * s, 0f);
        }

        /// <summary>
        /// Pops this bubble, contents and all, if not collected within <paramref name="frames"/>
        /// 30 Hz frames — the original's timed store bubbles.
        /// </summary>
        public void ExpireAfterFrames(int frames)
        {
            _lifeFrames = frames;
            _age = 0;
        }

        float _bornAt = -1f;
        bool _nudged;

        void Update()
        {
            if (_taken) return;

            // Left alone for 30 seconds: the original's "free item" nudge (after the tutorial).
            if (_bornAt < 0f) _bornAt = Time.time;
            if (!_nudged && Time.time - _bornAt > 30f && contents != null)
            {
                if (!Attention.TutorialRunning)
                {
                    _nudged = true;
                    var tag = contents.GetComponent<CatalogItem>();
                    string line = "You found a free <b>" + (tag != null ? StoryText.Name(tag.StoreName) : contents.name) + "</b>! Tap the bubble to pick it up.";
                    // Waits its turn, and is dropped unsaid if the bubble is popped first.
                    bool StillHere() => this != null && !_taken;
                    Attention.Ask("nudge:" + GetInstanceID(), () => Hints.Show(line, StillHere), StillHere);
                }
            }

            if (_lifeFrames >= 0)
            {
                _age += _clock.Consume(Time.deltaTime);
                if (_age > _lifeFrames) { Pop(); return; }

                // The last two seconds, it wobbles to warn it is about to go.
                if (_lifeFrames - _age < 60 && _body != null)
                    _body.localScale = Vector3.one * _bodyScale * (1f + Mathf.Sin(Time.time * 30f) * 0.05f);
            }

            float t = Time.time;
            if (_body != null)
                _body.localPosition = _bodyRest + new Vector3(0f, Mathf.Sin(t * bobRate * Mathf.PI * 2f) * bobHeight, 0f);
        }

        /// <summary>Fills a bubble created at runtime, for an item returning to the world.</summary>
        public void Fill(GameObject item)
        {
            contents = item;
            _taken = false;
            Awake();   // re-cache the body and the icon
        }

        /// <summary>Nearest uncollected bubble under a world point, or null.</summary>
        public static Bubble Pick(Vector2 worldPoint)
        {
            Bubble best = null;
            float bestDistance = float.MaxValue;
            foreach (var b in FindObjectsByType<Bubble>(FindObjectsSortMode.None))
            {
                if (b == null || b._taken || !b.isActiveAndEnabled) continue;
                float d = Vector2.Distance(b.transform.position, worldPoint);
                if (d > b.tapRadius || d >= bestDistance) continue;
                best = b;
                bestDistance = d;
            }
            return best;
        }

        /// <summary>Pops the bubble and hands over its contents, already in the inventory trail.</summary>
        public GameObject Collect()
        {
            if (_taken) return null;
            _taken = true;

            var item = contents;
            if (item != null)
            {
                item.transform.position = transform.position;
                item.SetActive(true);

                // Straight into the trail: picked-up items follow you until you place them.
                var placeable = item.GetComponent<Placeable>();
                if (placeable != null) placeable.MarkCarried();
                else
                {
                    var follower = item.GetComponent<Follower>();
                    if (follower != null) follower.PickUpIntoInventory();
                }
            }

            Sfx.PlayAt(SfxKind.Pop, transform.position);
            Collected?.Invoke(item, this);
            Purchasing.Collected(item);
            gameObject.SetActive(false);
            return item;
        }

        /// <summary>Gone unclaimed: the bubble bursts and takes its contents with it.</summary>
        public void Pop()
        {
            if (_taken) return;
            _taken = true;
            Sfx.PlayAt(SfxKind.Pop, transform.position, 0.6f, 0.2f);
            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Twinkle(transform.position + Vector3.up * 0.6f, 6);
            if (contents != null) Destroy(contents);
            Destroy(gameObject);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 1f, 0.9f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, tapRadius);
        }
    }
}
