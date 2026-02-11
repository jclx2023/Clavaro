using UnityEngine;

/// <summary>
/// 抓取次数管理器，负责追踪和管理每回合的剩余抓取次数
/// </summary>
public class GrabCountManager : MonoBehaviour
{
    public static GrabCountManager Instance { get; private set; }

    // Debug
    private const string SYSTEM = "Claw";
    private const string SCRIPT = "GrabCountManager";
    [SerializeField] private bool _enableDebugLog = true;
    private void DebugLog(string msg) { if (_enableDebugLog) Debug.Log($"[{SYSTEM}][{SCRIPT}] {msg}"); }

    #region 配置

    [Header("初始配置")]
    [SerializeField] private int _defaultGrabsPerRound = 5;

    #endregion

    #region 运行时数据

    private int _remainingGrabs = 0;

    #endregion

    #region 属性

    public int RemainingGrabs => _remainingGrabs;
    public bool HasGrabsRemaining => _remainingGrabs > 0;

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

    private void Start()
    {
        // 初始化次数
        InitializeGrabs(_defaultGrabsPerRound);
    }

    private void OnEnable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnGrabStarted += OnGrabStarted;
        }
    }

    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnGrabStarted -= OnGrabStarted;
        }
    }

    #endregion

    #region 事件回调

    private void OnGrabStarted()
    {
        ConsumeGrab();
    }

    #endregion

    #region 核心逻辑

    /// <summary>
    /// 消耗一次抓取次数
    /// </summary>
    private void ConsumeGrab()
    {
        if (_remainingGrabs <= 0)
        {
            DebugLog("Warning: No grabs remaining!");
            return;
        }

        _remainingGrabs--;
        DebugLog($"Grab consumed. Remaining: {_remainingGrabs}");

        // 触发次数变化事件
        EventManager.Instance?.TriggerGrabCountChanged(_remainingGrabs);

        // 检查是否耗尽
        if (_remainingGrabs <= 0)
        {
            OnGrabsExhausted();
        }
    }

    /// <summary>
    /// 次数耗尽时调用
    /// </summary>
    private void OnGrabsExhausted()
    {
        DebugLog("All grabs exhausted!");
        
        // 等待 GameFlow 层的 GameStateMachine 处理回合结束逻辑
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 初始化抓取次数（回合开始时调用）
    /// </summary>
    public void InitializeGrabs(int count)
    {
        _remainingGrabs = count;
        DebugLog($"Grabs initialized: {_remainingGrabs}");
        
        // 触发初始事件
        EventManager.Instance?.TriggerGrabCountChanged(_remainingGrabs);
    }

    /// <summary>
    /// 重置为默认次数
    /// </summary>
    public void ResetToDefault()
    {
        InitializeGrabs(_defaultGrabsPerRound);
    }

    /// <summary>
    /// 增加抓取次数（用于奖励或特殊效果）
    /// </summary>
    public void AddGrabs(int amount)
    {
        _remainingGrabs += amount;
        DebugLog($"Grabs added: +{amount} (Total: {_remainingGrabs})");
        
        EventManager.Instance?.TriggerGrabCountChanged(_remainingGrabs);
    }

    #endregion
}