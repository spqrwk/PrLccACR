using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using MCH.Data;

namespace MCH
{
    public static class ApiHelper
    {
        #region 玩家相关

        /// <summary>获取当前玩家对象</summary>
        public static IBattleChara? 玩家 => Core.Me;

        /// <summary>玩家当前血量百分比 (0-100)</summary>
        public static float 玩家的血量 => 玩家 != null ? PartyHelper.GetHpPercent(玩家) : 0;

        /// <summary>玩家当前蓝量百分比 (0-100)</summary>
        public static float 玩家的蓝量 => 玩家 != null ? PartyHelper.GetMpPercent(玩家) : 0;

        /// <summary>玩家当前所在位置</summary>
        public static Vector3 玩家的位置 => 玩家?.Position ?? Vector3.Zero;

        /// <summary>玩家是否在战斗中</summary>
        public static bool 战斗中 => Svc.Condition[ConditionFlag.InCombat];

        /// <summary>玩家是否存活</summary>
        public static bool 活着 => 玩家 != null && !玩家.IsDead;

        /// <summary>玩家当前等级</summary>
        public static uint 玩家的等级 => 玩家?.Level ?? 0;

        /// <summary>玩家当前职业</summary>
        public static uint 玩家的职业 => 玩家?.ClassJob.RowId ?? 0;

        #endregion

        #region 目标相关

        /// <summary>获取当前选中的目标</summary>
        public static IBattleChara? 目标 => Core.Target as IBattleChara;

        /// <summary>目标的血量百分比</summary>
        public static float 目标血量 => 目标 != null ? PartyHelper.GetHpPercent(目标) : 0;

        /// <summary>目标是否存活</summary>
        public static bool 目标活着 => 目标 != null && !目标.IsDead;

        /// <summary>目标是否可攻击</summary>
        public static bool 可攻击
        {
            get
            {
                if (目标 == null || 目标.IsDead) return false;
                return (目标.ObjectKind == ObjectKind.BattleNpc || 目标.ObjectKind == ObjectKind.EventNpc)
                       && 目标.IsTargetable;
            }
        }

        /// <summary>目标距离（米）</summary>
        public static float 目标距离
        {
            get
            {
                if (目标 == null || 玩家 == null) return 999;
                return Vector3.Distance(玩家的位置, 目标.Position) - (玩家.HitboxRadius + 目标.HitboxRadius);
            }
        }

        /// <summary>获取目标的方向（身位）</summary>
        public static Positional 目标身位 => TargetHelper.GetTargetPositional();

        /// <summary>目标是否有身位要求</summary>
        public static bool 目标有身位
        {
            get
            {
                if (目标 == null) return false;
                return TargetHelper.HasPositionalRequirement(目标);
            }
        }

        /// <summary>设置目标</summary>
        public static void 选中(IGameObject? 目标)
        {
            if (目标 != null) Svc.Targets.Target = 目标;
        }

        /// <summary>清除目标</summary>
        public static void 取消目标() => Svc.Targets.Target = null;

        #endregion

        #region 技能系统

        /// <summary>技能是否可用</summary>
        public static bool 技能可用(uint 技能ID) => ActionHelper.IsReady(技能ID);

        /// <summary>技能是否已解锁</summary>
        public static bool 技能已解锁(uint 技能ID) => ActionHelper.IsUnlocked(技能ID);

        /// <summary>技能冷却剩余（秒）</summary>
        public static float 技能冷却(uint 技能ID) => ActionHelper.GetActionCooldown(技能ID);

        /// <summary>技能是否冷却中</summary>
        public static bool 技能冷却中(uint 技能ID) => 技能冷却(技能ID) > 0;

        /// <summary>技能充能层数</summary>
        public static float 技能充能(uint 技能ID) => ActionHelper.GetActionCharges(技能ID);

        /// <summary>技能最大充能层数</summary>
        public static int 技能最大充能(uint 技能ID) => ActionHelper.GetMaxCharges(技能ID);

        /// <summary>充能是否已满</summary>
        public static bool 充能已满(uint 技能ID) => 技能充能(技能ID) >= 技能最大充能(技能ID);

        /// <summary>获取调整后的技能ID（连击变化）</summary>
        public static uint 调整技能ID(uint 技能ID) => ActionHelper.GetAdjustedActionId(技能ID);

        /// <summary>获取上一个连击ID</summary>
        public static uint 上个连击 => ActionHelper.GetLastComboID();

        /// <summary>技能是否高亮（可发动）</summary>
        public static bool 技能高亮(uint 技能ID) => ActionHelper.IsActionHighlighted(技能ID);

        /// <summary>技能类型（GCD/能力技）</summary>
        public static ActionType 技能类型(uint 技能ID) => ActionHelper.ResolveActionType(技能ID);

        /// <summary>创建技能 - GCD技能 (默认对目标)</summary>
        public static PAction GCD技能(uint 技能ID) => new(技能ID, ActionType.Gcd, ActionTargetType.Target);

