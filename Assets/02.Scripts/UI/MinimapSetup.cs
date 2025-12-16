using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 미니맵 UI 자동 설정 컴포넌트
/// 에디터에서 실행하여 미니맵 UI 구성 완료
/// </summary>
[ExecuteInEditMode]
public class MinimapSetup : MonoBehaviour
{
    [Header("Minimap Settings")]
    [SerializeField] private float _minimapSize = 200f;
    [SerializeField] private Vector2 _minimapOffset = new Vector2(-20f, -20f);

    [Header("Icon Sprites")]
    [SerializeField] private Sprite _maskSprite;
    [SerializeField] private Sprite _borderSprite;
    [SerializeField] private Sprite _playerIconSprite;
    [SerializeField] private Sprite _enemyIconSprite;

    [Header("References (Auto-assigned)")]
    [SerializeField] private RectTransform _minimapPanel;
    [SerializeField] private RawImage _minimapDisplay;
    [SerializeField] private Image _minimapMask;
    [SerializeField] private Image _minimapBorder;
    [SerializeField] private RectTransform _playerIcon;
    [SerializeField] private Camera _minimapCamera;

#if UNITY_EDITOR
    [ContextMenu("Setup Minimap")]
    public void SetupMinimap()
    {
        FindReferences();
        ConfigureMinimapPanel();
        ConfigureMinimapDisplay();
        ConfigureBorder();
        ConfigurePlayerIcon();
        ConfigureMinimapCamera();
        ConnectMinimapSystem();

        Debug.Log("Minimap setup completed!");
        EditorUtility.SetDirty(gameObject);
    }

    private void FindReferences()
    {
        // MinimapPanel 찾기
        Transform panel = transform.Find("Canvas/MinimapPanel");
        if (panel == null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                panel = canvas.transform.Find("MinimapPanel");
            }
        }

        if (panel != null)
        {
            _minimapPanel = panel.GetComponent<RectTransform>();
            _minimapMask = panel.GetComponent<Image>();

            Transform display = panel.Find("MinimapDisplay");
            if (display != null)
            {
                _minimapDisplay = display.GetComponent<RawImage>();
            }

            Transform border = panel.Find("MinimapBorder");
            if (border != null)
            {
                _minimapBorder = border.GetComponent<Image>();
            }

            Transform playerIcon = panel.Find("PlayerIcon");
            if (playerIcon != null)
            {
                _playerIcon = playerIcon.GetComponent<RectTransform>();
            }
        }

        // MinimapCamera 찾기
        Transform camTransform = transform.Find("MinimapCamera");
        if (camTransform != null)
        {
            _minimapCamera = camTransform.GetComponent<Camera>();
        }

