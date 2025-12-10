using UnityEngine;
using UnityEditor;

/// <summary>
/// War FX 이펙트 자동 설정 에디터 유틸리티
/// 메뉴: Tools > FPS Setup > Setup War FX Effects
/// </summary>
public class WarFXSetupUtility : EditorWindow
{
    [MenuItem("Tools/FPS Setup/Setup War FX Effects")]
    public static void SetupWarFXEffects()
    {
        SetupMuzzleFlashController();
        SetupImpactEffectManager();
        
        EditorUtility.DisplayDialog("War FX Setup", 
            "War FX 이펙트 설정이 완료되었습니다!\n\n" +
            "- MuzzleFlashController: 머즐플래시 프리팹 연결\n" +
            "- ImpactEffectManager: 임팩트 이펙트 프리팹 연결\n\n" +
            "씬을 저장해주세요.", "확인");
    }
    
    private static void SetupMuzzleFlashController()
    {
        MuzzleFlashController controller = Object.FindFirstObjectByType<MuzzleFlashController>();
        
        if (controller == null)
        {
            GameObject go = new GameObject("MuzzleFlashController");
            controller = go.AddComponent<MuzzleFlashController>();
            Debug.Log("[WarFXSetup] MuzzleFlashController 생성됨");
        }
        
        SerializedObject so = new SerializedObject(controller);
        
        // 기본 머즐플래시
        SetPrefabReference(so, "_defaultMuzzleFlashPrefab", 
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/MuzzleFlashes/FPS/WFX_MF FPS RIFLE1.prefab");
        
        // 라이플 머즐플래시 배열
        SerializedProperty rifleProp = so.FindProperty("_rifleMuzzleFlashPrefabs");
        if (rifleProp != null)
        {
            rifleProp.arraySize = 3;
            SetArrayElementPrefab(rifleProp, 0, "Assets/10.Assets/JMO Assets/WarFX/_Effects/MuzzleFlashes/FPS/WFX_MF FPS RIFLE1.prefab");
            SetArrayElementPrefab(rifleProp, 1, "Assets/10.Assets/JMO Assets/WarFX/_Effects/MuzzleFlashes/FPS/WFX_MF FPS RIFLE2.prefab");
            SetArrayElementPrefab(rifleProp, 2, "Assets/10.Assets/JMO Assets/WarFX/_Effects/MuzzleFlashes/FPS/WFX_MF FPS RIFLE3.prefab");
        }
        
        // 피스톨/샷건
        SetPrefabReference(so, "_pistolMuzzleFlashPrefab",
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/MuzzleFlashes/4Planes/WFX_MF 4P RIFLE1.prefab");
        SetPrefabReference(so, "_shotgunMuzzleFlashPrefab",
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/MuzzleFlashes/4Planes/WFX_MF 4P RIFLE2.prefab");
        
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(controller);
        
        Debug.Log("[WarFXSetup] MuzzleFlashController 설정 완료");
    }
    
    private static void SetupImpactEffectManager()
    {
        ImpactEffectManager manager = Object.FindFirstObjectByType<ImpactEffectManager>();
        
        if (manager == null)
        {
            Debug.LogWarning("[WarFXSetup] ImpactEffectManager가 씬에 없습니다.");
            return;
        }
        
        SerializedObject so = new SerializedObject(manager);
        
        SetPrefabReference(so, "_defaultParticlePrefab",
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/WFX_BImpact Concrete.prefab");
        SetPrefabReference(so, "_defaultDecalPrefab",
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/+ Bullet Hole/WFX_BImpact Dirt + Hole.prefab");
        
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
        
        CreateImpactDataAssets();
        
        Debug.Log("[WarFXSetup] ImpactEffectManager 설정 완료");
    }
    
    private static void CreateImpactDataAssets()
    {
        string dataPath = "Assets/02.Scripts/Weapons/Data/ImpactEffects";
        
        if (!AssetDatabase.IsValidFolder(dataPath))
        {
            AssetDatabase.CreateFolder("Assets/02.Scripts/Weapons/Data", "ImpactEffects");
        }
        
        // Default (콘크리트)
        CreateOrUpdateImpactData(dataPath, "ImpactData_Default", SurfaceType.Default,
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/WFX_BImpact Concrete.prefab",
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/+ Bullet Hole/Lit/WFX_BImpact Concrete + Hole Lit.prefab");
        
        // Metal
        CreateOrUpdateImpactData(dataPath, "ImpactData_Metal", SurfaceType.Metal,
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/WFX_BImpact Metal.prefab",
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/+ Bullet Hole/Lit/WFX_BImpact Metal + Hole Lit.prefab");
        
        // Wood
        CreateOrUpdateImpactData(dataPath, "ImpactData_Wood", SurfaceType.Wood,
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/WFX_BImpact Wood.prefab",
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/+ Bullet Hole/Lit/WFX_BImpact Wood + Hole Lit.prefab");
        
        // Dirt
        CreateOrUpdateImpactData(dataPath, "ImpactData_Dirt", SurfaceType.Dirt,
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/WFX_BImpact Dirt.prefab",
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/+ Bullet Hole/WFX_BImpact Dirt + Hole.prefab");
        
        // Flesh
        CreateOrUpdateImpactData(dataPath, "ImpactData_Flesh", SurfaceType.Flesh,
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/WFX_BImpact SoftBody.prefab",
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/+ Bullet Hole/WFX_BImpact SoftBody + Hole.prefab");
        
        // Grass (Sand 이펙트 활용)
        CreateOrUpdateImpactData(dataPath, "ImpactData_Grass", SurfaceType.Grass,
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/WFX_BImpact Sand.prefab",
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/+ Bullet Hole/WFX_BImpact Sand + Hole.prefab");
        
        // Glass
        CreateOrUpdateImpactData(dataPath, "ImpactData_Glass", SurfaceType.Glass,
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/WFX_BImpact Concrete.prefab",
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/+ Bullet Hole/Lit/WFX_BImpact Concrete + Hole Lit.prefab");
        
        // Water
        CreateOrUpdateImpactData(dataPath, "ImpactData_Water", SurfaceType.Water,
            "Assets/10.Assets/JMO Assets/WarFX/_Effects/Bullet Impacts/WFX_BImpact Sand.prefab",
            null);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[WarFXSetup] ImpactData 에셋 생성/업데이트 완료");
    }
    
    private static void CreateOrUpdateImpactData(string folderPath, string assetName, 
        SurfaceType surfaceType, string particlePath, string decalPath)
    {
        string assetPath = $"{folderPath}/{assetName}.asset";
        
        ImpactData data = AssetDatabase.LoadAssetAtPath<ImpactData>(assetPath);
        
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ImpactData>();
            AssetDatabase.CreateAsset(data, assetPath);
        }
        
        data.surfaceType = surfaceType;
        data.particlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(particlePath);
        
        if (!string.IsNullOrEmpty(decalPath))
        {
            data.decalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(decalPath);
        }
        
        data.particleLifetime = 1.5f;
        data.decalLifetime = 15f;
        data.decalSize = 0.08f;
        
        EditorUtility.SetDirty(data);
    }
    
    private static void SetPrefabReference(SerializedObject so, string propertyName, string assetPath)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                prop.objectReferenceValue = prefab;
            }
            else
            {
                Debug.LogWarning($"[WarFXSetup] 프리팹을 찾을 수 없음: {assetPath}");
            }
        }
    }
    
    private static void SetArrayElementPrefab(SerializedProperty arrayProp, int index, string assetPath)
    {
        if (index < arrayProp.arraySize)
        {
            SerializedProperty element = arrayProp.GetArrayElementAtIndex(index);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                element.objectReferenceValue = prefab;
            }
        }
    }
    
    [MenuItem("Tools/FPS Setup/Link Impact Data to Manager")]
    public static void LinkImpactDataToManager()
    {
        ImpactEffectManager manager = Object.FindFirstObjectByType<ImpactEffectManager>();
        
        if (manager == null)
        {
            EditorUtility.DisplayDialog("오류", "씬에 ImpactEffectManager가 없습니다.", "확인");
            return;
        }
        
        string dataPath = "Assets/02.Scripts/Weapons/Data/ImpactEffects";
        string[] guids = AssetDatabase.FindAssets("t:ImpactData", new[] { dataPath });
        
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty listProp = so.FindProperty("_impactDataList");
        
        if (listProp != null)
        {
            listProp.arraySize = guids.Length;
            
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ImpactData data = AssetDatabase.LoadAssetAtPath<ImpactData>(path);
                
                if (data != null)
                {
                    listProp.GetArrayElementAtIndex(i).objectReferenceValue = data;
                }
            }
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(manager);
            
            EditorUtility.DisplayDialog("완료", $"{guids.Length}개의 ImpactData가 연결되었습니다.", "확인");
        }
    }
}
