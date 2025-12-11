using UnityEngine;

/// <summary>
/// 적의 중앙 컨트롤러
/// EnemyData를 기반으로 모든 적 컴포넌트를 초기화하고 관리
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyAI))]
[RequireComponent(typeof(EnemyAttack))]
public class EnemyBase : MonoBehaviour, IPoolable
{
    #region Inspector Fields
    
    [Header("적 데이터")]
    [SerializeField] private EnemyData _enemyData;
    
    [Header("디버그")]
    [SerializeField] private bool _debugMode = false;
    
    #endregion
    
    #region Private Fields
    
    private EnemyHealth _health;
    private EnemyAI _ai;
    private EnemyAttack _attack;
    
    private Vector3 _spawnPosition;
    
    
    private EnemyHealthBar _healthBar;
private string _poolKey;
private bool _isInitialized;
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// 현재 적 데이터
    /// </summary>
    public EnemyData EnemyData => _enemyData;
    
    /// <summary>
    /// 적 이름
    /// </summary>
    public string EnemyName => _enemyData != null ? _enemyData.enemyName : gameObject.name;
    
    /// <summary>
    /// 적 타입
    /// </summary>
    public EnemyType EnemyType => _enemyData != null ? _enemyData.enemyType : EnemyType.Normal;
    
    /// <summary>
    /// 스폰 위치
    /// </summary>
    public Vector3 SpawnPosition => _spawnPosition;
    
    /// <summary>
    /// 체력 컴포넌트 참조
    /// </summary>
    public EnemyHealth Health => _health;
    
    /// <summary>
    /// AI 컴포넌트 참조
    /// </summary>
    public EnemyAI AI => _ai;
    
    /// <summary>
    /// 공격 컴포넌트 참조
    /// </summary>
    public EnemyAttack Attack => _attack;
    
    /// <summary>
    /// 초기화 완료 여부
    /// </summary>
    
    
    /// <summary>
    /// Object Pool 키 (풀링 사용 시)
    /// </summary>
    public string PoolKey
    {
        get => _poolKey;
        set => _poolKey = value;
    }
public bool IsInitialized => _isInitialized;
    
    #endregion
    
    #region Unity Callbacks
    
    private void Awake()
    {
        CacheComponents();
        _spawnPosition = transform.position;
    }
    
    private void Start()
    {
        Initialize();
    }

private void OnDestroy()
    {
        // 체력바 정리
        if (_healthBar != null)
        {
            _healthBar.Cleanup();
        }
    }

    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// 컴포넌트 캐싱
    /// </summary>
    private void CacheComponents()
    {
        _health = GetComponent<EnemyHealth>();
        _ai = GetComponent<EnemyAI>();
        _attack = GetComponent<EnemyAttack>();
    }
    
    /// <summary>
    /// EnemyData 기반 전체 초기화
    /// </summary>
public void Initialize()
    {
        if (_enemyData == null)
        {
            Debug.LogWarning($"[EnemyBase] {gameObject.name}: EnemyData가 할당되지 않았습니다!");
            _isInitialized = false;
            return;
        }
        
        if (_debugMode)
        {
            Debug.Log($"[EnemyBase] Initializing {_enemyData.enemyName}...");
        }
        
        // 각 컴포넌트 초기화
        InitializeHealth();
        InitializeAI();
        InitializeAttack();
        InitializeHealthBar();
        
        _isInitialized = true;
        
        if (_debugMode)
        {
            Debug.Log($"[EnemyBase] {_enemyData.enemyName} initialization complete!");
        }
    }
    
    /// <summary>
    /// 런타임에 EnemyData 변경 후 재초기화
    /// </summary>
    public void Initialize(EnemyData newData)
    {
        _enemyData = newData;
        Initialize();
    }
    
    /// <summary>
    /// 체력 컴포넌트 초기화
    /// </summary>
    private void InitializeHealth()
    {
        if (_health == null) return;
        
        _health.Initialize(
            maxHealth: _enemyData.maxHealth,
            armor: _enemyData.armor,
            headshotMultiplier: _enemyData.headshotMultiplier,
            hitEffectPrefab: _enemyData.hitEffectPrefab,
            deathEffectPrefab: _enemyData.deathEffectPrefab,
            hitFlashDuration: _enemyData.hitFlashDuration,
            hitFlashColor: _enemyData.hitFlashColor,
            hitSound: _enemyData.hitSound,
            deathSound: _enemyData.deathSound
        );
    }
    
    /// <summary>
    /// AI 컴포넌트 초기화
    /// </summary>
    private void InitializeAI()
    {
        if (_ai == null) return;
        
        _ai.Initialize(
            detectionRange: _enemyData.detectionRange,
            fieldOfView: _enemyData.fieldOfView,
            detectionHeight: _enemyData.detectionHeight,
            hearingRange: _enemyData.hearingRange,
            patrolSpeed: _enemyData.patrolSpeed,
            patrolWaitTime: _enemyData.patrolWaitTime,
            randomPatrol: _enemyData.randomPatrol,
            chaseSpeed: _enemyData.chaseSpeed,
            chaseRange: _enemyData.chaseRange,
            loseTargetTime: _enemyData.loseTargetTime,
            attackRange: _enemyData.attackRange,
            attackCooldown: _enemyData.attackCooldown,
            maxChaseDistance: _enemyData.maxChaseDistance,
            returnToSpawn: _enemyData.returnToSpawn,
            rotationSpeed: _enemyData.rotationSpeed,
            alertSound: _enemyData.alertSound
        );
    }
    
