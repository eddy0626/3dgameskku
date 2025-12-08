using UnityEngine;

/// <summary>
/// 무기 데이터를 저장하는 ScriptableObject
/// Project 창에서 Create > Weapons > Weapon Data로 생성 가능
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("기본 정보")]
    public string weaponName = "New Weapon";
    public Sprite weaponIcon;
    
    [Header("발사 설정")]
    [Tooltip("초당 발사 속도")]
    public float fireRate = 10f;
    
    [Tooltip("발사 데미지")]
    public float damage = 25f;
    
    [Tooltip("최대 사거리")]
    public float range = 100f;
    
    [Tooltip("발사 모드: 단발, 연발, 점사")]
    public FireMode fireMode = FireMode.Auto;
    
    [Header("탄약 설정")]
    [Tooltip("탄창 최대 탄약")]
    public int magazineSize = 30;
    
    [Tooltip("총 최대 탄약")]
    public int maxAmmo = 120;
    
    [Tooltip("재장전 시간 (초)")]
    public float reloadTime = 2f;
    
    [Header("반동 설정")]
    [Tooltip("수직 반동")]
    public float verticalRecoil = 1f;
    
    [Tooltip("수평 반동 (랜덤 범위)")]
    public float horizontalRecoil = 0.5f;
    
    [Tooltip("반동 회복 속도")]
    public float recoilRecoverySpeed = 5f;
    
    [Header("조준 설정 (ADS)")]
    [Tooltip("조준 시 이동할 위치")]
    public Vector3 adsPosition = new Vector3(0f, -0.1f, 0.1f);
    
    [Tooltip("조준 전환 속도")]
    public float adsSpeed = 10f;
    
    [Header("발사체 설정")]
    [Tooltip("발사 타입: Hitscan(즉시 적중) 또는 Projectile(물리 발사체)")]
    public FireType fireType = FireType.Hitscan;
    
    [Tooltip("발사체 프리팹 (Projectile 타입 시 필요)")]
    public GameObject projectilePrefab;
    
    [Tooltip("발사체 속도 (m/s)")]
    public float projectileSpeed = 100f;
    
    [Tooltip("발사체 중력 사용")]
    public bool projectileUseGravity = false;
    
    [Header("이펙트")]
    public GameObject muzzleFlashPrefab;
    public GameObject bulletHolePrefab;
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
}

/// <summary>
/// 발사 모드 열거형
/// </summary>
/// <summary>
/// 발사 타입 열거형
/// </summary>
public enum FireType
{
    Hitscan,    // Raycast 즉시 적중
    Projectile  // 물리 발사체
}

public enum FireMode
{
    Semi,   // 단발
    Auto,   // 연발
    Burst   // 점사 (3발)
}
