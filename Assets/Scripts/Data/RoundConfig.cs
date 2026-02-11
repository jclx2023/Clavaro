using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回合配置数据
/// 定义单个回合的目标、次数和默认球池
/// </summary>
[CreateAssetMenu(fileName = "NewRoundConfig", menuName = "Clavaro/RoundConfig")]
public class RoundConfig : ScriptableObject
{
    [Header("回合目标")]
    [Tooltip("通关所需分数")]
    public int targetScore = 300;
    
    [Tooltip("可用抓取次数")]
    public int grabCount = 5;

    [Header("回合默认球池")]
    [Tooltip("该回合额外提供的基础球池（不包含玩家背包）")]
    public List<SpawnEntry> defaultBallPool = new List<SpawnEntry>();

    [Header("回合类型")]
    [Tooltip("是否为Boss回合")]
    public bool isBossRound = false;
    
    [Tooltip("Boss回合的Debuff（预留）")]
    public List<string> debuffs = new List<string>();
}