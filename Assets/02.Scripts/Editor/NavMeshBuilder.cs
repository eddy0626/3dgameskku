using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;

public static class NavMeshBuilder
{
    [MenuItem("Tools/NavMesh/Build NavMesh")]
    public static void BuildNavMesh()
    {
        // Set Ground as Navigation Static
        var ground = GameObject.Find("Ground");
        if (ground != null)
        {
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.NavigationStatic);
            Debug.Log("Ground set to Navigation Static");
        }

        // Set 2F_Floors children as Navigation Static
        var floors2F = GameObject.Find("2F_Floors");
        if (floors2F != null)
        {
            foreach (Transform child in floors2F.transform)
            {
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.NavigationStatic);
            }
            Debug.Log($"2F_Floors: {floors2F.transform.childCount} tiles set to Navigation Static");
        }

        // Set 2F_MEGA_Floors children as Navigation Static
        var megaFloors = GameObject.Find("2F_MEGA_Floors");
        if (megaFloors != null)
        {
            foreach (Transform child in megaFloors.transform)
            {
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.NavigationStatic);
            }
            Debug.Log($"2F_MEGA_Floors: {megaFloors.transform.childCount} tiles set to Navigation Static");
        }

        // Set 2F_Stairs children as Navigation Static
        var stairs = GameObject.Find("2F_Stairs");
        if (stairs != null)
        {
            foreach (Transform child in stairs.transform)
            {
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.NavigationStatic);
            }
            Debug.Log($"2F_Stairs: {stairs.transform.childCount} stairs set to Navigation Static");
        }

        // Build NavMesh
        var surface = Object.FindObjectOfType<NavMeshSurface>();
        if (surface != null)
        {
            surface.BuildNavMesh();
            Debug.Log("<color=green>NavMesh Built Successfully!</color>");
        }
        else
        {
            Debug.LogError("NavMeshSurface not found in scene!");
        }
    }

    [MenuItem("Tools/NavMesh/Clear NavMesh")]
    public static void ClearNavMesh()
    {
        var surface = Object.FindObjectOfType<NavMeshSurface>();
        if (surface != null)
        {
            surface.RemoveData();
            Debug.Log("NavMesh Cleared");
        }
    }
}