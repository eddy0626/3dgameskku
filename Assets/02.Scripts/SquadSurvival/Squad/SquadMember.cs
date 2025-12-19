using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using System.Collections;

namespace SquadSurvival.Squad
{
    /// <summary>
    /// 분대원 개별 AI 컴포넌트
    /// 플레이어를 따라다니며 적을 자동 공격
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class SquadMember : MonoBehaviour
    {
        public enum SquadMemberState
        {
            Idle,           // 대기
            Following,      // 플레이어 따라가기
            Attacking,      // 적 공격
            Repositioning,  // 위치 재조정
            Dead            // 사망
        }

        [Header("분대원 정보")]
        [SerializeField] private string memberName = "Squad Member";
        [SerializeField] private int memberIndex = 0;
        [SerializeField] private Sprite memberIcon;

        [Header("체력")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float healthRegenRate = 2f;
        [SerializeField] private float healthRegenDelay = 5f;

        [Header("전투")]
        [SerializeField] private float attackRange = 15f;
        [SerializeField] private float attackDamage = 20f;
        [SerializeField] private float attackRate = 2f;
        [SerializeField] private float detectionRange = 20f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("이동")]
        [SerializeField] private float followDistance = 3f;
        [SerializeField] private float minFollowDistance = 2f;
        [SerializeField] private float maxFollowDistance = 8f;
        [SerializeField] private float repositionDistance = 5f;

        [Header("시각 효과")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private LineRenderer bulletTrail;
        [SerializeField] private float trailDuration = 0.05f;

        [Header("사운드")]
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip deathSound;

        [Header("이벤트")]
        public UnityEvent<float, float> OnHealthChanged;
        public UnityEvent OnDeath;
        public UnityEvent<Transform> OnTargetChanged;

        // 상태
        public SquadMemberState CurrentState { get; private set; } = SquadMemberState.Idle;
        public bool IsAlive => currentHealth > 0f;
        public float HealthPercent => currentHealth / maxHealth;
        public string MemberName => memberName;
        public int MemberIndex => memberIndex;
        public Sprite MemberIcon => memberIcon;
        public Transform CurrentTarget { get; private set; }

        // 컴포넌트
        private NavMeshAgent agent;
        private Animator animator;
        private AudioSource audioSource;
        private Transform playerTransform;

        // 내부 변수
        private float lastAttackTime;
        private float lastDamageTime;
        private Vector3 targetPosition;
        private Coroutine attackCoroutine;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int DeathHash = Animator.StringToHash("IsDead");

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }

            currentHealth = maxHealth;

            if (OnHealthChanged == null) OnHealthChanged = new UnityEvent<float, float>();
            if (OnDeath == null) OnDeath = new UnityEvent();
            if (OnTargetChanged == null) OnTargetChanged = new UnityEvent<Transform>();
        }

        private void Start()
        {
            // 플레이어 참조
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }

            // 적 레이어 자동 설정
            if (enemyLayer == 0)
            {
                enemyLayer = LayerMask.GetMask("Enemy");
            }

