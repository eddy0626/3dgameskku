
using UnityEngine;
using UnityEditor;

public static class ModelInstantiator
{
    [MenuItem("Tools/Spawn Soldier Model")]
    public static void SpawnSoldierModel()
    {
        string modelPath = "Assets/05.Models/Characters/Modern_Soldier.fbx";
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        
        if (modelPrefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            instance.name = "Soldier_Model";
            instance.transform.position = new Vector3(0, 0, 5);
            Selection.activeGameObject = instance;
            Debug.Log("Soldier model spawned successfully!");
        }
        else
        {
            Debug.LogError("Could not load model at: " + modelPath);
        }
    }
}
