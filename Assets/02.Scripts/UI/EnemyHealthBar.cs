using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 타격감 있는 적 체력바 UI (DNF/소울라이크 스타일)
/// 
/// 핵심 기능:
/// - 메인 바: 피격 시 즉시 감소
/// - 트레일 바: 딜레이 후 부드럽게 슬라이드
/// - 피격 플래시: 순간 흰색 플래시
/// - 떨림 효과: 임팩트 있는 흔들림
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    #region Inspector Fields
    
    [Header("UI 참조")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private Image _trailImage;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _flashOverlay;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _shakeTarget;
    
    [Header("트레일 바 설정")]
    [SerializeField] private float _trailDelay = 0.15f;
    [SerializeField] private float _trailSlideDuration = 0.4f;
    [SerializeField] private Ease _trailEaseType = Ease.OutQuad;
    
    [Header("피격 플래시 설정")]
    [SerializeField] private bool _enableFlash = true;
    [SerializeField] private Color _flashColor = Color.white;
    [SerializeField] private float _flashDuration = 0.08f;
    [SerializeField] private float _flashFadeDuration = 0.12f;
    
    [Header("떨림 효과 설정")]
    [SerializeField] private bool _enableShake = true;
    [SerializeField] private float _shakeIntensity = 8f;
    [SerializeField] private float _shakeDuration = 0.15f;
    [SerializeField] private int _shakeVibrato = 20;
    [SerializeField] private float _shakeRandomness = 90f;
    
    [Header("데미지 스케일링")]
    [SerializeField] private bool _scaledByDamage = true;
    [SerializeField] private float _minDamageThreshold = 0.05f;
    [SerializeField] private float _maxDamageForFullEffect = 0.3f;
    
    [Header("위치/표시 설정")]
    [SerializeField] private float _heightOffset = 2.5f;
    [SerializeField] private float _showDuration = 5f;
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private float _fadeOutDuration = 0.5f;
    [SerializeField] private bool _billboard = true;
    
    [Header("색상")]
    [SerializeField] private Color _healthyColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color _damagedColor = new Color(0.8f, 0.8f, 0.2f);
    [SerializeField] private Color _criticalColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color _trailColor = new Color(1f, 0.3f, 0.1f, 0.95f);
    
    #endregion
    
    #region Private Fields
    
    private Transform _target;
    private Camera _mainCamera;
    private EnemyHealth _enemyHealth;
    private float _hideTimer;
    private bool _isVisible;
    private float _lastHealthPercent = 1f;
    private Vector3 _originalShakePosition;
    
    // Tweens
    private Tween _trailTween;
    private Tween _fadeTween;
    private Tween _flashTween;
    private Tween _shakeTween;
    private Sequence _hitSequence;
    
    #endregion
    
    #region Unity Callbacks
    #if UNITY_EDITOR
    private void OnValidate()
    {
        // 에디터에서 자동 참조 연결
        AutoFindUIElements();
    }
    
    private void Reset()
    {
        AutoFindUIElements();
    }
    #endif
    

    
    private void Awake()
    {
        AutoFindUIElements();
        InitializeComponents();
    }
    
    private void Start()
    {
        _mainCamera = Camera.main;
        
        if (_target == null && _enemyHealth == null)
        {
            AutoInitialize();
        }
    }
    
    private void LateUpdate()
    {
        if (_target == null) return;
        
        UpdatePosition();
        
        if (_billboard && _mainCamera != null)
        {
            transform.rotation = _mainCamera.transform.rotation;
        }
        
        if (_isVisible && _showDuration > 0)
        {
            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0)
            {
                Hide();
            }
        }
    }
    
    private void OnDestroy()
    {
        KillAllTweens();
    }
    
    #endregion
    
    #region Initialization
    
