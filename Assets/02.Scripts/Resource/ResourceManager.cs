using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 플레이어 자원 관리 시스템
/// - 싱글톤 패턴
/// - 다중 리소스 타입 (Gold, Gem, Scrap, etc.)
/// - UI 연동용 이벤트
/// - 보너스 배율 지원
/// </summary>
public class ResourceManager : MonoBehaviour
{
    #region Singleton
    public static ResourceManager Instance { get; private set; }
    #endregion

    #region Inspector Fields
    [Header("시작 자원")]
    [SerializeField] private int startingGold = 0;
    [SerializeField] private int startingGems = 0;
    [SerializeField] private int startingScrap = 0;

    [Header("자원 데이터")]
    [SerializeField] private List<ResourceData> resourceDataList;

    [Header("설정")]
    [Tooltip("씬 전환 시 유지")]
    [SerializeField] private bool dontDestroyOnLoad = false;

    [Tooltip("최대 자원 제한 (0 = 무제한)")]
    [SerializeField] private int maxGold = 0;
    [SerializeField] private int maxGems = 0;

    [Header("디버그")]
    [SerializeField] private bool logResourceChanges = true;
    #endregion

    #region Private Fields
    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
    private Dictionary<ResourceType, ResourceData> resourceDataMap = new Dictionary<ResourceType, ResourceData>();

    // 보너스 배율
    private float coinMultiplier = 1f;
    private float gemMultiplier = 1f;
    private float expMultiplier = 1f;
    #endregion

    #region Events - General
    /// <summary>리소스 변경 시 (타입, 새 값, 변화량)</summary>
    public event Action<ResourceType, int, int> OnResourceChanged;
    /// <summary>리소스 추가 시</summary>
    public event Action<ResourceType, int> OnResourceAdded;
    /// <summary>리소스 사용 시</summary>
    public event Action<ResourceType, int> OnResourceSpent;
    /// <summary>리소스 부족 시</summary>
    public event Action<ResourceType, int> OnResourceInsufficient;
    #endregion

    #region Events - Coin (UI 연동용)
    /// <summary>코인 변경 시 (새 값)</summary>
    public event Action<int> OnCoinsChanged;
    /// <summary>코인 추가 시 (추가량)</summary>
    public event Action<int> OnCoinsAdded;
    /// <summary>코인 사용 시 (사용량)</summary>
    public event Action<int> OnCoinsSpent;
    #endregion

    #region Events - Gem (UI 연동용)
    /// <summary>젬 변경 시 (새 값)</summary>
    public event Action<int> OnGemsChanged;
    /// <summary>젬 추가 시 (추가량)</summary>
    public event Action<int> OnGemsAdded;
    /// <summary>젬 사용 시 (사용량)</summary>
    public event Action<int> OnGemsSpent;
    #endregion

    #region Properties - Shortcuts
    public int Coins => GetResource(ResourceType.Gold);
    public int Gold => GetResource(ResourceType.Gold);
    public int Gems => GetResource(ResourceType.Gem);
    public int Scrap => GetResource(ResourceType.Scrap);
    public int Experience => GetResource(ResourceType.Experience);

    public float CoinMultiplier => coinMultiplier;
    public float GemMultiplier => gemMultiplier;
    public float ExpMultiplier => expMultiplier;
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

