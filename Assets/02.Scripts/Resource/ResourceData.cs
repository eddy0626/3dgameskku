using UnityEngine;

/// <summary>
/// ScriptableObject containing resource drop configuration
/// </summary>
[CreateAssetMenu(fileName = "ResourceData", menuName = "Game/Resource/Resource Data")]
public class ResourceData : ScriptableObject
{
    [Header("Basic Info")]
    public string resourceName;
    public ResourceType type;
    public Sprite icon;
    public GameObject prefab;
    
    [Header("Value")]
    public int baseAmount = 1;
    public int minAmount = 1;
    public int maxAmount = 5;
    public bool randomizeAmount = false;
    
    [Header("Magnet")]
    public float magnetSpeed = 15f;
    public float autoCollectDelay = 0f; // 0이면 자동 수집 안함
    
    [Header("Visual")]
    public float bobHeight = 0.2f;
    public float bobSpeed = 2f;
    public float rotateSpeed = 90f;
    public Color glowColor = Color.yellow;
    
    [Header("Audio")]
    public AudioClip collectSound;
    
    [Header("Lifetime")]
    public float lifetime = 30f; // 0이면 무한
    
    public int GetAmount()
    {
        if (randomizeAmount)
        {
            return Random.Range(minAmount, maxAmount + 1);
        }
        return baseAmount;
    }
}