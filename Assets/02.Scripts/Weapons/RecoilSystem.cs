using UnityEngine;
using System;

/// <summary>
/// 통합 반동 시스템
/// 카메라 반동, 크로스헤어 확산, 총기 킥백, 화면 흔들림을 관리
/// Main Camera에 부착하여 사용
/// </summary>
public class RecoilSystem : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CameraRotate _cameraRotate;
    [SerializeField] private CrosshairController _crosshairController;
    [SerializeField] private WeaponSway _weaponSway;
    
    [Header("디버그")]
    [SerializeField] private bool _showDebugInfo = false;
    
    // 현재 무기 데이터
    private WeaponData _currentWeaponData;
    
    // 반동 상태
    private Vector3 _currentRecoil;      // 현재 반동 상태 (부드럽게 적용됨)
    private Vector3 _targetRecoil;       // 목표 반동 (발사 시 누적)
    private float _currentSpread;        // 현재 크로스헤어 확산
    
    // 화면 흔들림
    private float _shakeTimer;
    private float _shakeMagnitude;
    
    // 탄퍼짐 상태
    private float _currentBulletSpread;   // 현재 탄퍼짐 (발사로 인한 누적)
    
    // 플레이어 상태 (외부에서 설정)
    private bool _isAiming = false;
    private bool _isMoving = false;
    private bool _isAirborne = false;
    private bool _isCrouching = false;
    private float _movementSpeed = 0f;    // 현재 이동 속도 (0~1 정규화)
    
    // 이벤트
    public event Action<float> OnSpreadChanged;        // 크로스헤어 확산 변경
    public event Action<Vector3> OnRecoilApplied;      // 반동 적용
    public event Action<float> OnBulletSpreadChanged;  // 탄퍼짐 변경
    
    // 싱글톤 (선택적)
    public static RecoilSystem Instance { get; private set; }
    
private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // 자동 참조 찾기 - CameraRotate
        if (_cameraRotate == null)
        {
            _cameraRotate = GetComponent<CameraRotate>();
            if (_cameraRotate == null)
            {
                _cameraRotate = GetComponentInParent<CameraRotate>();
            }
        }
        
        // 자동 참조 찾기 - CrosshairController
        if (_crosshairController == null)
        {
            _crosshairController = FindFirstObjectByType<CrosshairController>();
        }
        
        // 자동 참조 찾기 - WeaponSway (자식 오브젝트에서)
        if (_weaponSway == null)
        {
            _weaponSway = GetComponentInChildren<WeaponSway>();
        }
    }
    
    private void Update()
    {
        if (_currentWeaponData == null) return;
        
        UpdateRecoil();
        UpdateSpread();
        UpdateBulletSpread();  // 탄퍼짐 업데이트 추가
        UpdateScreenShake();
    }
    
    /// <summary>
    /// 현재 무기 설정
    /// </summary>
    public void SetWeaponData(WeaponData weaponData)
    {
        _currentWeaponData = weaponData;
        
        // 무기 변경 시 반동 초기화
        ResetRecoil();
    }
    
    /// <summary>
    /// 발사 시 반동 적용 (FirearmWeapon에서 호출)
    /// </summary>
    public void ApplyRecoil()
    {
        if (_currentWeaponData == null) return;
        
        ApplyRecoil(_currentWeaponData);
    }
    
    /// <summary>
    /// 특정 무기 데이터로 반동 적용
    /// </summary>
    public void ApplyRecoil(WeaponData weaponData)
    {
        if (weaponData == null) return;
        
        // ADS 상태에 따른 반동 배율
        float recoilMultiplier = _isAiming ? weaponData.adsRecoilMultiplier : 1f;
        
        // 1. 카메라 반동 계산
        float verticalKick = weaponData.verticalRecoil * recoilMultiplier;
        float horizontalKick = UnityEngine.Random.Range(-weaponData.horizontalRecoil, weaponData.horizontalRecoil) * recoilMultiplier;
        
        // 목표 반동 누적 (최대값 제한)
        _targetRecoil.x = Mathf.Clamp(
            _targetRecoil.x - verticalKick, 
            -weaponData.maxVerticalRecoil, 
            0f
        );
        _targetRecoil.y = Mathf.Clamp(
            _targetRecoil.y + horizontalKick, 
            -weaponData.maxHorizontalRecoil, 
            weaponData.maxHorizontalRecoil
        );
        
        // 2. 크로스헤어 확산 누적
        _currentSpread = Mathf.Clamp(
            _currentSpread + weaponData.crosshairSpreadPerShot * recoilMultiplier, 
            0f, 
            weaponData.maxCrosshairSpread
        );
        
        // 3. 총기 킥백 적용
        if (_weaponSway != null)
        {
            _weaponSway.ApplyGunKick(
                weaponData.gunKickbackDistance * recoilMultiplier, 
                weaponData.gunKickbackRotation * recoilMultiplier
            );
        }
        
        // 4. 화면 흔들림 적용
        if (weaponData.screenShakeIntensity > 0)
        {
            ApplyScreenShake(
                weaponData.screenShakeIntensity * recoilMultiplier, 
                weaponData.screenShakeDuration
            );
        }
        
        // 5. 탄퍼짐 누적
        _currentBulletSpread = Mathf.Clamp(
            _currentBulletSpread + weaponData.bulletSpreadPerShot * recoilMultiplier,
            0f,
            weaponData.maxBulletSpread - weaponData.baseBulletSpread
        );
        
        // 이벤트 발생
        OnRecoilApplied?.Invoke(_targetRecoil);
        OnSpreadChanged?.Invoke(_currentSpread);
        OnBulletSpreadChanged?.Invoke(GetTotalBulletSpread());
        
        if (_showDebugInfo)
        {
            Debug.Log($"[RecoilSystem] Applied - Vertical: {verticalKick:F2}, Horizontal: {horizontalKick:F2}, Spread: {_currentSpread:F1}");
        }
    }
    
    /// <summary>
    /// 반동 업데이트 (매 프레임)
    /// </summary>
