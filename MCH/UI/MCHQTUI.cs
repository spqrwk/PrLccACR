using System.Numerics;
using System.Timers;
using MCH.Data;
using Dalamud.Bindings.ImGui;
using PromeRotation.Data;
using Timer = System.Timers.Timer;

namespace MCH.UI;

/// <summary>QT 面板 + 联动回调</summary>
public static class MCHQTUI
{
    private static Timer? _holdTimer;
    // ============================================================
    // === 主入口 ===
    // ============================================================
    public static void Draw()
    {
        // 渲染前先同步：上帧 QT 变更 → 联动
        PollCascade();
        if (!ImGui.BeginTabBar("MCH_QT")) return;

        if (ImGui.BeginTabItem("基础"))
        {
            ModeToggle();
            ImGui.Separator();

            QtToggle(MCHQT.启用起手, "启用起手");
            QtToggle(MCHQT.停手, "停手", OnHoldChanged);
            QtToggle(MCHQT.自动减伤, "自动减伤");
            QtToggle(MCHQT.AOE, "AOE 模式");
            QtToggle(MCHQT.优先打123, "优先打 1-2-3");
            QtToggle(MCHQT.攒资源, "攒资源");   // 联动由 ApiHelper.设置QT 自动处理
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("技能"))
        {
            QtToggle(MCHQT.整备, "整备", requiresSkillId: MCHSkill.整备);
            QtToggle(MCHQT.野火技能, "野火", requiresSkillId: MCHSkill.野火);
            QtToggle(MCHQT.野火优先, "野火优先");
            QtToggle(MCHQT.超荷, "超荷", requiresSkillId: MCHSkill.超荷);
            QtToggle(MCHQT.枪管加热, "枪管加热", requiresSkillId: MCHSkill.枪管加热);
            QtToggle(MCHQT.机器人, "机器人", requiresSkillId: MCHSkill.车式浮空炮塔);
            QtToggle(MCHQT.全金属爆发, "全金属爆发", requiresSkillId: MCHSkill.全金属爆发);
            QtToggle(MCHQT.回转飞锯, "回转飞锯", requiresSkillId: MCHSkill.回转飞锯);
            QtToggle(MCHQT.掘地飞轮, "掘地飞轮", requiresSkillId: MCHSkill.掘地飞轮);
            QtToggle(MCHQT.空气锚, "空气锚", requiresSkillId: MCHSkill.热弹);
            QtToggle(MCHQT.钻头, "钻头", requiresSkillId: MCHSkill.钻头);
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("资源"))
        {
            QtToggle(MCHQT.爆发药, "爆发药");
            QtToggle(MCHQT.爆发, "加热野火一套"); // 联动由 ApiHelper.设置QT 自动处理
            QtToggle(MCHQT.保留2层双将, "保留2层双将");
            QtToggle(MCHQT.保留2层将死, "保留2层将死");
            ImGui.EndTabItem();
        }
        ImGui.EndTabBar();

        // 渲染后：联动 + 模式同步
        PollCascade();
        SyncModeFromQt();
    }

    public static void PollCascade() => ApiHelper.轮询联动();

    /// <summary>直接对比 QuickToggles["高难模式"] 和 IsHighEnd，不一致就同步</summary>
    private static void SyncModeFromQt()
    {
        var qt = PromeSettings.Instance.QuickToggles;
        if (!qt.TryGetValue(MCHQT.高难模式, out var modeQt)) return;
        if (modeQt == MCHSettings.Instance.IsHighEnd) return;
        MCHSettings.Instance.IsHighEnd = modeQt;
        MCHSettings.Instance.SyncQtToMode();
        RebuildQtVisibility();
        ApiHelper.提示($"→ {(modeQt ? "高难" : "日随")}", 2);
    }

    /// <summary>模式切换后重建 QT 注册：隐藏当前模式不可见的 QT</summary>
    public static void RebuildQtVisibility() => ApiHelper.重建QT可见性();

