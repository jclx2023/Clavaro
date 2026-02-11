using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家球池数据
/// 存储玩家通过商店购买/升级的球
/// </summary>
[System.Serializable]
public class PlayerDeckData
{
    [Tooltip("玩家拥有的球")]
    public List<SpawnEntry> playerBalls = new List<SpawnEntry>();

    /// <summary>
    /// 添加球到背包
    /// </summary>
    public void AddBall(BallConfig config, int count = 1)
    {
        // 查找是否已存在
        var existing = playerBalls.Find(e => e.config == config);
        if (existing.config != null)
        {
            // 已存在，增加数量
            int index = playerBalls.IndexOf(existing);
            existing.count += count;
            playerBalls[index] = existing;
        }
        else
        {
            // 新增
            playerBalls.Add(new SpawnEntry { config = config, count = count });
        }
    }

    /// <summary>
    /// 移除球
    /// </summary>
    public void RemoveBall(BallConfig config, int count = 1)
    {
        var existing = playerBalls.Find(e => e.config == config);
        if (existing.config != null)
        {
            int index = playerBalls.IndexOf(existing);
            existing.count -= count;
            
            if (existing.count <= 0)
            {
                playerBalls.RemoveAt(index);
            }
            else
            {
                playerBalls[index] = existing;
            }
        }
    }

    /// <summary>
    /// 获取总球数
    /// </summary>
    public int GetTotalBallCount()
    {
        int total = 0;
        foreach (var entry in playerBalls)
        {
            total += entry.count;
        }
        return total;
    }
}