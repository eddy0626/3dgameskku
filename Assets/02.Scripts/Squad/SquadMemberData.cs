using UnityEngine;

/// <summary>
/// 분대원 데이터를 저장하는 ScriptableObject
/// Project 창에서 Create > Game/Squad > Member Data로 생성 가능
/// WeaponData.cs 패턴 참고
/// </summary>
[CreateAssetMenu(fileName = "NewSquadMemberData", menuName = "Game/Squad/Member Data")]
public class SquadMemberData : ScriptableObject
{
    #region Basic Info
    [Header("기본 정보")]
    [Tooltip("분대원 이름")]
    public string memberName = "Squad Member";

    [Tooltip("분대원 아이콘 (UI용)")]
    public Sprite icon;

    [Tooltip("분대원 프리팹")]
    public GameObject prefab;

    [Tooltip("분대원 역할/클래스")]
    public MemberRole role = MemberRole.Rifleman;

    [TextArea(2, 4)]
    [Tooltip("분대원 설명")]
    public string description;
    #endregion

    #region Stats
    [Header("기본 스탯")]
    [Tooltip("최대 체력")]
    [Range(50f, 500f)]
    public float maxHealth = 100f;

    [Tooltip("기본 공격력")]
    [Range(5f, 100f)]
    public float damage = 10f;

    [Tooltip("공격 쿨다운 (초)")]
    [Range(0.1f, 5f)]
    public float attackCooldown = 1f;

    [Tooltip("공격 사거리")]
    [Range(1f, 30f)]
    public float attackRange = 5f;

    [Tooltip("크리티컬 확률 (0~1)")]
    [Range(0f, 1f)]
    public float criticalChance = 0.1f;

    [Tooltip("크리티컬 데미지 배율")]
    [Range(1.5f, 3f)]
    public float criticalMultiplier = 2f;
    #endregion

    #region Movement
    [Header("이동 설정")]
    [Tooltip("이동 속도")]
    [Range(1f, 10f)]
    public float moveSpeed = 3.5f;

    [Tooltip("회전 속도")]
    [Range(1f, 20f)]
    public float rotationSpeed = 5f;

    [Tooltip("플레이어 따라다니는 거리")]
    [Range(1f, 10f)]
    public float followDistance = 3f;

    [Tooltip("최대 따라가기 거리 (이 이상이면 재집결)")]
    [Range(10f, 50f)]
    public float maxFollowDistance = 15f;
    #endregion

    #region Detection
    [Header("탐지 설정")]
    [Tooltip("적 탐지 범위")]
    [Range(5f, 30f)]
    public float detectionRange = 10f;

    [Tooltip("적 레이어 마스크")]
    public LayerMask enemyLayer;

    [Tooltip("시야각 (도)")]
    [Range(30f, 360f)]
    public float fieldOfView = 120f;

    [Tooltip("장애물 레이어 (시야 차단)")]
    public LayerMask obstacleLayer;
    #endregion

    #region Combat
    [Header("전투 설정")]
    [Tooltip("공격 타입")]
    public AttackType attackType = AttackType.Ranged;

    [Tooltip("근접 공격 범위")]
    [Range(1f, 5f)]
    public float meleeRange = 2f;

    [Tooltip("발사체 프리팹 (원거리 공격 시)")]
    public GameObject projectilePrefab;

    [Tooltip("발사체 속도")]
    [Range(5f, 50f)]
    public float projectileSpeed = 20f;

    [Tooltip("점사 발수 (0=단발)")]
    [Range(0, 5)]
    public int burstCount = 0;

    [Tooltip("점사 간격 (초)")]
    [Range(0.05f, 0.3f)]
    public float burstInterval = 0.1f;
    #endregion

    #region Upgrades
    [Header("레벨당 업그레이드")]
    [Tooltip("레벨당 데미지 증가")]
    public float damagePerLevel = 2f;

    [Tooltip("레벨당 체력 증가")]
    public float healthPerLevel = 10f;

    [Tooltip("레벨당 이동속도 증가")]
    public float speedPerLevel = 0.1f;

    [Tooltip("레벨당 공격속도 증가")]
    public float attackSpeedPerLevel = 0.05f;

    [Tooltip("최대 레벨")]
    [Range(1, 10)]
    public int maxLevel = 5;
    #endregion

    #region Effects
    [Header("이펙트")]
    [Tooltip("스폰 시 이펙트")]
    public GameObject spawnEffect;

    [Tooltip("공격 시 이펙트")]
    public GameObject attackEffect;

