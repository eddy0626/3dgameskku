using UnityEngine;
using DG.Tweening;

/// <summary>
/// 수류탄 착탄 지점 원형 마커 표시 컴포넌트
/// DOTween 애니메이션으로 펄스 효과 적용
/// </summary>
public class GrenadeImpactMarker : MonoBehaviour
{
    [Header("마커 설정")]
    [SerializeField] private float _baseSize = 5f;
    [SerializeField] private float _heightOffset = 0.05f;
    [SerializeField] private Color _markerColor = new Color(1f, 0.3f, 0f, 0.6f);
    [SerializeField] private Color _dangerColor = new Color(1f, 0f, 0f, 0.8f);
    
    [Header("수평면 제한")]
    [SerializeField, Range(0f, 90f), Tooltip("마커가 표시될 최대 표면 각도 (0=완전 수평만, 45=경사면 허용)")] 
    private float _maxSurfaceAngle = 45f;
    
    [Header("애니메이션 설정")]
    [SerializeField] private float _pulseScale = 1.15f;
    [SerializeField] private float _pulseDuration = 0.5f;
    [SerializeField] private float _rotationSpeed = 30f;
    [SerializeField] private bool _enablePulse = true;
    [SerializeField] private bool _enableRotation = true;
    
    [Header("내부 링")]
    [SerializeField] private bool _showInnerRing = true;
    [SerializeField] private float _innerRingScale = 0.6f;
    
    // 컴포넌트
    private Transform _outerRing;
    private Transform _innerRing;
    private MeshRenderer _outerRenderer;
    private MeshRenderer _innerRenderer;
    private Material _outerMaterial;
    private Material _innerMaterial;
    
    // 상태
    private bool _isVisible;
    private Vector3 _targetPosition;
    private Sequence _pulseSequence;
    private Tween _fadeInTween;
    private Tween _fadeOutTween;
    
    // 프로퍼티
    public bool IsVisible => _isVisible;
    public Vector3 TargetPosition => _targetPosition;
    
    private void Awake()
    {
        CreateMarkerVisuals();
        HideImmediate();
    }
    
private void Update()
    {
        // InnerRing만 회전 (OuterRing은 펄스 애니메이션만 적용)
        if (_isVisible && _enableRotation && _innerRing != null)
        {
            _innerRing.Rotate(Vector3.forward, _rotationSpeed * Time.deltaTime);
        }
    }
    
    private void OnDestroy()
    {
        _pulseSequence?.Kill();
        _fadeInTween?.Kill();
        _fadeOutTween?.Kill();
        
        if (_outerMaterial != null) Destroy(_outerMaterial);
        if (_innerMaterial != null) Destroy(_innerMaterial);
    }
    
    private void CreateMarkerVisuals()
    {
        _outerRing = CreateRingQuad("OuterRing", _baseSize, _markerColor);
        _outerRenderer = _outerRing.GetComponent<MeshRenderer>();
        _outerMaterial = _outerRenderer.material;
        
        if (_showInnerRing)
        {
            _innerRing = CreateRingQuad("InnerRing", _baseSize * _innerRingScale, _dangerColor);
            _innerRenderer = _innerRing.GetComponent<MeshRenderer>();
            _innerMaterial = _innerRenderer.material;
        }
    }
    
    private Transform CreateRingQuad(string name, float size, Color color)
    {
        GameObject ringObj = new GameObject(name);
        ringObj.transform.SetParent(transform);
        ringObj.transform.localPosition = Vector3.zero;
        ringObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ringObj.transform.localScale = Vector3.one * size;
        
        MeshFilter meshFilter = ringObj.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateRingMesh(32, 0.4f, 0.5f);
        
        MeshRenderer renderer = ringObj.AddComponent<MeshRenderer>();
        
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.color = color;
        
        renderer.material = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        
        return ringObj.transform;
    }
    
    private Mesh CreateRingMesh(int segments, float innerRadius, float outerRadius)
    {
        Mesh mesh = new Mesh();
        mesh.name = "RingMesh";
        
        int vertexCount = segments * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        int[] triangles = new int[segments * 6];
        
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            
            vertices[i * 2] = new Vector3(cos * innerRadius, sin * innerRadius, 0f);
            uv[i * 2] = new Vector2((float)i / segments, 0f);
            
            vertices[i * 2 + 1] = new Vector3(cos * outerRadius, sin * outerRadius, 0f);
            uv[i * 2 + 1] = new Vector2((float)i / segments, 1f);
        }
        
