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
    
    [Tooltip("무기 타입 (머즐플래시 이펙트 선택에 사용)")]
    public WeaponType weaponType = WeaponType.Rifle;
    
    
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
    
    [Header("반동 설정 - 카메라")]
    [Tooltip("수직 반동 (발사당)")]
    [Range(0f, 5f)]
    public float verticalRecoil = 1.5f;
    
    [Tooltip("수평 반동 랜덤 범위 (발사당)")]
    [Range(0f, 3f)]
    public float horizontalRecoil = 0.5f;
    
    [Tooltip("반동 스냅 속도 (반동이 적용되는 속도)")]
    [Range(1f, 20f)]
    public float recoilSnappiness = 6f;
    
    [Tooltip("반동 회복 속도")]
    [Range(1f, 20f)]
    public float recoilRecoverySpeed = 5f;
    
    [Tooltip("최대 수직 반동 누적량 (도)")]
    [Range(5f, 30f)]
    public float maxVerticalRecoil = 12f;
    
    [Tooltip("최대 수평 반동 누적량 (도)")]
    [Range(2f, 15f)]
    public float maxHorizontalRecoil = 5f;
    
    [Header("반동 설정 - 크로스헤어 확산")]
    [Tooltip("발사당 크로스헤어 확산량 (픽셀)")]
    [Range(0f, 20f)]
    public float crosshairSpreadPerShot = 5f;
    
    [Tooltip("최대 크로스헤어 확산 (픽셀)")]
    [Range(10f, 100f)]
    public float maxCrosshairSpread = 30f;
    
    [Tooltip("크로스헤어 확산 회복 속도")]
    [Range(5f, 30f)]
    public float crosshairRecoverySpeed = 15f;
    
    [Tooltip("기본 크로스헤어 크기 (픽셀)")]
    [Range(10f, 50f)]
    public float baseCrosshairSize = 20f;
    
    [Header("반동 설정 - 총기 킥백")]
    [Tooltip("총기 뒤로 밀림 거리")]
    [Range(0f, 0.2f)]
    public float gunKickbackDistance = 0.03f;
    
    [Tooltip("총기 상향 회전량 (도)")]
    [Range(0f, 15f)]
    public float gunKickbackRotation = 5f;
    
    [Tooltip("총기 킥백 회복 속도")]
    [Range(5f, 30f)]
    public float gunKickRecoverySpeed = 15f;
    
        [Header("탄퍼짐 설정 (Bullet Spread)")]
    [Tooltip("기본 탄퍼짐 각도 (조준하지 않은 상태, 정지 시)")]
    [Range(0f, 5f)]
    public float baseBulletSpread = 0.5f;
    
    [Tooltip("발사당 탄퍼짐 증가량 (도)")]
    [Range(0f, 2f)]
    public float bulletSpreadPerShot = 0.3f;
    
    [Tooltip("최대 탄퍼짐 각도")]
    [Range(1f, 15f)]
    public float maxBulletSpread = 8f;
    
    [Tooltip("탄퍼짐 회복 속도")]
    [Range(1f, 20f)]
    public float bulletSpreadRecoverySpeed = 10f;
    
    [Tooltip("이동 중 탄퍼짐 배율")]
    [Range(1f, 3f)]
    public float movementSpreadMultiplier = 1.5f;
    
    [Tooltip("공중(점프) 탄퍼짐 배율")]
    [Range(1.5f, 5f)]
    public float airborneSpreadMultiplier = 2.5f;
    
    [Tooltip("웅크리기 시 탄퍼짐 배율")]
    [Range(0.3f, 1f)]
    public float crouchSpreadMultiplier = 0.7f;
    
    [Tooltip("조준(ADS) 시 탄퍼짐 배율")]
    [Range(0.1f, 0.8f)]
    public float adsSpreadMultiplier = 0.3f;
    
[Header("반동 설정 - 화면 흔들림")]
    [Tooltip("화면 흔들림 강도 (0=없음)")]
    [Range(0f, 0.2f)]
    public float screenShakeIntensity = 0.02f;
    
    [Tooltip("화면 흔들림 지속시간 (초)")]
    [Range(0.01f, 0.2f)]
    public float screenShakeDuration = 0.05f;
    
    [Header("조준 설정 (ADS)")]
    [Tooltip("조준 시 이동할 위치")]
    public Vector3 adsPosition = new Vector3(0f, -0.1f, 0.1f);
    
    [Tooltip("조준 전환 속도")]
    public float adsSpeed = 10f;
    
    [Tooltip("조준 시 반동 감소 배율")]
    [Range(0.3f, 1f)]
    public float adsRecoilMultiplier = 0.6f;
    
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
/// 발사 타입 열거형
/// </summary>
public enum FireType
{
    Hitscan,    // Raycast 즉시 적중
    Projectile  // 물리 발사체
}

/// <summary>
/// 발사 모드 열거형
/// </summary>
public enum FireMode
{
    Semi,   // 단발
    Auto,   // 연발
    Burst   // 점사 (3발)
}
