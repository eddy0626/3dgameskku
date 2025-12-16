using UnityEngine;
using UnityEditor;
using System.Linq;

public class GrenadeModelReplacer : EditorWindow
{
    [MenuItem("Tools/Replace Grenade Mesh")]
    public static void ReplaceMesh()
    {
        Debug.Log("=== 수류탄 메시 교체 시작 ===");
        
        // 새 FBX 모델에서 메시 가져오기
        string fbxPath = "Assets/05.Models/Grenade.fbx";
        GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxModel == null)
        {
            Debug.LogError("FBX 모델을 찾을 수 없습니다: " + fbxPath);
            return;
        }
        Debug.Log("FBX 로드 성공: " + fbxModel.name);
        
        // FBX 내부의 모든 에셋 검색
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        Debug.Log("FBX 내부 에셋 개수: " + allAssets.Length);
        
        Mesh newMesh = null;
        Material[] newMaterials = null;
        
        foreach (Object asset in allAssets)
        {
            Debug.Log($"  - {asset.name} ({asset.GetType().Name})");
            
            if (asset is Mesh mesh)
            {
                newMesh = mesh;
                Debug.Log($"메시 발견: {mesh.name}, 정점: {mesh.vertexCount}");
            }
            else if (asset is Material mat)
            {
                Debug.Log($"머티리얼 발견: {mat.name}");
            }
        }
        
        // 머티리얼 배열 생성
        newMaterials = allAssets.OfType<Material>().ToArray();
        Debug.Log($"총 머티리얼 수: {newMaterials.Length}");
        
        if (newMesh == null)
        {
            Debug.LogError("FBX에서 메시를 찾을 수 없습니다!");
            return;
        }
        
        // 기존 프리팹 로드
        string prefabPath = "Assets/03.Prefabs/Weapons/Grenade.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("프리팹을 찾을 수 없습니다: " + prefabPath);
            return;
        }
        Debug.Log("프리팹 로드 성공: " + prefab.name);
        
        // 프리팹 인스턴스화 및 수정
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        MeshFilter meshFilter = instance.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = instance.GetComponent<MeshRenderer>();
        
        if (meshFilter != null)
        {
            meshFilter.sharedMesh = newMesh;
            Debug.Log("메시 교체 완료: " + newMesh.name);
        }
        else
        {
            Debug.LogError("프리팹에 MeshFilter가 없습니다!");
            DestroyImmediate(instance);
            return;
        }
        
        if (meshRenderer != null && newMaterials.Length > 0)
        {
            meshRenderer.sharedMaterials = newMaterials;
            Debug.Log("머티리얼 교체 완료: " + newMaterials.Length + "개");
        }
        
        // 스케일 조정 (FBX가 너무 크거나 작을 수 있음)
        // 기본적으로 1로 유지, 필요시 조정
        instance.transform.localScale = Vector3.one * 0.15f; // 적절한 크기로 조정
        Debug.Log("스케일 조정: 0.15");
        
        // 프리팹에 변경사항 저장
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        DestroyImmediate(instance);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("✅ 수류탄 프리팹 메시 교체 완료!");
    }
}
