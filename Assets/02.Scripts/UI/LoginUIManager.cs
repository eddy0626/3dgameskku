using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

namespace SquadSurvival.UI
{
    [System.Serializable]
    public class UserData
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    public class UserDatabase
    {
        public List<UserData> users = new List<UserData>();
    }

    public class LoginUIManager : MonoBehaviour
    {
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField idInputField;
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private TMP_InputField passwordConfirmInputField;

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI notificationText;

        [Header("Buttons - Login Mode")]
        [SerializeField] private Button registerButton;
        [SerializeField] private Button loginButton;

        [Header("Buttons - Signup Mode")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button signupButton;

        [Header("Rows")]
        [SerializeField] private GameObject passwordConfirmRow;

        [Header("Scene Settings")]
        [SerializeField] private string gameSceneName = "SampleScene";

        private const string USER_DATA_KEY = "UserDatabase";
        private UserDatabase userDatabase;
        private bool isSignupMode;

        private void Awake()
        {
            LoadUserDatabase();
            AutoBindReferences();
            SetupButtonListeners();
            SetLoginMode();
        }

        private void LoadUserDatabase()
        {
            string json = PlayerPrefs.GetString(USER_DATA_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                userDatabase = new UserDatabase();
            }
            else
            {
                userDatabase = JsonUtility.FromJson<UserDatabase>(json);
                if (userDatabase == null)
                    userDatabase = new UserDatabase();
            }
        }

        private void SaveUserDatabase()
        {
            string json = JsonUtility.ToJson(userDatabase);
            PlayerPrefs.SetString(USER_DATA_KEY, json);
            PlayerPrefs.Save();
        }

        private bool UserExists(string username)
        {
            foreach (var user in userDatabase.users)
            {
                if (user.username == username)
                    return true;
            }
            return false;
        }

        private bool ValidateLogin(string username, string password)
        {
            foreach (var user in userDatabase.users)
            {
                if (user.username == username && user.password == password)
                    return true;
            }
            return false;
        }

        private void RegisterUser(string username, string password)
        {
            var newUser = new UserData
            {
                username = username,
                password = password
            };
            userDatabase.users.Add(newUser);
            SaveUserDatabase();
        }

        private void AutoBindReferences()
        {
            var canvas = transform;

            if (titleText == null)
                titleText = canvas.Find("Title_Text")?.GetComponent<TextMeshProUGUI>();
            if (notificationText == null)
                notificationText = canvas.Find("Notification_Text")?.GetComponent<TextMeshProUGUI>();

            var loginForm = canvas.Find("LoginForm_Panel");
            if (loginForm != null)
            {
                if (idInputField == null)
                    idInputField = loginForm.Find("ID_Row/ID_InputField")?.GetComponent<TMP_InputField>();
                if (passwordInputField == null)
                    passwordInputField = loginForm.Find("Password_Row/Password_InputField")?.GetComponent<TMP_InputField>();
                if (passwordConfirmRow == null)
                    passwordConfirmRow = loginForm.Find("PasswordConfirm_Row")?.gameObject;
                if (passwordConfirmInputField == null && passwordConfirmRow != null)
                    passwordConfirmInputField = passwordConfirmRow.transform.Find("PasswordConfirm_InputField")?.GetComponent<TMP_InputField>();
            }

            var buttonGroup = canvas.Find("Button_Group");
            if (buttonGroup != null)
            {
                if (registerButton == null)
                    registerButton = buttonGroup.Find("Button_Register")?.GetComponent<Button>();
                if (loginButton == null)
                    loginButton = buttonGroup.Find("Button_Login")?.GetComponent<Button>();
                if (backButton == null)
                    backButton = buttonGroup.Find("Button_Back")?.GetComponent<Button>();
                if (signupButton == null)
                    signupButton = buttonGroup.Find("Button_Signup")?.GetComponent<Button>();
            }
        }

        private void SetupButtonListeners()
        {
            if (registerButton != null)
                registerButton.onClick.AddListener(OnRegisterButtonClick);
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginButtonClick);
            if (backButton != null)
                backButton.onClick.AddListener(OnBackButtonClick);
            if (signupButton != null)
                signupButton.onClick.AddListener(OnSignupButtonClick);
        }