    /// <summary>
    /// 공격 컴포넌트 초기화
    /// </summary>
private void InitializeAttack()
    {
        if (_attack == null) return;
        
        // 전역 AttackType 명시적 사용
        global::AttackType type = _enemyData.attackType;
        
        _attack.Initialize(
            attackType: type,
            meleeDamage: _enemyData.meleeDamage,
            meleeRange: _enemyData.attackRange,
            meleeAngle: _enemyData.meleeAngle,
            meleeRangeThreshold: _enemyData.meleeRangeThreshold,
            rangedDamage: _enemyData.rangedDamage,
            rangedRange: _enemyData.rangedRange,
            projectileSpeed: _enemyData.projectileSpeed,
            aimAccuracy: _enemyData.aimAccuracy,
            projectilePrefab: _enemyData.projectilePrefab,
            meleeEffectPrefab: _enemyData.meleeEffectPrefab,
            muzzleFlashPrefab: _enemyData.muzzleFlashPrefab,
            meleeAttackSound: _enemyData.meleeAttackSound,
            rangedAttackSound: _enemyData.rangedAttackSound
        );
    }

/// <summary>
    /// 체력바 UI 초기화
    /// </summary>
    private void InitializeHealthBar()
    {
        if (_enemyData == null || _enemyData.healthBarPrefab == null)
        {
            if (_debugMode)
            {
                Debug.Log($"[EnemyBase] {gameObject.name}: HealthBar prefab not assigned");
            }
            return;
        }
        
        // 기존 체력바 제거
        if (_healthBar != null)
        {
            Destroy(_healthBar.gameObject);
        }
        
        // 체력바 생성
        GameObject healthBarObj = Instantiate(_enemyData.healthBarPrefab, transform);
        _healthBar = healthBarObj.GetComponent<EnemyHealthBar>();
        
        if (_healthBar != null)
        {
            _healthBar.Initialize(transform, _health);
            
            if (_debugMode)
            {
                Debug.Log($"[EnemyBase] {gameObject.name}: HealthBar initialized");
            }
        }
        else
        {
            Debug.LogWarning($"[EnemyBase] {gameObject.name}: HealthBar prefab missing EnemyHealthBar component!");
            Destroy(healthBarObj);
        }
    }

    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 리스폰 (Object Pool용)
    /// </summary>
    public void Respawn(Vector3 position)
    {
        transform.position = position;
        _spawnPosition = position;
        
        gameObject.SetActive(true);
        
        // 체력 초기화
        if (_health != null)
        {
            _health.ResetHealth();
        }
        
        // AI 리셋
        if (_ai != null)
        {
            _ai.ResetAI();
        }
        
        if (_debugMode)
        {
            Debug.Log($"[EnemyBase] {EnemyName} respawned at {position}");
        }
    }
    
    /// <summary>
    /// 디스폰 (Object Pool용)
    /// </summary>
    public void Despawn()
    {
        gameObject.SetActive(false);
        
        if (_debugMode)
        {
            Debug.Log($"[EnemyBase] {EnemyName} despawned");
        }
    }
    
    /// <summary>
    /// 드롭 아이템 처리
    /// </summary>
    public void DropItems()
    {
        if (_enemyData == null || _enemyData.dropItems == null) return;
        
        foreach (DropItem dropItem in _enemyData.dropItems)
        {
            if (dropItem.itemPrefab == null) continue;
            
            // 드롭 확률 체크
            if (Random.value > dropItem.dropChance) continue;
            
            // 수량 결정
            int quantity = Random.Range(dropItem.minQuantity, dropItem.maxQuantity + 1);
            
            for (int i = 0; i < quantity; i++)
            {
                Vector3 dropPosition = transform.position + Vector3.up * 0.5f;
                dropPosition += new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    0f,
                    Random.Range(-0.5f, 0.5f)
                );
                
                Instantiate(dropItem.itemPrefab, dropPosition, Quaternion.identity);
            }
        }
    }
    
    /// <summary>
    /// 경험치 보상 반환
    /// </summary>
    public int GetExperienceReward()
    {
        return _enemyData != null ? _enemyData.experienceReward : 0;
    }
    
    /// <summary>
    /// 점수 보상 반환
    /// </summary>
    public int GetScoreReward()
    {
        return _enemyData != null ? _enemyData.scoreReward : 0;
    }
    
    #endregion
    
    #region Editor
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_enemyData != null)
        {
            gameObject.name = $"Enemy_{_enemyData.enemyName}";
        }
    }
#endif
    
    #endregion



    #region IPoolable Implementation
    
    /// <summary>
    /// 오브젝트 최초 생성 시 호출
    /// </summary>
    public void OnCreated()
    {
        CacheComponents();
        
        if (_debugMode)
        {
            Debug.Log($"[EnemyBase] {gameObject.name} created in pool");
        }
    }
    
    /// <summary>
    /// 풀에서 가져올 때 호출
    /// </summary>
    public void OnGetFromPool()
    {
        _isInitialized = false;
        
        // EnemyData가 이미 할당되어 있으면 초기화
        if (_enemyData != null)
        {
            Initialize();
        }
        
        // 컴포넌트 리셋
        if (_health != null)
        {
            _health.ResetHealth();
        }
        
        if (_ai != null)
        {
            _ai.ResetAI();
        }
        
        if (_debugMode)
        {
            Debug.Log($"[EnemyBase] {EnemyName} retrieved from pool");
        }
    }
    
    /// <summary>
    /// 풀에 반환할 때 호출
    /// </summary>
    public void OnReturnToPool()
    {
        // AI 정지
        if (_ai != null)
        {
            _ai.ResetAI();
        }
        
        if (_debugMode)
        {
            Debug.Log($"[EnemyBase] {EnemyName} returned to pool");
        }
    }
    
    #endregion
}
