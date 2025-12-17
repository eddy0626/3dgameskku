using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// UI 통합 관리 시스템
/// - 싱글톤 패턴
/// - GameState에 따른 UI 전환
/// - 시점(FPS/TPS/TopDown)에 따른 Crosshair 관리
/// - UI 패널 Stack 관리
/// </summary>
public class UIManager : MonoBehaviour
{
    #region Singleton
    public static UIManager Instance { get; private set; }
    #endregion

    #region UI State
    /// <summary>
    /// UI 상태
    /// </summary>
    public enum UIState
    {
        Playing,        // 게임 플레이 중
        Paused,         // 일시정지
        Upgrading,      // 업그레이드 선택 중
        GameOver,       // 게임 오버
        Inventory,      // 인벤토리
        Settings        // 설정
    }

    /// <summary>
    /// 카메라 시점 타입
    /// </summary>
    public enum ViewType
    {
        FPS,            // 1인칭
        TPS,            // 3인칭
        TopDown         // 탑뷰
    }
    #endregion

    #region Inspector Fields
    [Header("UI 패널 참조")]
    [SerializeField] private GameObject _playingPanel;
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _upgradePanel;
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private GameObject _inventoryPanel;
    [SerializeField] private GameObject _settingsPanel;

    [Header("Crosshair")]
    [SerializeField] private CrosshairController _crosshairController;
    [SerializeField] private bool _showCrosshairInTopDown = false;

    [Header("현재 시점")]
    [SerializeField] private ViewType _currentViewType = ViewType.TPS;

    [Header("설정")]
    [SerializeField] private bool _pauseGameOnUIOpen = true;
    [SerializeField] private bool _lockCursorWhilePlaying = true;

    [Header("디버그")]
    [SerializeField] private bool _logUIChanges = true;
    #endregion

    #region Private Fields
    private UIState _currentState = UIState.Playing;
    private UIState _previousState = UIState.Playing;
    private Stack<UIState> _uiStack = new Stack<UIState>();
    private Dictionary<UIState, GameObject> _panelMap = new Dictionary<UIState, GameObject>();
    #endregion

    #region Events
    /// <summary>UI 상태 변경 시 (이전, 새 상태)</summary>
    public event Action<UIState, UIState> OnUIStateChanged;

    /// <summary>시점 변경 시</summary>
    public event Action<ViewType> OnViewTypeChanged;

    /// <summary>UI 열림/닫힘</summary>
    public event Action<bool> OnUIToggled;
    #endregion

