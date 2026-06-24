# PromeRotation 本体 API 总结（详细注释版）

> 纯框架本体 API 参考。每个成员均注明：作用、返回含义、调用时机、注意事项。
> 仅描述 PR 本体对外可用的类型与成员，不含任何职业实现。

## 目录

- §1 Core — 玩家 / 目标 / 游戏状态
- §2 Data — 技能动作 / 设置 / 枚举
- §3 接口契约 — IRotation / IDecisionResolver / IOpener / 事件 / 元信息
- §4 Extensions — 角色 / 技能 扩展方法
- §5 Helpers — 全部辅助类
- §6 Managers — 循环 / 队列 / 战斗事件
- §7 Timeline — 时间轴节点系统
- §8 其它系统 — Network / GreenMove / TargetSelector / Service

---

## §1 Core（命名空间 `PromeRotation.Core`）

### Core —— 玩家与目标入口（静态类）

```csharp
static IBattleChara? Me
```
本地玩家对象。底层是 `Player.Object as IBattleChara`。
- **可能为 null**：未登录、读图、切场景的瞬间都会是 null。
- **每次用前判空**：`if (Core.Me == null) return new CheckResult(false, "玩家未加载");`
- 拿到后可接扩展方法：`Core.Me.HasStatus(id)`、`Core.Me.DistanceToMe()` 等。

```csharp
static IBattleChara? Target
```
本地玩家「当前选中」的目标。底层 `Svc.Targets.Target as IBattleChara`。
- **可能为 null**：没选目标、目标已消失时为 null。
- 这是玩家硬选的目标；不等于自动选怪结果（自动选怪见 §8 TargetSelector）。

```csharp
static void SetTarget(IBattleChara? target)
```
把游戏当前目标设为 target（相当于点选某个单位）。
- 当 `Me == null` 且 target 非 null 时，函数直接返回、不执行（防空指针）。
- 传 null 表示「取消选中」。
- 注意：会真实改变玩家选中目标，影响手动操作体验，慎用。

### GameData —— 游戏/战斗状态（静态类）

```csharp
static bool WillMove { get; set; }
```
「即将移动」标志。由移动系统写入，循环可读它来决定是否避免硬读条技能。一般你只读不写。

```csharp
static bool TpMove { get; set; }
```
TP（瞬移）移动标志。配合 GreenMove/瞬移系统使用。

```csharp
static bool IsInCombat()
```
是否处于战斗状态。底层读 `Svc.Condition[InCombat]`。返回 true=战斗中。

```csharp
static unsafe float GetCountdown()
```
当前倒计时剩余「秒数」。
- 正在倒计时→返回剩余秒（如 4.7）；没有倒计时→返回 0。
- 用于起手：倒计时进入某阈值时预读/预放技能。

```csharp
static unsafe uint Weather
```
当前天气 ID。某些副本机制/循环按天气切换时用。

```csharp
static bool IsPlayerOccupied()
```
玩家是否「被占用」而不能正常输出。
- 涵盖：不可选中、被击倒、过场动画、骑乘、**潜水/游泳/飞行**、区域切换、镶嵌魔晶石等所有占用态。
- 返回 true 时应**暂停出招**。循环里常作为总闸：`if (GameData.IsPlayerOccupied()) return null;`

```csharp
static unsafe uint GetBestPotionId()
```
返回当前职业「最该用」的爆发药物品 ID。
- 按职业自动选对应属性药（力/智/巧/意/刚），并按等级从高到低、**优先 HQ 再 NQ**。
- HQ 药返回值 = 原始ID + 1000000（游戏 HQ 约定）。
- 背包里一个都没有→返回 0。用前判断 `!= 0` 再排药。

```csharp
static float GetCurrentMeleeRange()
```
当前「近战」判定距离。
- 开了 `Hacks.IncreaseAttackRange`→返回 6；否则返回 3（游戏正常近战 3m）。

```csharp
static float GetCurrentAttackRange(float range)
```
传入技能基准距离，返回「校正后」的实际可用距离。
- 开扩程→返回 `range + 3`；否则原样返回。
- 用法：`var r = GameData.GetCurrentAttackRange(25); if (me.DistanceToMe() > r) ...`

