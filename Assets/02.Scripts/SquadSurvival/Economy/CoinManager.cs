using UnityEngine;
using UnityEngine.Events;
using SquadSurvival.Core;
using System.Collections;

namespace SquadSurvival.Economy
{
    /// <summary>
    /// 코인 경제 시스템 관리자
    /// 코인 획득, 소비, 저장 관리
    /// </summary>
    public class CoinManager : MonoBehaviour
    {
        public static CoinManager Instance { get; private set; }

        [Header("코인 설정")]
        [SerializeField] private int currentCoins = 0;
        [SerializeField] private int totalEarnedCoins = 0;
        [SerializeField] private int totalSpentCoins = 0;

        [Header("적 처치 보상")]
        [SerializeField] private int baseKillReward = 10;
        [SerializeField] private float waveMultiplier = 0.1f;
        [SerializeField] private int headShotBonus = 5;
        [SerializeField] private int eliteKillBonus = 20;

        [Header("웨이브 보상")]
        [SerializeField] private int waveCompleteReward = 50;
        [SerializeField] private float waveRewardMultiplier = 1.2f;

        [Header("코인 드롭")]
        [SerializeField] private GameObject coinPickupPrefab;
        [SerializeField] private float coinDropChance = 0.3f;
        [SerializeField] private int minCoinDrop = 5;
        [SerializeField] private int maxCoinDrop = 15;

        [Header("코인 버스트 드롭")]
        [SerializeField] private bool useBurstDrop = true;
        [SerializeField, Range(1, 500), Tooltip("최소 코인 개수")]
        private int minCoinCount = 100;
        [SerializeField, Range(1, 500), Tooltip("최대 코인 개수")]
        private int maxCoinCount = 100;
        [SerializeField, Range(0.5f, 20f), Tooltip("코인이 퍼지는 반경")]
        private float burstRadius = 6f;
        [SerializeField, Range(0.5f, 5f), Tooltip("코인이 튀어오르는 높이")]
        private float burstHeight = 1.5f;
        [SerializeField, Range(0f, 0.1f), Tooltip("코인 스폰 간격 (0이면 동시 스폰)")]
        private float burstSpawnDelay = 0.01f;

        [Header("이벤트")]
        public UnityEvent<int> OnCoinsChanged;
        public UnityEvent<int> OnCoinsEarned;
        public UnityEvent<int> OnCoinsSpent;
        public UnityEvent OnInsufficientFunds;

        // 프로퍼티
        public int CurrentCoins => currentCoins;
        public int TotalEarnedCoins => totalEarnedCoins;
        public int TotalSpentCoins => totalSpentCoins;

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

            InitializeEvents();
        }

        private void Start()
        {
            // SquadSurvivalManager 이벤트 구독
            if (SquadSurvivalManager.Instance != null)
            {
                SquadSurvivalManager.Instance.OnWaveComplete.AddListener(OnWaveComplete);
                SquadSurvivalManager.Instance.OnGameStart.AddListener(OnGameStart);
            }
        }

        private void InitializeEvents()
        {
            if (OnCoinsChanged == null) OnCoinsChanged = new UnityEvent<int>();
            if (OnCoinsEarned == null) OnCoinsEarned = new UnityEvent<int>();
            if (OnCoinsSpent == null) OnCoinsSpent = new UnityEvent<int>();
            if (OnInsufficientFunds == null) OnInsufficientFunds = new UnityEvent();
        }

        /// <summary>
        /// 게임 시작 시 초기화
        /// </summary>
        private void OnGameStart()
        {
            ResetCoins();
        }

        /// <summary>
        /// 웨이브 완료 보상
        /// </summary>
        private void OnWaveComplete(int wave)
        {
            int reward = CalculateWaveReward(wave);
            AddCoins(reward, $"웨이브 {wave} 완료");

#if UNITY_EDITOR
            Debug.Log($"[CoinManager] 웨이브 {wave} 완료 보상: {reward} 코인");
#endif
        }

        /// <summary>
        /// 코인 추가
        /// </summary>
        public void AddCoins(int amount, string source = "")
        {
            if (amount <= 0) return;

            currentCoins += amount;
            totalEarnedCoins += amount;

            OnCoinsEarned?.Invoke(amount);
            OnCoinsChanged?.Invoke(currentCoins);

#if UNITY_EDITOR
            Debug.Log($"[CoinManager] +{amount} 코인 ({source}). 현재: {currentCoins}");
#endif
        }

        /// <summary>
        /// 코인 사용
        /// </summary>
        public bool SpendCoins(int amount, string purpose = "")
        {
            if (amount <= 0) return false;

            if (currentCoins < amount)
            {
                OnInsufficientFunds?.Invoke();

#if UNITY_EDITOR
                Debug.Log($"[CoinManager] 코인 부족! 필요: {amount}, 보유: {currentCoins}");
#endif
                return false;
            }

            currentCoins -= amount;
            totalSpentCoins += amount;

            OnCoinsSpent?.Invoke(amount);
            OnCoinsChanged?.Invoke(currentCoins);

#if UNITY_EDITOR
            Debug.Log($"[CoinManager] -{amount} 코인 ({purpose}). 현재: {currentCoins}");
#endif
            return true;
        }

        /// <summary>
        /// 구매 가능 여부 확인
        /// </summary>
        public bool CanAfford(int cost)
        {
            return currentCoins >= cost;
        }

