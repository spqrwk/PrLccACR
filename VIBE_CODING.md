# LccMch Vibe Coding 开发指南

> 本文是 LccMch 的 AI 协作开发上下文。开始改代码前，先阅读本文和 `PromeRotation_本体API总结.md`；当文档与实际代码冲突时，以当前代码及可成功编译的 SDK API 为准，并同步修正文档。

## 1. 项目一句话说明

LccMch 是基于 PromeRotation 的《最终幻想 XIV》机工士（MCH）自动循环模块，目标是在不破坏玩家手动控制的前提下，为高难与日随场景提供可解释、可配置、可调试的 GCD、oGCD 和起手决策。

## 2. 当前技术基线

- 语言：C#，开启 Nullable 与 Implicit Usings。
- 项目入口：`MCH/MCH.csproj`。
- 默认构建：国服 CNGL，`net10.0-windows`，SDK 为 `PromeRotation.SDK.CNGL`。
- 可选构建：繁中服 TC，`net9.0-windows`，SDK 为 `PromeRotation.SDK.TC`。
- 框架契约：`IRotation`、`IDecisionResolver`、`IOpener`、`PAction`、`CheckResult`。
- UI：Dalamud / ImGui 风格设置与 QT 面板。
- 配置：`MCHSettings` 单例，使用 JSON 持久化；布尔战斗开关优先使用 QT。
- 参考资料：`PromeRotation_本体API总结.md`。

常用构建命令：

```powershell
dotnet build .\MCH\MCH.csproj
dotnet build .\MCH\MCH.csproj -p:SdkFlavor=TC
```

默认输出目录指向本机 PromeRotation ACR 配置目录；CI 可通过 `-p:OutputPath=...` 覆盖。

## 3. 目录与职责

```text
MCH/
├─ MCHRotation.cs               # 框架入口、Resolver 顺序、QT/起手注册
├─ MCHRotationEventHandler.cs   # 战斗事件处理
├─ MCHHelper.cs                 # 职业决策的共享算法
├─ ApiHelper.cs                 # PromeRotation API 的中文语义封装
├─ Data/
│  ├─ MCHSkill.cs               # 技能 ID
│  ├─ MCHBuff.cs                # 状态 ID
│  ├─ MCHQT.cs                  # QT 唯一数据源、联动和模式归属
│  ├─ MCHSettings.cs            # 数值/行为配置与持久化
│  └─ MCHBattleData.cs          # 战斗期状态
├─ Action/
│  ├─ Gcd/                      # GCD Resolver
│  └─ OffGcd/                   # oGCD Resolver
├─ Opener/                      # 各等级与场景的起手序列
└─ UI/                          # QT、设置、热键界面
```

依赖方向应尽量保持为：

```text
UI / Opener / Resolver → MCHHelper / ApiHelper → PromeRotation SDK
             ↓
       Data（技能、Buff、QT、Settings、战斗状态）
```

不要让 `Data` 反向依赖 UI，也不要在多个 Resolver 中复制同一段复杂窗口算法；共享规则应进入 `MCHHelper`。

## 4. 决策模型

### 4.1 Resolver 优先级就是战斗优先级

`MCHRotation` 分别维护 GCD 与 oGCD Resolver 列表。每帧按注册顺序调用 `Check()`，第一个 `Success == true` 的 Resolver 通过 `GetAction()` 产出动作。因此：

- 调整注册顺序属于战斗逻辑变更，必须说明影响。
- 高价值、强约束技能放在兜底技能之前。
- `基础连击Gcd` 必须保持为 GCD 末尾兜底。
- 新 Resolver 不能只验证自身能否释放，还要验证是否会抢占更高价值窗口。

当前 GCD 顺序：

1. 过热连击
2. 全金属爆发
3. 触发技能
4. 基础连击

当前 oGCD 顺序：

1. 内丹
2. 减伤
3. 爆发药
4. 野火
5. 整备
6. 枪管加热
7. 超荷
8. 机器人
9. 双将将死

### 4.2 `Check()` 与 `GetAction()` 的边界

