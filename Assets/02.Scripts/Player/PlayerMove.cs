using System;
using UnityEngine;

/// <summary>
/// 플레이어 이동, 점프, 스태미나 관리
/// RecoilSystem과 연동하여 탄퍼짐 계산에 플레이어 상태 전달
/// </summary>
public class PlayerMove : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float jumpForce = 8f;
    public float gravity = 20f;

    [Header("Double Jump")]
    public float doubleJumpStaminaCost = 25f;
    public float doubleJumpForce = 7f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    public float staminaRegenRate = 15f;
    public float staminaRegenDelay = 1f;

    [Header("References")]
    public Transform cameraTransform;

    [Header("Animation")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Animator _soldierAnimator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    [Header("Recoil System")]
    [SerializeField] private RecoilSystem _recoilSystem;
    
    // Animation parameter hashes (PlayerAnimator.controller와 일치)
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimIsShooting = Animator.StringToHash("IsShooting");

    // Private fields
    private CharacterController _characterController;
    private Vector3 _velocity;
    private bool _isGrounded;
    
    private float _currentStamina;
    private float _staminaRegenTimer;
    private bool _isRunning;
    private bool _canDoubleJump;

    // Events
    public event Action<float, float> OnStaminaChanged;

    // Properties
    public float CurrentStamina => _currentStamina;
    public bool CanRun => _currentStamina > 0f;
    public bool IsRunning => _isRunning;
    public bool IsGrounded => _isGrounded;

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        if (_characterController == null)
        {
            _characterController = gameObject.AddComponent<CharacterController>();
        }

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
        
        // RecoilSystem 자동 찾기
        if (_recoilSystem == null)
        {
            _recoilSystem = RecoilSystem.Instance;
            if (_recoilSystem == null)
            {
                _recoilSystem = FindFirstObjectByType<RecoilSystem>();
            }
        }
        
        // Soldier Animator 자동 찾기 (Soldier_demo의 Animator를 메인으로 사용)
        if (_soldierAnimator == null)
        {
            Transform soldierTransform = transform.Find("Soldier_demo");
            if (soldierTransform != null)
            {
                _soldierAnimator = soldierTransform.GetComponent<Animator>();
            }
        }

        // _animator도 Soldier_demo의 Animator 참조 (애니메이션 통합)
        if (_animator == null && _soldierAnimator != null)
        {
            _animator = _soldierAnimator;
        }
        else if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
_currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(_currentStamina, maxStamina);

        // 디버그: Animator 참조 확인
        #if UNITY_EDITOR
        Debug.Log($"[PlayerMove] Start - _animator: {(_animator != null ? _animator.name : "NULL")}, _soldierAnimator: {(_soldierAnimator != null ? _soldierAnimator.name : "NULL")}");
        #endif
    }

