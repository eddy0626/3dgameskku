using UnityEngine;

/// <summary>
/// 모든 무기의 기본 클래스 (추상 클래스)
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("무기 데이터")]
    [SerializeField] protected WeaponData _weaponData;
    
    [Header("참조")]
    [SerializeField] protected Transform _muzzlePoint;
    [SerializeField] protected AudioSource _audioSource;
    
    // 현재 탄약 상태
    protected int _currentMagazine;
    protected int _currentAmmo;
    
    // 발사 타이밍
    protected float _nextFireTime;
    
    // 재장전 상태
    protected bool _isReloading;
    
    // 프로퍼티
    public WeaponData WeaponData => _weaponData;
    public int CurrentMagazine => _currentMagazine;
    public int CurrentAmmo => _currentAmmo;
    public bool IsReloading => _isReloading;
    public bool CanFire => !_isReloading && _currentMagazine > 0 && Time.time >= _nextFireTime;
    
protected virtual void Awake()
    {
        // AudioSource 자동 찾기/추가
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // MuzzlePoint 자동 찾기
        if (_muzzlePoint == null)
        {
            _muzzlePoint = transform.Find("MuzzlePoint");
            if (_muzzlePoint == null)
            {
                // 자식 중에서 MuzzlePoint 이름을 가진 오브젝트 찾기
                foreach (Transform child in GetComponentsInChildren<Transform>())
                {
                    if (child.name.Contains("Muzzle") || child.name.Contains("muzzle"))
                    {
                        _muzzlePoint = child;
                        break;
                    }
                }
            }
        }
    }
    
protected virtual void Start()
    {
        InitializeAmmo();
        
        // 시작 시 무기 장착 처리 (RecoilSystem에 무기 데이터 전달)
        OnWeaponEquip();
    }
    
    /// <summary>
    /// 탄약 초기화
    /// </summary>
    protected virtual void InitializeAmmo()
    {
        if (_weaponData != null)
        {
            _currentMagazine = _weaponData.magazineSize;
            _currentAmmo = _weaponData.maxAmmo;
        }
    }
    
    /// <summary>
    /// 발사 시도 (파생 클래스에서 구현)
    /// </summary>
    public abstract void TryFire();
    
    /// <summary>
    /// 실제 발사 처리 (파생 클래스에서 구현)
    /// </summary>
    protected abstract void Fire();
    
    /// <summary>
    /// 재장전
    /// </summary>
    public virtual void Reload()
    {
        if (_isReloading) return;
        if (_currentMagazine >= _weaponData.magazineSize) return;
        if (_currentAmmo <= 0) return;
        
        StartCoroutine(ReloadCoroutine());
    }
    
    /// <summary>
    /// 재장전 코루틴
    /// </summary>
    protected virtual System.Collections.IEnumerator ReloadCoroutine()
    {
        _isReloading = true;
        
        // 재장전 사운드 재생
        PlaySound(_weaponData.reloadSound);
        
        // 재장전 대기
        yield return new WaitForSeconds(_weaponData.reloadTime);
        
        // 탄약 계산
        int ammoNeeded = _weaponData.magazineSize - _currentMagazine;
        int ammoToLoad = Mathf.Min(ammoNeeded, _currentAmmo);
        
        _currentMagazine += ammoToLoad;
        _currentAmmo -= ammoToLoad;
        
        _isReloading = false;
    }
    
    /// <summary>
    /// 탄약 소모
    /// </summary>
    protected virtual void ConsumeAmmo()
    {
        _currentMagazine--;
        _nextFireTime = Time.time + (1f / _weaponData.fireRate);
    }
    
    /// <summary>
    /// 사운드 재생
    /// </summary>
    protected void PlaySound(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// 탄창이 비었을 때
    /// </summary>
    protected void PlayEmptySound()
    {
        PlaySound(_weaponData.emptySound);
    }
    
    /// <summary>
    /// 탄약 추가 (픽업 등)
    /// </summary>
    public void AddAmmo(int amount)
    {
        _currentAmmo = Mathf.Min(_currentAmmo + amount, _weaponData.maxAmmo);
    }
    
    /// <summary>
    /// 무기 활성화/비활성화 시 호출
    /// </summary>
    public virtual void OnWeaponEquip() { }
    public virtual void OnWeaponUnequip() { }
}
