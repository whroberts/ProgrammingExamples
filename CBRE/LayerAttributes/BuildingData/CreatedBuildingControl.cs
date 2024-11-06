using BlueSky.BuildingManipulation;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityMeshSimplifier;

namespace BlueSky.Data
{
    public class CreatedBuildingControl : MonoBehaviour
    {
        [SerializeField] public BuildingDataClass buildingDataClass;
        private DecalProjector fullHighlightDecalProjector = null;
        [Tooltip("Decal Projectors by floor number")]
        [NonSerialized] public Dictionary<int, DecalProjector> floorHighlightDecalProjectors = new Dictionary<int, DecalProjector>();
        public GameObject highPolyModel = null;
        private Bounds bounds = new Bounds();
        public Bounds Bounds => bounds;
        //public Material fullHighlightDecalMaterial = null;
        //public Material floorHighlightDecalMaterial = null;
        [NonSerialized] public bool hasFullHighlight = false;

        //[HideInInspector] public prefabHighlightManager buildingHighlightManager = null;
        [NonSerialized] public BuildingPrefabManager prefabManager = null;

        //public GameObject lowPolyModelOpaque = null;

        private List<GameObject> subMeshGameObjects = new List<GameObject>();
        private List<MeshRenderer> subMeshRenderers = new List<MeshRenderer>();
        private List<MeshCollider> subMeshColliders = new List<MeshCollider>();

        //public GameObject fullHighlightPrefab = null;
        //public GameObject floorHighlightPrefab = null;

        private int lightLayerDefault = 0;
        private int noDecalLayer = 8;
        private int buildingHighlightDecalLayer = 9;
        private int floorHighlightDecalLayer = 12;

        private bool referencesApplied = false;
        [NonSerialized] public string popupId;
        [NonSerialized] public string markerId;

        public float height = 0;
        private CullLayerEnum cullLayer;
        private ArcGISLocationComponent locationComponent = null;
        private string layer = string.Empty;
        private int floorHighlightsEnabledCount = 0;

        private bool setStatic = true;
        private bool isSaving = false;
        private bool staticBatching = true;
        private bool allColliders = false;

        private BuildingHighlight buildingHighlight = null;

        //private LandingScene.Markets currentMarket = LandingScene.Markets.None;

        private enum CullLayerEnum
        {
            ExtraSmall = 6,
            Small = 7,
            Medium = 8,
            Large = 9,
            ExtraLarge = 10
        }

        #region Building Setup and Creation
        public void ConfirmReferencesOnLoad(BuildingPrefabManager prefabManager, bool makeStatic, bool staticBatching, bool allColliders, bool isSaving) // GameObject fullHighlight, GameObject floorHighlight,Material fullMat, Material floorMat)
        {
            this.prefabManager = prefabManager;
            //fullHighlightPrefab = fullHighlight;
            //floorHighlightPrefab = floorHighlight;
            //fullHighlightDecalMaterial = fullMat;
            //floorHighlightDecalMaterial = floorMat;
            this.staticBatching = staticBatching;
            //currentMarket = market;
            this.allColliders = allColliders;

#if UNITY_EDITOR
            this.isSaving = isSaving;
#else
            isSaving = false;
            setStatic = makeStatic;
#endif

            prefabManager.PrefabLoading.OnCreateHighPolyModels += new PrefabLoading.BuildingsLoadedEventHandler(CallCreateModel);
            prefabManager.PrefabLoading.OnSetLocation += new PrefabLoading.BuildingsLoadedEventHandler(CallSetLocation);
            prefabManager.PrefabLoading.OnFinishBuildingSetup += new PrefabLoading.BuildingsLoadedEventHandler(CallFinishSetup);

            if (isSaving)
            {
                setStatic = false;
                prefabManager.PrefabLoading.OnSaveBuildings += new PrefabLoading.BuildingsLoadedEventHandler(CallSaveBuildings);
            }
            else
            {
                setStatic = makeStatic;
                //prefabManager.PrefabLoading.OnCreateHighlights += new PrefabLoading.BuildingsLoadedEventHandler(CallCreateHighlight);
                prefabManager.PrefabLoading.OnCreateColliders += new PrefabLoading.BuildingsLoadedEventHandler(CallCreateColliders);
            }

            buildingHighlight = buildingDataClass.GetBuildingHighlight();

            if (buildingHighlight == null)
            {
                buildingHighlight = new BuildingHighlight()
                {
                    fullHighlightEnabled = false,
                    floorHighlightsEnabled = false,
                    fullHighlightColor = Color.white,
                    emissionColor = Color.white,
                    emissionStrength = 10,
                    isEmissive = 1,
                    floorHighlights = new List<FloorHighlight>()
                };
            }

            referencesApplied = true;
        }

        public void CallCreateModel()
        {
            StartCoroutine(CreateModel());
        }

        private IEnumerator CreateModel()
        {
            yield return new WaitUntil(() => referencesApplied);
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.1f, 0.5f));

            /*
            if (highPolyModel == null && buildingSO.highPolyModel != null)
            {
                //highPolyModel = Instantiate(buildingSO.highPolyModel, transform, false);

#if UNITY_EDITOR
                if (isSaving)
                {
                    highPolyModel = PrefabUtility.InstantiatePrefab(buildingSO.highPolyModel) as GameObject;
                    highPolyModel.transform.parent = transform;
                    highPolyModel.transform.localPosition = Vector3.zero;
                    highPolyModel.transform.localEulerAngles = Vector3.zero;
                    PrefabUtility.UnpackPrefabInstance(highPolyModel, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                }
#endif
            }
            */

            if (highPolyModel == null)
            {
                var childTransforms = GetComponentsInChildren<Transform>();
                if (childTransforms.Length > 1)
                {
                    if (childTransforms[1] != null)
                    {
                        highPolyModel = childTransforms[1].gameObject;
                    }
                }

            }

