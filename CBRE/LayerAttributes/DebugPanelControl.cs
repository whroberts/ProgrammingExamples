using CBRE.Services.Presentation;
using Newtonsoft.Json;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;

public class DebugPanelControl : MonoBehaviour
{
    [Header("Use Debug Menu")]
    [SerializeField] private bool useDebugMenu = true;

    [Header("UI")]
    [SerializeField] private RectTransform debugPanel = null;
    [SerializeField] private TMP_Text fpsText = null;

    [Header("Activating/Deactiving")]
    [SerializeField] private TMP_InputField hexInput = null;
    [SerializeField] private Slider redSliderMat = null;
    [SerializeField] private Slider greenSliderMat = null;
    [SerializeField] private Slider blueSliderMat = null;
    [SerializeField] private Slider alphaSliderMat = null;
    [SerializeField] private RawImage displayColorMat = null;
    [SerializeField] private TMP_Text redTextMat = null;
    [SerializeField] private TMP_Text greenTextMat = null;
    [SerializeField] private TMP_Text blueTextMat = null;
    [SerializeField] private TMP_Text alphaTextMat = null;

    [Header("Manual Highlighting")]
    [SerializeField] private Slider redSliderHighlight = null;
    [SerializeField] private Slider greenSliderHighlight = null;
    [SerializeField] private Slider blueSliderHighlight = null;
    [SerializeField] private Slider alphaSliderHighlight = null;
    [SerializeField] private RawImage displayColorHighlight = null;
    [SerializeField] private TMP_Text redTextHighlight = null;
    [SerializeField] private TMP_Text greenTextHighlight = null;
    [SerializeField] private TMP_Text blueTextHighlight = null;
    [SerializeField] private TMP_Text alphaTextHighlight = null;

    [Header("Draw Distance")]
    [SerializeField] private TMP_InputField extraSmallDistance = null;
    [SerializeField] private TMP_InputField smallDistance = null;
    [SerializeField] private TMP_InputField mediumDistance = null;
    [SerializeField] private TMP_InputField largeDistance = null;
    [SerializeField] private TMP_InputField extraLargeDistance = null;

    [SerializeField] private TMP_InputField presentationID = null;
    [SerializeField] private TMP_InputField token = null;

    [Header("RecreateMap")]
    [SerializeField] private GameObject nukePanel = null;
    private bool nukePanelEnabled = false;

    [Header("JumpToScene")]
    [SerializeField] private TMP_InputField sceneJSON = null;

    [Header("Scripts")]
    [SerializeField] private MapCreatorBase mapCreator = null;
    [SerializeField] private BuildingPrefabManager buildingPrefabManager = null;
    [SerializeField] private PostProcessingManager postProcessingManager = null;
    [SerializeField] private LayerCulling layerCulling = null;
    [SerializeField] private PresentationSceneManager presentationSceneManager = null;
    [SerializeField] private PropertiesPresentation propertiesPresentation = null;

    //fps
    private float updateInterval = 0.5f;
    private float accum = 0.0f;
    private int frames = 0;
    private float timeLeft = 0.0f;
    private float fps = 0.0f;

    //bools
    private bool debugPanelEnabled = false;
    private bool isColorSwatchEnabled = false;

    //Color
    private Color inactiveColor = new Color(0f, 0f, 0f, 0.5f);
    public Color InactiveColor => inactiveColor;

    private Color highlightColor = new Color(0f, 1f, 0f, 0.5f);
    public Color HighlightColor => highlightColor;

    private float startTime = 0;
    private float finishTime = 0;

    private void OnEnable()
    {
        presentationSceneManager.OnSetToken += new PresentationSceneManager.SetDebugMenuElements(SetTokenUI);
        presentationSceneManager.OnSetPresentationID += new PresentationSceneManager.SetDebugMenuElements(SetPresentationIDUI);
    }

    private void OnDisable()
    {
        presentationSceneManager.OnSetToken -= new PresentationSceneManager.SetDebugMenuElements(SetTokenUI);
        presentationSceneManager.OnSetPresentationID -= new PresentationSceneManager.SetDebugMenuElements(SetPresentationIDUI);
    }

    private void SetTokenUI(string tokenString)
    {
        token.text = tokenString;
    }

    private void SetPresentationIDUI(string id)
    {
        presentationID.text = id;
    }

    public void RunDebugMenu()
    {
        if (debugPanel != null && useDebugMenu)
        {
            DisplayDebugControls();
        }

        if (fpsText != null && useDebugMenu)
        {
            DisplayFPS();
        }
    }



