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
    
    [Header("반동 적용 대상")]
    [SerializeField] private Transform _recoilTarget; // 카메라 또는 카메라 회전 스크립트
    
    // 반동 관련
    private Vector3 _currentRecoil;
    private Vector3 _targetRecoil;
    
    // 점사 모드용
    private int _burstCount;
    private const int BURST_AMOUNT = 3;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 카메라 자동 찾기
        if (_cameraTransform == null)
        {
            _cameraTransform = Camera.main?.transform;
        }
    }
    
    private void Update()
    {
        // 반동 회복
        RecoverRecoil();
    }
    
    /// <summary>
    /// 발사 시도
    /// </summary>
    public override void TryFire()
    {
        if (_isReloading)
        {
            return;
        }
        
        if (_currentMagazine <= 0)
        {
            PlayEmptySound();
            return;
        }
        
        if (Time.time < _nextFireTime)
        {
            return;
        }
        
        Fire();
    }
    
    /// <summary>
    /// 실제 발사 처리
    /// </summary>
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
        
        // 반동 적용
        ApplyRecoil();
        
        // 탄약 소모
        ConsumeAmmo();
        
        // 점사 모드 처리
        if (_weaponData.fireMode == FireMode.Burst)
        {
            _burstCount++;
            if (_burstCount >= BURST_AMOUNT)
            {
                _burstCount = 0;
                _nextFireTime = Time.time + 0.3f;
            }
        }
    }
    
    /// <summary>
    /// Raycast로 타격 판정
    /// </summary>
    private void PerformRaycast()
    {
        if (_cameraTransform == null) return;
        
        Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);
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
        
        // 발사 방향 계산 (카메라 중앙 방향)
        Vector3 shootDirection = _cameraTransform != null 
            ? _cameraTransform.forward 
            : _muzzlePoint.forward;
        
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
        
        // 탄흔 생성
        SpawnBulletHole(hit);
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
    private void PlayMuzzleFlash()
    {
        if (_muzzleFlashParticle != null)
        {
            _muzzleFlashParticle.Play();
        }
        
        // 프리팹 방식
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
    /// 반동 적용
    /// </summary>
    private void ApplyRecoil()
    {
        if (_weaponData == null) return;
        
        float verticalKick = _weaponData.verticalRecoil;
        float horizontalKick = Random.Range(-_weaponData.horizontalRecoil, _weaponData.horizontalRecoil);
        
        _targetRecoil += new Vector3(-verticalKick, horizontalKick, 0f);
    }
    
    /// <summary>
    /// 반동 회복
    /// </summary>
    private void RecoverRecoil()
    {
        if (_weaponData == null) return;
        
        // 부드러운 반동 적용
        _currentRecoil = Vector3.Lerp(_currentRecoil, _targetRecoil, Time.deltaTime * 10f);
        
        // 반동 회복
        _targetRecoil = Vector3.Lerp(_targetRecoil, Vector3.zero, Time.deltaTime * _weaponData.recoilRecoverySpeed);
        
        // 카메라에 반동 적용 (필요시 CameraRotate와 연동)
        if (_recoilTarget != null)
        {
            // 실제 반동 적용은 CameraRotate 스크립트와 연동 필요
        }
    }
    
    /// <summary>
    /// 현재 반동값 반환 (외부에서 카메라에 적용용)
    /// </summary>
    public Vector3 GetRecoilDelta()
    {
        Vector3 delta = _currentRecoil - _targetRecoil;
        return delta * Time.deltaTime;
    }
}

/// <summary>
/// 데미지를 받을 수 있는 오브젝트 인터페이스
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);
}
