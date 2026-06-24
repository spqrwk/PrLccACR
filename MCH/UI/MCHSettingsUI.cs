using System.Numerics;
using MCH.Data;
using MCH.Helper;
using Dalamud.Bindings.ImGui;
using PromeRotation.Data;

namespace MCH.UI;

/// <summary>设置面板</summary>
public static class MCHSettingsUI
{
    public static void Draw()
    {
        if (!ImGui.BeginTabBar("Settings##MCH")) return;
        if (ImGui.BeginTabItem("通用设置"))   { DrawGeneral(); ImGui.EndTabItem(); }
        if (ImGui.BeginTabItem("QT管理"))     { DrawQtManage(); ImGui.EndTabItem(); }
        if (ImGui.BeginTabItem("默认值管理")) { DrawDefaultsManage(); ImGui.EndTabItem(); }
        if (ImGui.BeginTabItem("更新日志"))   { DrawChangelog(); ImGui.EndTabItem(); }
        if (ImGui.BeginTabItem("开发用"))     { DrawDev(); ImGui.EndTabItem(); }
        ImGui.EndTabBar();
        MCHSettings.Instance.Save();

        var cmd = MCHSettings.Instance.CommandWindowOpen;
        MCHMacroManager.DrawCommandWindow(ref cmd);
        MCHSettings.Instance.CommandWindowOpen = cmd;
    }

    // ============================================================
    private static void DrawGeneral()
    {
        var h = MCHSettings.Instance.IsHighEnd;
        Hdr("⚙ 基础设置");
        ImGui.Text($"当前模式：{(h ? "高难" : "日随")}");
        ImGui.SameLine();
        if (ImGui.SmallButton(h ? "切换日随" : "切换高难"))
            MCHSettings.Instance.SwitchMode(!h);

        ImGui.Separator();
        var cd = MCHSettings.Instance.Cdtolerance;
        if (ImGui.SliderInt("三大件CD容忍度(ms)", ref cd, 200, 1600)) MCHSettings.Instance.Cdtolerance = cd;
        var hs = MCHSettings.Instance.HoldTime / 1000;
        if (ImGui.SliderInt("停手时长(秒)", ref hs, 1, 10)) MCHSettings.Instance.HoldTime = hs * 1000;
        var mb = MCHSettings.Instance.MinBattery;
        if (ImGui.SliderInt("机器人最低电量", ref mb, 50, 100)) { if (mb % 10 != 0) mb = mb / 10 * 10; MCHSettings.Instance.MinBattery = mb; }
        var mh = MCHSettings.Instance.MinHeat;
        if (ImGui.SliderInt("最低热量", ref mh, 50, 100)) { if (mh % 5 != 0) mh = mh / 5 * 5; MCHSettings.Instance.MinHeat = mh; }
        var gl = MCHSettings.Instance.GrabItLimit;
        if (ImGui.SliderInt("抢开阈值(ms)", ref gl, 0, 1000)) MCHSettings.Instance.GrabItLimit = gl;

        ImGui.Separator(); Hdr("🔧 通用子设置");
        var asw = MCHSettings.Instance.AutoSecondWind;
        if (ImGui.Checkbox("自动内丹", ref asw)) MCHSettings.Instance.AutoSecondWind = asw;
        if (asw) { var th = MCHSettings.Instance.SecondWindThreshold; if (ImGui.SliderFloat("  触发血量%", ref th, 1, 100, "%.0f%%")) MCHSettings.Instance.SecondWindThreshold = th; }
        var up = MCHSettings.Instance.UsePeloton;
        if (ImGui.Checkbox("脱战自动速行", ref up)) MCHSettings.Instance.UsePeloton = up;
        var ar = MCHSettings.Instance.AutoResetBattleData;
        if (ImGui.Checkbox("自动重置战斗数据", ref ar)) MCHSettings.Instance.AutoResetBattleData = ar;
        var hs2 = MCHSettings.Instance.HandleStopMechs;
        if (ImGui.Checkbox("自动停手机制", ref hs2)) MCHSettings.Instance.HandleStopMechs = hs2;

        ImGui.Separator();
        if (h) DrawHighEnd(); else DrawNormal();
    }

