using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 结算池，检测进入的球并触发结算事件
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SettlementZone : MonoBehaviour
{
    // Debug
    private const string SYSTEM = "Scoring";
    private const string SCRIPT = "SettlementZone";
    [SerializeField] private bool _enableDebugLog = true;
    private void DebugLog(string msg) { if (_enableDebugLog) Debug.Log($"[{SYSTEM}][{SCRIPT}] {msg}"); }

    #region 配置

    [Header("检测配置")]
    [SerializeField] private float _settlementCheckInterval = 0.2f;
    [SerializeField] private float _velocityThreshold = 0.1f;
    [SerializeField] private float _settlementWaitTime = 0.5f;

    #endregion

    #region 运行时数据

    private List<BallBase> _enteredBalls = new List<BallBase>();
    private bool _isChecking = false;

    #endregion

    #region 生命周期

    private void Awake()
    {
        // 确保是触发器
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    #endregion

    #region 碰撞检测

    private void OnTriggerEnter2D(Collider2D other)
    {
        BallBase ball = other.GetComponent<BallBase>();
        if (ball != null && !_enteredBalls.Contains(ball))
        {
            _enteredBalls.Add(ball);
            DebugLog($"Ball entered: {ball.name} (Total: {_enteredBalls.Count})");

            // 开始检测静止状态
            if (!_isChecking)
            {
                StartCoroutine(CheckSettlementCoroutine());
            }
        }
    }

    #endregion

    #region 结算检测

    private IEnumerator CheckSettlementCoroutine()
    {
        _isChecking = true;
        DebugLog("Start checking settlement...");

        float waitTimer = 0f;

        while (true)
        {
            yield return new WaitForSeconds(_settlementCheckInterval);

            // 检查所有球是否静止
            bool allSettled = true;
            foreach (var ball in _enteredBalls)
            {
                if (ball == null) continue;

                Rigidbody2D rb = ball.Rb;
                if (rb != null && rb.velocity.magnitude > _velocityThreshold)
                {
                    allSettled = false;
                    break;
                }
            }

            if (allSettled)
            {
                waitTimer += _settlementCheckInterval;

                // 持续静止一段时间后触发结算
                if (waitTimer >= _settlementWaitTime)
                {
                    TriggerSettlement();
                    break;
                }
            }
            else
            {
                // 还有球在运动，重置计时
                waitTimer = 0f;
            }
        }

        _isChecking = false;
    }

    private void TriggerSettlement()
    {
        // 移除空引用
        _enteredBalls.RemoveAll(ball => ball == null);

        if (_enteredBalls.Count == 0)
        {
            DebugLog("No balls to settle");
            return;
        }

        DebugLog($"Settlement triggered: {_enteredBalls.Count} balls");

        // 触发结算事件
        EventManager.Instance?.TriggerAllBallsEnteredSettlementPool(_enteredBalls);

        // 清空列表
        _enteredBalls.Clear();
    }

    #endregion

    #region 公开方法
    /// <summary>
    /// 清空并销毁结算池中的所有球（每次 GrabAttempt 后调用）
    /// </summary>
    public void ClearAndDestroyBalls()
    {
        if (_isChecking)
        {
            StopAllCoroutines();
            _isChecking = false;
        }

        // 销毁球对象
        foreach (var ball in _enteredBalls)
        {
            if (ball != null)
            {
                Destroy(ball.gameObject);
            }
        }

        _enteredBalls.Clear();
        DebugLog("Pool cleared and balls destroyed");
    }
    /// <summary>
    /// 强制清空结算池（用于回合重置）
    /// </summary>
    public void ClearPool()
    {
        if (_isChecking)
        {
            StopAllCoroutines();
            _isChecking = false;
        }

        _enteredBalls.Clear();
        DebugLog("Pool cleared");
    }

    #endregion

    #region Editor 可视化

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawCube(transform.position, col.bounds.size);
        }
    }
#endif

    #endregion
}