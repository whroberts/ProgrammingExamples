#if UNITY_EDITOR
using BlueSky.Data;
using Esri.ArcGISMapsSDK.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueSky.Tools
{
    public class CreateBuildingPrefabs
    {
        private Dictionary<string, GameObject> modelPrefabs = new Dictionary<string, GameObject>();
        private List<GameObject> fbxPrefabs = new List<GameObject>();
        private ArcGISMapComponent arcGISMapComponent = null;
        private List<Features> features = new List<Features>();
        private Dictionary<GameObject, BuildingScriptableObject> createdBuildings = new Dictionary<GameObject, BuildingScriptableObject>();
        private List<BuildingScriptableObject> buildingsToDestroy = new List<BuildingScriptableObject>();

        private string buildingGOOutputPath = string.Empty;
        private string buildingSOOutputPath = string.Empty;
        private string buildingDatasetOutputPath = string.Empty;

        private BuildingDataset buildingDataset = null;
        private int noFeatureCount = 0;

        private List<Tuple<GameObject, string, BuildingScriptableObject, string>> buildingsAndSOByPath
            = new List<Tuple<GameObject, string, BuildingScriptableObject, string>>();
        private Dictionary<GameObject, BuildingScriptableObject> goBySO = new Dictionary<GameObject, BuildingScriptableObject>();

        public void CreatePrefabs(FeatureDataset featureData, List<GameObject> fbxPrefabs, string goPath, string soPath, BuildingDataset buildingDataset,
            ArcGISMapComponent arcGISMap, out Dictionary<GameObject, BuildingScriptableObject> goodBuildings, out List<BuildingScriptableObject> badBuildings)
        {
            modelPrefabs.Clear();
            this.fbxPrefabs.Clear();
            this.fbxPrefabs = fbxPrefabs; 
            arcGISMapComponent = arcGISMap;
            buildingGOOutputPath = goPath;
            buildingSOOutputPath = soPath;
            this.buildingDataset = AssetDatabase.LoadAssetAtPath<BuildingDataset>(AssetDatabase.GetAssetPath(buildingDataset));
            EditorUtility.SetDirty(this.buildingDataset);
            features = featureData.GetFeatures();

            CreateBuildings();

            goodBuildings = createdBuildings;
            badBuildings = buildingsToDestroy;

            //AssetDatabase.CreateAsset(buildingDataset, buildingDatasetOutputPath + "buildingsToLoad.asset");
            AssetDatabase.SaveAssets();
        }

        private void StoreModels(List<GameObject> prefabs)
        {
            foreach (var prefab in prefabs)
            {
                if (prefab.name.Contains("FBX__"))
                {
                    modelPrefabs.Add(prefab.name.Replace("FBX__", ""), prefab);
                }           
            }
        }

        private void CreateBuildings()
        {
            /*
            buildingDataset = ScriptableObject.CreateInstance<BuildingDataset>();
            EditorUtility.SetDirty(buildingDataset);
            buildingDataset.SetBuildingsToLoad(new List<GameObject>());
            buildingDataset.SetBuildingsWithoutFeatures(new List<GameObject>());
            */

            //AssetDatabase.StartAssetEditing();
            foreach (var model in this.fbxPrefabs)
            {
                StoreBuildingData(model);
            }
            //AssetDatabase.StopAssetEditing();

            List<string> prefabLocations = new List<string>();

            AssetDatabase.StartAssetEditing();
            foreach (var tuple in buildingsAndSOByPath)
            {
                //var building = pair.Value;
                var buildingSO = tuple.Item3;
                buildingSO.buildingGameObject = null;
                AssetDatabase.CreateAsset(buildingSO, tuple.Item4);
                PrefabUtility.SaveAsPrefabAssetAndConnect(tuple.Item1, tuple.Item2, InteractionMode.AutomatedAction, out bool success);

                if (success)
                {
                    prefabLocations.Add(tuple.Item2);
                    goBySO.Add(tuple.Item1, tuple.Item3);
                }
            }
            AssetDatabase.StopAssetEditing();
            /*            
            //EditorUtility.SetDirty(building);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(tuple.Item2);
            var building = AssetDatabase.LoadAssetAtPath<BuildingScriptableObject>(
                AssetDatabase.GetAssetPath(go.GetComponent<CreatedBuildingControl>().buildingSO));

            EditorUtility.SetDirty(building);
            building.buildingGameObject = go;
            AssetDatabase.SaveAssetIfDirty(building);
            */
            /*
            EditorUtility.SetDirty(buildingDataset);
            foreach (var prefab in prefabLocations)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
                if (go.GetType() == typeof(GameObject))
                {
                    var control = go.GetComponent<CreatedBuildingControl>();

                    if (control != null)
                    {
                        var so = control.buildingSO;
                        if (!so.hasFeature)
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
            */
            AssetDatabase.SaveAssetIfDirty(buildingDataset);
            AssetDatabase.SaveAssets();
        }

        private void StoreBuildingData(GameObject modelPrefab)
        {
            Features currentFeature = null;
            foreach (var feature in features)
            {
                if (feature.properties.Name != "")
                {
                    if (feature.properties.Name.Replace("/", "_").Replace("@", "_").Replace(".0", "") == modelPrefab.name.Replace("FBX__",""))
                    {
                        currentFeature = feature;
                        break;
                    }
                }
                else
                {
                    if (feature.properties.OSMId.ToString() == modelPrefab.name.Replace("FBX__way_",""))
                    {
                        currentFeature = feature;
                        break;
                    }
                }
            }

            BuildingScriptableObject building = null;
            if (currentFeature != null)
            {
                building = CreateBuildingScriptableObject(currentFeature, modelPrefab);
            }
            else
            {
                currentFeature = new Features();
                currentFeature.id = 100000 + noFeatureCount;
                noFeatureCount++;
                currentFeature.properties = new Properties();

                if (modelPrefab.name != string.Empty)
                {
                    currentFeature.properties.Name = "NO FEATURE " + modelPrefab.name;
                }
                else
                {
                    currentFeature.properties.Name = "NO FEATURE " + currentFeature.id;
                }
                currentFeature.properties.OSMId = currentFeature.id;
                currentFeature.properties.Latitude = 0;
                currentFeature.properties.Longitude = 0;

                building = CreateBuildingScriptableObject(currentFeature, modelPrefab);
            }

            //EditorUtility.SetDirty(building);

            if (!createdBuildings.ContainsValue(building))
            {
                try
                {
                    string buildingSOPath = buildingSOOutputPath + building.Name.Trim() + ".asset";
                    //buildingSOPath = AssetDatabase.GenerateUniqueAssetPath(buildingSOPath);
                    //AssetDatabase.CreateAsset(building, buildingSOPath);
                    var so = AssetDatabase.LoadAssetAtPath<BuildingScriptableObject>(buildingSOPath);
                    if (so != null)
                    {
                        AssetDatabase.DeleteAsset(buildingSOPath);
                        //AssetDatabase.SaveAssets();
                    }

                    try
                    {
                        //AssetDatabase.StartAssetEditing();
                        buildingSOPath = AssetDatabase.GenerateUniqueAssetPath(buildingSOPath);
                        //AssetDatabase.CreateAsset(building, buildingSOPath);

                        string buildingPrefabPath = buildingGOOutputPath + building.Name.Trim() + ".prefab";
                        //buildingPrefabPath = AssetDatabase.GenerateUniqueAssetPath(buildingPrefabPath);
                        //building.buildingGameObject.GetComponent<CreatedBuildingControl>().buildingSO = building;
                        //PrefabUtility.SaveAsPrefabAsset(building.buildingGameObject, buildingPrefabPath);
                        //AssetDatabase.StopAssetEditing();

                        //building = AssetDatabase.LoadAssetAtPath<BuildingScriptableObject>(AssetDatabase.GetAssetPath(building));
                        //EditorUtility.SetDirty(building);
                        //building.buildingGameObject = AssetDatabase.LoadAssetAtPath<GameObject>(buildingPrefabPath);

                        buildingsAndSOByPath.Add(Tuple.Create(building.buildingGameObject, buildingPrefabPath.Trim(), building, buildingSOPath.Trim()));
                        //createdBuildings.Add(building.buildingGameObject, building);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                    


                    //AssetDatabase.SaveAssets();


                    /*
                    if (!building.hasFeature)
                    {
                        if (!buildingDataset.GetBuildingsWithoutFeatures().Contains(building.buildingGameObject))
                        {
                            buildingDataset.SetBuildingsWithoutFeatures(building.buildingGameObject);
                        }
                        else
                        {
                            Debug.LogWarning("BuildingsWithoutFeatures already contains: " + building.buildingGameObject.name);
                        }
                    }
                    else
                    {
                        if (!buildingDataset.GetBuildingsToLoad().Contains(building.buildingGameObject))
                        {
                            buildingDataset.SetBuildingsToLoad(building.buildingGameObject);
                        }
                        else
                        {
                            Debug.LogWarning("BuildingsToLoad already contains: " + building.buildingGameObject.name);
                        }
                    }
                    */
                }
                catch (Exception e)
                {
                    Debug.LogError("Building: " + building.Name);
                    Debug.LogException(e);
                    return;
                }
            }
            else
            {
                buildingsToDestroy.Add(building);
            }
        }

        private BuildingScriptableObject CreateBuildingScriptableObject(Features feature, GameObject modelPrefab)
        {
            BuildingScriptableObject buildingSO = ScriptableObject.CreateInstance<BuildingScriptableObject>();
            buildingSO.ID = feature.id;
            buildingSO.uid = Guid.NewGuid().ToString();

            if (feature.properties.Name != "")
            {
                buildingSO.name = buildingSO.ID + ": " + feature.properties.Name;
                buildingSO.Name = feature.properties.Name;

                if (buildingSO.name.Contains("/"))
                {
                    buildingSO.name = buildingSO.name.Replace("/", "_");
                }
                if (buildingSO.Name.Contains("/"))
                {
                    buildingSO.Name = buildingSO.Name.Replace("/", "_");
                }
            }
            else if (feature.properties.OSMId > 0)
            {
                buildingSO.name = buildingSO.ID.ToString() + ": " + feature.properties.OSMId.ToString();
                buildingSO.Name = feature.properties.OSMId.ToString();

                if (buildingSO.name.Contains("/"))
                {
                    buildingSO.name = buildingSO.name.Replace("/", "_");
                }
                if (buildingSO.Name.Contains("/"))
                {
                    buildingSO.Name = buildingSO.Name.Replace("/", "_");
                }
            }

            if (feature.properties.Latitude == 0 || feature.properties.Longitude == 0)
            {
                buildingSO.hasFeature = false;
            }
            else
            {
                buildingSO.hasFeature = true;
            }
            buildingSO.feature = feature;
            buildingSO.latitude = feature.properties.Latitude;
            buildingSO.longitude = feature.properties.Longitude;
            buildingSO.highPolyModel = modelPrefab;

            try
            {
                if (buildingSO.highPolyModel != null)
                {
                    GameObject buildingGO = CreateBuildingPrefab(buildingSO);

                    try
                    {
                        if (buildingGO != null)
                        {
                            buildingSO.buildingGameObject = buildingGO;
                            SetupBuildingPrefab(buildingGO, buildingSO);
                        }
                    }
                    catch
                    {
                        Debug.LogError("Failed to set up building in world");
                    }

                }
            }
            catch
            {
                Debug.LogError("Failed to create building in world");
            }

            return buildingSO;
        }

        private GameObject CreateBuildingPrefab(BuildingScriptableObject buildingSO)
        {
            string customName = buildingSO.Name;

            GameObject createdBuilding = new GameObject(customName);
            //EditorUtility.SetDirty(createdBuilding);
            return createdBuilding;
        }

        private void SetupBuildingPrefab(GameObject buildingGameObject, BuildingScriptableObject buildingSO)
        {
            var buildingControl = buildingGameObject.AddComponent<CreatedBuildingControl>();
            //buildingControl.buildingSO = buildingSO;
            buildingControl.PlaceBuilding(arcGISMapComponent);
        }
    }

}
#endif