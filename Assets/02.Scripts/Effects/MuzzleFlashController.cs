using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 머즐 플래시(총구 섬광) 이펙트 컨트롤러
/// War FX 에셋과 연동하여 다양한 총구 이펙트 지원
/// 오브젝트 풀링으로 성능 최적화
/// </summary>
public class MuzzleFlashController : MonoBehaviour
{
    public static MuzzleFlashController Instance { get; private set; }
    
    [Header("머즐 플래시 프리팹 설정")]
    [Tooltip("기본 머즐 플래시 프리팹 (War FX: WFX_MF FPS RIFLE1 권장)")]
    [SerializeField] private GameObject _defaultMuzzleFlashPrefab;
    
    [Tooltip("라이플 머즐 플래시 프리팹들")]
    [SerializeField] private GameObject[] _rifleMuzzleFlashPrefabs;
    
    [Tooltip("피스톨 머즐 플래시 프리팹")]
    [SerializeField] private GameObject _pistolMuzzleFlashPrefab;
    
    [Tooltip("샷건 머즐 플래시 프리팹")]
    [SerializeField] private GameObject _shotgunMuzzleFlashPrefab;
    
    [Header("추가 총구 이펙트")]
    [Tooltip("총구 연기 프리팹")]
    [SerializeField] private GameObject _muzzleSmokePrefab;
    
    [Tooltip("총구 스파크 프리팹")]
    [SerializeField] private GameObject _muzzleSparkPrefab;
    
    [Header("이펙트 설정")]
    [Tooltip("머즐 플래시 지속 시간")]
    [SerializeField] private float _flashDuration = 0.05f;
    
    [Tooltip("연기 지속 시간")]
    [SerializeField] private float _smokeDuration = 1f;
    
    [Tooltip("랜덤 회전 적용")]
    [SerializeField] private bool _randomRotation = true;
    
    [Tooltip("랜덤 스케일 범위")]
    [SerializeField] private Vector2 _scaleRange = new Vector2(0.8f, 1.2f);
    
    [Header("풀 설정")]
    [SerializeField] private int _initialPoolSize = 10;
    [SerializeField] private int _maxPoolSize = 30;
    
    // 오브젝트 풀
    private Dictionary<GameObject, Queue<GameObject>> _effectPools;
    private Transform _poolParent;
    
    // 무기 타입별 머즐플래시 인덱스 (순환용)
    private int _currentRifleMuzzleIndex = 0;
    
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            Initialize();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 초기화
    /// </summary>
    private void Initialize()
    {
        // 풀 부모 생성
        GameObject poolObj = new GameObject("MuzzleFlashPool");
        poolObj.transform.SetParent(transform);
        _poolParent = poolObj.transform;
        
        _effectPools = new Dictionary<GameObject, Queue<GameObject>>();
        
        // 기본 풀 생성
        if (_defaultMuzzleFlashPrefab != null)
        {
            CreatePool(_defaultMuzzleFlashPrefab);
        }
        
        // 라이플 머즐플래시 풀 생성
        if (_rifleMuzzleFlashPrefabs != null)
        {
            foreach (var prefab in _rifleMuzzleFlashPrefabs)
            {
                if (prefab != null)
                {
                    CreatePool(prefab);
                }
            }
        }
        
        // 기타 머즐플래시 풀
        if (_pistolMuzzleFlashPrefab != null) CreatePool(_pistolMuzzleFlashPrefab);
        if (_shotgunMuzzleFlashPrefab != null) CreatePool(_shotgunMuzzleFlashPrefab);
        if (_muzzleSmokePrefab != null) CreatePool(_muzzleSmokePrefab);
        if (_muzzleSparkPrefab != null) CreatePool(_muzzleSparkPrefab);
        
        Debug.Log("[MuzzleFlashController] 초기화 완료");
    }
    
    /// <summary>
    /// 오브젝트 풀 생성
    /// </summary>
    private void CreatePool(GameObject prefab)
    {
        if (prefab == null || _effectPools.ContainsKey(prefab)) return;
        
        Queue<GameObject> pool = new Queue<GameObject>();
        
        for (int i = 0; i < _initialPoolSize; i++)
        {
            GameObject obj = Instantiate(prefab, _poolParent);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
        
        _effectPools[prefab] = pool;
    }
    
    /// <summary>
    /// 풀에서 오브젝트 가져오기
    /// </summary>
    private GameObject GetFromPool(GameObject prefab)
    {
        if (prefab == null) return null;
        
        if (!_effectPools.ContainsKey(prefab))
        {
            CreatePool(prefab);
        }
        
        Queue<GameObject> pool = _effectPools[prefab];
        GameObject obj;
        
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            obj = Instantiate(prefab, _poolParent);
        }
        
        obj.SetActive(true);
        return obj;
    }
    
    /// <summary>
    /// 풀에 오브젝트 반환
    /// </summary>
    private void ReturnToPool(GameObject obj, GameObject prefab)
    {
        if (obj == null) return;
        
        obj.SetActive(false);
        obj.transform.SetParent(_poolParent);
        
        if (_effectPools.ContainsKey(prefab) && _effectPools[prefab].Count < _maxPoolSize)
        {
            _effectPools[prefab].Enqueue(obj);
        }
        else
        {
            Destroy(obj);
        }
    }
    
