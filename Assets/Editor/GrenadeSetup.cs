using UnityEngine;
using UnityEditor;
using System.Linq;

public class GrenadeSetup : EditorWindow
{
    [MenuItem("Tools/Setup NewGrenade Mesh")]
    public static void SetupMesh()
    {
        Debug.Log("=== NewGrenade 메시 설정 시작 ===");
        
        // 씬에서 NewGrenade 찾기
        GameObject newGrenade = GameObject.Find("NewGrenade");
        if (newGrenade == null)
        {
            Debug.LogError("NewGrenade 오브젝트를 찾을 수 없습니다!");
            return;
        }
        Debug.Log("NewGrenade 발견!");
        
        // FBX에서 에셋 로드
        string fbxPath = "Assets/05.Models/Grenade.fbx";
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        Debug.Log("FBX 에셋 수: " + allAssets.Length);
        
        Mesh mesh = null;
        Material[] materials = null;
        
        foreach (Object asset in allAssets)
        {
            Debug.Log($"  - {asset.name} : {asset.GetType().Name}");
            if (asset is Mesh m)
            {
                mesh = m;
            }
        }
        
        materials = allAssets.OfType<Material>().ToArray();
        
        if (mesh == null)
        {
            Debug.LogError("메시를 찾을 수 없습니다!");
            return;
        }
        
        // 컴포넌트에 적용
        MeshFilter mf = newGrenade.GetComponent<MeshFilter>();
        MeshRenderer mr = newGrenade.GetComponent<MeshRenderer>();
        
        if (mf != null)
        {
            mf.sharedMesh = mesh;
            Debug.Log("메시 적용: " + mesh.name);
        }
        
        if (mr != null && materials.Length > 0)
        {
            mr.sharedMaterials = materials;
            Debug.Log("머티리얼 적용: " + materials.Length + "개");
        }
        
        // 스케일 조정
        newGrenade.transform.localScale = Vector3.one * 0.15f;
        
        Debug.Log("✅ NewGrenade 설정 완료!");
        
        // 씬 더티 마킹
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(newGrenade.scene);
    }
}
