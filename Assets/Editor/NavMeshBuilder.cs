using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;

public static class NavMeshBuilder
{
    [MenuItem("Tools/Build NavMesh")]
    public static void BuildNavMesh()
    {
        NavMeshSurface[] surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        
        if (surfaces.Length == 0)
        {
            Debug.LogWarning("[NavMeshBuilder] No NavMeshSurface found in scene!");
            return;
        }
        
        foreach (var surface in surfaces)
        {
            surface.BuildNavMesh();
            Debug.Log($"[NavMeshBuilder] NavMesh built for: {surface.gameObject.name}");
        }
        
        Debug.Log($"[NavMeshBuilder] Successfully built {surfaces.Length} NavMesh surface(s)!");
    }
}
