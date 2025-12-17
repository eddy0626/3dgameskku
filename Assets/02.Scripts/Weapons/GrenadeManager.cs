using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using System;

/// <summary>
/// 플레이어의 수류탄 관리 컴포넌트
/// 투척, 쿠킹, 인벤토리 관리, ObjectPool 적용
/// FPS/TPS 모든 시점에서 화면 중앙 조준 지원
/// </summary>
public class GrenadeManager : MonoBehaviour
{
    [Header("수류탄 설정")]
    [SerializeField] private GrenadeData _grenadeData;
    
    [Header("투척 설정")]
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private float _maxAimDistance = 100f;
    [SerializeField] private LayerMask _aimLayerMask = -1;
    
    [Header("인벤토리")]
    [SerializeField] private int _startGrenades = 5;
    [SerializeField] private int _maxGrenades = 10;
    private int _currentGrenades;
    
    [Header("쿠킹 설정")]
    [SerializeField] private bool _enableCooking = true;
    [SerializeField] private float _maxCookTime = 2.5f;
    
    [Header("착탄 마커")]
    [SerializeField] private bool _showImpactMarker = true;
    [SerializeField] private GrenadeImpactMarker _impactMarkerPrefab;
    private GrenadeImpactMarker _impactMarker;
    private Vector3 _predictedImpactPoint;
    private Vector3 _predictedImpactNormal;
    
    [Header("궤적 표시")]
    [SerializeField] private bool _showTrajectory = true;
    [SerializeField] private LineRenderer _trajectoryLine;
    [SerializeField] private int _trajectoryResolution = 30;
    
    [Header("오브젝트 풀링")]
    [SerializeField] private int _poolDefaultCapacity = 5;
    [SerializeField] private int _poolMaxSize = 10;
    [SerializeField] private int _explosionPoolDefaultCapacity = 10;
    [SerializeField] private int _explosionPoolMaxSize = 20;
    
    // 상태
    private float _nextThrowTime;
    private bool _isCooking;
    private float _cookStartTime;
    private bool _grenadeReady;
    
    // 오브젝트 풀
    private ObjectPool<Grenade> _grenadePool;
    private ObjectPool<GameObject> _explosionPool;
    
    // 이벤트
    public event Action<int, int> OnGrenadeCountChanged;
    public event Action OnGrenadeThrownEvent;
    public event Action<float> OnCookingProgress;
    
    // 프로퍼티
    public int CurrentGrenades => _currentGrenades;
    public int MaxGrenades => _maxGrenades;
    public bool CanThrow => _currentGrenades > 0 && Time.time >= _nextThrowTime && !_isCooking;
    public bool IsCooking => _isCooking;
    public float CookProgress => _isCooking ? Mathf.Clamp01((Time.time - _cookStartTime) / _grenadeData.fuseTime) : 0f;
    
    public static GrenadeManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        
        if (_playerCamera == null)
        {
            _playerCamera = Camera.main;
        }
        
        if (_throwPoint == null)
        {
            _throwPoint = _playerCamera?.transform;
        }
        
        _currentGrenades = _startGrenades;
        
        InitializePools();
        SetupTrajectoryLine();
        InitializeImpactMarker();
    }
    
    private void Start()
    {
        OnGrenadeCountChanged?.Invoke(_currentGrenades, _maxGrenades);
    }
    
