using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 全局事件管理器，负责游戏内各系统间的解耦通信
/// </summary>
public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    // Debug
    private const string SYSTEM = "Core";
    private const string SCRIPT = "EventManager";
    [SerializeField] private bool _enableDebugLog = true;
    private void DebugLog(string msg) { if (_enableDebugLog) Debug.Log($"[{SYSTEM}][{SCRIPT}] {msg}"); }

    #region 事件定义

    // 钩爪事件
    public event Action OnGrabStarted;
    public event Action OnGrabReleased;
    public event Action<List<BallBase>> OnGrabCompleted;

    // 球事件
    public event Action<BallBase> OnBallGrabbed;
    public event Action<BallBase> OnBallDropped;
    public event Action<BallBase> OnBallSettled;
    public event Action<int> OnBallsSpawnCompleted; // 球生成完毕，参数为数量

    // 计分事件
    public event Action<int, int> OnScoreCalculated; // (本次得分, 总分)
    public event Action<int> OnGrabCountChanged;     // 剩余抓取次数

    // 回合事件
    public event Action<bool> OnRoundResult; // true=通关, false=失败

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
        DontDestroyOnLoad(gameObject);
        DebugLog("Initialized");
    }

    #endregion

    #region 事件触发方法

    public void TriggerGrabStarted()
    {
        DebugLog("GrabStarted");
        OnGrabStarted?.Invoke();
    }

    public void TriggerGrabReleased()
    {
        DebugLog("GrabReleased");
        OnGrabReleased?.Invoke();
    }

    public void TriggerGrabCompleted(List<BallBase> balls)
    {
        DebugLog($"GrabCompleted: {balls.Count} balls");
        OnGrabCompleted?.Invoke(balls);
    }

    public void TriggerBallGrabbed(BallBase ball)
    {
        DebugLog($"BallGrabbed: {ball.name}");
        OnBallGrabbed?.Invoke(ball);
    }

    public void TriggerBallDropped(BallBase ball)
    {
        DebugLog($"BallDropped: {ball.name}");
        OnBallDropped?.Invoke(ball);
    }

    public void TriggerBallSettled(BallBase ball)
    {
        DebugLog($"BallSettled: {ball.name}");
        OnBallSettled?.Invoke(ball);
    }

    public void TriggerBallsSpawnCompleted(int count)
    {
        DebugLog($"BallsSpawnCompleted: {count} balls");
        OnBallsSpawnCompleted?.Invoke(count);
    }

    public void TriggerScoreCalculated(int roundScore, int totalScore)
    {
        DebugLog($"ScoreCalculated: +{roundScore}, Total={totalScore}");
        OnScoreCalculated?.Invoke(roundScore, totalScore);
    }

    public void TriggerGrabCountChanged(int remaining)
    {
        DebugLog($"GrabCountChanged: {remaining}");
        OnGrabCountChanged?.Invoke(remaining);
    }

    public void TriggerRoundResult(bool success)
    {
        DebugLog($"RoundResult: {(success ? "Success" : "Failed")}");
        OnRoundResult?.Invoke(success);
    }

    #endregion
}