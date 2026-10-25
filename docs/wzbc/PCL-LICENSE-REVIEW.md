# PCL 继承许可：名称、主题与分发方式核对

核对日期：2026-10-25。对象是 WZBC 实际继承的代码和当前产品，不以“仓库公开”或 GitHub 的 Apache 标签代替授权范围判断。本文是基于所列文件的项目核对记录，不对指南的司法效力作结论。

## 结论

**现有可核对依据尚不足以确认覆盖当前 WZBC 名称、紫色/奶蛙主题及修改版二进制分发。** 这不是已经确认侵权的判决，也不是 Mojang 拒绝原因的认定。

- 继承的正式许可授予的是符合免费条件的原样副本分发，没有看到对当前定制修改版的普遍修改、再分发授权。
- 附带指南对重度衍生使用给出条件，并作出有条件的不追责承诺；不能忽略这项承诺，也不能把它写成无条件、可再许可的 Apache 授权。WZBC 当前名称及非蓝色主题不符合其中明确列出的条件。
- SHOU 的定制许可或其自身的开源声明，只能在其有权授权的范围内适用；目前未取得覆盖这些 PCL 条件差异的单独依据。
- 还核对了 PCL 当前的 McLaunchPrecheck：其中文购买提示覆盖非 Microsoft 登录；WZBC 继承的购买提示条件排除了 Authlib/Yggdrasil Connect。该分支存在实际差异，不能仅以“保留 SHOU 行为”认定满足指南的劝导条件。

## 版本与证据

