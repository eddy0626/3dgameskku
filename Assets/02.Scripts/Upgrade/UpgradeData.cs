using UnityEngine;

/// <summary>
/// 업그레이드 데이터 ScriptableObject
/// Project 창에서 Create > Game/Upgrade > Upgrade Data로 생성 가능
/// WeaponData.cs 패턴 참고
/// </summary>
[CreateAssetMenu(fileName = "NewUpgradeData", menuName = "Game/Upgrade/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    #region Basic Info
    [Header("기본 정보")]
    [Tooltip("업그레이드 이름")]
    public string upgradeName = "New Upgrade";

    [TextArea(2, 4)]
    [Tooltip("업그레이드 설명")]
    public string description;

    [Tooltip("업그레이드 아이콘 (UI용)")]
    public Sprite icon;

    [Tooltip("업그레이드 ID (고유 식별자)")]
    public string upgradeId;
    #endregion

    #region Upgrade Type
    [Header("업그레이드 타입")]
    [Tooltip("스탯 타입 (Damage, Health, Speed, AttackRate 등)")]
    public UpgradeType statType = UpgradeType.Damage;

    [Tooltip("적용 대상 (Player, Squad, All)")]
    public UpgradeTarget target = UpgradeTarget.Player;

    [Tooltip("희귀도")]
    public UpgradeRarity rarity = UpgradeRarity.Common;

    [Tooltip("업그레이드 카테고리")]
    public UpgradeCategory category = UpgradeCategory.Offensive;
    #endregion

    #region Cost Settings
    [Header("비용 설정")]
    [Tooltip("기본 비용 (레벨 1)")]
    [Range(0, 10000)]
    public int baseCost = 100;

    [Tooltip("레벨당 비용 배율 (비용 = baseCost * costMultiplier^(level-1))")]
    [Range(1f, 5f)]
    public float costMultiplier = 1.5f;

    [Tooltip("수동 비용 배열 사용 (체크 시 위 계산식 대신 배열 사용)")]
    public bool useManualCosts = false;

    [Tooltip("레벨별 수동 비용 (useManualCosts가 true일 때 사용)")]
    public int[] manualCostPerLevel = { 100, 200, 400, 800, 1600 };
    #endregion

    #region Value Settings
    [Header("수치 설정")]
    [Tooltip("레벨당 증가 수치")]
    public float valuePerLevel = 5f;

    [Tooltip("수동 수치 배열 사용 (체크 시 위 값 대신 배열 사용)")]
    public bool useManualValues = false;

    [Tooltip("레벨별 수동 수치 (useManualValues가 true일 때 사용)")]
    public float[] manualValuePerLevel = { 5f, 10f, 15f, 20f, 25f };

    [Tooltip("퍼센트 기반 수치인지 여부")]
    public bool isPercentage = false;

    [Tooltip("곱연산 수치인지 여부 (false = 합연산)")]
    public bool isMultiplicative = false;
    #endregion

    #region Level Settings
    [Header("레벨 설정")]
    [Tooltip("최대 레벨")]
    [Range(1, 20)]
    public int maxLevel = 5;

    [Tooltip("시작 레벨 (기본값)")]
    [Range(0, 5)]
    public int startLevel = 0;
    #endregion

    #region Requirements
    [Header("해금 조건")]
    [Tooltip("해금에 필요한 웨이브")]
    [Range(1, 100)]
    public int unlockWave = 1;

    [Tooltip("선행 업그레이드 (이 업그레이드들을 먼저 완료해야 함)")]
    public UpgradeData[] prerequisites;

    [Tooltip("필요 플레이어 레벨")]
    [Range(1, 100)]
    public int requiredPlayerLevel = 1;

    [Tooltip("해금 비용 (최초 해금 시 필요, 0 = 무료)")]
    public int unlockCost = 0;
    #endregion

    #region Visual & Audio
    [Header("시각 효과")]
    [Tooltip("업그레이드 색상 (UI 및 이펙트)")]
    public Color upgradeColor = Color.white;

    [Tooltip("업그레이드 적용 시 이펙트")]
    public GameObject upgradeEffect;

    [Tooltip("업그레이드 최대 레벨 달성 시 이펙트")]
    public GameObject maxLevelEffect;

    [Header("사운드")]
    [Tooltip("업그레이드 적용 사운드")]
    public AudioClip upgradeSound;

    [Tooltip("해금 사운드")]
    public AudioClip unlockSound;
    #endregion

    #region Calculated Properties
    /// <summary>
    /// 실제 최대 레벨 (수동 배열 사용 시 배열 크기 고려)
    /// </summary>
    public int MaxLevel
    {
        get
        {
            if (useManualValues && manualValuePerLevel != null)
                return Mathf.Min(maxLevel, manualValuePerLevel.Length);
            if (useManualCosts && manualCostPerLevel != null)
                return Mathf.Min(maxLevel, manualCostPerLevel.Length);
            return maxLevel;
        }
    }

    /// <summary>
    /// 희귀도 색상
    /// </summary>
    public Color RarityColor => GetRarityColor(rarity);
    #endregion

    #region Public Methods
    /// <summary>
    /// 특정 레벨에서의 수치
    /// </summary>
    public float GetValueAtLevel(int level)
    {
        if (level <= 0) return 0f;
        if (level > MaxLevel) return GetValueAtLevel(MaxLevel);

        if (useManualValues && manualValuePerLevel != null && level <= manualValuePerLevel.Length)
        {
            return manualValuePerLevel[level - 1];
        }

        return valuePerLevel * level;
    }

    /// <summary>
    /// 특정 레벨까지의 총 누적 수치
    /// </summary>
    public float GetTotalValueAtLevel(int level)
    {
        if (useManualValues && manualValuePerLevel != null)
        {
            float total = 0f;
            for (int i = 0; i < level && i < manualValuePerLevel.Length; i++)
            {
                total += manualValuePerLevel[i];
            }
            return total;
        }

        // 등차급수: n * (n + 1) / 2 * valuePerLevel
        return level * (level + 1) / 2f * valuePerLevel;
    }

    /// <summary>
    /// 특정 레벨에서의 비용
    /// </summary>
    public int GetCostAtLevel(int level)
    {
        if (level <= 0 || level > MaxLevel) return int.MaxValue;

        if (useManualCosts && manualCostPerLevel != null && level <= manualCostPerLevel.Length)
        {
            return manualCostPerLevel[level - 1];
        }

        // 공식: baseCost * costMultiplier^(level-1)
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, level - 1));
    }

    /// <summary>
    /// 특정 레벨까지의 총 비용
    /// </summary>
    public int GetTotalCostToLevel(int level)
    {
        int total = 0;
        for (int i = 1; i <= level && i <= MaxLevel; i++)
        {
            total += GetCostAtLevel(i);
        }
        return total;
    }

    /// <summary>
    /// 다음 레벨 업그레이드 가능 여부
    /// </summary>
    public bool CanUpgrade(int currentLevel, int currentGold)
    {
        if (currentLevel >= MaxLevel) return false;
        return currentGold >= GetCostAtLevel(currentLevel + 1);
    }

    /// <summary>
    /// 선행 조건 충족 여부
    /// </summary>
    public bool ArePrerequisitesMet(System.Func<UpgradeData, int> getLevelFunc)
    {
        if (prerequisites == null || prerequisites.Length == 0) return true;

        foreach (var prereq in prerequisites)
        {
            if (prereq != null && getLevelFunc(prereq) < prereq.MaxLevel)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 포맷된 설명 텍스트 (현재/다음 레벨 비교)
    /// </summary>
    public string GetFormattedDescription(int currentLevel)
    {
        string baseDesc = description;

        if (currentLevel >= MaxLevel)
        {
            return $"{baseDesc}\n<color=#FFD700>MAX LEVEL</color>";
        }

        float currentValue = GetTotalValueAtLevel(currentLevel);
        float nextValue = GetTotalValueAtLevel(currentLevel + 1);
        int nextCost = GetCostAtLevel(currentLevel + 1);

        return $"{baseDesc}\n" +
               $"현재: <color=#00FF00>{FormatValue(currentValue)}</color>\n" +
               $"다음: <color=#00FFFF>{FormatValue(nextValue)}</color> " +
               $"(<color=#FFD700>{nextCost:N0}</color> 골드)";
    }

    /// <summary>
    /// 수치 포맷팅
    /// </summary>
    public string FormatValue(float value)
    {
        if (isPercentage)
        {
            return $"{value:F1}%";
        }

        return statType switch
        {
            UpgradeType.CriticalChance => $"{value:F1}%",
            UpgradeType.CriticalDamage => $"{value:F0}%",
            UpgradeType.AttackSpeed => $"+{value:F2}",
            UpgradeType.Speed => $"+{value:F1}",
            UpgradeType.MagnetRange => $"+{value:F1}m",
            UpgradeType.AttackRange => $"+{value:F1}m",
            UpgradeType.SquadSize => $"+{value:F0}",
            _ => $"+{value:F0}"
        };
    }

    /// <summary>
    /// 스탯 타입 이름 (한글)
    /// </summary>
    public string GetStatTypeName()
    {
        return statType switch
        {
            UpgradeType.Damage => "공격력",
            UpgradeType.Health => "체력",
            UpgradeType.Speed => "이동 속도",
            UpgradeType.AttackSpeed => "공격 속도",
            UpgradeType.AttackRange => "공격 범위",
            UpgradeType.MagnetRange => "마그넷 범위",
            UpgradeType.SquadSize => "분대 인원",
            UpgradeType.CriticalChance => "치명타 확률",
            UpgradeType.CriticalDamage => "치명타 데미지",
            _ => statType.ToString()
        };
    }

    /// <summary>
    /// 희귀도 이름 (한글)
    /// </summary>
    public static string GetRarityName(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Common => "일반",
            UpgradeRarity.Uncommon => "고급",
            UpgradeRarity.Rare => "희귀",
            UpgradeRarity.Epic => "영웅",
            UpgradeRarity.Legendary => "전설",
            _ => rarity.ToString()
        };
    }

    /// <summary>
    /// 희귀도 색상
    /// </summary>
    public static Color GetRarityColor(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Common => Color.white,
            UpgradeRarity.Uncommon => new Color(0.3f, 1f, 0.3f),     // Green
            UpgradeRarity.Rare => new Color(0.3f, 0.5f, 1f),         // Blue
            UpgradeRarity.Epic => new Color(0.7f, 0.3f, 1f),         // Purple
            UpgradeRarity.Legendary => new Color(1f, 0.65f, 0f),     // Orange
            _ => Color.white
        };
    }

    /// <summary>
    /// 카테고리 이름 (한글)
    /// </summary>
    public static string GetCategoryName(UpgradeCategory category)
    {
        return category switch
        {
            UpgradeCategory.Offensive => "공격",
            UpgradeCategory.Defensive => "방어",
            UpgradeCategory.Utility => "유틸리티",
            UpgradeCategory.Squad => "분대",
            UpgradeCategory.Resource => "자원",
            _ => category.ToString()
        };
    }
    #endregion

    #region Validation