    /// <summary>
    /// 머즐 플래시 재생 (기본)
    /// </summary>
    /// <param name="muzzlePoint">총구 Transform</param>
    /// <param name="weaponType">무기 타입</param>
    public void PlayMuzzleFlash(Transform muzzlePoint, WeaponType weaponType = WeaponType.Rifle)
    {
        if (muzzlePoint == null) return;
        
        GameObject prefab = GetMuzzleFlashPrefab(weaponType);
        if (prefab == null) return;
        
        PlayEffect(prefab, muzzlePoint.position, muzzlePoint.rotation, _flashDuration);
        
        // 추가 이펙트 (연기, 스파크)
        PlayAdditionalEffects(muzzlePoint);
    }
    
    /// <summary>
    /// 커스텀 머즐 플래시 재생
    /// </summary>
    /// <param name="customPrefab">커스텀 프리팹</param>
    /// <param name="muzzlePoint">총구 Transform</param>
    public void PlayCustomMuzzleFlash(GameObject customPrefab, Transform muzzlePoint)
    {
        if (customPrefab == null || muzzlePoint == null) return;
        
        // 커스텀 프리팹 풀 생성 (없으면)
        if (!_effectPools.ContainsKey(customPrefab))
        {
            CreatePool(customPrefab);
        }
        
        PlayEffect(customPrefab, muzzlePoint.position, muzzlePoint.rotation, _flashDuration);
    }
    
    /// <summary>
    /// 무기 타입에 따른 머즐플래시 프리팹 반환
    /// </summary>
    private GameObject GetMuzzleFlashPrefab(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Pistol:
                return _pistolMuzzleFlashPrefab ?? _defaultMuzzleFlashPrefab;
                
            case WeaponType.Shotgun:
                return _shotgunMuzzleFlashPrefab ?? _defaultMuzzleFlashPrefab;
                
            case WeaponType.Rifle:
            case WeaponType.SMG:
            case WeaponType.LMG:
            default:
                // 라이플 계열은 여러 머즐플래시 순환 사용
                if (_rifleMuzzleFlashPrefabs != null && _rifleMuzzleFlashPrefabs.Length > 0)
                {
                    _currentRifleMuzzleIndex = (_currentRifleMuzzleIndex + 1) % _rifleMuzzleFlashPrefabs.Length;
                    return _rifleMuzzleFlashPrefabs[_currentRifleMuzzleIndex] ?? _defaultMuzzleFlashPrefab;
                }
                return _defaultMuzzleFlashPrefab;
        }
    }
    
    /// <summary>
    /// 이펙트 재생
    /// </summary>
    private void PlayEffect(GameObject prefab, Vector3 position, Quaternion rotation, float duration)
    {
        GameObject effectObj = GetFromPool(prefab);
        if (effectObj == null) return;
        
        // 위치 및 회전 설정
        effectObj.transform.position = position;
        
        if (_randomRotation)
        {
            // 기본 회전에 랜덤 Z축 회전 추가
            float randomZ = Random.Range(0f, 360f);
            effectObj.transform.rotation = rotation * Quaternion.Euler(0f, 0f, randomZ);
        }
        else
        {
            effectObj.transform.rotation = rotation;
        }
        
        // 랜덤 스케일
        float scale = Random.Range(_scaleRange.x, _scaleRange.y);
        effectObj.transform.localScale = Vector3.one * scale;
        
        // 파티클 시스템 재생
        ParticleSystem ps = effectObj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear();
            ps.Play();
        }
        
        // 자식 파티클 시스템도 재생
        ParticleSystem[] childPS = effectObj.GetComponentsInChildren<ParticleSystem>();
        foreach (var childParticle in childPS)
        {
            childParticle.Clear();
            childParticle.Play();
        }
        
        // 일정 시간 후 풀에 반환
        StartCoroutine(ReturnEffectAfterDelay(effectObj, prefab, duration));
    }
    
    /// <summary>
    /// 추가 이펙트 재생 (연기, 스파크)
    /// </summary>
    private void PlayAdditionalEffects(Transform muzzlePoint)
    {
        // 총구 연기
        if (_muzzleSmokePrefab != null)
        {
            PlayEffect(_muzzleSmokePrefab, muzzlePoint.position, muzzlePoint.rotation, _smokeDuration);
        }
        
        // 총구 스파크 (낮은 확률)
        if (_muzzleSparkPrefab != null && Random.value < 0.3f)
        {
            PlayEffect(_muzzleSparkPrefab, muzzlePoint.position, muzzlePoint.rotation, 0.1f);
        }
    }
    
    /// <summary>
    /// 지연 후 풀에 반환
    /// </summary>
    private IEnumerator ReturnEffectAfterDelay(GameObject obj, GameObject prefab, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(obj, prefab);
    }
    
    /// <summary>
    /// 외부에서 직접 호출용 - 위치와 회전 지정
    /// </summary>
    public void PlayMuzzleFlashAt(Vector3 position, Quaternion rotation, WeaponType weaponType = WeaponType.Rifle)
    {
        GameObject prefab = GetMuzzleFlashPrefab(weaponType);
        if (prefab == null) return;
        
        PlayEffect(prefab, position, rotation, _flashDuration);
    }
}

/// <summary>
/// 무기 타입 열거형
/// </summary>
public enum WeaponType
{
    Pistol,
    Rifle,
    SMG,
    Shotgun,
    LMG,
    Sniper
}