            SetState(SquadMemberState.Following);
        }

        private void Update()
        {
            if (!IsAlive) return;

            UpdateState();
            UpdateAnimation();
            RegenerateHealth();
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public void Initialize(int index, string name, Transform player)
        {
            memberIndex = index;
            memberName = name;
            playerTransform = player;
            currentHealth = maxHealth;
            SetState(SquadMemberState.Following);
        }

        /// <summary>
        /// 상태 업데이트
        /// </summary>
        private void UpdateState()
        {
            switch (CurrentState)
            {
                case SquadMemberState.Following:
                    UpdateFollowing();
                    break;

                case SquadMemberState.Attacking:
                    UpdateAttacking();
                    break;

                case SquadMemberState.Repositioning:
                    UpdateRepositioning();
                    break;

                case SquadMemberState.Idle:
                    // 적 탐색
                    FindTarget();
                    if (CurrentTarget == null && playerTransform != null)
                    {
                        SetState(SquadMemberState.Following);
                    }
                    break;
            }
        }

        /// <summary>
        /// 따라가기 상태 업데이트
        /// </summary>
        private void UpdateFollowing()
        {
            if (playerTransform == null) return;

            // 적 탐색
            FindTarget();
            if (CurrentTarget != null)
            {
                SetState(SquadMemberState.Attacking);
                return;
            }

            // 플레이어와의 거리 확인
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer > maxFollowDistance)
            {
                // 너무 멀면 따라가기
                Vector3 followPos = GetFollowPosition();
                agent.SetDestination(followPos);
            }
            else if (distanceToPlayer < minFollowDistance)
            {
                // 너무 가까우면 약간 뒤로
                Vector3 awayDir = (transform.position - playerTransform.position).normalized;
                agent.SetDestination(transform.position + awayDir * 2f);
            }
            else if (!agent.hasPath || agent.remainingDistance < 0.5f)
            {
                // 적당한 거리면 정지
                agent.ResetPath();
            }
        }

        /// <summary>
        /// 공격 상태 업데이트
        /// </summary>
        private void UpdateAttacking()
        {
            // 타겟 유효성 확인
            if (CurrentTarget == null || !IsTargetValid(CurrentTarget))
            {
                CurrentTarget = null;
                FindTarget();

                if (CurrentTarget == null)
                {
                    SetState(SquadMemberState.Following);
                    return;
                }
            }

            float distanceToTarget = Vector3.Distance(transform.position, CurrentTarget.position);

            // 공격 범위 밖이면 접근
            if (distanceToTarget > attackRange)
            {
                agent.SetDestination(CurrentTarget.position);
            }
            else
            {
                // 공격 범위 내면 공격
                agent.ResetPath();
                
                // 타겟을 바라보기
                Vector3 lookDir = CurrentTarget.position - transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(lookDir),
                        Time.deltaTime * 10f
                    );
                }

                // 공격 실행
                if (Time.time - lastAttackTime >= 1f / attackRate)
                {
                    Attack();
                }
            }

            // 플레이어와 너무 멀어지면 복귀
            if (playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distanceToPlayer > maxFollowDistance * 2f)
                {
                    CurrentTarget = null;
                    SetState(SquadMemberState.Following);
                }
            }
        }

        /// <summary>
        /// 위치 재조정 상태 업데이트
        /// </summary>
        private void UpdateRepositioning()
        {
            if (!agent.hasPath || agent.remainingDistance < 0.5f)
            {
                SetState(SquadMemberState.Following);
            }
        }

        /// <summary>
        /// 적 탐색
        /// </summary>
        private void FindTarget()
        {
            Transform closestEnemy = null;
            float closestDistance = float.MaxValue;

            // 1. 레이어 기반 탐색
            if (enemyLayer != 0)
            {
                Collider[] enemies = Physics.OverlapSphere(transform.position, detectionRange, enemyLayer);
                foreach (var enemy in enemies)
                {
                    if (!IsTargetValid(enemy.transform)) continue;

                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy.transform;
                    }
                }
            }

            // 2. 태그 기반 탐색 (레이어에서 못 찾은 경우)
            if (closestEnemy == null)
            {
                GameObject[] taggedEnemies = GameObject.FindGameObjectsWithTag("Enemy");
                foreach (var enemy in taggedEnemies)
                {
                    if (!IsTargetValid(enemy.transform)) continue;

                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance <= detectionRange && distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy.transform;
                    }
                }
            }

            if (closestEnemy != CurrentTarget)
            {
                CurrentTarget = closestEnemy;
                OnTargetChanged?.Invoke(CurrentTarget);

#if UNITY_EDITOR
                if (CurrentTarget != null)
                {
                    Debug.Log($"[SquadMember] {memberName} 새 타겟: {CurrentTarget.name}, 거리: {closestDistance:F1}m");
                }
#endif
            }
        }

        /// <summary>
        /// 타겟 유효성 확인
        /// </summary>
        private bool IsTargetValid(Transform target)
        {
            if (target == null) return false;
            if (!target.gameObject.activeInHierarchy) return false;

            // EnemyHealth 확인
            var enemyHealth = target.GetComponent<EnemyHealth>();
            if (enemyHealth != null && !enemyHealth.IsAlive)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 공격 실행
        /// </summary>
        private void Attack()
        {
            if (CurrentTarget == null) return;

            lastAttackTime = Time.time;

            // 애니메이션
            if (animator != null)
            {
                animator.SetTrigger(AttackHash);
            }

            // 데미지 적용
            var enemyHealth = CurrentTarget.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                Vector3 hitPoint = CurrentTarget.position + Vector3.up;
                Vector3 hitNormal = (transform.position - CurrentTarget.position).normalized;
                enemyHealth.TakeDamage(attackDamage, hitPoint, hitNormal);
            }

            // 이펙트
            PlayFireEffect();
            PlaySound(fireSound);

#if UNITY_EDITOR
            Debug.Log($"[SquadMember] {memberName} attacked {CurrentTarget.name} for {attackDamage} damage");
#endif
        }

        /// <summary>
        /// 발사 이펙트
        /// </summary>
        private void PlayFireEffect()
        {
            // 머즐 플래시
            if (muzzleFlashPrefab != null && firePoint != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
                Destroy(flash, 0.1f);
            }

            // 총알 궤적
            if (bulletTrail != null && firePoint != null && CurrentTarget != null)
            {
                StartCoroutine(ShowBulletTrail());
            }
        }

        private IEnumerator ShowBulletTrail()
        {
            bulletTrail.enabled = true;
            bulletTrail.SetPosition(0, firePoint.position);
            bulletTrail.SetPosition(1, CurrentTarget.position + Vector3.up);
            yield return new WaitForSeconds(trailDuration);
            bulletTrail.enabled = false;
        }

        /// <summary>
        /// 따라가기 위치 계산
        /// </summary>
        private Vector3 GetFollowPosition()
        {
            if (playerTransform == null) return transform.position;

            // 플레이어 뒤쪽 양옆에 배치
            float angle = (memberIndex - 1.5f) * 45f;
            Vector3 offset = Quaternion.Euler(0, angle, 0) * (-playerTransform.forward) * followDistance;
            Vector3 targetPos = playerTransform.position + offset;

            // NavMesh 위 위치 찾기
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 5f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return targetPos;
        }

        /// <summary>
        /// 데미지 받기
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            lastDamageTime = Time.time;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            PlaySound(hitSound);

