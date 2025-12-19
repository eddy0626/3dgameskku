using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using SquadSurvival.Core;

namespace SquadSurvival.Squad
{
    /// <summary>
    /// 분대 전체 관리 컨트롤러
    /// 분대원 생성, 관리, 명령 전달
    /// </summary>
    public class SquadController : MonoBehaviour
    {
        public static SquadController Instance { get; private set; }

        [Header("분대원 설정")]
        [SerializeField] private GameObject squadMemberPrefab;
        [SerializeField] private int maxSquadSize = 4;
        [SerializeField] private string[] memberNames = { "Alpha", "Bravo", "Charlie", "Delta" };

        [Header("스폰 설정")]
        [SerializeField] private float spawnRadius = 3f;
        [SerializeField] private float spawnHeightOffset = 0.5f;

        [Header("분대 행동")]
        [SerializeField] private float formationSpacing = 2f;
        [SerializeField] private SquadFormation currentFormation = SquadFormation.Diamond;

        [Header("이벤트")]
        public UnityEvent<SquadMember> OnMemberAdded;
        public UnityEvent<SquadMember> OnMemberDeath;
        public UnityEvent OnSquadWiped;

        public enum SquadFormation
        {
            Line,       // 일렬
            Diamond,    // 다이아몬드
            Wedge,      // 쐐기
            Column      // 종대
        }

        // 분대원 리스트
        private List<SquadMember> squadMembers = new List<SquadMember>();
        private Transform playerTransform;

        // 프로퍼티
        public List<SquadMember> SquadMembers => squadMembers;
        public int AliveCount => squadMembers.FindAll(m => m != null && m.IsAlive).Count;
        public int TotalCount => squadMembers.Count;
        public bool IsSquadAlive => AliveCount > 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (OnMemberAdded == null) OnMemberAdded = new UnityEvent<SquadMember>();
            if (OnMemberDeath == null) OnMemberDeath = new UnityEvent<SquadMember>();
            if (OnSquadWiped == null) OnSquadWiped = new UnityEvent();
        }

        private void Start()
        {
            // 플레이어 참조
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }

