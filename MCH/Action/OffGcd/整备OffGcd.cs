using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MCH.Action.OffGcd;

public class 整备OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (ApiHelper.玩家 == null) return new(false, "玩家未加载");
        if (ApiHelper.读条中) return new(false, "读条中");
        if (ApiHelper.获取QT(MCHQT.优先打123)) return new(false, "优先123");
        if (ApiHelper.获取QT(MCHQT.停手)) return new(false, "停手");
        if (!ApiHelper.获取QT(MCHQT.整备)) return new(false, "QT未开");
        if (ApiHelper.玩家有状态(MCHBuff.整备预备)) return new(false, "已有整备");
        if (ApiHelper.玩家有状态(MCHBuff.过热)) return new(false, "过热中");
        if (!ApiHelper.技能已解锁(MCHSkill.整备)) return new(false, "未解锁");
        if (ApiHelper.技能充能(MCHSkill.整备) < 1 ) return new(false, "无充能");
        if (ApiHelper.最近用过(MCHSkill.整备, 1000)) return new(false, "刚用过");
        if (!MCHHelper.CheckReassembleGcd(MCHSettings.Instance.Cdtolerance, out _)) return new(false, "无合适GCD");
        return new(true, "整备就绪");
    }
    public PAction GetAction() => ApiHelper.自我能力(MCHSkill.整备);
}
