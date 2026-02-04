using UnityEngine;

/// <summary>
/// 钩爪状态枚举
/// </summary>
public enum ClawState
{
    Disabled,       // 禁用（非玩家操作阶段）
    Idle,           // 待机，可左右移动
    Descending,     // 下降中
    Grabbing,       // 夹取中
    Ascending,      // 上升中
    MovingToDrop,   // 移动到结算池
    Releasing,      // 松开
    Returning       // 返回初始位置
}

/// <summary>
/// 钩爪控制器，负责状态机、输入处理和移动
/// </summary>
public class ClawController : MonoBehaviour
{
    public static ClawController Instance { get; private set; }

    // Debug
    private const string SYSTEM = "Claw";
    private const string SCRIPT = "ClawController";
    [SerializeField] private bool _enableDebugLog = true;
    private void DebugLog(string msg) { if (_enableDebugLog) Debug.Log($"[{SYSTEM}][{SCRIPT}] {msg}"); }

    #region 配置

    [Header("水平移动")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private Transform _leftBound;
    [SerializeField] private Transform _rightBound;

    [Header("垂直移动")]
    [SerializeField] private float _descendSpeed = 3f;
    [SerializeField] private float _ascendSpeed = 2f;
    [SerializeField] private Transform _topPosition;
    [SerializeField] private Transform _bottomPosition;

    [Header("结算池")]
    [SerializeField] private Transform _dropZone;
    [SerializeField] private float _moveToDropSpeed = 4f;

    [Header("摆动")]
    [SerializeField] private Transform _clawPivot;
    [SerializeField] private float _swingAngle = 15f;
    [SerializeField] private float _swingSpeed = 8f;

    [Header("爪瓣 - 旋转轴")]
    [SerializeField] private Transform _leftClawPivot;
    [SerializeField] private Transform _rightClawPivot;

    [Header("爪瓣 - 视觉")]
    [SerializeField] private Transform _leftClaw;
    [SerializeField] private Transform _rightClaw;
    
    [Header("爪瓣 - 角度")]
    [Tooltip("待机时的角度（稍微张开）")]
    [SerializeField] private float _idleAngle = 15f;
    [Tooltip("完全张开的角度（下降/释放时）")]
    [SerializeField] private float _openAngle = 45f;
    [Tooltip("闭合角度（抓取时）")]
    [SerializeField] private float _closeAngle = 5f;
    [Tooltip("爪瓣旋转速度")]
    [SerializeField] private float _clawRotateSpeed = 5f;

    [Header("Grabbing阶段")]
    [SerializeField] private float _grabDuration = 0.3f;

    #endregion

    #region 运行时数据

    private ClawState _currentState = ClawState.Disabled;
    
    // 摆动
    private float _currentSwing = 0f;
    private float _targetSwing = 0f;
    
    // 爪瓣
    private float _currentClawAngle;
    private float _targetClawAngle;
    
    // Grabbing 计时
    private float _grabTimer = 0f;
    
    // 初始位置
    private Vector3 _initialPosition;

    #endregion

    #region 属性

    public ClawState CurrentState => _currentState;

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
        _initialPosition = transform.position;
        
        // 初始状态：爪瓣稍微张开（待机状态）
        _currentClawAngle = _idleAngle;
        _targetClawAngle = _idleAngle;
        ApplyClawAngle();
    }

    private void Update()
    {
        switch (_currentState)
        {
            case ClawState.Idle:
                UpdateIdle();
                break;
            case ClawState.Descending:
                UpdateDescending();
                break;
            case ClawState.Grabbing:
                UpdateGrabbing();
                break;
            case ClawState.Ascending:
                UpdateAscending();
                break;
            case ClawState.MovingToDrop:
                UpdateMovingToDrop();
                break;
            case ClawState.Releasing:
                UpdateReleasing();
                break;
            case ClawState.Returning:
                UpdateReturning();
                break;
        }

        UpdateSwing();
        UpdateClawAngle();
    }

    #endregion

    #region 状态控制