#if UNITY_EDITOR
            Debug.Log($"[SquadMember] {memberName} took {damage} damage. HP: {currentHealth}/{maxHealth}");
#endif

            if (!IsAlive)
            {
                Die();
            }
        }

        /// <summary>
        /// 체력 회복
        /// </summary>
        public void Heal(float amount)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// 체력 자동 재생
        /// </summary>
        private void RegenerateHealth()
        {
            if (!IsAlive) return;
            if (currentHealth >= maxHealth) return;
            if (Time.time - lastDamageTime < healthRegenDelay) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + healthRegenRate * Time.deltaTime);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// 사망
        /// </summary>
        private void Die()
        {
            SetState(SquadMemberState.Dead);
            
            if (agent != null)
            {
                agent.enabled = false;
            }

            if (animator != null)
            {
                animator.SetBool(DeathHash, true);
            }

            PlaySound(deathSound);
            OnDeath?.Invoke();

#if UNITY_EDITOR
            Debug.Log($"[SquadMember] {memberName} died!");
#endif
        }

        /// <summary>
        /// 부활
        /// </summary>
        public void Revive(float healthPercent = 0.5f)
        {
            currentHealth = maxHealth * healthPercent;
            
            if (agent != null)
            {
                agent.enabled = true;
            }

            if (animator != null)
            {
                animator.SetBool(DeathHash, false);
            }

            SetState(SquadMemberState.Following);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

#if UNITY_EDITOR
            Debug.Log($"[SquadMember] {memberName} revived with {healthPercent:P0} health");
#endif
        }

        /// <summary>
        /// 상태 변경
        /// </summary>
        private void SetState(SquadMemberState newState)
        {
            CurrentState = newState;

#if UNITY_EDITOR
            Debug.Log($"[SquadMember] {memberName} state changed to: {newState}");
#endif
        }

        /// <summary>
        /// 애니메이션 업데이트
        /// </summary>
        private void UpdateAnimation()
        {
            if (animator == null) return;

            float speed = agent.velocity.magnitude / agent.speed;
            animator.SetFloat(SpeedHash, speed);
        }

        /// <summary>
        /// 사운드 재생
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip);
        }

        private void OnDrawGizmosSelected()
        {
            // 탐지 범위
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // 공격 범위
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // 따라가기 범위
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, followDistance);
        }
    }
}
