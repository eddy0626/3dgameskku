using UnityEngine;
using UnityEditor;

/// <summary>
/// 적 시스템 설정 유틸리티
/// Missing Script 제거 및 EnemyData 생성
/// </summary>
public static class EnemySetupUtility
{
    [MenuItem("Tools/Enemy Setup/1. Create Basic EnemyData", priority = 100)]
    public static void CreateBasicEnemyData()
    {
        // 폴더 확인/생성
        if (!AssetDatabase.IsValidFolder("Assets/03.Prefabs/Enemy"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/03.Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "03.Prefabs");
            }
            AssetDatabase.CreateFolder("Assets/03.Prefabs", "Enemy");
        }
        
        string path = "Assets/03.Prefabs/Enemy/EnemyData_Basic.asset";
        
        // 이미 존재하면 로드
        var existingData = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (existingData != null)
        {
            Debug.Log($"[EnemySetup] EnemyData already exists: {path}");
            Selection.activeObject = existingData;
            return;
        }
        
        // 새로 생성
        var enemyData = ScriptableObject.CreateInstance<EnemyData>();
        
        // 현재 Enemy_Basic 설정값 기반으로 데이터 설정
        enemyData.enemyName = "Basic Enemy";
        enemyData.enemyType = EnemyType.Normal;
        enemyData.maxHealth = 1000f;
        enemyData.armor = 0f;
        enemyData.headshotMultiplier = 2f;
        
        // 이동 설정
        enemyData.patrolSpeed = 2f;
        enemyData.chaseSpeed = 5f;
        enemyData.rotationSpeed = 10f;
        enemyData.patrolWaitTime = 2f;
        
        // 감지 설정
        enemyData.detectionRange = 12.6f;
        enemyData.fieldOfView = 118f;
        enemyData.detectionHeight = 2f;
        enemyData.hearingRange = 10f;
        
        // 추적 설정
        enemyData.chaseRange = 17.2f;
        enemyData.loseTargetTime = 5f;
        enemyData.maxChaseDistance = 20.7f;
        enemyData.returnToSpawn = true;
        
        // 공격 설정 (Ranged)
        enemyData.attackType = AttackType.Ranged;
        enemyData.attackRange = 10f;
        enemyData.attackCooldown = 1.5f;
        enemyData.rangedDamage = 1f;
        enemyData.rangedRange = 20f;
        enemyData.projectileSpeed = 80.5f;
        enemyData.aimAccuracy = 0.9f;
        
        // 보상 설정
        enemyData.experienceReward = 10;
        enemyData.scoreReward = 100;
        
        AssetDatabase.CreateAsset(enemyData, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[EnemySetup] Created EnemyData: {path}");
        Selection.activeObject = enemyData;
        EditorGUIUtility.PingObject(enemyData);
    }
    
    [MenuItem("Tools/Enemy Setup/2. Remove Missing Scripts (Scene)", priority = 101)]
    public static void RemoveMissingScriptsInScene()
    {
        int totalRemoved = 0;
        
        // 씬의 모든 게임오브젝트 검사
        var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (var go in allObjects)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0)
            {
                totalRemoved += removed;
                Debug.Log($"[EnemySetup] Removed {removed} missing script(s) from: {go.name}");
                EditorUtility.SetDirty(go);
            }
        }
        
        if (totalRemoved > 0)
        {
            Debug.Log($"[EnemySetup] Total removed: {totalRemoved} missing script component(s)");
        }
        else
        {
            Debug.Log("[EnemySetup] No missing scripts found in scene.");
        }
    }
    
    [MenuItem("Tools/Enemy Setup/3. Assign EnemyData to All Enemies", priority = 102)]
    public static void AssignEnemyDataToAllEnemies()
    {
        string dataPath = "Assets/03.Prefabs/Enemy/EnemyData_Basic.asset";
        var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
        
        if (enemyData == null)
        {
            Debug.LogError($"[EnemySetup] EnemyData not found: {dataPath}. Run 'Create Basic EnemyData' first.");
            return;
        }
        
        int assignedCount = 0;
        var allEnemyBases = GameObject.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (var enemyBase in allEnemyBases)
        {
            // SerializedObject를 통해 private 필드에 접근
            var serializedObj = new SerializedObject(enemyBase);
            var dataProperty = serializedObj.FindProperty("_enemyData");
            
            if (dataProperty != null && dataProperty.objectReferenceValue == null)
            {
                dataProperty.objectReferenceValue = enemyData;
                serializedObj.ApplyModifiedProperties();
                EditorUtility.SetDirty(enemyBase);
                assignedCount++;
                Debug.Log($"[EnemySetup] Assigned EnemyData to: {enemyBase.gameObject.name}");
            }
        }
        
        if (assignedCount > 0)
        {
            Debug.Log($"[EnemySetup] Assigned EnemyData to {assignedCount} enemy(s)");
        }
        else
        {
            Debug.Log("[EnemySetup] No enemies needed EnemyData assignment.");
        }
    }
    
    [MenuItem("Tools/Enemy Setup/Run All Setup Steps", priority = 200)]
    public static void RunAllSetupSteps()
    {
        Debug.Log("=== Starting Enemy Setup ===");
        
        // Step 1: Missing Scripts 제거
        RemoveMissingScriptsInScene();
        
        // Step 2: EnemyData 생성
        CreateBasicEnemyData();
        
        // Step 3: EnemyData 할당
        AssignEnemyDataToAllEnemies();
        
        Debug.Log("=== Enemy Setup Complete ===");
    }
}
