using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

/// <summary>
/// 분대 서바이벌 UI Canvas 자동 생성 에디터
/// - Menu: Tools/Squad Survival/Create UI Canvas
/// </summary>
public class SquadSurvivalUISetup : EditorWindow
{
    #region Constants
    private const string SPRITES_PATH = "Assets/04.Images/UI";
    private const string PREFABS_PATH = "Assets/03.Prefabs/UI";
    private const string FONTS_PATH = "Assets/08.Fonts";
    #endregion

    #region Window
    [MenuItem("Tools/Squad Survival/Create UI Canvas")]
    public static void ShowWindow()
    {
        GetWindow<SquadSurvivalUISetup>("Squad Survival UI Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Squad Survival UI Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Create Full UI Canvas", GUILayout.Height(40)))
        {
            CreateFullUICanvas();
        }

        GUILayout.Space(10);
        GUILayout.Label("Individual Components:", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Playing Panel (CoinUI, WaveUI)"))
        {
            var canvas = FindOrCreateCanvas();
            CreatePlayingPanel(canvas.transform);
        }

        if (GUILayout.Button("Create Pause Panel"))
        {
            var canvas = FindOrCreateCanvas();
            CreatePausePanel(canvas.transform);
        }

        if (GUILayout.Button("Create Upgrade Panel"))
        {
            var canvas = FindOrCreateCanvas();
            CreateUpgradePanel(canvas.transform);
        }

        if (GUILayout.Button("Create Game Over Panel"))
        {
            var canvas = FindOrCreateCanvas();
            CreateGameOverPanel(canvas.transform);
        }

        if (GUILayout.Button("Create Crosshair"))
        {
            var canvas = FindOrCreateCanvas();
            CreateCrosshair(canvas.transform);
        }

        GUILayout.Space(20);
        if (GUILayout.Button("Generate UI Sprites (Aseprite)", GUILayout.Height(30)))
        {
            GenerateUISprites();
        }
    }
    #endregion


    #region Main Creation
    private static void CreateFullUICanvas()
    {
        // Canvas 생성
        var canvas = FindOrCreateCanvas();

        // 각 패널 생성
        CreatePlayingPanel(canvas.transform);
        CreatePausePanel(canvas.transform);
        CreateUpgradePanel(canvas.transform);
        CreateGameOverPanel(canvas.transform);
        CreateCrosshair(canvas.transform);

        // UIManager 컴포넌트 추가 및 연결
        var uiManager = canvas.GetComponent<UIManager>();
        if (uiManager == null)
        {
            uiManager = canvas.gameObject.AddComponent<UIManager>();
        }

        // 패널 참조 연결 (SerializedObject 사용)
        SerializedObject so = new SerializedObject(uiManager);
        so.FindProperty("_playingPanel").objectReferenceValue = canvas.transform.Find("PlayingPanel")?.gameObject;
        so.FindProperty("_pausePanel").objectReferenceValue = canvas.transform.Find("PausePanel")?.gameObject;
        so.FindProperty("_upgradePanel").objectReferenceValue = canvas.transform.Find("UpgradePanel")?.gameObject;
        so.FindProperty("_gameOverPanel").objectReferenceValue = canvas.transform.Find("GameOverPanel")?.gameObject;
        so.FindProperty("_crosshairController").objectReferenceValue = canvas.transform.Find("Crosshair")?.GetComponent<CrosshairController>();
        so.ApplyModifiedProperties();

        // 프리팹 저장
        SaveAsPrefab(canvas.gameObject, "SquadSurvivalCanvas");

        Debug.Log("<color=green>[UI Setup] Full UI Canvas 생성 완료!</color>");
    }

    private static Canvas FindOrCreateCanvas()
    {
        var existing = GameObject.Find("SquadSurvivalCanvas");
        if (existing != null)
        {
            return existing.GetComponent<Canvas>();
        }

        // Canvas 생성
        var canvasGO = new GameObject("SquadSurvivalCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        Undo.RegisterCreatedObjectUndo(canvasGO, "Create UI Canvas");
        return canvas;
    }
    #endregion


    #region Playing Panel (CoinUI, WaveUI, SquadStatusUI)
    private static void CreatePlayingPanel(Transform parent)
    {
        var existing = parent.Find("PlayingPanel");
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }

        // PlayingPanel (전체 화면)
        var panel = CreateUIElement("PlayingPanel", parent);
        SetFullStretch(panel);

        // CoinUI (좌상단)
        var coinUI = CreateCoinUI(panel);

        // WaveUI (상단 중앙)
        var waveUI = CreateWaveUI(panel);

        // SquadStatusUI (좌하단)
        var squadUI = CreateSquadStatusUI(panel);

        Debug.Log("[UI Setup] PlayingPanel 생성 완료");
    }

