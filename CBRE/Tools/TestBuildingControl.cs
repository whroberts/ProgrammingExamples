using BlueSky.Data;
using BlueSky.BuildingManipulation;
using Esri.ArcGISMapsSDK.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;

namespace BlueSky.Tools
{
    public class TestBuildingControl : MonoBehaviour
    {
        [SerializeField] public BuildingData buildingData;

        private DecalProjector fullHighlightDecalProjector = null;
        [Tooltip("Decal Projectors by floor number")]
        [HideInInspector] public Dictionary<int, DecalProjector> floorHighlightDecalProjectors = new Dictionary<int, DecalProjector>();
        public GameObject highPolyModel = null;
        private Bounds bounds = new Bounds();
        public Bounds Bounds => bounds;
        public Material fullHighlightDecalMaterial = null;
        public Material floorHighlightDecalMaterial = null;
        [HideInInspector] public bool hasFullHighlight = false;

        [HideInInspector] public BuildingPrefabManager prefabManager = null;

        public GameObject fullHighlightPrefab = null;
        public GameObject floorHighlightPrefab = null;

        public string popupId;
        public string markerId;

        public float height = 0;

        public void PlaceBuilding(ArcGISMapComponent arcGISMapComponent)
        {
            var properties = buildingData.Feature.properties;
            gameObject.transform.parent = arcGISMapComponent.transform;
            var locationComponent = gameObject.AddComponent<ArcGISLocationComponent>();
            locationComponent.Position = new ArcGISPoint(properties.Longitude, properties.Latitude, properties.Elevation, ArcGISSpatialReference.WGS84());
            locationComponent.Rotation = new ArcGISRotation(180, 90, 0);
            locationComponent.SyncPositionWithHPTransform();
            DestroyImmediate(locationComponent);
        }
    }

}
