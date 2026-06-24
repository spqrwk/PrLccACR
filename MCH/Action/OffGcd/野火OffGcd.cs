using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MCH.Action.OffGcd;

public class 野火OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (ApiHelper.玩家 == null) return new(false, "玩家未加载");
        if (ApiHelper.读条中) return new(false, "读条中");
        if (ApiHelper.目标 == null) return new(false, "无目标");
        if (ApiHelper.目标!.IsPlayer()) return new(false, "目标为玩家");
        if (MCHSettings.Instance.WFOnlyBOSS && !ApiHelper.目标.IsEnemy()) return new(false, "仅Boss野火");
        if (ApiHelper.获取QT(MCHQT.优先打123)) return new(false, "优先123");
        if (ApiHelper.获取QT(MCHQT.停手)) return new(false, "停手");
        if (!ApiHelper.获取QT(MCHQT.野火技能)) return new(false, "QT未开");
        if (!ApiHelper.技能已解锁(MCHSkill.野火)) return new(false, "未解锁");
        if (!ApiHelper.技能可用(MCHSkill.野火)) return new(false, "冷却中");
        if (ApiHelper.最近用过(MCHSkill.野火, 1500)) return new(false, "刚用过");
        if (ApiHelper.玩家有状态(MCHBuff.野火)) return new(false, "已贴野火");
        if (!超荷OffGcd.CanBurst()) return new(false, "爆发资源不足");

        // // GCD 后半窗口检测：野火只在 GCD 后半释放，给前半留时间插 GCD
        // if (!ApiHelper.GCD后半安全) return new(false, "等待GCD后半");

        // 野火优先 QT：只要满足条件就放
        if (ApiHelper.获取QT(MCHQT.野火优先)) return new(true, "野火优先");

        // GCD 长 + 野火转好：提前释放
        if (ApiHelper.GCD剩余 >= 2f) return new(true, "GCD长+野火转好");

        // 关键 GCD 即将转好时不放
        if (MCHHelper.SdjCD() < 0) return new(false, "关键技能即将冷却");

        return new(true, "野火就绪");
    }

    public PAction GetAction()
    {

        // === 后半就绪：野火 → GCD → 超荷 ===
        var queue = new List<PAction>();
        if (!ApiHelper.GCD后半窗口)
        {
            Thread.Sleep(600);
        }
        if (ApiHelper.玩家有状态(MCHBuff.全金属爆发预备) && ApiHelper.获取QT(MCHQT.全金属爆发))
            queue.Add(new PAction(MCHSkill.全金属爆发, ActionType.Gcd, ActionTargetType.Target));
        else
        {
            var tl = ApiHelper.GCD剩余 * 1000f + MCHSettings.Instance.Cdtolerance;
            if (MCHHelper.CheckReassembleGcd(tl, out var sid))
            {
                if (sid == MCHSkill.狙击弹 && ApiHelper.技能已解锁(MCHSkill.热弹) && ApiHelper.技能可用(MCHSkill.热弹))
                    sid = MCHSkill.热弹;
                queue.Add(new PAction(sid, ActionType.Gcd, ActionTargetType.Target));
            }
            else
                queue.Add(new PAction(MCHHelper.GetBaseComboAction(), ActionType.Gcd, ActionTargetType.Target));
        }

        queue.Add(ApiHelper.自我能力(MCHSkill.超荷));
        ApiHelper.排入队列(queue, 高优先: true);

        return new PAction(MCHSkill.野火, ActionType.OffGcd, ActionTargetType.Target);
    }
}
