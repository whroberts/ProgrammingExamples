using System.Collections.Generic;
using UnityEngine;

namespace BlueSky.BuildingManipulation
{
    public class BuildingHighlight
    {
        //public string uid;
        public bool fullHighlightEnabled;
        public bool floorHighlightsEnabled;
        public Color fullHighlightColor;
        public Color emissionColor;
        public float emissionStrength;
        public float isEmissive;
        public List<FloorHighlight> floorHighlights;
    }

}
