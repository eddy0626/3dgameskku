using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 엘리트 몬스터 AI
/// 일반 좀비와 다른 고급 FSM 구현
///
/// FSM 상태:
/// - Idle: 대기 (플레이어 감지 전)
/// - Patrol: 순찰 (느린 이동)
/// - Alert: 경계 (플레이어 발견, 포효)
/// - Chase: 추적 (빠른 이동)
/// - Attack: 일반 공격 (근접)
/// - Charge: 돌진 공격 (거리가 있을 때)
/// - Stomp: 지면 강타 (범위 공격)
/// - Rage: 분노 모드 (체력 30% 이하)
/// - Recover: 회복 (분노 모드 진입 시 잠시 무적)
/// - Dead: 사망
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EliteEnemyAI : MonoBehaviour
{
    #region Enums

    public enum EliteState
    {
        Idle,       // 대기
        Patrol,     // 순찰
        Alert,      // 경계 (포효)
        Chase,      // 추적
        Attack,     // 일반 근접 공격
        Charge,     // 돌진 공격
        Stomp,      // 지면 강타 (범위)
        Rage,       // 분노 모드 전환
        Recover,    // 회복 중
        Dead        // 사망
    }

    #endregion

    #region Inspector Fields

    [Header("기본 설정")]
    [SerializeField] private string _targetTag = "Player";
    [SerializeField] private float _scale = 2.5f; // 좀비보다 2.5배 크기

    [Header("감지 설정")]
    [SerializeField] private float _detectionRange = 20f;
    [SerializeField] private float _fieldOfView = 150f;
    [SerializeField] private LayerMask _obstacleMask;

    [Header("순찰 설정")]
    [SerializeField] private Transform[] _patrolPoints;
    [SerializeField] private float _patrolSpeed = 1.5f;
    [SerializeField] private float _patrolWaitTime = 3f;

    [Header("추적 설정")]
    [SerializeField] private float _chaseSpeed = 6f;
    [SerializeField] private float _chaseRange = 30f;
    [SerializeField] private float _rotationSpeed = 8f;

    [Header("일반 공격")]
    [SerializeField] private float _attackRange = 3f;
    [SerializeField] private float _attackDamage = 40f;
    [SerializeField] private float _attackCooldown = 2f;
    [SerializeField] private float _attackAngle = 120f;

    [Header("돌진 공격")]
    [SerializeField] private float _chargeMinDistance = 8f;
    [SerializeField] private float _chargeMaxDistance = 15f;
    [SerializeField] private float _chargeSpeed = 15f;
    [SerializeField] private float _chargeDamage = 60f;
    [SerializeField] private float _chargeCooldown = 8f;
    [SerializeField] private float _chargeWindupTime = 1f;

    [Header("지면 강타 (로스트아크 스타일)")]
    [SerializeField] private float _stompRadius = 10f; // 범위 확대
    [SerializeField] private float _stompDamage = 50f;
    [SerializeField] private float _stompCooldown = 6f; // 쿨다운 (초)
    [SerializeField] private float _stompWindupTime = 2f; // 경고 표시 시간 (회피 시간)
    [SerializeField] private float _stompKnockbackForce = 10f;
    [SerializeField] private Color _stompIndicatorColor = new Color(1f, 0f, 0f, 0.4f);
    [SerializeField] private GameObject _groundCrackEffect; // 바닥 균열 이펙트
    [SerializeField] private GameObject _shockwaveEffect;   // 충격파 이펙트
    [SerializeField] private float _cameraShakeIntensity = 0.5f;
    [SerializeField] private float _cameraShakeDuration = 0.3f;

    [Header("분노 모드")]
    [SerializeField] private float _rageHealthThreshold = 0.3f; // 30% 이하 시 분노
    [SerializeField] private float _rageDamageMultiplier = 1.5f;
    [SerializeField] private float _rageSpeedMultiplier = 1.3f;
    [SerializeField] private float _rageRecoverDuration = 2f;
    [SerializeField] private float _rageRecoverHealth = 0.1f; // 10% 회복

    [Header("경계 설정")]
    [SerializeField] private float _alertDuration = 1.5f;

    [Header("이펙트")]
    [SerializeField] private GameObject _chargeTrailEffect;
    [SerializeField] private GameObject _stompEffect;
    [SerializeField] private GameObject _rageAuraEffect;
    [SerializeField] private Color _rageColor = new Color(1f, 0.3f, 0.3f);

    [Header("사운드")]
    [SerializeField] private AudioClip _roarSound;
    [SerializeField] private AudioClip _chargeSound;
    [SerializeField] private AudioClip _stompSound;
    [SerializeField] private AudioClip _rageSound;

    [Header("디버그")]
    [SerializeField] private bool _showGizmos = true;

    #endregion

    #region Private Fields

    private NavMeshAgent _agent;
    private EnemyHealth _health;
    private Animator _animator;
    private AudioSource _audioSource;

    // 애니메이션 파라미터
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");
    private static readonly int AnimCharge = Animator.StringToHash("Charge");
    private static readonly int AnimStomp = Animator.StringToHash("Jump"); // Jump Attack 애니메이션
    private static readonly int AnimRoar = Animator.StringToHash("Roar");
    private static readonly int AnimRage = Animator.StringToHash("IsRage");
    private static readonly int AnimDie = Animator.StringToHash("Die");
    private static readonly int AnimHit = Animator.StringToHash("Hit");

    // 애니메이터 파라미터 존재 여부 캐시
    private bool _hasSpeedParam;
    private bool _hasAttackParam;
    private bool _hasRageParam;
    private bool _hasDieParam;
    private bool _hasHitParam;

    // 상태 관련
    private EliteState _currentState = EliteState.Idle;
    private EliteState _previousState;
    private Transform _target;
    private Vector3 _spawnPosition;

    // 타이머
    private float _stateTimer;
    private float _attackTimer;
    private float _chargeTimer;
    private float _stompTimer;
    private float _patrolWaitTimer;

    // 플래그
    private bool _hasTarget;
    private bool _canSeeTarget;
    private bool _isRageMode;
    private bool _isCharging;
    private bool _hasAlerted; // 최초 발견 시 한 번만 포효

    // 순찰
    private int _currentPatrolIndex;

    // 돌진
    private Vector3 _chargeDirection;
    private Vector3 _chargeStartPos;

    // 렌더러 (분노 모드 색상 변경용)
    private Renderer[] _renderers;
    private Color[] _originalColors;

    // 분노 모드 이펙트
    private GameObject _activeRageAura;

    // 스탬프 범위 인디케이터
    private AreaIndicator _stompIndicator;

    #endregion

    #region Properties

    public EliteState CurrentState => _currentState;
    public bool IsRageMode => _isRageMode;
    public bool HasTarget => _hasTarget;
    public float HealthPercent => _health != null ? _health.HealthPercent : 1f;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<EnemyHealth>();
        _animator = GetComponent<Animator>();
        _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 1f;
        }

        _spawnPosition = transform.position;

        // 스케일 적용
        transform.localScale = Vector3.one * _scale;

        // 렌더러 캐싱
        CacheRenderers();
    }

    private void Start()
    {
        // 플레이어 찾기
        FindTarget();

        // 이벤트 구독
        if (_health != null)
        {
            _health.OnDeath += OnDeath;
            _health.OnDamaged += OnDamaged;
        }

        // 애니메이터 파라미터 존재 여부 확인
        CacheAnimatorParameters();

        // 초기 상태
        if (_patrolPoints != null && _patrolPoints.Length > 0)
        {
            ChangeState(EliteState.Patrol);
        }
        else
        {
            ChangeState(EliteState.Idle);
        }

        Debug.Log($"[EliteEnemyAI] {gameObject.name} 초기화 완료 - Scale: {_scale}x");
    }

    private void Update()
    {
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
            return;

        if (_currentState == EliteState.Dead) return;

        // 타겟 감지
        DetectTarget();

        // 분노 모드 체크
        CheckRageMode();

        // 쿨타임 업데이트
        UpdateCooldowns();

        // 상태별 업데이트
        UpdateState();

        // 애니메이션 업데이트
        UpdateAnimation();
    }

    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnDeath -= OnDeath;
            _health.OnDamaged -= OnDamaged;
        }

        if (_activeRageAura != null)
        {
            Destroy(_activeRageAura);
        }

        // 스탬프 인디케이터 정리
        if (_stompIndicator != null)
        {
            Destroy(_stompIndicator.gameObject);
        }
    }

    #endregion

    #region State Machine

    private void ChangeState(EliteState newState)
    {
        if (_currentState == newState) return;

        _previousState = _currentState;
        ExitState(_currentState);

        _currentState = newState;
        _stateTimer = 0f;

        EnterState(newState);

#if UNITY_EDITOR
        Debug.Log($"[EliteEnemyAI] {gameObject.name} 상태 변경: {_previousState} -> {newState}");
#endif
    }

    private void EnterState(EliteState state)
    {
        switch (state)
        {
            case EliteState.Idle:
                StopAgent();
                break;

            case EliteState.Patrol:
                ResumeAgent(_patrolSpeed);
                SetNextPatrolPoint();
                break;

            case EliteState.Alert:
                StopAgent();
                PlaySound(_roarSound);
                TriggerAnimation(AnimRoar);
                break;

            case EliteState.Chase:
                ResumeAgent(GetCurrentSpeed());
                break;

            case EliteState.Attack:
                StopAgent();
                break;

            case EliteState.Charge:
                StartCharge();
                break;

            case EliteState.Stomp:
                StopAgent();
                StartCoroutine(PerformStomp());
                break;

            case EliteState.Rage:
                EnterRageMode();
                break;

            case EliteState.Recover:
                StopAgent();
                StartCoroutine(PerformRecover());
                break;

            case EliteState.Dead:
                StopAgent();
                _agent.enabled = false;
                TriggerAnimation(AnimDie);
                break;
        }
    }

    private void ExitState(EliteState state)
    {
        switch (state)
        {
            case EliteState.Charge:
                EndCharge();
                break;
        }
    }

    private void UpdateState()
    {
        _stateTimer += Time.deltaTime;

        switch (_currentState)
        {
            case EliteState.Idle:
                UpdateIdle();
                break;
            case EliteState.Patrol:
                UpdatePatrol();
                break;
            case EliteState.Alert:
                UpdateAlert();
                break;
            case EliteState.Chase:
                UpdateChase();
                break;
            case EliteState.Attack:
                UpdateAttack();
                break;
            case EliteState.Charge:
                UpdateCharge();
                break;
            // Stomp, Rage, Recover는 코루틴에서 처리
        }
    }

    #endregion

    #region State Updates

    private void UpdateIdle()
    {
        if (_hasTarget)
        {
            if (!_hasAlerted)
            {
                _hasAlerted = true;
                ChangeState(EliteState.Alert);
            }
            else
            {
                ChangeState(EliteState.Chase);
            }
        }
    }

    private void UpdatePatrol()
    {
        if (_hasTarget)
        {
            if (!_hasAlerted)
            {
                _hasAlerted = true;
                ChangeState(EliteState.Alert);
            }
            else
            {
                ChangeState(EliteState.Chase);
            }
            return;
        }

        if (_patrolPoints == null || _patrolPoints.Length == 0)
        {
            ChangeState(EliteState.Idle);
            return;
        }

        if (_agent.isOnNavMesh && !_agent.pathPending && _agent.remainingDistance < 1f)
        {
            _patrolWaitTimer += Time.deltaTime;
            if (_patrolWaitTimer >= _patrolWaitTime)
            {
                _patrolWaitTimer = 0f;
                SetNextPatrolPoint();
            }
        }
    }

    private void UpdateAlert()
    {
        LookAtTarget();

        if (_stateTimer >= _alertDuration)
        {
            ChangeState(EliteState.Chase);
        }
    }

    private void UpdateChase()
    {
        if (_target == null || !_hasTarget)
        {
            ChangeState(EliteState.Patrol);
            return;
        }

        float distance = Vector3.Distance(transform.position, _target.position);

        // 공격 범위 내 진입
        if (distance <= _attackRange)
        {
            // 스탬프 공격 (쿨타임 완료 시 우선 사용)
            // 분노 모드에서는 항상, 일반 모드에서는 30% 확률
            if (_stompTimer <= 0f)
            {
                bool shouldStomp = _isRageMode || Random.value < 0.3f;
                if (shouldStomp)
                {
                    ChangeState(EliteState.Stomp);
                    return;
                }
            }

            ChangeState(EliteState.Attack);
            return;
        }

        // 돌진 거리 체크
        if (distance >= _chargeMinDistance && distance <= _chargeMaxDistance && _chargeTimer <= 0f)
        {
            ChangeState(EliteState.Charge);
            return;
        }

        // 추적 이동
        if (_agent.isOnNavMesh)
        {
            _agent.SetDestination(_target.position);
        }

        LookAtTarget();
    }

    private void UpdateAttack()
    {
        LookAtTarget();

        if (_attackTimer <= 0f)
        {
            PerformAttack();
            _attackTimer = _attackCooldown;

            // 공격 후 다시 추적
            if (_target != null)
            {
                float distance = Vector3.Distance(transform.position, _target.position);
                if (distance > _attackRange)
                {
                    ChangeState(EliteState.Chase);
                }
            }
        }
        else if (_stateTimer > 0.5f) // 공격 애니메이션 후
        {
            ChangeState(EliteState.Chase);
        }
    }

    private void UpdateCharge()
    {
        if (!_isCharging) return;

        // 돌진 이동
        transform.position += _chargeDirection * _chargeSpeed * Time.deltaTime;

        // 돌진 거리 체크
        float chargedDistance = Vector3.Distance(_chargeStartPos, transform.position);
        if (chargedDistance >= _chargeMaxDistance)
        {
            EndCharge();
            ChangeState(EliteState.Chase);
            return;
        }

        // 플레이어 충돌 체크
        CheckChargeHit();

        // 벽 충돌 체크
        if (Physics.Raycast(transform.position + Vector3.up, _chargeDirection, 1.5f, _obstacleMask))
        {
            EndCharge();
            ChangeState(EliteState.Chase);
        }
    }

    #endregion

    #region Combat Actions

    private void PerformAttack()
    {
        TriggerAnimation(AnimAttack);
        PlaySound(_roarSound);

        // 범위 내 플레이어 데미지
        if (_target != null)
        {
            float distance = Vector3.Distance(transform.position, _target.position);
            if (distance <= _attackRange)
            {
                Vector3 dirToTarget = (_target.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToTarget);

                if (angle <= _attackAngle * 0.5f)
                {
                    float damage = _attackDamage * (_isRageMode ? _rageDamageMultiplier : 1f);
                    DealDamageToPlayer(damage);
                }
            }
        }
    }

    private void StartCharge()
    {
        if (_target == null) return;

        _chargeTimer = _chargeCooldown;

        // 윈드업
        StopAgent();
        TriggerAnimation(AnimCharge);
        PlaySound(_chargeSound);

        // 돌진 방향 계산
        _chargeDirection = (_target.position - transform.position).normalized;
        _chargeDirection.y = 0f;
        _chargeStartPos = transform.position;

        // 방향 바라보기
        transform.rotation = Quaternion.LookRotation(_chargeDirection);

        // 이펙트
        if (_chargeTrailEffect != null)
        {
            var trail = Instantiate(_chargeTrailEffect, transform);
            Destroy(trail, 3f);
        }

        StartCoroutine(ChargeWindup());
    }

    private IEnumerator ChargeWindup()
    {
        yield return new WaitForSeconds(_chargeWindupTime);

        if (_currentState == EliteState.Charge)
        {
            _isCharging = true;
            _agent.enabled = false; // NavMeshAgent 비활성화하고 직접 이동
        }
    }

    private void CheckChargeHit()
    {
        if (_target == null) return;

        float distance = Vector3.Distance(transform.position, _target.position);
        if (distance <= 2f)
        {
            float damage = _chargeDamage * (_isRageMode ? _rageDamageMultiplier : 1f);
            DealDamageToPlayer(damage);

            // 넉백
            ApplyKnockbackToPlayer(_chargeDirection);

            EndCharge();
            ChangeState(EliteState.Chase);
        }
    }

    private void EndCharge()
    {
        _isCharging = false;
        _agent.enabled = true;

        if (_agent.isOnNavMesh)
        {
            _agent.Warp(transform.position);
        }
    }

    private IEnumerator PerformStomp()
    {
        _stompTimer = _stompCooldown;

        // 팔 들어올리기 애니메이션
        TriggerAnimation(AnimStomp);

        // === 1단계: 빨간색 범위 경고 표시 ===
        Vector3 stompPosition = transform.position;

        // 범위 인디케이터 생성
        _stompIndicator = AreaIndicator.Create(stompPosition, _stompRadius);
        _stompIndicator.Show(_stompWindupTime, null);

#if UNITY_EDITOR
        Debug.Log($"[EliteEnemyAI] 지면 강타 준비 - 범위 표시 시작 ({_stompWindupTime}초)");
#endif

        // 윈드업 동안 대기 (플레이어 회피 시간)
        yield return new WaitForSeconds(_stompWindupTime);

        if (_currentState != EliteState.Stomp)
        {
            // 상태가 변경되었으면 인디케이터 정리
            if (_stompIndicator != null)
            {
                Destroy(_stompIndicator.gameObject);
                _stompIndicator = null;
            }
            yield break;
        }

        // === 2단계: 내려치기 실행 ===
        PlaySound(_stompSound);

        // 인디케이터 정리
        if (_stompIndicator != null)
        {
            Destroy(_stompIndicator.gameObject);
            _stompIndicator = null;
        }

        // 지면 균열 이펙트
        if (_groundCrackEffect != null)
        {
            var crack = Instantiate(_groundCrackEffect, stompPosition, Quaternion.identity);
            Destroy(crack, 5f);
        }

        // 기본 스탬프 이펙트
        if (_stompEffect != null)
        {
            var effect = Instantiate(_stompEffect, stompPosition, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // 충격파 이펙트 (프리팹이 없으면 자동 생성)
        if (_shockwaveEffect != null)
        {
            var shockwave = Instantiate(_shockwaveEffect, stompPosition + Vector3.up * 0.1f, Quaternion.identity);
            Destroy(shockwave, 2f);
        }
        else
        {
            // GroundSlamEffect 자동 생성
            GroundSlamEffect.Create(stompPosition, _stompRadius);
        }

        // 카메라 흔들림
        TriggerCameraShake();

        // === 3단계: 범위 내 데미지 ===
        Collider[] hits = Physics.OverlapSphere(stompPosition, _stompRadius);
        int hitCount = 0;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                float damage = _stompDamage * (_isRageMode ? _rageDamageMultiplier : 1f);

                var playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Vector3 hitPoint = hit.transform.position + Vector3.up;
                    Vector3 hitNormal = (stompPosition - hit.transform.position).normalized;
                    playerHealth.TakeDamage(damage, hitPoint, hitNormal);
                    hitCount++;
                }

                // 넉백 (중심에서 바깥으로)
                Vector3 knockbackDir = (hit.transform.position - stompPosition).normalized;
                if (knockbackDir.sqrMagnitude < 0.01f)
                {
                    knockbackDir = Vector3.back; // 기본 방향
                }
                ApplyKnockbackToTarget(hit.transform, knockbackDir);
            }
        }

