using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 플레이어 체력바 UI 컨트롤러
/// DNF 스타일: 메인 바 즉시 감소 + 딜레이 바가 천천히 따라감
/// </summary>
public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private Image _damageDelayImage;
    [SerializeField] private Image _backgroundImage;
    
    [Header("Player Reference")]
    [SerializeField] private PlayerHealth _playerHealth;
    
    [Header("Animation Settings")]
    [SerializeField] private float _damageDelayDuration = 0.5f;
    [SerializeField] private float _damageDelayDelay = 0.3f;
    [SerializeField] private Ease _damageDelayEase = Ease.OutQuad;
    
    [Header("Color Settings")]
    [SerializeField] private Color _healthyColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color _warnColor = new Color(1f, 0.8f, 0f);
    [SerializeField] private Color _dangerColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private float _warnThreshold = 0.5f;
    [SerializeField] private float _dangerThreshold = 0.25f;
    
    private Tweener _delayTween;
    
    private void Start()
    {
        // PlayerHealth 자동 찾기
        if (_playerHealth == null)
        {
            _playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
        
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged += HandleHealthChanged;
            
            // 초기 상태 설정
            float percent = _playerHealth.HealthPercent;
            SetFillImmediate(percent);
            UpdateHealthColor(percent);
        }
        else
        {
            Debug.LogWarning("[PlayerHealthBarUI] PlayerHealth를 찾을 수 없습니다!");
        }
    }
    
    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged -= HandleHealthChanged;
        }
        
        _delayTween?.Kill();
    }
    
    /// <summary>
    /// 체력 변경 처리
    /// </summary>
    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        float percent = maxHealth > 0 ? currentHealth / maxHealth : 0f;
        
        // 메인 체력바 즉시 업데이트
        if (_fillImage != null)
        {
            _fillImage.fillAmount = percent;
        }
        
        // 색상 업데이트
        UpdateHealthColor(percent);
        
        // 딜레이 바 애니메이션 (데미지 시에만)
        AnimateDamageDelay(percent);
    }
    
    /// <summary>
    /// 데미지 딜레이 바 애니메이션
    /// </summary>
    private void AnimateDamageDelay(float targetPercent)
    {
        if (_damageDelayImage == null) return;
        
        // 현재 딜레이 바가 타겟보다 큰 경우에만 애니메이션 (데미지 받은 경우)
        if (_damageDelayImage.fillAmount > targetPercent)
        {
            _delayTween?.Kill();
            
            _delayTween = _damageDelayImage
                .DOFillAmount(targetPercent, _damageDelayDuration)
                .SetDelay(_damageDelayDelay)
                .SetEase(_damageDelayEase);
        }
        else
        {
            // 힐 받은 경우 즉시 동기화
            _damageDelayImage.fillAmount = targetPercent;
        }
    }
    
    /// <summary>
    /// 체력에 따른 색상 업데이트
    /// </summary>
    private void UpdateHealthColor(float percent)
    {
        if (_fillImage == null) return;
        
        Color targetColor;
        
        if (percent <= _dangerThreshold)
        {
            targetColor = _dangerColor;
        }
        else if (percent <= _warnThreshold)
        {
            // 경고와 위험 사이 보간
            float t = (percent - _dangerThreshold) / (_warnThreshold - _dangerThreshold);
            targetColor = Color.Lerp(_dangerColor, _warnColor, t);
        }
        else
        {
            // 경고와 건강 사이 보간
            float t = (percent - _warnThreshold) / (1f - _warnThreshold);
            targetColor = Color.Lerp(_warnColor, _healthyColor, t);
        }
        
        _fillImage.color = targetColor;
    }
    
    /// <summary>
    /// 즉시 체력바 설정 (초기화용)
    /// </summary>
    private void SetFillImmediate(float percent)
    {
        if (_fillImage != null)
        {
            _fillImage.fillAmount = percent;
        }
        
        if (_damageDelayImage != null)
        {
            _damageDelayImage.fillAmount = percent;
        }
    }
    
    /// <summary>
    /// 체력바 리셋 (부활 시 사용)
    /// </summary>
    public void ResetHealthBar()
    {
        _delayTween?.Kill();
        SetFillImmediate(1f);
        UpdateHealthColor(1f);
    }
}