            if (highPolyModel != null)
            {
                bounds = GetTotalBoundingBox();

                height = bounds.size.y;

                if (gameObject.CompareTag("AlwaysRender"))
                {
                    if (height > 0)
                    {
                        if (height < 200)
                        {
                            layer = "LargeBuildings";
                            cullLayer = CullLayerEnum.Large;
                        }
                        else
                        {
                            layer = "ExtraLargeBuildings";
                            cullLayer = CullLayerEnum.ExtraLarge;
                        }
                    }

                }
                else
                {
                    if (height > 0)
                    {
                        if (height <= 20)
                        {
                            cullLayer = CullLayerEnum.ExtraSmall;
                            layer = "ExtraSmallBuildings";

                            foreach (var renderer in subMeshRenderers)
                            {
                                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                            }
                        }
                        else if (height > 20 && height <= 50)
                        {
                            cullLayer = CullLayerEnum.Small;
                            layer = "SmallBuildings";

                            foreach (var renderer in subMeshRenderers)
                            {
                                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                            }
                        }
                        else if (height > 50 && height <= 100)
                        {
                            cullLayer = CullLayerEnum.Medium;
                            layer = "MediumBuildings";
                            foreach (var renderer in subMeshRenderers)
                            {
                                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                            }
                        }
                        else if (height > 100 && height <= 200)
                        {
                            cullLayer = CullLayerEnum.Large;
                            layer = "LargeBuildings";
                        }
                        else if (height > 200)
                        {
                            cullLayer = CullLayerEnum.ExtraLarge;
                            layer = "ExtraLargeBuildings";
                        }

                        gameObject.layer = LayerMask.NameToLayer(layer);
                        highPolyModel.layer = LayerMask.NameToLayer(layer);
                        foreach (Transform transform in highPolyModel.GetComponentInChildren<Transform>())
                        {
                            transform.gameObject.layer = LayerMask.NameToLayer(layer);
                        }
                    }
                }
            }

            yield return new WaitForEndOfFrame();
            prefabManager.PrefabLoading.OnCreateHighPolyModels -= new PrefabLoading.BuildingsLoadedEventHandler(CallCreateModel);
        }

        public void CallCreateHighlight()
        {
            StartCoroutine(CreateHighlight());
        }

        private IEnumerator CreateHighlight()
        {
            yield return new WaitUntil(() => referencesApplied);
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.1f, 0.5f));

            if (highPolyModel != null)
            {
                switch (cullLayer)
                {
                    case CullLayerEnum.ExtraSmall:
                        if (fullHighlightDecalProjector == null)
                        {
                            CreateFullHighlight();
                        }
                        break;
                    case CullLayerEnum.Small:
                        if (fullHighlightDecalProjector == null)
                        {
                            CreateFullHighlight();
                        }
                        break;
                    case CullLayerEnum.Medium:
                        if (fullHighlightDecalProjector == null)
                        {
                            CreateFullHighlight();
                        }
                        break;
                    case CullLayerEnum.Large:
                        if (fullHighlightDecalProjector == null)
                        {
                            CreateFullHighlight();
                        }
                        break;
                    case CullLayerEnum.ExtraLarge:
                        if (fullHighlightDecalProjector == null)
                        {
                            CreateFullHighlight();
                        }
                        break;
                }
            }

            yield return new WaitForEndOfFrame();
            prefabManager.PrefabLoading.OnCreateHighlights -= new PrefabLoading.BuildingsLoadedEventHandler(CallCreateHighlight);
        }

        public void CallSetLocation()
        {
            StartCoroutine(SetLocation());
        }

        private IEnumerator SetLocation()
        {
            yield return new WaitUntil(() => referencesApplied);
            //yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.5f, 10f));

            if (highPolyModel != null)
            {
                locationComponent = gameObject.AddComponent<ArcGISLocationComponent>();
                var wait01 = UnityEngine.Random.Range(0.1f, 0.5f);
                yield return new WaitForSecondsRealtime(wait01);
                locationComponent.Rotation = new ArcGISRotation(180, 90, 0);

                var wait02 = UnityEngine.Random.Range(wait01, 0.5f);
                yield return new WaitForSecondsRealtime(wait02);

                locationComponent.Position = new ArcGISPoint(
                    (double)buildingDataClass.Longitude, 
                    (double)buildingDataClass.Latitude, 
                    (double)buildingDataClass.Elevation,
                    ArcGISSpatialReference.WGS84());

                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(wait02, 0.5f));
                DestroyImmediate(locationComponent);
            }

            yield return new WaitForEndOfFrame();
            prefabManager.PrefabLoading.OnSetLocation -= new PrefabLoading.BuildingsLoadedEventHandler(CallSetLocation);
        }

        public void CallCreateColliders()
        {
            StartCoroutine(CreateColliders());
        }

        private IEnumerator CreateColliders()
        {
            yield return new WaitUntil(() => referencesApplied);
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.1f, 0.5f));

            if (highPolyModel != null)
            {
                switch (cullLayer)
                {
                    case CullLayerEnum.ExtraSmall:
                        CreateMeshColliders(highPolyModel);
                        break;
                    case CullLayerEnum.Small:
                        CreateMeshColliders(highPolyModel);
                        break;
                    case CullLayerEnum.Medium:
                        CreateMeshColliders(highPolyModel);
                        break;
                    case CullLayerEnum.Large:
                        CreateMeshColliders(highPolyModel);
                        break;
                    case CullLayerEnum.ExtraLarge:
                        CreateMeshColliders(highPolyModel);
                        break;
                }
            }

            yield return new WaitForEndOfFrame();
            prefabManager.PrefabLoading.OnCreateColliders -= new PrefabLoading.BuildingsLoadedEventHandler(CallCreateColliders);
            //CallFinishSetup();
        }

        public void CallFinishSetup()
        {
            StartCoroutine(FinishSetup());
        }

        private IEnumerator FinishSetup()
        {
            yield return new WaitUntil(() => referencesApplied);
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.1f, 2.5f));

            if (highPolyModel != null)
            {
                foreach (var transform in GetComponentsInChildren<Transform>())
                {
                    transform.gameObject.isStatic = setStatic;

                    switch (cullLayer)
                    {
                        case CullLayerEnum.ExtraSmall:
                            transform.gameObject.layer = LayerMask.NameToLayer("ExtraSmallBuildings");
                            break;
                        case CullLayerEnum.Small:
                            transform.gameObject.layer = LayerMask.NameToLayer("SmallBuildings");
                            break;
                        case CullLayerEnum.Medium:
                            transform.gameObject.layer = LayerMask.NameToLayer("MediumBuildings");
                            break;
                        case CullLayerEnum.Large:
                            transform.gameObject.layer = LayerMask.NameToLayer("LargeBuildings");
                            break;
                        case CullLayerEnum.ExtraLarge:
                            transform.gameObject.layer = LayerMask.NameToLayer("ExtraLargeBuildings");
                            break;
                    }

                    yield return new WaitForEndOfFrame();
                }

                if (setStatic && staticBatching)
                {
                    //StaticBatchingUtility.Combine(highPolyModel);
                    //StaticBatchingUtility.Combine(gameObject);
                }

                /*
                foreach (var renderer in subMeshRenderers)
                {
                    var mesh = renderer.GetComponent<MeshFilter>().mesh;
                    if (mesh != null)
                    {
                        if (mesh.isReadable)
                        {
                            mesh.UploadMeshData(true);
                        }
                    }
                }
                */
            }

            yield return new WaitForEndOfFrame();
            prefabManager.PrefabLoading.OnFinishBuildingSetup -= new PrefabLoading.BuildingsLoadedEventHandler(CallFinishSetup);

            //prefabManager.PrefabLoading.OnCreateColliders -= new PrefabLoading.BuildingsLoadedEventHandler(CallCreateColliders);
        }

        private void ConvertToStructToClass()
        {
            /*
            if (buildingDataClass == null)
            {
                buildingDataClass = gameObject.AddComponent<BuildingDataClass>();

                buildingDataClass.ID = buildingData.ID;
                buildingDataClass.UUID = buildingData.UUID;
                buildingDataClass.Name = buildingData.Name;
                buildingDataClass.DatabaseName = buildingData.DatabaseName;
                buildingDataClass.HasFeature = buildingData.HasFeature;
                buildingDataClass.FutureBuilding = buildingData.FutureBuilding;
                buildingDataClass.Feature = buildingData.Feature;
                buildingDataClass.Latitude = buildingData.Latitude;
                buildingDataClass.Longitude = buildingData.Longitude;
                buildingDataClass.Elevation = buildingData.Elevation;
                buildingHighlight = buildingData.BuildingHighlight;
                buildingDataClass.BuildingHighlightLayer = buildingData.BuildingHighlightLayer;
            }
            else
            {
                var classes = gameObject.GetComponents<BuildingDataClass>();

                foreach (var c in classes)
                {
                    DestroyImmediate(c);
                }
                buildingDataClass = null;

                ConvertToStructToClass();
            }
            */
            /*
            buildingData = new BuildingData();
            buildingData.ID = buildingSO.ID;
            buildingData.UUID = buildingSO.uid;
            buildingData.Name = buildingSO.Name;
            buildingData.DatabaseName = buildingSO.DatabaseName;
            buildingData.HasFeature = buildingSO.hasFeature;
            buildingData.FutureBuilding = buildingSO.futureBuilding;
            buildingData.Feature = buildingSO.feature;
            buildingData.Latitude = buildingSO.latitude;
            buildingData.Longitude = buildingSO.longitude;
            buildingData.Elevation = buildingSO.feature.properties.Elevation;
            buildingData.BuildingHighlight = buildingSO.buildingHighlight;
            buildingData.BuildingHighlightLayer = buildingSO.buildingHighlightLayer;
            */
        }
        

        private void CallSaveBuildings()
        {
            StartCoroutine(DelayToApplyPrefabChanges());
        }

        private IEnumerator DelayToApplyPrefabChanges()
        {
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.5f, 2.5f));
            ConvertToStructToClass();