    private void DisplayDebugControls()
    {
        if (Input.GetKeyDown(KeyCode.M) || Input.GetMouseButtonDown(2))
        {
            if (debugPanel != null && !debugPanelEnabled)
            {
                debugPanel.gameObject.SetActive(true);
                debugPanelEnabled = true;
            }
            else if (debugPanel != null && debugPanelEnabled)
            {
                debugPanel.gameObject.SetActive(false);
                debugPanelEnabled = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.N) && debugPanelEnabled)
        {
            if (nukePanel != null && !nukePanelEnabled)
            {
                nukePanel.SetActive(true);
                nukePanelEnabled = true;
            }
            else if (nukePanel != null && nukePanelEnabled)
            {
                nukePanel.SetActive(false);
                nukePanelEnabled = false;
            }
        }
    }

    private void DisplayFPS()
    {
        if (fpsText != null)
        {
            timeLeft -= Time.deltaTime;
            accum += Time.timeScale / Time.deltaTime;
            frames++;

            if (timeLeft <= 0.0)
            {
                fps = (accum / frames);
                timeLeft = updateInterval;
                accum = 0;
                frames = 0;
            }

            fpsText.text = "FPS: " + (int) fps;
        }
    }

    //tied to button
    public void ButtonToActivateBuildings()
    {
        if (buildingPrefabManager != null)
        {
            buildingPrefabManager.ActivateBuildingsFromUI();
        }
    }

    // tied to button
    public void ButtonToDeactivateBuildings()
    {
        if (buildingPrefabManager != null)
        {
            buildingPrefabManager.DeactivateBuildingsFromUI(inactiveColor);
        }
    }

    public void ClearAllHighlights()
    {
        buildingPrefabManager.PrefabHighlightManager.DisableHighlights(true, true);
    }

    public void ColorStart()
    {
        redSliderMat.value = inactiveColor.r * 255;
        greenSliderMat.value = inactiveColor.g * 255;
        blueSliderMat.value = inactiveColor.b * 255;
        alphaSliderMat.value = inactiveColor.a * 255;
        displayColorMat.color = inactiveColor;
        redTextMat.text = redSliderMat.value.ToString();
        greenTextMat.text = greenSliderMat.value.ToString();
        blueTextMat.text = blueSliderMat.value.ToString();
        alphaTextMat.text = alphaSliderMat.value.ToString();

        redSliderHighlight.value = highlightColor.r * 255;
        greenSliderHighlight.value = highlightColor.g * 255;
        blueSliderHighlight.value = highlightColor.b * 255;
        alphaSliderHighlight.value = highlightColor.a * 255;
        displayColorHighlight.color = highlightColor;
        redTextHighlight.text = redSliderHighlight.value.ToString();
        greenTextHighlight.text = greenSliderHighlight.value.ToString();
        blueTextHighlight.text = blueSliderHighlight.value.ToString();
        alphaTextHighlight.text = alphaSliderHighlight.value.ToString();

        isColorSwatchEnabled = true;
    }

    public void ColorSwatch()
    {
        if (isColorSwatchEnabled)
        {
            inactiveColor.r = redSliderMat.value / 255;
            inactiveColor.g = greenSliderMat.value / 255;
            inactiveColor.b = blueSliderMat.value / 255;
            inactiveColor.a = alphaSliderMat.value / 255;

            redTextMat.text = redSliderMat.value.ToString();
            greenTextMat.text = greenSliderMat.value.ToString();
            blueTextMat.text = blueSliderMat.value.ToString();
            alphaTextMat.text = alphaSliderMat.value.ToString();
            hexInput.text = ColorUtility.ToHtmlStringRGBA(inactiveColor);


            displayColorMat.color = inactiveColor;

            highlightColor.r = redSliderHighlight.value / 255;
            highlightColor.g = greenSliderHighlight.value / 255;
            highlightColor.b = blueSliderHighlight.value / 255;
            highlightColor.a = alphaSliderHighlight.value / 255;

            redTextHighlight.text = redSliderHighlight.value.ToString();
            greenTextHighlight.text = greenSliderHighlight.value.ToString();
            blueTextHighlight.text = blueSliderHighlight.value.ToString();
            alphaTextHighlight.text = alphaSliderHighlight.value.ToString();

            displayColorHighlight.color = highlightColor;
        }
    }

    public void HexCodeInput()
    {
        string input = hexInput.text;
        if (!input.Contains("#"))
        {
            input = "#" + input;
        }

        if (ColorUtility.TryParseHtmlString(input, out Color hexColor))
        {
            inactiveColor = hexColor;
            ColorStart();
        }
    }

    public void SetStartTime(float time)
    {
        startTime = time;

        if (finishTime == 0)
        {
            ChangeTimeOfDay(time);
        }
    }

    public void SetFinishTime(float time)
    {
        finishTime = time;
    }

