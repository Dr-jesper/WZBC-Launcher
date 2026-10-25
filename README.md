[简体中文](README.md) | [English](README-EN.md) | [繁體中文](README-ZH_TW.md)

# WZBC-Launcher

**NOT AN OFFICIAL MINECRAFT PRODUCT. NOT APPROVED BY OR ASSOCIATED WITH MOJANG OR MICROSOFT.**

本项目是基于 SHOU-PCL / PCL-CE / PCL 的独立第三方启动器，由 **[Dr-jesper](https://github.com/Dr-jesper) 与 Codex** 共同维护，维护者对发布和支持负责。校名用于说明社群背景，本项目未声称获得温州商学院官方背书。

**项目支持及安全联系：[1175234170@qq.com](mailto:1175234170@qq.com)** · [隐私说明](PRIVACY.md) · [使用条款](TERMS.md) · [安全报告](SECURITY.md)

> **混合许可，不能将整个仓库视为 Apache-2.0 项目。** PCL 界面与启动器目录受单独的有限许可及附带指南约束，其他代码和第三方素材按各自许可处理。[许可范围与未解决事项](LICENSING.md)。
>
> **新的二进制公开发行暂缓。** 名称、主题和分发方式的单独核对集中在[许可核对记录](docs/wzbc/PCL-LICENSE-REVIEW.md)。

<img src="Plain%20Craft%20Launcher%202/Images/Wzbc/club-logo.png" alt="WZBC 社团标志" width="80" height="80">

WZBC 社群启动器保留账号管理、游戏下载与实例安装、模组依赖和启动能力，加入品牌紫、Logo、校园复建轮播、官网、皮肤站和社团服务器入口。WZBC、奶蛙配色、奶蛙黄均支持深浅模式，更新保留已选主题。此描述是功能范围，不是全部功能已真实验收的声明。

## 当前状态（2026-10-25）

- Microsoft 应用注册和 Azure 主页、隐私、条款、徽标资料已完成；**Minecraft Services 授权尚未获得**。两轮申请未通过，具体原因未确认。自有 Client ID 实测到 XSTS 成功，Minecraft 换票为 HTTP 403。
- [统一账号准入](docs/wzbc/ACCOUNT-ADMISSION.md)沿用上游三条身份路径，不新增微软绑定。微软路径检查游戏权益；皮肤站和离线角色不证明 Microsoft 游戏所有权，不能据此声称满足 AppID 全部要求。
- 已有历史预览版为 [v1.2.0-wzbc.2](https://github.com/Dr-jesper/WZBC-Launcher/releases/tag/v1.2.0-wzbc.2)。它不包含本轮源码修复，不应被视为已完成整改或已通过 Mojang 审核。
- 当前审核源码版本为 1.2.0-wzbc.3：认证阶段错误提示、失效会话阻断、账号缓存隔离和启动身份复核已纳入。408 项本地离线回归通过；Windows x64 Beta/Debug 编译通过。Actions 使用自有公开 Client ID 进行 BuildOnly，仅上传测试报告。
- 完整真实登录、刷新、游戏下载、Minecraft 启动和入服仍待验收。[审核版本入口](docs/wzbc/REVIEW.md)集中列出证据与剩余工作；同步源码不代表发起新一轮申请。

[构建说明](docs/wzbc/BUILD.md) · [定制说明](docs/wzbc/CUSTOMIZATION.md) · [社团官网](https://wzbcmc.cn/) · [皮肤站](https://skin.wzbcmc.cn/)

发行包不附带 Minecraft 游戏、修改后的游戏文件、玩家账号或社团整合包。游戏下载能力由用户操作，游戏及服务器使用仍受各服务条款约束。

## 来源与致谢

PCL 原作者：[龙腾猫跃](https://github.com/Meloong-Git/PCL)（[赞助](https://ifdian.net/a/LTCat)）。感谢 [PCL Community](https://github.com/PCL-Community/PCL-CE) 与 [SHOU-PCL / Conduit-Club](https://github.com/Conduit-Club/SHOU-PCL)（原社团版贡献 Moeary / Codex）。

首轮来源基线是 SHOU-PCL main 的 842393281c5ed8bb5745efc37ce775bd0a21f4a1（v1.2.0 / zyf）。来源仓库现已改名为 SHOU-Launcher；旧链接由 GitHub 重定向，当前修复没有迁入其新架构或改变原基线。[来源记录](docs/wzbc/SOURCE-PARITY.md)与[历史](docs/conduit/HISTORY.md)保留。后续来源同步仅跟踪该社团仓库。
