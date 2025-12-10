using UnityEngine;
using UnityEditor;

/// <summary>
/// WeaponData ScriptableObject 에셋 자동 생성 에디터 도구
/// 기본 무기 프리셋을 생성합니다.
/// </summary>
public class WeaponDataCreator : Editor
{
    private const string WEAPON_DATA_PATH = "Assets/09.ScriptableObjects/WeaponData/";
    
    [MenuItem("Tools/Weapons/Create All Weapon Data Assets")]
    public static void CreateAllWeaponData()
    {
        CreateAssaultRifle();
        CreateSMG();
        CreatePistol();
        CreateShotgun();
        CreateSniperRifle();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[WeaponDataCreator] 모든 무기 데이터 에셋이 생성되었습니다!");
    }
    
    [MenuItem("Tools/Weapons/Create Assault Rifle Data")]
    public static void CreateAssaultRifle()
    {
        WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
        
        // 기본 정보
        data.weaponName = "K2 Assault Rifle";
        
        // 발사 설정
        data.fireRate = 12f;
        data.damage = 28f;
        data.range = 100f;
        data.fireMode = FireMode.Auto;
        
        // 탄약 설정
        data.magazineSize = 30;
        data.maxAmmo = 150;
        data.reloadTime = 2.2f;
        
        // 카메라 반동
        data.verticalRecoil = 1.2f;
        data.horizontalRecoil = 0.4f;
        data.recoilSnappiness = 7f;
        data.recoilRecoverySpeed = 6f;
        data.maxVerticalRecoil = 10f;
        data.maxHorizontalRecoil = 4f;
        
        // 크로스헤어 확산
        data.crosshairSpreadPerShot = 4f;
        data.maxCrosshairSpread = 25f;
        data.crosshairRecoverySpeed = 12f;
        data.baseCrosshairSize = 20f;
        
        // 총기 킥백
        data.gunKickbackDistance = 0.03f;
        data.gunKickbackRotation = 4f;
        data.gunKickRecoverySpeed = 15f;
        
        // 탄퍼짐 설정 (중간 수준)
        data.baseBulletSpread = 0.4f;
        data.bulletSpreadPerShot = 0.25f;
        data.maxBulletSpread = 6f;
        data.bulletSpreadRecoverySpeed = 8f;
        data.movementSpreadMultiplier = 1.4f;
        data.airborneSpreadMultiplier = 2.2f;
        data.crouchSpreadMultiplier = 0.75f;
        data.adsSpreadMultiplier = 0.35f;
        
        // 화면 흔들림
        data.screenShakeIntensity = 0.015f;
        data.screenShakeDuration = 0.04f;
        
        // ADS 설정
        data.adsPosition = new Vector3(0f, -0.08f, 0.12f);
        data.adsSpeed = 10f;
        data.adsRecoilMultiplier = 0.65f;
        
        // 발사체 설정
        data.fireType = FireType.Hitscan;
        data.projectileSpeed = 200f;
        data.projectileUseGravity = false;
        
        SaveAsset(data, "AssaultRifle_K2");
    }
    
    [MenuItem("Tools/Weapons/Create SMG Data")]
    public static void CreateSMG()
    {
        WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
        
        // 기본 정보
        data.weaponName = "MP5 SMG";
        
        // 발사 설정 (높은 연사력, 낮은 데미지)
        data.fireRate = 16f;
        data.damage = 18f;
        data.range = 60f;
        data.fireMode = FireMode.Auto;
        
        // 탄약 설정
        data.magazineSize = 35;
        data.maxAmmo = 175;
        data.reloadTime = 1.8f;
        
        // 카메라 반동 (낮음)
        data.verticalRecoil = 0.7f;
        data.horizontalRecoil = 0.35f;
        data.recoilSnappiness = 9f;
        data.recoilRecoverySpeed = 8f;
        data.maxVerticalRecoil = 7f;
        data.maxHorizontalRecoil = 3f;
        
        // 크로스헤어 확산 (빠른 회복)
        data.crosshairSpreadPerShot = 3f;
        data.maxCrosshairSpread = 20f;
        data.crosshairRecoverySpeed = 18f;
        data.baseCrosshairSize = 22f;
        
        // 총기 킥백 (가벼움)
        data.gunKickbackDistance = 0.02f;
        data.gunKickbackRotation = 3f;
        data.gunKickRecoverySpeed = 18f;
        
        // 탄퍼짐 설정 (넓지만 빠른 회복)
        data.baseBulletSpread = 0.6f;
        data.bulletSpreadPerShot = 0.2f;
        data.maxBulletSpread = 7f;
        data.bulletSpreadRecoverySpeed = 12f;
        data.movementSpreadMultiplier = 1.2f;
        data.airborneSpreadMultiplier = 2.0f;
        data.crouchSpreadMultiplier = 0.8f;
        data.adsSpreadMultiplier = 0.4f;
        
        // 화면 흔들림 (미미)
        data.screenShakeIntensity = 0.01f;
        data.screenShakeDuration = 0.03f;
        
        // ADS 설정
        data.adsPosition = new Vector3(0f, -0.06f, 0.1f);
        data.adsSpeed = 12f;
        data.adsRecoilMultiplier = 0.7f;
        
        // 발사체 설정
        data.fireType = FireType.Hitscan;
        data.projectileSpeed = 150f;
        data.projectileUseGravity = false;
        
        SaveAsset(data, "SMG_MP5");
    }
    