    private static RectTransform CreateCoinUI(RectTransform parent)
    {
        var coinPanel = CreateUIElement("CoinUI", parent);
        SetAnchor(coinPanel, AnchorPreset.TopLeft);
        coinPanel.anchoredPosition = new Vector2(20, -20);
        coinPanel.sizeDelta = new Vector2(200, 50);

        var layoutGroup = coinPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = 10;
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.padding = new RectOffset(10, 10, 5, 5);
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;

        // 코인 아이콘
        var iconObj = CreateUIElement("CoinIcon", coinPanel);
        iconObj.sizeDelta = new Vector2(40, 40);
        var iconImage = iconObj.gameObject.AddComponent<Image>();
        iconImage.color = new Color(1f, 0.84f, 0f); // 금색

        // 코인 텍스트
        var textObj = CreateUIElement("CoinText", coinPanel);
        textObj.sizeDelta = new Vector2(140, 40);
        var coinText = textObj.gameObject.AddComponent<TextMeshProUGUI>();
        coinText.text = "0";
        coinText.fontSize = 32;
        coinText.fontStyle = FontStyles.Bold;
        coinText.alignment = TextAlignmentOptions.MidlineLeft;

        // 변화량 텍스트 (옵션)
        var changeObj = CreateUIElement("ChangeText", coinPanel);
        changeObj.sizeDelta = new Vector2(100, 30);
        changeObj.anchoredPosition = new Vector2(100, 30);
        var changeText = changeObj.gameObject.AddComponent<TextMeshProUGUI>();
        changeText.text = "";
        changeText.fontSize = 20;
        changeText.alignment = TextAlignmentOptions.MidlineLeft;
        changeObj.gameObject.SetActive(false);

        // CoinUI 컴포넌트 추가
        var coinUIComp = coinPanel.gameObject.AddComponent<CoinUI>();
        SerializedObject so = new SerializedObject(coinUIComp);
        so.FindProperty("_coinText").objectReferenceValue = coinText;
        so.FindProperty("_coinIcon").objectReferenceValue = iconImage;
        so.FindProperty("_changeText").objectReferenceValue = changeText;
        so.ApplyModifiedProperties();

        return coinPanel;
    }


    private static RectTransform CreateWaveUI(RectTransform parent)
    {
        var wavePanel = CreateUIElement("WaveUI", parent);
        SetAnchor(wavePanel, AnchorPreset.TopCenter);
        wavePanel.anchoredPosition = new Vector2(0, -20);
        wavePanel.sizeDelta = new Vector2(300, 80);

        // 웨이브 텍스트
        var waveTextObj = CreateUIElement("WaveText", wavePanel);
        SetAnchor(waveTextObj, AnchorPreset.TopCenter);
        waveTextObj.anchoredPosition = new Vector2(0, 0);
        waveTextObj.sizeDelta = new Vector2(300, 40);
        var waveText = waveTextObj.gameObject.AddComponent<TextMeshProUGUI>();
        waveText.text = "Wave 1 / 10";
        waveText.fontSize = 28;
        waveText.fontStyle = FontStyles.Bold;
        waveText.alignment = TextAlignmentOptions.Center;

        // 적 카운트 텍스트
        var enemyTextObj = CreateUIElement("EnemyCountText", wavePanel);
        SetAnchor(enemyTextObj, AnchorPreset.BottomCenter);
        enemyTextObj.anchoredPosition = new Vector2(0, 10);
        enemyTextObj.sizeDelta = new Vector2(200, 30);
        var enemyText = enemyTextObj.gameObject.AddComponent<TextMeshProUGUI>();
        enemyText.text = "Enemies: 0 / 0";
        enemyText.fontSize = 18;
        enemyText.alignment = TextAlignmentOptions.Center;

        // 웨이브 알림 텍스트 (중앙, 숨김 상태)
        var announcementObj = CreateUIElement("WaveAnnouncementText", wavePanel);
        SetAnchor(announcementObj, AnchorPreset.MiddleCenter);
        announcementObj.anchoredPosition = Vector2.zero;
        announcementObj.sizeDelta = new Vector2(600, 100);
        var announcementText = announcementObj.gameObject.AddComponent<TextMeshProUGUI>();
        announcementText.text = "WAVE 1";
        announcementText.fontSize = 72;
        announcementText.fontStyle = FontStyles.Bold;
        announcementText.alignment = TextAlignmentOptions.Center;
        announcementObj.gameObject.SetActive(false);

        // 카운트다운 텍스트
        var countdownObj = CreateUIElement("CountdownText", wavePanel);
        SetAnchor(countdownObj, AnchorPreset.MiddleCenter);
        countdownObj.anchoredPosition = new Vector2(0, -80);
        countdownObj.sizeDelta = new Vector2(200, 60);
        var countdownText = countdownObj.gameObject.AddComponent<TextMeshProUGUI>();
        countdownText.text = "3";
        countdownText.fontSize = 48;
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownObj.gameObject.SetActive(false);

        // WaveUI 컴포넌트 추가
        var waveUIComp = wavePanel.gameObject.AddComponent<WaveUI>();
        SerializedObject so = new SerializedObject(waveUIComp);
        so.FindProperty("_waveText").objectReferenceValue = waveText;
        so.FindProperty("_enemyCountText").objectReferenceValue = enemyText;
        so.FindProperty("_waveAnnouncementText").objectReferenceValue = announcementText;
        so.FindProperty("_countdownText").objectReferenceValue = countdownText;
        so.ApplyModifiedProperties();

        return wavePanel;
    }


