using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AssetsImportor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        var importer = (TextureImporter)assetImporter;
        var defaultSetting = importer.GetPlatformTextureSettings("Standalone");
        defaultSetting.overridden = true;
        defaultSetting.format = TextureImporterFormat.DXT5;
        importer.SetPlatformTextureSettings(defaultSetting);
        importer.maxTextureSize = 512;
        importer.npotScale = TextureImporterNPOTScale.ToLarger;
    }

    void OnPreprocessModel()
    {
        var importer = (ModelImporter)assetImporter;
        importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Local);
    }
}