```csharp
static bool IsIn120()
static bool IsInPure120()
```
是否处于「2 分钟团队爆发窗口」（场上有团辅 buff）。
- `IsIn120`：包含个人爆发药（刚力宝）在内的判定。
- `IsInPure120`：**排除**爆发药，只看真正的团辅 buff（战歌/连祷/鼓励等）。
- 用于「攒爆发资源对齐团爆」的逻辑。

### PlayerSnapshot
玩家状态的快照结构（某一时刻的属性集合）。具体字段按需查源码，一般循环直接用 `Core.Me` 即可。

---

## §2 Data（命名空间 `PromeRotation.Data`）

### PAction —— 一个「待执行技能」（resolver 的产出物）

```csharp
PAction(uint actionId, ActionType type, ActionTargetType target)
```
构造一个技能动作。三个必填参数：
- `actionId`：技能 ID。
- `type`：GCD / 能力技 / 道具 / 极限技（见下方 ActionType）。
- `target`：打谁（见下方 ActionTargetType）。

```csharp
readonly uint ActionId          // 技能 ID（构造后不可改）
readonly ActionType Type        // 技能类型
readonly ActionTargetType Target// 目标类型
```

```csharp
bool RequiresVerification { get; set; } = false
```
「需要命中校验」。主要用于**起手序列**：标记 true 时，框架会等这一步确认命中后才推进到下一步，防止序列因 miss/打空而错位。普通循环一般用不上。

```csharp
uint NetworkTid { get; set; } = 0
```
网络目标 ID。一般框架内部用，通常保持默认 0。

```csharp
bool IsLocationAction { get; set; } = false
Vector3 Position { get; set; } = Vector3.Zero
```
**地面放置类技能**专用（如黑魔黑洞、绘灵法师落点等）。
- `IsLocationAction = true` 表示这是个地面技能；
- `Position` 是落点坐标。
- 蝰蛇等无地面技能的职业用不到，保持默认即可。

### 枚举

```csharp
enum ActionType { Gcd, OffGcd, Item, LimitBreak }
```
技能类型。`Gcd`=占 GCD 的技能；`OffGcd`=能力技（不占 GCD，插在 GCD 间隙）；`Item`=道具（如爆发药）；`LimitBreak`=极限技。框架按 GCD/OffGcd 分两条队列分别决策。

```csharp
enum ActionTargetType {
    Self, Target, TargetOfTarget, FocusTarget, MouseOver,
    LowestHealthPartyMember, PartyMember2..8 }
```
技能目标类型。常用：`Self`=自己、`Target`=当前目标、`FocusTarget`=焦点目标、`MouseOver`=鼠标悬停、`LowestHealthPartyMember`=队伍血最少者（治疗用）、`PartyMember2..8`=小队第 N 人。框架用 `TargetResolver.Resolve()`（§3）把它解析成实际对象。

```csharp
enum AcrState { Off, Hold, On }
```
ACR 总开关状态。`On`=正常运行；`Hold`=暂停出招（保持但不放技能）；`Off`=关闭。

```csharp
enum SelectorModeType { None, Closest, Farthest, LowestHp, HighestHp,
    LowestHpIn3R, HighestHpIn3R, LowestHpIn6R, HighestHpIn6R }
```
自动选目标模式。`None`=不自动选（用玩家手选）；其余为「最近/最远/血最少/血最多」，带 `In3R/In6R` 后缀的限定在 3m/6m 范围内。由 `PromeSettings.TargetSelectorMode` 驱动（见 §8）。

### PromeSettings —— 全局设置（单例）

```csharp
static PromeSettings Instance { get; }
```
单例入口。所有设置/QT 都通过 `PromeSettings.Instance.xxx` 访问。

```csharp
HackSettings Hacks { get; }                  // 行为开关集合（见下）
AcrState EnableAcr { get; set; }             // ACR 总开关
bool OpenerHasBeenExecuted { get; set; }     // 本场起手是否已执行（脱战时复位）
SelectorModeType TargetSelectorMode { get; set; }  // 自动选目标模式
Dictionary<string,bool> QuickToggles { get; }      // 所有 QT 开关的字典（一般用下面方法读写）
```

```csharp
void AddQt(string key, bool defaultValue)
```
注册一个 QT 开关（战斗面板上的快捷开关）。一般在入口构造时把本职业所有 QT 注册进去。`defaultValue`=默认开/关。

