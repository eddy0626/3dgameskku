using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 탄약 UI 컨트롤러
/// 현재 탄창과 보유 탄약을 화면에 표시
/// </summary>
public class AmmoUIController : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TMP_Text _magazineText;      // 탄창 텍스트 (30/30)
    [SerializeField] private TMP_Text _totalAmmoText;     // 보유 탄약 텍스트 (| 120)
    [SerializeField] private GameObject _reloadingIndicator; // 재장전 표시
    
    [Header("애니메이션 설정")]
    [SerializeField] private float _punchScale = 1.2f;
    [SerializeField] private float _punchDuration = 0.15f;
    [SerializeField] private Color _lowAmmoColor = new Color(1f, 0.3f, 0.3f); // 빨간색
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private int _lowAmmoThreshold = 5; // 이하면 경고 색상
    
    // 참조
    private PlayerShooting _playerShooting;
    private WeaponBase _currentWeapon;
    
    // 캐시
    private int _lastMagazine = -1;
    private int _lastAmmo = -1;
    private bool _lastReloading = false;
    
private void Start()
    {
        FindUIReferences();
        FindReferences();
        
        if (_reloadingIndicator != null)
        {
            _reloadingIndicator.SetActive(false);
        }
    }

/// <summary>
    /// UI 참조 자동 찾기
    /// </summary>
    private void FindUIReferences()
    {
        if (_magazineText == null)
        {
            Transform magTransform = transform.Find("MagazineText");
            if (magTransform != null)
            {
                _magazineText = magTransform.GetComponent<TMP_Text>();
            }
        }
        
        if (_totalAmmoText == null)
        {
            Transform totalTransform = transform.Find("TotalAmmoText");
            if (totalTransform != null)
            {
                _totalAmmoText = totalTransform.GetComponent<TMP_Text>();
            }
        }
        
        if (_reloadingIndicator == null)
        {
            Transform reloadTransform = transform.Find("ReloadingIndicator");
            if (reloadTransform != null)
            {
                _reloadingIndicator = reloadTransform.gameObject;
            }
        }
    }

    
    /// <summary>
    /// 참조 자동 찾기
    /// </summary>
    private void FindReferences()
    {
        if (_playerShooting == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerShooting = player.GetComponent<PlayerShooting>();
            }
        }
    }
    
    private void Update()
    {
        if (_playerShooting == null)
        {
            FindReferences();
            return;
        }
        
        // 현재 무기 가져오기
        _currentWeapon = _playerShooting.GetCurrentWeapon();
        
        if (_currentWeapon == null) return;
        
        UpdateAmmoDisplay();
        UpdateReloadingState();
    }
    
    /// <summary>
    /// 탄약 표시 업데이트
    /// </summary>
    private void UpdateAmmoDisplay()
    {
        int currentMag = _currentWeapon.CurrentMagazine;
        int currentAmmo = _currentWeapon.CurrentAmmo;
        int maxMag = _currentWeapon.WeaponData != null ? _currentWeapon.WeaponData.magazineSize : 30;
        
        // 탄창 변경 시 업데이트
        if (currentMag != _lastMagazine)
        {
            UpdateMagazineText(currentMag, maxMag);
            
            // 탄약이 줄었을 때 펀치 효과
            if (_lastMagazine > 0 && currentMag < _lastMagazine)
            {
                PlayFireAnimation();
            }
            
            _lastMagazine = currentMag;
        }
        
        // 보유 탄약 변경 시 업데이트
        if (currentAmmo != _lastAmmo)
        {
            UpdateTotalAmmoText(currentAmmo);
            _lastAmmo = currentAmmo;
        }
    }
    
    /// <summary>
    /// 탄창 텍스트 업데이트
    /// </summary>
    private void UpdateMagazineText(int current, int max)
    {
        if (_magazineText == null) return;
        
        _magazineText.text = $"{current} / {max}";
        
        // 탄약 부족 시 색상 변경
        if (current <= _lowAmmoThreshold)
        {
            _magazineText.color = _lowAmmoColor;
        }
        else
        {
            _magazineText.color = _normalColor;
        }
    }
    
    /// <summary>
    /// 보유 탄약 텍스트 업데이트
    /// </summary>
    private void UpdateTotalAmmoText(int total)
    {
        if (_totalAmmoText == null) return;
        
        _totalAmmoText.text = $"| {total}";
        
        // 보유 탄약 부족 시 색상 변경
        if (total <= 0)
        {
            _totalAmmoText.color = _lowAmmoColor;
        }
        else
        {
            _totalAmmoText.color = _normalColor;
        }
    }
    
    /// <summary>
    /// 발사 시 애니메이션
    /// </summary>
    private void PlayFireAnimation()
    {
        if (_magazineText == null) return;
        
        // DOTween으로 펀치 스케일 효과
        _magazineText.transform.DOKill();
        _magazineText.transform.localScale = Vector3.one;
        _magazineText.transform.DOPunchScale(Vector3.one * (_punchScale - 1f), _punchDuration, 1, 0.5f);
    }
    
    /// <summary>
    /// 재장전 상태 업데이트
    /// </summary>
    private void UpdateReloadingState()
    {
        bool isReloading = _currentWeapon.IsReloading;
        
        if (isReloading != _lastReloading)
        {
            if (_reloadingIndicator != null)
            {
                _reloadingIndicator.SetActive(isReloading);
            }
            
            // 재장전 시작 시 깜빡임 효과
            if (isReloading && _magazineText != null)
            {
                _magazineText.DOKill();
                _magazineText.DOFade(0.3f, 0.3f).SetLoops(-1, LoopType.Yoyo);
            }
            // 재장전 완료 시 효과 종료
            else if (!isReloading && _magazineText != null)
            {
                _magazineText.DOKill();
                _magazineText.DOFade(1f, 0.1f);
                
                // 재장전 완료 효과
                PlayReloadCompleteAnimation();
            }
            
            _lastReloading = isReloading;
        }
    }
    
    /// <summary>
    /// 재장전 완료 애니메이션
    /// </summary>
    private void PlayReloadCompleteAnimation()
    {
        if (_magazineText == null) return;
        
        _magazineText.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 2, 0.5f);
    }
    
    /// <summary>
    /// 무기 변경 시 호출
    /// </summary>
    public void OnWeaponChanged()
    {
        _lastMagazine = -1;
        _lastAmmo = -1;
        _lastReloading = false;
    }
}
