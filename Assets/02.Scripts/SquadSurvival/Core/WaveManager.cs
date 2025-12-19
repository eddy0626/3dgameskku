using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace SquadSurvival.Core
{
    /// <summary>
    /// 웨이브 시스템 관리자
    /// 적 스폰, 웨이브 진행, 난이도 조절 담당
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [System.Serializable]
        public class EnemySpawnInfo
        {
            public GameObject prefab;
            public string enemyName;
            [Range(0f, 1f)]
            public float spawnWeight = 1f;
            public int minWaveToSpawn = 1;
        }

        [System.Serializable]
        public class WaveConfig
        {
            public int waveNumber;
            public int baseEnemyCount = 10;
            public float spawnInterval = 2f;
            public float difficultyMultiplier = 1f;
        }

        [Header("적 프리팹")]
        [SerializeField] private List<EnemySpawnInfo> enemyPrefabs = new List<EnemySpawnInfo>();

        [Header("스폰 설정")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float minSpawnDistance = 15f;
        [SerializeField] private float maxSpawnDistance = 30f;
        [SerializeField] private float spawnHeightOffset = 0.5f;

        [Header("웨이브 설정")]
        [SerializeField] private int baseEnemyCount = 10;
        [SerializeField] private float enemyCountGrowth = 1.2f;
        [SerializeField] private float baseSpawnInterval = 2f;
        [SerializeField] private float minSpawnInterval = 0.5f;
        [SerializeField] private float spawnIntervalReduction = 0.1f;

        [Header("난이도 설정")]
        [SerializeField] private float healthMultiplierPerWave = 1.1f;
        [SerializeField] private float damageMultiplierPerWave = 1.05f;
        [SerializeField] private int maxSimultaneousEnemies = 30;

        [Header("이벤트")]
        public UnityEvent<GameObject> OnEnemySpawned;
        public UnityEvent<int> OnSpawnComplete; // 웨이브의 모든 적 스폰 완료

        // 현재 상태
        private int currentWave = 0;
        private int enemiesToSpawn = 0;
        private int enemiesSpawned = 0;
        private bool isSpawning = false;
        private Transform playerTransform;
        private List<GameObject> activeEnemies = new List<GameObject>();
        private Coroutine spawnCoroutine;

        // 프로퍼티
        public int CurrentWave => currentWave;
        public int EnemiesToSpawn => enemiesToSpawn;
        public int EnemiesSpawned => enemiesSpawned;
        public bool IsSpawning => isSpawning;
        public int ActiveEnemyCount => activeEnemies.Count;

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

            if (OnEnemySpawned == null) OnEnemySpawned = new UnityEvent<GameObject>();
            if (OnSpawnComplete == null) OnSpawnComplete = new UnityEvent<int>();
        }

        private void Start()
        {
            // 플레이어 참조
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }

            // SquadSurvivalManager 이벤트 구독
            if (SquadSurvivalManager.Instance != null)
            {
                SquadSurvivalManager.Instance.OnWaveStart.AddListener(OnWaveStart);
                SquadSurvivalManager.Instance.OnGameEnd.AddListener(OnGameEnd);
            }

            // 스폰 포인트가 없으면 자동 생성
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                CreateDefaultSpawnPoints();
            }

            // 적 프리팹이 없으면 자동 로드
            if (enemyPrefabs == null || enemyPrefabs.Count == 0)
            {
                AutoLoadEnemyPrefabs();
            }
        }

        /// <summary>
        /// 적 프리팹 자동 로드 (Resources 또는 하드코딩된 경로에서)
        /// </summary>
        private void AutoLoadEnemyPrefabs()
        {
            enemyPrefabs = new List<EnemySpawnInfo>();

            // Resources 폴더에서 적 프리팹 찾기
            GameObject[] enemyResources = Resources.LoadAll<GameObject>("Enemies");
            foreach (var prefab in enemyResources)
            {
                enemyPrefabs.Add(new EnemySpawnInfo
                {
                    prefab = prefab,
                    enemyName = prefab.name,
                    spawnWeight = 1f,
                    minWaveToSpawn = 1
                });
            }

            // Resources에서 못 찾으면 에디터에서 프리팹 찾기
            if (enemyPrefabs.Count == 0)
            {
#if UNITY_EDITOR
                // 에디터에서는 AssetDatabase 사용 - Enemy 폴더에서만 검색
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/03.Prefabs/Enemy" });
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    // UI 프리팹 제외, 실제 적 프리팹만 포함
                    if (prefab != null && !path.Contains("UI") && prefab.GetComponent<EnemyHealth>() != null)
                    {
                        enemyPrefabs.Add(new EnemySpawnInfo
                        {
                            prefab = prefab,
                            enemyName = prefab.name,
                            spawnWeight = 1f,
                            minWaveToSpawn = 1
                        });
                    }
                }
#endif
            }

