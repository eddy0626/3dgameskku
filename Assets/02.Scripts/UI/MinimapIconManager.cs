using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니맵 아이콘 관리 매니저
/// 적, 아이템 등의 미니맵 아이콘을 자동 생성 및 관리
/// </summary>
public class MinimapIconManager : MonoBehaviour
{
    public static MinimapIconManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RectTransform _minimapContainer;
    [SerializeField] private Camera _minimapCamera;

    [Header("Icon Sprites")]
    [SerializeField] private Sprite _playerIconSprite;
    [SerializeField] private Sprite _enemyIconSprite;
    [SerializeField] private Sprite _itemIconSprite;
    [SerializeField] private Sprite _objectiveIconSprite;

    [Header("Icon Settings")]
    [SerializeField] private float _playerIconSize = 30f;
    [SerializeField] private float _enemyIconSize = 20f;
    [SerializeField] private Color _playerIconColor = Color.cyan;
    [SerializeField] private Color _enemyIconColor = Color.red;
    [SerializeField] private Color _itemIconColor = Color.yellow;

    [Header("Auto Detection")]
    [SerializeField] private bool _autoDetectEnemies = true;
    [SerializeField] private float _detectionInterval = 1f;
    [SerializeField] private string _enemyTag = "Enemy";

    private Dictionary<Transform, MinimapIconData> _trackedIcons = new Dictionary<Transform, MinimapIconData>();
    private Transform _playerTransform;
    private RectTransform _playerIconRect;
    private float _lastDetectionTime;

    private class MinimapIconData
    {
        public RectTransform RectTransform;
        public Image IconImage;
        public CanvasGroup CanvasGroup;
        public MinimapIcon.IconType IconType;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

private void Start()
    {
        FindReferences();
        LoadIconSprites();
        CreatePlayerIcon();
        
        if (_autoDetectEnemies)
        {
            DetectAndRegisterEnemies();
        }
    }


private void LoadIconSprites()
    {
        #if UNITY_EDITOR
        // 적 아이콘 스프라이트 - 항상 minimap_enemy.png 강제 로드
        Object[] enemyAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/04.Images/minimap_enemy.png");
        foreach (Object asset in enemyAssets)
        {
            if (asset is Sprite sprite)
            {
                _enemyIconSprite = sprite;
                Debug.Log($"[MinimapIconManager] Force loaded enemy sprite: {sprite.name}");
                break;
            }
        }
        
        if (_enemyIconSprite == null)
        {
            Debug.LogWarning("[MinimapIconManager] Failed to load minimap_enemy.png sprite!");
        }
        else
        {
            Debug.Log($"[MinimapIconManager] Enemy sprite ready: {_enemyIconSprite.name}, Color: {_enemyIconColor}");
        }
        #else
        Debug.Log("[MinimapIconManager] Build mode - using inspector sprite settings");
        #endif
    }


private void FindReferences()
    {
        Debug.Log("[MinimapIconManager] FindReferences started...");
        
        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
            Debug.Log($"[MinimapIconManager] Found Player: {player.name}");
        }
        else
        {
            Debug.LogWarning("[MinimapIconManager] WARNING: Player not found!");
        }

        // 미니맵 컨테이너 찾기 (MinimapDisplay를 컨테이너로 사용)
        if (_minimapContainer == null)
        {
            GameObject displayObj = GameObject.Find("MinimapDisplay");
            if (displayObj != null)
            {
                _minimapContainer = displayObj.GetComponent<RectTransform>();
                Debug.Log("[MinimapIconManager] Found MinimapContainer (MinimapDisplay)");
            }
            else
            {
                Debug.LogWarning("[MinimapIconManager] WARNING: MinimapDisplay not found for container!");
            }
        }

        // 미니맵 카메라 찾기
        if (_minimapCamera == null)
        {
            // MinimapSystem.Instance가 아직 없을 수 있으므로 직접 찾기
            GameObject minimapSystemObj = GameObject.Find("MinimapSystem");
            if (minimapSystemObj != null)
            {
                _minimapCamera = minimapSystemObj.GetComponentInChildren<Camera>();
                Debug.Log(_minimapCamera != null 
                    ? $"[MinimapIconManager] Found MinimapCamera: {_minimapCamera.name}" 
                    : "[MinimapIconManager] WARNING: MinimapCamera not found in MinimapSystem!");
            }
            else
            {
                Debug.LogWarning("[MinimapIconManager] WARNING: MinimapSystem GameObject not found!");
            }
        }
        
        Debug.Log("[MinimapIconManager] FindReferences completed.");
    }

    private void CreatePlayerIcon()
    {
        if (_minimapContainer == null || _playerTransform == null)
        {
            return;
        }

        GameObject iconObj = new GameObject("PlayerIcon");
        iconObj.transform.SetParent(_minimapContainer, false);

        _playerIconRect = iconObj.AddComponent<RectTransform>();
        _playerIconRect.sizeDelta = new Vector2(_playerIconSize, _playerIconSize);
        _playerIconRect.anchoredPosition = Vector2.zero;

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.sprite = _playerIconSprite;
        iconImage.color = _playerIconColor;
        iconImage.raycastTarget = false;

        // 플레이어 아이콘은 항상 중앙에 (미니맵이 플레이어 따라다니므로)
    }

