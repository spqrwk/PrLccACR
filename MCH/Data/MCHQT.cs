namespace MCH.Data;

/// <summary>QT 模式归属：通用 / 高难专属 / 日随专属</summary>
public enum QTMode { Common, HighEndOnly, DailyOnly }

/// <summary>
/// MCH QT 开关 — 唯一数据源。
/// 新增 QT 只需在此加一行，QtList / 默认值 / 宏命令全部自动同步。
/// </summary>
public class MCHQT
{
    // === 键名常量 ===
    public const string 启用起手 = "启用起手";
    public const string 停手 = "停手";
    public const string 自动减伤 = "自动减伤";
    public const string 攒资源 = "攒资源";
    public const string 野火优先 = "野火优先";
    public const string AOE = "AOE";
    public const string 全金属爆发 = "全金属爆发";
    public const string 回转飞锯 = "回转飞锯";
    public const string 掘地飞轮 = "掘地飞轮";
    public const string 空气锚 = "空气锚";
    public const string 钻头 = "钻头";
    public const string 保留2层双将 = "保留2层双将";
    public const string 保留2层将死 = "保留2层将死";
    public const string 爆发药 = "爆发药";
    public const string 爆发 = "爆发";
    public const string 整备 = "整备";
    public const string 优先打123 = "优先打123";
    public const string 超荷 = "超荷";
    public const string 野火技能 = "野火技能";
    public const string 枪管加热 = "枪管加热";
    public const string 机器人 = "机器人";
    public const string 高难模式 = "高难模式";

    // === 唯一数据源：键名 → 默认值 ===
    public static readonly IReadOnlyDictionary<string, bool> All = new Dictionary<string, bool>
    {
        { 启用起手, true },   { 停手, false },   { 自动减伤, false },
        { 攒资源, false },     { 野火优先, false }, { AOE, false },
        { 全金属爆发, true },  { 回转飞锯, true },  { 掘地飞轮, true },
        { 空气锚, true },      { 钻头, true },
        { 保留2层双将, false }, { 保留2层将死, false },
        { 爆发药, false },     { 爆发, true },     { 整备, true },
        { 优先打123, false },  { 超荷, true },     { 野火技能, true },
        { 枪管加热, true },    { 机器人, true },    { 高难模式, true },
    };

    /// <summary>获取指定 key 的默认值（不存在返回 false）</summary>
    public static bool Default(string key) => All.TryGetValue(key, out var v) && v;

    /// <summary>是否是元数据 QT（非实际战斗 QT，不参与保存/恢复）</summary>
    public static bool IsMetaKey(string key) => key == 高难模式;

    // === QT 联动表：trigger 键 → 应同步的 (目标键, 是否取反) ===
    /// <summary>QT 联动规则（invert=true 表示目标值=!触发值）</summary>
    public static readonly IReadOnlyDictionary<string, (string key, bool invert)[]> CascadeRules =
        new Dictionary<string, (string, bool)[]>
        {
            { 爆发, new[] { (野火技能, false), (枪管加热, false) } },
            { 攒资源, new[] {
                (爆发, true), (整备, true), (钻头, true),
                (野火技能, true), (枪管加热, true),
                (超荷, true), (机器人, true),
                (保留2层双将, false), (保留2层将死, false)
            }},
        };

    // === 模式归属元数据 ===
    /// <summary>每个 QT 的模式归属</summary>
    public static readonly IReadOnlyDictionary<string, QTMode> ModeCategories = new Dictionary<string, QTMode>
    {
        // 通用 — 两模式均可见（15 个）
        { 启用起手, QTMode.Common }, { 停手, QTMode.Common },
        { AOE, QTMode.Common }, { 优先打123, QTMode.Common },
        { 整备, QTMode.Common }, { 野火技能, QTMode.Common }, { 超荷, QTMode.Common },
        { 枪管加热, QTMode.Common }, { 机器人, QTMode.Common }, { 全金属爆发, QTMode.Common },
        { 回转飞锯, QTMode.Common }, { 掘地飞轮, QTMode.Common }, { 空气锚, QTMode.Common },
        { 钻头, QTMode.Common }, { 爆发, QTMode.Common },
        { 高难模式, QTMode.Common },
        // 高难专属（5 个）— 仅高难可见
        { 攒资源, QTMode.HighEndOnly }, { 野火优先, QTMode.HighEndOnly },
        { 爆发药, QTMode.HighEndOnly },
        { 保留2层双将, QTMode.HighEndOnly }, { 保留2层将死, QTMode.HighEndOnly },
        // 日随专属（1 个）— 仅日随可见
        { 自动减伤, QTMode.DailyOnly },
    };

    /// <summary>判断指定 QT 在当前模式下是否可见</summary>
    public static bool IsVisibleInMode(string key, bool isHighEnd) =>
        ModeCategories.TryGetValue(key, out var cat) && cat switch
        {
            QTMode.Common => true,
            QTMode.HighEndOnly => isHighEnd,
            QTMode.DailyOnly => !isHighEnd,
            _ => true
        };

    /// <summary>获取指定 QT 的模式归属（不存在返回 Common）</summary>
    public static QTMode GetModeCategory(string key) =>
        ModeCategories.TryGetValue(key, out var cat) ? cat : QTMode.Common;
}
