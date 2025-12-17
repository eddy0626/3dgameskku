using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 게임 저장/로드 통합 관리 시스템
/// - 싱글톤 패턴
/// - ResourceManager, UpgradeManager, WaveManager 데이터 통합
/// - PlayerPrefs 기반 영구 저장
/// - 자동 저장 기능
/// </summary>
public class GameSaveManager : MonoBehaviour
{
    #region Singleton
    public static GameSaveManager Instance { get; private set; }
    #endregion

    #region Inspector Fields
    [Header("저장 설정")]
    [Tooltip("저장 슬롯 키 접두사")]
    [SerializeField] private string saveKeyPrefix = "GameSave_";

    [Tooltip("현재 사용 중인 저장 슬롯")]
    [SerializeField] private int currentSlot = 0;

    [Tooltip("최대 저장 슬롯 수")]
    [SerializeField] private int maxSaveSlots = 3;

    [Header("자동 저장")]
    [Tooltip("자동 저장 활성화")]
    [SerializeField] private bool autoSaveEnabled = true;

    [Tooltip("자동 저장 간격 (초)")]
    [Range(30f, 300f)]
    [SerializeField] private float autoSaveInterval = 60f;

    [Tooltip("웨이브 완료 시 자동 저장")]
    [SerializeField] private bool saveOnWaveComplete = true;

    [Tooltip("게임 종료 시 자동 저장")]
    [SerializeField] private bool saveOnApplicationQuit = true;

    [Header("설정")]
    [Tooltip("씬 전환 시 유지")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("디버그")]
    [SerializeField] private bool logSaveEvents = true;
    #endregion

    #region Private Fields
    private Coroutine _autoSaveCoroutine;
    private bool _isSaving;
    private bool _isLoading;
    private float _lastSaveTime;
    #endregion

    #region Events
    /// <summary>저장 시작</summary>
    public event Action OnSaveStart;

    /// <summary>저장 완료 (성공 여부)</summary>
    public event Action<bool> OnSaveComplete;

    /// <summary>로드 시작</summary>
    public event Action OnLoadStart;

    /// <summary>로드 완료 (성공 여부)</summary>
    public event Action<bool> OnLoadComplete;

    /// <summary>자동 저장 발생</summary>
    public event Action OnAutoSave;

    /// <summary>저장 슬롯 변경</summary>
    public event Action<int> OnSlotChanged;
    #endregion

    #region Properties
    public int CurrentSlot => currentSlot;
    public int MaxSaveSlots => maxSaveSlots;
    public bool IsSaving => _isSaving;
    public bool IsLoading => _isLoading;
    public bool AutoSaveEnabled => autoSaveEnabled;
    public float LastSaveTime => _lastSaveTime;

