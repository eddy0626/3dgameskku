using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 탑다운 미니맵 시스템 (원신 스타일)
/// 플레이어를 따라다니는 직교 카메라로 미니맵 렌더링
/// </summary>
public class MinimapSystem : MonoBehaviour
{
    public static MinimapSystem Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera _minimapCamera;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private RawImage _minimapDisplay;
    [SerializeField] private Image _minimapMask;
    [SerializeField] private Image _minimapBorder;
    [SerializeField] private RectTransform _playerIcon;

    [Header("Camera Settings")]
    [SerializeField] private float _cameraHeight = 50f;
    [SerializeField] private float _cameraSize = 20f;
    [SerializeField] private float _smoothSpeed = 10f;
    [SerializeField] private bool _rotateWithPlayer = true;

    [Header("Render Texture")]
    [SerializeField] private int _renderTextureSize = 512;
    
    private RenderTexture _minimapRenderTexture;

    [Header("Zoom Settings")]
    [SerializeField] private float _minZoom = 10f;
    [SerializeField] private float _maxZoom = 50f;
    [SerializeField] private float _zoomStep = 5f;
    [SerializeField] private float _scrollZoomSpeed = 10f;
    [SerializeField] private bool _enableScrollZoom = true;
    
    // Zoom 버튼 참조 (UI에서 연결)
    [Header("UI Buttons (Optional)")]
    [SerializeField] private UnityEngine.UI.Button _zoomInButton;
    [SerializeField] private UnityEngine.UI.Button _zoomOutButton;

    private Vector3 _cameraOffset;

private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        AutoFindReferences();
        InitializeMinimapCamera();
    }


private void AutoFindReferences()
    {
        Debug.Log("[MinimapSystem] AutoFindReferences started...");
        
        // 미니맵 카메라 찾기
        if (_minimapCamera == null)
        {
            _minimapCamera = GetComponentInChildren<Camera>();
            Debug.Log(_minimapCamera != null 
                ? $"[MinimapSystem] Found MinimapCamera: {_minimapCamera.name}" 
                : "[MinimapSystem] WARNING: MinimapCamera not found!");
        }

        // 플레이어 트랜스폼 찾기
        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                Debug.Log($"[MinimapSystem] Found Player: {player.name}");
            }
            else
            {
                Debug.LogWarning("[MinimapSystem] WARNING: Player not found! Make sure Player has 'Player' tag.");
            }
        }

        // UI 요소들 찾기
        if (_minimapDisplay == null)
        {
            GameObject displayObj = GameObject.Find("MinimapDisplay");
            if (displayObj != null)
            {
                _minimapDisplay = displayObj.GetComponent<UnityEngine.UI.RawImage>();
                Debug.Log(_minimapDisplay != null 
                    ? "[MinimapSystem] Found MinimapDisplay RawImage" 
                    : "[MinimapSystem] WARNING: MinimapDisplay has no RawImage!");
            }
            else
            {
                Debug.LogWarning("[MinimapSystem] WARNING: MinimapDisplay GameObject not found!");
            }
        }

        if (_minimapMask == null)
        {
            GameObject panelObj = GameObject.Find("MinimapPanel");
            if (panelObj != null)
            {
                _minimapMask = panelObj.GetComponent<UnityEngine.UI.Image>();
                Debug.Log(_minimapMask != null 
                    ? "[MinimapSystem] Found MinimapPanel Image (mask)" 
                    : "[MinimapSystem] MinimapPanel has no Image component");
            }
        }

        if (_minimapBorder == null)
        {
            GameObject borderObj = GameObject.Find("MinimapBorder");
            if (borderObj != null)
            {
                _minimapBorder = borderObj.GetComponent<UnityEngine.UI.Image>();
                Debug.Log(_minimapBorder != null 
                    ? "[MinimapSystem] Found MinimapBorder Image" 
                    : "[MinimapSystem] MinimapBorder has no Image component");
            }
        }

        if (_playerIcon == null)
        {
            GameObject iconObj = GameObject.Find("PlayerIcon");
            if (iconObj != null)
            {
                _playerIcon = iconObj.GetComponent<RectTransform>();
                Debug.Log(_playerIcon != null 
                    ? "[MinimapSystem] Found PlayerIcon RectTransform" 
                    : "[MinimapSystem] WARNING: PlayerIcon has no RectTransform!");
            }
            else
            {
                Debug.LogWarning("[MinimapSystem] WARNING: PlayerIcon GameObject not found!");
            }
        }
        
        Debug.Log("[MinimapSystem] AutoFindReferences completed.");
    }


