using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 钩爪检测器，监听触发区域内的球
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ClawDetector : MonoBehaviour
{
    // Debug
    private const string SYSTEM = "Claw";
    private const string SCRIPT = "ClawDetector";
    [SerializeField] private bool _enableDebugLog = false;
    private void DebugLog(string msg) { if (_enableDebugLog) Debug.Log($"[{SYSTEM}][{SCRIPT}] {msg}"); }

    #region 运行时数据

    private List<BallBase> _ballsInRange = new List<BallBase>();

    #endregion

    #region 属性

    /// <summary>
    /// 当前检测到的球数量
    /// </summary>
    public int BallCount => _ballsInRange.Count;

    /// <summary>
    /// 获取检测到的所有球
    /// </summary>
    public List<BallBase> BallsInRange => _ballsInRange;

    #endregion

    #region 生命周期

    private void Awake()
    {
        // 确保是触发器
        Collider2D col = GetComponent<Collider2D>();
    }

    #endregion

    #region 触发检测

    private void OnTriggerEnter2D(Collider2D other)
    {
        BallBase ball = other.GetComponent<BallBase>();
        if (ball != null && !_ballsInRange.Contains(ball))
        {
            _ballsInRange.Add(ball);
            DebugLog($"Ball entered: {ball.name} (Total: {_ballsInRange.Count})");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        BallBase ball = other.GetComponent<BallBase>();
        if (ball != null && _ballsInRange.Contains(ball))
        {
            _ballsInRange.Remove(ball);
            DebugLog($"Ball exited: {ball.name} (Total: {_ballsInRange.Count})");
        }
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 清空检测列表
    /// </summary>
    public void Clear()
    {
        _ballsInRange.Clear();
        DebugLog("Detector cleared");
    }

    /// <summary>
    /// 检查是否有球在检测范围内
    /// </summary>
    public bool HasBalls()
    {
        return _ballsInRange.Count > 0;
    }

    #endregion
    
}