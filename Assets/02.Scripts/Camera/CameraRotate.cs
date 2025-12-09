using UnityEngine;

/// <summary>
/// 카메라 회전 제어 (FPS/TPS 모드 지원)
/// FPS: 카메라만 회전
/// TPS: 플레이어 몸체(Y축) + 카메라(X축) 분리 회전
/// </summary>
public class CameraRotate : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;

    [Header("References")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private CameraViewSwitcher viewSwitcher;

    [Header("TPS Settings")]
    [SerializeField] private float tpsVerticalOffset = 10f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    private void Awake()
    {
        // 자동 참조 설정
        if (playerBody == null)
        {
            playerBody = transform.parent;
        }

        if (viewSwitcher == null && playerBody != null)
        {
            viewSwitcher = playerBody.GetComponent<CameraViewSwitcher>();
        }
    }

    private void Start()
    {
        // 현재 회전값으로 초기화
        if (playerBody != null)
        {
            rotationY = playerBody.eulerAngles.y;
        }
        rotationX = transform.localEulerAngles.x;

        // 180도 이상이면 음수로 변환
        if (rotationX > 180f)
        {
            rotationX -= 360f;
        }

        // 커서 숨기고 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleRotation();
    }

    private void HandleRotation()
    {
        // 마우스 입력
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

        // 회전값 계산
        rotationY += mouseX;
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

        bool isThirdPerson = viewSwitcher != null && viewSwitcher.IsThirdPerson();

        if (isThirdPerson)
        {
            // TPS 모드: 플레이어 몸체 Y축 회전, 카메라 X축만 회전
            ApplyTPSRotation();
        }
        else
        {
            // FPS 모드: 카메라만 전체 회전
            ApplyFPSRotation();
        }
    }

    /// <summary>
    /// FPS 모드 회전 적용
    /// 카메라가 X, Y축 모두 회전
    /// </summary>
    private void ApplyFPSRotation()
    {
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }

    /// <summary>
    /// TPS 모드 회전 적용
    /// 플레이어 몸체: Y축 회전 (좌우)
    /// 카메라: X축 회전만 (상하) + 오프셋
    /// </summary>
    private void ApplyTPSRotation()
    {
        // 플레이어 몸체 Y축 회전
        if (playerBody != null)
        {
            playerBody.rotation = Quaternion.Euler(0f, rotationY, 0f);
        }

        // 카메라 X축 회전 (로컬)
        // TPS에서는 약간 아래를 바라보도록 오프셋 추가
        float adjustedRotationX = rotationX + tpsVerticalOffset;
        adjustedRotationX = Mathf.Clamp(adjustedRotationX, minVerticalAngle, maxVerticalAngle);

        transform.localRotation = Quaternion.Euler(adjustedRotationX, 0f, 0f);
    }

    /// <summary>
    /// 외부에서 회전값 동기화 (시점 전환 시 호출)
    /// </summary>
    public void SyncRotation()
    {
        if (playerBody != null)
        {
            rotationY = playerBody.eulerAngles.y;
        }

        rotationX = transform.localEulerAngles.x;
        if (rotationX > 180f)
        {
            rotationX -= 360f;
        }
    }

    /// <summary>
    /// 현재 수평 회전값 반환
    /// </summary>
    public float GetHorizontalRotation()
    {
        return rotationY;
    }

    /// <summary>
    /// 현재 수직 회전값 반환
    /// </summary>
    public float GetVerticalRotation()
    {
        return rotationX;
    }
}
