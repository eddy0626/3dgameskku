using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 오버 패널 UI 컨트롤러
/// - Restart, MainMenu 버튼 이벤트 연결
/// - 점수/웨이브 표시
/// </summary>
public class GameOverPanelUI : MonoBehaviour
{
    #region Inspector Fields

    [Header("버튼 참조")]
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _mainMenuButton;

    [Header("텍스트 참조")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _waveReachedText;
    [SerializeField] private TextMeshProUGUI _titleText;

    #endregion

    #region Unity Callbacks

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoFindComponents();
    }

    private void Reset()
    {
        AutoFindComponents();
    }
#endif

    private void Awake()
    {
        AutoFindComponents();
        SetupButtonListeners();
    }

    private void OnEnable()
    {
        SetupButtonListeners();
        UpdateDisplay();
    }

    #endregion

    #region Initialization

    private void AutoFindComponents()
    {
        if (_restartButton == null)
        {
            Transform t = transform.Find("RestartButton");
            if (t != null) _restartButton = t.GetComponent<Button>();
        }

        if (_mainMenuButton == null)
        {
            Transform t = transform.Find("MainMenuButton");
            if (t != null) _mainMenuButton = t.GetComponent<Button>();
        }

        if (_scoreText == null)
        {
            Transform t = transform.Find("ScoreText");
            if (t != null) _scoreText = t.GetComponent<TextMeshProUGUI>();
        }

        if (_waveReachedText == null)
        {
            Transform t = transform.Find("WaveReachedText");
            if (t != null) _waveReachedText = t.GetComponent<TextMeshProUGUI>();
        }

        if (_titleText == null)
        {
            Transform t = transform.Find("GameOverTitle");
            if (t != null) _titleText = t.GetComponent<TextMeshProUGUI>();
        }
    }

    private void SetupButtonListeners()
    {
        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.RemoveAllListeners();
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    #endregion

    #region Display

    private void UpdateDisplay()
    {
        // 점수 표시
        if (_scoreText != null)
        {
            int score = 0;
            // ScoreManager나 GameManager에서 점수 가져오기
            // 현재는 ResourceManager의 Gold로 대체
            if (ResourceManager.Instance != null)
            {
                score = ResourceManager.Instance.Gold;
            }
            _scoreText.text = $"Score: {score:N0}";
        }

        // 웨이브 표시
        if (_waveReachedText != null)
        {
            int wave = 1;
            if (WaveManager.Instance != null)
            {
                wave = WaveManager.Instance.CurrentWave;
            }
            _waveReachedText.text = $"Wave Reached: {wave}";
        }
    }

    /// <summary>
    /// 외부에서 결과 설정
    /// </summary>
    public void SetResult(int score, int waveReached, bool isVictory = false)
    {
        if (_scoreText != null)
        {
            _scoreText.text = $"Score: {score:N0}";
        }

        if (_waveReachedText != null)
        {
            _waveReachedText.text = $"Wave Reached: {waveReached}";
        }

        if (_titleText != null)
        {
            _titleText.text = isVictory ? "VICTORY!" : "GAME OVER";
            _titleText.color = isVictory ? Color.yellow : Color.red;
        }
    }

    #endregion

    #region Button Callbacks

    private void OnRestartClicked()
    {
        Debug.Log("[GameOverPanelUI] Restart 버튼 클릭");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.RestartGame();
        }
        else
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("[GameOverPanelUI] MainMenu 버튼 클릭");

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
}
