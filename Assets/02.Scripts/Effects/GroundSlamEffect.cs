using UnityEngine;
using DG.Tweening;

/// <summary>
/// 지면 강타 충격파 이펙트 (DOTween Pro 고급 버전)
/// 여러 겹의 충격파, 파편, 균열 효과
/// </summary>
public class GroundSlamEffect : MonoBehaviour
{
    [Header("충격파 설정")]
    [SerializeField] private float _maxRadius = 10f;
    [SerializeField] private float _expandDuration = 0.5f;
    [SerializeField] private int _waveCount = 3;
    [SerializeField] private float _waveDelay = 0.1f;
    [SerializeField] private int _segments = 64;

    [Header("색상")]
    [SerializeField] private Color _primaryColor = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private Color _secondaryColor = new Color(1f, 0.2f, 0f, 0.8f);
    [SerializeField] private Color _flashColor = new Color(1f, 1f, 0.8f, 1f);

    [Header("파편 설정")]
    [SerializeField] private int _debrisCount = 16;
    [SerializeField] private float _debrisHeight = 3f;
    [SerializeField] private float _debrisRadius = 5f;

    [Header("균열 설정")]
    [SerializeField] private int _crackCount = 8;
    [SerializeField] private float _crackLength = 6f;

    [Header("먼지 설정")]
    [SerializeField] private int _dustCount = 20;
    [SerializeField] private float _dustHeight = 2.5f;
    [SerializeField] private float _dustRadius = 4f;

    private Sequence _mainSequence;

    /// <summary>
    /// 정적 생성 메서드
    /// </summary>
    public static GroundSlamEffect Create(Vector3 position, float radius = 10f)
    {
        GameObject obj = new GameObject("GroundSlamEffect");
        obj.transform.position = position + Vector3.up * 0.1f;

        var effect = obj.AddComponent<GroundSlamEffect>();
        effect._maxRadius = radius;

        return effect;
    }

    private void Start()
    {
        PlayEffect();
    }

    private void PlayEffect()
    {
        _mainSequence = DOTween.Sequence();

        // === 1. 중앙 플래시 ===
        CreateCenterFlash();

        // === 2. 다중 충격파 링 ===
        for (int i = 0; i < _waveCount; i++)
        {
            float delay = i * _waveDelay;
            float ringWidth = Mathf.Lerp(0.8f, 0.3f, (float)i / _waveCount);
            float duration = _expandDuration * (1f + i * 0.2f);
            Color color = Color.Lerp(_primaryColor, _secondaryColor, (float)i / _waveCount);

            CreateShockwaveRing(delay, ringWidth, duration, color);
        }

        // === 3. 바닥 균열 ===
        CreateGroundCracks();

        // === 4. 파편 튀김 ===
        CreateDebris();

        // === 5. 먼지 구름 ===
        CreateDustCloud();

        // === 6. 내부 충격 링 (안에서 바깥으로 빠르게) ===
        CreateInnerBlast();

        // 전체 효과 완료 후 제거
        float totalDuration = _expandDuration + (_waveCount * _waveDelay) + 0.5f;
        Destroy(gameObject, totalDuration + 1f);
    }

    #region 중앙 플래시

    private void CreateCenterFlash()
    {
        GameObject flash = new GameObject("CenterFlash");
        flash.transform.SetParent(transform);
        flash.transform.localPosition = Vector3.up * 0.05f;
        flash.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        MeshFilter mf = flash.AddComponent<MeshFilter>();
        MeshRenderer mr = flash.AddComponent<MeshRenderer>();

        mf.mesh = CreateCircleMesh(2f);

        Material mat = CreateMaterial(_flashColor);
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // 플래시 애니메이션
        flash.transform.localScale = Vector3.zero;

        Sequence flashSeq = DOTween.Sequence();
        flashSeq.Append(flash.transform.DOScale(Vector3.one * 3f, 0.08f).SetEase(Ease.OutQuad));
        flashSeq.Join(DOTween.To(() => mat.color, c => mat.color = c,
            new Color(_flashColor.r, _flashColor.g, _flashColor.b, 0f), 0.2f).SetEase(Ease.OutQuad));
        flashSeq.Join(flash.transform.DOScale(Vector3.one * 5f, 0.2f).SetEase(Ease.OutQuad));
        flashSeq.OnComplete(() => {
            Destroy(mat);
            Destroy(flash);
        });
    }