/// <summary>
    /// 반동 업데이트 (매 프레임)
    /// </summary>
    private void UpdateRecoil()
    {
        if (_currentWeaponData == null) return;
        
        // 이전 반동 값 저장 (델타 계산용)
        Vector3 previousRecoil = _currentRecoil;
        
        // 1. 반동 스냅 적용 (목표 반동으로 부드럽게 이동)
        _currentRecoil = Vector3.Lerp(
            _currentRecoil, 
            _targetRecoil, 
            Time.deltaTime * _currentWeaponData.recoilSnappiness
        );
        
        // 2. 반동 회복 (목표 반동이 0으로 돌아감)
        _targetRecoil = Vector3.Lerp(
            _targetRecoil, 
            Vector3.zero, 
            Time.deltaTime * _currentWeaponData.recoilRecoverySpeed
        );
        
        // 3. 카메라에 반동 델타 적용
        Vector3 recoilDelta = _currentRecoil - previousRecoil;
        if (_cameraRotate != null && recoilDelta.sqrMagnitude > 0.0001f)
        {
            // x = 수직 (pitch), y = 수평 (yaw)
            _cameraRotate.AddRecoil(new Vector2(recoilDelta.x, recoilDelta.y));
        }
        
        // 4. 크로스헤어에 반동 오프셋 전달 (강화된 연동)
        if (_crosshairController != null)
        {
            // 현재 반동을 크로스헤어 오프셋으로 전달
            _crosshairController.SetRecoilOffset(new Vector2(_currentRecoil.x, _currentRecoil.y));
        }
    }
    
    /// <summary>
    /// 크로스헤어 확산 업데이트
    /// </summary>
    private void UpdateSpread()
    {
        if (_currentWeaponData == null) return;
        
        // 확산 회복
        float previousSpread = _currentSpread;
        _currentSpread = Mathf.Lerp(
            _currentSpread, 
            0f, 
            Time.deltaTime * _currentWeaponData.crosshairRecoverySpeed
        );
        
        // 크로스헤어 컨트롤러에 적용
        if (_crosshairController != null && Mathf.Abs(_currentSpread - previousSpread) > 0.01f)
        {
            _crosshairController.SetSpread(_currentSpread);
        }
        
        // 이벤트 발생 (값이 변경되었을 때만)
        if (Mathf.Abs(_currentSpread - previousSpread) > 0.01f)
        {
            OnSpreadChanged?.Invoke(_currentSpread);
        }
    }
    
    /// <summary>
    /// 화면 흔들림 업데이트
    /// </summary>
    private void UpdateScreenShake()
    {
        if (_shakeTimer > 0)
        {
            _shakeTimer -= Time.deltaTime;
            
            // 랜덤 오프셋 계산
            float offsetX = UnityEngine.Random.Range(-1f, 1f) * _shakeMagnitude;
            float offsetY = UnityEngine.Random.Range(-1f, 1f) * _shakeMagnitude;
            
            // 카메라에 오프셋 적용
            if (_cameraRotate != null)
            {
                _cameraRotate.SetRecoilOffset(new Vector3(offsetX, offsetY, 0f));
            }
        }
        else if (_shakeMagnitude > 0)
        {
            // 흔들림 종료 시 오프셋 초기화
            _shakeMagnitude = 0f;
            if (_cameraRotate != null)
            {
                _cameraRotate.ResetRecoilOffset();
            }
        }
    }

