using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// UI 문제 수정 에디터 도구
/// - 흰색 이미지 제거/수정
/// - 배경 투명화
/// - 미니맵 설정
/// </summary>
public class UIFixer : EditorWindow
{
    [MenuItem("Tools/UI Fixer/Fix All UI Issues")]
    public static void FixAllUIIssues()
    {
        FixCrosshair();
        FixCoinUI();
        FixWaveUI();
        FixPlayingPanel();
        Debug.Log("[UIFixer] 모든 UI 문제 수정 완료!");
    }

    [MenuItem("Tools/UI Fixer/Fix Crosshair (Remove White Image)")]
    public static void FixCrosshair()
    {
        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null)
        {
            Debug.LogWarning("[UIFixer] GameCanvas를 찾을 수 없습니다.");
            return;
        }

        // CrosshairUI 찾기
        var crosshairUI = canvas.transform.Find("PlayingPanel/CrosshairUI");
        if (crosshairUI == null)
        {
            crosshairUI = canvas.transform.Find("PlayingPanel/Crosshair");
        }

        if (crosshairUI != null)
        {
            var image = crosshairUI.GetComponent<Image>();
            if (image != null)
            {
                // 스프라이트가 없으면 이미지 비활성화
                if (image.sprite == null)
                {
                    image.enabled = false;
                    Debug.Log("[UIFixer] Crosshair 이미지 비활성화 (스프라이트 없음)");
                }
                else
                {
                    // 스프라이트가 있으면 raycast 비활성화
                    image.raycastTarget = false;
                }
            }

            // 자식 이미지들도 확인
            var childImages = crosshairUI.GetComponentsInChildren<Image>();
            foreach (var childImage in childImages)
            {
                if (childImage.sprite == null)
                {
                    childImage.enabled = false;
                }
                childImage.raycastTarget = false;
            }

            EditorUtility.SetDirty(crosshairUI.gameObject);
            Debug.Log("[UIFixer] Crosshair 수정 완료");
        }
        else
        {
            Debug.LogWarning("[UIFixer] CrosshairUI를 찾을 수 없습니다.");
        }
    }

    [MenuItem("Tools/UI Fixer/Fix Coin UI Background")]
    public static void FixCoinUI()
    {
        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null)
        {
            Debug.LogWarning("[UIFixer] GameCanvas를 찾을 수 없습니다.");
            return;
        }

        var coinUI = canvas.transform.Find("PlayingPanel/CoinUI");
        if (coinUI != null)
        {
            var image = coinUI.GetComponent<Image>();
            if (image != null)
            {
                // 배경을 투명하게 또는 반투명 검정으로
                image.color = new Color(0f, 0f, 0f, 0.5f);
                Debug.Log("[UIFixer] CoinUI 배경 색상 변경 (반투명 검정)");
            }

            // CoinIcon이 흰색이면 노란색으로
            var coinIcon = coinUI.Find("CoinIcon");
            if (coinIcon != null)
            {
                var iconImage = coinIcon.GetComponent<Image>();
                if (iconImage != null && iconImage.sprite == null)
                {
                    iconImage.color = new Color(1f, 0.84f, 0f); // Gold color
                }
            }

            EditorUtility.SetDirty(coinUI.gameObject);
            Debug.Log("[UIFixer] CoinUI 수정 완료");
        }
    }

    [MenuItem("Tools/UI Fixer/Fix Wave UI Background")]
    public static void FixWaveUI()
    {
        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null)
        {
            Debug.LogWarning("[UIFixer] GameCanvas를 찾을 수 없습니다.");
            return;
        }

        var waveUI = canvas.transform.Find("PlayingPanel/WaveUI");
        if (waveUI != null)
        {
            // WaveUI 자체의 Image 컴포넌트 확인
            var image = waveUI.GetComponent<Image>();
            if (image != null)
            {
                // 스프라이트가 없으면 비활성화 또는 투명화
                if (image.sprite == null)
                {
                    image.enabled = false;
                    Debug.Log("[UIFixer] WaveUI 배경 이미지 비활성화");
                }
            }

            // 자식 중 흰색 배경 찾기
            var allImages = waveUI.GetComponentsInChildren<Image>(true);
            foreach (var img in allImages)
            {
                // 스프라이트 없고 흰색인 경우
                if (img.sprite == null && IsWhiteColor(img.color))
                {
                    string name = img.gameObject.name.ToLower();

                    // 배경 관련이면 투명화 또는 비활성화
                    if (name.Contains("background") || name.Contains("bg") || name.Contains("panel"))
                    {
                        img.color = new Color(0f, 0f, 0f, 0.3f);
                    }
                    else if (img.gameObject == waveUI.gameObject)
                    {
                        // WaveUI 루트 오브젝트의 이미지는 비활성화
                        img.enabled = false;
                    }
                    else
                    {
                        // 기타 이미지는 투명화
                        img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
                    }
                }
            }

            EditorUtility.SetDirty(waveUI.gameObject);
            Debug.Log("[UIFixer] WaveUI 수정 완료");
        }
        else
        {
            Debug.LogWarning("[UIFixer] WaveUI를 찾을 수 없습니다.");
        }
    }

    private static bool IsWhiteColor(Color color)
    {
        return color.r > 0.9f && color.g > 0.9f && color.b > 0.9f && color.a > 0.5f;
    }

    [MenuItem("Tools/UI Fixer/Fix Playing Panel")]
    public static void FixPlayingPanel()
    {
        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null)
        {
            Debug.LogWarning("[UIFixer] GameCanvas를 찾을 수 없습니다.");
            return;
        }

        var playingPanel = canvas.transform.Find("PlayingPanel");
        if (playingPanel != null)
        {
            // PlayingPanel 자체의 Image 제거 또는 투명화
            var panelImage = playingPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.enabled = false;
            }

            // 모든 하위 요소 중 스프라이트 없는 흰색 이미지 찾기
            var allImages = playingPanel.GetComponentsInChildren<Image>(true);
            int fixedCount = 0;

            foreach (var img in allImages)
            {
                // 스프라이트가 없고 흰색인 경우
                if (img.sprite == null && img.color == Color.white)
                {
                    // 배경용인지 확인 (이름에 Background, BG, Panel 포함)
                    string name = img.gameObject.name.ToLower();
                    if (name.Contains("background") || name.Contains("bg") || name.Contains("panel"))
                    {
                        img.color = new Color(0f, 0f, 0f, 0.5f);
                        fixedCount++;
                    }
                    else if (name.Contains("crosshair") || name.Contains("icon"))
                    {
                        // 크로스헤어나 아이콘은 비활성화
                        img.enabled = false;
                        fixedCount++;
                    }
                }
            }

            EditorUtility.SetDirty(playingPanel.gameObject);
            Debug.Log($"[UIFixer] PlayingPanel 수정 완료 ({fixedCount}개 이미지 수정)");
        }
    }

    [MenuItem("Tools/UI Fixer/Create Simple Crosshair")]
    public static void CreateSimpleCrosshair()
    {
        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null)
        {
            Debug.LogWarning("[UIFixer] GameCanvas를 찾을 수 없습니다.");
            return;
        }

        var playingPanel = canvas.transform.Find("PlayingPanel");
        if (playingPanel == null)
        {
            Debug.LogWarning("[UIFixer] PlayingPanel을 찾을 수 없습니다.");
            return;
        }

        // 기존 CrosshairUI 삭제
        var existingCrosshair = playingPanel.Find("CrosshairUI");
        if (existingCrosshair != null)
        {
            DestroyImmediate(existingCrosshair.gameObject);
        }

        // 새 크로스헤어 생성
        var crosshairObj = new GameObject("CrosshairUI");
        crosshairObj.transform.SetParent(playingPanel, false);

        var rect = crosshairObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(20, 20);

        var canvasGroup = crosshairObj.AddComponent<CanvasGroup>();
        var controller = crosshairObj.AddComponent<CrosshairController>();

        // 십자 모양 만들기 (4개의 작은 사각형)
        CreateCrosshairLine(crosshairObj.transform, "Top", new Vector2(0, 8), new Vector2(2, 6));
        CreateCrosshairLine(crosshairObj.transform, "Bottom", new Vector2(0, -8), new Vector2(2, 6));
        CreateCrosshairLine(crosshairObj.transform, "Left", new Vector2(-8, 0), new Vector2(6, 2));
        CreateCrosshairLine(crosshairObj.transform, "Right", new Vector2(8, 0), new Vector2(6, 2));

        EditorUtility.SetDirty(crosshairObj);
        Debug.Log("[UIFixer] 심플 크로스헤어 생성 완료");
    }

    private static void CreateCrosshairLine(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var lineObj = new GameObject(name);
        lineObj.transform.SetParent(parent, false);

        var rect = lineObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var image = lineObj.AddComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
    }

    [MenuItem("Tools/UI Fixer/Setup Minimap")]
    public static void SetupMinimap()
    {
        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null)
        {
            Debug.LogWarning("[UIFixer] GameCanvas를 찾을 수 없습니다.");
            return;
        }

        var playingPanel = canvas.transform.Find("PlayingPanel");
        if (playingPanel == null)
        {
            Debug.LogWarning("[UIFixer] PlayingPanel을 찾을 수 없습니다.");
            return;
        }

        // 기존 미니맵 확인
        var existingMinimap = playingPanel.Find("Minimap");
        if (existingMinimap != null)
        {
            Debug.Log("[UIFixer] Minimap이 이미 존재합니다.");
            return;
        }

        // 미니맵 컨테이너 생성
        var minimapObj = new GameObject("Minimap");
        minimapObj.transform.SetParent(playingPanel, false);

        var rect = minimapObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -20);
        rect.sizeDelta = new Vector2(200, 200);

        // 배경
        var bgObj = new GameObject("MinimapBackground");
        bgObj.transform.SetParent(minimapObj.transform, false);

        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // 마스크
        var mask = bgObj.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // 미니맵 렌더 영역
        var mapRenderObj = new GameObject("MinimapRender");
        mapRenderObj.transform.SetParent(bgObj.transform, false);

        var mapRenderRect = mapRenderObj.AddComponent<RectTransform>();
        mapRenderRect.anchorMin = Vector2.zero;
        mapRenderRect.anchorMax = Vector2.one;
        mapRenderRect.offsetMin = Vector2.zero;
        mapRenderRect.offsetMax = Vector2.zero;

        var mapRenderImage = mapRenderObj.AddComponent<RawImage>();
        mapRenderImage.color = new Color(0.2f, 0.3f, 0.2f, 1f);

        // 플레이어 아이콘
        var playerIconObj = new GameObject("PlayerIcon");
        playerIconObj.transform.SetParent(bgObj.transform, false);

        var playerRect = playerIconObj.AddComponent<RectTransform>();
        playerRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerRect.anchoredPosition = Vector2.zero;
        playerRect.sizeDelta = new Vector2(10, 10);

        var playerImage = playerIconObj.AddComponent<Image>();
        playerImage.color = Color.cyan;

        // 테두리
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(minimapObj.transform, false);

        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2, -2);
        borderRect.offsetMax = new Vector2(2, 2);

        var borderImage = borderObj.AddComponent<Image>();
        borderImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        borderImage.raycastTarget = false;
        borderObj.transform.SetAsFirstSibling();

        EditorUtility.SetDirty(minimapObj);
        Debug.Log("[UIFixer] 미니맵 UI 생성 완료 (MinimapCamera 별도 설정 필요)");
    }

    [MenuItem("Tools/UI Fixer/Setup Minimap Camera (Full Setup)")]
    public static void SetupMinimapCamera()
    {
        // 1. 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[UIFixer] Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
            return;
        }

        // 2. 기존 MinimapCamera 확인
        var existingCam = GameObject.Find("MinimapCamera");
        if (existingCam != null)
        {
            Debug.Log("[UIFixer] 기존 MinimapCamera 삭제");
            DestroyImmediate(existingCam);
        }

        // 3. MinimapCamera 오브젝트 생성
        var camObj = new GameObject("MinimapCamera");
        camObj.transform.position = player.transform.position + Vector3.up * 50f;
        camObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // 4. Camera 컴포넌트 설정
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.15f, 0.1f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = 30f;
        cam.nearClipPlane = 1f;
        cam.farClipPlane = 100f;
        cam.depth = -10;
        cam.cullingMask = ~(1 << LayerMask.NameToLayer("UI")); // UI 레이어 제외

        // 5. RenderTexture 생성
        var renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
        renderTexture.name = "MinimapRenderTexture";
        renderTexture.filterMode = FilterMode.Bilinear;
        renderTexture.Create();

        // RenderTexture 에셋으로 저장
        string rtPath = "Assets/04.Data/MinimapRenderTexture.renderTexture";
        EnsureDirectoryExists("Assets/04.Data");
        AssetDatabase.CreateAsset(renderTexture, rtPath);
        cam.targetTexture = renderTexture;

        // 6. MinimapController 추가
        var controller = camObj.AddComponent<MinimapController>();

        // 7. UI 설정
        SetupMinimapUI(renderTexture);

        EditorUtility.SetDirty(camObj);
        AssetDatabase.SaveAssets();

        Debug.Log("[UIFixer] 미니맵 카메라 전체 설정 완료!");
        Debug.Log("  - MinimapCamera 오브젝트 생성");
        Debug.Log("  - RenderTexture 생성 및 저장");
        Debug.Log("  - MinimapController 추가");
        Debug.Log("  - UI 연결 완료");

        Selection.activeGameObject = camObj;
    }

    private static void SetupMinimapUI(RenderTexture renderTexture)
    {
        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null)
        {
            Debug.LogWarning("[UIFixer] GameCanvas를 찾을 수 없습니다.");
            return;
        }

        var playingPanel = canvas.transform.Find("PlayingPanel");
        if (playingPanel == null)
        {
            Debug.LogWarning("[UIFixer] PlayingPanel을 찾을 수 없습니다.");
            return;
        }

        // 기존 미니맵 확인/삭제
        var existingMinimap = playingPanel.Find("Minimap");
        if (existingMinimap != null)
        {
            DestroyImmediate(existingMinimap.gameObject);
        }

        // 미니맵 컨테이너 생성
        var minimapObj = new GameObject("Minimap");
        minimapObj.transform.SetParent(playingPanel, false);

        var rect = minimapObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -20);
        rect.sizeDelta = new Vector2(180, 180);

        // 테두리 (먼저 생성해서 뒤에 배치)
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(minimapObj.transform, false);

        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-3, -3);
        borderRect.offsetMax = new Vector2(3, 3);

        var borderImage = borderObj.AddComponent<Image>();
        borderImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        borderImage.raycastTarget = false;

        // 배경 (마스크 역할)
        var bgObj = new GameObject("MinimapMask");
        bgObj.transform.SetParent(minimapObj.transform, false);

        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        // 원형 마스크를 위한 Mask 컴포넌트
        var mask = bgObj.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // 미니맵 렌더 이미지
        var renderObj = new GameObject("MinimapRender");
        renderObj.transform.SetParent(bgObj.transform, false);

        var renderRect = renderObj.AddComponent<RectTransform>();
        renderRect.anchorMin = Vector2.zero;
        renderRect.anchorMax = Vector2.one;
        renderRect.offsetMin = Vector2.zero;
        renderRect.offsetMax = Vector2.zero;

        var rawImage = renderObj.AddComponent<RawImage>();
        rawImage.texture = renderTexture;
        rawImage.raycastTarget = false;

        // 플레이어 아이콘 (중앙 고정)
        var playerIconObj = new GameObject("PlayerIcon");
        playerIconObj.transform.SetParent(bgObj.transform, false);

        var playerRect = playerIconObj.AddComponent<RectTransform>();
        playerRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerRect.anchoredPosition = Vector2.zero;
        playerRect.sizeDelta = new Vector2(12, 12);
        playerRect.localRotation = Quaternion.Euler(0, 0, 0);

        var playerImage = playerIconObj.AddComponent<Image>();
        playerImage.color = Color.cyan;
        playerImage.raycastTarget = false;

        // 방향 표시 (삼각형 형태)
        var directionObj = new GameObject("Direction");
        directionObj.transform.SetParent(playerIconObj.transform, false);

        var dirRect = directionObj.AddComponent<RectTransform>();
        dirRect.anchorMin = new Vector2(0.5f, 1f);
        dirRect.anchorMax = new Vector2(0.5f, 1f);
        dirRect.anchoredPosition = new Vector2(0, 5);
        dirRect.sizeDelta = new Vector2(8, 8);

        var dirImage = directionObj.AddComponent<Image>();
        dirImage.color = Color.cyan;
        dirImage.raycastTarget = false;

        // MinimapController에 참조 연결
        var minimapCamera = GameObject.Find("MinimapCamera");
        if (minimapCamera != null)
        {
            var controller = minimapCamera.GetComponent<MinimapController>();
            if (controller != null)
            {
                // SerializedObject를 통해 private 필드 설정
                var so = new SerializedObject(controller);
                so.FindProperty("_minimapImage").objectReferenceValue = rawImage;
                so.FindProperty("_playerIcon").objectReferenceValue = playerRect;
                so.FindProperty("_minimapMask").objectReferenceValue = bgRect;
                so.ApplyModifiedProperties();
            }
        }

        EditorUtility.SetDirty(minimapObj);
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] folders = path.Split('/');
            string currentPath = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }
    }
}
