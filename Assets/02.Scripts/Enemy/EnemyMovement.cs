using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 이동을 담당하는 스크립트
/// NavMeshAgent를 사용하여 순찰, 추적, 복귀 상태를 관리합니다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    #region 상태 열거형
    
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Return
    }
    
    #endregion

    #region 직렬화 필드
    
    [Header("이동 설정")]
    [SerializeField] private float _patrolSpeed = 2f;
    [SerializeField] private float _chaseSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 5f;
    
    [Header("감지 설정")]
    [SerializeField] private float _detectionRange = 10f;
    [SerializeField] private float _loseTargetRange = 15f;
    [SerializeField] private float _fieldOfView = 120f;
    [SerializeField] private LayerMask _obstacleLayer;
    
    [Header("순찰 설정")]
    [SerializeField] private Transform[] _patrolPoints;
    [SerializeField] private float _waypointWaitTime = 2f;
    [SerializeField] private float _waypointReachDistance = 0.5f;
    
    [Header("디버그")]
    [SerializeField] private bool _showDebugGizmos = true;
    
    #endregion

    #region Private 필드
    
    private NavMeshAgent _agent;
    private Transform _player;
    private EnemyState _currentState = EnemyState.Idle;
    private int _currentPatrolIndex;
    private float _waypointWaitTimer;
    private Vector3 _startPosition;
    private bool _isWaiting;
    
    #endregion

    #region 프로퍼티
    
    public EnemyState CurrentState => _currentState;
    public bool IsChasing => _currentState == EnemyState.Chase;
    
    #endregion

    #region Unity 생명주기
    
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _startPosition = transform.position;
    }

    private void Start()
    {
        InitializePlayer();
        InitializePatrol();
    }

    private void Update()
    {
        UpdateState();
        ExecuteState();
    }
    
    #endregion

    #region 초기화
    
    private void InitializePlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            _player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning($"[EnemyMovement] Player 태그를 가진 오브젝트를 찾을 수 없습니다. ({gameObject.name})");
        }
    }

    private void InitializePatrol()
    {
        if (_patrolPoints == null || _patrolPoints.Length == 0)
        {
            _currentState = EnemyState.Idle;
            return;
        }
        
        _currentState = EnemyState.Patrol;
        _agent.speed = _patrolSpeed;
        SetDestinationToCurrentPatrolPoint();
    }
    
    #endregion

    #region 상태 관리
    
    private void UpdateState()
    {
        switch (_currentState)
        {
            case EnemyState.Idle:
                CheckForPlayer();
                break;
                
            case EnemyState.Patrol:
                CheckForPlayer();
                break;
                
            case EnemyState.Chase:
                CheckLosePlayer();
                break;
                
            case EnemyState.Return:
                CheckForPlayer();
                CheckReturnComplete();
                break;
        }
    }

    private void ExecuteState()
    {
        switch (_currentState)
        {
            case EnemyState.Idle:
                ExecuteIdle();
                break;
                
            case EnemyState.Patrol:
                ExecutePatrol();
                break;
                
            case EnemyState.Chase:
                ExecuteChase();
                break;
                
            case EnemyState.Return:
                ExecuteReturn();
                break;
        }
    }
    
    #endregion

    #region 상태별 실행
    
    private void ExecuteIdle()
    {
        // Idle 상태에서는 제자리에서 대기
        _agent.isStopped = true;
    }

    private void ExecutePatrol()
    {
        if (_patrolPoints == null || _patrolPoints.Length == 0)
        {
            return;
        }

        if (_isWaiting)
        {
            _waypointWaitTimer -= Time.deltaTime;
            if (_waypointWaitTimer <= 0f)
            {
                _isWaiting = false;
                MoveToNextPatrolPoint();
            }
            return;
        }

        if (HasReachedDestination())
        {
            _isWaiting = true;
            _waypointWaitTimer = _waypointWaitTime;
            _agent.isStopped = true;
        }
    }

    private void ExecuteChase()
    {
        if (_player == null)
        {
            TransitionToReturn();
            return;
        }

        _agent.isStopped = false;
        _agent.SetDestination(_player.position);
        
        // 플레이어 방향으로 부드럽게 회전
        Vector3 directionToPlayer = (_player.position - transform.position).normalized;
        directionToPlayer.y = 0f;
        
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    private void ExecuteReturn()
    {
        _agent.isStopped = false;
    }
    
    #endregion

    #region 상태 전환 조건
    
    private void CheckForPlayer()
    {
        if (_player == null)
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
        
        if (distanceToPlayer <= _detectionRange && CanSeePlayer())
        {
            TransitionToChase();
        }
    }

    private void CheckLosePlayer()
    {
        if (_player == null)
        {
            TransitionToReturn();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
        
        if (distanceToPlayer > _loseTargetRange || !CanSeePlayer())
        {
            TransitionToReturn();
        }
    }

    private void CheckReturnComplete()
    {
        if (HasReachedDestination())
        {
            if (_patrolPoints != null && _patrolPoints.Length > 0)
            {
                TransitionToPatrol();
            }
            else
            {
                TransitionToIdle();
            }
        }
    }
    
    #endregion

    #region 상태 전환
    
    private void TransitionToIdle()
    {
        _currentState = EnemyState.Idle;
        _agent.isStopped = true;
    }

    private void TransitionToPatrol()
    {
        _currentState = EnemyState.Patrol;
        _agent.speed = _patrolSpeed;
        _agent.isStopped = false;
        _isWaiting = false;
        SetDestinationToCurrentPatrolPoint();
    }

    private void TransitionToChase()
    {
        _currentState = EnemyState.Chase;
        _agent.speed = _chaseSpeed;
        _agent.isStopped = false;
        _isWaiting = false;
    }

    private void TransitionToReturn()
    {
        _currentState = EnemyState.Return;
        _agent.speed = _patrolSpeed;
        _agent.isStopped = false;
        
        // 순찰 포인트가 있으면 가장 가까운 포인트로, 없으면 시작 위치로
        if (_patrolPoints != null && _patrolPoints.Length > 0)
        {
            FindNearestPatrolPoint();
            SetDestinationToCurrentPatrolPoint();
        }
        else
        {
            _agent.SetDestination(_startPosition);
        }
    }
    
    #endregion

    #region 순찰 헬퍼
    
    private void SetDestinationToCurrentPatrolPoint()
    {
        if (_patrolPoints == null || _patrolPoints.Length == 0)
        {
            return;
        }
        
        if (_patrolPoints[_currentPatrolIndex] != null)
        {
            _agent.SetDestination(_patrolPoints[_currentPatrolIndex].position);
        }
    }

    private void MoveToNextPatrolPoint()
    {
        _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Length;
        _agent.isStopped = false;
        SetDestinationToCurrentPatrolPoint();
    }

    private void FindNearestPatrolPoint()
    {
        if (_patrolPoints == null || _patrolPoints.Length == 0)
        {
            return;
        }

        float nearestDistance = float.MaxValue;
        int nearestIndex = 0;

        for (int i = 0; i < _patrolPoints.Length; i++)
        {
            if (_patrolPoints[i] == null)
            {
                continue;
            }
            
            float distance = Vector3.Distance(transform.position, _patrolPoints[i].position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        _currentPatrolIndex = nearestIndex;
    }
    
    #endregion

    #region 감지 헬퍼
    
    private bool CanSeePlayer()
    {
        if (_player == null)
        {
            return false;
        }

        Vector3 directionToPlayer = (_player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        
        // 시야각 체크
        if (angleToPlayer > _fieldOfView * 0.5f)
        {
            return false;
        }

        // 장애물 체크
        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
        Vector3 playerEyePosition = _player.position + Vector3.up * 1.5f;
        
        if (Physics.Linecast(eyePosition, playerEyePosition, _obstacleLayer))
        {
            return false;
        }

        return true;
    }

    private bool HasReachedDestination()
    {
        if (!_agent.pathPending && _agent.remainingDistance <= _waypointReachDistance)
        {
            return true;
        }
        return false;
    }
    
    #endregion

    #region 공개 메서드
    
    /// <summary>
    /// 런타임에 순찰 포인트를 설정합니다.
    /// </summary>
    public void SetPatrolPoints(Transform[] points)
    {
        _patrolPoints = points;
        _currentPatrolIndex = 0;
        
        if (_patrolPoints != null && _patrolPoints.Length > 0)
        {
            TransitionToPatrol();
        }
    }

    /// <summary>
    /// 적을 특정 위치로 강제 이동시킵니다.
    /// </summary>
    public void ForceMoveTo(Vector3 position)
    {
        _agent.isStopped = false;
        _agent.SetDestination(position);
    }

    /// <summary>
    /// 적의 이동을 멈춥니다.
    /// </summary>
    public void StopMovement()
    {
        _agent.isStopped = true;
        TransitionToIdle();
    }

    /// <summary>
    /// 적의 이동을 재개합니다.
    /// </summary>
    public void ResumeMovement()
    {
        if (_patrolPoints != null && _patrolPoints.Length > 0)
        {
            TransitionToPatrol();
        }
        else
        {
            TransitionToIdle();
        }
    }
    
    #endregion

    #region 디버그
    
    private void OnDrawGizmosSelected()
    {
        if (!_showDebugGizmos)
        {
            return;
        }

        // 감지 범위 (녹색)
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        
        // 추적 해제 범위 (빨간색)
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, _loseTargetRange);
        
        // 시야각 표시
        Gizmos.color = Color.yellow;
        Vector3 leftBoundary = Quaternion.Euler(0, -_fieldOfView * 0.5f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, _fieldOfView * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftBoundary * _detectionRange);
        Gizmos.DrawRay(transform.position, rightBoundary * _detectionRange);
        
        // 순찰 포인트 표시
        if (_patrolPoints != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _patrolPoints.Length; i++)
            {
                if (_patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(_patrolPoints[i].position, 0.3f);
                    
                    if (i < _patrolPoints.Length - 1 && _patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(_patrolPoints[i].position, _patrolPoints[i + 1].position);
                    }
                }
            }
            
            // 마지막과 처음 연결
            if (_patrolPoints.Length > 1 && _patrolPoints[0] != null && _patrolPoints[_patrolPoints.Length - 1] != null)
            {
                Gizmos.DrawLine(_patrolPoints[_patrolPoints.Length - 1].position, _patrolPoints[0].position);
            }
        }
    }
    
    #endregion
}
