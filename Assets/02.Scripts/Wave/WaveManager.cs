using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 웨이브 관리 시스템
/// - 웨이브 진행 및 적 스폰 관리
/// - 난이도 스케일링 (무한 모드)
/// - 보스 스폰 및 보상 시스템
/// - Save/Load 지원
/// </summary>
public class WaveManager : MonoBehaviour
{
    #region Singleton
    public static WaveManager Instance { get; private set; }
    #endregion

    #region Inspector Fields
    [Header("웨이브 데이터")]
    [Tooltip("웨이브 설정 목록")]
    [SerializeField] private List<WaveConfig> waves = new List<WaveConfig>();

    [Header("스폰 포인트")]
    [Tooltip("적 스폰 위치들")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("스폰 위치 랜덤 반경")]
    [SerializeField] private float spawnRadius = 2f;

    [Tooltip("NavMesh 위치 검증")]
    [SerializeField] private bool validateNavMeshPosition = true;

    [Tooltip("NavMesh 샘플링 거리")]
    [SerializeField] private float navMeshSampleDistance = 5f;

    [Header("타이밍 설정")]
    [Tooltip("웨이브 사이 대기 시간")]
    [SerializeField] private float timeBetweenWaves = 10f;

    [Tooltip("자동으로 다음 웨이브 시작")]
    [SerializeField] private bool autoStartNextWave = true;

    [Tooltip("게임 시작 시 자동 시작")]
    [SerializeField] private bool autoStartOnAwake = false;

    [Header("무한 모드")]
    [Tooltip("무한 모드 활성화 (웨이브 무제한)")]
    [SerializeField] private bool infiniteMode = false;

    [Tooltip("웨이브당 체력 스케일 증가")]
    [SerializeField] private float healthScalePerWave = 0.1f;

    [Tooltip("웨이브당 데미지 스케일 증가")]
    [SerializeField] private float damageScalePerWave = 0.05f;

    [Tooltip("웨이브당 추가 적 수")]
    [SerializeField] private int extraEnemiesPerWave = 2;

    [Tooltip("웨이브당 스폰 속도 증가")]
    [SerializeField] private float spawnRateIncreasePerWave = 0.05f;

    [Header("난이도 설정")]
    [Tooltip("글로벌 난이도 배율 (1.0 = 기본)")]
    [Range(0.5f, 3f)]
    [SerializeField] private float difficultyMultiplier = 1f;

    [Tooltip("플레이어 주변 스폰 최소 거리")]
    [SerializeField] private float minSpawnDistanceFromPlayer = 10f;

    [Tooltip("플레이어 주변 스폰 최대 거리")]
    [SerializeField] private float maxSpawnDistanceFromPlayer = 25f;

    [Header("설정")]
    [Tooltip("씬 전환 시 유지")]
    [SerializeField] private bool dontDestroyOnLoad = false;

    [Header("디버그")]
    [SerializeField] private bool showSpawnGizmos = true;
    [SerializeField] private bool logWaveEvents = true;
    #endregion

    #region Private Fields
    private int currentWaveIndex = -1;
    private int enemiesAlive;
    private int enemiesSpawned;
    private int enemiesKilledThisWave;
    private int totalEnemiesKilled;
    private bool waveInProgress;
    private bool isPaused;
    private bool isGameStarted;
    private float waveStartTime;
    private float totalPlayTime;
    private float gameStartTime;

    private Transform player;
    private Coroutine waveCoroutine;
    private Coroutine preparationCoroutine;
    private List<GameObject> activeEnemies = new List<GameObject>();

    // 통계
    private WaveStatistics statistics = new WaveStatistics();
    #endregion

    #region Events
    /// <summary>웨이브 시작 (웨이브 번호)</summary>
    public event Action<int> OnWaveStart;

    /// <summary>웨이브 완료 (웨이브 번호)</summary>
    public event Action<int> OnWaveComplete;

    /// <summary>모든 웨이브 완료</summary>
    public event Action OnAllWavesComplete;

