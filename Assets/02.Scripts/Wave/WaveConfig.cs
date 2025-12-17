using UnityEngine;
using System;

/// <summary>
/// 웨이브 설정 ScriptableObject
/// Project 창에서 Create > Game/Wave/Wave Config로 생성 가능
/// </summary>
[CreateAssetMenu(fileName = "WaveConfig", menuName = "Game/Wave/Wave Config")]
public class WaveConfig : ScriptableObject
{
    #region Basic Info
    [Header("웨이브 정보")]
    [Tooltip("웨이브 번호")]
    public int waveNumber;

    [Tooltip("웨이브 이름")]
    public string waveName;

    [TextArea(2, 3)]
    [Tooltip("웨이브 설명")]
    public string description;
    #endregion

    #region Timing
    [Header("타이밍")]
    [Tooltip("준비 시간 (초)")]
    [Range(0f, 30f)]
    public float preparationTime = 5f;

    [Tooltip("웨이브 최대 지속 시간 (초)")]
    [Range(30f, 300f)]
    public float waveDuration = 60f;
    #endregion

    #region Spawn Groups
    [Header("스폰 그룹")]
    [Tooltip("적 스폰 그룹 목록")]
    public WaveSpawnGroup[] spawnGroups;
    #endregion

    #region Boss
    [Header("보스")]
    [Tooltip("보스 웨이브 여부")]
    public bool hasBoss;

    [Tooltip("보스 적 데이터")]
    public EnemyData bossEnemy;

    [Tooltip("보스 스폰 시점 (초)")]
    [Range(0f, 120f)]
    public float bossSpawnTime = 45f;

    [Tooltip("보스 수")]
    [Range(1, 5)]
    public int bossCount = 1;
    #endregion

    #region Rewards
    [Header("보상")]
    [Tooltip("골드 보상")]
    [Range(0, 10000)]
    public int goldReward = 100;

    [Tooltip("젬 보상")]
    [Range(0, 100)]
    public int gemReward = 0;

    [Tooltip("경험치 보상")]
    [Range(0, 1000)]
    public int experienceReward = 50;

    [Tooltip("보너스 아이템 드롭 확률")]
    [Range(0f, 1f)]
    public float bonusDropChance = 0f;
    #endregion

    #region Difficulty Scaling
    [Header("난이도 스케일링")]
    [Tooltip("적 체력 배율")]
    [Range(0.5f, 5f)]
    public float healthMultiplier = 1f;

    [Tooltip("적 공격력 배율")]
    [Range(0.5f, 5f)]
    public float damageMultiplier = 1f;

    [Tooltip("스폰 속도 배율 (높을수록 빠름)")]
    [Range(0.5f, 3f)]
    public float spawnRateMultiplier = 1f;

    [Tooltip("적 이동속도 배율")]
    [Range(0.5f, 2f)]
    public float speedMultiplier = 1f;
    #endregion

    #region Special Events
    [Header("특수 이벤트")]
    [Tooltip("특수 이벤트 타입")]
    public WaveEventType eventType = WaveEventType.None;

    [Tooltip("이벤트 시작 시점 (초)")]
    public float eventStartTime = 30f;

    [Tooltip("이벤트 지속 시간 (초)")]
    public float eventDuration = 10f;
    #endregion

    #region Calculated Properties
    /// <summary>
    /// 총 적 수 (보스 포함)
    /// </summary>
    public int TotalEnemyCount
    {
        get
        {
            int total = 0;
            if (spawnGroups != null)
            {
                foreach (var group in spawnGroups)
                {
                    if (group.enemyType != null)
                        total += group.count;
                }
            }
            if (hasBoss) total += bossCount;
            return total;
        }
    }

    /// <summary>
    /// 예상 총 스폰 시간
    /// </summary>
    public float EstimatedSpawnDuration
    {
        get
        {
            float duration = 0f;
            if (spawnGroups != null)
            {
                foreach (var group in spawnGroups)
                {
                    duration += group.startDelay;
                    if (!group.spawnAllAtOnce)
                    {
                        duration += group.count * group.spawnInterval / spawnRateMultiplier;
                    }
                    duration += group.groupDelay;
                }
            }
            return duration;
        }
    }

    /// <summary>
    /// 총 보상 가치 (골드 + 젬*10)
    /// </summary>
    public int TotalRewardValue => goldReward + gemReward * 10;
    #endregion

