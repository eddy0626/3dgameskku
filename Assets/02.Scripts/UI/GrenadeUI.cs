using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 수류탄 UI 컴포넌트
/// 왼쪽 하단에 아이콘, 개수, 쿠킹 게이지 표시
/// </summary>
public class GrenadeUI : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Image _grenadeIcon;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private Image _cookingGaugeFill;
    [SerializeField] private GameObject _cookingGaugeContainer;
    
    [Header("색상 설정")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _emptyColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [SerializeField] private Color _cookingStartColor = new Color(1f, 0.8f, 0f, 1f);
    [SerializeField] private Color _cookingEndColor = new Color(1f, 0.2f, 0f, 1f);
    
    [Header("애니메이션")]
    [SerializeField] private float _pulseSpeed = 3f;
    [SerializeField] private float _pulseMinScale = 0.9f;
    [SerializeField] private float _pulseMaxScale = 1.1f;
    
    [Header("참조")]
    [SerializeField] private GrenadeManager _grenadeManager;
    
    private RectTransform _iconRectTransform;
    private Vector3 _originalIconScale;
    private bool _isCooking;
    
    private void Awake()
    {
        // 아이콘 RectTransform 캐싱
        if (_grenadeIcon != null)
        {
            _iconRectTransform = _grenadeIcon.GetComponent<RectTransform>();
            _originalIconScale = _iconRectTransform.localScale;
        }
        
        // 쿠킹 게이지 초기 숨김
        if (_cookingGaugeContainer != null)
        {
            _cookingGaugeContainer.SetActive(false);
        }
    }
    
    private void Start()
    {
        // GrenadeManager 자동 찾기
        if (_grenadeManager == null)
        {
            _grenadeManager = GrenadeManager.Instance;
            
            if (_grenadeManager == null)
            {
                _grenadeManager = FindFirstObjectByType<GrenadeManager>();
            }
        }
        
        // 이벤트 구독
        if (_grenadeManager != null)
        {
            _grenadeManager.OnGrenadeCountChanged += UpdateGrenadeCount;
            _grenadeManager.OnCookingProgress += UpdateCookingGauge;
            _grenadeManager.OnGrenadeThrownEvent += OnGrenadeThrown;
            
            // 초기 값 설정
            UpdateGrenadeCount(_grenadeManager.CurrentGrenades, _grenadeManager.MaxGrenades);
        }
        else
        {
            Debug.LogWarning("GrenadeUI: GrenadeManager를 찾을 수 없습니다.");
        }
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (_grenadeManager != null)
        {
            _grenadeManager.OnGrenadeCountChanged -= UpdateGrenadeCount;
            _grenadeManager.OnCookingProgress -= UpdateCookingGauge;
            _grenadeManager.OnGrenadeThrownEvent -= OnGrenadeThrown;
        }
    }
    
    private void Update()
    {
        // 쿠킹 중 아이콘 펄스 애니메이션
        if (_isCooking && _iconRectTransform != null)
        {
            float pulse = Mathf.PingPong(Time.time * _pulseSpeed, 1f);
            float scale = Mathf.Lerp(_pulseMinScale, _pulseMaxScale, pulse);
            _iconRectTransform.localScale = _originalIconScale * scale;
        }
    }
    
    /// <summary>
    /// 수류탄 개수 업데이트
    /// </summary>
    private void UpdateGrenadeCount(int current, int max)
    {
        // 텍스트 업데이트
        if (_countText != null)
        {
            _countText.text = $"{current}/{max}";
        }
        
        // 아이콘 색상 업데이트
        if (_grenadeIcon != null)
        {
            _grenadeIcon.color = current > 0 ? _normalColor : _emptyColor;
        }
        
        // 텍스트 색상도 업데이트
        if (_countText != null)
        {
            _countText.color = current > 0 ? _normalColor : _emptyColor;
        }
    }
    
    /// <summary>
    /// 쿠킹 게이지 업데이트
    /// </summary>
    private void UpdateCookingGauge(float progress)
    {
        // 쿠킹 시작/종료 감지
        bool wasCooking = _isCooking;
        _isCooking = progress > 0f;
        
        // 쿠킹 시작
        if (_isCooking && !wasCooking)
        {
            if (_cookingGaugeContainer != null)
            {
                _cookingGaugeContainer.SetActive(true);
            }
        }
        
        // 쿠킹 종료
        if (!_isCooking && wasCooking)
        {
            if (_cookingGaugeContainer != null)
            {
                _cookingGaugeContainer.SetActive(false);
            }
            
            // 아이콘 스케일 복원
            if (_iconRectTransform != null)
            {
                _iconRectTransform.localScale = _originalIconScale;
            }
        }
        
        // 게이지 Fill 업데이트
        if (_cookingGaugeFill != null && _isCooking)
        {
            _cookingGaugeFill.fillAmount = progress;
            
            // 색상 그라데이션 (시작 → 종료)
            _cookingGaugeFill.color = Color.Lerp(_cookingStartColor, _cookingEndColor, progress);
        }
    }
    
    /// <summary>
    /// 수류탄 투척 시 호출
    /// </summary>
    private void OnGrenadeThrown()
    {
        // 투척 애니메이션 (아이콘 흔들림)
        StartCoroutine(ThrowAnimation());
    }
    
    private IEnumerator ThrowAnimation()
    {
        if (_iconRectTransform == null) yield break;
        
        // 빠른 스케일 축소 후 복원
        float duration = 0.15f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 축소 후 바운스
            float scale;
            if (t < 0.5f)
            {
                scale = Mathf.Lerp(1f, 0.7f, t * 2f);
            }
            else
            {
                scale = Mathf.Lerp(0.7f, 1f, (t - 0.5f) * 2f);
            }
            
            _iconRectTransform.localScale = _originalIconScale * scale;
            yield return null;
        }
        
        _iconRectTransform.localScale = _originalIconScale;
    }
    
    /// <summary>
    /// 수류탄 추가 시 애니메이션
    /// </summary>
    public void PlayPickupAnimation()
    {
        StartCoroutine(PickupAnimation());
    }
    
    private IEnumerator PickupAnimation()
    {
        if (_iconRectTransform == null) yield break;
        
        // 스케일 확대 후 복원
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 바운스 효과
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
            _iconRectTransform.localScale = _originalIconScale * scale;
            yield return null;
        }
        
        _iconRectTransform.localScale = _originalIconScale;
    }
}
