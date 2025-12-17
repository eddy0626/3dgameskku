using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// DNF 스타일 적 체력바 UI
///
/// 핵심 기능:
/// - 메인 바: 피격 시 즉시 감소
/// - 트레일 바: 딜레이 후 부드럽게 슬라이드
/// - 피격 플래시: 흰색 번쩍임
/// - 떨림 효과: 살짝 흔들림
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
    [SerializeField] private float _trailDelay = 0.3f;
    [SerializeField] private float _trailDuration = 0.5f;
    [SerializeField] private Ease _trailEaseType = Ease.OutQuad;
    [SerializeField] private Color _trailColor = new Color(0.8f, 0.3f, 0.1f, 1f);

    [Header("피격 플래시 설정")]
    [SerializeField] private bool _enableFlash = true;
    [SerializeField] private Color _flashColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private float _flashDuration = 0.1f;

    [Header("떨림 효과 설정")]
    [SerializeField] private bool _enableShake = true;
    [SerializeField] private float _shakeIntensity = 5f;
    [SerializeField] private float _shakeDuration = 0.15f;
    [SerializeField] private int _shakeVibrato = 15;

    [Header("위치/표시 설정")]
    [SerializeField] private float _heightOffset = 2.5f;
    [SerializeField] private float _showDuration = 5f;
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private float _fadeOutDuration = 0.5f;
    [SerializeField] private bool _billboard = true;

    [Header("체력바 색상")]
    [SerializeField] private Color _healthyColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color _damagedColor = new Color(0.8f, 0.8f, 0.2f);
    [SerializeField] private Color _criticalColor = new Color(0.8f, 0.2f, 0.2f);

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

    #endregion

    #region Unity Callbacks

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 에디터에서 참조 자동 할당
        AutoAssignReferences();
    }

    private void Reset()
    {
        // 컴포넌트 추가/리셋 시 참조 자동 할당
        AutoAssignReferences();
    }

    /// <summary>
    /// 에디터에서 참조 자동 할당 및 저장
    /// </summary>
    [ContextMenu("Auto Assign References")]
    private void AutoAssignReferences()
    {
        bool changed = false;

        // CanvasGroup
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup != null) changed = true;
        }

        // ShakeTarget (RectTransform)
        if (_shakeTarget == null)
        {
            _shakeTarget = GetComponent<RectTransform>();
            if (_shakeTarget != null) changed = true;
        }

        // Fill Image
        if (_fillImage == null)
        {
            Transform t = transform.Find("Fill");
            if (t != null)
            {
                _fillImage = t.GetComponent<Image>();
                if (_fillImage != null) changed = true;
            }
        }

        // Trail Image (DamageDelay 또는 Trail)
        if (_trailImage == null)
        {
            Transform t = transform.Find("DamageDelay");
            if (t == null) t = transform.Find("Trail");
            if (t != null)
            {
                _trailImage = t.GetComponent<Image>();
                if (_trailImage != null) changed = true;
            }
        }

        // Background Image
        if (_backgroundImage == null)
        {
            Transform t = transform.Find("Background");
            if (t != null)
            {
                _backgroundImage = t.GetComponent<Image>();
                if (_backgroundImage != null) changed = true;
            }
        }

        // Flash Overlay
        if (_flashOverlay == null)
        {
            Transform t = transform.Find("FlashOverlay");
            if (t == null) t = transform.Find("Flash");
            if (t != null)
            {
                _flashOverlay = t.GetComponent<Image>();
                if (_flashOverlay != null) changed = true;
            }
        }

        // 변경 사항이 있으면 에디터에 알림
        if (changed && !Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            Debug.Log($"[EnemyHealthBar] 참조 자동 할당 완료: {gameObject.name}");
        }
    }
