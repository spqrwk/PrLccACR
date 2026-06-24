using MCH.Data;
using PromeRotation.Rotation;

namespace MCH.Opener;

public static class MCHOpenerManager
{
    public static IOpener? GetOpener()
    {
        var me = ApiHelper.玩家;
        if (!ApiHelper.获取QT(MCHQT.启用起手)) {return null;}
        if (me == null) return null;
        var lv = me.Level;
        if (!MCHSettings.Instance.IsHighEnd) return null;

        if (lv >= 100) return MCHSettings.Instance.Opener switch
        {
            0 => new MCHAirAnchorOpener100(), 1 => new MCHDrillOpener100(), 2 => new MCHKfkOpener100(),
            _ => new MCHAirAnchorOpener100(),
        };
        if (lv >= 90) return MCHSettings.Instance.Opener switch
        { 3 => new MCHAirAnchorOpener90(), _ => new MCHAirAnchorOpener90(), };
        return null;
    }
}
