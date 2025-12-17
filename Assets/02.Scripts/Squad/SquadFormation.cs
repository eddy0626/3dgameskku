using UnityEngine;
using System;

/// <summary>
/// 분대 대형 시스템
/// - 다양한 대형 타입 지원 (Circle, Line, Wedge, Spread)
/// - 플레이어 기준 상대 위치 계산
/// - 동적 대형 전환 지원
/// - 에디터 시각화 (Gizmos)
///
/// 사용법:
/// 1. Static Utility: SquadFormation.GetFormationPosition(...)
/// 2. Component: GetComponent<SquadFormation>().CalculatePosition(...)
/// </summary>
public class SquadFormation : MonoBehaviour
{
    #region Enums
    /// <summary>
    /// 대형 타입
    /// </summary>
    public enum FormationType
    {
        Circle,     // 원형 - 방어에 유리, 360도 커버
        Line,       // 횡대 - 화력 집중, 넓은 전선
        Wedge,      // 쐐기 - 돌격에 유리, V자 진형
        Spread      // 산개 - 넓게 분산, 포위 공격
    }
    #endregion

    #region Inspector Fields
    [Header("대형 설정")]
    [SerializeField] private FormationType defaultFormationType = FormationType.Wedge;
    [SerializeField] private float formationRadius = 3f;
    [SerializeField] private float formationSpacing = 2f;

    [Header("세부 조정")]
    [Tooltip("Circle: 플레이어 뒤쪽으로 밀리는 정도 (0~1)")]
    [SerializeField, Range(0f, 1f)] private float circleBackwardOffset = 0.3f;

    [Tooltip("Wedge: V자 각도 (낮을수록 좁은 V)")]
    [SerializeField, Range(0.1f, 1f)] private float wedgeAngleFactor = 0.6f;

    [Tooltip("Wedge: 뒤로 갈수록 벌어지는 정도")]
    [SerializeField, Range(0.5f, 1.5f)] private float wedgeDepthFactor = 0.8f;

    [Tooltip("Spread: 분산 범위 (반경)")]
    [SerializeField] private float spreadRange = 5f;

    [Tooltip("Spread: 최소 간격")]
    [SerializeField] private float spreadMinDistance = 2f;

    [Header("동적 조정")]
    [Tooltip("인원수에 따라 반경 자동 조정")]
    [SerializeField] private bool autoScaleRadius = true;

    [Tooltip("인원당 추가 반경")]
    [SerializeField] private float radiusPerMember = 0.2f;

    [Header("디버그")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Transform referenceTransform;
    #endregion

    #region Properties
    public FormationType CurrentFormationType
    {
        get => defaultFormationType;
        set => defaultFormationType = value;
    }

    public float FormationRadius
    {
        get => formationRadius;
        set => formationRadius = Mathf.Max(0.5f, value);
    }

    public float FormationSpacing
    {
        get => formationSpacing;
        set => formationSpacing = Mathf.Max(0.5f, value);
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (referenceTransform == null)
        {
            // 기본적으로 플레이어를 찾음
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                referenceTransform = player.transform;
            }
        }
    }

    private void OnValidate()
    {
        // Inspector에서 값 변경 시 검증
        formationRadius = Mathf.Max(0.5f, formationRadius);
        formationSpacing = Mathf.Max(0.5f, formationSpacing);
        spreadRange = Mathf.Max(1f, spreadRange);
        spreadMinDistance = Mathf.Max(0.5f, spreadMinDistance);
    }
    #endregion

    #region Public API - Instance Methods
    /// <summary>
    /// 대형 위치 계산 (인스턴스 메서드)
    /// </summary>
    /// <param name="index">분대원 인덱스 (0부터 시작)</param>
    /// <param name="totalMembers">전체 분대원 수</param>
    /// <param name="reference">기준 Transform (null이면 referenceTransform 사용)</param>
    /// <returns>월드 좌표 위치</returns>
    public Vector3 CalculatePosition(int index, int totalMembers, Transform reference = null)
    {
        Transform refTrans = reference ?? referenceTransform;
        if (refTrans == null)
        {
            Debug.LogWarning("[SquadFormation] Reference transform is null!");
            return Vector3.zero;
        }

        float effectiveRadius = GetEffectiveRadius(totalMembers);

        return defaultFormationType switch
        {
            FormationType.Circle => CalculateCirclePosition(index, totalMembers, refTrans, effectiveRadius),
            FormationType.Line => CalculateLinePosition(index, totalMembers, refTrans, effectiveRadius),
            FormationType.Wedge => CalculateWedgePosition(index, totalMembers, refTrans, effectiveRadius),
            FormationType.Spread => CalculateSpreadPosition(index, totalMembers, refTrans),
            _ => refTrans.position
        };
    }

