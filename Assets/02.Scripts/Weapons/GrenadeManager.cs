using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using System;

/// <summary>
/// 플레이어의 수류탄 관리 컴포넌트
/// 투척, 쿠킹, 인벤토리 관리, ObjectPool 적용
/// </summary>
public class GrenadeManager : MonoBehaviour
{
    [Header("수류탄 설정")]
    [SerializeField] private GrenadeData _grenadeData;
    
    [Header("투척 설정")]
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private Camera _playerCamera;
    
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
    public event Action<int, int> OnGrenadeCountChanged; // (현재, 최대)
    public event Action OnGrenadeThrownEvent;
    public event Action<float> OnCookingProgress; // 쿠킹 진행률 (0~1)
    
    // 프로퍼티
    public int CurrentGrenades => _currentGrenades;
    public int MaxGrenades => _maxGrenades;
    public bool CanThrow => _currentGrenades > 0 && Time.time >= _nextThrowTime && !_isCooking;
    public bool IsCooking => _isCooking;
    public float CookProgress => _isCooking ? Mathf.Clamp01((Time.time - _cookStartTime) / _grenadeData.fuseTime) : 0f;
    
    // 정적 인스턴스 (UI 접근용)
    public static GrenadeManager Instance { get; private set; }
    
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        
        // 카메라 자동 찾기
        if (_playerCamera == null)
        {
            _playerCamera = Camera.main;
        }
        
        // 투척 포인트 설정
        if (_throwPoint == null)
        {
            _throwPoint = _playerCamera?.transform;
        }
        
        // 시작 수류탄 개수 설정
        _currentGrenades = _startGrenades;
        
        // 오브젝트 풀 초기화
        InitializePools();
        
