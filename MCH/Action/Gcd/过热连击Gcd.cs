using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MCH.Action.Gcd;

public class 过热连击Gcd : IDecisionResolver
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
        if (ApiHelper.获取QT(MCHQT.停手)) return new(false, "停手");
        // 过热检查提前：过热期间只能打热冲击/自动弩，"优先打123"不适用，直接放行
        if (ApiHelper.玩家有状态(MCHBuff.过热))
        {
            var ha = MCHHelper.GetHeatBlastAction();
            if (!ApiHelper.技能已解锁(ha)) return new(false, "热冲击未解锁");
            return new(true, "过热连击");
        }
        // 非过热：基础连击会处理，这里不拦（避免与"优先打123"组合成 GCD 死路）
        if (ApiHelper.获取QT(MCHQT.优先打123)) return new(false, "优先123");
        return new(false, "未过热");
    }

    public PAction GetAction()
    {
        if (MCHHelper.ShouldUseAoe(5f, 120f, 5) && ApiHelper.技能已解锁(MCHSkill.自动弩))
        {
            // 复用 AE 版逻辑：用自动弩自身 ID 找目标（5目标/120°），找不到就回退热冲击
            var best = MCHHelper.FindBestAoeTarget(MCHSkill.自动弩, 120f, 5);
            if (best != null)
            {
                ApiHelper.切换目标(best);
                return new(MCHSkill.自动弩, ActionType.Gcd, ActionTargetType.Target);
            }
        }

        return new(MCHHelper.GetHeatBlastAction(), ActionType.Gcd, ActionTargetType.Target);
    }
}
