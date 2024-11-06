#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlueSky.Tools
{
    public class FBXPrefabCreation
    {
        private List<GameObject> createdModelPrefabs = new List<GameObject>();
        public List<GameObject> CreatedModelPrefabs => createdModelPrefabs;

        private Dictionary<GameObject, string> gameObjectsAndPaths = new Dictionary<GameObject, string>();

        public void LoadFBXModelsFromPath(List<GameObject> loadedModels, out List<GameObject> scenePrefabs, string outPath) //string inPath, out List<GameObject> scenePrefabs, string outPath)
        {
            scenePrefabs = new List<GameObject>();

            if (loadedModels.Count <= 0)
            {
                Debug.LogError("No FBX found at path");
                return;
            }

            if (Directory.Exists(outPath))
            {
                foreach (var fbx in loadedModels)
                {
                    if (fbx.GetType() == typeof(GameObject))
                    {
                        CreatePrefabFromFBX(fbx, outPath, out GameObject createdPrefab);
                        //createdModelPrefabs.Add(createdPrefab);
                    }
                    else
                    {
                        return;
                    }
                }

                AssetDatabase.StartAssetEditing();
                foreach (var pair in gameObjectsAndPaths)
                {
                    try
                    {
                        //PrefabUtility.SaveAsPrefabAssetAndConnect(fbxPrefab, localPath, InteractionMode.AutomatedAction, out bool success);
                        PrefabUtility.SaveAsPrefabAsset(pair.Key, pair.Value, out bool success);

                        if (success)
                        {
                            createdModelPrefabs.Add(pair.Key);
                        }
                        else
                        {
                            Debug.LogWarning("Prefab failed to save: " + pair.Key.name);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.Log(e);
                    }
                }
                AssetDatabase.StopAssetEditing();
                //AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                scenePrefabs = createdModelPrefabs;
            }
            else
            {
                Debug.LogError("Invalid path to save model prefabs");
                return;
            }

        }

        private void CreatePrefabFromFBX(Object fbxAsset, string outputPath, out GameObject createdPrefab)
        {
            createdPrefab = null;

            var name = fbxAsset.name;
            if (name.Contains("FBX"))
            {
                name.Replace("FBX", "PFB");
            }
            GameObject fbxPrefab = new GameObject(name);
            EditorUtility.SetDirty(fbxPrefab);
            GameObject modelTransform = new GameObject("ModelTransformOffset");
            modelTransform.transform.parent = fbxPrefab.transform;
            GameObject loadedFBX = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, modelTransform.transform);
            loadedFBX.transform.localPosition = Vector3.zero;
            loadedFBX.transform.localEulerAngles = Vector3.zero;

            if (name.Contains("/"))
            {
                name = name.Replace("/", "_");
            }

            string localPath = outputPath + name + ".prefab";

            //localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

            gameObjectsAndPaths.Add(fbxPrefab, localPath);

            /*
            PrefabUtility.SaveAsPrefabAssetAndConnect(fbxPrefab, localPath, InteractionMode.AutomatedAction, out bool success);

            if (success)
            {
                createdPrefab = fbxPrefab;
            }
            else
            {
                Debug.LogWarning("Prefab failed to save: " + fbxPrefab.name);
                createdPrefab = null;
            }
            */
        }
    }
}
#endif