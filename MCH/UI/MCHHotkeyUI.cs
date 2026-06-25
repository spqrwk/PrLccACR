using MCH.Data;
using PromeRotation.Data;
using PromeRotation.UI.HotKey;

namespace MCH.UI;

/// <summary>热键面板</summary>
public static class MCHHotkeyUI
{
    public static void Setup()
    {
        var p = new HotkeyPanel(columns: 3);
        p.AddHotkey("爆发药", new PAction(MCHSkill.爆发药, ActionType.Item, ActionTargetType.Self));
        p.AddHotkey("冲刺",   new PAction(3, ActionType.OffGcd, ActionTargetType.Self));
        p.AddHotkey("防击退", new PAction(7548, ActionType.OffGcd, ActionTargetType.Self));
        p.AddHotkey("内丹",   new PAction(MCHSkill.内丹, ActionType.OffGcd, ActionTargetType.Self));
        p.AddHotkey("超荷",   new PAction(MCHSkill.超荷, ActionType.OffGcd, ActionTargetType.Self));
        p.AddHotkey("策动",   new PAction(MCHSkill.策动, ActionType.OffGcd, ActionTargetType.Self));
        p.AddHotkey("武装解除", new PAction(MCHSkill.武装解除, ActionType.OffGcd, ActionTargetType.Target));
        p.AddHotkey("火焰喷射器", new PAction(MCHSkill.火焰喷射器, ActionType.Gcd, ActionTargetType.Target));
        p.AddHotkey("人偶结算", new PAction(MCHSkill.超档后式人偶, ActionType.OffGcd, ActionTargetType.Target));
        p.AddHotkey("野火结算", new PAction(MCHSkill.起爆器, ActionType.OffGcd, ActionTargetType.Target));
        p.AddHotkey("极限技",   new PAction(ApiHelper.LB技能ID, ActionType.LimitBreak, ActionTargetType.Target));
        p.AddHotkey("速行",   new PAction(MCHSkill.速行, ActionType.OffGcd, ActionTargetType.Self));
        HotkeyManager.Instance.AddHotkeyPanel(p);
    }
}
