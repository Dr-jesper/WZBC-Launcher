# WZBC-Launcher v1.2.0-wzbc.3

Windows x64 预览版 · 2026-10-27 · 维护：Dr-jesper 与 Codex。

## 更新

- HTTP 客户端、认证请求、下载、更新检查及 DoH 统一使用 `WZBC-Launcher/1.2.0-wzbc.3`，移除自动附加的上游 Referer；保留自定义 User-Agent 与浏览器兼容模式。
- 纳入认证阶段提示、日志凭据脱敏、失效会话阻断、账号缓存隔离、跨重启拒绝状态及启动身份复核。
- 简化三语首页、关于页与许可索引，保留原许可、作者署名、赞助和第三方声明。
- 保留 WZBC、奶蛙配色及奶蛙黄，更新不重置主题。

## 使用

下载 `WZBC-Launcher-win-x64.zip`，校验对应 SHA-256，解压后运行同名 EXE。包含 .NET 运行时，启动 Minecraft 仍需 Java。此包为便携包，无安装向导，不附带 Minecraft、玩家账号或整合包。

Microsoft 登录当前不可用：自有应用尚未获得 Minecraft Services 批准。离线角色与第三方 Yggdrasil 身份不核验 Microsoft 游戏所有权。

## 验证

完整离线回归 413 项通过，相关请求头及认证回归 29 项通过。记录见 [VALIDATION.md](VALIDATION.md)。本版没有重新进行真实 Microsoft 授权及 Minecraft 启动、入服验收；ARM64 和干净系统未验收，EXE 未签名。

CI 只上传测试结果；此预览包由维护者授权手动发布。构建清单见 [BUILD.md](BUILD.md)，许可见 [LICENSING.md](../../LICENSING.md)。

**NOT AN OFFICIAL MINECRAFT PRODUCT. NOT APPROVED BY OR ASSOCIATED WITH MOJANG OR MICROSOFT.**

支持及安全联系：1175234170@qq.com。