        /// <summary>创建技能 - 指定类型</summary>
        public static PAction 技能(uint 技能ID, ActionType 类型) => new(技能ID, 类型, ActionTargetType.Target);

        /// <summary>创建技能 - 对指定目标</summary>
        public static PAction 对目标技能(uint 技能ID, IBattleChara? 目标)
        {
            if (目标 == null) return new PAction(技能ID, ActionType.Gcd, ActionTargetType.Target);
            if (目标 == 玩家) return new PAction(技能ID, ActionType.Gcd, ActionTargetType.Self);
            var party = PartyHelper.GetParty();
            for (int i = 0; i < party.Count; i++)
                if (party[i] == 目标) return new PAction(技能ID, ActionType.Gcd, PartyHelper.GetTargetTypeByIndex(i));
            return new PAction(技能ID, ActionType.Gcd, ActionTargetType.Target);
        }

        /// <summary>对指定目标使用技能 - 能力技</summary>
        public static PAction 对目标能力(uint 技能ID, IBattleChara? 目标)
        {
            if (目标 == null) return new PAction(技能ID, ActionType.OffGcd, ActionTargetType.Target);
            if (目标 == 玩家) return new PAction(技能ID, ActionType.OffGcd, ActionTargetType.Self);
            var party = PartyHelper.GetParty();
            for (int i = 0; i < party.Count; i++)
                if (party[i] == 目标) return new PAction(技能ID, ActionType.OffGcd, PartyHelper.GetTargetTypeByIndex(i));
            return new PAction(技能ID, ActionType.OffGcd, ActionTargetType.Target);
        }

        /// <summary>对自己使用技能</summary>
        public static PAction 对自己技能(uint 技能ID, ActionType 类型 = ActionType.OffGcd)
            => new(技能ID, 类型, ActionTargetType.Self);

        /// <summary>地面目标技能</summary>
        public static void 放地面技能(uint 技能ID, Vector3 位置)
            => ActionHelper.UseActionLocation(技能ID, 0, 位置);

        /// <summary>地面目标技能 - 在指定目标脚下施放</summary>
        public static void 放地面技能(uint 技能ID, IBattleChara 目标)
        {
            if (目标 == null) return;
            ActionHelper.UseActionLocation(技能ID, 目标.GameObjectId, 目标.Position);
        }

        /// <summary>记录技能使用（防止重复）</summary>
        public static void 记录技能(uint 技能ID) => ActionHelper.RecordAction(技能ID);

        /// <summary>技能是否最近使用过</summary>
        public static bool 最近用过(uint 技能ID, int 毫秒) => ActionHelper.RecentlyUsed(技能ID, 毫秒);

        /// <summary>技能任务是否已解锁</summary>
        public static bool 技能任务已解锁(uint 技能ID) => ActionHelper.IsActionQuestUnlocked(技能ID);

        /// <summary>技能等级是否足够</summary>
        public static bool 技能等级足够(uint 技能ID) => ActionHelper.IsActionLevelEnough(技能ID);

        /// <summary>技能等级和任务均已满足</summary>
        public static bool 技能等级和任务已解锁(uint 技能ID) => ActionHelper.IsActionAvailableByLevelAndQuest(技能ID);

        /// <summary>综合判断能否对目标释放技能（等级+任务+冷却+目标可选）</summary>
        public static bool 能否释放(uint 技能ID, IBattleChara? 指定目标 = null) => ActionHelper.CanCast(技能ID, 指定目标);

        /// <summary>技能复唱总时间（秒）</summary>
        public static float 技能复唱总时间(uint 技能ID) => ActionHelper.GetActionRecastTime(技能ID);

        /// <summary>技能复唱已过时间（秒）</summary>
        public static float 技能复唱已过(uint 技能ID) => ActionHelper.GetActionRecastTimeElapsed(技能ID);

        /// <summary>手动设置动画锁 0.2s（用于强制插入延迟）</summary>
        public static void 设置动画锁() => ActionHelper.SetAnimetionLock();

        /// <summary>获取光标指向的世界坐标（用于地面技能）</summary>
        public static Vector3 光标位置 => ActionHelper.GetCursorPosition();

        /// <summary>为 PAction 设置最小 GCD 已过时间（编织延迟，仅对 oGCD/Item 生效）</summary>
        public static PAction 编织延迟(PAction action, int 最小GCD已过毫秒) => action.WithWeaveDelay(最小GCD已过毫秒);

        #endregion

        #region GCD 系统

        /// <summary>GCD总时长（秒）</summary>
        public static float GCD总时长 => ActionHelper.GetGcdTotal();

        /// <summary>GCD已过时间（秒）</summary>
        public static float GCD已过 => ActionHelper.GetGcdElapsed();

