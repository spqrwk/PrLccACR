using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MCH.Action.OffGcd;

public class 减伤OffGcd : IDecisionResolver
{
    private uint _id;

    public CheckResult Check()
    {
        if (ApiHelper.玩家 == null) return new(false, "玩家未加载");
        if (MCHSettings.Instance.IsHighEnd) return new(false, "高难不自动");
        if (!ApiHelper.获取QT(MCHQT.自动减伤)) return new(false, "QT未开");

        if (ApiHelper.技能已解锁(MCHSkill.策动) && ApiHelper.技能可用(MCHSkill.策动))
        {
            if (ApiHelper.玩家有状态(MCHBuff.策动)) return new(false, "已有策动");
            if (ApiHelper.玩家有状态(1826) || ApiHelper.玩家有状态(1934)) return new(false, "已有远程减伤"); // Samba/Troubadour
            _id = MCHSkill.策动; return new(true, "策动就绪");
        }

        var t = ApiHelper.目标;
        if (ApiHelper.技能已解锁(MCHSkill.武装解除) && ApiHelper.技能可用(MCHSkill.武装解除) && t != null && t.IsEnemy())
        {
            if (ApiHelper.有状态(t, MCHBuff.被武装解除)) return new(false, "已有武装解除");
            if (ApiHelper.技能冷却(MCHSkill.策动) > 0 && ApiHelper.技能冷却(MCHSkill.策动) < 2) return new(false, "策动刚用过");
            _id = MCHSkill.武装解除; return new(true, "武装解除就绪");
        }
        return new(false, "均不可用");
    }
    public PAction GetAction() => _id == MCHSkill.策动 ? ApiHelper.自我能力(MCHSkill.策动) : new(_id, ActionType.OffGcd, ActionTargetType.Target);
}
