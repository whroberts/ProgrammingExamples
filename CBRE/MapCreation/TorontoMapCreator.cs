using UnityEngine;

[RequireComponent(typeof(AuthenticationHelper))]
public class TorontoMapCreator : MapCreatorBase
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
