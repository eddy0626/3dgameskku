using UnityEngine;

/// <summary>
/// 수류탄 데이터를 저장하는 ScriptableObject
/// Project 창에서 Create > Weapons > Grenade Data로 생성 가능
/// </summary>
[CreateAssetMenu(fileName = "NewGrenadeData", menuName = "Weapons/Grenade Data")]
public class GrenadeData : ScriptableObject
{
    [Header("기본 정보")]
    public string grenadeName = "Frag Grenade";
    public Sprite grenadeIcon;
    
    [Header("투척 설정")]
    [Tooltip("투척 힘")]
    public float throwForce = 20f;
    
    [Tooltip("위로 올리는 힘")]
    public float upwardForce = 5f;
    
    [Tooltip("투척 쿨다운")]
    public float throwCooldown = 1f;
    
    [Header("폭발 설정")]
    [Tooltip("폭발까지 시간 (초)")]
    public float fuseTime = 3f;
    
    [Tooltip("폭발 반경")]
    public float explosionRadius = 8f;
    
    [Tooltip("최대 데미지 (중심부)")]
    public float maxDamage = 150f;
    
    [Tooltip("최소 데미지 (외곽)")]
    public float minDamage = 25f;
    
    [Tooltip("폭발 물리력")]
    public float explosionForce = 700f;
    
    [Header("프리팹")]
    [Tooltip("수류탄 프리팹")]
    public GameObject grenadePrefab;
    
    [Tooltip("폭발 이펙트 프리팹")]
    public GameObject explosionPrefab;
    
    [Header("사운드")]
    public AudioClip throwSound;
    public AudioClip bounceSound;
    public AudioClip explosionSound;
    
    [Header("레이어 설정")]
    [Tooltip("데미지를 받는 레이어")]
    public LayerMask damageableLayers;
    
    [Tooltip("폭발 시 가려지는 레이어 (벽 등)")]
    public LayerMask obstacleLayers;
}