        // 스프라이트 로드
        LoadSprites();
    }

    private void LoadSprites()
    {
        string resourcePath = "Assets/10.Assets/Minimap_Resource/";

        _maskSprite = AssetDatabase.LoadAssetAtPath<Sprite>(resourcePath + "MinimapMask02.png");
        _borderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(resourcePath + "MinimapRound02.png");
        _playerIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(resourcePath + "MinimapIcon_Player.png");
        _enemyIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(resourcePath + "MinimapIcon_Enemy01.png");
    }

    private void ConfigureMinimapPanel()
    {
        if (_minimapPanel == null)
        {
            return;
        }

        // 오른쪽 상단 앵커
        _minimapPanel.anchorMin = new Vector2(1f, 1f);
        _minimapPanel.anchorMax = new Vector2(1f, 1f);
        _minimapPanel.pivot = new Vector2(1f, 1f);
        _minimapPanel.anchoredPosition = _minimapOffset;
        _minimapPanel.sizeDelta = new Vector2(_minimapSize, _minimapSize);

        // 마스크 이미지 설정
        if (_minimapMask != null && _maskSprite != null)
        {
            _minimapMask.sprite = _maskSprite;
            _minimapMask.color = Color.white;
            _minimapMask.raycastTarget = false;
        }
    }

    private void ConfigureMinimapDisplay()
    {
        if (_minimapDisplay == null)
        {
            return;
        }

        RectTransform rect = _minimapDisplay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        _minimapDisplay.raycastTarget = false;
    }

    private void ConfigureBorder()
    {
        if (_minimapBorder == null)
        {
            return;
        }

        RectTransform rect = _minimapBorder.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-5f, -5f);
        rect.offsetMax = new Vector2(5f, 5f);

        if (_borderSprite != null)
        {
            _minimapBorder.sprite = _borderSprite;
            _minimapBorder.color = new Color(1f, 1f, 1f, 0.9f);
        }
        _minimapBorder.raycastTarget = false;
    }

    private void ConfigurePlayerIcon()
    {
        if (_playerIcon == null)
        {
            return;
        }

        _playerIcon.anchorMin = new Vector2(0.5f, 0.5f);
        _playerIcon.anchorMax = new Vector2(0.5f, 0.5f);
        _playerIcon.pivot = new Vector2(0.5f, 0.5f);
        _playerIcon.anchoredPosition = Vector2.zero;
        _playerIcon.sizeDelta = new Vector2(30f, 30f);

        Image iconImage = _playerIcon.GetComponent<Image>();
        if (iconImage != null && _playerIconSprite != null)
        {
            iconImage.sprite = _playerIconSprite;
            iconImage.color = Color.cyan;
            iconImage.raycastTarget = false;
        }
    }

    private void ConfigureMinimapCamera()
    {
        if (_minimapCamera == null)
        {
            return;
        }

        _minimapCamera.orthographic = true;
        _minimapCamera.orthographicSize = 20f;
        _minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        _minimapCamera.backgroundColor = new Color(0.1f, 0.12f, 0.18f, 1f);
        _minimapCamera.depth = -2;
        _minimapCamera.cullingMask = LayerMask.GetMask("Default") | LayerMask.GetMask("Ground");

        // 위치 설정 (플레이어 위 50m)
        _minimapCamera.transform.localPosition = new Vector3(0f, 50f, 0f);
        _minimapCamera.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void ConnectMinimapSystem()
    {
        MinimapSystem minimapSystem = GetComponent<MinimapSystem>();
        MinimapIconManager iconManager = GetComponent<MinimapIconManager>();

        if (minimapSystem == null || iconManager == null)
        {
            return;
        }

        // SerializedObject를 통해 private 필드 설정
        SerializedObject systemSO = new SerializedObject(minimapSystem);
        systemSO.FindProperty("_minimapCamera").objectReferenceValue = _minimapCamera;
        systemSO.FindProperty("_minimapDisplay").objectReferenceValue = _minimapDisplay;
        systemSO.FindProperty("_minimapMask").objectReferenceValue = _minimapMask;
        systemSO.FindProperty("_minimapBorder").objectReferenceValue = _minimapBorder;
        systemSO.FindProperty("_playerIcon").objectReferenceValue = _playerIcon;
        systemSO.ApplyModifiedProperties();

        SerializedObject managerSO = new SerializedObject(iconManager);
        managerSO.FindProperty("_minimapContainer").objectReferenceValue = _minimapPanel;
        managerSO.FindProperty("_minimapCamera").objectReferenceValue = _minimapCamera;
        managerSO.FindProperty("_playerIconSprite").objectReferenceValue = _playerIconSprite;
        managerSO.FindProperty("_enemyIconSprite").objectReferenceValue = _enemyIconSprite;
        managerSO.ApplyModifiedProperties();

        Debug.Log("MinimapSystem and MinimapIconManager connected!");
    }

    [ContextMenu("Create RenderTexture Asset")]
    public void CreateRenderTextureAsset()
    {
        RenderTexture rt = new RenderTexture(512, 512, 16);
        rt.name = "MinimapRenderTexture";

        string path = "Assets/10.Assets/Minimap_Resource/MinimapRenderTexture.renderTexture";
        AssetDatabase.CreateAsset(rt, path);
        AssetDatabase.SaveAssets();

        if (_minimapCamera != null)
        {
            _minimapCamera.targetTexture = rt;
        }

        if (_minimapDisplay != null)
        {
            _minimapDisplay.texture = rt;
        }

        Debug.Log($"RenderTexture created at: {path}");
    }
#endif
}
