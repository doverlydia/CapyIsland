using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CapyIsland.EditorTools
{
    /// <summary>
    /// Rebuilds the playable island. The island starts **desolate** — no trees, no buildings, no
    /// workers — exactly as the original opens. Everything arrives through bubbles the player taps:
    /// forest, then sawmill, then capy house. Only then can a capybara be bought and posted to a mill.
    ///
    /// Menu: CapyIsland > Build > Island
    /// </summary>
    public static class IslandBuilder
    {
        const string RootName = "Island";

        // Matches ResponsiveCameraRig's default worldBounds.
        const float IslandExtent = 20f;

        // Props sit well above the ground layers (-200 water, -100 landmass) at every island Y.
        const int PropSortBase = 3000;

        [MenuItem("CapyIsland/Build/Island")]
        public static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[IslandBuilder] Exit play mode before rebuilding the island.");
                return;
            }

            foreach (var stale in new[] { RootName, "IslandPlaceholder" })
            {
                var go = GameObject.Find(stale);
                if (go != null) Object.DestroyImmediate(go);
            }

            var root = new GameObject(RootName);
            BuildGround(root.transform);

            var shadow = Load("Assets/Art/Common/Shadow_Ellipse.png");

            var actors = new GameObject("Actors");
            actors.transform.SetParent(root.transform, false);

            var king = Prop(actors.transform, "King", Load("Assets/Art/Capybaras/Capy_King.png"),
                shadow, new Vector2(0f, -2f), 1.3f, 0.55f);
            king.AddComponent<Game.PlayerAvatar>();
            king.AddComponent<Game.YSorter>();

            // Everything the player will own starts inactive inside a bubble.
            var stash = new GameObject("BubbleContents");
            stash.transform.SetParent(root.transform, false);

            var bubbles = new GameObject("Bubbles");
            bubbles.transform.SetParent(root.transform, false);

            MakeBubble(bubbles.transform, new Vector2(-5.5f, 1.5f), BuildForestItem(stash.transform, shadow));
            MakeBubble(bubbles.transform, new Vector2(5.5f, 2.5f), BuildSawmillItem(stash.transform, shadow));
            MakeBubble(bubbles.transform, new Vector2(0.5f, 6f), BuildCapyHouseItem(stash.transform, shadow));

            var systems = new GameObject("Systems");
            systems.transform.SetParent(root.transform, false);
            systems.AddComponent<Game.ResourceStore>();

            BuildPlacementPreview(systems.transform);

            BuildHud(root.transform);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log("[IslandBuilder] Built a desolate island with 3 bubbles.");
        }

        // ---------------------------------------------------------------- bubble contents

        static GameObject BuildForestItem(Transform parent, Sprite shadow)
        {
            var item = MakeItem(parent, "Medium Pine Forest", Load("Assets/Art/Terrain/Tree_Pine.png"),
                shadow, 1.85f, 0.62f,
                "Plant pine trees to grow lovely forests. Place a Sawmill and a Capybara nearby to harvest wood.",
                "Trees + Sawmill + Capy = 3 wood", inner: 1.6f, outer: 6.5f);

            var h = item.AddComponent<Game.Harvestable>();
            var so = new SerializedObject(h);
            so.FindProperty("kind").enumValueIndex = (int)Game.ResourceKind.Wood;
            so.FindProperty("yieldPerTrip").intValue = 3;      // game.xml: price/pine
            so.FindProperty("capacity").intValue = 24;
            so.FindProperty("renewable").boolValue = true;
            so.FindProperty("regrowSeconds").floatValue = 30f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Carried as one small tree icon; planting grows it into a clump of five.
            item.AddComponent<Game.ScatterOnPlace>();
            return item;
        }

        static GameObject BuildSawmillItem(Transform parent, Sprite shadow)
        {
            var item = MakeItem(parent, "Sawmill", Load("Assets/Art/Buildings/Build_Sawmill.png"),
                shadow, 1.7f, 0.85f,
                "Place a Sawmill next to a Forest. Add a Capybara to harvest wood.",
                "Trees + Sawmill + Capy = 3 wood", inner: 1.4f, outer: 9f);

            var mill = item.AddComponent<Game.Workplace>();
            var so = new SerializedObject(mill);
            so.FindProperty("accepts").enumValueIndex = (int)Game.ResourceKind.Wood;
            so.FindProperty("workRadius").floatValue = 9f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        static GameObject BuildCapyHouseItem(Transform parent, Sprite shadow)
        {
            var item = MakeItem(parent, "Capy House", Load("Assets/Art/Buildings/Build_CapyHouse.png"),
                shadow, 1.7f, 0.85f,
                "Purchase Capybaras from a Capy House to help harvest wood and gold.",
                "Capy House = buy Capybaras", inner: 1.4f, outer: 4f);

            // The sample capybara on its pedestal beside the shop, which you tap to buy.
            var sample = new GameObject("Sample");
            sample.transform.SetParent(item.transform, false);
            sample.transform.localPosition = new Vector3(1.25f, 0f, 0f);
            sample.transform.localScale = Vector3.one * 0.62f;
            var sr = sample.AddComponent<SpriteRenderer>();
            sr.sprite = Load("Assets/Art/Capybaras/Capy_Idle.png");
            sr.sortingOrder = PropSortBase + 1;

            var template = BuildWorkerTemplate(parent, shadow);

            var shop = item.AddComponent<Game.ShopBuilding>();
            var so = new SerializedObject(shop);
            so.FindProperty("itemName").stringValue = "Capybara";
            so.FindProperty("blurb").stringValue =
                "A calm creature with a broad nose and endless patience. Capybaras earn you wood and gold.";
            so.FindProperty("itemPrefab").objectReferenceValue = template;
            so.FindProperty("woodCost").intValue = 10;   // the original's Bunni price
            so.FindProperty("goldCost").intValue = 10;
            so.FindProperty("gemCost").intValue = 0;
            so.FindProperty("sample").objectReferenceValue = sample.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        /// <summary>An inactive worker the shop clones on purchase.</summary>
        static GameObject BuildWorkerTemplate(Transform parent, Sprite shadow)
        {
            var go = Prop(parent, "CapybaraTemplate", Load("Assets/Art/Capybaras/Capy_Idle.png"),
                shadow, Vector2.zero, 1.2f, 0.5f);
            go.AddComponent<Game.Worker>();
            go.AddComponent<Game.Follower>();
            go.AddComponent<Game.YSorter>();
            go.AddComponent<Game.DraggableActor>();
            go.SetActive(false);
            return go;
        }

        /// <summary>A carried item: sprite, shadow, follower, draggable, placeable identity.</summary>
        static GameObject MakeItem(Transform parent, string name, Sprite sprite, Sprite shadow,
            float scale, float shadowWidth, string description, string recipe, float inner, float outer)
        {
            var go = Prop(parent, name, sprite, shadow, Vector2.zero, scale, shadowWidth);
            go.AddComponent<Game.Follower>();
            go.AddComponent<Game.YSorter>();
            go.AddComponent<Game.DraggableActor>();

            var placeable = go.AddComponent<Game.Placeable>();
            var so = new SerializedObject(placeable);
            so.FindProperty("displayName").stringValue = name;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("recipeLine").stringValue = recipe;
            so.FindProperty("innerRadius").floatValue = inner;
            so.FindProperty("outerRadius").floatValue = outer;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Scale is left authored here; Placeable shrinks it to icon size in Awake.
            go.SetActive(false);   // lives inside a bubble until collected
            return go;
        }

        static void MakeBubble(Transform parent, Vector2 pos, GameObject contents)
        {
            var go = new GameObject("Bubble_" + contents.name.Replace(" ", ""));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);

            int order = PropSortBase + Mathf.RoundToInt(-pos.y * 100f);

            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = Vector3.one * 1.15f;
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = Load("Assets/Art/Common/Bubble.png");
            sr.sortingOrder = order;

            var arrow = new GameObject("Arrow");
            arrow.transform.SetParent(go.transform, false);
            arrow.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            arrow.transform.localScale = Vector3.one * 0.7f;
            var ar = arrow.AddComponent<SpriteRenderer>();
            ar.sprite = Load("Assets/Art/Common/Arrow_Down.png");
            ar.sortingOrder = order + 2;

            var bubble = go.AddComponent<Game.Bubble>();
            var so = new SerializedObject(bubble);
            so.FindProperty("contents").objectReferenceValue = contents;
            so.FindProperty("arrow").objectReferenceValue = arrow.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The dark ellipse shown under a held item: its area of effect. One instance follows
        /// whatever is currently being dragged.
        /// </summary>
        static void BuildPlacementPreview(Transform parent)
        {
            var go = new GameObject("PlacementPreview");
            go.transform.SetParent(parent, false);

            var ringGo = new GameObject("Ring");
            ringGo.transform.SetParent(go.transform, false);
            var sr = ringGo.AddComponent<SpriteRenderer>();
            sr.sprite = Load("Assets/Art/Common/Radius_Ring.png");
            sr.sortingOrder = -50;   // above the landmass (-100), below every prop
            sr.enabled = false;

            var preview = go.AddComponent<Game.PlacementPreview>();
            var so = new SerializedObject(preview);
            so.FindProperty("ring").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- ground and shared bits

        static void BuildGround(Transform parent)
        {
            var ground = new GameObject("Ground");
            ground.transform.SetParent(parent, false);

            // Larger than any orientation can show, so rotating never reveals an edge.
            Quad(ground.transform, "Water", Vector3.zero, new Vector2(120f, 120f),
                new Color(0.56f, 0.80f, 0.88f), -200);

            var islandSprite = Load("Assets/Art/Terrain/Island_Main.png");
            var island = new GameObject("Landmass");
            island.transform.SetParent(ground.transform, false);
            var sr = island.AddComponent<SpriteRenderer>();
            sr.sprite = islandSprite;
            sr.sortingOrder = -100;
            if (islandSprite != null)
            {
                float spriteWidth = islandSprite.bounds.size.x;
                if (spriteWidth > 0.01f)
                    island.transform.localScale = Vector3.one * (IslandExtent * 2f / spriteWidth);
            }
        }

        static void Quad(Transform parent, string name, Vector3 pos, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Load("Assets/Art/Common/White.png");
            sr.color = color;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.sortingOrder = order;
        }

        /// <summary>A sprite plus its own shadow. The art is shadow-free, so the shadow is separate.</summary>
        static GameObject Prop(Transform parent, string name, Sprite sprite, Sprite shadow,
            Vector2 pos, float scale, float shadowWidth)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * scale;

            int order = PropSortBase + Mathf.RoundToInt(-pos.y * 100f);

            if (shadow != null)
            {
                var sh = new GameObject("Shadow");
                sh.transform.SetParent(go.transform, false);
                sh.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                sh.transform.localScale = new Vector3(shadowWidth, shadowWidth, 1f);
                var shr = sh.AddComponent<SpriteRenderer>();
                shr.sprite = shadow;
                shr.sortingOrder = order - 1;
            }

            var body = new GameObject("Sprite");
            body.transform.SetParent(go.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return go;
        }

        // ---------------------------------------------------------------- HUD

        static void BuildHud(Transform parent)
        {
            var canvasGo = new GameObject("HUD",
                typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.transform.SetParent(parent, false);

            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            // Half-and-half, so the UI scales sensibly in both orientations.
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var safe = new GameObject("SafeArea", typeof(RectTransform));
            safe.transform.SetParent(canvasGo.transform, false);
            safe.AddComponent<UI.SafeAreaFitter>();
            var safeRect = safe.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;

            BuildResourceBar(safe.transform);
            BuildPurchaseDialog(safe.transform);
        }

        static void BuildResourceBar(Transform safeArea)
        {
            var bar = new GameObject("ResourceBar", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            bar.transform.SetParent(safeArea, false);
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -88f);
            rect.offsetMax = Vector2.zero;

            var backing = bar.GetComponent<UnityEngine.UI.Image>();
            backing.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Common/White.png");
            backing.color = new Color(0.14f, 0.10f, 0.07f, 0.55f);
            backing.raycastTarget = false;

            var wood = Counter(bar.transform, "Wood", "Assets/Art/UI/Icon_Wood.png", 0f);
            var gold = Counter(bar.transform, "Gold", "Assets/Art/UI/Icon_Gold.png", 230f);
            var gem = Counter(bar.transform, "Gem", "Assets/Art/UI/Icon_Gem.png", 460f);
            var capy = Counter(bar.transform, "Capy", "Assets/Art/Capybaras/Capy_Idle.png", 690f);

            var script = bar.AddComponent<UI.ResourceBar>();
            var so = new SerializedObject(script);
            so.FindProperty("woodLabel").objectReferenceValue = wood;
            so.FindProperty("goldLabel").objectReferenceValue = gold;
            so.FindProperty("gemLabel").objectReferenceValue = gem;
            so.FindProperty("capyLabel").objectReferenceValue = capy;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The buy panel: name, blurb, three prices, Cancel / Purchase. Centred and hidden until a
        /// shop is browsed.
        /// </summary>
        static void BuildPurchaseDialog(Transform safeArea)
        {
            // The listener lives on an always-active root; only the visual panel toggles. If the
            // component sat on the panel it disables, its OnEnable would never run once hidden and
            // it could never hear the next shop.
            var root = new GameObject("PurchaseDialogRoot", typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var panel = new GameObject("PurchaseDialog", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(root.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 460f);

            var bg = panel.GetComponent<UnityEngine.UI.Image>();
            bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Common/White.png");
            bg.color = new Color(0.98f, 0.96f, 0.91f, 0.98f);

            var title = Label(panel.transform, "Title", 52f, TMPro.TextAlignmentOptions.Center,
                new Vector2(0f, 160f), new Vector2(700f, 70f), new Color(0.22f, 0.16f, 0.11f));
            var blurb = Label(panel.transform, "Blurb", 32f, TMPro.TextAlignmentOptions.TopLeft,
                new Vector2(0f, 40f), new Vector2(660f, 170f), new Color(0.30f, 0.24f, 0.18f));

            // Prices stack down the right, each with its icon.
            var wood = PriceRow(panel.transform, "WoodCost", "Assets/Art/UI/Icon_Wood.png", 150f);
            var gold = PriceRow(panel.transform, "GoldCost", "Assets/Art/UI/Icon_Gold.png", 80f);
            var gem = PriceRow(panel.transform, "GemCost", "Assets/Art/UI/Icon_Gem.png", 10f);

            var cancel = DialogButton(panel.transform, "Cancel", "Cancel", -160f,
                new Color(0.86f, 0.83f, 0.77f), new Color(0.28f, 0.22f, 0.16f));
            var purchase = DialogButton(panel.transform, "Purchase", "Purchase", 160f,
                new Color(0.42f, 0.66f, 0.30f), Color.white);

            var dialog = root.AddComponent<UI.PurchaseDialog>();
            var so = new SerializedObject(dialog);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("blurbLabel").objectReferenceValue = blurb;
            so.FindProperty("woodLabel").objectReferenceValue = wood;
            so.FindProperty("goldLabel").objectReferenceValue = gold;
            so.FindProperty("gemLabel").objectReferenceValue = gem;
            so.FindProperty("cancelButton").objectReferenceValue = cancel;
            so.FindProperty("purchaseButton").objectReferenceValue = purchase;
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
        }

        static TMPro.TMP_Text Label(Transform parent, string name, float size,
            TMPro.TextAlignmentOptions align, Vector2 pos, Vector2 dims, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchoredPosition = pos;
            r.sizeDelta = dims;

            var t = go.GetComponent<TMPro.TextMeshProUGUI>();
            t.fontSize = size;
            t.alignment = align;
            t.color = colour;
            t.raycastTarget = false;
            return t;
        }

        static TMPro.TMP_Text PriceRow(Transform parent, string name, string iconPath, float y)
        {
            var icon = new GameObject(name + "Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            icon.transform.SetParent(parent, false);
            var ir = icon.GetComponent<RectTransform>();
            ir.anchoredPosition = new Vector2(300f, y);
            ir.sizeDelta = new Vector2(52f, 52f);
            var img = icon.GetComponent<UnityEngine.UI.Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            img.preserveAspect = true;
            img.raycastTarget = false;

            return Label(parent, name, 38f, TMPro.TextAlignmentOptions.Right,
                new Vector2(215f, y), new Vector2(140f, 52f), new Color(0.22f, 0.16f, 0.11f));
        }

        static UnityEngine.UI.Button DialogButton(Transform parent, string name, string caption,
            float x, Color fill, Color textColour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(x, -160f);
            r.sizeDelta = new Vector2(280f, 86f);

            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Common/White.png");
            img.color = fill;

            Label(go.transform, "Caption", 36f, TMPro.TextAlignmentOptions.Center,
                Vector2.zero, new Vector2(280f, 86f), textColour).text = caption;

            return go.GetComponent<UnityEngine.UI.Button>();
        }

        static TMPro.TMP_Text Counter(Transform parent, string name, string iconPath, float x)
        {
            var group = new GameObject(name, typeof(RectTransform));
            group.transform.SetParent(parent, false);
            var groupRect = group.GetComponent<RectTransform>();
            groupRect.anchorMin = new Vector2(0f, 0.5f);
            groupRect.anchorMax = new Vector2(0f, 0.5f);
            groupRect.pivot = new Vector2(0f, 0.5f);
            groupRect.anchoredPosition = new Vector2(24f + x, 0f);
            groupRect.sizeDelta = new Vector2(220f, 72f);

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            icon.transform.SetParent(group.transform, false);
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(60f, 60f);
            var img = icon.GetComponent<UnityEngine.UI.Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            img.preserveAspect = true;
            img.raycastTarget = false;

            var label = new GameObject("Value", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            label.transform.SetParent(group.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(150f, 60f);
            labelRect.anchoredPosition = new Vector2(70f, 0f);

            var text = label.GetComponent<TMPro.TextMeshProUGUI>();
            text.fontSize = 40f;
            text.alignment = TMPro.TextAlignmentOptions.Left;
            text.color = Color.white;
            text.text = "0";
            text.raycastTarget = false;
            text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            text.overflowMode = TMPro.TextOverflowModes.Overflow;
            return text;
        }

        static GameObject NewImage(string name, Transform parent, string spritePath, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            image.raycastTarget = false;
            return go;
        }

        static Sprite Load(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogWarning($"[IslandBuilder] Missing sprite: {path}");
            return s;
        }
    }
}