        /// <summary>GCD剩余时间（秒）</summary>
        public static float GCD剩余 => ActionHelper.GetGcdRemain();

        /// <summary>GCD是否激活</summary>
        public static bool GCD激活 => ActionHelper.GetGcdIsActive();

        /// <summary>是否可以使用GCD</summary>
        public static bool 可用GCD => GCD剩余 <= 0.1f;

        /// <summary>是否可以使用能力技</summary>
        public static bool 可用能力技 => GCD剩余 <= 0.5f;

        /// <summary>动画锁定时间（秒）</summary>
        public static float 动画锁定 => ActionHelper.GetAnimationLock();

        /// <summary>是否动画锁定中</summary>
        public static bool 动画锁定中 => 动画锁定 > 0;

        #endregion

        #region GCD 偏移决策（GcdOffsetEngine）

        /// <summary>GCD剩余时间转毫秒</summary>
        public static float GCD剩余毫秒 => GcdOffsetEngine.GetGcdRemainMs(GCD剩余);

        /// <summary>GCD偏移决策结果：能否排队/能否使用/能否优化使用</summary>
        public static (bool 可排队, bool 可使用, bool 可优化使用, string 原因) GCD偏移决策(bool 启用, int 排队窗口毫秒, int 偏移毫秒, int 帧补偿毫秒 = 0)
            => GcdOffsetEngine.Decide(GCD剩余, 启用, 排队窗口毫秒, 偏移毫秒, 帧补偿毫秒);

        /// <summary>GCD 是否在排队窗口内（常用简化判断）</summary>
        public static bool GCD可排队(int 排队窗口毫秒 = 500) => GCD剩余毫秒 <= Math.Clamp(排队窗口毫秒, 50, 1000);

        /// <summary>GCD 是否在偏移窗口内（优化使用判断）</summary>
        public static bool GCD可优化使用(int 偏移毫秒, int 帧补偿毫秒 = 0) => GCD剩余毫秒 <= Math.Clamp(偏移毫秒, 0, 500) + Math.Clamp(帧补偿毫秒, 0, 100);

        #endregion

        #region uint 扩展：技能冷却窗口预判

        /// <summary>技能冷却是否在 X 个 GCD 以内</summary>
        public static bool 冷却在几GCD内(uint 技能ID, int GCD数) => 技能ID.CoolDownInGcds(GCD数);

        /// <summary>能力技冷却是否在接下来 X 个 GCD 窗口内转好（含队列窗口容差）</summary>
        public static bool 冷却在几GCD窗口内(uint 技能ID, int GCD数) => 技能ID.AbilityCoolDownInNextXGcdsWindow(GCD数);

        #endregion

        #region 咏唱系统

        /// <summary>是否在读条</summary>
        public static bool 读条中 => 玩家?.IsCasting ?? false;

        /// <summary>读条总时长（秒）</summary>
        public static float 读条总时长 => ActionHelper.GetCastTimeTotal();

        /// <summary>读条已过时间（秒）</summary>
        public static float 读条已过 => ActionHelper.GetCastTimeElapsed();

        /// <summary>读条剩余时间（秒）</summary>
        public static float 读条剩余 => ActionHelper.GetCastTimeRemain();

        /// <summary>连击剩余时间（秒）</summary>
        public static float 连击剩余 => ActionHelper.GetComboLeftTime();

        #endregion

        #region 读条滑步

        /// <summary>获取技能读条时间（毫秒）</summary>
        public static int 技能读条毫秒(uint 技能ID)
        {
            var row = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>()?.GetRowOrDefault(技能ID);
            if (!row.HasValue) return 0;
            return row.Value.Cast100ms * 100;
        }

        /// <summary>调整后剩余读条时间（毫秒），已自动减去 200ms 滑步窗口</summary>
        public static double 调整后剩余读条毫秒
        {
            get
            {
                if (!读条中) return 0;
                return Math.Max(0, (读条总时长 - 读条已过) * 1000.0 - 200.0);
            }
        }

        /// <summary>是否应该等待读条完成（延迟未超上限 + 剩余读条 < 剩余等待预算）</summary>
        public static bool 应等读条(DateTime 开始时间, int 允许延迟毫秒)
        {
            if (允许延迟毫秒 <= 0) return false;
            var 剩余 = 调整后剩余读条毫秒;
            if (剩余 <= 0) return false;
            var 已过 = (DateTime.UtcNow - 开始时间).TotalMilliseconds;
            return 已过 < 允许延迟毫秒 && 剩余 < 允许延迟毫秒 - 已过;
        }

        /// <summary>当前读条是否在滑步窗口内（剩余读条 ≤ 200ms）</summary>
        public static bool 滑步窗口内 => 读条中 && 调整后剩余读条毫秒 <= 0;

        #endregion

        #region 小队相关

        /// <summary>所有队友（包括自己，按UI顺序）</summary>
        public static List<IBattleChara> 所有队友 => PartyHelper.GetParty();

