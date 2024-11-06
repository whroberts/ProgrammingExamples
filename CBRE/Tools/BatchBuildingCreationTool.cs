#if UNITY_EDITOR
using BlueSky.Data;
using Esri.ArcGISMapsSDK.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace BlueSky.Tools
{
    public class BatchBuildingCreationTool : EditorWindow
    {
        private Rect headerSection;
        private Rect creationSection;

        private Rect featureSection;
        private Rect fbxPrefabSection;
        private Rect buildingSection;
        private Rect completeSection;
        private Rect staticSection;

        private Rect[] baseSections;

        private Texture2D outlineTexture = null;

        //private static BuildingScriptableObject buildingSO;
        //public static BuildingScriptableObject BuildingSO => buildingSO;
        private static BatchBuildingCreationTool window = null;

        private FBXPrefabCreation fbxPrefabCreation;
        private GetBuildingInformation getBuildingInformation;
        private CreateBuildingPrefabs createBuildingPrefabs;
        private CreateCompleteBuildings createCompleteBuildings;

        private string featureDataImportPath = "";
        private string featureDataOutputPath = "";
        private string fbxImportPath = "";
        private string modelPrefabOutputPath = "";
        private string buildingGOOutputPath = "";
        private string buildingSOOutputPath = "";
        private string buildingDatasetOutputPath = "";
        private string buildingDatasetImportPath = "";
        //private string buildingDatasetOutputPath = "";

        //private string featureButtonMessage = string.Empty;
        private bool isFeatureCreating = false;

        private string fbxPrefabButtonMessage = string.Empty;
        private bool isFBXPrefabCreating = false;

        private bool isBuildingCreating = false;
        private bool isCompleteCreating = false;

        private FeatureDataset featureData = null;
        private BuildingDataset buildingDataset = null;
        //private string featureLayerName = string.Empty;

        private List<GameObject> loadedFBX = new List<GameObject>();

        private string buildingOutputPath = "";

        private string featureLayerURL = string.Empty;

        private Material fullHighlightDecalMat = null;
        private Material floorHighlightDecalMat = null;
        private GameObject fullHighlightPrefab = null;
        private GameObject floorHighlightPrefab = null;
        private bool allReferencesApplied = false;
        private ArcGISMapComponent arcGISMapComponent = null;


        [MenuItem("Buildings/Batch Building Creation")]
        private static void OpenWindow()
        {
            window = (BatchBuildingCreationTool)GetWindow(typeof(BatchBuildingCreationTool));
            window.titleContent = new GUIContent("Batch Building Creation");
            window.minSize = new Vector2(1200, 550);

            window.Show();
            AssetDatabase.ActiveRefreshImportMode = AssetDatabase.RefreshImportMode.OutOfProcessPerQueue;
            //AssetDatabase.DisallowAutoRefresh();
        }

        private void OnEnable()
        {
            baseSections = new Rect[] { headerSection, featureSection, completeSection }; //, staticSection };

            fbxPrefabCreation = new FBXPrefabCreation();
            getBuildingInformation = new GetBuildingInformation();
            createBuildingPrefabs = new CreateBuildingPrefabs();
            createCompleteBuildings = new CreateCompleteBuildings();

            outlineTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/outline.png");

            fullHighlightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SharedPrefabs/DecalProjector_FullHighlight.prefab");
            fullHighlightDecalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/M_FullHighlight.mat");

            floorHighlightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SharedPrefabs/DecalProjector_FloorHighlight.prefab");
            floorHighlightDecalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/M_FloorHighlight.mat");
        }

        private void OnGUI()
        {
            DrawBorders();
            DrawBaseSections();
            DrawHeaderSection();
            DrawFeatureSection();
            //DrawFbxPrefabSection();
            //DrawBuildingSection();
            DrawCompleteSection();
            //DrawCreationSection();
            //DrawStaticSection();
        }

        private void DrawBaseSections()
        {
            baseSections[0].x = 0;
            baseSections[0].y = 0;
            baseSections[0].width = Screen.width;
            baseSections[0].height = 50;

            float totalHeight = baseSections[0].height;
            for (int i = 1; i < baseSections.Length - 1; i++)
            {
                baseSections[i].x = 0;
                baseSections[i].y = totalHeight;
                baseSections[i].width = Screen.width;
                baseSections[i].height = 100;

                totalHeight += baseSections[i].height;
            }

            baseSections[baseSections.Length - 1].x = 0;
            baseSections[baseSections.Length - 1].y = totalHeight;
            baseSections[baseSections.Length - 1].width = Screen.width;
            baseSections[baseSections.Length - 1].height = 300;

            headerSection = baseSections[0];
            featureSection = baseSections[1];
            completeSection = baseSections[2];
            //staticSection = baseSections[3];

            //creationSection = baseSections[1];
        }

        private void DrawBorders()
        {
            GUIStyle style = new GUIStyle();
            style.normal.background = outlineTexture;
            GUI.Box(headerSection, GUIContent.none, style);
            GUI.Box(featureSection, GUIContent.none, style);
            //GUI.Box(fbxPrefabSection, GUIContent.none, style);
            //GUI.Box(buildingSection, GUIContent.none, style);
            GUI.Box(completeSection, GUIContent.none, style);
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

        private void DrawHeaderSection()
        {
            GUILayout.BeginArea(headerSection);
            GUILayout.Label("Create Buildings From Models");
            GUILayout.EndArea();
        }

        private void DrawFeatureSection()
        {
            GUILayout.BeginArea(featureSection);
            EditorGUILayout.BeginHorizontal();

            Rect createRectMessage = new Rect(0, 0, 250, featureSection.height / 3);
            Rect createRectButton = new Rect(createRectMessage.x, createRectMessage.height, 250, createRectMessage.height);

            Rect loadRectMessage = new Rect(255, 0, 250, featureSection.height / 3);
            Rect loadRectButton = new Rect(loadRectMessage.x, loadRectMessage.height, 250, loadRectMessage.height);

            Rect featureRectMessage = new Rect(510, 0, 250, featureSection.height / 3);
            Rect featureRectField = new Rect(featureRectMessage.x, featureRectMessage.height, 250, featureRectMessage.height);

            Rect featureURLMessage = new Rect(510, 0, 400, featureSection.height / 3);
            Rect featureURLInput = new Rect(featureURLMessage.x, featureURLMessage.height, 500, featureURLMessage.height);

            if (!isFeatureCreating)
            {
                if (featureDataOutputPath != string.Empty && featureLayerURL != string.Empty)
                {
                    EditorGUI.LabelField(createRectMessage, featureDataOutputPath);
                    if (GUI.Button(createRectButton, "Create Feature Dataset"))
                    {
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        EditorCoroutineUtility.StartCoroutine(CreateFeatureDataset(), window);
                    }
                }
                else
                {
                    EditorGUI.HelpBox(createRectMessage, "No location to save feature dataset", MessageType.Warning);
                    if (GUI.Button(createRectButton, "Set Location and Create"))
                    {
                        featureDataOutputPath = SplitForAssetDatabase(EditorUtility.OpenFolderPanel("Save feature dataset", "Assets/", ""));
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        EditorCoroutineUtility.StartCoroutine(CreateFeatureDataset(), window);
                    }
                }

                if (featureDataImportPath != string.Empty)
                {
                    EditorGUI.LabelField(loadRectMessage, featureDataImportPath);
                }

                featureData = (FeatureDataset)EditorGUI.ObjectField(loadRectButton, featureData, typeof(FeatureDataset), false);

                EditorGUI.LabelField(featureURLMessage, "Input Feature Layer URL\nInsure the ending is /FeatureServer/0/");
                featureLayerURL = EditorGUI.TextField(featureURLInput, featureLayerURL);

                if (featureData != null)
                {
                    featureDataImportPath = AssetDatabase.GetAssetPath(featureData);

                    string[] split = featureDataImportPath.Split("/" + featureData.name, StringSplitOptions.None);
                    featureDataOutputPath = split[0];
                }
            }
            else
            {
                EditorGUI.HelpBox(createRectMessage, "Creating...", MessageType.Info);
            }


            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();

        }

        private IEnumerator CreateFeatureDataset()
        {

            if (featureDataOutputPath != null)
            {
                isFeatureCreating = true;
                getBuildingInformation.CreateFeatureList(featureLayerURL);
                yield return new WaitUntil(() => getBuildingInformation.FinishedQuery);
                featureData = getBuildingInformation.SaveFeatureDataset(featureDataOutputPath, out featureDataImportPath);
                isFeatureCreating = false;
            }
            else
            {
                Debug.LogError("Failed to set path");



            }


        }

        private void DrawBuildingSection()
        {
            GUILayout.BeginArea(buildingSection);
            EditorGUILayout.BeginVertical();

            Rect mainMessage = new Rect(0, 0, 250, buildingSection.height / 4);
            Rect mainButton = new Rect(mainMessage.x, mainMessage.height, 250, mainMessage.height);

            Rect fbxOutputRectMessage = new Rect(255, 0, 250, buildingSection.height / 6);
            Rect fbxOutputRectButton = new Rect(fbxOutputRectMessage.x, fbxOutputRectMessage.height, 250, fbxOutputRectMessage.height);

            Rect buildingGOOutputMessage = new Rect(255, (fbxOutputRectButton.height + fbxOutputRectButton.y) + 25, 250, buildingSection.height / 6);
            Rect buildingGOOutputButton = new Rect(buildingGOOutputMessage.x, buildingGOOutputMessage.height + buildingGOOutputMessage.y, 250, buildingGOOutputMessage.height);

            Rect buildingSOOutputMessage = new Rect(510, 0, 250, buildingSection.height / 6);
            Rect buildingSOOutputButton = new Rect(buildingSOOutputMessage.x, buildingSOOutputMessage.height, 250, buildingSOOutputMessage.height);

            Rect buildingDatasetOutputMessage = new Rect(510, (buildingSOOutputButton.height + buildingSOOutputButton.y) + 25, 250, buildingSection.height / 6);
            Rect buildingDatasetOutputButton = new Rect(buildingDatasetOutputMessage.x, buildingDatasetOutputMessage.height + buildingDatasetOutputMessage.y, 250, buildingDatasetOutputMessage.height);
            Rect buildingDatasetObjectField = new Rect(buildingDatasetOutputMessage.x, buildingDatasetOutputButton.height + buildingDatasetOutputButton.y, 250, buildingDatasetOutputMessage.height);

            if (featureData != null)
            {
                if (featureData.GetFeatures().Count <= 0)
                {
                    EditorGUI.HelpBox(mainMessage, "Feature Dataset is empty", MessageType.Error);
                }
                else
                {
                    if (!isBuildingCreating)
                    {
                        if (modelPrefabOutputPath != string.Empty && buildingGOOutputPath != string.Empty && buildingSOOutputPath != string.Empty && buildingDatasetOutputPath != string.Empty)
                        {
                            //EditorGUI.HelpBox(mainMessage, "Feature Dataset is empty", MessageType.Error);
                            if (GUI.Button(mainButton, "Create Building Prefabs"))
                            {
                                CreateBuildingPrefabs();

                                /*
                                isBuildingCreating = true;
                                ArcGISMapComponent arcGISMapComponent = FindObjectOfType<ArcGISMapComponent>();

                                createBuildingPrefabs.CreatePrefabs(featureData, loadedFBX, buildingGOOutputPath + "/", buildingSOOutputPath + "/", buildingDataset,
                                    arcGISMapComponent, out Dictionary<GameObject, BuildingScriptableObject> createdBuildings, out List<BuildingScriptableObject> buildingsToDestroy);

                                foreach (var building in buildingsToDestroy)
                                {
                                    DestroyImmediate(building.buildingGameObject);
                                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(building));
                                }
                                buildingsToDestroy.Clear();

                                foreach (var building in arcGISMapComponent.GetComponentsInChildren<CreatedBuildingControl>())
                                {
                                    DestroyImmediate(building.gameObject);
                                }
                                isBuildingCreating = false;
                                */
                            }
                        }

                        if (GUI.Button(fbxOutputRectButton, "Set Location"))
                        {
                            modelPrefabOutputPath = EditorUtility.OpenFolderPanel("Save Model Prefabs", "Assets/Prefabs/", "");

                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();

                            if (loadedFBX.Count <= 0)
                            {
                                List<GameObject> tempList = new List<GameObject>();

                                var files = Directory.GetFiles(modelPrefabOutputPath + "/");

                                foreach (var file in files)
                                {
                                    GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(SplitForAssetDatabase(file));
                                    if (fbx != null)
                                    {
                                        tempList.Add(fbx);
                                    }
                                }
                                loadedFBX = tempList;
                            }

                        }

                        if (modelPrefabOutputPath != string.Empty)
                        {
                            EditorGUI.LabelField(fbxOutputRectMessage, SplitForAssetDatabase(modelPrefabOutputPath));
                        }
                        else
                        {
                            EditorGUI.HelpBox(fbxOutputRectMessage, "No location set to open fbx prefabs", MessageType.Warning);
                        }

                        if (GUI.Button(buildingGOOutputButton, "Set Location"))
                        {
                            buildingGOOutputPath = SplitForAssetDatabase(EditorUtility.OpenFolderPanel("Save Buildings", "Assets/Buildings/", ""));
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        }

                        if (buildingGOOutputPath != string.Empty)
                        {
                            EditorGUI.LabelField(buildingGOOutputMessage, buildingGOOutputPath);
                        }
                        else
                        {
                            EditorGUI.HelpBox(buildingGOOutputMessage, "No location set to save buildings", MessageType.Warning);
                        }

                        if (GUI.Button(buildingSOOutputButton, "Set Location"))
                        {
                            buildingSOOutputPath = SplitForAssetDatabase(EditorUtility.OpenFolderPanel("Save Building Data", "Assets/ScriptableObjects/", ""));
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        }

                        if (buildingSOOutputPath != string.Empty)
                        {
                            EditorGUI.LabelField(buildingSOOutputMessage, buildingSOOutputPath);
                        }
                        else
                        {
                            EditorGUI.HelpBox(buildingSOOutputMessage, "No location set to save building data", MessageType.Warning);
                        }

                        /*
                        if (GUI.Button(buildingDatasetOutputButton, "Set Location And Create") && buildingDataset == null)
                        {
                            buildingDatasetOutputPath = SplitForAssetDatabase(EditorUtility.OpenFolderPanel("Save Building Dataset", "Assets/ScriptableObjects/", ""));
                            CreateBuildingDataset();
                        }

                        EditorGUI.ObjectField(buildingDatasetObjectField, buildingDataset, typeof(BuildingDataset), false);

                        if (buildingDatasetOutputPath != string.Empty)
                        {
                            EditorGUI.LabelField(buildingDatasetOutputMessage, buildingDatasetOutputPath);
                        }
                        else
                        {
                            EditorGUI.HelpBox(buildingDatasetOutputMessage, "No location set to save building dataset", MessageType.Warning);
                        }
                        */


                        if (buildingDatasetImportPath != string.Empty)
                        {
                            EditorGUI.LabelField(buildingDatasetOutputMessage, buildingDatasetImportPath);
                        }
                        else
                        {
                            EditorGUI.LabelField(buildingDatasetOutputMessage, "No location set to save building dataset");
                        }

                        if (buildingDataset == null)
                        {
                            if (GUI.Button(buildingDatasetOutputButton, "Set Location And Create"))
                            {
                                buildingDatasetOutputPath = SplitForAssetDatabase(EditorUtility.OpenFolderPanel("Save Building Dataset", "Assets/ScriptableObjects/", ""));

                                AssetDatabase.SaveAssets();
                                AssetDatabase.Refresh();

                                if (buildingDatasetOutputPath != string.Empty)
                                {
                                    //CreateBuildingDataset();
                                }
                            }
                            buildingDataset = (BuildingDataset)EditorGUI.ObjectField(buildingDatasetObjectField, buildingDataset, typeof(BuildingDataset), false);
                        }
                        else
                        {
                            buildingDataset = (BuildingDataset)EditorGUI.ObjectField(buildingDatasetOutputButton, buildingDataset, typeof(BuildingDataset), false);
                            buildingDatasetImportPath = AssetDatabase.GetAssetPath(buildingDataset);

                            string[] split = buildingDatasetImportPath.Split("/" + buildingDataset.name, StringSplitOptions.None);
                            buildingDatasetOutputPath = split[0];
                        }
                    }
                    else
                    {
                        EditorGUI.HelpBox(mainMessage, "Creating...", MessageType.Info);
                    }
                }
            }
            else
            {
                EditorGUI.HelpBox(mainMessage, "Feature Dataset is null", MessageType.Error);
            }

            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void CreateBuildingPrefabs()
        {
            try
            {
                //Must Be Both


                isBuildingCreating = true;
                ArcGISMapComponent arcGISMapComponent = FindObjectOfType<ArcGISMapComponent>();


                createBuildingPrefabs.CreatePrefabs(featureData, loadedFBX, buildingGOOutputPath + "/", buildingSOOutputPath + "/", buildingDataset,
                    arcGISMapComponent, out Dictionary<GameObject, BuildingScriptableObject> createdBuildings, out List<BuildingScriptableObject> buildingsToDestroy);

                /*
                foreach (var building in buildingsToDestroy)
                {
                    DestroyImmediate(building.buildingGameObject);
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(building));
                }
                buildingsToDestroy.Clear();
                */

                foreach (var building in arcGISMapComponent.GetComponentsInChildren<CreatedBuildingControl>())
                {
                    DestroyImmediate(building.gameObject);
                }
                isBuildingCreating = false;

                //Must Be Both


            }
            catch (Exception e)
            {
                Debug.LogError("Error Creating Building Prefabs");
                Debug.LogException(e);



            }
        }

        private void DrawCompleteSection()
        {
            GUILayout.BeginArea(completeSection);
            EditorGUILayout.BeginHorizontal();

            Rect mainMessage = new Rect(0, 0, 250, completeSection.height / 3);
            Rect mainButton = new Rect(mainMessage.x, mainMessage.height, 250, mainMessage.height);

            Rect fbxImportRectMessage = new Rect(255, 0, 250, completeSection.height / 6);
            Rect fbxImportRectButton = new Rect(fbxImportRectMessage.x, fbxImportRectMessage.height, 250, fbxImportRectMessage.height);

            Rect buildingOutputRectMessage = new Rect(510, 0, 250, completeSection.height / 6);
            Rect buildingOutputRectButton = new Rect(buildingOutputRectMessage.x, buildingOutputRectMessage.height, 250, buildingOutputRectMessage.height);

            Rect buildingDatasetOutputMessage = new Rect(255, (fbxImportRectButton.height + fbxImportRectButton.y) + 25, 250, completeSection.height / 6);
            Rect buildingDatasetOutputButton = new Rect(buildingDatasetOutputMessage.x, buildingDatasetOutputMessage.height + buildingDatasetOutputMessage.y, 250, buildingDatasetOutputMessage.height);

            Rect referencesSection1 = new Rect(510, (buildingOutputRectButton.height + buildingOutputRectButton.y) + 25, 500, completeSection.height / 10);
            Rect referencesSection2 = new Rect(510, (referencesSection1.height + referencesSection1.y), 500, referencesSection1.height);
            Rect referencesSection3 = new Rect(510, (referencesSection2.height + referencesSection2.y), 500, referencesSection1.height);
            Rect referencesSection4 = new Rect(510, (referencesSection3.height + referencesSection3.y), 500, referencesSection1.height);

            arcGISMapComponent = FindObjectOfType<ArcGISMapComponent>();

            if (featureData != null)
            {
                if (!isCompleteCreating)
                {
                    if (fbxImportPath != string.Empty && buildingOutputPath != string.Empty && buildingDatasetOutputPath != string.Empty &&
                        allReferencesApplied && arcGISMapComponent != null)
                    {
                        EditorGUI.HelpBox(mainMessage, fbxPrefabButtonMessage, MessageType.Info);
                        if (GUI.Button(mainButton, "Click to Create Complete Buildings"))
                        {
                            //EditorCoroutineUtility.StartCoroutine(LoadAndCreateFBXPrefabs(), window);
                            CreateCompletePrefabs();
                        }
                    }
                    else
                    {
                        EditorGUI.HelpBox(mainMessage, "Set locations to create prefabs", MessageType.Error);
                    }

                    if (GUI.Button(fbxImportRectButton, "Set Location"))
                    {
                        fbxImportPath = EditorUtility.OpenFolderPanel("Load FBX", "Assets/Models/", "");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }

                    if (fbxImportPath != string.Empty)
                    {
                        EditorGUI.LabelField(fbxImportRectMessage, SplitForAssetDatabase(fbxImportPath));
                        if (modelPrefabOutputPath != string.Empty && !isFBXPrefabCreating)
                        {
                            fbxPrefabButtonMessage = "Waiting to Create";
                        }
                    }
                    else
                    {

                        EditorGUI.HelpBox(fbxImportRectMessage, "No location set to import fbx", MessageType.Warning);
                    }

                    if (GUI.Button(buildingOutputRectButton, "Set Location"))
                    {
                        buildingOutputPath = EditorUtility.OpenFolderPanel("Save Building Prefabs", "Assets/Buildings/", "");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }

                    if (buildingOutputPath != string.Empty)
                    {
                        EditorGUI.LabelField(buildingOutputRectMessage, SplitForAssetDatabase(buildingOutputPath));
                        if (fbxImportPath != string.Empty && !isCompleteCreating)
                        {
                            fbxPrefabButtonMessage = "Waiting to Create";
                        }
                    }
                    else
                    {
                        EditorGUI.HelpBox(buildingOutputRectMessage, "No location set to save building prefabs", MessageType.Warning);
                    }

                    if (buildingDataset == null)
                    {
                        if (GUI.Button(buildingDatasetOutputButton, "Set Location And Create"))
                        {
                            buildingDatasetOutputPath = SplitForAssetDatabase(EditorUtility.OpenFolderPanel("Save Building Dataset", "Assets/ScriptableObjects/", ""));

                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();

                            if (buildingDatasetOutputPath != string.Empty)
                            {
                                CreateBuildingDataset();
                            }
                        }
                        buildingDataset = (BuildingDataset)EditorGUI.ObjectField(buildingDatasetOutputMessage, buildingDataset, typeof(BuildingDataset), false);
                    }
                    else
                    {
                        buildingDataset = (BuildingDataset)EditorGUI.ObjectField(buildingDatasetOutputButton, buildingDataset, typeof(BuildingDataset), false);
                        buildingDatasetImportPath = AssetDatabase.GetAssetPath(buildingDataset);

                        string[] split = buildingDatasetImportPath.Split("/" + buildingDataset.name, StringSplitOptions.None);
                        buildingDatasetOutputPath = split[0];
                    }

                    fullHighlightDecalMat = (Material)EditorGUI.ObjectField(referencesSection1, "Full Highlight Decal Material", fullHighlightDecalMat, typeof(Material), false);
                    floorHighlightDecalMat = (Material)EditorGUI.ObjectField(referencesSection2, "Floor Highlight Decal Material", floorHighlightDecalMat, typeof(Material), false);
                    fullHighlightPrefab = (GameObject)EditorGUI.ObjectField(referencesSection3, "Full Highlight Decal Prefab", fullHighlightPrefab, typeof(GameObject), false);
                    floorHighlightPrefab = (GameObject)EditorGUI.ObjectField(referencesSection4, "Floor Highlight Decal Prefab", floorHighlightPrefab, typeof(GameObject), false);

                    if (fullHighlightDecalMat != null && floorHighlightDecalMat != null && fullHighlightPrefab != null && floorHighlightPrefab != null)
                    {
                        allReferencesApplied = true;
                    }
                }
                else
                {
                    EditorGUI.HelpBox(mainMessage, fbxPrefabButtonMessage, MessageType.Info);
                }
            }
            else
            {
                EditorGUI.HelpBox(mainMessage, "Feature Dataset is null", MessageType.Error);
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawStaticSection()
        {
            GUILayout.BeginArea(staticSection);
            EditorGUILayout.BeginHorizontal();

            Rect mainMessage = new Rect(0, 0, 250, staticSection.height / 3);
            Rect mainButton = new Rect(mainMessage.x, mainMessage.height, 250, mainMessage.height);

            if (GUI.Button(mainButton, "Set Location"))
            {
                string path = EditorUtility.OpenFolderPanel("Buildings To Set To Static", "Assets/Buildings/", "");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                AssetDatabase.StartAssetEditing();
                var files = Directory.GetFiles(path);

                foreach (var file in files)
                {
                    GameObject building = AssetDatabase.LoadAssetAtPath<GameObject>(SplitForAssetDatabase(file));
                    if (building != null)
                    {
                        //EditorUtility.SetDirty(building);
                        //building.isStatic = true;
                        List<Transform> transformList = building.GetComponentsInChildren<Transform>().ToList();

                        GameObjectUtility.SetStaticEditorFlags(building, StaticEditorFlags.BatchingStatic);

                        foreach (var trans in transformList)
                        {
                            GameObjectUtility.SetStaticEditorFlags(trans.gameObject, StaticEditorFlags.BatchingStatic);
                        }
                        //AssetDatabase.SaveAssetIfDirty(building);
                        AssetDatabase.SaveAssets();
                    }
                }

                AssetDatabase.StopAssetEditing();
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void CreateCompletePrefabs()
        {

            isCompleteCreating = true;
            fbxPrefabButtonMessage = "Creating...";

            try
            {
                createCompleteBuildings.CreateBuildings(featureData, fbxImportPath + "/", buildingOutputPath + "/", buildingDataset,
                    arcGISMapComponent, fullHighlightDecalMat, floorHighlightDecalMat, fullHighlightPrefab, floorHighlightPrefab, out List<GameObject> toDestroy);

                foreach (var building in toDestroy)
                {
                    DestroyImmediate(building);
                }

                AssetDatabase.SaveAssetIfDirty(buildingDataset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                AssetDatabase.SaveAssetIfDirty(buildingDataset);

            }
            catch (Exception e)
            {
                Debug.LogError("Error Creating Building Prefabs");
                Debug.LogException(e);
            }
        }

        private BuildingDataset CreateBuildingDataset()
        {
            try
            {
                //Must Be Both


                buildingDataset = ScriptableObject.CreateInstance<BuildingDataset>();
                EditorUtility.SetDirty(buildingDataset);

                buildingDataset.SetBuildingsToLoad(new List<GameObject>());
                buildingDataset.SetBuildingsWithoutFeatures(new List<GameObject>());

                AssetDatabase.CreateAsset(buildingDataset, buildingDatasetOutputPath + "/" + featureData.name.Replace("_gdb", "") + "_buildingsToLoad.asset");
                AssetDatabase.SaveAssets();
                buildingDatasetImportPath = AssetDatabase.GetAssetPath(buildingDataset);

                //Must Be Both


            }
            catch (Exception e)
            {
                Debug.Log("Error Creating Building Dataset");
                Debug.LogException(e);
            }

            return buildingDataset;
        }

        private void OnDestroy()
        {
            AssetDatabase.SaveAssets();
            fbxPrefabCreation = null;
            getBuildingInformation = null;
            createBuildingPrefabs = null;
            //AssetDatabase.AllowAutoRefresh();
        }
    }
}
#endif