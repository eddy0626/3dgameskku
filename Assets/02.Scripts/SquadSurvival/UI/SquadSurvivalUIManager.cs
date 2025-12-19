using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SquadSurvival.Core;

namespace SquadSurvival.UI
{
    /// <summary>
    /// 분대 서바이벌 UI 총괄 관리자
    /// 모든 UI 패널과 텍스트 요소를 관리
    /// </summary>
    public class SquadSurvivalUIManager : MonoBehaviour
    {
        public static SquadSurvivalUIManager Instance { get; private set; }

        [Header("메인 컨테이너")]
        [SerializeField] private GameObject squadSurvivalUI;

        [Header("웨이브 패널")]
        [SerializeField] private GameObject wavePanel;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI enemyCountText;

        [Header("코인 UI")]
        [SerializeField] private GameObject coinUI;
        [SerializeField] private Image coinIcon;
        [SerializeField] private TextMeshProUGUI coinText;

        [Header("분대 상태 패널")]
        [SerializeField] private GameObject squadStatusPanel;
        [SerializeField] private GameObject[] squadMemberSlots;

        [Header("업그레이드 버튼")]
        [SerializeField] private Button upgradeButton;

        [Header("게임오버 패널")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;

        [Header("설정")]
        [SerializeField] private Color victoryColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color defeatColor = new Color(0.8f, 0.2f, 0.2f);

        // 현재 코인
        private int currentCoins = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // SquadSurvivalManager 이벤트 구독
            if (SquadSurvivalManager.Instance != null)
            {
                SquadSurvivalManager.Instance.OnWaveStart.AddListener(OnWaveStart);
                SquadSurvivalManager.Instance.OnWaveComplete.AddListener(OnWaveComplete);
                SquadSurvivalManager.Instance.OnStateChanged.AddListener(OnStateChanged);
                SquadSurvivalManager.Instance.OnEnemyCountChanged.AddListener(OnEnemyCountChanged);
                SquadSurvivalManager.Instance.OnGameStart.AddListener(OnGameStart);
                SquadSurvivalManager.Instance.OnGameEnd.AddListener(OnGameEnd);
            }

            // 버튼 이벤트 연결
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.AddListener(OnExitClicked);
            }

            if (upgradeButton != null)
            {
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            // 초기 UI 상태
            InitializeUI();
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            UpdateWaveText(0);
            UpdateEnemyCount(0, 0);
            UpdateCoinText(0);
            HideGameOverPanel();
        }

        /// <summary>
        /// 웨이브 텍스트 업데이트
        /// </summary>
        public void UpdateWaveText(int wave)
        {
            if (waveText != null)
            {
                if (wave <= 0)
                {
                    waveText.text = "READY";
                }
                else
                {
                    waveText.text = $"WAVE {wave}";
                }
            }
        }

        /// <summary>
        /// 적 카운트 업데이트
        /// </summary>
        public void UpdateEnemyCount(int killed, int total)
        {
            if (enemyCountText != null)
            {
                if (total <= 0)
                {
                    enemyCountText.text = "적: -/-";
                }
                else
                {
                    enemyCountText.text = $"적: {killed}/{total}";
                }
            }
        }

        /// <summary>
        /// 코인 텍스트 업데이트
        /// </summary>
        public void UpdateCoinText(int coins)
        {
            currentCoins = coins;
            if (coinText != null)
            {
                coinText.text = coins.ToString("N0");
            }
        }

        /// <summary>
        /// 코인 추가
        /// </summary>
        public void AddCoins(int amount)
        {
            currentCoins += amount;
            UpdateCoinText(currentCoins);
        }

        /// <summary>
        /// 게임오버 패널 표시
        /// </summary>
        public void ShowGameOverPanel(bool isVictory)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);

                if (resultText != null)
                {
                    resultText.text = isVictory ? "VICTORY" : "DEFEAT";
                    resultText.color = isVictory ? victoryColor : defeatColor;
                }

                // 통계 업데이트
                UpdateStatsPanel();

