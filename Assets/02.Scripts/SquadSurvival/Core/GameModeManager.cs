using UnityEngine;
using UnityEngine.Events;

namespace SquadSurvival.Core
{
    /// <summary>
    /// 게임 모드 관리자 - FPS/분대 서바이벌 모드 전환
    /// 기존 UI/시스템은 수정하지 않고 SetActive로 가시성만 제어
    /// </summary>
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        public enum GameMode
        {
            FPS,            // 기존 1인칭 모드
            SquadSurvival   // 분대 서바이벌 모드
        }

        [Header("현재 모드")]
        [SerializeField] private GameMode currentMode = GameMode.SquadSurvival;

        [Header("FPS Mode UI (기존 UI - 참조만)")]
        [SerializeField] private GameObject healthBar;
        [SerializeField] private GameObject staminaBar;
        [SerializeField] private GameObject ammoUI;
        [SerializeField] private GameObject grenadeUI;
        [SerializeField] private GameObject crosshair;
        [SerializeField] private GameObject minimapPanel;
        [SerializeField] private GameObject stateText;

        [Header("Squad Survival UI (새로 추가된 UI)")]
        [SerializeField] private GameObject squadSurvivalUI;

        [Header("Events")]
        public UnityEvent<GameMode> OnModeChanged;

        public GameMode CurrentMode => currentMode;
        public bool IsSquadSurvivalMode => currentMode == GameMode.SquadSurvival;
        public bool IsFPSMode => currentMode == GameMode.FPS;

        private void Awake()
        {
            // 싱글톤 설정
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // 이벤트 초기화
            if (OnModeChanged == null)
            {
                OnModeChanged = new UnityEvent<GameMode>();
            }
        }

        private void Start()
        {
            // 초기 모드 적용
            ApplyMode(currentMode);

            // 초기 모드 이벤트 발생 (구독자들에게 알림)
            OnModeChanged?.Invoke(currentMode);

#if UNITY_EDITOR
            Debug.Log($"[GameModeManager] 초기 모드: {currentMode}");
#endif
        }

        /// <summary>
        /// FPS 모드로 전환
        /// </summary>
        public void SwitchToFPSMode()
        {
            if (currentMode == GameMode.FPS) return;

            currentMode = GameMode.FPS;
            ApplyMode(currentMode);
            OnModeChanged?.Invoke(currentMode);

#if UNITY_EDITOR
            Debug.Log("[GameModeManager] FPS 모드로 전환됨");
#endif
        }

        /// <summary>
        /// 분대 서바이벌 모드로 전환
        /// </summary>
        public void SwitchToSquadSurvivalMode()
        {
            if (currentMode == GameMode.SquadSurvival) return;

            currentMode = GameMode.SquadSurvival;
            ApplyMode(currentMode);
            OnModeChanged?.Invoke(currentMode);

#if UNITY_EDITOR
            Debug.Log("[GameModeManager] 분대 서바이벌 모드로 전환됨");
#endif
        }

        /// <summary>
        /// 모드 토글 (F1 키 등으로 사용)
        /// </summary>
        public void ToggleMode()
        {
            if (currentMode == GameMode.FPS)
            {
                SwitchToSquadSurvivalMode();
            }
            else
            {
                SwitchToFPSMode();
            }
        }

        /// <summary>
        /// 모드에 따른 UI 적용
        /// </summary>
        private void ApplyMode(GameMode mode)
        {
            if (mode == GameMode.FPS)
            {
                // 기존 FPS UI 활성화
                SetUIActive(healthBar, true);
                SetUIActive(staminaBar, true);
                SetUIActive(ammoUI, true);
                SetUIActive(grenadeUI, true);
                SetUIActive(crosshair, true);
                SetUIActive(minimapPanel, true);
                SetUIActive(stateText, true);

                // 분대 서바이벌 UI 비활성화
                SetUIActive(squadSurvivalUI, false);
            }
            else // SquadSurvival
            {
                // 기존 FPS UI 유지 (1인칭이므로!)
                SetUIActive(healthBar, true);      // 플레이어 체력 표시
                SetUIActive(staminaBar, true);     // 플레이어 스태미나 표시
                SetUIActive(ammoUI, true);         // 탄약 표시
                SetUIActive(grenadeUI, true);      // 수류탄 표시
                SetUIActive(crosshair, true);      // 조준점 표시
                SetUIActive(minimapPanel, true);   // 미니맵 표시
                SetUIActive(stateText, true);      // 상태 텍스트 표시

                // 분대 서바이벌 UI 추가 활성화
                SetUIActive(squadSurvivalUI, true);
            }
        }

        /// <summary>
        /// 안전한 UI 활성화/비활성화
        /// </summary>
        private void SetUIActive(GameObject uiObject, bool active)
        {
            if (uiObject != null)
            {
                uiObject.SetActive(active);
            }
        }

        /// <summary>
        /// Canvas에서 기존 UI 자동 탐색 및 참조 설정
        /// </summary>
        [ContextMenu("Auto Find UI References")]
        public void AutoFindUIReferences()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[GameModeManager] Canvas를 찾을 수 없습니다.");
                return;
            }

            Transform canvasTransform = canvas.transform;

            // 기존 UI 참조 탐색
            healthBar = FindUIByName(canvasTransform, "HealthBar");
            staminaBar = FindUIByName(canvasTransform, "StaminaBar");
            ammoUI = FindUIByName(canvasTransform, "AmmoUI");
            grenadeUI = FindUIByName(canvasTransform, "GrenadeUI");
            crosshair = FindUIByName(canvasTransform, "Crosshair");
            minimapPanel = FindUIByName(canvasTransform, "MinimapPanel");
            stateText = FindUIByName(canvasTransform, "StateText");

            // 분대 서바이벌 UI 참조 탐색
            squadSurvivalUI = FindUIByName(canvasTransform, "SquadSurvivalUI");

#if UNITY_EDITOR
            Debug.Log("[GameModeManager] UI 참조 자동 설정 완료");
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private GameObject FindUIByName(Transform parent, string name)
        {
            Transform found = parent.Find(name);
            if (found != null)
            {
                return found.gameObject;
            }

            // 자식에서 재귀 탐색
            foreach (Transform child in parent)
            {
                GameObject result = FindUIByName(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        private void Update()
        {
            // 에디터에서 F1 키로 모드 전환 테스트
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ToggleMode();
            }
        }
#endif
    }
}
