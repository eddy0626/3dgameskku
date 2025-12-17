using UnityEngine;
using DG.Tweening;

/// <summary>
/// 코인 픽업 - 마그넷 효과로 자동 수집
/// ResourceDrop의 간소화 버전 (코인 전용)
/// </summary>
[RequireComponent(typeof(Collider))]
public class CoinPickup : MonoBehaviour, ICollectable
{
    #region Inspector Fields
    [Header("코인 설정")]
    [SerializeField] private int coinValue = 1;
    [SerializeField] private ResourceType resourceType = ResourceType.Gold;

    [Header("마그넷 설정")]
    [Tooltip("마그넷 효과 범위")]
    [SerializeField] private float magnetRange = 5f;

    [Tooltip("마그넷 이동 속도")]
    [SerializeField] private float magnetSpeed = 10f;

    [Tooltip("자동 마그넷 활성화 (플레이어가 범위 내 들어오면 자동 흡수)")]
    [SerializeField] private bool autoMagnet = true;

    [Header("시각 효과")]
    [Tooltip("위아래 흔들림 높이")]
    [SerializeField] private float bobHeight = 0.3f;

    [Tooltip("위아래 흔들림 속도")]
    [SerializeField] private float bobSpeed = 2f;

    [Tooltip("회전 속도")]
    [SerializeField] private float rotateSpeed = 90f;

    [Header("수집 효과")]
    [SerializeField] private GameObject collectEffectPrefab;
    [SerializeField] private AudioClip collectSound;

    [Header("수명")]
    [Tooltip("자동 소멸 시간 (0 = 무한)")]
    [SerializeField] private float lifetime = 30f;
    #endregion

    #region Private Fields
    private Transform player;
    private bool isCollected;
    private bool isMagneting;
    private Vector3 startPosition;
    private float spawnTime;

    // DOTween
    private Tweener bobTween;
    private Tweener rotateTween;
    private Tweener magnetTween;
    #endregion

    #region Properties
    public int Value => coinValue;
    public ResourceType Type => resourceType;
    public bool IsMagneting => isMagneting;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        // 플레이어 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        startPosition = transform.position;
        spawnTime = Time.time;

        // 시각 효과 시작
        StartVisualEffects();

        // Collider를 Trigger로 설정
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Update()
    {
        if (isCollected) return;

        // 자동 마그넷 체크
        if (autoMagnet && !isMagneting && player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= magnetRange)
            {
                StartMagnet(player);
            }
        }

