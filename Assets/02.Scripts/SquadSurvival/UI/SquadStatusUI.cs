using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SquadSurvival.Squad;

namespace SquadSurvival.UI
{
    /// <summary>
    /// 분대원 상태 UI 관리
    /// 각 분대원의 체력과 상태 표시
    /// </summary>
    public class SquadStatusUI : MonoBehaviour
    {
        [System.Serializable]
        public class MemberUISlot
        {
            public GameObject slotObject;
            public Image iconImage;
            public Image healthFill;
            public Image backgroundImage;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI statusText;
            public GameObject deadOverlay;
        }

        [Header("분대원 슬롯")]
        [SerializeField] private List<MemberUISlot> memberSlots = new List<MemberUISlot>();
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Transform slotContainer;

        [Header("색상 설정")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color woundedColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private Color deadColor = Color.gray;
        [SerializeField] private float woundedThreshold = 0.6f;
        [SerializeField] private float criticalThreshold = 0.3f;

        [Header("레이아웃")]
        [SerializeField] private float slotSpacing = 10f;

        private Dictionary<SquadMember, MemberUISlot> memberToSlot = new Dictionary<SquadMember, MemberUISlot>();

        private void Start()
        {
            // SquadController 이벤트 구독
            if (SquadController.Instance != null)
            {
                SquadController.Instance.OnMemberAdded.AddListener(OnMemberAdded);
                SquadController.Instance.OnMemberDeath.AddListener(OnMemberDeath);
            }

            // 자동 슬롯 탐색
            AutoFindSlots();
        }

        /// <summary>
        /// 기존 슬롯 자동 탐색
        /// </summary>
        private void AutoFindSlots()
        {
            if (slotContainer == null)
            {
                slotContainer = transform;
            }

            // 기존 자식 오브젝트에서 슬롯 찾기
            for (int i = 0; i < slotContainer.childCount && memberSlots.Count < 4; i++)
            {
                Transform child = slotContainer.GetChild(i);
                if (child.name.Contains("SquadMember"))
                {
                    var slot = CreateSlotFromExisting(child.gameObject);
                    if (slot != null)
                    {
                        memberSlots.Add(slot);
                    }
                }
            }
        }

        /// <summary>
        /// 기존 오브젝트에서 슬롯 생성
        /// </summary>
        private MemberUISlot CreateSlotFromExisting(GameObject slotObj)
        {
            var slot = new MemberUISlot
            {
                slotObject = slotObj,
                iconImage = slotObj.GetComponent<Image>(),
                healthFill = slotObj.transform.Find("HealthFill")?.GetComponent<Image>(),
                backgroundImage = slotObj.transform.Find("Background")?.GetComponent<Image>(),
                nameText = slotObj.GetComponentInChildren<TextMeshProUGUI>(),
                deadOverlay = slotObj.transform.Find("DeadOverlay")?.gameObject
            };

            return slot;
        }

        /// <summary>
        /// 분대원 추가 시 호출
        /// </summary>
        private void OnMemberAdded(SquadMember member)
        {
            int index = member.MemberIndex;

            // 슬롯이 부족하면 생성
            while (memberSlots.Count <= index)
            {
                CreateNewSlot();
            }

            var slot = memberSlots[index];
            memberToSlot[member] = slot;

            // 초기 UI 설정
            SetupSlot(slot, member);

            // 이벤트 구독
            member.OnHealthChanged.AddListener((current, max) => UpdateMemberHealth(member, current, max));
        }

        /// <summary>
        /// 분대원 사망 시 호출
        /// </summary>
        private void OnMemberDeath(SquadMember member)
        {
            if (memberToSlot.TryGetValue(member, out var slot))
            {
                SetSlotDead(slot, true);
            }
        }

        /// <summary>
        /// 새 슬롯 생성
        /// </summary>
        private void CreateNewSlot()
        {
            GameObject slotObj;

            if (slotPrefab != null)
            {
                slotObj = Instantiate(slotPrefab, slotContainer);
            }
            else
            {
                // 기본 슬롯 생성
                slotObj = new GameObject($"SquadMemberSlot_{memberSlots.Count}");
                slotObj.transform.SetParent(slotContainer);
                
                var rectTransform = slotObj.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(80, 80);

                var image = slotObj.AddComponent<Image>();
                image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }

            var slot = new MemberUISlot
            {
                slotObject = slotObj,
                iconImage = slotObj.GetComponent<Image>()
            };

            memberSlots.Add(slot);
        }

        /// <summary>
        /// 슬롯 초기 설정
        /// </summary>
        private void SetupSlot(MemberUISlot slot, SquadMember member)
        {
            if (slot.slotObject != null)
            {
                slot.slotObject.SetActive(true);
            }

            // 이름 설정
            if (slot.nameText != null)
            {
                slot.nameText.text = member.MemberName;
            }

            // 아이콘 설정
            if (slot.iconImage != null && member.MemberIcon != null)
            {
                slot.iconImage.sprite = member.MemberIcon;
            }

            // 체력바 초기화
            UpdateHealthBar(slot, 1f);

            // 사망 오버레이 숨기기
            SetSlotDead(slot, false);
        }

        /// <summary>
        /// 분대원 체력 업데이트
        /// </summary>
        private void UpdateMemberHealth(SquadMember member, float current, float max)
        {
            if (!memberToSlot.TryGetValue(member, out var slot)) return;

            float healthPercent = max > 0 ? current / max : 0f;
            UpdateHealthBar(slot, healthPercent);

            // 사망 상태 확인
            if (current <= 0)
            {
                SetSlotDead(slot, true);
            }
        }

        /// <summary>
        /// 체력바 업데이트
        /// </summary>
        private void UpdateHealthBar(MemberUISlot slot, float healthPercent)
        {
            // Fill 이미지 업데이트
            if (slot.healthFill != null)
            {
                slot.healthFill.fillAmount = healthPercent;
                slot.healthFill.color = GetHealthColor(healthPercent);
            }

            // 배경 이미지 색상
            if (slot.backgroundImage != null)
            {
                slot.backgroundImage.color = GetHealthColor(healthPercent) * 0.3f;
            }

            // 아이콘 색상
            if (slot.iconImage != null)
            {
                slot.iconImage.color = healthPercent > 0 ? Color.white : deadColor;
            }

            // 상태 텍스트
            if (slot.statusText != null)
            {
                slot.statusText.text = GetStatusText(healthPercent);
            }
        }

        /// <summary>
        /// 체력 비율에 따른 색상
        /// </summary>
        private Color GetHealthColor(float healthPercent)
        {
            if (healthPercent <= 0)
            {
                return deadColor;
            }
            else if (healthPercent < criticalThreshold)
            {
                return criticalColor;
            }
            else if (healthPercent < woundedThreshold)
            {
                return woundedColor;
            }
            else
            {
                return healthyColor;
            }
        }

        /// <summary>
        /// 상태 텍스트
        /// </summary>
        private string GetStatusText(float healthPercent)
        {
            if (healthPercent <= 0)
            {
                return "KIA";
            }
            else if (healthPercent < criticalThreshold)
            {
                return "Critical";
            }
            else if (healthPercent < woundedThreshold)
            {
                return "Wounded";
            }
            else
            {
                return "OK";
            }
        }

        /// <summary>
        /// 사망 상태 설정
        /// </summary>
        private void SetSlotDead(MemberUISlot slot, bool isDead)
        {
            if (slot.deadOverlay != null)
            {
                slot.deadOverlay.SetActive(isDead);
            }

            if (slot.iconImage != null)
            {
                slot.iconImage.color = isDead ? deadColor : Color.white;
            }
        }

        /// <summary>
        /// 특정 인덱스 슬롯 업데이트
        /// </summary>
        public void UpdateSlot(int index, float healthPercent, bool isAlive)
        {
            if (index < 0 || index >= memberSlots.Count) return;

            var slot = memberSlots[index];
            UpdateHealthBar(slot, healthPercent);
            SetSlotDead(slot, !isAlive);
        }

        /// <summary>
        /// 모든 슬롯 초기화
        /// </summary>
        public void ResetAllSlots()
        {
            foreach (var slot in memberSlots)
            {
                UpdateHealthBar(slot, 1f);
                SetSlotDead(slot, false);
            }
        }

        /// <summary>
        /// UI 참조 자동 탐색
        /// </summary>
        [ContextMenu("Auto Find UI References")]
        public void AutoFindUIReferences()
        {
            if (slotContainer == null)
            {
                slotContainer = transform;
            }

            memberSlots.Clear();

            for (int i = 0; i < 4; i++)
            {
                Transform child = slotContainer.Find($"SquadMember{i + 1}");
                if (child != null)
                {
                    var slot = CreateSlotFromExisting(child.gameObject);
                    memberSlots.Add(slot);
                }
            }

#if UNITY_EDITOR
            Debug.Log($"[SquadStatusUI] {memberSlots.Count}개의 슬롯 찾음");
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void OnDestroy()
        {
            if (SquadController.Instance != null)
            {
                SquadController.Instance.OnMemberAdded.RemoveListener(OnMemberAdded);
                SquadController.Instance.OnMemberDeath.RemoveListener(OnMemberDeath);
            }
        }
    }
}