private void Start()
    {
        _cameraOffset = new Vector3(0f, _cameraHeight, 0f);
        
        // 줌 버튼 이벤트 연결
        SetupZoomButtons();

    }

    private void Update()
    {
        HandleScrollZoom();
    }

    /// <summary>
    /// 마우스 휠 줌 처리 (미니맵 위에서만)
    /// </summary>
    private void HandleScrollZoom()
    {
        if (!_enableScrollZoom) return;
        if (_minimapDisplay == null) return;
        
        // 마우스가 미니맵 UI 위에 있는지 확인
        if (!IsMouseOverMinimap()) return;
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // 스크롤 위 = 줌 인, 스크롤 아래 = 줌 아웃
            float newSize = _cameraSize - (scroll * _scrollZoomSpeed);
            SetCameraSize(newSize);
        }
    }
    
    /// <summary>
    /// 마우스가 미니맵 위에 있는지 확인
    /// </summary>
    private bool IsMouseOverMinimap()
    {
        if (_minimapDisplay == null) return false;
        
        RectTransform rectTransform = _minimapDisplay.rectTransform;
        Vector2 localMousePosition;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, 
            Input.mousePosition, 
            null, 
            out localMousePosition))
        {
            return rectTransform.rect.Contains(localMousePosition);
        }
        
        return false;
    }

/// <summary>
    /// 줌 버튼 이벤트 연결
    /// </summary>
    private void SetupZoomButtons()
    {
        // 자동 버튼 찾기
        if (_zoomInButton == null)
        {
            GameObject zoomInObj = GameObject.Find("MinimapZoomIn");
            if (zoomInObj != null)
            {
                _zoomInButton = zoomInObj.GetComponent<UnityEngine.UI.Button>();
            }
        }
        
        if (_zoomOutButton == null)
        {
            GameObject zoomOutObj = GameObject.Find("MinimapZoomOut");
            if (zoomOutObj != null)
            {
                _zoomOutButton = zoomOutObj.GetComponent<UnityEngine.UI.Button>();
            }
        }
        
        // 버튼 클릭 이벤트 연결
        if (_zoomInButton != null)
        {
            _zoomInButton.onClick.RemoveAllListeners();
            _zoomInButton.onClick.AddListener(ZoomIn);
            Debug.Log("[MinimapSystem] ZoomIn button connected");
        }
        
        if (_zoomOutButton != null)
        {
            _zoomOutButton.onClick.RemoveAllListeners();
            _zoomOutButton.onClick.AddListener(ZoomOut);
            Debug.Log("[MinimapSystem] ZoomOut button connected");
        }
    }
    
    /// <summary>
    /// 현재 줌 레벨 반환 (UI 표시용)
    /// </summary>
    public float GetCurrentZoom()
    {
        return _cameraSize;
    }
    
    /// <summary>
    /// 줌 레벨을 퍼센트로 반환 (0 = 최대 확대, 1 = 최대 축소)
    /// </summary>
    public float GetZoomPercent()
    {
        return (_cameraSize - _minZoom) / (_maxZoom - _minZoom);
    }


    
private void LateUpdate()
    {
        if (_playerTransform == null || _minimapCamera == null)
        {
            return;
        }

        UpdateCameraPosition();
        UpdateCameraRotation();
        UpdatePlayerIcon();
    }

