using UnityEngine;

/// <summary>
/// 적 공격 컴포넌트
/// 근접/원거리 공격 지원
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    // AttackType enum은 EnemyData.cs에서 전역으로 정의됨
    
    #region Inspector Fields
    
    [Header("공격 타입")]
    [SerializeField] private AttackType _attackType = AttackType.Melee;
    [SerializeField] private float _meleeRangeThreshold = 3f;
    
    [Header("근접 공격 설정")]
    [SerializeField] private float _meleeDamage = 20f;
    [SerializeField] private float _meleeRange = 2f;
    [SerializeField] private float _meleeAngle = 90f;
    [SerializeField] private LayerMask _meleeTargetLayer;
    
    [Header("원거리 공격 설정")]
    [SerializeField] private float _rangedDamage = 15f;
    [SerializeField] private float _rangedRange = 20f;
    [SerializeField] private float _projectileSpeed = 30f;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _aimAccuracy = 0.9f;
    
    [Header("공격 이펙트")]
    [SerializeField] private GameObject _meleeEffectPrefab;
    [SerializeField] private GameObject _muzzleFlashPrefab;
    [SerializeField] private AudioClip _meleeSound;
    [SerializeField] private AudioClip _rangedSound;
    
    [Header("애니메이션")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _meleeAttackTrigger = "MeleeAttack";
    [SerializeField] private string _rangedAttackTrigger = "RangedAttack";
    
    #endregion
    
    #region Private Fields
    
    private AudioSource _audioSource;
    private Transform _currentTarget;
    
    #endregion
    
    #region Properties
    
    public float MeleeDamage => _meleeDamage;
    public float RangedDamage => _rangedDamage;
    public AttackType CurrentAttackType => _attackType;
    
    #endregion
    
    #region Unity Callbacks
    
private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
        
        // FirePoint가 없으면 MuzzlePoint 자식 오브젝트 검색
        if (_firePoint == null)
        {
            Transform muzzle = transform.Find("GunHolder/EnemyGun/MuzzlePoint");
            if (muzzle == null)
            {
                muzzle = GetComponentInChildren<Transform>().Find("MuzzlePoint");
            }
            _firePoint = muzzle ?? transform;
            
            if (muzzle != null)
            {
                Debug.Log($"[EnemyAttack] Auto-found MuzzlePoint at {muzzle.name}");
            }
        }
        
        // 발사체 프리팹이 없으면 Resources에서 자동 로드
        if (_projectilePrefab == null && (_attackType == AttackType.Ranged || _attackType == AttackType.Both))
        {
            _projectilePrefab = Resources.Load<GameObject>("Prefabs/EnemyBullet");
            
            if (_projectilePrefab != null)
            {
                Debug.Log($"[EnemyAttack] Auto-loaded projectile prefab: EnemyBullet");
            }
            else
            {
                Debug.LogWarning($"[EnemyAttack] Could not find 'Prefabs/EnemyBullet' in Resources folder!");
            }
        }
    }


    /// <summary>
    /// EnemyData 기반 초기화 (EnemyBase에서 호출)
    /// </summary>
    public void Initialize(
        AttackType attackType,
        float meleeDamage,
        float meleeRange,
        float meleeAngle,
        float meleeRangeThreshold,
        float rangedDamage,
        float rangedRange,
        float projectileSpeed,
        float aimAccuracy,
        GameObject projectilePrefab,
        GameObject meleeEffectPrefab,
        GameObject muzzleFlashPrefab,
        AudioClip meleeAttackSound,
        AudioClip rangedAttackSound)
    {
        _attackType = attackType;
        _meleeDamage = meleeDamage;
        _meleeRange = meleeRange;
        _meleeAngle = meleeAngle;
        _meleeRangeThreshold = meleeRangeThreshold;
        _rangedDamage = rangedDamage;
        _rangedRange = rangedRange;
        _projectileSpeed = projectileSpeed;
        _aimAccuracy = aimAccuracy;
        
        if (projectilePrefab != null)
            _projectilePrefab = projectilePrefab;
        if (meleeEffectPrefab != null)
            _meleeEffectPrefab = meleeEffectPrefab;
        if (muzzleFlashPrefab != null)
            _muzzleFlashPrefab = muzzleFlashPrefab;
        if (meleeAttackSound != null)
            _meleeSound = meleeAttackSound;
        if (rangedAttackSound != null)
            _rangedSound = rangedAttackSound;
        
        Debug.Log($"[EnemyAttack] {gameObject.name} initialized - Type: {_attackType}, Melee: {_meleeDamage}, Ranged: {_rangedDamage}");
    }

    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 공격 실행 (EnemyAI에서 호출)
    /// </summary>
    public void Attack(Transform target)
    {
        if (target == null) return;
        
        _currentTarget = target;
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        switch (_attackType)
        {
            case AttackType.Melee:
                PerformMeleeAttack();
                break;
                
            case AttackType.Ranged:
                PerformRangedAttack();
                break;
                
            case AttackType.Both:
                if (distanceToTarget <= _meleeRangeThreshold)
                {
                    PerformMeleeAttack();
                }
                else
                {
                    PerformRangedAttack();
                }
                break;
        }
    }
    
    /// <summary>
    /// 데미지 설정
    /// </summary>
    public void SetDamage(float meleeDamage, float rangedDamage)
    {
        _meleeDamage = meleeDamage;
        _rangedDamage = rangedDamage;
    }
    
    #endregion
    
    #region Melee Attack
    
    /// <summary>
    /// 근접 공격 실행
    /// </summary>
    private void PerformMeleeAttack()
    {
        Debug.Log($"[EnemyAttack] {gameObject.name} performs melee attack!");
        
        // 애니메이션 트리거
        if (_animator != null)
        {
            _animator.SetTrigger(_meleeAttackTrigger);
        }
        
        // 사운드 재생
        PlaySound(_meleeSound);
        
        // 이펙트
        if (_meleeEffectPrefab != null)
        {
            Vector3 effectPos = transform.position + transform.forward * 1f + Vector3.up;
            GameObject effect = Instantiate(_meleeEffectPrefab, effectPos, transform.rotation);
            Destroy(effect, 1f);
        }
        
        // 데미지 처리 (부채꼴 범위 내 타겟)
        DealMeleeDamage();
    }
    
    /// <summary>
    /// 근접 데미지 적용
    /// </summary>
    private void DealMeleeDamage()
    {
        // 범위 내 콜라이더 검색
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _meleeRange, _meleeTargetLayer);
        
        foreach (Collider col in hitColliders)
        {
            // 자기 자신 제외
            if (col.transform == transform) continue;
            
            // 각도 체크
            Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            
            if (angle > _meleeAngle * 0.5f) continue;
            
            // 데미지 적용
            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector3 hitPoint = col.ClosestPoint(transform.position);
                Vector3 hitNormal = (hitPoint - transform.position).normalized;
                
                damageable.TakeDamage(_meleeDamage, hitPoint, hitNormal);
                
                Debug.Log($"[EnemyAttack] Melee hit: {col.name}, Damage: {_meleeDamage}");
            }
        }
    }
    
    #endregion
    
    #region Ranged Attack
    
    /// <summary>
    /// 원거리 공격 실행
    /// </summary>
    private void PerformRangedAttack()
    {
        Debug.Log($"[EnemyAttack] {gameObject.name} performs ranged attack!");
        
        // 애니메이션 트리거
        if (_animator != null)
        {
            _animator.SetTrigger(_rangedAttackTrigger);
        }
        
        // 사운드 재생
        PlaySound(_rangedSound);
        
        // 머즐 플래시
        if (_muzzleFlashPrefab != null && _firePoint != null)
        {
            GameObject flash = Instantiate(_muzzleFlashPrefab, _firePoint.position, _firePoint.rotation);
            Destroy(flash, 0.5f);
        }
        
        // 발사체 생성 또는 히트스캔
        if (_projectilePrefab != null)
        {
            FireProjectile();
        }
        else
        {
            PerformHitscan();
        }
    }
    
    /// <summary>
    /// 발사체 발사
    /// </summary>
    private void FireProjectile()
    {
        if (_currentTarget == null || _projectilePrefab == null) return;
        
        // 조준 방향 계산 (정확도 적용)
        Vector3 targetPosition = _currentTarget.position + Vector3.up * 1f;
        Vector3 aimDirection = (targetPosition - _firePoint.position).normalized;
        
        // 정확도에 따른 랜덤 오프셋
        float inaccuracy = (1f - _aimAccuracy) * 10f;
        aimDirection += new Vector3(
            Random.Range(-inaccuracy, inaccuracy) * 0.1f,
            Random.Range(-inaccuracy, inaccuracy) * 0.1f,
            Random.Range(-inaccuracy, inaccuracy) * 0.1f
        );
        aimDirection.Normalize();
        
        // 발사체 생성
        GameObject projectileObj = Instantiate(
            _projectilePrefab,
            _firePoint.position,
            Quaternion.LookRotation(aimDirection)
        );
        
        // Projectile 컴포넌트가 있으면 초기화
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(_projectileSpeed, _rangedDamage, aimDirection, gameObject);
        }
        else
        {
            // Rigidbody만 있는 경우
            Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = aimDirection * _projectileSpeed;
            }
        }
    }
    
    /// <summary>
    /// 히트스캔 공격 (레이캐스트)
    /// </summary>
    private void PerformHitscan()
    {
        if (_currentTarget == null) return;
        
        Vector3 targetPosition = _currentTarget.position + Vector3.up * 1f;
        Vector3 direction = (targetPosition - _firePoint.position).normalized;
        
        // 정확도 적용
        float inaccuracy = (1f - _aimAccuracy) * 5f;
        direction += new Vector3(
            Random.Range(-inaccuracy, inaccuracy) * 0.1f,
            Random.Range(-inaccuracy, inaccuracy) * 0.1f,
            Random.Range(-inaccuracy, inaccuracy) * 0.1f
        );
        direction.Normalize();
        
        RaycastHit hit;
        if (Physics.Raycast(_firePoint.position, direction, out hit, _rangedRange))
        {
            // 데미지 적용
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(_rangedDamage, hit.point, hit.normal);
                Debug.Log($"[EnemyAttack] Hitscan hit: {hit.collider.name}, Damage: {_rangedDamage}");
            }
            
            // 디버그 라인
            Debug.DrawLine(_firePoint.position, hit.point, Color.red, 0.5f);
        }
        else
        {
            Debug.DrawRay(_firePoint.position, direction * _rangedRange, Color.yellow, 0.5f);
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// 사운드 재생
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        
        if (_audioSource != null)
        {
            _audioSource.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        // 근접 공격 범위
        if (_attackType == AttackType.Melee || _attackType == AttackType.Both)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _meleeRange);
            
            // 공격 각도
            Vector3 leftBoundary = Quaternion.Euler(0, -_meleeAngle * 0.5f, 0) * transform.forward;
            Vector3 rightBoundary = Quaternion.Euler(0, _meleeAngle * 0.5f, 0) * transform.forward;
            
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, leftBoundary * _meleeRange);
            Gizmos.DrawRay(transform.position, rightBoundary * _meleeRange);
        }
        
        // 원거리 공격 범위
        if (_attackType == AttackType.Ranged || _attackType == AttackType.Both)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _rangedRange);
            
            // 발사 지점
            if (_firePoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(_firePoint.position, 0.1f);
                Gizmos.DrawRay(_firePoint.position, _firePoint.forward * 2f);
            }
        }
        
        // 근접/원거리 전환 거리 (Both 타입)
        if (_attackType == AttackType.Both)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, _meleeRangeThreshold);
        }
    }
    
    #endregion
}
