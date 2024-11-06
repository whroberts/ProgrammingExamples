using BlueSky.BuildingCreation;
using BlueSky.Data;
using Esri.ArcGISMapsSDK.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPrefabManager : MonoBehaviour
{
    [Header("Scripts")]
    [SerializeField] private MapCreatorBase mapCreator = null;
    [SerializeField] private AuthenticationHelper authenticationHelper = null;
    [SerializeField] private ArcGISMapComponent arcGISMapComponent = null;
    [SerializeField] private DebugPanelControl debugPanelControl = null;
    [SerializeField] private PrefabRaycast prefabRaycast = null;

    public MapCreatorBase MapCreator => mapCreator;
    public ArcGISMapComponent ArcGISMapComponent => arcGISMapComponent;
    public AuthenticationHelper AuthenticationHelper => authenticationHelper;

    private PrefabLoading prefabLoading = null;
    public PrefabLoading PrefabLoading => prefabLoading;

    private PrefabHighlightManager prefabHighlightManager = null;
    public PrefabHighlightManager PrefabHighlightManager => prefabHighlightManager;

    public PrefabRaycast PrefabRaycast => prefabRaycast;

    //private LandingScene.Markets currentMarket = LandingScene.Markets.None;
    //public LandingScene.Markets CurrentMarket => currentMarket;

    private Color highlightColor = Color.green;

    List<GameObject> hitBuildings = new List<GameObject>();

    private void Awake()
    {
        prefabLoading = GetComponent<PrefabLoading>();
        prefabHighlightManager = GetComponent<PrefabHighlightManager>();
    }

    private void Start()
    {
        if (debugPanelControl != null)
        {
            debugPanelControl.ColorStart();
            debugPanelControl.InitCullDistances();
        }
    }

    private void Update()
    {
        if (debugPanelControl != null)
        {
            debugPanelControl.RunDebugMenu();
        } 
    }


    public void OnClickFullHighlight(GameObject clickedBuilding)
    {
        /*
        if (buildingCreationUI != null)
        {
            buildingHighlightManager.EnableFullHighlight(clickedBuilding, buildingCreationUI.HighlightColor);
        }
        */
        if (debugPanelControl != null)
        {
            highlightColor = debugPanelControl.HighlightColor;
        }
        else
        {
            highlightColor = Color.green;
        }
        prefabHighlightManager.EnableFullHighlight(clickedBuilding, highlightColor);
    }

    public void ToggleBuildingSaturation(bool state)
    {
        if (state)
        {
            prefabHighlightManager.DesaturateBuildings();
        }
        else
        {
            prefabHighlightManager.SaturateBuildings();
        }
    }


    public void ActivateBuildingsFromUI()
    {
        prefabHighlightManager.SaturateBuildings();
    }

    public void DeactivateBuildingsFromUI(Color inactiveColor)
    {
        prefabHighlightManager.DesaturateBuildings();
    }

    public void ClearHighlight()
    {
        prefabHighlightManager.DisableHighlights(true, true);
    }

    public void ToggleFutureBuildings(bool flip, bool state = true)
    {
        if (flip)
        {
            foreach (var buildingGO in prefabLoading.FutureBuildingsByUID.Values)
            {
                if (buildingGO.activeInHierarchy)
                {
                    buildingGO.SetActive(false);
                }
                else
                {
                    buildingGO.SetActive(true);
                }
            }
        }
        else
        {
            foreach (var buildingGO in prefabLoading.FutureBuildingsByUID.Values)
            {
                if (buildingGO != null)
                {
                    buildingGO.SetActive(state);
                }
            }
        }
    }

    public void AllBuildingsState(bool active, Vector3 location)
    {
        /*
        foreach (var building in prefabLoading.BuildingsByUID.Values)
        {
            building.SetActive(active);
        }

        if (active)
        {
            //StartCoroutine(ReplaceBuildings());
        }
        */

        var colliders = Physics.OverlapSphere(location, 100f);

        if (active)
        {
            foreach (var building in hitBuildings)
            {
                building.SetActive(active);
            }
            hitBuildings.Clear();
        }
        else
        {
            foreach (var collider in colliders)
            {
                var control = collider.GetComponentInParent<CreatedBuildingControl>();

                if (control != null)
                {
                    if (!hitBuildings.Contains(control.gameObject))
                    {
                        control.gameObject.SetActive(active);

                        hitBuildings.Add(control.gameObject);
                    }
                }
            }
        }
    }

    private IEnumerator ReplaceBuildings()
    {
        int count = 0;
        int modValue = 2500;
        foreach (var building in prefabLoading.BuildingsByUID.Values)
        {
            var control = building.GetComponent<CreatedBuildingControl>();
            if (control != null)
            {
                control.PlaceBuilding(arcGISMapComponent);
            }
            count++;

            if (count % modValue == 0)
            {
                yield return new WaitForSeconds(2f);
            }
        }
    }

    //public void SetCurrentMarket(LandingScene.Markets market)
    //{
    //    currentMarket = market;
    //}
}
