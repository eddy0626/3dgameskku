using UnityEngine;
using System;

/// <summary>
/// 플레이어 체력 관리 컴포넌트
/// IDamageable 인터페이스 구현
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    #region Inspector Fields
    
    [Header("체력 설정")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _currentHealth;
    
    [Header("무적 설정")]
    [SerializeField] private float _invincibilityDuration = 0.5f;
    [SerializeField] private bool _canTakeDamage = true;
    
    [Header("피격 효과")]
    [SerializeField] private AudioClip _hitSound;
    [SerializeField] private AudioClip _deathSound;
    
    #endregion
    
    #region Private Fields
    
    private AudioSource _audioSource;
    private float _invincibilityTimer;
    
    #endregion
    
    #region Events
    
    public event Action<float, float> OnHealthChanged;  // current, max
    public event Action<float> OnDamageTaken;           // damage amount
    public event Action OnDeath;
    public event Action OnRevive;
    
    #endregion
    
    #region Properties (IDamageable)
    
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public bool IsAlive => _currentHealth > 0f;
    public float HealthPercent => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
    
    #endregion
    
    #region Unity Callbacks
    
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void Start()
    {
        // 체력 초기화
        _currentHealth = _maxHealth;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }
    
    private void Update()
    {
        // 무적 타이머 업데이트
        if (!_canTakeDamage)
        {
            _invincibilityTimer -= Time.deltaTime;
            if (_invincibilityTimer <= 0f)
            {
                _canTakeDamage = true;
            }
        }
    }
    
    #endregion
    
    #region IDamageable Implementation
    
    /// <summary>
    /// 데미지 처리
    /// </summary>
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!IsAlive || !_canTakeDamage) return;
        
        // 데미지 적용
        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        
        Debug.Log($"[PlayerHealth] Took {damage} damage! Health: {_currentHealth}/{_maxHealth}");
        
        // 이벤트 발생
        OnDamageTaken?.Invoke(damage);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        
        // 피격 사운드
        PlaySound(_hitSound);
        
        // 무적 시간 설정
        if (_invincibilityDuration > 0f)
        {
            _canTakeDamage = false;
            _invincibilityTimer = _invincibilityDuration;
        }
        
        // 사망 체크
        if (_currentHealth <= 0f)
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
        
        float previousHealth = _currentHealth;
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        
        float actualHeal = _currentHealth - previousHealth;
        
        if (actualHeal > 0f)
        {
            Debug.Log($"[PlayerHealth] Healed {actualHeal}! Health: {_currentHealth}/{_maxHealth}");
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }
    
    #endregion
    
    #region Health Management
    
    /// <summary>
    /// 사망 처리
    /// </summary>
    private void Die()
    {
        Debug.Log("[PlayerHealth] Player died!");
        
        PlaySound(_deathSound);
        OnDeath?.Invoke();
        
        // 여기에 사망 처리 로직 추가
        // 예: 게임 오버 화면, 리스폰 등
    }
    
    /// <summary>
    /// 부활
    /// </summary>
    public void Revive(float healthPercent = 1f)
    {
        _currentHealth = _maxHealth * Mathf.Clamp01(healthPercent);
        _canTakeDamage = true;
        
        Debug.Log($"[PlayerHealth] Revived with {_currentHealth} health!");
        
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        OnRevive?.Invoke();
    }
    
    /// <summary>
    /// 체력 설정 (최대 체력 변경)
    /// </summary>
    public void SetMaxHealth(float newMaxHealth, bool healToFull = false)
    {
        _maxHealth = Mathf.Max(1f, newMaxHealth);
        
        if (healToFull)
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
    /// 즉시 사망 (함정 등)
    /// </summary>
    public void InstantKill()
    {
        _currentHealth = 0f;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        Die();
    }
    
    #endregion
    
    #region Helper Methods
    
    private void PlaySound(AudioClip clip)
    {
        if (clip == null || _audioSource == null) return;
        _audioSource.PlayOneShot(clip);
    }
    
    #endregion
}
