using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace SquadSurvival.Core
{
    /// <summary>
    /// 분대 서바이벌 모드의 핵심 관리자
    /// 웨이브, 분대원, 경제 시스템을 총괄 관리
    /// </summary>
    public class SquadSurvivalManager : MonoBehaviour
    {
        public static SquadSurvivalManager Instance { get; private set; }

        public enum GameState
        {
            Idle,           // 대기 상태 (모드 비활성)
            Preparing,      // 웨이브 준비 중
            InWave,         // 웨이브 진행 중
            WaveComplete,   // 웨이브 완료 (휴식 시간)
            GameOver,       // 게임 오버
            Victory         // 승리
        }

        [Header("게임 상태")]
        [SerializeField] private GameState currentState = GameState.Idle;
        [SerializeField] private bool isActive = false;
        [SerializeField] private bool autoStart = true; // 자동 시작 여부

        [Header("웨이브 설정")]
        [SerializeField] private int currentWave = 0;
        [SerializeField] private int maxWaves = 10;
        [SerializeField] private float wavePrepareTime = 5f;
        [SerializeField] private float waveRestTime = 10f;

        [Header("적 설정")]
        [SerializeField] private int baseEnemyCount = 10;
        [SerializeField] private float enemyCountMultiplier = 1.2f;
        [SerializeField] private int currentEnemyCount = 0;
        [SerializeField] private int totalEnemiesThisWave = 0;
        [SerializeField] private int enemiesKilledThisWave = 0;

        [Header("플레이어 참조")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private GameObject playerObject;

        [Header("통계")]
        [SerializeField] private int totalKills = 0;
        [SerializeField] private int totalCoinsEarned = 0;
        [SerializeField] private float survivalTime = 0f;

        [Header("이벤트")]
        public UnityEvent OnGameStart;
        public UnityEvent OnGameEnd;
        public UnityEvent<int> OnWaveStart;
        public UnityEvent<int> OnWaveComplete;
        public UnityEvent<GameState> OnStateChanged;
        public UnityEvent<int, int> OnEnemyCountChanged; // (killed, total)

        // 프로퍼티
        public GameState CurrentState => currentState;
        public bool IsActive => isActive;
        public int CurrentWave => currentWave;
        public int MaxWaves => maxWaves;
        public int CurrentEnemyCount => currentEnemyCount;
        public int TotalEnemiesThisWave => totalEnemiesThisWave;
        public int EnemiesKilledThisWave => enemiesKilledThisWave;
        public int TotalKills => totalKills;
        public int TotalCoinsEarned => totalCoinsEarned;
        public float SurvivalTime => survivalTime;
        public Transform PlayerTransform => playerTransform;

        private float stateTimer = 0f;

        private void Awake()
        {
            // 싱글톤 설정
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeEvents();
        }

        private void Start()
        {
            // 플레이어 참조 자동 설정
            if (playerObject == null)
            {
                playerObject = GameObject.FindGameObjectWithTag("Player");
            }

            if (playerObject != null && playerTransform == null)
            {
                playerTransform = playerObject.transform;
            }

            // GameModeManager와 연동
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.OnModeChanged.AddListener(OnGameModeChanged);

                // 현재 모드가 SquadSurvival이면 바로 시작
                if (GameModeManager.Instance.CurrentMode == GameModeManager.GameMode.SquadSurvival)
                {
                    StartSquadSurvival();
                }
            }

            // autoStart가 true면 바로 시작 (테스트용)
            if (autoStart && !isActive)
            {
#if UNITY_EDITOR
                Debug.Log("[SquadSurvivalManager] autoStart 활성화됨 - 분대 서바이벌 모드 자동 시작");
#endif
                StartSquadSurvival();
            }
        }

        private void Update()
        {
            if (!isActive) return;

            // 생존 시간 업데이트
            if (currentState == GameState.InWave || currentState == GameState.WaveComplete)
            {
                survivalTime += Time.deltaTime;
            }

            // 상태별 업데이트
            UpdateState();
        }

        private void InitializeEvents()
        {
            if (OnGameStart == null) OnGameStart = new UnityEvent();
            if (OnGameEnd == null) OnGameEnd = new UnityEvent();
            if (OnWaveStart == null) OnWaveStart = new UnityEvent<int>();
            if (OnWaveComplete == null) OnWaveComplete = new UnityEvent<int>();
            if (OnStateChanged == null) OnStateChanged = new UnityEvent<GameState>();
            if (OnEnemyCountChanged == null) OnEnemyCountChanged = new UnityEvent<int, int>();
        }

        /// <summary>
        /// 게임 모드 변경 시 호출
        /// </summary>
        private void OnGameModeChanged(GameModeManager.GameMode mode)
        {
            if (mode == GameModeManager.GameMode.SquadSurvival)
            {
                StartSquadSurvival();
            }
            else
            {
                StopSquadSurvival();
            }
        }

        /// <summary>
        /// 분대 서바이벌 모드 시작
        /// </summary>
        public void StartSquadSurvival()
        {
            if (isActive) return;

            isActive = true;
            ResetStats();
            SetState(GameState.Preparing);
            OnGameStart?.Invoke();

#if UNITY_EDITOR
            Debug.Log("[SquadSurvivalManager] 분대 서바이벌 모드 시작!");
#endif
        }

        /// <summary>
        /// 분대 서바이벌 모드 중지
        /// </summary>
        public void StopSquadSurvival()
        {
            if (!isActive) return;

            isActive = false;
            SetState(GameState.Idle);
            OnGameEnd?.Invoke();

#if UNITY_EDITOR
            Debug.Log("[SquadSurvivalManager] 분대 서바이벌 모드 종료");
#endif
        }

        /// <summary>
        /// 상태 업데이트
        /// </summary>
        private void UpdateState()
        {
            stateTimer += Time.deltaTime;

            switch (currentState)
            {
                case GameState.Preparing:
                    if (stateTimer >= wavePrepareTime)
                    {
                        StartNextWave();
                    }
                    break;

                case GameState.InWave:
                    // 모든 적 처치 확인
                    if (enemiesKilledThisWave >= totalEnemiesThisWave && currentEnemyCount <= 0)
                    {
                        CompleteWave();
                    }
                    break;

                case GameState.WaveComplete:
                    if (stateTimer >= waveRestTime)
                    {
                        // 마지막 웨이브 확인
                        if (currentWave >= maxWaves)
                        {
                            SetState(GameState.Victory);
                        }
                        else
                        {
                            SetState(GameState.Preparing);
                        }
                    }
                    break;

                case GameState.GameOver:
                case GameState.Victory:
                    // 게임 종료 상태 - 추가 처리 없음
                    break;
            }
        }

        /// <summary>
        /// 다음 웨이브 시작
        /// </summary>
        private void StartNextWave()
        {
            currentWave++;
            enemiesKilledThisWave = 0;
            totalEnemiesThisWave = CalculateEnemyCount(currentWave);
            currentEnemyCount = 0;

            SetState(GameState.InWave);
            OnWaveStart?.Invoke(currentWave);

#if UNITY_EDITOR
            Debug.Log($"[SquadSurvivalManager] 웨이브 {currentWave} 시작! 적: {totalEnemiesThisWave}");
#endif
        }

        /// <summary>
        /// 웨이브 완료
        /// </summary>
        private void CompleteWave()
        {
            SetState(GameState.WaveComplete);
            OnWaveComplete?.Invoke(currentWave);

#if UNITY_EDITOR
            Debug.Log($"[SquadSurvivalManager] 웨이브 {currentWave} 완료!");
#endif
        }

        /// <summary>
        /// 웨이브별 적 수 계산
        /// </summary>
        private int CalculateEnemyCount(int wave)
        {
            return Mathf.RoundToInt(baseEnemyCount * Mathf.Pow(enemyCountMultiplier, wave - 1));
        }

        /// <summary>
        /// 상태 변경
        /// </summary>
        private void SetState(GameState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            stateTimer = 0f;
            OnStateChanged?.Invoke(newState);

#if UNITY_EDITOR
            Debug.Log($"[SquadSurvivalManager] 상태 변경: {newState}");
#endif
        }

        /// <summary>
        /// 적 스폰 시 호출
        /// </summary>
        public void OnEnemySpawned()
        {
            currentEnemyCount++;
            OnEnemyCountChanged?.Invoke(enemiesKilledThisWave, totalEnemiesThisWave);
        }

        /// <summary>
        /// 적 처치 시 호출
        /// </summary>
        public void OnEnemyKilled(int coinReward = 10)
        {
            currentEnemyCount--;
            enemiesKilledThisWave++;
            totalKills++;
            totalCoinsEarned += coinReward;

            OnEnemyCountChanged?.Invoke(enemiesKilledThisWave, totalEnemiesThisWave);

#if UNITY_EDITOR
            Debug.Log($"[SquadSurvivalManager] 적 처치! ({enemiesKilledThisWave}/{totalEnemiesThisWave})");
#endif
        }

        /// <summary>
        /// 플레이어 사망 시 호출
        /// </summary>
        public void OnPlayerDeath()
        {
            if (currentState == GameState.GameOver) return;

            SetState(GameState.GameOver);

#if UNITY_EDITOR
            Debug.Log("[SquadSurvivalManager] 플레이어 사망 - 게임 오버!");
#endif
        }

        /// <summary>
        /// 통계 초기화
        /// </summary>
        private void ResetStats()
        {
            currentWave = 0;
            currentEnemyCount = 0;
            totalEnemiesThisWave = 0;
            enemiesKilledThisWave = 0;
            totalKills = 0;
            totalCoinsEarned = 0;
            survivalTime = 0f;
            stateTimer = 0f;
        }

        /// <summary>
        /// 게임 재시작
        /// </summary>
        public void RestartGame()
        {
            ResetStats();
            SetState(GameState.Preparing);
            OnGameStart?.Invoke();
        }

        /// <summary>
        /// 현재 웨이브 준비 시간 반환
        /// </summary>
        public float GetPrepareTimeRemaining()
        {
            if (currentState != GameState.Preparing) return 0f;
            return Mathf.Max(0f, wavePrepareTime - stateTimer);
        }

        /// <summary>
        /// 현재 휴식 시간 반환
        /// </summary>
        public float GetRestTimeRemaining()
        {
            if (currentState != GameState.WaveComplete) return 0f;
            return Mathf.Max(0f, waveRestTime - stateTimer);
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.OnModeChanged.RemoveListener(OnGameModeChanged);
            }
        }
    }
}
