using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 적 머리 위에 표시되는 월드 스페이스 체력바 UI
/// 카메라를 향해 빌보드 처리됨
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    #region Inspector Fields
    
    [Header("UI 참조")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private Image _damageDelayImage;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private CanvasGroup _canvasGroup;
    
    [Header("설정")]
    [SerializeField] private float _heightOffset = 2.5f;
    [SerializeField] private float _damageDelayTime = 0.5f;
    [SerializeField] private float _damageDelayDuration = 0.3f;
    [SerializeField] private float _showDuration = 5f;
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private float _fadeOutDuration = 0.5f;
    
    [Header("색상")]
    [SerializeField] private Color _healthyColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color _damagedColor = new Color(0.8f, 0.8f, 0.2f);
    [SerializeField] private Color _criticalColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color _damageDelayColor = new Color(1f, 0.5f, 0f, 0.8f);
    
    [Header("빌보드")]
    [SerializeField] private bool _billboard = true;
    
    #endregion
    
    #region Private Fields
    
    private Transform _target;
    private Camera _mainCamera;
    private EnemyHealth _enemyHealth;
    private float _hideTimer;
    private bool _isVisible;
    private Tween _delayTween;
    private Tween _fadeTween;
    
    #endregion
    
    #region Unity Callbacks
    
private void Awake()
    {
        // 자동으로 자식 UI 요소 찾기
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
        
        if (_fillImage == null)
        {
            Transform fillTransform = transform.Find("Fill");
            if (fillTransform != null)
            {
                _fillImage = fillTransform.GetComponent<Image>();
            }
        }
        
        if (_damageDelayImage == null)
        {
            Transform damageDelayTransform = transform.Find("DamageDelay");
            if (damageDelayTransform != null)
            {
                _damageDelayImage = damageDelayTransform.GetComponent<Image>();
            }
        }
        
        if (_backgroundImage == null)
        {
            Transform backgroundTransform = transform.Find("Background");
            if (backgroundTransform != null)
            {
                _backgroundImage = backgroundTransform.GetComponent<Image>();
            }
        }
        
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }
        
        _isVisible = false;
    }
    
private void Start()
    {
        _mainCamera = Camera.main;
        
        // 자동 초기화: Initialize가 호출되지 않은 경우 부모에서 EnemyHealth 찾기
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
        _delayTween?.Kill();
        _fadeTween?.Kill();
    }
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// 체력바 초기화
    /// </summary>
    public void Initialize(Transform target, EnemyHealth enemyHealth)
    {
        _target = target;
        _enemyHealth = enemyHealth;
        
        if (_enemyHealth != null)
        {
            _enemyHealth.OnHealthChanged += OnHealthChanged;
            _enemyHealth.OnDamaged += OnEnemyDamaged;
            _enemyHealth.OnDeath += OnEnemyDeath;
        }
        
        UpdateHealthBar(1f);
        UpdatePosition();
    }


    /// <summary>
    /// 자동 초기화 - 부모 오브젝트에서 EnemyHealth 찾기
    /// </summary>
private void AutoInitialize()
    {
        // 부모에서 EnemyHealth 찾기
        Transform parent = transform.parent;
        if (parent != null)
        {
            EnemyHealth enemyHealth = parent.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = parent.GetComponentInParent<EnemyHealth>();
            }
            
            if (enemyHealth != null)
            {
                Initialize(parent, enemyHealth);
                Debug.Log($"[EnemyHealthBar] Auto-initialized for {parent.name}");
                
                // 자동 초기화 시 항상 표시
                SetAlwaysVisible(true);
            }
            else
            {
                Debug.LogWarning($"[EnemyHealthBar] Could not find EnemyHealth on parent: {parent.name}");
            }
        }
    }

    
    /// <summary>
    /// 정리
    /// </summary>
    public void Cleanup()
    {
        if (_enemyHealth != null)
        {
            _enemyHealth.OnHealthChanged -= OnHealthChanged;
            _enemyHealth.OnDamaged -= OnEnemyDamaged;
            _enemyHealth.OnDeath -= OnEnemyDeath;
        }
        
        _delayTween?.Kill();
        _fadeTween?.Kill();
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
        bool isDamage = _fillImage != null && healthPercent < _fillImage.fillAmount;
        
        Debug.Log($"[EnemyHealthBar] OnHealthChanged - HP: {currentHealth}/{maxHealth} = {healthPercent:P0}, FillImage: {(_fillImage != null ? "OK" : "NULL")}, DamageDelayImage: {(_damageDelayImage != null ? "OK" : "NULL")}");
        
        UpdateHealthBar(healthPercent, isDamage);
    }
    
    private void OnEnemyDeath()
    {
        _fadeTween?.Kill();
        _fadeTween = _canvasGroup.DOFade(0f, 0.2f)
            .OnComplete(() => gameObject.SetActive(false));
    }
    
    #endregion
    
    #region UI Updates
    
private void UpdateHealthBar(float healthPercent, bool withDamageDelay = false)
    {
        healthPercent = Mathf.Clamp01(healthPercent);
        
        if (_fillImage != null)
        {
            float oldFill = _fillImage.fillAmount;
            _fillImage.fillAmount = healthPercent;
            _fillImage.color = GetHealthColor(healthPercent);
            Debug.Log($"[EnemyHealthBar] UpdateHealthBar - FillAmount: {oldFill:F2} -> {healthPercent:F2}, withDelay: {withDamageDelay}");
        }
        
        if (withDamageDelay && _damageDelayImage != null)
        {
            _delayTween?.Kill();
            
            _delayTween = DOVirtual.DelayedCall(_damageDelayTime, () =>
            {
                Debug.Log($"[EnemyHealthBar] DamageDelay animation started - Target: {healthPercent:F2}");
                _damageDelayImage.DOFillAmount(healthPercent, _damageDelayDuration)
                    .SetEase(Ease.OutQuad);
            });
        }
        else if (_damageDelayImage != null)
        {
            _damageDelayImage.fillAmount = healthPercent;
        }
    }
    
    private Color GetHealthColor(float healthPercent)
    {
        if (healthPercent > 0.6f)
        {
            return Color.Lerp(_damagedColor, _healthyColor, (healthPercent - 0.6f) / 0.4f);
        }
        else if (healthPercent > 0.3f)
        {
            return Color.Lerp(_criticalColor, _damagedColor, (healthPercent - 0.3f) / 0.3f);
        }
        else
        {
            return _criticalColor;
        }
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
            UpdateHealthBar(_enemyHealth.HealthPercent);
        }
    }
    
    public void SetHeightOffset(float offset)
    {
        _heightOffset = offset;
    }
    
    #endregion
}
