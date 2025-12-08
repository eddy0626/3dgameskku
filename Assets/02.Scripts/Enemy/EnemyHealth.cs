using System;
using UnityEngine;

/// <summary>
/// 적 체력 관리 컴포넌트
/// IDamageable 인터페이스 구현
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("체력 설정")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _currentHealth;
    
    [Header("피격 이펙트")]
    [SerializeField] private GameObject _hitEffectPrefab;
    [SerializeField] private GameObject _deathEffectPrefab;
    [SerializeField] private float _hitEffectDuration = 1f;
    
    [Header("피격 반응")]
    [SerializeField] private float _hitFlashDuration = 0.1f;
    [SerializeField] private Color _hitFlashColor = Color.red;
    
    // 프로퍼티
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public bool IsAlive => _currentHealth > 0f;
    public float HealthPercent => _currentHealth / _maxHealth;
    
    // 이벤트
    public event Action<float, float> OnHealthChanged;  // (currentHealth, maxHealth)
    public event Action<float, Vector3> OnDamaged;      // (damage, hitPoint)
    public event Action OnDeath;
    
    // 내부 변수
    private Renderer[] _renderers;
    private Color[] _originalColors;
    private bool _isFlashing;
    
    private void Awake()
    {
        _currentHealth = _maxHealth;
        CacheRenderers();
    }
    
    /// <summary>
    /// 렌더러 캐싱 (피격 플래시용)
    /// </summary>
    private void CacheRenderers()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _originalColors = new Color[_renderers.Length];
        
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i].material.HasProperty("_Color"))
            {
                _originalColors[i] = _renderers[i].material.color;
            }
        }
    }
    
    /// <summary>
    /// 데미지 처리
    /// </summary>
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!IsAlive) return;
        if (damage <= 0f) return;
        
        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        
        // 이벤트 발생
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        OnDamaged?.Invoke(damage, hitPoint);
        
        // 피격 이펙트
        SpawnHitEffect(hitPoint, hitNormal);
        StartHitFlash();
        
        Debug.Log($"[EnemyHealth] {gameObject.name} took {damage} damage. HP: {_currentHealth}/{_maxHealth}");
        
        // 사망 체크
        if (!IsAlive)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 체력 회복
    /// </summary>
    public void Heal(float amount)
    {
        if (!IsAlive) return;
        if (amount <= 0f) return;
        
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        
        Debug.Log($"[EnemyHealth] {gameObject.name} healed {amount}. HP: {_currentHealth}/{_maxHealth}");
    }
    
    /// <summary>
    /// 체력 초기화 (리스폰용)
    /// </summary>
    public void ResetHealth()
    {
        _currentHealth = _maxHealth;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }
    
    /// <summary>
    /// 최대 체력 설정
    /// </summary>
    public void SetMaxHealth(float newMaxHealth, bool resetCurrent = true)
    {
        _maxHealth = Mathf.Max(1f, newMaxHealth);
        if (resetCurrent)
        {
            _currentHealth = _maxHealth;
        }
        else
        {
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
        }
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }
    
    /// <summary>
    /// 피격 이펙트 생성
    /// </summary>
    private void SpawnHitEffect(Vector3 position, Vector3 normal)
    {
        if (_hitEffectPrefab == null) return;
        
        GameObject effect = Instantiate(
            _hitEffectPrefab,
            position,
            Quaternion.LookRotation(normal)
        );
        Destroy(effect, _hitEffectDuration);
    }
    
    /// <summary>
    /// 피격 플래시 시작
    /// </summary>
    private void StartHitFlash()
    {
        if (_isFlashing) return;
        StartCoroutine(HitFlashCoroutine());
    }
    
    /// <summary>
    /// 피격 플래시 코루틴
    /// </summary>
    private System.Collections.IEnumerator HitFlashCoroutine()
    {
        _isFlashing = true;
        
        // 빨간색으로 변경
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _renderers[i].material.HasProperty("_Color"))
            {
                _renderers[i].material.color = _hitFlashColor;
            }
        }
        
        yield return new WaitForSeconds(_hitFlashDuration);
        
        // 원래 색상 복구
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _renderers[i].material.HasProperty("_Color"))
            {
                _renderers[i].material.color = _originalColors[i];
            }
        }
        
        _isFlashing = false;
    }
    
    /// <summary>
    /// 사망 처리
    /// </summary>
    private void Die()
    {
        Debug.Log($"[EnemyHealth] {gameObject.name} died!");
        
        // 사망 이벤트 발생
        OnDeath?.Invoke();
        
        // 사망 이펙트
        if (_deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                _deathEffectPrefab,
                transform.position,
                Quaternion.identity
            );
            Destroy(effect, 3f);
        }
        
        // 오브젝트 제거 (나중에 Object Pool로 변경 권장)
        Destroy(gameObject, 0.1f);
    }
    
    /// <summary>
    /// 즉시 사망 (강제 킬)
    /// </summary>
    public void InstantKill()
    {
        if (!IsAlive) return;
        
        _currentHealth = 0f;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        Die();
    }
}
