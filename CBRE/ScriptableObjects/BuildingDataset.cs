using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingDataset", menuName = "ScriptableObjects/BuildingDataset", order = 3)]
public class BuildingDataset : ScriptableObject
{
    [SerializeField] private List<GameObject> buildingsToLoad;
    [SerializeField] private List<GameObject> buildingsWithoutFeatures;

    public void SetBuildingsToLoad(List<GameObject> buildingsToLoad)
    {
        this.buildingsToLoad = buildingsToLoad;
    }

    public void SetBuildingsToLoad(GameObject buildingToLoad)
    {
        this.buildingsToLoad.Add(buildingToLoad);
    }

    public List<GameObject> GetBuildingsToLoad()
    {
        return this.buildingsToLoad;
    }

    public void SetBuildingsWithoutFeatures(List<GameObject> buildingsWithoutFeatures)
    {
        this.buildingsWithoutFeatures = buildingsWithoutFeatures;
    }

    public void SetBuildingsWithoutFeatures(GameObject buildingWithoutFeatures)
    {
        this.buildingsWithoutFeatures.Add(buildingWithoutFeatures);
    }

    public List<GameObject> GetBuildingsWithoutFeatures()
    {
        return this.buildingsWithoutFeatures;
    }

    public int GetDatasetLength()
    {
        return this.buildingsToLoad.Count;
    }
}
