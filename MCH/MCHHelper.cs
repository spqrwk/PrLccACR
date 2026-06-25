using Dalamud.Game.ClientState.Objects.Types;
using MCH.Data;
using PromeRotation.Extensions;

namespace MCH;

/// <summary>MCH 辅助方法 — 全部走 ApiHelper 壳</summary>
public static class MCHHelper
{
    // ============================================================
    // 基础
    // ============================================================
    public static uint GetBaseComboAction()
    {
        // 1-2-3 连击链：上个连击 存的是基础 ID，用基础 ID 对比，返回调整后的 ID
        var last = ApiHelper.上个连击;
        if ((last == MCHSkill.分裂弹 || last == MCHSkill.热分裂弹)
            && ApiHelper.技能已解锁(MCHSkill.独头弹))
            return ApiHelper.调整技能ID(MCHSkill.独头弹);
        if ((last == MCHSkill.独头弹 || last == MCHSkill.热独头弹)
            && ApiHelper.技能已解锁(MCHSkill.狙击弹))
            return ApiHelper.调整技能ID(MCHSkill.狙击弹);
        return ApiHelper.调整技能ID(MCHSkill.分裂弹);
    }

    public static uint GetAoeAction() => ApiHelper.调整技能ID(MCHSkill.散射);

    public static uint GetHotShotOrAirAnchor() => ApiHelper.调整技能ID(MCHSkill.热弹);

    public static uint GetHeatBlastAction() => ApiHelper.调整技能ID(MCHSkill.热冲击);

    public static uint GetRobotAction() => ApiHelper.调整技能ID(MCHSkill.车式浮空炮塔);

    // ============================================================
    // 整备目标选择（7等级段）
    // ============================================================
    public static bool CheckReassembleGcd(float timeleft, out uint spellId)
    {
        spellId = 0;
        var me = ApiHelper.玩家;
        if (me == null) return false;
        var level = me.Level;
        if (level >= 96) return CheckReassemble96_100(timeleft, out spellId);
        if (level >= 94) return CheckReassemble94_95(timeleft, out spellId);
        if (level >= 90) return CheckReassemble90_93(timeleft, out spellId);
        if (level >= 76) return CheckReassemble76_89(timeleft, out spellId);
        if (level >= 58) return CheckReassemble58_75(timeleft, out spellId);
        if (level >= 26) return CheckReassemble26_57(timeleft, out spellId);
        if (level >= 4)  return CheckReassemble4_25(timeleft, out spellId);
        return false;
    }

    private static bool CheckReassemble4_25(float t, out uint id)
    {
        id = 0;
        return IsReadyOrSoon(MCHSkill.热弹, t, MCHQT.空气锚, out id);
    }

    private static bool CheckReassemble26_57(float t, out uint id)
    {
        id = 0;
        if (IsReadyOrSoon(MCHSkill.热弹, t, MCHQT.空气锚, out id)) return true;
        var adj = ApiHelper.调整技能ID(MCHSkill.狙击弹);
        if (adj != 0 && ApiHelper.技能已解锁(adj) && ApiHelper.技能可用(adj)) { id = MCHSkill.狙击弹; return true; }
        return false;
    }

    private static bool CheckReassemble58_75(float t, out uint id)
    {
        id = 0;
        if (CheckDrill1Charge(t, MCHQT.钻头, out id)) { if (CheckDrilAoe()) id = MCHSkill.毒菌喷射器; return true; }
        var hs = GetHotShotOrAirAnchor();
        if (IsReadyOrSoon(hs, t, MCHQT.空气锚, out id)) return true;
        return false;
    }

    private static bool CheckReassemble76_89(float t, out uint id)
    {
        id = 0;
        if (IsReadyOrSoon(MCHSkill.空气锚, t, MCHQT.空气锚, out id)) return true;
        if (CheckDrill1Charge(t, MCHQT.钻头, out id)) { if (CheckDrilAoe()) id = MCHSkill.毒菌喷射器; return true; }
        return false;
    }

    private static bool CheckReassemble90_93(float t, out uint id)
    {
        id = 0;
        if (IsReadyOrSoon(MCHSkill.空气锚, t, MCHQT.空气锚, out id)) return true;
        if (CheckDrill1Charge(t, MCHQT.钻头, out id)) { if (CheckDrilAoe()) id = MCHSkill.毒菌喷射器; if (ApiHelper.连击剩余 < 3) return false; return true; }
        if (IsReadyOrSoon(MCHSkill.回转飞锯, t, MCHQT.回转飞锯, out id)) return true;
        return false;
    }

    private static bool CheckReassemble94_95(float t, out uint id)
    {
        id = 0;
        if (IsReadyOrSoon(MCHSkill.空气锚, t, MCHQT.空气锚, out id)) return true;
        if (CheckDrill2Charge(MCHQT.钻头, out id)) { if (CheckDrilAoe()) id = MCHSkill.毒菌喷射器; return true; }
        if (IsReadyOrSoon(MCHSkill.回转飞锯, t, MCHQT.回转飞锯, out id)) return true;
        if (CheckDrill1Charge(t, MCHQT.钻头, out id)) { if (CheckDrilAoe()) id = MCHSkill.毒菌喷射器; if (ApiHelper.连击剩余 < 3) return false; return true; }
        return false;
    }