    [Tooltip("피격 시 이펙트")]
    public GameObject hitEffect;

    [Tooltip("사망 시 이펙트")]
    public GameObject deathEffect;

    [Tooltip("레벨업 이펙트")]
    public GameObject levelUpEffect;
    #endregion

    #region Audio
    [Header("사운드")]
    [Tooltip("공격 사운드")]
    public AudioClip attackSound;

    [Tooltip("피격 사운드")]
    public AudioClip hurtSound;

    [Tooltip("사망 사운드")]
    public AudioClip deathSound;

    [Tooltip("스폰 사운드")]
    public AudioClip spawnSound;
    #endregion

    #region Recruitment
    [Header("영입 설정")]
    [Tooltip("영입 비용 (골드)")]
    [Range(0, 10000)]
    public int recruitCost = 100;

    [Tooltip("해금 웨이브")]
    [Range(1, 50)]
    public int unlockWave = 1;

    [Tooltip("희귀도 (영입 확률 및 강화에 영향)")]
    public MemberRarity rarity = MemberRarity.Common;
    #endregion

    #region Calculated Properties
    /// <summary>
    /// 초당 공격 횟수 (DPS 계산용)
    /// </summary>
    public float AttackRate => 1f / attackCooldown;

    /// <summary>
    /// 기본 DPS
    /// </summary>
    public float BaseDPS => damage * AttackRate;

    /// <summary>
    /// 크리티컬 포함 평균 DPS
    /// </summary>
    public float AverageDPS => BaseDPS * (1f + criticalChance * (criticalMultiplier - 1f));

    /// <summary>
    /// 특정 레벨에서의 데미지
    /// </summary>
    public float GetDamageAtLevel(int level) => damage + damagePerLevel * (level - 1);

    /// <summary>
    /// 특정 레벨에서의 체력
    /// </summary>
    public float GetHealthAtLevel(int level) => maxHealth + healthPerLevel * (level - 1);

    /// <summary>
    /// 특정 레벨에서의 이동속도
    /// </summary>
    public float GetSpeedAtLevel(int level) => moveSpeed + speedPerLevel * (level - 1);
    #endregion

    #region Validation
#if UNITY_EDITOR
    private void OnValidate()
    {
        // 값 범위 검증
        maxHealth = Mathf.Max(1f, maxHealth);
        damage = Mathf.Max(0f, damage);
        attackCooldown = Mathf.Max(0.1f, attackCooldown);
        attackRange = Mathf.Max(0.5f, attackRange);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        detectionRange = Mathf.Max(attackRange, detectionRange);
        meleeRange = Mathf.Min(meleeRange, attackRange);

        // 자동 이름 설정
        if (string.IsNullOrEmpty(memberName))
        {
            memberName = name;
        }
    }

    /// <summary>
    /// 스탯 요약 출력 (에디터용)
    /// </summary>
    [ContextMenu("Print Stats Summary")]
    private void PrintStatsSummary()
    {
        Debug.Log($"=== {memberName} Stats ===");
        Debug.Log($"Role: {role} | Rarity: {rarity}");
        Debug.Log($"HP: {maxHealth} | DMG: {damage} | ATK Rate: {AttackRate:F2}/s");
        Debug.Log($"Base DPS: {BaseDPS:F1} | Avg DPS (w/ Crit): {AverageDPS:F1}");
        Debug.Log($"Move Speed: {moveSpeed} | Detection: {detectionRange}");
        Debug.Log($"Cost: {recruitCost} Gold | Unlock Wave: {unlockWave}");
    }
#endif
    #endregion
}

/// <summary>
/// 분대원 역할/클래스
/// </summary>
public enum MemberRole
{
    Rifleman,   // 소총수 - 균형잡힌 스탯
    Tank,       // 탱커 - 높은 체력, 낮은 이동속도
    Sniper,     // 저격수 - 높은 데미지, 긴 사거리
    Medic,      // 의무병 - 치유 능력
    Support,    // 지원병 - 버프/디버프
    Scout,      // 정찰병 - 빠른 이동, 넓은 시야
    Heavy,      // 중화기병 - 높은 화력, 느린 공격속도
    Engineer    // 공병 - 설치물/터렛 배치
}

/// <summary>
/// 분대원 희귀도
/// </summary>
public enum MemberRarity
{
    Common,     // 일반 (흰색)
    Uncommon,   // 고급 (녹색)
    Rare,       // 희귀 (파란색)
    Epic,       // 영웅 (보라색)
    Legendary   // 전설 (주황색)
}
