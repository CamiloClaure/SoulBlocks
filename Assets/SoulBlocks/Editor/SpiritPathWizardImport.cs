using UnityEditor;

namespace SoulBlocks.Editor
{
    // Import settings for the Blender-exported Spirit Path wizard (Resources/Characters): bake
    // Blender's axis conversion so the model stands upright with an identity root, and Legacy
    // clips so SoulPlayer can loop Idle/Walk on an Animation component without an
    // AnimatorController asset.
    public sealed class SpiritPathWizardImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.Contains("TheSpiritPath_Wizard")) return;
            var importer = (ModelImporter)assetImporter;
            importer.bakeAxisConversion = true;
            importer.animationType = ModelImporterAnimationType.Legacy;
        }
    }
}
