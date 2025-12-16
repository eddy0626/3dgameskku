using System;
using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 게임 상태를 관리하는 매니저
/// Ready → Go → Playing → GameOver 플로우 제어
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    
    public enum GameState
    {
        Ready,      // 게임 시작 대기 (2초)
        Go,         // Go! 표시 (0.5초)
        Playing,    // 게임 플레이 중
        GameOver    // 게임 오버
    }
    
    [Header("Current State")]
    [SerializeField] private GameState _currentState = GameState.Ready;
    
    [Header("State Timing")]
    [SerializeField] private float _readyDuration = 2f;
    [SerializeField] private float _goDuration = 0.5f;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private CanvasGroup _stateTextCanvasGroup;
    
    [Header("Player Reference")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private CameraRotate _cameraRotate;
    
    // Events
    public event Action<GameState> OnStateChanged;
    
    // Properties
    public GameState CurrentState => _currentState;
    public bool IsPlaying => _currentState == GameState.Playing;
    
    // 커서 모드 상태
    private bool _isCursorMode = false;
    public bool IsCursorMode => _isCursorMode;

    
    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
private void Start()
    {
        // UI 자동 참조 찾기
        if (_stateText == null)
        {
            var stateTextObj = GameObject.Find("StateText");
            if (stateTextObj != null)
            {
                _stateText = stateTextObj.GetComponent<TextMeshProUGUI>();
                // CanvasGroup이 없으면 추가
                _stateTextCanvasGroup = stateTextObj.GetComponent<CanvasGroup>();
                if (_stateTextCanvasGroup == null)
                {
                    _stateTextCanvasGroup = stateTextObj.AddComponent<CanvasGroup>();
                }
                Debug.Log("[GameStateManager] StateText UI 자동 연결 완료");
            }
            else
            {
                Debug.LogWarning("[GameStateManager] StateText UI를 찾을 수 없습니다!");
            }
        }
        
        // Player 자동 참조 찾기
        if (_playerHealth == null)
        {
            _playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
        if (_playerMove == null)
        {
            _playerMove = FindFirstObjectByType<PlayerMove>();
        }
        if (_cameraRotate == null)
        {
            _cameraRotate = FindFirstObjectByType<CameraRotate>();
        }
        
        // 사망 이벤트 구독
        if (_playerHealth != null)
        {
            _playerHealth.OnDeath += HandlePlayerDeath;
        }
        
        // 초기 컨트롤 비활성화
        SetControlsEnabled(false);
        SetEnemiesEnabled(false);
        
        // 게임 시작 시퀀스
        StartCoroutine(GameStartSequence());
    }
    
    private void Update()
    {
        // Playing 상태에서만 Tab키 커서 모드 전환 가능
        if (_currentState == GameState.Playing && Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleCursorMode();
        }
    }
    
    /// <summary>
    /// 커서 모드 토글 (Tab키)
    /// </summary>
    public void ToggleCursorMode()
    {
        _isCursorMode = !_isCursorMode;
        
        if (_isCursorMode)
        {
            // 커서 모드 활성화 - 게임 일시정지
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetControlsEnabled(false);
            Debug.Log("[GameStateManager] 커서 모드 ON - 게임 일시정지");
        }
        else
        {
            // 커서 모드 비활성화 - 게임 재개
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SetControlsEnabled(true);
            Debug.Log("[GameStateManager] 커서 모드 OFF - 게임 재개");
        }
    }
    
    
private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDeath -= HandlePlayerDeath;
        }
        
        // DOTween 정리
        if (_stateText != null)
        {
            _stateText.DOKill();
        }
        if (_stateTextCanvasGroup != null)
        {
            _stateTextCanvasGroup.DOKill();
        }
    }
    
    /// <summary>
    /// 게임 시작 시퀀스: Ready → Go → Playing
    /// </summary>
