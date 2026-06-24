---
name: mch-dev
description: LccMch ACR 开发与维护——基于 PromeRotation 的机工士自动循环。用于修改、调试、扩展 LccMch ACR 插件时使用。
---

# LccMch ACR 开发与维护

LccMch 是一个基于 **PromeRotation** 框架的 FFXIV 机工士 (MCH) 自动循环 ACR。

## 项目结构

```
MCH/
├── MCH.csproj                  # 动态取最新 PromeRotation 版本，输出到 ACR\MCH
├── MCHRotation.cs              # ★ 核心协调器：Resolver 注册 + 循环 + UI 委托（~100行）
├── MCHRotationEventHandler.cs  # 事件回调 + IRotationLifecycle（OnEnterAcr/OnExitAcr）
├── MCHHelper.cs                # 辅助方法：整备选择(7段)/AOE判断/SdjCD/JobGauge/IsTargetImmune
├── ApiHelper.cs                # ★ 中文 API 壳（540行全量，底层框架升级只需改此文件）
├── Data/
│   ├── MCHSkill.cs             # 技能 ID 常量
│   ├── MCHBuff.cs              # Buff/Debuff ID 常量
│   ├── MCHQT.cs                # ★ QT 唯一数据源（键名 + 默认值）
│   ├── MCHBattleData.cs        # 单场战斗缓存
│   └── MCHSettings.cs          # 运行期设置 + JSON 持久化 + QT 默认值管理
├── Action/
│   ├── Gcd/                    # GCD Resolver（优先级=注册顺序）
│   │   ├── 过热连击Gcd.cs      # 1. 过热：热冲击/烈焰弹/自动弩
│   │   ├── 全金属爆发Gcd.cs    # 2. 全金属爆发（含野火CD联动）
│   │   ├── 触发技能Gcd.cs      # 3. 委托 CheckReassembleGcd 选高价值 GCD
│   │   └── 基础连击Gcd.cs      # 4. 1-2-3 连击 / 霰弹枪 AoE（兜底）
│   └── OffGcd/                 # oGCD Resolver
│       ├── 内丹OffGcd.cs       # 1. 保命（可配置血量阈值）
│       ├── 减伤OffGcd.cs       # 2. 策动/武装解除（高难跳过）
│       ├── 爆发药OffGcd.cs     # 3. 高难自动爆发药（野火CD联动）
│       ├── 野火OffGcd.cs       # 4. 野火（WFOnlyBoss + CanBurst）
│       ├── 整备OffGcd.cs       # 5. 整备（RecentlyUsed 防重复）
│       ├── 枪管加热OffGcd.cs   # 6. 枪管加热
│       ├── 超荷OffGcd.cs       # 7. 超荷（CanBurst + 连击保护）
│       ├── 机器人OffGcd.cs     # 8. 机器人（IsRobotActive + 真实电量）
│       └── 双将将死OffGcd.cs   # 9. 充能泄出（优先层数多的）
├── Opener/
│   ├── MCHOpenerManager.cs     # 起手调度（按等级 + 设置选择）
│   ├── MCHOpenerChecker.cs     # 起手爆发技能就绪校验
│   ├── MCHAirAnchorOpener100.cs # 空气锚起手（12 GCD，抢开检测）
│   ├── MCHDrillOpener100.cs    # 钻头起手（12 GCD，抢开检测）
│   ├── MCHKfkOpener100.cs      # 绝妖星特化起手（11 GCD，抢开检测）
│   └── MCHAirAnchorOpener90.cs # 90级空气锚起手（11 GCD，抢开检测）
├── UI/
│   ├── MCHQTUI.cs              # QT 面板 + 联动回调
│   ├── MCHSettingsUI.cs        # 设置面板（通用/高难/日随/QT管理/更新日志/Dev）
│   └── MCHHotkeyUI.cs          # 热键面板（11 按钮）
└── Helper/
    └── MCHMacroManager.cs       # 聊天命令 /Lcc_Mch（切换QT/保存/模式切换）
```

## 核心开发流程

### 0. 代码入口

```
PromeRotation 每帧调用:
  OnEnterAcr()              → 切换到本 ACR 时（IRotationLifecycle）
  NextAlways()               → 立即执行（无视 GCD）
  NextGcd()                  → GCD 转好时调用 → 遍历 _gcdResolvers
  NextOffGcd()               → GCD 转着时调用 → 遍历 _offGcdResolvers
  GetOpener()                → 进战时调用，返回起手序列
  OnExitAcr()                → 从本 ACR 切走时（IRotationLifecycle）

每个 Resolver:
  Check()    → "能不能/该不该放？" → 返回 CheckResult(Success, Message)
  GetAction() → "放什么？" → 返回 PAction(技能ID, 类型, 目标)

优先级 = 注册顺序。先 Add 的 Resolver 先被 Check，第一个成功的就执行。
```

### 1. 新增 QT 开关

只需改 2 个文件：