            // GameModeManager 이벤트 구독
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.OnModeChanged.AddListener(OnGameModeChanged);
            }
        }

        /// <summary>
        /// 게임 모드 변경 시 호출
        /// </summary>
        private void OnGameModeChanged(GameModeManager.GameMode mode)
        {
            if (mode == GameModeManager.GameMode.SquadSurvival)
            {
                SpawnSquad();
            }
            else
            {
                DespawnSquad();
            }
        }

        /// <summary>
        /// 분대 생성
        /// </summary>
        public void SpawnSquad()
        {
            if (squadMemberPrefab == null)
            {
                Debug.LogWarning("[SquadController] 분대원 프리팹이 설정되지 않았습니다.");
                return;
            }

            // 기존 분대 제거
            DespawnSquad();

            // 새 분대원 생성
            for (int i = 0; i < maxSquadSize; i++)
            {
                SpawnMember(i);
            }

#if UNITY_EDITOR
            Debug.Log($"[SquadController] 분대 생성 완료: {squadMembers.Count}명");
#endif
        }

        /// <summary>
        /// 분대원 생성
        /// </summary>
        private SquadMember SpawnMember(int index)
        {
            Vector3 spawnPos = GetFormationPosition(index);
            Quaternion spawnRot = playerTransform != null 
                ? Quaternion.LookRotation(playerTransform.forward) 
                : Quaternion.identity;

            GameObject memberObj = Instantiate(squadMemberPrefab, spawnPos, spawnRot);
            memberObj.name = $"SquadMember_{index}_{GetMemberName(index)}";

            SquadMember member = memberObj.GetComponent<SquadMember>();
            if (member == null)
            {
                member = memberObj.AddComponent<SquadMember>();
            }

            member.Initialize(index, GetMemberName(index), playerTransform);
            member.OnDeath.AddListener(() => OnMemberDied(member));

            squadMembers.Add(member);
            OnMemberAdded?.Invoke(member);

            return member;
        }

        /// <summary>
        /// 분대원 이름 가져오기
        /// </summary>
        private string GetMemberName(int index)
        {
            if (memberNames != null && index < memberNames.Length)
            {
                return memberNames[index];
            }
            return $"Member {index + 1}";
        }

        /// <summary>
        /// 포메이션 위치 계산
        /// </summary>
        private Vector3 GetFormationPosition(int index)
        {
            Vector3 basePos = playerTransform != null ? playerTransform.position : transform.position;
            Vector3 offset = Vector3.zero;

            switch (currentFormation)
            {
                case SquadFormation.Line:
                    // 플레이어 뒤쪽 일렬
                    offset = new Vector3((index - 1.5f) * formationSpacing, 0f, -spawnRadius);
                    break;

                case SquadFormation.Diamond:
                    // 다이아몬드 형태
                    switch (index)
                    {
                        case 0: offset = new Vector3(0f, 0f, -spawnRadius); break;           // 뒤
                        case 1: offset = new Vector3(-spawnRadius, 0f, 0f); break;           // 왼쪽
                        case 2: offset = new Vector3(spawnRadius, 0f, 0f); break;            // 오른쪽
                        case 3: offset = new Vector3(0f, 0f, -spawnRadius * 2f); break;      // 맨 뒤
                    }
                    break;

                case SquadFormation.Wedge:
                    // 쐐기 형태 (V자)
                    float wedgeAngle = 30f;
                    float side = (index % 2 == 0) ? -1f : 1f;
                    int row = index / 2;
                    offset = new Vector3(
                        side * (row + 1) * formationSpacing * Mathf.Sin(wedgeAngle * Mathf.Deg2Rad),
                        0f,
                        -(row + 1) * formationSpacing * Mathf.Cos(wedgeAngle * Mathf.Deg2Rad)
                    );
                    break;

                case SquadFormation.Column:
                    // 종대 (일렬 뒤로)
                    offset = new Vector3(0f, 0f, -(index + 1) * formationSpacing);
                    break;
            }

            // 플레이어 방향 기준으로 회전
            if (playerTransform != null)
            {
                offset = playerTransform.rotation * offset;
            }

            Vector3 spawnPos = basePos + offset + Vector3.up * spawnHeightOffset;

            // NavMesh 위 위치 찾기
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }

            return spawnPos;
        }

        /// <summary>
        /// 분대 제거
        /// </summary>
        public void DespawnSquad()
        {
            foreach (var member in squadMembers)
            {
                if (member != null)
                {
                    Destroy(member.gameObject);
                }
            }
            squadMembers.Clear();
        }

        /// <summary>
        /// 분대원 사망 처리
        /// </summary>
        private void OnMemberDied(SquadMember member)
        {
            OnMemberDeath?.Invoke(member);

            // 전멸 체크
            if (AliveCount <= 0)
            {
                OnSquadWiped?.Invoke();

#if UNITY_EDITOR
                Debug.Log("[SquadController] 분대 전멸!");
#endif
            }
        }

        /// <summary>
        /// 특정 분대원 부활
        /// </summary>
        public void ReviveMember(int index, float healthPercent = 0.5f)
        {
            if (index < 0 || index >= squadMembers.Count) return;

            var member = squadMembers[index];
            if (member != null && !member.IsAlive)
            {
                member.Revive(healthPercent);
            }
        }

        /// <summary>
        /// 모든 분대원 부활
        /// </summary>
        public void ReviveAllMembers(float healthPercent = 0.5f)
        {
            foreach (var member in squadMembers)
            {
                if (member != null && !member.IsAlive)
                {
                    member.Revive(healthPercent);
                }
            }
        }

        /// <summary>
        /// 모든 분대원 회복
        /// </summary>
        public void HealAllMembers(float amount)
        {
            foreach (var member in squadMembers)
            {
                if (member != null && member.IsAlive)
                {
                    member.Heal(amount);
                }
            }
        }

        /// <summary>
        /// 포메이션 변경
        /// </summary>
        public void SetFormation(SquadFormation formation)
        {
            currentFormation = formation;

#if UNITY_EDITOR
            Debug.Log($"[SquadController] 포메이션 변경: {formation}");
#endif
        }

        /// <summary>
        /// 분대원 정보 가져오기
        /// </summary>
        public SquadMember GetMember(int index)
        {
            if (index < 0 || index >= squadMembers.Count) return null;
            return squadMembers[index];
        }

        /// <summary>
        /// 살아있는 분대원 목록
        /// </summary>
        public List<SquadMember> GetAliveMembers()
        {
            return squadMembers.FindAll(m => m != null && m.IsAlive);
        }

        /// <summary>
        /// 분대원 추가 (런타임)
        /// </summary>
        public SquadMember AddMember()
        {
            if (squadMembers.Count >= maxSquadSize)
            {
                Debug.LogWarning("[SquadController] 최대 분대 인원에 도달했습니다.");
                return null;
            }

            return SpawnMember(squadMembers.Count);
        }

        private void OnDestroy()
        {
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.OnModeChanged.RemoveListener(OnGameModeChanged);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 포메이션 위치 시각화
            Vector3 basePos = playerTransform != null ? playerTransform.position : transform.position;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < maxSquadSize; i++)
            {
                Vector3 pos = GetFormationPosition(i);
                Gizmos.DrawWireSphere(pos, 0.5f);
                Gizmos.DrawLine(basePos, pos);
            }
        }
#endif
    }
}
