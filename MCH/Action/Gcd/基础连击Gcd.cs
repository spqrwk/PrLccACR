using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MCH.Action.Gcd;

public class 基础连击Gcd : IDecisionResolver
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
        if (ApiHelper.获取QT(MCHQT.停手)) return new(false, "停手");
        if (ApiHelper.玩家有状态(MCHBuff.过热)) return new(false, "过热中");
        return new(true, "基础连击");
    }

    public PAction GetAction()
    {
        var best = MCHHelper.FindBestAoeTarget(MCHSkill.散射, 120f, 3);
        if (best != null)
        {
            ApiHelper.切换目标(best);
            return new(MCHHelper.GetAoeAction(), ActionType.Gcd, ActionTargetType.Target);
        }
        return new(MCHHelper.GetBaseComboAction(), ActionType.Gcd, ActionTargetType.Target);
    }
}
