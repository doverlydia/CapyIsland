using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CapyIsland.Game;

namespace CapyIsland.EditorTools
{
    /// <summary>
    /// Builds a template for every store item the game can hand out — the runtime catalogue. Each
    /// item's rules come from <see cref="StoreData"/> (store_items.xml); this file only decides what
    /// each one looks like and which behaviour it carries.
    /// </summary>
    public static partial class IslandBuilder
    {
        /// <summary>Art for an item: sprite path, carried/planted scale, shadow width.</summary>
        struct Look
        {
            public string sprite;
            public float scale;
            public float shadow;
            public string planted;   // gardens: the single flower a planted bed becomes
            public Look(string sprite, float scale, float shadow, string planted = null)
            {
                this.sprite = sprite; this.scale = scale; this.shadow = shadow; this.planted = planted;
            }
        }

        const string Terrain = "Assets/Art/Terrain/";
        const string Buildings = "Assets/Art/Buildings/";
        const string Capys = "Assets/Art/Capybaras/";
        const string Props = "Assets/Art/Props/";

        static Look LookFor(StoreItemDef def)
        {
            string n = def.Name;
            if (n.EndsWith("Posie Garden")) return new Look(Terrain + "Garden_Posie.png", 1.6f, 0.8f, Terrain + "Flower_Posie.png");
            if (n.EndsWith("Tulip Garden")) return new Look(Terrain + "Garden_Tulip.png", 1.6f, 0.8f, Terrain + "Flower_Tulip.png");
            if (n.EndsWith("Rose Garden")) return new Look(Terrain + "Garden_Rose.png", 1.6f, 0.8f, Terrain + "Flower_Rose.png");
            if (n.EndsWith("Pine Forest")) return new Look(Terrain + "Tree_Pine.png", 1.85f, 0.62f);
            if (n.EndsWith("Maple Forest")) return new Look(Terrain + "Tree_Maple.png", 1.85f, 0.7f);
            if (n.EndsWith("Ironwood Forest")) return new Look(Terrain + "Tree_Ironwood.png", 1.85f, 0.55f);
            if (n.EndsWith("Gray Hill")) return new Look(Terrain + "Hill_Gray.png", 1.6f, 0.95f);
            if (n.EndsWith("Black Hill")) return new Look(Terrain + "Hill_Black.png", 1.6f, 0.95f);
            if (n.EndsWith("Gold Hill")) return new Look(Terrain + "Hill_Gold.png", 1.6f, 0.95f);

            switch (n)
            {
                case "Sawmill": return new Look(Buildings + "Build_Sawmill.png", 1.7f, 0.85f);
                case "Legendary Sawmill": return new Look(Buildings + "Build_LegendMill.png", 1.7f, 0.85f);
                case "Mine": return new Look(Buildings + "Build_Mine.png", 1.7f, 0.85f);
                case "Legendary Mine": return new Look(Buildings + "Build_LegendMine.png", 1.7f, 0.85f);
                case "General Store": return new Look(Buildings + "Store_General.png", 1.8f, 0.9f);
                case "Bronze Store": return new Look(Buildings + "Store_Bronze.png", 1.8f, 0.9f);
                case "Silver Store": return new Look(Buildings + "Store_Silver.png", 1.8f, 0.9f);
                case "Steel Store": return new Look(Buildings + "Store_Steel.png", 1.8f, 0.9f);
                case "Gold Store": return new Look(Buildings + "Store_Gold.png", 1.8f, 0.9f);
                case "Legendary Gem Store": return new Look(Buildings + "Store_Gem.png", 1.8f, 0.9f);
                case "Bank": return new Look(Buildings + "Store_Bank.png", 1.8f, 0.9f);
                case "Fancy Store": return new Look(Buildings + "Store_Fancy.png", 1.8f, 0.9f);
                case "Capy House": return new Look(Buildings + "Build_CapyHouse.png", 1.7f, 0.85f);
                case "Fox House": return new Look(Buildings + "Build_FoxHouse.png", 1.7f, 0.85f);
                case "Monster House": return new Look(Buildings + "Build_MonsterHouse.png", 1.7f, 0.85f);
                case "Church": return new Look(Buildings + "Build_Church.png", 2.6f, 1.2f);
                case "Tower Of Love": return new Look(Buildings + "Build_Tower.png", 2.4f, 0.9f);

                case "Capybara": return new Look(Capys + "Capy_Idle.png", 1.2f, 0.5f);
                case "Pirate": return new Look(Capys + "Capy_Pirate.png", 1.2f, 0.5f);
                case "Fox": return new Look(Capys + "Animal_Fox.png", 1.15f, 0.5f);
                case "Panda": return new Look(Capys + "Animal_Panda.png", 1.2f, 0.55f);
                case "Monster": return new Look(Capys + "Animal_Monster.png", 1.3f, 0.6f);
                case "Deer": return new Look(Capys + "Animal_Deer.png", 1.3f, 0.6f);
                case "Coriander": return new Look(Capys + "Capy_Coriander.png", 1.25f, 0.5f);
                case "Pirate Peach": return new Look(Capys + "Capy_Peach.png", 1.25f, 0.5f);
                case "Pirate Pete": return new Look(Capys + "Capy_Pete.png", 1.25f, 0.5f);
                case "Ghost": return new Look(Capys + "Capy_Ghost.png", 1.3f, 0.45f);

                case "Bomb": return new Look(Props + "Item_Bomb.png", 0.9f, 0.45f);
                case "Deer Bomb": return new Look(Props + "Item_DeerBomb.png", 0.9f, 0.45f);
                case "Wedding Dress": return new Look(Props + "Item_Dress.png", 1f, 0.4f);
                case "String of Pearls": return new Look(Props + "Item_Pearls.png", 0.9f, 0.4f);
                case "Tiara": return new Look(Props + "Item_Tiara.png", 0.9f, 0.4f);
                case "Buy Wood": return new Look("Assets/Art/UI/Icon_Wood.png", 0.8f, 0.35f);
                case "Buy Gold": return new Look("Assets/Art/UI/Icon_Gold.png", 0.8f, 0.35f);

                case "White Flower Tree": return new Look(Props + "Deco_WhiteFlowerTree.png", 1.7f, 0.6f);
                case "Pink Flower Tree": return new Look(Props + "Deco_PinkFlowerTree.png", 1.7f, 0.6f);
                case "Holy Statue": return new Look(Props + "Deco_HolyStatue.png", 1.5f, 0.6f);
                case "Gem Statue": return new Look(Props + "Deco_GemStatue.png", 1.5f, 0.6f);
                case "Bushy Tree": return new Look(Props + "Deco_BushyTree.png", 1.6f, 0.6f);
                case "Fall Color": return new Look(Props + "Deco_FallTree.png", 1.7f, 0.6f);
                case "Tiny Bush": return new Look(Props + "Deco_TinyBush.png", 0.9f, 0.45f);
                case "Little Greeny": return new Look(Props + "Deco_LittleGreeny.png", 1.1f, 0.45f);
                case "Splash Fountain": return new Look(Props + "Deco_Fountain.png", 1.9f, 0.9f);
                case "Monster Den": return new Look(Props + "Deco_MonsterDen.png", 1.8f, 0.9f);
            }
            return default;
        }

