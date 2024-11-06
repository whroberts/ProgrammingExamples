using UnityEngine;

[RequireComponent(typeof(AuthenticationHelper))]
public class BrooklynMapCreator : MapCreatorBase
{
    //center = 40.6816351275727, -74.00124893313416
    private void Awake()
    {
        authenticationHelper = GetComponent<AuthenticationHelper>();
    }

    private void Start()
    {
        CreateArcGISMapComponent();
        CreateArcGISMap();
    }
}
