using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MCH.Action.OffGcd;

public class 爆发药OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (ApiHelper.玩家 == null) return new(false, "玩家未加载");
        if (ApiHelper.读条中) return new(false, "读条中");
        if (!MCHSettings.Instance.IsHighEnd) return new(false, "非高难");
        if (!ApiHelper.获取QT(MCHQT.爆发药)) return new(false, "QT未开");
        if (ApiHelper.获取QT(MCHQT.优先打123)) return new(false, "优先123");
        if (ApiHelper.获取QT(MCHQT.停手)) return new(false, "停手");
        if (!ApiHelper.获取QT(MCHQT.野火技能)) return new(false, "野火QT未开");
        if (!超荷OffGcd.CanBurst() && !(ApiHelper.技能已解锁(MCHSkill.枪管加热) && ApiHelper.技能可用(MCHSkill.枪管加热)))
            return new(false, "无爆发资源");
        var pot = ApiHelper.最佳爆发药;
        if (pot == 0) return new(false, "无可用爆发药");
        if (ApiHelper.技能已解锁(MCHSkill.野火))
        {
            var wf = ApiHelper.技能冷却(MCHSkill.野火);
            if (wf > MCHSettings.Instance.autoPotion) return new(false, $"野火CD({wf:F0}s)>{MCHSettings.Instance.autoPotion}s");
        }
        return new(true, "爆发药就绪");
    }
    public PAction GetAction() => new(ApiHelper.最佳爆发药, ActionType.Item, ActionTargetType.Self);
}