    #endregion

    #region 충격파 링

    private void CreateShockwaveRing(float delay, float width, float duration, Color color)
    {
        GameObject ring = new GameObject($"ShockwaveRing_{delay}");
        ring.transform.SetParent(transform);
        ring.transform.localPosition = Vector3.up * 0.05f;

        LineRenderer lr = ring.AddComponent<LineRenderer>();
        SetupRingLineRenderer(lr, 0.1f, width);

        Material mat = CreateMaterial(color);
        lr.material = mat;
        lr.startColor = color;
        lr.endColor = color;

        // 시퀀스로 확장 애니메이션
        Sequence ringSeq = DOTween.Sequence();
        ringSeq.SetDelay(delay);

        float currentRadius = 0.1f;

        // 확장
        ringSeq.Append(DOTween.To(() => currentRadius, r => {
            currentRadius = r;
            UpdateRingPositions(lr, r);
        }, _maxRadius, duration).SetEase(Ease.OutQuad));

        // 색상 페이드
        ringSeq.Join(DOTween.To(() => color, c => {
            mat.color = c;
            lr.startColor = c;
            lr.endColor = c;
        }, new Color(color.r, color.g, color.b, 0f), duration).SetEase(Ease.InQuad));

        // 두께 감소
        ringSeq.Join(DOTween.To(() => width, w => {
            lr.startWidth = w;
            lr.endWidth = w;
        }, 0.05f, duration).SetEase(Ease.InQuad));

        ringSeq.OnComplete(() => {
            Destroy(mat);
            Destroy(ring);
        });
    }

    private void SetupRingLineRenderer(LineRenderer lr, float radius, float width)
    {
        lr.positionCount = _segments + 1;
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        UpdateRingPositions(lr, radius);
    }

    private void UpdateRingPositions(LineRenderer lr, float radius)
    {
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

    #region 바닥 균열

    private void CreateGroundCracks()
    {
        for (int i = 0; i < _crackCount; i++)
        {
            float angle = (360f / _crackCount) * i + Random.Range(-15f, 15f);
            CreateSingleCrack(angle);
        }
    }

    private void CreateSingleCrack(float angle)
    {
        GameObject crack = new GameObject("Crack");
        crack.transform.SetParent(transform);
        crack.transform.localPosition = Vector3.up * 0.03f;
        crack.transform.localRotation = Quaternion.Euler(0f, angle, 0f);

        LineRenderer lr = crack.AddComponent<LineRenderer>();
        lr.positionCount = 5; // 지그재그 균열
        lr.useWorldSpace = false;
        lr.startWidth = 0.15f;
        lr.endWidth = 0.05f;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // 균열 포인트 (지그재그)
        float length = _crackLength * Random.Range(0.7f, 1.3f);
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, new Vector3(Random.Range(-0.3f, 0.3f), 0f, length * 0.25f));
        lr.SetPosition(2, new Vector3(Random.Range(-0.5f, 0.5f), 0f, length * 0.5f));
        lr.SetPosition(3, new Vector3(Random.Range(-0.3f, 0.3f), 0f, length * 0.75f));
        lr.SetPosition(4, new Vector3(Random.Range(-0.2f, 0.2f), 0f, length));

        Color crackColor = new Color(0.3f, 0.2f, 0.1f, 1f);
        Material mat = CreateMaterial(crackColor);
        lr.material = mat;
        lr.startColor = crackColor;
        lr.endColor = new Color(crackColor.r, crackColor.g, crackColor.b, 0f);

        // 균열 확장 애니메이션
        crack.transform.localScale = new Vector3(1f, 1f, 0f);

        Sequence crackSeq = DOTween.Sequence();
        crackSeq.SetDelay(0.05f);
        crackSeq.Append(crack.transform.DOScaleZ(1f, 0.15f).SetEase(Ease.OutQuad));

        // 페이드 아웃
        crackSeq.Append(DOTween.To(() => crackColor.a, a => {
            Color c = new Color(crackColor.r, crackColor.g, crackColor.b, a);
            mat.color = c;
            lr.startColor = c;
            lr.endColor = new Color(c.r, c.g, c.b, 0f);
        }, 0f, 1f).SetDelay(0.5f));

        crackSeq.OnComplete(() => {
            Destroy(mat);
            Destroy(crack);
        });
    }

