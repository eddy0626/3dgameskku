using UnityEngine;
using DG.Tweening;

namespace SquadSurvival.Economy
{
    /// <summary>
    /// 필드에서 획득 가능한 코인 픽업 아이템
    /// </summary>
    public class CoinPickup : MonoBehaviour
    {
        [Header("코인 설정")]
        [SerializeField] private int coinAmount = 10;
        [SerializeField] private bool autoCollect = true;
        [SerializeField] private float collectRadius = 2f;
        [SerializeField] private LayerMask playerLayer;

        [Header("비주얼")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobHeight = 0.3f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private ParticleSystem collectEffect;
        [SerializeField] private GameObject visualObject;

        [Header("자석 효과")]
        [SerializeField] private bool magnetEnabled = true;
        [SerializeField] private float magnetRange = 5f;
        [SerializeField] private float magnetSpeed = 10f;

        [Header("수명")]
        [SerializeField] private float lifetime = 30f;
        [SerializeField] private float blinkStartTime = 25f;
        [SerializeField] private float blinkInterval = 0.2f;

        [Header("사운드")]
        [SerializeField] private AudioClip collectSound;

        private Transform playerTransform;
        private Vector3 startPosition;
        private float spawnTime;
        private bool isCollected = false;
        private bool isBlinking = false;
        private Renderer[] renderers;

        // 버스트 드롭 관련
        private bool isBurstMode = false;
        private bool isBurstAnimating = false;
        private Vector3 burstTargetPos;
        private float burstJumpHeight;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();

            // 자동으로 CoinVisual 자식 찾기
            if (visualObject == null)
            {
                Transform visual = transform.Find("CoinVisual");
                if (visual != null)
                {
                    visualObject = visual.gameObject;
                }
                else if (transform.childCount > 0)
                {
                    // 첫 번째 자식을 비주얼로 사용
                    visualObject = transform.GetChild(0).gameObject;
                }
                else
                {
                    visualObject = gameObject;
                }
            }
        }

        private void Start()
        {
            startPosition = transform.position;
            spawnTime = Time.time;

            // 플레이어 찾기
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }

            // 플레이어 레이어 자동 설정
            if (playerLayer == 0)
            {
                playerLayer = LayerMask.GetMask("Player");
            }

            // 스폰 애니메이션
            PlaySpawnAnimation();

            // 수명 타이머
            if (lifetime > 0)
            {
                Invoke(nameof(DestroySelf), lifetime);
            }
        }

        private void Update()
        {
            if (isCollected) return;

            // 버스트 애니메이션 중에는 다른 동작 스킵
            if (isBurstAnimating)
            {
                // 회전만 적용
                visualObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
                return;
            }

            // 회전
            visualObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            // 위아래 움직임
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            visualObject.transform.position = startPosition + Vector3.up * bobOffset;

            // 자석 효과
            if (magnetEnabled && playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance < magnetRange)
                {
                    Vector3 direction = (playerTransform.position - transform.position).normalized;
                    float speed = magnetSpeed * (1f - distance / magnetRange);
                    transform.position += direction * speed * Time.deltaTime;
                    startPosition = transform.position - Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                }
            }

            // 자동 수집
            if (autoCollect)
            {
                CheckAutoCollect();
            }

            // 깜빡임 (수명 끝나갈 때)
            if (!isBlinking && lifetime > 0 && Time.time - spawnTime > blinkStartTime)
            {
                isBlinking = true;
                InvokeRepeating(nameof(ToggleVisibility), 0f, blinkInterval);
            }
        }

        /// <summary>
        /// 스폰 애니메이션
        /// </summary>
        private void PlaySpawnAnimation()
        {
            visualObject.transform.localScale = Vector3.zero;
            visualObject.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

            // 버스트 모드일 때는 타겟 위치로 점프
            if (isBurstMode)
            {
                isBurstAnimating = true;
                transform.DOJump(burstTargetPos, burstJumpHeight, 1, 0.4f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        startPosition = transform.position;
                        isBurstAnimating = false;
                    });
            }
            else
            {
                // 기본 위로 튀어오르는 효과
                transform.DOJump(startPosition + Vector3.up * 0.5f, 1f, 1, 0.5f)
                    .OnComplete(() => startPosition = transform.position);
            }
        }

        /// <summary>
        /// 버스트 드롭 타겟 설정
        /// </summary>
        public void SetBurstTarget(Vector3 targetPos, float jumpHeight)
        {
            isBurstMode = true;
            burstTargetPos = targetPos;
            burstJumpHeight = jumpHeight;
        }

        /// <summary>
        /// 자동 수집 체크
        /// </summary>
        private void CheckAutoCollect()
        {
            if (playerTransform == null) return;

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance < collectRadius)
            {
                Collect();
            }
        }

        /// <summary>
        /// 코인 수집
        /// </summary>
        public void Collect()
        {
            if (isCollected) return;
            isCollected = true;

            // 코인 추가
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.AddCoins(coinAmount, "픽업");
            }

            // 이펙트
            PlayCollectEffect();

            // 사운드
            PlayCollectSound();

            // 수집 애니메이션
            visualObject.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
            transform.DOMove(playerTransform != null ? playerTransform.position + Vector3.up : transform.position + Vector3.up, 0.2f)
                .OnComplete(() => Destroy(gameObject));

#if UNITY_EDITOR
            Debug.Log($"[CoinPickup] {coinAmount} 코인 획득!");
#endif
        }

        /// <summary>
        /// 수집 이펙트
        /// </summary>
        private void PlayCollectEffect()
        {
            if (collectEffect != null)
            {
                var effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, effect.main.duration + 1f);
            }
        }

        /// <summary>
        /// 수집 사운드
        /// </summary>
        private void PlayCollectSound()
        {
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }
        }

        /// <summary>
        /// 가시성 토글 (깜빡임용)
        /// </summary>
        private void ToggleVisibility()
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = !renderer.enabled;
                }
            }
        }

        /// <summary>
        /// 코인 양 설정
        /// </summary>
        public void SetAmount(int amount)
        {
            coinAmount = Mathf.Max(1, amount);
        }

        /// <summary>
        /// 자기 파괴
        /// </summary>
        private void DestroySelf()
        {
            if (!isCollected)
            {
                // 사라지는 애니메이션
                visualObject.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack)
                    .OnComplete(() => Destroy(gameObject));
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            // 플레이어 태그 체크
            if (other.CompareTag("Player"))
            {
                Collect();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 수집 범위
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collectRadius);

            // 자석 범위
            if (magnetEnabled)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, magnetRange);
            }
        }
    }
}
