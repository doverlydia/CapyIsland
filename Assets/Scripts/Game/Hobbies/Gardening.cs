using System;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Gardening. Vegetable plots come from the Garden Shop like any other store item: buy one,
    /// put it wherever you like, tap it to plant seeds bought with a little gold, and come back
    /// when they have grown (real minutes, even while the game is closed). Harvests go in your
    /// bag, for cooking at your stove. The more you grow, the more kinds of seed you can plant
    /// and the more plots the shop will sell you.
    ///
    /// Each plot's crop is kept in <see cref="HobbyLog.Plots"/> by where the plot stands, so a
    /// plot and its growing seeds survive saving, loading and the odd bomb (which loses the crop).
    /// </summary>
    public class Gardening : MonoBehaviour
    {
        public const int DigWood = 20;
        public static Gardening Instance { get; private set; }

        /// <summary>How many plots the shop will sell you at your gardening level.</summary>
        public static int MaxPlots => 2 + HobbyLog.Level(Hobby.Gardening);

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (Visit.Active) return;   // your plots grow on your island
            HobbyItems.AfterLoad(this, Migrate);
        }

        /// <summary>
        /// Plots made before they were store items live only in the hobby log: turn each into a real
        /// plot where it stood (or a bubble, if it was never put down). Ones already in the world —
        /// every plot bought from the shop — are found by position and left alone.
        /// </summary>
        void Migrate()
        {
            var catalog = ItemCatalog.Instance;
            if (catalog == null) return;
            var views = FindObjectsByType<PlotView>(FindObjectsSortMode.None);
            for (int i = HobbyLog.Plots.Count - 1; i >= 0; i--)
            {
                var plot = HobbyLog.Plots[i];
                bool standing = false;
                foreach (var v in views)
                    if (v.Data == plot) { standing = true; break; }
                if (standing) continue;
                if (plot.unplaced)
                {
                    HobbyLog.Plots.RemoveAt(i);
                    var king = FindFirstObjectByType<PlayerAvatar>();
                    catalog.MakeStore("Vegetable Plot", king != null ? (Vector2)king.transform.position + new Vector2(1.6f, -0.8f) : plot.pos);
                }
                else catalog.MakeItem("Vegetable Plot", plot.pos);   // finds its record by position
            }
            HobbyLog.Save();
        }

        /// <summary>The crop record for a plot standing at <paramref name="at"/>, if any.</summary>
        static HobbyLog.Plot PlotAt(Vector2 at)
        {
            foreach (var p in HobbyLog.Plots)
                if (!p.unplaced && (p.pos - at).sqrMagnitude < 0.09f) return p;
            return null;
        }

        // ---------------------------------------------------------------- one plot

        public class PlotView : MonoBehaviour, ITappable
        {
            HobbyLog.Plot _data;
            SpriteRenderer _soil, _crop;
            Placeable _placeable;
            string _shownStage;
            float _lastY = float.NaN;

            public HobbyLog.Plot Data => _data;

            void Awake()
            {
                _soil = transform.Find("Sprite").GetComponent<SpriteRenderer>();
                _crop = transform.Find("Crop").GetComponent<SpriteRenderer>();
                _placeable = GetComponent<Placeable>();
            }

            void OnPlacedInWorld()
            {
                if (Visit.Active)
                {
                    // Seeds only grow at home; on a friend's island it is just a pretty bed of soil.
                    _data = null;
                    return;
                }
                Vector2 at = transform.position;
                _data = PlotAt(at);
                bool fresh = _data == null;
                if (fresh)
                {
                    _data = new HobbyLog.Plot { pos = at };
                    HobbyLog.Plots.Add(_data);
                    HobbyLog.Save();
                }
                _shownStage = null;
                Refresh();
                if (fresh && !SaveGame.Restoring)
                {
                    Sfx.PlayAt(SfxKind.Plop, at);
                    Hints.Show("Tap your plot to plant some seeds.");
                }
            }

            /// <summary>Knocked back into a bubble (a bomb): the crop is lost; a new one starts when it is put down.</summary>
            void OnReturnedToBubble()
            {
                if (_data != null)
                {
                    HobbyLog.Plots.Remove(_data);
                    HobbyLog.Save();
                }
                _data = null;
                if (_crop != null) _crop.enabled = false;
            }

            void OnEnable() => TapTargets.Register(this);
            void OnDisable() => TapTargets.Unregister(this);

            void LateUpdate()
            {
                // Soil lies flat on the ground, so it sorts a little behind things at the same depth.
                float y = transform.position.y;
                if (!float.IsNaN(_lastY) && Mathf.Abs(y - _lastY) < 0.005f) return;
                _lastY = y;
                int order = YSorter.PropSortBase + Mathf.RoundToInt(-y * 100f);
                _soil.sortingOrder = _placeable.Placed ? order - 50 : order;
                _crop.sortingOrder = _soil.sortingOrder + 1;
            }

            float Progress
            {
                get
                {
                    var d = Data;
                    if (d == null || string.IsNullOrEmpty(d.crop)) return 0f;
                    var kind = HobbyData.Crop(d.crop);
                    double minutes = (DateTime.Now - new DateTime(d.plantedTicks)).TotalMinutes;
                    return kind == null ? 1f : Mathf.Clamp01((float)(minutes / kind.Minutes));
                }
            }

            void Update()
            {
                // A few times a second is plenty for something that grows over minutes.
                if (Time.frameCount % 15 == 0) Refresh();
            }

            void Refresh()
            {
                var d = Data;
                if (d == null || !_placeable.Placed) return;
                _lastY = float.NaN;
                bool planted = !string.IsNullOrEmpty(d.crop);
                float p = Progress;
                string stage = !planted ? "empty" : p >= 1f ? "ready" : "growing";
                var cropT = _crop.transform;
                if (stage == "growing")
                {
                    if (_shownStage != "growing") _crop.sprite = HobbyData.Art("Sprout");
                    float s = Mathf.Lerp(0.35f, 0.75f, p);
                    cropT.localScale = Vector3.one * s;
                }
                if (stage == _shownStage) return;
                _shownStage = stage;
                _crop.enabled = planted;
                if (stage == "ready")
                {
                    _crop.sprite = HobbyData.Art("Crop_" + d.crop);
                    cropT.localScale = Vector3.one * 0.7f;
                    cropT.localPosition = new Vector3(0f, 0.15f, 0f);
                    cropT.DOKill();
                    cropT.DOLocalMoveY(0.3f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                }
                else
                {
                    cropT.DOKill();
                    cropT.localPosition = new Vector3(0f, 0.1f, 0f);
                }
            }

            public float TapDistance(Vector2 worldPoint)
            {
                if (_soil == null || !_placeable.Placed) return -1f;
                var b = _soil.bounds;
                return b.Contains(new Vector3(worldPoint.x, worldPoint.y, b.center.z)) ? Vector2.Distance(b.center, worldPoint) : -1f;
            }

            public void Tap()
            {
                var d = Data;
                if (d == null) return;
                if (string.IsNullOrEmpty(d.crop)) { ChooseSeed(); return; }
                var kind = HobbyData.Crop(d.crop);
                float p = Progress;
                if (p < 1f)
                {
                    double left = kind.Minutes * (1f - p);
                    string when = left < 1 ? "less than a minute" : $"about {Math.Ceiling(left)} min";
                    Hints.Show($"Your {kind.Name.ToLower()} will be ready in {when}.");
                    return;
                }
                Harvest(kind);
            }

            void ChooseSeed()
            {
                int level = HobbyLog.Level(Hobby.Gardening);
                var store = ResourceStore.Instance;
                var choices = new System.Collections.Generic.List<UI.SpeechDialog.Choice>();
                foreach (var c in HobbyData.Crops)
                {
                    if (c.MinLevel > level) continue;
                    var kind = c;
                    bool afford = store != null && store.Get(ResourceKind.Gold) >= c.SeedGold;
                    choices.Add(new UI.SpeechDialog.Choice($"{c.Name} ({c.SeedGold} gold)", () => Plant(kind), afford));
                    if (choices.Count >= 4) break;
                }
                choices.Add(new UI.SpeechDialog.Choice("Not now", null));
                UI.SpeechDialog.ShowChoices(HobbyData.Art("Plot"), "Vegetable plot",
                    new[] { "What would you like to plant? Seeds grow while you're away, too." }, null, choices);
            }

            void Plant(CropKind kind)
            {
                var store = ResourceStore.Instance;
                if (store == null || !store.TrySpend(0, kind.SeedGold, 0)) return;
                var d = Data;
                d.crop = kind.Id;
                d.plantedTicks = DateTime.Now.Ticks;
                HobbyLog.Save();
                Sfx.PlayAt(SfxKind.Plop, transform.position);
                transform.DOPunchScale(Vector3.one * 0.08f, 0.3f, 6, 0.6f);
                Refresh();
                Hints.Show($"{kind.Name} seeds planted! Ready in {kind.Minutes:0} min.");
            }

            void Harvest(CropKind kind)
            {
                var d = Data;
                d.crop = null;
                HobbyLog.AddToBag("crop:" + kind.Id, kind.Yield, save: false);
                bool first = HobbyLog.LogCrop(kind.Id, kind.Yield);   // the vegetable journal (and saves)
                HobbyLog.AddXp(Hobby.Gardening, 8 + kind.SeedGold / 2);
                Motes.Trigger("hobby_garden");
                var fx = HarvestEffects.Instance;
                if (fx != null) fx.Twinkle(transform.position + Vector3.up * 0.4f, 8);
                Sfx.PlayAt(SfxKind.Rustle, transform.position);
                Hints.Show($"You harvested {kind.Yield} {kind.Name.ToLower()}{(kind.Yield > 1 ? "s" : "")}! They're in your bag.");
                if (first) Hints.Show(UI.Loc.F("New in your vegetable journal: {0}!", UI.Loc.T(kind.Name)));
                Refresh();
            }
        }
    }
}
