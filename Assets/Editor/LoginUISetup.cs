using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class LoginUISetup : Editor
{
    [MenuItem("Tools/Setup Login UI")]
    public static void SetupLoginUI()
    {
        var canvas = GameObject.Find("Login_Canvas");
        if (canvas == null)
        {
            Debug.LogError("Login_Canvas not found!");
            return;
        }

        // Setup Canvas
        var canvasComp = canvas.GetComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Setup Background - Stretch to fill
        var background = canvas.transform.Find("Background");
        if (background != null)
        {
            var bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Use RawImage for Texture2D
            var bgRawImage = background.GetComponent<RawImage>();
            if (bgRawImage != null)
            {
                bgRawImage.color = Color.white;

                string bgPath = "Assets/04.Images/로그인배경화면.png";
                var bgTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(bgPath);
                if (bgTexture != null)
                {
                    bgRawImage.texture = bgTexture;
                    Debug.Log("Background image loaded successfully!");
                }
                else
                {
                    Debug.LogWarning("Background image not found at: " + bgPath);
                }
            }
        }

        // Setup Title_Text
        var titleText = canvas.transform.Find("Title_Text");
        if (titleText != null)
        {
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -80);
            titleRect.sizeDelta = new Vector2(400, 100);

            var titleTMP = titleText.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "FPS";
            titleTMP.fontSize = 90;
            titleTMP.color = new Color(1f, 0.27f, 0.27f, 1f); // #FF4444
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.fontStyle = FontStyles.Bold;
        }

        // Setup Notification_Text
        var notificationText = canvas.transform.Find("Notification_Text");
        if (notificationText != null)
        {
            var notifRect = notificationText.GetComponent<RectTransform>();
            notifRect.anchorMin = new Vector2(0.5f, 1f);
            notifRect.anchorMax = new Vector2(0.5f, 1f);
            notifRect.pivot = new Vector2(0.5f, 1f);
            notifRect.anchoredPosition = new Vector2(0, -200);
            notifRect.sizeDelta = new Vector2(600, 40);

            var notifTMP = notificationText.GetComponent<TextMeshProUGUI>();
            notifTMP.text = "";
            notifTMP.fontSize = 24;
            notifTMP.color = Color.white;
            notifTMP.alignment = TextAlignmentOptions.Center;
        }

        // Setup LoginForm_Panel
        var loginForm = canvas.transform.Find("LoginForm_Panel");
        if (loginForm != null)
        {
            var formRect = loginForm.GetComponent<RectTransform>();
            formRect.anchorMin = new Vector2(0.5f, 0.5f);
            formRect.anchorMax = new Vector2(0.5f, 0.5f);
            formRect.pivot = new Vector2(0.5f, 0.5f);
            formRect.anchoredPosition = new Vector2(0, 0);
            formRect.sizeDelta = new Vector2(400, 200);

            var vlg = loginForm.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            // Setup ID_Row
            SetupInputRow(loginForm.Find("ID_Row"), "ID", "Enter ID");
            // Setup Password_Row
            SetupInputRow(loginForm.Find("Password_Row"), "Password", "Enter Password", true);
            // Setup PasswordConfirm_Row
            SetupInputRow(loginForm.Find("PasswordConfirm_Row"), "Confirm", "Confirm Password", true);
        }

        // Setup Button_Group
        var buttonGroup = canvas.transform.Find("Button_Group");
        if (buttonGroup != null)
        {
            var btnGroupRect = buttonGroup.GetComponent<RectTransform>();
            btnGroupRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnGroupRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnGroupRect.pivot = new Vector2(0.5f, 0.5f);
            btnGroupRect.anchoredPosition = new Vector2(0, -150);
            btnGroupRect.sizeDelta = new Vector2(400, 50);

            var hlg = buttonGroup.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Login mode buttons
            SetupButton(buttonGroup.Find("Button_Register"), "Register", 120, 40);
            SetupButton(buttonGroup.Find("Button_Login"), "Login", 120, 40);
            // Signup mode buttons
            SetupButton(buttonGroup.Find("Button_Back"), "Back", 120, 40);
            SetupButton(buttonGroup.Find("Button_Signup"), "Sign Up", 120, 40);
        }

        EditorUtility.SetDirty(canvas);
        Debug.Log("Login UI setup complete!");
    }

    private static void SetupInputRow(Transform row, string labelText, string placeholder, bool isPassword = false)
    {
        if (row == null) return;

        var rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(370, 45);

        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Setup Label
        var label = row.Find(row.name.Replace("_Row", "_Label"));
        if (label != null)
        {
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(100, 40);

            var le = label.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth = 100;
                le.preferredHeight = 40;
            }

            var labelTMP = label.GetComponent<TextMeshProUGUI>();
            labelTMP.text = labelText;
            labelTMP.fontSize = 20;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        }

        // Setup InputField
        var inputField = row.Find(row.name.Replace("_Row", "_InputField"));
        if (inputField != null)
        {
            var inputRect = inputField.GetComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(250, 40);

            var le = inputField.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth = 250;
                le.preferredHeight = 40;
            }

            var inputImage = inputField.GetComponent<Image>();
            inputImage.color = Color.white;

            var tmpInput = inputField.GetComponent<TMP_InputField>();
            if (tmpInput != null)
            {
                if (isPassword)
                    tmpInput.contentType = TMP_InputField.ContentType.Password;
                else
                    tmpInput.contentType = TMP_InputField.ContentType.Standard;

                // Setup Text Area
                var textArea = inputField.Find("Text Area");
                if (textArea != null)
                {
                    var textAreaRect = textArea.GetComponent<RectTransform>();
                    textAreaRect.anchorMin = Vector2.zero;
                    textAreaRect.anchorMax = Vector2.one;
                    textAreaRect.offsetMin = new Vector2(10, 5);
                    textAreaRect.offsetMax = new Vector2(-10, -5);

                    // Setup Placeholder
                    var placeholderObj = textArea.Find("Placeholder");
                    if (placeholderObj != null)
                    {
                        var phRect = placeholderObj.GetComponent<RectTransform>();
                        phRect.anchorMin = Vector2.zero;
                        phRect.anchorMax = Vector2.one;
                        phRect.offsetMin = Vector2.zero;
                        phRect.offsetMax = Vector2.zero;

                        var phTMP = placeholderObj.GetComponent<TextMeshProUGUI>();
                        phTMP.text = placeholder;
                        phTMP.fontSize = 16;
                        phTMP.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                        phTMP.alignment = TextAlignmentOptions.MidlineLeft;

                        tmpInput.placeholder = phTMP;
                    }

                    // Setup Text
                    var textObj = textArea.Find("Text");
                    if (textObj != null)
                    {
                        var txtRect = textObj.GetComponent<RectTransform>();
                        txtRect.anchorMin = Vector2.zero;
                        txtRect.anchorMax = Vector2.one;
                        txtRect.offsetMin = Vector2.zero;
                        txtRect.offsetMax = Vector2.zero;

                        var txtTMP = textObj.GetComponent<TextMeshProUGUI>();
                        txtTMP.text = "";
                        txtTMP.fontSize = 16;
                        txtTMP.color = Color.black;
                        txtTMP.alignment = TextAlignmentOptions.MidlineLeft;

                        tmpInput.textComponent = txtTMP;
                    }
                }
            }
        }
    }

    private static void SetupButton(Transform button, string text, float width, float height)
    {
        if (button == null) return;

        var btnRect = button.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(width, height);

        var le = button.GetComponent<LayoutElement>();
        if (le != null)
        {
            le.preferredWidth = width;
            le.preferredHeight = height;
        }

        var btnImage = button.GetComponent<Image>();
        btnImage.color = new Color(1f, 1f, 1f, 0.7f); // Semi-transparent white

        // Setup Text
        var textObj = button.Find("Text");
        if (textObj != null)
        {
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textTMP = textObj.GetComponent<TextMeshProUGUI>();
            textTMP.text = text;
            textTMP.fontSize = 18;
            textTMP.color = Color.black;
            textTMP.alignment = TextAlignmentOptions.Center;
        }
    }
}
