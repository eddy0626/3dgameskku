using UnityEngine;
using DG.Tweening;

/// <summary>
/// 보스 공격 범위 표시 인디케이터 (로스트아크 스타일)
/// DOTween Pro 고급 기능 활용
/// </summary>
public class AreaIndicator : MonoBehaviour
{
    [Header("비주얼 설정")]
    [SerializeField] private Color _fillColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private Color _edgeColor = new Color(1f, 0.3f, 0f, 1f);
    [SerializeField] private Color _warningColor = new Color(1f, 1f, 0f, 1f);
    [SerializeField] private float _edgeWidth = 0.4f;
    [SerializeField] private int _segments = 64;

    private float _radius = 5f;

    // 메인 원
    private GameObject _fillObject;
    private MeshFilter _fillMeshFilter;
    private MeshRenderer _fillMeshRenderer;
    private Material _fillMaterial;
    private Mesh _fillMesh;

    // 외곽 링 (회전)
    private GameObject _edgeObject;
    private LineRenderer _edgeRenderer;
    private Material _edgeMaterial;

    // 내부 수축 링
    private GameObject _innerRingObject;
    private LineRenderer _innerRingRenderer;
    private Material _innerRingMaterial;

    // 외부 펄스 링
    private GameObject _pulseRingObject;
    private LineRenderer _pulseRingRenderer;
    private Material _pulseRingMaterial;

    // 경고 마크
    private GameObject _warningMarkObject;
    private TextMesh _warningText;

    private Sequence _mainSequence;
    private Sequence _pulseSequence;
    private Sequence _rotateSequence;
    private bool _isDestroyed = false;

    /// <summary>
    /// 인스턴스 생성
    /// </summary>
    public static AreaIndicator Create(Vector3 position, float radius)
    {
        GameObject obj = new GameObject("AreaIndicator");
        obj.transform.position = position + Vector3.up * 0.05f;

        var indicator = obj.AddComponent<AreaIndicator>();
        indicator._radius = radius;
        indicator.Initialize();

        return indicator;
    }

    private void Initialize()
    {
        CreateFillCircle();
        CreateEdgeRing();
        CreateInnerRing();
        CreatePulseRing();
        CreateWarningMark();
    }

    #region 비주얼 요소 생성

    private void CreateFillCircle()
    {
        _fillObject = new GameObject("Fill");
        _fillObject.transform.SetParent(transform);
        _fillObject.transform.localPosition = Vector3.zero;
        _fillObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        _fillObject.transform.localScale = Vector3.zero;

        _fillMeshFilter = _fillObject.AddComponent<MeshFilter>();
        _fillMeshRenderer = _fillObject.AddComponent<MeshRenderer>();

        _fillMesh = CreateCircleMesh(_radius);
        _fillMeshFilter.mesh = _fillMesh;

        _fillMaterial = CreateTransparentMaterial(_fillColor);
        _fillMeshRenderer.material = _fillMaterial;
        _fillMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _fillMeshRenderer.receiveShadows = false;
    }

    private void CreateEdgeRing()
    {
        _edgeObject = new GameObject("Edge");
        _edgeObject.transform.SetParent(transform);
        _edgeObject.transform.localPosition = Vector3.up * 0.02f;
        _edgeObject.transform.localScale = Vector3.zero;

        _edgeRenderer = _edgeObject.AddComponent<LineRenderer>();
        SetupRing(_edgeRenderer, _radius, _edgeWidth);

        _edgeMaterial = CreateTransparentMaterial(_edgeColor);
        _edgeRenderer.material = _edgeMaterial;
        _edgeRenderer.startColor = _edgeColor;
        _edgeRenderer.endColor = _edgeColor;
    }

    private void CreateInnerRing()
    {
        _innerRingObject = new GameObject("InnerRing");
        _innerRingObject.transform.SetParent(transform);
        _innerRingObject.transform.localPosition = Vector3.up * 0.03f;
        _innerRingObject.transform.localScale = Vector3.one * 2f; // 바깥에서 시작

        _innerRingRenderer = _innerRingObject.AddComponent<LineRenderer>();
        SetupRing(_innerRingRenderer, _radius, 0.15f);

        Color innerColor = new Color(1f, 0.5f, 0f, 0.8f);
        _innerRingMaterial = CreateTransparentMaterial(innerColor);
        _innerRingRenderer.material = _innerRingMaterial;
        _innerRingRenderer.startColor = innerColor;
        _innerRingRenderer.endColor = innerColor;
    }