        static readonly Dictionary<string, string> StoreLists = new()
        {
            { "General Store", "general_store" }, { "Bronze Store", "bronze_store" },
            { "Silver Store", "silver_store" }, { "Steel Store", "steel_store" },
            { "Gold Store", "gold_store" }, { "Legendary Gem Store", "gem_store" },
            { "Bank", "bank" }, { "Fancy Store", "fancy_store" },
        };

        static readonly Dictionary<string, string> HouseAnimals = new()
        {
            { "Capy House", "Capybara" }, { "Fox House", "Fox" }, { "Monster House", "Monster" },
        };

        /// <summary>
        /// A template for one store item, or null if it has no look (unobtainable in the shipped
        /// game, or built elsewhere like the bridge ghosts).
        /// </summary>
        static GameObject BuildCatalogItem(string name, Transform parent, Sprite shadow)
        {
            if (!StoreData.TryGet(name, out var def)) { Debug.LogWarning($"[IslandBuilder] Unknown store item {name}"); return null; }
            if (def.Kind == StoreItemKind.FruitTree)
            {
                var kind = name == "Apple Tree" ? FruitKind.Apple : name == "Pear Tree" ? FruitKind.Pear : FruitKind.Plum;
                return Tag(BuildFruitTreeItem(parent, shadow, kind), name);
            }

            var look = LookFor(def);
            if (string.IsNullOrEmpty(look.sprite)) return null;
            var sprite = Load(look.sprite);

            GameObject go;
            switch (def.Kind)
            {
                case StoreItemKind.Animal:
                    go = BuildAnimal(def, sprite, shadow, parent, look);
                    break;

                case StoreItemKind.Gift:
                case StoreItemKind.BankTrade:
                    // Never placed: only its picture is used, on a shop display.
                    go = Prop(parent, name, sprite, shadow, Vector2.zero, look.scale, look.shadow);
                    go.SetActive(false);
                    break;

                case StoreItemKind.Character when name == "Ghost":
                    go = Prop(parent, name, sprite, shadow, Vector2.zero, look.scale, look.shadow);
                    go.AddComponent<YSorter>();
                    go.AddComponent<QuestGiver>().Configure(QuestGiver.Kind.Ghost);
                    go.SetActive(false);
                    break;

                default:
                    go = MakeItem(parent, name, sprite, shadow, look.scale, look.shadow, def.Description, "", def.Inner, def.Outer);
                    AddBehaviour(go, def, look);
                    break;
            }
            return Tag(go, name);
        }

