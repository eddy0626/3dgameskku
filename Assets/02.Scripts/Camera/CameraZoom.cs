using UnityEngine;
using DG.Tweening;
using System;

/// <summary>
/// 카메라 줌 시스템 (마우스 우클릭 토글 방식)
/// DOTween을 이용한 부드러운 FOV 전환
/// FPS/TPS 모드 연동 지원
/// </summary>
public class CameraZoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomedFOV = 30f;
    [SerializeField] private float zoomDuration = 0.2f;
    [SerializeField] private Ease zoomEase = Ease.OutQuad;

    [Header("TPS Mode Settings")]
    [SerializeField] private float tpsNormalFOV = 70f;
    [SerializeField] private float tpsZoomedFOV = 40f;

    [Header("Sensitivity Adjustment")]
    [Tooltip("줌 상태에서의 마우스 감도 배율")]
    [SerializeField] private float zoomSensitivityMultiplier = 0.5f;

    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private CameraViewSwitcher viewSwitcher;
    [SerializeField] private CameraRotate cameraRotate;

    // 줌 상태
    public bool IsZoomed { get; private set; }
    public bool IsTransitioning { get; private set; }

    // 이벤트
    public event Action<bool> OnZoomChanged;

    // 내부 변수
    private Tweener _fovTween;
    private float _originalSensitivity;
    private float _currentNormalFOV;
    private float _currentZoomedFOV;

    private void Awake()
    {
        // 자동 참조 설정
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }
        }

        if (viewSwitcher == null)
        {
            viewSwitcher = GetComponentInParent<CameraViewSwitcher>();
            if (viewSwitcher == null)
            {
                // Player 오브젝트에서 찾기
                Transform parent = transform.parent;
                if (parent != null)
                {
                    viewSwitcher = parent.GetComponent<CameraViewSwitcher>();
                }
            }
        }

        if (cameraRotate == null)
        {
            cameraRotate = GetComponent<CameraRotate>();
        }

        UpdateFOVForCurrentMode();
    }

    private void Start()
    {
        // 초기 FOV 설정
        if (targetCamera != null)
        {
            targetCamera.fieldOfView = _currentNormalFOV;
        }
    }

    private void Update()
    {
        // 시점 전환 중이면 줌 입력 무시
        if (viewSwitcher != null && viewSwitcher.IsTransitioning)
        {
            return;
        }

        // 탑뷰일 때는 줌 비활성화
        if (viewSwitcher != null && viewSwitcher.IsTopView())
        {
            return;
        }

        // 마우스 우클릭 토글
        if (Input.GetMouseButtonDown(1))
        {
            ToggleZoom();
        }
    }

    /// <summary>
    /// 줌 상태 토글
    /// </summary>
    public void ToggleZoom()
    {
        if (IsTransitioning)
        {
            return;
        }

        if (IsZoomed)
        {
            ZoomOut();
        }
        else
        {
            ZoomIn();
        }
    }

    /// <summary>
    /// 줌인
    /// </summary>
    public void ZoomIn()
    {
        if (IsZoomed || IsTransitioning || targetCamera == null)
        {
            return;
        }

        UpdateFOVForCurrentMode();
        StartZoomTransition(_currentZoomedFOV, true);
    }

    /// <summary>
    /// 줌아웃
    /// </summary>
    public void ZoomOut()
    {
        if (!IsZoomed || IsTransitioning || targetCamera == null)
        {
            return;
        }

        UpdateFOVForCurrentMode();
        StartZoomTransition(_currentNormalFOV, false);
    }

    /// <summary>
    /// 줌 전환 시작
    /// </summary>
    private void StartZoomTransition(float targetFOV, bool zoomingIn)
    {
        KillTween();
        IsTransitioning = true;

        _fovTween = targetCamera
            .DOFieldOfView(targetFOV, zoomDuration)
            .SetEase(zoomEase)
            .OnComplete(() =>
            {
                IsTransitioning = false;
                IsZoomed = zoomingIn;
                OnZoomChanged?.Invoke(IsZoomed);

                // 디버그 로그
                Debug.Log($"[CameraZoom] Zoom {(IsZoomed ? "In" : "Out")} - FOV: {targetFOV}");
            });
    }

    /// <summary>
    /// 현재 카메라 모드(FPS/TPS)에 따른 FOV 값 업데이트
    /// </summary>
    private void UpdateFOVForCurrentMode()
    {
        bool isThirdPerson = viewSwitcher != null && viewSwitcher.IsThirdPerson();

        if (isThirdPerson)
        {
            _currentNormalFOV = tpsNormalFOV;
            _currentZoomedFOV = tpsZoomedFOV;
        }
        else
        {
            _currentNormalFOV = normalFOV;
            _currentZoomedFOV = zoomedFOV;
        }
    }

    /// <summary>
    /// 시점 전환 시 호출 (CameraViewSwitcher에서 호출)
    /// </summary>
    public void OnViewSwitched()
    {
        // 줌 상태 해제
        if (IsZoomed)
        {
            ForceZoomOut();
        }

        UpdateFOVForCurrentMode();
    }

    /// <summary>
    /// 즉시 줌 아웃 (애니메이션 없이)
    /// </summary>
    public void ForceZoomOut()
    {
        KillTween();

        UpdateFOVForCurrentMode();

        if (targetCamera != null)
        {
            targetCamera.fieldOfView = _currentNormalFOV;
        }

        IsZoomed = false;
        IsTransitioning = false;
        OnZoomChanged?.Invoke(false);
    }

    /// <summary>
    /// 줌 배율 반환 (0 = 일반, 1 = 최대 줌)
    /// </summary>
    public float GetZoomRatio()
    {
        if (targetCamera == null)
        {
            return 0f;
        }

        float currentFOV = targetCamera.fieldOfView;
        return Mathf.InverseLerp(_currentNormalFOV, _currentZoomedFOV, currentFOV);
    }

    /// <summary>
    /// 현재 줌 상태에 따른 감도 배율 반환
    /// </summary>
    public float GetSensitivityMultiplier()
    {
        if (!IsZoomed)
        {
            return 1f;
        }

        return zoomSensitivityMultiplier;
    }

    /// <summary>
    /// 줌 FOV 설정 변경
    /// </summary>
    public void SetZoomFOV(float newZoomedFOV)
    {
        zoomedFOV = newZoomedFOV;
        UpdateFOVForCurrentMode();
    }

    /// <summary>
    /// 일반 FOV 설정 변경
    /// </summary>
    public void SetNormalFOV(float newNormalFOV)
    {
        normalFOV = newNormalFOV;
        UpdateFOVForCurrentMode();
    }

    private void KillTween()
    {
        _fovTween?.Kill();
        _fovTween = null;
    }

    private void OnDestroy()
    {
        KillTween();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Inspector에서 값 변경 시 유효성 검사
        zoomedFOV = Mathf.Clamp(zoomedFOV, 10f, normalFOV - 5f);
        tpsZoomedFOV = Mathf.Clamp(tpsZoomedFOV, 10f, tpsNormalFOV - 5f);
        zoomDuration = Mathf.Max(0.05f, zoomDuration);
        zoomSensitivityMultiplier = Mathf.Clamp(zoomSensitivityMultiplier, 0.1f, 1f);
    }
#endif
}