`Check()` 负责纯决策，并返回可读原因；`GetAction()` 负责构造动作或队列。

推荐写法：

```csharp
public CheckResult Check()
{
    if (ApiHelper.玩家 == null) return new(false, "玩家未加载");
    if (ApiHelper.目标 == null) return new(false, "无目标");
    if (ApiHelper.获取QT(MCHQT.停手)) return new(false, "停手");
    if (!ApiHelper.技能已解锁(MCHSkill.某技能)) return new(false, "未解锁");
    if (!ApiHelper.技能可用(MCHSkill.某技能)) return new(false, "冷却中");
    return new(true, "某技能就绪");
}

public PAction GetAction() =>
    new(MCHSkill.某技能, ActionType.OffGcd, ActionTargetType.Target);
```

规则：

- `Check()` 会在正常决策和调试状态刷新时重复执行，避免在其中切目标、排队、保存配置或修改战斗状态。
- 每个失败分支给出短而具体的中文原因，确保调试面板能定位阻塞点。
- 玩家、目标、读图状态都可能瞬间为空，任何外部对象使用前必须判空。
- 需要一次性连续执行多动作时，使用框架队列；排队动作的 Resolver 要有防重入和超时恢复。
- `GetAction()` 若已经主动排入组合队列，应返回 `null`，避免同帧重复出招。

## 5. 数据与配置约定

### 5.1 技能与 Buff ID

- 技能 ID 统一放入 `MCHSkill.cs`。
- Buff/Debuff ID 统一放入 `MCHBuff.cs`。
- 禁止在 Resolver 中散落无语义的数字 ID。
- 新增 ID 时写清游戏内中文名；跨客户端不一致时，验证 CNGL 与 TC SDK/游戏数据。

### 5.2 QT 与 Settings

布尔型、需要战斗中快速切换的功能放 `MCHQT`；数值阈值和较稳定的偏好放 `MCHSettings`。

新增 QT 时至少完成：

1. 在 `MCHQT` 增加键名常量。
2. 加入 `All` 并明确默认值。
3. 加入 `ModeCategories`，明确通用、高难或日随归属。
4. 如有联动，加入 `CascadeRules`。
5. 在 UI 中安排展示位置与提示。
6. 在 Resolver 中读取 QT，不硬编码另一套默认值。

新增 Settings 字段时至少完成：

1. 给出稳定默认值和单位注释。
2. 在设置 UI 中提供合理范围。
3. 考虑旧 JSON 缺少该字段时的兼容行为。
4. 只在用户操作完成或状态确需落盘时调用 `Save()`，不要每帧写文件。

高难与日随各自保存 QT 快照。`高难模式` 是元数据 QT，不参与普通快照复制。

## 6. 战斗逻辑设计原则

按以下顺序思考每个技能决策：

1. 安全：玩家/目标有效、目标可攻击、距离正确、未读条、未停手。
2. 可用：等级、职业任务、冷却、充能、Buff 和资源满足。
3. 用户意图：QT、模式、起手选择和配置允许。
4. 窗口：GCD 编织安全、动画锁、关键技能即将转好、120 秒爆发对齐。
5. 价值：是否覆盖高价值 GCD、溢出资源或损失充能。
6. 恢复：队列失败、目标消失、战斗重置后能否自动回到正常状态。

额外约束：

- “停手”应阻止伤害动作，但不应无意阻断用户明确允许的保命逻辑。
- AoE 决策必须受 `AOE` QT 和目标数量阈值约束，切目标前确认收益与目标合法性。
- 高难默认保守，减少自动切目标与自动减伤；日随可更自动化。
- 不用固定延迟掩盖状态机问题。确需延迟时，应写明它对应的 GCD/动画锁窗口。
- 时间统一注明单位。SDK 多为秒，设置中若使用毫秒，要在边界处显式换算。

## 7. 起手开发约定

- 每套起手实现 `IOpener`，并在 `MCHRotation.Openers` 注册。
- 起手名称是外部时间轴和用户配置的契约，修改名称要考虑已有配置兼容。
- 起手必须考虑等级、技能是否解锁、是否使用爆发药，以及中途目标丢失。
- 通用循环不能依赖起手一定完整执行；起手中断后应能自然落回 Resolver 循环。
- 新起手至少用一段表格或注释记录动作顺序、预期时间和资源变化。

