using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Component that attracts nearby resource drops (coin magnet effect)
/// Attach to player or any object that should collect resources
/// </summary>
public class ResourceMagnet : MonoBehaviour
{
    #region Inspector Fields
    [Header("Magnet Settings")]
    [SerializeField] private float magnetRange = 5f;
    [SerializeField] private float collectRange = 1f;
    [SerializeField] private LayerMask resourceLayer = -1; // Everything by default
    
    [Header("Filters")]
    [SerializeField] private bool collectAll = true;
    [SerializeField] private List<ResourceType> collectTypes;
    
    [Header("Performance")]
    [SerializeField] private float checkInterval = 0.1f;
    
    [Header("Upgrade")]
    [SerializeField] private float bonusRange = 0f;
    #endregion

    #region Private Fields
    private float lastCheckTime;
    private HashSet<ResourceDrop> attractedDrops = new HashSet<ResourceDrop>();
    #endregion

    #region Properties
    public float TotalMagnetRange => magnetRange + bonusRange;
    public float CollectRange => collectRange;
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        // 성능을 위해 일정 간격으로만 체크
        if (Time.time - lastCheckTime < checkInterval) return;
        lastCheckTime = Time.time;
        
        CheckForResources();
        CheckForCollection();
    }

    private void OnDisable()
    {
        // 비활성화 시 끌어당기던 드롭 해제
        foreach (var drop in attractedDrops)
        {
            if (drop != null)
            {
                drop.StopMagnet();
            }
        }
        attractedDrops.Clear();
    }
    #endregion

    #region Magnet Logic
    private void CheckForResources()
    {
        float totalRange = TotalMagnetRange;
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, totalRange, resourceLayer);
        
        foreach (var col in colliders)
        {
            if (!col.TryGetComponent<ResourceDrop>(out var drop)) continue;
            
            // 이미 끌어당기는 중이면 스킵
            if (attractedDrops.Contains(drop)) continue;
            
            // 필터 체크
            if (!ShouldCollect(drop.Type)) continue;
            
            // 마그넷 시작
            drop.StartMagnet(transform);
            attractedDrops.Add(drop);
        }
        
        // 파괴된 드롭 정리
        attractedDrops.RemoveWhere(d => d == null);
    }

    private void CheckForCollection()
    {
        // 즉시 수집 범위 체크
        Collider[] colliders = Physics.OverlapSphere(transform.position, collectRange, resourceLayer);
        
        foreach (var col in colliders)
        {
            if (col.TryGetComponent<ResourceDrop>(out var drop))
            {
                if (ShouldCollect(drop.Type))
                {
                    drop.OnCollect(transform);
                    attractedDrops.Remove(drop);
                }
            }
        }
    }

    private bool ShouldCollect(ResourceType type)
    {
        if (collectAll) return true;
        return collectTypes.Contains(type);
    }
    #endregion

    #region Public Methods
    public void SetMagnetRange(float range)
    {
        magnetRange = range;
    }

    public void AddBonusRange(float bonus)
    {
        bonusRange += bonus;
    }

    public void SetCollectTypes(List<ResourceType> types)
    {
        collectAll = false;
        collectTypes = types;
    }

    public void SetCollectAll(bool all)
    {
        collectAll = all;
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        // 마그넷 범위 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRange + bonusRange);
        
        // 수집 범위 (녹색)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectRange);
    }
    #endregion
}