using BlueSky.Data;
using BlueSky.BuildingManipulation;
using CBRE.Services.Presentation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlueSky.BuildingCreation
{
    [RequireComponent(typeof(BuildingPrefabManager))]
    public class PrefabRaycast : MonoBehaviour
    {
        [SerializeField] private RightClickSettings rightClickSettings = RightClickSettings.None;
        [Flags]
        private enum RightClickSettings
        {
            None = 0x0,
            EnableRightClick = 0x1,
            EnableHighlight = 0x2,
            EnableBuildingData = 0x4,
            EnableFloorPlan = 0x8
        }
#if UNITY_EDITOR
        [SerializeField] private FloorPlanController floorPlanController = null;
#endif
        [HideInInspector] public bool EnableSendingBuildingData = false;

        private PresentationSceneManager presentationSceneManager = null;
        private BuildingPrefabManager buildingPrefabManager = null;
        private InputHandler inputHandler = null;



        private void Awake()
        {
            buildingPrefabManager = GetComponent<BuildingPrefabManager>();
            presentationSceneManager = GetComponentInParent<MapCreatorBase>()?.GetComponentInChildren<PresentationSceneManager>();
        }

        private void Start()
        {
            if (buildingPrefabManager != null && rightClickSettings.HasFlag(RightClickSettings.EnableRightClick))
            {
                buildingPrefabManager.PrefabLoading.OnFullMapCreated += new PrefabLoading.BuildingsLoadedEventHandler(SetupRaycast);
            }
        }

        private void OnDisable()
        {
            if (inputHandler != null)
            {
                inputHandler.E_RightDoubleTap -= OnRightClickDouble;
            }
        }

        private void SetupRaycast()
        {
            if (inputHandler == null)
            {
                inputHandler = GetComponent<LayerCulling>().customCamera.GetComponent<InputHandler>();

                if (inputHandler == null)
                {
                    Debug.LogError("InputHandler.cs is required for raycast");
                }
                else
                {
                    inputHandler.E_RightDoubleTap += OnRightClickDouble;
                }
            }


            buildingPrefabManager.PrefabLoading.OnFullMapCreated -= new PrefabLoading.BuildingsLoadedEventHandler(SetupRaycast);
        }


        private void OnRightClickDouble(Vector2 position)
        {
            Ray ray = Camera.main.ScreenPointToRay(position);
            RaycastToBuilding(ray);
        }

        private void RaycastToBuilding(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var buildingControl = hit.collider.GetComponentInParent<CreatedBuildingControl>();
                if (buildingControl != null)
                {
                    if (rightClickSettings.HasFlag(RightClickSettings.EnableHighlight))
                    {
                        buildingControl.EnableHighlightFromManager(RandomBuildingHighlight());
                    }

                    if (rightClickSettings.HasFlag(RightClickSettings.EnableBuildingData))
                    {
                        string json = buildingControl.SendBuildingDataMessage();
                        Debug.Log(json);

                        var data = buildingControl.GetBuildingData();

                        if (!buildingPrefabManager.PrefabHighlightManager.buildingJSONDatas.Contains(data))
                        {
                            //buildingPrefabManager.PrefabHighlightManager.buildingJSONDatas.Add(data);
                        }

                        if (EnableSendingBuildingData)
                        {
                            if (presentationSceneManager != null)
                            {
                                presentationSceneManager.SendBuildingData(data);
                            }
                        }
                    }
#if UNITY_EDITOR
                    if (rightClickSettings.HasFlag(RightClickSettings.EnableFloorPlan) && floorPlanController != null)
                    {
                        floorPlanController.CallLoadFloorPlan(hit.point, buildingControl.gameObject);
                    }
#endif

                }
                else
                {
                    var radius = 20f;
                    if (Physics.CheckSphere(hit.point, radius))
                    {
                        HighlightInRadius(ray, hit, radius);
                    }
                    else
                    {
                        Debug.Log("Increasing Radius");
                        radius *= 2;
                        HighlightInRadius(ray, hit, radius);
                    }
                }
            }
            else
            {
                Debug.Log("Didn't Hit Building");
            }
        }

        private BuildingHighlight RandomBuildingHighlight()
        {
            Color color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));

            BuildingHighlight highlight = new BuildingHighlight()
            {
                fullHighlightColor = color,
                emissionColor = color,
                isEmissive = 1,
                fullHighlightEnabled = true,
                floorHighlightsEnabled = false
            };

            return highlight;
        }

        int numTries = 1;

        private void HighlightInRadius(Ray ray, RaycastHit hit, float radius)
        {
            numTries++;

            bool highlightAll = false;
            var colliders = Physics.OverlapSphere(hit.point, radius);

            /*
            if (colliders.Length == 0 || colliders == null)
            {
                HighlightInRadius(ray, hit, radius * numTries);
            }
            */

            if (highlightAll)
            {
                List<CreatedBuildingControl> buildingsHit = new List<CreatedBuildingControl>();
                foreach (var collider in colliders)
                {
                    CreatedBuildingControl createdBuildingControl = collider.GetComponentInParent<CreatedBuildingControl>();
                    if (createdBuildingControl != null)
                    {
                        if (!buildingsHit.Contains(createdBuildingControl))
                        {
                            createdBuildingControl.EnableHighlightFromManager(RandomBuildingHighlight());

                            buildingsHit.Add(createdBuildingControl);
                        }

                    }

                }
            }
            else
            {
                Dictionary<CreatedBuildingControl, float> buildingsHit = new Dictionary<CreatedBuildingControl, float>();
                foreach (var collider in colliders)
                {
                    CreatedBuildingControl createdBuildingControl = collider.GetComponentInParent<CreatedBuildingControl>();
                    if (createdBuildingControl != null)
                    {
                        float distance = Vector3.Distance(collider.ClosestPointOnBounds(hit.point), hit.point);
                        if (!buildingsHit.ContainsKey(createdBuildingControl))
                        {
                            buildingsHit.Add(createdBuildingControl, distance);
                        }
                        else
                        {
                            if (buildingsHit[createdBuildingControl] > distance)
                            {
                                buildingsHit[createdBuildingControl] = distance;
                            }
                        }
                    }
                }

                CreatedBuildingControl closestBuilding = null;
                float closestPoint = 10000f;

                foreach (var buildingSet in buildingsHit)
                {
                    var building = buildingSet.Key;
                    var distance = buildingSet.Value;

                    if (distance < closestPoint)
                    {
                        closestPoint = distance;
                        closestBuilding = building;
                    }
                }

                if (closestBuilding != null)
                {
                    closestBuilding.EnableHighlightFromManager(RandomBuildingHighlight());
                }
            }

            colliders = null;
        }
    }
}
