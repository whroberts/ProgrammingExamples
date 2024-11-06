
using BlueSky.Data;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using System.Data;
using UnityEngine.ResourceManagement.ResourceLocations;
using BlueSky.BuildingManipulation;

[RequireComponent(typeof(BuildingPrefabManager))]
public class PrefabLoading : MonoBehaviour
{
    [Header("BuildingsToLoad")]
    //[SerializeField] private List<BuildingDataset> buildingDatasets = new List<BuildingDataset>();
    private List<BuildingDataset> buildingDatasetsToLoad = new List<BuildingDataset>();
    [SerializeField] private BuildingDataset futureBuildingsDataset = null;

    [Header("Generation Controls")]
    [SerializeField] private HideFlags hideFlags = HideFlags.None;

    [Flags]
    private enum GenerationFlags
    {
        None = 0x0,
        HighPolyModels = 0x1,
        Highlights = 0x3,
        Colliders = 0x5,
    }
    [SerializeField] private GenerationFlags generationFlags = GenerationFlags.None;

    [Flags]
    private enum BuildingFlags
    {
        None = 0x0,
        MakeStatic = 0x1,
        StaticBatching = 0x2,
        AllColliders = 0x4,
    }
    [SerializeField] private BuildingFlags buildingFlags = BuildingFlags.MakeStatic;

    [Flags]
    private enum SaveFlags
    {
        None = 0x0,
        DoNotSave = 0x1,
        SaveBuildings01 = 0x2,
        SaveBuildings02 = 0x4,
        SaveBuildings03 = 0x8,
        SaveBuildings04 = 0x10,
    }
    [Tooltip("LITERALLY DO NOT TOUCH IF YOU DO NOT KNOW WHAT YOU ARE DOING")]
    [SerializeField] private SaveFlags saveFlags = SaveFlags.DoNotSave;

    /*
    [SerializeField] private bool createHighPolyModels = true;
    [SerializeField] private bool createHighlights = true;
    [SerializeField] private bool createColliders = true;
    */

    [SerializeField] private bool createJSONFileForDatabase = false;
    [SerializeField] private bool createJSONFileForStorage = false;

    private Dictionary<string, GameObject> buildingsByUID = new Dictionary<string, GameObject>();
    public Dictionary<string, GameObject> BuildingsByUID => buildingsByUID;

    private Dictionary<string, GameObject> futureBuildingsByUID = new Dictionary<string, GameObject>();
    public Dictionary<string, GameObject> FutureBuildingsByUID => futureBuildingsByUID;

    private List<CreatedBuildingControl> createdBuildingControls = new List<CreatedBuildingControl>();

    [Header("Decals")]
    [SerializeField] private GameObject fullHighlightDecal = null;
    public GameObject FullHighlightDecal => fullHighlightDecal;
    [SerializeField] private GameObject floorHighlightDecal = null;
    public GameObject FloorHighlightDecal => floorHighlightDecal;
    [SerializeField] private Material fullHighlightDecalMat = null;
    public Material FullHighlightDecalMat => fullHighlightDecalMat;
    [SerializeField] private Material floorHighlightDecalMat = null;
    public Material FloorHighlightDecalMat => floorHighlightDecalMat;

    private BuildingPrefabManager prefabManager = null;
    private int numBuildingsLoaded = 0;

    private int numBuildingsToLoad = 0;
    public int NumBuildingsToLoad => numBuildingsToLoad;

    private float numBuildingsPositioned = 0;
    public float NumBuildingsPositioned => numBuildingsPositioned;

    private List<GameObject> duplicateBuildingsToDestroy = new List<GameObject>();

    public delegate void BuildingsLoadedEventHandler();
    public event BuildingsLoadedEventHandler OnCreateHighPolyModels;
    public event BuildingsLoadedEventHandler OnSetInitCullDistances;
    public event BuildingsLoadedEventHandler OnCreateLowPolyModels;
    public event BuildingsLoadedEventHandler OnCreateHighlights;
    public event BuildingsLoadedEventHandler OnSetLocation;
    public event BuildingsLoadedEventHandler OnCreateColliders;
    public event BuildingsLoadedEventHandler OnFinishBuildingSetup;
    //public event BuildingsLoadedEventHandler OnDestroyComponents;
    public event BuildingsLoadedEventHandler OnFullMapCreated;
    public event BuildingsLoadedEventHandler OnStartLoadingBuildings;
    public event BuildingsLoadedEventHandler OnOptimizeBuildings;
    public event BuildingsLoadedEventHandler OnSaveBuildings;

    public delegate void GeneratingBuildingsEventHandler(string message);
    public event GeneratingBuildingsEventHandler OnSendLogMessage;

    private int numLoops;

    private AsyncOperationHandle<IList<BuildingDataset>> loadHandleDatasets;

    private AsyncOperationHandle<IList<GameObject>> loadHandleGameObjects;
    private List<GameObject> loadedGameObjects = new List<GameObject>();

    private void Awake()
    {
        prefabManager = GetComponent<BuildingPrefabManager>();

        //Screen.SetResolution(1920, 1080, false);

#if !UNITY_EDITOR
        saveFlags = SaveFlags.DoNotSave;
#endif
    }


