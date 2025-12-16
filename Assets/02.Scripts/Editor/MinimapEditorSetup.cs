using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

public static class MinimapEditorSetup
{
    [MenuItem("Tools/FPS Game/Setup Minimap System")]
    public static void SetupMinimapSystem()
    {
        // MinimapSystem 찾기
        MinimapSystem minimapSystem = Object.FindFirstObjectByType<MinimapSystem>();
        if (minimapSystem == null)
        {
            Debug.LogError("MinimapSystem not found in scene!");
            return;
        }

        GameObject systemObj = minimapSystem.gameObject;
        
        // MinimapCamera 찾기/설정
        Camera minimapCamera = systemObj.GetComponentInChildren<Camera>();
        if (minimapCamera != null)
        {
            ConfigureCamera(minimapCamera);
        }

        // Canvas에서 MinimapPanel 찾기
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found!");
            return;
        }

        Transform minimapPanel = canvas.transform.Find("MinimapPanel");
        if (minimapPanel == null)
        {
            Debug.LogError("MinimapPanel not found in Canvas!");
            return;
        }

        // 스프라이트 로드
        string resourcePath = "Assets/10.Assets/Minimap_Resource/";
        Sprite maskSprite = AssetDatabase.LoadAssetAtPath<Sprite>(resourcePath + "MinimapMask02.png");
        Sprite borderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(resourcePath + "MinimapRound02.png");
        Sprite playerIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(resourcePath + "MinimapIcon_Player.png");
        Sprite enemyIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(resourcePath + "MinimapIcon_Enemy01.png");

        // MinimapPanel 설정
        RectTransform panelRect = minimapPanel.GetComponent<RectTransform>();
        ConfigureMinimapPanel(panelRect, maskSprite);

        // MinimapDisplay 설정
        Transform displayTransform = minimapPanel.Find("MinimapDisplay");
        RawImage minimapDisplay = null;
        if (displayTransform != null)
        {
            minimapDisplay = displayTransform.GetComponent<RawImage>();
            ConfigureDisplay(displayTransform.GetComponent<RectTransform>());
        }

        // MinimapBorder 설정
        Transform borderTransform = minimapPanel.Find("MinimapBorder");
        Image minimapBorder = null;
        if (borderTransform != null)
        {
            minimapBorder = borderTransform.GetComponent<Image>();
            ConfigureBorder(borderTransform.GetComponent<RectTransform>(), minimapBorder, borderSprite);
        }

        // PlayerIcon 설정
        Transform playerIconTransform = minimapPanel.Find("PlayerIcon");
        RectTransform playerIconRect = null;
        if (playerIconTransform != null)
        {
            playerIconRect = playerIconTransform.GetComponent<RectTransform>();
            Image playerIconImage = playerIconTransform.GetComponent<Image>();
            ConfigurePlayerIcon(playerIconRect, playerIconImage, playerIconSprite);
        }

        // RenderTexture 생성 및 연결
        RenderTexture rt = CreateRenderTexture();
        if (minimapCamera != null)
        {
            minimapCamera.targetTexture = rt;
        }
        if (minimapDisplay != null)
        {
            minimapDisplay.texture = rt;
        }

        // MinimapSystem 컴포넌트 연결
        ConnectMinimapSystem(minimapSystem, minimapCamera, minimapDisplay, 
            minimapPanel.GetComponent<Image>(), minimapBorder, playerIconRect);

        // MinimapIconManager 연결
        MinimapIconManager iconManager = systemObj.GetComponent<MinimapIconManager>();
        if (iconManager != null)
        {
            ConnectIconManager(iconManager, panelRect, minimapCamera, playerIconSprite, enemyIconSprite);
        }

        // 씬 저장 표시
        EditorUtility.SetDirty(systemObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("✅ Minimap System setup completed!");
    }

    private static void ConfigureCamera(Camera cam)
    {
        cam.orthographic = true;
        cam.orthographicSize = 20f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.12f, 0.18f, 1f);
        cam.depth = -2;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 100f;
        
        cam.transform.localPosition = new Vector3(0f, 50f, 0f);
        cam.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        Debug.Log("Camera configured");
    }

    private static void ConfigureMinimapPanel(RectTransform rect, Sprite maskSprite)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -20f);
        rect.sizeDelta = new Vector2(200f, 200f);

        Image maskImage = rect.GetComponent<Image>();
        if (maskImage != null && maskSprite != null)
        {
            maskImage.sprite = maskSprite;
            maskImage.color = Color.white;
            maskImage.raycastTarget = false;
        }

        Debug.Log("MinimapPanel configured");
    }

    private static void ConfigureDisplay(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RawImage rawImage = rect.GetComponent<RawImage>();
        if (rawImage != null)
        {
            rawImage.raycastTarget = false;
        }

        Debug.Log("MinimapDisplay configured");
    }

    private static void ConfigureBorder(RectTransform rect, Image borderImage, Sprite borderSprite)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-5f, -5f);
        rect.offsetMax = new Vector2(5f, 5f);

        if (borderImage != null && borderSprite != null)
        {
            borderImage.sprite = borderSprite;
            borderImage.color = new Color(1f, 1f, 1f, 0.9f);
            borderImage.raycastTarget = false;
        }

        Debug.Log("MinimapBorder configured");
    }

    private static void ConfigurePlayerIcon(RectTransform rect, Image iconImage, Sprite iconSprite)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(30f, 30f);

        if (iconImage != null && iconSprite != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.color = Color.cyan;
            iconImage.raycastTarget = false;
        }

        Debug.Log("PlayerIcon configured");
    }

    private static RenderTexture CreateRenderTexture()
    {
        string rtPath = "Assets/10.Assets/Minimap_Resource/MinimapRenderTexture.renderTexture";
        RenderTexture existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        
        if (existing != null)
        {
            Debug.Log("Using existing RenderTexture");
            return existing;
        }

        RenderTexture rt = new RenderTexture(512, 512, 16);
        rt.name = "MinimapRenderTexture";
        rt.antiAliasing = 2;
        
        AssetDatabase.CreateAsset(rt, rtPath);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"RenderTexture created: {rtPath}");
        return rt;
    }

    private static void ConnectMinimapSystem(MinimapSystem system, Camera cam, 
        RawImage display, Image mask, Image border, RectTransform playerIcon)
    {
        // Player 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        SerializedObject so = new SerializedObject(system);
        so.FindProperty("_minimapCamera").objectReferenceValue = cam;
        if (player != null)
        {
            so.FindProperty("_playerTransform").objectReferenceValue = player.transform;
        }
        so.FindProperty("_minimapDisplay").objectReferenceValue = display;
        so.FindProperty("_minimapMask").objectReferenceValue = mask;
        so.FindProperty("_minimapBorder").objectReferenceValue = border;
        so.FindProperty("_playerIcon").objectReferenceValue = playerIcon;
        so.ApplyModifiedProperties();

        Debug.Log("MinimapSystem references connected");
    }

    private static void ConnectIconManager(MinimapIconManager manager, RectTransform container,
        Camera cam, Sprite playerSprite, Sprite enemySprite)
    {
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("_minimapContainer").objectReferenceValue = container;
        so.FindProperty("_minimapCamera").objectReferenceValue = cam;
        so.FindProperty("_playerIconSprite").objectReferenceValue = playerSprite;
        so.FindProperty("_enemyIconSprite").objectReferenceValue = enemySprite;
        so.ApplyModifiedProperties();

        Debug.Log("MinimapIconManager references connected");
    }
}
#endif
