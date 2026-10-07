using UnityEditor;
using UnityEngine;

namespace CapyIsland.EditorTools
{
    /// <summary>
    /// Gives every newly added texture under Assets/Art the project's sprite settings, so generated
    /// art is correct the moment it lands: Sprite, 128 pixels per unit, bilinear, no mipmaps,
    /// crunched on WebGL, capped at its own size. Props, trees, buildings and characters pivot at
    /// their feet; particle sprites and UI icons pivot at their centre.
    ///
    /// Only touches files importing for the first time, so settings changed by hand stick.
    /// </summary>
    public class ArtImportRules : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            // Wardrobe art loads at runtime from Resources, so it lives outside Assets/Art.
            bool wardrobe = assetPath.StartsWith("Assets/Resources/Wardrobe/");
            bool hobbies = assetPath.StartsWith("Assets/Resources/Hobbies/");
            if (!assetPath.StartsWith("Assets/Art/") && !wardrobe && !hobbies) return;
            if (!assetImporter.importSettingsMissing) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            // Outfit pieces sit centred on their anchor; the bare capybara stands on its feet like the king.
            bool centred = assetPath.StartsWith("Assets/Art/FX/") || assetPath.StartsWith("Assets/Art/UI/")
                           || hobbies || (wardrobe && !assetPath.EndsWith("/Capy_Player.png"));
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)(centred ? SpriteAlignment.Center : SpriteAlignment.BottomCenter);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            var web = importer.GetPlatformTextureSettings("WebGL");
            web.overridden = true;
            web.maxTextureSize = 256;
            web.crunchedCompression = true;
            web.compressionQuality = 60;
            web.format = TextureImporterFormat.Automatic;
            importer.SetPlatformTextureSettings(web);
        }
    }
}
