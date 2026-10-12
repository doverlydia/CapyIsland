using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace CapyIsland.EditorTools
{
    /// <summary>
    /// Capy Island > App Icon: puts the icon art (Tools/ChatGPT/batch_icon.sh, shipped to
    /// Assets/Art/Icon/) into Player Settings — the full icon as the default and for iOS and
    /// older Android launchers, and the foreground/background pair as Android's adaptive icon.
    /// </summary>
    public static class AppIcon
    {
        const string Folder = "Assets/Art/Icon/";

        [MenuItem("Capy Island/App Icon")]
        public static void Apply()
        {
            var full = Load("Icon_Full");
            var fore = Load("Icon_Foreground");
            var back = Load("Icon_Background");
            if (full == null) { Debug.LogError("App icon: Assets/Art/Icon/Icon_Full.png is missing. Run Tools/ChatGPT/batch_icon.sh."); return; }

            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { full }, IconKind.Any);

            foreach (var target in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS })
            {
                foreach (var kind in PlayerSettings.GetSupportedIconKinds(target))
                {
                    var icons = PlayerSettings.GetPlatformIcons(target, kind);
                    bool adaptive = kind.ToString().Contains("Adaptive");
                    foreach (var icon in icons)
                    {
                        if (adaptive && fore != null && back != null) icon.SetTextures(back, fore);   // layer 0 behind, 1 in front
                        else icon.SetTexture(full);
                    }
                    PlayerSettings.SetPlatformIcons(target, kind, icons);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("App icon set for the default, Android (adaptive, round, legacy) and iOS.");
        }

        static Texture2D Load(string name)
        {
            string path = Folder + name + ".png";
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                bool dirty = importer.textureType != TextureImporterType.Default || importer.mipmapEnabled
                             || importer.textureCompression != TextureImporterCompression.Uncompressed;
                if (dirty)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize = 1024;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