private void InitializeMinimapCamera()
    {
        if (_minimapCamera == null)
        {
            Debug.LogWarning("[MinimapSystem] MinimapCamera is null!");
            return;
        }

        // RenderTexture 생성
        _minimapRenderTexture = new RenderTexture(_renderTextureSize, _renderTextureSize, 16);
        _minimapRenderTexture.name = "MinimapRenderTexture";
        _minimapCamera.targetTexture = _minimapRenderTexture;
        Debug.Log($"[MinimapSystem] RenderTexture created: {_renderTextureSize}x{_renderTextureSize}");

        // 카메라 설정
        _minimapCamera.orthographic = true;
        _minimapCamera.orthographicSize = _cameraSize;
        // 모든 기본 레이어를 포함하도록 설정 (Minimap 전용 레이어가 없으므로)
        _minimapCamera.cullingMask = ~0; // Everything
        Debug.Log($"[MinimapSystem] Camera configured - Size: {_cameraSize}, CullingMask: Everything");

        // UI에 렌더 텍스처 연결
        if (_minimapDisplay != null)
        {
            _minimapDisplay.texture = _minimapRenderTexture;
            Debug.Log("[MinimapSystem] RenderTexture connected to MinimapDisplay");
        }
        else
        {
            Debug.LogWarning("[MinimapSystem] MinimapDisplay is null! Cannot connect RenderTexture.");
        }
    }

    private void UpdateCameraPosition()
    {
        Vector3 targetPosition = _playerTransform.position + _cameraOffset;
        _minimapCamera.transform.position = Vector3.Lerp(
            _minimapCamera.transform.position,
            targetPosition,
            _smoothSpeed * Time.deltaTime
        );
    }

    private void UpdateCameraRotation()
    {
        if (_rotateWithPlayer)
        {
            // 플레이어 Y축 회전에 맞춰 카메라 회전 (미니맵이 플레이어 시점 기준)
            float playerYRotation = _playerTransform.eulerAngles.y;
            _minimapCamera.transform.rotation = Quaternion.Euler(90f, playerYRotation, 0f);
        }
        else
        {
            // 고정 방향 (북쪽이 항상 위)
            _minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private void UpdatePlayerIcon()
    {
        if (_playerIcon == null || !_rotateWithPlayer)
        {
            return;
        }

        // 플레이어 아이콘은 항상 위를 향함 (미니맵이 회전하므로)
        _playerIcon.localRotation = Quaternion.identity;
    }

public void SetCameraSize(float size)
    {
        _cameraSize = Mathf.Clamp(size, _minZoom, _maxZoom);
        if (_minimapCamera != null)
        {
            _minimapCamera.orthographicSize = _cameraSize;
        }
    }

public void ZoomIn()
    {
        SetCameraSize(_cameraSize - _zoomStep);
        Debug.Log($"[MinimapSystem] Zoom In: {_cameraSize}");
    }

public void ZoomOut()
    {
        SetCameraSize(_cameraSize + _zoomStep);
        Debug.Log($"[MinimapSystem] Zoom Out: {_cameraSize}");
    }

    public void SetRotateWithPlayer(bool rotate)
    {
        _rotateWithPlayer = rotate;
    }

    private void OnDestroy()
    {
        if (_minimapRenderTexture != null)
        {
            _minimapRenderTexture.Release();
            Destroy(_minimapRenderTexture);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Setup Minimap Camera")]
    private void SetupMinimapCameraEditor()
    {
        if (_minimapCamera == null)
        {
            GameObject camObj = new GameObject("MinimapCamera");
            camObj.transform.SetParent(transform);
            _minimapCamera = camObj.AddComponent<Camera>();
            _minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            _minimapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            _minimapCamera.orthographic = true;
            _minimapCamera.orthographicSize = _cameraSize;
            Debug.Log("MinimapCamera created!");
        }
    }
#endif
}