private void Update()
    {
        // 게임이 Playing 상태가 아니면 입력 처리 안함
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying) return;
        
        HandleLegacyInput();
        
        if (_showTrajectory && _trajectoryLine != null)
        {
            if (_grenadeReady || _isCooking)
            {
                UpdateTrajectoryLine();
                _trajectoryLine.enabled = true;
            }
            else
            {
                _trajectoryLine.enabled = false;
            }
            UpdateImpactMarker();
        }
        
        if (_isCooking)
        {
            OnCookingProgress?.Invoke(CookProgress);
            
            if (_enableCooking)
            {
                float cookTime = Time.time - _cookStartTime;
                if (cookTime >= _maxCookTime)
                {
                    ThrowGrenade();
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        _grenadePool?.Dispose();
        _explosionPool?.Dispose();
        
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #region Aim Point Calculation

    private const float _fallbackAimDistance = 50f;

    /// <summary>
    /// 화면 중앙에서 Raycast를 쏴서 실제 조준점을 계산
    /// FPS/TPS 모든 시점에서 정확한 조준 지원
    /// </summary>
    private Vector3 GetAimPoint()
    {
        // 카메라 검증 및 자동 복구
        Camera activeCamera = ValidateAndGetCamera();

        if (activeCamera == null)
        {
            Debug.LogWarning("[GrenadeManager] 유효한 카메라를 찾을 수 없습니다. throwPoint forward 사용");
            return _throwPoint.position + _throwPoint.forward * _fallbackAimDistance;
        }

        // 화면 중앙에서 Ray 생성
        Ray aimRay = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Raycast로 조준점 찾기
        if (Physics.Raycast(aimRay, out RaycastHit hit, _maxAimDistance, _aimLayerMask))
        {
            return hit.point;
        }

        // hit 없으면 카메라 forward 방향 * 50m 지점 사용
        return activeCamera.transform.position + activeCamera.transform.forward * _fallbackAimDistance;
    }

    /// <summary>
    /// 카메라 검증 및 자동 복구
    /// </summary>
    private Camera ValidateAndGetCamera()
    {
        // 1. 할당된 카메라가 유효한지 확인
        if (_playerCamera != null && _playerCamera.isActiveAndEnabled)
        {
            return _playerCamera;
        }

        // 2. Camera.main으로 복구 시도
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.isActiveAndEnabled)
        {
            _playerCamera = mainCamera;
            Debug.Log("[GrenadeManager] Camera.main으로 카메라 재할당됨");
            return _playerCamera;
        }

        // 3. 씬에서 활성화된 카메라 검색
        Camera[] allCameras = Camera.allCameras;
        foreach (Camera cam in allCameras)
        {
            if (cam.isActiveAndEnabled && cam.CompareTag("MainCamera"))
            {
                _playerCamera = cam;
                Debug.Log($"[GrenadeManager] MainCamera 태그 카메라로 재할당됨: {cam.name}");
                return _playerCamera;
            }
        }

        // 4. 태그 없어도 활성화된 첫 번째 카메라 사용
        if (allCameras.Length > 0)
        {
            _playerCamera = allCameras[0];
            Debug.LogWarning($"[GrenadeManager] 첫 번째 활성 카메라로 재할당됨: {_playerCamera.name}");
            return _playerCamera;
        }

        return null;
    }
    
    /// <summary>
    /// 투척 포인트에서 조준점을 향한 방향 계산
    /// </summary>
    private Vector3 GetThrowDirection()
    {
        Vector3 aimPoint = GetAimPoint();
        Vector3 throwDirection = (aimPoint - _throwPoint.position).normalized;
        
        // 방향이 너무 아래를 향하지 않도록 최소 각도 제한
        if (throwDirection.y < -0.9f)
        {
            throwDirection.y = -0.9f;
            throwDirection = throwDirection.normalized;
        }
        
        return throwDirection;
    }
    
    #endregion

    #region Object Pooling
    
    private void InitializePools()
    {
        _grenadePool = new ObjectPool<Grenade>(
            createFunc: CreateGrenade,
            actionOnGet: OnGrenadeGet,
            actionOnRelease: OnGrenadeRelease,
            actionOnDestroy: OnGrenadeDestroy,
            collectionCheck: true,
            defaultCapacity: _poolDefaultCapacity,
            maxSize: _poolMaxSize
        );
        
        if (_grenadeData?.explosionPrefab != null)
        {
            _explosionPool = new ObjectPool<GameObject>(
                createFunc: CreateExplosion,
                actionOnGet: OnExplosionGet,
                actionOnRelease: OnExplosionRelease,
                actionOnDestroy: OnExplosionDestroy,
                collectionCheck: true,
                defaultCapacity: _explosionPoolDefaultCapacity,
                maxSize: _explosionPoolMaxSize
            );
        }
    }
    
    private Grenade CreateGrenade()
    {
        if (_grenadeData?.grenadePrefab == null) return null;
        
        GameObject grenadeObj = Instantiate(_grenadeData.grenadePrefab);
        Grenade grenade = grenadeObj.GetComponent<Grenade>();
        
        if (grenade != null)
        {
            grenade.SetPool(_grenadePool, _explosionPool);
        }
        
        grenadeObj.SetActive(false);
        return grenade;
    }
    
    private void OnGrenadeGet(Grenade grenade)
    {
        grenade.gameObject.SetActive(true);
    }
    
    private void OnGrenadeRelease(Grenade grenade)
    {
        grenade.gameObject.SetActive(false);
        grenade.ResetGrenade();
    }
    
    private void OnGrenadeDestroy(Grenade grenade)
    {
        if (grenade != null)
        {
            Destroy(grenade.gameObject);
        }
    }
    
    private GameObject CreateExplosion()
    {
        if (_grenadeData?.explosionPrefab == null) return null;
        
        GameObject explosionObj = Instantiate(_grenadeData.explosionPrefab);
        explosionObj.SetActive(false);
        return explosionObj;
    }
    
    private void OnExplosionGet(GameObject explosion)
    {
        explosion.SetActive(true);
    }
    
    private void OnExplosionRelease(GameObject explosion)
    {
        explosion.SetActive(false);
    }
    
    private void OnExplosionDestroy(GameObject explosion)
    {
        if (explosion != null)
        {
            Destroy(explosion);
        }
    }
    
    public GameObject GetExplosionFromPool()
    {
        return _explosionPool?.Get();
    }
    
    public void ReturnExplosionToPool(GameObject explosion)
    {
        _explosionPool?.Release(explosion);
    }
    
    #endregion

    private void HandleLegacyInput()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            PrepareGrenade();
        }
        
        if (Input.GetKeyUp(KeyCode.G))
        {
            if (_grenadeReady || _isCooking)
            {
                ThrowGrenade();
            }
        }
    }

    public void PrepareGrenade()
    {
        if (!CanThrow) return;
        
        _grenadeReady = true;
        
        if (_enableCooking)
        {
            _isCooking = true;
            _cookStartTime = Time.time;
        }
    }
    
    public void ThrowGrenade()
    {
        if (_currentGrenades <= 0) return;
        if (!_grenadeReady && !_isCooking) return;
        
        float cookTime = 0f;
        if (_isCooking && _enableCooking)
        {
            cookTime = Time.time - _cookStartTime;
        }
        
        SpawnGrenade(cookTime);
        PlayThrowSound();
        
        _currentGrenades--;
        _nextThrowTime = Time.time + _grenadeData.throwCooldown;
        _isCooking = false;
        _grenadeReady = false;
        
        OnGrenadeCountChanged?.Invoke(_currentGrenades, _maxGrenades);
        OnGrenadeThrownEvent?.Invoke();
        OnCookingProgress?.Invoke(0f);
    }
    
    public void CancelThrow()
    {
        _isCooking = false;
        _grenadeReady = false;
        OnCookingProgress?.Invoke(0f);
    }
    
    /// <summary>
    /// 수류탄 생성 및 발사 - 카메라 Raycast 기반 조준점 사용
    /// </summary>
    private void SpawnGrenade(float cookTime)
    {
        if (_grenadeData?.grenadePrefab == null)
        {
            Debug.LogWarning("GrenadeManager: 수류탄 프리팹이 설정되지 않았습니다.");
            return;
        }
        
        // 투척 방향 계산 (카메라 Raycast 기반 - FPS/TPS 모두 지원)
        Vector3 throwDirection = GetThrowDirection();
        
        Debug.Log($"[GrenadeManager] 던지기 방향: {throwDirection}, 조준점: {GetAimPoint()}");
        
        Grenade grenade = _grenadePool.Get();
        
        if (grenade != null)
        {
            grenade.transform.position = _throwPoint.position;
            grenade.transform.rotation = Quaternion.LookRotation(throwDirection);
            grenade.Initialize(_grenadeData, throwDirection, gameObject, cookTime);
        }
        else
        {
            Debug.LogWarning("GrenadeManager: 풀에서 수류탄을 가져올 수 없습니다.");
        }
    }
    
    private void SetupTrajectoryLine()
    {
        if (_trajectoryLine == null && _showTrajectory)
        {
            GameObject lineObj = new GameObject("GrenadeTrajectory");
            lineObj.transform.SetParent(transform);
            _trajectoryLine = lineObj.AddComponent<LineRenderer>();
            
            _trajectoryLine.startWidth = 0.05f;
            _trajectoryLine.endWidth = 0.02f;
            _trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
            _trajectoryLine.startColor = new Color(1f, 0.5f, 0f, 0.8f);
            _trajectoryLine.endColor = new Color(1f, 0.2f, 0f, 0.3f);
            _trajectoryLine.positionCount = _trajectoryResolution;
            _trajectoryLine.enabled = false;
        }
    }
    
    /// <summary>
    /// 궤적 라인 업데이트 - 카메라 Raycast 기반 조준점 사용
    /// </summary>
    private void UpdateTrajectoryLine()
    {
        if (_trajectoryLine == null || _grenadeData == null) return;
        
        Vector3[] points = new Vector3[_trajectoryResolution];
        Vector3 startPos = _throwPoint.position;
        
        // 조준점 기반 투척 방향 계산
        Vector3 throwDirection = GetThrowDirection();
        Vector3 startVelocity = throwDirection * _grenadeData.throwForce;
        startVelocity.y += _grenadeData.upwardForce;
        
        float timeStep = 0.1f;
        
        for (int i = 0; i < _trajectoryResolution; i++)
        {
            float t = i * timeStep;
            points[i] = startPos + startVelocity * t + 0.5f * Physics.gravity * t * t;
            
            if (i > 0)
            {
                if (Physics.Linecast(points[i - 1], points[i], out RaycastHit hit))
                {
                    points[i] = hit.point;
                    for (int j = i + 1; j < _trajectoryResolution; j++)
                    {
                        points[j] = hit.point;
                    }
                    break;
                }
            }
        }
        
        _trajectoryLine.positionCount = _trajectoryResolution;
        _trajectoryLine.SetPositions(points);
    }
    
    private void PlayThrowSound()
    {
        if (_grenadeData?.throwSound != null)
        {
            AudioSource.PlayClipAtPoint(_grenadeData.throwSound, transform.position, 0.8f);
        }
    }
    
    public void AddGrenades(int amount)
    {
        _currentGrenades = Mathf.Min(_currentGrenades + amount, _maxGrenades);
        OnGrenadeCountChanged?.Invoke(_currentGrenades, _maxGrenades);
    }
    
    public void SetGrenadeCount(int count)
    {
        _currentGrenades = Mathf.Clamp(count, 0, _maxGrenades);
        OnGrenadeCountChanged?.Invoke(_currentGrenades, _maxGrenades);
    }
    
    public void SetGrenadeData(GrenadeData data)
    {
        _grenadeData = data;
    }
    
    #region Input System
    