/// <summary>
    /// 탄퍼짐 업데이트 (발사로 인한 누적 탄퍼짐 회복)
    /// </summary>
    private void UpdateBulletSpread()
    {
        if (_currentWeaponData == null) return;
        
        float previousSpread = _currentBulletSpread;
        
        // 탄퍼짐 회복 (0으로 돌아감)
        _currentBulletSpread = Mathf.Lerp(
            _currentBulletSpread,
            0f,
            Time.deltaTime * _currentWeaponData.bulletSpreadRecoverySpeed
        );
        
        // 값이 변경되었을 때만 이벤트 발생
        if (Mathf.Abs(_currentBulletSpread - previousSpread) > 0.001f)
        {
            OnBulletSpreadChanged?.Invoke(GetTotalBulletSpread());
        }
    }

/// <summary>
    /// 총 탄퍼짐 각도 계산 (기본 + 누적 + 플레이어 상태 반영)
    /// </summary>
    public float GetTotalBulletSpread()
    {
        if (_currentWeaponData == null) return 0f;
        
        // 기본 탄퍼짐 + 발사로 인한 누적 탄퍼짐
        float totalSpread = _currentWeaponData.baseBulletSpread + _currentBulletSpread;
        
        // 플레이어 상태에 따른 배율 적용
        float stateMultiplier = 1f;
        
        // 공중 상태가 가장 높은 페널티
        if (_isAirborne)
        {
            stateMultiplier = _currentWeaponData.airborneSpreadMultiplier;
        }
        // 이동 중 (이동 속도에 비례)
        else if (_isMoving)
        {
            stateMultiplier = Mathf.Lerp(1f, _currentWeaponData.movementSpreadMultiplier, _movementSpeed);
        }
        // 웅크리기 중
        else if (_isCrouching)
        {
            stateMultiplier = _currentWeaponData.crouchSpreadMultiplier;
        }
        
        // 조준(ADS) 시 추가 배율 적용
        if (_isAiming)
        {
            stateMultiplier *= _currentWeaponData.adsSpreadMultiplier;
        }
        
        totalSpread *= stateMultiplier;
        
        // 최대값 제한
        return Mathf.Clamp(totalSpread, 0f, _currentWeaponData.maxBulletSpread);
    }

/// <summary>
    /// 탄퍼짐이 적용된 발사 방향 반환
    /// FirearmWeapon에서 호출하여 실제 발사 방향에 사용
    /// </summary>
    /// <param name="baseDirection">기본 발사 방향 (카메라 forward)</param>
    /// <returns>탄퍼짐이 적용된 방향</returns>
    public Vector3 GetSpreadDirection(Vector3 baseDirection)
    {
        float spreadAngle = GetTotalBulletSpread();
        
        if (spreadAngle <= 0.001f)
        {
            return baseDirection;
        }
        
        // 랜덤 탄퍼짐 각도 계산 (원뿐 내 균등 분포)
        float randomAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float randomRadius = Mathf.Sqrt(UnityEngine.Random.Range(0f, 1f)) * spreadAngle;
        
        // 탄퍼짐 오프셋 계산
        float spreadX = Mathf.Cos(randomAngle) * randomRadius;
        float spreadY = Mathf.Sin(randomAngle) * randomRadius;
        
        // 기본 방향을 기준으로 회전 적용
        Quaternion baseRotation = Quaternion.LookRotation(baseDirection);
        Quaternion spreadRotation = Quaternion.Euler(spreadY, spreadX, 0f);
        
        Vector3 spreadDirection = baseRotation * spreadRotation * Vector3.forward;
        
        if (_showDebugInfo)
        {
            Debug.Log($"[RecoilSystem] Spread: {spreadAngle:F2}°, Applied: ({spreadX:F2}, {spreadY:F2})");
        }
        
        return spreadDirection.normalized;
    }



    
    /// <summary>
    /// 화면 흔들림 적용
    /// </summary>
    private void ApplyScreenShake(float intensity, float duration)
    {
        _shakeMagnitude = intensity;
        _shakeTimer = duration;
    }
    
    /// <summary>
    /// ADS 상태 설정
    /// </summary>
    public void SetAiming(bool isAiming)
    {
        _isAiming = isAiming;
        
        // 크로스헤어에도 전달
        if (_crosshairController != null)
        {
            _crosshairController.SetAiming(isAiming);
        }
    }

