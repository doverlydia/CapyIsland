using System;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Gardening. The plots are on Sunflower Farm (<see cref="Farm"/>): tap one to plant seeds
    /// bought with a little gold, and come back when they have grown (real minutes, even while the
    /// game is closed). Harvests go in your bag, for cooking at the farm's stove. The more you grow,
    /// the more kinds of seed you can plant and the more of the farm's plots open up.
    ///
    /// Each plot's crop is kept in <see cref="HobbyLog.Plots"/> by where the plot stands, so its
    /// growing seeds survive saving and loading.
    /// </summary>
    public class Gardening : MonoBehaviour
    {
        public const int DigWood = 20;
        public static Gardening Instance { get; private set; }

        /// <summary>How many of the farm's plots are open at your gardening level.</summary>
        public static int MaxPlots => 2 + HobbyLog.Level(Hobby.Gardening);

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
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

            /// <summary>Its place in the farm's field (0 = first), set by <see cref="Farm"/>.</summary>
            [System.NonSerialized] public int FarmIndex = -1;

            /// <summary>Beyond what your gardening level allows: roped off until you grow into it.</summary>
            public bool Locked => FarmIndex >= MaxPlots;

            /// <summary>The gardening level that opens this plot.</summary>
            public int OpensAt => Mathf.Max(1, FarmIndex - 1);

            SpriteRenderer _lock;

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
                if (fresh && !SaveGame.Restoring && !Farm.LayingOut)
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

            /// <summary>A roped-off plot: dimmed soil and a padlock until your level reaches it.</summary>
            void ShowLock()
            {
                bool locked = Locked;
                if (locked && _lock == null)
                {
                    var go = new GameObject("Lock");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                    _lock = go.AddComponent<SpriteRenderer>();
                    _lock.sprite = UI.IconPainter.Lock;
                    _lock.color = new Color(1f, 0.97f, 0.9f, 0.95f);   // cream, to stand out on the dark soil
                    float size = 0.75f / Mathf.Max(0.01f, _lock.sprite.bounds.size.y * transform.lossyScale.y);
                    go.transform.localScale = Vector3.one * size;
                }
                if (_lock != null)
                {
                    _lock.enabled = locked;
                    _lock.sortingOrder = _soil.sortingOrder + 2;
                }
                _soil.color = locked ? new Color(0.72f, 0.68f, 0.64f) : Color.white;
            }

            void Refresh()
            {
                var d = Data;
                if (d == null || !_placeable.Placed) return;
                if (FarmIndex >= 0) ShowLock();
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
                if (Locked && string.IsNullOrEmpty(d.crop))
                {
                    Hints.Show(WhyNot);
                    return;
                }
                if (string.IsNullOrEmpty(d.crop))
                {
                    Hints.Show("Buy seeds at the barn, then drag them from your trail onto an empty plot.");
                    return;
                }
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

            /// <summary>Open, and nothing growing: a seed packet can go here.</summary>
            public bool CanPlant => FarmIndex >= 0 && !Locked && Data != null && string.IsNullOrEmpty(Data.crop) && _placeable.Placed;

            /// <summary>Why a packet can't go here, or null if it can.</summary>
            public string WhyNot => Locked ? string.Format("This plot opens at gardening level {0}.", OpensAt)
                                  : Data != null && !string.IsNullOrEmpty(Data.crop) ? "Something is already growing there."
                                  : null;

            /// <summary>The farm plot under a point (a dropped packet), or null.</summary>
            public static PlotView Under(Vector2 at)
            {
                PlotView best = null;
                float bestDistance = 1.1f;
                foreach (var v in FindObjectsByType<PlotView>(FindObjectsSortMode.None))
                {
                    if (v.FarmIndex < 0) continue;
                    float d = Vector2.Distance(v._soil != null ? (Vector2)v._soil.bounds.center : (Vector2)v.transform.position, at);
                    if (d < bestDistance) { best = v; bestDistance = d; }
                }
                return best;
            }

            /// <summary>A packet of seeds dropped on this plot (paid for at the barn).</summary>
            public void Plant(CropKind kind)
            {
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
