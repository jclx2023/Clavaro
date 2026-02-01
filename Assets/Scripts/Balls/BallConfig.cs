using UnityEngine;

/// <summary>
/// 球类型枚举
/// </summary>
public enum BallType
{
    Score,      // 基础分球
    Multiplier  // 乘倍球
}

/// <summary>
/// 球的配置数据（ScriptableObject）
/// 用于定义不同种类球的属性
/// </summary>
[CreateAssetMenu(fileName = "NewBallConfig", menuName = "Clavaro/BallConfig")]
public class BallConfig : ScriptableObject
{
    [Header("基础属性")]
    [Tooltip("球的名称标识")]
    public string ballName;
    
    [Tooltip("球的类型：分数球或乘倍球")]
    public BallType ballType;
    
    [Tooltip("数值：分数球为加分值，乘倍球为倍率")]
    public float value;
    
    [Tooltip("球的外观")]
    public Sprite sprite;
    
    [Tooltip("数值文本颜色")]
    public Color textColor = Color.white;

    [Header("尺寸")]
    [Tooltip("球的半径（用于生成时碰撞检测）")]
    public float radius = 0.5f;

    [Header("物理属性")]
    [Tooltip("质量")]
    public float mass = 1f;
    
    [Tooltip("线性阻力")]
    public float linearDrag = 0.5f;
    
    [Tooltip("角阻力")]
    public float angularDrag = 0.5f;
    
    [Tooltip("重力缩放")]
    public float gravityScale = 1f;
    
    [Tooltip("物理材质（摩擦力/弹性）")]
    public PhysicsMaterial2D physicsMaterial;

    /// <summary>
    /// 获取显示文本（如"+10"或"×1.5"）
    /// </summary>
    public string GetDisplayText()
    {
        return ballType == BallType.Score 
            ? $"+{value:0}" 
            : $"×{value:0.##}";
    }
}