#if UNITY_EDITOR
            if (enemyPrefabs.Count > 0)
            {
                Debug.Log($"[WaveManager] 적 프리팹 {enemyPrefabs.Count}개 자동 로드됨");
            }
            else
            {
                Debug.LogWarning("[WaveManager] 적 프리팹을 찾을 수 없습니다. Assets/03.Prefabs/Enemy 폴더를 확인하세요.");
            }
#endif
        }

        /// <summary>
        /// 웨이브 시작 시 호출
        /// </summary>
        private void OnWaveStart(int wave)
        {
            currentWave = wave;
            StartWaveSpawn(wave);
        }

        /// <summary>
        /// 게임 종료 시 호출
        /// </summary>
        private void OnGameEnd()
        {
            StopSpawning();
            ClearAllEnemies();
        }

        /// <summary>
        /// 웨이브 스폰 시작
        /// </summary>
        public void StartWaveSpawn(int wave)
        {
            if (isSpawning)
            {
                StopSpawning();
            }

            currentWave = wave;
            enemiesToSpawn = CalculateEnemyCount(wave);
            enemiesSpawned = 0;

            spawnCoroutine = StartCoroutine(SpawnWaveCoroutine());

#if UNITY_EDITOR
            Debug.Log($"[WaveManager] 웨이브 {wave} 스폰 시작: {enemiesToSpawn}명");
#endif
        }

        /// <summary>
        /// 스폰 중지
        /// </summary>
        public void StopSpawning()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            isSpawning = false;
        }

        /// <summary>
        /// 웨이브 스폰 코루틴
        /// </summary>
        private IEnumerator SpawnWaveCoroutine()
        {
            isSpawning = true;
            float spawnInterval = CalculateSpawnInterval(currentWave);

            while (enemiesSpawned < enemiesToSpawn)
            {
                // 동시 적 수 제한
                if (activeEnemies.Count < maxSimultaneousEnemies)
                {
                    SpawnEnemy();
                    enemiesSpawned++;
                }

                yield return new WaitForSeconds(spawnInterval);
            }

            isSpawning = false;
            OnSpawnComplete?.Invoke(currentWave);

#if UNITY_EDITOR
            Debug.Log($"[WaveManager] 웨이브 {currentWave} 스폰 완료");
#endif
        }

        /// <summary>
        /// 적 스폰
        /// </summary>
        private void SpawnEnemy()
        {
            GameObject prefab = SelectEnemyPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("[WaveManager] 스폰할 적 프리팹이 없습니다.");
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition();
            Quaternion spawnRotation = GetSpawnRotation(spawnPosition);

            GameObject enemy = Instantiate(prefab, spawnPosition, spawnRotation);
            
            // 난이도 적용
            ApplyDifficultyModifiers(enemy);

            // 활성 적 리스트에 추가
            activeEnemies.Add(enemy);

            // 적 사망 이벤트 연결
            SetupEnemyDeathCallback(enemy);

            // SquadSurvivalManager에 알림
            if (SquadSurvivalManager.Instance != null)
            {
                SquadSurvivalManager.Instance.OnEnemySpawned();
            }

            OnEnemySpawned?.Invoke(enemy);

#if UNITY_EDITOR
            Debug.Log($"[WaveManager] 적 스폰: {enemy.name} at {spawnPosition}");
#endif
        }

        /// <summary>
        /// 적 프리팹 선택 (가중치 기반)
        /// </summary>
        private GameObject SelectEnemyPrefab()
        {
            if (enemyPrefabs == null || enemyPrefabs.Count == 0)
            {
                return null;
            }

            // 현재 웨이브에서 스폰 가능한 적만 필터링
            List<EnemySpawnInfo> availableEnemies = new List<EnemySpawnInfo>();
            float totalWeight = 0f;

            foreach (var info in enemyPrefabs)
            {
                if (info.prefab != null && currentWave >= info.minWaveToSpawn)
                {
                    availableEnemies.Add(info);
                    totalWeight += info.spawnWeight;
                }
            }

            if (availableEnemies.Count == 0)
            {
                return enemyPrefabs[0]?.prefab;
            }

            // 가중치 기반 랜덤 선택
            float randomValue = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var info in availableEnemies)
            {
                cumulative += info.spawnWeight;
                if (randomValue <= cumulative)
                {
                    return info.prefab;
                }
            }

            return availableEnemies[0].prefab;
        }

        /// <summary>
        /// 스폰 위치 계산
        /// </summary>
        private Vector3 GetSpawnPosition()
        {
            // 스폰 포인트가 있으면 사용
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (randomPoint != null)
                {
                    return randomPoint.position + Vector3.up * spawnHeightOffset;
                }
            }

            // 플레이어 주변 랜덤 위치
            if (playerTransform != null)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance
                );

                Vector3 spawnPos = playerTransform.position + offset;

                // NavMesh 위 위치 찾기
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    return hit.position + Vector3.up * spawnHeightOffset;
                }

                return spawnPos + Vector3.up * spawnHeightOffset;
            }

            return Vector3.zero;
        }

        /// <summary>
        /// 스폰 회전 계산 (플레이어를 향하도록)
        /// </summary>
        private Quaternion GetSpawnRotation(Vector3 spawnPosition)
        {
            if (playerTransform != null)
            {
                Vector3 direction = playerTransform.position - spawnPosition;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.01f)
                {
                    return Quaternion.LookRotation(direction);
                }
            }
            return Quaternion.identity;
        }

        /// <summary>
        /// 난이도 수정자 적용
        /// </summary>
        private void ApplyDifficultyModifiers(GameObject enemy)
        {
            // 체력 증가
            var health = enemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                float healthMultiplier = Mathf.Pow(healthMultiplierPerWave, currentWave - 1);
                health.SetMaxHealth(health.MaxHealth * healthMultiplier);
            }

            // EnemyAI 공격력 증가 (있다면)
            // var enemyAI = enemy.GetComponent<EnemyAI>();
            // if (enemyAI != null)
            // {
            //     float damageMultiplier = Mathf.Pow(damageMultiplierPerWave, currentWave - 1);
            //     enemyAI.SetDamageMultiplier(damageMultiplier);
            // }
        }

        /// <summary>
        /// 적 사망 콜백 설정
        /// </summary>
        private void SetupEnemyDeathCallback(GameObject enemy)
        {
            // EnemyHealth 컴포넌트에서 OnDeath 이벤트 연결
            var enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // OnDeath 이벤트에 연결
                enemyHealth.OnDeath += () => OnEnemyDeath(enemy);
            }
            else
            {
                // EnemyHealth가 없으면 별도의 사망 감지 컴포넌트 추가
                var deathHandler = enemy.AddComponent<EnemyDeathHandler>();
                deathHandler.Initialize(this, enemy);
            }
        }

        /// <summary>
        /// 적 사망 처리
        /// </summary>
        public void OnEnemyDeath(GameObject enemy, int coinReward = 10)
        {
            if (activeEnemies.Contains(enemy))
            {
                activeEnemies.Remove(enemy);
            }

            // 코인 보상 지급
            if (Economy.CoinManager.Instance != null)
            {
                Vector3? dropPos = enemy != null ? enemy.transform.position : (Vector3?)null;
                Economy.CoinManager.Instance.RewardKill(false, false, dropPos);
            }

            // SquadSurvivalManager에 알림
            if (SquadSurvivalManager.Instance != null)
            {
                SquadSurvivalManager.Instance.OnEnemyKilled(coinReward);
            }
        }

        /// <summary>
        /// 적 수 계산
        /// </summary>
        private int CalculateEnemyCount(int wave)
        {
            return Mathf.RoundToInt(baseEnemyCount * Mathf.Pow(enemyCountGrowth, wave - 1));
        }

        /// <summary>
        /// 스폰 간격 계산
        /// </summary>
        private float CalculateSpawnInterval(int wave)
        {
            float interval = baseSpawnInterval - (spawnIntervalReduction * (wave - 1));
            return Mathf.Max(interval, minSpawnInterval);
        }

        /// <summary>
        /// 모든 적 제거
        /// </summary>
        public void ClearAllEnemies()
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            activeEnemies.Clear();
        }

        /// <summary>
        /// 기본 스폰 포인트 생성
        /// </summary>
        private void CreateDefaultSpawnPoints()
        {
            GameObject spawnPointsParent = new GameObject("EnemySpawnPoints");
            spawnPointsParent.transform.SetParent(transform);

            // 8방향 스폰 포인트 생성
            spawnPoints = new Transform[8];
            float radius = 25f;

            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );

                GameObject point = new GameObject($"SpawnPoint_{i}");
                point.transform.SetParent(spawnPointsParent.transform);
                point.transform.position = position;
                spawnPoints[i] = point.transform;
            }

