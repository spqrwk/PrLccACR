using System.Text.Json;
using PromeRotation.Helpers;

namespace MCH.Data;

/// <summary>
/// MCH 运行期设置（单例，支持 JSON 持久化）
/// 开关类配置走 QT；数值类配置走此 Settings
/// </summary>
public class MCHSettings
{
    private static MCHSettings? _instance;
    public static MCHSettings Instance => _instance ??= Load();

    /// <summary>三大件 CD 容忍度（毫秒），整备窗口预判用</summary>
    public int Cdtolerance = 250;

    /// <summary>停手持续时长（毫秒），到时自动解除</summary>
    public int HoldTime = 3000;

    /// <summary>高难模式 / 日随模式</summary>
    public bool IsHighEnd = true;

    /// <summary>是否仅对 Boss 放野火</summary>
    public bool WFOnlyBOSS = true;

    /// <summary>机器人最低电量阈值</summary>
    public int MinBattery = 50;

    /// <summary>超荷最低热量阈值</summary>
    public int MinHeat = 50;

    /// <summary>抢开阈值（毫秒），提前开第一发 GCD</summary>
    public int GrabItLimit = 300;

    /// <summary>起手使用爆发药</summary>
    public bool UsePotionInOpener = false;

    /// <summary>自动爆发药：野火 CD 小于 N 秒时自动吃</summary>
    public int autoPotion = 10;

    /// <summary>120s 爆发前 TTS 提示</summary>
    public bool BurstWarm = true;

    /// <summary>自动内丹</summary>
    public bool AutoSecondWind = true;

    /// <summary>内丹触发血量阈值（%）</summary>
    public float SecondWindThreshold = 40f;

    /// <summary>脱战自动速行</summary>
    public bool UsePeloton = false;

    /// <summary>自动停手机制</summary>
    public bool HandleStopMechs = false;

    /// <summary>进场自动重置战斗数据</summary>
    public bool AutoResetBattleData = true;

    /// <summary>欢迎语音</summary>
    public bool needwelcome = true;

    /// <summary>命令窗口开关</summary>
    public bool CommandWindowOpen = false;

    /// <summary>小怪密集度阈值</summary>
    public int ConcentrationThreshold = 5;

    /// <summary>小怪留爆发：拉怪过程</summary>
    public bool PullingNoBurst = true;

    /// <summary>小怪留爆发：即将死亡</summary>
    public bool NoBurst = true;

    /// <summary>小怪血量阈值</summary>
    public float MinMobHpPercent = 0.4f;

    /// <summary>小怪预计死亡时间阈值（秒）</summary>
    public int minTTK = 12;

    /// <summary>起手选择：0=空气锚 1=钻头 2=绝妖星</summary>
    public int Opener = 0;

    // ============================================================
    // === QT 默认值持久化（按模式） ===
    // ============================================================

    /// <summary>用户自定义的 QT 默认值（通用回退，ResetQt 时恢复）。初始值从 MCHQT.All 拷贝</summary>
    public Dictionary<string, bool> QtDefaultValues = new(MCHQT.All);

    /// <summary>高难模式专用 QT 默认值</summary>
    public Dictionary<string, bool> QtHighEndDefaults = new();

    /// <summary>日随模式专用 QT 默认值</summary>
    public Dictionary<string, bool> QtDailyDefaults = new();

    /// <summary>
    /// 从 QtDefaultValues 字典获取 QT 默认值，不存在则用 fallback
    /// </summary>
    public bool GetQtDefault(string key, bool fallback)
    {
        if (QtDefaultValues.TryGetValue(key, out var val))
            return val;
        return fallback;
    }

    /// <summary>
    /// 获取当前模式对应的默认值字典
    /// </summary>
    public Dictionary<string, bool> GetCurrentModeDefaults()
        => IsHighEnd ? QtHighEndDefaults : QtDailyDefaults;

    /// <summary>
    /// 保存当前所有 QT 状态为用户自定义默认值（写入当前模式字典）
    /// </summary>
    public void SaveCurrentQtAsDefault()
    {
        var dict = GetCurrentModeDefaults();
        foreach (var key in MCHQT.All.Keys)
        {
            if (MCHQT.IsMetaKey(key)) continue;
            dict[key] = ApiHelper.获取QT(key);
        }
        // 同时写入通用回退
        foreach (var key in MCHQT.All.Keys)
        {
            if (MCHQT.IsMetaKey(key)) continue;
            QtDefaultValues[key] = ApiHelper.获取QT(key);
        }
    }

    /// <summary>
    /// 保存当前 QT 状态到指定模式的默认值字典
    /// </summary>
    public void SaveQtSnapshot(bool toHighEnd)
    {
        var dict = toHighEnd ? QtHighEndDefaults : QtDailyDefaults;
        foreach (var key in MCHQT.All.Keys)
        {
            if (MCHQT.IsMetaKey(key)) continue;
            dict[key] = ApiHelper.获取QT(key);
        }
    }