```csharp
bool GetQt(string key)         // 读某 QT 当前是否开启
void SetQt(string key, bool value)  // 设某 QT（代码联动开关时用）
void ClearQts()                // 清空全部 QT
```

### HackSettings —— 行为开关（影响多处框架行为）

```csharp
bool IncreaseAttackRange        // 扩大攻击距离判定（影响 GameData.GetCurrent*Range）
bool ShowGroundCircle           // 显示地面落点圈
bool GroundCircleRainbow        // 落点圈彩虹色
bool NoActionMoveEnabled        // 无动作移动（边走边放）相关
bool GcdWaitForNetworkPacket    // GCD 等服务器回包确认后再走下一个（更稳，默认 true）
bool OgcdWaitForNetworkPacket   // 能力技同上
bool EnableUalLogs / EnableAepLogs   // UseActionLocation / ActionEffectParse 调试日志
bool DevMode                    // 开发者模式
bool ShowGreenMoveDebug         // 显示移动系统调试
bool GreenMoveUseGameQueue      // 移动走游戏队列
SerializableVector2 FloatingIconPosition   // 悬浮图标位置
```

### SerializableVector2
可序列化的二维向量（存设置用）。`float X, Y;`，与 `System.Numerics.Vector2` 可隐式互转。

---

## §3 接口契约（`PromeRotation.Rotation` / `PromeRotation.Resolvers`）

### IRotation —— ACR 入口接口

每个职业 ACR 的主类实现它，并在类上加 `[RotationMetadata(jobId, "名字")]`。框架靠它驱动整个循环。

```csharp
string RotationName { get; }   // ACR 显示名（应与 RotationMetadata 里的名字一致）
uint   JobId { get; }          // 职业 ID
```
```csharp
PAction? NextGcd()
```
框架每帧调用，问「现在该放哪个 GCD」。返回要放的技能；没有则返回 null。**这是 GCD 循环的核心出口**——内部一般遍历 GCD resolver 列表，返回第一个条件满足的。
```csharp
PAction? NextOffGcd()
```
同上，但问「该插哪个能力技」。GCD 间隙被框架调用。
```csharp
void UpdateDebugStatus()
```
刷新调试面板数据。一般遍历所有 resolver，把各自 `Check()` 的成功/失败原因写进 `RotationManager.GcdSolverStatus / OffGcdSolverStatus`，方便在面板上看「为什么没放某技能」。
```csharp
IOpener? GetOpener()
```
返回当前应使用的起手对象；无起手返回 null。框架在倒计时/进战时调用。
```csharp
IRotationEventHandler GetEventHandler()
```
返回本 ACR 的事件处理器（见下 IRotationEventHandler）。
```csharp
void DrawSettings()   // 画本职业的设置界面（ImGui），可留空
void DrawQTs()        // 画本职业的 QT 开关界面（ImGui），可留空
```

### IDecisionResolver —— 决策单元（一个技能一个）

```csharp
CheckResult Check()    // 判断「现在能不能/该不该放这个技能」
PAction     GetAction()// Check 通过后，返回具体要放的 PAction
```
设计模式：每个技能（或一组判断）写一个 resolver。框架先调 `Check()`，`Success==true` 才调 `GetAction()` 取技能。**列表中靠前的优先级更高**。

```csharp
struct CheckResult(bool Success, string Message)
```
- `Success`：条件是否满足。
- `Message`：原因说明，会显示在调试面板。**失败分支务必写清原因**（如「过远」「无目标」「CD中」），排查极方便。

### IOpener —— 起手

```csharp
string OpenerName { get; }
```
起手名称（多套起手时用于区分/选择）。
```csharp
void InitializeCountdown(CountDownHandler countdownHandler)
```
倒计时阶段登记技能。在这里用 `countdownHandler.AddAction(...)` 挂「剩余 X 毫秒时放某技能」。
```csharp
List<PAction> InCombatSequence { get; }
```
开怪后的固定起手序列，框架按顺序逐个执行。需要严格命中校验的步骤把 `PAction.RequiresVerification` 设 true。

### IRotationEventHandler —— 事件回调（全部同步 void，无 async）

