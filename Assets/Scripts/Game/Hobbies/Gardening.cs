using System;
using DG.Tweening;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Gardening. Dig vegetable plots on your own island (each arrives in a bubble, to put wherever you like), plant seeds bought with a little gold,
    /// and come back when they have grown (real minutes, even while the game is closed). Harvests
    /// go in your bag, for cooking. The more you grow, the more kinds of seed you can plant and the
    /// more plots you can dig.
    /// </summary>
    public class Gardening : MonoBehaviour
    {
        public const int DigWood = 20;
        public static Gardening Instance { get; private set; }

        public static int MaxPlots => 2 + HobbyLog.Level(Hobby.Gardening);

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (Visit.Active) return;   // your plots grow on your island
            for (int i = 0; i < HobbyLog.Plots.Count; i++)
            {
                if (!HobbyLog.Plots[i].unplaced) PlotView.Make(i).Plant(HobbyLog.Plots[i].pos);
                else
                {
                    int index = i;
                    HobbyItems.AfterLoad(this, () => HobbyItems.Offer(PlotView.Make(index).gameObject));
                }
            }
        }

        /// <summary>Makes a new plot and hands it over in a bubble. Returns a reason it could not, or null.</summary>
        public static string Dig()
        {
            if (Visit.Active) return "You can only dig plots on your own island.";
            if (HobbyLog.Plots.Count >= MaxPlots) return $"You have all {MaxPlots} plots your gardening level allows. Level up for more!";
            var store = ResourceStore.Instance;
            if (store == null || !store.TrySpend(DigWood, 0, 0)) return $"A new plot needs {DigWood} wood for its frame.";

            HobbyLog.Plots.Add(new HobbyLog.Plot { unplaced = true });
            HobbyLog.Save();
            HobbyItems.Offer(PlotView.Make(HobbyLog.Plots.Count - 1).gameObject,
                "Here's your new vegetable plot! Pop the bubble and put it somewhere sunny.");
            return null;
        }

        // ---------------------------------------------------------------- one plot

        public class PlotView : MonoBehaviour, ITappable
        {
            int _index;
            SpriteRenderer _soil, _crop;
            Placeable _placeable;
            string _shownStage;
            float _lastY = float.NaN;

            HobbyLog.Plot Data => _index < HobbyLog.Plots.Count ? HobbyLog.Plots[_index] : null;

            /// <summary>The plot as an item: inactive, ready for a bubble or to be planted where it stood.</summary>
            public static PlotView Make(int index)
            {
                var go = HobbyItems.Make("Vegetable plot", HobbyData.Art("Plot"), 1.3f, 0f, 45f, ySort: false);
                var view = go.AddComponent<PlotView>();
                view._index = index;
                view._soil = go.transform.Find("Sprite").GetComponent<SpriteRenderer>();
                view._placeable = go.GetComponent<Placeable>();

                var crop = new GameObject("Crop");
                crop.transform.SetParent(go.transform, false);
                view._crop = crop.AddComponent<SpriteRenderer>();
                view._crop.enabled = false;
                return view;
            }

            /// <summary>Stands a saved plot back where it was.</summary>
            public void Plant(Vector2 at)
            {
                transform.position = new Vector3(at.x, at.y, 0f);
                gameObject.SetActive(true);
                _placeable.MarkPlaced(silent: true);
            }

            void OnPlacedInWorld()
            {
                var d = Data;
                if (d == null) return;
                bool first = d.unplaced;
                d.pos = transform.position;
                d.unplaced = false;
                HobbyLog.Save();
                _shownStage = null;
                Refresh();
                if (!first) return;
                Sfx.PlayAt(SfxKind.Plop, transform.position);
                if (string.IsNullOrEmpty(d.crop)) Hints.Show("Tap your plot to plant some seeds.");
            }

            void OnReturnedToBubble()
            {
                var d = Data;
                if (d == null) return;
                d.unplaced = true;
                HobbyLog.Save();
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
                HobbyLog.Save();
                HobbyLog.AddXp(Hobby.Gardening, 8 + kind.SeedGold / 2);
                Motes.Trigger("hobby_garden");
                var fx = HarvestEffects.Instance;
                if (fx != null) fx.Twinkle(transform.position + Vector3.up * 0.4f, 8);
                Sfx.PlayAt(SfxKind.Rustle, transform.position);
                Hints.Show($"You harvested {kind.Yield} {kind.Name.ToLower()}{(kind.Yield > 1 ? "s" : "")}! They're in your bag.");
                Refresh();
            }
        }
    }
}