    private static void DrawHighEnd()
    {
        Hdr("🏆 高难模式", new(1f, 0.8f, 0.2f, 1f));
        var names = ApiHelper.玩家等级 >= 100
            ? new[] { "空气锚起手100", "钻头起手100", "绝妖星特化100" }
            : new[] { "空气锚起手90" };
        var idx = MCHSettings.Instance.Opener;
        if (idx >= names.Length) idx = 0;
        if (ImGui.BeginCombo("起手选择", names[idx])) { for (int i = 0; i < names.Length; i++) if (ImGui.Selectable(names[i], idx == i)) MCHSettings.Instance.Opener = i; ImGui.EndCombo(); }
        var wb = MCHSettings.Instance.WFOnlyBOSS;
        if (ImGui.Checkbox("野火仅Boss", ref wb)) MCHSettings.Instance.WFOnlyBOSS = wb;
        var po = MCHSettings.Instance.UsePotionInOpener;
        if (ImGui.Checkbox("起手爆发药", ref po)) MCHSettings.Instance.UsePotionInOpener = po;
        var ap = MCHSettings.Instance.autoPotion;
        if (ImGui.SliderInt("自动爆发药(野火CD<N秒)", ref ap, 0, 20)) MCHSettings.Instance.autoPotion = ap;
        var bw = MCHSettings.Instance.BurstWarm;
        if (ImGui.Checkbox("120s爆发TTS", ref bw)) MCHSettings.Instance.BurstWarm = bw;
    }

    private static void DrawNormal()
    {
        Hdr("🎯 日随模式", new(0.2f, 1f, 0.4f, 1f));
        var pn = MCHSettings.Instance.PullingNoBurst;
        ImGui.Text(pn ? "拉怪留爆发" : "拉怪用爆发"); ImGui.SameLine();
        if (ImGui.SmallButton(pn ? "改用" : "改留")) MCHSettings.Instance.PullingNoBurst = !pn;
        var nb = MCHSettings.Instance.NoBurst;
        ImGui.Text(nb ? "小怪死前留爆发" : "小怪死前用爆发"); ImGui.SameLine();
        if (ImGui.SmallButton(nb ? "改用" : "改留")) MCHSettings.Instance.NoBurst = !nb;
        var mhp = MCHSettings.Instance.MinMobHpPercent * 100f;
        if (ImGui.SliderFloat("小怪血量阈值(%)", ref mhp, 0, 100, "%.0f%%")) MCHSettings.Instance.MinMobHpPercent = mhp / 100f;
        var ttk = MCHSettings.Instance.minTTK;
        if (ImGui.SliderInt("小怪死亡时间阈值(s)", ref ttk, 0, 30)) MCHSettings.Instance.minTTK = ttk;
        ImGui.Separator();
        ImGui.TextWrapped("5目标自动弩才赚。3目标用霰弹枪。");
    }