    [MenuItem("Tools/Weapons/Create Pistol Data")]
    public static void CreatePistol()
    {
        WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
        
        // 기본 정보
        data.weaponName = "K5 Pistol";
        
        // 발사 설정 (단발, 중간 데미지)
        data.fireRate = 6f;
        data.damage = 35f;
        data.range = 50f;
        data.fireMode = FireMode.Semi;
        
        // 탄약 설정
        data.magazineSize = 15;
        data.maxAmmo = 75;
        data.reloadTime = 1.5f;
        
        // 카메라 반동 (중간)
        data.verticalRecoil = 1.8f;
        data.horizontalRecoil = 0.3f;
        data.recoilSnappiness = 10f;
        data.recoilRecoverySpeed = 10f;
        data.maxVerticalRecoil = 8f;
        data.maxHorizontalRecoil = 3f;
        
        // 크로스헤어 확산
        data.crosshairSpreadPerShot = 6f;
        data.maxCrosshairSpread = 28f;
        data.crosshairRecoverySpeed = 20f;
        data.baseCrosshairSize = 18f;
        
        // 총기 킥백
        data.gunKickbackDistance = 0.04f;
        data.gunKickbackRotation = 6f;
        data.gunKickRecoverySpeed = 20f;
        
        // 탄퍼짐 설정 (정확하지만 빠른 연사 시 증가)
        data.baseBulletSpread = 0.3f;
        data.bulletSpreadPerShot = 0.4f;
        data.maxBulletSpread = 5f;
        data.bulletSpreadRecoverySpeed = 15f;
        data.movementSpreadMultiplier = 1.3f;
        data.airborneSpreadMultiplier = 2.5f;
        data.crouchSpreadMultiplier = 0.7f;
        data.adsSpreadMultiplier = 0.25f;
        
        // 화면 흔들림
        data.screenShakeIntensity = 0.02f;
        data.screenShakeDuration = 0.05f;
        
        // ADS 설정
        data.adsPosition = new Vector3(0f, -0.05f, 0.08f);
        data.adsSpeed = 15f;
        data.adsRecoilMultiplier = 0.6f;
        
        // 발사체 설정
        data.fireType = FireType.Hitscan;
        data.projectileSpeed = 120f;
        data.projectileUseGravity = false;
        
        SaveAsset(data, "Pistol_K5");
    }
    
