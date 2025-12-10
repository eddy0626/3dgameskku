using UnityEngine;

/// <summary>
/// 플레이어 사격 제어 스크립트
/// 마우스 입력을 받아 무기 발사/재장전 처리
/// </summary>
public class PlayerShooting : MonoBehaviour
{
    [Header("무기 설정")]
    [SerializeField] private FirearmWeapon _currentWeapon;
    
    [Header("입력 설정")]
    [SerializeField] private KeyCode _reloadKey = KeyCode.R;
    
    // 캐시
    private WeaponData _weaponData;
    private bool _isFiring;
    
private void Start()
    {
        // 무기 자동 찾기
        if (_currentWeapon == null)
        {
            // 먼저 자식에서 찾기
            _currentWeapon = GetComponentInChildren<FirearmWeapon>();
            
            // 없으면 GunHolder에서 찾기
            if (_currentWeapon == null)
            {
                Transform gunHolder = Camera.main?.transform.Find("GunHolder");
                if (gunHolder != null)
                {
                    _currentWeapon = gunHolder.GetComponentInChildren<FirearmWeapon>();
                }
            }
            
            // 그래도 없으면 씬 전체에서 찾기
            if (_currentWeapon == null)
            {
                _currentWeapon = FindFirstObjectByType<FirearmWeapon>();
            }
        }
        
        if (_currentWeapon != null)
        {
            _weaponData = _currentWeapon.WeaponData;
            Debug.Log($"[PlayerShooting] Weapon found: {_currentWeapon.name}, WeaponData: {(_weaponData != null ? _weaponData.weaponName : "null")}");
        }
        else
        {
            Debug.LogWarning("[PlayerShooting] No weapon found!");
        }
    }
    
    private void Update()
    {
        if (_currentWeapon == null) return;
        
        HandleFireInput();
        HandleReloadInput();
    }
    
    /// <summary>
    /// 발사 입력 처리
    /// </summary>
    private void HandleFireInput()
    {
        if (_weaponData == null) return;
        
        switch (_weaponData.fireMode)
        {
            case FireMode.Auto:
                // 연발: 마우스 버튼 누르고 있는 동안 발사
                if (Input.GetMouseButton(0))
                {
                    _currentWeapon.TryFire();
                }
                break;
                
            case FireMode.Semi:
                // 단발: 클릭할 때마다 한 발
                if (Input.GetMouseButtonDown(0))
                {
                    _currentWeapon.TryFire();
                }
                break;
                
            case FireMode.Burst:
                // 점사: 클릭 시 3발 연속
                if (Input.GetMouseButtonDown(0))
                {
                    _currentWeapon.TryFire();
                }
                // 점사 중 자동 발사
                else if (_isFiring)
                {
                    _currentWeapon.TryFire();
                }
                break;
        }
    }
    
    /// <summary>
    /// 재장전 입력 처리
    /// </summary>
    private void HandleReloadInput()
    {
        if (Input.GetKeyDown(_reloadKey))
        {
            _currentWeapon.Reload();
        }
        
        // 탄창이 비면 자동 재장전
        if (_currentWeapon.CurrentMagazine <= 0 && !_currentWeapon.IsReloading)
        {
            _currentWeapon.Reload();
        }
    }
    
    /// <summary>
    /// 현재 무기 교체
    /// </summary>
    public void SetWeapon(FirearmWeapon newWeapon)
    {
        if (_currentWeapon != null)
        {
            _currentWeapon.OnWeaponUnequip();
        }
        
        _currentWeapon = newWeapon;
        
        if (_currentWeapon != null)
        {
            _weaponData = _currentWeapon.WeaponData;
            _currentWeapon.OnWeaponEquip();
        }
    }
    
    /// <summary>
    /// 현재 무기 반환
    /// </summary>
    public FirearmWeapon GetCurrentWeapon()
    {
        return _currentWeapon;
    }
}