```csharp
void OnUpdate()             // 每帧调用，无论是否战斗
void OnOutOfBattleUpdate()  // 仅非战斗状态每帧
void OnBattleStarted()      // 进入战斗那一瞬（只触发一次）
void OnBattleUpdate()       // 战斗中每帧
void OnBattleEnded()        // 脱战 / 团灭 / 通关时；常在此复位战斗状态与起手标志
void OnTerritoryChanged(ushort territoryId)  // 切换地图区域时；通常也做复位
```
要感知「自己刚放了什么技能」（即其它框架的 AfterSpell），在 `OnUpdate` 里订阅 `CombatEventManager.OnActionEffect`（见 §6）。

### IRotationMeta —— 元信息（入口类用 static 实现）

```csharp
static abstract IReadOnlyDictionary<string,bool> QtList { get; }   // 对外公开的 QT 列表（名→默认值）
static abstract IReadOnlyDictionary<string,Type> Openers { get; }  // 对外公开的起手模板（名→类型）
```
框架反射读取这两个静态成员，用来生成 QT 面板和起手下拉选单。

### CountDownHandler —— 倒计时技能登记器

```csharp
void AddAction(int timeRemainingMs, PAction action)        // 倒计时剩余 X 毫秒时放「固定」技能
void AddAction(int timeRemainingMs, Func<PAction> factory) // 剩余 X 毫秒时「实时求解」技能（factory 返回 null 则跳过）
```
用于起手对齐倒计时。`factory` 版适合「到点再根据当时状态决定放什么」。

### RotationMetadataAttribute

```csharp
[RotationMetadata(uint jobId, string rotationName)]
```
标注在 ACR 入口类上，告诉框架这是哪个职业、叫什么。`rotationName` 为空会抛异常。

### TargetResolver —— 目标类型解析（静态）

```csharp
static IBattleChara? Resolve(ActionTargetType targetType)
```
把 `ActionTargetType` 枚举解析成实际角色对象：`Self`→Me、`Target`→当前目标、`FocusTarget`/`MouseOver`、`PartyMember2..8`→小队第 N 人。未实现的类型返回 null。

---

## §4 Extensions（命名空间 `PromeRotation.Extensions`）

### BattleCharaExtensions —— 角色扩展（`this IBattleChara`）

```csharp
bool HasStatus(uint statusId)
```
该角色身上是否有指定 buff/debuff。判断增益/减益最常用。
```csharp
int GetStatusStackCount(uint statusId)
```
指定 status 的层数（如毒层数、连击层数）。没有该 status 时一般为 0。
```csharp
float GetStatusLeftTime(uint statusId)
```
指定 status 的剩余秒数。用于「buff 快掉了就刷新」的判断；没有则为 0。
```csharp
float DistanceToMe()
```
该角色到「本地玩家」的距离（米）。判断攻击距离：`if (target.DistanceToMe() > range) ...`。
```csharp
Vector2 PositionXY()
```
该角色的平面坐标（忽略高度 Y）。算身位/距离用。
```csharp
bool IsPlayer()
```
该角色是否为玩家（真人/队友），用于排除「目标是玩家」的情况。
```csharp
bool IsEnemy()
```
该角色是否为敌对单位（可攻击的怪）。
```csharp
bool CanUseAttackActionOn()
```
能否对该角色使用攻击技能（综合可选中/敌对/距离等判断）。

### SkillExtensions —— 技能扩展（`this uint actionId`）

```csharp
float GetActionCooldown()        // 该技能剩余冷却（秒），0=好了
uint  GetAdjustedActionId()      // 取「连击替换/变招」后当前实际该按的技能 ID（连击系统关键）
float GetActionCharges()         // 当前可用充能层数（带充能的技能，如疾跑）
float GetActionRecastTime()      // 该技能的总复唱时间（秒）
float GetActionRecastTimeElapsed()// 复唱已过去的时间（秒）
bool  IsActionHighlighted()      // 该技能图标当前是否「高亮」（游戏提示可用/连击就绪）
```
`GetAdjustedActionId()` 特别有用：按一个起始技能 ID 就能拿到「当前连段下该按的那个」，无需自己写连击状态机。

---

## §5 Helpers（命名空间 `PromeRotation.Helpers`）

### ActionHelper —— GCD 与技能时间（放技能时机的核心）

