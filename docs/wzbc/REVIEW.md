# WZBC-Launcher 审核源码入口 / Review source index

更新：2026-10-25。维护者：Dr-jesper 与 Codex。公开支持与安全邮箱：1175234170@qq.com。

**NOT AN OFFICIAL MINECRAFT PRODUCT. NOT APPROVED BY OR ASSOCIATED WITH MOJANG OR MICROSOFT.**

当前审核源码版本为 **1.2.0-wzbc.3**，来自 SHOU-PCL main `842393281c5ed8bb5745efc37ce775bd0a21f4a1`。以当前访问的 Git 提交为审核对象；历史 Release `v1.2.0-wzbc.2` 不是该源码的构建产物。

## 状态与证据

| 检查项 | 当前事实 | 可核对入口 |
| --- | --- | --- |
| 应用资料 | Azure 主页、隐私、条款、徽标与内部维护备注已保存；个人 Microsoft 账户，公共客户端流启用 | [主页](../../README-EN.md)、[隐私](../../PRIVACY.md)、[条款](../../TERMS.md)；门户截图及私人配置不上传 |
| Microsoft 应用 | 自有公开 Client ID 为 `b42a22d0-3ca4-450d-8389-a466092f3ff7`；注册已完成，Minecraft Services 未获批准 | [申请状态](APPID-REVIEW.md) |
| 真实认证 | 2026-10-24 本人完成官方网页授权；OAuth、Xbox Live、XSTS 成功，Minecraft 换票 403；未到达权益与 Java profile | [验证边界](VALIDATION.md)；没有公开令牌、设备码或账号资料 |
| 错误与缓存 | 按失败阶段提示；明确 Microsoft 否定不能退回缓存成功；否定状态跨重启保留，重新认证成功后清除 | [ProfileService.Authentication.cs](../../PCL.Core/Minecraft/Profile/ProfileService.Authentication.cs)、[认证回归](../../PCL.Core.Test/Minecraft/Authentication) |
| 准入策略 | 所有现有启动入口共用实例及社团规则；沿用三类身份，不新增 Microsoft 绑定；启动前复核身份与会话 | [ACCOUNT-ADMISSION.md](ACCOUNT-ADMISSION.md) |
| 游戏所有权边界 | 仅 Microsoft 路径检查 Minecraft 权益；皮肤站及离线身份不因此证明游戏所有权，不能称全路径正版验证 | [策略](ACCOUNT-ADMISSION.md)、[待办](PENDING.md) |
| 自动测试 | 408 项完整本地离线回归通过；云端以对应提交的 WZBC Build 为准 | [Actions](https://github.com/Dr-jesper/WZBC-Launcher/actions/workflows/wzbc-build.yml)、[VALIDATION.md](VALIDATION.md) |
| 编译与发行 | 本地 Windows x64 Beta/Debug 通过；CI 内置自有公开 ID，BuildOnly 且仅上传测试报告；不发布 EXE/ZIP | [构建说明](BUILD.md)、[工作流](../../.github/workflows/wzbc-build.yml) |
| 继承许可 | 原许可文件不变；当前名称、主题及修改版分发的覆盖范围尚未确认 | [单独核对](PCL-LICENSE-REVIEW.md) |

## 本轮同步包含什么

本轮将此前仅在本地的认证 HTTP 阶段错误、安全提示、账号缓存隔离、失效状态保存、重新授权防覆盖、启动会话快照与统一入口策略同步到 GitHub，同时更新三语 README 和审核记录。Azure 条款来自维护者已确认的 `TERMS.md`。

本轮不修改原主题索引、不迁入 SHOU 新架构、不同步 PCL-CE、不更换认证端点，不借用其他启动器的 Client ID。只同步必要源码和项目文档，不上传 SDK、账号配置、密钥、日志、截图、数据库、旧备份历史或本地构建产物。

## 仍需完成

1. 如需保持当前名称、主题和修改版分发，补齐范围明确的继承许可依据；本轮核实不等于新授权。
2. Minecraft Services 授权，以及授权后 Java profile、刷新、重启恢复的真实验证。
3. 真实皮肤站认证、下载、整合包、游戏启动与入服；ARM64、干净 Windows 和签名。
4. 若再次申请，先核对当时的官方表单与受理规则；如实披露离线/第三方路径及既往拒绝情况。

**本轮没有提交新的 AppID 申请，也没有新发公开二进制。**

English: This source snapshot includes authentication error handling, cache isolation and shared launch admission, with offline tests and clearly stated real-account testing limits. Azure display details are complete. Minecraft Services approval, full real-account/game acceptance and inherited derivative-permission scope remain unresolved. Offline and third-party identities do not establish Microsoft game ownership. This synchronization is neither a new AppID submission nor a public binary release.
