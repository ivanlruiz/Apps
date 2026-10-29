using UnityEditor;

namespace BichiKart.Editor
{
    /// <summary>
    /// Cómo se importan los modelos de los bichitos hechos en Blender (Resources/Bichitos): sin animaciones,
    /// cámaras ni luces, y con los materiales con su nombre de Blender ("#color|aspereza|brillo|laca"), que
    /// el juego lee para armarlos con sus propios materiales.
    /// </summary>
    public sealed class ImportarBichitos : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.Contains("/Resources/Bichitos/")) return;
            var importador = (ModelImporter)assetImporter;
            importador.importAnimation = false;
            importador.animationType = ModelImporterAnimationType.None;
            importador.importCameras = false;
            importador.importLights = false;
            importador.importBlendShapes = false;
            importador.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importador.bakeAxisConversion = true;
        }
    }
}