        for (int i = 0; i < segments; i++)
        {
            int baseIndex = i * 6;
            int currentInner = i * 2;
            int currentOuter = i * 2 + 1;
            int nextInner = ((i + 1) % segments) * 2;
            int nextOuter = ((i + 1) % segments) * 2 + 1;
            
            triangles[baseIndex] = currentInner;
            triangles[baseIndex + 1] = nextInner;
            triangles[baseIndex + 2] = currentOuter;
            
            triangles[baseIndex + 3] = currentOuter;
            triangles[baseIndex + 4] = nextInner;
            triangles[baseIndex + 5] = nextOuter;
        }
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
/// <summary>
    /// 마커 위치 업데이트 (수평면 체크 포함)
    /// </summary>
    /// <param name="position">충돌 위치</param>
    /// <param name="normal">충돌 마커 법선</param>
    /// <returns>유효한 수평면인지 여부</returns>
    public bool UpdatePosition(Vector3 position, Vector3 normal)
    {
        // 수평면 체크: 법선과 Vector3.up 사이 각도가 45도 이내여야 함
        float angle = Vector3.Angle(normal, Vector3.up);
        bool isValidSurface = angle <= _maxSurfaceAngle;
        
        if (!isValidSurface)
        {
            return false;
        }
        
        _targetPosition = position + normal * _heightOffset;
        transform.position = _targetPosition;
        
        // 마커를 다음을 항해 회전: 바닥에 누워지도록 X축 90도 회전
        // 법선 방향을 기준으로 회전 (바닥이 아닌 경사면에도 대응)
        if (normal != Vector3.zero)
        {
            // 마커의 Y축(위)이 법선 방향을 향하도록
            transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
        }
        
        return true;
    }
    
    public void SetSize(float explosionRadius)
    {
        float size = explosionRadius * 2f;
        if (_outerRing != null)
        {
            _outerRing.localScale = Vector3.one * size;
        }
        
        if (_innerRing != null)
        {
            _innerRing.localScale = Vector3.one * size * _innerRingScale;
        }
    }
    
    public void Show()
    {
        if (_isVisible) return;
        
        _isVisible = true;
        gameObject.SetActive(true);
        
        _fadeOutTween?.Kill();
        _pulseSequence?.Kill();
        
        SetAlpha(0f);
        _fadeInTween = DOTween.To(() => GetAlpha(), x => SetAlpha(x), 1f, 0.2f)
            .SetEase(Ease.OutQuad);
        
        if (_enablePulse)
        {
            StartPulseAnimation();
        }
    }
    
    public void Hide()
    {
        if (!_isVisible && !gameObject.activeSelf) return;
        
        _isVisible = false;
        
        _fadeInTween?.Kill();
        _pulseSequence?.Kill();
        
        _fadeOutTween = DOTween.To(() => GetAlpha(), x => SetAlpha(x), 0f, 0.15f)
            .SetEase(Ease.InQuad)
            .OnComplete(() => gameObject.SetActive(false));
    }
    
    public void HideImmediate()
    {
        _isVisible = false;
        _pulseSequence?.Kill();
        _fadeInTween?.Kill();
        _fadeOutTween?.Kill();
        gameObject.SetActive(false);
    }
    
    private void StartPulseAnimation()
    {
        _pulseSequence?.Kill();
        
        if (_outerRing == null) return;
        
        Vector3 baseScale = _outerRing.localScale;
        Vector3 pulseScale = baseScale * _pulseScale;
        
        _pulseSequence = DOTween.Sequence();
        _pulseSequence.Append(_outerRing.DOScale(pulseScale, _pulseDuration * 0.5f).SetEase(Ease.OutQuad));
        _pulseSequence.Append(_outerRing.DOScale(baseScale, _pulseDuration * 0.5f).SetEase(Ease.InQuad));
        _pulseSequence.SetLoops(-1);
    }
    
    private void SetAlpha(float alpha)
    {
        if (_outerMaterial != null)
        {
            Color c = _outerMaterial.color;
            c.a = _markerColor.a * alpha;
            _outerMaterial.color = c;
        }
        
        if (_innerMaterial != null)
        {
            Color c = _innerMaterial.color;
            c.a = _dangerColor.a * alpha;
            _innerMaterial.color = c;
        }
    }
    
    private float GetAlpha()
    {
        if (_outerMaterial != null && _markerColor.a > 0)
        {
            return _outerMaterial.color.a / _markerColor.a;
        }
        return 0f;
    }
    
    public void SetCookingProgress(float progress)
    {
        if (_outerMaterial != null)
        {
            Color lerpedColor = Color.Lerp(_markerColor, _dangerColor, progress);
            lerpedColor.a = _outerMaterial.color.a;
            _outerMaterial.color = lerpedColor;
        }
        
        if (_enablePulse && progress > 0.5f && _pulseSequence != null)
        {
            float speedMultiplier = 1f + (progress - 0.5f) * 2f;
            _pulseSequence.timeScale = speedMultiplier;
        }
    }
}