| 证据 | 可复核对象 | 本轮发现 |
| --- | --- | --- |
| 实际继承基线 | [SHOU main 842393281c5ed8bb5745efc37ce775bd0a21f4a1 的 LICENCE](https://github.com/Conduit-Club/SHOU-Launcher/blob/842393281c5ed8bb5745efc37ce775bd0a21f4a1/Plain%20Craft%20Launcher%202/LICENCE) | 正式分发许可全文与合理使用指南同时存在 |
| 当前 WZBC 文件 | [本地保留的 LICENCE](../../Plain%20Craft%20Launcher%202/LICENCE) | Git blob `2613d200fdc193c9219e6d0d476a6a27c92eb9d5`，与继承基线逐字一致 |
| SHOU 当前 main | [c8c0a75229acac7e3fc1e156d2b14a0dd24ba3d7 的 LICENCE](https://github.com/Conduit-Club/SHOU-Launcher/blob/c8c0a75229acac7e3fc1e156d2b14a0dd24ba3d7/Plain%20Craft%20Launcher%202/LICENCE) | 同一 blob，未新增针对 WZBC 的名称、主题或分发例外 |
| PCL 当前仓库 | [93ede5d9e638c99fcec853a9ba1eecd91ceee7f5 的 LICENCE](https://github.com/Meloong-Git/PCL/blob/93ede5d9e638c99fcec853a9ba1eecd91ceee7f5/LICENCE) | Git blob `ae3dd050edaf0c4f2bf50bf7b4d8d508319394f1`。正式许可改为外部链接；指南仍保留名称、署名、购买/赞助提示与蓝色色相条件 |
| PCL 链接的正文 | [PCL 法律声明：分发有限许可](https://shimo.im/docs/rGrd8pY8xWkt6ryW#anchor-X0bo) | 本轮在公开网页实际读取。当前费用条件改为不以软件本身为收费主体，并说明独立内容/服务与一般平台收入例外；授权对象仍为原样副本，仍含不得再许可的限制 |
| PCL 当前购买/赞助行为 | [同一 PCL 提交的 ModLaunch.vb / McLaunchPrecheck](https://github.com/Meloong-Git/PCL/blob/93ede5d9e638c99fcec853a9ba1eecd91ceee7f5/Plain%20Craft%20Launcher%202/Modules/Minecraft/ModLaunch.vb) | 与当前 WZBC 的对应函数实际阅读比对；第三方登录的购买提示条件存在差异 |
| SHOU 来源记录 | [HISTORY.md](../conduit/HISTORY.md) | 记载 PCL-CE 来源与快照迁移；历史重写和独立维护没有变更继承代码权利 |

外部正文为可变网页，不能把其中当前的费用表述当成 WZBC 继承时已有的相同文本。本轮不替换继承的原许可，也不将费用条件变化推导为新增修改版授权。文档顶部用户协议的修订日期不作为分发许可单独的版本日期。

## 逐项对应

| 项目 | 已核对条件 | WZBC 当前事实 | 判断与后续 |
| --- | --- | --- | --- |
| 使用程度 | 指南将基于仓库修改、实现启动/下载等实质功能列为重度使用 | 保留完整 Windows 启动器、游戏下载及模组管理，非仅引用小段独立代码 | 按指南的重度使用检查，不能套用轻度使用条件 |
| 名称 | 重度使用名称需以 PCL 标识开头，并清楚体现第三方修改身份 | 应用、仓库与 Azure 显示 WZBC-Launcher | 当前名称不符合所列前缀条件；单独依据未确认，不自动改名 |
| 主题 | Windows 版不得呈现类似赞助解锁的功能或表现；指南明确举出主要蓝色色相 | 默认 WZBC 品牌紫 `#601986`；另有奶蛙、奶蛙黄主题（索引 6/7） | 不能确认非蓝色主题在承诺范围内；只把默认项改蓝也不能证明其他主题选项已解决，本轮保留用户选择 |
| 非官方身份 | 明确第三方独立二创 | 三语 README、关于页与 TERMS 明确独立身份和维护者 | 已有声明；声明本身不替代其他条件 |
| 署名与赞助 | 关于页首位显示龙腾猫跃及赞助链接，公开源码并保留指南 | 关于页第一行保留原作者与赞助入口，许可原文保留 | 代码/资源层面确认存在；不据此认定整个授权链已完成 |
| 购买与赞助劝导 | 与 PCL McLaunchPrecheck 一致或更严格 | Beta 赞助节点、购买及试玩提示保留；但 WZBC 购买条件排除 Authlib/Yggdrasil Connect，PCL 当前中文非 Microsoft 提示未这样排除。Debug 跳过提示，赞助段受 BETA 编译条件控制 | 已确认存在分支差异；正式分发前应核对目标构建全部分支或取得相应范围的许可，不能声称该项已完整满足 |
| GitHub 公开修改源码 | 正式许可的原样副本授权不自动涵盖全部修改；指南要求衍生源码公开 | 本轮同步审核源码，保留来源及全部声明 | 满足“可审阅”不等于独立的修改与再分发许可证明；命名/主题差异仍需范围明确的依据 |
| 新的 EXE/ZIP | 修改版不属于未经改变的原样副本；免费或无广告不消除这个差异 | 新的公开二进制发行暂缓，CI 仅编译和上传测试结果 | 继续暂缓，不以本地 BuildOnly、代码签名或 AppID 申请代替依据 |
| 历史预览包 | 发布过不证明已经获得许可 | v1.2.0-wzbc.2 仍是旧预览，不含当前修复 | 如实标为历史；本轮不删除、重上传或声称已撤回旧包 |

产品证据：[关于页](../../Plain%20Craft%20Launcher%202/Pages/PageSetup/PageSetupAbout.xaml)、[主题](../../PCL.Core/UI/Theme)、[启动前检查](../../Plain%20Craft%20Launcher%202/Modules/Minecraft/ModLaunch.cs)、[构建工作流](../../.github/workflows/wzbc-build.yml)。

## 可以怎样闭合依据

若保留当前品牌和主题，需要有权授权者可核对的说明，明确涉及的 PCL 版本/代码范围、WZBC 名称、非蓝色及可选主题、修改后源码与 Windows EXE/ZIP 的公开提供，以及适用的费用、署名和下游条件。若由 SHOU 提供，应能说明其自身有权许可这些继承部分的依据和范围，而非只声明允许改动 SHOU 自有部分。

另一个方向是核实并满足适用指南的条件；这涉及实际命名、主题和劝导行为，不是只补一段 README。本轮没有替维护者选择改名、删除主题或变更发布方式，也没有联系原作者、发送邮件或代签协议。

## 与审核源码同步的关系

许可核对单独记录在本文。GitHub 同步同时公开已验证的认证错误处理、缓存隔离和统一账号准入，以及 Azure 展示资料完成状态。沿用上游的离线/第三方路径仍不证明 Microsoft 游戏所有权；本轮不以资料同步声称满足 AppID 全部要求或发起新申请。