        /// <summary>
        /// 적 처치 보상 계산
        /// </summary>
        public int CalculateKillReward(bool isHeadshot = false, bool isElite = false)
        {
            int currentWave = SquadSurvivalManager.Instance != null 
                ? SquadSurvivalManager.Instance.CurrentWave 
                : 1;

            int reward = baseKillReward;
            
            // 웨이브 보너스
            reward += Mathf.RoundToInt(baseKillReward * waveMultiplier * (currentWave - 1));

            // 헤드샷 보너스
            if (isHeadshot)
            {
                reward += headShotBonus;
            }

            // 엘리트 보너스
            if (isElite)
            {
                reward += eliteKillBonus;
            }

            return reward;
        }

        /// <summary>
        /// 웨이브 완료 보상 계산
        /// </summary>
        public int CalculateWaveReward(int wave)
        {
            return Mathf.RoundToInt(waveCompleteReward * Mathf.Pow(waveRewardMultiplier, wave - 1));
        }

        /// <summary>
        /// 적 처치 시 코인 보상
        /// </summary>
        public void RewardKill(bool isHeadshot = false, bool isElite = false, Vector3? dropPosition = null)
        {
            int reward = CalculateKillReward(isHeadshot, isElite);
            AddCoins(reward, isElite ? "엘리트 처치" : "적 처치");

            // 코인 드롭
            if (dropPosition.HasValue && ShouldDropCoin())
            {
                SpawnCoinPickup(dropPosition.Value);
            }
        }

        /// <summary>
        /// 코인 드롭 여부 결정
        /// </summary>
        private bool ShouldDropCoin()
        {
            return Random.value < coinDropChance;
        }

        /// <summary>
        /// 코인 픽업 스폰
        /// </summary>
        public void SpawnCoinPickup(Vector3 position, int amount = 0)
        {
            if (coinPickupPrefab == null) return;

            if (amount <= 0)
            {
                amount = Random.Range(minCoinDrop, maxCoinDrop + 1);
            }

            // 버스트 드롭 사용 시
            if (useBurstDrop)
            {
                SpawnCoinBurst(position, amount);
                return;
            }

            // 단일 코인 드롭
            GameObject coinObj = Instantiate(coinPickupPrefab, position + Vector3.up * 0.5f, Quaternion.identity);

            var coinPickup = coinObj.GetComponent<CoinPickup>();
            if (coinPickup != null)
            {
                coinPickup.SetAmount(amount);
            }
        }

        /// <summary>
        /// 코인 버스트 드롭 (여러 코인이 퍼져나감)
        /// </summary>
        private void SpawnCoinBurst(Vector3 position, int totalAmount)
        {
            int coinCount = Random.Range(minCoinCount, maxCoinCount + 1);

            // 순차 스폰 또는 동시 스폰
            if (burstSpawnDelay > 0f)
            {
                StartCoroutine(SpawnCoinBurstCoroutine(position, totalAmount, coinCount));
            }
            else
            {
                SpawnCoinsImmediate(position, totalAmount, coinCount);
            }

#if UNITY_EDITOR
            Debug.Log($"[CoinManager] 코인 버스트 드롭: {coinCount}개, 총 {totalAmount} 코인");
#endif
        }

        /// <summary>
        /// 코인 즉시 스폰 (모든 코인 동시에)
        /// </summary>
        private void SpawnCoinsImmediate(Vector3 position, int totalAmount, int coinCount)
        {
            int baseAmount = totalAmount / coinCount;
            int remainder = totalAmount % coinCount;

            for (int i = 0; i < coinCount; i++)
            {
                SpawnSingleCoin(position, baseAmount + (i == 0 ? remainder : 0));
            }
        }

        /// <summary>
        /// 코인 순차 스폰 코루틴 (프레임 분산)
        /// </summary>
        private IEnumerator SpawnCoinBurstCoroutine(Vector3 position, int totalAmount, int coinCount)
        {
            int baseAmount = totalAmount / coinCount;
            int remainder = totalAmount % coinCount;

            for (int i = 0; i < coinCount; i++)
            {
                SpawnSingleCoin(position, baseAmount + (i == 0 ? remainder : 0));

                if (burstSpawnDelay > 0f)
                {
                    yield return new WaitForSeconds(burstSpawnDelay);
                }
            }
        }

        /// <summary>
        /// 단일 코인 스폰
        /// </summary>
        private void SpawnSingleCoin(Vector3 position, int amount)
        {
            // 랜덤 방향으로 퍼지는 위치 계산
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(0.3f, burstRadius);

            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );

            Vector3 spawnPos = position + Vector3.up * 0.5f;
            Vector3 targetPos = position + offset + Vector3.up * 0.3f;

            // 코인 생성
            GameObject coinObj = Instantiate(coinPickupPrefab, spawnPos, Quaternion.identity);

            var coinPickup = coinObj.GetComponent<CoinPickup>();
            if (coinPickup != null)
            {
                amount = Mathf.Max(1, amount); // 최소 1
                coinPickup.SetAmount(amount);
                coinPickup.SetBurstTarget(targetPos, burstHeight);
            }
        }

        /// <summary>
        /// 코인 초기화
        /// </summary>
        public void ResetCoins()
        {
            currentCoins = 0;
            totalEarnedCoins = 0;
            totalSpentCoins = 0;
            OnCoinsChanged?.Invoke(currentCoins);

#if UNITY_EDITOR
            Debug.Log("[CoinManager] 코인 초기화됨");
#endif
        }

        /// <summary>
        /// 코인 설정 (치트/디버그용)
        /// </summary>
        public void SetCoins(int amount)
        {
            currentCoins = Mathf.Max(0, amount);
            OnCoinsChanged?.Invoke(currentCoins);
        }

        private void OnDestroy()
        {
            if (SquadSurvivalManager.Instance != null)
            {
                SquadSurvivalManager.Instance.OnWaveComplete.RemoveListener(OnWaveComplete);
                SquadSurvivalManager.Instance.OnGameStart.RemoveListener(OnGameStart);
            }
        }
    }
}