private IEnumerator GameStartSequence()
    {
        // Ready 상태
        SetState(GameState.Ready);
        ShowStateText("Ready", Color.yellow);
        SetControlsEnabled(false);
        SetEnemiesEnabled(false);
        
        yield return new WaitForSeconds(_readyDuration);
        
        // Go 상태
        SetState(GameState.Go);
        ShowStateText("Go!", Color.green, true);
        
        yield return new WaitForSeconds(_goDuration);
        
        // Playing 상태 - 컨트롤 활성화
        SetState(GameState.Playing);
        HideStateText();
        SetControlsEnabled(true);
        SetEnemiesEnabled(true);
        
        // 마우스 커서 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    /// <summary>
    /// 상태 변경
    /// </summary>
    private void SetState(GameState newState)
    {
        if (_currentState == newState) return;
        
        GameState previousState = _currentState;
        _currentState = newState;
        
        Debug.Log($"[GameStateManager] State changed: {previousState} → {newState}");
        
        OnStateChanged?.Invoke(_currentState);
    }
    
    /// <summary>
    /// 플레이어 사망 처리
    /// </summary>
private void HandlePlayerDeath()
    {
        if (_currentState == GameState.GameOver) return;
        
        SetState(GameState.GameOver);
        SetControlsEnabled(false);
        SetEnemiesEnabled(false);
        ShowGameOverText();
        
        // 마우스 커서 해제
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    /// <summary>
    /// 상태 텍스트 표시
    /// </summary>
    private void ShowStateText(string text, Color color, bool punch = false)
    {
        if (_stateText == null) return;
        
        _stateText.text = text;
        _stateText.color = color;
        _stateText.gameObject.SetActive(true);
        
        if (_stateTextCanvasGroup != null)
        {
            _stateTextCanvasGroup.alpha = 1f;
        }
        
        // 텍스트 애니메이션
        _stateText.transform.localScale = Vector3.one;
        
        if (punch)
        {
            // Go! 텍스트는 펀치 효과
            _stateText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.5f);
        }
        else
        {
            // Ready는 펄스 효과
            _stateText.transform.DOScale(1.1f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }
    
    /// <summary>
    /// 상태 텍스트 숨기기
    /// </summary>
    private void HideStateText()
    {
        if (_stateText == null) return;
        
        _stateText.DOKill();
        _stateText.transform.localScale = Vector3.one;
        
        if (_stateTextCanvasGroup != null)
        {
            _stateTextCanvasGroup.DOFade(0f, 0.3f)
                .OnComplete(() => _stateText.gameObject.SetActive(false));
        }
        else
        {
            _stateText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Game Over 텍스트 표시
    /// </summary>
    private void ShowGameOverText()
    {
        if (_stateText == null) return;
        
        _stateText.DOKill();
        _stateText.text = "Game Over";
        _stateText.color = Color.red;
        _stateText.gameObject.SetActive(true);
        _stateText.transform.localScale = Vector3.zero;
        
        if (_stateTextCanvasGroup != null)
        {
            _stateTextCanvasGroup.alpha = 1f;
        }
        
        // 드라마틱한 등장 애니메이션
        Sequence seq = DOTween.Sequence();
        seq.Append(_stateText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack));
        seq.Append(_stateText.transform.DOShakePosition(0.5f, 10f, 20));
        seq.Append(_stateText.DOFade(0.7f, 0.5f).SetLoops(-1, LoopType.Yoyo));
    }
    
    /// <summary>
    /// 게임 재시작 (필요 시 호출)
    /// </summary>
    public void RestartGame()
    {
        StopAllCoroutines();
        
        if (_playerHealth != null)
        {
            _playerHealth.Revive(1f);
        }
        
        StartCoroutine(GameStartSequence());
    }


    /// <summary>
    /// 플레이어 컨트롤 활성화/비활성화
    /// </summary>
    private void SetControlsEnabled(bool enabled)
    {
        if (_playerMove != null)
        {
            _playerMove.enabled = enabled;
        }
        
        if (_cameraRotate != null)
        {
            _cameraRotate.enabled = enabled;
        }
    }
    
    /// <summary>
    /// 적 AI 활성화/비활성화
    /// </summary>
    private void SetEnemiesEnabled(bool enabled)
    {
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.enabled = enabled;
        }
    }

}
