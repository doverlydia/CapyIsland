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
    public static partial class IslandBuilder
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

            MakeBubble(bubbles.transform, new Vector2(-5.5f, 1.5f), BuildCatalogItem("Medium Pine Forest", stash.transform, shadow));
            MakeBubble(bubbles.transform, new Vector2(5.5f, 2.5f), BuildCatalogItem("Sawmill", stash.transform, shadow));
            MakeBubble(bubbles.transform, new Vector2(0.5f, 6f), BuildCatalogItem("Capy House", stash.transform, shadow));

            // The garden has no bubble yet: the tutorial drops one the moment the first worker
            // falls asleep, so the player sees the problem before the fix.
            var garden = BuildCatalogItem("Medium Posie Garden", stash.transform, shadow);

            var systems = new GameObject("Systems");
            systems.transform.SetParent(root.transform, false);
            systems.AddComponent<Game.ResourceStore>();

            BuildPlacementPreview(systems.transform);
            BuildHarvestEffects(systems.transform);
            systems.AddComponent<Game.Wind>();
            BuildGameArt(systems.transform, shadow);
            systems.AddComponent<Game.GameBootstrap>();
            systems.AddComponent<Game.QuestSystem>();
            systems.AddComponent<Game.SaveGame>();
            var sfx = systems.AddComponent<Game.Sfx>();
            var names = System.Enum.GetNames(typeof(Game.SfxKind));
            var clips = new AudioClip[names.Length];
            for (int i = 0; i < names.Length; i++)
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sfx_" + names[i] + ".wav");
            sfx.SetClips(clips);
            EditorUtility.SetDirty(sfx);
            BuildCatalog(root.transform, systems.transform, shadow);
            var pointer = BuildTutorialPointer(systems.transform);

            var ghost = BuildHud(root.transform);

            var tutorial = systems.AddComponent<Game.TutorialDirector>();
            var tso = new SerializedObject(tutorial);
            tso.FindProperty("dialogue").objectReferenceValue = ghost;
            tso.FindProperty("pointer").objectReferenceValue = pointer;
            tso.FindProperty("gardenItem").objectReferenceValue = garden.GetComponent<Game.Placeable>();
            tso.FindProperty("bubbleParent").objectReferenceValue = bubbles.transform;
            tso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log("[IslandBuilder] Built a desolate island with 3 bubbles.");
        }

        // ---------------------------------------------------------------- bubble contents

        static void ConfigureNode(GameObject item, Game.NodeResource resource, float inner, float outer,
            float artScale, Sprite planted)
        {
            var node = item.AddComponent<Game.Harvestable>();
            var so = new SerializedObject(node);
            so.FindProperty("resource").enumValueIndex = (int)resource;
            so.FindProperty("isParent").boolValue = true;
            so.FindProperty("innerSize").floatValue = inner;
            so.FindProperty("outerRadius").floatValue = outer;
            so.FindProperty("artScale").floatValue = artScale;
            so.FindProperty("plantedSprite").objectReferenceValue = planted;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Trees and flowers bend in the wind; trees shed leaves in the gusts.
            var sway = item.AddComponent<Game.Sway>();
            if (Game.BunniRules.FamilyOf(resource) == Game.NodeFamily.Flower)
                sway.Configure(1.6f, 0f, Color.white);
            else
                sway.Configure(1f, 0.7f, new Color(0.33f, 0.52f, 0.30f));
        }

        // ---------------------------------------------------------------- fruit trees

        static readonly (Game.FruitKind kind, string name, string art, string blurb)[] FruitTrees =
        {
            (Game.FruitKind.Apple, "Apple Tree", "Tree_Apple",
                "A magical tree that produces food and gold.  A tree spirit lives inside."),
            (Game.FruitKind.Pear, "Pear Tree", "Tree_Pear",
                "A magical tree that produces food and gold.  A tree spirit lives inside."),
            (Game.FruitKind.Plum, "Legendary Plum Tree", "Tree_Plum",
                "Juicy plums grow heavily from the sweeping branches.  Plum trees produce valuable items when shaken."),
        };

        static GameObject BuildFruitTreeItem(Transform parent, Sprite shadow, Game.FruitKind kind)
        {
            var def = System.Array.Find(FruitTrees, f => f.kind == kind);
            var item = MakeItem(parent, def.name, Load("Assets/Art/Terrain/" + def.art + ".png"),
                shadow, 1.55f, 0.75f, def.blurb, "Tap to shake down fruit", inner: 50f, outer: 0f);

            var tree = item.AddComponent<Game.FruitTree>();
            var so = new SerializedObject(tree);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("artScale").floatValue = 1.55f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var sway = item.AddComponent<Game.Sway>();
            sway.Configure(0.9f, 1.1f, kind == Game.FruitKind.Plum
                ? new Color(0.40f, 0.52f, 0.46f)
                : new Color(0.45f, 0.68f, 0.32f));
            return item;
        }

        /// <summary>Sprites that runtime spawns need: fruit, chests, resource icons, a shadow.</summary>
        static void BuildGameArt(Transform systems, Sprite shadow)
        {
            var shadowTemplate = new GameObject("ShadowTemplate");
            shadowTemplate.transform.SetParent(systems, false);
            shadowTemplate.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var sr = shadowTemplate.AddComponent<SpriteRenderer>();
            sr.sprite = shadow;
            shadowTemplate.SetActive(false);

            var art = systems.gameObject.AddComponent<Game.GameArt>();
            art.shadowTemplate = shadowTemplate;
            art.apple = Load("Assets/Art/Props/Fruit_Apple.png");
            art.pear = Load("Assets/Art/Props/Fruit_Pear.png");
            art.plum = Load("Assets/Art/Props/Fruit_Plum.png");
            art.chestClosed = Load("Assets/Art/Props/Chest_Closed.png");
            art.chestOpen = Load("Assets/Art/Props/Chest_Open.png");
            art.priest = Load("Assets/Art/Capybaras/Capy_Priest.png");
            art.heart = Load("Assets/Art/Props/Prop_Mote.png");
            art.wood = Load("Assets/Art/UI/Icon_Wood.png");
            art.gold = Load("Assets/Art/UI/Icon_Gold.png");
            art.gem = Load("Assets/Art/UI/Icon_Gem.png");
            EditorUtility.SetDirty(art);
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
            body.transform.localScale = Vector3.one * 1.6f;
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = Load("Assets/Art/Common/Bubble.png");
            sr.sortingOrder = order;

            var arrow = new GameObject("Arrow");
            arrow.transform.SetParent(go.transform, false);
            arrow.transform.localPosition = new Vector3(0f, 2.0f, 0f);
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

        /// <summary>Particle bursts for hits, falls and fruit. Sprites are generated by Tools/fx.</summary>
        static void BuildHarvestEffects(Transform parent)
        {
            var go = new GameObject("HarvestEffects");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<Game.HarvestEffects>();
            var so = new SerializedObject(fx);
            so.FindProperty("leaf").objectReferenceValue = Load("Assets/Art/FX/FX_Leaf.png");
            so.FindProperty("chip").objectReferenceValue = Load("Assets/Art/FX/FX_Chip.png");
            so.FindProperty("petal").objectReferenceValue = Load("Assets/Art/FX/FX_Petal.png");
            so.FindProperty("grit").objectReferenceValue = Load("Assets/Art/FX/FX_Grit.png");
            so.FindProperty("sparkle").objectReferenceValue = Load("Assets/Art/FX/FX_Sparkle.png");
            so.FindProperty("juice").objectReferenceValue = Load("Assets/Art/FX/FX_Juice.png");
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

        /// <summary>Set before Build to regenerate the island textures instead of reusing them.</summary>
        public static bool RepaintLand;

        [MenuItem("CapyIsland/Delete Save")]
        public static void DeleteSave() => Game.SaveGame.Delete();

        [MenuItem("CapyIsland/Build/Island (repaint land)")]
        public static void BuildRepainting()
        {
            RepaintLand = true;
            try { Build(); } finally { RepaintLand = false; }
        }

        /// <summary>
        /// The sea and the archipelago: seven islands and six bridges, laid out on the original's
        /// 4500-unit world with the start island at the origin. Bridges start closed and hidden.
        /// </summary>
        static void BuildGround(Transform parent)
        {
            var ground = new GameObject("Ground");
            ground.transform.SetParent(parent, false);

            var worldCentre = Game.LandMap.WorldToUnity(new Vector2(Game.LandMap.WorldSize * 0.5f, Game.LandMap.WorldSize * 0.5f));
            Quad(ground.transform, "Water", worldCentre, new Vector2(300f, 200f), new Color(0.56f, 0.80f, 0.88f), -200);

            var pieces = IslandPainter.Layout();
            for (int i = 0; i < pieces.Count; i++)
            {
                pieces[i].layers = IslandPainter.Paint(pieces[i], i, ground.transform, RepaintLand);
                if (pieces[i].isBridge)
                    foreach (var layer in pieces[i].layers) layer.enabled = false;
            }

            var land = ground.AddComponent<Game.LandMap>();
            land.SetPieces(pieces);
            EditorUtility.SetDirty(land);
            ScatterDressing(ground.transform, land);

            // The camera may roam the whole world, with a little sea beyond it.
            var rig = Object.FindFirstObjectByType<CameraRig.ResponsiveCameraRig>();
            if (rig != null)
            {
                rig.WorldBounds = new Bounds(new Vector3(worldCentre.x, worldCentre.y, 0f), new Vector3(200f, 104f, 0f));
                EditorUtility.SetDirty(rig);
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

        static UI.GhostDialogue BuildHud(Transform parent)
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

            // Later siblings draw on top: the ghost's hints under any dialog, the card over all.
            BuildResourceBar(safe.transform);
            var ghost = BuildGhostDialogue(safe.transform);
            BuildSpeechDialog(safe.transform);
            BuildPurchaseDialog(safe.transform);
            BuildCardPopup(safe.transform);
            return ghost;
        }

        /// <summary>
        /// The pointing hand, in world space so it can sit over a moving target without any
        /// screen-space conversion.
        /// </summary>
        static Game.TutorialPointer BuildTutorialPointer(Transform parent)
        {
            var go = new GameObject("TutorialPointer");
            go.transform.SetParent(parent, false);

            var handGo = new GameObject("Hand");
            handGo.transform.SetParent(go.transform, false);
            handGo.transform.localScale = Vector3.one * 1.15f;
            var sr = handGo.AddComponent<SpriteRenderer>();
            sr.sprite = Load("Assets/Art/Common/UI_PointerHand.png");
            sr.sortingOrder = PropSortBase + 9000;   // always over the world
            sr.enabled = false;

            var pointer = go.AddComponent<Game.TutorialPointer>();
            var so = new SerializedObject(pointer);
            so.FindProperty("hand").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();
            return pointer;
        }

        /// <summary>The ghost who talks you through the opening, bottom-left like the original.</summary>
        static UI.GhostDialogue BuildGhostDialogue(Transform safeArea)
        {
            var root = new GameObject("GhostDialogueRoot", typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(root.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(24f, 24f);
            rect.sizeDelta = new Vector2(880f, 210f);

            // Ghost portrait on the left.
            var portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            portraitGo.transform.SetParent(panel.transform, false);
            var pr = portraitGo.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0f, 0.5f);
            pr.anchorMax = new Vector2(0f, 0.5f);
            pr.pivot = new Vector2(0f, 0.5f);
            pr.sizeDelta = new Vector2(180f, 180f);
            var portrait = portraitGo.GetComponent<UnityEngine.UI.Image>();
            portrait.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Capybaras/Capy_Ghost.png");
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;

            // Speech bubble to its right.
            var bubbleGo = new GameObject("Bubble", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            bubbleGo.transform.SetParent(panel.transform, false);
            var br = bubbleGo.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0f, 0.5f);
            br.anchorMax = new Vector2(0f, 0.5f);
            br.pivot = new Vector2(0f, 0.5f);
            br.anchoredPosition = new Vector2(196f, 0f);
            br.sizeDelta = new Vector2(660f, 180f);
            var bubble = bubbleGo.GetComponent<UnityEngine.UI.Image>();
            bubble.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Common/White.png");
            bubble.color = new Color(0.16f, 0.13f, 0.10f, 0.82f);
            bubble.raycastTarget = false;

            var labelGo = new GameObject("Line", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            labelGo.transform.SetParent(bubbleGo.transform, false);
            var lr = labelGo.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(26f, 20f);
            lr.offsetMax = new Vector2(-26f, -20f);

            var label = labelGo.GetComponent<TMPro.TextMeshProUGUI>();
            label.fontSize = 32f;
            label.alignment = TMPro.TextAlignmentOptions.TopLeft;
            label.color = new Color(0.98f, 0.96f, 0.92f);
            label.raycastTarget = false;
            label.text = "";

            var dialogue = root.AddComponent<UI.GhostDialogue>();
            var so = new SerializedObject(dialogue);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("portrait").objectReferenceValue = portrait;
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return dialogue;
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
            rect.sizeDelta = new Vector2(820f, 520f);

            var bg = panel.GetComponent<UnityEngine.UI.Image>();
            bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Common/White.png");
            bg.color = new Color(0.98f, 0.96f, 0.91f, 0.98f);

            var title = Label(panel.transform, "Title", 52f, TMPro.TextAlignmentOptions.Center,
                new Vector2(0f, 195f), new Vector2(760f, 70f), new Color(0.22f, 0.16f, 0.11f));
            var blurb = Label(panel.transform, "Blurb", 30f, TMPro.TextAlignmentOptions.TopLeft,
                new Vector2(-105f, 35f), new Vector2(560f, 230f), new Color(0.30f, 0.24f, 0.18f));
            blurb.textWrappingMode = TMPro.TextWrappingModes.Normal;
            blurb.enableAutoSizing = true;
            blurb.fontSizeMin = 18f;
            blurb.fontSizeMax = 30f;

            // Prices stack down the right, each with its icon.
            var wood = PriceRow(panel.transform, "WoodCost", "Assets/Art/UI/Icon_Wood.png", 120f);
            var gold = PriceRow(panel.transform, "GoldCost", "Assets/Art/UI/Icon_Gold.png", 50f);
            var gem = PriceRow(panel.transform, "GemCost", "Assets/Art/UI/Icon_Gem.png", -20f);

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
            ir.anchoredPosition = new Vector2(350f, y);
            ir.sizeDelta = new Vector2(52f, 52f);
            var img = icon.GetComponent<UnityEngine.UI.Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            img.preserveAspect = true;
            img.raycastTarget = false;

            return Label(parent, name, 38f, TMPro.TextAlignmentOptions.Right,
                new Vector2(265f, y), new Vector2(130f, 52f), new Color(0.22f, 0.16f, 0.11f));
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
