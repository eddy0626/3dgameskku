using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 폭발하는 오브젝트 (드럼통, 가스통 등)
/// 체력이 0 이하가 되면 폭발하여 주변에 데미지를 주고 연쇄 폭발을 일으킴
/// </summary>
[RequireComponent(typeof(NavMeshObstacle))]
public class ExplosiveObject : MonoBehaviour, IDamageable
{
    [Header("체력 설정")]
    [SerializeField] private float _maxHealth = 50f;
    [SerializeField] private float _currentHealth;
    
    [Header("폭발 설정")]
    [SerializeField] private float _explosionRadius = 5f;
    [SerializeField] private float _explosionDamage = 100f;
    [SerializeField] private float _explosionForce = 500f;
    [SerializeField] private float _upwardForce = 300f;
    [SerializeField] private LayerMask _damageableLayers;
    [SerializeField] private LayerMask _obstacleLayers;
    
    [Header("이펙트")]
    [SerializeField] private GameObject _explosionPrefab;
    [SerializeField] private AudioClip _explosionSound;
    [SerializeField] private float _explosionSoundVolume = 1f;
    
    [Header("연쇄 폭발 설정")]
    [SerializeField] private float _chainExplosionDelay = 0.1f;
    [SerializeField] private bool _enableChainExplosion = true;
    
    [Header("비주얼")]
    [SerializeField] private Renderer _objectRenderer;
    [SerializeField] private Color _damageFlashColor = Color.red;
    [SerializeField] private float _flashDuration = 0.1f;
    
    // 컴포넌트 캐싱
    private Rigidbody _rigidbody;
    private NavMeshObstacle _navMeshObstacle;
    private Collider _collider;
    private Color _originalColor;
    private Material _material;
    
    // 상태
    private bool _hasExploded;
    private bool _isFlashing;
    
    // IDamageable 구현
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public bool IsAlive => _currentHealth > 0 && !_hasExploded;
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _navMeshObstacle = GetComponent<NavMeshObstacle>();
        _collider = GetComponent<Collider>();
        