    /// <summary>
    /// 대형 변경
    /// </summary>
    public void SetFormationType(FormationType type)
    {
        defaultFormationType = type;
    }

    /// <summary>
    /// 대형 순환
    /// </summary>
    public FormationType CycleFormation()
    {
        int current = (int)defaultFormationType;
        int next = (current + 1) % Enum.GetValues(typeof(FormationType)).Length;
        defaultFormationType = (FormationType)next;
        return defaultFormationType;
    }
    #endregion

    #region Static API - Utility Methods
    /// <summary>
    /// 정적 메서드: 대형 위치 계산
    /// </summary>
    public static Vector3 GetFormationPosition(
        int index,
        int totalMembers,
        Transform reference,
        FormationType formationType,
        float radius = 3f,
        float spacing = 2f)
    {
        if (reference == null)
        {
            Debug.LogWarning("[SquadFormation] Reference transform is null!");
            return Vector3.zero;
        }

        totalMembers = Mathf.Max(1, totalMembers);

        return formationType switch
        {
            FormationType.Circle => GetCirclePosition(index, totalMembers, reference, radius),
            FormationType.Line => GetLinePosition(index, totalMembers, reference, radius, spacing),
            FormationType.Wedge => GetWedgePosition(index, totalMembers, reference, radius, spacing),
            FormationType.Spread => GetSpreadPosition(index, totalMembers, reference, radius * 1.5f, spacing),
            _ => reference.position
        };
    }

    /// <summary>
    /// 정적 메서드: 원형 대형
    /// </summary>
    public static Vector3 GetCirclePosition(int index, int totalMembers, Transform reference, float radius, float backwardOffset = 0.3f)
    {
        totalMembers = Mathf.Max(1, totalMembers);

        // 균등하게 원형 배치
        float angle = (360f / totalMembers) * index;
        float rad = angle * Mathf.Deg2Rad;

        Vector3 localOffset = new Vector3(
            Mathf.Sin(rad) * radius,
            0,
            Mathf.Cos(rad) * radius
        );

        // 월드 좌표로 변환
        Vector3 worldOffset = reference.TransformDirection(localOffset);

        // 플레이어 뒤쪽으로 약간 이동 (360도 방어 형성)
        worldOffset -= reference.forward * radius * backwardOffset;

        return reference.position + worldOffset;
    }

    /// <summary>
    /// 정적 메서드: 횡대 대형
    /// </summary>
    public static Vector3 GetLinePosition(int index, int totalMembers, Transform reference, float backDistance, float spacing)
    {
        totalMembers = Mathf.Max(1, totalMembers);

        // 중앙 정렬: 홀수면 중앙에 한 명, 짝수면 중앙 양쪽에 배치
        float offset = (index - (totalMembers - 1) / 2f) * spacing;

        Vector3 position = reference.position;
        position -= reference.forward * backDistance; // 뒤로
        position += reference.right * offset; // 좌우로

        return position;
    }

    /// <summary>
    /// 정적 메서드: 쐐기 대형 (V자)
    /// </summary>
    public static Vector3 GetWedgePosition(
        int index,
        int totalMembers,
        Transform reference,
        float baseDistance,
        float spacing,
        float angleFactor = 0.6f,
        float depthFactor = 0.8f)
    {
        // V자 대형: 좌우 교대 배치
        // index 0 -> 왼쪽 첫번째
        // index 1 -> 오른쪽 첫번째
        // index 2 -> 왼쪽 두번째

        int row = (index / 2) + 1; // 행 번호 (1부터)
        int side = (index % 2 == 0) ? -1 : 1; // 0,2,4... = 왼쪽, 1,3,5... = 오른쪽

        // 뒤로 갈수록 더 깊이, 더 넓게
        float backOffset = row * spacing * depthFactor;
        float sideOffset = row * spacing * angleFactor * side;

        Vector3 position = reference.position;
        position -= reference.forward * (baseDistance + backOffset);
        position += reference.right * sideOffset;

        return position;
    }

