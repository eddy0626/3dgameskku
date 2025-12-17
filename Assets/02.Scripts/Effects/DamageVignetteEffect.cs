using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

/// <summary>
/// 플레이어 피격 시 빨간색 비네트 스크린 효과
/// URP Post Processing Volume 사용, DOTween 애니메이션
/// </summary>
public class DamageVignetteEffect : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Volume _postProcessVolume;
    [SerializeField] private PlayerHealth _playerHealth;
    
    [Header("비네트 설정")]
    [SerializeField] private Color _damageColor = new Color(0.8f, 0f, 0f, 1f);
    [SerializeField] private float _maxIntensity = 0.5f;
    [SerializeField] private float _minIntensity = 0f;
    
    [Header("애니메이션 설정")]
    [SerializeField] private float _fadeInDuration = 0.1f;
    [SerializeField] private float _fadeOutDuration = 0.4f;
    [SerializeField] private Ease _fadeInEase = Ease.OutQuad;
    [SerializeField] private Ease _fadeOutEase = Ease.InQuad;
    
    [Header("체력 기반 효과")]
    [SerializeField] private bool _useHealthBasedIntensity = true;
    [SerializeField] private float _lowHealthThreshold = 0.3f;
    [SerializeField] private float _lowHealthPulseSpeed = 2f;
    
    private Vignette _vignette;
    private Tween _currentTween;
    private bool _isLowHealth;
    private float _baseIntensity;
    
    private void Awake()
    {
        if (_playerHealth == null)
        {
            _playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
        
        if (_postProcessVolume == null)
        {
            _postProcessVolume = FindFirstObjectByType<Volume>();
        }
    }
    
    private void Start()
    {
        InitializeVignette();
        SubscribeEvents();
    }
    
    private void OnDestroy()
    {
        UnsubscribeEvents();
        _currentTween?.Kill();
    }
    
    private void Update()
    {
        if (_isLowHealth && _vignette != null)
        {
            float pulse = Mathf.Sin(Time.time * _lowHealthPulseSpeed * Mathf.PI) * 0.5f + 0.5f;
            float pulseIntensity = Mathf.Lerp(_minIntensity, _maxIntensity * 0.5f, pulse);
            _vignette.intensity.value = Mathf.Max(_baseIntensity, pulseIntensity);
        }
    }
    
    private void InitializeVignette()
    {
        if (_postProcessVolume == null)
        {
            Debug.LogWarning("[DamageVignetteEffect] Post Process Volume이 없습니다. 비네트 효과 비활성화.");
            enabled = false;
            return;
        }

        if (_postProcessVolume.profile == null)
        {
            Debug.LogWarning("[DamageVignetteEffect] Volume Profile이 없습니다. 비네트 효과 비활성화.");
            enabled = false;
            return;
        }

        if (_postProcessVolume.profile.TryGet(out _vignette))
        {
            _vignette.active = true;
            _vignette.intensity.overrideState = true;
            _vignette.color.overrideState = true;
            _vignette.intensity.value = _minIntensity;
            _vignette.color.value = _damageColor;

            Debug.Log("[DamageVignetteEffect] Vignette 초기화 완료");
        }
        else
        {
            Debug.LogWarning("[DamageVignetteEffect] Volume Profile에 Vignette가 없습니다. 비네트 효과 비활성화.");
            enabled = false;
        }
    }
    
    private void SubscribeEvents()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDamageTaken += OnPlayerDamaged;
            _playerHealth.OnHealthChanged += OnHealthChanged;
            _playerHealth.OnDeath += OnPlayerDeath;
            Debug.Log("[DamageVignetteEffect] PlayerHealth 이벤트 구독 완료");
        }
        else
        {
            Debug.LogWarning("[DamageVignetteEffect] PlayerHealth를 찾을 수 없습니다!");
        }
    }
    
    private void UnsubscribeEvents()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDamageTaken -= OnPlayerDamaged;
            _playerHealth.OnHealthChanged -= OnHealthChanged;
            _playerHealth.OnDeath -= OnPlayerDeath;
        }
    }
    
    private void OnPlayerDamaged(float damage)
    {
        PlayDamageEffect();
    }
    
    private void OnHealthChanged(float current, float max)
    {
        if (!_useHealthBasedIntensity) return;
        
        float healthPercent = max > 0 ? current / max : 0f;
        _isLowHealth = healthPercent <= _lowHealthThreshold && healthPercent > 0f;
        
        if (!_isLowHealth)
        {
            _baseIntensity = _minIntensity;
        }
    }
    
    private void OnPlayerDeath()
    {
        _currentTween?.Kill();
        _isLowHealth = false;
        
        if (_vignette != null)
        {
            DOTween.To(
                () => _vignette.intensity.value,
                x => _vignette.intensity.value = x,
                _maxIntensity * 1.2f,
                _fadeInDuration * 2f
            ).SetEase(Ease.OutQuad);
        }
    }
    
    /// <summary>
    /// 피격 비네트 효과 재생
    /// </summary>
    public void PlayDamageEffect()
    {
        if (_vignette == null) return;
        
        _currentTween?.Kill();
        
        Sequence sequence = DOTween.Sequence();
        
        sequence.Append(
            DOTween.To(
                () => _vignette.intensity.value,
                x => _vignette.intensity.value = x,
                _maxIntensity,
                _fadeInDuration
            ).SetEase(_fadeInEase)
        );
        
        sequence.Append(
            DOTween.To(
                () => _vignette.intensity.value,
                x => _vignette.intensity.value = x,
                _isLowHealth ? _maxIntensity * 0.3f : _minIntensity,
                _fadeOutDuration
            ).SetEase(_fadeOutEase)
        );
        
        sequence.OnComplete(() => _baseIntensity = _vignette.intensity.value);
        sequence.SetUpdate(true);
        
        _currentTween = sequence;
    }
    
    /// <summary>
    /// 효과 즉시 중지
    /// </summary>
    public void StopEffect()
    {
        _currentTween?.Kill();
        _isLowHealth = false;
        
        if (_vignette != null)
        {
            _vignette.intensity.value = _minIntensity;
        }
    }
    
    [ContextMenu("Test Damage Effect")]
    public void TestEffect()
    {
        PlayDamageEffect();
    }
}
