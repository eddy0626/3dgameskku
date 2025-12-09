using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StaminaBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private PlayerMove playerMove;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private Ease animationEase = Ease.OutQuad;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.18f, 0.55f, 0.34f);
    [SerializeField] private Color depletedColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private float depletedThreshold = 0.1f;

    [Header("Effects")]
    [SerializeField] private bool flashOnDepleted = true;
    [SerializeField] private float flashDuration = 0.5f;

    private Tween _fillTween;
    private Tween _colorTween;
    private Tween _flashTween;

void Start()
    {
        // fillImage 자동 탐색
        if (fillImage == null)
        {
            fillImage = GetComponentInChildren<Image>();
        }

        if (fillImage == null)
        {
            Debug.LogError($"[StaminaBarUI] fillImage가 할당되지 않았습니다. GameObject: {gameObject.name}", this);
            enabled = false;
            return;
        }

        if (playerMove == null)
        {
            playerMove = FindFirstObjectByType<PlayerMove>();
        }

        if (playerMove != null)
        {
            playerMove.OnStaminaChanged += HandleStaminaChanged;
            UpdateFillImmediate(playerMove.CurrentStamina / playerMove.maxStamina);
        }
        else
        {
            Debug.LogWarning($"[StaminaBarUI] PlayerMove를 찾을 수 없습니다. GameObject: {gameObject.name}", this);
        }
    }

    void OnDestroy()
    {
        if (playerMove != null)
        {
            playerMove.OnStaminaChanged -= HandleStaminaChanged;
        }

        _fillTween?.Kill();
        _colorTween?.Kill();
        _flashTween?.Kill();
    }

    private void HandleStaminaChanged(float current, float max)
    {
        float normalizedStamina = current / max;
        AnimateFill(normalizedStamina);
        UpdateStaminaColor(normalizedStamina);
    }

private void AnimateFill(float targetFill)
    {
        if (fillImage == null) return;

        _fillTween?.Kill();
        _fillTween = fillImage.DOFillAmount(targetFill, animationDuration)
            .SetEase(animationEase);
    }

private void UpdateFillImmediate(float fill)
    {
        if (fillImage == null) return;

        fillImage.fillAmount = fill;
        UpdateStaminaColor(fill);
    }

private void UpdateStaminaColor(float normalizedStamina)
    {
        if (fillImage == null) return;

        if (normalizedStamina <= depletedThreshold)
        {
            _colorTween?.Kill();
            _colorTween = fillImage.DOColor(depletedColor, animationDuration * 0.5f);

            if (flashOnDepleted && _flashTween == null)
            {
                _flashTween = fillImage.DOFade(0.5f, flashDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }
        else
        {
            _flashTween?.Kill();
            _flashTween = null;
            
            Color currentColor = fillImage.color;
            currentColor.a = 1f;
            fillImage.color = currentColor;

            _colorTween?.Kill();
            _colorTween = fillImage.DOColor(normalColor, animationDuration * 0.5f);
        }
    }
}