    private static RectTransform CreateSquadStatusUI(RectTransform parent)
    {
        var squadPanel = CreateUIElement("SquadStatusUI", parent);
        SetAnchor(squadPanel, AnchorPreset.BottomLeft);
        squadPanel.anchoredPosition = new Vector2(20, 20);
        squadPanel.sizeDelta = new Vector2(250, 200);

        var layoutGroup = squadPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 5;
        layoutGroup.childAlignment = TextAnchor.LowerLeft;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        // 분대원 슬롯 컨테이너
        var slotsContainer = CreateUIElement("MemberSlots", squadPanel);
        slotsContainer.sizeDelta = new Vector2(230, 180);

        // SquadStatusUI 컴포넌트 추가
        var squadUIComp = squadPanel.gameObject.AddComponent<SquadStatusUI>();
        SerializedObject so = new SerializedObject(squadUIComp);
        so.FindProperty("_memberSlotsContainer").objectReferenceValue = slotsContainer;
        so.ApplyModifiedProperties();

        return squadPanel;
    }
    #endregion


    #region Pause Panel
    private static void CreatePausePanel(Transform parent)
    {
        var existing = parent.Find("PausePanel");
        if (existing != null) DestroyImmediate(existing.gameObject);

        var panel = CreateUIElement("PausePanel", parent);
        SetFullStretch(panel);

        // 반투명 배경
        var bgImage = panel.gameObject.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);

        // 타이틀
        var titleObj = CreateUIElement("Title", panel);
        SetAnchor(titleObj, AnchorPreset.MiddleCenter);
        titleObj.anchoredPosition = new Vector2(0, 100);
        titleObj.sizeDelta = new Vector2(400, 80);
        var titleText = titleObj.gameObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "PAUSED";
        titleText.fontSize = 56;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        // 버튼 컨테이너
        var buttonsPanel = CreateUIElement("Buttons", panel);
        SetAnchor(buttonsPanel, AnchorPreset.MiddleCenter);
        buttonsPanel.anchoredPosition = new Vector2(0, -30);
        buttonsPanel.sizeDelta = new Vector2(300, 200);

        var layout = buttonsPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 15;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        // Resume 버튼
        CreateButton("ResumeButton", "RESUME", buttonsPanel, new Vector2(250, 50));

        // Settings 버튼
        CreateButton("SettingsButton", "SETTINGS", buttonsPanel, new Vector2(250, 50));

        // Quit 버튼
        CreateButton("QuitButton", "QUIT", buttonsPanel, new Vector2(250, 50));