        /// <summary>所有队友（不包括自己）</summary>
        public static List<IBattleChara> 队友们
        {
            get
            {
                var list = new List<IBattleChara>();
                foreach (var x in 所有队友) if (x != 玩家) list.Add(x);
                return list;
            }
        }

        /// <summary>存活的队友</summary>
        public static List<IBattleChara> 活队友
        {
            get
            {
                var list = new List<IBattleChara>();
                foreach (var x in 队友们) if (!x.IsDead) list.Add(x);
                return list;
            }
        }

        /// <summary>指定范围内的队友</summary>
        public static List<IBattleChara> 范围内队友(float 范围)
        {
            var result = new List<IBattleChara>();
            if (玩家 == null) return result;
            foreach (var x in 活队友)
            {
                var dist = Vector3.Distance(玩家的位置, x.Position) - (玩家.HitboxRadius + x.HitboxRadius);
                if (dist <= 范围) result.Add(x);
            }
            return result;
        }

        /// <summary>30米内队友</summary>
        public static List<IBattleChara> 三十米内 => 范围内队友(30);
        /// <summary>20米内队友</summary>
        public static List<IBattleChara> 二十米内 => 范围内队友(20);
        /// <summary>10米内队友</summary>
        public static List<IBattleChara> 十米内 => 范围内队友(10);

        /// <summary>获取队友血量百分比</summary>
        public static float 队友血量(IBattleChara? 队友) => 队友 != null ? PartyHelper.GetHpPercent(队友) : 0;

        /// <summary>获取队友蓝量百分比</summary>
        public static float 队友蓝量(IBattleChara? 队友) => 队友 != null ? PartyHelper.GetMpPercent(队友) : 0;

        /// <summary>血量最低的队友</summary>
        public static IBattleChara? 最残血
        {
            get
            {
                var list = 活队友;
                if (list.Count == 0) return null;
                IBattleChara? result = null;
                float lowest = float.MaxValue;
                foreach (var x in list) { var hp = PartyHelper.GetHpPercent(x); if (hp < lowest) { lowest = hp; result = x; } }
                return result;
            }
        }

        /// <summary>按索引获取队友 (0=自己)</summary>
        public static IBattleChara? 队友(int 索引) => PartyHelper.GetPartyMember(索引);

        /// <summary>获取队友目标类型（用于PAction）</summary>
        public static ActionTargetType 队友目标类型(int 索引) => PartyHelper.GetTargetTypeByIndex(索引);

        /// <summary>查找带指定状态的队友</summary>
        public static IBattleChara? 找带状态的队友(uint 状态ID, bool 包含自己 = true)
            => PartyHelper.FindPartyMemberCharWithStatus(状态ID, 包含自己);

        #endregion

        #region 职业量谱 - 坦克

        public static class DRK { public static byte 暗血 => JobGaugeHelper.DRK.Blood; public static bool 暗技 => JobGaugeHelper.DRK.HasDarkArts; public static float 暗黑剩余 => JobGaugeHelper.DRK.DarksideTimeRemaining; public static float 弗雷剩余 => JobGaugeHelper.DRK.ShadowTimeRemaining; }
        public static class WAR { public static byte 兽魂 => JobGaugeHelper.WAR.Beast; }
        public static class PLD { public static byte 忠义 => JobGaugeHelper.PLD.Oath; }
        public static class GNB { public static byte 子弹 => JobGaugeHelper.GNB.Ammo; public static byte 连击阶段 => JobGaugeHelper.GNB.AmmoComboStep; }

        #endregion

        #region 职业量谱 - 治疗

        public static class WHM { public static byte 百合 => JobGaugeHelper.WHM.Lily; public static byte 血百合 => JobGaugeHelper.WHM.BloodLily; public static short 百合计时 => JobGaugeHelper.WHM.LilyTimer; }
        public static class SCH { public static short 豆子 => JobGaugeHelper.SCH.AetherflowStack; public static short 炽天剩余 => JobGaugeHelper.SCH.SeraphTimer; public static short 妖精能量 => JobGaugeHelper.SCH.FairyGauge; }
        public static class SGE { public static byte 蓝豆 => JobGaugeHelper.SGE.Addersgall; public static short 蓝豆计时 => JobGaugeHelper.SGE.AddersgallTimer; public static byte 红豆 => JobGaugeHelper.SGE.Addersting; public static bool 均衡 => JobGaugeHelper.SGE.Eukrasia; }

        #endregion

        #region 职业量谱 - 近战

