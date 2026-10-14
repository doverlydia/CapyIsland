using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// The Tower of Love (MapItems/Tower). It fills with light as bits of love are found; tap it to
    /// read the diary — how many are left, and how hopeful the island feels. Every heart you tap
    /// flies here and adds 50 to your score. Once every heart is home, the island is saved and the
    /// Tower drops a little chest of wood or gold every second.
    /// </summary>
    public class TowerOfLove : MonoBehaviour, ITappable, IMapMarker
    {
        public static TowerOfLove Instance { get; private set; }

        /// <summary>Raised when the diary is closed — the tutorial's last step.</summary>
        public static event Action DiaryClosed;

        public static int Score { get; set; }

        SpriteRenderer _bodyRenderer;
        SpriteRenderer _glow;
        FrameClock _clock;
        int _rewardCounter;

        HotSpring _spring;

        void Awake()
        {
            var body = transform.Find("Sprite");
            _bodyRenderer = body != null ? body.GetComponent<SpriteRenderer>() : null;
            _spring = gameObject.AddComponent<HotSpring>();   // the old hot spring at its foot
        }

        const string CentredKey = "CapyIsland.TowerCentred";

        /// <summary>
        /// The tower belongs in the middle of its island. Once per game it moves to the nearest
        /// clear spot to the middle (older games planted it wherever there was room).
        /// </summary>
        IEnumerator Start()
        {
            yield return null;
            yield return null;   // the save puts things back first
            if (PlayerPrefs.GetInt(CentredKey, 0) == 1) yield break;
            var placeable = GetComponent<Placeable>();
            var land = LandMap.Instance;
            if (placeable == null || !placeable.Placed || land == null) yield break;
            int island = land.IslandAt(transform.position);
            if (island < 0) yield break;
            var spot = land.SpotNearIslandMiddle(island, 83f, placeable.CanPlaceAt);
            transform.position = new Vector3(spot.x, spot.y, transform.position.z);
            PlayerPrefs.SetInt(CentredKey, 1);
            PlayerPrefs.Save();
        }

        void OnEnable()
        {
            Instance = this;
            TapTargets.Register(this);
            MapMarkers.Register(this);
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            TapTargets.Unregister(this);
            MapMarkers.Unregister(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            DiaryClosed = null;
            Score = 0;
        }

        public MarkerKind MarkerKind => MarkerKind.Tower;
        public bool MarkerActive => true;
        public Vector2 MarkerPosition => transform.position;
        public Sprite MarkerIcon => _bodyRenderer != null ? _bodyRenderer.sprite : null;

        public bool IslandSaved => Motes.CollectedCount >= Motes.Total && YuzuHeart.Married;

        void Update()
        {
            // Glows warmer as more of the island's love is found.
            if (_bodyRenderer != null)
            {
                float filled = Motes.Total > 0 ? (float)Motes.CollectedCount / Motes.Total : 0f;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 2f);
                _bodyRenderer.color = Color.Lerp(new Color(0.8f, 0.8f, 0.85f), Color.white, 0.4f + 0.6f * filled)
                                      + new Color(0.08f, 0.02f, 0.05f, 0f) * pulse * filled;
            }

            if (!IslandSaved) return;
            int frames = _clock.Consume(Time.deltaTime);
            for (int i = 0; i < frames; i++)
            {
                if (++_rewardCounter < 30) continue;
                _rewardCounter = 0;
                var catalog = ItemCatalog.Instance;
                var spot = (Vector2)transform.position + BunniSpace.ToUnity(UnityEngine.Random.insideUnitCircle * 90f + new Vector2(0f, -60f));
                // One a second, for good: quietly, or the island never stops dinging.
                TreasureChest.Spawn(spot, 4, catalog != null ? catalog.ItemParent : transform.parent).Silent = true;
            }
        }

        /// <summary>A heart arrived (TowerItem.MoteFound).</summary>
        public void MoteFound(string moteName)
        {
            Motes.MarkClicked(moteName);
            Score += 50;
            var fx = HarvestEffects.Instance;
            if (fx != null) fx.Twinkle(TopOfTower, 10);
            var body = transform.Find("Sprite");
            if (body != null) body.DOPunchScale(new Vector3(0.08f, 0.12f, 0f), 0.4f, 6, 0.6f);
        }

        public Vector2 TopOfTower => _bodyRenderer != null
            ? new Vector2(_bodyRenderer.bounds.center.x, _bodyRenderer.bounds.max.y - 0.3f)
            : (Vector2)transform.position + Vector2.up * 2f;

        public float TapDistance(Vector2 worldPoint)
        {
            if (_bodyRenderer == null) return -1f;
            if (_spring != null && _spring.PoolContains(worldPoint)) return Vector2.Distance(_spring.PoolBounds.center, worldPoint);
            var b = _bodyRenderer.bounds;
            if (!b.Contains(new Vector3(worldPoint.x, worldPoint.y, b.center.z))) return -1f;
            return Vector2.Distance(b.center, worldPoint);
        }

        /// <summary>Opens the goals list on its achievements, with how the island is feeling on top.</summary>
        public void Tap()
        {
            // The same count as the achievements list below it: one number, never two.
            int total = Motes.Total, found = Motes.CollectedCount;
            int percent = total > 0 ? Mathf.RoundToInt(100f * found / total) : 0;
            string mood = percent < 20 ? "The island is still empty and quiet"
                        : percent < 40 ? "Little sparks of life are popping up"
                        : percent < 60 ? "The island is waking up"
                        : percent < 80 ? "The island is busy, bright and humming"
                        : "The whole island is glowing";
            string left = total - found == 0 ? "Every heart is home."
                        : total - found == 1 ? "One heart still to bring home."
                        : $"{total - found} hearts still to bring home.";
            string banner = $"<b>{mood}.</b> {left}";
            // A soak in the spring first; the page opens while the capybara relaxes in the water,
            // and closing it sends the capybara climbing back out.
            void Open(Action climbOut) => UI.ObjectivesScreen.Show(UI.ObjectivesScreen.Tab.Achievements, banner,
                () => { DiaryClosed?.Invoke(); climbOut?.Invoke(); });
            if (_spring != null) _spring.Visit(Open);
            else Open(null);
        }
    }
}
