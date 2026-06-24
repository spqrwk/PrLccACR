namespace MCH.Data;

/// <summary>
/// MCH 战斗缓存数据（单场战斗内有效）
/// </summary>
public class MCHBattleData
{
    public static MCHBattleData Instance { get; set; } = new();

    /// <summary>热键使用高优先级</summary>
    public bool HotkeyUseHighPrioritySlot = false;

    public void Reset()
    {
        HotkeyUseHighPrioritySlot = false;
    }
}
