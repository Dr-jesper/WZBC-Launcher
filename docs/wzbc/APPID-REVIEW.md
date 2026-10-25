# Minecraft AppID 再次申请准备情况

核对日期：2026-10-25。结论：**已同步可审阅源码和 Azure 展示资料；服务授权、完整真实验收及全路径游戏权益要求仍未闭合。** [审核版本入口](REVIEW.md)集中列出证据。

## 已确认与未确认

- Microsoft 应用注册已完成；2026-10-25 Azure 主页、隐私、条款、徽标与维护备注已保存，个人账户类型和公共客户端流已核对。应用注册成功和 Minecraft Services 授权是两件事。
- 维护者报告 AppID 申请于 2026-10-21 被拒。本轮未读取拒绝邮件原件，也未据此推断具体原因；不公开邮件或 Azure 私人资料。
- 2026-10-23 已用同一 Client ID 提交第二轮申请；维护者提供当日未通过通知。2026-10-24 没有提交第三轮、创建新应用/邮箱或删除原 Client ID。
- 2026-10-24 本地自有 ID 实测：OAuth、Xbox Live、XSTS 成功，Minecraft Services 换票 HTTP 403；权益与 Java profile 未到达。403 本身不能确定具体拒审原因或封禁状态。详见 [验证记录](VALIDATION.md)。
- PCL 继承许可的名称、主题和分发范围单独记在[核对文档](PCL-LICENSE-REVIEW.md)。
- 先前读取的官方 AppID 表单要求在启用游戏或服务器访问前认证并核验所有权。当前已按维护者选择统一启动准入并保留上游三类身份，但只在 Microsoft 路径核验游戏权益；第三方认证和离线身份不证明 Microsoft 所有权。统一准入不等于全路径权益要求已完成，提交前仍需核对当时表单。
- 本轮未找到可确认的同一 App ID 被拒后复审受理保证。没有检索到禁止规定，也不能当成正式允许或必然受理的证明。

## 本轮实际修复

三语 README 统一为 WZBC，提供显著第三方声明、责任人、有效邮箱和混合许可说明；新增隐私及安全报告说明；关于页提前展示原作者署名与赞助，并列出维护者、非官方身份、支持、隐私和许可入口。

日志支持字段及已知启动凭据改为完整脱敏，补充日志退出时尾部刷新；无 Sentry 配置时不再继续采集环境。微软认证增加可注入的测试网络客户端，不改变默认端点/验证顺序；模拟设备码、拒绝/过期、刷新及 entitlement 等边界。现有离线/第三方功能保留，明确它们不证明游戏所有权。

**这些修改不等于已获 Mojang 或上游授权，也不能证明拒绝原因已消除。**

2026-10-24 至 10-25 同步增加阶段化 HTTP 错误提示、明确拒绝后的缓存阻断、否定状态跨重启保存、所选账号/角色/认证源隔离，以及启动过程会话复核。完整离线回归 408 项通过；真实 Services 403 没有解除。Azure 隐私和条款公开链接及徽标已完成；没有提交第三轮。

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
