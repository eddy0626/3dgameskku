using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// 분대 상태 UI 컴포넌트
/// - SquadManager 연동
/// - 분대원별 미니 체력바 (최대 4명)
/// - 죽은 분대원은 회색 처리
/// - 분대원 상태 실시간 표시
/// </summary>
public class SquadStatusUI : MonoBehaviour
{
    #region Inspector Fields
    [Header("패널 참조")]
    [Tooltip("분대 상태 패널")]
    [SerializeField] private GameObject _squadPanel;

    [Tooltip("분대원 슬롯 컨테이너")]
    [SerializeField] private Transform _slotsContainer;

    [Tooltip("분대원 슬롯 프리팹")]
    [SerializeField] private GameObject _memberSlotPrefab;

    [Header("미리 배치된 슬롯 (프리팹 사용 안 할 경우)")]
    [Tooltip("분대원 슬롯 배열")]
    [SerializeField] private MemberSlot[] _memberSlots;

    [Header("표시 설정")]
    [Tooltip("최대 표시 분대원 수")]
    [SerializeField] private int _maxDisplayMembers = 4;

    [Tooltip("빈 슬롯 표시")]
    [SerializeField] private bool _showEmptySlots = true;

    [Header("색상 설정")]
    [Tooltip("체력바 풀 색상")]
    [SerializeField] private Color _healthFullColor = new Color(0.2f, 0.8f, 0.2f);

    [Tooltip("체력바 중간 색상")]
    [SerializeField] private Color _healthMidColor = new Color(0.9f, 0.9f, 0.2f);

    [Tooltip("체력바 낮음 색상")]
    [SerializeField] private Color _healthLowColor = new Color(0.9f, 0.2f, 0.2f);

    [Tooltip("죽음 상태 색상")]
    [SerializeField] private Color _deadColor = new Color(0.3f, 0.3f, 0.3f);

    [Tooltip("빈 슬롯 색상")]
    [SerializeField] private Color _emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    [Header("애니메이션 설정")]
    [Tooltip("체력바 업데이트 시간")]
    [SerializeField] private float _healthBarUpdateDuration = 0.3f;

    [Tooltip("낮은 체력 임계값")]
    [SerializeField] private float _lowHealthThreshold = 0.3f;

    [Tooltip("중간 체력 임계값")]
    [SerializeField] private float _midHealthThreshold = 0.6f;
    #endregion