        // 수명 체크
        if (lifetime > 0 && Time.time - spawnTime > lifetime)
        {
            FadeAndDestroy();
        }
    }

    private void OnDestroy()
    {
        // DOTween 정리
        KillAllTweens();
    }
    #endregion

    #region Visual Effects
    private void StartVisualEffects()
    {
        // 위아래 흔들림 (Bob)
        if (bobHeight > 0)
        {
            bobTween = transform.DOMoveY(startPosition.y + bobHeight, 1f / bobSpeed)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        // 회전
        if (rotateSpeed > 0)
        {
            rotateTween = transform.DORotate(
                new Vector3(0, 360, 0),
                360f / rotateSpeed,
                RotateMode.FastBeyond360
            )
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
        }
    }

    private void StopVisualEffects()
    {
        bobTween?.Kill();
        rotateTween?.Kill();
    }

    private void KillAllTweens()
    {
        bobTween?.Kill();
        rotateTween?.Kill();
        magnetTween?.Kill();
        transform.DOKill();
    }
    #endregion

    #region Magnet System
    /// <summary>
    /// 마그넷 효과 시작 - 타겟으로 끌려감
    /// </summary>
    public void StartMagnet(Transform target)
    {
        if (isCollected || isMagneting) return;

        isMagneting = true;
        player = target;

        // 시각 효과 중지
        StopVisualEffects();

        // 타겟으로 이동
        MoveToTarget();
    }

    private void MoveToTarget()
    {
        if (player == null)
        {
            isMagneting = false;
            return;
        }

        // 현재 거리 기반 duration 계산
        float distance = Vector3.Distance(transform.position, player.position);
        float duration = Mathf.Max(0.1f, distance / magnetSpeed);

        // 부드러운 곡선 이동
        magnetTween = transform.DOMove(player.position, duration)
            .SetEase(Ease.InQuad)
            .OnUpdate(() =>
            {
                // 실시간으로 타겟 위치 추적
                if (player != null && magnetTween != null && magnetTween.IsActive())
                {
                    // DOTween의 endValue를 동적으로 변경
                    float remainingDist = Vector3.Distance(transform.position, player.position);

                    // 충분히 가까우면 수집
                    if (remainingDist < 0.5f)
                    {
                        Collect();
                    }
                }
            })
            .OnComplete(() =>
            {
                if (!isCollected)
                {
                    Collect();
                }
            });
    }

    /// <summary>
    /// 마그넷 중지
    /// </summary>
    public void StopMagnet()
    {
        if (!isMagneting) return;

        isMagneting = false;
        magnetTween?.Kill();

        // 시각 효과 재시작
        startPosition = transform.position;
        StartVisualEffects();
    }
    #endregion

    #region Collection
    /// <summary>
    /// 코인 수집
    /// </summary>
    private void Collect()
    {
        if (isCollected) return;

        isCollected = true;
        KillAllTweens();

        // ResourceManager에 추가
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddCoins(coinValue);
        }

        // 수집 효과
        PlayCollectEffect();

        // 사운드
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, 0.5f);
        }

        // 수집 애니메이션 후 파괴
        transform.DOScale(0f, 0.15f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }

    private void PlayCollectEffect()
    {
        if (collectEffectPrefab != null)
        {
            GameObject effect = Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    private void FadeAndDestroy()
    {
        if (isCollected) return;

        isCollected = true;
        KillAllTweens();

        // 페이드 아웃
        transform.DOScale(0f, 0.5f)
            .SetEase(Ease.InQuad)
            .OnComplete(() => Destroy(gameObject));
    }
    #endregion

    #region ICollectable Implementation
    public void OnCollect(Transform collector)
    {
        Collect();
    }
    #endregion

    #region Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        // 플레이어와 충돌 시 즉시 수집
        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// 코인 값 설정
    /// </summary>
    public void SetValue(int value)
    {
        coinValue = Mathf.Max(1, value);
    }

    /// <summary>
    /// 리소스 타입 설정
    /// </summary>
    public void SetResourceType(ResourceType type)
    {
        resourceType = type;
    }

    /// <summary>
    /// 마그넷 범위 설정
    /// </summary>
    public void SetMagnetRange(float range)
    {
        magnetRange = Mathf.Max(0f, range);
    }
    #endregion

    #region Static Factory
    /// <summary>
    /// 코인 스폰
    /// </summary>
    public static CoinPickup Spawn(Vector3 position, int value = 1, GameObject prefab = null)
    {
        GameObject obj;

        if (prefab != null)
        {
            obj = Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            // 기본 오브젝트 생성
            obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
            obj.name = "Coin";

            // 골드 색상 적용
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0.84f, 0f); // Gold
            }
        }

        CoinPickup coin = obj.GetComponent<CoinPickup>();
        if (coin == null)
        {
            coin = obj.AddComponent<CoinPickup>();
        }

        coin.SetValue(value);
        return coin;
    }

    /// <summary>
    /// 여러 코인 스폰 (버스트 효과)
    /// </summary>
    public static void SpawnBurst(Vector3 position, int totalValue, int coinCount = 5, float radius = 1f, GameObject prefab = null)
    {
        int valuePerCoin = Mathf.Max(1, totalValue / coinCount);
        int remainder = totalValue % coinCount;

        for (int i = 0; i < coinCount; i++)
        {
            // 랜덤 오프셋
            Vector3 randomOffset = Random.insideUnitSphere * radius;
            randomOffset.y = Mathf.Abs(randomOffset.y) + 0.5f;

            Vector3 spawnPos = position + randomOffset;
            int value = valuePerCoin + (i < remainder ? 1 : 0);

            CoinPickup coin = Spawn(spawnPos, value, prefab);

            if (coin != null)
            {
                // 튀어오르는 효과
                Vector3 targetPos = position + Random.insideUnitSphere * radius;
                targetPos.y = position.y;

                coin.transform.DOJump(targetPos, 1.5f, 1, 0.5f)
                    .SetEase(Ease.OutQuad);
            }
        }
    }
    #endregion

    #region Gizmos
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 마그넷 범위
        Gizmos.color = new Color(1f, 0.84f, 0f, 0.3f); // Gold with alpha
        Gizmos.DrawWireSphere(transform.position, magnetRange);

        // 라벨
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            $"Value: {coinValue}\nMagnet: {magnetRange}m"
        );
    }
#endif
    #endregion
}
