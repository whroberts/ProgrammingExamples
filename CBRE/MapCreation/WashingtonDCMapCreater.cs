
using UnityEngine;

[RequireComponent(typeof(AuthenticationHelper))]
public class WashingtonDCMapCreater : MapCreatorBase
{
    //center = 38.9, -77.02
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