    /// <summary>
    /// 정적 메서드: 산개 대형
    /// </summary>
    public static Vector3 GetSpreadPosition(
        int index,
        int totalMembers,
        Transform reference,
        float spreadRange,
        float minDistance)
    {
        totalMembers = Mathf.Max(1, totalMembers);

        // 격자형 + 랜덤 오프셋 방식
        // 인원수에 따라 격자 크기 결정
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(totalMembers));

        int row = index / gridSize;
        int col = index % gridSize;

        // 그리드 중심을 플레이어 뒤로
        float gridWidth = (gridSize - 1) * minDistance;
        float startX = -gridWidth / 2f;
        float startZ = -spreadRange / 2f;

        // 기본 그리드 위치
        float x = startX + col * minDistance;
        float z = startZ - row * minDistance;

        // 약간의 랜덤성 추가 (시드를 인덱스로 사용해 일관성 유지)
        UnityEngine.Random.InitState(index + 1000);
        x += UnityEngine.Random.Range(-minDistance * 0.3f, minDistance * 0.3f);
        z += UnityEngine.Random.Range(-minDistance * 0.3f, minDistance * 0.3f);

        Vector3 localOffset = new Vector3(x, 0, z);
        Vector3 worldOffset = reference.TransformDirection(localOffset);

