using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 총알 임팩트 이펙트 매니저
/// 오브젝트 풀링으로 성능 최적화
/// </summary>
public class ImpactEffectManager : MonoBehaviour
{
    public static ImpactEffectManager Instance { get; private set; }
    
    [Header("임팩트 데이터")]
    [SerializeField] private ImpactData[] _impactDataList;
    
    [Header("기본 이펙트 (데이터 없을 때 사용)")]
    [SerializeField] private GameObject _defaultParticlePrefab;
    [SerializeField] private GameObject _defaultDecalPrefab;
    
    [Header("풀 설정")]
    [SerializeField] private int _initialPoolSize = 20;
    [SerializeField] private int _maxPoolSize = 50;
    
    // 표면 타입별 데이터 딕셔너리
    private Dictionary<SurfaceType, ImpactData> _impactDataDict;
    
    // 오브젝트 풀
    private Dictionary<GameObject, Queue<GameObject>> _particlePools;
    private Dictionary<GameObject, Queue<GameObject>> _decalPools;
    
    // 풀 부모 오브젝트
    private Transform _poolParent;
    
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
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
        GameObject poolParentObj = new GameObject("ImpactEffectPool");
        poolParentObj.transform.SetParent(transform);
        _poolParent = poolParentObj.transform;
        
        // 딕셔너리 초기화
        _impactDataDict = new Dictionary<SurfaceType, ImpactData>();
        _particlePools = new Dictionary<GameObject, Queue<GameObject>>();
        _decalPools = new Dictionary<GameObject, Queue<GameObject>>();
        
        // 임팩트 데이터 등록
        if (_impactDataList != null)
        {
            foreach (var data in _impactDataList)
            {
                if (data != null && !_impactDataDict.ContainsKey(data.surfaceType))
                {
                    _impactDataDict[data.surfaceType] = data;
                    
                    // 풀 사전 생성
                    if (data.particlePrefab != null)
                    {
                        CreatePool(data.particlePrefab, _particlePools);
                    }
                    if (data.decalPrefab != null)
                    {
                        CreatePool(data.decalPrefab, _decalPools);
                    }
                }
            }
        }
        
        // 기본 이펙트 풀 생성
        if (_defaultParticlePrefab != null)
        {
            CreatePool(_defaultParticlePrefab, _particlePools);
        }
        if (_defaultDecalPrefab != null)
        {
            CreatePool(_defaultDecalPrefab, _decalPools);
        }
        
