#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class Sprite64Importer : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        // Only touch sprites in this folder tree
        if (!assetPath.StartsWith("Assets/Art/Sprites/", System.StringComparison.OrdinalIgnoreCase))
            return;

        var ti = (TextureImporter)assetImporter;

        // Core sprite import settings
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 64f;                 // 1 tile = 64 px
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.alphaIsTransparency = true;
        ti.sRGBTexture = true;

        // Set alignment+pivot via TextureImporterSettings (portable across Unity versions)
        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);

        bool isDoor = assetPath.IndexOf("/Doors/", System.StringComparison.OrdinalIgnoreCase) >= 0;

        settings.spriteAlignment = (int)(isDoor ? SpriteAlignment.Custom : SpriteAlignment.Center);
        settings.spritePivot     = isDoor ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
        // (We intentionally don't touch fallback physics shape—API changes across versions.)

        ti.SetTextureSettings(settings);
    }
}
#endif