    [MenuItem("Tools/Weapons/Create Shotgun Data")]
    public static void CreateShotgun()
    {
        WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
        
        // 기본 정보
        data.weaponName = "M870 Shotgun";
        
        // 발사 설정 (단발, 높은 데미지, 짧은 사거리)
        data.fireRate = 1.2f;
        data.damage = 15f; // 펠릿당 데미지 (8발 = 120)
        data.range = 30f;
        data.fireMode = FireMode.Semi;
        
        // 탄약 설정
        data.magazineSize = 8;
        data.maxAmmo = 32;
        data.reloadTime = 0.6f; // 1발당 장전시간
        
        // 카메라 반동 (강함)
        data.verticalRecoil = 3.5f;
        data.horizontalRecoil = 1.0f;
        data.recoilSnappiness = 8f;
        data.recoilRecoverySpeed = 4f;
        data.maxVerticalRecoil = 15f;
        data.maxHorizontalRecoil = 6f;
        
        // 크로스헤어 확산 (큼)
        data.crosshairSpreadPerShot = 15f;
        data.maxCrosshairSpread = 50f;
        data.crosshairRecoverySpeed = 10f;
        data.baseCrosshairSize = 30f;
        
        // 총기 킥백 (강함)
        data.gunKickbackDistance = 0.08f;
        data.gunKickbackRotation = 10f;
        data.gunKickRecoverySpeed = 8f;
        
        // 탄퍼짐 설정 (넓은 산탄 패턴)
        data.baseBulletSpread = 3.0f;
        data.bulletSpreadPerShot = 0.5f;
        data.maxBulletSpread = 12f;
        data.bulletSpreadRecoverySpeed = 6f;
        data.movementSpreadMultiplier = 1.6f;
        data.airborneSpreadMultiplier = 3.0f;
        data.crouchSpreadMultiplier = 0.8f;
        data.adsSpreadMultiplier = 0.5f;
        
        // 화면 흔들림 (강함)
        data.screenShakeIntensity = 0.05f;
        data.screenShakeDuration = 0.08f;
        
        // ADS 설정
        data.adsPosition = new Vector3(0f, -0.1f, 0.15f);
        data.adsSpeed = 8f;
        data.adsRecoilMultiplier = 0.8f;
        
        // 발사체 설정
        data.fireType = FireType.Hitscan;
        data.projectileSpeed = 80f;
        data.projectileUseGravity = false;
        
        SaveAsset(data, "Shotgun_M870");
    }
    
    [MenuItem("Tools/Weapons/Create Sniper Rifle Data")]
    public static void CreateSniperRifle()
    {
        WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
        
        // 기본 정보
        data.weaponName = "K14 Sniper Rifle";
        
        // 발사 설정 (단발, 높은 데미지, 긴 사거리)
        data.fireRate = 0.8f;
        data.damage = 120f;
        data.range = 300f;
        data.fireMode = FireMode.Semi;
        
        // 탄약 설정
        data.magazineSize = 5;
        data.maxAmmo = 25;
        data.reloadTime = 3.0f;
        
        // 카메라 반동 (매우 강함)
        data.verticalRecoil = 4.0f;
        data.horizontalRecoil = 0.5f;
        data.recoilSnappiness = 6f;
        data.recoilRecoverySpeed = 3f;
        data.maxVerticalRecoil = 18f;
        data.maxHorizontalRecoil = 4f;
        
        // 크로스헤어 확산
        data.crosshairSpreadPerShot = 20f;
        data.maxCrosshairSpread = 60f;
        data.crosshairRecoverySpeed = 8f;
        data.baseCrosshairSize = 25f;
        
        // 총기 킥백 (매우 강함)
        data.gunKickbackDistance = 0.1f;
        data.gunKickbackRotation = 12f;
        data.gunKickRecoverySpeed = 6f;
        
        // 탄퍼짐 설정 (ADS 없이는 부정확, ADS 시 정확)
        data.baseBulletSpread = 2.0f;
        data.bulletSpreadPerShot = 1.0f;
        data.maxBulletSpread = 10f;
        data.bulletSpreadRecoverySpeed = 4f;
        data.movementSpreadMultiplier = 2.0f;
        data.airborneSpreadMultiplier = 4.0f;
        data.crouchSpreadMultiplier = 0.6f;
        data.adsSpreadMultiplier = 0.1f; // 조준 시 매우 정확
        
        // 화면 흔들림 (강함)
        data.screenShakeIntensity = 0.04f;
        data.screenShakeDuration = 0.1f;
        
        // ADS 설정 (스코프)
        data.adsPosition = new Vector3(0f, -0.12f, 0.2f);
        data.adsSpeed = 6f;
        data.adsRecoilMultiplier = 0.5f;
        
        // 발사체 설정
        data.fireType = FireType.Hitscan;
        data.projectileSpeed = 400f;
        data.projectileUseGravity = false;
        
        SaveAsset(data, "SniperRifle_K14");
    }
    
private static void SaveAsset(WeaponData data, string fileName)
    {
        string path = WEAPON_DATA_PATH + fileName + ".asset";
        
        // 에셋 이름 설정 (Unity 내부 이름)
        data.name = fileName;
        
        // 기존 에셋 확인
        WeaponData existing = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
        if (existing != null)
        {
            // 기존 에셋 업데이트
            EditorUtility.CopySerialized(data, existing);
            existing.name = fileName;
            EditorUtility.SetDirty(existing);
            Debug.Log($"[WeaponDataCreator] 업데이트: {fileName}");
        }
        else
        {
            // 새 에셋 생성
            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"[WeaponDataCreator] 생성: {fileName}");
        }
    }
}