        public static class SAM { public static byte 剑气 => JobGaugeHelper.SAM.剑气; public static byte 剑压 => JobGaugeHelper.SAM.剑压; public static bool 雪 => JobGaugeHelper.SAM.HasYuki; public static bool 月 => JobGaugeHelper.SAM.HasMoon; public static bool 花 => JobGaugeHelper.SAM.HasHana; public static int 闪数量 => JobGaugeHelper.SAM.GetSenCount(); }
        public static class NIN { public static byte 风魔 => JobGaugeHelper.NIN.Kazematoi; public static byte 忍气 => JobGaugeHelper.NIN.Ninki; }
        public static class DRG { public static byte 龙眼 => JobGaugeHelper.DRG.FirstmindsFocusCount; public static bool 红龙血 => JobGaugeHelper.DRG.IsLOTDActive; public static float 红龙剩余 => JobGaugeHelper.DRG.LOTDTimer; }
        public static class MNK { public static byte 脉轮 => JobGaugeHelper.MNK.Chakra; public static float 必杀剩余 => JobGaugeHelper.MNK.BlitzTimeRemaining; }
        public static class RPR { public static byte 灵魂 => JobGaugeHelper.RPR.灵魂值; public static byte 魂衣 => JobGaugeHelper.RPR.魂衣值; public static ushort 夜游剩余 => JobGaugeHelper.RPR.夜游魂衣剩余时间; public static byte 夜游魂 => JobGaugeHelper.RPR.夜游魂; public static byte 虚无魂 => JobGaugeHelper.RPR.虚无魂; }
        public static class VPR { public static byte 飞蛇 => JobGaugeHelper.VPR.飞蛇之魂层数; public static byte 灵力 => JobGaugeHelper.VPR.灵力值; public static byte 祖灵 => JobGaugeHelper.VPR.祖灵力档数; }

        #endregion

        #region 职业量谱 - 远程

        public static class BRD { public static Dalamud.Game.ClientState.JobGauge.Enums.Song 当前歌曲 => JobGaugeHelper.BRD.GetCurrentSong; public static ushort 歌曲剩余 => JobGaugeHelper.BRD.GetCurrentSongTimer; public static byte 灵魂之声 => JobGaugeHelper.BRD.GetSoulVoice; public static byte 诗心 => JobGaugeHelper.BRD.GetRepertoire; }
        public static class MCH { public static byte 热量 => JobGaugeHelper.MCH.Heat; public static byte 电池 => JobGaugeHelper.MCH.Battery; public static bool 机器人激活 => JobGaugeHelper.MCH.IsRobotActive; }
        public static class DNC { public static bool 跳舞中 => JobGaugeHelper.DNC.IsDancing; public static byte 伶俐 => JobGaugeHelper.DNC.Esprit; public static byte 幻扇 => JobGaugeHelper.DNC.Feathers; public static uint 下一步 => JobGaugeHelper.DNC.NextStep; public static uint 已完成步数 => JobGaugeHelper.DNC.CompletedSteps; }
        public static class SMN { public static bool 有宠物 => JobGaugeHelper.SMN.HasPet; public static byte 豆子 => JobGaugeHelper.SMN.AetherflowStacks; public static bool 巴哈姆特 => JobGaugeHelper.SMN.IsBahamutReady; public static bool 凤凰 => JobGaugeHelper.SMN.IsPhoenixReady; }
        public static class RDM { public static byte 黑魔元 => JobGaugeHelper.RDM.BlackMana; public static byte 白魔元 => JobGaugeHelper.RDM.WhiteMana; public static byte 魔元集 => JobGaugeHelper.RDM.ManaStacks; }
        public static class PCT { public static byte 颜料 => JobGaugeHelper.PCT.PalleteGauge; public static byte 豆子 => JobGaugeHelper.PCT.Paint; public static bool 生物画 => JobGaugeHelper.PCT.CreatureMotifDrawn; public static bool 武器画 => JobGaugeHelper.PCT.WeaponMotifDrawn; public static bool 风景画 => JobGaugeHelper.PCT.LandscapeMotifDrawn; public static bool 莫古炮 => JobGaugeHelper.PCT.MooglePortraitReady; public static bool 马蒂恩 => JobGaugeHelper.PCT.MadeenPortraitReady; }

        #endregion

        #region 状态/Buff 相关

        /// <summary>是否有指定状态</summary>
        public static bool 有状态(IBattleChara? 单位, uint 状态ID) => 单位 != null && StatusHelper.HasStatus(单位, 状态ID);

        /// <summary>获取状态层数</summary>
        public static int 状态层数(IBattleChara? 单位, uint 状态ID) => 单位 == null ? 0 : StatusHelper.GetStatusStack(单位, 状态ID);

        /// <summary>获取状态剩余时间（秒）</summary>
        public static float 状态剩余(IBattleChara? 单位, uint 状态ID) => 单位 == null ? 0 : StatusHelper.GetStatusLeftTime(单位, 状态ID);

        /// <summary>自己是否有状态</summary>
        public static bool 玩家有状态(uint 状态ID) => 有状态(玩家, 状态ID);

        /// <summary>目标是否有状态</summary>
        public static bool 目标有状态(uint 状态ID) => 有状态(目标, 状态ID);

