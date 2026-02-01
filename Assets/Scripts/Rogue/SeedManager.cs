using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 种子管理器，为各子系统提供独立的确定性随机数生成器
/// </summary>
public class SeedManager : MonoBehaviour
{
    public static SeedManager Instance { get; private set; }

    // Debug
    private const string SYSTEM = "Rogue";
    private const string SCRIPT = "SeedManager";
    [SerializeField] private bool _enableDebugLog = true;
    private void DebugLog(string msg) { if (_enableDebugLog) Debug.Log($"[{SYSTEM}][{SCRIPT}] {msg}"); }

    #region 配置

    private const int SEED_LENGTH = 6;
    private const string SEED_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    #endregion

    #region 运行时数据

    private string _masterSeed;
    private Dictionary<string, System.Random> _subsystemRandoms;

    #endregion

    #region 属性

    public string MasterSeed => _masterSeed;

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
        DontDestroyOnLoad(gameObject);

        _subsystemRandoms = new Dictionary<string, System.Random>();
        DebugLog("Initialized");
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 初始化主种子
    /// </summary>
    /// <param name="seed">指定种子，为空则自动生成</param>
    public void InitializeSeed(string seed = null)
    {
        _masterSeed = string.IsNullOrEmpty(seed) ? GenerateRandomSeed() : seed.ToUpper();
        _subsystemRandoms.Clear();
        DebugLog($"Master Seed: {_masterSeed}");
    }

    /// <summary>
    /// 获取指定子系统的随机器（缓存复用）
    /// </summary>
    public System.Random GetRandom(string subsystem)
    {
        if (!_subsystemRandoms.TryGetValue(subsystem, out var random))
        {
            int hash = $"{_masterSeed}_{subsystem}".GetHashCode();
            random = new System.Random(hash);
            _subsystemRandoms[subsystem] = random;
            DebugLog($"Created Random for [{subsystem}], Hash: {hash}");
        }
        return random;
    }

    /// <summary>
    /// 获取带额外标识的随机器（不缓存，每次新建）
    /// </summary>
    public System.Random GetSeededRandom(string subsystem, string extra)
    {
        string key = $"{_masterSeed}_{subsystem}_{extra}";
        int hash = key.GetHashCode();
        DebugLog($"Created Seeded Random for [{subsystem}_{extra}], Hash: {hash}");
        return new System.Random(hash);
    }

    /// <summary>
    /// 重置指定子系统的随机器
    /// </summary>
    public void ResetSubsystem(string subsystem)
    {
        if (_subsystemRandoms.Remove(subsystem))
        {
            DebugLog($"Reset subsystem: {subsystem}");
        }
    }

    /// <summary>
    /// 重置所有子系统随机器
    /// </summary>
    public void ResetAllSubsystems()
    {
        _subsystemRandoms.Clear();
        DebugLog("Reset all subsystems");
    }

    #endregion

    #region 内部方法

    private string GenerateRandomSeed()
    {
        var sb = new StringBuilder(SEED_LENGTH);
        var random = new System.Random();

        for (int i = 0; i < SEED_LENGTH; i++)
        {
            sb.Append(SEED_CHARS[random.Next(SEED_CHARS.Length)]);
        }

        return sb.ToString();
    }

    #endregion
}