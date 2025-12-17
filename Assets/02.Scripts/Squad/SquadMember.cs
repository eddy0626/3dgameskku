using UnityEngine;
using UnityEngine.AI;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// AI-controlled squad member that follows the player and engages enemies automatically
/// Based on EnemyAI state machine pattern with enhanced combat and formation support
/// Features:
/// - NavMeshAgent-based movement with formation positioning
/// - OverlapSphere enemy detection with line-of-sight checks
/// - Automatic targeting and engagement system
/// - State machine: Idle, Follow, Combat, Regroup
/// - Integration with SquadManager commands
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(SquadMemberHealth))]
public class SquadMember : MonoBehaviour, IUpgradeable
{
    #region States
    public enum SquadState
    {
        Idle,       // Standing by, scanning for enemies
        Follow,     // Following player in formation
        Combat,     // Engaging enemies
        Regroup,    // Rushing back to player when too far
        Dead        // Deceased
    }

    /// <summary>
    /// Commands received from SquadManager
    /// </summary>
    public enum MemberCommand
    {
        Follow,     // Follow player in formation
        Attack,     // Attack specified target/position
        Hold        // Hold current position
    }
    #endregion

    #region Inspector Fields
    [Header("References")]
    [SerializeField] private SquadMemberData memberData;
    [SerializeField] private Transform firePoint;

    [Header("Formation")]
    [SerializeField] private int formationIndex;

    [Header("Detection Settings")]
    [SerializeField] private float detectionUpdateInterval = 0.3f;
    [SerializeField] private float lineOfSightHeight = 1.5f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Combat Settings")]
    [SerializeField] private float attackRangeMultiplier = 0.8f;
    [SerializeField] private float targetSwitchCooldown = 2f;
    [SerializeField] private bool prioritizeClosestTarget = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;
    #endregion

    #region Private Fields
    // Core Components
    private NavMeshAgent agent;
    private SquadMemberHealth health;
    private SquadManager squadManager;

    // State Machine
    private SquadState currentState = SquadState.Idle;
    private SquadState previousState;

    // References
    private Transform player;
    private Transform currentTarget;

    // Combat
    private float lastAttackTime;
    private float lastTargetSwitchTime;
    private bool hasTarget;
    private bool canSeeTarget;

    // Command System (from SquadManager)
    private MemberCommand currentMemberCommand = MemberCommand.Follow;
    private Vector3 commandTargetPosition;
    private Transform commandTarget;

    // Detection
    private float detectionTimer;
    private Collider[] detectionBuffer = new Collider[20];

    // Runtime Stats (modified by upgrades)
    private float currentDamage;
    private float currentAttackSpeed;
    private float currentMoveSpeed;
    private float currentDetectionRange;
    private float currentAttackRange;
    #endregion

    #region Properties
    public SquadState CurrentState => currentState;
    public SquadMemberData Data => memberData;
    public int FormationIndex { get => formationIndex; set => formationIndex = value; }
    public bool IsAlive => health != null && health.IsAlive;
    public Transform CurrentTarget => currentTarget;
    public bool HasTarget => hasTarget;
    public bool CanSeeTarget => canSeeTarget;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<SquadMemberHealth>();
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        squadManager = SquadManager.Instance;

        if (player == null)
        {
            Debug.LogError($"[SquadMember] {gameObject.name}: Player not found!");
        }

        if (memberData != null)
        {
            InitializeFromData(memberData);
        }

        // Subscribe to health events
        if (health != null)
        {
            health.OnDeath += OnDeath;
        }

        // Start in Follow state
        if (player != null)
        {
            ChangeState(SquadState.Follow);
        }