    /// <summary>적 수 변경 (생존, 총)</summary>
    public event Action<int, int> OnEnemyCountChanged;

    /// <summary>준비 시간 시작 (남은 시간)</summary>
    public event Action<float> OnPreparationStart;

    /// <summary>준비 시간 업데이트 (남은 시간)</summary>
    public event Action<float> OnPreparationUpdate;

    /// <summary>보스 스폰</summary>
    public event Action<EnemyData> OnBossSpawn;

    /// <summary>보스 처치</summary>
    public event Action OnBossDefeated;

    /// <summary>적 사망 (EnemyData, 위치)</summary>
    public event Action<EnemyData, Vector3> OnEnemyKilled;

    /// <summary>게임 시작</summary>
    public event Action OnGameStart;

    /// <summary>게임 종료 (승리 여부)</summary>
    public event Action<bool> OnGameEnd;
    #endregion

    #region Properties
    public int CurrentWave => currentWaveIndex + 1;
    public int TotalWaves => waves.Count;
    public int EnemiesAlive => enemiesAlive;
    public int EnemiesSpawned => enemiesSpawned;
    public int EnemiesKilledThisWave => enemiesKilledThisWave;
    public int TotalEnemiesKilled => totalEnemiesKilled;
    public bool IsWaveInProgress => waveInProgress;
    public bool IsPaused => isPaused;
    public bool IsGameStarted => isGameStarted;
    public bool IsInfiniteMode => infiniteMode;
    public float DifficultyMultiplier => difficultyMultiplier;
    public float TotalPlayTime => isGameStarted ? Time.time - gameStartTime : totalPlayTime;
    public WaveStatistics Statistics => statistics;

    public float WaveProgress
    {
        get
        {
            if (!waveInProgress) return 0f;
            var config = GetCurrentWaveConfig();
            if (config == null) return 0f;
            return Mathf.Clamp01((Time.time - waveStartTime) / config.waveDuration);
        }
    }

    public float WaveTimeRemaining
    {
        get
        {
            if (!waveInProgress) return 0f;
            var config = GetCurrentWaveConfig();
            if (config == null) return 0f;
            return Mathf.Max(0f, config.waveDuration - (Time.time - waveStartTime));
        }
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 스폰 포인트 자동 탐지
        AutoDetectSpawnPoints();
        
        // 웨이브 설정 자동 로드
        AutoLoadWaveConfigs();
    }
    
