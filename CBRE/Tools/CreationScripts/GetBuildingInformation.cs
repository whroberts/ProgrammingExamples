#if UNITY_EDITOR
using BlueSky.Data;
using Esri.GameEngine.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace BlueSky.Tools
{
    public class GetBuildingInformation
    {
        //private string aPIKey = "AAPK26d8c16393054ed9950fbb706c99763feTcV27Rp4xsLaIACjD1bgEP9BM2hGYE4D025sPnspbwmL6EJPQQ8s6qRRbk4yhRS";
        //private string congfigName = "Dimension";
        private string clientID = "a3ZZQHqo5oXXWYFJ";
        private string clientSecret = "e287354bdfe64ff7935a00e42065df96";

        //private string featureLayerURL = "https://services5.arcgis.com/QGM0VeSNqhUanPGN/arcgis/rest/services/North_Manhattan_gdb/FeatureServer/0/";
        //private string featureLayerURL = "https://services5.arcgis.com/QGM0VeSNqhUanPGN/arcgis/rest/services/Central_Manhattan_gdb/FeatureServer/0/";
        //private string featureLayerURL = "https://services5.arcgis.com/QGM0VeSNqhUanPGN/arcgis/rest/services/South_Manhattan_gdb/FeatureServer/0/";
        //private string featureLayerURL = "https://services5.arcgis.com/QGM0VeSNqhUanPGN/arcgis/rest/services/FinancialDistrict_Manhattan_gdb/FeatureServer/0/";
        //private string featureLayerURL = "https://services5.arcgis.com/QGM0VeSNqhUanPGN/arcgis/rest/services/North_Brooklyn_gdb/FeatureServer/0/";
        //private string featureLayerURL = "https://services5.arcgis.com/QGM0VeSNqhUanPGN/arcgis/rest/services/Central_Brooklyn_gdb/FeatureServer/0/";
        //private string featureLayerURL = "https://services5.arcgis.com/QGM0VeSNqhUanPGN/arcgis/rest/services/South_Brooklyn_gdb/FeatureServer/0/";
        //private string featureLayerURL = "https://services5.arcgis.com/QGM0VeSNqhUanPGN/arcgis/rest/services/WashingtonDC/FeatureServer/0/";
        //private string featureLayerURL = "https://services5.arcgis.com/QGM0VeSNqhUanPGN/arcgis/rest/services/Dallas_gdb/FeatureServer/0/";
        //private string featureLayerURL = "https://services5.arcgis.com/QGM0VeSNqhUanPGN/arcgis/rest/services/Atlanta_gdb/FeatureServer/0/";
        //private string featureLayerURL = "https://services5.arcgis.com/QGM0VeSNqhUanPGN/arcgis/rest/services/Bellevue_gdb/FeatureServer/0/";
        private string featureLayerURL = "https://services5.arcgis.com/QGM0VeSNqhUanPGN/arcgis/rest/services/Denver_gdb/FeatureServer/0/";


        private string tokenBaseURL = "https://www.arcgis.com/sharing/rest/oauth2/token?";
        private string tokenGrantType = "&grant_type=client_credentials";

        private ArcGISOAuthAuthorizationCredential oAuthCredential;

        private int featuresEstimateCount = 0;
        private bool continueQuery = false;
        private bool finishedQuery = false;
        public bool FinishedQuery => finishedQuery;

        private List<Features> featureList = new List<Features>();
        private FeatureDataset featureDataset = null;

        public void CreateFeatureList(string url)
        {
            if (url != string.Empty)
            {
                if (url.Contains("/0/"))
                {
                    featureLayerURL = url;
                }
                else
                {
                    featureLayerURL = url + "/0/";
                }
            }

            EditorCoroutineUtility.StartCoroutine(TokenWebRequest(), this);
        }

        public FeatureDataset SaveFeatureDataset(string featureDataOutputPath, out string featureDataImportPath)
        {
            featureDataset = ScriptableObject.CreateInstance<FeatureDataset>();
            EditorUtility.SetDirty(featureDataset);

            featureDataset.SetFeatures(featureList);

            string[] split = featureLayerURL.Split("/rest/services/", StringSplitOptions.None);
            string featureLayerName = split[1].Replace("/FeatureServer/0/", "").Trim();

            AssetDatabase.CreateAsset(featureDataset, featureDataOutputPath + "/" + featureLayerName + ".asset");
            AssetDatabase.SaveAssetIfDirty(featureDataset);
            AssetDatabase.SaveAssets();
            featureDataImportPath = AssetDatabase.GetAssetPath(featureDataset);
            return featureDataset;
        }

        // Generates token using OAuth2.0 App on ArcGIS developer dashboard
        private IEnumerator TokenWebRequest()
        {
            string GenerateTokenRequestURL = tokenBaseURL + "client_id=" + clientID + "&client_secret=" + clientSecret + tokenGrantType;
            Debug.Log("Generating Token");
            UnityWebRequest Request = UnityWebRequest.Get(GenerateTokenRequestURL);
            yield return Request.SendWebRequest();
            if (Request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(Request.error);
            }
            else
            {
                var deserialized = JsonUtility.FromJson<TokenResponseData>(Request.downloadHandler.text);
                CreateOAuthCredential(deserialized.access_token, DateTimeOffset.FromUnixTimeSeconds(deserialized.expires_in), "None");
            }
        }

        //Creates credential
        private void CreateOAuthCredential(string accessToken, DateTimeOffset expirationDate, string refreshToken)
        {
            //accessToken = "ddmsGhEXJiUHOQl-8omj3nFgPoCDIW_sWaOW2955xAEhZ2YFBEt5id83qiLBmHgtVPurCQWRN08rJEVSwOrsrxpRmiG1b1SPHImNcDy7my7jMqokNHtudXLUv4przkt8eFfo8ArbuEHaPifuYlOKFw..";
            oAuthCredential = new ArcGISOAuthAuthorizationCredential(accessToken, expirationDate, refreshToken);
            Debug.Log("Created Access Token: " + oAuthCredential.AccessToken);

            EditorCoroutineUtility.StartCoroutine(GetFeaturesEstimate(), this);
        }

        private IEnumerator GetFeaturesEstimate()
        {
            string EstimatesURL = featureLayerURL + "getEstimates?" + "token=" + oAuthCredential.AccessToken + "&f=json";
            UnityWebRequest Request = UnityWebRequest.Get(EstimatesURL);
            yield return Request.SendWebRequest();
            if (Request.result != UnityWebRequest.Result.Success)
            {
                //does not catch invalid tokens
                Debug.Log(Request.error);
            }
            else
            {
                var deserialzied = JsonUtility.FromJson<FeatureEstimateData>(Request.downloadHandler.text);
                featuresEstimateCount = deserialzied.count;
                Debug.Log("Features Count: " + deserialzied.count);

                EditorCoroutineUtility.StartCoroutine(GetAllFeatures(), this);
            }
        }

        private IEnumerator GetAllFeatures()
        {
            /*
            EditorCoroutineUtility.StartCoroutine(GetFeatures(1, true), this);
            yield return new WaitUntil(() => continueQuery);

            EditorCoroutineUtility.StartCoroutine(GetFeatures(2001, false), this);
            yield return new WaitUntil(() => finishedQuery);
            */

            var numQueries = featuresEstimateCount / 2000;
            int i = 0;

            for (i = 0; i < numQueries; i++)
            {
                if (i < numQueries)
                {
                    EditorCoroutineUtility.StartCoroutine(GetFeatures((i * 2000) + 1, true), this);
                    yield return new WaitUntil(() => continueQuery);
                }
            }

            if (i == numQueries)
            {
                EditorCoroutineUtility.StartCoroutine(GetFeatures((i * 2000) + 1, false), this);
                yield return new WaitUntil(() => finishedQuery);
            }
            AssetDatabase.SaveAssets();
        }

        private IEnumerator GetFeatures(int idToStart, bool continuing)
        {
            var RequestHeaders = "&where=OBJECTID>=" + idToStart + "&returnGeometry=false&outFields=*&f=geojson";
            string QueryRequestURL = featureLayerURL + "query?token=" + oAuthCredential.AccessToken + RequestHeaders;

            Debug.Log(QueryRequestURL);
            UnityWebRequest Request = UnityWebRequest.Get(QueryRequestURL);

            yield return Request.SendWebRequest();

            if (Request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(Request.error);
            }
            else
            {
                GetResponse(Request.downloadHandler.text, continuing);
            }
        }

        private void GetResponse(string Response, bool continuing)
        {
            var deserialized = JsonUtility.FromJson<FeatureCollectionData>(Response);

            if (deserialized.features != null)
            {
                if (deserialized.features.Length > 0)
                {
                    foreach (var feature in deserialized.features)
                    {
                        featureList.Add(feature);
                    }
                }
                else
                {
                    Debug.LogWarning("No features returned with the associated query");
                }
            }
            else
            {
                Debug.LogError("Invalid query");
            }

            if (continuing)
            {
                finishedQuery = false;
                continueQuery = true;
            }
            else
            {
                finishedQuery = true;
                continueQuery = false;
            }
        }
    }
}
#endif