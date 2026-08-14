using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MCH.Action.Gcd;

public class 触发技能Gcd : IDecisionResolver
{
    private uint _id;

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
        // GCD 卡死检测激活时 GCD剩余被框架强制归零，"即将就绪"窗口预判会失真；
        // 此时只放行已完全转好的技能（onlyReady），不拦截真可用
        if (ApiHelper.GCD卡死)
        {
            if (!MCHHelper.CheckReassembleGcd(MCHSettings.Instance.Cdtolerance, out var sidReady, onlyReady: true)) { _id = 0; return new(false, "GCD卡死+无就绪技能"); }
            if (sidReady == MCHSkill.狙击弹 && ApiHelper.技能已解锁(MCHSkill.热弹) && ApiHelper.技能可用(MCHSkill.热弹))
                sidReady = MCHSkill.热弹;
            _id = sidReady;
            return new(true, $"卡死就绪({_id})");
        }

        var tl = ApiHelper.GCD剩余 * 1000f + MCHSettings.Instance.Cdtolerance;
        if (!MCHHelper.CheckReassembleGcd(tl, out var sid)) { _id = 0; return new(false, "无高价值GCD"); }
        if (sid == MCHSkill.狙击弹 && ApiHelper.技能已解锁(MCHSkill.热弹) && ApiHelper.技能可用(MCHSkill.热弹))
            sid = MCHSkill.热弹;
        _id = sid;
        return new(true, $"就绪({_id})");
    }

    public PAction GetAction() => new(_id, ActionType.Gcd, ActionTargetType.Target);
}