    /// <summary>
    /// 씬에서 "EnemySpawn" 이름을 가진 오브젝트들을 자동으로 찾아 스폰포인트로 설정
    /// </summary>
    private void AutoDetectSpawnPoints()
    {
        if (spawnPoints != null && spawnPoints.Length > 0) return;
        
        List<Transform> foundPoints = new List<Transform>();
        
        // "EnemySpawn" 이름 패턴으로 오브젝트 찾기
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("EnemySpawn"))
            {
                foundPoints.Add(obj.transform);
            }
        }
        
        // 이름순 정렬
        foundPoints.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        
        if (foundPoints.Count > 0)
        {
            spawnPoints = foundPoints.ToArray();
            if (logWaveEvents)
            {
                Debug.Log($"[WaveManager] 자동 탐지: {spawnPoints.Length}개 스폰포인트 발견");
            }
        }
        else
        {
            Debug.LogWarning("[WaveManager] 스폰포인트를 찾을 수 없습니다. 'EnemySpawn'으로 시작하는 오브젝트가 필요합니다.");
        }
    }
    
    /// <summary>
    /// Resources 또는 에디터에서 WaveConfig를 자동 로드
    /// </summary>
    private void AutoLoadWaveConfigs()
    {
        if (waves != null && waves.Count > 0) return;
        
        #if UNITY_EDITOR
        // 에디터에서 Assets/04.Data/Wave 폴더의 WaveConfig 로드
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:WaveConfig", new[] { "Assets/09.Data/Waves" });
        if (guids.Length > 0)
        {
            waves = new List<WaveConfig>();
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                WaveConfig config = UnityEditor.AssetDatabase.LoadAssetAtPath<WaveConfig>(path);
                if (config != null)
                {
                    waves.Add(config);
                }
            }
            
            // 이름순 정렬
            waves.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            
            if (logWaveEvents)
            {
                Debug.Log($"[WaveManager] 자동 로드: {waves.Count}개 웨이브 설정 발견");
            }
        }
        else
        {
            Debug.LogWarning("[WaveManager] WaveConfig를 찾을 수 없습니다. Assets/04.Data/Wave 폴더를 확인하세요.");
        }
        #else
        // 빌드에서는 Resources 폴더에서 로드
        WaveConfig[] loadedConfigs = Resources.LoadAll<WaveConfig>("Wave");
        if (loadedConfigs.Length > 0)
        {
            waves = new List<WaveConfig>(loadedConfigs);
            waves.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            Debug.Log($"[WaveManager] Resources에서 {waves.Count}개 웨이브 로드");
        }
        #endif
    }

    private void Start()
    {
        // 플레이어 찾기
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            LogDebug("플레이어를 찾을 수 없습니다!", true);
        }

        // 자동 시작
        if (autoStartOnAwake)
        {
            StartGame();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region Game Control
    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        if (isGameStarted)
        {
            LogDebug("게임이 이미 시작되었습니다!");
            return;
        }

        isGameStarted = true;
        gameStartTime = Time.time;
        statistics = new WaveStatistics();

        OnGameStart?.Invoke();
        LogDebug("게임 시작!");

        StartFirstWave();
    }

    /// <summary>
    /// 게임 종료
    /// </summary>
    public void EndGame(bool victory)
    {
        if (!isGameStarted) return;

        isGameStarted = false;
        totalPlayTime = Time.time - gameStartTime;

        StopCurrentWave();

        // 통계 저장
        statistics.totalPlayTime = totalPlayTime;
        statistics.highestWave = CurrentWave;
        statistics.totalEnemiesKilled = totalEnemiesKilled;
        statistics.victory = victory;

        OnGameEnd?.Invoke(victory);
        LogDebug($"게임 종료! 승리: {victory}, 도달 웨이브: {CurrentWave}");
    }

    /// <summary>
    /// 게임 리셋
    /// </summary>
    public void ResetGame()
    {
        StopCurrentWave();

        currentWaveIndex = -1;
        totalEnemiesKilled = 0;
        isGameStarted = false;
        statistics = new WaveStatistics();

        LogDebug("게임 리셋");
    }
    #endregion

    #region Wave Control
    /// <summary>
    /// 첫 번째 웨이브 시작
    /// </summary>
    public void StartFirstWave()
    {
        currentWaveIndex = -1;
        totalEnemiesKilled = 0;
        StartNextWave();
    }

    /// <summary>
    /// 다음 웨이브 시작
    /// </summary>
    public void StartNextWave()
    {
        if (waveInProgress)
        {
            LogDebug("웨이브가 이미 진행 중입니다!");
            return;
        }

        currentWaveIndex++;

        // 모든 웨이브 완료 체크
        if (!infiniteMode && currentWaveIndex >= waves.Count)
        {
            LogDebug("모든 웨이브 완료!");
            OnAllWavesComplete?.Invoke();
            EndGame(true);
            return;
        }

        // 웨이브 시작
        waveCoroutine = StartCoroutine(RunWave());
    }

    /// <summary>
    /// 웨이브 일시정지
    /// </summary>
    public void PauseWave()
    {
        isPaused = true;
        LogDebug("웨이브 일시정지");
    }

    /// <summary>
    /// 웨이브 재개
    /// </summary>
    public void ResumeWave()
    {
        isPaused = false;
        LogDebug("웨이브 재개");
    }

    /// <summary>
    /// 현재 웨이브 중지 및 적 제거
    /// </summary>
    public void StopCurrentWave()
    {
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        if (preparationCoroutine != null)
        {
            StopCoroutine(preparationCoroutine);
            preparationCoroutine = null;
        }

        waveInProgress = false;

        // 모든 적 제거
        ClearAllEnemies();
    }

    /// <summary>
    /// 특정 웨이브로 이동
    /// </summary>
    public void SkipToWave(int waveNumber)
    {
        StopCurrentWave();
        currentWaveIndex = waveNumber - 2; // StartNextWave에서 +1 하므로
        StartNextWave();
    }

    /// <summary>
    /// 모든 적 제거
    /// </summary>
    public void ClearAllEnemies()
    {
        foreach (var enemy in activeEnemies.ToArray())
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();
        enemiesAlive = 0;
    }
    #endregion

    #region Wave Execution
    private IEnumerator RunWave()
    {
        WaveConfig waveData = GetCurrentWaveConfig();

        if (waveData == null && !infiniteMode)
        {
            LogDebug("웨이브 데이터를 찾을 수 없습니다!", true);
            yield break;
        }

        // 준비 시간
        float prepTime = waveData?.preparationTime ?? 5f;
        OnPreparationStart?.Invoke(prepTime);

        LogDebug($"웨이브 {CurrentWave} 시작까지 {prepTime}초...");

        // 준비 시간 카운트다운
        preparationCoroutine = StartCoroutine(PreparationCountdown(prepTime));
        yield return preparationCoroutine;

        // 웨이브 시작
        waveInProgress = true;
        waveStartTime = Time.time;
        enemiesSpawned = 0;
        enemiesAlive = 0;
        enemiesKilledThisWave = 0;

        OnWaveStart?.Invoke(CurrentWave);

        // UpgradeManager에 웨이브 알림
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.SetCurrentWave(CurrentWave);
        }

        LogDebug($"웨이브 {CurrentWave} 시작!");

        if (waveData != null)
        {
            // 일반 적 스폰
            yield return StartCoroutine(SpawnWaveEnemies(waveData));

            // 보스 스폰
            if (waveData.hasBoss && waveData.bossEnemy != null)
            {
                yield return new WaitForSeconds(waveData.bossSpawnTime);
                SpawnBoss(waveData);
            }
        }
        else if (infiniteMode)
        {
            // 무한 모드 스폰
            yield return StartCoroutine(SpawnInfiniteModeEnemies());
        }

        // 모든 적 처치 대기
        while (enemiesAlive > 0)
        {
            while (isPaused)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
        }

        // 웨이브 완료
        CompleteWave(waveData);
    }

    private IEnumerator PreparationCountdown(float duration)
    {
        float remaining = duration;

        while (remaining > 0)
        {
            while (isPaused)
            {
                yield return null;
            }

            OnPreparationUpdate?.Invoke(remaining);
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }
    }

    private IEnumerator SpawnWaveEnemies(WaveConfig waveData)
    {
        if (waveData.spawnGroups == null) yield break;

        foreach (var group in waveData.spawnGroups)
        {
            if (group.enemyType == null) continue;

            // 시작 딜레이
            yield return new WaitForSeconds(group.startDelay);

            if (group.spawnAllAtOnce)
            {
                // 한번에 모두 스폰
                for (int i = 0; i < group.count; i++)
                {
                    SpawnEnemy(group.enemyType, group.pattern, waveData);
                }
            }
            else
            {
                // 간격을 두고 스폰
                for (int i = 0; i < group.count; i++)
                {
                    while (isPaused)
                    {
                        yield return null;
                    }

                    SpawnEnemy(group.enemyType, group.pattern, waveData);

                    float interval = group.spawnInterval / waveData.spawnRateMultiplier;
                    yield return new WaitForSeconds(interval);
                }
            }

            // 그룹 딜레이
            yield return new WaitForSeconds(group.groupDelay);
        }
    }

    private IEnumerator SpawnInfiniteModeEnemies()
    {
        // 무한 모드: 마지막 웨이브 기반 + 스케일링
        if (waves.Count == 0) yield break;

        WaveConfig baseWave = waves[waves.Count - 1];
        int extraWaves = CurrentWave - waves.Count;
        int extraEnemies = extraWaves * extraEnemiesPerWave;
        float spawnRateMultiplier = 1f + extraWaves * spawnRateIncreasePerWave;

        foreach (var group in baseWave.spawnGroups)
        {
            if (group.enemyType == null) continue;

            int spawnCount = group.count + extraEnemies / baseWave.spawnGroups.Length;

            for (int i = 0; i < spawnCount; i++)
            {
                while (isPaused)
                {
                    yield return null;
                }

                SpawnEnemy(group.enemyType, group.pattern, baseWave, true);

                float interval = group.spawnInterval / spawnRateMultiplier;
                yield return new WaitForSeconds(interval);
            }

            yield return new WaitForSeconds(group.groupDelay);
        }

        // 무한 모드 보스 (5웨이브마다)
        if (CurrentWave % 5 == 0 && baseWave.bossEnemy != null)
        {
            yield return new WaitForSeconds(2f);
            SpawnBoss(baseWave, true);
        }
    }
    #endregion

    #region Enemy Spawning
    private void SpawnEnemy(EnemyData data, SpawnPattern pattern, WaveConfig waveData, bool isInfiniteMode = false)
    {
        if (data == null || data.enemyPrefab == null)
        {
            LogDebug($"적 데이터 또는 프리팹이 없습니다: {data?.enemyName}", true);
            return;
        }

        Vector3 spawnPos = GetSpawnPosition(pattern);

        // NavMesh 검증
        if (validateNavMeshPosition)
        {
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }
            else
            {
                LogDebug($"NavMesh 위치를 찾을 수 없어 기본 스폰 포인트 사용");
                spawnPos = GetRandomSpawnPoint();
            }
        }

        GameObject enemy = Instantiate(data.enemyPrefab, spawnPos, Quaternion.identity);

        // EnemyBase 초기화
        if (enemy.TryGetComponent<EnemyBase>(out var enemyBase))
        {
            enemyBase.Initialize(data);

            // 스케일링 적용
            float healthMult = waveData.healthMultiplier * difficultyMultiplier;
            float damageMult = waveData.damageMultiplier * difficultyMultiplier;

            if (isInfiniteMode)
            {
                int extraWaves = CurrentWave - waves.Count;
                healthMult += extraWaves * healthScalePerWave;
                damageMult += extraWaves * damageScalePerWave;
            }

            // EnemyBase를 통해 체력에 스케일링 적용
            if (enemyBase.Health != null)
            {
                float scaledHealth = data.maxHealth * healthMult;
                enemyBase.Health.SetMaxHealth(scaledHealth, true);
            }
        }

        // 사망 이벤트 연결
        if (enemy.TryGetComponent<EnemyHealth>(out var health))
        {
            health.OnDeath += () => HandleEnemyDeath(enemy, data);
        }

        activeEnemies.Add(enemy);
        enemiesSpawned++;
        enemiesAlive++;

        OnEnemyCountChanged?.Invoke(enemiesAlive, GetTotalEnemyCount());
    }

    private void SpawnBoss(WaveConfig waveData, bool isInfiniteMode = false)
    {
        if (waveData.bossEnemy == null) return;

        LogDebug($"보스 스폰: {waveData.bossEnemy.enemyName}");

        int bossCount = isInfiniteMode ? 1 + (CurrentWave - waves.Count) / 10 : waveData.bossCount;

        for (int i = 0; i < bossCount; i++)
        {
            SpawnEnemy(waveData.bossEnemy, SpawnPattern.Single, waveData, isInfiniteMode);
        }

        OnBossSpawn?.Invoke(waveData.bossEnemy);
    }

    private Vector3 GetSpawnPosition(SpawnPattern pattern)
    {
        Vector3 basePos;

        switch (pattern)
        {
            case SpawnPattern.Sequential:
                int index = enemiesSpawned % Mathf.Max(1, spawnPoints.Length);
                basePos = spawnPoints.Length > 0 ? spawnPoints[index].position : Vector3.zero;
                break;

            case SpawnPattern.Surrounding:
                if (player != null)
                {
                    float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float distance = UnityEngine.Random.Range(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);
                    basePos = player.position + new Vector3(
                        Mathf.Cos(angle) * distance,
                        0,
                        Mathf.Sin(angle) * distance
                    );
                }
                else
                {
                    basePos = GetRandomSpawnPoint();
                }
                break;

            case SpawnPattern.Single:
                basePos = spawnPoints.Length > 0 ? spawnPoints[0].position : Vector3.zero;
                break;

            case SpawnPattern.Random:
            default:
                basePos = GetRandomSpawnPoint();
                break;
        }

        // 약간의 랜덤 오프셋
        Vector3 offset = UnityEngine.Random.insideUnitSphere * spawnRadius;
        offset.y = 0;

        return basePos + offset;
    }

    private Vector3 GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            // 스폰 포인트가 없으면 플레이어 주변 스폰
            if (player != null)
            {
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = UnityEngine.Random.Range(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);
                return player.position + new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
            }
            return Vector3.zero;
        }

        int index = UnityEngine.Random.Range(0, spawnPoints.Length);
        return spawnPoints[index].position;
    }

    private int GetTotalEnemyCount()
    {
        var config = GetCurrentWaveConfig();
        return config?.TotalEnemyCount ?? enemiesSpawned;
    }
    #endregion

    #region Enemy Death
    private void HandleEnemyDeath(GameObject enemy, EnemyData data)
    {
        activeEnemies.Remove(enemy);
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        enemiesKilledThisWave++;
        totalEnemiesKilled++;

        OnEnemyCountChanged?.Invoke(enemiesAlive, GetTotalEnemyCount());
        OnEnemyKilled?.Invoke(data, enemy.transform.position);

        // 자원 드롭
        if (data != null && ResourceManager.Instance != null)
        {
            int minGold = Mathf.Max(1, data.scoreReward / 10);
            int maxGold = Mathf.Max(2, data.scoreReward / 5);
            float gemChance = data.enemyType == EnemyType.Boss ? 0.5f : 0.05f;

            ResourceManager.Instance.SpawnRandomDrops(
                enemy.transform.position,
                minGold,
                maxGold,
                gemChance
            );
        }

        // 보스 처치 체크
        WaveConfig waveData = GetCurrentWaveConfig();
        if (waveData != null && waveData.hasBoss && data == waveData.bossEnemy)
        {
            OnBossDefeated?.Invoke();
            LogDebug("보스 처치!");
        }

        LogDebug($"적 처치. 남은 적: {enemiesAlive}");
    }

    /// <summary>
    /// 외부에서 적 사망 등록 (EnemyBase에서 호출)
    /// </summary>
    public void RegisterEnemyDeath(GameObject enemy = null, EnemyData data = null)
    {
        if (enemy != null)
        {
            activeEnemies.Remove(enemy);
        }

        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        enemiesKilledThisWave++;
        totalEnemiesKilled++;

        OnEnemyCountChanged?.Invoke(enemiesAlive, GetTotalEnemyCount());

        if (data != null && enemy != null)
        {
            OnEnemyKilled?.Invoke(data, enemy.transform.position);
        }
    }
    #endregion

    #region Wave Completion
    private void CompleteWave(WaveConfig waveData)
    {
        waveInProgress = false;
        float waveDuration = Time.time - waveStartTime;

        LogDebug($"웨이브 {CurrentWave} 완료! 처치: {enemiesKilledThisWave}, 소요시간: {waveDuration:F1}초");

        // 통계 업데이트
        statistics.wavesCompleted++;
        statistics.totalEnemiesKilled = totalEnemiesKilled;
        if (waveDuration < statistics.fastestWaveTime || statistics.fastestWaveTime == 0)
        {
            statistics.fastestWaveTime = waveDuration;
        }

        // 보상 지급
        if (waveData != null && ResourceManager.Instance != null)
        {
            // 무한 모드 보너스
            float bonusMultiplier = infiniteMode && CurrentWave > waves.Count
                ? 1f + (CurrentWave - waves.Count) * 0.1f
                : 1f;

            int goldReward = Mathf.RoundToInt(waveData.goldReward * bonusMultiplier);
            int gemReward = Mathf.RoundToInt(waveData.gemReward * bonusMultiplier);
            int expReward = Mathf.RoundToInt(waveData.experienceReward * bonusMultiplier);

            ResourceManager.Instance.AddResource(ResourceType.Gold, goldReward);
            ResourceManager.Instance.AddResource(ResourceType.Gem, gemReward);
            ResourceManager.Instance.AddResource(ResourceType.Experience, expReward);

            LogDebug($"보상: Gold +{goldReward}, Gem +{gemReward}, Exp +{expReward}");
        }

        OnWaveComplete?.Invoke(CurrentWave);

        // 자동 다음 웨이브
        if (autoStartNextWave)
        {
            StartCoroutine(AutoStartNextWave());
        }
    }

    private IEnumerator AutoStartNextWave()
    {
        yield return new WaitForSeconds(timeBetweenWaves);

        if (!waveInProgress && isGameStarted)
        {
            StartNextWave();
        }
    }
    #endregion

    #region Query Methods
    /// <summary>
    /// 현재 웨이브 설정 조회
    /// </summary>
    public WaveConfig GetCurrentWaveConfig()
    {
        if (currentWaveIndex < 0 || currentWaveIndex >= waves.Count)
        {
            return infiniteMode && waves.Count > 0 ? waves[waves.Count - 1] : null;
        }
        return waves[currentWaveIndex];
    }

    /// <summary>
    /// 특정 웨이브 설정 조회
    /// </summary>
    public WaveConfig GetWaveConfig(int waveNumber)
    {
        int index = waveNumber - 1;
        if (index < 0 || index >= waves.Count)
        {
            return null;
        }
        return waves[index];
    }

    /// <summary>
    /// 현재 난이도 배율 (무한 모드 스케일링 포함)
    /// </summary>
    public float GetCurrentDifficultyScale()
    {
        float scale = difficultyMultiplier;

        if (infiniteMode && CurrentWave > waves.Count)
        {
            int extraWaves = CurrentWave - waves.Count;
            scale += extraWaves * healthScalePerWave;
        }

        return scale;
    }
    #endregion

    #region Difficulty
    /// <summary>
    /// 난이도 배율 설정
    /// </summary>
    public void SetDifficultyMultiplier(float multiplier)
    {
        difficultyMultiplier = Mathf.Clamp(multiplier, 0.5f, 5f);
        LogDebug($"난이도 배율: {difficultyMultiplier:F2}");
    }

    /// <summary>
    /// 무한 모드 설정
    /// </summary>
    public void SetInfiniteMode(bool enabled)
    {
        infiniteMode = enabled;
        LogDebug($"무한 모드: {(enabled ? "활성화" : "비활성화")}");
    }
    #endregion

    #region Save/Load
    /// <summary>
    /// 저장용 데이터 생성
    /// </summary>
    public WaveSaveData GetSaveData()
    {
        return new WaveSaveData
        {
            currentWaveIndex = currentWaveIndex,
            totalEnemiesKilled = totalEnemiesKilled,
            totalPlayTime = TotalPlayTime,
            infiniteMode = infiniteMode,
            difficultyMultiplier = difficultyMultiplier,
            statistics = statistics
        };
    }

    /// <summary>
    /// 저장 데이터 로드
    /// </summary>
    public void LoadSaveData(WaveSaveData saveData)
    {
        if (saveData == null) return;

        currentWaveIndex = saveData.currentWaveIndex - 1; // StartNextWave에서 +1
        totalEnemiesKilled = saveData.totalEnemiesKilled;
        totalPlayTime = saveData.totalPlayTime;
        infiniteMode = saveData.infiniteMode;
        difficultyMultiplier = saveData.difficultyMultiplier;
        statistics = saveData.statistics ?? new WaveStatistics();

        LogDebug($"웨이브 데이터 로드: Wave {saveData.currentWaveIndex + 1}");
    }

    /// <summary>
    /// JSON으로 저장
    /// </summary>
    public string GetSaveJson()
    {
        return JsonUtility.ToJson(GetSaveData());
    }

    /// <summary>
    /// JSON에서 로드
    /// </summary>
    public void LoadFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        var saveData = JsonUtility.FromJson<WaveSaveData>(json);
        LoadSaveData(saveData);
    }
    #endregion

    #region Utility
    private void LogDebug(string message, bool isWarning = false)
    {
        if (!logWaveEvents) return;

        if (isWarning)
            Debug.LogWarning($"[WaveManager] {message}");
        else
            Debug.Log($"[WaveManager] {message}");
    }
    #endregion

    #region Debug
