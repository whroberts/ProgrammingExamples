using BlueSky.Data;
using BlueSky.BuildingManipulation;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BuildingPrefabManager))]
public class PrefabHighlightManager : MonoBehaviour
{
    [Tooltip("Buildings sorted by their game objects")]
    [SerializeField] private Dictionary<GameObject, BuildingDataClass> highlightedBuildings = new Dictionary<GameObject, BuildingDataClass>();
    [HideInInspector] public Dictionary<GameObject, BuildingDataClass> HighlightedBuildings => highlightedBuildings;
    [SerializeField] private Dictionary<GameObject, BuildingDataClass> inactiveBuildings = new Dictionary<GameObject, BuildingDataClass>();
    [HideInInspector] public Dictionary<GameObject, BuildingDataClass> InactiveBuildings => inactiveBuildings;

    public List<BuildingJSONData> buildingJSONDatas = new List<BuildingJSONData>();

    private BuildingPrefabManager prefabManager = null;

    #region Unity Functions
    private void Awake()
    {
        prefabManager = GetComponent<BuildingPrefabManager>();
        buildingJSONDatas.Clear();
    }

#if UNITY_EDITOR
    /*
    private void OnDisable()
    {
        if (buildingJSONDatas.Count > 0)
        {
            try
            {
                var path = "./Assets/Models/JSON/" + SceneManager.GetActiveScene().name.Replace("_GameCast", "") + "CustomList.json";
                File.WriteAllText(path, JsonConvert.SerializeObject(buildingJSONDatas));

                Debug.Log("Created JSONBuildingData file");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to write JSONBuildingData to file");
                Debug.LogException(e);
            }
        }
    }
    */
#endif
    #endregion

