# WZBC-Launcher 构建与发行状态

更新：2026-10-23。源码修复版本 1.2.0-wzbc.3；历史发行 v1.2.0-wzbc.2 不包含这批修复。Windows / .NET 10 SDK，本轮验证 SDK 10.0.402。

**PCL 衍生使用依据仍待核实，新的公开二进制发行暂缓。** 本地编译及通过测试不产生修改或分发授权，详见 [LICENSING.md](../../LICENSING.md)。

```powershell
# 本地编译
dotnet build 'Plain Craft Launcher 2/Plain Craft Launcher 2.csproj' -c Beta -p:Platform=x64 -p:UseSharedCompilation=false
# 完整离线回归，使用模拟认证数据，不使用真实账号
dotnet test PCL.Core.Test/PCL.Core.Test.csproj -c Beta -p:Platform=x64 --filter 'TestCategory!=Integration' --blame-hang-timeout 2m
# 自包含构建检查，不创建分发 ZIP，不上传 EXE
./scripts/conduit/Publish.ps1 -BuildOnly -OutputRoot artifacts/review
```

可通过 -Dotnet 'path/to/dotnet.exe' 指定 SDK。脚本支持 ARM64 构建参数，但未进行该架构实机验收。输出目录必须尚不存在。

## 构建内容与数据

BuildOnly 输出 EXE、许可、隐私/安全/待办文档与 BUILD-INFO.txt，仅用于本地检查。构建信息记录源码提交、工作区状态、版本、SDK、架构、配置和 Microsoft Client ID 是否注入。源码提交不收录本机运行目录、账号、密钥、日志、SDK 或原始参考图。

脚本的打包模式保留供分发依据确认后的流程使用；严格限定为 EXE、LICENSE、LICENCE、BUILD.md、PENDING.md、BUILD-INFO.txt、PRIVACY.md、SECURITY.md、LICENSING.md。它不是授权证明，当前不得据此自动恢复公开发行。

## Microsoft 与其他服务

应用已完成注册，Minecraft Services 审核未获批准；维护者报告 2026-10-21 被拒。配置自己的公开 Client ID 只能启用客户端流程，不能代替服务授权或保证 Minecraft 登录成功。当前修复没有注入其他项目的 App ID、密钥或 Sentry DSN。

脚本只接受 .env.example 所述公开 Client ID 与源码 SHA 输入，隔离其他 PCL_* 环境变量。官网启动日志源未启用；外部授权服务和真实流程限制见 [PENDING.md](PENDING.md)。

## GitHub Actions

WZBC Build 对 main、dev、WZBC-Launcher 推送/PR 和手动运行执行 BuildOnly 及完整离线测试；只上传测试报告，不创建新分发 ZIP，不上传新的二进制包，不自动发布 Release。仓库权限为 contents: read。

来源基线与历史见 [SOURCE-PARITY.md](SOURCE-PARITY.md)；本轮验证见 [VALIDATION.md](VALIDATION.md)。不要用来源社团 docs/conduit 中的历史批准或服务配置代表 WZBC 的状态。
