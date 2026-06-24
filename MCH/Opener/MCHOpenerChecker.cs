using MCH.Data;

namespace MCH.Opener;

public static class MCHOpenerChecker
{
    public static bool CheckOpenerOutbreakSpells(uint skipSkillId)
    {
        var me = ApiHelper.玩家;
        if (me == null) return false;
        var lv = me.Level;

        if (!ApiHelper.技能已解锁(MCHSkill.野火) || !ApiHelper.技能可用(MCHSkill.野火)) return false;
        if (!ApiHelper.技能已解锁(MCHSkill.枪管加热) || !ApiHelper.技能可用(MCHSkill.枪管加热)) return false;
        if (skipSkillId != MCHSkill.回转飞锯)
        {
            if (lv >= 90 && (!ApiHelper.技能已解锁(MCHSkill.回转飞锯) || !ApiHelper.技能可用(MCHSkill.回转飞锯))) return false;
        }

        if (skipSkillId != MCHSkill.空气锚)
        {
            var aa = lv >= 76 ? MCHSkill.空气锚 : MCHSkill.热弹;
            if (!ApiHelper.技能已解锁(aa) || !ApiHelper.技能可用(aa)) return false;
        }
        if (skipSkillId != MCHSkill.钻头)
        {
            if (!ApiHelper.技能已解锁(MCHSkill.钻头) || !ApiHelper.技能可用(MCHSkill.钻头) || ApiHelper.技能充能(MCHSkill.钻头) < 2) return false;
        }
        return true;
    }
}
