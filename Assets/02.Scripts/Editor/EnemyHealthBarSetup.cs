using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class EnemyHealthBarSetup : Editor
{
    [MenuItem("Tools/Setup EnemyHealthBar Prefab References")]
    public static void SetupPrefabReferences()
    {
        string prefabPath = "Assets/03.Prefabs/UI/EnemyHealthBar_UI.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at: {prefabPath}");
            return;
        }
        
        // Get or add EnemyHealthBar component
        EnemyHealthBar healthBar = prefab.GetComponent<EnemyHealthBar>();
        if (healthBar == null)
        {
            healthBar = prefab.AddComponent<EnemyHealthBar>();
            Debug.Log("Added EnemyHealthBar component to prefab");
        }
        
        // Use SerializedObject for proper prefab editing
        SerializedObject so = new SerializedObject(healthBar);
        
        // Find and assign UI elements
        Transform root = prefab.transform;
        
        // Fill Image (main health bar)
        Transform fillTransform = root.Find("Fill");
        if (fillTransform != null)
        {
            Image fillImage = fillTransform.GetComponent<Image>();
            if (fillImage != null)
            {
                so.FindProperty("_fillImage").objectReferenceValue = fillImage;
                Debug.Log("Assigned Fill Image");
            }
        }
        
        // Trail Image (damage delay)
        Transform trailTransform = root.Find("DamageDelay") ?? root.Find("Trail");
        if (trailTransform != null)
        {
            Image trailImage = trailTransform.GetComponent<Image>();
            if (trailImage != null)
            {
                so.FindProperty("_trailImage").objectReferenceValue = trailImage;
                Debug.Log("Assigned Trail Image");
            }
        }
        
        // Background Image
        Transform bgTransform = root.Find("Background");
        if (bgTransform != null)
        {
            Image bgImage = bgTransform.GetComponent<Image>();
            if (bgImage != null)
            {
                so.FindProperty("_backgroundImage").objectReferenceValue = bgImage;
                Debug.Log("Assigned Background Image");
            }
        }
        
        // Flash Overlay
        Transform flashTransform = root.Find("FlashOverlay") ?? root.Find("Flash");
        if (flashTransform != null)
        {
            Image flashImage = flashTransform.GetComponent<Image>();
            if (flashImage != null)
            {
                so.FindProperty("_flashOverlay").objectReferenceValue = flashImage;
                Debug.Log("Assigned Flash Overlay");
            }
        }
        
        // Canvas Group (on root or parent)
        CanvasGroup canvasGroup = prefab.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = prefab.AddComponent<CanvasGroup>();
            Debug.Log("Added CanvasGroup to prefab");
        }
        so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
        
        // Shake Target (RectTransform of root)
        RectTransform shakeTarget = prefab.GetComponent<RectTransform>();
        if (shakeTarget != null)
        {
            so.FindProperty("_shakeTarget").objectReferenceValue = shakeTarget;
            Debug.Log("Assigned Shake Target");
        }
        
        // Apply changes
        so.ApplyModifiedProperties();
        
        // Mark prefab as dirty and save
        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
        
        Debug.Log("EnemyHealthBar prefab setup complete!");
    }
    
    [MenuItem("Tools/Show EnemyHealthBar Prefab Structure")]
    public static void ShowPrefabStructure()
    {
        string prefabPath = "Assets/03.Prefabs/UI/EnemyHealthBar_UI.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at: {prefabPath}");
            return;
        }
        
        Debug.Log($"=== Prefab Structure: {prefab.name} ===");
        PrintHierarchy(prefab.transform, 0);
        
        // Check for EnemyHealthBar component
        EnemyHealthBar healthBar = prefab.GetComponent<EnemyHealthBar>();
        if (healthBar != null)
        {
            Debug.Log("EnemyHealthBar component found!");
        }
        else
        {
            Debug.LogWarning("EnemyHealthBar component NOT found on prefab root!");
        }
    }
    
    private static void PrintHierarchy(Transform t, int depth)
    {
        string indent = new string(' ', depth * 2);
        string components = "";
        
        foreach (var comp in t.GetComponents<Component>())
        {
            if (comp != null && !(comp is Transform))
            {
                components += $"[{comp.GetType().Name}] ";
            }
        }
        
        Debug.Log($"{indent}- {t.name} {components}");
        
        foreach (Transform child in t)
        {
            PrintHierarchy(child, depth + 1);
        }
    }
}
