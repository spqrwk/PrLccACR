using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MCH.Action.OffGcd;

public class 超荷OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (ApiHelper.玩家 == null) return new(false, "玩家未加载");
        if (ApiHelper.读条中) return new(false, "读条中");
        if (ApiHelper.GCD剩余 * 1000f < 600f) return new(false, "GCD窗口短");
        if (ApiHelper.获取QT(MCHQT.优先打123)) return new(false, "优先123");
        if (ApiHelper.获取QT(MCHQT.停手)) return new(false, "停手");
        if (!ApiHelper.获取QT(MCHQT.超荷)) return new(false, "QT未开");
        if (!ApiHelper.技能已解锁(MCHSkill.超荷)) return new(false, "未解锁");
        if (ApiHelper.玩家有状态(MCHBuff.整备预备)) return new(false, "有整备");
        if (ApiHelper.玩家有状态(MCHBuff.过热)) return new(false, "已过热");
        if (ApiHelper.玩家有状态(MCHBuff.全金属爆发预备)) return new(false, "有全金属");
        if (ApiHelper.玩家等级 < 30) return new(false, "等级<30");
        if (!CanBurst()) return new(false, "爆发资源不足");
        if (MCHHelper.SdjCD() < 0) return new(false, "关键技能即将冷却");
        if (!ApiHelper.技能可用(MCHSkill.超荷)) return new(false, "冷却中");
        if (ApiHelper.最近用过(MCHSkill.超荷, 1200)) return new(false, "刚用过");
        if (!MCHSettings.Instance.IsHighEnd) return new(true, "日随超荷");
        if (ApiHelper.获取QT(MCHQT.野火技能) && ApiHelper.技能已解锁(MCHSkill.野火))
        { var wf = ApiHelper.技能冷却(MCHSkill.野火); if (wf <= 5 && wf > 0) return new(false, "等野火"); }
        var heat = MCHHelper.GetHeat();
        if (heat < MCHSettings.Instance.MinHeat && !ApiHelper.玩家有状态(MCHBuff.超荷预备)) return new(false, $"热量不足({heat}<{MCHSettings.Instance.MinHeat})");
        if (ApiHelper.玩家有状态(MCHBuff.超荷预备) && !ApiHelper.玩家有状态(MCHBuff.野火) && heat < 50) return new(false, "预备+无野火+热量<50");
        if (ApiHelper.连击剩余 < 8 && ApiHelper.连击剩余 > 0) return new(false, "连击保护");
        return new(true, "超荷就绪");
    }
    public PAction GetAction() => ApiHelper.自我能力(MCHSkill.超荷);

    public static bool CanBurst()
        => (ApiHelper.玩家有状态(MCHBuff.超荷预备) || MCHHelper.GetHeat() >= 50)
           && ApiHelper.获取QT(MCHQT.超荷);
}
