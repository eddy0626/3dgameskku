using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 적 체력 관리 컴포넌트
/// IDamageable 인터페이스 구현
/// DOTween Pro를 사용한 피격 애니메이션 시스템
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("체력 설정")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _currentHealth;
    [SerializeField] private float _armor = 0f;
    [SerializeField] private float _headshotMultiplier = 2f;
    
    [Header("피격 이펙트")]
    [SerializeField] private GameObject _hitEffectPrefab;
    [SerializeField] private GameObject _deathEffectPrefab;
    [SerializeField] private float _hitEffectDuration = 1f;
    
    [Header("피격 애니메이션 - DOTween")]
    [SerializeField] private bool _useHitAnimation = true;
    [SerializeField] private float _hitFlashDuration = 0.15f;
    [SerializeField] private Color _hitFlashColor = Color.red;
    [SerializeField] private float _shakeStrength = 0.3f;
    [SerializeField] private int _shakeVibrato = 10;
    [SerializeField] private float _shakeDuration = 0.2f;
    [SerializeField] private float _punchScale = 0.15f;
    [SerializeField] private float _punchDuration = 0.2f;
    [SerializeField] private float _knockbackDistance = 0.3f;
    [SerializeField] private float _knockbackDuration = 0.15f;
    
    [Header("사운드")]
    [SerializeField] private AudioClip _hitSound;
    [SerializeField] private AudioClip _deathSound;
    
    // 프로퍼티
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public float Armor => _armor;
    public bool IsAlive => _currentHealth > 0f;
    public float HealthPercent => _currentHealth / _maxHealth;
    
    // 이벤트
    public event Action<float, float> OnHealthChanged;  // (currentHealth, maxHealth)
    public event Action<float, Vector3> OnDamaged;      // (damage, hitPoint)
    public event Action OnDeath;
    
    // 내부 변수
    private Renderer[] _renderers;
    private Material[] _materials;
    private Color[] _originalColors;
    private AudioSource _audioSource;
    private Transform _modelTransform;
    private Vector3 _originalPosition;
    private Vector3 _originalScale;
    
    // DOTween 시퀀스 관리
    private Sequence _hitSequence;
    private Tween _colorTween;
    private Tween _shakeTween;
    private Tween _scaleTween;
    private Tween _knockbackTween;
    
    private void Awake()
    {
        _currentHealth = _maxHealth;
        _audioSource = GetComponent<AudioSource>();
        
        // 모델 트랜스폼 찾기 (자신 또는 첫 번째 자식)
        _modelTransform = transform.childCount > 0 ? transform.GetChild(0) : transform;
        _originalPosition = _modelTransform.localPosition;
        _originalScale = _modelTransform.localScale;
        
        CacheRenderers();
    }
    
    private void OnDestroy()
    {
        // DOTween 클린업
        KillAllTweens();
    }
    
    /// <summary>
    /// 모든 트윈 정리
    /// </summary>
    private void KillAllTweens()
    {
        _hitSequence?.Kill();
        _colorTween?.Kill();
        _shakeTween?.Kill();
        _scaleTween?.Kill();
        _knockbackTween?.Kill();
    }
    
    /// <summary>
    /// 렌더러 캐싱 (피격 플래시용)
    /// </summary>
    private void CacheRenderers()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _materials = new Material[_renderers.Length];
        _originalColors = new Color[_renderers.Length];
        
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                // MaterialPropertyBlock 대신 직접 Material 인스턴스 사용
                _materials[i] = _renderers[i].material;
                if (_materials[i].HasProperty("_Color"))
                {
                    _originalColors[i] = _materials[i].color;
                }
                else if (_materials[i].HasProperty("_BaseColor"))
                {
                    _originalColors[i] = _materials[i].GetColor("_BaseColor");
                }
            }
        }
    }
    
    /// <summary>
    /// EnemyData 기반 초기화 (EnemyBase에서 호출)
    /// </summary>
    public void Initialize(
        float maxHealth,
        float armor,
        float headshotMultiplier,
        GameObject hitEffectPrefab,
        GameObject deathEffectPrefab,
        float hitFlashDuration,
        Color hitFlashColor,
        AudioClip hitSound,
        AudioClip deathSound)
    {
        _maxHealth = maxHealth;
        _armor = armor;
        _headshotMultiplier = headshotMultiplier;
        _hitEffectPrefab = hitEffectPrefab;
        _deathEffectPrefab = deathEffectPrefab;
        _hitFlashDuration = hitFlashDuration;
        _hitFlashColor = hitFlashColor;
        _hitSound = hitSound;
        _deathSound = deathSound;
        
        _currentHealth = _maxHealth;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        
        Debug.Log($"[EnemyHealth] {gameObject.name} initialized - HP: {_maxHealth}, Armor: {_armor:P0}");
    }
    
    /// <summary>
    /// 데미지 처리
    /// </summary>
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!IsAlive) return;
        if (damage <= 0f) return;
        
        // 방어력 적용
        float actualDamage = CalculateDamage(damage);
        
        _currentHealth = Mathf.Max(0f, _currentHealth - actualDamage);
        
        // 이벤트 발생
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        OnDamaged?.Invoke(actualDamage, hitPoint);
        
        // 피격 이펙트 및 사운드
        SpawnHitEffect(hitPoint, hitNormal);
        PlaySound(_hitSound);
        
        // DOTween 피격 애니메이션
        if (_useHitAnimation)
        {
            PlayHitAnimation(hitNormal);
        }
        
        Debug.Log($"[EnemyHealth] {gameObject.name} took {actualDamage:F1} damage (raw: {damage}). HP: {_currentHealth}/{_maxHealth}");
        
        // 사망 체크
        if (!IsAlive)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 헤드샷 데미지 처리
    /// </summary>
    public void TakeHeadshotDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        float headshotDamage = damage * _headshotMultiplier;
        TakeDamage(headshotDamage, hitPoint, hitNormal);
        Debug.Log($"[EnemyHealth] HEADSHOT! Multiplier: {_headshotMultiplier}x");
    }
    
    /// <summary>
    /// DOTween 피격 애니메이션 재생
    /// </summary>
    private void PlayHitAnimation(Vector3 hitNormal)
    {
        // 기존 트윈 정리
        KillAllTweens();
        
        // 원래 상태로 리셋
        ResetToOriginalState();
        
        // 시퀀스 생성
        _hitSequence = DOTween.Sequence();
        
        // 1. 색상 플래시 (빨간색으로 변했다가 원래대로)
        PlayColorFlash();
        
        // 2. 흔들림 효과
        PlayShakeEffect();
        
        // 3. 스케일 펀치 효과
        PlayScalePunch();
        
        // 4. 넉백 효과 (피격 방향 반대로)
        PlayKnockback(hitNormal);
    }
    
    /// <summary>
    /// 색상 플래시 애니메이션
    /// </summary>
    private void PlayColorFlash()
    {
        for (int i = 0; i < _materials.Length; i++)
        {
            if (_materials[i] == null) continue;
            
            Material mat = _materials[i];
            Color originalColor = _originalColors[i];
            
            // URP BaseColor 또는 Standard Color 지원
            string colorProperty = mat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            
            if (!mat.HasProperty(colorProperty)) continue;
            
            // 빨간색으로 즉시 변경 후 원래 색상으로 복귀
            mat.SetColor(colorProperty, _hitFlashColor);
            
            _colorTween = mat.DOColor(originalColor, colorProperty, _hitFlashDuration)
                .SetEase(Ease.OutQuad);
        }
    }
    
    /// <summary>
    /// 흔들림 효과 (위치 기반)
    /// </summary>
    private void PlayShakeEffect()
    {
        if (_modelTransform == null) return;
        
        _shakeTween = _modelTransform.DOShakePosition(
            duration: _shakeDuration,
            strength: _shakeStrength,
            vibrato: _shakeVibrato,
            randomness: 90f,
            snapping: false,
            fadeOut: true
        ).OnComplete(() => {
            _modelTransform.localPosition = _originalPosition;
        });
    }
    
    /// <summary>
    /// 스케일 펀치 효과 (커졌다가 원래대로)
    /// </summary>
    private void PlayScalePunch()
    {
        if (_modelTransform == null) return;
        
        Vector3 punchVector = Vector3.one * _punchScale;
        
        _scaleTween = _modelTransform.DOPunchScale(
            punch: punchVector,
            duration: _punchDuration,
            vibrato: 5,
            elasticity: 0.5f
        ).OnComplete(() => {
            _modelTransform.localScale = _originalScale;
        });
    }
    
    /// <summary>
    /// 넉백 효과 (피격 방향 반대로 밀림)
    /// </summary>
    private void PlayKnockback(Vector3 hitNormal)
    {
        if (_knockbackDistance <= 0f) return;
        
        // 피격 노말의 반대 방향 (XZ 평면만)
        Vector3 knockbackDir = new Vector3(-hitNormal.x, 0f, -hitNormal.z).normalized;
        
        if (knockbackDir.sqrMagnitude < 0.01f)
        {
            knockbackDir = -transform.forward;
        }
        
        Vector3 knockbackTarget = transform.position + knockbackDir * _knockbackDistance;
        
        _knockbackTween = transform.DOMove(knockbackTarget, _knockbackDuration)
            .SetEase(Ease.OutQuad);
    }
    
    /// <summary>
    /// 원래 상태로 리셋
    /// </summary>
    private void ResetToOriginalState()
    {
        if (_modelTransform != null)
        {
            _modelTransform.localPosition = _originalPosition;
            _modelTransform.localScale = _originalScale;
        }
        
        // 색상 복원
        for (int i = 0; i < _materials.Length; i++)
        {
            if (_materials[i] == null) continue;
            
            string colorProperty = _materials[i].HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            if (_materials[i].HasProperty(colorProperty))
            {
                _materials[i].SetColor(colorProperty, _originalColors[i]);
            }
        }
    }
    
    /// <summary>
    /// 방어력 적용된 데미지 계산
    /// </summary>
    private float CalculateDamage(float rawDamage)
    {
        float reducedDamage = rawDamage * (1f - _armor);
        return Mathf.Max(1f, reducedDamage); // 최소 1 데미지
    }
    
    /// <summary>
    /// 체력 회복
    /// </summary>
    public void Heal(float amount)
    {
        if (!IsAlive) return;
        if (amount <= 0f) return;
        
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        
        Debug.Log($"[EnemyHealth] {gameObject.name} healed {amount}. HP: {_currentHealth}/{_maxHealth}");
    }
    
    /// <summary>
    /// 체력 초기화 (리스폰용)
    /// </summary>
    public void ResetHealth()
    {
        _currentHealth = _maxHealth;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        ResetToOriginalState();
    }
    
    /// <summary>
    /// 최대 체력 설정
    /// </summary>
    public void SetMaxHealth(float newMaxHealth, bool resetCurrent = true)
    {
        _maxHealth = Mathf.Max(1f, newMaxHealth);
        if (resetCurrent)
        {
            _currentHealth = _maxHealth;
        }
        else
        {
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
        }
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }
    
    /// <summary>
    /// 피격 이펙트 생성
    /// </summary>
    private void SpawnHitEffect(Vector3 position, Vector3 normal)
    {
        if (_hitEffectPrefab == null) return;
        
        GameObject effect = Instantiate(
            _hitEffectPrefab,
            position,
            Quaternion.LookRotation(normal)
        );
        Destroy(effect, _hitEffectDuration);
    }
    
    /// <summary>
    /// 사운드 재생
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        
        if (_audioSource != null)
        {
            _audioSource.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
    
    /// <summary>
    /// 사망 처리
    /// </summary>
    private void Die()
    {
        Debug.Log($"[EnemyHealth] {gameObject.name} died!");
        
        // 트윈 정리
        KillAllTweens();
        
        // 사망 사운드 재생
        PlaySound(_deathSound);
        
        // 사망 이벤트 발생
        OnDeath?.Invoke();
        
        // 사망 애니메이션 (옵션)
        PlayDeathAnimation();
    }
    
    /// <summary>
    /// 사망 애니메이션
    /// </summary>
    private void PlayDeathAnimation()
    {
        // 사망 이펙트
        if (_deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                _deathEffectPrefab,
                transform.position,
                Quaternion.identity
            );
            Destroy(effect, 3f);
        }
        
        // DOTween 사망 애니메이션: 줄어들면서 사라짐
        Sequence deathSequence = DOTween.Sequence();
        
        // 색상을 어둡게
        foreach (var mat in _materials)
        {
            if (mat == null) continue;
            string colorProperty = mat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            if (mat.HasProperty(colorProperty))
            {
                deathSequence.Join(mat.DOColor(Color.black, colorProperty, 0.3f));
            }
        }
        
        // 스케일 축소
        if (_modelTransform != null)
        {
            deathSequence.Join(_modelTransform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack));
        }
        
        // 애니메이션 완료 후 오브젝트 제거
        deathSequence.OnComplete(() => {
            Destroy(gameObject);
        });
    }
    
    /// <summary>
    /// 즉시 사망 (강제 킬)
    /// </summary>
    public void InstantKill()
    {
        if (!IsAlive) return;
        
        _currentHealth = 0f;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        Die();
    }
}
