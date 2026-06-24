using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MCH.Action.OffGcd;

public class 双将将死OffGcd : IDecisionResolver
{
    private uint _id;

    public CheckResult Check()
    {
        if (ApiHelper.玩家 == null) return new(false, "玩家未加载");
        if (ApiHelper.读条中) return new(false, "读条中");
        if (ApiHelper.GCD剩余 * 1000f < 600f) return new(false, "GCD窗口短");
        if (ApiHelper.获取QT(MCHQT.停手)) return new(false, "停手");

        // 调整技能ID：92级前=虹吸弹/弹射，92级=双将/将死
        var dcAdj = ApiHelper.调整技能ID(MCHSkill.虹吸弹);
        var cmAdj = ApiHelper.调整技能ID(MCHSkill.弹射);
        var dcOk = dcAdj != 0 && ApiHelper.技能充能(dcAdj) > 0;
        var cmOk = cmAdj != 0 && ApiHelper.技能充能(cmAdj) > 0;
        if (!dcOk && !cmOk) return new(false, "虹吸弹/弹射无充能");

        var dcChg = dcOk ? (int)ApiHelper.技能充能(dcAdj) : 0;
        var cmChg = cmOk ? (int)ApiHelper.技能充能(cmAdj) : 0;
        var resDc = ApiHelper.获取QT(MCHQT.保留2层双将);
        var resCm = ApiHelper.获取QT(MCHQT.保留2层将死);
        var canDc = !resDc || dcChg >= 3;
        var canCm = !resCm || cmChg >= 3;
        if (!canDc && !canCm) return new(false, "保留限制");

        if (canDc && canCm && dcOk && cmOk) { _id = dcChg >= cmChg ? dcAdj : cmAdj; return new(true, dcChg >= cmChg ? $"DC({dcChg})" : $"CM({cmChg})"); }
        if (canDc && dcOk) { _id = dcAdj; return new(true, $"DC({dcChg})"); }
        if (canCm && cmOk) { _id = cmAdj; return new(true, $"CM({cmChg})"); }
        return new(false, "不满足");
    }
    public PAction GetAction() => new(_id, ActionType.OffGcd, ActionTargetType.Target);
}
