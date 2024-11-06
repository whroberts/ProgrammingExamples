using CBRE.ArcGis.Camera.Controllers;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class LayerCulling : MonoBehaviour
{
    [SerializeField] private bool onPresentation = true;
    [SerializeField] private bool onMapLoad = true;
    [SerializeField] public Camera customCamera = null;
    [SerializeField] private List<int> layerMasks = new List<int>();
    [SerializeField] private List<float> distances = new List<float>();

    public List<int> LayerMasks => layerMasks;
    public List<float> Distances => distances;

    private float[] cullDistances = new float[32];

    private PrefabLoading prefabLoading = null;

    private float maxDistance = 15000;
    private List<float> maxDistances = new List<float>() { 5000, 10000, 15000, 15000, 15000 };
    private List<float> prevDistances = new List<float>();

    private void Awake()
    {
        prefabLoading = GetComponent<PrefabLoading>();
    }
    private void OnEnable()
    {
        prefabLoading.OnFullMapCreated += new PrefabLoading.BuildingsLoadedEventHandler(CullDistances);
        prefabLoading.OnFullMapCreated += new PrefabLoading.BuildingsLoadedEventHandler(SetupRebase);
    }

    private void CullDistances()
    {
        if (onMapLoad)
        {
            /*
            for (var i = 0; i < layerMasks[0]; i++)
            {
                cullDistances[i] = 0f;
            }
            */

            var j = 0;
            foreach (var layerMask in layerMasks)
            {
                cullDistances[layerMask] = Mathf.Clamp(distances[j], 0, 15000f);
                j++;
            }
            /*
            for (var i = j; i < cullDistances.Length; i++)
            {
                cullDistances[i] = 0f;
            }
            */

            SetCullDistances();
        }

        customCamera.gameObject.SetActive(true);
        customCamera.enabled = true;
        
        prefabLoading.OnFullMapCreated -= new PrefabLoading.BuildingsLoadedEventHandler(CullDistances);
    }

    private void SetCullDistances()
    {
        if (customCamera != null)
        {
            customCamera.layerCullSpherical = true;
            customCamera.layerCullDistances = cullDistances;
        }

#if UNITY_EDITOR
        if(SceneView.lastActiveSceneView == null)
        {
            return;
        }
        var editorCamera = SceneView.lastActiveSceneView.camera;
        if (editorCamera != null)
        {
            editorCamera.layerCullSpherical = true;
            editorCamera.layerCullDistances = cullDistances;
        }
#endif
    }

    public void SetDistances(int layer, float distance)
    {
        cullDistances[layer] = distance;

        SetCullDistances();
    }

    public void SetToMaxCullDistance()
    {
        if (onPresentation)
        {
            Debug.Log("Setting to max cull distances");
            prevDistances = distances;


            int i = 0;
            foreach (var layer in layerMasks)
            {
                cullDistances[layer] = maxDistances[i];

                i++;
            }

            SetCullDistances();
        }
        else
        {
            Debug.Log("Not Setting to max cull distances");
        }
    }

    public void SetToPrevCullDistance()
    {
        if (onPresentation)
        {
            Debug.Log("Setting to previous cull distances");

            var i = 0;
            foreach (var layer in layerMasks)
            {
                cullDistances[layer] = prevDistances[i];
                i++;
            }

            SetCullDistances();
        }
    }

    private void SetupRebase()
    {
        RebaseWorld rebaseWorld = customCamera.GetComponent<RebaseWorld>();

        if (rebaseWorld != null)
        {
            rebaseWorld.AllowRebase(true);
        }

        prefabLoading.OnFullMapCreated -= new PrefabLoading.BuildingsLoadedEventHandler(SetupRebase);
    }
}