    // ============================================================
    // === 绘制辅助 ===
    // ============================================================
    /// <summary>模式感知的 QT 开关：联动由 ApiHelper.设置QT 自动处理</summary>
    public static void QtToggle(string key, string label, Action<bool>? onChanged = null, uint requiresSkillId = 0)
    {
        if (requiresSkillId != 0 && !ApiHelper.技能已解锁(requiresSkillId)) return;
        if (!MCHQT.IsVisibleInMode(key, MCHSettings.Instance.IsHighEnd)) return;

        var v = ApiHelper.获取QT(key);
        var cat = MCHQT.GetModeCategory(key);

        if (cat == QTMode.HighEndOnly)
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.8f, 0.2f, 1f));
        else if (cat == QTMode.DailyOnly)
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.2f, 1f, 0.4f, 1f));

        var display = v ? $"☑ {label}" : $"☐ {label}";
        if (ImGui.Selectable($"{display}##{key}"))
        {
            v = !v;
            var qt = PromeSettings.Instance.QuickToggles;
            qt[key] = v;
            if (MCHQT.CascadeRules.TryGetValue(key, out var links))
                foreach ((string lk, bool inv) in links)
                    qt[lk] = inv ? !v : v;
            onChanged?.Invoke(v);
        }

        if (cat != QTMode.Common)
            ImGui.PopStyleColor();
    }

    [System.Obsolete("请使用 QtToggle 替代 Toggle", false)]
    public static void Toggle(string key, string label, Action<bool>? onChanged = null, uint requiresSkillId = 0)
    {
        if (requiresSkillId != 0 && !ApiHelper.技能已解锁(requiresSkillId)) return;
        var v = ApiHelper.获取QT(key);
        var display = v ? $"☑ {label}" : $"☐ {label}";
        if (ImGui.Selectable($"{display}##{key}"))
        {
            v = !v;
            ApiHelper.设置QT(key, v);
        }
    }

    [System.Obsolete("请使用 QtToggle 替代 ToggleIfMode — QtToggle 自动根据 ModeCategory 判断可见性", false)]
    public static void ToggleIfMode(string key, string label, bool hideInHighEnd = false, bool hideInNormal = false)
    {
        if (hideInHighEnd && MCHSettings.Instance.IsHighEnd) return;
        if (hideInNormal && !MCHSettings.Instance.IsHighEnd) return;
        Toggle(key, label);
    }

    // ============================================================
    // === 模式切换 QT ===
    // ============================================================
    /// <summary>绘制模式切换 QT（着色）—— 直接绑定 IsHighEnd，不走 QT 存储</summary>
    private static void ModeToggle()
    {
        var v = MCHSettings.Instance.IsHighEnd;
        ImGui.PushStyleColor(ImGuiCol.Text, v
            ? new Vector4(1f, 0.8f, 0.2f, 1f)
            : new Vector4(0.2f, 1f, 0.4f, 1f));
        var display = v ? "☑ 高难模式" : "☐ 日随模式";
        if (ImGui.Selectable($"{display}##ModeSwitch"))
            OnModeSwitchChanged(!v);
        ImGui.PopStyleColor();
    }

    private static void OnModeSwitchChanged(bool isHighEnd)
    {
        ApiHelper.提示($"切换至{(isHighEnd ? "高难" : "日随")}模式", 2);
        MCHSettings.Instance.SwitchMode(isHighEnd);
    }

    // ============================================================
    // === QT 联动回调 ===
    // ============================================================
    // OnBurst120Changed / OnStoreChanged 联动逻辑已移入 ApiHelper.设置QT 自动处理
    // 保留方法签名供外部兼容（不再需要手动调用，CascadeRules 表驱动）

    public static void OnHoldChanged(bool isSet)
    {
        if (!isSet) return;
        _holdTimer?.Stop(); _holdTimer?.Dispose();
        _holdTimer = new Timer(MCHSettings.Instance.HoldTime) { AutoReset = false };
        _holdTimer.Elapsed += (_, _) =>
        {
            ApiHelper.设置QT(MCHQT.停手, false);
            _holdTimer?.Dispose(); _holdTimer = null;
        };
        _holdTimer.Start();
    }

    public static void OnModeChanged(bool isHighEnd)
    {
        MCHSettings.Instance.SyncQtToMode();
    }
}