        Debug.Log($"[ImpactEffectManager] 초기화 완료 - {_impactDataDict.Count}개 표면 타입 등록");
    }
    
    /// <summary>
    /// 풀 생성
    /// </summary>
    private void CreatePool(GameObject prefab, Dictionary<GameObject, Queue<GameObject>> pools)
    {
        if (prefab == null || pools.ContainsKey(prefab)) return;
        
        Queue<GameObject> pool = new Queue<GameObject>();
        
        for (int i = 0; i < _initialPoolSize; i++)
        {
            GameObject obj = Instantiate(prefab, _poolParent);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
        
        pools[prefab] = pool;
    }
    
    /// <summary>
    /// 풀에서 오브젝트 가져오기
    /// </summary>
    private GameObject GetFromPool(GameObject prefab, Dictionary<GameObject, Queue<GameObject>> pools)
    {
        if (prefab == null) return null;
        
        // 풀이 없으면 생성
        if (!pools.ContainsKey(prefab))
        {
            CreatePool(prefab, pools);
        }
        
        Queue<GameObject> pool = pools[prefab];
        GameObject obj;
        
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            // 풀이 비었으면 새로 생성
            obj = Instantiate(prefab, _poolParent);
        }
        
        obj.SetActive(true);
        return obj;
    }
    
    /// <summary>
    /// 풀에 오브젝트 반환
    /// </summary>
    private void ReturnToPool(GameObject obj, GameObject prefab, Dictionary<GameObject, Queue<GameObject>> pools)
    {
        if (obj == null) return;
        
        obj.SetActive(false);
        obj.transform.SetParent(_poolParent);
        
        if (pools.ContainsKey(prefab) && pools[prefab].Count < _maxPoolSize)
        {
            pools[prefab].Enqueue(obj);
        }
        else
        {
            Destroy(obj);
        }
    }
    
    /// <summary>
    /// 임팩트 이펙트 재생 (RaycastHit 사용)
    /// </summary>
    public void PlayImpact(RaycastHit hit)
    {
        SurfaceType surfaceType = GetSurfaceType(hit.collider);
        PlayImpact(hit.point, hit.normal, surfaceType, hit.collider);
    }
    
    /// <summary>
    /// 임팩트 이펙트 재생 (직접 파라미터)
    /// </summary>
    public void PlayImpact(Vector3 position, Vector3 normal, SurfaceType surfaceType, Collider hitCollider = null)
    {
        ImpactData data = GetImpactData(surfaceType);
        
        Quaternion rotation = Quaternion.LookRotation(normal);
        
        // 파티클 이펙트
        SpawnParticle(data, position, rotation);
        
        // 탄흔 데칼
        SpawnDecal(data, position, normal, rotation);
        
        // 사운드
        PlayImpactSound(data, position);
        
        // 물리 효과 (Rigidbody가 있는 경우)
        ApplyImpactForce(data, hitCollider, position, normal);
    }
    
    /// <summary>
    /// 표면 타입 가져오기
    /// </summary>
    private SurfaceType GetSurfaceType(Collider collider)
    {
        if (collider == null) return SurfaceType.Default;
        
        // SurfaceIdentifier 컴포넌트 확인
        SurfaceIdentifier identifier = collider.GetComponent<SurfaceIdentifier>();
        if (identifier != null)
        {
            return identifier.SurfaceType;
        }
        
        // 태그로 판단 (폴백)
        switch (collider.tag)
        {
            case "Metal":
                return SurfaceType.Metal;
            case "Wood":
                return SurfaceType.Wood;
            case "Enemy":
            case "Player":
                return SurfaceType.Flesh;
            case "Water":
                return SurfaceType.Water;
            case "Glass":
                return SurfaceType.Glass;
            default:
                return SurfaceType.Default;
        }
    }
    
    /// <summary>
    /// 임팩트 데이터 가져오기
    /// </summary>
    private ImpactData GetImpactData(SurfaceType surfaceType)
    {
        if (_impactDataDict.TryGetValue(surfaceType, out ImpactData data))
        {
            return data;
        }
        
        // 기본 데이터 반환
        if (_impactDataDict.TryGetValue(SurfaceType.Default, out ImpactData defaultData))
        {
            return defaultData;
        }
        
        return null;
    }
    
    /// <summary>
    /// 파티클 생성
    /// </summary>
    private void SpawnParticle(ImpactData data, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = data?.particlePrefab ?? _defaultParticlePrefab;
        if (prefab == null) return;
        
        GameObject particle = GetFromPool(prefab, _particlePools);
        if (particle == null) return;
        
        particle.transform.position = position;
        particle.transform.rotation = rotation;
        
        // 파티클 시스템 재생
        ParticleSystem ps = particle.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear();
            ps.Play();
        }
        
        // 일정 시간 후 풀에 반환
        float lifetime = data?.particleLifetime ?? 1f;
        StartCoroutine(ReturnToPoolAfterDelay(particle, prefab, _particlePools, lifetime));
    }
    
    /// <summary>
    /// 탄흔 데칼 생성
    /// </summary>
    private void SpawnDecal(ImpactData data, Vector3 position, Vector3 normal, Quaternion rotation)
    {
        GameObject prefab = data?.decalPrefab ?? _defaultDecalPrefab;
        if (prefab == null) return;
        
        GameObject decal = GetFromPool(prefab, _decalPools);
        if (decal == null) return;
        
        // 표면에서 살짝 떨어뜨려 Z-fighting 방지
        decal.transform.position = position + normal * 0.001f;
        decal.transform.rotation = rotation;
        
        // 크기 설정
        float size = data?.decalSize ?? 0.1f;
        decal.transform.localScale = Vector3.one * size;
        
        // 일정 시간 후 풀에 반환
        float lifetime = data?.decalLifetime ?? 10f;
        StartCoroutine(ReturnToPoolAfterDelay(decal, prefab, _decalPools, lifetime));
    }
    
    /// <summary>
    /// 임팩트 사운드 재생
    /// </summary>
    private void PlayImpactSound(ImpactData data, Vector3 position)
    {
        if (data == null) return;
        
        AudioClip clip = data.GetRandomSound();
        if (clip == null) return;
        
        AudioSource.PlayClipAtPoint(clip, position, data.volume);
    }
    
    /// <summary>
    /// 물리 효과 적용
    /// </summary>
    private void ApplyImpactForce(ImpactData data, Collider hitCollider, Vector3 position, Vector3 normal)
    {
        if (data == null || hitCollider == null || data.impactForce <= 0) return;
        
        Rigidbody rb = hitCollider.attachedRigidbody;
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForceAtPosition(-normal * data.impactForce, position, ForceMode.Impulse);
        }
    }
    
    /// <summary>
    /// 일정 시간 후 풀에 반환
    /// </summary>
    private System.Collections.IEnumerator ReturnToPoolAfterDelay(
        GameObject obj, 
        GameObject prefab, 
        Dictionary<GameObject, Queue<GameObject>> pools, 
        float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(obj, prefab, pools);
    }
}
