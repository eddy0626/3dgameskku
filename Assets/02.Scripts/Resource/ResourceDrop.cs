using UnityEngine;
using DG.Tweening;

/// <summary>
/// Collectible resource drop with magnet attraction effect
/// </summary>
public class ResourceDrop : MonoBehaviour, ICollectable
{
    #region Inspector Fields
    [Header("Data")]
    [SerializeField] private ResourceData resourceData;
    
    [Header("Override Settings")]
    [SerializeField] private bool useOverrideAmount;
    [SerializeField] private int overrideAmount;
    
    [Header("Visual")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private ParticleSystem collectParticle;
    #endregion

    #region Private Fields
    private bool isCollecting;
    private bool isMagneting;
    private Transform magnetTarget;
    private int amount;
    private float spawnTime;
    private Vector3 startPosition;
    
    // Animation
    private Tweener bobTween;
    private Tweener rotateTween;
    #endregion

    #region Properties
    public ResourceType Type => resourceData != null ? resourceData.type : ResourceType.Gold;
    public int Amount => amount;
    public ResourceData Data => resourceData;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (isCollecting) return;
        
        // 수명 체크
        if (resourceData != null && resourceData.lifetime > 0)
        {
            if (Time.time - spawnTime > resourceData.lifetime)
            {
                FadeAndDestroy();
            }
        }
    }

    private void OnDestroy()
    {
        // DOTween 정리
        bobTween?.Kill();
        rotateTween?.Kill();
    }
    #endregion

    #region Initialization
    public void Initialize()
    {
        if (resourceData == null)
        {
            Debug.LogWarning("[ResourceDrop] No ResourceData assigned!");
            return;
        }
        
        // 양 결정
        amount = useOverrideAmount ? overrideAmount : resourceData.GetAmount();
        
        spawnTime = Time.time;
        startPosition = transform.position;
        isCollecting = false;
        isMagneting = false;
        
        // 시각 효과 시작
        StartVisualEffects();
    }

    public void Initialize(ResourceData data, int customAmount = -1)
    {
        resourceData = data;
        
        if (customAmount > 0)
        {
            useOverrideAmount = true;
            overrideAmount = customAmount;
        }
        
        Initialize();
    }
    #endregion

    #region Visual Effects
    private void StartVisualEffects()
    {
        if (resourceData == null) return;
        
        Transform target = visualTransform != null ? visualTransform : transform;
        
        // 위아래 흔들림
        if (resourceData.bobHeight > 0)
        {
            bobTween = target.DOMoveY(
                startPosition.y + resourceData.bobHeight, 
                1f / resourceData.bobSpeed
            )
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
        }
        
        // 회전
        if (resourceData.rotateSpeed > 0)
        {
            rotateTween = target.DORotate(
                new Vector3(0, 360, 0), 
                360f / resourceData.rotateSpeed, 
                RotateMode.FastBeyond360
            )
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
        }
    }
    #endregion

    #region ICollectable Implementation
    public void OnCollect(Transform collector)
    {
        if (isCollecting) return;
        
        isCollecting = true;
        
        // 트윈 정지
        bobTween?.Kill();
        rotateTween?.Kill();
        
        // 리소스 추가
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource(Type, amount);
        }
        
        // 수집 효과
        PlayCollectEffect();
        
        // 사운드
        if (resourceData != null && resourceData.collectSound != null)
        {
            AudioSource.PlayClipAtPoint(resourceData.collectSound, transform.position);
        }
        
        // 수집 애니메이션 후 파괴
        transform.DOScale(0f, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }
    #endregion

    #region Magnet System
    public void StartMagnet(Transform target)
    {
        if (isCollecting || isMagneting) return;
        
        isMagneting = true;
        magnetTarget = target;
        
        // 트윈 정지
        bobTween?.Kill();
        rotateTween?.Kill();
        
        // 마그넷 이동 시작
        MoveToTarget();
    }

    private void MoveToTarget()
    {
        if (magnetTarget == null)
        {
            isMagneting = false;
            return;
        }
        
        float speed = resourceData != null ? resourceData.magnetSpeed : 15f;
        
        // 타겟까지의 거리 기반 duration 계산
        float distance = Vector3.Distance(transform.position, magnetTarget.position);
        float duration = distance / speed;
        
        transform.DOMove(magnetTarget.position, duration)
            .SetEase(Ease.InQuad)
            .OnUpdate(() =>
            {
                // 타겟 위치 갱신
                if (magnetTarget != null && !isCollecting)
                {
                    float remainingDist = Vector3.Distance(transform.position, magnetTarget.position);
                    if (remainingDist < 0.5f)
                    {
                        OnCollect(magnetTarget);
                    }
                }
            })
            .OnComplete(() =>
            {
                if (!isCollecting && magnetTarget != null)
                {
                    OnCollect(magnetTarget);
                }
            });
    }

    public void StopMagnet()
    {
        if (!isMagneting) return;
        
        isMagneting = false;
        magnetTarget = null;
        
        transform.DOKill();
        
        // 시각 효과 재시작
        startPosition = transform.position;
        StartVisualEffects();
    }
    #endregion

    #region Effects
    private void PlayCollectEffect()
    {
        if (collectParticle != null)
        {
            collectParticle.transform.SetParent(null);
            collectParticle.Play();
            Destroy(collectParticle.gameObject, collectParticle.main.duration);
        }
    }

    private void FadeAndDestroy()
    {
        if (isCollecting) return;
        
        isCollecting = true; // 추가 수집 방지
        
        // 페이드 아웃
        if (TryGetComponent<Renderer>(out var renderer))
        {
            Material mat = renderer.material;
            mat.DOFade(0f, 0.5f).OnComplete(() => Destroy(gameObject));
        }
        else
        {
            transform.DOScale(0f, 0.5f).OnComplete(() => Destroy(gameObject));
        }
    }
    #endregion

    #region Static Factory
    public static ResourceDrop Spawn(ResourceData data, Vector3 position, int amount = -1)
    {
        if (data == null || data.prefab == null)
        {
            Debug.LogError("[ResourceDrop] Invalid ResourceData for spawning!");
            return null;
        }
        
        GameObject obj = Instantiate(data.prefab, position, Quaternion.identity);
        ResourceDrop drop = obj.GetComponent<ResourceDrop>();
        
        if (drop == null)
        {
            drop = obj.AddComponent<ResourceDrop>();
        }
        
        drop.Initialize(data, amount);
        return drop;
    }

    public static void SpawnWithBurst(ResourceData data, Vector3 position, int count, float radius = 1f)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * radius;
            randomOffset.y = Mathf.Abs(randomOffset.y); // 위로만
            
            Vector3 spawnPos = position + randomOffset;
            
            ResourceDrop drop = Spawn(data, spawnPos);
            
            if (drop != null)
            {
                // 튀어오르는 효과
                drop.transform.DOJump(
                    spawnPos + Random.insideUnitSphere * radius * 0.5f,
                    1f,
                    1,
                    0.5f
                ).SetEase(Ease.OutQuad);
            }
        }
    }
    #endregion
}