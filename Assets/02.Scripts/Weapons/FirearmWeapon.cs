using UnityEngine;

/// <summary>
/// Raycast 기반 총기 무기 클래스
/// Hitscan 방식으로 즉시 타격 판정
/// </summary>
public class FirearmWeapon : WeaponBase
{
    [Header("Raycast 설정")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private LayerMask _hitLayers = ~0; // 모든 레이어
    
    [Header("머즐 플래시")]
    [SerializeField] private ParticleSystem _muzzleFlashParticle;
    
    [Header("반동 시스템")]
    [SerializeField] private RecoilSystem _recoilSystem;
    
    // 점사 모드용
    private int _burstCount;
    private const int BURST_AMOUNT = 3;
    
    // 런타임 발사 모드 (WeaponData의 fireMode 대신 사용)
    private FireMode _currentFireMode;
    private bool _isTriggerHeld = false;
    private bool _hasFiredThisTrigger = false;
    
    // 발사 모드 변경 이벤트
    public event System.Action<FireMode> OnFireModeChanged;
    
    /// <summary>
    /// 현재 발사 모드 프로퍼티
    /// </summary>
    

    /// <summary>
    /// 트리거(마우스 버튼) 상태 설정
    /// 외부에서 호출하여 트리거 누름/뗴 상태 전달
    /// </summary>
    public void SetTriggerState(bool isHeld)
    {
        bool wasPreviouslyHeld = _isTriggerHeld;
        _isTriggerHeld = isHeld;
        
        // 트리거를 떼을 때 플래그 리셋
        if (wasPreviouslyHeld && !isHeld)
        {
            _hasFiredThisTrigger = false;
        }
    }
public FireMode CurrentFireMode => _currentFireMode;

    
    protected override void Awake()
    {
        base.Awake();
        
        // 카메라 자동 찾기
        if (_cameraTransform == null)
        {
            _cameraTransform = Camera.main?.transform;
        }
        
        // RecoilSystem 자동 찾기
        if (_recoilSystem == null)
        {
            _recoilSystem = RecoilSystem.Instance;
            if (_recoilSystem == null)
            {
                _recoilSystem = FindFirstObjectByType<RecoilSystem>();
            }
        }
    }


    /// <summary>
    /// 매 프레임 B키 입력 체크 및 초기화
    /// </summary>
    private void Update()
    {
        // 초기 발사 모드 설정 (첫 프레임에서)
        if (_weaponData != null && _currentFireMode == 0 && !System.Enum.IsDefined(typeof(FireMode), _currentFireMode))
        {
            _currentFireMode = _weaponData.fireMode;
        }
        
        // B키로 발사 모드 전환
        if (Input.GetKeyDown(KeyCode.B))
        {
            CycleFireMode();
        }
    }
    
    /// <summary>
    /// 발사 모드 순환 (Semi → Burst → Auto → Semi...)
    /// </summary>
    public void CycleFireMode()
    {
        switch (_currentFireMode)
        {
            case FireMode.Semi:
                _currentFireMode = FireMode.Burst;
                break;
            case FireMode.Burst:
                _currentFireMode = FireMode.Auto;
                break;
            case FireMode.Auto:
                _currentFireMode = FireMode.Semi;
                break;
        }
        
        // 모드 변경 시 점사 카운트 초기화
        _burstCount = 0;
        _hasFiredThisTrigger = false;
        
        Debug.Log($"[FirearmWeapon] 발사 모드 변경: {_currentFireMode}");
        OnFireModeChanged?.Invoke(_currentFireMode);
    }
    
    /// <summary>
    /// 발사 모드 직접 설정
    /// </summary>
    public void SetFireMode(FireMode mode)
    {
        if (_currentFireMode != mode)
        {
            _currentFireMode = mode;
            _burstCount = 0;
            _hasFiredThisTrigger = false;
            
            Debug.Log($"[FirearmWeapon] 발사 모드 설정: {_currentFireMode}");
            OnFireModeChanged?.Invoke(_currentFireMode);
        }
    }

    
    /// <summary>
    /// 무기 장착 시 호출
    /// </summary>
    public override void OnWeaponEquip()
    {
        base.OnWeaponEquip();
        
        // RecoilSystem에 현재 무기 데이터 설정
        if (_recoilSystem != null && _weaponData != null)
        {
            _recoilSystem.SetWeaponData(_weaponData);
        }
        
        // 발사 모드 초기화
        _currentFireMode = _weaponData.fireMode;
        OnFireModeChanged?.Invoke(_currentFireMode);

    }
    
    /// <summary>
    /// 발사 시도
    /// </summary>
public override void TryFire()
    {
        if (_isReloading) return;
        
        if (_currentMagazine <= 0)
        {
            PlayEmptySound();
            return;
        }
        
        if (Time.time < _nextFireTime) return;
        
        // 발사 모드별 처리
        switch (_currentFireMode)
        {
            case FireMode.Semi:
                // 단발: 트리거당 1발만 발사
                if (!_hasFiredThisTrigger)
                {
                    Fire();
                    _hasFiredThisTrigger = true;
                }
                break;
                
            case FireMode.Burst:
                // 점사: 트리거당 3발 연속 발사
                if (!_hasFiredThisTrigger || _burstCount > 0)
                {
                    Fire();
                    _burstCount++;
                    if (_burstCount >= BURST_AMOUNT)
                    {
                        _burstCount = 0;
                        _hasFiredThisTrigger = true;
                        _nextFireTime = Time.time + 0.2f; // 점사 후 쿨다운
                    }
                }
                break;
                
            case FireMode.Auto:
                // 연발: 누르고 있으면 계속 발사
                Fire();
                break;
        }
    }
    
    /// <summary>
    /// 실제 발사 처리
    /// </summary>
protected override void Fire()
    {
        // 발사 타입에 따른 처리
        switch (_weaponData.fireType)
        {
            case FireType.Hitscan:
                PerformRaycast();
                break;
            case FireType.Projectile:
                FireProjectile();
                break;
        }
        
        // 이펙트
        PlayMuzzleFlash();
        PlaySound(_weaponData.fireSound);
        
        // 반동 적용 (RecoilSystem 사용)
        ApplyRecoil();
        
        // 탄약 소모
        ConsumeAmmo();
    }
    
    /// <summary>
    /// Raycast로 타격 판정
    /// </summary>
/// <summary>
    /// Raycast로 타격 판정 (탄퍼짐 적용)
    /// </summary>
    private void PerformRaycast()
    {
        if (_cameraTransform == null) return;
        
        // 기본 발사 방향
        Vector3 baseDirection = _cameraTransform.forward;
        
        // 탄퍼짐이 적용된 발사 방향
        Vector3 shootDirection = baseDirection;
        if (_recoilSystem != null)
        {
            shootDirection = _recoilSystem.GetSpreadDirection(baseDirection);
        }
        
        Ray ray = new Ray(_cameraTransform.position, shootDirection);
        RaycastHit hit;
        
        // 디버그용 레이 시각화
        Debug.DrawRay(ray.origin, ray.direction * _weaponData.range, Color.red, 0.1f);
        
        if (Physics.Raycast(ray, out hit, _weaponData.range, _hitLayers))
        {
            // 히트 처리
            OnHit(hit);
        }
    }
    
    /// <summary>
    /// 발사체(총알) 발사
    /// </summary>
/// <summary>
    /// 발사체(총알) 발사 (탄퍼짐 적용)
    /// </summary>
    private void FireProjectile()
    {
        if (_weaponData.projectilePrefab == null)
        {
            Debug.LogWarning($"[{_weaponData.weaponName}] Projectile prefab이 설정되지 않았습니다!");
            return;
        }
        
        if (_muzzlePoint == null)
        {
            Debug.LogWarning($"[{_weaponData.weaponName}] MuzzlePoint가 설정되지 않았습니다!");
            return;
        }
        
        // 크로스헤어(화면 중앙)가 가리키는 목표 지점 계산
        Vector3 targetPoint = GetCrosshairTargetPoint();
        
        // MuzzlePoint에서 목표 지점을 향한 기본 발사 방향
        Vector3 baseDirection = (targetPoint - _muzzlePoint.position).normalized;
        
        // 탄퍼짐이 적용된 발사 방향
        Vector3 shootDirection = baseDirection;
        if (_recoilSystem != null)
        {
            shootDirection = _recoilSystem.GetSpreadDirection(baseDirection);
        }
        
        // 발사체 생성
        GameObject projectileObj = Instantiate(
            _weaponData.projectilePrefab,
            _muzzlePoint.position,
            Quaternion.LookRotation(shootDirection)
        );
        
        // Projectile 컴포넌트 초기화
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(
                _weaponData.projectileSpeed,
                _weaponData.damage,
                shootDirection,
                transform.root.gameObject
            );
            projectile.SetGravity(_weaponData.projectileUseGravity);
            projectile.SetHitLayers(_hitLayers);
        }
        else
        {
            // Projectile 컴포넌트가 없으면 Rigidbody로 발사
            Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = shootDirection * _weaponData.projectileSpeed;
            }
        }
        