```csharp
float GetGcdTotal()      // 当前 GCD 总时长（秒，受技速影响）
float GetGcdElapsed()    // 当前 GCD 已过去时间（秒）
float GetGcdRemain()     // 当前 GCD 剩余时间（秒）——判断能否插能力技的关键
bool  GetGcdIsActive()   // GCD 是否正在转
```
```csharp
float GetAnimationLock() // 当前动画锁剩余（秒）。锁住时不能出招
void  SetAnimetionLock() // 手动设置动画锁（特殊场景，谨慎用）
```
```csharp
float GetComboLeftTime() // 连击剩余时间（秒），快到 0 表示连击要断
uint  GetLastComboID()   // 上一个连击动作 ID（判断连段进行到哪一步）
```
```csharp
float GetCastTimeTotal()    // 当前读条总时长
float GetCastTimeElapsed()  // 读条已过去
float GetCastTimeRemain()   // 读条剩余
```
```csharp
float GetActionCooldown(uint id)          // 指定技能剩余冷却（秒）
float GetActionCharges(uint id)           // 指定技能当前充能数
float GetActionRecastTime(uint id)        // 指定技能总复唱
float GetActionRecastTimeElapsed(uint id) // 指定技能复唱已过
int   GetMaxCharges(uint id)              // 指定技能最大充能数
uint  GetAdjustedActionId(uint id)        // 连击/变招后实际技能 ID
bool  IsActionHighlighted(uint id)        // 图标是否高亮
```
```csharp
void UseActionLocation(uint id, ulong targetId, Vector3 pos)
```
直接施放「地面放置技能」到指定坐标。`targetId`=目标对象 ID，`pos`=落点。
```csharp
Vector3 GetCursorPosition()
```
当前鼠标在世界中的坐标（给地面技能取落点用）。

### CastHelper —— 读条辅助

```csharp
int GetActionCast100Ms(uint id)   // 指定技能读条时长（单位 100 毫秒）
double GetAdjustedRemainingCastMs()// 校正后的剩余读条毫秒
bool ShouldWaitForCast(DateTime startUtc, int allowedDelayMs)// 是否该等读条完成（防打断/滑步）
```

### StatusHelper —— buff 查询（非扩展写法）

```csharp
bool  HasStatus(IBattleChara c, uint id)        // c 是否有某 status
int   GetStatusStack(IBattleChara c, uint id)   // 层数
float GetStatusLeftTime(IBattleChara c, uint id)// 剩余秒
```
和 §4 的扩展方法等价，只是写法不同（这里把角色当参数传）。

### TargetHelper —— 目标 / 身位 / 范围

```csharp
Positional GetTargetPositional()              // 玩家相对当前目标的身位（正面/侧面/背面）
bool HasPositionalRequirement(IBattleChara c) // 目标是否有身位需求
Vector3 GetStancePoint(IBattleChara t, Positional s)        // 取「站到某身位」的落点坐标
Vector3 GetStancePoint_Center(IBattleChara t, Positional s) // 同上(以中心算)
uint EnemyIn5m()                              // 5 米内敌人数量
uint EnemyInRange(float range)                // 指定半径内敌人数量（判断是否该用 AOE）
uint EnemyInRangeTarget(IBattleChara? t, float range)// 以 t 为中心指定半径内敌数
IBattleChara? FindNpcByBaseId(uint baseId)    // 按 NPC 基础 ID 找一个单位
List<IBattleChara> FindNpcsByBaseId(uint baseId)// 找全部匹配单位
bool IsAllBossUntargetable()                  // 所有 Boss 是否都不可选中（转场/无敌阶段）
```

### PartyHelper —— 小队

```csharp
List<IBattleChara> GetParty()    // 当前小队成员（含自己）
List<IBattleChara> GetUIParty()  // 按 UI 列表顺序的小队成员
```

### PosHelper / MathHelper —— 坐标与数学

```csharp
bool PosHelper.IsWithinRange(Vector3 cur, Vector3 tgt, float range, bool ignoreY=true)
// 两点是否在 range 内；ignoreY=true 时忽略高度差（默认）
Vector3 PosHelper.ScreenToWorld()  // 屏幕（鼠标）坐标转世界坐标
double  MathHelper.θ(x1,y1,x2,y2)            // 两点连线角度
double  MathHelper.θVector2(Vector2 a, Vector2 b)// 同上(向量版)
double  MathHelper.NormalizeAngle(double a)  // 角度归一化
Vector3 MathHelper.Round(this Vector3 v, int dot)// 坐标四舍五入到指定小数位
```

