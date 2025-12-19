using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SquadSurvival.Core;

namespace SquadSurvival.UI
{
    /// <summary>
    /// 웨이브 UI 관리
    /// 웨이브 번호, 적 카운트, 준비 시간 표시
    /// </summary>
    public class WaveUI : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI enemyCountText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image progressBar;

        [Header("웨이브 알림")]
        [SerializeField] private GameObject waveAnnouncementPanel;
        [SerializeField] private TextMeshProUGUI waveAnnouncementText;
        [SerializeField] private float announcementDuration = 2f;

        [Header("애니메이션 설정")]
        [SerializeField] private float punchScale = 1.2f;
        [SerializeField] private float punchDuration = 0.3f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color dangerColor = Color.red;

        [Header("사운드 (선택)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip waveStartSound;
        [SerializeField] private AudioClip waveCompleteSound;

        private Tween timerTween;
        private Tween announcementTween;

        private void Start()
        {
            // UI 참조 자동 탐색
            AutoFindUIReferences();

            // SquadSurvivalManager 이벤트 구독
            if (SquadSurvivalManager.Instance != null)
            {
                SquadSurvivalManager.Instance.OnWaveStart.AddListener(OnWaveStart);
                SquadSurvivalManager.Instance.OnWaveComplete.AddListener(OnWaveComplete);
                SquadSurvivalManager.Instance.OnStateChanged.AddListener(OnStateChanged);
                SquadSurvivalManager.Instance.OnEnemyCountChanged.AddListener(OnEnemyCountChanged);
            }

            // 초기 상태
            HideAnnouncement();
            if (timerText != null) timerText.gameObject.SetActive(false);
        }

        private void Update()
        {
            UpdateTimerDisplay();
        }

        /// <summary>
        /// 타이머 표시 업데이트
        /// </summary>
        private void UpdateTimerDisplay()
        {
            if (SquadSurvivalManager.Instance == null) return;

            var state = SquadSurvivalManager.Instance.CurrentState;

            if (state == SquadSurvivalManager.GameState.Preparing)
            {
                float time = SquadSurvivalManager.Instance.GetPrepareTimeRemaining();
                ShowTimer("준비", time);
            }
            else if (state == SquadSurvivalManager.GameState.WaveComplete)
            {
                float time = SquadSurvivalManager.Instance.GetRestTimeRemaining();
                ShowTimer("휴식", time);
            }
            else
            {
                HideTimer();
            }
        }

        /// <summary>
        /// 타이머 표시
        /// </summary>
        private void ShowTimer(string label, float time)
        {
            if (timerText == null) return;

            timerText.gameObject.SetActive(true);
            timerText.text = $"{label}: {Mathf.CeilToInt(time)}초";

            // 3초 이하면 경고 색상
            if (time <= 3f)
            {
                timerText.color = dangerColor;
            }
            else if (time <= 5f)
            {
                timerText.color = warningColor;
            }
            else
            {
                timerText.color = normalColor;
            }
        }

        /// <summary>
        /// 타이머 숨기기
        /// </summary>
        private void HideTimer()
        {
            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 웨이브 텍스트 업데이트
        /// </summary>
        public void UpdateWaveText(int wave)
        {
            if (waveText == null) return;

            if (wave <= 0)
            {
                waveText.text = "READY";
            }
            else
            {
                waveText.text = $"WAVE {wave}";
            }

            // 펀치 애니메이션
            waveText.transform.DOKill();
            waveText.transform.localScale = Vector3.one;
            waveText.transform.DOPunchScale(Vector3.one * (punchScale - 1f), punchDuration, 1, 0.5f);
        }

        /// <summary>
        /// 적 카운트 업데이트
        /// </summary>
        public void UpdateEnemyCount(int killed, int total)
        {
            if (enemyCountText == null) return;

            if (total <= 0)
            {
                enemyCountText.text = "적: -/-";
                if (progressBar != null) progressBar.fillAmount = 0f;
            }
            else
            {
                enemyCountText.text = $"적: {killed}/{total}";

                // 프로그레스 바 업데이트
                if (progressBar != null)
                {
                    float progress = (float)killed / total;
                    progressBar.DOKill();
                    progressBar.DOFillAmount(progress, 0.3f);

                    // 색상 변경
                    if (progress >= 0.8f)
                    {
                        progressBar.color = Color.green;
                    }
                    else if (progress >= 0.5f)
                    {
                        progressBar.color = Color.yellow;
                    }
                    else
                    {
                        progressBar.color = Color.red;
                    }
                }

                // 남은 적이 적으면 경고 색상
                int remaining = total - killed;
                if (remaining <= 3 && remaining > 0)
                {
                    enemyCountText.color = warningColor;
                }
                else
                {
                    enemyCountText.color = normalColor;
                }
            }
        }

        /// <summary>
        /// 웨이브 알림 표시
        /// </summary>
        public void ShowWaveAnnouncement(int wave, bool isComplete = false)
        {
            if (waveAnnouncementPanel == null || waveAnnouncementText == null) return;

            // 기존 애니메이션 취소
            announcementTween?.Kill();
            waveAnnouncementPanel.transform.DOKill();

            waveAnnouncementPanel.SetActive(true);

            if (isComplete)
            {
                waveAnnouncementText.text = $"WAVE {wave}\nCOMPLETE!";
                waveAnnouncementText.color = Color.green;
            }
            else
            {
                waveAnnouncementText.text = $"WAVE {wave}\nSTART!";
                waveAnnouncementText.color = Color.white;
            }

            // 등장 애니메이션
            waveAnnouncementPanel.transform.localScale = Vector3.zero;
            waveAnnouncementPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            // 자동 숨김
            announcementTween = DOVirtual.DelayedCall(announcementDuration, HideAnnouncement);
        }

        /// <summary>
        /// 웨이브 알림 숨기기
        /// </summary>
        public void HideAnnouncement()
        {
            if (waveAnnouncementPanel == null) return;

            waveAnnouncementPanel.transform.DOKill();
            waveAnnouncementPanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => waveAnnouncementPanel.SetActive(false));
        }

        #region Event Handlers

        private void OnWaveStart(int wave)
        {
            UpdateWaveText(wave);
            ShowWaveAnnouncement(wave, false);

            // 사운드 재생
            if (audioSource != null && waveStartSound != null)
            {
                audioSource.PlayOneShot(waveStartSound);
            }

#if UNITY_EDITOR
            Debug.Log($"[WaveUI] 웨이브 {wave} 시작 UI 업데이트");
#endif
        }

        private void OnWaveComplete(int wave)
        {
            ShowWaveAnnouncement(wave, true);

            // 사운드 재생
            if (audioSource != null && waveCompleteSound != null)
            {
                audioSource.PlayOneShot(waveCompleteSound);
            }

#if UNITY_EDITOR
            Debug.Log($"[WaveUI] 웨이브 {wave} 완료 UI 업데이트");
#endif
        }

        private void OnStateChanged(SquadSurvivalManager.GameState newState)
        {
            switch (newState)
            {
                case SquadSurvivalManager.GameState.Preparing:
                    if (SquadSurvivalManager.Instance.CurrentWave == 0)
                    {
                        UpdateWaveText(0);
                    }
                    break;

                case SquadSurvivalManager.GameState.GameOver:
                case SquadSurvivalManager.GameState.Victory:
                    HideTimer();
                    HideAnnouncement();
                    break;
            }
        }

        private void OnEnemyCountChanged(int killed, int total)
        {
            UpdateEnemyCount(killed, total);
        }

        #endregion

        /// <summary>
        /// UI 참조 자동 탐색
        /// </summary>
        [ContextMenu("Auto Find UI References")]
        public void AutoFindUIReferences()
        {
            Transform parent = transform;

            // WavePanel에서 찾기
            var wavePanel = parent.Find("WavePanel");
            if (wavePanel != null)
            {
                waveText = wavePanel.Find("WaveText")?.GetComponent<TextMeshProUGUI>();
                enemyCountText = wavePanel.Find("EnemyCountText")?.GetComponent<TextMeshProUGUI>();
                timerText = wavePanel.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
                progressBar = wavePanel.Find("ProgressBar")?.GetComponent<Image>();
            }

            // WaveAnnouncementPanel 찾기
            waveAnnouncementPanel = parent.Find("WaveAnnouncementPanel")?.gameObject;
            if (waveAnnouncementPanel != null)
            {
                waveAnnouncementText = waveAnnouncementPanel.GetComponentInChildren<TextMeshProUGUI>();
            }

#if UNITY_EDITOR
            Debug.Log("[WaveUI] UI 참조 자동 설정 완료");
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void OnDestroy()
        {
            // 트윈 정리
            timerTween?.Kill();
            announcementTween?.Kill();

            // 이벤트 구독 해제
            if (SquadSurvivalManager.Instance != null)
            {
                SquadSurvivalManager.Instance.OnWaveStart.RemoveListener(OnWaveStart);
                SquadSurvivalManager.Instance.OnWaveComplete.RemoveListener(OnWaveComplete);
                SquadSurvivalManager.Instance.OnStateChanged.RemoveListener(OnStateChanged);
                SquadSurvivalManager.Instance.OnEnemyCountChanged.RemoveListener(OnEnemyCountChanged);
            }
        }
    }
}
