using UnityEngine;

/// <summary>
/// 카메라 회전 제어 (FPS/TPS 모드 지원)
/// FPS: 카메라만 회전
/// TPS: 플레이어 몸체(Y축) + 카메라(X축) 분리 회전
/// 외부에서 반동 입력을 받아 적용
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

    // 회전 상태
    private float _rotationX = 0f;
    private float _rotationY = 0f;
    
    // 반동 관련
    private Vector3 _recoilOffset;
    private Vector3 _currentRecoilOffset;

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
            _rotationY = playerBody.eulerAngles.y;
        }
        _rotationX = transform.localEulerAngles.x;

        // 180도 이상이면 음수로 변환
        if (_rotationX > 180f)
        {
            _rotationX -= 360f;
        }

        // 커서 숨기고 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

private void Update()
    {
        // 게임 상태가 Playing이 아니면 회전 입력 무시
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
        {
            return;
        }
        
        HandleRotation();
        UpdateRecoilOffset();
    }

    private void HandleRotation()
    {
        // 마우스 입력
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

        // 회전값 계산 (반동 포함)
        _rotationY += mouseX;
        _rotationX -= mouseY;
        _rotationX = Mathf.Clamp(_rotationX, minVerticalAngle, maxVerticalAngle);

        bool isThirdPerson = viewSwitcher != null && viewSwitcher.IsThirdPerson();

        if (isThirdPerson)
        {
            ApplyTPSRotation();
        }
        else
        {
            ApplyFPSRotation();
        }
    }

    /// <summary>
    /// 반동 오프셋 부드럽게 적용
    /// </summary>
    private void UpdateRecoilOffset()
    {
        // 반동 오프셋 부드럽게 적용
        _currentRecoilOffset = Vector3.Lerp(_currentRecoilOffset, _recoilOffset, Time.deltaTime * 10f);
    }

    /// <summary>
    /// FPS 모드 회전 적용
    /// 카메라가 X, Y축 모두 회전 + 반동
    /// </summary>
    private void ApplyFPSRotation()
    {
        float finalRotationX = _rotationX + _currentRecoilOffset.x;
        float finalRotationY = _rotationY + _currentRecoilOffset.y;
        
        // 수직 각도 제한 적용
        finalRotationX = Mathf.Clamp(finalRotationX, minVerticalAngle, maxVerticalAngle);
        
        transform.rotation = Quaternion.Euler(finalRotationX, finalRotationY, 0f);
    }

    /// <summary>
    /// TPS 모드 회전 적용
    /// 플레이어 몸체: Y축 회전 (좌우)
    /// 카메라: X축 회전만 (상하) + 오프셋
    /// </summary>
    private void ApplyTPSRotation()
    {
        // 플레이어 몸체 Y축 회전 (반동 포함)
        if (playerBody != null)
        {
            float finalRotationY = _rotationY + _currentRecoilOffset.y;
            playerBody.rotation = Quaternion.Euler(0f, finalRotationY, 0f);
        }

        // 카메라 X축 회전 (로컬) + 반동
        float adjustedRotationX = _rotationX + tpsVerticalOffset + _currentRecoilOffset.x;
        adjustedRotationX = Mathf.Clamp(adjustedRotationX, minVerticalAngle, maxVerticalAngle);

        transform.localRotation = Quaternion.Euler(adjustedRotationX, 0f, 0f);
    }

    /// <summary>
    /// 외부에서 반동 추가 (RecoilSystem에서 호출)
    /// </summary>
    /// <param name="recoil">반동 벡터 (x=수직, y=수평)</param>
    public void AddRecoil(Vector2 recoil)
    {
        // 반동을 실제 회전값에 직접 적용
        _rotationX += recoil.x;
        _rotationY += recoil.y;
        
        // 수직 각도 제한
        _rotationX = Mathf.Clamp(_rotationX, minVerticalAngle, maxVerticalAngle);
    }
    
    /// <summary>
    /// 일시적인 반동 오프셋 설정 (시각적 효과용)
    /// </summary>
    public void SetRecoilOffset(Vector3 offset)
    {
        _recoilOffset = offset;
    }
    
    /// <summary>
    /// 반동 오프셋 초기화
    /// </summary>
    public void ResetRecoilOffset()
    {
        _recoilOffset = Vector3.zero;
    }

    /// <summary>
    /// 외부에서 회전값 동기화 (시점 전환 시 호출)
    /// </summary>
    public void SyncRotation()
    {
        if (playerBody != null)
        {
            _rotationY = playerBody.eulerAngles.y;
        }

        _rotationX = transform.localEulerAngles.x;
        if (_rotationX > 180f)
        {
            _rotationX -= 360f;
        }
        
        // 반동 초기화
        _recoilOffset = Vector3.zero;
        _currentRecoilOffset = Vector3.zero;
    }

    /// <summary>
    /// 현재 수평 회전값 반환
    /// </summary>
    public float GetHorizontalRotation()
    {
        return _rotationY;
    }

    /// <summary>
    /// 현재 수직 회전값 반환
    /// </summary>
    public float GetVerticalRotation()
    {
        return _rotationX;
    }
}
