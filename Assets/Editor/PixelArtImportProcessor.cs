using UnityEditor;
using UnityEngine;

// Forces every texture under Assets/Art/Kenney to import as a crisp,
// unfiltered, uncompressed pixel-art sprite (16px = 1 world unit).
public class PixelArtImportProcessor : AssetPostprocessor
{
    private const string WatchedFolder = "Assets/Art/Kenney";

    private void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains(WatchedFolder))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;

        var settings = importer.GetDefaultPlatformTextureSettings();
        settings.format = TextureImporterFormat.RGBA32;
        settings.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(settings);
    }
}