    /// <summary>
    /// 从指定模式的默认值字典恢复 QT 状态
    /// </summary>
    public void RestoreQtSnapshot(bool fromHighEnd)
    {
        var dict = fromHighEnd ? QtHighEndDefaults : QtDailyDefaults;
        if (dict.Count == 0)
        {
            // 该模式尚无保存记录，用通用回退 + 模式默认值
            foreach (var key in MCHQT.All.Keys)
            {
                if (MCHQT.IsMetaKey(key)) continue;
                ApiHelper.设置QT(key, GetQtDefault(key, MCHQT.Default(key)));
            }
            ApplyModeDefaults(fromHighEnd);
        }
        else
        {
            foreach (var key in MCHQT.All.Keys)
            {
                if (MCHQT.IsMetaKey(key)) continue;
                ApiHelper.设置QT(key, dict.TryGetValue(key, out var v) ? v : MCHQT.Default(key));
            }
        }
    }

    /// <summary>
    /// 为指定模式设置专属 QT 的推荐默认值
    /// </summary>
    public void ApplyModeDefaults(bool isHighEnd)
    {
        if (isHighEnd)
        {
            ApiHelper.设置QT(MCHQT.自动减伤, false);
            ApiHelper.设置QT(MCHQT.AOE, false);
            ApiHelper.设置QT(MCHQT.爆发药, true);
        }
        else
        {
            ApiHelper.设置QT(MCHQT.自动减伤, true);
            ApiHelper.设置QT(MCHQT.AOE, true);
            ApiHelper.设置QT(MCHQT.爆发药, false);
        }
    }

    /// <summary>
    /// 统一模式切换入口：保存当前 → 切换 → 恢复目标模式
    /// </summary>
    public void SwitchMode(bool toHighEnd)
    {
        if (IsHighEnd == toHighEnd) return;
        SaveQtSnapshot(IsHighEnd);
        IsHighEnd = toHighEnd;
        ApiHelper.设置QT(MCHQT.高难模式, toHighEnd);
        RestoreQtSnapshot(toHighEnd);
        Save();
    }

    /// <summary>
    /// 重置所有 QT 到用户自定义默认值（当前模式）或通用回退
    /// </summary>
    public void ResetQt()
    {
        var dict = GetCurrentModeDefaults();
        if (dict.Count == 0) dict = QtDefaultValues;
        foreach (var key in MCHQT.All.Keys)
        {
            if (MCHQT.IsMetaKey(key)) continue;
            ApiHelper.设置QT(key, dict.TryGetValue(key, out var v) ? v : MCHQT.Default(key));
        }
    }

    /// <summary>
    /// 根据当前 IsHighEnd 同步关联的 QT 状态（高难/日随模式切换时调用）
    /// </summary>
    public void SyncQtToMode()
    {
        ApiHelper.设置QT(MCHQT.自动减伤, !IsHighEnd);
        ApiHelper.设置QT(MCHQT.AOE, !IsHighEnd);
        ApiHelper.设置QT(MCHQT.爆发药, IsHighEnd);
    }

    /// <summary>
    /// 只重置指定模式的默认值字典到内置值
    /// </summary>
    public void ResetModeDefaults(bool isHighEnd)
    {
        var dict = isHighEnd ? QtHighEndDefaults : QtDailyDefaults;
        dict.Clear();
        foreach (var (key, defVal) in MCHQT.All)
        {
            if (MCHQT.IsMetaKey(key)) continue;
            dict[key] = defVal;
        }
    }

    /// <summary>
    /// 将当前模式的默认值复制到另一模式
    /// </summary>
    public void CopyDefaultsToOtherMode()
    {
        var src = GetCurrentModeDefaults();
        var dst = IsHighEnd ? QtDailyDefaults : QtHighEndDefaults;
        if (src.Count == 0) return;
        foreach (var kv in src)
        {
            if (MCHQT.IsMetaKey(kv.Key)) continue;
            dst[kv.Key] = kv.Value;
        }
        Save();
    }

    // ============================================================
    // === JSON 持久化 ===
    // ============================================================
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true,
    };

    public static string FilePath
    {
        get
        {
            try
            {
                var root = CachePathHelper.EnsureAcrCacheRoot();
                return System.IO.Path.Combine(root, "LccMch.Settings.json");
            }
            catch
            {
                return System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                    "XIVLauncherCN", "pluginConfigs", "PromeRotation", "LccMch.Settings.json");
            }
        }
    }

    public static MCHSettings Load()
    {
        try
        {
            if (System.IO.File.Exists(FilePath))
            {
                var json = System.IO.File.ReadAllText(FilePath);
                var s = JsonSerializer.Deserialize<MCHSettings>(json, JsonOptions);
                if (s != null)
                {
                    s.SyncQtToMode();
                    // 同步模式 QT 值
                    ApiHelper.设置QT(MCHQT.高难模式, s.IsHighEnd);
                    return s;
                }
            }
        }
        catch (Exception e) { ECommons.Logging.PluginLog.Error($"[LccMch] 设置加载失败: {e.Message}"); }
        return new MCHSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, JsonOptions);
            var dir = System.IO.Path.GetDirectoryName(FilePath);
            if (dir != null && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(FilePath, json);
        }
        catch { /* 写失败静默 */ }
    }

}
