using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 크로스헤어 UI 컨트롤러 (강화된 탄퍼짐 연동)
/// 반동 시스템에서 전달받은 확산값에 따라 크로스헤어 크기와 색상을 조절
/// Canvas의 Crosshair UI 오브젝트에 부착
/// </summary>
public class CrosshairController : MonoBehaviour
{
    [Header("크로스헤어 기본 설정")]
    [Tooltip("기본 크로스헤어 크기 (픽셀)")]
    [SerializeField] private float _baseCrosshairSize = 20f;
    
    [Tooltip("최대 확산 시 크기 배율")]
    [SerializeField] private float _maxSpreadMultiplier = 3f;
    
    [Tooltip("크기 변경 부드러움")]
    [SerializeField] private float _smoothSpeed = 15f;
    
    [Header("탄퍼짐 연동 설정")]
    [Tooltip("탄퍼짐 각도당 픽셀 변환 배율")]
    [SerializeField] private float _spreadToPixelRatio = 4f;
    
    [Tooltip("탄퍼짐에 따른 색상 변화 사용")]
    [SerializeField] private bool _useSpreadColorFeedback = true;
    
    [Tooltip("정확(낮은 탄퍼짐) 시 색상")]
    [SerializeField] private Color _accurateColor = new Color(0f, 1f, 0.5f, 1f);
    
    [Tooltip("중간 탄퍼짐 시 색상")]
    [SerializeField] private Color _mediumSpreadColor = new Color(1f, 1f, 0f, 1f);
    
    [Tooltip("부정확(높은 탄퍼짐) 시 색상")]
    [SerializeField] private Color _inaccurateColor = new Color(1f, 0.3f, 0f, 1f);
    
    [Tooltip("탄퍼짐 색상 변화 임계값 - 정확->중간 (각도)")]
    [SerializeField] private float _spreadThreshold1 = 2f;
    
    [Tooltip("탄퍼짐 색상 변화 임계값 - 중간->부정확 (각도)")]
    [SerializeField] private float _spreadThreshold2 = 5f;
    
    [Header("ADS 설정")]
    [Tooltip("조준 시 크로스헤어 숨김")]
    [SerializeField] private bool _hideOnADS = true;
    
    [Tooltip("ADS 전환 페이드 속도")]
    [SerializeField] private float _adsFadeSpeed = 10f;
    
    [Header("플레이어 상태 피드백")]
    [Tooltip("플레이어 상태에 따른 시각적 표시")]
    [SerializeField] private bool _showPlayerStateFeedback = true;
    
    [Tooltip("이동 중 색상 보정")]
    [SerializeField] private Color _movingTint = new Color(1f, 1f, 1f, 0.9f);
    
    [Tooltip("공중(점프) 상태 색상 보정")]
    [SerializeField] private Color _airborneTint = new Color(1f, 0.6f, 0.6f, 0.85f);
    
    [Header("시각적 설정")]
    [Tooltip("기본 크로스헤어 색상")]
    [SerializeField] private Color _normalColor = Color.white;
    
    [Tooltip("적 타겟팅 시 색상")]
    [SerializeField] private Color _enemyTargetColor = Color.red;
    
    [Header("참조 (자동 할당)")]
    [SerializeField] private RectTransform _crosshairRect;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image[] _crosshairImages;
    
    [Header("디버그")]
    [SerializeField] private bool _showDebugInfo = false;
    
    // 상태
    private float _currentSpread;
    private float _targetSpread;
    private float _currentBulletSpread;
    private float _targetBulletSpread;
    private float _currentAlpha = 1f;
    private float _targetAlpha = 1f;
    private bool _isAiming;
    private bool _isTargetingEnemy;
    private Color _currentColor;
    private Color _targetColor;
    
    // 플레이어 상태
    private bool _isMoving;
    private bool _isAirborne;
    private bool _isCrouching;
    
    
    // 카메라 반동 연동
    private Vector2 _currentRecoilOffset;
    private Vector2 _targetRecoilOffset;
    [Header("카메라 반동 연동")]
    [Tooltip("카메라 반동을 크로스헤어 위치에 반영")]
    [SerializeField] private bool _syncWithCameraRecoil = true;
    [Tooltip("반동 오프셋 배율")]
    [SerializeField] private float _recoilOffsetMultiplier = 0.5f;
    [Tooltip("반동 오프셋 최대값 (픽셀)")]
    [SerializeField] private float _maxRecoilOffset = 20f;
private float _movementSpeed;
    
    // 초기값 저장
    private Vector2 _initialSize;
    