## 8. UI 与交互约定

- UI 只负责展示和修改配置，不承载战斗算法。
- QT 名称保持短、可扫读；复杂行为使用 tooltip 解释。
- 数值输入必须限制范围，并显示单位。
- 修改模式时统一走 `MCHSettings.SwitchMode()`，不要在 UI 里手工复制状态。
- 用户保存、重置、复制默认值后，应立即看到一致的 QT 状态。

## 9. 一次标准改动流程

1. 定位需求属于 Resolver、共享算法、配置、起手还是 UI。
2. 阅读相关文件及 `PromeRotation_本体API总结.md` 对应章节。
3. 写出可验证的行为条件：何时成功、何时阻止、优先级在哪里。
4. 做最小闭环改动，避免顺手重构无关代码。
5. 编译 CNGL；涉及跨服 API、目标框架或公共代码时同时编译 TC。
6. 检查调试原因是否能解释每个关键失败分支。
7. 检查空值、单位、队列防重入、模式默认值与旧配置兼容。
8. 汇报改了什么、为何这样决策、如何验证、仍有哪些游戏内验证项。

## 10. 完成定义（Definition of Done）

代码改动只有同时满足以下条件才算完成：

- 项目编译通过，无新增警告；若因环境无法编译，要明确报告原始错误。
- 新逻辑在 Resolver 顺序中的位置合理，且没有覆盖更高优先级动作。
- `CheckResult.Message` 足以从调试面板理解决策。
- 所有玩家、目标和游戏对象访问均处理瞬时 `null`。
- 技能/Buff ID 没有散落，秒与毫秒没有混用。
- QT、Settings、UI 和持久化形成完整闭环。
- 组合队列具有防重入与失败恢复。
- 不修改与需求无关的用户代码，不覆盖现有本地改动。
- 涉及实际循环手感的改动，列出需要进木桩或副本验证的场景。

建议的游戏内回归场景：

- 无目标、目标为玩家、目标死亡或超出距离。
- 低等级同步导致技能未解锁。
- 木桩单体完整起手与 120 秒爆发。
- 三目标及以上 AoE，以及 AoE QT 临时关闭。
- 野火队列中途目标丢失或队列未消费。
- 高难/日随切换、重载插件、旧配置文件加载。
- 停手、攒资源、优先 123 等 QT 联动。

## 11. AI 协作提示词模板

### 新增或调整技能逻辑

```text
请阅读 VIBE_CODING.md、PromeRotation_本体API总结.md 和相关 Resolver。
目标：<描述期望行为>。
约束：不要改变无关 Resolver 的优先级；补齐 CheckResult 原因；处理 null、等级同步、QT、编织窗口和队列防重入。
完成后编译 CNGL；若使用公共 SDK API，再编译 TC，并列出游戏内回归场景。
```

### 排查循环问题

```text
请只诊断，先不要修改代码。
现象：<实际表现>。
期望：<正确表现>。
上下文：<等级、目标数量、QT、资源、技能 CD、最近动作>。
请沿 MCHRotation 的 Resolver 注册顺序分析，指出第一个错误成功或错误失败的 Check()，并给出证据和最小修复方案。
```

### 增加配置项

```text
请按 VIBE_CODING.md 增加配置：<配置说明>。
判断它应属于 QT 还是 Settings，补齐默认值、模式归属、UI、持久化和旧配置兼容；不要把战斗算法写进 UI。
```

## 12. 当前维护重点

- 保持 CNGL 与 TC 双 SDK 构建兼容。
- 把 Resolver 顺序视为公开行为，任何调整都需要回归验证。
- 优先复用 `ApiHelper` 的中文语义封装；只有封装缺失时才直接调用 SDK，并补充通用封装。
- 对野火等组合队列重点验证重复排队、目标丢失和超时恢复。
- 新增功能时优先保证调试可解释性，再优化战斗细节。
