using UnityEngine;

/// <summary>
/// 적 데이터를 저장하는 ScriptableObject
/// Project 창에서 Create > Enemy > Enemy Data로 생성 가능
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    #region 기본 정보
    
    [Header("기본 정보")]
    [Tooltip("적 이름")]
    public string enemyName = "New Enemy";
    
    [Tooltip("적 타입")]
    public EnemyType enemyType = EnemyType.Normal;
    
    [Tooltip("적 아이콘 (UI용)")]
    public Sprite enemyIcon;
    
    [Tooltip("적 프리팹")]
    public GameObject enemyPrefab;
    
    [Tooltip("적 설명")]
    [TextArea(2, 4)]
    public string description;
    
    #endregion
    
    #region 체력 설정
    
    [Header("체력 설정")]
    [Tooltip("최대 체력")]
    [Range(1f, 1000f)]
    public float maxHealth = 100f;
    
    [Tooltip("방어력 (데미지 감소율)")]
    [Range(0f, 0.9f)]
    public float armor = 0f;
    
    [Tooltip("헤드샷 데미지 배율")]
    [Range(1f, 5f)]
    public float headshotMultiplier = 2f;
    
    #endregion
    
    #region 이동 설정
    
    [Header("이동 설정")]
    [Tooltip("순찰 속도")]
    [Range(0.5f, 5f)]
    public float patrolSpeed = 2f;
    
    [Tooltip("추적 속도")]
    [Range(1f, 10f)]
    public float chaseSpeed = 5f;
    
    [Tooltip("회전 속도")]
    [Range(1f, 20f)]
    public float rotationSpeed = 10f;
    
    [Tooltip("순찰 포인트 대기 시간")]
    [Range(0f, 10f)]
    public float patrolWaitTime = 2f;
    
    [Tooltip("랜덤 순찰 여부")]
    public bool randomPatrol = false;
    
    #endregion
    
    #region 감지 설정
    
    [Header("감지 설정")]
    [Tooltip("감지 범위")]
    [Range(5f, 50f)]
    public float detectionRange = 15f;
    
    [Tooltip("시야각 (도)")]
    [Range(30f, 360f)]
    public float fieldOfView = 120f;
    
    [Tooltip("감지 높이 차이")]
    [Range(0.5f, 10f)]
    public float detectionHeight = 2f;
    
    [Tooltip("소리 감지 범위 (0 = 비활성화)")]
    [Range(0f, 30f)]
    public float hearingRange = 10f;
    
    #endregion
    
    #region 추적 설정
    
    [Header("추적 설정")]
    [Tooltip("추적 유지 범위")]
    [Range(5f, 50f)]
    public float chaseRange = 20f;
    
    [Tooltip("타겟 상실 후 추적 유지 시간")]
    [Range(1f, 15f)]
    public float loseTargetTime = 5f;
    
    [Tooltip("최대 추적 거리 (스폰 위치 기준)")]
    [Range(10f, 100f)]
    public float maxChaseDistance = 30f;
    
    [Tooltip("스폰 위치로 복귀 여부")]
    public bool returnToSpawn = true;
    
    #endregion
    
    #region 공격 설정
    
    [Header("공격 설정")]
    [Tooltip("공격 타입")]
    public AttackType attackType = AttackType.Melee;
    
    [Tooltip("공격 범위")]
    [Range(1f, 30f)]
    public float attackRange = 2f;
    
    [Tooltip("공격 쿨다운")]
    [Range(0.1f, 5f)]
    public float attackCooldown = 1.5f;
    
    [Header("근접 공격")]
    [Tooltip("근접 데미지")]
    [Range(1f, 100f)]
    public float meleeDamage = 20f;
    
    [Tooltip("근접 공격 각도")]
    [Range(30f, 180f)]
    public float meleeAngle = 90f;
    
    [Tooltip("근접/원거리 전환 거리 (Both 타입 시)")]
    [Range(1f, 10f)]
    public float meleeRangeThreshold = 3f;
    
    [Header("원거리 공격")]
    [Tooltip("원거리 데미지")]
    [Range(1f, 100f)]
    public float rangedDamage = 15f;
    
    [Tooltip("원거리 사거리")]
    [Range(5f, 100f)]
    public float rangedRange = 20f;
    
    [Tooltip("발사체 속도")]
    [Range(5f, 100f)]
    public float projectileSpeed = 30f;
    
    [Tooltip("조준 정확도 (0~1)")]
    [Range(0f, 1f)]
    public float aimAccuracy = 0.9f;
    
    [Tooltip("발사체 프리팹")]
    public GameObject projectilePrefab;
    
    #endregion
    
    #region 이펙트 설정
    
    [Header("이펙트 설정")]
    [Tooltip("피격 이펙트 프리팹")]
    public GameObject hitEffectPrefab;
    
    [Tooltip("사망 이펙트 프리팹")]
    public GameObject deathEffectPrefab;
    
    [Tooltip("체력바 UI 프리팹")]
    public GameObject healthBarPrefab;
    
    [Tooltip("공격 이펙트 프리팹 (근접)")]
    public GameObject meleeEffectPrefab;
    
    [Tooltip("머즐 플래시 프리팹 (원거리)")]
    public GameObject muzzleFlashPrefab;
    
    [Tooltip("피격 플래시 지속시간")]
    [Range(0.05f, 0.5f)]
    public float hitFlashDuration = 0.1f;
    
    [Tooltip("피격 플래시 색상")]
    public Color hitFlashColor = Color.red;
    
    #endregion
    
    #region 사운드 설정
    
    [Header("사운드 설정")]
    [Tooltip("근접 공격 사운드")]
    public AudioClip meleeAttackSound;
    
    [Tooltip("원거리 공격 사운드")]
    public AudioClip rangedAttackSound;
    
    [Tooltip("피격 사운드")]
    public AudioClip hitSound;
    
    [Tooltip("사망 사운드")]
    public AudioClip deathSound;
    
    [Tooltip("경계/발견 사운드")]
    public AudioClip alertSound;
    
    [Tooltip("순찰 중 발소리")]
    public AudioClip footstepSound;
    
    #endregion
    
    #region 보상 설정
    
    [Header("보상 설정")]
    [Tooltip("처치 시 경험치")]
    [Range(0, 1000)]
    public int experienceReward = 10;
    
    [Tooltip("처치 시 점수")]
    [Range(0, 10000)]
    public int scoreReward = 100;
    
    [Tooltip("드롭 아이템 목록")]
    public DropItem[] dropItems;
    
    #endregion
    
    #region 스폰 설정
    
    [Header("스폰 설정")]
    [Tooltip("스폰 가중치 (높을수록 자주 스폰)")]
    [Range(1, 100)]
    public int spawnWeight = 10;
    
    [Tooltip("최소 출현 웨이브/레벨")]
    [Range(1, 100)]
    public int minSpawnWave = 1;
    
    [Tooltip("최대 동시 스폰 수")]
    [Range(1, 50)]
    public int maxConcurrentSpawn = 10;
    
    #endregion
    
    #region 헬퍼 메서드
    
    /// <summary>
    /// 방어력 적용된 실제 데미지 계산
    /// </summary>
    public float CalculateDamage(float incomingDamage, bool isHeadshot = false)
    {
        float damage = incomingDamage * (1f - armor);
        if (isHeadshot)
        {
            damage *= headshotMultiplier;
        }
        return Mathf.Max(1f, damage);
    }
    
    /// <summary>
    /// 현재 공격 타입에 따른 데미지 반환
    /// </summary>
    public float GetDamageByType(bool useRanged)
    {
        return useRanged ? rangedDamage : meleeDamage;
    }
    
    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public override string ToString()
    {
        return $"[EnemyData] {enemyName} (Type: {enemyType}, HP: {maxHealth}, ATK: {meleeDamage}/{rangedDamage})";
    }
    
    #endregion
}

#region 열거형 정의

/// <summary>
/// 적 타입 열거형
/// </summary>
public enum EnemyType
{
    Normal,     // 일반 적
    Elite,      // 정예 적
    Boss,       // 보스
    Minion,     // 소환된 하수인
    Turret,     // 고정 포탑
    Swarm       // 떼 (다수 스폰)
}

/// <summary>
/// 공격 타입 열거형
/// </summary>
public enum AttackType
{
    Melee,      // 근접 공격
    Ranged,     // 원거리 공격
    Both        // 혼합 (거리에 따라 전환)
}

#endregion

#region 드롭 아이템 구조체

/// <summary>
/// 드롭 아이템 정보
/// </summary>
[System.Serializable]
public struct DropItem
{
    [Tooltip("드롭 아이템 프리팹")]
    public GameObject itemPrefab;
    
    [Tooltip("드롭 확률 (0~1)")]
    [Range(0f, 1f)]
    public float dropChance;
    
    [Tooltip("최소 드롭 수량")]
    [Range(1, 10)]
    public int minQuantity;
    
    [Tooltip("최대 드롭 수량")]
    [Range(1, 10)]
    public int maxQuantity;
}

#endregion