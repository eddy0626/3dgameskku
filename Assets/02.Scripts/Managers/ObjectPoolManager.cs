using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 범용 오브젝트 풀 매니저
/// 싱글톤 패턴으로 전역 접근 가능
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    #region Singleton
    
    private static ObjectPoolManager _instance;
    
    public static ObjectPoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ObjectPoolManager>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("ObjectPoolManager");
                    _instance = go.AddComponent<ObjectPoolManager>();
                }
            }
            return _instance;
        }
    }
    
    #endregion
    
    #region Inspector Fields
    
    [Header("풀 설정")]
    [SerializeField] private PoolConfig[] _poolConfigs;
    
    [Header("디버그")]
    [SerializeField] private bool _debugMode = false;
    
    #endregion
    
    #region Private Fields
    
    private Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, PoolConfig> _configMap = new Dictionary<string, PoolConfig>();
    private Dictionary<string, Transform> _poolParents = new Dictionary<string, Transform>();
    private Transform _poolRoot;
    
    #endregion
    
    #region Unity Callbacks
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializePools();
    }
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// 모든 풀 초기화
    /// </summary>
    private void InitializePools()
    {
        // 풀 루트 생성
        _poolRoot = new GameObject("PoolRoot").transform;
        _poolRoot.SetParent(transform);
        
        if (_poolConfigs == null) return;
        
        foreach (PoolConfig config in _poolConfigs)
        {
            if (config.prefab == null) continue;
            
            CreatePool(config);
        }
        
        if (_debugMode)
        {
            Debug.Log($"[ObjectPoolManager] Initialized {_pools.Count} pools");
        }
    }
    
    /// <summary>
    /// 개별 풀 생성
    /// </summary>
    private void CreatePool(PoolConfig config)
    {
        string key = config.poolKey;
        
        if (string.IsNullOrEmpty(key))
        {
            key = config.prefab.name;
        }
        
        if (_pools.ContainsKey(key))
        {
            Debug.LogWarning($"[ObjectPoolManager] Pool '{key}' already exists!");
            return;
        }
        
        // 풀 컨테이너 생성
        Transform poolParent = new GameObject($"Pool_{key}").transform;
        poolParent.SetParent(_poolRoot);
        _poolParents[key] = poolParent;
        
        // 설정 저장
        _configMap[key] = config;
        
        // 풀 초기화
        Queue<GameObject> pool = new Queue<GameObject>();
        
        for (int i = 0; i < config.initialSize; i++)
        {
            GameObject obj = CreateNewObject(config.prefab, poolParent);
            pool.Enqueue(obj);
        }
        
        _pools[key] = pool;
        
        if (_debugMode)
        {
            Debug.Log($"[ObjectPoolManager] Created pool '{key}' with {config.initialSize} objects");
        }
    }
    
    /// <summary>
    /// 새 오브젝트 생성
    /// </summary>
    private GameObject CreateNewObject(GameObject prefab, Transform parent)
    {
        GameObject obj = Instantiate(prefab, parent);
        obj.SetActive(false);
        
        // IPoolable 인터페이스 초기화
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnCreated();
        }
        
        return obj;
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 런타임에 새 풀 등록
    /// </summary>
    public void RegisterPool(string key, GameObject prefab, int initialSize = 10, int maxSize = 50, bool canExpand = true)
    {
        if (_pools.ContainsKey(key))
        {
            if (_debugMode)
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{key}' already registered");
            }
            return;
        }
        
        PoolConfig config = new PoolConfig
        {
            poolKey = key,
            prefab = prefab,
            initialSize = initialSize,
            maxSize = maxSize,
            canExpand = canExpand
        };
        
        CreatePool(config);
    }
    
    /// <summary>
    /// 풀에서 오브젝트 가져오기
    /// </summary>
    public GameObject Get(string key)
    {
        return Get(key, Vector3.zero, Quaternion.identity);
    }
    
    /// <summary>
    /// 풀에서 오브젝트 가져오기 (위치/회전 지정)
    /// </summary>
    public GameObject Get(string key, Vector3 position, Quaternion rotation)
    {
        if (!_pools.ContainsKey(key))
        {
            Debug.LogWarning($"[ObjectPoolManager] Pool '{key}' not found!");
            return null;
        }
        
        Queue<GameObject> pool = _pools[key];
        PoolConfig config = _configMap[key];
        GameObject obj = null;
        
        // 풀에서 사용 가능한 오브젝트 찾기
        while (pool.Count > 0 && obj == null)
        {
            obj = pool.Dequeue();
            
            // null 체크 (씬 전환 등으로 파괴된 경우)
            if (obj == null)
            {
                continue;
            }
        }
        
        // 풀이 비었으면 새로 생성
        if (obj == null)
        {
            if (config.canExpand)
            {
                obj = CreateNewObject(config.prefab, _poolParents[key]);
                
                if (_debugMode)
                {
                    Debug.Log($"[ObjectPoolManager] Pool '{key}' expanded");
                }
            }
            else
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{key}' is empty and cannot expand!");
                return null;
            }
        }
        
        // 위치/회전 설정 및 활성화
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        
        // IPoolable 콜백
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnGetFromPool();
        }
        
        return obj;
    }
    
    /// <summary>
    /// 오브젝트를 풀에 반환
    /// </summary>
    public void Return(string key, GameObject obj)
    {
        if (obj == null) return;
        
        if (!_pools.ContainsKey(key))
        {
            Debug.LogWarning($"[ObjectPoolManager] Pool '{key}' not found! Destroying object.");
            Destroy(obj);
            return;
        }
        
        // IPoolable 콜백
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnReturnToPool();
        }
        
        obj.SetActive(false);
        obj.transform.SetParent(_poolParents[key]);
        
        _pools[key].Enqueue(obj);
    }
    
    /// <summary>
    /// 지정 시간 후 자동 반환
    /// </summary>
    public void ReturnDelayed(string key, GameObject obj, float delay)
    {
        if (obj == null) return;
        
        StartCoroutine(ReturnDelayedRoutine(key, obj, delay));
    }
    
    /// <summary>
    /// 프리팹 이름으로 오브젝트 가져오기
    /// </summary>
    public GameObject GetByPrefab(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        
        string key = prefab.name;
        
        // 풀이 없으면 자동 등록
        if (!_pools.ContainsKey(key))
        {
            RegisterPool(key, prefab, 5, 30, true);
        }
        
        return Get(key, position, rotation);
    }
    
    /// <summary>
    /// 프리팹 이름으로 반환
    /// </summary>
    public void ReturnByPrefab(GameObject obj)
    {
        if (obj == null) return;
        
        // 프리팹 이름 추출 (Clone 제거)
        string key = obj.name.Replace("(Clone)", "").Trim();
        Return(key, obj);
    }
    
    /// <summary>
    /// 특정 풀의 현재 크기
    /// </summary>
    public int GetPoolSize(string key)
    {
        if (!_pools.ContainsKey(key)) return 0;
        return _pools[key].Count;
    }
    
    /// <summary>
    /// 특정 풀 비우기
    /// </summary>
    public void ClearPool(string key)
    {
        if (!_pools.ContainsKey(key)) return;
        
        while (_pools[key].Count > 0)
        {
            GameObject obj = _pools[key].Dequeue();
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        
        if (_debugMode)
        {
            Debug.Log($"[ObjectPoolManager] Cleared pool '{key}'");
        }
    }
    
    /// <summary>
    /// 모든 풀 비우기
    /// </summary>
    public void ClearAllPools()
    {
        foreach (string key in _pools.Keys)
        {
            ClearPool(key);
        }
    }
    
    /// <summary>
    /// 풀 존재 여부 확인
    /// </summary>
    public bool HasPool(string key)
    {
        return _pools.ContainsKey(key);
    }
    
    #endregion
    
    #region Private Methods
    
    private System.Collections.IEnumerator ReturnDelayedRoutine(string key, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Return(key, obj);
    }
    
    #endregion
}

#region Pool Config

/// <summary>
/// 풀 설정 구조체
/// </summary>
[System.Serializable]
public class PoolConfig
{
    [Tooltip("풀 식별 키 (비어있으면 프리팹 이름 사용)")]
    public string poolKey;
    
    [Tooltip("풀링할 프리팹")]
    public GameObject prefab;
    
    [Tooltip("초기 풀 크기")]
    [Range(1, 100)]
    public int initialSize = 10;
    
    [Tooltip("최대 풀 크기")]
    [Range(1, 500)]
    public int maxSize = 50;
    
    [Tooltip("풀 확장 가능 여부")]
    public bool canExpand = true;
}

#endregion

#region IPoolable Interface

/// <summary>
/// 풀링 가능한 오브젝트 인터페이스
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 오브젝트 최초 생성 시 호출
    /// </summary>
    void OnCreated();
    
    /// <summary>
    /// 풀에서 가져올 때 호출
    /// </summary>
    void OnGetFromPool();
    
    /// <summary>
    /// 풀에 반환할 때 호출
    /// </summary>
    void OnReturnToPool();
}

#endregion