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
    private Material _materialInstance; // 材质实例

    #endregion

    #region 防重叠配置

    [Header("防重叠配置")]
    [Tooltip("是否启用防重叠系统")]
    [SerializeField] private bool _enableAntiOverlap = true;
    
    [Tooltip("检测半径倍数（相对于球半径）")]
    [Range(1.0f, 3.0f)]
    [SerializeField] private float _detectionRadiusMultiplier = 1.5f;
    
    [Tooltip("分离力度")]
    [Range(1f, 200f)]
    [SerializeField] private float _separationForce = 8f;
    
    [Tooltip("最小安全距离（额外间距）")]
    [Range(0f, 0.3f)]
    [SerializeField] private float _minSafetyMargin = 0.05f;
    
    [Tooltip("检测间隔（秒），降低性能开销")]
    [Range(0.02f, 0.2f)]
    [SerializeField] private float _checkInterval = 0.05f;
    
    [Tooltip("被抓住时的分离力衰减系数")]
    [Range(0f, 1f)]
    [SerializeField] private float _grabbedForceMultiplier = 0.3f;
    
    [Tooltip("球的图层（用于检测）")]
    [SerializeField] private LayerMask _ballLayerMask = -1; // 默认检测所有层

    #endregion

    #region 运行时数据

    private BallConfig _config;
    private bool _isGrabbed;
    
    // 防重叠
    private float _checkTimer = 0f;
    private float _ballRadius = 0.5f; // 缓存球半径

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

    protected virtual void Update()
    {
        if (_enableAntiOverlap)
        {
            UpdateAntiOverlap();
        }
    }

    protected virtual void OnDestroy()
    {
        // 清理材质实例
        if (_materialInstance != null)
        {
            Destroy(_materialInstance);
        }
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 使用配置初始化球
    /// </summary>
    public virtual void Initialize(BallConfig config)
    {
        _config = config;
        
        ApplyScale();         // 应用缩放
        ApplyVisuals();       // 应用视觉
        ApplyPhysics();       // 应用物理
        ApplyShaderEffects(); // 应用Shader效果
        
        // 缓存球半径（用于防重叠检测）
        _ballRadius = _config.radius * _config.GetScale();
        
        DebugLog($"Initialized: {config.ballName}, radius={_ballRadius}");
    }
    
    private void ApplyScale()
    {
        float scale = _config.GetScale();
        transform.localScale = Vector3.one * scale;
    }

    private void ApplyVisuals()
    {
        _spriteRenderer.sprite = _config.sprite;

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

    /// <summary>
    /// 应用Shader效果
    /// </summary>
    private void ApplyShaderEffects()
    {
        if (_spriteRenderer == null) return;

        // 创建材质实例（避免修改共享材质）
        _materialInstance = new Material(_spriteRenderer.material);
        _spriteRenderer.material = _materialInstance;

        // 启用 HSV 关键字
        if (_config.hueShift != 0f || _config.saturation != 1f || _config.brightness != 1f)
        {
            _materialInstance.EnableKeyword("HSV_ON");
            
            // 设置 HSV 参数
            _materialInstance.SetFloat("_HsvShift", _config.hueShift);
            _materialInstance.SetFloat("_HsvSaturation", _config.saturation);
            _materialInstance.SetFloat("_HsvBright", _config.brightness);
        }
        else
        {
            _materialInstance.DisableKeyword("HSV_ON");
        }
    }

    #endregion

    #region 防重叠系统

    /// <summary>
    /// 更新防重叠逻辑（间隔检测）
    /// </summary>
    private void UpdateAntiOverlap()
    {
        // 间隔检测，降低性能开销
        _checkTimer += Time.deltaTime;
        if (_checkTimer < _checkInterval)
            return;
        
        _checkTimer = 0f;
        
        CheckAndSeparateFromNearbyBalls();
    }

    /// <summary>
    /// 检测并分离附近的球
    /// </summary>
    private void CheckAndSeparateFromNearbyBalls()
    {
        // 1. 检测范围
        float detectionRadius = _ballRadius * _detectionRadiusMultiplier;
        
        // 2. 检测附近的碰撞器
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(
            transform.position, 
            detectionRadius, 
            _ballLayerMask
        );
        
        // 3. 累积分离力
        Vector2 totalSeparationForce = Vector2.zero;
        int overlapCount = 0;
        
        foreach (var otherCollider in nearbyColliders)
        {
            // 跳过自己
            if (otherCollider == _collider)
                continue;
            
            // 获取对方的BallBase
            BallBase otherBall = otherCollider.GetComponent<BallBase>();
            if (otherBall == null)
                continue;
            
            // 计算分离力
            Vector2 separationForce = CalculateSeparationForce(otherBall);
            if (separationForce.sqrMagnitude > 0.001f)
            {
                totalSeparationForce += separationForce;
                overlapCount++;
            }
        }
        
        // 4. 应用力
        if (totalSeparationForce.sqrMagnitude > 0.001f)
        {
            // 被抓住时降低力度
            float forceMultiplier = _isGrabbed ? _grabbedForceMultiplier : 1f;
            
            _rb.AddForce(totalSeparationForce * forceMultiplier, ForceMode2D.Force);
            
            if (_enableDebugLog && overlapCount > 0)
            {
                //DebugLog($"Separating from {overlapCount} balls, force={totalSeparationForce.magnitude:F2}");
            }
        }
    }

    /// <summary>
    /// 计算与另一个球的分离力
    /// </summary>
    private Vector2 CalculateSeparationForce(BallBase otherBall)
    {
        // 1. 计算方向和距离
        Vector2 direction = (Vector2)transform.position - (Vector2)otherBall.transform.position;
        float currentDistance = direction.magnitude;
        
        // 防止除零
        if (currentDistance < 0.001f)
        {
            // 如果完全重叠，给一个随机方向
            direction = Random.insideUnitCircle.normalized;
            currentDistance = 0.001f;
        }
        else
        {
            direction.Normalize();
        }
        
        // 2. 计算最小安全距离
        float otherRadius = otherBall._ballRadius;
        float minDistance = _ballRadius + otherRadius + _minSafetyMargin;
        
        // 3. 如果没有重叠，不施加力
        if (currentDistance >= minDistance)
            return Vector2.zero;
        
        // 4. 计算重叠量
        float overlap = minDistance - currentDistance;
        
        // 5. 分离力与重叠量成正比
        float forceMagnitude = overlap * _separationForce;
        
        return direction * forceMagnitude;
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