    #region Properties
    public UIState CurrentState => _currentState;
    public ViewType CurrentViewType => _currentViewType;
    public bool IsUIOpen => _currentState != UIState.Playing;
    public bool IsPaused => _currentState == UIState.Paused;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePanelMap();
    }

    private void Start()
    {
        // CrosshairController 찾기
        if (_crosshairController == null)
        {
            _crosshairController = FindFirstObjectByType<CrosshairController>();
        }

        // GameStateManager 이벤트 구독
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
        }

        // WaveManager 이벤트 구독 (웨이브 완료 시 업그레이드 패널)
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveComplete += OnWaveComplete;
            WaveManager.Instance.OnGameEnd += OnGameEnd;
        }

        // 초기 상태 설정
        SetUIState(UIState.Playing);
        UpdateCrosshairVisibility();

        LogDebug("UIManager 초기화 완료");
    }

    private void Update()
    {
        HandleInput();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        // 이벤트 구독 해제
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveComplete -= OnWaveComplete;
            WaveManager.Instance.OnGameEnd -= OnGameEnd;
        }
    }
    #endregion

    #region Initialization
    private void InitializePanelMap()
    {
        _panelMap.Clear();

        // 패널 자동 찾기
        AutoFindPanels();

        if (_playingPanel != null) _panelMap[UIState.Playing] = _playingPanel;
        if (_pausePanel != null) _panelMap[UIState.Paused] = _pausePanel;
        if (_upgradePanel != null) _panelMap[UIState.Upgrading] = _upgradePanel;
        if (_gameOverPanel != null) _panelMap[UIState.GameOver] = _gameOverPanel;
        if (_inventoryPanel != null) _panelMap[UIState.Inventory] = _inventoryPanel;
        if (_settingsPanel != null) _panelMap[UIState.Settings] = _settingsPanel;

        // PausePanel 초기 비활성화
        if (_pausePanel != null)
        {
            _pausePanel.SetActive(false);
            LogDebug("PausePanel 초기 비활성화");
        }
    }

    /// <summary>
    /// 패널 자동 찾기 (Inspector 미할당 시)
    /// </summary>
    private void AutoFindPanels()
    {
        Transform canvasTransform = transform;

        // PlayingPanel
        if (_playingPanel == null)
        {
            Transform t = canvasTransform.Find("PlayingPanel");
            if (t != null) _playingPanel = t.gameObject;
        }

        // PausePanel
        if (_pausePanel == null)
        {
            Transform t = canvasTransform.Find("PausePanel");
            if (t != null)
            {
                _pausePanel = t.gameObject;

                // PausePanelUI 컴포넌트 자동 추가
                if (_pausePanel.GetComponent<PausePanelUI>() == null)
                {
                    _pausePanel.AddComponent<PausePanelUI>();
                    LogDebug("PausePanelUI 컴포넌트 자동 추가");
                }
            }
        }

        // UpgradePanel
        if (_upgradePanel == null)
        {
            Transform t = canvasTransform.Find("UpgradePanel");
            if (t != null) _upgradePanel = t.gameObject;
        }

        // GameOverPanel
        if (_gameOverPanel == null)
        {
            Transform t = canvasTransform.Find("GameOverPanel");
            if (t != null)
            {
                _gameOverPanel = t.gameObject;
            }
        }

        // InventoryPanel
        if (_inventoryPanel == null)
        {
            Transform t = canvasTransform.Find("InventoryPanel");
            if (t != null) _inventoryPanel = t.gameObject;
        }

        // SettingsPanel
        if (_settingsPanel == null)
        {
            Transform t = canvasTransform.Find("SettingsPanel");
            if (t != null) _settingsPanel = t.gameObject;
        }

        LogDebug($"패널 자동 찾기 완료 - Pause:{_pausePanel != null}, GameOver:{_gameOverPanel != null}");
    }
    #endregion

    #region Input Handling
    private void HandleInput()
    {
        // ESC 키: 일시정지 / 뒤로가기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeKey();
        }

        // I 키: 인벤토리 (Playing 상태에서만)
        if (Input.GetKeyDown(KeyCode.I) && _currentState == UIState.Playing)
        {
            if (_inventoryPanel != null)
            {
                OpenUI(UIState.Inventory);
            }
        }
    }

    private void HandleEscapeKey()
    {
        switch (_currentState)
        {
            case UIState.Playing:
                // 일시정지
                OpenUI(UIState.Paused);
                break;

            case UIState.Paused:
                // 게임 재개
                CloseCurrentUI();
                break;

            case UIState.Upgrading:
                // 업그레이드 중에는 ESC 무시 (선택 필수)
                break;

            case UIState.Inventory:
            case UIState.Settings:
                // 이전 상태로 돌아가기
                CloseCurrentUI();
                break;

            case UIState.GameOver:
                // 게임 오버에서는 ESC 무시
                break;
        }
    }
    #endregion

    #region UI State Control
    /// <summary>
    /// UI 상태 변경
    /// </summary>
    public void SetUIState(UIState newState)
    {
        if (_currentState == newState) return;

        _previousState = _currentState;
        _currentState = newState;

        // 패널 활성화/비활성화
        UpdatePanelVisibility();

        // 게임 일시정지/재개
        UpdateGamePause();

        // 커서 상태 업데이트
        UpdateCursorState();

        // Crosshair 업데이트
        UpdateCrosshairVisibility();

        OnUIStateChanged?.Invoke(_previousState, _currentState);
        OnUIToggled?.Invoke(_currentState != UIState.Playing);

        LogDebug($"UI 상태 변경: {_previousState} → {_currentState}");
    }

    /// <summary>
    /// UI 열기 (스택에 추가)
    /// </summary>
    public void OpenUI(UIState state)
    {
        if (_currentState == state) return;

        _uiStack.Push(_currentState);
        SetUIState(state);
    }

    /// <summary>
    /// 현재 UI 닫기 (스택에서 복원)
    /// </summary>
    public void CloseCurrentUI()
    {
        if (_uiStack.Count > 0)
        {
            UIState previousState = _uiStack.Pop();
            SetUIState(previousState);
        }
        else
        {
            SetUIState(UIState.Playing);
        }
    }

    /// <summary>
    /// 모든 UI 닫고 Playing으로 돌아가기
    /// </summary>
    public void CloseAllUI()
    {
        _uiStack.Clear();
        SetUIState(UIState.Playing);
    }

    /// <summary>
    /// 업그레이드 UI 열기
    /// </summary>
    public void ShowUpgradeUI()
    {
        OpenUI(UIState.Upgrading);
    }

    /// <summary>
    /// 업그레이드 UI 닫기
    /// </summary>
    public void HideUpgradeUI()
    {
        if (_currentState == UIState.Upgrading)
        {
            CloseCurrentUI();
        }
    }

    /// <summary>
    /// 게임 오버 UI 표시
    /// </summary>
    public void ShowGameOverUI()
    {
        _uiStack.Clear();
        SetUIState(UIState.GameOver);
    }
    #endregion

    #region Panel Management
    private void UpdatePanelVisibility()
    {
        foreach (var kvp in _panelMap)
        {
            if (kvp.Value != null)
            {
                bool shouldBeActive = kvp.Key == _currentState;

                // Playing 패널은 항상 보이되, 다른 UI 위에 표시
                if (kvp.Key == UIState.Playing)
                {
                    shouldBeActive = true;
                }

                kvp.Value.SetActive(shouldBeActive);
            }
        }
    }

    private void UpdateGamePause()
    {
        if (!_pauseGameOnUIOpen) return;

        bool shouldPause = _currentState == UIState.Paused ||
                          _currentState == UIState.Upgrading ||
                          _currentState == UIState.Settings;

        Time.timeScale = shouldPause ? 0f : 1f;
    }

    private void UpdateCursorState()
    {
        bool showCursor = _currentState != UIState.Playing;

        if (showCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (_lockCursorWhilePlaying)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    #endregion

    #region Crosshair Management
    /// <summary>
    /// 시점 타입 설정
    /// </summary>
    public void SetViewType(ViewType viewType)
    {
        if (_currentViewType == viewType) return;

        _currentViewType = viewType;
        UpdateCrosshairVisibility();

        OnViewTypeChanged?.Invoke(_currentViewType);
        LogDebug($"시점 변경: {viewType}");
    }

    /// <summary>
    /// Crosshair 가시성 업데이트
    /// </summary>
    private void UpdateCrosshairVisibility()
    {
        if (_crosshairController == null) return;

        bool shouldShow = _currentState == UIState.Playing;

        // 탑뷰에서는 설정에 따라 숨김
        if (_currentViewType == ViewType.TopDown && !_showCrosshairInTopDown)
        {
            shouldShow = false;
        }

        _crosshairController.SetVisible(shouldShow);
    }

    /// <summary>
    /// Crosshair 표시/숨김 (외부 호출용)
    /// </summary>
    public void SetCrosshairVisible(bool visible)
    {
        if (_crosshairController != null)
        {
            _crosshairController.SetVisible(visible);
        }
    }
    #endregion

    #region Event Handlers
    private void OnGameStateChanged(GameStateManager.GameState gameState)
    {
        switch (gameState)
        {
            case GameStateManager.GameState.Ready:
            case GameStateManager.GameState.Go:
                // 준비/시작 중에는 Playing UI
                if (_currentState != UIState.Playing)
                {
                    CloseAllUI();
                }
                break;

            case GameStateManager.GameState.Playing:
                // 게임 플레이 중
                if (_currentState != UIState.Playing && _currentState != UIState.Upgrading)
                {
                    CloseAllUI();
                }
                break;

            case GameStateManager.GameState.GameOver:
                // 게임 오버
                ShowGameOverUI();
                break;
        }
    }

    private void OnWaveComplete(int waveNumber)
    {
        // 웨이브 완료 시 업그레이드 UI 표시 (옵션)
        // ShowUpgradeUI();
    }

    private void OnGameEnd(bool victory)
    {
        if (!victory)
        {
            ShowGameOverUI();
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 특정 패널 활성화/비활성화
    /// </summary>
    public void SetPanelActive(UIState state, bool active)
    {
        if (_panelMap.TryGetValue(state, out var panel) && panel != null)
        {
            panel.SetActive(active);
        }
    }

    /// <summary>
    /// 패널 참조 설정 (런타임)
    /// </summary>
    public void RegisterPanel(UIState state, GameObject panel)
    {
        _panelMap[state] = panel;
    }

    /// <summary>
    /// 게임 일시정지
    /// </summary>
    public void PauseGame()
    {
        if (_currentState == UIState.Playing)
        {
            OpenUI(UIState.Paused);
        }
    }

    /// <summary>
    /// 게임 재개
    /// </summary>
    public void ResumeGame()
    {
        if (_currentState == UIState.Paused)
        {
            CloseCurrentUI();
        }
    }

    /// <summary>
    /// 게임 종료 (앱 종료)
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 메인 메뉴로 이동
    /// </summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    #endregion

    #region Debug
    private void LogDebug(string message)
    {
        if (_logUIChanges)
        {
            Debug.Log($"[UIManager] {message}");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Show Upgrade UI")]
    private void DebugShowUpgradeUI() => ShowUpgradeUI();

    [ContextMenu("Show Game Over UI")]
    private void DebugShowGameOverUI() => ShowGameOverUI();

    [ContextMenu("Toggle Pause")]
    private void DebugTogglePause()
    {
        if (_currentState == UIState.Paused)
            ResumeGame();
        else
            PauseGame();
    }

    [ContextMenu("Set View: FPS")]
    private void DebugSetViewFPS() => SetViewType(ViewType.FPS);

    [ContextMenu("Set View: TPS")]
    private void DebugSetViewTPS() => SetViewType(ViewType.TPS);

    [ContextMenu("Set View: TopDown")]
    private void DebugSetViewTopDown() => SetViewType(ViewType.TopDown);
#endif
    #endregion
}
