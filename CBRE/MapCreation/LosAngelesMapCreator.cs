using UnityEngine;

[RequireComponent(typeof(AuthenticationHelper))]
public class LosAngelesMapCreator : MapCreatorBase
{
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
