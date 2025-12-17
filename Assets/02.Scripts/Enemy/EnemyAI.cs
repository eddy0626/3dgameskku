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
        Hit,        // 피격 (경직)
        Jump,       // 점프 (Off-Mesh Link 이동)
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
    
    [Header("피격 설정")]
    [SerializeField] private float _hitStunDuration = 0.3f;
    [SerializeField] private bool _canBeStunned = true;
    
    [Header("점프 설정 (Off-Mesh Link)")]
    [SerializeField] private float _jumpHeight = 2f;
    [SerializeField] private float _jumpDuration = 0.6f;
    [SerializeField] private bool _useOffMeshLinks = true;
    [SerializeField] private AnimationCurve _jumpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("자동 점프 설정")]
    [SerializeField] private bool _enableAutoJump = true;
    [SerializeField] private float _autoJumpMaxDistance = 10f;
    [SerializeField] private float _autoJumpMaxHeightUp = 5f;
    [SerializeField] private float _autoJumpMaxHeightDown = 8f;
    [SerializeField] private float _autoJumpCooldown = 1f;
    [SerializeField] private float _autoJumpCheckInterval = 0.3f;
    [SerializeField] private LayerMask _autoJumpObstacleMask;
    
    
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
    
    // 피격 상태 관련
    private float _hitTimer;
    private EnemyState _previousState;
    private bool _canSeeTarget;
    
    // 점프 상태 관련
    private bool _isJumping;
    private Vector3 _jumpStartPos;
    private Vector3 _jumpEndPos;
    private float _jumpTimer;
    private Coroutine _jumpCoroutine;

    // 자동 점프 관련
    private float _autoJumpCooldownTimer;
    private float _autoJumpCheckTimer;
    private bool _isAutoJump;

    
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
        
        // NavMeshAgent Off-Mesh Link 설정
        if (_agent != null)
        {
            // Off-Mesh Link 수동 순회 (점프 애니메이션 적용을 위해 false)
            _agent.autoTraverseOffMeshLink = false;
        }
    }
    
private void Start()
    {
        // 초기 플레이어 찾기
        FindTarget();
        
        // 체력 이벤트 구독
        if (_health != null)
        {
            _health.OnDeath += OnDeath;
            _health.OnDamaged += OnHit;
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
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.speed = _patrolSpeed;
            }
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
        // 게임 상태가 Playing이 아니면 AI 동작 정지
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
        {
            return;
        }

        if (_currentState == EnemyState.Dead) return;

        // Off-Mesh Link 처리 (NavMesh Link 자동 순회)
        HandleOffMeshLink();

        // 타겟 감지
        DetectTarget();

        // 자동 점프 체크 (타겟이 있고 높이 차이가 있으면 상태와 관계없이 점프)
        if (_hasTarget && _currentState != EnemyState.Jump && _agent != null && _agent.isOnNavMesh)
        {
            CheckAutoJump();
        }

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
            case EnemyState.Hit:
                UpdateHit();
                break;
        }
    }
    
    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnDeath -= OnDeath;
            _health.OnDamaged -= OnHit;
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
/// <summary>
    /// 타겟 감지 (시야 + 거리 + NavMesh 경로 기반)
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

        // 높이 차이 계산
        float heightDiff = Mathf.Abs(transform.position.y - _target.position.y);

        // 수평 거리 계산
        float horizontalDist = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(_target.position.x, 0, _target.position.z)
        );

        // 높이 차이가 크면 점프 가능 여부 확인
        if (heightDiff > _detectionHeight)
        {
            // 자동 점프 범위 내면 타겟으로 인식 (NavMesh 경로 없어도 점프 가능)
            if (_enableAutoJump && horizontalDist <= _autoJumpMaxDistance &&
                heightDiff <= Mathf.Max(_autoJumpMaxHeightUp, _autoJumpMaxHeightDown))
            {
                _canSeeTarget = true;
                _hasTarget = true;
                _lastKnownTargetPosition = _target.position;
                _loseTargetTimer = 0f;
                return;
            }

            // NavMesh 경로 유효성 검사
            if (!CanReachTargetViaNavMesh())
            {
                _canSeeTarget = false;
                return;
            }
        }

        // 시야각 체크
        Vector3 directionToTarget = (_target.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);

        if (angle > _fieldOfView * 0.5f)
        {
            // 시야각 밖이지만 점프 범위 내면 감지
            if (heightDiff > 1f && _enableAutoJump && horizontalDist <= _autoJumpMaxDistance)
            {
                _canSeeTarget = false;
                _hasTarget = true;
                _lastKnownTargetPosition = _target.position;
                _loseTargetTimer = 0f;
                return;
            }
            _canSeeTarget = false;
            return;
        }

        // 장애물 체크 (레이캐스트) - 같은 층일 때만
        if (heightDiff <= _detectionHeight)
        {
            Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
            Vector3 targetPosition = _target.position + Vector3.up * 1f;

            if (Physics.Linecast(eyePosition, targetPosition, _obstacleMask))
            {
                _canSeeTarget = false;
                return;
            }
        }

        // 타겟 발견!
        _canSeeTarget = true;
        _hasTarget = true;
        _lastKnownTargetPosition = _target.position;
        _loseTargetTimer = 0f;
    }