### ItemHelper / LimitBreakHelper —— 道具与极限技

```csharp
uint ItemHelper.GetItemCountInInventory(uint id, bool isHq)// 背包中某物品数量（区分 HQ）
uint LimitBreakHelper.GetLimitBreakCharge()    // 当前极限技充能档位
uint LimitBreakHelper.GetLimitBreakActionId()  // 当前可用的极限技技能 ID
```

### JobGaugeHelper —— 职业量谱

```csharp
JobGaugeHelper.<JOB>.*
```
每个职业一个静态嵌套类（如 BRD/WHM/SGE/DRK/VPR…），里面是该职业专属量谱属性（歌姬的歌、贤者的蓝量、武士的剑气等）。**各职业属性名不同**，用前查源码对应段确认确切名称。

### HintHelper / SoundHelper / IconHelper —— 提示 / 音效 / 图标

```csharp
void HintHelper.ShowToast2(string text, float sec, HintType type)// 屏幕弹出提示文字
void SoundHelper.PlaySoundEffect(uint id)        // 播放游戏音效
void SoundHelper.PlayChatSoundEffect(uint id)    // 播放聊天提示音
// IconHelper（取 UI 贴图，画面板用）：
GetGameIcon/GetActionIcon/GetStatusIcon/GetJobIcon(uint) → IDalamudTextureWrap?
GetActionIconId/GetStatusIconId(uint) → uint?
GetDamageTypeIcon(DamageIconType) → IDalamudTextureWrap?
```

### HackHelper —— 移动 / 瞬移

```csharp
void TeleportNormal(Vector3 position)  // 瞬移到指定坐标
void TeleportToMouse()                 // 瞬移到鼠标位置
async Task TeleportWithReturn(Vector3 posA, Vector3 posB, int delayMs)
// 瞬移到 A，停留 delayMs 毫秒后再回到 B（机制位移/打断后归位等）
```
注意：瞬移类操作风险高、易被检测，使用需谨慎且确保场景允许。

### IdHelper / UiHelper / CachePathHelper / 其它

```csharp
ulong IdHelper.GetAccountID()       // 账号 ID
string IdHelper.GetWorldIdName()    // 服务器名
string IdHelper.GetHWID()           // 硬件标识
void UiHelper.PulseActionBar(uint actionId)// 让技能栏对应技能闪烁提示
void UiHelper.SetInventoryUiLocal(InventoryType t, int slot, uint itemId, uint qty, bool hq)// 本地改背包 UI 显示
string CachePathHelper.GetAcrCacheRoot()/EnsureAcrCacheRoot()/...// ACR 缓存目录路径
void CleanCacheHelper.CleanupStaleCacheDirectories()// 清理过期缓存
// HealerHelper：治疗职业专用辅助，按需查源码
```

---

## §6 Managers（命名空间 `PromeRotation.Managers`）

### RotationManager —— 循环总管（静态）

```csharp
List<SolverStatus> GcdSolverStatus      // GCD resolver 调试状态列表（入口 UpdateDebugStatus 填）
List<SolverStatus> OffGcdSolverStatus   // 能力技 resolver 调试状态列表
IRotation? GetCurrentRotation()         // 当前加载的 ACR
void LoadRotationForJob(uint jobId)     // 为某职业加载 ACR
IReadOnlyDictionary<string,bool>? GetQtListByJob(int jobId)// 取某职业 QT 列表
IReadOnlyDictionary<string,bool>? GetCurrentQtList()       // 当前职业 QT 列表
IReadOnlyDictionary<string,Type>? GetOpenersByJob(int jobId)// 某职业起手模板（起手下拉来源）
IJobNodeProvider? GetJobNodeProvider(int jobId)// 某职业的时间轴节点提供者
void Tick(IFramework framework)         // 每帧驱动（框架内部调）
void Initialize() / Dispose()
```
```csharp
struct SolverStatus { string Name; bool Success; string Message; }
```
单条 resolver 的调试状态：名字 + 是否满足 + 原因。入口 `UpdateDebugStatus()` 里逐个填充，面板据此显示。

### ActionQueueManager —— 技能队列（静态，直接排技能）

