using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MCH.Action.Gcd;

public class 全金属爆发Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var r = ApiHelper.攻击距离(25);
        var p = ApiHelper.玩家;
        if (p == null) return new(false, "玩家未加载");
        if (ApiHelper.目标 == null) return new(false, "无目标");
        if (ApiHelper.目标!.EntityId == p.EntityId) return new(false, "目标为自己");
        if (ApiHelper.目标.IsPlayer()) return new(false, "目标为玩家");
        if (p.DistanceToMe() > r) return new(false, $"过远(>{r}m)");
        if (ApiHelper.读条中) return new(false, "读条中");
        if (ApiHelper.获取QT(MCHQT.优先打123)) return new(false, "优先123");
        if (ApiHelper.获取QT(MCHQT.停手)) return new(false, "停手");
        if (ApiHelper.玩家有状态(MCHBuff.过热)) return new(false, "过热中");

        if (!ApiHelper.获取QT(MCHQT.全金属爆发)) return new(false, "QT未开");
        if (ApiHelper.有状态(p, MCHBuff.全金属爆发预备) && ApiHelper.状态剩余(p, MCHBuff.全金属爆发预备) <5) return new(true, "全金属buff不足");
        if (ApiHelper.技能已解锁(MCHSkill.野火) && (ApiHelper.技能可用(MCHSkill.野火)|| ApiHelper.技能冷却(MCHSkill.野火) < 25)) return new(false, "未解锁");
        if (!ApiHelper.玩家有状态(MCHBuff.全金属爆发预备)) return new(false, "无全金属buff");

        if (ApiHelper.玩家有状态(MCHBuff.全金属爆发预备)) return new(true, "全金属预备");
        if (!ApiHelper.技能已解锁(MCHSkill.全金属爆发)) return new(false, "未解锁");
        if (!ApiHelper.技能可用(MCHSkill.全金属爆发)) return new(false, "冷却中");

        if (!ApiHelper.玩家有状态(MCHBuff.野火))
        {
            var wfReady = ApiHelper.技能已解锁(MCHSkill.野火) && ApiHelper.技能可用(MCHSkill.野火);
            var fmfRemain = ApiHelper.状态剩余(p, MCHBuff.全金属爆发预备) * 1000f;
            var wfCdMs = ApiHelper.技能冷却(MCHSkill.野火) * 1000f;
            if (wfReady || fmfRemain >= wfCdMs + 2500f) return new(false, "等野火");
        }
        return new(true, "全金属就绪");
    }

    public PAction GetAction() => new(MCHSkill.全金属爆发, ActionType.Gcd, ActionTargetType.Target);
}