```csharp
// ① MCHQT.cs — 加常量 + 默认值
public const string 新功能 = "新功能";
// 在 All 字典加：{ 新功能, true },

// ② MCHQTUI.cs — 加 UI 按钮
Toggle(MCHQT.新功能, "新功能");
// QtList/QtDefaultValues/宏命令全部自动同步，无需改其他文件
```

### 2. 新增 GCD / oGCD Resolver

```csharp
// ① 新建文件 Action/Gcd/新技能Gcd.cs
public class 新技能Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // 标准前检查链
        var r = ApiHelper.攻击距离(25);
        var p = ApiHelper.玩家;
        if (p == null) return new(false, "玩家未加载");
        if (ApiHelper.目标 == null) return new(false, "无目标");
        if (ApiHelper.目标!.EntityId == p.EntityId) return new(false, "目标为自己");
        if (ApiHelper.目标.IsPlayer()) return new(false, "目标为玩家");
        if (p.DistanceToMe() > r) return new(false, "过远");
        if (ApiHelper.读条中) return new(false, "读条中");
        if (ApiHelper.获取QT(MCHQT.停手)) return new(false, "停手");

        // 业务逻辑
        if (!ApiHelper.技能可用(MCHSkill.xxx)) return new(false, "冷却中");
        return new(true, "就绪");
    }

    public PAction GetAction() => new(MCHSkill.xxx, ActionType.Gcd, ActionTargetType.Target);
}

// ② 在 MCHRotation 构造函数中按优先级注册
_gcdResolvers.Add(new 新技能Gcd());
```

### 3. 新增设置项

```csharp
// ① MCHSettings.cs — 加字段
public int 新设置 = 默认值;

// ② MCHSettingsUI.cs — 加 UI
// 在 DrawGeneral() 或 DrawHighEnd()/DrawNormal() 中加滑块/复选框
```

### 4. API 速查

全部走 `ApiHelper` 壳（不要直接调原生 PR API）：

| 需求 | 调用 |
|------|------|
| 玩家/目标 | `ApiHelper.玩家` `ApiHelper.目标` |
| 技能可用 | `ApiHelper.技能可用(id)` |
| 技能冷却 | `ApiHelper.技能冷却(id)` |
| 技能充能 | `ApiHelper.技能充能(id)` |
| Buff 判断 | `ApiHelper.玩家有状态(buffId)` |
| Buff 剩余 | `ApiHelper.状态剩余(player, buffId)` |
| GCD 剩余 | `ApiHelper.GCD剩余` |
| 连击剩余 | `ApiHelper.连击剩余` |
| 是否读条 | `ApiHelper.读条中` |
| 血量 | `ApiHelper.玩家血量` |
| 周围敌人 | `ApiHelper.周围敌人(range)` |
| 扇形敌人 | `ApiHelper.扇形敌人(me, target, range, angle)` |
| 最佳AoE目标 | `ApiHelper.最佳AoE目标(spellId, minTargets, angle)` |
| 攻击距离 | `ApiHelper.攻击距离(baseRange)` |
| QT 读写 | `ApiHelper.获取QT(key)` `ApiHelper.设置QT(key, val)` |
| 热量/电池 | `ApiHelper.热量` `ApiHelper.电池` |
| 机器人激活 | `ApiHelper.机器人激活` |
| 爆发药 | `ApiHelper.最佳爆发药` |
| 防重复 | `ApiHelper.最近用过(id, ms)` |
| 屏幕提示 | `ApiHelper.提示(msg, sec)` |
| 构建 PAction | `ApiHelper.自我能力(id)` → OffGcd+Self |
| 切换目标 | `ApiHelper.切换目标(target)` |

### 5. 构建 & 验证

```powershell
cd MCH
dotnet build
# 输出 → %APPDATA%\XIVLauncherCN\pluginConfigs\PromeRotation\ACR\MCH\MCH.dll
```

进游戏 → 切机工 → 打开 PromeRotation 面板 → 确认 ACR 列表中显示 `LccMch` → 打木人验证。

### 6. 常见问题

| 问题 | 解决 |
|------|------|
| `dotnet build` 失败 | 检查 PR 版本目录是否存在 `PromeRotation.dll`，csproj 自动取最新 |
| ACR 不加载 | 输出目录名必须和 `RotationMetadata` 的 author 一致 |
| 技能不放但 Debug 显示成功 | 等级不够 / 技能 ID 错 / GCD 窗口不足 |
| 新增 QT 不生效 | 确认在 `MCHRotation` 构造函数中调了 `PromeSettings.Instance.AddQt` |
| ApiHelper 找不到某方法 | 查看 `ApiHelper.cs` 文件，全量 API 已包含 |

## 设计原则

1. **单一数据源**：QT 键名+默认值只在 `MCHQT.All` 一处定义
2. **API 壳隔离**：所有 PR 框架调用走 `ApiHelper`，框架升级只改壳
3. **Resolver 模式**：每个技能一个 Resolver，Check → GetAction，优先级靠注册顺序
4. **UI 解耦**：QT/设置/热键 UI 在独立 `UI/` 目录，MCHRotation 只做协调
5. **起手动态**：`InCombatSequence` 是 getter，运行时检测抢开