        Debug.Log($"[{_weaponData.weaponName}] Projectile fired!");
    }
    
    /// <summary>
    /// 크로스헤어(화면 중앙)가 가리키는 월드 좌표 목표 지점 반환
    /// </summary>
    private Vector3 GetCrosshairTargetPoint()
    {
        if (_cameraTransform == null) 
        {
            return _muzzlePoint.position + _muzzlePoint.forward * _weaponData.range;
        }
        
        Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, _weaponData.range, _hitLayers))
        {
            return hit.point;
        }
        else
        {
            // 아무것도 맞지 않으면 최대 사거리 지점 반환
            return _cameraTransform.position + _cameraTransform.forward * _weaponData.range;
        }
    }
    
    /// <summary>
    /// 피격 처리
    /// </summary>
    private void OnHit(RaycastHit hit)
    {
        Debug.Log($"[{_weaponData.weaponName}] Hit: {hit.collider.name} at {hit.point}");
        
        // 데미지 처리 (IDamageable 인터페이스가 있다면)
        var damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(_weaponData.damage, hit.point, hit.normal);
        }
        
        // 임팩트 이펙트 재생 (ImpactEffectManager 사용)
        if (ImpactEffectManager.Instance != null)
        {
            ImpactEffectManager.Instance.PlayImpact(hit);
        }
        else
        {
            // 폴백: 기존 탄흔 생성
            SpawnBulletHole(hit);
        }
    }
    
    /// <summary>
    /// 탄흔 생성
    /// </summary>
    private void SpawnBulletHole(RaycastHit hit)
    {
        if (_weaponData.bulletHolePrefab == null) return;
        
        GameObject hole = Instantiate(
            _weaponData.bulletHolePrefab, 
            hit.point + hit.normal * 0.001f, // 표면에서 살짝 떨어뜨림
            Quaternion.LookRotation(hit.normal)
        );
        
        // 5초 후 자동 삭제
        Destroy(hole, 5f);
    }
    
    /// <summary>
    /// 머즐 플래시 재생
    /// </summary>
    /// <summary>
    /// 머즐 플래시 재생 (War FX 연동)
    /// </summary>
    private void PlayMuzzleFlash()
    {
        // MuzzleFlashController 사용 (우선)
        if (MuzzleFlashController.Instance != null && _muzzlePoint != null)
        {
            // WeaponData에 커스텀 머즐플래시 프리팩이 있으면 사용
            if (_weaponData.muzzleFlashPrefab != null)
            {
                MuzzleFlashController.Instance.PlayCustomMuzzleFlash(
                    _weaponData.muzzleFlashPrefab, 
                    _muzzlePoint
                );
            }
            else
            {
                // 기본 머즐플래시 (무기 타입별)
                MuzzleFlashController.Instance.PlayMuzzleFlash(
                    _muzzlePoint, 
                    _weaponData.weaponType
                );
            }
            return;
        }
        
        // 폴백: 기존 파티클 시스템 사용
        if (_muzzleFlashParticle != null)
        {
            _muzzleFlashParticle.Play();
        }
        
        // 폴백: 프리팩 방식
        if (_weaponData.muzzleFlashPrefab != null && _muzzlePoint != null)
        {
            GameObject flash = Instantiate(
                _weaponData.muzzleFlashPrefab, 
                _muzzlePoint.position, 
                _muzzlePoint.rotation
            );
            Destroy(flash, 0.1f);
        }
    }
    
    /// <summary>
    /// 반동 적용 (RecoilSystem에 위임)
    /// </summary>
private void ApplyRecoil()
    {
        Debug.Log($"[FirearmWeapon] ApplyRecoil called - RecoilSystem: {_recoilSystem != null}, WeaponData: {_weaponData != null}");
        
        if (_recoilSystem != null && _weaponData != null)
        {
            Debug.Log($"[FirearmWeapon] Calling RecoilSystem.ApplyRecoil with {_weaponData.weaponName}");
            _recoilSystem.ApplyRecoil(_weaponData);
        }
        else
        {
            Debug.LogWarning($"[FirearmWeapon] Cannot apply recoil - RecoilSystem: {_recoilSystem}, WeaponData: {_weaponData}");
        }
    }
    
    /// <summary>
    /// RecoilSystem 설정
    /// </summary>
    public void SetRecoilSystem(RecoilSystem recoilSystem)
    {
        _recoilSystem = recoilSystem;
    }
}
