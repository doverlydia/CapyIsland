using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Chef Tofu's kitchen stall on the Heart of the Isles, the hot spring's island, there from the
    /// start. His wares stand out front like a shop's: the stove (the way into cooking), packs of
    /// what you can't grow or catch (eggs, milk, flour, rice, sugar) and his recipe book. Tap the
    /// book, or him, for the recipes he sells; the ones above your cooking level wait, greyed.
    ///
    /// Where the stall stands is chosen once, on clear land near the island's middle, and
    /// remembered, so it never wanders as the island fills up.
    /// </summary>
    public class ChefTofu : MonoBehaviour
    {
        const int IslandIndex = 1;   // the Heart of the Isles (see SeaLibrary)
        const string SpotKey = "CapyIsland.ChefSpot";
        const float StallHeight = 3.4f, ChefHeight = 1.7f;

        public static ChefTofu Instance { get; private set; }

        /// <summary>Where to send you for the chef (his stall's wares), once it stands.</summary>
        public static Vector2? Spot => Instance != null && Instance._root != null ? (Vector2)Instance._root.transform.position + new Vector2(0f, -1.8f) : null;

        GameObject _root;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        IEnumerator Start()
        {
            if (Visit.Active) yield break;   // a friend's island has its own way of doing things
            // After the island and its items have loaded, so the stall finds land that's clear.
            yield return null;
            yield return null;
            Build();
        }

        void Build()
        {
            var land = LandMap.Instance;
            if (land == null || land.Island(IslandIndex) == null) return;
            Vector2 at;
            var saved = PlayerPrefs.GetString(SpotKey, "");
            var parts = saved.Split(',');
            if (parts.Length == 2 && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x)
                && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y))
                at = new Vector2(x, y);
            else
            {
                at = FindSpot(land);
                PlayerPrefs.SetString(SpotKey, at.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," + at.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
            }

            _root = new GameObject("ChefTofusKitchen");
            var catalog = ItemCatalog.Instance;
            if (catalog != null) _root.transform.SetParent(catalog.ItemParent, false);
            _root.transform.position = at;
            Stall(at);
            MakeChef(at + new Vector2(2.3f, -0.5f));
            Book(BookSpot(at));
            LayOutWares(at);
        }

        /// <summary>His recipe book, open on its stand beside the stall (the chef is on the other side).</summary>
        static Vector2 BookSpot(Vector2 stall) => stall + new Vector2(-2.9f, 0.1f);

        // ---------------------------------------------------------------- where

        /// <summary>Everything the stall needs to stand on, around its feet.</summary>
        static IEnumerable<Vector2> Footprint(Vector2 at)
        {
            for (float dx = -2.2f; dx <= 2.21f; dx += 1.1f)
                for (float dy = 0f; dy <= 2.41f; dy += 1.2f) yield return at + new Vector2(dx, dy);   // the stall, up to its awning
            yield return at + new Vector2(2.3f, -0.5f);   // the chef
            yield return BookSpot(at);
            foreach (var w in WareSpots(at)) yield return w;
        }

        /// <summary>The nearest clear spot to the island's middle with room for the stall, the chef and his wares.</summary>
        static Vector2 FindSpot(LandMap land)
        {
            var taken = new List<Vector2>();
            foreach (var p in FindObjectsByType<Placeable>(FindObjectsSortMode.None)) if (p.Placed) taken.Add(p.transform.position);
            foreach (var h in FindObjectsByType<Harvestable>(FindObjectsSortMode.None)) taken.Add(h.transform.position);
            if (SeaLibrary.Spot != null) taken.Add(SeaLibrary.Spot.position);
            // The Steam Tower, its pool and Old Yuzu by it aren't items, but they stand their ground too.
            var spring = new List<Bounds>();
            if (HotSpring.Instance != null)
                foreach (var r in HotSpring.Instance.GetComponentsInChildren<SpriteRenderer>())
                {
                    var b = r.bounds;
                    b.Expand(new Vector3(2.4f, 2.4f, 0f));
                    spring.Add(b);
                }
            bool Fits(Vector2 at)
            {
                foreach (var f in Footprint(at))
                {
                    if (!BunniSpace.PointOnLand(f) || land.IslandAt(f) != IslandIndex) return false;
                    foreach (var t in taken) if ((t - f).sqrMagnitude < 1.7f * 1.7f) return false;
                    foreach (var b in spring) if (b.Contains(new Vector3(f.x, f.y, b.center.z))) return false;
                }
                return true;
            }
            // On the right-hand side of the island, where there's room: the clear spot furthest
            // east, keeping to the middle of the island north to south.
            var island = land.Island(IslandIndex);
            Vector2 lo = new Vector2(float.MaxValue, float.MaxValue), hi = -lo;
            void Grow(Vector2 world) { var u = LandMap.WorldToUnity(world); lo = Vector2.Min(lo, u); hi = Vector2.Max(hi, u); }
            foreach (var c in island.circles) { Grow(new Vector2(c.x - c.z, c.y - c.z)); Grow(new Vector2(c.x + c.z, c.y + c.z)); }
            foreach (var r in island.rects) { Grow(r.min); Grow(r.max); }
            float midY = (lo.y + hi.y) * 0.5f;
            Vector2? best = null;
            float bestScore = float.MinValue;
            for (float x = lo.x; x <= hi.x; x += 0.8f)
                for (float y = lo.y; y <= hi.y; y += 0.8f)
                {
                    var p = new Vector2(x, y);
                    float score = x - Mathf.Abs(y - midY) * 0.6f;
                    if (score <= bestScore || !Fits(p)) continue;
                    best = p;
                    bestScore = score;
                }
            return best ?? land.SpotNearIslandMiddle(IslandIndex, 60f, Fits);
        }

        // ---------------------------------------------------------------- the stall

        void Stall(Vector2 feet)
        {
            var sprite = HobbyData.Art("Chef_Stall");
            if (sprite == null) return;
            var go = new GameObject("Chef_Stall");
            go.transform.SetParent(_root.transform, false);
            go.transform.position = feet;
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            float scale = StallHeight / Mathf.Max(0.01f, sprite.bounds.size.y);
            body.transform.localScale = Vector3.one * scale;
            body.transform.localPosition = new Vector3(0f, StallHeight * 0.5f, 0f);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            go.AddComponent<YSorter>();
            go.AddComponent<OpensRecipes>().Init(sr);   // tap the stall for his recipes
            // Holds its ground: nothing can be put down on top of it.
            var placeable = go.AddComponent<Placeable>();
            placeable.Configure("Chef_Stall", 55f, 55f);
            placeable.MarkPlaced(silent: true);
        }

        void MakeChef(Vector2 at)
        {
            var art = HobbyData.Art("Chef");
            if (art == null) return;
            var go = new GameObject("ChefTofu");
            go.transform.SetParent(_root.transform, false);
            go.transform.position = at;
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = art;
            sr.color = new Color(1f, 1f, 1f, 0.9f);   // a spirit, like Old Mochi: not quite all there
            body.transform.localPosition = new Vector3(0f, art.bounds.extents.y - art.bounds.center.y, 0f);
            go.transform.localScale = Vector3.one * (ChefHeight / Mathf.Max(0.01f, art.bounds.size.y));
            go.AddComponent<YSorter>();
            go.AddComponent<Chef>().Init(sr);
        }

        void Book(Vector2 feet)
        {
            var art = HobbyData.Art("Recipe_Book");
            if (art == null) return;
            var go = new GameObject("RecipeBook");
            go.transform.SetParent(_root.transform, false);
            go.transform.position = feet;
            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = art;
            float scale = 1.3f / Mathf.Max(0.01f, art.bounds.size.x);
            body.transform.localScale = Vector3.one * scale;
            body.transform.localPosition = new Vector3(0f, art.bounds.size.y * scale * 0.5f, 0f);
            go.AddComponent<YSorter>();
            go.AddComponent<OpensRecipes>().Init(sr);
        }

        /// <summary>His wares for sale in an arc in front of the stall, like a shop's: the stove and the pantry.</summary>
        static List<Vector2> WareSpots(Vector2 stall)
        {
            var spots = new List<Vector2>();
            int count = 1 + HobbyData.Pantry.Length;
            var middle = stall + new Vector2(0f, -0.4f);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float a = Mathf.Lerp(-72f, 72f, t) * Mathf.Deg2Rad;
                var offset = new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * 3.1f;
                offset.y *= 0.65f;   // seen from above at an angle: circles are squashed
                spots.Add(middle + offset);
            }
            return spots;
        }

        void LayOutWares(Vector2 stall)
        {
            var spots = WareSpots(stall);
            SaleDisplay.Create("Stove", spots[0], _root.transform);
            for (int i = 0; i < HobbyData.Pantry.Length; i++)
            {
                var p = HobbyData.Pantry[i];
                BaitStand.Create("Pantry_" + p.Id, p.Name, p.Blurb, new Vector3Int(0, p.Gold, 0),
                    HobbyData.ItemArt(p.Item), () => BuyPantry(p), spots[1 + i], _root.transform,
                    // only any use once you can cook
                    () => HobbyLog.IsUnlocked(Hobby.Cooking) ? null : "You need a stove first. Chef Tofu sells one right here.");
            }
        }

        static void OpenRecipes()
        {
            if (UI.RecipeShopScreen.Instance != null) UI.RecipeShopScreen.Instance.Open();
        }

        // ---------------------------------------------------------------- buying

        /// <summary>A pack of a pantry good, straight into the bag.</summary>
        public static bool BuyPantry(PantryKind p)
        {
            var store = ResourceStore.Instance;
            if (p == null || store == null || !store.TrySpend(0, p.Gold, 0)) return false;
            HobbyLog.AddToBag(p.Item, p.Pack);
            Sfx.Play(SfxKind.Buy, 0.6f);
            return true;
        }

        /// <summary>Why a recipe can't be bought right now, or null if it can.</summary>
        public static string WhyNot(DishKind d)
        {
            if (HobbyLog.KnowsRecipe(d.Id)) return "Already in your recipe book";
            if (d.MinLevel > HobbyLog.Level(Hobby.Cooking)) return UI.Loc.F("Requires cooking level {0}", d.MinLevel);
            return null;
        }

        public static bool BuyRecipe(DishKind d)
        {
            var store = ResourceStore.Instance;
            if (d == null || WhyNot(d) != null || store == null || !store.TrySpend(0, d.RecipeGold, 0)) return false;
            HobbyLog.LearnRecipe(d.Id);
            Sfx.Play(SfxKind.Buy, 0.7f);
            return true;
        }

        /// <summary>His stall and his recipe book: tap either for the recipes he sells.</summary>
        class OpensRecipes : MonoBehaviour, ITappable
        {
            SpriteRenderer _sr;

            public void Init(SpriteRenderer sr) => _sr = sr;

            void OnEnable() => TapTargets.Register(this);
            void OnDisable() => TapTargets.Unregister(this);

            public float TapDistance(Vector2 worldPoint)
            {
                if (_sr == null) return -1f;
                var b = _sr.bounds;
                return b.Contains(new Vector3(worldPoint.x, worldPoint.y, b.center.z)) ? Vector2.Distance(b.center, worldPoint) : -1f;
            }

            public void Tap()
            {
                Sfx.Play(SfxKind.Tap, 0.5f);
                OpenRecipes();
            }
        }

        /// <summary>Chef Tofu: a word, then his recipes. A spirit, he bobs gently where he floats.</summary>
        class Chef : MonoBehaviour, ITappable
        {
            SpriteRenderer _sr;
            float _restY;

            public void Init(SpriteRenderer sr)
            {
                _sr = sr;
                _restY = sr.transform.localPosition.y;
            }

            void Update()
            {
                if (_sr == null) return;
                var body = _sr.transform;
                body.localPosition = new Vector3(0f, _restY + Mathf.Sin(Time.time * 1.4f) * 0.06f / Mathf.Max(0.01f, transform.localScale.y), 0f);
            }

            void OnEnable() => TapTargets.Register(this);
            void OnDisable() => TapTargets.Unregister(this);

            public float TapDistance(Vector2 worldPoint)
            {
                if (_sr == null) return -1f;
                var b = _sr.bounds;
                return b.Contains(new Vector3(worldPoint.x, worldPoint.y, b.center.z)) ? Vector2.Distance(b.center, worldPoint) : -1f;
            }

            public void Tap()
            {
                Sfx.Play(SfxKind.Tap, 0.5f);
                var pages = CookingStove.Placed() == null && !HobbyLog.IsUnlocked(Hobby.Cooking) ? StoryText.Tofu.Meet : StoryText.Tofu.Back;
                UI.SpeechDialog.Show(_sr.sprite, StoryText.Chef, pages, OpenRecipes);
            }
        }
    }
}