    private void CreatePulseRing()
    {
        _pulseRingObject = new GameObject("PulseRing");
        _pulseRingObject.transform.SetParent(transform);
        _pulseRingObject.transform.localPosition = Vector3.up * 0.01f;
        _pulseRingObject.transform.localScale = Vector3.one * 0.5f;

        _pulseRingRenderer = _pulseRingObject.AddComponent<LineRenderer>();
        SetupRing(_pulseRingRenderer, _radius, 0.3f);

        Color pulseColor = new Color(1f, 0f, 0f, 0f);
        _pulseRingMaterial = CreateTransparentMaterial(pulseColor);
        _pulseRingRenderer.material = _pulseRingMaterial;
        _pulseRingRenderer.startColor = pulseColor;
        _pulseRingRenderer.endColor = pulseColor;
    }

    private void CreateWarningMark()
    {
        _warningMarkObject = new GameObject("WarningMark");
        _warningMarkObject.transform.SetParent(transform);
        _warningMarkObject.transform.localPosition = Vector3.up * 0.1f;
        _warningMarkObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        _warningMarkObject.transform.localScale = Vector3.zero;

        _warningText = _warningMarkObject.AddComponent<TextMesh>();
        _warningText.text = "!";
        _warningText.fontSize = 100;
        _warningText.characterSize = 0.5f;
        _warningText.anchor = TextAnchor.MiddleCenter;
        _warningText.alignment = TextAlignment.Center;
        _warningText.color = _warningColor;
        _warningText.fontStyle = FontStyle.Bold;

        // 머티리얼
        var renderer = _warningMarkObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private void SetupRing(LineRenderer lr, float radius, float width)
    {
        lr.positionCount = _segments + 1;
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        float angleStep = 360f / _segments;
        for (int i = 0; i <= _segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, 0f, z));
        }
    }

    #endregion

    #region 머티리얼 & 메시

    private Material CreateTransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
        }
        if (mat.HasProperty("_SrcBlend"))
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite"))
            mat.SetInt("_ZWrite", 0);

        mat.color = color;
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        return mat;
    }

    private Mesh CreateCircleMesh(float radius)
    {
        Mesh mesh = new Mesh { name = "CircleMesh" };

        Vector3[] vertices = new Vector3[_segments + 1];
        Vector2[] uv = new Vector2[_segments + 1];
        Color[] colors = new Color[_segments + 1];
        int[] triangles = new int[_segments * 3];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);
        colors[0] = Color.white;

        float angleStep = 360f / _segments;
        for (int i = 0; i < _segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            uv[i + 1] = new Vector2((Mathf.Cos(angle) + 1f) * 0.5f, (Mathf.Sin(angle) + 1f) * 0.5f);
            colors[i + 1] = Color.white;
        }

        for (int i = 0; i < _segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % _segments + 1;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    #endregion

    #region 애니메이션

    /// <summary>
    /// 인디케이터 표시 (DOTween Pro Sequence 활용)
    /// </summary>
    public void Show(float duration, System.Action onComplete = null)
    {
        if (_isDestroyed) return;

        gameObject.SetActive(true);

        // 모든 시퀀스 생성
        _mainSequence = DOTween.Sequence();
        _pulseSequence = DOTween.Sequence();
        _rotateSequence = DOTween.Sequence();

        // === 1. 초기 등장 애니메이션 ===
        float appearTime = 0.3f;

        // 메인 원 - 탄성 있게 확장
        _fillObject.transform.localScale = Vector3.zero;
        _mainSequence.Append(_fillObject.transform.DOScale(Vector3.one, appearTime)
            .SetEase(Ease.OutBack, 1.5f));

        // 외곽 링 - 약간 지연 후 확장
        _edgeObject.transform.localScale = Vector3.zero;
        _mainSequence.Join(_edgeObject.transform.DOScale(Vector3.one, appearTime)
            .SetEase(Ease.OutBack, 2f)
            .SetDelay(0.05f));

        // 경고 마크 - 펀치 효과로 등장
        _warningMarkObject.transform.localScale = Vector3.zero;
        _mainSequence.Join(_warningMarkObject.transform.DOScale(Vector3.one * 3f, appearTime * 0.5f)
            .SetEase(Ease.OutBack, 3f)
            .SetDelay(0.1f));
        _mainSequence.Join(_warningMarkObject.transform.DOPunchScale(Vector3.one * 0.5f, 0.3f, 5, 0.5f)
            .SetDelay(appearTime * 0.5f + 0.1f));

        // === 2. 외곽 링 회전 ===
        _rotateSequence.Append(_edgeObject.transform.DORotate(new Vector3(0f, 360f, 0f), 2f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart));

        // === 3. 내부 수축 링 (바깥에서 안으로 반복) ===
        _innerRingObject.transform.localScale = Vector3.one * 1.5f;
        SetRingAlpha(_innerRingRenderer, _innerRingMaterial, 0f);

        float shrinkDuration = duration / 3f;
        _pulseSequence.Append(_innerRingObject.transform.DOScale(Vector3.one, shrinkDuration)
            .SetEase(Ease.Linear));
        _pulseSequence.Join(DOTween.To(() => 0f, a => SetRingAlpha(_innerRingRenderer, _innerRingMaterial, a), 0.8f, shrinkDuration * 0.3f));
        _pulseSequence.Join(DOTween.To(() => 0.8f, a => SetRingAlpha(_innerRingRenderer, _innerRingMaterial, a), 0f, shrinkDuration * 0.7f)
            .SetDelay(shrinkDuration * 0.3f));
        _pulseSequence.SetLoops(-1, LoopType.Restart);

        // === 4. 색상 강도 증가 + 깜빡임 ===
        StartIntensityAnimation(duration);

        // === 5. 외부 펄스 링 (안에서 바깥으로 퍼짐) ===
        StartOuterPulse(duration);

        // === 6. 경고 마크 깜빡임 ===
        StartWarningBlink(duration);

        // === 7. 완료 시 최종 플래시 ===
        DOVirtual.DelayedCall(duration, () =>
        {
            if (!_isDestroyed)
            {
                ExecuteFinalFlash(onComplete);
            }
        });
    }

    private void StartIntensityAnimation(float duration)
    {
        if (_isDestroyed) return;

        float elapsed = 0f;
        float pulseSpeed = 12f;

        DOTween.To(() => elapsed, x =>
        {
            if (_isDestroyed || _fillMaterial == null || _edgeMaterial == null) return;

            elapsed = x;
            float progress = elapsed / duration;

            // 점점 빨라지는 깜빡임
            float currentPulseSpeed = Mathf.Lerp(pulseSpeed, pulseSpeed * 3f, progress);
            float pulse = Mathf.Sin(elapsed * currentPulseSpeed) * 0.3f + 0.7f;

            // 기본 강도 증가
            float baseAlpha = Mathf.Lerp(0.3f, 0.9f, progress);

            // Fill 색상
            Color fillC = _fillColor;
            fillC.a = baseAlpha * pulse;

            // 마지막 25%에서 더 붉어짐
            if (progress > 0.75f)
            {
                float redProgress = (progress - 0.75f) / 0.25f;
                fillC = Color.Lerp(fillC, new Color(1f, 0f, 0f, fillC.a), redProgress * 0.5f);
            }
            _fillMaterial.color = fillC;

            // Edge 색상 (더 밝게)
            Color edgeC = _edgeColor;
            edgeC.a = Mathf.Min(1f, baseAlpha * pulse * 1.5f);
            _edgeMaterial.color = edgeC;

            if (_edgeRenderer != null)
            {
                _edgeRenderer.startColor = edgeC;
                _edgeRenderer.endColor = edgeC;

                // 테두리 두께 펄스
                float edgeWidth = _edgeWidth * (1f + Mathf.Sin(elapsed * currentPulseSpeed * 0.5f) * 0.3f);
                edgeWidth = Mathf.Lerp(edgeWidth, edgeWidth * 1.5f, progress);
                _edgeRenderer.startWidth = edgeWidth;
                _edgeRenderer.endWidth = edgeWidth;
            }

        }, duration, duration).SetEase(Ease.Linear);
    }

    private void StartOuterPulse(float duration)
    {
        if (_isDestroyed) return;

        float pulseCycle = 0.5f;
        int pulseCount = Mathf.CeilToInt(duration / pulseCycle);

        Sequence outerSeq = DOTween.Sequence();

        for (int i = 0; i < pulseCount; i++)
        {
            float delay = i * pulseCycle;

            outerSeq.Insert(delay, _pulseRingObject.transform.DOScale(Vector3.one * 0.3f, 0f));
            outerSeq.Insert(delay, DOTween.To(() => 0f, a => SetRingAlpha(_pulseRingRenderer, _pulseRingMaterial, a), 0.6f, 0.1f));
            outerSeq.Insert(delay, _pulseRingObject.transform.DOScale(Vector3.one * 1.2f, pulseCycle * 0.8f).SetEase(Ease.OutQuad));
            outerSeq.Insert(delay + 0.1f, DOTween.To(() => 0.6f, a => SetRingAlpha(_pulseRingRenderer, _pulseRingMaterial, a), 0f, pulseCycle * 0.7f).SetEase(Ease.OutQuad));
        }
    }

    private void StartWarningBlink(float duration)
    {
        if (_isDestroyed || _warningText == null) return;

        // 경고 마크 깜빡임 (점점 빨라짐)
        float elapsed = 0f;

        DOTween.To(() => elapsed, x =>
        {
            if (_isDestroyed || _warningText == null) return;

            elapsed = x;
            float progress = elapsed / duration;

            // 점점 빨라지는 깜빡임
            float blinkSpeed = Mathf.Lerp(4f, 20f, progress);
            float blink = Mathf.Sin(elapsed * blinkSpeed) > 0 ? 1f : 0.3f;

            Color c = _warningColor;
            c.a = blink;
            _warningText.color = c;

            // 크기도 약간 펄스
            float scale = 3f + Mathf.Sin(elapsed * blinkSpeed * 0.5f) * 0.5f;
            _warningMarkObject.transform.localScale = Vector3.one * scale;

        }, duration, duration).SetEase(Ease.Linear);
    }

    private void ExecuteFinalFlash(System.Action onComplete)
    {
        if (_isDestroyed) return;

        // 모든 시퀀스 정지
        _mainSequence?.Kill();
        _pulseSequence?.Kill();
        _rotateSequence?.Kill();

        Sequence flashSeq = DOTween.Sequence();

        // 1. 흰색 플래시 + 확대
        flashSeq.Append(_fillObject.transform.DOScale(Vector3.one * 1.2f, 0.1f).SetEase(Ease.OutQuad));
        flashSeq.Join(DOTween.To(() => _fillMaterial.color, c => {
            if (_fillMaterial != null) _fillMaterial.color = c;
        }, Color.white, 0.1f));
        flashSeq.Join(DOTween.To(() => _edgeMaterial.color, c => {
            if (_edgeMaterial != null) _edgeMaterial.color = c;
            if (_edgeRenderer != null)
            {
                _edgeRenderer.startColor = c;
                _edgeRenderer.endColor = c;
            }
        }, Color.white, 0.1f));

        // 경고 마크 사라짐
        flashSeq.Join(_warningMarkObject.transform.DOScale(Vector3.zero, 0.1f).SetEase(Ease.InBack));

        // 2. 빠르게 페이드 아웃
        flashSeq.Append(DOTween.To(() => 1f, a =>
        {
            if (_isDestroyed) return;

            if (_fillMaterial != null)
            {
                Color c = _fillMaterial.color;
                c.a = a;
                _fillMaterial.color = c;
            }
            if (_edgeMaterial != null)
            {
                Color c = _edgeMaterial.color;
                c.a = a;
                _edgeMaterial.color = c;
            }
            if (_edgeRenderer != null)
            {
                Color c = new Color(1f, 1f, 1f, a);
                _edgeRenderer.startColor = c;
                _edgeRenderer.endColor = c;
            }
        }, 0f, 0.15f).SetEase(Ease.OutQuad));

        // 3. 완료
        flashSeq.OnComplete(() =>
        {
            onComplete?.Invoke();
            if (!_isDestroyed)
            {
                Destroy(gameObject);
            }
        });
    }

    private void SetRingAlpha(LineRenderer lr, Material mat, float alpha)
    {
        if (_isDestroyed || lr == null || mat == null) return;

        Color c = mat.color;
        c.a = alpha;
        mat.color = c;
        lr.startColor = c;
        lr.endColor = c;
    }

    #endregion

    public void Hide()
    {
        KillAllTweens();
        if (!_isDestroyed)
        {
            Destroy(gameObject);
        }
    }

    public void SetRadius(float radius)
    {
        _radius = radius;
    }

    private void KillAllTweens()
    {
        _mainSequence?.Kill();
        _pulseSequence?.Kill();
        _rotateSequence?.Kill();
        DOTween.Kill(transform);
        DOTween.Kill(_fillObject?.transform);
        DOTween.Kill(_edgeObject?.transform);
        DOTween.Kill(_innerRingObject?.transform);
        DOTween.Kill(_pulseRingObject?.transform);
        DOTween.Kill(_warningMarkObject?.transform);
    }

    private void OnDestroy()
    {
        _isDestroyed = true;
        KillAllTweens();

        if (_fillMaterial != null) Destroy(_fillMaterial);
        if (_edgeMaterial != null) Destroy(_edgeMaterial);
        if (_innerRingMaterial != null) Destroy(_innerRingMaterial);
        if (_pulseRingMaterial != null) Destroy(_pulseRingMaterial);
        if (_fillMesh != null) Destroy(_fillMesh);
    }
}