        return reference.position + worldOffset;
    }
    #endregion

    #region Formation Calculations - Instance
    private Vector3 CalculateCirclePosition(int index, int totalMembers, Transform reference, float radius)
    {
        return GetCirclePosition(index, totalMembers, reference, radius, circleBackwardOffset);
    }

    private Vector3 CalculateLinePosition(int index, int totalMembers, Transform reference, float backDistance)
    {
        return GetLinePosition(index, totalMembers, reference, backDistance, formationSpacing);
    }

    private Vector3 CalculateWedgePosition(int index, int totalMembers, Transform reference, float baseDistance)
    {
        return GetWedgePosition(index, totalMembers, reference, baseDistance, formationSpacing, wedgeAngleFactor, wedgeDepthFactor);
    }

    private Vector3 CalculateSpreadPosition(int index, int totalMembers, Transform reference)
    {
        return GetSpreadPosition(index, totalMembers, reference, spreadRange, spreadMinDistance);
    }

    private float GetEffectiveRadius(int totalMembers)
    {
        if (!autoScaleRadius) return formationRadius;

        // 인원수에 따라 반경 자동 조정
        return formationRadius + (totalMembers - 1) * radiusPerMember;
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// 대형 이름 가져오기
    /// </summary>
    public static string GetFormationName(FormationType type)
    {
        return type switch
        {
            FormationType.Circle => "원형 대형",
            FormationType.Line => "횡대 대형",
            FormationType.Wedge => "쐐기 대형",
            FormationType.Spread => "산개 대형",
            _ => "알 수 없음"
        };
    }

    /// <summary>
    /// 대형 설명 가져오기
    /// </summary>
    public static string GetFormationDescription(FormationType type)
    {
        return type switch
        {
            FormationType.Circle => "플레이어를 중심으로 원형 배치. 360도 방어에 유리.",
            FormationType.Line => "일렬 횡대. 넓은 전선과 화력 집중에 유리.",
            FormationType.Wedge => "V자 쐐기형. 돌격과 집중 돌파에 유리.",
            FormationType.Spread => "넓게 산개. 포위 공격과 탐색에 유리.",
            _ => "알 수 없는 대형"
        };
    }

    /// <summary>
    /// 대형 색상 (시각화용)
    /// </summary>
    public static Color GetFormationColor(FormationType type)
    {
        return type switch
        {
            FormationType.Circle => Color.cyan,
            FormationType.Line => Color.yellow,
            FormationType.Wedge => Color.magenta,
            FormationType.Spread => Color.green,
            _ => Color.white
        };
    }

    /// <summary>
    /// 특정 위치에서 가장 가까운 대형 슬롯 찾기
    /// </summary>
    public int FindClosestFormationSlot(Vector3 position, int totalMembers, Transform reference = null)
    {
        Transform refTrans = reference ?? referenceTransform;
        if (refTrans == null) return 0;

        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < totalMembers; i++)
        {
            Vector3 slotPosition = CalculatePosition(i, totalMembers, refTrans);
            float distance = Vector3.Distance(position, slotPosition);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    /// <summary>
    /// 모든 대형 위치 배열로 반환
    /// </summary>
    public Vector3[] GetAllFormationPositions(int totalMembers, Transform reference = null)
    {
        Transform refTrans = reference ?? referenceTransform;
        if (refTrans == null) return new Vector3[0];

        Vector3[] positions = new Vector3[totalMembers];
        for (int i = 0; i < totalMembers; i++)
        {
            positions[i] = CalculatePosition(i, totalMembers, refTrans);
        }

        return positions;
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Transform reference = referenceTransform;
        if (reference == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) reference = player.transform;
        }

        if (reference == null) return;

        // 테스트용 분대원 수 (에디터에서 시각화)
        int testMemberCount = 5;

        Color formationColor = GetFormationColor(defaultFormationType);
        float effectiveRadius = GetEffectiveRadius(testMemberCount);

        // 각 포지션 그리기
        for (int i = 0; i < testMemberCount; i++)
        {
            Vector3 pos = CalculatePosition(i, testMemberCount, reference);

            // 위치 표시
            Gizmos.color = formationColor;
            Gizmos.DrawWireSphere(pos, 0.5f);
            Gizmos.DrawSphere(pos, 0.2f);

            // 인덱스 표시를 위한 작은 구
            Gizmos.color = Color.white;
            Gizmos.DrawLine(pos, pos + Vector3.up * 0.5f);

            // 기준점과의 연결선
            Gizmos.color = new Color(formationColor.r, formationColor.g, formationColor.b, 0.3f);
            Gizmos.DrawLine(reference.position, pos);
        }

        // 대형 반경 표시
        Gizmos.color = new Color(formationColor.r, formationColor.g, formationColor.b, 0.5f);
        DrawCircle(reference.position, effectiveRadius, 32);

        // 전방 방향 표시
        Gizmos.color = Color.red;
        Gizmos.DrawRay(reference.position, reference.forward * effectiveRadius);
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    #endregion

    #region Editor Helper
#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 대형 미리보기
    /// </summary>
    [ContextMenu("Preview All Formations")]
    private void PreviewAllFormations()
    {
        if (referenceTransform == null)
        {
            Debug.LogWarning("Reference Transform이 없습니다!");
            return;
        }

        int testCount = 5;
        Debug.Log("=== Formation Preview ===");

        foreach (FormationType type in Enum.GetValues(typeof(FormationType)))
        {
            Debug.Log($"\n[{GetFormationName(type)}]");
            Debug.Log(GetFormationDescription(type));

            FormationType originalType = defaultFormationType;
            defaultFormationType = type;

            for (int i = 0; i < testCount; i++)
            {
                Vector3 pos = CalculatePosition(i, testCount, referenceTransform);
                Debug.Log($"  Member {i}: {pos}");
            }

            defaultFormationType = originalType;
        }
    }

    /// <summary>
    /// 대형 정보 출력
    /// </summary>
    [ContextMenu("Show Formation Info")]
    private void ShowFormationInfo()
    {
        Debug.Log($"=== Squad Formation Info ===");
        Debug.Log($"Formation Type: {GetFormationName(defaultFormationType)}");
        Debug.Log($"Description: {GetFormationDescription(defaultFormationType)}");
        Debug.Log($"Radius: {formationRadius}");
        Debug.Log($"Spacing: {formationSpacing}");
        Debug.Log($"Auto Scale: {autoScaleRadius}");
    }
#endif
    #endregion
}