        #endregion

        #region 移动相关

        /// <summary>是否在移动</summary>
        public static bool 移动中 => MoveManager.IsLocalPlayerMoving;

        /// <summary>获取鼠标位置的世界坐标</summary>
        public static Vector3 鼠标位置 => PosHelper.ScreenToWorld();

        /// <summary>传送（需认证）</summary>
        public static void 传送(Vector3 位置) => HackHelper.TeleportNormal(位置);

        /// <summary>传送到鼠标位置</summary>
        public static void 传送到鼠标() => HackHelper.TeleportToMouse();

        /// <summary>判断两点是否在指定范围内</summary>
        public static bool 在范围内(Vector3 当前, Vector3 目标, float 范围, bool 忽略Y轴 = true)
            => PosHelper.IsWithinRange(当前, 目标, 范围, 忽略Y轴);

        #endregion

        #region 目标查找

        /// <summary>获取周围敌人数量</summary>
        public static uint 周围敌人(float 范围) => TargetHelper.EnemyInRange(范围);

        /// <summary>5米内敌人</summary>
        public static uint 五米敌人 => TargetHelper.EnemyIn5m();

        /// <summary>目标周围敌人数量</summary>
        public static uint 目标周围敌人(IBattleChara? 目标, float 范围) => 目标 == null ? 0 : TargetHelper.EnemyInRangeTarget(目标, 范围);

        /// <summary>扇形范围内敌人数量</summary>
        public static int 扇形敌人(IBattleChara 自身, IBattleChara 目标, float 扇形半径, float 扇形角度)
            => TargetHelper.GetEnemyCountInsideSector(自身, 目标, 扇形半径, 扇形角度);

        /// <summary>找最佳AoE目标</summary>
        public static IBattleChara? 最佳AoE目标(uint 技能ID, int 最少目标数, float 扇形角度)
            => TargetHelper.GetMostCanTargetObjects(技能ID, 最少目标数, 扇形角度);

        /// <summary>攻击距离（含扩程修正）</summary>
        public static float 攻击距离(float 基础距离) => GameData.GetCurrentAttackRange(基础距离);

        /// <summary>切换当前目标</summary>
        public static void 切换目标(IBattleChara? t) => Core.SetTarget(t);

        /// <summary>按BaseId找NPC</summary>
        public static IBattleChara? 找NPC(uint BaseId) => TargetHelper.FindNpcByBaseId(BaseId);

        /// <summary>按BaseId找所有NPC</summary>
        public static List<IBattleChara> 找所有NPC(uint BaseId) => TargetHelper.FindNpcsByBaseId(BaseId);

        #endregion

        #region 物品相关

        /// <summary>获取背包物品数量</summary>
        public static uint 物品数量(uint 物品ID, bool 高品质 = false) => ItemHelper.GetItemCountInInventory(物品ID, 高品质);

        /// <summary>是否有物品</summary>
        public static bool 有物品(uint 物品ID, bool 高品质 = false) => 物品数量(物品ID, 高品质) > 0;

        /// <summary>最佳爆发药</summary>
        public static uint 最佳爆发药 => GameData.GetBestPotionId();

        #endregion

        #region 极限技

        /// <summary>极限技充能段数</summary>
        public static uint LB段数 => LimitBreakHelper.GetLimitBreakCharge();

        /// <summary>极限技技能ID</summary>
        public static uint LB技能ID => LimitBreakHelper.GetLimitBreakActionId();

        /// <summary>LB是否可用</summary>
        public static bool LB可用 => LB段数 < 999 && LB段数 >= 0;

        #endregion

        #region UI相关

        /// <summary>技能栏脉冲（闪烁提示）</summary>
        public static void 脉冲技能(uint 技能ID) => UiHelper.PulseActionBar(技能ID);

        /// <summary>显示屏幕提示</summary>
        public static void 屏幕提示(string 文本, float 秒数, HintHelper.HintType 类型 = HintHelper.HintType.Info)
            => HintHelper.ShowToast2(文本, 秒数, 类型);

        #endregion

        #region 日志相关

        public static void 调试(string 消息) => Svc.Log.Debug(消息);
        public static void 信息(string 消息) => Svc.Log.Info(消息);
        public static void 警告日志(string 消息) => Svc.Log.Warning(消息);
        public static void 错误(string 消息) => Svc.Log.Error(消息);

        #endregion

        #region 数学工具

        public static double 角度(Vector2 点1, Vector2 点2) => MathHelper.θVector2(点1, 点2);
        public static double 标准化角度(double 角度) => MathHelper.NormalizeAngle(角度);
        public static Vector3 取整(Vector3 向量, int 位数) => MathHelper.Round(向量, 位数);

        #endregion

        #region QT 设置相关

        /// <summary>获取 QT 开关状态</summary>
        public static bool 获取QT(string qtKey) => PromeSettings.Instance.GetQt(qtKey);