void Update()
    {
        // 게임 상태가 Playing이 아니면 입력 무시
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
        {
            return;
        }

        // Animator null 체크 (시작 시 한 번만)
        if (_animator == null && Time.frameCount % 60 == 0)
        {
            Debug.LogWarning("[PlayerMove] _animator is NULL!");
        }

        HandleGroundCheck();
        HandleMovement();
        HandleStamina();
        HandleJump();
        ApplyGravity();
        UpdateRecoilSystemState();
    }

    private void HandleGroundCheck()
    {
        _isGrounded = _characterController.isGrounded;
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
            _canDoubleJump = false;
        }

        // 애니메이션 파라미터 업데이트
        if (_animator != null)
        {
            _animator.SetBool(IsGroundedHash, _isGrounded);
            // 착지 시 점프 애니메이션 종료
            if (_isGrounded)
            {
                _animator.SetBool(IsJumpingHash, false);
            }
        }
    }

    // 디버그용: 현재 애니메이션 상태 로깅
    private float _debugLogTimer = 0f;
    private void LogAnimationState()
    {
        _debugLogTimer += Time.deltaTime;
        if (_debugLogTimer >= 1f && _animator != null)
        {
            float speed = _animator.GetFloat(SpeedHash);
            bool isJumping = _animator.GetBool(IsJumpingHash);
            bool isGrounded = _animator.GetBool(IsGroundedHash);
            Debug.Log($"[Animation] Speed: {speed:F2}, IsJumping: {isJumping}, IsGrounded: {isGrounded}");
            _debugLogTimer = 0f;
        }
    }

    private void HandleMovement()
    {
        // 디버그 로그 호출
        LogAnimationState();

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraRight * moveX + cameraForward * moveZ;
        bool isMoving = moveDirection.magnitude > 0.1f;
        bool wantsToRun = Input.GetKey(KeyCode.LeftShift);

        _isRunning = isMoving && wantsToRun && CanRun;
        float currentSpeed = _isRunning ? runSpeed : walkSpeed;

        

        // 애니메이션 Speed 파라미터 업데이트
        if (_animator != null)
        {
            float animSpeed = isMoving ? (currentSpeed / runSpeed) : 0f;
            _animator.SetFloat(SpeedHash, animSpeed);
        }
_characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
        
        // 애니메이션 파라미터 업데이트
        UpdateAnimation(isMoving, moveDirection.magnitude);
    }
    
    private void UpdateAnimation(bool isMoving, float moveSpeed)
    {
        if (_soldierAnimator == null) return;

        // Speed 파라미터로 BlendTree 제어 (0 = Idle, 1 = Run)
        _soldierAnimator.SetFloat(AnimSpeed, _isRunning ? 1f : (isMoving ? 0.5f : 0f));
    }

    /// <summary>
    /// 발사 애니메이션 설정 (외부에서 호출)
    /// </summary>
    public void SetShootingAnimation(bool isShooting)
    {
        if (_soldierAnimator == null) return;
        _soldierAnimator.SetBool(AnimIsShooting, isShooting);
    }

    private void HandleStamina()
    {
        float previousStamina = _currentStamina;

        if (_isRunning)
        {
            _currentStamina -= staminaDrainRate * Time.deltaTime;
            _currentStamina = Mathf.Max(_currentStamina, 0f);
            _staminaRegenTimer = staminaRegenDelay;
        }
        else
        {
            if (_staminaRegenTimer > 0f)
            {
                _staminaRegenTimer -= Time.deltaTime;
            }
            else if (_currentStamina < maxStamina)
            {
                _currentStamina += staminaRegenRate * Time.deltaTime;
                _currentStamina = Mathf.Min(_currentStamina, maxStamina);
            }
        }

        if (Mathf.Abs(previousStamina - _currentStamina) > 0.01f)
        {
            OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
        }
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            if (_isGrounded)
            {
                // 1단 점프: 스태미나 소모 없음
                _velocity.y = Mathf.Sqrt(jumpForce * 2f * gravity);
                _canDoubleJump = true;

                // 점프 애니메이션 트리거
                if (_animator != null)
                {
                    _animator.SetBool(IsJumpingHash, true);
                    #if UNITY_EDITOR
                    Debug.Log("[Animation] Jump triggered! IsJumping: true");
                    #endif
                }
            }
            else if (_canDoubleJump && _currentStamina >= doubleJumpStaminaCost)
            {
                // 2단 점프: 스태미나 소모
                _velocity.y = Mathf.Sqrt(doubleJumpForce * 2f * gravity);
                _currentStamina -= doubleJumpStaminaCost;
                _staminaRegenTimer = staminaRegenDelay;
                _canDoubleJump = false;
                OnStaminaChanged?.Invoke(_currentStamina, maxStamina);

                // 2단 점프 애니메이션
                if (_animator != null)
                {
                    _animator.SetBool(IsJumpingHash, true);
                    #if UNITY_EDITOR
                    Debug.Log("[Animation] Double Jump triggered! IsJumping: true");
                    #endif
                }
            }
        }
    }

    private void ApplyGravity()
    {
        _velocity.y -= gravity * Time.deltaTime;
        _characterController.Move(_velocity * Time.deltaTime);
    }

    /// <summary>
    /// RecoilSystem에 플레이어 상태 전달 (탄퍼짐 계산용)
    /// 이동/점프/웅크리기 상태에 따라 탄퍼짐이 변화함
    /// </summary>
    private void UpdateRecoilSystemState()
    {
        if (_recoilSystem == null) return;
        
        // 이동 상태 확인
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 moveInput = new Vector3(moveX, 0f, moveZ);
        bool isMoving = moveInput.magnitude > 0.1f;
        
        // 이동 속도 정규화 (달리기 시 1.0, 걷기 시 0.5)
        float normalizedSpeed = 0f;
        if (isMoving)
        {
            normalizedSpeed = _isRunning ? 1f : 0.5f;
        }
        
        // 공중 상태
        bool isAirborne = !_isGrounded;
        
        // 웅크리기 (현재 미구현)
        bool isCrouching = false;
        
        // RecoilSystem에 상태 전달
        _recoilSystem.SetPlayerState(isMoving, isAirborne, isCrouching, normalizedSpeed);
    }

    public void RestoreStamina(float amount)
    {
        _currentStamina = Mathf.Min(_currentStamina + amount, maxStamina);
        OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
    }

    public void SetStamina(float amount)
    {
        _currentStamina = Mathf.Clamp(amount, 0f, maxStamina);
        OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
    }
}
