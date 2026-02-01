using UnityEngine;
using TMPro;

/// <summary>
/// 球的基类，承载数据、物理行为和视觉表现
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BallBase : MonoBehaviour
{
    // Debug
    private const string SYSTEM = "Balls";
    protected virtual string ScriptName => GetType().Name;
    [SerializeField] private bool _enableDebugLog = false;
    protected void DebugLog(string msg) { if (_enableDebugLog) Debug.Log($"[{SYSTEM}][{ScriptName}] {msg}"); }

    #region 组件引用

    [Header("组件引用")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private TMP_Text _valueText;
    
    private Rigidbody2D _rb;
    private CircleCollider2D _collider;

    #endregion

    #region 运行时数据

    private BallConfig _config;
    private bool _isGrabbed;

    #endregion

    #region 属性

    public BallConfig Config => _config;
    public bool IsGrabbed => _isGrabbed;
    public Rigidbody2D Rb => _rb;

    #endregion

    #region 生命周期

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 使用配置初始化球
    /// </summary>
    public virtual void Initialize(BallConfig config)
    {
        _config = config;
        
        ApplyVisuals();
        ApplyPhysics();
        
        DebugLog($"Initialized: {config.ballName}");
    }

    private void ApplyVisuals()
    {
        if (_spriteRenderer != null && _config.sprite != null)
        {
            _spriteRenderer.sprite = _config.sprite;
        }

        if (_valueText != null)
        {
            _valueText.text = _config.GetDisplayText();
            _valueText.color = _config.textColor;
        }
    }

    private void ApplyPhysics()
    {
        _rb.mass = _config.mass;
        _rb.drag = _config.linearDrag;
        _rb.angularDrag = _config.angularDrag;
        _rb.gravityScale = _config.gravityScale;

        if (_config.physicsMaterial != null)
        {
            _collider.sharedMaterial = _config.physicsMaterial;
        }
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 获取球的数值（分数或倍率）
    /// </summary>
    public float GetValue()
    {
        return _config != null ? _config.value : 0f;
    }

    /// <summary>
    /// 获取球的类型
    /// </summary>
    public BallType GetBallType()
    {
        return _config != null ? _config.ballType : BallType.Score;
    }

    /// <summary>
    /// 设置抓取状态
    /// </summary>
    public void SetGrabbed(bool grabbed)
    {
        if (_isGrabbed == grabbed) return;
        
        _isGrabbed = grabbed;
        
        if (grabbed)
            OnGrabbed();
        else
            OnDropped();
    }

    #endregion

    #region 事件回调（可重写）

    /// <summary>
    /// 被抓住时调用
    /// </summary>
    protected virtual void OnGrabbed()
    {
        DebugLog("Grabbed");
        EventManager.Instance?.TriggerBallGrabbed(this);
    }

    /// <summary>
    /// 从钩爪掉落时调用
    /// </summary>
    protected virtual void OnDropped()
    {
        DebugLog("Dropped");
        EventManager.Instance?.TriggerBallDropped(this);
    }

    /// <summary>
    /// 落入结算池时调用
    /// </summary>
    public virtual void OnSettled()
    {
        DebugLog("Settled");
        EventManager.Instance?.TriggerBallSettled(this);
    }

    #endregion
}