#if UNITY_EDITOR
        Debug.Log($"[EliteEnemyAI] 지면 강타 완료! 피해 대상: {hitCount}명, 데미지: {_stompDamage * (_isRageMode ? _rageDamageMultiplier : 1f)}");
#endif

        // 후딜레이
        yield return new WaitForSeconds(0.8f);

        if (_currentState == EliteState.Stomp)
        {
            ChangeState(EliteState.Chase);
        }
    }

    /// <summary>
    /// 카메라 흔들림 효과
    /// </summary>
    private void TriggerCameraShake()
    {
        // 메인 카메라 찾기
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        // DOTween으로 카메라 흔들림
        mainCam.transform.DOShakePosition(
            _cameraShakeDuration,
            _cameraShakeIntensity,
            10,
            90f,
            false,
            true
        );
    }

    #endregion

    #region Rage Mode

    private void CheckRageMode()
    {
        if (_isRageMode) return;
        if (_health == null) return;

        if (_health.HealthPercent <= _rageHealthThreshold)
        {
            ChangeState(EliteState.Rage);
        }
    }

    private void EnterRageMode()
    {
        _isRageMode = true;

        PlaySound(_rageSound);
        // AnimRage는 UpdateAnimation에서 Bool로 처리됨

        // 색상 변경
        SetRageVisuals(true);

        // 분노 오라 이펙트
        if (_rageAuraEffect != null)
        {
            _activeRageAura = Instantiate(_rageAuraEffect, transform);
        }

        Debug.Log($"[EliteEnemyAI] {gameObject.name} 분노 모드 진입!");

        // 잠시 후 회복 상태로
        StartCoroutine(RageTransition());
    }

    private IEnumerator RageTransition()
    {
        yield return new WaitForSeconds(1f);

        if (_currentState == EliteState.Rage)
        {
            ChangeState(EliteState.Recover);
        }
    }

    private IEnumerator PerformRecover()
    {
        // 무적 상태 (체력바 깜빡임 등으로 표시 가능)
        // TODO: 무적 처리

        // 회복
        if (_health != null)
        {
            float healAmount = _health.MaxHealth * _rageRecoverHealth;
            _health.Heal(healAmount);
        }

        yield return new WaitForSeconds(_rageRecoverDuration);

        if (_currentState == EliteState.Recover)
        {
            ChangeState(EliteState.Chase);
        }
    }

    private void SetRageVisuals(bool rage)
    {
        if (_renderers == null) return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;

            var mat = _renderers[i].material;
            string colorProp = mat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";

            if (mat.HasProperty(colorProp))
            {
                mat.SetColor(colorProp, rage ? _rageColor : _originalColors[i]);
            }

            // 이미션 추가 (분노 시 빛나는 효과)
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", rage ? _rageColor * 0.5f : Color.black);
            }
        }
    }

    #endregion

    #region Helper Methods

    private void FindTarget()
    {
        var targetObj = GameObject.FindGameObjectWithTag(_targetTag);
        if (targetObj != null)
        {
            _target = targetObj.transform;
        }
    }

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

        float distance = Vector3.Distance(transform.position, _target.position);

        if (distance > _detectionRange)
        {
            _canSeeTarget = false;
            // 추적 중이었다면 계속 추적
            if (_currentState == EliteState.Chase)
            {
                _hasTarget = distance < _chaseRange;
            }
            else
            {
                _hasTarget = false;
            }
            return;
        }

        // 시야각 체크
        Vector3 dirToTarget = (_target.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToTarget);

        if (angle > _fieldOfView * 0.5f)
        {
            _canSeeTarget = false;
            return;
        }

        // 장애물 체크
        Vector3 eyePos = transform.position + Vector3.up * 2f;
        Vector3 targetPos = _target.position + Vector3.up;

        if (Physics.Linecast(eyePos, targetPos, _obstacleMask))
        {
            _canSeeTarget = false;
            return;
        }

        _canSeeTarget = true;
        _hasTarget = true;
    }

    private void LookAtTarget()
    {
        if (_target == null) return;

        Vector3 dir = (_target.position - transform.position).normalized;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * _rotationSpeed);
        }
    }

    private void SetNextPatrolPoint()
    {
        if (_patrolPoints == null || _patrolPoints.Length == 0) return;

        _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Length;

        if (_patrolPoints[_currentPatrolIndex] != null && _agent.isOnNavMesh)
        {
            _agent.SetDestination(_patrolPoints[_currentPatrolIndex].position);
        }
    }

    private void StopAgent()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }
    }

    private void ResumeAgent(float speed)
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.speed = speed;
        }
    }

    private float GetCurrentSpeed()
    {
        float baseSpeed = _chaseSpeed;
        if (_isRageMode) baseSpeed *= _rageSpeedMultiplier;
        return baseSpeed;
    }

    private void UpdateCooldowns()
    {
        if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;
        if (_chargeTimer > 0f) _chargeTimer -= Time.deltaTime;
        if (_stompTimer > 0f) _stompTimer -= Time.deltaTime;
    }

    /// <summary>
    /// 애니메이터 파라미터 존재 여부 캐싱
    /// </summary>
    private void CacheAnimatorParameters()
    {
        if (_animator == null) return;

        foreach (var param in _animator.parameters)
        {
            if (param.nameHash == AnimSpeed) _hasSpeedParam = true;
            else if (param.nameHash == AnimAttack) _hasAttackParam = true;
            else if (param.nameHash == AnimRage) _hasRageParam = true;
            else if (param.nameHash == AnimDie) _hasDieParam = true;
            else if (param.nameHash == AnimHit) _hasHitParam = true;
        }
    }

    private void UpdateAnimation()
    {
        if (_animator == null) return;

        float speed = _agent != null && _agent.isOnNavMesh ? _agent.velocity.magnitude : 0f;

        if (_hasSpeedParam)
        {
            _animator.SetFloat(AnimSpeed, speed);
        }

        if (_hasRageParam)
        {
            _animator.SetBool(AnimRage, _isRageMode);
        }
    }

    private void TriggerAnimation(int hash)
    {
        if (_animator == null) return;

        // 안전한 트리거 호출 - 파라미터 존재 시에만 호출
        bool paramExists = false;
        foreach (var param in _animator.parameters)
        {
            if (param.nameHash == hash && param.type == AnimatorControllerParameterType.Trigger)
            {
                paramExists = true;
                break;
            }
        }

        if (paramExists)
        {
            _animator.SetTrigger(hash);
        }
        // 파라미터 없으면 무시 (Roar, Charge, Stomp 등 엘리트 전용)
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || _audioSource == null) return;
        _audioSource.PlayOneShot(clip);
    }

    private void CacheRenderers()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _originalColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            var mat = _renderers[i].material;
            string colorProp = mat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            if (mat.HasProperty(colorProp))
            {
                _originalColors[i] = mat.GetColor(colorProp);
            }
        }
    }

    private void DealDamageToPlayer(float damage)
    {
        if (_target == null) return;

        var playerHealth = _target.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            Vector3 hitPoint = _target.position + Vector3.up;
            Vector3 hitNormal = (transform.position - _target.position).normalized;
            playerHealth.TakeDamage(damage, hitPoint, hitNormal);
        }
    }

    private void ApplyKnockbackToPlayer(Vector3 direction)
    {
        if (_target == null) return;
        ApplyKnockbackToTarget(_target, direction);
    }

    private void ApplyKnockbackToTarget(Transform target, Vector3 direction)
    {
        var cc = target.GetComponent<CharacterController>();
        if (cc != null)
        {
            // CharacterController는 직접 Move로 넉백
            StartCoroutine(KnockbackCoroutine(target, direction));
        }

        var rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(direction * _stompKnockbackForce, ForceMode.Impulse);
        }
    }

    private IEnumerator KnockbackCoroutine(Transform target, Vector3 direction)
    {
        var cc = target.GetComponent<CharacterController>();
        if (cc == null) yield break;

        float duration = 0.2f;
        float timer = 0f;

        while (timer < duration)
        {
            cc.Move(direction * _stompKnockbackForce * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void OnDeath()
    {
        ChangeState(EliteState.Dead);

        if (_activeRageAura != null)
        {
            Destroy(_activeRageAura);
        }
    }

    private void OnDamaged(float damage, Vector3 hitPoint)
    {
        // 피격 시 플레이어 위치로 타겟 갱신
        if (!_hasTarget && _target != null)
        {
            _hasTarget = true;

            if (_currentState == EliteState.Idle || _currentState == EliteState.Patrol)
            {
                if (!_hasAlerted)
                {
                    _hasAlerted = true;
                    ChangeState(EliteState.Alert);
                }
                else
                {
                    ChangeState(EliteState.Chase);
                }
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

        // 스탬프 범위
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, _stompRadius);

        // 돌진 범위
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _chargeMinDistance);
        Gizmos.DrawWireSphere(transform.position, _chargeMaxDistance);

        // 시야각
        Gizmos.color = Color.green;
        Vector3 leftBoundary = Quaternion.Euler(0, -_fieldOfView * 0.5f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, _fieldOfView * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position + Vector3.up * 2f, leftBoundary * _detectionRange);
        Gizmos.DrawRay(transform.position + Vector3.up * 2f, rightBoundary * _detectionRange);
    }

    #endregion
}
