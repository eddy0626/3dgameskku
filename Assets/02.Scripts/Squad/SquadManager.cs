using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 분대 관리 시스템
/// - 싱글톤 패턴
/// - 분대원 List 관리 (최대 5명)
/// - 대형 시스템 (Circle, Line, Wedge)
/// - 공격/따라오기 명령
///
/// EnemyAI.cs 패턴 참고
/// </summary>
public class SquadManager : MonoBehaviour
{
    #region Singleton
    public static SquadManager Instance { get; private set; }
    #endregion

    #region Enums
    /// <summary>
    /// 대형 타입 (SquadFormation과 동기화)
    /// </summary>
    public enum FormationType
    {
        Circle,     // 원형 - 방어에 유리
        Line,       // 횡대 - 화력 집중
        Wedge,      // 쐐기 - 돌격에 유리
        Spread      // 산개 - 넓게 분산
    }

    /// <summary>
    /// 분대 명령 타입
    /// </summary>
    public enum SquadCommand
    {
        Follow,     // 플레이어 따라다니기 (기본)
        Attack,     // 지정 위치/적 공격
        Hold        // 현재 위치 고수
    }
    #endregion

    #region Inspector Fields
    [Header("분대 설정")]
    [SerializeField] private int maxSquadSize = 5;
    [SerializeField] private List<SquadMemberData> availableMembers;

    [Header("대형 설정")]
    [SerializeField] private FormationType formationType = FormationType.Wedge;
    [SerializeField] private float formationRadius = 3f;
    [SerializeField] private float formationSpacing = 2f;

    [Header("명령 설정")]
    [SerializeField] private SquadCommand currentCommand = SquadCommand.Follow;
    [SerializeField] private float commandResponseTime = 0.5f;

    [Header("스폰 설정")]
    [SerializeField] private Transform spawnPoint;

    [Header("입력 설정")]
    [SerializeField] private KeyCode formationKey = KeyCode.F;
    [SerializeField] private KeyCode attackKey = KeyCode.G;
    [SerializeField] private KeyCode followKey = KeyCode.H;
    [SerializeField] private KeyCode holdKey = KeyCode.J;

    [Header("디버그")]
    [SerializeField] private bool showFormationGizmos = true;
    [SerializeField] private bool showDebugLogs = true;
    #endregion

    #region Private Fields
    private List<SquadMember> squadMembers = new List<SquadMember>();
    private Transform player;
    private Vector3 attackTargetPosition;
    private Transform attackTarget;
    private Camera mainCamera;
    #endregion

    #region Properties
    public int CurrentSquadSize => squadMembers.Count;
    public int MaxSquadSize => maxSquadSize;
    public FormationType CurrentFormation => formationType;
    public SquadCommand CurrentCommand => currentCommand;
    public IReadOnlyList<SquadMember> Members => squadMembers;
    public bool IsSquadFull => squadMembers.Count >= maxSquadSize;
    #endregion

    #region Events
    public event Action<SquadMember> OnMemberAdded;
    public event Action<SquadMember> OnMemberRemoved;
    public event Action<int, int> OnSquadSizeChanged; // current, max
    public event Action<FormationType> OnFormationChanged;
    public event Action<SquadCommand> OnCommandChanged;
    public event Action<Vector3> OnAttackOrderIssued;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 플레이어 찾기
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        mainCamera = Camera.main;

        if (player == null)
        {
            Debug.LogError("[SquadManager] Player not found!");
        }

