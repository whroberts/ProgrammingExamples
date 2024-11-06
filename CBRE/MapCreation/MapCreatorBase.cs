// Copyright 2022 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//

// ArcGISMapsSDK
using Esri.ArcGISMapsSDK.Components;
using Esri.GameEngine.Extent;
using Esri.GameEngine.Geometry;
using Esri.GameEngine.Layers;

// System

using Esri.GameEngine.Map;
using Esri.Unity;
using System;
using System.Collections.Generic;




// Unity
using UnityEngine;


[RequireComponent(typeof(AuthenticationHelper))]
public abstract class MapCreatorBase : MonoBehaviour
{
    protected enum Market
    {
        Manual = 0,
        Toronto = 1,
        LosAngeles = 2,
        NewYork = 3,
        Brooklyn = 4,
        Detroit = 5,
        WashingtonDC = 6,
        Dallas = 7,
        LasVegas = 8,
        Denver = 9,
        SanJose = 10,
        Atlanta = 11,
        SanAntonio = 12,
        Bellevue = 13,
        SaltLakeCity = 14,
        Seattle = 15,
        Houston = 16,
        Phoenix = 17,
        Boise = 18,
        BeverlyHills = 19,
        SanFrancisco = 20,
        Boston = 21,
        Charlotte = 22,
        Chicago = 23,
        KansasCity = 24,
        Knoxville = 25,
        Tampa = 26,
        Tucson = 27
    }

    [SerializeField] protected Market market;
    [Header("If Market is Manual, take these coordinates. Otherwise ignore\n")]
    [SerializeField] protected ArcGISPoint geographicCoordinates;
    public ArcGISPoint GeographicCoordinates => geographicCoordinates;

    [SerializeField] public ArcGISBasemapStyle basemapStyle = ArcGISBasemapStyle.ArcGISImageryStandard;

    protected ArcGISMapComponent arcGISMapComponent;
    public ArcGISMapComponent ArcGISMapComponent => arcGISMapComponent;

    //private ArcGISPoint geographicCoordinates = new ArcGISPoint(-79.3873941, 43.6335676, 1200, ArcGISSpatialReference.WGS84());

    protected AuthenticationHelper authenticationHelper;

    protected ArcGISMap arcGISMap;
    public ArcGISMap ArcGISMap => arcGISMap;

    protected ArcGISBasemap baseMap = null;
    public ArcGISBasemap BaseMap => baseMap;

    [Serializable]
    public class ImageLayerInfo{
        public string name;
        public string url;
        public float opacity;
        public bool isVisible;
    }

    [Header("Image Layers")]
    public List<ImageLayerInfo> imageLayers;

    protected ArcGISMapElevation elevationMap = null;
    public ArcGISMapElevation ElevationMap => elevationMap;

    [HideInInspector] public ArcGISBasemapStyle oldBasemapStyle = ArcGISBasemapStyle.ArcGISImageryStandard;

    public void SetBasemap(int basemap)
    {
        switch (basemap)
        {
            case 1:
                if (basemapStyle != ArcGISBasemapStyle.ArcGISImageryStandard)
                {
                    basemapStyle = ArcGISBasemapStyle.ArcGISImageryStandard;
                    CreateArcGISBasemapLayer();
                    CreateArcGISImageLayer();
                }
                break;
            case 11:
                if (basemapStyle != ArcGISBasemapStyle.ArcGISStreets)
                {
                    basemapStyle = ArcGISBasemapStyle.ArcGISStreets;
                    CreateArcGISBasemapLayer();
                    CreateArcGISImageLayer();
                }
                break;
        }

        //CreateArcGISBasemapLayer();
    }

    protected void CreateArcGISMapComponent()
    {
        arcGISMapComponent = FindObjectOfType<ArcGISMapComponent>();

        if (!arcGISMapComponent)
        {
            var arcGISMapGameObject = new GameObject("ArcGISMap");
            arcGISMapComponent = arcGISMapGameObject.AddComponent<ArcGISMapComponent>();
        }

        arcGISMapComponent.OriginPosition = geographicCoordinates;
        arcGISMapComponent.MapType = Esri.GameEngine.Map.ArcGISMapType.Global;
        arcGISMapComponent.MapTypeChanged += new ArcGISMapComponent.MapTypeChangedEventHandler(CreateArcGISMap);
    }

    protected void CreateArcGISBasemapLayer()
    {
        // Set the Basemap
        //baseMap = new ArcGISBasemap(ArcGISBasemapStyle.ArcGISImageryStandard, authenticationHelper.APIKey);
        baseMap = new ArcGISBasemap(basemapStyle, authenticationHelper.APIKey);
        arcGISMap.Basemap = baseMap;
    }



    protected void CreateArcGISImageLayer()
    {
        //https://services5.arcgis.com/QGM0VeSNqhUanPGN/arcgis/rest/services/374600_Canada_Toronto_Data/FeatureServer/0

        //var apiLayer = new ArcGISImageLayer("https://tiles.arcgis.com/tiles/oZfKvdlWHN1MwS48/arcgis/rest/services/TotPopTile2/MapServer", "MDF-HDF Total Population", 1, true, authenticationHelper.APIKey);

        if (imageLayers != null)
        {
            foreach (var layer in imageLayers)
            {
                if (layer.isVisible)
                {
                    var apiLayer = new ArcGISImageLayer(layer.url, layer.name, layer.opacity, true, authenticationHelper.APIKey);

                    arcGISMap.Layers.Add(apiLayer);
                }
            }
        }
    }



    protected void CreateArcGISElevationLayer()
    {
        elevationMap = new ArcGISMapElevation(new Esri.GameEngine.Elevation.ArcGISImageElevationSource("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer", "Elevation", ""));
        arcGISMap.Elevation = elevationMap;
    }

    protected void CreateArcGISExtent()
    {
        if (arcGISMap.MapType == ArcGISMapType.Local)
        {
            // Set this to true to enable an extent on the map component
            arcGISMapComponent.EnableExtent = true;

            var extentCenter = geographicCoordinates;
            var extent = new ArcGISExtentCircle(extentCenter, 5000);
            arcGISMap.ClippingArea = extent;
        }
    }

    public void CreateArcGISMap()
    {
        // Create the Map Document
        // You need to create a new ArcGISMap whenever you change the map type
        arcGISMap = new ArcGISMap(arcGISMapComponent.MapType);

        CreateArcGISBasemapLayer();
        CreateArcGISElevationLayer();
        CreateArcGISExtent();
        CreateArcGISImageLayer();
        //Authentication 
        authenticationHelper.CreateOAuthConfiguration();

        // We have completed setup and are ready to assign the ArcGISMap object to the View
        authenticationHelper.CallSetupAuthenticationOnPlay();
        arcGISMapComponent.View.Map = arcGISMap;

    }
    // @@End(ArcGISMap)
}
