#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BaseballTyping.Editor
{
    /// <summary>
    /// Keeps every Kenney texture crisp: point filtering, no compression, and consistent
    /// pixels-per-unit. UI frames also get the 9-slice border their thick outline needs.
    /// </summary>
    public sealed class KenneyImportSettings : AssetPostprocessor
    {
        private const int WorldPixelsPerUnit = 18;
        private const int UiOutlinePixels = 8;

        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("Resources/Kenney/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = WorldPixelsPerUnit;

            if (assetPath.Contains("/Ui/"))
            {
                importer.spriteBorder = new Vector4(UiOutlinePixels, UiOutlinePixels, UiOutlinePixels, UiOutlinePixels);
            }
        }

        private void OnPreprocessAsset()
        {
            if (assetImporter is not TrueTypeFontImporter fontImporter) return;
            if (!assetPath.Contains("Resources/Fonts/")) return;

            // Hinted raster keeps the pixel fonts from blurring at runtime sizes.
            fontImporter.fontRenderingMode = FontRenderingMode.HintedRaster;
            fontImporter.fontTextureCase = FontTextureCase.Dynamic;
        }
    }
}
#endif
