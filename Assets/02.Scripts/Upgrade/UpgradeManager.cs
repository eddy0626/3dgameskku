using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 업그레이드 관리 시스템
/// - 싱글톤 패턴
/// - 업그레이드 레벨 및 해금 상태 관리
/// - ResourceManager와 연동 (코인 차감)
/// - 플레이어/분대 스탯 적용
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    #region Singleton
    public static UpgradeManager Instance { get; private set; }
    #endregion

    #region Inspector Fields
    [Header("업그레이드 목록")]
    [Tooltip("사용 가능한 모든 업그레이드 데이터")]
    [SerializeField] private List<UpgradeData> allUpgrades = new List<UpgradeData>();

    [Header("참조")]
    [SerializeField] private Transform player;

    [Header("설정")]
    [Tooltip("웨이브당 업그레이드 선택지 수")]
    [SerializeField] private int upgradeChoicesPerWave = 3;

    [Tooltip("씬 전환 시 유지")]
    [SerializeField] private bool dontDestroyOnLoad = false;

    [Header("디버그")]
    [SerializeField] private bool logUpgradeChanges = true;
    #endregion

    #region Private Fields
    // 업그레이드 레벨 (upgradeId → level)
    private Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();

    // 해금 상태 (upgradeId → isUnlocked)
    private Dictionary<string, bool> upgradeUnlocks = new Dictionary<string, bool>();

    // 빠른 조회용 맵 (upgradeId → UpgradeData)
    private Dictionary<string, UpgradeData> upgradeMap = new Dictionary<string, UpgradeData>();

    // 현재 웨이브
    private int currentWave = 1;

    // 플레이어 레벨
    private int playerLevel = 1;
    #endregion

    #region Events
    /// <summary>업그레이드 적용 시 (업그레이드, 새 레벨)</summary>
    public event Action<UpgradeData, int> OnUpgradeApplied;

    /// <summary>업그레이드 해금 시</summary>
    public event Action<UpgradeData> OnUpgradeUnlocked;

    /// <summary>업그레이드 최대 레벨 도달 시</summary>
    public event Action<UpgradeData> OnUpgradeMaxed;

    /// <summary>업그레이드 실패 시 (업그레이드, 실패 사유)</summary>
    public event Action<UpgradeData, UpgradeFailReason> OnUpgradeFailed;

    /// <summary>업그레이드 선택지 준비 시</summary>
    public event Action<List<UpgradeData>> OnUpgradeChoicesReady;

    /// <summary>전체 스탯 변경 시</summary>
    public event Action OnStatsChanged;
    #endregion

    #region Properties
    public IReadOnlyList<UpgradeData> AllUpgrades => allUpgrades;
    public int CurrentWave => currentWave;
    public int PlayerLevel => playerLevel;
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

        InitializeUpgrades();
    }

    private void Start()
    {
        // 플레이어 찾기
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
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

    #region Initialization
    private void InitializeUpgrades()
    {
        upgradeMap.Clear();
        upgradeLevels.Clear();
        upgradeUnlocks.Clear();

        foreach (var upgrade in allUpgrades)
        {
            if (upgrade == null) continue;

            string id = GetUpgradeId(upgrade);
            upgradeMap[id] = upgrade;
            upgradeLevels[id] = upgrade.startLevel;

            // 해금 비용이 0이면 기본 해금
            upgradeUnlocks[id] = upgrade.unlockCost <= 0;
        }

        LogDebug($"UpgradeManager 초기화: {allUpgrades.Count}개 업그레이드 등록");
    }

    private string GetUpgradeId(UpgradeData upgrade)
    {
        // upgradeId가 있으면 사용, 없으면 name 사용
        return string.IsNullOrEmpty(upgrade.upgradeId) ? upgrade.name : upgrade.upgradeId;
    }
    #endregion

    #region Core Upgrade Operations
    /// <summary>
    /// 업그레이드 시도 (비용 차감)
    /// </summary>
    public bool TryUpgrade(UpgradeData upgrade)
    {
        if (upgrade == null)
        {
            LogDebug("업그레이드 데이터가 null입니다!", true);
            return false;
        }

        string id = GetUpgradeId(upgrade);
        int currentLevel = GetUpgradeLevel(upgrade);

        // 최대 레벨 체크
        if (currentLevel >= upgrade.MaxLevel)
        {
            LogDebug($"{upgrade.upgradeName}은 이미 최대 레벨입니다!");
            OnUpgradeFailed?.Invoke(upgrade, UpgradeFailReason.MaxLevel);
            return false;
        }

        // 해금 체크
        if (!IsUnlocked(upgrade))
        {
            LogDebug($"{upgrade.upgradeName}이 해금되지 않았습니다!");
            OnUpgradeFailed?.Invoke(upgrade, UpgradeFailReason.NotUnlocked);
            return false;
        }

        // 선행 조건 체크
        if (!CheckPrerequisites(upgrade))
        {
            LogDebug($"{upgrade.upgradeName}의 선행 조건이 충족되지 않았습니다!");
            OnUpgradeFailed?.Invoke(upgrade, UpgradeFailReason.PrerequisitesNotMet);
            return false;
        }

        // 비용 체크 및 지불
        int cost = GetUpgradeCost(upgrade);

        if (ResourceManager.Instance == null)
        {
            LogDebug("ResourceManager가 없습니다!", true);
            return false;
        }

        if (!ResourceManager.Instance.SpendCoins(cost))
        {
            LogDebug($"{upgrade.upgradeName} 구매 실패: 골드 부족 (필요: {cost}, 보유: {ResourceManager.Instance.Coins})");
            OnUpgradeFailed?.Invoke(upgrade, UpgradeFailReason.NotEnoughGold);
            return false;
        }

        // 레벨 업
        int newLevel = currentLevel + 1;
        upgradeLevels[id] = newLevel;

        // 스탯 적용
        ApplyUpgrade(upgrade, newLevel);

        // 이펙트 재생
        PlayUpgradeEffect(upgrade, newLevel);

        // 사운드 재생
        PlayUpgradeSound(upgrade);

        LogDebug($"{upgrade.upgradeName} 레벨 {newLevel} 업그레이드 완료 (비용: {cost})");

        OnUpgradeApplied?.Invoke(upgrade, newLevel);
        OnStatsChanged?.Invoke();

        // 최대 레벨 도달 체크
        if (newLevel >= upgrade.MaxLevel)
        {
            OnUpgradeMaxed?.Invoke(upgrade);
        }

        return true;
    }

    /// <summary>
    /// 무료 업그레이드 (비용 없이)
    /// </summary>
    public bool TryFreeUpgrade(UpgradeData upgrade)
    {
        if (upgrade == null) return false;

        string id = GetUpgradeId(upgrade);
        int currentLevel = GetUpgradeLevel(upgrade);

        if (currentLevel >= upgrade.MaxLevel) return false;
        if (!IsUnlocked(upgrade)) return false;
        if (!CheckPrerequisites(upgrade)) return false;

        int newLevel = currentLevel + 1;
        upgradeLevels[id] = newLevel;

        ApplyUpgrade(upgrade, newLevel);
        PlayUpgradeEffect(upgrade, newLevel);

        LogDebug($"{upgrade.upgradeName} 무료 업그레이드 → 레벨 {newLevel}");

        OnUpgradeApplied?.Invoke(upgrade, newLevel);
        OnStatsChanged?.Invoke();

        if (newLevel >= upgrade.MaxLevel)
        {
            OnUpgradeMaxed?.Invoke(upgrade);
        }

        return true;
    }

    /// <summary>
    /// 업그레이드 해금 시도
    /// </summary>
    public bool TryUnlock(UpgradeData upgrade)
    {
        if (upgrade == null) return false;

        string id = GetUpgradeId(upgrade);

        // 이미 해금됨
        if (upgradeUnlocks.TryGetValue(id, out bool unlocked) && unlocked)
        {
            return true;
        }

        // 무료 해금
        if (upgrade.unlockCost <= 0)
        {
            upgradeUnlocks[id] = true;
            OnUpgradeUnlocked?.Invoke(upgrade);
            return true;
        }

        // 비용 지불
        if (ResourceManager.Instance == null) return false;

        if (!ResourceManager.Instance.SpendCoins(upgrade.unlockCost))
        {
            LogDebug($"{upgrade.upgradeName} 해금 실패: 골드 부족");
            return false;
        }

        upgradeUnlocks[id] = true;
        LogDebug($"{upgrade.upgradeName} 해금 완료 (비용: {upgrade.unlockCost})");

        // 해금 사운드
        if (upgrade.unlockSound != null)
        {
            AudioSource.PlayClipAtPoint(upgrade.unlockSound, Camera.main.transform.position);
        }

        OnUpgradeUnlocked?.Invoke(upgrade);
        return true;
    }
    #endregion

    #region Upgrade Application
    private void ApplyUpgrade(UpgradeData upgrade, int level)
    {
        float value = upgrade.GetValueAtLevel(level);

        switch (upgrade.target)
        {
            case UpgradeTarget.Player:
                ApplyToPlayer(upgrade, value);
                break;

            case UpgradeTarget.Squad:
                ApplyToSquad(upgrade, value);
                break;

            case UpgradeTarget.All:
                ApplyToPlayer(upgrade, value);
                ApplyToSquad(upgrade, value);
                break;
        }
    }

    private void ApplyToPlayer(UpgradeData upgrade, float value)
    {
        if (player == null) return;

        // IUpgradeable 인터페이스 우선
        if (player.TryGetComponent<IUpgradeable>(out var upgradeable))
        {
            upgradeable.ApplyUpgrade(upgrade.statType, value, upgrade.isMultiplicative);
            return;
        }

        // 개별 컴포넌트 처리
        switch (upgrade.statType)
        {
            case UpgradeType.Health:
                if (player.TryGetComponent<PlayerHealth>(out var health))
                {
                    health.SetMaxHealth(health.MaxHealth + value, false);
                }
                break;

            case UpgradeType.Speed:
                if (player.TryGetComponent<PlayerMove>(out var move))
                {
                    // PlayerMove에 AddBonusSpeed 메서드 필요
                }
                break;

            case UpgradeType.MagnetRange:
                if (player.TryGetComponent<ResourceMagnet>(out var magnet))
                {
                    magnet.AddBonusRange(value);
                }
                break;

            case UpgradeType.Damage:
            case UpgradeType.AttackSpeed:
            case UpgradeType.AttackRange:
            case UpgradeType.CriticalChance:
            case UpgradeType.CriticalDamage:
                // 플레이어 전투 컴포넌트에 적용
                break;
        }
    }

    private void ApplyToSquad(UpgradeData upgrade, float value)
    {
        if (SquadManager.Instance == null) return;

        // 분대 크기 업그레이드
        if (upgrade.statType == UpgradeType.SquadSize)
        {
            SquadManager.Instance.IncreaseMaxSquadSize((int)value);
            return;
        }

        // 모든 분대원에게 적용
        SquadManager.Instance.ApplyUpgradeToAll(upgrade.statType, value);
    }

    private void PlayUpgradeEffect(UpgradeData upgrade, int level)
    {
        if (player == null) return;

        GameObject effectPrefab = level >= upgrade.MaxLevel
            ? upgrade.maxLevelEffect ?? upgrade.upgradeEffect
            : upgrade.upgradeEffect;

        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, player.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
    }

    private void PlayUpgradeSound(UpgradeData upgrade)
    {
        if (upgrade.upgradeSound != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(upgrade.upgradeSound, Camera.main.transform.position);
        }
    }
    #endregion

    #region Query Methods
    /// <summary>
    /// 업그레이드 현재 레벨 조회
    /// </summary>
    public int GetUpgradeLevel(UpgradeData upgrade)
    {
        if (upgrade == null) return 0;

        string id = GetUpgradeId(upgrade);
        return upgradeLevels.TryGetValue(id, out int level) ? level : 0;
    }

    /// <summary>
    /// 다음 레벨 업그레이드 비용 조회
    /// </summary>
    public int GetUpgradeCost(UpgradeData upgrade)
    {
        if (upgrade == null) return int.MaxValue;

        int currentLevel = GetUpgradeLevel(upgrade);
        if (currentLevel >= upgrade.MaxLevel) return int.MaxValue;

        return upgrade.GetCostAtLevel(currentLevel + 1);
    }

    /// <summary>
    /// ID로 업그레이드 조회
    /// </summary>
    public UpgradeData GetUpgradeById(string upgradeId)
    {
        return upgradeMap.TryGetValue(upgradeId, out var upgrade) ? upgrade : null;
    }

    /// <summary>
    /// 해금 여부 확인
    /// </summary>
    public bool IsUnlocked(UpgradeData upgrade)
    {
        if (upgrade == null) return false;

        string id = GetUpgradeId(upgrade);
        return upgradeUnlocks.TryGetValue(id, out bool unlocked) && unlocked;
    }

    /// <summary>
    /// 최대 레벨 여부 확인
    /// </summary>
    public bool IsMaxLevel(UpgradeData upgrade)
    {
        return GetUpgradeLevel(upgrade) >= upgrade.MaxLevel;
    }

    /// <summary>
    /// 구매 가능 여부 확인
    /// </summary>
    public bool CanAfford(UpgradeData upgrade)
    {
        if (upgrade == null) return false;
        if (IsMaxLevel(upgrade)) return false;

        int cost = GetUpgradeCost(upgrade);
        return ResourceManager.Instance != null && ResourceManager.Instance.HasCoins(cost);
    }

    /// <summary>
    /// 업그레이드 가능 여부 확인 (모든 조건)
    /// </summary>
    public bool CanUpgrade(UpgradeData upgrade)
    {
        return upgrade != null
               && !IsMaxLevel(upgrade)
               && IsUnlocked(upgrade)
               && CheckPrerequisites(upgrade)
               && CanAfford(upgrade);
    }

    /// <summary>
    /// 선행 조건 확인
    /// </summary>
    public bool CheckPrerequisites(UpgradeData upgrade)
    {
        if (upgrade == null) return false;

        // 웨이브 요구사항
        if (upgrade.unlockWave > currentWave) return false;

        // 플레이어 레벨 요구사항
        if (upgrade.requiredPlayerLevel > playerLevel) return false;

        // 선행 업그레이드 요구사항 (UpgradeData의 메서드 사용)
        return upgrade.ArePrerequisitesMet(GetUpgradeLevel);
    }
    #endregion

    #region Filtering Methods
    /// <summary>
    /// 사용 가능한 업그레이드 목록
    /// </summary>
    public List<UpgradeData> GetAvailableUpgrades()
    {
        return allUpgrades.Where(u =>
            u != null &&
            !IsMaxLevel(u) &&
            IsUnlocked(u) &&
            CheckPrerequisites(u)
        ).ToList();
    }

    /// <summary>
    /// 타입별 업그레이드 목록
    /// </summary>
    public List<UpgradeData> GetUpgradesByType(UpgradeType type)
    {
        return allUpgrades.Where(u => u != null && u.statType == type).ToList();
    }

    /// <summary>
    /// 대상별 업그레이드 목록
    /// </summary>
    public List<UpgradeData> GetUpgradesByTarget(UpgradeTarget target)
    {
        return allUpgrades.Where(u => u != null && u.target == target).ToList();
    }

    /// <summary>
    /// 희귀도별 업그레이드 목록
    /// </summary>
    public List<UpgradeData> GetUpgradesByRarity(UpgradeRarity rarity)
    {
        return allUpgrades.Where(u => u != null && u.rarity == rarity).ToList();
    }

    /// <summary>
    /// 카테고리별 업그레이드 목록
    /// </summary>
    public List<UpgradeData> GetUpgradesByCategory(UpgradeCategory category)
    {
        return allUpgrades.Where(u => u != null && u.category == category).ToList();
    }

    /// <summary>
    /// 해금 가능한 업그레이드 목록
    /// </summary>
    public List<UpgradeData> GetUnlockableUpgrades()
    {
        return allUpgrades.Where(u =>
            u != null &&
            !IsUnlocked(u) &&
            u.unlockWave <= currentWave &&
            u.requiredPlayerLevel <= playerLevel
        ).ToList();
    }
    #endregion

    #region Wave Integration
    /// <summary>
    /// 현재 웨이브 설정
    /// </summary>
    public void SetCurrentWave(int wave)
    {
        currentWave = Mathf.Max(1, wave);
        LogDebug($"웨이브 {currentWave} 설정");
    }

    /// <summary>
    /// 플레이어 레벨 설정
    /// </summary>
    public void SetPlayerLevel(int level)
    {
        playerLevel = Mathf.Max(1, level);
        LogDebug($"플레이어 레벨 {playerLevel} 설정");
    }

    /// <summary>
    /// 랜덤 업그레이드 선택지 생성 (희귀도 가중치 적용)
    /// </summary>
    public List<UpgradeData> GetRandomUpgradeChoices(int count = -1)
    {
        if (count < 0) count = upgradeChoicesPerWave;

        var available = GetAvailableUpgrades();

        if (available.Count <= count)
        {
            OnUpgradeChoicesReady?.Invoke(available);
            return available;
        }

        // 가중치 기반 랜덤 선택
        var choices = new List<UpgradeData>();
        var pool = new List<UpgradeData>(available);

        while (choices.Count < count && pool.Count > 0)
        {
            float totalWeight = pool.Sum(u => GetRarityWeight(u.rarity));
            float random = UnityEngine.Random.Range(0f, totalWeight);

            float cumulative = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                cumulative += GetRarityWeight(pool[i].rarity);
                if (random <= cumulative)
                {
                    choices.Add(pool[i]);
                    pool.RemoveAt(i);
                    break;
                }
            }
        }

        OnUpgradeChoicesReady?.Invoke(choices);
        return choices;
    }

    private float GetRarityWeight(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Common => 50f,
            UpgradeRarity.Uncommon => 30f,
            UpgradeRarity.Rare => 15f,
            UpgradeRarity.Epic => 4f,
            UpgradeRarity.Legendary => 1f,
            _ => 10f
        };
    }
    #endregion

    #region Total Stats Calculation
    /// <summary>
    /// 특정 타입의 총 보너스 값 (합연산)
    /// </summary>
    public float GetTotalBonusForType(UpgradeType type)
    {
        float total = 0f;

        foreach (var upgrade in allUpgrades)
        {
            if (upgrade == null || upgrade.statType != type) continue;
            if (upgrade.isMultiplicative) continue; // 곱연산 제외

            int level = GetUpgradeLevel(upgrade);
            if (level > 0)
            {
                total += upgrade.GetTotalValueAtLevel(level);
            }
        }

        return total;
    }

    /// <summary>
    /// 특정 타입의 총 배율 (곱연산)
    /// </summary>
    public float GetTotalMultiplierForType(UpgradeType type)
    {
        float multiplier = 1f;

        foreach (var upgrade in allUpgrades)
        {
            if (upgrade == null || upgrade.statType != type) continue;
            if (!upgrade.isMultiplicative) continue; // 합연산 제외

            int level = GetUpgradeLevel(upgrade);
            if (level > 0)
            {
                float value = upgrade.GetTotalValueAtLevel(level);
                if (upgrade.isPercentage)
                {
                    multiplier *= 1f + value / 100f;
                }
                else
                {
                    multiplier *= value;
                }
            }
        }

        return multiplier;
    }

    /// <summary>
    /// 기본값에 모든 업그레이드 적용
    /// </summary>
    public float ApplyAllUpgrades(UpgradeType type, float baseValue)
    {
        float bonus = GetTotalBonusForType(type);
        float multiplier = GetTotalMultiplierForType(type);

        return (baseValue + bonus) * multiplier;
    }
    #endregion

    #region Save/Load
    /// <summary>
    /// 저장용 데이터 생성
    /// </summary>
    public UpgradeSaveData GetSaveData()
    {
        var saveData = new UpgradeSaveData();

        foreach (var kvp in upgradeLevels)
        {
            if (kvp.Value > 0)
            {
                saveData.levels.Add(new UpgradeLevelEntry { id = kvp.Key, level = kvp.Value });
            }
        }

        foreach (var kvp in upgradeUnlocks)
        {
            if (kvp.Value)
            {
                saveData.unlocks.Add(kvp.Key);
            }
        }

        saveData.currentWave = currentWave;
        saveData.playerLevel = playerLevel;

        return saveData;
    }

    /// <summary>
    /// 저장 데이터 로드
    /// </summary>
    public void LoadSaveData(UpgradeSaveData saveData)
    {
        if (saveData == null) return;

        // 레벨 초기화
        foreach (var key in upgradeLevels.Keys.ToList())
        {
            upgradeLevels[key] = 0;
        }

        // 레벨 로드 및 적용
        foreach (var entry in saveData.levels)
        {
            if (upgradeMap.TryGetValue(entry.id, out var upgrade))
            {
                upgradeLevels[entry.id] = 0;

                // 레벨만큼 업그레이드 적용
                for (int i = 1; i <= entry.level; i++)
                {
                    upgradeLevels[entry.id] = i;
                    ApplyUpgrade(upgrade, i);
                }
            }
        }

        // 해금 상태 로드
        foreach (var id in saveData.unlocks)
        {
            if (upgradeUnlocks.ContainsKey(id))
            {
                upgradeUnlocks[id] = true;
            }
        }

        currentWave = saveData.currentWave;
        playerLevel = saveData.playerLevel;

        OnStatsChanged?.Invoke();
        LogDebug("업그레이드 데이터 로드 완료");
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

        var saveData = JsonUtility.FromJson<UpgradeSaveData>(json);
        LoadSaveData(saveData);
    }

    /// <summary>
    /// 모든 업그레이드 초기화
    /// </summary>
    public void ResetAllUpgrades()
    {
        foreach (var key in upgradeLevels.Keys.ToList())
        {
            upgradeLevels[key] = 0;
        }

        // 무료 해금만 유지
        foreach (var upgrade in allUpgrades)
        {
            if (upgrade != null)
            {
                string id = GetUpgradeId(upgrade);
                upgradeUnlocks[id] = upgrade.unlockCost <= 0;

                // 시작 레벨 적용
                if (upgrade.startLevel > 0)
                {
                    upgradeLevels[id] = upgrade.startLevel;
                }
            }
        }

        currentWave = 1;
        playerLevel = 1;

        OnStatsChanged?.Invoke();
        LogDebug("모든 업그레이드 초기화됨");
    }
    #endregion

    #region Utility
    /// <summary>
    /// 업그레이드 추가 (런타임)
    /// </summary>
    public void RegisterUpgrade(UpgradeData upgrade)
    {
        if (upgrade == null) return;
        if (allUpgrades.Contains(upgrade)) return;

        allUpgrades.Add(upgrade);

        string id = GetUpgradeId(upgrade);
        upgradeMap[id] = upgrade;
        upgradeLevels[id] = upgrade.startLevel;
        upgradeUnlocks[id] = upgrade.unlockCost <= 0;

        LogDebug($"업그레이드 등록: {upgrade.upgradeName}");
    }

    /// <summary>
    /// 업그레이드 제거 (런타임)
    /// </summary>
    public void UnregisterUpgrade(UpgradeData upgrade)
    {
        if (upgrade == null) return;

        string id = GetUpgradeId(upgrade);

        allUpgrades.Remove(upgrade);
        upgradeMap.Remove(id);
        upgradeLevels.Remove(id);
        upgradeUnlocks.Remove(id);
    }
    #endregion

    #region Debug
    private void LogDebug(string message, bool isWarning = false)
    {
        if (!logUpgradeChanges) return;

        if (isWarning)
            Debug.LogWarning($"[UpgradeManager] {message}");
        else
            Debug.Log($"[UpgradeManager] {message}");
    }

