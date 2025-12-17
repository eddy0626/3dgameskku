using UnityEngine;

/// <summary>
/// Interface for collectible items (resources, powerups, etc.)
/// </summary>
public interface ICollectable
{
    /// <summary>
    /// Called when the item is collected
    /// </summary>
    /// <param name="collector">The transform that collected this item</param>
    void OnCollect(Transform collector);
}