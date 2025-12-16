using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 타격감 있는 적 체력바 UI (DNF/소울라이크 스타일)
/// 
/// 핵심 기능:
/// - 메인 바: 피격 시 즉시 감소
/// - 트레일 바: 딜레이 후 부드럽게 슬라이드
/// - 데미지 플래시: 데미지 영역에만 표시되고 트레일과 함께 축소
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
    [SerializeField] private Color _flashColor = new Color(1f, 0.9f, 0.9f, 0.8f);
    [SerializeField] private float _flashDuration = 0.08f;
    [SerializeField] private float _flashFadeDuration = 0.15f;
    
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
    [SerializeField] private Color _trailColor = new Color(0.6f, 0.6f, 0.6f, 0.95f);
    
    #endregion
    
    #region Private Fields
    
    private Transform _target;
    private Camera _mainCamera;
    private EnemyHealth _enemyHealth;
    private float _hideTimer;
    private bool _isVisible;
    private float _lastHealthPercent = 1f;
    private Vector3 _originalShakePosition;
    
    // FlashOverlay용 RectTransform
    private RectTransform _flashOverlayRect;
    private RectTransform _fillParentRect;
    
    // 현재 데미지 영역 추적
    private float _currentDamageStart;
    private float _currentDamageEnd;
    
    // Tweens
    private Tween _trailTween;
    private Tween _fadeTween;
    private Tween _flashFadeTween;
    private Tween _flashSlideTween;
    private Tween _shakeTween;
    private Sequence _hitSequence;
    
    #endregion
    
    #region Unity Callbacks
    
