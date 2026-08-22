using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MCH.Action.OffGcd;

public class 机器人OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (ApiHelper.玩家 == null) return new(false, "玩家未加载");
        if (MCHSettings.Instance.IsHighEnd && ApiHelper.目标 == null) return new(false, "高难模式无目标");
        if (ApiHelper.获取QT(MCHQT.停手)) return new(false, "停手");
        if (!ApiHelper.获取QT(MCHQT.机器人)) return new(false, "QT未开");
        var ra = MCHHelper.GetRobotAction();
        if (!ApiHelper.技能已解锁(ra)) return new(false, "未解锁");
        if (!ApiHelper.技能可用(ra)) return new(false, "冷却中");
        if (ApiHelper.玩家有状态(MCHBuff.过热)) return new(false, "过热中");
        if (ApiHelper.机器人激活) return new(false, "已激活");
        var b = MCHHelper.GetBattery();
        if (b < MCHSettings.Instance.MinBattery) return new(false, $"电量不足({b}<{MCHSettings.Instance.MinBattery})");
        return new(true, $"机器人就绪(电量{b})");
    }
    public PAction GetAction() => ApiHelper.自我能力(MCHHelper.GetRobotAction());
}
