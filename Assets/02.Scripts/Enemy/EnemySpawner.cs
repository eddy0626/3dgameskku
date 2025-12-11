using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스포너
/// EnemyData 기반으로 적을 스폰하고 관리
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    #region Enums
    
    public enum SpawnMode
    {
        Single,         // 단일 스폰
        Wave,           // 웨이브 기반
        Continuous,     // 지속적 스폰
        Triggered       // 트리거 기반
    }
    
    #endregion
    
    #region Inspector Fields
    
    [Header("스폰 설정")]
    [SerializeField] private SpawnMode _spawnMode = SpawnMode.Wave;
    [SerializeField] private bool _autoStart = true;
    [SerializeField] private float _initialDelay = 2f;
    
    [Header("적 데이터")]
    [SerializeField] private EnemySpawnData[] _enemySpawnDatas;
    
    [Header("스폰 포인트")]
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private bool _randomSpawnPoint = true;
    [SerializeField] private float _spawnRadius = 2f;
    
    [Header("웨이브 설정")]
    [SerializeField] private WaveData[] _waves;
    [SerializeField] private float _waveDelay = 5f;
    [SerializeField] private bool _autoNextWave = true;
    
    [Header("Object Pooling")]
    [SerializeField] private bool _useObjectPooling = true;
    [SerializeField] private int _poolInitialSize = 10;
    [SerializeField] private int _poolMaxSize = 30;
    
    [Header("연속 스폰 설정")]
    [SerializeField] private float _spawnInterval = 3f;
    [SerializeField] private int _maxEnemiesAlive = 10;
    
    [Header("디버그")]
    [SerializeField] private bool _debugMode = false;
    
    #endregion
    
    #region Private Fields
    
    private List<EnemyBase> _activeEnemies = new List<EnemyBase>();
    private int _currentWaveIndex = 0;
    private int _currentSpawnPointIndex = 0;
    private bool _isSpawning = false;
    private Coroutine _spawnCoroutine;
    
    #endregion
    
    #region Properties
    
    public int ActiveEnemyCount => _activeEnemies.Count;
    public int CurrentWaveIndex => _currentWaveIndex;
    public int TotalWaves => _waves != null ? _waves.Length : 0;
    public bool IsSpawning => _isSpawning;
    public bool AllWavesCompleted => _currentWaveIndex >= TotalWaves;
    
    #endregion
    
    #region Events
    
    public System.Action<EnemyBase> OnEnemySpawned;
    public System.Action<EnemyBase> OnEnemyDied;
    public System.Action<int> OnWaveStarted;
    public System.Action<int> OnWaveCompleted;
    public System.Action OnAllWavesCompleted;
    
    #endregion
    
    #region Unity Callbacks
    
    private void Start()
    {
        if (_autoStart)
        {
            StartSpawning();
        }
    }
    
    private void OnDisable()
    {
        StopSpawning();
    }
    
    #endregion
    
    #region Public Methods
    
    public void StartSpawning()
    {
        if (_isSpawning) return;
        
        _isSpawning = true;
        
        switch (_spawnMode)
        {
            case SpawnMode.Single:
                SpawnSingleEnemy();
                break;
            case SpawnMode.Wave:
                _spawnCoroutine = StartCoroutine(WaveSpawnRoutine());
                break;
            case SpawnMode.Continuous:
                _spawnCoroutine = StartCoroutine(ContinuousSpawnRoutine());
                break;
            case SpawnMode.Triggered:
                break;
        }
        
        if (_debugMode)
        {
            Debug.Log($"[EnemySpawner] Started spawning - Mode: {_spawnMode}");
        }
    }
    
    public void StopSpawning()
    {
        _isSpawning = false;
        
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }
    
    public void TriggerSpawn(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnRandomEnemy();
        }
    }
    
    public EnemyBase SpawnEnemy(EnemyData enemyData, Vector3 position)
    {
        if (enemyData == null || enemyData.enemyPrefab == null)
        {
            Debug.LogWarning("[EnemySpawner] Invalid EnemyData or missing prefab!");
            return null;
        }
        
        GameObject enemyObj;
        string poolKey = $"Enemy_{enemyData.enemyName}";
        
        if (_useObjectPooling)
        {
            if (!ObjectPoolManager.Instance.HasPool(poolKey))
            {
                ObjectPoolManager.Instance.RegisterPool(
                    poolKey,
                    enemyData.enemyPrefab,
                    _poolInitialSize,
                    _poolMaxSize,
                    true
                );
            }
            
            enemyObj = ObjectPoolManager.Instance.Get(poolKey, position, Quaternion.identity);
            
            if (enemyObj == null)
            {
                Debug.LogWarning($"[EnemySpawner] Failed to get enemy from pool: {poolKey}");
                return null;
            }
        }
        else
        {
            enemyObj = Instantiate(enemyData.enemyPrefab, position, Quaternion.identity);
        }
        
        EnemyBase enemyBase = enemyObj.GetComponent<EnemyBase>();
        if (enemyBase == null)
        {
            enemyBase = enemyObj.AddComponent<EnemyBase>();
        }
        
        enemyBase.Initialize(enemyData);
        enemyBase.PoolKey = poolKey;
        
        EnemyHealth health = enemyBase.Health;
        if (health != null)
        {
            health.OnDeath += () => OnEnemyDeathHandler(enemyBase);
        }
        
        _activeEnemies.Add(enemyBase);
        OnEnemySpawned?.Invoke(enemyBase);
        
        if (_debugMode)
        {
            Debug.Log($"[EnemySpawner] Spawned {enemyData.enemyName} at {position}");
        }
        
        return enemyBase;
    }
    
    public void ForceNextWave()
    {
        if (_spawnMode != SpawnMode.Wave) return;
        
        StopSpawning();
        _spawnCoroutine = StartCoroutine(WaveSpawnRoutine());
    }
    
    public void ClearAllEnemies()
    {
        foreach (EnemyBase enemy in _activeEnemies.ToArray())
        {
            if (enemy != null)
            {
                if (_useObjectPooling && !string.IsNullOrEmpty(enemy.PoolKey))
                {
                    ObjectPoolManager.Instance.Return(enemy.PoolKey, enemy.gameObject);
                }
                else
                {
                    Destroy(enemy.gameObject);
                }
            }
        }
        
        _activeEnemies.Clear();
    }
    
    public void ResetWaves()
    {
        _currentWaveIndex = 0;
        ClearAllEnemies();
    }
    
    #endregion
    
    #region Private Methods
    
    private void SpawnSingleEnemy()
    {
        SpawnRandomEnemy();
        _isSpawning = false;
    }
    
    private EnemyBase SpawnRandomEnemy()
    {
        if (_enemySpawnDatas == null || _enemySpawnDatas.Length == 0)
        {
            Debug.LogWarning("[EnemySpawner] No enemy spawn data configured!");
            return null;
        }
        
        EnemyData selectedData = SelectRandomEnemyData();
        if (selectedData == null) return null;
        
        Vector3 spawnPosition = GetSpawnPosition();
        return SpawnEnemy(selectedData, spawnPosition);
    }
    
    private EnemyData SelectRandomEnemyData()
    {
        int totalWeight = 0;
        foreach (EnemySpawnData spawnData in _enemySpawnDatas)
        {
            if (spawnData.enemyData != null && spawnData.canSpawn)
            {
                totalWeight += spawnData.spawnWeight;
            }
        }
        
        if (totalWeight == 0) return null;
        
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;
        
        foreach (EnemySpawnData spawnData in _enemySpawnDatas)
        {
            if (spawnData.enemyData != null && spawnData.canSpawn)
            {
                currentWeight += spawnData.spawnWeight;
                if (randomValue < currentWeight)
                {
                    return spawnData.enemyData;
                }
            }
        }
        
        return _enemySpawnDatas[0].enemyData;
    }
    
    private Vector3 GetSpawnPosition()
    {
        Vector3 basePosition;
        
        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            if (_randomSpawnPoint)
            {
                basePosition = _spawnPoints[Random.Range(0, _spawnPoints.Length)].position;
            }
            else
            {
                basePosition = _spawnPoints[_currentSpawnPointIndex].position;
                _currentSpawnPointIndex = (_currentSpawnPointIndex + 1) % _spawnPoints.Length;
            }
        }
        else
        {
            basePosition = transform.position;
        }
        
        if (_spawnRadius > 0)
        {
            Vector2 randomCircle = Random.insideUnitCircle * _spawnRadius;
            basePosition += new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        
        return basePosition;
    }
    
    private void OnEnemyDeathHandler(EnemyBase enemy)
    {
        _activeEnemies.Remove(enemy);
        OnEnemyDied?.Invoke(enemy);
        
        if (_debugMode)
        {
            Debug.Log($"[EnemySpawner] Enemy died. Active: {_activeEnemies.Count}");
        }
        
        if (_useObjectPooling && !string.IsNullOrEmpty(enemy.PoolKey))
        {
            StartCoroutine(ReturnToPoolDelayed(enemy, 2f));
        }
        else
        {
            Destroy(enemy.gameObject, 2f);
        }
        
        if (_spawnMode == SpawnMode.Wave && _activeEnemies.Count == 0 && !_isSpawning)
        {
            OnWaveCompleted?.Invoke(_currentWaveIndex - 1);
            
            if (_autoNextWave && _currentWaveIndex < TotalWaves)
            {
                StartCoroutine(DelayedNextWave());
            }
            else if (_currentWaveIndex >= TotalWaves)
            {
                OnAllWavesCompleted?.Invoke();
            }
        }
    }
    
    private IEnumerator ReturnToPoolDelayed(EnemyBase enemy, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (enemy != null && !string.IsNullOrEmpty(enemy.PoolKey))
        {
            ObjectPoolManager.Instance.Return(enemy.PoolKey, enemy.gameObject);
        }
    }
    
    private IEnumerator DelayedNextWave()
    {
        yield return new WaitForSeconds(_waveDelay);
        
        if (_currentWaveIndex < TotalWaves)
        {
            _isSpawning = true;
            _spawnCoroutine = StartCoroutine(SpawnWave(_waves[_currentWaveIndex]));
        }
    }
    
    #endregion
    
    #region Spawn Routines
    
    private IEnumerator WaveSpawnRoutine()
    {
        yield return new WaitForSeconds(_initialDelay);
        
        while (_currentWaveIndex < TotalWaves && _isSpawning)
        {
            WaveData wave = _waves[_currentWaveIndex];
            
            OnWaveStarted?.Invoke(_currentWaveIndex);
            
            if (_debugMode)
            {
                Debug.Log($"[EnemySpawner] Starting Wave {_currentWaveIndex + 1}/{TotalWaves}");
            }
            
            yield return StartCoroutine(SpawnWave(wave));
            
            _currentWaveIndex++;
            
            if (!_autoNextWave)
            {
                while (_activeEnemies.Count > 0)
                {
                    yield return null;
                }
            }
            
            _isSpawning = false;
        }
        
        if (_currentWaveIndex >= TotalWaves)
        {
            OnAllWavesCompleted?.Invoke();
        }
    }
    
    private IEnumerator SpawnWave(WaveData wave)
    {
        if (wave.waveEnemies == null) yield break;
        
        foreach (WaveEnemyData waveEnemy in wave.waveEnemies)
        {
            if (waveEnemy.enemyData == null) continue;
            
            for (int i = 0; i < waveEnemy.count; i++)
            {
                Vector3 spawnPos = GetSpawnPosition();
                SpawnEnemy(waveEnemy.enemyData, spawnPos);
                
                yield return new WaitForSeconds(waveEnemy.spawnDelay);
            }
        }
    }
    
    private IEnumerator ContinuousSpawnRoutine()
    {
        yield return new WaitForSeconds(_initialDelay);
        
        while (_isSpawning)
        {
            if (_activeEnemies.Count < _maxEnemiesAlive)
            {
                SpawnRandomEnemy();
            }
            
            yield return new WaitForSeconds(_spawnInterval);
        }
    }
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        if (_spawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (Transform point in _spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                    
                    if (_spawnRadius > 0)
                    {
                        Gizmos.color = new Color(0, 1, 0, 0.3f);
                        Gizmos.DrawWireSphere(point.position, _spawnRadius);
                        Gizmos.color = Color.green;
                    }
                }
            }
        }
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
    
    #endregion
}

#region Data Structures

[System.Serializable]
public class EnemySpawnData
{
    [Tooltip("적 데이터")]
    public EnemyData enemyData;
    
    [Tooltip("스폰 가중치")]
    [Range(1, 100)]
    public int spawnWeight = 10;
    
    [Tooltip("스폰 가능 여부")]
    public bool canSpawn = true;
}

[System.Serializable]
public class WaveData
{
    [Tooltip("웨이브 이름")]
    public string waveName = "Wave";
    
    [Tooltip("웨이브에 포함된 적들")]
    public WaveEnemyData[] waveEnemies;
}

[System.Serializable]
public class WaveEnemyData
{
    [Tooltip("적 데이터")]
    public EnemyData enemyData;
    
    [Tooltip("스폰 수량")]
    [Range(1, 50)]
    public int count = 1;
    
    [Tooltip("각 적 스폰 간격")]
    [Range(0f, 5f)]
    public float spawnDelay = 0.5f;
}

#endregion