        static GameObject Tag(GameObject go, string name)
        {
            if (go == null) return null;
            var tag = go.GetComponent<CatalogItem>();
            if (tag == null) tag = go.AddComponent<CatalogItem>();
            tag.SetName(name);
            EditorUtility.SetDirty(tag);
            return go;
        }

        static void AddBehaviour(GameObject go, StoreItemDef def, Look look)
        {
            string n = def.Name;
            switch (def.Kind)
            {
                case StoreItemKind.Forest:
                case StoreItemKind.Garden:
                case StoreItemKind.Hill:
                {
                    var resource = ResourceFor(n);
                    float art = def.Kind == StoreItemKind.Garden ? 0.8f : def.Kind == StoreItemKind.Hill ? 1.1f : 1.4f;
                    ConfigureNode(go, resource, def.Inner, def.Outer, art, look.planted != null ? Load(look.planted) : null);
                    break;
                }

                case StoreItemKind.Workplace:
                {
                    var wp = go.AddComponent<Workplace>();
                    var so = new SerializedObject(wp);
                    so.FindProperty("accepts").enumValueIndex = (int)(n.Contains("Mine") ? ResourceKind.Gold : ResourceKind.Wood);
                    so.FindProperty("legendary").boolValue = n.StartsWith("Legendary");
                    so.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }

                case StoreItemKind.Store:
                    go.AddComponent<StoreBuilding>().Configure(StoreLists.TryGetValue(n, out var list) ? list : "", "");
                    break;

                case StoreItemKind.House:
                    go.AddComponent<StoreBuilding>().Configure("", HouseAnimals.TryGetValue(n, out var animal) ? animal : "");
                    break;

                case StoreItemKind.Bomb:
                    go.AddComponent<Bomb>().Configure(deerOnly: false);
                    break;

                case StoreItemKind.DeerBomb:
                    go.AddComponent<Bomb>().Configure(deerOnly: true);
                    break;

                case StoreItemKind.Church:
                    go.AddComponent<Church>();
                    break;

                case StoreItemKind.Tower:
                    go.AddComponent<TowerOfLove>();
                    break;

                case StoreItemKind.Character:
                    if (n == "Coriander") go.AddComponent<QuestGiver>().Configure(QuestGiver.Kind.Coriander);
                    else if (n == "Pirate Peach") go.AddComponent<QuestGiver>().Configure(QuestGiver.Kind.Peach);
                    else if (n == "Pirate Pete") go.AddComponent<PiratePete>();
                    break;

                case StoreItemKind.Decoration:
                    if (n.Contains("Tree") || n == "Fall Color" || n == "Tiny Bush" || n == "Bushy Tree")
                        go.AddComponent<Sway>().Configure(0.8f, 0.5f, new Color(0.45f, 0.66f, 0.33f));
                    break;
            }
        }

