using UnityEngine;

/// <summary>
/// Projectile fired by squad members
/// </summary>
public class SquadProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private GameObject hitEffect;
    
    private float damage;
    private float speed;
    private Transform target;
    private Vector3 direction;
    private bool isInitialized;

    public void Initialize(float damage, float speed, Transform target = null)
    {
        this.damage = damage;
        this.speed = speed;
        this.target = target;
        
        if (target != null)
        {
            direction = (target.position - transform.position).normalized;
        }
        else
        {
            direction = transform.forward;
        }
        
        isInitialized = true;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!isInitialized) return;
        
        // 타겟 추적 (선택적)
        if (target != null && target.gameObject.activeInHierarchy)
        {
            direction = Vector3.Lerp(
                direction, 
                (target.position - transform.position).normalized, 
                Time.deltaTime * 2f
            );
        }
        
        transform.position += direction * speed * Time.deltaTime;
        transform.forward = direction;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 아군 무시
        if (other.CompareTag("Player") || other.GetComponent<SquadMember>() != null)
            return;
        
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            Vector3 hitNormal = -transform.forward;
            damageable.TakeDamage(damage, transform.position, hitNormal);
        }
        
        // 히트 이펙트
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject);
    }
}