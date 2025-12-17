using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 탑뷰 모드에서 마우스 우클릭으로 NavMeshAgent 이동
/// CameraViewSwitcher와 연동하여 탑뷰일 때만 활성화
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class TopViewClickMove : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraViewSwitcher _viewSwitcher;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private PlayerMove _playerMove;

    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _stoppingDistance = 0.5f;
    [SerializeField] private LayerMask _groundLayer = ~0;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject _clickIndicatorPrefab;
    [SerializeField] private float _indicatorDuration = 1f;

    private NavMeshAgent _agent;
    private Camera _mainCamera;
    private bool _isTopViewMode;
    private GameObject _currentIndicator;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _mainCamera = Camera.main;

        // 자동 참조 설정
        if (_viewSwitcher == null)
        {
            _viewSwitcher = GetComponent<CameraViewSwitcher>();
        }

        if (_characterController == null)
        {
            _characterController = GetComponent<CharacterController>();
        }

        if (_playerMove == null)
        {
            _playerMove = GetComponent<PlayerMove>();
        }

        // NavMeshAgent 초기 설정
        if (_agent != null)
        {
            _agent.speed = _moveSpeed;
            _agent.stoppingDistance = _stoppingDistance;
            _agent.enabled = false; // 초기에는 비활성화
            _agent.updatePosition = false; // CharacterController와 충돌 방지
            _agent.updateRotation = false;
        }
    }

    private void Update()
    {
        // 게임 상태 체크
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
        {
            return;
        }

        // 탑뷰 모드 체크
        bool wasTopView = _isTopViewMode;
        _isTopViewMode = _viewSwitcher != null && _viewSwitcher.IsTopView();

        // 모드 전환 감지
        if (_isTopViewMode != wasTopView)
        {
            OnViewModeChanged(_isTopViewMode);
        }

        // 탑뷰일 때만 클릭 이동 처리
        if (_isTopViewMode)
        {
            HandleClickMovement();
            SyncAgentToTransform();
        }
    }

    /// <summary>
    /// 뷰 모드 전환 시 호출
    /// </summary>
    private void OnViewModeChanged(bool isTopView)
    {
        if (_agent == null) return;

        if (isTopView)
        {
            // 탑뷰 모드 진입: NavMeshAgent 활성화
            _agent.enabled = true;

            // 현재 위치로 NavMeshAgent 워프
            if (_agent.isOnNavMesh)
            {
                _agent.Warp(transform.position);
            }

            // CharacterController 이동 비활성화 (PlayerMove에서 처리)
            if (_playerMove != null)
            {
                _playerMove.enabled = false;
            }

            Debug.Log("[TopViewClickMove] 탑뷰 모드 활성화 - NavMeshAgent ON");
        }
        else
        {
            // FPS 모드 복귀: NavMeshAgent 비활성화
            if (_agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }
            _agent.enabled = false;

            // PlayerMove 다시 활성화
            if (_playerMove != null)
            {
                _playerMove.enabled = true;
            }

            Debug.Log("[TopViewClickMove] FPS 모드 복귀 - NavMeshAgent OFF");
        }
    }

    /// <summary>
    /// 마우스 클릭 이동 처리
    /// </summary>
    private void HandleClickMovement()
    {
        // 마우스 우클릭
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _groundLayer))
            {
                MoveToPosition(hit.point);
            }
        }
    }

    /// <summary>
    /// 지정된 위치로 이동
    /// </summary>
    public void MoveToPosition(Vector3 targetPosition)
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
        {
            Debug.LogWarning("[TopViewClickMove] NavMeshAgent가 활성화되지 않았거나 NavMesh 위에 없습니다.");
            return;
        }

        // 목적지 설정
        if (_agent.SetDestination(targetPosition))
        {
            Debug.Log($"[TopViewClickMove] 이동 목적지 설정: {targetPosition}");

            // 클릭 위치 표시
            ShowClickIndicator(targetPosition);
        }
    }

    /// <summary>
    /// NavMeshAgent 위치를 Transform에 동기화
    /// </summary>
    private void SyncAgentToTransform()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        // NavMeshAgent의 다음 위치로 CharacterController 이동
        if (_characterController != null && _characterController.enabled)
        {
            Vector3 moveDirection = _agent.desiredVelocity;
            _characterController.Move(moveDirection * Time.deltaTime);

            // 이동 방향으로 회전
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Vector3 lookDirection = moveDirection;
                lookDirection.y = 0f;
                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(lookDirection),
                        Time.deltaTime * 10f
                    );
                }
            }

            // NavMeshAgent 위치를 현재 Transform 위치로 동기화
            _agent.nextPosition = transform.position;
        }
    }

    /// <summary>
    /// 클릭 위치에 인디케이터 표시
    /// </summary>
    private void ShowClickIndicator(Vector3 position)
    {
        // 기존 인디케이터 제거
        if (_currentIndicator != null)
        {
            Destroy(_currentIndicator);
        }

        // 새 인디케이터 생성
        if (_clickIndicatorPrefab != null)
        {
            _currentIndicator = Instantiate(_clickIndicatorPrefab, position + Vector3.up * 0.1f, Quaternion.identity);
            Destroy(_currentIndicator, _indicatorDuration);
        }
    }

    /// <summary>
    /// 이동 중인지 확인
    /// </summary>
    public bool IsMoving()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return false;
        return _agent.hasPath && _agent.remainingDistance > _stoppingDistance;
    }

    /// <summary>
    /// 이동 중지
    /// </summary>
    public void StopMovement()
    {
        if (_agent != null && _agent.enabled && _agent.isOnNavMesh && _agent.hasPath)
        {
            _agent.ResetPath();
        }
    }

    private void OnDisable()
    {
        // 비활성화 시 NavMeshAgent도 비활성화
        if (_agent != null)
        {
            if (_agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }
            _agent.enabled = false;
        }

        // PlayerMove 다시 활성화
        if (_playerMove != null)
        {
            _playerMove.enabled = true;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_agent != null && _agent.enabled && _agent.hasPath)
        {
            // 경로 시각화
            Gizmos.color = Color.green;
            Vector3[] corners = _agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }

            // 목적지 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_agent.destination, 0.3f);
        }
    }
#endif
}
