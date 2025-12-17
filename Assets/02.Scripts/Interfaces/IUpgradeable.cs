/// <summary>
/// Interface for entities that can be upgraded
/// </summary>
public interface IUpgradeable
{
    /// <summary>
    /// Apply an upgrade to this entity
    /// </summary>
    /// <param name="type">Type of upgrade</param>
    /// <param name="value">Value to apply</param>
    /// <param name="isMultiplicative">True if value is multiplicative, false for additive</param>
    void ApplyUpgrade(UpgradeType type, float value, bool isMultiplicative = false);
}

/// <summary>
/// Types of upgrades available in the game
/// </summary>
public enum UpgradeType
{
    Damage,
    Health,
    Speed,
    AttackSpeed,
    AttackRange,
    MagnetRange,
    SquadSize,
    CriticalChance,
    CriticalDamage
}

/// <summary>
/// Target for upgrade application
/// </summary>
public enum UpgradeTarget
{
    Player,
    Squad,
    All
}