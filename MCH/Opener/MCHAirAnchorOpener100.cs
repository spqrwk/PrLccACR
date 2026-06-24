using System.Collections.Generic;
using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MCH.Opener;

public class MCHAirAnchorOpener100 : IOpener
{
    public string OpenerName => "MCH_空气锚起手100";

    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        countdownHandler.AddAction(3500, O(MCHSkill.整备, ActionTargetType.Self));
        if (MCHSettings.Instance.UsePotionInOpener)
            countdownHandler.AddAction(MCHSettings.Instance.GrabItLimit + 1000,
                new PAction(ApiHelper.最佳爆发药, ActionType.Item, ActionTargetType.Self));
        countdownHandler.AddAction(MCHSettings.Instance.GrabItLimit, G(MCHSkill.空气锚));
    }

    public List<PAction> InCombatSequence
    {
        get
        {
            if (!MCHOpenerChecker.CheckOpenerOutbreakSpells(MCHSkill.空气锚)) return new List<PAction>();
            var s = new List<PAction>();
            if (ApiHelper.技能可用(MCHSkill.空气锚) && ApiHelper.技能已解锁(MCHSkill.空气锚))
            {
                if (!ApiHelper.玩家有状态(MCHBuff.整备预备)) s.Add(O(MCHSkill.整备, ActionTargetType.Self));
                s.Add(G(MCHSkill.空气锚));
            }
            s.Add(O(MCHSkill.将死)); s.Add(O(MCHSkill.双将));
            s.Add(G(MCHSkill.钻头)); s.Add(O(MCHSkill.枪管加热, ActionTargetType.Self));
            s.Add(G(MCHSkill.回转飞锯)); s.Add(O(MCHSkill.双将)); s.Add(O(MCHSkill.将死));
            s.Add(G(MCHSkill.掘地飞轮)); s.Add(O(MCHSkill.后式自走人偶, ActionTargetType.Self)); s.Add(O(MCHSkill.野火));
            s.Add(G(MCHSkill.全金属爆发)); s.Add(O(MCHSkill.双将)); s.Add(O(MCHSkill.超荷, ActionTargetType.Self));
            s.Add(G(MCHSkill.烈焰弹)); s.Add(O(MCHSkill.将死));
            s.Add(G(MCHSkill.烈焰弹)); s.Add(O(MCHSkill.双将));
            s.Add(G(MCHSkill.烈焰弹)); s.Add(O(MCHSkill.将死));
            s.Add(G(MCHSkill.烈焰弹)); s.Add(O(MCHSkill.双将));
            s.Add(G(MCHSkill.烈焰弹)); s.Add(O(MCHSkill.整备, ActionTargetType.Self));
            s.Add(G(MCHSkill.钻头)); s.Add(O(MCHSkill.双将)); s.Add(O(MCHSkill.将死));
            s.Add(G(MCHSkill.钻头));
            return s;
        }
    }

    private static PAction G(uint id) => new(id, ActionType.Gcd, ActionTargetType.Target) { RequiresVerification = true };
    private static PAction O(uint id, ActionTargetType t = ActionTargetType.Target) => new(id, ActionType.OffGcd, t);
}
