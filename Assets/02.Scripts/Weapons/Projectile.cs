using UnityEngine;

/// <summary>
/// 발사체(총알) 스크립트
/// 물리 기반 또는 직선 이동 발사체
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("발사체 설정")]
    [SerializeField] private float _speed = 100f;
    [SerializeField] private float _damage = 25f;
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private bool _useGravity = false;
    [SerializeField] private float _gravityMultiplier = 1f;
    
    [Header("충돌 설정")]
    [SerializeField] private LayerMask _hitLayers = ~0;
    [SerializeField] private float _hitCheckRadius = 0.05f;
    
    [Header("이펙트")]
    [SerializeField] private GameObject _impactEffectPrefab;
    [SerializeField] private GameObject _trailPrefab;
    
    // 내부 변수
    private Vector3 _velocity;
    private Vector3 _previousPosition;
    private float _spawnTime;
    private bool _hasHit;
    
    // 외부에서 설정
    private float _customDamage;
    private GameObject _owner;
    
    private void Start()
    {
        _spawnTime = Time.time;
        _previousPosition = transform.position;
        _velocity = transform.forward * _speed;
        
        // 트레일 이펙트 생성
        if (_trailPrefab != null)
        {
            Instantiate(_trailPrefab, transform.position, Quaternion.identity, transform);
        }
    }
    
    private void Update()
    {
        if (_hasHit) return;
        
        // 수명 체크
        if (Time.time - _spawnTime > _lifeTime)
        {
            Destroy(gameObject);
            return;
        }
        
        // 중력 적용
        if (_useGravity)
        {
            _velocity += Physics.gravity * _gravityMultiplier * Time.deltaTime;
        }
        
        // 이동
        MoveProjectile();
        
        // 충돌 검사
        CheckCollision();
    }
    
    /// <summary>
    /// 발사체 이동
    /// </summary>
    private void MoveProjectile()
    {
        _previousPosition = transform.position;
        transform.position += _velocity * Time.deltaTime;
        
        // 이동 방향으로 회전
        if (_velocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(_velocity);
        }
    }
    
    /// <summary>
    /// 레이캐스트 기반 충돌 검사 (관통 방지)
    /// </summary>
    private void CheckCollision()
    {
        Vector3 direction = transform.position - _previousPosition;
        float distance = direction.magnitude;
        
        if (distance < 0.001f) return;
        
        RaycastHit hit;
        if (Physics.SphereCast(
            _previousPosition, 
            _hitCheckRadius, 
            direction.normalized, 
            out hit, 
            distance, 
            _hitLayers))
        {
            OnHit(hit);
        }
    }
    
    /// <summary>
    /// 충돌 처리
    /// </summary>
    private void OnHit(RaycastHit hit)
    {
        if (_hasHit) return;
        _hasHit = true;
        
        // 위치를 충돌 지점으로 이동
        transform.position = hit.point;
        
        // 데미지 처리
        float finalDamage = _customDamage > 0 ? _customDamage : _damage;
        
        var damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(finalDamage, hit.point, hit.normal);
        }
        
        // 디버그 로그
        Debug.Log($"[Projectile] Hit: {hit.collider.name}, Damage: {finalDamage}");
        
        // 임팩트 이펙트
        SpawnImpactEffect(hit);
        
        // 총알 제거
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 임팩트 이펙트 생성
    /// </summary>
/// <summary>
    /// 임팩트 이펙트 생성
    /// </summary>
    private void SpawnImpactEffect(RaycastHit hit)
    {
        // ImpactEffectManager 사용 (우선)
        if (ImpactEffectManager.Instance != null)
        {
            ImpactEffectManager.Instance.PlayImpact(hit);
            return;
        }
        
        // 폴백: 기존 방식
        if (_impactEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                _impactEffectPrefab,
                hit.point,
                Quaternion.LookRotation(hit.normal)
            );
            Destroy(effect, 2f);
        }
    }
    
    /// <summary>
    /// 충돌 트리거 방식 (보조)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (_hasHit) return;
        if (_owner != null && other.gameObject == _owner) return;
        
        // LayerMask 체크
        if ((_hitLayers.value & (1 << other.gameObject.layer)) == 0) return;
        
        _hasHit = true;
        
        float finalDamage = _customDamage > 0 ? _customDamage : _damage;
        
        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(finalDamage, transform.position, -transform.forward);
        }
        
        Debug.Log($"[Projectile] Trigger Hit: {other.name}");
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 발사체 초기화 (외부에서 호출)
    /// </summary>
    public void Initialize(float speed, float damage, Vector3 direction, GameObject owner = null)
    {
        _speed = speed;
        _customDamage = damage;
        _velocity = direction.normalized * speed;
        _owner = owner;
        
        transform.rotation = Quaternion.LookRotation(direction);
    }
    
    /// <summary>
    /// 중력 설정
    /// </summary>
    public void SetGravity(bool useGravity, float multiplier = 1f)
    {
        _useGravity = useGravity;
        _gravityMultiplier = multiplier;
    }
    
    /// <summary>
    /// 히트 레이어 설정
    /// </summary>
    public void SetHitLayers(LayerMask layers)
    {
        _hitLayers = layers;
    }
}
