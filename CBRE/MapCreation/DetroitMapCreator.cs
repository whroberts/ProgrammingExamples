using UnityEngine;

[RequireComponent(typeof(AuthenticationHelper))]
public class DetroitMapCreator : MapCreatorBase
{
    //center = 42.33551866, -83.03900854
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