    private static bool CheckReassemble96_100(float t, out uint id)
    {
        id = 0;
        if (IsReadyOrSoon(MCHSkill.空气锚, t, MCHQT.空气锚, out id)) return true;
        if (CheckDrill2Charge(MCHQT.钻头, out id)) { if (CheckDrilAoe()) id = MCHSkill.毒菌喷射器; return true; }
        if (IsReadyOrSoon(MCHSkill.回转飞锯, t, MCHQT.回转飞锯, out id)) return true;
        if (ApiHelper.技能已解锁(MCHSkill.掘地飞轮) && ApiHelper.技能可用(MCHSkill.掘地飞轮) && ApiHelper.获取QT(MCHQT.掘地飞轮)) { id = MCHSkill.掘地飞轮; return true; }
        if (CheckDrill1Charge(t, MCHQT.钻头, out id)) { if (CheckDrilAoe()) id = MCHSkill.毒菌喷射器; if (ApiHelper.连击剩余 < 3) return false; return true; }
        return false;
    }

    private static bool IsReadyOrSoon(uint skillId, float timeleftMs, string qtKey, out uint outId)
    {
        outId = 0;
        if (!ApiHelper.获取QT(qtKey)) return false;
        var adj = ApiHelper.调整技能ID(skillId);
        if (adj == 0 || !ApiHelper.技能已解锁(adj)) return false;
        if (ApiHelper.技能可用(adj)) { outId = adj; return true; }
        if (ApiHelper.技能冷却(adj) * 1000f <= timeleftMs) { outId = adj; return true; }
        return false;
    }

    private static bool CheckDrill1Charge(float timeleft, string qtKey, out uint id)
    {
        id = 0;
        var adj = ApiHelper.调整技能ID(MCHSkill.钻头);
        if (adj == 0 || !ApiHelper.技能已解锁(adj)) return false;
        var cdMs = ApiHelper.技能冷却(adj) * 1000f;
        if ((ApiHelper.技能可用(adj) || (cdMs - 20000f) <= timeleft) && ApiHelper.获取QT(qtKey)) { id = adj; return true; }
        return false;
    }

    private static bool CheckDrill2Charge(string qtKey, out uint id)
    {
        id = 0;
        var adj = ApiHelper.调整技能ID(MCHSkill.钻头);
        if (adj == 0 || !ApiHelper.技能已解锁(adj)) return false;
        if (ApiHelper.技能可用(adj) && ApiHelper.技能充能(adj) >= 2 && ApiHelper.获取QT(qtKey)) { id = adj; return true; }
        return false;
    }

    // ============================================================
    // 职业量谱
    // ============================================================
    public static int GetHeat() => ApiHelper.热量;
    public static int GetBattery() => ApiHelper.电池;
    public static bool IsOverheated() => ApiHelper.玩家有状态(MCHBuff.过热);
    public static long OverheatRemainMs() => (long)(ApiHelper.状态剩余(ApiHelper.玩家, MCHBuff.过热) * 1000);

    // ============================================================
    // 目标状态
    // ============================================================
    public static bool IsTargetImmune()
    {
        var t = ApiHelper.目标;
        if (t == null) return true;
        var invuln = new uint[] { 325, 529, 656, 671, 775, 776, 969, 981, 1570, 1697, 1829, 1302, 1836, 811, 810, 3255, 409, 941 };
        foreach (var id in invuln) if (ApiHelper.有状态(t, id)) return true;
        return false;
    }

    // ============================================================
    // SdjCD & AOE
    // ============================================================
    public static int SdjCD()
    {
        var me = ApiHelper.玩家;
        if (me == null) return 1;

        if (ApiHelper.获取QT(MCHQT.空气锚))
        {
            var adj = ApiHelper.调整技能ID(MCHSkill.空气锚);
            if (adj != 0 && ApiHelper.技能已解锁(adj))
            { if (ApiHelper.技能冷却(adj) > 0 && ApiHelper.技能冷却(adj) < 9) return -1; }
            else { var aa = ApiHelper.调整技能ID(MCHSkill.热弹); if (aa != 0 && ApiHelper.技能已解锁(aa)) { if (ApiHelper.技能冷却(aa) > 0 && ApiHelper.技能冷却(aa) < 9) return -1; } }
        }
        if (ApiHelper.获取QT(MCHQT.回转飞锯) && ApiHelper.技能已解锁(MCHSkill.回转飞锯))
        { if (ApiHelper.技能冷却(MCHSkill.回转飞锯) > 0 && ApiHelper.技能冷却(MCHSkill.回转飞锯) < 9) return -1; }
        if (ApiHelper.获取QT(MCHQT.钻头))
        {
            var adj = ApiHelper.调整技能ID(MCHSkill.钻头);
            if (adj != 0 && ApiHelper.技能已解锁(adj))
            { if (!ApiHelper.技能可用(adj) && ApiHelper.技能冷却(adj) < 9) return -1; }
        }
        return 1;
    }

    // ============================================================
    public static bool CheckDrilAoe()
    {
        if (!ApiHelper.获取QT(MCHQT.AOE)) return false;
        if (!ApiHelper.技能已解锁(MCHSkill.毒菌喷射器)) return false;
        if (!ApiHelper.技能可用(MCHSkill.毒菌喷射器)) return false;
        if (ApiHelper.玩家有状态(MCHBuff.整备预备)) return false;

        var best = FindBestAoeTarget(MCHSkill.毒菌喷射器, 120f, 3);
        if (best == null) return false;
        ApiHelper.切换目标(best);
        return true;
    }

    public static bool ShouldUseAoe(float coneRange, float coneAngle, int minTargets)
    {
        if (!ApiHelper.获取QT(MCHQT.AOE)) return false;
        var me = ApiHelper.玩家; var t = ApiHelper.目标;
        if (me == null || t == null) return false;
        return ApiHelper.扇形敌人(me, t, coneRange, coneAngle) >= minTargets;
    }

    public static IBattleChara? FindBestAoeTarget(uint spellId, float coneAngle, int minTargets)
    {
        if (!ApiHelper.获取QT(MCHQT.AOE)) return null;
        return ApiHelper.最佳AoE目标(spellId, minTargets, coneAngle);
    }
}