    private void Awake()
    {
        if (_crosshairRect == null)
            _crosshairRect = GetComponent<RectTransform>();
        
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        if (_crosshairImages == null || _crosshairImages.Length == 0)
            _crosshairImages = GetComponentsInChildren<Image>();
        
        if (_crosshairRect != null)
            _initialSize = _crosshairRect.sizeDelta;
        
        _currentColor = _normalColor;
        _targetColor = _normalColor;
    }
    
    private void Start()
    {
        if (RecoilSystem.Instance != null)
        {
            RecoilSystem.Instance.SetCrosshairController(this);
            RecoilSystem.Instance.OnBulletSpreadChanged += OnBulletSpreadChanged;
        }
    }
    
    private void OnBulletSpreadChanged(float bulletSpread)
    {
        _targetBulletSpread = bulletSpread;
    }
    
    private void OnDestroy()
    {
        if (RecoilSystem.Instance != null)
            RecoilSystem.Instance.OnBulletSpreadChanged -= OnBulletSpreadChanged;
    }
    
private void Update()
    {
        SyncPlayerState();
        UpdateCrosshairSize();
        UpdateCrosshairColor();
        UpdateCrosshairAlpha();
        UpdateRecoilOffset();  // 반동 오프셋 업데이트 추가
        
        if (_showDebugInfo)
            DrawDebugInfo();
    }
    
    private void SyncPlayerState()
    {
        if (RecoilSystem.Instance != null)
        {
            _isMoving = RecoilSystem.Instance.IsMoving();
            _isAirborne = RecoilSystem.Instance.IsAirborne();
            _isCrouching = RecoilSystem.Instance.IsCrouching();
        }
    }
    
private void UpdateCrosshairSize()
    {
        _currentSpread = Mathf.Lerp(_currentSpread, _targetSpread, Time.deltaTime * _smoothSpeed);
        _currentBulletSpread = Mathf.Lerp(_currentBulletSpread, _targetBulletSpread, Time.deltaTime * _smoothSpeed);
        
        if (_crosshairRect != null)
        {
            // 탄퍼짐 각도를 화면 픽셀로 정확하게 변환
            // FOV와 화면 크기를 고려한 계산
            float fov = Camera.main != null ? Camera.main.fieldOfView : 60f;
            float screenHeight = Screen.height;
            
            // 탄퍼짐 각도(도)를 화면 픽셀로 변환
            // 공식: pixels = tan(angle) * (screenHeight / 2) / tan(fov/2)
            float bulletSpreadPixels = Mathf.Tan(_currentBulletSpread * Mathf.Deg2Rad) 
                                       * (screenHeight * 0.5f) 
                                       / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            
            // 기존 확산값과 결합 (둘 중 큰 값 사용)
            float totalSpreadPixels = Mathf.Max(_currentSpread, bulletSpreadPixels * 2f);
            
            // 최종 크기 계산
            float targetSize = _baseCrosshairSize + totalSpreadPixels;
            float clampedSize = Mathf.Clamp(targetSize, _baseCrosshairSize, _baseCrosshairSize * _maxSpreadMultiplier);
            
            _crosshairRect.sizeDelta = new Vector2(clampedSize, clampedSize);
        }
    }
    
    private void UpdateCrosshairColor()
    {
        if (_isTargetingEnemy)
        {
            _targetColor = _enemyTargetColor;
        }
        else
        {
            Color spreadBasedColor = CalculateSpreadColor();
            
            if (_showPlayerStateFeedback)
                spreadBasedColor = ApplyPlayerStateTint(spreadBasedColor);
            
            _targetColor = spreadBasedColor;
        }
        
        _currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime * _smoothSpeed);
        
        foreach (var image in _crosshairImages)
        {
            if (image != null)
                image.color = _currentColor;
        }
    }
    
    private Color CalculateSpreadColor()
    {
        if (!_useSpreadColorFeedback)
            return _normalColor;
        
        float spread = _currentBulletSpread;
        
        if (spread <= _spreadThreshold1)
        {
            float t = spread / _spreadThreshold1;
            return Color.Lerp(_accurateColor, _mediumSpreadColor, t * 0.3f);
        }
        else if (spread <= _spreadThreshold2)
        {
            float t = (spread - _spreadThreshold1) / (_spreadThreshold2 - _spreadThreshold1);
            return Color.Lerp(_accurateColor, _mediumSpreadColor, t);
        }
        else
        {
            float t = Mathf.Clamp01((spread - _spreadThreshold2) / _spreadThreshold2);
            return Color.Lerp(_mediumSpreadColor, _inaccurateColor, t);
        }
    }
    
    private Color ApplyPlayerStateTint(Color baseColor)
    {
        if (_isAirborne)
            return baseColor * _airborneTint;
        
        if (_isMoving && _movementSpeed > 0.3f)
            return Color.Lerp(baseColor, baseColor * _movingTint, _movementSpeed);
        
        return baseColor;
    }
    
    private void UpdateRecoilOffset()
    {
        if (!_syncWithCameraRecoil || _crosshairRect == null) return;
        
        // 부드럽게 오프셋 적용
        _currentRecoilOffset = Vector2.Lerp(_currentRecoilOffset, _targetRecoilOffset, Time.deltaTime * _smoothSpeed * 2f);
        
        // 크로스헤어 위치에 오프셋 적용
        _crosshairRect.anchoredPosition = _currentRecoilOffset;
        
        // 반동 회복 (목표가 0으로 돌아감)
        _targetRecoilOffset = Vector2.Lerp(_targetRecoilOffset, Vector2.zero, Time.deltaTime * 8f);
    }
    
    
