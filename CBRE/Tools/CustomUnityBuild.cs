#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomUnityBuild : MonoBehaviour
{
    [MenuItem("MyTools/Windows Build With Video Stitcher")]
    public static void BuildGame()
    {
        // Get filename.
        string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
        /*
        string[] levels = { SceneManager.GetSceneByBuildIndex(0).path };

        // Build player.
        BuildPipeline.BuildPlayer(levels, path + "/" + Application.productName + ".exe", BuildTarget.StandaloneWindows, BuildOptions.None);

        //EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));

        // Copy a file from the project folder to the build folder, alongside the built game.
        FileUtil.CopyFileOrDirectory(Application.dataPath + "Resources/ffmpeg/", path + "/" + Application.productName + "_Data/");
        */

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
        {
            scenes = new[] { SceneManager.GetSceneByBuildIndex(0).path },

            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            // Copy a file from the project folder to the build folder, alongside the built game.
            FileUtil.CopyFileOrDirectory(Application.dataPath.Replace("BlueSkyUnity/Assets", "ffmpeg/"), path + "/" + Application.productName + "_Data/Resources/");
        }
        else if (summary.result == BuildResult.Failed)
        {
            UnityEngine.Debug.LogError("Build Failed");
        }
    }
}
#endif