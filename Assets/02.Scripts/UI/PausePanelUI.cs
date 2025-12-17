using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일시정지 패널 UI 컨트롤러
/// - Resume, Settings, Quit 버튼 이벤트 연결
/// - UIManager와 연동
/// </summary>
public class PausePanelUI : MonoBehaviour
{
    #region Inspector Fields

    [Header("버튼 참조")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _mainMenuButton;

    #endregion

    #region Unity Callbacks

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoFindButtons();
    }

    private void Reset()
    {
        AutoFindButtons();
    }
#endif

    private void Awake()
    {
        Debug.Log("[PausePanelUI] Awake 호출");
        AutoFindButtons();
        SetupButtonListeners();
    }

    private void OnEnable()
    {
        Debug.Log("[PausePanelUI] OnEnable 호출");
        // 패널 활성화 시 버튼 이벤트 재확인
        SetupButtonListeners();
    }

    #endregion

    #region Initialization

    private void AutoFindButtons()
    {
        if (_resumeButton == null)
        {
            Transform t = transform.Find("ResumeButton");
            if (t != null) _resumeButton = t.GetComponent<Button>();
        }

        if (_settingsButton == null)
        {
            Transform t = transform.Find("SettingsButton");
            if (t != null) _settingsButton = t.GetComponent<Button>();
        }

        if (_quitButton == null)
        {
            Transform t = transform.Find("QuitButton");
            if (t != null) _quitButton = t.GetComponent<Button>();
        }

        if (_mainMenuButton == null)
        {
            Transform t = transform.Find("MainMenuButton");
            if (t != null) _mainMenuButton = t.GetComponent<Button>();
        }
    }

    private void SetupButtonListeners()
    {
        // Resume 버튼
        if (_resumeButton != null)
        {
            _resumeButton.onClick.RemoveAllListeners();
            _resumeButton.onClick.AddListener(OnResumeClicked);
            Debug.Log("[PausePanelUI] ResumeButton 연결됨");
        }
        else
        {
            Debug.LogWarning("[PausePanelUI] ResumeButton을 찾을 수 없음!");
        }

        // Settings 버튼
        if (_settingsButton != null)
        {
            _settingsButton.onClick.RemoveAllListeners();
            _settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        // Quit 버튼
        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveAllListeners();
            _quitButton.onClick.AddListener(OnQuitClicked);
        }

        // Main Menu 버튼
        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.RemoveAllListeners();
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    #endregion

    #region Button Callbacks

    private void OnResumeClicked()
    {
        Debug.Log("[PausePanelUI] Resume 버튼 클릭");

        // 게임 재개
        Time.timeScale = 1f;

        // 패널 직접 비활성화 (UIManager가 패널 참조 없어도 동작)
        gameObject.SetActive(false);

        // UIManager 상태 변경 (패널 비활성화 후 호출)
        if (UIManager.Instance != null)
        {
            // UIManager가 커서 상태도 관리
            UIManager.Instance.CloseAllUI();
        }
        else
        {
            // UIManager 없으면 직접 커서 처리
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Debug.Log("[PausePanelUI] 게임 재개 완료");
    }

    private void OnSettingsClicked()
    {
        Debug.Log("[PausePanelUI] Settings 버튼 클릭");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenUI(UIManager.UIState.Settings);
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("[PausePanelUI] Quit 버튼 클릭");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.QuitGame();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("[PausePanelUI] MainMenu 버튼 클릭");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.GoToMainMenu();
        }
        else
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 외부에서 Resume 호출
    /// </summary>
    public void Resume()
    {
        OnResumeClicked();
    }

    #endregion
}
