using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 체력바 UI 컨트롤러
/// - 딜레이 감소 효과 (Ghost Bar)
/// - 체력 낮을 때 펄스/색상 변화
/// - 자연스러운 Ease 곡선 애니메이션
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Image damageDelayImage;  // 딜레이 감소 효과용 (Ghost Bar)
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Animation Settings")]
    [SerializeField] private float fillDuration = 0.25f;
    [SerializeField] private Ease fillEase = Ease.OutQuart;
    
    [Header("Delay Effect Settings")]
    [SerializeField] private float delayBeforeGhostFade = 0.4f;  // 딜레이 시작 전 대기
    [SerializeField] private float ghostFadeDuration = 0.6f;     // Ghost Bar 감소 시간
    [SerializeField] private Ease ghostEase = Ease.InOutSine;

    [Header("Color Settings")]
    [SerializeField] private Color fullHealthColor = new Color(0.2f, 0.8f, 0.3f);    // 녹색
    [SerializeField] private Color midHealthColor = new Color(0.9f, 0.7f, 0.2f);     // 노란색
    [SerializeField] private Color lowHealthColor = new Color(0.85f, 0.2f, 0.2f);    // 빨간색
    [SerializeField] private Color criticalHealthColor = new Color(0.6f, 0.1f, 0.1f); // 진한 빨강
    [SerializeField] private Color ghostBarColor = new Color(1f, 1f, 1f, 0.5f);      // Ghost Bar 색상
    
    [Header("Threshold Settings")]
    [SerializeField] private float midHealthThreshold = 0.6f;
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [SerializeField] private float criticalHealthThreshold = 0.15f;

    [Header("Critical Health Effects")]
    [SerializeField] private bool enablePulseEffect = true;
    [SerializeField] private float pulseMinAlpha = 0.6f;
    [SerializeField] private float pulseDuration = 0.5f;
    [SerializeField] private bool enableShakeEffect = true;
    [SerializeField] private float shakeStrength = 3f;
    [SerializeField] private int shakeVibrato = 15;

    private float _previousHealth = 1f;
    private Tween _fillTween;
    private Tween _ghostTween;
    private Tween _colorTween;
    private Tween _pulseTween;
    private Sequence _damageSequence;

    void Start()
    {
        InitializeComponents();
        
        if (fillImage == null)
        {
            Debug.LogError($"[HealthBarUI] fillImage가 할당되지 않았습니다. GameObject: {gameObject.name}", this);
            enabled = false;
            return;
        }

        SetupFillImage(fillImage);
        if (damageDelayImage != null)
        {
            SetupFillImage(damageDelayImage);
            damageDelayImage.color = ghostBarColor;
        }

        ConnectToPlayerHealth();
    }

    private void InitializeComponents()
    {
        // fillImage 자동 탐색
        if (fillImage == null)
        {
            Transform fillTransform = transform.Find("Fill");
            if (fillTransform != null)
                fillImage = fillTransform.GetComponent<Image>();
        }

        // damageDelayImage 자동 탐색
        if (damageDelayImage == null)
        {
            Transform delayTransform = transform.Find("DamageDelay");
            if (delayTransform != null)
                damageDelayImage = delayTransform.GetComponent<Image>();
        }

        // PlayerHealth 자동 탐색
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void SetupFillImage(Image image)
    {
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private void ConnectToPlayerHealth()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            float initialHealth = playerHealth.CurrentHealth / playerHealth.MaxHealth;
            _previousHealth = initialHealth;
            UpdateFillImmediate(initialHealth);
        }
        else
        {
            Debug.LogWarning($"[HealthBarUI] PlayerHealth를 찾을 수 없습니다. GameObject: {gameObject.name}", this);
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= HandleHealthChanged;

        KillAllTweens();
    }

    private void KillAllTweens()
    {
        _fillTween?.Kill();
        _ghostTween?.Kill();
        _colorTween?.Kill();
        _pulseTween?.Kill();
        _damageSequence?.Kill();
    }

    private void HandleHealthChanged(float current, float max)
    {
        float normalizedHealth = current / max;
        bool isDamage = normalizedHealth < _previousHealth;
        
        if (isDamage)
        {
            AnimateDamage(normalizedHealth);
        }
        else
        {
            AnimateHeal(normalizedHealth);
        }
        
        UpdateHealthColor(normalizedHealth);
        UpdateCriticalEffects(normalizedHealth);
        
        _previousHealth = normalizedHealth;
    }

    /// <summary>
    /// 데미지 시 애니메이션: 메인바 즉시 감소 → Ghost Bar 딜레이 후 감소
    /// </summary>
    private void AnimateDamage(float targetFill)
    {
        if (fillImage == null) return;

        // 진행 중인 시퀀스 취소
        _damageSequence?.Kill();
        _fillTween?.Kill();
        _ghostTween?.Kill();

        // Ghost Bar는 현재 위치 유지
        if (damageDelayImage != null)
        {
            damageDelayImage.fillAmount = _previousHealth;
        }

        // 메인 체력바 즉시 감소
        _fillTween = fillImage.DOFillAmount(targetFill, fillDuration)
            .SetEase(fillEase);

        // Ghost Bar 딜레이 후 감소 (Sequence 사용)
        if (damageDelayImage != null)
        {
            _damageSequence = DOTween.Sequence();
            _damageSequence.AppendInterval(delayBeforeGhostFade);
            _damageSequence.Append(
                damageDelayImage.DOFillAmount(targetFill, ghostFadeDuration)
                    .SetEase(ghostEase)
            );
        }

        // 데미지 시 흔들림 효과
        if (enableShakeEffect && targetFill <= lowHealthThreshold)
        {
            transform.DOShakePosition(0.15f, shakeStrength, shakeVibrato);
        }
    }

    /// <summary>
    /// 힐 시 애니메이션: 메인바와 Ghost Bar 동시에 증가
    /// </summary>
    private void AnimateHeal(float targetFill)
    {
        if (fillImage == null) return;

        _damageSequence?.Kill();
        _fillTween?.Kill();
        _ghostTween?.Kill();

        // 힐 시에는 OutBack으로 약간 튀어오르는 느낌
        _fillTween = fillImage.DOFillAmount(targetFill, fillDuration)
            .SetEase(Ease.OutBack);

        // Ghost Bar도 함께 증가
        if (damageDelayImage != null)
        {
            _ghostTween = damageDelayImage.DOFillAmount(targetFill, fillDuration * 0.5f)
                .SetEase(Ease.OutQuad);
        }
    }

    private void UpdateFillImmediate(float fill)
    {
        if (fillImage == null) return;

        fillImage.fillAmount = fill;
        if (damageDelayImage != null)
            damageDelayImage.fillAmount = fill;

        UpdateHealthColor(fill);
    }

    /// <summary>
    /// 체력에 따른 그라데이션 색상 변화
    /// </summary>
    private void UpdateHealthColor(float normalizedHealth)
    {
        if (fillImage == null) return;

        Color targetColor;
        
        if (normalizedHealth <= criticalHealthThreshold)
        {
            targetColor = criticalHealthColor;
        }
        else if (normalizedHealth <= lowHealthThreshold)
        {
            // 빨강-진한빨강 보간
            float t = Mathf.InverseLerp(criticalHealthThreshold, lowHealthThreshold, normalizedHealth);
            targetColor = Color.Lerp(criticalHealthColor, lowHealthColor, t);
        }
        else if (normalizedHealth <= midHealthThreshold)
        {
            // 노랑-빨강 보간
            float t = Mathf.InverseLerp(lowHealthThreshold, midHealthThreshold, normalizedHealth);
            targetColor = Color.Lerp(lowHealthColor, midHealthColor, t);
        }
        else
        {
            // 녹색-노랑 보간
            float t = Mathf.InverseLerp(midHealthThreshold, 1f, normalizedHealth);
            targetColor = Color.Lerp(midHealthColor, fullHealthColor, t);
        }

        _colorTween?.Kill();
        _colorTween = fillImage.DOColor(targetColor, fillDuration * 0.8f)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 위험 상태일 때 펄스 효과
    /// </summary>
    private void UpdateCriticalEffects(float normalizedHealth)
    {
        if (!enablePulseEffect) return;

        if (normalizedHealth <= criticalHealthThreshold)
        {
            // 펄스 시작
            if (_pulseTween == null || !_pulseTween.IsActive())
            {
                _pulseTween = fillImage.DOFade(pulseMinAlpha, pulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }
        else
        {
            // 펄스 중지 및 알파 복원
            if (_pulseTween != null && _pulseTween.IsActive())
            {
                _pulseTween.Kill();
                _pulseTween = null;
                
                Color c = fillImage.color;
                c.a = 1f;
                fillImage.color = c;
            }
        }
    }
}