#if UNITY_EDITOR
            PrefabUtility.ApplyAddedComponent(buildingDataClass, AssetDatabase.GetAssetPath(gameObject), InteractionMode.AutomatedAction);
            PrefabUtility.ApplyPrefabInstance(gameObject, InteractionMode.AutomatedAction);
#endif
            prefabManager.PrefabLoading.OnSaveBuildings -= new PrefabLoading.BuildingsLoadedEventHandler(CallSaveBuildings);
        }


#endregion

        public void PlaceBuilding(ArcGISMapComponent arcGISMapComponent)
        {
            /*
            Properties properties = null;
            if (buildingSO != null)
            {
                properties = buildingSO.feature.properties;

            }
            else
            {
                properties = buildingData.Feature.properties;
            }
            */

            //Properties properties = buildingDataClass.Feature.properties;

            gameObject.transform.parent = arcGISMapComponent.transform;
            var locationComponent = gameObject.AddComponent<ArcGISLocationComponent>();
            locationComponent.Position = new ArcGISPoint(buildingDataClass.Longitude, buildingDataClass.Latitude, buildingDataClass.Elevation, ArcGISSpatialReference.WGS84());
            locationComponent.Rotation = new ArcGISRotation(180, 90, 0);
            locationComponent.SyncPositionWithHPTransform();
            DestroyImmediate(locationComponent);

            var hp = GetComponent<HPTransform>();

            if (hp != null)
            {
                hp.LocalPosition = new Unity.Mathematics.double3(571455.17169418908, 4273191.6764483433, -4687555.3380244179);
            }
        }

        #region Region Call From Manager
        public void EnableHighlightFromManager(Color highlightColor)
        {
            prefabManager.PrefabHighlightManager.EnableFullHighlight(gameObject, highlightColor);
        }

        public void EnableHighlightFromManager(BuildingHighlight buildingHighlight)
        {
            if (buildingHighlight.fullHighlightEnabled)
            {
                prefabManager.PrefabHighlightManager.EnableFullHighlight(gameObject, buildingHighlight);
            }

            if (buildingHighlight.floorHighlightsEnabled)
            {
                if (buildingHighlight.floorHighlights != null)
                {
                    foreach (var floorHighlight in buildingHighlight.floorHighlights)
                    {
                        prefabManager.PrefabHighlightManager.CreateIndividualFloorHighlight(gameObject, floorHighlight);
                    }
                }
            }
        }

        //Enable fullhighlight by id
        public void EnableHighlightFromManager(int id, Color highlightColor)
        {
            //prefabManager.PrefabHighlightManager.EnableFullHighlight(id, highlightColor);

        }

        public void DisableHighlightFromManager()
        {
            prefabManager.PrefabHighlightManager.DisableFullHighlight();
            DisableAllIndividualFloorHighlights();
        }
#endregion