#if UNITY_EDITOR
            Debug.Log("[WaveManager] 기본 스폰 포인트 8개 생성됨");
#endif
        }

        /// <summary>
        /// 적 프리팹 추가 (런타임)
        /// </summary>
        public void AddEnemyPrefab(GameObject prefab, string name, float weight = 1f, int minWave = 1)
        {
            enemyPrefabs.Add(new EnemySpawnInfo
            {
                prefab = prefab,
                enemyName = name,
                spawnWeight = weight,
                minWaveToSpawn = minWave
            });
        }

        /// <summary>
        /// 스폰 포인트 설정
        /// </summary>
        public void SetSpawnPoints(Transform[] points)
        {
            spawnPoints = points;
        }

        [ContextMenu("Auto Find Enemy Prefabs")]
        public void AutoFindEnemyPrefabs()
        {
#if UNITY_EDITOR
            enemyPrefabs.Clear();

            // Resources 폴더에서 Enemy 프리팹 찾기
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/03.Prefabs/Enemy" });
            
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null)
                {
                    enemyPrefabs.Add(new EnemySpawnInfo
                    {
                        prefab = prefab,
                        enemyName = prefab.name,
                        spawnWeight = 1f,
                        minWaveToSpawn = 1
                    });
                }
            }

            Debug.Log($"[WaveManager] {enemyPrefabs.Count}개의 적 프리팹 찾음");
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void OnDestroy()
        {
            if (SquadSurvivalManager.Instance != null)
            {
                SquadSurvivalManager.Instance.OnWaveStart.RemoveListener(OnWaveStart);
                SquadSurvivalManager.Instance.OnGameEnd.RemoveListener(OnGameEnd);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 스폰 범위 시각화
            if (playerTransform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistance);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(playerTransform.position, maxSpawnDistance);
            }

            // 스폰 포인트 시각화
            if (spawnPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (var point in spawnPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 1f);
                    }
                }
            }
        }