private void UpdateCrosshairAlpha()
    {
        _currentAlpha = Mathf.Lerp(_currentAlpha, _targetAlpha, Time.deltaTime * _adsFadeSpeed);
        
        if (_canvasGroup != null)
            _canvasGroup.alpha = _currentAlpha;
    }
    
    public void SetSpread(float spread)
    {
        _targetSpread = spread;
    }
    
    public void SetBulletSpread(float bulletSpread)
    {
        _targetBulletSpread = bulletSpread;
    }
    
    public void SetAiming(bool isAiming)
    {
        _isAiming = isAiming;
        
        if (_hideOnADS)
            _targetAlpha = isAiming ? 0f : 1f;
    }
    
    public void SetPlayerState(bool isMoving, bool isAirborne, bool isCrouching, float normalizedSpeed = 1f)
    {
        _isMoving = isMoving;
        _isAirborne = isAirborne;
        _isCrouching = isCrouching;
        _movementSpeed = Mathf.Clamp01(normalizedSpeed);
    }
    
    public void SetTargetingEnemy(bool isTargeting)
    {
        _isTargetingEnemy = isTargeting;
    }
    
    public void SetVisible(bool visible)
    {
        _targetAlpha = visible ? 1f : 0f;
    }
    
    public void ResetCrosshair()
    {
        _currentSpread = 0f;
        _targetSpread = 0f;
        _currentBulletSpread = 0f;
        _targetBulletSpread = 0f;
        _currentAlpha = 1f;
        _targetAlpha = 1f;
        _currentColor = _normalColor;
        _targetColor = _normalColor;
        
        if (_crosshairRect != null)
            _crosshairRect.sizeDelta = new Vector2(_baseCrosshairSize, _baseCrosshairSize);
        
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
        
        foreach (var image in _crosshairImages)
        {
            if (image != null)
                image.color = _normalColor;
        }
    }
    
    public void SetBaseCrosshairSize(float size)
    {
        _baseCrosshairSize = size;
    }
    
    public void SetSpreadToPixelRatio(float ratio)
    {
        _spreadToPixelRatio = ratio;
    }
    
    public float GetCurrentSpread() => _currentSpread;
    public float GetCurrentBulletSpread() => _currentBulletSpread;
    
    
    /// <summary>
    /// 카메라 반동 오프셋 설정 (RecoilSystem에서 호출)
    /// </summary>
    public void SetRecoilOffset(Vector2 recoilOffset)
    {
        if (_syncWithCameraRecoil)
        {
            // 반동 방향을 크로스헤어 오프셋으로 변환
            _targetRecoilOffset.x = Mathf.Clamp(recoilOffset.y * _recoilOffsetMultiplier, -_maxRecoilOffset, _maxRecoilOffset);
            _targetRecoilOffset.y = Mathf.Clamp(-recoilOffset.x * _recoilOffsetMultiplier, -_maxRecoilOffset, _maxRecoilOffset);
        }
    }
    
    /// <summary>
    /// 카메라 반동 오프셋 초기화
    /// </summary>
    public void ResetRecoilOffset()
    {
        _targetRecoilOffset = Vector2.zero;
    }
public float GetCurrentSize() => _crosshairRect != null ? _crosshairRect.sizeDelta.x : _baseCrosshairSize;
    
    private void DrawDebugInfo()
    {
        string state = _isAirborne ? "공중" : _isMoving ? "이동" : _isCrouching ? "웅크림" : "정지";
        Debug.Log($"[Crosshair] 탄퍼짐: {_currentBulletSpread:F2}° | 확산: {_currentSpread:F1}px | 크기: {GetCurrentSize():F1}px | 상태: {state}");
    }
}