#region Region Mesh Control

        private int Contains(List<Material> searchList, string searchName)
        {
            for (int i = 0; i < searchList.Count; i++)
            {
                if (((Material)searchList[i]).name == searchName)
                {
                    return i;
                }
            }
            return -1;
        }

        public void SetActiveModel(bool highPoly)
        {
            if (highPoly)
            {
                if (hasFullHighlight || buildingHighlight.fullHighlightEnabled || buildingHighlight.floorHighlightsEnabled)
                {
                    SetRenderMask(buildingHighlightDecalLayer, -1, false);
                }
                else
                {
                    SetRenderMask(noDecalLayer, -1, false);
                }

                //highPolyModel.SetActive(true);
                //lowPolyModelTransparent.SetActive(false);
            }
            else
            {
                if (hasFullHighlight || buildingHighlight.fullHighlightEnabled || buildingHighlight.floorHighlightsEnabled)
                {
                    SetRenderMask(buildingHighlightDecalLayer, -1, false);
                }
                else
                {
                    SetRenderMask(noDecalLayer, -1, true);
                }

                //highPolyModel.SetActive(true);
                //lowPolyModelTransparent.SetActive(true);
            }


            /*
            if (lowPolyModelTransparent == null)
            {
                highPolyModel.SetActive(true);
                return;
            }
            else
            {
                if (highPoly)
                {
                    highPolyModel.SetActive(true);
                    lowPolyModelTransparent.SetActive(false);
                }
                else
                {
                    highPolyModel.SetActive(true);
                    lowPolyModelTransparent.SetActive(true);
                }
            }

            */
        }
        #endregion

#region Region Full Highlight

        public void HighlightBuilding(BuildingHighlight buildingHighlight)
        {
            if (fullHighlightDecalProjector != null)
            {
                EnableFullHighlight(buildingHighlight);
            }
            else
            {
                CreateFullHighlight(buildingHighlight);
            }
        }

        private void CreateFullHighlight(BuildingHighlight buildingHighlight = null)
        {
            var decalGO = Instantiate(prefabManager.PrefabLoading.FullHighlightDecal, transform, false);
            
            decalGO.transform.parent = transform;

            var decalProjector = decalGO.GetComponentInChildren<DecalProjector>();

            
            if (buildingDataClass.BuildingHighlightLayer == -1)
            {
                buildingHighlightDecalLayer = UnityEngine.Random.Range(9, 15);
                
            }
            else
            {
                buildingHighlightDecalLayer = buildingDataClass.BuildingHighlightLayer;
            }
            floorHighlightDecalLayer = buildingHighlightDecalLayer;

            CreateFullHighlightDecal(decalProjector, buildingHighlight);
        }

        private void CreateFullHighlightDecal(DecalProjector decalProjector, BuildingHighlight buildingHighlight = null)
        {
            Debug.Log("Creating Highlight");

            fullHighlightDecalProjector = decalProjector;

            //fullHighlightDecalProjector.pivot = new Vector3(bounds.center.x, bounds.center.z, bounds.size.y / -2);
            Vector3 position = subMeshGameObjects[0].transform.parent.localPosition;
            Vector3 rotation = subMeshGameObjects[0].transform.parent.localEulerAngles;

            if (position.x == 0 && position.y == 0 && position.z == 0)
            {
                position = subMeshGameObjects[0].transform.parent.transform.parent.localPosition;
            }

            if (rotation.x == 0 && rotation.y == 0 && rotation.z == 0)
            {
                rotation = subMeshGameObjects[0].transform.parent.transform.parent.localEulerAngles;
            }

            fullHighlightDecalProjector.transform.localPosition = new Vector3(position.x, position.y, position.z);

            if (rotation != Vector3.zero)
            {
                fullHighlightDecalProjector.transform.localEulerAngles = new Vector3(90, 0, -rotation.y);
            }
            else
            {
                // Models from WashingtonDC are not defaulted rotated 17 degrees
                // I do not know why, but this is a temporary fix
                if (ApiConstants.market.Equals( MarketConstants.WashingtonDC,StringComparison.OrdinalIgnoreCase) ||
                   ApiConstants.market.Equals(MarketConstants.Atlanta, StringComparison.OrdinalIgnoreCase))
                {
                    fullHighlightDecalProjector.transform.localEulerAngles = new Vector3(90, 0, 0);
                }
                else
                {
                    fullHighlightDecalProjector.transform.localEulerAngles = new Vector3(90, 0, 17);
                }
            }

            if (buildingHighlightDecalLayer == 9)
            {
                fullHighlightDecalProjector.decalLayerMask = DecalLayerEnum.DecalLayer1;
            }
            else if (buildingHighlightDecalLayer == 10)
            {
                fullHighlightDecalProjector.decalLayerMask = DecalLayerEnum.DecalLayer2;
            }
            else if (buildingHighlightDecalLayer == 11)
            {
                fullHighlightDecalProjector.decalLayerMask = DecalLayerEnum.DecalLayer3;
            }
            else if (buildingHighlightDecalLayer == 12)
            {
                fullHighlightDecalProjector.decalLayerMask = DecalLayerEnum.DecalLayer4;
            }
            else if (buildingHighlightDecalLayer == 13)
            {
                fullHighlightDecalProjector.decalLayerMask = DecalLayerEnum.DecalLayer5;
            }
            else if (buildingHighlightDecalLayer == 14)
            {
                fullHighlightDecalProjector.decalLayerMask = DecalLayerEnum.DecalLayer6;
            }
            else if (buildingHighlightDecalLayer == 15)
            {
                fullHighlightDecalProjector.decalLayerMask = DecalLayerEnum.DecalLayer7;
            }

            if (rotation.x != 0)
            {
                fullHighlightDecalProjector.size = new Vector3(bounds.size.x + 25f, bounds.size.y + 25f, bounds.size.z + 25f);
                fullHighlightDecalProjector.pivot = new Vector3(0, 0, (bounds.size.z / -2) - 12.5f);
            }
            else
            {
                fullHighlightDecalProjector.size = new Vector3(bounds.size.x + 25f, bounds.size.z + 25f, bounds.size.y + 25f);
                fullHighlightDecalProjector.pivot = new Vector3(0, 0, (bounds.size.y / -2) - 12.5f);
            }

            fullHighlightDecalProjector.material = new Material(prefabManager.PrefabLoading.FullHighlightDecalMat);
            fullHighlightDecalProjector.material.SetColor("_HighlightColor", Color.white);

            fullHighlightDecalProjector.gameObject.SetActive(false);

            if (buildingHighlight == null)
            {
                buildingHighlight = new BuildingHighlight()
                {
                    fullHighlightEnabled = false,
                    floorHighlightsEnabled = false,
                    floorHighlights = new List<FloorHighlight>()
                };
            }

            if (buildingHighlight != null)
            {
                EnableFullHighlight(buildingHighlight);
            }
        }

        /*public void EnableFullHighlight(Color highlightColor)
        {
            SetRenderMask(buildingHighlightDecalLayer, floorHighlightDecalLayer);

            fullHighlightDecalProjector.material.SetColor("_HighlightColor", highlightColor);
            fullHighlightDecalProjector.material.SetFloat("_Alpha", highlightColor.a);

            if (highlightColor.a <= 0)
            {
                fullHighlightDecalProjector.material.SetFloat("_isEmissive", 0);
                fullHighlightDecalProjector.material.SetFloat("_EmissionStrength", 0);
            }
            else
            {
                fullHighlightDecalProjector.material.SetFloat("_isEmissive", 1);
                fullHighlightDecalProjector.material.SetFloat("_EmissionStrength", 10);
            }

            fullHighlightDecalProjector.material.SetColor("_EmissionColor", highlightColor);


            hasFullHighlight = true;
            fullHighlightDecalProjector.gameObject.SetActive(true);
        }
        */

        public void EnableFullHighlight(BuildingHighlight buildingHighlight)
        {
            Debug.Log("Enabling Highlight");
            /*
            var buildingData = GetBuildingData();
            if (!prefabManager.PrefabHighlightManager.buildingJSONDatas.Contains(buildingData))
            {
                prefabManager.PrefabHighlightManager.buildingJSONDatas.Add(GetBuildingData());
            }
            */
            SetRenderMask(buildingHighlightDecalLayer, floorHighlightDecalLayer);

            //buildingSO.buildingHighlight = buildingHighlight;
            fullHighlightDecalProjector.material.SetColor("_HighlightColor", buildingHighlight.fullHighlightColor);
            fullHighlightDecalProjector.material.SetFloat("_Alpha", buildingHighlight.fullHighlightColor.a);

            if (buildingHighlight.fullHighlightColor.a <= 0)
            {
                fullHighlightDecalProjector.material.SetFloat("_isEmissive", 0);
                fullHighlightDecalProjector.material.SetFloat("_EmissionStrength", 0);
            }
            else
            {
                if (floorHighlightsEnabledCount > 0 || buildingHighlight.floorHighlightsEnabled == true)
                {
                    fullHighlightDecalProjector.material.SetFloat("_isEmissive", 0);
                    fullHighlightDecalProjector.material.SetFloat("_EmissionStrength", 0);
                }
                else
                {
                    fullHighlightDecalProjector.material.SetFloat("_isEmissive", 1);
                    fullHighlightDecalProjector.material.SetFloat("_EmissionStrength", 10);
                }
            }

            fullHighlightDecalProjector.material.SetColor("_EmissionColor", buildingHighlight.fullHighlightColor);


            hasFullHighlight = true;
            fullHighlightDecalProjector.gameObject.SetActive(true);

            gameObject.layer = LayerMask.NameToLayer("ExtraLargeBuildings");
            foreach (var transform in gameObject.GetComponentsInChildren<Transform>())
            {
                transform.gameObject.layer = LayerMask.NameToLayer("ExtraLargeBuildings");
            }
        }

        public void DisableFullHighlight()
        {
            SetRenderMask(noDecalLayer , -1, false);

            hasFullHighlight = false;
            if (fullHighlightDecalProjector != null)
            {
                fullHighlightDecalProjector.gameObject.SetActive(false);
            }

            buildingHighlight.fullHighlightEnabled = false;

            gameObject.layer = LayerMask.NameToLayer(layer);
            foreach (var transform in gameObject.GetComponentsInChildren<Transform>())
            {
                transform.gameObject.layer = LayerMask.NameToLayer(layer);
            }
        }
