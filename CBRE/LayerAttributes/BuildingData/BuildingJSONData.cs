namespace BlueSky.Data
{
    [System.Serializable]
    public class BuildingJSONData
    {
        public int property_id = 0;
        public string property_name = string.Empty;
        public string address1 = string.Empty;
        public string address2 = string.Empty;
        public string city = string.Empty;
        //public Data data = null;
        public string data = string.Empty;
        //public string ModelName = string.Empty;
        //public string BuildingObjectName = string.Empty;
        //public bool hasFeature = false;
        //public double Latitude = 0;
        //public double Longitude = 0;
        //public double Elevation = 0;
        //public float Height = 0;
        //public bool FutureBuilding = false;
        //public Features Feature = null;
        //public GameObject highPolyModel = null;
        //public GameObject buildingGameObject = null;
        //public BuildingHighlight buildingHighlight = null;
    }

    [System.Serializable]
    public class Data
    {
        public string uuid = string.Empty;
        public string uid = string.Empty;
        public string city = string.Empty;
        public string name = string.Empty;
        public string state = string.Empty;
        public string country = string.Empty;
        public string zipcode = string.Empty;
        //public Dictionary<string, double> location = new Dictionary<string, double>();
        public BuildingLocation location;
        public string streetAddress = string.Empty;
    }

    [System.Serializable]
    public class BuildingLocation
    {
        public double lat;
        public double lng;
    }

}
