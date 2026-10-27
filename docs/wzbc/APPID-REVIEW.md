# Microsoft 登录服务说明

更新：2026-10-27。

本项目使用自己的公开 Microsoft Client ID `b42a22d0-3ca4-450d-8389-a466092f3ff7`，仅面向个人 Microsoft 账户，采用设备授权流程。应用注册及 Azure 展示资料已完成；Minecraft Services 尚未批准，因此当前 Microsoft 登录不可用。

此前本人授权测试已到达 Xbox Live / XSTS，Minecraft Services 换票返回 HTTP 403，未到达游戏权益和 Java 玩家资料请求。没有据此确定具体拒审原因或应用封禁状态。

Microsoft 路径包含游戏权益检查。离线角色和第三方 Yggdrasil 账户沿用上游方式，不绑定 Microsoft 账号，也不证明 Microsoft 游戏所有权。详细行为见 [ACCOUNT-ADMISSION.md](ACCOUNT-ADMISSION.md)。

服务申请与源代码发布分别处理；发布更新不会自动更改服务审批状态。申请材料仅保存在本地，本次没有提交新的申请。
