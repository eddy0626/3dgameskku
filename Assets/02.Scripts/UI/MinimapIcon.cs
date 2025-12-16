using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니맵에 표시될 아이콘 (적, 아이템 등)
/// 월드 위치를 미니맵 UI 좌표로 변환
/// </summary>
public class MinimapIcon : MonoBehaviour
{
    public enum IconType
    {
        Enemy,
        Item,
        Objective,
        Custom
    }

    [Header("Icon Settings")]
    [SerializeField] private IconType _iconType = IconType.Enemy;
    [SerializeField] private Sprite _iconSprite;
    [SerializeField] private Color _iconColor = Color.red;
    [SerializeField] private float _iconSize = 20f;
    [SerializeField] private bool _rotateWithTarget = true;

    [Header("Visibility")]
    [SerializeField] private float _visibleRange = 50f;
    [SerializeField] private bool _alwaysVisible = false;
    [SerializeField] private bool _fadeWithDistance = true;

    private RectTransform _iconRectTransform;
    private Image _iconImage;
    private CanvasGroup _canvasGroup;
    private Transform _playerTransform;
    private Camera _minimapCamera;
    private RectTransform _minimapRect;
    private bool _isInitialized;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }

        // MinimapSystem에서 카메라 참조
        if (MinimapSystem.Instance != null)
        {
            _minimapCamera = MinimapSystem.Instance.GetComponentInChildren<Camera>();
        }

        _isInitialized = true;
    }

    public void SetupIcon(RectTransform minimapContainer, Sprite sprite = null)
    {
        // 아이콘 GameObject 생성
        GameObject iconObj = new GameObject($"MinimapIcon_{gameObject.name}");
        iconObj.transform.SetParent(minimapContainer, false);

        _iconRectTransform = iconObj.AddComponent<RectTransform>();
        _iconRectTransform.sizeDelta = new Vector2(_iconSize, _iconSize);

        _iconImage = iconObj.AddComponent<Image>();
        _iconImage.sprite = sprite ?? _iconSprite;
        _iconImage.color = _iconColor;
        _iconImage.raycastTarget = false;

        _canvasGroup = iconObj.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
    }

    private void LateUpdate()
    {
        if (!_isInitialized || _iconRectTransform == null)
        {
            return;
        }

        UpdateIconPosition();
        UpdateIconVisibility();
        UpdateIconRotation();
    }

    private void UpdateIconPosition()
    {
        if (_minimapCamera == null || _minimapRect == null)
        {
            return;
        }

        // 월드 좌표를 미니맵 뷰포트 좌표로 변환
        Vector3 worldPos = transform.position;
        Vector3 viewportPos = _minimapCamera.WorldToViewportPoint(worldPos);

        // 뷰포트 좌표를 미니맵 UI 좌표로 변환
        float minimapWidth = _minimapRect.rect.width;
        float minimapHeight = _minimapRect.rect.height;

        float x = (viewportPos.x - 0.5f) * minimapWidth;
        float y = (viewportPos.y - 0.5f) * minimapHeight;

        _iconRectTransform.anchoredPosition = new Vector2(x, y);

        // 미니맵 범위 밖이면 클램프하거나 숨김
        bool isInView = viewportPos.x >= 0f && viewportPos.x <= 1f &&
                        viewportPos.y >= 0f && viewportPos.y <= 1f &&
                        viewportPos.z > 0f;

        _iconRectTransform.gameObject.SetActive(isInView || _alwaysVisible);
    }

    private void UpdateIconVisibility()
    {
        if (_playerTransform == null || _canvasGroup == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, _playerTransform.position);

        if (_alwaysVisible)
        {
            _canvasGroup.alpha = 1f;
            return;
        }

        if (distance > _visibleRange)
        {
            _canvasGroup.alpha = 0f;
            return;
        }

        if (_fadeWithDistance)
        {
            float fadeStart = _visibleRange * 0.7f;
            if (distance > fadeStart)
            {
                float fadeProgress = (distance - fadeStart) / (_visibleRange - fadeStart);
                _canvasGroup.alpha = 1f - fadeProgress;
            }
            else
            {
                _canvasGroup.alpha = 1f;
            }
        }
        else
        {
            _canvasGroup.alpha = 1f;
        }
    }

    private void UpdateIconRotation()
    {
        if (!_rotateWithTarget || _iconRectTransform == null)
        {
            return;
        }

        // 타겟의 Y축 회전을 아이콘에 적용 (미니맵 카메라 회전 보정)
        float targetRotation = transform.eulerAngles.y;
        
        if (_minimapCamera != null)
        {
            targetRotation -= _minimapCamera.transform.eulerAngles.y;
        }

        _iconRectTransform.localRotation = Quaternion.Euler(0f, 0f, -targetRotation);
    }

    public void SetMinimapRect(RectTransform rect)
    {
        _minimapRect = rect;
    }

    public void SetMinimapCamera(Camera cam)
    {
        _minimapCamera = cam;
    }

    private void OnDestroy()
    {
        if (_iconRectTransform != null)
        {
            Destroy(_iconRectTransform.gameObject);
        }
    }

    private void OnDisable()
    {
        if (_iconRectTransform != null)
        {
            _iconRectTransform.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (_iconRectTransform != null)
        {
            _iconRectTransform.gameObject.SetActive(true);
        }
    }
}
