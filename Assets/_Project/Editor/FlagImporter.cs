using UnityEditor;

namespace GeoPuzzle.Editor
{
    /// <summary>Imports every PNG in the flags folder as a UI sprite with the same settings.</summary>
    public class FlagImporter : AssetPostprocessor
    {
        public const string FlagsFolder = "Assets/_Project/Art/Flags";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(FlagsFolder + "/"))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 256;
            importer.textureCompression = TextureImporterCompression.Compressed;
        }
    }
}
