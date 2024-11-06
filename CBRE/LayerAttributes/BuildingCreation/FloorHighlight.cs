using UnityEngine;

namespace BlueSky.BuildingManipulation
{
    [System.Serializable]
    public class FloorHighlight
    {
        public float floor;
        public float floorStart;
        public float floorEnd;
        public float floorHeight;
        public float floorPosition;
        public Color highlightColor;
        public Color emissionColor;
        public float emissionStrength;
        public float isEmissive;
    }

}
