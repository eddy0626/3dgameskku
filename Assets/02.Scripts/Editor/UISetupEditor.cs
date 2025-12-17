using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class UISetupEditor : EditorWindow
{
    [MenuItem("Tools/Setup Game UI")]
    public static void SetupUI()
    {
        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null)
        {
            Debug.LogError("GameCanvas not found!");
            return;
        }

        // Load sprites from UI folder
        var coinIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/04.Images/UI/coin_icon.png");
        var crosshair = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/04.Images/UI/crosshair.png");
        var healthBarBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/04.Images/UI/health_bar_bg.png");
        var healthBarFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/04.Images/UI/health_bar_fill.png");
        var heartIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/04.Images/UI/heart_icon.png");
        var squadMemberIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/04.Images/UI/squad_member_icon.png");
        var lightningIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/04.Images/UI/lightning_icon.png");

        // Setup Canvas
        var canvasRect = canvas.GetComponent<RectTransform>();
        
        // Setup PlayingPanel
        SetupPlayingPanel(canvas, coinIcon, crosshair, healthBarBg, healthBarFill, squadMemberIcon);
        
        // Setup PausePanel
        SetupPausePanel(canvas);
        
        // Setup UpgradePanel
        SetupUpgradePanel(canvas, lightningIcon);
        
        // Setup GameOverPanel
        SetupGameOverPanel(canvas);
        
        // Hide non-playing panels initially
        SetPanelActive(canvas, "PausePanel", false);
        SetPanelActive(canvas, "UpgradePanel", false);
        SetPanelActive(canvas, "GameOverPanel", false);
        SetPanelActive(canvas, "InventoryPanel", false);
        SetPanelActive(canvas, "SettingsPanel", false);

        EditorUtility.SetDirty(canvas);
        Debug.Log("UI Setup Complete!");
    }

    private static void SetupPlayingPanel(GameObject canvas, Sprite coinIcon, Sprite crosshair, 
        Sprite healthBarBg, Sprite healthBarFill, Sprite squadMemberIcon)
    {
        var playingPanel = canvas.transform.Find("PlayingPanel");
        if (playingPanel == null) return;
        
        // Stretch PlayingPanel to full screen
        SetFullStretch(playingPanel.GetComponent<RectTransform>());

        // CoinUI - Top Left
        var coinUI = playingPanel.Find("CoinUI");
        if (coinUI != null)
        {
            var coinRect = coinUI.GetComponent<RectTransform>();
            SetAnchor(coinRect, AnchorPreset.TopLeft);
            coinRect.anchoredPosition = new Vector2(100, -50);
            coinRect.sizeDelta = new Vector2(200, 50);

            var coinIconObj = coinUI.Find("CoinIcon");
            if (coinIconObj != null)
            {
                var iconRect = coinIconObj.GetComponent<RectTransform>();
                SetAnchor(iconRect, AnchorPreset.MiddleLeft);
                iconRect.anchoredPosition = new Vector2(20, 0);
                iconRect.sizeDelta = new Vector2(40, 40);
                var img = coinIconObj.GetComponent<Image>();
                if (img && coinIcon) img.sprite = coinIcon;
            }

            var coinText = coinUI.Find("CoinText");
            if (coinText != null)
            {
                var textRect = coinText.GetComponent<RectTransform>();
                SetAnchor(textRect, AnchorPreset.MiddleCenter);
                textRect.anchoredPosition = new Vector2(30, 0);
                textRect.sizeDelta = new Vector2(100, 40);
                var tmp = coinText.GetComponent<TextMeshProUGUI>();
                if (tmp)
                {
                    tmp.text = "0";
                    tmp.fontSize = 28;
                    tmp.alignment = TextAlignmentOptions.Left;
                    tmp.color = Color.yellow;
                }
            }
        }

        // WaveUI - Top Center
        var waveUI = playingPanel.Find("WaveUI");
        if (waveUI != null)
        {
            var waveRect = EnsureRectTransform(waveUI.gameObject);
            SetAnchor(waveRect, AnchorPreset.TopCenter);
            waveRect.anchoredPosition = new Vector2(0, -50);
            waveRect.sizeDelta = new Vector2(300, 80);

            var waveText = waveUI.Find("WaveText");
            if (waveText != null)
            {
                var textRect = waveText.GetComponent<RectTransform>();
                SetAnchor(textRect, AnchorPreset.TopCenter);
                textRect.anchoredPosition = new Vector2(0, 0);
                textRect.sizeDelta = new Vector2(200, 40);
                var tmp = waveText.GetComponent<TextMeshProUGUI>();
                if (tmp)
                {
                    tmp.text = "Wave 1";
                    tmp.fontSize = 32;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = Color.white;
                }
            }

            var enemyCountText = waveUI.Find("EnemyCountText");
            if (enemyCountText != null)
            {
                var textRect = enemyCountText.GetComponent<RectTransform>();
                SetAnchor(textRect, AnchorPreset.BottomCenter);
                textRect.anchoredPosition = new Vector2(0, 10);
                textRect.sizeDelta = new Vector2(150, 30);
                var tmp = enemyCountText.GetComponent<TextMeshProUGUI>();
                if (tmp)
                {
                    tmp.text = "Enemies: 0";
                    tmp.fontSize = 20;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = Color.white;
                }
            }
        }

        // CrosshairUI - Center
        var crosshairUI = playingPanel.Find("CrosshairUI");
        if (crosshairUI != null)
        {
            var crosshairRect = crosshairUI.GetComponent<RectTransform>();
            SetAnchor(crosshairRect, AnchorPreset.MiddleCenter);
            crosshairRect.anchoredPosition = Vector2.zero;
            crosshairRect.sizeDelta = new Vector2(64, 64);
            var img = crosshairUI.GetComponent<Image>();
            if (img && crosshair)
            {
                img.sprite = crosshair;
                img.color = Color.white;
                img.raycastTarget = false;
            }
        }

        // SquadStatusUI - Bottom Left
        var squadStatusUI = playingPanel.Find("SquadStatusUI");
        if (squadStatusUI != null)
        {
            var squadRect = EnsureRectTransform(squadStatusUI.gameObject);
            SetAnchor(squadRect, AnchorPreset.BottomLeft);
            squadRect.anchoredPosition = new Vector2(20, 20);
            squadRect.sizeDelta = new Vector2(450, 100);

            var slotsContainer = squadStatusUI.Find("SlotsContainer");
            if (slotsContainer != null)
            {
                var containerRect = slotsContainer.GetComponent<RectTransform>();
                SetFullStretch(containerRect);

                var layout = slotsContainer.GetComponent<HorizontalLayoutGroup>();
                if (layout)
                {
                    layout.spacing = 10;
                    layout.childAlignment = TextAnchor.MiddleLeft;
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = false;
                }

                // Setup each member slot
                for (int i = 1; i <= 4; i++)
                {
                    var slot = slotsContainer.Find($"MemberSlot_{i}");
                    if (slot != null)
                    {
                        var slotRect = slot.GetComponent<RectTransform>();
                        slotRect.sizeDelta = new Vector2(100, 80);
                        
                        var slotImg = slot.GetComponent<Image>();
                        if (slotImg)
                        {
                            slotImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                        }

                        // Setup member icon if exists
                        var memberIcon = slot.Find("MemberIcon");
                        if (memberIcon != null)
                        {
                            var iconRect = memberIcon.GetComponent<RectTransform>();
                            SetAnchor(iconRect, AnchorPreset.TopCenter);
                            iconRect.anchoredPosition = new Vector2(0, -10);
                            iconRect.sizeDelta = new Vector2(32, 32);
                            var img = memberIcon.GetComponent<Image>();
                            if (img && squadMemberIcon) img.sprite = squadMemberIcon;
                        }

                        // Setup health bar background
                        var healthBg = slot.Find("HealthBarBG");
                        if (healthBg != null)
                        {
                            var bgRect = healthBg.GetComponent<RectTransform>();
                            SetAnchor(bgRect, AnchorPreset.BottomCenter);
                            bgRect.anchoredPosition = new Vector2(0, 15);
                            bgRect.sizeDelta = new Vector2(80, 10);
                            var img = healthBg.GetComponent<Image>();
                            if (img && healthBarBg) img.sprite = healthBarBg;
                            if (img) img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                        }

                        // Setup health bar fill
                        var healthFill = slot.Find("HealthBarFill");
                        if (healthFill != null)
                        {
                            var fillRect = healthFill.GetComponent<RectTransform>();
                            SetAnchor(fillRect, AnchorPreset.BottomCenter);
                            fillRect.anchoredPosition = new Vector2(0, 15);
                            fillRect.sizeDelta = new Vector2(80, 10);
                            var img = healthFill.GetComponent<Image>();
                            if (img && healthBarFill) img.sprite = healthBarFill;
                            if (img)
                            {
                                img.color = Color.green;
                                img.type = Image.Type.Filled;
                                img.fillMethod = Image.FillMethod.Horizontal;
                            }
                        }
                    }
                }
            }
        }
    }

    private static void SetupPausePanel(GameObject canvas)
    {
        var pausePanel = canvas.transform.Find("PausePanel");
        if (pausePanel == null) return;

        var pauseRect = EnsureRectTransform(pausePanel.gameObject);
        SetFullStretch(pauseRect);

        var bg = pausePanel.Find("PauseBackground");
        if (bg != null)
        {
            var bgRect = bg.GetComponent<RectTransform>();
            SetFullStretch(bgRect);
            var img = bg.GetComponent<Image>();
            if (img) img.color = new Color(0, 0, 0, 0.7f);
        }

        var title = pausePanel.Find("PauseTitle");
        if (title != null)
        {
            var titleRect = title.GetComponent<RectTransform>();
            SetAnchor(titleRect, AnchorPreset.TopCenter);
            titleRect.anchoredPosition = new Vector2(0, -100);
            titleRect.sizeDelta = new Vector2(400, 80);
            var tmp = title.GetComponent<TextMeshProUGUI>();
            if (tmp)
            {
                tmp.text = "PAUSED";
                tmp.fontSize = 64;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }
        }

        SetupButton(pausePanel, "ResumeButton", new Vector2(0, 50), "Resume");
        SetupButton(pausePanel, "SettingsButton", new Vector2(0, -30), "Settings");
        SetupButton(pausePanel, "QuitButton", new Vector2(0, -110), "Quit");
    }

    private static void SetupUpgradePanel(GameObject canvas, Sprite lightningIcon)
    {
        var upgradePanel = canvas.transform.Find("UpgradePanel");
        if (upgradePanel == null) return;

        var upgradeRect = EnsureRectTransform(upgradePanel.gameObject);
        SetFullStretch(upgradeRect);

        var bg = upgradePanel.Find("BackgroundOverlay");
        if (bg != null)
        {
            var bgRect = bg.GetComponent<RectTransform>();
            SetFullStretch(bgRect);
            var img = bg.GetComponent<Image>();
            if (img) img.color = new Color(0, 0, 0, 0.8f);
        }

        var title = upgradePanel.Find("TitleText");
        if (title != null)
        {
            var titleRect = title.GetComponent<RectTransform>();
            SetAnchor(titleRect, AnchorPreset.TopCenter);
            titleRect.anchoredPosition = new Vector2(0, -80);
            titleRect.sizeDelta = new Vector2(500, 60);
            var tmp = title.GetComponent<TextMeshProUGUI>();
            if (tmp)
            {
                tmp.text = "Choose an Upgrade";
                tmp.fontSize = 48;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.yellow;
            }
        }

        var buttonsContainer = upgradePanel.Find("UpgradeButtonsContainer");
        if (buttonsContainer != null)
        {
            var containerRect = buttonsContainer.GetComponent<RectTransform>();
            SetAnchor(containerRect, AnchorPreset.MiddleCenter);
            containerRect.anchoredPosition = new Vector2(0, 0);
            containerRect.sizeDelta = new Vector2(900, 300);

            var layout = buttonsContainer.GetComponent<HorizontalLayoutGroup>();
            if (layout)
            {
                layout.spacing = 30;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            for (int i = 1; i <= 3; i++)
            {
                var btn = buttonsContainer.Find($"UpgradeButton_{i}");
                if (btn != null)
                {
                    var btnRect = btn.GetComponent<RectTransform>();
                    btnRect.sizeDelta = new Vector2(250, 280);
                    var img = btn.GetComponent<Image>();
                    if (img) img.color = new Color(0.15f, 0.15f, 0.25f, 1f);
                }
            }
        }

        SetupButton(upgradePanel, "SkipButton", new Vector2(-150, -220), "Skip (+50g)");
        SetupButton(upgradePanel, "RerollButton", new Vector2(150, -220), "Reroll (100g)");
    }

    private static void SetupGameOverPanel(GameObject canvas)
    {
        var gameOverPanel = canvas.transform.Find("GameOverPanel");
        if (gameOverPanel == null) return;

        var goRect = EnsureRectTransform(gameOverPanel.gameObject);
        SetFullStretch(goRect);

        var bg = gameOverPanel.Find("GameOverBackground");
        if (bg != null)
        {
            var bgRect = bg.GetComponent<RectTransform>();
            SetFullStretch(bgRect);
            var img = bg.GetComponent<Image>();
            if (img) img.color = new Color(0.1f, 0, 0, 0.9f);
        }

        var title = gameOverPanel.Find("GameOverTitle");
        if (title != null)
        {
            var titleRect = title.GetComponent<RectTransform>();
            SetAnchor(titleRect, AnchorPreset.TopCenter);
            titleRect.anchoredPosition = new Vector2(0, -150);
            titleRect.sizeDelta = new Vector2(600, 100);
            var tmp = title.GetComponent<TextMeshProUGUI>();
            if (tmp)
            {
                tmp.text = "GAME OVER";
                tmp.fontSize = 72;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.red;
            }
        }

        var scoreText = gameOverPanel.Find("ScoreText");
        if (scoreText != null)
        {
            var scoreRect = scoreText.GetComponent<RectTransform>();
            SetAnchor(scoreRect, AnchorPreset.MiddleCenter);
            scoreRect.anchoredPosition = new Vector2(0, 50);
            scoreRect.sizeDelta = new Vector2(400, 50);
            var tmp = scoreText.GetComponent<TextMeshProUGUI>();
            if (tmp)
            {
                tmp.text = "Score: 0";
                tmp.fontSize = 36;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }
        }

        var waveText = gameOverPanel.Find("WaveReachedText");
        if (waveText != null)
        {
            var waveRect = waveText.GetComponent<RectTransform>();
            SetAnchor(waveRect, AnchorPreset.MiddleCenter);
            waveRect.anchoredPosition = new Vector2(0, 0);
            waveRect.sizeDelta = new Vector2(400, 40);
            var tmp = waveText.GetComponent<TextMeshProUGUI>();
            if (tmp)
            {
                tmp.text = "Wave Reached: 1";
                tmp.fontSize = 28;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.gray;
            }
        }

        SetupButton(gameOverPanel, "RestartButton", new Vector2(0, -100), "Restart");
        SetupButton(gameOverPanel, "MainMenuButton", new Vector2(0, -180), "Main Menu");
    }

    private static void SetupButton(Transform parent, string buttonName, Vector2 position, string text)
    {
        var btn = parent.Find(buttonName);
        if (btn == null) return;

        var btnRect = btn.GetComponent<RectTransform>();
        SetAnchor(btnRect, AnchorPreset.MiddleCenter);
        btnRect.anchoredPosition = position;
        btnRect.sizeDelta = new Vector2(200, 60);

        var img = btn.GetComponent<Image>();
        if (img) img.color = new Color(0.3f, 0.3f, 0.4f, 1f);

        // Add text child if not exists
        var textObj = btn.Find("Text");
        if (textObj == null)
        {
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btn);
            textGO.AddComponent<RectTransform>();
            textGO.AddComponent<TextMeshProUGUI>();
            textObj = textGO.transform;
        }

        var textRect = textObj.GetComponent<RectTransform>();
        SetFullStretch(textRect);
        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        if (tmp)
        {
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }
    }

    private static void SetPanelActive(GameObject canvas, string panelName, bool active)
    {
        var panel = canvas.transform.Find(panelName);
        if (panel != null) panel.gameObject.SetActive(active);
    }

    private static RectTransform EnsureRectTransform(GameObject go)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect == null)
        {
            var oldTransform = go.transform;
            var parent = oldTransform.parent;
            var siblingIndex = oldTransform.GetSiblingIndex();
            
            DestroyImmediate(oldTransform);
            rect = go.AddComponent<RectTransform>();
            go.transform.SetParent(parent);
            go.transform.SetSiblingIndex(siblingIndex);
        }
        return rect;
    }

    private enum AnchorPreset
    {
        TopLeft, TopCenter, TopRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        BottomLeft, BottomCenter, BottomRight
    }

    private static void SetAnchor(RectTransform rect, AnchorPreset preset)
    {
        switch (preset)
        {
            case AnchorPreset.TopLeft:
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                break;
            case AnchorPreset.TopCenter:
                rect.anchorMin = new Vector2(0.5f, 1);
                rect.anchorMax = new Vector2(0.5f, 1);
                rect.pivot = new Vector2(0.5f, 1);
                break;
            case AnchorPreset.TopRight:
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 1);
                break;
            case AnchorPreset.MiddleLeft:
                rect.anchorMin = new Vector2(0, 0.5f);
                rect.anchorMax = new Vector2(0, 0.5f);
                rect.pivot = new Vector2(0, 0.5f);
                break;
            case AnchorPreset.MiddleCenter:
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                break;
            case AnchorPreset.MiddleRight:
                rect.anchorMin = new Vector2(1, 0.5f);
                rect.anchorMax = new Vector2(1, 0.5f);
                rect.pivot = new Vector2(1, 0.5f);
                break;
            case AnchorPreset.BottomLeft:
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(0, 0);
                rect.pivot = new Vector2(0, 0);
                break;
            case AnchorPreset.BottomCenter:
                rect.anchorMin = new Vector2(0.5f, 0);
                rect.anchorMax = new Vector2(0.5f, 0);
                rect.pivot = new Vector2(0.5f, 0);
                break;
            case AnchorPreset.BottomRight:
                rect.anchorMin = new Vector2(1, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(1, 0);
                break;
        }
    }

    private static void SetFullStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}