    #endregion

    #region 파편

    private void CreateDebris()
    {
        for (int i = 0; i < _debrisCount; i++)
        {
            float delay = Random.Range(0f, 0.1f);
            CreateSingleDebris(delay);
        }
    }

    private void CreateSingleDebris(float delay)
    {
        // 랜덤 위치
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(0.5f, _debrisRadius);
        Vector3 startPos = transform.position + new Vector3(
            Mathf.Cos(angle) * distance * 0.3f,
            0.1f,
            Mathf.Sin(angle) * distance * 0.3f
        );

        // 파편 생성
        GameObject debris = GameObject.CreatePrimitive(
            Random.value > 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere
        );
        debris.name = "Debris";
        debris.transform.position = startPos;

        float size = Random.Range(0.1f, 0.3f);
        debris.transform.localScale = Vector3.one * size;
        debris.transform.rotation = Random.rotation;

        // 콜라이더 제거
        var col = debris.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // 머티리얼
        var renderer = debris.GetComponent<Renderer>();
        Color debrisColor = new Color(
            Random.Range(0.3f, 0.5f),
            Random.Range(0.2f, 0.4f),
            Random.Range(0.1f, 0.3f),
            1f
        );
        Material mat = CreateMaterial(debrisColor);
        renderer.material = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // 목표 위치 (포물선)
        Vector3 targetPos = transform.position + new Vector3(
            Mathf.Cos(angle) * distance,
            0f,
            Mathf.Sin(angle) * distance
        );

        float height = _debrisHeight * Random.Range(0.5f, 1.5f);
        float duration = Random.Range(0.4f, 0.8f);

        // DOTween Pro 시퀀스
        Sequence debrisSeq = DOTween.Sequence();
        debrisSeq.SetDelay(delay);

        // 포물선 이동 (DOPath 또는 Jump)
        debrisSeq.Append(debris.transform.DOJump(targetPos, height, 1, duration)
            .SetEase(Ease.OutQuad));

        // 회전
        debrisSeq.Join(debris.transform.DORotate(
            Random.insideUnitSphere * 720f, duration, RotateMode.FastBeyond360
        ).SetEase(Ease.Linear));

        // 착지 후 바운스 + 축소
        debrisSeq.Append(debris.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 3, 0.5f));
        debrisSeq.Append(debris.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));

        debrisSeq.OnComplete(() => {
            Destroy(mat);
            Destroy(debris);
        });
    }

    #endregion

    #region 먼지 구름

    private void CreateDustCloud()
    {
        for (int i = 0; i < _dustCount; i++)
        {
            float delay = Random.Range(0f, 0.15f);
            CreateSingleDust(delay);
        }
    }

    private void CreateSingleDust(float delay)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(0.3f, _dustRadius);

        Vector3 startPos = transform.position + new Vector3(
            Mathf.Cos(angle) * distance * 0.5f,
            0.1f,
            Mathf.Sin(angle) * distance * 0.5f
        );

        // 먼지 파티클 (구체)
        GameObject dust = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dust.name = "Dust";
        dust.transform.position = startPos;

        float size = Random.Range(0.3f, 0.8f);
        dust.transform.localScale = Vector3.one * size;

        var col = dust.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var renderer = dust.GetComponent<Renderer>();
        Color dustColor = new Color(0.6f, 0.55f, 0.45f, 0.6f);
        Material mat = CreateMaterial(dustColor);
        renderer.material = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Vector3 targetPos = transform.position + new Vector3(
            Mathf.Cos(angle) * distance,
            _dustHeight * Random.Range(0.5f, 1.5f),
            Mathf.Sin(angle) * distance
        );

        float duration = Random.Range(0.6f, 1.2f);

        Sequence dustSeq = DOTween.Sequence();
        dustSeq.SetDelay(delay);

        // 이동 (위로 퍼짐)
        dustSeq.Append(dust.transform.DOMove(targetPos, duration).SetEase(Ease.OutQuad));

        // 확대
        dustSeq.Join(dust.transform.DOScale(Vector3.one * size * 2f, duration * 0.5f)
            .SetEase(Ease.OutQuad));

        // 페이드 아웃
        dustSeq.Join(DOTween.To(() => dustColor.a, a => {
            Color c = new Color(dustColor.r, dustColor.g, dustColor.b, a);
            mat.color = c;
        }, 0f, duration).SetEase(Ease.InQuad));

        // 후반 축소
        dustSeq.Join(dust.transform.DOScale(Vector3.one * size * 0.5f, duration * 0.5f)
            .SetEase(Ease.InQuad)
            .SetDelay(duration * 0.5f));

        dustSeq.OnComplete(() => {
            Destroy(mat);
            Destroy(dust);
        });
    }

    #endregion

    #region 내부 충격

    private void CreateInnerBlast()
    {
        // 빠르게 퍼지는 내부 링
        GameObject blast = new GameObject("InnerBlast");
        blast.transform.SetParent(transform);
        blast.transform.localPosition = Vector3.up * 0.08f;
        blast.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        MeshFilter mf = blast.AddComponent<MeshFilter>();
        MeshRenderer mr = blast.AddComponent<MeshRenderer>();

        // 링 메시 생성
        mf.mesh = CreateRingMesh(0.5f, 1f);

        Color blastColor = new Color(1f, 0.8f, 0.3f, 0.9f);
        Material mat = CreateMaterial(blastColor);
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        blast.transform.localScale = Vector3.zero;

        Sequence blastSeq = DOTween.Sequence();
        blastSeq.Append(blast.transform.DOScale(Vector3.one * _maxRadius * 0.8f, 0.15f)
            .SetEase(Ease.OutQuad));
        blastSeq.Join(DOTween.To(() => blastColor.a, a => {
            mat.color = new Color(blastColor.r, blastColor.g, blastColor.b, a);
        }, 0f, 0.15f).SetEase(Ease.InQuad));

        blastSeq.OnComplete(() => {
            Destroy(mat);
            Destroy(blast);
        });
    }

    #endregion

    #region 유틸리티

    private Material CreateMaterial(Color color)
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
        mat.renderQueue = 3100;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        return mat;
    }

    private Mesh CreateCircleMesh(float radius)
    {
        Mesh mesh = new Mesh { name = "CircleMesh" };

        int segments = 32;
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % segments + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private Mesh CreateRingMesh(float innerRadius, float outerRadius)
    {
        Mesh mesh = new Mesh { name = "RingMesh" };

        int segments = 32;
        Vector3[] vertices = new Vector3[segments * 2];
        int[] triangles = new int[segments * 6];

        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            vertices[i * 2] = new Vector3(cos * innerRadius, sin * innerRadius, 0f);
            vertices[i * 2 + 1] = new Vector3(cos * outerRadius, sin * outerRadius, 0f);
        }

        for (int i = 0; i < segments; i++)
        {
            int current = i * 2;
            int next = ((i + 1) % segments) * 2;

            triangles[i * 6] = current;
            triangles[i * 6 + 1] = current + 1;
            triangles[i * 6 + 2] = next;

            triangles[i * 6 + 3] = next;
            triangles[i * 6 + 4] = current + 1;
            triangles[i * 6 + 5] = next + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    #endregion

    private void OnDestroy()
    {
        _mainSequence?.Kill();
        DOTween.Kill(transform);
    }
}