    #region Validation
#if UNITY_EDITOR
    private void OnValidate()
    {
        // 자동 이름 설정
        if (string.IsNullOrEmpty(waveName))
        {
            waveName = $"Wave {waveNumber}";
        }

        // 값 검증
        preparationTime = Mathf.Max(0f, preparationTime);
        waveDuration = Mathf.Max(10f, waveDuration);
        goldReward = Mathf.Max(0, goldReward);
        gemReward = Mathf.Max(0, gemReward);
        experienceReward = Mathf.Max(0, experienceReward);

        // 보스 스폰 시간 검증
        if (hasBoss)
        {
            bossSpawnTime = Mathf.Clamp(bossSpawnTime, 0f, waveDuration);
        }
    }

    [ContextMenu("Print Wave Summary")]
    private void PrintWaveSummary()
    {
        Debug.Log($"=== Wave {waveNumber}: {waveName} ===");
        Debug.Log($"적 수: {TotalEnemyCount} (보스: {(hasBoss ? bossCount : 0)})");
        Debug.Log($"준비 시간: {preparationTime}s | 지속 시간: {waveDuration}s");
        Debug.Log($"난이도: HP x{healthMultiplier}, DMG x{damageMultiplier}");
        Debug.Log($"보상: Gold {goldReward}, Gem {gemReward}, Exp {experienceReward}");
        Debug.Log($"예상 스폰 시간: {EstimatedSpawnDuration:F1}s");
    }

    [ContextMenu("Auto Balance Difficulty")]
    private void AutoBalanceDifficulty()
    {
        // 웨이브 번호 기반 자동 난이도 설정
        healthMultiplier = 1f + (waveNumber - 1) * 0.15f;
        damageMultiplier = 1f + (waveNumber - 1) * 0.1f;
        spawnRateMultiplier = 1f + (waveNumber - 1) * 0.05f;

        // 보상 자동 설정
        goldReward = 50 + waveNumber * 25;
        experienceReward = 25 + waveNumber * 15;
        gemReward = waveNumber >= 5 ? (waveNumber - 4) : 0;

        Debug.Log($"웨이브 {waveNumber} 난이도 자동 밸런스 완료");
    }
#endif
    #endregion
}

#region Spawn Group
/// <summary>
/// 웨이브 스폰 그룹 설정
/// </summary>
[Serializable]
public class WaveSpawnGroup
{
    [Header("적 설정")]
    [Tooltip("스폰할 적 데이터")]
    public EnemyData enemyType;

    [Tooltip("스폰 수")]
    [Range(1, 100)]
    public int count = 5;

    [Header("타이밍")]
    [Tooltip("그룹 시작 딜레이 (초)")]
    [Range(0f, 30f)]
    public float startDelay = 0f;

    [Tooltip("각 적 스폰 간격 (초)")]
    [Range(0.1f, 10f)]
    public float spawnInterval = 1f;

    [Tooltip("다음 그룹까지 딜레이 (초)")]
    [Range(0f, 30f)]
    public float groupDelay = 3f;

    [Header("옵션")]
    [Tooltip("한번에 모두 스폰")]
    public bool spawnAllAtOnce = false;

    [Tooltip("스폰 패턴")]
    public SpawnPattern pattern = SpawnPattern.Random;

    [Tooltip("특정 스폰 포인트 인덱스 (-1 = 모두 사용)")]
    public int specificSpawnPointIndex = -1;
}
#endregion

#region Enums
/// <summary>
/// 스폰 패턴
/// </summary>
public enum SpawnPattern
{
    [Tooltip("랜덤 스폰 포인트")]
    Random,

    [Tooltip("순차적 스폰 포인트")]
    Sequential,

    [Tooltip("플레이어 주변")]
    Surrounding,

    [Tooltip("단일 포인트")]
    Single
}

/// <summary>
/// 웨이브 특수 이벤트 타입
/// </summary>
public enum WaveEventType
{
    None,           // 없음
    EliteSpawn,     // 엘리트 적 추가 스폰
    ResourceBonus,  // 자원 보너스
    SpeedBoost,     // 적 속도 증가
    HealthRegen,    // 플레이어 체력 회복
    DoubleDamage,   // 플레이어 데미지 2배
    Swarm           // 다수 약한 적 스폰
}
#endregion
