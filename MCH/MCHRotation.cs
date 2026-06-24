using MCH.Action.Gcd;
using MCH.Action.OffGcd;
using MCH.Data;
using MCH.Opener;
using MCH.UI;
using ECommons.ExcelServices;
using ECommons.Logging;
using PromeRotation.Data;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;

namespace MCH;

[RotationMetadata((uint)Job.MCH, "LccMch", "LccMch", "1.0.0.0")]
public class MCHRotation : IRotation
{
    public string RotationName => "LccMch";
    public uint JobId => (uint)Job.MCH;

    private readonly IRotationEventHandler _eventHandler = new MCHRotationEventHandler();
    public IRotationEventHandler GetEventHandler() => _eventHandler;

    private readonly List<IDecisionResolver> _gcdResolvers = new();
    private readonly List<IDecisionResolver> _offGcdResolvers = new();

    // ============================================================
    // === QT & Opener 注册表 ===
    // ============================================================
    public static IReadOnlyDictionary<string, bool> QtList => MCHQT.All;

    public static IReadOnlyDictionary<string, Type> Openers { get; } = new Dictionary<string, Type>
    {
        { "MCH_空气锚起手100", typeof(MCHAirAnchorOpener100) },
        { "MCH_钻头起手100", typeof(MCHDrillOpener100) },
        { "MCH_绝妖星特化起手100", typeof(MCHKfkOpener100) },
        { "MCH_空气锚起手90", typeof(MCHAirAnchorOpener90) },
    };

    // ============================================================
    // === 构造 ===
    // ============================================================
    public MCHRotation()
    {
        _gcdResolvers.Add(new 过热连击Gcd());
        _gcdResolvers.Add(new 全金属爆发Gcd());
        _gcdResolvers.Add(new 触发技能Gcd());
        _gcdResolvers.Add(new 基础连击Gcd());

        _offGcdResolvers.Add(new 内丹OffGcd());
        _offGcdResolvers.Add(new 减伤OffGcd());
        _offGcdResolvers.Add(new 爆发药OffGcd());
        _offGcdResolvers.Add(new 野火OffGcd());
        _offGcdResolvers.Add(new 整备OffGcd());
        _offGcdResolvers.Add(new 枪管加热OffGcd());
        _offGcdResolvers.Add(new 超荷OffGcd());
        _offGcdResolvers.Add(new 机器人OffGcd());
        _offGcdResolvers.Add(new 双将将死OffGcd());

        foreach (var (n, d) in QtList) ApiHelper.添加QT(n, d);
        // 注册后恢复当前模式保存的 QT 默认值
        MCHSettings.Instance.RestoreQtSnapshot(MCHSettings.Instance.IsHighEnd);
        MCHHotkeyUI.Setup();
    }

    // ============================================================
    // === 起手 ===
    // ============================================================
    public IOpener? GetOpener()
    {
        if (!ApiHelper.获取QT(MCHQT.启用起手) && ApiHelper.玩家 == null) return null;
        var n = ApiHelper.时间轴起手名称;
        if (string.IsNullOrWhiteSpace(n)) n = ApiHelper.元数据起手名称;
        if (!string.IsNullOrWhiteSpace(n))
        {
            var openers = ApiHelper.获取起手列表((int)(ApiHelper.玩家?.ClassJob.RowId ?? 0));
            if (openers != null && openers.TryGetValue(n, out var t))
                try { return Activator.CreateInstance(t) as IOpener; }
                catch (Exception ex) { PluginLog.Error($"[ACR] 起手失败: {ex.Message}"); }
        }
        return MCHOpenerManager.GetOpener();
    }

    // ============================================================
    // === 循环出口 ===
    // ============================================================
    public PAction? NextAlways() => null;
    public PAction? NextGcd() { foreach (var r in _gcdResolvers) if (r.Check().Success) return r.GetAction(); return null; }
    public PAction? NextOffGcd() { foreach (var r in _offGcdResolvers) if (r.Check().Success) return r.GetAction(); return null; }

    public void UpdateDebugStatus()
    {
        ApiHelper.清除GCD调试状态();
        ApiHelper.清除oGCD调试状态();
        foreach (var r in _gcdResolvers) { var x = r.Check(); ApiHelper.添加GCD调试状态(r.GetType().Name, x.Success, x.Message); }
        foreach (var r in _offGcdResolvers) { var x = r.Check(); ApiHelper.添加oGCD调试状态(r.GetType().Name, x.Success, x.Message); }
    }

    // ============================================================
    // === UI 委托 ===
    // ============================================================
    public void DrawQTs() => MCHQTUI.Draw();
    public void DrawSettings() => MCHSettingsUI.Draw();
}
