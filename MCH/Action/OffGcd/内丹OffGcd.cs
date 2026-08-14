using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MCH.Action.OffGcd;

public class 内丹OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (ApiHelper.玩家 == null) return new(false, "玩家未加载");
        if (ApiHelper.读条中) return new(false, "读条中");
        if (!MCHSettings.Instance.AutoSecondWind) return new(false, "自动内丹关闭");
        if (ApiHelper.GCD卡死) return new(false, "GCD卡死检测中");
        if (ApiHelper.GCD剩余 * 1000f < 600f) return new(false, "GCD窗口短");
        if (!ApiHelper.技能已解锁(MCHSkill.内丹)) return new(false, "未解锁");
        if (!ApiHelper.技能可用(MCHSkill.内丹)) return new(false, "冷却中");
        if (ApiHelper.最近用过(MCHSkill.内丹, 1000)) return new(false, "刚用过");
        var hp = ApiHelper.玩家血量;
        if (hp > MCHSettings.Instance.SecondWindThreshold) return new(false, $"血量({hp:F0}%)>{MCHSettings.Instance.SecondWindThreshold}%");
        return new(true, $"血量{hp:F0}%");
    }
    public PAction GetAction() => ApiHelper.自我能力(MCHSkill.内丹);
}
