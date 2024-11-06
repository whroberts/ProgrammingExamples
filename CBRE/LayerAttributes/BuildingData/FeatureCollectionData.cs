namespace BlueSky.Data
{
    // Must be named this due to how the data is received on query
    [System.Serializable]
    public class FeatureCollectionData
    {
        public string type;
        public Features[] features;
    }
}
