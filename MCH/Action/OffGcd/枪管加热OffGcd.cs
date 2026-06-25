using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MCH.Action.OffGcd;

public class 枪管加热OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (ApiHelper.玩家 == null) return new(false, "玩家未加载");
        if (ApiHelper.读条中) return new(false, "读条中");
        if (ApiHelper.获取QT(MCHQT.优先打123)) return new(false, "优先123");
        if (ApiHelper.获取QT(MCHQT.停手)) return new(false, "停手");
        if (!ApiHelper.获取QT(MCHQT.枪管加热)) return new(false, "QT未开");
        if (!ApiHelper.技能已解锁(MCHSkill.枪管加热)) return new(false, "未解锁");
        if (!ApiHelper.技能可用(MCHSkill.枪管加热)) return new(false, "冷却中");
        if (ApiHelper.最近用过(MCHSkill.枪管加热, 1000)) return new(false, "刚用过");
        if (ApiHelper.玩家有状态(MCHBuff.超荷预备)) return new(false, "已有超荷预备");
        if (ApiHelper.玩家有状态(MCHBuff.过热)) return new(false, "过热中");
        return new(true, "枪管加热就绪");
    }
    public PAction GetAction() => ApiHelper.自我能力(MCHSkill.枪管加热);
}