    /// <summary>
    /// 切换钩爪状态
    /// </summary>
    public void SetState(ClawState newState)
    {
        if (_currentState == newState) return;

        OnStateExit(_currentState);
        DebugLog($"State: {_currentState} → {newState}");
        _currentState = newState;
        
        #if UNITY_EDITOR
        _debugState = newState; // 同步到调试字段
        #endif
        
        OnStateEnter(newState);
    }

    private void OnStateEnter(ClawState state)
    {
        switch (state)
        {
            case ClawState.Idle:
                IdleClaw();
                break;
            case ClawState.Descending:
                OpenClaw();
                EventManager.Instance?.TriggerGrabStarted();
                break;
            case ClawState.Grabbing:
                _grabTimer = 0f;
                CloseClaw();
                break;
            case ClawState.Releasing:
                OpenClaw();
                EventManager.Instance?.TriggerGrabReleased();
                break;
            case ClawState.Returning:
                IdleClaw();
                break;
        }
    }

    private void OnStateExit(ClawState state)
    {
        // 预留，暂无逻辑
    }

    #endregion

    #region 状态更新

    private void UpdateIdle()
    {
        // 水平移动
        float horizontal = Input.GetAxisRaw("Horizontal");
        MoveHorizontal(horizontal);

        // 按空格开始下降
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetState(ClawState.Descending);
        }
    }

    private void UpdateDescending()
    {
        // 匀速下降
        Vector3 pos = transform.position;
        pos.y -= _descendSpeed * Time.deltaTime;

        // 到达底部
        if (pos.y <= _bottomPosition.position.y)
        {
            pos.y = _bottomPosition.position.y;
            transform.position = pos;
            SetState(ClawState.Grabbing);
            return;
        }

        transform.position = pos;
    }

    private void UpdateGrabbing()
    {
        _grabTimer += Time.deltaTime;

        // 停留时间结束
        if (_grabTimer >= _grabDuration)
        {
            SetState(ClawState.Ascending);
        }
    }

    private void UpdateAscending()
    {
        // 匀速上升
        Vector3 pos = transform.position;
        pos.y += _ascendSpeed * Time.deltaTime;

        // 到达顶部
        if (pos.y >= _topPosition.position.y)
        {
            pos.y = _topPosition.position.y;
            transform.position = pos;
            SetState(ClawState.MovingToDrop);
            return;
        }

        transform.position = pos;
    }

    private void UpdateMovingToDrop()
    {
        // 水平移动到结算池
        Vector3 pos = transform.position;
        float targetX = _dropZone.position.x;
        float direction = Mathf.Sign(targetX - pos.x);
        
        pos.x += direction * _moveToDropSpeed * Time.deltaTime;

        // 到达结算池
        if ((direction > 0 && pos.x >= targetX) || (direction < 0 && pos.x <= targetX))
        {
            pos.x = targetX;
            transform.position = pos;
            SetState(ClawState.Releasing);
            return;
        }

        transform.position = pos;
    }

    private void UpdateReleasing()
    {
        // 等待外部调用 OnSettlementComplete()
    }

    private void UpdateReturning()
    {
        // 水平移动回初始位置
        Vector3 pos = transform.position;
        float targetX = _initialPosition.x;
        float direction = Mathf.Sign(targetX - pos.x);
        
        pos.x += direction * _moveToDropSpeed * Time.deltaTime;

        // 到达初始位置
        if (Mathf.Abs(pos.x - targetX) < 0.01f)
        {
            pos.x = targetX;
            transform.position = pos;
            SetState(ClawState.Idle);
            return;
        }

        transform.position = pos;
    }

    #endregion

    #region 外部接口

    /// <summary>
    /// 结算完成后调用，触发返回
    /// </summary>
    public void OnSettlementComplete()
    {
        if (_currentState == ClawState.Releasing)
        {
            DebugLog("Settlement complete, returning");
            SetState(ClawState.Returning);
        }
    }

    #endregion

    #region 水平移动

    private void MoveHorizontal(float direction)
    {
        // 更新摆动目标
        _targetSwing = -direction * _swingAngle;

        if (direction == 0f) return;

        // 计算新位置
        Vector3 newPos = transform.position + Vector3.right * direction * _moveSpeed * Time.deltaTime;

        // 边界限制
        newPos.x = Mathf.Clamp(newPos.x, _leftBound.position.x, _rightBound.position.x);

        transform.position = newPos;
    }

    #endregion

    #region 摆动

    private void UpdateSwing()
    {
        // 无输入时回正
        if (_currentState == ClawState.Idle && Input.GetAxisRaw("Horizontal") == 0f)
        {
            _targetSwing = 0f;
        }

        // 非 Idle 状态也回正
        if (_currentState != ClawState.Idle)
        {
            _targetSwing = 0f;
        }

        // 平滑过渡
        _currentSwing = Mathf.Lerp(_currentSwing, _targetSwing, Time.deltaTime * _swingSpeed);

        // 应用旋转
        if (_clawPivot != null)
        {
            _clawPivot.localRotation = Quaternion.Euler(0f, 0f, _currentSwing);
        }
    }

    #endregion

    #region 爪瓣开合

    private void IdleClaw()
    {
        _targetClawAngle = _idleAngle;
        DebugLog($"Claw idle: → {_idleAngle}°");
    }

    private void OpenClaw()
    {
        _targetClawAngle = _openAngle;
        DebugLog($"Claw opening: → {_openAngle}°");
    }

    private void CloseClaw()
    {
        _targetClawAngle = _closeAngle;
        DebugLog($"Claw closing: → {_closeAngle}°");
    }

    private void UpdateClawAngle()
    {
        // 平滑过渡
        _currentClawAngle = Mathf.Lerp(_currentClawAngle, _targetClawAngle, Time.deltaTime * _clawRotateSpeed);
        ApplyClawAngle();
    }

    private void ApplyClawAngle()
    {
        // 旋转左右爪的pivot
        if (_leftClawPivot != null)
        {
            _leftClawPivot.localRotation = Quaternion.Euler(0f, 0f, -_currentClawAngle);  // 改为负
        }
        if (_rightClawPivot != null)
        {
            _rightClawPivot.localRotation = Quaternion.Euler(0f, 0f, _currentClawAngle);   // 改为正
        }
    }

    #endregion

    #region 调试工具

