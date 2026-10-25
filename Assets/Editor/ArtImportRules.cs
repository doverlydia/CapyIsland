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
            bool cutscene = assetPath.StartsWith("Assets/Resources/Cutscenes/")    // full-stage paintings
                            || assetPath.StartsWith("Assets/Resources/Events/");   // and the event banners
            bool world = assetPath.StartsWith("Assets/Resources/World/");      // runtime-built world pieces (the hot spring)
            bool home = assetPath.StartsWith("Assets/Resources/Home/");        // your burrow, its furniture and shop
            // Full-screen paintings drawn big in the UI: the kitchen, the burrow, the tunnel.
            bool painting = assetPath.EndsWith("/Ema_Rack.png") || assetPath.EndsWith("/Shrine_Backdrop.png") || assetPath.EndsWith("/Cook_Kitchen.png") || assetPath.EndsWith("/Burrow_Cave.png") || assetPath.EndsWith("/Burrow_Room.png") || assetPath.EndsWith("/Tunnel.png") || assetPath.EndsWith("/Sew_Room.png");
            bool big = assetPath.EndsWith("/Cook_Stove.png") || assetPath.EndsWith("/Cook_Pot.png") || assetPath.EndsWith("/Store_Furniture.png") || assetPath.EndsWith("/Burrow_Door.png") || assetPath.EndsWith("/Burrow_Arch.png")
                       || assetPath.Contains("/Vignette_") || assetPath.EndsWith("/Sew_Mannequin.png") || assetPath.EndsWith("/Shrine.png");   // drawn big in the hobby carousel and sewing room
            if (!assetPath.StartsWith("Assets/Art/") && !wardrobe && !hobbies && !cutscene && !world && !home) return;
            if (assetPath.StartsWith("Assets/Art/Icon/")) return;   // app icons: full size, uncompressed, set by the App Icon menu
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
                           || (hobbies && !assetPath.EndsWith("/Monster_Happy.png") && !assetPath.Contains("/Sleep_") && !assetPath.EndsWith("/Sailor.png") && !assetPath.EndsWith("/Mochi.png") && !assetPath.EndsWith("/Shrine.png") && !assetPath.Contains("/Miso_Dress")) || cutscene || world || home || (wardrobe && !assetPath.EndsWith("/Capy_Player.png"));
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)(centred ? SpriteAlignment.Center : SpriteAlignment.BottomCenter);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            var web = importer.GetPlatformTextureSettings("WebGL");
            web.overridden = true;
            // The burrow room is a wide panorama the camera walks along: it keeps its width.
            web.maxTextureSize = assetPath.EndsWith("/Burrow_Room.png") ? 2048 : cutscene || painting ? 1024 : world || big ? 512 : 256;
            web.crunchedCompression = true;
            web.compressionQuality = 60;
            web.format = TextureImporterFormat.Automatic;
            importer.SetPlatformTextureSettings(web);
        }
    }
}