```csharp
void Enqueue(PAction action, bool isHighPriority=false)        // 入队一个技能
void Enqueue(List<PAction> actions, bool isHighPriority=false) // 入队一组
void EnqueueOffGcdList(List<PAction> actions, bool isHighPriority=false)// 入队一组能力技
void ClearNormalQueues()    // 清普通队列
void ClearAllQueues()       // 清全部队列
bool HasActionsInQueue()    // 队列里是否有技能
bool HasHighPriorityAction()// 是否有高优先技能
bool HasActionsInGcdQueue() / HasActionsInOffGcdQueue()// 分别查 GCD/能力技队列
Dictionary<string,List<string>> GetQueueStatus()// 取队列状态（调试用）
```
`isHighPriority=true` 会插到队首优先执行。起手序列、强制排招等场景用得到。

其它管理器：`ActionFlowManager`（动作流）、`ActionGroupCommand`（动作组）、`CameraSyncManager`（镜头同步）。

### CombatEventManager —— 战斗事件（静态，订阅式）

感知战斗中发生的事（谁放了技能、谁加了 buff、谁被点名等）。**订阅对应事件**即可。

```csharp
static event Action<CombatEvent>?          OnEvent;          // 所有事件总入口
static event Action<StartCastEvent>?       OnStartCast;      // 某单位开始读条
static event Action<ActionEffectEvent>?    OnActionEffect;   // 技能命中生效（最常用）
static event Action<ActionEffectEntryEvent>? OnActionEffectEntry;
static event Action<CreateObjectEvent>?    OnCreateObject;   // 召唤物/物件生成
static event Action<NpcYellEvent>?         OnNpcYell;        // NPC 喊话
static event Action<ActorControlEvent>?    OnActorControl;   // 通用单位控制事件
static event Action<ActorControlAddStatusEvent>?    OnActorControlAddStatus;    // buff 添加
static event Action<ActorControlRemoveStatusEvent>? OnActorControlRemoveStatus; // buff 移除
static event Action<ActorControlTargetIconEvent>?   OnActorControlTargetIcon;   // 头顶点名图标
static event Action<ActorControlTetherEvent>?       OnActorControlTether;       // 连线
static event Action<ActorControlCancelCastEvent>?   OnActorControlCancelCast;   // 读条被打断
```

`ActionEffectEvent` 主要字段：
```csharp
uint SourceId       // 施放者 ID（判断是不是自己：== Core.Me.EntityId）
uint ActionId       // 施放的技能 ID（判断「刚放了什么」）
uint GlobalSequence float AnimationLockTime Vector3 SourcePos
IReadOnlyList<ActionEffectTarget> Targets   // 命中的目标们
IReadOnlyList<EffectEntry> Effects          // 效果条目（伤害/治疗/上 buff 等）
```
典型用法（感知「自己刚放了某技能」→ 推进自定义状态）：
```csharp
CombatEventManager.OnActionEffect += e => {
    if (e.SourceId != Core.Me?.EntityId) return;   // 只关心自己
    // 根据 e.ActionId 处理
};
```

---

## §7 Timeline 时间轴节点系统（`PromeRotation.Timeline` / `PureTimeline`）

ACR 可在时间轴编辑器里用「条件 + 动作」节点编排副本特定时机的行为，并可注册自定义节点。

```csharp
ActionFactory.Register(string key, Func<ActionDto, IAction> creator)
ConditionFactory.Register(string key, Func<ConditionDto, ICondition> creator)
```
注册自定义「动作 / 条件」节点。`key`=节点标识，`creator`=根据配置 Dto 造出实例。在 ACR 启动时注册。

```csharp
interface IAction { void Execute(); }
interface ICondition { bool EvaluateImmediate(); bool EvaluateWait(); }
```
- `IAction.Execute()`：节点触发时执行的动作。
- `ICondition`：`EvaluateImmediate()` 立即判定；`EvaluateWait()` 等待式判定（持续等到满足）。

**内置动作节点**（直接在编辑器里用，无需写代码）：
`TriggerQtAction`（开关 QT）、`BatchTriggerQtAction`（批量开关 QT）、`EnqueueSkillAction`（排入技能）、`ForceUseSkillAction`（强制放技能）、`EnqueueLocationAction` / `ForceUseLocationAction`（地面技能）、`SetTargetAction`（设目标）、`SetTargetSelectorModeAction`（设自动选怪模式）、`HeadingControlAction`（朝向）、`GreenMoveToPositionAction`（移动到点）、`TeleportToPositionAction`（瞬移到点）、`ExecuteCommandAction`（执行命令）、`ClearAllQueuesAction`（清队列）、`CustomLogAction`（打日志）。

