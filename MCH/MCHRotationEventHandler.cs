using MCH.Data;
using MCH.Helper;
using PromeRotation.Rotation;

namespace MCH;

public class MCHRotationEventHandler : IRotationEventHandler, IRotationLifecycle
{
    private int _lastWfWarnTime;

    // === IRotationLifecycle ===
    public void OnEnterAcr()
    {
        ECommons.Logging.PluginLog.Information("[LccMch] ACR已加载");
        foreach (var (n, d) in MCHQT.All) ApiHelper.添加QT(n, d);
        ApiHelper.重建QT可见性();
        // 恢复当前模式保存的 QT 默认值
        var mode = MCHSettings.Instance.IsHighEnd ? "高难" : "日随";
        var count = MCHSettings.Instance.GetCurrentModeDefaults().Count;
        MCHSettings.Instance.RestoreQtSnapshot(MCHSettings.Instance.IsHighEnd);
        ApiHelper.提示($"LccMch [{mode}] 恢复{count}个默认值", 3);
        MCHMacroManager.Init();
    }

    public void OnExitAcr()
    {
        MCHMacroManager.Exit();
        MCHSettings.Instance.Save();
        ApiHelper.提示("LccMch ACR 已卸载", 3);
    }

    // === IRotationEventHandler ===
    public void OnUpdate()
    {
        ApiHelper.轮询联动();
        // 同步模式（PR 面板可能直接改了 QT 值）
        var qt = PromeRotation.Data.PromeSettings.Instance.QuickToggles;
        if (qt.TryGetValue(MCHQT.高难模式, out var m) && m != MCHSettings.Instance.IsHighEnd)
        {
            MCHSettings.Instance.IsHighEnd = m;
            MCHSettings.Instance.SyncQtToMode();
            ApiHelper.重建QT可见性();
        }
    }
    public void OnOutOfBattleUpdate() { }
    public void OnBattleStarted() => _lastWfWarnTime = 0;

    public void OnBattleUpdate()
    {
        if (!MCHSettings.Instance.IsHighEnd || !MCHSettings.Instance.BurstWarm) return;
        if (!ApiHelper.技能已解锁(MCHSkill.野火)) return;

        var wfCd = ApiHelper.技能冷却(MCHSkill.野火);
        if (wfCd > 0 && wfCd < 15
            && ApiHelper.获取QT(MCHQT.野火技能)
            && !ApiHelper.获取QT(MCHQT.优先打123)
            && !ApiHelper.获取QT(MCHQT.停手))
        {
            var now = System.Environment.TickCount;
            if (now - _lastWfWarnTime > 20000)
            {
                ApiHelper.提示("120秒爆发即将准备完成", 5);
                _lastWfWarnTime = now;
            }
        }
    }

    public void OnBattleEnded()
    {
        if (MCHSettings.Instance.AutoResetBattleData)
        {
            MCHBattleData.Instance.Reset();
            MCHSettings.Instance.ResetQt();
        }
        ApiHelper.起手已执行 = false;
        _lastWfWarnTime = 0;
    }

    public void OnTerritoryChanged(ushort territoryId)
    {
        MCHBattleData.Instance.Reset();
        ApiHelper.起手已执行 = false;
        _lastWfWarnTime = 0;
    }

    public void OnNoTarget() { }
}
