using System;
using UnityEngine;

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

        _currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
    }

    void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleStamina();
        HandleJump();
        ApplyGravity();
    }

private void HandleGroundCheck()
    {
        _isGrounded = _characterController.isGrounded;
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
            _canDoubleJump = false; // 땅에 닿으면 2단 점프 상태 리셋
        }
    }

    private void HandleMovement()
    {
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

        _characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
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
            }
            else if (_canDoubleJump && _currentStamina >= doubleJumpStaminaCost)
            {
                // 2단 점프: 스태미나 소모
                _velocity.y = Mathf.Sqrt(doubleJumpForce * 2f * gravity);
                _currentStamina -= doubleJumpStaminaCost;
                _staminaRegenTimer = staminaRegenDelay;
                _canDoubleJump = false;
                OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
            }
        }
    }

    private void ApplyGravity()
    {
        _velocity.y -= gravity * Time.deltaTime;
        _characterController.Move(_velocity * Time.deltaTime);
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
