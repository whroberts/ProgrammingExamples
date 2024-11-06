
using Esri.GameEngine.Geometry;


public class MapCreator : MapCreatorBase
{
    private void Awake()
    {
        authenticationHelper = GetComponent<AuthenticationHelper>();
        CheckForMarket();
    }

    private void Start()
    {
        CreateArcGISMapComponent();
        CreateArcGISMap();
    }

    private void CheckForMarket()
    {
        switch (market)
        {
            case Market.Manual:
                //Do Nothing, take the coordinates from the inspector
                break;
            case Market.Toronto:
                geographicCoordinates = new ArcGISPoint(-79.3873941, 43.6335676, 75, ArcGISSpatialReference.WGS84());
                break;
            case Market.LosAngeles:
                geographicCoordinates = new ArcGISPoint(-118.26613224, 34.04301641, 150, ArcGISSpatialReference.WGS84());
                break;
            case Market.NewYork:
                geographicCoordinates = new ArcGISPoint(-74.011267750708214, 40.706954641923517, 250, ArcGISSpatialReference.WGS84());
                break;
            case Market.Brooklyn:
                geographicCoordinates = new ArcGISPoint(-74.001248933134164, 40.6816351275727, 200, ArcGISSpatialReference.WGS84());
                break;
            case Market.Detroit:
                geographicCoordinates = new ArcGISPoint(-83.03900854, 42.33551866, 250, ArcGISSpatialReference.WGS84());
                break;
            case Market.WashingtonDC:
                geographicCoordinates = new ArcGISPoint(-77.02, 38.9, 250, ArcGISSpatialReference.WGS84());
                break;
            case Market.Dallas:
                geographicCoordinates = new ArcGISPoint(-96.798989722514747, 32.783640243190135, 76, ArcGISSpatialReference.WGS84());
                break;
            case Market.LasVegas:
                geographicCoordinates = new ArcGISPoint(-115.17648505352405, 36.11296174799743, 625, ArcGISSpatialReference.WGS84());
                break;
            case Market.Denver:
                geographicCoordinates = new ArcGISPoint(-104.99394087860637, 39.74720896855635, 1590, ArcGISSpatialReference.WGS84());
                break;
            case Market.SanJose:
                geographicCoordinates = new ArcGISPoint(-121.89055560347266, 37.33247437965613, 26, ArcGISSpatialReference.WGS84());
                break;
            case Market.Atlanta:
                geographicCoordinates = new ArcGISPoint(-84.38835476128482, 33.76778669212222, 289, ArcGISSpatialReference.WGS84());
                break;
            case Market.SanAntonio:
                geographicCoordinates = new ArcGISPoint(-98.49435960897192, 29.425215539120924, 197, ArcGISSpatialReference.WGS84());
                break;
            case Market.Bellevue:
                geographicCoordinates = new ArcGISPoint(-122.19835665808758, 47.614131491019165, 47, ArcGISSpatialReference.WGS84());
                break;
            case Market.SaltLakeCity:
                geographicCoordinates = new ArcGISPoint(-111.89081184565289, 40.7664051229623, 1300, ArcGISSpatialReference.WGS84());
                break;
            case Market.Seattle:
                geographicCoordinates = new ArcGISPoint(-122.33177573236489, 47.601774253206734, 22, ArcGISSpatialReference.WGS84());
                break;
            case Market.Houston:
                geographicCoordinates = new ArcGISPoint(-95.36945513398217, 29.76056488884895, 14, ArcGISSpatialReference.WGS84());
                break;
            case Market.Phoenix:
                geographicCoordinates = new ArcGISPoint(-112.071215, 33.445927, 300, ArcGISSpatialReference.WGS84());
                break;
            case Market.Boise:
                geographicCoordinates = new ArcGISPoint(-116.20221147548432, 43.614992203519655, 820, ArcGISSpatialReference.WGS84());
                break;
            case Market.BeverlyHills:
                geographicCoordinates = new ArcGISPoint(-118.4152456184131, 34.059399931248656, 60, ArcGISSpatialReference.WGS84());
                break;
            case Market.SanFrancisco:
                geographicCoordinates = new ArcGISPoint(-122.41912898661855, 37.77475778080879, 5, ArcGISSpatialReference.WGS84());
                break;
            case Market.Boston:
                geographicCoordinates = new ArcGISPoint(-71.05718882347163, 42.361005711807515, 10, ArcGISSpatialReference.WGS84());
                break;
            case Market.Charlotte:
                geographicCoordinates = new ArcGISPoint(-80.8427102440622, 35.22643008041328, 230, ArcGISSpatialReference.WGS84());
                break;
            case Market.Chicago:
                geographicCoordinates = new ArcGISPoint(-87.63178565055736, 41.87929027281656, 181, ArcGISSpatialReference.WGS84());
                break;
            case Market.KansasCity:
                geographicCoordinates = new ArcGISPoint(-94.58198859174932, 39.10023618776234, 267, ArcGISSpatialReference.WGS84());
                break;
            case Market.Knoxville:
                geographicCoordinates = new ArcGISPoint(-83.92062160895792, 35.96051662608138, 300, ArcGISSpatialReference.WGS84());
                break;
            case Market.Tampa:
                geographicCoordinates = new ArcGISPoint(-82.45299119637929, 27.95168160700458, 48, ArcGISSpatialReference.WGS84());
                break;
            case Market.Tucson:
                geographicCoordinates = new ArcGISPoint(-110.97404446168566, 32.2209673807307, 500, ArcGISSpatialReference.WGS84());
                break;
        }
    }
}