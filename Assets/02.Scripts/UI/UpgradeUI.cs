using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// 업그레이드 UI 컴포넌트
/// - UpgradeManager 연동
/// - 3개 업그레이드 선택지 버튼
/// - 업그레이드 선택 패널 (웨이브 클리어 시 표시)
/// - 각 버튼에 업그레이드 정보 표시 (이름, 설명, 비용, 레벨)
/// </summary>
public class UpgradeUI : MonoBehaviour
{
    #region Inspector Fields
    [Header("패널 참조")]
    [Tooltip("업그레이드 선택 패널")]
    [SerializeField] private GameObject _upgradePanel;

    [Tooltip("패널 배경 (어둡게 처리용)")]
    [SerializeField] private Image _backgroundOverlay;

    [Tooltip("패널 제목 텍스트")]
    [SerializeField] private TextMeshProUGUI _titleText;

    [Header("업그레이드 버튼")]
    [Tooltip("업그레이드 선택 버튼들")]
    [SerializeField] private UpgradeButton[] _upgradeButtons;

    [Header("스킵 버튼")]
    [Tooltip("스킵 버튼 (선택하지 않고 넘기기)")]
    [SerializeField] private Button _skipButton;

    [Tooltip("스킵 시 보너스 골드")]
    [SerializeField] private int _skipBonusGold = 50;

    [Header("리롤 버튼")]
    [Tooltip("리롤 버튼 (선택지 다시 받기)")]
    [SerializeField] private Button _rerollButton;

    [Tooltip("리롤 비용")]
    [SerializeField] private int _rerollCost = 100;

    [Tooltip("리롤 비용 증가량")]
    [SerializeField] private int _rerollCostIncrease = 50;

    [Tooltip("리롤 비용 텍스트")]
    [SerializeField] private TextMeshProUGUI _rerollCostText;

    [Header("애니메이션 설정")]
    [Tooltip("패널 등장 시간")]
    [SerializeField] private float _showDuration = 0.5f;

    [Tooltip("버튼 등장 딜레이 (각각)")]
    [SerializeField] private float _buttonShowDelay = 0.1f;

    [Header("설정")]
    [Tooltip("선택지 개수")]
    [SerializeField] private int _choiceCount = 3;

    [Tooltip("스킵 허용")]
    [SerializeField] private bool _allowSkip = true;

    [Tooltip("리롤 허용")]
    [SerializeField] private bool _allowReroll = true;
    #endregion

    #region Private Fields
    private List<UpgradeData> _currentChoices = new List<UpgradeData>();
    private int _currentRerollCost;
    private bool _isShowing;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 패널 초기 비활성화
        if (_upgradePanel != null)
        {
            _upgradePanel.SetActive(false);
        }

