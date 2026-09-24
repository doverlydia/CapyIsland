using System;
using System.Text;
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
    public class TowerOfLove : MonoBehaviour, ITappable
    {
        public static TowerOfLove Instance { get; private set; }

        /// <summary>Raised when the diary is closed — the tutorial's last step.</summary>
        public static event Action DiaryClosed;

        public static int Score { get; set; }

        SpriteRenderer _bodyRenderer;
        SpriteRenderer _glow;
        FrameClock _clock;
        int _rewardCounter;

        void Awake()
        {
            var body = transform.Find("Sprite");
            _bodyRenderer = body != null ? body.GetComponent<SpriteRenderer>() : null;
        }

        void OnEnable()
        {
            Instance = this;
            TapTargets.Register(this);
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            TapTargets.Unregister(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            DiaryClosed = null;
            Score = 0;
        }

        public bool IslandSaved => Motes.ClickedCount >= Motes.Total;

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
                TreasureChest.Spawn(spot, 4, catalog != null ? catalog.ItemParent : transform.parent);
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
            var b = _bodyRenderer.bounds;
            if (!b.Contains(new Vector3(worldPoint.x, worldPoint.y, b.center.z))) return -1f;
            return Vector2.Distance(b.center, worldPoint);
        }

        public void Tap()
        {
            int total = Motes.Total, clicked = Motes.ClickedCount;
            int percent = total > 0 ? Mathf.RoundToInt(100f * clicked / total) : 0;
            string mood = percent < 20 ? $"{percent}% away from certain death"
                        : percent < 40 ? $"{percent}% hopeful that life has meaning"
                        : percent < 60 ? $"{percent}% determined to rebuild what was lost"
                        : percent < 80 ? $"{percent}% delighted to be bustling and moving forward"
                        : $"{percent}% convinced that Love is eternal";

            var found = new StringBuilder();
            foreach (var m in QuestData.Motes)
                if (Motes.IsClicked(m.Name)) found.Append("♥ ").Append(m.Name).Append("   ");

            var pages = new[]
            {
                $"<b>Dear Diary</b>\nToday I am {mood}.\n\nBits of love left to collect ({total - clicked}).",
                clicked > 0 ? "Bits of love found so far:\n" + found : "No bits of love have come home yet. Tap the hearts that follow you to send them here.",
            };
            var face = _bodyRenderer != null ? _bodyRenderer.sprite : null;
            UI.SpeechDialog.Show(face, "Tower of Love", pages, () => DiaryClosed?.Invoke());
        }
    }
}
