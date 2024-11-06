using BlueSky.BuildingManipulation;
using UnityEngine;
using System;

namespace BlueSky.Data
{
    [System.Serializable]
    public class BuildingDataClass : MonoBehaviour
    {
        public int ID;
        public string UUID = string.Empty;
        public string Name = string.Empty;
        public string DatabaseName = string.Empty;
        public bool HasFeature = false;
        public bool FutureBuilding = false;
        public Features Feature = null;
        public double Latitude = 0;
        public double Longitude = 0;
        public double Elevation = 0;
        private BuildingHighlight BuildingHighlight = null;
        public int BuildingHighlightLayer = -1;

        public BuildingHighlight GetBuildingHighlight()
        {
            return this.BuildingHighlight;
        }

        /*
        public BuildingDataClass(int iD, string uuid, string name, string databaseName, bool hasFeature, bool futureBuilding,
            Features feature, double latitude, double longitude, double elevation,
            BuildingHighlight buildingHighlight, int buildingHighlightLayer)
        {
            this.ID = iD;
            this.UUID = uuid;
            this.Name = name;
            this.DatabaseName = databaseName;
            this.HasFeature = hasFeature;
            this.FutureBuilding = futureBuilding;
            this.Feature = feature;
            this.Latitude = latitude;
            this.Longitude = longitude;
            this.Elevation = elevation;
            this.BuildingHighlight = buildingHighlight;
            this.BuildingHighlightLayer = buildingHighlightLayer;
        }
        */
    }
}
