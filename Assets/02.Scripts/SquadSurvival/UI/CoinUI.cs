using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SquadSurvival.Economy;

namespace SquadSurvival.UI
{
    /// <summary>
    /// 코인 UI 관리
    /// 코인 표시, 획득/소비 애니메이션
    /// </summary>
    public class CoinUI : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private Image coinIcon;
        [SerializeField] private RectTransform coinContainer;

        [Header("애니메이션")]
        [SerializeField] private float punchScale = 1.3f;
        [SerializeField] private float punchDuration = 0.2f;
        [SerializeField] private Color earnedColor = Color.yellow;
        [SerializeField] private Color spentColor = Color.red;
        [SerializeField] private float colorFlashDuration = 0.3f;

        [Header("숫자 애니메이션")]
        [SerializeField] private bool animateNumber = true;
        [SerializeField] private float numberAnimDuration = 0.5f;

        [Header("플라이 텍스트")]
        [SerializeField] private GameObject flyTextPrefab;
        [SerializeField] private Transform flyTextContainer;
        [SerializeField] private float flyTextDuration = 1f;
        [SerializeField] private float flyTextDistance = 50f;

        private int displayedCoins = 0;
        private Tweener numberTween;
        private Tweener colorTween;
        private Color originalColor;

        private void Awake()
        {
            if (coinText != null)
            {
                originalColor = coinText.color;
            }
        }

        private void Start()
        {
            // CoinManager 이벤트 구독
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.OnCoinsChanged.AddListener(OnCoinsChanged);
                CoinManager.Instance.OnCoinsEarned.AddListener(OnCoinsEarned);
                CoinManager.Instance.OnCoinsSpent.AddListener(OnCoinsSpent);
                CoinManager.Instance.OnInsufficientFunds.AddListener(OnInsufficientFunds);

                // 초기 값 설정
                UpdateCoinText(CoinManager.Instance.CurrentCoins, false);
            }
        }

        /// <summary>
        /// 코인 변경 시 호출
        /// </summary>
        private void OnCoinsChanged(int newAmount)
        {
            UpdateCoinText(newAmount, animateNumber);
        }

        /// <summary>
        /// 코인 획득 시 호출
        /// </summary>
        private void OnCoinsEarned(int amount)
        {
            // 펀치 애니메이션
            PlayPunchAnimation();

            // 색상 플래시
            FlashColor(earnedColor);

            // 플라이 텍스트
            ShowFlyText($"+{amount}", earnedColor);
        }

        /// <summary>
        /// 코인 소비 시 호출
        /// </summary>
        private void OnCoinsSpent(int amount)
        {
            // 색상 플래시
            FlashColor(spentColor);

            // 플라이 텍스트
            ShowFlyText($"-{amount}", spentColor);
        }

        /// <summary>
        /// 잔액 부족 시 호출
        /// </summary>
        private void OnInsufficientFunds()
        {
            // 흔들림 애니메이션
            if (coinContainer != null)
            {
                coinContainer.DOKill();
                coinContainer.DOShakePosition(0.3f, new Vector3(10f, 0f, 0f), 20, 90f, false, true);
            }

            // 빨간색 플래시
            FlashColor(Color.red);
        }

        /// <summary>
        /// 코인 텍스트 업데이트
        /// </summary>
        private void UpdateCoinText(int amount, bool animate)
        {
            if (coinText == null) return;

            if (animate && animateNumber)
            {
                // 숫자 애니메이션
                numberTween?.Kill();
                numberTween = DOTween.To(
                    () => displayedCoins,
                    x =>
                    {
                        displayedCoins = x;
                        coinText.text = displayedCoins.ToString("N0");
                    },
                    amount,
                    numberAnimDuration
                ).SetEase(Ease.OutQuad);
            }
            else
            {
                displayedCoins = amount;
                coinText.text = amount.ToString("N0");
            }
        }

        /// <summary>
        /// 펀치 애니메이션
        /// </summary>
        private void PlayPunchAnimation()
        {
            if (coinContainer == null) return;

            coinContainer.DOKill();
            coinContainer.localScale = Vector3.one;
            coinContainer.DOPunchScale(Vector3.one * (punchScale - 1f), punchDuration, 1, 0.5f);
        }

        /// <summary>
        /// 색상 플래시
        /// </summary>
        private void FlashColor(Color flashColor)
        {
            if (coinText == null) return;

            colorTween?.Kill();
            coinText.color = flashColor;
            colorTween = coinText.DOColor(originalColor, colorFlashDuration);
        }

        /// <summary>
        /// 플라이 텍스트 표시
        /// </summary>
        private void ShowFlyText(string text, Color color)
        {
            if (flyTextPrefab == null) return;

            Transform parent = flyTextContainer != null ? flyTextContainer : transform;
            GameObject flyObj = Instantiate(flyTextPrefab, parent);

            var flyText = flyObj.GetComponent<TextMeshProUGUI>();
            if (flyText == null)
            {
                flyText = flyObj.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (flyText != null)
            {
                flyText.text = text;
                flyText.color = color;

                // 위로 올라가면서 페이드 아웃
                var rectTransform = flyObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector3 startPos = rectTransform.anchoredPosition;
                    Vector3 endPos = startPos + Vector3.up * flyTextDistance;

                    Sequence flySequence = DOTween.Sequence();
                    flySequence.Append(rectTransform.DOAnchorPos(endPos, flyTextDuration));
                    flySequence.Join(flyText.DOFade(0f, flyTextDuration));
                    flySequence.OnComplete(() => Destroy(flyObj));
                }
                else
                {
                    Destroy(flyObj, flyTextDuration);
                }
            }
            else
            {
                Destroy(flyObj);
            }
        }

        /// <summary>
        /// 직접 코인 설정 (UI만 업데이트)
        /// </summary>
        public void SetCoins(int amount, bool animate = false)
        {
            UpdateCoinText(amount, animate);
        }

        /// <summary>
        /// UI 참조 자동 탐색
        /// </summary>
        [ContextMenu("Auto Find UI References")]
        public void AutoFindUIReferences()
        {
            coinContainer = GetComponent<RectTransform>();

            // CoinText 찾기
            var textObj = transform.Find("CoinText");
            if (textObj != null)
            {
                coinText = textObj.GetComponent<TextMeshProUGUI>();
            }

            // CoinIcon 찾기
            var iconObj = transform.Find("CoinIcon");
            if (iconObj != null)
            {
                coinIcon = iconObj.GetComponent<Image>();
            }

            // FlyTextContainer 찾기
            flyTextContainer = transform.Find("FlyTextContainer");

#if UNITY_EDITOR
            Debug.Log("[CoinUI] UI 참조 자동 설정 완료");
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void OnDestroy()
        {
            numberTween?.Kill();
            colorTween?.Kill();

            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.OnCoinsChanged.RemoveListener(OnCoinsChanged);
                CoinManager.Instance.OnCoinsEarned.RemoveListener(OnCoinsEarned);
                CoinManager.Instance.OnCoinsSpent.RemoveListener(OnCoinsSpent);
                CoinManager.Instance.OnInsufficientFunds.RemoveListener(OnInsufficientFunds);
            }
        }
    }
}
