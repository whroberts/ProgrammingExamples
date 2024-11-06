#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateCityEngineMaterials : MonoBehaviour
{
    [SerializeField] private string textureImportPath = string.Empty;
    [SerializeField] private string materialSavePath = string.Empty;
    [SerializeField] private Material rootMaterial = null;

    private void Start()
    {
        AssetDatabase.StartAssetEditing();
        CreateMaterials();
        AssetDatabase.StopAssetEditing();
    }

    private void CreateMaterials ()
    {
        string appPath = Application.dataPath.Replace("Assets", "");
        var texturePaths = Directory.GetFiles(appPath + textureImportPath);

        int i = 1;
        foreach (var texturePath in texturePaths)
        {

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath.Replace(appPath,""));

            if (texture != null)
            {
                Material newMat = new Material(rootMaterial);
                newMat.name = "M_CityEngineMaterial_" + i;
                newMat.mainTexture = texture;

                AssetDatabase.CreateAsset(newMat, materialSavePath + "/" + newMat.name + ".mat");
                i++;
            }
        }
    }
}
#endif