                // 커서 표시
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        /// <summary>
        /// 게임오버 패널 숨기기
        /// </summary>
        public void HideGameOverPanel()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 통계 패널 업데이트
        /// </summary>
        private void UpdateStatsPanel()
        {
            if (statsPanel == null || SquadSurvivalManager.Instance == null) return;

            // StatsPanel 내부에 텍스트가 있다면 업데이트
            var statsText = statsPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (statsText != null)
            {
                var manager = SquadSurvivalManager.Instance;
                statsText.text = $"처치: {manager.TotalKills}\n" +
                                 $"웨이브: {manager.CurrentWave}/{manager.MaxWaves}\n" +
                                 $"코인: {manager.TotalCoinsEarned}\n" +
                                 $"생존: {FormatTime(manager.SurvivalTime)}";
            }
        }

        /// <summary>
        /// 시간 포맷팅
        /// </summary>
        private string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }

        /// <summary>
        /// 분대원 상태 업데이트
        /// </summary>
        public void UpdateSquadMemberStatus(int memberIndex, float healthPercent, bool isAlive)
        {
            if (squadMemberSlots == null || memberIndex < 0 || memberIndex >= squadMemberSlots.Length)
                return;

            var slot = squadMemberSlots[memberIndex];
            if (slot == null) return;

            // 활성화/비활성화
            slot.SetActive(isAlive);

            // 체력바 업데이트 (있다면)
            var healthBar = slot.GetComponentInChildren<Slider>();
            if (healthBar != null)
            {
                healthBar.value = healthPercent;
            }

            // 색상으로 상태 표시
            var image = slot.GetComponent<Image>();
            if (image != null)
            {
                if (!isAlive)
                {
                    image.color = Color.gray;
                }
                else if (healthPercent < 0.3f)
                {
                    image.color = Color.red;
                }
                else if (healthPercent < 0.6f)
                {
                    image.color = Color.yellow;
                }
                else
                {
                    image.color = Color.green;
                }
            }
        }

        #region Event Handlers

        private void OnWaveStart(int wave)
        {
            UpdateWaveText(wave);
            
            if (SquadSurvivalManager.Instance != null)
            {
                UpdateEnemyCount(0, SquadSurvivalManager.Instance.TotalEnemiesThisWave);
            }

#if UNITY_EDITOR
            Debug.Log($"[SquadSurvivalUIManager] 웨이브 {wave} 시작 UI 업데이트");
#endif
        }

        private void OnWaveComplete(int wave)
        {
            // 웨이브 완료 UI 효과 (추후 구현 가능)
#if UNITY_EDITOR
            Debug.Log($"[SquadSurvivalUIManager] 웨이브 {wave} 완료 UI 업데이트");
#endif
        }

        private void OnStateChanged(SquadSurvivalManager.GameState newState)
        {
            switch (newState)
            {
                case SquadSurvivalManager.GameState.GameOver:
                    ShowGameOverPanel(false);
                    break;

                case SquadSurvivalManager.GameState.Victory:
                    ShowGameOverPanel(true);
                    break;

                case SquadSurvivalManager.GameState.Preparing:
                    HideGameOverPanel();
                    if (SquadSurvivalManager.Instance != null && SquadSurvivalManager.Instance.CurrentWave == 0)
                    {
                        UpdateWaveText(0);
                    }
                    break;
            }
        }

        private void OnEnemyCountChanged(int killed, int total)
        {
            UpdateEnemyCount(killed, total);
        }

        private void OnGameStart()
        {
            InitializeUI();
#if UNITY_EDITOR
            Debug.Log("[SquadSurvivalUIManager] 게임 시작 - UI 초기화");
#endif
        }

        private void OnGameEnd()
        {
#if UNITY_EDITOR
            Debug.Log("[SquadSurvivalUIManager] 게임 종료");
#endif
        }

        private void OnRestartClicked()
        {
            HideGameOverPanel();

            // 커서 숨기기
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // 게임 재시작
            if (SquadSurvivalManager.Instance != null)
            {
                SquadSurvivalManager.Instance.RestartGame();
            }
        }

        private void OnExitClicked()
        {
            HideGameOverPanel();

            // FPS 모드로 전환
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SwitchToFPSMode();
            }

            // 커서 숨기기
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnUpgradeClicked()
        {
            // 업그레이드 시스템 열기 (Phase 5에서 구현)
#if UNITY_EDITOR
            Debug.Log("[SquadSurvivalUIManager] 업그레이드 버튼 클릭");
#endif
        }

        #endregion

        /// <summary>
        /// Canvas에서 UI 참조 자동 탐색
        /// </summary>
        [ContextMenu("Auto Find UI References")]
        public void AutoFindUIReferences()
        {
            // SquadSurvivalUI 찾기
            if (squadSurvivalUI == null)
            {
                squadSurvivalUI = GameObject.Find("SquadSurvivalUI");
            }

            if (squadSurvivalUI == null)
            {
                Debug.LogWarning("[SquadSurvivalUIManager] SquadSurvivalUI를 찾을 수 없습니다.");
                return;
            }

            Transform root = squadSurvivalUI.transform;

            // WavePanel
            wavePanel = FindChild(root, "WavePanel");
            if (wavePanel != null)
            {
                waveText = FindChild(wavePanel.transform, "WaveText")?.GetComponent<TextMeshProUGUI>();
                enemyCountText = FindChild(wavePanel.transform, "EnemyCountText")?.GetComponent<TextMeshProUGUI>();
            }

            // CoinUI
            coinUI = FindChild(root, "CoinUI");
            if (coinUI != null)
            {
                var coinIconObj = FindChild(coinUI.transform, "CoinIcon");
                coinIcon = coinIconObj?.GetComponent<Image>();
                coinText = FindChild(coinUI.transform, "CoinText")?.GetComponent<TextMeshProUGUI>();
            }

            // SquadStatusPanel
            squadStatusPanel = FindChild(root, "SquadStatusPanel");
            if (squadStatusPanel != null)
            {
                squadMemberSlots = new GameObject[4];
                for (int i = 0; i < 4; i++)
                {
                    squadMemberSlots[i] = FindChild(squadStatusPanel.transform, $"SquadMember{i + 1}");
                }
            }

            // UpgradeButton
            var upgradeObj = FindChild(root, "UpgradeButton");
            upgradeButton = upgradeObj?.GetComponent<Button>();

            // GameOverPanel
            gameOverPanel = FindChild(root, "GameOverPanel");
            if (gameOverPanel != null)
            {
                resultText = FindChild(gameOverPanel.transform, "ResultText")?.GetComponent<TextMeshProUGUI>();
                statsPanel = FindChild(gameOverPanel.transform, "StatsPanel");
                restartButton = FindChild(gameOverPanel.transform, "RestartButton")?.GetComponent<Button>();
                exitButton = FindChild(gameOverPanel.transform, "ExitButton")?.GetComponent<Button>();
            }

#if UNITY_EDITOR
            Debug.Log("[SquadSurvivalUIManager] UI 참조 자동 설정 완료");
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private GameObject FindChild(Transform parent, string name)
        {
            Transform found = parent.Find(name);
            return found?.gameObject;
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (SquadSurvivalManager.Instance != null)
            {
                SquadSurvivalManager.Instance.OnWaveStart.RemoveListener(OnWaveStart);
                SquadSurvivalManager.Instance.OnWaveComplete.RemoveListener(OnWaveComplete);
                SquadSurvivalManager.Instance.OnStateChanged.RemoveListener(OnStateChanged);
                SquadSurvivalManager.Instance.OnEnemyCountChanged.RemoveListener(OnEnemyCountChanged);
                SquadSurvivalManager.Instance.OnGameStart.RemoveListener(OnGameStart);
                SquadSurvivalManager.Instance.OnGameEnd.RemoveListener(OnGameEnd);
            }
        }
    }
}
