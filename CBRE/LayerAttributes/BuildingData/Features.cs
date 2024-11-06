namespace BlueSky.Data
{
    // Must be named this due to how the data is received on query
    // Very annoying because it is only referencing one feature, but must be named "Features" plural
    [System.Serializable]
    public class Features
    {
        public string type;
        public Properties properties;
        public int id;
    }
}