        /// <summary>设置 QT 开关状态（自动联动，批量写入）</summary>
        public static void 设置QT(string qtKey, bool 值)
        {
            var dict = PromeSettings.Instance.QuickToggles;
            dict[qtKey] = 值;
            if (MCHQT.CascadeRules.TryGetValue(qtKey, out var links))
                foreach ((string lk, bool inv) in links)
                    dict[lk] = inv ? !值 : 值;
        }

        private static readonly Dictionary<string, bool> _prevQt = new();

        /// <summary>重建 QT 可见性：ClearQts 后只注册当前模式可见的 QT</summary>
        public static void 重建QT可见性()
        {
            var isHigh = MCHSettings.Instance.IsHighEnd;
            var qt = PromeSettings.Instance.QuickToggles;
            var saved = new Dictionary<string, bool>();
            foreach (var (key, _) in MCHQT.All)
                if (qt.TryGetValue(key, out var v)) saved[key] = v;
            PromeSettings.Instance.ClearQts();
            foreach (var (key, defVal) in MCHQT.All)
            {
                if (!MCHQT.IsVisibleInMode(key, isHigh)) continue;
                var val = saved.TryGetValue(key, out var sv) ? sv : defVal;
                PromeSettings.Instance.AddQt(key, val);
                qt[key] = val;
            }
            PromeSettings.Instance.AddQt(MCHQT.高难模式, isHigh);
            qt[MCHQT.高难模式] = isHigh;
        }

        /// <summary>轮询检测 QT 联动变更（供 OnUpdate / Draw 调用）</summary>
        public static void 轮询联动()
        {
            var qt = PromeSettings.Instance.QuickToggles;
            bool changed = true;
            int safety = 0;
            while (changed && safety++ < 10)
            {
                changed = false;
                foreach (var (trigger, links) in MCHQT.CascadeRules)
                {
                    if (!qt.TryGetValue(trigger, out var cur)) continue;
                    var prev = _prevQt.TryGetValue(trigger, out var p) ? p : cur;
                    if (cur != prev)
                    {
                        foreach ((string lk, bool inv) in links)
                            qt[lk] = inv ? !cur : cur;
                        changed = true;
                    }
                    _prevQt[trigger] = cur;
                }
            }
        }

        /// <summary>切换 QT 开关状态</summary>
        public static void 切换QT状态(string qtKey)
        {
            var 当前值 = PromeSettings.Instance.GetQt(qtKey);
            PromeSettings.Instance.SetQt(qtKey, !当前值);
        }

        /// <summary>批量设置 QT</summary>
        public static void 批量设置QT状态<T>(Dictionary<T, bool> 配置表) where T : Enum
        {
            foreach (var kvp in 配置表)
                PromeSettings.Instance.SetQt(kvp.Key.ToString(), kvp.Value);
        }

        /// <summary>注册 QT 开关（仅在 ACR 构造中调用）</summary>
        public static void 添加QT(string qtKey, bool 默认值) => PromeSettings.Instance.AddQt(qtKey, 默认值);

        /// <summary>起手是否已执行（框架状态读写）</summary>
        public static bool 起手已执行
        {
            get => PromeSettings.Instance.OpenerHasBeenExecuted;
            set => PromeSettings.Instance.OpenerHasBeenExecuted = value;
        }

        #endregion

        #region 起手 / Timeline 相关

        /// <summary>获取 PureTimeline 选择的起手名称</summary>
        public static string? 时间轴起手名称 => PromeRotation.PureTimeline.PtlManager.CurrentOpener;

        /// <summary>获取 Timeline 元数据中的起手名称</summary>
        public static string? 元数据起手名称 => PromeRotation.Timeline.TimelineManager.CurrentMeta?.Opener;

        /// <summary>按职业 ID 获取所有注册的起手</summary>
        public static IReadOnlyDictionary<string, Type>? 获取起手列表(int 职业ID) => RotationManager.GetOpenersByJob(职业ID);

        #endregion

        #region Debug 状态

        /// <summary>清除 GCD Solver 调试状态</summary>
        public static void 清除GCD调试状态() => RotationManager.GcdSolverStatus.Clear();

        /// <summary>清除 oGCD Solver 调试状态</summary>
        public static void 清除oGCD调试状态() => RotationManager.OffGcdSolverStatus.Clear();

        /// <summary>添加 GCD Solver 调试状态条目</summary>
        public static void 添加GCD调试状态(string name, bool success, string message)
            => RotationManager.GcdSolverStatus.Add(new PromeRotation.Managers.SolverStatus { Name = name, Success = success, Message = message });

        /// <summary>添加 oGCD Solver 调试状态条目</summary>
        public static void 添加oGCD调试状态(string name, bool success, string message)
            => RotationManager.OffGcdSolverStatus.Add(new PromeRotation.Managers.SolverStatus { Name = name, Success = success, Message = message });

