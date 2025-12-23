using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BuildSettingsSetup : Editor
{
    [MenuItem("Tools/Setup Build Settings")]
    public static void SetupBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        
        // Add LoginScene first (index 0)
        string loginScenePath = "Assets/01.Scenes/LoginScene.unity";
        if (System.IO.File.Exists(loginScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(loginScenePath, true));
            Debug.Log("Added LoginScene to Build Settings");
        }
        
        // Add SampleScene (index 1)
        string sampleScenePath = "Assets/Scenes/SampleScene.unity";
        if (System.IO.File.Exists(sampleScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(sampleScenePath, true));
            Debug.Log("Added SampleScene to Build Settings");
        }
        
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("Build Settings updated successfully!");
    }
}
