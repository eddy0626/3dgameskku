using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 스테미너바 UI 컨트롤러
/// - 사용/회복 시 다른 애니메이션
/// - 고갈 시 경고 효과
/// - 자연스러운 Ease 곡선 애니메이션
/// </summary>
public class StaminaBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Image recoveryDelayImage;  // 회복 예측 표시용
    [SerializeField] private PlayerMove playerMove;

    [Header("Animation Settings")]
    [SerializeField] private float drainDuration = 0.15f;      // 감소 애니메이션 (빠르게)
    [SerializeField] private float recoverDuration = 0.3f;     // 회복 애니메이션 (부드럽게)
    [SerializeField] private Ease drainEase = Ease.OutQuart;
    [SerializeField] private Ease recoverEase = Ease.OutSine;

    [Header("Color Settings")]
    [SerializeField] private Color fullStaminaColor = new Color(0.18f, 0.75f, 0.45f);    // 밝은 녹색
    [SerializeField] private Color midStaminaColor = new Color(0.18f, 0.55f, 0.34f);     // 녹색
    [SerializeField] private Color lowStaminaColor = new Color(0.7f, 0.6f, 0.2f);        // 노란색
    [SerializeField] private Color depletedColor = new Color(0.5f, 0.3f, 0.3f);          // 어두운 빨강
    [SerializeField] private Color recoveryPreviewColor = new Color(0.3f, 0.7f, 0.5f, 0.3f); // 회복 예측 색상
    
    [Header("Threshold Settings")]
    [SerializeField] private float lowStaminaThreshold = 0.3f;
    [SerializeField] private float depletedThreshold = 0.1f;

    [Header("Effects")]
    [SerializeField] private bool enableDepletedFlash = true;
    [SerializeField] private float flashDuration = 0.4f;
    [SerializeField] private float flashMinAlpha = 0.5f;
    [SerializeField] private bool enableRecoveryPulse = true;
    [SerializeField] private float recoveryPulseDuration = 0.3f;

    private float _previousStamina = 1f;
    private Tween _fillTween;
    private Tween _recoveryTween;
    private Tween _colorTween;
    private Tween _flashTween;
    private Tween _recoveryPulseTween;
    private bool _isRecovering;

    void Start()
    {
        InitializeComponents();
        
        if (fillImage == null)
        {
            Debug.LogError($"[StaminaBarUI] fillImage가 할당되지 않았습니다. GameObject: {gameObject.name}", this);
            enabled = false;
            return;
        }

        SetupFillImage(fillImage);
        if (recoveryDelayImage != null)
        {
            SetupFillImage(recoveryDelayImage);
            recoveryDelayImage.color = recoveryPreviewColor;
        }

        ConnectToPlayerMove();
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

        // recoveryDelayImage 자동 탐색
        if (recoveryDelayImage == null)
        {
            Transform delayTransform = transform.Find("RecoveryDelay");
            if (delayTransform != null)
                recoveryDelayImage = delayTransform.GetComponent<Image>();
        }

        // PlayerMove 자동 탐색
        if (playerMove == null)
            playerMove = FindFirstObjectByType<PlayerMove>();
    }

    private void SetupFillImage(Image image)
    {
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private void ConnectToPlayerMove()
    {
        if (playerMove != null)
        {
            playerMove.OnStaminaChanged += HandleStaminaChanged;
            float initialStamina = playerMove.CurrentStamina / playerMove.maxStamina;
            _previousStamina = initialStamina;
            UpdateFillImmediate(initialStamina);
        }
        else
        {
            Debug.LogWarning($"[StaminaBarUI] PlayerMove를 찾을 수 없습니다. GameObject: {gameObject.name}", this);
        }
    }

    void OnDestroy()
    {
        if (playerMove != null)
            playerMove.OnStaminaChanged -= HandleStaminaChanged;

        KillAllTweens();
    }

    private void KillAllTweens()
    {
        _fillTween?.Kill();
        _recoveryTween?.Kill();
        _colorTween?.Kill();
        _flashTween?.Kill();
        _recoveryPulseTween?.Kill();
    }

    private void HandleStaminaChanged(float current, float max)
    {
        float normalizedStamina = current / max;
        bool isDraining = normalizedStamina < _previousStamina;
        
        if (isDraining)
        {
            _isRecovering = false;
            AnimateDrain(normalizedStamina);
        }
        else
        {
            _isRecovering = true;
            AnimateRecovery(normalizedStamina);
        }
        
        UpdateStaminaColor(normalizedStamina);
        UpdateDepletedEffects(normalizedStamina);
        
        _previousStamina = normalizedStamina;
    }

    /// <summary>
    /// 스테미너 사용 시 애니메이션: 빠르게 감소
    /// </summary>
    private void AnimateDrain(float targetFill)
    {
        if (fillImage == null) return;

        _fillTween?.Kill();
        _recoveryTween?.Kill();
        _recoveryPulseTween?.Kill();

        // 메인바 빠르게 감소
        _fillTween = fillImage.DOFillAmount(targetFill, drainDuration)
            .SetEase(drainEase);

        // 회복 예측 바 숨기기
        if (recoveryDelayImage != null)
        {
            _recoveryTween = recoveryDelayImage.DOFillAmount(targetFill, drainDuration * 0.5f)
                .SetEase(Ease.Linear);
        }
    }

    /// <summary>
    /// 스테미너 회복 시 애니메이션: 부드럽게 증가
    /// </summary>
    private void AnimateRecovery(float targetFill)
    {
        if (fillImage == null) return;

        _fillTween?.Kill();
        _recoveryTween?.Kill();

        // 메인바 부드럽게 증가
        _fillTween = fillImage.DOFillAmount(targetFill, recoverDuration)
            .SetEase(recoverEase);

        // 회복 예측 바 (메인바보다 살짝 앞서 보여줌)
        if (recoveryDelayImage != null && targetFill < 1f)
        {
            float previewFill = Mathf.Min(targetFill + 0.15f, 1f);
            recoveryDelayImage.fillAmount = previewFill;
        }

        // 회복 중 펄스 효과
        if (enableRecoveryPulse && (_recoveryPulseTween == null || !_recoveryPulseTween.IsActive()))
        {
            _recoveryPulseTween = fillImage.transform.DOPunchScale(Vector3.one * 0.03f, recoveryPulseDuration, 1, 0.5f)
                .SetLoops(-1, LoopType.Restart);
        }
    }

    private void UpdateFillImmediate(float fill)
    {
        if (fillImage == null) return;

        fillImage.fillAmount = fill;
        if (recoveryDelayImage != null)
            recoveryDelayImage.fillAmount = fill;

        UpdateStaminaColor(fill);
    }

    /// <summary>
    /// 스테미너에 따른 색상 변화
    /// </summary>
    private void UpdateStaminaColor(float normalizedStamina)
    {
        if (fillImage == null) return;

        Color targetColor;
        
        if (normalizedStamina <= depletedThreshold)
        {
            targetColor = depletedColor;
        }
        else if (normalizedStamina <= lowStaminaThreshold)
        {
            // 노랑-고갈색 보간
            float t = Mathf.InverseLerp(depletedThreshold, lowStaminaThreshold, normalizedStamina);
            targetColor = Color.Lerp(depletedColor, lowStaminaColor, t);
        }
        else if (normalizedStamina <= 0.7f)
        {
            // 녹색-노랑 보간
            float t = Mathf.InverseLerp(lowStaminaThreshold, 0.7f, normalizedStamina);
            targetColor = Color.Lerp(lowStaminaColor, midStaminaColor, t);
        }
        else
        {
            // 밝은녹색-녹색 보간
            float t = Mathf.InverseLerp(0.7f, 1f, normalizedStamina);
            targetColor = Color.Lerp(midStaminaColor, fullStaminaColor, t);
        }

        _colorTween?.Kill();
        _colorTween = fillImage.DOColor(targetColor, drainDuration)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 고갈 상태 효과
    /// </summary>
    private void UpdateDepletedEffects(float normalizedStamina)
    {
        if (normalizedStamina <= depletedThreshold)
        {
            // 회복 펄스 중지
            if (_recoveryPulseTween != null)
            {
                _recoveryPulseTween.Kill();
                _recoveryPulseTween = null;
                fillImage.transform.localScale = Vector3.one;
            }

            // 고갈 시 깜빡임
            if (enableDepletedFlash && (_flashTween == null || !_flashTween.IsActive()))
            {
                _flashTween = fillImage.DOFade(flashMinAlpha, flashDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }
        else
        {
            // 깜빡임 중지
            if (_flashTween != null && _flashTween.IsActive())
            {
                _flashTween.Kill();
                _flashTween = null;
                
                Color c = fillImage.color;
                c.a = 1f;
                fillImage.color = c;
            }

            // 회복 중이 아닐 때 펄스도 중지
            if (!_isRecovering && _recoveryPulseTween != null)
            {
                _recoveryPulseTween.Kill();
                _recoveryPulseTween = null;
                fillImage.transform.localScale = Vector3.one;
            }
        }
    }
}