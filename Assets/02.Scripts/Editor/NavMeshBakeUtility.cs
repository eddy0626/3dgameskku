using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;

/// <summary>
/// NavMesh 베이크 유틸리티
/// </summary>
public static class NavMeshBakeUtility
{
    [MenuItem("Tools/FPS Setup/Bake NavMesh")]
    public static void BakeNavMesh()
    {
        // 씬의 모든 NavMeshSurface 찾기
        NavMeshSurface[] surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        
        if (surfaces.Length == 0)
        {
            Debug.LogWarning("[NavMeshBakeUtility] NavMeshSurface가 씬에 없습니다.");
            return;
        }
        
        foreach (var surface in surfaces)
        {
            surface.BuildNavMesh();
            Debug.Log($"[NavMeshBakeUtility] NavMesh 베이크 완료: {surface.gameObject.name}");
        }
        
        // 씬 저장
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[NavMeshBakeUtility] 모든 NavMesh 베이크 및 씬 저장 완료!");
    }
}
