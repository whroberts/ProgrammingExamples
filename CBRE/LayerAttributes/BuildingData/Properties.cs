namespace BlueSky.Data
{
    // Must be named this due to how the data is received on query
    [System.Serializable]
    public class Properties
    {
        public int OBJECTID;
        public double BuildingLevels;
        public double Height;
        public double OSMId;
        public string Name;

        public double Elevation;
        public double Latitude;
        public double Longitude;

        public double ShapePivotX;
        public double ShapePivotY;
        public double ShapePivotZ;

        public double WorldPosX;
        public double WorldPosY;
        public double WorldPosZ;

        public double WorldRotX;
        public double WorldRotY;
        public double WorldRotZ;

        public double WorldScaleX;
        public double WorldScaleY;
        public double WorldScaleZ;

        public string Access;
        public string AddressCity;
        public string AddressCountry;
        public string AddressDistrict;
        public string AddressFlats;
        public string AddressFull;
        public string AddressHouseName;
        public string AddressHouseNumber;
        public string AddressPlace;
        public string AddressPostcode;
        public string AddressProvince;
        public string AddressStreet;
        public string AddressSubdistrict;
        public string AddressUnit;
        public string Aeroway;
        public string Building;
        public string Name__EN;
        public string OfficialName;
        public string Model_Name;
        public string Name_from_model;

        public enum ProperityFields
        {
            OBJECTID,
            Name,
            Height,
            BuildingLevels,
            OSMId,

            elevation,
            latitude,
            longitude,

            shapePivotX,
            shapePivotY,
            shapePivotZ,

            worldPosX,
            worldPosY,
            worldPosZ,

            worldRotX,
            worldRotY,
            worldRotZ,

            worldScaleX,
            worldScaleY,
            worldScaleZ,

            Access,
            AddressCity,
            AddressCountry,
            AddressDistrict,
            AddressFlats,
            AddressFull,
            AddressHouseName,
            AddressHouseNumber,
            AddressPlace,
            AddressPostcode,
            AddressProvince,
            AddressStreet,
            AddressUnit,
            Aeroway,
            Building,
            Name__EN,
            OfficialName
        }
    }
}
