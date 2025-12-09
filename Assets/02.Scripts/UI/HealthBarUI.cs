using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private Ease animationEase = Ease.OutQuad;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.77f, 0.19f, 0.19f);
    [SerializeField] private Color lowHealthColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float lowHealthThreshold = 0.3f;

    private Tween _fillTween;
    private Tween _colorTween;

void Start()
    {
        // fillImage 자동 탐색
        if (fillImage == null)
        {
            fillImage = GetComponentInChildren<Image>();
        }

        if (fillImage == null)
        {
            Debug.LogError($"[HealthBarUI] fillImage가 할당되지 않았습니다. GameObject: {gameObject.name}", this);
            enabled = false;
            return;
        }

        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            UpdateFillImmediate(playerHealth.CurrentHealth / playerHealth.MaxHealth);
        }
        else
        {
            Debug.LogWarning($"[HealthBarUI] PlayerHealth를 찾을 수 없습니다. GameObject: {gameObject.name}", this);
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
        }

        _fillTween?.Kill();
        _colorTween?.Kill();
    }

    private void HandleHealthChanged(float current, float max)
    {
        float normalizedHealth = current / max;
        AnimateFill(normalizedHealth);
        UpdateHealthColor(normalizedHealth);
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
        UpdateHealthColor(fill);
    }

private void UpdateHealthColor(float normalizedHealth)
    {
        if (fillImage == null) return;

        Color targetColor = normalizedHealth <= lowHealthThreshold ? lowHealthColor : normalColor;
        
        _colorTween?.Kill();
        _colorTween = fillImage.DOColor(targetColor, animationDuration * 0.5f);

        if (normalizedHealth <= lowHealthThreshold)
        {
            fillImage.transform.DOShakePosition(0.2f, 3f, 20);
        }
    }
}
