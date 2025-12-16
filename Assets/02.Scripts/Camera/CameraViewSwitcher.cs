using UnityEngine;
using DG.Tweening;

/// <summary>
/// FPS/TopView 카메라 전환 시스템
/// T키를 눌러 시점 전환, DOTween으로 부드러운 애니메이션
/// </summary>
public class CameraViewSwitcher : MonoBehaviour
{
    public enum CameraView
    {
        FirstPerson,
        TopView
    }

    [Header("Camera Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform cameraTransform;

    [Header("FPS Position (Local)")]
    [SerializeField] private Vector3 fpsPosition = new Vector3(0f, 0.58f, 0.08f);
    [SerializeField] private Vector3 fpsRotation = Vector3.zero;

    [Header("Top View Position (Local)")]
    [SerializeField] private Vector3 topViewPosition = new Vector3(0f, 12f, 0f);
    [SerializeField] private Vector3 topViewRotation = new Vector3(90f, 0f, 0f);

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private Ease transitionEase = Ease.InOutQuad;

    [Header("Gun Holder (Optional)")]
    [SerializeField] private Transform gunHolder;
    [SerializeField] private Vector3 fpsGunPosition = new Vector3(0.3f, -0.3f, 0.5f);
    [SerializeField] private bool hideGunInTopView = true;

    [Header("FOV Settings")]
    [SerializeField] private float fpsFOV = 60f;
    [SerializeField] private float topViewFOV = 60f;

    public CameraView CurrentView { get; private set; } = CameraView.FirstPerson;
    public bool IsTransitioning { get; private set; }

    private Camera mainCamera;
    private Tweener positionTween;
    private Tweener rotationTween;
    private Tweener fovTween;
    private Tweener gunPositionTween;
    private Tweener gunScaleTween;

    private CameraRotate cameraRotate;
    private CameraZoom cameraZoom;
    private Vector3 originalGunScale;

    private void Awake()
    {
        // 자동 참조 설정
        if (playerTransform == null)
        {
            playerTransform = transform;
        }

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
            if (cameraTransform == null)
            {
                cameraTransform = GetComponentInChildren<Camera>()?.transform;
            }
        }

        mainCamera = cameraTransform?.GetComponent<Camera>();
        cameraRotate = cameraTransform?.GetComponent<CameraRotate>();
        cameraZoom = cameraTransform?.GetComponent<CameraZoom>();

        // GunHolder 자동 탐색
        if (gunHolder == null && cameraTransform != null)
        {
            gunHolder = cameraTransform.Find("GunHolder");
        }

        if (gunHolder != null)
        {
            originalGunScale = gunHolder.localScale;
        }
        else
        {
            originalGunScale = Vector3.one;
        }
    }

    private void Start()
    {
        // 초기 위치 설정
        ApplyViewImmediate(CurrentView);
    }

    private void Update()
    {
        // T키로 전환
        if (Input.GetKeyDown(KeyCode.T) && !IsTransitioning)
        {
            ToggleView();
        }
    }

    /// <summary>
    /// 카메라 시점 토글
    /// </summary>
    public void ToggleView()
    {
        CameraView targetView = CurrentView == CameraView.FirstPerson
            ? CameraView.TopView
            : CameraView.FirstPerson;

        SwitchToView(targetView);
    }

    /// <summary>
    /// 특정 시점으로 전환
    /// </summary>
    public void SwitchToView(CameraView targetView)
    {
        if (IsTransitioning || targetView == CurrentView)
        {
            return;
        }

        KillAllTweens();
        IsTransitioning = true;

        Vector3 targetPosition;
        Vector3 targetRotation;
        float targetFOV;

        if (targetView == CameraView.FirstPerson)
        {
            targetPosition = fpsPosition;
            targetRotation = fpsRotation;
            targetFOV = fpsFOV;

            // GunHolder 표시
            if (gunHolder != null)
            {
                gunHolder.gameObject.SetActive(true);
                gunPositionTween = gunHolder
                    .DOLocalMove(fpsGunPosition, transitionDuration)
                    .SetEase(transitionEase);
                gunScaleTween = gunHolder
                    .DOScale(originalGunScale, transitionDuration)
                    .SetEase(transitionEase);
            }

            // 마우스 회전 활성화
            if (cameraRotate != null)
            {
                cameraRotate.enabled = true;
            }

            // FPS에서 커서 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else // TopView
        {
            targetPosition = topViewPosition;
            targetRotation = topViewRotation;
            targetFOV = topViewFOV;

            // GunHolder 숨기기
            if (gunHolder != null && hideGunInTopView)
            {
                gunScaleTween = gunHolder
                    .DOScale(Vector3.zero, transitionDuration * 0.5f)
                    .SetEase(transitionEase)
                    .OnComplete(() => gunHolder.gameObject.SetActive(false));
            }

            // 탑뷰에서는 마우스 회전 비활성화
            if (cameraRotate != null)
            {
                cameraRotate.enabled = false;
            }

            // 탑뷰에서 커서 활성화
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 카메라 위치 전환
        positionTween = cameraTransform
            .DOLocalMove(targetPosition, transitionDuration)
            .SetEase(transitionEase);

        // 카메라 회전 전환
        if (targetView == CameraView.TopView)
        {
            // 탑뷰: 월드 회전 사용 (플레이어 회전과 무관하게 항상 아래를 봄)
            rotationTween = cameraTransform
                .DORotate(targetRotation, transitionDuration)
                .SetEase(transitionEase);
        }
        else
        {
            // FPS: 로컬 회전 사용
            rotationTween = cameraTransform
                .DOLocalRotate(targetRotation, transitionDuration)
                .SetEase(transitionEase);
        }

        // FOV 전환
        if (mainCamera != null)
        {
            fovTween = mainCamera
                .DOFieldOfView(targetFOV, transitionDuration)
                .SetEase(transitionEase);
        }

        // 전환 완료 콜백
        positionTween.OnComplete(() =>
        {
            CurrentView = targetView;
            IsTransitioning = false;
            OnViewChanged(targetView);
        });
    }

    /// <summary>
    /// 즉시 시점 적용 (전환 애니메이션 없음)
    /// </summary>
    public void ApplyViewImmediate(CameraView view)
    {
        KillAllTweens();

        if (view == CameraView.FirstPerson)
        {
            cameraTransform.localPosition = fpsPosition;
            cameraTransform.localRotation = Quaternion.Euler(fpsRotation);

            if (mainCamera != null)
            {
                mainCamera.fieldOfView = fpsFOV;
            }

            if (gunHolder != null)
            {
                gunHolder.gameObject.SetActive(true);
                gunHolder.localPosition = fpsGunPosition;
                gunHolder.localScale = originalGunScale;
            }

            if (cameraRotate != null)
            {
                cameraRotate.enabled = true;
            }

            // FPS에서 커서 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else // TopView
        {
            cameraTransform.localPosition = topViewPosition;
            // 탑뷰: 월드 회전 사용 (플레이어 회전과 무관하게 항상 아래를 봄)
            cameraTransform.rotation = Quaternion.Euler(topViewRotation);

            if (mainCamera != null)
            {
                mainCamera.fieldOfView = topViewFOV;
            }

            if (gunHolder != null && hideGunInTopView)
            {
                gunHolder.gameObject.SetActive(false);
            }

            if (cameraRotate != null)
            {
                cameraRotate.enabled = false;
            }

            // 탑뷰에서 커서 활성화
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        CurrentView = view;
        IsTransitioning = false;
    }

    /// <summary>
    /// 시점 변경 완료 시 호출되는 콜백
    /// </summary>
    protected virtual void OnViewChanged(CameraView newView)
    {
        // 회전값 동기화 (FPS로 돌아올 때만)
        if (newView == CameraView.FirstPerson && cameraRotate != null)
        {
            cameraRotate.SyncRotation();
        }

        // 줌 상태 리셋 및 FOV 업데이트
        cameraZoom?.OnViewSwitched();

        Debug.Log($"Camera view changed to: {newView}");
    }

    /// <summary>
    /// 현재 시점이 FPS인지 확인
    /// </summary>
    public bool IsFirstPerson()
    {
        return CurrentView == CameraView.FirstPerson;
    }

    /// <summary>
    /// 현재 시점이 TopView인지 확인
    /// </summary>
    public bool IsTopView()
    {
        return CurrentView == CameraView.TopView;
    }

    /// <summary>
    /// 현재 시점이 3인칭(TopView)인지 확인 (호환성용)
    /// </summary>
    public bool IsThirdPerson()
    {
        return CurrentView == CameraView.TopView;
    }

    private void KillAllTweens()
    {
        positionTween?.Kill();
        rotationTween?.Kill();
        fovTween?.Kill();
        gunPositionTween?.Kill();
        gunScaleTween?.Kill();
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform camTrans = cameraTransform != null ? cameraTransform : transform;
        Transform parent = camTrans.parent;

        if (parent == null)
        {
            return;
        }

        // FPS 위치 시각화 (파란색)
        Gizmos.color = Color.blue;
        Vector3 fpsWorldPos = parent.TransformPoint(fpsPosition);
        Gizmos.DrawWireSphere(fpsWorldPos, 0.1f);
        Gizmos.DrawLine(parent.position, fpsWorldPos);

        // TopView 위치 시각화 (노란색)
        Gizmos.color = Color.yellow;
        Vector3 topViewWorldPos = parent.TransformPoint(topViewPosition);
        Gizmos.DrawWireSphere(topViewWorldPos, 0.3f);
        Gizmos.DrawLine(parent.position, topViewWorldPos);
        
        // TopView 카메라 방향 시각화
        Vector3 topViewDir = Quaternion.Euler(topViewRotation) * Vector3.forward;
        Gizmos.DrawRay(topViewWorldPos, topViewDir * 2f);
    }
#endif
}