    #region Private Fields
    private List<MemberSlotData> _slotData = new List<MemberSlotData>();
    private Dictionary<SquadMember, int> _memberToSlotIndex = new Dictionary<SquadMember, int>();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 슬롯 초기화
        InitializeSlots();
    }

    private void Start()
    {
        // SquadManager 이벤트 구독
        if (SquadManager.Instance != null)
        {
            SquadManager.Instance.OnMemberAdded += OnMemberAdded;
            SquadManager.Instance.OnMemberRemoved += OnMemberRemoved;
            SquadManager.Instance.OnSquadSizeChanged += OnSquadSizeChanged;

            // 기존 분대원 등록
            RefreshAllSlots();
        }
        else
        {
            Debug.LogWarning("[SquadStatusUI] SquadManager를 찾을 수 없습니다!");
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (SquadManager.Instance != null)
        {
            SquadManager.Instance.OnMemberAdded -= OnMemberAdded;
            SquadManager.Instance.OnMemberRemoved -= OnMemberRemoved;
            SquadManager.Instance.OnSquadSizeChanged -= OnSquadSizeChanged;
        }

        // 분대원 이벤트 해제
        foreach (var slotData in _slotData)
        {
            if (slotData.member != null && slotData.health != null)
            {
                slotData.health.OnHealthChanged -= slotData.OnHealthChanged;
                slotData.health.OnDeath -= slotData.OnDeath;
            }
        }

        // Tween 정리
        KillAllTweens();
    }
    #endregion

    #region Initialization
    private void InitializeSlots()
    {
        _slotData.Clear();
        _memberToSlotIndex.Clear();

        // 미리 배치된 슬롯 사용
        if (_memberSlots != null && _memberSlots.Length > 0)
        {
            for (int i = 0; i < _memberSlots.Length && i < _maxDisplayMembers; i++)
            {
                var slotData = new MemberSlotData
                {
                    slot = _memberSlots[i],
                    slotIndex = i,
                    uiComponent = this
                };
                _slotData.Add(slotData);

                // 빈 슬롯 상태로 초기화
                SetSlotEmpty(slotData);
            }
        }
        // 프리팹으로 동적 생성
        else if (_memberSlotPrefab != null && _slotsContainer != null)
        {
            for (int i = 0; i < _maxDisplayMembers; i++)
            {
                GameObject slotObj = Instantiate(_memberSlotPrefab, _slotsContainer);
                var slot = slotObj.GetComponent<MemberSlot>();

                if (slot == null)
                {
                    slot = new MemberSlot();
                    // 컴포넌트 자동 찾기
                    slot.slotRoot = slotObj;
                    slot.healthBar = slotObj.GetComponentInChildren<Image>();
                    slot.nameText = slotObj.GetComponentInChildren<TextMeshProUGUI>();
                }

                var slotData = new MemberSlotData
                {
                    slot = slot,
                    slotIndex = i,
                    uiComponent = this
                };
                _slotData.Add(slotData);

                SetSlotEmpty(slotData);
            }
        }
    }
    #endregion

    #region Event Handlers
    private void OnMemberAdded(SquadMember member)
    {
        AssignMemberToSlot(member);
    }

    private void OnMemberRemoved(SquadMember member)
    {
        RemoveMemberFromSlot(member);
    }

    private void OnSquadSizeChanged(int current, int max)
    {
        RefreshAllSlots();
    }
    #endregion

    #region Slot Management
    /// <summary>
    /// 분대원을 슬롯에 할당
    /// </summary>
    private void AssignMemberToSlot(SquadMember member)
    {
        if (member == null) return;
        if (_memberToSlotIndex.ContainsKey(member)) return;

        // 빈 슬롯 찾기
        int slotIndex = FindEmptySlot();
        if (slotIndex < 0)
        {
            Debug.LogWarning("[SquadStatusUI] 빈 슬롯이 없습니다!");
            return;
        }

        var slotData = _slotData[slotIndex];
        slotData.member = member;
        slotData.health = member.GetComponent<SquadMemberHealth>();

        _memberToSlotIndex[member] = slotIndex;

        // 슬롯 설정
        SetupSlot(slotData);

        // 체력 이벤트 구독
        if (slotData.health != null)
        {
            slotData.OnHealthChanged = (current, max) => UpdateHealthBar(slotData, current, max);
            slotData.OnDeath = () => SetSlotDead(slotData);

            slotData.health.OnHealthChanged += slotData.OnHealthChanged;
            slotData.health.OnDeath += slotData.OnDeath;

            // 초기 체력 표시
            UpdateHealthBar(slotData, slotData.health.CurrentHealth, slotData.health.MaxHealth);
        }

        // 등장 애니메이션
        PlaySlotAppearAnimation(slotData);

        Debug.Log($"[SquadStatusUI] 분대원 할당: {member.name} -> Slot {slotIndex}");
    }

    /// <summary>
    /// 분대원을 슬롯에서 제거
    /// </summary>
    private void RemoveMemberFromSlot(SquadMember member)
    {
        if (member == null) return;
        if (!_memberToSlotIndex.TryGetValue(member, out int slotIndex)) return;

        var slotData = _slotData[slotIndex];

        // 이벤트 구독 해제
        if (slotData.health != null)
        {
            slotData.health.OnHealthChanged -= slotData.OnHealthChanged;
            slotData.health.OnDeath -= slotData.OnDeath;
        }

        _memberToSlotIndex.Remove(member);
        slotData.member = null;
        slotData.health = null;

        // 빈 슬롯으로 설정
        SetSlotEmpty(slotData);

        Debug.Log($"[SquadStatusUI] 분대원 제거: {member.name} from Slot {slotIndex}");
    }

    /// <summary>
    /// 빈 슬롯 인덱스 찾기
    /// </summary>
    private int FindEmptySlot()
    {
        for (int i = 0; i < _slotData.Count; i++)
        {
            if (_slotData[i].member == null)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 모든 슬롯 새로고침
    /// </summary>
    public void RefreshAllSlots()
    {
        // 기존 연결 해제
        foreach (var slotData in _slotData)
        {
            if (slotData.member != null && slotData.health != null)
            {
                slotData.health.OnHealthChanged -= slotData.OnHealthChanged;
                slotData.health.OnDeath -= slotData.OnDeath;
            }
            slotData.member = null;
            slotData.health = null;
            SetSlotEmpty(slotData);
        }
        _memberToSlotIndex.Clear();

        // 현재 분대원 재등록
        if (SquadManager.Instance != null)
        {
            var members = SquadManager.Instance.Members;
            for (int i = 0; i < members.Count && i < _maxDisplayMembers; i++)
            {
                AssignMemberToSlot(members[i]);
            }
        }
    }
    #endregion

    #region Slot Display
    /// <summary>
    /// 슬롯 설정
    /// </summary>
    private void SetupSlot(MemberSlotData slotData)
    {
        var slot = slotData.slot;
        if (slot == null) return;

        // 활성화
        if (slot.slotRoot != null)
        {
            slot.slotRoot.SetActive(true);
        }

        // 이름
        if (slot.nameText != null && slotData.member != null)
        {
            string name = slotData.member.Data != null
                ? slotData.member.Data.memberName
                : slotData.member.name;
            slot.nameText.text = name;
            slot.nameText.color = Color.white;
        }

        // 아이콘
        if (slot.iconImage != null && slotData.member != null && slotData.member.Data != null)
        {
            if (slotData.member.Data.icon != null)
            {
                slot.iconImage.sprite = slotData.member.Data.icon;
                slot.iconImage.color = Color.white;
            }
        }

        // 배경 활성화
        if (slot.backgroundImage != null)
        {
            slot.backgroundImage.color = Color.white;
        }
    }

    /// <summary>
    /// 빈 슬롯 설정
    /// </summary>
    private void SetSlotEmpty(MemberSlotData slotData)
    {
        var slot = slotData.slot;
        if (slot == null) return;

        if (!_showEmptySlots && slot.slotRoot != null)
        {
            slot.slotRoot.SetActive(false);
            return;
        }

        if (slot.slotRoot != null)
        {
            slot.slotRoot.SetActive(true);
        }

        // 빈 슬롯 표시
        if (slot.nameText != null)
        {
            slot.nameText.text = "---";
            slot.nameText.color = _emptySlotColor;
        }

        if (slot.healthBar != null)
        {
            slot.healthBar.fillAmount = 0f;
            slot.healthBar.color = _emptySlotColor;
        }

        if (slot.backgroundImage != null)
        {
            slot.backgroundImage.color = _emptySlotColor;
        }

        if (slot.iconImage != null)
        {
            slot.iconImage.color = _emptySlotColor;
        }
    }

    /// <summary>
    /// 죽음 상태 설정
    /// </summary>
    private void SetSlotDead(MemberSlotData slotData)
    {
        var slot = slotData.slot;
        if (slot == null) return;

        // 회색 처리
        if (slot.healthBar != null)
        {
            slot.healthBar.DOColor(_deadColor, _healthBarUpdateDuration);
        }

        if (slot.backgroundImage != null)
        {
            slot.backgroundImage.DOColor(_deadColor, _healthBarUpdateDuration);
        }

        if (slot.iconImage != null)
        {
            slot.iconImage.DOColor(_deadColor, _healthBarUpdateDuration);
        }

        if (slot.nameText != null)
        {
            slot.nameText.DOColor(_deadColor, _healthBarUpdateDuration);
        }

        // 죽음 애니메이션
        PlayDeathAnimation(slotData);
    }

    /// <summary>
    /// 체력바 업데이트
    /// </summary>
    private void UpdateHealthBar(MemberSlotData slotData, float current, float max)
    {
        var slot = slotData.slot;
        if (slot == null || slot.healthBar == null) return;

        float healthPercent = max > 0 ? current / max : 0f;

        // 체력바 채우기 애니메이션
        slot.healthBar.DOFillAmount(healthPercent, _healthBarUpdateDuration);

        // 체력에 따른 색상
        Color targetColor;
        if (healthPercent <= _lowHealthThreshold)
        {
            targetColor = _healthLowColor;
        }
        else if (healthPercent <= _midHealthThreshold)
        {
            targetColor = Color.Lerp(_healthLowColor, _healthMidColor,
                (healthPercent - _lowHealthThreshold) / (_midHealthThreshold - _lowHealthThreshold));
        }
        else
        {
            targetColor = Color.Lerp(_healthMidColor, _healthFullColor,
                (healthPercent - _midHealthThreshold) / (1f - _midHealthThreshold));
        }

        slot.healthBar.DOColor(targetColor, _healthBarUpdateDuration);

        // 체력 텍스트 (있는 경우)
        if (slot.healthText != null)
        {
            slot.healthText.text = $"{current:F0}/{max:F0}";
        }

        // 낮은 체력 경고 애니메이션
        if (healthPercent <= _lowHealthThreshold && healthPercent > 0)
        {
            PlayLowHealthWarning(slotData);
        }
    }
    #endregion

    #region Animations
    private void PlaySlotAppearAnimation(MemberSlotData slotData)
    {
        var slot = slotData.slot;
        if (slot == null || slot.slotRoot == null) return;

        slot.slotRoot.transform.localScale = Vector3.zero;
        slot.slotRoot.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    private void PlayDeathAnimation(MemberSlotData slotData)
    {
        var slot = slotData.slot;
        if (slot == null || slot.slotRoot == null) return;

        // 흔들기 + 축소
        Sequence seq = DOTween.Sequence();
        seq.Append(slot.slotRoot.transform.DOShakePosition(0.3f, 5f, 20));
        seq.Append(slot.slotRoot.transform.DOScale(0.9f, 0.2f));
    }

    private void PlayLowHealthWarning(MemberSlotData slotData)
    {
        var slot = slotData.slot;
        if (slot == null || slot.healthBar == null) return;

        // 이미 깜빡이고 있으면 무시
        if (DOTween.IsTweening(slot.healthBar.transform)) return;

        // 깜빡임 효과
        slot.healthBar.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5, 0.5f);
    }

    private void KillAllTweens()
    {
        foreach (var slotData in _slotData)
        {
            if (slotData.slot != null)
            {
                if (slotData.slot.slotRoot != null)
                    slotData.slot.slotRoot.transform.DOKill();
                if (slotData.slot.healthBar != null)
                    slotData.slot.healthBar.DOKill();
            }
        }
    }
    #endregion

    #region Nested Types
    /// <summary>
    /// 분대원 슬롯 UI 요소
    /// </summary>
    [System.Serializable]
    public class MemberSlot
    {
        [Tooltip("슬롯 루트 오브젝트")]
        public GameObject slotRoot;

        [Tooltip("체력바 이미지 (Filled)")]
        public Image healthBar;

        [Tooltip("체력바 배경 이미지")]
        public Image healthBarBackground;

        [Tooltip("배경 이미지")]
        public Image backgroundImage;

        [Tooltip("아이콘 이미지")]
        public Image iconImage;

        [Tooltip("이름 텍스트")]
        public TextMeshProUGUI nameText;

        [Tooltip("체력 수치 텍스트")]
        public TextMeshProUGUI healthText;

        [Tooltip("상태 텍스트")]
        public TextMeshProUGUI statusText;
    }

    /// <summary>
    /// 슬롯 런타임 데이터
    /// </summary>
    private class MemberSlotData
    {
        public MemberSlot slot;
        public int slotIndex;
        public SquadMember member;
        public SquadMemberHealth health;
        public SquadStatusUI uiComponent;

        // 이벤트 핸들러 참조 (해제용)
        public System.Action<float, float> OnHealthChanged;
        public System.Action OnDeath;
    }
    #endregion
}
