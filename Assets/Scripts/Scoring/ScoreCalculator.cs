using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 分数计算器，负责根据球的数值计算本次得分
/// </summary>
public class ScoreCalculator : MonoBehaviour
{
    public static ScoreCalculator Instance { get; private set; }

    // Debug
    private const string SYSTEM = "Scoring";
    private const string SCRIPT = "ScoreCalculator";
    [SerializeField] private bool _enableDebugLog = true;
    private void DebugLog(string msg) { if (_enableDebugLog) Debug.Log($"[{SYSTEM}][{SCRIPT}] {msg}"); }

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
            EventManager.Instance.OnAllBallsEnteredSettlementPool += OnBallsEntered;
        }
    }

    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnAllBallsEnteredSettlementPool -= OnBallsEntered;
        }
    }

    #endregion

    #region 事件回调

    private void OnBallsEntered(List<BallBase> balls)
    {
        if (balls == null || balls.Count == 0)
        {
            DebugLog("No balls to calculate");
            return;
        }

        int roundScore = CalculateScore(balls);
        int currentTotal = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        int newTotal = currentTotal + roundScore;

        DebugLog($"Calculated: +{roundScore} (Total: {currentTotal} → {newTotal})");

        // 触发计分事件
        EventManager.Instance?.TriggerScoreCalculated(roundScore, newTotal);
    }

    #endregion

    #region 计算逻辑

    /// <summary>
    /// 计算本次得分
    /// 公式: 本次得分 = Σ(基础分) × Σ(乘倍)
    /// </summary>
    private int CalculateScore(List<BallBase> balls)
    {
        float totalBaseScore = 0f;
        float totalMultiplier = 0f;
        int scoreBallCount = 0;
        int multiplierBallCount = 0;

        // 分离并累加
        foreach (var ball in balls)
        {
            if (ball == null || ball.Config == null) continue;

            if (ball.GetBallType() == BallType.Score)
            {
                totalBaseScore += ball.GetValue();
                scoreBallCount++;
            }
            else if (ball.GetBallType() == BallType.Multiplier)
            {
                totalMultiplier += ball.GetValue();
                multiplierBallCount++;
            }
        }

        // 如果没有乘倍球，默认倍率为 1.0
        if (multiplierBallCount == 0)
        {
            totalMultiplier = 1f;
        }

        float result = totalBaseScore * totalMultiplier;
        int finalScore = Mathf.RoundToInt(result);

        DebugLog($"Score Balls: {scoreBallCount} (Total: {totalBaseScore})");
        DebugLog($"Multiplier Balls: {multiplierBallCount} (Total: ×{totalMultiplier})");
        DebugLog($"Formula: {totalBaseScore} × {totalMultiplier} = {finalScore}");

        return finalScore;
    }

    #endregion
}