#endregion

#region Region Individual Floor Highlight
        public void CreateIndividualFloorHighlight(int floor, float floorStart, float floorEnd, Color highlightColor)
        {
            if (!floorHighlightDecalProjectors.ContainsKey(floor))
            {
                var decalGO = Instantiate(prefabManager.PrefabLoading.FloorHighlightDecal, transform, false);
                decalGO.name = "Floor: " + floor + " Highlight";
                var decalProjector = decalGO.GetComponentInChildren<DecalProjector>();

                if (buildingHighlightDecalLayer == 9)
                {
                    decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer1;
                }
                else if (buildingHighlightDecalLayer == 10)
                {
                    decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer2;
                }
                else if (buildingHighlightDecalLayer == 11)
                {
                    decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer3;
                }
                else if (buildingHighlightDecalLayer == 12)
                {
                    decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer4;
                }
                else if (buildingHighlightDecalLayer == 13)
                {
                    decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer5;
                }
                else if (buildingHighlightDecalLayer == 14)
                {
                    decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer6;
                }
                else if (buildingHighlightDecalLayer == 15)
                {
                    decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer7;
                }

                floorHighlightDecalProjectors.Add(floor,decalProjector);

                EditIndividualFloorHighlightDecal(floor, floorStart, floorEnd, highlightColor);
            }
            else
            {
                EditIndividualFloorHighlightDecal(floor, floorStart, floorEnd, highlightColor);
            }
        }

        public void CreateIndividualFloorHighlight(FloorHighlight floorHighlight)
        {
            if (!floorHighlightDecalProjectors.ContainsKey((int)floorHighlight.floor))
            {
                var decalGO = Instantiate(prefabManager.PrefabLoading.FloorHighlightDecal, transform, false);
                decalGO.name = "Floor: " + (int)floorHighlight.floor + " Highlight";
                var decalProjector = decalGO.GetComponentInChildren<DecalProjector>();

                if (buildingHighlightDecalLayer == 9)
                {
                    decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer1;
                }
                else if (buildingHighlightDecalLayer == 10)
                {
                    decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer2;
                }
                else if (buildingHighlightDecalLayer == 11)
                {
                    decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer3;
                }
                else if (buildingHighlightDecalLayer == 12)
                {
                    decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer4;
                }
                else if (buildingHighlightDecalLayer == 13)
                {
                    decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer5;
                }
                else if (buildingHighlightDecalLayer == 14)
                {
                    decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer6;
                }
                else if (buildingHighlightDecalLayer == 15)
                {
                    decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer7;
                }

                floorHighlightDecalProjectors.Add((int)floorHighlight.floor, decalProjector);

                EditIndividualFloorHighlightDecal(floorHighlight);
            }
            else
            {
                EditIndividualFloorHighlightDecal(floorHighlight);
            }
          
        }

        private void EditIndividualFloorHighlightDecal(int floor, float start, float end, Color color)
        {
            var decal = floorHighlightDecalProjectors[floor];

            var floorHeight = end - start;
            var floorPosition = (start + end) / 2;

            Vector3 position = subMeshGameObjects[0].transform.parent.localPosition;
            Vector3 rotation = subMeshGameObjects[0].transform.parent.localEulerAngles;

            if (position.x == 0 && position.y == 0 && position.z == 0)
            {
                position = subMeshGameObjects[0].transform.parent.transform.parent.localPosition;
            }

            if (rotation.x == 0 && rotation.y == 0 && rotation.z == 0)
            {
                rotation = subMeshGameObjects[0].transform.parent.transform.parent.localEulerAngles;
            }

            decal.transform.localPosition = new Vector3(position.x, floorPosition, position.z);

            if (rotation != Vector3.zero)
            {
                decal.transform.localEulerAngles = new Vector3(90, 0, -rotation.y);
            }
            else
            {
                if (ApiConstants.market.Equals(MarketConstants.WashingtonDC, StringComparison.OrdinalIgnoreCase))
                {
                    decal.transform.localEulerAngles = new Vector3(90, 0, 0);
                }
                else
                {
                    decal.transform.localEulerAngles = new Vector3(90, 0, 17);
                }
            }

            if (rotation.x != 0)
            {
                decal.size = new Vector3(bounds.size.x + 25f, bounds.size.y + 25f, floorHeight);
            }
            else
            {
                decal.size = new Vector3(bounds.size.x + 25f, bounds.size.z + 25f, floorHeight);
            }

            //decal.gameObject.transform.localPosition = new Vector3(0, floorPosition, 0);
            //decal.gameObject.transform.localEulerAngles = new Vector3(90, 0, 17f);
            //decal.size = new Vector3(bounds.size.x, bounds.size.z, floorHeight);

            decal.material = new Material(prefabManager.PrefabLoading.FloorHighlightDecalMat);
            decal.material.SetColor("_HighlightColor", color);

            if (color.a <= 0)
            {
                decal.material.SetFloat("_isEmissive", 0);
                decal.material.SetFloat("_EmissionStrength", 0);
            }
            else
            {
                fullHighlightDecalProjector.material.SetFloat("_isEmissive", 0);
                fullHighlightDecalProjector.material.SetFloat("_EmissionStrength", 0);

                decal.material.SetFloat("_isEmissive", 1);
                decal.material.SetFloat("_EmissionStrength", 10);
            }

            decal.material.SetColor("_EmissionColor", color);

            floorHighlightDecalProjectors[floor] = decal;

            gameObject.layer = LayerMask.NameToLayer("ExtraLargeBuildings");
            foreach (var transform in gameObject.GetComponentsInChildren<Transform>())
            {
                transform.gameObject.layer = LayerMask.NameToLayer("ExtraLargeBuildings");
            }

            EnableIndividualFloorHighlight(decal, color);
        }

        private void EditIndividualFloorHighlightDecal(FloorHighlight floorHighlight)
        {
            var decal = floorHighlightDecalProjectors[(int)floorHighlight.floor];

            Vector3 position = subMeshGameObjects[0].transform.parent.localPosition;
            Vector3 rotation = subMeshGameObjects[0].transform.parent.localEulerAngles;

            if (position.x == 0 && position.y == 0 && position.z == 0)
            {
                position = subMeshGameObjects[0].transform.parent.transform.parent.localPosition;
            }

            if (rotation.x == 0 && rotation.y == 0 && rotation.z == 0)
            {
                rotation = subMeshGameObjects[0].transform.parent.transform.parent.localEulerAngles;
            }

            decal.transform.localPosition = new Vector3(position.x, floorHighlight.floorPosition, position.z);

            if (rotation != Vector3.zero)
            {
                decal.transform.localEulerAngles = new Vector3(90, 0, -rotation.y);
            }
            else
            {
                decal.transform.localEulerAngles = new Vector3(90, 0, 17);
            }

            if (rotation.x != 0)
            {
                decal.size = new Vector3(bounds.size.x + 25f, bounds.size.y + 25f, floorHighlight.floorHeight);
            }
            else
            {
                decal.size = new Vector3(bounds.size.x + 25f, bounds.size.z + 25f, floorHighlight.floorHeight);
            }

            //decal.gameObject.transform.localPosition = new Vector3(position.x, floorHighlight.floorPosition, position.z);
            //decal.gameObject.transform.localEulerAngles = new Vector3(90, 0, 17f);
            //decal.size = new Vector3(bounds.size.x, bounds.size.z, floorHighlight.floorHeight);

            decal.material = new Material(prefabManager.PrefabLoading.FloorHighlightDecalMat);
            decal.material.SetColor("_HighlightColor", floorHighlight.highlightColor);

            if (floorHighlight.highlightColor.a <= 0)
            {
                decal.material.SetFloat("_isEmissive", 0);
                decal.material.SetFloat("_EmissionStrength", 0);
            }
            else
            {
                fullHighlightDecalProjector.material.SetFloat("_isEmissive", 0);
                fullHighlightDecalProjector.material.SetFloat("_EmissionStrength", 0);

                decal.material.SetFloat("_isEmissive", 1);
                decal.material.SetFloat("_EmissionStrength", 10);
            }

            decal.material.SetColor("_EmissionColor", floorHighlight.highlightColor);

            floorHighlightDecalProjectors[(int)floorHighlight.floor] = decal;

            gameObject.layer = LayerMask.NameToLayer("ExtraLargeBuildings");
            foreach (var transform in gameObject.GetComponentsInChildren<Transform>())
            {
                transform.gameObject.layer = LayerMask.NameToLayer("ExtraLargeBuildings");
            }

            EnableIndividualFloorHighlight(decal, floorHighlight.highlightColor);
        }

        public void EnableIndividualFloorHighlight(DecalProjector decalProjector, Color color)
        {
            decalProjector.material.SetColor("_HighlightColor", color);

            if (color.a <= 0)
            {
                decalProjector.material.SetFloat("_isEmissive", 0);
                decalProjector.material.SetFloat("_EmissionStrength", 0);
            }
            else
            {
                fullHighlightDecalProjector.material.SetFloat("_isEmissive", 0);
                fullHighlightDecalProjector.material.SetFloat("_EmissionStrength", 0);

                decalProjector.material.SetFloat("_isEmissive", 1);
                decalProjector.material.SetFloat("_EmissionStrength", 10);
            }

            decalProjector.material.SetColor("_EmissionColor", color);


            SetRenderMask(floorHighlightDecalLayer, buildingHighlightDecalLayer);

            decalProjector.gameObject.SetActive(true);
            floorHighlightsEnabledCount++;
            buildingHighlight.floorHighlightsEnabled = true;
        }

        /*public void EnableIndividualFloorHighlight(int id, Color color)
        {
            var decalProjector = floorHighlightDecalProjectors[id];

            if (color.a <= 0)
            {
                decalProjector.material.SetFloat("_isEmissive", 0);
                decalProjector.material.SetFloat("_EmissionStrength", 0);
            }
            else
            {
                fullHighlightDecalProjector.material.SetFloat("_isEmissive", 0);
                fullHighlightDecalProjector.material.SetFloat("_EmissionStrength", 0);

                decalProjector.material.SetFloat("_isEmissive", 1);
                decalProjector.material.SetFloat("_EmissionStrength", 10);
            }

            decalProjector.material.SetColor("_EmissionColor", color);

            decalProjector.material.SetColor("_HighlightColor", color);

            SetRenderMask(floorHighlightDecalLayer, buildingHighlightDecalLayer);

            decalProjector.gameObject.SetActive(true);
        }
        */

        public void DisableIndividualFloorHighlight(int floorNum)
        {
            SetRenderMask(noDecalLayer);

            floorHighlightDecalProjectors[floorNum].gameObject.SetActive(false);

            floorHighlightsEnabledCount--;

            if (floorHighlightsEnabledCount <= 0)
            {

                gameObject.layer = LayerMask.NameToLayer(layer);
                foreach (var transform in gameObject.GetComponentsInChildren<Transform>())
                {
                    transform.gameObject.layer = LayerMask.NameToLayer(layer);
                }
                floorHighlightsEnabledCount = 0;
                buildingHighlight.floorHighlightsEnabled = false;
            }
        }

        public void DisableAllIndividualFloorHighlights()
        {
            SetRenderMask(noDecalLayer);

            foreach (var decalProjector in floorHighlightDecalProjectors.Values)
            {
                decalProjector.gameObject.SetActive(false);
            }

            gameObject.layer = LayerMask.NameToLayer(layer);
            foreach (var transform in gameObject.GetComponentsInChildren<Transform>())
            {
                transform.gameObject.layer = LayerMask.NameToLayer(layer);
            }
            floorHighlightsEnabledCount = 0;

            buildingHighlight.floorHighlightsEnabled = false;
        }

        /*public void DestroyAllFloorHighlights()
        {
            foreach (var decalProjector in floorHighlightDecalProjectors)
            {
                var floor = decalProjector.Key;
                var decal = decalProjector.Value;

                var parent = decal.GetComponentInParent<Transform>().gameObject;
                DestroyImmediate(parent);
            }
        }
        */
        #endregion