        // 궤적 라인 설정
        SetupTrajectoryLine();
        // 착탄 마커 초기화
        InitializeImpactMarker();

    }
    
    private void Start()
    {
        // 초기 수류탄 수 이벤트 발생
        OnGrenadeCountChanged?.Invoke(_currentGrenades, _maxGrenades);
    }
    
    private void Update()
    {
        // 레거시 입력 처리
        HandleLegacyInput();
        
        // 궤적 표시 업데이트
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
        // 착탄 마커 업데이트
        UpdateImpactMarker();

        }
        
        // 쿠킹 진행률 이벤트 발생
        if (_isCooking)
        {
            OnCookingProgress?.Invoke(CookProgress);
            
            // 최대 쿠킹 시간 초과 시 자동 투척
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
        // 풀 정리
        _grenadePool?.Dispose();
        _explosionPool?.Dispose();
        
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #region Object Pooling
    
    /// <summary>
    /// 오브젝트 풀 초기화
    /// </summary>
    private void InitializePools()
    {
        // 수류탄 풀
        _grenadePool = new ObjectPool<Grenade>(
            createFunc: CreateGrenade,
            actionOnGet: OnGrenadeGet,
            actionOnRelease: OnGrenadeRelease,
            actionOnDestroy: OnGrenadeDestroy,
            collectionCheck: true,
            defaultCapacity: _poolDefaultCapacity,
            maxSize: _poolMaxSize
        );
        
        // 폭발 이펙트 풀
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
    
    // 수류탄 풀 콜백
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
    
    // 폭발 이펙트 풀 콜백
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
    
    /// <summary>
    /// 폭발 이펙트 풀에서 가져오기 (Grenade에서 호출)
    /// </summary>
    public GameObject GetExplosionFromPool()
    {
        return _explosionPool?.Get();
    }
    
    /// <summary>
    /// 폭발 이펙트 풀로 반환 (Grenade에서 호출)
    /// </summary>
    public void ReturnExplosionToPool(GameObject explosion)
    {
        _explosionPool?.Release(explosion);
    }
    
    #endregion

    /// <summary>
    /// Update - 레거시 입력 처리
    /// </summary>
    private void HandleLegacyInput()
    {
        // G키로 수류탄 투척 (누르면 쿠킹 시작, 놓으면 투척)
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

    
    /// <summary>
    /// 수류탄 준비 (G키 누름)
    /// </summary>
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
    
    /// <summary>
    /// 수류탄 투척 (G키 놓음)
    /// </summary>
    public void ThrowGrenade()
    {
        if (_currentGrenades <= 0) return;
        if (!_grenadeReady && !_isCooking) return;
        
        // 쿠킹 시간 계산
        float cookTime = 0f;
        if (_isCooking && _enableCooking)
        {
            cookTime = Time.time - _cookStartTime;
        }
        
        // 수류탄 생성 (풀에서 가져오기)
        SpawnGrenade(cookTime);
        
        // 투척 사운드 재생
        PlayThrowSound();
        
        // 상태 초기화
        _currentGrenades--;
        _nextThrowTime = Time.time + _grenadeData.throwCooldown;
        _isCooking = false;
        _grenadeReady = false;
        
        // 이벤트 발생
        OnGrenadeCountChanged?.Invoke(_currentGrenades, _maxGrenades);
        OnGrenadeThrownEvent?.Invoke();
        OnCookingProgress?.Invoke(0f);
    }
    
    /// <summary>
    /// 수류탄 투척 취소
    /// </summary>
    public void CancelThrow()
    {
        _isCooking = false;
        _grenadeReady = false;
        OnCookingProgress?.Invoke(0f);
    }
    
    /// <summary>
    /// 수류탄 생성 및 발사 (ObjectPool 사용)
    /// </summary>
    private void SpawnGrenade(float cookTime)
    {
        if (_grenadeData?.grenadePrefab == null)
        {
            Debug.LogWarning("GrenadeManager: 수류탄 프리팹이 설정되지 않았습니다.");
            return;
        }
        
        // 투척 방향 계산 (카메라 전방)
        Vector3 throwDirection = _playerCamera.transform.forward;
        
        // 풀에서 수류탄 가져오기
        Grenade grenade = _grenadePool.Get();
        
        if (grenade != null)
        {
            // 위치 및 회전 설정
            grenade.transform.position = _throwPoint.position;
            grenade.transform.rotation = Quaternion.LookRotation(throwDirection);
            
            // 초기화
            grenade.Initialize(_grenadeData, throwDirection, gameObject, cookTime);
        }
        else
        {
            Debug.LogWarning("GrenadeManager: 풀에서 수류탄을 가져올 수 없습니다.");
        }
    }
    
    /// <summary>
    /// 궤적 라인 설정
    /// </summary>
    private void SetupTrajectoryLine()
    {
        if (_trajectoryLine == null && _showTrajectory)
        {
            GameObject lineObj = new GameObject("GrenadeTrajectory");
            lineObj.transform.SetParent(transform);
            _trajectoryLine = lineObj.AddComponent<LineRenderer>();
            
            // 라인 스타일 설정
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
    /// 궤적 라인 업데이트
    /// </summary>
    private void UpdateTrajectoryLine()
    {
        if (_trajectoryLine == null || _grenadeData == null) return;
        
        Vector3[] points = new Vector3[_trajectoryResolution];
        Vector3 startPos = _throwPoint.position;
        Vector3 startVelocity = _playerCamera.transform.forward * _grenadeData.throwForce;
        startVelocity.y += _grenadeData.upwardForce;
        
        float timeStep = 0.1f;
        
        for (int i = 0; i < _trajectoryResolution; i++)
        {
            float t = i * timeStep;
            
            // 물리 공식: position = startPos + velocity * t + 0.5 * gravity * t^2
            points[i] = startPos + startVelocity * t + 0.5f * Physics.gravity * t * t;
            
            // 충돌 체크 (선택적)
            if (i > 0)
            {
                if (Physics.Linecast(points[i - 1], points[i], out RaycastHit hit))
                {
                    points[i] = hit.point;
                    // 나머지 포인트는 충돌 지점으로 설정
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
    
    /// <summary>
    /// 투척 사운드 재생
    /// </summary>
    private void PlayThrowSound()
    {
        if (_grenadeData?.throwSound != null)
        {
            AudioSource.PlayClipAtPoint(_grenadeData.throwSound, transform.position, 0.8f);
        }
    }
    
    /// <summary>
    /// 수류탄 추가 (픽업)
    /// </summary>
    public void AddGrenades(int amount)
    {
        _currentGrenades = Mathf.Min(_currentGrenades + amount, _maxGrenades);
        OnGrenadeCountChanged?.Invoke(_currentGrenades, _maxGrenades);
    }
    
    /// <summary>
    /// 수류탄 설정
    /// </summary>
    public void SetGrenadeCount(int count)
    {
        _currentGrenades = Mathf.Clamp(count, 0, _maxGrenades);
        OnGrenadeCountChanged?.Invoke(_currentGrenades, _maxGrenades);
    }
    
    /// <summary>
    /// 수류탄 데이터 변경
    /// </summary>
    public void SetGrenadeData(GrenadeData data)
    {
        _grenadeData = data;
    }
    
    #region Input System 연동
    
    /// <summary>
    /// Input System 수류탄 버튼 시작 (버튼 누름)
    /// </summary>
    public void OnGrenadeStarted(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            PrepareGrenade();
        }
    }
    
    /// <summary>
    /// Input System 수류탄 버튼 종료 (버튼 놓음)
    /// </summary>
    public void OnGrenadePerformed(InputAction.CallbackContext context)
    {
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
    
    /// <summary>
    /// 착탄 마커 초기화
    /// </summary>
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
    
    /// <summary>
    /// 착탄 마커 업데이트
    /// </summary>
/// <summary>
    /// 착탄 마커 업데이트 (수평면에만 표시)
    /// </summary>
    private void UpdateImpactMarker()
    {
        if (!_showImpactMarker || _impactMarker == null) return;
        
        bool shouldShow = _grenadeReady && (_isCooking || _currentGrenades > 0);
        
        if (shouldShow && _trajectoryLine != null && _trajectoryLine.enabled)
        {
            CalculateImpactPoint();
            
            if (_predictedImpactPoint != Vector3.zero)
            {
                // UpdatePosition이 false를 반환하면 수평면이 아님 -> 마커 숨김
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
    /// 착탄 지점 계산
    /// </summary>
    private void CalculateImpactPoint()
    {
        if (_throwPoint == null || _playerCamera == null || _grenadeData == null) return;
        
        Vector3 startPosition = _throwPoint.position;
        Vector3 startVelocity = _playerCamera.transform.forward * _grenadeData.throwForce;
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
    
    /// <summary>
    /// 마커 숨기기
    /// </summary>
    private void HideImpactMarker()
    {
        if (_impactMarker != null)
        {
            _impactMarker.Hide();
        }
    }
    
    #endregion
}