        #endregion

        #region 技能排程（ActionQueueManager）

        /// <summary>将单个技能加入执行队列（高优先=插队）</summary>
        public static void 排入队列(PAction action, bool 高优先 = false)
            => ActionQueueManager.Enqueue(action, 高优先);

        /// <summary>将一组技能加入执行队列</summary>
        public static void 排入队列(List<PAction> actions, bool 高优先 = false)
            => ActionQueueManager.Enqueue(actions, 高优先);

        /// <summary>清空所有队列</summary>
        public static void 清空队列() => ActionQueueManager.ClearAllQueues();

        /// <summary>队列中是否有待执行技能</summary>
        public static bool 队列中有技能 => ActionQueueManager.HasActionsInQueue();

        /// <summary>将单个技能加入队列并记录使用</summary>
        public static void 排入并记录(PAction action, bool 高优先 = false)
            => ActionQueueManager.EnqueueAndRecord(action, 高优先);

        /// <summary>将一组技能加入队列并记录使用</summary>
        public static void 排入并记录(List<PAction> actions, bool 高优先 = false)
            => ActionQueueManager.EnqueueAndRecord(actions, 高优先);

        /// <summary>是否有高优先级动作待执行</summary>
        public static bool 有高优动作 => ActionQueueManager.HasHighPriorityAction();

        /// <summary>Always 队列中是否有动作</summary>
        public static bool Always队列有动作 => ActionQueueManager.HasActionsInAlwaysQueue();

        /// <summary>GCD 队列中是否有动作</summary>
        public static bool GCD队列有动作 => ActionQueueManager.HasActionsInGcdQueue();

        /// <summary>oGCD 队列中是否有动作</summary>
        public static bool oGCD队列有动作 => ActionQueueManager.HasActionsInOffGcdQueue();

        #endregion

        #region GCD 卡死检测（GcdWatchdogService）

        /// <summary>GCD 是否卡死（框架检测到 GCD 长时间无进展时强制返回0）</summary>
        public static bool GCD卡死 => ActionHelper.Watchdog?.ShouldForceRemainZero ?? false;

        /// <summary>GCD 卡死持续时间（秒）</summary>
        public static double GCD卡死时长 => ActionHelper.Watchdog?.StuckDurationSeconds ?? 0;

        #endregion

        // ============================================================
        // === MCH 兼容快捷方式（保持现有代码调用不变） ===
        // ============================================================
        public static void 提示(string msg, float sec = 3) => 屏幕提示(msg, sec);
        public static void 提示(string msg, float sec, HintHelper.HintType type) => 屏幕提示(msg, sec, type);
        public static float 玩家血量 => 玩家的血量;
        public static byte 玩家等级 => (byte)玩家的等级;
        public static byte 热量 => MCH.热量;
        public static byte 电池 => MCH.电池;
        public static bool 机器人激活 => MCH.机器人激活;
        public static PAction 自我能力(uint id) => 对自己技能(id);

        /// <summary>当前是否在 GCD 后半窗口（安全编织 oGCD）</summary>
        public static bool GCD后半窗口 => GCD总时长 - GCD剩余 >= GCD总时长 * 0.5f;

        /// <summary>GCD 后半是否还剩足够时间放一个 oGCD</summary>
        public static bool GCD后半安全 => GCD后半窗口 && GCD剩余 >= 0.6f;
    }

    /// <summary>扩展方法</summary>
    public static class ApiExtensions
    {
        /// <summary>血量百分比</summary>
        public static float 血量(this ICharacter? 角色) => 角色 is IBattleChara battle ? PartyHelper.GetHpPercent(battle) : 0;

        /// <summary>蓝量百分比</summary>
        public static float 蓝量(this ICharacter? 角色) => 角色 is IBattleChara battle ? PartyHelper.GetMpPercent(battle) : 0;

        /// <summary>是否存活</summary>
        public static bool 活着(this ICharacter? 角色) => 角色 != null && !角色.IsDead;

        /// <summary>是否死亡</summary>
        public static bool 死了(this ICharacter? 角色) => 角色 == null || 角色.IsDead;

        /// <summary>距玩家距离</summary>
        public static float 距玩家(this IGameObject? 对象)
        {
            if (对象 == null || ApiHelper.玩家 == null) return 999;
            return Vector3.Distance(ApiHelper.玩家的位置, 对象.Position)
                   - (ApiHelper.玩家.HitboxRadius + 对象.HitboxRadius);
        }

        /// <summary>是否在范围内</summary>
        public static bool 在玩家范围内(this IGameObject? 对象, float 范围) => 对象.距玩家() <= 范围;

        /// <summary>是否是敌人</summary>
        public static bool 是敌人(this IGameObject? 对象)
            => 对象 != null && (对象.ObjectKind == ObjectKind.BattleNpc || 对象.ObjectKind == ObjectKind.EventNpc);
    }
}
