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
        // 武装解除是对目标技能：目标须存活且可攻击，避免目标消失/起飞时执行失败
        if (ApiHelper.技能已解锁(MCHSkill.武装解除) && ApiHelper.技能可用(MCHSkill.武装解除)
            && t != null && t.IsEnemy() && !t.IsDead && ApiHelper.可攻击)
        {
            if (ApiHelper.有状态(t, MCHBuff.被武装解除)) return new(false, "已有武装解除");
            if (ApiHelper.技能冷却(MCHSkill.策动) > 0 && ApiHelper.技能冷却(MCHSkill.策动) < 2) return new(false, "策动刚用过");
            _id = MCHSkill.武装解除; return new(true, "武装解除就绪");
        }
        return new(false, "均不可用");
    }
    public PAction GetAction()
    {
        // 执行前防御：武装解除需目标存活且可攻击；若已失效退回策动或返回基础兜底
        if (_id == MCHSkill.武装解除)
        {
            var t = ApiHelper.目标;
            if (t == null || t.IsDead || !ApiHelper.可攻击)
                return ApiHelper.自我能力(MCHSkill.策动); // 退化为策动（自身减伤），避免空放
        }
        return _id == MCHSkill.策动 ? ApiHelper.自我能力(MCHSkill.策动) : new(_id, ActionType.OffGcd, ActionTargetType.Target);
    }
}