    private void OnEnable()
    {
#if UNITY_EDITOR
        //AssetDatabase.StartAssetEditing();
#endif
        prefabManager.AuthenticationHelper.OnBaseMapLoaded += new AuthenticationHelper.BaseMapLoadedEventHandler(CreateBuildings);
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        //AssetDatabase.StopAssetEditing();
#endif
        prefabManager.AuthenticationHelper.OnBaseMapLoaded -= new AuthenticationHelper.BaseMapLoadedEventHandler(CreateBuildings);

        saveFlags = SaveFlags.DoNotSave;
    }

    public void CreateBuildings()
    {
        StartCoroutine(LoadAllBuildingsFromDataset());
        //StartCoroutine(LoadAllBuildingsFromGameObjects());
    }

    private IEnumerator LoadAllBuildingsFromDataset()
    {
        try
        {
            List<string> keys = new List<string>() { ApiConstants.market, "BuildingDataset" };

            loadHandleDatasets = Addressables.LoadAssetsAsync<BuildingDataset>(
                keys,
                addressable =>
                {
                    buildingDatasetsToLoad.Add(addressable);
                },
                Addressables.MergeMode.Intersection,
                true);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load building datasets");
            Debug.LogException(e);
        }

        yield return new WaitUntil(() => loadHandleDatasets.IsDone);

        //yield return new WaitUntil(() => buildingDatasetsToLoad.Count > 0);

        buildingsByUID.Clear();
        futureBuildingsByUID.Clear();
        createdBuildingControls.Clear();
        numBuildingsLoaded = 0;
        numBuildingsToLoad = 0;
        numBuildingsPositioned = 0;

        while (ArcGisUtility.arcGISMapComponent.View.SpatialReference == null)
        {
            yield return null;
        }
        if (loadHandleDatasets.Result.Count > 0)
        {
            foreach (var dataset in loadHandleDatasets.Result)
            {
                numBuildingsToLoad += dataset.GetDatasetLength();
            }

            if (futureBuildingsDataset != null)
            {
                numBuildingsToLoad += futureBuildingsDataset.GetDatasetLength();
            }

            if (OnStartLoadingBuildings != null)
            {
                OnStartLoadingBuildings();
            }

            if (OnSendLogMessage != null)
            {
                OnSendLogMessage("Beginning Map Creation");
            }

            if (!generationFlags.HasFlag(GenerationFlags.None) || generationFlags.HasFlag(GenerationFlags.HighPolyModels))
            {
                Debug.Log("Beginning Map Creation");
                //load main datasets
                float piece = 0f;
                foreach (var dataset in loadHandleDatasets.Result)
                {
                    var buildingPrefabs = dataset.GetBuildingsToLoad();
                    //var buildingPrefabs = dataset.GetBuildingsWithoutFeatures();

                    var modValue = 2500;
                    foreach (var building in buildingPrefabs)
                    {
                        if (building != null)
                        {
                            var control = building.GetComponent<CreatedBuildingControl>();
                            if (control != null)
                            {

                                CreateBuildingsFromPrefab(building, false);

                                numBuildingsLoaded += 1;
                                if (numBuildingsLoaded % modValue == 0)
                                {
                                    //Debug.LogAssertion("Still Loading Buildings: " + numBuildingsLoaded);
                                    //yield return new WaitForSecondsRealtime(0.001f);

                                    if (OnSendLogMessage != null)
                                    {
                                        OnSendLogMessage("Loading...");
                                    }

                                    yield return new WaitForEndOfFrame();
                                    yield return new WaitForEndOfFrame();
                                    yield return new WaitForEndOfFrame();

                                    piece = 0.1667f * (numBuildingsLoaded - numBuildingsPositioned);

                                    if (generationFlags.HasFlag(GenerationFlags.HighPolyModels) && OnCreateHighPolyModels != null)
                                    {
                                        OnCreateHighPolyModels();

                                        yield return new WaitUntil(() => OnCreateHighPolyModels == null);
                                    }
                                    numBuildingsPositioned += piece;

                                    /*
                                    if (OnCreateHighPolyModels != null && createHighPolyModels)
                                    {
                                        //Debug.Log("Creating High Poly Models");
                                        OnCreateHighPolyModels();

                                        yield return new WaitUntil(() => OnCreateHighPolyModels == null);
                                        //Debug.Log("Done Creating High Poly Models");
                                    }

                                    numBuildingsPositioned += piece;
                                    */

                                    if (generationFlags.HasFlag(GenerationFlags.Highlights) && OnCreateHighlights != null)
                                    {
                                        OnCreateHighlights();

                                        yield return new WaitUntil(() => OnCreateHighlights == null);
                                    }
                                    numBuildingsPositioned += piece;

                                    /*
                                    if (OnCreateHighlights != null && createHighlights)
                                    {
                                        //Debug.Log("Creating Highlights");
                                        OnCreateHighlights();

                                        yield return new WaitUntil(() => OnCreateHighlights == null);
                                        //Debug.Log("Done Creating Highlights");
                                    }
                                    numBuildingsPositioned += piece;
                                    */

                                    if (generationFlags.HasFlag(GenerationFlags.Colliders) && OnCreateColliders != null)
                                    {
                                        OnCreateColliders();

                                        yield return new WaitUntil(() => OnCreateColliders == null);
                                    }
                                    numBuildingsPositioned += piece;

                                    /*
                                    if (OnCreateColliders != null && createColliders)
                                    {
                                        //Debug.Log("Creating Colliders");
                                        OnCreateColliders();

                                        yield return new WaitUntil(() => OnCreateColliders == null);
                                        //Debug.Log("Done Creating Colliders");
                                    }
                                    numBuildingsPositioned += piece;
                                    */

                                    if (OnSetLocation != null)
                                    {
                                        //Debug.Log("Setting Location");
                                        OnSetLocation();

                                        yield return new WaitUntil(() => OnSetLocation == null);
                                        //Debug.Log("Done Setting Building Locations");
                                    }
                                    numBuildingsPositioned += piece;
                                    numBuildingsPositioned += piece;

                                    if (OnSaveBuildings != null)
                                    {
                                        Debug.Log("Saving Buildings");
                                        OnSaveBuildings();

                                        yield return new WaitUntil(() => OnSaveBuildings == null);
                                        Debug.Log("Done Saving Buildings");
                                    }

                                    numLoops++;
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError("Missing Prefab");
                        }
                    }

                    piece = 0.1f * (numBuildingsLoaded - numBuildingsPositioned);

                    if (generationFlags.HasFlag(GenerationFlags.HighPolyModels) && OnCreateHighPolyModels != null)
                    {
                        OnCreateHighPolyModels();

                        yield return new WaitUntil(() => OnCreateHighPolyModels == null);
                    }
                    numBuildingsPositioned += piece;

                    /*
                    if (OnCreateHighPolyModels != null && createHighPolyModels)
                    {
                        //Debug.Log("Creating High Poly Models");
                        OnCreateHighPolyModels();

                        yield return new WaitUntil(() => OnCreateHighPolyModels == null);
                        //Debug.Log("Done Creating High Poly Models");
                    }

                    numBuildingsPositioned += piece;
                    */

                    if (generationFlags.HasFlag(GenerationFlags.Highlights) && OnCreateHighlights != null)
                    {
                        OnCreateHighlights();

                        yield return new WaitUntil(() => OnCreateHighlights == null);
                    }
                    numBuildingsPositioned += piece;

                    /*
                    if (OnCreateHighlights != null && createHighlights)
                    {
                        //Debug.Log("Creating Highlights");
                        OnCreateHighlights();

                        yield return new WaitUntil(() => OnCreateHighlights == null);
                        //Debug.Log("Done Creating Highlights");
                    }
                    numBuildingsPositioned += piece;
                    */

                    if (generationFlags.HasFlag(GenerationFlags.Colliders) && OnCreateColliders != null)
                    {
                        OnCreateColliders();

                        yield return new WaitUntil(() => OnCreateColliders == null);
                    }
                    numBuildingsPositioned += piece;

                    /*
                    if (OnCreateColliders != null && createColliders)
                    {
                        //Debug.Log("Creating Colliders");
                        OnCreateColliders();

                        yield return new WaitUntil(() => OnCreateColliders == null);
                        //Debug.Log("Done Creating Colliders");
                    }
                    numBuildingsPositioned += piece;
                    */

                    if (OnSetLocation != null)
                    {
                        //Debug.Log("Setting Location");
                        OnSetLocation();

                        yield return new WaitUntil(() => OnSetLocation == null);
                        //Debug.Log("Done Setting Building Locations");
                    }
                    numBuildingsPositioned += piece;

                    numBuildingsPositioned += piece;

                    if (OnSaveBuildings != null)
                    {
                        Debug.Log("Saving Buildings");
                        OnSaveBuildings();

                        yield return new WaitUntil(() => OnSaveBuildings == null);
                        Debug.Log("Done Saving Buildings");
                    }

                    //Debug.Log("Done Loading Buildings");
                }

                if (OnSendLogMessage != null)
                {
                    OnSendLogMessage("Almost done!");
                }

                //load future building dataset
                if (futureBuildingsDataset != null)
                {
                    var futureBuildings = futureBuildingsDataset.GetBuildingsToLoad();
                    //var futureBuildings = futureBuildingsDataset.GetBuildingsWithoutFeatures();

                    if (futureBuildings != null)
                    {
                        if (OnSendLogMessage != null)
                        {
                            OnSendLogMessage("Optimizing...");
                        }

                        var modValue2 = 500;
                        foreach (var prefab in futureBuildings)
                        {
                            if (prefab != null)
                            {
                                var control = prefab.GetComponent<CreatedBuildingControl>();
                                if (control != null)
                                {

                                    CreateBuildingsFromPrefab(prefab, true);

                                    numBuildingsLoaded += 1;
                                    if (numBuildingsLoaded % modValue2 == 0)
                                    {
                                        //Debug.LogAssertion("Still Loading Buildings: " + numBuildingsLoaded);
                                        //yield return new WaitForSecondsRealtime(0.001f);

                                        yield return new WaitForEndOfFrame();
                                        yield return new WaitForEndOfFrame();
                                        yield return new WaitForEndOfFrame();
                                        //yield return new WaitForEndOfFrame();
                                        //yield return new WaitForEndOfFrame();

                                        numBuildingsPositioned += (numBuildingsToLoad - numBuildingsLoaded) / 2;
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogError("Missing Prefab");
                            }
                        }


                        if (generationFlags.HasFlag(GenerationFlags.HighPolyModels) && OnCreateHighPolyModels != null)
                        {
                            OnCreateHighPolyModels();

                            yield return new WaitUntil(() => OnCreateHighPolyModels == null);
                        }

                        /*
                        if (OnCreateHighPolyModels != null && createHighPolyModels)
                        {
                            //Debug.Log("Creating High Poly Models");
                            OnCreateHighPolyModels();

                            yield return new WaitUntil(() => OnCreateHighPolyModels == null);
                            //Debug.Log("Done Creating High Poly Models");
                        }

                        numBuildingsPositioned += piece;
                        */

                        if (generationFlags.HasFlag(GenerationFlags.Highlights) && OnCreateHighlights != null)
                        {
                            OnCreateHighlights();

                            yield return new WaitUntil(() => OnCreateHighlights == null);
                        }

                        /*
                        if (OnCreateHighlights != null && createHighlights)
                        {
                            //Debug.Log("Creating Highlights");
                            OnCreateHighlights();

                            yield return new WaitUntil(() => OnCreateHighlights == null);
                            //Debug.Log("Done Creating Highlights");
                        }
                        numBuildingsPositioned += piece;
                        */

                        if (generationFlags.HasFlag(GenerationFlags.Colliders) && OnCreateColliders != null)
                        {
                            OnCreateColliders();

                            yield return new WaitUntil(() => OnCreateColliders == null);
                        }

                        /*
                        if (OnCreateColliders != null && createColliders)
                        {
                            //Debug.Log("Creating Colliders");
                            OnCreateColliders();

                            yield return new WaitUntil(() => OnCreateColliders == null);
                            //Debug.Log("Done Creating Colliders");
                        }
                        numBuildingsPositioned += piece;
                        */

                        if (OnSetLocation != null)
                        {
                            //Debug.Log("Setting Location");
                            OnSetLocation();

                            yield return new WaitUntil(() => OnSetLocation == null);
                            //Debug.Log("Done Setting Building Locations");
                        }

                        if (OnSaveBuildings != null)
                        {
                            Debug.Log("Saving Buildings");
                            OnSaveBuildings();

                            yield return new WaitUntil(() => OnSaveBuildings == null);
                            Debug.Log("Done Saving Buildings");
                        }
                    }
                }

                Debug.Log("All Buildings Created");

                if (OnFinishBuildingSetup != null)
                {
                    Debug.Log("Finishing Setup");
                    OnFinishBuildingSetup();

                    yield return new WaitUntil(() => OnFinishBuildingSetup == null);
                    //Debug.Log("Done Finishing Setup");
                }

                numBuildingsPositioned = numBuildingsToLoad;

                if (OnSendLogMessage != null)
                {
                    OnSendLogMessage("Making it look pretty!");
                }
            }
           

            if (OnFullMapCreated != null)
            {
                Debug.Log("On Full Map Created");
                OnFullMapCreated();

                //yield return new WaitUntil(() => OnFullMapCreated == null);
                //Debug.Log("Post Processing Enabled");
            }


            if (OnSaveBuildings != null)
            {
                Debug.Log("Saving Buildings");
                OnSaveBuildings();

                yield return new WaitUntil(() => OnSaveBuildings == null);
                Debug.Log("Done Saving Buildings");
            }
            else
            {
                Debug.Log("No Buildings To Save");
            }

            //SceneManager.UnloadSceneAsync(0);

            #region JSON File
#if UNITY_EDITOR
            if (createJSONFileForDatabase)
            {
                ReturnForDatabase();
            }
            else if (createJSONFileForStorage)
            {
                ReturnForStorage();
            }
#endif
#endregion
        }
        else
        {
            Debug.LogError("No BuildingDataset Found. Cannot load buildings");

            if (OnFullMapCreated != null)
            {
                Debug.Log("On Full Map Created");
                OnFullMapCreated();

                //yield return new WaitUntil(() => OnFullMapCreated == null);
                //Debug.Log("Post Processing Enabled");
            }
        }

        bool success = WipeDuplicateBuildings();

        if (success)
        {
            Debug.Log("Wiped Duplicates");
        }
        //Addressables.Release(loadHandleDatasets);
    }

    private IEnumerator LoadAllBuildingsFromGameObjects()
    {
        while (ArcGisUtility.arcGISMapComponent.View.SpatialReference == null)
        {
            yield return null;
        }

        try
        {
            List<string> keys = new List<string>() { ApiConstants.market, "BuildingPrefab" };

            loadHandleGameObjects = Addressables.LoadAssetsAsync<GameObject>(
                keys,
                addressable =>
                {
                    loadedGameObjects.Add(addressable);
                },
                Addressables.MergeMode.Intersection,
                true);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load building datasets");
            Debug.LogException(e);
        }

        yield return new WaitUntil(() => loadHandleGameObjects.IsDone);

        yield return new WaitUntil(() => loadedGameObjects.Count > 0);

        buildingsByUID.Clear();
        futureBuildingsByUID.Clear();
        createdBuildingControls.Clear();
        numBuildingsLoaded = 0;
        numBuildingsToLoad = 0;
        numBuildingsPositioned = 0;

        while (ArcGisUtility.arcGISMapComponent.View.SpatialReference == null)
        {
            yield return null;
        }
        if (loadedGameObjects.Count > 0)
        {
            numBuildingsToLoad = loadedGameObjects.Count;

            if (futureBuildingsDataset != null)
            {
                numBuildingsToLoad += futureBuildingsDataset.GetDatasetLength();
            }

            if (OnStartLoadingBuildings != null)
            {
                OnStartLoadingBuildings();
            }

            if (OnSendLogMessage != null)
            {
                OnSendLogMessage("Beginning Map Creation");
            }

            if (!generationFlags.HasFlag(GenerationFlags.None) || generationFlags.HasFlag(GenerationFlags.HighPolyModels))
            {
                Debug.Log("Beginning Map Creation");
                //load main datasets
                float piece = 0f;

                foreach (var building in loadedGameObjects)
                {
                    var modValue = 2500;
                    if (building != null)
                    {
                        string n = building.name.ToLower();

                        if (n.Length > 4)
                        {
                            if (n[0] == 'n' &&
                                n[1] == 'o' &&
                                n[2] == ' ' &&
                                n[3] == 'f')
                            {
                                //No Feature Building
                            }
                            else
                            {
                                CreateBuildingsFromPrefab(building, false);
                                numBuildingsLoaded += 1;
                            }
                        }
                        else
                        {
                            CreateBuildingsFromPrefab(building, false);
                            numBuildingsLoaded += 1;
                        }

                        if (numBuildingsLoaded % modValue == 0)
                        {
                            //Debug.LogAssertion("Still Loading Buildings: " + (numBuildingsToLoad - numBuildingsLoaded));
                            //yield return new WaitForSecondsRealtime(0.001f);

                            if (OnSendLogMessage != null)
                            {
                                OnSendLogMessage("Loading...");
                            }

                            yield return new WaitForEndOfFrame();
                            yield return new WaitForEndOfFrame();
                            yield return new WaitForEndOfFrame();

                            piece = 0.1667f * (numBuildingsLoaded - numBuildingsPositioned);

                            if (generationFlags.HasFlag(GenerationFlags.HighPolyModels) && OnCreateHighPolyModels != null)
                            {
                                OnCreateHighPolyModels();

                                yield return new WaitUntil(() => OnCreateHighPolyModels == null);
                            }
                            numBuildingsPositioned += piece;

                            /*
                            if (OnCreateHighPolyModels != null && createHighPolyModels)
                            {
                                //Debug.Log("Creating High Poly Models");
                                OnCreateHighPolyModels();

                                yield return new WaitUntil(() => OnCreateHighPolyModels == null);
                                //Debug.Log("Done Creating High Poly Models");
                            }

                            numBuildingsPositioned += piece;
                            */

                            if (generationFlags.HasFlag(GenerationFlags.Highlights) && OnCreateHighlights != null)
                            {
                                OnCreateHighlights();

                                yield return new WaitUntil(() => OnCreateHighlights == null);
                            }
                            numBuildingsPositioned += piece;

                            /*
                            if (OnCreateHighlights != null && createHighlights)
                            {
                                //Debug.Log("Creating Highlights");
                                OnCreateHighlights();

                                yield return new WaitUntil(() => OnCreateHighlights == null);
                                //Debug.Log("Done Creating Highlights");
                            }
                            numBuildingsPositioned += piece;
                            */

                            if (generationFlags.HasFlag(GenerationFlags.Colliders) && OnCreateColliders != null)
                            {
                                OnCreateColliders();

                                yield return new WaitUntil(() => OnCreateColliders == null);
                            }
                            numBuildingsPositioned += piece;

                            /*
                            if (OnCreateColliders != null && createColliders)
                            {
                                //Debug.Log("Creating Colliders");
                                OnCreateColliders();

                                yield return new WaitUntil(() => OnCreateColliders == null);
                                //Debug.Log("Done Creating Colliders");
                            }
                            numBuildingsPositioned += piece;
                            */

                            if (OnSetLocation != null)
                            {
                                //Debug.Log("Setting Location");
                                OnSetLocation();

                                yield return new WaitUntil(() => OnSetLocation == null);
                                //Debug.Log("Done Setting Building Locations");
                            }
                            numBuildingsPositioned += piece;
                            numBuildingsPositioned += piece;

                            if (OnSaveBuildings != null)
                            {
                                Debug.Log("Saving Buildings");
                                OnSaveBuildings();

                                yield return new WaitUntil(() => OnSaveBuildings == null);
                                Debug.Log("Done Saving Buildings");
                            }

                            numLoops++;
                        }
                    }
                    else
                    {
                        Debug.LogError("Missing Prefab");
                    }
                }

                piece = 0.1f * (numBuildingsLoaded - numBuildingsPositioned);

                if (generationFlags.HasFlag(GenerationFlags.HighPolyModels) && OnCreateHighPolyModels != null)
                {
                    OnCreateHighPolyModels();

                    yield return new WaitUntil(() => OnCreateHighPolyModels == null);
                }
                numBuildingsPositioned += piece;

                /*
                if (OnCreateHighPolyModels != null && createHighPolyModels)
                {
                    //Debug.Log("Creating High Poly Models");
                    OnCreateHighPolyModels();

                    yield return new WaitUntil(() => OnCreateHighPolyModels == null);
                    //Debug.Log("Done Creating High Poly Models");
                }

                numBuildingsPositioned += piece;
                */

                if (generationFlags.HasFlag(GenerationFlags.Highlights) && OnCreateHighlights != null)
                {
                    OnCreateHighlights();

                    yield return new WaitUntil(() => OnCreateHighlights == null);
                }
                numBuildingsPositioned += piece;

                /*
                if (OnCreateHighlights != null && createHighlights)
                {
                    //Debug.Log("Creating Highlights");
                    OnCreateHighlights();

                    yield return new WaitUntil(() => OnCreateHighlights == null);
                    //Debug.Log("Done Creating Highlights");
                }
                numBuildingsPositioned += piece;
                */

                if (generationFlags.HasFlag(GenerationFlags.Colliders) && OnCreateColliders != null)
                {
                    OnCreateColliders();

                    yield return new WaitUntil(() => OnCreateColliders == null);
                }
                numBuildingsPositioned += piece;

                /*
                if (OnCreateColliders != null && createColliders)
                {
                    //Debug.Log("Creating Colliders");
                    OnCreateColliders();

                    yield return new WaitUntil(() => OnCreateColliders == null);
                    //Debug.Log("Done Creating Colliders");
                }
                numBuildingsPositioned += piece;
                */

                if (OnSetLocation != null)
                {
                    //Debug.Log("Setting Location");
                    OnSetLocation();

                    yield return new WaitUntil(() => OnSetLocation == null);
                    //Debug.Log("Done Setting Building Locations");
                }
                numBuildingsPositioned += piece;

                numBuildingsPositioned += piece;

                if (OnSaveBuildings != null)
                {
                    Debug.Log("Saving Buildings");
                    OnSaveBuildings();

                    yield return new WaitUntil(() => OnSaveBuildings == null);
                    Debug.Log("Done Saving Buildings");
                }

                //Debug.Log("Done Loading Buildings");

                if (OnSendLogMessage != null)
                {
                    OnSendLogMessage("Almost done!");
                }

                //load future building dataset
                if (futureBuildingsDataset != null)
                {
                    var futureBuildings = futureBuildingsDataset.GetBuildingsToLoad();
                    //var futureBuildings = futureBuildingsDataset.GetBuildingsWithoutFeatures();

                    if (futureBuildings != null)
                    {
                        if (OnSendLogMessage != null)
                        {
                            OnSendLogMessage("Optimizing...");
                        }

                        var modValue2 = 500;
                        foreach (var prefab in futureBuildings)
                        {
                            if (prefab != null)
                            {
                                var control = prefab.GetComponent<CreatedBuildingControl>();
                                if (control != null)
                                {

                                    CreateBuildingsFromPrefab(prefab, true);

                                    numBuildingsLoaded += 1;
                                    if (numBuildingsLoaded % modValue2 == 0)
                                    {
                                        //Debug.LogAssertion("Still Loading Buildings: " + numBuildingsLoaded);
                                        //yield return new WaitForSecondsRealtime(0.001f);

                                        yield return new WaitForEndOfFrame();
                                        yield return new WaitForEndOfFrame();
                                        yield return new WaitForEndOfFrame();
                                        //yield return new WaitForEndOfFrame();
                                        //yield return new WaitForEndOfFrame();

                                        numBuildingsPositioned += (numBuildingsToLoad - numBuildingsLoaded) / 2;
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogError("Missing Prefab");
                            }
                        }


                        if (generationFlags.HasFlag(GenerationFlags.HighPolyModels) && OnCreateHighPolyModels != null)
                        {
                            OnCreateHighPolyModels();

                            yield return new WaitUntil(() => OnCreateHighPolyModels == null);
                        }

                        /*
                        if (OnCreateHighPolyModels != null && createHighPolyModels)
                        {
                            //Debug.Log("Creating High Poly Models");
                            OnCreateHighPolyModels();

                            yield return new WaitUntil(() => OnCreateHighPolyModels == null);
                            //Debug.Log("Done Creating High Poly Models");
                        }

                        numBuildingsPositioned += piece;
                        */

                        if (generationFlags.HasFlag(GenerationFlags.Highlights) && OnCreateHighlights != null)
                        {
                            OnCreateHighlights();

                            yield return new WaitUntil(() => OnCreateHighlights == null);
                        }

                        /*
                        if (OnCreateHighlights != null && createHighlights)
                        {
                            //Debug.Log("Creating Highlights");
                            OnCreateHighlights();

                            yield return new WaitUntil(() => OnCreateHighlights == null);
                            //Debug.Log("Done Creating Highlights");
                        }
                        numBuildingsPositioned += piece;
                        */

                        if (generationFlags.HasFlag(GenerationFlags.Colliders) && OnCreateColliders != null)
                        {
                            OnCreateColliders();

                            yield return new WaitUntil(() => OnCreateColliders == null);
                        }

                        /*
                        if (OnCreateColliders != null && createColliders)
                        {
                            //Debug.Log("Creating Colliders");
                            OnCreateColliders();

                            yield return new WaitUntil(() => OnCreateColliders == null);
                            //Debug.Log("Done Creating Colliders");
                        }
                        numBuildingsPositioned += piece;
                        */

                        if (OnSetLocation != null)
                        {
                            //Debug.Log("Setting Location");
                            OnSetLocation();

                            yield return new WaitUntil(() => OnSetLocation == null);
                            //Debug.Log("Done Setting Building Locations");
                        }

                        if (OnSaveBuildings != null)
                        {
                            Debug.Log("Saving Buildings");
                            OnSaveBuildings();

                            yield return new WaitUntil(() => OnSaveBuildings == null);
                            Debug.Log("Done Saving Buildings");
                        }
                    }
                }

                Debug.Log("All Buildings Created");

                if (OnFinishBuildingSetup != null)
                {
                    Debug.Log("Finishing Setup");
                    OnFinishBuildingSetup();

                    yield return new WaitUntil(() => OnFinishBuildingSetup == null);
                    //Debug.Log("Done Finishing Setup");
                }

                numBuildingsPositioned = numBuildingsToLoad;

                if (OnSendLogMessage != null)
                {
                    OnSendLogMessage("Making it look pretty!");
                }
            }


            if (OnFullMapCreated != null)
            {
                Debug.Log("On Full Map Created");
                OnFullMapCreated();

                //yield return new WaitUntil(() => OnFullMapCreated == null);
                //Debug.Log("Post Processing Enabled");
            }


            if (OnSaveBuildings != null)
            {
                Debug.Log("Saving Buildings");
                OnSaveBuildings();

                yield return new WaitUntil(() => OnSaveBuildings == null);
                Debug.Log("Done Saving Buildings");
            }
            else
            {
                Debug.Log("No Buildings To Save");
            }

            //SceneManager.UnloadSceneAsync(0);

            #region JSON File
#if UNITY_EDITOR
            if (createJSONFileForDatabase)
            {
                ReturnForDatabase();
            }
            else if (createJSONFileForStorage)
            {
                ReturnForStorage();
            }
#endif
            #endregion
        }
        else
        {
            Debug.LogError("No BuildingDataset Found. Cannot load buildings");

            if (OnFullMapCreated != null)
            {
                Debug.Log("On Full Map Created");
                OnFullMapCreated();

                //yield return new WaitUntil(() => OnFullMapCreated == null);
                //Debug.Log("Post Processing Enabled");
            }
        }

        /*
        if (loadHandleGameObjects.IsValid())
        {
            Addressables.Release(loadHandleGameObjects);
        }
        */
    }

    private void CreateBuildingsFromPrefab(GameObject prefab, bool future = false)
    {
        GameObject createdBuilding = null;
        bool save = false;

        if (saveFlags.HasFlag(SaveFlags.DoNotSave))
        {
            save = false;
        }
        else
        {
            if (saveFlags.HasFlag(SaveFlags.SaveBuildings01))
            {
                if (saveFlags.HasFlag(SaveFlags.SaveBuildings03))
                {
                    save = true;
                }
            }
        }

#if UNITY_EDITOR

        if (save)
        {
            createdBuilding = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            createdBuilding.transform.SetParent(prefabManager.ArcGISMapComponent.transform, false);
        }
        else
        {
            createdBuilding = Instantiate(prefab, prefabManager.ArcGISMapComponent.transform, false);
        }
#else
        createdBuilding = Instantiate(prefab, prefabManager.ArcGISMapComponent.transform, false);

#endif

        createdBuilding.hideFlags = this.hideFlags;

        var buildingControl = createdBuilding.GetComponent<CreatedBuildingControl>();
        if (buildingControl != null)
        {
            //var buildingSO = buildingControl.buildingSO;
            //var location = prefabManager.ArcGISMapComponent.View.GeographicToWorld(new ArcGISPoint(buildingSO.longitude, buildingSO.latitude, buildingSO.feature.properties.Elevation));

            buildingControl.ConfirmReferencesOnLoad(prefabManager,
                buildingFlags.HasFlag(BuildingFlags.MakeStatic),
                false,
                buildingFlags.HasFlag(BuildingFlags.AllColliders),
                save);// fullHighlightDecal,floorHighlightDecal, fullHighlightDecalMat, floorHighlightDecalMat, save);

            if (!buildingsByUID.TryGetValue(buildingControl.buildingDataClass.UUID, out GameObject testSO))
            {
                buildingsByUID.Add(buildingControl.buildingDataClass.UUID, createdBuilding);
                createdBuildingControls.Add(buildingControl);
            }
            else
            {
                Debug.LogError("Cannot add duplicate building: " + " Name: " + buildingControl.gameObject.name + " ID: " + buildingControl.buildingDataClass.UUID);
                duplicateBuildingsToDestroy.Add(buildingControl.gameObject);
            }

            if (future)
            {
                if (!futureBuildingsByUID.TryGetValue(buildingControl.buildingDataClass.UUID, out GameObject futureBuilding))
                {
                    futureBuildingsByUID.Add(buildingControl.buildingDataClass.UUID, createdBuilding);
                }
                else
                {
                    Debug.LogError("Cannot add future building: " + " Name: " + buildingControl.gameObject.name + " ID: " + buildingControl.buildingDataClass.ID);
                }
            }

            /*
            if (!buildingSOByID.ContainsKey(buildingControl.buildingSO.ID))
            {
                buildingSOByID.Add(buildingControl.buildingSO.ID, buildingControl.buildingSO);
            }
            else
            {
                Debug.LogError(buildingControl.buildingSO.Name + " " + buildingControl.buildingSO.ID);
            }
            */
            //createdBuilding.SetActive(false);
            buildingControl = null;
        }
        createdBuilding = null;
    }

    private bool WipeDuplicateBuildings()
    {
        try
        {
            for (int i = 0; i < duplicateBuildingsToDestroy.Count; i++)
            {
                Destroy(duplicateBuildingsToDestroy[i]);


            }

            duplicateBuildingsToDestroy.Clear();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Failed to wipe duplicate Buildings");
            return false;
        }
    }

    private void ReturnForDatabase()
    {
        Debug.Log("Creating JSONBuildingData file");
        List<BuildingJSONData> buildingJSONs = new List<BuildingJSONData>();

        List<GameObject> buildingsByHeight = new List<GameObject>();

        float currentMinHeight = 0;

        GameObject minObject = null;

        foreach (var building in buildingsByUID.Values)
        {
            if (building != null)
            {
                var control = building.GetComponent<CreatedBuildingControl>();

                if (control != null)
                {
                    var height = control.height;
                    if (buildingsByHeight.Count > 0)
                    {
                        if (height >= currentMinHeight)
                        {
                            buildingsByHeight.Add(building);

                            if (buildingsByHeight.Count > 40)
                            {
                                var smallest = 1000000.0f;
                                foreach (var obj in buildingsByHeight)
                                {
                                    var curHeight = obj.GetComponent<CreatedBuildingControl>().height;

                                    if (currentMinHeight < smallest)
                                    {
                                        minObject = obj;
                                        smallest = currentMinHeight;
                                    }
                                }

                                buildingsByHeight.Remove(minObject);
                                minObject = null;
                                currentMinHeight = smallest;
                            }
                        }
                    }
                    else
                    {
                        buildingsByHeight.Add(building);
                    }
                }
            }
        }


        foreach (var building in buildingsByHeight)
        {
            if (building != null)
            {
                var control = building.GetComponent<CreatedBuildingControl>();

                if (control != null)
                {
                    var data = control.GetBuildingData();
                    buildingJSONs.Add(data);

                }
            }
        }

        try
        {
            File.WriteAllText("./Assets/ScriptableObjects/BuildingDataJSONsForDatabase/" + 
                ApiConstants.market + "SmallJSON.json", JsonConvert.SerializeObject(buildingJSONs));

            Debug.Log("Created JSONBuildingData file");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to write JSONBuildingData to file");
            Debug.LogException(e);
        }
    }

    private void ReturnForStorage()
    {
#if UNITY_EDITOR
        Dictionary<string, JSONDataClass> buildingDataClasses = new Dictionary<string, JSONDataClass>();
        foreach (var dataset in buildingDatasetsToLoad)
        {
            if (dataset != null)
            {
                var allBuildings = dataset.GetBuildingsToLoad();
                var nonFeatures = dataset.GetBuildingsWithoutFeatures();

                foreach (var nonFeature in nonFeatures)
                {
                    allBuildings.Add(nonFeature);
                }

                foreach (var building in allBuildings)
                {
                    if (building != null)
                    {
                        var control = building.GetComponent<CreatedBuildingControl>();

                        if (control != null)
                        {
                            var dataClass = control.ReturnBuildingDataClass();

                            JSONDataClass jsonClass = new JSONDataClass()
                            {
                                ID = dataClass.ID.ToString(),
                                UUID = dataClass.UUID,
                                Name = dataClass.Name,
                                DatabaseName = dataClass.DatabaseName,
                                HasFeature = dataClass.HasFeature,
                                FutureBuilding = dataClass.FutureBuilding.ToString(),
                                Feature = dataClass.Feature,
                                Latitude = dataClass.Latitude,
                                Longitude = dataClass.Longitude,
                                Elevation = dataClass.Elevation,
                                //BuildingHighlight = JsonUtility.ToJson(dataClass.BuildingHighlight),
                                BuildingHighlightLayer = dataClass.BuildingHighlightLayer
                            };

                            if (!buildingDataClasses.ContainsKey(dataClass.UUID))
                            {
                                buildingDataClasses.Add(dataClass.UUID, jsonClass);
                            }
                        }
                    }
                }
            }

        }

        try
        {
            string path = "./Assets/ScriptableObjects/BuildingDataJSONsForStorage/" + ApiConstants.market + "Stored.json";
            string json = JsonConvert.SerializeObject(buildingDataClasses);

            File.WriteAllText(path, json);

            Debug.Log("Created JSONBuildingData file");

            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to write JSONBuildingData to file");
            Debug.LogException(e);
        }
#endif
    }

    private void OnDestroy()
    {
        if (loadHandleDatasets.IsValid())
        {
            Addressables.Release(loadHandleDatasets);
        }

        if (loadHandleGameObjects.IsValid())
        {
            Addressables.Release(loadHandleGameObjects);
        }

        ApiConstants.market = null;
    }
}

[Serializable]
public class JSONDataClass
{
    public string ID;
    public string UUID = string.Empty;
    public string Name = string.Empty;
    public string DatabaseName = string.Empty;
    public bool HasFeature;
    public string FutureBuilding;
    public Features Feature = null;
    public double Latitude;
    public double Longitude;
    public double Elevation;
    //public BuildingHighlight BuildingHighlight = null;
    public int BuildingHighlightLayer;
}