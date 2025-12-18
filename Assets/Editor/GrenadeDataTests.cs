using NUnit.Framework;
using UnityEngine;

/// <summary>
/// GrenadeData ScriptableObject 유닛 테스트
/// </summary>
[TestFixture]
public class GrenadeDataTests
{
    private GrenadeData _grenadeData;

    [SetUp]
    public void SetUp()
    {
        _grenadeData = ScriptableObject.CreateInstance<GrenadeData>();

        // 기본 테스트 값 설정
        _grenadeData.grenadeName = "Test Grenade";
        _grenadeData.maxDamage = 100f;
        _grenadeData.minDamage = 20f;
        _grenadeData.explosionRadius = 5f;
        _grenadeData.explosionForce = 500f;
        _grenadeData.fuseTime = 3f;
        _grenadeData.throwForce = 15f;
        _grenadeData.upwardForce = 5f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_grenadeData);
    }

    #region 데이터 유효성 테스트

    [Test]
    public void GrenadeData_MaxDamage_IsPositive()
    {
        // Assert
        Assert.Greater(_grenadeData.maxDamage, 0f);
    }

    [Test]
    public void GrenadeData_MinDamage_IsLessThanOrEqualToMax()
    {
        // Assert
        Assert.LessOrEqual(_grenadeData.minDamage, _grenadeData.maxDamage);
    }

    [Test]
    public void GrenadeData_ExplosionRadius_IsPositive()
    {
        // Assert
        Assert.Greater(_grenadeData.explosionRadius, 0f);
    }

    [Test]
    public void GrenadeData_FuseTime_IsPositive()
    {
        // Assert
        Assert.Greater(_grenadeData.fuseTime, 0f);
    }

    #endregion

    #region 데미지 계산 테스트

    [Test]
    public void DamageCalculation_AtCenter_ReturnsMaxDamage()
    {
        // Arrange
        float distance = 0f;
        float normalizedDistance = distance / _grenadeData.explosionRadius;

        // Act
        float damage = Mathf.Lerp(_grenadeData.maxDamage, _grenadeData.minDamage, normalizedDistance);

        // Assert
        Assert.AreEqual(_grenadeData.maxDamage, damage, 0.001f);
    }

    [Test]
    public void DamageCalculation_AtEdge_ReturnsMinDamage()
    {
        // Arrange
        float distance = _grenadeData.explosionRadius;
        float normalizedDistance = distance / _grenadeData.explosionRadius;

        // Act
        float damage = Mathf.Lerp(_grenadeData.maxDamage, _grenadeData.minDamage, normalizedDistance);

        // Assert
        Assert.AreEqual(_grenadeData.minDamage, damage, 0.001f);
    }

    [Test]
    public void DamageCalculation_AtHalfRadius_ReturnsMiddleDamage()
    {
        // Arrange
        float distance = _grenadeData.explosionRadius * 0.5f;
        float normalizedDistance = distance / _grenadeData.explosionRadius;

        // Act
        float damage = Mathf.Lerp(_grenadeData.maxDamage, _grenadeData.minDamage, normalizedDistance);

        // Assert
        float expectedDamage = (_grenadeData.maxDamage + _grenadeData.minDamage) / 2f;
        Assert.AreEqual(expectedDamage, damage, 0.001f);
    }

    [Test]
    public void DamageCalculation_BeyondRadius_ClampedToMinDamage()
    {
        // Arrange
        float distance = _grenadeData.explosionRadius * 2f;
        float normalizedDistance = Mathf.Clamp01(distance / _grenadeData.explosionRadius);

        // Act
        float damage = Mathf.Lerp(_grenadeData.maxDamage, _grenadeData.minDamage, normalizedDistance);

        // Assert
        Assert.AreEqual(_grenadeData.minDamage, damage, 0.001f);
    }

    [Test]
    [TestCase(0f, 100f)]
    [TestCase(1.25f, 80f)]
    [TestCase(2.5f, 60f)]
    [TestCase(3.75f, 40f)]
    [TestCase(5f, 20f)]
    public void DamageCalculation_AtVariousDistances_ReturnsCorrectDamage(float distance, float expectedDamage)
    {
        // Arrange
        float normalizedDistance = Mathf.Clamp01(distance / _grenadeData.explosionRadius);

        // Act
        float damage = Mathf.Lerp(_grenadeData.maxDamage, _grenadeData.minDamage, normalizedDistance);

        // Assert
        Assert.AreEqual(expectedDamage, damage, 0.01f);
    }

    #endregion

    #region 물리 계산 테스트

    [Test]
    public void ThrowForce_IsPositive()
    {
        // Assert
        Assert.Greater(_grenadeData.throwForce, 0f);
    }

    [Test]
    public void ExplosionForce_IsPositive()
    {
        // Assert
        Assert.Greater(_grenadeData.explosionForce, 0f);
    }

    [Test]
    public void ThrowVelocity_Calculation()
    {
        // Arrange
        Vector3 direction = Vector3.forward;

        // Act
        Vector3 throwVelocity = direction.normalized * _grenadeData.throwForce;
        throwVelocity.y += _grenadeData.upwardForce;

        // Assert
        Assert.AreEqual(_grenadeData.throwForce, throwVelocity.z, 0.001f);
        Assert.AreEqual(_grenadeData.upwardForce, throwVelocity.y, 0.001f);
    }

    #endregion
}
