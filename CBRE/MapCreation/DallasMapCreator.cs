using UnityEngine;

[RequireComponent(typeof(AuthenticationHelper))]
public class DallasMapCreator : MapCreatorBase
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