#region Region Private Constructors
        private void CreateMeshColliders(GameObject model)
        {
            bool hasCollider = false;
            if (!allColliders)
            {
                foreach (var meshRenderer in subMeshRenderers)
                {
                    /*
                    var collider = meshRenderer.gameObject.GetComponent<MeshCollider>();

                    if (collider == null)
                    {
                        collider = meshRenderer.gameObject.AddComponent<MeshCollider>();
                    }
                    hasCollider = true;
                    subMeshColliders.Add(collider);
                    */
                    string name = meshRenderer.gameObject.name;

                    if (name.Equals("CityEngineMaterial"))
                    {
                        var col = meshRenderer.gameObject.GetComponent<MeshCollider>();

                        if (col == null)
                        {
                            col = meshRenderer.gameObject.AddComponent<MeshCollider>();
                        }
                        hasCollider = true;
                        meshRenderer.enabled = false;
                        subMeshColliders.Add(col);
                    }


                    meshRenderer.gameObject.isStatic = setStatic;

                }
            }


            if (!hasCollider)
            {
                foreach (var meshRenderer in subMeshRenderers)
                {
                    var collider = meshRenderer.gameObject.GetComponent<MeshCollider>();

                    if (collider == null)
                    {
                        collider = meshRenderer.gameObject.AddComponent<MeshCollider>();
                    }

                    subMeshColliders.Add(collider);
                }
            }
            
            //StaticBatchingUtility.Combine(model);

            /*
            Ray ray;

            if (Physics.CheckSphere(bounds.cen)
            */
        }

        private void CreateBoxCollider()
        {
            var boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.size = bounds.size;
            boxCollider.center = bounds.center;
        }

        private Bounds GetTotalBoundingBox()
        {
            var bounds = new Bounds();

            /*
            if (subMeshRenderers.Count == 1)
            {
                bounds.Encapsulate(subMeshRenderers[0].bounds);
            }
            else
            {
                foreach (var renderer in subMeshRenderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            */

            foreach (var meshRenderer in highPolyModel.GetComponentsInChildren<MeshRenderer>())
            {

                subMeshGameObjects.Add(meshRenderer.gameObject);
                subMeshRenderers.Add(meshRenderer);
                //subMeshMaterials.Add(meshRenderer.sharedMaterial);

                bounds.Encapsulate(meshRenderer.localBounds);
            }

            /*
            if (bounds.size.y > 1000)
            {
                //bounds = new Bounds(highPolyModel.transform.position, Vector3.zero);
                bounds = new Bounds();
                foreach (var meshRenderer in subMeshRenderers)
                {
                    bounds.Encapsulate(meshRenderer.bounds);
                }
            }
            */

            return bounds;
        }

        
        private void Simplify(float quality)
        {
            var meshFilters = highPolyModel.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter meshFilter in meshFilters)
            {
                SimplifyMeshFilter(meshFilter, quality);
            }
        }

        private void SimplifyMeshFilter(MeshFilter meshFilter, float quality)
        {
            Mesh sourceMesh = meshFilter.sharedMesh;
            if (sourceMesh == null) // verify that the mesh filter actually has a mesh
                return;

            // Create our mesh simplifier and setup our entire mesh in it
            var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
            meshSimplifier.Initialize(sourceMesh);

            SimplificationOptions options = new SimplificationOptions()
            {
                PreserveSurfaceCurvature = true,
                PreserveBorderEdges = true
            };

            // This is where the magic happens, lets simplify!
            meshSimplifier.SimplifyMesh(quality);

            // Create our final mesh and apply it back to our mesh filter
            meshFilter.sharedMesh = meshSimplifier.ToMesh();
        }

        private void SetupLoD(float lod1Min, float lod2Min, float lod3Min)
        {
            //SimplificationOptions simplificationOptions = SimplificationOptions.Default;
            SimplificationOptions simplificationOptions = new SimplificationOptions()
            {
                PreserveSurfaceCurvature = true,
                PreserveBorderEdges = true,
            };
            bool autoCollectRenderers = true;
            LODLevel[] levels = new LODLevel[]
            {
                new LODLevel(lod1Min, 1f)
                {
                    CombineMeshes = true,
                    CombineSubMeshes = true,
                    SkinQuality = SkinQuality.Auto,
                    ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                    ReceiveShadows = true,
                    SkinnedMotionVectors = true,
                    LightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes,
                    ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes,
                    Quality = 1.0f
                },
                new LODLevel(lod2Min, lod1Min)
                {
                    CombineMeshes = true,
                    CombineSubMeshes = true,
                    SkinQuality = SkinQuality.Auto,
                    ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                    ReceiveShadows = true,
                    SkinnedMotionVectors = true,
                    LightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes,
                    ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Simple,
                    Quality = 0.9f
                },
                new LODLevel(lod3Min, lod2Min)
                {
                    CombineMeshes = true,
                    CombineSubMeshes = true,
                    SkinQuality = SkinQuality.Bone2,
                    ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                    ReceiveShadows = false,
                    SkinnedMotionVectors = false,
                    LightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off,
                    ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off,
                    Quality = 0.75f
                }
            };
            LODGenerator.GenerateLODs(gameObject, levels, autoCollectRenderers, simplificationOptions);

            var LODGroup = GetComponent<LODGroup>();

            if (LODGroup != null)
            {
                LODGroup.fadeMode = LODFadeMode.CrossFade;
                LODGroup.animateCrossFading = true;
                LODGroup.lastLODBillboard = true;
            }
        }

        private void SetRenderMask(int decalToRenderLayer, int secondDecalToRenderLayer = -1, bool disableLightLayer = false)
        {
            foreach (var meshRenderer in subMeshRenderers)
            {
                uint renderMask;
                if (disableLightLayer)
                {
                    renderMask = ((uint)1 << decalToRenderLayer | ((uint)1 << secondDecalToRenderLayer));
                }
                else
                {
                    renderMask = ((uint)1 << lightLayerDefault) | ((uint)1 << decalToRenderLayer | ((uint)1 << secondDecalToRenderLayer));
                }

                meshRenderer.renderingLayerMask = renderMask;
            }
        }
        #endregion

