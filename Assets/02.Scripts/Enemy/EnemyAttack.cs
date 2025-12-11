using UnityEngine;
using System.Collections;

/// <summary>
/// 적 공격 시스템 - 라이플화
/// Single/Burst/FullAuto 연사 모드 지원
/// 탄퍼짐(Spread) 시스템, 이펙트 연동
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    #region Enums
    
    /// <summary>
    /// 연사 모드
    /// </summary>
    public enum FireMode
    {
        Single,     // 단발
        Burst,      // 점사
        FullAuto    // 완전 자동
    }
    
    #endregion
    
    #region Inspector Fields
    
    [Header("공격 타입")]
    [SerializeField] private AttackType _attackType = AttackType.Ranged;
    [SerializeField] private float _meleeRangeThreshold = 3f;
    
    [Header("연사 모드 설정")]
    [SerializeField] private FireMode _fireMode = FireMode.FullAuto;
    [Tooltip("초당 발사 수 (RPM / 60)")]
    [SerializeField] private float _fireRate = 10f;
    [Tooltip("버스트 모드 시 연속 발사 수")]
    [SerializeField] private int _burstCount = 3;
    [Tooltip("버스트 간 딜레이 (초)")]
    [SerializeField] private float _burstDelay = 0.3f;
    
    [Header("탄퍼짐(Spread) 설정")]
    [Tooltip("기본 탄퍼짐 (도)")]
    [SerializeField] private float _baseSpread = 2f;
    [Tooltip("연사 시 탄퍼짐 증가량 (도)")]
    [SerializeField] private float _spreadIncreasePerShot = 0.5f;
    [Tooltip("탄퍼짐 회복 속도 (도/초)")]
    [SerializeField] private float _spreadRecoveryRate = 5f;
    [Tooltip("최대 탄퍼짐 (도)")]
    [SerializeField] private float _maxSpread = 10f;
    
    [Header("근접 공격 설정")]
    [SerializeField] private float _meleeDamage = 20f;
    [SerializeField] private float _meleeRange = 2f;
    [SerializeField] private float _meleeAngle = 90f;
    [SerializeField] private LayerMask _meleeTargetLayer;
    
    [Header("원거리 공격 설정")]
    [SerializeField] private float _rangedDamage = 10f;
    [SerializeField] private float _rangedRange = 30f;
    [SerializeField] private float _projectileSpeed = 50f;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private LayerMask _hitLayer;
    
    [Header("명중률")]
    [Tooltip("기본 명중률 (0~1)")]
    [SerializeField] private float _aimAccuracy = 0.85f;
    
    [Header("이펙트 설정")]
    [SerializeField] private WeaponType _weaponType = WeaponType.Rifle;
    [SerializeField] private GameObject _meleeEffectPrefab;
    [SerializeField] private GameObject _customMuzzleFlashPrefab;
    
    [Header("사운드")]
    [SerializeField] private AudioClip _meleeSound;
    [SerializeField] private AudioClip _fireSound;
    
    [Header("애니메이션")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _meleeAttackTrigger = "MeleeAttack";
    [SerializeField] private string _fireAttackTrigger = "Fire";
    [SerializeField] private string _isFiringBool = "IsFiring";
    
    #endregion
    
    #region Private Fields
    
    private AudioSource _audioSource;
    private Transform _currentTarget;
    
    // 연사 관련
    private float _lastFireTime;
    private float _fireInterval;
    private bool _isFiring;
    private Coroutine _firingCoroutine;
    
    // 탄퍼짐
    private float _currentSpread;
    
    // 버스트
    private int _currentBurstCount;
    
    #endregion
    
    #region Properties
    
    public float MeleeDamage => _meleeDamage;
    public float RangedDamage => _rangedDamage;
    public AttackType CurrentAttackType => _attackType;
    public FireMode CurrentFireMode => _fireMode;
    public bool IsFiring => _isFiring;
    public float CurrentSpread => _currentSpread;
    
    #endregion
    
    #region Unity Callbacks
    
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
        
        // FirePoint 자동 검색
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
                Debug.Log($"[EnemyAttack] Auto-found MuzzlePoint: {muzzle.name}");
            }
        }
        
        // 발사체 프리팹 자동 로드
        if (_projectilePrefab == null && (_attackType == AttackType.Ranged || _attackType == AttackType.Both))
        {
            _projectilePrefab = Resources.Load<GameObject>("Prefabs/EnemyBullet");
            
            if (_projectilePrefab != null)
            {
                Debug.Log("[EnemyAttack] Auto-loaded projectile prefab: EnemyBullet");
            }
        }
        
        // 발사 간격 계산
        _fireInterval = 1f / _fireRate;
        _currentSpread = _baseSpread;
        
        // 레이어 마스크 기본값
        if (_hitLayer == 0)
        {
            _hitLayer = ~LayerMask.GetMask("Enemy", "Ignore Raycast");
        }
    }
    
    private void Update()
    {
        // 탄퍼짐 회복
        RecoverSpread();
    }
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// EnemyData 기반 초기화
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
        
        if (projectilePrefab != null) _projectilePrefab = projectilePrefab;
        if (meleeEffectPrefab != null) _meleeEffectPrefab = meleeEffectPrefab;
        if (muzzleFlashPrefab != null) _customMuzzleFlashPrefab = muzzleFlashPrefab;
        if (meleeAttackSound != null) _meleeSound = meleeAttackSound;
        if (rangedAttackSound != null) _fireSound = rangedAttackSound;
        
        _fireInterval = 1f / _fireRate;
        
        Debug.Log($"[EnemyAttack] {gameObject.name} initialized - Type: {_attackType}, FireMode: {_fireMode}, FireRate: {_fireRate}");
    }
    
    /// <summary>
    /// 연사 모드 설정
    /// </summary>
    public void SetFireMode(FireMode mode, float fireRate, int burstCount = 3)
    {
        _fireMode = mode;
        _fireRate = fireRate;
        _burstCount = burstCount;
        _fireInterval = 1f / _fireRate;
    }
    
    /// <summary>
    /// 탄퍼짐 설정
    /// </summary>
    public void SetSpreadSettings(float baseSpread, float increasePerShot, float recoveryRate, float maxSpread)
    {
        _baseSpread = baseSpread;
        _spreadIncreasePerShot = increasePerShot;
        _spreadRecoveryRate = recoveryRate;
        _maxSpread = maxSpread;
        _currentSpread = _baseSpread;
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 공격 시작 (EnemyAI에서 호출)
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
                StartFiring();
                break;
                
            case AttackType.Both:
                if (distanceToTarget <= _meleeRangeThreshold)
                {
                    PerformMeleeAttack();
                }
                else
                {
                    StartFiring();
                }
                break;
        }
    }
    
    /// <summary>
    /// 연사 시작
    /// </summary>
    public void StartFiring()
    {
        if (_isFiring) return;
        
        _isFiring = true;
        
        if (_animator != null)
        {
            _animator.SetBool(_isFiringBool, true);
        }
        
        switch (_fireMode)
        {
            case FireMode.Single:
                FireSingleShot();
                _isFiring = false;
                break;
                
            case FireMode.Burst:
                _firingCoroutine = StartCoroutine(FireBurstCoroutine());
                break;
                
            case FireMode.FullAuto:
                _firingCoroutine = StartCoroutine(FireFullAutoCoroutine());
                break;
        }
    }
    
    /// <summary>
    /// 연사 중지
    /// </summary>
    public void StopFiring()
    {
        if (!_isFiring) return;
        
        _isFiring = false;
        
        if (_firingCoroutine != null)
        {
            StopCoroutine(_firingCoroutine);
            _firingCoroutine = null;
        }
        
        if (_animator != null)
        {
            _animator.SetBool(_isFiringBool, false);
        }
        
        _currentBurstCount = 0;
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
    
    #region Firing Coroutines
    
    /// <summary>
    /// 버스트 연사 코루틴
    /// </summary>
    private IEnumerator FireBurstCoroutine()
    {
        while (_isFiring && _currentTarget != null)
        {
            // 버스트 발사
            for (int i = 0; i < _burstCount && _isFiring; i++)
            {
                FireSingleShot();
                _currentBurstCount++;
                
                if (i < _burstCount - 1)
                {
                    yield return new WaitForSeconds(_fireInterval);
                }
            }
            
            _currentBurstCount = 0;
            
            // 버스트 간 딜레이
            yield return new WaitForSeconds(_burstDelay);
        }
        
        StopFiring();
    }
    
    /// <summary>
    /// 풀오토 연사 코루틴
    /// </summary>
    private IEnumerator FireFullAutoCoroutine()
    {
        while (_isFiring && _currentTarget != null)
        {
            FireSingleShot();
            yield return new WaitForSeconds(_fireInterval);
        }
        
        StopFiring();
    }
    
    #endregion
    
    #region Single Shot
    
    /// <summary>
    /// 단발 발사
    /// </summary>
    private void FireSingleShot()
    {
        if (_currentTarget == null) return;
        
        // 타겟 방향 계산
        Vector3 targetPosition = _currentTarget.position + Vector3.up * 1f;
        Vector3 baseDirection = (targetPosition - _firePoint.position).normalized;
        
        // 탄퍼짐 적용
        Vector3 spreadDirection = ApplySpread(baseDirection);
        
        // 머즐 플래시
        PlayMuzzleFlash();
        
        // 사운드
        PlayFireSound();
        
        // 애니메이션
        if (_animator != null)
        {
            _animator.SetTrigger(_fireAttackTrigger);
        }
        
        // 히트스캔 또는 발사체
        if (_projectilePrefab != null)
        {
            FireProjectile(spreadDirection);
        }
        else
        {
            PerformHitscan(spreadDirection);
        }
        
        // 탄퍼짐 증가
        IncreaseSpread();
        
        _lastFireTime = Time.time;
        
        Debug.Log($"[EnemyAttack] {gameObject.name} fired! Spread: {_currentSpread:F1}°");
    }
    
    /// <summary>
    /// 탄퍼짐 적용
    /// </summary>
    private Vector3 ApplySpread(Vector3 baseDirection)
    {
        // 명중률에 따른 추가 오프셋
        float accuracyOffset = (1f - _aimAccuracy) * 5f;
        
        // 총 탄퍼짐 = 기본 탄퍼짐 + 명중률 오프셋
        float totalSpread = _currentSpread + accuracyOffset;
        
        // 랜덤 탄퍼짐 (원뿔 모양)
        float spreadX = Random.Range(-totalSpread, totalSpread);
        float spreadY = Random.Range(-totalSpread, totalSpread);
        
        // 방향 벡터에 탄퍼짐 적용
        Quaternion spreadRotation = Quaternion.Euler(spreadY, spreadX, 0f);
        Vector3 spreadDirection = spreadRotation * baseDirection;
        
        return spreadDirection.normalized;
    }
    
    /// <summary>
    /// 탄퍼짐 증가
    /// </summary>
    private void IncreaseSpread()
    {
        _currentSpread = Mathf.Min(_currentSpread + _spreadIncreasePerShot, _maxSpread);
    }
    
    /// <summary>
    /// 탄퍼짐 회복
    /// </summary>
    private void RecoverSpread()
    {
        if (_currentSpread > _baseSpread)
        {
            _currentSpread = Mathf.Max(
                _currentSpread - _spreadRecoveryRate * Time.deltaTime,
                _baseSpread
            );
        }
    }
    
    #endregion
    
    #region Projectile & Hitscan
    
    /// <summary>
    /// 발사체 발사
    /// </summary>
    private void FireProjectile(Vector3 direction)
    {
        if (_projectilePrefab == null || _firePoint == null) return;
        
        GameObject projectileObj = Instantiate(
            _projectilePrefab,
            _firePoint.position,
            Quaternion.LookRotation(direction)
        );
        
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(_projectileSpeed, _rangedDamage, direction, gameObject);
        }
        else
        {
            Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = direction * _projectileSpeed;
            }
        }
    }
    
    /// <summary>
    /// 히트스캔 발사
    /// </summary>
    private void PerformHitscan(Vector3 direction)
    {
        if (_firePoint == null) return;
        
        RaycastHit hit;
        if (Physics.Raycast(_firePoint.position, direction, out hit, _rangedRange, _hitLayer))
        {
            // 임팩트 이펙트
            PlayImpactEffect(hit);
            
            // 데미지 적용
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(_rangedDamage, hit.point, hit.normal);
                Debug.Log($"[EnemyAttack] Hit: {hit.collider.name}, Damage: {_rangedDamage}");
            }
            
            // 디버그 라인
            Debug.DrawLine(_firePoint.position, hit.point, Color.red, 0.1f);
        }
        else
        {
            Debug.DrawRay(_firePoint.position, direction * _rangedRange, Color.yellow, 0.1f);
        }
    }
    
    #endregion
    
    #region Effects
    
    /// <summary>
    /// 머즐 플래시 재생
    /// </summary>
    private void PlayMuzzleFlash()
    {
        if (_firePoint == null) return;
        
        // MuzzleFlashController 사용 (우선)
        if (MuzzleFlashController.Instance != null)
        {
            if (_customMuzzleFlashPrefab != null)
            {
                MuzzleFlashController.Instance.PlayCustomMuzzleFlash(_customMuzzleFlashPrefab, _firePoint);
            }
            else
            {
                MuzzleFlashController.Instance.PlayMuzzleFlash(_firePoint, _weaponType);
            }
        }
        // 폴백: 직접 생성
        else if (_customMuzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(_customMuzzleFlashPrefab, _firePoint.position, _firePoint.rotation);
            Destroy(flash, 0.1f);
        }
    }
    
    /// <summary>
    /// 임팩트 이펙트 재생
    /// </summary>
    private void PlayImpactEffect(RaycastHit hit)
    {
        if (ImpactEffectManager.Instance != null)
        {
            ImpactEffectManager.Instance.PlayImpact(hit);
        }
    }
    
    /// <summary>
    /// 발사 사운드 재생
    /// </summary>
    private void PlayFireSound()
    {
        if (_fireSound == null) return;
        
        if (_audioSource != null)
        {
            _audioSource.PlayOneShot(_fireSound);
        }
        else
        {
            AudioSource.PlayClipAtPoint(_fireSound, _firePoint.position);
        }
    }
    
    #endregion
    
    #region Melee Attack
    
    /// <summary>
    /// 근접 공격 실행
    /// </summary>
    private void PerformMeleeAttack()
    {
        Debug.Log($"[EnemyAttack] {gameObject.name} performs melee attack!");
        
        if (_animator != null)
        {
            _animator.SetTrigger(_meleeAttackTrigger);
        }
        
        PlaySound(_meleeSound);
        
        if (_meleeEffectPrefab != null)
        {
            Vector3 effectPos = transform.position + transform.forward * 1f + Vector3.up;
            GameObject effect = Instantiate(_meleeEffectPrefab, effectPos, transform.rotation);
            Destroy(effect, 1f);
        }
        
        DealMeleeDamage();
    }
    
    /// <summary>
    /// 근접 데미지 적용
    /// </summary>
    private void DealMeleeDamage()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _meleeRange, _meleeTargetLayer);
        
        foreach (Collider col in hitColliders)
        {
            if (col.transform == transform) continue;
            
            Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            
            if (angle > _meleeAngle * 0.5f) continue;
            
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
            
            if (_firePoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(_firePoint.position, 0.1f);
                Gizmos.DrawRay(_firePoint.position, _firePoint.forward * 2f);
                
                // 탄퍼짐 시각화 (원뿔)
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                float spreadRadius = Mathf.Tan(_currentSpread * Mathf.Deg2Rad) * 5f;
                Gizmos.DrawWireSphere(_firePoint.position + _firePoint.forward * 5f, spreadRadius);
            }
        }
        
        // 근접/원거리 전환 거리
        if (_attackType == AttackType.Both)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, _meleeRangeThreshold);
        }
    }
    
    #endregion
}
