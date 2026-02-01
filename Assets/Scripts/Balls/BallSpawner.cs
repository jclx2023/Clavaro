using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生成配置条目
/// </summary>
[System.Serializable]
public struct SpawnEntry
{
    public BallConfig config;
    public int count;
}

/// <summary>
/// 球生成器，负责在指定区域生成初始球池
/// </summary>
public class BallSpawner : MonoBehaviour
{
    // Debug
    private const string SYSTEM = "Balls";
    private const string SCRIPT = "BallSpawner";
    [SerializeField] private bool _enableDebugLog = true;
    private void DebugLog(string msg) { if (_enableDebugLog) Debug.Log($"[{SYSTEM}][{SCRIPT}] {msg}"); }

    #region 配置

    [Header("生成配置")]
    [SerializeField] private GameObject _ballPrefab;
    [SerializeField] private List<SpawnEntry> _spawnConfigs;

    [Header("生成区域")]
    [SerializeField] private Transform _topLeft;
    [SerializeField] private Transform _bottomRight;

    [Header("生成参数")]
    [SerializeField] private float _spawnInterval = 0.05f;
    [SerializeField] private int _maxRetries = 100;

    #endregion

    #region 运行时数据

    private List<BallBase> _spawnedBalls = new List<BallBase>();
    private System.Random _random;

    // 生成时的位置和半径记录（用于重叠检测）
    private List<Vector2> _spawnedPositions = new List<Vector2>();
    private List<float> _spawnedRadii = new List<float>();

    #endregion

    #region 属性

    public List<BallBase> SpawnedBalls => _spawnedBalls;

    #endregion

    #region 公开方法

    /// <summary>
    /// 开始生成所有球
    /// </summary>
    public void SpawnAllBalls()
    {
        StartCoroutine(SpawnAllBallsCoroutine());
    }

    /// <summary>
    /// 清除所有已生成的球
    /// </summary>
    public void ClearAllBalls()
    {
        foreach (var ball in _spawnedBalls)
        {
            if (ball != null)
            {
                Destroy(ball.gameObject);
            }
        }
        _spawnedBalls.Clear();
        _spawnedPositions.Clear();
        _spawnedRadii.Clear();
        DebugLog("All balls cleared");
    }

    /// <summary>
    /// 重新生成所有球
    /// </summary>
    public void RespawnAllBalls()
    {
        ClearAllBalls();
        SpawnAllBalls();
    }

    #endregion

    #region 内部方法

    private IEnumerator SpawnAllBallsCoroutine()
    {
        // 获取随机器
        _random = SeedManager.Instance.GetRandom("BallSpawner");

        // 展开配置列表
        List<BallConfig> ballsToSpawn = ExpandSpawnConfigs();

        // 随机打乱顺序
        ShuffleList(ballsToSpawn);

        DebugLog($"Starting spawn: {ballsToSpawn.Count} balls");

        // 清空位置记录
        _spawnedPositions.Clear();
        _spawnedRadii.Clear();

        // 逐个生成
        int successCount = 0;
        for (int i = 0; i < ballsToSpawn.Count; i++)
        {
            BallConfig config = ballsToSpawn[i];
            Vector2? position = FindValidPosition(config.radius);

            if (position.HasValue)
            {
                SpawnBall(config, position.Value);
                successCount++;
            }
            else
            {
                DebugLog($"Warning: Could not find valid position for ball {i} ({config.ballName})");
            }

            if (_spawnInterval > 0)
            {
                yield return new WaitForSeconds(_spawnInterval);
            }
        }

        DebugLog($"Spawn completed: {successCount}/{ballsToSpawn.Count} balls");
        EventManager.Instance?.TriggerBallsSpawnCompleted(successCount);
    }

    private List<BallConfig> ExpandSpawnConfigs()
    {
        List<BallConfig> result = new List<BallConfig>();

        foreach (var entry in _spawnConfigs)
        {
            for (int i = 0; i < entry.count; i++)
            {
                result.Add(entry.config);
            }
        }

        return result;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _random.Next(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private Vector2? FindValidPosition(float radius)
    {
        float minX = _topLeft.position.x + radius;
        float maxX = _bottomRight.position.x - radius;
        float minY = _bottomRight.position.y + radius;
        float maxY = _topLeft.position.y - radius;

        for (int retry = 0; retry < _maxRetries; retry++)
        {
            float x = minX + (float)_random.NextDouble() * (maxX - minX);
            float y = minY + (float)_random.NextDouble() * (maxY - minY);
            Vector2 newPos = new Vector2(x, y);

            if (!CheckOverlap(newPos, radius))
            {
                return newPos;
            }
        }

        return null;
    }

    private bool CheckOverlap(Vector2 position, float radius)
    {
        for (int i = 0; i < _spawnedPositions.Count; i++)
        {
            float minDistance = radius + _spawnedRadii[i];
            if (Vector2.Distance(position, _spawnedPositions[i]) < minDistance)
            {
                return true;
            }
        }
        return false;
    }

    private void SpawnBall(BallConfig config, Vector2 position)
    {
        GameObject ballObj = Instantiate(_ballPrefab, position, Quaternion.identity, transform);
        ballObj.name = $"Ball_{config.ballName}_{_spawnedBalls.Count}";

        BallBase ball = ballObj.GetComponent<BallBase>();
        if (ball != null)
        {
            ball.Initialize(config);
            _spawnedBalls.Add(ball);
            _spawnedPositions.Add(position);
            _spawnedRadii.Add(config.radius);
        }
        else
        {
            DebugLog($"Error: BallBase component not found on prefab");
            Destroy(ballObj);
        }
    }

    #endregion

    #region Editor 可视化

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_topLeft == null || _bottomRight == null) return;

        Gizmos.color = Color.cyan;

        Vector3 topLeft = _topLeft.position;
        Vector3 bottomRight = _bottomRight.position;
        Vector3 topRight = new Vector3(bottomRight.x, topLeft.y, 0);
        Vector3 bottomLeft = new Vector3(topLeft.x, bottomRight.y, 0);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
#endif

    #endregion
}