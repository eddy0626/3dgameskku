using UnityEngine;

/// <summary>
/// 데미지를 받을 수 있는 객체를 위한 인터페이스
/// 플레이어, 적, 파괴 가능 오브젝트 등에서 구현
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 현재 체력
    /// </summary>
    float CurrentHealth { get; }
    
    /// <summary>
    /// 최대 체력
    /// </summary>
    float MaxHealth { get; }
    
    /// <summary>
    /// 생존 여부
    /// </summary>
    bool IsAlive { get; }
    
    /// <summary>
    /// 데미지 처리
    /// </summary>
    /// <param name="damage">받은 데미지량</param>
    /// <param name="hitPoint">피격 위치</param>
    /// <param name="hitNormal">피격 방향</param>
    void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);
    
    /// <summary>
    /// 체력 회복
    /// </summary>
    /// <param name="amount">회복량</param>
    void Heal(float amount);
}
