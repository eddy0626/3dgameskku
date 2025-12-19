using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SquadSurvival.Squad;
using SquadSurvival.Core;

namespace SquadSurvival.UI
{
    /// <summary>
    /// 분대원 상태 UI 관리
    /// 각 분대원의 체력, 이름, 상태 표시
    /// </summary>
    public class SquadUI : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private Transform squadMemberContainer;
        [SerializeField] private GameObject squadMemberUIPrefab;

        [Header("색상 설정")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color woundedColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private Color deadColor = Color.gray;

        // 분대원 UI 인스턴스 관리
        private List<SquadMemberUIElement> memberUIElements = new List<SquadMemberUIElement>();

        private void Start()
        {
            // UI 참조 자동 탐색
            AutoFindUIReferences();

            // SquadController 이벤트 구독
            if (SquadController.Instance != null)
            {
                SquadController.Instance.OnMemberAdded.AddListener(OnMemberAdded);
                SquadController.Instance.OnMemberDeath.AddListener(OnMemberDeath);

                // 기존 분대원 UI 생성
                foreach (var member in SquadController.Instance.SquadMembers)
                {
                    CreateMemberUI(member);
                }
            }
        }

        /// <summary>
        /// UI 참조 자동 탐색
        /// </summary>
        private void AutoFindUIReferences()
        {
            if (squadMemberContainer == null)
            {
                squadMemberContainer = transform.Find("SquadMemberContainer");
                if (squadMemberContainer == null)
                {
                    // 없으면 생성
                    GameObject container = new GameObject("SquadMemberContainer");
                    container.transform.SetParent(transform, false);
                    var rect = container.AddComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    var layout = container.AddComponent<VerticalLayoutGroup>();
                    layout.spacing = 5;
                    layout.childControlHeight = false;
                    layout.childControlWidth = true;
                    squadMemberContainer = container.transform;
                }
            }

#if UNITY_EDITOR
            Debug.Log("[SquadUI] UI 참조 설정 완료");
#endif
        }

        private void Update()
        {
            // 분대원 상태 업데이트
            UpdateAllMemberUI();
        }

        /// <summary>
        /// 분대원 UI 요소 생성
        /// </summary>
        private void CreateMemberUI(SquadMember member)
        {
            if (squadMemberContainer == null || member == null) return;

            // 이미 존재하는지 확인
            if (memberUIElements.Exists(e => e.Member == member)) return;

            GameObject uiObj;
            if (squadMemberUIPrefab != null)
            {
                uiObj = Instantiate(squadMemberUIPrefab, squadMemberContainer);
            }
            else
            {
                // 프리팹이 없으면 기본 UI 생성
                uiObj = CreateDefaultMemberUI(member);
            }

            var element = new SquadMemberUIElement
            {
                Member = member,
                RootObject = uiObj,
                NameText = uiObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>(),
                HealthBar = uiObj.transform.Find("HealthBar")?.GetComponent<Image>(),
                HealthText = uiObj.transform.Find("HealthText")?.GetComponent<TextMeshProUGUI>(),
                StateText = uiObj.transform.Find("StateText")?.GetComponent<TextMeshProUGUI>(),
                Icon = uiObj.transform.Find("Icon")?.GetComponent<Image>()
            };

            // 초기 설정
            if (element.NameText != null)
            {
                element.NameText.text = member.MemberName;
            }

            if (element.Icon != null && member.MemberIcon != null)
            {
                element.Icon.sprite = member.MemberIcon;
            }

            memberUIElements.Add(element);
            UpdateMemberUI(element);

#if UNITY_EDITOR
            Debug.Log($"[SquadUI] {member.MemberName} UI 생성됨");
#endif
        }

        /// <summary>
        /// 기본 분대원 UI 생성 (프리팹 없을 때)
        /// </summary>
        private GameObject CreateDefaultMemberUI(SquadMember member)
        {
            // 루트 오브젝트
            GameObject root = new GameObject($"MemberUI_{member.MemberName}");
            root.transform.SetParent(squadMemberContainer, false);

            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(200, 50);

            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;

            // 배경
            var bg = root.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);

            // 이름 텍스트
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(root.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(60, 40);
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = member.MemberName;
            nameText.fontSize = 14;
            nameText.alignment = TextAlignmentOptions.Left;

            // 체력바 배경
            GameObject healthBgObj = new GameObject("HealthBarBG");
            healthBgObj.transform.SetParent(root.transform, false);
            var healthBgRect = healthBgObj.AddComponent<RectTransform>();
            healthBgRect.sizeDelta = new Vector2(80, 20);
            var healthBgImg = healthBgObj.AddComponent<Image>();
            healthBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // 체력바
            GameObject healthObj = new GameObject("HealthBar");
            healthObj.transform.SetParent(healthBgObj.transform, false);
            var healthRect = healthObj.AddComponent<RectTransform>();
            healthRect.anchorMin = Vector2.zero;
            healthRect.anchorMax = Vector2.one;
            healthRect.offsetMin = Vector2.zero;
            healthRect.offsetMax = Vector2.zero;
            healthRect.pivot = new Vector2(0, 0.5f);
            var healthImg = healthObj.AddComponent<Image>();
            healthImg.color = healthyColor;
            healthImg.type = Image.Type.Filled;
            healthImg.fillMethod = Image.FillMethod.Horizontal;

            // 상태 텍스트
            GameObject stateObj = new GameObject("StateText");
            stateObj.transform.SetParent(root.transform, false);
            var stateRect = stateObj.AddComponent<RectTransform>();
            stateRect.sizeDelta = new Vector2(50, 40);
            var stateText = stateObj.AddComponent<TextMeshProUGUI>();
            stateText.fontSize = 12;
            stateText.alignment = TextAlignmentOptions.Center;

            return root;
        }

        /// <summary>
        /// 모든 분대원 UI 업데이트
        /// </summary>
        private void UpdateAllMemberUI()
        {
            foreach (var element in memberUIElements)
            {
                UpdateMemberUI(element);
            }
        }

        /// <summary>
        /// 개별 분대원 UI 업데이트
        /// </summary>
        private void UpdateMemberUI(SquadMemberUIElement element)
        {
            if (element.Member == null || element.RootObject == null) return;

            // 체력바 업데이트
            if (element.HealthBar != null)
            {
                element.HealthBar.fillAmount = element.Member.HealthPercent;
                element.HealthBar.color = GetHealthColor(element.Member.HealthPercent, element.Member.IsAlive);
            }

            // 체력 텍스트 업데이트
            if (element.HealthText != null)
            {
                element.HealthText.text = $"{Mathf.CeilToInt(element.Member.HealthPercent * 100)}%";
            }

            // 상태 텍스트 업데이트
            if (element.StateText != null)
            {
                element.StateText.text = GetStateText(element.Member.CurrentState);
                element.StateText.color = element.Member.IsAlive ? Color.white : deadColor;
            }

            // 사망 시 투명도 조절
            if (!element.Member.IsAlive)
            {
                var canvasGroup = element.RootObject.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = element.RootObject.AddComponent<CanvasGroup>();
                }
                canvasGroup.alpha = 0.5f;
            }
        }

