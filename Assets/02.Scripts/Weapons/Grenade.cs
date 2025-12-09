using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 투척된 수류탄의 동작을 제어하는 컴포넌트
/// 물리 기반 이동, 바운스, 폭발 처리, ObjectPool 지원
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class Grenade : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private GrenadeData _grenadeData;
    
    // 컴포넌트 캐싱
    private Rigidbody _rigidbody;
    private AudioSource _audioSource;
    
    // 상태
    private bool _hasExploded;
    private float _timer;
    private bool _isInitialized;
    
    // 투척자 정보 (자기 데미지 방지용)
    private GameObject _thrower;
    
    // 오브젝트 풀 참조
    private ObjectPool<Grenade> _grenadePool;
    private ObjectPool<GameObject> _explosionPool;
    private bool _usePool;
    
    public GrenadeData GrenadeData
    {
        get => _grenadeData;
        set => _grenadeData = value;
    }
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _audioSource = GetComponent<AudioSource>();
        
        // Rigidbody 기본 설정
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }
    
    private void OnEnable()
    {
        // 풀에서 재사용 시 상태 리셋
        if (_isInitialized)
        {
            // Initialize에서 설정되므로 여기서는 패스
        }
    }
    
    private void Start()
    {
        if (_grenadeData != null && !_isInitialized)
        {
            _timer = _grenadeData.fuseTime;
        }
        else if (!_isInitialized)
        {
            _timer = 3f; // 기본값
        }
    }
    
    private void Update()
    {
        if (_hasExploded) return;
        
        // 타이머 카운트다운
        _timer -= Time.deltaTime;
        
        if (_timer <= 0f)
        {
            Explode();
        }
    }
    
    /// <summary>
    /// 오브젝트 풀 참조 설정
    /// </summary>
    public void SetPool(ObjectPool<Grenade> grenadePool, ObjectPool<GameObject> explosionPool)
    {
        _grenadePool = grenadePool;
        _explosionPool = explosionPool;
        _usePool = true;
    }
    
    /// <summary>
    /// 수류탄 상태 리셋 (풀 반환 시 호출)
    /// </summary>
    public void ResetGrenade()
    {
        _hasExploded = false;
        _isInitialized = false;
        _timer = 3f;
        _thrower = null;
        
        // Rigidbody 리셋
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }
    
    /// <summary>
    /// 수류탄 초기화 및 투척
    /// </summary>
    public void Initialize(GrenadeData data, Vector3 direction, GameObject thrower, float cookTime = 0f)
    {
        _grenadeData = data;
        _thrower = thrower;
        _timer = data.fuseTime - cookTime; // 쿠킹 시간 적용
        _hasExploded = false;
        _isInitialized = true;
        
        // 투척 힘 적용
        Vector3 throwVelocity = direction.normalized * data.throwForce;
        throwVelocity.y += data.upwardForce;
        _rigidbody.linearVelocity = throwVelocity;
        
        // 약간의 회전 추가 (시각적 효과)
        _rigidbody.angularVelocity = Random.insideUnitSphere * 5f;
    }
    
    /// <summary>
    /// 충돌 시 바운스 사운드 재생
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (_hasExploded) return;
        
        // 충분한 속도로 충돌했을 때만 사운드 재생
        if (collision.relativeVelocity.magnitude > 2f)
        {
            PlayBounceSound();
        }
    }
    
    /// <summary>
    /// 폭발 처리
    /// </summary>
    private void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;
        
        // 폭발 이펙트 생성
        SpawnExplosionEffect();
        
        // 폭발 사운드 재생
        PlayExplosionSound();
        
        // 범위 내 오브젝트에 데미지 및 물리력 적용
        ApplyExplosionDamage();
        ApplyExplosionForce();
        
        // 수류탄 오브젝트 풀로 반환 또는 파괴
        ReturnToPool();
    }
    
    /// <summary>
    /// 풀로 반환 또는 파괴
    /// </summary>
    private void ReturnToPool()
    {
        if (_usePool && _grenadePool != null)
        {
            // 약간의 딜레이 후 풀로 반환
            StartCoroutine(DelayedReturnToPool(0.1f));
        }
        else
        {
            // 풀 사용 안함 - 파괴
            Destroy(gameObject, 0.1f);
        }
    }
    
    private IEnumerator DelayedReturnToPool(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (_grenadePool != null)
        {
            _grenadePool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 폭발 이펙트 생성 (풀링 지원)
    /// </summary>
    private void SpawnExplosionEffect()
    {
        if (_grenadeData?.explosionPrefab == null) return;
        
        GameObject effect = null;
        
        // 풀에서 가져오기 시도
        if (_usePool && _explosionPool != null)
        {
            effect = _explosionPool.Get();
            if (effect != null)
            {
                effect.transform.position = transform.position;
                effect.transform.rotation = Quaternion.identity;
                
                // 파티클 시스템 재시작
                ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem ps in particles)
                {
                    ps.Clear();
                    ps.Play();
                }
                
                // 일정 시간 후 풀로 반환
                StartCoroutine(ReturnExplosionToPool(effect, 5f));
            }
        }
        
        // 풀이 없거나 실패하면 Instantiate
        if (effect == null)
        {
            effect = Instantiate(
                _grenadeData.explosionPrefab,
                transform.position,
                Quaternion.identity
            );
            
            // 이펙트 자동 제거 (5초 후)
            Destroy(effect, 5f);
        }
    }
    
    private IEnumerator ReturnExplosionToPool(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (_explosionPool != null && effect != null)
        {
            _explosionPool.Release(effect);
        }
        else if (effect != null)
        {
            Destroy(effect);
        }
    }
    
    /// <summary>
    /// 폭발 범위 내 데미지 적용
    /// </summary>
    private void ApplyExplosionDamage()
    {
        if (_grenadeData == null) return;
        
        // 폭발 범위 내 모든 콜라이더 탐색
        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            _grenadeData.explosionRadius,
            _grenadeData.damageableLayers
        );
        
        HashSet<GameObject> damagedObjects = new HashSet<GameObject>();
        
        foreach (Collider hitCollider in hitColliders)
        {
            GameObject hitObject = hitCollider.gameObject;
            
            // 이미 데미지를 준 오브젝트는 스킵
            if (damagedObjects.Contains(hitObject.transform.root.gameObject))
                continue;
            
            // 장애물 체크 (벽 뒤에 있으면 데미지 감소 또는 무시)
            Vector3 directionToTarget = hitCollider.bounds.center - transform.position;
            float distanceToTarget = directionToTarget.magnitude;
            
            bool isBlocked = Physics.Raycast(
                transform.position,
                directionToTarget.normalized,
                distanceToTarget,
                _grenadeData.obstacleLayers
            );
            
            if (isBlocked) continue; // 장애물에 가려진 경우 스킵
            
            // 거리에 따른 데미지 계산 (선형 감소)
            float normalizedDistance = distanceToTarget / _grenadeData.explosionRadius;
            float damage = Mathf.Lerp(
                _grenadeData.maxDamage,
                _grenadeData.minDamage,
                normalizedDistance
            );
            
            // IDamageable 인터페이스를 통해 데미지 적용
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = hitCollider.GetComponentInParent<IDamageable>();
            }
            
            if (damageable != null)
            {
                // 폭발 중심에서 타겟으로의 방향 계산
                Vector3 hitDirection = (hitCollider.bounds.center - transform.position).normalized;
                damageable.TakeDamage(damage, hitCollider.bounds.center, -hitDirection);
                damagedObjects.Add(hitObject.transform.root.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 폭발 물리력 적용
    /// </summary>
    private void ApplyExplosionForce()
    {
        if (_grenadeData == null) return;
        
        // 폭발 범위 내 모든 Rigidbody에 힘 적용
        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            _grenadeData.explosionRadius
        );
        
        HashSet<Rigidbody> affectedRigidbodies = new HashSet<Rigidbody>();
        
        foreach (Collider hitCollider in hitColliders)
        {
            Rigidbody rb = hitCollider.attachedRigidbody;
            
            if (rb != null && !affectedRigidbodies.Contains(rb))
            {
                rb.AddExplosionForce(
                    _grenadeData.explosionForce,
                    transform.position,
                    _grenadeData.explosionRadius,
                    1f, // 위로 올리는 힘
                    ForceMode.Impulse
                );
                
                affectedRigidbodies.Add(rb);
            }
        }
    }
    
    /// <summary>
    /// 바운스 사운드 재생
    /// </summary>
    private void PlayBounceSound()
    {
        if (_audioSource != null && _grenadeData?.bounceSound != null)
        {
            _audioSource.PlayOneShot(_grenadeData.bounceSound, 0.5f);
        }
    }
    
    /// <summary>
    /// 폭발 사운드 재생
    /// </summary>
    private void PlayExplosionSound()
    {
        if (_grenadeData?.explosionSound != null)
        {
            // 수류탄이 파괴되기 전에 사운드 재생을 위해 새 오브젝트 생성
            AudioSource.PlayClipAtPoint(
                _grenadeData.explosionSound,
                transform.position,
                1f
            );
        }
    }
    
    /// <summary>
    /// 디버그용 폭발 범위 표시
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (_grenadeData != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _grenadeData.explosionRadius);
        }
    }
}