        panel.gameObject.SetActive(false);
        Debug.Log("[UI Setup] PausePanel 생성 완료");
    }
    #endregion

    #region Upgrade Panel
    private static void CreateUpgradePanel(Transform parent)
    {
        var existing = parent.Find("UpgradePanel");
        if (existing != null) DestroyImmediate(existing.gameObject);

        var panel = CreateUIElement("UpgradePanel", parent);
        SetFullStretch(panel);

        // 반투명 배경
        var bgImage = panel.gameObject.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);

        // 타이틀
        var titleObj = CreateUIElement("Title", panel);
        SetAnchor(titleObj, AnchorPreset.TopCenter);
        titleObj.anchoredPosition = new Vector2(0, -50);
        titleObj.sizeDelta = new Vector2(500, 60);
        var titleText = titleObj.gameObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "SELECT UPGRADE";
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        // 업그레이드 옵션 컨테이너
        var optionsPanel = CreateUIElement("UpgradeOptions", panel);
        SetAnchor(optionsPanel, AnchorPreset.MiddleCenter);
        optionsPanel.anchoredPosition = Vector2.zero;
        optionsPanel.sizeDelta = new Vector2(900, 400);

        var layout = optionsPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 30;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // 3개의 업그레이드 슬롯 생성
        for (int i = 0; i < 3; i++)
        {
            CreateUpgradeSlot($"UpgradeSlot_{i}", optionsPanel);
        }

        // UpgradeUI 컴포넌트 추가
        var upgradeUIComp = panel.gameObject.AddComponent<UpgradeUI>();
        SerializedObject so = new SerializedObject(upgradeUIComp);
        so.FindProperty("_upgradeOptionsContainer").objectReferenceValue = optionsPanel;
        so.ApplyModifiedProperties();

        panel.gameObject.SetActive(false);
        Debug.Log("[UI Setup] UpgradePanel 생성 완료");
    }

    private static void CreateUpgradeSlot(string name, RectTransform parent)
    {
        var slot = CreateUIElement(name, parent);
        slot.sizeDelta = new Vector2(250, 350);

        var image = slot.gameObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        var button = slot.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        // 아이콘
        var iconObj = CreateUIElement("Icon", slot);
        SetAnchor(iconObj, AnchorPreset.TopCenter);
        iconObj.anchoredPosition = new Vector2(0, -30);
        iconObj.sizeDelta = new Vector2(80, 80);
        var iconImage = iconObj.gameObject.AddComponent<Image>();
        iconImage.color = Color.white;

        // 이름
        var nameObj = CreateUIElement("Name", slot);
        SetAnchor(nameObj, AnchorPreset.TopCenter);
        nameObj.anchoredPosition = new Vector2(0, -130);
        nameObj.sizeDelta = new Vector2(220, 40);
        var nameText = nameObj.gameObject.AddComponent<TextMeshProUGUI>();
        nameText.text = "Upgrade Name";
        nameText.fontSize = 22;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;

        // 설명
        var descObj = CreateUIElement("Description", slot);
        SetAnchor(descObj, AnchorPreset.MiddleCenter);
        descObj.anchoredPosition = new Vector2(0, 20);
        descObj.sizeDelta = new Vector2(220, 100);
        var descText = descObj.gameObject.AddComponent<TextMeshProUGUI>();
        descText.text = "Upgrade description goes here...";
        descText.fontSize = 16;
        descText.alignment = TextAlignmentOptions.Center;

        // 비용
        var costObj = CreateUIElement("Cost", slot);
        SetAnchor(costObj, AnchorPreset.BottomCenter);
        costObj.anchoredPosition = new Vector2(0, 30);
        costObj.sizeDelta = new Vector2(150, 40);
        var costText = costObj.gameObject.AddComponent<TextMeshProUGUI>();
        costText.text = "100 Coins";
        costText.fontSize = 20;
        costText.alignment = TextAlignmentOptions.Center;
        costText.color = new Color(1f, 0.84f, 0f);
    }
    #endregion


    #region Game Over Panel
    private static void CreateGameOverPanel(Transform parent)
    {
        var existing = parent.Find("GameOverPanel");
        if (existing != null) DestroyImmediate(existing.gameObject);

        var panel = CreateUIElement("GameOverPanel", parent);
        SetFullStretch(panel);

        // 반투명 배경
        var bgImage = panel.gameObject.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.85f);

        // 타이틀
        var titleObj = CreateUIElement("Title", panel);
        SetAnchor(titleObj, AnchorPreset.MiddleCenter);
        titleObj.anchoredPosition = new Vector2(0, 120);
        titleObj.sizeDelta = new Vector2(600, 100);
        var titleText = titleObj.gameObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "GAME OVER";
        titleText.fontSize = 72;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.9f, 0.2f, 0.2f);

        // 스탯 정보 컨테이너
        var statsPanel = CreateUIElement("Stats", panel);
        SetAnchor(statsPanel, AnchorPreset.MiddleCenter);
        statsPanel.anchoredPosition = new Vector2(0, 20);
        statsPanel.sizeDelta = new Vector2(400, 150);
        var statsLayout = statsPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        statsLayout.spacing = 10;
        statsLayout.childAlignment = TextAnchor.MiddleCenter;

        // 웨이브 도달
        var waveStatObj = CreateUIElement("WaveStat", statsPanel);
        waveStatObj.sizeDelta = new Vector2(400, 40);
        var waveStat = waveStatObj.gameObject.AddComponent<TextMeshProUGUI>();
        waveStat.text = "Waves Survived: 0";
        waveStat.fontSize = 28;
        waveStat.alignment = TextAlignmentOptions.Center;

        // 처치 수
        var killStatObj = CreateUIElement("KillStat", statsPanel);
        killStatObj.sizeDelta = new Vector2(400, 40);
        var killStat = killStatObj.gameObject.AddComponent<TextMeshProUGUI>();
        killStat.text = "Enemies Killed: 0";
        killStat.fontSize = 28;
        killStat.alignment = TextAlignmentOptions.Center;

        // 획득 코인
        var coinStatObj = CreateUIElement("CoinStat", statsPanel);
        coinStatObj.sizeDelta = new Vector2(400, 40);
        var coinStat = coinStatObj.gameObject.AddComponent<TextMeshProUGUI>();
        coinStat.text = "Coins Earned: 0";
        coinStat.fontSize = 28;
        coinStat.alignment = TextAlignmentOptions.Center;

        // 버튼 컨테이너
        var buttonsPanel = CreateUIElement("Buttons", panel);
        SetAnchor(buttonsPanel, AnchorPreset.MiddleCenter);
        buttonsPanel.anchoredPosition = new Vector2(0, -120);
        buttonsPanel.sizeDelta = new Vector2(400, 120);
        var btnLayout = buttonsPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
        btnLayout.spacing = 30;
        btnLayout.childAlignment = TextAnchor.MiddleCenter;

        // Retry 버튼
        CreateButton("RetryButton", "RETRY", buttonsPanel, new Vector2(150, 50));

        // Quit 버튼
        CreateButton("QuitButton", "QUIT", buttonsPanel, new Vector2(150, 50));

        panel.gameObject.SetActive(false);
        Debug.Log("[UI Setup] GameOverPanel 생성 완료");
    }
    #endregion

    #region Crosshair
    private static void CreateCrosshair(Transform parent)
    {
        var existing = parent.Find("Crosshair");
        if (existing != null) DestroyImmediate(existing.gameObject);

        var crosshair = CreateUIElement("Crosshair", parent);
        SetAnchor(crosshair, AnchorPreset.MiddleCenter);
        crosshair.anchoredPosition = Vector2.zero;
        crosshair.sizeDelta = new Vector2(50, 50);

        // 크로스헤어 이미지
        var crosshairImage = crosshair.gameObject.AddComponent<Image>();
        crosshairImage.color = Color.white;
        crosshairImage.raycastTarget = false;

        // 히트 마커 (숨김 상태)
        var hitMarker = CreateUIElement("HitMarker", crosshair);
        hitMarker.anchoredPosition = Vector2.zero;
        hitMarker.sizeDelta = new Vector2(60, 60);
        var hitImage = hitMarker.gameObject.AddComponent<Image>();
        hitImage.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        hitImage.raycastTarget = false;
        hitMarker.gameObject.SetActive(false);

        // CrosshairController 컴포넌트 추가
        var controller = crosshair.gameObject.AddComponent<CrosshairController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("_crosshairImage").objectReferenceValue = crosshairImage;
        so.FindProperty("_hitMarker").objectReferenceValue = hitMarker.gameObject;
        so.ApplyModifiedProperties();

        Debug.Log("[UI Setup] Crosshair 생성 완료");
    }
    #endregion

    #region Sprite Generation (Aseprite MCP)
    private static void GenerateUISprites()
    {
        Debug.Log("[UI Setup] Aseprite MCP를 통해 UI 스프라이트를 생성하세요.");
        Debug.Log("- 코인 아이콘: 32x32 금색 원형");
        Debug.Log("- 크로스헤어: 32x32 십자 형태");
        Debug.Log("- 히트 마커: 32x32 X 형태");
        Debug.Log("- 버튼 배경: 슬라이스 가능한 둥근 사각형");
        EditorUtility.DisplayDialog("UI Sprites", 
            "Aseprite MCP를 사용하여 다음 스프라이트를 생성하세요:\n\n" +
            "• coin_icon.png (32x32)\n" +
            "• crosshair.png (32x32)\n" +
            "• hit_marker.png (32x32)\n" +
            "• button_bg.png (64x32, 9-slice)\n\n" +
            "저장 경로: Assets/04.Images/UI/", "확인");
    }
    #endregion


    #region Utility Methods
    private static RectTransform CreateUIElement(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static RectTransform CreateUIElement(string name, RectTransform parent)
    {
        return CreateUIElement(name, (Transform)parent);
    }

    private static void CreateButton(string name, string text, RectTransform parent, Vector2 size)
    {
        var btnObj = CreateUIElement(name, parent);
        btnObj.sizeDelta = size;

        var btnImage = btnObj.gameObject.AddComponent<Image>();
        btnImage.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);

        var button = btnObj.gameObject.AddComponent<Button>();
        button.targetGraphic = btnImage;

        var colors = button.colors;
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        button.colors = colors;

        var textObj = CreateUIElement("Text", btnObj);
        SetFullStretch(textObj);
        var btnText = textObj.gameObject.AddComponent<TextMeshProUGUI>();
        btnText.text = text;
        btnText.fontSize = 24;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
    }

    private enum AnchorPreset
    {
        TopLeft, TopCenter, TopRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        BottomLeft, BottomCenter, BottomRight
    }

    private static void SetAnchor(RectTransform rect, AnchorPreset preset)
    {
        Vector2 anchor = Vector2.zero;
        Vector2 pivot = new Vector2(0.5f, 0.5f);

        switch (preset)
        {
            case AnchorPreset.TopLeft:
                anchor = new Vector2(0, 1); pivot = new Vector2(0, 1); break;
            case AnchorPreset.TopCenter:
                anchor = new Vector2(0.5f, 1); pivot = new Vector2(0.5f, 1); break;
            case AnchorPreset.TopRight:
                anchor = new Vector2(1, 1); pivot = new Vector2(1, 1); break;
            case AnchorPreset.MiddleLeft:
                anchor = new Vector2(0, 0.5f); pivot = new Vector2(0, 0.5f); break;
            case AnchorPreset.MiddleCenter:
                anchor = new Vector2(0.5f, 0.5f); pivot = new Vector2(0.5f, 0.5f); break;
            case AnchorPreset.MiddleRight:
                anchor = new Vector2(1, 0.5f); pivot = new Vector2(1, 0.5f); break;
            case AnchorPreset.BottomLeft:
                anchor = new Vector2(0, 0); pivot = new Vector2(0, 0); break;
            case AnchorPreset.BottomCenter:
                anchor = new Vector2(0.5f, 0); pivot = new Vector2(0.5f, 0); break;
            case AnchorPreset.BottomRight:
                anchor = new Vector2(1, 0); pivot = new Vector2(1, 0); break;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
    }

    private static void SetFullStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SaveAsPrefab(GameObject obj, string prefabName)
    {
        // 폴더 생성 확인
        if (!AssetDatabase.IsValidFolder(PREFABS_PATH))
        {
            string parentFolder = Path.GetDirectoryName(PREFABS_PATH);
            string newFolderName = Path.GetFileName(PREFABS_PATH);
            AssetDatabase.CreateFolder(parentFolder, newFolderName);
        }

        string prefabPath = $"{PREFABS_PATH}/{prefabName}.prefab";
        
        // 기존 프리팹 확인
        var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null)
        {
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            Debug.Log($"[UI Setup] 프리팹 업데이트: {prefabPath}");
        }
        else
        {
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            Debug.Log($"[UI Setup] 프리팹 생성: {prefabPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    #endregion
}