        public string GetUsername()
        {
            return idInputField != null ? idInputField.text : string.Empty;
        }

        public string GetPassword()
        {
            return passwordInputField != null ? passwordInputField.text : string.Empty;
        }

        public string GetPasswordConfirm()
        {
            return passwordConfirmInputField != null ? passwordConfirmInputField.text : string.Empty;
        }

        public void SetNotification(string message, Color? color = null)
        {
            if (notificationText != null)
            {
                notificationText.text = message;
                if (color.HasValue)
                    notificationText.color = color.Value;
            }
        }

        public void ClearNotification()
        {
            SetNotification(string.Empty);
        }

        public void SetLoginMode()
        {
            isSignupMode = false;

            // Show login mode UI
            if (passwordConfirmRow != null)
                passwordConfirmRow.SetActive(false);
            if (registerButton != null)
                registerButton.gameObject.SetActive(true);
            if (loginButton != null)
                loginButton.gameObject.SetActive(true);

            // Hide signup mode UI
            if (backButton != null)
                backButton.gameObject.SetActive(false);
            if (signupButton != null)
                signupButton.gameObject.SetActive(false);

            ClearInputFields();
            ClearNotification();
        }

        public void SetSignupMode()
        {
            isSignupMode = true;

            // Show signup mode UI
            if (passwordConfirmRow != null)
                passwordConfirmRow.SetActive(true);
            if (backButton != null)
                backButton.gameObject.SetActive(true);
            if (signupButton != null)
                signupButton.gameObject.SetActive(true);

            // Hide login mode UI
            if (registerButton != null)
                registerButton.gameObject.SetActive(false);
            if (loginButton != null)
                loginButton.gameObject.SetActive(false);

            ClearInputFields();
            ClearNotification();
        }

        private void ClearInputFields()
        {
            if (idInputField != null)
                idInputField.text = string.Empty;
            if (passwordInputField != null)
                passwordInputField.text = string.Empty;
            if (passwordConfirmInputField != null)
                passwordConfirmInputField.text = string.Empty;
        }

        public void OnLoginButtonClick()
        {
            string username = GetUsername();
            string password = GetPassword();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                SetNotification("Please enter username and password.", Color.red);
                return;
            }

            if (ValidateLogin(username, password))
            {
                Debug.Log($"Login successful - Username: {username}");
                SetNotification("Login successful!", Color.green);

                // Save current user
                PlayerPrefs.SetString("CurrentUser", username);
                PlayerPrefs.Save();

                // Load game scene
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                Debug.Log($"Login failed - Username: {username}");
                SetNotification("Invalid username or password.", Color.red);
            }
        }

        public void OnRegisterButtonClick()
        {
            SetSignupMode();
        }

        public void OnSignupButtonClick()
        {
            string username = GetUsername();
            string password = GetPassword();
            string passwordConfirm = GetPasswordConfirm();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                SetNotification("Please enter username and password.", Color.red);
                return;
            }

            if (username.Length < 3)
            {
                SetNotification("Username must be at least 3 characters.", Color.red);
                return;
            }

            if (password.Length < 4)
            {
                SetNotification("Password must be at least 4 characters.", Color.red);
                return;
            }

            if (password != passwordConfirm)
            {
                SetNotification("Passwords do not match.", Color.red);
                return;
            }

            if (UserExists(username))
            {
                SetNotification("Username already exists.", Color.red);
                return;
            }

            // Register new user
            RegisterUser(username, password);
            Debug.Log($"Signup successful - Username: {username}");
            SetNotification("Account created successfully!", Color.green);

            // Switch back to login mode after short delay
            Invoke(nameof(SetLoginMode), 1.5f);
        }

        public void OnBackButtonClick()
        {
            SetLoginMode();
        }

        private void OnDestroy()
        {
            if (registerButton != null)
                registerButton.onClick.RemoveListener(OnRegisterButtonClick);
            if (loginButton != null)
                loginButton.onClick.RemoveListener(OnLoginButtonClick);
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackButtonClick);
            if (signupButton != null)
                signupButton.onClick.RemoveListener(OnSignupButtonClick);
        }
    }
}
