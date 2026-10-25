using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CapyIsland.Game;
using UnityEditor;
using UnityEngine;

namespace CapyIsland.EditorTools
{
    /// <summary>
    /// CapyIsland ▸ Holiday Skins: try a skin from this repo's Skins folder in Play, before it is
    /// pushed to GitHub (and so before any player sees it), and list the sprite names a skin can
    /// replace. See Skins/README.md.
    /// </summary>
    public class HolidaySkinsWindow : EditorWindow
    {
        static string SkinsFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Skins"));

        Vector2 _scroll;

        [MenuItem("CapyIsland/Holiday Skins")]
        static void Open() => GetWindow<HolidaySkinsWindow>("Holiday Skins");

        void OnGUI()
        {
            string preview = EditorPrefs.GetString(SkinService.EditorPreviewKey, "");
            EditorGUILayout.HelpBox(
                "Players' games read Skins/skins.json from GitHub (main branch) every start and wear the skin whose dates hold today.\n" +
                "Preview shows a skin from this folder instead, whatever the date. It takes effect the next time Play starts.",
                MessageType.Info);

            EditorGUILayout.LabelField("Previewing", preview.Length > 0 ? preview : "nothing — the real schedule from GitHub");
            if (preview.Length > 0 && GUILayout.Button("Stop previewing"))
                EditorPrefs.DeleteKey(SkinService.EditorPreviewKey);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Skins in this repo", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (Directory.Exists(SkinsFolder))
                foreach (var dir in Directory.GetDirectories(SkinsFolder).OrderBy(d => d))
                {
                    if (!File.Exists(Path.Combine(dir, "skin.json"))) continue;
                    string id = Path.GetFileName(dir);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(id, id == preview ? EditorStyles.boldLabel : EditorStyles.label);
                        if (GUILayout.Button("Preview", GUILayout.Width(80)))
                        {
                            EditorPrefs.SetString(SkinService.EditorPreviewKey, id);
                            if (EditorApplication.isPlaying) ShowNotification(new GUIContent("Restart Play to see it"));
                        }
                        if (GUILayout.Button("Greet again", GUILayout.Width(90)))
                        {
                            foreach (var v in Enumerable.Range(0, 50)) PlayerPrefs.DeleteKey("CapyIsland.Skins.Greeted." + id + ".v" + v);
                            PlayerPrefs.Save();
                        }
                    }
                }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("Write Skins/ART_NAMES.txt (every sprite a skin can replace)")) WriteNames();
        }

        /// <summary>Every sprite in the game, by the name a skin's sprites/&lt;name&gt;.png must have, with its size.</summary>
        static void WriteNames()
        {
            var lines = new SortedDictionary<string, string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art", "Assets/Resources" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                    lines[s.name] = $"{s.name}\t{(int)s.rect.width}x{(int)s.rect.height}\t{path}";
            }
            var sb = new StringBuilder();
            sb.AppendLine("# Sprite names a holiday skin can replace (sprites/<name>.png). Name, size in pixels, source.");
            sb.AppendLine("# Any size works if the proportions match: the picture is scaled to the original's size and pivot.");
            foreach (var l in lines.Values) sb.AppendLine(l);
            Directory.CreateDirectory(SkinsFolder);
            File.WriteAllText(Path.Combine(SkinsFolder, "ART_NAMES.txt"), sb.ToString());
            Debug.Log($"[Skins] Wrote {lines.Count} sprite names to Skins/ART_NAMES.txt");
        }
    }
}