/// <summary>
    /// NavMesh 경로로 타겟에 도달 가능한지 확인
    /// </summary>
    private bool CanReachTargetViaNavMesh()
    {
        if (_target == null || _agent == null || !_agent.isOnNavMesh) return false;
        
        NavMeshPath path = new NavMeshPath();
        
        // 타겟 위치로 경로 계산
        if (_agent.CalculatePath(_target.position, path))
        {
            // 경로가 완전하거나 부분적으로 유효한 경우
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                return true;
            }
            // 부분 경로도 허용 (가까이는 지점까지라도 이동)
            else if (path.status == NavMeshPathStatus.PathPartial)
            {
                // 부분 경로의 끝점이 타겟과 가까운지 확인
                if (path.corners.Length > 0)
                {
                    Vector3 endPoint = path.corners[path.corners.Length - 1];
                    float distToTarget = Vector3.Distance(endPoint, _target.position);
                    // 5미터 이내면 유효한 경로로 판단
                    return distToTarget < 5f;
                }
            }
        }
        
        return false;
    }

/// <summary>
    /// Off-Mesh Link (NavMesh Link) 자동 순회 처리
    /// </summary>
/// <summary>
    /// Off-Mesh Link (NavMesh Link) 점프 처리
    /// </summary>
/// <summary>
    /// Off-Mesh Link (NavMesh Link) 수동 순회 처리 - 점프 애니메이션 적용
    /// </summary>
    private void HandleOffMeshLink()
    {
        if (_agent == null || !_agent.isOnNavMesh) return;
        if (!_useOffMeshLinks) return;
        
        // Off-Mesh Link 위에 있고 점프 중이 아닌 경우
        if (_agent.isOnOffMeshLink && !_isJumping)
        {
            // 점프 시작!
            StartJump();
            Debug.Log($"[EnemyAI] {gameObject.name} Off-Mesh Link 감지 - 점프 시작!");
        }
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
                if (_agent != null && _agent.isOnNavMesh)
                    _agent.isStopped = true;
                break;

            case EnemyState.Patrol:
                if (_agent != null && _agent.isOnNavMesh)
                {
                    _agent.isStopped = false;
                    _agent.speed = _patrolSpeed;
                }
                SetNextPatrolPoint();
                break;

            case EnemyState.Chase:
                if (_agent != null && _agent.isOnNavMesh)
                {
                    _agent.isStopped = false;
                    _agent.speed = _chaseSpeed;
                }
                break;

            case EnemyState.Attack:
                if (_agent != null && _agent.isOnNavMesh)
                    _agent.isStopped = true;
                break;

            case EnemyState.Return:
                if (_agent != null && _agent.isOnNavMesh)
                {
                    _agent.isStopped = false;
                    _agent.speed = _patrolSpeed;
                    _agent.SetDestination(_spawnPosition);
                }
                break;

            case EnemyState.Dead:
                if (_agent != null && _agent.isOnNavMesh)
                    _agent.isStopped = true;
                _agent.enabled = false;
                break;

            case EnemyState.Hit:
                if (_agent != null && _agent.isOnNavMesh)
                    _agent.isStopped = true;
                _hitTimer = 0f;
                // TODO: 피격 애니메이션 트리거
                break;

            case EnemyState.Jump:
                if (_agent != null && _agent.isOnNavMesh)
                    _agent.isStopped = true;
                // 점프 시작은 StartJump()에서 처리
                break;

        }
    }
    
    /// <summary>
    /// 상태 종료 처리
    /// </summary>
