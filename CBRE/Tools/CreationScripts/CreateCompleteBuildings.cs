#if UNITY_EDITOR
using BlueSky.Data;
using Esri.ArcGISMapsSDK.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace BlueSky.Tools
{
    public class CreateCompleteBuildings
    {
        private ArcGISMapComponent arcGISMapComponent = null;
        private List<Features> features = new List<Features>();
        private Dictionary<GameObject, string> createdBuildings = new Dictionary<GameObject, string>();
        private List<BuildingScriptableObject> buildingsToDestroy = new List<BuildingScriptableObject>();

        private string buildingGOOutputPath = string.Empty;

        private BuildingDataset buildingDataset = null;
        private int noFeatureCount = 0;
        private Material fullHighlightDecalMat = null;
        private Material floorHighlightDecalMat = null;
        private GameObject fullHighlightPrefab = null;
        private GameObject floorHighlightPrefab = null;

        public void CreateBuildings(FeatureDataset featureData, string fbxPath, string goPath, BuildingDataset buildingDataset,
            ArcGISMapComponent arcGISMap, Material fullMat, Material floorMat, GameObject fullPrefab, GameObject floorPrefab,
            out List<GameObject> toDestroy)
        {
            //modelPrefabs.Clear();
            //this.fbxPrefabs = fbxPrefabs;
            arcGISMapComponent = arcGISMap;
            buildingGOOutputPath = goPath;
            //this.buildingDataset = AssetDatabase.LoadAssetAtPath<BuildingDataset>(AssetDatabase.GetAssetPath(buildingDataset));
            //buildingDataset = CreateBuildingDataset(datasetPath);
            this.buildingDataset = AssetDatabase.LoadAssetAtPath<BuildingDataset>(AssetDatabase.GetAssetPath(buildingDataset));
            EditorUtility.SetDirty(this.buildingDataset);
            features = featureData.GetFeatures();

            fullHighlightDecalMat = fullMat;
            floorHighlightDecalMat = floorMat;
            fullHighlightPrefab = fullPrefab;
            floorHighlightPrefab = floorPrefab;


            List<GameObject> loadedModels = LoadFBXModelsFromPath(fbxPath);

            if (loadedModels != null)
            {
                foreach (var model in loadedModels)
                {
                    CreatePrefab(model);
                }

            }

            AssetDatabase.StartAssetEditing();
            foreach (var pair in createdBuildings)
            {
                var exsiting = AssetDatabase.LoadAssetAtPath<GameObject>(SplitForAssetDatabase(pair.Value));
                if (exsiting != null)
                {
                    AssetDatabase.DeleteAsset(pair.Value);
                    //AssetDatabase.SaveAssets();
                }

                PrefabUtility.SaveAsPrefabAssetAndConnect(pair.Key, pair.Value, InteractionMode.AutomatedAction);
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            toDestroy = createdBuildings.Keys.ToList();

            EditorUtility.SetDirty(buildingDataset);
            foreach (var prefab in createdBuildings.Values)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(SplitForAssetDatabase(prefab));
                if (go.GetType() == typeof(GameObject))
                {
                    var control = go.GetComponent<CreatedBuildingControl>();

                    if (control != null)
                    {
                        var data = control.buildingDataClass;
                        if (!data.HasFeature)
                        {
                            if (!buildingDataset.GetBuildingsWithoutFeatures().Contains(go))
                            {
                                buildingDataset.SetBuildingsWithoutFeatures(go);
                            }
                            else
                            {
                                Debug.LogWarning("BuildingsWithoutFeatures already contains: " + go.name);
                            }
                        }
                        else
                        {
                            if (!buildingDataset.GetBuildingsToLoad().Contains(go))
                            {
                                buildingDataset.SetBuildingsToLoad(go);
                            }
                            else
                            {
                                Debug.LogWarning("BuildingsToLoad already contains: " + go.name);
                            }
                        }
                    }

                }
            }

            AssetDatabase.SaveAssetIfDirty(buildingDataset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AssetDatabase.SaveAssets();
        }

        public void CreatePrefab(GameObject model)
        {
            Features currentFeature = null;
            foreach (var feature in features)
            {
                if (feature.properties.Name != "")
                {
                    if (feature.properties.Name.Replace("/", "_").Replace("@", "_").Replace(".0", "") == model.name.Replace("FBX__", ""))
                    {
                        currentFeature = feature;
                        break;
                    }
                }
                else
                {
                    if (feature.properties.OSMId.ToString() == model.name.Replace("FBX__way_", ""))
                    {
                        currentFeature = feature;
                        break;
                    }
                }
            }

            GameObject buildingGO = new GameObject();
            EditorUtility.SetDirty(buildingGO);
            GameObject modelRoot = new GameObject(model.name);
            modelRoot.transform.parent = buildingGO.transform;
            GameObject modelTransform = new GameObject("ModelTransformOffset");
            modelTransform.transform.parent = modelRoot.transform;
            GameObject loadedModel = (GameObject)PrefabUtility.InstantiatePrefab(model, modelTransform.transform);
            loadedModel.transform.localPosition = Vector3.zero;
            loadedModel.transform.localEulerAngles = Vector3.zero;
            CreatedBuildingControl createdBuildingControl = buildingGO.AddComponent<CreatedBuildingControl>();

            BuildingDataClass buildingDataClass = null;

            if (currentFeature != null)
            {
                buildingDataClass = CreateBuildingData(currentFeature, buildingGO);
            }
            else
            {
                currentFeature = new Features();
                currentFeature.id = 100000 + noFeatureCount;
                noFeatureCount++;
                currentFeature.properties = new Properties();

                if (model.name != string.Empty)
                {
                    currentFeature.properties.Name = "NO FEATURE " + model.name;
                }
                else
                {
                    currentFeature.properties.Name = "NO FEATURE " + currentFeature.id;
                }
                currentFeature.properties.OSMId = currentFeature.id;
                currentFeature.properties.Latitude = 0;
                currentFeature.properties.Longitude = 0;

                //buildingData = CreateBuildingData(currentFeature, buildingData);
                buildingDataClass = CreateBuildingData(currentFeature, buildingGO);
            }

            if (buildingGO != null)
            {
                //createdBuildingControl.buildingDataClass = buildingData;
                buildingGO.name = buildingDataClass.Name;
                createdBuildingControl.highPolyModel = modelRoot;
                //createdBuildingControl.fullHighlightDecalMaterial = fullHighlightDecalMat;
                //createdBuildingControl.floorHighlightDecalMaterial = floorHighlightDecalMat;
                //createdBuildingControl.fullHighlightPrefab = fullHighlightPrefab;
                //createdBuildingControl.floorHighlightPrefab = floorHighlightPrefab;
                createdBuildingControl.PlaceBuilding(arcGISMapComponent);
            }

            /*
            List<Transform> transformList = buildingGO.GetComponentsInChildren<Transform>().ToList();

            GameObjectUtility.SetStaticEditorFlags(buildingGO, StaticEditorFlags.BatchingStatic);

            foreach (var trans in transformList)
            {
                GameObjectUtility.SetStaticEditorFlags(trans.gameObject, StaticEditorFlags.BatchingStatic);
            }
            */

            if (!createdBuildings.ContainsKey(buildingGO))
            {
                createdBuildings.Add(buildingGO, buildingGOOutputPath + buildingDataClass.Name.Trim() + ".prefab");
            }
        }

        public List<GameObject> LoadFBXModelsFromPath(string path) //string inPath, out List<GameObject> scenePrefabs, string outPath)
        {
            List<GameObject> tempList = new List<GameObject>();

            var files = Directory.GetFiles(path, "*.fbx", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(SplitForAssetDatabase(file));
                if (fbx != null)
                {
                    tempList.Add(fbx);
                }
            }

            if (tempList.Count <= 0)
            {
                Debug.LogError("No FBX found at path");
                return null;
            }

            return tempList;
        }

        private BuildingDataClass CreateBuildingData(Features feature, GameObject buildingGO)
        {
            BuildingDataClass createdDataClass = buildingGO.AddComponent<BuildingDataClass>();

            //BuildingScriptableObject buildingSO = ScriptableObject.CreateInstance<BuildingScriptableObject>();
            createdDataClass.ID = feature.id;
            createdDataClass.UUID = Guid.NewGuid().ToString();

            if (feature.properties.Name != "")
            {
                //buildingData.name = buildingData.ID + ": " + feature.properties.Name;
                createdDataClass.Name = feature.properties.Name;

                /*
                if (buildingData.name.Contains("/"))
                {
                    buildingData.name = buildingData.name.Replace("/", "_");
                }
                */

                if (createdDataClass.Name.Contains("/"))
                {
                    createdDataClass.Name = createdDataClass.Name.Replace("/", "_");
                }
            }
            else if (feature.properties.OSMId > 0)
            {
                //buildingSO.name = buildingSO.ID.ToString() + ": " + feature.properties.OSMId.ToString();
                createdDataClass.Name = feature.properties.OSMId.ToString();

                /*
                if (buildingData.name.Contains("/"))
                {
                    buildingData.name = buildingData.name.Replace("/", "_");
                }
                */
                if (createdDataClass.Name.Contains("/"))
                {
                    createdDataClass.Name = createdDataClass.Name.Replace("/", "_");
                }
            }

            if (feature.properties.Latitude == 0 || feature.properties.Longitude == 0)
            {
                createdDataClass.HasFeature = false;
            }
            else
            {
                createdDataClass.HasFeature = true;
            }
            createdDataClass.Feature = feature;
            createdDataClass.Latitude = feature.properties.Latitude;
            createdDataClass.Longitude = feature.properties.Longitude;
            createdDataClass.Elevation = feature.properties.Elevation;
            createdDataClass.BuildingHighlightLayer = -1;

            var control = buildingGO.GetComponent<CreatedBuildingControl>();

            if (control != null)
            {
                control.buildingDataClass = createdDataClass;
            }

            return createdDataClass;
        }


        private string SplitForAssetDatabase(string fullPath, string splitAt = "BlueSkyUnity/")
        {
            if (fullPath != string.Empty)
            {
                string[] splitPath = fullPath.Split(splitAt, System.StringSplitOptions.None);
                return splitPath[1];
            }
            else return fullPath;
        }

        private BuildingDataset CreateBuildingDataset(string path)
        {
            try
            {
                buildingDataset = ScriptableObject.CreateInstance<BuildingDataset>();
                EditorUtility.SetDirty(buildingDataset);

                buildingDataset.SetBuildingsToLoad(new List<GameObject>());
                buildingDataset.SetBuildingsWithoutFeatures(new List<GameObject>());

                string saveTo = SplitForAssetDatabase(path) + "buildingsToLoad.asset";

                var exsiting = AssetDatabase.LoadAssetAtPath<BuildingDataset>(saveTo);
                if (exsiting != null)
                {
                    //saveTo = AssetDatabase.GenerateUniqueAssetPath(saveTo);
                    AssetDatabase.DeleteAsset(saveTo);
                    //AssetDatabase.SaveAssets();
                }

                AssetDatabase.CreateAsset(buildingDataset, saveTo);
                AssetDatabase.SaveAssets();
                //buildingDatasetImportPath = AssetDatabase.GetAssetPath(buildingDataset);
            }
            catch (Exception e)
            {
                Debug.Log("Error Creating Building Dataset");
                Debug.LogException(e);
            }

            return buildingDataset;
        }
    }
}
#endif