/// <summary>
    /// 이동 상태 설정 (PlayerController에서 호출)
    /// </summary>
    /// <param name="isMoving">이동 여부</param>
    /// <param name="normalizedSpeed">정규화된 이동 속도 (0~1)</param>
    public void SetMovementState(bool isMoving, float normalizedSpeed = 1f)
    {
        _isMoving = isMoving;
        _movementSpeed = Mathf.Clamp01(normalizedSpeed);
    }
    
    /// <summary>
    /// 공중 상태 설정 (PlayerController에서 호출)
    /// </summary>
    public void SetAirborne(bool isAirborne)
    {
        _isAirborne = isAirborne;
    }
    
    /// <summary>
    /// 웅크리기 상태 설정 (PlayerController에서 호출)
    /// </summary>
    public void SetCrouching(bool isCrouching)
    {
        _isCrouching = isCrouching;
    }
    
    /// <summary>
    /// 모든 플레이어 상태를 한 번에 설정
    /// </summary>
    public void SetPlayerState(bool isMoving, bool isAirborne, bool isCrouching, float normalizedSpeed = 1f)
    {
        _isMoving = isMoving;
        _isAirborne = isAirborne;
        _isCrouching = isCrouching;
        _movementSpeed = Mathf.Clamp01(normalizedSpeed);
    }

    
    /// <summary>
    /// 반동 초기화
    /// </summary>
/// <summary>
    /// 반동 및 탄퍼짐 초기화
    /// </summary>
    public void ResetRecoil()
    {
        _currentRecoil = Vector3.zero;
        _targetRecoil = Vector3.zero;
        _currentSpread = 0f;
        _currentBulletSpread = 0f;  // 탄퍼짐 초기화
        _shakeTimer = 0f;
        _shakeMagnitude = 0f;
        
        if (_cameraRotate != null)
        {
            _cameraRotate.ResetRecoilOffset();
        }
        
        if (_crosshairController != null)
        {
            _crosshairController.SetSpread(0f);
        }
    }
    
    /// <summary>
    /// 크로스헤어 컨트롤러 설정
    /// </summary>
    public void SetCrosshairController(CrosshairController controller)
    {
        _crosshairController = controller;
    }
    
    /// <summary>
    /// WeaponSway 설정
    /// </summary>
    public void SetWeaponSway(WeaponSway sway)
    {
        _weaponSway = sway;
    }
    
    /// <summary>
    /// 현재 크로스헤어 확산값 반환
    /// </summary>
    public float GetCurrentSpread()
    {
        return _currentSpread;
    }
    
    /// <summary>
    /// 현재 반동 상태 반환
    /// </summary>
    public Vector3 GetCurrentRecoil()
    {
        return _currentRecoil;
    }
    
    /// <summary>
    /// ADS 상태 반환
    /// </summary>
    public bool IsAiming()
    {
        return _isAiming;
    }

/// <summary>
    /// 현재 탄퍼짐 누적값 반환 (발사로 인한 것만)
    /// </summary>
    public float GetCurrentBulletSpread()
    {
        return _currentBulletSpread;
    }
    
    /// <summary>
    /// 이동 상태 반환
    /// </summary>
    public bool IsMoving()
    {
        return _isMoving;
    }
    
    /// <summary>
    /// 공중 상태 반환
    /// </summary>
    public bool IsAirborne()
    {
        return _isAirborne;
    }
    
    /// <summary>
    /// 웅크리기 상태 반환
    /// </summary>
    public bool IsCrouching()
    {
        return _isCrouching;
    }

    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