#if UNITY_EDITOR
    [ContextMenu("Start Game")]
    private void DebugStartGame() => StartGame();

    [ContextMenu("Start Next Wave")]
    private void DebugStartNextWave() => StartNextWave();

    [ContextMenu("Skip to Wave 5")]
    private void DebugSkipToWave5() => SkipToWave(5);

    [ContextMenu("Kill All Enemies")]
    private void DebugKillAllEnemies() => ClearAllEnemies();

    [ContextMenu("Toggle Infinite Mode")]
    private void DebugToggleInfinite()
    {
        infiniteMode = !infiniteMode;
        Debug.Log($"무한 모드: {infiniteMode}");
    }

    [ContextMenu("Print Statistics")]
    private void DebugPrintStats()
    {
        Debug.Log("=== Wave Statistics ===");
        Debug.Log($"Current Wave: {CurrentWave}");
        Debug.Log($"Enemies Alive: {enemiesAlive}");
        Debug.Log($"Total Killed: {totalEnemiesKilled}");
        Debug.Log($"Play Time: {TotalPlayTime:F1}s");
        Debug.Log($"Difficulty: {GetCurrentDifficultyScale():F2}x");
    }

    [ContextMenu("Spawn 10 Enemies")]
    private void DebugSpawn10Enemies()
    {
        var config = GetCurrentWaveConfig();
        if (config != null && config.spawnGroups.Length > 0)
        {
            var data = config.spawnGroups[0].enemyType;
            for (int i = 0; i < 10; i++)
            {
                SpawnEnemy(data, SpawnPattern.Random, config);
            }
        }
    }
#endif
    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (!showSpawnGizmos || spawnPoints == null) return;

        Gizmos.color = Color.red;
        foreach (var point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, spawnRadius);
                Gizmos.DrawIcon(point.position, "d_winbtn_mac_close", true);
            }
        }

        // 플레이어 주변 스폰 범위
        if (player != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(player.position, minSpawnDistanceFromPlayer);
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(player.position, maxSpawnDistanceFromPlayer);
        }
    }
    #endregion
}

#region Data Classes
/// <summary>
/// 웨이브 저장 데이터
/// </summary>
[Serializable]
public class WaveSaveData
{
    public int currentWaveIndex;
    public int totalEnemiesKilled;
    public float totalPlayTime;
    public bool infiniteMode;
    public float difficultyMultiplier;
    public WaveStatistics statistics;
}

/// <summary>
/// 웨이브 통계
/// </summary>
[Serializable]
public class WaveStatistics
{
    public int wavesCompleted;
    public int totalEnemiesKilled;
    public float totalPlayTime;
    public float fastestWaveTime;
    public int highestWave;
    public bool victory;
}
#endregion
