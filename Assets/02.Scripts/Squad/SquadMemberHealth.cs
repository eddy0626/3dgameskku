using UnityEngine;
using System;

/// <summary>
/// Health component for squad members implementing IDamageable
/// </summary>
public class SquadMemberHealth : MonoBehaviour, IDamageable
{
    #region Events
    public event Action<float, float> OnHealthChanged;
    public event Action<float> OnDamaged;
    public event Action OnDeath;
    #endregion

    #region Private Fields
    private float currentHealth;
    private float maxHealth;
    private bool isDead;
    private SquadMember squadMember;
    #endregion

    #region IDamageable Implementation
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => !isDead && currentHealth > 0;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        squadMember = GetComponent<SquadMember>();
    }
    #endregion

    #region Initialization
    public void Initialize(float max)
    {
        maxHealth = max;
        currentHealth = maxHealth;
        isDead = false;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetMaxHealth(float newMax, bool healToFull = false)
    {
        float healthPercent = currentHealth / maxHealth;
        maxHealth = newMax;
        
        if (healToFull)
            currentHealth = maxHealth;
        else
            currentHealth = maxHealth * healthPercent;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    #endregion

    #region IDamageable Methods
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        OnDamaged?.Invoke(damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Simplified overload for internal use
    public void TakeDamageSimple(float damage)
    {
        TakeDamage(damage, transform.position, Vector3.up);
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    #endregion

    #region Death
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        OnDeath?.Invoke();
        
        if (squadMember != null)
        {
            squadMember.OnDeath();
        }
    }
    #endregion

    #region Public Methods
    public void Revive(float healthPercent = 1f)
    {
        isDead = false;
        currentHealth = maxHealth * healthPercent;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
    #endregion
}