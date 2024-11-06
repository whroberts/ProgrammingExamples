
using BlueSky.BuildingManipulation;
using System;
using UnityEngine;

namespace BlueSky.Data
{
    [System.Serializable]
    public struct BuildingData
    {
        public int ID;
        public string UUID;
        public string Name;
        public string DatabaseName;
        public bool HasFeature;
        public bool FutureBuilding;
        [NonSerialized] public Features Feature;
        public double Latitude;
        public double Longitude;
        public double Elevation;
        [NonSerialized] public BuildingHighlight BuildingHighlight;
        public int BuildingHighlightLayer;

        public BuildingData(int iD, string uuid, string name, string databaseName, bool hasFeature, bool futureBuilding,
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
    }
}