        /// <summary>
        /// 체력에 따른 색상 반환
        /// </summary>
        private Color GetHealthColor(float healthPercent, bool isAlive)
        {
            if (!isAlive) return deadColor;

            if (healthPercent > 0.6f) return healthyColor;
            if (healthPercent > 0.3f) return woundedColor;
            return criticalColor;
        }

        /// <summary>
        /// 상태 텍스트 반환
        /// </summary>
        private string GetStateText(SquadMember.SquadMemberState state)
        {
            return state switch
            {
                SquadMember.SquadMemberState.Idle => "IDLE",
                SquadMember.SquadMemberState.Following => "FOLLOW",
                SquadMember.SquadMemberState.Attacking => "ATTACK",
                SquadMember.SquadMemberState.Repositioning => "MOVE",
                SquadMember.SquadMemberState.Dead => "DEAD",
                _ => "-"
            };
        }

        #region Event Handlers

        private void OnMemberAdded(SquadMember member)
        {
            CreateMemberUI(member);
        }

        private void OnMemberDeath(SquadMember member)
        {
            var element = memberUIElements.Find(e => e.Member == member);
            if (element != null)
            {
                UpdateMemberUI(element);
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (SquadController.Instance != null)
            {
                SquadController.Instance.OnMemberAdded.RemoveListener(OnMemberAdded);
                SquadController.Instance.OnMemberDeath.RemoveListener(OnMemberDeath);
            }
        }

        /// <summary>
        /// 분대원 UI 요소 데이터 클래스
        /// </summary>
        private class SquadMemberUIElement
        {
            public SquadMember Member;
            public GameObject RootObject;
            public TextMeshProUGUI NameText;
            public Image HealthBar;
            public TextMeshProUGUI HealthText;
            public TextMeshProUGUI StateText;
            public Image Icon;
        }
    }
}
