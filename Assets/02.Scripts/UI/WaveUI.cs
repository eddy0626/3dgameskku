using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 웨이브 UI 컴포넌트
/// - WaveManager 연동
/// - "Wave X / Y" 텍스트 표시
/// - 웨이브 시작 시 알림 애니메이션
/// - 준비 카운트다운 표시
/// </summary>
public class WaveUI : MonoBehaviour
{
    #region Inspector Fields
    [Header("웨이브 텍스트")]
    [Tooltip("웨이브 번호 텍스트")]
    [SerializeField] private TextMeshProUGUI _waveText;

    [Tooltip("웨이브 알림 텍스트 (중앙 표시용)")]
    [SerializeField] private TextMeshProUGUI _waveAnnouncementText;

    [Tooltip("준비 카운트다운 텍스트")]
    [SerializeField] private TextMeshProUGUI _countdownText;

    [Header("적 카운터")]
    [Tooltip("남은 적 수 텍스트")]
    [SerializeField] private TextMeshProUGUI _enemyCountText;

    [Tooltip("적 카운터 프로그레스 바")]
    [SerializeField] private Image _enemyProgressBar;

    [Header("알림 애니메이션")]
    [Tooltip("웨이브 시작 알림 지속 시간")]
    [SerializeField] private float _announcementDuration = 2f;

    [Tooltip("알림 텍스트 크기")]
    [SerializeField] private float _announcementScale = 1.5f;

    [Header("포맷 설정")]
    [Tooltip("웨이브 텍스트 포맷 (무한 모드일 때)")]
    [SerializeField] private string _infiniteModeFormat = "Wave {0}";

    [Tooltip("웨이브 텍스트 포맷 (일반 모드)")]
    [SerializeField] private string _normalModeFormat = "Wave {0} / {1}";

    [Header("색상 설정")]
    [Tooltip("일반 웨이브 색상")]
    [SerializeField] private Color _normalWaveColor = Color.white;

    [Tooltip("보스 웨이브 색상")]
    [SerializeField] private Color _bossWaveColor = new Color(1f, 0.3f, 0.3f);

    [Tooltip("최종 웨이브 색상")]
    [SerializeField] private Color _finalWaveColor = new Color(1f, 0.84f, 0f);
    #endregion

    #region Private Fields
    private int _currentWave;
    private int _totalWaves;
    private int _totalEnemies;
    private Vector3 _originalAnnouncementScale;
    private Coroutine _announcementCoroutine;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 초기 스케일 저장
        if (_waveAnnouncementText != null)
        {
            _originalAnnouncementScale = _waveAnnouncementText.transform.localScale;
            _waveAnnouncementText.gameObject.SetActive(false);
        }

        if (_countdownText != null)
        {
            _countdownText.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // WaveManager 이벤트 구독
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStart += OnWaveStart;
            WaveManager.Instance.OnWaveComplete += OnWaveComplete;
            WaveManager.Instance.OnEnemyCountChanged += OnEnemyCountChanged;
            WaveManager.Instance.OnPreparationStart += OnPreparationStart;
            WaveManager.Instance.OnPreparationUpdate += OnPreparationUpdate;
            WaveManager.Instance.OnBossSpawn += OnBossSpawn;
            WaveManager.Instance.OnAllWavesComplete += OnAllWavesComplete;

            // 초기값 설정
            _totalWaves = WaveManager.Instance.TotalWaves;
            UpdateWaveText(0);
        }
        else
        {
            Debug.LogWarning("[WaveUI] WaveManager를 찾을 수 없습니다!");
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStart -= OnWaveStart;
            WaveManager.Instance.OnWaveComplete -= OnWaveComplete;
            WaveManager.Instance.OnEnemyCountChanged -= OnEnemyCountChanged;
            WaveManager.Instance.OnPreparationStart -= OnPreparationStart;
            WaveManager.Instance.OnPreparationUpdate -= OnPreparationUpdate;
            WaveManager.Instance.OnBossSpawn -= OnBossSpawn;
            WaveManager.Instance.OnAllWavesComplete -= OnAllWavesComplete;
        }