private void AutoFindUIElements()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        
        if (_shakeTarget == null)
            _shakeTarget = GetComponent<RectTransform>();
        
        if (_fillImage == null)
        {
            Transform t = transform.Find("Fill");
            if (t != null) _fillImage = t.GetComponent<Image>();
        }
        
        if (_trailImage == null)
        {
            Transform t = transform.Find("DamageDelay") ?? transform.Find("Trail");
            if (t != null) _trailImage = t.GetComponent<Image>();
        }
        
        if (_backgroundImage == null)
        {
            Transform t = transform.Find("Background");
            if (t != null) _backgroundImage = t.GetComponent<Image>();
        }
        
        if (_flashOverlay == null)
        {
            Transform t = transform.Find("FlashOverlay") ?? transform.Find("Flash");
            if (t != null)
            {
                _flashOverlay = t.GetComponent<Image>();
            }
            else if (Application.isPlaying)
            {
                // 런타임에 FlashOverlay 동적 생성
                CreateFlashOverlay();
            }
        }
    }

/// <summary>
    /// FlashOverlay 동적 생성 (런타임)
    /// </summary>
/// <summary>
    /// FlashOverlay 동적 생성 (런타임) - Fill 이미지와 동일한 위치/크기
    /// </summary>
    private void CreateFlashOverlay()
    {
        if (_fillImage == null)
        {
            Debug.LogWarning("[EnemyHealthBar] Fill 이미지가 없어 FlashOverlay를 생성할 수 없습니다.");
            return;
        }
        
        GameObject flashObj = new GameObject("FlashOverlay");
        flashObj.transform.SetParent(_fillImage.transform.parent, false);
        
        // Fill 이미지의 RectTransform 복사
        RectTransform fillRect = _fillImage.GetComponent<RectTransform>();
        RectTransform rect = flashObj.AddComponent<RectTransform>();
        
        rect.anchorMin = fillRect.anchorMin;
        rect.anchorMax = fillRect.anchorMax;
        rect.anchoredPosition = fillRect.anchoredPosition;
        rect.sizeDelta = fillRect.sizeDelta;
        rect.pivot = fillRect.pivot;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        
        // Image 추가 (Simple 타입, Stretch)
        _flashOverlay = flashObj.AddComponent<Image>();
        _flashOverlay.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, 0f);
        _flashOverlay.raycastTarget = false;
        
        // Fill 바로 위에 배치
        flashObj.transform.SetSiblingIndex(_fillImage.transform.GetSiblingIndex() + 1);
        
        Debug.Log($"[EnemyHealthBar] FlashOverlay 동적 생성 완료 (Fill 위치/크기 복사): {transform.parent?.name}");
    }

    
