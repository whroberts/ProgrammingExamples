using BlueSky.BuildingManipulation;
using System.Collections.Generic;
using UnityEngine;

namespace BlueSky.Data
{
    [System.Serializable]
    public class Building : ScriptableObject
    {
        public int ID;
        public string Name = "";
        public string uid = "";
        public bool hasFeature = false;
        //public string modelPath = "";
        //public string prefabPath = "";
        //public List<GameObject> gameObjects = new List<GameObject>();
        public Features feature = null;
        public double latitude = 0;
        public double longitude = 0;
        public GameObject highPolyModel = null;
        public GameObject buildingGameObject = null;
        public List<FloorHighlight> floorHighlights = null;
    }
}