public void OnGrenadeStarted(InputAction.CallbackContext context)
    {
        if (!GameStateManager.Instance.IsPlaying) return;
        
        if (context.started)
        {
            PrepareGrenade();
        }
    }
    
public void OnGrenadePerformed(InputAction.CallbackContext context)
    {
        if (!GameStateManager.Instance.IsPlaying) return;
        
        if (context.canceled)
        {
            if (_grenadeReady || _isCooking)
            {
                ThrowGrenade();
            }
        }
    }
    
    #endregion

    #region Impact Marker
    
    private void InitializeImpactMarker()
    {
        if (!_showImpactMarker || _impactMarkerPrefab == null) return;
        
        _impactMarker = Instantiate(_impactMarkerPrefab);
        _impactMarker.gameObject.SetActive(false);
        
        if (_grenadeData != null)
        {
            _impactMarker.SetSize(_grenadeData.explosionRadius);
        }
    }
    
    private void UpdateImpactMarker()
    {
        if (!_showImpactMarker || _impactMarker == null) return;
        
        bool shouldShow = _grenadeReady && (_isCooking || _currentGrenades > 0);
        
        if (shouldShow && _trajectoryLine != null && _trajectoryLine.enabled)
        {
            CalculateImpactPoint();
            
            if (_predictedImpactPoint != Vector3.zero)
            {
                bool isValidSurface = _impactMarker.UpdatePosition(_predictedImpactPoint, _predictedImpactNormal);
                
                if (isValidSurface)
                {
                    _impactMarker.SetCookingProgress(CookProgress);
                    _impactMarker.Show();
                }
                else
                {
                    _impactMarker.Hide();
                }
            }
            else
            {
                _impactMarker.Hide();
            }
        }
        else
        {
            _impactMarker.Hide();
        }
    }
    
    /// <summary>
    /// 착탄 지점 계산 - 카메라 Raycast 기반 조준점 사용
    /// </summary>
    private void CalculateImpactPoint()
    {
        if (_throwPoint == null || _playerCamera == null || _grenadeData == null) return;
        
        Vector3 startPosition = _throwPoint.position;
        
        // 조준점 기반 투척 방향 계산
        Vector3 throwDirection = GetThrowDirection();
        Vector3 startVelocity = throwDirection * _grenadeData.throwForce;
        startVelocity.y += _grenadeData.upwardForce;
        
        Vector3 previousPoint = startPosition;
        float timeStep = 0.05f;
        float maxTime = 5f;
        
        for (float t = timeStep; t < maxTime; t += timeStep)
        {
            Vector3 currentPoint = startPosition + startVelocity * t + 0.5f * Physics.gravity * t * t;
            Vector3 direction = currentPoint - previousPoint;
            float distance = direction.magnitude;
            
            if (Physics.Raycast(previousPoint, direction.normalized, out RaycastHit hit, distance))
            {
                _predictedImpactPoint = hit.point;
                _predictedImpactNormal = hit.normal;
                return;
            }
            
            previousPoint = currentPoint;
        }
        
        _predictedImpactPoint = Vector3.zero;
        _predictedImpactNormal = Vector3.up;
    }
    
    private void HideImpactMarker()
    {
        if (_impactMarker != null)
        {
            _impactMarker.Hide();
        }
    }

    #endregion

    #region Debug Gizmos

    [Header("디버그")]
    [SerializeField] private bool _showDebugGizmos = true;

    private void OnDrawGizmos()
    {
        if (!_showDebugGizmos) return;
        if (_throwPoint == null) return;

        Camera activeCamera = _playerCamera != null ? _playerCamera : Camera.main;
        if (activeCamera == null) return;

        // 카메라 위치
        Vector3 cameraPos = activeCamera.transform.position;
        Vector3 cameraForward = activeCamera.transform.forward;

        // 화면 중앙 Ray
        Ray aimRay = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // 조준점 계산
        Vector3 aimPoint;
        bool hasHit = Physics.Raycast(aimRay, out RaycastHit hit, _maxAimDistance, _aimLayerMask);

        if (hasHit)
        {
            aimPoint = hit.point;
        }
        else
        {
            aimPoint = cameraPos + cameraForward * _fallbackAimDistance;
        }

        // 던지기 방향
        Vector3 throwDirection = (aimPoint - _throwPoint.position).normalized;

        // 1. 카메라 -> 조준점 Ray (파란색)
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(cameraPos, aimPoint);

        // 2. 조준점 표시 (hit: 녹색, no hit: 노란색)
        Gizmos.color = hasHit ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(aimPoint, 0.3f);

        // 3. throwPoint 위치 (마젠타)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(_throwPoint.position, 0.15f);

        // 4. throwPoint -> 조준점 방향 (빨간색, 던지기 방향)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(_throwPoint.position, throwDirection * 5f);

        // 5. 카메라 forward 방향 (흰색)
        Gizmos.color = Color.white;
        Gizmos.DrawRay(cameraPos, cameraForward * 3f);

        // 6. throwPoint forward 방향 (주황색, 비교용)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawRay(_throwPoint.position, _throwPoint.forward * 3f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!_showDebugGizmos) return;
        if (_throwPoint == null) return;

        Camera activeCamera = _playerCamera != null ? _playerCamera : Camera.main;
        if (activeCamera == null) return;

        // 선택 시 추가 정보 표시
        Vector3 cameraPos = activeCamera.transform.position;
        Ray aimRay = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 aimPoint;
        if (Physics.Raycast(aimRay, out RaycastHit hit, _maxAimDistance, _aimLayerMask))
        {
            aimPoint = hit.point;

            // hit normal 표시 (초록색)
            Gizmos.color = Color.green;
            Gizmos.DrawRay(hit.point, hit.normal * 1f);
        }
        else
        {
            aimPoint = cameraPos + activeCamera.transform.forward * _fallbackAimDistance;
        }

        // 카메라 -> throwPoint 연결선 (회색 점선 효과)
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(cameraPos, _throwPoint.position);

        // throwPoint -> aimPoint 연결선 (실제 던지기 경로)
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawLine(_throwPoint.position, aimPoint);
    }

    #endregion
}