    private static void DrawQtManage()
    {
        var isHighEnd = MCHSettings.Instance.IsHighEnd;
        var modeColor = isHighEnd
            ? new Vector4(1f, 0.8f, 0.2f, 1f)
            : new Vector4(0.2f, 1f, 0.4f, 1f);

        Hdr("🎮 QT 开关管理");
        ImGui.TextColored(modeColor, $"当前模式：{(isHighEnd ? "高难" : "日随")}");
        ImGui.SameLine();
        if (ImGui.SmallButton(isHighEnd ? "切换日随" : "切换高难"))
            MCHSettings.Instance.SwitchMode(!isHighEnd);

        ImGui.Separator();

        // ── 通用 QT ──
        if (ImGui.CollapsingHeader("🔵 通用 QT", ImGuiTreeNodeFlags.DefaultOpen))
            DrawQtGroup(QTMode.Common, isHighEnd);

        // ── 高难专属 QT ──
        var highColor = new Vector4(1f, 0.8f, 0.2f, 1f);
        if (ImGui.CollapsingHeader("🟠 高难专属 QT"))
        {
            if (!isHighEnd)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "  （切换至高难模式后可编辑）");
            }
            DrawQtGroup(QTMode.HighEndOnly, isHighEnd);
        }

        // ── 日随专属 QT ──
        var dailyColor = new Vector4(0.2f, 1f, 0.4f, 1f);
        if (ImGui.CollapsingHeader("🟢 日随专属 QT"))
        {
            if (isHighEnd)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "  （切换至日随模式后可编辑）");
            }
            DrawQtGroup(QTMode.DailyOnly, isHighEnd);
        }

        ImGui.Separator();
        Hdr("⌨ 聊天命令");
        ImGui.TextWrapped("使用 /Lcc_Mch <QT名称> 在聊天框切换QT。结合游戏内宏可方便手柄用户。");
        var co = MCHSettings.Instance.CommandWindowOpen;
        if (ImGui.Button("📋 打开命令列表")) co = true;
        MCHSettings.Instance.CommandWindowOpen = co;
    }

    /// <summary>绘制指定模式分类的 QT 开关列表</summary>
    private static void DrawQtGroup(QTMode category, bool isHighEnd)
    {
        foreach (var (key, _) in MCHQT.All)
        {
            if (MCHQT.IsMetaKey(key)) continue;
            if (MCHQT.GetModeCategory(key) != category) continue;

            var visible = MCHQT.IsVisibleInMode(key, isHighEnd);
            var v = ApiHelper.获取QT(key);

            if (!visible)
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.4f, 0.4f, 0.6f));

            if (ImGui.Checkbox($"{key}##qtmgr_{key}", ref v))
            {
                if (visible)
                    ApiHelper.设置QT(key, v);
            }

            if (!visible)
                ImGui.PopStyleColor();
        }
    }

    /// <summary>绘制"默认值管理"页签</summary>
    private static void DrawDefaultsManage()
    {
        Hdr("📦 默认值管理");
        ImGui.TextWrapped("按模式分别管理 QT 开关的默认值。切换模式时自动保存/恢复。");

        ImGui.Separator();
        ImGui.Columns(2, "DefaultsCols", true);

        // ── 左列：高难默认值 ──
        Hdr("🟠 高难默认值", new Vector4(1f, 0.8f, 0.2f, 1f));
        foreach (var (key, _) in MCHQT.All)
        {
            if (MCHQT.IsMetaKey(key)) continue;
            var cat = MCHQT.GetModeCategory(key);
            if (cat == QTMode.DailyOnly) continue; // 日随专属不在高难列表

            var dict = MCHSettings.Instance.QtHighEndDefaults;
            var v = dict.TryGetValue(key, out var dv) ? dv : MCHQT.Default(key);
            if (ImGui.Checkbox($"{key}##hdef_{key}", ref v))
                dict[key] = v;
        }
        if (ImGui.Button("💾 保存当前QT##hdef_save"))
        {
            MCHSettings.Instance.SaveQtSnapshot(true);
            MCHSettings.Instance.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("🔄 重置为内置##hdef_reset"))
            MCHSettings.Instance.ResetModeDefaults(true);

        ImGui.NextColumn();

        // ── 右列：日随默认值 ──
        Hdr("🟢 日随默认值", new Vector4(0.2f, 1f, 0.4f, 1f));
        foreach (var (key, _) in MCHQT.All)
        {
            if (MCHQT.IsMetaKey(key)) continue;
            var cat = MCHQT.GetModeCategory(key);
            if (cat == QTMode.HighEndOnly) continue; // 高难专属不在日随列表

            var dict = MCHSettings.Instance.QtDailyDefaults;
            var v = dict.TryGetValue(key, out var dv) ? dv : MCHQT.Default(key);
            if (ImGui.Checkbox($"{key}##ddef_{key}", ref v))
                dict[key] = v;
        }
        if (ImGui.Button("💾 保存当前QT##ddef_save"))
        {
            MCHSettings.Instance.SaveQtSnapshot(false);
            MCHSettings.Instance.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("🔄 重置为内置##ddef_reset"))
            MCHSettings.Instance.ResetModeDefaults(false);

        ImGui.Columns(1);
        ImGui.Separator();

        // ── 全局操作 ──
        if (ImGui.Button("📋 复制当前模式默认值到另一模式"))
        {
            MCHSettings.Instance.CopyDefaultsToOtherMode();
            ApiHelper.提示("已复制默认值到另一模式", 2);
        }
        ImGui.SameLine();
        if (ImGui.Button("🔄 全部重置为内置默认"))
        {
            MCHSettings.Instance.ResetModeDefaults(true);
            MCHSettings.Instance.ResetModeDefaults(false);
            MCHSettings.Instance.Save();
            ApiHelper.提示("所有默认值已重置为内置值", 2);
        }
    }

    private static void DrawChangelog()
    {
        Hdr("📋 更新日志");
        Log("2026.06", "从LccMchAcr (AEAssist) 全量迁移至 PromeRotation。GCD/oGCD/起手/QT/热键/宏全部就绪。");
        Log("2025.12", "源: 爆发药适配、120s提示、轴控");
        Log("2025.10", "源: V1 发布");
    }

    private static void DrawDev()
    {
        // 诊断：高难模式 QT 状态
        var qt = PromeSettings.Instance.QuickToggles;
        var modeQt = qt.TryGetValue(MCHQT.高难模式, out var m) ? m : (bool?)null;
        ImGui.Text($"高难模式QT={modeQt}  IsHighEnd={MCHSettings.Instance.IsHighEnd}");
        ImGui.Separator();

        ImGui.Text($"WFOnlyBOSS={MCHSettings.Instance.WFOnlyBOSS}  IsHighEnd={MCHSettings.Instance.IsHighEnd}");
        ImGui.Text($"配置路径: {MCHSettings.FilePath}");
        ImGui.Separator();
        var p = ApiHelper.玩家;
        if (p == null) { ImGui.Text("未加载"); return; }
        ImGui.Text($"等级:{p.Level} 过热:{ApiHelper.玩家有状态(MCHBuff.过热)} 超荷预备:{ApiHelper.玩家有状态(MCHBuff.超荷预备)}");
        ImGui.Text($"整备:{ApiHelper.玩家有状态(MCHBuff.整备预备)} 全金属:{ApiHelper.玩家有状态(MCHBuff.全金属爆发预备)} 掘地:{ApiHelper.玩家有状态(MCHBuff.掘地飞轮预备)}");
        ImGui.Separator();
        ImGui.Text($"钻头:{ApiHelper.技能冷却(MCHSkill.钻头):F1}s 空气锚:{ApiHelper.技能冷却(MCHSkill.空气锚):F1}s 飞锯:{ApiHelper.技能冷却(MCHSkill.回转飞锯):F1}s");
        ImGui.Text($"野火:{ApiHelper.技能冷却(MCHSkill.野火):F1}s 超荷:{ApiHelper.技能冷却(MCHSkill.超荷):F1}s GCD:{ApiHelper.GCD剩余:F2}s");
        ImGui.Text($"热量:{MCHHelper.GetHeat()} 电池:{MCHHelper.GetBattery()} 模式:{(MCHSettings.Instance.IsHighEnd ? "高难" : "日随")} 起手:{MCHSettings.Instance.Opener}");
    }

    private static void Hdr(string t, Vector4? c = null) => ImGui.TextColored(c ?? new(0.2f, 0.8f, 1f, 1f), t);
    private static void Log(string d, string c) { if (ImGui.CollapsingHeader(d, ImGuiTreeNodeFlags.DefaultOpen)) ImGui.TextWrapped(c); }
}
