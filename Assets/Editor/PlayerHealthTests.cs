using NUnit.Framework;
using UnityEngine;

/// <summary>
/// PlayerHealth 컴포넌트 유닛 테스트
/// </summary>
[TestFixture]
public class PlayerHealthTests
{
    private GameObject _playerObject;
    private PlayerHealth _playerHealth;

    [SetUp]
    public void SetUp()
    {
        _playerObject = new GameObject("TestPlayer");
        _playerHealth = _playerObject.AddComponent<PlayerHealth>();

        // EditMode에서는 Start()가 자동 호출되지 않으므로 수동 초기화
        // SetMaxHealth를 호출하여 체력 초기화
        _playerHealth.SetMaxHealth(100f, true);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_playerObject);
    }

    #region 초기화 테스트

    [Test]
    public void PlayerHealth_InitialHealth_EqualsMaxHealth()
    {
        // Assert
        Assert.AreEqual(_playerHealth.MaxHealth, _playerHealth.CurrentHealth);
    }

    [Test]
    public void PlayerHealth_InitialState_IsAlive()
    {
        // Assert
        Assert.IsTrue(_playerHealth.IsAlive);
    }

    [Test]
    public void PlayerHealth_HealthPercent_IsOneAtFullHealth()
    {
        // Assert
        Assert.AreEqual(1f, _playerHealth.HealthPercent, 0.001f);
    }

    #endregion

    #region 데미지 테스트

    [Test]
    public void TakeDamage_ReducesHealth()
    {
        // Arrange
        float initialHealth = _playerHealth.CurrentHealth;
        float damage = 25f;

        // Act
        _playerHealth.TakeDamage(damage, Vector3.zero, Vector3.up);

        // Assert
        Assert.AreEqual(initialHealth - damage, _playerHealth.CurrentHealth, 0.001f);
    }

    [Test]
    public void TakeDamage_ZeroDamage_NoHealthChange()
    {
        // Arrange
        float initialHealth = _playerHealth.CurrentHealth;

        // Act
        _playerHealth.TakeDamage(0f, Vector3.zero, Vector3.up);

        // Assert
        Assert.AreEqual(initialHealth, _playerHealth.CurrentHealth);
    }

    [Test]
    public void TakeDamage_NegativeDamage_HealsPlayer()
    {
        // Arrange
        float initialHealth = _playerHealth.CurrentHealth;

        // Act - 음수 데미지는 힐링으로 작용
        _playerHealth.TakeDamage(-10f, Vector3.zero, Vector3.up);

        // Assert - 체력이 증가함
        Assert.AreEqual(initialHealth + 10f, _playerHealth.CurrentHealth);
    }

    [Test]
    public void TakeDamage_HealthNeverBelowZero()
    {
        // Arrange
        float excessiveDamage = _playerHealth.MaxHealth * 2f;

        // Act
        _playerHealth.TakeDamage(excessiveDamage, Vector3.zero, Vector3.up);

        // Assert
        Assert.AreEqual(0f, _playerHealth.CurrentHealth);
        Assert.GreaterOrEqual(_playerHealth.CurrentHealth, 0f);
    }

    [Test]
    public void TakeDamage_FiresOnDamageTakenEvent()
    {
        // Arrange
        bool eventFired = false;
        float receivedDamage = 0f;
        _playerHealth.OnDamageTaken += (damage) =>
        {
            eventFired = true;
            receivedDamage = damage;
        };

        // Act
        _playerHealth.TakeDamage(30f, Vector3.zero, Vector3.up);

        // Assert
        Assert.IsTrue(eventFired);
        Assert.AreEqual(30f, receivedDamage, 0.001f);
    }

    [Test]
    public void TakeDamage_FiresOnHealthChangedEvent()
    {
        // Arrange
        bool eventFired = false;
        float currentHealth = 0f;
        float maxHealth = 0f;
        _playerHealth.OnHealthChanged += (current, max) =>
        {
            eventFired = true;
            currentHealth = current;
            maxHealth = max;
        };

        // Act
        _playerHealth.TakeDamage(20f, Vector3.zero, Vector3.up);

        // Assert
        Assert.IsTrue(eventFired);
        Assert.AreEqual(_playerHealth.CurrentHealth, currentHealth, 0.001f);
        Assert.AreEqual(_playerHealth.MaxHealth, maxHealth, 0.001f);
    }

    #endregion

    #region 사망 테스트

    [Test]
    public void TakeDamage_LethalDamage_PlayerDies()
    {
        // Arrange
        float lethalDamage = _playerHealth.MaxHealth;

        // Act
        _playerHealth.TakeDamage(lethalDamage, Vector3.zero, Vector3.up);

        // Assert
        Assert.IsFalse(_playerHealth.IsAlive);
        Assert.AreEqual(0f, _playerHealth.CurrentHealth);
    }

    [Test]
    public void TakeDamage_LethalDamage_FiresOnDeathEvent()
    {
        // Arrange
        bool deathEventFired = false;
        _playerHealth.OnDeath += () => deathEventFired = true;

        // Act
        _playerHealth.TakeDamage(_playerHealth.MaxHealth, Vector3.zero, Vector3.up);

        // Assert
        Assert.IsTrue(deathEventFired);
    }

    [Test]
    public void TakeDamage_WhenDead_NoDamageApplied()
    {
        // Arrange
        _playerHealth.TakeDamage(_playerHealth.MaxHealth, Vector3.zero, Vector3.up);
        Assert.IsFalse(_playerHealth.IsAlive);

        // Act
        _playerHealth.TakeDamage(50f, Vector3.zero, Vector3.up);

        // Assert
        Assert.AreEqual(0f, _playerHealth.CurrentHealth);
    }

    [Test]
    public void InstantKill_KillsPlayer()
    {
        // Act
        _playerHealth.InstantKill();

        // Assert
        Assert.IsFalse(_playerHealth.IsAlive);
        Assert.AreEqual(0f, _playerHealth.CurrentHealth);
    }

    #endregion

    #region 회복 테스트

    [Test]
    public void Heal_IncreasesHealth()
    {
        // Arrange
        _playerHealth.TakeDamage(50f, Vector3.zero, Vector3.up);
        float healthAfterDamage = _playerHealth.CurrentHealth;

        // Act
        _playerHealth.Heal(25f);

        // Assert
        Assert.AreEqual(healthAfterDamage + 25f, _playerHealth.CurrentHealth, 0.001f);
    }

    [Test]
    public void Heal_DoesNotExceedMaxHealth()
    {
        // Arrange
        _playerHealth.TakeDamage(10f, Vector3.zero, Vector3.up);

        // Act
        _playerHealth.Heal(100f);

        // Assert
        Assert.AreEqual(_playerHealth.MaxHealth, _playerHealth.CurrentHealth, 0.001f);
    }

    [Test]
    public void Heal_WhenDead_NoHealing()
    {
        // Arrange
        _playerHealth.InstantKill();
        Assert.IsFalse(_playerHealth.IsAlive);

        // Act
        _playerHealth.Heal(50f);

        // Assert
        Assert.AreEqual(0f, _playerHealth.CurrentHealth);
    }

    [Test]
    public void Heal_ZeroAmount_NoChange()
    {
        // Arrange
        _playerHealth.TakeDamage(30f, Vector3.zero, Vector3.up);
        float healthBefore = _playerHealth.CurrentHealth;

        // Act
        _playerHealth.Heal(0f);

        // Assert
        Assert.AreEqual(healthBefore, _playerHealth.CurrentHealth);
    }

    #endregion

    #region 부활 테스트

    [Test]
    public void Revive_RestoresHealth()
    {
        // Arrange
        _playerHealth.InstantKill();

        // Act
        _playerHealth.Revive(1f);

        // Assert
        Assert.IsTrue(_playerHealth.IsAlive);
        Assert.AreEqual(_playerHealth.MaxHealth, _playerHealth.CurrentHealth, 0.001f);
    }

    [Test]
    public void Revive_PartialHealth_RestoresCorrectAmount()
    {
        // Arrange
        _playerHealth.InstantKill();

        // Act
        _playerHealth.Revive(0.5f);

        // Assert
        Assert.IsTrue(_playerHealth.IsAlive);
        Assert.AreEqual(_playerHealth.MaxHealth * 0.5f, _playerHealth.CurrentHealth, 0.001f);
    }

    [Test]
    public void Revive_FiresOnReviveEvent()
    {
        // Arrange
        bool reviveEventFired = false;
        _playerHealth.OnRevive += () => reviveEventFired = true;
        _playerHealth.InstantKill();

        // Act
        _playerHealth.Revive(1f);

        // Assert
        Assert.IsTrue(reviveEventFired);
    }

    #endregion

    #region 최대 체력 변경 테스트

    [Test]
    public void SetMaxHealth_UpdatesMaxHealth()
    {
        // Act
        _playerHealth.SetMaxHealth(200f, true);

        // Assert
        Assert.AreEqual(200f, _playerHealth.MaxHealth, 0.001f);
    }

    [Test]
    public void SetMaxHealth_WithHealToFull_RestoresHealth()
    {
        // Arrange
        _playerHealth.TakeDamage(50f, Vector3.zero, Vector3.up);

        // Act
        _playerHealth.SetMaxHealth(150f, true);

        // Assert
        Assert.AreEqual(150f, _playerHealth.CurrentHealth, 0.001f);
    }

    [Test]
    public void SetMaxHealth_WithoutHealToFull_CapsCurrentHealth()
    {
        // Arrange - 현재 체력 50
        _playerHealth.TakeDamage(50f, Vector3.zero, Vector3.up);
        float currentHealth = _playerHealth.CurrentHealth;

        // Act - 최대 체력을 40으로 낮춤
        _playerHealth.SetMaxHealth(40f, false);

        // Assert - 현재 체력이 새 최대치로 제한됨
        Assert.AreEqual(40f, _playerHealth.CurrentHealth, 0.001f);
    }

    [Test]
    public void SetMaxHealth_MinimumIsOne()
    {
        // Act
        _playerHealth.SetMaxHealth(0f, true);

        // Assert
        Assert.AreEqual(1f, _playerHealth.MaxHealth, 0.001f);
    }

    #endregion
}