#if UNITY_EDITOR
    [Header("━━━━━━ 调试工具 ━━━━━━")]
    [SerializeField] private ClawState _debugState = ClawState.Disabled;
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            // 在运行时，如果手动修改了调试状态，则切换状态
            if (_debugState != _currentState)
            {
                SetState(_debugState);
            }
        }
    }
    
#endif

    #endregion

    #region Editor 可视化

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 绘制水平移动边界
        if (_leftBound != null && _rightBound != null)
        {
            Gizmos.color = Color.yellow;
            float topY = _topPosition != null ? _topPosition.position.y : transform.position.y + 2f;
            float bottomY = _bottomPosition != null ? _bottomPosition.position.y : transform.position.y - 2f;

            Gizmos.DrawLine(new Vector3(_leftBound.position.x, topY, 0f), new Vector3(_leftBound.position.x, bottomY, 0f));
            Gizmos.DrawLine(new Vector3(_rightBound.position.x, topY, 0f), new Vector3(_rightBound.position.x, bottomY, 0f));
        }

        // 绘制垂直移动范围
        if (_topPosition != null && _bottomPosition != null)
        {
            Gizmos.color = Color.cyan;
            float leftX = _leftBound != null ? _leftBound.position.x : transform.position.x - 2f;
            float rightX = _rightBound != null ? _rightBound.position.x : transform.position.x + 2f;

            Gizmos.DrawLine(new Vector3(leftX, _topPosition.position.y, 0f), new Vector3(rightX, _topPosition.position.y, 0f));
            Gizmos.DrawLine(new Vector3(leftX, _bottomPosition.position.y, 0f), new Vector3(rightX, _bottomPosition.position.y, 0f));
        }

        // 绘制结算池位置
        if (_dropZone != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_dropZone.position, 0.5f);
        }
    }
#endif

    #endregion
}