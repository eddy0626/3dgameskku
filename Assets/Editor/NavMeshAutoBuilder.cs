using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;

[InitializeOnLoad]
public static class NavMeshAutoBuilder
{
    static NavMeshAutoBuilder()
    {
        // 에디터 업데이트 이벤트에 등록
        EditorApplication.delayCall += BuildNavMeshOnce;
    }
    
    private static void BuildNavMeshOnce()
    {
        NavMeshSurface[] surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        
        if (surfaces.Length == 0)
        {
            Debug.LogWarning("[NavMeshAutoBuilder] No NavMeshSurface found in scene!");
            return;
        }
        
        foreach (var surface in surfaces)
        {
            surface.BuildNavMesh();
            Debug.Log($"[NavMeshAutoBuilder] NavMesh built for: {surface.gameObject.name}");
        }
        
        EditorUtility.SetDirty(surfaces[0]);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log($"[NavMeshAutoBuilder] Successfully rebuilt {surfaces.Length} NavMesh surface(s)!");
    }
    
    [MenuItem("Tools/Rebuild NavMesh Now")]
    public static void RebuildNavMesh()
    {
        BuildNavMeshOnce();
    }
}