        // Tween 정리
        KillAllTweens();
    }
    #endregion

    #region Event Handlers
    private void OnWaveStart(int waveNumber)
    {
        _currentWave = waveNumber;

        // 웨이브 텍스트 업데이트
        UpdateWaveText(waveNumber);

        // 웨이브 시작 알림
        ShowWaveAnnouncement(waveNumber);

        // 카운트다운 숨기기
        HideCountdown();
    }

    private void OnWaveComplete(int waveNumber)
    {
        // 웨이브 완료 애니메이션
        PlayWaveCompleteAnimation();
    }

    private void OnEnemyCountChanged(int alive, int total)
    {
        UpdateEnemyCount(alive, total);
    }

    private void OnPreparationStart(float duration)
    {
        ShowCountdown(duration);
    }

    private void OnPreparationUpdate(float remaining)
    {
        UpdateCountdown(remaining);
    }

    private void OnBossSpawn(EnemyData bossData)
    {
        ShowBossAnnouncement(bossData);
    }

    private void OnAllWavesComplete()
    {
        ShowVictoryAnnouncement();
    }
    #endregion

    #region Wave Text
    /// <summary>
    /// 웨이브 텍스트 업데이트
    /// </summary>
    private void UpdateWaveText(int waveNumber)
    {
        if (_waveText == null) return;

        bool isInfinite = WaveManager.Instance != null && WaveManager.Instance.IsInfiniteMode;

        if (isInfinite || _totalWaves == 0)
        {
            _waveText.text = string.Format(_infiniteModeFormat, waveNumber);
        }
        else
        {
            _waveText.text = string.Format(_normalModeFormat, waveNumber, _totalWaves);
        }

        // 색상 설정
        if (waveNumber == _totalWaves && !isInfinite)
        {
            _waveText.color = _finalWaveColor;
        }
        else
        {
            _waveText.color = _normalWaveColor;
        }
    }
    #endregion

    #region Enemy Count
    /// <summary>
    /// 적 수 업데이트
    /// </summary>
    private void UpdateEnemyCount(int alive, int total)
    {
        _totalEnemies = total;

        if (_enemyCountText != null)
        {
            _enemyCountText.text = $"{alive} / {total}";
        }

        if (_enemyProgressBar != null)
        {
            float progress = total > 0 ? 1f - ((float)alive / total) : 0f;
            _enemyProgressBar.DOFillAmount(progress, 0.3f).SetUpdate(true);
        }
    }
    #endregion

    #region Announcements
    /// <summary>
    /// 웨이브 시작 알림
    /// </summary>
    private void ShowWaveAnnouncement(int waveNumber)
    {
        if (_waveAnnouncementText == null) return;

        // 기존 코루틴 정지
        if (_announcementCoroutine != null)
        {
            StopCoroutine(_announcementCoroutine);
        }

        _announcementCoroutine = StartCoroutine(WaveAnnouncementSequence(waveNumber));
    }

    private IEnumerator WaveAnnouncementSequence(int waveNumber)
    {
        _waveAnnouncementText.DOKill();

        // 보스 웨이브 체크
        var waveConfig = WaveManager.Instance?.GetCurrentWaveConfig();
        bool isBossWave = waveConfig != null && waveConfig.hasBoss;
        bool isFinalWave = waveNumber == _totalWaves && !WaveManager.Instance.IsInfiniteMode;

        // 텍스트 설정
        string waveText = isBossWave ? $"BOSS WAVE {waveNumber}" :
                         isFinalWave ? $"FINAL WAVE {waveNumber}" :
                         $"WAVE {waveNumber}";

        Color waveColor = isBossWave ? _bossWaveColor :
                         isFinalWave ? _finalWaveColor :
                         _normalWaveColor;

        _waveAnnouncementText.text = waveText;
        _waveAnnouncementText.color = waveColor;
        _waveAnnouncementText.transform.localScale = Vector3.zero;
        _waveAnnouncementText.gameObject.SetActive(true);

        // 애니메이션 시퀀스
        Sequence seq = DOTween.Sequence();

        // 스케일 인
        seq.Append(_waveAnnouncementText.transform.DOScale(_originalAnnouncementScale * _announcementScale, 0.3f)
            .SetEase(Ease.OutBack));

        // 대기
        seq.AppendInterval(_announcementDuration - 0.6f);

        // 스케일 아웃 + 페이드 아웃
        seq.Append(_waveAnnouncementText.transform.DOScale(_originalAnnouncementScale * 0.5f, 0.3f));
        seq.Join(_waveAnnouncementText.DOFade(0f, 0.3f));

        seq.SetUpdate(true);

        yield return seq.WaitForCompletion();

        _waveAnnouncementText.gameObject.SetActive(false);
        _waveAnnouncementText.alpha = 1f;
        _announcementCoroutine = null;
    }

    /// <summary>
    /// 보스 등장 알림
    /// </summary>
    private void ShowBossAnnouncement(EnemyData bossData)
    {
        if (_waveAnnouncementText == null) return;

        // 기존 애니메이션 정지
        if (_announcementCoroutine != null)
        {
            StopCoroutine(_announcementCoroutine);
        }

        _announcementCoroutine = StartCoroutine(BossAnnouncementSequence(bossData));
    }

    private IEnumerator BossAnnouncementSequence(EnemyData bossData)
    {
        _waveAnnouncementText.DOKill();

        string bossName = bossData != null ? bossData.enemyName : "BOSS";
        _waveAnnouncementText.text = $"WARNING!\n{bossName}";
        _waveAnnouncementText.color = _bossWaveColor;
        _waveAnnouncementText.transform.localScale = Vector3.zero;
        _waveAnnouncementText.gameObject.SetActive(true);

        // 흔들림 효과와 함께 등장
        Sequence seq = DOTween.Sequence();

        seq.Append(_waveAnnouncementText.transform.DOScale(_originalAnnouncementScale * _announcementScale, 0.5f)
            .SetEase(Ease.OutElastic));

        seq.Append(_waveAnnouncementText.transform.DOShakePosition(0.3f, 10f, 30));

        seq.AppendInterval(_announcementDuration);

        seq.Append(_waveAnnouncementText.DOFade(0f, 0.5f));

        seq.SetUpdate(true);

        yield return seq.WaitForCompletion();

        _waveAnnouncementText.gameObject.SetActive(false);
        _waveAnnouncementText.alpha = 1f;
        _announcementCoroutine = null;
    }

    /// <summary>
    /// 승리 알림
    /// </summary>
    private void ShowVictoryAnnouncement()
    {
        if (_waveAnnouncementText == null) return;

        _waveAnnouncementText.DOKill();
        _waveAnnouncementText.text = "VICTORY!";
        _waveAnnouncementText.color = _finalWaveColor;
        _waveAnnouncementText.transform.localScale = Vector3.zero;
        _waveAnnouncementText.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();
        seq.Append(_waveAnnouncementText.transform.DOScale(_originalAnnouncementScale * 2f, 0.5f)
            .SetEase(Ease.OutElastic));
        seq.Append(_waveAnnouncementText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 5, 0.5f));

        seq.SetUpdate(true);
    }

    /// <summary>
    /// 웨이브 완료 애니메이션
    /// </summary>
    private void PlayWaveCompleteAnimation()
    {
        if (_waveText == null) return;

        _waveText.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 5, 0.5f).SetUpdate(true);

        // 색상 깜빡임
        Sequence colorSeq = DOTween.Sequence();
        colorSeq.Append(_waveText.DOColor(_finalWaveColor, 0.2f));
        colorSeq.Append(_waveText.DOColor(_normalWaveColor, 0.2f));
        colorSeq.SetLoops(2).SetUpdate(true);
    }
    #endregion

    #region Countdown
    /// <summary>
    /// 카운트다운 표시
    /// </summary>
    private void ShowCountdown(float duration)
    {
        if (_countdownText == null) return;

        _countdownText.gameObject.SetActive(true);
        UpdateCountdown(duration);
    }

    /// <summary>
    /// 카운트다운 업데이트
    /// </summary>
    private void UpdateCountdown(float remaining)
    {
        if (_countdownText == null) return;

        int seconds = Mathf.CeilToInt(remaining);

        if (seconds <= 0)
        {
            _countdownText.text = "START!";
            _countdownText.color = Color.green;

            // 시작 애니메이션
            _countdownText.transform.DOPunchScale(Vector3.one * 0.5f, 0.3f, 5, 0.5f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _countdownText.DOFade(0f, 0.3f)
                        .SetUpdate(true)
                        .OnComplete(() => _countdownText.gameObject.SetActive(false));
                });
        }
        else
        {
            _countdownText.text = seconds.ToString();
            _countdownText.color = seconds <= 3 ? Color.red : Color.white;
            _countdownText.alpha = 1f;

            // 매 초 펀치 애니메이션
            _countdownText.transform.DOKill();
            _countdownText.transform.localScale = Vector3.one * 1.5f;
            _countdownText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBounce).SetUpdate(true);
        }
    }

    /// <summary>
    /// 카운트다운 숨기기
    /// </summary>
    private void HideCountdown()
    {
        if (_countdownText == null) return;

        _countdownText.DOKill();
        _countdownText.gameObject.SetActive(false);
    }
    #endregion

    #region Utility
    /// <summary>
    /// 모든 Tween 정리
    /// </summary>
    private void KillAllTweens()
    {
        if (_waveText != null) _waveText.DOKill();
        if (_waveAnnouncementText != null) _waveAnnouncementText.DOKill();
        if (_countdownText != null) _countdownText.DOKill();
        if (_enemyProgressBar != null) _enemyProgressBar.DOKill();
    }

    /// <summary>
    /// UI 새로고침
    /// </summary>
    public void RefreshUI()
    {
        if (WaveManager.Instance != null)
        {
            _totalWaves = WaveManager.Instance.TotalWaves;
            UpdateWaveText(WaveManager.Instance.CurrentWave);
            UpdateEnemyCount(WaveManager.Instance.EnemiesAlive, WaveManager.Instance.EnemiesSpawned);
        }
    }
    #endregion
}
