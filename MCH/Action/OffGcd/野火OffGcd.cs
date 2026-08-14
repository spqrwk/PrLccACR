using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MCH.Action.OffGcd;

public class 野火OffGcd : IDecisionResolver
{
    /// <summary>野火入队时间戳（DateTime.MinValue = 未排队），用于防重入：仅拦自己排的野火，不拦其他技能</summary>
    private DateTime _wfQueuedAt = DateTime.MinValue;

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
        // 超时兜底：入队超过 5s 视为队列已消费/失败，先解除标记（放行下一帧的 Check），防止野火被永久压制
        if (_wfQueuedAt != DateTime.MinValue && (DateTime.UtcNow - _wfQueuedAt).TotalSeconds > 5)
            _wfQueuedAt = DateTime.MinValue;
        // 防重入：仅当野火自己已入队（5s 内）且队列未清空时拦截；其他技能排队不再压制野火
        if (_wfQueuedAt != DateTime.MinValue && ApiHelper.队列中有技能) return new(false, "已排列野火");
           

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

    public PAction? GetAction()
    {
        // === 野火 → GCD → 超荷，打包高优队列一次性执行 ===
        var queue = new List<PAction>();

        if(ApiHelper.最近用过(MCHSkill.野火, 1500))
            return null;
        // 1. 野火（oGCD，WeaveDelay 确保后半 GCD 释放）
        queue.Add(new PAction(MCHSkill.野火, ActionType.OffGcd, ActionTargetType.Target)
                      .WithWeaveDelay(600));

        // 2. GCD：整备高价值 > 基础连击兜底
        if (ApiHelper.玩家有状态(MCHBuff.全金属爆发预备) && ApiHelper.获取QT(MCHQT.全金属爆发))
            queue.Add(new PAction(MCHSkill.全金属爆发, ActionType.Gcd, ActionTargetType.Target));
        else if (MCHHelper.CheckReassembleGcd(ApiHelper.GCD剩余 * 1000f + MCHSettings.Instance.Cdtolerance, out var sid))
            queue.Add(new PAction(sid, ActionType.Gcd, ActionTargetType.Target));
        else
            queue.Add(new PAction(MCHHelper.GetBaseComboAction(), ActionType.Gcd, ActionTargetType.Target));

        // 3. 超荷（oGCD）
        queue.Add(ApiHelper.自我能力(MCHSkill.超荷));

        ApiHelper.排入队列(queue);
        _wfQueuedAt = DateTime.UtcNow; // 记录入队时间，Check 据此防重入

        // Group 自动执行，不返回
        return null;
    }
}