**内置条件节点**：
`SkillCooldownCondition`（技能 CD）、`HasBuffFriendlyCondition`（有无 buff）、`BuffTimeFriendlyCondition`（buff 剩余）、`CountdownCondition`（倒计时）、`InCombatCondition`（是否战斗）、`PlayerPositionCondition`（玩家位置）、`CastStartCondition`（开始读条）、`ActionEffectCondition`（技能命中）、`TargetSelectableCondition`（目标可选中）、`ChatLogCondition`（聊天日志）、`WeatherCondition`（天气）。

---

## §8 其它系统

### Network / Protocol（封包协议层，`PromeRotation.Network`）

```csharp
// OpcodeMap —— opcode ↔ 语义(OpcodeKind) ↔ PacketId 三向映射
Dictionary<ushort,OpcodeKind> OpcodeToKind     Dictionary<OpcodeKind,ushort> KindToOpcode
Dictionary<OpcodeKind,PacketId> KindToPacketId
bool TryGetKind(ushort opcode, out OpcodeKind kind)
bool TryGetOpcode(OpcodeKind kind, out ushort opcode)
bool TryGetPacketId(OpcodeKind kind, out PacketId packetId)
static OpcodeMap BuildFromDumper(OpcodeDumper dumper)
// OpcodeDumper —— 运行时从游戏 dump opcode
void Init() / Dispose()    int Opcode(PacketId id)    PacketId ID(int opcode)
void PrintPacketIdToOpcodeToChat()  // 把 PacketId→opcode 映射打到聊天框
void PrintOpcodeKindMapToChat()     // 把 opcode 语义映射打到聊天框
```
配套：`PacketId`/`OpcodeKind`（枚举）、`ActorControlId`、`PacketStructs`（封包结构体）、`NetworkHook`/`NetworkDispatcher`（hook 与分发）、`CombatEventBridge`（把封包转成 §6 战斗事件）。

### NetworkVerifier（联网校验，`PromeRotation.NetworkVerifier`）

服务器侧授权 + 客户端完整性校验机制。主要类型：`NetworkVerifierManager` / `VerificationManager`（主控）、`ServerSignatureVerifier`（服务器签名校验）、`StateIntegrityGuard`（状态完整性守卫）、`TamperStateReporter`（篡改上报）、`HeartbeatScheduler` / `SeqAllocator` / `VerifierStateMachine`（会话）、`AuthTransportClient` / `BcTlsSupport`（加密传输）。一般 ACR 不直接调用。

### GreenMoveSystem（移动 / 寻路，`PromeRotation.GreenMoveSystem`）

```csharp
interface IGreenMoveSystem        // 移动系统接口
```
配套：`GreenMoveManager`（管理）、`GreenMovementInputController`（输入控制）、`GreenMoveCommands`、`GreenMovePoint`（路径点）、`GreenMoveStatus`、`GreenMoveIpc`（跨插件）、`GreenMoveDebugSnapshot/UI`（调试）。与 `HackSettings.GreenMoveUseGameQueue / ShowGreenMoveDebug`、`GameData.WillMove/TpMove` 联动。

### TargetSelector（自动选目标，`PromeRotation.TargetSelector`）

```csharp
// TargetSelectorService（静态）
void SetMinUpdateInterval(int ms)  // 设最小刷新间隔（节流）
void Update()                      // 刷新一次自动选目标
void Clear()                       // 清除
uint EnemyInRange(float range)     // 范围内敌数
```
由 `PromeSettings.TargetSelectorMode`（§2 SelectorModeType）驱动。配套：`TargetInfo`、`TargetSelectorController`、`TargetSelectorHelper`。

### Service（`PromeRotation.Service`）

```csharp
GcdWatchdogService { void Initialize(); void Dispose(); }// GCD 看门狗（监控 GCD 异常）
interface ISpeedMultiplierService : IDisposable          // 变速服务接口
SpeedMultiplierService                                   // 变速实现（游戏速度倍率）
```

---
> 仅含 PR 本体 API。需要某成员的完整实现细节时再单独查对应源码文件。




