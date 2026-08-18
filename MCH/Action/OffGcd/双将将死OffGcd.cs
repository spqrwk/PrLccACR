using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MCH.Action.OffGcd;

public class 双将将死OffGcd : IDecisionResolver
{
    private static readonly TimeSpan DecisionThrottle = TimeSpan.FromMilliseconds(700);

    private uint _id;
    private uint _lastDecisionId;
    private DateTime _lastDecisionAt = DateTime.MinValue;

    public CheckResult Check()
    {
        if (ApiHelper.玩家 == null) return new(false, "玩家未加载");
        if (ApiHelper.读条中) return new(false, "读条中");
        if (ApiHelper.获取QT(MCHQT.停手)) return new(false, "停手");
        // NextOffGcd 会逐帧询问。在上一发尚未被游戏确认前，充能数不会立刻变化，
        // 不节流会让下一帧又返回另一发，视觉上像两个技能同时按下，并可能挤占 GCD。
        var sinceLastDecision = DateTime.UtcNow - _lastDecisionAt;
        if (sinceLastDecision < DecisionThrottle)
            return new(false, $"等待上一发({_lastDecisionId})确认");
        if (ApiHelper.动画锁定中) return new(false, "动画锁中");
        if (ApiHelper.GCD剩余 <= 0.7f) return new(false, $"GCD窗口不足({ApiHelper.GCD剩余:F2}s)");
        if (ApiHelper.队列中有技能) return new(false, "动作队列未清空");
        // 双将/将死/虹吸弹/弹射均为对目标技能：无目标或目标为玩家时不应尝试
        if (ApiHelper.目标 == null) return new(false, "无目标");
        if (ApiHelper.目标!.IsPlayer()) return new(false, "目标为玩家");

        // 调整技能ID：92级前=虹吸弹/弹射，92级=双将/将死
        var dcAdj = ApiHelper.调整技能ID(MCHSkill.虹吸弹);
        var cmAdj = ApiHelper.调整技能ID(MCHSkill.弹射);
        var dcOk = dcAdj != 0 && ApiHelper.技能充能(dcAdj) >= 1;
        var cmOk = cmAdj != 0 && ApiHelper.技能充能(cmAdj) >= 1;
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

    public PAction GetAction()
    {
        _lastDecisionId = _id;
        _lastDecisionAt = DateTime.UtcNow;
        return new(_id, ActionType.OffGcd, ActionTargetType.Target);
    }
}