#endif

    private void Awake()
    {
        // 런타임에서도 누락된 참조 찾기
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

    /// <summary>
    /// 런타임용 UI 요소 자동 탐색
    /// </summary>
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
            if (t != null) _flashOverlay = t.GetComponent<Image>();
        }
    }

    private void InitializeComponents()
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        // Fill Image 설정
        if (_fillImage != null)
        {
            _fillImage.type = Image.Type.Filled;
            _fillImage.fillMethod = Image.FillMethod.Horizontal;
            _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fillImage.fillAmount = 1f;
        }

        // Trail Image 설정
        if (_trailImage != null)
        {
            _trailImage.type = Image.Type.Filled;
            _trailImage.fillMethod = Image.FillMethod.Horizontal;
            _trailImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            _trailImage.fillAmount = 1f;
            _trailImage.color = _trailColor;
        }

        // Flash Overlay 설정 (없으면 동적 생성)
        if (_flashOverlay == null && Application.isPlaying)
        {
            CreateFlashOverlay();
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
    /// FlashOverlay 동적 생성
    /// </summary>
    private void CreateFlashOverlay()
    {
        Transform parent = _fillImage != null ? _fillImage.transform.parent : transform;

        GameObject flashObj = new GameObject("FlashOverlay");
        flashObj.transform.SetParent(parent, false);

        RectTransform flashRect = flashObj.AddComponent<RectTransform>();

        // 부모 전체를 덮도록 스트레치
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;

        _flashOverlay = flashObj.AddComponent<Image>();
        _flashOverlay.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, 0f);
        _flashOverlay.raycastTarget = false;

        // 가장 위에 배치
        flashObj.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 기존 FlashOverlay 설정
    /// </summary>
    private void SetupFlashOverlay()
    {
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
            // 데미지 받음 - DNF 스타일 애니메이션 적용
            ApplyDamageAnimation(healthPercent);
        }
        else if (healthPercent > _lastHealthPercent)
        {
            // 회복 - 즉시 업데이트
            UpdateHealthBarImmediate(healthPercent);
        }

        _lastHealthPercent = healthPercent;
    }

    private void OnEnemyDeath()
    {
        KillAllTweens();

        if (_canvasGroup != null)
        {
            _fadeTween = _canvasGroup.DOFade(0f, 0.3f)
                .OnComplete(() => gameObject.SetActive(false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    #endregion

    #region DNF Style Animation

    /// <summary>
    /// DNF 스타일 데미지 애니메이션 적용
    /// 메인바 즉시 감소 + 트레일바 딜레이 후 슬라이드
    /// </summary>
    private void ApplyDamageAnimation(float targetHealthPercent)
    {
        // 1. 메인 체력바 즉시 감소
        if (_fillImage != null)
        {
            _fillImage.fillAmount = targetHealthPercent;
            _fillImage.color = GetHealthColor(targetHealthPercent);
        }

        // 2. 피격 이펙트 (플래시 + 쉐이크 동시 실행)
        if (_enableFlash)
        {
            PlayFlashEffect();
        }

        if (_enableShake)
        {
            PlayShakeEffect();
        }

        // 3. 트레일바 딜레이 후 슬라이드
        if (_trailImage != null && _trailImage.fillAmount > targetHealthPercent)
        {
            _trailTween?.Kill();

            _trailTween = _trailImage
                .DOFillAmount(targetHealthPercent, _trailDuration)
                .SetDelay(_trailDelay)
                .SetEase(_trailEaseType);
        }
    }

    #endregion

    #region Hit Effects

    /// <summary>
    /// 흰색 플래시 효과 (0.1초)
    /// </summary>
    private void PlayFlashEffect()
    {
        if (_flashOverlay == null) return;

        _flashTween?.Kill();

        // 즉시 흰색으로 설정
        _flashOverlay.color = _flashColor;

        // 페이드 아웃
        _flashTween = _flashOverlay
            .DOFade(0f, _flashDuration)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 떨림 효과
    /// </summary>
    private void PlayShakeEffect()
    {
        if (_shakeTarget == null) return;

        _shakeTween?.Kill();
        _shakeTarget.anchoredPosition = _originalShakePosition;

        _shakeTween = _shakeTarget
            .DOShakeAnchorPos(_shakeDuration, _shakeIntensity, _shakeVibrato, 90f, false, true)
            .OnComplete(() => _shakeTarget.anchoredPosition = _originalShakePosition);
    }

    #endregion

    #region Health Bar Update

    /// <summary>
    /// 체력바 즉시 업데이트 (회복 또는 초기화 시)
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

    /// <summary>
    /// 체력 비율에 따른 색상 반환
    /// </summary>
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
    /// 테스트용 피격 효과 트리거
    /// </summary>
    public void TriggerHitEffect(float damagePercent = 0.1f)
    {
        float targetHealth = Mathf.Max(0f, _lastHealthPercent - damagePercent);
        ApplyDamageAnimation(targetHealth);
        _lastHealthPercent = targetHealth;
    }

    // Setter 메서드
    public void SetHeightOffset(float offset) => _heightOffset = offset;
    public void SetTrailDelay(float delay) => _trailDelay = Mathf.Max(0f, delay);
    public void SetTrailDuration(float duration) => _trailDuration = Mathf.Max(0.1f, duration);
    public void SetShakeIntensity(float intensity) => _shakeIntensity = intensity;
    public void SetFlashEnabled(bool enabled) => _enableFlash = enabled;
    public void SetShakeEnabled(bool enabled) => _enableShake = enabled;

    #endregion
}
