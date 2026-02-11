using UnityEngine;

/// <summary>
/// 分数管理器，负责维护总分、目标分数和通关判定
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // Debug
    private const string SYSTEM = "Scoring";
    private const string SCRIPT = "ScoreManager";
    [SerializeField] private bool _enableDebugLog = true;
    private void DebugLog(string msg) { if (_enableDebugLog) Debug.Log($"[{SYSTEM}][{SCRIPT}] {msg}"); }

    #region 运行时数据

    private int _currentScore = 0;
    private int _targetScore = 300;

    #endregion

    #region 属性

    public int CurrentScore => _currentScore;
    public int TargetScore => _targetScore;

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
        }
    }

    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnScoreCalculated -= OnScoreCalculated;
        }
    }

    #endregion

    #region 事件回调

    private void OnScoreCalculated(int roundScore, int newTotal)
    {
        // 更新总分
        _currentScore = newTotal;
        DebugLog($"Score updated: {_currentScore}/{_targetScore}");

        // 判断是否通关
        CheckRoundComplete();

        // 通知钩爪结算完成，可以返回
        ClawController.Instance?.OnSettlementComplete();
    }

    #endregion

    #region 通关判定

    private void CheckRoundComplete()
    {
        if (_currentScore >= _targetScore)
        {
            DebugLog($"Round Complete! Score: {_currentScore}/{_targetScore}");
            EventManager.Instance?.TriggerRoundResult(true);
        }
        else
        {
            DebugLog($"Round in progress: {_currentScore}/{_targetScore}");
        }
    }

    #endregion

    #region 公开方法
    /// 初始化回合分数（由 RoundPlayManager 调用）
    /// </summary>
    public void InitializeRound(int targetScore)
    {
        _targetScore = targetScore;
        _currentScore = 0;
        DebugLog($"Round initialized: Target={_targetScore}");
    }

    /// <summary>
    /// 重置当前分数（回合开始时调用）
    /// </summary>
    public void ResetScore()
    {
        _currentScore = 0;
        DebugLog("Score reset to 0");
    }

    /// <summary>
    /// 手动添加分数（用于测试或特殊奖励）
    /// </summary>
    public void AddScore(int amount)
    {
        _currentScore += amount;
        DebugLog($"Score added: +{amount} (Total: {_currentScore})");
        CheckRoundComplete();
    }

    #endregion
}