using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 코인 UI 컴포넌트
/// - ResourceManager.OnCoinsChanged 이벤트 구독
/// - DOTween으로 숫자 증가 애니메이션
/// - 코인 획득/사용 시 시각 피드백
/// </summary>
public class CoinUI : MonoBehaviour
{
    #region Inspector Fields
    [Header("UI 참조")]
    [Tooltip("코인 수량 텍스트")]
    [SerializeField] private TextMeshProUGUI _coinText;

    [Tooltip("코인 아이콘 이미지")]
    [SerializeField] private Image _coinIcon;

    [Tooltip("변화량 표시 텍스트 (옵션)")]
    [SerializeField] private TextMeshProUGUI _changeText;

    [Header("애니메이션 설정")]
    [Tooltip("숫자 카운트 애니메이션 시간")]
    [SerializeField] private float _countDuration = 0.5f;

    [Tooltip("펀치 스케일 크기")]
    [SerializeField] private float _punchScale = 0.2f;

    [Tooltip("펀치 애니메이션 시간")]
    [SerializeField] private float _punchDuration = 0.3f;

    [Header("색상 설정")]
    [Tooltip("기본 텍스트 색상")]
    [SerializeField] private Color _normalColor = Color.white;

    [Tooltip("코인 증가 시 색상")]
    [SerializeField] private Color _addColor = new Color(0.3f, 1f, 0.3f);

    [Tooltip("코인 감소 시 색상")]
    [SerializeField] private Color _subtractColor = new Color(1f, 0.3f, 0.3f);

    [Tooltip("코인 부족 시 색상")]
    [SerializeField] private Color _insufficientColor = new Color(1f, 0.5f, 0f);

    [Header("변화량 표시 설정")]
    [Tooltip("변화량 표시 시간")]
    [SerializeField] private float _changeDisplayDuration = 1.5f;

    [Tooltip("변화량 이동 거리 (위로)")]
    [SerializeField] private float _changeFloatDistance = 30f;

    [Header("포맷 설정")]
    [Tooltip("천 단위 구분 기호 사용")]
    [SerializeField] private bool _useThousandSeparator = true;

    [Tooltip("약어 사용 (1K, 1M 등)")]
    [SerializeField] private bool _useAbbreviation = false;
    #endregion

    #region Private Fields
    private int _displayedCoins;
    private int _targetCoins;
    private Tween _countTween;
    private Tween _colorTween;
    private Tween _scaleTween;
    private Vector3 _originalScale;
    private Vector3 _originalChangePosition;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 초기 스케일 저장
        if (_coinText != null)
        {
            _originalScale = _coinText.transform.localScale;
        }

        // 변화량 텍스트 위치 저장
        if (_changeText != null)
        {
            _originalChangePosition = _changeText.rectTransform.anchoredPosition;
            _changeText.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // ResourceManager 이벤트 구독
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnCoinsChanged += OnCoinsChanged;
            ResourceManager.Instance.OnCoinsAdded += OnCoinsAdded;
            ResourceManager.Instance.OnCoinsSpent += OnCoinsSpent;
            ResourceManager.Instance.OnResourceInsufficient += OnResourceInsufficient;

            // 초기값 설정
            _displayedCoins = ResourceManager.Instance.Coins;
            _targetCoins = _displayedCoins;
            UpdateCoinText(_displayedCoins);
        }
        else
        {
            Debug.LogWarning("[CoinUI] ResourceManager를 찾을 수 없습니다!");
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnCoinsChanged -= OnCoinsChanged;
            ResourceManager.Instance.OnCoinsAdded -= OnCoinsAdded;
            ResourceManager.Instance.OnCoinsSpent -= OnCoinsSpent;
            ResourceManager.Instance.OnResourceInsufficient -= OnResourceInsufficient;
        }

