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
    
    [Header("Animation")]
    [SerializeField] private Animator _animator;
    private static readonly int IsShootingHash = Animator.StringToHash("IsShooting");
    
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

        // Animator 자동 찾기 (Soldier_demo의 Animator 우선)
        if (_animator == null)
        {
            Transform soldierTransform = transform.Find("Soldier_demo");
            if (soldierTransform != null)
            {
                _animator = soldierTransform.GetComponent<Animator>();
            }
        }
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
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
            // UI 위에서는 사격 애니메이션 중지
            if (_animator != null)
            {
                _animator.SetBool(IsShootingHash, false);
            }
            return;
        }

        // 트리거 상태 전달 (마우스 버튼 누름 여부)
        bool isMouseHeld = Input.GetMouseButton(0);
        _currentWeapon.SetTriggerState(isMouseHeld);

        // 사격 애니메이션 파라미터 업데이트 (마우스 상태에 따라 true/false)
        if (_animator != null)
        {
            bool prevShooting = _animator.GetBool(IsShootingHash);
            _animator.SetBool(IsShootingHash, isMouseHeld);

            // 상태 변경 시 로그 출력
            if (prevShooting != isMouseHeld)
            {
                Debug.Log($"[Animation] IsShooting: {isMouseHeld}");
            }
        }

        // 마우스 버튼 누르고 있으면 발사 시도
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
