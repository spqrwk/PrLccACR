using System.Collections.Generic;
using MCH.Data;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MCH.Opener;

public class MCHAirAnchorOpener90 : IOpener
{
    public string OpenerName => "MCH_空气锚起手90";

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
            s.Add(O(MCHSkill.虹吸弹)); s.Add(O(MCHSkill.弹射));
            s.Add(G(MCHSkill.钻头)); s.Add(O(MCHSkill.枪管加热, ActionTargetType.Self)); s.Add(O(MCHSkill.整备, ActionTargetType.Self));
            s.Add(G(MCHSkill.回转飞锯));
            s.Add(G(MCHSkill.分裂弹)); s.Add(O(MCHSkill.虹吸弹)); s.Add(O(MCHSkill.弹射));
            s.Add(G(MCHSkill.独头弹)); s.Add(O(MCHSkill.虹吸弹)); s.Add(O(MCHSkill.野火));
            s.Add(G(MCHSkill.狙击弹)); s.Add(O(MCHSkill.超荷, ActionTargetType.Self)); s.Add(O(MCHSkill.车式浮空炮塔, ActionTargetType.Self));
            s.Add(G(MCHSkill.烈焰弹)); s.Add(O(MCHSkill.虹吸弹));
            s.Add(G(MCHSkill.烈焰弹)); s.Add(O(MCHSkill.弹射));
            s.Add(G(MCHSkill.烈焰弹)); s.Add(O(MCHSkill.虹吸弹));
            s.Add(G(MCHSkill.烈焰弹)); s.Add(O(MCHSkill.弹射));
            s.Add(G(MCHSkill.烈焰弹)); s.Add(O(MCHSkill.虹吸弹));
            s.Add(G(MCHSkill.钻头)); s.Add(O(MCHSkill.虹吸弹)); s.Add(O(MCHSkill.弹射));
            return s;
        }
    }

    private static PAction G(uint id) => new(id, ActionType.Gcd, ActionTargetType.Target) { RequiresVerification = true };
    private static PAction O(uint id, ActionTargetType t = ActionTargetType.Target) => new(id, ActionType.OffGcd, t);
}
