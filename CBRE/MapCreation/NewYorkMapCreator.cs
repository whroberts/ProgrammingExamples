using UnityEngine;

[RequireComponent(typeof(AuthenticationHelper))]
public class NewYorkMapCreator : MapCreatorBase
{
    //center = 40.70695464192352, -74.01126775070821
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