        InitializeResources();
    }

    private void Start()
    {
        // 초기 자원 설정
        SetResource(ResourceType.Gold, startingGold);
        SetResource(ResourceType.Gem, startingGems);
        SetResource(ResourceType.Scrap, startingScrap);

        LogDebug("ResourceManager 초기화 완료");
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
    private void InitializeResources()
    {
        // 모든 리소스 타입 초기화
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        {
            resources[type] = 0;
        }

        // ResourceData 매핑
        if (resourceDataList != null)
        {
            foreach (var data in resourceDataList)
            {
                if (data != null)
                {
                    resourceDataMap[data.type] = data;
                }
            }
        }
    }

    /// <summary>
    /// 모든 자원 초기화 (게임 리셋용)
    /// </summary>
    public void ResetAllResources()
    {
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        {
            SetResource(type, 0);
        }

        SetResource(ResourceType.Gold, startingGold);
        SetResource(ResourceType.Gem, startingGems);
        SetResource(ResourceType.Scrap, startingScrap);

        LogDebug("모든 자원 초기화됨");
    }
    #endregion

    #region Resource Operations - General
    /// <summary>
    /// 리소스 추가
    /// </summary>
    public void AddResource(ResourceType type, int amount)
    {
        if (amount <= 0) return;

        // 배율 적용
        int finalAmount = ApplyMultiplier(type, amount);

        int oldAmount = resources[type];
        resources[type] += finalAmount;

        // 최대치 제한
        ApplyMaxLimit(type);

        int actualAdded = resources[type] - oldAmount;

        LogDebug($"+{actualAdded} {type} (Total: {resources[type]})");

        OnResourceAdded?.Invoke(type, actualAdded);
        OnResourceChanged?.Invoke(type, resources[type], actualAdded);
    }

    /// <summary>
    /// 리소스 사용
    /// </summary>
    public bool SpendResource(ResourceType type, int amount)
    {
        if (amount <= 0) return true;

        if (resources[type] < amount)
        {
            LogDebug($"자원 부족! {type}: 보유 {resources[type]}, 필요 {amount}", true);
            OnResourceInsufficient?.Invoke(type, amount - resources[type]);
            return false;
        }

        resources[type] -= amount;

        LogDebug($"-{amount} {type} (Total: {resources[type]})");

        OnResourceSpent?.Invoke(type, amount);
        OnResourceChanged?.Invoke(type, resources[type], -amount);

        return true;
    }

    /// <summary>
    /// 리소스 사용 시도
    /// </summary>
    public bool TrySpendResource(ResourceType type, int amount)
    {
        return SpendResource(type, amount);
    }

    /// <summary>
    /// 리소스 설정
    /// </summary>
    public void SetResource(ResourceType type, int amount)
    {
        int oldAmount = resources[type];
        resources[type] = Mathf.Max(0, amount);

        ApplyMaxLimit(type);

        int delta = resources[type] - oldAmount;
        if (delta != 0)
        {
            OnResourceChanged?.Invoke(type, resources[type], delta);

            // 코인/젬 전용 이벤트
            if (type == ResourceType.Gold)
                OnCoinsChanged?.Invoke(resources[type]);
            else if (type == ResourceType.Gem)
                OnGemsChanged?.Invoke(resources[type]);
        }
    }

    /// <summary>
    /// 리소스 조회
    /// </summary>
    public int GetResource(ResourceType type)
    {
        return resources.TryGetValue(type, out int amount) ? amount : 0;
    }

    /// <summary>
    /// 리소스 보유 확인
    /// </summary>
    public bool HasResource(ResourceType type, int amount)
    {
        return GetResource(type) >= amount;
    }

    /// <summary>
    /// 여러 리소스 동시 확인
    /// </summary>
    public bool HasResources(Dictionary<ResourceType, int> required)
    {
        foreach (var kvp in required)
        {
            if (!HasResource(kvp.Key, kvp.Value))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 여러 리소스 동시 사용
    /// </summary>
    public bool SpendResources(Dictionary<ResourceType, int> costs)
    {
        // 먼저 모든 자원이 충분한지 확인
        if (!HasResources(costs))
            return false;

        // 모두 사용
        foreach (var kvp in costs)
        {
            SpendResource(kvp.Key, kvp.Value);
        }
        return true;
    }

    private int ApplyMultiplier(ResourceType type, int amount)
    {
        float multiplier = type switch
        {
            ResourceType.Gold => coinMultiplier,
            ResourceType.Gem => gemMultiplier,
            ResourceType.Experience => expMultiplier,
            _ => 1f
        };

        return Mathf.RoundToInt(amount * multiplier);
    }

    private void ApplyMaxLimit(ResourceType type)
    {
        if (type == ResourceType.Gold && maxGold > 0)
        {
            resources[type] = Mathf.Min(resources[type], maxGold);
        }
        else if (type == ResourceType.Gem && maxGems > 0)
        {
            resources[type] = Mathf.Min(resources[type], maxGems);
        }
    }
    #endregion

    #region Coin Operations (UI 연동)
    /// <summary>코인 추가</summary>
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        int before = Gold;
        AddResource(ResourceType.Gold, amount);
        int added = Gold - before;

        OnCoinsAdded?.Invoke(added);
        OnCoinsChanged?.Invoke(Gold);
    }

    /// <summary>코인 사용</summary>
    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return true;

        if (!HasCoins(amount))
        {
            OnResourceInsufficient?.Invoke(ResourceType.Gold, amount - Gold);
            return false;
        }

        SpendResource(ResourceType.Gold, amount);

        OnCoinsSpent?.Invoke(amount);
        OnCoinsChanged?.Invoke(Gold);

        return true;
    }

    /// <summary>코인 사용 시도</summary>
    public bool TrySpendCoins(int amount) => SpendCoins(amount);

    /// <summary>코인 보유 확인</summary>
    public bool HasCoins(int amount) => HasResource(ResourceType.Gold, amount);

    /// <summary>코인 설정</summary>
    public void SetCoins(int amount)
    {
        SetResource(ResourceType.Gold, amount);
        OnCoinsChanged?.Invoke(Gold);
    }
    #endregion

    #region Gem Operations (UI 연동)
    /// <summary>젬 추가</summary>
    public void AddGems(int amount)
    {
        if (amount <= 0) return;

        int before = Gems;
        AddResource(ResourceType.Gem, amount);
        int added = Gems - before;

        OnGemsAdded?.Invoke(added);
        OnGemsChanged?.Invoke(Gems);
    }

    /// <summary>젬 사용</summary>
    public bool SpendGems(int amount)
    {
        if (amount <= 0) return true;

        if (!HasGems(amount))
        {
            OnResourceInsufficient?.Invoke(ResourceType.Gem, amount - Gems);
            return false;
        }

        SpendResource(ResourceType.Gem, amount);

        OnGemsSpent?.Invoke(amount);
        OnGemsChanged?.Invoke(Gems);

        return true;
    }

    /// <summary>젬 사용 시도</summary>
    public bool TrySpendGems(int amount) => SpendGems(amount);

    /// <summary>젬 보유 확인</summary>
    public bool HasGems(int amount) => HasResource(ResourceType.Gem, amount);

    /// <summary>젬 설정</summary>
    public void SetGems(int amount)
    {
        SetResource(ResourceType.Gem, amount);
        OnGemsChanged?.Invoke(Gems);
    }
    #endregion

    #region Multiplier
    /// <summary>코인 배율 설정</summary>
    public void SetCoinMultiplier(float multiplier)
    {
        coinMultiplier = Mathf.Max(0f, multiplier);
        LogDebug($"코인 배율: {coinMultiplier}x");
    }

    /// <summary>젬 배율 설정</summary>
    public void SetGemMultiplier(float multiplier)
    {
        gemMultiplier = Mathf.Max(0f, multiplier);
        LogDebug($"젬 배율: {gemMultiplier}x");
    }

    /// <summary>경험치 배율 설정</summary>
    public void SetExpMultiplier(float multiplier)
    {
        expMultiplier = Mathf.Max(0f, multiplier);
        LogDebug($"경험치 배율: {expMultiplier}x");
    }

    /// <summary>모든 배율 초기화</summary>
    public void ResetMultipliers()
    {
        coinMultiplier = 1f;
        gemMultiplier = 1f;
        expMultiplier = 1f;
    }
    #endregion

    #region Resource Data
    public ResourceData GetResourceData(ResourceType type)
    {
        return resourceDataMap.TryGetValue(type, out var data) ? data : null;
    }

    public Sprite GetResourceIcon(ResourceType type)
    {
        var data = GetResourceData(type);
        return data != null ? data.icon : null;
    }

    public Color GetResourceColor(ResourceType type)
    {
        return type switch
        {
            ResourceType.Gold => new Color(1f, 0.84f, 0f),      // Gold
            ResourceType.Gem => new Color(0.5f, 0f, 1f),        // Purple
            ResourceType.Scrap => new Color(0.5f, 0.5f, 0.5f),  // Gray
            ResourceType.Experience => new Color(0f, 1f, 0.5f), // Green
            ResourceType.Health => new Color(1f, 0.2f, 0.2f),   // Red
            ResourceType.Ammo => new Color(1f, 0.5f, 0f),       // Orange
            _ => Color.white
        };
    }
    #endregion

    #region Drop Spawning
    public void SpawnDrop(ResourceType type, Vector3 position, int amount = -1)
    {
        var data = GetResourceData(type);
        if (data != null)
        {
            ResourceDrop.Spawn(data, position, amount);
        }
    }

    public void SpawnDropBurst(ResourceType type, Vector3 position, int count, float radius = 1f)
    {
        var data = GetResourceData(type);
        if (data != null)
        {
            ResourceDrop.SpawnWithBurst(data, position, count, radius);
        }
    }

    public void SpawnRandomDrops(Vector3 position, int minGold, int maxGold, float gemChance = 0.1f)
    {
        int goldAmount = UnityEngine.Random.Range(minGold, maxGold + 1);
        if (goldAmount > 0)
        {
            SpawnDrop(ResourceType.Gold, position, goldAmount);
        }

        if (UnityEngine.Random.value < gemChance)
        {
            SpawnDrop(ResourceType.Gem, position, 1);
        }
    }
    #endregion

    #region Save/Load
    public Dictionary<ResourceType, int> GetAllResources()
    {
        return new Dictionary<ResourceType, int>(resources);
    }

    public void LoadResources(Dictionary<ResourceType, int> savedResources)
    {
        foreach (var kvp in savedResources)
        {
            SetResource(kvp.Key, kvp.Value);
        }
        LogDebug("자원 로드 완료");
    }

    /// <summary>
    /// JSON 직렬화용 데이터
    /// </summary>
    public string GetSaveData()
    {
        var saveData = new ResourceSaveData
        {
            gold = Gold,
            gems = Gems,
            scrap = Scrap,
            experience = Experience
        };
        return JsonUtility.ToJson(saveData);
    }

    public void LoadSaveData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        var saveData = JsonUtility.FromJson<ResourceSaveData>(json);
        SetResource(ResourceType.Gold, saveData.gold);
        SetResource(ResourceType.Gem, saveData.gems);
        SetResource(ResourceType.Scrap, saveData.scrap);
        SetResource(ResourceType.Experience, saveData.experience);
    }

    [Serializable]
    private class ResourceSaveData
    {
        public int gold;
        public int gems;
        public int scrap;
        public int experience;
    }
    #endregion

    #region UI Helpers
    /// <summary>
    /// 숫자 포맷팅 (1000 → 1K, 1000000 → 1M)
    /// </summary>
    public static string FormatNumber(int number)
    {
        if (number >= 1000000)
            return $"{number / 1000000f:F1}M";
        if (number >= 1000)
            return $"{number / 1000f:F1}K";
        return number.ToString();
    }

    /// <summary>
    /// 자원 텍스트 (아이콘 포함용)
    /// </summary>
    public string GetResourceText(ResourceType type)
    {
        return $"{GetResource(type):N0}";
    }
    #endregion

    #region Debug
    private void LogDebug(string message, bool isWarning = false)
    {
        if (!logResourceChanges) return;

        if (isWarning)
            Debug.LogWarning($"[ResourceManager] {message}");
        else
            Debug.Log($"[ResourceManager] {message}");
    }

#if UNITY_EDITOR
    [ContextMenu("Add 1000 Coins")]
    private void DebugAdd1000Coins() => AddCoins(1000);

    [ContextMenu("Add 100 Gems")]
    private void DebugAdd100Gems() => AddGems(100);

    [ContextMenu("Print All Resources")]
    private void DebugPrintResources()
    {
        Debug.Log("=== Current Resources ===");
        foreach (var kvp in resources)
        {
            if (kvp.Value > 0)
                Debug.Log($"{kvp.Key}: {kvp.Value}");
        }
    }

    [ContextMenu("Reset All")]
    private void DebugResetAll() => ResetAllResources();
#endif
    #endregion
}
