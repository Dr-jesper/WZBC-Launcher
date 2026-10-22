# Minecraft AppID 再次申请准备情况

核对日期：2026-10-23。结论：**已进行仓库资料及安全修复，但当前不能称为可以直接重新申请的已整改完成版本。**

## 已确认与未确认

- Microsoft 应用注册已完成。应用注册成功和 Minecraft Services 授权是两件事。
- 维护者报告 AppID 申请于 2026-10-21 被拒。本轮未读取拒绝邮件原件，也未据此推断具体原因；不公开邮件或 Azure 私人资料。
- 本轮没有再次提交申请，没有新建应用/邮箱来规避审核，也没有删除原 Client ID。
- 未取得允许当前 WZBC 命名及紫色/奶蛙主题的 PCL 单独授权。[许可核对](../../LICENSING.md)保留此问题。
- 先前读取的官方 AppID 表单明确要求在启用游戏或服务器访问前验证所有权并进行认证，且不因服务器配置而例外。当前微软路径有 entitlement 检查，其他路径没有统一核验；第三方认证不应被描述成微软正版验证。
- 本轮未找到可确认的同一 App ID 被拒后复审受理保证。没有检索到禁止规定，也不能当成正式允许或必然受理的证明。

## 本轮实际修复

三语 README 统一为 WZBC，提供显著第三方声明、责任人、有效邮箱和混合许可说明；新增隐私及安全报告说明；关于页提前展示原作者署名与赞助，并列出维护者、非官方身份、支持、隐私和许可入口。

日志支持字段及已知启动凭据改为完整脱敏，补充日志退出时尾部刷新；无 Sentry 配置时不再继续采集环境。微软认证增加可注入的测试网络客户端，不改变默认端点/验证顺序；模拟设备码、拒绝/过期、刷新及 entitlement 等边界。现有离线/第三方功能保留，明确它们不证明游戏所有权。

**这些修改不等于已获 Mojang 或上游授权，也不能证明拒绝原因已消除。**

## 重新提交前仍需完成

1. 解决并记录 PCL 衍生使用依据；不要仅通过改 README 声称已授权。
2. 确定并落实满足表单要求的所有权/认证策略，使其覆盖实际启动入口，而不是只改变界面文案。
3. 保留离线/模拟测试记录，补充可获授权条件下的真实流程验收；Minecraft Services 受限时准确记录阻塞阶段，不共享令牌、设备码或玩家资料。
4. 核对当时官方表单和同 ID 复审路径。若再次提交，提供真实改动提交及先前拒绝事实，不猜测原因，不承诺获批。

## 依据

- [Minecraft Usage Guidelines](https://www.minecraft.net/en-us/usage-guidelines)：非官方身份、责任人、联系渠道及游戏文件分发要求。
- [Minecraft EULA](https://www.minecraft.net/en-us/eula)。
- [此前使用的官方 AppID 表单](https://forms.cloud.microsoft/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR-ajEQ1td1ROpz00KtS8Gd5UNVpPTkVLNFVROVQxNkdRMEtXVjNQQjdXVC4u)：本轮公开抓取无法读取正文，表单要求引用自 2026-10-21 对实时页面的核对，提交前应再次读取当前页面。
- [本地保留许可](../../Plain%20Craft%20Launcher%202/LICENCE)及 [PCL 原项目许可入口](https://github.com/Meloong-Git/PCL/blob/main/LICENCE)。