    /// <summary>현재 슬롯의 저장 키</summary>
    private string CurrentSaveKey => $"{saveKeyPrefix}{currentSlot}";
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
    }

    private void Start()
    {
        // 이벤트 구독
        SubscribeToEvents();

        // 자동 저장 시작
        if (autoSaveEnabled)
        {
            StartAutoSave();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            UnsubscribeFromEvents();
            Instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        if (saveOnApplicationQuit)
        {
            SaveGame();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // 모바일에서 앱 일시정지 시 저장
        if (pauseStatus && autoSaveEnabled)
        {
            SaveGame();
        }
    }
    #endregion

    #region Event Subscription
    private void SubscribeToEvents()
    {
        // WaveManager 이벤트
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveComplete += HandleWaveComplete;
            WaveManager.Instance.OnGameEnd += HandleGameEnd;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveComplete -= HandleWaveComplete;
            WaveManager.Instance.OnGameEnd -= HandleGameEnd;
        }
    }

    private void HandleWaveComplete(int waveNumber)
    {
        if (saveOnWaveComplete)
        {
            SaveGame();
            LogDebug($"웨이브 {waveNumber} 완료 - 자동 저장");
        }
    }

    private void HandleGameEnd(bool victory)
    {
        SaveGame();
        LogDebug($"게임 종료 - 저장 (승리: {victory})");
    }
    #endregion

    #region Auto Save
    /// <summary>
    /// 자동 저장 시작
    /// </summary>
    public void StartAutoSave()
    {
        if (_autoSaveCoroutine != null)
        {
            StopCoroutine(_autoSaveCoroutine);
        }

        _autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
        LogDebug($"자동 저장 시작 (간격: {autoSaveInterval}초)");
    }

    /// <summary>
    /// 자동 저장 중지
    /// </summary>
    public void StopAutoSave()
    {
        if (_autoSaveCoroutine != null)
        {
            StopCoroutine(_autoSaveCoroutine);
            _autoSaveCoroutine = null;
        }

        LogDebug("자동 저장 중지");
    }

    /// <summary>
    /// 자동 저장 활성화/비활성화
    /// </summary>
    public void SetAutoSaveEnabled(bool enabled)
    {
        autoSaveEnabled = enabled;

        if (enabled)
        {
            StartAutoSave();
        }
        else
        {
            StopAutoSave();
        }
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (autoSaveEnabled)
        {
            yield return new WaitForSeconds(autoSaveInterval);

            // 게임이 진행 중일 때만 자동 저장
            if (WaveManager.Instance != null && WaveManager.Instance.IsGameStarted)
            {
                SaveGame();
                OnAutoSave?.Invoke();
                LogDebug("자동 저장 실행");
            }
        }
    }
    #endregion

    #region Save
    /// <summary>
    /// 현재 슬롯에 게임 저장
    /// </summary>
    public bool SaveGame()
    {
        return SaveGame(currentSlot);
    }

    /// <summary>
    /// 지정된 슬롯에 게임 저장
    /// </summary>
    public bool SaveGame(int slot)
    {
        if (_isSaving)
        {
            LogDebug("이미 저장 중입니다!", true);
            return false;
        }

        if (slot < 0 || slot >= maxSaveSlots)
        {
            LogDebug($"잘못된 저장 슬롯: {slot}", true);
            return false;
        }

        _isSaving = true;
        OnSaveStart?.Invoke();

        try
        {
            // 통합 저장 데이터 생성
            GameSaveData saveData = CreateSaveData();

            // JSON 변환
            string json = JsonUtility.ToJson(saveData, true);

            // PlayerPrefs에 저장
            string key = $"{saveKeyPrefix}{slot}";
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();

            _lastSaveTime = Time.time;
            _isSaving = false;

            LogDebug($"게임 저장 완료 (슬롯 {slot})");
            OnSaveComplete?.Invoke(true);

            return true;
        }
        catch (Exception e)
        {
            LogDebug($"저장 실패: {e.Message}", true);
            _isSaving = false;
            OnSaveComplete?.Invoke(false);
            return false;
        }
    }

    /// <summary>
    /// 저장 데이터 생성
    /// </summary>
    private GameSaveData CreateSaveData()
    {
        var saveData = new GameSaveData
        {
            saveVersion = 1,
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            playTime = WaveManager.Instance?.TotalPlayTime ?? 0f
        };

        // ResourceManager 데이터
        if (ResourceManager.Instance != null)
        {
            saveData.resourceData = ResourceManager.Instance.GetSaveData();
        }

        // UpgradeManager 데이터
        if (UpgradeManager.Instance != null)
        {
            saveData.upgradeData = UpgradeManager.Instance.GetSaveJson();
        }

        // WaveManager 데이터
        if (WaveManager.Instance != null)
        {
            saveData.waveData = WaveManager.Instance.GetSaveJson();
        }

        return saveData;
    }
    #endregion

    #region Load
    /// <summary>
    /// 현재 슬롯에서 게임 로드
    /// </summary>
    public bool LoadGame()
    {
        return LoadGame(currentSlot);
    }

    /// <summary>
    /// 지정된 슬롯에서 게임 로드
    /// </summary>
    public bool LoadGame(int slot)
    {
        if (_isLoading)
        {
            LogDebug("이미 로딩 중입니다!", true);
            return false;
        }

        if (slot < 0 || slot >= maxSaveSlots)
        {
            LogDebug($"잘못된 저장 슬롯: {slot}", true);
            return false;
        }

        string key = $"{saveKeyPrefix}{slot}";

        if (!PlayerPrefs.HasKey(key))
        {
            LogDebug($"저장 데이터 없음 (슬롯 {slot})");
            return false;
        }

        _isLoading = true;
        OnLoadStart?.Invoke();

        try
        {
            // PlayerPrefs에서 로드
            string json = PlayerPrefs.GetString(key);

            // JSON 파싱
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

            // 데이터 적용
            ApplySaveData(saveData);

            currentSlot = slot;
            _isLoading = false;

            LogDebug($"게임 로드 완료 (슬롯 {slot})");
            OnLoadComplete?.Invoke(true);

            return true;
        }
        catch (Exception e)
        {
            LogDebug($"로드 실패: {e.Message}", true);
            _isLoading = false;
            OnLoadComplete?.Invoke(false);
            return false;
        }
    }

    /// <summary>
    /// 저장 데이터 적용
    /// </summary>
    private void ApplySaveData(GameSaveData saveData)
    {
        // ResourceManager 데이터 적용
        if (ResourceManager.Instance != null && !string.IsNullOrEmpty(saveData.resourceData))
        {
            ResourceManager.Instance.LoadSaveData(saveData.resourceData);
        }

        // UpgradeManager 데이터 적용
        if (UpgradeManager.Instance != null && !string.IsNullOrEmpty(saveData.upgradeData))
        {
            UpgradeManager.Instance.LoadFromJson(saveData.upgradeData);
        }

        // WaveManager 데이터 적용
        if (WaveManager.Instance != null && !string.IsNullOrEmpty(saveData.waveData))
        {
            WaveManager.Instance.LoadFromJson(saveData.waveData);
        }
    }
    #endregion

    #region Slot Management
    /// <summary>
    /// 저장 슬롯 변경
    /// </summary>
    public void SetCurrentSlot(int slot)
    {
        if (slot < 0 || slot >= maxSaveSlots)
        {
            LogDebug($"잘못된 슬롯 번호: {slot}", true);
            return;
        }

        currentSlot = slot;
        OnSlotChanged?.Invoke(slot);
        LogDebug($"저장 슬롯 변경: {slot}");
    }

    /// <summary>
    /// 슬롯에 저장 데이터 존재 여부 확인
    /// </summary>
    public bool HasSaveData(int slot)
    {
        if (slot < 0 || slot >= maxSaveSlots) return false;

        string key = $"{saveKeyPrefix}{slot}";
        return PlayerPrefs.HasKey(key);
    }

    /// <summary>
    /// 슬롯 정보 조회
    /// </summary>
    public SaveSlotInfo GetSlotInfo(int slot)
    {
        if (slot < 0 || slot >= maxSaveSlots) return null;

        string key = $"{saveKeyPrefix}{slot}";

        if (!PlayerPrefs.HasKey(key))
        {
            return new SaveSlotInfo
            {
                slotIndex = slot,
                isEmpty = true
            };
        }

        try
        {
            string json = PlayerPrefs.GetString(key);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

            // WaveData에서 웨이브 정보 추출
            int waveNumber = 1;
            if (!string.IsNullOrEmpty(saveData.waveData))
            {
                var waveData = JsonUtility.FromJson<WaveSaveData>(saveData.waveData);
                waveNumber = waveData.currentWaveIndex + 1;
            }

            return new SaveSlotInfo
            {
                slotIndex = slot,
                isEmpty = false,
                saveTime = saveData.saveTime,
                playTime = saveData.playTime,
                waveNumber = waveNumber
            };
        }
        catch
        {
            return new SaveSlotInfo
            {
                slotIndex = slot,
                isEmpty = true
            };
        }
    }

    /// <summary>
    /// 모든 슬롯 정보 조회
    /// </summary>
    public SaveSlotInfo[] GetAllSlotInfo()
    {
        SaveSlotInfo[] slots = new SaveSlotInfo[maxSaveSlots];

        for (int i = 0; i < maxSaveSlots; i++)
        {
            slots[i] = GetSlotInfo(i);
        }

        return slots;
    }
    #endregion

    #region Delete
    /// <summary>
    /// 저장 데이터 삭제
    /// </summary>
    public bool DeleteSave(int slot)
    {
        if (slot < 0 || slot >= maxSaveSlots)
        {
            LogDebug($"잘못된 슬롯 번호: {slot}", true);
            return false;
        }

        string key = $"{saveKeyPrefix}{slot}";

        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            LogDebug($"저장 데이터 삭제 (슬롯 {slot})");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 모든 저장 데이터 삭제
    /// </summary>
    public void DeleteAllSaves()
    {
        for (int i = 0; i < maxSaveSlots; i++)
        {
            string key = $"{saveKeyPrefix}{i}";
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        PlayerPrefs.Save();
        LogDebug("모든 저장 데이터 삭제");
    }
    #endregion

    #region New Game
    /// <summary>
    /// 새 게임 시작 (현재 진행 초기화)
    /// </summary>
    public void StartNewGame()
    {
        // 모든 매니저 초기화
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResetAllResources();
        }

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.ResetAllUpgrades();
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.ResetGame();
        }

        LogDebug("새 게임 시작");
    }
    #endregion

    #region Utility
    /// <summary>
    /// 저장 데이터 내보내기 (JSON 문자열)
    /// </summary>
    public string ExportSaveData(int slot)
    {
        if (!HasSaveData(slot)) return null;

        string key = $"{saveKeyPrefix}{slot}";
        return PlayerPrefs.GetString(key);
    }

    /// <summary>
    /// 저장 데이터 가져오기 (JSON 문자열)
    /// </summary>
    public bool ImportSaveData(int slot, string json)
    {
        if (slot < 0 || slot >= maxSaveSlots) return false;
        if (string.IsNullOrEmpty(json)) return false;

        try
        {
            // JSON 유효성 검사
            var testData = JsonUtility.FromJson<GameSaveData>(json);
            if (testData == null) return false;

            string key = $"{saveKeyPrefix}{slot}";
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();

            LogDebug($"저장 데이터 가져오기 완료 (슬롯 {slot})");
            return true;
        }
        catch (Exception e)
        {
            LogDebug($"저장 데이터 가져오기 실패: {e.Message}", true);
            return false;
        }
    }

    private void LogDebug(string message, bool isWarning = false)
    {
        if (!logSaveEvents) return;

        if (isWarning)
            Debug.LogWarning($"[GameSaveManager] {message}");
        else
            Debug.Log($"[GameSaveManager] {message}");
    }
    #endregion

    #region Debug
#if UNITY_EDITOR
    [ContextMenu("Save Current Game")]
    private void DebugSaveGame() => SaveGame();

    [ContextMenu("Load Current Game")]
    private void DebugLoadGame() => LoadGame();

    [ContextMenu("Delete Current Slot")]
    private void DebugDeleteCurrentSlot() => DeleteSave(currentSlot);

    [ContextMenu("Delete All Saves")]
    private void DebugDeleteAllSaves() => DeleteAllSaves();

    [ContextMenu("Print All Slot Info")]
    private void DebugPrintSlotInfo()
    {
        Debug.Log("=== Save Slots ===");
        var slots = GetAllSlotInfo();
        foreach (var slot in slots)
        {
            if (slot.isEmpty)
            {
                Debug.Log($"Slot {slot.slotIndex}: Empty");
            }
            else
            {
                Debug.Log($"Slot {slot.slotIndex}: Wave {slot.waveNumber}, " +
                          $"Time: {slot.playTime:F1}s, Saved: {slot.saveTime}");
            }
        }
    }

    [ContextMenu("Start New Game")]
    private void DebugStartNewGame() => StartNewGame();

    [ContextMenu("Toggle Auto Save")]
    private void DebugToggleAutoSave()
    {
        SetAutoSaveEnabled(!autoSaveEnabled);
        Debug.Log($"Auto Save: {autoSaveEnabled}");
    }
#endif
    #endregion
}

#region Data Classes
/// <summary>
/// 통합 게임 저장 데이터
/// </summary>
[Serializable]
public class GameSaveData
{
    public int saveVersion;
    public string saveTime;
    public float playTime;

    // 각 매니저별 JSON 데이터
    public string resourceData;
    public string upgradeData;
    public string waveData;
}

/// <summary>
/// 저장 슬롯 정보
/// </summary>
[Serializable]
public class SaveSlotInfo
{
    public int slotIndex;
    public bool isEmpty;
    public string saveTime;
    public float playTime;
    public int waveNumber;

    /// <summary>
    /// 플레이 시간 포맷팅
    /// </summary>
    public string FormattedPlayTime
    {
        get
        {
            int hours = (int)(playTime / 3600);
            int minutes = (int)((playTime % 3600) / 60);
            int seconds = (int)(playTime % 60);

            if (hours > 0)
                return $"{hours}:{minutes:D2}:{seconds:D2}";
            else
                return $"{minutes}:{seconds:D2}";
        }
    }
}
#endregion