        // 버튼 이벤트 연결
        SetupButtons();
    }

    private void Start()
    {
        // WaveManager 이벤트 구독
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveComplete += OnWaveComplete;
        }

        // UpgradeManager 이벤트 구독
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgradeChoicesReady += OnUpgradeChoicesReady;
        }

        _currentRerollCost = _rerollCost;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveComplete -= OnWaveComplete;
        }

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgradeChoicesReady -= OnUpgradeChoicesReady;
        }

        // Tween 정리
        KillAllTweens();
    }
    #endregion

    #region Setup
    private void SetupButtons()
    {
        // 업그레이드 버튼 이벤트
        for (int i = 0; i < _upgradeButtons.Length; i++)
        {
            int index = i; // 클로저를 위한 복사
            if (_upgradeButtons[i] != null && _upgradeButtons[i].button != null)
            {
                _upgradeButtons[i].button.onClick.AddListener(() => OnUpgradeSelected(index));
            }
        }

        // 스킵 버튼
        if (_skipButton != null)
        {
            _skipButton.onClick.AddListener(OnSkipClicked);
            _skipButton.gameObject.SetActive(_allowSkip);
        }

        // 리롤 버튼
        if (_rerollButton != null)
        {
            _rerollButton.onClick.AddListener(OnRerollClicked);
            _rerollButton.gameObject.SetActive(_allowReroll);
        }
    }
    #endregion

    #region Event Handlers
    private void OnWaveComplete(int waveNumber)
    {
        // 웨이브 완료 시 업그레이드 선택 표시
        ShowUpgradeSelection();
    }

    private void OnUpgradeChoicesReady(List<UpgradeData> choices)
    {
        _currentChoices = choices;
        DisplayChoices(choices);
    }
    #endregion

    #region Show/Hide Panel
    /// <summary>
    /// 업그레이드 선택 패널 표시
    /// </summary>
    public void ShowUpgradeSelection()
    {
        if (_isShowing) return;
        if (UpgradeManager.Instance == null) return;

        _isShowing = true;

        // 선택지 생성
        var choices = UpgradeManager.Instance.GetRandomUpgradeChoices(_choiceCount);

        if (choices.Count == 0)
        {
            Debug.Log("[UpgradeUI] 사용 가능한 업그레이드가 없습니다.");
            _isShowing = false;
            return;
        }

        _currentChoices = choices;

        // 패널 표시
        ShowPanel();
        DisplayChoices(choices);

        // UIManager에 알림
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowUpgradeUI();
        }
    }

    /// <summary>
    /// 패널 닫기
    /// </summary>
    public void HideUpgradeSelection()
    {
        HidePanel();

        // UIManager에 알림
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideUpgradeUI();
        }
    }

    private void ShowPanel()
    {
        if (_upgradePanel == null) return;

        _upgradePanel.SetActive(true);

        // 배경 페이드 인
        if (_backgroundOverlay != null)
        {
            _backgroundOverlay.color = new Color(0, 0, 0, 0);
            _backgroundOverlay.DOFade(0.7f, _showDuration).SetUpdate(true);
        }

        // 패널 스케일 애니메이션
        _upgradePanel.transform.localScale = Vector3.zero;
        _upgradePanel.transform.DOScale(1f, _showDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);

        // 버튼들 순차 등장
        for (int i = 0; i < _upgradeButtons.Length; i++)
        {
            if (_upgradeButtons[i] != null && _upgradeButtons[i].buttonRoot != null)
            {
                var buttonObj = _upgradeButtons[i].buttonRoot;
                buttonObj.transform.localScale = Vector3.zero;
                buttonObj.transform.DOScale(1f, 0.3f)
                    .SetDelay(_showDuration + _buttonShowDelay * i)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }
        }

        // 리롤 비용 업데이트
        UpdateRerollCostDisplay();
    }

    private void HidePanel()
    {
        if (_upgradePanel == null) return;

        _isShowing = false;

        // 패널 스케일 아웃
        _upgradePanel.transform.DOScale(0f, 0.3f)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _upgradePanel.SetActive(false);
                Time.timeScale = 1f; // 게임 재개
            });

        // 배경 페이드 아웃
        if (_backgroundOverlay != null)
        {
            _backgroundOverlay.DOFade(0f, 0.3f).SetUpdate(true);
        }
    }
    #endregion

    #region Display Choices
    /// <summary>
    /// 선택지 표시
    /// </summary>
    private void DisplayChoices(List<UpgradeData> choices)
    {
        // 모든 버튼 비활성화
        foreach (var btn in _upgradeButtons)
        {
            if (btn != null && btn.buttonRoot != null)
            {
                btn.buttonRoot.SetActive(false);
            }
        }

        // 선택지만큼 버튼 활성화 및 정보 설정
        for (int i = 0; i < choices.Count && i < _upgradeButtons.Length; i++)
        {
            var upgrade = choices[i];
            var btn = _upgradeButtons[i];

            if (btn == null || btn.buttonRoot == null) continue;

            btn.buttonRoot.SetActive(true);
            SetupUpgradeButton(btn, upgrade);
        }
    }

    /// <summary>
    /// 업그레이드 버튼 설정
    /// </summary>
    private void SetupUpgradeButton(UpgradeButton btn, UpgradeData upgrade)
    {
        if (upgrade == null) return;

        int currentLevel = UpgradeManager.Instance?.GetUpgradeLevel(upgrade) ?? 0;
        int cost = UpgradeManager.Instance?.GetUpgradeCost(upgrade) ?? 0;
        bool canAfford = UpgradeManager.Instance?.CanAfford(upgrade) ?? false;

        // 이름
        if (btn.nameText != null)
        {
            btn.nameText.text = upgrade.upgradeName;
            btn.nameText.color = upgrade.RarityColor;
        }

        // 설명
        if (btn.descriptionText != null)
        {
            btn.descriptionText.text = upgrade.GetFormattedDescription(currentLevel);
        }

        // 레벨
        if (btn.levelText != null)
        {
            btn.levelText.text = $"Lv.{currentLevel} / {upgrade.MaxLevel}";
        }

        // 비용
        if (btn.costText != null)
        {
            btn.costText.text = $"{cost:N0}";
            btn.costText.color = canAfford ? Color.white : Color.red;
        }

        // 아이콘
        if (btn.iconImage != null && upgrade.icon != null)
        {
            btn.iconImage.sprite = upgrade.icon;
            btn.iconImage.color = Color.white;
        }

        // 희귀도 배경
        if (btn.rarityBackground != null)
        {
            btn.rarityBackground.color = new Color(
                upgrade.RarityColor.r,
                upgrade.RarityColor.g,
                upgrade.RarityColor.b,
                0.3f
            );
        }

        // 희귀도 텍스트
        if (btn.rarityText != null)
        {
            btn.rarityText.text = UpgradeData.GetRarityName(upgrade.rarity);
            btn.rarityText.color = upgrade.RarityColor;
        }

        // 버튼 상호작용
        if (btn.button != null)
        {
            btn.button.interactable = canAfford;
        }

        // 스탯 타입 텍스트
        if (btn.statTypeText != null)
        {
            btn.statTypeText.text = upgrade.GetStatTypeName();
        }
    }
    #endregion

    #region Button Callbacks
    /// <summary>
    /// 업그레이드 선택
    /// </summary>
    private void OnUpgradeSelected(int index)
    {
        if (index < 0 || index >= _currentChoices.Count) return;

        UpgradeData selected = _currentChoices[index];

        if (UpgradeManager.Instance != null)
        {
            bool success = UpgradeManager.Instance.TryUpgrade(selected);

            if (success)
            {
                Debug.Log($"[UpgradeUI] 업그레이드 적용: {selected.upgradeName}");

                // 선택된 버튼 피드백
                PlaySelectFeedback(index);

                // 패널 닫기
                HideUpgradeSelection();

                // 리롤 비용 초기화
                _currentRerollCost = _rerollCost;
            }
            else
            {
                // 실패 피드백
                PlayFailFeedback(index);
            }
        }
    }

    /// <summary>
    /// 스킵 버튼 클릭
    /// </summary>
    private void OnSkipClicked()
    {
        if (!_allowSkip) return;

        // 스킵 보너스 지급
        if (ResourceManager.Instance != null && _skipBonusGold > 0)
        {
            ResourceManager.Instance.AddCoins(_skipBonusGold);
            Debug.Log($"[UpgradeUI] 스킵 보너스: +{_skipBonusGold} Gold");
        }

        HideUpgradeSelection();
        _currentRerollCost = _rerollCost;
    }

    /// <summary>
    /// 리롤 버튼 클릭
    /// </summary>
    private void OnRerollClicked()
    {
        if (!_allowReroll) return;
        if (ResourceManager.Instance == null) return;

        // 비용 지불
        if (!ResourceManager.Instance.SpendCoins(_currentRerollCost))
        {
            // 비용 부족 피드백
            if (_rerollButton != null)
            {
                _rerollButton.transform.DOShakePosition(0.3f, 5f, 20).SetUpdate(true);
            }
            return;
        }

        // 비용 증가
        _currentRerollCost += _rerollCostIncrease;
        UpdateRerollCostDisplay();

        // 새 선택지 생성
        if (UpgradeManager.Instance != null)
        {
            var newChoices = UpgradeManager.Instance.GetRandomUpgradeChoices(_choiceCount);
            _currentChoices = newChoices;
            DisplayChoices(newChoices);

            // 리롤 애니메이션
            foreach (var btn in _upgradeButtons)
            {
                if (btn != null && btn.buttonRoot != null && btn.buttonRoot.activeSelf)
                {
                    btn.buttonRoot.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f)
                        .SetUpdate(true);
                }
            }
        }
    }

    private void UpdateRerollCostDisplay()
    {
        if (_rerollCostText != null)
        {
            _rerollCostText.text = $"{_currentRerollCost:N0}";

            bool canAfford = ResourceManager.Instance != null &&
                            ResourceManager.Instance.HasCoins(_currentRerollCost);
            _rerollCostText.color = canAfford ? Color.white : Color.red;
        }

        if (_rerollButton != null)
        {
            _rerollButton.interactable = ResourceManager.Instance != null &&
                                         ResourceManager.Instance.HasCoins(_currentRerollCost);
        }
    }
    #endregion

    #region Feedback
    private void PlaySelectFeedback(int index)
    {
        if (index < 0 || index >= _upgradeButtons.Length) return;

        var btn = _upgradeButtons[index];
        if (btn == null || btn.buttonRoot == null) return;

        // 선택된 버튼 확대 후 사라짐
        Sequence seq = DOTween.Sequence();
        seq.Append(btn.buttonRoot.transform.DOScale(1.2f, 0.2f));

        // CanvasGroup이 있으면 페이드 아웃, 없으면 스케일 축소
        var canvasGroup = btn.buttonRoot.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            seq.Join(canvasGroup.DOFade(0f, 0.3f));
        }
        else
        {
            seq.Append(btn.buttonRoot.transform.DOScale(0f, 0.3f));
        }

        seq.SetUpdate(true);
    }

    private void PlayFailFeedback(int index)
    {
        if (index < 0 || index >= _upgradeButtons.Length) return;

        var btn = _upgradeButtons[index];
        if (btn == null || btn.buttonRoot == null) return;

        // 흔들기
        btn.buttonRoot.transform.DOShakePosition(0.3f, 10f, 20).SetUpdate(true);
    }
    #endregion

    #region Utility
    private void KillAllTweens()
    {
        if (_upgradePanel != null) _upgradePanel.transform.DOKill();
        if (_backgroundOverlay != null) _backgroundOverlay.DOKill();

        foreach (var btn in _upgradeButtons)
        {
            if (btn != null && btn.buttonRoot != null)
            {
                btn.buttonRoot.transform.DOKill();
            }
        }
    }

    /// <summary>
    /// 외부에서 패널 표시 강제 호출
    /// </summary>
    public void ForceShowPanel()
    {
        ShowUpgradeSelection();
    }
    #endregion

    #region Nested Types
    /// <summary>
    /// 업그레이드 버튼 구성 요소
    /// </summary>
    [System.Serializable]
    public class UpgradeButton
    {
        [Tooltip("버튼 루트 오브젝트")]
        public GameObject buttonRoot;

        [Tooltip("버튼 컴포넌트")]
        public Button button;

        [Tooltip("업그레이드 이름 텍스트")]
        public TextMeshProUGUI nameText;

        [Tooltip("설명 텍스트")]
        public TextMeshProUGUI descriptionText;

        [Tooltip("레벨 텍스트")]
        public TextMeshProUGUI levelText;

        [Tooltip("비용 텍스트")]
        public TextMeshProUGUI costText;

        [Tooltip("스탯 타입 텍스트")]
        public TextMeshProUGUI statTypeText;

        [Tooltip("희귀도 텍스트")]
        public TextMeshProUGUI rarityText;

        [Tooltip("아이콘 이미지")]
        public Image iconImage;

        [Tooltip("희귀도 배경 이미지")]
        public Image rarityBackground;
    }
    #endregion
}