    #region Saturation
    public void SaturateBuildings()
    {
        foreach (var building in inactiveBuildings.Keys)
        {
            try
            {
                if (building != null)
                {
                    var buildingControl = building.GetComponent<CreatedBuildingControl>();

                    if (buildingControl != null)
                    {
                        buildingControl.SetActiveModel(true);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("SaturateBuildings Error");
            }
        }

        inactiveBuildings.Clear();
    }

    public void DesaturateBuildings()//Color inactiveColor)
    {
        foreach (var building in prefabManager.PrefabLoading.BuildingsByUID.Values)
        {
            try
            {
                if (building != null)
                {
                    var buildingControl = building.GetComponent<CreatedBuildingControl>();

                    if (buildingControl != null)
                    {
                        var buildingDataClass = buildingControl.buildingDataClass;
                        if (!highlightedBuildings.ContainsValue(buildingDataClass))
                        {
                            buildingControl.SetActiveModel(false);
                            if (!inactiveBuildings.ContainsKey(building))
                            {
                                inactiveBuildings.Add(building, buildingDataClass);

                                //buildingControl.SetLowPolyTexture(inactiveColor);
                            }
                            else
                            {
                                //buildingControl.SetLowPolyTexture(inactiveColor);
                            }
                        }
                        else
                        {
                            buildingControl.SetActiveModel(true);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("DesaturateBuildings Error");
            }
        }
    }

    #endregion

    #region Full Highlight

    #region Enable
    /// <summary>
    /// Calls actual function on building
    /// </summary>
    /// <param name="buildingGO"></param>
    /// <param name="buildingHighlight"></param>
    public void EnableFullHighlight(GameObject buildingGO, BuildingHighlight buildingHighlight)
    {
        try
        {
            var buildingControl = buildingGO.GetComponent<CreatedBuildingControl>();

            if (buildingControl != null)
            {
                var buildingDataClass = buildingControl.buildingDataClass;

                if (!highlightedBuildings.ContainsKey(buildingGO))
                {
                    highlightedBuildings.Add(buildingGO, buildingDataClass);
                }

                if (inactiveBuildings.ContainsKey(buildingGO))
                {
                    inactiveBuildings.Remove(buildingGO);
                }

                buildingControl.SetActiveModel(true);
                buildingControl.HighlightBuilding(buildingHighlight);
                //buildingControl.EnableFullHighlight(buildingHighlight);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("EnableFullHighlight(GameObject, BuildingHighlight) Error");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Called when highlighting building with uuid and already existing building highlight
    /// </summary>
    /// <param name="uuid"></param>
    /// <param name="buildingHighlight"></param>
    public void EnableFullHighlight(string uuid, BuildingHighlight buildingHighlight)
    {
        try
        {
            if (prefabManager.PrefabLoading.BuildingsByUID.TryGetValue(uuid, out GameObject buildingGO))
            {
                if (buildingGO != null)
                {
                    EnableFullHighlight(buildingGO, buildingHighlight);
                }
                else
                {
                    Debug.LogError("Building is null at uuid: " + uuid);
                }
            }
            else
            {
                Debug.LogError("No building at uuid: " + uuid);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("EnableFullHighlight(string, BuildingHighlight) Error");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Called when you have the building object and color
    /// </summary>
    /// <param name="buildingGO"></param>
    /// <param name="highlightColor"></param>
    public void EnableFullHighlight(GameObject buildingGO, Color highlightColor)
    {
        try
        {
            var buildingControl = buildingGO.GetComponent<CreatedBuildingControl>();

            if (buildingControl != null)
            {
                var buildingDataClass = buildingControl.buildingDataClass;

                if (buildingDataClass != null)
                {
                    BuildingHighlight buildingHighlight = buildingDataClass.GetBuildingHighlight();
                    buildingHighlight.fullHighlightEnabled = true;
                    buildingHighlight.fullHighlightColor = highlightColor;
                    buildingHighlight.emissionColor = highlightColor;
                    buildingHighlight.isEmissive = 1;

                    EnableFullHighlight(buildingGO, buildingHighlight);
                }

            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("EnableFullHighlight(GameObject, Color) Error");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Called when highlighting building with uuid and color
    /// </summary>
    /// <param name="uuid"></param>
    /// <param name="highlightColor"></param>
    public void EnableFullHighlight(string uuid, Color highlightColor)
    {
        try
        {
            if (prefabManager.PrefabLoading.BuildingsByUID.TryGetValue(uuid, out GameObject buildingGO))
            {
                if (buildingGO != null)
                {
                    EnableFullHighlight(buildingGO, highlightColor);
                }
                else
                {
                    Debug.LogError("Building is null");
                }
            }
            else
            {
                Debug.LogError("No building at uuid: " + uuid);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("EnableFullHighlight(string, Color) Error");
            Debug.LogException(e);
        }
    }
    #endregion

    #region Disable

    /// <summary>
    /// Disabling Full Highlight by gameObject
    /// </summary>
    /// <param name="buildingGO"></param>
    public void DisableFullHighlight(GameObject buildingGO)
    {
        try
        {
            var buildingControl = buildingGO.GetComponent<CreatedBuildingControl>();

            if (buildingControl != null)
            {
                buildingControl.SetActiveModel(true);
                buildingControl.DisableFullHighlight();

                // check to make sure there are no floor highlights before removing from list
                if (buildingControl.buildingDataClass.GetBuildingHighlight().floorHighlights.Count <= 0)
                {
                    if (highlightedBuildings.ContainsKey(buildingGO))
                    {
                        highlightedBuildings.Remove(buildingGO);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("DisableFullHighlight(GameObject) Error");
        }
    }

    /// <summary>
    /// Disabling Full Highlight by uuid
    /// </summary>
    /// <param name="uuid"></param>
    public void DisableFullHighlight(string uuid)
    {
        try
        {
            if (prefabManager.PrefabLoading.BuildingsByUID.TryGetValue(uuid, out GameObject buildingGO))
            {
                if (buildingGO != null)
                {
                    DisableFullHighlight(buildingGO);
                }
                else
                {
                    Debug.LogError("Building is null at uuid: " + uuid);
                }
            }
            else
            {
                Debug.LogError("No building at uuid: " + uuid);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("DisableFullHighlight(string) Error");
        }
    }

    /// <summary>
    /// Disabling all full highlights
    /// </summary>
    public void DisableFullHighlight()
    {
        foreach (var buildingGO in highlightedBuildings.Keys)
        {
            try
            {
                DisableFullHighlight(buildingGO);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("DisableFullHighlight() Error");
            }
        }
    }
    #endregion

    #endregion

    #region Individual Floor Highlight

    #region Create
    
    /// <summary>
    /// Create floor highlights by gameObject
    /// </summary>
    /// <param name="buildingGO"></param>
    /// <param name="floorHighlight"></param>
    public void CreateIndividualFloorHighlight(GameObject buildingGO, FloorHighlight floorHighlight)
    {
        try
        {
            var buildingControl = buildingGO.GetComponent<CreatedBuildingControl>();

            if (buildingControl != null)
            {
                var buildingDataClass = buildingControl.buildingDataClass;

                if (prefabManager.PrefabLoading.BuildingsByUID.ContainsValue(buildingGO))
                {
                    // check to make sure we don't double add
                    if (!highlightedBuildings.ContainsKey(buildingGO))
                    {
                        highlightedBuildings.Add(buildingGO, buildingDataClass);
                    }

                    // check to remove from inactive
                    if (inactiveBuildings.ContainsKey(buildingGO))
                    {
                        inactiveBuildings.Remove(buildingGO);
                    }

                    buildingControl.SetActiveModel(true);
                    buildingControl.CreateIndividualFloorHighlight(floorHighlight);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("CreateIndividualFloorHighlight(GameObject, FloorHighlight) Error");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Create floor highlight by uuid
    /// </summary>
    /// <param name="uuid"></param>
    /// <param name="floorHighlight"></param>
    public void CreateIndividualFloorHighlight(string uuid, FloorHighlight floorHighlight)
    {
        try
        {
            if (prefabManager.PrefabLoading.BuildingsByUID.TryGetValue(uuid, out GameObject buildingGO))
            {
                if (buildingGO != null)
                {
                    CreateIndividualFloorHighlight(buildingGO, floorHighlight);
                }
                else
                {
                    Debug.LogError("Building is null at uuid: " + uuid);
                }
            }
            else
            {
                Debug.LogError("No building found at uuid: " + uuid);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("CreateIndividualFloorHighlight(string, FloorHighlight) Error");
            Debug.LogException(e);
        }
    }
    #endregion

    #region Disable

    /// <summary>
    /// Disables Floor Highlights on GameObject for list or all
    /// </summary>
    /// <param name="buildingGO"></param>
    /// <param name="floorList"></param>
    public void DisableFloorHighlights(GameObject buildingGO, List<int> floorList = null)
    {
        try
        {
            var buildingControl = buildingGO.GetComponent<CreatedBuildingControl>();
            if (buildingControl != null)
            {
                if (floorList != null)
                {
                    foreach (var floor in floorList)
                    {
                        if (buildingControl.floorHighlightDecalProjectors.ContainsKey(floor))
                        {
                            buildingControl.DisableIndividualFloorHighlight(floor);
                        }
                    }
                }
                else
                {
                    buildingControl.DisableAllIndividualFloorHighlights();
                }

                if (!buildingControl.hasFullHighlight)
                {
                    if (highlightedBuildings.ContainsKey(buildingGO))
                    {
                        highlightedBuildings.Remove(buildingGO);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("DisableFloorHighlight(GameObject, List<int>) Error");
        } 
    }

    /// <summary>
    /// Disables Floor Highlights by uuid for list or all
    /// </summary>
    /// <param name="uuid"></param>
    /// <param name="floorList"></param>
    public void DisableFloorHighlights(string uuid, List<int> floorList = null)
    {
        try
        {
            if (prefabManager.PrefabLoading.BuildingsByUID.TryGetValue(uuid, out GameObject buildingGO))
            {
                if (buildingGO != null)
                {
                    DisableFloorHighlights(buildingGO, floorList);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("DisableFloorHighlight(string, List<int>) Error");
        }
    }

    public void DisableHighlights(bool disableFull = false, bool disableFloors = false)
    {
        foreach (var buildingGO in highlightedBuildings.Keys)
        {
            try
            {
                var buildingControl = buildingGO.GetComponent<CreatedBuildingControl>();
                if (buildingControl != null)
                {
                    if (disableFull)
                    {
                        buildingControl.DisableFullHighlight();
                    }
                    if (disableFloors && buildingControl.floorHighlightDecalProjectors.Count > 0)
                    {
                        buildingControl.DisableAllIndividualFloorHighlights();
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error Disabling Building Highlights");
                Debug.LogException(e);
            }
        }

        foreach (var buildingGO in inactiveBuildings.Keys)
        {
            try
            {
                var buildingControl = buildingGO.GetComponent<CreatedBuildingControl>();
                if (buildingControl != null)
                {
                    if (disableFull)
                    {
                        buildingControl.DisableFullHighlight();
                    }

                    if (disableFloors && buildingControl.floorHighlightDecalProjectors.Count > 0)
                    {
                        buildingControl.DisableAllIndividualFloorHighlights();
                    }

                    buildingControl.SetActiveModel(true);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error Disabling Inactive Building Highlights");
                Debug.LogException(e);
            }
        }

        highlightedBuildings.Clear();
        inactiveBuildings.Clear();
    }
    #endregion
    #endregion

    #region Public Functions
    public CreatedBuildingControl GetCreatedBuildingControlById(string uid)
    {
        try
        {
            if (prefabManager.PrefabLoading.BuildingsByUID.TryGetValue(uid, out GameObject buildingSO))
            {
                return buildingSO.GetComponent<CreatedBuildingControl>();
            }
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError("GetCreatedBuildingControlById Error");
            Debug.LogException(e);
            return null;
        }
    }
    #endregion
}
