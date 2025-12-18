using UnityEngine;
using UnityEditor;

/// <summary>
/// 씬과 프리팹에서 Missing Script를 찾는 에디터 유틸리티
/// </summary>
public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts")]
    public static void FindMissing()
    {
        Debug.Log("=== Searching for Missing Scripts ===");
        int missingCount = 0;
        
        // 씬 내 모든 게임오브젝트 검색
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        
        foreach (GameObject go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();
            
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingCount++;
                    string path = GetFullPath(go);
                    Debug.LogError($"Missing Script found on: {path} (Component index: {i})", go);
                }
            }
        }
        
        if (missingCount == 0)
        {
            Debug.Log("No missing scripts found in scene!");
        }
        else
        {
            Debug.LogError($"Total missing scripts found: {missingCount}");
        }
    }
    
    [MenuItem("Tools/Find Missing Scripts in All Prefabs")]
    public static void FindMissingInPrefabs()
    {
        Debug.Log("=== Searching for Missing Scripts in Prefabs ===");
        int missingCount = 0;
        
        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab");
        
        foreach (string guid in prefabPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                {
                    Component[] components = child.gameObject.GetComponents<Component>();
                    
                    for (int i = 0; i < components.Length; i++)
                    {
                        if (components[i] == null)
                        {
                            missingCount++;
                            Debug.LogError($"Missing Script in Prefab: {path} on {child.name}", prefab);
                        }
                    }
                }
            }
        }
        
        if (missingCount == 0)
        {
            Debug.Log("No missing scripts found in prefabs!");
        }
        else
        {
            Debug.LogError($"Total missing scripts in prefabs: {missingCount}");
        }
    }
    
    static string GetFullPath(GameObject go)
    {
        string path = go.name;
        Transform parent = go.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
}
