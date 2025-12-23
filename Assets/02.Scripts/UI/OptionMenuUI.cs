using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace SquadSurvival.UI
{
    public class OptionMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        [Header("Mode Display")]
        [SerializeField] private TextMeshProUGUI modeText;
        [SerializeField] private Image progressBarFill;

        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

        private CanvasGroup canvasGroup;
        private bool isOpen;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            AutoBindReferences();

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            Close();
        }

        private void AutoBindReferences()
        {
            if (continueButton == null)
                continueButton = transform.Find("ContinueButton")?.GetComponent<Button>();
            if (restartButton == null)
                restartButton = transform.Find("RestartButton")?.GetComponent<Button>();
            if (quitButton == null)
                quitButton = transform.Find("QuitButton")?.GetComponent<Button>();
            if (modeText == null)
                modeText = transform.Find("ModeContainer/ModeText")?.GetComponent<TextMeshProUGUI>();
            if (progressBarFill == null)
                progressBarFill = transform.Find("ModeContainer/ProgressBarBg/ProgressBarFill")?.GetComponent<Image>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                if (isOpen)
                    Close();
                else
                    Open();
            }
        }

        public void Open()
        {
            isOpen = true;
            gameObject.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // Force TMP text to rebuild meshes
            var tmpTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmpTexts)
            {
                tmp.ForceMeshUpdate();
            }

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            isOpen = false;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnContinueClicked()
        {
            Close();
        }

        private void OnRestartClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void SetModeText(string mode)
        {
            if (modeText != null)
                modeText.text = mode;
        }

        public void SetProgress(float progress)
        {
            if (progressBarFill != null)
            {
                var rt = progressBarFill.rectTransform;
                rt.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            }
        }

        private void OnDestroy()
        {
            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);
            if (restartButton != null)
                restartButton.onClick.RemoveListener(OnRestartClicked);
            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitClicked);
        }
    }
}
