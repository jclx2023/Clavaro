using System.Collections;
using UnityEngine;

/// <summary>
/// 回合状态枚举
/// </summary>
public enum RoundState
{
    Idle,       // 空闲（未开始）
    Starting,   // 启动中（初始化配置）
    Playing,    // 进行中（玩家操作）
    Ending      // 结束中（结算）
}

/// <summary>
/// 回合游戏管理器
/// 负责单个回合的完整生命周期和子系统协调
/// </summary>
public class RoundPlayManager : MonoBehaviour
{
    public static RoundPlayManager Instance { get; private set; }

    // Debug
    private const string SYSTEM = "Gameflow";
    private const string SCRIPT = "RoundPlayManager";
    [SerializeField] private bool _enableDebugLog = true;
    private void DebugLog(string msg) { if (_enableDebugLog) Debug.Log($"[{SYSTEM}][{SCRIPT}] {msg}"); }

    #region 子系统引用

    [Header("子系统引用")]
    [SerializeField] private ClawController _clawController;
    [SerializeField] private BallSpawner _ballSpawner;
    [SerializeField] private ScoreManager _scoreManager;
    [SerializeField] private GrabCountManager _grabCountManager;
    [SerializeField] private SettlementZone _settlementZone;

    #endregion

    #region 配置

    [Header("测试配置")]
    [Tooltip("测试用回合配置")]
    [SerializeField] private RoundConfig _testRoundConfig;
    
    [Tooltip("测试用玩家球池")]
    [SerializeField] private PlayerDeckData _testPlayerDeck = new PlayerDeckData();

    [Header("清理配置")]
    [Tooltip("每次 GrabAttempt 后等待多久再清理结算池")]
    [SerializeField] private float _settlementClearDelay = 1f;

    #endregion

    #region 运行时数据

    private RoundState _currentState = RoundState.Idle;
    private RoundConfig _currentRoundConfig;
    private PlayerDeckData _currentPlayerDeck;
    private bool _waitingForFinalSettlement = false;

    #endregion

    #region 属性

    public RoundState CurrentState => _currentState;
    public bool IsPlaying => _currentState == RoundState.Playing;

    #endregion