    public void RunSunLerp()
    {
        postProcessingManager.StartTimeOfDayLerp(startTime, finishTime);

        debugPanel.gameObject.SetActive(false);
        debugPanelEnabled = false;

        startTime = 0;
        finishTime = 0;
    }

    public void ChangeTimeOfDay(float newTime)
    {
        if (postProcessingManager != null)
        {
            postProcessingManager.UpdateTimeOfDay(newTime);
        }
    }

    public void InitCullDistances()
    {
        extraSmallDistance.text = layerCulling.Distances[0].ToString();
        smallDistance.text = layerCulling.Distances[1].ToString();
        mediumDistance.text = layerCulling.Distances[2].ToString();
        largeDistance.text = layerCulling.Distances[3].ToString();
        extraLargeDistance.text = layerCulling.Distances[4].ToString();
    }

    public void CullDistances()
    {
        if (float.TryParse(extraSmallDistance.text, out float extraSmallDistanceValue))
        {
            layerCulling.SetDistances(6, extraSmallDistanceValue);
        }

        if (float.TryParse(smallDistance.text, out float smallDistanceValue))
        {
            layerCulling.SetDistances(7, smallDistanceValue);
        }

        if (float.TryParse(mediumDistance.text, out float mediumDistanceValue))
        {
            layerCulling.SetDistances(8, mediumDistanceValue);
        }

        if (float.TryParse(largeDistance.text, out float largeDistanceValue))
        {
            layerCulling.SetDistances(9, largeDistanceValue);
        }

        if (float.TryParse(extraLargeDistance.text, out float extraLargeDistanceValue))
        {
            layerCulling.SetDistances(10, extraLargeDistanceValue);
        }
    }

    public void ToggleFutureBuildings()
    {
        buildingPrefabManager.ToggleFutureBuildings(true);

        debugPanel.gameObject.SetActive(false);
        debugPanelEnabled = false;
    }

    public void RunPreview()
    {
        //token.text.Trim();
        if (!string.IsNullOrEmpty(presentationID.text) && !string.IsNullOrEmpty(token.text))
        {
            ApiConstants.token = token.text.Trim();
            presentationSceneManager.StartPreview(presentationID.text.Trim());
        }
    }

    public void RunExport()
    {
        // Have RecordingManager listen for PresentationAnimationManager events to automatically start/stop recording
        // Call before RunPreview to guarantee the RecordingManager catches the StartAnimation event
        //RecordingManager.Instance.ListenForAnimEvents(PresentationSceneManager.Instance.presentationAnimationManager);
        presentationSceneManager.SetExportVideo();
        RunPreview();
    }

    public void SetBasemap(int basemap)
    {
        mapCreator.SetBasemap(basemap);

        debugPanel.gameObject.SetActive(false);
        debugPanelEnabled = false;
    }

    public void StartRecording()
    {
        if (RecordingManager.Instance != null)
        {
            RecordingManager.Instance.StartRecording();
        }
    }
    public void StopRecording()
    {
        if (RecordingManager.Instance != null)
        {
            RecordingManager.Instance.StopRecording();
        }
    }

    public void NukeBuildings()
    {
        foreach (var building in buildingPrefabManager.PrefabLoading.BuildingsByUID.Values)
        {
            DestroyImmediate(building);
        }
        buildingPrefabManager.PrefabLoading.BuildingsByUID.Clear();
    }

    public void RecreateBuildings()
    {
        buildingPrefabManager.PrefabLoading.CreateBuildings();
        nukePanel.SetActive(false);
        nukePanelEnabled = false;
    }

    public void RunJumpToScene()
    {
        if (sceneJSON != null)
        {
            CBRE.Models.Scene scene = JsonConvert.DeserializeObject<CBRE.Models.Scene>(sceneJSON.text);
            presentationSceneManager.JumpToScene(scene);
        }
    }

    public void AnimatePropertiesPresentation()
    {
        if (!string.IsNullOrEmpty(token.text))
        {
            ApiConstants.token = token.text.Trim();
        }
        propertiesPresentation.StratDebugAniamtion(presentationSceneManager);
    }

    public void CallLoadLandingScene()
    {
        StartCoroutine(LoadLandingScene());
    }

    private IEnumerator LoadLandingScene()
    {
        Application.backgroundLoadingPriority = ThreadPriority.High;
        AsyncOperation loadLandingScene = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);

        while (!loadLandingScene.isDone)
        {
            if (loadLandingScene.progress >= 0.9f)
            {
                Debug.Log("Closing Map Scene, returning to Landing Scene");

                //loadLandingScene.allowSceneActivation = true;
            }

            yield return null;
        }

        //Task connect = WsClient.Instance.Connect(WsClient.Instance.WebSocketQueryStringUrl);
    }
}