/// <summary>
    /// 상태 종료 처리
    /// </summary>
    private void ExitState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Attack:
                // 연사 중지
                if (_attack != null)
                {
                    _attack.StopFiring();
                }
                break;
        }
    }
    
    #endregion
    
    #region State Updates
    
    /// <summary>
    /// 대기 상태 업데이트
    /// </summary>
/// <summary>
    /// 대기 상태 업데이트
    /// </summary>
    private void UpdateIdle()
    {
        // 시야로 발견하거나 경로 기반으로 감지한 경우 추적
        if (_canSeeTarget || _hasTarget)
        {
            ChangeState(EnemyState.Chase);
        }
    }
    
    /// <summary>
    /// 순찰 상태 업데이트
    /// </summary>
/// <summary>
    /// 순찰 상태 업데이트
    /// </summary>
    private void UpdatePatrol()
    {
        // 시야로 발견하거나 경로 기반으로 감지한 경우 추적
        if (_canSeeTarget || _hasTarget)
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
        if (_agent != null && _agent.isOnNavMesh && !_agent.pathPending && _agent.remainingDistance < 0.5f)
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
        
        // 공격 범위 진입 시 공격 (높이 차이가 크면 공격 불가)
        float heightDiffToTarget = Mathf.Abs(_target.position.y - transform.position.y);
        if (distanceToTarget <= _attackRange && heightDiffToTarget < 1.5f)
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
        if (_agent != null && _agent.isOnNavMesh)
        {
            if (_canSeeTarget)
            {
                _agent.SetDestination(_target.position);
            }
            else
            {
                // 마지막 위치로 이동
                _agent.SetDestination(_lastKnownTargetPosition);
            }
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
        float heightDiff = Mathf.Abs(_target.position.y - transform.position.y);

        // 높이 차이가 크면 추적으로 전환 (점프 필요)
        if (heightDiff >= 1.5f)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

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
        if (_agent != null && _agent.isOnNavMesh && !_agent.pathPending && _agent.remainingDistance < 0.5f)
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

    /// <summary>
    /// 피격 상태 업데이트 (경직 후 이전 상태로 복귀)
    /// </summary>
    private void UpdateHit()
    {
        _hitTimer += Time.deltaTime;
        
        // 경직 시간 종료 시 이전 상태로 복귀
        if (_hitTimer >= _hitStunDuration)
        {
            // 피격 중 타깃을 볼 수 있으면 추적
            if (_canSeeTarget && _hasTarget)
            {
                ChangeState(EnemyState.Chase);
            }
            else
            {
                // 이전 상태로 복귀 (사망/피격 제외)
                EnemyState returnState = _previousState;
                if (returnState == EnemyState.Dead || returnState == EnemyState.Hit)
                {
                    returnState = EnemyState.Idle;
                }
                ChangeState(returnState);
            }
        }
    }

    /// <summary>
    /// 점프 상태 업데이트 (코루틴에서 처리되므로 추가 로직 불필요)
    /// </summary>
    private void UpdateJump()
    {
        // 점프 중 타겟 감지는 계속 수행
        // 코루틴에서 점프 완료 후 상태 변경 처리
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

        if (_patrolPoints[_currentPatrolIndex] != null && _agent != null && _agent.isOnNavMesh)
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
    
    #region Auto Jump

    /// <summary>
    /// 자동 점프 체크 (추적 중 호출)
    /// </summary>
    private void CheckAutoJump()
    {
        if (!_enableAutoJump || _isJumping || _target == null) return;

        // 쿨다운 타이머 감소
        if (_autoJumpCooldownTimer > 0f)
        {
            _autoJumpCooldownTimer -= Time.deltaTime;
            return;
        }

        // 체크 간격 타이머
        _autoJumpCheckTimer -= Time.deltaTime;
        if (_autoJumpCheckTimer > 0f) return;
        _autoJumpCheckTimer = _autoJumpCheckInterval;

        // 높이 차이 계산
        float heightDiff = _target.position.y - transform.position.y;
        float absHeightDiff = Mathf.Abs(heightDiff);

        // 높이 차이가 1m 이상이면 점프 필요!
        if (absHeightDiff >= 1.0f)
        {
            // 수평 거리 계산
            float horizontalDist = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(_target.position.x, 0, _target.position.z)
            );

            // 점프 범위 내면 무조건 점프 시도
            if (horizontalDist <= _autoJumpMaxDistance)
            {
                TryAutoJump();
            }
        }
    }

    /// <summary>
    /// NavMesh 경로 길이 계산
    /// </summary>
    private float GetPathLength()
    {
        if (_agent == null || !_agent.hasPath) return float.MaxValue;

        float length = 0f;
        Vector3[] corners = _agent.path.corners;

        for (int i = 0; i < corners.Length - 1; i++)
        {
            length += Vector3.Distance(corners[i], corners[i + 1]);
        }

        return length;
    }

    /// <summary>
    /// 자동 점프 시도
    /// </summary>
    private void TryAutoJump()
    {
        if (_target == null) return;

        // 점프 가능 여부 및 착지점 계산
        if (CanAutoJumpToTarget(out Vector3 landingPoint))
        {
            StartAutoJump(landingPoint);
        }
    }

    /// <summary>
    /// 자동 점프 가능 여부 판단 및 착지점 계산
    /// </summary>
    private bool CanAutoJumpToTarget(out Vector3 landingPoint)
    {
        landingPoint = Vector3.zero;

        if (_target == null) return false;

        Vector3 targetPos = _target.position;
        Vector3 myPos = transform.position;

        // 높이 차이 계산
        float heightDiff = targetPos.y - myPos.y;

        // 위로 점프 제한
        if (heightDiff > _autoJumpMaxHeightUp) return false;

        // 아래로 점프 제한
        if (heightDiff < -_autoJumpMaxHeightDown) return false;

        // 착지점 찾기 (타겟 근처의 NavMesh 위치)
        // 여러 위치에서 NavMesh 샘플링 시도
        Vector3[] sampleOffsets = new Vector3[]
        {
            Vector3.zero,
            Vector3.forward * 1f,
            Vector3.back * 1f,
            Vector3.left * 1f,
            Vector3.right * 1f
        };

        foreach (var offset in sampleOffsets)
        {
            Vector3 samplePos = targetPos + offset;
            if (NavMesh.SamplePosition(samplePos, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
            {
                // 착지점이 타겟과 비슷한 높이인지 확인
                if (Mathf.Abs(navHit.position.y - targetPos.y) < 1f)
                {
                    landingPoint = navHit.position;

                    // 간단한 장애물 체크 (머리 위 공간만 확인)
                    Vector3 jumpPeak = (myPos + landingPoint) / 2f + Vector3.up * (_jumpHeight + 1f);
                    if (!Physics.Linecast(myPos + Vector3.up, jumpPeak, _autoJumpObstacleMask))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 자동 점프 시작 (NavMeshLink 없이)
    /// </summary>
    private void StartAutoJump(Vector3 targetPosition)
    {
        if (_isJumping) return;

        // 쿨다운 설정
        _autoJumpCooldownTimer = _autoJumpCooldown;

        // 이전 상태 저장
        _previousState = _currentState;

        // 점프 상태로 변경
        ChangeState(EnemyState.Jump);

        // 점프 위치 설정
        _jumpStartPos = transform.position;
        _jumpEndPos = targetPosition + Vector3.up * _agent.baseOffset;

        // 자동 점프 플래그 설정
        _isAutoJump = true;

        // NavMeshAgent 일시 정지
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.updatePosition = false;
        }

        // 점프 코루틴 시작
        _jumpCoroutine = StartCoroutine(DoJump());

        Debug.Log($"[EnemyAI] {gameObject.name} 자동 점프! 높이차: {(_jumpEndPos.y - _jumpStartPos.y):F1}m");
    }

    #endregion

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
    /// 점프 시작 (Off-Mesh Link 감지 시 호출)
    /// </summary>
    private void StartJump()
    {
        if (_isJumping) return;

        // 이전 상태 저장
        _previousState = _currentState;

        // 점프 상태로 변경
        ChangeState(EnemyState.Jump);

        // Off-Mesh Link 데이터 얻기
        OffMeshLinkData linkData = _agent.currentOffMeshLinkData;
        _jumpStartPos = transform.position;
        _jumpEndPos = linkData.endPos + Vector3.up * _agent.baseOffset;

        // Off-Mesh Link 점프 플래그
        _isAutoJump = false;

        // 점프 코루틴 시작
        _jumpCoroutine = StartCoroutine(DoJump());

        Debug.Log($"[EnemyAI] {gameObject.name} Off-Mesh Link 점프 시작: {_jumpStartPos} -> {_jumpEndPos}");
    }
    
    /// <summary>
    /// 점프 코루틴 (포물선 이동)
    /// </summary>
    private System.Collections.IEnumerator DoJump()
    {
        _isJumping = true;
        _jumpTimer = 0f;
        
        // 점프 방향 바라보기
        Vector3 jumpDirection = (_jumpEndPos - _jumpStartPos).normalized;
        jumpDirection.y = 0f;
        if (jumpDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(jumpDirection);
        }
        
        // 포물선 점프 애니메이션
        while (_jumpTimer < _jumpDuration)
        {
            _jumpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_jumpTimer / _jumpDuration);
            
            // 수평 이동 (선형 보간)
            Vector3 horizontalPos = Vector3.Lerp(_jumpStartPos, _jumpEndPos, t);
            
            // 수직 이동 (포물선 곡선)
            float heightOffset = CalculateJumpHeight(t);
            
            // 최종 위치 설정
            transform.position = new Vector3(horizontalPos.x, horizontalPos.y + heightOffset, horizontalPos.z);
            
            yield return null;
        }
        
        // 점프 완료
        CompleteJump();
    }
    
    /// <summary>
    /// 점프 높이 계산 (포물선)
    /// </summary>
    private float CalculateJumpHeight(float t)
    {
        // 시작/끝 높이 차이 계산
        float heightDiff = _jumpEndPos.y - _jumpStartPos.y;
        
        // 포물선 곡선 (4 * h * t * (1 - t))
        float parabola = 4f * _jumpHeight * t * (1f - t);
        
        // AnimationCurve 적용 (선택적)
        float curveValue = _jumpCurve.Evaluate(t);
        
        // 기본 높이 보정 + 포물선
        return parabola;
    }
    
    /// <summary>
    /// 점프 완료 처리
    /// </summary>
    private void CompleteJump()
    {
        _isJumping = false;
        _jumpCoroutine = null;

        // 최종 위치 설정
        transform.position = _jumpEndPos;

        if (_isAutoJump)
        {
            // 자동 점프 완료: NavMeshAgent 위치 동기화 및 재활성화
            _isAutoJump = false;

            if (_agent != null)
            {
                _agent.updatePosition = true;

                // NavMesh 위에 위치 동기화
                if (NavMesh.SamplePosition(_jumpEndPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    _agent.Warp(hit.position);
                }

                if (_agent.isOnNavMesh)
                {
                    _agent.isStopped = false;
                }
            }

            Debug.Log($"[EnemyAI] {gameObject.name} 자동 점프 완료!");
        }
        else
        {
            // Off-Mesh Link 점프 완료
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.CompleteOffMeshLink();
                _agent.isStopped = false;
            }

            Debug.Log($"[EnemyAI] {gameObject.name} Off-Mesh Link 점프 완료");
        }

        // 이전 상태로 복귀 또는 타겟 추적
        if (_canSeeTarget && _hasTarget)
        {
            ChangeState(EnemyState.Chase);
        }
        else if (_previousState == EnemyState.Patrol || _previousState == EnemyState.Return)
        {
            ChangeState(_previousState);
        }
        else
        {
            ChangeState(EnemyState.Chase);
        }
    }
    
    /// <summary>
    /// 사망 처리
    /// </summary>
    private void OnDeath()
    {
        ChangeState(EnemyState.Dead);
    }

    /// <summary>
    /// 피격 처리 (EnemyHealth.OnDamaged 이벤트에서 호출)
    /// </summary>
    private void OnHit(float damage, Vector3 hitPoint)
    {
        // 사망 상태이거나 경직 불가 시 무시
        if (_currentState == EnemyState.Dead || !_canBeStunned) return;
        
        // 이미 피격 중이면 타이머만 리셋
        if (_currentState == EnemyState.Hit)
        {
            _hitTimer = 0f;
            return;
        }
        
        // 현재 상태 저장 (복귀용)
        _previousState = _currentState;
        
        // 피격 상태로 전환
        ChangeState(EnemyState.Hit);
        
        Debug.Log($"[EnemyAI] {gameObject.name} 피격! 데미지: {damage:F1}, 이전 상태: {_previousState}");
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
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.SetDestination(alertPosition);
            }
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