        // Tween 정리
        KillAllTweens();
    }
    #endregion

    #region Event Handlers
    private void OnCoinsChanged(int newAmount)
    {
        _targetCoins = newAmount;
        AnimateCoinCount(_displayedCoins, newAmount);
    }

    private void OnCoinsAdded(int addedAmount)
    {
        // 증가 피드백
        PlayAddFeedback();
        ShowChangeText($"+{FormatNumber(addedAmount)}", _addColor);
    }

    private void OnCoinsSpent(int spentAmount)
    {
        // 감소 피드백
        PlaySpendFeedback();
        ShowChangeText($"-{FormatNumber(spentAmount)}", _subtractColor);
    }

    private void OnResourceInsufficient(ResourceType type, int needed)
    {
        if (type == ResourceType.Gold)
        {
            // 코인 부족 피드백
            PlayInsufficientFeedback();
        }
    }
    #endregion

    #region Animation
    /// <summary>
    /// 코인 카운트 애니메이션
    /// </summary>
    private void AnimateCoinCount(int from, int to)
    {
        // 기존 Tween 정리
        _countTween?.Kill();

        // 숫자 카운트 애니메이션
        _countTween = DOTween.To(
            () => _displayedCoins,
            x =>
            {
                _displayedCoins = x;
                UpdateCoinText(_displayedCoins);
            },
            to,
            _countDuration
        ).SetEase(Ease.OutQuad)
         .SetUpdate(true); // TimeScale 영향 없이 동작
    }

    /// <summary>
    /// 코인 획득 피드백
    /// </summary>
    private void PlayAddFeedback()
    {
        if (_coinText == null) return;

        // 스케일 펀치
        _scaleTween?.Kill();
        _coinText.transform.localScale = _originalScale;
        _scaleTween = _coinText.transform.DOPunchScale(
            Vector3.one * _punchScale,
            _punchDuration,
            5,
            0.5f
        ).SetUpdate(true);

        // 색상 변경
        _colorTween?.Kill();
        _coinText.color = _addColor;
        _colorTween = _coinText.DOColor(_normalColor, _punchDuration * 2f)
            .SetUpdate(true);

        // 아이콘 애니메이션
        if (_coinIcon != null)
        {
            _coinIcon.transform.DOPunchRotation(new Vector3(0, 0, 15f), _punchDuration, 5, 0.5f)
                .SetUpdate(true);
        }
    }

    /// <summary>
    /// 코인 사용 피드백
    /// </summary>
    private void PlaySpendFeedback()
    {
        if (_coinText == null) return;

        // 스케일 펀치 (작게)
        _scaleTween?.Kill();
        _coinText.transform.localScale = _originalScale;
        _scaleTween = _coinText.transform.DOPunchScale(
            Vector3.one * -_punchScale * 0.5f,
            _punchDuration,
            3,
            0.5f
        ).SetUpdate(true);

        // 색상 변경
        _colorTween?.Kill();
        _coinText.color = _subtractColor;
        _colorTween = _coinText.DOColor(_normalColor, _punchDuration * 2f)
            .SetUpdate(true);
    }

    /// <summary>
    /// 코인 부족 피드백
    /// </summary>
    private void PlayInsufficientFeedback()
    {
        if (_coinText == null) return;

        // 흔들기 애니메이션
        _coinText.transform.DOShakePosition(_punchDuration, 5f, 20, 90f, false, true)
            .SetUpdate(true);

        // 색상 깜빡임
        Sequence colorSeq = DOTween.Sequence();
        colorSeq.Append(_coinText.DOColor(_insufficientColor, 0.1f));
        colorSeq.Append(_coinText.DOColor(_normalColor, 0.1f));
        colorSeq.SetLoops(3)
                .SetUpdate(true);
    }

    /// <summary>
    /// 변화량 텍스트 표시
    /// </summary>
    private void ShowChangeText(string text, Color color)
    {
        if (_changeText == null) return;

        // 기존 애니메이션 정리
        _changeText.DOKill();

        // 텍스트 설정
        _changeText.text = text;
        _changeText.color = color;
        _changeText.rectTransform.anchoredPosition = _originalChangePosition;
        _changeText.gameObject.SetActive(true);

        // 애니메이션 시퀀스
        Sequence seq = DOTween.Sequence();

        // 페이드 인
        _changeText.alpha = 0f;
        seq.Append(_changeText.DOFade(1f, 0.2f));

        // 위로 이동
        seq.Join(_changeText.rectTransform.DOAnchorPosY(
            _originalChangePosition.y + _changeFloatDistance,
            _changeDisplayDuration
        ).SetEase(Ease.OutQuad));

        // 대기 후 페이드 아웃
        seq.Append(_changeText.DOFade(0f, 0.3f));

        // 완료 시 비활성화
        seq.OnComplete(() => _changeText.gameObject.SetActive(false));

        seq.SetUpdate(true);
    }
    #endregion

    #region Display
    /// <summary>
    /// 코인 텍스트 업데이트
    /// </summary>
    private void UpdateCoinText(int amount)
    {
        if (_coinText == null) return;

        _coinText.text = FormatNumber(amount);
    }

    /// <summary>
    /// 숫자 포맷팅
    /// </summary>
    private string FormatNumber(int number)
    {
        if (_useAbbreviation)
        {
            return ResourceManager.FormatNumber(number);
        }

        if (_useThousandSeparator)
        {
            return number.ToString("N0");
        }

        return number.ToString();
    }

    /// <summary>
    /// 즉시 값 설정 (애니메이션 없이)
    /// </summary>
    public void SetCoinValueImmediate(int amount)
    {
        KillAllTweens();
        _displayedCoins = amount;
        _targetCoins = amount;
        UpdateCoinText(amount);
        _coinText.color = _normalColor;
        _coinText.transform.localScale = _originalScale;
    }

    /// <summary>
    /// 모든 Tween 정리
    /// </summary>
    private void KillAllTweens()
    {
        _countTween?.Kill();
        _colorTween?.Kill();
        _scaleTween?.Kill();

        if (_coinText != null)
        {
            _coinText.DOKill();
        }

        if (_changeText != null)
        {
            _changeText.DOKill();
        }

        if (_coinIcon != null)
        {
            _coinIcon.transform.DOKill();
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 수동으로 피드백 재생 (외부 호출용)
    /// </summary>
    public void PlayFeedback(int changeAmount)
    {
        if (changeAmount > 0)
        {
            PlayAddFeedback();
            ShowChangeText($"+{FormatNumber(changeAmount)}", _addColor);
        }
        else if (changeAmount < 0)
        {
            PlaySpendFeedback();
            ShowChangeText($"{FormatNumber(changeAmount)}", _subtractColor);
        }
    }

    /// <summary>
    /// UI 새로고침
    /// </summary>
    public void RefreshUI()
    {
        if (ResourceManager.Instance != null)
        {
            SetCoinValueImmediate(ResourceManager.Instance.Coins);
        }
    }
    #endregion
}