#if UNITY_EDITOR
    [ContextMenu("Print All Upgrades")]
    private void DebugPrintAllUpgrades()
    {
        Debug.Log("=== All Upgrades ===");
        foreach (var upgrade in allUpgrades)
        {
            if (upgrade == null) continue;

            int level = GetUpgradeLevel(upgrade);
            bool unlocked = IsUnlocked(upgrade);
            int cost = level < upgrade.MaxLevel ? GetUpgradeCost(upgrade) : 0;

            Debug.Log($"{upgrade.upgradeName}: Lv.{level}/{upgrade.MaxLevel} | " +
                      $"Unlocked: {unlocked} | Cost: {cost} | " +
                      $"Category: {upgrade.category} | Rarity: {upgrade.rarity}");
        }
    }

    [ContextMenu("Print Total Stats")]
    private void DebugPrintTotalStats()
    {
        Debug.Log("=== Total Upgrade Stats ===");
        foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
        {
            float bonus = GetTotalBonusForType(type);
            float mult = GetTotalMultiplierForType(type);

            if (bonus != 0 || mult != 1f)
            {
                Debug.Log($"{type}: +{bonus:F1} | x{mult:F2}");
            }
        }
    }

    [ContextMenu("Add 10000 Gold (Debug)")]
    private void DebugAddGold()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddCoins(10000);
        }
    }

    [ContextMenu("Unlock All Upgrades")]
    private void DebugUnlockAll()
    {
        foreach (var key in upgradeUnlocks.Keys.ToList())
        {
            upgradeUnlocks[key] = true;
        }
        Debug.Log("모든 업그레이드 해금됨");
    }

    [ContextMenu("Reset All (Debug)")]
    private void DebugResetAll() => ResetAllUpgrades();
#endif
    #endregion
}

#region Enums & Data Classes
/// <summary>
/// 업그레이드 실패 사유
/// </summary>
public enum UpgradeFailReason
{
    MaxLevel,           // 최대 레벨
    NotEnoughGold,      // 골드 부족
    NotUnlocked,        // 미해금
    PrerequisitesNotMet,// 선행 조건 미충족
    InvalidData         // 데이터 오류
}

/// <summary>
/// 업그레이드 저장 데이터
/// </summary>
[Serializable]
public class UpgradeSaveData
{
    public List<UpgradeLevelEntry> levels = new List<UpgradeLevelEntry>();
    public List<string> unlocks = new List<string>();
    public int currentWave = 1;
    public int playerLevel = 1;
}

[Serializable]
public class UpgradeLevelEntry
{
    public string id;
    public int level;
}
#endregion

