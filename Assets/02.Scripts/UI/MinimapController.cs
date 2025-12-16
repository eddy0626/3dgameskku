using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 탑다운 미니맵 시스템 - 원신 스타일
/// 플레이어를 중심으로 회전하며 주변 환경을 표시
/// </summary>
public class MinimapController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera _minimapCamera;
    [SerializeField] private float _cameraHeight = 50f;
    [SerializeField] private float _cameraSize = 30f;
    
    [Header("Target")]
    [SerializeField] private Transform _player;
    
    [Header("UI References")]
    [SerializeField] private RawImage _minimapImage;
    [SerializeField] private RectTransform _playerIcon;
    [SerializeField] private RectTransform _minimapMask;
    
    [Header("Rotation Settings")]
    [SerializeField] private bool _rotateWithPlayer = true;
    
private void Awake()
    {
        InitializeReferences();
        SetupRenderTexture();
    }
    
    private void InitializeReferences()
    {
        if (_player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.transform;
            }
        }
        
        if (_minimapCamera == null)
        {
            _minimapCamera = GetComponentInChildren<Camera>();
        }
    }

private RenderTexture _renderTexture;
    
    private void SetupRenderTexture()
    {
        if (_minimapCamera == null)
        {
            return;
        }
        
        // RenderTexture 동적 생성
        _renderTexture = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32);
        _renderTexture.filterMode = FilterMode.Bilinear;
        _renderTexture.Create();
        
        // 카메라에 렌더 타겟 설정
        _minimapCamera.targetTexture = _renderTexture;
        
        // UI에 연결
        if (_minimapImage != null)
        {
            _minimapImage.texture = _renderTexture;
        }
    }

private void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
    }


    
    private void LateUpdate()
    {
        if (_player == null || _minimapCamera == null)
        {
            return;
        }
        
        UpdateCameraPosition();
        UpdateCameraRotation();
        UpdatePlayerIcon();
    }
    
    private void UpdateCameraPosition()
    {
        Vector3 newPosition = _player.position;
        newPosition.y = _player.position.y + _cameraHeight;
        _minimapCamera.transform.position = newPosition;
    }
    
    private void UpdateCameraRotation()
    {
        if (_rotateWithPlayer)
        {
            // 카메라가 플레이어 Y축 회전을 따라감 (원신 스타일)
            float playerYRotation = _player.eulerAngles.y;
            _minimapCamera.transform.rotation = Quaternion.Euler(90f, playerYRotation, 0f);
        }
        else
        {
            // 고정 방향 (북쪽이 위)
            _minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
    
    private void UpdatePlayerIcon()
    {
        if (_playerIcon == null)
        {
            return;
        }
        
        // 플레이어 아이콘은 항상 중앙에 고정
        // 회전 모드에 따라 아이콘 회전 처리
        if (_rotateWithPlayer)
        {
            // 카메라가 회전하므로 아이콘은 고정 (항상 위를 향함)
            _playerIcon.localRotation = Quaternion.identity;
        }
        else
        {
            // 카메라 고정이면 아이콘이 플레이어 방향을 표시
            _playerIcon.localRotation = Quaternion.Euler(0f, 0f, -_player.eulerAngles.y);
        }
    }
    
    /// <summary>
    /// 미니맵 줌 레벨 조절
    /// </summary>
    public void SetZoom(float size)
    {
        _cameraSize = Mathf.Clamp(size, 10f, 100f);
        if (_minimapCamera != null)
        {
            _minimapCamera.orthographicSize = _cameraSize;
        }
    }
    
    /// <summary>
    /// 회전 모드 토글
    /// </summary>
    public void ToggleRotationMode()
    {
        _rotateWithPlayer = !_rotateWithPlayer;
    }
    
    /// <summary>
    /// 런타임에서 플레이어 설정
    /// </summary>
    public void SetPlayer(Transform player)
    {
        _player = player;
    }
}