    #region 生命周期

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DebugLog("Initialized");
    }

    private void OnEnable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnScoreCalculated += OnScoreCalculated;
            EventManager.Instance.OnGrabCountChanged += OnGrabCountChanged;
            EventManager.Instance.OnAllBallsEnteredSettlementPool += OnBallsEnteredSettlement;
        }
    }

    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnScoreCalculated -= OnScoreCalculated;
            EventManager.Instance.OnGrabCountChanged -= OnGrabCountChanged;
            EventManager.Instance.OnAllBallsEnteredSettlementPool -= OnBallsEnteredSettlement;
        }
    }

    #endregion

    #region 回合控制

    /// <summary>
    /// 开始回合
    /// </summary>
    public void StartRound(RoundConfig roundConfig, PlayerDeckData playerDeck)
    {
        if (_currentState != RoundState.Idle)
        {
            DebugLog($"Cannot start round: Current state is {_currentState}");
            return;
        }

        _currentRoundConfig = roundConfig;
        _currentPlayerDeck = playerDeck;
        _waitingForFinalSettlement = false;

        DebugLog($"Starting round: Target={roundConfig.targetScore}, Grabs={roundConfig.grabCount}");
        
        StartCoroutine(StartRoundCoroutine());
    }

    /// <summary>
    /// 开始回合协程
    /// </summary>
    private IEnumerator StartRoundCoroutine()
    {
        SetState(RoundState.Starting);

        // 1. 初始化 ScoreManager
        if (_scoreManager != null)
        {
            _scoreManager.InitializeRound(_currentRoundConfig.targetScore);
        }

        // 2. 初始化 GrabCountManager
        if (_grabCountManager != null)
        {
            _grabCountManager.InitializeGrabs(_currentRoundConfig.grabCount);
        }

        // 3. 初始化 BallSpawner（合并回合球池和玩家球池）
        if (_ballSpawner != null)
        {
            _ballSpawner.Initialize(
                _currentRoundConfig.defaultBallPool,
                _currentPlayerDeck.playerBalls
            );
            
            // 生成球（协程，等待完成）
            _ballSpawner.SpawnAllBalls();
            
            // 等待生成完成（监听 OnBallsSpawnCompleted 事件）
            yield return new WaitUntil(() => CheckBallsSpawned());
        }

        // 4. 启用 ClawController
        if (_clawController != null)
        {
            _clawController.EnableClaw();
        }

        // 5. 进入游戏阶段
        SetState(RoundState.Playing);
        DebugLog("Round started successfully");
    }

    /// <summary>
    /// 检查球是否生成完毕（简单实现：检查 BallSpawner 的球列表）
    /// </summary>
    private bool CheckBallsSpawned()
    {
        // 可以通过监听 OnBallsSpawnCompleted 事件，或直接检查 BallSpawner
        // 这里简化为延迟等待
        return _ballSpawner.SpawnedBalls.Count > 0;
    }

    /// <summary>
    /// 结束回合
    /// </summary>
    public void EndRound(bool success)
    {
        if (_currentState != RoundState.Playing)
        {
            DebugLog($"Cannot end round: Current state is {_currentState}");
            return;
        }

        DebugLog($"Ending round: {(success ? "Success" : "Failed")}");
        
        StartCoroutine(EndRoundCoroutine(success));
    }

    /// <summary>
    /// 结束回合协程
    /// </summary>
    private IEnumerator EndRoundCoroutine(bool success)
    {
        SetState(RoundState.Ending);

        // 1. 禁用 ClawController
        if (_clawController != null)
        {
            _clawController.DisableClaw();
        }

        // 2. 清理球池
        if (_ballSpawner != null)
        {
            _ballSpawner.ClearAllBalls();
        }

        // 3. 清理结算池
        if (_settlementZone != null)
        {
            _settlementZone.ClearPool();
        }

        // 4. 等待一帧，确保清理完成
        yield return null;

        // 5. 触发回合结果事件（通知上层，如 GameStateMachine）
        EventManager.Instance?.TriggerRoundResult(success);

        // 6. 重置状态
        SetState(RoundState.Idle);
        _currentRoundConfig = null;
        _currentPlayerDeck = null;
        _waitingForFinalSettlement = false;

        DebugLog("Round ended and cleaned up");
    }

    #endregion

    #region 状态控制

    private void SetState(RoundState newState)
    {
        if (_currentState == newState) return;

        DebugLog($"State: {_currentState} → {newState}");
        _currentState = newState;
    }

    #endregion

    #region 事件回调

    /// <summary>
    /// 分数计算完成
    /// </summary>
    private void OnScoreCalculated(int roundScore, int totalScore)
    {
        if (_currentState != RoundState.Playing)
            return;

        DebugLog($"Score calculated: +{roundScore} (Total: {totalScore}/{_currentRoundConfig.targetScore})");

        // 检查是否通关
        if (totalScore >= _currentRoundConfig.targetScore)
        {
            DebugLog("Target score reached! Round success!");
            EndRound(true);
            return;
        }

        // 检查是否次数耗尽且分数不足
        if (_waitingForFinalSettlement)
        {
            DebugLog("Final settlement complete. Score insufficient, round failed.");
            EndRound(false);
        }
    }

    /// <summary>
    /// 抓取次数变化
    /// </summary>
    private void OnGrabCountChanged(int remaining)
    {
        if (_currentState != RoundState.Playing)
            return;

        DebugLog($"Grabs remaining: {remaining}");

        // 次数耗尽，标记等待最终结算
        if (remaining == 0)
        {
            DebugLog("Grabs exhausted! Waiting for final settlement...");
            _waitingForFinalSettlement = true;
        }
    }

    /// <summary>
    /// 球进入结算池（每次 GrabAttempt 后清理）
    /// </summary>
    private void OnBallsEnteredSettlement(System.Collections.Generic.List<BallBase> balls)
    {
        if (_currentState != RoundState.Playing)
            return;

        DebugLog($"Balls entered settlement: {balls.Count}");

        // 延迟清理结算池
        StartCoroutine(ClearSettlementPoolDelayed());
    }

    /// <summary>
    /// 延迟清理结算池（等待玩家看到结果）
    /// </summary>
    private IEnumerator ClearSettlementPoolDelayed()
    {
        yield return new WaitForSeconds(_settlementClearDelay);

        if (_settlementZone != null)
        {
            _settlementZone.ClearAndDestroyBalls();
        }
    }

    #endregion

    #region 测试方法

    /// <summary>
    /// 测试：开始回合
    /// </summary>
    [ContextMenu("Test: Start Round")]
    private void TestStartRound()
    {
        if (_testRoundConfig == null)
        {
            Debug.LogError($"[{SCRIPT}] Test round config is not assigned!");
            return;
        }

        StartRound(_testRoundConfig, _testPlayerDeck);
    }

    /// <summary>
    /// 测试：强制结束回合（成功）
    /// </summary>
    [ContextMenu("Test: Force End Round (Success)")]
    private void TestForceEndSuccess()
    {
        EndRound(true);
    }

    /// <summary>
    /// 测试：强制结束回合（失败）
    /// </summary>
    [ContextMenu("Test: Force End Round (Failed)")]
    private void TestForceEndFailed()
    {
        EndRound(false);
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 获取当前回合配置
    /// </summary>
    public RoundConfig GetCurrentRoundConfig()
    {
        return _currentRoundConfig;
    }

    #endregion
}