        static NodeResource ResourceFor(string name)
        {
            if (name.Contains("Posie")) return NodeResource.Posie;
            if (name.Contains("Tulip")) return NodeResource.Tulip;
            if (name.Contains("Rose")) return NodeResource.Rose;
            if (name.Contains("Pine")) return NodeResource.Pine;
            if (name.Contains("Maple")) return NodeResource.Maple;
            if (name.Contains("Ironwood")) return NodeResource.Ironwood;
            if (name.Contains("Gray")) return NodeResource.Bronze;
            if (name.Contains("Black")) return NodeResource.Silver;
            return NodeResource.Gold;
        }

        /// <summary>
        /// Worker species, game.xml &lt;animals&gt;: base speed, per-resource speeds, food rate.
        /// Monsters and deer are built wild; a monster becomes a worker once tamed.
        /// </summary>
        static GameObject BuildAnimal(StoreItemDef def, Sprite sprite, Sprite shadow, Transform parent, Look look)
        {
            var go = Prop(parent, def.Name, sprite, shadow, Vector2.zero, look.scale, look.shadow);
            go.AddComponent<YSorter>();
            go.AddComponent<WalkBob>();

            if (def.Name == "Deer")
            {
                go.AddComponent<DeerAnimal>();
                go.SetActive(false);
                return go;
            }

            var worker = go.AddComponent<Worker>();
            var wso = new SerializedObject(worker);
            wso.FindProperty("speciesName").stringValue = def.Name;
            int speed = 3; float food = 1f;
            var mods = new List<(NodeResource, int)>();
            switch (def.Name)
            {
                case "Pirate": speed = 4; mods.Add((NodeResource.Posie, 8)); break;
                case "Panda": speed = 2; food = 2f; mods.Add((NodeResource.Maple, 8)); mods.Add((NodeResource.Tulip, 8)); break;
                case "Fox": speed = 6; mods.Add((NodeResource.Ironwood, 12)); mods.Add((NodeResource.Tulip, 12)); break;
                case "Monster": speed = 6; mods.Add((NodeResource.Bronze, 12)); mods.Add((NodeResource.Silver, 12)); mods.Add((NodeResource.Gold, 12)); break;
            }
            wso.FindProperty("normalSpeed").intValue = speed;
            wso.FindProperty("foodRate").floatValue = food;
            var list = wso.FindProperty("modifiers");
            list.arraySize = mods.Count;
            for (int i = 0; i < mods.Count; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("resource").enumValueIndex = (int)mods[i].Item1;
                e.FindPropertyRelative("speed").intValue = mods[i].Item2;
            }
            wso.ApplyModifiedPropertiesWithoutUndo();

            var visuals = go.AddComponent<WorkerVisuals>();
            var vso = new SerializedObject(visuals);
            vso.FindProperty("axe").objectReferenceValue = Load("Assets/Art/Common/Tool_Axe.png");
            vso.FindProperty("pickaxe").objectReferenceValue = Load("Assets/Art/Common/Tool_Pickaxe.png");
            vso.FindProperty("log").objectReferenceValue = Load("Assets/Art/Common/Prop_Log.png");
            if (def.Name == "Capybara")
                vso.FindProperty("sleeping").objectReferenceValue = Load("Assets/Art/Capybaras/Capy_Sleep.png");
            vso.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent<Follower>();
            go.AddComponent<DraggableActor>();
            if (def.Name == "Monster") go.AddComponent<MonsterAnimal>();
            go.SetActive(false);
            return go;
        }

        /// <summary>
        /// Every item's template, inactive under Templates, registered with the runtime catalogue.
        /// </summary>
        static void BuildCatalog(Transform root, Transform systems, Sprite shadow)
        {
            var templates = new GameObject("Templates");
            templates.transform.SetParent(root, false);
            templates.SetActive(false);   // nothing in here ever runs

            var items = new GameObject("Items");
            items.transform.SetParent(root, false);

            var list = new List<CatalogItem>();
            foreach (var def in StoreData.All)
            {
                var go = BuildCatalogItem(def.Name, templates.transform, shadow);
                if (go != null) list.Add(go.GetComponent<CatalogItem>());
            }

            var catalog = systems.gameObject.AddComponent<ItemCatalog>();
            var so = new SerializedObject(catalog);
            var t = so.FindProperty("templates");
            t.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) t.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.FindProperty("itemParent").objectReferenceValue = items.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
