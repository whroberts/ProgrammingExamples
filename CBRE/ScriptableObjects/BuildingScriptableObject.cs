using BlueSky.BuildingManipulation;
using UnityEngine;

namespace BlueSky.Data
{
    [CreateAssetMenu(fileName = "BuildingData", menuName = "ScriptableObjects/BuildingData", order = 1)]
    [System.Serializable]
    public class BuildingScriptableObject : ScriptableObject
    {
        public int ID;
        public string uid = string.Empty;
        public string Name = string.Empty;
        public string DatabaseName = string.Empty;
        public bool hasFeature = false;
        public bool futureBuilding = false;
        public Features feature = null;
        public double latitude = 0;
        public double longitude = 0;
        public GameObject highPolyModel = null;
        public GameObject buildingGameObject = null;
        public BuildingHighlight buildingHighlight = null;
        public int buildingHighlightLayer = -1;
    }
}