#region Region Public Constructors

        public Vector3 TopOfBounds()
        {
            Vector3 absoluteTop = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            //var closestTopPoint = bounds.ClosestPoint(absoluteTop);

            return absoluteTop;
        }

        public void SelectFloor(Vector3 hit)
        {
            if (floorHighlightDecalProjectors.Count > 0)
            {
                int closestFloor = -1;
                float smallestDistance = 10000;

                foreach (var pair in floorHighlightDecalProjectors)
                {
                    int floorID = pair.Key;
                    var floor = pair.Value;

                    if (floor.enabled)
                    {
                        float distance = Mathf.Abs(hit.y - floor.transform.position.y);

                        if (distance < smallestDistance)
                        {
                            smallestDistance = distance;
                            closestFloor = floorID;
                        }
                    }
                }

                Debug.Log("Closest Floor: " + closestFloor + " Distance: " + smallestDistance);
            }
        }

        public BuildingJSONData GetBuildingData()
        {
            string address = string.Empty;
            var properties = buildingDataClass.Feature.properties;

            var number = properties.AddressHouseNumber;
            if (number.Contains(".0"))
            {
                number = number.Replace(".0", "");
            }

            address = number + " " + properties.AddressStreet;
            address = address.Trim();

            if (address == string.Empty)
            {
                address = buildingDataClass.Name;
            }

            var name = buildingDataClass.Name;
            if (buildingDataClass.DatabaseName != string.Empty)
            {
                name = buildingDataClass.DatabaseName;
            }

            /*
            var location = new Dictionary<string, double>();
            location.Add("lat", buildingSO.latitude);
            location.Add("lng", buildingSO.longitude);
            */

            var location = new BuildingLocation()
            {
                lat = buildingDataClass.Latitude,
                lng = buildingDataClass.Longitude
            };

            var zipcode = properties.AddressPostcode;
            if (zipcode.Contains(".0"))
            {
                zipcode = zipcode.Replace(".0", "");
            }

            Data data = new Data()
            {
                uuid = buildingDataClass.UUID,
                uid = "prop#" + buildingDataClass.UUID,
                city = buildingDataClass.Feature.properties.AddressCity,
                name = name,
                state = "",
                country = properties.AddressCountry,
                zipcode = zipcode,
                location = location,
                streetAddress = address
            };

            BuildingJSONData jsonData = new BuildingJSONData()
            {
                property_id = buildingDataClass.ID,
                property_name = name,
                address1 = address,
                city = buildingDataClass.Feature.properties.AddressCity,
                data = "[" + JsonUtility.ToJson(data) + "]"
            };
            
            return jsonData;
        }

        public BuildingDataClass ReturnBuildingDataClass()
        {
            return buildingDataClass;
        }

        public string SendBuildingDataMessage()
        {
            string jsonString = JsonUtility.ToJson(GetBuildingData());

            return jsonString;
        }

        #endregion
    }
}