        LogDebug("Initialized and ready");
    }

    private void Update()
    {
        // Don't update if game is not playing
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
        {
            return;
        }

        if (currentState == SquadState.Dead) return;
        if (player == null) return;

        // Periodic enemy detection (optimized)
        detectionTimer -= Time.deltaTime;
        if (detectionTimer <= 0f)
        {
            detectionTimer = detectionUpdateInterval;
            DetectEnemies();
        }

        // State machine update
        UpdateCurrentState();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= OnDeath;
        }
    }
    #endregion

    #region Initialization
    public void InitializeFromData(SquadMemberData data)
    {
        memberData = data;

        // Initialize runtime stats from data
        currentDamage = data.damage;
        currentAttackSpeed = 1f / data.attackCooldown;
        currentMoveSpeed = data.moveSpeed;
        currentDetectionRange = data.detectionRange;
        currentAttackRange = data.attackRange;

        // Configure NavMeshAgent
        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = currentMoveSpeed;
            agent.stoppingDistance = data.followDistance;
            agent.angularSpeed = data.rotationSpeed * 100f;
            agent.acceleration = 8f;
        }

        // Initialize health
        if (health != null)
        {
            health.Initialize(data.maxHealth);
        }

        // Spawn effect
        if (data.spawnEffect != null)
        {
            GameObject effect = Instantiate(data.spawnEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        LogDebug($"Initialized with stats - DMG:{currentDamage} SPD:{currentMoveSpeed} RNG:{currentDetectionRange}");
    }
    #endregion

    #region State Machine
    /// <summary>
    /// Change to a new state with proper enter/exit handling
    /// </summary>
    private void ChangeState(SquadState newState)
    {
        if (currentState == newState) return;

        // Exit current state
        ExitState(currentState);

        // Save previous state
        previousState = currentState;

        // Enter new state
        currentState = newState;
        EnterState(newState);

        LogDebug($"State: {previousState} -> {currentState}");
    }

    /// <summary>
    /// Initialize state-specific settings on entry
    /// </summary>
    private void EnterState(SquadState state)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        switch (state)
        {
            case SquadState.Idle:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                break;

            case SquadState.Follow:
                agent.isStopped = false;
                agent.stoppingDistance = memberData.followDistance;
                agent.speed = currentMoveSpeed;
                break;

            case SquadState.Combat:
                agent.isStopped = false;
                agent.stoppingDistance = GetEffectiveAttackRange() * attackRangeMultiplier;
                agent.speed = currentMoveSpeed * 0.9f; // Slightly slower in combat for accuracy
                break;

            case SquadState.Regroup:
                agent.isStopped = false;
                agent.stoppingDistance = memberData.followDistance;
                agent.speed = currentMoveSpeed * 1.3f; // Faster regrouping
                break;

            case SquadState.Dead:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.enabled = false;
                break;
        }
    }

    /// <summary>
    /// Cleanup when exiting a state
    /// </summary>
    private void ExitState(SquadState state)
    {
        switch (state)
        {
            case SquadState.Combat:
                // Clear target when leaving combat
                currentTarget = null;
                hasTarget = false;
                canSeeTarget = false;
                break;

            case SquadState.Regroup:
                // Restore normal speed
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.speed = currentMoveSpeed;
                }
                break;
        }
    }

    /// <summary>
    /// Main state machine update dispatcher
    /// </summary>
    private void UpdateCurrentState()
    {
        switch (currentState)
        {
            case SquadState.Idle:
                UpdateIdle();
                break;
            case SquadState.Follow:
                UpdateFollow();
                break;
            case SquadState.Combat:
                UpdateCombat();
                break;
            case SquadState.Regroup:
                UpdateRegroup();
                break;
        }
    }
    #endregion

    #region State Updates - Idle
    /// <summary>
    /// Idle state: Standing by, scanning for enemies
    /// </summary>
    private void UpdateIdle()
    {
        // Enemy detected - engage
        if (hasTarget && canSeeTarget)
        {
            ChangeState(SquadState.Combat);
            return;
        }

        // Player moved too far - start following
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > memberData.followDistance * 1.5f)
        {
            ChangeState(SquadState.Follow);
        }
    }
    #endregion

    #region State Updates - Follow
    /// <summary>
    /// Follow state: Moving to formation position behind player
    /// </summary>
    private void UpdateFollow()
    {
        // Enemy detected - switch to combat
        if (hasTarget && canSeeTarget)
        {
            ChangeState(SquadState.Combat);
            return;
        }

        // Calculate formation position
        Vector3 targetPos = GetFormationPosition();
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Too far from player - regroup urgently
        if (distanceToPlayer > memberData.maxFollowDistance)
        {
            ChangeState(SquadState.Regroup);
            return;
        }

        // Move to formation position
        if (agent != null && agent.isOnNavMesh)
        {
            float distanceToFormation = Vector3.Distance(transform.position, targetPos);

            if (distanceToFormation > agent.stoppingDistance)
            {
                agent.SetDestination(targetPos);
            }
            else
            {
                // At formation position - transition to idle
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    ChangeState(SquadState.Idle);
                }
            }
        }
    }

    /// <summary>
    /// Get current formation position from SquadManager or fallback
    /// </summary>
    private Vector3 GetFormationPosition()
    {
        if (squadManager != null)
        {
            return squadManager.GetFormationPosition(formationIndex);
        }
        else
        {
            // Fallback: simple position behind player
            return player.position - player.forward * memberData.followDistance;
        }
    }
    #endregion

    #region State Updates - Combat
    /// <summary>
    /// Combat state: Engaging current target with attacks
    /// </summary>
    private void UpdateCombat()
    {
        // Validate current target
        if (currentTarget == null || !IsTargetValid(currentTarget))
        {
            currentTarget = null;
            hasTarget = false;
            canSeeTarget = false;
            ChangeState(SquadState.Follow);
            return;
        }

        // Too far from player - prioritize regrouping
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > memberData.maxFollowDistance * 0.9f)
        {
            ChangeState(SquadState.Regroup);
            return;
        }

        // Lost line of sight to target
        if (!canSeeTarget)
        {
            // Redetect or return to follow
            ChangeState(SquadState.Follow);
            return;
        }

        // Rotate towards target
        LookAtTarget(currentTarget.position);

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        float effectiveRange = GetEffectiveAttackRange();

        // Within attack range - stop and fire
        if (distanceToTarget <= effectiveRange)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(transform.position); // Stop moving
            }
            PerformAutoAttack();
        }
        else
        {
            // Move closer to target
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(currentTarget.position);
            }
        }
    }

    /// <summary>
    /// Smooth rotation towards target position
    /// </summary>
    private void LookAtTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f; // Keep horizontal

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                memberData.rotationSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// Automatic attack with cooldown management
    /// </summary>
    private void PerformAutoAttack()
    {
        float attackCooldown = memberData.attackCooldown / currentAttackSpeed;
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;

        // Determine attack type based on range and member data
        bool useMelee = memberData.attackType == AttackType.Melee ||
                       (memberData.attackType == AttackType.Both &&
                        Vector3.Distance(transform.position, currentTarget.position) <= memberData.meleeRange);

        if (useMelee)
        {
            ExecuteMeleeAttack();
        }
        else
        {
            ExecuteRangedAttack();
        }
    }

    /// <summary>
    /// Execute melee attack on current target
    /// </summary>
    private void ExecuteMeleeAttack()
    {
        if (currentTarget == null) return;

        // Apply damage
        if (currentTarget.TryGetComponent<IDamageable>(out var damageable))
        {
            Vector3 hitPoint = currentTarget.position;
            Vector3 hitNormal = (transform.position - currentTarget.position).normalized;
            damageable.TakeDamage(currentDamage, hitPoint, hitNormal);
        }

        // Visual effects
        if (memberData.attackEffect != null)
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            GameObject effect = Instantiate(memberData.attackEffect, spawnPos, transform.rotation);
            Destroy(effect, 2f);
        }

        // Audio
        PlaySound(memberData.attackSound);

        LogDebug($"Melee attack on {currentTarget.name} for {currentDamage} damage");
    }

    /// <summary>
    /// Execute ranged attack (projectile or hitscan)
    /// </summary>
    private void ExecuteRangedAttack()
    {
        if (currentTarget == null) return;

        Vector3 targetPosition = currentTarget.position + Vector3.up * 1f; // Aim at center mass

        // Projectile-based attack
        if (memberData.projectilePrefab != null && firePoint != null)
        {
            Vector3 direction = (targetPosition - firePoint.position).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);

            GameObject projectile = Instantiate(memberData.projectilePrefab, firePoint.position, rotation);

            // Initialize projectile with damage and target
            if (projectile.TryGetComponent<SquadProjectile>(out var proj))
            {
                proj.Initialize(currentDamage, memberData.projectileSpeed, currentTarget);
            }
            else if (projectile.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = direction * memberData.projectileSpeed;
            }
        }
        else
        {
            // Hitscan attack (instant hit)
            if (currentTarget.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector3 hitPoint = targetPosition;
                Vector3 hitNormal = (transform.position - targetPosition).normalized;
                damageable.TakeDamage(currentDamage, hitPoint, hitNormal);
            }
        }

        // Visual effects
        if (memberData.attackEffect != null && firePoint != null)
        {
            GameObject effect = Instantiate(memberData.attackEffect, firePoint.position, firePoint.rotation);
            Destroy(effect, 2f);
        }

        // Audio
        PlaySound(memberData.attackSound);

        LogDebug($"Ranged attack on {currentTarget.name} for {currentDamage} damage");
    }

    /// <summary>
    /// Get effective attack range based on attack type
    /// </summary>
    private float GetEffectiveAttackRange()
    {
        if (memberData.attackType == AttackType.Melee)
            return memberData.meleeRange;

        return currentAttackRange;
    }
    #endregion

    #region State Updates - Regroup
    /// <summary>
    /// Regroup state: Rushing back to player, ignoring enemies
    /// </summary>
    private void UpdateRegroup()
    {
        // Move to formation position urgently
        Vector3 targetPos = GetFormationPosition();

        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(targetPos);
        }

        // Check if regrouped successfully
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= memberData.followDistance * 1.2f)
        {
            ChangeState(SquadState.Follow);
        }
    }
    #endregion

    #region Enemy Detection
    /// <summary>
    /// Detect enemies using OverlapSphere with line-of-sight validation
    /// Based on EnemyAI detection pattern
    /// </summary>
    private void DetectEnemies()
    {
        // Use OverlapSphereNonAlloc for better performance
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            currentDetectionRange,
            detectionBuffer,
            memberData.enemyLayer
        );

        if (count == 0)
        {
            hasTarget = false;
            canSeeTarget = false;
            return;
        }

        // Find best target
        Transform bestTarget = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Transform potentialTarget = detectionBuffer[i].transform;

            // Validate target
            if (!IsTargetValid(potentialTarget)) continue;

            // Check line of sight
            if (!HasLineOfSight(potentialTarget)) continue;

            // Score target (prioritize closest or current target)
            float score = CalculateTargetScore(potentialTarget);

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = potentialTarget;
            }
        }

        // Update target status
        if (bestTarget != null)
        {
            // Prevent rapid target switching
            if (currentTarget != bestTarget && Time.time - lastTargetSwitchTime < targetSwitchCooldown)
            {
                // Keep current target if still valid
                if (IsTargetValid(currentTarget) && HasLineOfSight(currentTarget))
                {
                    bestTarget = currentTarget;
                }
                else
                {
                    lastTargetSwitchTime = Time.time;
                }
            }
            else if (currentTarget != bestTarget)
            {
                lastTargetSwitchTime = Time.time;
                LogDebug($"Target switched: {bestTarget.name}");
            }

            currentTarget = bestTarget;
            hasTarget = true;
            canSeeTarget = true;
        }
        else
        {
            // No valid targets
            if (hasTarget)
            {
                LogDebug("Lost all targets");
            }
            hasTarget = false;
            canSeeTarget = false;
        }
    }

    /// <summary>
    /// Check if we have clear line of sight to target
    /// </summary>
    private bool HasLineOfSight(Transform target)
    {
        if (target == null) return false;

        Vector3 eyePosition = transform.position + Vector3.up * lineOfSightHeight;
        Vector3 targetPosition = target.position + Vector3.up * 1f;
        Vector3 direction = targetPosition - eyePosition;
        float distance = direction.magnitude;

        // Raycast for obstacles
        if (Physics.Raycast(eyePosition, direction.normalized, out RaycastHit hit, distance, obstacleMask))
        {
            // Hit something - check if it's the target
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        return true; // No obstacles
    }

    /// <summary>
    /// Calculate target priority score (lower is better)
    /// </summary>
    private float CalculateTargetScore(Transform target)
    {
        float distance = Vector3.Distance(transform.position, target.position);

        // Prioritize current target to prevent flickering
        if (currentTarget == target)
        {
            distance *= 0.7f; // 30% preference
        }

        if (prioritizeClosestTarget)
        {
            return distance;
        }
        else
        {
            // Could add health-based or threat-based scoring here
            return distance;
        }
    }

    /// <summary>
    /// Check if target is valid and alive
    /// </summary>
    private bool IsTargetValid(Transform target)
    {
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;

        // Check if damageable and alive
        if (target.TryGetComponent<IDamageable>(out var damageable))
        {
            return damageable.IsAlive;
        }

        return true;
    }
    #endregion

    #region IUpgradeable Implementation
    public void ApplyUpgrade(UpgradeType type, float value, bool isMultiplicative = false)
    {
        // TODO: isMultiplicative 지원 추가 필요 시 구현
        switch (type)
        {
            case UpgradeType.Damage:
                currentDamage += value;
                LogDebug($"Damage upgraded: +{value} (Total: {currentDamage})");
                break;

            case UpgradeType.Health:
                if (health != null)
                {
                    health.SetMaxHealth(health.MaxHealth + value, false);
                    LogDebug($"Health upgraded: +{value} (Total: {health.MaxHealth})");
                }
                break;

            case UpgradeType.Speed:
                currentMoveSpeed += value;
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.speed = currentMoveSpeed;
                }
                LogDebug($"Speed upgraded: +{value} (Total: {currentMoveSpeed})");
                break;

            case UpgradeType.AttackSpeed:
                currentAttackSpeed += value;
                LogDebug($"Attack speed upgraded: +{value} (Total: {currentAttackSpeed})");
                break;

            case UpgradeType.AttackRange:
                currentDetectionRange += value;
                currentAttackRange += value;
                LogDebug($"Range upgraded: +{value} (Detection: {currentDetectionRange}, Attack: {currentAttackRange})");
                break;
        }
    }
    #endregion

    #region Command System
    /// <summary>
    /// Set formation index (called by SquadManager)
    /// </summary>
    public void SetFormationIndex(int index)
    {
        formationIndex = index;
        LogDebug($"Formation index set to {index}");
    }

    /// <summary>
    /// Receive command from SquadManager
    /// Commands override current behavior
    /// </summary>
    public void SetCommand(MemberCommand command, Vector3 position = default, Transform target = null)
    {
        currentMemberCommand = command;
        commandTargetPosition = position;
        commandTarget = target;

        LogDebug($"Command received: {command}");

        // Execute command based on type
        switch (command)
        {
            case MemberCommand.Follow:
                // Return to formation following
                if (currentState != SquadState.Combat || !hasTarget)
                {
                    ChangeState(SquadState.Follow);
                }
                break;

            case MemberCommand.Attack:
                // Force attack on specified target or position
                if (target != null)
                {
                    currentTarget = target;
                    hasTarget = true;
                    canSeeTarget = true;
                    ChangeState(SquadState.Combat);
                }
                else if (position != default)
                {
                    // Move to position and engage enemies there
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.SetDestination(position);
                    }
                    ChangeState(SquadState.Follow);
                }
                break;

            case MemberCommand.Hold:
                // Hold position and defend
                ChangeState(SquadState.Idle);
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.SetDestination(transform.position);
                }
                break;
        }
    }
    #endregion

    #region Health & Death
    /// <summary>
    /// Called when health reaches zero
    /// </summary>
    public void OnDeath()
    {
        ChangeState(SquadState.Dead);

        // Death effects
        if (memberData.deathEffect != null)
        {
            GameObject effect = Instantiate(memberData.deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, 5f);
        }

        // Death sound
        PlaySound(memberData.deathSound);

        // Notify SquadManager
        if (squadManager != null)
        {
            squadManager.OnMemberDeath(this);
        }

        LogDebug("Death");

        // Start death sequence
        StartCoroutine(DeathSequence());
    }

    /// <summary>
    /// Cleanup after death
    /// </summary>
    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(3f);

        // Deactivate or return to pool
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Revive member (e.g., after respawn)
    /// </summary>
    public void Revive(float healthPercentage = 0.5f)
    {
        if (health != null)
        {
            health.Revive(healthPercentage);
        }

        if (agent != null)
        {
            agent.enabled = true;
        }

        ChangeState(SquadState.Follow);
        LogDebug($"Revived with {healthPercentage * 100}% health");
    }
    #endregion

    #region Audio
    /// <summary>
    /// Play audio clip at member's position
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, 0.7f);
        }
    }
    #endregion

    #region Utility
    /// <summary>
    /// Get current stats for UI/debugging
    /// </summary>
    public string GetStatsString()
    {
        return $"DMG:{currentDamage:F1} HP:{health?.CurrentHealth:F0}/{health?.MaxHealth:F0} " +
               $"SPD:{currentMoveSpeed:F1} RNG:{currentDetectionRange:F1}";
    }

    /// <summary>
    /// Debug logging with conditional compilation
    /// </summary>
    private void LogDebug(string message)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[SquadMember] {gameObject.name}: {message}");
        }
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        if (memberData == null) return;

        Vector3 position = transform.position;

        // Detection Range (Yellow)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(position, currentDetectionRange > 0 ? currentDetectionRange : memberData.detectionRange);

        // Attack Range (Red)
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        float range = currentAttackRange > 0 ? currentAttackRange : memberData.attackRange;
        Gizmos.DrawWireSphere(position, range);

        // Follow Distance (Green)
        if (player != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(position, memberData.followDistance);

            // Max Follow Distance (Blue)
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.15f);
            Gizmos.DrawWireSphere(position, memberData.maxFollowDistance);

            // Line to player
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(position, player.position);
        }

        // Current Target
        if (currentTarget != null)
        {
            Gizmos.color = canSeeTarget ? Color.red : Color.yellow;
            Gizmos.DrawLine(position + Vector3.up, currentTarget.position + Vector3.up);

            // Target indicator
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }

        // Line of sight ray
        if (currentTarget != null)
        {
            Vector3 eyePos = position + Vector3.up * lineOfSightHeight;
            Vector3 targetPos = currentTarget.position + Vector3.up;
            Gizmos.color = canSeeTarget ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawLine(eyePos, targetPos);
        }

        // Formation position
        if (squadManager != null && Application.isPlaying)
        {
            Vector3 formationPos = GetFormationPosition();
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(formationPos, 0.3f);
            Gizmos.DrawLine(position, formationPos);
        }

        // State indicator (Editor only)
        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            Handles.Label(position + Vector3.up * 2f,
                $"{currentState}\n{GetStatsString()}");
        }
        #endif
    }
    #endregion
}