        // Rigidbody 설정 (없으면 추가)
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
        }
        
        // NavMeshObstacle 설정
        SetupNavMeshObstacle();
        
        // 렌더러 설정
        SetupRenderer();
        
        // 체력 초기화
        _currentHealth = _maxHealth;
    }
    
    private void SetupNavMeshObstacle()
    {
        if (_navMeshObstacle == null) return;
        
        // Carve 설정 (NavMesh에 구멍을 뚫어서 AI가 지나가지 못하게 함)
        _navMeshObstacle.carving = true;
        _navMeshObstacle.carveOnlyStationary = true;
        _navMeshObstacle.carvingMoveThreshold = 0.1f;
        _navMeshObstacle.carvingTimeToStationary = 0.5f;
    }
    
    private void SetupRenderer()
    {
        // 렌더러가 지정되지 않았으면 자동 탐색
        if (_objectRenderer == null)
        {
            _objectRenderer = GetComponentInChildren<Renderer>();
        }
        
        if (_objectRenderer != null)
        {
            // 머티리얼 인스턴스화 (원본 수정 방지)
            _material = _objectRenderer.material;
            _originalColor = _material.color;
        }
    }
    
    /// <summary>
    /// 데미지 처리
    /// </summary>
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_hasExploded) return;
        
        _currentHealth -= damage;
        
        // 피격 플래시
        if (!_isFlashing && _material != null)
        {
            StartCoroutine(DamageFlashCoroutine());
        }
        
        // 체력이 0 이하면 폭발
        if (_currentHealth <= 0)
        {
            Explode();
        }
    }
    
    /// <summary>
    /// 회복 (폭발 오브젝트는 회복 불가)
    /// </summary>
    public void Heal(float amount)
    {
        // 폭발 오브젝트는 회복 기능 없음
    }
    
    /// <summary>
    /// 폭발 처리
    /// </summary>
    private void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;
        
        // NavMesh Carve 비활성화
        if (_navMeshObstacle != null)
        {
            _navMeshObstacle.carving = false;
            _navMeshObstacle.enabled = false;
        }
        
        // Rigidbody를 Kinematic에서 해제하고 하늘로 솟구치게
        LaunchIntoAir();
        
        // 폭발 이펙트 생성
        SpawnExplosionEffect();
        
        // 폭발 사운드
        PlayExplosionSound();
        
        // 주변 데미지 적용
        ApplyExplosionDamage();
        
        // 주변 물리력 적용
        ApplyExplosionForce();
        
        // 시각적 파괴 (메시 비활성화)
        DisableVisuals();
        
        // 일정 시간 후 오브젝트 제거
        StartCoroutine(DestroyAfterDelay(3f));
    }
    
    /// <summary>
    /// 하늘로 솟구치는 효과
    /// </summary>
    private void LaunchIntoAir()
    {
        if (_rigidbody == null) return;
        
        // Kinematic 해제
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        
        // 위쪽 + 랜덤 방향으로 힘 적용
        Vector3 randomDirection = Random.insideUnitSphere;
        randomDirection.y = Mathf.Abs(randomDirection.y); // 항상 위로
        
        Vector3 launchForce = Vector3.up * _upwardForce + randomDirection * (_upwardForce * 0.3f);
        _rigidbody.AddForce(launchForce, ForceMode.Impulse);
        
        // 회전력 추가
        _rigidbody.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
    }
    
    /// <summary>
    /// 폭발 이펙트 생성
    /// </summary>
    private void SpawnExplosionEffect()
    {
        if (_explosionPrefab == null) return;
        
        GameObject effect = Instantiate(
            _explosionPrefab,
            transform.position,
            Quaternion.identity
        );
        
        // 5초 후 이펙트 제거
        Destroy(effect, 5f);
    }
    
    /// <summary>
    /// 폭발 사운드 재생
    /// </summary>
    private void PlayExplosionSound()
    {
        if (_explosionSound == null) return;
        
        AudioSource.PlayClipAtPoint(
            _explosionSound,
            transform.position,
            _explosionSoundVolume
        );
    }
    
    /// <summary>
    /// 폭발 범위 내 데미지 적용
    /// </summary>
    private void ApplyExplosionDamage()
    {
        // 폭발 범위 내 모든 콜라이더 탐색
        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            _explosionRadius,
            _damageableLayers
        );
        
        HashSet<GameObject> damagedObjects = new HashSet<GameObject>();
        List<ExplosiveObject> chainExplosives = new List<ExplosiveObject>();
        
        foreach (Collider hitCollider in hitColliders)
        {
            // 자기 자신 제외
            if (hitCollider.gameObject == gameObject) continue;
            
            GameObject hitObject = hitCollider.gameObject;
            GameObject rootObject = hitObject.transform.root.gameObject;
            
            // 이미 데미지를 준 오브젝트는 스킵
            if (damagedObjects.Contains(rootObject)) continue;
            
            // 장애물 체크
            Vector3 directionToTarget = hitCollider.bounds.center - transform.position;
            float distanceToTarget = directionToTarget.magnitude;
            
            bool isBlocked = Physics.Raycast(
                transform.position,
                directionToTarget.normalized,
                distanceToTarget * 0.9f, // 약간 짧게 체크
                _obstacleLayers
            );
            
            if (isBlocked) continue;
            
            // 거리에 따른 데미지 계산 (거리가 멀수록 감소)
            float normalizedDistance = distanceToTarget / _explosionRadius;
            float damage = _explosionDamage * (1f - normalizedDistance);
            
            // IDamageable 인터페이스 검색
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = hitCollider.GetComponentInParent<IDamageable>();
            }
            
            if (damageable != null)
            {
                Vector3 hitDirection = (hitCollider.bounds.center - transform.position).normalized;
                damageable.TakeDamage(damage, hitCollider.bounds.center, -hitDirection);
                damagedObjects.Add(rootObject);
                
                // 연쇄 폭발 대상 수집
                if (_enableChainExplosion)
                {
                    ExplosiveObject explosive = hitCollider.GetComponent<ExplosiveObject>();
                    if (explosive == null)
                    {
                        explosive = hitCollider.GetComponentInParent<ExplosiveObject>();
                    }
                    
                    if (explosive != null && explosive != this && !explosive._hasExploded)
                    {
                        chainExplosives.Add(explosive);
                    }
                }
            }
        }
        
        // 연쇄 폭발 처리 (약간의 딜레이)
        if (_enableChainExplosion && chainExplosives.Count > 0)
        {
            StartCoroutine(TriggerChainExplosions(chainExplosives));
        }
    }
    
    /// <summary>
    /// 연쇄 폭발 트리거
    /// </summary>
    private IEnumerator TriggerChainExplosions(List<ExplosiveObject> explosives)
    {
        foreach (ExplosiveObject explosive in explosives)
        {
            if (explosive != null && !explosive._hasExploded)
            {
                yield return new WaitForSeconds(_chainExplosionDelay);
                
                // 남은 체력에 관계없이 폭발
                explosive._currentHealth = 0;
                explosive.Explode();
            }
        }
    }
    
    /// <summary>
    /// 폭발 물리력 적용
    /// </summary>
    private void ApplyExplosionForce()
    {
        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            _explosionRadius
        );
        
        HashSet<Rigidbody> affectedRigidbodies = new HashSet<Rigidbody>();
        
        foreach (Collider hitCollider in hitColliders)
        {
            // 자기 자신 제외
            if (hitCollider.gameObject == gameObject) continue;
            
            Rigidbody rb = hitCollider.attachedRigidbody;
            
            if (rb != null && !affectedRigidbodies.Contains(rb))
            {
                rb.AddExplosionForce(
                    _explosionForce,
                    transform.position,
                    _explosionRadius,
                    1f,
                    ForceMode.Impulse
                );
                
                affectedRigidbodies.Add(rb);
            }
        }
    }
    
    /// <summary>
    /// 비주얼 비활성화
    /// </summary>
    private void DisableVisuals()
    {
        if (_objectRenderer != null)
        {
            _objectRenderer.enabled = false;
        }
        
        if (_collider != null)
        {
            _collider.enabled = false;
        }
    }
    
    /// <summary>
    /// 피격 플래시 코루틴
    /// </summary>
    private IEnumerator DamageFlashCoroutine()
    {
        _isFlashing = true;
        
        if (_material != null)
        {
            _material.color = _damageFlashColor;
            yield return new WaitForSeconds(_flashDuration);
            _material.color = _originalColor;
        }
        
        _isFlashing = false;
    }
    
    /// <summary>
    /// 지연 후 오브젝트 제거
    /// </summary>
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 에디터에서 폭발 범위 시각화
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 폭발 범위
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, _explosionRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}