    private void LateUpdate()
    {
        UpdateAllIcons();
        
        if (_autoDetectEnemies && Time.time - _lastDetectionTime > _detectionInterval)
        {
            DetectAndRegisterEnemies();
            _lastDetectionTime = Time.time;
        }

        CleanupDestroyedTargets();
    }

private void DetectAndRegisterEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(_enemyTag);
        Debug.Log($"[MinimapIconManager] DetectAndRegisterEnemies - Found {enemies.Length} enemies with tag '{_enemyTag}'");
        
        foreach (GameObject enemy in enemies)
        {
            if (!_trackedIcons.ContainsKey(enemy.transform))
            {
                RegisterIcon(enemy.transform, MinimapIcon.IconType.Enemy);
                Debug.Log($"[MinimapIconManager] Registered enemy icon: {enemy.name}");
            }
        }
    }

    public void RegisterIcon(Transform target, MinimapIcon.IconType iconType, Sprite customSprite = null, Color? customColor = null)
    {
        if (target == null || _minimapContainer == null || _trackedIcons.ContainsKey(target))
        {
            return;
        }

        GameObject iconObj = new GameObject($"MinimapIcon_{target.name}");
        iconObj.transform.SetParent(_minimapContainer, false);

        RectTransform rectTransform = iconObj.AddComponent<RectTransform>();
        Image iconImage = iconObj.AddComponent<Image>();
        CanvasGroup canvasGroup = iconObj.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // 아이콘 타입별 설정
        switch (iconType)
        {
            case MinimapIcon.IconType.Enemy:
                iconImage.sprite = customSprite ?? _enemyIconSprite;
                iconImage.color = customColor ?? _enemyIconColor;
                rectTransform.sizeDelta = new Vector2(_enemyIconSize, _enemyIconSize);
                break;
            case MinimapIcon.IconType.Item:
                iconImage.sprite = customSprite ?? _itemIconSprite;
                iconImage.color = customColor ?? _itemIconColor;
                rectTransform.sizeDelta = new Vector2(_enemyIconSize, _enemyIconSize);
                break;
            case MinimapIcon.IconType.Objective:
                iconImage.sprite = customSprite ?? _objectiveIconSprite;
                iconImage.color = customColor ?? Color.green;
                rectTransform.sizeDelta = new Vector2(_enemyIconSize * 1.5f, _enemyIconSize * 1.5f);
                break;
            default:
                iconImage.sprite = customSprite;
                iconImage.color = customColor ?? Color.white;
                rectTransform.sizeDelta = new Vector2(_enemyIconSize, _enemyIconSize);
                break;
        }

        iconImage.raycastTarget = false;

        MinimapIconData iconData = new MinimapIconData
        {
            RectTransform = rectTransform,
            IconImage = iconImage,
            CanvasGroup = canvasGroup,
            IconType = iconType
        };

        _trackedIcons.Add(target, iconData);
    }

    public void UnregisterIcon(Transform target)
    {
        if (target == null || !_trackedIcons.ContainsKey(target))
        {
            return;
        }

        MinimapIconData iconData = _trackedIcons[target];
        if (iconData.RectTransform != null)
        {
            Destroy(iconData.RectTransform.gameObject);
        }

        _trackedIcons.Remove(target);
    }

    private void UpdateAllIcons()
    {
        if (_minimapCamera == null || _minimapContainer == null)
        {
            return;
        }

        float minimapWidth = _minimapContainer.rect.width;
        float minimapHeight = _minimapContainer.rect.height;

        foreach (var kvp in _trackedIcons)
        {
            Transform target = kvp.Key;
            MinimapIconData iconData = kvp.Value;

            if (target == null || iconData.RectTransform == null)
            {
                continue;
            }

            // 월드 좌표를 뷰포트 좌표로 변환
            Vector3 viewportPos = _minimapCamera.WorldToViewportPoint(target.position);

            // 뷰포트 좌표를 미니맵 UI 좌표로 변환
            float x = (viewportPos.x - 0.5f) * minimapWidth;
            float y = (viewportPos.y - 0.5f) * minimapHeight;

            iconData.RectTransform.anchoredPosition = new Vector2(x, y);

            // 미니맵 범위 체크
            bool isInView = viewportPos.x >= 0f && viewportPos.x <= 1f &&
                            viewportPos.y >= 0f && viewportPos.y <= 1f &&
                            viewportPos.z > 0f;

            iconData.RectTransform.gameObject.SetActive(isInView);

            // 회전 업데이트 (적의 바라보는 방향)
            float rotation = -target.eulerAngles.y + _minimapCamera.transform.eulerAngles.y;
            iconData.RectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }
    }

    private void CleanupDestroyedTargets()
    {
        List<Transform> toRemove = new List<Transform>();

        foreach (var kvp in _trackedIcons)
        {
            if (kvp.Key == null)
            {
                toRemove.Add(kvp.Key);
                if (kvp.Value.RectTransform != null)
                {
                    Destroy(kvp.Value.RectTransform.gameObject);
                }
            }
        }

        foreach (Transform target in toRemove)
        {
            _trackedIcons.Remove(target);
        }
    }

    public void SetMinimapContainer(RectTransform container)
    {
        _minimapContainer = container;
    }

    public void SetMinimapCamera(Camera cam)
    {
        _minimapCamera = cam;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
