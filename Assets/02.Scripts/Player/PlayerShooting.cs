using UnityEngine;
using UnityEngine.EventSystems;


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
        // 게임 상태가 Playing이 아니면 입력 무시
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
        {
            return;
        }
        
        if (_currentWeapon == null) return;
        
        HandleFireInput();
        HandleReloadInput();
    }
    
    /// <summary>
    /// 발사 입력 처리
    /// </summary>
private void HandleFireInput()
    {
        if (_currentWeapon == null) return;
        
        // UI 위에 마우스가 있으면 발사 차단
        if (IsPointerOverUI())
        {
            _currentWeapon.SetTriggerState(false);
            return;
        }
        
        // 트리거 상태 전달 (마우스 버튼 누름 여부)
        bool isMouseHeld = Input.GetMouseButton(0);
        _currentWeapon.SetTriggerState(isMouseHeld);
        
        // 마우스 버튼 누르고 있으면 발사 시도
        // 발사 모드별 처리는 FirearmWeapon.TryFire()에서 담당
        if (isMouseHeld)
        {
            _currentWeapon.TryFire();
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



    /// <summary>
    /// UI 위에 마우스 포인터가 있는지 확인
    /// </summary>
    private bool IsPointerOverUI()
    {
        // 현재 EventSystem이 있는지 확인
        if (EventSystem.current == null) return false;
        
        // 마우스 포인터가 UI 요소 위에 있는지 확인
        return EventSystem.current.IsPointerOverGameObject();
    }
}