#if UNITY_EDITOR
    private void OnValidate()
    {
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
            Transform t = transform.Find("FlashOverlay") ?? transform.Find("Flash") ?? transform.Find("DamageFlash");
            if (t != null)
            {
                _flashOverlay = t.GetComponent<Image>();
            }
        }
    }
    
    private void InitializeComponents()
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
        
        if (_fillImage != null)
        {
            _fillImage.type = Image.Type.Filled;
            _fillImage.fillMethod = Image.FillMethod.Horizontal;
            _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fillImage.fillAmount = 1f;
            
            _fillParentRect = _fillImage.transform.parent as RectTransform;
        }
        
        if (_trailImage != null)
        {
            _trailImage.type = Image.Type.Filled;
            _trailImage.fillMethod = Image.FillMethod.Horizontal;
            _trailImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            _trailImage.fillAmount = 1f;
            _trailImage.color = _trailColor;
        }
        
        // FlashOverlay 설정 - 런타임에 동적 생성
        if (_flashOverlay == null && Application.isPlaying)
        {
            CreateDamageFlashOverlay();
        }
        else if (_flashOverlay != null)
        {
            SetupFlashOverlay();
        }
        
        if (_shakeTarget != null)
            _originalShakePosition = _shakeTarget.anchoredPosition;
        
        _isVisible = false;
        _lastHealthPercent = 1f;
    }
    
    /// <summary>
    /// 데미지 영역 표시용 FlashOverlay 생성 (앵커 기반)
    /// </summary>
    private void CreateDamageFlashOverlay()
    {
        if (_fillParentRect == null && _fillImage != null)
        {
            _fillParentRect = _fillImage.transform.parent as RectTransform;
        }
        
        if (_fillParentRect == null)
        {
            Debug.LogWarning("[EnemyHealthBar] Fill 부모가 없어 FlashOverlay를 생성할 수 없습니다.");
            return;
        }
        
        GameObject flashObj = new GameObject("DamageFlash");
        flashObj.transform.SetParent(_fillParentRect, false);
        
        _flashOverlayRect = flashObj.AddComponent<RectTransform>();
        
        // 앵커 기반 스트레치 설정 (부모 기준 상대 위치)
        _flashOverlayRect.anchorMin = new Vector2(0f, 0f);
        _flashOverlayRect.anchorMax = new Vector2(0f, 1f);
        _flashOverlayRect.offsetMin = Vector2.zero;
        _flashOverlayRect.offsetMax = Vector2.zero;
        _flashOverlayRect.pivot = new Vector2(0f, 0.5f);
        
        _flashOverlay = flashObj.AddComponent<Image>();
        _flashOverlay.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, 0f);
        _flashOverlay.raycastTarget = false;
        
        // Fill과 Trail 사이에 배치 (Trail 위, Fill 아래)
        if (_trailImage != null)
        {
            flashObj.transform.SetSiblingIndex(_trailImage.transform.GetSiblingIndex() + 1);
        }
        
        Debug.Log($"[EnemyHealthBar] DamageFlash 동적 생성 완료: {transform.parent?.name}");
    }
    
    /// <summary>
    /// 기존 FlashOverlay를 앵커 기반으로 설정
    /// </summary>
    private void SetupFlashOverlay()
    {
        _flashOverlayRect = _flashOverlay.GetComponent<RectTransform>();
        
        if (_flashOverlayRect != null)
        {
            // 앵커 기반 스트레치 설정
            _flashOverlayRect.anchorMin = new Vector2(0f, 0f);
            _flashOverlayRect.anchorMax = new Vector2(0f, 1f);
            _flashOverlayRect.offsetMin = Vector2.zero;
            _flashOverlayRect.offsetMax = Vector2.zero;
            _flashOverlayRect.pivot = new Vector2(0f, 0.5f);
        }
        
        _flashOverlay.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, 0f);
        _flashOverlay.raycastTarget = false;
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
        _flashFadeTween?.Kill();
        _flashSlideTween?.Kill();
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
            ApplyHitImpact(healthPercent, damageAmount);
        }
        else if (healthPercent > _lastHealthPercent)
        {
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
        float previousHealth = targetHealthPercent + damageAmount;
        
        _hitSequence?.Kill();
        _hitSequence = DOTween.Sequence();
        
        // 1. 메인 바 즉시 감소
        if (_fillImage != null)
        {
            _fillImage.fillAmount = targetHealthPercent;
            _fillImage.color = GetHealthColor(targetHealthPercent);
        }
        
        // 2. 데미지 영역 플래시 (GIF 스타일)
        if (_enableFlash)
        {
            PlayDamageFlashEffect(targetHealthPercent, previousHealth, effectScale);
        }
        
        // 3. 떨림 효과
        if (_enableShake)
        {
            PlayShakeEffect(effectScale);
        }
        
        // 4. 트레일 바 딜레이 후 슬라이드 (플래시와 동기화)
        if (_trailImage != null && _trailImage.fillAmount > targetHealthPercent)
        {
            float delay = _trailDelay * (1f + effectScale * 0.5f);
            float duration = _trailSlideDuration;
            
            _trailTween?.Kill();
            _trailTween = _trailImage.DOFillAmount(targetHealthPercent, duration)
                .SetEase(_trailEaseType)
                .SetDelay(delay)
                .OnUpdate(() =>
                {
                    // 트레일 슬라이드와 플래시 영역 동기화
                    if (_flashOverlayRect != null && _flashOverlay.color.a > 0.01f)
                    {
                        UpdateFlashOverlayPosition(targetHealthPercent, _trailImage.fillAmount);
                    }
                })
                .OnComplete(() =>
                {
                    // 트레일 완료 시 플래시 완전 숨김
                    HideFlashOverlay();
                });
        }
    }
    
    private float CalculateEffectScale(float damageAmount)
    {
        if (!_scaledByDamage) return 1f;
        
        if (damageAmount < _minDamageThreshold) return 0.3f;
        
        float normalized = Mathf.InverseLerp(_minDamageThreshold, _maxDamageForFullEffect, damageAmount);
        return Mathf.Lerp(0.4f, 1f, normalized);
    }
    
    #endregion
    
    #region Damage Flash Effect (GIF Style)
    
    /// <summary>
    /// 데미지 영역에만 흰색 플래시 표시 (GIF 참고 스타일)
    /// - 현재 체력 ~ 이전 체력 사이에만 표시
    /// - 트레일과 함께 슬라이드하며 축소
    /// </summary>
    private void PlayDamageFlashEffect(float currentHealthPercent, float previousHealthPercent, float intensity)
    {
        if (_flashOverlay == null || _flashOverlayRect == null) return;
        
        _flashFadeTween?.Kill();
        _flashSlideTween?.Kill();
        
        _currentDamageStart = currentHealthPercent;
        _currentDamageEnd = previousHealthPercent;
        
        // 플래시 영역 설정 (데미지 영역만)
        UpdateFlashOverlayPosition(currentHealthPercent, previousHealthPercent);
        
        // 플래시 색상 설정 (즉시 표시)
        Color flashCol = _flashColor;
        flashCol.a = Mathf.Lerp(0.6f, 0.9f, intensity);
        _flashOverlay.color = flashCol;
        
        // 플래시 페이드 아웃 (트레일보다 먼저 완료되도록)
        float totalDuration = _trailDelay + _trailSlideDuration;
        float fadeDuration = totalDuration * 0.8f;
        
        _flashFadeTween = _flashOverlay.DOFade(0f, fadeDuration)
            .SetDelay(_flashDuration)
            .SetEase(Ease.OutQuad);
    }
    
    /// <summary>
    /// FlashOverlay 위치 업데이트 (앵커 기반)
    /// </summary>
    private void UpdateFlashOverlayPosition(float startPercent, float endPercent)
    {
        if (_flashOverlayRect == null) return;
        
        // 앵커 기반으로 데미지 영역 설정
        // anchorMin.x = 현재 체력 (왼쪽 경계)
        // anchorMax.x = 트레일 체력 (오른쪽 경계)
        _flashOverlayRect.anchorMin = new Vector2(startPercent, 0f);
        _flashOverlayRect.anchorMax = new Vector2(endPercent, 1f);
        _flashOverlayRect.offsetMin = Vector2.zero;
        _flashOverlayRect.offsetMax = Vector2.zero;
    }
    
    /// <summary>
    /// FlashOverlay 숨김
    /// </summary>
    private void HideFlashOverlay()
    {
        if (_flashOverlay == null) return;
        
        _flashFadeTween?.Kill();
        _flashOverlay.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, 0f);
    }
    
    #endregion
    
    #region Shake Effect
    
    private void PlayShakeEffect(float intensity)
    {
        if (_shakeTarget == null) return;
        
        _shakeTween?.Kill();
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
        
        HideFlashOverlay();
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
    
    public void ForceUpdate()
    {
        if (_enemyHealth != null)
        {
            _lastHealthPercent = _enemyHealth.HealthPercent;
            UpdateHealthBarImmediate(_lastHealthPercent);
        }
    }
    
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
