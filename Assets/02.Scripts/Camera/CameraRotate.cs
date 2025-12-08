using UnityEngine;

public class CameraRotate : MonoBehaviour
{
    // 회전 속도
    public float rotationSpeed = 200f;

    // 수직 회전 제한 (위/아래)
    public float minVerticalAngle = -80f;
    public float maxVerticalAngle = 80f;

    // 현재 회전 각도
    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        // 현재 카메라의 회전값으로 초기화
        Vector3 currentRotation = transform.eulerAngles;
        rotationX = currentRotation.x;
        rotationY = currentRotation.y;

        // 커서 숨기고 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. 마우스 입력 받기
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 2. 마우스 입력에 따른 회전 방향 계산
        rotationY += mouseX * rotationSpeed * Time.deltaTime;
        rotationX -= mouseY * rotationSpeed * Time.deltaTime;

        // 수직 회전 각도 제한 (고개를 너무 많이 들거나 숙이지 않도록)
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

        // 3. 카메라를 회전 방향으로 회전
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }
}
