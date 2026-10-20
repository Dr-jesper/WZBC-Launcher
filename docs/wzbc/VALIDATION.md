# WZBC 预览版验证记录

日期：2026-10-20。Windows x64，.NET SDK 10.0.402，Beta 自包含构建。

## 已完成

- SHOU-PCL main / dev 来源核对：主线源码树一致，来源跟踪文件无缺失，关键模块一致性见 [SOURCE-PARITY.md](SOURCE-PARITY.md)。
- 离线回归：发行源接入后完整检查 332 项通过，0 失败、0 跳过。覆盖社团入口、认证来源识别、账号管理、版本解析、日志解析、主题资源与对比度，以及默认更新源接受 WZBC、拒绝原社团和相似仓库的边界。云端结果可在 [GitHub Actions](https://github.com/Dr-jesper/WZBC-Launcher/actions/workflows/wzbc-build.yml) 查看。
- Windows x64 自包含包构建成功。发行构建信息与源码提交写入包内 BUILD-INFO.txt；附件另附 SHA-256。
- 实际界面检查：WZBC 品牌、校园轮播、设置页、两种奶蛙主题的深浅模式、顶栏与侧栏可读性、切回 WZBC、更新解压到新目录后保留主题选择。
- 官网、皮肤站公开认证元数据与官网校园地图可访问；这仅证明公开入口可读。

## 验证边界

本记录不代表真实账号登录、令牌刷新、游戏下载、整合包安装、Minecraft 启动或服务器准入已经通过。未在用户真实账号或整合包上执行上述验收。Microsoft Client ID 仍在申请，ARM64 与代码签名亦未验收。详细限制见 [PENDING.md](PENDING.md)。

测试原始日志、桌面截图、运行配置与历史包只保留本地。发行附件只含应用、许可、构建信息与使用限制。
