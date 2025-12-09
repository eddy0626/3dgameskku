using UnityEngine;
using DG.Tweening;

/// <summary>
/// FPS/TPS 카메라 전환 시스템
/// T키를 눌러 시점 전환, DOTween으로 부드러운 애니메이션
/// </summary>
public class CameraViewSwitcher : MonoBehaviour
{
    public enum CameraView
    {
        FirstPerson,
        ThirdPerson
    }

    [Header("Camera Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform cameraTransform;

    [Header("FPS Position (Local)")]
    [SerializeField] private Vector3 fpsPosition = new Vector3(0f, 0.58f, 0.08f);
    [SerializeField] private Vector3 fpsRotation = Vector3.zero;

    [Header("TPS Position (Local)")]
    [SerializeField] private Vector3 tpsPosition = new Vector3(0.5f, 0.8f, -2.5f);
    [SerializeField] private Vector3 tpsRotation = new Vector3(10f, 0f, 0f);

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private Ease transitionEase = Ease.InOutQuad;

    [Header("Gun Holder (Optional)")]
    [SerializeField] private Transform gunHolder;
    [SerializeField] private Vector3 fpsGunPosition = new Vector3(0.3f, -0.3f, 0.5f);
    [SerializeField] private Vector3 tpsGunPosition = new Vector3(0.4f, -0.2f, 0.3f);
    [SerializeField] private Vector3 tpsGunScale = new Vector3(0.7f, 0.7f, 0.7f);

    [Header("FOV Settings")]
    [SerializeField] private float fpsFOV = 60f;
    [SerializeField] private float tpsFOV = 70f;

    public CameraView CurrentView { get; private set; } = CameraView.FirstPerson;
    public bool IsTransitioning { get; private set; }

    private Camera mainCamera;
    private Tweener positionTween;
    private Tweener rotationTween;
    private Tweener fovTween;
    private Tweener gunPositionTween;
    private Tweener gunScaleTween;

    
    private CameraRotate cameraRotate;
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
            ? CameraView.ThirdPerson
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
        Vector3 targetGunPosition;
        Vector3 targetGunScale;

        if (targetView == CameraView.FirstPerson)
        {
            targetPosition = fpsPosition;
            targetRotation = fpsRotation;
            targetFOV = fpsFOV;
            targetGunPosition = fpsGunPosition;
            targetGunScale = originalGunScale;
        }
        else
        {
            targetPosition = tpsPosition;
            targetRotation = tpsRotation;
            targetFOV = tpsFOV;
            targetGunPosition = tpsGunPosition;
            targetGunScale = Vector3.Scale(originalGunScale, tpsGunScale);
        }

        // 카메라 위치 전환
        positionTween = cameraTransform
            .DOLocalMove(targetPosition, transitionDuration)
            .SetEase(transitionEase);

        // 카메라 로컬 회전 전환 (수직 각도만)
        rotationTween = cameraTransform
            .DOLocalRotate(targetRotation, transitionDuration)
            .SetEase(transitionEase);

        // FOV 전환
        if (mainCamera != null)
        {
            fovTween = mainCamera
                .DOFieldOfView(targetFOV, transitionDuration)
                .SetEase(transitionEase);
        }

        // Gun Holder 전환
        if (gunHolder != null)
        {
            gunPositionTween = gunHolder
                .DOLocalMove(targetGunPosition, transitionDuration)
                .SetEase(transitionEase);

            gunScaleTween = gunHolder
                .DOScale(targetGunScale, transitionDuration)
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
                gunHolder.localPosition = fpsGunPosition;
                gunHolder.localScale = originalGunScale;
            }
        }
        else
        {
            cameraTransform.localPosition = tpsPosition;
            cameraTransform.localRotation = Quaternion.Euler(tpsRotation);

            if (mainCamera != null)
            {
                mainCamera.fieldOfView = tpsFOV;
            }

            if (gunHolder != null)
            {
                gunHolder.localPosition = tpsGunPosition;
                gunHolder.localScale = Vector3.Scale(originalGunScale, tpsGunScale);
            }
        }

        CurrentView = view;
        IsTransitioning = false;
    }

    /// <summary>
    /// 시점 변경 완료 시 호출되는 콜백
    /// </summary>
protected virtual void OnViewChanged(CameraView newView)
    {
        // 회전값 동기화
        if (cameraRotate != null)
        {
            cameraRotate.SyncRotation();
        }

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
    /// 현재 시점이 TPS인지 확인
    /// </summary>
    public bool IsThirdPerson()
    {
        return CurrentView == CameraView.ThirdPerson;
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

        // TPS 위치 시각화 (초록색)
        Gizmos.color = Color.green;
        Vector3 tpsWorldPos = parent.TransformPoint(tpsPosition);
        Gizmos.DrawWireSphere(tpsWorldPos, 0.1f);
        Gizmos.DrawLine(parent.position, tpsWorldPos);
    }
#endif
}
