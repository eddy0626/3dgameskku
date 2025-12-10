using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using System;
using DG.Tweening;

/// <summary>
/// 플레이어의 수류탄 관리 컴포넌트
/// 투척, 쿠킹, 인벤토리 관리, ObjectPool 적용
/// 착탄 마커 및 DOTween 궤도 애니메이션 지원
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
    
    [Header("궤적 표시")]
    [SerializeField] private bool _showTrajectory = true;
    [SerializeField] private LineRenderer _trajectoryLine;
    [SerializeField] private int _trajectoryResolution = 30;
    
    [Header("궤적 DOTween 설정")]
    [SerializeField] private float _trajectoryFlowSpeed = 3f;
    [SerializeField] private Color _trajectoryStartColor = new Color(1f, 0.8f, 0f, 0.9f);
    [SerializeField] private Color _trajectoryEndColor = new Color(1f, 0.3f, 0f, 0.4f);
    [SerializeField] private float _trajectoryStartWidth = 0.1f;
    [SerializeField] private float _trajectoryEndWidth = 0.03f;
    [SerializeField] private float _trajectoryDashLength = 0.3f;
    
    [Header("착탄 마커 설정")]
    [SerializeField] private bool _showImpactMarker = true;
    [SerializeField] private GameObject _impactMarkerPrefab;
    [SerializeField] private bool _forceDefaultMarker = true; // 프리팹 무시하고 기본 마커 강제 생성
    [SerializeField] private float _markerSize = 2f;
    [SerializeField] private Color _markerColor = new Color(1f, 0.3f, 0f, 0.7f);
    [SerializeField] private float _maxSurfaceAngle = 45f; // 바닥으로 인식할 최대 각도
    
    [Header("착탄 마커 DOTween 설정")]
    [SerializeField] private float _markerPulseScale = 1.2f;
    [SerializeField] private float _markerPulseDuration = 0.5f;
    [SerializeField] private float _innerRotationSpeed = 60f; // 내부 원 회전 속도
    
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
    
    // 착탄 마커
    private GameObject _impactMarkerInstance;
    private Transform _impactMarkerTransform;
    private Transform _innerCircleTransform; // 내부 원 별도 참조 (회전용)
    private Transform _outerRingTransform;   // 외곽 링 별도 참조 (고정)
    private SpriteRenderer _impactMarkerRenderer;
    private MeshRenderer _impactMarkerMeshRenderer;
    private Vector3 _currentImpactPoint;
    private Vector3 _currentImpactNormal;
    private bool _hasValidImpact;
    
    // 궤적 머티리얼
    private Material _trajectoryMaterial;
    private float _trajectoryUVOffset;
    
    // DOTween 시퀀스
    private Sequence _trajectorySequence;
    private Sequence _markerPulseSequence;
    private Tween _innerRotationTween;
    
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
        
        // 착탄 마커 설정
        SetupImpactMarker();
    }
    
    private void Start()
    {
        // 초기 수류탄 수 이벤트 발생
        OnGrenadeCountChanged?.Invoke(_currentGrenades, _maxGrenades);
        
        // DOTween 궤적 애니메이션 시작
        StartTrajectoryAnimation();
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
                ShowImpactMarker(true);
            }
            else
            {
                _trajectoryLine.enabled = false;
                ShowImpactMarker(false);
            }
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
        
        // 착탄 마커 업데이트
        UpdateImpactMarkerPosition();
        
        // 궤적 UV 오프셋 업데이트 (점선 흐름 효과)
        UpdateTrajectoryUVOffset();
    }
    
    private void OnDestroy()
    {
        // DOTween 정리
        _trajectorySequence?.Kill();
        _markerPulseSequence?.Kill();
        _innerRotationTween?.Kill();
        
        // 풀 정리
        _grenadePool?.Dispose();
        _explosionPool?.Dispose();
        
        // 착탄 마커 정리
        if (_impactMarkerInstance != null)
        {
            Destroy(_impactMarkerInstance);
        }
        
        // 머티리얼 정리
        if (_trajectoryMaterial != null)
        {
            Destroy(_trajectoryMaterial);
        }
        
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

    #region Input Handling
    
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
    
    #endregion

    #region Grenade Throwing
    
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
    
    #endregion

    #region Trajectory Line
    
    /// <summary>
    /// 궤적 라인 설정
    /// </summary>
    private void SetupTrajectoryLine()
    {
        // 궤적 라인이 없거나 강제 재생성일 경우 새로 생성
        if (_trajectoryLine == null && _showTrajectory)
        {
            GameObject lineObj = new GameObject("GrenadeTrajectory");
            lineObj.transform.SetParent(transform);
            _trajectoryLine = lineObj.AddComponent<LineRenderer>();
        }
        
        if (_trajectoryLine != null)
        {
            // 점선 텍스처 생성
            Texture2D dashTexture = CreateDashTexture();
            
            // 점선 머티리얼 설정
            _trajectoryMaterial = new Material(Shader.Find("Sprites/Default"));
            _trajectoryMaterial.mainTexture = dashTexture;
            _trajectoryMaterial.color = Color.white;
            
            // 라인 스타일 설정
            _trajectoryLine.startWidth = _trajectoryStartWidth;
            _trajectoryLine.endWidth = _trajectoryEndWidth;
            _trajectoryLine.material = _trajectoryMaterial;
            _trajectoryLine.startColor = _trajectoryStartColor;
            _trajectoryLine.endColor = _trajectoryEndColor;
            _trajectoryLine.positionCount = _trajectoryResolution;
            _trajectoryLine.enabled = false;
            
            // 텍스처 모드 설정 (타일링 - 점선 효과용)
            _trajectoryLine.textureMode = LineTextureMode.Tile;
            _trajectoryLine.numCapVertices = 4;
            _trajectoryLine.numCornerVertices = 4;
        }
    }
    
    /// <summary>
    /// 점선 텍스처 생성
    /// </summary>
    private Texture2D CreateDashTexture()
    {
        int width = 32;
        int height = 4;
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[width * height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 점선 패턴: 앞 절반은 불투명, 뒤 절반은 반투명
                float alpha = (x < width / 2) ? 1f : 0.3f;
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return texture;
    }
    
    /// <summary>
    /// 궤적 UV 오프셋 업데이트 (점선 흐름 효과)
    /// </summary>
    private void UpdateTrajectoryUVOffset()
    {
        if (_trajectoryMaterial == null || !_trajectoryLine.enabled) return;
        
        // UV 오프셋을 시간에 따라 증가시켜 점선이 흐르는 효과
        _trajectoryUVOffset += Time.deltaTime * _trajectoryFlowSpeed;
        if (_trajectoryUVOffset > 1f)
        {
            _trajectoryUVOffset -= 1f;
        }
        
        _trajectoryMaterial.mainTextureOffset = new Vector2(-_trajectoryUVOffset, 0f);
    }
    
    /// <summary>
    /// DOTween 궤적 애니메이션 시작 (펄스 효과)
    /// </summary>
    private void StartTrajectoryAnimation()
    {
        if (_trajectoryLine == null) return;
        
        // 기존 시퀀스 정리
        _trajectorySequence?.Kill();
        
        // 새 시퀀스 생성
        _trajectorySequence = DOTween.Sequence();
        
        // 너비 펄스 애니메이션
        _trajectorySequence.Append(
            DOTween.To(
                () => _trajectoryLine.startWidth,
                x => _trajectoryLine.startWidth = x,
                _trajectoryStartWidth * 1.4f,
                0.4f
            ).SetEase(Ease.InOutSine)
        );
        
        _trajectorySequence.Append(
            DOTween.To(
                () => _trajectoryLine.startWidth,
                x => _trajectoryLine.startWidth = x,
                _trajectoryStartWidth,
                0.4f
            ).SetEase(Ease.InOutSine)
        );
        
        // 무한 반복
        _trajectorySequence.SetLoops(-1, LoopType.Restart);
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
        _hasValidImpact = false;
        
        for (int i = 0; i < _trajectoryResolution; i++)
        {
            float t = i * timeStep;
            
            // 물리 공식: position = startPos + velocity * t + 0.5 * gravity * t^2
            points[i] = startPos + startVelocity * t + 0.5f * Physics.gravity * t * t;
            
            // 충돌 체크
            if (i > 0)
            {
                if (Physics.Linecast(points[i - 1], points[i], out RaycastHit hit))
                {
                    points[i] = hit.point;
                    
                    // 착탄 지점 저장 (바닥 체크)
                    CheckAndSetImpactPoint(hit);
                    
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
    
    #endregion

    #region Impact Marker
    
    /// <summary>
    /// 착탄 마커 설정
    /// </summary>
    private void SetupImpactMarker()
    {
        if (!_showImpactMarker) return;
        
        // 강제 기본 마커 사용 또는 프리팹이 없으면 기본 원형 마커 생성
        if (_forceDefaultMarker || _impactMarkerPrefab == null)
        {
            // 기본 원형 마커 생성 (외곽 고정, 내부 회전 구조)
            _impactMarkerInstance = CreateDefaultMarker();
            Debug.Log("GrenadeManager: 새 착탄 마커 구조 생성 (OuterRing + InnerCircle)");
        }
        else
        {
            _impactMarkerInstance = Instantiate(_impactMarkerPrefab);
            
            // 기존 프리팹에서 참조 찾기 시도
            _outerRingTransform = _impactMarkerInstance.transform.Find("OuterRing");
            Transform innerContainer = _impactMarkerInstance.transform.Find("InnerContainer");
            if (innerContainer != null)
            {
                _innerCircleTransform = innerContainer;
            }
        }
        
        _impactMarkerTransform = _impactMarkerInstance.transform;
        _impactMarkerInstance.SetActive(false);
        
        // 렌더러 참조 가져오기
        _impactMarkerRenderer = _impactMarkerInstance.GetComponentInChildren<SpriteRenderer>();
        _impactMarkerMeshRenderer = _impactMarkerInstance.GetComponentInChildren<MeshRenderer>();
        
        // DOTween 마커 애니메이션 시작
        StartMarkerAnimation();
    }
    
    /// <summary>
    /// 기본 원형 마커 생성 (외곽 고정, 내부 회전 구조)
    /// </summary>
    private GameObject CreateDefaultMarker()
    {
        GameObject marker = new GameObject("GrenadeImpactMarker");
        
        // 외곽 링 (고정 - 바닥에 붙어있음)
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "OuterRing";
        ring.transform.SetParent(marker.transform);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localScale = new Vector3(_markerSize, 0.01f, _markerSize);
        _outerRingTransform = ring.transform;
        
        // Collider 제거
        Destroy(ring.GetComponent<Collider>());
        
        // 외곽 링 머티리얼 (테두리만 보이도록 반투명)
        MeshRenderer meshRenderer = ring.GetComponent<MeshRenderer>();
        Material outerMaterial = new Material(Shader.Find("Sprites/Default"));
        outerMaterial.color = new Color(_markerColor.r, _markerColor.g, _markerColor.b, _markerColor.a * 0.6f);
        meshRenderer.material = outerMaterial;
        
        // 내부 회전 컨테이너 (이것만 회전)
        GameObject innerContainer = new GameObject("InnerContainer");
        innerContainer.transform.SetParent(marker.transform);
        innerContainer.transform.localPosition = Vector3.zero;
        _innerCircleTransform = innerContainer.transform;
        
        // 내부 원 (폭발 범위 표시 - 회전함)
        GameObject innerCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        innerCircle.name = "InnerCircle";
        innerCircle.transform.SetParent(innerContainer.transform);
        innerCircle.transform.localPosition = Vector3.zero;
        innerCircle.transform.localScale = new Vector3(_markerSize * 0.4f, 0.005f, _markerSize * 0.4f);
        
        // Collider 제거
        Destroy(innerCircle.GetComponent<Collider>());
        
        // 내부 원 머티리얼
        MeshRenderer innerRenderer = innerCircle.GetComponent<MeshRenderer>();
        Material innerMaterial = new Material(Shader.Find("Sprites/Default"));
        innerMaterial.color = new Color(_markerColor.r * 1.2f, _markerColor.g, _markerColor.b, _markerColor.a);
        innerRenderer.material = innerMaterial;
        
        // 십자 표시 (내부 컨테이너에 추가 - 함께 회전)
        CreateCrossLines(innerContainer.transform);
        
        return marker;
    }
    
    /// <summary>
    /// 십자 표시 생성 (회전하는 내부 컨테이너에 추가)
    /// </summary>
    private void CreateCrossLines(Transform parent)
    {
        float lineLength = _markerSize * 0.6f;
        float lineWidth = 0.02f;
        
        // 가로선
        GameObject horizontal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        horizontal.name = "CrossHorizontal";
        horizontal.transform.SetParent(parent);
        horizontal.transform.localPosition = Vector3.up * 0.01f;
        horizontal.transform.localScale = new Vector3(lineLength, 0.01f, lineWidth);
        Destroy(horizontal.GetComponent<Collider>());
        
        MeshRenderer hRenderer = horizontal.GetComponent<MeshRenderer>();
        Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.color = new Color(1f, 1f, 1f, 0.8f);
        hRenderer.material = lineMaterial;
        
        // 세로선
        GameObject vertical = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vertical.name = "CrossVertical";
        vertical.transform.SetParent(parent);
        vertical.transform.localPosition = Vector3.up * 0.01f;
        vertical.transform.localScale = new Vector3(lineWidth, 0.01f, lineLength);
        Destroy(vertical.GetComponent<Collider>());
        
        MeshRenderer vRenderer = vertical.GetComponent<MeshRenderer>();
        vRenderer.material = lineMaterial;
    }
    
    /// <summary>
    /// 충돌 지점이 바닥(수평면)인지 확인하고 착탄 지점 설정
    /// </summary>
    private void CheckAndSetImpactPoint(RaycastHit hit)
    {
        // 법선 벡터와 위쪽 벡터 사이의 각도 계산
        float angle = Vector3.Angle(hit.normal, Vector3.up);
        
        // 설정된 최대 각도 이내인 경우에만 바닥으로 인식
        if (angle <= _maxSurfaceAngle)
        {
            _currentImpactPoint = hit.point;
            _currentImpactNormal = hit.normal;
            _hasValidImpact = true;
        }
        else
        {
            // 바닥이 아닌 경우 (벽 등) - 마커 표시 안함
            _hasValidImpact = false;
        }
    }
    
    /// <summary>
    /// 착탄 마커 표시/숨김
    /// </summary>
    private void ShowImpactMarker(bool show)
    {
        if (_impactMarkerInstance == null) return;
        
        // 유효한 충돌 지점이 있고 표시 요청인 경우에만 표시
        bool shouldShow = show && _hasValidImpact && _showImpactMarker;
        
        if (_impactMarkerInstance.activeSelf != shouldShow)
        {
            _impactMarkerInstance.SetActive(shouldShow);
            
            // 표시될 때 DOTween 애니메이션 재시작
            if (shouldShow)
            {
                RestartMarkerAnimation();
            }
        }
    }
    
    /// <summary>
    /// 착탄 마커 위치 업데이트
    /// </summary>
    private void UpdateImpactMarkerPosition()
    {
        if (_impactMarkerTransform == null || !_impactMarkerInstance.activeSelf) return;
        if (!_hasValidImpact) return;
        
        // 위치 설정 (약간 위로 올려서 z-fighting 방지)
        _impactMarkerTransform.position = _currentImpactPoint + _currentImpactNormal * 0.01f;
        
        // 전체 마커 회전 설정 - 법선 벡터를 기준으로 바닥에 평평하게 놓이도록
        Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, _currentImpactNormal);
        _impactMarkerTransform.rotation = surfaceRotation;
        
        // 내부 원은 DOTween에서 로컬 Y축 회전 (Update에서 하지 않음 - DOTween이 담당)
    }
    
    /// <summary>
    /// DOTween 마커 애니메이션 시작
    /// </summary>
    private void StartMarkerAnimation()
    {
        if (_impactMarkerTransform == null) return;
        
        // 기존 애니메이션 정리
        _markerPulseSequence?.Kill();
        _innerRotationTween?.Kill();
        
        // 펄스 스케일 애니메이션 (외곽 링에 적용)
        if (_outerRingTransform != null)
        {
            _markerPulseSequence = DOTween.Sequence();
            
            Vector3 originalScale = new Vector3(_markerSize, 0.01f, _markerSize);
            Vector3 pulseScale = originalScale * _markerPulseScale;
            
            // 외곽 링 스케일 펄스
            _markerPulseSequence.Append(
                _outerRingTransform.DOScale(pulseScale, _markerPulseDuration * 0.5f)
                    .SetEase(Ease.OutQuad)
            );
            
            _markerPulseSequence.Append(
                _outerRingTransform.DOScale(originalScale, _markerPulseDuration * 0.5f)
                    .SetEase(Ease.InQuad)
            );
            
            _markerPulseSequence.SetLoops(-1, LoopType.Restart);
        }
        
        // 내부 원만 로컬 Y축 회전 (바닥에 붙어서 평평하게 돌아감)
        if (_innerCircleTransform != null)
        {
            _innerRotationTween = _innerCircleTransform
                .DOLocalRotate(new Vector3(0, 360, 0), 360f / _innerRotationSpeed, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }
    }
    
    /// <summary>
    /// 마커 애니메이션 재시작
    /// </summary>
    private void RestartMarkerAnimation()
    {
        if (_outerRingTransform != null)
        {
            // 외곽 링 스케일 리셋
            _outerRingTransform.localScale = new Vector3(_markerSize, 0.01f, _markerSize);
        }
        
        if (_innerCircleTransform != null)
        {
            // 내부 원 로컬 회전 리셋
            _innerCircleTransform.localRotation = Quaternion.identity;
        }
        
        // 애니메이션 재생
        _markerPulseSequence?.Restart();
        _innerRotationTween?.Restart();
    }
    
    #endregion

    #region Audio
    
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
    
    #endregion

    #region Inventory Management
    
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
    
    #endregion
    
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
}
