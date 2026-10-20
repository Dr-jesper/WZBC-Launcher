# WZBC-Launcher 开发约定

- 本项目由 Dr-jesper 与 Codex 共同维护，README 与关于页须明确标注；SHOU-PCL 等来源贡献者的署名继续保留。

- 本仓库为温州商学院 Minecraft 社团定制，基于 SHOU-PCL main 的 842393281c5ed8bb5745efc37ce775bd0a21f4a1（v1.2.0 / zyf）。保留原功能、许可证、作者署名、赞助链接与第三方声明。
- origin 为 https://github.com/Dr-jesper/WZBC-Launcher.git；SHOU-PCL 来源远程可命名为 shou。只向自己的 fork 推送。来源更新只跟踪 SHOU-PCL，不直接同步 PCL-CE。
- main 为发行基线，WZBC-Launcher 为首轮定制分支。修改前阅读本文件、CONTRIBUTING.md 与相关模块文档，核对分支、远程及工作区现有修改。
- 保留无关改动。不强推、重置或删除用户文件。提交、推送、PR、合并与发布遵循用户在当前会话中的授权；不得上传旧备份分支、stash 或恢复历史。
- WZBC 品牌紫为 #601986。奶蛙配色与奶蛙黄为独立选项（枚举 6 / 7），保留原主题索引与用户选择，不在更新或更换目录时重置主题。
- 账号、共享配置和缓存使用 WZBC 独立目录。Microsoft Client ID 与外部服务配置使用项目自己的授权，不借用原社团凭据。缺少配置须显示真实状态。
- 源码只收录必要的优化素材；不提交原始参考图、本地 SDK、账号配置、密钥、日志、截图、数据库、备份或构建目录。发行包只包含构建脚本列明的文件，作为 Release 附件上传。
- 构建与验证见 docs/wzbc/；docs/conduit/ 保留来源历史。默认中文沟通，分别报告编译、自动测试、界面检查和真实登录/游戏验收，不把测试成功等同于全部功能已验收。
