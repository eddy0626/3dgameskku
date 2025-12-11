using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 AI 행동 컨트롤러
/// NavMeshAgent 기반 순찰/추적/공격 상태 머신
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    #region Enums
    
    public enum EnemyState
    {
        Idle,       // 대기
        Patrol,     // 순찰
        Chase,      // 추적
        Attack,     // 공격
        Return,     // 복귀
        Dead        // 사망
    }
    
    #endregion
    
    #region Inspector Fields
    
    [Header("타겟 설정")]
    [SerializeField] private string _targetTag = "Player";
    [SerializeField] private LayerMask _targetLayer;
    
        [Header("감지 설정")]
    [SerializeField] private float _detectionRange = 15f;
    [SerializeField] private float _fieldOfView = 120f;
    [SerializeField] private float _detectionHeight = 2f;
    [SerializeField] private float _hearingRange = 10f;
    [SerializeField] private LayerMask _obstacleMask;
    
    [Header("순찰 설정")]
    [SerializeField] private Transform[] _patrolPoints;
    [SerializeField] private float _patrolWaitTime = 2f;
    [SerializeField] private float _patrolSpeed = 2f;
    [SerializeField] private bool _randomPatrol = false;
    
        [Header("추적 설정")]
    [SerializeField] private float _chaseSpeed = 5f;
    [SerializeField] private float _chaseRange = 20f;
    [SerializeField] private float _loseTargetTime = 5f;
    [SerializeField] private float _rotationSpeed = 10f;
    
    [Header("공격 설정")]
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _attackCooldown = 1.5f;
    
    [Header("복귀 설정")]
    [SerializeField] private float _maxChaseDistance = 30f;
    [SerializeField] private bool _returnToSpawn = true;
    
        [Header("사운드")]
    [SerializeField] private AudioClip _alertSound;
    
    [Header("디버그")]
    [SerializeField] private bool _showGizmos = true;
    
    #endregion
    
    #region Private Fields
    
    private NavMeshAgent _agent;
    private EnemyHealth _health;
    private EnemyAttack _attack;
    
    private EnemyState _currentState = EnemyState.Idle;
    private Transform _target;
    private Vector3 _spawnPosition;
    private Vector3 _lastKnownTargetPosition;
    
    private int _currentPatrolIndex;
    private float _patrolWaitTimer;
    private float _loseTargetTimer;
    private float _attackTimer;
    
    private bool _hasTarget;
    private bool _canSeeTarget;
    
    #endregion
    
    #region Properties
    
    public EnemyState CurrentState => _currentState;
    public Transform Target => _target;
    public bool HasTarget => _hasTarget;
    public float DistanceToTarget => _target != null ? Vector3.Distance(transform.position, _target.position) : float.MaxValue;
    
    #endregion
    
    #region Unity Callbacks
    
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<EnemyHealth>();
        _attack = GetComponent<EnemyAttack>();
        
        _spawnPosition = transform.position;
    }
    
    private void Start()
    {
        // 초기 플레이어 찾기
        FindTarget();
        
        // 체력 이벤트 구독
        if (_health != null)
        {
            _health.OnDeath += OnDeath;
        }
        
        // 초기 상태 설정
        if (_patrolPoints != null && _patrolPoints.Length > 0)
        {
            ChangeState(EnemyState.Patrol);
        }
        else
        {
            ChangeState(EnemyState.Idle);
        }
    }


    /// <summary>
    /// EnemyData 기반 초기화 (EnemyBase에서 호출)
    /// </summary>
    public void Initialize(
        float detectionRange,
        float fieldOfView,
        float detectionHeight,
        float hearingRange,
        float patrolSpeed,
        float patrolWaitTime,
        bool randomPatrol,
        float chaseSpeed,
        float chaseRange,
        float loseTargetTime,
        float attackRange,
        float attackCooldown,
        float maxChaseDistance,
        bool returnToSpawn,
        float rotationSpeed,
        AudioClip alertSound)
    {
        _detectionRange = detectionRange;
        _fieldOfView = fieldOfView;
        _detectionHeight = detectionHeight;
        _hearingRange = hearingRange;
        _patrolSpeed = patrolSpeed;
        _patrolWaitTime = patrolWaitTime;
        _randomPatrol = randomPatrol;
        _chaseSpeed = chaseSpeed;
        _chaseRange = chaseRange;
        _loseTargetTime = loseTargetTime;
        _attackRange = attackRange;
        _attackCooldown = attackCooldown;
        _maxChaseDistance = maxChaseDistance;
        _returnToSpawn = returnToSpawn;
        _rotationSpeed = rotationSpeed;
        _alertSound = alertSound;
        
        // NavMeshAgent 속도 업데이트
        if (_agent != null)
        {
            _agent.speed = _patrolSpeed;
        }
        
        Debug.Log($"[EnemyAI] {gameObject.name} initialized - Detection: {_detectionRange}m, Chase: {_chaseSpeed}m/s");
    }
    
    /// <summary>
    /// AI 상태 리셋 (리스폰용)
    /// </summary>
    public void ResetAI()
    {
        _hasTarget = false;
        _canSeeTarget = false;
        _loseTargetTimer = 0f;
        _attackTimer = 0f;
        _patrolWaitTimer = 0f;
        _currentPatrolIndex = 0;
        _spawnPosition = transform.position;
        
        // NavMeshAgent 리셋
        if (_agent != null)
        {
            _agent.enabled = true;
            _agent.isStopped = false;
            _agent.speed = _patrolSpeed;
        }
        
        // 초기 상태 설정
        if (_patrolPoints != null && _patrolPoints.Length > 0)
        {
            ChangeState(EnemyState.Patrol);
        }
        else
        {
            ChangeState(EnemyState.Idle);
        }
        
        Debug.Log($"[EnemyAI] {gameObject.name} AI reset");
    }

    
    private void Update()
    {
        if (_currentState == EnemyState.Dead) return;
        
        // 타겟 감지
        DetectTarget();
        
        // 상태별 행동
        switch (_currentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.Return:
                UpdateReturn();
                break;
        }
    }
    
    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnDeath -= OnDeath;
        }
    }
    
    #endregion
    
    #region Target Detection
    
    /// <summary>
    /// 타겟 찾기
    /// </summary>
    private void FindTarget()
    {
        GameObject targetObj = GameObject.FindGameObjectWithTag(_targetTag);
        if (targetObj != null)
        {
            _target = targetObj.transform;
        }
    }
    
    /// <summary>
    /// 타겟 감지 (시야 + 거리)
    /// </summary>
    private void DetectTarget()
    {
        if (_target == null)
        {
            FindTarget();
            if (_target == null)
            {
                _hasTarget = false;
                _canSeeTarget = false;
                return;
            }
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, _target.position);
        
        // 감지 범위 체크
        if (distanceToTarget > _detectionRange)
        {
            _canSeeTarget = false;
            return;
        }
        
        // 높이 체크
        float heightDiff = Mathf.Abs(transform.position.y - _target.position.y);
        if (heightDiff > _detectionHeight)
        {
            _canSeeTarget = false;
            return;
        }
        
        // 시야각 체크
        Vector3 directionToTarget = (_target.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        
        if (angle > _fieldOfView * 0.5f)
        {
            _canSeeTarget = false;
            return;
        }
        
        // 장애물 체크 (레이캐스트)
        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
        Vector3 targetPosition = _target.position + Vector3.up * 1f;
        
        if (Physics.Linecast(eyePosition, targetPosition, _obstacleMask))
        {
            _canSeeTarget = false;
            return;
        }
        
        // 타겟 발견!
        _canSeeTarget = true;
        _hasTarget = true;
        _lastKnownTargetPosition = _target.position;
        _loseTargetTimer = 0f;
    }
    
    #endregion
    
    #region State Machine
    
    /// <summary>
    /// 상태 변경
    /// </summary>
    private void ChangeState(EnemyState newState)
    {
        if (_currentState == newState) return;
        
        // 이전 상태 종료
        ExitState(_currentState);
        
        // 새 상태 진입
        _currentState = newState;
        EnterState(newState);
        
        Debug.Log($"[EnemyAI] {gameObject.name} state changed to: {newState}");
    }
    
    /// <summary>
    /// 상태 진입 처리
    /// </summary>
    private void EnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Idle:
                _agent.isStopped = true;
                break;
                
            case EnemyState.Patrol:
                _agent.isStopped = false;
                _agent.speed = _patrolSpeed;
                SetNextPatrolPoint();
                break;
                
            case EnemyState.Chase:
                _agent.isStopped = false;
                _agent.speed = _chaseSpeed;
                break;
                
            case EnemyState.Attack:
                _agent.isStopped = true;
                break;
                
            case EnemyState.Return:
                _agent.isStopped = false;
                _agent.speed = _patrolSpeed;
                _agent.SetDestination(_spawnPosition);
                break;
                
            case EnemyState.Dead:
                _agent.isStopped = true;
                _agent.enabled = false;
                break;
        }
    }
    
    /// <summary>
    /// 상태 종료 처리
    /// </summary>
    private void ExitState(EnemyState state)
    {
        // 필요시 정리 로직 추가
    }
    
    #endregion
    
    #region State Updates
    
    /// <summary>
    /// 대기 상태 업데이트
    /// </summary>
    private void UpdateIdle()
    {
        if (_canSeeTarget)
        {
            ChangeState(EnemyState.Chase);
        }
    }
    
    /// <summary>
    /// 순찰 상태 업데이트
    /// </summary>
    private void UpdatePatrol()
    {
        // 타겟 발견 시 추적
        if (_canSeeTarget)
        {
            ChangeState(EnemyState.Chase);
            return;
        }
        
        // 순찰 포인트가 없으면 대기
        if (_patrolPoints == null || _patrolPoints.Length == 0)
        {
            ChangeState(EnemyState.Idle);
            return;
        }
        
        // 목적지 도착 체크
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            // 대기 시간
            _patrolWaitTimer += Time.deltaTime;
            
            if (_patrolWaitTimer >= _patrolWaitTime)
            {
                _patrolWaitTimer = 0f;
                SetNextPatrolPoint();
            }
        }
    }
    
    /// <summary>
    /// 추적 상태 업데이트
    /// </summary>
    private void UpdateChase()
    {
        if (_target == null)
        {
            ChangeState(EnemyState.Return);
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, _target.position);
        float distanceFromSpawn = Vector3.Distance(transform.position, _spawnPosition);
        
        // 최대 추적 거리 초과 시 복귀
        if (_returnToSpawn && distanceFromSpawn > _maxChaseDistance)
        {
            _hasTarget = false;
            ChangeState(EnemyState.Return);
            return;
        }
        
        // 공격 범위 진입 시 공격
        if (distanceToTarget <= _attackRange)
        {
            ChangeState(EnemyState.Attack);
            return;
        }
        
        // 추적 범위 벗어남 + 시야 상실
        if (!_canSeeTarget && distanceToTarget > _chaseRange)
        {
            _loseTargetTimer += Time.deltaTime;
            
            if (_loseTargetTimer >= _loseTargetTime)
            {
                _hasTarget = false;
                ChangeState(EnemyState.Return);
                return;
            }
        }
        
        // 추적 이동
        if (_canSeeTarget)
        {
            _agent.SetDestination(_target.position);
        }
        else
        {
            // 마지막 위치로 이동
            _agent.SetDestination(_lastKnownTargetPosition);
        }
        
        // 타겟 방향 바라보기
        LookAtTarget();
    }
    
    /// <summary>
    /// 공격 상태 업데이트
    /// </summary>
    private void UpdateAttack()
    {
        if (_target == null)
        {
            ChangeState(EnemyState.Return);
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, _target.position);
        
        // 공격 범위 이탈 시 추적
        if (distanceToTarget > _attackRange * 1.2f)
        {
            ChangeState(EnemyState.Chase);
            return;
        }
        
        // 타겟 바라보기
        LookAtTarget();
        
        // 공격 쿨다운
        _attackTimer += Time.deltaTime;
        
        if (_attackTimer >= _attackCooldown)
        {
            _attackTimer = 0f;
            PerformAttack();
        }
    }
    
    /// <summary>
    /// 복귀 상태 업데이트
    /// </summary>
    private void UpdateReturn()
    {
        // 이동 중 타겟 발견 시 추적
        if (_canSeeTarget)
        {
            ChangeState(EnemyState.Chase);
            return;
        }
        
        // 스폰 위치 도착 체크
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            if (_patrolPoints != null && _patrolPoints.Length > 0)
            {
                ChangeState(EnemyState.Patrol);
            }
            else
            {
                ChangeState(EnemyState.Idle);
            }
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// 다음 순찰 포인트 설정
    /// </summary>
    private void SetNextPatrolPoint()
    {
        if (_patrolPoints == null || _patrolPoints.Length == 0) return;
        
        if (_randomPatrol)
        {
            _currentPatrolIndex = Random.Range(0, _patrolPoints.Length);
        }
        else
        {
            _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Length;
        }
        
        if (_patrolPoints[_currentPatrolIndex] != null)
        {
            _agent.SetDestination(_patrolPoints[_currentPatrolIndex].position);
        }
    }
    
    /// <summary>
    /// 타겟 방향 바라보기
    /// </summary>
    private void LookAtTarget()
    {
        if (_target == null) return;
        
        Vector3 direction = (_target.position - transform.position).normalized;
        direction.y = 0f;
        
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * _rotationSpeed
            );
        }
    }
    
    /// <summary>
    /// 공격 실행
    /// </summary>
    private void PerformAttack()
    {
        if (_attack != null)
        {
            _attack.Attack(_target);
        }
        else
        {
            Debug.Log($"[EnemyAI] {gameObject.name} attacks (no EnemyAttack component)");
        }
    }
    
    /// <summary>
    /// 사망 처리
    /// </summary>
    private void OnDeath()
    {
        ChangeState(EnemyState.Dead);
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 강제 타겟 설정
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        _target = newTarget;
        _hasTarget = newTarget != null;
        
        if (_hasTarget && _currentState != EnemyState.Dead)
        {
            ChangeState(EnemyState.Chase);
        }
    }
    
    /// <summary>
    /// 순찰 포인트 설정
    /// </summary>
    public void SetPatrolPoints(Transform[] points)
    {
        _patrolPoints = points;
        _currentPatrolIndex = 0;
    }
    
    /// <summary>
    /// 알림 (소리 등으로 인지)
    /// </summary>
    public void Alert(Vector3 alertPosition)
    {
        if (_currentState == EnemyState.Dead) return;
        
        _lastKnownTargetPosition = alertPosition;
        
        if (_currentState == EnemyState.Idle || _currentState == EnemyState.Patrol)
        {
            _agent.SetDestination(alertPosition);
        }
    }
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        if (!_showGizmos) return;
        
        // 감지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        
        // 추적 범위
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, _chaseRange);
        
        // 공격 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
        
        // 최대 추적 거리
        Gizmos.color = Color.blue;
        Vector3 spawnPos = Application.isPlaying ? _spawnPosition : transform.position;
        Gizmos.DrawWireSphere(spawnPos, _maxChaseDistance);
        
        // 시야각
        Gizmos.color = Color.green;
        Vector3 leftBoundary = Quaternion.Euler(0, -_fieldOfView * 0.5f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, _fieldOfView * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, leftBoundary * _detectionRange);
        Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, rightBoundary * _detectionRange);
        
        // 순찰 포인트
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
        }
    }
    
    #endregion
}