#if UNITY_EDITOR
    private void OnValidate()
    {
        // 기본값 검증
        baseCost = Mathf.Max(0, baseCost);
        costMultiplier = Mathf.Max(1f, costMultiplier);
        maxLevel = Mathf.Max(1, maxLevel);
        valuePerLevel = Mathf.Max(0f, valuePerLevel);

        // ID 자동 생성
        if (string.IsNullOrEmpty(upgradeId))
        {
            upgradeId = name.ToLower().Replace(" ", "_");
        }

        // 수동 배열 길이 검증
        if (useManualCosts && (manualCostPerLevel == null || manualCostPerLevel.Length == 0))
        {
            manualCostPerLevel = new int[] { baseCost };
        }

        if (useManualValues && (manualValuePerLevel == null || manualValuePerLevel.Length == 0))
        {
            manualValuePerLevel = new float[] { valuePerLevel };
        }
    }

    [ContextMenu("Print Cost Table")]
    private void PrintCostTable()
    {
        Debug.Log($"=== {upgradeName} Cost Table ===");
        for (int i = 1; i <= MaxLevel; i++)
        {
            Debug.Log($"Level {i}: {GetCostAtLevel(i):N0} Gold, Value: {FormatValue(GetValueAtLevel(i))}");
        }
        Debug.Log($"Total Cost to Max: {GetTotalCostToLevel(MaxLevel):N0} Gold");
        Debug.Log($"Total Value at Max: {FormatValue(GetTotalValueAtLevel(MaxLevel))}");
    }

    [ContextMenu("Generate Balanced Costs")]
    private void GenerateBalancedCosts()
    {
        useManualCosts = true;
        manualCostPerLevel = new int[maxLevel];
        for (int i = 0; i < maxLevel; i++)
        {
            manualCostPerLevel[i] = Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, i));
        }
        Debug.Log($"Generated {maxLevel} balanced costs starting at {baseCost}");
    }

    [ContextMenu("Generate Linear Values")]
    private void GenerateLinearValues()
    {
        useManualValues = true;
        manualValuePerLevel = new float[maxLevel];
        for (int i = 0; i < maxLevel; i++)
        {
            manualValuePerLevel[i] = valuePerLevel * (i + 1);
        }
        Debug.Log($"Generated {maxLevel} linear values: {valuePerLevel} per level");
    }
#endif
    #endregion
}

/// <summary>
/// 업그레이드 희귀도
/// </summary>
public enum UpgradeRarity
{
    Common,     // 일반 (흰색)
    Uncommon,   // 고급 (녹색)
    Rare,       // 희귀 (파란색)
    Epic,       // 영웅 (보라색)
    Legendary   // 전설 (주황색)
}

/// <summary>
/// 업그레이드 카테고리
/// </summary>
public enum UpgradeCategory
{
    Offensive,  // 공격 관련
    Defensive,  // 방어 관련
    Utility,    // 유틸리티 (마그넷, 속도 등)
    Squad,      // 분대 관련
    Resource    // 자원 획득 관련
}
