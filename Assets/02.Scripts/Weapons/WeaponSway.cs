using UnityEngine;

/// <summary>
/// 무기 스웨이(흔들림) 효과
/// 마우스 이동과 플레이어 움직임에 따른 자연스러운 총 흔들림
/// </summary>
public class WeaponSway : MonoBehaviour
{
    [Header("마우스 스웨이")]
    [SerializeField] private float _mouseSwayAmount = 0.02f;
    [SerializeField] private float _maxMouseSway = 0.06f;
    [SerializeField] private float _mouseSwaySmoothness = 6f;
    
    [Header("이동 스웨이 (Bobbing)")]
    [SerializeField] private float _bobFrequency = 10f;
    [SerializeField] private float _bobHorizontalAmount = 0.02f;
    [SerializeField] private float _bobVerticalAmount = 0.01f;
    [SerializeField] private float _bobSmoothness = 10f;
    
        [Header("총기 킥백 (발사 시)")]
    [SerializeField] private float _kickRecoverySpeed = 15f;
    
    // 킥백 상태
    private Vector3 _kickPositionOffset;
    private Vector3 _kickRotationOffset;
    private Vector3 _targetKickPosition;
    private Vector3 _targetKickRotation;
    
[Header("기울기 (Tilt)")]
    [SerializeField] private float _tiltAmount = 4f;
    [SerializeField] private float _tiltSmoothness = 8f;
    
    [Header("참조")]
    [SerializeField] private CharacterController _characterController;
    
    // 초기값 저장
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    
    // 스웨이 계산용
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private float _bobTimer;
    
    private void Start()
    {
        _initialPosition = transform.localPosition;
        _initialRotation = transform.localRotation;
        
        if (_characterController == null)
        {
            _characterController = GetComponentInParent<CharacterController>();
        }
    }
    
    private void Update()
    {
        CalculateMouseSway();
        CalculateMovementBob();
        CalculateTilt();
        UpdateKickback();
        
        ApplySway();
    }
    
    /// <summary>
    /// 마우스 이동에 따른 스웨이 계산
    /// </summary>
    private void CalculateMouseSway()
    {
        float mouseX = Input.GetAxis("Mouse X") * _mouseSwayAmount;
        float mouseY = Input.GetAxis("Mouse Y") * _mouseSwayAmount;
        
        mouseX = Mathf.Clamp(mouseX, -_maxMouseSway, _maxMouseSway);
        mouseY = Mathf.Clamp(mouseY, -_maxMouseSway, _maxMouseSway);
        
        _targetPosition = new Vector3(-mouseX, -mouseY, 0f);
    }
    
    /// <summary>
    /// 이동 시 상하좌우 흔들림 (Bobbing)
    /// </summary>
    private void CalculateMovementBob()
    {
        if (_characterController == null) return;
        
        Vector3 velocity = _characterController.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;
        
        if (speed > 0.1f && _characterController.isGrounded)
        {
            _bobTimer += Time.deltaTime * _bobFrequency * (speed / 5f);
            
            float horizontalBob = Mathf.Sin(_bobTimer) * _bobHorizontalAmount;
            float verticalBob = Mathf.Sin(_bobTimer * 2f) * _bobVerticalAmount;
            
            _targetPosition += new Vector3(horizontalBob, verticalBob, 0f);
        }
        else
        {
            _bobTimer = 0f;
        }
    }
    
    /// <summary>
    /// 좌우 이동 시 기울기
    /// </summary>
    private void CalculateTilt()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float tiltZ = -horizontalInput * _tiltAmount;
        
        _targetRotation = Quaternion.Euler(0f, 0f, tiltZ);
    }
    
    /// <summary>
    /// 킥백 업데이트 (매 프레임)
    /// </summary>
    private void UpdateKickback()
    {
        // 목표 킥백이 0으로 회복
        _targetKickPosition = Vector3.Lerp(_targetKickPosition, Vector3.zero, Time.deltaTime * _kickRecoverySpeed);
        _targetKickRotation = Vector3.Lerp(_targetKickRotation, Vector3.zero, Time.deltaTime * _kickRecoverySpeed);
        
        // 현재 킥백이 목표로 부드럽게 이동
        _kickPositionOffset = Vector3.Lerp(_kickPositionOffset, _targetKickPosition, Time.deltaTime * _kickRecoverySpeed * 2f);
        _kickRotationOffset = Vector3.Lerp(_kickRotationOffset, _targetKickRotation, Time.deltaTime * _kickRecoverySpeed * 2f);
    }
    
    /// <summary>
    /// 총기 킥백 적용 (RecoilSystem에서 호출)
    /// </summary>
    /// <param name="kickbackDistance">뒤로 밀리는 거리 (Z축)</param>
    /// <param name="kickbackRotation">위로 회전하는 각도 (X축)</param>
    public void ApplyGunKick(float kickbackDistance, float kickbackRotation)
    {
        // 위치 킥백 (뒤로 밀림)
        _targetKickPosition.z = -kickbackDistance;
        
        // 회전 킥백 (위로 들림)
        _targetKickRotation.x = -kickbackRotation;
        
        // 약간의 랜덤 좌우 회전 추가 (자연스러움)
        _targetKickRotation.y = Random.Range(-kickbackRotation * 0.2f, kickbackRotation * 0.2f);
    }

    
    /// <summary>
    /// 스웨이 적용
    /// </summary>
    /// <summary>
    /// 스웨이 적용
    /// </summary>
    private void ApplySway()
    {
        // 위치: 기본 스웨이 + 킥백 오프셋
        Vector3 finalPosition = _initialPosition + _targetPosition + _kickPositionOffset;
        transform.localPosition = Vector3.Lerp(
            transform.localPosition, 
            finalPosition, 
            Time.deltaTime * _mouseSwaySmoothness
        );
        
        // 회전: 기본 틸트 + 킥백 회전
        Quaternion kickRotation = Quaternion.Euler(_kickRotationOffset);
        Quaternion finalRotation = _initialRotation * _targetRotation * kickRotation;
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, 
            finalRotation, 
            Time.deltaTime * _tiltSmoothness
        );
    }
    
    /// <summary>
    /// 스웨이 일시 정지 (조준 시 등)
    /// </summary>
    public void SetSwayEnabled(bool enabled)
    {
        this.enabled = enabled;
        
        if (!enabled)
        {
            transform.localPosition = _initialPosition;
            transform.localRotation = _initialRotation;
        }
    }
}