private void InitializeComponents()
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
        
        // Image Type을 Filled로 강제 설정 (프리팹 설정 오류 방지)
        if (_fillImage != null)
        {
            _fillImage.type = Image.Type.Filled;
            _fillImage.fillMethod = Image.FillMethod.Horizontal;
            _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fillImage.fillAmount = 1f;
        }
        
        if (_trailImage != null)
        {
            _trailImage.type = Image.Type.Filled;
            _trailImage.fillMethod = Image.FillMethod.Horizontal;
            _trailImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            _trailImage.fillAmount = 1f;
            _trailImage.color = _trailColor;
        }
        
        if (_flashOverlay != null)
        {
            _flashOverlay.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, 0f);
            _flashOverlay.raycastTarget = false;
        }
        
        if (_shakeTarget != null)
            _originalShakePosition = _shakeTarget.anchoredPosition;
        
        _isVisible = false;
        _lastHealthPercent = 1f;
    }
    
    public void Initialize(Transform target, EnemyHealth enemyHealth)
    {
        _target = target;
        _enemyHealth = enemyHealth;
        
        if (_enemyHealth != null)
        {
            _enemyHealth.OnHealthChanged += OnHealthChanged;
            _enemyHealth.OnDamaged += OnEnemyDamaged;
            _enemyHealth.OnDeath += OnEnemyDeath;
            _lastHealthPercent = _enemyHealth.HealthPercent;
        }
        
        UpdateHealthBarImmediate(_lastHealthPercent);
        UpdatePosition();
    }
    
    private void AutoInitialize()
    {
        Transform parent = transform.parent;
        if (parent != null)
        {
            EnemyHealth enemyHealth = parent.GetComponent<EnemyHealth>() 
                                   ?? parent.GetComponentInParent<EnemyHealth>();
            
            if (enemyHealth != null)
            {
                Initialize(parent, enemyHealth);
                Debug.Log($"[EnemyHealthBar] Impact-style initialized for {parent.name}");
                SetAlwaysVisible(true);
            }
        }
    }
    
    public void Cleanup()
    {
        if (_enemyHealth != null)
        {
            _enemyHealth.OnHealthChanged -= OnHealthChanged;
            _enemyHealth.OnDamaged -= OnEnemyDamaged;
            _enemyHealth.OnDeath -= OnEnemyDeath;
        }
        KillAllTweens();
    }
    
    private void KillAllTweens()
    {
        _trailTween?.Kill();
        _fadeTween?.Kill();
        _flashTween?.Kill();
        _shakeTween?.Kill();
        _hitSequence?.Kill();
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnEnemyDamaged(float damage, Vector3 hitPoint)
    {
        Show();
    }
    
    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        float healthPercent = maxHealth > 0 ? currentHealth / maxHealth : 0f;
        float damageAmount = _lastHealthPercent - healthPercent;
        
        if (damageAmount > 0.001f)
        {
            // 피격 시 타격감 효과 적용
            ApplyHitImpact(healthPercent, damageAmount);
        }
        else if (healthPercent > _lastHealthPercent)
        {
            // 회복 시 즉시 반영
            UpdateHealthBarImmediate(healthPercent);
        }
        
        _lastHealthPercent = healthPercent;
    }
    
    private void OnEnemyDeath()
    {
        KillAllTweens();
        _fadeTween = _canvasGroup.DOFade(0f, 0.3f)
            .OnComplete(() => gameObject.SetActive(false));
    }
    
    #endregion
    
    #region Hit Impact System
    
    /// <summary>
    /// 피격 시 타격감 효과 적용
    /// </summary>
    private void ApplyHitImpact(float targetHealthPercent, float damageAmount)
    {
        float effectScale = CalculateEffectScale(damageAmount);
        
        // 기존 시퀀스 정지
        _hitSequence?.Kill();
        _hitSequence = DOTween.Sequence();
        
        // 1. 메인 바 즉시 감소
        if (_fillImage != null)
        {
            _fillImage.fillAmount = targetHealthPercent;
            _fillImage.color = GetHealthColor(targetHealthPercent);
        }
        
        // 2. 흰색 플래시
        if (_enableFlash)
        {
            PlayFlashEffect(effectScale);
        }
        
        // 3. 떨림 효과
        if (_enableShake)
        {
            PlayShakeEffect(effectScale);
        }
        
        // 4. 트레일 바 딜레이 후 슬라이드
        if (_trailImage != null && _trailImage.fillAmount > targetHealthPercent)
        {
            float delay = _trailDelay * (1f + effectScale * 0.5f);
            float duration = _trailSlideDuration;
            
            _trailTween?.Kill();
            _trailTween = _trailImage.DOFillAmount(targetHealthPercent, duration)
                .SetEase(_trailEaseType)
                .SetDelay(delay);
        }
    }
    
    /// <summary>
    /// 데미지 양에 따른 효과 스케일 계산 (0~1)
    /// </summary>
    private float CalculateEffectScale(float damageAmount)
    {
        if (!_scaledByDamage) return 1f;
        
        if (damageAmount < _minDamageThreshold) return 0.3f;
        
        float normalized = Mathf.InverseLerp(_minDamageThreshold, _maxDamageForFullEffect, damageAmount);
        return Mathf.Lerp(0.4f, 1f, normalized);
    }
    
    #endregion
    
    #region Flash Effect
    
    /// <summary>
    /// 피격 시 흰색 플래시 효과
    /// </summary>
    private void PlayFlashEffect(float intensity)
    {
        _flashTween?.Kill();
        
        // 방법 1: FlashOverlay 이미지 사용 (권장)
        if (_flashOverlay != null)
        {
            Color flashCol = _flashColor;
            flashCol.a = intensity;
            
            _flashOverlay.color = flashCol;
            _flashTween = _flashOverlay.DOFade(0f, _flashFadeDuration)
                .SetDelay(_flashDuration)
                .SetEase(Ease.OutQuad);
        }
        // 방법 2: Fill 이미지 직접 플래시
        else if (_fillImage != null)
        {
            Color originalColor = GetHealthColor(_lastHealthPercent);
            Color flashCol = Color.Lerp(originalColor, _flashColor, intensity * 0.7f);
            
            _fillImage.color = flashCol;
            _flashTween = _fillImage.DOColor(originalColor, _flashFadeDuration)
                .SetDelay(_flashDuration)
                .SetEase(Ease.OutQuad);
        }
    }
    
    #endregion
    
    #region Shake Effect
    
    /// <summary>
    /// 체력바 떨림 효과
    /// </summary>
    private void PlayShakeEffect(float intensity)
    {
        if (_shakeTarget == null) return;
        
        _shakeTween?.Kill();
        
        // 원위치 복구 후 흔들기
        _shakeTarget.anchoredPosition = _originalShakePosition;
        
        float shakeStrength = _shakeIntensity * intensity;
        int vibrato = Mathf.RoundToInt(_shakeVibrato * intensity);
        
        _shakeTween = _shakeTarget.DOShakeAnchorPos(
            _shakeDuration,
            shakeStrength,
            vibrato,
            _shakeRandomness,
            snapping: false,
            fadeOut: true
        ).OnComplete(() => 
        {
            _shakeTarget.anchoredPosition = _originalShakePosition;
        });
    }
    
    #endregion
    
    #region Health Bar Update
    
    /// <summary>
    /// 체력바 즉시 업데이트 (초기화/회복용)
    /// </summary>
    private void UpdateHealthBarImmediate(float healthPercent)
    {
        healthPercent = Mathf.Clamp01(healthPercent);
        
        if (_fillImage != null)
        {
            _fillImage.fillAmount = healthPercent;
            _fillImage.color = GetHealthColor(healthPercent);
        }
        
        if (_trailImage != null)
        {
            _trailTween?.Kill();
            _trailImage.fillAmount = healthPercent;
        }
    }
    
    private Color GetHealthColor(float healthPercent)
    {
        if (healthPercent > 0.6f)
            return Color.Lerp(_damagedColor, _healthyColor, (healthPercent - 0.6f) / 0.4f);
        else if (healthPercent > 0.3f)
            return Color.Lerp(_criticalColor, _damagedColor, (healthPercent - 0.3f) / 0.3f);
        else
            return _criticalColor;
    }
    
    private void UpdatePosition()
    {
        if (_target != null)
        {
            transform.position = _target.position + Vector3.up * _heightOffset;
        }
    }
    
    #endregion
    
    #region Visibility
    
    public void Show()
    {
        if (_canvasGroup == null) return;
        
        _hideTimer = _showDuration;
        
        if (!_isVisible)
        {
            _isVisible = true;
            _fadeTween?.Kill();
            _fadeTween = _canvasGroup.DOFade(1f, _fadeInDuration);
        }
    }
    
    public void Hide()
    {
        if (_canvasGroup == null) return;
        
        _isVisible = false;
        _fadeTween?.Kill();
        _fadeTween = _canvasGroup.DOFade(0f, _fadeOutDuration);
    }
    
    public void SetAlwaysVisible(bool alwaysVisible)
    {
        if (alwaysVisible)
        {
            _showDuration = 0f;
            Show();
        }
        else
        {
            _showDuration = 5f;
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 강제 업데이트
    /// </summary>
    public void ForceUpdate()
    {
        if (_enemyHealth != null)
        {
            _lastHealthPercent = _enemyHealth.HealthPercent;
            UpdateHealthBarImmediate(_lastHealthPercent);
        }
    }
    
    /// <summary>
    /// 수동으로 피격 효과 트리거 (테스트용)
    /// </summary>
    public void TriggerHitEffect(float damagePercent = 0.1f)
    {
        float targetHealth = Mathf.Max(0f, _lastHealthPercent - damagePercent);
        ApplyHitImpact(targetHealth, damagePercent);
        _lastHealthPercent = targetHealth;
    }
    
    public void SetHeightOffset(float offset) => _heightOffset = offset;
    public void SetTrailDuration(float duration) => _trailSlideDuration = Mathf.Max(0.1f, duration);
    public void SetShakeIntensity(float intensity) => _shakeIntensity = intensity;
    public void SetFlashEnabled(bool enabled) => _enableFlash = enabled;
    public void SetShakeEnabled(bool enabled) => _enableShake = enabled;
    
    #endregion
}
