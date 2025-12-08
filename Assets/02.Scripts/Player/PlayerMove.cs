using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    // 이동 속도
    public float walkSpeed = 5f;
    public float runSpeed = 10f;

    // 점프 힘
    public float jumpForce = 8f;

    // 중력
    public float gravity = 20f;

    // 카메라 Transform (보는 방향 기준)
    public Transform cameraTransform;

    // 컴포넌트
    private CharacterController characterController;

    // 현재 속도
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        // CharacterController 컴포넌트 가져오기
        characterController = GetComponent<CharacterController>();

        // CharacterController가 없으면 자동으로 추가
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }

        // 카메라가 지정되지 않았으면 메인 카메라 사용
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // 바닥 체크
        isGrounded = characterController.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 바닥에 붙어있도록
        }

        // 1. 키보드 입력 받기 (WASD 또는 방향키)
        float moveX = Input.GetAxis("Horizontal"); // A/D 또는 좌/우
        float moveZ = Input.GetAxis("Vertical");   // W/S 또는 상/하

        // 2. 카메라가 보는 방향 가져오기 (Y축 회전만 사용)
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // 수평 이동만 하도록 Y값 제거
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 3. 이동 방향 계산 (카메라 방향 기준)
        Vector3 moveDirection = cameraRight * moveX + cameraForward * moveZ;

        // 4. 달리기 체크 (Left Shift)
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        // 5. 이동 적용
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        // 6. 점프 (Space)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * 2f * gravity);
        }

        // 7. 중력 적용
        velocity.y -= gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}