#endif
    }

    /// <summary>
    /// 적 사망 감지 헬퍼 컴포넌트
    /// EnemyHealth가 없는 적을 위한 폴백
    /// </summary>
    public class EnemyDeathHandler : MonoBehaviour
    {
        private WaveManager waveManager;
        private GameObject enemy;
        private bool isDead = false;
        private bool wasAlive = true;

        [SerializeField] private int coinReward = 10;

        public void Initialize(WaveManager manager, GameObject enemyObj)
        {
            waveManager = manager;
            enemy = enemyObj;
        }

        private void Update()
        {
            // EnemyHealth 컴포넌트 확인 (IsAlive 사용)
            var health = GetComponent<EnemyHealth>();
            if (health != null)
            {
                // IsAlive가 true였다가 false가 되면 사망
                if (wasAlive && !health.IsAlive && !isDead)
                {
                    isDead = true;
                    if (waveManager != null)
                    {
                        waveManager.OnEnemyDeath(enemy, coinReward);
                    }
                }
                wasAlive = health.IsAlive;
            }
        }

        private void OnDestroy()
        {
            // 오브젝트가 파괴될 때 (사망 외의 이유로)
            if (!isDead && waveManager != null)
            {
                waveManager.OnEnemyDeath(enemy, 0);
            }
        }
    }
}
