using UnityEngine;
using TMPro;

/// <summary>
/// 游戏HUD，显示核心玩法时的关键信息
/// 集成 TextAnimator 富文本动画效果
/// </summary>
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    // Debug
    private const string SYSTEM = "UI";
    private const string SCRIPT = "GameHUD";
    [SerializeField] private bool _enableDebugLog = true;
    private void DebugLog(string msg) { if (_enableDebugLog) Debug.Log($"[{SYSTEM}][{SCRIPT}] {msg}"); }

    #region UI组件引用

    [Header("文本组件")]
    [SerializeField] private TMP_Text _currentScoreText;
    [SerializeField] private TMP_Text _targetScoreText;
    [SerializeField] private TMP_Text _grabsRemainingText;

    #endregion

    #region 配置

    [Header("显示格式")]
    [SerializeField] private string _currentScoreFormat = "Current: {0}";
    [SerializeField] private string _targetScoreFormat = "Target: {0}";
    [SerializeField] private string _grabsFormat = "Grabs: {0}";

    [Header("TextAnimator 动画标签")]
    [Tooltip("分数变化时的动画标签")]
    [SerializeField] private string _scoreChangeTag = "bounce";
    [Tooltip("次数减少时的动画标签")]
    [SerializeField] private string _grabDecreaseTag = "shake";
    [Tooltip("低次数警告时的动画标签")]
    [SerializeField] private string _grabWarningTag = "wave";

    [Header("警告阈值")]
    [Tooltip("剩余次数低于此值时文本变红")]
    [SerializeField] private int _lowGrabsThreshold = 2;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _warningColor = Color.red;

    #endregion

    #region 运行时数据

    private int _currentScore = 0;
    private int _targetScore = 0;
    private int _grabsRemaining = 0;

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
        // 从 ScoreManager 获取初始数据
        if (ScoreManager.Instance != null)
        {
            _currentScore = ScoreManager.Instance.CurrentScore;
            _targetScore = ScoreManager.Instance.TargetScore;
        }
        if (GrabCountManager.Instance != null)
        {
            _grabsRemaining = GrabCountManager.Instance.RemainingGrabs;
        }
        
        // 初始显示（无动画）
        UpdateScoreDisplay(false);
        UpdateGrabsDisplay(false);
    }

    private void OnEnable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnScoreCalculated += OnScoreCalculated;
            EventManager.Instance.OnGrabCountChanged += OnGrabCountChanged;
        }
    }

    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnScoreCalculated -= OnScoreCalculated;
            EventManager.Instance.OnGrabCountChanged -= OnGrabCountChanged;
        }
    }

    #endregion

    #region 事件回调

    private void OnScoreCalculated(int roundScore, int newTotal)
    {
        _currentScore = newTotal;
        UpdateScoreDisplay(true); // 带动画
    }

    private void OnGrabCountChanged(int remaining)
    {
        _grabsRemaining = remaining;
        UpdateGrabsDisplay(true); // 带动画
    }

    #endregion

    #region 显示更新

    /// <summary>
    /// 更新分数显示
    /// </summary>
    /// <param name="withAnimation">是否添加动画标签</param>
    private void UpdateScoreDisplay(bool withAnimation = false)
    {
        string formattedScore = FormatNumber(_currentScore);
        
        // 如果需要动画，包裹标签
        if (withAnimation && !string.IsNullOrEmpty(_scoreChangeTag))
        {
            formattedScore = WrapWithTag(formattedScore, _scoreChangeTag);
        }

        if (_currentScoreText != null)
        {
            _currentScoreText.text = string.Format(_currentScoreFormat, formattedScore);
        }

        if (_targetScoreText != null)
        {
            _targetScoreText.text = string.Format(_targetScoreFormat, FormatNumber(_targetScore));
        }

        DebugLog($"Score updated: {_currentScore}/{_targetScore}");
    }

    /// <summary>
    /// 更新抓取次数显示
    /// </summary>
    /// <param name="withAnimation">是否添加动画标签</param>
    private void UpdateGrabsDisplay(bool withAnimation = false)
    {
        if (_grabsRemainingText == null) return;

        string grabText = _grabsRemaining.ToString();
        
        // 根据剩余次数选择动画标签
        if (withAnimation)
        {
            if (_grabsRemaining <= _lowGrabsThreshold && !string.IsNullOrEmpty(_grabWarningTag))
            {
                // 低次数警告动画
                grabText = WrapWithTag(grabText, _grabWarningTag);
            }
            else if (!string.IsNullOrEmpty(_grabDecreaseTag))
            {
                // 普通减少动画
                grabText = WrapWithTag(grabText, _grabDecreaseTag);
            }
        }

        _grabsRemainingText.text = string.Format(_grabsFormat, grabText);

        // 低次数警告变色
        _grabsRemainingText.color = _grabsRemaining <= _lowGrabsThreshold 
            ? _warningColor 
            : _normalColor;

        DebugLog($"Grabs updated: {_grabsRemaining}");
    }

    /// <summary>
    /// 将文本包裹在 TextAnimator 标签中
    /// </summary>
    private string WrapWithTag(string text, string tag)
    {
        return $"<{tag}>{text}</{tag}>";
    }

    /// <summary>
    /// 格式化数字（添加千位分隔符）
    /// </summary>
    private string FormatNumber(int number)
    {
        return number.ToString("N0"); // N0 = 千位分隔，无小数
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 手动设置目标分数（供外部调用，如回合开始时）
    /// </summary>
    public void SetTargetScore(int target)
    {
        _targetScore = target;
        UpdateScoreDisplay(false);
    }
    
    #endregion
}