        LogDebug("SquadManager initialized");
    }

    private void Update()
    {
        HandleInput();
    }
    #endregion

    #region Input Handling
    private void HandleInput()
    {
        // 대형 변경 (F키)
        if (Input.GetKeyDown(formationKey))
        {
            CycleFormation();
        }

        // 공격 명령 (G키 + 마우스 클릭)
        if (Input.GetKeyDown(attackKey))
        {
            IssueAttackCommand();
        }

        // 따라오기 명령 (H키)
        if (Input.GetKeyDown(followKey))
        {
            IssueFollowCommand();
        }

        // 현재 위치 고수 (J키)
        if (Input.GetKeyDown(holdKey))
        {
            IssueHoldCommand();
        }
    }
    #endregion

    #region Formation Position Calculation
    /// <summary>
    /// 인덱스에 해당하는 대형 위치 반환
    /// SquadFormation 시스템 통합
    /// </summary>
    public Vector3 GetFormationPosition(int index)
    {
        if (player == null) return Vector3.zero;

        int totalMembers = Mathf.Max(squadMembers.Count, 1);

        // SquadFormation static 메서드 사용
        return SquadFormation.GetFormationPosition(
            index,
            totalMembers,
            player,
            ConvertToSquadFormationType(formationType),
            formationRadius,
            formationSpacing
        );
    }

    /// <summary>
    /// SquadManager.FormationType을 SquadFormation.FormationType으로 변환
    /// </summary>
    private SquadFormation.FormationType ConvertToSquadFormationType(FormationType type)
    {
        return type switch
        {
            FormationType.Circle => SquadFormation.FormationType.Circle,
            FormationType.Line => SquadFormation.FormationType.Line,
            FormationType.Wedge => SquadFormation.FormationType.Wedge,
            FormationType.Spread => SquadFormation.FormationType.Spread,
            _ => SquadFormation.FormationType.Wedge
        };
    }

    /// <summary>
    /// 대형 이름 가져오기
    /// </summary>
    public string GetFormationName()
    {
        return SquadFormation.GetFormationName(ConvertToSquadFormationType(formationType));
    }

    /// <summary>
    /// 대형 설명 가져오기
    /// </summary>
    public string GetFormationDescription()
    {
        return SquadFormation.GetFormationDescription(ConvertToSquadFormationType(formationType));
    }
    #endregion

    #region Squad Commands
    /// <summary>
    /// 따라오기 명령
    /// </summary>
    public void IssueFollowCommand()
    {
        currentCommand = SquadCommand.Follow;
        attackTarget = null;

        foreach (var member in squadMembers)
        {
            if (member != null && member.IsAlive)
            {
                member.SetCommand(SquadMember.MemberCommand.Follow);
            }
        }

        OnCommandChanged?.Invoke(currentCommand);
        LogDebug("명령: 따라오기");
    }

    /// <summary>
    /// 공격 명령 - 마우스 위치로 공격
    /// </summary>
    public void IssueAttackCommand()
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            IssueAttackCommand(hit.point, hit.transform);
        }
    }

    /// <summary>
    /// 공격 명령 - 특정 위치/타겟
    /// </summary>
    public void IssueAttackCommand(Vector3 position, Transform target = null)
    {
        currentCommand = SquadCommand.Attack;
        attackTargetPosition = position;
        attackTarget = target;

        foreach (var member in squadMembers)
        {
            if (member != null && member.IsAlive)
            {
                member.SetCommand(SquadMember.MemberCommand.Attack, position, target);
            }
        }

        OnCommandChanged?.Invoke(currentCommand);
        OnAttackOrderIssued?.Invoke(position);
        LogDebug($"명령: 공격 -> {position}");
    }

    /// <summary>
    /// 현재 위치 고수 명령
    /// </summary>
    public void IssueHoldCommand()
    {
        currentCommand = SquadCommand.Hold;

        foreach (var member in squadMembers)
        {
            if (member != null && member.IsAlive)
            {
                member.SetCommand(SquadMember.MemberCommand.Hold);
            }
        }

        OnCommandChanged?.Invoke(currentCommand);
        LogDebug("명령: 위치 고수");
    }

    /// <summary>
    /// 특정 멤버에게 명령
    /// </summary>
    public void IssueCommandToMember(SquadMember member, SquadMember.MemberCommand command,
        Vector3 position = default, Transform target = null)
    {
        if (member != null && member.IsAlive)
        {
            member.SetCommand(command, position, target);
        }
    }
    #endregion

    #region Formation Control
    /// <summary>
    /// 대형 설정
    /// </summary>
    public void SetFormationType(FormationType type)
    {
        if (formationType == type) return;

        formationType = type;
        OnFormationChanged?.Invoke(type);

        LogDebug($"대형 변경: {GetFormationName()}");
    }

    /// <summary>
    /// 대형 순환 (Circle -> Line -> Wedge -> Spread -> Circle)
    /// </summary>
    public void CycleFormation()
    {
        int current = (int)formationType;
        int next = (current + 1) % System.Enum.GetValues(typeof(FormationType)).Length;
        SetFormationType((FormationType)next);
    }

    /// <summary>
    /// 대형 반경 설정
    /// </summary>
    public void SetFormationRadius(float radius)
    {
        formationRadius = Mathf.Max(1f, radius);
    }

    /// <summary>
    /// 대형 간격 설정
    /// </summary>
    public void SetFormationSpacing(float spacing)
    {
        formationSpacing = Mathf.Max(0.5f, spacing);
    }
    #endregion

    #region Squad Management
    /// <summary>
    /// 분대원 스폰
    /// </summary>
    public bool SpawnMember(SquadMemberData data)
    {
        if (squadMembers.Count >= maxSquadSize)
        {
            LogDebug("분대가 가득 찼습니다!");
            return false;
        }

        if (data == null || data.prefab == null)
        {
            Debug.LogError("[SquadManager] Invalid member data!");
            return false;
        }

        // 스폰 위치 결정
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position :
                          GetFormationPosition(squadMembers.Count);

        // 멤버 생성
        GameObject memberObj = Instantiate(data.prefab, spawnPos, Quaternion.identity);
        SquadMember member = memberObj.GetComponent<SquadMember>();

        if (member == null)
        {
            Debug.LogError("[SquadManager] Prefab does not have SquadMember component!");
            Destroy(memberObj);
            return false;
        }

        // 초기화
        member.InitializeFromData(data);
        return AddMember(member);
    }

    /// <summary>
    /// 분대원 추가
    /// </summary>
    public bool AddMember(SquadMember member)
    {
        if (squadMembers.Count >= maxSquadSize)
        {
            LogDebug("분대가 가득 찼습니다!");
            return false;
        }

        if (squadMembers.Contains(member))
        {
            LogDebug("이미 분대에 있는 멤버입니다!");
            return false;
        }

        squadMembers.Add(member);
        member.SetFormationIndex(squadMembers.Count - 1);

        // 현재 명령 적용
        ApplyCurrentCommandToMember(member);

        OnMemberAdded?.Invoke(member);
        OnSquadSizeChanged?.Invoke(squadMembers.Count, maxSquadSize);

        LogDebug($"분대원 추가: {member.name} (Index: {member.FormationIndex}, 현재 {squadMembers.Count}/{maxSquadSize})");

        return true;
    }

    /// <summary>
    /// 분대원 사망 처리
    /// </summary>
    public void OnMemberDeath(SquadMember member)
    {
        if (!squadMembers.Contains(member)) return;

        squadMembers.Remove(member);
        ReassignFormationIndices();

        OnMemberRemoved?.Invoke(member);
        OnSquadSizeChanged?.Invoke(squadMembers.Count, maxSquadSize);

        LogDebug($"분대원 사망: {member.name} (현재 {squadMembers.Count}/{maxSquadSize})");
    }

    /// <summary>
    /// 분대원 제거
    /// </summary>
    public void RemoveMember(SquadMember member)
    {
        if (!squadMembers.Contains(member)) return;

        squadMembers.Remove(member);
        ReassignFormationIndices();

        OnMemberRemoved?.Invoke(member);
        OnSquadSizeChanged?.Invoke(squadMembers.Count, maxSquadSize);

        LogDebug($"분대원 제거: {member.name}");
    }

    /// <summary>
    /// 대형 인덱스 재할당
    /// </summary>
    private void ReassignFormationIndices()
    {
        for (int i = 0; i < squadMembers.Count; i++)
        {
            squadMembers[i].SetFormationIndex(i);
        }
    }

    /// <summary>
    /// 현재 명령을 특정 멤버에게 적용
    /// </summary>
    private void ApplyCurrentCommandToMember(SquadMember member)
    {
        switch (currentCommand)
        {
            case SquadCommand.Follow:
                member.SetCommand(SquadMember.MemberCommand.Follow);
                break;
            case SquadCommand.Attack:
                member.SetCommand(SquadMember.MemberCommand.Attack, attackTargetPosition, attackTarget);
                break;
            case SquadCommand.Hold:
                member.SetCommand(SquadMember.MemberCommand.Hold);
                break;
        }
    }
    #endregion

    #region Squad Upgrades
    /// <summary>
    /// 최대 분대 크기 증가
    /// </summary>
    public void IncreaseMaxSquadSize(int amount)
    {
        maxSquadSize += amount;
        OnSquadSizeChanged?.Invoke(squadMembers.Count, maxSquadSize);
        LogDebug($"최대 분대 크기 증가: {maxSquadSize}");
    }

    /// <summary>
    /// 모든 분대원에게 업그레이드 적용
    /// </summary>
    public void ApplyUpgradeToAll(UpgradeType type, float value)
    {
        foreach (var member in squadMembers)
        {
            if (member != null && member.IsAlive)
            {
                member.ApplyUpgrade(type, value);
            }
        }
        LogDebug($"전체 업그레이드: {type} +{value}");
    }
    #endregion

    #region Utility
    /// <summary>
    /// 특정 위치에서 가장 가까운 분대원
    /// </summary>
    public SquadMember GetClosestMemberTo(Vector3 position)
    {
        SquadMember closest = null;
        float closestDist = float.MaxValue;

        foreach (var member in squadMembers)
        {
            if (member == null || !member.IsAlive) continue;

            float dist = Vector3.Distance(position, member.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = member;
            }
        }

        return closest;
    }

    /// <summary>
    /// 특정 범위 내 분대원 목록
    /// </summary>
    public List<SquadMember> GetMembersInRange(Vector3 position, float range)
    {
        List<SquadMember> inRange = new List<SquadMember>();

        foreach (var member in squadMembers)
        {
            if (member == null || !member.IsAlive) continue;

            if (Vector3.Distance(position, member.transform.position) <= range)
            {
                inRange.Add(member);
            }
        }

        return inRange;
    }

    /// <summary>
    /// 살아있는 분대원 수
    /// </summary>
    public int GetAliveMemberCount()
    {
        int count = 0;
        foreach (var member in squadMembers)
        {
            if (member != null && member.IsAlive)
                count++;
        }
        return count;
    }

    /// <summary>
    /// 모든 분대원 체력 회복
    /// </summary>
    public void HealAllMembers(float amount)
    {
        foreach (var member in squadMembers)
        {
            if (member != null && member.IsAlive)
            {
                var health = member.GetComponent<SquadMemberHealth>();
                if (health != null)
                {
                    health.Heal(amount);
                }
            }
        }
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SquadManager] {message}");
        }
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (!showFormationGizmos) return;
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) return;

        int count = squadMembers.Count > 0 ? squadMembers.Count : maxSquadSize;

        // SquadFormation에서 색상 가져오기
        Color formationColor = SquadFormation.GetFormationColor(ConvertToSquadFormationType(formationType));

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetFormationPosition(i);

            // 활성 멤버는 진한 색, 빈 슬롯은 연한 색
            bool isActiveSlot = i < squadMembers.Count;
            Gizmos.color = isActiveSlot ? formationColor : new Color(formationColor.r, formationColor.g, formationColor.b, 0.3f);

            // 포메이션 위치 표시
            Gizmos.DrawWireSphere(pos, 0.5f);
            Gizmos.DrawSphere(pos, 0.15f);

            // 플레이어와 연결선
            Gizmos.color = new Color(formationColor.r, formationColor.g, formationColor.b, 0.5f);
            Gizmos.DrawLine(player.position, pos);

            // 인덱스 표시 (위쪽 라인)
            Gizmos.color = Color.white;
            Gizmos.DrawLine(pos, pos + Vector3.up * 0.5f);
        }

        // 대형 반경
        Gizmos.color = new Color(formationColor.r, formationColor.g, formationColor.b, 0.5f);
        DrawGizmoCircle(player.position, formationRadius, 32);

        // 플레이어 전방 표시
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(player.position, player.forward * formationRadius);

        // 공격 타겟 표시
        if (currentCommand == SquadCommand.Attack)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackTargetPosition, 1f);
            Gizmos.DrawLine(player.position, attackTargetPosition);
        }
    }

    /// <summary>
    /// Gizmos용 원 그리기
    /// </summary>
    private void DrawGizmoCircle(Vector3 center, float radius, int segments)
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
}
